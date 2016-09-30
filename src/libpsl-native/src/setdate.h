#pragma once

#include "pal.h"

PAL_BEGIN_EXTERNC

typedef struct setDateInfo
{
    // the order of members does matter here
    int32_t Year;
    int32_t Month;
    int32_t Day;
    int32_t Hour;
    int32_t Minute;
    int32_t Second;
    int32_t Millisecond;
    int32_t DST;
} SetDateInfo;

int32_t SetDate(const SetDateInfo info);

PAL_END_EXTERNC
