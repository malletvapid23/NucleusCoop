#pragma once

#include <iomanip>
#include <fstream>

#ifdef _DEBUG
#define DEBUGLOG(x) \
{std::ofstream& __of = get_outfile(); \
__of << date_string() << "HOOK: " << x; \
__of.flush(); \
__of.close();}
#else
#define DEBUGLOG(x) ;
#endif

std::ofstream& get_outfile();

std::string date_string();