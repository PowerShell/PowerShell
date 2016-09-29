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
//! @exception errno Passes these errors via errno to GetLastError:
//! - ERROR_INVALID_FUNCTION: getlogin_r() returned an unrecognized error code
//! - ERROR_INVALID_ADDRESS: buffer is an invalid address
//! - ERROR_GEN_FAILURE: buffer not large enough
//!
//! @retval username as UTF-8 string, or null if unsuccessful

char* GetComputerName()
{
    errno = 0; 
    // Get computername from system, note that gethostname(2) gets the
    // nodename from uname
    std::string computername(_POSIX_HOST_NAME_MAX, 0);
    int err = gethostname(&computername[0], computername.length());
    // Map errno to Win32 Error Codes
    if (err != 0) 
    {
        switch (errno) 
        {
            case EFAULT:
                errno = ERROR_INVALID_ADDRESS;
                break;
            case ENAMETOOLONG:
                errno = ERROR_GEN_FAILURE;
                break; 
            default:
                errno = ERROR_INVALID_FUNCTION;
        }
        return NULL;
    }
    
    return strdup(computername.c_str());
}
