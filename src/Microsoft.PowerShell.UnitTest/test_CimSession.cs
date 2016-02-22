using Xunit;
using System;

namespace Microsoft.PowerShell.UnitTest
{

    public static class CimSessionTests
    {
        // TODO: use Theory keyword to try out the different parameter options
        [Fact]
        public static void TestCimSessionCanBeCreatedNullOptions()
        {
            Assert.True(null != Microsoft.Management.Infrastructure.CimSession.Create("localhost"));
        }
    }
}
