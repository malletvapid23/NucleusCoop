﻿#pragma once

class Options
{
public:
	bool preventWindowDeactivation;
	bool setWindow;
	bool hideCursor;
	bool hookFocus;
	bool setCursorPos;
	bool getCursorPos;
	bool getKeyState;
	bool getAsyncKeyState;
	bool getKeyboardState;
	bool filterRawInput;
	bool filterMouseMessages;
	bool legacyInput;
	bool updateAbsoluteFlagInMouseMessage;
	bool mouseVisibilitySendBack;//Updates C# side whenever the game changes the mouse visibility
};
