#include <gtest/gtest.h>
#include "common/hostutil.h"

TEST(HostUtilTest,simple)
{
    // syntactical corner cases
    ASSERT_EQ("",HostUtil::getAbsolutePathList(""));
    ASSERT_EQ("",HostUtil::getAbsolutePathList(":"));
    ASSERT_EQ("",HostUtil::getAbsolutePathList("::"));
    ASSERT_EQ("",HostUtil::getAbsolutePathList(":::::"));
 
    // current directory
    char* cwd = get_current_dir_name();
    ASSERT_EQ(std::string(cwd),HostUtil::getAbsolutePathList("."));

    // relative and absolute paths that don't exist
    ASSERT_EQ("",HostUtil::getAbsolutePathList("/something/that/does/not/exist"));
    ASSERT_EQ("",HostUtil::getAbsolutePathList(":/something/that/does/not/exist:"));
    ASSERT_EQ("",HostUtil::getAbsolutePathList("something/relative/that/does/not/exist"));
    ASSERT_EQ("",HostUtil::getAbsolutePathList(":something/relative/that/does/not/exist:"));

    // absolute existing paths
    ASSERT_EQ("/tmp",HostUtil::getAbsolutePathList("/tmp"));
    ASSERT_EQ("/tmp:/tmp",HostUtil::getAbsolutePathList("/tmp:/tmp"));
    
    // relative paths
    chdir("/");
    ASSERT_EQ("/tmp",HostUtil::getAbsolutePathList("tmp"));
    ASSERT_EQ("/tmp:/tmp",HostUtil::getAbsolutePathList("/tmp:tmp:"));
    chdir(cwd);
    
    // cleanup
    free(cwd);
}

