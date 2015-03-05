#include "getcurrentprocessorid.h"
#include <unistd.h>

HANDLE GetCurrentProcessId()
{
	pid_t pid = getpid();
	return reinterpret_cast<HANDLE>(pid);
}

