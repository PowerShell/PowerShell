#include <gtest/gtest.h>
#include "getcpinfo.h"

// This is a very simple test case to show how tests can be written
TEST(GetCPInfo,utf8)
{
  
	LPCPINFO cpinfo = new CPINFO();
	bool utf8 = GetCPInfo(65001, cpinfo);
	

	// first make sure that on this platform those types are of the same size
	ASSERT_TRUE(utf8 == 1);
	
	// now compare the actual values
	ASSERT_EQ(cpinfo->DefaultChar[0],'?');
	ASSERT_EQ(cpinfo->DefaultChar[1],'0');
	ASSERT_EQ(cpinfo->LeadByte[0] ,'0');
	ASSERT_EQ(cpinfo->LeadByte[1] ,'0');
	ASSERT_EQ(cpinfo->MaxCharSize ,4);
	
}


TEST(GetCPInfo,utf7)
{
	LPCPINFO cpinfo = new CPINFO();
	bool utf7 = GetCPInfo(65000, cpinfo);
	

	// first make sure that on this platform those types are of the same size
	ASSERT_TRUE(utf7 == 1);
	
	// now compare the actual values
	ASSERT_EQ(cpinfo->DefaultChar[0],'?');
	ASSERT_EQ(cpinfo->DefaultChar[1],'0');
	ASSERT_EQ(cpinfo->LeadByte[0] ,'0');
	ASSERT_EQ(cpinfo->LeadByte[1] ,'0');
	ASSERT_EQ(cpinfo->MaxCharSize ,5);
}