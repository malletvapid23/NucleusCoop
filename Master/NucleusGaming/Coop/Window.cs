using Nucleus.Gaming.Coop.BasicTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nucleus.Gaming.Coop
{
	public class Window
	{
		//Window handle
		public readonly IntPtr hWnd;

		//Some games use an invisible window called DIEmWin. WM_INPUT needs to be sent to this hWnd instead of the visible game hWnd or it is ignored.
		public IntPtr DIEmWin_hWnd = IntPtr.Zero;

		public readonly int pid;//Process ID

		public IntPtr MouseAttached { get; set; } = new IntPtr(0);
		public IntVector2 MousePosition { get; } = new IntVector2();//This is a reference type
		public (bool l, bool m, bool r, bool x1, bool x2) MouseState { get; set; } = (false, false, false, false, false);
		public byte WASD_State { get; set; } = 0;

		public IntPtr KeyboardAttached { get; set; } = new IntPtr(0);
		public readonly BitArray keysDown = new BitArray(0xFF);

		public int ControllerIndex { get; set; } = 0;//0 = none, 1234 = 1234

		public RECT Bounds { get; private set; }
		public int Width => Bounds.Right - Bounds.Left;
		public int Height => Bounds.Bottom - Bounds.Top;

		//TODO: Window.HooksNamedPipe
		//public NamedPipe HooksNamedPipe { get; set; }
	}
	}
