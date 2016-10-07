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
//! @retval 0 successfully set date
//! @retval -1 if failure occurred.
//!
int32_t SetDate(struct tm* time)
{
    errno = 0;

    // Select locale from environment
    setlocale(LC_ALL, "");

    struct timeval tv;

    time_t newTime = mktime(time);
    if (newTime == -1)
    {
        return -1;
    }

    tv.tv_sec = newTime;
    tv.tv_usec = 0;

    return settimeofday(&tv, NULL);
}
