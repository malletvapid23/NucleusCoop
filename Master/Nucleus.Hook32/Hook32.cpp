#include "pch.h"
#include "easyhook.h"
#include "framework.h"
//#include "string"
//#include "windows.h"
//#include "winuser.h"
//#include <sstream>
//#include <ios>
//#include <iostream>
#include <fstream>
//#include <vector>
#include <locale>
#include <codecvt>
#include <time.h>
#include <stdio.h>
using namespace std;

HWND hWnd = 0;

bool IsDebug = false;

std::ofstream outfile;
std::wstring nucleusFolder;
std::wstring logFile = L"\\debug-log.txt";

std::string ws2s(const std::wstring& wstr)
{
	using convert_typeX = std::codecvt_utf8<wchar_t>;
	std::wstring_convert<convert_typeX, wchar_t> converterX;

	return converterX.to_bytes(wstr);
}

inline std::string date_string()
{
	time_t rawtime;
	std::time(&rawtime);
	struct tm* tinfo = std::localtime(&rawtime);
	char buffer[21];
	strftime(buffer, 21, "%Y-%m-%d %H:%M:%S", tinfo);
	return "[" + std::string(buffer) + "]";
}

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

int WINAPI ShowCursor_Hook(BOOL bShow)
{
	return ShowCursor(FALSE);
}

HCURSOR WINAPI SetCursor_Hook(HCURSOR hCursor)
{
	return SetCursor(nullptr);
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
		if (IsDebug)
		{
			outfile.open(nucleusFolder + logFile, std::ios_base::app);
			outfile << date_string() << "HOOK32: Error installing " << proc << " hook, error msg: " << RtlGetLastErrorString() << "\n";
		}
	}
	else
	{
		// If the threadId in the ACL is set to 0,
		// then internally EasyHook uses GetCurrentThreadId()
		ULONG ACLEntries[1] = { 0 };

		// Disable the hook for the provided threadIds, enable for all others
		LhSetExclusiveACL(ACLEntries, 1, &hHook);

		if (IsDebug)
		{
			outfile.open(nucleusFolder + logFile, std::ios_base::app);
			outfile << date_string() << "HOOK32: Successfully installed " << proc << " hook, in module: " << moduleHandle << ", result: " << result << "\n";
		}
	}
	outfile.flush();
	outfile.close();
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

	BYTE* _p = data + 6;

	//IsDebug = data[6] == 1;
	//const bool HideCursor = data[7] == 1;
	//const bool HookFocus = data[8] == 1;
	IsDebug = *(_p++) == 1;
	bool HideCursor = *(_p++) == 1;
	bool HookFocus = *(_p++) == 1;

	const size_t pathLength = (data[9] << 24) + (data[10] << 16) + (data[11] << 8) + data[12];
	auto nucleusFolderPath = static_cast<PWSTR>(malloc(pathLength + sizeof(WCHAR)));
	memcpy(nucleusFolderPath, &data[13], pathLength);
	nucleusFolderPath[pathLength / sizeof(WCHAR)] = '\0';

	nucleusFolder = nucleusFolderPath;

	if (IsDebug)
	{
		outfile.open(nucleusFolder + logFile, std::ios_base::app);
		outfile << date_string() << "HOOK32: Starting hook injection, HideCursor: " << HideCursor << " HookFocus: " << HookFocus << "\n";
		outfile.flush();
		outfile.close();
	}

	if (HookFocus)
	{
		if (IsDebug)
		{
			outfile.open(nucleusFolder + logFile, std::ios_base::app);
			outfile << date_string() << "HOOK32: Injecting HookFocus hooks\n";
			outfile.flush();
			outfile.close();
		}
		HookInstall("user32", "GetForegroundWindow", GetForegroundWindow_Hook);
		HookInstall("user32", "WindowFromPoint", WindowFromPoint_Hook);
		HookInstall("user32", "GetActiveWindow", GetActiveWindow_Hook);
		HookInstall("user32", "IsWindowEnabled", IsWindowEnabled_Hook);
		HookInstall("user32", "GetFocus", GetFocus_Hook);
		HookInstall("user32", "GetCapture", GetCapture_Hook);
	}

	if (HideCursor)
	{
		if (IsDebug)
		{
			outfile.open(nucleusFolder + logFile, std::ios_base::app);
			outfile << date_string() << "HOOK32: Injecting HideCursor hooks\n";
			outfile.flush();
			outfile.close();
		}
		HookInstall("user32", "ShowCursor", ShowCursor_Hook);
		HookInstall("user32", "SetCursor", SetCursor_Hook);
	}

	if (IsDebug)
	{
		outfile.open(nucleusFolder + logFile, std::ios_base::app);
		outfile << date_string() << "HOOK32: Hook injection complete\n";
		outfile.flush();
		outfile.close();
	}

	return;
}