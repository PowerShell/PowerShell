//! @file test-getcpinfo.cpp
//! @author Aaron Ktaz <v-aakatz@microsoft.com>
//! @brief Implements Unit test for GetCPInfoW

#include <gtest/gtest.h>
#include "getcpinfo.h"

// This test is with correct parameters
TEST(GetCPInfo,Utf8)
{
    CPINFO* cpinfo;
    int UTF8CodePageNumber = const_cpinfo::UTF8;
    BOOL result = GetCPInfoW(UTF8CodePageNumber, cpinfo);

    // first make sure that the function worked
    ASSERT_EQ(TRUE, result);
    
    // now compare the actual values
    EXPECT_EQ(cpinfo->DefaultChar[0], '?');
    EXPECT_EQ(cpinfo->DefaultChar[1], '0');
    EXPECT_EQ(cpinfo->MaxCharSize,4);
    for(int i = 0; i < const_cpinfo::MAX_LEADBYTES; i++)
    {
        EXPECT_EQ(cpinfo->LeadByte[i], '0');
    }
}

// This test is with codepage not being utf8
TEST(GetCPInfo, CodePageNotUTF8)
{
    CPINFO* cpinfo;
    BOOL result = GetCPInfoW(65000, cpinfo);
    
    ASSERT_EQ(FALSE, result);
    ASSERT_EQ(ERROR_INVALID_PARAMETER, errno);
    
}