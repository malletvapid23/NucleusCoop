using Nucleus;
using Nucleus.Gaming;
using Nucleus.Gaming.Windows;
using Nucleus.Interop.User32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasyHook;
using System.Runtime.InteropServices;
using Nucleus.Gaming.Coop;
using System.Security;

namespace StartGame
{
    class Program
    {
        private static int tries = 5;
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

        private static bool gameIs64 = false;

        private static string mutexToRename;

        //private static string rawHid;

        private static bool isDebug;
        private static string nucleusFolderPath;

        private static int pOutPID = 0;

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

        [DllImport("kernel32.dll")]
        public static extern bool CreateProcess(string lpApplicationName,
        string lpCommandLine, IntPtr lpProcessAttributes,
        IntPtr lpThreadAttributes,
        bool bInheritHandles, ProcessCreationFlags dwCreationFlags,
        IntPtr lpEnvironment, string lpCurrentDirectory,
        ref STARTUPINFO lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll")]
        public static extern uint ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        public static extern uint SuspendThread(IntPtr hThread);

        public struct STARTUPINFO
        {
            public uint cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
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

            int tri = 0;
            ProcessStartInfo startInfo;
            startInfo = new ProcessStartInfo();
            startInfo.FileName = path;
            startInfo.Arguments = args;
            
            if (!string.IsNullOrWhiteSpace(workingDir))
            {
                startInfo.WorkingDirectory = workingDir;
            }


            try
            {
                //proc = Process.Start(startInfo);
                string currDir = Directory.GetCurrentDirectory();

                //bool is64 = EasyHook.RemoteHooking.IsX64Process((int)pi.dwProcessId);
                if (Is64Bit(path) == true)
                {
                    gameIs64 = true;
                }

                if (isHook || renameMutex || setWindow || blockRaw)
                {
                    var targetsBytes = Encoding.Unicode.GetBytes(mutexToRename);
                    int targetsBytesLength = targetsBytes.Length;

                    var logPath = Encoding.Unicode.GetBytes(nucleusFolderPath);
                    int logPathLength = logPath.Length;

                    //var hidBytes = Encoding.Unicode.GetBytes(rawHid);
                    //int hidBytesLength = hidBytes.Length;

                    int size = 27 + logPathLength + targetsBytesLength;
                    var data = new byte[size];
                    data[0] = isHook == true ? (byte)1 : (byte)0;
                    data[1] = renameMutex == true ? (byte)1 : (byte)0;
                    data[2] = setWindow == true ? (byte)1 : (byte)0;
                    data[3] = isDebug == true ? (byte)1 : (byte)0;
                    data[4] = blockRaw == true ? (byte)1 : (byte)0;

                    data[10] = (byte)(logPathLength >> 24);
                    data[11] = (byte)(logPathLength >> 16);
                    data[12] = (byte)(logPathLength >> 8);
                    data[13] = (byte)logPathLength;

                    data[14] = (byte)(targetsBytesLength >> 24);
                    data[15] = (byte)(targetsBytesLength >> 16);
                    data[16] = (byte)(targetsBytesLength >> 8);
                    data[17] = (byte)targetsBytesLength;

                    //data[18] = (byte)(hidBytesLength >> 24);
                    //data[19] = (byte)(hidBytesLength >> 16);
                    //data[20] = (byte)(hidBytesLength >> 8);
                    //data[21] = (byte)hidBytesLength;

                    Array.Copy(logPath, 0, data, 18, logPathLength);

                    //Array.Copy(hidBytes, 0, data, 23 + logPathLength, hidBytesLength);

                    Array.Copy(targetsBytes, 0, data, 19 + logPathLength, targetsBytesLength);

                    IntPtr ptr = Marshal.AllocHGlobal(size);
                    Marshal.Copy(data, 0, ptr, size);

                    if (!isDelay) // CreateandInject method
                    {
                        Log("Starting game and injecting start up hooks using create and inject method");
                        if (gameIs64)
                        {
                            try
                            {
                                Log("x64 game detected, injecting Nucleus.SHook64.dll");
                                IntPtr pid = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(uint)));
                                RhCreateAndInject(path, args, 0, 0, null, Path.Combine(currDir, "Nucleus.SHook64.dll"), ptr, (uint)size, pid);
                                pOutPID = Marshal.ReadInt32(pid);
                                Marshal.FreeHGlobal(pid);
                            }
                            catch (Exception ex)
                            {
                                Log(string.Format("ERROR - {0}", ex.Message));
                                //using (StreamWriter writer = new StreamWriter("error-log.txt", true))
                                //{
                                //    writer.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]" + "ex msg: {0}, ex str: {1}", ex.Message, ex.ToString());
                                //}
                            }
                        }
                        else //if (Is64Bit(path) == false)
                        {
                            try
                            {
                                Log("x32 game detected, using Nucleus.Inject32 and injecting Nucleus.SHook32.dll");
                                //pidTest = gen.Inject(path, args, 0, 0, Path.Combine(currDir, "Nucleus.Hook32.dll"), null, IntPtr.Zero, 0);
                                string injectorPath = Path.Combine(currDir, "Nucleus.Inject32.exe");
                                ProcessStartInfo injstartInfo = new ProcessStartInfo();
                                injstartInfo.FileName = injectorPath;
                                object[] injargs = new object[]
                                {
                                    0, path, args, 0, 0, Path.Combine(currDir, "Nucleus.SHook32.dll"), null, isHook, renameMutex, mutexToRename, setWindow, isDebug, nucleusFolderPath, blockRaw
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

                                //if (runAdmin)
                                //{
                                //    //    injstartInfo.FileName = "explorer";
                                //    //    injstartInfo.Arguments = injectorPath + " " + arguments;
                                //    injstartInfo.Verb = "runas";
                                //}
                                Process injectProc = Process.Start(injstartInfo);
                                injectProc.OutputDataReceived += proc_OutputDataReceived;
                                injectProc.BeginOutputReadLine();

                                //using (StreamWriter writer = new StreamWriter("important.txt", true))
                                //{
                                //    writer.WriteLine("readtoend: {0}, readline: {1}", injectProc.StandardOutput.ReadToEnd(), injectProc.StandardOutput.ReadLine());
                                //}


                                injectProc.WaitForExit();

                                //GenericGameHandler.RhCreateAndInject(path, args, 0, 0, Path.Combine(currDir, "Nucleus.Hook32.dll"), Path.Combine(currDir, "Nucleus.Hook64.dll"), IntPtr.Zero, 0, pid);
                                //pidTest = Nucleus.Injector32.Injector32.RhCreateAndInject(path, args, 0, 0, Path.Combine(currDir, "Nucleus.Hook32.dll"), Path.Combine(currDir, "Nucleus.Hook64.dll"), IntPtr.Zero, 0, pid);                        
                            }
                            catch (Exception ex)
                            {
                                Log(string.Format("ERROR - {0}", ex.Message));
                                //using (StreamWriter writer = new StreamWriter("error-log.txt", true))
                                //{
                                //    writer.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]" + "is64: false, ex msg: {0}, ex str: {1}", ex.Message, ex.ToString());
                                //}
                            }
                        }
                        //else
                        //{
                        //    Log(string.Format("ERROR - Machine type {0} not implemented", GetDllMachineType(path)));
                        //    //using (StreamWriter writer = new StreamWriter("error-log.txt", true))
                        //    //{
                        //    //    writer.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]" + "Machine type: '{0}' not implemented.", GetDllMachineType(path));
                        //    //}
                        //}
                    }
                    else // delay method
                    {
                        Log("Starting game and injecting start up hooks using delay method");

                        string directoryPath = Path.GetDirectoryName(path);
                        STARTUPINFO si = new STARTUPINFO();
                        PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
                        bool success = CreateProcess(path, args, IntPtr.Zero, IntPtr.Zero, false, ProcessCreationFlags.CREATE_SUSPENDED, IntPtr.Zero, directoryPath, ref si, out pi);

                        if (!success)
                        {
                            Log(string.Format("ERROR - CreateProcess failed - startGamePath: {0}, startArgs: {1}, dirpath: {2}", path, args, directoryPath));
                            //using (StreamWriter writer = new StreamWriter("error-log.txt", true))
                            //{
                            //    writer.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]" + ");
                            //}
                            return;
                        }

                        ResumeThread(pi.hThread);

                        WaitForInputIdle(pi.hProcess, uint.MaxValue);

                        SuspendThread(pi.hThread);

                        if (gameIs64)
                        {
                            Log("x64 game detected, injecting Nucleus.SHook64.dll");
                            NativeAPI.RhInjectLibrary((int)pi.dwProcessId, 0, 0, null, Path.Combine(currDir, "Nucleus.SHook64.dll"), ptr, size);
                            pOutPID = (int)pi.dwProcessId;
                        }
                        else //if (Is64Bit(path) == false)
                        {
                            try
                            {
                                Log("x32 game detected, using Nucleus.Inject32 and injecting Nucleus.SHook32.dll");
                                string injectorPath = Path.Combine(currDir, "Nucleus.Inject32.exe");
                                ProcessStartInfo injstartInfo = new ProcessStartInfo();
                                injstartInfo.FileName = injectorPath;
                                object[] injargs = new object[]
                                {
                                    1, (int)pi.dwProcessId, 0, 0, Path.Combine(currDir, "Nucleus.SHook32.dll"), null, isHook, renameMutex, mutexToRename, setWindow, isDebug, nucleusFolderPath, blockRaw
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

                                //if (runAdmin)
                                //{
                                //    //injstartInfo.FileName = "explorer";
                                //    //injstartInfo.Arguments = injectorPath + " " + arguments;
                                //    injstartInfo.Verb = "runas";
                                //}
                                Process injectProc = Process.Start(injstartInfo);
                                //injectProc.OutputDataReceived += proc_OutputDataReceived;
                                //injectProc.BeginOutputReadLine();

                                //using (StreamWriter writer = new StreamWriter("important.txt", true))
                                //{
                                //    writer.WriteLine("readtoend: {0}, readline: {1}", injectProc.StandardOutput.ReadToEnd(), injectProc.StandardOutput.ReadLine());
                                //}


                                injectProc.WaitForExit();
                            }
                            catch (Exception ex)
                            {
                                Log(string.Format("ERROR - {0}", ex.Message));
                                //using (StreamWriter writer = new StreamWriter("error-log.txt", true))
                                //{
                                //    writer.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]" + "ex msg: {0}, ex str: {1}", ex.Message, ex.ToString());
                                //}
                            }
                        }
                        ResumeThread(pi.hThread);
                        pOutPID = (int)pi.dwProcessId;
                    }
                }
                else // regular method (no hooks)
                {
                    Log("Starting game via regular process start method (no start up hooks enabled)");
                    //if(runAdmin)
                    //{
                    //    //startInfo.FileName = "explorer";
                    //    //startInfo.Arguments = path + " " + args;
                    //    startInfo.Verb = "runas";
                    //}
                    proc = Process.Start(startInfo);


                    pOutPID = proc.Id;
                    regMethod = true;
                    
                }


                
                ConsoleU.WriteLine("Game started, process ID:" + pOutPID /*Marshal.ReadInt32(pid)*/ /*proc.Id*/ /*(int)pi.dwProcessId*/, Palette.Success);
                if(isDebug)
                {
                    if (regMethod)
                    {
                        Thread.Sleep(100);
                        Log(string.Format("Game started, process {0} (pid {1})", proc.ProcessName, pOutPID));
                    }
                    else
                    {
                        Thread.Sleep(100);
                        Log(string.Format("Game started, process ID: {0}", pOutPID));
                    }
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
            //Log($"Redirected output: {e.Data}");
            Thread.Sleep(100);
            Console.WriteLine($"Redirected output: {e.Data}");
            int.TryParse(e.Data, out pOutPID);
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
                             //&& !skey.Contains("runadmin")
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
                    //else if (key.Contains("runnotadmin"))
                    //{
                    //    runAdmin = Boolean.Parse(splited[1]);
                    //}
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
