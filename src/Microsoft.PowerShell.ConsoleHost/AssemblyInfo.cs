using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security.Permissions;

[assembly:AssemblyCulture("")]
[assembly:NeutralResourcesLanguage("en-US")]

[assembly:AssemblyConfiguration("")]
[assembly:AssemblyInformationalVersionAttribute (@"10.0.10011.16384")]
[assembly:ReliabilityContractAttribute(Consistency.MayCorruptAppDomain, Cer.MayFail)]
[assembly:AssemblyTitle("Microsoft.PowerShell.ConsoleHost")]
[assembly:AssemblyDescription("Microsoft Windows PowerShell Console Host")]

[assembly:System.Runtime.Versioning.TargetFrameworkAttribute(".NETFramework,Version=v4.5")]
[assembly:System.Runtime.InteropServices.ComVisible(false)]
[assembly:System.Reflection.AssemblyVersion("3.0.0.0")]
[assembly:System.Reflection.AssemblyProduct("Microsoft (R) Windows (R) Operating System")]
[assembly:System.Reflection.AssemblyCopyright("Copyright (c) Microsoft Corporation. All rights reserved.")]
[assembly:System.Reflection.AssemblyCompany("Microsoft Corporation")]
[assembly:System.Reflection.AssemblyFileVersion("10.0.10011.16384")]
[assembly:AssemblyKeyFileAttribute(@"..\..\src\monad\monad\src\graphicalhost\visualstudiopublic.snk")]
[assembly:System.Reflection.AssemblyDelaySign(true)]

internal static class AssemblyStrings
{
    internal const string AssemblyVersion = @"3.0.0.0";
    internal const string AssemblyCopyright = "Copyright (C) 2006 Microsoft Corporation. All rights reserved.";
}
