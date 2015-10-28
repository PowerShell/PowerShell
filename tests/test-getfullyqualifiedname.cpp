//! @file test-getfullyqualifiedname.cpp
//! @author George Fleming <v-geflem@microsoft.com>
//! @brief Unit tests for GetFullyQualifiedName

#include <stdio.h>
#include <gtest/gtest.h>
#include "getfullyqualifiedname.h"

//! Test fixture for GetComputerNameTest
class GetFullyQualifiedNameTest : public ::testing::Test
{
};

TEST_F(GetFullyQualifiedNameTest, ValidateLinuxGetFullyQualifiedDomainName)
{
    char expectedComputerName[HOST_NAME_MAX];
    
    //Get expected result from using linux command

    FILE *fPtr = popen("/bin/hostname --fqdn", "r");
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

    ASSERT_STREQ(GetFullyQualifiedName(), expectedComputerName);
}
