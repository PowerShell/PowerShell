#pragma once

#include "pal.h"

PAL_BEGIN_EXTERNC

BOOL GetComputerNameW(WCHAR_T* lpBuffer, LPDWORD lpnSize);

PAL_END_EXTERNC