//! @file followSymLink.cpp
//! @author George FLeming <v-geflem@microsoft.com>
//! @brief returns whether a path is a symbolic link

#include "followsymlink.h"
#include "issymlink.h"

#include <assert.h>
#include <errno.h>
#include <unistd.h>
#include <string>

//! @brief FollowSymLink determines target path of a sym link
//!
//! @param[in] fileName
//! @parblock
//! A pointer to the buffer that contains the file name
//!
//! char* is marshaled as an LPStr, which on Linux is UTF-8.
//! @endparblock
//!
//! @retval target path, or NULL if unsuccessful
//!

char* FollowSymLink(const char* fileName)
{
    assert(fileName);
    errno = 0;

    // return null for non symlinks
    if (!IsSymLink(fileName))
    {
        return NULL;
    }

    // attempt to resolve with the absolute file path
    char buffer[PATH_MAX];
    char* realPath = realpath(fileName, buffer);

    if (realPath)
    {
        return strndup(realPath, strlen(realPath) + 1);
    }

    // if the path wasn't resolved, use readlink
    ssize_t sz = readlink(fileName, buffer, PATH_MAX);
    if  (sz == -1)
    {
        return NULL;
    }

    buffer[sz] = '\0';
    return strndup(buffer, sz + 1);
}
