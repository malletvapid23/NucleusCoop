#pragma once
#include <Windows.h>
#include <easyhook.h>

NTSTATUS installHook(LPCSTR moduleHandle, LPCSTR proc, void* callBack);

void installSetWindowHook();

void installFocusHooks();

void installHideCursorHooks();

void installSetCursorPosHook();
void installGetCursorPosHook();

void installGetAsyncKeyStateHook();
void installGetKeyStateHook();
void installGetKeyboardStateHook();

void installMessageFilterHooks();