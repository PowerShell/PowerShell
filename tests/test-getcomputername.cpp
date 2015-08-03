#include <gtest/gtest.h>
#include "getcomputername.h"
#include <iostream>

TEST(GetComputerName,simple)
{
    char hostname[HOST_NAME_MAX];
    std::string hostnameFunctionTest(LOGIN_NAME_MAX, '\0');;
    DWORD hostSize = HOST_NAME_MAX;
    BOOL getComputerName = GetComputerNameW(&hostnameFunctionTest[0], &hostSize);
    BOOL host =  gethostname(hostname, sizeof hostname);

    std::string hostnameString(hostname);
    std::string hostnameStringTest(&hostnameFunctionTest[0]);
  
    ASSERT_TRUE(getComputerName == TRUE);
    ASSERT_EQ(host,0);
    ASSERT_EQ(hostnameString,hostnameStringTest);
}

TEST(GetComputerName,bufferttoosmall)
{
    std::string hostnameFunctionTest;
    DWORD hostSize = 0;
    BOOL getComputerName = GetComputerNameW(&hostnameFunctionTest[0], &hostSize);
    ASSERT_TRUE(getComputerName == 0);
    EXPECT_EQ(errno, ERROR_BUFFER_OVERFLOW);
}