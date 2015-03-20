#include "getcpinfo.h"
#include <unistd.h>

bool GetCPInfo(unsigned int codepage, LPCPINFO cpinfo)
{
  switch(codepage)
  {
    case 65000:
      	 cpinfo->DefaultChar[0]= '?';
	 cpinfo->DefaultChar[1]= '0';
	 cpinfo->LeadByte[0] = cpinfo->LeadByte[1] = '0';
	 cpinfo->MaxCharSize =  5;
	 return true;
      break;
    case 65001:
      	 cpinfo->DefaultChar[0]= '?';
	 cpinfo->DefaultChar[1]= '0';
	 cpinfo->LeadByte[0] = cpinfo->LeadByte[1] = '0';
	 cpinfo->MaxCharSize =  4;
      return true;
      break;
    default:
      return false;
  }

}

