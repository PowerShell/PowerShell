//! @file test-getfullyqualifiedname.cpp
//! @author George Fleming <v-geflem@microsoft.com>
//! @brief Unit tests for GetFullyQualifiedName

#include <gtest/gtest.h>
#include "getfullyqualifiedname.h"
#include <sys/types.h>
#include <sys/socket.h>
#include <netdb.h>
#include <string>

//! Test fixture for GetComputerNameTest
class GetFullyQualifiedNameTest : public ::testing::Test
{
};

TEST_F(GetFullyQualifiedNameTest, ValidateLinuxGetFullyQualifiedDomainName)
{
    std::string actual(GetFullyQualifiedName());

    std::string hostname(_POSIX_HOST_NAME_MAX, 0);
    ASSERT_FALSE(gethostname(&hostname[0], hostname.length()));
    // trim null characters from string
    hostname = std::string(hostname.c_str());

    struct addrinfo hints, *info;
    memset(&hints, 0, sizeof(hints));
    hints.ai_family = AF_UNSPEC;
    hints.ai_socktype = SOCK_STREAM;
    hints.ai_flags = AI_CANONNAME;
    ASSERT_FALSE(getaddrinfo(hostname.c_str(), "http", &hints, &info));

    // Compare hostname part of FQDN
    ASSERT_EQ(hostname, actual.substr(0, hostname.length()));

    // Compare canonical name to FQDN
    ASSERT_EQ(info->ai_canonname, actual);

    freeaddrinfo(info);
}
