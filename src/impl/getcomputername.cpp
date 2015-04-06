#include "getcomputername.h"
#include <unistd.h>

BOOL GetComputerName(LPTSTR name, LPDWORD len)
{
  int host =  gethostname(name, HOST_NAME_MAX);
  if(host == 0)
  {
    return TRUE;
  }  
  return FALSE;
}
