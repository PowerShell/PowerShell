#include <gtest/gtest.h>
#include "getcomputername.h"

TEST(GetComputerName,simple)
{	
    char hostname[HOST_NAME_MAX];
    TCHAR hostnameFunctionTest[HOST_NAME_MAX];
    DWORD hostSize = HOST_NAME_MAX;

    BOOL getComputerName = GetComputerName(hostnameFunctionTest, &hostSize);
    BOOL host =  gethostname(hostname, sizeof hostname);

    if(host == 0)
    {
      host = TRUE;    
    }
    else
    {
      host = FALSE;
    }

    std::string hostnameSting(hostname);
    std::string hostnameStingTest(hostnameFunctionTest);
  
    ASSERT_TRUE(getComputerName == TRUE);
    ASSERT_EQ(host,TRUE);
    ASSERT_EQ(hostnameSting,hostnameFunctionTest);
}