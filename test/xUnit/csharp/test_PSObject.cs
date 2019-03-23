// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Management.Automation.Language;
using Xunit;

namespace PSTests.Parallel
{
    using System.Linq;
    using System.Management.Automation;
    using System.Management.Automation.Runspaces;
    using System.Xml;

    using Microsoft.Management.Infrastructure;

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
    }
}
