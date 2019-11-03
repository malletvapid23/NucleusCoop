using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using EasyHook;
using Nucleus.Gaming;

namespace Nucleus.Inject64
{
    class Program
    {
        [DllImport("EasyHook64.dll", CharSet = CharSet.Ansi)]
        public static extern int RhCreateAndInject(
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

        private static readonly IniFile ini = new IniFile(Path.Combine(Directory.GetCurrentDirectory(), "Settings.ini"));
        private static void Log(string logMessage)
        {
            if (ini.IniReadValue("Misc", "DebugLog") == "True")
            {
                using (StreamWriter writer = new StreamWriter("debug-log.txt", true))
                {
                    writer.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}]INJECT64: {logMessage}");
                    writer.Close();
                }
            }
        }

        static void Main(string[] args)
        {

            int i = 0;
            int.TryParse(args[i++], out int Tier);

            if (Tier == 0)
            {
                string InEXEPath = args[i++];
                string InCommandLine = args[i++];
                uint.TryParse(args[i++], out uint InProcessCreationFlags);
                uint.TryParse(args[i++], out uint InInjectionOptions);
                string InLibraryPath_x86 = args[i++];
                string InLibraryPath_x64 = args[i++];
                IntPtr InPassThruBuffer = Marshal.StringToHGlobalUni(args[i++]);
                uint.TryParse(args[i++], out uint InPassThruSize);
                IntPtr pid = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(uint)));

                try
                {
                    int result = RhCreateAndInject(InEXEPath, InCommandLine, InProcessCreationFlags, InInjectionOptions, InLibraryPath_x86, InLibraryPath_x64, InPassThruBuffer, InPassThruSize, pid);
                    int attmpts = 0; // 4 additional attempts to inject
                    while (result != 0)
                    {
                        Thread.Sleep(1000);
                        attmpts++;
                        result = RhCreateAndInject(InEXEPath, InCommandLine, InProcessCreationFlags, InInjectionOptions, InLibraryPath_x86, InLibraryPath_x64, InPassThruBuffer, InPassThruSize, pid);

                        if (attmpts == 4)
                            break;
                    }
                    Marshal.FreeHGlobal(pid);

                    Console.WriteLine(Marshal.ReadInt32(pid).ToString());
                }
                catch (Exception ex)
                {
                    Log("ERROR - " + ex.Message);
                    //using (StreamWriter writer = new StreamWriter("error-log.txt", true))
                    //{
                    //    writer.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]" + "ex msg: {0}, ex str: {1}", ex.Message, ex.ToString());
                    //}
                }
            }
            else if (Tier == 1)
            {
                int.TryParse(args[i++], out int InTargetPID);
                int.TryParse(args[i++], out int InWakeUpTID);
                int.TryParse(args[i++], out int InInjectionOptions);
                string InLibraryPath_x86 = args[i++];
                string InLibraryPath_x64 = args[i++];
                //IntPtr InPassThruBuffer = Marshal.StringToHGlobalUni(args[i++]);
                int.TryParse(args[i++], out int hWnd);
                bool.TryParse(args[i++], out bool hookFocus);
                bool.TryParse(args[i++], out bool hideCursor);
                bool.TryParse(args[i++], out bool isDebug);
                string nucleusFolderPath = args[i++];
                bool.TryParse(args[i++], out bool setWindow);
                bool.TryParse(args[i++], out bool preventWindowDeactivation);

                var logPath = Encoding.Unicode.GetBytes(nucleusFolderPath);
                int logPathLength = logPath.Length;
                //int.TryParse(args[i++], out int InPassThruSize);

                int size = 42 + logPathLength;
                IntPtr intPtr = Marshal.AllocHGlobal(size);
                byte[] dataToSend = new byte[size];

                dataToSend[0] = (byte)(hWnd >> 24);
                dataToSend[1] = (byte)(hWnd >> 16);
                dataToSend[2] = (byte)(hWnd >> 8);
                dataToSend[3] = (byte)(hWnd);

                dataToSend[4] = preventWindowDeactivation == true ? (byte)1 : (byte)0;
                dataToSend[5] = setWindow == true ? (byte)1 : (byte)0;
                dataToSend[6] = isDebug == true ? (byte)1 : (byte)0;
                dataToSend[7] = hideCursor == true ? (byte)1 : (byte)0;
                dataToSend[8] = hookFocus == true ? (byte)1 : (byte)0;

                dataToSend[9] = (byte)(logPathLength >> 24);
                dataToSend[10] = (byte)(logPathLength >> 16);
                dataToSend[11] = (byte)(logPathLength >> 8);
                dataToSend[12] = (byte)logPathLength;

                Array.Copy(logPath, 0, dataToSend, 13, logPathLength);

                Marshal.Copy(dataToSend, 0, intPtr, size);                

                //using (StreamWriter writer = new StreamWriter("error-log.txt", true))
                //{
                //    writer.WriteLine("injecting args: {0}, {1}, {2}, {3}, {4}, {5}, {6}", InTargetPID, InWakeUpTID, InInjectionOptions, InLibraryPath_x86, InLibraryPath_x64, intPtr, InPassThruSize);
                //}

                try
                {
                    NativeAPI.RhInjectLibrary(InTargetPID, InWakeUpTID, InInjectionOptions, InLibraryPath_x86, InLibraryPath_x64, intPtr, size);
                }
                catch (Exception ex)
                {
                    Log("ERROR - " + ex.Message);
                    //using (StreamWriter writer = new StreamWriter("error-log.txt", true))
                    //{
                    //    writer.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]" + "ex msg: {0}, ex str: {1}", ex.Message, ex.ToString());
                    //}
                }
            }
        }
    }
}
