#include "pch.h"
#include "easyhook.h"
#include "framework.h"
#include "windows.h"
#include <locale>
#include <iomanip>
#include "Logging.h"
#include "InstallHooks.h"
#include "Globals.h"
#include "Piping.h"
#include "FakeMouse.h"

#ifdef _DEBUG
bool IsDebug = false;
#else
bool IsDebug = true;
#endif

//Globals.h
Options options;
HWND hWnd = nullptr;

// Structure used to communicate data from and to enumeration procedure
struct EnumData {
	DWORD dwProcessId;
	HWND hWnd;
};

// Application-defined callback for EnumWindows
BOOL CALLBACK EnumProc(HWND hWnd, LPARAM lParam) {
	// Retrieve storage location for communication data
	EnumData& ed = *reinterpret_cast<EnumData*>(lParam);
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
	if (!EnumWindows(EnumProc, reinterpret_cast<LPARAM>(&ed)) &&
		(GetLastError() == ERROR_SUCCESS)) {
		return ed.hWnd;
	}
	return NULL;
}

// Helper method for convenience
HWND FindWindowFromProcess(HANDLE hProcess) {
	return FindWindowFromProcessId(GetProcessId(hProcess));
}

NTSTATUS installHook(const LPCSTR moduleHandle, const LPCSTR proc, void* callBack)
{
	// Perform hooking
	HOOK_TRACE_INFO hHook = { NULL }; // keep track of our hook

	// Install the hook
	const auto result = LhInstallHook(
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
	const auto pid = GetCurrentProcessId();
	hWnd = FindWindowFromProcessId(pid);

	InitializeCriticalSection(&FakeMouse::fakeMouseCriticalSection);

	BYTE* data = inRemoteInfo->UserData;
	auto p = data;

	if (reinterpret_cast<INT>(hWnd) == 0)
	{
		hWnd = reinterpret_cast<HWND>(bytesToInt(p));
	}
	p += 4;

	FakeMouse::allowedMouseHandle = reinterpret_cast<HANDLE>(bytesToInt(p));
	p += 4;

	FakeMouse::allowedKeyboardHandle = reinterpret_cast<HANDLE>(bytesToInt(p));
	p += 4;

#define NEXTBOOL *(p++) == 1
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

	const auto pathLength = static_cast<size_t>(bytesToInt(p));
	const auto writePipeNameLength = static_cast<size_t>(bytesToInt(p + 4));
	const auto readPipeNameLength = static_cast<size_t>(bytesToInt(p + 8));
	p += 12;

	//C# gives number of bytes without null termination
	const auto nucleusFolderPath = static_cast<PWSTR>(malloc(pathLength + sizeof(WCHAR)));
	memcpy(nucleusFolderPath, p, pathLength);
	p += pathLength;
	nucleusFolderPath[pathLength / sizeof(WCHAR)] = '\0';//Null-terminate the string
	Logging::nucleusFolder = nucleusFolderPath;

	Piping::writePipeName = std::wstring(reinterpret_cast<wchar_t*>(p), writePipeNameLength/2);
	p += writePipeNameLength;

	Piping::readPipeName = std::wstring(reinterpret_cast<wchar_t*>(p), readPipeNameLength/2);
	p += readPipeNameLength;

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
		installSetWindowHook();

	if (options.hookFocus)
		installFocusHooks();

	if (options.hideCursor || options.mouseVisibilitySendBack)
		installHideCursorHooks();

	if (options.getCursorPos)
		installGetCursorPosHook();

	if (options.setCursorPos)
		installSetCursorPosHook();

	if (options.getAsyncKeyState)
		installGetAsyncKeyStateHook();

	if (options.getKeyState)
		installGetKeyStateHook();

	if (options.getKeyboardState)
		installGetKeyboardStateHook();

	if (options.filterRawInput || options.filterMouseMessages || options.legacyInput || options.preventWindowDeactivation)
	{
		installMessageFilterHooks();
	}

	DEBUGLOG("Hook injection complete\n");

	Piping::startPipeListen();
}