using Nucleus.Gaming.Windows;
using Nucleus.Gaming.Windows.Interop;
using Nucleus.Interop.User32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Nucleus.Gaming.Coop.Generic.Cursor;
using WindowScrape.Constants;
using WindowScrape.Types;
using Nucleus.Gaming.Coop;
using System.Reflection;
using Nucleus.Gaming.Tools.GameStarter;
using EasyHook;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;

namespace Nucleus.Gaming
{
    public class GenericGameHandler : IGameHandler, ILogNode
    {
        private const float HWndInterval = 10000;

        [DllImport("User32")]
        private static extern int ShowWindow(int hwnd, int nCmdShow);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hWnd, ref RECT Rect);

        private int origWidth = 0;
        private int origHeight = 0;
        private double origRatio = 1;
        private int playerBoundsWidth = 0;
        private int playerBoundsHeight = 0;
        private int prevWindowWidth = 0;
        private int prevWindowHeight = 0;
        private int prevWindowX = 0;
        private int prevWindowY = 0;
        private ProcessData prevProcessData;

        private UserGameInfo userGame;
        private GameProfile profile;
        private GenericGameInfo gen;
        private Dictionary<string, string> jsData;

        private double timer;
        private int exited;
        private List<Process> attached = new List<Process>();
        private List<int> attachedIds = new List<int>() { 0 };

        protected bool hasEnded;
        protected double timerInterval = 1000;

        public event Action Ended;
        private CursorModule _cursorModule;

        private bool hasKeyboardPlayer = false;

        private Thread fakeFocus;

        private bool dllResize = false;
        private bool dllRepos = false;

        private readonly IniFile ini = new Gaming.IniFile(Path.Combine(Directory.GetCurrentDirectory(), "Settings.ini"));

        public enum MachineType : ushort
        {
            IMAGE_FILE_MACHINE_UNKNOWN = 0x0,
            IMAGE_FILE_MACHINE_AM33 = 0x1d3,
            IMAGE_FILE_MACHINE_AMD64 = 0x8664,
            IMAGE_FILE_MACHINE_ARM = 0x1c0,
            IMAGE_FILE_MACHINE_EBC = 0xebc,
            IMAGE_FILE_MACHINE_I386 = 0x14c,
            IMAGE_FILE_MACHINE_IA64 = 0x200,
            IMAGE_FILE_MACHINE_M32R = 0x9041,
            IMAGE_FILE_MACHINE_MIPS16 = 0x266,
            IMAGE_FILE_MACHINE_MIPSFPU = 0x366,
            IMAGE_FILE_MACHINE_MIPSFPU16 = 0x466,
            IMAGE_FILE_MACHINE_POWERPC = 0x1f0,
            IMAGE_FILE_MACHINE_POWERPCFP = 0x1f1,
            IMAGE_FILE_MACHINE_R4000 = 0x166,
            IMAGE_FILE_MACHINE_SH3 = 0x1a2,
            IMAGE_FILE_MACHINE_SH3DSP = 0x1a3,
            IMAGE_FILE_MACHINE_SH4 = 0x1a6,
            IMAGE_FILE_MACHINE_SH5 = 0x1a8,
            IMAGE_FILE_MACHINE_THUMB = 0x1c2,
            IMAGE_FILE_MACHINE_WCEMIPSV2 = 0x169,
        }

        public static MachineType GetDllMachineType(string dllPath)
        {
            // See http://www.microsoft.com/whdc/system/platform/firmware/PECOFF.mspx
            // Offset to PE header is always at 0x3C.
            // The PE header starts with "PE\0\0" =  0x50 0x45 0x00 0x00,
            // followed by a 2-byte machine type field (see the document above for the enum).
            //
            FileStream fs = new FileStream(dllPath, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            fs.Seek(0x3c, SeekOrigin.Begin);
            Int32 peOffset = br.ReadInt32();
            fs.Seek(peOffset, SeekOrigin.Begin);
            UInt32 peHead = br.ReadUInt32();

            if (peHead != 0x00004550) // "PE\0\0", little-endian
                throw new Exception("Can't find PE header");

            MachineType machineType = (MachineType)br.ReadUInt16();
            br.Close();
            fs.Close();
            return machineType;
        }

        // Returns true if the exe is 64-bit, false if 32-bit, and null if unknown
        public static bool? Is64Bit(string exePath)
        {
            switch (GetDllMachineType(exePath))
            {
                case MachineType.IMAGE_FILE_MACHINE_AMD64:
                case MachineType.IMAGE_FILE_MACHINE_IA64:
                    return true;
                case MachineType.IMAGE_FILE_MACHINE_I386:
                    return false;
                default:
                    return null;
            }
        }

        [DllImport("user32.dll")]
        static extern bool SetWindowText(IntPtr hWnd, string text);


        [DllImport("EasyHook32.dll", CharSet = CharSet.Ansi)]
        public static extern int RhCreateAndInject(
            [MarshalAsAttribute(UnmanagedType.LPWStr)] string InEXEPath,
            [MarshalAsAttribute(UnmanagedType.LPWStr)] string InCommandLine,
            int InProcessCreationFlags,
            int InInjectionOptions,
            [MarshalAsAttribute(UnmanagedType.LPWStr)] string InLibraryPath_x86,
            [MarshalAsAttribute(UnmanagedType.LPWStr)] string InLibraryPath_x64,
            IntPtr InPassThruBuffer,
            int InPassThruSize,
            IntPtr OutProcessId //Pointer to a UINT (the PID of the new process)
            );

        public Thread FakeFocus
        {
            get { return fakeFocus; }
        }

        private enum FocusMessages
        {
            WM_ACTIVATEAPP = 0x001C,
            WM_ACTIVATE = 0x0006,
            WM_NCACTIVATE = 0x0086,
            WM_SETFOCUS = 0x0007
        }

        public virtual bool HasEnded
        {
            get { return hasEnded; }
        }

        public double TimerInterval
        {
            get { return timerInterval; }
        }

        private void ForceFinish()
        {
            // search for game instances left behind
            try
            {
                Process[] procs;
                if(gen.GameName == "Halo Custom Edition")
                {
                    procs = Process.GetProcessesByName("haloce");
                }
                else
                {
                    procs = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(gen.ExecutableName.ToLower()));
                }
                
                if (procs.Length > 0)
                {
                    for (int i = 0; i < procs.Length; i++)
                    {
                        procs[i].Kill();
                    }
                }
            }
            catch { }
        }

        public void End()
        {

            if (fakeFocus != null && fakeFocus.IsAlive)
            {
                fakeFocus.Abort();
            }

            User32Util.ShowTaskBar();

            hasEnded = true;
            GameManager.Instance.ExecuteBackup(this.userGame.Game);

            LogManager.UnregisterForLogCallback(this);

            Cursor.Clip = Rectangle.Empty; // guarantee were not clipping anymore
            string backupDir = GameManager.Instance.GempTempFolder(this.userGame.Game);
            ForceFinish();

            if (_cursorModule != null)
                _cursorModule.Stop();

            Thread.Sleep(1000);
            // delete symlink folder
            try
            {
#if RELEASE
                if(gen.KeepSymLinkOnExit == false)
                {
                    for (int i = 0; i < profile.PlayerData.Count; i++)
                    {
                        string linkFolder = Path.Combine(backupDir, "Instance" + i);
                        if (Directory.Exists(linkFolder))
                        {
                            Directory.Delete(linkFolder, true);
                        }
                    }
                }
#endif
            }
            catch
            {

            }

            if (Ended != null)
            {
                Ended();
            }
        }

        public string GetFolder(Folder folder)
        {
            string str = folder.ToString();
            string output;
            if (jsData.TryGetValue(str, out output))
            {
                return output;
            }
            return "";
        }

        public bool Initialize(UserGameInfo game, GameProfile profile)
        {
            this.userGame = game;
            this.profile = profile;


            List<PlayerInfo> players = profile.PlayerData;
            gen = game.Game as GenericGameInfo;
            // see if we have any save game to backup
            if (gen == null)
            {
                // you fucked up
                return false;
            }

            try
            {
                // if there's a keyboard player, re-order play list
                hasKeyboardPlayer = players.Any(c => c.IsKeyboardPlayer);
                if (hasKeyboardPlayer)
                {
                    if (gen.KeyboardPlayerFirst)
                    {
                        players.Sort((x, y) => y.IsKeyboardPlayer.CompareTo(x.IsKeyboardPlayer));
                    }
                    else
                    {
                        players.Sort((x, y) => x.IsKeyboardPlayer.CompareTo(y.IsKeyboardPlayer));
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message + " " + ex.StackTrace);
            }


            if (gen.LockMouse)
                _cursorModule = new CursorModule();

            jsData = new Dictionary<string, string>();
            jsData.Add(Folder.Documents.ToString(), Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            jsData.Add(Folder.MainGameFolder.ToString(), Path.GetDirectoryName(game.ExePath));
            jsData.Add(Folder.InstancedGameFolder.ToString(), Path.GetDirectoryName(game.ExePath));

            timerInterval = gen.HandlerInterval;

            LogManager.RegisterForLogCallback(this);

            return true;
        }

        public string ReplaceCaseInsensitive(string str, string toFind, string toReplace)
        {
            string lowerOriginal = str.ToLower();
            string lowerFind = toFind.ToLower();
            string lowerRep = toReplace.ToLower();

            int start = lowerOriginal.IndexOf(lowerFind);
            if (start == -1)
            {
                return str;
            }

            string end = str.Remove(start, toFind.Length);
            end = end.Insert(start, toReplace);

            return end;
        }

        private static string GetRootFolder(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            int failsafe = 20;
            for (;;)
            {
                failsafe--;
                if (failsafe < 0)
                {
                    break;
                }

                string temp = Path.GetDirectoryName(path);
                if (String.IsNullOrEmpty(temp))
                {
                    break;
                }
                path = temp;
            }
            return path;
        }

        private static string internalGetRelativePath(DirectoryInfo dirInfo, DirectoryInfo rootInfo, string str)
        {
            if (dirInfo.FullName == rootInfo.FullName || dirInfo == null)
            {
                return str;
            }

            if (!string.IsNullOrWhiteSpace(Path.GetExtension(dirInfo.Name)))
            {
                str = dirInfo.Name;
            }
            else
            {
                str = dirInfo.Name + "\\" + str;
            }

            dirInfo = dirInfo.Parent;
            return internalGetRelativePath(dirInfo, rootInfo, str);
        }

        public string GetRelativePath(string dirPath, string rootFolder)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(dirPath);
            DirectoryInfo rootInfo = new DirectoryInfo(rootFolder);
            return internalGetRelativePath(dirInfo, rootInfo, "");
        }

        public string Play()
        {
            ForceFinish();

            List<PlayerInfo> players = profile.PlayerData;

            for (int i = 0; i < players.Count; i++)
            {
                players[i].PlayerID = i;
            }

            UserScreen[] all = ScreensUtil.AllScreens();

            string nucleusRootFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            string tempDir = GameManager.Instance.GempTempFolder(gen);
            string exeFolder = Path.GetDirectoryName(userGame.ExePath).ToLower();
            string rootFolder = exeFolder;
            string workingFolder = exeFolder;
            if (!string.IsNullOrEmpty(gen.BinariesFolder))
            {
                rootFolder = ReplaceCaseInsensitive(exeFolder, gen.BinariesFolder.ToLower(), "");
            }
            if (!string.IsNullOrEmpty(gen.WorkingFolder))
            {
                workingFolder = Path.Combine(exeFolder, gen.WorkingFolder.ToLower());
            }

            bool first = true;
            bool keyboard = false;
            //if (gen.SupportsKeyboard)
            //{
            //    // make sure the keyboard player is the last to be started,
            //    // so it will get the focus by default
            //    KeyboardPlayer player = (KeyboardPlayer)profile.Options["KeyboardPlayer"];
            //    if (player.Value != -1)
            //    {
            //        keyboard = true;
            //        List<PlayerInfo> newPlayers = new List<PlayerInfo>();

            //        for (int i = 0; i < players.Count; i++)
            //        {
            //            PlayerInfo p = players[i];
            //            if (i == player.Value)
            //            {
            //                continue;
            //            }

            //            newPlayers.Add(p);
            //        }
            //        newPlayers.Add(players[player.Value]);
            //        players = newPlayers;
            //    }
            //}

            for (int i = 0; i < players.Count; i++)
            {
                PlayerInfo player = players[i];
                ProcessData procData = player.ProcessData;
                bool hasSetted = procData != null && procData.Setted;

                //SlimDX.XInput.Controller gamePad = new SlimDX.XInput.Controller(SlimDX.XInput.UserIndex.One);
                //SlimDX.XInput.Vibration vib = new SlimDX.XInput.Vibration();

                //vib.LeftMotorSpeed = 32000;
                //vib.RightMotorSpeed = 16000;
                //gamePad.SetVibration(vib);

                //SlimDX.DirectInput.Get
                //SlimDX.DirectInput.Joystick device = new SlimDX.DirectInput.Joystick()

                if (!gen.RenameNotKillMutex && i > 0 && (gen.KillMutex?.Length > 0 || !hasSetted))
                {
                    PlayerInfo before = players[i - 1];
                    for (;;)
                    {
                        if (exited > 0)
                        {
                            return "";
                        }
                        Thread.Sleep(1000);

                        if (gen.KillMutex != null)
                        {
                            if (gen.KillMutex.Length > 0 && !before.ProcessData.KilledMutexes)
                            {
                                // check for the existence of the mutexes
                                // before invoking our StartGame app to kill them
                                ProcessData pdata = before.ProcessData;

                                if(gen.KillMutexType == null)
                                {
                                    gen.KillMutexType = "Mutant";
                                }

                                if(gen.KillMutexDelay > 0)
                                {
                                    Thread.Sleep((gen.KillMutexDelay * 1000));
                                }

                                if (StartGameUtil.MutexExists(pdata.Process, gen.KillMutexType, gen.PartialMutexSearch, gen.KillMutex))
                                {
                                    // mutexes still exist, must kill
                                    StartGameUtil.KillMutex(pdata.Process, gen.KillMutexType, gen.PartialMutexSearch, gen.KillMutex);
                                    pdata.KilledMutexes = true;
                                    break;
                                }
                                else
                                {
                                    // mutexes dont exist anymore
                                    break;
                                }
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                Rectangle playerBounds = player.MonitorBounds;
                UserScreen owner = player.Owner;

                int width = playerBounds.Width;
                int height = playerBounds.Height;
                bool isFullscreen = owner.Type == UserScreenType.FullScreen;

                string exePath;
                string linkFolder;
                string linkBinFolder;

                if (gen.SymlinkGame || gen.HardcopyGame)
                {
                    List<string> dirExclusions = new List<string>();
                    List<string> fileExclusions = new List<string>();
                    List<string> fileCopies = new List<string>();

                    // symlink the game folder (and not the bin folder, if we have one)
                    linkFolder = Path.Combine(tempDir, "Instance" + i);
                    Directory.CreateDirectory(linkFolder);

                    linkBinFolder = linkFolder;
                    if (!string.IsNullOrEmpty(gen.BinariesFolder))
                    {
                        linkBinFolder = Path.Combine(linkFolder, gen.BinariesFolder);
                        dirExclusions.Add(gen.BinariesFolder);
                    }
                    exePath = Path.Combine(linkBinFolder, this.userGame.Game.ExecutableName);

                    //if (gen.ForceWindowedMethodA)
                    //{
                    //    byte[] d3d9 = Properties.Resources.d3d9;

                    //    using (Stream str = File.OpenWrite(Path.Combine(linkBinFolder, "d3d9.dll")))
                    //    {
                    //        str.Write(d3d9, 0, d3d9.Length);
                    //    }
                    //    //File.Copy(Properties.Resources.d3d9, )
                    //}

                    if (gen.SymlinkFiles != null)
                    {
                        string[] filesToSymlink = gen.SymlinkFiles;
                        for (int f = 0; f < filesToSymlink.Length; f++)
                        {
                            string s = filesToSymlink[f].ToLower();
                            // make sure it's lower case
                            CmdUtil.MkLinkFile(Path.Combine(rootFolder, s), Path.Combine(linkFolder, s), out int exitCode);
                        }
                    }

                    if (gen.CopyFiles != null)
                    {
                        string[] filesToCopy = gen.CopyFiles;
                        for (int c = 0; c < filesToCopy.Length; c++)
                        {
                            string s = filesToCopy[c].ToLower();
                            File.Copy(Path.Combine(rootFolder, s), Path.Combine(linkFolder, s), true);
                        }
                    }

                    if (!string.IsNullOrEmpty(gen.WorkingFolder))
                    {
                        linkBinFolder = Path.Combine(linkFolder, gen.WorkingFolder);
                        dirExclusions.Add(gen.WorkingFolder);
                    }

                    // some games have save files inside their game folder, so we need to access them inside the loop
                    jsData[Folder.InstancedGameFolder.ToString()] = linkFolder;

                    if (gen.Hook.CustomDllEnabled)
                    {
                        fileExclusions.Add("xinput1_3.dll");
                        fileExclusions.Add("ncoop.ini");
                    }
                    if (!gen.SymlinkExe)
                    {
                        fileCopies.Add(gen.ExecutableName.ToLower());
                    }

                    // additional ignored files by the generic info
                    if (gen.FileSymlinkExclusions != null)
                    {
                        string[] symlinkExclusions = gen.FileSymlinkExclusions;
                        for (int k = 0; k < symlinkExclusions.Length; k++)
                        {
                            string s = symlinkExclusions[k];
                            // make sure it's lower case
                            fileExclusions.Add(s.ToLower());
                        }
                    }
                    if (gen.FileSymlinkCopyInstead != null)
                    {
                        string[] fileSymlinkCopyInstead = gen.FileSymlinkCopyInstead;
                        for (int k = 0; k < fileSymlinkCopyInstead.Length; k++)
                        {
                            string s = fileSymlinkCopyInstead[k];
                            // make sure it's lower case
                            fileCopies.Add(s.ToLower());
                        }
                    }
                    if (gen.DirSymlinkExclusions != null)
                    {
                        string[] symlinkExclusions = gen.DirSymlinkExclusions;
                        for (int k = 0; k < symlinkExclusions.Length; k++)
                        {
                            string s = symlinkExclusions[k];
                            // make sure it's lower case
                            dirExclusions.Add(s.ToLower());
                        }
                    }

                    string[] fileExclusionsArr = fileExclusions.ToArray();
                    string[] fileCopiesArr = fileCopies.ToArray();

                    if (gen.HardcopyGame)
                    {
                        // copy the directory
                        int exitCode;
                        FileUtil.CopyDirectory(rootFolder, new DirectoryInfo(rootFolder), linkFolder, out exitCode, dirExclusions.ToArray(), fileExclusionsArr, true);
                    }
                    else
                    {
                        int exitCode;
                        CmdUtil.LinkDirectory(rootFolder, new DirectoryInfo(rootFolder), linkFolder, out exitCode, dirExclusions.ToArray(), fileExclusionsArr, fileCopiesArr, true);

                        if (!gen.SymlinkExe)
                        {
                            //File.Copy(userGame.ExePath, exePath, true);
                        }
                    }
                }
                else
                {
                    exePath = userGame.ExePath;
                    linkBinFolder = rootFolder;
                    linkFolder = workingFolder;
                }

                if(gen.ChangeExe)
                {
                    string newExe = Path.GetFileNameWithoutExtension(this.userGame.Game.ExecutableName) + " - Player " + (i + 1) + ".exe";
                    if(File.Exists(Path.Combine(linkBinFolder, newExe)))
                    {
                        File.Delete(Path.Combine(linkBinFolder, newExe));
                    }
                    File.Move(Path.Combine(linkBinFolder, this.userGame.Game.ExecutableName), Path.Combine(linkBinFolder, newExe));
                    exePath = Path.Combine(linkBinFolder, newExe);
                }


                GenericContext context = gen.CreateContext(profile, player, this, hasKeyboardPlayer);
                context.PlayerID = player.PlayerID;
                context.IsFullscreen = isFullscreen;

                context.ExePath = exePath;
                context.RootInstallFolder = exeFolder;
                context.RootFolder = linkFolder;

                if (gen.UseX360ce)
                {
                    string x360exe = "";
                    string x360dll = "";
                    string utilFolder = Path.Combine(Directory.GetCurrentDirectory(), "utils\\x360ce");
                    if (i == 0)
                    {
                        if (Is64Bit(exePath) == true)
                        {
                            x360exe = "x360ce_x64.exe";
                            x360dll = "xinput1_3_x64.dll";
                        }
                        else if (Is64Bit(exePath) == false)
                        {
                            x360exe = "x360ce.exe";
                            x360dll = "xinput1_3.dll";
                        }
                        else
                        {
                            using (StreamWriter writer = new StreamWriter("error-log.txt", true))
                            {
                                writer.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]" + "Machine type: '{0}' not implemented.", GetDllMachineType(exePath));
                            }
                        }


                        if (!File.Exists(Path.Combine(linkBinFolder, x360exe)))
                        {
                            //File.Delete(Path.Combine(linkBinFolder, "x360ce.exe"));
                            File.Copy(Path.Combine(utilFolder, x360exe), Path.Combine(linkBinFolder, x360exe));
                        }

                        if (!File.Exists(Path.Combine(linkBinFolder, "xinput1_3.dll")))
                        {
                            //File.Delete(Path.Combine(linkBinFolder, "x360ce.exe"));
                            File.Copy(Path.Combine(utilFolder, x360dll), Path.Combine(linkBinFolder, "xinput1_3.dll"));
                        }

                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.UseShellExecute = true;
                        startInfo.WorkingDirectory = Path.GetDirectoryName(exePath);
                        startInfo.FileName = Path.Combine(linkBinFolder, x360exe);
                        Process util = Process.Start(startInfo);
                        util.WaitForExit();
                    }
                    else
                    {
                        if (File.Exists(Path.Combine(linkBinFolder, "x360ce.ini")))
                        {
                            File.Delete(Path.Combine(linkBinFolder, "x360ce.ini"));
                        }
                        File.Copy(Path.Combine(linkBinFolder.Substring(0, linkBinFolder.LastIndexOf('\\') + 1) + "Instance0", "xinput1_3.dll"), Path.Combine(linkBinFolder, "xinput1_3.dll"));
                        File.Copy(Path.Combine(linkBinFolder.Substring(0,linkBinFolder.LastIndexOf('\\') + 1) + "Instance0", "x360ce.ini"), Path.Combine(linkBinFolder, "x360ce.ini"));
                    }

                    string[] change = new string[] { context.FindLineNumberInTextFile(Path.Combine(linkBinFolder, "x360ce.ini"), "PAD1=", SearchType.StartsWith) + "|PAD1=" + context.GamepadGuid };

                    context.ReplaceLinesInTextFile(Path.Combine(linkBinFolder, "x360ce.ini"), change);

                }

                gen.PrePlay(context, this, player);

                string startArgs = context.StartArguments;

                if (context.Hook.CustomDllEnabled)
                {
                    byte[] xdata;
                    if (gen.Hook.UseAlpha8CustomDll && Is64Bit(exePath) != true)
                    {
                        xdata = Properties.Resources.xinput1_3;
                    }
                    else
                    {
                        if (Is64Bit(exePath) == true)
                        {
                            xdata = Properties.Resources.xinput1_3_a10_x64;
                        }
                        else if (Is64Bit(exePath) == false)
                        {
                            xdata = Properties.Resources.xinput1_3_a10;
                        }
                        else
                        {
                            xdata = null;
                            using (StreamWriter writer = new StreamWriter("error-log.txt", true))
                            {
                                writer.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]" + "Machine type: '{0}' not implemented.", GetDllMachineType(exePath));
                            }
                        }
                    }
                    
                    if (context.Hook.XInputNames == null)
                    {
                        using (Stream str = File.OpenWrite(Path.Combine(linkBinFolder, "xinput1_3.dll")))
                        {
                            str.Write(xdata, 0, xdata.Length);
                        }
                    }
                    else
                    {
                        string[] xinputs = context.Hook.XInputNames;
                        for (int z = 0; z < xinputs.Length; z++)
                        {
                            string xinputName = xinputs[z];
                            using (Stream str = File.OpenWrite(Path.Combine(linkBinFolder, xinputName)))
                            {
                                str.Write(xdata, 0, xdata.Length);
                            }
                        }
                    }

                    string ncoopIni = Path.Combine(linkBinFolder, "ncoop.ini");
                    using (Stream str = File.OpenWrite(ncoopIni))
                    {
                        byte[] ini = Properties.Resources.ncoop;
                        str.Write(ini, 0, ini.Length);
                    }

                    IniFile x360 = new IniFile(ncoopIni);
                    //x360.IniWriteValue("Options", "Log", "0");
                    x360.IniWriteValue("Options", "ForceFocus", gen.Hook.ForceFocus.ToString(CultureInfo.InvariantCulture));
                    if(!gen.Hook.UseAlpha8CustomDll)
                    {
                        //x360.IniWriteValue("Options", "Version", "2");
                        x360.IniWriteValue("Options", "ForceFocusWindowRegex", gen.Hook.ForceFocusWindowName.ToString(CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        x360.IniWriteValue("Options", "ForceFocusWindowName", gen.Hook.ForceFocusWindowName.ToString(CultureInfo.InvariantCulture));
                    }

                    if (context.Hook.WindowX > 0 && context.Hook.WindowY > 0)
                    {
                        x360.IniWriteValue("Options", "WindowX", context.Hook.WindowX.ToString(CultureInfo.InvariantCulture));
                        x360.IniWriteValue("Options", "WindowY", context.Hook.WindowY.ToString(CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        x360.IniWriteValue("Options", "WindowX", playerBounds.X.ToString(CultureInfo.InvariantCulture));
                        x360.IniWriteValue("Options", "WindowY", playerBounds.Y.ToString(CultureInfo.InvariantCulture));
                    }

                    if(context.Hook.ResWidth > 0 && context.Hook.ResHeight > 0)
                    {
                        x360.IniWriteValue("Options", "ResWidth", context.Hook.ResWidth.ToString(CultureInfo.InvariantCulture));
                        x360.IniWriteValue("Options", "ResHeight", context.Hook.ResHeight.ToString(CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        x360.IniWriteValue("Options", "ResWidth", context.Width.ToString(CultureInfo.InvariantCulture));
                        x360.IniWriteValue("Options", "ResHeight", context.Height.ToString(CultureInfo.InvariantCulture));
                    }

                    if(!gen.Hook.UseAlpha8CustomDll)
                    {
                        if (context.Hook.FixResolution)
                        {
                            dllResize = true;
                        }
                        if(context.Hook.FixPosition)
                        {
                            dllRepos = true;
                        }
                        x360.IniWriteValue("Options", "FixResolution", context.Hook.FixResolution.ToString(CultureInfo.InvariantCulture));
                        x360.IniWriteValue("Options", "FixPosition", context.Hook.FixPosition.ToString(CultureInfo.InvariantCulture));
                        x360.IniWriteValue("Options", "ClipMouse", player.IsKeyboardPlayer.ToString(CultureInfo.InvariantCulture)); //context.Hook.ClipMouse
                    }

                    x360.IniWriteValue("Options", "RerouteInput", context.Hook.XInputReroute.ToString(CultureInfo.InvariantCulture));
                    x360.IniWriteValue("Options", "RerouteJoystickTemplate", JoystickDatabase.GetID(player.GamepadProductGuid.ToString()).ToString(CultureInfo.InvariantCulture));

                    x360.IniWriteValue("Options", "EnableMKBInput", player.IsKeyboardPlayer.ToString(CultureInfo.InvariantCulture));

                    // windows events

                    x360.IniWriteValue("Options", "BlockInputEvents", context.Hook.BlockInputEvents.ToString(CultureInfo.InvariantCulture));
                    x360.IniWriteValue("Options", "BlockMouseEvents", context.Hook.BlockMouseEvents.ToString(CultureInfo.InvariantCulture));
                    x360.IniWriteValue("Options", "BlockKeyboardEvents", context.Hook.BlockKeyboardEvents.ToString(CultureInfo.InvariantCulture));

                    // xinput
                    x360.IniWriteValue("Options", "XInputEnabled", context.Hook.XInputEnabled.ToString(CultureInfo.InvariantCulture));
                    x360.IniWriteValue("Options", "XInputPlayerID", player.GamepadId.ToString(CultureInfo.InvariantCulture));

                    // dinput
                    x360.IniWriteValue("Options", "DInputEnabled", context.Hook.DInputEnabled.ToString(CultureInfo.InvariantCulture));
                    x360.IniWriteValue("Options", "DInputGuid", player.GamepadGuid.ToString().ToUpper());
                    x360.IniWriteValue("Options", "DInputForceDisable", context.Hook.DInputForceDisable.ToString());

                    //force feedback
                    //x360.IniWriteValue("Options", "ProductGUID", player.GamepadProductGuid.ToString());
                    //x360.IniWriteValue("Options", "InstanceGUID", player.GamepadGuid.ToString());
                    //x360.IniWriteValue("Options", "UseForceFeedback", "1");
                    //x360.IniWriteValue("Options", "ForcePercent", "100");
                    //x360.IniWriteValue("Options", "ForcesPassThrough", "1");
                    //x360.IniWriteValue("Options", "PassThrough", "0");
                    //x360.IniWriteValue("Options", "PassThroughIndex", "0");
                    //x360.IniWriteValue("Options", "ControllerType", "1");
                    //x360.IniWriteValue("Options", "SwapMotor", "0");
                    //x360.IniWriteValue("Options", "FFBType", "0");
                    //x360.IniWriteValue("Options", "LeftMotorPeriod", "120");
                    //x360.IniWriteValue("Options", "LeftMotorStrength", "0");
                    //x360.IniWriteValue("Options", "LeftMotorDirection", "0");
                    //x360.IniWriteValue("Options", "RightMotorPeriod", "60");
                    //x360.IniWriteValue("Options", "RightMotorStrength", "0");
                    //x360.IniWriteValue("Options", "RightMotorDirection", "0");

                }

                if (ini.IniReadValue("Misc", "UseNicksInGame") == "True")
                {
                    string[] files = Directory.GetFiles(linkFolder, "account_name.txt", SearchOption.AllDirectories);
                    foreach (string nameFile in files)
                    {
                        if(!string.IsNullOrEmpty(player.Nickname))
                        {
                            //MessageBox.Show("Found account_name.txt at: " + nameFile + ", replacing: " + File.ReadAllText(nameFile) + " with: " + player.Nickname + " for player " + i);
                            File.Delete(nameFile);
                            File.WriteAllText(nameFile, player.Nickname);
                        }
                    }
                }

                Process proc;
                if (context.NeedsSteamEmulation)
                {
                    string steamEmu = GameManager.Instance.ExtractSteamEmu(Path.Combine(linkFolder, "SmartSteamLoader"));
                    //string steamEmu = GameManager.Instance.ExtractSteamEmu();
                    if (string.IsNullOrEmpty(steamEmu))
                    {
                        return "Extraction of SmartSteamEmu failed!";
                    }

                    string emuExe = Path.Combine(steamEmu, "SmartSteamLoader.exe");
                    string emuIni = Path.Combine(steamEmu, "SmartSteamEmu.ini");
                    IniFile emu = new IniFile(emuIni);

                    emu.IniWriteValue("Launcher", "Target", exePath);
                    emu.IniWriteValue("Launcher", "StartIn", Path.GetDirectoryName(exePath));
                    emu.IniWriteValue("Launcher", "CommandLine", startArgs);
                    emu.IniWriteValue("Launcher", "SteamClientPath", Path.Combine(steamEmu, "SmartSteamEmu.dll"));
                    emu.IniWriteValue("Launcher", "SteamClientPath64", Path.Combine(steamEmu, "SmartSteamEmu64.dll"));
                    emu.IniWriteValue("Launcher", "InjectDll", "1");

                    emu.IniWriteValue("SmartSteamEmu", "AppId", context.SteamID);
                    //emu.IniWriteValue("SmartSteamEmu", "SteamIdGeneration", "Static");

                    //string userName = $"Player { context.PlayerID }";

                    //emu.IniWriteValue("SmartSteamEmu", "PersonaName", userName);
                    //emu.IniWriteValue("SmartSteamEmu", "ManualSteamId", userName);

                    //emu.IniWriteValue("SmartSteamEmu", "Offline", "False");
                    //emu.IniWriteValue("SmartSteamEmu", "MasterServer", "");
                    //emu.IniWriteValue("SmartSteamEmu", "MasterServerGoldSrc", "");

                    gen.SetupSse?.Invoke();

                    if (context.KillMutex?.Length > 0)
                    {
                        // to kill the mutexes we need to orphanize the process
                        proc = ProcessUtil.RunOrphanProcess(emuExe);
                    }
                    else
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.FileName = emuExe;
                        proc = Process.Start(startInfo);
                    }

                    player.SteamEmu = true;
                }
                else
                {
                    if (((context.KillMutex?.Length > 0 || (gen.HookInit || gen.RenameNotKillMutex || gen.SetWindowHook)) && !gen.CMDLaunch) || (gen.CMDLaunch && i==0))
                    {

                        string mu = "";
                        if(gen.RenameNotKillMutex && context.KillMutex?.Length > 0)
                        {
                            for (int m = 0; m < gen.KillMutex.Length; m++)
                            {
                                mu += gen.KillMutex[m];

                                if (m != gen.KillMutex.Length - 1)
                                {
                                    mu += ";";
                                }
                            }
                        }

                        proc = Process.GetProcessById(StartGameUtil.StartGame(
                            GetRelativePath(exePath, nucleusRootFolder), startArgs,
                            gen.HookInit, gen.HookInitDelay, gen.RenameNotKillMutex, mu, gen.SetWindowHook, GetRelativePath(linkFolder, nucleusRootFolder)));
                    }
                    else
                    {
                        if (gen.CMDLaunch && i == 1)
                        {
                            string[] cmdOps = gen.CMDOptions;
                            Process cmd = new Process();
                            cmd.StartInfo.FileName = "cmd.exe";
                            cmd.StartInfo.RedirectStandardInput = true;
                            cmd.StartInfo.RedirectStandardOutput = true;
                            //cmd.StartInfo.CreateNoWindow = true;
                            cmd.StartInfo.UseShellExecute = false;
                            cmd.Start();

                            cmd.StandardInput.WriteLine(cmdOps[i] + " \"" + exePath + "\" " + startArgs);
                            cmd.StandardInput.Flush();
                            cmd.StandardInput.Close();
                            //cmd.WaitForExit();
                            //Console.WriteLine(cmd.StandardOutput.ReadToEnd());
                            proc = null;

                        }
                        else
                        {
                            ProcessStartInfo startInfo = new ProcessStartInfo();
                            startInfo.UseShellExecute = true;                             
                            startInfo.WorkingDirectory = Path.GetDirectoryName(exePath);
                            startInfo.FileName = exePath;
                            startInfo.Arguments = startArgs;
                            proc = Process.Start(startInfo);
                        }

                    }

                    if (proc == null || gen.CMDLaunch && i == 1 || gen.GameName == "Halo Custom Edition")
                    {
                        //bool foundUnique = false;
                        for (int times = 0; times < 200; times++)
                        {
                            Thread.Sleep(50);

                            
                            string proceName = "";
                            if (gen.GameName == "Halo Custom Edition")
                            {
                                Thread.Sleep(10000);
                                proceName = "haloce";
                            }
                            else
                            {
                                proceName = Path.GetFileNameWithoutExtension(gen.ExecutableName).ToLower();
                            }
                            string launcherName = Path.GetFileNameWithoutExtension(gen.LauncherExe).ToLower();

                            Process[] procs = Process.GetProcesses();
                            for (int j = 0; j < procs.Length; j++)
                            {
                                Process p = procs[j];

                                string lowerP = p.ProcessName.ToLower();

                                if (lowerP == proceName || lowerP == launcherName)
                                {
                                    if (!attachedIds.Contains(p.Id))
                                    {
                                        attached.Add(p);
                                        attachedIds.Add(p.Id);
                                        proc = p;
                                        //InjectDLLs(proc);
                                        //foundUnique = true;
                                        break;
                                    }
                                }
                            }

                            if (proc != null)
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        attached.Add(proc);
                        attachedIds.Add(proc.Id);
                        //InjectDLLs(proc);
                    }
                }

                if(i > 0 && gen.ResetWindows && prevProcessData != null)
                {
                    MessageBox.Show("Going to attempt to reposition and resize instance " + (i - 1));
                    prevProcessData.HWnd.Location = new Point(prevWindowX, prevWindowY);
                    prevProcessData.HWnd.Size = new Size(prevWindowWidth, prevWindowHeight);
                }

                ProcessData data = new ProcessData(proc);
                prevProcessData = data;

                playerBoundsWidth = playerBounds.Width;
                playerBoundsHeight = playerBounds.Height;

                if (context.Hook.WindowX > 0 && context.Hook.WindowY > 0)
                {
                    data.Position = new Point(context.Hook.WindowX, context.Hook.WindowY);
                    prevWindowX = context.Hook.WindowX;
                    prevWindowY = context.Hook.WindowY;
                }
                else
                {
                    data.Position = new Point(playerBounds.X, playerBounds.Y);
                    prevWindowX = playerBounds.X;
                    prevWindowY = playerBounds.Y;
                }

                if (context.Hook.ResWidth > 0 && context.Hook.ResHeight > 0)
                {
                    data.Size = new Size(context.Hook.ResWidth, context.Hook.ResHeight);
                    prevWindowWidth = context.Hook.ResWidth;
                    prevWindowHeight = context.Hook.ResHeight;
                }
                else
                {
                    data.Size = new Size(playerBounds.Width, playerBounds.Height);
                    prevWindowWidth = playerBounds.Width;
                    prevWindowHeight = playerBounds.Height;
                }

                data.KilledMutexes = context.KillMutex?.Length == 0;
                player.ProcessData = data;

                first = false;
                //InjectDLLs(proc);

                if(gen.ProcessorPriorityClass?.Length > 0)
                {
                    switch (gen.ProcessorPriorityClass)
                    {
                        case "AboveNormal":
                            proc.PriorityClass = ProcessPriorityClass.AboveNormal;
                            break;
                        case "High":
                            proc.PriorityClass = ProcessPriorityClass.High;
                            break;
                        case "RealTime":
                            proc.PriorityClass = ProcessPriorityClass.RealTime;
                            break;
                        default:
                            proc.PriorityClass = ProcessPriorityClass.Normal;
                            break;
                    }
                }

                if(gen.IdealProcessor > 0)
                {
                    ProcessThreadCollection threads = proc.Threads;
                    for(int t = 0; t < threads.Count; t++)
                    {
                        threads[t].IdealProcessor = (gen.IdealProcessor + 1);
                    }
                }

                if(gen.UseProcessor?.Length > 0)
                {
                    ulong affinityMask = gen.UseProcessor.Split(',')
                                .Select(int.Parse)
                                .Aggregate(0UL, (mask, id) => mask | (1UL << id));
                    proc.ProcessorAffinity = (IntPtr)affinityMask;
                }

                if (gen.IdInWindowTitle)
                {
                    if ((int)proc.MainWindowHandle == 0)
                    {
                        for (int times = 0; times < 200; times++)
                        {
                            Thread.Sleep(50);
                            if ((int)proc.MainWindowHandle > 0)
                            {
                                SetWindowText(proc.MainWindowHandle, proc.MainWindowTitle + "(" + proc.Id + ")");
                                break;
                            }
                            if (times == 199 && (int)proc.MainWindowHandle == 0)
                            {
                                MessageBox.Show(string.Format("IdInWindowTitle: Could not find main window handle for {0} (pid:{1})", proc.ProcessName, proc.Id), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }


                if (gen.PromptBetweenInstances && i < (players.Count - 1))
                {
                    MessageBox.Show("Press OK when ready to launch instance " + (i + 1) + ".", "Waiting", MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                }
                else if(gen.PromptBetweenInstances && i == players.Count - 1 && gen.HookFocus)
                {
                    MessageBox.Show("Press OK when ready to install focus hooks.", "Waiting", MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                }
                else
                {
                    Thread.Sleep(TimeSpan.FromSeconds(gen.PauseBetweenStarts));
                }

                if (i == (players.Count - 1)) // all instances accounted for
                {
                    if (gen.FakeFocus)
                    {
                        fakeFocus = new System.Threading.Thread(SendFocusMsgs);
                        fakeFocus.Start();
                    }

                    if (gen.ForceWindowTitle)
                    {
                        foreach (Process aproc in attached)
                        {
                            if ((int)aproc.MainWindowHandle == 0)
                            {
                                for (int times = 0; times < 200; times++)
                                {
                                    Thread.Sleep(50);
                                    if ((int)aproc.MainWindowHandle > 0)
                                    {
                                        SetWindowText(aproc.MainWindowHandle, gen.Hook.ForceFocusWindowName);
                                        break;
                                    }
                                    if (times == 199 && (int)aproc.MainWindowHandle == 0)
                                    {
                                        MessageBox.Show(string.Format("ChangeWindowTitle: Could not find main window handle for {0} (pid:{1})", aproc.ProcessName, aproc.Id), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                }
                            }
                        }
                    }


                    if (gen.HookFocus || gen.SetWindowHook)
                    {
                        if (!data.Setted)
                        {
                            for (int times = 0; times < 200; times++)
                            {
                                Thread.Sleep(50);
                                if (data.Setted)
                                {
                                    break;
                                }
                            }
                        }

                        List<int> piresult = new List<int>();
                        int pi = 0;
                        if (gen.HookFocusInstances?.Length > 0)
                        { 
                            piresult = gen.HookFocusInstances.Split(',').Select(Int32.Parse).ToList();
                        }
                        foreach (Process aproc in attached)
                        {    
                            if (gen.HookFocusInstances?.Length > 0)
                            {
                                if(piresult.Contains(pi))
                                {
                                    InjectDLLs(aproc);
                                }

                                pi++;
                            }
                            else
                            {
                                InjectDLLs(aproc);
                            }
                            //using (StreamWriter writer = new StreamWriter("error-log.txt", true))
                            //{
                            //    writer.WriteLine("InjectDLLs: Entered for loop, going to inject process: {0} (pid:{1}), main window title: {2}", aproc.ProcessName, aproc.Id, aproc.MainWindowTitle);
                            //}
                            //aproc.Refresh();
                            //if (aproc == null || aproc.HasExited || aproc.MainWindowTitle.ToLower() == gen.LauncherTitle.ToLower() || aproc.MainModule.FileName == gen.LauncherExe)
                            //    continue;

                        }
                    }
                }               
            }

            return string.Empty;
        }

        private void InjectDLLs(Process proc)
        {
            if ((int)proc.MainWindowHandle == 0)
            {
                for (int times = 0; times < 200; times++)
                {
                    Thread.Sleep(50);
                    if ((int)proc.MainWindowHandle > 0)
                    {
                        break;
                    }
                    if (times == 199 && (int)proc.MainWindowHandle == 0)
                    {
                        using (StreamWriter writer = new StreamWriter("error-log.txt", true))
                        {
                            writer.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]" + "InjectDLLs: Could not find main window handle for {0} (pid:{1})", proc.ProcessName, proc.Id);
                        }
                    }
                }
            }


            bool is64 = EasyHook.RemoteHooking.IsX64Process(proc.Id);                 
            string currDir = Directory.GetCurrentDirectory();

            //using (StreamWriter writer = new StreamWriter("important.txt", true))
            //{
            //    writer.WriteLine("aproc id: {0}, aproc procname: {1}, title: {2}, handle: {3}, handleint: {4}, bytefour: {5}, byteeight: {6}, datatosend[8]: {7}, datatosend[9]: {8}, intptr: {9}", proc.Id, proc.ProcessName, proc.MainWindowTitle, proc.MainWindowHandle, (int)proc.MainWindowHandle, BitConverter.ToUInt32(dataToSend, 0), BitConverter.ToUInt64(dataToSend, 0), dataToSend[8], dataToSend[9], intPtr);
            //}




            try
            {
                if (is64)
                {
                    string injectorPath = Path.Combine(currDir, "Nucleus.Inject64.exe");
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = injectorPath;
                    object[] args = new object[]
                    {
                        1, proc.Id, 0, 0, null, Path.Combine(currDir, "Nucleus.Hook64.dll"), proc.MainWindowHandle, gen.HookFocus, gen.HideCursor
                    };
                    var sbArgs = new StringBuilder();
                    foreach (object arg in args)
                    {
                        sbArgs.Append(" \"");
                        sbArgs.Append(arg);
                        sbArgs.Append("\"");
                    }

                    string arguments = sbArgs.ToString();
                    startInfo.Arguments = arguments;
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo.CreateNoWindow = true;
                    startInfo.UseShellExecute = false;
                    startInfo.RedirectStandardOutput = true;
                    Process injectProc = Process.Start(startInfo);
                    injectProc.WaitForExit();
                }
                else
                {
                    int size = 9;
                    IntPtr intPtr = Marshal.AllocHGlobal(size);
                    byte[] dataToSend = new byte[size];
                    dataToSend[0] = (byte)((int)proc.MainWindowHandle >> 24);
                    dataToSend[1] = (byte)((int)proc.MainWindowHandle >> 16);
                    dataToSend[2] = (byte)((int)proc.MainWindowHandle >> 8);
                    dataToSend[3] = (byte)((int)proc.MainWindowHandle);

                    dataToSend[7] = gen.HideCursor == true ? (byte)1 : (byte)0;
                    dataToSend[8] = gen.HookFocus == true ? (byte)1 : (byte)0;
                    Marshal.Copy(dataToSend, 0, intPtr, size);
                    NativeAPI.RhInjectLibrary(proc.Id, 0, 0, Path.Combine(currDir, "Nucleus.Hook32.dll"), null, intPtr, size);
                }
            }
            catch (Exception ex)
            {
                using (StreamWriter writer = new StreamWriter("error-log.txt", true))
                {
                    writer.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]" + "is64: {0}, ex msg: {1}, ex str: {2}", is64, ex.Message, ex.ToString());
                }
            }
        }

        private void SendFocusMsgs()
        {
            while(true)
            {
                //should make user defined?
                Thread.Sleep(1000);

                foreach (Process proc in attached)
                {
                    User32Interop.SendMessage(proc.MainWindowHandle, (int)FocusMessages.WM_ACTIVATEAPP, (IntPtr)1, IntPtr.Zero);
                    User32Interop.SendMessage(proc.MainWindowHandle, (int)FocusMessages.WM_ACTIVATE, (IntPtr)0x00000002, IntPtr.Zero);
                    User32Interop.SendMessage(proc.MainWindowHandle, (int)FocusMessages.WM_NCACTIVATE, (IntPtr)0x00000001, IntPtr.Zero);
                    User32Interop.SendMessage(proc.MainWindowHandle, (int)FocusMessages.WM_SETFOCUS, IntPtr.Zero, IntPtr.Zero);
                }
                
            }
        }

        struct TickThread
        {
            public double Interval;
            public Action Function;
        }

        public void StartPlayTick(double interval, Action function)
        {
            Thread t = new Thread(PlayTickThread);

            TickThread tick = new TickThread();
            tick.Interval = interval;
            tick.Function = function;
            t.Start(tick);
        }

        private void PlayTickThread(object state)
        {
            TickThread t = (TickThread)state;

            for (;;)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(t.Interval));
                t.Function();

                if (hasEnded)
                {
                    break;
                }
            }
        }

        public void CenterCursor()
        {
            List<PlayerInfo> players = profile.PlayerData;
            if (players == null)
            {
                return;
            }

            for (int i = 0; i < players.Count; i++)
            {
                PlayerInfo p = players[i];

                if (p.IsKeyboardPlayer)
                {
                    ProcessData data = p.ProcessData;
                    if (data == null)
                    {
                        continue;
                    }

                    Rectangle r = p.MonitorBounds;
                    //Cursor.Clip = r;
                    if (data.HWnd != null)
                    {
                        User32Interop.SetForegroundWindow(data.HWnd.NativePtr);
                    }
                }
            }
        }

        public void Update(double delayMS)
        {
            if (profile == null)
            {
                return;
            }

            exited = 0;
            List<PlayerInfo> players = profile.PlayerData;
            timer += delayMS;

            bool updatedHwnd = false;
            if (timer > HWndInterval)
            {
                updatedHwnd = true;
                timer = 0;
            }

            for (int i = 0; i < players.Count; i++)
            {
                PlayerInfo p = players[i];
                ProcessData data = p.ProcessData;
                if (data == null)
                {
                    continue;
                }

                if (data.Finished)
                {
                    if (data.Process.HasExited)
                    {
                        exited++;
                    }
                    continue;
                }


                if (p.SteamEmu)
                {
                    List<int> children = ProcessUtil.GetChildrenProcesses(data.Process);

                    // catch the game process, that was spawned from Smart Steam Emu
                    if (children.Count > 0)
                    {
                        for (int j = 0; j < children.Count; j++)
                        {
                            int id = children[j];
                            Process child = Process.GetProcessById(id);
                            try
                            {
                                if (child.ProcessName.Contains("conhost"))
                                {
                                    continue;
                                }
                            }
                            catch
                            {
                                continue;
                            }

                            data.AssignProcess(child);
                            p.SteamEmu = child.ProcessName.Contains("SmartSteamLoader") || child.ProcessName.Contains("cmd");
                        }
                    }
                }
                else
                {
                    if (updatedHwnd)
                    {
                        if (data.Setted)
                        {
                            if (data.Process.HasExited)
                            {
                                exited++;
                                continue;
                            }

                            data.HWnd.TopMost = true;

                            if (data.Status == 2)
                            {

                                //using (StreamWriter writer = new StreamWriter("proc-test.txt", true))
                                //{
                                //    writer.WriteLine("state 2 data Hwnd: {0}, data process Id {1}, data processname: {2}, data process mainmodule {3}, data process mainwindowhandle: {4}", data.HWnd, data.Process.Id, data.Process.ProcessName, data.Process.MainModule, data.Process.MainWindowHandle);
                                //}

                                uint lStyle = User32Interop.GetWindowLong(data.HWnd.NativePtr, User32_WS.GWL_STYLE);
                                lStyle = lStyle & ~User32_WS.WS_CAPTION;
                                lStyle = lStyle & ~User32_WS.WS_THICKFRAME;
                                lStyle = lStyle & ~User32_WS.WS_MINIMIZE;
                                lStyle = lStyle & ~User32_WS.WS_MAXIMIZE;
                                lStyle = lStyle & ~User32_WS.WS_SYSMENU;
                                User32Interop.SetWindowLong(data.HWnd.NativePtr, User32_WS.GWL_STYLE, lStyle);

                                lStyle = User32Interop.GetWindowLong(data.HWnd.NativePtr, User32_WS.GWL_EXSTYLE);
                                lStyle = lStyle & ~User32_WS.WS_EX_DLGMODALFRAME;
                                lStyle = lStyle & ~User32_WS.WS_EX_CLIENTEDGE;
                                lStyle = lStyle & ~User32_WS.WS_EX_STATICEDGE;
                                User32Interop.SetWindowLong(data.HWnd.NativePtr, User32_WS.GWL_EXSTYLE, lStyle);
                                User32Interop.SetWindowPos(data.HWnd.NativePtr, IntPtr.Zero, 0, 0, 0, 0, (uint)(PositioningFlags.SWP_FRAMECHANGED | PositioningFlags.SWP_NOMOVE | PositioningFlags.SWP_NOSIZE | PositioningFlags.SWP_NOZORDER | PositioningFlags.SWP_NOOWNERZORDER));
                                //User32Interop.SetForegroundWindow(data.HWnd.NativePtr);

                                data.Finished = true;



                                Debug.WriteLine("State 2");

                                if (i == players.Count - 1 && gen.LockMouse)
                                {
                                    //last screen setuped
                                    _cursorModule.SetActiveWindow();
                                }
                            }
                            else if (data.Status == 1)
                            {
                                if(!dllRepos)
                                {
                                    data.HWnd.Location = data.Position;
                                }
                                
                                data.Status++;
                                Debug.WriteLine("State 1");

                                if (gen.LockMouse)
                                {
                                    if (p.IsKeyboardPlayer)
                                    {
                                        _cursorModule.Setup(data.Process, p.MonitorBounds);
                                    }
                                    else
                                    {
                                        _cursorModule.AddOtherGameHandle(data.Process.MainWindowHandle);
                                    }
                                }
                            }
                            else if (data.Status == 0)
                            {
                                if (!dllResize)
                                {
                                    if(gen.KeepAspectRatio)
                                    {
                                        RECT Rect = new RECT();
                                        if (GetWindowRect(data.Process.MainWindowHandle, ref Rect))
                                        {
                                            origWidth = Rect.Right - Rect.Left;
                                            origHeight = Rect.Bottom - Rect.Top;
                                            origRatio = origWidth / origHeight;

                                            int newWidth = 0;
                                            int newHeight = 0;

                                            if (origWidth > playerBoundsWidth)
                                            {
                                                newWidth = playerBoundsWidth;
                                                newHeight = Convert.ToInt32(playerBoundsWidth / origRatio);
                                            }
                                            if(newHeight > playerBoundsHeight || newHeight == 0)
                                            {
                                                newHeight = playerBoundsHeight;
                                            }
                                            if(newWidth > playerBoundsWidth || newWidth == 0)
                                            {
                                                newWidth = Convert.ToInt32(playerBoundsHeight * origRatio);
                                            }

                                            data.Size = new Size(newWidth, newHeight);
                                        }
                                    }
                                    else
                                    {
                                        data.HWnd.Size = data.Size;
                                    }
                                }
                                

                                data.Status++;
                                Debug.WriteLine("State 0");
                            }
                        }
                        else
                        {
                            data.Process.Refresh();

                            if (data.Process.HasExited)
                            {
                                if (p.GotLauncher)
                                {
                                    if (p.GotGame)
                                    {
                                        exited++;
                                    }
                                    else
                                    {
                                        List<int> children = ProcessUtil.GetChildrenProcesses(data.Process);
                                        if (children.Count > 0)
                                        {
                                            for (int j = 0; j < children.Count; j++)
                                            {
                                                int id = children[j];
                                                Process pro = Process.GetProcessById(id);

                                                if (!attached.Contains(pro))
                                                {
                                                    attached.Add(pro);
                                                    attachedIds.Add(pro.Id);
                                                    data.HWnd = null;
                                                    p.GotGame = true;
                                                    data.AssignProcess(pro);
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // Steam showing a launcher, need to find our game process
                                    string launcher = gen.LauncherExe;
                                    if (!string.IsNullOrEmpty(launcher))
                                    {
                                        if (launcher.ToLower().EndsWith(".exe"))
                                        {
                                            launcher = launcher.Remove(launcher.Length - 4, 4);
                                        }

                                        Process[] procs = Process.GetProcessesByName(launcher);
                                        for (int j = 0; j < procs.Length; j++)
                                        {
                                            Process pro = procs[j];

                                            if (!attachedIds.Contains(pro.Id))
                                            {
                                                attached.Add(pro);
                                                attachedIds.Add(pro.Id);
                                                data.AssignProcess(pro);
                                                p.GotLauncher = true;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (data.HWNDRetry || data.HWnd == null || data.HWnd.NativePtr != data.Process.MainWindowHandle)
                                {
                                    data.HWnd = new HwndObject(data.Process.MainWindowHandle);
                                    Point pos = data.HWnd.Location;

                                    if (String.IsNullOrEmpty(data.HWnd.Title) ||
                                        pos.X == -32000 ||
                                        data.HWnd.Title.ToLower() == gen.LauncherTitle.ToLower())
                                    {
                                        data.HWNDRetry = true;
                                    }
                                    else if (!string.IsNullOrEmpty(gen.Hook.ForceFocusWindowName) &&
                                        // TODO: this Levenshtein distance is being used to help us around Call of Duty Black Ops, as it uses a ® icon in the title bar
                                        //       there must be a better way
                                        StringUtil.ComputeLevenshteinDistance(data.HWnd.Title, gen.Hook.ForceFocusWindowName) > 2 && !gen.HasDynamicWindowTitle) 
                                    {
                                        data.HWNDRetry = true;                                        
                                    }
                                    else
                                    {
                                        Size s = data.Size;
                                        data.Setted = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (exited == players.Count)
            {

                if (!hasEnded)
                {
                    End();
                }
            }
        }

        public void Log(StreamWriter writer)
        {
        }
    }
}