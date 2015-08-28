//! @file test-getcpinfo.cpp
//! @author Aaron Ktaz <v-aakatz@microsoft.com>
//! @brief Implements Unit test for GetCPInfoW

#include <gtest/gtest.h>
#include "getcpinfo.h"

TEST(GetCPInfo, CodePageIsUTF8)
{
    CPINFO cpinfo;
    BOOL result = GetCPInfoW(const_cpinfo::UTF8, &cpinfo);

    // first make sure that the function worked
    ASSERT_EQ(TRUE, result);

    // now compare the actual values
    EXPECT_EQ(cpinfo.DefaultChar[0], '?');
    EXPECT_EQ(cpinfo.DefaultChar[1], '0');
    EXPECT_EQ(cpinfo.MaxCharSize, 4);

    for (int i = 0; i < const_cpinfo::MAX_LEADBYTES; i++)
    {
        EXPECT_EQ(cpinfo.LeadByte[i], '0');
    }
}

TEST(GetCPInfo, CodePageIsNotUTF8)
{
    CPINFO cpinfo;
    BOOL result = GetCPInfoW(const_cpinfo::UTF8+1, &cpinfo);

    ASSERT_EQ(FALSE, result);
    ASSERT_EQ(ERROR_INVALID_PARAMETER, errno);
}
