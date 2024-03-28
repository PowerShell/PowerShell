// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation.Remoting;
using Microsoft.PowerShell.Commands;
using Xunit;

namespace PSTests.Parallel
{
    public class RemotingUtilsTests
    {
        private readonly NewPSSessionCommand cmdlet;

        public RemotingUtilsTests()
        {
            cmdlet = new NewPSSessionCommand();
        }

        [Fact]
        public void TestParseSshHostname()
        {
            string hostname = "foo.bar.com";
            RemotingUtils.ParseSshHostName(cmdlet, hostname, out string host, out string userName, out int port);
            Assert.Equal(hostname, host);
            Assert.Null(userName);
            Assert.Equal(0, port);
        }

        [Fact]
        public void TestParseSshHostnameWithPort()
        {
            string hostname = "foo.bar.com:22";
            RemotingUtils.ParseSshHostName(cmdlet, hostname, out string host, out string userName, out int port);
            Assert.Equal("foo.bar.com", host);
            Assert.Null(userName);
            Assert.Equal(22, port);
        }

        [Fact]
        public void TestParseSshHostnameWithUsername()
        {
            string hostname = "user@foo.bar.com";
            RemotingUtils.ParseSshHostName(cmdlet, hostname, out string host, out string userName, out int port);
            Assert.Equal("foo.bar.com", host);
            Assert.Equal("user", userName);
            Assert.Equal(0, port);
        }

        [Fact]
        public void TestParseSshHostnameWithUsernameAndPort()
        {
            string hostname = "user@foo.bar.com:22";
            RemotingUtils.ParseSshHostName(cmdlet, hostname, out string host, out string userName, out int port);
            Assert.Equal("foo.bar.com", host);
            Assert.Equal("user", userName);
            Assert.Equal(22, port);
        }

        [Fact]
        public void TestParseSshHostnameCaseInsensitiveHost()
        {
            string hostname = "Foo.Bar.com";
            RemotingUtils.ParseSshHostName(cmdlet, hostname, out string host, out string userName, out int port);
            Assert.Equal(hostname, host);
            Assert.Null(userName);
            Assert.Equal(0, port);
        }

        [Fact]
        public void TestParseSshHostnameThrowsInvalidHostnameException()
        {
            string hostname = "foo.bar..";
            Assert.Throws<ArgumentException>(() => RemotingUtils.ParseSshHostName(cmdlet, hostname, out string host, out string userName, out int port));
        }
    }
}
