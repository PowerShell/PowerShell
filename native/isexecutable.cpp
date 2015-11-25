//! @file isExecutable.cpp
//! @author George FLeming <v-geflem@microsoft.com>
//! @brief returns whether a file is executable

#include <errno.h>
#include <unistd.h>
#include <string>
#include "isexecutable.h"

//! @brief IsExecutable determines if path is executable
//!
//! IsExecutable
//!
//! @param[in] fileName
//! @parblock
//! A pointer to the buffer that contains the file name
//!
//! char* is marshaled as an LPStr, which on Linux is UTF-8.
//! @endparblock
//!
//! @exception errno Passes these errors via errno to GetLastError:
//! - ERROR_BAD_ENVIRONMENT: locale is not UTF-8
//! - ERROR_FILE_NOT_FOUND: the system cannot find the file specified
//! - ERROR_INVALID_ADDRESS: attempt to access invalid address
//! - ERROR_GEN_FAILURE: device attached to the system is not functioning
//! - ERROR_INVALID_NAME: filename, directory name, or volume label syntax is incorrect
//! - ERROR_INVALID_FUNCTION: incorrect function
//! - ERROR_INVALID_PARAMETER: parameter to access(2) call is incorrect
//!
//! @retval 1 if path is an executable
//! @retval 0 if path is not a executable
//! @retval -1 If the function fails.. To get extended error information, call GetLastError.
//!

int32_t IsExecutable(const char* fileName)
{
    errno = 0;

    // Check parameters
    if (!fileName)
    {
        errno = ERROR_INVALID_PARAMETER;
        return -1;
    }

    int returnCode = access(fileName, X_OK);

    if  (returnCode == 0)
    {
        return 1;
    }

    switch(errno)
    {
    case EACCES:
        return 0;
    case EBADF:
    case ENOENT:
        errno = ERROR_FILE_NOT_FOUND;
        break;
    case EFAULT:
        errno = ERROR_INVALID_ADDRESS;
        break;
    case ELOOP:
        errno = ERROR_STOPPED_ON_SYMLINK;
        break;
    case EIO:
    case ENOMEM:
        errno = ERROR_GEN_FAILURE;
        break;
    case ENOTDIR:
    case ENAMETOOLONG:
        errno = ERROR_INVALID_NAME;
        break;
    case EINVAL:
        errno = ERROR_INVALID_PARAMETER;
        break;
    default:
        errno = ERROR_INVALID_FUNCTION;
    }
    return -1;
}
