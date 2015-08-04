#include <gtest/gtest.h>
#include "getcomputername.h"
#include <iostream>

class GetComputerNameTest : public ::testing::Test {
protected:
    DWORD lpnSize;
    BOOL result;
    std::string expectedComputerName;
    std::string computerName;
    
    //Get expected result from using linux call
    GetComputerNameTest(){
        char hostname[HOST_MAX_NAME];
        BOOL host =  gethostname(hostname, sizeof hostname);
        expectedComputerName = hostname;
    }
    
    void TestWithSize(DWORD size) {
        lpnSize = size;
        // allocate a DWORD buffer to receive computername
        computerName.assign(lpnSize, '\0');
        result = GetComputerNameW(&computerName[0], &lpnSize);
    }
    
    void TestSuccess() {
        SCOPED_TRACE("");
        // returns 1 on success
        EXPECT_EQ(1, result);
        //Resize to cut off '\0'
        computerName.resize(expectedComputerName.length());
        ASSERT_EQ(expectedComputerName.size()+1, lpnSize);       
        EXPECT_EQ(computerName, expectedComputerName);
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
    computerName.push_back('\0');
    result = GetComputerNameW(&computerName[0], NULL);
    TestInvalidParameter();
}

TEST_F(GetComputerNameTest, BufferSizeAsZero) {
    TestWithSize(0);
    EXPECT_EQ(errno, ERROR_INSUFFICIENT_BUFFER);
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
    TestWithSize(HOST_MAX_NAME);
    TestSuccess();
}