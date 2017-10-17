#include <syslog.h>
#include <nativesyslog.h>

extern "C" void Native_SysLog(int32_t priority, const char* message)
{
    syslog(priority, "%s", message);
}

extern "C" void Native_OpenLog(const char* ident, int facility)
{
    openlog(ident, LOG_NDELAY | LOG_PID, facility);
}

extern "C" void Native_CloseLog()
{
    closelog();
}