//! @file test-getfileowner.cpp
//! @author Andrew Schwartzmeyer <andschwa@microsoft.com>
//! @brief Tests GetFileOwner

#include <gtest/gtest.h>
#include <errno.h>
#include <unistd.h>
#include "getfileowner.h"

TEST(GetFileOwnerTest, CanGetOwnerOfRoot)
{
    EXPECT_STREQ(GetFileOwner("/"), "root");
}

TEST(GetFileOwnerTest, CannotGetOwnerOfFakeFile)
{
    EXPECT_STREQ(GetFileOwner("SomeMadeUpFileNameThatDoesNotExist"), NULL);
    EXPECT_EQ(errno, ERROR_FILE_NOT_FOUND);
}

TEST(GetFileOwnerTest, ReturnsNullForNullInput)
{
    EXPECT_STREQ(GetFileOwner(NULL), NULL);
    EXPECT_EQ(errno, ERROR_INVALID_PARAMETER);
}
