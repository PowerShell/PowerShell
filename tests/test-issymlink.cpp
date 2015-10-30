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
    const string fileTemplate = "/tmp/symlinktest.fXXXXXXX";
    const string dirTemplate = "/tmp/symlinktest.dXXXXXXX";
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
	int ret1 = symlink(file, fileSymLink.c_str());
	EXPECT_EQ(ret1, 0);
        
        // Create symbolic link to directory
	int ret2 = symlink(dir, dirSymLink.c_str());
	EXPECT_EQ(ret2, 0);
    }

    ~isSymLinkTest()
    {
        int ret;

        ret = unlink(fileSymLink.c_str());
	EXPECT_EQ(0, ret);

        ret = unlink(dirSymLink.c_str());
	EXPECT_EQ(0, ret);

        ret = unlink(file);
	EXPECT_EQ(0, ret);

	ret = rmdir(dir);
	EXPECT_EQ(0, ret);       
    }
};

TEST_F(isSymLinkTest, FilePathNameIsNull)
{
    int retVal = IsSymLink(NULL);
    EXPECT_EQ(retVal, -1);
    EXPECT_EQ(ERROR_INVALID_PARAMETER, errno);
}

TEST_F(isSymLinkTest, FilePathNameDoesNotExist)
{
    std::string invalidFile = "/tmp/symlinktest_invalidFile";
    int retVal = IsSymLink(invalidFile.c_str());
    EXPECT_EQ(retVal, -1);
    EXPECT_EQ(ERROR_FILE_NOT_FOUND, errno);
}

TEST_F(isSymLinkTest, NormalFileIsNotSymLink)
{
    int retVal = IsSymLink(file);
    EXPECT_EQ(0, retVal);
}

TEST_F(isSymLinkTest, SymLinkToFile)
{
    int retVal = IsSymLink(fileSymLink.c_str());
    EXPECT_EQ(1, retVal);
}

TEST_F(isSymLinkTest, NormalDirectoryIsNotSymbLink)
{
    int retVal = IsSymLink(dir);
    EXPECT_EQ(0, retVal);
}

TEST_F(isSymLinkTest, SymLinkToDirectory)
{
    int retVal = IsSymLink(dirSymLink.c_str());
    EXPECT_EQ(1, retVal);
}

