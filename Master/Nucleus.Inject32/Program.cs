using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using EasyHook;

namespace Nucleus.Inject32
{
    class Program
    {
        [DllImport("EasyHook32.dll", CharSet = CharSet.Ansi)]
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

        static void Main(string[] args)
        {

            int i = 0;
            int.TryParse(args[i++], out int Tier);

            if (Tier==0)
            {
                string InEXEPath = args[i++];
                string InCommandLine = args[i++];
                uint.TryParse(args[i++], out uint InProcessCreationFlags);
                uint.TryParse(args[i++], out uint InInjectionOptions);
                string InLibraryPath_x86 = args[i++];
                string InLibraryPath_x64 = args[i++];
                bool.TryParse(args[i++], out bool isHook);
                bool.TryParse(args[i++], out bool renameMutex);
                string mutexToRename = args[i++];
                bool.TryParse(args[i++], out bool setWindow);
                bool.TryParse(args[i++], out bool isDebug);
                string nucleusFolderPath = args[i++];

                //IntPtr InPassThruBuffer = Marshal.StringToHGlobalUni(args[i++]);
                //uint.TryParse(args[i++], out uint InPassThruSize);

                var logPath = Encoding.Unicode.GetBytes(nucleusFolderPath);
                int logPathLength = logPath.Length;

                var targetsBytes = Encoding.Unicode.GetBytes(mutexToRename);
                int targetsBytesLength = targetsBytes.Length;

                int size = 1024;
                var data = new byte[size];
                data[0] = isHook == true ? (byte)1 : (byte)0;
                data[1] = renameMutex == true ? (byte)1 : (byte)0;
                data[2] = setWindow == true ? (byte)1 : (byte)0;
                data[3] = isDebug == true ? (byte)1 : (byte)0;

                data[4] = (byte)(logPathLength >> 24);
                data[5] = (byte)(logPathLength >> 16);
                data[6] = (byte)(logPathLength >> 8);
                data[7] = (byte)logPathLength;

                data[8] = (byte)(targetsBytesLength >> 24);
                data[9] = (byte)(targetsBytesLength >> 16);
                data[10] = (byte)(targetsBytesLength >> 8);
                data[11] = (byte)targetsBytesLength;

                Array.Copy(logPath, 0, data, 12, logPathLength);

                Array.Copy(targetsBytes, 0, data, 13 + logPathLength, targetsBytesLength);

                IntPtr ptr = Marshal.AllocHGlobal(size);
                Marshal.Copy(data, 0, ptr, size);



                IntPtr pid = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(int)));

                try
                {
                    int result = RhCreateAndInject(InEXEPath, InCommandLine, InProcessCreationFlags, InInjectionOptions, InLibraryPath_x86, InLibraryPath_x64, ptr, (uint)size, pid);
                    int attmpts = 0; // 4 additional attempts to inject
                    while (result != 0)
                    {
                        Thread.Sleep(1000);
                        attmpts++;
                        result = RhCreateAndInject(InEXEPath, InCommandLine, InProcessCreationFlags, InInjectionOptions, InLibraryPath_x86, InLibraryPath_x64, ptr, (uint)size, pid);
                        
                        if (attmpts == 4)
                            break;
                    }
                    Marshal.FreeHGlobal(pid);

                    Console.WriteLine(Marshal.ReadInt32(pid).ToString());
                }
                catch (Exception ex)
                {
                    using (StreamWriter writer = new StreamWriter("error-log.txt", true))
                    {
                        writer.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]" + "ex msg: {0}, ex str: {1}", ex.Message, ex.ToString());
                    }
                }
            }
            else if(Tier==1)
            {
                int.TryParse(args[i++], out int InTargetPID);
                int.TryParse(args[i++], out int InWakeUpTID);
                int.TryParse(args[i++], out int InInjectionOptions);
                string InLibraryPath_x86 = args[i++];
                string InLibraryPath_x64 = args[i++];
                //IntPtr InPassThruBuffer = Marshal.StringToHGlobalUni(args[i++]);
                //int.TryParse(args[i++], out int InPassThruSize);
                bool.TryParse(args[i++], out bool isHook);
                bool.TryParse(args[i++], out bool renameMutex);
                string mutexToRename = args[i++];
                bool.TryParse(args[i++], out bool setWindow);
                bool.TryParse(args[i++], out bool isDebug);
                string nucleusFolderPath = args[i++];

                //IntPtr InPassThruBuffer = Marshal.StringToHGlobalUni(args[i++]);
                //uint.TryParse(args[i++], out uint InPassThruSize);

                var logPath = Encoding.Unicode.GetBytes(nucleusFolderPath);
                int logPathLength = logPath.Length;

                var targetsBytes = Encoding.Unicode.GetBytes(mutexToRename);
                int targetsBytesLength = targetsBytes.Length;

                int size = 1024;
                var data = new byte[size];
                data[0] = isHook == true ? (byte)1 : (byte)0;
                data[1] = renameMutex == true ? (byte)1 : (byte)0;
                data[2] = setWindow == true ? (byte)1 : (byte)0;
                data[3] = isDebug == true ? (byte)1 : (byte)0;

                data[4] = (byte)(logPathLength >> 24);
                data[5] = (byte)(logPathLength >> 16);
                data[6] = (byte)(logPathLength >> 8);
                data[7] = (byte)logPathLength;

                data[8] = (byte)(targetsBytesLength >> 24);
                data[9] = (byte)(targetsBytesLength >> 16);
                data[10] = (byte)(targetsBytesLength >> 8);
                data[11] = (byte)targetsBytesLength;

                Array.Copy(logPath, 0, data, 12, logPathLength);

                Array.Copy(targetsBytes, 0, data, 13 + logPathLength, targetsBytesLength);

                IntPtr ptr = Marshal.AllocHGlobal(size);
                Marshal.Copy(data, 0, ptr, size);

                try
                {
                    NativeAPI.RhInjectLibrary(InTargetPID, InWakeUpTID, InInjectionOptions, InLibraryPath_x86, InLibraryPath_x64, ptr, size);
                }
                catch (Exception ex)
                {
                    using (StreamWriter writer = new StreamWriter("error-log.txt", true))
                    {
                        writer.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]" + "ex msg: {0}, ex str: {1}", ex.Message, ex.ToString());
                    }
                }
            }
        }
    }
}
