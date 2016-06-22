using Xunit;
using System;
using System.Management.Automation.Language;

namespace PSTests
{
    [Collection("AssemblyLoadContext")]
    public static class PSEnumerableBinderTests
    {
        [CiFact]
        public static void TestCiFactFound()
        {
            Assert.True(true);
        }
        [FeatureFact]
        public static void TestFeatureFactFound()
        {
            Assert.True(true);
        }
        [ScenarioFact]
        public static void TestScenarioFactFound()
        {
            Assert.True(true);
        }
    }
}
