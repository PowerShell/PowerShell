#pragma once

#include "pal.h"

//!* NameSpace

namespace const_cpinfo
{
    constexpr int MAX_DEFAULTCHAR = 2;
    constexpr int MAX_LEADBYTES = 12;
    constexpr int UTF8 = 65001;
}

//!* Structs

typedef struct _cpinfo
{
    UINT MaxCharSize;
    BYTE DefaultChar[const_cpinfo::MAX_DEFAULTCHAR];
    BYTE LeadByte[const_cpinfo::MAX_LEADBYTES];
} CPINFO;

PAL_BEGIN_EXTERNC

BOOL GetCPInfoW(UINT codepage, CPINFO* cpinfo);

PAL_END_EXTERNC
