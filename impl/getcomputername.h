#pragma once

#include "pal.h"

PAL_BEGIN_EXTERNC

BOOL GetComputerNameW(LPTSTR nameOfComputer, LPDWORD len);

PAL_END_EXTERNC