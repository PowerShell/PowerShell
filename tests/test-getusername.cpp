//! @file test-getusername.cpp
//! @author Andrew Schwartzmeyer <andschwa@microsoft.com>
//! @brief Unit tests for GetUserName

#include <string>
#include <vector>
#include <unistd.h>
#include <gtest/gtest.h>
#include <unicode/utypes.h>
#include <unicode/ucnv.h>
#include <unicode/ustring.h>
#include <unicode/uchar.h>
#include "getusername.h"

//! Test fixture for GetUserNameW
class GetUserNameTest : public ::testing::Test
{
protected:
    DWORD lpnSize;
    std::vector<WCHAR_T> lpBuffer;
    BOOL result;
    std::string expectedUsername;
    DWORD expectedSize;

    GetUserNameTest(): expectedUsername(std::string(getlogin())),
                       expectedSize(expectedUsername.length()+1)
    {
    }

    //! Invokes GetUserNameW with lpnSize and lpBuffer, saves result.
    //!
    //! @param size Assigns to lpnSize and allocates lpBuffer with
    //! size number of null characters.
    void TestWithSize(DWORD size)
    {
        lpnSize = size;
        // allocate a WCHAR_T buffer to receive username
        lpBuffer.assign(lpnSize, '\0');
        result = GetUserNameW(&lpBuffer[0], &lpnSize);
    }

    //! Checks the effects of GetUserNameW for success.
    void TestSuccess()
    {
        SCOPED_TRACE("");

        //! Returns TRUE on success.
        EXPECT_EQ(TRUE, result);

        //! Sets lpnSize to number of WCHARs including null.
        ASSERT_EQ(expectedSize, lpnSize);

        // setup for conversion from UTF-16LE
        const char* begin = reinterpret_cast<char*>(&lpBuffer[0]);
        // multiply to get number of bytes
        icu::UnicodeString username16(begin, lpnSize*sizeof(char16_t), "UTF-16LE");
        // username16 length includes null and is number of characters
        ASSERT_EQ(expectedSize, username16.length());

        // convert (minus null) to UTF-8 for comparison
        std::string username(lpnSize-1, 0);
        ASSERT_EQ(expectedUsername.length(), username.length());
        username16.extract(0, username.length(),
                           reinterpret_cast<char*>(&username[0]), "UTF-8");

        //! Returned username (after conversion) is what was expected.
        EXPECT_EQ(expectedUsername, username);
    }

    //! Checks the effects of GetUserNameW on failure with invalid parameters.
    void TestInvalidParameter()
    {
        SCOPED_TRACE("");

        //! Returns FALSE on failure.
        EXPECT_EQ(FALSE, result);

        //! Sets errno to ERROR_INVALID_PARAMETER when lpBuffer is null
        //! (which is the case for an empty vector).
        EXPECT_EQ(errno, ERROR_INVALID_PARAMETER);
    }

    //! Checks the effects of GetUserNameW on failure with a buffer that is too small.
    void TestInsufficientBuffer()
    {
        SCOPED_TRACE("");

        //! Returns FALSE on failure.
        EXPECT_EQ(FALSE, result);

        //! Sets errno to ERROR_INSUFFICIENT_BUFFER.
        EXPECT_EQ(errno, ERROR_INSUFFICIENT_BUFFER);

        //! Sets lpnSize to length of username plus null.
        EXPECT_EQ(expectedSize, lpnSize);
    }
};

TEST_F(GetUserNameTest, BufferAsNullButNotBufferSize)
{
    lpnSize = 1;
    result = GetUserNameW(NULL, &lpnSize);

    TestInvalidParameter();
    // does not reset lpnSize
    EXPECT_EQ(1, lpnSize);
}

TEST_F(GetUserNameTest, BufferSizeAsNullButNotBuffer)
{
    lpBuffer.push_back('\0');
    result = GetUserNameW(&lpBuffer[0], NULL);

    TestInvalidParameter();
}

TEST_F(GetUserNameTest, BufferSizeAsZero)
{
    TestWithSize(0);
    TestInvalidParameter();
    // does not reset lpnSize
    EXPECT_EQ(0, lpnSize);
}

TEST_F(GetUserNameTest, BufferSizeAsOne)
{
    // theoretically this should never fail because any non-empty
    // username length will be >1 with trailing null
    TestWithSize(1);
    TestInsufficientBuffer();
}

TEST_F(GetUserNameTest, BufferSizeAsUsername)
{
    // the buffer is too small because it does not account for null
    TestWithSize(expectedUsername.length());
    TestInsufficientBuffer();
}

TEST_F(GetUserNameTest, BufferSizeAsUsernamePlusOne)
{
    // includes null and so should be sufficient
    TestWithSize(expectedUsername.length()+1);
    TestSuccess();
}

TEST_F(GetUserNameTest, BufferSizeAsExpectedSize)
{
    // expectedSize is the same as username.size()+1
    TestWithSize(expectedSize);
    TestSuccess();
}

TEST_F(GetUserNameTest, BufferSizeAsLoginNameMax)
{
    // LoginNameMax is big enough to hold any username, including null
    TestWithSize(LOGIN_NAME_MAX);
    TestSuccess();
}
