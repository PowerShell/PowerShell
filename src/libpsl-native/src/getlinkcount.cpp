//! @file getlinkcount.cpp
//! @author George FLeming <v-geflem@microsoft.com>
//! @brief Retrieve link count of a file

#include "getlinkcount.h"

#include <assert.h>
#include <errno.h>
#include <locale.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <unistd.h>
#include <string>

//! @brief GetLinkCount retrieves the file link count (number of hard links)
//! for the given file
//!
//! GetLinkCount
//!
//! @param[in] fileName
//! @parblock
//! A pointer to the buffer that contains the file name
//!
//! char* is marshaled as an LPStr, which on Linux is UTF-8.
//! @endparblock
//!
//! @param[out] count
//! @parblock
//! This function returns the number of hard links associated with this file
//! @endparblock
//!
//! @retval 1 If the function succeeds, and the variable pointed to by buffer contains
//! information about the files
//! @retval 0 If the function fails, the return value is zero. To get
//! extended error information, call GetLastError.
//!

int32_t GetLinkCount(const char* fileName, int32_t *count)
{
    assert(fileName);
    assert(count);
    errno = 0;

    struct stat statBuf;

    int32_t ret = lstat(fileName, &statBuf);

    *count = statBuf.st_nlink;
    return ret;
}
