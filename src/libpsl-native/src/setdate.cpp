//! @file setdate.cpp
//! @author George FLeming <v-geflem@microsoft.com>
//! @brief set local/system date and time

#include "setdate.h"

#include <errno.h>
#include <langinfo.h>
#include <locale.h>
#include <string>
#include <time.h>
#include <sys/time.h>

//! @brief SetDate sets the date and time on local computer.  You must 
//!      be super-user to set the time.
//!
//! SetDate
//!
//! @param[in] info
//! @parblock
//! A struct that contains program to execute and its parameters
//!
//! @exception errno Passes these errors via errno to GetLastError:
//! - ERROR_BAD_ENVIRONMENT: locale is not UTF-8
//! - ERROR_INVALID_PARAMETER:  time was not passed in correctly
//! - ERROR_ACCESS_DENIED:  you must be super-user to set the date
//!
//! @retval 0 successfully set date
//! @retval -1 if failure occurred.  To get extended error information, call GetLastError.
//!

int32_t SetDate(const SetDateInfo &info)
{
    errno = 0;

    // Select locale from environment
    setlocale(LC_ALL, "");
    // Check that locale is UTF-8
    if (nl_langinfo(CODESET) != std::string("UTF-8"))
    {
        errno = ERROR_BAD_ENVIRONMENT;
        return -1;
    }

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
        errno = ERROR_INVALID_PARAMETER;
        return -1;
    }

    tv.tv_sec = newTime;
    tv.tv_usec = 0;

    int result = settimeofday(&tv, NULL);
    if (result == -1)
    {
        errno = ERROR_ACCESS_DENIED;
        return -1;
    }

    return 0;
}
