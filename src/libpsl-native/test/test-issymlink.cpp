//! @file test-issymlink.cpp
//! @author George Fleming <v-geflem@microsoft.com>
//! @brief Implements test for isSymLink()

#include <gtest/gtest.h>
#include <errno.h>
#include <unistd.h>
#include "issymlink.h"

using namespace std;

class isSymLinkTest : public ::testing::Test
{
protected:

    static const int bufSize = 64;
    const string fileTemplate = "/tmp/symlinktest.fXXXXXX";
    const string dirTemplate = "/tmp/symlinktest.dXXXXXX";
    const string fileSymLink = "/tmp/symlinktest.flink";
    const string dirSymLink = "/tmp/symlinktest.dlink";
    char *file, *dir;
    char fileTemplateBuf[bufSize], dirTemplateBuf[bufSize];

    isSymLinkTest()
    {
        // since mkstemp and mkdtemp modifies the template string, let's give them writable buffers
        strcpy(fileTemplateBuf, fileTemplate.c_str());
        strcpy(dirTemplateBuf, dirTemplate.c_str());

        // First create a file
        int fd = mkstemp(fileTemplateBuf);
        EXPECT_TRUE(fd != -1);
        file = fileTemplateBuf;

        // Create a temp directory
        dir = mkdtemp(dirTemplateBuf);
        EXPECT_TRUE(dir != NULL);

        // Create symbolic link to file
        EXPECT_FALSE(symlink(file, fileSymLink.c_str()));

        // Create symbolic link to directory
        EXPECT_FALSE(symlink(dir, dirSymLink.c_str()));
    }

    ~isSymLinkTest()
    {
        EXPECT_FALSE(unlink(fileSymLink.c_str()));

        EXPECT_FALSE(unlink(dirSymLink.c_str()));

        EXPECT_FALSE(unlink(file));

        EXPECT_FALSE(rmdir(dir));
    }
};

TEST_F(isSymLinkTest, FilePathNameDoesNotExist)
{
    std::string invalidFile = "/tmp/symlinktest_invalidFile";
    EXPECT_FALSE(IsSymLink(invalidFile.c_str()));
    EXPECT_EQ(ENOENT, errno);
}

TEST_F(isSymLinkTest, NormalFileIsNotSymLink)
{
    EXPECT_FALSE(IsSymLink(file));
}

TEST_F(isSymLinkTest, SymLinkToFile)
{
    EXPECT_TRUE(IsSymLink(fileSymLink.c_str()));
}

TEST_F(isSymLinkTest, NormalDirectoryIsNotSymbLink)
{
    EXPECT_FALSE(IsSymLink(dir));
}

TEST_F(isSymLinkTest, SymLinkToDirectory)
{
    EXPECT_TRUE(IsSymLink(dirSymLink.c_str()));
}
