//! @file getfullyqualifiedname.cpp
//! @author George Fleming <v-geflem@microsoft.com>
//! @brief Implements GetFullyQualifiedName on Linux

#include <errno.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <netdb.h>
#include "getcomputername.h"
#include "getfullyqualifiedname.h"

//! @brief GetFullyQualifiedName retrieves the full name of the host associated with
//! the current thread.
//!
//! @exception errno Passes these errors via errno to GetLastError:
//! - ERROR_BAD_ENVIRONMENT: locale is not UTF-8
//! - ERROR_INVALID_FUNCTION: getlogin_r() returned an unrecognized error code
//! - ERROR_INVALID_ADDRESS:  buffer is an invalid address
//! - ERROR_GEN_FAILURE: buffer not large enough
//! - ERROR_BAD_NET_NAME: Cannot determine network short name
//!
//! @retval username as UTF-8 string, or null if unsuccessful

char* GetFullyQualifiedName()
{
    errno = 0;
    
    char *computerName = GetComputerName();
    if (NULL == computerName)
    {
	return NULL;
    }

    struct addrinfo hints, *info;
    int gai_result;

    memset(&hints, 0, sizeof hints);
    hints.ai_family = AF_UNSPEC; /*either IPV4 or IPV6*/
    hints.ai_socktype = SOCK_STREAM;
    hints.ai_flags = AI_CANONNAME;

    if ((gai_result = getaddrinfo(computerName, "http", &hints, &info)) != 0) 
    {
        errno = ERROR_BAD_NET_NAME;
	return NULL;
    }

    // info is actually a link-list.  We'll just return the first full name

    char *fullName = strdup(info->ai_canonname);

    freeaddrinfo(info);
    free(computerName);
    return fullName;
}

