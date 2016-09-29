//! @file getfileowner.cpp
//! @author Andrew Schwartzmeyer <andschwa@microsoft.com>
//! @brief returns the owner of a file

#include "getstat.h"
#include "getpwuid.h"
#include "getfileowner.h"

#include <errno.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <pwd.h>
#include <string.h>
#include <unistd.h>

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
//! @exception errno Passes this error via errno to GetLastError:
//! - ERROR_INVALID_PARAMETER: parameter is not valid
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

    struct stat buf;
    ret = GetStat(fileName, &buf);
    if (ret != 0)
    {
        return NULL;
    }

    return GetPwUid(buf.st_uid);
}
