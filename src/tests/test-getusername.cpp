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

	void TestWithSize(DWORD size) {
		lpnSize = size;
		// allocate a WCHAR_T buffer to receive username
		lpBuffer.assign(lpnSize, '\0');
		result = GetUserName(&lpBuffer[0], &lpnSize);
	}

	void TestSuccess() {
		SCOPED_TRACE("");
		// returns 1 on success
		EXPECT_EQ(1, result);

		// sets lpnSize to length of username + null
		ASSERT_EQ(userName.size()+1, lpnSize);

		// copy UTF-16 bytes (excluding null) from lpBuffer to vector for conversion
		unsigned char *begin = reinterpret_cast<unsigned char *>(&lpBuffer[0]);
		// -1 to skip null; *2 because UTF-16 encodes two bytes per character
		unsigned char *end = begin + (lpnSize-1)*2;
		std::vector<unsigned char> input(begin, end);
		// convert to UTF-8 for comparison
		std::string output;
		SCXCoreLib::Utf16leToUtf8(input, output);

		EXPECT_EQ(userName, output);
	}

	void TestInvalidParameter() {
		SCOPED_TRACE("");

		// returns 0 on failure
		EXPECT_EQ(0, result);

		// sets errno to ERROR_INVALID_PARAMETER when lpBuffer is null
		// (which is the case for an empty vector)
		EXPECT_EQ(errno, ERROR_INVALID_PARAMETER);
	}

	void TestInsufficientBuffer() {
		SCOPED_TRACE("");

		// returns 0 on failure
		EXPECT_EQ(0, result);

		// sets errno to ERROR_INSUFFICIENT_BUFFER
		EXPECT_EQ(errno, ERROR_INSUFFICIENT_BUFFER);

		// sets lpnSize to length of username + null
		EXPECT_EQ(userName.size()+1, lpnSize);
	}
};

TEST_F(GetUserNameTest, BufferAsNullButNotBufferSize) {
	lpnSize = 1;
	result = GetUserName(NULL, &lpnSize);

	TestInvalidParameter();
	// does not reset lpnSize
	EXPECT_EQ(1, lpnSize);
}

TEST_F(GetUserNameTest, BufferSizeAsNullButNotBuffer) {
	lpBuffer.push_back('\0');
	result = GetUserName(&lpBuffer[0], NULL);

	TestInvalidParameter();
}

TEST_F(GetUserNameTest, BufferSizeAsZero) {
	TestWithSize(0);
	TestInvalidParameter();
	// does not reset lpnSize
	EXPECT_EQ(0, lpnSize);
}

TEST_F(GetUserNameTest, BufferSizeAsOne) {
	// theoretically this should never fail because any non-empty
	// username length will be >1 with trailing null
	TestWithSize(1);
	TestInsufficientBuffer();
}

TEST_F(GetUserNameTest, BufferSizeAsUserName) {
	// the buffer is too small because this does not include null
	TestWithSize(userName.size());
	TestInsufficientBuffer();
}

TEST_F(GetUserNameTest, BufferSizeAsUserNameMinusOne) {
	// the buffer is also too small
	TestWithSize(userName.size()-1);
	TestInsufficientBuffer();
}

TEST_F(GetUserNameTest, BufferSizeAsUserNamePlusOne) {
	// the buffer is exactly big enough will null
	TestWithSize(userName.size()+1);
	TestSuccess();
}

TEST_F(GetUserNameTest, BufferSizeAsLoginNameMax) {
	// LoginNameMax is big enough to hold any username
	TestWithSize(LOGIN_NAME_MAX);
	TestSuccess();
}
