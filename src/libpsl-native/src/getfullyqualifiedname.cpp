//! @file getfullyqualifiedname.cpp
//! @author George Fleming <v-geflem@microsoft.com>
//! @brief Implements GetFullyQualifiedName on Linux

#include <errno.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <netdb.h>
#include "getcomputername.h"
#include "getfullyqualifiedname.h"

//! @brief GetFullyQualifiedName retrieves the fully qualified dns name of the host
//!
//! @retval username as UTF-8 string, or null if unsuccessful
char *GetFullyQualifiedName()
{
    errno = 0;

    char *computerName = GetComputerName();
    if (computerName == NULL)
    {
        return NULL;
    }

    struct addrinfo hints, *info;

    memset(&hints, 0, sizeof hints);
    hints.ai_family = AF_UNSPEC; /*either IPV4 or IPV6*/
    hints.ai_socktype = SOCK_STREAM;
    hints.ai_flags = AI_CANONNAME;

    /* There are several ways to get the domain name:
     * uname(2), gethostbyname(3), resolver(3), getdomainname(2),
     * and getaddrinfo(3).  Some of these are not portable, some aren't
     * POSIX compliant, and some are being deprecated. getaddrinfo seems
     * to be the best choice.
     */
    char *fullName = NULL;
    if (getaddrinfo(computerName, "http", &hints, &info) != 0)
    {
        goto exit;
    }

    // return the first canonical name in the list
    fullName = strndup(info->ai_canonname, strlen(info->ai_canonname));

    // only free info if getaddrinfo was successful
    freeaddrinfo(info);
exit:
    free(computerName);
    return fullName;
}
