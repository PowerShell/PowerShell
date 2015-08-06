#include <gtest/gtest.h>
#include "getcpinfo.h"

// This test is with correct parameters
TEST(GetCPInfo,Utf8)
{
    CPINFO cpinfo;
    BOOL result = GetCPInfoW(UTF8, cpinfo);

    // first make sure that the function worked
    ASSERT_TRUE(result == TRUE);
    
    // now compare the actual values
    ASSERT_EQ(cpinfo.DefaultChar[0],'?');
    ASSERT_EQ(cpinfo.DefaultChar[1],'0');
    ASSERT_EQ(cpinfo.LeadByte[0],'0');
    ASSERT_EQ(cpinfo.LeadByte[1],'0');
    ASSERT_EQ(cpinfo.MaxCharSize,4);
}

// This test is with codepage being null
TEST(GetCPInfo, NullForCodePageUINTButNotCpinfo)
{
    CPINFO cpinfo;
    BOOL result = GetCPInfoW(NULL, cpinfo);
    
    ASSERT_TRUE(result == FALSE);
    EXPECT_EQ(errno, ERROR_INVALID_PARAMETER);
    
}

// This test is with codepage not being utf8
TEST(GetCPInfo, CodePageNotUTF8)
{
    CPINFO cpinfo;
    BOOL result = GetCPInfoW(65000, cpinfo);
    
    ASSERT_TRUE(result == FALSE);
    EXPECT_EQ(errno, ERROR_INVALID_PARAMETER);
    
}