//! @file getpwuid.cpp
//! @author Andrew Schwartzmeyer <andschwa@microsoft.com>
//! @brief returns the username for a uid

#include "getpwuid.h"

#include <errno.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <pwd.h>
#include <string.h>
#include <unistd.h>

//! @brief GetPwUid returns the username for a uid
//!
//! GetPwUid
//!
//! @param[in] uid
//! @parblock
//! The user identifier to lookup.
//! @endparblock
//!
//! @retval username as UTF-8 string, or NULL if unsuccessful
//!
char* GetPwUid(uid_t uid)
{
    int32_t ret = 0;
    struct passwd pwd;
    struct passwd* result = NULL;
    char* buf;

    int buflen = sysconf(_SC_GETPW_R_SIZE_MAX);
    if (buflen < 1)
    {
        buflen = 2048;
    }

allocate:
    buf = (char*)calloc(buflen, sizeof(char));

    errno = 0;
    ret = getpwuid_r(uid, &pwd, buf, buflen, &result);

    if (ret != 0)
    {
        if (errno == ERANGE)
        {
            free(buf);
            buflen *= 2;
            goto allocate;
        }
        return NULL;
    }

    // no result
    if (result == NULL)
    {
        return NULL;
    }

    // allocate copy on heap so CLR can free it
    size_t userlen = strnlen(pwd.pw_name, buflen);
    char* username = strndup(pwd.pw_name, userlen);
    free(buf);
    return username;
}
