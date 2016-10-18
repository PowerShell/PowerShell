//! @file isdirectory.cpp
//! @author Andrew Schwartzmeyer <andschwa@microsoft.com>
//! @brief returns if the path is a directory

#include "getstat.h"
#include "getpwuid.h"
#include "getfileowner.h"
#include "isdirectory.h"

#include <assert.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <pwd.h>
#include <string.h>
#include <unistd.h>

//! @brief returns if the path is a directory; uses stat and so follows symlinks
//!
//! IsDirectory
//!
//! @param[in] path
//! @parblock
//! A pointer to the buffer that contains the file name
//!
//! char* is marshaled as an LPStr, which on Linux is UTF-8.
//! @endparblock
//!
//! @retval true if directory, false otherwise
//!
bool IsDirectory(const char* path)
{
    assert(path);

    struct stat buf;
    int32_t ret = GetStat(path, &buf);
    if (ret != 0)
    {
        return false;
    }

    return S_ISDIR(buf.st_mode);
}
