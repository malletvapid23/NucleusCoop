using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using EasyHook;
using Nucleus.Gaming;

namespace Nucleus.Inject
{
    class Program
    {
		class Injector32
		{
			[DllImport("EasyHook32.dll", CharSet = CharSet.Ansi)]
			public static extern int RhInjectLibrary(
				uint InTargetPID,
				uint InWakeUpTID,
				uint InInjectionOptions,
				[MarshalAs(UnmanagedType.LPWStr)] string InLibraryPath_x86,
				[MarshalAs(UnmanagedType.LPWStr)] string InLibraryPath_x64,
				IntPtr InPassThruBuffer,
				uint InPassThruSize
				);

			[DllImport("EasyHook32.dll", CharSet = CharSet.Ansi)]
			public static extern int RhCreateAndInject(
				[MarshalAs(UnmanagedType.LPWStr)] string InEXEPath,
				[MarshalAs(UnmanagedType.LPWStr)] string InCommandLine,
				uint InProcessCreationFlags,
				uint InInjectionOptions,
				[MarshalAs(UnmanagedType.LPWStr)] string InLibraryPath_x86,
				[MarshalAs(UnmanagedType.LPWStr)] string InLibraryPath_x64,
				IntPtr InPassThruBuffer,
				uint InPassThruSize,
				IntPtr OutProcessId //Pointer to a UINT (the PID of the new process)
				);
		}

		class Injector64
		{
			[DllImport("EasyHook64.dll", CharSet = CharSet.Ansi)]
			public static extern int RhInjectLibrary(
				uint InTargetPID,
				uint InWakeUpTID,
				uint InInjectionOptions,
				[MarshalAs(UnmanagedType.LPWStr)] string InLibraryPath_x86,
				[MarshalAs(UnmanagedType.LPWStr)] string InLibraryPath_x64,
				IntPtr InPassThruBuffer,
				uint InPassThruSize
				);

			[DllImport("EasyHook64.dll", CharSet = CharSet.Ansi)]
			public static extern int RhCreateAndInject(
				[MarshalAs(UnmanagedType.LPWStr)] string InEXEPath,
				[MarshalAs(UnmanagedType.LPWStr)] string InCommandLine,
				uint InProcessCreationFlags,
				uint InInjectionOptions,
				[MarshalAs(UnmanagedType.LPWStr)] string InLibraryPath_x86,
				[MarshalAs(UnmanagedType.LPWStr)] string InLibraryPath_x64,
				IntPtr InPassThruBuffer,
				uint InPassThruSize,
				IntPtr OutProcessId //Pointer to a UINT (the PID of the new process)
				);
		}

		private static readonly IniFile ini = new IniFile(Path.Combine(Directory.GetCurrentDirectory(), "Settings.ini"));
        private static void Log(string logMessage)
        {
            if (ini.IniReadValue("Misc", "DebugLog") == "True")
            {
                using (StreamWriter writer = new StreamWriter("debug-log.txt", true))
                {
                    writer.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}]INJECT: {logMessage}");
                    writer.Close();
                }
            }
        }

        static void Main(string[] args)
        {
	        bool is64 = Environment.Is64BitProcess;

	        var argsDecoded = new string[args.Length];
	        for (int j = 0; j < args.Length; j++)
	        {
				argsDecoded[j] = Encoding.UTF8.GetString(Convert.FromBase64String(args[j]));
	        }

	        int i = 0;
            int.TryParse(argsDecoded[i++], out int tier);

            if (tier == 0)
            {
	            StartupHook(argsDecoded, i, is64);
            }
            else if (tier == 1)
            {
	            RuntimeHook(argsDecoded, i, is64);
            }
        }

        private static void StartupHook(string[] args, int i, bool is64)
        {
			string InEXEPath = args[i++];
			string InCommandLine = args[i++];
			uint.TryParse(args[i++], out uint InProcessCreationFlags);
			uint.TryParse(args[i++], out uint InInjectionOptions);
			string InLibraryPath_x86 = args[i++];
			string InLibraryPath_x64 = args[i++];
			bool.TryParse(args[i++], out bool hookWindow); // E.g. FindWindow, etc
			bool.TryParse(args[i++], out bool renameMutex);
			string mutexToRename = args[i++];
			bool.TryParse(args[i++], out bool setWindow);
			bool.TryParse(args[i++], out bool isDebug);
			string nucleusFolderPath = args[i++];
			bool.TryParse(args[i++], out bool blockRaw);

			//IntPtr InPassThruBuffer = Marshal.StringToHGlobalUni(args[i++]);
			//uint.TryParse(args[i++], out uint InPassThruSize);

			var logPath = Encoding.Unicode.GetBytes(nucleusFolderPath);
			int logPathLength = logPath.Length;

			var targetsBytes = Encoding.Unicode.GetBytes(mutexToRename);
			int targetsBytesLength = targetsBytes.Length;

			int size = 27 + logPathLength + targetsBytesLength;
			var data = new byte[size];
			data[0] = hookWindow == true ? (byte)1 : (byte)0;
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

			Array.Copy(logPath, 0, data, 18, logPathLength);

			Array.Copy(targetsBytes, 0, data, 19 + logPathLength, targetsBytesLength);

			IntPtr ptr = Marshal.AllocHGlobal(size);
			Marshal.Copy(data, 0, ptr, size);



			IntPtr pid = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(int)));

			try
			{
				int result = -1;
				int attempts = 0; // 5 attempts to inject

				while (result != 0)
				{
					if (is64)
						result = Injector64.RhCreateAndInject(InEXEPath, InCommandLine, InProcessCreationFlags, InInjectionOptions, "", InLibraryPath_x64, ptr, (uint)size, pid);
					else
						result = Injector32.RhCreateAndInject(InEXEPath, InCommandLine, InProcessCreationFlags, InInjectionOptions, InLibraryPath_x86, "", ptr, (uint)size, pid);
					
					Thread.Sleep(1000);
					attempts++;

					if (attempts == 4)
						break;
				}
				Marshal.FreeHGlobal(pid);

				Console.WriteLine(Marshal.ReadInt32(pid).ToString());
			}
			catch (Exception ex)
			{
				Log(string.Format("ERROR - {0}", ex.Message));
			}
        }
        private static void RuntimeHook(string[] args, int i, bool is64)
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

			bool.TryParse(args[i++], out bool setCursorPos);
			bool.TryParse(args[i++], out bool getCursorPos);
			bool.TryParse(args[i++], out bool getKeyState);
			bool.TryParse(args[i++], out bool getAsyncKeyState);
			bool.TryParse(args[i++], out bool getKeyboardState);
			bool.TryParse(args[i++], out bool filterRawInput);
			bool.TryParse(args[i++], out bool filterMouseMessages);
			bool.TryParse(args[i++], out bool legacyInput);
			bool.TryParse(args[i++], out bool updateAbsoluteFlagInMouseMessage);
			bool.TryParse(args[i++], out bool mouseVisibilitySendBack);

			string writePipeName = args[i++];
			string readPipeName = args[i++];

			int.TryParse(args[i++], out int allowedRawMouseHandle);
			int.TryParse(args[i++], out int allowedRawKeyboardHandle);

			var logPath = Encoding.Unicode.GetBytes(nucleusFolderPath);
			int logPathLength = logPath.Length;
			//int.TryParse(args[i++], out int InPassThruSize);

			var writePipeNameBytes = Encoding.Unicode.GetBytes(writePipeName);
			int writePipeNameLength = writePipeNameBytes.Length;

			var readPipeNameBytes = Encoding.Unicode.GetBytes(readPipeName);
			int readPipeNameLength = readPipeNameBytes.Length;

			int size = 42 + logPathLength + writePipeNameLength + readPipeNameLength;
			IntPtr intPtr = Marshal.AllocHGlobal(size);
			byte[] dataToSend = new byte[size];

			int index = 0;
			dataToSend[index++] = (byte)(hWnd >> 24);
			dataToSend[index++] = (byte)(hWnd >> 16);
			dataToSend[index++] = (byte)(hWnd >> 8);
			dataToSend[index++] = (byte)(hWnd);

			dataToSend[index++] = (byte)(allowedRawMouseHandle >> 24);
			dataToSend[index++] = (byte)(allowedRawMouseHandle >> 16);
			dataToSend[index++] = (byte)(allowedRawMouseHandle >> 8);
			dataToSend[index++] = (byte)(allowedRawMouseHandle);

			dataToSend[index++] = (byte)(allowedRawKeyboardHandle >> 24);
			dataToSend[index++] = (byte)(allowedRawKeyboardHandle >> 16);
			dataToSend[index++] = (byte)(allowedRawKeyboardHandle >> 8);
			dataToSend[index++] = (byte)(allowedRawKeyboardHandle);

			byte Bool_1_0(bool x) => x ? (byte)1 : (byte)0;
			dataToSend[index++] = Bool_1_0(preventWindowDeactivation);
			dataToSend[index++] = Bool_1_0(setWindow);
			dataToSend[index++] = Bool_1_0(isDebug);
			dataToSend[index++] = Bool_1_0(hideCursor);
			dataToSend[index++] = Bool_1_0(hookFocus);

			dataToSend[index++] = Bool_1_0(setCursorPos);
			dataToSend[index++] = Bool_1_0(getCursorPos);
			dataToSend[index++] = Bool_1_0(getKeyState);
			dataToSend[index++] = Bool_1_0(getAsyncKeyState);
			dataToSend[index++] = Bool_1_0(getKeyboardState);
			dataToSend[index++] = Bool_1_0(filterRawInput);
			dataToSend[index++] = Bool_1_0(filterMouseMessages);
			dataToSend[index++] = Bool_1_0(legacyInput);
			dataToSend[index++] = Bool_1_0(updateAbsoluteFlagInMouseMessage);
			dataToSend[index++] = Bool_1_0(mouseVisibilitySendBack);

			dataToSend[index++] = (byte)(logPathLength >> 24);
			dataToSend[index++] = (byte)(logPathLength >> 16);
			dataToSend[index++] = (byte)(logPathLength >> 8);
			dataToSend[index++] = (byte)logPathLength;

			dataToSend[index++] = (byte)(writePipeNameLength >> 24);
			dataToSend[index++] = (byte)(writePipeNameLength >> 16);
			dataToSend[index++] = (byte)(writePipeNameLength >> 8);
			dataToSend[index++] = (byte)writePipeNameLength;

			dataToSend[index++] = (byte)(readPipeNameLength >> 24);
			dataToSend[index++] = (byte)(readPipeNameLength >> 16);
			dataToSend[index++] = (byte)(readPipeNameLength >> 8);
			dataToSend[index++] = (byte)readPipeNameLength;

			Array.Copy(logPath, 0, dataToSend, index, logPathLength);
			Array.Copy(writePipeNameBytes, 0, dataToSend, index + logPathLength, writePipeNameLength);
			Array.Copy(readPipeNameBytes, 0, dataToSend, index + logPathLength + writePipeNameLength, readPipeNameLength);

			Marshal.Copy(dataToSend, 0, intPtr, size);

			try
			{
				if (is64)
				{
					Injector64.RhInjectLibrary((uint)InTargetPID, (uint)InWakeUpTID, (uint)InInjectionOptions, "", InLibraryPath_x64, intPtr, (uint)size);
				}
				else
				{
					Injector32.RhInjectLibrary((uint)InTargetPID, (uint)InWakeUpTID, (uint)InInjectionOptions, InLibraryPath_x86, "", intPtr, (uint)size);
				}
			}
			catch (Exception ex)
			{
				Log("ERROR - " + ex.Message);
			}
		}
	}
}
