#pragma once

#include <fstream>

extern std::ofstream loggingOutfile;
extern std::wstring nucleusFolder;
const std::wstring LOG_FILE = L"\\debug-log-hooks.txt";//Use different path to C# side or it can crash if writing at the same time

#ifdef _DEBUG
#define DEBUGLOG(x) \
{loggingOutfile.open(nucleusFolder + LOG_FILE, std::ios_base::app); \
loggingOutfile << dateString() << " " << x; \
loggingOutfile.flush(); \
loggingOutfile.close();}
#else
#define DEBUGLOG(x) ;
#endif

std::string dateString();