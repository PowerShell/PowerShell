//! @file getcomputername.cpp
//! @author George Fleming <v-geflem@microsoft>
//! @brief Implements GetComputerName Win32 API

#include "getcomputername.h"

#include <errno.h>
#include <unistd.h>
#include <string>

//! @brief GetComputerName retrieves the name of the host associated with
//! the current thread.
//!
//! @retval username as UTF-8 string, or null if unsuccessful

char* GetComputerName()
{
    errno = 0;
    // Get computername from system, note that gethostname(2) gets the
    // nodename from uname
    std::string computername(_POSIX_HOST_NAME_MAX, 0);
    int32_t ret = gethostname(&computername[0], computername.length());
    // Map errno to Win32 Error Codes
    if (ret != 0)
    {
        return NULL;
    }

    return strdup(computername.c_str());
}
