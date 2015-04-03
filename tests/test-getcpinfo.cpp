#include <gtest/gtest.h>
#include "getcpinfo.h"

// This is a very simple test case to show how tests can be written
TEST(GetCPInfo,utf8)
{
	CPINFO cpinfo;
	BOOL utf8 = GetCPInfo(65001, cpinfo);
	

	// first make sure that the function worked
	ASSERT_TRUE(utf8 == TRUE);
	
	// now compare the actual values
	ASSERT_EQ(cpinfo.DefaultChar[0],'?');
	ASSERT_EQ(cpinfo.DefaultChar[1],'0');
	ASSERT_EQ(cpinfo.LeadByte[0] ,'0');
	ASSERT_EQ(cpinfo.LeadByte[1] ,'0');
	ASSERT_EQ(cpinfo.MaxCharSize ,4);
	
}


TEST(GetCPInfo,utf7)
{
	CPINFO cpinfo;
	BOOL utf7 = GetCPInfo(65000, cpinfo);
	

	// first make sure that the function worked
	ASSERT_TRUE(utf7 == TRUE);
	
	// now compare the actual values
	ASSERT_EQ(cpinfo.DefaultChar[0],'?');
	ASSERT_EQ(cpinfo.DefaultChar[1],'0');
	ASSERT_EQ(cpinfo.LeadByte[0] ,'0');
	ASSERT_EQ(cpinfo.LeadByte[1] ,'0');
	ASSERT_EQ(cpinfo.MaxCharSize ,5);
}