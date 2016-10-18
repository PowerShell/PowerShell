//! @file isexecutable.cpp
//! @author George Fleming <v-geflem@microsoft.com>
//! @brief returns whether a file is executable

#include "isexecutable.h"

#include <assert.h>
#include <unistd.h>
#include <string>

//! @brief IsExecutable determines if path is executable
//!
//! IsExecutable
//!
//! @param[in] path
//! @parblock
//! A pointer to the buffer that contains the file name
//!
//! char* is marshaled as an LPStr, which on Linux is UTF-8.
//! @endparblock
//!
//! @retval true if path is an executable, false otherwise
//!

bool IsExecutable(const char* path)
{
    assert(path);

    return access(path, X_OK) != -1;
}
