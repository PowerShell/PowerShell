using System.Reflection;

[assembly:AssemblyFileVersionAttribute("3.0.0.0")]
[assembly:AssemblyVersion("3.0.0.0")]

#if !CORECLR
[assembly:AssemblyKeyFileAttribute(@"..\..\src\monad\monad\src\graphicalhost\visualstudiopublic.snk")]
[assembly:AssemblyDelaySignAttribute(true)]
#endif