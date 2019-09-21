﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Nucleus.Gaming.Interop;
using System.Threading;
using System.Management;
using System.IO;

namespace Nucleus
{
    public static class ProcessUtil
    {
        public static Process RunOrphanProcess(string path, string arguments = "")
        {
            ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
            psi.FileName = @"cmd";
            psi.Arguments = "/C \"" + path + "\" " + arguments;
            return Process.Start(psi);
            //ThreadPool.QueueUserWorkItem(KillParent, p);
        }
        private static void KillParent(object state)
        {
            Process p = (Process)state;

        }
        private static void Log(string logMessage)
        {
            StreamWriter w = new StreamWriter("mutex-debug.txt", true);
            w.WriteLine($"[{DateTime.Now.ToLongTimeString()} {DateTime.Now.ToLongDateString()}] {logMessage}");
            w.Dispose();
        }
        public static bool KillMutex(Process process, string mutexType, string mutexName)
        {
            //Log(string.Format("Attempting to kill mutex"));
            // 4 tries
            for (int i = 1; i < 4; i++)
            {
                //Log("Attempt #" + i);
                Console.WriteLine("Loop " + i);
                //var handles = Win32Processes.GetHandles(process, mutexType, "", mutexName);

                var handles = Win32Processes.GetHandles(process, mutexType);

                //if (handles.Count == 0)
                //{
                //    Log(string.Format("{0} not found in process handles", mutexName));
                //    continue;
                //}

                foreach (var handle in handles)
                {
                    string strObjectName = Win32Processes.getObjectName(handle, Process.GetProcessById(handle.ProcessID));

                    if (!string.IsNullOrWhiteSpace(strObjectName) && strObjectName.Contains(mutexName))
                    {
                        //Log(string.Format("Killing mutex located at '{0}'", strObjectName));
                        IntPtr ipHandle = IntPtr.Zero;
                        if (!Win32API.DuplicateHandle(Process.GetProcessById(handle.ProcessID).Handle, handle.Handle, Win32API.GetCurrentProcess(), out ipHandle, 0, false, Win32API.DUPLICATE_CLOSE_SOURCE))
                        {
                            Log(string.Format("DuplicateHandle() failed, error = {0}", Marshal.GetLastWin32Error()));
                            Console.WriteLine("DuplicateHandle() failed, error = {0}", Marshal.GetLastWin32Error());
                        }
                        else
                        {
                            //Log("Mutex was killed successfully");
                            //Log("----------------END-----------------");
                            return true;
                        }
                    }
                }
            }
            Log("Mutex was not killed");
            Log("----------------END-----------------");
            return false;
        }

        public static bool MutexExists(Process process, string mutexType, string mutexName)
        {
            //Log(string.Format("Checking if mutex '{0}' of type '{1}' exists in process '{2} (pid {3})'", mutexName,mutexType,process.MainWindowTitle,process.Id));
            // 4 tries
            for (int i = 0; i < 4; i++)
            {
                try
                {
                    var handles = Win32Processes.GetHandles(process, mutexType, "", mutexName);
                    //if (handles.Count > 0)
                    //{
                    //    Log("Found at least one mutex that matches");
                    //    return true;
                    //}
                    bool foundHandle = false;

                    foreach (var handle in handles)
                    {
                        string strObjectName = Win32Processes.getObjectName(handle, Process.GetProcessById(handle.ProcessID));
                        if (!string.IsNullOrWhiteSpace(strObjectName) && strObjectName.Contains(mutexName))
                        {
                            //Log(string.Format("Found mutex that contains search criteria. Located at {0}", strObjectName));
                            foundHandle = true;
                        }
                    }

                    return foundHandle;
                }
                catch (IndexOutOfRangeException)
                {
                    Log(string.Format("The process name '{0}' is not currently running", process.MainWindowTitle));
                }
                catch (ArgumentException)
                {
                    Log(string.Format("The mutex '{0}' was not found in the process '{1}'", mutexName, process.MainWindowTitle));
                }
            }
            return false;
        }

        public static List<int> GetChildrenProcesses(Process process)
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                        "SELECT * " +
                        "FROM Win32_Process " +
                        "WHERE ParentProcessId=" + process.Id);
            ManagementObjectCollection collection = searcher.Get();

            List<int> ids = new List<int>();
            foreach (var item in collection)
            {
                uint ui = (uint)item["ProcessId"];

                ids.Add((int)ui);
            }

            return ids;
        }

     
    }
}
//        [DllImport("ntdll.dll")]
//        public static extern uint NtQuerySystemInformation(int
//            SystemInformationClass, IntPtr SystemInformation, int SystemInformationLength,
//            ref int returnLength);

//        [DllImport("kernel32.dll", EntryPoint = "RtlCopyMemory")]
//        static extern void CopyMemory(byte[] Destination, IntPtr Source, uint Length);

//        [StructLayout(LayoutKind.Sequential, Pack = 1)]
//        public struct SYSTEM_HANDLE_INFORMATION
//        { // Information Class 16
//            public int ProcessID;
//            public byte ObjectTypeNumber;
//            public byte Flags; // 0x01 = PROTECT_FROM_CLOSE, 0x02 = INHERIT
//            public ushort Handle;
//            public int Object_Pointer;
//            public UInt32 GrantedAccess;
//        }

//        const int CNST_SYSTEM_HANDLE_INFORMATION = 16;
//        const uint STATUS_INFO_LENGTH_MISMATCH = 0xc0000004;

//        public static List<SYSTEM_HANDLE_INFORMATION> GetHandles(Process process)
//        {
//            uint nStatus;
//            int nHandleInfoSize = 0x10000;
//            IntPtr ipHandlePointer = Marshal.AllocHGlobal(nHandleInfoSize);
//            int nLength = 0;
//            IntPtr ipHandle = IntPtr.Zero;

//            while ((nStatus = NtQuerySystemInformation(CNST_SYSTEM_HANDLE_INFORMATION, ipHandlePointer, nHandleInfoSize, ref nLength)) == STATUS_INFO_LENGTH_MISMATCH)
//            {
//                nHandleInfoSize = nLength;
//                Marshal.FreeHGlobal(ipHandlePointer);
//                ipHandlePointer = Marshal.AllocHGlobal(nLength);
//            }

//            byte[] baTemp = new byte[nLength];
//            CopyMemory(baTemp, ipHandlePointer, (uint)nLength);

//            long lHandleCount = 0;
//            if (Is64Bits())
//            {
//                lHandleCount = Marshal.ReadInt64(ipHandlePointer);
//                ipHandle = new IntPtr(ipHandlePointer.ToInt64() + 8);
//            }
//            else
//            {
//                lHandleCount = Marshal.ReadInt32(ipHandlePointer);
//                ipHandle = new IntPtr(ipHandlePointer.ToInt32() + 4);
//            }

//            SYSTEM_HANDLE_INFORMATION shHandle;
//            List<SYSTEM_HANDLE_INFORMATION> lstHandles = new List<SYSTEM_HANDLE_INFORMATION>();

//            for (long lIndex = 0; lIndex < lHandleCount; lIndex++)
//            {
//                shHandle = new SYSTEM_HANDLE_INFORMATION();
//                if (Is64Bits())
//                {
//                    shHandle = (SYSTEM_HANDLE_INFORMATION)Marshal.PtrToStructure(ipHandle, shHandle.GetType());
//                    ipHandle = new IntPtr(ipHandle.ToInt64() + Marshal.SizeOf(shHandle) + 8);
//                }
//                else
//                {
//                    ipHandle = new IntPtr(ipHandle.ToInt64() + Marshal.SizeOf(shHandle));
//                    shHandle = (SYSTEM_HANDLE_INFORMATION)Marshal.PtrToStructure(ipHandle, shHandle.GetType());
//                }
//                if (shHandle.ProcessID != process.Id) continue;
//                lstHandles.Add(shHandle);
//            }
//            return lstHandles;

//        }

//        static bool Is64Bits()
//        {
//            return Marshal.SizeOf(typeof(IntPtr)) == 8 ? true : false;
//        }
//    }
//}
