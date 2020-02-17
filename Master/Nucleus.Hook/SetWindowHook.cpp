#include "pch.h"
#include "InstallHooks.h"
#include "Logging.h"

BOOL WINAPI SetWindowPos_Hook(HWND hWnd, HWND hWndInsertAfter, int X, int Y, int cx, int cy, UINT uFlags)
{
	return true;
}

void installSetWindowHook()
{
	DEBUGLOG("Injecting SetWindow hook\n");
	installHook("user32", "SetWindowPos", SetWindowPos_Hook);
}