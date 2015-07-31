#pragma once

#include "pal.h"

PAL_BEGIN_EXTERNC

BOOL GetUserName(WCHAR_T* userName, UINT32* maxLength);

PAL_END_EXTERNC

