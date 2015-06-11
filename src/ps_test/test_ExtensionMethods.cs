using Xunit;
using System;
using System.Management.Automation;

namespace PSTests
{
    public static class PSTypeExtensionsTests
    {
        public static void TestIsComObject()
        {
            // It just needs an arbitrary type
            Assert.False(PSTypeExtensions.IsComObject(42.GetType()));
        }
    }
}
