#pragma once
#include "pch.h"
#include <string>

namespace Piping
{
	extern std::wstring writePipeName;
	extern std::wstring readPipeName;

	extern HANDLE hPipeRead;
	extern HANDLE hPipeWrite;

	void startPipeListen();
}