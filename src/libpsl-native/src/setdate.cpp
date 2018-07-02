// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//! @brief set local/system date and time

#include "setdate.h"

#include <assert.h>
#include <errno.h>
#include <langinfo.h>
#include <locale.h>
#include <string>
#include <time.h>
#include <sys/time.h>

//! @brief SetDate sets the date and time on local computer.
//!      You must be super-user to set the time.
//!      See comment in setdate.h about the use of private_tm
//!
//! SetDate
//!
//! @retval 0 successfully set date
//! @retval -1 if failure occurred.
//!
int32_t SetDate(struct private_tm* time)
{
    errno = 0;

    // Select locale from environment
    setlocale(LC_ALL, "");

    struct timeval tv;
    int32_t result = GetTimeVal(*time,tv);
    if(result != 0)
    {
        return result;
    }

    return settimeofday(&tv, NULL);
}

static int32_t GetTimeVal(struct private_tm& time, struct timeval& tv)
{
    struct tm nativeTime = {0};
    nativeTime.tm_hour  = static_cast<int>(time.Hour);
    nativeTime.tm_isdst = static_cast<int>(time.IsDst);
    nativeTime.tm_mday  = static_cast<int>(time.DayOfMonth);
    nativeTime.tm_min   = static_cast<int>(time.Minutes);
    nativeTime.tm_mon   = static_cast<int>(time.Month);
    nativeTime.tm_sec   = static_cast<int>(time.Seconds);
    nativeTime.tm_wday  = static_cast<int>(time.DayOfWeek);
    nativeTime.tm_yday  = static_cast<int>(time.DayInYear);
    nativeTime.tm_year  = static_cast<int>(time.Year);

    time_t newTime = mktime(&nativeTime);
    if (newTime == -1)
    {
        return -1;
    }

    tv.tv_sec = newTime;
    tv.tv_usec = 0;

    return 0;
}
