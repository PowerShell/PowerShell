//! @file getusername.cpp
//! @author Andrew Schwartzmeyer <andschwa@microsoft.com>
//! @brief Implements GetUserName for Linux

#include <errno.h>
#include <langinfo.h>
#include <locale.h>
#include <unistd.h>
#include <string>
#include <unicode/unistr.h>
#include <pwd.h>
#include "getusername.h"

//! @brief GetUserName retrieves the name of the user associated with
//! the current thread.
//!
//! @exception errno Passes these errors via errno to GetLastError:
//! - ERROR_INVALID_PARAMETER: parameter is not valid
//! - ERROR_BAD_ENVIRONMENT: locale is not UTF-8
//! - ERROR_NO_SUCH_USER: there was no corresponding user
//! - ERROR_GEN_FAILURE: sysconf() or getpwuid() failed for unknown reasons
//!
//! @retval username as UTF-8 string, or null if unsuccessful
char* GetUserName()
{
    errno = 0;

    // Select locale from environment
    setlocale(LC_ALL, "");
    // Check that locale is UTF-8
    if (nl_langinfo(CODESET) != std::string("UTF-8"))
    {
        errno = ERROR_BAD_ENVIRONMENT;
        return NULL;
    }

    struct passwd pwd;
    struct passwd* result;
    // gets the initial suggested size for buf
    int buflen = sysconf(_SC_GETPW_R_SIZE_MAX);
    if (buflen == -1)
    {
        errno = ERROR_GEN_FAILURE;
        return NULL;
    }
    std::string buf(buflen, 0);

    // geteuid() gets the effective user ID of the calling process, and is always successful
    int ret = getpwuid_r(geteuid(), &pwd, &buf[0], buflen, &result);

    // Map errno to Win32 Error Codes
    if (ret)
    {
        switch (errno)
        {
        case ENOENT:
        case ESRCH:
        case EBADF:
        case EPERM:
            errno = ERROR_NO_SUCH_USER;
            break;
        default:
            errno = ERROR_GEN_FAILURE;
        }
        return NULL;
    }

    // Check if no user matched
    if (result == NULL)
    {
        errno = ERROR_NO_SUCH_USER;
        return NULL;
    }

    // allocate copy on heap so CLR can free it
    return strdup(result->pw_name);
}
