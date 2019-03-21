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
            var psObject = new PSObject();
            var actual = psObject.GetFirstPropertyOrDefault(name => true);
            Assert.Null(actual);
        }

        [Fact]
        public static void TestWrappedDateTimeHasReflectedMember()
        {
            var psObject = new PSObject(DateTime.Now);
            var member = psObject.GetFirstPropertyOrDefault(name => name == "DayOfWeek");
            Assert.NotNull(member);
            Assert.Equal("DayOfWeek", member.Name);
        }

        [Fact]
        public static void TestAdaptedMember()
        {
            var psObject = new PSObject(DateTime.Now);
            psObject.Members.Add(new PSNoteProperty("NewMember", "AValue"));
            var member = psObject.GetFirstPropertyOrDefault(name => name == "NewMember");
            Assert.NotNull(member);
            Assert.Equal("NewMember", member.Name);
        }

        [Fact]
        public static void TestShadowedMember()
        {
            var psObject = new PSObject(DateTime.Now);
            psObject.Members.Add(new PSNoteProperty("DayOfWeek", "AValue"));
            var member = psObject.GetFirstPropertyOrDefault(name => name == "DayOfWeek");
            Assert.NotNull(member);
            Assert.Equal("DayOfWeek", member.Name);
            Assert.Equal("AValue", member.Value);
        }

        [Fact]
        public static void TestMemberSetIsNotProperty()
        {
            var psObject = new PSObject(DateTime.Now);
            var psNoteProperty = new PSNoteProperty("NewMember", "AValue");
            psObject.Members.Add(psNoteProperty);
            psObject.Members.Add(new PSMemberSet("NewMemberSet", new[] { psNoteProperty }));

            var member = psObject.GetFirstPropertyOrDefault(name => name == "NewMemberSet");
            Assert.Null(member);
        }

        [Fact]
        public static void TestMemberSet()
        {
            var psObject = new PSObject(DateTime.Now);
            var psNoteProperty = new PSNoteProperty("NewMember", "AValue");
            psObject.Members.Add(psNoteProperty);
            psObject.Members.Add(new PSMemberSet("NewMemberSet", new[] { psNoteProperty }));

            var member = psObject.Members.FirstOrDefault(name => name == "NewMemberSet");
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

            var psObject = new PSObject(root);
            var member = psObject.GetFirstPropertyOrDefault(name => name.StartsWith("elem"));
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

            var psObject = new PSObject(root);
            var member = psObject.GetFirstPropertyOrDefault(name => name.StartsWith("attr"));
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
