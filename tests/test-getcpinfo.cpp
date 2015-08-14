//! @file test-getcpinfo.cpp
//! @author Aaron Ktaz <v-aakatz@microsoft.com>
//! @brief Implements Unit test for GetCPInfoW

#include <gtest/gtest.h>
#include "getcpinfo.h"

// This test is with correct parameters
TEST(GetCPInfo,Utf8)
{
    CPINFO* cpinfo;
    int UTF8CodePageNumber = 65001;
    BOOL result = GetCPInfoW(UTF8CodePageNumber, cpinfo);

    // first make sure that the function worked
    ASSERT_EQ(result, TRUE);
    
    // now compare the actual values
    ASSERT_EQ(cpinfo->DefaultChar[0],'?');
    ASSERT_EQ(cpinfo->DefaultChar[1],'0');
    ASSERT_EQ(cpinfo->MaxCharSize,4);
    for(int i = 0; i < const_cpinfo::MAX_LEADBYTES; i++ ){
        ASSERT_EQ(cpinfo->LeadByte[i],'0');
    }
}

// This test is with codepage not being utf8
TEST(GetCPInfo, CodePageNotUTF8)
{
    CPINFO* cpinfo;
    BOOL result = GetCPInfoW(65000, cpinfo);
    
    ASSERT_EQ(result, FALSE);
    EXPECT_EQ(errno, ERROR_INVALID_PARAMETER);
    
}