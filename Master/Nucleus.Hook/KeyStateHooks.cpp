#include "pch.h"
#include "InstallHooks.h"
#include "Logging.h"
#include "KeyStates.h"

SHORT WINAPI GetAsyncKeyState_Hook(int vKey)
{
	return is_vkey_down(vKey) ? 0b1000000000000000 : 0;
}

SHORT WINAPI GetKeyState_Hook(int nVirtKey)
{
	return is_vkey_down(nVirtKey) ? 0b1000000000000000 : 0;
}

BOOL WINAPI GetKeyboardState_Hook(PBYTE lpKeyState)
{
	memset(lpKeyState, 0, 256);

	for (int vkey = 0; vkey < 256; vkey++)
	{
		lpKeyState[vkey] = is_vkey_down(vkey) ? 0b10000000 : 0;
	}

	return TRUE;
}

void install_get_async_key_state_hook()
{
	DEBUGLOG("Injecting GetAsyncKeyState hook\n");
	HookInstall(TEXT("user32"), "GetAsyncKeyState", GetAsyncKeyState_Hook);
}

void install_get_key_state_hook()
{
	DEBUGLOG("Injecting GetKeyState hook\n");
	HookInstall(TEXT("user32"), "GetKeyState", GetKeyState_Hook);
}

void install_get_keyboard_state_hook()
{
	DEBUGLOG("Injecting GetKeyboardState hook\n");
	HookInstall(TEXT("user32"), "GetKeyboardState", GetKeyboardState_Hook);
}