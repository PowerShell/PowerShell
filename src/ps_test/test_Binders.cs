using Xunit;
using System;
using System.Management.Automation.Language;

namespace PSTests
{
    public static class PSEnumerableBinderTests
    {
        [Fact]
        public static void TestIsComObject()
        {
            // It just needs an arbitrary object
            Assert.False(PSEnumerableBinder.IsComObject(42));
        }
    }
}
