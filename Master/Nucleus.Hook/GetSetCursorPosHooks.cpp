#include "pch.h"
#include "InstallHooks.h"
#include "Logging.h"
#include "Globals.h"

//TODO: remove temporary legacy input / use abs cursor pos 
bool enable_legacy_input = false;
bool use_absolute_cursor_pos = true;

//TODO: legacy input needs to be moved into its own file.
int use_absolute_cursor_pos_counter = 0; // 0/1/2/3/... : FALSE, requiredAbsCount : TRUE
const int required_abs_count = 40;
int origin_x, origin_y;

void update_absolute_cursor_check()
{
	if (enable_legacy_input && !use_absolute_cursor_pos)
	{
		//We assume we're in a menu and need absolute cursor pos
		use_absolute_cursor_pos_counter++;
		if (use_absolute_cursor_pos_counter == required_abs_count)
		{
			use_absolute_cursor_pos = true;
		}
	}
}

BOOL WINAPI GetCursorPos_Hook(LPPOINT lpPoint)
{
	if (lpPoint)
	{
		EnterCriticalSection(&mcs);
		if ((!enable_legacy_input) || use_absolute_cursor_pos)
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

	if (!enable_legacy_input)
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