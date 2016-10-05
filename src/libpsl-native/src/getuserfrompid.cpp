#include "pal.h"
#include "getfileowner.h"
#include "getpwuid.h"
#include "getuserfrompid.h"

#include <string>
#include <sstream>
#include <errno.h>
#include <sys/sysctl.h>

char* GetUserFromPid(pid_t pid)
{

#if defined(__linux__)

    // Get effective owner of pid from procfs
    std::stringstream ss;
    ss << "/proc/" << pid;
    std::string path;
    ss >> path;

    return GetFileOwner(path.c_str());

#elif defined(__APPLE__) && defined(__MACH__)

    // Get effective owner of pid from sysctl
    struct kinfo_proc oldp;
    size_t oldlenp = sizeof(oldp);
    int name[] = {CTL_KERN, KERN_PROC, KERN_PROC_PID, pid};
    u_int namelen = sizeof(name)/sizeof(int);

    // Read-only query
    int ret = sysctl(name, namelen, &oldp, &oldlenp, NULL, 0);
    if (ret != 0 || oldlenp == 0)
    {
        return NULL;
    }

    return GetPwUid(oldp.kp_eproc.e_ucred.cr_uid);

#else

    return NULL;

#endif

}
