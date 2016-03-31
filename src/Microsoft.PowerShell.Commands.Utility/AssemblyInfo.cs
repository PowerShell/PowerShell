using System.Reflection;
using System.Resources;

[assembly:AssemblyFileVersionAttribute("3.0.0.0")]
[assembly:AssemblyVersion("3.0.0.0")]

[assembly:AssemblyCulture("")]
[assembly:NeutralResourcesLanguage("en-US")]

#if !CORECLR
[assembly:AssemblyKeyFileAttribute(@"..\signing\visualstudiopublic.snk")]
[assembly:AssemblyDelaySignAttribute(true)]
#endif
