#include "getstrerror.h"

#include <assert.h>
#include <sys/types.h>
#include <errno.h>
#include <stdio.h>
#include <stdlib.h>

char* GetStrError(int errnum)
{
    size_t buflen = 256;
    char* buf = (char*)calloc(buflen, sizeof(char));

// Note that we must use strerror_r because plain strerror is not
// thread-safe.
//
// However, there are two versions of strerror_r:
//    - GNU:   char* strerror_r(int, char*, size_t);
//    - POSIX: int   strerror_r(int, char*, size_t);
//
// The former may or may not use the supplied buffer, and returns
// the error message string. The latter stores the error message
// string into the supplied buffer and returns an error code.

#if HAVE_GNU_STRERROR_R
    const char* ret = strerror_r(errnum, buf, buflen);
    assert(ret != NULL);
    if (ret != buf)
    {
        // message was returned but is static, copy for return
        strncpy(buf, ret, buflen);
    }
#else
    int ret = strerror_r(errnum, buf, buflen);
    // EINVAL: unknown error but reasonable message returned.
    // ERANGE: too small for entire message, but still filled and null-terminated.
    assert(ret == 0 || ret == EINVAL || ret == ERANGE);
#endif
    return buf;
}
