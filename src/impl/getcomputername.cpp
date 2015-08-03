#include "getcomputername.h"
#include <unistd.h>
#include <errno.h>
#include <string>
#include <langinfo.h>

BOOL GetComputerNameW(LPTSTR name, LPDWORD len)
{
    const std::string utf8 = "UTF-8";
    errno = 0;
    
    // Check parameters
    if (!name || !len) {
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
    
    size_t len2 = *len; 
    int host =  gethostname(name, len2);
    if(host == 0)
    {
        return TRUE;
    }
    else 
    {
        errno = ERROR_BUFFER_OVERFLOW;
        return FALSE;
    }
}

