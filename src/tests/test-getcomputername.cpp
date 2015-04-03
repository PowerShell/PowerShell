#include <gtest/gtest.h>
#include "getcomputername.h"

TEST(GetComputerName,simple)
{
	
	char hostname[128];
	char hostnameFunctionTest[128];

	int getComputerName = GetComputerName(hostnameFunctionTest, sizeof hostnameFunctionTest);
	int host =  gethostname(hostname, sizeof hostname);

	// first make sure that on this platform those types are of the same size
	
	ASSERT_TRUE(getComputerName == 0);
	ASSERT_TRUE(host == 0);
	
	// now compare the actual values
	for(int i =0; hostname[i] != '\0'; i++)
	{
	  ASSERT_EQ(hostnameFunctionTest[i],hostname[i]);
	}
}