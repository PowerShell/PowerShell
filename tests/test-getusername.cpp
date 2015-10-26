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

//! Test fixture for GetUserName
class GetUserNameTest : public ::testing::Test
{
protected:
    std::string UserName;

    GetUserNameTest(): UserName(std::string(getpwuid(geteuid())->pw_name))
    {
    }
};

TEST_F(GetUserNameTest, Success)
{
    ASSERT_EQ(GetUserName(), UserName);
}
