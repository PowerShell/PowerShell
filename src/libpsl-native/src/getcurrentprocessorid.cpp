#include "getcurrentprocessorid.h"

#include <unistd.h>

pid_t GetCurrentProcessId()
{
    return getpid();
}
