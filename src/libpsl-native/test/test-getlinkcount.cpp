//! @file test-getlinkcount.cpp
//! @author George Fleming <v-geflem@microsoft.com>
//! @brief Implements test for getLinkCount()

#include <gtest/gtest.h>
#include <pwd.h>
#include <fstream>
#include <sys/stat.h>
#include <sys/types.h>
#include <errno.h>
#include <unistd.h>
#include "getlinkcount.h"

class getLinkCountTest : public ::testing::Test
{
protected:

    static const int bufSize = 64;
    const std::string fileTemplate = "/tmp/createFile.XXXXXX";
    char fileTemplateBuf[bufSize];

    int32_t count;
    char *file;

    getLinkCountTest()
    {
        // since mkstemp modifies the template string, let's give it writable buffer
        strcpy(fileTemplateBuf, fileTemplate.c_str());

        int fd = mkstemp(fileTemplateBuf);
        EXPECT_TRUE(fd != -1);
	file = fileTemplateBuf;
    }

    void createFileForTesting(const std::string &theFile)
    {
        std::ofstream ofs;
        ofs.open(theFile, std::ofstream::out);
        ofs << "hi there, ms ostc!";
        ofs.close();
    }

    std::string createHardLink(const std::string &origFile)
    {
        std::string newFile = origFile + "_link";
	int ret = link(origFile.c_str(), newFile.c_str());
        EXPECT_EQ(0, ret);

	return newFile;
    }

    void removeFile(const std::string &fileName)
    {
        int ret = unlink(fileName.c_str());
	EXPECT_EQ(0, ret);
    }
};

TEST_F(getLinkCountTest, FilePathNameIsNull)
{
    int32_t retVal = GetLinkCount(NULL, &count );
    ASSERT_FALSE(retVal);
    EXPECT_EQ(ERROR_INVALID_PARAMETER, errno);
}

TEST_F(getLinkCountTest, FilePathNameDoesNotExist)
{
    std::string invalidFile = "/tmp/createFile";
    int32_t retVal = GetLinkCount(invalidFile.c_str(), &count);
    ASSERT_FALSE(retVal);
    EXPECT_EQ(ERROR_FILE_NOT_FOUND, errno);
}

TEST_F(getLinkCountTest, LinkCountOfSinglyLinkedFile)
{
    createFileForTesting(file);
    int32_t retVal = GetLinkCount(file, &count);
    ASSERT_TRUE(retVal);
    EXPECT_EQ(1, count);

    removeFile(file);
}

TEST_F(getLinkCountTest, LinkCountOfMultiplyLinkedFile)
{
    createFileForTesting(file);
    std::string newFile = createHardLink(file);
    int32_t retVal = GetLinkCount(file, &count);
    ASSERT_TRUE(retVal);
    EXPECT_EQ(2, count);

    removeFile(file);
    removeFile(newFile);
}

