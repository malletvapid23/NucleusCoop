﻿using Nucleus;
using Nucleus.Gaming;
using Nucleus.Gaming.Windows;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Collections;
using System.Linq;

namespace StartGame
{
    class Program
    {
        private static int tries = 5;
        private static int tri = 0;
        private static Process proc;
        private static string mt;
        private static bool partialMutex;

        private static bool isHook;
        private static bool isDelay;
        private static bool renameMutex;
        private static bool setWindow;
        private static bool blockRaw;
        //private static bool runAdmin;

        private static string root;
        private static string currentDir;
        private static string destination;
        private static string[] dirExclusions;
        private static string[] fileExclusions;
        private static string[] fileCopyInstead;
        private static bool hardLink;
        private static bool symFolders;
        private static int numPlayers;
        private static string playerNick;
        private static bool useNucleusEnvironment;
        private static bool injectFailed;

        //private static bool gameIs64 = false;

        private static string mutexToRename;

        //private static string rawHid;

        private static bool isDebug;
        private static string nucleusFolderPath;

        private static uint pOutPID = 0;

        private static readonly IniFile ini = new Nucleus.Gaming.IniFile(Path.Combine(Directory.GetCurrentDirectory(), "Settings.ini"));

        [DllImport("EasyHook64.dll", CharSet = CharSet.Ansi)]
        private static extern int RhCreateAndInject(
            [MarshalAsAttribute(UnmanagedType.LPWStr)] string InEXEPath,
            [MarshalAsAttribute(UnmanagedType.LPWStr)] string InCommandLine,
            uint InProcessCreationFlags,
            uint InInjectionOptions,
            [MarshalAsAttribute(UnmanagedType.LPWStr)] string InLibraryPath_x86,
            [MarshalAsAttribute(UnmanagedType.LPWStr)] string InLibraryPath_x64,
            IntPtr InPassThruBuffer,
            uint InPassThruSize,
            IntPtr OutProcessId //Pointer to a UINT (the PID of the new process)
            );

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

        [DllImport("kernel32.dll")]
        static extern uint ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        static extern int SuspendThread(IntPtr hThread);


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

        [DllImport("user32.dll")]
        static extern uint WaitForInputIdle(IntPtr hProcess, uint dwMilliseconds);

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

        static void Log(string logMessage)
        {
            if (ini.IniReadValue("Misc", "DebugLog") == "True")
            {
                using (StreamWriter writer = new StreamWriter("debug-log.txt", true))
                {
                    writer.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}]STARTGAME: {logMessage}");
                    writer.Close();
                }
            }
        }

        static void StartGame(string path, string args = "", string workingDir = null)
        {
            System.IO.Stream str = new System.IO.MemoryStream();
            //GenericGameInfo gen = new GenericGameInfo(null, null, str);
            bool regMethod = false;

            if (!Path.IsPathRooted(path))
            {
                string root = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                path = Path.Combine(root, path);
            }

            


            try
            {
                //proc = Process.Start(startInfo);
                //string currDir = Directory.GetCurrentDirectory();

				// This works even if the current directory is set elsewhere.
                string currDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                //bool is64 = EasyHook.RemoteHooking.IsX64Process((int)pi.dwProcessId);
                //if (Is64Bit(path) == true)
                //{
                //    gameIs64 = true;
                //}

                //bool is64 = EasyHook.RemoteHooking.IsX64Process((int)pi.dwProcessId);

                IntPtr envPtr = IntPtr.Zero;
                uint pCreationFlags = 0;

                if (useNucleusEnvironment)
                {
                    Log("Setting up Nucleus environment");
                    var sb = new StringBuilder();
                    IDictionary envVars = Environment.GetEnvironmentVariables();
                    var username = Environment.UserName;
                    envVars["USERPROFILE"] = $@"C:\Users\{username}\NucleusCoop\{playerNick}";
                    envVars["HOMEPATH"] = $@"\Users\{username}\NucleusCoop\{playerNick}";
                    envVars["APPDATA"] = $@"C:\Users\{username}\NucleusCoop\{playerNick}\AppData\Roaming";
                    envVars["LOCALAPPDATA"] = $@"C:\Users\{username}\NucleusCoop\{playerNick}\AppData\Local";

                    //Some games will crash if the directories don't exist
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

                    pCreationFlags += (uint)ProcessCreationFlags.CREATE_UNICODE_ENVIRONMENT;
                }

                STARTUPINFO startup = new STARTUPINFO();
                startup.cb = Marshal.SizeOf(startup);

                if (isHook || renameMutex || setWindow || blockRaw)
                {
                    Log("Starting game and injecting start up hooks via Nucleus.Inject");

                    bool? is64_n = Is64Bit(path);

					if (!is64_n.HasValue)
					{
						Log(string.Format("ERROR - Machine type {0} not implemented", GetDllMachineType(path)));
						return;
					}

					bool is64 = is64_n.Value;

                    PROCESS_INFORMATION procInfo = new PROCESS_INFORMATION();

                    try
					{
                        //if(useNucleusEnvironment)
                        //{
                        //    bool success = CreateProcess(null, path + " " + args, IntPtr.Zero, IntPtr.Zero, false, (uint)ProcessCreationFlags.CREATE_SUSPENDED | (uint)ProcessCreationFlags.CREATE_UNICODE_ENVIRONMENT, envPtr, Path.GetDirectoryName(path), ref startup, out PROCESS_INFORMATION processInformation);
                        //    if (!success)
                        //    {
                        //        Log(string.Format("ERROR - CreateProcess failed - startGamePath: {0}, startArgs: {1}, dirpath: {2}", path, args, Path.GetDirectoryName(path)));
                        //        return;
                        //    }

                        //    procInfo = processInformation;
                        //    pOutPID = (uint)processInformation.dwProcessId;
                        //}

                        //Thread.Sleep(1000);

                        string injectorPath = Path.Combine(currDir, $"Nucleus.IJ{(is64 ? "x64" : "x86")}.exe");
						ProcessStartInfo injstartInfo = new ProcessStartInfo();
						injstartInfo.FileName = injectorPath;
                        object[] injargs = new object[]
                        {
                                0, // Tier 0 : start up hook
								path, // EXE path
								args, // Command line arguments. TODO: these args should be converted to base64 to prevent any special characters e.g. " breaking the injargs
								pCreationFlags, // Process creation flags
								0, // InInjectionOptions (EasyHook)
								Path.Combine(currDir, "Nucleus.SHook32.dll"), // lib path 32
								Path.Combine(currDir, "Nucleus.SHook64.dll"), // lib path 64
								isHook, // Window hooks
								renameMutex, // Renames mutexes/semaphores/events hook
								mutexToRename,
                                setWindow, // Set window hook
								isDebug,
                                nucleusFolderPath,
                                blockRaw,
                                useNucleusEnvironment,
                                playerNick
                        };

						var sbArgs = new StringBuilder();
						foreach (object arg in injargs)
						{
							sbArgs.Append(" \"");
							sbArgs.Append(arg);
							sbArgs.Append("\"");
						}

						string arguments = sbArgs.ToString();
						injstartInfo.Arguments = arguments;
						//injstartInfo.WindowStyle = ProcessWindowStyle.Hidden;
						//injstartInfo.CreateNoWindow = true;
						injstartInfo.UseShellExecute = false;
						injstartInfo.RedirectStandardOutput = true;
                        injstartInfo.RedirectStandardInput = true;

						Process injectProc = Process.Start(injstartInfo);

                        injectProc.OutputDataReceived += proc_OutputDataReceived;
						injectProc.BeginOutputReadLine();
						injectProc.WaitForExit();

                    }
					catch (Exception ex)
					{
						Log(string.Format("ERROR - {0}", ex.Message));
					}

                    if (injectFailed)
                    {
                        injectFailed = false;
                        throw new Exception("Failed to create and/or inject start up hook dll.");
                    }

                    //if(useNucleusEnvironment)
                    //{
                    //    ResumeThread(procInfo.hThread);
                    //}
                }
                else // regular method (no hooks)
                {
                    Log("Starting game via regular process start method (no start up hooks enabled)");

                    bool success = CreateProcess(null, path + " " + args, IntPtr.Zero, IntPtr.Zero, false, (uint)ProcessCreationFlags.CREATE_UNICODE_ENVIRONMENT, envPtr, Path.GetDirectoryName(path), ref startup, out PROCESS_INFORMATION processInformation);
                    if (!success)
                    {
                        Log(string.Format("ERROR - CreateProcess failed - startGamePath: {0}, startArgs: {1}, dirpath: {2}", path, args, Path.GetDirectoryName(path)));
                        return;
                    }

                    pOutPID = (uint)processInformation.dwProcessId;
                    proc = Process.GetProcessById((int)pOutPID);
                    //pOutPID = proc.Id;
                    regMethod = true;
                }

                while (pOutPID == 0)
                {
                    Thread.Sleep(50);
                }

                //Thread.Sleep(100);
                bool isRunning = Process.GetProcesses().Any(x => x.Id == (int)pOutPID);
                //if (proc != null && proc.Threads[0].ThreadState != System.Diagnostics.ThreadState.Running)
                if(!isRunning)
                {
                    Log("Process with ID " + pOutPID + " is not yet running. Checking every 50 miliseconds for 10 seconds to see if process is running yet.");
                    for (int times = 0; times < 200; times++)
                    {
                        Thread.Sleep(50);
                        isRunning = Process.GetProcesses().Any(x => x.Id == (int)pOutPID);
                        if (isRunning)
                        {
                            Log("Attempt #" + times + " - Process is now running");
                            proc = Process.GetProcessById((int)pOutPID);
                            break;
                        }
                        if (times == 199 && !isRunning)
                        {
                            Log(string.Format("ERROR - Process with an id of {0} is not running after 10 seconds. Aborting.", pOutPID));
                            throw new Exception(string.Format("ERROR - Process with an id of {0} is not running after 10 seconds. Aborting.", pOutPID));
                        }
                    }
                }

                ConsoleU.WriteLine("Game started, process ID:" + pOutPID /*Marshal.ReadInt32(pid)*/ /*proc.Id*/ /*(int)pi.dwProcessId*/, Palette.Success);
                if (isDebug)
                {
                    //if (regMethod)
                    //{
                        Thread.Sleep(100);
                        Log(string.Format("Game started, process ID: {0}", pOutPID));
                    //}
                    //else
                    //{
                    //    Thread.Sleep(100);
                    //    Log(string.Format("Game started, process ID: {0}", pOutPID));
                    //}
                }
                
            }

            catch (Exception ex)
            {
                tri++;
                if (tri < tries)
                {
                    if(!ex.Message.Contains("debug-log"))
                    {
                        Log(string.Format("ERROR - Failed to start process. EXCEPTION: {0} STACKTRACE: {1}", ex.Message, ex.StackTrace));
                        Console.WriteLine("Failed to start process. Retrying...");
                    }
                    StartGame(path, args);
                }
            }
        }

        public static void proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data))
            {
                return;
            }
            if(e.Data.Equals("injectfailed"))
            {
                return;
            }
            //Log($"Redirected output: {e.Data}");
            //Thread.Sleep(100);
            Console.WriteLine($"Redirected output: {e.Data}");
            uint.TryParse(e.Data, out pOutPID);
        }

        static void Main(string[] args)
        {
            // We need this, else Windows will fake
            // all the data about monitors inside the application
            User32Util.SetProcessDpiAwareness(ProcessDPIAwareness.ProcessPerMonitorDPIAware);

            if (args.Length == 0)
            {
                ConsoleU.WriteLine("Invalid usage! Need arguments to proceed!", Palette.Error);
                return;
            }

#if RELEASE
            try
#endif
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i];
                    ConsoleU.WriteLine("Parsing line " + i + ": " + arg, Palette.Feedback);
                    //Log(string.Format("Parsing line {0}: {1}", i, arg));

                    string argument = "";
                    for (int j = i; j < args.Length; j++)
                    {
                        string skey = args[j];
                        if (!skey.Contains("monitors")
                             && !skey.Contains("game")
                             && !skey.Contains("partialmutex")
                             && !skey.Contains("mutextype")
                             && !skey.Contains("mutex")
                             && !skey.Contains("proc")
                             && !skey.Contains("hook")
                             && !skey.Contains("delay")
                             && !skey.Contains("renamemutex")
                             && !skey.Contains("mutextorename")
                             && !skey.Contains("setwindow")
                             && !skey.Contains("isdebug")
                             && !skey.Contains("nucleusfolderpath")
                             && !skey.Contains("blockraw")
                             && !skey.Contains("nucenv")
                             && !skey.Contains("playernick")
                             && !skey.Contains("root")
                             && !skey.Contains("destination")
                             && !skey.Contains("direxclusions")
                             && !skey.Contains("fileexclusions")
                             && !skey.Contains("filecopyinstead")
                             && !skey.Contains("hardlink")
                             && !skey.Contains("symfolder")
                             && !skey.Contains("numplayers")
                             && !skey.Contains("symlink")
                             //&& !skey.Contains("rawhid")
                             && !skey.Contains("output"))
                             
                                                          
                        {
                            i++;
                            if (string.IsNullOrEmpty(argument))
                            {
                                argument = skey;
                            }
                            else
                            {
                                argument = argument + " " + skey;
                            }
                        }
                    }
                    //Log("Extra arguments:" + argument);
                    ConsoleU.WriteLine("Extra arguments:" + argument, Palette.Feedback);

                    string[] splited = (arg + argument).Split(new string[] { "|::|" }, StringSplitOptions.None);
                    string key = splited[0].ToLower();

                    if (key.Contains("monitors"))
                    {

                    }
                    else if (key.Contains("hook"))
                    {
                        isHook = Boolean.Parse(splited[1]);
                    }
                    else if (key.Contains("delay"))
                    {
                        isDelay = Boolean.Parse(splited[1]);
                    }
                    else if (key.Contains("renamemutex"))
                    {
                        renameMutex = Boolean.Parse(splited[1]);
                    }
                    else if (key.Contains("mutextorename"))
                    {
                        mutexToRename = splited[1];
                    }
                    else if (key.Contains("partialmutex"))
                    {
                        partialMutex = Boolean.Parse(splited[1]);
                    }
                    else if (key.Contains("setwindow"))
                    {
                        setWindow = Boolean.Parse(splited[1]);
                    }
                    else if (key.Contains("isdebug"))
                    {
                        isDebug = Boolean.Parse(splited[1]);
                    }
                    else if (key.Contains("nucleusfolderpath"))
                    {
                        nucleusFolderPath = splited[1];
                    }
                    else if (key.Contains("blockraw"))
                    {
                        blockRaw = Boolean.Parse(splited[1]);
                    }
                    else if (key.Contains("nucenv"))
                    {
                        useNucleusEnvironment = Boolean.Parse(splited[1]);
                    }
                    else if (key.Contains("playernick"))
                    {
                        playerNick = splited[1];
                    }
                    else if (key.Contains("root"))
                    {
                        root = splited[1];
                    }
                    else if (key.Contains("currentdir"))
                    {
                        currentDir = splited[1];
                    }
                    else if (key.Contains("destination"))
                    {
                        destination = splited[1].Substring(0,splited[1].LastIndexOf('\\'));
                    }
                    else if (key.Contains("direxclusions"))
                    {
                        dirExclusions = splited[1].Split(new string[] { "|==|" }, StringSplitOptions.None);
                    }
                    else if (key.Contains("fileexclusions"))
                    {
                        fileExclusions = splited[1].Split(new string[] { "|==|" }, StringSplitOptions.None);
                    }
                    else if (key.Contains("filecopyinstead"))
                    {
                        fileCopyInstead = splited[1].Split(new string[] { "|==|" }, StringSplitOptions.None);
                    }
                    else if (key.Contains("hardlink"))
                    {
                        hardLink = Boolean.Parse(splited[1]);
                    }
                    else if (key.Contains("symfolder"))
                    {
                        symFolders = Boolean.Parse(splited[1]);
                    }
                    else if (key.Contains("numplayers"))
                    {
                         numPlayers = int.Parse(splited[1]);
                    }
                    else if (key.Contains("symlink"))
                    {
                        int exitCode = 1;
                        for (int p = 0; p < numPlayers; p++)
                        {
                            Nucleus.Gaming.Platform.Windows.IO.WinDirectoryUtil.LinkDirectory(root, new DirectoryInfo(root), destination + "\\Instance" + p, out exitCode, dirExclusions, fileExclusions, fileCopyInstead, hardLink, symFolders);
                        }
                    }
                    //else if (key.Contains("rawhid"))
                    //{
                    //    rawHid = splited[1];
                    //}
                    else if (key.Contains("game"))
                    {
                        string data = splited[1];
                        string[] subArgs = data.Split(';');
                        string path = subArgs[0];

                        string argu = null;
                        if (subArgs.Length > 1)
                        {
                            argu = subArgs[1];
                        }

                        string workingDir = null;
                        if (path.Contains("|"))
                        {
                            string[] div = path.Split('|');
                            path = div[0];
                            workingDir = div[1];
                        }

                        Log($"EXE: {path} ARGS: {argu} WORKDIR: {workingDir}");
                        ConsoleU.WriteLine($"Start game: EXE: {path} ARGS: {argu} WORKDIR: {workingDir}", Palette.Feedback);
                        StartGame(path, argu, workingDir);
                    }
                    else if (key.Contains("mutextype"))
                    {
                        mt = splited[1];
                    }
                    else if (key.Contains("mutex"))
                    {
                        string[] mutex = splited[1].Split(new string[] { "|==|"}, StringSplitOptions.None );
                        ConsoleU.WriteLine("Trying to kill mutexes", Palette.Wait);
                        for (int j = 0; j < mutex.Length; j++)
                        {
                            string m = mutex[j];
                            ConsoleU.WriteLine("Trying to kill mutex: " + m, Palette.Feedback);
                            if (!ProcessUtil.KillMutex(proc, mt, m, partialMutex))
                            {
                                ConsoleU.WriteLine("Mutex " + m + " could not be killed", Palette.Error);
                            }
                            else
                            {
                                ConsoleU.WriteLine("Mutex killed " + m, Palette.Success);
                            }
                            Thread.Sleep(150);
                        }
                    }
                    else if (key.Contains("proc"))
                    {
                        string procId = splited[1];
                        int id = int.Parse(procId);
                        try
                        {
                            proc = Process.GetProcessById(id);
                            Log(string.Format($"Process ID {id} found!"));
                            ConsoleU.WriteLine($"Process ID {id} found!", Palette.Success);
                        }
                        catch
                        {
                            Log(string.Format($"Process ID {id} not found"));
                            ConsoleU.WriteLine($"Process ID {id} not found", Palette.Error);
                        }
                    }
                    else if (key.Contains("output"))
                    {
                        string[] mutex = splited[1].Split(new string[] { "|==|" }, StringSplitOptions.None);
                        bool all = true;

                        for (int j = 0; j < mutex.Length; j++)
                        {
                            string m = mutex[j];
                            ConsoleU.WriteLine("Requested mutex: " + m, Palette.Error);
                            bool exists = ProcessUtil.MutexExists(proc, mt, m, partialMutex);
                            if (!exists)
                            {
                                all = false;
                            }
                            
                            Thread.Sleep(500);
                        }
                        Console.WriteLine(all.ToString());
                    }
                }
            }
#if RELEASE
            catch (Exception ex)
            {
                ConsoleU.WriteLine(ex.Message);
            }
#endif
        }
    }
}
