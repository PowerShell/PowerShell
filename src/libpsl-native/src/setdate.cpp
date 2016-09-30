//! @file setdate.cpp
//! @author George FLeming <v-geflem@microsoft.com>
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
//!
//! SetDate
//!
//! @param[in] info
//! @parblock
//! A struct that contains program to execute and its parameters
//!
//! @retval 0 successfully set date
//! @retval -1 if failure occurred.
//!
int32_t SetDate(const SetDateInfo info)
{
    errno = 0;

    // Select locale from environment
    setlocale(LC_ALL, "");
    // Check that locale is UTF-8
    assert(nl_langinfo(CODESET) == std::string("UTF-8"));

    struct tm bdTime;
    struct timeval tv;

    bdTime.tm_year = info.Year - 1900;
    bdTime.tm_mon = info.Month - 1;  // This is zero-based
    bdTime.tm_mday = info.Day;
    bdTime.tm_hour = info.Hour;
    bdTime.tm_min = info.Minute;
    bdTime.tm_sec = info.Second;
    bdTime.tm_isdst = info.DST;

    time_t newTime = mktime(&bdTime);
    if (newTime == -1)
    {
        return -1;
    }

    tv.tv_sec = newTime;
    tv.tv_usec = 0;

    return settimeofday(&tv, NULL);
}
