// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//! @brief Unit tests for GetUserFromPid

#include <gtest/gtest.h>
#include <pwd.h>
#include "getuserfrompid.h"

TEST(GetUserFromPid, Success)
{
    char* expected = getpwuid(geteuid())->pw_name;
    EXPECT_STREQ(GetUserFromPid(getpid()), expected);
}
