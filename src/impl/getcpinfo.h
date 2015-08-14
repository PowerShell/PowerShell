#pragma once

#include "pal.h"

PAL_BEGIN_EXTERNC

BOOL GetCPInfoW(UINT codepage, CPINFO* cpinfo);

PAL_END_EXTERNC
