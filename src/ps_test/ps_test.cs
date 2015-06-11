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
            PlatformTests.TestIsLinux();
            PlatformTests.TestHasCom();
            PlatformTests.TestHasAmsi();

            PSTypeExtensionsTests.TestIsComObject();

            SecuritySupportTests.TestScanContent();
            SecuritySupportTests.TestCurrentDomain_ProcessExit();
            SecuritySupportTests.TestCloseSession();
            SecuritySupportTests.TestUninitialize();

            Console.WriteLine("Finished running tests");
        }
    }
}
