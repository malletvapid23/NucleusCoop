#include "pch.h"
#include "Logging.h"
#include "FakeMouse.h"
#include "KeyStates.h"

namespace Piping
{
	std::wstring writePipeName;
	std::wstring readPipeName;

	HANDLE hPipeRead;
	HANDLE hPipeWrite;
	bool pipe_closed = false;

	void startPipeListen()
	{
		//Read pipe
		char _pipeNameChars[256];
		sprintf_s(_pipeNameChars, R"(\\.\pipe\%s)", std::string(readPipeName.begin(), readPipeName.end()).c_str());

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
		sprintf_s(_pipeNameCharsWrite, "\\\\.\\pipe\\%s", std::string(writePipeName.begin(), writePipeName.end()).c_str());

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
				//TODO: needs to quit if failed too many times (in a row)
				//cout << "Failed to read message\n";
			}
		}
	}
}