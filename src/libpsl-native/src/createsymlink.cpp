//! @file createsymlink.cpp
//! @author George FLeming <v-geflem@microsoft.com>
//! @brief create new symbolic link

#include "createsymlink.h"

#include <assert.h>
#include <errno.h>
#include <unistd.h>
#include <string>

//! @brief Createsymlink create new symbolic link
//!
//! Createsymlink
//!
//! @param[in] link
//! @parblock
//! A pointer to the buffer that contains the symbolic link to create
//!
//! char* is marshaled as an LPStr, which on Linux is UTF-8.
//! @endparblock
//!
//! @param[in] target
//! @parblock
//! A pointer to the buffer that contains the existing file
//!
//! char* is marshaled as an LPStr, which on Linux is UTF-8.
//! @endparblock
//!
//! @retval 0 if successful, -1 otherwise
//!

int32_t CreateSymLink(const char *link, const char *target)
{
    assert(link);
    assert(target);

    errno = 0;

    return symlink(target, link);
}
