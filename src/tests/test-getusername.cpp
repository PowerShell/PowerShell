#include <unistd.h>
#include <gtest/gtest.h>
#include "getusername.h"

TEST(GetUserName,simple)
{
	// allocate a WCHAR_T buffer to receive username
	DWORD lpnSize = 64;
	WCHAR_T lpBuffer[lpnSize];

	BOOL result = GetUserName(lpBuffer, &lpnSize);

	// GetUserName returns 1 on success
	ASSERT_EQ(1, result);

	// get expected username
	const char *username = getlogin();

	// GetUserName sets lpnSize to length of username including null
	ASSERT_EQ(strlen(username)+1, lpnSize);

	// TODO: ASSERT_STREQ(username, Utf16leToUtf8(lpBuffer))
}
