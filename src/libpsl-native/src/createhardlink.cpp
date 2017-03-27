//! @file createsymlink.cpp
//! @author George FLeming <v-geflem@microsoft.com>
//! @brief create new hard link

#include "createhardlink.h"

#include <assert.h>
#include <unistd.h>
#include <string>

//! @brief Createhardlink create new symbolic link
//!
//! Createhardlink
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
//! @retval 0 if successful, otherwise -1
//!

int32_t CreateHardLink(const char *newlink, const char *target)
{
    assert(newlink);
    assert(target);

    return link(target, newlink);
}
