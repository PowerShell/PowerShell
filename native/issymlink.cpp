//! @file isSymLink.cpp
//! @author George FLeming <v-geflem@microsoft.com>
//! @brief returns whether a path is a symbolic link

#include <errno.h>
#include <langinfo.h>
#include <locale.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <unistd.h>
#include <string>
#include <unicode/unistr.h>
#include "issymlink.h"

//! @brief IsSymLink determines if path is a symbolic link 
//!
//! IsSymLink
//!
//! @param[in] fileName
//! @parblock
//! A pointer to the buffer that contains the file name
//!
//! char* is marshaled as an LPStr, which on Linux is UTF-8.
//! @endparblock
//!
//! @exception errno Passes these errors via errno to GetLastError:
//! - ERROR_INVALID_PARAMETER: parameter is not valid
//! - ERROR_BAD_ENVIRONMENT: locale is not UTF-8
//! - ERROR_FILE_NOT_FOUND: file does not exist
//! - ERROR_ACCESS_DENIED: access is denied
//! - ERROR_FILE_NOT_FOUND: the system cannot find the file specified
//! - ERROR_INVALID_ADDRESS: attempt to access invalid address
//! - ERROR_STOPPED_ON_SYMLINK: the operation stopped after reaching a symbolic link
//! - ERROR_GEN_FAILURE: device attached to the system is not functioning
//! - ERROR_NO_SUCH_USER: there was no corresponding entry in the utmp-file
//! - ERROR_INVALID_NAME: filename, directory name, or volume label syntax is incorrect
//! - ERROR_BUFFER_OVERFLOW:  file name is too long
//! - ERROR_INVALID_FUNCTION: incorrect function
//! - ERROR_BAD_PATH_NAME: pathname is too long, or contains invalid characters
//!
//! @retval 1 if path is a symbolic link
//! @retval 0 if path is not a symbolic link
//! @retval -1 If the function fails.. To get extended error information, call GetLastError.
//!

int32_t IsSymLink(const char* fileName)
{
    errno = 0;

    // Check parameters
    if (!fileName)
    {
        errno = ERROR_INVALID_PARAMETER;
        return -1;
    }

    // Select locale from environment
    setlocale(LC_ALL, "");
    // Check that locale is UTF-8
    if (nl_langinfo(CODESET) != std::string("UTF-8"))
    {
        errno = ERROR_BAD_ENVIRONMENT;
        return -1;
    }

    struct stat statBuf;

    int returnCode = lstat(fileName, &statBuf);

    if  (returnCode != 0)
    {
        switch(errno)
        {
        case EACCES:
            errno = ERROR_ACCESS_DENIED;
            break;
        case EBADF:
            errno = ERROR_FILE_NOT_FOUND;
            break;
        case EFAULT:
            errno = ERROR_INVALID_ADDRESS;
            break;
        case ELOOP:
            errno = ERROR_STOPPED_ON_SYMLINK;
            break;
        case ENAMETOOLONG:
            errno = ERROR_GEN_FAILURE;
            break;
        case ENOENT:
            errno = ERROR_FILE_NOT_FOUND;
            break;
        case ENOMEM:
            errno = ERROR_NO_SUCH_USER;
            break;
        case ENOTDIR:
            errno = ERROR_INVALID_NAME;
            break;
        case EOVERFLOW:
            errno = ERROR_BUFFER_OVERFLOW;
            break;
        default:
            errno = ERROR_INVALID_FUNCTION;
        }
        return -1;
    }

    return S_ISLNK(statBuf.st_mode) ? 1 : 0;
}
