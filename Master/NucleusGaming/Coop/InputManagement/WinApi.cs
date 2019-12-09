using Nucleus.Gaming.Coop.InputManagement.Enums;
using Nucleus.Gaming.Coop.InputManagement.Structs;
using System;
using System.Runtime.InteropServices;

namespace Nucleus.Gaming.Coop.InputManagement
{
	class WinApi
	{
		[DllImport("user32.dll")]
		public static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevice, uint uiNumDevices, uint cbSize);

		[DllImport("user32.dll")]
		public static extern int GetRawInputData(IntPtr hRawInput, DataCommand uiCommand, [Out] IntPtr pData, ref uint pcbSize, int cbSizeHeader);
		[DllImport("user32.dll")]
		public static extern int GetRawInputData(IntPtr hRawInput, DataCommand uiCommand, out RAWINPUT pData, ref uint pcbSize, int cbSizeHeader);

		[DllImport("user32.dll")]
		public static extern uint GetRawInputDeviceList(IntPtr pRawInputDeviceList, ref uint puiNumDevices, uint cbSize);

		[DllImport("user32.dll")]
		public static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, IntPtr pData, ref uint pcbSize);
	}
}
