//! @file getusername.cpp
//! @author Andrew Schwartzmeyer <andschwa@microsoft.com>
//! @brief Implements GetUserName Win32 API

#include <errno.h>
#include <langinfo.h>
#include <locale.h>
#include <unistd.h>
#include <string>
#include <unicode/utypes.h>
#include <unicode/ucnv.h>
#include <unicode/ustring.h>
#include <unicode/uchar.h>
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
//! - ERROR_TOO_MANY_OPEN_FILES: already have the maximum allowed number of open files
//! - ERROR_NO_ASSOCIATION: calling process has no controlling terminal
//! - ERROR_INSUFFICIENT_BUFFER: buffer not large enough to hold username string
//! - ERROR_NO_SUCH_USER: there was no corresponding entry in the utmp-file
//! - ERROR_OUTOFMEMORY: insufficient memory to allocate passwd structure
//! - ERROR_NO_ASSOCIATION: standard input didn't refer to a terminal
//! - ERROR_INVALID_FUNCTION: getlogin_r() returned an unrecognized error code
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
    errno = FALSE;

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

    // Get username from system in a thread-safe manner
    int ret = getlogin_r(&username[0], username.size());
    std::string username(LOGIN_NAME_MAX, 0);
    // Map errno to Win32 Error Codes
    if (ret)
    {
        switch (errno)
        {
        case EMFILE:
        case ENFILE:
            errno = ERROR_TOO_MANY_OPEN_FILES;
            break;
        case ENXIO:
            errno = ERROR_NO_ASSOCIATION;
            break;
        case ERANGE:
            errno = ERROR_GEN_FAILURE;
            break;
        case ENOENT:
            errno = ERROR_NO_SUCH_USER;
            break;
        case ENOMEM:
            errno = ERROR_OUTOFMEMORY;
            break;
        case ENOTTY:
            errno = ERROR_NO_ASSOCIATION;
            break;
        default:
            errno = ERROR_INVALID_FUNCTION;
        }
        return FALSE;
    }

    // Convert to char* to WCHAR_T* (UTF-8 to UTF-16 LE w/o BOM)
    std::basic_string<char16_t> username16(LOGIN_NAME_MAX+1, 0);
    icu::UnicodeString username8(username.c_str(), "UTF-8");
    int32_t targetSize = username8.extract(0, username8.length(),
                                           reinterpret_cast<char*>(&username16[0]),
                                           (username16.size()-1)*sizeof(char16_t),
                                           "UTF-16LE");
    // Number of characters including null
    username16.resize(targetSize/sizeof(char16_t)+1);

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

    // Copy bytes from string to buffer
    memcpy(lpBuffer, &username16[0], size*sizeof(char16_t));
    *lpnSize = size;

    return TRUE;
}
