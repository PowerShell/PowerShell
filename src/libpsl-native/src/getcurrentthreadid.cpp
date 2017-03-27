#include "getcurrentthreadid.h"

#include <unistd.h>
#include <sys/types.h>
#include <sys/syscall.h>
#include <pthread.h>

pid_t GetCurrentThreadId()
{
    pid_t tid = 0;
#if defined(__linux__)
    tid = syscall(SYS_gettid);
#elif defined(__APPLE__) && defined(__MACH__)
    uint64_t tid64;
    pthread_threadid_np(NULL, &tid64);
    tid = (pid_t)tid64;
#endif
    return tid;
}
