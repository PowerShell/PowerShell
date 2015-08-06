#include "getcpinfo.h"
#include <errno.h>
#include <string>
#include <langinfo.h>


// https://msdn.microsoft.com/en-us/library/windows/desktop/ms724432(v=vs.85).aspx
// Sets errno to:
//     ERROR_INVALID_PARAMETER - parameter is not valid
//     ERROR_BAD_ENVIRONMENT - locale is not UTF-8
//
// Returns:
//     TRUE - succeeded
//     FALSE - failed

BOOL GetCPInfoW(UINT codepage, CPINFO &cpinfo)
{
    
    const std::string utf8 = "UTF-8";
    errno = 0;
       
    //Check that codepage is not null
    if(!codepage) {
        errno = ERROR_INVALID_PARAMETER;
        return FALSE;
    }
    
    // Select locale from environment
    setlocale(LC_ALL, "");
    // Check that locale is UTF-8
    if (nl_langinfo(CODESET) != utf8) {
        errno = ERROR_BAD_ENVIRONMENT;
        return 0;
    }
    
    //if codepage is utf8
    if(codepage == 65001) {
        cpinfo.DefaultChar[0] = '?';
        cpinfo.DefaultChar[1] = '0';
        cpinfo.LeadByte[0] = '0';
        cpinfo.LeadByte[1] = '0';
        cpinfo.MaxCharSize = 4;
        return TRUE;
    }
    else{
        errno = ERROR_INVALID_PARAMETER;
        return FALSE;
    }
}

