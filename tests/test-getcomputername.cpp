//! @file test-getcomputername.cpp
//! @author George Fleming <v-geflem@microsoft.com>
//! @brief Unit tests for GetComputerName

#include <gtest/gtest.h>
#include "getcomputername.h"

//! Test fixture for GetComputerNameTest
class GetComputerNameTest : public ::testing::Test
{
};

TEST_F(GetComputerNameTest, ValidateLinuxGetHostnameSystemCall)
{
    char expectedComputerName[HOST_NAME_MAX];

    //Get expected result from using linux command

    FILE *fPtr = popen("hostname -s", "r");
    ASSERT_TRUE(fPtr != NULL);

    char *linePtr = fgets(expectedComputerName, sizeof(expectedComputerName), fPtr);
    ASSERT_TRUE(linePtr != NULL);

    // There's a tendency to have \n at end of fgets string, so remove it before compare
    size_t sz = strlen(expectedComputerName);
    if (sz > 0 && expectedComputerName[sz - 1] == '\n')
    {
        expectedComputerName[sz - 1] = '\0';
    }
    pclose(fPtr);

    ASSERT_STREQ(GetComputerName(), expectedComputerName);
}
