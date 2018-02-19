// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//! @brief Unit tests for GetComputerName

#include <gtest/gtest.h>
#include "getcomputername.h"

//! Test fixture for GetComputerNameTest
class GetComputerNameTest : public ::testing::Test
{
};

TEST_F(GetComputerNameTest, Success)
{
    char expectedComputerName[_POSIX_HOST_NAME_MAX];

    // the gethostname system call gets the nodename from uname
    FILE *fPtr = popen("uname -n", "r");
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
