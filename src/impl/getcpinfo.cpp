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
//! 65001 in the number for UTF8.  It is the only valid input parameter
//!because Linux and Unix only use UTF8 for codepage
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
//! typedef struct _cpinfo {
//!    UINT MaxCharSize;
//!    BYTE DefaultChar[MAX_DEFAULTCHAR];
//!    BYTE LeadByte[MAX_LEADBYTES];
//! } CPINFO, *LPCPINFO;
//!
//! _cpinfo is a struct that comprises
//!
//! UINT MaxCharSize;
//! Maximum length, in bytes, of a character in the code page.
//! The length can be 1 for a single-byte character set (SBCS),
//! 2 for a double-byte character set (DBCS), or a value larger
//! than 2 for other character set types. The function cannot
//! use the size to distinguish an SBCS or a DBCS from other
//! character sets because of other factors, for example,
//! the use of ISCII or ISO-2022-xx code pages.
//!
//! BYTE DefaultChar[const_cpinfo::MAX_DEFAULTCHAR];
//! Default character used when translating character
//! strings to the specific code page. This character is used by
//! the WideCharToMultiByte function if an explicit default
//! character is not specified. The default is usually the "?"
//! character for the code page
//!
//! BYTE LeadByte[const_cpinfo::MAX_LEADBYTES];
//! A fixed-length array of lead byte ranges, for which the number
//! of lead byte ranges is variable. If the code page has no lead
//! bytes, every element of the array is set to NULL. If the code
//! page has lead bytes, the array specifies a starting value and
//! an ending value for each range. Ranges are inclusive, and the
//! maximum number of ranges for any code page is five. The array
//! uses two bytes to describe each range, with two null bytes as
//! a terminator after the last range.
//!
//! MAX_DEFAULTCHAR is an int of size 2
//! MAX_LEADBYTES is an int of size 12
//!
//!
//! @exception errno Passes these errors via errno to GetLastError:
//! - ERROR_INVALID_PARAMETER: parameter is not valid
//!
//! @retval TRUE If the function succeeds, the return value is a nonzero
//! value, and cpinfo is population with information about the code page
//!
//! @retval FALSE If the function fails, the return value is zero. To get
//! extended error information, call GetLastError.
//!
//! [MSDN documentation]:https://msdn.microsoft.com/en-us/library/windows/desktop/dd318078%28v=vs.85%29.aspx
//! [_cpinfo]: https://msdn.microsoft.com/en-us/library/windows/desktop/dd317780(v=vs.85).aspx
//! [CodePageIdentifiers] https://msdn.microsoft.com/en-us/library/windows/desktop/dd317756(v=vs.85).aspx

BOOL GetCPInfoW(UINT codepage, CPINFO* cpinfo)
{
    errno = FALSE;

    // Select locale from environment
    setlocale(LC_ALL, "");

    // Check that locale is UTF-8
    if (nl_langinfo(CODESET) != std::string("UTF-8"))
    {
        errno = ERROR_BAD_ENVIRONMENT;
        return FALSE;
    }

    if (codepage != const_cpinfo::UTF8)
    {
        //If other value is used return error because Linux and Unix only used UTF-8 for codepage
        errno = ERROR_INVALID_PARAMETER;
        return FALSE;
    }

    // UTF-8 uses the default char for DefaultChar[0] which is '?'
    cpinfo->DefaultChar[0] = '?';
    cpinfo->DefaultChar[1] = '0';
    cpinfo->MaxCharSize = 4;

    for (int i = 0; i < const_cpinfo::MAX_LEADBYTES; i++ )
    {
        // UTF-8 uses the default has no LeadByte as result LeadByte is '0'
        cpinfo->LeadByte[i] = '0';
    }

    return TRUE;

}

