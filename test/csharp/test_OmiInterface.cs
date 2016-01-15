using Xunit;
using System;
using System.Management.Automation;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace PSTests
{
    public static class OmiInterfaceTests
    {
        [Fact]
        public static void TestHostName()
        {
            const string ns = "root/omi";
            const string cn = "OMI_Identify";
            const string property = "SystemName";

            string expected = null;

            var startInfo = new ProcessStartInfo
            {
                FileName = @"/usr/bin/env",
                Arguments = "hostname",
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            using (Process process = Process.Start(startInfo))
            {
                 // Get output of call to hostname without trailing newline
                expected = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                // The process should return an exit code of 0 on success
                Assert.Equal(0, process.ExitCode);
            }

            string value = null;
            OmiInterface oi = new OmiInterface();
            oi.GetOmiValue(ns, cn, property, out value);
            Assert.Equal(expected, value);
        }

        [Fact]
        public static void TestXHugeNumberEnumerable()
        {
            const string ns = "interop";
            const string cn = "X_HugeNumber";

            OmiInterface oi = new OmiInterface();
            IEnumerable<XElement> elements;
            oi.GetOmiValues(ns, cn, out elements);
            Assert.True(elements.FirstOrDefault() != null);
        }

        [Fact]
        public static void TestXHugeNumberOmiData()
        {
            const string ns = "interop";
            const string cn = "X_HugeNumber";

            OmiInterface oi = new OmiInterface();
            
            OmiData data;
            oi.GetOmiValues(ns, cn, out data);
            Assert.True(data != null);
            Assert.Equal(data.Values.Count(), 22);
        }
    }
}
