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

void install_focus_hooks()
{
	DEBUGLOG("Injecting Focus hooks\n");
	HookInstall("user32", "GetForegroundWindow", GetForegroundWindow_Hook);
	HookInstall("user32", "WindowFromPoint", WindowFromPoint_Hook);
	HookInstall("user32", "GetActiveWindow", GetActiveWindow_Hook);
	HookInstall("user32", "IsWindowEnabled", IsWindowEnabled_Hook);
	HookInstall("user32", "GetFocus", GetFocus_Hook);
	HookInstall("user32", "GetCapture", GetCapture_Hook);
}