//! @file isSymLink.cpp
//! @author George FLeming <v-geflem@microsoft.com>
//! @brief returns whether a path is a symbolic link

#include <errno.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <unistd.h>
#include <string>
#include "issymlink.h"

//! @brief IsSymlink determines if path is a symbolic link
//!
//! IsSymlink
//!
//! @param[in] path
//! @parblock
//! A pointer to the buffer that contains the file name
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
//! @retval true if path is a symbolic link, false otherwise
//!

bool IsSymLink(const char* path)
{
    errno = 0;

    // Check parameters
    if (!path)
    {
        errno = ERROR_INVALID_PARAMETER;
        return false;
    }

    struct stat statBuf;

    int ret = lstat(path, &statBuf);

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
        return false;
    }

    return S_ISLNK(statBuf.st_mode);
}
