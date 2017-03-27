//! @file test-getfullyqualifiedname.cpp
//! @author George Fleming <v-geflem@microsoft.com>
//! @brief Unit tests for GetFullyQualifiedName

#include <gtest/gtest.h>
#include "getcomputername.h"
#include "getfullyqualifiedname.h"
#include <sys/types.h>
#include <sys/socket.h>
#include <netdb.h>
#include <string>

TEST(GetFullyQualifiedNameTest, ValidateLinuxGetFullyQualifiedDomainName)
{
    char *hostname = GetComputerName();
    ASSERT_STRNE(NULL, hostname);

    // this might be fail
    errno = 0;
    char *actual = GetFullyQualifiedName();
    int fqdnErrno = errno;

    struct addrinfo hints, *info;
    memset(&hints, 0, sizeof(hints));
    hints.ai_family = AF_UNSPEC;
    hints.ai_socktype = SOCK_STREAM;
    hints.ai_flags = AI_CANONNAME;
    errno = 0;
    if (getaddrinfo(hostname, "http", &hints, &info) != 0)
    {
        // test that getaddrinfo failed the same way
        EXPECT_EQ(fqdnErrno, errno);
        goto exit;
    }

    // Compare canonical name to FQDN
    EXPECT_STREQ(info->ai_canonname, actual);
    freeaddrinfo(info);
exit:
    free(hostname);
}
