#pragma once
#include <ntstatus.h>
#include <Windows.h>
#include <easyhook.h>

NTSTATUS HookInstall(LPCSTR moduleHandle, LPCSTR proc, void* callBack);

void install_set_window_hook();

void install_focus_hooks();

void install_hide_cursor_hooks();

void install_set_cursor_hook();

void install_get_cursor_hook();