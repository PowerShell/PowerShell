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
//     ERROR_BAD_ENVIRONMENT - locale is not UTF-8
//
// Returns:
//     1 - succeeded
//     0 - failed
BOOL GetUserName(WCHAR_T* lpBuffer, LPDWORD lpnSize)
{
	errno = 0;

	// Select locale from environment
	setlocale(LC_ALL, "");
	// Check that locale is UTF-8
	if (nl_langinfo(CODESET) != utf8) {
		errno = ERROR_BAD_ENVIRONMENT;
		return 0;
	}

	return 1;
}

