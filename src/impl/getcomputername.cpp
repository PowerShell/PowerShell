#include "getcomputername.h"
#include <unistd.h>
#include <errno.h>

BOOL GetComputerName(LPTSTR name, LPDWORD len)
{
    errno = 0;
    size_t len2 = *len; 
    int host =  gethostname(name, len2);
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
