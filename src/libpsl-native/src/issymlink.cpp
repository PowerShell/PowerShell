//! @file isSymLink.cpp
//! @author George FLeming <v-geflem@microsoft.com>
//! @brief returns whether a path is a symbolic link

#include "issymlink.h"

#include <assert.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <unistd.h>
#include <string>

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
//! @retval true if path is a symbolic link, false otherwise
//!

bool IsSymLink(const char* path)
{
    assert(path);

    struct stat buf;
    int32_t ret = lstat(path, &buf);
    if (ret != 0)
    {
        return false;
    }

    return S_ISLNK(buf.st_mode);
}
