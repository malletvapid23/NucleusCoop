#include "pch.h"
#include "Logging.h"
#include "FakeMouse.h"
#include "Globals.h"

CRITICAL_SECTION mcs;
int fake_x; //Delta X
int fake_y;

int absolute_x;
int absolute_y;

int use_absolute_cursor_pos_counter = 0; // 0/1/2/3/... : FALSE, requiredAbsCount : TRUE
const int required_abs_count = 40;
int origin_x, origin_y;

HANDLE allowed_mouse_handle = nullptr; //We will allow raw input from this mouse handle.

bool use_absolute_cursor_pos = true;

int lastX, lastY;

void update_absolute_cursor_check()
{
	if (enableLegacyInput && !use_absolute_cursor_pos)
	{
		//We assume we're in a menu and need absolute cursor pos
		use_absolute_cursor_pos_counter++;
		if (use_absolute_cursor_pos_counter == required_abs_count)
		{
			use_absolute_cursor_pos = true;
		}
	}
}