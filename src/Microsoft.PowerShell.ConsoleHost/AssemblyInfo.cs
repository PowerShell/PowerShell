using System.Reflection;
using System.Runtime.CompilerServices;
#if !CORECLR
using System.Runtime.ConstrainedExecution;
#else
using System.Resources;
#endif

[assembly: InternalsVisibleTo("powershell-tests,PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")]

#if CORECLR
[assembly: AssemblyCulture("")]
[assembly: NeutralResourcesLanguage("en-US")]
#else
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyInformationalVersionAttribute(@"10.0.10011.16384")]
[assembly: ReliabilityContractAttribute(Consistency.MayCorruptAppDomain, Cer.MayFail)]
[assembly: AssemblyTitle("Microsoft.PowerShell.ConsoleHost")]
[assembly: AssemblyDescription("Microsoft Windows PowerShell Console Host")]

[assembly: System.Runtime.Versioning.TargetFrameworkAttribute(".NETFramework,Version=v4.5")]
[assembly: System.Reflection.AssemblyFileVersion("10.0.10011.16384")]
#endif

[assembly: System.Runtime.InteropServices.ComVisible(false)]
[assembly: System.Reflection.AssemblyVersion("3.0.0.0")]
[assembly: System.Reflection.AssemblyProduct("Microsoft (R) Windows (R) Operating System")]
[assembly: System.Reflection.AssemblyCopyright("Copyright (c) Microsoft Corporation. All rights reserved.")]
[assembly: System.Reflection.AssemblyCompany("Microsoft Corporation")]

internal static class AssemblyStrings
{
    internal const string AssemblyVersion = @"3.0.0.0";
    internal const string AssemblyCopyright = "Copyright (C) 2006 Microsoft Corporation. All rights reserved.";
}
