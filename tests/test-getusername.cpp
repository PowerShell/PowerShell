#include <string>
#include <vector>
#include <unistd.h>
#include <gtest/gtest.h>
#include <scxcorelib/scxstrencodingconv.h>
#include "getusername.h"

class GetUserNameTest : public ::testing::Test {
protected:
	DWORD lpnSize;
	std::vector<WCHAR_T> lpBuffer;
	BOOL result;
	std::string userName;

	GetUserNameTest(): userName(std::string(getlogin())) {}

	void GetUserNameWithSize(DWORD size) {
		lpnSize = size;
		// allocate a WCHAR_T buffer to receive username
		lpBuffer.assign(lpnSize, '\0');
		result = GetUserName(&lpBuffer[0], &lpnSize);
	}
};

TEST_F(GetUserNameTest, NormalUse) {
	GetUserNameWithSize(L_cuserid);

	// GetUserName returns 1 on success
	ASSERT_EQ(1, result);

	// GetUserName sets lpnSize to length of username including null
	ASSERT_EQ(userName.size()+1, lpnSize);

	// copy UTF-16 bytes (excluding null) from lpBuffer to vector for conversion
	unsigned char *begin = reinterpret_cast<unsigned char *>(&lpBuffer[0]);
	// -1 to skip null; *2 because UTF-16 encodes two bytes per character
	unsigned char *end = begin + (lpnSize-1)*2;
	std::vector<unsigned char> input(begin, end);
	// convert to UTF-8 for assertion
	std::string output;
	SCXCoreLib::Utf16leToUtf8(input, output);

	EXPECT_EQ(userName, output);
}
