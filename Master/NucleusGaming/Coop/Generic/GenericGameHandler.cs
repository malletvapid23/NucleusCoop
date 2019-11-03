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
using Microsoft.Win32;
using System.Management;

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

        private bool gameIs64 = false;

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

        private int prevProcId = 0;

        private int plyrIndex = 0;

        private long random_steam_id = 76561199023125438;

        private string nucleusFolderPath;

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

        public string exePath;
        private string instanceExeFolder;

        private readonly IniFile ini = new Gaming.IniFile(Path.Combine(Directory.GetCurrentDirectory(), "Settings.ini"));
        bool isDebug;

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
                Process[] procs = Process.GetProcesses();
                //if(gen.GameName == "Halo Custom Edition")
                //{
                //    procs = Process.GetProcessesByName("haloce");
                //}
                //else
                //{
                //    procs = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(gen.ExecutableName.ToLower()));
                //}
                
                //if (procs.Length > 0)
                //{
                    foreach(Process proc in procs)
                    {
                        try
                        {
                            if (proc.ProcessName.ToLower() == Path.GetFileNameWithoutExtension(gen.ExecutableName.ToLower()) || (proc.Id != 0 && attachedIds.Contains(proc.Id)) || (gen.Hook.ForceFocusWindowName != "" && proc.MainWindowTitle == gen.Hook.ForceFocusWindowName))
                            {
                                Log(string.Format("Killing process {0} (pid {1})", proc.ProcessName, proc.Id));
                                proc.Kill();
                            }
                        }
                        catch
                        {

                        }
                    }
                //}
            }
            catch { }
        }

        //public static void RecursiveDelete(DirectoryInfo baseDir)
        //{
        //    if (!baseDir.Exists)
        //        return;

        //    foreach (var dir in baseDir.EnumerateDirectories())
        //    {
        //        RecursiveDelete(dir);
        //    }
        //    var files = baseDir.GetFiles();
        //    foreach (var file in files)
        //    {
        //        file.IsReadOnly = false;
        //        file.Delete();
        //    }
        //    baseDir.Delete();
        //}

        public void End()
        {
            Log("----------------- SHUTTING DOWN -----------------");
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

            int tempIndex = 0;

#if RELEASE
            if (gen.KeepSymLinkOnExit == false)
            {
                for (int i = 0; i < profile.PlayerData.Count; i++)
                {
                    tempIndex = i;
                    string linkFolder = Path.Combine(backupDir, "Instance" + i);
                    Log(string.Format("Deleting folder {0} and all of its contents.", linkFolder));
                    int retries = 0;
                    while (Directory.Exists(linkFolder))
                    {
                        
                        try
                        {
                            Directory.Delete(linkFolder, true);
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            Log("ERROR - UnauthorizedAccessException - " + ex.Message);
                            int pFrom = ex.Message.IndexOf("\'") + 1;
                            int pTo = ex.Message.LastIndexOf("\'");
                            string fileName = ex.Message.Substring(pFrom, pTo - pFrom);

                            //string linkFolder = Path.Combine(backupDir, "Instance" + tempIndex);
                            if (!fileName.Contains("\\"))
                            {
                                string[] files = Directory.GetFiles(linkFolder, fileName, SearchOption.AllDirectories);
                                foreach (string file in files)
                                {
                                    FileInfo fi = new FileInfo(file);
                                    if (fi.IsReadOnly/*fi.Attributes == FileAttributes.ReadOnly*/)
                                    {
                                        Log(string.Format("File was read-only. Setting '{0}' to normal file attributes and deleting", fileName));
                                        File.SetAttributes(file, FileAttributes.Normal);
                                        File.Delete(file);
                                    }
                                    else
                                    {
                                        Log("ERROR - Unknown reason why file cannot be deleted. Skipping this file for now");
                                    }
                                }
                            }

                            if (retries < 10)
                            {
                                retries++;
                                Log("Retrying to delete folder and all its contents");
                            }
                            else
                            {
                                Log("Abandoning trying to delete folder and all its contents");
                                //throw;
                            }

                        }
                        catch (Exception ex)
                        {
                            Log("ERROR - " + ex.Message);

                            if (retries < 10)
                            {
                                retries++;
                                Log("Retrying to delete folder and all its contents");
                            }
                            else
                            {
                                Log("Abandoning trying to delete folder and all its contents");
                                //throw;
                            }
                        }
                        if(Directory.Exists(linkFolder))
                        {
                            Thread.Sleep(1000);
                        }
                        else
                        {
                            Log("Folder and all its contents have successfully been deleted");
                        }
                    }
                }
                Log("File deletion complete");
            }
#endif

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
            isDebug = Boolean.Parse(ini.IniReadValue("Misc", "DebugLog"));

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
            //bool gameIs64 = false;
            string garch = "x86";
            if (Is64Bit(userGame.ExePath) == true)
            {
                gameIs64 = true;
                garch = "x64";
            }

            if (isDebug)
            {
                Log("--------------------- START ---------------------");
                Log(string.Format("Game: {0}, Arch: {1}, Executable: {2}, SteamID: {3}, Script: {4}, Data: {5}, DPIHandling: {6}, DPI Scale: {7}", gen.GameName, garch, gen.ExecutableName, gen.SteamID, gen.JsFileName, gen.GUID, gen.DPIHandling, DPIManager.Scale));

                if (string.IsNullOrEmpty(gen.StartArguments))
                {
                    Log("Game has no starting arguments");
                }
                else
                {
                    Log("Starting arguments: " + gen.StartArguments);
                }
                Log(string.Format("Utils - UseGoldberg: {0}, NeedsSteamEmulation: {1}, UseX360ce: {2}", gen.UseGoldberg, gen.NeedsSteamEmulation, gen.UseX360ce));
                Log(string.Format("Hooks - HookInit: {0}, RenameNotKillMutex: {1}, SetWindowHook: {2}, HookFocus: {3}, HideCursor: {4}, PreventWindowDeactivation: {5}", gen.HookInit, gen.RenameNotKillMutex, gen.SetWindowHook, gen.HookFocus, gen.HideCursor, gen.PreventWindowDeactivation));

                if (gen.KillMutex?.Length > 0)
                {
                    string mutexList = string.Join(",", gen.KillMutex);
                    Log(string.Format("Mutexes - Handle(s): ({0}), KillMutexDelay: {1}, KillMutexType: {2}, RenameNotKillMutex: {3}, PartialMutexSearch: {4}", mutexList, gen.KillMutexDelay, gen.KillMutexType, gen.RenameNotKillMutex, gen.PartialMutexSearch));
                }

                Log("NucleusCoop mod version: 0.9.8.0 ALPHA");
                string pcSpecs = "PC Info - ";
                var name = (from x in new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem").Get().Cast<ManagementObject>()
                            select x.GetPropertyValue("Caption")).FirstOrDefault();
                //Log(name != null ? "OS:" + name.ToString() : "Windows OS: Unknown");

                pcSpecs += name != null ? "OS: " + name.ToString() + ", " : "Windows OS: Unknown, ";

                const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

                using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subkey))
                {
                    if (ndpKey != null && ndpKey.GetValue("Release") != null)
                    {
                        pcSpecs += $".NET Framework Version: {CheckFor45PlusVersion((int)ndpKey.GetValue("Release"))}";
                        //Log($".NET Framework Version: {CheckFor45PlusVersion((int)ndpKey.GetValue("Release"))}");
                    }
                    else
                    {
                        pcSpecs += $".NET Framework Version: {Environment.Version}";
                        //Log($".NET Framework Version: {Environment.Version}");
                    }
                }

                Log(pcSpecs);
            }

            ForceFinish();

            List<PlayerInfo> players = profile.PlayerData;

            for (int i = 0; i < players.Count; i++)
            {
                players[i].PlayerID = i;
            }

            UserScreen[] all = ScreensUtil.AllScreens();

            string nucleusRootFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            nucleusFolderPath = nucleusRootFolder;

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

            Log(string.Format("Number of players: {0}",players.Count));

            for (int i = 0; i < players.Count; i++)
            {
                Log("********** Setting up player " + (i + 1) + " **********");
                PlayerInfo player = players[i];
                ProcessData procData = player.ProcessData;
                bool hasSetted = procData != null && procData.Setted;

                //if (ini.IniReadValue("Misc", "VibrateOpen") == "True")
                //{
                //    SlimDX.XInput.Controller gamePad = new SlimDX.XInput.Controller((SlimDX.XInput.UserIndex)i);
                //    SlimDX.XInput.Vibration vib = new SlimDX.XInput.Vibration();

                //    vib.LeftMotorSpeed = 32000;
                //    vib.RightMotorSpeed = 16000;
                //    gamePad.SetVibration(vib);
                //}

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
                                //Process prevProcess;
                                //if (pdata.Process == null)
                                //{
                                //    prevProcess = Process.GetProcessById(prevProcId);
                                //}
                                //else
                                //{
                                //    prevProcess = pdata.Process;
                                //}

                                //using (StreamWriter writer = new StreamWriter("important.txt", true))
                                //{
                                //    writer.WriteLine("Process name: " + prevProcess.ProcessName + " pid: " + prevProcess.Id);
                                //}

                                //if (gen.KillMutexType == null)
                                //{
                                //    gen.KillMutexType = "Mutant";
                                //}

                                if(gen.KillMutexDelay > 0)
                                {
                                    Thread.Sleep((gen.KillMutexDelay * 1000));
                                }

                                if (StartGameUtil.MutexExists(pdata.Process/*prevProcess*/, gen.KillMutexType, gen.PartialMutexSearch, gen.KillMutex))
                                {
                                    // mutexes still exist, must kill
                                    StartGameUtil.KillMutex(pdata.Process/*prevProcess*/, gen.KillMutexType, gen.PartialMutexSearch, gen.KillMutex);
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

                if(i > 0 && (gen.HookFocus || gen.SetWindowHook || gen.HideCursor || gen.PreventWindowDeactivation))
                {
                    Log("Injecting hook DLL for previous instance");
                    PlayerInfo before = players[i - 1];
                    Thread.Sleep(1000);
                    ProcessData pdata = before.ProcessData;
                    InjectDLLs(pdata.Process);
                }

                Rectangle playerBounds = player.MonitorBounds;
                UserScreen owner = player.Owner;

                int width = playerBounds.Width;
                int height = playerBounds.Height;
                bool isFullscreen = owner.Type == UserScreenType.FullScreen;

                
                string linkFolder;
                string linkBinFolder;
                string origRootFolder = "";

                if (gen.SymlinkGame || gen.HardcopyGame || gen.HardlinkGame)
                {

                    List<string> dirExclusions = new List<string>();
                    List<string> fileExclusions = new List<string>();
                    List<string> fileCopies = new List<string>();

                    // symlink the game folder (and not the bin folder, if we have one)
                    linkFolder = Path.Combine(tempDir, "Instance" + i);

                    Log("Commencing file operations");
                    for (int f = 0; f < players.Count; f++)
                    {
                        string insFolder = Path.Combine(tempDir, "Instance" + f);
                        Log(string.Format("Creating instance folder {0}", insFolder.Substring(insFolder.IndexOf("content\\"))));
                        Directory.CreateDirectory(insFolder);
                    }

                    linkBinFolder = linkFolder;
                    if (!string.IsNullOrEmpty(gen.BinariesFolder))
                    {
                        linkBinFolder = Path.Combine(linkFolder, gen.BinariesFolder);
                        //dirExclusions.Add(gen.BinariesFolder);
                    }
                    exePath = Path.Combine(linkBinFolder, this.userGame.Game.ExecutableName);

                    Log("Starting symlink and copies");
                    if (gen.SymlinkFiles != null)
                    {
                        Log("Symlinking files in Game.SymlinkFiles");
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
                        Log("Copying files in Game.CopyFiles");
                        string[] filesToCopy = gen.CopyFiles;
                        for (int c = 0; c < filesToCopy.Length; c++)
                        {
                            string s = filesToCopy[c].ToLower();
                            File.Copy(Path.Combine(rootFolder, s), Path.Combine(linkFolder, s), true);
                        }
                    }

                    origRootFolder = rootFolder;
                    instanceExeFolder = linkBinFolder;

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
                        Log("Game executable (" + gen.ExecutableName + ") will be copied and not symlinked.");// Will be placed in " + Path.GetDirectoryName(GetRelativePath(exePath, nucleusRootFolder)));
                        fileCopies.Add(gen.ExecutableName.ToLower());
                    }

                    // additional ignored files by the generic info
                    if (gen.FileSymlinkExclusions != null)
                    {
                        Log("Files in Game.FileSymlinkExclusions will not be symlinked");
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
                        Log("Files in Game.FileSymlinkCopyInstead will be copied instead of symlinked");
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
                        Log("Directories in Game.DirSymlinkExclusions will be ignored");
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

                    bool skipped = false;
                    if (!gen.KeepSymLinkOnExit || (gen.KeepSymLinkOnExit && Directory.Exists(linkFolder) && !Directory.EnumerateFileSystemEntries(linkFolder).Any()))
                    {
                        if (gen.HardcopyGame)
                        {
                            Log(string.Format("Copying game folder {0} to {1} ", rootFolder, linkFolder));
                            // copy the directory
                            int exitCode;
                            FileUtil.CopyDirectory(rootFolder, new DirectoryInfo(rootFolder), linkFolder, out exitCode, dirExclusions.ToArray(), fileExclusionsArr, true);
                        }
                        else if (gen.HardlinkGame)
                        {
                            if(i==0)
                            {
                                Log(string.Format("Hardlinking game files {0} to {1}, for each instance", rootFolder, linkFolder));
                                int exitCode;
                                //CmdUtil.LinkDirectory(rootFolder, new DirectoryInfo(rootFolder), linkFolder, out exitCode, dirExclusions.ToArray(), fileExclusionsArr, fileCopiesArr, false, true);
                                //Nucleus.Gaming.Platform.Windows.IO.LinkDirectory(rootFolder, new DirectoryInfo(rootFolder), linkFolder, out exitCode, dirExclusions.ToArray(), fileExclusionsArr, fileCopiesArr, true);
                                while (!StartGameUtil.SymlinkGame(rootFolder, linkFolder, out exitCode, dirExclusions.ToArray(), fileExclusionsArr, fileCopiesArr, true, gen.SymlinkFolders, players.Count))
                                {
                                    Thread.Sleep(25);
                                }
                            }
                        }
                        else
                        {
                            if(i==0)
                            {
                                Log(string.Format("Symlinking game folder and files at {0} to {1}, for each instance", rootFolder, tempDir));
                                int exitCode;
                                //CmdUtil.LinkDirectory(rootFolder, new DirectoryInfo(rootFolder), linkFolder, out exitCode, dirExclusions.ToArray(), fileExclusionsArr, fileCopiesArr, true, true);
                                //Nucleus.Gaming.Platform.Windows.IO.WinDirectoryUtil.LinkDirectory(rootFolder, new DirectoryInfo(rootFolder), linkFolder, out exitCode, dirExclusions.ToArray(), fileExclusionsArr, fileCopiesArr, true);
                                while (!StartGameUtil.SymlinkGame(rootFolder, linkFolder, out exitCode, dirExclusions.ToArray(), fileExclusionsArr, fileCopiesArr, false, gen.SymlinkFolders, players.Count))
                                {
                                    Thread.Sleep(25);
                                }

                                if (!gen.SymlinkExe)
                                {
                                    //File.Copy(userGame.ExePath, exePath, true);
                                }
                            }
                        }
                    }
                    else
                    {
                        skipped = true;
                        Log("Skipping linking or copying files as it is not needed");
                    }
                    if(!skipped)
                    {
                        Log("File operations complete");
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
                    if(File.Exists(Path.Combine(instanceExeFolder, newExe)))
                    {
                        File.Delete(Path.Combine(instanceExeFolder, newExe));
                    }
                    File.Move(Path.Combine(instanceExeFolder, this.userGame.Game.ExecutableName), Path.Combine(instanceExeFolder, newExe));
                    exePath = Path.Combine(instanceExeFolder, newExe);
                    Log("Changed game executable from " + gen.ExecutableName + " to " + newExe);
                }

                GenericContext context = gen.CreateContext(profile, player, this, hasKeyboardPlayer);
                context.PlayerID = player.PlayerID;
                context.IsFullscreen = isFullscreen;

                context.ExePath = exePath;
                context.RootInstallFolder = exeFolder;
                context.RootFolder = linkFolder;

                if (gen.HexEditAllExes?.Length > 0)
                {
                    Log("HexEditAllExes - Patching executable");

                    bool origExists = false;
                    if (File.Exists(Path.Combine(Path.GetDirectoryName(exePath), Path.GetFileNameWithoutExtension(exePath) + "-ORIG.exe")))
                    {
                        origExists = true;
                        //File.Delete(Path.Combine(Path.GetDirectoryName(exePath), Path.GetFileNameWithoutExtension(exePath) + "-ORIG.exe"));
                    }

                    foreach (string asciiValues in gen.HexEditAllExes)
                    {
                        if (origExists)
                        {
                        
                        
                            string[] splitValues = asciiValues.Split('|');
                            if (splitValues.Length == 2)
                            {
                                Log(string.Format("Temporarily renaming original executable {0} to {1}", gen.ExecutableName, Path.GetFileNameWithoutExtension(gen.ExecutableName) + "-TEMP.exe"));
                                File.Move(exePath, Path.Combine(Path.GetDirectoryName(exePath), Path.GetFileNameWithoutExtension(exePath) + "-TEMP.exe"));
                                Log(string.Format("Created patched executable {0} where the text string '{1}' has been replaced with '{2}'", gen.ExecutableName, splitValues[0], splitValues[1]));
                                context.PatchFile(exePath.Substring(0, exePath.Length - 4) + "-TEMP.exe", exePath, splitValues[0], splitValues[1]);
                                Log(string.Format("Deleting temporary executable {0}", Path.GetFileNameWithoutExtension(gen.ExecutableName) + "-TEMP.exe"));
                                File.Delete(Path.Combine(Path.GetDirectoryName(exePath), Path.GetFileNameWithoutExtension(exePath) + "-TEMP.exe"));
                            }
                            else
                            {
                                Log("Invalid # of parameters provided for: " + asciiValues + ", skipping");
                            }
                        }
                        else
                        {
                        
                        
                            string[] splitValues = asciiValues.Split('|');
                            if (splitValues.Length == 2)
                            {
                                Log(string.Format("Renaming original executable {0} to {1}", gen.ExecutableName, Path.GetFileNameWithoutExtension(gen.ExecutableName) + "-ORIG.exe"));
                                File.Move(exePath, Path.Combine(Path.GetDirectoryName(exePath), Path.GetFileNameWithoutExtension(exePath) + "-ORIG.exe"));
                                Log(string.Format("Created patched executable {0} where the text string '{1}' has been replaced with '{2}'", gen.ExecutableName, splitValues[0], splitValues[1]));
                                context.PatchFile(exePath.Substring(0, exePath.Length - 4) + "-ORIG.exe", exePath, splitValues[0], splitValues[1]);
                            }
                            else
                            {
                                Log("Invalid # of parameters provided for: " + asciiValues + ", skipping");
                            }
                        }

                        //File.Move(exePath, Path.Combine(Path.GetDirectoryName(exePath), Path.GetFileNameWithoutExtension(exePath) + "-ORIG.exe"));
                        //string[] splitValues = asciiValues.Split('|');
                        //if (splitValues.Length > 1)
                        //{
                        //    context.PatchFile(exePath.Substring(0, exePath.Length - 4) + "-ORIG.exe", exePath, splitValues[0], splitValues[1]);
                        //}
                    }
                    Log("Patching executable complete");
                }

                if (gen.HexEditExe?.Length > 0)
                {
                    //foreach (string asciiValues in gen.HexEditExe)
                    //{
                    Log("HexEditExe - Patching individual executable");
                    bool origExists = false;
                    if (File.Exists(Path.Combine(Path.GetDirectoryName(exePath), Path.GetFileNameWithoutExtension(exePath) + "-ORIG.exe")))
                    {
                        origExists = true;
                        //File.Delete(Path.Combine(Path.GetDirectoryName(exePath), Path.GetFileNameWithoutExtension(exePath) + "-ORIG.exe"));
                    }

                    if (origExists)
                    {

                        string[] splitValues = gen.HexEditExe[i].Split('|');
                        if (splitValues.Length == 2)
                        {
                            Log(string.Format("Temporarily renaming original executable {0} to {1}", gen.ExecutableName, Path.GetFileNameWithoutExtension(gen.ExecutableName) + "-TEMP.exe"));
                            File.Move(exePath, Path.Combine(Path.GetDirectoryName(exePath), Path.GetFileNameWithoutExtension(exePath) + "-TEMP.exe"));                          
                            Log(string.Format("Created patched executable {0} where the text string '{1}' has been replaced with '{2}'", gen.ExecutableName, splitValues[0], splitValues[1]));
                            context.PatchFile(exePath.Substring(0, exePath.Length - 4) + "-TEMP.exe", exePath, splitValues[0], splitValues[1]);
                            Log(string.Format("Deleting temporary executable {0}", Path.GetFileNameWithoutExtension(gen.ExecutableName) + "-TEMP.exe"));
                            File.Delete(Path.Combine(Path.GetDirectoryName(exePath), Path.GetFileNameWithoutExtension(exePath) + "-TEMP.exe"));
                        }
                        else
                        {
                            Log("Invalid # of parameters provided for: " + gen.HexEditExe[i] + ", skipping");
                        }

                    }
                    else
                    {

                        string[] splitValues = gen.HexEditExe[i].Split('|');
                        if (splitValues.Length == 2)
                        {
                            Log(string.Format("Renaming original executable {0} to {1}", gen.ExecutableName, Path.GetFileNameWithoutExtension(gen.ExecutableName) + "-ORIG.exe"));
                            File.Move(exePath, Path.Combine(Path.GetDirectoryName(exePath), Path.GetFileNameWithoutExtension(exePath) + "-ORIG.exe"));
                            Log(string.Format("Created patched executable {0} where {1} has been replaced with {2}", gen.ExecutableName, splitValues[0], splitValues[1]));
                            context.PatchFile(exePath.Substring(0, exePath.Length - 4) + "-ORIG.exe", exePath, splitValues[0], splitValues[1]);
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(gen.HexEditExe[i]))
                            {
                                Log("Nothing to change for this instance's executable");
                            }
                            else
                            {
                                Log("Invalid # of parameters provided for: " + gen.HexEditFile[i] + ", skipping");
                            }
                        }
                    }

                    Log("Patching executable complete");
                    //}
                }

                if (gen.HexEditAllFiles?.Length > 0)
                {
                    foreach (string asciiValues in gen.HexEditAllFiles)
                    {
                        string[] splitValues = asciiValues.Split('|');
                        if(splitValues.Length == 3)
                        {
                            string filePath = splitValues[0];
                            string fullPath = Path.Combine(Path.Combine(linkFolder, filePath));
                            string fullFileName = Path.GetFileName(filePath);
                            string strToSearch = splitValues[1];
                            string replacedStr = splitValues[2];
                            Log(string.Format("HexEditAllFiles - Patching file: {0}",filePath));

                            //if(!File.Exists(Path.GetFileNameWithoutExtension(fullPath)))
                            //{
                            //    if (File.Exists(Path.Combine(rootFolder, filePath)))
                            //    {
                            //        File.Copy(Path.Combine(rootFolder, filePath), fullPath, true);
                            //    }
                            //}
                            
                            bool origExists = false;
                            if (File.Exists(Path.GetDirectoryName(fullPath) + "\\" + Path.GetFileNameWithoutExtension(fullPath) + "-ORIG" + Path.GetExtension(filePath)))
                            {
                                origExists = true;
                                //File.Delete(Path.Combine(Path.GetDirectoryName(exePath), Path.GetFileNameWithoutExtension(exePath) + "-ORIG.exe"));
                            }

                            if (origExists)
                            {
                                Log(string.Format("Temporarily renaming original file {0} to {1}", fullFileName, Path.GetFileNameWithoutExtension(fullFileName) + "-TEMP" + Path.GetExtension(filePath)));
                                File.Move(fullPath, Path.Combine(Path.GetDirectoryName(fullPath), Path.GetFileNameWithoutExtension(fullPath) + "-TEMP" + Path.GetExtension(filePath)));
                                Log(string.Format("Created patched executable {0} where the text string '{1}' has been replaced with '{2}'", fullFileName, strToSearch, replacedStr));
                                context.PatchFile(fullPath.Substring(0, fullPath.Length - 4) + "-TEMP" + Path.GetExtension(filePath), fullPath, splitValues[1], splitValues[2]);
                                Log(string.Format("Deleting temporary file {0}", Path.GetFileNameWithoutExtension(fullFileName) + "-TEMP" + Path.GetExtension(filePath)));
                                File.Delete(Path.Combine(Path.GetDirectoryName(fullPath), Path.GetFileNameWithoutExtension(fullPath) + "-TEMP" + Path.GetExtension(filePath)));
                            }
                            else
                            {
                                Log(string.Format("Renaming original file {0} to {1}", fullFileName, Path.GetFileNameWithoutExtension(fullFileName) + "-ORIG" + Path.GetExtension(filePath)));
                                File.Move(fullPath, Path.Combine(Path.GetDirectoryName(fullPath), Path.GetFileNameWithoutExtension(fullPath) + "-ORIG" + Path.GetExtension(filePath)));
                                Log(string.Format("Created patched file {0} where the text string '{1}' has been replaced with '{2}'", fullFileName, strToSearch, replacedStr));
                                context.PatchFile(fullPath.Substring(0, fullPath.Length - 4) + "-ORIG" + Path.GetExtension(filePath), fullPath, splitValues[1], splitValues[2]);
                            }
                        }
                        else
                        {
                            Log("Invalid # of parameters provided for: " + asciiValues + ", skipping");
                        }
                    }
                    Log("Patching executable complete");
                }

                if (gen.HexEditFile?.Length > 0)
                {
                    string[] splitValues = gen.HexEditFile[i].Split('|');
                    if (splitValues.Length == 3)
                    {
                        string filePath = splitValues[0];
                        string fullPath = Path.Combine(Path.Combine(linkFolder, filePath));
                        string fullFileName = Path.GetFileName(filePath);
                        string strToSearch = splitValues[1];
                        string replacedStr = splitValues[2];
                        Log(string.Format("HexEditFile - Patching file: {0}", filePath));

                        bool origExists = false;
                        if (File.Exists(Path.GetDirectoryName(fullPath) + "\\" + Path.GetFileNameWithoutExtension(fullPath) + "-ORIG" + Path.GetExtension(filePath)))
                        {
                            origExists = true;
                            //File.Delete(Path.Combine(Path.GetDirectoryName(exePath), Path.GetFileNameWithoutExtension(exePath) + "-ORIG.exe"));
                        }

                        if (origExists)
                        {
                            Log(string.Format("Temporarily renaming original file {0} to {1}", fullFileName, Path.GetFileNameWithoutExtension(fullFileName) + "-TEMP" + Path.GetExtension(filePath)));
                            File.Move(fullPath, Path.Combine(Path.GetDirectoryName(fullPath), Path.GetFileNameWithoutExtension(fullPath) + "-TEMP" + Path.GetExtension(filePath)));
                            Log(string.Format("Created patched executable {0} where the text string '{1}' has been replaced with '{2}'", fullFileName, strToSearch, replacedStr));
                            context.PatchFile(fullPath.Substring(0, fullPath.Length - 4) + "-TEMP" + Path.GetExtension(filePath), fullPath, splitValues[1], splitValues[2]);
                            Log(string.Format("Deleting temporary file {0}", Path.GetFileNameWithoutExtension(fullFileName) + "-TEMP" + Path.GetExtension(filePath)));
                            File.Delete(Path.Combine(Path.GetDirectoryName(fullPath), Path.GetFileNameWithoutExtension(fullPath) + "-TEMP" + Path.GetExtension(filePath)));
                        }
                        else
                        {
                            Log(string.Format("Renaming original file {0} to {1}", fullFileName, Path.GetFileNameWithoutExtension(fullFileName) + "-ORIG" + Path.GetExtension(filePath)));
                            File.Move(fullPath, Path.Combine(Path.GetDirectoryName(fullPath), Path.GetFileNameWithoutExtension(fullPath) + "-ORIG" + Path.GetExtension(filePath)));
                            Log(string.Format("Created patched file {0} where the text string '{1}' has been replaced with '{2}'", fullFileName, strToSearch, replacedStr));
                            context.PatchFile(fullPath.Substring(0, fullPath.Length - 4) + "-ORIG" + Path.GetExtension(filePath), fullPath, splitValues[1], splitValues[2]);
                        }
                    }
                    else
                    {
                        Log("Invalid # of parameters provided for: " + gen.HexEditFile[i] + ", skipping");
                    }
                    Log("Patching executable complete");
                }

                if (gen.UseSteamStubDRMPatcher)
                {
                    string utilFolder = Path.Combine(Directory.GetCurrentDirectory(), "utils\\Steam Stub DRM Patcher");

                    try
                    {
                        if (File.Exists(Path.Combine(instanceExeFolder, "winmm.dll")))
                        {
                            File.Delete(Path.Combine(instanceExeFolder, "winmm.dll"));
                        }
                        Log(string.Format("Copying over winmm.dll ({0})", garch));
                        File.Copy(Path.Combine(utilFolder, garch + "\\winmm.dll"), Path.Combine(instanceExeFolder, "winmm.dll"), true);
                    }

                    catch (Exception ex)
                    {
                        Log("ERROR - " + ex.Message);
                        Log("Using alternative copy method for winmm.dll");
                        CmdUtil.ExecuteCommand(utilFolder, out int exitCode, "copy \"" + Path.Combine(utilFolder, garch + "\\winmm.dll") + "\" \"" + Path.Combine(instanceExeFolder, "winmm.dll") + "\"");
                    }
                }

                if (gen.UseGoldberg)
                {
                    Log("Starting Goldberg setup");
                    string utilFolder = Path.Combine(Directory.GetCurrentDirectory(), "utils\\GoldbergEmu");
                    string steamDllrootFolder = string.Empty;
                    string steamDllFolder = string.Empty;
                    string instanceSteamDllFolder = string.Empty;
                    string instanceSteamSettingsFolder = string.Empty;
                    //string instanceSteam_SettingsFolder = string.Empty;
                    string prevSteamDllFilePath = string.Empty;
                    

                    string steam64Dll = string.Empty;
                    string steamDll = string.Empty;
                    if (gen.GoldbergExperimental)
                    {
                        Log("Using experimental Goldberg");
                        steam64Dll += "experimental\\";
                        steamDll += "experimental\\";
                    }
                    steam64Dll += "steam_api64.dll";
                    steamDll += "steam_api.dll";


                    string[] steamDllFiles = Directory.GetFiles(rootFolder, "steam_api*.dll", SearchOption.AllDirectories);
                    foreach (string nameFile in steamDllFiles)
                    {
                        Log("Found " + nameFile);
                        steamDllrootFolder = Path.GetDirectoryName(nameFile);
                        string tempRootFolder = rootFolder;
                        if (tempRootFolder.EndsWith("\\"))
                        {
                            tempRootFolder = tempRootFolder.Substring(0, tempRootFolder.Length - 1);
                        }
                        steamDllFolder = steamDllrootFolder.Remove(0, (tempRootFolder.Length));
                        //instanceSteamDllFolder = Path.Combine(linkFolder, steamDllFolder);
                        instanceSteamDllFolder = linkFolder.TrimEnd('\\') + "\\" + steamDllFolder.TrimStart('\\');
                        instanceSteamSettingsFolder = Path.Combine(instanceSteamDllFolder, "settings");
                        //instanceSteam_SettingsFolder = Path.Combine(instanceSteamDllFolder, "steam_settings");
                        //MessageBox.Show(string.Format("Directories\n\ntempRootFolder={0}\n\nsteamDllFolder={1}\n\ninstanceSteamDllFolder={2}\n\ninstanceSteamSettingsFolder={3}\n\nrootFolder={4}\n\nsteamDllrootFolder={5}\n\nlinkFolder={6}", tempRootFolder, steamDllFolder, instanceSteamDllFolder, instanceSteamSettingsFolder, rootFolder, steamDllrootFolder, linkFolder));
                        //if(Directory.Exists(instanceSteamSettingsFolder))
                        //{
                        //    Directory.Delete(instanceSteamSettingsFolder);
                        //}
                        Directory.CreateDirectory(instanceSteamSettingsFolder);
                        //Directory.CreateDirectory(instanceSteam_SettingsFolder);

                        if (nameFile.EndsWith("steam_api64.dll"))
                        {
                            try
                            {
                                if (File.Exists(Path.Combine(instanceSteamDllFolder, "steam_api64.dll")))
                                {
                                    if(gen.GoldbergExperimental)
                                    {
                                        if (File.Exists(Path.Combine(instanceSteamDllFolder, "cracksteam_api64.dll")))
                                        {
                                            Log("cracksteam_api64.dll already exists in instance folder, deleting it and then renaming");
                                            File.Delete(Path.Combine(instanceSteamDllFolder, "cracksteam_api64.dll"));
                                        }
                                        Log("Renaming original steam_api64.dll to cracksteam_api64.dll");
                                        File.Move(Path.Combine(instanceSteamDllFolder, "steam_api64.dll"), Path.Combine(instanceSteamDllFolder, "cracksteam_api64.dll"));
                                    }
                                    else
                                    {
                                        File.Delete(Path.Combine(instanceSteamDllFolder, "steam_api64.dll"));
                                    }
                                }
                                Log("Placing Goldberg steam_api64.dll in instance steam dll folder");
                                File.Copy(Path.Combine(utilFolder, steam64Dll), Path.Combine(instanceSteamDllFolder, "steam_api64.dll"), true);
                            }
                            catch(Exception ex)
                            {
                                Log("ERROR - " + ex.Message);
                                Log("Using alternative copy method for steam_api64.dll");
                                CmdUtil.ExecuteCommand(utilFolder, out int exitCode, "copy \"" + Path.Combine(utilFolder, steam64Dll) + "\" \"" + Path.Combine(instanceSteamDllFolder, "steam_api64.dll") + "\"");
                            }
                        }

                        if (nameFile.EndsWith("steam_api.dll"))
                        {
                            try
                            {
                                if (File.Exists(Path.Combine(instanceSteamDllFolder, "steam_api.dll")))
                                {
                                    if (gen.GoldbergExperimental)
                                    {
                                        if (File.Exists(Path.Combine(instanceSteamDllFolder, "cracksteam_api.dll")))
                                        {
                                            Log("cracksteam_api.dll already exists in instance folder, deleting it and then renaming");
                                            File.Delete(Path.Combine(instanceSteamDllFolder, "cracksteam_api.dll"));
                                        }
                                        Log("Renaming original steam_api.dll to cracksteam_api.dll");
                                        File.Move(Path.Combine(instanceSteamDllFolder, "steam_api.dll"), Path.Combine(instanceSteamDllFolder, "cracksteam_api.dll"));
                                    }
                                    else
                                    {
                                        File.Delete(Path.Combine(instanceSteamDllFolder, "steam_api.dll"));
                                    }
                                }
                                Log("Placing Goldberg steam_api.dll in instance steam dll folder");
                                File.Copy(Path.Combine(utilFolder, steamDll), Path.Combine(instanceSteamDllFolder, "steam_api.dll"), true);
                            }
                            catch(Exception ex)
                            {
                                Log("ERROR - " + ex.Message);
                                Log("Using alternative copy method for steam_api.dll");
                                CmdUtil.ExecuteCommand(utilFolder, out int exitCode, "copy \"" + Path.Combine(utilFolder, steamDll) + "\" \"" + Path.Combine(instanceSteamDllFolder, "steam_api.dll") + "\"");
                            }
                        }

                        if(gen.GoldbergExperimental)
                        {
                            if (File.Exists(Path.Combine(instanceSteamDllFolder, "steamclient.dll")))
                            {
                                File.Delete(Path.Combine(instanceSteamDllFolder, "steamclient.dll"));
                            }
                            Log("Placing Goldberg steamclient.dll in instance steam dll folder");
                            File.Copy(Path.Combine(utilFolder, "experimental\\steamclient.dll"), Path.Combine(instanceSteamDllFolder, "steamclient.dll"), true);

                            if (File.Exists(Path.Combine(instanceSteamDllFolder, "steamclient64.dll")))
                            {
                                File.Delete(Path.Combine(instanceSteamDllFolder, "steamclient64.dll"));
                            }
                            Log("Placing Goldberg steamclient64.dll in instance steam dll folder");
                            File.Copy(Path.Combine(utilFolder, "experimental\\steamclient64.dll"), Path.Combine(instanceSteamDllFolder, "steamclient64.dll"), true);
                        }

                        if (!string.IsNullOrEmpty(prevSteamDllFilePath))
                        {
                            if (prevSteamDllFilePath == nameFile)
                            {
                                continue;
                            }
                        }
                        prevSteamDllFilePath = nameFile;

                        //File.WriteAllText(Path.Combine(instanceSteam_SettingsFolder, "offline.txt"), "");

                        if (ini.IniReadValue("Misc", "UseNicksInGame") == "True" && !string.IsNullOrEmpty(player.Nickname))
                        {
                            if (File.Exists(Path.Combine(instanceSteamSettingsFolder, "account_name.txt")))
                            {
                                File.Delete(Path.Combine(instanceSteamSettingsFolder, "account_name.txt"));
                            }
                            File.WriteAllText(Path.Combine(instanceSteamSettingsFolder, "account_name.txt"), player.Nickname);
                            Log("Generating account_name.txt with nickname " + player.Nickname);
                        }
                        else
                        {
                            if (File.Exists(Path.Combine(instanceSteamSettingsFolder, "account_name.txt")))
                            {
                                File.Delete(Path.Combine(instanceSteamSettingsFolder, "account_name.txt"));
                            }
                            File.WriteAllText(Path.Combine(instanceSteamSettingsFolder, "account_name.txt"), "Player " + (i + 1));
                            Log("Generating account_name.txt with nickname Player " + (i + 1));
                        }

                        Log("Generating user_steam_id.txt with random user steam ID " + (random_steam_id + i).ToString());
                        if (File.Exists(Path.Combine(instanceSteamSettingsFolder, "user_steam_id.txt")))
                        {
                            File.Delete(Path.Combine(instanceSteamSettingsFolder, "user_steam_id.txt"));
                        }
                        File.WriteAllText(Path.Combine(instanceSteamSettingsFolder, "user_steam_id.txt"), (random_steam_id + i).ToString());

                        string lang = "english";
                        if(gen.GoldbergLanguage?.Length > 0)
                        {
                            lang = gen.GoldbergLanguage;
                        }
                        else
                        {
                            lang = gen.GetSteamLanguage();
                        }
                        Log("Generating language.txt with language set to " + lang);
                        if (File.Exists(Path.Combine(instanceSteamSettingsFolder, "language.txt")))
                        {
                            File.Delete(Path.Combine(instanceSteamSettingsFolder, "language.txt"));
                        }
                        File.WriteAllText(Path.Combine(instanceSteamSettingsFolder, "language.txt"), lang);

                        if (gen.GoldbergIgnoreSteamAppId)
                        {
                            Log("Skipping steam_appid.txt creation");
                        }
                        else
                        {
                            Log("Generating steam_appid.txt using game steam ID " + gen.SteamID);
                            if (File.Exists(Path.Combine(instanceSteamDllFolder, "steam_appid.txt")))
                            {
                                File.Delete(Path.Combine(instanceSteamDllFolder, "steam_appid.txt"));
                            }
                            File.WriteAllText(Path.Combine(instanceSteamDllFolder, "steam_appid.txt"), gen.SteamID);
                        }

                        if (File.Exists(Path.Combine(instanceSteamDllFolder, "local_save.txt")))
                        {
                            File.Delete(Path.Combine(instanceSteamDllFolder, "local_save.txt"));
                        }
                        File.WriteAllText(Path.Combine(instanceSteamDllFolder, "local_save.txt"), "");

                        if (gen.GoldbergNeedSteamInterface)
                        {
                            if (File.Exists(Path.Combine(instanceSteamDllFolder, "steam_interfaces.txt")))
                            {
                                Log("steam_interfaces.txt already exists in instance folder, deleting that file first then copying");
                                File.Delete(Path.Combine(instanceSteamDllFolder, "steam_interfaces.txt"));
                            }
                            if (File.Exists(Path.Combine(utilFolder, "tools\\steam_interfaces.txt")))
                            {
                                Log("Found generated steam_interfaces.txt file in Nucleus util folder, copying this file");
                                //if (File.Exists(Path.Combine(instanceSteamDllFolder, "steam_interfaces.txt")))
                                //{
                                //    Log("steam_interfaces.txt already exists in instance folder, deleting that file first then copying");
                                //    File.Delete(Path.Combine(instanceSteamDllFolder, "steam_interfaces.txt"));
                                //}
                                File.Copy(Path.Combine(utilFolder, "tools\\steam_interfaces.txt"), Path.Combine(instanceSteamDllFolder, "steam_interfaces.txt"), true);
                            }
                            else if (File.Exists(Path.Combine(steamDllrootFolder, "steam_interfaces.txt")))
                            {
                                Log("Found steam_interfaces.txt in original game folder, copying this file");
                                //if (File.Exists(Path.Combine(instanceSteamDllFolder, "steam_interfaces.txt")))
                                //{
                                //    Log("steam_interfaces.txt already exists in instance folder, deleting that file first then copying");
                                //    File.Delete(Path.Combine(instanceSteamDllFolder, "steam_interfaces.txt"));
                                //}
                                File.Copy(Path.Combine(steamDllrootFolder, "steam_interfaces.txt"), Path.Combine(instanceSteamDllFolder, "steam_interfaces.txt"), true);
                            }
                            else if (File.Exists(nucleusRootFolder.TrimEnd('\\') + "\\scripts\\" + gen.JsFileName.Substring(0, gen.JsFileName.Length - 3) + "\\steam_interfaces.txt"))
                            {
                                Log("Found steam_interfaces.txt in Nucleus game folder");
                                //if (File.Exists(Path.Combine(instanceSteamDllFolder, "steam_interfaces.txt")))
                                //{
                                //    Log("steam_interfaces.txt already exists in instance folder, deleting that file first then copying");
                                //    File.Delete(Path.Combine(instanceSteamDllFolder, "steam_interfaces.txt"));
                                //}
                                File.Copy(nucleusRootFolder.TrimEnd('\\') + "\\scripts\\" + gen.JsFileName.Substring(0, gen.JsFileName.Length - 3) + "\\steam_interfaces.txt", Path.Combine(instanceSteamDllFolder, "steam_interfaces.txt"), true);
                            }
                            else if (gen.OrigSteamDllPath?.Length > 0 && File.Exists(gen.OrigSteamDllPath))
                            {
                                Log("Attempting to create steam_interfaces.txt with the steam api dll located at: " + gen.OrigSteamDllPath);
                                Process cmd = new Process();
                                cmd.StartInfo.FileName = "cmd.exe";
                                cmd.StartInfo.RedirectStandardInput = true;
                                cmd.StartInfo.RedirectStandardOutput = true;
                                //cmd.StartInfo.CreateNoWindow = true;
                                cmd.StartInfo.UseShellExecute = false;
                                cmd.StartInfo.WorkingDirectory = Path.Combine(utilFolder, "tools");
                                cmd.Start();

                                cmd.StandardInput.WriteLine("generate_interfaces_file.exe \"" + gen.OrigSteamDllPath + "\"");
                                cmd.StandardInput.Flush();
                                cmd.StandardInput.Close();
                                cmd.WaitForExit();
                                if (File.Exists(Path.Combine(instanceSteamDllFolder, "steam_interfaces.txt")))
                                {
                                    Log("steam_interfaces.txt already exists in instance folder, deleting that file first then copying");
                                    File.Delete(Path.Combine(instanceSteamDllFolder, "steam_interfaces.txt"));
                                }
                                Log("Copying over generated steam_interfaces.txt");
                                File.Copy(Path.Combine(Path.Combine(utilFolder, "tools"), "steam_interfaces.txt"), Path.Combine(instanceSteamDllFolder, "steam_interfaces.txt"), true);

                                if((i+1)==players.Count)
                                {
                                    Log("Deleting generated steam_interfaces.txt file");
                                    File.Delete(Path.Combine(utilFolder, "tools\\steam_interfaces.txt"));
                                }
                            }
                            else
                            {
                                //MessageBox.Show(string.Format("Directories\n\nnucleusRootFolder={0}\n\nformatted js filename={1}\n\nsteamDllrootFolder={2}", nucleusRootFolder,gen.JsFileName.Substring(0, gen.JsFileName.Length - 3),steamDllrootFolder));
                                Log("Unable to locate steam_interfaces.txt or create one, skipping this file");
                                if (i == 0)
                                {
                                    MessageBox.Show("Goldberg was unable to locate steam_interfaces.txt or create one. Process will continue without using this file.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }
                            }
                        }
                    }   

                    if(steamDllFiles == null || steamDllFiles.Length < 1)
                    {
                        Log("Unable to locate a steam_api(64).dll file, Goldberg will not be used");
                        MessageBox.Show("Goldberg was unable to locate a steam_api(64).dll file. The built-in Goldberg will not be used.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    Log("Goldberg setup complete");
                }

                if(gen.CreateSteamAppIdByExe)
                {
                    Log("Creating steam_appid.txt with steam ID " + gen.SteamID + " at " + GetRelativePath(instanceExeFolder, tempDir));
                    if (File.Exists(Path.Combine(instanceExeFolder, "steam_appid.txt")))
                    {
                        File.Delete(Path.Combine(instanceExeFolder, "steam_appid.txt"));
                    }
                    File.WriteAllText(Path.Combine(instanceExeFolder, "steam_appid.txt"), gen.SteamID);
                }

                if (gen.XInputPlusDll?.Length > 0)
                {
                    Log("Setting up XInput Plus");
                    string utilFolder = Path.Combine(Directory.GetCurrentDirectory(), "utils\\XInputPlus");
                    //string arch = string.Empty;
                    //if (Is64Bit(exePath) == true)
                    //{
                    //    arch = "x64";
                    //}
                    //else if (Is64Bit(exePath) == false)
                    //{
                    //    arch = "x86";
                    //}
                    //else
                    //{
                    //    Log(string.Format("ERROR - Machine type {0} not implemented", GetDllMachineType(exePath)));
                    //    //using (StreamWriter writer = new StreamWriter("error-log.txt", true))
                    //    //{
                    //    //    writer.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]" + "Machine type: '{0}' not implemented.", GetDllMachineType(exePath));
                    //    //}
                    //}

                    foreach(string xinputDllName in gen.XInputPlusDll)
                    {
                        string xinputDll = "xinput1_3.dl_";
                        try
                        {
                            if (File.Exists(Path.Combine(instanceExeFolder, xinputDllName)))
                            {
                                File.Delete(Path.Combine(instanceExeFolder, xinputDllName));
                            }
                            
                            if (xinputDllName.ToLower().StartsWith("dinput."))
                            {
                                xinputDll = "Dinput.dl_";
                            }
                            else if (xinputDllName.ToLower().StartsWith("dinput8."))
                            {
                                xinputDll = "Dinput8.dl_";
                            }
                            Log("Using " + xinputDll + " (" + garch + ") as base and naming it: " + xinputDllName);

                            File.Copy(Path.Combine(utilFolder, garch + "\\" + xinputDll), Path.Combine(instanceExeFolder, xinputDllName), true);
                        }
                        catch (Exception ex)
                        {
                            Log("ERROR - " + ex.Message);
                            Log("Using alternative copy method for " + xinputDll);
                            CmdUtil.ExecuteCommand(utilFolder, out int exitCode, "copy \"" + Path.Combine(utilFolder, garch + "\\" + xinputDll) + "\" \"" + Path.Combine(instanceExeFolder, xinputDllName) + "\"");
                        }

                        if (File.Exists(Path.Combine(instanceExeFolder, "XInputPlus.ini")))
                        {
                            File.Delete(Path.Combine(instanceExeFolder, "XInputPlus.ini"));
                        }
                        Log("Copying XInputPlus.ini");
                        File.Copy(Path.Combine(utilFolder, "XInputPlus.ini"), Path.Combine(instanceExeFolder, "XInputPlus.ini"), true);

                        //string[] change = new string[] {
                        //    context.FindLineNumberInTextFile(Path.Combine(instanceExeFolder, "XInputPlus.ini"), "Controller1=", SearchType.StartsWith) + "|Controller1=" + (i + 1),
                        //    context.FindLineNumberInTextFile(Path.Combine(instanceExeFolder, "XInputPlus.ini"), "FileVersion=", SearchType.StartsWith) + "|FileVersion=" + arch,
                        //};

                        Log("Making changes to the lines in XInputPlus.ini; FileVersion and Controller values");
                        List<string> textChanges = new List<string>();
                        textChanges.Add(context.FindLineNumberInTextFile(Path.Combine(instanceExeFolder, "XInputPlus.ini"), "FileVersion=", SearchType.StartsWith) + "|FileVersion=" + garch);

                        if(!player.IsKeyboardPlayer)
                        {
                            if (gen.PlayersPerInstance > 1)
                            {
                                for (int x = 1; x <= gen.PlayersPerInstance; x++)
                                {
                                    textChanges.Add(context.FindLineNumberInTextFile(Path.Combine(instanceExeFolder, "XInputPlus.ini"), "Controller" + x + "=", SearchType.StartsWith) + "|Controller" + x + "=" + (x + plyrIndex));
                                }
                                plyrIndex += gen.PlayersPerInstance;
                            }
                            else
                            {
                                textChanges.Add(context.FindLineNumberInTextFile(Path.Combine(instanceExeFolder, "XInputPlus.ini"), "Controller1=", SearchType.StartsWith) + "|Controller1=" + (i + 1));
                            }
                        }
                        else
                        {
                            Log("Skipping setting controller value for this instance, as this player is using keyboard");
                        }

                        context.ReplaceLinesInTextFile(Path.Combine(instanceExeFolder, "XInputPlus.ini"), textChanges.ToArray());
                    }
                    Log("XInput Plus setup complete");
                }

                if(gen.UseDevReorder)
                {
                    Log("Setting up Devreorder");
                    string utilFolder = Path.Combine(Directory.GetCurrentDirectory(), "utils\\devreorder");
                    //string arch = string.Empty;
                    //if (Is64Bit(exePath) == true)
                    //{
                    //    arch = "x64";
                    //}
                    //else if (Is64Bit(exePath) == false)
                    //{
                    //    arch = "x86";
                    //}
                    //else
                    //{
                    //    Log(string.Format("ERROR - Machine type {0} not implemented", GetDllMachineType(exePath)));
                    //    //using (StreamWriter writer = new StreamWriter("error-log.txt", true))
                    //    //{
                    //    //    writer.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]" + "Machine type: '{0}' not implemented.", GetDllMachineType(exePath));
                    //    //}
                    //}

                    if (File.Exists(Path.Combine(instanceExeFolder, "dinput8.dll")))
                    {
                        File.Delete(Path.Combine(instanceExeFolder, "dinput8.dll"));
                    }
                    Log("Copying dinput8.dll");
                    File.Copy(Path.Combine(utilFolder, garch + "\\dinput8.dll"), Path.Combine(instanceExeFolder, "dinput8.dll"), true);

                    if (File.Exists(Path.Combine(instanceExeFolder, "devreorder.ini")))
                    {
                        File.Delete(Path.Combine(instanceExeFolder, "devreorder.ini"));
                    }

                    List<string> iniConfig = new List<string>();
                    iniConfig.Add("[order]");
                    iniConfig.Add("{" + player.GamepadGuid + "}");
                    iniConfig.Add(string.Empty);
                    iniConfig.Add("[hidden]");

                    for (int p = 0; p < players.Count; p++)
                    {
                        if(p != i)
                        {
                            iniConfig.Add("{" + players[p].GamepadGuid + "}");
                        }
                    }
                    Log("Writing devreorder.ini with the only visible gamepad guid: " + player.GamepadGuid);
                    File.WriteAllLines(Path.Combine(instanceExeFolder, "devreorder.ini"), iniConfig.ToArray());
                    Log("devreorder setup complete");
                }

                if (gen.UseX360ce)
                {
                    Log("Setting up x360ce");
                    string x360exe = "";
                    string x360dll = "";
                    string utilFolder = Path.Combine(Directory.GetCurrentDirectory(), "utils\\x360ce");
                    if (i == 0)
                    {
                        if (gameIs64)
                        {
                            x360exe = "x360ce_x64.exe";
                            x360dll = "xinput1_3_x64.dll";
                        }
                        else //if (Is64Bit(exePath) == false)
                        {
                            x360exe = "x360ce.exe";
                            x360dll = "xinput1_3.dll";
                        }
                        //else
                        //{
                        //    Log(string.Format("ERROR - Machine type {0} not implemented", GetDllMachineType(exePath)));
                        //    //using (StreamWriter writer = new StreamWriter("error-log.txt", true))
                        //    //{
                        //    //    writer.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]" + "Machine type: '{0}' not implemented.", GetDllMachineType(exePath));
                        //    //}
                        //}

                        if (File.Exists(Path.Combine(instanceExeFolder, x360exe)))
                        {
                            File.Delete(Path.Combine(instanceExeFolder, x360exe));
                        }
                        Log("Copying over " + x360exe);
                        File.Copy(Path.Combine(utilFolder, x360exe), Path.Combine(instanceExeFolder, x360exe), true);

                        if (File.Exists(Path.Combine(instanceExeFolder, "xinput1_3.dll")))
                        {
                            File.Delete(Path.Combine(instanceExeFolder, "xinput1_3.dll"));
                        }
                        if(x360dll != "xinput1_3.dll")
                        {
                            Log("Copying over " + x360dll + " and renaming it to xinput1_3.dll");
                        }
                        else
                        {
                            Log("Copying over " + x360dll);
                        }
                        File.Copy(Path.Combine(utilFolder, x360dll), Path.Combine(instanceExeFolder, "xinput1_3.dll"), true);

                        Log("Starting x360ce process");
                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.UseShellExecute = true;
                        startInfo.WorkingDirectory = Path.GetDirectoryName(exePath);
                        startInfo.FileName = Path.Combine(instanceExeFolder, x360exe);
                        //if (gen.RunAsAdmin)
                        //{
                        //    startInfo.Verb = "runas";
                        //}
                        Process util = Process.Start(startInfo);
                        Log("Waiting until x360ce process is exited");
                        util.WaitForExit();
                    }
                    else
                    {
                        if (File.Exists(Path.Combine(instanceExeFolder, "x360ce.ini")))
                        {
                            File.Delete(Path.Combine(instanceExeFolder, "x360ce.ini"));
                        }
                        if (File.Exists(Path.Combine(instanceExeFolder, "xinput1_3.dll")))
                        {
                            File.Delete(Path.Combine(instanceExeFolder, "xinput1_3.dll"));
                        }
                        Log("Carrying over xinput1_3.dll and x360ce.ini from Instance0");
                        File.Copy(Path.Combine(instanceExeFolder.Substring(0, instanceExeFolder.LastIndexOf('\\') + 1) + "Instance0", "xinput1_3.dll"), Path.Combine(instanceExeFolder, "xinput1_3.dll"), true);
                        File.Copy(Path.Combine(instanceExeFolder.Substring(0, instanceExeFolder.LastIndexOf('\\') + 1) + "Instance0", "x360ce.ini"), Path.Combine(instanceExeFolder, "x360ce.ini"), true);
                    }

                    Log("Making changes to x360ce.ini; PAD mapping to player");
                    //string[] change = new string[] {
                    //    context.FindLineNumberInTextFile(Path.Combine(instanceExeFolder, "x360ce.ini"), "PAD1=", SearchType.StartsWith) + "|PAD1=" + context.x360ceGamepadGuid,
                    //    context.FindLineNumberInTextFile(Path.Combine(instanceExeFolder, "x360ce.ini"), "PAD2=", SearchType.StartsWith) + "|PAD2=",
                    //    context.FindLineNumberInTextFile(Path.Combine(instanceExeFolder, "x360ce.ini"), "PAD3=", SearchType.StartsWith) + "|PAD3=",
                    //    context.FindLineNumberInTextFile(Path.Combine(instanceExeFolder, "x360ce.ini"), "PAD4=", SearchType.StartsWith) + "|PAD4="
                    //};

                    List<string> textChanges = new List<string>();
                    
                    if(!player.IsKeyboardPlayer)
                    {
                        if (gen.PlayersPerInstance > 1)
                        {
                            for (int x = 1; x <= gen.PlayersPerInstance; x++)
                            {
                                textChanges.Add(context.FindLineNumberInTextFile(Path.Combine(instanceExeFolder, "x360ce.ini"), "PAD" + x + "=", SearchType.StartsWith) + "|PAD" + x + "=IG_" + players[x].GamepadGuid.ToString().Replace("-", string.Empty));
                            }
                            for (int x = gen.PlayersPerInstance + 1; x <= 4; x++)
                            {
                                textChanges.Add(context.FindLineNumberInTextFile(Path.Combine(instanceExeFolder, "x360ce.ini"), "PAD" + x + "=", SearchType.StartsWith) + "|PAD" + x + "=IG_" + players[x].GamepadGuid.ToString().Replace("-", string.Empty));
                            }
                            plyrIndex += gen.PlayersPerInstance;
                        }
                        else
                        {
                            textChanges.Add(context.FindLineNumberInTextFile(Path.Combine(instanceExeFolder, "x360ce.ini"), "PAD1=", SearchType.StartsWith) + "|PAD1=" + context.x360ceGamepadGuid);
                            textChanges.Add(context.FindLineNumberInTextFile(Path.Combine(instanceExeFolder, "x360ce.ini"), "PAD2=", SearchType.StartsWith) + "|PAD2=");
                            textChanges.Add(context.FindLineNumberInTextFile(Path.Combine(instanceExeFolder, "x360ce.ini"), "PAD3=", SearchType.StartsWith) + "|PAD3=");
                            textChanges.Add(context.FindLineNumberInTextFile(Path.Combine(instanceExeFolder, "x360ce.ini"), "PAD4=", SearchType.StartsWith) + "|PAD4=");
                        }

                        context.ReplaceLinesInTextFile(Path.Combine(instanceExeFolder, "x360ce.ini"), textChanges.ToArray());
                    }

                    if(gen.XboxOneControllerFix)
                    {
                        Log("Implementing Xbox One controller fix");
                        //change = new string[] { context.FindLineNumberInTextFile(Path.Combine(instanceExeFolder, "x360ce.ini"), "HookMode=1", SearchType.Full) + "|" +
                        //    "HookLL=0\n" +
                        //    "HookCOM=1\n" +
                        //    "HookSA=0\n" +
                        //    "HookWT=0\n" +
                        //    "HOOKDI=1\n" +
                        //    "HOOKPIDVID=1\n" +
                        //    "HookName=0\n" +
                        //    "HookMode=0\n"
                        //};
                        textChanges.Clear();
                        textChanges.Add(context.FindLineNumberInTextFile(Path.Combine(instanceExeFolder, "x360ce.ini"), "HookMode=1", SearchType.Full) + "|" +
                            "HookLL=0\n" +
                            "HookCOM=1\n" +
                            "HookSA=0\n" +
                            "HookWT=0\n" +
                            "HOOKDI=1\n" +
                            "HOOKPIDVID=1\n" +
                            "HookName=0\n" +
                            "HookMode=0\n"
                        );
                        context.ReplaceLinesInTextFile(Path.Combine(instanceExeFolder, "x360ce.ini"), textChanges.ToArray());
                    }
                    Log("x360ce setup complete");
                }

                if(gen.CopyCustomUtils?.Length > 0)
                {
                    Log("Copying custom files");
                    string utilFolder = Path.Combine(Directory.GetCurrentDirectory(), "utils\\User");
                    foreach (string customUtil in gen.CopyCustomUtils)
                    {
                        int numParams = customUtil.Count(x => x == '|') + 1;
                        string[] splitParams = customUtil.Split('|');
                        string utilName = splitParams[0];
                        string utilPath = string.Empty;
                        if (numParams == 2)
                        {
                            utilPath = splitParams[1];
                        }

                        if (numParams == 3)
                        {
                            string utilInstances = splitParams[2];
                            List<int> instances = new List<int>();
                            instances = utilInstances.Split(',').Select(Int32.Parse).ToList();
                            if(!instances.Contains(i))
                            {
                                continue;
                            }
                        }

                        if (File.Exists(Path.Combine(linkFolder.TrimEnd('\\') + '\\' + utilPath, Path.GetFileName(utilName))))
                        {
                            File.Delete(Path.Combine(linkFolder.TrimEnd('\\') + '\\' + utilPath, Path.GetFileName(utilName)));
                        }
                        Log("Copying " + utilName + " to " + "Instance" + i + "\\" + utilPath);
                        File.Copy(Path.Combine(utilFolder, utilName), Path.Combine(linkFolder.TrimEnd('\\') + '\\' + utilPath, Path.GetFileName(utilName)), true);
                    }
                    Log("Copying custom files complete");
                }

                gen.PrePlay(context, this, player);

                string startArgs = context.StartArguments;

                if (context.Hook.CustomDllEnabled)
                {
                    Log(string.Format("Setting up Custom DLL, UseAlpha8CustomDll: {0}",gen.Hook.UseAlpha8CustomDll));
                    byte[] xdata;
                    if (gen.Hook.UseAlpha8CustomDll && !gameIs64)
                    {
                        xdata = Properties.Resources.xinput1_3;
                    }
                    else
                    {
                        if(gen.Hook.UseAlpha8CustomDll)
                        {
                            Log("Using Alpha 10 custom dll as there is no Alpha 8 x64 custom dll");
                        }

                        if (gameIs64)
                        {
                            xdata = Properties.Resources.xinput1_3_a10_x64;
                        }
                        else //if (Is64Bit(exePath) == false)
                        {
                            xdata = Properties.Resources.xinput1_3_a10;
                        }
                        //else
                        //{
                        //    xdata = null;
                        //    Log(string.Format("ERROR - Machine type {0} not implemented", GetDllMachineType(exePath)));
                        //    //using (StreamWriter writer = new StreamWriter("error-log.txt", true))
                        //    //{
                        //    //    writer.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]" + "Machine type: '{0}' not implemented.", GetDllMachineType(exePath));
                        //    //}
                        //}
                    }
                    
                    if (context.Hook.XInputNames == null)
                    {
                        Log(string.Format("Writing custom dll xinput1_3.dll to {0}", Path.GetDirectoryName(GetRelativePath(exePath, nucleusRootFolder))));
                        using (Stream str = File.OpenWrite(Path.Combine(instanceExeFolder, "xinput1_3.dll")))
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
                            Log(string.Format("Writing custom dll {0} to {1}", xinputName, Path.GetDirectoryName(GetRelativePath(exePath, nucleusRootFolder))));
                            using (Stream str = File.OpenWrite(Path.Combine(instanceExeFolder, xinputName)))
                            {
                                str.Write(xdata, 0, xdata.Length);
                            }
                        }
                    }

                    Log(string.Format("Writing ncoop.ini to {0} with Game.Hook values", Path.GetDirectoryName(GetRelativePath(exePath, nucleusRootFolder))));
                    string ncoopIni = Path.Combine(instanceExeFolder, "ncoop.ini");
                    using (Stream str = File.OpenWrite(ncoopIni))
                    {
                        byte[] ini = Properties.Resources.ncoop;
                        str.Write(ini, 0, ini.Length);
                    }

                    IniFile x360 = new IniFile(ncoopIni);
                    x360.IniWriteValue("Options", "Log", "0");
                    x360.IniWriteValue("Options", "FileLog", "0");
                    x360.IniWriteValue("Options", "ForceFocus", gen.Hook.ForceFocus.ToString(CultureInfo.InvariantCulture));
                    if(!gen.Hook.UseAlpha8CustomDll)
                    {
                        x360.IniWriteValue("Options", "Version", "2");
                        x360.IniWriteValue("Options", "ForceFocusWindowRegex", gen.Hook.ForceFocusWindowName.ToString(CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        x360.IniWriteValue("Options", "ForceFocusWindowName", gen.Hook.ForceFocusWindowName.ToString(CultureInfo.InvariantCulture));
                    }

                    int wx;
                    int wy;
                    int rw;
                    int rh;
                    if (context.Hook.WindowX > 0 && context.Hook.WindowY > 0)
                    {
                        wx = context.Hook.WindowX;
                        wy = context.Hook.WindowY;
                        x360.IniWriteValue("Options", "WindowX", context.Hook.WindowX.ToString(CultureInfo.InvariantCulture));
                        x360.IniWriteValue("Options", "WindowY", context.Hook.WindowY.ToString(CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        wx = playerBounds.X;
                        wy = playerBounds.Y;
                        x360.IniWriteValue("Options", "WindowX", playerBounds.X.ToString(CultureInfo.InvariantCulture));
                        x360.IniWriteValue("Options", "WindowY", playerBounds.Y.ToString(CultureInfo.InvariantCulture));
                    }

                    if(context.Hook.ResWidth > 0 && context.Hook.ResHeight > 0)
                    {
                        rw = context.Hook.ResWidth;
                        rh = context.Hook.ResHeight;
                        x360.IniWriteValue("Options", "ResWidth", context.Hook.ResWidth.ToString(CultureInfo.InvariantCulture));
                        x360.IniWriteValue("Options", "ResHeight", context.Hook.ResHeight.ToString(CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        rw = context.Width;
                        rh = context.Height;
                        x360.IniWriteValue("Options", "ResWidth", context.Width.ToString(CultureInfo.InvariantCulture));
                        x360.IniWriteValue("Options", "ResHeight", context.Height.ToString(CultureInfo.InvariantCulture));
                    }

                    if (!gen.Hook.UseAlpha8CustomDll)
                    {
                        if (context.Hook.FixResolution)
                        {
                            Log(string.Format("Custom DLL will be doing the resizing with values width:{0}, height:{1}", rw, rh));
                            dllResize = true;
                        }
                        if (context.Hook.FixPosition)
                        {
                            Log(string.Format("Custom DLL will be doing the repositioning with values x:{0}, y:{1}", wx, wy));
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
                    Log("Custom DLL setup complete");
                }

                if (!gen.UseGoldberg && ini.IniReadValue("Misc", "UseNicksInGame") == "True")
                {
                    
                    string[] files = Directory.GetFiles(linkFolder, "account_name.txt", SearchOption.AllDirectories);
                    foreach (string nameFile in files)
                    {
                        if(!string.IsNullOrEmpty(player.Nickname))
                        {
                            Log(string.Format("Writing nickname {0} in account_name.txt", player.Nickname));
                            //MessageBox.Show("Found account_name.txt at: " + nameFile + ", replacing: " + File.ReadAllText(nameFile) + " with: " + player.Nickname + " for player " + i);
                            File.Delete(nameFile);
                            File.WriteAllText(nameFile, player.Nickname);
                        }
                    }
                }

                Process proc;
                if (context.NeedsSteamEmulation)
                {
                    Log("Setting up SmartSteamEmu");
                    
                    string steamEmu = Path.Combine(linkFolder, "SmartSteamLoader"); //GameManager.Instance.ExtractSteamEmu(Path.Combine(linkFolder, "SmartSteamLoader"));
                    string sourcePath = Path.Combine(GameManager.Instance.GetUtilsPath(), "SmartSteamEmu");

                    Log(string.Format("Copying SmartSteamEmu files to {0}", Path.GetDirectoryName(GetRelativePath(steamEmu, nucleusRootFolder))));
                    try
                    {
                        //Create all of the directories
                        foreach (string dirPath in Directory.GetDirectories(sourcePath, "*",
                            SearchOption.AllDirectories))
                            Directory.CreateDirectory(dirPath.Replace(sourcePath, steamEmu));

                        //Copy all the files & Replaces any files with the same name
                        foreach (string newPath in Directory.GetFiles(sourcePath, "*.*",
                            SearchOption.AllDirectories))
                            File.Copy(newPath, newPath.Replace(sourcePath, steamEmu), true);
                    }
                    catch
                    {
                        Log(string.Format("ERROR - Copying of SmartSteamEmu files failed!"));
                        return "Extraction of SmartSteamEmu failed!";
                    }

                    string sseLoader = string.Empty;
                    if (gameIs64)
                    {
                        sseLoader = "SmartSteamLoader_x64.exe";
                    }
                    else //if (Is64Bit(exePath) == false)
                    {
                        sseLoader = "SmartSteamLoader.exe";
                    }
                    //else
                    //{
                    //    Log(string.Format("ERROR - Machine type {0} not implemented", GetDllMachineType(exePath)));
                    //    //using (StreamWriter writer = new StreamWriter("error-log.txt", true))
                    //    //{
                    //    //    writer.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]" + "Machine type: '{0}' not implemented.", GetDllMachineType(exePath));
                    //    //}
                    //}

                    string emuExe = Path.Combine(steamEmu, sseLoader);
                    string emuIni = Path.Combine(steamEmu, "SmartSteamEmu.ini");
                    IniFile emu = new IniFile(emuIni);

                    Log("Writing SmartSteamEmu.ini");
                    emu.IniWriteValue("Launcher", "Target", exePath);
                    emu.IniWriteValue("Launcher", "StartIn", Path.GetDirectoryName(exePath));
                    emu.IniWriteValue("Launcher", "CommandLine", startArgs);
                    emu.IniWriteValue("Launcher", "SteamClientPath", Path.Combine(steamEmu, "SmartSteamEmu.dll"));
                    emu.IniWriteValue("Launcher", "SteamClientPath64", Path.Combine(steamEmu, "SmartSteamEmu64.dll"));
                    emu.IniWriteValue("Launcher", "InjectDll", "1");

                    emu.IniWriteValue("SmartSteamEmu", "AppId", context.SteamID);
                    emu.IniWriteValue("SmartSteamEmu", "SteamIdGeneration", "Manual");
                    emu.IniWriteValue("SmartSteamEmu", "ManualSteamId", (random_steam_id + i).ToString());

                    if (ini.IniReadValue("Misc", "UseNicksInGame") == "True" && !string.IsNullOrEmpty(player.Nickname))
                    {
                        emu.IniWriteValue("SmartSteamEmu", "PersonaName", player.Nickname);
                    }
                    else
                    {
                        emu.IniWriteValue("SmartSteamEmu", "PersonaName", "Player" + (i + 1));
                    }

                    emu.IniWriteValue("SmartSteamEmu", "DisableOverlay", "1");
                    emu.IniWriteValue("SmartSteamEmu", "SeparateStorageByName", "1");
                    
                    //string userName = $"Player { context.PlayerID }";

                    //emu.IniWriteValue("SmartSteamEmu", "PersonaName", userName);
                    //emu.IniWriteValue("SmartSteamEmu", "ManualSteamId", userName);

                    //emu.IniWriteValue("SmartSteamEmu", "Offline", "False");
                    //emu.IniWriteValue("SmartSteamEmu", "MasterServer", "");
                    //emu.IniWriteValue("SmartSteamEmu", "MasterServerGoldSrc", "");

                    //gen.SetupSse?.Invoke();

                    if (context.KillMutex?.Length > 0)
                    {
                        // to kill the mutexes we need to orphanize the process
                        proc = ProcessUtil.RunOrphanProcess(emuExe/*, gen.RunAsAdmin*/);
                        Log(string.Format("Started process {0} (pid {1}) as an orphan in order to kill mutexes in future", proc.ProcessName, proc.Id));
                    }
                    else
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.FileName = emuExe;
                        //if(gen.RunAsAdmin)
                        //{
                        //    startInfo.Verb = "runas";
                        //}
                        proc = Process.Start(emuExe);
                        Log(string.Format("Started process {0} (pid {1})", proc.ProcessName, proc.Id));
                    }

                    player.SteamEmu = true;
                    Log("SmartSteamEmu setup complete");
                }
                else
                {
                    if ((context.KillMutex?.Length > 0 || (gen.HookInit || gen.RenameNotKillMutex || gen.SetWindowHookStart || gen.BlockRawInput)) && !gen.CMDLaunch && !gen.UseForceBindIP) /*|| (gen.CMDLaunch && i==0))*/
                    {

                        string mu = "";
                        if(gen.RenameNotKillMutex && context.KillMutex?.Length > 0)
                        {
                            for (int m = 0; m < gen.KillMutex.Length; m++)
                            {
                                mu += gen.KillMutex[m];

                                if (m != gen.KillMutex.Length - 1)
                                {
                                    mu += "|==|";
                                }
                            }
                        }


                        
                        //string rawHID = player.DInputJoystick.Properties.InterfacePath;
                        //string frmtRawHidPartA = rawHID.Substring(0, rawHID.LastIndexOf("&")).ToUpper();
                        //string frmtRawHidPartB = rawHID.Substring(rawHID.LastIndexOf("&") + 1);
                        //string frmtRawHid = frmtRawHidPartA + frmtRawHidPartB;
                        //MessageBox.Show(frmtRawHid);

                        Log(string.Format("Launching game through StartGameUtil located at {0}", GetRelativePath(exePath, tempDir)));
                        proc = Process.GetProcessById(StartGameUtil.StartGame(
                            GetRelativePath(exePath, nucleusRootFolder), startArgs,
                            gen.HookInit, gen.HookInitDelay, gen.RenameNotKillMutex, mu, gen.SetWindowHookStart, isDebug, nucleusRootFolder, gen.BlockRawInput, /*gen.RunAsAdmin, rawHID,*/ GetRelativePath(linkFolder, nucleusRootFolder)));
                    }
                    else
                    {
                        if (gen.CMDLaunch /*&& i >= 1*/ || (gen.UseForceBindIP && i > 0))
                        {
                            string[] cmdOps = gen.CMDOptions;
                            Process cmd = new Process();
                            cmd.StartInfo.FileName = "cmd.exe";
                            cmd.StartInfo.RedirectStandardInput = true;
                            cmd.StartInfo.RedirectStandardOutput = true;
                            //cmd.StartInfo.CreateNoWindow = true;
                            cmd.StartInfo.UseShellExecute = false;
                            cmd.Start();

                            if(gen.CMDLaunch)
                            {
                                string cmdLine = cmdOps[i] + " \"" + exePath + "\" " + startArgs;
                                Log(string.Format("Launching game via command prompt with the following line: {0}", cmdLine));
                                cmd.StandardInput.WriteLine(cmdLine);
                            }
                            else
                            {
                                string forceBindexe = string.Empty;

                                if (gameIs64)
                                {
                                    forceBindexe = "ForceBindIP64.exe";
                                }
                                else //if (Is64Bit(exePath) == false)
                                {
                                    forceBindexe = "ForceBindIP.exe";
                                }
                                //else
                                //{
                                //    Log(string.Format("ERROR - Machine type {0} not implemented", GetDllMachineType(exePath)));
                                //    //using (StreamWriter writer = new StreamWriter("error-log.txt", true))
                                //    //{
                                //    //    writer.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]" + "Machine type: '{0}' not implemented.", GetDllMachineType(exePath));
                                //    //}
                                //}
                                string cmdLine = "\"" + Path.Combine(GameManager.Instance.GetUtilsPath(), "ForceBindIP\\" + forceBindexe) + "\" 127.0.0." + (i + 2) + " \"" + exePath + "\" " + startArgs;
                                Log(string.Format("Launching game using ForceBindIP command line argument: {0}", cmdLine));
                                cmd.StandardInput.WriteLine(cmdLine);
                            }
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
                            //if (gen.RunAsAdmin)
                            //{
                            //    startInfo.Verb = "runas";
                            //}
                            Log(string.Format("Launching game directly at {0}", GetRelativePath(exePath, nucleusRootFolder)));
                            proc = Process.Start(startInfo);
                        }

                    }

                    if (proc == null || gen.CMDLaunch  || gen.UseForceBindIP || gen.GameName == "Halo Custom Edition" /*|| gen.LauncherExe?.Length > 0*/)
                    {
                        //bool foundUnique = false;
                        for (int times = 0; times < 200; times++)
                        {
                            Thread.Sleep(50);
                            
                            string proceName = "";
                            if (gen.GameName == "Halo Custom Edition" /*|| gen.LauncherExe?.Length > 0*/)
                            {
                                Thread.Sleep(10000);
                                //if (gen.GameName == "Halo Custom Edition")
                                //{
                                //    proceName = "haloce";
                                //}
                                //else
                                //{
                                    proceName = Path.GetFileNameWithoutExtension(gen.ExecutableName).ToLower();
                                //}
                            }
                            else
                            {
                                proceName = Path.GetFileNameWithoutExtension(gen.ExecutableName).ToLower();
                            }
                            string launcherName = Path.GetFileNameWithoutExtension(gen.LauncherExe).ToLower();

                            if(!string.IsNullOrEmpty(launcherName))
                            {
                                Log(string.Format("Attempting to find game process {0}, or its launcher: {1}", proceName, launcherName));
                            }
                            else
                            {
                                Log(string.Format("Attempting to find game process {0}", proceName));
                            }
                            

                            Process[] procs = Process.GetProcesses();
                            for (int j = 0; j < procs.Length; j++)
                            {
                                Process p = procs[j];

                                string lowerP = p.ProcessName.ToLower();

                                if (lowerP == proceName || lowerP == launcherName)
                                {
                                    if (!attachedIds.Contains(p.Id))
                                    {
                                        //using (StreamWriter writer = new StreamWriter("important.txt", true))
                                        //{
                                        //    writer.WriteLine("Found process name: " + p.ProcessName + " pid: " + p.Id);
                                        //}
                                        Log(string.Format("Found process {0} (pid {1})", p.ProcessName, p.Id));
                                        attached.Add(p);
                                        attachedIds.Add(p.Id);
                                        proc = p;
                                        prevProcId = p.Id;
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
                        Log(string.Format("Obtained process {0} (pid {1})", proc.ProcessName, proc.Id));
                        attached.Add(proc);
                        attachedIds.Add(proc.Id);
                        //InjectDLLs(proc);
                    }
                }

                if (i > 0 && gen.ResetWindows && prevProcessData != null)
                {
                    Log("Attempting to repoisition, resize and strip borders for instance " + (i - 1));
                    //MessageBox.Show("Going to attempt to reposition and resize instance " + (i - 1));
                    try
                    {
                        prevProcessData.HWnd.Location = new Point(prevWindowX, prevWindowY);
                        prevProcessData.HWnd.Size = new Size(prevWindowWidth, prevWindowHeight);
                        uint lStyle = User32Interop.GetWindowLong(prevProcessData.HWnd.NativePtr, User32_WS.GWL_STYLE);
                        lStyle = lStyle & ~User32_WS.WS_CAPTION;
                        lStyle = lStyle & ~User32_WS.WS_THICKFRAME;
                        lStyle = lStyle & ~User32_WS.WS_MINIMIZE;
                        lStyle = lStyle & ~User32_WS.WS_MAXIMIZE;
                        lStyle = lStyle & ~User32_WS.WS_SYSMENU;
                        User32Interop.SetWindowLong(prevProcessData.HWnd.NativePtr, User32_WS.GWL_STYLE, lStyle);

                        lStyle = User32Interop.GetWindowLong(prevProcessData.HWnd.NativePtr, User32_WS.GWL_EXSTYLE);
                        lStyle = lStyle & ~User32_WS.WS_EX_DLGMODALFRAME;
                        lStyle = lStyle & ~User32_WS.WS_EX_CLIENTEDGE;
                        lStyle = lStyle & ~User32_WS.WS_EX_STATICEDGE;
                        User32Interop.SetWindowLong(prevProcessData.HWnd.NativePtr, User32_WS.GWL_EXSTYLE, lStyle);
                        User32Interop.SetWindowPos(prevProcessData.HWnd.NativePtr, IntPtr.Zero, 0, 0, 0, 0, (uint)(PositioningFlags.SWP_FRAMECHANGED | PositioningFlags.SWP_NOMOVE | PositioningFlags.SWP_NOSIZE | PositioningFlags.SWP_NOZORDER | PositioningFlags.SWP_NOOWNERZORDER));

                    }
                    catch (Exception ex)
                    {
                        Log("ERROR - ResetWindows was unsuccessful for instance " + (i - 1) + ". " + ex.Message);
                    }
                    if(prevProcessData.HWnd.Location != new Point(prevWindowX, prevWindowY) || prevProcessData.HWnd.Size != new Size(prevWindowWidth, prevWindowHeight))
                    {
                        Log("ERROR - ResetWindows was unsuccessful for instance " + (i - 1));
                    }

                }

                //using (StreamWriter writer = new StreamWriter("important.txt", true))
                //{
                //    writer.WriteLine("ProcessData process name: " + proc.ProcessName + " pid: " + proc.Id);
                //}
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
                    Log(string.Format("Setting process priority to {0}", gen.ProcessorPriorityClass));
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
                    Log(string.Format("Setting ideal processor to {0}", gen.IdealProcessor));
                    ProcessThreadCollection threads = proc.Threads;
                    for(int t = 0; t < threads.Count; t++)
                    {
                        threads[t].IdealProcessor = (gen.IdealProcessor + 1);
                    }
                }

                if(gen.UseProcessor?.Length > 0)
                {
                    Log(string.Format("Assigning processors {0}", gen.UseProcessor));
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
                                break;
                            }
                            //if (times == 199 && (int)proc.MainWindowHandle == 0)
                            //{
                                //Log(string.Format("ERROR - IdInWindowTitle could not find main window handle for {0} (pid {1})", proc.ProcessName, proc.Id));
                                //MessageBox.Show(string.Format("IdInWindowTitle: Could not find main window handle for {0} (pid:{1})", proc.ProcessName, proc.Id), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            //}
                        }
                    }
                    if ((int)proc.MainWindowHandle > 0)
                    {
                        string windowTitle = proc.MainWindowTitle + "(" + proc.Id + ")";
                        Log(string.Format("Setting window text to {0}", windowTitle));
                        SetWindowText(proc.MainWindowHandle, windowTitle);
                    }
                    else
                    {
                        Log(string.Format("ERROR - IdInWindowTitle could not find main window handle for {0} (pid {1})", proc.ProcessName, proc.Id));
                        MessageBox.Show(string.Format("IdInWindowTitle: Could not find main window handle for {0} (pid:{1})", proc.ProcessName, proc.Id), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                
                if (gen.PromptBetweenInstances && i < (players.Count - 1))
                {
                    Log(string.Format("Prompted user for Instance {0}", (i + 1)));
                    MessageBox.Show("Press OK when ready to launch instance " + (i + 1) + ".", "Waiting", MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                }
                else if (gen.PromptBetweenInstances && i == players.Count - 1 && (gen.HookFocus || gen.FakeFocus || gen.SetWindowHook || gen.HideCursor))
                {
                    Log("Prompted user to install focus hooks");
                    MessageBox.Show("Press OK when ready to install hooks and/or start sending fake messages.", "Waiting", MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    foreach (Process aproc in attached)
                    {
                        User32Interop.SetWindowPos(aproc.MainWindowHandle, new IntPtr(-1), 0, 0, 0, 0, (uint)(PositioningFlags.SWP_NOMOVE | PositioningFlags.SWP_NOSIZE | PositioningFlags.SWP_SHOWWINDOW));
                    }
                }
                else
                {
                    Log(string.Format("Pausing for {0} seconds", gen.PauseBetweenStarts));
                    Thread.Sleep(TimeSpan.FromSeconds(gen.PauseBetweenStarts));
                }

                if (i == (players.Count - 1)) // all instances accounted for
                {
                    if (gen.ResetWindows)
                    {

                        Log("Attempting to repoisition, resize and strip borders for instance " + i);
                        //prevProcessData.HWnd.Location = new Point(prevWindowX, prevWindowY);
                        //prevProcessData.HWnd.Size = new Size(prevWindowWidth, prevWindowHeight);
                        //uint lStyle = User32Interop.GetWindowLong(prevProcessData.HWnd.NativePtr, User32_WS.GWL_STYLE);
                        //lStyle = lStyle & ~User32_WS.WS_CAPTION;
                        //lStyle = lStyle & ~User32_WS.WS_THICKFRAME;
                        //lStyle = lStyle & ~User32_WS.WS_MINIMIZE;
                        //lStyle = lStyle & ~User32_WS.WS_MAXIMIZE;
                        //lStyle = lStyle & ~User32_WS.WS_SYSMENU;
                        //User32Interop.SetWindowLong(prevProcessData.HWnd.NativePtr, User32_WS.GWL_STYLE, lStyle);

                        //lStyle = User32Interop.GetWindowLong(prevProcessData.HWnd.NativePtr, User32_WS.GWL_EXSTYLE);
                        //lStyle = lStyle & ~User32_WS.WS_EX_DLGMODALFRAME;
                        //lStyle = lStyle & ~User32_WS.WS_EX_CLIENTEDGE;
                        //lStyle = lStyle & ~User32_WS.WS_EX_STATICEDGE;
                        //User32Interop.SetWindowLong(prevProcessData.HWnd.NativePtr, User32_WS.GWL_EXSTYLE, lStyle);
                        //User32Interop.SetWindowPos(prevProcessData.HWnd.NativePtr, IntPtr.Zero, 0, 0, 0, 0, (uint)(PositioningFlags.SWP_FRAMECHANGED | PositioningFlags.SWP_NOMOVE | PositioningFlags.SWP_NOSIZE | PositioningFlags.SWP_NOZORDER | PositioningFlags.SWP_NOOWNERZORDER));
                        try
                        {
                            data.HWnd.Location = new Point(prevWindowX, prevWindowY);
                            data.HWnd.Size = new Size(prevWindowWidth, prevWindowHeight);
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
                        }
                        catch(Exception ex)
                        {
                            Log("ERROR - ResetWindows was unsuccessful for instance " + i + ". " + ex.Message);
                        }
                    }


                    if (gen.FakeFocus)
                    {
                        Log("Start sending fake focus messages every 1000 ms");
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
                                        break;
                                    }
                                    //if (times == 199 && (int)aproc.MainWindowHandle == 0)
                                    //{
                                    //    Log(string.Format("ERROR - ChangeWindowTitle could not find main window handle for {0} (pid:{1})", aproc.ProcessName, aproc.Id));
                                    //    MessageBox.Show(string.Format("ChangeWindowTitle: Could not find main window handle for {0} (pid:{1})", aproc.ProcessName, aproc.Id), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    //}
                                }
                            }
                            if ((int)aproc.MainWindowHandle > 0)
                            {
                                Log(string.Format("Renaming window title {0} to {1} for pid {2}", aproc.MainWindowHandle, gen.Hook.ForceFocusWindowName, aproc.Id));
                                SetWindowText(aproc.MainWindowHandle, gen.Hook.ForceFocusWindowName);
                            }
                            else
                            {
                                Log(string.Format("ERROR - ChangeWindowTitle could not find main window handle for {0} (pid:{1})", aproc.ProcessName, aproc.Id));
                                MessageBox.Show(string.Format("ChangeWindowTitle: Could not find main window handle for {0} (pid:{1})", aproc.ProcessName, aproc.Id), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }

                    if (i > 0 && (gen.HookFocus || gen.SetWindowHook || gen.HideCursor || gen.PreventWindowDeactivation))
                    {
                        Log("Injecting hook DLL for last instance");
                        InjectDLLs(data.Process);
                    }
                    //if (gen.HookFocus || gen.SetWindowHook || gen.HideCursor)
                    //{
                    //    if (!data.Setted)
                    //    {
                    //        for (int times = 0; times < 200; times++)
                    //        {
                    //            Thread.Sleep(50);
                    //            if (data.Setted)
                    //            {
                    //                break;
                    //            }
                    //        }
                    //    }

                    //    List<int> piresult = new List<int>();
                    //    int pi = 0;
                    //    if (gen.HookFocusInstances?.Length > 0)
                    //    {
                    //        piresult = gen.HookFocusInstances.Split(',').Select(Int32.Parse).ToList();
                    //    }
                    //    foreach (Process aproc in attached)
                    //    {
                    //        if (gen.HookFocusInstances?.Length > 0)
                    //        {
                    //            if (piresult.Contains(pi))
                    //            {
                    //                InjectDLLs(aproc);
                    //            }

                    //            pi++;
                    //        }
                    //        else
                    //        {
                    //            InjectDLLs(aproc);
                    //        }
                    //        //using (StreamWriter writer = new StreamWriter("error-log.txt", true))
                    //        //{
                    //        //    writer.WriteLine("InjectDLLs: Entered for loop, going to inject process: {0} (pid:{1}), main window title: {2}", aproc.ProcessName, aproc.Id, aproc.MainWindowTitle);
                    //        //}
                    //        //aproc.Refresh();
                    //        //if (aproc == null || aproc.HasExited || aproc.MainWindowTitle.ToLower() == gen.LauncherTitle.ToLower() || aproc.MainModule.FileName == gen.LauncherExe)
                    //        //    continue;

                    //    }
                    //}

                    if (gen.SetForegroundWindowElsewhere)
                    {
                        Log("Setting the foreground window to Nucleus");
                        IntPtr nucHwnd = User32Interop.FindWindow(null, "Nucleus Coop (Alpha 8 Mod)");

                        if (nucHwnd != IntPtr.Zero)
                        {
                            Log("ERROR - Could not obtain the window handle for Nucleus");
                            User32Interop.SetForegroundWindow(nucHwnd);
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
                        Log(string.Format("ERROR - InjectDLLs could not find main window handle for {0} (pid {1})", proc.ProcessName, proc.Id));
                        //using (StreamWriter writer = new StreamWriter("error-log.txt", true))
                        //{
                        //    writer.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]" + "InjectDLLs: Could not find main window handle for {0} (pid:{1})", proc.ProcessName, proc.Id);
                        //}
                    }
                }
            }

            //bool is64 = EasyHook.RemoteHooking.IsX64Process(proc.Id);                 
            string currDir = Directory.GetCurrentDirectory();


            //using (StreamWriter writer = new StreamWriter("important.txt", true))
            //{
            //    writer.WriteLine("aproc id: {0}, aproc procname: {1}, title: {2}, handle: {3}, handleint: {4}, bytefour: {5}, byteeight: {6}, datatosend[8]: {7}, datatosend[9]: {8}, intptr: {9}", proc.Id, proc.ProcessName, proc.MainWindowTitle, proc.MainWindowHandle, (int)proc.MainWindowHandle, BitConverter.ToUInt32(dataToSend, 0), BitConverter.ToUInt64(dataToSend, 0), dataToSend[8], dataToSend[9], intPtr);
            //}

            try
            {
                if (gameIs64)
                {
                    Log("x64 game detected, injecting Nucleus.Hook64.dll");
                    try
                    {
                        string injectorPath = Path.Combine(currDir, "Nucleus.Inject64.exe");
                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.FileName = injectorPath;
                        object[] args = new object[]
                        {
                            1, proc.Id, 0, 0, null, Path.Combine(currDir, "Nucleus.Hook64.dll"), proc.MainWindowHandle, gen.HookFocus, gen.HideCursor, isDebug, nucleusFolderPath, gen.SetWindowHook, gen.PreventWindowDeactivation
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

                        //if(gen.RunAsAdmin)
                        //{
                        //    startInfo.Verb = "runas";
                        //}
                        //startInfo.Verb = "runas";
                        Process injectProc = Process.Start(startInfo);
                        injectProc.WaitForExit();
                    }
                    catch (Exception ex)
                    {
                        Log("ERROR - " + ex.Message);
                    }
                }
                else
                {
                    Log("x86 game detected, injecting Nucleus.Hook32.dll");
                    var logPath = Encoding.Unicode.GetBytes(nucleusFolderPath);
                    int logPathLength = logPath.Length;

                    int size = 21 + logPathLength;
                    IntPtr intPtr = Marshal.AllocHGlobal(size);
                    byte[] dataToSend = new byte[size];
                    dataToSend[0] = (byte)((int)proc.MainWindowHandle >> 24);
                    dataToSend[1] = (byte)((int)proc.MainWindowHandle >> 16);
                    dataToSend[2] = (byte)((int)proc.MainWindowHandle >> 8);
                    dataToSend[3] = (byte)((int)proc.MainWindowHandle);

                    dataToSend[4] = gen.PreventWindowDeactivation == true ? (byte)1 : (byte)0;
                    dataToSend[5] = gen.SetWindowHook == true ? (byte)1 : (byte)0;
                    dataToSend[6] = isDebug == true ? (byte)1 : (byte)0;
                    dataToSend[7] = gen.HideCursor == true ? (byte)1 : (byte)0;
                    dataToSend[8] = gen.HookFocus == true ? (byte)1 : (byte)0;

                    dataToSend[9] = (byte)(logPathLength >> 24);
                    dataToSend[10] = (byte)(logPathLength >> 16);
                    dataToSend[11] = (byte)(logPathLength >> 8);
                    dataToSend[12] = (byte)logPathLength;

                    Array.Copy(logPath, 0, dataToSend, 13, logPathLength);

                    Marshal.Copy(dataToSend, 0, intPtr, size);
                    NativeAPI.RhInjectLibrary(proc.Id, 0, 0, Path.Combine(currDir, "Nucleus.Hook32.dll"), null, intPtr, size);
                }
            }
            catch (Exception ex)
            {
                Log(string.Format("ERROR - {0}", ex.Message));
                //using (StreamWriter writer = new StreamWriter("error-log.txt", true))
                //{
                //    writer.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]" + "is64: {0}, ex msg: {1}, ex str: {2}", gameIs64, ex.Message, ex.ToString());
                //}
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

                            if(!gen.PromptBetweenInstances)
                            {
                                Log("Setting game window to top most");
                                data.HWnd.TopMost = true;
                            }
                            

                            if (data.Status == 2)
                            {

                                //using (StreamWriter writer = new StreamWriter("proc-test.txt", true))
                                //{
                                //    writer.WriteLine("state 2 data Hwnd: {0}, data process Id {1}, data processname: {2}, data process mainmodule {3}, data process mainwindowhandle: {4}", data.HWnd, data.Process.Id, data.Process.ProcessName, data.Process.MainModule, data.Process.MainWindowHandle);
                                //}
                                Log("Removing game window border for pid " + data.Process.Id);
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
                                    Log(string.Format("Repostioning game window for pid {0} to coords x:{1},y:{2}", data.Process.Id,data.Position.X,data.Position.Y));
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
                                            Log(string.Format("Resizing game window for pid {0} to the following width:{1}, height:{2}, aspectratio:{3}",data.Process.Id,newWidth,newHeight,origRatio));
                                            data.Size = new Size(newWidth, newHeight);
                                        }
                                    }
                                    else
                                    {
                                        Log(string.Format("Resizing game window for pid {0} to the following width:{1}, height:{2}",data.Process.Id, data.Size.Width, data.Size.Height));
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
                                                    //using (StreamWriter writer = new StreamWriter("important.txt", true))
                                                    //{
                                                    //    writer.WriteLine("GotLauncher found process name: " + pro.ProcessName + " pid: " + pro.Id);
                                                    //}
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
                                                //using (StreamWriter writer = new StreamWriter("important.txt", true))
                                                //{
                                                //    writer.WriteLine("!GotLauncher found process name: " + pro.ProcessName + " pid: " + pro.Id);
                                                //}
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
                    Log("Update method calling Handler End function");
                    End();
                }
            }
        }

        public void Log(StreamWriter writer)
        {
        }

        private void Log(string logMessage)
        {
            try
            {
                if (ini.IniReadValue("Misc", "DebugLog") == "True")
                {
                    using (StreamWriter writer = new StreamWriter("debug-log.txt", true))
                    {
                        writer.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}]HANDLER: {logMessage}");
                        writer.Close();
                    }
                }
            }
            catch { }
        }

        string CheckFor45PlusVersion(int releaseKey)
        {
            if (releaseKey >= 528040)
                return "4.8 or later";
            if (releaseKey >= 461808)
                return "4.7.2";
            if (releaseKey >= 461308)
                return "4.7.1";
            if (releaseKey >= 460798)
                return "4.7";
            if (releaseKey >= 394802)
                return "4.6.2";
            if (releaseKey >= 394254)
                return "4.6.1";
            if (releaseKey >= 393295)
                return "4.6";
            if (releaseKey >= 379893)
                return "4.5.2";
            if (releaseKey >= 378675)
                return "4.5.1";
            if (releaseKey >= 378389)
                return "4.5";
            // This code should never execute. A non-null release key should mean
            // that 4.5 or later is installed.
            return "No 4.5 or later version detected";
        }
    }
}