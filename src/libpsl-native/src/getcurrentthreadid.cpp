#include "getcurrentthreadid.h"

#include <unistd.h>
#include <pthread.h>

pid_t GetCurrentThreadId()
{
		return pthread_self();
}
