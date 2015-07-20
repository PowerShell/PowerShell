using System.Runtime.CompilerServices;
using System.Reflection;


[assembly:InternalsVisibleTo("Microsoft.PowerShell.Commands.Management")]
[assembly:InternalsVisibleTo("Microsoft.PowerShell.Commands.Utility")]
[assembly:InternalsVisibleTo("Microsoft.PowerShell.Security")]
[assembly:InternalsVisibleTo("Microsoft.PowerShell.CoreCLR.AssemblyLoadContext")]
[assembly:InternalsVisibleTo("ps_test")]
[assembly:AssemblyFileVersionAttribute("1.0.0.0")]


//
// PH: (TODO linux)
// all the following code comes from the original AssemblyInfo.cs file for PowerShell,
// most of it is commented out, but at the bottom there is a type that is needed to build
// this dll



/*

[assembly: AssemblyConfiguration("")]
[assembly: AssemblyInformationalVersionAttribute (@"10.0.10011.0")]



















[module: SuppressMessage("Microsoft.Design", "CA1014:MarkAssembliesWithClsCompliant")]






[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope="member", Target="Microsoft.PowerShell.ConsoleControl.#UpdateLocaleSpecificFont()", MessageId="System.Runtime.InteropServices.SafeHandle.DangerousGetHandle")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope="member", Target="System.Management.Automation.Runspaces.RunspaceConfigForSingleShell.LoadMshSnapinAssembly(System.Management.Automation.PSSnapInInfo):System.Reflection.Assembly", MessageId="System.Reflection.Assembly.LoadFrom")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope="member", Target="System.Management.Automation.ExecutionContext.LoadAssembly(System.Management.Automation.Runspaces.AssemblyConfigurationEntry,System.Exception&):System.Reflection.Assembly", MessageId="System.Reflection.Assembly.LoadFrom")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope="member", Target="Microsoft.PowerShell.ConsoleControl.SetConsoleScreenBufferSize(Microsoft.Win32.SafeHandles.SafeFileHandle,System.Management.Automation.Host.Size):System.Void", MessageId="System.Runtime.InteropServices.SafeHandle.DangerousGetHandle")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope="member", Target="Microsoft.PowerShell.ConsoleControl.WriteConsoleOutputPlain(Microsoft.Win32.SafeHandles.SafeFileHandle,System.Management.Automation.Host.Coordinates,System.Management.Automation.Host.BufferCell[,]):System.Void", MessageId="System.Runtime.InteropServices.SafeHandle.DangerousGetHandle")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope="member", Target="Microsoft.PowerShell.ConsoleControl.FillConsoleOutputAttribute(Microsoft.Win32.SafeHandles.SafeFileHandle,System.UInt16,System.Int32,System.Management.Automation.Host.Coordinates):System.Void", MessageId="System.Runtime.InteropServices.SafeHandle.DangerousGetHandle")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope="member", Target="Microsoft.PowerShell.ConsoleControl.GetConsoleScreenBufferInfo(Microsoft.Win32.SafeHandles.SafeFileHandle):Microsoft.PowerShell.ConsoleControl+CONSOLE_SCREEN_BUFFER_INFO", MessageId="System.Runtime.InteropServices.SafeHandle.DangerousGetHandle")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope="member", Target="Microsoft.PowerShell.ConsoleControl.SetConsoleCursorInfo(Microsoft.Win32.SafeHandles.SafeFileHandle,Microsoft.PowerShell.ConsoleControl+CONSOLE_CURSOR_INFO):System.Void", MessageId="System.Runtime.InteropServices.SafeHandle.DangerousGetHandle")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope="member", Target="Microsoft.PowerShell.ConsoleControl.PeekConsoleInput(Microsoft.Win32.SafeHandles.SafeFileHandle,Microsoft.PowerShell.ConsoleControl+INPUT_RECORD[]&):System.Int32", MessageId="System.Runtime.InteropServices.SafeHandle.DangerousGetHandle")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope="member", Target="Microsoft.PowerShell.ConsoleControl.SetMode(Microsoft.Win32.SafeHandles.SafeFileHandle,Microsoft.PowerShell.ConsoleControl+ConsoleModes):System.Void", MessageId="System.Runtime.InteropServices.SafeHandle.DangerousGetHandle")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope="member", Target="Microsoft.PowerShell.ConsoleControl.GetNumberOfConsoleInputEvents(Microsoft.Win32.SafeHandles.SafeFileHandle):System.Int32", MessageId="System.Runtime.InteropServices.SafeHandle.DangerousGetHandle")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope="member", Target="Microsoft.PowerShell.ConsoleControl.FlushConsoleInputBuffer(Microsoft.Win32.SafeHandles.SafeFileHandle):System.Void", MessageId="System.Runtime.InteropServices.SafeHandle.DangerousGetHandle")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope="member", Target="Microsoft.PowerShell.ConsoleControl.SetConsoleWindowInfo(Microsoft.Win32.SafeHandles.SafeFileHandle,System.Boolean,Microsoft.PowerShell.ConsoleControl+SMALL_RECT):System.Void", MessageId="System.Runtime.InteropServices.SafeHandle.DangerousGetHandle")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope="member", Target="Microsoft.PowerShell.ConsoleControl.SetConsoleTextAttribute(Microsoft.Win32.SafeHandles.SafeFileHandle,System.UInt16):System.Void", MessageId="System.Runtime.InteropServices.SafeHandle.DangerousGetHandle")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope="member", Target="Microsoft.PowerShell.ConsoleControl.WriteConsoleOutputCJK(Microsoft.Win32.SafeHandles.SafeFileHandle,System.Management.Automation.Host.Coordinates,System.Management.Automation.Host.Rectangle,System.Management.Automation.Host.BufferCell[,],Microsoft.PowerShell.ConsoleControl+BufferCellArrayRowType):System.Void", MessageId="System.Runtime.InteropServices.SafeHandle.DangerousGetHandle")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope="member", Target="Microsoft.PowerShell.ConsoleControl.ReadConsoleOutputPlain(Microsoft.Win32.SafeHandles.SafeFileHandle,System.Management.Automation.Host.Coordinates,System.Management.Automation.Host.Rectangle,System.Management.Automation.Host.BufferCell[,]&):System.Void", MessageId="System.Runtime.InteropServices.SafeHandle.DangerousGetHandle")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope="member", Target="Microsoft.PowerShell.ConsoleControl.ReadConsoleOutputCJKSmall(Microsoft.Win32.SafeHandles.SafeFileHandle,System.UInt32,System.Management.Automation.Host.Coordinates,System.Management.Automation.Host.Rectangle,System.Management.Automation.Host.BufferCell[,]&):System.Boolean", MessageId="System.Runtime.InteropServices.SafeHandle.DangerousGetHandle")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope="member", Target="Microsoft.PowerShell.ConsoleControl.ScrollConsoleScreenBuffer(Microsoft.Win32.SafeHandles.SafeFileHandle,Microsoft.PowerShell.ConsoleControl+SMALL_RECT,Microsoft.PowerShell.ConsoleControl+SMALL_RECT,Microsoft.PowerShell.ConsoleControl+COORD,Microsoft.PowerShell.ConsoleControl+CHAR_INFO):System.Void", MessageId="System.Runtime.InteropServices.SafeHandle.DangerousGetHandle")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope="member", Target="Microsoft.PowerShell.ConsoleControl.GetConsoleCursorInfo(Microsoft.Win32.SafeHandles.SafeFileHandle):Microsoft.PowerShell.ConsoleControl+CONSOLE_CURSOR_INFO", MessageId="System.Runtime.InteropServices.SafeHandle.DangerousGetHandle")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope="member", Target="Microsoft.PowerShell.ConsoleControl.GetConsoleFontInfo(Microsoft.Win32.SafeHandles.SafeFileHandle):Microsoft.PowerShell.ConsoleControl+CONSOLE_FONT_INFO_EX", MessageId="System.Runtime.InteropServices.SafeHandle.DangerousGetHandle")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope="member", Target="Microsoft.PowerShell.ConsoleControl.ReadConsoleInput(Microsoft.Win32.SafeHandles.SafeFileHandle,Microsoft.PowerShell.ConsoleControl+INPUT_RECORD[]&):System.Int32", MessageId="System.Runtime.InteropServices.SafeHandle.DangerousGetHandle")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope="member", Target="Microsoft.PowerShell.ConsoleControl.ReadConsole(Microsoft.Win32.SafeHandles.SafeFileHandle,System.String,System.Int32,System.Boolean,System.UInt32&):System.String", MessageId="System.Runtime.InteropServices.SafeHandle.DangerousGetHandle")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope="member", Target="Microsoft.PowerShell.ConsoleControl.SetConsoleCursorPosition(Microsoft.Win32.SafeHandles.SafeFileHandle,System.Management.Automation.Host.Coordinates):System.Void", MessageId="System.Runtime.InteropServices.SafeHandle.DangerousGetHandle")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope="member", Target="Microsoft.PowerShell.ConsoleControl.GetLargestConsoleWindowSize(Microsoft.Win32.SafeHandles.SafeFileHandle):System.Management.Automation.Host.Size", MessageId="System.Runtime.InteropServices.SafeHandle.DangerousGetHandle")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope="member", Target="Microsoft.PowerShell.ConsoleControl.WriteConsole(Microsoft.Win32.SafeHandles.SafeFileHandle,System.String):System.Void", MessageId="System.Runtime.InteropServices.SafeHandle.DangerousGetHandle")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope="member", Target="Microsoft.PowerShell.ConsoleControl.GetMode(Microsoft.Win32.SafeHandles.SafeFileHandle):Microsoft.PowerShell.ConsoleControl+ConsoleModes", MessageId="System.Runtime.InteropServices.SafeHandle.DangerousGetHandle")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope="member", Target="Microsoft.PowerShell.ConsoleControl.FillConsoleOutputCharacter(Microsoft.Win32.SafeHandles.SafeFileHandle,System.Char,System.Int32,System.Management.Automation.Host.Coordinates):System.Void", MessageId="System.Runtime.InteropServices.SafeHandle.DangerousGetHandle")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope="member", Target="System.Management.Automation.MakeKit.VersionChecker.LoadAssemblyFrom(System.String,System.Boolean):System.Reflection.Assembly", MessageId="System.Reflection.Assembly.LoadFrom")]
[module: SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope="member", Target="System.Management.Automation.MakeKit.AssemblyAnalyzer.LoadAssembly(System.String):System.Reflection.Assembly", MessageId="System.Reflection.Assembly.LoadFrom")]


[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.Commands.HtmlWebResponseObject..ctor(System.Net.WebResponse,System.Management.Automation.ExecutionContext)")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.Commands.ExportAliasCommand.ThrowFileOpenError(System.Exception,System.String):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.Commands.InputFileOpenModeConversion.Convert(Microsoft.PowerShell.Commands.OpenMode):System.IO.FileMode")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.Commands.Internal.Format.InnerFormatShapeCommand.WriteInternalErrorMessage(System.String):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.Commands.CertificateNotFoundException..ctor(System.Exception)")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.Commands.CertificateStoreLocationNotFoundException..ctor(System.Exception)")]

[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.Commands.CertificateProviderCodeSigningDynamicParameters.set_CodeSigningCert(System.Management.Automation.SwitchParameter):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.Commands.CertificateProviderDynamicParameters.set_CodeSigningCert(System.Management.Automation.SwitchParameter):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.Commands.CertificateProviderDynamicParameters.set_DnsName(System.Management.Automation.SwitchParameter):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.Commands.CertificateProviderDynamicParameters.set_Eku(System.Management.Automation.SwitchParameter):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.Commands.CertificateProviderDynamicParameters.set_ExpiringIn(System.Management.Automation.SwitchParameter):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.Commands.ProviderRemoveItemDynamicParameters.set_DeleteKey(System.Management.Automation.SwitchParameter):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.Commands.CertificateStoreNotFoundException..ctor(System.Exception)")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.Commands.CertificateProviderItemNotFoundException..ctor(System.Exception)")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.Commands.RegistryProvider.GetIndexFromAt(System.Object):System.Int32")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Management.Automation.Runspaces.MshConsoleInfo.set_PSVersion(System.Version):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Management.Automation.Runspaces.RunspaceConfigForSingleShell.GetProperty(System.Object,System.String):System.String")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Management.Automation.PSInstaller.get_WriteToFile():System.Boolean")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Management.Automation.PSInstaller.get_RegFile():System.String")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Management.Automation.Parser+NumericConstantNode..ctor(System.Management.Automation.Token,System.Object)")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Management.Automation.PSSnapInInfo.get_VendorIndirect():System.String")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Management.Automation.PSSnapInInfo.get_DescriptionIndirect():System.String")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Management.Automation.NativeCommandProcessor.GetBinaryTypeA(System.String,System.Int32&):System.Int32")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Management.Automation.EventLogLogProvider.GetMessageDllPath(System.String):System.String")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Management.Automation.MamlNode.IsEmptyLine(System.String):System.Boolean")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Management.Automation.MamlNode.GetMinIndentation(System.String[]):System.Int32")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Management.Automation.MamlNode.GetPreformattedText(System.String):System.String")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Management.Automation.MamlNode.TrimLines(System.String[]):System.String[]")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Management.Automation.MamlNode.GetIndentation(System.String):System.Int32")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Management.Automation.Parser+CharacterTokenReader..ctor(System.String,System.Boolean)")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.Commands.Internal.Format.PSObjectHelper.GetSmartToStringDisplayName(System.Object,Microsoft.PowerShell.Commands.Internal.Format.MshExpressionFactory):System.String")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.Commands.Internal.Format.TypeMatch.get_ActiveTracer():System.Management.Automation.PSTraceSource")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Management.Automation.Internal.ObjectStream.DFT_AddHandler_OnDataReady(System.EventHandler):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Management.Automation.Internal.ObjectStream.DFT_RemoveHandler_OnDataReady(System.EventHandler):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHost+ConsoleColorProxy.get_DebugBackgroundColor():System.ConsoleColor")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHost+ConsoleColorProxy.set_DebugBackgroundColor(System.ConsoleColor):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHost+ConsoleColorProxy.set_ErrorBackgroundColor(System.ConsoleColor):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHost+ConsoleColorProxy.get_ErrorBackgroundColor():System.ConsoleColor")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHost+ConsoleColorProxy.get_WarningBackgroundColor():System.ConsoleColor")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHost+ConsoleColorProxy.set_WarningBackgroundColor(System.ConsoleColor):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHost+ConsoleColorProxy.set_ErrorForegroundColor(System.ConsoleColor):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHost+ConsoleColorProxy.get_ErrorForegroundColor():System.ConsoleColor")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHost+ConsoleColorProxy.set_ProgressForegroundColor(System.ConsoleColor):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHost+ConsoleColorProxy.get_ProgressForegroundColor():System.ConsoleColor")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHost+ConsoleColorProxy.set_DebugForegroundColor(System.ConsoleColor):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHost+ConsoleColorProxy.get_DebugForegroundColor():System.ConsoleColor")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHost+ConsoleColorProxy.get_VerboseForegroundColor():System.ConsoleColor")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHost+ConsoleColorProxy.set_VerboseForegroundColor(System.ConsoleColor):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHost+ConsoleColorProxy.get_WarningForegroundColor():System.ConsoleColor")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHost+ConsoleColorProxy.set_WarningForegroundColor(System.ConsoleColor):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHost+ConsoleColorProxy.get_ProgressBackgroundColor():System.ConsoleColor")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHost+ConsoleColorProxy.set_ProgressBackgroundColor(System.ConsoleColor):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHost+ConsoleColorProxy.set_VerboseBackgroundColor(System.ConsoleColor):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHost+ConsoleColorProxy.get_VerboseBackgroundColor():System.ConsoleColor")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHost+ConsoleHostStartupException..ctor(System.String)")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHost.WriteErrorLine(System.String):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHost+InputLoop.get_RunningLoopCount():System.Int32")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHostUserInterface.get_DebugBackgroundColor():System.ConsoleColor")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHostUserInterface.set_DebugBackgroundColor(System.ConsoleColor):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHostUserInterface.set_WarningForegroundColor(System.ConsoleColor):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHostUserInterface.set_ProgressBackgroundColor(System.ConsoleColor):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHostUserInterface.set_ErrorForegroundColor(System.ConsoleColor):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHostUserInterface.get_ErrorForegroundColor():System.ConsoleColor")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHostUserInterface.set_ProgressForegroundColor(System.ConsoleColor):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHostUserInterface.set_VerboseBackgroundColor(System.ConsoleColor):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHostUserInterface.get_VerboseBackgroundColor():System.ConsoleColor")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHostUserInterface.get_VerboseForegroundColor():System.ConsoleColor")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHostUserInterface.set_VerboseForegroundColor(System.ConsoleColor):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHostUserInterface.set_DebugForegroundColor(System.ConsoleColor):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHostUserInterface.get_DebugForegroundColor():System.ConsoleColor")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHostUserInterface.set_WarningBackgroundColor(System.ConsoleColor):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHostUserInterface.set_ErrorBackgroundColor(System.ConsoleColor):System.Void")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHostUserInterface.get_ErrorBackgroundColor():System.ConsoleColor")]

[module: SuppressMessage("Microsoft.Interoperability", "CA1404:CallGetLastErrorImmediatelyAfterPInvoke", Scope="member", Target="Microsoft.PowerShell.ConsoleControl.GetActiveScreenBufferHandle():Microsoft.Win32.SafeHandles.SafeFileHandle")]
[module: SuppressMessage("Microsoft.Interoperability", "CA1404:CallGetLastErrorImmediatelyAfterPInvoke", Scope="member", Target="Microsoft.PowerShell.ConsoleControl.ReadConsoleOutputCJK(Microsoft.Win32.SafeHandles.SafeFileHandle,System.UInt32,System.Management.Automation.Host.Coordinates,System.Management.Automation.Host.Rectangle,System.Management.Automation.Host.BufferCell[,]&):System.Void")]
[module: SuppressMessage("Microsoft.Interoperability", "CA1404:CallGetLastErrorImmediatelyAfterPInvoke", Scope="member", Target="Microsoft.PowerShell.ConsoleControl.GetInputHandle():Microsoft.Win32.SafeHandles.SafeFileHandle")]
[module: SuppressMessage("Microsoft.Interoperability", "CA1404:CallGetLastErrorImmediatelyAfterPInvoke", Scope="member", Target="Microsoft.PowerShell.ConsoleControl.LengthInBufferCellsFE(System.Char,System.IntPtr&,System.IntPtr&,System.Boolean&,Microsoft.PowerShell.ConsoleControl+TEXTMETRIC&):System.Int32")]
[module: SuppressMessage("Microsoft.Interoperability", "CA1404:CallGetLastErrorImmediatelyAfterPInvoke", Scope="member", Target="Microsoft.PowerShell.Commands.SetServiceCommand.ProcessRecord():System.Void")]
[module: SuppressMessage("Microsoft.Interoperability", "CA1404:CallGetLastErrorImmediatelyAfterPInvoke", Scope="member", Target="Microsoft.PowerShell.Commands.NewServiceCommand.BeginProcessing():System.Void")]

[module: SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", Scope="member", Target="Microsoft.PowerShell.Commands.ImportXmlHelper.Dispose():System.Void", MessageId="_xr")]
[module: SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", Scope="member", Target="System.Management.Automation.Runspaces.PipelineBase.Dispose(System.Boolean):System.Void", MessageId="_pipelineFinishedEvent")]
[module: SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", Scope="member", Target="Microsoft.PowerShell.ConsoleHost.Dispose(System.Boolean):System.Void", MessageId="consoleWriter")]
[module: SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", Scope="member", Target="Microsoft.PowerShell.Commands.FileSystemProvider.IsValidPath(System.String):System.Boolean")]
[module: SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", Scope="member", Target="System.Management.Automation.ParameterBinderController.UpdatePositionalDictionary(System.Collections.Generic.SortedDictionary`2<System.Int32,System.Collections.Generic.Dictionary`2<System.Management.Automation.MergedCompiledCommandParameter,System.Management.Automation.PositionalCommandParameter>>,System.UInt32):System.Void", MessageId="System.Collections.ObjectModel.Collection`1<System.Management.Automation.MergedCompiledCommandParameter>")]
[module: SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", Scope="member", Target="Microsoft.PowerShell.Commands.StartProcessCommand.StartWithCreateProcess(System.Diagnostics.ProcessStartInfo):System.Diagnostics.Process")]

[module: SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", Scope="member", Target="Microsoft.PowerShell.Commands.Internal.Format.PrinterLineOutput.VerifyFont(System.Drawing.Graphics):System.Void", MessageId="System.Drawing.Graphics.MeasureString(System.String,System.Drawing.Font)")]
[module: SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", Scope="member", Target="Microsoft.PowerShell.PSAuthorizationManager.ShouldRun(System.Management.Automation.CommandInfo,System.Management.Automation.CommandOrigin,System.Management.Automation.Host.PSHost,System.Exception&):System.Boolean", MessageId="System.ArgumentException.#ctor(System.String)")]
[module: SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", Scope="member", Target="Microsoft.PowerShell.Commands.Internal.Format.TypeInfoDataBaseLoader.LoadXmlFile(Microsoft.PowerShell.Commands.Internal.Format.XmlFileLoadInfo,Microsoft.PowerShell.Commands.Internal.Format.TypeInfoDataBase,Microsoft.PowerShell.Commands.Internal.Format.MshExpressionFactory):System.Boolean", MessageId="Microsoft.PowerShell.Commands.Internal.Format.XmlLoaderBase.ReportTrace(System.String)")]
[module: SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", Scope="member", Target="Microsoft.PowerShell.Commands.Internal.Format.XmlLoaderBase.LoadXmlDocumentFromFileLoadingInfo():System.Xml.XmlDocument", MessageId="Microsoft.PowerShell.Commands.Internal.Format.XmlLoaderBase.ReportTrace(System.String)")]

[module: SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Scope="member", Target="System.Management.Automation.Runspaces.InvalidPipelineStateException..ctor(System.Runtime.Serialization.SerializationInfo,System.Runtime.Serialization.StreamingContext)")]
[module: SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Scope="type", Target="System.Management.Automation.PSObjectDisposedException")]

[module: SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Scope="type", Target="System.Management.Automation.Internal.ParsingBaseAttribute")]
[module: SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Scope="type", Target="System.Management.Automation.Internal.CmdletMetadataAttribute")]
[module: SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Scope="type", Target="System.Management.Automation.Internal.CommonParameters")]
[module: SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Scope="type", Target="System.Management.Automation.Internal.AutomationNull")]
[module: SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Scope="type", Target="System.Management.Automation.Internal.InternalCommand")]
[module: SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Scope="type", Target="System.Management.Automation.Internal.ShouldProcessParameters")]

[module: SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.MatchInfo.Filename", MessageId="Filename")]
[module: SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", Scope="type", Target="Microsoft.PowerShell.Commands.OutLineOutputCommand", MessageId="OutLine")]
[module: SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.ConvertToSecureStringCommand.AsPlainText", MessageId="PlainText")]
[module: SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", Scope="member", Target="System.Management.Automation.InvocationInfo.OffsetInLine", MessageId="InLine")]

[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="Microsoft.PowerShell.Commands.ImportClixmlCommand", MessageId="Clixml")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="Microsoft.PowerShell.Commands.ExportClixmlCommand", MessageId="Clixml")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.Internal.Format.FrontEndCommandBase.WriteObjectCall(System.Object):System.Void", MessageId="0#o")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.SecurityDescriptorCommandsBase.GetSddl(System.Management.Automation.PSObject):System.String", MessageId="Sddl")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.X509StoreLocation..ctor(System.Security.Cryptography.X509Certificates.StoreLocation)", MessageId="0#l")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="Microsoft.PowerShell.Commands.GetPfxCertificateCommand", MessageId="Pfx")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.SetAclCommand.Passthru", MessageId="Passthru")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Host.ControlKeyStates.NumLockOn", MessageId="Num")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Host.Coordinates..ctor(System.Int32,System.Int32)", MessageId="0#x")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Host.Coordinates..ctor(System.Int32,System.Int32)", MessageId="1#y")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Host.Coordinates.X", MessageId="X")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Host.Coordinates.Y", MessageId="Y")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.AddHistoryCommand.Passthru", MessageId="Passthru")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="Microsoft.PowerShell.Commands.GetPSSnapinCommand", MessageId="Snapin")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="Microsoft.PowerShell.Commands.AddPSSnapinCommand", MessageId="Snapin")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.GetCommandCommand.PSSnapin", MessageId="Snapin")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="Microsoft.PowerShell.Commands.RemovePSSnapinCommand", MessageId="Snapin")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="namespace", Target="System.Management.Automation.Runspaces", MessageId="Runspaces")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="namespace", Target="System.Management.Automation.Sqm", MessageId="Sqm")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="System.Management.Automation.Runspaces.RunspaceConfigurationAttributeException", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="System.Management.Automation.Runspaces.RunspaceStateEventArgs", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspaceStateEventArgs.RunspaceStateInfo", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="System.Management.Automation.Runspaces.InvalidRunspaceStateException", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="System.Management.Automation.Runspaces.RunspaceState", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.Pipeline.Runspace", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="System.Management.Automation.Runspaces.RunspaceConfigurationEntry", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="System.Management.Automation.Runspaces.RunspaceConfigurationTypeAttribute", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspaceConfigurationTypeAttribute.RunspaceConfigurationType", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="System.Management.Automation.Runspaces.RunspaceFactory", MessageId="Runspace")]

[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspaceFactory.CreateRunspace(System.Management.Automation.Host.PSHost):System.Management.Automation.Runspaces.Runspace", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspaceFactory.#CreateRunspace(System.Management.Automation.Runspaces.RunspaceConfiguration)", MessageId="Runspace", Justification="Runspace is a valid word in PowerShell.")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspaceFactory.#CreateRunspace(System.Management.Automation.Host.PSHost,System.Management.Automation.Runspaces.RunspaceConfiguration)", MessageId="runspace", Justification="Runspace is a valid word in PowerShell")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspaceFactory.#CreateRunspace(System.Management.Automation.Runspaces.RunspaceConfiguration)", MessageId="runspace", Justification="Runspace is a valid word in PowerShell")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspaceFactory.CreateRunspace(System.Management.Automation.Host.PSHost,System.Management.Automation.Runspaces.RunspaceConfiguration):System.Management.Automation.Runspaces.Runspace", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspaceFactory.CreateRunspace(System.Management.Automation.Host.PSHost,System.Management.Automation.Runspaces.RunspaceConfiguration):System.Management.Automation.Runspaces.Runspace", MessageId="1#runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspaceFactory.CreateRunspace():System.Management.Automation.Runspaces.Runspace", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspaceFactory.#CreateRunspacePool()", MessageId="Runspace", Justification="Runspace is a valid word in PowerShell.")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspaceFactory.#CreateRunspacePool(System.Int32)", MessageId="Runspace", Justification="Runspace is a valid word in PowerShell.")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspaceFactory.#CreateRunspacePool(System.Int32)", MessageId="Runspaces", Justification="Runspace is a valid word in PowerShell.")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspaceFactory.#CreateRunspacePool(System.Int32,System.Int32)", MessageId="Runspace", Justification="Runspace is a valid word in PowerShell.")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspaceFactory.#CreateRunspacePool(System.Int32,System.Int32)", MessageId="Runspaces", Justification="Runspace is a valid word in PowerShell.")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspaceFactory.#CreateRunspacePool(System.Management.Automation.Runspaces.RunspaceConfiguration)", MessageId="runspace", Justification="Runspace is a valid word in PowerShell.")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspaceFactory.#CreateRunspacePool(System.Management.Automation.Runspaces.RunspaceConfiguration)", MessageId="Runspace", Justification="Runspace is a valid word in PowerShell.")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspaceFactory.#CreateRunspacePool(System.Management.Automation.Host.PSHost)", MessageId="Runspace", Justification="Runspace is a valid keyword in PowerShell.")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspaceFactory.#CreateRunspacePool(System.Management.Automation.Runspaces.RunspaceConfiguration,System.Management.Automation.Host.PSHost)", MessageId="runspace", Justification="Runspace is a valid keyword in PowerShell.")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspaceFactory.#CreateRunspacePool(System.Management.Automation.Runspaces.RunspaceConfiguration,System.Management.Automation.Host.PSHost)", MessageId="Runspace", Justification="Runspace is a valid keyword in PowerShell.")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspaceFactory.#CreateRunspacePool(System.Int32,System.Int32,System.Management.Automation.Runspaces.RunspaceConfiguration,System.Management.Automation.Host.PSHost)", MessageId="runspace", Justification="Runspace is a valid keyword in PowerShell.")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspaceFactory.#CreateRunspacePool(System.Int32,System.Int32,System.Management.Automation.Runspaces.RunspaceConfiguration,System.Management.Automation.Host.PSHost)", MessageId="Runspace", Justification="Runspace is a valid keyword in PowerShell.")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspaceFactory.#CreateRunspacePool(System.Int32,System.Int32,System.Management.Automation.Runspaces.RunspaceConfiguration,System.Management.Automation.Host.PSHost)", MessageId="Runspaces", Justification="Runspace is a valid keyword in PowerShell.")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="System.Management.Automation.Runspaces.RunspacePool", MessageId="Runspace", Justification="Runspace is a valid word in PowerShell.")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspacePool.#GetMaxRunspaces()", MessageId="Runspaces", Justification="Runspace is a valid word in PowerShell.")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspacePool.#GetMinRunspaces()", MessageId="Runspaces", Justification="Runspace is a valid word in PowerShell.")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspacePool.#RunspaceConfiguration", MessageId="Runspace", Justification="Runspace is a valid word in PowerShell.")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspacePool.#SetMaxRunspaces(System.Int32)", MessageId="Runspaces", Justification="Runspace is a valid word in PowerShell.")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspacePool.#SetMinRunspaces(System.Int32)", MessageId="Runspaces", Justification="Runspace is a valid word in PowerShell.")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspacePool.#GetAvailableRunspaces()", MessageId="Runspaces")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="System.Management.Automation.Runspaces.RunspaceConfigurationTypeException", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="System.Management.Automation.Runspaces.RunspaceConfigurationEntryCollection`1", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="System.Management.Automation.Runspaces.Runspace", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.Runspace.DefaultRunspace", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.Runspace.RunspaceConfiguration", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.Runspace.RunspaceStateInfo", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="System.Management.Automation.Runspaces.RunspaceConfiguration", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="System.Management.Automation.Runspaces.RunspaceStateInfo", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="System.Management.Automation.RunspaceInvoke", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.RunspaceInvoke..ctor(System.Management.Automation.Runspaces.Runspace)", MessageId="0#runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.RunspaceInvoke..ctor(System.Management.Automation.Runspaces.RunspaceConfiguration)", MessageId="0#runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.WildcardPattern.Unescape(System.String):System.String", MessageId="Unescape")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.CommandOrigin.Runspace", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="System.Management.Automation.Runspaces.RunspacePoolState", MessageId="Runspace", Justification="Runspace is a valid word in PowerShell")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="System.Management.Automation.Runspaces.RunspacePoolStateChangedEventArgs", MessageId="Runspace", Justification="Runspace is a valid word in PowerShell")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspacePoolStateChangedEventArgs.#RunspacePoolState", MessageId="Runspace", Justification="Runspace is a valid word in PowerShell")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="TypesXmlStrings.resources", MessageId="ps", Justification="ps referes to PowerShell and is used at many places in the product.")]


[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.SelectObjectCommand.ExcludeProperty")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.SelectObjectCommand.Property")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ImportClixmlCommand.Path")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ImportCsvCommand.Path")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.RemoveVariableCommand.Name")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.RemoveVariableCommand.Include")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.RemoveVariableCommand.Exclude")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.VariableCommandBase.ExcludeFilters")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.VariableCommandBase.IncludeFilters")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.OrderObjectBase.Property")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.NewObjectCommand.ArgumentList")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetMemberCommand.Name")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetTraceSourceCommand.Name")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.UpdateData.AppendPath")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.UpdateData.PrependPath")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.TraceCommandCommand.ArgumentList")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.TraceCommandCommand.Name")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetAliasCommand.Name")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetAliasCommand.Exclude")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.WriteOutputCommand.InputObject")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ExportAliasCommand.Name")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.SelectStringCommand.Exclude")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.SelectStringCommand.Path")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.SelectStringCommand.Include")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.SelectStringCommand.Pattern")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ClearVariableCommand.Name")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ClearVariableCommand.Include")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ClearVariableCommand.Exclude")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetVariableCommand.Name")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetVariableCommand.Include")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetVariableCommand.Exclude")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.CompareObjectCommand.Property")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.CompareObjectCommand.DifferenceObject")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.CompareObjectCommand.ReferenceObject")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ConvertToHtmlCommand.Head")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ConvertToHtmlCommand.Body")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ConvertToHtmlCommand.Property")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.SetVariableCommand.Exclude")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.SetVariableCommand.Include")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.SetVariableCommand.Name")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.FormatCustomCommand.Property")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.MeasureObjectCommand.Property")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.SetTraceSourceCommand.Name")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.SetTraceSourceCommand.RemoveListener")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.SetTraceSourceCommand.RemoveFileListener")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.Internal.Format.OuterFormatTableAndListBase.Property")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.AddHistoryCommand.InputObject")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetPSSnapinCommand.Name")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetHelpCommand.Functionality")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetHelpCommand.Category")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetHelpCommand.Role")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetHelpCommand.Component")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetHistoryCommand.Id")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ForEachObjectCommand.Process")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.AddPSSnapinCommand.Name")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetCommandCommand.ArgumentList")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetCommandCommand.Noun")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetCommandCommand.PSSnapin")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetCommandCommand.Name")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetCommandCommand.Verb")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.RemovePSSnapinCommand.Name")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="System.Management.Automation.PSSnapIn.Formats")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="System.Management.Automation.PSSnapIn.Types")]

[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.SignatureCommandsBase.FilePath")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetAclCommand.Path")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.SecurityDescriptorCommandsBase.Exclude")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.SecurityDescriptorCommandsBase.Include")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetPfxCertificateCommand.FilePath")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.SetAclCommand.Path")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ConvertFromToSecureStringCommandBase.Key")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetProcessCommand.Name")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetProcessCommand.Id")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.StopProcessCommand.Name")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.StopProcessCommand.Id")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ProcessBaseCommand.InputObject")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ResolvePathCommand.Path")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ResolvePathCommand.LiteralPath")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.MultipleServiceCommandBase.Exclude")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.MultipleServiceCommandBase.DisplayName")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.MultipleServiceCommandBase.Include")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.MultipleServiceCommandBase.InputObject")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.MoveItemCommand.Path")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.MoveItemCommand.LiteralPath")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ConvertPathCommand.Path")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ConvertPathCommand.LiteralPath")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.CopyItemPropertyCommand.LiteralPath")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.CopyItemPropertyCommand.Path")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.NewItemCommand.Path")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.SetItemCommand.LiteralPath")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.SetItemCommand.Path")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.RemoveItemPropertyCommand.Name")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.RemoveItemPropertyCommand.LiteralPath")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.RemoveItemPropertyCommand.Path")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.RemoveItemCommand.LiteralPath")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.RemoveItemCommand.Path")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ContentCommandBase.Path")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ContentCommandBase.LiteralPath")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.InvokeItemCommand.LiteralPath")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.InvokeItemCommand.Path")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.RemovePSDriveCommand.Name")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.RemovePSDriveCommand.LiteralName")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.RemovePSDriveCommand.PSProvider")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetItemPropertyCommand.Name")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetItemPropertyCommand.Path")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetItemPropertyCommand.LiteralPath")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetWmiObjectCommand.ComputerName")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetWmiObjectCommand.Property")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetItemCommand.LiteralPath")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetItemCommand.Path")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.MoveItemPropertyCommand.Name")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.MoveItemPropertyCommand.LiteralPath")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.MoveItemPropertyCommand.Path")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetPSProviderCommand.PSProvider")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ClearItemPropertyCommand.Path")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ClearItemPropertyCommand.LiteralPath")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetPSDriveCommand.Name")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetPSDriveCommand.LiteralName")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetPSDriveCommand.PSProvider")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.NewItemPropertyCommand.LiteralPath")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.NewItemPropertyCommand.Path")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ClearItemCommand.LiteralPath")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ClearItemCommand.Path")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.SetItemPropertyCommand.LiteralPath")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.SetItemPropertyCommand.Path")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.TestPathCommand.LiteralPath")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.TestPathCommand.Path")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.WriteContentCommandBase.Value")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetServiceCommand.Name")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ServiceOperationBaseCommand.Name")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetLocationCommand.PSDrive")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetLocationCommand.PSProvider")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetLocationCommand.StackName")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.CoreCommandBase.Exclude")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.CoreCommandBase.Include")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetChildItemCommand.LiteralPath")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetChildItemCommand.Path")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.SplitPathCommand.Path")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.SplitPathCommand.LiteralPath")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.CopyItemCommand.Path")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.CopyItemCommand.LiteralPath")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.JoinPathCommand.Path")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetPSBreakpointCommand.Id")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.NewServiceCommand.DependsOn")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.SetPSBreakpointCommand.Command")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.SetPSBreakpointCommand.Function")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.SetPSBreakpointCommand.Script")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.SetPSBreakpointCommand.Line")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.SetPSBreakpointCommand.Variable")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.PSBreakpointCommandBase.Breakpoint")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.PSBreakpointCommandBase.Id")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="System.Management.Automation.PSDebugContext.Breakpoints")]

[module: SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Scope="member", Target="Microsoft.PowerShell.Commands.GetHelpCommand.Full")]
[module: SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Scope="member", Target="Microsoft.PowerShell.Commands.GetHelpCommand.Detailed")]
[module: SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Scope="member", Target="Microsoft.PowerShell.Commands.GetHelpCommand.Examples")]

[module: SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes", Scope="type", Target="Microsoft.PowerShell.Commands.SelectStringCommand+FileinfoToStringAttribute")]

[module: SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Scope="type", Target="System.Management.Automation.PathInfoStack")]
[module: SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Scope="type", Target="System.Management.Automation.PathInfoStack")]

[module: SuppressMessage("Microsoft.Naming", "CA1714:FlagsEnumsShouldHavePluralNames", Scope="type", Target="System.Management.Automation.ShouldProcessReason")]

[module: SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces", Scope="type", Target="System.Management.Automation.SessionState")]

[module: SuppressMessage("Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible", Scope="member", Target="System.Management.Automation.VerbsOther.Use")]
[module: SuppressMessage("Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible", Scope="member", Target="System.Management.Automation.Remoting.PSSessionConfigurationData.IsServerManager")]



[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="RemotingErrorIdStrings.resources", MessageId="URIs")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="RunspaceStrings.resources", MessageId="runspaces")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="HistoryStrings.resources", MessageId="commandline")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="WebCmdletStrings.resources", MessageId="From-Json")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="WebCmdletStrings.resources", MessageId="To-Json")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="Logging.resources", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="Runspace.resources", MessageId="runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="Runspace.resources", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="MiniShellErrors.resources", MessageId="runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="MiniShellErrors.resources", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ParserStrings.resources", MessageId="Param")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ParserStrings.resources", MessageId="foreach")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ParserStrings.resources", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ParserStrings.resources", MessageId="scriptblock")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ParserStrings.resources", MessageId="subexpression")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="PowerShellStrings.resources", MessageId="Runspaces")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="DiscoveryExceptions.resources", MessageId="pssnapin")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="DiscoveryExceptions.resources", MessageId="shellid")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="RegistryProviderStrings.resources", MessageId="itemproperty")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="RegistryProviderStrings.resources", MessageId="Multi")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="RegistryProviderStrings.resources", MessageId="multi")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="RunspaceInit.resources", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="MshSnapinInfo.resources", MessageId="multistring")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="SessionStateStrings.resources", MessageId="psprovider")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="SessionStateStrings.resources", MessageId="forwardslashes")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="CommandLineParameterParserStrings.resources", MessageId="stdin")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ConsoleHostUserInterfaceStrings.resources", MessageId="noninteractive")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ManagedEntranceStrings.resources", MessageId="Noninteractive")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="MakeKitMessages.resources", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="MakeKitMessages.resources", MessageId="cscflags")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="MakeKitMessages.resources", MessageId="formatdata")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="MakeKitMessages.resources", MessageId="initscript")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="MakeKitMessages.resources", MessageId="typedata")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="MakeKitMessages.resources", MessageId="Powershell")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="MakeKitMessages.resources", MessageId="authorizationmanager")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="MakeKitMessages.resources", MessageId="builtinscript")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="MakeKitMessages.resources", MessageId="csc")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ConsoleHostStrings.resources", MessageId="aboutprompt")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="Debugger.resources", MessageId="ps", Justification="ps refers to the ps1 extension for PowerShell script files.")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="Debugger.resources", MessageId="psm", Justification="ps refers to the ps1 extension for PowerShell module files.")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="DebuggerStrings.resources", MessageId="aboutprompt", Justification="about_prompt is a valid help topic")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="TabCompletionStrings.resources", MessageId="ps", Justification="ps refers to the ps1 extension for PowerShell script files.")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="TabCompletionStrings.resources", MessageId="bak", Justification="bak refers to the bak extension.")]
[module: SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", Scope="member", Target="System.Management.Automation.Runspaces.PipelineWriter.#Write(System.Object)", MessageId="obj", Justification="This will be a breaking change as V1 is  already shipped.")]
[module: SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", Scope="member", Target="System.Management.Automation.Runspaces.PipelineWriter.#Write(System.Object,System.Boolean)", MessageId="obj", Justification="This will be a breaking change as V1 is  already shipped.")]

[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.ConsoleHost+NativeMethods.SHGetFolderPath(System.IntPtr,System.UInt32,System.IntPtr,System.UInt32,System.Text.StringBuilder):System.UInt32")]
[module: SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", Scope="member", Target="System.Management.Automation.Diagnostics.Assert(System.Boolean,System.String,System.String):System.Void", MessageId="System.Management.Automation.AssertException.#ctor(System.String)")]
[module: SuppressMessage("Microsoft.Naming", "CA1701:ResourceStringCompoundWordsShouldBeCasedCorrectly", Scope="resource", Target="SignatureCommands.resources", MessageId="TimeStamp")]
[module: SuppressMessage("Microsoft.Naming", "CA1701:ResourceStringCompoundWordsShouldBeCasedCorrectly", Scope="resource", Target="ConsoleHostStrings.resources", MessageId="Username")]
[module: SuppressMessage("Microsoft.Naming", "CA1701:ResourceStringCompoundWordsShouldBeCasedCorrectly", Scope="resource", Target="Authenticode.resources", MessageId="TimeStamp")]
[module: SuppressMessage("Microsoft.Naming", "CA1701:ResourceStringCompoundWordsShouldBeCasedCorrectly", Scope="resource", Target="ParserStrings.resources", MessageId="filename")]
[module: SuppressMessage("Microsoft.Naming", "CA1701:ResourceStringCompoundWordsShouldBeCasedCorrectly", Scope="resource", Target="ParserStrings.resources", MessageId="Filename")]
[module: SuppressMessage("Microsoft.Naming", "CA1701:ResourceStringCompoundWordsShouldBeCasedCorrectly", Scope="resource", Target="FormatAndOutXmlLoadingStrings.resources", MessageId="FormatTable")]

[module: SuppressMessage("Microsoft.Usage", "CA2229:ImplementSerializationConstructors", Scope="type", Target="System.Management.Automation.RuntimeDefinedParameterDictionary")]

[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspaceConfigurationTypeAttribute.#.ctor(System.String)", MessageId="runspace", Justification="Runspace is a valid word in PowerShell")]

[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ManagedEntranceStrings.resources", MessageId="nologo")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ManagedEntranceStrings.resources", MessageId="outputformat")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ManagedEntranceStrings.resources", MessageId="eventlog")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ManagedEntranceStrings.resources", MessageId="powershell")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ManagedEntranceStrings.resources", MessageId="logname")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ManagedEntranceStrings.resources", MessageId="psconsolefile")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ManagedEntranceStrings.resources", MessageId="inputformat")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="System.Management.Automation.Runspaces.InvalidRunspacePoolStateException", MessageId="Runspace", Justification="Runspace is a valid word for PowerShell")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.RunspaceInvoke.#.ctor(System.Management.Automation.Runspaces.Runspace)", MessageId="runspace", Justification="Runspace is a valid word in PowerShell")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.RunspaceInvoke.#.ctor(System.Management.Automation.Runspaces.RunspaceConfiguration)", MessageId="runspace", Justification="Runspace is a valid word in PowerShell")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.PowerShell.#GetRunspace()", MessageId="Runspace", Justification="Runspace is a valid word in PowerShell.")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.PowerShell.#SetRunspace(System.Management.Automation.Runspaces.Runspace)", MessageId="Runspace", Justification="Runspace is a valid word in PowerShell.")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.PowerShell.#SetRunspace(System.Management.Automation.Runspaces.RunspacePool)", MessageId="Runspace", Justification="Runspace is a valid word in PowerShell.")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="PowerShellStrings.resources", MessageId="runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", Scope="member", Target="System.Management.Automation.Runspaces.Pipeline.#Error", MessageId="Error", Justification="This is part of V1 code and we cannot break V1.")]
[module: SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", Scope="member", Target="System.Management.Automation.Runspaces.Pipeline.#Stop()", MessageId="Stop", Justification="This is part of V1 code and we cannot break V1.")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="PowerShellStrings.resources", MessageId="powershell")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="RunspacePoolStrings.resources", MessageId="runspace", Justification="Runspace is a valid word in PowerShell.")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="RunspacePoolStrings.resources", MessageId="Runspace", Justification="Runspace is a valid word in PowerShell.")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="SessionStateStrings.resources", MessageId="get-psprovider", Justification="This is part of V1 and V1 is already shipped.")]

[module: SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Scope="member", Target="System.Management.Automation.PSCredential.op_Explicit(System.Management.Automation.PSCredential):System.Net.NetworkCredential")]
[module: SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Scope="member", Target="Microsoft.PowerShell.Commands.GroupInfo..ctor(Microsoft.PowerShell.Commands.OrderByPropertyEntry)")]
[module: SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Scope="member", Target="Microsoft.PowerShell.Commands.Internal.Format.CommandParameterDefinition..ctor()")]
[module: SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Scope="member", Target="System.Management.Automation.ActionPreferenceStopException..ctor(System.Management.Automation.InvocationInfo,System.String,System.String,System.Object[])")]
[module: SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Scope="member", Target="System.Management.Automation.LookupPathCollection..ctor(System.Collections.Generic.IEnumerable`1<System.String>)")]
[module: SuppressMessage("Microsoft.Usage", "CA2240:ImplementISerializableCorrectly", Scope="type", Target="System.Management.Automation.ProviderNameAmbiguousException")]
[module: SuppressMessage("Microsoft.Usage", "CA2240:ImplementISerializableCorrectly", Scope="type", Target="System.Management.Automation.RuntimeDefinedParameterDictionary")]
[module: SuppressMessage("Microsoft.Usage", "CA2240:ImplementISerializableCorrectly", Scope="type", Target="System.Management.Automation.PSSecurityException")]
[module: SuppressMessage("Microsoft.Design", "CA1036:OverrideMethodsOnComparableTypes", Scope="type", Target="System.Management.Automation.PSObject")]

[module: SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Scope="type", Target="System.Management.Automation.SessionStateInternal", Justification="This is internal and well tested as part of v1.")]

[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetAsyncResultCommand.#Command")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetAsyncResultCommand.#Name")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetRunspaceCommand.#ComputerName")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetRunspaceCommand.#RemoteRunspaceID")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.InvokeRemoteExpressionCommand.#ComputerName")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.InvokeRemoteExpressionCommand.#Runspace")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.IRemoteOperationAsyncResult.#ComputerName")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.IRemoteOperationAsyncResult.#Runspace")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.NewRunspaceCommand.#ComputerName")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.NewRunspaceCommand.#Runspace")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.NewRunspaceCommand.#URI")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ReceiveAsyncResultCommand.#ComputerName")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ReceiveAsyncResultCommand.#Result")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ReceiveAsyncResultCommand.#Runspace")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.RemoveRunspaceCommand.#Runspace")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.StopAsyncResultCommand.#Id")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.StopAsyncResultCommand.#Result")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.WaitAsyncResultCommand.#Id")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.WaitAsyncResultCommand.#Result")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="Microsoft.PowerShell.Commands.GetRunspaceCommand", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.InvokeRemoteExpressionCommand.#Runspace", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.IRemoteOperationAsyncResult.#Runspace", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="Microsoft.PowerShell.Commands.NewRunspaceCommand", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.NewRunspaceCommand.#Runspace", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.ReceiveAsyncResultCommand.#Runspace", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="System.Management.Automation.Remoting.RemoteRunspaceInfo", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Remoting.RemoteRunspaceInfo.#RemoteRunspaceID", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Remoting.RemoteRunspaceInfo.#RunspaceStateInfo", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Remoting.OriginInfo.#.ctor(System.String,System.Guid,System.Management.Automation.Runspaces.Command)", MessageId="runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Remoting.OriginInfo.#RunspaceID", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="System.Management.Automation.Remoting.RemoteRunspace", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="System.Management.Automation.Runspaces.RemoteRunspaceBase", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RemoteRunspaceBase.#ByPassRunspaceStateCheck", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RemoteRunspaceBase.#RaiseRunspaceStateEvents()", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RemoteRunspaceBase.#RunspaceState", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RemoteRunspaceBase.#SetRunspaceState(System.Management.Automation.Runspaces.RunspaceState)", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RemoteRunspaceBase.#SetRunspaceState(System.Management.Automation.Runspaces.RunspaceState,System.Exception)", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Remoting.RemoteRunspaceInfo.#RemoteRunspaceId", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetRunspaceCommand.#RemoteRunspaceId")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.NewRunspaceCommand.#Uri")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.GetRunspaceCommand.#RemoteRunspaceId", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="Microsoft.PowerShell.Commands.RemoveRunspaceCommand", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.RemoveRunspaceCommand.#Runspace", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RemoteRunspaceBase.#BypassRunspaceStateCheck", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.GetRunspaceCommand.#RemoteRunspaceId", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="Microsoft.PowerShell.Commands.RemoveRunspaceCommand", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.RemoveRunspaceCommand.#Runspace", MessageId="Runspace")]


[module: SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Scope="member", Target="System.Management.Automation.Runspaces.RemoteRunspaceBase.#RaiseRunspaceStateEvents()")]

[module: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.Internal.Format.FormatObjectDeserializer.#VerifyDataNotNull(System.Object,System.String)", Justification="The ArgumentException is constructed as part of construction of an ErrorRecord. The error details contains the name of the parameter")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.Internal.Format.FrontEndCommandBase.#WriteObjectCall(System.Object)", MessageId="o", Justification="This will be a breaking change")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.Commands.Internal.Format.TypeMatch.#get_ActiveTracer()", Justification="Unused for now, may be required in future")]
[module: SuppressMessage("Microsoft.Design", "CA1064:ExceptionsShouldBePublic", Scope="type", Target="Microsoft.PowerShell.Commands.Internal.Format.TypeInfoDataBaseLoaderException", Justification="This class has been designed for internal consumption")]
[module: SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Scope="type", Target="Microsoft.PowerShell.Commands.Internal.Format.TooManyErrorsException", Justification="This class has been designed for internal consumption")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.WriteOutputCommand.#InputObject", Justification="Cmdlet properties do return arrays")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.FormatCustomCommand.#Property", Justification="Cmdlet properties do return arrays")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.Internal.Format.OuterFormatTableAndListBase.#Property", Justification="Cmdlet properties do return arrays")]


[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope = "resource", Target = "MakeKitMessages.resources", MessageId = "ps")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope = "resource", Target = "MakeKitMessages.resources", MessageId = "resourcefile")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope = "resource", Target = "MakeKitMessages.resources", MessageId = "ico")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope = "resource", Target = "MakeKitMessages.resources", MessageId = "libdirectory")]


[module: SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope="member", Target="Microsoft.Powershell.Commands.NewPSDebugCommand.#Commands")]
[module: SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope="member", Target="Microsoft.Powershell.Commands.NewPSDebugCommand.#Variables")]


[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="MshSnapinInfo.resources", MessageId="Snapin")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ParserStrings.resources", MessageId="Foreach")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ParserStrings.resources", MessageId="uiculture")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ParserStrings.resources", MessageId="convertfrom")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ParserStrings.resources", MessageId="param")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ParserStrings.resources", MessageId="Splatted")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ParserStrings.resources", MessageId="Opeartor")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ParserStrings.resources", MessageId="Runspaces")]

[module: SuppressMessage("Microsoft.Naming", "CA1701:ResourceStringCompoundWordsShouldBeCasedCorrectly", Scope="resource", Target="ImportLocalizedData.resources", MessageId="filename")]

[module: SuppressMessage("Microsoft.Design", "CA1018:MarkAttributesWithAttributeUsage", Scope="type", Target="System.Management.Automation.ValidateScriptAttribute")]

[module: SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Scope="type", Target="System.Management.Automation.BreakException")]

[module: SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Scope="type", Target="System.Management.Automation.ContinueException")]

[module: SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Scope="type", Target="System.Management.Automation.ExitException")]

[module: SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Scope="type", Target="System.Management.Automation.ReturnException")]

[module: SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Scope="type", Target="System.Management.Automation.Interpreter.RethrowException")]
[module: SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Scope="type", Target="System.Management.Automation.Interpreter.RethrowException")]

[module: SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", Scope="member", Target="System.Management.Automation.ExpressionNode.#OperatorCompare(System.Management.Automation.ComparisonToken,System.Object,System.Object)", MessageId="System.Management.Automation.LanguagePrimitives.Compare(System.Object,System.Object,System.Boolean)")]


[module: SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Scope="type", Target="System.Management.Automation.FlowControlException")]
[module: SuppressMessage("Microsoft.Design", "CA1064:ExceptionsShouldBePublic", Scope="type", Target="System.Management.Automation.FlowControlException")]

[module: SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", Scope="member", Target="System.Management.Automation.ScriptBlock.#EnterScope(System.Management.Automation.ScriptInvocationContext)", MessageId="System.Management.Automation.LanguagePrimitives.ConvertTo(System.Object,System.Type)")]

[module: SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Scope="member", Target="System.Management.Automation.PSToken.#Type")]


[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Management.Automation.ArrayReferenceNode.#DoSetValue(System.Collections.IList,System.Object,System.Object)")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Management.Automation.ArrayReferenceNode.#DoGetValue(System.Collections.IList,System.Object)")]


[module: SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Scope="member", Target="System.Management.Automation.ScriptCallDepthException.#get_ErrorRecord()")]


[module: SuppressMessage("Microsoft.Usage", "CA1816:CallGCSuppressFinalizeCorrectly", Scope="member", Target="System.Management.Automation.PSScriptCmdlet.#Dispose()")]


[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ManagedEntranceStrings.resources", MessageId="psc")]

[module: SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.WriteHostCommand.#NoNewline", MessageId="Newline", Justification="This code is in V1 and changing is breaking change.")]
[module: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.NewPSDebugCommand.#ProcessRecord()")]
[module: SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope="member", Target="Microsoft.PowerShell.Commands.NewPSDebugCommand.#Commands", Justification="This is parameter on cmdlet which needs to be settable")]
[module: SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope="member", Target="Microsoft.PowerShell.Commands.NewPSDebugCommand.#Variables", Justification="This is parameter on cmdlet which needs to be excluded.")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.SelectStringCommand.#Context", Justification="This is parameter to cmdlet")]
[module: SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Scope="member", Target="Microsoft.PowerShell.Commands.TraceListenerCommandBase.#AddTraceListenersToSources(System.Collections.ObjectModel.Collection`1<System.Management.Automation.PSTraceSource>)")]
[module: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.ConsoleColorCmdlet.#BuildOutOfRangeErrorRecord(System.Object,System.String)")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ConsoleHostStrings.resources", MessageId="Hmmss", Justification="Hmmss is time format")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ConsoleHostStrings.resources", MessageId="yyyy", Justification="yyyy is time format")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ConsoleHostStrings.resources", MessageId="Mdd", Justification="mdd is time format")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ManagedEntranceStrings.resources", MessageId="sqlsnapin")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Host.Coordinates.#.ctor(System.Int32,System.Int32)", MessageId="x")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Host.Coordinates.#.ctor(System.Int32,System.Int32)", MessageId="y")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Host.KeyInfo.#.ctor(System.Int32,System.Char,System.Management.Automation.Host.ControlKeyStates,System.Boolean)", MessageId="ch")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.PSArgumentNullException.#.ctor(System.String,System.String)", MessageId="param", Justification=".Net ArugmentNullException has the same parameter name.")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.PSArgumentNullException.#.ctor(System.String)", MessageId="param", Justification=".Net ArugmentNullException has the same parameter name")]
[module: SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", Scope="member", Target="System.Management.Automation.SwitchParameter.#ToBool()", MessageId="bool")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.PSArgumentOutOfRangeException.#.ctor(System.String)", MessageId="param", Justification=".Net exception use same parameter name.")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.PSArgumentOutOfRangeException.#.ctor(System.String,System.Object,System.String)", MessageId="param", Justification=".Net exception uses same parameter name.")]
[module: SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", Scope="member", Target="System.Management.Automation.ErrorRecord.#.ctor(System.Exception,System.String,System.Management.Automation.ErrorCategory,System.Object)", MessageId="object")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ConsoleInfoErrorStrings.resources", MessageId="psc")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="DiscoveryExceptions.resources", MessageId="ps")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="HelpDisplayStrings.resources", MessageId="aboutcommonparameters")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="HelpDisplayStrings.resources", MessageId="fwlink")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="HelpErrors.resources", MessageId="fwlink")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="HelpErrors.resources", MessageId="fwlink")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="HelpErrors.resources", MessageId="pshome")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="MshSignature.resources", MessageId="aboutsigning")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="AutomationExceptions.resources", MessageId="scriptblock")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="AutomationExceptions.resources", MessageId="steppable")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="AutomationExceptions.resources", MessageId="dynamicparam")]
[module: SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", Scope="member", Target="System.Management.Automation.ICommandRuntime.#ShouldContinue(System.String,System.String,System.Boolean&,System.Boolean&)", MessageId="2#")]
[module: SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", Scope="member", Target="System.Management.Automation.ICommandRuntime.#ShouldContinue(System.String,System.String,System.Boolean&,System.Boolean&)", MessageId="3#")]
[module: SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", Scope="member", Target="System.Management.Automation.Cmdlet.#ShouldContinue(System.String,System.String,System.Boolean&,System.Boolean&)", MessageId="2#")]
[module: SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", Scope="member", Target="System.Management.Automation.Cmdlet.#ShouldContinue(System.String,System.String,System.Boolean&,System.Boolean&)", MessageId="3#")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.PSArgumentException.#.ctor(System.String,System.String)", MessageId="param", Justification=".Net exception use the same parameter name")]
[module: SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Scope="member", Target="System.Management.Automation.PSArgumentException.#get_ErrorRecord()", Justification="Exception is not thrown from this property.")]
[module: SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Scope="member", Target="System.Management.Automation.PSNotSupportedException.#get_ErrorRecord()", Justification="Property is not throwing the exception.")]
[module: SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Scope="member", Target="System.Management.Automation.PSArgumentNullException.#get_ErrorRecord()", Justification="Property is not throwing the exception.")]
[module: SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Scope="member", Target="System.Management.Automation.RuntimeException.#get_ErrorRecord()", Justification="Property is not throwing the exception.")]
[module: SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Scope="member", Target="System.Management.Automation.CommandNotFoundException.#get_ErrorRecord()", Justification="Property is not throwing the exception.")]
[module: SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Scope="member", Target="System.Management.Automation.PSNotImplementedException.#get_ErrorRecord()", Justification="Property is not throwing the exception.")]
[module: SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Scope="member", Target="System.Management.Automation.PSInvalidCastException.#get_ErrorRecord()", Justification="Property is not throwing the exception.")]
[module: SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Scope="member", Target="System.Management.Automation.PSArgumentOutOfRangeException.#get_ErrorRecord()", Justification="Property is not throwing the exception.")]
[module: SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Scope="member", Target="System.Management.Automation.PSObjectDisposedException.#get_ErrorRecord()", Justification="Property is not throwing the exception.")]
[module: SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Scope="member", Target="System.Management.Automation.PSInvalidOperationException.#get_ErrorRecord()", Justification="Property is not throwing the exception.")]
[module: SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Scope="member", Target="System.Management.Automation.ProviderInvocationException.#get_ErrorRecord()", Justification="Property is not throwing the exception.")]
[module: SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Scope="member", Target="System.Management.Automation.CmdletInvocationException.#get_ErrorRecord()", Justification="Property is not throwing the exception.")]
[module: SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Scope="member", Target="System.Management.Automation.Runspaces.PSSnapInException.#get_ErrorRecord()", Justification="Property is not throwing the exception.")]
[module: SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Scope="member", Target="System.Management.Automation.PSScriptProperty.#get_Value()")]
[module: SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Scope="member", Target="System.Management.Automation.PSCodeProperty.#get_TypeNameOfValue()")]
[module: SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Scope="member", Target="System.Management.Automation.PSCodeProperty.#get_Value()")]
[module: SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Scope="type", Target="System.Management.Automation.ExitNestedPromptException")]
[module: SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Scope="type", Target="System.Management.Automation.TerminateException")]
[module: SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Scope="type", Target="System.Management.Automation.StopUpstreamCommandsException")]
[module: SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Scope="type", Target="System.Management.Automation.AssertException")]
[module: SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Scope="type", Target="System.Management.Automation.ScriptRequiresSyntaxException")]
[module: SuppressMessage("Microsoft.Design", "CA1064:ExceptionsShouldBePublic", Scope="type", Target="System.Management.Automation.AssertException")]
[module: SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Scope="type", Target="System.Management.Automation.AssertException", Justification="This is internal class.")]
[module: SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Scope="type", Target="Microsoft.PowerShell.Commands.Internal.Format.TypeInfoDataBaseLoaderException", Justification="This is internal class")]
[module: SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Scope="type", Target="System.Management.Automation.ScriptRequiresSyntaxException")]
[module: SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Scope="member", Target="System.Management.Automation.CommandProcessor.#.ctor(System.String,System.Management.Automation.ExecutionContext)", Justification="This is all internal code.")]
[module: SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Scope="member", Target="System.Management.Automation.CommandProcessor.#.ctor(System.Management.Automation.CmdletInfo,System.Management.Automation.ExecutionContext)", Justification="This is all internal code.")]
[module: SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Scope="member", Target="System.Management.Automation.CommandProcessor.#.ctor(System.Management.Automation.CmdletInfo,System.Management.Automation.ExecutionContext,System.Boolean)", Justification="This is all internal code.")]
[module: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope="member", Target="System.Management.Automation.PSTraceSource.#GetNewTraceSource(System.String,System.String,System.Boolean)")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetPSJobCommand.#Command")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetPSJobCommand.#InstanceId")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetPSJobCommand.#Name")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.GetPSJobCommand.#SessionId")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ReceivePSJobCommand.#ComputerName")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ReceivePSJobCommand.#Job")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ReceivePSJobCommand.#Runspace")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.RemovePSJob.#InstanceId")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.RemovePSJob.#Job")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.RemovePSJob.#Name")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.RemovePSJob.#SessionId")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.StartJobCommand.#ComputerName")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.StartJobCommand.#Runspace")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.StopPSJobCommand.#InstanceId")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.StopPSJobCommand.#Job")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.StopPSJobCommand.#Name")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.StopPSJobCommand.#SessionId")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.WaitPSJobCommand.#InstanceId")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.WaitPSJobCommand.#Job")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.WaitPSJobCommand.#Name")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.WaitPSJobCommand.#SessionId")]


[module: SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope="member", Target="System.Management.Automation.PSJob.#Debug")]
[module: SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope="member", Target="System.Management.Automation.PSJob.#Error")]
[module: SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope="member", Target="System.Management.Automation.PSJob.#Output")]
[module: SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope="member", Target="System.Management.Automation.PSJob.#Progress")]
[module: SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope="member", Target="System.Management.Automation.PSJob.#Verbose")]
[module: SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope="member", Target="System.Management.Automation.PSJob.#Warning")]


[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.SplitOptions", MessageId="Singleline")]
[module: SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", Scope="member", Target="System.Management.Automation.SplitOptions", MessageId="IgnorePatternWhitespace")]

[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="System.Management.Automation.RemoteRunspace", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="System.Management.Automation.Runspaces.RunspaceConnectionInfo", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspaceFactory.#CreateRunspacePool(System.Int32,System.Int32,System.Management.Automation.Host.PSHost,System.Management.Automation.Runspaces.RunspaceConnectionInfo)", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspaceFactory.#CreateRunspacePool(System.Int32,System.Int32,System.Management.Automation.Runspaces.RunspaceConnectionInfo)", MessageId="Runspace")]


[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ParserStrings.resources", MessageId="runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="PipelineStrings.resources", MessageId="steppable")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="PowerShellStrings.resources", MessageId="Runspace")]

[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="RemotingErrorIdStrings.resources", MessageId="runspaces")]


[module: SuppressMessage("Microsoft.Globalization", "CA1309:UseOrdinalStringComparison", Scope="member", Target="System.Management.Automation.ValidateSetAttribute.#ValidateElement(System.Object)", MessageId="System.String.Compare(System.String,System.String,System.Boolean,System.Globalization.CultureInfo)")]

[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.StartJobCommand.#Runspace", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.ReceivePSJobCommand.#Runspace", MessageId="Runspace")]

[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ParserStrings.resources", MessageId="Scriptblocks")]


[module: SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope="member", Target="Microsoft.PowerShell.Commands.SetWmiInstance.#Argument")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.RemoveWmiObject.#ComputerName")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.InvokeWmiMethod.#ComputerName")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.InvokeWmiMethod.#ArgumentList")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.SetWmiInstance.#ComputerName")]


[module: SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", Scope="member", Target="System.Management.Automation.ParameterAttributeNode.#GetAttribute(System.Management.Automation.ParameterAttribute)", MessageId="System.Management.Automation.LanguagePrimitives.ConvertTo(System.Object,System.Type)")]

[module: SuppressMessage("Microsoft.Design", "CA1018:MarkAttributesWithAttributeUsage", Scope="type", Target="System.Management.Automation.ValidateScriptBlockAttribute")]

[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.InvokeCommandCommand.#ArgumentList")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.PSRemotingBaseCmdlet.#ConnectionUri")]

[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="RemotingErrorIdStrings.resources", MessageId="ps")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="RemotingErrorIdStrings.resources", MessageId="pssc")]

[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="Authenticode.resources", MessageId="aboutexecutionpolicies")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="Authenticode.resources", MessageId="fwlink")]

[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="MshSignature.resources", MessageId="fwlink")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ExecutionPolicyCommands.resources", MessageId="fwlink")]

[module: SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", Scope="member", Target="System.Management.Automation.Tracing.PowerShellTraceKeywords.#ManagedPlugIn", MessageId="PlugIn")]

[module: SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly", Scope="member", Target="System.Management.Automation.ScriptBlockMemberMethodWrapper.#_emptyArgumentArray")]
















































































































































































































































































[assembly: AssemblyTitle("System.Management.Automation")]
[assembly: AssemblyDescription("Microsoft Windows PowerShell Engine Core Assembly")]


[assembly: InternalsVisibleTo("Microsoft.Test.Management.Automation.GPowershell.Analyzers,PublicKey=00240000048000009400000006020000002400005253413100040000010001003f8c902c8fe7ac83af7401b14c1bd103973b26dfafb2b77eda478a2539b979b56ce47f36336741b4ec52bbc51fecd51ba23810cec47070f3e29a2261a2d1d08e4b2b4b457beaa91460055f78cc89f21cd028377af0cc5e6c04699b6856a1e49d5fad3ef16d3c3d6010f40df0a7d6cc2ee11744b5cfb42e0f19a52b8a29dc31b0")]

[assembly: InternalsVisibleTo(@"System.Management.Automation.Help"+@",PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")]
[assembly: InternalsVisibleTo(@"Microsoft.PowerShell.Commands.Utility"+@",PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")]
[assembly: InternalsVisibleTo(@"Microsoft.PowerShell.Commands.Management"+@",PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")]
[assembly: InternalsVisibleTo(@"Microsoft.PowerShell.Security"+@",PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")]
[assembly: InternalsVisibleTo(@"System.Management.Automation.Remoting"+@",PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")]
[assembly: InternalsVisibleTo(@"Export-Command"+@",PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")]
[assembly: InternalsVisibleTo(@"Microsoft.PowerShell.ConsoleHost"+@",PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")]
[assembly: InternalsVisibleTo(@"Microsoft.PowerShell.PowerShellLanguageService"+@",PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")]
[assembly: InternalsVisibleTo(@"Microsoft.PowerShell.GraphicalHost"+@",PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")]
[assembly: InternalsVisibleTo(@"Microsoft.PowerShell.GPowerShell"+@",PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")]
[assembly: InternalsVisibleTo(@"Microsoft.PowerShell.ISECommon"+@",PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")]
[assembly: InternalsVisibleTo(@"Microsoft.PowerShell.Editor"+@",PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")]
[assembly: InternalsVisibleTo(@"powershell_ise"+@",PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")]

[module: SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Scope="type", Target="Microsoft.PowerShell.Commands.Internal.Format.OuterFormatTableBase", Justification="This will lead to a breaking change")]
[module: SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", Scope="type", Target="Microsoft.PowerShell.Commands.Internal.Format.OuterFormatTableBase", MessageId="FormatTable", Justification="This will be a breaking change")]
[module: SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Scope="type", Target="Microsoft.PowerShell.Commands.Internal.Format.OuterFormatTableAndListBase", Justification="This will be a breaking change")]
[module: SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", Scope="type", Target="Microsoft.PowerShell.Commands.Internal.Format.OuterFormatTableAndListBase", MessageId="FormatTable", Justification="This will be a breaking change")]
[module: SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Scope="type", Target="Microsoft.PowerShell.Commands.Internal.Format.OuterFormatShapeCommandBase", Justification="This will be a breaking change")]
[module: SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Scope="type", Target="Microsoft.PowerShell.Commands.Internal.Format.FrontEndCommandBase", Justification="This will be a breaking change")]

[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ManagedEntranceStrings.resources", MessageId="nologo")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ManagedEntranceStrings.resources", MessageId="outputformat")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ManagedEntranceStrings.resources", MessageId="eventlog")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ManagedEntranceStrings.resources", MessageId="powershell")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ManagedEntranceStrings.resources", MessageId="logname")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ManagedEntranceStrings.resources", MessageId="psconsolefile")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ManagedEntranceStrings.resources", MessageId="inputformat")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ParameterBinderStrings.resources", MessageId="fwlink")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ParserStrings.resources", MessageId="splatted")]


[module: SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Scope="member", Target="Microsoft.PowerShell.Commands.RegistryProviderSetItemDynamicParameter.#Type", Justification="This is the type of the registry key, and used as a dynamic parameter. This should stay as-is, and would be a breaking change if changed anyways.")]
[module: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.FileSystemProvider.#GetContentReader(System.String)", Justification="This would be a breaking change, and is consistent with the way we handle other exceptions.")]
[module: SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.FileSystemCmdletProviderEncoding.#UTF7", MessageId="UTF", Justification="This would be a breaking change")]
[module: SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.FileSystemCmdletProviderEncoding.#UTF8", MessageId="UTF", Justification="This would be a breaking change")]
[module: SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.FileSystemCmdletProviderEncoding.#UTF32", MessageId="UTF")]
[module: SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", Scope="member", Target="System.Management.Automation.Provider.CmdletProvider.#Stop()", MessageId="Stop", Justification="This would be a breaking change")]
[module: SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", Scope="member", Target="System.Management.Automation.Provider.CmdletProvider.#ShouldContinue(System.String,System.String,System.Boolean&,System.Boolean&)", MessageId="2#", Justification="This would be a breaking change")]
[module: SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", Scope="member", Target="System.Management.Automation.Provider.CmdletProvider.#ShouldContinue(System.String,System.String,System.Boolean&,System.Boolean&)", MessageId="3#", Justification="This would be a breaking change")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Management.Automation.SessionStateScope.#get_ErrorCapacity()", Justification="Lazy initialization was added to all properties of this class to improve performance.  Removing this would make it likely that future work with the ErrorCapacity variable would be done without lazy initialization.")]
[module: SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Scope="type", Target="System.Management.Automation.SessionStateInternal", Justification="This is a bridge class between internal classses and a public interface. It requires this much coupling.")]
[module: SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Scope="member", Target="System.Management.Automation.SessionStateException.#get_ErrorRecord()", Justification="This doesn't raise the error record, it just creates it in lieu of one that should be there.")]
[module: SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Scope="member", Target="System.Management.Automation.PSSecurityException.#get_ErrorRecord()", Justification="This doesn't raise the error record, it just creates it in lieu of one that should be there.")]
[module: SuppressMessage("Microsoft.Interoperability", "CA1404:CallGetLastErrorImmediatelyAfterPInvoke", Scope="member", Target="System.Management.Automation.SecuritySupport.#GetCertEKU(System.Security.Cryptography.X509Certificates.X509Certificate2)", Justification="This is a false positive. Marshal is in an else block, right after a P/Invoke call.")]
[module: SuppressMessage("Microsoft.Interoperability", "CA1404:CallGetLastErrorImmediatelyAfterPInvoke", Scope="member", Target="Microsoft.PowerShell.Commands.FileSystemProvider.#GetSubstitutedPathForNetworkDosDevice(System.String)", Justification="This is a false positive. Marshal is in an else block, right after a P/Invoke call.")]
[module: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope="member", Target="System.Management.Automation.Security.NativeMethods+WINTRUST_BLOB_INFO.#pbMemObject")]
[module: SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Scope="member", Target="System.Management.Automation.SessionStateCapacityVariable.#.ctor(System.String,System.Management.Automation.SessionStateCapacityVariable)", Justification="This accesses the Attributes collection in the base class, not a derived class.")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Provider.ICmdletProviderSupportsHelp.#GetHelpMaml(System.String,System.String)", MessageId="Maml")]
    
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="RemotingErrorIdStrings.en.resources", MessageId="winrm")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="RemotingErrorIdStrings.en.resources", MessageId="runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="RemotingErrorIdStrings.en.resources", MessageId="runspace's")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="RemotingErrorIdStrings.en.resources", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="RemotingErrorIdStrings.en.resources", MessageId="unmarshalling")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="RemotingErrorIdStrings.resources", MessageId="winrm")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="RemotingErrorIdStrings.resources", MessageId="runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="RemotingErrorIdStrings.resources", MessageId="runspace's")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="RemotingErrorIdStrings.resources", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="RemotingErrorIdStrings.resources", MessageId="unmarshalling")]

[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="Authenticode.resources", MessageId="aboutsigning")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="SessionStateStrings.resources", MessageId="get-psprovider")]


[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.PSRemotingBaseCmdlet.#Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.PSRemotingBaseCmdlet.#Runspace", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.PSRemotingBaseCmdlet.#ValidateRemoteRunspacesSpecified()", MessageId="Runspaces")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.PSRemotingBaseCmdlet.#Uri")]


[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.PSRemotingBaseCmdlet.#ResolvedComputerNames")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.PSRemotingBaseCmdlet.#ComputerName")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="Microsoft.PowerShell.Commands.PSRunspaceCmdlet", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.PSRunspaceCmdlet.#RemoteRunspaceId")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.PSRunspaceCmdlet.#RemoteRunspaceId", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.PSRunspaceCmdlet.#ComputerName")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.PSRunspaceCmdlet.#Name")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.PSRunspaceCmdlet.#RunspaceIdParameterSet", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.PSRunspaceCmdlet.#GetMatchingRunspaces(System.Boolean,System.Boolean)", MessageId="writeobject")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.PSRunspaceCmdlet.#GetMatchingRunspaces(System.Boolean,System.Boolean)", MessageId="Runspaces")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.PSRunspaceCmdlet.#SessionId")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.PSRemotingCmdlet.#RunspaceParameterSet", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.JobCmdletBase.#Command")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.JobCmdletBase.#Name")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.JobCmdletBase.#InstanceId")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.JobCmdletBase.#SessionId")]
[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="Microsoft.PowerShell.Commands.ReceiveJobCommand.#Location")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.PowerShell.Commands.WaitJobCommand.#get_Command()")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspacePoolStateChangedEventArgs.#RunspacePoolStateInfo", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspaceFactory.#CreateRunspace(System.Management.Automation.Host.PSHost,System.Management.Automation.Runspaces.RunspaceConnectionInfo)", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Runspaces.RunspaceFactory.#CreateRunspace(System.Management.Automation.Runspaces.RunspaceConnectionInfo)", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope="member", Target="System.Management.Automation.Remoting.FanIn.Client.WSManClientNativeApi+WSManCommandArgSet+WSManCommandArgSetInternal.#args")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="System.Management.Automation.RunspaceRepository", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.RunspaceRepository.#Runspaces", MessageId="Runspaces")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.SplitOptions.#Singleline", MessageId="Singleline")]

[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.RemoteRunspace.#ByPassRunspaceStateCheck", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", Scope="member", Target="System.Management.Automation.RemoteRunspace.#ByPassRunspaceStateCheck", MessageId="ByPass")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Remoting.OriginInfo.#.ctor(System.String,System.Guid)", MessageId="runspace")]

[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="AutomationExceptions.resources", MessageId="param", Justification="param is not a misspelled word - it is a PowerShell language keyword")]


[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="System.Management.Automation.RunspacePoolStateInfo", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="System.Management.Automation.Host.IHostSupportsPushRunspace", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Host.IHostSupportsPushRunspace.#PopRunspace()", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Host.IHostSupportsPushRunspace.#PushRunspace(System.Management.Automation.Runspaces.Runspace)", MessageId="runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Host.IHostSupportsPushRunspace.#PushRunspace(System.Management.Automation.Runspaces.Runspace)", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="Microsoft.PowerShell.Commands.PopRunspaceCommand", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="Microsoft.PowerShell.Commands.PushRunspaceCommand", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.PushRunspaceCommand.#Runspace", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="Modules.resources", MessageId="runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="Modules.resources", MessageId="ps")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="Modules.resources", MessageId="psd")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="RemotingErrorIdStrings.resources", MessageId="psd")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="Modules.resources", MessageId="psm")]



[module: SuppressMessage("Microsoft.Maintainability", "CA1505:AvoidUnmaintainableCode", Scope="member", Target="System.Management.Automation.Remoting.RemoteHostMethodInfo.#LookUp(System.Management.Automation.Remoting.RemoteHostMethodId)")]

[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="Microsoft.PowerShell.Commands.PopRunspaceCommand", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="Microsoft.PowerShell.Commands.PushRunspaceCommand", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="type", Target="System.Management.Automation.Host.IHostSupportsPushRunspace", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Host.IHostSupportsPushRunspace.#PopRunspace()", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Host.IHostSupportsPushRunspace.#PushRunspace(System.Management.Automation.Runspaces.Runspace)", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Host.IHostSupportsPushRunspace.#Runspace", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Host.IHostSupportsPushRunspace.#IsRunspacePushed", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="System.Management.Automation.Host.IHostSupportsPushRunspace.#PushRunspace(System.Management.Automation.Runspaces.Runspace)", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="member", Target="Microsoft.PowerShell.Commands.PushRunspaceCommand.#Runspace", MessageId="Runspace")]



[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="RemotingErrorIdStrings.resources", MessageId="Push-Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="RemotingErrorIdStrings.resources", MessageId="Pop-Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="RemotingErrorIdStrings.resources", MessageId="Runspace")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="RemotingErrorIdStrings.resources", MessageId="aboutremote")]

[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="SuggestionStrings.resources", MessageId="aboutcommandsearches")]

[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="Modules.resources", MessageId="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx")]

[module: SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope="member", Target="Microsoft.Powershell.Commands.NewPSSessionOptionCommand.#ApplicationArguments")]

[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ParserStrings.resources", MessageId="splatting")]
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="ParserStrings.resources", MessageId="nmust")] 

[module: SuppressMessage("Microsoft.Naming", "CA1701:ResourceStringCompoundWordsShouldBeCasedCorrectly", Scope="resource", Target="Logging.resources", MessageId="tError")]

[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="Modules.resources", MessageId="cdxml")]

[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope="resource", Target="CmdletizationCoreResources.resources", MessageId="cdxml")]

[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope="namespace", Target="Microsoft.PowerShell.Cmdletization", MessageId="Cmdletization")]

[module: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope="member", Target="System.Management.Automation.ParseException.#Errors")]
[module: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope="member", Target="System.Management.Automation.Remoting.Internal.PSStreamObject.#WriteStreamObject(System.Management.Automation.Cmdlet,System.Boolean)")]
[module: SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Scope="type", Target="System.Management.Automation.Remoting.Internal.PSStreamObjectType")]
[module: SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Scope="type", Target="System.Management.Automation.Remoting.Internal.PSStreamObject")]
[module: SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Scope="type", Target="System.Management.Automation.Remoting.Internal.PSStreamObjectType")]

[module: SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", Scope="member", Target="System.Management.Automation.Remoting.RemoteHostCall.#ExecuteNonVoidMethodOnObject(System.Object)", MessageId="System.String.ToUpper")]
[module: SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", Scope="member", Target="System.Management.Automation.Remoting.RemoteHostCall.#ModifyMessage(System.String,System.String)", MessageId="System.String.ToUpper")]
[module: SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", Scope="member", Target="System.Management.Automation.Remoting.RemoteHostCall.#ConstructWarningMessageForSecureString(System.String,System.String)", MessageId="System.String.ToUpper")]
[module: SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", Scope="member", Target="System.Management.Automation.Remoting.RemoteHostCall.#ConstructWarningMessageForGetBufferContents(System.String)", MessageId="System.String.ToUpper")]
*/

// PH: the block above is all commented out because it does not yet make sense for the linux build, especially the InternalsVisibleTo declarations

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

