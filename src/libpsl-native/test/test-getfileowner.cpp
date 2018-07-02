// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
    EXPECT_EQ(ENOENT, errno);
}
