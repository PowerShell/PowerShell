//! @file test-isdirectory.cpp
//! @author Andrew Schwartzmeyer <andschwa@microsoft.com>
//! @brief Tests IsDirectory

#include <gtest/gtest.h>
#include <errno.h>
#include <unistd.h>
#include "isdirectory.h"

TEST(IsDirectoryTest, RootIsDirectory)
{
    EXPECT_TRUE(IsDirectory("/"));
}

TEST(IsDirectoryTest, BinLsIsNotDirectory)
{
    EXPECT_FALSE(IsDirectory("/bin/ls"));
}


TEST(IsDirectoryTest, ReturnsFalseForFakeDirectory)
{
    EXPECT_FALSE(IsDirectory("SomeMadeUpFileNameThatDoesNotExist"));
    EXPECT_EQ(ENOENT, errno);
}
