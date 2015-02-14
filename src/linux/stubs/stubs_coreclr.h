#pragma once

#include "pal.h"

DWORD WINAPI ExpandEnvironmentStringsW(
  PCWSTR lpSrc,
  PWSTR lpDst,
  DWORD nSize
);

