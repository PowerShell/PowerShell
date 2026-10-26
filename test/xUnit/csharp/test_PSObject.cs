// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Xml;
using Microsoft.Management.Infrastructure;
using Xunit;

namespace PSTests.Parallel
{
    public static class PSObjectTests
    {
        [Fact]
        public static void TestEmptyObjectHasNoProperty()
        {
            var pso = new PSObject();
            var actual = pso.GetFirstPropertyOrDefault(name => true);
            Assert.Null(actual);
        }

        [Fact]
        public static void TestWrappedDateTimeHasReflectedMember()
        {
            var pso = new PSObject(DateTime.Now);
            var member = pso.GetFirstPropertyOrDefault(name => name == "DayOfWeek");
            Assert.NotNull(member);
            Assert.Equal("DayOfWeek", member.Name);
        }

        [Fact]
        public static void TestAdaptedMember()
        {
            var pso = new PSObject(DateTime.Now);
            pso.Members.Add(new PSNoteProperty("NewMember", "AValue"));
            var member = pso.GetFirstPropertyOrDefault(name => name == "NewMember");
            Assert.NotNull(member);
            Assert.Equal("NewMember", member.Name);
        }

        [Fact]
        public static void TestShadowedMember()
        {
            var pso = new PSObject(DateTime.Now);
            pso.Members.Add(new PSNoteProperty("DayOfWeek", "AValue"));
            var member = pso.GetFirstPropertyOrDefault(name => name == "DayOfWeek");
            Assert.NotNull(member);
            Assert.Equal("DayOfWeek", member.Name);
            Assert.Equal("AValue", member.Value);
        }

        [Fact]
        public static void TestMemberSetIsNotProperty()
        {
            var pso = new PSObject(DateTime.Now);
            var psNoteProperty = new PSNoteProperty("NewMember", "AValue");
            pso.Members.Add(psNoteProperty);
            pso.Members.Add(new PSMemberSet("NewMemberSet", new[] { psNoteProperty }));

            var member = pso.GetFirstPropertyOrDefault(name => name == "NewMemberSet");
            Assert.Null(member);
        }

        [Fact]
        public static void TestMemberSet()
        {
            var pso = new PSObject(DateTime.Now);
            var psNoteProperty = new PSNoteProperty("NewMember", "AValue");
            pso.Members.Add(psNoteProperty);
            pso.Members.Add(new PSMemberSet("NewMemberSet", new[] { psNoteProperty }));

            var member = pso.Members.FirstOrDefault(name => name == "NewMemberSet");
            Assert.NotNull(member);
            Assert.Equal("NewMemberSet", member.Name);
        }

        [Fact]
        public static void TextXmlElementMember()
        {
            var doc = new XmlDocument();
            var root = doc.CreateElement("root");
            doc.AppendChild(root);
            var firstChild = doc.CreateElement("elem1");
            root.AppendChild(firstChild);
            root.InsertAfter(doc.CreateElement("elem2"), firstChild);

            var pso = new PSObject(root);
            var member = pso.GetFirstPropertyOrDefault(name => name.StartsWith("elem"));
            Assert.Equal("elem1", member.Name);
        }

        [Fact]
        public static void TextXmlAttributeMember()
        {
            var doc = new XmlDocument();
            var root = doc.CreateElement("root");
            doc.AppendChild(root);
            root.SetAttribute("attr", "value");
            root.AppendChild(doc.CreateElement("elem"));

            var pso = new PSObject(root);
            var member = pso.GetFirstPropertyOrDefault(name => name.StartsWith("attr"));
            Assert.Equal("attr", member.Name);
        }

        [SkippableFact]
        public static void TestCimInstanceProperty()
        {
            Skip.IfNot(Platform.IsWindows);
            var iss = InitialSessionState.CreateDefault2();
            iss.Commands.Add(new SessionStateCmdletEntry("Get-CimInstance", typeof(Microsoft.Management.Infrastructure.CimCmdlets.GetCimInstanceCommand), null));
            using (var ps = PowerShell.Create(iss))
            {
                ps.AddCommand("Get-CimInstance").AddParameter("ClassName", "Win32_BIOS");
                var res = ps.Invoke().FirstOrDefault();
                Assert.NotNull(res);
                var member = res.GetFirstPropertyOrDefault(name => name == "Name");
                Assert.NotNull(member);
            }
        }

        [Fact]
        public static void MetadataAccessorsAreTypedOrderedAndCachedWithoutEagerMembers()
        {
            TestMetadata.AccessCount = 0;
            var pso = NewMetadataObject(presenceMask: 0);
            var sourceField = typeof(PSMemberInfoInternalCollection<PSMemberInfo>)
                .GetField("_providerSource", BindingFlags.Instance | BindingFlags.NonPublic);
            var dictionaryField = typeof(PSMemberInfoInternalCollection<PSMemberInfo>)
                .GetField("_members", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(sourceField.GetValue(pso.InstanceMembers));
            Assert.Null(dictionaryField.GetValue(pso.InstanceMembers));
            Assert.True(pso.TryGetInstanceValue("fIrSt", out object value));
            Assert.Equal("value", value);
            Assert.Equal(1, TestMetadata.AccessCount);
            Assert.False(pso.ContainsInstanceMember("Second"));
            Assert.Equal("First", pso.InstanceMembers[0].Name);
            Assert.Equal("BaseOnly", pso.InstanceMembers[1].Name);
            Assert.Equal(PSMemberTypes.NoteProperty, pso.InstanceMembers["first"].MemberType);
            Assert.Null(sourceField.GetValue(pso.InstanceMembers));
        }

        [Fact]
        public static void MetadataInspectionMaterializesAllAndMutationUsesOrdinaryNotes()
        {
            var pso = NewMetadataObject(presenceMask: 1);
            Assert.Equal(3, pso.InstanceMembers.Count);
            pso.InstanceMembers.Add(new PSNoteProperty("Third", 3));
            Assert.Null(typeof(PSMemberInfoInternalCollection<PSMemberInfo>)
                .GetField("_providerSource", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(pso.InstanceMembers));
            Assert.Equal("value", pso.InstanceMembers["FIRST"].Value);
            Assert.Equal(42, pso.InstanceMembers["Second"].Value);
            Assert.Equal(3, pso.InstanceMembers["third"].Value);
            pso.InstanceMembers.Remove("Second");
            Assert.Null(pso.InstanceMembers["Second"]);
        }

        [Fact]
        public static void MetadataInstanceMembersSurviveResurrectionAndCopy()
        {
            object baseObject = new object();
            var pso = NewMetadataObject(baseObject);
            Assert.True(PSObject.HasInstanceMembers(baseObject, out var resurrected));
            Assert.True(resurrected.TryGetValue("First", out object value));
            Assert.Equal("value", value);
            dynamic dynamicObject = pso;
            Assert.Equal("value", (string)dynamicObject.First);

            var copy = pso.Copy();
            Assert.Equal("value", copy.Members["First"].Value);
        }

        [Fact]
        public static void MetadataMembersSurviveNestedWrappingAndCachedDynamicReads()
        {
            object baseObject = new object();
            var pso = NewMetadataObject(baseObject);
            var nested = new PSObject(new PSObject(pso));
            Assert.Equal("value", PSObject.AsPSObject(baseObject).Members["First"].Value);
            dynamic dynamicObject = nested;
            Assert.Equal("value", (string)dynamicObject.First);
        }

        [Fact]
        public static void MetadataCopyOwnsIndependentOrdinaryNotesAndSerializes()
        {
            var pso = NewMetadataObject("content");
            var copy = pso.Copy();
            copy.Members["First"].Value = "copy";
            Assert.Equal("value", pso.Members["First"].Value);

            var roundTrip = PSObject.AsPSObject(PSSerializer.Deserialize(PSSerializer.Serialize(pso)));
            Assert.Equal("value", roundTrip.Members["First"].Value);
        }

        [Fact]
        public static void MetadataSourcePromotesForMatchingMutationAndCollision()
        {
            var pso = NewMetadataObject(presenceMask: 1);
            Assert.Equal("Second", pso.InstanceMembers.Match("Sec*", PSMemberTypes.NoteProperty)[0].Name);
            pso.InstanceMembers["First"].Value = "updated";
            pso.InstanceMembers.Remove("Second");
            Assert.Null(pso.InstanceMembers["Second"]);
            pso.InstanceMembers.Add(new PSNoteProperty("Added", 3));
            Assert.Equal(3, pso.InstanceMembers["added"].Value);

            object baseObject = new object();
            var ordinary = new PSObject(baseObject);
            ordinary.Members.Add(new PSNoteProperty("First", "old"));
            var metadata = new TestMetadata { First = "new", Second = 5 };
            var promoted = new PSObject<TestMetadata>(baseObject, metadata);
            promoted.AttachProviderInstanceMembers(promoted);
            Assert.Equal("new", promoted.Members["first"].Value);
        }

        [Fact]
        public static void MetadataConstructionCostsLessThanEagerNoteProperties()
        {
            for (int i = 0; i < 32; i++)
            {
                NewMetadataObject(presenceMask: 1);
                var eager = new PSObject(new object());
                eager.Members.Add(new PSNoteProperty("First", "value"));
                eager.Members.Add(new PSNoteProperty("Second", 42));
                eager.Members.Add(new PSNoteProperty("BaseOnly", "Object"));
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long metadataStart = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 128; i++)
            {
                NewMetadataObject(presenceMask: 1);
            }

            long metadataBytes = GC.GetAllocatedBytesForCurrentThread() - metadataStart;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long noteStart = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 128; i++)
            {
                var eager = new PSObject(new object());
                eager.Members.Add(new PSNoteProperty("First", "value"));
                eager.Members.Add(new PSNoteProperty("Second", 42));
                eager.Members.Add(new PSNoteProperty("BaseOnly", "Object"));
            }

            long noteBytes = GC.GetAllocatedBytesForCurrentThread() - noteStart;
            Assert.True(metadataBytes < noteBytes, $"Metadata source: {metadataBytes} bytes; note properties: {noteBytes} bytes.");
        }

        [Fact]
        public static void MetadataAccessorValidationRejectsNameCollisionsAndInvalidSignatures()
        {
            Assert.Throws<TypeInitializationException>(() =>
            {
                var duplicate = new PSObject<DuplicateMetadata>(new object(), default);
                duplicate.AttachProviderInstanceMembers(duplicate);
                duplicate.ContainsInstanceMember("Same");
            });

            Assert.Throws<TypeInitializationException>(() =>
            {
                var invalid = new PSObject<InvalidMetadata>(new object(), new InvalidMetadata(4));
                invalid.AttachProviderInstanceMembers(invalid);
                invalid.ContainsInstanceMember("Bad");
            });
        }

        [Fact]
        public static void DirectDynamicAndAdaptedReadsDoNotPromoteMetadataSource()
        {
            TestMetadata.AccessCount = 0;
            var pso = NewMetadataObject(new System.IO.FileInfo("example.txt"));
            Assert.Equal("value", ReadFirstAtCachedCallSite(pso));
            Assert.Equal("example.txt", ReadNameAtCachedCallSite(pso));
            Assert.Equal(1, TestMetadata.AccessCount);

            Assert.Null(pso.InstanceMembers["AbsentMember"]);
            Assert.Null(pso.InstanceMembers["ToString"]);
            Assert.NotNull(typeof(PSMemberInfoInternalCollection<PSMemberInfo>)
                .GetField("_providerSource", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(pso.InstanceMembers));
            Assert.Null(typeof(PSMemberInfoInternalCollection<PSMemberInfo>)
                .GetField("_members", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(pso.InstanceMembers));
        }

        [Fact]
        public static void CachedReadCallSiteHandlesOrdinaryAndPromotedMembers()
        {
            var pso = NewMetadataObject();
            var ordinary = new PSObject(new object());
            ordinary.Members.Add(new PSNoteProperty("First", "ordinary"));

            Assert.Equal("value", ReadFirstAtCachedCallSite(pso));
            Assert.Equal("ordinary", ReadFirstAtCachedCallSite(ordinary));
            Assert.Equal("value", ReadFirstAtCachedCallSite(pso));

            pso.Members["First"].Value = "promoted";
            Assert.Equal("promoted", ReadFirstAtCachedCallSite(pso));
            Assert.Equal("ordinary", ReadFirstAtCachedCallSite(ordinary));
            Assert.Equal("promoted", ReadFirstAtCachedCallSite(pso));
        }

        [Fact]
        public static void ProviderSourceAndItsResurrectionCycleDoNotKeepBaseAlive()
        {
            WeakReference weakReference = CreateMetadataWeakReference();
            for (int i = 0; i < 5 && weakReference.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            Assert.False(weakReference.IsAlive);
        }

        [SkippableFact]
        public static void FileSystemProviderDirectReadsDoNotPromoteMetadataSource()
        {
            Skip.IfNot(ExperimentalFeature.IsEnabled(ExperimentalFeature.PSOptimizedProviderObjects));
            string path = System.IO.Path.GetTempFileName();
            try
            {
                using (var runspace = RunspaceFactory.CreateRunspace())
                {
                    runspace.Open();
                    using (var powerShell = PowerShell.Create())
                    {
                        powerShell.Runspace = runspace;
                        string escapedPath = path.Replace("'", "''");
                        powerShell.AddScript($"$item = Get-Item -LiteralPath '{escapedPath}'; $item");
                        PSObject output = powerShell.Invoke<PSObject>().Single();
                        Assert.Empty(powerShell.Streams.Error);

                        FieldInfo sourceField = typeof(PSMemberInfoInternalCollection<PSMemberInfo>)
                            .GetField("_providerSource", BindingFlags.Instance | BindingFlags.NonPublic);
                        FieldInfo membersField = typeof(PSMemberInfoInternalCollection<PSMemberInfo>)
                            .GetField("_members", BindingFlags.Instance | BindingFlags.NonPublic);
                        Assert.NotNull(sourceField
                            .GetValue(output.InstanceMembers));
                        Assert.Null(membersField
                            .GetValue(output.InstanceMembers));

                        powerShell.Commands.Clear();
                        powerShell.AddScript("$item.PSPath; $item.PSDrive; $item.PSChildName");
                        var directValues = powerShell.Invoke<PSObject>();
                        Assert.Empty(powerShell.Streams.Error);
                        Assert.Equal(3, directValues.Count);
                        Assert.Contains("FileSystem::", (string)directValues[0].BaseObject);
                        Assert.NotNull(directValues[1].BaseObject);
                        Assert.Equal(System.IO.Path.GetFileName(path), (string)directValues[2].BaseObject);
                        Assert.NotNull(sourceField.GetValue(output.InstanceMembers));
                        Assert.Null(membersField.GetValue(output.InstanceMembers));

                        powerShell.Commands.Clear();
                        powerShell.AddScript("$item | ForEach-Object -MemberName FullName");
                        var fullNames = powerShell.Invoke<string>();
                        Assert.Empty(powerShell.Streams.Error);
                        Assert.Contains(path, fullNames);
                        Assert.NotNull(sourceField.GetValue(output.InstanceMembers));
                        Assert.Null(membersField.GetValue(output.InstanceMembers));

                        powerShell.Commands.Clear();
                        powerShell.AddScript("$item | ForEach-Object -MemberName psparentpath");
                        var parentPaths = powerShell.Invoke<string>();
                        Assert.Empty(powerShell.Streams.Error);
                        string expectedParentPath = $"Microsoft.PowerShell.Core\\FileSystem::{System.IO.Path.GetDirectoryName(path)}";
                        Assert.Equal(expectedParentPath, parentPaths.Single());
                        Assert.NotNull(sourceField.GetValue(output.InstanceMembers));
                        Assert.Null(membersField.GetValue(output.InstanceMembers));

                        powerShell.Commands.Clear();
                        powerShell.AddScript("$item | ForEach-Object -MemberName psparentpath -WhatIf");
                        powerShell.Invoke();
                        Assert.Empty(powerShell.Streams.Error);
                        Assert.NotNull(sourceField.GetValue(output.InstanceMembers));
                        Assert.Null(membersField.GetValue(output.InstanceMembers));

                        powerShell.Commands.Clear();
                        powerShell.AddScript("$item | Add-Member -NotePropertyName Added -NotePropertyValue value; $item | ForEach-Object -MemberName pSParentPath");
                        var mutatedParentPaths = powerShell.Invoke<string>();
                        Assert.Empty(powerShell.Streams.Error);
                        Assert.Equal(expectedParentPath, mutatedParentPaths.Single());

                        Assert.Contains("FileSystem::", (string)output.Properties["PSPath"].Value);
                        Assert.Null(sourceField.GetValue(output.InstanceMembers));
                    }
                }
            }
            finally
            {
                System.IO.File.Delete(path);
            }
        }

        private static PSObject<TestMetadata> NewMetadataObject(object baseObject = null, uint presenceMask = 0)
        {
            var metadata = new TestMetadata { First = "value", Second = 42 };
            var result = new PSObject<TestMetadata>(baseObject ?? new object(), metadata, presenceMask);
            result.AttachProviderInstanceMembers(result);
            return result;
        }

        private static string ReadFirstAtCachedCallSite(dynamic value) => value.First;

        private static string ReadNameAtCachedCallSite(dynamic value) => value.Name;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static WeakReference CreateMetadataWeakReference()
        {
            var baseObject = new object();
            _ = NewMetadataObject(baseObject);
            return new WeakReference(baseObject);
        }

        private struct TestMetadata
        {
            internal static int AccessCount;
            internal string First;
            internal int Second;

            [PSObjectMetadataAccessor("First", 0)]
            private static string GetFirst(object baseObject, in TestMetadata metadata)
            {
                AccessCount++;
                return metadata.First;
            }

            [PSObjectMetadataAccessor("Second", 1, 1)]
            private static int GetSecond(object baseObject, in TestMetadata metadata)
            {
                AccessCount++;
                return metadata.Second;
            }

            [PSObjectMetadataAccessor("BaseOnly", 2)]
            private static string GetBaseOnly(object baseObject) => baseObject.GetType().Name;
        }

        private struct DuplicateMetadata
        {
            [PSObjectMetadataAccessor("Same", 0)]
            private static int GetFirst(object baseObject) => 1;

            [PSObjectMetadataAccessor("same", 1)]
            private static int GetSecond(object baseObject) => 2;
        }

        private struct InvalidMetadata
        {
            private int _value;

            internal InvalidMetadata(int value) => _value = value;

            [PSObjectMetadataAccessor("Bad", 0)]
            private int GetInstanceValue(object baseObject) => _value;
        }
    }
}
