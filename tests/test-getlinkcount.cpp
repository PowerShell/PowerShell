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

    std::string file;
    const std::string fileTemplate = "/tmp/createFile.XXXXXXX";
    ulong count;

    getLinkCountTest()
    {
        file = fileTemplate;
        file = mktemp(const_cast<char*>(file.c_str()));        
        std::ifstream fileCondition(file);
        EXPECT_EQ(0, fileCondition.good());
    }

    void createFileForTesting(std::string theFile)
    {
        std::ofstream ofs;
        ofs.open(theFile, std::ofstream::out);
        ofs << "hi there, ms ostc!";
        ofs.close();
    }

    std::string createHardLink(std::string origFile)
    {
        std::string newFile = origFile + "_link";
	int ret = link(const_cast<char*>(origFile.c_str()), const_cast<char*>(newFile.c_str()));
        EXPECT_EQ(0, ret);

	return newFile;
    }

    void removeFile(std::string fileName)
    {
        int ret = unlink(const_cast<char*>(fileName.c_str()));
	EXPECT_EQ(0, ret);
    }
};

TEST_F(getLinkCountTest, FilePathNameIsNull) 
{
    bool retVal = GetLinkCount(NULL, &count );
    ASSERT_FALSE(retVal);
    EXPECT_EQ(ERROR_INVALID_PARAMETER, errno);
}

TEST_F(getLinkCountTest, FilePathNameDoesNotExist) 
{
    std::string invalidFile = "/tmp/createFile";
    bool retVal = GetLinkCount(const_cast<char*>(invalidFile.c_str()), &count);
    ASSERT_FALSE(retVal);
    EXPECT_EQ(ERROR_FILE_NOT_FOUND, errno);
}

TEST_F(getLinkCountTest, LinkCountOfSinglyLinkedFile)
{
    createFileForTesting(file);
    bool retVal = GetLinkCount(const_cast<char*>(file.c_str()), &count);
    ASSERT_TRUE(retVal);
    EXPECT_EQ(1, count);

    removeFile(file);
}

TEST_F(getLinkCountTest, LinkCountOfMultipliLinkedFile)
{
    createFileForTesting(file);
    std::string newFile = createHardLink(file);
    bool retVal = GetLinkCount(const_cast<char*>(file.c_str()), &count);
    ASSERT_TRUE(retVal);
    EXPECT_EQ(2, count);

    removeFile(file);
    removeFile(newFile);
}

