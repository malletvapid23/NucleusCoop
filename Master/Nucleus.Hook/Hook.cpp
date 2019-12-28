#include "pch.h"
#include "easyhook.h"
#include "framework.h"
//#include "string"
#include "windows.h"
//#include <sstream>
//#include <ios>
#include <fstream>
//#include <atlbase.h>
#include <locale>
#include <codecvt>
#include <ctime>
#include <stdio.h>
#include <iomanip>
#include "Logging.h"
#include "InstallHooks.h"
#include "Globals.h"
#include "KeyStates.h"
#include "FakeMouse.h"
#include "Piping.h"

#ifdef _DEBUG
bool IsDebug = false;
#else
bool IsDebug = true;
#endif

//Logging.h
std::ofstream logging_outfile;
std::wstring nucleusFolder;

//Globals.h
Options options;
HWND hWnd = 0;

std::string date_string()
{
	tm tinfo;
	time_t rawtime;
	std::time(&rawtime);
	localtime_s(&tinfo, &rawtime);
	char buffer[21];
	strftime(buffer, 21, "%Y-%m-%d %H:%M:%S", &tinfo);
	return "[" + std::string(buffer) + "]";
}

// Structure used to communicate data from and to enumeration procedure
struct EnumData {
	DWORD dwProcessId;
	HWND hWnd;
};

// Application-defined callback for EnumWindows
BOOL CALLBACK EnumProc(HWND hWnd, LPARAM lParam) {
	// Retrieve storage location for communication data
	EnumData& ed = *(EnumData*)lParam;
	DWORD dwProcessId = 0x0;
	// Query process ID for hWnd
	GetWindowThreadProcessId(hWnd, &dwProcessId);
	// Apply filter - if you want to implement additional restrictions,
	// this is the place to do so.
	if (ed.dwProcessId == dwProcessId) {
		// Found a window matching the process ID
		ed.hWnd = hWnd;
		// Report success
		SetLastError(ERROR_SUCCESS);
		// Stop enumeration
		return FALSE;
	}
	// Continue enumeration
	return TRUE;
}

// Main entry
HWND FindWindowFromProcessId(DWORD dwProcessId) {
	EnumData ed = { dwProcessId };
	if (!EnumWindows(EnumProc, (LPARAM)& ed) &&
		(GetLastError() == ERROR_SUCCESS)) {
		return ed.hWnd;
	}
	return NULL;
}

// Helper method for convenience
HWND FindWindowFromProcess(HANDLE hProcess) {
	return FindWindowFromProcessId(GetProcessId(hProcess));
}

NTSTATUS HookInstall(LPCSTR moduleHandle, LPCSTR proc, void* callBack)
{
	// Perform hooking
	HOOK_TRACE_INFO hHook = { NULL }; // keep track of our hook

	// Install the hook
	NTSTATUS result = LhInstallHook(
		GetProcAddress(GetModuleHandle(moduleHandle), proc),
		callBack,
		NULL,
		&hHook);
	
	if (FAILED(result))
	{
		DEBUGLOG("Error installing " << proc << " hook, error msg: " << RtlGetLastErrorString() << "\n")
	}
	else
	{
		// If the threadId in the ACL is set to 0,
		// then internally EasyHook uses GetCurrentThreadId()
		ULONG ACLEntries[1] = { 0 };

		// Disable the hook for the provided threadIds, enable for all others
		LhSetExclusiveACL(ACLEntries, 1, &hHook);

		DEBUGLOG("Successfully installed " << proc << " hook, in module: " << moduleHandle << ", result: " << result << "\n")
	}

	return result;
}

// EasyHook will be looking for this export to support DLL injection. If not found then 
// DLL injection will fail.
extern "C" void __declspec(dllexport) __stdcall NativeInjectionEntryPoint(REMOTE_ENTRY_INFO* inRemoteInfo);

void __stdcall NativeInjectionEntryPoint(REMOTE_ENTRY_INFO* inRemoteInfo)
{
	DWORD pid = GetCurrentProcessId();
	hWnd = FindWindowFromProcessId(pid);

	InitializeCriticalSection(&mcs);

	BYTE* data = inRemoteInfo->UserData;
	BYTE* _p = data;

	if (reinterpret_cast<INT>(hWnd) == 0)
	{
		hWnd = reinterpret_cast<HWND>(bytesToInt(_p));
	}
	_p += 4;

	allowed_mouse_handle = reinterpret_cast<HANDLE>(bytesToInt(_p));
	_p += 4;
	
	allowed_keyboard_handle = reinterpret_cast<HANDLE>(bytesToInt(_p));
	_p += 4;

#define NEXTBOOL *(_p++) == 1
	options.preventWindowDeactivation = NEXTBOOL;
	options.setWindow = NEXTBOOL;
	IsDebug = NEXTBOOL;
	options.hideCursor = NEXTBOOL;
	options.hookFocus = NEXTBOOL;
	options.setCursorPos = NEXTBOOL;
	options.getCursorPos = NEXTBOOL;
	options.getKeyState = NEXTBOOL;
	options.getAsyncKeyState = NEXTBOOL;
	options.getKeyboardState = NEXTBOOL;
	options.filterRawInput = NEXTBOOL;
	options.filterMouseMessages = NEXTBOOL;
	options.legacyInput = NEXTBOOL;
	options.updateAbsoluteFlagInMouseMessage = NEXTBOOL;
	options.mouseVisibilitySendBack = NEXTBOOL;
#undef NEXTBOOL

	const auto pathLength = static_cast<size_t>(bytesToInt(_p));
	const auto writePipeNameLength = static_cast<size_t>(bytesToInt(_p + 4));
	const auto readPipeNameLength = static_cast<size_t>(bytesToInt(_p + 8));
	_p += 12;

	//C# gives number of bytes without null termination
	auto nucleusFolderPath = static_cast<PWSTR>(malloc(pathLength + sizeof(WCHAR)));
	memcpy(nucleusFolderPath, _p, pathLength);
	_p += pathLength;
	nucleusFolderPath[pathLength / sizeof(WCHAR)] = '\0';//Null-terminate the string
	nucleusFolder = nucleusFolderPath;

	Piping::writePipeName = std::wstring(reinterpret_cast<wchar_t*>(_p), writePipeNameLength/2);
	_p += writePipeNameLength;

	Piping::readPipeName = std::wstring(reinterpret_cast<wchar_t*>(_p), readPipeNameLength/2);
	_p += readPipeNameLength;

	DEBUGLOG("Starting hook injection," <<
		" WritePipeName: " << std::string(Piping::writePipeName.begin(), Piping::writePipeName.end()) <<
		" ReadPipeName: " << std::string(Piping::readPipeName.begin(), Piping::readPipeName.end()) <<
		" preventWindowDeactivation: " << options.preventWindowDeactivation <<
		" setCursorPos: " << options.setCursorPos <<
		" getCursorPos: " << options.getCursorPos <<
		" getKeyState: " << options.getKeyState <<
		" getAsyncKeyState: " << options.getAsyncKeyState <<
		" getKeyboardState: " << options.getKeyboardState <<
		" filterRawInput: " << options.filterRawInput <<
		" filterMouseMessages: " << options.filterMouseMessages <<
		" legacyInput: " << options.legacyInput <<
		" updateAbsoluteFlagInMouseMessage: " << options.updateAbsoluteFlagInMouseMessage <<
		" setWindow: " << options.setWindow <<
		" hideCursor: " << options.hideCursor <<
		" hookFocus: " << options.hookFocus <<
		"\n");

	if (options.setWindow)
		install_set_window_hook();

	if (options.hookFocus)
		install_focus_hooks();

	if (options.hideCursor || options.mouseVisibilitySendBack)
		install_hide_cursor_hooks();

	if (options.getCursorPos)
		install_get_cursor_pos_hook();

	if (options.setCursorPos)
		install_set_cursor_pos_hook();

	if (options.getAsyncKeyState)
		install_get_async_key_state_hook();

	if (options.getKeyState)
		install_get_key_state_hook();

	if (options.getKeyboardState)
		install_get_keyboard_state_hook();

	if (options.filterRawInput || options.filterMouseMessages || options.legacyInput || options.preventWindowDeactivation)
	{
		install_message_filter_hooks();
	}

	DEBUGLOG("Hook injection complete\n");

	Piping::startPipeListen();
}