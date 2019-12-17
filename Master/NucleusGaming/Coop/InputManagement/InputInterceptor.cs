using Nucleus.Gaming.Coop.InputManagement.Logging;
using Nucleus.Gaming.Coop.InputManagement.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Gaming.Coop.InputManagement
{
	static class InputInterceptor
	{
		public static bool InterceptEnabled = false;

		private static WinApi.GetMsgProc mouseProc = MouseHookCallback;
		private static IntPtr mouseHookID = IntPtr.Zero;

		private static WinApi.GetMsgProc keyboardProc = KeyboardHookCallback;
		private static IntPtr keyboardHookID = IntPtr.Zero;

		private const int WH_MOUSE_LL = 14;
		private const int WH_KEYBOARD_LL = 13;

		private static readonly ushort[] bannedVkeysList = {
			0x03,//VK_CANCEL
			0x5B,//VK_LWIN
			0x5C,//VK_RWIN
			0x5D,//VK_APPS
		};

		static InputInterceptor()
		{
			Array.Sort(bannedVkeysList);

			mouseHookID = SetHook(mouseProc, WH_MOUSE_LL);
			keyboardHookID = SetHook(keyboardProc, WH_KEYBOARD_LL);
			Logger.WriteLine("InputInterceptor keyboard and mouse hooked");
		}

		private static IntPtr SetHook(WinApi.GetMsgProc proc, int hookID)
		{
			return WinApi.SetWindowsHookEx(hookID, proc, Marshal.GetHINSTANCE(typeof(InputInterceptor).Module), 0);
		}

		private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
		{
			return InterceptEnabled ? (IntPtr)1 : WinApi.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
		}

		private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
		{
			if (!InterceptEnabled) return WinApi.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);

			var kb = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));

			int vk = kb.vkCode;

			if ((vk == 0x09 || vk == 0x1B) //tab or escape
				&& (kb.flags & 0b100000) != 0)//is alt down
			{
				return (IntPtr)1;//alt+tab and alt+esc will change foreground window.
			}

			for (ushort i = 0; i < bannedVkeysList.Length; i++)
			{
				ushort bvk = bannedVkeysList[i];
				if (bvk == vk) return (IntPtr)1;
				else if (vk > bvk) break;
			}

			//Ctrl+esc
			if (vk == 0x1B && WinApi.GetAsyncKeyState(0x11) != 0)
				return (IntPtr)1;

			return WinApi.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
		}
	}
}
