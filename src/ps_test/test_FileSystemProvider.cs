using Xunit;
using System;
using System.Management.Automation;
using Microsoft.PowerShell.Commands;

namespace PSTests
{
    public static class FileSystemProviderTests
    {
        [Fact]
        public static void TestCreateJunctionFails()
        {
            Assert.False(InternalSymbolicLinkLinkCodeMethods.CreateJunction("",""));
        }
    }
}
