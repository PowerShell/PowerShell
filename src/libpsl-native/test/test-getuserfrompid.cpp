//! @file test-getuserfrompid.cpp
//! @author Andrew Schwartzmeyer <andschwa@microsoft.com>
//! @brief Unit tests for GetUserFromPid

#include <gtest/gtest.h>
#include <pwd.h>
#include "getuserfrompid.h"

TEST(GetUserFromPid, Success)
{
    char* expected = getpwuid(geteuid())->pw_name;
    EXPECT_STREQ(GetUserFromPid(getpid()), expected);
}
