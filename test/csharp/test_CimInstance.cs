using Xunit;
using System;
using Microsoft.Management.Infrastructure;

namespace Microsoft.PowerShell.UnitTest
{

    public static class CimInstanceTests
    {
        // TODO: use Theory keyword to try out the different parameter options
        [Fact]
        public static void TestCimInstanceCanBeCreatedNullOptions()
        {
            var actual = new CimInstance("MyClass");
            Assert.True(null != actual);
        }
    }
}
