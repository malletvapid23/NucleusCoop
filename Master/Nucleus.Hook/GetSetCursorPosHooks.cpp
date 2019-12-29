#include "pch.h"
#include "InstallHooks.h"
#include "Logging.h"
#include "Globals.h"
#include "FakeMouse.h"
using namespace FakeMouse;

//TODO: remove temporary legacy input / use abs cursor pos 

BOOL WINAPI GetCursorPos_Hook(LPPOINT lpPoint)
{
	if (lpPoint)
	{
		EnterCriticalSection(&fakeMouseCriticalSection);
		if (!options.legacyInput || useAbsoluteCursorPos)
		{
			//Absolute mouse position (always do this if legacy input is off)
			lpPoint->x = absoluteX;
			lpPoint->y = absoluteY;
		}
		else
		{
			//Delta mouse position
			lpPoint->x = fakeX;
			lpPoint->y = fakeY;
		}

		LeaveCriticalSection(&fakeMouseCriticalSection);
		ClientToScreen(hWnd, lpPoint);

		updateAbsoluteCursorCheck();
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

	originX = p.x;
	originY = p.y;

	if (!options.legacyInput)
	{
		EnterCriticalSection(&fakeMouseCriticalSection);
		absoluteX = p.x;
		absoluteY = p.y;
		LeaveCriticalSection(&fakeMouseCriticalSection);
	}
	else
	{
		EnterCriticalSection(&fakeMouseCriticalSection);
		fakeX = p.x;
		fakeY = p.y;
		LeaveCriticalSection(&fakeMouseCriticalSection);

		useAbsoluteCursorPosCounter = 0;
		useAbsoluteCursorPos = false;
	}

	return TRUE;
}

void installSetCursorPosHook()
{
	DEBUGLOG("Injecting SetCursorPos hook\n");
	installHook("user32", "SetCursorPos", SetCursorPos_Hook);
}


void installGetCursorPosHook()
{
	DEBUGLOG("Injecting GetCursorPos hook\n");
	installHook("user32", "GetCursorPos", GetCursorPos_Hook);
}