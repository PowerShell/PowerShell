//! @file createsymlink.cpp
//! @author George FLeming <v-geflem@microsoft.com>
//! @brief create new hard link

#include <errno.h>
#include <unistd.h>
#include <string>
#include "createhardlink.h"

//! @brief Createhardlink create new symbolic link
//!
//! Createhardlink
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
//! - ERROR_TOO_MANY_LINK: max number of hard links has been exceeded
//! - ERROR_GEN_FAILURE: device attached to the system is not functioning
//! - ERROR_NO_SUCH_USER: there was no corresponding entry in the utmp-file
//! - ERROR_INVALID_NAME: filename, directory name, or volume label syntax is incorrect
//! - ERROR_BUFFER_OVERFLOW:  file name is too long
//! - ERROR_INVALID_FUNCTION: incorrect function
//! - ERROR_BAD_PATH_NAME: pathname is too long, or contains invalid characters
//!
//! @retval 1 if creation is successful
//! @retval 0 if creation failed
//!

int32_t CreateHardLink(const char *newlink, const char *target)
{
    errno = 0;  

    // Check parameters
    if (!newlink || !target)
    {
        errno = ERROR_INVALID_PARAMETER;
        return 0;
    }

    int returnCode = link(target, newlink);

    if (returnCode == 0)
    {
        return 1;
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
            errno = ERROR_TOO_MANY_LINKS;
            break;
        case EMLINK:
            errno = ERROR_TOO_MANY_LINKS;
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
            errno = ERROR_ACCESS_DENIED;
            break;
        case EROFS:
            errno = ERROR_ACCESS_DENIED;
            break;
        case EXDEV:
            errno = ERROR_GEN_FAILURE;
            break;
        default:
            errno = ERROR_INVALID_FUNCTION;
    }
    return 0;
}
