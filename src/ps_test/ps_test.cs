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
            PlatformTests.TestUsesCodeSignedAssemblies();
            PlatformTests.TestHasDriveAutoMounting();
            PlatformTests.TestHasRegistrySupport();

            PSTypeExtensionsTests.TestIsComObject();

            PSEnumerableBinderTests.TestIsComObject();

            SecuritySupportTests.TestScanContent();
            SecuritySupportTests.TestCurrentDomain_ProcessExit();
            SecuritySupportTests.TestCloseSession();
            SecuritySupportTests.TestUninitialize();

            MshSnapinInfoTests mshSnapinInfoTests = new MshSnapinInfoTests();
            mshSnapinInfoTests.TestReadRegistryInfo();
            mshSnapinInfoTests.TestReadCoreEngineSnapIn();

            Console.WriteLine("Finished running tests");
        }
    }
}
