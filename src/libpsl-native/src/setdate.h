// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#pragma once

#include "pal.h"

#include <time.h>

PAL_BEGIN_EXTERNC

int32_t SetDate(struct private_tm* time);

static int32_t GetTimeVal(struct private_tm& time, struct timeval& tv);

PAL_END_EXTERNC

// Using a private struct because theuse externally defined structs
// in managed code has proven to be buggy
// (memory corruption issues due to layout difference between platforms)
// see https://github.com/dotnet/corefx/issues/29700#issuecomment-389313075
#pragma pack(push, 4) // exact fit - no padding
struct private_tm
{
    int32_t Seconds;   /* Seconds (0-60) */
    int32_t Minutes;   /* Minutes (0-59) */
    int32_t Hour;      /* Hours (0-23) */
    int32_t DayOfMonth;/* Day of the month (1-31) */
    int32_t Month;     /* Month (0-11) */
    int32_t Year;      /* Year - 1900 */
    int32_t DayOfWeek; /* Day of the week (0-6, Sunday = 0) */
    int32_t DayInYear; /* Day in the year (0-365, 1 Jan = 0) */
    int32_t IsDst;     /* Daylight saving time */
};
#pragma pack(pop) //back to whatever the previous packing mode was
