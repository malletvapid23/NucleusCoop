﻿using System;
using System.Runtime.InteropServices;
using Nucleus.Gaming.Coop.Generic.Cursor;

namespace Nucleus.Gaming.Coop.InputManagement.Structs
{
	[StructLayout(LayoutKind.Sequential)]
	public struct MSG
	{
		public IntPtr hwnd;
		public uint message;
		public IntPtr wParam;
		public IntPtr lParam;
		public int time;
		public POINT pt;
	}
}