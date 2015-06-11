using System;

namespace PSTests
{
    public static class TestRunner
    {
        // TODO: Replace with xUnit test runner
        // - Add [Fact] attributes to test
        // - Remove static keywords
        static void Main()
        {
            PlatformTests.testIsLinux();
            Console.WriteLine("Finished running tests");
        }
    }
}
