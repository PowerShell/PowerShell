#include "getusername.h"
#include <errno.h>
#include <langinfo.h>
#include <locale.h>
#include <unistd.h>
#include <string>
#include <unicode/utypes.h>
#include <unicode/ucnv.h>
#include <unicode/ustring.h>
#include <unicode/uchar.h>

/*
  GetUserName function

  https://msdn.microsoft.com/en-us/library/windows/desktop/ms724432(v=vs.85).aspx

  Retrieves the name of the user associated with the current thread.

  Parameters

  lpBuffer [out]

  A pointer to the buffer to receive the user's logon name. If this
  buffer is not large enough to contain the entire user name, the
  function fails. A buffer size of (UNLEN + 1) characters will hold
  the maximum length user name including the terminating null
  character. UNLEN is defined in Lmcons.h.

  lpnSize [in, out]

  On input, this variable specifies the size of the lpBuffer buffer,
  in TCHARs. On output, the variable receives the number of TCHARs
  copied to the buffer, including the terminating null character.

  Note that TCHAR is WCHAR here because UNICODE is defined, and so a
  TCHAR is a byte.

  https://msdn.microsoft.com/en-us/library/windows/desktop/aa383751(v=vs.85).aspx#CHAR

  If lpBuffer is too small, the function fails and GetLastError
  returns ERROR_INSUFFICIENT_BUFFER. This parameter receives the
  required buffer size, including the terminating null character.


  Sets errno to:
  ERROR_INVALID_PARAMETER - parameter is not valid
  ERROR_BAD_ENVIRONMENT - locale is not UTF-8
  ERROR_TOO_MANY_OPEN_FILES - already have the maximum allowed number of open files
  ERROR_NO_ASSOCIATION - calling process has no controlling terminal
  ERROR_INSUFFICIENT_BUFFER - buffer not large enough to hold username string
  ERROR_NO_SUCH_USER - there was no corresponding entry in the utmp-file
  ERROR_OUTOFMEMORY - insufficient memory to allocate passwd structure
  ERROR_NO_ASSOCIATION - standard input didn't refer to a terminal
  ERROR_INVALID_FUNCTION - getlogin_r() returned an unrecognized error code

  Return value

  If the function succeeds, the return value is a nonzero value, and
  the variable pointed to by lpnSize contains the number of TCHARs
  copied to the buffer specified by lpBuffer, including the
  terminating null character.

  If the function fails, the return value is zero. To get extended
  error information, call GetLastError.
*/
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

	// Convert to char * to WCHAR_T * (UTF-8 to UTF-16 LE w/o BOM)
	std::basic_string<char16_t> username16(LOGIN_NAME_MAX+1, 0);
	icu::UnicodeString username8(username.c_str(), "UTF-8");
	int32_t targetSize = username8.extract(0, username8.length(),
	                                       reinterpret_cast<char *>(&username16[0]),
	                                       (username16.size()-1)*sizeof(char16_t),
	                                       "UTF-16LE");
	// Number of characters including null
	username16.resize(targetSize/sizeof(char16_t)+1);

	// Size in WCHARs including null
	const DWORD size = username16.length();
	if (size > *lpnSize) {
		errno = ERROR_INSUFFICIENT_BUFFER;
		// Set lpnSize if buffer is too small to inform user
		// of necessary size
		*lpnSize = size;
		return 0;
	}

	// Copy bytes from string to buffer
	memcpy(lpBuffer, &username16[0], size*sizeof(char16_t));
	*lpnSize = size;

	return 1;
}
