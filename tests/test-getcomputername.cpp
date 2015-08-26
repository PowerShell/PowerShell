//! @file test-getcomputername.cpp
//! @author Aaron Katz <v-aakatz@microsoft.com>
//! @brief Unit tests for GetComputerNameW

#include <string>
#include <cstring>
#include <vector>
#include <unistd.h>
#include <gtest/gtest.h>
#include <unicode/unistr.h>
#include "getcomputername.h"

//! Test fixture for GetComputerNameTest
class GetComputerNameTest : public ::testing::Test
{
protected:
    DWORD lpnSize;
    std::vector<WCHAR_T> lpBuffer;
    BOOL result;
    std::string expectedComputerName;
    DWORD expectedSize;
    
    //Get expected result from using linux call
    GetComputerNameTest()
    {     
        expectedComputerName.resize(HOST_NAME_MAX);
        BOOL ret = gethostname(&expectedComputerName[0], expectedComputerName.length());
        EXPECT_EQ(0, ret);
        expectedSize = std::strlen(expectedComputerName.c_str()) + 1;
        expectedComputerName.resize(expectedSize - 1);
    }

    //! Invokes GetComputerNameW with lpnSize and lpBuffer, saves result.
    //!
    //! @param size Assigns to lpnSize and allocates lpBuffer with
    //! size number of null characters.
    void TestWithSize(DWORD size) 
    {
        lpnSize = size;
        lpBuffer.assign(lpnSize, 0);
        result = GetComputerNameW(&lpBuffer[0], &lpnSize);
    }
    
    void TestSuccess()
    {
        SCOPED_TRACE("");
        
        //! Returns TRUE on success.
        EXPECT_EQ(TRUE, result);
        
        //! Sets lpnSize to number of WCHARs including null.
        ASSERT_EQ(expectedSize, lpnSize);
        
        // Read lpBuffer into UnicodeString (without null)
        const char* begin = reinterpret_cast<char*>(&lpBuffer[0]);
        icu::UnicodeString computername16(begin, (lpnSize-1)*sizeof(UChar), "UTF-16LE");
        // Convert to UTF-8 for comparison
        std::string computername;
        computername16.toUTF8String(computername);
        
        ASSERT_EQ(expectedSize, computername.length() + 1);
        
        //! Returned computername(after conversion) is what was expected.
        EXPECT_EQ(expectedComputerName, computername);
    }
    
    void TestInvalidParameter() 
    {
        SCOPED_TRACE("");
        
        // returns FALSE on failure
        EXPECT_EQ(FALSE, result);
        
        // sets errno to ERROR_INVALID_PARAMETER when lpBuffer is null
        EXPECT_EQ(errno, ERROR_INVALID_PARAMETER);
    }
    
    void TestInsufficientBuffer() 
    {
        SCOPED_TRACE("");
        
        // returns FALSE on failure
        EXPECT_EQ(FALSE, result);
        
        // sets errno to ERROR_INSUFFICIENT_BUFFER
        EXPECT_EQ(errno, ERROR_INSUFFICIENT_BUFFER);
        
        // sets lpnSize to length of username + null
        ASSERT_EQ(expectedSize, lpnSize); 
    }
};

TEST_F(GetComputerNameTest, BufferAsNullButNotBufferSize) 
{
    lpnSize = 1;
    result = GetComputerNameW(NULL, &lpnSize);
    TestInvalidParameter();
    // does not reset lpnSize
    EXPECT_EQ(1, lpnSize);
}

TEST_F(GetComputerNameTest, BufferSizeAsNullButNotBuffer) 
{
    lpBuffer.push_back('\0');
    result = GetComputerNameW(&lpBuffer[0], NULL);
    TestInvalidParameter();
}

TEST_F(GetComputerNameTest, BufferSizeAsZero) 
{
    TestWithSize(0);
    EXPECT_EQ(errno, ERROR_INVALID_PARAMETER);
}

TEST_F(GetComputerNameTest, BufferSizeAsUserNameMinusOne)
{  
    // the buffer is also too small
    TestWithSize(expectedSize-1);
    TestInsufficientBuffer();
}

TEST_F(GetComputerNameTest, BufferSizeAsUserNamePlusOne)
{
    // the buffer is exactly big enough
    TestWithSize(expectedSize+1);
    TestSuccess();
}

TEST_F(GetComputerNameTest, BufferSizeAsLoginNameMax) 
{
    // the buffer larger than needed
    TestWithSize(HOST_NAME_MAX);
    TestSuccess();
}