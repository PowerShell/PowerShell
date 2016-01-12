#include "getcurrentprocessorid.h"
#include <unistd.h>

int32_t GetCurrentProcessId()
{
    pid_t pid = getpid();
    return static_cast<int32_t>(pid);
}

