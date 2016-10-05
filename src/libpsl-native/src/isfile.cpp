//! @file isfile.cpp
//! @author Andrew Schwartzmeyer <andschwa@microsoft.com>
//! @brief returns if the path exists

#include "isfile.h"

#include <assert.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <pwd.h>
#include <string.h>
#include <fcntl.h>
#include <unistd.h>

//! @brief returns if the path is a file or directory
//!
//! IsFile
//!
//! @param[in] path
//! @parblock
//! A pointer to the buffer that contains the file name
//!
//! char* is marshaled as an LPStr, which on Linux is UTF-8.
//! @endparblock
//!
//! @retval true if path exists, false otherwise
//!
bool IsFile(const char* path)
{
    assert(path);

    struct stat buf;
    return lstat(path, &buf) == 0;
}
