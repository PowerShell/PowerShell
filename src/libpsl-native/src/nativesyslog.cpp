//! @file nativesyslog.cpp
//! @brief Provides wrappers around the syslog apis to support exporting
//! for PInvoke calls by powershell.
//! These functions are intended only for PowerShell internal use.
#include <syslog.h>
#include <nativesyslog.h>

//! @brief Native_SysLog is a wrapper around the syslog api.
//! It explicitly passes the message as a parameter to a %s format
//! string since the message may have arbitray characters that can
//! be misinterpreted as format specifiers.
//!
//! @retval none.
extern "C" void Native_SysLog(int32_t priority, const char* message)
{
    syslog(priority, "%s", message);
}

//! @brief Native_OpenLog is a wrapper around the openlog, syslog api.
//! it allows passing an ident and facility but uses an explicit
//! option value for consistent logging across powershell instances.
//!
//! @retval none.
extern "C" void Native_OpenLog(const char* ident, int facility)
{
    openlog(ident, LOG_NDELAY | LOG_PID, facility);
}

//! @brief Native_OpenLog is a wrapper around the closelog, syslog api.
//!
//! @retval none.
extern "C" void Native_CloseLog()
{
    closelog();
}
