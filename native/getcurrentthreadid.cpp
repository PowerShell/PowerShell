#include "getcurrentthreadid.h"
#include <unistd.h>
#include <pthread.h>

HANDLE GetCurrentThreadId()
{
	pid_t tid = pthread_self();
	return reinterpret_cast<HANDLE>(tid);
}
