#include "getcomputername.h"
#include <unistd.h>

int GetComputerName(char *name, size_t len)
{
  int host =  gethostname(name, len);
  return host;
}
