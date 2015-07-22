#include <string>
#include <vector>
#include <unistd.h>
#include <gtest/gtest.h>
#include <scxcorelib/scxstrencodingconv.h>
#include "getusername.h"

using std::string;
using std::vector;
using SCXCoreLib::Utf16leToUtf8;

TEST(GetUserName,simple)
{
	// allocate a WCHAR_T buffer to receive username
	DWORD lpnSize = 64;
	WCHAR_T lpBuffer[lpnSize];

	BOOL result = GetUserName(lpBuffer, &lpnSize);

	// GetUserName returns 1 on success
	ASSERT_EQ(1, result);

	// get expected username
	string username = string(getlogin());

	// GetUserName sets lpnSize to length of username including null
	ASSERT_EQ(username.size()+1, lpnSize);

	// copy UTF-16 bytes from lpBuffer to vector for conversion
	vector<unsigned char> input(reinterpret_cast<unsigned char *>(&lpBuffer[0]),
	                            reinterpret_cast<unsigned char *>(&lpBuffer[lpnSize-1]));
	// convert to UTF-8 for assertion
	string output;
	Utf16leToUtf8(input, output);

	EXPECT_EQ(username, output);
}
