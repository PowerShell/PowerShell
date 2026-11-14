// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Language;
using Xunit;

namespace PSTests.Parallel
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

    public static class ParameterMetadataTests
    {
        [Theory]
        [InlineData("Name")]
        [InlineData("name")]
        [InlineData("Alias")]
        [InlineData("alias")]
        [InlineData("-Name")]
        [InlineData("\u2013Alias")]
        public static void ExactNamesTakePrecedenceOverPrefixes(string name)
        {
            var metadata = CreateMetadata();
            metadata.MakeReadOnly();

            var parameter = metadata.GetMatchingParameter(name, false, true, null);

            Assert.Same(metadata.BindableParameters["Name"], parameter);
            Assert.Equal(
                "AmbiguousParameter",
                Assert.Throws<ParameterBindingException>(() => metadata.GetMatchingParameter(name, false, false, null)).ErrorId);
        }

        [Theory]
        [InlineData(false, "Name")]
        [InlineData(true, "Name")]
        [InlineData(true, "alias")]
        public static void ExactLookupDoesNotAllocate(bool readOnly, string name)
        {
            var metadata = CreateMetadata();
            if (readOnly)
            {
                metadata.MakeReadOnly();
            }

            MergedCompiledCommandParameter parameter = null;
            for (int i = 0; i < 100; i++)
            {
                parameter = metadata.GetMatchingParameter(name, false, true, null);
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++)
            {
                parameter = metadata.GetMatchingParameter(name, false, true, null);
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.Same(metadata.BindableParameters["Name"], parameter);
            Assert.Equal(0, allocated);
        }

        [Fact]
        public static void PrefixLookupPreservesAliasDeduplicationAndDeclaredParameterPreference()
        {
            var metadata = CreateMetadata();
            var name = metadata.BindableParameters["Name"];
            metadata.BindableParameters.Remove("NameLong");
            metadata.AliasedParameters.Remove("AliasLong");
            metadata.AliasedParameters.Add("Names", name);
            metadata.BindableParameters.Add(
                "NameCommon",
                new MergedCompiledCommandParameter(name.Parameter, ParameterBinderAssociation.CommonParameters));

            Assert.Same(name, metadata.GetMatchingParameter("Nam", false, true, null));
            Assert.Same(name, metadata.GetMatchingParameter("Alia", false, true, null));
            Assert.Equal(
                "AmbiguousParameter",
                Assert.Throws<ParameterBindingException>(() => metadata.GetMatchingParameter("Nam", false, false, null)).ErrorId);
            Assert.Null(metadata.GetMatchingParameter("Missing", false, true, null));
            Assert.Equal(
                "NamedParameterNotFound",
                Assert.Throws<ParameterBindingException>(() => metadata.GetMatchingParameter("Missing", true, true, null)).ErrorId);
        }

        private static MergedCommandParameterMetadata CreateMetadata()
        {
            var metadata = new MergedCommandParameterMetadata();
            foreach (string name in new[] { "Name", "NameLong" })
            {
                var parameter = new MergedCompiledCommandParameter(
                    new CompiledCommandParameter(
                        new RuntimeDefinedParameter(name, typeof(int), new Collection<Attribute>()),
                        processingDynamicParameters: false),
                    ParameterBinderAssociation.DeclaredFormalParameters);
                metadata.BindableParameters.Add(name, parameter);
                metadata.AliasedParameters.Add(name == "Name" ? "Alias" : "AliasLong", parameter);
            }

            return metadata;
        }
    }
}
