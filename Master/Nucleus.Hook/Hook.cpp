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

#ifdef _DEBUG
bool IsDebug = false;
#else
bool IsDebug = true;
#endif

//TODO: remove temporary hook enabled options
bool hookSetCursorPos = true;
bool hookGetCursorPos = true;
bool hookGetKeyState = true;
bool hookGetAsyncKeyState = true;
bool hookGetKeyboardState = true;

HWND hWnd = 0;

//TODO: move to logging.cpp
std::ofstream outfile;
std::wstring nucleusFolder;
std::wstring logFile = L"\\debug-log-hooks.txt";//Use different path to C# side or it can crash if writing at the same time

std::wstring _writePipeName;
std::wstring _readPipeName;
HANDLE hPipeRead;
HANDLE hPipeWrite;
bool pipe_closed = false;

//TODO: move mouse into own file
CRITICAL_SECTION mcs;
int fake_x; //Delta X
int fake_y;

int absolute_x;
int absolute_y;

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

void startPipeListen()
{
	//Read pipe
	char _pipeNameChars[256];
	sprintf_s(_pipeNameChars, "\\\\.\\pipe\\%s", std::string(_readPipeName.begin(), _readPipeName.end()).c_str());

	hPipeRead = CreateFile(
		_pipeNameChars,
		GENERIC_READ,
		FILE_SHARE_READ | FILE_SHARE_WRITE,
		nullptr,
		OPEN_EXISTING,
		FILE_ATTRIBUTE_NORMAL,
		nullptr
	);

	if (hPipeRead == INVALID_HANDLE_VALUE)
	{
		DEBUGLOG("Failed to connect to pipe (read)\n")
		return;
	}

	DEBUGLOG("Connected to pipe (read)\n")

	//Write pipe
	char _pipeNameCharsWrite[256];
	sprintf_s(_pipeNameCharsWrite, "\\\\.\\pipe\\%s", std::string(_writePipeName.begin(), _writePipeName.end()).c_str());

	hPipeWrite = CreateFile(
		_pipeNameCharsWrite,
		GENERIC_WRITE,
		FILE_SHARE_READ | FILE_SHARE_WRITE,
		nullptr,
		OPEN_EXISTING,
		FILE_ATTRIBUTE_NORMAL,
		nullptr
	);

	if (hPipeWrite == INVALID_HANDLE_VALUE)
	{
		DEBUGLOG("Failed to connect to pipe (write)\n")
	}
	else
	{
		DEBUGLOG("Connected to pipe (write)\n")
	}

	//Loop until pipe close message is received
	for (;;)
	{
		BYTE buffer[9]; //9 bytes are sent at a time (1st is message, next 8 for 2 ints)
		DWORD bytesRead = 0;

		BOOL result = ReadFile(
			hPipeRead,
			buffer,
			9 * sizeof(BYTE),
			&bytesRead,
			nullptr
		);

		if (result && bytesRead == 9)
		{
			int param1 = bytesToInt(&buffer[1]);

			int param2 = bytesToInt(&buffer[5]);

			//cout << "Received message. Msg=" << (int)buffer[0] << ", param1=" << param1 << ", param2=" << param2 << "\n";

			switch (buffer[0])
			{
			case 0x01: //Add delta cursor pos
			{
				EnterCriticalSection(&mcs);
				fake_x += param1;
				fake_y += param2;
				LeaveCriticalSection(&mcs);
				break;
			}
			case 0x04: //Set absolute cursor pos
			{
				EnterCriticalSection(&mcs);
				absolute_x = param1;
				absolute_y = param2;
				LeaveCriticalSection(&mcs);
				break;
			}
			case 0x02: //Set VKey
			{
				setVkeyState(param1, param2 != 0);
				break;
			}
			case 0x03: //Close named pipe
			{
				DEBUGLOG("Received pipe closed message. Closing pipe..." << "\n")
				pipe_closed = true;
				return;
			}
			case 0x05: //Focus desktop
			{
				//If the game brings itself to the foreground, it is the only window that can set something else as foreground (so it's required to do this in Hooks)
				SetForegroundWindow(GetDesktopWindow());
				break;
			}
			default:
			{
				break;
			}
			}
		}
		else
		{
			//cout << "Failed to read message\n";
		}
	}
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

	const size_t pathLength = (size_t)bytesToInt(_p);
	const size_t writePipeNameLength = (size_t)bytesToInt(_p + 4);
	const size_t readPipeNameLength = (size_t)bytesToInt(_p + 8);
	_p += 12;

	//C# gives number of bytes without null termination
	auto nucleusFolderPath = static_cast<PWSTR>(malloc(pathLength + sizeof(WCHAR)));
	memcpy(nucleusFolderPath, _p, pathLength);
	_p += pathLength;
	nucleusFolderPath[pathLength / sizeof(WCHAR)] = '\0';//Null-terminate the string
	nucleusFolder = nucleusFolderPath;

	_writePipeName = std::wstring(reinterpret_cast<wchar_t*>(_p), writePipeNameLength/2);
	_p += writePipeNameLength;

	_readPipeName = std::wstring(reinterpret_cast<wchar_t*>(_p), readPipeNameLength/2);
	_p += readPipeNameLength;

	DEBUGLOG("Starting hook injection," <<
		" SetWindow: " << SetWindow << 
		" HookFocus: " << HookFocus << 
		" HideCursor: " << HideCursor << 
		" PreventWindowDeactivation: " << PreventWindowDeactivation <<
		" WritePipeName: " << std::string(_writePipeName.begin(), _writePipeName.end()) <<
		" ReadPipeName: " << std::string(_readPipeName.begin(), _readPipeName.end()) <<
		"\n");

	if (SetWindow)
		install_set_window_hook();

	if (HookFocus)
		install_focus_hooks();

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

	if (hookGetCursorPos)
		install_get_cursor_pos_hook();

	if (hookSetCursorPos)
		install_set_cursor_pos_hook();

	if (hookGetAsyncKeyState)
		install_get_async_key_state_hook();

	if (hookGetKeyState)
		install_get_key_state_hook();

	if (hookGetKeyboardState)
		install_get_keyboard_state_hook();

	DEBUGLOG("Hook injection complete\n");

	startPipeListen();

	return;
}