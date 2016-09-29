//! @file isexecutable.cpp
//! @author George Fleming <v-geflem@microsoft.com>
//! @brief returns whether a file is executable

#include "isexecutable.h"

#include <errno.h>
#include <unistd.h>
#include <string>

//! @brief IsExecutable determines if path is executable
//!
//! IsExecutable
//!
//! @param[in] path
//! @parblock
//! A pointer to the buffer that contains the file name
//!
//! char* is marshaled as an LPStr, which on Linux is UTF-8.
//! @endparblock
//!
//! @exception errno Passes these errors via errno to GetLastError:
//! - ERROR_INVALID_ADDRESS: attempt to access invalid address
//!
//! @retval true if path is an executable, false otherwise
//!

bool IsExecutable(const char* path)
{
    errno = 0;

    // Check parameters
    if (!path)
    {
        errno = ERROR_INVALID_PARAMETER;
        return false;
    }

    return access(path, X_OK) != -1;
}
