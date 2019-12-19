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

HWND hWnd = 0;

#ifdef _DEBUG
bool IsDebug = false;
#else
bool IsDebug = true;
#endif

std::ofstream outfile;
std::wstring nucleusFolder;
std::wstring logFile = L"\\debug-log.txt";

std::ofstream& get_outfile()
{
	outfile.open(nucleusFolder + logFile, std::ios_base::app);
	return outfile;
}

std::string ws2s(const std::wstring& wstr)
{
	using convert_typeX = std::codecvt_utf8<wchar_t>;
	std::wstring_convert<convert_typeX, wchar_t> converterX;

	return converterX.to_bytes(wstr);
}

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

LRESULT CALLBACK WndProc_Hook(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	switch (uMsg)
	{
		case WM_KILLFOCUS:
		{
			//SetFocus(hWnd);
			return -1;
		}

		default:
			DefWindowProc(hwnd, uMsg, wParam, lParam);
	}
	return 1;
}

inline int bytesToInt(BYTE* bytes)
{
	return (int)(bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3]);
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

	BYTE* data = inRemoteInfo->UserData;

	if ((INT)hWnd == 0)
	{
		hWnd = (HWND)bytesToInt(data);
	}

	BYTE* _p = data + 4;

	//IsDebug = data[6] == 1;
	//const bool HideCursor = data[7] == 1;
	//const bool HookFocus = data[8] == 1;
	bool PreventWindowDeactivation = *(_p++) == 1;
	bool SetWindow = *(_p++) == 1;
	IsDebug = *(_p++) == 1;
	bool HideCursor = *(_p++) == 1;
	bool HookFocus = *(_p++) == 1;

	const size_t pathLength = (data[9] << 24) + (data[10] << 16) + (data[11] << 8) + data[12];
	auto nucleusFolderPath = static_cast<PWSTR>(malloc(pathLength + sizeof(WCHAR)));
	memcpy(nucleusFolderPath, &data[13], pathLength);
	nucleusFolderPath[pathLength / sizeof(WCHAR)] = '\0';

	nucleusFolder = nucleusFolderPath;

	DEBUGLOG("Starting hook injection," <<
		" SetWindow: " << SetWindow << 
		" HookFocus: " << HookFocus << 
		" HideCursor: " << HideCursor << 
		" PreventWindowDeactivation: " << PreventWindowDeactivation << 
		"\n");
	
	if (SetWindow)
	{
		install_set_window_hook();
	}

	if (HookFocus)
	{
		install_focus_hooks();		
	}

	if (PreventWindowDeactivation)
	{
		DEBUGLOG("Preventing window deactivation by blocking WM_KILLFOCUS\n");
		//TODO: Windows can terminate hooks if they take too long or intercept too many messages. Replace with GetMessage hooks/etc.
		WNDPROC g_OldWndProc = (WNDPROC)SetWindowLongPtr(hWnd, GWLP_WNDPROC, (LONG_PTR)WndProc_Hook);
	}

	if (HideCursor)
	{
		install_hide_cursor_hooks();

		//WNDPROC g_OldWndProc = (WNDPROC)SetWindowLongPtr(hWnd, GWLP_WNDPROC, (LONG_PTR)WndProc_Hook);
	}

	DEBUGLOG("Hook injection complete\n");

	return;
}