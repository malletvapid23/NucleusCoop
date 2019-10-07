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
#include <random>
#include <string>
#include <map>
#include <iostream>
//#include <vector>
#include <locale>
#include <codecvt>
#include <time.h>
#include <stdio.h>
#include <Xinput.h>
using namespace std;

#pragma comment(lib, "XInput.lib")

HWND hWnd = 0;

bool IsDebug = false;

std::ofstream outfile;
std::wstring nucleusFolder;
std::wstring logFile = L"\\debug-log.txt";

std::mt19937 randomGenerator;

//Key: search term. Value: the assigned name that is replaced for every name that matched the search term. (value is empty if needs generating)
std::map <std::wstring, std::wstring> searchTermsToAssignedNames;

typedef enum _EVENT_TYPE {
	NotificationEvent,
	SynchronizationEvent
} EVENT_TYPE, * PEVENT_TYPE;

typedef NTSTATUS(NTAPI* t_NtCreateMutant)(PHANDLE MutantHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes, BOOLEAN InitialOwner);
typedef NTSTATUS(NTAPI* t_NtOpenMutant)(PHANDLE MutantHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes);

typedef NTSTATUS(NTAPI* t_NtCreateEvent)(PHANDLE EventHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes, EVENT_TYPE EventType, BOOLEAN InitialState);
typedef NTSTATUS(NTAPI* t_NtOpenEvent)(PHANDLE EventHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes);

typedef NTSTATUS(NTAPI* t_NtCreateSemaphore)(PHANDLE SemaphoreHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes, ULONG InitialCount, ULONG MaximumCount);
typedef NTSTATUS(NTAPI* t_NtOpenSemaphore)(PHANDLE SemaphoreHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes);

static t_NtCreateMutant NtCreateMutant;
static t_NtOpenMutant NtOpenMutant;

static t_NtCreateEvent NtCreateEvent;
static t_NtOpenEvent NtOpenEvent;

static t_NtCreateSemaphore NtCreateSemaphore;
static t_NtOpenSemaphore NtOpenSemaphore;

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

inline UNICODE_STRING stdWStringToUnicodeString(const std::wstring& str) {
	UNICODE_STRING unicodeString;
	DWORD len = 0;

	len = str.length();
	LPWSTR cstr = new WCHAR[len + 1];
	memcpy(cstr, str.c_str(), (len + 1) * sizeof(WCHAR));
	unicodeString.Buffer = cstr;
	unicodeString.Length = (USHORT)(len * sizeof(WCHAR));
	unicodeString.MaximumLength = (USHORT)((len + 1) * sizeof(WCHAR));
	return unicodeString;
}

void updateName(PUNICODE_STRING inputName)
{
	if (!(inputName->Length > 0 && inputName->Length <= inputName->MaximumLength)) return;

	for (std::map<std::wstring, std::wstring>::value_type& pair : searchTermsToAssignedNames)
	{
		if (wcsstr(inputName->Buffer, pair.first.c_str()) != nullptr)
		{
			if (pair.second.empty())
			{
				const auto rand = std::to_wstring(randomGenerator());

				const std::wstring oldName = inputName->Buffer;
				const auto newName = oldName + rand;
				if (IsDebug)
				{
					outfile.open(nucleusFolder + logFile, std::ios_base::app);
					outfile << date_string() << "SHOOK32: Mutex function called, renaming mutex " << ws2s(oldName) << " to " << ws2s(newName) << "\n";
					outfile.close();
				}
				pair.second = newName;
			}

			*inputName = stdWStringToUnicodeString(pair.second);
		}
	}
}

inline void updateNameObject(POBJECT_ATTRIBUTES ObjectAttributes)
{
	if (ObjectAttributes != NULL && ObjectAttributes->ObjectName != NULL)
	{
		updateName(ObjectAttributes->ObjectName);
	}
}

UINT WINAPI GetRawInputDeviceList_Hook(PRAWINPUTDEVICELIST pRawInputDeviceList, PUINT puiNumDevices, UINT cbSize)
{
	*puiNumDevices = 0;

	return 0; //GetRawInputDeviceList(pRawInputDeviceList, puiNumDevices, cbSize);
}

UINT WINAPI GetRegisteredRawInputDevices_Hook(PRAWINPUTDEVICE pRawInputDevices, PUINT puiNumDevices, UINT cbSize)
{
	*puiNumDevices = 0;

	return 0; // GetRegisteredRawInputDevices(pRawInputDevices, puiNumDevices, cbSize);
}

BOOL WINAPI SetWindowPos_Hook(HWND hWnd, HWND hWndInsertAfter, int X, int Y, int cx, int cy, UINT uFlags)
{
	return true;
}

NTSTATUS NTAPI NtCreateMutant_Hook(OUT PHANDLE MutantHandle, IN ULONG DesiredAccess, IN POBJECT_ATTRIBUTES ObjectAttributes OPTIONAL, IN BOOLEAN InitialOwner)
{
	updateNameObject(ObjectAttributes);
	return NtCreateMutant(MutantHandle, DesiredAccess, ObjectAttributes, InitialOwner);
}

NTSTATUS NTAPI NtOpenMutant_Hook(PHANDLE MutantHandle, ULONG DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes)
{
	updateNameObject(ObjectAttributes);
	return NtOpenMutant(MutantHandle, DesiredAccess, ObjectAttributes);
}

NTSTATUS NTAPI NtCreateEvent_Hook(PHANDLE EventHandle, DWORD DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes, EVENT_TYPE EventType, BOOLEAN InitialState)
{
	updateNameObject(ObjectAttributes);
	return NtCreateEvent(EventHandle, DesiredAccess, ObjectAttributes, EventType, InitialState);
}

NTSTATUS NTAPI NtOpenEvent_Hook(PHANDLE EventHandle, DWORD DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes)
{
	updateNameObject(ObjectAttributes);
	return NtOpenEvent(EventHandle, DesiredAccess, ObjectAttributes);
}

NTSTATUS NTAPI NtCreateSemaphore_Hook(PHANDLE SemaphoreHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes, ULONG InitialCount, ULONG MaximumCounts)
{
	updateNameObject(ObjectAttributes);
	return NtCreateSemaphore(SemaphoreHandle, DesiredAccess, ObjectAttributes, InitialCount, MaximumCounts);
}

NTSTATUS NTAPI NtOpenSemaphore_Hook(PHANDLE SemaphoreHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes)
{
	updateNameObject(ObjectAttributes);
	return NtOpenSemaphore(SemaphoreHandle, DesiredAccess, ObjectAttributes);
}

HWND WINAPI FindWindow_Hook(LPCSTR lpClassName, LPCSTR lpWindowName)
{
	return NULL;
}

HWND WINAPI FindWindowEx_Hook(LPCSTR lpClassName, LPCSTR lpWindowName)
{
	return NULL;
}

BOOL WINAPI EnumWindows_Hook(WNDENUMPROC lpEnumFunc, LPARAM lParam)
{
	return TRUE;
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
			outfile << date_string() << "SHOOK32: Error installing " << proc << " hook, error msg: " << RtlGetLastErrorString() << "\n";
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
			outfile << date_string() << "SHOOK32: Successfully installed " << proc << " hook, in module: " << moduleHandle << ", result: " << result << "\n";
		}
	}
	outfile.close();

	return result;
}

void installFindMutexHooks(LPCWSTR targets)
{
	//Random
	std::random_device rd;
	randomGenerator = static_cast<std::mt19937>(rd());


	//Search terms
#define ADD_SEARCH_TERM(term) searchTermsToAssignedNames.insert(std::make_pair((term), L"")); std::wcout << L"Added search term: " << sub << std::endl;

	{
		std::wstring target_s(targets);
		std::wstring splitter = L"|==|";
		unsigned int startIndex = 0;
		unsigned int endIndex = 0;

		while ((endIndex = target_s.find(splitter, startIndex)) < target_s.size())
		{
			std::wstring sub = target_s.substr(startIndex, endIndex - startIndex);
			std::string s(sub.begin(), sub.end());
			if (IsDebug)
			{
				outfile.open(nucleusFolder + logFile, std::ios_base::app);
				outfile << date_string() << "SHOOK32: Rename mutex, adding search term: " << ws2s(sub) << "\n";
				outfile.close();
			}
			ADD_SEARCH_TERM(sub);
			startIndex = endIndex + splitter.size();
		}

		if (startIndex < target_s.size())
		{
			//No splitters in string
			std::wstring sub = target_s.substr(startIndex);
			if (IsDebug)
			{
				outfile.open(nucleusFolder + logFile, std::ios_base::app);
				outfile << date_string() << "SHOOK32: Rename mutex, adding search term: " << ws2s(sub) << "\n";
				outfile.close();
			}
			ADD_SEARCH_TERM(sub);
		}
	}

#undef ADD_SEARCH_TERM


	//Ntdll functions
#define GET_NT_PROC(name, type) (type)GetProcAddress(GetModuleHandle("ntdll.dll"), name)

	NtCreateMutant = GET_NT_PROC("NtCreateMutant", t_NtCreateMutant);
	NtOpenMutant = GET_NT_PROC("NtOpenMutant", t_NtOpenMutant);

	NtCreateEvent = GET_NT_PROC("NtCreateEvent", t_NtCreateEvent);
	NtOpenEvent = GET_NT_PROC("NtOpenEvent", t_NtOpenEvent);

	NtCreateSemaphore = GET_NT_PROC("NtCreateSemaphore", t_NtCreateSemaphore);
	NtOpenSemaphore = GET_NT_PROC("NtOpenSemaphore", t_NtOpenSemaphore);

#undef GET_NT_PROC

	//Hooks
	HookInstall("ntdll.dll", "NtCreateMutant", NtCreateMutant_Hook);
	HookInstall("ntdll.dll", "NtOpenMutant", NtOpenMutant_Hook);

	HookInstall("ntdll.dll", "NtCreateEvent", NtCreateEvent_Hook);
	HookInstall("ntdll.dll", "NtOpenEvent", NtOpenEvent_Hook);

	HookInstall("ntdll.dll", "NtCreateSemaphore", NtCreateSemaphore_Hook);
	HookInstall("ntdll.dll", "NtOpenSemaphore", NtOpenSemaphore_Hook);

	if (IsDebug)
	{
		outfile.open(nucleusFolder + logFile, std::ios_base::app);
		outfile << date_string() << "SHOOK32: Hook injection complete\n";
		outfile.close();
	}
	RhWakeUpProcess();
}

// EasyHook will be looking for this export to support DLL injection. If not found then 
// DLL injection will fail.
extern "C" void __declspec(dllexport) __stdcall NativeInjectionEntryPoint(REMOTE_ENTRY_INFO* inRemoteInfo);

void __stdcall NativeInjectionEntryPoint(REMOTE_ENTRY_INFO* inRemoteInfo)
{
	DWORD pid = GetCurrentProcessId();
	hWnd = FindWindowFromProcessId(pid);

	BYTE* data = inRemoteInfo->UserData;
	const bool HookWindow = data[0] == 1;
	const bool RenameMutex = data[1] == 1;
	const bool SetWindow = data[2] == 1;
	IsDebug = data[3] == 1;
	const bool BlockRaw = data[4] == 1;

	const size_t pathLength = (data[10] << 24) + (data[11] << 16) + (data[12] << 8) + data[13];
	auto nucleusFolderPath = static_cast<PWSTR>(malloc(pathLength + sizeof(WCHAR)));
	memcpy(nucleusFolderPath, &data[18], pathLength);
	nucleusFolderPath[pathLength / sizeof(WCHAR)] = '\0';

	nucleusFolder = nucleusFolderPath;

	if (IsDebug)
	{
		outfile.open(nucleusFolder + logFile, std::ios_base::app);
		outfile << date_string() << "SHOOK32: Starting hook injection, HookWindow: " << HookWindow << " RenameMutex: " << RenameMutex << " SetWindow: " << SetWindow << " BlockRaw: " << BlockRaw << "\n";
		outfile.close();
	}

	if (SetWindow)
	{
		if (IsDebug)
		{
			outfile.open(nucleusFolder + logFile, std::ios_base::app);
			outfile << date_string() << "SHOOK32: Injecting SetWindow hook\n";
			outfile.close();
		}
		HookInstall("user32", "SetWindowPos", SetWindowPos_Hook);
		if (!HookWindow && !RenameMutex && !BlockRaw)
		{
			if (IsDebug)
			{
				outfile.open(nucleusFolder + logFile, std::ios_base::app);
				outfile << date_string() << "SHOOK32: Hook injection complete\n";
				outfile.close();
			}
			RhWakeUpProcess();
		}
	}

	if (HookWindow)
	{
		if (IsDebug)
		{
			outfile.open(nucleusFolder + logFile, std::ios_base::app);
			outfile << date_string() << "SHOOK32: Injecting HookWindow hooks\n";
			outfile.close();
		}
		HookInstall("user32", "FindWindowA", FindWindow_Hook);
		HookInstall("user32", "FindWindowW", FindWindow_Hook);
		HookInstall("user32", "FindWindowExA", FindWindowEx_Hook);
		HookInstall("user32", "FindWindowExW", FindWindowEx_Hook);
		HookInstall("user32", "EnumWindows", EnumWindows_Hook);
		if (!RenameMutex && !BlockRaw)
		{
			if (IsDebug)
			{
				outfile.open(nucleusFolder + logFile, std::ios_base::app);
				outfile << date_string() << "SHOOK32: Hook injection complete\n";
				outfile.close();
			}
			RhWakeUpProcess();
		}
	}

	if (BlockRaw)
	{
		//char mbstr[256];
		//std::wcstombs(mbstr, rawHid.c_str(), 256);

		if (IsDebug)
		{
			outfile.open(nucleusFolder + logFile, std::ios_base::app);
			outfile << date_string() << "SHOOK32: Injecting BlockRaw hooks\n";
			outfile.close();
		}

		/*RAWINPUTDEVICE rawInputDevice[1];
		rawInputDevice[0].usUsagePage = 1;
		rawInputDevice[0].usUsage = 5;
		rawInputDevice[0].dwFlags = 0;
		rawInputDevice[0].hwndTarget = hWnd;

		BOOL result = RegisterRawInputDevices(rawInputDevice, 1, sizeof(rawInputDevice[0]));

		if (!result)
		{
			if (IsDebug)
			{
				outfile.open(nucleusFolder + logFile, std::ios_base::app);
				outfile << date_string() << "block raw RegisterRawInputDevices Error: " << GetLastError() << "\n";
				outfile.close();
			}
		}*/

		//HookInstall("user32", "GetMessageA", GetMessageA_Hook);
		//HookInstall("user32", "GetMessageW", GetMessageW_Hook);
		//HookInstall("user32", "PeekMessageA", PeekMessageA_Hook);
		//HookInstall("user32", "PeekMessageW", PeekMessageW_Hook);

		HookInstall("user32", "GetRawInputDeviceList", GetRawInputDeviceList_Hook);
		HookInstall("user32", "GetRegisteredRawInputDevices", GetRegisteredRawInputDevices_Hook);
		if (!RenameMutex)
		{
			if (IsDebug)
			{
				outfile.open(nucleusFolder + logFile, std::ios_base::app);
				outfile << date_string() << "SHOOK32: Hook injection complete\n";
				outfile.close();
			}
			RhWakeUpProcess();
		}
	}

	if (RenameMutex)
	{
		if (IsDebug)
		{
			outfile.open(nucleusFolder + logFile, std::ios_base::app);
			outfile << date_string() << "SHOOK32: Injecting RenameMutex hooks\n";
			outfile.close();
		}
		const size_t targetsLength = (data[14] << 24) + (data[15] << 16) + (data[16] << 8) + data[17];
		auto targets = static_cast<PWSTR>(malloc(targetsLength + sizeof(WCHAR)));
		memcpy(targets, &data[19 + pathLength], targetsLength);
		targets[targetsLength / sizeof(WCHAR)] = '\0';
		installFindMutexHooks(targets);
	}

	

	return;
}