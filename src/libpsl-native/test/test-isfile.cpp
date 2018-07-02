// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//! @brief Tests Isfile

#include <gtest/gtest.h>
#include <errno.h>
#include <unistd.h>
#include "isfile.h"

TEST(IsFileTest, RootIsFile)
{
    EXPECT_FALSE(IsFile("/"));
}

TEST(IsFileTest, BinLsIsFile)
{
    EXPECT_TRUE(IsFile("/bin/ls"));
}

TEST(IsFileTest, CannotGetOwnerOfFakeFile)
{
    EXPECT_FALSE(IsFile("SomeMadeUpFileNameThatDoesNotExist"));
    EXPECT_EQ(errno, ENOENT);
}
