#include "getusername.h"
#include <errno.h>
#include <langinfo.h>
#include <locale.h>
#include <unistd.h>
#include <string>

using namespace std;

const string utf8 = "UTF-8";

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
BOOL GetUserName(WCHAR_T *lpBuffer, LPDWORD lpnSize)
{
	errno = 0;

	// Check parameters
	if (!lpBuffer || !lpnSize) {
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

	// Get username from system in a thread-safe manner
	char userName[*lpnSize];
	int err = getlogin_r(userName, *lpnSize);
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
		default:
			errno = ERROR_INVALID_FUNCTION;
		}
		return 0;
	}

	// TODO: Convert to char* to wchar* (UTF-8 to UTF-16 LE w/o BOM)

	// Unfortunately codecvt is not yet supported by G++, othwerise:
	// wstring_convert<std::codecvt_utf8_utf16<char16_t>,char16_t> conversion;

	// TODO: Use PAL's Unicode converters

	// TODO: set the lpnSize to the userName length (see msdn)

	return 1;
}

