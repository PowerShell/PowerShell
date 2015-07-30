using Xunit;
using System;
using System.Management.Automation;

namespace PSTests
{
    public static class UtilsTests
    {
        [Fact]
        public static void TestIsWinPEHost()
        {
            Assert.False(Utils.IsWinPEHost());
        }
    }
}
