//! @file createsymlink.cpp
//! @author George FLeming <v-geflem@microsoft.com>
//! @brief create new symbolic link

#include "createsymlink.h"

#include <errno.h>
#include <unistd.h>
#include <string>

//! @brief Createsymlink create new symbolic link
//!
//! Createsymlink
//!
//! @param[in] link
//! @parblock
//! A pointer to the buffer that contains the symbolic link to create
//!
//! char* is marshaled as an LPStr, which on Linux is UTF-8.
//! @endparblock
//!
//! @param[in] target
//! @parblock
//! A pointer to the buffer that contains the existing file
//!
//! char* is marshaled as an LPStr, which on Linux is UTF-8.
//! @endparblock
//!
//! @exception errno Passes these errors via errno to GetLastError:
//! - ERROR_INVALID_PARAMETER: parameter is not valid
//! - ERROR_FILE_NOT_FOUND: file does not exist
//! - ERROR_ACCESS_DENIED: access is denied
//! - ERROR_FILE_NOT_FOUND: the system cannot find the file specified
//! - ERROR_INVALID_ADDRESS: attempt to access invalid address
//! - ERROR_STOPPED_ON_SYMLINK: the operation stopped after reaching a symbolic link
//! - ERROR_GEN_FAILURE: device attached to the system is not functioning
//! - ERROR_NO_SUCH_USER: there was no corresponding entry in the utmp-file
//! - ERROR_INVALID_NAME: filename, directory name, or volume label syntax is incorrect
//! - ERROR_BUFFER_OVERFLOW:  file name is too long
//! - ERROR_INVALID_FUNCTION: incorrect function
//! - ERROR_BAD_PATH_NAME: pathname is too long, or contains invalid characters
//!
//! @retval boolean successful
//!

bool CreateSymLink(const char *link, const char *target)
{
    errno = 0;

    // Check parameters
    if (!link || !target)
    {
        errno = ERROR_INVALID_PARAMETER;
        return false;
    }

    int ret = symlink(target, link);

    if (ret == 0)
    {
        return true;
    }

    switch(errno)
    {
        case EACCES:
            errno = ERROR_ACCESS_DENIED;
            break;
        case EDQUOT:
            errno = ERROR_DISK_FULL;
            break;
        case EEXIST:
            errno = ERROR_FILE_EXISTS;
            break;
        case EFAULT:
            errno = ERROR_INVALID_ADDRESS;
            break;
        case EIO:
            errno = ERROR_GEN_FAILURE;
            break;
        case ELOOP:
            errno = ERROR_STOPPED_ON_SYMLINK;
            break;
        case ENAMETOOLONG:
            errno = ERROR_BAD_PATH_NAME;
            break;
        case ENOENT:
            errno = ERROR_FILE_NOT_FOUND;
            break;
        case ENOMEM:
            errno = ERROR_OUTOFMEMORY;
            break;
        case ENOTDIR:
            errno = ERROR_INVALID_NAME;
            break;
        case ENOSPC:
            errno = ERROR_DISK_FULL;
            break;
        case EPERM:
            errno = ERROR_GEN_FAILURE;
            break;
        default:
            errno = ERROR_INVALID_FUNCTION;
    }

    return false;
}
