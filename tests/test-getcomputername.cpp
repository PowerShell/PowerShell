//! @file test-getcomputername.cpp
//! @author Aaron Katz <v-aakatz@microsoft.com>
//! @brief Unit tests for GetComputerNameW

#include <string>
#include <unistd.h>
#include <gtest/gtest.h>
#include "getcomputername.h"

//! Test fixture for GetComputerNameTest
class GetComputerNameTest : public ::testing::Test
{
protected:
    char expectedComputerName[HOST_NAME_MAX];
    
    //Get expected result from using linux call
    GetComputerNameTest()
    {     
        BOOL ret = gethostname(expectedComputerName, HOST_NAME_MAX);
        EXPECT_EQ(0, ret);
    }
};

TEST_F(GetComputerNameTest, Success)
{
    ASSERT_STREQ(GetComputerName(), expectedComputerName);
}
