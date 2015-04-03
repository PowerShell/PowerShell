#include "getcpinfo.h"
#include <unistd.h>
#include <string>

BOOL GetCPInfo(UINT codepage, CPINFO &cpinfo)
{
  std::string test;
  switch(codepage)
  {
    case 65000:
      	 cpinfo.DefaultChar[0]= '?';
	 cpinfo.DefaultChar[1]= '0';
	 cpinfo.LeadByte[0] = '0';
	 cpinfo.LeadByte[1] = '0';
	 cpinfo.MaxCharSize =  5;
      return TRUE;
    case 65001:
      	 cpinfo.DefaultChar[0]= '?';
	 cpinfo.DefaultChar[1]= '0';
	 cpinfo.LeadByte[0] = '0';
	 cpinfo.LeadByte[1] = '0';
	 cpinfo.MaxCharSize =  4;
      return TRUE;
    default:
      return FALSE;
  }
}

