#include <gtest/gtest.h>
#include "getcomputername.h"

TEST(GetComputerName,simple)
{	
    char hostname[HOST_NAME_MAX];
    std::string hostnameFunctionTest;
    DWORD hostSize = HOST_NAME_MAX;
    BOOL getComputerName = GetComputerName(&hostnameFunctionTest[0], &hostSize);
    BOOL host =  gethostname(hostname, sizeof hostname);

    if(host == 0)
    {
        host = TRUE;    
    }
    else
    {
        host = FALSE;
    }

    std::string hostnameString(hostname);
    std::string hostnameStringTest(&hostnameFunctionTest[0]);
  
    ASSERT_TRUE(getComputerName == TRUE);
    ASSERT_EQ(host,TRUE);
    ASSERT_EQ(hostnameString,hostnameStringTest);
}

TEST(GetComputerName,buffertosmall)
{
    char hostname[HOST_NAME_MAX];
    std::string hostnameFunctionTest;
    DWORD hostSize = 0;
    BOOL getComputerName = GetComputerName(&hostnameFunctionTest[0], &hostSize);
    ASSERT_TRUE(getComputerName != 0);
}