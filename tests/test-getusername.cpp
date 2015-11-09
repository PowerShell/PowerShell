//! @file test-getusername.cpp
//! @author Andrew Schwartzmeyer <andschwa@microsoft.com>
//! @brief Unit tests for GetUserName

#include <string>
#include <vector>
#include <unistd.h>
#include <gtest/gtest.h>
#include <unicode/unistr.h>
#include <pwd.h>
#include "getusername.h"

TEST(GetUserName, Success)
{
    char* expected = getpwuid(geteuid())->pw_name;
    ASSERT_TRUE(expected != NULL);
    ASSERT_EQ(GetUserName(), std::string(expected));
}
