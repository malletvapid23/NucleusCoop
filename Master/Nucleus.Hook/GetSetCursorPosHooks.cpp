#include "pch.h"
#include "InstallHooks.h"
#include "Logging.h"
#include "Globals.h"
#include "FakeMouse.h"

//TODO: remove temporary legacy input / use abs cursor pos 

BOOL WINAPI GetCursorPos_Hook(LPPOINT lpPoint)
{
	if (lpPoint)
	{
		EnterCriticalSection(&mcs);
		if ((!options.legacyInput) || use_absolute_cursor_pos)
		{
			//Absolute mouse position (always do this if legacy input is off)
			lpPoint->x = absolute_x;
			lpPoint->y = absolute_y;
		}
		else
		{
			//Delta mouse position
			lpPoint->x = fake_x;
			lpPoint->y = fake_y;
		}

		LeaveCriticalSection(&mcs);
		ClientToScreen(hWnd, lpPoint);

		update_absolute_cursor_check();
	}
	return true;
}

BOOL WINAPI SetCursorPos_Hook(int X, int Y)
{
	POINT p;
	p.x = X;
	p.y = Y;

	//SetCursorPos require screen coordinates (relative to 0,0 of monitor)
	ScreenToClient(hWnd, &p);

	origin_x = p.x;
	origin_y = p.y;

	if (!options.legacyInput)
	{
		EnterCriticalSection(&mcs);
		absolute_x = p.x;
		absolute_y = p.y;
		LeaveCriticalSection(&mcs);
	}
	else
	{
		EnterCriticalSection(&mcs);
		fake_x = p.x;
		fake_y = p.y;
		LeaveCriticalSection(&mcs);

		use_absolute_cursor_pos_counter = 0;
		use_absolute_cursor_pos = false;
	}

	return TRUE;
}

void install_set_cursor_pos_hook()
{
	DEBUGLOG("Injecting SetCursorPos hook\n");
	HookInstall("user32", "SetCursorPos", SetCursorPos_Hook);
}


void install_get_cursor_pos_hook()
{
	DEBUGLOG("Injecting GetCursorPos hook\n");
	HookInstall("user32", "GetCursorPos", GetCursorPos_Hook);
}