#pragma once

#include <iomanip>
#include <fstream>

extern std::ofstream logging_outfile;
extern std::wstring nucleusFolder;
const std::wstring logFile = L"\\debug-log-hooks.txt";//Use different path to C# side or it can crash if writing at the same time

#ifdef _DEBUG
#define DEBUGLOG(x) \
{logging_outfile.open(nucleusFolder + logFile, std::ios_base::app); \
logging_outfile << date_string() << " " << x; \
logging_outfile.flush(); \
logging_outfile.close();}
#else
#define DEBUGLOG(x) ;
#endif

std::ofstream& get_outfile();

std::string date_string();