#pragma once

#include "pal.h"

PAL_BEGIN_EXTERNC


 const int MAX_DEFAULTCHAR =   2;

 const int MAX_LEADBYTES   =  12;


typedef struct _cpinfo {
    unsigned int MaxCharSize;
    char DefaultChar[MAX_DEFAULTCHAR];
    char LeadByte[MAX_LEADBYTES];
} CPINFO, *LPCPINFO;


bool GetCPInfo(unsigned int codepage, LPCPINFO cpinfo);

PAL_END_EXTERNC
