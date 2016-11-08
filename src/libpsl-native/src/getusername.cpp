//! @file getusername.cpp
//! @author Andrew Schwartzmeyer <andschwa@microsoft.com>
//! @brief Implements GetUserName for Linux

#include "getpwuid.h"
#include "getusername.h"

#include <unistd.h>

//! @brief GetUserName retrieves the name of the user associated with
//! the current thread.
//!
//! @retval username as UTF-8 string, or null if unsuccessful
char* GetUserName()
{
    // geteuid() gets the effective user ID of the calling process, and is always successful
    return GetPwUid(geteuid());
}
