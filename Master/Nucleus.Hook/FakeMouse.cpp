#include "pch.h"
#include "Logging.h"
#include "FakeMouse.h"
#include "Globals.h"
#include "Piping.h"

CRITICAL_SECTION mcs;
int fake_x; //Delta X
int fake_y;

int absolute_x;
int absolute_y;

int use_absolute_cursor_pos_counter = 0; // 0/1/2/3/... : FALSE, requiredAbsCount : TRUE
const int required_abs_count = 40;
int origin_x, origin_y;

HANDLE allowed_mouse_handle = HANDLE(-1); //We will allow raw input from this mouse handle.
HANDLE allowed_keyboard_handle = HANDLE(-1);

bool use_absolute_cursor_pos = true;

int lastX, lastY;

bool sentVisibility = true;//The visibility last sent to the C# side

void update_absolute_cursor_check()
{
	if (options.legacyInput && !use_absolute_cursor_pos)
	{
		//We assume we're in a menu and need absolute cursor pos
		use_absolute_cursor_pos_counter++;
		if (use_absolute_cursor_pos_counter == required_abs_count)
		{
			use_absolute_cursor_pos = true;
		}
	}
}

void SetCursorVisibility(bool show)
{
	if (show != sentVisibility)
	{
		BYTE buffer[9] = { 0x06, 0, 0, 0, (show ? 1 : 0), 0, 0, 0, 0 };

		DWORD bytesRead = 0;

		BOOL result = WriteFile(
			Piping::hPipeWrite,
			buffer,
			9 * sizeof(BYTE),
			&bytesRead,
			nullptr
		);

		if (result == FALSE)
		{
			DEBUGLOG("SetCursorVisibility fail, error = " << GetLastError() << "\n")
		}

		sentVisibility = show;
	}
}