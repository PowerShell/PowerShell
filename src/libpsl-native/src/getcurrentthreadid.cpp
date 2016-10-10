#include "getcurrentthreadid.h"

#include <unistd.h>
#include <sys/types.h>
#include <sys/syscall.h>

pid_t GetCurrentThreadId()
{
    pid_t tid = 0;
#if defined(__linux__)
    tid = syscall(SYS_gettid);
#elif defined(__APPLE__) && defined(__MACH__)
    tid = syscall(SYS_thread_selfid);
#endif
    return tid;
}
