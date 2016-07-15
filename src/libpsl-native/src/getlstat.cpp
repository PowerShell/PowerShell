//! @file getlstat.cpp
//! @author Andrew Schwartzmeyer <andschwa@microsoft.com>
//! @brief returns the lstat of a file

#include <errno.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <pwd.h>
#include <string.h>
#include <unistd.h>
#include "getlstat.h"

//! @brief GetLStat returns the lstat of a file. This simply delegates to the
//! lstat() system call and maps errno to the expected values for GetLastError.
//!
//! GetLstat
//!
//! @param[in] path
//! @parblock
//! A pointer to the buffer that contains the file name
//!
//! char* is marshaled as an LPStr, which on Linux is UTF-8.
//! @endparblock
//!
//! @param[in] lstat
//! @parblock
//! A pointer to the buffer in which to place the lstat information
//! @endparblock
//!
//! @exception errno Passes these errors via errno to GetLastError:
//! - ERROR_INVALID_PARAMETER: parameter is not valid
//! - ERROR_FILE_NOT_FOUND: file does not exist
//! - ERROR_ACCESS_DENIED: access is denied
//! - ERROR_INVALID_ADDRESS: attempt to access invalid address
//! - ERROR_STOPPED_ON_SYMLINK: too many symbolic links
//! - ERROR_GEN_FAILURE: I/O error occurred
//! - ERROR_INVALID_NAME: file provided is not a symbolic link
//! - ERROR_INVALID_FUNCTION: incorrect function
//! - ERROR_BAD_PATH_NAME: pathname is too long
//! - ERROR_OUTOFMEMORY insufficient kernel memory
//!
//! @retval 0 if successful
//! @retval -1 if failed
//!

int32_t GetLStat(const char* path, struct stat* buf)
{
    errno = 0;

    if (!path)
    {
        errno = ERROR_INVALID_PARAMETER;
        return -1;
    }

    int32_t ret = lstat(path, buf);

    if (ret != 0)
    {
        switch(errno)
        {
        case EACCES:
            errno = ERROR_ACCESS_DENIED;
            break;
        case EBADF:
            errno = ERROR_FILE_NOT_FOUND;
            break;
        case EFAULT:
            errno = ERROR_INVALID_ADDRESS;
            break;
        case ELOOP:
            errno = ERROR_STOPPED_ON_SYMLINK;
            break;
        case ENAMETOOLONG:
            errno = ERROR_GEN_FAILURE;
            break;
        case ENOENT:
            errno = ERROR_FILE_NOT_FOUND;
            break;
        case ENOMEM:
            errno = ERROR_NO_SUCH_USER;
            break;
        case ENOTDIR:
            errno = ERROR_INVALID_NAME;
            break;
        case EOVERFLOW:
            errno = ERROR_BUFFER_OVERFLOW;
            break;
        default:
            errno = ERROR_INVALID_FUNCTION;
        }
    }

    return ret;
}
