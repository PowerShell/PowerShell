#include "getcomputername.h"
#include <errno.h>
#include <langinfo.h>
#include <unistd.h>
#include <string>
#include <iostream>


// https://msdn.microsoft.com/en-us/library/windows/desktop/ms724432(v=vs.85).aspx
// Sets errno to:
//     ERROR_INVALID_PARAMETER - parameter is not valid
//     ERROR_BAD_ENVIRONMENT - locale is not UTF-8
//     ERROR_TOO_MANY_OPEN_FILES - already have the maximum allowed number of open files
//     ERROR_NO_ASSOCIATION - calling process has no controlling terminal
//     ERROR_INSUFFICIENT_BUFFER - buffer not large enough to hold username string
//     ERROR_NO_SUCH_USER - there was no corresponding entry in the utmp-file
//     ERROR_OUTOFMEMORY - insufficient memory to allocate passwd structure
//     ERROR_NO_ASSOCIATION - standard input didn't refer to a terminal
//     ERROR_INVALID_FUNCTION - getlogin_r() returned an unrecognized error code
//
// Returns:
//     1 - succeeded
//     0 - failed
BOOL GetComputerNameW(LPTSTR nameOfComputer, LPDWORD len)
{
    const std::string utf8 = "UTF-8";
    errno = 0;
    
    // Check parameters
    if (!nameOfComputer || !len) {
        errno = ERROR_INVALID_PARAMETER;
        return 0;
    }
    
    // Select locale from environment
    setlocale(LC_ALL, "");
    // Check that locale is UTF-8
    if (nl_langinfo(CODESET) != utf8) {
        errno = ERROR_BAD_ENVIRONMENT;
        return 0;
    }
    
    // Get computername from system in a thread-safe manner
    std::string computerName(HOST_MAX_NAME, '\0');
    int err = gethostname(&computerName[0], computerName.size());
    // Map errno to Win32 Error Codes
    if (err != 0) {
        switch (errno) {
            case EMFILE:
            case ENFILE:
                errno = ERROR_TOO_MANY_OPEN_FILES;
                break;
            case ENXIO:
                errno = ERROR_NO_ASSOCIATION;
                break;
            case ERANGE:
                errno = ERROR_INSUFFICIENT_BUFFER;
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
                errno = ERROR_NO_ASSOCIATION;
            default:
                errno = ERROR_INVALID_FUNCTION;
        }
        return 0;
    }
    
    //Resize the string to not inlcude extra buffer space
    const DWORD length = computerName.find_first_of('\0') + 1;
    computerName.resize(length);
    
    //Check if parameters passed enough buffer space
    if (length > *len) {
        errno = ERROR_INSUFFICIENT_BUFFER;
        // Set lpnSize if buffer is too small to inform user
        // of necessary size
        *len= length;
        return 0;
    }
    
    memcpy(nameOfComputer, &computerName[0], length);
    *len = length;
    return 1;
    
}

