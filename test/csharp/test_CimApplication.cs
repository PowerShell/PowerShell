using Xunit;
using System;
using Microsoft.Management.Infrastructure.Internal;

namespace Microsoft.PowerShell.UnitTest
{

    public static class CimApplicationTests
    {
        [Fact]
        public static void TestCimApplicationHandleCanBeCreated()
        {
            Assert.True(null != CimApplication.Handle);
        }

    }
}
