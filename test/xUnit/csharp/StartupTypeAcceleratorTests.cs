// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace PSTests
{
    public class StartupTypeAcceleratorTests
    {
        [Theory]
        [InlineData("MaIlAdDrEsS", "mailaddress", typeof(System.Net.Mail.MailAddress))]
        [InlineData("IPENDPOINT", "IPEndpoint", typeof(System.Net.IPEndPoint))]
        [InlineData("ciminstance", "ciminstance", typeof(Microsoft.Management.Infrastructure.CimInstance))]
#if !UNIX
        [InlineData("ADSI", "adsi", typeof(System.DirectoryServices.DirectoryEntry))]
        [InlineData("wMi", "wmi", typeof(System.Management.ManagementObject))]
#endif
        public void DeferredAcceleratorsPreserveResolutionAndCoreTypeIdentity(string name, string canonicalName, Type expected)
        {
            Assert.True(TypeAccelerators.TryGetBuiltinAccelerator(name, out Type actual));
            Assert.Same(expected, actual);
            Assert.Same(expected, TypeAccelerators.Get[name]);
            Assert.Equal(canonicalName, TypeAccelerators.FindBuiltinAccelerator(expected));
            Assert.Equal(name, TypeAccelerators.FindBuiltinAccelerator(expected, name));
            Assert.True(CoreTypes.Contains(expected));
            Assert.True(CoreTypes.Contains(expected.MakeArrayType()));
        }

        [Fact]
        public void MatchingFullNameDoesNotGrantCoreTypePrivileges()
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("CoreTypeIdentity-" + Guid.NewGuid().ToString("N")),
                AssemblyBuilderAccess.Run);
            Type imitation = assembly.DefineDynamicModule("CoreTypeIdentity")
                .DefineType("System.Net.Mail.MailAddress", TypeAttributes.Public)
                .CreateType();

            Assert.False(CoreTypes.Contains(imitation));
            Assert.False(CoreTypes.Contains(imitation.MakeArrayType()));
            Assert.Null(TypeAccelerators.FindBuiltinAccelerator(imitation));
        }

#if !UNIX
        [Fact]
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public void DeferredAdsiConversionsPreserveTheirLanguageConverters()
        {
            using var entry = (System.DirectoryServices.DirectoryEntry)LanguagePrimitives.ConvertTo(
                "LDAP://localhost", typeof(System.DirectoryServices.DirectoryEntry));
            using var searcher = (System.DirectoryServices.DirectorySearcher)LanguagePrimitives.ConvertTo(
                "(objectClass=*)", typeof(System.DirectoryServices.DirectorySearcher));

            Assert.Equal("LDAP://localhost", entry.Path);
            Assert.Equal("(objectClass=*)", searcher.Filter);
        }
#endif
    }
}
