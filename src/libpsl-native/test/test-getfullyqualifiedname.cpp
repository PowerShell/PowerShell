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
    std::string actual(GetFullyQualifiedName());
    std::string hostname(GetComputerName());

    struct addrinfo hints, *info;
    memset(&hints, 0, sizeof(hints));
    hints.ai_family = AF_UNSPEC;
    hints.ai_socktype = SOCK_STREAM;
    hints.ai_flags = AI_CANONNAME;
    EXPECT_FALSE(getaddrinfo(hostname.c_str(), "http", &hints, &info));

    // Compare canonical name to FQDN
    EXPECT_STREQ(info->ai_canonname, actual.c_str());

    freeaddrinfo(info);

}
