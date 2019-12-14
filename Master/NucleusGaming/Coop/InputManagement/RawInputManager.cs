﻿using Nucleus.Gaming.Coop.InputManagement.Enums;
using Nucleus.Gaming.Coop.InputManagement.Logging;
using Nucleus.Gaming.Coop.InputManagement.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Nucleus.Gaming.Coop.InputManagement
{
	static class RawInputManager
	{
		public static readonly List<Window> windows = new List<Window>();

		public static void RegisterRawInput(IntPtr windowHandle)
		{
			//GetDeviceList();
			Logger.WriteLine($"Attempting to RegisterRawInputDevices for window handle {windowHandle}");

			//https://docs.microsoft.com/en-us/windows-hardware/drivers/hid/hidclass-hardware-ids-for-top-level-collections
			RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[2];

			//Keyboard
			rid[0].usUsagePage = 0x01;
			rid[0].usUsage = 0x06;
			rid[0].dwFlags = (uint)RawInputDevice_dwFlags.RIDEV_INPUTSINK;
			rid[0].hwndTarget = windowHandle;

			//Mouse
			rid[1].usUsagePage = 0x01;
			rid[1].usUsage = 0x02;
			rid[1].dwFlags = (uint)RawInputDevice_dwFlags.RIDEV_INPUTSINK;
			rid[1].hwndTarget = windowHandle;

			bool success = WinApi.RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0]));
			Logger.WriteLine($"Succeeded RegisterRawInputDevices Keyboard = {success}");

			if (!success)
			{
				int error = Marshal.GetLastWin32Error();
				Logger.WriteLine($"Error code = {error}");
			}

			success = WinApi.RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[1]));
			Logger.WriteLine($"Succeeded RegisterRawInputDevices Mouse = {success}");

			if (!success)
			{
				int error = Marshal.GetLastWin32Error();
				Logger.WriteLine($"Error code = {error}");
			}
		}

		public static IEnumerable<(RID_DEVICE_INFO deviceInfo, IntPtr deviceHandle)> GetDeviceList()
		{
			uint numDevices = 0;
			int cbSize = Marshal.SizeOf(typeof(RAWINPUTDEVICELIST));

			if (WinApi.GetRawInputDeviceList(IntPtr.Zero, ref numDevices, (uint)cbSize) == 0)//Return value isn't zero if there is an error
			{
				IntPtr pRawInputDeviceList = Marshal.AllocHGlobal((int)(cbSize * numDevices));
				WinApi.GetRawInputDeviceList(pRawInputDeviceList, ref numDevices, (uint)cbSize);

				for (int i = 0; i < numDevices; i++)
				{
					RAWINPUTDEVICELIST rid = (RAWINPUTDEVICELIST)Marshal.PtrToStructure(new IntPtr(pRawInputDeviceList.ToInt32() + (cbSize * i)), typeof(RAWINPUTDEVICELIST));

					uint pcbSize = 0;
					WinApi.GetRawInputDeviceInfo(rid.hDevice, 0x2000000b, IntPtr.Zero, ref pcbSize);//Get the size required in memory
					IntPtr pData = Marshal.AllocHGlobal((int)pcbSize);
					WinApi.GetRawInputDeviceInfo(rid.hDevice, 0x2000000b, pData, ref pcbSize);
					var device = (RID_DEVICE_INFO)Marshal.PtrToStructure(pData, typeof(RID_DEVICE_INFO));
					if (device.dwType == 0)
					{
						//Mouse
						Logger.WriteLine($"Found mouse. Mouse ID = {device.mouse.dwId}, number of buttons = {device.mouse.dwNumberOfButtons}, sample rate = {device.mouse.dwSampleRate}, has horizontal wheel = {device.mouse.dwSampleRate}");
					}
					else if (device.dwType == 1)
					{
						//Keyboard
						Logger.WriteLine($"Found keyboard. Keyboard type = {device.keyboard.dwType}, keyboard subtype = {device.keyboard.dwSubType}, scan code mode = {device.keyboard.dwKeyboardMode}, number of keys = {device.keyboard.dwNumberOfKeysTotal}");
					}
					yield return (device, rid.hDevice);
				}

				Marshal.FreeHGlobal(pRawInputDeviceList);
			}
		}

		public static IEnumerable<PlayerInfo> GetDeviceInputInfos()
		{
			//TODO: Add device handle zero mouse & keyboard w/ special icon

			int i = 100;

			foreach(var device in GetDeviceList().Where(x => x.deviceInfo.dwType <= 1))
			{
				PlayerInfo player = new PlayerInfo
				{
					GamepadId = i++,
					IsRawMouse = device.deviceInfo.dwType == 0,
					IsRawKeyboard = device.deviceInfo.dwType == 1,
					RawDeviceHandle = device.deviceHandle
				};

				yield return player;
			}
		}
	}
}
