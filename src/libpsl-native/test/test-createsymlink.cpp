//! @file test-createsymlink.cpp
//! @author George Fleming <v-geflem@microsoft.com>
//! @brief Implements test for CreateSymLink() and FollowSymLink()

#include <gtest/gtest.h>
#include <errno.h>
#include <unistd.h>
#include "issymlink.h"
#include "createsymlink.h"
#include "followsymlink.h"

using namespace std;

class CreateSymLinkTest : public ::testing::Test
{
protected:

    static const int bufSize = 64;
    const string fileTemplate = "/tmp/symlinktest.fXXXXXX";
    const string dirTemplate = "/tmp/symlinktest.dXXXXXX";
    const string fileSymLink = "/tmp/symlinktest.flink";
    const string dirSymLink = "/tmp/symlinktest.dlink";
    char *file, *dir;
    char fileTemplateBuf[bufSize], dirTemplateBuf[bufSize];

    CreateSymLinkTest()
    {
        // since mkstemp and mkdtemp modifies the template string, let's give them writable buffers
        strcpy(fileTemplateBuf, fileTemplate.c_str());
        strcpy(dirTemplateBuf, dirTemplate.c_str());

        // First create a temp file
        int fd = mkstemp(fileTemplateBuf);
        EXPECT_TRUE(fd != -1);
        file = fileTemplateBuf;

        // Create a temp directory
        dir = mkdtemp(dirTemplateBuf);
        EXPECT_TRUE(dir != NULL);

        // Create symbolic link to file
        bool ret1 = CreateSymLink(fileSymLink.c_str(), file);
        EXPECT_TRUE(ret1);

        // Create symbolic link to directory
        bool ret2 = CreateSymLink(dirSymLink.c_str(), dir);
        EXPECT_TRUE(ret2);
    }

    ~CreateSymLinkTest()
    {
        bool ret;

        ret = unlink(fileSymLink.c_str());
        EXPECT_FALSE(ret);

        ret = unlink(dirSymLink.c_str());
        EXPECT_FALSE(ret);

        ret = unlink(file);
        EXPECT_FALSE(ret);

        ret = rmdir(dir);
        EXPECT_FALSE(ret);
    }
};

TEST_F(CreateSymLinkTest, FilePathNameIsNull)
{
    bool retVal = CreateSymLink(NULL, NULL);
    EXPECT_FALSE(retVal);
    EXPECT_EQ(ERROR_INVALID_PARAMETER, errno);
}

TEST_F(CreateSymLinkTest, FilePathNameDoesNotExist)
{
    std::string invalidFile = "/tmp/symlinktest_invalidFile";
    std::string invalidLink = "/tmp/symlinktest_invalidLink";

    // make sure neither exists
    unlink(invalidFile.c_str());
    unlink(invalidLink.c_str());

    // Linux allows creation of symbolic link that points to an invalid file
    bool retVal = CreateSymLink(invalidLink.c_str(), invalidFile.c_str());
    EXPECT_TRUE(retVal);

    std::string target = FollowSymLink(invalidLink.c_str());
    EXPECT_EQ(target, invalidFile);

    unlink(invalidLink.c_str());
}

TEST_F(CreateSymLinkTest, SymLinkToFile)
{
    bool retVal = IsSymLink(fileSymLink.c_str());
    EXPECT_TRUE(retVal);

    std::string target = FollowSymLink(fileSymLink.c_str());
    char buffer[PATH_MAX];
    std::string expected = realpath(file, buffer);
    EXPECT_EQ(target, expected);
}

TEST_F(CreateSymLinkTest, SymLinkToDirectory)
{
    bool retVal = IsSymLink(dirSymLink.c_str());
    EXPECT_TRUE(retVal);

    std::string target = FollowSymLink(dirSymLink.c_str());
    char buffer[PATH_MAX];
    std::string expected = realpath(dir, buffer);
    EXPECT_EQ(target, expected);
}

TEST_F(CreateSymLinkTest, SymLinkAgain)
{
    bool retVal = CreateSymLink(fileSymLink.c_str(), file);
    EXPECT_FALSE(retVal);
    EXPECT_EQ(ERROR_FILE_EXISTS, errno);
}
