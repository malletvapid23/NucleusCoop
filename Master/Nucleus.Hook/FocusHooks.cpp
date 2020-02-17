#include "pch.h"
#include "InstallHooks.h"
#include "Logging.h"
#include "Globals.h"

HWND WINAPI GetForegroundWindow_Hook()
{
	return hWnd;
}

HWND WINAPI WindowFromPoint_Hook(POINT Point)
{
	return hWnd;
}

HWND WINAPI GetActiveWindow_Hook()
{
	return hWnd;
}

BOOL WINAPI IsWindowEnabled_Hook(HWND hWnd)
{
	return TRUE;
}

HWND WINAPI GetFocus_Hook()
{
	return hWnd;
}

HWND WINAPI GetCapture_Hook()
{
	return hWnd;
}

void installFocusHooks()
{
	DEBUGLOG("Injecting Focus hooks\n");
	installHook("user32", "GetForegroundWindow", GetForegroundWindow_Hook);
	installHook("user32", "WindowFromPoint", WindowFromPoint_Hook);
	installHook("user32", "GetActiveWindow", GetActiveWindow_Hook);
	installHook("user32", "IsWindowEnabled", IsWindowEnabled_Hook);
	installHook("user32", "GetFocus", GetFocus_Hook);
	installHook("user32", "GetCapture", GetCapture_Hook);
}