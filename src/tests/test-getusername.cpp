#include <string>
#include <unistd.h>
#include <gtest/gtest.h>
#include <unicode/utypes.h>
#include <unicode/ucnv.h>
#include <unicode/ustring.h>
#include <unicode/uchar.h>
#include "getusername.h"

class GetUserNameTest : public ::testing::Test {
protected:
	DWORD lpnSize;
	std::vector<WCHAR_T> lpBuffer;
	BOOL result;
	std::string expectedUsername;
	DWORD expectedSize;

	GetUserNameTest(): expectedUsername(std::string(getlogin())),
	                   expectedSize((expectedUsername.length()+1)*sizeof(char16_t))
	{}

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

		// sets lpnSize to number of bytes including null,
		// note that this is (length+1)*sizeof(char16_t)
		ASSERT_EQ(expectedSize, lpnSize);

		// setup for conversion from UTF-16LE
		const char *begin = reinterpret_cast<char *>(&lpBuffer[0]);
		icu::UnicodeString username16(begin, lpnSize, "UTF-16LE");
		// username16 length includes null and is number of characters
		ASSERT_EQ(expectedUsername.length()+1, username16.length());

		// convert (minus null) to UTF-8 for comparison
		std::string username(lpnSize/sizeof(char16_t)-1, 0);
		ASSERT_EQ(expectedUsername.length(), username.length());
		int32_t targetSize = username16.extract(0, username.length(),
		                                        reinterpret_cast<char *>(&username[0]),
		                                        "UTF-8");

		EXPECT_EQ(expectedUsername, username);
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
		EXPECT_EQ(expectedSize, lpnSize);
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

TEST_F(GetUserNameTest, BufferSizeAsUsername) {
	// the buffer is too small because this is a UTF-8 size
	TestWithSize(expectedUsername.size());
	TestInsufficientBuffer();
}

TEST_F(GetUserNameTest, BufferSizeAsUsernamePlusOne) {
	// the buffer is still too small even with null
	TestWithSize(expectedUsername.size()+1);
	TestInsufficientBuffer();
}

TEST_F(GetUserNameTest, BufferSizeAsUsernameInUTF16) {
	// the buffer is still too small because it is missing null
	TestWithSize(expectedUsername.size()*sizeof(char16_t));
	TestInsufficientBuffer();
}

TEST_F(GetUserNameTest, BufferSizeAsUsernamePlusOneInUTF16) {
	// the buffer is exactly big enough
	TestWithSize((expectedUsername.size()+1)*sizeof(char16_t));
	TestSuccess();
}

TEST_F(GetUserNameTest, BufferSizeAsExpectedSize) {
	// expectedSize is the same as username.size()+1 in UTF16
	TestWithSize(expectedSize);
	TestSuccess();
}

TEST_F(GetUserNameTest, BufferSizeAsLoginNameMax) {
	// LoginNameMax is big enough to hold any username
	TestWithSize(LOGIN_NAME_MAX);
	TestSuccess();
}
