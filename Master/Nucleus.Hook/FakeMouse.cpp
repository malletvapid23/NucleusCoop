#include "pch.h"
#include "Logging.h"
#include "FakeMouse.h"
#include "Globals.h"
#include "Piping.h"

CRITICAL_SECTION mcs;
int fakeX; //Delta X
int fakeY;

int absoluteX;
int absoluteY;

int useAbsoluteCursorPosCounter = 0; // 0/1/2/3/... : FALSE, requiredAbsCount : TRUE
const int REQUIRED_ABS_COUNT = 40;
int originX, originY;

HANDLE allowedMouseHandle = HANDLE(-1); //We will allow raw input from this mouse handle.
HANDLE allowedKeyboardHandle = HANDLE(-1);

bool useAbsoluteCursorPos = true;

int lastX, lastY;

bool sentVisibility = true;//The visibility last sent to the C# side

void updateAbsoluteCursorCheck()
{
	if (options.legacyInput && !useAbsoluteCursorPos)
	{
		//We assume we're in a menu and need absolute cursor pos
		useAbsoluteCursorPosCounter++;
		if (useAbsoluteCursorPosCounter == REQUIRED_ABS_COUNT)
		{
			useAbsoluteCursorPos = true;
		}
	}
}

void setCursorVisibility(const bool show)
{
	if (show != sentVisibility)
	{
		BYTE buffer[9] = { 0x06, 0, 0, 0, (show ? 1 : 0), 0, 0, 0, 0 };

		DWORD bytesRead = 0;

		const BOOL result = WriteFile(
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