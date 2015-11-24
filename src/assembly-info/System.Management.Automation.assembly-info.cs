using System.Runtime.CompilerServices;
using System.Reflection;

[assembly:InternalsVisibleTo("Microsoft.PowerShell.Commands.Management")]
[assembly:InternalsVisibleTo("Microsoft.PowerShell.Commands.Utility")]
[assembly:InternalsVisibleTo("Microsoft.PowerShell.Security")]
[assembly:InternalsVisibleTo("Microsoft.PowerShell.Linux.Host")]
[assembly:InternalsVisibleTo("Microsoft.PowerShell.Linux.UnitTests")]
[assembly:InternalsVisibleTo("powershell")]
[assembly:AssemblyFileVersionAttribute("1.0.0.0")]
[assembly:AssemblyVersion("1.0.0.0")]


namespace System.Management.Automation
{
	internal class NTVerpVars
	{
		internal const int PRODUCTMAJORVERSION = 10;
		internal const int PRODUCTMINORVERSION = 0;
		internal const int PRODUCTBUILD        = 10032;
		internal const int PRODUCTBUILD_QFE    = 0;
		internal const int PACKAGEBUILD_QFE    = 814;
	}
}

