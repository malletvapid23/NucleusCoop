#include "pch.h"
#include "InstallHooks.h"
#include "Logging.h"
#include "Globals.h"
#include "FakeMouse.h"

int WINAPI ShowCursor_Hook(BOOL bShow)
{
	if (options.hideCursor)
	{
		return ShowCursor(FALSE);
	}
	
	SetCursorVisibility(bShow == TRUE);
	if (bShow == FALSE) ShowCursor(FALSE);
	return (bShow == TRUE) ? 0 : -1;
}

HCURSOR WINAPI SetCursor_Hook(HCURSOR hCursor)
{
	if (options.hideCursor)
	{
		return SetCursor(nullptr);
	}

	SetCursorVisibility(hCursor != nullptr);
	if (hCursor == nullptr) SetCursor(nullptr);
	return hCursor;
}

void install_hide_cursor_hooks()
{
	DEBUGLOG("Injecting ShowCursor and SetCursor hooks\n");
	HookInstall("user32", "ShowCursor", ShowCursor_Hook);
	HookInstall("user32", "SetCursor", SetCursor_Hook);
}