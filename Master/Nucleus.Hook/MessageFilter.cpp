#include "pch.h"
#include "Logging.h"
#include "FakeMouse.h"
#include "InstallHooks.h"
#include "Globals.h"
#include <windowsx.h>

BOOL filterMessage(const LPMSG lpMsg)
{
	const auto msg = lpMsg->message;
	const auto wParam = lpMsg->wParam;
	const auto lParam = lpMsg->lParam;

#define ALLOW return 1;
#define BLOCK memset(lpMsg, 0, sizeof(MSG)); return -1;

	//Filter raw input
	if (msg == WM_INPUT && options.filterRawInput)
	{
		lpMsg->wParam = RIM_INPUT;//While in foreground

		UINT dwSize = 0;
		const auto sorh = sizeof(RAWINPUTHEADER);
		static RAWINPUT raw[sorh];

		if (0 == GetRawInputData(reinterpret_cast<HRAWINPUT>(lpMsg->lParam), RID_HEADER, nullptr, &dwSize, sorh) &&
			dwSize == sorh &&
			dwSize == GetRawInputData(reinterpret_cast<HRAWINPUT>(lpMsg->lParam), RID_HEADER, raw, &dwSize, sorh))
		{
			if (raw->header.dwType == RIM_TYPEMOUSE)
			{
				if (raw->header.hDevice == allowedMouseHandle)
				{
					ALLOW;
				}
				
				BLOCK;
			}
			
			if (raw->header.dwType == RIM_TYPEKEYBOARD)
			{
				if (raw->header.hDevice == allowedKeyboardHandle)
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
		if (msg == WM_MOUSEMOVE)
		{
			if ((static_cast<int>(wParam) & 0b10000000) > 0) //Signature for message sent from USS (C#)
			{
				if (useAbsoluteCursorPos == false)
				{
					if (options.updateAbsoluteFlagInMouseMessage)
					{
						const int x = GET_X_LPARAM(lParam);
						const int y = GET_Y_LPARAM(lParam);

						if (!(x == 0 && y == 0) && !(x == lastX && y == lastY))
							// - Minecraft (GLFW/LWJGL) will create a WM_MOUSEMOVE message with (0,0) AND another with (lastX, lastY) 
							//whenever a mouse button is clicked, WITHOUT calling SetCursorPos
							// - This would cause absoluteCursorPos to be turned on when it shouldn't.
						{
							updateAbsoluteCursorCheck();
						}

						if (x != 0)
							lastX = x;

						if (y != 0)
							lastY = y;

						lpMsg->lParam = MAKELPARAM(fakeX, fakeY);
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

	if(msg == WM_KILLFOCUS && options.preventWindowDeactivation)
	{
		BLOCK;
	}
	
	//USS signature is 1 << 7 or 0b10000000 for WM_MOUSEMOVE(0x0200). If this is detected, allow event to pass
	if (options.filterMouseMessages)
	{
		if (msg == WM_KILLFOCUS || msg == WM_ACTIVATE && wParam == 0 || msg == WM_CAPTURECHANGED || msg == WM_ACTIVATE && static_cast<int>(lParam) == reinterpret_cast<int>(hWnd))
		{
			BLOCK;
		}

		if (msg == WM_MOUSEMOVE && (static_cast<int>(wParam) & 0b10000000) > 0)
			ALLOW;

		// || Msg == 0x00FF
		if (msg >= WM_XBUTTONDOWN && msg <= WM_XBUTTONDBLCLK || msg == WM_MOUSEMOVE || msg == WM_MOUSEACTIVATE || msg
			== WM_MOUSEHOVER || msg == WM_MOUSELEAVE || msg == WM_MOUSEWHEEL || msg == WM_SETCURSOR || msg ==
			WM_NCMOUSELEAVE) //Other mouse events. 
		{
			BLOCK;
		}

		if (msg == WM_ACTIVATE)
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
	const auto ret = GetMessageA(lpMsg, hWnd, wMsgFilterMin, wMsgFilterMax);

	return ret == -1 ? -1 : filterMessage(lpMsg);
}

BOOL WINAPI GetMessageW_Hook(LPMSG lpMsg, HWND hWnd, UINT wMsgFilterMin, UINT wMsgFilterMax)
{
	const auto ret = GetMessageW(lpMsg, hWnd, wMsgFilterMin, wMsgFilterMax);

	return ret == -1 ? -1 : filterMessage(lpMsg);
}

BOOL WINAPI PeekMessageA_Hook(LPMSG lpMsg, HWND hWnd, UINT wMsgFilterMin, UINT wMsgFilterMax, UINT wRemoveMsg)
{
	const auto ret = PeekMessageA(lpMsg, hWnd, wMsgFilterMin, wMsgFilterMax, wRemoveMsg);

	return ret == FALSE ? FALSE : filterMessage(lpMsg);
}

BOOL WINAPI PeekMessageW_Hook(LPMSG lpMsg, HWND hWnd, UINT wMsgFilterMin, UINT wMsgFilterMax, UINT wRemoveMsg)
{
	const auto ret = PeekMessageW(lpMsg, hWnd, wMsgFilterMin, wMsgFilterMax, wRemoveMsg);

	return ret == FALSE ? FALSE : filterMessage(lpMsg);
}

void installMessageFilterHooks()
{
	DEBUGLOG("Injecting message filter hooks\n");
	installHook(TEXT("user32"), "GetMessageA", GetMessageA_Hook);
	installHook(TEXT("user32"), "GetMessageW", GetMessageW_Hook);

	installHook(TEXT("user32"), "PeekMessageA", PeekMessageA_Hook);
	installHook(TEXT("user32"), "PeekMessageW", PeekMessageW_Hook);
}
