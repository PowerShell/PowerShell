// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//! @brief Determines whether two paths ultimately point to the same filesystem object

#include "getstat.h"
#include "issamefilesystemitem.h"

#include <assert.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <unistd.h>

//! @brief Returns a boolean value indicating whether two paths ultimately refer to the same file or directory.
//!
//! IsSameFileSystemItem
//!
//! @param[in] path_one
//! @parblock
//! A pointer to the buffer that contains the first path.
//!
//! char* is marshaled as an LPStr, which on Linux is UTF-8.
//! @endparblock
//!
//! @param[in] path_two
//! @parblock
//! A pointer to the buffer that contains the second path.
//!
//! char* is marshaled as an LPStr, which on Linux is UTF-8.
//! @endparblock
//!
//! @retval true if both paths point to the same filesystem object,
//! false otherwise
//!
bool IsSameFileSystemItem(const char* path_one, const char* path_two)
{
    assert(path_one);
    assert(path_two);

    struct stat buf_1;
    struct stat buf_2;

    if (GetStat(path_one, &buf_1) == 0 && GetStat(path_two, &buf_2) == 0)
    {
        return buf_1.st_dev == buf_2.st_dev && buf_1.st_ino == buf_2.st_ino;
    }

    return false;
}
