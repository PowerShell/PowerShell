//! @file isfile.cpp
//! @author Andrew Schwartzmeyer <andschwa@microsoft.com>
//! @brief returns if the path exists

#include <errno.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <pwd.h>
#include <string.h>
#include <unistd.h>
#include "isfile.h"

//! @brief returns if the path is a file or directory
//!
//! IsFile
//!
//! @param[in] path
//! @parblock
//! A pointer to the buffer that contains the file name
//!
//! char* is marshaled as an LPStr, which on Linux is UTF-8.
//! @endparblock
//!
//! @exception errno Passes this error via errno to GetLastError:
//! - ERROR_INVALID_PARAMETER: parameter is not valid
//!
//! @retval true if path exists, false otherwise
//!
bool IsFile(const char* path)
{
    errno = 0;

    if (!path)
    {
        errno = ERROR_INVALID_PARAMETER;
        return false;
    }

    return access(path, F_OK) != -1;
}
