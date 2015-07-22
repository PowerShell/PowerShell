#pragma once

#include "pal.h"

PAL_BEGIN_EXTERNC

// WCHAR_T * is a Unicode LPTSTR
// See https://msdn.microsoft.com/en-us/library/windows/desktop/aa383751(v=vs.85).aspx#LPTSTR
BOOL GetUserName(WCHAR_T *lpBuffer, LPDWORD lpnSize);

PAL_END_EXTERNC
