#include "getusername.h"
#include <errno.h>
#include <langinfo.h>
#include <locale.h>
#include <unistd.h>
#include <string>
#include <vector>
#include <scxcorelib/scxstrencodingconv.h>

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
	const std::string utf8 = "UTF-8";

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
	std::string username(LOGIN_NAME_MAX, '\0');
	int err = getlogin_r(&username[0], username.size());
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

	// "Trim" the username to the first trailing null because
	// otherwise the std::string with repeated null characters is
	// valid, and the conversion will still include all the null
	// characters. Creating a std::string from the C string of the
	// original effectively trims it to the first null, without
	// the need to manually trim whitespace (nor using Boost).
	username = std::string(username.c_str());

	// Convert to char * to WCHAR_T * (UTF-8 to UTF-16 LE w/o BOM)
	std::vector<unsigned char> output;
	SCXCoreLib::Utf8ToUtf16le(username, output);

	// The length is the number of characters in the string, which
	// is half the string size because UTF-16 encodes two bytes
	// per character, plus one for the trailing null.
	const DWORD length = output.size()/2 + 1;
	if (length > *lpnSize) {
		errno = ERROR_INSUFFICIENT_BUFFER;
		// Set lpnSize if buffer is too small to inform user
		// of necessary size
		*lpnSize = length;
		return 0;
	}

	// Add two null bytes (because it's UTF-16)
	output.push_back('\0');
	output.push_back('\0');

	memcpy(lpBuffer, &output[0], output.size());
	*lpnSize = output.size()/2;

	return 1;
}
