using Xunit;
using System;
using System.Management.Automation.Language;

namespace PSTests
{
    public static class PSEnumerableBinderTests
    {
        [Fact]
        public static void TestIsStaticTypePossiblyEnumerable()
        {
            // It just needs an arbitrary type
            Assert.False(PSEnumerableBinder.IsStaticTypePossiblyEnumerable(42.GetType()));
        }
    }
}
