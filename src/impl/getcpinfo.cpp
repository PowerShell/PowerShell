//! @file getcpinfo.cpp
//! @author Aaron Ktaz <v-aakatz@microsoft.com>
//! @brief Implements GetCpInfoW Win32 API

#include <errno.h>
#include <string>
#include <langinfo.h>
#include "getcpinfo.h"

//! @brief GetCPInfoW retrieves the name of the code page associated with
//! the current thread.
//!
//! GetCpInfoW the Unicode variation. See [MSDN documentation].
//!
//! @param[in] codepage
//! @parblock
//! An UINT Identifier for the code page for which to retrieve information.
//! See Code Page Identifiers for details
//!
//! @endparblock
//!
//! @param[out] cpinfo
//! @parblock
//! Pointer to a CPINFO structure that receives information about the code page
//!
//! LPCPINFO is a pointer to the structure _cpinfo used to contain the information 
//! about a code page
//!
//!
//! @exception errno Passes these errors via errno to GetLastError:
//! - ERROR_INVALID_PARAMETER: parameter is not valid
//!
//! @retval 1 If the function succeeds, the return value is a nonzero
//! value, and cpinfo is population with information about the code page
//!
//! @retval 0 If the function fails, the return value is zero. To get
//! extended error information, call GetLastError.
//!
//! [MSDN documentation]:https://msdn.microsoft.com/en-us/library/windows/desktop/dd318078%28v=vs.85%29.aspx
//! [_cpinfo]: https://msdn.microsoft.com/en-us/library/windows/desktop/dd317780%28v=vs.85%29.aspx
//! [CodePageIdentifiers] https://msdn.microsoft.com/en-us/library/windows/desktop/dd317756%28v=vs.85%29.aspx

BOOL GetCPInfoW(UINT codepage, CPINFO* cpinfo)
{
    
    const std::string utf8 = "UTF-8";
    errno = 0;
       
    // Select locale from environment
    setlocale(LC_ALL, "");
    // Check that locale is UTF-8
    if (nl_langinfo(CODESET) != utf8)
    {
        errno = ERROR_BAD_ENVIRONMENT;
        return 0;
    }
    
    // if codepage is utf8
    if(codepage == 65001) 
    {
        cpinfo->DefaultChar[0] = '?';
        cpinfo->DefaultChar[1] = '0';
        cpinfo->MaxCharSize = 4;
        for(int i = 0; i < const_cpinfo::MAX_LEADBYTES; i++ ){
            cpinfo->LeadByte[i] = '0';
        }
        return TRUE;
    }
    else
    {
        errno = ERROR_INVALID_PARAMETER;
        return FALSE;
    }
}

