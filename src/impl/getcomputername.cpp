#include "getcomputername.h"
#include <unistd.h>
#include <errno.h>

BOOL GetComputerName(LPTSTR name, LPDWORD len)
{
    errno = 0;
    
    int host =  gethostname(name, HOST_NAME_MAX);
    if(host == 0)
    {
        return TRUE;
    }
    else 
    {
        errno = ERROR_BUFFER_OVERFLOW;
        return FALSE;
    }
}
