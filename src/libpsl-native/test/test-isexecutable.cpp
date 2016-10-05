//! @file test-isexecutable.cpp
//! @author George Fleming <v-geflem@microsoft.com>
//! @brief Implements test for isexecutable()

#include <gtest/gtest.h>
#include <errno.h>
#include <unistd.h>
#include <sys/stat.h>
#include "isexecutable.h"

using namespace std;

class IsExecutableTest : public ::testing::Test
{
protected:

    static const int bufSize = 64;
    const string fileTemplate = "/tmp/isexecutabletest.fXXXXXXX";
    const mode_t mode_700 = S_IRUSR | S_IWUSR | S_IXUSR;
    const mode_t mode_070 = S_IRGRP | S_IWGRP | S_IXGRP;
    const mode_t mode_007 = S_IROTH | S_IWOTH | S_IXOTH;
    const mode_t mode_777 = mode_700 | mode_070 | mode_007;
    const mode_t mode_444 = S_IRUSR | S_IRGRP | S_IROTH;

    char *file;
    char fileTemplateBuf[bufSize];

    IsExecutableTest()
    {
        // since mkstemp modifies the template string, let's give it writable buffers
        strcpy(fileTemplateBuf, fileTemplate.c_str());

        // First create a file
        int fd = mkstemp(fileTemplateBuf);
        EXPECT_TRUE(fd != -1);
        file = fileTemplateBuf;
    }

    ~IsExecutableTest()
    {
        EXPECT_FALSE(unlink(file));
    }

    void ChangeFilePermission(const char* file, mode_t mode)
    {
        EXPECT_FALSE(chmod(file, mode));
    }
};

TEST_F(IsExecutableTest, FilePathNameDoesNotExist)
{
    std::string invalidFile = "/tmp/isexecutabletest_invalidFile";
    EXPECT_FALSE(IsExecutable(invalidFile.c_str()));
    EXPECT_EQ(ENOENT, errno);
}

TEST_F(IsExecutableTest, NormalFileIsNotIsexecutable)
{
    EXPECT_FALSE(IsExecutable(file));

    ChangeFilePermission(file, mode_444);

    EXPECT_FALSE(IsExecutable(file));
}

TEST_F(IsExecutableTest, FilePermission_700)
{
    ChangeFilePermission(file, mode_700);

    EXPECT_TRUE(IsExecutable(file));
}

TEST_F(IsExecutableTest, FilePermission_777)
{
    ChangeFilePermission(file, mode_777);

    EXPECT_TRUE(IsExecutable(file));
}
