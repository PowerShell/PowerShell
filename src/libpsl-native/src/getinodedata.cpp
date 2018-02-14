// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//! @brief Retrieve the device ID and inode number of a file

#include "getinodedata.h"

#include <assert.h>
#include <errno.h>
#include <locale.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <unistd.h>
#include <string>

//! @brief GetInodeData retrieves a file's device and inode information.
//!
//! GetInodeData
//!
//! @param[in] fileName
//! @parblock
//! A pointer to the buffer that contains the file path
//!
//! char* is marshaled as an LPStr, which on Linux is UTF-8.
//! @endparblock
//!
//! @param[out] device
//! @parblock
//! Points to a uint64_t value that will contain the file's device ID.
//! @endparblock
//!
//! @param[out] inode
//! @parblock
//! Points to a uint64_t value that will contain the file's inode number.
//! @endparblock
//!
//! @retval 0 If the function succeeds, -1 otherwise.
//!

int32_t GetInodeData(const char* fileName, uint64_t* device, uint64_t* inode)
{
    assert(fileName);
    assert(device);
    assert(inode);
    errno = 0;

    struct stat statBuf;
    int ret = stat(fileName, &statBuf);

    if (ret == 0)
    {
        *device = statBuf.st_dev;
        *inode = statBuf.st_ino;
    }

    return ret;
}
