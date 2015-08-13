//! @file test-getcomputername.cpp
//! @author Aaron Katz <v-aakatz@microsoft.com>
//! @brief Unit tests for GetComputerNameW

#include <string>
#include <vector>
#include <unistd.h>
#include <gtest/gtest.h>
#include <unicode/utypes.h>
#include <unicode/ucnv.h>
#include <unicode/ustring.h>
#include <unicode/uchar.h>
#include "getcomputername.h"

//! Test fixture for GetComputerNameTest
class GetComputerNameTest : public ::testing::Test {
protected:
    DWORD lpnSize;
    std::vector<WCHAR_T> lpBuffer;
    BOOL result;
    std::string expectedComputerName;
    DWORD expectedSize;
    
    //Get expected result from using linux call
    GetComputerNameTest(){
        char hostname[HOST_NAME_MAX];
        BOOL host =  gethostname(hostname, sizeof hostname);
        expectedComputerName = hostname;
        expectedSize = expectedComputerName.length() + 1;
    }
    
    void TestWithSize(DWORD size) {
        lpnSize = size;
        // allocate a DWORD buffer to receive computername
        lpBuffer.assign(lpnSize, '\0');
        result = GetComputerNameW(&lpBuffer[0], &lpnSize);
    }
    
    void TestSuccess() {
        SCOPED_TRACE("");
        
        //! Returns TRUE on success.
        EXPECT_EQ(TRUE, result);
        
        //! Sets lpnSize to number of WCHARs including null.
        ASSERT_EQ(expectedSize, lpnSize);
        
        // setup for conversion from UTF-16LE
        const char* begin = reinterpret_cast<char*>(&lpBuffer[0]);
        // multiply to get number of bytes
        icu::UnicodeString usercomputer16(begin, lpnSize*sizeof(char16_t), "UTF-16LE");
        // username16 length includes null and is number of characters
        ASSERT_EQ(expectedSize, usercomputer16.length());
        
        // convert (minus null) to UTF-8 for comparison
        std::string computername(lpnSize-1, 0);
        ASSERT_EQ(expectedComputerName.length(), computername.length());
        usercomputer16.extract(0, computername.length(),
                               reinterpret_cast<char*>(&computername[0]), "UTF-8");
        
        //! Returned computername(after conversion) is what was expected.
        EXPECT_EQ(expectedComputerName, computername);
    }
    
    void TestInvalidParameter() {
        SCOPED_TRACE("");
        
        // returns 0 on failure
        EXPECT_EQ(0, result);
        
        // sets errno to ERROR_INVALID_PARAMETER when lpBuffer is null
        EXPECT_EQ(errno, ERROR_INVALID_PARAMETER);
    }
    
    void TestInsufficientBuffer() {
        SCOPED_TRACE("");
        
        // returns 0 on failure
        EXPECT_EQ(0, result);
        
        // sets errno to ERROR_INSUFFICIENT_BUFFER
        EXPECT_EQ(errno, ERROR_INSUFFICIENT_BUFFER);
        
        // sets lpnSize to length of username + null
        ASSERT_EQ(expectedComputerName.size()+1, lpnSize); 
    }
};

TEST_F(GetComputerNameTest, BufferAsNullButNotBufferSize) {
    lpnSize = 1;
    result = GetComputerNameW(NULL, &lpnSize);
    TestInvalidParameter();
    // does not reset lpnSize
    EXPECT_EQ(1, lpnSize);
}

TEST_F(GetComputerNameTest, BufferSizeAsNullButNotBuffer) {
    lpBuffer.push_back('\0');
    result = GetComputerNameW(&lpBuffer[0], NULL);
    TestInvalidParameter();
}

TEST_F(GetComputerNameTest, BufferSizeAsZero) {
    TestWithSize(0);
    EXPECT_EQ(errno, ERROR_INVALID_PARAMETER);
}

TEST_F(GetComputerNameTest, BufferSizeAsUserNameMinusOne) {  
    // the buffer is also too small
    TestWithSize(expectedComputerName.size()-1);
    TestInsufficientBuffer();
}

TEST_F(GetComputerNameTest, BufferSizeAsUserNamePlusOne) {
    // the buffer is exactly big enough
    TestWithSize(expectedComputerName.size()+1);
    TestSuccess();
}

TEST_F(GetComputerNameTest, BufferSizeAsLoginNameMax) {
    // the buffer larger than needed
    TestWithSize(HOST_NAME_MAX);
    TestSuccess();
}