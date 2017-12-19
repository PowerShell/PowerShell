//! @file nativesyslog.cpp
//! @brief Provides wrappers around the syslog apis to support exporting
//! for PInvoke calls by powershell.
//! These functions are intended only for PowerShell internal use.
//! To view log output in real time
//! On Linux
//!    tail -f /var/log/syslog | grep powershell 
//!  On OSX
//!    sudo log stream
//!  NOTE: replace powershell with the LogIdentity value when overriding in configuration
#include <syslog.h>
#include <nativesyslog.h>

#if defined(__APPLE__) && defined(__MACH__)

#include <os/log.h>
#include <stdio.h>
static os_log_t _log = NULL;

// This format string ensures the message string for the log statements
// are visible. Using just %s marks them as private and the do not show
// up with log stream.
#define MESSAGE_FORMAT "%{public}s"

// The submodule name to pass to os_log_create.
// The passed in ident value will be used for the category.
// see man os_log_create
#define SUBMODULE_NAME "com.microsoft.powershell"

#endif

//! @brief Native_SysLog is a wrapper around the syslog api.
//! It explicitly passes the message as a parameter to a %s format
//! string since the message may have arbitrary characters that can
//! be misinterpreted as format specifiers.
//!
//! @retval none.
extern "C" void Native_SysLog(int32_t priority, const char* message)
{
#if defined(__APPLE__) && defined(__MACH__)
    switch (priority)
    {
        case LOG_EMERG:
        case LOG_ALERT:
        case LOG_CRIT:
            os_log_fault(_log, MESSAGE_FORMAT, message);
            break;
        
        case LOG_ERR:
            os_log_error(_log, MESSAGE_FORMAT, message);
            break;

        case LOG_DEBUG:
            os_log_debug(_log, MESSAGE_FORMAT, message);
            break;
        
        default:
            os_log(_log, MESSAGE_FORMAT, message);
            break;        
    }
#else
    syslog(priority, "%s", message);
#endif
}

//! @brief Native_OpenLog is a wrapper around the openlog, syslog api.
//! it allows passing an ident and facility but uses an explicit
//! option value for consistent logging across powershell instances.
//!
//! @retval none.
extern "C" void Native_OpenLog(const char* ident, int facility)
{
#if defined(__APPLE__) && defined(__MACH__)
    _log = os_log_create(SUBMODULE_NAME, ident);
#else
    openlog(ident, LOG_NDELAY | LOG_PID, facility);
#endif
}

//! @brief Native_OpenLog is a wrapper around the closelog, syslog api.
//!
//! @retval none.
extern "C" void Native_CloseLog()
{
#if defined(__APPLE__) && defined(__MACH__)
    // Nothing to do here now, for now.
#else 
    closelog();
#endif
}
