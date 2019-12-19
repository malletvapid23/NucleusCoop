#include "pch.h"
#include "InstallHooks.h"
#include "Logging.h"

int WINAPI ShowCursor_Hook(BOOL bShow)
{
	return ShowCursor(FALSE);
}

HCURSOR WINAPI SetCursor_Hook(HCURSOR hCursor)
{
	return SetCursor(nullptr);
}

void install_hide_cursor_hooks()
{
	DEBUGLOG("Injecting HideCursor hooks\n");
	HookInstall("user32", "ShowCursor", ShowCursor_Hook);
	HookInstall("user32", "SetCursor", SetCursor_Hook);
}