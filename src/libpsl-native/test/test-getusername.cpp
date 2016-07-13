//! @file test-getusername.cpp
//! @author Andrew Schwartzmeyer <andschwa@microsoft.com>
//! @brief Unit tests for GetUserName

#include <gtest/gtest.h>
#include <pwd.h>
#include "getusername.h"

TEST(GetUserName, Success)
{
    char* expected = getpwuid(geteuid())->pw_name;
    EXPECT_STREQ(GetUserName(), expected);
}
