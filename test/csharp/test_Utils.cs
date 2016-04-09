using Xunit;
using System;
using System.Management.Automation;

namespace PSTests
{
    [Collection("AssemblyLoadContext")]
    public static class UtilsTests
    {
        [Fact]
        public static void TestIsWinPEHost()
        {
            Assert.False(Utils.IsWinPEHost());
        }
    }
}
