using Nucleus.Gaming.Coop.InputManagement.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Gaming.Coop.InputManagement
{
	static class LockInput
	{
		//private static AutoHotkeyEngine ahk;
		
		//public static bool IsAutoHotKeyNull => ahk == null;
		//private static bool IsInitialised = false;
		//private static Thread initThread = null;

		private static bool isLocking = false;

		public static bool IsLocked { get; private set; }

		//AHK not needed with new InputInterceptor
		/*public static void Init()
		{
			initThread = new Thread(ThreadTask);
			initThread.Start();
		}

		private static void ThreadTask()
		{
			try
			{
				Logger.WriteLine("Initialising LockInput AHK");
				ahk = new AutoHotkeyEngine();
				ahk.Suspend();

				//The star means it will disable even with modifier keys e.g. Shift

				ahk.ExecRaw("*MButton:: return");
				ahk.ExecRaw("*XButton1:: return");
				ahk.ExecRaw("*XButton2:: return");

				ahk.ExecRaw("*LWin:: return");
				ahk.ExecRaw("*Control:: return");
				ahk.ExecRaw("*Alt:: return");
				ahk.ExecRaw("*Shift:: return");//Important or shift will not function properly in game

				ahk.ExecRaw("*RButton:: return");
				ahk.ExecRaw("*LButton:: return");

				ahk.Suspend();

				IsInitialised = true;

				Logger.WriteLine("Initialised LockInput");
			}
			catch
			{
				Logger.WriteLine("Could not load LockInput");
			}
		}*/

		public static void Lock()
		{
			if (isLocking)
				return;
			else isLocking = true;

			/*if (!IsInitialised)
			{
				if (initThread == null)
					Init();

				initThread.Join();
			}*/

			//ThreadTask();
			//ahk?.UnSuspend();

			InputInterceptor.InterceptEnabled = true;

			

			System.Windows.Forms.Cursor.Hide();

			//No need to hide the cursor
			/*int i = 0;
			while (WinApi.ShowCursor(false) >= 0 && i++ < 30) ;

			WinApi.SetCursor(IntPtr.Zero);*/

			//Only works on admin. When it does, it prevents raw input
			/*for (int k = 0; k < 5; k++)
			{
				WinApi.BlockInput(true);
				var er = Marshal.GetLastWin32Error();
				Debug.WriteLine($"BlockInput GetLastErr = {er}");
			}*/

			System.Windows.Forms.Cursor.Position = new System.Drawing.Point(0, 0);
			System.Windows.Forms.Cursor.Clip = new System.Drawing.Rectangle(0, 0, 1, 1);

			WinApi.SetForegroundWindow(WinApi.GetDesktopWindow());
			
			Debug.WriteLine("Locked input");
			
			isLocking = false;
			IsLocked = true;
		}

		public static void Unlock()
		{
			if (isLocking)
				return;
			else isLocking = true;

			/*if (ahk != null)
			{
				ahk.Suspend();
			}*/

			System.Windows.Forms.Cursor.Show();
			System.Windows.Forms.Cursor.Clip = new System.Drawing.Rectangle();

			//WinApi.BlockInput(false);

			InputInterceptor.InterceptEnabled = false;

			Debug.WriteLine("Unlocked input");

			isLocking = false;
			IsLocked = false;
		}
	}
}
