#include "pch.h"
#include "pch.h"
#include "Logging.h"
#include "Globals.h"
#include <string>
#include "InstallHooks.h"
#include "ReRegisterRawInput.h"

/* Each process can only have ONE window registered for input.
 * To deregister raw input, call RegisterRawInputDevices with RIDEV_REMOVE and hwnd = NULL
 * To find the window the process expects WM_INPUT messages, MessageFilter find the first message of WM_INPUT and passes its hwnd here.
 * RegisterRawInputDevices is then called to reregister raw input with RIDEV_INPUTSINK
 * RIDEV_INPUTSINK allows the process to receive input in the background, which is the goal. */

BOOL WINAPI RegisterRawInputDevices_Hook(PCRAWINPUTDEVICE pRawInputDevices, UINT uiNumDevices, UINT cbSize)
{
	//DEBUGLOG("Blocking a RegisterRawInputDevices call\n"); (some games spam Register)
	return TRUE;//Pretend it succeeded
}

void reRegisterRawInput(HWND hwnd)
{
	installHook("user32", "RegisterRawInputDevices", RegisterRawInputDevices_Hook);
	
	DEBUGLOG("Reregistering raw input for hwnd " << hwnd << "\n");
		
	//Deregister mouse
	{
		RAWINPUTDEVICE rid[1];
		rid[0].usUsagePage = 0x01;
		rid[0].usUsage = 0x02;
		rid[0].dwFlags = RIDEV_REMOVE;
		rid[0].hwndTarget = NULL;
		auto res = RegisterRawInputDevices(rid, 1, sizeof(rid[0]));
		DEBUGLOG("Deregister mouse result: " << res << "\n");
	}

	//Deregister keyboard
	{
		RAWINPUTDEVICE rid[1];
		rid[0].usUsagePage = 0x01;
		rid[0].usUsage = 0x06;
		rid[0].dwFlags = RIDEV_REMOVE;
		rid[0].hwndTarget = NULL;
		auto res = RegisterRawInputDevices(rid, 1, sizeof(rid[0]));
		DEBUGLOG("Deregister keyboard result: " << res << "\n");
	}

	//Reregister mouse
	{
		RAWINPUTDEVICE rid[1];
		rid[0].usUsagePage = 0x01;
		rid[0].usUsage = 0x02;
		rid[0].dwFlags = RIDEV_INPUTSINK;
		rid[0].hwndTarget = hwnd;
		auto res = RegisterRawInputDevices(rid, 1, sizeof(rid[0]));
		DEBUGLOG("Reregister mouse result: " << res << "\n");
	}

	//Reregister mouse
	{
		RAWINPUTDEVICE rid[1];
		rid[0].usUsagePage = 0x01;
		rid[0].usUsage = 0x06;
		rid[0].dwFlags = RIDEV_INPUTSINK;
		rid[0].hwndTarget = hwnd;
		auto res = RegisterRawInputDevices(rid, 1, sizeof(rid[0]));
		DEBUGLOG("Reregister keyboard result: " << res << "\n");
	}
}