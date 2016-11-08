//! @file getstat.cpp
//! @author Andrew Schwartzmeyer <andschwa@microsoft.com>
//! @brief returns the stat of a file

#include "getstat.h"

#include <errno.h>
#include <assert.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <pwd.h>
#include <string.h>
#include <unistd.h>

//! @brief GetStat returns the stat of a file. This simply delegates to the
//! stat() system call and maps errno to the expected values for GetLastError.
//!
//! GetStat
//!
//! @param[in] path
//! @parblock
//! A pointer to the buffer that contains the file name
//!
//! char* is marshaled as an LPStr, which on Linux is UTF-8.
//! @endparblock
//!
//! @param[in] stat
//! @parblock
//! A pointer to the buffer in which to place the stat information
//! @endparblock
//!
//! @retval 0 if successful
//! @retval -1 if failed
//!

int32_t GetStat(const char* path, struct stat* buf)
{
    assert(path);
    errno = 0;

    return stat(path, buf);
}
