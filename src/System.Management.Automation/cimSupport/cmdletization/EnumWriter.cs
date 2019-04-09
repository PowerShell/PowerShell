// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.PowerShell.Cmdletization.Xml;
using System.Management.Automation;

namespace Microsoft.PowerShell.Cmdletization
{
    internal static class EnumWriter
    {
        private const string namespacePrefix = "Microsoft.PowerShell.Cmdletization.GeneratedTypes";

        private static ModuleBuilder CreateModuleBuilder()
        {
            AssemblyName aName = new AssemblyName(namespacePrefix);
            AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(aName, AssemblyBuilderAccess.Run);
            ModuleBuilder mb = ab.DefineDynamicModule(aName.Name);
            return mb;
        }

        private static Lazy<ModuleBuilder> s_moduleBuilder = new Lazy<ModuleBuilder>(CreateModuleBuilder, isThreadSafe: true);
        private static object s_moduleBuilderUsageLock = new object();

        internal static string GetEnumFullName(EnumMetadataEnum enumMetadata)
        {
            return namespacePrefix + "." + enumMetadata.EnumName;
        }

        internal static void Compile(EnumMetadataEnum enumMetadata)
        {
            string fullEnumName = GetEnumFullName(enumMetadata);

            Type underlyingType;
            if (enumMetadata.UnderlyingType != null)
            {
                underlyingType = (Type)LanguagePrimitives.ConvertTo(enumMetadata.UnderlyingType, typeof(Type), CultureInfo.InvariantCulture);
            }
            else
            {
                underlyingType = typeof(int);
            }

            ModuleBuilder mb = s_moduleBuilder.Value;
            EnumBuilder eb;
            lock (s_moduleBuilderUsageLock)
            {
                eb = mb.DefineEnum(fullEnumName, TypeAttributes.Public, underlyingType);
            }

            if (enumMetadata.BitwiseFlagsSpecified && enumMetadata.BitwiseFlags)
            {
                var cab = new CustomAttributeBuilder(typeof(FlagsAttribute).GetConstructor(Type.EmptyTypes), Array.Empty<object>());
                eb.SetCustomAttribute(cab);
            }

            foreach (var value in enumMetadata.Value)
            {
                string name = value.Name;
                object integerValue = LanguagePrimitives.ConvertTo(value.Value, underlyingType, CultureInfo.InvariantCulture);
                eb.DefineLiteral(name, integerValue);
            }

            eb.CreateTypeInfo();
        }
    }
}
