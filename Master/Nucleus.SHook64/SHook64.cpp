#include "pch.h"
#include "easyhook.h"
#include "framework.h"
//#include "string"
#include "windows.h"
//#include <sstream>
//#include <ios>
#include <fstream>
#include <atlbase.h>
#include <random>
#include <string>
#include <map>
#include <iostream>
using namespace std;

HWND hWnd = 0;


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
	USES_CONVERSION;
	LPCWSTR moduleHandlew = A2W(moduleHandle);

	std::ofstream outfile;
	outfile.open("error-log.txt", std::ios_base::app);

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
		outfile << "error installing hook: " << proc << ", error msg: " << (LPCWSTR)RtlGetLastErrorString() << "\n";
	}
	else
	{
		// If the threadId in the ACL is set to 0,
		// then internally EasyHook uses GetCurrentThreadId()
		ULONG ACLEntries[1] = { 0 };

		// Disable the hook for the provided threadIds, enable for all others
		LhSetExclusiveACL(ACLEntries, 1, &hHook);

		//outfile << "hWnd: " << (INT)hWnd << ", hook: " << proc << ", in module: " << moduleHandle << ", result: " << result << "\n";
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
		std::wstring splitter = L";";
		unsigned int startIndex = 0;
		unsigned int endIndex = 0;

		while ((endIndex = target_s.find(splitter, startIndex)) < target_s.size())
		{
			std::wstring sub = target_s.substr(startIndex, endIndex - startIndex);
			ADD_SEARCH_TERM(sub);
			startIndex = endIndex + splitter.size();
		}

		if (startIndex < target_s.size())
		{
			//No splitters in string
			std::wstring sub = target_s.substr(startIndex);
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

	if (SetWindow)
	{
		HookInstall("user32", "SetWindowPos", SetWindowPos_Hook);
		if (!HookWindow && !RenameMutex)
		{
			RhWakeUpProcess();
		}
	}

	if (HookWindow)
	{
		HookInstall("user32", "FindWindowA", FindWindow_Hook);
		HookInstall("user32", "FindWindowW", FindWindow_Hook);
		HookInstall("user32", "FindWindowExA", FindWindowEx_Hook);
		HookInstall("user32", "FindWindowExW", FindWindowEx_Hook);
		HookInstall("user32", "EnumWindows", EnumWindows_Hook);
		if (!RenameMutex)
		{
			RhWakeUpProcess();
		}
	}

	if (RenameMutex)
	{
		const size_t targetsLength = (data[3] << 24) + (data[4] << 16) + (data[5] << 8) + data[6];
		auto targets = static_cast<PWSTR>(malloc(targetsLength + sizeof(WCHAR)));
		memcpy(targets, &data[7], targetsLength);
		targets[targetsLength / sizeof(WCHAR)] = '\0';
		installFindMutexHooks(targets);
	}

	return;
}