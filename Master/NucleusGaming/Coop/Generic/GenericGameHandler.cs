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
//using EasyHook;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using Microsoft.Win32;
using System.Management;
using Nucleus.Gaming.Coop.InputManagement;
using System.Threading.Tasks;
using Nucleus.Gaming.Coop.BasicTypes;
using Nucleus.Gaming.Coop.InputManagement.Logging;
using System.Collections;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Xml;
using System.Net;

namespace Nucleus.Gaming
{
    public class GenericGameHandler : IGameHandler, ILogNode
    {
        private const float HWndInterval = 10000;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_MAXIMIZE = 3;
        private const int SW_MINIMIZE = 6;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            //ref SECURITY_ATTRIBUTES lpProcessAttributes,
            //ref SECURITY_ATTRIBUTES lpThreadAttributes,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            //IntPtr lpStartupInfo,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        public enum ProcessCreationFlags : uint
        {
            ZERO_FLAG = 0x00000000,
            CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
            CREATE_DEFAULT_ERROR_MODE = 0x04000000,
            CREATE_NEW_CONSOLE = 0x00000010,
            CREATE_NEW_PROCESS_GROUP = 0x00000200,
            CREATE_NO_WINDOW = 0x08000000,
            CREATE_PROTECTED_PROCESS = 0x00040000,
            CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
            CREATE_SEPARATE_WOW_VDM = 0x00001000,
            CREATE_SHARED_WOW_VDM = 0x00001000,
            CREATE_SUSPENDED = 0x00000004,
            CREATE_UNICODE_ENVIRONMENT = 0x00000400,
            DEBUG_ONLY_THIS_PROCESS = 0x00000002,
            DEBUG_PROCESS = 0x00000001,
            DETACHED_PROCESS = 0x00000008,
            EXTENDED_STARTUPINFO_PRESENT = 0x00080000,
            INHERIT_PARENT_AFFINITY = 0x00010000
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT Rect);

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
        private int kbi = 1;

        private long random_steam_id = 76561199023125438;

        private string nucleusFolderPath;

        private UserGameInfo userGame;
        private GameProfile profile;
        private GenericGameInfo gen;
        private Dictionary<string, string> jsData;
        private GenericContext context;

		//private RawInputProcessor rawInputProcessor;

		//private List<Window> gameWindows;

        private double timer;
        private int exited;
        private List<Process> attached = new List<Process>();
        private List<int> attachedIds = new List<int>();

        private List<Process> attachedLaunchers = new List<Process>();
        private List<int> attachedIdsLaunchers = new List<int>();

        protected bool hasEnded;
        protected double timerInterval = 1000;

        public event Action Ended;
        private CursorModule _cursorModule;

        private bool hasKeyboardPlayer = false;
        private string keyboardInstance;
        private int keyboardProcId;

        private Thread fakeFocus;

        private bool symlinkNeeded;

        private int numPlayers = 0;

        private bool dllResize = false;
        private bool dllRepos = false;

        private string exePath;
        //public string UserProfileConfigPath;
        //public string UserProfileSavePath;
        private string instanceExeFolder;
        private string garch;

        private static string lobbyConnectArg;
        private static bool readToEnd = false;

        private string currentIPaddress;
        private string currentSubnetMask;
        private bool isDHCPenabled;
        private bool isDynamicDns;
        private string currentGateway;
        private int hostAddr = 169;
        private List<string> dnsAddresses = new List<string>();
        string dnsServersStr;
        private string iniNetworkInterface;
        private bool isPrevent;

        private List<string> addedFiles = new List<string>();

        private readonly IniFile ini = new Gaming.IniFile(Path.Combine(Directory.GetCurrentDirectory(), "Settings.ini"));
        bool isDebug;

        UserScreen owner;

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


        //[DllImport("EasyHook32.dll", CharSet = CharSet.Ansi)]
        //public static extern int RhCreateAndInject(
        //    [MarshalAsAttribute(UnmanagedType.LPWStr)] string InEXEPath,
        //    [MarshalAsAttribute(UnmanagedType.LPWStr)] string InCommandLine,
        //    int InProcessCreationFlags,
        //    int InInjectionOptions,
        //    [MarshalAsAttribute(UnmanagedType.LPWStr)] string InLibraryPath_x86,
        //    [MarshalAsAttribute(UnmanagedType.LPWStr)] string InLibraryPath_x64,
        //    IntPtr InPassThruBuffer,
        //    int InPassThruSize,
        //    IntPtr OutProcessId //Pointer to a UINT (the PID of the new process)
        //    );

        public Thread FakeFocus
        {
            get { return fakeFocus; }
        }

        private enum FocusMessages
        {
            WM_ACTIVATEAPP = 0x001C,
            WM_ACTIVATE = 0x0006,
            WM_NCACTIVATE = 0x0086,
            WM_SETFOCUS = 0x0007,
            WM_MOUSEACTIVATE = 0x0021,
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

                List<string> addtlProcsToKill = new List<string>();
                if (gen.KillProcessesOnClose?.Length > 0)
                {
                    addtlProcsToKill = gen.KillProcessesOnClose.ToList();
                }

                foreach (Process proc in procs)
                {
                    try
                    {
                        if ((gen.LauncherExe != null && proc.ProcessName.ToLower() == Path.GetFileNameWithoutExtension(gen.LauncherExe.ToLower())) || addtlProcsToKill.Contains(proc.ProcessName, StringComparer.OrdinalIgnoreCase) || proc.ProcessName.ToLower() == Path.GetFileNameWithoutExtension(gen.ExecutableName.ToLower()) || (proc.Id != 0 && attachedIds.Contains(proc.Id)) || (gen.Hook.ForceFocusWindowName != "" && proc.MainWindowTitle == gen.Hook.ForceFocusWindowName))
                        {
                            Log(string.Format("Killing process {0} (pid {1})", proc.ProcessName, proc.Id));
                            proc.Kill();
                        }
                        
                    }
                    catch (Exception ex)
                    {
                        Log(ex.InnerException + " " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Log(ex.InnerException + " " + ex.Message);
            }
        }

        public void End(bool fromStopButton)
        {
	        if (fromStopButton && LockInput.IsLocked)
	        {
				//TODO: For some reason the Stop button is clicked during split screen. Temporary fix is to not end if input is locked.
		        Log("IGNORING SHUTDOWN BECAUSE INPUT LOCKED");
		        return;
	        }

            Log("----------------- SHUTTING DOWN -----------------");
			if (fakeFocus != null && fakeFocus.IsAlive)
            {
                fakeFocus.Abort();
            }

            if(gen.ChangeIPPerInstance)
            {
                Log("Reverting IP settings back to normal");
                MessageBox.Show("Reverting IP settings back to normal. You may receive another prompt to action it.", "Nucleus - Change IP Per Instance");
                if(isDHCPenabled)
                {
                    SetDHCP(iniNetworkInterface);
                }
                else
                {
                    SetIP(iniNetworkInterface, currentIPaddress, currentSubnetMask, currentGateway);
                }

                //if (isDynamicDns)
                //{
                //    SetDNS(iniNetworkInterface, true);
                //}
            }

            if(gen.FlawlessWidescreen?.Length > 0)
            {
                Process[] runnProcs = Process.GetProcesses();
                foreach (Process proc in runnProcs)
                {
                    if (proc.ProcessName.ToLower() == "flawlesswidescreen")
                    {
                        Log("Killing Flawless Widescreen app");
                        proc.Kill();
                    }
                }
            }

            User32Util.ShowTaskBar();

            hasEnded = true;
            GameManager.Instance.ExecuteBackup(this.userGame.Game);

            LogManager.UnregisterForLogCallback(this);

            foreach (var window in RawInputManager.windows)
            {
				window.HookPipe?.Close();
            }

            Cursor.Clip = Rectangle.Empty; // guarantee were not clipping anymore
            string backupDir = GameManager.Instance.GempTempFolder(this.userGame.Game);
            ForceFinish();

            if (_cursorModule != null)
                _cursorModule.Stop();

            Thread.Sleep(1000);
            // delete symlink folder

            int tempIndex = 0;

			RawInputManager.EndSplitScreen();

#if RELEASE
            if (!gen.SymlinkGame && !gen.HardlinkGame && !gen.HardcopyGame)
            {
                Log("Deleting any files Nucleus added to original game folder");
                foreach (string addedFilePath in addedFiles)
                {
                    File.Delete(addedFilePath);
                }
            }

            if (gen.KeepSymLinkOnExit == false)
            {
                if(gen.SymlinkGame || gen.HardlinkGame || gen.HardcopyGame)
                {
                    for (int i = 0; i < profile.PlayerData.Count; i++)
                    {
                        tempIndex = i;
                        string linkFolder = Path.Combine(backupDir, "Instance" + i);
                        Log(string.Format("Deleting folder {0} and all of its contents", linkFolder));
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
                            if (Directory.Exists(linkFolder))
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
            }
#endif

            Ended?.Invoke();
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
//#if DEBUG
//			isDebug = true;
//#else
//	        isDebug = false;
//#endif
            if (ini.IniReadValue("Misc", "DebugLog") == "True")
            {
                isDebug = true;
            }


            iniNetworkInterface = ini.IniReadValue("Misc", "Network");

            List<PlayerInfo> players = profile.PlayerData;

            gen = game.Game as GenericGameInfo;
            // see if we have any save game to backup
            if (gen == null)
            {
                // you fucked up
                return false;
            }

            RawInputProcessor.CurrentGameInfo = gen;

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
            catch (Exception ex)
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

        //private static string GetRootFolder(string path)
        //{
        //    if (string.IsNullOrEmpty(path))
        //    {
        //        return path;
        //    }

        //    int failsafe = 20;
        //    for (; ; )
        //    {
        //        failsafe--;
        //        if (failsafe < 0)
        //        {
        //            break;
        //        }

        //        string temp = Path.GetDirectoryName(path);
        //        if (String.IsNullOrEmpty(temp))
        //        {
        //            break;
        //        }
        //        path = temp;
        //    }
        //    return path;
        //}

        private static string internalGetRelativePath(DirectoryInfo dirInfo, DirectoryInfo rootInfo, string str)
        {
            if (dirInfo == null || dirInfo.FullName == rootInfo.FullName)
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

        //public string GetRelativePath(string dirPath, string rootFolder)
        //{
        //    DirectoryInfo dirInfo = new DirectoryInfo(dirPath);
        //    DirectoryInfo rootInfo = new DirectoryInfo(rootFolder);
        //    return internalGetRelativePath(dirInfo, rootInfo, "");
        //}

        public string Play()
        {
            //bool gameIs64 = false;
            garch = "x86";
            if (Is64Bit(userGame.ExePath) == true)
            {
                gameIs64 = true;
                garch = "x64";
            }

            if (isDebug)
            {
                Log("--------------------- START ---------------------");
                Log(string.Format("Game: {0}, Arch: {1}, Executable: {2}, Launcher: {3}, SteamID: {4}, Script: {5}, Content Folder: {6}", gen.GameName, garch, gen.ExecutableName, gen.LauncherExe, gen.SteamID, gen.JsFileName, gen.GUID));

                if (string.IsNullOrEmpty(gen.StartArguments))
                {
                    Log("Game has no starting arguments");
                }
                else
                {
                    Log("Starting arguments: " + gen.StartArguments);
                }
                Log(string.Format("Utils - UseGoldberg: {0}, NeedsSteamEmulation: {1}, UseX360ce: {2}, UseDevReorder: {3}, UseDirectX9Wrapper: {4}, UseSteamStubDRMPatcher: {5}", gen.UseGoldberg, gen.NeedsSteamEmulation, gen.UseX360ce, gen.UseDevReorder, gen.UseDirectX9Wrapper, gen.UseSteamStubDRMPatcher));
                Log(string.Format("Options - UseNucleusEnvironment: {0}, ThirdPartyLaunch: {1}, SetForegroundWindowElsewhere: {2}, SymlinkFolders: {3}", gen.UseNucleusEnvironment, gen.ThirdPartyLaunch, gen.SetForegroundWindowElsewhere, gen.SymlinkFolders));
                Log(string.Format("Goldberg Settings - GoldbergNeedSteamInterface: {0}, GoldbergExperimental: {1}, GoldbergIgnoreSteamAppId: {2}, CreateSteamAppIdByExe: {3}, GoldbergLobbyConnect: {4}, GoldbergNoLocalSave: {5}", gen.GoldbergNeedSteamInterface, gen.GoldbergExperimental, gen.GoldbergIgnoreSteamAppId, gen.CreateSteamAppIdByExe, gen.GoldbergLobbyConnect, gen.GoldbergNoLocalSave));
                Log(string.Format("Start-up Hooks - HookInit: {0}, RenameNotKillMutex: {1}, SetWindowHookStart: {2}, BlockRawInput: {3}, CreateSingleDeviceFile: {4}", gen.HookInit, gen.RenameNotKillMutex, gen.SetWindowHookStart, gen.BlockRawInput, gen.CreateSingleDeviceFile));
                Log(string.Format("Post Hooks - SetWindowHook: {0}, HookFocus: {1}, HideCursor: {2}, PreventWindowDeactivation: {3}", gen.SetWindowHook, gen.HookFocus, gen.HideCursor, gen.PreventWindowDeactivation));

                if (gen.KillMutex?.Length > 0)
                {
                    string mutexList = string.Join(",", gen.KillMutex);
                    Log(string.Format("Mutexes - Handle(s): ({0}), KillMutexDelay: {1}, KillMutexType: {2}, RenameNotKillMutex: {3}, PartialMutexSearch: {4}", mutexList, gen.KillMutexDelay, gen.KillMutexType, gen.RenameNotKillMutex, gen.PartialMutexSearch));
                }

                Log("NucleusCoop mod version: 0.9.9.9 f1");
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

			//Merge raw keyboard/mouse players into one
			var groupWindows = players.Where(x => x.IsRawKeyboard || x.IsRawMouse).GroupBy(x => x.MonitorBounds).ToList();
			foreach(var group in groupWindows)
			{
				var firstInGroup = group.First();
				firstInGroup.IsRawKeyboard = group.Count(x => x.IsRawKeyboard) > 0;
				firstInGroup.IsRawMouse = group.Count(x => x.IsRawMouse) > 0;

				if (firstInGroup.IsRawKeyboard) firstInGroup.RawKeyboardDeviceHandle = group.First(x => x.RawKeyboardDeviceHandle != (IntPtr)(-1)).RawKeyboardDeviceHandle;
				if (firstInGroup.IsRawMouse) firstInGroup.RawMouseDeviceHandle = group.First(x => x.RawMouseDeviceHandle != (IntPtr)(-1)).RawMouseDeviceHandle;

				foreach(var x in group)
				{
					players.Remove(x);
				}

				players.Add(firstInGroup);
			}


            for (int i = 0; i < players.Count; i++)
            {
                players[i].PlayerID = i;
            }

            UserScreen[] all = ScreensUtil.AllScreens();

            Log(string.Format("Display - DPIHandling: {0}, DPI Scale: {1}, KeepAspectRatio: {2}, KeepMonitorAspectRatio: {3}, ResetWindows: {4}", gen.DPIHandling, DPIManager.Scale, gen.KeepAspectRatio, gen.KeepMonitorAspectRatio, gen.ResetWindows));
            for (int x=0; x < all.Length; x++)
            {
                Log(string.Format("Monitor {0} - Resolution: {1}", x, all[x].MonitorBounds.Width + "x" + all[x].MonitorBounds.Height));
            }

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

			RawInputProcessor.ToggleLockInputKey = gen.LockInputToggleKey;

			RawInputManager.windows.Clear();
			Window nextWindowToInject = null;

            numPlayers = players.Count;
            Log(string.Format("Number of players: {0}", numPlayers));

            for (int i = 0; i < players.Count; i++)
            {
                Log("********** Setting up player " + (i + 1) + " **********");
                PlayerInfo player = players[i];

                if (ini.IniReadValue("Misc", "UseNicksInGame") == "True")
                {
                    if (!player.IsKeyboardPlayer || (player.IsKeyboardPlayer && player.IsRawKeyboard))
                    {
                        if (ini.IniReadValue("ControllerMapping", player.HIDDeviceID) == "")
                        {
                            player.Nickname = "Player" + (i + 1);
                        }
                        else
                        {
                            player.Nickname = ini.IniReadValue("ControllerMapping", player.HIDDeviceID);
                        }
                    }
                    else
                    {
                        keyboardInstance = i.ToString();
                        if (ini.IniReadValue("ControllerMapping", "Keyboard") == "")
                        {
                            player.Nickname = "Player" + (i + 1);
                        }
                        else
                        {
                            player.Nickname = ini.IniReadValue("ControllerMapping", "Keyboard");
                        }
                    }
                }
                else
                {
                    player.Nickname = "Player" + (i + 1);
                }


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
				
                if (i > 0 && (gen.KillMutexLauncher?.Length > 0))
                {
                    for (; ; )
                    {
                        if (exited > 0)
                        {
                            return "";
                        }
                        Thread.Sleep(1000);

                        if (gen.KillMutexLauncher != null)
                        {
                            if (gen.KillMutexLauncher.Length > 0)
                            {
                                if (gen.KillMutexDelayLauncher > 0)
                                {
                                    Thread.Sleep((gen.KillMutexDelayLauncher * 1000));
                                }

                                Process[] currentProcs = Process.GetProcesses();
                                Process launcher = null;
                                foreach (Process currentProc in currentProcs)
                                {
                                    if (currentProc.ProcessName.ToLower() == Path.GetFileNameWithoutExtension(gen.LauncherExe).ToLower())
                                    {
                                        if (!attachedIdsLaunchers.Contains(currentProc.Id))
                                        {
                                            launcher = currentProc;
                                            attachedIdsLaunchers.Add(currentProc.Id);
                                            attachedLaunchers.Add(currentProc);
                                        }
                                    }
                                }

                                if (launcher == null)
                                {
                                    Log("Could not find launcher process to kill mutexes");
                                    break;
                                }

                                if (StartGameUtil.MutexExists(launcher, gen.KillMutexTypeLauncher, gen.PartialMutexSearchLauncher, gen.KillMutexLauncher))
                                {
                                    // mutexes still exist, must kill
                                    StartGameUtil.KillMutex(launcher, gen.KillMutexTypeLauncher, gen.PartialMutexSearchLauncher, gen.KillMutexLauncher);
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

                if (!gen.RenameNotKillMutex && i > 0 && (gen.KillMutex?.Length > 0 || !hasSetted))
                {
                    PlayerInfo before = players[i - 1];
                    for (; ; )
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

                                if (gen.KillMutexDelay > 0)
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

                if (i > 0 && (gen.HookFocus || gen.SetWindowHook || gen.HideCursor || gen.PreventWindowDeactivation || gen.SupportsMultipleKeyboardsAndMice))
                {
                    if(gen.PreventWindowDeactivation && !isPrevent)
                    {
                        Log("PreventWindowDeactivation detected, setting flag");
                        isPrevent = true;
                    }

                    if(isPrevent)
                    {
                        if (players[i - 1].IsKeyboardPlayer && gen.KeyboardPlayerSkipPreventWindowDeactivate)
                        {
                            Log("Ignoring PreventWindowDeactivation for keyboard player");
                            gen.PreventWindowDeactivation = false;
                        }
                        else
                        {
                            Log("Keeping PreventWindowDeactivation on");
                            gen.PreventWindowDeactivation = true;
                        }
                    }

                    if (gen.PostHookInstances?.Length > 0)
                    {
                        string[] instancesToHook = gen.PostHookInstances.Split(',');
                        foreach (string instanceToHook in instancesToHook)
                        {
                            if (int.Parse(instanceToHook) == i)
                            {
                                Log("Injecting hook DLL for previous instance");
                                PlayerInfo before = players[i - 1];
                                Thread.Sleep(1000);
                                ProcessData pdata = before.ProcessData;
                                User32Interop.SetForegroundWindow(pdata.Process.MainWindowHandle);
                                InjectDLLs(pdata.Process, nextWindowToInject);
							}
						}
                    }
                    else
                    {
                        Log("Injecting hook DLL for previous instance");
                        PlayerInfo before = players[i - 1];
                        Thread.Sleep(1000);
                        ProcessData pdata = before.ProcessData;
                        User32Interop.SetForegroundWindow(pdata.Process.MainWindowHandle);
                        InjectDLLs(pdata.Process, nextWindowToInject);
					}
				}

				Rectangle playerBounds = player.MonitorBounds;
                owner = player.Owner;

                int width = playerBounds.Width;
                int height = playerBounds.Height;
                Log("Player monitor's resolution: " + owner.display.Width + "x" + owner.display.Height);
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
                        Log("Symlinking " + gen.SymlinkFiles.Length + " files in Game.SymlinkFiles");
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
                        Log("Copying " + gen.CopyFiles.Length + " files in Game.CopyFiles");
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
                        Log("Game executable (" + gen.ExecutableName + ") will be copied and not symlinked");
                        fileCopies.Add(gen.ExecutableName.ToLower());
                        if (gen.LauncherExe?.Length > 0 && !gen.LauncherExe.Contains(':'))
                        {
                            Log("Launcher executable (" + gen.LauncherExe + ") will be copied and not symlinked");
                            fileCopies.Add(gen.LauncherExe.ToLower());
                        }
                    }

                    // additional ignored files by the generic info
                    if (gen.FileSymlinkExclusions != null)
                    {
                        Log(gen.FileSymlinkExclusions.Length + " Files in Game.FileSymlinkExclusions will not be symlinked");
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
                        Log(gen.FileSymlinkCopyInstead.Length + " Files in Game.FileSymlinkCopyInstead will be copied instead of symlinked");
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
                        Log(gen.DirSymlinkExclusions.Length + " Directories in Game.DirSymlinkExclusions will be ignored");
                        string[] symlinkExclusions = gen.DirSymlinkExclusions;
                        for (int k = 0; k < symlinkExclusions.Length; k++)
                        {
                            string s = symlinkExclusions[k];
                            // make sure it's lower case
                            dirExclusions.Add(s.ToLower());
                        }
                    }
                    if (gen.DirExclusions != null)
                    {
                        Log(gen.DirExclusions.Length + " Directories (and their contents) in Game.DirExclusions will be skipped");
                        string[] skipExclusions = gen.DirExclusions;
                        for (int k = 0; k < skipExclusions.Length; k++)
                        {
                            string s = skipExclusions[k];
                            // make sure it's lower case
                            dirExclusions.Add("direxskip" + s.ToLower());
                        }
                    }
                    if (gen.DirSymlinkCopyInstead != null)
                    {
                        Log(gen.DirSymlinkCopyInstead.Length + " directories and all its contents in Game.DirSymlinkCopyInstead will be copied instead of symlinked");
                        string[] dirSymlinkCopyInstead = gen.DirSymlinkCopyInstead;
                        for (int k = 0; k < dirSymlinkCopyInstead.Length; k++)
                        {
                            string[] files = Directory.GetFiles(Path.Combine(rootFolder, dirSymlinkCopyInstead[k]), "*", SearchOption.TopDirectoryOnly);

                            foreach (string s in files)
                            {
                                fileCopies.Add(Path.GetFileName(s).ToLower());
                            }

                            //string s = dirSymlinkCopyInstead[k];
                            // make sure it's lower case
                            //fileCopies.Add(s.ToLower());
                        }
                    }

                    string[] fileExclusionsArr = fileExclusions.ToArray();
                    string[] fileCopiesArr = fileCopies.ToArray();


                    if (i == 0)
                    {
                        for (int p = 0; p < players.Count; p++)
                        {
                            string path = Path.Combine(tempDir, "Instance" + p);
                            if (!Directory.Exists(path) || Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any<string>())
                            {
                                symlinkNeeded = true;
                            }
                        }
                    }

                    bool skipped = false;
                    if (gen.ForceSymlink || !gen.KeepSymLinkOnExit || symlinkNeeded)
                    {
                        if (gen.ForceSymlink)
                        {
                            Log("Forcing symlink/copy");
                        }

                        if (gen.HardcopyGame)
                        {
                            Log(string.Format("Copying game folder {0} to {1} ", rootFolder, linkFolder));
                            // copy the directory
                            int exitCode;
                            FileUtil.CopyDirectory(rootFolder, new DirectoryInfo(rootFolder), linkFolder, out exitCode, dirExclusions.ToArray(), fileExclusionsArr, true);
                        }
                        else if (gen.HardlinkGame)
                        {
                            if (i == 0)
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
                            if (i == 0)
                            {
                                Log(string.Format("Symlinking game folder and files at {0} to {1}, for each instance", rootFolder, tempDir));
                                int exitCode;
                                //CmdUtil.LinkDirectory(rootFolder, new DirectoryInfo(rootFolder), linkFolder, out exitCode, dirExclusions.ToArray(), fileExclusionsArr, fileCopiesArr, true, true);
                                //Nucleus.Gaming.Platform.Windows.IO.WinDirectoryUtil.LinkDirectory(rootFolder, new DirectoryInfo(rootFolder), linkFolder, out exitCode, dirExclusions.ToArray(), fileExclusionsArr, fileCopiesArr, true);
                                while (!StartGameUtil.SymlinkGame(rootFolder, linkFolder, out exitCode, dirExclusions.ToArray(), fileExclusionsArr, fileCopiesArr, false, gen.SymlinkFolders, players.Count))
                                {
                                    Thread.Sleep(25);
                                }

                                //if (!gen.SymlinkExe)
                                //{
                                //File.Copy(userGame.ExePath, exePath, true);
                                //}
                            }
                        }
                    }
                    else
                    {
                        skipped = true;
                        Log("Skipping linking or copying files as it is not needed");
                    }

                    if (gen.LauncherExe?.Length > 0)
                    {
                        if(gen.LauncherExe.Contains(':') && gen.LauncherExe.Contains('\\'))
                        {
                            exePath = gen.LauncherExe;
                        }
                        else
                        {
                            string[] launcherFiles = Directory.GetFiles(linkFolder, gen.LauncherExe, SearchOption.AllDirectories);
                            if (launcherFiles.Length < 1)
                            {
                                Log("ERROR - Could not find " + gen.LauncherExe + " in instance folder, Game executable will be used instead; " + exePath);
                            }
                            else if (launcherFiles.Length == 1)
                            {
                                exePath = launcherFiles[0];
                                Log("Found launcher exe at " + exePath + ". This will be used to launch the game");
                            }
                            else
                            {
                                exePath = launcherFiles[0];
                                Log("Multiple " + gen.LauncherExe + "'s found in instance folder." + " Using " + exePath + " to launch the game");
                            }
                        }
                    }

                    if (!skipped)
                    {
                        Log("File operations complete");
                    }
                }
                else
                {
                    exePath = userGame.ExePath;

                    linkBinFolder = workingFolder;
                    linkFolder = rootFolder;

                    //nucleusRootFolder = Directory.GetParent(rootFolder).FullName;
                    //nucleusRootFolder = rootFolder;

                    instanceExeFolder = linkBinFolder;
                    origRootFolder = rootFolder;


                    if (gen.LauncherExe?.Length > 0)
                    {
                        if (gen.LauncherExe.Contains(':') && gen.LauncherExe.Contains('\\'))
                        {
                            exePath = gen.LauncherExe;
                        }
                        else
                        {
                            string[] launcherFiles = Directory.GetFiles(linkFolder, gen.LauncherExe, SearchOption.AllDirectories);
                            if (launcherFiles.Length < 1)
                            {
                                Log("ERROR - Could not find " + gen.LauncherExe + " in instance folder, Game executable will be used instead; " + exePath);
                            }
                            else if (launcherFiles.Length == 1)
                            {
                                exePath = launcherFiles[0];
                                Log("Found launcher exe at " + exePath + ". This will be used to launch the game");
                            }
                            else
                            {
                                exePath = launcherFiles[0];
                                Log("Multiple " + gen.LauncherExe + "'s found in instance folder." + " Using " + exePath + " to launch the game");
                            }
                        }
                    }
                }

                if ((gen.UserProfileConfigPath?.Length > 0 || gen.ForceUserProfileConfigCopy) && gen.UseNucleusEnvironment)
                {
                    UserProfileConfigCopy(player);
                }

                if ((gen.UserProfileSavePath?.Length > 0 || gen.ForceUserProfileSaveCopy) && gen.UseNucleusEnvironment)
                {
                    UserProfileSaveCopy(player);
                }

                if(gen.DeleteFilesInConfigPath?.Length > 0)
                {
                    string path = Path.Combine($@"C:\Users\{Environment.UserName}\NucleusCoop\{player.Nickname}\", gen.UserProfileConfigPath);
                    //string realConfigPath = Path.Combine(Environment.GetEnvironmentVariable("userprofile"), gen.UserProfileConfigPath);
                    foreach(string fileName in gen.DeleteFilesInConfigPath)
                    {
                        string[] foundFiles = Directory.GetFiles(path, fileName, SearchOption.AllDirectories);
                        foreach(string foundFile in foundFiles)
                        {
                            if(!gen.IgnoreDeleteFilesPrompt)
                            {
                                DialogResult dialogResult = MessageBox.Show("Are you sure you want to delete '" + foundFile + "'?", "Nucleus - Delete Files In Config Path", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                                if (dialogResult != DialogResult.Yes)
                                {
                                    continue;
                                }
                            }
                            Log(string.Format("Deleting file {0}", foundFile));
                            File.Delete(foundFile);
                        }
                    }
                }

                if (gen.DeleteFilesInSavePath?.Length > 0)
                {
                    string path = Path.Combine($@"C:\Users\{Environment.UserName}\NucleusCoop\{player.Nickname}\", gen.UserProfileConfigPath);
                    //string realConfigPath = Path.Combine(Environment.GetEnvironmentVariable("userprofile"), gen.UserProfileSavePath);
                    foreach (string fileName in gen.DeleteFilesInSavePath)
                    {
                        string[] foundFiles = Directory.GetFiles(path, fileName, SearchOption.AllDirectories);
                        foreach (string foundFile in foundFiles)
                        {
                            if (!gen.IgnoreDeleteFilesPrompt)
                            {
                                DialogResult dialogResult = MessageBox.Show("Are you sure you want to delete '" + foundFile + "'?", "Nucleus - Delete Files In Save Path", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                                if (dialogResult != DialogResult.Yes)
                                {
                                    continue;
                                }
                            }
                            Log(string.Format("Deleting file {0}", foundFile));
                            File.Delete(foundFile);
                        }
                    }
                }

                if (gen.ChangeExe)
                {
                    ChangeExe(i);
                }

                if(gen.RenameAndOrMoveFiles?.Length > 0)
                {
                    RenameOrMoveFiles(linkFolder, i);
                }

                if(gen.DeleteFiles?.Length > 0)
                {
                    DeleteFiles(linkFolder, i);
                }

                context = gen.CreateContext(profile, player, this, hasKeyboardPlayer);
                context.PlayerID = player.PlayerID;
                context.IsFullscreen = isFullscreen;

                context.ExePath = exePath;
                context.RootInstallFolder = exeFolder;
                context.RootFolder = linkFolder;
                context.UserProfileConfigPath = gen.UserProfileConfigPath;
                context.UserProfileSavePath = gen.UserProfileSavePath;

                bool setupDll = true;
                if (!gen.SymlinkGame && !gen.HardlinkGame && !gen.HardcopyGame && i > 0)
                {
                    setupDll = false;
                }

                if (gen.HexEditExeAddress?.Length > 0)
                {
                    HexEditExeAddress(exePath, i);
                }

                if (gen.HexEditFileAddress?.Length > 0)
                {
                    HexEditFileAddress(linkFolder, i);
                }

                if (gen.HexEditAllExes?.Length > 0)
                {
                    HexEditAllExes(context, i);
                }

                if (gen.HexEditExe?.Length > 0)
                {
                    HexEditExe(context, i);
                }

                if (gen.HexEditAllFiles?.Length > 0)
                {
                    HexEditAllFiles(context, linkFolder);
                }

                if (gen.HexEditFile?.Length > 0)
                {
                    HexEditFile(context, i, linkFolder);
                }

                if (gen.UseSteamStubDRMPatcher)
                {
                    UseSteamStubDRMPatcher(garch);
                }

                if (gen.UseGoldberg)
                {
                    UseGoldberg(rootFolder, nucleusRootFolder, linkFolder, i, player, players);
                }

                if (gen.CreateSteamAppIdByExe)
                {
                    CreateSteamAppIdByExe();
                }

                if (gen.XInputPlusDll?.Length > 0 && !gen.ProcessChangesAtEnd)
                {
                    SetupXInputPlusDll(garch, player, context, i, setupDll);
                }

                if (gen.UseDevReorder && !gen.ProcessChangesAtEnd)
                {
                    UseDevReorder(garch, player, players, i, setupDll);
                }

                if (gen.UseX360ce && !gen.ProcessChangesAtEnd)
                {
                    UseX360ce(i, players, player, context, setupDll);
                }

                if (gen.UseDirectX9Wrapper)
                {
                    UseDirectX9Wrapper();
                }

                if (gen.CopyCustomUtils?.Length > 0)
                {
                    CopyCustomUtils(i, linkFolder);
                }

                if (!string.IsNullOrEmpty(gen.FlawlessWidescreen))
                {
                    UseFlawlessWidescreen(i);
                }

                gen.PrePlay(context, this, player);

                string startArgs = context.StartArguments;

                if (!string.IsNullOrEmpty(lobbyConnectArg) && i > 0)
                {
                    startArgs = lobbyConnectArg + " " + startArgs;
                    Log("Goldberg Lobby Connect: Will join lobby ID " + lobbyConnectArg.Substring(15));
                }

                if (context.Hook.CustomDllEnabled && !gen.ProcessChangesAtEnd)
                {
                    CustomDllEnabled(context, player, playerBounds, i);
                }

                if (!gen.UseGoldberg && ini.IniReadValue("Misc", "UseNicksInGame") == "True")
                {

                    string[] files = Directory.GetFiles(linkFolder, "account_name.txt", SearchOption.AllDirectories);
                    if(files.Length > 0)
                    {
                        Log("Goldberg is not enabled in script, however account_name.txt file(s) were found in folder. Will update nicknames");
                    }
                    foreach (string nameFile in files)
                    {
                        if (!string.IsNullOrEmpty(player.Nickname))
                        {
                            Log(string.Format("Writing nickname {0} in account_name.txt", player.Nickname));
                            //MessageBox.Show("Found account_name.txt at: " + nameFile + ", replacing: " + File.ReadAllText(nameFile) + " with: " + player.Nickname + " for player " + i);
                            File.Delete(nameFile);
                            File.WriteAllText(nameFile, player.Nickname);
                        }
                        else
                        {
                            if (player.IsKeyboardPlayer && ini.IniReadValue("ControllerMapping", "Keyboard") != "")
                            {
                                Log(string.Format("Writing nickname {0} in account_name.txt", ini.IniReadValue("ControllerMapping", "Keyboard")));
                                //MessageBox.Show("Found account_name.txt at: " + nameFile + ", replacing: " + File.ReadAllText(nameFile) + " with: " + player.Nickname + " for player " + i);
                                File.Delete(nameFile);
                                File.WriteAllText(nameFile, ini.IniReadValue("ControllerMapping", "Keyboard"));
                            }
                            else
                            {
                                File.Delete(nameFile);
                                File.WriteAllText(nameFile, "Player " + (i + 1));
                            }
                        }
                    }
                }

                if (gen.ChangeIPPerInstance && !gen.ProcessChangesAtEnd)
                {
                    ChangeIPPerInstance(i);
                }

                Process proc = null;

                if (gen.LauncherExe?.Length > 0)
                {
                    //Force no starting arguments as a launcher is being used
                    //Log("Removing starting arguments as a launcher is being used");
                    //startArgs = string.Empty;

                    if (gen.HookInit || gen.RenameNotKillMutex || gen.SetWindowHookStart || gen.BlockRawInput || gen.CreateSingleDeviceFile)
                    {
                        Log("Disabling start up hooks as a launcher is being used");
                        gen.HookInit = false;
                        gen.RenameNotKillMutex = false;
                        gen.SetWindowHookStart = false;
                        gen.BlockRawInput = false;
                        gen.CreateSingleDeviceFile = false;
                    }
                }

                if (context.NeedsSteamEmulation)
                {
                    Log("Setting up SmartSteamEmu");

                    string steamEmu = Path.Combine(linkFolder, "SmartSteamLoader"); //GameManager.Instance.ExtractSteamEmu(Path.Combine(linkFolder, "SmartSteamLoader"));
                    string sourcePath = Path.Combine(GameManager.Instance.GetUtilsPath(), "SmartSteamEmu");

                    Log(string.Format("Copying SmartSteamEmu files to {0}", steamEmu));
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
                    string lang = "english";
                    if (ini.IniReadValue("Misc", "SteamLang") != "" && ini.IniReadValue("Misc", "SteamLang") != "Automatic")
                    {
                        gen.GoldbergLanguage = ini.IniReadValue("Misc", "SteamLang").ToLower();
                    }
                    if (gen.GoldbergLanguage?.Length > 0)
                    {
                        lang = gen.GoldbergLanguage;
                    }
                    else
                    {
                        lang = gen.GetSteamLanguage();
                    }
                    emu.IniWriteValue("SmartSteamEmu", "Language", lang);

                    if (ini.IniReadValue("Misc", "UseNicksInGame") == "True" && !string.IsNullOrEmpty(player.Nickname))
                    {
                        emu.IniWriteValue("SmartSteamEmu", "PersonaName", player.Nickname);
                    }
                    else
                    {
                        if (ini.IniReadValue("Misc", "UseNicksInGame") == "True" && player.IsKeyboardPlayer && !player.IsRawKeyboard && ini.IniReadValue("ControllerMapping", "Keyboard") != "")
                        {
                            emu.IniWriteValue("SmartSteamEmu", "PersonaName", ini.IniReadValue("ControllerMapping", "Keyboard"));
                        }
                        else
                        {
                            emu.IniWriteValue("SmartSteamEmu", "PersonaName", "Player" + (i + 1));
                        }
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

                    if (!gen.ThirdPartyLaunch)
                    {
                        if (context.KillMutex?.Length > 0)
                        {
                            // to kill the mutexes we need to orphanize the process
                            proc = ProcessUtil.RunOrphanProcess(emuExe);
                            Log(string.Format("Started process {0} (pid {1}) as an orphan in order to kill mutexes in future", proc.ProcessName, proc.Id));
                        }
                        else
                        {
                            ProcessStartInfo startInfo = new ProcessStartInfo();
                            startInfo.FileName = emuExe;

                            proc = Process.Start(emuExe);
                            Log(string.Format("Started process {0} (pid {1})", proc.ProcessName, proc.Id));
                        }
                    }
                    else
                    {
                        Log("Skipping launching of game via Nucleus for third party launch");
                    }

                    //player.SteamEmu = true;
                    Log("SmartSteamEmu setup complete");

                    proc = null;
                    Thread.Sleep(5000);
                }
                else
                {

                    if (!gen.ThirdPartyLaunch)
                    {
                        if (/*context.KillMutex?.Length > 0 || */(gen.HookInit || (gen.RenameNotKillMutex && context.KillMutex?.Length > 0) || gen.SetWindowHookStart || gen.BlockRawInput || gen.CreateSingleDeviceFile) && !gen.CMDLaunch && !gen.UseForceBindIP) /*|| (gen.CMDLaunch && i==0))*/
                        {

                            string mu = "";
                            if (gen.RenameNotKillMutex && context.KillMutex?.Length > 0)
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

                            bool startupHooksEnabled = true;
                            if (gen.StartHookInstances?.Length > 0)
                            {
                                string[] instancesToHook = gen.StartHookInstances.Split(',');
                                if (!instancesToHook.ToList().Contains((i + 1).ToString()))
                                {
                                    startupHooksEnabled = false;
                                }
                            }

                            //string rawHID = player.DInputJoystick.Properties.InterfacePath;
                            //string frmtRawHidPartA = rawHID.Substring(0, rawHID.LastIndexOf("&")).ToUpper();
                            //string frmtRawHidPartB = rawHID.Substring(rawHID.LastIndexOf("&") + 1);
                            //string frmtRawHid = frmtRawHidPartA + frmtRawHidPartB;
                            //MessageBox.Show(frmtRawHid);

                            Log(string.Format("Launching game located at {0} through StartGameUtil", exePath));
                            uint sguOutPID = StartGameUtil.StartGame(/*
                                GetRelativePath(exePath, nucleusRootFolder)*/exePath, startArgs,
                                gen.HookInit, gen.HookInitDelay, gen.RenameNotKillMutex, mu, gen.SetWindowHookStart, isDebug, nucleusRootFolder, gen.BlockRawInput, gen.UseNucleusEnvironment, player.Nickname, startupHooksEnabled, gen.CreateSingleDeviceFile, player.RawHID/*, gen.RunAsAdmin, rawHID,*/ /*GetRelativePath(linkFolder, nucleusRootFolder)*/);

                            try
                            {
                                proc = Process.GetProcessById((int)sguOutPID);
                            }
                            catch (Exception ex)
                            {
                                proc = null;
                                Log("Process By ID failed, setting process to null and continuing, will try and catch it later");
                            }
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

                                if (gen.CMDLaunch)
                                {
                                    if (gen.UseNucleusEnvironment)
                                    {
                                        Log("Setting up Nucleus environment");
                                        var username = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).Replace(@"C:\Users\", "");
                                        cmd.StandardInput.WriteLine($@"set APPDATA=C:\Users\{username}\NucleusCoop\{player.Nickname}\AppData\Roaming");
                                        cmd.StandardInput.WriteLine($@"set LOCALAPPDATA=C:\Users\{username}\NucleusCoop\{player.Nickname}\AppData\Local");
                                        cmd.StandardInput.WriteLine($@"set USERPROFILE=C:\Users\{username}\NucleusCoop\{player.Nickname}");
                                        cmd.StandardInput.WriteLine($@"set HOMEPATH=\Users\{username}\NucleusCoop\{player.Nickname}");

                                        //Some games will crash if the directories don't exist
                                        Directory.CreateDirectory($@"C:\Users\{username}\NucleusCoop");
                                        Directory.CreateDirectory($@"C:\Users\{username}\NucleusCoop\{player.Nickname}\AppData\Roaming");
                                        Directory.CreateDirectory($@"C:\Users\{username}\NucleusCoop\{player.Nickname}\AppData\Local");
                                        Directory.CreateDirectory($@"C:\Users\{username}\NucleusCoop\{player.Nickname}");
                                        Directory.CreateDirectory($@"\Users\{username}\NucleusCoop\{player.Nickname}");
                                        Directory.CreateDirectory($@"C:\Users\{username}\NucleusCoop\{player.Nickname}\Documents");
                                    }

                                    if (gen.CMDBatchBefore?.Length > 0 || gen.CMDBatchAfter?.Length > 0)
                                    {
                                        cmd.StandardInput.WriteLine($@"set NUCLEUS_EXE=" + Path.GetFileName(exePath));
                                        cmd.StandardInput.WriteLine($@"set NUCLEUS_INST_EXE_FOLDER=" + Path.GetDirectoryName(exePath));
                                        cmd.StandardInput.WriteLine($@"set NUCLEUS_INST_FOLDER=" + linkFolder);
                                        cmd.StandardInput.WriteLine($@"set NUCLEUS_FOLDER=" + nucleusFolderPath);
                                        cmd.StandardInput.WriteLine($@"set NUCLEUS_ORIG_EXE_FOLDER=" + exeFolder);
                                        cmd.StandardInput.WriteLine($@"set NUCLEUS_ORIG_FOLDER=" + exeFolder.Substring(0, (exeFolder.Length - gen.BinariesFolder.Length)));
                                    }

                                    if (gen.CMDBatchBefore?.Length > 0)
                                    {

                                        for (int x = 0; x < gen.CMDBatchBefore.Length; x++)
                                        {
                                            if (!gen.CMDBatchBefore[x].Contains("|"))
                                            {
                                                Log("Running command line: " + gen.CMDBatchBefore[x]);
                                                cmd.StandardInput.WriteLine(gen.CMDBatchBefore[x]);
                                            }
                                            else
                                            {
                                                string[] clineSplit = gen.CMDBatchBefore[x].Split('|');
                                                if (clineSplit[0] == i.ToString())
                                                {
                                                    Log("Running command line: " + clineSplit[1]);
                                                    cmd.StandardInput.WriteLine(clineSplit[1]);
                                                }
                                            }
                                        }
                                    }

                                    string cmdLine = "\"" + exePath + "\" " + startArgs;
                                    if (cmdOps?.Length > 0 && i < cmdOps.Length)
                                    {
                                        cmdLine = cmdOps[i] + " \"" + exePath + "\" " + startArgs;
                                    }
                                    //string cmdLine = cmdOps[i] + " \"" + exePath + " " + startArgs + "\"";
                                    Log(string.Format("Launching game via command prompt with the following line: {0}", cmdLine));
                                    cmd.StandardInput.WriteLine(cmdLine);


                                    if (gen.CMDBatchAfter?.Length > 0)
                                    {
                                        for (int x = 0; x < gen.CMDBatchAfter.Length; x++)
                                        {
                                            if (!gen.CMDBatchAfter[x].Contains("|"))
                                            {
                                                Log("Running command line: " + gen.CMDBatchAfter[x]);
                                                cmd.StandardInput.WriteLine(gen.CMDBatchAfter[x]);
                                            }
                                            else
                                            {
                                                string[] clineSplit = gen.CMDBatchAfter[x].Split('|');
                                                if (clineSplit[0] == i.ToString())
                                                {
                                                    Log("Running command line: " + clineSplit[1]);
                                                    cmd.StandardInput.WriteLine(clineSplit[1]);
                                                }
                                            }
                                        }
                                    }
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
                                    //}
                                    if (gen.UseNucleusEnvironment)
                                    {
                                        Log("Setting up Nucleus environment");
                                        var username = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).Replace(@"C:\Users\", "");
                                        cmd.StandardInput.WriteLine($@"set APPDATA=C:\Users\{username}\NucleusCoop\{player.Nickname}\AppData\Roaming");
                                        cmd.StandardInput.WriteLine($@"set LOCALAPPDATA=C:\Users\{username}\NucleusCoop\{player.Nickname}\AppData\Local");
                                        cmd.StandardInput.WriteLine($@"set USERPROFILE=C:\Users\{username}\NucleusCoop\{player.Nickname}");
                                        cmd.StandardInput.WriteLine($@"set HOMEPATH=\Users\{username}\NucleusCoop\{player.Nickname}");

                                        //Some games will crash if the directories don't exist
                                        Directory.CreateDirectory($@"C:\Users\{username}\NucleusCoop");
                                        Directory.CreateDirectory($@"C:\Users\{username}\NucleusCoop\{player.Nickname}\AppData\Roaming");
                                        Directory.CreateDirectory($@"C:\Users\{username}\NucleusCoop\{player.Nickname}\AppData\Local");
                                        Directory.CreateDirectory($@"C:\Users\{username}\NucleusCoop\{player.Nickname}");
                                        Directory.CreateDirectory($@"\Users\{username}\NucleusCoop\{player.Nickname}");
                                        Directory.CreateDirectory($@"C:\Users\{username}\NucleusCoop\{player.Nickname}\Documents");
                                    }

                                    if (gen.CMDBatchBefore?.Length > 0 || gen.CMDBatchAfter?.Length > 0)
                                    {
                                        cmd.StandardInput.WriteLine($@"set NUCLEUS_EXE=" + Path.GetFileName(exePath));
                                        cmd.StandardInput.WriteLine($@"set NUCLEUS_INST_EXE_FOLDER=" + Path.GetDirectoryName(exePath));
                                        cmd.StandardInput.WriteLine($@"set NUCLEUS_INST_FOLDER=" + linkFolder);
                                        cmd.StandardInput.WriteLine($@"set NUCLEUS_FOLDER=" + nucleusFolderPath);
                                        cmd.StandardInput.WriteLine($@"set NUCLEUS_ORIG_EXE_FOLDER=" + exeFolder);
                                        cmd.StandardInput.WriteLine($@"set NUCLEUS_ORIG_FOLDER=" + exeFolder.Substring(0, (exeFolder.Length - gen.BinariesFolder.Length)));
                                    }

                                    if (gen.CMDBatchBefore?.Length > 0)
                                    {

                                        for (int x = 0; x < gen.CMDBatchBefore.Length; x++)
                                        {
                                            if (!gen.CMDBatchBefore[x].Contains("|"))
                                            {
                                                Log("Running command line: " + gen.CMDBatchBefore[x]);
                                                cmd.StandardInput.WriteLine(gen.CMDBatchBefore[x]);
                                            }
                                            else
                                            {
                                                string[] clineSplit = gen.CMDBatchBefore[x].Split('|');
                                                if (clineSplit[0] == i.ToString())
                                                {
                                                    Log("Running command line: " + clineSplit[1]);
                                                    cmd.StandardInput.WriteLine(clineSplit[1]);
                                                }
                                            }
                                        }
                                    }

                                    string cmdLine = "\"" + Path.Combine(GameManager.Instance.GetUtilsPath(), "ForceBindIP\\" + forceBindexe) + "\" 127.0.0." + (i + 2) + " \"" + exePath + "\" " + startArgs;
                                    Log(string.Format("Launching game using ForceBindIP command line argument: {0}", cmdLine));
                                    cmd.StandardInput.WriteLine(cmdLine);

                                    if (gen.CMDBatchAfter?.Length > 0)
                                    {
                                        for (int x = 0; x < gen.CMDBatchAfter.Length; x++)
                                        {
                                            if (!gen.CMDBatchAfter[x].Contains("|"))
                                            {
                                                Log("Running command line: " + gen.CMDBatchAfter[x]);
                                                cmd.StandardInput.WriteLine(gen.CMDBatchAfter[x]);
                                            }
                                            else
                                            {
                                                string[] clineSplit = gen.CMDBatchAfter[x].Split('|');
                                                if (clineSplit[0] == i.ToString())
                                                {
                                                    Log("Running command line: " + clineSplit[1]);
                                                    cmd.StandardInput.WriteLine(clineSplit[1]);
                                                }
                                            }
                                        }
                                    }
                                }

                                proc = null;
                                cmd.StandardInput.Flush();
                                cmd.StandardInput.Close();
                                //cmd.WaitForExit();
                                //Console.WriteLine(cmd.StandardOutput.ReadToEnd());

                            }
                            else
                            {

                                //ProcessStartInfo startInfo = new ProcessStartInfo();
                                //startInfo.UseShellExecute = false;
                                //startInfo.WorkingDirectory = Path.GetDirectoryName(exePath);
                                //startInfo.FileName = exePath;
                                //startInfo.Arguments = startArgs;

                                IntPtr envPtr = IntPtr.Zero;

                                if (gen.UseNucleusEnvironment)
                                {
                                    Log("Setting up Nucleus environment");
                                    var sb = new StringBuilder();
                                    IDictionary envVars = Environment.GetEnvironmentVariables();
                                    var username = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).Replace(@"C:\Users\", "");
                                    envVars["USERPROFILE"] = $@"C:\Users\{username}\NucleusCoop\{player.Nickname}";
                                    envVars["HOMEPATH"] = $@"\Users\{username}\NucleusCoop\{player.Nickname}";
                                    envVars["APPDATA"] = $@"C:\Users\{username}\NucleusCoop\{player.Nickname}\AppData\Roaming";
                                    envVars["LOCALAPPDATA"] = $@"C:\Users\{username}\NucleusCoop\{player.Nickname}\AppData\Local";

                                    //Some games will crash if the directories don't exist
                                    Directory.CreateDirectory($@"C:\Users\{username}\NucleusCoop");
                                    Directory.CreateDirectory(envVars["USERPROFILE"].ToString());
                                    Directory.CreateDirectory(Path.Combine(envVars["USERPROFILE"].ToString(), "Documents"));
                                    Directory.CreateDirectory(envVars["APPDATA"].ToString());
                                    Directory.CreateDirectory(envVars["LOCALAPPDATA"].ToString());

                                    foreach (object envVarKey in envVars.Keys)
                                    {
                                        if (envVarKey != null)
                                        {
                                            string key = envVarKey.ToString();
                                            string value = envVars[envVarKey].ToString();

                                            sb.Append(key);
                                            sb.Append("=");
                                            sb.Append(value);
                                            sb.Append("\0");
                                        }
                                    }

                                    sb.Append("\0");

                                    byte[] envBytes = Encoding.Unicode.GetBytes(sb.ToString());
                                    envPtr = Marshal.AllocHGlobal(envBytes.Length);
                                    Marshal.Copy(envBytes, 0, envPtr, envBytes.Length);

                                }

                                STARTUPINFO startup = new STARTUPINFO();
                                startup.cb = Marshal.SizeOf(startup);

                                bool success = CreateProcess(null, exePath + " " + startArgs, IntPtr.Zero, IntPtr.Zero, false, (uint)ProcessCreationFlags.CREATE_UNICODE_ENVIRONMENT, envPtr, Path.GetDirectoryName(exePath), ref startup, out PROCESS_INFORMATION processInformation);
                                Log(string.Format("Launching game directly at {0}", exePath));

                                if (!success)
                                {
                                    int error = Marshal.GetLastWin32Error();
                                    Log(string.Format("ERROR {0} - CreateProcess failed - startGamePath: {1}, startArgs: {2}, dirpath: {3}", error, exePath, startArgs, Path.GetDirectoryName(exePath)));
                                }
                                //proc = Process.Start(startInfo);

                                proc = Process.GetProcessById(processInformation.dwProcessId);

                            }

                        }
                    }
                    else
                    {
                        Log("Skipping launching of game via Nucleus for third party launch");
                        MessageBox.Show("Press Ok when game has launched.", "Nucleus - Third Party Launch");

                    }

                    if (gen.LauncherExe?.Length > 0)
                    {
                        //Force process to be null as we don't want launcher process
                        Log("Dropping process as it is the launcher");
                        proc = null;
                    }

                }

                if(gen.ProcessChangesAtEnd)
                {
                    if(i == (players.Count - 1))
                    {
                        if (gen.PauseBetweenStarts > 0)
                        {
                            Log(string.Format("Pausing for {0} seconds", gen.PauseBetweenStarts));
                            Thread.Sleep(TimeSpan.FromSeconds(gen.PauseBetweenStarts));
                        }

                        if(gen.PromptProcessChangesAtEnd)
                        {
                            Log("Prompted user before searching for game process");
                            MessageBox.Show("Press OK when ready to make changes to game processes.", "Nucleus - Prompt Process Changes At End", MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                        }
                        ProcessEnd();

                        return string.Empty;
                    }
                    else
                    {
                        if (gen.PromptBetweenInstances)
                        {
                            if (gen.PauseBetweenStarts > 0)
                            {
                                Log(string.Format("Pausing for {0} seconds", gen.PauseBetweenStarts));
                                Thread.Sleep(TimeSpan.FromSeconds(gen.PauseBetweenStarts));
                            }
                            Log(string.Format("Prompted user for Instance {0}", (i + 2)));
                            MessageBox.Show("Press OK when ready to launch instance " + (i + 2) + ".", "Nucleus - Prompt Between Instances", MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                        }
                        else
                        {
                            if (gen.PauseBetweenStarts > 0)
                            {
                                Log(string.Format("Pausing for {0} seconds", gen.PauseBetweenStarts));
                                Thread.Sleep(TimeSpan.FromSeconds(gen.PauseBetweenStarts));
                            }
                        }

                        continue;
                    }
                }

                if (gen.PromptBeforeProcessGrab)
                {
                    Log("Prompted user before searching for game process");
                    MessageBox.Show("Press OK when ready for Nucleus to search for game process.", "Nucleus - Prompt Before Process Grab", MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                }

                if (gen.NeedsSteamEmulation || gen.ForceProcessPick || proc == null || gen.CMDLaunch || gen.UseForceBindIP || gen.GameName == "Halo Custom Edition" /*|| gen.LauncherExe?.Length > 0*/)
                {
                    Log("Searching for game process");
                    if (!gen.ForceProcessPick)
                    {
                        //bool foundUnique = false;
                        for (int times = 0; times < 200; times++)
                        {
                            Thread.Sleep(50);

                            string proceName = "";
                            if (gen.GameName == "Halo Custom Edition" /*|| gen.LauncherExe?.Length > 0*/)
                            {
                                //Halo CE seems to need to wait X additional seconds otherwise crashes...
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

                            //string launcherName = string.Empty;
                            //if (gen.LauncherExe?.Length > 0)
                            //{
                            //    launcherName = Path.GetFileNameWithoutExtension(gen.LauncherExe).ToLower();
                            //}

                            //if (!string.IsNullOrEmpty(launcherName))
                            //{
                            //    Log(string.Format("Attempting to find game process {0}, or its launcher: {1}", proceName, launcherName));
                            //}
                            //else
                            //{
                            //    Log(string.Format("Attempting to find game process {0}", proceName));
                            //}


                            Process[] procs = Process.GetProcesses();
                            for (int j = 0; j < procs.Length; j++)
                            {
                                Process p = procs[j];

                                string lowerP = p.ProcessName.ToLower();

                                if (lowerP == proceName) //|| lowerP == launcherName)
                                {
                                    if (!attachedIds.Contains(p.Id)) //&& (int)p.MainWindowHandle > 0)
                                    {
                                        if (p.ProcessName == "javaw")
                                        {
                                            if ((int)p.MainWindowHandle == 0)
                                                continue;
                                        }
                                        Log(string.Format("Found process {0} (pid {1})", p.ProcessName, p.Id));
                                        attached.Add(p);
                                        attachedIds.Add(p.Id);
                                        if(player.IsKeyboardPlayer && !player.IsRawKeyboard)
                                        {
                                            keyboardProcId = p.Id;
                                        }
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

                    if (proc == null || gen.ForceProcessPick)
                    {
                        proc = LaunchProcessPick(player);
                    }

                }
                else
                {
                    Log(string.Format("Obtained process {0} (pid {1})", proc.ProcessName, proc.Id));
                    attached.Add(proc);
                    attachedIds.Add(proc.Id);
                    if (player.IsKeyboardPlayer && !player.IsRawKeyboard)
                    {
                        keyboardProcId = proc.Id;
                    }
                    //InjectDLLs(proc);
                }

                if (gen.GoldbergLobbyConnect && i == 0)
                {
                    GoldbergLobbyConnect();
                }

                if (i > 0 && gen.ResetWindows && prevProcessData != null)
                {
                    ResetWindows(prevProcessData, prevWindowX, prevWindowY, prevWindowWidth, prevWindowHeight, i);
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

                if (gen.ProcessorPriorityClass?.Length > 0)
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

                if (gen.IdealProcessor > 0)
                {
                    Log(string.Format("Setting ideal processor to {0}", gen.IdealProcessor));
                    ProcessThreadCollection threads = proc.Threads;
                    for (int t = 0; t < threads.Count; t++)
                    {
                        threads[t].IdealProcessor = (gen.IdealProcessor + 1);
                    }
                }

                //if(gen.UseProcessor?.Length > 0)
                //{
                //    Log(string.Format("Assigning processors {0}", gen.UseProcessor));
                //    ulong affinityMask = gen.UseProcessor.Split(',')
                //                .Select(int.Parse)
                //                .Aggregate(0UL, (mask, id) => mask | (1UL << id));
                //    proc.ProcessorAffinity = (IntPtr)affinityMask;
                //}

                if ((gen.UseProcessor != null ? (gen.UseProcessor.Length > 0 ? 1 : 0) : 0) != 0)
                {
                    this.Log(string.Format("Assigning processors {0}", gen.UseProcessor));
                    string[] strArray = this.gen.UseProcessor.Split(',');
                    int num2 = 0;
                    foreach (string s in strArray)
                        num2 |= 1 << int.Parse(s) - 1;
                    proc.ProcessorAffinity = (IntPtr)num2;
                }
                else
                {
                    string[] processorsPerInstance = gen.UseProcessorsPerInstance;
                    if ((processorsPerInstance != null ? ((uint)processorsPerInstance.Length > 0U ? 1 : 0) : 0) != 0)
                    {
                        foreach (string str7 in this.gen.UseProcessorsPerInstance)
                        {
                            int num2 = int.Parse(str7.Split('|')[0]);
                            string str8 = str7.Split('|')[1];
                            if (num2 == i + 1)
                            {
                                this.Log(string.Format("Assigning processors {0} for instance {1}", (object)str8, i));
                                string[] strArray = str8.Split(',');
                                int num3 = 0;
                                foreach (string s in strArray)
                                    num3 |= 1 << int.Parse(s) - 1;
                                proc.ProcessorAffinity = (IntPtr)num3;
                            }
                        }
                    }
                }

                if (gen.IdInWindowTitle || !string.IsNullOrEmpty(gen.FlawlessWidescreen))
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
                        string windowTitle = proc.MainWindowTitle + "(" + i + ")";
                        if(!string.IsNullOrEmpty(gen.FlawlessWidescreen))
                        {
                            windowTitle = "Nucleus Instance " + (i + 1) + "(" + gen.Hook.ForceFocusWindowName + ")";
                        }
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
                    if (gen.PauseBetweenStarts > 0)
                    {
                        Log(string.Format("Pausing for {0} seconds", gen.PauseBetweenStarts));
                        Thread.Sleep(TimeSpan.FromSeconds(gen.PauseBetweenStarts));
                    }
                    Log(string.Format("Prompted user for Instance {0}", (i + 2)));
                    MessageBox.Show("Press OK when ready to launch instance " + (i + 2) + ".", "Nucleus - Prompt Between Instances", MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                }
                else if (gen.PromptBetweenInstances && i == players.Count - 1 && (gen.HookFocus || gen.FakeFocus || gen.SetWindowHook || gen.HideCursor || gen.PreventWindowDeactivation))
                {
                    if (gen.PauseBetweenStarts > 0)
                    {
                        Log(string.Format("Pausing for {0} seconds", gen.PauseBetweenStarts));
                        Thread.Sleep(TimeSpan.FromSeconds(gen.PauseBetweenStarts));
                    }
                    Log("Prompted user to install focus hooks");
                    MessageBox.Show("Press OK when ready to install hooks and/or start sending fake messages.", "Nucleus - Prompt Between Instances", MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    foreach (Process aproc in attached)
                    {
                        IntPtr topMostFlag = new IntPtr(-1);
                        if(gen.NotTopMost)
                        {
                            topMostFlag = new IntPtr(-2);
                        }
                        User32Interop.SetWindowPos(aproc.MainWindowHandle, topMostFlag, 0, 0, 0, 0, (uint)(PositioningFlags.SWP_NOMOVE | PositioningFlags.SWP_NOSIZE | PositioningFlags.SWP_SHOWWINDOW));
                    }
                }
                else
                {
                    Log(string.Format("Pausing for {0} seconds", gen.PauseBetweenStarts));
                    Thread.Sleep(TimeSpan.FromSeconds(gen.PauseBetweenStarts));
                }

				//Set up raw input window
				//if (player.IsRawKeyboard || player.IsRawMouse)
				{
					var window = CreateRawInputWindow(proc, player);

					nextWindowToInject = window;
				}

				if (i == (players.Count - 1)) // all instances accounted for
                {
                    if(gen.KillLastInstanceMutex && !gen.RenameNotKillMutex)
                    {
                        for (; ; )
                        {
                            if (gen.KillMutex != null)
                            {
                                if (gen.KillMutex.Length > 0 && !players[i].ProcessData.KilledMutexes)
                                {
                                    if (gen.KillMutexDelay > 0)
                                    {
                                        Thread.Sleep((gen.KillMutexDelay * 1000));
                                    }

                                    if (StartGameUtil.MutexExists(players[i].ProcessData.Process, gen.KillMutexType, gen.PartialMutexSearch, gen.KillMutex))
                                    {
                                        // mutexes still exist, must kill
                                        StartGameUtil.KillMutex(players[i].ProcessData.Process, gen.KillMutexType, gen.PartialMutexSearch, gen.KillMutex);
                                        players[i].ProcessData.KilledMutexes = true;
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

                    Thread.Sleep(1000);

                    if (gen.ResetWindows)
                    {
                        ResetWindows(data, prevWindowX, prevWindowY, prevWindowWidth, prevWindowHeight, i + 1);
                    }

                    if (gen.FakeFocus)
                    {
                        Log("Start sending fake focus messages every 1000 ms");
                        fakeFocus = new Thread(SendFocusMsgs);
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

                    if(!string.IsNullOrEmpty(gen.FlawlessWidescreen))
                    {
                        for(int fw = 0; fw < players.Count; fw++)
                        {
                            Process fwProc = Process.GetProcessById(players[fw].ProcessData.Process.Id);
                            string windowTitle = "Nucleus Instance " + (fw + 1) + "(" + gen.Hook.ForceFocusWindowName + ")";

                            if (fwProc.MainWindowTitle != windowTitle)
                            {
                                Log(string.Format("Resetting window text for pid {0} to {0}", fwProc.Id, windowTitle));
                                SetWindowText(fwProc.MainWindowHandle, windowTitle);
                            }
                        }
                    }

					if ((i > 0 || players.Count == 1) && (gen.HookFocus || gen.SetWindowHook || gen.HideCursor || gen.PreventWindowDeactivation || gen.SupportsMultipleKeyboardsAndMice))
                    {
                        if (gen.PreventWindowDeactivation && !isPrevent)
                        {
                            Log("PreventWindowDeactivation detected, setting flag");
                            isPrevent = true;
                        }

                        if (isPrevent)
                        {
                            if (players[i].IsKeyboardPlayer && gen.KeyboardPlayerSkipPreventWindowDeactivate)
                            {
                                Log("Ignoring PreventWindowDeactivation for keyboard player");
                                gen.PreventWindowDeactivation = false;
                            }
                            else
                            {
                                Log("Keeping PreventWindowDeactivation on");
                                gen.PreventWindowDeactivation = true;
                            }
                        }

                        if (gen.PostHookInstances?.Length > 0)
                        {
                            string[] instancesToHook = gen.PostHookInstances.Split(',');
                            foreach (string instanceToHook in instancesToHook)
                            {
                                if (int.Parse(instanceToHook) == (i + 1))
                                {
                                    Log("Injecting hook DLL for last instance");
                                    User32Interop.SetForegroundWindow(data.Process.MainWindowHandle);
                                    InjectDLLs(data.Process, nextWindowToInject);
                                }
                            }
                        }
                        else
                        {
                            Log("Injecting hook DLL for last instance");
                            User32Interop.SetForegroundWindow(data.Process.MainWindowHandle);
                            InjectDLLs(data.Process, nextWindowToInject);
						}
					}

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

                    foreach (PlayerInfo plyr in players)
                    {
                        Thread.Sleep(1000);

                        Process plyrProc = plyr.ProcessData.Process;

                        const int flip = 0x00C00000 | 0x00080000 | 0x00040000; //WS_BORDER | WS_SYSMENU

                        var x = (int)User32Interop.GetWindowLong(plyrProc.MainWindowHandle, User32_WS.GWL_STYLE);
                        if ((x & flip) > 0)//has a border
                        {
                            Log("Process id " + plyrProc.Id + ", still has or regained a border, trying to remove it (again)");
                            x &= (~flip);
                            ResetWindows(plyr.ProcessData, plyr.ProcessData.Position.X, plyr.ProcessData.Position.Y, plyr.ProcessData.Size.Width, plyr.ProcessData.Size.Height, plyr.PlayerID + 1);
                        }
                    }

                    //Window setup
                    foreach (var window in RawInputManager.windows)
					{
						var hWnd = window.hWnd;

						//Logger.WriteLine($"hWnd={hWnd}, mouse={window.MouseAttached}, kb={window.KeyboardAttached}");

						if (gen.DrawFakeMouseCursor && gen.SupportsMultipleKeyboardsAndMice)
						{
							window.NeedsCursorToBeCreatedOnMainMessageLoop = true;
						}

						//Borderlands 2 (and some other games) requires WM_INPUT to be sent to a window named DIEmWin, not the main hWnd.
						foreach (ProcessThread thread in Process.GetProcessById(window.pid).Threads)
						{

							int WindowEnum(IntPtr _hWnd, int lParam)
							{
								var threadId = WinApi.GetWindowThreadProcessId(_hWnd, out int pid);
								if (threadId == lParam)
								{
									string windowText = WinApi.GetWindowText(_hWnd);
									//Logger.WriteLine($" - thread id=0x{threadId:x}, _hWnd=0x{_hWnd:x}, window text={windowText}");

									if (windowText != null && windowText.ToLower().Contains("DIEmWin".ToLower()))
									{
										window.DIEmWin_hWnd = _hWnd;
									}
								}

								return 1;
							}

							WinApi.EnumWindows(WindowEnum, thread.Id);
						}
					}

					RawInputProcessor.Start();

					if (gen.SupportsMultipleKeyboardsAndMice && gen.LockInputAtStart)
					{
						LockInput.Lock();
					}

				}
            }

            return string.Empty;
        }

        private Window CreateRawInputWindow(Process proc, PlayerInfo player)
        {
	        var hWnd = WaitForProcWindowHandleNotZero(proc);
	        var mouseHdev = player.IsRawKeyboard ? player.RawMouseDeviceHandle : (IntPtr) (-1);
	        var keyboardHdev = player.IsRawMouse ? player.RawKeyboardDeviceHandle : (IntPtr) (-1);

	        var window = new Window(hWnd)
	        {
		        CursorVisibility = player.IsRawMouse && !gen.HideCursor && gen.DrawFakeMouseCursor,
		        KeyboardAttached = keyboardHdev,
		        MouseAttached = mouseHdev
	        };

	        window.CreateHookPipe(gen);

	        RawInputManager.windows.Add(window);
	        return window;
        }

        private void DeleteFiles(string linkFolder, int i)
		{
			foreach (string deleteLine in gen.DeleteFiles)
			{
				string[] deleteSplit = deleteLine.Split('|');
				int indexOffset = 1;
				if (deleteSplit.Length == 2)
				{
					if (int.Parse(deleteSplit[0]) != (i + 1))
					{
						continue;
					}
					indexOffset = 0;
				}
				//else if (deleteSplit.Length <= 1)
				//{
				//    Log("Invalid # of parameters provided for: " + deleteLine + ", skipping");
				//    continue;
				//}

				string fullFilePath = Path.Combine(linkFolder, deleteSplit[1 - indexOffset]);
                //FileInfo pathInfo = new FileInfo(fullFilePath);
                //if (pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                //{
                //    Log("Skipping HexEditFileAddress, " + Path.GetFileName(renameSplit[1 - indexOffset]) + " is symlinked");
                //    continue;
                //}

                if (File.Exists(fullFilePath))
                {
                    
                    if (!gen.IgnoreDeleteFilesPrompt)
                    {
                        DialogResult dialogResult = MessageBox.Show("Are you sure you want to delete '" + fullFilePath + "'?", "Nucleus - Delete Files In Config Path", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (dialogResult != DialogResult.Yes)
                        {
                            continue;
                        }
                    }
                    Log(string.Format("Deleting file {0}", Path.GetFileName(fullFilePath), Path.Combine(linkFolder, deleteSplit[2 - indexOffset])));
                    File.Delete(fullFilePath);
                }
                else
                {
                    Log("ERROR - Could not find file: " + fullFilePath + " to delete");
                }
            }
        }

        private void RenameOrMoveFiles(string linkFolder, int i)
        {
            foreach (string renameLine in gen.RenameAndOrMoveFiles)
            {
                string[] renameSplit = renameLine.Split('|');
                int indexOffset = 1;
                if (renameSplit.Length == 3)
                {
                    if (int.Parse(renameSplit[0]) != (i + 1))
                    {
                        continue;
                    }
                    indexOffset = 0;
                }
                else if (renameSplit.Length <= 1)
                {
                    Log("Invalid # of parameters provided for: " + renameLine + ", skipping renaming and or moving");
                    continue;
                }

                string fullFilePath = Path.Combine(linkFolder, renameSplit[1 - indexOffset]);
                //FileInfo pathInfo = new FileInfo(fullFilePath);
                //if (pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                //{
                //    Log("Skipping HexEditFileAddress, " + Path.GetFileName(renameSplit[1 - indexOffset]) + " is symlinked");
                //    continue;
                //}

                if (File.Exists(fullFilePath))
                {
                    Log(string.Format("Renaming and/or moving {0} to {1}", Path.GetFileName(fullFilePath), Path.Combine(linkFolder, renameSplit[2 - indexOffset])));
                    if(!Directory.Exists(Path.GetDirectoryName(Path.Combine(linkFolder, renameSplit[2 - indexOffset]))))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(linkFolder, renameSplit[2 - indexOffset])));
                    }
                    File.Move(fullFilePath, Path.Combine(linkFolder, renameSplit[2 - indexOffset]));
                }
                else
                {
                    Log("ERROR - Could not find file: " + fullFilePath + " to rename and/or move");
                }
            }
        }

        private void UseFlawlessWidescreen(int i)
        {
            Log("Setting up Flawless Widescreen");

            bool pcIs64 = Environment.Is64BitOperatingSystem;
            string pcArch = pcIs64 ? "x64" : "x86";

            string utilFolder = Path.Combine(Directory.GetCurrentDirectory(), "utils\\FlawlessWidescreen\\" + pcArch);
            string fwGameFolder = Path.Combine(utilFolder, "PluginCache\\FWS_Plugins\\Modules\\" + gen.FlawlessWidescreen);



            if (!Directory.Exists(fwGameFolder))
            {
                MessageBox.Show("Nucleus could not an installed plugin for \"" + gen.FlawlessWidescreen + "\" in FlawlessWidescren. FlawlessWidescreen will now open. Please make sure to install the plugin and make any required changes. When yo close FlawlessWidescreen, Nucleus will continue. Press OK to open FlawlessWidescreen", "Nucleus - Use Flawless Widescreen", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                //bool appRunning = false;
                Process[] runnProcs = Process.GetProcesses();
                foreach (Process proc in runnProcs)
                {
                    if (proc.ProcessName.ToLower() == "flawlesswidescreen")
                    {
                        proc.Kill();
                    }
                }

                Log("Starting Flawless Widescreen process");
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.UseShellExecute = true;
                startInfo.WorkingDirectory = utilFolder;
                startInfo.FileName = Path.Combine(utilFolder, "FlawlessWidescreen.exe");
                Process util = Process.Start(startInfo);
                util.WaitForExit();
                //
                //if (!Directory.Exists(fwGameFolder))
                //{
                //    MessageBox.Show("Nucleus still unable to find folder. Skipping Flawless Widescreen setup.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //    return;
                //}
            }

            if(Directory.Exists(fwGameFolder))
            {

                if (File.Exists(Path.Combine(fwGameFolder, "Dependencies\\Scripts\\" + gen.FlawlessWidescreen + ".lua")))
                {
                    List<string> otextChanges = new List<string>();
                    string oscriptPath = Path.Combine(fwGameFolder, "Dependencies\\Scripts\\" + gen.FlawlessWidescreen + ".lua");

                    otextChanges.Add(context.FindLineNumberInTextFile(oscriptPath, "Process_WindowName = ", SearchType.StartsWith) + "|Process_WindowName = \"Removed\"");
                    context.ReplaceLinesInTextFile(oscriptPath, otextChanges.ToArray());
                }

                string newFwGameFolder = fwGameFolder + " - Nucleus Instance " + (i + 1);
                if(Directory.Exists(newFwGameFolder))
                {
                    Directory.Delete(newFwGameFolder, true);
                }
                Directory.CreateDirectory(newFwGameFolder);

                foreach (string dir in Directory.GetDirectories(fwGameFolder, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(Path.Combine(newFwGameFolder, dir.Substring(fwGameFolder.Length + 1)));
                }

                foreach (string file_name in Directory.GetFiles(fwGameFolder, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        File.Copy(file_name, Path.Combine(newFwGameFolder, file_name.Substring(fwGameFolder.Length + 1)));
                        Log("Copying file " + Path.GetFileName(file_name));
                    }
                    catch
                    {
                        //Log("ERROR - " + ex.Message);
                        Log("Using alternative copy method for " + file_name);
                        CmdUtil.ExecuteCommand(Path.GetDirectoryName(Path.Combine(newFwGameFolder, file_name.Substring(fwGameFolder.Length + 1))), out int exitCode, "copy \"" + file_name + "\" \"" + Path.Combine(newFwGameFolder, file_name.Substring(fwGameFolder.Length + 1)) + "\"", false);
                    }

                    if (file_name.EndsWith("Dependencies\\Scripts\\" + gen.FlawlessWidescreen + ".lua"))
                    {
                        File.Move(Path.Combine(newFwGameFolder + "\\Dependencies\\Scripts\\", gen.FlawlessWidescreen + ".lua"), Path.Combine(newFwGameFolder + "\\Dependencies\\Scripts\\", Path.GetFileNameWithoutExtension(file_name) + " - Nucleus Instance " + (i + 1) + ".lua"));
                    }
                }

                List<string> textChanges = new List<string>();
                string scriptPath = Path.Combine(newFwGameFolder, "Dependencies\\Scripts\\" + gen.FlawlessWidescreen + " - Nucleus Instance " + (i + 1) + ".lua");

                textChanges.Add(context.FindLineNumberInTextFile(scriptPath, "Process_WindowName = ", SearchType.StartsWith) + "|Process_WindowName = \"" + "Nucleus Instance " + (i + 1) + "(" + gen.Hook.ForceFocusWindowName + ")\"");
                context.ReplaceLinesInTextFile(scriptPath, textChanges.ToArray());

                string path = Path.Combine(utilFolder, "Plugins\\FWS_Plugins.fws");
                path = Environment.ExpandEnvironmentVariables(path);

                var doc = new XmlDocument();
                doc.Load(path);
                var nodes = doc.SelectNodes("Plugin/Modules/Module");
                bool exists = false;
                XmlNode origNode = null;
                foreach (XmlNode node in nodes)
                {
                    if (node.Attributes["NameSpace"].Value == gen.FlawlessWidescreen)
                    {
                        origNode = node;
                    }
                    if (node.Attributes["NameSpace"].Value == gen.FlawlessWidescreen + " - Nucleus Instance " + (i + 1))
                    {
                        exists = true;
                    }
                    if(origNode != null && exists)
                    {
                        break;
                    }
                }

                if(!exists)
                {
                    // Create a new node with the name of your new server
                    XmlNode newNode = doc.CreateElement("Module");

                    // set the inner xml of a new node to inner xml of original node
                    newNode.InnerXml = origNode.InnerXml;

                    XmlAttribute attr = doc.CreateAttribute("NameSpace");
                    attr.Value = gen.FlawlessWidescreen + " - Nucleus Instance " + (i + 1);

                    newNode.Attributes.SetNamedItem(attr);
                    newNode["FriendlyName"].InnerText = newNode["FriendlyName"].InnerText + " - Nucleus Instance " + (i + 1);

                    // append new node to DocumentElement, not XmlDocument
                    //doc.DocumentElement.AppendChild(newNode);
                    doc.DocumentElement["Modules"].AppendChild(newNode);
                }

                doc.Save(path);


                if (i == (numPlayers - 1))
                {
                    //bool appRunning = false;
                    Process[] runnProcs = Process.GetProcesses();
                    foreach (Process proc in runnProcs)
                    {
                        if (proc.ProcessName.ToLower() == "flawlesswidescreen")
                        {
                            proc.Kill();
                        }
                    }

                    //if (!appRunning)
                    //{
                        Log("Starting Flawless Widescreen process");
                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.UseShellExecute = true;
                        startInfo.WorkingDirectory = utilFolder;
                        startInfo.FileName = Path.Combine(utilFolder, "FlawlessWidescreen.exe");
                        Process.Start(startInfo);
                    //}
                }

                Log("Flawless Widescreen setup complete");
                //if (util != null)
                //{
                    //ShowWindow(util.MainWindowHandle, SW_MINIMIZE);
                //}
            }
        }

        private Process LaunchProcessPick(PlayerInfo player)
        {
            Log("Launching process picker");
            Label ppDesc = new Label();
            string ppDescStr = "Select a process for Nucleus to use for process manipulation, including (but not limited to): resizing, repositioning and installing post-launch hooks.";
            ppDesc.Text = ppDescStr;

            ListBox listBox = new ListBox();
            listBox.DoubleClick += new EventHandler(SelBtn_Click);

            Button refrshBtn = new Button();
            refrshBtn.Text = "Refresh";
            refrshBtn.Click += new EventHandler(RefrshBtn_Click);

            Button selBtn = new Button();
            selBtn.Text = "Select";
            selBtn.Click += new EventHandler(SelBtn_Click);

            Process[] allProc = Process.GetProcesses();
            foreach (Process p in allProc)
            {
                if (p.Id == 0 || string.IsNullOrEmpty(p.MainWindowTitle))
                {
                    continue;
                }
                if (attachedIds.Contains(p.Id))
                {
                    if (p.ProcessName.ToLower().Contains(Path.GetFileNameWithoutExtension(gen.ExecutableName).ToLower()) || (gen.LauncherExe?.Length > 0 && p.ProcessName.ToLower().Contains(Path.GetFileNameWithoutExtension(gen.LauncherExe).ToLower())))
                    {
                        listBox.Items.Insert(0, p.Id + " - (DO NOT USE - Already assigned in Nucleus) " + p.ProcessName);
                    }
                    else
                    {
                        listBox.Items.Add(p.Id + " - (DO NOT USE - Already assigned in Nucleus) " + p.ProcessName);
                    }
                }
                else
                {
                    if (p.ProcessName.ToLower().Contains(Path.GetFileNameWithoutExtension(gen.ExecutableName).ToLower()) || (gen.LauncherExe?.Length > 0 && p.ProcessName.ToLower().Contains(Path.GetFileNameWithoutExtension(gen.LauncherExe).ToLower())))
                    {
                        listBox.Items.Insert(0, p.Id + " - " + p.ProcessName);
                    }
                    else
                    {
                        listBox.Items.Add(p.Id + " - " + p.ProcessName);
                    }
                }
            }

            using (Form ppform = new Form())
            {
                ppform.Text = "Nucleus Coop - Process Picker";
                ppform.Width = 320;

                ppform.ControlBox = false;
                ppform.StartPosition = FormStartPosition.CenterScreen;
                ppform.MaximizeBox = false;
                ppform.MinimizeBox = false;
                ppform.FormBorderStyle = FormBorderStyle.FixedSingle;
                ppform.TopMost = true;

                ppDesc.Width = ppform.Width - 20;
                ppDesc.Height = 50;
                ppDesc.AutoSize = false;
                //ppDesc.MaximumSize = new Size(ppDesc.Width, 0);
                //ppDesc.AutoSize = true;
                ppDesc.Location = new Point(10, 10);

                listBox.Width = ppform.Width - 22;
                listBox.Height = 150;
                listBox.Location = new Point(10, 20 + ppDesc.Height);
                refrshBtn.Location = new Point((ppform.Width / 2) - (refrshBtn.Width + 5), 30 + ppDesc.Height + listBox.Height);
                selBtn.Location = new Point((ppform.Width / 2) + 5, 30 + ppDesc.Height + listBox.Height);

                ppform.Height = 40 + ppDesc.Height + listBox.Height + refrshBtn.Height + selBtn.Height;

                ppform.Controls.Add(ppDesc);
                ppform.Controls.Add(listBox);
                ppform.Controls.Add(refrshBtn);
                ppform.Controls.Add(selBtn);

                ppform.ShowDialog();
            }

            if (listBox.SelectedItem != null)
            {
                //MessageBox.Show(listBox.SelectedItem.ToString());
                Process proc = Process.GetProcessById(int.Parse(listBox.SelectedItem.ToString().Split(' ')[0]));
                Log(string.Format("Obtained process {0} (pid {1}) via process picker", proc.ProcessName, proc.Id));
                attached.Add(proc);
                attachedIds.Add(proc.Id);
                if (player.IsKeyboardPlayer && !player.IsRawKeyboard)
                {
                    keyboardProcId = proc.Id;
                }
                return proc;
            }

            return null;
            //aForm.ShowDialog();  // Or just use Show(); if you don't want it to be modal.
        }

        private void ProcessEnd()
        {
            List<PlayerInfo> players = profile.PlayerData;
            

            for (int i = 0; i < players.Count; i++)
            {
                PlayerInfo player = players[i];

                if (gen.ChangeIPPerInstance)
                {
                    ChangeIPPerInstance(i);
                }

                bool setupDll = true;
                if (!gen.SymlinkGame && !gen.HardlinkGame && !gen.HardcopyGame && i > 0)
                {
                    setupDll = false;
                }

                if (gen.XInputPlusDll?.Length > 0)
                {
                    SetupXInputPlusDll(garch, player, context, i, setupDll);
                }

                if (gen.UseDevReorder)
                {
                    UseDevReorder(garch, player, players, i, setupDll);
                }

                if (gen.UseX360ce)
                {
                    UseX360ce(i, players, player, context, setupDll);
                }

                if (gen.PromptBetweenInstancesEnd)
                {
                    Log(string.Format("Prompted user for Instance {0}", (i + 1)));
                    MessageBox.Show("Press OK when game instance " + (i + 1) + " has been launched and/or you wish to continue.", "Nucleus - Prompt Between Instances End", MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }

            int playerIndex = 0;

            Process[] currProcs = Process.GetProcesses();
            //foreach(Process proc in currProcs)
            for(int i=0; i < currProcs.Length; i++)
            {
                Process proc = currProcs[i];
                
                string procName = Path.GetFileNameWithoutExtension(gen.ExecutableName).ToLower();
                if (proc.ProcessName.ToLower() == procName)
                {
                    Thread.Sleep(1000);
                    ChangeGameWindow(proc, players, playerIndex);
                    playerIndex++;
                }
            }

            if(playerIndex < players.Count)
            {
                int numMissing = players.Count - playerIndex;
                for(int x = 0; x <= numMissing; x++)
                {
                    Thread.Sleep(1000);
                    Process proc = LaunchProcessPick(players[playerIndex + 1]);
                    ChangeGameWindow(proc, players, playerIndex + 1);
                    playerIndex++;
                }
            }

            Thread.Sleep(1000);

            if (gen.FakeFocus)
            {
                Log("Start sending fake focus messages every 1000 ms");
                fakeFocus = new Thread(SendFocusMsgs);
                fakeFocus.Start();
            }

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

        private void ChangeGameWindow(Process proc, List<PlayerInfo> players, int playerIndex)
        {
            Log(string.Format("Found process {0} (pid {1})", proc.ProcessName, proc.Id));
            attached.Add(proc);
            attachedIds.Add(proc.Id);

            var hwnd = WaitForProcWindowHandleNotZero(proc);

			Log("Removing game window border for this process");
            Point loc = new Point(players[playerIndex].MonitorBounds.X, players[playerIndex].MonitorBounds.Y);
            Size size = new Size(players[playerIndex].MonitorBounds.Width, players[playerIndex].MonitorBounds.Height);
            uint lStyle = User32Interop.GetWindowLong(hwnd, User32_WS.GWL_STYLE);
            if (gen.WindowStyleValues?.Length > 0)
            {
                Log("Using user custom window style");
                foreach (string val in gen.WindowStyleValues)
                {
                    if (val.StartsWith("~"))
                    {
                        lStyle &= ~Convert.ToUInt32(val.Substring(1), 16);
                    }
                    else
                    {
                        lStyle |= Convert.ToUInt32(val, 16);
                    }
                }
            }
            else
            {
                lStyle &= ~User32_WS.WS_CAPTION;
                lStyle &= ~User32_WS.WS_THICKFRAME;
                lStyle &= ~User32_WS.WS_MINIMIZE;
                lStyle &= ~User32_WS.WS_MAXIMIZE;
                lStyle &= ~User32_WS.WS_SYSMENU;
            }
            User32Interop.SetWindowLong(hwnd, User32_WS.GWL_STYLE, lStyle);

            lStyle = User32Interop.GetWindowLong(hwnd, User32_WS.GWL_EXSTYLE);
            if (gen.ExtWindowStyleValues?.Length > 0)
            {
                Log("Using user custom extended window style");
                foreach (string val in gen.ExtWindowStyleValues)
                {
                    if (val.StartsWith("~"))
                    {
                        lStyle &= ~Convert.ToUInt32(val.Substring(1), 16);
                    }
                    else
                    {
                        lStyle |= Convert.ToUInt32(val, 16);
                    }
                }
            }
            else
            {
                lStyle &= ~User32_WS.WS_EX_DLGMODALFRAME;
                lStyle &= ~User32_WS.WS_EX_CLIENTEDGE;
                lStyle &= ~User32_WS.WS_EX_STATICEDGE;
            }

            User32Interop.SetWindowLong(hwnd, User32_WS.GWL_EXSTYLE, lStyle);
            User32Interop.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, (uint)(PositioningFlags.SWP_FRAMECHANGED | PositioningFlags.SWP_NOMOVE | PositioningFlags.SWP_NOSIZE | PositioningFlags.SWP_NOZORDER | PositioningFlags.SWP_NOOWNERZORDER));

            if (gen.KeepAspectRatio || gen.KeepMonitorAspectRatio)
            {
                if (gen.KeepMonitorAspectRatio)
                {
                    origWidth = owner.MonitorBounds.Width;
                    origHeight = owner.MonitorBounds.Height;
                }
                else
                {
                    if (GetWindowRect(hwnd, out RECT Rect))
                    {
                        origWidth = Rect.Right - Rect.Left;
                        origHeight = Rect.Bottom - Rect.Top;
                    }
                }

                double newWidth = playerBoundsWidth;
                double newHeight = playerBoundsHeight;

                if (newWidth < origWidth)
                {
                    if (origHeight > 0 && origWidth > 0)
                    {
                        origRatio = (double)origWidth / origHeight;

                        newHeight = (newWidth / origRatio);

                        if (newHeight > playerBoundsWidth)
                        {
                            newHeight = playerBoundsWidth;
                        }
                    }
                }
                else
                {
                    if (origHeight > 0 && origWidth > 0)
                    {
                        origRatio = (double)origWidth / origHeight;

                        newWidth = (newHeight * origRatio);

                        if (newWidth > playerBoundsHeight)
                        {
                            newWidth = playerBoundsHeight;
                        }
                    }
                }
                size.Width = (int)newWidth;
                size.Height = (int)newHeight;

                if (newWidth < origWidth)
                {
                    int yOffset = Convert.ToInt32(loc.Y + ((playerBoundsHeight - newHeight) / 2));
                    loc.Y = yOffset;
                }
                if (newHeight < origHeight)
                {
                    int xOffset = Convert.ToInt32(loc.X + ((playerBoundsWidth - newWidth) / 2));
                    loc.X = xOffset;
                }
            }

            if(!gen.DontResize)
            {
                Log(string.Format("Resizing this game window and keeping aspect ratio. Values: width:{0}, height:{1}, aspectratio:{2}, origwidth:{3}, origheight:{4}, plyrboundwidth:{5}, plyrboundheight:{6}", size.Width, size.Height, origRatio, origWidth, origHeight, playerBoundsWidth, playerBoundsHeight));
                WindowScrape.Static.HwndInterface.SetHwndSize(hwnd, size.Width, size.Height);
            }

            if(!gen.DontReposition)
            {
                Log(string.Format("Repostioning this game window to coords x:{0},y:{1}", loc.X, loc.Y));
                WindowScrape.Static.HwndInterface.SetHwndPos(hwnd, loc.X, loc.Y);
            }

            //User32Util.HideTaskbar();
            if(!gen.NotTopMost)
            {
                Log("Setting this game window to top most");
                WindowScrape.Static.HwndInterface.MakeTopMost(hwnd);
            }

			//Set up raw input window.
			var window = CreateRawInputWindow(proc, players[playerIndex]);

            Thread.Sleep(1000);

            if (gen.HookFocus || gen.SetWindowHook || gen.HideCursor || gen.PreventWindowDeactivation || gen.SupportsMultipleKeyboardsAndMice)
            {
                Log("Injecting post-launch hooks for this process");
                //InjectDLLs(proc, window);
                //Thread.Sleep(1000);

                if (gen.PreventWindowDeactivation && !isPrevent)
                {
                    Log("PreventWindowDeactivation detected, setting flag");
                    isPrevent = true;
                }

                if (isPrevent)
                {
                    if (players[playerIndex].IsKeyboardPlayer && gen.KeyboardPlayerSkipPreventWindowDeactivate)
                    {
                        Log("Ignoring PreventWindowDeactivation for keyboard player");
                        gen.PreventWindowDeactivation = false;
                    }
                    else
                    {
                        Log("Keeping PreventWindowDeactivation on");
                        gen.PreventWindowDeactivation = true;
                    }
                }

                if (gen.PostHookInstances?.Length > 0)
                {
                    string[] instancesToHook = gen.PostHookInstances.Split(',');
                    foreach (string instanceToHook in instancesToHook)
                    {
                        if (int.Parse(instanceToHook) == (playerIndex + 1))
                        {
                            User32Interop.SetForegroundWindow(proc.MainWindowHandle);
                            InjectDLLs(proc, window);
                        }
                    }
                }
                else
                {
                    User32Interop.SetForegroundWindow(proc.MainWindowHandle);
                    InjectDLLs(proc, window);
                }
            }
        }

        private void ChangeIPPerInstance(int i)
        {
            Log(string.Format("Changing IP for instance {0}", i+1));
            if (i == 0)
            {
                if(string.IsNullOrEmpty(iniNetworkInterface) || iniNetworkInterface == "Automatic")
                {
                    Log("No network interface provided, attempting to automatically find it");
                    var ni = NetworkInterface.GetAllNetworkInterfaces();
                    foreach (NetworkInterface item in ni)
                    {
                        if (item.OperationalStatus == OperationalStatus.Up)
                        {
                            foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                            {
                                if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                                {
                                    string ipAddress = ip.Address.ToString();
                                    if (ipAddress == context.LocalIP)
                                    {
                                        iniNetworkInterface = item.Name;
                                        Log("Found network interface: " + iniNetworkInterface);
                                    }
                                }
                            }
                        }
                    }
                }

                if(iniNetworkInterface == null)
                {
                    Log("ERROR - Unable to resolve network interface");
                    MessageBox.Show("Unable to resolve network interface", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                try
                {
                    var networkInterface = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(nw => nw.Name == iniNetworkInterface);
                    var ipProperties = networkInterface.GetIPProperties();
                    var ipInfo = ipProperties.UnicastAddresses.FirstOrDefault(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork);
                    currentIPaddress = ipInfo.Address.ToString();
                    currentSubnetMask = ipInfo.IPv4Mask.ToString();
                    currentGateway = ipProperties.GatewayAddresses?.FirstOrDefault(g => g.Address.AddressFamily.ToString() == "InterNetwork")?.Address.ToString();
                    isDHCPenabled = ipProperties.GetIPv4Properties().IsDhcpEnabled;
                    isDynamicDns = ipProperties.IsDynamicDnsEnabled;
                    IPAddressCollection dnsServers = ipProperties.DnsAddresses;
                    foreach (IPAddress dns in dnsServers)
                    {
                        if(dns.AddressFamily == AddressFamily.InterNetwork)
                        {
                            dnsAddresses.Add(dns.ToString());
                            dnsServersStr += dns.ToString() + " ";
                        }
                    }
                    Log("Default IP settings for NetworkInterface: " + iniNetworkInterface + ", IP: " + currentIPaddress + " Subnet Mask: " + currentSubnetMask + " Default Gateway: " + currentGateway + " DHCP: " + isDHCPenabled + " Dnyamic DNS: " + isDynamicDns + " DNS: " + dnsServersStr);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Obtaining default IP settings error. " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            MessageBox.Show("WARNING: This feature is highly experimental!\n\nYour computers IP is about to be changed. You may receive a prompt immediately after to complete this action. Your IP settings will be reverted back to normal upon exiting Nucleus normally. However, if Nucleus crashes, it is not gauranteed that your settings will be set back.\n\nPress OK when ready to have your IP changed.\n\nOriginal Settings:\nNetworkInterface: " + iniNetworkInterface + "\nIP: " + currentIPaddress + "\nSubnet Mask: " + currentSubnetMask + "\nDefault Gateway: " + currentGateway + "\nDHCP: " + isDHCPenabled + "\nDynamic DNS: " + isDynamicDns + "\nDNS: " + dnsServersStr, "Nucleus - Change IP Per Instance", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            string ipNetwork = currentIPaddress.Substring(0, currentIPaddress.LastIndexOf('.') + 1);

            Ping pingSender = new Ping();
            for(int a = 0; a < 10; a++)
            {
                PingReply reply = pingSender.Send(ipNetwork + (hostAddr + i).ToString(), 1000);

                if (reply.Status == IPStatus.Success)
                {
                    hostAddr++;
                    continue;
                }
                else
                {
                    break;
                }
            }

            string shostAddr = (hostAddr + i).ToString();
            Log("Changing IP to: " + ipNetwork + shostAddr);
            SetIP(iniNetworkInterface, ipNetwork + shostAddr, currentSubnetMask, currentGateway);
            //if(isDynamicDns)
            //{
            //    SetDNS(iniNetworkInterface, false);
            //}
            //hostAddr++;
        }

        private bool SetIP(string networkInterfaceName, string ipAddress, string subnetMask, string gateway = null)
        {
            var networkInterface = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(nw => nw.Name == networkInterfaceName);
            var ipProperties = networkInterface.GetIPProperties();
            var ipInfo = ipProperties.UnicastAddresses.FirstOrDefault(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork);
            var currentIPaddress = ipInfo.Address.ToString();
            var currentSubnetMask = ipInfo.IPv4Mask.ToString();
            var isDHCPenabled = ipProperties.GetIPv4Properties().IsDhcpEnabled;

            if (!isDHCPenabled && currentIPaddress == ipAddress && currentSubnetMask == subnetMask)
                return true;    // no change necessary

            string args = $"interface ip set address \"{networkInterfaceName}\" static {ipAddress} {subnetMask} " + (string.IsNullOrWhiteSpace(gateway) ? "" : $"{gateway} 1");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo("netsh", args) { Verb = "runas" }
            };
            process.Start();
            process.WaitForExit();
            var successful = process.ExitCode == 0;
            process.Dispose();
            return successful;
        }

        private bool SetDHCP(string networkInterfaceName)
        {
            var networkInterface = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(nw => nw.Name == networkInterfaceName);
            var ipProperties = networkInterface.GetIPProperties();
            var isDHCPenabled = ipProperties.GetIPv4Properties().IsDhcpEnabled;

            if (isDHCPenabled)
                return true;    // no change necessary

            var process = new Process
            {
                StartInfo = new ProcessStartInfo("netsh", $"interface ip set address \"{networkInterfaceName}\" dhcp") { Verb = "runas" }
            };
            process.Start();
            process.WaitForExit();
            var successful = process.ExitCode == 0;
            process.Dispose();
            return successful;
        }

        //private bool SetDNS(string networkInterfaceName, bool useDhcp)
        //{
        //    var networkInterface = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(nw => nw.Name == networkInterfaceName);
        //    var ipProperties = networkInterface.GetIPProperties();
        //    var isDHCPenabled = ipProperties.GetIPv4Properties().IsDhcpEnabled;

        //    if (isDHCPenabled)
        //        return true;    // no change necessary

        //    string args = $"interface ip set dns \"{networkInterfaceName}\" static {dnsAddresses[0]}";
        //    if (useDhcp)
        //    {
        //        args = $"interface ip set dns \"{networkInterfaceName}\" dhcp";
        //    }

        //    var process = new Process
        //    {
        //        StartInfo = new ProcessStartInfo("netsh", args) { Verb = "runas" }
        //    };
        //    process.Start();
        //    process.WaitForExit();
        //    var successful = process.ExitCode == 0;
        //    process.Dispose();
        //    return successful;
        //}

        private void SelBtn_Click(object sender, EventArgs e)
        {
            Control control = (Control)sender;
            Form ppform = control.FindForm();

            if (control.GetType() == typeof(ListBox))
            {
                var listBox = control as ListBox;
                if (listBox.SelectedItem == null)
                {
                    MessageBox.Show("No process has been selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else if (control.GetType() == typeof(Button))
            {

                foreach (Control c in ppform.Controls)
                {
                    if (c.GetType() == typeof(ListBox))
                    {
                        var listBox = c as ListBox;
                        if (listBox.SelectedItem == null)
                        {
                            MessageBox.Show("No process has been selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                }
            }

            ppform.Close();
        }

        private void RefrshBtn_Click(object sender, EventArgs e)
        {
            Control control = (Button)sender;

            Form ppform = control.FindForm();
            foreach (Control c in ppform.Controls)
            {
                if (c.GetType() == typeof(ListBox))
                {
                    var listBox = c as ListBox;
                    listBox.Items.Clear();

                    Process[] allProc = Process.GetProcesses();
                    foreach (Process p in allProc)
                    {
                        if (p.Id == 0 || string.IsNullOrEmpty(p.MainWindowTitle))
                        {
                            continue;
                        }
                        if (attachedIds.Contains(p.Id))
                        {
                            if (p.ProcessName.ToLower().Contains(Path.GetFileNameWithoutExtension(gen.ExecutableName).ToLower()) || (gen.LauncherExe?.Length > 0 && p.ProcessName.ToLower().Contains(Path.GetFileNameWithoutExtension(gen.LauncherExe).ToLower())))
                            {
                                listBox.Items.Insert(0, p.Id + " - (DO NOT USE - Already assigned in Nucleus)" + p.ProcessName);
                            }
                            else
                            {
                                listBox.Items.Add(p.Id + " - (DO NOT USE - Already assigned in Nucleus)" + p.ProcessName);
                            }
                        }
                        else
                        {
                            if (p.ProcessName.ToLower().Contains(Path.GetFileNameWithoutExtension(gen.ExecutableName).ToLower()) || (gen.LauncherExe?.Length > 0 && p.ProcessName.ToLower().Contains(Path.GetFileNameWithoutExtension(gen.LauncherExe).ToLower())))
                            {
                                listBox.Items.Insert(0, p.Id + " - " + p.ProcessName);
                            }
                            else
                            {
                                listBox.Items.Add(p.Id + " - " + p.ProcessName);
                            }
                        }
                    }
                }
            }
        }

        public static void proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(e.Data))
                {
                    readToEnd = true;
                    return;
                }

                if (e.Data.Contains("+connect_lobby") && string.IsNullOrEmpty(lobbyConnectArg))
                {
                    string toFind1 = "+connect_lobby ";
                    int start = e.Data.IndexOf(toFind1);
                    string string2 = e.Data.Substring(start);
                    lobbyConnectArg = string2;
                    readToEnd = true;
                    return;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Goldberg Lobby Connect output data error. " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

		//private void InjectDLLs(Process proc)
		//{
		private void InjectDLLs(Process proc, Window window)
		{
			WaitForProcWindowHandleNotZero(proc);

			bool is64 = EasyHook.RemoteHooking.IsX64Process(proc.Id);
			string currDir = Directory.GetCurrentDirectory();

			bool windowNull = (window == null);

            //using (StreamWriter writer = new StreamWriter("important.txt", true))
            //{
            //    writer.WriteLine("aproc id: {0}, aproc procname: {1}, title: {2}, handle: {3}, handleint: {4}, bytefour: {5}, byteeight: {6}, datatosend[8]: {7}, datatosend[9]: {8}, intptr: {9}", proc.Id, proc.ProcessName, proc.MainWindowTitle, proc.MainWindowHandle, (int)proc.MainWindowHandle, BitConverter.ToUInt32(dataToSend, 0), BitConverter.ToUInt64(dataToSend, 0), dataToSend[8], dataToSend[9], intPtr);
            //}

            try
            {
                string injectorPath = Path.Combine(currDir, $"Nucleus.IJ{(is64 ? "x64" : "x86")}.exe");
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = injectorPath;

				object[] args = new object[]
				{
					1, // Tier. 0 == start up hook, 1 == runtime hook
		            proc.Id, // Target PID
		            0, // WakeUp Thread ID
		            0, // InInjectionOptions (EasyHook)
		            "Nucleus.Hook32.dll", // lib path x86. Inject32/64 will decide which one to use, so pass in both
		            "Nucleus.Hook64.dll", // lib path x64
		            proc.MainWindowHandle, // Game hWnd
		            gen.HookFocus, // Hook GetForegroundWindow/etc
		            gen.HideCursor,
					isDebug,
					nucleusFolderPath, // Primarily for log output
		            gen.SetWindowHook, // SetWindow hook (prevents window from moving)
					gen.PreventWindowDeactivation,

					//These options are enabled by default, but if the game isn't using these features the hooks are unwanted
					gen.SupportsMultipleKeyboardsAndMice && gen.HookSetCursorPos,
					gen.SupportsMultipleKeyboardsAndMice && gen.HookGetCursorPos,
					gen.SupportsMultipleKeyboardsAndMice && gen.HookGetKeyState,
					gen.SupportsMultipleKeyboardsAndMice && gen.HookGetAsyncKeyState,
					gen.SupportsMultipleKeyboardsAndMice && gen.HookGetKeyboardState,
					gen.SupportsMultipleKeyboardsAndMice && gen.HookFilterRawInput,
					gen.SupportsMultipleKeyboardsAndMice && gen.HookFilterMouseMessages,
					gen.SupportsMultipleKeyboardsAndMice && gen.HookUseLegacyInput,
					!gen.HookDontUpdateLegacyInMouseMsg,
					gen.SupportsMultipleKeyboardsAndMice && gen.HookMouseVisibility,
					gen.HookReRegisterRawInput,
					gen.HookReRegisterRawInputMouse,
					gen.HookReRegisterRawInputKeyboard,

					windowNull ? "" : (window.HookPipe?.pipeNameWrite ?? ""),
					windowNull ? "" : (window.HookPipe?.pipeNameRead ?? ""),
					windowNull ? -1 : window.MouseAttached.ToInt32(),
					windowNull ? -1 : window.KeyboardAttached.ToInt32()
				};

				var sbArgs = new StringBuilder();
				foreach (object arg in args)
				{
					//Converting to base64 prevents characters like " or \ breaking the arguments
					string arg64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(arg.ToString()));

					sbArgs.Append(" \"");
					sbArgs.Append(arg64);
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
			catch (Exception ex)
			{
                Log(string.Format("ERROR - {0}", ex.Message));
            }
        }

        private void SendFocusMsgs()
        {
            List<Process> fakeFocusProcs = new List<Process>();
            var windows = RawInputManager.windows;
            string ffPIDs = "";

            if (gen.FakeFocusInstances?.Length > 0)
            {
                string[] fakeFocusInstances = gen.FakeFocusInstances.Split(',');
                for (int i = 0; i < fakeFocusInstances.Length; i++)
                {
                    if(int.Parse(fakeFocusInstances[i]) <= numPlayers)
                    {
                        fakeFocusProcs.Add(attached[int.Parse(fakeFocusInstances[i]) - 1]);
                    }
                }
            }
            else
            {
                fakeFocusProcs = attached;
            }

            if (gen.KeyboardPlayerSkipFakeFocus)
            {
                for (int i = 0; i < fakeFocusProcs.Count; i++)
                {
                    if (fakeFocusProcs[i].Id == keyboardProcId)
                    {
                        fakeFocusProcs.RemoveAt(i);
                    }
                }
            }

            foreach (Process p in fakeFocusProcs)
            {
                ffPIDs = ffPIDs + p.Id + " ";
            }

			try
			{
				while (true)
				{
					Thread.Sleep(gen.FakeFocusInterval);

					foreach (Process proc in attached)
					{
						//Deep Rock Galactic doesn't work with this message
						if (gen.FakeFocusSendActivate)
						{
							//User32Interop.SendMessage(proc.MainWindowHandle, (int) FocusMessages.WM_ACTIVATE, (IntPtr) 0x00000001, (IntPtr) proc.MainWindowHandle);
							User32Interop.SendMessage(proc.MainWindowHandle, (int)FocusMessages.WM_ACTIVATE, (IntPtr)0x00000002, IntPtr.Zero);
						}

						User32Interop.SendMessage(proc.MainWindowHandle, (int)FocusMessages.WM_ACTIVATEAPP, (IntPtr)1, IntPtr.Zero);
						User32Interop.SendMessage(proc.MainWindowHandle, (int)FocusMessages.WM_NCACTIVATE, (IntPtr)0x00000001, IntPtr.Zero);
						User32Interop.SendMessage(proc.MainWindowHandle, (int)FocusMessages.WM_SETFOCUS, IntPtr.Zero, IntPtr.Zero);
						User32Interop.SendMessage(proc.MainWindowHandle, (int)FocusMessages.WM_MOUSEACTIVATE, (IntPtr)proc.MainWindowHandle, (IntPtr)1);
					}

					if (gen.PreventGameFocus)
					{
						foreach (var window in windows)
						{
							window.HookPipe.SendPreventForegroundWindow();
						}
					}
				}
			}
			catch (Exception e)
			{
				Logger.WriteLine($"ThreadAbortException in FakeFocus. Exiting. Error: {e}");
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

            for (; ; )
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

                if (p.IsKeyboardPlayer && !p.IsRawKeyboard)
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

                            if (!gen.PromptBetweenInstances)
                            {
                                if(!gen.NotTopMost)
                                {
                                    Log("Setting game window to top most");
                                    data.HWnd.TopMost = true;
                                }
                            }


                            if (data.Status == 2)
                            {

                                Log("Removing game window border for pid " + data.Process.Id);
                                uint lStyle = User32Interop.GetWindowLong(data.HWnd.NativePtr, User32_WS.GWL_STYLE);
                                if(gen.WindowStyleValues?.Length > 0)
                                {
                                    Log("Using user custom window style");
                                    foreach(string val in gen.WindowStyleValues)
                                    {
                                        if(val.StartsWith("~"))
                                        {
                                            lStyle &= ~Convert.ToUInt32(val.Substring(1), 16);
                                        }
                                        else
                                        {
                                            lStyle |= Convert.ToUInt32(val, 16);
                                        }
                                    }
                                }
                                else
                                {
                                    lStyle &= ~User32_WS.WS_CAPTION;
                                    lStyle &= ~User32_WS.WS_THICKFRAME;
                                    lStyle &= ~User32_WS.WS_MINIMIZE;
                                    lStyle &= ~User32_WS.WS_MAXIMIZE;
                                    lStyle &= ~User32_WS.WS_SYSMENU;
                                }
                                User32Interop.SetWindowLong(data.HWnd.NativePtr, User32_WS.GWL_STYLE, lStyle);

                                lStyle = User32Interop.GetWindowLong(data.HWnd.NativePtr, User32_WS.GWL_EXSTYLE);

                                if (gen.ExtWindowStyleValues?.Length > 0)
                                {
                                    Log("Using user custom extended window style");
                                    foreach (string val in gen.ExtWindowStyleValues)
                                    {
                                        if (val.StartsWith("~"))
                                        {
                                            lStyle &= ~Convert.ToUInt32(val.Substring(1), 16);
                                        }
                                        else
                                        {
                                            lStyle |= Convert.ToUInt32(val, 16);
                                        }
                                    }
                                }
                                else
                                {
                                    lStyle &= ~User32_WS.WS_EX_DLGMODALFRAME;
                                    lStyle &= ~User32_WS.WS_EX_CLIENTEDGE;
                                    lStyle &= ~User32_WS.WS_EX_STATICEDGE;
                                }

                                User32Interop.SetWindowLong(data.HWnd.NativePtr, User32_WS.GWL_EXSTYLE, lStyle);
                                User32Interop.SetWindowPos(data.HWnd.NativePtr, IntPtr.Zero, 0, 0, 0, 0, (uint)(PositioningFlags.SWP_FRAMECHANGED | PositioningFlags.SWP_NOMOVE | PositioningFlags.SWP_NOSIZE | PositioningFlags.SWP_NOZORDER | PositioningFlags.SWP_NOOWNERZORDER));
                                //User32Interop.SetForegroundWindow(data.HWnd.NativePtr);
                                //User32Interop.SetWindowPos(data.HWnd.NativePtr, new IntPtr(-2), 0, 0, 0, 0, (uint)(PositioningFlags.SWP_NOSIZE | PositioningFlags.SWP_NOMOVE));
                                
                                //Minimise and un-minimise the window. Fixes black borders in Minecraft, but causing stretching issues in games like Minecraft.
                                if (gen.RefreshWindowAfterStart)
                                {
	                                ShowWindow(data.HWnd.NativePtr, 6);
	                                ShowWindow(data.HWnd.NativePtr, 9);
                                }


                                //User32Interop.SetWindowPos(data.HWnd.NativePtr, IntPtr.Zero, 0, 0, 0, 0, (uint)(PositioningFlags.SWP_NOSIZE | PositioningFlags.SWP_NOMOVE));

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
                                if (!gen.KeepAspectRatio && !gen.KeepMonitorAspectRatio && !dllRepos && !gen.DontResize)
                                {
                                    Log(string.Format("Repostioning game window for pid {0} to coords x:{1},y:{2}", data.Process.Id, data.Position.X, data.Position.Y));
                                    data.HWnd.Location = data.Position;
                                }

                                data.Status++;
                                Debug.WriteLine("State 1");

                                if (gen.LockMouse)
                                {
                                    if (p.IsKeyboardPlayer && !p.IsRawKeyboard)
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
                                if (!dllResize && !gen.DontResize)
                                {
                                    if (gen.KeepAspectRatio || gen.KeepMonitorAspectRatio)
                                    {

                                        if (gen.KeepMonitorAspectRatio)
                                        {
                                            origWidth = owner.MonitorBounds.Width;
                                            origHeight = owner.MonitorBounds.Height;
                                        }
                                        else
                                        {
                                            if (GetWindowRect(data.Process.MainWindowHandle, out RECT Rect))
                                            {
                                                origWidth = Rect.Right - Rect.Left;
                                                origHeight = Rect.Bottom - Rect.Top;
                                            }
                                        }

                                        double newWidth = playerBoundsWidth;
                                        double newHeight = playerBoundsHeight;

                                        if(newWidth < origWidth)
                                        {
                                            if (origHeight > 0 && origWidth > 0)
                                            {
                                                origRatio = (double)origWidth / origHeight;

                                                newHeight = (newWidth / origRatio);

                                                if (newHeight > playerBoundsHeight)
                                                {
                                                    newHeight = playerBoundsHeight;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (origHeight > 0 && origWidth > 0)
                                            {
                                                origRatio = (double)origWidth / origHeight;

                                                newWidth = (newHeight * origRatio);

                                                if (newWidth > playerBoundsWidth)
                                                {
                                                    newWidth = playerBoundsWidth;
                                                }
                                            }
                                        }

                                        Log(string.Format("Resizing game window for pid {0} and keeping aspect ratio. Values: width:{1}, height:{2}, aspectratio:{3}, origwidth:{4}, origheight:{5}, plyrboundwidth:{6}, plyrboundheight:{7}", data.Process.Id, (int)newWidth, (int)newHeight, (Math.Truncate(origRatio * 100) / 100), origWidth, origHeight, playerBoundsWidth, playerBoundsHeight));
                                        data.HWnd.Size = new Size(Convert.ToInt32(newWidth), Convert.ToInt32(newHeight));

                                        //x horizontal , y vertical
                                        if(newWidth < origWidth)
                                        {
                                            int yOffset = Convert.ToInt32(data.Position.Y + ((playerBoundsHeight - newHeight) / 2));
                                            data.Position.Y = yOffset;
                                        }
                                        if(newHeight < origHeight)
                                        {
                                            int xOffset = Convert.ToInt32(data.Position.X + ((playerBoundsWidth - newWidth) / 2));
                                            data.Position.X = xOffset;
                                        }

                                        Log(string.Format("Resizing game window (for horizontal centering), coords x:{1},y:{2}", data.Process.Id, data.Position.X, data.Position.Y));
                                        data.HWnd.Location = data.Position;
                                    }
                                    else
                                    {
                                        Log(string.Format("Resizing game window for pid {0} to the following width:{1}, height:{2}", data.Process.Id, data.Size.Width, data.Size.Height));
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
                                else
                                {
                                    data.HWnd = new HwndObject(data.Process.MainWindowHandle);
                                    Point pos = data.HWnd.Location;

                                    Size s = data.Size;
                                    data.Setted = true;
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
                    End(false);
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

		private IntPtr WaitForProcWindowHandleNotZero(Process proc)
		{
			try
			{
				if ((int)proc.MainWindowHandle == 0)
				{
					for (int times = 0; times < 200; times++)
					{
						Thread.Sleep(500);
						if ((int)proc.MainWindowHandle > 0)
						{
							break;
						}

						if (times == 199 && (int)proc.MainWindowHandle == 0)
						{
							Log(string.Format(
								"ERROR - WaitForProcWindowHandleNotZero could not find main window handle for {0} (pid {1})",
								proc.ProcessName, proc.Id));
						}
					}
				}

				return proc.MainWindowHandle;
			}
			catch
			{
				Log("ERROR - WaitForProcWindowHandleNotZero encountered an exception");
				return (IntPtr)(-1);
			}
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

        private void GoldbergLobbyConnect()
        {
            MessageBox.Show("Goldberg Lobby Connect: Press OK after you are hosting a game.", "Nucleus - Goldberg Lobby Connect", MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = Path.Combine(Directory.GetCurrentDirectory(), "utils\\GoldbergEmu\\lobby_connect\\lobby_connect.exe");

            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            Process p = Process.Start(startInfo);
            p.OutputDataReceived += proc_OutputDataReceived;
            p.BeginOutputReadLine();

            while (readToEnd == false)
            {
                Thread.Sleep(25);
            }
            try
            {
                Thread.Sleep(2500);
                p.Kill();
            }
            catch (Exception ex)
            { MessageBox.Show(ex.Message); }


            if (!string.IsNullOrEmpty(lobbyConnectArg))
            {
                //MessageBox.Show(lobbyConnectArg);
                Log("Goldberg Lobby Connect: Setting lobby ID to " + lobbyConnectArg.Substring(15));
            }
            else
            {
                Log("Goldberg Lobby Connect: Could not find lobby ID");
                MessageBox.Show("Goldberg Lobby Connect: Could not find lobby ID.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void ResetWindows(ProcessData processData, int x, int y, int w, int h, int i)
        {
            Log("Attempting to repoisition, resize and strip borders for instance " + (i - 1));
            //MessageBox.Show("Going to attempt to reposition and resize instance " + (i - 1));
            try
            {
                if(!gen.DontReposition)
                    processData.HWnd.Location = new Point(x, y);
                if(!gen.DontResize)
                    processData.HWnd.Size = new Size(w, h);

                uint lStyle = User32Interop.GetWindowLong(processData.HWnd.NativePtr, User32_WS.GWL_STYLE);
                if (gen.WindowStyleValues?.Length > 0)
                {
                    Log("Using user custom window style");
                    foreach (string val in gen.WindowStyleValues)
                    {
                        if (val.StartsWith("~"))
                        {
                            lStyle &= ~Convert.ToUInt32(val.Substring(1), 16);
                        }
                        else
                        {
                            lStyle |= Convert.ToUInt32(val, 16);
                        }
                    }
                }
                else
                {
                    lStyle &= ~User32_WS.WS_CAPTION;
                    lStyle &= ~User32_WS.WS_THICKFRAME;
                    lStyle &= ~User32_WS.WS_MINIMIZE;
                    lStyle &= ~User32_WS.WS_MAXIMIZE;
                    lStyle &= ~User32_WS.WS_SYSMENU;
                }
                User32Interop.SetWindowLong(processData.HWnd.NativePtr, User32_WS.GWL_STYLE, lStyle);

                lStyle = User32Interop.GetWindowLong(processData.HWnd.NativePtr, User32_WS.GWL_EXSTYLE);

                if (gen.ExtWindowStyleValues?.Length > 0)
                {
                    Log("Using user custom extended window style");
                    foreach (string val in gen.ExtWindowStyleValues)
                    {
                        if (val.StartsWith("~"))
                        {
                            lStyle &= ~Convert.ToUInt32(val.Substring(1), 16);
                        }
                        else
                        {
                            lStyle |= Convert.ToUInt32(val, 16);
                        }
                    }
                }
                else
                {
                    lStyle &= ~User32_WS.WS_EX_DLGMODALFRAME;
                    lStyle &= ~User32_WS.WS_EX_CLIENTEDGE;
                    lStyle &= ~User32_WS.WS_EX_STATICEDGE;
                }


                User32Interop.SetWindowLong(processData.HWnd.NativePtr, User32_WS.GWL_EXSTYLE, lStyle);
                User32Interop.SetWindowPos(processData.HWnd.NativePtr, IntPtr.Zero, 0, 0, 0, 0, (uint)(PositioningFlags.SWP_FRAMECHANGED | PositioningFlags.SWP_NOMOVE | PositioningFlags.SWP_NOSIZE | PositioningFlags.SWP_NOZORDER | PositioningFlags.SWP_NOOWNERZORDER));

            }
            catch (Exception ex)
            {
                Log("ERROR - ResetWindows was unsuccessful for instance " + (i - 1) + ". " + ex.Message);
            }

            try
            {
	            if ((processData.HWnd.Location != new Point(x, y) && !gen.DontReposition) || (processData.HWnd.Size != new Size(w, h) && !gen.DontResize))
	            {
		            Log("ERROR - ResetWindows was unsuccessful for instance " + (i - 1));
	            }
            }
            catch(Exception e)
			{
				Log("ERROR - Exception in ResetWindows for instance " + (i - 1) + ", error = " + e);
			}
        }

        private void CustomDllEnabled(GenericContext context, PlayerInfo player, Rectangle playerBounds, int i)
        {
            Log(string.Format("Setting up Custom DLL, UseAlpha8CustomDll: {0}", gen.Hook.UseAlpha8CustomDll));
            byte[] xdata;
            if (gen.Hook.UseAlpha8CustomDll && !gameIs64)
            {
                xdata = Properties.Resources.xinput1_3;
            }
            else
            {
                if (gen.Hook.UseAlpha8CustomDll)
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
                //}
            }

            if (context.Hook.XInputNames == null)
            {
                Log(string.Format("Writing custom dll xinput1_3.dll to {0}", instanceExeFolder));
                using (Stream str = File.OpenWrite(Path.Combine(instanceExeFolder, "xinput1_3.dll")))
                {
                    str.Write(xdata, 0, xdata.Length);
                }
                if (File.Exists(Path.Combine(instanceExeFolder, "xinput1_3.dll")))
                {
                    addedFiles.Add(Path.Combine(instanceExeFolder, "xinput1_3.dll"));
                }
            }
            else
            {
                string[] xinputs = context.Hook.XInputNames;
                for (int z = 0; z < xinputs.Length; z++)
                {
                    string xinputName = xinputs[z];
                    Log(string.Format("Writing custom dll {0} to {1}", xinputName, instanceExeFolder));
                    using (Stream str = File.OpenWrite(Path.Combine(instanceExeFolder, xinputName)))
                    {
                        str.Write(xdata, 0, xdata.Length);
                    }

                    if (File.Exists(Path.Combine(instanceExeFolder, xinputName)))
                    {
                        addedFiles.Add(Path.Combine(instanceExeFolder, xinputName));
                    }
                }
            }

            Log(string.Format("Writing ncoop.ini to {0} with Game.Hook values", instanceExeFolder));
            string ncoopIni = Path.Combine(instanceExeFolder, "ncoop.ini");
            using (Stream str = File.OpenWrite(ncoopIni))
            {
                byte[] ini = Properties.Resources.ncoop;
                str.Write(ini, 0, ini.Length);
            }

            if (File.Exists(Path.Combine(instanceExeFolder, "ncoop.ini")))
            {
                addedFiles.Add(Path.Combine(instanceExeFolder, "ncoop.ini"));
            }

            IniFile x360 = new IniFile(ncoopIni);
            x360.IniWriteValue("Options", "Log", "0");
            x360.IniWriteValue("Options", "FileLog", "0");
            x360.IniWriteValue("Options", "ForceFocus", gen.Hook.ForceFocus.ToString(CultureInfo.InvariantCulture));
            if (!gen.Hook.UseAlpha8CustomDll)
            {
                x360.IniWriteValue("Options", "Version", "2");
                x360.IniWriteValue("Options", "ForceFocusWindowRegex", gen.Hook.ForceFocusWindowName.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                string windowTitle = gen.Hook.ForceFocusWindowName;
                if (gen.IdInWindowTitle || gen.FlawlessWidescreen?.Length > 0)
                {
                    windowTitle = gen.Hook.ForceFocusWindowName + "(" + i + ")";
                    if (!string.IsNullOrEmpty(gen.FlawlessWidescreen))
                    {
                        windowTitle = "Nucleus Instance " + (i + 1) + "(" + gen.Hook.ForceFocusWindowName + ")";
                    }
                }
                x360.IniWriteValue("Options", "ForceFocusWindowName", windowTitle.ToString(CultureInfo.InvariantCulture));
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

            if (context.Hook.ResWidth > 0 && context.Hook.ResHeight > 0)
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

        private void CopyCustomUtils(int i, string linkFolder)
        {
            Log("Copying custom files/folders");
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
                    if (!instances.Contains(i))
                    {
                        continue;
                    }
                }

                string source_dir = Directory.GetCurrentDirectory() + "\\utils\\User\\" + utilPath;
                FileAttributes attr = File.GetAttributes(source_dir);

                if (attr.HasFlag(FileAttributes.Directory)) //directory
                {
                    string destination_dir = linkFolder.TrimEnd('\\') + '\\' + utilPath;

                    foreach (string dir in System.IO.Directory.GetDirectories(source_dir, "*", SearchOption.AllDirectories))
                    {
                        Directory.CreateDirectory(System.IO.Path.Combine(destination_dir, dir.Substring(source_dir.Length + 1)));
                    }

                    Log("Copying user folder " + utilPath + " and all its contents to " + "Instance" + i + "\\" + utilPath);
                    foreach (string file_name in System.IO.Directory.GetFiles(source_dir, "*", SearchOption.AllDirectories))
                    {
                        File.Copy(file_name, System.IO.Path.Combine(destination_dir, file_name.Substring(source_dir.Length + 1)));
                    }
                }
                else //file
                {
                    if (File.Exists(Path.Combine(linkFolder.TrimEnd('\\') + '\\' + utilPath, Path.GetFileName(utilName))))
                    {
                        File.Delete(Path.Combine(linkFolder.TrimEnd('\\') + '\\' + utilPath, Path.GetFileName(utilName)));
                    }
                    Log("Copying " + utilName + " to " + "Instance" + i + "\\" + utilPath);
                    File.Copy(Path.Combine(utilFolder, utilName), Path.Combine(linkFolder.TrimEnd('\\') + '\\' + utilPath, Path.GetFileName(utilName)), true);
                }

                if (File.Exists(Path.Combine(linkFolder.TrimEnd('\\') + '\\' + utilPath, Path.GetFileName(utilName))))
                {
                    addedFiles.Add(Path.Combine(linkFolder.TrimEnd('\\') + '\\' + utilPath, Path.GetFileName(utilName)));
                }
            }
            Log("Copying custom files complete");
        }

        private void UseDirectX9Wrapper()
        {
            Log("Copying over DirectX 9, Direct 3D Wrapper (d3d9.dll) to instance executable folder");
            string utilFolder = Path.Combine(Directory.GetCurrentDirectory(), "utils\\DirectXWrapper");

            if (File.Exists(Path.Combine(instanceExeFolder, "d3d9.dll")))
            {
                File.Delete(Path.Combine(instanceExeFolder, "d3d9.dll"));
            }
            File.Copy(Path.Combine(utilFolder, "d3d9.dll"), Path.Combine(instanceExeFolder, "d3d9.dll"), true);
            if (File.Exists(Path.Combine(instanceExeFolder, "d3d9.dll")))
            {
                addedFiles.Add(Path.Combine(instanceExeFolder, "d3d9.dll"));
            }
        }

        private void UseX360ce(int i, List<PlayerInfo> players, PlayerInfo player, GenericContext context, bool setupDll)
        {
            Log("Setting up x360ce");
            string x360exe = "";
            string x360dll = "";
            string utilFolder = Path.Combine(Directory.GetCurrentDirectory(), "utils\\x360ce");

            //else
            //{
            //    Log(string.Format("ERROR - Machine type {0} not implemented", GetDllMachineType(exePath)));
            //}

            string[] x360cedlls = { "xinput1_3.dll" };
            if (gen.X360ceDll?.Length > 0)
            {
                x360cedlls = gen.X360ceDll;
            }

            if(setupDll)
            {
                foreach (string x360ceDllName in x360cedlls)
                {
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

                        if (x360ceDllName.ToLower().StartsWith("dinput"))
                        {
                            if (gameIs64)
                            {
                                x360dll = "dinput8_x64.dll";
                            }
                            else
                            {
                                x360dll = "dinput8.dll";
                            }
                        }

                        if (File.Exists(Path.Combine(instanceExeFolder, x360exe)))
                        {
                            File.Delete(Path.Combine(instanceExeFolder, x360exe));
                        }
                        Log("Copying over " + x360exe);
                        try
                        {
                            File.Copy(Path.Combine(utilFolder, x360exe), Path.Combine(instanceExeFolder, x360exe), true);
                        }
                        catch
                        {
                            Log("Using alternative copy method");
                            CmdUtil.ExecuteCommand(utilFolder, out int exitCode, "copy \"" + Path.Combine(utilFolder, x360exe) + "\" \"" + Path.Combine(instanceExeFolder, x360exe) + "\"");
                        }

                        if (File.Exists(Path.Combine(instanceExeFolder, x360ceDllName)))
                        {
                            File.Delete(Path.Combine(instanceExeFolder, x360ceDllName));
                        }
                        if (x360dll != x360ceDllName)
                        {
                            Log("Copying over " + x360dll + " and renaming it to " + x360ceDllName);
                        }
                        else
                        {
                            Log("Copying over " + x360dll);
                        }
                        try
                        {
                            File.Copy(Path.Combine(utilFolder, x360dll), Path.Combine(instanceExeFolder, x360ceDllName), true);
                        }
                        catch
                        {
                            Log("Using alternative copy method");
                            CmdUtil.ExecuteCommand(utilFolder, out int exitCode, "copy \"" + Path.Combine(utilFolder, x360dll) + "\" \"" + Path.Combine(instanceExeFolder, x360dll) + "\"");
                        }

                    }
                    else
                    {
                        if (gen.SymlinkGame || gen.HardlinkGame || gen.HardcopyGame)
                        {
                            Log("Carrying over " + x360ceDllName + " from Instance0");
                            if (File.Exists(Path.Combine(instanceExeFolder, x360ceDllName)))
                            {
                                File.Delete(Path.Combine(instanceExeFolder, x360ceDllName));
                            }

                            File.Copy(Path.Combine(instanceExeFolder.Substring(0, instanceExeFolder.LastIndexOf('\\') + 1) + "Instance0", x360ceDllName), Path.Combine(instanceExeFolder, x360ceDllName), true);
                        }
                    }

                    if (File.Exists(Path.Combine(instanceExeFolder, x360ceDllName)))
                    {
                        addedFiles.Add(Path.Combine(instanceExeFolder, x360ceDllName));
                    }
                }
            }

            if (i > 0 && (gen.SymlinkGame || gen.HardlinkGame || gen.HardcopyGame))
            {
                Log("Carrying over x360ce.ini from Instance0");
                if (File.Exists(Path.Combine(instanceExeFolder, "x360ce.ini")))
                {
                    File.Delete(Path.Combine(instanceExeFolder, "x360ce.ini"));
                }
                File.Copy(Path.Combine(instanceExeFolder.Substring(0, instanceExeFolder.LastIndexOf('\\') + 1) + "Instance0", "x360ce.ini"), Path.Combine(instanceExeFolder, "x360ce.ini"), true);
            }
            else
            {
                Log("Starting x360ce process");
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.UseShellExecute = true;
                startInfo.WorkingDirectory = instanceExeFolder;
                startInfo.FileName = Path.Combine(instanceExeFolder, x360exe);
                //if (gen.RunAsAdmin)
                //{
                //    startInfo.Verb = "runas";
                //}
                Process util = Process.Start(startInfo);
                Log("Waiting until x360ce process is exited");
                util.WaitForExit();
            }


            //string[] change = new string[] {
            //    context.FindLineNumberInTextFile(Path.Combine(instanceExeFolder, "x360ce.ini"), "PAD1=", SearchType.StartsWith) + "|PAD1=" + context.x360ceGamepadGuid,
            //    context.FindLineNumberInTextFile(Path.Combine(instanceExeFolder, "x360ce.ini"), "PAD2=", SearchType.StartsWith) + "|PAD2=",
            //    context.FindLineNumberInTextFile(Path.Combine(instanceExeFolder, "x360ce.ini"), "PAD3=", SearchType.StartsWith) + "|PAD3=",
            //    context.FindLineNumberInTextFile(Path.Combine(instanceExeFolder, "x360ce.ini"), "PAD4=", SearchType.StartsWith) + "|PAD4="
            //};

            if (!File.Exists(Path.Combine(instanceExeFolder, "x360ce.ini")))
            {
                Log("x360ce.ini has not been generated. Copying default x360ce.ini from utils");
                try
                {
                    File.Copy(Path.Combine(utilFolder, "x360ce.ini"), Path.Combine(instanceExeFolder, "x360ce.ini"), true);
                }
                catch
                {
                    Log("Using alternative copy method");
                    CmdUtil.ExecuteCommand(utilFolder, out int exitCode, "copy \"" + Path.Combine(utilFolder, "x360ce.ini") + "\" \"" + Path.Combine(instanceExeFolder, "x360ce.ini") + "\"");
                }
            }

            Log("Making changes to x360ce.ini; PAD mapping to player");

            List<string> textChanges = new List<string>();

            if (!player.IsKeyboardPlayer)
            {
                Thread.Sleep(1000);
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

            if (gen.XboxOneControllerFix)
            {
                Thread.Sleep(1000);
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

            if (File.Exists(Path.Combine(instanceExeFolder, "x360ce.ini")))
            {
                addedFiles.Add(Path.Combine(instanceExeFolder, "x360ce.ini"));
            }
            Log("x360ce setup complete");
        }

        private void UseDevReorder(string garch, PlayerInfo player, List<PlayerInfo> players, int i, bool setupDll)
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
            //}

            if(setupDll)
            {
                if (File.Exists(Path.Combine(instanceExeFolder, "dinput8.dll")))
                {
                    File.Delete(Path.Combine(instanceExeFolder, "dinput8.dll"));
                }
                Log("Copying dinput8.dll");
                File.Copy(Path.Combine(utilFolder, garch + "\\dinput8.dll"), Path.Combine(instanceExeFolder, "dinput8.dll"), true);
                if (File.Exists(Path.Combine(instanceExeFolder, "dinput8.dll")))
                {
                    addedFiles.Add(Path.Combine(instanceExeFolder, "dinput8.dll"));
                }
            }

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
                if (p != i)
                {
                    iniConfig.Add("{" + players[p].GamepadGuid + "}");
                }
            }
            Log("Writing devreorder.ini with the only visible gamepad guid: " + player.GamepadGuid);
            File.WriteAllLines(Path.Combine(instanceExeFolder, "devreorder.ini"), iniConfig.ToArray());
            if (File.Exists(Path.Combine(instanceExeFolder, "devreorder.ini")))
            {
                addedFiles.Add(Path.Combine(instanceExeFolder, "devreorder.ini"));
            }
            Log("devreorder setup complete");
        }

        private void SetupXInputPlusDll(string garch, PlayerInfo player, GenericContext context, int i, bool setupDll)
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
            //}
            if(setupDll)
            {
                foreach (string xinputDllName in gen.XInputPlusDll)
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
                    if(File.Exists(Path.Combine(instanceExeFolder, xinputDllName)))
                    {
                        addedFiles.Add(Path.Combine(instanceExeFolder, xinputDllName));
                    }
                }
            }

            List<string> textChanges = new List<string>();
            if (!player.IsKeyboardPlayer || (player.IsKeyboardPlayer && gen.PlayersPerInstance <= 1))
            {
                if (File.Exists(Path.Combine(instanceExeFolder, "XInputPlus.ini")))
                {
                    File.Delete(Path.Combine(instanceExeFolder, "XInputPlus.ini"));
                }
                Log("Copying XInputPlus.ini");
                File.Copy(Path.Combine(utilFolder, "XInputPlus.ini"), Path.Combine(instanceExeFolder, "XInputPlus.ini"), true);

                if (File.Exists(Path.Combine(instanceExeFolder, "XInputPlus.ini")))
                {
                    addedFiles.Add(Path.Combine(instanceExeFolder, "XInputPlus.ini"));
                }

                Log("Making changes to the lines in XInputPlus.ini; FileVersion and Controller values");

                if (gen.XInputPlusDll.ToList().Any(val => val.StartsWith("dinput") == true)) //(xinputDll.ToLower().StartsWith("dinput"))
                {
                    Log("A Dinput dll has been detected, also enabling X2Dinput in XInputPlus.ini");
                    textChanges.Add(context.FindLineNumberInTextFile(Path.Combine(instanceExeFolder, "XInputPlus.ini"), "EnableX2Dinput=", SearchType.StartsWith) + "|EnableX2Dinput=True");
                }

                textChanges.Add(context.FindLineNumberInTextFile(Path.Combine(instanceExeFolder, "XInputPlus.ini"), "FileVersion=", SearchType.StartsWith) + "|FileVersion=" + garch);

                if (!player.IsKeyboardPlayer)
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
                        textChanges.Add(context.FindLineNumberInTextFile(Path.Combine(instanceExeFolder, "XInputPlus.ini"), "Controller1=", SearchType.StartsWith) + "|Controller1=" + (i + kbi));
                    }
                }
                else
                {
                    Log("Skipping setting controller value for this instance, as this player is using keyboard");
                    kbi = 0;
                }

                context.ReplaceLinesInTextFile(Path.Combine(instanceExeFolder, "XInputPlus.ini"), textChanges.ToArray());
            }

            Log("XInput Plus setup complete");
        }

        private void CreateSteamAppIdByExe()
        {
            Log("Creating steam_appid.txt with steam ID " + gen.SteamID + " at " + instanceExeFolder);
            if (File.Exists(Path.Combine(instanceExeFolder, "steam_appid.txt")))
            {
                File.Delete(Path.Combine(instanceExeFolder, "steam_appid.txt"));
            }
            File.WriteAllText(Path.Combine(instanceExeFolder, "steam_appid.txt"), gen.SteamID);
        }

        private void UseGoldberg(string rootFolder, string nucleusRootFolder, string linkFolder, int i, PlayerInfo player, List<PlayerInfo> players)
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


                if (gen.UseNucleusEnvironment && gen.GoldbergNoLocalSave)
                {
                    instanceSteamSettingsFolder = $@"C:\Users\{Environment.UserName}\NucleusCoop\{player.Nickname}\AppData\Roaming\Goldberg SteamEmu Saves\settings\";
                }
                else
                {
                    instanceSteamSettingsFolder = Path.Combine(instanceSteamDllFolder, "settings");
                }

                //instanceSteam_SettingsFolder = Path.Combine(instanceSteamDllFolder, "steam_settings");

                //if(Directory.Exists(instanceSteamSettingsFolder))
                //{
                //    Directory.Delete(instanceSteamSettingsFolder);
                //}

                Directory.CreateDirectory(instanceSteamSettingsFolder);
                //Directory.CreateDirectory(instanceSteam_SettingsFolder);

                if (nameFile.EndsWith("steam_api64.dll", true, null))
                {
                    try
                    {
                        if (File.Exists(Path.Combine(instanceSteamDllFolder, "steam_api64.dll")))
                        {
                            if (gen.GoldbergExperimental && gen.GoldbergExperimentalRename)
                            {
                                if (File.Exists(Path.Combine(instanceSteamDllFolder, "cracksteam_api64.dll")))
                                {
                                    Log("cracksteam_api64.dll already exists in instance folder, deleting");
                                    File.Delete(Path.Combine(instanceSteamDllFolder, "cracksteam_api64.dll"));
                                }
                                Log("Renaming steam_api64.dll to cracksteam_api64.dll");
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
                    catch (Exception ex)
                    {
                        Log("ERROR - " + ex.Message);
                        Log("Using alternative copy method for steam_api64.dll");
                        CmdUtil.ExecuteCommand(utilFolder, out int exitCode, "copy \"" + Path.Combine(utilFolder, steam64Dll) + "\" \"" + Path.Combine(instanceSteamDllFolder, "steam_api64.dll") + "\"");
                    }
                }

                if (nameFile.EndsWith("steam_api.dll", true, null))
                {
                    try
                    {
                        if (File.Exists(Path.Combine(instanceSteamDllFolder, "steam_api.dll")))
                        {
                            if (gen.GoldbergExperimental && gen.GoldbergExperimentalRename)
                            {
                                if (File.Exists(Path.Combine(instanceSteamDllFolder, "cracksteam_api.dll")))
                                {
                                    Log("cracksteam_api.dll already exists in instance folder, deleting it and then renaming");
                                    File.Delete(Path.Combine(instanceSteamDllFolder, "cracksteam_api.dll"));
                                }
                                Log("Renaming steam_api.dll to cracksteam_api.dll");
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
                    catch (Exception ex)
                    {
                        Log("ERROR - " + ex.Message);
                        Log("Using alternative copy method for steam_api.dll");
                        CmdUtil.ExecuteCommand(utilFolder, out int exitCode, "copy \"" + Path.Combine(utilFolder, steamDll) + "\" \"" + Path.Combine(instanceSteamDllFolder, "steam_api.dll") + "\"");
                    }
                }

                if (gen.GoldbergExperimental)
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

                    if (ini.IniReadValue("Misc", "UseNicksInGame") == "True" && player.IsKeyboardPlayer && !player.IsRawKeyboard && ini.IniReadValue("ControllerMapping", "Keyboard") != "")
                    {
                        File.WriteAllText(Path.Combine(instanceSteamSettingsFolder, "account_name.txt"), ini.IniReadValue("ControllerMapping", "Keyboard"));
                        Log("Generating account_name.txt with nickname " + ini.IniReadValue("ControllerMapping", "Keyboard"));
                    }
                    else
                    {
                        File.WriteAllText(Path.Combine(instanceSteamSettingsFolder, "account_name.txt"), "Player " + (i + 1));
                        Log("Generating account_name.txt with nickname Player " + (i + 1));
                    }
                }

                long steamID = random_steam_id + i;
                if(gen.PlayerSteamIDs != null)
                {
                    if(i < gen.PlayerSteamIDs.Length && !string.IsNullOrEmpty(gen.PlayerSteamIDs[i]))
                    {
                        Log("Using a manually entered steam ID");
                        steamID = long.Parse(gen.PlayerSteamIDs[i]);
                    }
                }

                Log("Generating user_steam_id.txt with random user steam ID " + (steamID).ToString());
                if (File.Exists(Path.Combine(instanceSteamSettingsFolder, "user_steam_id.txt")))
                {
                    File.Delete(Path.Combine(instanceSteamSettingsFolder, "user_steam_id.txt"));
                }
                File.WriteAllText(Path.Combine(instanceSteamSettingsFolder, "user_steam_id.txt"), (steamID).ToString());

                string lang = "english";
                if (ini.IniReadValue("Misc", "SteamLang") != "" && ini.IniReadValue("Misc", "SteamLang") != "Automatic")
                {
                    gen.GoldbergLanguage = ini.IniReadValue("Misc", "SteamLang").ToLower();
                }
                if (gen.GoldbergLanguage?.Length > 0)
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
                if (!gen.GoldbergNoLocalSave)
                {
                    File.WriteAllText(Path.Combine(instanceSteamDllFolder, "local_save.txt"), "");
                }

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

                        if ((i + 1) == players.Count)
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

            if (steamDllFiles == null || steamDllFiles.Length < 1)
            {
                Log("Unable to locate a steam_api(64).dll file, Goldberg will not be used");
                MessageBox.Show("Goldberg was unable to locate a steam_api(64).dll file. The built-in Goldberg will not be used.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            Log("Goldberg setup complete");
        }

        private void UseSteamStubDRMPatcher(string garch)
        {
            string utilFolder = Path.Combine(Directory.GetCurrentDirectory(), "utils\\Steam Stub DRM Patcher");

            string archToUse = garch;
            if (gen.SteamStubDRMPatcherArch?.Length > 0)
            {
                archToUse = "x" + gen.SteamStubDRMPatcherArch;
            }

            try
            {
                if (File.Exists(Path.Combine(instanceExeFolder, "winmm.dll")))
                {
                    File.Delete(Path.Combine(instanceExeFolder, "winmm.dll"));
                }
                Log(string.Format("Copying over winmm.dll ({0})", archToUse));
                File.Copy(Path.Combine(utilFolder, archToUse + "\\winmm.dll"), Path.Combine(instanceExeFolder, "winmm.dll"), true);
            }

            catch (Exception ex)
            {
                Log("ERROR - " + ex.Message);
                Log("Using alternative copy method for winmm.dll");
                CmdUtil.ExecuteCommand(utilFolder, out int exitCode, "copy \"" + Path.Combine(utilFolder, archToUse + "\\winmm.dll") + "\" \"" + Path.Combine(instanceExeFolder, "winmm.dll") + "\"");
            }

            //if (File.Exists(Path.Combine(instanceExeFolder, "winmm.dll")))
            //{
            //    addedFiles.Add(Path.Combine(instanceExeFolder, "winmm.dll"));
            //}
        }

        private void HexEditFile(GenericContext context, int i, string linkFolder)
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

        private void HexEditAllFiles(GenericContext context, string linkFolder)
        {
            foreach (string asciiValues in gen.HexEditAllFiles)
            {
                string[] splitValues = asciiValues.Split('|');
                if (splitValues.Length == 3)
                {
                    string filePath = splitValues[0];
                    string fullPath = Path.Combine(Path.Combine(linkFolder, filePath));
                    string fullFileName = Path.GetFileName(filePath);
                    string strToSearch = splitValues[1];
                    string replacedStr = splitValues[2];
                    Log(string.Format("HexEditAllFiles - Patching file: {0}", filePath));

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

        private void HexEditExe(GenericContext context, int i)
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

        private void HexEditAllExes(GenericContext context, int i)
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

        private void HexEditExeAddress(string exePath, int i)
        {
            if(gen.SymlinkExe)
            {
                Log("Skipping HexEditExeAddress, " + Path.GetFileName(exePath) + " is symlinked");
                return;
            }

            Log("HexEditExeAddress - Patching executable, " + Path.GetFileName(exePath) + ", in instance folder");
            
            foreach (string hexSplitLine in gen.HexEditExeAddress)
            {
                string[] hexSplit = hexSplitLine.Split('|');
                int indexOffset = 1;
                if (hexSplit.Length == 3)
                {
                    if (int.Parse(hexSplit[0]) != (i + 1))
                    {
                        continue;
                    }
                    indexOffset = 0;
                }
                else if (hexSplit.Length == 1)
                {
                    Log("Invalid # of parameters provided for: " + hexSplitLine + ", skipping");
                    continue;
                }

                //bool origExists = false;
                //string exeSuffix = "-ORIG.exe";
                //if (File.Exists(Path.Combine(Path.GetDirectoryName(exePath), Path.GetFileNameWithoutExtension(exePath) + "-ORIG.exe")))
                //{
                //    origExists = true;
                //    exeSuffix = "-TEMP.exe";
                //}

                //if(File.Exists(exePath))
                //{
                //    Log(string.Format("Renaming original executable {0} to {1}", Path.GetFileName(exePath), Path.GetFileNameWithoutExtension(exePath) + exeSuffix));
                //    File.Move(exePath, Path.Combine(Path.GetDirectoryName(exePath), Path.GetFileNameWithoutExtension(exePath) + exeSuffix));
                //}

                Log(string.Format("Bytes at address '{0}' to be replaced with '{1}'", hexSplit[1 - indexOffset], hexSplit[2 - indexOffset]));
                List<byte> bytesConv = new List<byte>();
                for (int s = 0; s < hexSplit[2 - indexOffset].Length; s += 2)
                {
                    bytesConv.Add(Convert.ToByte(hexSplit[2 - indexOffset].Substring(s,2), 16));
                }
                byte[] bArray = bytesConv.ToArray();

                using (Stream stream = File.Open(exePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    stream.Position = long.Parse(hexSplit[1 - indexOffset], NumberStyles.HexNumber);
                    stream.Write(bArray, 0, bArray.Length);
                }
                
                //if(origExists)
                //{
                //    Log(string.Format("Deleting temporary executable {0}", Path.GetFileNameWithoutExtension(exePath) + "-TEMP.exe"));
                //    File.Delete(Path.Combine(Path.GetDirectoryName(exePath), Path.GetFileNameWithoutExtension(exePath) + "-TEMP.exe"));
                //}
            }
        }

        private void HexEditFileAddress(string linkFolder, int i)
        {
            

            foreach (string hexSplitLine in gen.HexEditFileAddress)
            {
                string[] hexSplit = hexSplitLine.Split('|');
                int indexOffset = 1;
                if (hexSplit.Length == 4)
                {
                    if (int.Parse(hexSplit[0]) != (i + 1))
                    {
                        continue;
                    }
                    indexOffset = 0;
                }
                else if (hexSplit.Length <= 2)
                {
                    Log("Invalid # of parameters provided for: " + hexSplitLine + ", skipping hex edit file address");
                    continue;
                }

                string fullFilePath = Path.Combine(linkFolder, hexSplit[1 - indexOffset]);
                FileInfo pathInfo = new FileInfo(fullFilePath);
                if (pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    Log("Skipping HexEditFileAddress, " + Path.GetFileName(hexSplit[1 - indexOffset]) + " is symlinked");
                    continue;
                }

                //bool origExists = false;
                //string exeSuffix = "-ORIG.exe";
                //if (File.Exists(Path.Combine(Path.GetDirectoryName(exePath), Path.GetFileNameWithoutExtension(exePath) + "-ORIG.exe")))
                //{
                //    origExists = true;
                //    exeSuffix = "-TEMP.exe";
                //}

                //if(File.Exists(exePath))
                //{
                //    Log(string.Format("Renaming original executable {0} to {1}", Path.GetFileName(exePath), Path.GetFileNameWithoutExtension(exePath) + exeSuffix));
                //    File.Move(exePath, Path.Combine(Path.GetDirectoryName(exePath), Path.GetFileNameWithoutExtension(exePath) + exeSuffix));
                //}

                if(File.Exists(fullFilePath))
                {
                    Log("HexEditFileAddress - Patching file, " + Path.GetFileName(fullFilePath) + ", in instance folder");
                    Log(string.Format("Bytes at address '{0}' to be replaced with '{1}'", hexSplit[2 - indexOffset], hexSplit[3 - indexOffset]));
                    List<byte> bytesConv = new List<byte>();
                    for (int s = 0; s < hexSplit[3 - indexOffset].Length; s += 2)
                    {
                        bytesConv.Add(Convert.ToByte(hexSplit[3 - indexOffset].Substring(s, 2), 16));
                    }
                    byte[] bArray = bytesConv.ToArray();

                    using (Stream stream = File.Open(fullFilePath, FileMode.Open, FileAccess.ReadWrite))
                    {
                        stream.Position = long.Parse(hexSplit[2 - indexOffset], NumberStyles.HexNumber);
                        stream.Write(bArray, 0, bArray.Length);
                    }
                }
                else
                {
                    Log("ERROR - Could not find file: " + fullFilePath + " to rename");
                }

                //if(origExists)
                //{
                //    Log(string.Format("Deleting temporary executable {0}", Path.GetFileNameWithoutExtension(exePath) + "-TEMP.exe"));
                //    File.Delete(Path.Combine(Path.GetDirectoryName(exePath), Path.GetFileNameWithoutExtension(exePath) + "-TEMP.exe"));
                //}
            }
        }

        private void UserProfileConfigCopy(PlayerInfo player)
        {
            //UserProfileConfigPath = gen.UserProfileConfigPath;
            string nucConfigPath = Path.Combine($@"C:\Users\{Environment.UserName}\NucleusCoop\{player.Nickname}\", gen.UserProfileConfigPath);
            string realConfigPath = Path.Combine(Environment.GetEnvironmentVariable("userprofile"), gen.UserProfileConfigPath);
            if ((!Directory.Exists(nucConfigPath) && Directory.Exists(realConfigPath)) || (Directory.Exists(realConfigPath) && gen.ForceUserProfileConfigCopy))
            {
                Directory.CreateDirectory(nucConfigPath);

                Log("Config path " + gen.UserProfileConfigPath + " does not exist for Nucleus nickname " + player.Nickname + " environment. Copying folder and all its contents over");
                foreach (string dir in Directory.GetDirectories(realConfigPath, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(Path.Combine(nucConfigPath, dir.Substring(realConfigPath.Length + 1)));
                }

                foreach (string file_name in Directory.GetFiles(realConfigPath, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        File.Copy(file_name, Path.Combine(nucConfigPath, file_name.Substring(realConfigPath.Length + 1)));
                        Log("Copying file " + Path.GetFileName(file_name));
                    }
                    catch
                    {
                        //Log("ERROR - " + ex.Message);
                        Log("Using alternative copy method for " + file_name);
                        CmdUtil.ExecuteCommand(Path.GetDirectoryName(Path.Combine(nucConfigPath, file_name.Substring(realConfigPath.Length + 1))), out int exitCode, "copy \"" + file_name + "\" \"" + Path.Combine(nucConfigPath, file_name.Substring(realConfigPath.Length + 1)) + "\"", false);
                    }
                }
            }
        }

        private void UserProfileSaveCopy(PlayerInfo player)
        {
            //UserProfileSavePath = gen.UserProfileSavePath;
            string nucSavePath = Path.Combine($@"C:\Users\{Environment.UserName}\NucleusCoop\{player.Nickname}\", gen.UserProfileSavePath);
            string realSavePath = Path.Combine(Environment.GetEnvironmentVariable("userprofile"), gen.UserProfileSavePath);
            if ((!Directory.Exists(nucSavePath) && Directory.Exists(realSavePath)) || (Directory.Exists(realSavePath) && gen.ForceUserProfileSaveCopy))
            {
                Directory.CreateDirectory(nucSavePath);

                Log("Save path " + gen.UserProfileConfigPath + " does not exist in Nucleus nickname " + player.Nickname + " environment. Copying folder and all its contents over");
                foreach (string dir in Directory.GetDirectories(realSavePath, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(Path.Combine(nucSavePath, dir.Substring(realSavePath.Length + 1)));
                }

                foreach (string file_name in Directory.GetFiles(realSavePath, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        File.Copy(file_name, Path.Combine(nucSavePath, file_name.Substring(realSavePath.Length + 1)));
                        Log("Copying file " + Path.GetFileName(file_name));
                    }
                    catch
                    {
                        //Log("ERROR - " + ex.Message);
                        Log("Using alternative copy method for " + file_name);
                        CmdUtil.ExecuteCommand(Path.GetDirectoryName(Path.Combine(nucSavePath, file_name.Substring(realSavePath.Length + 1))), out int exitCode, "copy \"" + file_name + "\" \"" + Path.Combine(nucSavePath, file_name.Substring(realSavePath.Length + 1)) + "\"", false);
                    }
                }
            }
        }

        private void ChangeExe(int i)
        {
            string newExe = Path.GetFileNameWithoutExtension(this.userGame.Game.ExecutableName) + " - Player " + (i + 1) + ".exe";
            if (File.Exists(Path.Combine(instanceExeFolder, newExe)))
            {
                File.Delete(Path.Combine(instanceExeFolder, newExe));
            }
            File.Move(Path.Combine(instanceExeFolder, this.userGame.Game.ExecutableName), Path.Combine(instanceExeFolder, newExe));
            exePath = Path.Combine(instanceExeFolder, newExe);
            Log("Changed game executable from " + gen.ExecutableName + " to " + newExe);
        }
    }
}