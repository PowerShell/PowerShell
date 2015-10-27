//! @file getcomputername.cpp
//! @author Aaron Katz <v-aakatz@microsoft.com>
//! @brief Implements GetComputerName Win32 API

#include <errno.h>
#include <langinfo.h>
#include <locale.h>
#include <unistd.h>
#include <string>
#include <iostream>
#include "getcomputername.h"

//! @brief GetComputerName retrieves the name of the host associated with
//! the current thread.
//!
//! @exception errno Passes these errors via errno to GetLastError:
//! - ERROR_BAD_ENVIRONMENT: locale is not UTF-8
//! - ERROR_INVALID_FUNCTION: getlogin_r() returned an unrecognized error code
//! - ERROR_INVALID_ADDRESS:  buffer is an invalid address
//! - ERROR_GEN_FAILURE: buffer not large enough
//!
//! @retval username as UTF-8 string, or null if unsuccessful

char* GetComputerName()
{
    errno = 0;
    
    // Select locale from environment
    setlocale(LC_ALL, "");
    // Check that locale is UTF-8
    if (nl_langinfo(CODESET) != std::string("UTF-8"))
    {
        errno = ERROR_BAD_ENVIRONMENT;
        return FALSE;
    }
    
    // Get computername from system in a thread-safe manner
    std::string computername(HOST_NAME_MAX, 0);
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
        return FALSE;
    }
    
    return strdup(computername.c_str());
    
}

