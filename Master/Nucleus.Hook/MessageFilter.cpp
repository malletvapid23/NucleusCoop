#include "pch.h"
#include "Logging.h"
#include "FakeMouse.h"
#include "InstallHooks.h"
#include "Globals.h"
#include "FakeMouse.h"
#include <windowsx.h>

BOOL FilterMessage(LPMSG lpMsg)
{
	UINT Msg = lpMsg->message;
	WPARAM _wParam = lpMsg->wParam;
	LPARAM _lParam = lpMsg->lParam;

#define ALLOW return 1;
#define BLOCK memset(lpMsg, 0, sizeof(MSG)); return -1;

	//Filter raw input
	if (Msg == WM_INPUT && options.filterRawInput)
	{
		lpMsg->wParam = RIM_INPUT;//While in foreground

		UINT dwSize = 0;
		const UINT sorh = sizeof(RAWINPUTHEADER);
		static RAWINPUT raw[sorh];

		if ((0 == GetRawInputData((HRAWINPUT)lpMsg->lParam, RID_HEADER, nullptr, &dwSize, sorh)) &&
			(dwSize == sorh) &&
			(dwSize == GetRawInputData((HRAWINPUT)lpMsg->lParam, RID_HEADER, raw, &dwSize, sorh)))
		{
			if (raw->header.dwType == RIM_TYPEMOUSE)
			{
				if (raw->header.hDevice == allowed_mouse_handle)
				{
					ALLOW;
				}
				
				BLOCK;
			}
			
			if (raw->header.dwType == RIM_TYPEKEYBOARD)
			{
				if (raw->header.hDevice == allowed_keyboard_handle)
				{
					ALLOW;
				}

				BLOCK;
			}
		}
	}

	//Legacy input filter
	if (options.legacyInput)
	{
		if (Msg == WM_MOUSEMOVE)
		{
			if (((int)_wParam & 0b10000000) > 0) //Signature for message sent from USS (C#)
			{
				if (use_absolute_cursor_pos == false)
				{
					if (options.updateAbsoluteFlagInMouseMessage)
					{
						int x = GET_X_LPARAM(_lParam);
						int y = GET_Y_LPARAM(_lParam);

						if (!(x == 0 && y == 0) && !(x == lastX && y == lastY))
							// - Minecraft (GLFW/LWJGL) will create a WM_MOUSEMOVE message with (0,0) AND another with (lastX, lastY) 
							//whenever a mouse button is clicked, WITHOUT calling SetCursorPos
							// - This would cause absoluteCursorPos to be turned on when it shouldn't.
						{
							update_absolute_cursor_check();
						}

						if (x != 0)
							lastX = x;

						if (y != 0)
							lastY = y;

						lpMsg->lParam = MAKELPARAM(fake_x, fake_y);
						ALLOW;
					}
					BLOCK;
				}
				//pMsg->lParam = MAKELPARAM(absoluteX, absoluteY);
				ALLOW;
			}
			BLOCK;
		}
	}

	if(Msg == WM_KILLFOCUS && options.preventWindowDeactivation)
	{
		BLOCK;
	}
	
	//USS signature is 1 << 7 or 0b10000000 for WM_MOUSEMOVE(0x0200). If this is detected, allow event to pass
	if (options.filterMouseMessages)
	{
		if (Msg == WM_KILLFOCUS || (Msg == WM_ACTIVATE && _wParam == 0) || Msg == WM_CAPTURECHANGED || (Msg == WM_ACTIVATE && (int)_lParam == (int)hWnd))
		{
			BLOCK;
		}

		if (Msg == WM_MOUSEMOVE && ((int)_wParam & 0b10000000) > 0)
			ALLOW;

		// || Msg == 0x00FF
		if ((Msg >= WM_XBUTTONDOWN && Msg <= WM_XBUTTONDBLCLK) || Msg == WM_MOUSEMOVE || Msg == WM_MOUSEACTIVATE || Msg
			== WM_MOUSEHOVER || Msg == WM_MOUSELEAVE || Msg == WM_MOUSEWHEEL || Msg == WM_SETCURSOR || Msg ==
			WM_NCMOUSELEAVE) //Other mouse events. 
		{
			BLOCK;
		}

		if (Msg == WM_ACTIVATE)
		{
			lpMsg->lParam = 1;
			lpMsg->wParam = 0;
		}
		ALLOW;
	}

	ALLOW;

#undef ALLOW
#undef BLOCK
}

BOOL WINAPI GetMessageA_Hook(LPMSG lpMsg, HWND hWnd, UINT wMsgFilterMin, UINT wMsgFilterMax)
{
	BOOL ret = GetMessageA(lpMsg, hWnd, wMsgFilterMin, wMsgFilterMax);

	return ret == -1 ? -1 : FilterMessage(lpMsg);
}

BOOL WINAPI GetMessageW_Hook(LPMSG lpMsg, HWND hWnd, UINT wMsgFilterMin, UINT wMsgFilterMax)
{
	BOOL ret = GetMessageW(lpMsg, hWnd, wMsgFilterMin, wMsgFilterMax);

	return ret == -1 ? -1 : FilterMessage(lpMsg);
}

BOOL WINAPI PeekMessageA_Hook(LPMSG lpMsg, HWND hWnd, UINT wMsgFilterMin, UINT wMsgFilterMax, UINT wRemoveMsg)
{
	BOOL ret = PeekMessageA(lpMsg, hWnd, wMsgFilterMin, wMsgFilterMax, wRemoveMsg);

	return ret == FALSE ? FALSE : FilterMessage(lpMsg);
}

BOOL WINAPI PeekMessageW_Hook(LPMSG lpMsg, HWND hWnd, UINT wMsgFilterMin, UINT wMsgFilterMax, UINT wRemoveMsg)
{
	BOOL ret = PeekMessageW(lpMsg, hWnd, wMsgFilterMin, wMsgFilterMax, wRemoveMsg);

	return ret == FALSE ? FALSE : FilterMessage(lpMsg);
}

void install_message_filter_hooks()
{
	DEBUGLOG("Injecting message filter hooks\n");
	HookInstall(TEXT("user32"), "GetMessageA", GetMessageA_Hook);
	HookInstall(TEXT("user32"), "GetMessageW", GetMessageW_Hook);

	HookInstall(TEXT("user32"), "PeekMessageA", PeekMessageA_Hook);
	HookInstall(TEXT("user32"), "PeekMessageW", PeekMessageW_Hook);
}
