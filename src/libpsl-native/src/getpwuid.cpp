//! @file getpwuid.cpp
//! @author Andrew Schwartzmeyer <andschwa@microsoft.com>
//! @brief returns the username for a uid

#include <errno.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <pwd.h>
#include <string.h>
#include <unistd.h>
#include "getpwuid.h"

//! @brief GetPwUid returns the username for a uid
//!
//! GetPwUid
//!
//! @param[in] uid
//! @parblock
//! The user identifier to lookup.
//! @endparblock
//!
//! @exception errno Passes these errors via errno to GetLastError:
//! - ERROR_NO_SUCH_USER: user lookup unsuccessful
//! - ERROR_OUTOFMEMORY insufficient kernel memory
//! - ERROR_GEN_FAILURE: anything else
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

    ret = getpwuid_r(uid, &pwd, buf, buflen, &result);

    if (ret != 0)
    {
        switch(errno)
        {
        case ERANGE:
            free(buf);
            buflen *= 2;
            goto allocate;
        case ENOENT:
        case ESRCH:
        case EBADF:
        case EPERM:
            errno = ERROR_NO_SUCH_USER;
            break;
        case ENOMEM:
            errno = ERROR_OUTOFMEMORY;
            break;
        default:
            errno = ERROR_GEN_FAILURE;
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
