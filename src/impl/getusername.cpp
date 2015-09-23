//! @file getusername.cpp
//! @author Andrew Schwartzmeyer <andschwa@microsoft.com>
//! @brief Implements GetUserName Win32 API

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
//! GetUserNameW is the Unicode variation. See [MSDN documentation].
//!
//! @param[out] lpBuffer
//! @parblock
//! A pointer to the buffer to receive the user's
//! logon name. If this buffer is not large enough to contain the
//! entire user name, the function fails.
//!
//! WCHAR_T* is a Unicode [LPTSTR].
//! @endparblock
//!
//! @param[in, out] lpnSize
//! @parblock
//! On input, this variable specifies the size of the lpBuffer buffer,
//! in TCHARs. On output, the variable receives the number of TCHARs
//! copied to the buffer, including the terminating null character.
//!
//! TCHAR is a Unicode 16-bit [WCHAR].
//!
//! If lpBuffer is too small, the function fails and GetLastError
//! returns ERROR_INSUFFICIENT_BUFFER. This parameter receives the
//! required buffer size, including the terminating null character.
//! @endparblock
//!
//! @exception errno Passes these errors via errno to GetLastError:
//! - ERROR_INVALID_PARAMETER: parameter is not valid
//! - ERROR_BAD_ENVIRONMENT: locale is not UTF-8
//! - ERROR_INSUFFICIENT_BUFFER: buffer not large enough to hold username string
//! - ERROR_NO_SUCH_USER: there was no corresponding user
//! - ERROR_GEN_FAILURE: sysconf() or getpwuid() failed for unknown reasons
//!
//! @retval TRUE If the function succeeds, the return value is a nonzero
//! value, and the variable pointed to by lpnSize contains the number
//! of TCHARs copied to the buffer specified by lpBuffer, including
//! the terminating null character.
//!
//! @retval FALSE If the function fails, the return value is zero. To get
//! extended error information, call GetLastError.
//!
//! [MSDN documentation]: https://msdn.microsoft.com/en-us/library/windows/desktop/ms724432(v=vs.85).aspx
//! [WCHAR]: https://msdn.microsoft.com/en-us/library/windows/desktop/aa383751(v=vs.85).aspx#WCHAR
//! [LPTSTR]: https://msdn.microsoft.com/en-us/library/windows/desktop/aa383751(v=vs.85).aspx#LPTSTR
BOOL GetUserNameW(WCHAR_T* lpBuffer, LPDWORD lpnSize)
{
    // Check parameters
    if (!lpBuffer || !lpnSize)
    {
        errno = ERROR_INVALID_PARAMETER;
        return FALSE;
    }

    // Select locale from environment
    setlocale(LC_ALL, "");
    // Check that locale is UTF-8
    if (nl_langinfo(CODESET) != std::string("UTF-8"))
    {
        errno = ERROR_BAD_ENVIRONMENT;
        return FALSE;
    }

    struct passwd pwd;
    struct passwd* result;
    // gets the initial suggested size for buf
    int buflen = sysconf(_SC_GETPW_R_SIZE_MAX);
    if (buflen == -1)
    {
        errno = ERROR_GEN_FAILURE;
        return FALSE;
    }
    std::string buf(buflen, 0);

    // geteuid() gets the effective user ID of the calling process, and is always successful
    errno = 0;
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
        return FALSE;
    }

    // Check if no user matched
    if (result == NULL)
    {
        errno = ERROR_NO_SUCH_USER;
        return FALSE;
    }

    std::string username(result->pw_name);

    // Convert to UnicodeString
    auto username16 = icu::UnicodeString::fromUTF8(username.c_str());
    // Terminate string with null
    username16.append('\0');

    // Size in WCHARs including null
    const DWORD size = username16.length();
    if (size > *lpnSize)
    {
        errno = ERROR_INSUFFICIENT_BUFFER;
        // Set lpnSize if buffer is too small to inform user
        // of necessary size
        *lpnSize = size;
        return FALSE;
    }

    // Extract string as UTF-16LE to buffer
    username16.extract(0, size, reinterpret_cast<char*>(lpBuffer), "UTF-16LE");

    *lpnSize = size;

    return TRUE;
}
