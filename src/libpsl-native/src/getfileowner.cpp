//! @file getfileowner.cpp
//! @author Andrew Schwartzmeyer <andschwa@microsoft.com>
//! @brief returns the owner of a file

#include <errno.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <pwd.h>
#include <string.h>
#include <unistd.h>
#include "getstat.h"
#include "getfileowner.h"

//! @brief GetFileOwner returns the owner of a file
//!
//! GetFileOwner
//!
//! @param[in] fileName
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
//! - ERROR_INVALID_ADDRESS: attempt to access invalid address
//! - ERROR_STOPPED_ON_SYMLINK: too many symbolic links
//! - ERROR_GEN_FAILURE: I/O error occurred
//! - ERROR_INVALID_NAME: file provided is not a symbolic link
//! - ERROR_INVALID_FUNCTION: incorrect function
//! - ERROR_BAD_PATH_NAME: pathname is too long
//! - ERROR_OUTOFMEMORY insufficient kernal memory
//!
//! @retval file owner, or NULL if unsuccessful
//!
char* GetFileOwner(const char* fileName)
{
    int32_t ret = 0;
    errno = 0;

    if (!fileName)
    {
        errno = ERROR_INVALID_PARAMETER;
        return NULL;
    }

    struct stat fileStat;
    ret = GetStat(fileName, &fileStat);
    if (ret != 0)
    {
        return NULL;
    }

    struct passwd pwd;
    struct passwd* result = NULL;
    char* buf;

    int buflen = sysconf(_SC_GETPW_R_SIZE_MAX);
    if (buflen < 1)
    {
        buflen = 2048;
    }

allocate:
    buf = (char*)calloc(buflen, sizeof(char));

    ret = getpwuid_r(fileStat.st_uid, &pwd, buf, buflen, &result);

    if (ret != 0)
    {
        switch(errno)
        {
        case ERANGE:
            free(buf);
            buflen *= 2;
            goto allocate;
        case ENOENT:
        case ESRCH:
        case EBADF:
        case EPERM:
            errno = ERROR_NO_SUCH_USER;
            break;
        case ENOMEM:
            errno = ERROR_OUTOFMEMORY;
            break;
       default:
            errno = ERROR_GEN_FAILURE;
        }
        return NULL;
    }

    // no result
    if (result == NULL)
    {
        return NULL;
    }

    size_t userlen = strnlen(pwd.pw_name, buflen);
    char* username = strndup(pwd.pw_name, userlen);
    free(buf);
    return username;
}
