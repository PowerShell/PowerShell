//! @file test-getfileowner.cpp
//! @author Andrew Schwartzmeyer <andschwa@microsoft.com>
//! @brief Tests GetFileOwner

#include <gtest/gtest.h>
#include <errno.h>
#include <unistd.h>
#include "getfileowner.h"

using namespace std;

//! Test fixture for GetFileOwner
class GetFileOwnerTest : public ::testing::Test
{
};

TEST_F(GetFileOwnerTest, CanGetOwnerOfRoot)
{
    ASSERT_STREQ(GetFileOwner("/"), "root");
}

TEST_F(GetFileOwnerTest, CannotGetOwnerOfFakeFile)
{
    EXPECT_STREQ(GetFileOwner("SomeMadeUpFileNameThatDoesNotExist"), NULL);
    EXPECT_EQ(errno, ERROR_FILE_NOT_FOUND);
}

TEST_F(GetFileOwnerTest, ReturnsNullForNullInput)
{
    EXPECT_STREQ(GetFileOwner(NULL), NULL);
    EXPECT_EQ(errno, ERROR_INVALID_PARAMETER);
}
