//! @file test-createhardlink.cpp
//! @author George Fleming <v-geflem@microsoft.com>
//! @brief Implements test for CreateHardLink()

#include <gtest/gtest.h>
#include <unistd.h>
#include "getlinkcount.h"
#include "createhardlink.h"

using namespace std;

class CreateHardLinkTest : public ::testing::Test
{
protected:

    static const int bufSize = 64;
    const string fileTemplate = "/tmp/symlinktest.fXXXXXX";
    const string dirTemplate = "/tmp/symlinktest.dXXXXXX";
    const string fileHardLink = "/tmp/symlinktest.flink";
    const string dirHardLink = "/tmp/symlinktest.dlink";
    char *file, *dir;
    char fileTemplateBuf[bufSize], dirTemplateBuf[bufSize];

    CreateHardLinkTest()
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

        // Create hard link to file
        int ret = CreateHardLink(fileHardLink.c_str(), file);
        EXPECT_EQ(ret, 0);

        // Create hard link to directory - should fail
        ret = CreateHardLink(dirHardLink.c_str(), dir);
        EXPECT_EQ(ret, -1);
    }

    ~CreateHardLinkTest()
    {
        int ret;

        ret = unlink(fileHardLink.c_str());
        EXPECT_EQ(0, ret);

        ret = unlink(file);
        EXPECT_EQ(0, ret);

        ret = rmdir(dir);
        EXPECT_EQ(0, ret);
    }
};

TEST_F(CreateHardLinkTest, FilePathNameDoesNotExist)
{
    std::string invalidFile = "/tmp/symlinktest_invalidFile";
    std::string invalidLink = "/tmp/symlinktest_invalidLink";

    // make sure neither exists
    unlink(invalidFile.c_str());
    unlink(invalidLink.c_str());

    int ret = CreateHardLink(invalidLink.c_str(), invalidFile.c_str());
    EXPECT_EQ(-1, ret);
}

TEST_F(CreateHardLinkTest, VerifyLinkCount)
{
    int count = 0;
    int ret = GetLinkCount(fileHardLink.c_str(), &count);
    EXPECT_EQ(0, ret);
    EXPECT_EQ(2, count);
}
