using Xunit;
using System;
using System.Management.Automation;

namespace PSTests
{
    public static class SecuritySupportTests
    {
        public static void TestScanContent()
        {
            Assert.Equal(AmsiUtils.ScanContent("", ""), AmsiUtils.AmsiNativeMethods.AMSI_RESULT.AMSI_RESULT_NOT_DETECTED);
        }

        public static void TestCurrentDomain_ProcessExit()
        {
            AmsiUtils.CurrentDomain_ProcessExit(null, EventArgs.Empty);
        }

        public static void TestCloseSession()
        {
            AmsiUtils.CloseSession();
        }

        public static void TestUninitialize()
        {
            AmsiUtils.Uninitialize();
        }
    }
}
