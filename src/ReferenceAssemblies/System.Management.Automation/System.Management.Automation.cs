namespace Microsoft.PowerShell
{
    public sealed class DeserializingTypeConverter : PSTypeConverter
    {
        public DeserializingTypeConverter();
        public override bool CanConvertFrom(PSObject sourceValue, Type destinationType);
        public override bool CanConvertFrom(object sourceValue, Type destinationType);
        public override bool CanConvertTo(PSObject sourceValue, Type destinationType);
        public override bool CanConvertTo(object sourceValue, Type destinationType);
        public override object ConvertFrom(PSObject sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase);
        public override object ConvertFrom(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase);
        public override object ConvertTo(PSObject sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase);
        public override object ConvertTo(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase);
        public static Guid GetFormatViewDefinitionInstanceId(PSObject instance);
        public static PSObject GetInvocationInfo(PSObject instance);
        public static uint GetParameterSetMetadataFlags(PSObject instance);
    }
    public enum ExecutionPolicy
    {
        AllSigned = 2,
        Bypass = 4,
        Default = 3,
        RemoteSigned = 1,
        Restricted = 3,
        Undefined = 5,
        Unrestricted = 0,
    }
    public enum ExecutionPolicyScope
    {
        CurrentUser = 1,
        LocalMachine = 2,
        MachinePolicy = 4,
        Process = 0,
        UserPolicy = 3,
    }
    public static class ProcessCodeMethods
    {
        public static object GetParentProcess(PSObject obj);
    }
    public sealed class PSAuthorizationManager : AuthorizationManager
    {
        public PSAuthorizationManager(string shellId);
        protected internal override bool ShouldRun(CommandInfo commandInfo, CommandOrigin origin, PSHost host, out Exception reason);
    }
    public static class ToStringCodeMethods
    {
        public static string Type(PSObject instance);
        public static string XmlNode(PSObject instance);
        public static string XmlNodeList(PSObject instance);
    }
}
namespace Microsoft.PowerShell.Cim
{
    public sealed class CimInstanceAdapter : PSPropertyAdapter
    {
        public CimInstanceAdapter();
        public override Collection<PSAdaptedProperty> GetProperties(object baseObject);
        public override PSAdaptedProperty GetProperty(object baseObject, string propertyName);
        public override string GetPropertyTypeName(PSAdaptedProperty adaptedProperty);
        public override object GetPropertyValue(PSAdaptedProperty adaptedProperty);
        public override Collection<string> GetTypeNameHierarchy(object baseObject);
        public override bool IsGettable(PSAdaptedProperty adaptedProperty);
        public override bool IsSettable(PSAdaptedProperty adaptedProperty);
        public override void SetPropertyValue(PSAdaptedProperty adaptedProperty, object value);
    }
}
namespace Microsoft.PowerShell.Cmdletization
{
    public enum BehaviorOnNoMatch
    {
        Default = 0,
        ReportErrors = 1,
        SilentlyContinue = 2,
    }
    public abstract class CmdletAdapter<TObjectInstance> where TObjectInstance : class
    {
        protected CmdletAdapter();
        public string ClassName { get; }
        public string ClassVersion { get; }
        public PSCmdlet Cmdlet { get; }
        public Version ModuleVersion { get; }
        public IDictionary<string, string> PrivateData { get; }
        public virtual void BeginProcessing();
        public virtual void EndProcessing();
        public virtual QueryBuilder GetQueryBuilder();
        public void Initialize(PSCmdlet cmdlet, string className, string classVersion, Version moduleVersion, IDictionary<string, string> privateData);
        public virtual void ProcessRecord(MethodInvocationInfo methodInvocationInfo);
        public virtual void ProcessRecord(QueryBuilder query);
        public virtual void ProcessRecord(QueryBuilder query, MethodInvocationInfo methodInvocationInfo, bool passThru);
        public virtual void ProcessRecord(TObjectInstance objectInstance, MethodInvocationInfo methodInvocationInfo, bool passThru);
        public virtual void StopProcessing();
    }
    public sealed class MethodInvocationInfo
    {
        public MethodInvocationInfo(string name, IEnumerable<MethodParameter> parameters, MethodParameter returnValue);
        public string MethodName { get; }
        public KeyedCollection<string, MethodParameter> Parameters { get; }
        public MethodParameter ReturnValue { get; }
    }
    public sealed class MethodParameter
    {
        public MethodParameter();
        public MethodParameterBindings Bindings { get; set; }
        public bool IsValuePresent { get; set; }
        public string Name { get; set; }
        public Type ParameterType { get; set; }
        public string ParameterTypeName { get; set; }
        public object Value { get; set; }
    }
    public enum MethodParameterBindings
    {
        Error = 4,
        In = 1,
        Out = 2,
    }
    public abstract class QueryBuilder
    {
        protected QueryBuilder();
        public virtual void AddQueryOption(string optionName, object optionValue);
        public virtual void ExcludeByProperty(string propertyName, IEnumerable excludedPropertyValues, bool wildcardsEnabled, BehaviorOnNoMatch behaviorOnNoMatch);
        public virtual void FilterByAssociatedInstance(object associatedInstance, string associationName, string sourceRole, string resultRole, BehaviorOnNoMatch behaviorOnNoMatch);
        public virtual void FilterByMaxPropertyValue(string propertyName, object maxPropertyValue, BehaviorOnNoMatch behaviorOnNoMatch);
        public virtual void FilterByMinPropertyValue(string propertyName, object minPropertyValue, BehaviorOnNoMatch behaviorOnNoMatch);
        public virtual void FilterByProperty(string propertyName, IEnumerable allowedPropertyValues, bool wildcardsEnabled, BehaviorOnNoMatch behaviorOnNoMatch);
    }
}
namespace Microsoft.PowerShell.Cmdletization.Xml
{
    public enum ConfirmImpact
    {
        High = 3,
        Low = 1,
        Medium = 2,
        None = 0,
    }
    public enum ItemsChoiceType
    {
        ExcludeQuery = 0,
        MaxValueQuery = 1,
        MinValueQuery = 2,
        RegularQuery = 3,
    }
}
namespace Microsoft.PowerShell.Commands
{
    public class AddHistoryCommand : PSCmdlet
    {
        public AddHistoryCommand();
        public PSObject[] InputObject { get; set; }
        public SwitchParameter Passthru { get; set; }
        protected override void BeginProcessing();
        protected override void ProcessRecord();
    }
    public sealed class AliasProvider : SessionStateProviderBase
    {
        public const string ProviderName = "Alias";
        public AliasProvider();
        protected override Collection<PSDriveInfo> InitializeDefaultDrives();
        protected override object NewItemDynamicParameters(string path, string type, object newItemValue);
        protected override object SetItemDynamicParameters(string path, object value);
    }
    public class AliasProviderDynamicParameters
    {
        public AliasProviderDynamicParameters();
        public ScopedItemOptions Options { get; set; }
    }
    public class ClearHistoryCommand : PSCmdlet
    {
        public ClearHistoryCommand();
        public string[] CommandLine { get; set; }
        public int Count { get; set; }
        public int[] Id { get; set; }
        public SwitchParameter Newest { get; set; }
        protected override void BeginProcessing();
        protected override void ProcessRecord();
    }
    public class ConnectPSSessionCommand : PSRunspaceCmdlet, IDisposable
    {
        public ConnectPSSessionCommand();
        public SwitchParameter AllowRedirection { get; set; }
        public string ApplicationName { get; set; }
        public AuthenticationMechanism Authentication { get; set; }
        public string CertificateThumbprint { get; set; }
        public override string[] ComputerName { get; set; }
        public string ConfigurationName { get; set; }
        public Uri[] ConnectionUri { get; set; }
        public override string[] ContainerId { get; }
        public PSCredential Credential { get; set; }
        public override Guid[] InstanceId { get; set; }
        public override string[] Name { get; set; }
        public int Port { get; set; }
        public PSSession[] Session { get; set; }
        public PSSessionOption SessionOption { get; set; }
        public int ThrottleLimit { get; set; }
        public SwitchParameter UseSSL { get; set; }
        public override Guid[] VMId { get; }
        public override string[] VMName { get; }
        protected override void BeginProcessing();
        public void Dispose();
        protected override void EndProcessing();
        protected override void ProcessRecord();
        protected override void StopProcessing();
    }
    public sealed class DebugJobCommand : PSCmdlet
    {
        public DebugJobCommand();
        public int Id { get; set; }
        public Guid InstanceId { get; set; }
        public Job Job { get; set; }
        public string Name { get; set; }
        protected override void EndProcessing();
        protected override void StopProcessing();
    }
    public sealed class DisablePSRemotingCommand : PSCmdlet
    {
        public DisablePSRemotingCommand();
        public SwitchParameter Force { get; set; }
        protected override void BeginProcessing();
        protected override void EndProcessing();
    }
    public sealed class DisablePSSessionConfigurationCommand : PSCmdlet
    {
        public DisablePSSessionConfigurationCommand();
        public SwitchParameter Force { get; set; }
        public string[] Name { get; set; }
        public SwitchParameter NoServiceRestart { get; set; }
        protected override void BeginProcessing();
        protected override void EndProcessing();
        protected override void ProcessRecord();
    }
    public class DisconnectPSSessionCommand : PSRunspaceCmdlet, IDisposable
    {
        public DisconnectPSSessionCommand();
        public override string[] ComputerName { get; set; }
        public override string[] ContainerId { get; }
        public int IdleTimeoutSec { get; set; }
        public OutputBufferingMode OutputBufferingMode { get; set; }
        public PSSession[] Session { get; set; }
        public int ThrottleLimit { get; set; }
        public override Guid[] VMId { get; }
        public override string[] VMName { get; }
        protected override void BeginProcessing();
        public void Dispose();
        protected override void EndProcessing();
        protected override void ProcessRecord();
        protected override void StopProcessing();
    }
    public sealed class EnablePSRemotingCommand : PSCmdlet
    {
        public EnablePSRemotingCommand();
        public SwitchParameter Force { get; set; }
        public SwitchParameter SkipNetworkProfileCheck { get; set; }
        protected override void BeginProcessing();
        protected override void EndProcessing();
    }
    public sealed class EnablePSSessionConfigurationCommand : PSCmdlet
    {
        public EnablePSSessionConfigurationCommand();
        public SwitchParameter Force { get; set; }
        public string[] Name { get; set; }
        public SwitchParameter NoServiceRestart { get; set; }
        public string SecurityDescriptorSddl { get; set; }
        public SwitchParameter SkipNetworkProfileCheck { get; set; }
        protected override void BeginProcessing();
        protected override void EndProcessing();
        protected override void ProcessRecord();
    }
    public sealed class EnterPSHostProcessCommand : PSCmdlet
    {
        public EnterPSHostProcessCommand();
        public string AppDomainName { get; set; }
        public PSHostProcessInfo HostProcessInfo { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public Process Process { get; set; }
        protected override void EndProcessing();
        protected override void StopProcessing();
    }
    public class EnterPSSessionCommand : PSRemotingBaseCmdlet
    {
        public EnterPSSessionCommand();
        public new string ComputerName { get; set; }
        public string ConfigurationName { get; set; }
        public new Uri ConnectionUri { get; set; }
        public new string ContainerId { get; set; }
        public override PSCredential Credential { get; set; }
        public SwitchParameter EnableNetworkAccess { get; set; }
        public new string HostName { get; set; }
        public int Id { get; set; }
        public Guid InstanceId { get; set; }
        public string Name { get; set; }
        public new PSSession Session { get; set; }
        public override Hashtable[] SSHConnection { get; }
        public new int ThrottleLimit { get; set; }
        public new Guid VMId { get; set; }
        public new string VMName { get; set; }
        protected override void BeginProcessing();
        protected override void EndProcessing();
        protected override void ProcessRecord();
        protected override void StopProcessing();
    }
    public sealed class EnvironmentProvider : SessionStateProviderBase
    {
        public const string ProviderName = "Environment";
        public EnvironmentProvider();
        protected override Collection<PSDriveInfo> InitializeDefaultDrives();
    }
    public sealed class ExitPSHostProcessCommand : PSCmdlet
    {
        public ExitPSHostProcessCommand();
        protected override void ProcessRecord();
    }
    public class ExitPSSessionCommand : PSRemotingCmdlet
    {
        public ExitPSSessionCommand();
        protected override void ProcessRecord();
    }
    public sealed class ExportModuleMemberCommand : PSCmdlet
    {
        public ExportModuleMemberCommand();
        public string[] Alias { get; set; }
        public string[] Cmdlet { get; set; }
        public string[] Function { get; set; }
        public string[] Variable { get; set; }
        protected override void ProcessRecord();
    }
    public class FileSystemClearContentDynamicParameters
    {
        public FileSystemClearContentDynamicParameters();
        public string Stream { get; set; }
    }
    public enum FileSystemCmdletProviderEncoding
    {
        Ascii = 8,
        BigEndianUnicode = 4,
        BigEndianUTF32 = 11,
        Byte = 3,
        Default = 9,
        Oem = 10,
        String = 1,
        Unicode = 2,
        Unknown = 0,
        UTF32 = 7,
        UTF7 = 6,
        UTF8 = 5,
    }
    public class FileSystemContentDynamicParametersBase
    {
        public FileSystemContentDynamicParametersBase();
        public FileSystemCmdletProviderEncoding Encoding { get; set; }
        public Encoding EncodingType { get; }
        public string Stream { get; set; }
        public bool UsingByteEncoding { get; }
        public bool WasStreamTypeSpecified { get; }
    }
    public class FileSystemContentReaderDynamicParameters : FileSystemContentDynamicParametersBase
    {
        public FileSystemContentReaderDynamicParameters();
        public string Delimiter { get; set; }
        public bool DelimiterSpecified { get; }
        public SwitchParameter Raw { get; set; }
        public SwitchParameter Wait { get; set; }
    }
    public class FileSystemContentWriterDynamicParameters : FileSystemContentDynamicParametersBase
    {
        public FileSystemContentWriterDynamicParameters();
        public SwitchParameter NoNewline { get; set; }
    }
    public class FileSystemItemProviderDynamicParameters
    {
        public FileSystemItemProviderDynamicParameters();
        public Nullable<DateTime> NewerThan { get; set; }
        public Nullable<DateTime> OlderThan { get; set; }
    }
    public sealed class FileSystemProvider : NavigationCmdletProvider, ICmdletProviderSupportsHelp, IContentCmdletProvider, IPropertyCmdletProvider, ISecurityDescriptorCmdletProvider
    {
        public const string ProviderName = "FileSystem";
        public FileSystemProvider();
        public void ClearContent(string path);
        public object ClearContentDynamicParameters(string path);
        public void ClearProperty(string path, Collection<string> propertiesToClear);
        public object ClearPropertyDynamicParameters(string path, Collection<string> propertiesToClear);
        protected override bool ConvertPath(string path, string filter, ref string updatedPath, ref string updatedFilter);
        protected override void CopyItem(string path, string destinationPath, bool recurse);
        protected override object CopyItemDynamicParameters(string path, string destination, bool recurse);
        protected override void GetChildItems(string path, bool recurse, uint depth);
        protected override object GetChildItemsDynamicParameters(string path, bool recurse);
        protected override string GetChildName(string path);
        protected override void GetChildNames(string path, ReturnContainers returnContainers);
        protected override object GetChildNamesDynamicParameters(string path);
        public IContentReader GetContentReader(string path);
        public object GetContentReaderDynamicParameters(string path);
        public IContentWriter GetContentWriter(string path);
        public object GetContentWriterDynamicParameters(string path);
        public string GetHelpMaml(string helpItemName, string path);
        protected override void GetItem(string path);
        protected override object GetItemDynamicParameters(string path);
        protected override string GetParentPath(string path, string root);
        public void GetProperty(string path, Collection<string> providerSpecificPickList);
        public object GetPropertyDynamicParameters(string path, Collection<string> providerSpecificPickList);
        public void GetSecurityDescriptor(string path, AccessControlSections sections);
        protected override bool HasChildItems(string path);
        protected override Collection<PSDriveInfo> InitializeDefaultDrives();
        protected override void InvokeDefaultAction(string path);
        protected override bool IsItemContainer(string path);
        protected override bool IsValidPath(string path);
        protected override bool ItemExists(string path);
        protected override object ItemExistsDynamicParameters(string path);
        public static string Mode(PSObject instance);
        protected override void MoveItem(string path, string destination);
        protected override PSDriveInfo NewDrive(PSDriveInfo drive);
        protected override void NewItem(string path, string type, object value);
        public ObjectSecurity NewSecurityDescriptorFromPath(string path, AccessControlSections sections);
        public ObjectSecurity NewSecurityDescriptorOfType(string type, AccessControlSections sections);
        protected override string NormalizeRelativePath(string path, string basePath);
        protected override PSDriveInfo RemoveDrive(PSDriveInfo drive);
        protected override void RemoveItem(string path, bool recurse);
        protected override object RemoveItemDynamicParameters(string path, bool recurse);
        protected override void RenameItem(string path, string newName);
        public void SetProperty(string path, PSObject propertyToSet);
        public object SetPropertyDynamicParameters(string path, PSObject propertyValue);
        public void SetSecurityDescriptor(string path, ObjectSecurity securityDescriptor);
        protected override ProviderInfo Start(ProviderInfo providerInfo);
    }
    public class FileSystemProviderGetItemDynamicParameters
    {
        public FileSystemProviderGetItemDynamicParameters();
        public string[] Stream { get; set; }
    }
    public class FileSystemProviderRemoveItemDynamicParameters
    {
        public FileSystemProviderRemoveItemDynamicParameters();
        public string[] Stream { get; set; }
    }
    public sealed class ForEachObjectCommand : PSCmdlet
    {
        public ForEachObjectCommand();
        public object[] ArgumentList { get; set; }
        public ScriptBlock Begin { get; set; }
        public ScriptBlock End { get; set; }
        public PSObject InputObject { get; set; }
        public string MemberName { get; set; }
        public ScriptBlock[] Process { get; set; }
        public ScriptBlock[] RemainingScripts { get; set; }
        protected override void BeginProcessing();
        protected override void EndProcessing();
        protected override void ProcessRecord();
    }
    public class FormatDefaultCommand : FrontEndCommandBase
    {
        public FormatDefaultCommand();
    }
    public sealed class FunctionProvider : SessionStateProviderBase
    {
        public const string ProviderName = "Function";
        public FunctionProvider();
        protected override Collection<PSDriveInfo> InitializeDefaultDrives();
        protected override object NewItemDynamicParameters(string path, string type, object newItemValue);
        protected override object SetItemDynamicParameters(string path, object value);
    }
    public class FunctionProviderDynamicParameters
    {
        public FunctionProviderDynamicParameters();
        public ScopedItemOptions Options { get; set; }
    }
    public sealed class GetCommandCommand : PSCmdlet
    {
        public GetCommandCommand();
        public SwitchParameter All { get; set; }
        public object[] ArgumentList { get; set; }
        public CommandTypes CommandType { get; set; }
        public ModuleSpecification[] FullyQualifiedModule { get; set; }
        public SwitchParameter ListImported { get; set; }
        public string[] Module { get; set; }
        public string[] Name { get; set; }
        public string[] Noun { get; set; }
        public string[] ParameterName { get; set; }
        public PSTypeName[] ParameterType { get; set; }
        public SwitchParameter ShowCommandInfo { get; set; }
        public SwitchParameter Syntax { get; set; }
        public int TotalCount { get; set; }
        public string[] Verb { get; set; }
        protected override void BeginProcessing();
        protected override void EndProcessing();
        protected override void ProcessRecord();
    }
    public static class GetHelpCodeMethods
    {
        public static string GetHelpUri(PSObject commandInfoPSObject);
    }
    public sealed class GetHelpCommand : PSCmdlet
    {
        public GetHelpCommand();
        public string[] Category { get; set; }
        public string[] Component { get; set; }
        public SwitchParameter Detailed { set; }
        public SwitchParameter Examples { set; }
        public SwitchParameter Full { set; }
        public string[] Functionality { get; set; }
        public string Name { get; set; }
        public SwitchParameter Online { get; set; }
        public string Parameter { get; set; }
        public string Path { get; set; }
        public string[] Role { get; set; }
        public SwitchParameter ShowWindow { get; set; }
        protected override void BeginProcessing();
        protected override void ProcessRecord();
    }
    public class GetHistoryCommand : PSCmdlet
    {
        public GetHistoryCommand();
        public int Count { get; set; }
        public long[] Id { get; set; }
        protected override void ProcessRecord();
    }
    public class GetJobCommand : JobCmdletBase
    {
        public GetJobCommand();
        public DateTime After { get; set; }
        public DateTime Before { get; set; }
        public JobState ChildJobState { get; set; }
        public bool HasMoreData { get; set; }
        public override int[] Id { get; set; }
        public SwitchParameter IncludeChildJob { get; set; }
        public int Newest { get; set; }
        protected List<Job> FindJobs();
        protected override void ProcessRecord();
    }
    public sealed class GetModuleCommand : ModuleCmdletBase, IDisposable
    {
        public GetModuleCommand();
        public SwitchParameter All { get; set; }
        public string CimNamespace { get; set; }
        public Uri CimResourceUri { get; set; }
        public CimSession CimSession { get; set; }
        public ModuleSpecification[] FullyQualifiedName { get; set; }
        public SwitchParameter ListAvailable { get; set; }
        public string[] Name { get; set; }
        public string PSEdition { get; set; }
        public PSSession PSSession { get; set; }
        public SwitchParameter Refresh { get; set; }
        public void Dispose();
        protected override void ProcessRecord();
        protected override void StopProcessing();
    }
    public sealed class GetPSHostProcessInfoCommand : PSCmdlet
    {
        public GetPSHostProcessInfoCommand();
        public int[] Id { get; set; }
        public string[] Name { get; set; }
        public Process[] Process { get; set; }
        protected override void EndProcessing();
    }
    public sealed class GetPSSessionCapabilityCommand : PSCmdlet
    {
        public GetPSSessionCapabilityCommand();
        public string ConfigurationName { get; set; }
        public SwitchParameter Full { get; set; }
        public string Username { get; set; }
        protected override void BeginProcessing();
    }
    public class GetPSSessionCommand : PSRunspaceCmdlet, IDisposable
    {
        public GetPSSessionCommand();
        public SwitchParameter AllowRedirection { get; set; }
        public string ApplicationName { get; set; }
        public AuthenticationMechanism Authentication { get; set; }
        public string CertificateThumbprint { get; set; }
        public override string[] ComputerName { get; set; }
        public string ConfigurationName { get; set; }
        public Uri[] ConnectionUri { get; set; }
        public PSCredential Credential { get; set; }
        public override Guid[] InstanceId { get; set; }
        public override string[] Name { get; set; }
        public int Port { get; set; }
        public PSSessionOption SessionOption { get; set; }
        public SessionFilterState State { get; set; }
        public int ThrottleLimit { get; set; }
        public SwitchParameter UseSSL { get; set; }
        protected override void BeginProcessing();
        public void Dispose();
        protected override void EndProcessing();
        protected override void ProcessRecord();
        protected override void StopProcessing();
    }
    public sealed class GetPSSessionConfigurationCommand : PSCmdlet
    {
        public GetPSSessionConfigurationCommand();
        public SwitchParameter Force { get; set; }
        public string[] Name { get; set; }
        protected override void BeginProcessing();
        protected override void ProcessRecord();
    }
    public class HelpCategoryInvalidException : ArgumentException, IContainsErrorRecord
    {
        public HelpCategoryInvalidException();
        protected HelpCategoryInvalidException(SerializationInfo info, StreamingContext context);
        public HelpCategoryInvalidException(string helpCategory);
        public HelpCategoryInvalidException(string helpCategory, Exception innerException);
        public ErrorRecord ErrorRecord { get; }
        public string HelpCategory { get; }
        public override string Message { get; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
    }
    public class HelpNotFoundException : SystemException, IContainsErrorRecord
    {
        public HelpNotFoundException();
        protected HelpNotFoundException(SerializationInfo info, StreamingContext context);
        public HelpNotFoundException(string helpTopic);
        public HelpNotFoundException(string helpTopic, Exception innerException);
        public ErrorRecord ErrorRecord { get; }
        public string HelpTopic { get; }
        public override string Message { get; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
    }
    public class HistoryInfo
    {
        public string CommandLine { get; }
        public DateTime EndExecutionTime { get; }
        public PipelineState ExecutionStatus { get; }
        public long Id { get; }
        public DateTime StartExecutionTime { get; }
        public HistoryInfo Clone();
        public override string ToString();
    }
    public sealed class ImportModuleCommand : ModuleCmdletBase, IDisposable
    {
        public ImportModuleCommand();
        public string[] Alias { get; set; }
        public object[] ArgumentList { get; set; }
        public SwitchParameter AsCustomObject { get; set; }
        public Assembly[] Assembly { get; set; }
        public string CimNamespace { get; set; }
        public Uri CimResourceUri { get; set; }
        public CimSession CimSession { get; set; }
        public string[] Cmdlet { get; set; }
        public SwitchParameter DisableNameChecking { get; set; }
        public SwitchParameter Force { get; set; }
        public ModuleSpecification[] FullyQualifiedName { get; set; }
        public string[] Function { get; set; }
        public SwitchParameter Global { get; set; }
        public string MaximumVersion { get; set; }
        public Version MinimumVersion { get; set; }
        public PSModuleInfo[] ModuleInfo { get; set; }
        public string[] Name { get; set; }
        public SwitchParameter NoClobber { get; set; }
        public SwitchParameter PassThru { get; set; }
        public string Prefix { get; set; }
        public PSSession PSSession { get; set; }
        public Version RequiredVersion { get; set; }
        public string Scope { get; set; }
        public string[] Variable { get; set; }
        protected override void BeginProcessing();
        public void Dispose();
        protected override void ProcessRecord();
        protected override void StopProcessing();
    }
    public static class InternalSymbolicLinkLinkCodeMethods
    {
        public static string GetLinkType(PSObject instance);
        public static IEnumerable<string> GetTarget(PSObject instance);
    }
    public class InvokeCommandCommand : PSExecutionCmdlet, IDisposable
    {
        public InvokeCommandCommand();
        public override SwitchParameter AllowRedirection { get; set; }
        public override string ApplicationName { get; set; }
        public SwitchParameter AsJob { get; set; }
        public override AuthenticationMechanism Authentication { get; set; }
        public override string[] ComputerName { get; set; }
        public override string ConfigurationName { get; set; }
        public override Uri[] ConnectionUri { get; set; }
        public override PSCredential Credential { get; set; }
        public override SwitchParameter EnableNetworkAccess { get; set; }
        public override string FilePath { get; set; }
        public SwitchParameter HideComputerName { get; set; }
        public override string[] HostName { get; set; }
        public SwitchParameter InDisconnectedSession { get; set; }
        public string JobName { get; set; }
        public override string KeyFilePath { get; set; }
        public SwitchParameter NoNewScope { get; set; }
        public override int Port { get; set; }
        public virtual SwitchParameter RemoteDebug { get; set; }
        public override SwitchParameter RunAsAdministrator { get; set; }
        public override ScriptBlock ScriptBlock { get; set; }
        public override PSSession[] Session { get; set; }
        public string[] SessionName { get; set; }
        public override PSSessionOption SessionOption { get; set; }
        public override Hashtable[] SSHConnection { get; set; }
        public override SwitchParameter SSHTransport { get; set; }
        public override int ThrottleLimit { get; set; }
        public override string UserName { get; set; }
        public override SwitchParameter UseSSL { get; set; }
        protected override void BeginProcessing();
        public void Dispose();
        protected override void EndProcessing();
        protected override void ProcessRecord();
        protected override void StopProcessing();
    }
    public class InvokeHistoryCommand : PSCmdlet
    {
        public InvokeHistoryCommand();
        public string Id { get; set; }
        protected override void EndProcessing();
    }
    public class JobCmdletBase : PSRemotingCmdlet
    {
        public JobCmdletBase();
        public virtual string[] Command { get; set; }
        public virtual Hashtable Filter { get; set; }
        public virtual int[] Id { get; set; }
        public Guid[] InstanceId { get; set; }
        public string[] Name { get; set; }
        public virtual JobState State { get; set; }
        protected override void BeginProcessing();
    }
    public class ModuleCmdletBase : PSCmdlet
    {
        public ModuleCmdletBase();
        protected bool AddToAppDomainLevelCache { get; set; }
        protected object[] BaseArgumentList { get; set; }
        protected bool BaseDisableNameChecking { get; set; }
        protected internal void ImportModuleMembers(PSModuleInfo sourceModule, string prefix);
        protected internal void ImportModuleMembers(PSModuleInfo sourceModule, string prefix, ModuleCmdletBase.ImportModuleOptions options);
        protected internal struct ImportModuleOptions
        {
        }
    }
    public class ModuleSpecification
    {
        public ModuleSpecification();
        public ModuleSpecification(Hashtable moduleSpecification);
        public ModuleSpecification(string moduleName);
        public Nullable<Guid> Guid { get; }
        public string MaximumVersion { get; }
        public string Name { get; }
        public Version RequiredVersion { get; }
        public Version Version { get; }
        public override string ToString();
        public static bool TryParse(string input, out ModuleSpecification result);
    }
    public sealed class NewModuleCommand : ModuleCmdletBase
    {
        public NewModuleCommand();
        public object[] ArgumentList { get; set; }
        public SwitchParameter AsCustomObject { get; set; }
        public string[] Cmdlet { get; set; }
        public string[] Function { get; set; }
        public string Name { get; set; }
        public SwitchParameter ReturnResult { get; set; }
        public ScriptBlock ScriptBlock { get; set; }
        protected override void EndProcessing();
    }
    public sealed class NewModuleManifestCommand : PSCmdlet
    {
        public NewModuleManifestCommand();
        public string[] AliasesToExport { get; set; }
        public string Author { get; set; }
        public Version ClrVersion { get; set; }
        public string[] CmdletsToExport { get; set; }
        public string CompanyName { get; set; }
        public string[] CompatiblePSEditions { get; set; }
        public string Copyright { get; set; }
        public string DefaultCommandPrefix { get; set; }
        public string Description { get; set; }
        public Version DotNetFrameworkVersion { get; set; }
        public string[] DscResourcesToExport { get; set; }
        public string[] FileList { get; set; }
        public string[] FormatsToProcess { get; set; }
        public string[] FunctionsToExport { get; set; }
        public Guid Guid { get; set; }
        public string HelpInfoUri { get; set; }
        public Uri IconUri { get; set; }
        public Uri LicenseUri { get; set; }
        public object[] ModuleList { get; set; }
        public Version ModuleVersion { get; set; }
        public object[] NestedModules { get; set; }
        public SwitchParameter PassThru { get; set; }
        public string Path { get; set; }
        public string PowerShellHostName { get; set; }
        public Version PowerShellHostVersion { get; set; }
        public Version PowerShellVersion { get; set; }
        public object PrivateData { get; set; }
        public ProcessorArchitecture ProcessorArchitecture { get; set; }
        public Uri ProjectUri { get; set; }
        public string ReleaseNotes { get; set; }
        public string[] RequiredAssemblies { get; set; }
        public object[] RequiredModules { get; set; }
        public string RootModule { get; set; }
        public string[] ScriptsToProcess { get; set; }
        public string[] Tags { get; set; }
        public string[] TypesToProcess { get; set; }
        public string[] VariablesToExport { get; set; }
        protected override void EndProcessing();
    }
    public class NewPSRoleCapabilityFileCommand : PSCmdlet
    {
        public NewPSRoleCapabilityFileCommand();
        public IDictionary[] AliasDefinitions { get; set; }
        public string[] AssembliesToLoad { get; set; }
        public string Author { get; set; }
        public string CompanyName { get; set; }
        public string Copyright { get; set; }
        public string Description { get; set; }
        public IDictionary EnvironmentVariables { get; set; }
        public string[] FormatsToProcess { get; set; }
        public IDictionary[] FunctionDefinitions { get; set; }
        public Guid Guid { get; set; }
        public object[] ModulesToImport { get; set; }
        public string Path { get; set; }
        public string[] ScriptsToProcess { get; set; }
        public string[] TypesToProcess { get; set; }
        public object VariableDefinitions { get; set; }
        public string[] VisibleAliases { get; set; }
        public object[] VisibleCmdlets { get; set; }
        public string[] VisibleExternalCommands { get; set; }
        public object[] VisibleFunctions { get; set; }
        public string[] VisibleProviders { get; set; }
        protected override void ProcessRecord();
    }
    public class NewPSSessionCommand : PSRemotingBaseCmdlet, IDisposable
    {
        public NewPSSessionCommand();
        public override string[] ComputerName { get; set; }
        public string ConfigurationName { get; set; }
        public override PSCredential Credential { get; set; }
        public SwitchParameter EnableNetworkAccess { get; set; }
        public string[] Name { get; set; }
        public override PSSession[] Session { get; set; }
        protected override void BeginProcessing();
        public void Dispose();
        protected void Dispose(bool disposing);
        protected override void EndProcessing();
        protected override void ProcessRecord();
        protected override void StopProcessing();
    }
    public class NewPSSessionConfigurationFileCommand : PSCmdlet
    {
        public NewPSSessionConfigurationFileCommand();
        public IDictionary[] AliasDefinitions { get; set; }
        public string[] AssembliesToLoad { get; set; }
        public string Author { get; set; }
        public string CompanyName { get; set; }
        public string Copyright { get; set; }
        public string Description { get; set; }
        public IDictionary EnvironmentVariables { get; set; }
        public ExecutionPolicy ExecutionPolicy { get; set; }
        public string[] FormatsToProcess { get; set; }
        public SwitchParameter Full { get; set; }
        public IDictionary[] FunctionDefinitions { get; set; }
        public string GroupManagedServiceAccount { get; set; }
        public Guid Guid { get; set; }
        public PSLanguageMode LanguageMode { get; set; }
        public object[] ModulesToImport { get; set; }
        public SwitchParameter MountUserDrive { get; set; }
        public string Path { get; set; }
        public Version PowerShellVersion { get; set; }
        public IDictionary RequiredGroups { get; set; }
        public IDictionary RoleDefinitions { get; set; }
        public SwitchParameter RunAsVirtualAccount { get; set; }
        public string[] RunAsVirtualAccountGroups { get; set; }
        public Version SchemaVersion { get; set; }
        public string[] ScriptsToProcess { get; set; }
        public SessionType SessionType { get; set; }
        public string TranscriptDirectory { get; set; }
        public string[] TypesToProcess { get; set; }
        public long UserDriveMaximumSize { get; set; }
        public object VariableDefinitions { get; set; }
        public string[] VisibleAliases { get; set; }
        public object[] VisibleCmdlets { get; set; }
        public string[] VisibleExternalCommands { get; set; }
        public object[] VisibleFunctions { get; set; }
        public string[] VisibleProviders { get; set; }
        protected override void ProcessRecord();
    }
    public sealed class NewPSSessionOptionCommand : PSCmdlet
    {
        public NewPSSessionOptionCommand();
        public PSPrimitiveDictionary ApplicationArguments { get; set; }
        public int CancelTimeout { get; set; }
        public CultureInfo Culture { get; set; }
        public int IdleTimeout { get; set; }
        public SwitchParameter IncludePortInSPN { get; set; }
        public int MaxConnectionRetryCount { get; set; }
        public int MaximumReceivedDataSizePerCommand { get; set; }
        public int MaximumReceivedObjectSize { get; set; }
        public int MaximumRedirection { get; set; }
        public SwitchParameter NoCompression { get; set; }
        public SwitchParameter NoEncryption { get; set; }
        public SwitchParameter NoMachineProfile { get; set; }
        public int OpenTimeout { get; set; }
        public int OperationTimeout { get; set; }
        public OutputBufferingMode OutputBufferingMode { get; set; }
        public ProxyAccessType ProxyAccessType { get; set; }
        public AuthenticationMechanism ProxyAuthentication { get; set; }
        public PSCredential ProxyCredential { get; set; }
        public SwitchParameter SkipCACheck { get; set; }
        public SwitchParameter SkipCNCheck { get; set; }
        public SwitchParameter SkipRevocationCheck { get; set; }
        public CultureInfo UICulture { get; set; }
        public SwitchParameter UseUTF16 { get; set; }
        protected override void BeginProcessing();
    }
    public sealed class NewPSTransportOptionCommand : PSCmdlet
    {
        public NewPSTransportOptionCommand();
        public Nullable<int> IdleTimeoutSec { get; set; }
        public Nullable<int> MaxConcurrentCommandsPerSession { get; set; }
        public Nullable<int> MaxConcurrentUsers { get; set; }
        public Nullable<int> MaxIdleTimeoutSec { get; set; }
        public Nullable<int> MaxMemoryPerSessionMB { get; set; }
        public Nullable<int> MaxProcessesPerSession { get; set; }
        public Nullable<int> MaxSessions { get; set; }
        public Nullable<int> MaxSessionsPerUser { get; set; }
        public Nullable<OutputBufferingMode> OutputBufferingMode { get; set; }
        public Nullable<int> ProcessIdleTimeoutSec { get; set; }
        protected override void ProcessRecord();
    }
    public class NounArgumentCompleter : IArgumentCompleter
    {
        public NounArgumentCompleter();
        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters);
    }
    public abstract class ObjectEventRegistrationBase : PSCmdlet
    {
        protected ObjectEventRegistrationBase();
        public ScriptBlock Action { get; set; }
        public SwitchParameter Forward { get; set; }
        public int MaxTriggerCount { get; set; }
        public PSObject MessageData { get; set; }
        protected PSEventSubscriber NewSubscriber { get; }
        public string SourceIdentifier { get; set; }
        public SwitchParameter SupportEvent { get; set; }
        protected override void BeginProcessing();
        protected override void EndProcessing();
        protected abstract object GetSourceObject();
        protected abstract string GetSourceObjectEventName();
    }
    public enum OpenMode
    {
        Add = 0,
        New = 1,
        Overwrite = 2,
    }
    public class OutDefaultCommand : FrontEndCommandBase
    {
        public OutDefaultCommand();
        public SwitchParameter Transcript { get; set; }
        protected override void BeginProcessing();
        protected override void EndProcessing();
        protected override void InternalDispose();
        protected override void ProcessRecord();
    }
    public class OutHostCommand : FrontEndCommandBase
    {
        public OutHostCommand();
        public SwitchParameter Paging { get; set; }
        protected override void BeginProcessing();
    }
    public class OutLineOutputCommand : FrontEndCommandBase
    {
        public OutLineOutputCommand();
        public object LineOutput { get; set; }
        protected override void BeginProcessing();
    }
    public class OutNullCommand : PSCmdlet
    {
        public OutNullCommand();
        public PSObject InputObject { get; set; }
        protected override void ProcessRecord();
    }
    public enum OutTarget
    {
        Default = 0,
        Host = 1,
        Job = 2,
    }
    public class PSEditionArgumentCompleter : IArgumentCompleter
    {
        public PSEditionArgumentCompleter();
        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters);
    }
    public abstract class PSExecutionCmdlet : PSRemotingBaseCmdlet
    {
        protected const string FilePathComputerNameParameterSet = "FilePathComputerName";
        protected const string FilePathContainerIdParameterSet = "FilePathContainerId";
        protected const string FilePathSessionParameterSet = "FilePathRunspace";
        protected const string FilePathSSHHostHashParameterSet = "FilePathSSHHostHash";
        protected const string FilePathSSHHostParameterSet = "FilePathSSHHost";
        protected const string FilePathUriParameterSet = "FilePathUri";
        protected const string FilePathVMIdParameterSet = "FilePathVMId";
        protected const string FilePathVMNameParameterSet = "FilePathVMName";
        protected const string LiteralFilePathComputerNameParameterSet = "LiteralFilePathComputerName";
        protected PSExecutionCmdlet();
        public virtual object[] ArgumentList { get; set; }
        public virtual string ConfigurationName { get; set; }
        public override string[] ContainerId { get; set; }
        protected string[] DisconnectedSessionName { get; set; }
        public virtual SwitchParameter EnableNetworkAccess { get; set; }
        public virtual string FilePath { get; set; }
        public virtual PSObject InputObject { get; set; }
        protected bool InvokeAndDisconnect { get; set; }
        protected bool IsLiteralPath { get; set; }
        public virtual ScriptBlock ScriptBlock { get; set; }
        public override Guid[] VMId { get; set; }
        public override string[] VMName { get; set; }
        protected override void BeginProcessing();
        protected void CloseAllInputStreams();
        protected virtual void CreateHelpersForSpecifiedComputerNames();
        protected virtual void CreateHelpersForSpecifiedContainerSession();
        protected void CreateHelpersForSpecifiedRunspaces();
        protected void CreateHelpersForSpecifiedSSHComputerNames();
        protected void CreateHelpersForSpecifiedSSHHashComputerNames();
        protected void CreateHelpersForSpecifiedUris();
        protected virtual void CreateHelpersForSpecifiedVMSession();
        protected ScriptBlock GetScriptBlockFromFile(string filePath, bool isLiteralPath);
    }
    public sealed class PSHostProcessInfo
    {
        public string AppDomainName { get; }
        public int ProcessId { get; }
        public string ProcessName { get; }
    }
    public abstract class PSRemotingBaseCmdlet : PSRemotingCmdlet
    {
        protected const string UriParameterSet = "Uri";
        protected PSRemotingBaseCmdlet();
        public virtual SwitchParameter AllowRedirection { get; set; }
        public virtual string ApplicationName { get; set; }
        public virtual AuthenticationMechanism Authentication { get; set; }
        public virtual string CertificateThumbprint { get; set; }
        public virtual string[] ComputerName { get; set; }
        public virtual Uri[] ConnectionUri { get; set; }
        public virtual string[] ContainerId { get; set; }
        public virtual PSCredential Credential { get; set; }
        public virtual string[] HostName { get; set; }
        public virtual string KeyFilePath { get; set; }
        public virtual int Port { get; set; }
        protected string[] ResolvedComputerNames { get; set; }
        public virtual SwitchParameter RunAsAdministrator { get; set; }
        public virtual PSSession[] Session { get; set; }
        public virtual PSSessionOption SessionOption { get; set; }
        public virtual Hashtable[] SSHConnection { get; set; }
        public virtual SwitchParameter SSHTransport { get; set; }
        public virtual int ThrottleLimit { get; set; }
        public virtual string UserName { get; set; }
        public virtual SwitchParameter UseSSL { get; set; }
        public virtual Guid[] VMId { get; set; }
        public virtual string[] VMName { get; set; }
        protected override void BeginProcessing();
        protected void ValidateComputerName(string[] computerNames);
        protected void ValidateRemoteRunspacesSpecified();
    }
    public abstract class PSRemotingCmdlet : PSCmdlet
    {
        protected const string ComputerInstanceIdParameterSet = "ComputerInstanceId";
        protected const string ComputerNameParameterSet = "ComputerName";
        protected const string ContainerIdParameterSet = "ContainerId";
        protected const string DefaultPowerShellRemoteShellAppName = "WSMan";
        protected const string DefaultPowerShellRemoteShellName = "http://schemas.microsoft.com/powershell/Microsoft.PowerShell";
        protected const string SessionParameterSet = "Session";
        protected const string SSHHostHashParameterSet = "SSHHostHashParam";
        protected const string SSHHostParameterSet = "SSHHost";
        protected const string VMIdParameterSet = "VMId";
        protected const string VMNameParameterSet = "VMName";
        protected PSRemotingCmdlet();
        protected override void BeginProcessing();
        protected string ResolveAppName(string appName);
        protected string ResolveComputerName(string computerName);
        protected void ResolveComputerNames(string[] computerNames, out string[] resolvedComputerNames);
        protected string ResolveShell(string shell);
    }
    public abstract class PSRunspaceCmdlet : PSRemotingCmdlet
    {
        protected const string ContainerIdInstanceIdParameterSet = "ContainerIdInstanceId";
        protected const string IdParameterSet = "Id";
        protected const string InstanceIdParameterSet = "InstanceId";
        protected const string NameParameterSet = "Name";
        protected const string VMIdInstanceIdParameterSet = "VMIdInstanceId";
        protected const string VMNameInstanceIdParameterSet = "VMNameInstanceId";
        protected PSRunspaceCmdlet();
        public virtual string[] ComputerName { get; set; }
        public virtual string[] ContainerId { get; set; }
        public int[] Id { get; set; }
        public virtual Guid[] InstanceId { get; set; }
        public virtual string[] Name { get; set; }
        public virtual Guid[] VMId { get; set; }
        public virtual string[] VMName { get; set; }
        protected Dictionary<Guid, PSSession> GetMatchingRunspaces(bool writeobject, bool writeErrorOnNoMatch);
        protected Dictionary<Guid, PSSession> GetMatchingRunspaces(bool writeobject, bool writeErrorOnNoMatch, SessionFilterState filterState, string configurationName);
        protected Dictionary<Guid, PSSession> GetMatchingRunspacesByName(bool writeobject, bool writeErrorOnNoMatch);
        protected Dictionary<Guid, PSSession> GetMatchingRunspacesByRunspaceId(bool writeobject, bool writeErrorOnNoMatch);
    }
    public class PSSessionConfigurationCommandBase : PSCmdlet
    {
        public PSSessionConfigurationAccessMode AccessMode { get; set; }
        public string ApplicationBase { get; set; }
        public string AssemblyName { get; set; }
        public string ConfigurationTypeName { get; set; }
        public SwitchParameter Force { get; set; }
        public Nullable<double> MaximumReceivedDataSizePerCommandMB { get; set; }
        public Nullable<double> MaximumReceivedObjectSizeMB { get; set; }
        public object[] ModulesToImport { get; set; }
        public string Name { get; set; }
        public SwitchParameter NoServiceRestart { get; set; }
        public string Path { get; set; }
        public Version PSVersion { get; set; }
        public PSCredential RunAsCredential { get; set; }
        protected bool RunAsVirtualAccount { get; set; }
        protected string RunAsVirtualAccountGroups { get; set; }
        protected bool RunAsVirtualAccountSpecified { get; set; }
        public string SecurityDescriptorSddl { get; set; }
        public PSSessionTypeOption SessionTypeOption { get; set; }
        public SwitchParameter ShowSecurityDescriptorUI { get; set; }
        public string StartupScript { get; set; }
        public PSThreadOptions ThreadOptions { get; set; }
        public PSTransportOption TransportOption { get; set; }
        public SwitchParameter UseSharedProcess { get; set; }
    }
    public class ReceiveJobCommand : JobCmdletBase, IDisposable
    {
        protected const string LocationParameterSet = "Location";
        public ReceiveJobCommand();
        public SwitchParameter AutoRemoveJob { get; set; }
        public override string[] Command { get; }
        public string[] ComputerName { get; set; }
        public override Hashtable Filter { get; }
        public SwitchParameter Force { get; set; }
        public Job[] Job { get; set; }
        public SwitchParameter Keep { get; set; }
        public string[] Location { get; set; }
        public SwitchParameter NoRecurse { get; set; }
        public PSSession[] Session { get; set; }
        public override JobState State { get; }
        public SwitchParameter Wait { get; set; }
        public SwitchParameter WriteEvents { get; set; }
        public SwitchParameter WriteJobInResults { get; set; }
        protected override void BeginProcessing();
        public void Dispose();
        protected void Dispose(bool disposing);
        protected override void EndProcessing();
        protected override void ProcessRecord();
        protected override void StopProcessing();
    }
    public class ReceivePSSessionCommand : PSRemotingCmdlet
    {
        public ReceivePSSessionCommand();
        public SwitchParameter AllowRedirection { get; set; }
        public string ApplicationName { get; set; }
        public AuthenticationMechanism Authentication { get; set; }
        public string CertificateThumbprint { get; set; }
        public string ComputerName { get; set; }
        public string ConfigurationName { get; set; }
        public Uri ConnectionUri { get; set; }
        public PSCredential Credential { get; set; }
        public int Id { get; set; }
        public Guid InstanceId { get; set; }
        public string JobName { get; set; }
        public string Name { get; set; }
        public OutTarget OutTarget { get; set; }
        public int Port { get; set; }
        public PSSession Session { get; set; }
        public PSSessionOption SessionOption { get; set; }
        public SwitchParameter UseSSL { get; set; }
        protected override void ProcessRecord();
        protected override void StopProcessing();
    }
    public sealed class RegisterPSSessionConfigurationCommand : PSSessionConfigurationCommandBase
    {
        public RegisterPSSessionConfigurationCommand();
        public string ProcessorArchitecture { get; set; }
        public PSSessionType SessionType { get; set; }
        protected override void BeginProcessing();
        protected override void EndProcessing();
        protected override void ProcessRecord();
    }
    public sealed class RegistryProvider : NavigationCmdletProvider, IDynamicPropertyCmdletProvider, IPropertyCmdletProvider, ISecurityDescriptorCmdletProvider
    {
        public const string ProviderName = "Registry";
        public RegistryProvider();
        protected override void ClearItem(string path);
        public void ClearProperty(string path, Collection<string> propertyToClear);
        public object ClearPropertyDynamicParameters(string path, Collection<string> propertyToClear);
        protected override void CopyItem(string path, string destination, bool recurse);
        public void CopyProperty(string sourcePath, string sourceProperty, string destinationPath, string destinationProperty);
        public object CopyPropertyDynamicParameters(string sourcePath, string sourceProperty, string destinationPath, string destinationProperty);
        protected override void GetChildItems(string path, bool recurse, uint depth);
        protected override string GetChildName(string path);
        protected override void GetChildNames(string path, ReturnContainers returnContainers);
        protected override void GetItem(string path);
        protected override string GetParentPath(string path, string root);
        public void GetProperty(string path, Collection<string> providerSpecificPickList);
        public object GetPropertyDynamicParameters(string path, Collection<string> providerSpecificPickList);
        public void GetSecurityDescriptor(string path, AccessControlSections sections);
        protected override bool HasChildItems(string path);
        protected override Collection<PSDriveInfo> InitializeDefaultDrives();
        protected override bool IsItemContainer(string path);
        protected override bool IsValidPath(string path);
        protected override bool ItemExists(string path);
        protected override void MoveItem(string path, string destination);
        public void MoveProperty(string sourcePath, string sourceProperty, string destinationPath, string destinationProperty);
        public object MovePropertyDynamicParameters(string sourcePath, string sourceProperty, string destinationPath, string destinationProperty);
        protected override PSDriveInfo NewDrive(PSDriveInfo drive);
        protected override void NewItem(string path, string type, object newItem);
        public void NewProperty(string path, string propertyName, string type, object value);
        public object NewPropertyDynamicParameters(string path, string propertyName, string type, object value);
        public ObjectSecurity NewSecurityDescriptorFromPath(string path, AccessControlSections sections);
        public ObjectSecurity NewSecurityDescriptorOfType(string type, AccessControlSections sections);
        protected override void RemoveItem(string path, bool recurse);
        public void RemoveProperty(string path, string propertyName);
        public object RemovePropertyDynamicParameters(string path, string propertyName);
        protected override void RenameItem(string path, string newName);
        public void RenameProperty(string path, string sourceProperty, string destinationProperty);
        public object RenamePropertyDynamicParameters(string path, string sourceProperty, string destinationProperty);
        protected override void SetItem(string path, object value);
        protected override object SetItemDynamicParameters(string path, object value);
        public void SetProperty(string path, PSObject propertyValue);
        public object SetPropertyDynamicParameters(string path, PSObject propertyValue);
        public void SetSecurityDescriptor(string path, ObjectSecurity securityDescriptor);
    }
    public class RegistryProviderSetItemDynamicParameter
    {
        public RegistryProviderSetItemDynamicParameter();
        public RegistryValueKind Type { get; set; }
    }
    public class RemoveJobCommand : JobCmdletBase, IDisposable
    {
        public RemoveJobCommand();
        public SwitchParameter Force { get; set; }
        public Job[] Job { get; set; }
        public void Dispose();
        protected void Dispose(bool disposing);
        protected override void EndProcessing();
        protected override void ProcessRecord();
        protected override void StopProcessing();
    }
    public sealed class RemoveModuleCommand : ModuleCmdletBase
    {
        public RemoveModuleCommand();
        public SwitchParameter Force { get; set; }
        public ModuleSpecification[] FullyQualifiedName { get; set; }
        public PSModuleInfo[] ModuleInfo { get; set; }
        public string[] Name { get; set; }
        protected override void EndProcessing();
        protected override void ProcessRecord();
    }
    public class RemovePSSessionCommand : PSRunspaceCmdlet
    {
        public RemovePSSessionCommand();
        public override string[] ContainerId { get; set; }
        public PSSession[] Session { get; set; }
        public override Guid[] VMId { get; set; }
        public override string[] VMName { get; set; }
        protected override void ProcessRecord();
    }
    public class ResumeJobCommand : JobCmdletBase, IDisposable
    {
        public ResumeJobCommand();
        public override string[] Command { get; }
        public Job[] Job { get; set; }
        public SwitchParameter Wait { get; set; }
        public void Dispose();
        protected void Dispose(bool disposing);
        protected override void EndProcessing();
        protected override void ProcessRecord();
        protected override void StopProcessing();
    }
    public sealed class SaveHelpCommand : UpdatableHelpCommandBase
    {
        public SaveHelpCommand();
        public string[] DestinationPath { get; set; }
        public ModuleSpecification[] FullyQualifiedModule { get; set; }
        public string[] LiteralPath { get; set; }
        public PSModuleInfo[] Module { get; set; }
        protected override void ProcessRecord();
    }
    public enum SessionFilterState
    {
        All = 0,
        Broken = 4,
        Closed = 3,
        Disconnected = 2,
        Opened = 1,
    }
    public abstract class SessionStateProviderBase : ContainerCmdletProvider, IContentCmdletProvider
    {
        protected SessionStateProviderBase();
        public void ClearContent(string path);
        public object ClearContentDynamicParameters(string path);
        protected override void ClearItem(string path);
        protected override void CopyItem(string path, string copyPath, bool recurse);
        protected override void GetChildItems(string path, bool recurse);
        protected override void GetChildNames(string path, ReturnContainers returnContainers);
        public IContentReader GetContentReader(string path);
        public object GetContentReaderDynamicParameters(string path);
        public IContentWriter GetContentWriter(string path);
        public object GetContentWriterDynamicParameters(string path);
        protected override void GetItem(string name);
        protected override bool HasChildItems(string path);
        protected override bool IsValidPath(string path);
        protected override bool ItemExists(string path);
        protected override void NewItem(string path, string type, object newItem);
        protected override void RemoveItem(string path, bool recurse);
        protected override void RenameItem(string name, string newName);
        protected override void SetItem(string name, object value);
    }
    public class SessionStateProviderBaseContentReaderWriter : IContentReader, IContentWriter, IDisposable
    {
        public void Close();
        public void Dispose();
        public IList Read(long readCount);
        public void Seek(long offset, SeekOrigin origin);
        public IList Write(IList content);
    }
    public sealed class SetPSDebugCommand : PSCmdlet
    {
        public SetPSDebugCommand();
        public SwitchParameter Off { get; set; }
        public SwitchParameter Step { get; set; }
        public SwitchParameter Strict { get; set; }
        public int Trace { get; set; }
        protected override void BeginProcessing();
    }
    public sealed class SetPSSessionConfigurationCommand : PSSessionConfigurationCommandBase
    {
        public SetPSSessionConfigurationCommand();
        protected override void BeginProcessing();
        protected override void EndProcessing();
        protected override void ProcessRecord();
    }
    public class SetStrictModeCommand : PSCmdlet
    {
        public SetStrictModeCommand();
        public SwitchParameter Off { get; set; }
        public Version Version { get; set; }
        protected override void EndProcessing();
    }
    public class StartJobCommand : PSExecutionCmdlet, IDisposable
    {
        public StartJobCommand();
        public override SwitchParameter AllowRedirection { get; }
        public override string ApplicationName { get; }
        public override object[] ArgumentList { get; set; }
        public override AuthenticationMechanism Authentication { get; set; }
        public override string CertificateThumbprint { get; set; }
        public override string[] ComputerName { get; }
        public override string ConfigurationName { get; set; }
        public override Uri[] ConnectionUri { get; }
        public override string[] ContainerId { get; }
        public override PSCredential Credential { get; set; }
        public string DefinitionName { get; set; }
        public string DefinitionPath { get; set; }
        public override SwitchParameter EnableNetworkAccess { get; }
        public override string FilePath { get; set; }
        public virtual ScriptBlock InitializationScript { get; set; }
        public override PSObject InputObject { get; set; }
        public override string KeyFilePath { get; }
        public string LiteralPath { get; set; }
        public virtual string Name { get; set; }
        public override int Port { get; }
        public virtual Version PSVersion { get; set; }
        public virtual SwitchParameter RunAs32 { get; set; }
        public override SwitchParameter RunAsAdministrator { get; }
        public override ScriptBlock ScriptBlock { get; set; }
        public override PSSession[] Session { get; }
        public override PSSessionOption SessionOption { get; set; }
        public override Hashtable[] SSHConnection { get; }
        public override SwitchParameter SSHTransport { get; }
        public override int ThrottleLimit { get; }
        public string Type { get; set; }
        public override string UserName { get; }
        public override SwitchParameter UseSSL { get; }
        public override Guid[] VMId { get; }
        public override string[] VMName { get; }
        protected override void BeginProcessing();
        protected override void CreateHelpersForSpecifiedComputerNames();
        public void Dispose();
        protected override void EndProcessing();
        protected override void ProcessRecord();
    }
    public class StopJobCommand : JobCmdletBase, IDisposable
    {
        public StopJobCommand();
        public override string[] Command { get; }
        public Job[] Job { get; set; }
        public SwitchParameter PassThru { get; set; }
        public void Dispose();
        protected void Dispose(bool disposing);
        protected override void EndProcessing();
        protected override void ProcessRecord();
        protected override void StopProcessing();
    }
    public class SuspendJobCommand : JobCmdletBase, IDisposable
    {
        public SuspendJobCommand();
        public override string[] Command { get; }
        public SwitchParameter Force { get; set; }
        public Job[] Job { get; set; }
        public SwitchParameter Wait { get; set; }
        public void Dispose();
        protected void Dispose(bool disposing);
        protected override void EndProcessing();
        protected override void ProcessRecord();
        protected override void StopProcessing();
    }
    public sealed class TestModuleManifestCommand : ModuleCmdletBase
    {
        public TestModuleManifestCommand();
        public string Path { get; set; }
        protected override void ProcessRecord();
    }
    public class TestPSSessionConfigurationFileCommand : PSCmdlet
    {
        public TestPSSessionConfigurationFileCommand();
        public string Path { get; set; }
        protected override void ProcessRecord();
    }
    public sealed class UnregisterPSSessionConfigurationCommand : PSCmdlet
    {
        public UnregisterPSSessionConfigurationCommand();
        public SwitchParameter Force { get; set; }
        public string Name { get; set; }
        public SwitchParameter NoServiceRestart { get; set; }
        protected override void BeginProcessing();
        protected override void EndProcessing();
        protected override void ProcessRecord();
    }
    public class UpdatableHelpCommandBase : PSCmdlet
    {
        public PSCredential Credential { get; set; }
        public SwitchParameter Force { get; set; }
        public CultureInfo[] UICulture { get; set; }
        public SwitchParameter UseDefaultCredentials { get; set; }
        protected override void EndProcessing();
        protected override void StopProcessing();
    }
    public sealed class UpdateHelpCommand : UpdatableHelpCommandBase
    {
        public UpdateHelpCommand();
        public ModuleSpecification[] FullyQualifiedModule { get; set; }
        public string[] LiteralPath { get; set; }
        public string[] Module { get; set; }
        public SwitchParameter Recurse { get; set; }
        public string[] SourcePath { get; set; }
        protected override void BeginProcessing();
        protected override void ProcessRecord();
    }
    public sealed class VariableProvider : SessionStateProviderBase
    {
        public const string ProviderName = "Variable";
        public VariableProvider();
        protected override Collection<PSDriveInfo> InitializeDefaultDrives();
    }
    public class WaitJobCommand : JobCmdletBase, IDisposable
    {
        public WaitJobCommand();
        public SwitchParameter Any { get; set; }
        public override string[] Command { get; set; }
        public SwitchParameter Force { get; set; }
        public Job[] Job { get; set; }
        public int Timeout { get; set; }
        protected override void BeginProcessing();
        public void Dispose();
        protected override void EndProcessing();
        protected override void ProcessRecord();
        protected override void StopProcessing();
    }
    public sealed class WhereObjectCommand : PSCmdlet
    {
        public WhereObjectCommand();
        public SwitchParameter CContains { get; set; }
        public SwitchParameter CEQ { get; set; }
        public SwitchParameter CGE { get; set; }
        public SwitchParameter CGT { get; set; }
        public SwitchParameter CIn { get; set; }
        public SwitchParameter CLE { get; set; }
        public SwitchParameter CLike { get; set; }
        public SwitchParameter CLT { get; set; }
        public SwitchParameter CMatch { get; set; }
        public SwitchParameter CNE { get; set; }
        public SwitchParameter CNotContains { get; set; }
        public SwitchParameter CNotIn { get; set; }
        public SwitchParameter CNotLike { get; set; }
        public SwitchParameter CNotMatch { get; set; }
        public SwitchParameter Contains { get; set; }
        public SwitchParameter EQ { get; set; }
        public ScriptBlock FilterScript { get; set; }
        public SwitchParameter GE { get; set; }
        public SwitchParameter GT { get; set; }
        public SwitchParameter In { get; set; }
        public PSObject InputObject { get; set; }
        public SwitchParameter Is { get; set; }
        public SwitchParameter IsNot { get; set; }
        public SwitchParameter LE { get; set; }
        public SwitchParameter Like { get; set; }
        public SwitchParameter LT { get; set; }
        public SwitchParameter Match { get; set; }
        public SwitchParameter NE { get; set; }
        public SwitchParameter NotContains { get; set; }
        public SwitchParameter NotIn { get; set; }
        public SwitchParameter NotLike { get; set; }
        public SwitchParameter NotMatch { get; set; }
        public string Property { get; set; }
        public object Value { get; set; }
        protected override void BeginProcessing();
        protected override void ProcessRecord();
    }
    public class WSManConfigurationOption : PSTransportOption
    {
        public Nullable<int> IdleTimeoutSec { get; }
        public Nullable<int> MaxConcurrentCommandsPerSession { get; }
        public Nullable<int> MaxConcurrentUsers { get; }
        public Nullable<int> MaxIdleTimeoutSec { get; }
        public Nullable<int> MaxMemoryPerSessionMB { get; }
        public Nullable<int> MaxProcessesPerSession { get; }
        public Nullable<int> MaxSessions { get; }
        public Nullable<int> MaxSessionsPerUser { get; }
        public Nullable<OutputBufferingMode> OutputBufferingMode { get; }
        public Nullable<int> ProcessIdleTimeoutSec { get; }
        protected internal override void LoadFromDefaults(PSSessionType sessionType, bool keepAssigned);
    }
}
namespace Microsoft.PowerShell.Commands.Internal
{
    public static class RemotingErrorResources
    {
        public static string CouldNotResolveRoleDefinitionPrincipal { get; }
        public static string WinRMRestartWarning { get; }
    }
}
namespace Microsoft.PowerShell.Commands.Internal.Format
{
    public abstract class FrontEndCommandBase : PSCmdlet, IDisposable
    {
        protected FrontEndCommandBase();
        public PSObject InputObject { get; set; }
        protected override void BeginProcessing();
        public void Dispose();
        protected virtual void Dispose(bool disposing);
        protected override void EndProcessing();
        protected virtual PSObject InputObjectCall();
        protected virtual void InternalDispose();
        protected virtual PSCmdlet OuterCmdletCall();
        protected override void ProcessRecord();
        protected override void StopProcessing();
        protected virtual void WriteObjectCall(object value);
    }
    public class OuterFormatShapeCommandBase : FrontEndCommandBase
    {
        public OuterFormatShapeCommandBase();
        public SwitchParameter DisplayError { get; set; }
        public string Expand { get; set; }
        public SwitchParameter Force { get; set; }
        public object GroupBy { get; set; }
        public SwitchParameter ShowError { get; set; }
        public string View { get; set; }
        protected override void BeginProcessing();
    }
    public class OuterFormatTableAndListBase : OuterFormatShapeCommandBase
    {
        public OuterFormatTableAndListBase();
        public object[] Property { get; set; }
    }
    public class OuterFormatTableBase : OuterFormatTableAndListBase
    {
        public OuterFormatTableBase();
        public SwitchParameter AutoSize { get; set; }
        public SwitchParameter HideTableHeaders { get; set; }
        public SwitchParameter Wrap { get; set; }
    }
}
namespace Microsoft.PowerShell.CoreCLR
{
    public static class AssemblyExtensions
    {
        public static Assembly LoadFrom(Stream assembly);
        public static Assembly LoadFrom(string assemblyPath);
    }
}
namespace Microsoft.PowerShell.CoreClr.Stubs
{
    public enum AuthenticationLevel
    {
        Call = 3,
        Connect = 2,
        Default = 0,
        None = 1,
        Packet = 4,
        PacketIntegrity = 5,
        PacketPrivacy = 6,
        Unchanged = -1,
    }
    public enum ImpersonationLevel
    {
        Anonymous = 1,
        Default = 0,
        Delegate = 4,
        Identify = 2,
        Impersonate = 3,
    }
    public enum SecurityZone
    {
        Internet = 3,
        Intranet = 1,
        MyComputer = 0,
        NoZone = -1,
        Trusted = 2,
        Untrusted = 4,
    }
}
namespace Microsoft.PowerShell.DesiredStateConfiguration
{
    public sealed class ArgumentToConfigurationDataTransformationAttribute : ArgumentTransformationAttribute
    {
        public ArgumentToConfigurationDataTransformationAttribute();
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData);
    }
}
namespace Microsoft.PowerShell.DesiredStateConfiguration.Internal
{
    public static class DscClassCache
    {
        public static void ClearCache();
        public static ErrorRecord DebugModeShouldHaveOneValue();
        public static ErrorRecord DisabledRefreshModeNotValidForPartialConfig(string resourceId);
        public static ErrorRecord DuplicateResourceIdInNodeStatementErrorRecord(string duplicateResourceId, string nodeName);
        public static string GenerateMofForType(Type type);
        public static ErrorRecord GetBadlyFormedExclusiveResourceIdErrorRecord(string badExclusiveResourcereference, string definingResource);
        public static ErrorRecord GetBadlyFormedRequiredResourceIdErrorRecord(string badDependsOnReference, string definingResource);
        public static List<CimClass> GetCachedClassByFileName(string fileName);
        public static List<CimClass> GetCachedClassByModuleName(string moduleName);
        public static List<Tuple<DSCResourceRunAsCredential, CimClass>> GetCachedClasses();
        public static List<CimClass> GetCachedClassesForModule(PSModuleInfo module);
        public static Collection<DynamicKeyword> GetCachedKeywords();
        public static string GetDSCResourceUsageString(DynamicKeyword keyword);
        public static List<string> GetFileDefiningClass(string className);
        public static string[] GetLoadedFiles();
        public static ErrorRecord GetPullModeNeedConfigurationSource(string resourceId);
        public static bool GetResourceMethodsLinePosition(PSModuleInfo moduleInfo, string resourceName, out Dictionary<string, int> resourceMethodsLinePosition, out string resourceFilePath);
        public static string GetStringFromSecureString(SecureString value);
        public static bool ImportCimKeywordsFromModule(PSModuleInfo module, string resourceName, out string schemaFilePath);
        public static bool ImportCimKeywordsFromModule(PSModuleInfo module, string resourceName, out string schemaFilePath, Dictionary<string, ScriptBlock> functionsToDefine);
        public static bool ImportCimKeywordsFromModule(PSModuleInfo module, string resourceName, out string schemaFilePath, Dictionary<string, ScriptBlock> functionsToDefine, Collection<Exception> errors);
        public static List<CimClass> ImportClasses(string path, Tuple<string, Version> moduleInfo, Collection<Exception> errors);
        public static List<string> ImportClassResourcesFromModule(PSModuleInfo moduleInfo, ICollection<string> resourcesToImport, Dictionary<string, ScriptBlock> functionsToDefine);
        public static List<CimInstance> ImportInstances(string path);
        public static List<CimInstance> ImportInstances(string path, int schemaValidationOption);
        public static bool ImportScriptKeywordsFromModule(PSModuleInfo module, string resourceName, out string schemaFilePath);
        public static bool ImportScriptKeywordsFromModule(PSModuleInfo module, string resourceName, out string schemaFilePath, Dictionary<string, ScriptBlock> functionsToDefine);
        public static void Initialize();
        public static void Initialize(Collection<Exception> errors, List<string> modulePathList);
        public static ErrorRecord InvalidConfigurationNameErrorRecord(string configurationName);
        public static ErrorRecord InvalidLocalConfigurationManagerPropertyErrorRecord(string propertyName, string validProperties);
        public static ErrorRecord InvalidValueForPropertyErrorRecord(string propertyName, string value, string keywordName, string validValues);
        public static void LoadDefaultCimKeywords();
        public static void LoadDefaultCimKeywords(Dictionary<string, ScriptBlock> functionsToDefine);
        public static void LoadDefaultCimKeywords(List<string> modulePathList);
        public static void LoadDefaultCimKeywords(Collection<Exception> errors);
        public static void LoadDefaultCimKeywords(Collection<Exception> errors, bool cacheResourcesFromMultipleModuleVersions);
        public static void LoadResourcesFromModule(IScriptExtent scriptExtent, ModuleSpecification[] moduleSpecifications, string[] resourceNames, List<ParseError> errorList);
        public static ErrorRecord MissingValueForMandatoryPropertyErrorRecord(string keywordName, string typeName, string propertyName);
        public static ErrorRecord PsDscRunAsCredentialMergeErrorForCompositeResources(string resourceId);
        public static ErrorRecord UnsupportedValueForPropertyErrorRecord(string propertyName, string value, string keywordName, string validValues);
        public static void ValidateInstanceText(string instanceText);
        public static ErrorRecord ValueNotInRangeErrorRecord(string property, string name, int providedValue, int lower, int upper);
    }
    public static class DscRemoteOperationsClass
    {
        public static object ConvertCimInstanceToObject(Type targetType, CimInstance instance, string moduleName);
    }
}
namespace Microsoft.PowerShell.Telemetry.Internal
{
    public interface IHostProvidesTelemetryData
    {
        bool HostIsInteractive { get; }
        int InteractiveCommandCount { get; }
        double ProfileLoadTimeInMS { get; }
        double ReadyForInputTimeInMS { get; }
    }
    public static class TelemetryAPI
    {
        public static void ReportExitTelemetry(IHostProvidesTelemetryData ihptd);
        public static void ReportStartupTelemetry(IHostProvidesTelemetryData ihptd);
        public static void TraceMessage<T>(string message, T arguments);
    }
}
namespace System.Management.Automation
{
    public enum ActionPreference
    {
        Continue = 2,
        Ignore = 4,
        Inquire = 3,
        SilentlyContinue = 0,
        Stop = 1,
        Suspend = 5,
    }
    public class ActionPreferenceStopException : RuntimeException
    {
        public ActionPreferenceStopException();
        protected ActionPreferenceStopException(SerializationInfo info, StreamingContext context);
        public ActionPreferenceStopException(string message);
        public ActionPreferenceStopException(string message, Exception innerException);
        public override ErrorRecord ErrorRecord { get; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
    }
    public sealed class AliasAttribute : ParsingBaseAttribute
    {
        public AliasAttribute(params string[] aliasNames);
        public IList<string> AliasNames { get; }
    }
    public class AliasInfo : CommandInfo
    {
        public override string Definition { get; }
        public string Description { get; set; }
        public ScopedItemOptions Options { get; set; }
        public override ReadOnlyCollection<PSTypeName> OutputType { get; }
        public CommandInfo ReferencedCommand { get; }
        public CommandInfo ResolvedCommand { get; }
    }
    public enum Alignment
    {
        Center = 2,
        Left = 1,
        Right = 3,
        Undefined = 0,
    }
    public sealed class AllowEmptyCollectionAttribute : CmdletMetadataAttribute
    {
        public AllowEmptyCollectionAttribute();
    }
    public sealed class AllowEmptyStringAttribute : CmdletMetadataAttribute
    {
        public AllowEmptyStringAttribute();
    }
    public sealed class AllowNullAttribute : CmdletMetadataAttribute
    {
        public AllowNullAttribute();
    }
    public class ApplicationFailedException : RuntimeException
    {
        public ApplicationFailedException();
        protected ApplicationFailedException(SerializationInfo info, StreamingContext context);
        public ApplicationFailedException(string message);
        public ApplicationFailedException(string message, Exception innerException);
    }
    public class ApplicationInfo : CommandInfo
    {
        public override string Definition { get; }
        public string Extension { get; }
        public override ReadOnlyCollection<PSTypeName> OutputType { get; }
        public string Path { get; }
        public override string Source { get; }
        public override Version Version { get; }
        public override SessionStateEntryVisibility Visibility { get; set; }
    }
    public class ArgumentCompleterAttribute : Attribute
    {
        public ArgumentCompleterAttribute(ScriptBlock scriptBlock);
        public ArgumentCompleterAttribute(Type type);
        public ScriptBlock ScriptBlock { get; }
        public Type Type { get; }
    }
    public abstract class ArgumentTransformationAttribute : CmdletMetadataAttribute
    {
        protected ArgumentTransformationAttribute();
        public virtual bool TransformNullOptionalParameters { get; }
        public abstract object Transform(EngineIntrinsics engineIntrinsics, object inputData);
    }
    public class ArgumentTransformationMetadataException : MetadataException
    {
        public ArgumentTransformationMetadataException();
        protected ArgumentTransformationMetadataException(SerializationInfo info, StreamingContext context);
        public ArgumentTransformationMetadataException(string message);
        public ArgumentTransformationMetadataException(string message, Exception innerException);
    }
    public class AuthorizationManager
    {
        public AuthorizationManager(string shellId);
        protected internal virtual bool ShouldRun(CommandInfo commandInfo, CommandOrigin origin, PSHost host, out Exception reason);
    }
    public sealed class BreakException : LoopFlowException
    {
    }
    public abstract class Breakpoint
    {
        public ScriptBlock Action { get; }
        public bool Enabled { get; }
        public int HitCount { get; }
        public int Id { get; }
        public string Script { get; }
    }
    public class BreakpointUpdatedEventArgs : EventArgs
    {
        public Breakpoint Breakpoint { get; }
        public int BreakpointCount { get; }
        public BreakpointUpdateType UpdateType { get; }
    }
    public enum BreakpointUpdateType
    {
        Disabled = 3,
        Enabled = 2,
        Removed = 1,
        Set = 0,
    }
    public sealed class CallStackFrame
    {
        public CallStackFrame(InvocationInfo invocationInfo);
        public string FunctionName { get; }
        public InvocationInfo InvocationInfo { get; }
        public IScriptExtent Position { get; }
        public int ScriptLineNumber { get; }
        public string ScriptName { get; }
        public Dictionary<string, PSVariable> GetFrameVariables();
        public string GetScriptLocation();
        public override string ToString();
    }
    public class CatalogInformation
    {
        public CatalogInformation();
        public Dictionary<string, string> CatalogItems { get; set; }
        public string HashAlgorithm { get; set; }
        public Dictionary<string, string> PathItems { get; set; }
        public Signature Signature { get; set; }
        public CatalogValidationStatus Status { get; set; }
    }
    public enum CatalogValidationStatus
    {
        Valid = 0,
        ValidationFailed = 1,
    }
    public sealed class ChildItemCmdletProviderIntrinsics
    {
        public Collection<PSObject> Get(string path, bool recurse);
        public Collection<PSObject> Get(string[] path, bool recurse, bool force, bool literalPath);
        public Collection<PSObject> Get(string[] path, bool recurse, uint depth, bool force, bool literalPath);
        public Collection<string> GetNames(string path, ReturnContainers returnContainers, bool recurse);
        public Collection<string> GetNames(string[] path, ReturnContainers returnContainers, bool recurse, bool force, bool literalPath);
        public Collection<string> GetNames(string[] path, ReturnContainers returnContainers, bool recurse, uint depth, bool force, bool literalPath);
        public bool HasChild(string path);
        public bool HasChild(string path, bool force, bool literalPath);
    }
    public abstract class Cmdlet : InternalCommand
    {
        protected Cmdlet();
        public ICommandRuntime CommandRuntime { get; set; }
        public static HashSet<string> CommonParameters { get; }
        public PSTransactionContext CurrentPSTransaction { get; }
        public static HashSet<string> OptionalCommonParameters { get; }
        public bool Stopping { get; }
        protected virtual void BeginProcessing();
        protected virtual void EndProcessing();
        public virtual string GetResourceString(string baseName, string resourceId);
        public IEnumerable Invoke();
        public IEnumerable<T> Invoke<T>();
        protected virtual void ProcessRecord();
        public bool ShouldContinue(string query, string caption);
        public bool ShouldContinue(string query, string caption, bool hasSecurityImpact, ref bool yesToAll, ref bool noToAll);
        public bool ShouldContinue(string query, string caption, ref bool yesToAll, ref bool noToAll);
        public bool ShouldProcess(string target);
        public bool ShouldProcess(string target, string action);
        public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption);
        public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption, out ShouldProcessReason shouldProcessReason);
        protected virtual void StopProcessing();
        public void ThrowTerminatingError(ErrorRecord errorRecord);
        public bool TransactionAvailable();
        public void WriteCommandDetail(string text);
        public void WriteDebug(string text);
        public void WriteError(ErrorRecord errorRecord);
        public void WriteInformation(InformationRecord informationRecord);
        public void WriteInformation(object messageData, string[] tags);
        public void WriteObject(object sendToPipeline);
        public void WriteObject(object sendToPipeline, bool enumerateCollection);
        public void WriteProgress(ProgressRecord progressRecord);
        public void WriteVerbose(string text);
        public void WriteWarning(string text);
    }
    public sealed class CmdletAttribute : CmdletCommonMetadataAttribute
    {
        public CmdletAttribute(string verbName, string nounName);
        public string NounName { get; }
        public string VerbName { get; }
    }
    public class CmdletBindingAttribute : CmdletCommonMetadataAttribute
    {
        public CmdletBindingAttribute();
        public bool PositionalBinding { get; set; }
    }
    public abstract class CmdletCommonMetadataAttribute : CmdletMetadataAttribute
    {
        protected CmdletCommonMetadataAttribute();
        public ConfirmImpact ConfirmImpact { get; set; }
        public string DefaultParameterSetName { get; set; }
        public string HelpUri { get; set; }
        public RemotingCapability RemotingCapability { get; set; }
        public bool SupportsPaging { get; set; }
        public bool SupportsShouldProcess { get; set; }
        public bool SupportsTransactions { get; set; }
    }
    public class CmdletInfo : CommandInfo
    {
        public CmdletInfo(string name, Type implementingType);
        public string DefaultParameterSet { get; }
        public override string Definition { get; }
        public string HelpFile { get; }
        public Type ImplementingType { get; }
        public string Noun { get; }
        public ScopedItemOptions Options { get; set; }
        public override ReadOnlyCollection<PSTypeName> OutputType { get; }
        public PSSnapInInfo PSSnapIn { get; }
        public string Verb { get; }
        public override Version Version { get; }
    }
    public class CmdletInvocationException : RuntimeException
    {
        public CmdletInvocationException();
        protected CmdletInvocationException(SerializationInfo info, StreamingContext context);
        public CmdletInvocationException(string message);
        public CmdletInvocationException(string message, Exception innerException);
        public override ErrorRecord ErrorRecord { get; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
    }
    public class CmdletProviderInvocationException : CmdletInvocationException
    {
        public CmdletProviderInvocationException();
        protected CmdletProviderInvocationException(SerializationInfo info, StreamingContext context);
        public CmdletProviderInvocationException(string message);
        public CmdletProviderInvocationException(string message, Exception innerException);
        public ProviderInfo ProviderInfo { get; }
        public ProviderInvocationException ProviderInvocationException { get; }
    }
    public sealed class CmdletProviderManagementIntrinsics
    {
        public Collection<ProviderInfo> Get(string name);
        public IEnumerable<ProviderInfo> GetAll();
        public ProviderInfo GetOne(string name);
    }
    public class CmsMessageRecipient
    {
        public CmsMessageRecipient(X509Certificate2 certificate);
        public CmsMessageRecipient(string identifier);
        public X509Certificate2Collection Certificates { get; }
        public void Resolve(SessionState sessionState, ResolutionPurpose purpose, out ErrorRecord error);
    }
    public class CommandBreakpoint : Breakpoint
    {
        public string Command { get; }
        public override string ToString();
    }
    public class CommandCompletion
    {
        public CommandCompletion(Collection<CompletionResult> matches, int currentMatchIndex, int replacementIndex, int replacementLength);
        public Collection<CompletionResult> CompletionMatches { get; set; }
        public int CurrentMatchIndex { get; set; }
        public int ReplacementIndex { get; set; }
        public int ReplacementLength { get; set; }
        public static CommandCompletion CompleteInput(Ast ast, Token[] tokens, IScriptPosition positionOfCursor, Hashtable options);
        public static CommandCompletion CompleteInput(Ast ast, Token[] tokens, IScriptPosition cursorPosition, Hashtable options, PowerShell powershell);
        public static CommandCompletion CompleteInput(string input, int cursorIndex, Hashtable options);
        public static CommandCompletion CompleteInput(string input, int cursorIndex, Hashtable options, PowerShell powershell);
        public CompletionResult GetNextResult(bool forward);
        public static Tuple<Ast, Token[], IScriptPosition> MapStringInputToParsedInput(string input, int cursorIndex);
    }
    public abstract class CommandInfo
    {
        public CommandTypes CommandType { get; }
        public abstract string Definition { get; }
        public PSModuleInfo Module { get; }
        public string ModuleName { get; }
        public string Name { get; }
        public abstract ReadOnlyCollection<PSTypeName> OutputType { get; }
        public virtual Dictionary<string, ParameterMetadata> Parameters { get; }
        public ReadOnlyCollection<CommandParameterSetInfo> ParameterSets { get; }
        public RemotingCapability RemotingCapability { get; }
        public virtual string Source { get; }
        public virtual Version Version { get; }
        public virtual SessionStateEntryVisibility Visibility { get; set; }
        public ParameterMetadata ResolveParameter(string name);
        public override string ToString();
    }
    public class CommandInvocationIntrinsics
    {
        public EventHandler<CommandLookupEventArgs> CommandNotFoundAction { get; set; }
        public bool HasErrors { get; set; }
        public EventHandler<CommandLookupEventArgs> PostCommandLookupAction { get; set; }
        public EventHandler<CommandLookupEventArgs> PreCommandLookupAction { get; set; }
        public string ExpandString(string source);
        public CmdletInfo GetCmdlet(string commandName);
        public CmdletInfo GetCmdletByTypeName(string cmdletTypeName);
        public List<CmdletInfo> GetCmdlets();
        public List<CmdletInfo> GetCmdlets(string pattern);
        public CommandInfo GetCommand(string commandName, CommandTypes type);
        public CommandInfo GetCommand(string commandName, CommandTypes type, object[] arguments);
        public List<string> GetCommandName(string name, bool nameIsPattern, bool returnFullName);
        public IEnumerable<CommandInfo> GetCommands(string name, CommandTypes commandTypes, bool nameIsPattern);
        public Collection<PSObject> InvokeScript(bool useLocalScope, ScriptBlock scriptBlock, IList input, params object[] args);
        public Collection<PSObject> InvokeScript(SessionState sessionState, ScriptBlock scriptBlock, params object[] args);
        public Collection<PSObject> InvokeScript(string script);
        public Collection<PSObject> InvokeScript(string script, bool useNewScope, PipelineResultTypes writeToPipeline, IList input, params object[] args);
        public Collection<PSObject> InvokeScript(string script, params object[] args);
        public ScriptBlock NewScriptBlock(string scriptText);
    }
    public class CommandLookupEventArgs : EventArgs
    {
        public CommandInfo Command { get; set; }
        public string CommandName { get; }
        public CommandOrigin CommandOrigin { get; }
        public ScriptBlock CommandScriptBlock { get; set; }
        public bool StopSearch { get; set; }
    }
    public sealed class CommandMetadata
    {
        public CommandMetadata(CommandInfo commandInfo);
        public CommandMetadata(CommandInfo commandInfo, bool shouldGenerateCommonParameters);
        public CommandMetadata(CommandMetadata other);
        public CommandMetadata(string path);
        public CommandMetadata(Type commandType);
        public Type CommandType { get; }
        public ConfirmImpact ConfirmImpact { get; set; }
        public string DefaultParameterSetName { get; set; }
        public string HelpUri { get; set; }
        public string Name { get; set; }
        public Dictionary<string, ParameterMetadata> Parameters { get; }
        public bool PositionalBinding { get; set; }
        public RemotingCapability RemotingCapability { get; set; }
        public bool SupportsPaging { get; set; }
        public bool SupportsShouldProcess { get; set; }
        public bool SupportsTransactions { get; set; }
        public static Dictionary<string, CommandMetadata> GetRestrictedCommands(SessionCapabilities sessionCapabilities);
    }
    public class CommandNotFoundException : RuntimeException
    {
        public CommandNotFoundException();
        protected CommandNotFoundException(SerializationInfo info, StreamingContext context);
        public CommandNotFoundException(string message);
        public CommandNotFoundException(string message, Exception innerException);
        public string CommandName { get; set; }
        public override ErrorRecord ErrorRecord { get; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
    }
    public enum CommandOrigin
    {
        Internal = 1,
        Runspace = 0,
    }
    public class CommandParameterInfo
    {
        public ReadOnlyCollection<string> Aliases { get; }
        public ReadOnlyCollection<Attribute> Attributes { get; }
        public string HelpMessage { get; }
        public bool IsDynamic { get; }
        public bool IsMandatory { get; }
        public string Name { get; }
        public Type ParameterType { get; }
        public int Position { get; }
        public bool ValueFromPipeline { get; }
        public bool ValueFromPipelineByPropertyName { get; }
        public bool ValueFromRemainingArguments { get; }
    }
    public class CommandParameterSetInfo
    {
        public bool IsDefault { get; }
        public string Name { get; }
        public ReadOnlyCollection<CommandParameterInfo> Parameters { get; }
        public override string ToString();
    }
    public enum CommandTypes
    {
        Alias = 1,
        All = 511,
        Application = 32,
        Cmdlet = 8,
        Configuration = 256,
        ExternalScript = 16,
        Filter = 4,
        Function = 2,
        Script = 64,
        Workflow = 128,
    }
    public static class CompletionCompleters
    {
        public static IEnumerable<CompletionResult> CompleteCommand(string commandName);
        public static IEnumerable<CompletionResult> CompleteCommand(string commandName, string moduleName, CommandTypes commandTypes=(CommandTypes)(511));
        public static IEnumerable<CompletionResult> CompleteFilename(string fileName);
        public static List<CompletionResult> CompleteOperator(string wordToComplete);
        public static IEnumerable<CompletionResult> CompleteType(string typeName);
        public static IEnumerable<CompletionResult> CompleteVariable(string variableName);
    }
    public class CompletionResult
    {
        public CompletionResult(string completionText);
        public CompletionResult(string completionText, string listItemText, CompletionResultType resultType, string toolTip);
        public string CompletionText { get; }
        public string ListItemText { get; }
        public CompletionResultType ResultType { get; }
        public string ToolTip { get; }
    }
    public enum CompletionResultType
    {
        Command = 2,
        DynamicKeyword = 13,
        History = 1,
        Keyword = 12,
        Method = 6,
        Namespace = 10,
        ParameterName = 7,
        ParameterValue = 8,
        Property = 5,
        ProviderContainer = 4,
        ProviderItem = 3,
        Text = 0,
        Type = 11,
        Variable = 9,
    }
    public class ConfigurationInfo : FunctionInfo
    {
        public bool IsMetaConfiguration { get; }
    }
    public enum ConfirmImpact
    {
        High = 3,
        Low = 1,
        Medium = 2,
        None = 0,
    }
    public sealed class ContainerParentJob : Job2
    {
        public ContainerParentJob(string command);
        public ContainerParentJob(string command, string name);
        public ContainerParentJob(string command, string name, Guid instanceId);
        public ContainerParentJob(string command, string name, Guid instanceId, string jobType);
        public ContainerParentJob(string command, string name, JobIdentifier jobId);
        public ContainerParentJob(string command, string name, JobIdentifier jobId, string jobType);
        public ContainerParentJob(string command, string name, string jobType);
        public override bool HasMoreData { get; }
        public override string Location { get; }
        public override string StatusMessage { get; }
        public void AddChildJob(Job2 childJob);
        protected override void Dispose(bool disposing);
        public override void ResumeJob();
        public override void ResumeJobAsync();
        public override void StartJob();
        public override void StartJobAsync();
        public override void StopJob();
        public override void StopJob(bool force, string reason);
        public override void StopJobAsync();
        public override void StopJobAsync(bool force, string reason);
        public override void SuspendJob();
        public override void SuspendJob(bool force, string reason);
        public override void SuspendJobAsync();
        public override void SuspendJobAsync(bool force, string reason);
        public override void UnblockJob();
        public override void UnblockJobAsync();
    }
    public sealed class ContentCmdletProviderIntrinsics
    {
        public void Clear(string path);
        public void Clear(string[] path, bool force, bool literalPath);
        public Collection<IContentReader> GetReader(string path);
        public Collection<IContentReader> GetReader(string[] path, bool force, bool literalPath);
        public Collection<IContentWriter> GetWriter(string path);
        public Collection<IContentWriter> GetWriter(string[] path, bool force, bool literalPath);
    }
    public sealed class ContinueException : LoopFlowException
    {
    }
    public class ConvertThroughString : PSTypeConverter
    {
        public ConvertThroughString();
        public override bool CanConvertFrom(object sourceValue, Type destinationType);
        public override bool CanConvertTo(object sourceValue, Type destinationType);
        public override object ConvertFrom(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase);
        public override object ConvertTo(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase);
    }
    public enum CopyContainers
    {
        CopyChildrenOfTargetContainer = 1,
        CopyTargetContainer = 0,
    }
    public sealed class CredentialAttribute : ArgumentTransformationAttribute
    {
        public CredentialAttribute();
        public override bool TransformNullOptionalParameters { get; }
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData);
    }
    public sealed class CustomControl : PSControl
    {
        public List<CustomControlEntry> Entries { get; set; }
        public static CustomControlBuilder Create(bool outOfBand=false);
    }
    public sealed class CustomControlBuilder
    {
        public CustomControl EndControl();
        public CustomControlBuilder GroupByProperty(string property, CustomControl customControl=null, string label=null);
        public CustomControlBuilder GroupByScriptBlock(string scriptBlock, CustomControl customControl=null, string label=null);
        public CustomEntryBuilder StartEntry(IEnumerable<string> entrySelectedByType=null, IEnumerable<DisplayEntry> entrySelectedByCondition=null);
    }
    public sealed class CustomControlEntry
    {
        public List<CustomItemBase> CustomItems { get; set; }
        public EntrySelectedBy SelectedBy { get; set; }
    }
    public sealed class CustomEntryBuilder
    {
        public CustomEntryBuilder AddCustomControlExpressionBinding(CustomControl customControl, bool enumerateCollection=false, string selectedByType=null, string selectedByScript=null);
        public CustomEntryBuilder AddNewline(int count=1);
        public CustomEntryBuilder AddPropertyExpressionBinding(string property, bool enumerateCollection=false, string selectedByType=null, string selectedByScript=null, CustomControl customControl=null);
        public CustomEntryBuilder AddScriptBlockExpressionBinding(string scriptBlock, bool enumerateCollection=false, string selectedByType=null, string selectedByScript=null, CustomControl customControl=null);
        public CustomEntryBuilder AddText(string text);
        public CustomControlBuilder EndEntry();
        public CustomEntryBuilder EndFrame();
        public CustomEntryBuilder StartFrame(uint leftIndent=(uint)0, uint rightIndent=(uint)0, uint firstLineHanging=(uint)0, uint firstLineIndent=(uint)0);
    }
    public abstract class CustomItemBase
    {
        protected CustomItemBase();
    }
    public sealed class CustomItemExpression : CustomItemBase
    {
        public CustomControl CustomControl { get; set; }
        public bool EnumerateCollection { get; set; }
        public DisplayEntry Expression { get; set; }
        public DisplayEntry ItemSelectionCondition { get; set; }
    }
    public sealed class CustomItemFrame : CustomItemBase
    {
        public List<CustomItemBase> CustomItems { get; set; }
        public uint FirstLineHanging { get; set; }
        public uint FirstLineIndent { get; set; }
        public uint LeftIndent { get; set; }
        public uint RightIndent { get; set; }
    }
    public sealed class CustomItemNewline : CustomItemBase
    {
        public CustomItemNewline();
        public int Count { get; set; }
    }
    public sealed class CustomItemText : CustomItemBase
    {
        public CustomItemText();
        public string Text { get; set; }
    }
    public sealed class DataAddedEventArgs : EventArgs
    {
        public int Index { get; }
        public Guid PowerShellInstanceId { get; }
    }
    public sealed class DataAddingEventArgs : EventArgs
    {
        public object ItemAdded { get; }
        public Guid PowerShellInstanceId { get; }
    }
    public abstract class Debugger
    {
        protected Debugger();
        protected bool DebuggerStopped { get; }
        public DebugModes DebugMode { get; protected set; }
        public virtual bool InBreakpoint { get; }
        public virtual Guid InstanceId { get; }
        public virtual bool IsActive { get; }
        public event EventHandler<BreakpointUpdatedEventArgs> BreakpointUpdated;
        public event EventHandler<EventArgs> CancelRunspaceDebugProcessing;
        public event EventHandler<DebuggerStopEventArgs> DebuggerStop;
        public event EventHandler<ProcessRunspaceDebugEndEventArgs> RunspaceDebugProcessingCompleted;
        public event EventHandler<StartRunspaceDebugProcessingEventArgs> StartRunspaceDebugProcessing;
        public virtual void CancelDebuggerProcessing();
        public virtual IEnumerable<CallStackFrame> GetCallStack();
        public abstract DebuggerStopEventArgs GetDebuggerStopArgs();
        protected bool IsDebuggerBreakpointUpdatedEventSubscribed();
        protected bool IsDebuggerStopEventSubscribed();
        protected bool IsStartRunspaceDebugProcessingEventSubscribed();
        public abstract DebuggerCommandResults ProcessCommand(PSCommand command, PSDataCollection<PSObject> output);
        protected void RaiseBreakpointUpdatedEvent(BreakpointUpdatedEventArgs args);
        protected void RaiseCancelRunspaceDebugProcessingEvent();
        protected void RaiseDebuggerStopEvent(DebuggerStopEventArgs args);
        protected void RaiseRunspaceProcessingCompletedEvent(ProcessRunspaceDebugEndEventArgs args);
        protected void RaiseStartRunspaceDebugProcessingEvent(StartRunspaceDebugProcessingEventArgs args);
        public virtual void ResetCommandProcessorSource();
        public virtual void SetBreakpoints(IEnumerable<Breakpoint> breakpoints);
        public abstract void SetDebuggerAction(DebuggerResumeAction resumeAction);
        public virtual void SetDebuggerStepMode(bool enabled);
        public virtual void SetDebugMode(DebugModes mode);
        public virtual void SetParent(Debugger parent, IEnumerable<Breakpoint> breakPoints, Nullable<DebuggerResumeAction> startAction, PSHost host, PathInfo path);
        public virtual void SetParent(Debugger parent, IEnumerable<Breakpoint> breakPoints, Nullable<DebuggerResumeAction> startAction, PSHost host, PathInfo path, Dictionary<string, DebugSource> functionSourceMap);
        public abstract void StopProcessCommand();
    }
    public sealed class DebuggerCommandResults
    {
        public DebuggerCommandResults(Nullable<DebuggerResumeAction> resumeAction, bool evaluatedByDebugger);
        public bool EvaluatedByDebugger { get; }
        public Nullable<DebuggerResumeAction> ResumeAction { get; }
    }
    public enum DebuggerResumeAction
    {
        Continue = 0,
        StepInto = 1,
        StepOut = 2,
        StepOver = 3,
        Stop = 4,
    }
    public class DebuggerStopEventArgs : EventArgs
    {
        public DebuggerStopEventArgs(InvocationInfo invocationInfo, Collection<Breakpoint> breakpoints, DebuggerResumeAction resumeAction);
        public ReadOnlyCollection<Breakpoint> Breakpoints { get; }
        public InvocationInfo InvocationInfo { get; }
        public DebuggerResumeAction ResumeAction { get; set; }
    }
    public enum DebugModes
    {
        Default = 1,
        LocalScript = 2,
        None = 0,
        RemoteScript = 4,
    }
    public class DebugRecord : InformationalRecord
    {
        public DebugRecord(PSObject record);
        public DebugRecord(string message);
    }
    public sealed class DebugSource
    {
        public DebugSource(string script, string scriptFile, string xamlDefinition);
        public string Script { get; }
        public string ScriptFile { get; }
        public string XamlDefinition { get; }
    }
    public sealed class DefaultParameterDictionary : Hashtable
    {
        public DefaultParameterDictionary();
        public DefaultParameterDictionary(IDictionary dictionary);
        public override object this[object key] { get; set; }
        public override void Add(object key, object value);
        public bool ChangeSinceLastCheck();
        public override void Clear();
        public override bool Contains(object key);
        public override bool ContainsKey(object key);
        public override void Remove(object key);
    }
    public sealed class DisplayEntry
    {
        public DisplayEntry(string value, DisplayEntryValueType type);
        public string Value { get; }
        public DisplayEntryValueType ValueType { get; }
        public override string ToString();
    }
    public enum DisplayEntryValueType
    {
        Property = 0,
        ScriptBlock = 1,
    }
    public sealed class DriveManagementIntrinsics
    {
        public PSDriveInfo Current { get; }
        public PSDriveInfo Get(string driveName);
        public Collection<PSDriveInfo> GetAll();
        public Collection<PSDriveInfo> GetAllAtScope(string scope);
        public Collection<PSDriveInfo> GetAllForProvider(string providerName);
        public PSDriveInfo GetAtScope(string driveName, string scope);
        public PSDriveInfo New(PSDriveInfo drive, string scope);
        public void Remove(string driveName, bool force, string scope);
    }
    public class DriveNotFoundException : SessionStateException
    {
        public DriveNotFoundException();
        protected DriveNotFoundException(SerializationInfo info, StreamingContext context);
        public DriveNotFoundException(string message);
        public DriveNotFoundException(string message, Exception innerException);
    }
    public class DscLocalConfigurationManagerAttribute : CmdletMetadataAttribute
    {
        public DscLocalConfigurationManagerAttribute();
    }
    public class DscPropertyAttribute : CmdletMetadataAttribute
    {
        public DscPropertyAttribute();
        public bool Key { get; set; }
        public bool Mandatory { get; set; }
        public bool NotConfigurable { get; set; }
    }
    public class DscResourceAttribute : CmdletMetadataAttribute
    {
        public DscResourceAttribute();
        public DSCResourceRunAsCredential RunAsCredential { get; set; }
    }
    public class DscResourceInfo
    {
        public string CompanyName { get; set; }
        public string FriendlyName { get; set; }
        public string HelpFile { get; }
        public ImplementedAsType ImplementedAs { get; set; }
        public PSModuleInfo Module { get; }
        public string Name { get; }
        public string ParentPath { get; set; }
        public string Path { get; set; }
        public ReadOnlyCollection<DscResourcePropertyInfo> Properties { get; }
        public string ResourceType { get; set; }
        public void UpdateProperties(IList<DscResourcePropertyInfo> properties);
    }
    public sealed class DscResourcePropertyInfo
    {
        public bool IsMandatory { get; set; }
        public string Name { get; set; }
        public string PropertyType { get; set; }
        public ReadOnlyCollection<string> Values { get; }
    }
    public enum DSCResourceRunAsCredential
    {
        Default = 0,
        Mandatory = 2,
        NotSupported = 1,
        Optional = 0,
    }
    public class DynamicClassImplementationAssemblyAttribute : Attribute
    {
        public DynamicClassImplementationAssemblyAttribute();
    }
    public class EngineIntrinsics
    {
        public PSEventManager Events { get; }
        public PSHost Host { get; }
        public CommandInvocationIntrinsics InvokeCommand { get; }
        public ProviderIntrinsics InvokeProvider { get; }
        public SessionState SessionState { get; }
    }
    public sealed class EntrySelectedBy
    {
        public EntrySelectedBy();
        public List<DisplayEntry> SelectionCondition { get; set; }
        public List<string> TypeNames { get; set; }
    }
    public enum ErrorCategory
    {
        AuthenticationError = 28,
        CloseError = 2,
        ConnectionError = 27,
        DeadlockDetected = 4,
        DeviceError = 3,
        FromStdErr = 24,
        InvalidArgument = 5,
        InvalidData = 6,
        InvalidOperation = 7,
        InvalidResult = 8,
        InvalidType = 9,
        LimitsExceeded = 29,
        MetadataError = 10,
        NotEnabled = 31,
        NotImplemented = 11,
        NotInstalled = 12,
        NotSpecified = 0,
        ObjectNotFound = 13,
        OpenError = 1,
        OperationStopped = 14,
        OperationTimeout = 15,
        ParserError = 17,
        PermissionDenied = 18,
        ProtocolError = 26,
        QuotaExceeded = 30,
        ReadError = 22,
        ResourceBusy = 19,
        ResourceExists = 20,
        ResourceUnavailable = 21,
        SecurityError = 25,
        SyntaxError = 16,
        WriteError = 23,
    }
    public class ErrorCategoryInfo
    {
        public string Activity { get; set; }
        public ErrorCategory Category { get; }
        public string Reason { get; set; }
        public string TargetName { get; set; }
        public string TargetType { get; set; }
        public string GetMessage();
        public string GetMessage(CultureInfo uiCultureInfo);
        public override string ToString();
    }
    public class ErrorDetails : ISerializable
    {
        public ErrorDetails(Cmdlet cmdlet, string baseName, string resourceId, params object[] args);
        public ErrorDetails(IResourceSupplier resourceSupplier, string baseName, string resourceId, params object[] args);
        public ErrorDetails(Assembly assembly, string baseName, string resourceId, params object[] args);
        protected ErrorDetails(SerializationInfo info, StreamingContext context);
        public ErrorDetails(string message);
        public string Message { get; }
        public string RecommendedAction { get; set; }
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context);
        public override string ToString();
    }
    public class ErrorRecord : ISerializable
    {
        public ErrorRecord(Exception exception, string errorId, ErrorCategory errorCategory, object targetObject);
        public ErrorRecord(ErrorRecord errorRecord, Exception replaceParentContainsErrorRecordException);
        protected ErrorRecord(SerializationInfo info, StreamingContext context);
        public ErrorCategoryInfo CategoryInfo { get; }
        public ErrorDetails ErrorDetails { get; set; }
        public Exception Exception { get; }
        public string FullyQualifiedErrorId { get; }
        public InvocationInfo InvocationInfo { get; }
        public ReadOnlyCollection<int> PipelineIterationInfo { get; }
        public string ScriptStackTrace { get; }
        public object TargetObject { get; }
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context);
        public override string ToString();
    }
    public class ExitException : FlowControlException
    {
        public object Argument { get; }
    }
    public sealed class ExtendedTypeDefinition
    {
        public ExtendedTypeDefinition(string typeName);
        public ExtendedTypeDefinition(string typeName, IEnumerable<FormatViewDefinition> viewDefinitions);
        public List<FormatViewDefinition> FormatViewDefinition { get; }
        public string TypeName { get; }
        public List<string> TypeNames { get; }
        public override string ToString();
    }
    public class ExtendedTypeSystemException : RuntimeException
    {
        public ExtendedTypeSystemException();
        protected ExtendedTypeSystemException(SerializationInfo info, StreamingContext context);
        public ExtendedTypeSystemException(string message);
        public ExtendedTypeSystemException(string message, Exception innerException);
    }
    public class ExternalScriptInfo : CommandInfo
    {
        public override string Definition { get; }
        public Encoding OriginalEncoding { get; }
        public override ReadOnlyCollection<PSTypeName> OutputType { get; }
        public string Path { get; }
        public ScriptBlock ScriptBlock { get; }
        public string ScriptContents { get; }
        public override string Source { get; }
        public override SessionStateEntryVisibility Visibility { get; set; }
        public void ValidateScriptInfo(PSHost host);
    }
    public class FilterInfo : FunctionInfo
    {
    }
    public sealed class FlagsExpression<T> where T : struct, IConvertible
    {
        public FlagsExpression(object[] expression);
        public FlagsExpression(string expression);
        public bool Evaluate(T value);
    }
    public abstract class FlowControlException : SystemException
    {
    }
    public sealed class FormatViewDefinition
    {
        public FormatViewDefinition(string name, PSControl control);
        public PSControl Control { get; }
        public string Name { get; }
    }
    public class ForwardedEventArgs : EventArgs
    {
        public PSObject SerializedRemoteEventArgs { get; }
    }
    public class FunctionInfo : CommandInfo
    {
        public bool CmdletBinding { get; }
        public string DefaultParameterSet { get; }
        public override string Definition { get; }
        public string Description { get; set; }
        public string HelpFile { get; }
        public string Noun { get; }
        public ScopedItemOptions Options { get; set; }
        public override ReadOnlyCollection<PSTypeName> OutputType { get; }
        public ScriptBlock ScriptBlock { get; }
        public string Verb { get; }
        protected internal virtual void Update(FunctionInfo newFunction, bool force, ScopedItemOptions options, string helpFile);
    }
    public delegate bool GetSymmetricEncryptionKey(StreamingContext context, out byte[] key, out byte[] iv);
    public class GettingValueExceptionEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public bool ShouldThrow { get; set; }
        public object ValueReplacement { get; set; }
    }
    public class GetValueException : ExtendedTypeSystemException
    {
        public GetValueException();
        protected GetValueException(SerializationInfo info, StreamingContext context);
        public GetValueException(string message);
        public GetValueException(string message, Exception innerException);
    }
    public class GetValueInvocationException : GetValueException
    {
        public GetValueInvocationException();
        protected GetValueInvocationException(SerializationInfo info, StreamingContext context);
        public GetValueInvocationException(string message);
        public GetValueInvocationException(string message, Exception innerException);
    }
    public class HaltCommandException : SystemException
    {
        public HaltCommandException();
        protected HaltCommandException(SerializationInfo info, StreamingContext context);
        public HaltCommandException(string message);
        public HaltCommandException(string message, Exception innerException);
    }
    public sealed class HiddenAttribute : ParsingBaseAttribute
    {
        public HiddenAttribute();
    }
    public class HostInformationMessage
    {
        public HostInformationMessage();
        public Nullable<ConsoleColor> BackgroundColor { get; set; }
        public Nullable<ConsoleColor> ForegroundColor { get; set; }
        public string Message { get; set; }
        public Nullable<bool> NoNewLine { get; set; }
        public override string ToString();
    }
    public static class HostUtilities
    {
        public const string CreatePSEditFunction = "\r\n            param (\r\n                [string] $PSEditFunction\r\n            )\r\n\r\n            if ($PSVersionTable.PSVersion -lt ([version] '3.0'))\r\n            {\r\n                throw (new-object System.NotSupportedException)\r\n            }\r\n\r\n            Register-EngineEvent -SourceIdentifier PSISERemoteSessionOpenFile -Forward\r\n\r\n            if ((Test-Path -Path 'function:\\global:PSEdit') -eq $false)\r\n            {\r\n                Set-Item -Path 'function:\\global:PSEdit' -Value $PSEditFunction\r\n            }\r\n        ";
        public const string PSEditFunction = "\r\n            param (\r\n                [Parameter(Mandatory=$true)] [String[]] $FileName\r\n            )\r\n\r\n            foreach ($file in $FileName)\r\n            {\r\n                dir $file -File | foreach {\r\n                    $filePathName = $_.FullName\r\n\r\n                    # Get file contents\r\n                    $contentBytes = Get-Content -Path $filePathName -Raw -Encoding Byte\r\n\r\n                    # Notify client for file open.\r\n                    New-Event -SourceIdentifier PSISERemoteSessionOpenFile -EventArguments @($filePathName, $contentBytes) > $null\r\n                }\r\n            }\r\n        ";
        public const string RemoteSessionOpenFileEvent = "PSISERemoteSessionOpenFile";
        public const string RemovePSEditFunction = "\r\n            if ($PSVersionTable.PSVersion -lt ([version] '3.0'))\r\n            {\r\n                throw (new-object System.NotSupportedException)\r\n            }\r\n\r\n            if ((Test-Path -Path 'function:\\global:PSEdit') -eq $true)\r\n            {\r\n                Remove-Item -Path 'function:\\global:PSEdit' -Force\r\n            }\r\n\r\n            Get-EventSubscriber -SourceIdentifier PSISERemoteSessionOpenFile -EA Ignore | Remove-Event\r\n        ";
        public static Collection<PSObject> InvokeOnRunspace(PSCommand command, Runspace runspace);
    }
    public interface IArgumentCompleter
    {
        IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters);
    }
    public interface ICommandRuntime
    {
        PSTransactionContext CurrentPSTransaction { get; }
        PSHost Host { get; }
        bool ShouldContinue(string query, string caption);
        bool ShouldContinue(string query, string caption, ref bool yesToAll, ref bool noToAll);
        bool ShouldProcess(string target);
        bool ShouldProcess(string target, string action);
        bool ShouldProcess(string verboseDescription, string verboseWarning, string caption);
        bool ShouldProcess(string verboseDescription, string verboseWarning, string caption, out ShouldProcessReason shouldProcessReason);
        void ThrowTerminatingError(ErrorRecord errorRecord);
        bool TransactionAvailable();
        void WriteCommandDetail(string text);
        void WriteDebug(string text);
        void WriteError(ErrorRecord errorRecord);
        void WriteObject(object sendToPipeline);
        void WriteObject(object sendToPipeline, bool enumerateCollection);
        void WriteProgress(long sourceId, ProgressRecord progressRecord);
        void WriteProgress(ProgressRecord progressRecord);
        void WriteVerbose(string text);
        void WriteWarning(string text);
    }
    public interface ICommandRuntime2 : ICommandRuntime
    {
        bool ShouldContinue(string query, string caption, bool hasSecurityImpact, ref bool yesToAll, ref bool noToAll);
        void WriteInformation(InformationRecord informationRecord);
    }
    public interface IContainsErrorRecord
    {
        ErrorRecord ErrorRecord { get; }
    }
    public interface IDynamicParameters
    {
        object GetDynamicParameters();
    }
    public interface IJobDebugger
    {
        Debugger Debugger { get; }
        bool IsAsync { get; set; }
    }
    public interface IModuleAssemblyCleanup
    {
        void OnRemove(PSModuleInfo psModuleInfo);
    }
    public interface IModuleAssemblyInitializer
    {
        void OnImport();
    }
    public enum ImplementedAsType
    {
        Binary = 2,
        Composite = 3,
        None = 0,
        PowerShell = 1,
    }
    public class IncompleteParseException : ParseException
    {
        public IncompleteParseException();
        protected IncompleteParseException(SerializationInfo info, StreamingContext context);
        public IncompleteParseException(string message);
        public IncompleteParseException(string message, Exception innerException);
    }
    public abstract class InformationalRecord
    {
        public InvocationInfo InvocationInfo { get; }
        public string Message { get; set; }
        public ReadOnlyCollection<int> PipelineIterationInfo { get; }
        public override string ToString();
    }
    public class InformationRecord
    {
        public InformationRecord(object messageData, string source);
        public string Computer { get; set; }
        public uint ManagedThreadId { get; set; }
        public object MessageData { get; }
        public uint NativeThreadId { get; set; }
        public uint ProcessId { get; set; }
        public string Source { get; set; }
        public List<string> Tags { get; }
        public DateTime TimeGenerated { get; set; }
        public string User { get; set; }
        public override string ToString();
    }
    public class InvalidJobStateException : SystemException
    {
        public InvalidJobStateException();
        public InvalidJobStateException(JobState currentState, string actionMessage);
        protected InvalidJobStateException(SerializationInfo info, StreamingContext context);
        public InvalidJobStateException(string message);
        public InvalidJobStateException(string message, Exception innerException);
        public JobState CurrentState { get; }
    }
    public class InvalidPowerShellStateException : SystemException
    {
        public InvalidPowerShellStateException();
        protected InvalidPowerShellStateException(SerializationInfo info, StreamingContext context);
        public InvalidPowerShellStateException(string message);
        public InvalidPowerShellStateException(string message, Exception innerException);
        public PSInvocationState CurrentState { get; }
    }
    public class InvocationInfo
    {
        public Dictionary<string, object> BoundParameters { get; }
        public CommandOrigin CommandOrigin { get; }
        public IScriptExtent DisplayScriptPosition { get; set; }
        public bool ExpectingInput { get; }
        public long HistoryId { get; }
        public string InvocationName { get; }
        public string Line { get; }
        public CommandInfo MyCommand { get; }
        public int OffsetInLine { get; }
        public int PipelineLength { get; }
        public int PipelinePosition { get; }
        public string PositionMessage { get; }
        public string PSCommandPath { get; }
        public string PSScriptRoot { get; }
        public int ScriptLineNumber { get; }
        public string ScriptName { get; }
        public List<object> UnboundArguments { get; }
        public static InvocationInfo Create(CommandInfo commandInfo, IScriptExtent scriptPosition);
    }
    public interface IResourceSupplier
    {
        string GetResourceString(string baseName, string resourceId);
    }
    public sealed class ItemCmdletProviderIntrinsics
    {
        public Collection<PSObject> Clear(string path);
        public Collection<PSObject> Clear(string[] path, bool force, bool literalPath);
        public Collection<PSObject> Copy(string path, string destinationPath, bool recurse, CopyContainers copyContainers);
        public Collection<PSObject> Copy(string[] path, string destinationPath, bool recurse, CopyContainers copyContainers, bool force, bool literalPath);
        public bool Exists(string path);
        public bool Exists(string path, bool force, bool literalPath);
        public Collection<PSObject> Get(string path);
        public Collection<PSObject> Get(string[] path, bool force, bool literalPath);
        public void Invoke(string path);
        public void Invoke(string[] path, bool literalPath);
        public bool IsContainer(string path);
        public Collection<PSObject> Move(string path, string destination);
        public Collection<PSObject> Move(string[] path, string destination, bool force, bool literalPath);
        public Collection<PSObject> New(string path, string name, string itemTypeName, object content);
        public Collection<PSObject> New(string[] path, string name, string itemTypeName, object content, bool force);
        public void Remove(string path, bool recurse);
        public void Remove(string[] path, bool recurse, bool force, bool literalPath);
        public Collection<PSObject> Rename(string path, string newName);
        public Collection<PSObject> Rename(string path, string newName, bool force);
        public Collection<PSObject> Set(string path, object value);
        public Collection<PSObject> Set(string[] path, object value, bool force, bool literalPath);
    }
    public class ItemNotFoundException : SessionStateException
    {
        public ItemNotFoundException();
        protected ItemNotFoundException(SerializationInfo info, StreamingContext context);
        public ItemNotFoundException(string message);
        public ItemNotFoundException(string message, Exception innerException);
    }
    public abstract class Job : IDisposable
    {
        protected Job();
        protected Job(string command);
        protected Job(string command, string name);
        protected Job(string command, string name, IList<Job> childJobs);
        protected Job(string command, string name, Guid instanceId);
        protected Job(string command, string name, JobIdentifier token);
        public IList<Job> ChildJobs { get; }
        public string Command { get; }
        public PSDataCollection<DebugRecord> Debug { get; set; }
        public PSDataCollection<ErrorRecord> Error { get; set; }
        public WaitHandle Finished { get; }
        public abstract bool HasMoreData { get; }
        public int Id { get; }
        public PSDataCollection<InformationRecord> Information { get; set; }
        public Guid InstanceId { get; }
        public JobStateInfo JobStateInfo { get; }
        public abstract string Location { get; }
        public string Name { get; set; }
        public PSDataCollection<PSObject> Output { get; set; }
        public PSDataCollection<ProgressRecord> Progress { get; set; }
        public Nullable<DateTime> PSBeginTime { get; protected set; }
        public Nullable<DateTime> PSEndTime { get; protected set; }
        public string PSJobTypeName { get; protected internal set; }
        public abstract string StatusMessage { get; }
        public PSDataCollection<VerboseRecord> Verbose { get; set; }
        public PSDataCollection<WarningRecord> Warning { get; set; }
        public event EventHandler<JobStateEventArgs> StateChanged;
        protected string AutoGenerateJobName();
        public void Dispose();
        protected virtual void Dispose(bool disposing);
        protected virtual void DoLoadJobStreams();
        protected virtual void DoUnloadJobStreams();
        public void LoadJobStreams();
        protected void SetJobState(JobState state);
        public abstract void StopJob();
        public void UnloadJobStreams();
    }
    public abstract class Job2 : Job
    {
        protected Job2();
        protected Job2(string command);
        protected Job2(string command, string name);
        protected Job2(string command, string name, IList<Job> childJobs);
        protected Job2(string command, string name, Guid instanceId);
        protected Job2(string command, string name, JobIdentifier token);
        public List<CommandParameterCollection> StartParameters { get; set; }
        protected object SyncRoot { get; }
        public event EventHandler<AsyncCompletedEventArgs> ResumeJobCompleted;
        public event EventHandler<AsyncCompletedEventArgs> StartJobCompleted;
        public event EventHandler<AsyncCompletedEventArgs> StopJobCompleted;
        public event EventHandler<AsyncCompletedEventArgs> SuspendJobCompleted;
        public event EventHandler<AsyncCompletedEventArgs> UnblockJobCompleted;
        protected virtual void OnResumeJobCompleted(AsyncCompletedEventArgs eventArgs);
        protected virtual void OnStartJobCompleted(AsyncCompletedEventArgs eventArgs);
        protected virtual void OnStopJobCompleted(AsyncCompletedEventArgs eventArgs);
        protected virtual void OnSuspendJobCompleted(AsyncCompletedEventArgs eventArgs);
        protected virtual void OnUnblockJobCompleted(AsyncCompletedEventArgs eventArgs);
        public abstract void ResumeJob();
        public abstract void ResumeJobAsync();
        protected void SetJobState(JobState state, Exception reason);
        public abstract void StartJob();
        public abstract void StartJobAsync();
        public abstract void StopJob(bool force, string reason);
        public abstract void StopJobAsync();
        public abstract void StopJobAsync(bool force, string reason);
        public abstract void SuspendJob();
        public abstract void SuspendJob(bool force, string reason);
        public abstract void SuspendJobAsync();
        public abstract void SuspendJobAsync(bool force, string reason);
        public abstract void UnblockJob();
        public abstract void UnblockJobAsync();
    }
    public sealed class JobDataAddedEventArgs : EventArgs
    {
        public PowerShellStreamType DataType { get; }
        public int Index { get; }
        public Job SourceJob { get; }
    }
    public class JobDefinition : ISerializable
    {
        protected JobDefinition(SerializationInfo info, StreamingContext context);
        public JobDefinition(Type jobSourceAdapterType, string command, string name);
        public string Command { get; }
        public CommandInfo CommandInfo { get; }
        public Guid InstanceId { get; set; }
        public Type JobSourceAdapterType { get; }
        public string JobSourceAdapterTypeName { get; set; }
        public string ModuleName { get; set; }
        public string Name { get; set; }
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context);
        public virtual void Load(Stream stream);
        public virtual void Save(Stream stream);
    }
    public class JobFailedException : SystemException
    {
        public JobFailedException();
        public JobFailedException(Exception innerException, ScriptExtent displayScriptPosition);
        protected JobFailedException(SerializationInfo serializationInfo, StreamingContext streamingContext);
        public JobFailedException(string message);
        public JobFailedException(string message, Exception innerException);
        public ScriptExtent DisplayScriptPosition { get; }
        public override string Message { get; }
        public Exception Reason { get; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
    }
    public sealed class JobIdentifier
    {
    }
    public class JobInvocationInfo : ISerializable
    {
        protected JobInvocationInfo();
        public JobInvocationInfo(JobDefinition definition, Dictionary<string, object> parameters);
        public JobInvocationInfo(JobDefinition definition, IEnumerable<Dictionary<string, object>> parameterCollectionList);
        public JobInvocationInfo(JobDefinition definition, IEnumerable<CommandParameterCollection> parameters);
        public JobInvocationInfo(JobDefinition definition, CommandParameterCollection parameters);
        protected JobInvocationInfo(SerializationInfo info, StreamingContext context);
        public string Command { get; set; }
        public JobDefinition Definition { get; set; }
        public Guid InstanceId { get; }
        public string Name { get; set; }
        public List<CommandParameterCollection> Parameters { get; }
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context);
        public virtual void Load(Stream stream);
        public virtual void Save(Stream stream);
    }
    public sealed class JobManager
    {
        public bool IsRegistered(string typeName);
        public Job2 NewJob(JobDefinition definition);
        public Job2 NewJob(JobInvocationInfo specification);
        public void PersistJob(Job2 job, JobDefinition definition);
    }
    public class JobRepository : Repository<Job>
    {
        public List<Job> Jobs { get; }
        public Job GetJob(Guid instanceId);
        protected override Guid GetKey(Job item);
    }
    public abstract class JobSourceAdapter
    {
        protected JobSourceAdapter();
        public string Name { get; set; }
        public abstract Job2 GetJobByInstanceId(Guid instanceId, bool recurse);
        public abstract Job2 GetJobBySessionId(int id, bool recurse);
        public abstract IList<Job2> GetJobs();
        public abstract IList<Job2> GetJobsByCommand(string command, bool recurse);
        public abstract IList<Job2> GetJobsByFilter(Dictionary<string, object> filter, bool recurse);
        public abstract IList<Job2> GetJobsByName(string name, bool recurse);
        public abstract IList<Job2> GetJobsByState(JobState state, bool recurse);
        public Job2 NewJob(JobDefinition definition);
        public abstract Job2 NewJob(JobInvocationInfo specification);
        public virtual Job2 NewJob(string definitionName, string definitionPath);
        public virtual void PersistJob(Job2 job);
        public abstract void RemoveJob(Job2 job);
        protected JobIdentifier RetrieveJobIdForReuse(Guid instanceId);
        public void StoreJobIdForReuse(Job2 job, bool recurse);
    }
    public enum JobState
    {
        AtBreakpoint = 10,
        Blocked = 5,
        Completed = 2,
        Disconnected = 7,
        Failed = 3,
        NotStarted = 0,
        Running = 1,
        Stopped = 4,
        Stopping = 9,
        Suspended = 6,
        Suspending = 8,
    }
    public sealed class JobStateEventArgs : EventArgs
    {
        public JobStateEventArgs(JobStateInfo jobStateInfo);
        public JobStateEventArgs(JobStateInfo jobStateInfo, JobStateInfo previousJobStateInfo);
        public JobStateInfo JobStateInfo { get; }
        public JobStateInfo PreviousJobStateInfo { get; }
    }
    public sealed class JobStateInfo
    {
        public JobStateInfo(JobState state);
        public JobStateInfo(JobState state, Exception reason);
        public Exception Reason { get; }
        public JobState State { get; }
        public override string ToString();
    }
    public enum JobThreadOptions
    {
        Default = 0,
        UseNewThread = 2,
        UseThreadPoolThread = 1,
    }
    public static class LanguagePrimitives
    {
        public static int Compare(object first, object second);
        public static int Compare(object first, object second, bool ignoreCase);
        public static int Compare(object first, object second, bool ignoreCase, IFormatProvider formatProvider);
        public static object ConvertPSObjectToType(PSObject valueToConvert, Type resultType, bool recursion, IFormatProvider formatProvider, bool ignoreUnknownMembers);
        public static object ConvertTo(object valueToConvert, Type resultType);
        public static object ConvertTo(object valueToConvert, Type resultType, IFormatProvider formatProvider);
        public static T ConvertTo<T>(object valueToConvert);
        public static string ConvertTypeNameToPSTypeName(string typeName);
        public static new bool Equals(object first, object second);
        public static bool Equals(object first, object second, bool ignoreCase);
        public static bool Equals(object first, object second, bool ignoreCase, IFormatProvider formatProvider);
        public static IEnumerable GetEnumerable(object obj);
        public static IEnumerator GetEnumerator(object obj);
        public static PSDataCollection<PSObject> GetPSDataCollection(object inputValue);
        public static bool IsTrue(object obj);
        public static bool TryConvertTo(object valueToConvert, Type resultType, IFormatProvider formatProvider, out object result);
        public static bool TryConvertTo(object valueToConvert, Type resultType, out object result);
        public static bool TryConvertTo<T>(object valueToConvert, IFormatProvider formatProvider, out T result);
        public static bool TryConvertTo<T>(object valueToConvert, out T result);
    }
    public class LineBreakpoint : Breakpoint
    {
        public int Column { get; }
        public int Line { get; }
        public override string ToString();
    }
    public sealed class ListControl : PSControl
    {
        public ListControl();
        public ListControl(IEnumerable<ListControlEntry> entries);
        public List<ListControlEntry> Entries { get; }
        public static ListControlBuilder Create(bool outOfBand=false);
    }
    public class ListControlBuilder
    {
        public ListControl EndList();
        public ListControlBuilder GroupByProperty(string property, CustomControl customControl=null, string label=null);
        public ListControlBuilder GroupByScriptBlock(string scriptBlock, CustomControl customControl=null, string label=null);
        public ListEntryBuilder StartEntry(IEnumerable<string> entrySelectedByType=null, IEnumerable<DisplayEntry> entrySelectedByCondition=null);
    }
    public sealed class ListControlEntry
    {
        public ListControlEntry();
        public ListControlEntry(IEnumerable<ListControlEntryItem> listItems);
        public ListControlEntry(IEnumerable<ListControlEntryItem> listItems, IEnumerable<string> selectedBy);
        public EntrySelectedBy EntrySelectedBy { get; }
        public List<ListControlEntryItem> Items { get; }
        public List<string> SelectedBy { get; }
    }
    public sealed class ListControlEntryItem
    {
        public ListControlEntryItem(string label, DisplayEntry entry);
        public DisplayEntry DisplayEntry { get; }
        public string FormatString { get; }
        public DisplayEntry ItemSelectionCondition { get; }
        public string Label { get; }
    }
    public class ListEntryBuilder
    {
        public ListEntryBuilder AddItemProperty(string property, string label=null, string format=null);
        public ListEntryBuilder AddItemScriptBlock(string scriptBlock, string label=null, string format=null);
        public ListControlBuilder EndEntry();
    }
    public abstract class LoopFlowException : FlowControlException
    {
        public string Label { get; }
    }
    public class MetadataException : RuntimeException
    {
        public MetadataException();
        protected MetadataException(SerializationInfo info, StreamingContext context);
        public MetadataException(string message);
        public MetadataException(string message, Exception innerException);
    }
    public class MethodException : ExtendedTypeSystemException
    {
        public MethodException();
        protected MethodException(SerializationInfo info, StreamingContext context);
        public MethodException(string message);
        public MethodException(string message, Exception innerException);
    }
    public class MethodInvocationException : MethodException
    {
        public MethodInvocationException();
        protected MethodInvocationException(SerializationInfo info, StreamingContext context);
        public MethodInvocationException(string message);
        public MethodInvocationException(string message, Exception innerException);
    }
    public enum ModuleAccessMode
    {
        Constant = 2,
        ReadOnly = 1,
        ReadWrite = 0,
    }
    public class ModuleIntrinsics
    {
        public static string GetModulePath(string currentProcessModulePath, string hklmMachineModulePath, string hkcuUserModulePath);
    }
    public enum ModuleType
    {
        Binary = 1,
        Cim = 3,
        Manifest = 2,
        Script = 0,
        Workflow = 4,
    }
    public sealed class OutputTypeAttribute : CmdletMetadataAttribute
    {
        public OutputTypeAttribute(params string[] type);
        public OutputTypeAttribute(params Type[] type);
        public string[] ParameterSetName { get; set; }
        public string ProviderCmdlet { get; set; }
        public PSTypeName[] Type { get; }
    }
    public sealed class PagingParameters
    {
        public ulong First { get; set; }
        public SwitchParameter IncludeTotalCount { get; set; }
        public ulong Skip { get; set; }
        public PSObject NewTotalCount(ulong totalCount, double accuracy);
    }
    public sealed class ParameterAttribute : ParsingBaseAttribute
    {
        public const string AllParameterSets = "__AllParameterSets";
        public ParameterAttribute();
        public bool DontShow { get; set; }
        public string HelpMessage { get; set; }
        public string HelpMessageBaseName { get; set; }
        public string HelpMessageResourceId { get; set; }
        public bool Mandatory { get; set; }
        public string ParameterSetName { get; set; }
        public int Position { get; set; }
        public bool ValueFromPipeline { get; set; }
        public bool ValueFromPipelineByPropertyName { get; set; }
        public bool ValueFromRemainingArguments { get; set; }
    }
    public class ParameterBindingException : RuntimeException
    {
        public ParameterBindingException();
        protected ParameterBindingException(SerializationInfo info, StreamingContext context);
        public ParameterBindingException(string message);
        public ParameterBindingException(string message, Exception innerException);
        public InvocationInfo CommandInvocation { get; }
        public string ErrorId { get; }
        public long Line { get; }
        public override string Message { get; }
        public long Offset { get; }
        public string ParameterName { get; }
        public Type ParameterType { get; }
        public Type TypeSpecified { get; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
    }
    public sealed class ParameterMetadata
    {
        public ParameterMetadata(ParameterMetadata other);
        public ParameterMetadata(string name);
        public ParameterMetadata(string name, Type parameterType);
        public Collection<string> Aliases { get; }
        public Collection<Attribute> Attributes { get; }
        public bool IsDynamic { get; set; }
        public string Name { get; set; }
        public Dictionary<string, ParameterSetMetadata> ParameterSets { get; }
        public Type ParameterType { get; set; }
        public bool SwitchParameter { get; }
        public static Dictionary<string, ParameterMetadata> GetParameterMetadata(Type type);
    }
    public sealed class ParameterSetMetadata
    {
        public string HelpMessage { get; set; }
        public string HelpMessageBaseName { get; set; }
        public string HelpMessageResourceId { get; set; }
        public bool IsMandatory { get; set; }
        public int Position { get; set; }
        public bool ValueFromPipeline { get; set; }
        public bool ValueFromPipelineByPropertyName { get; set; }
        public bool ValueFromRemainingArguments { get; set; }
    }
    public class ParentContainsErrorRecordException : SystemException
    {
        public ParentContainsErrorRecordException();
        public ParentContainsErrorRecordException(Exception wrapperException);
        protected ParentContainsErrorRecordException(SerializationInfo info, StreamingContext context);
        public ParentContainsErrorRecordException(string message);
        public ParentContainsErrorRecordException(string message, Exception innerException);
        public override string Message { get; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
    }
    public class ParseException : RuntimeException
    {
        public ParseException();
        public ParseException(ParseError[] errors);
        protected ParseException(SerializationInfo info, StreamingContext context);
        public ParseException(string message);
        public ParseException(string message, Exception innerException);
        public ParseError[] Errors { get; }
        public override string Message { get; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
    }
    public class ParsingMetadataException : MetadataException
    {
        public ParsingMetadataException();
        protected ParsingMetadataException(SerializationInfo info, StreamingContext context);
        public ParsingMetadataException(string message);
        public ParsingMetadataException(string message, Exception innerException);
    }
    public sealed class PathInfo
    {
        public PSDriveInfo Drive { get; }
        public string Path { get; }
        public ProviderInfo Provider { get; }
        public string ProviderPath { get; }
        public override string ToString();
    }
    public sealed class PathInfoStack : Stack<PathInfo>
    {
        public string Name { get; }
    }
    public sealed class PathIntrinsics
    {
        public PathInfo CurrentFileSystemLocation { get; }
        public PathInfo CurrentLocation { get; }
        public string Combine(string parent, string child);
        public PathInfo CurrentProviderLocation(string providerName);
        public Collection<string> GetResolvedProviderPathFromProviderPath(string path, string providerId);
        public Collection<string> GetResolvedProviderPathFromPSPath(string path, out ProviderInfo provider);
        public Collection<PathInfo> GetResolvedPSPathFromPSPath(string path);
        public string GetUnresolvedProviderPathFromPSPath(string path);
        public string GetUnresolvedProviderPathFromPSPath(string path, out ProviderInfo provider, out PSDriveInfo drive);
        public bool IsProviderQualified(string path);
        public bool IsPSAbsolute(string path, out string driveName);
        public bool IsValid(string path);
        public PathInfoStack LocationStack(string stackName);
        public string NormalizeRelativePath(string path, string basePath);
        public string ParseChildName(string path);
        public string ParseParent(string path, string root);
        public PathInfo PopLocation(string stackName);
        public void PushCurrentLocation(string stackName);
        public PathInfoStack SetDefaultLocationStack(string stackName);
        public PathInfo SetLocation(string path);
    }
    public class PipelineClosedException : RuntimeException
    {
        public PipelineClosedException();
        protected PipelineClosedException(SerializationInfo info, StreamingContext context);
        public PipelineClosedException(string message);
        public PipelineClosedException(string message, Exception innerException);
    }
    public class PipelineDepthException : SystemException, IContainsErrorRecord
    {
        public PipelineDepthException();
        protected PipelineDepthException(SerializationInfo info, StreamingContext context);
        public PipelineDepthException(string message);
        public PipelineDepthException(string message, Exception innerException);
        public int CallDepth { get; }
        public ErrorRecord ErrorRecord { get; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
    }
    public class PipelineStoppedException : RuntimeException
    {
        public PipelineStoppedException();
        protected PipelineStoppedException(SerializationInfo info, StreamingContext context);
        public PipelineStoppedException(string message);
        public PipelineStoppedException(string message, Exception innerException);
    }
    public static class Platform
    {
        public static bool IsCoreCLR { get; }
        public static bool IsIoT { get; }
        public static bool IsLinux { get; }
        public static bool IsNanoServer { get; }
        public static bool IsOSX { get; }
        public static bool IsWindows { get; }
    }
    public sealed class PowerShell : IDisposable
    {
        public PSCommand Commands { get; set; }
        public bool HadErrors { get; }
        public string HistoryString { get; set; }
        public Guid InstanceId { get; }
        public PSInvocationStateInfo InvocationStateInfo { get; }
        public bool IsNested { get; }
        public bool IsRunspaceOwner { get; }
        public Runspace Runspace { get; set; }
        public RunspacePool RunspacePool { get; set; }
        public PSDataStreams Streams { get; }
        public event EventHandler<PSInvocationStateChangedEventArgs> InvocationStateChanged;
        public PowerShell AddArgument(object value);
        public PowerShell AddCommand(CommandInfo commandInfo);
        public PowerShell AddCommand(string cmdlet);
        public PowerShell AddCommand(string cmdlet, bool useLocalScope);
        public PowerShell AddParameter(string parameterName);
        public PowerShell AddParameter(string parameterName, object value);
        public PowerShell AddParameters(IDictionary parameters);
        public PowerShell AddParameters(IList parameters);
        public PowerShell AddScript(string script);
        public PowerShell AddScript(string script, bool useLocalScope);
        public PowerShell AddStatement();
        public PSJobProxy AsJobProxy();
        public IAsyncResult BeginInvoke();
        public IAsyncResult BeginInvoke<T>(PSDataCollection<T> input);
        public IAsyncResult BeginInvoke<T>(PSDataCollection<T> input, PSInvocationSettings settings, AsyncCallback callback, object state);
        public IAsyncResult BeginInvoke<TInput, TOutput>(PSDataCollection<TInput> input, PSDataCollection<TOutput> output);
        public IAsyncResult BeginInvoke<TInput, TOutput>(PSDataCollection<TInput> input, PSDataCollection<TOutput> output, PSInvocationSettings settings, AsyncCallback callback, object state);
        public IAsyncResult BeginStop(AsyncCallback callback, object state);
        public Collection<PSObject> Connect();
        public IAsyncResult ConnectAsync();
        public IAsyncResult ConnectAsync(PSDataCollection<PSObject> output, AsyncCallback invocationCallback, object state);
        public static PowerShell Create();
        public static PowerShell Create(RunspaceMode runspace);
        public static PowerShell Create(InitialSessionState initialSessionState);
        public PowerShell CreateNestedPowerShell();
        public void Dispose();
        public PSDataCollection<PSObject> EndInvoke(IAsyncResult asyncResult);
        public void EndStop(IAsyncResult asyncResult);
        public Collection<PSObject> Invoke();
        public Collection<PSObject> Invoke(IEnumerable input);
        public Collection<PSObject> Invoke(IEnumerable input, PSInvocationSettings settings);
        public Collection<T> Invoke<T>();
        public Collection<T> Invoke<T>(IEnumerable input);
        public void Invoke<T>(IEnumerable input, IList<T> output);
        public void Invoke<T>(IEnumerable input, IList<T> output, PSInvocationSettings settings);
        public Collection<T> Invoke<T>(IEnumerable input, PSInvocationSettings settings);
        public void Invoke<TInput, TOutput>(PSDataCollection<TInput> input, PSDataCollection<TOutput> output, PSInvocationSettings settings);
        public void Stop();
    }
    public sealed class PowerShellStreams<TInput, TOutput> : IDisposable
    {
        public PowerShellStreams();
        public PowerShellStreams(PSDataCollection<TInput> pipelineInput);
        public PSDataCollection<DebugRecord> DebugStream { get; set; }
        public PSDataCollection<ErrorRecord> ErrorStream { get; set; }
        public PSDataCollection<InformationRecord> InformationStream { get; set; }
        public PSDataCollection<TInput> InputStream { get; set; }
        public PSDataCollection<TOutput> OutputStream { get; set; }
        public PSDataCollection<ProgressRecord> ProgressStream { get; set; }
        public PSDataCollection<VerboseRecord> VerboseStream { get; set; }
        public PSDataCollection<WarningRecord> WarningStream { get; set; }
        public void CloseAll();
        public void Dispose();
    }
    public enum PowerShellStreamType
    {
        Debug = 5,
        Error = 2,
        Information = 7,
        Input = 0,
        Output = 1,
        Progress = 6,
        Verbose = 4,
        Warning = 3,
    }
    public sealed class ProcessRunspaceDebugEndEventArgs : EventArgs
    {
        public ProcessRunspaceDebugEndEventArgs(Runspace runspace);
        public Runspace Runspace { get; }
    }
    public class ProgressRecord
    {
        public ProgressRecord(int activityId, string activity, string statusDescription);
        public string Activity { get; set; }
        public int ActivityId { get; }
        public string CurrentOperation { get; set; }
        public int ParentActivityId { get; set; }
        public int PercentComplete { get; set; }
        public ProgressRecordType RecordType { get; set; }
        public int SecondsRemaining { get; set; }
        public string StatusDescription { get; set; }
        public override string ToString();
    }
    public enum ProgressRecordType
    {
        Completed = 1,
        Processing = 0,
    }
    public sealed class PropertyCmdletProviderIntrinsics
    {
        public void Clear(string path, Collection<string> propertyToClear);
        public void Clear(string[] path, Collection<string> propertyToClear, bool force, bool literalPath);
        public Collection<PSObject> Copy(string sourcePath, string sourceProperty, string destinationPath, string destinationProperty);
        public Collection<PSObject> Copy(string[] sourcePath, string sourceProperty, string destinationPath, string destinationProperty, bool force, bool literalPath);
        public Collection<PSObject> Get(string path, Collection<string> providerSpecificPickList);
        public Collection<PSObject> Get(string[] path, Collection<string> providerSpecificPickList, bool literalPath);
        public Collection<PSObject> Move(string sourcePath, string sourceProperty, string destinationPath, string destinationProperty);
        public Collection<PSObject> Move(string[] sourcePath, string sourceProperty, string destinationPath, string destinationProperty, bool force, bool literalPath);
        public Collection<PSObject> New(string path, string propertyName, string propertyTypeName, object value);
        public Collection<PSObject> New(string[] path, string propertyName, string propertyTypeName, object value, bool force, bool literalPath);
        public void Remove(string path, string propertyName);
        public void Remove(string[] path, string propertyName, bool force, bool literalPath);
        public Collection<PSObject> Rename(string path, string sourceProperty, string destinationProperty);
        public Collection<PSObject> Rename(string[] path, string sourceProperty, string destinationProperty, bool force, bool literalPath);
        public Collection<PSObject> Set(string path, PSObject propertyValue);
        public Collection<PSObject> Set(string[] path, PSObject propertyValue, bool force, bool literalPath);
    }
    public class PropertyNotFoundException : ExtendedTypeSystemException
    {
        public PropertyNotFoundException();
        protected PropertyNotFoundException(SerializationInfo info, StreamingContext context);
        public PropertyNotFoundException(string message);
        public PropertyNotFoundException(string message, Exception innerException);
    }
    public static class ProviderCmdlet
    {
        public const string AddContent = "Add-Content";
        public const string ClearContent = "Clear-Content";
        public const string ClearItem = "Clear-Item";
        public const string ClearItemProperty = "Clear-ItemProperty";
        public const string ConvertPath = "Convert-Path";
        public const string CopyItem = "Copy-Item";
        public const string CopyItemProperty = "Copy-ItemProperty";
        public const string GetAcl = "Get-Acl";
        public const string GetChildItem = "Get-ChildItem";
        public const string GetContent = "Get-Content";
        public const string GetItem = "Get-Item";
        public const string GetItemProperty = "Get-ItemProperty";
        public const string GetLocation = "Get-Location";
        public const string GetPSDrive = "Get-PSDrive";
        public const string GetPSProvider = "Get-PSProvider";
        public const string InvokeItem = "Invoke-Item";
        public const string JoinPath = "Join-Path";
        public const string MoveItem = "Move-Item";
        public const string MoveItemProperty = "Move-ItemProperty";
        public const string NewItem = "New-Item";
        public const string NewItemProperty = "New-ItemProperty";
        public const string NewPSDrive = "New-PSDrive";
        public const string PopLocation = "Pop-Location";
        public const string PushLocation = "Push-Location";
        public const string RemoveItem = "Remove-Item";
        public const string RemoveItemProperty = "Remove-ItemProperty";
        public const string RemovePSDrive = "Remove-PSDrive";
        public const string RenameItem = "Rename-Item";
        public const string RenameItemProperty = "Rename-ItemProperty";
        public const string ResolvePath = "Resolve-Path";
        public const string SetAcl = "Set-Acl";
        public const string SetContent = "Set-Content";
        public const string SetItem = "Set-Item";
        public const string SetItemProperty = "Set-ItemProperty";
        public const string SetLocation = "Set-Location";
        public const string SplitPath = "Split-Path";
        public const string TestPath = "Test-Path";
    }
    public class ProviderInfo
    {
        protected ProviderInfo(ProviderInfo providerInfo);
        public ProviderCapabilities Capabilities { get; }
        public string Description { get; set; }
        public Collection<PSDriveInfo> Drives { get; }
        public string HelpFile { get; }
        public string Home { get; set; }
        public Type ImplementingType { get; }
        public PSModuleInfo Module { get; }
        public string ModuleName { get; }
        public string Name { get; }
        public PSSnapInInfo PSSnapIn { get; }
        public bool VolumeSeparatedByColon { get; }
        public override string ToString();
    }
    public sealed class ProviderIntrinsics
    {
        public ChildItemCmdletProviderIntrinsics ChildItem { get; }
        public ContentCmdletProviderIntrinsics Content { get; }
        public ItemCmdletProviderIntrinsics Item { get; }
        public PropertyCmdletProviderIntrinsics Property { get; }
        public SecurityDescriptorCmdletProviderIntrinsics SecurityDescriptor { get; }
    }
    public class ProviderInvocationException : RuntimeException
    {
        public ProviderInvocationException();
        protected ProviderInvocationException(SerializationInfo info, StreamingContext context);
        public ProviderInvocationException(string message);
        public ProviderInvocationException(string message, Exception innerException);
        public override ErrorRecord ErrorRecord { get; }
        public override string Message { get; }
        public ProviderInfo ProviderInfo { get; }
    }
    public class ProviderNameAmbiguousException : ProviderNotFoundException
    {
        public ProviderNameAmbiguousException();
        protected ProviderNameAmbiguousException(SerializationInfo info, StreamingContext context);
        public ProviderNameAmbiguousException(string message);
        public ProviderNameAmbiguousException(string message, Exception innerException);
        public ReadOnlyCollection<ProviderInfo> PossibleMatches { get; }
    }
    public class ProviderNotFoundException : SessionStateException
    {
        public ProviderNotFoundException();
        protected ProviderNotFoundException(SerializationInfo info, StreamingContext context);
        public ProviderNotFoundException(string message);
        public ProviderNotFoundException(string message, Exception innerException);
    }
    public sealed class ProxyCommand
    {
        public static string Create(CommandMetadata commandMetadata);
        public static string Create(CommandMetadata commandMetadata, string helpComment);
        public static string Create(CommandMetadata commandMetadata, string helpComment, bool generateDynamicParameters);
        public static string GetBegin(CommandMetadata commandMetadata);
        public static string GetCmdletBindingAttribute(CommandMetadata commandMetadata);
        public static string GetDynamicParam(CommandMetadata commandMetadata);
        public static string GetEnd(CommandMetadata commandMetadata);
        public static string GetHelpComments(PSObject help);
        public static string GetParamBlock(CommandMetadata commandMetadata);
        public static string GetProcess(CommandMetadata commandMetadata);
    }
    public class PSAdaptedProperty : PSProperty
    {
        public PSAdaptedProperty(string name, object tag);
        public object BaseObject { get; }
        public object Tag { get; }
        public override PSMemberInfo Copy();
    }
    public class PSAliasProperty : PSPropertyInfo
    {
        public PSAliasProperty(string name, string referencedMemberName);
        public PSAliasProperty(string name, string referencedMemberName, Type conversionType);
        public Type ConversionType { get; }
        public override bool IsGettable { get; }
        public override bool IsSettable { get; }
        public override PSMemberTypes MemberType { get; }
        public string ReferencedMemberName { get; }
        public override string TypeNameOfValue { get; }
        public override object Value { get; set; }
        public override PSMemberInfo Copy();
        public override string ToString();
    }
    public class PSArgumentException : ArgumentException, IContainsErrorRecord
    {
        public PSArgumentException();
        protected PSArgumentException(SerializationInfo info, StreamingContext context);
        public PSArgumentException(string message);
        public PSArgumentException(string message, Exception innerException);
        public PSArgumentException(string message, string paramName);
        public ErrorRecord ErrorRecord { get; }
        public override string Message { get; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
    }
    public class PSArgumentNullException : ArgumentNullException, IContainsErrorRecord
    {
        public PSArgumentNullException();
        protected PSArgumentNullException(SerializationInfo info, StreamingContext context);
        public PSArgumentNullException(string paramName);
        public PSArgumentNullException(string message, Exception innerException);
        public PSArgumentNullException(string paramName, string message);
        public ErrorRecord ErrorRecord { get; }
        public override string Message { get; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
    }
    public class PSArgumentOutOfRangeException : ArgumentOutOfRangeException, IContainsErrorRecord
    {
        public PSArgumentOutOfRangeException();
        protected PSArgumentOutOfRangeException(SerializationInfo info, StreamingContext context);
        public PSArgumentOutOfRangeException(string paramName);
        public PSArgumentOutOfRangeException(string message, Exception innerException);
        public PSArgumentOutOfRangeException(string paramName, object actualValue, string message);
        public ErrorRecord ErrorRecord { get; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
    }
    public sealed class PSChildJobProxy : Job2
    {
        public override bool HasMoreData { get; }
        public override string Location { get; }
        public override string StatusMessage { get; }
        public event EventHandler<JobDataAddedEventArgs> JobDataAdded;
        protected override void Dispose(bool disposing);
        public override void ResumeJob();
        public override void ResumeJobAsync();
        public override void StartJob();
        public override void StartJobAsync();
        public override void StopJob();
        public override void StopJob(bool force, string reason);
        public override void StopJobAsync();
        public override void StopJobAsync(bool force, string reason);
        public override void SuspendJob();
        public override void SuspendJob(bool force, string reason);
        public override void SuspendJobAsync();
        public override void SuspendJobAsync(bool force, string reason);
        public override void UnblockJob();
        public override void UnblockJobAsync();
    }
    public sealed class PSClassInfo
    {
        public string HelpFile { get; }
        public ReadOnlyCollection<PSClassMemberInfo> Members { get; }
        public PSModuleInfo Module { get; }
        public string Name { get; }
        public void UpdateMembers(IList<PSClassMemberInfo> members);
    }
    public sealed class PSClassMemberInfo
    {
        public string DefaultValue { get; }
        public string Name { get; }
        public string TypeName { get; }
    }
    public abstract class PSCmdlet : Cmdlet
    {
        protected PSCmdlet();
        public PSEventManager Events { get; }
        public PSHost Host { get; }
        public CommandInvocationIntrinsics InvokeCommand { get; }
        public ProviderIntrinsics InvokeProvider { get; }
        public JobManager JobManager { get; }
        public JobRepository JobRepository { get; }
        public InvocationInfo MyInvocation { get; }
        public PagingParameters PagingParameters { get; }
        public string ParameterSetName { get; }
        public SessionState SessionState { get; }
        public PathInfo CurrentProviderLocation(string providerId);
        public Collection<string> GetResolvedProviderPathFromPSPath(string path, out ProviderInfo provider);
        public string GetUnresolvedProviderPathFromPSPath(string path);
        public object GetVariableValue(string name);
        public object GetVariableValue(string name, object defaultValue);
    }
    public class PSCodeMethod : PSMethodInfo
    {
        public PSCodeMethod(string name, MethodInfo codeReference);
        public MethodInfo CodeReference { get; }
        public override PSMemberTypes MemberType { get; }
        public override Collection<string> OverloadDefinitions { get; }
        public override string TypeNameOfValue { get; }
        public override PSMemberInfo Copy();
        public override object Invoke(params object[] arguments);
        public override string ToString();
    }
    public class PSCodeProperty : PSPropertyInfo
    {
        public PSCodeProperty(string name, MethodInfo getterCodeReference);
        public PSCodeProperty(string name, MethodInfo getterCodeReference, MethodInfo setterCodeReference);
        public MethodInfo GetterCodeReference { get; }
        public override bool IsGettable { get; }
        public override bool IsSettable { get; }
        public override PSMemberTypes MemberType { get; }
        public MethodInfo SetterCodeReference { get; }
        public override string TypeNameOfValue { get; }
        public override object Value { get; set; }
        public override PSMemberInfo Copy();
        public override string ToString();
    }
    public sealed class PSCommand
    {
        public PSCommand();
        public CommandCollection Commands { get; }
        public PSCommand AddArgument(object value);
        public PSCommand AddCommand(Command command);
        public PSCommand AddCommand(string command);
        public PSCommand AddCommand(string cmdlet, bool useLocalScope);
        public PSCommand AddParameter(string parameterName);
        public PSCommand AddParameter(string parameterName, object value);
        public PSCommand AddScript(string script);
        public PSCommand AddScript(string script, bool useLocalScope);
        public PSCommand AddStatement();
        public void Clear();
        public PSCommand Clone();
    }
    public abstract class PSControl
    {
        protected PSControl();
        public PSControlGroupBy GroupBy { get; set; }
        public bool OutOfBand { get; set; }
    }
    public sealed class PSControlGroupBy
    {
        public PSControlGroupBy();
        public CustomControl CustomControl { get; set; }
        public DisplayEntry Expression { get; set; }
        public string Label { get; set; }
    }
    public sealed class PSCredential : ISerializable
    {
        public PSCredential(PSObject pso);
        public PSCredential(string userName, SecureString password);
        public static PSCredential Empty { get; }
        public static GetSymmetricEncryptionKey GetSymmetricEncryptionKeyDelegate { get; set; }
        public SecureString Password { get; }
        public string UserName { get; }
        public NetworkCredential GetNetworkCredential();
        public void GetObjectData(SerializationInfo info, StreamingContext context);
        public static explicit operator NetworkCredential (PSCredential credential);
    }
    public enum PSCredentialTypes
    {
        Default = 3,
        Domain = 2,
        Generic = 1,
    }
    public enum PSCredentialUIOptions
    {
        AlwaysPrompt = 2,
        Default = 1,
        None = 0,
        ReadOnlyUserName = 3,
        ValidateUserNameSyntax = 1,
    }
    public class PSCustomObject
    {
        public override string ToString();
    }
    public class PSDataCollection<T> : ICollection, ICollection<T>, IDisposable, IEnumerable, IEnumerable<T>, IList, IList<T>, ISerializable
    {
        public PSDataCollection();
        public PSDataCollection(IEnumerable<T> items);
        public PSDataCollection(int capacity);
        protected PSDataCollection(SerializationInfo info, StreamingContext context);
        public bool BlockingEnumerator { get; set; }
        public int Count { get; }
        public int DataAddedCount { get; set; }
        public bool EnumeratorNeverBlocks { get; set; }
        public bool IsAutoGenerated { get; set; }
        public bool IsOpen { get; }
        public bool IsReadOnly { get; }
        public T this[int index] { get; set; }
        public bool SerializeInput { get; set; }
        bool System.Collections.ICollection.IsSynchronized { get; }
        object System.Collections.ICollection.SyncRoot { get; }
        bool System.Collections.IList.IsFixedSize { get; }
        bool System.Collections.IList.IsReadOnly { get; }
        object System.Collections.IList.this[int index] { get; set; }
        public event EventHandler Completed;
        public event EventHandler<DataAddedEventArgs> DataAdded;
        public event EventHandler<DataAddingEventArgs> DataAdding;
        public void Add(T item);
        public void Clear();
        public void Complete();
        public bool Contains(T item);
        public void CopyTo(T[] array, int arrayIndex);
        public void Dispose();
        protected void Dispose(bool disposing);
        public IEnumerator<T> GetEnumerator();
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context);
        public int IndexOf(T item);
        public void Insert(int index, T item);
        protected virtual void InsertItem(Guid psInstanceId, int index, T item);
        public static implicit operator PSDataCollection<T> (bool valueToConvert);
        public static implicit operator PSDataCollection<T> (byte valueToConvert);
        public static implicit operator PSDataCollection<T> (Hashtable valueToConvert);
        public static implicit operator PSDataCollection<T> (int valueToConvert);
        public static implicit operator PSDataCollection<T> (object[] arrayToConvert);
        public static implicit operator PSDataCollection<T> (string valueToConvert);
        public static implicit operator PSDataCollection<T> (T valueToConvert);
        public Collection<T> ReadAll();
        public bool Remove(T item);
        public void RemoveAt(int index);
        protected virtual void RemoveItem(int index);
        void System.Collections.ICollection.CopyTo(Array array, int index);
        IEnumerator System.Collections.IEnumerable.GetEnumerator();
        int System.Collections.IList.Add(object value);
        bool System.Collections.IList.Contains(object value);
        int System.Collections.IList.IndexOf(object value);
        void System.Collections.IList.Insert(int index, object value);
        void System.Collections.IList.Remove(object value);
    }
    public sealed class PSDataStreams
    {
        public PSDataCollection<DebugRecord> Debug { get; set; }
        public PSDataCollection<ErrorRecord> Error { get; set; }
        public PSDataCollection<InformationRecord> Information { get; set; }
        public PSDataCollection<ProgressRecord> Progress { get; set; }
        public PSDataCollection<VerboseRecord> Verbose { get; set; }
        public PSDataCollection<WarningRecord> Warning { get; set; }
        public void ClearStreams();
    }
    public class PSDebugContext
    {
        public PSDebugContext(InvocationInfo invocationInfo, List<Breakpoint> breakpoints);
        public Breakpoint[] Breakpoints { get; }
        public InvocationInfo InvocationInfo { get; }
    }
    public sealed class PSDefaultValueAttribute : ParsingBaseAttribute
    {
        public PSDefaultValueAttribute();
        public string Help { get; set; }
        public object Value { get; set; }
    }
    public class PSDriveInfo : IComparable
    {
        protected PSDriveInfo(PSDriveInfo driveInfo);
        public PSDriveInfo(string name, ProviderInfo provider, string root, string description, PSCredential credential);
        public PSDriveInfo(string name, ProviderInfo provider, string root, string description, PSCredential credential, bool persist);
        public PSDriveInfo(string name, ProviderInfo provider, string root, string description, PSCredential credential, string displayRoot);
        public PSCredential Credential { get; }
        public string CurrentLocation { get; set; }
        public string Description { get; set; }
        public string DisplayRoot { get; }
        public Nullable<long> MaximumSize { get; }
        public string Name { get; }
        public ProviderInfo Provider { get; }
        public string Root { get; }
        public bool VolumeSeparatedByColon { get; }
        public int CompareTo(PSDriveInfo drive);
        public int CompareTo(object obj);
        public bool Equals(PSDriveInfo drive);
        public override bool Equals(object obj);
        public override int GetHashCode();
        public static bool operator ==(PSDriveInfo drive1, PSDriveInfo drive2);
        public static bool operator >(PSDriveInfo drive1, PSDriveInfo drive2);
        public static bool operator !=(PSDriveInfo drive1, PSDriveInfo drive2);
        public static bool operator <(PSDriveInfo drive1, PSDriveInfo drive2);
        public override string ToString();
    }
    public class PSDynamicMember : PSMemberInfo
    {
        public override PSMemberTypes MemberType { get; }
        public override string TypeNameOfValue { get; }
        public override object Value { get; set; }
        public override PSMemberInfo Copy();
        public override string ToString();
    }
    public sealed class PSEngineEvent
    {
        public const string Exiting = "PowerShell.Exiting";
        public const string OnIdle = "PowerShell.OnIdle";
        public const string WorkflowJobStartEvent = "PowerShell.WorkflowJobStartEvent";
    }
    public class PSEvent : PSMemberInfo
    {
        public override PSMemberTypes MemberType { get; }
        public override string TypeNameOfValue { get; }
        public sealed override object Value { get; set; }
        public override PSMemberInfo Copy();
        public override string ToString();
    }
    public class PSEventArgs : EventArgs
    {
        public string ComputerName { get; }
        public int EventIdentifier { get; }
        public PSObject MessageData { get; }
        public Guid RunspaceId { get; }
        public object Sender { get; }
        public object[] SourceArgs { get; }
        public EventArgs SourceEventArgs { get; }
        public string SourceIdentifier { get; }
        public DateTime TimeGenerated { get; }
    }
    public class PSEventArgsCollection : IEnumerable, IEnumerable<PSEventArgs>
    {
        public PSEventArgsCollection();
        public int Count { get; }
        public PSEventArgs this[int index] { get; }
        public object SyncRoot { get; }
        public event PSEventReceivedEventHandler PSEventReceived;
        public IEnumerator<PSEventArgs> GetEnumerator();
        public void RemoveAt(int index);
        IEnumerator System.Collections.IEnumerable.GetEnumerator();
    }
    public class PSEventHandler
    {
        protected PSEventManager eventManager;
        protected PSObject extraData;
        protected object sender;
        protected string sourceIdentifier;
        public PSEventHandler();
        public PSEventHandler(PSEventManager eventManager, object sender, string sourceIdentifier, PSObject extraData);
    }
    public class PSEventJob : Job
    {
        public PSEventJob(PSEventManager eventManager, PSEventSubscriber subscriber, ScriptBlock action, string name);
        public override bool HasMoreData { get; }
        public override string Location { get; }
        public PSModuleInfo Module { get; }
        public override string StatusMessage { get; }
        public override void StopJob();
    }
    public abstract class PSEventManager
    {
        protected PSEventManager();
        public PSEventArgsCollection ReceivedEvents { get; }
        public abstract List<PSEventSubscriber> Subscribers { get; }
        protected abstract PSEventArgs CreateEvent(string sourceIdentifier, object sender, object[] args, PSObject extraData);
        public PSEventArgs GenerateEvent(string sourceIdentifier, object sender, object[] args, PSObject extraData);
        public PSEventArgs GenerateEvent(string sourceIdentifier, object sender, object[] args, PSObject extraData, bool processInCurrentThread, bool waitForCompletionInCurrentThread);
        public abstract IEnumerable<PSEventSubscriber> GetEventSubscribers(string sourceIdentifier);
        protected int GetNextEventId();
        protected abstract void ProcessNewEvent(PSEventArgs newEvent, bool processInCurrentThread);
        protected internal virtual void ProcessNewEvent(PSEventArgs newEvent, bool processInCurrentThread, bool waitForCompletionWhenInCurrentThread);
        public abstract PSEventSubscriber SubscribeEvent(object source, string eventName, string sourceIdentifier, PSObject data, PSEventReceivedEventHandler handlerDelegate, bool supportEvent, bool forwardEvent);
        public abstract PSEventSubscriber SubscribeEvent(object source, string eventName, string sourceIdentifier, PSObject data, PSEventReceivedEventHandler handlerDelegate, bool supportEvent, bool forwardEvent, int maxTriggerCount);
        public abstract PSEventSubscriber SubscribeEvent(object source, string eventName, string sourceIdentifier, PSObject data, ScriptBlock action, bool supportEvent, bool forwardEvent);
        public abstract PSEventSubscriber SubscribeEvent(object source, string eventName, string sourceIdentifier, PSObject data, ScriptBlock action, bool supportEvent, bool forwardEvent, int maxTriggerCount);
        public abstract void UnsubscribeEvent(PSEventSubscriber subscriber);
    }
    public delegate void PSEventReceivedEventHandler(object sender, PSEventArgs e);
    public class PSEventSubscriber : IEquatable<PSEventSubscriber>
    {
        public PSEventJob Action { get; }
        public string EventName { get; }
        public bool ForwardEvent { get; }
        public PSEventReceivedEventHandler HandlerDelegate { get; }
        public string SourceIdentifier { get; }
        public object SourceObject { get; }
        public int SubscriptionId { get; set; }
        public bool SupportEvent { get; }
        public event PSEventUnsubscribedEventHandler Unsubscribed;
        public bool Equals(PSEventSubscriber other);
        public override int GetHashCode();
    }
    public class PSEventUnsubscribedEventArgs : EventArgs
    {
        public PSEventSubscriber EventSubscriber { get; }
    }
    public delegate void PSEventUnsubscribedEventHandler(object sender, PSEventUnsubscribedEventArgs e);
    public class PSInvalidCastException : InvalidCastException, IContainsErrorRecord
    {
        public PSInvalidCastException();
        protected PSInvalidCastException(SerializationInfo info, StreamingContext context);
        public PSInvalidCastException(string message);
        public PSInvalidCastException(string message, Exception innerException);
        public ErrorRecord ErrorRecord { get; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
    }
    public class PSInvalidOperationException : InvalidOperationException, IContainsErrorRecord
    {
        public PSInvalidOperationException();
        protected PSInvalidOperationException(SerializationInfo info, StreamingContext context);
        public PSInvalidOperationException(string message);
        public PSInvalidOperationException(string message, Exception innerException);
        public ErrorRecord ErrorRecord { get; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
    }
    public sealed class PSInvocationSettings
    {
        public PSInvocationSettings();
        public bool AddToHistory { get; set; }
        public Nullable<ActionPreference> ErrorActionPreference { get; set; }
        public bool ExposeFlowControlExceptions { get; set; }
        public bool FlowImpersonationPolicy { get; set; }
        public PSHost Host { get; set; }
        public RemoteStreamOptions RemoteStreamOptions { get; set; }
    }
    public enum PSInvocationState
    {
        Completed = 4,
        Disconnected = 6,
        Failed = 5,
        NotStarted = 0,
        Running = 1,
        Stopped = 3,
        Stopping = 2,
    }
    public sealed class PSInvocationStateChangedEventArgs : EventArgs
    {
        public PSInvocationStateInfo InvocationStateInfo { get; }
    }
    public sealed class PSInvocationStateInfo
    {
        public Exception Reason { get; }
        public PSInvocationState State { get; }
    }
    public sealed class PSJobProxy : Job2
    {
        public override bool HasMoreData { get; }
        public override string Location { get; }
        public Guid RemoteJobInstanceId { get; }
        public bool RemoveRemoteJobOnCompletion { get; set; }
        public Runspace Runspace { get; set; }
        public RunspacePool RunspacePool { get; set; }
        public override string StatusMessage { get; }
        public event EventHandler<AsyncCompletedEventArgs> RemoveJobCompleted;
        public static ICollection<PSJobProxy> Create(Runspace runspace);
        public static ICollection<PSJobProxy> Create(Runspace runspace, Hashtable filter);
        public static ICollection<PSJobProxy> Create(Runspace runspace, Hashtable filter, bool receiveImmediately);
        public static ICollection<PSJobProxy> Create(Runspace runspace, Hashtable filter, EventHandler<JobDataAddedEventArgs> dataAdded, EventHandler<JobStateEventArgs> stateChanged);
        public static ICollection<PSJobProxy> Create(RunspacePool runspacePool);
        public static ICollection<PSJobProxy> Create(RunspacePool runspacePool, Hashtable filter);
        public static ICollection<PSJobProxy> Create(RunspacePool runspacePool, Hashtable filter, bool receiveImmediately);
        public static ICollection<PSJobProxy> Create(RunspacePool runspacePool, Hashtable filter, EventHandler<JobDataAddedEventArgs> dataAdded, EventHandler<JobStateEventArgs> stateChanged);
        protected override void Dispose(bool disposing);
        public void ReceiveJob();
        public void ReceiveJob(EventHandler<JobDataAddedEventArgs> dataAdded, EventHandler<JobStateEventArgs> stateChanged);
        public void RemoveJob(bool removeRemoteJob);
        public void RemoveJob(bool removeRemoteJob, bool force);
        public void RemoveJobAsync(bool removeRemoteJob);
        public void RemoveJobAsync(bool removeRemoteJob, bool force);
        public override void ResumeJob();
        public override void ResumeJobAsync();
        public override void StartJob();
        public void StartJob(EventHandler<JobDataAddedEventArgs> dataAdded, EventHandler<JobStateEventArgs> stateChanged, PSDataCollection<object> input);
        public void StartJob(PSDataCollection<object> input);
        public override void StartJobAsync();
        public void StartJobAsync(EventHandler<JobDataAddedEventArgs> dataAdded, EventHandler<JobStateEventArgs> stateChanged, PSDataCollection<object> input);
        public void StartJobAsync(PSDataCollection<object> input);
        public override void StopJob();
        public override void StopJob(bool force, string reason);
        public override void StopJobAsync();
        public override void StopJobAsync(bool force, string reason);
        public override void SuspendJob();
        public override void SuspendJob(bool force, string reason);
        public override void SuspendJobAsync();
        public override void SuspendJobAsync(bool force, string reason);
        public override void UnblockJob();
        public override void UnblockJobAsync();
    }
    public sealed class PSJobStartEventArgs : EventArgs
    {
        public PSJobStartEventArgs(Job job, Debugger debugger, bool isAsync);
        public Debugger Debugger { get; }
        public bool IsAsync { get; }
        public Job Job { get; }
    }
    public enum PSLanguageMode
    {
        ConstrainedLanguage = 3,
        FullLanguage = 0,
        NoLanguage = 2,
        RestrictedLanguage = 1,
    }
    public class PSListModifier
    {
        public PSListModifier();
        public PSListModifier(Hashtable hash);
        public PSListModifier(Collection<object> removeItems, Collection<object> addItems);
        public PSListModifier(object replacementItems);
        public Collection<object> Add { get; }
        public Collection<object> Remove { get; }
        public Collection<object> Replace { get; }
        public void ApplyTo(IList collectionToUpdate);
        public void ApplyTo(object collectionToUpdate);
    }
    public class PSListModifier<T> : PSListModifier
    {
        public PSListModifier();
        public PSListModifier(Hashtable hash);
        public PSListModifier(Collection<object> removeItems, Collection<object> addItems);
        public PSListModifier(object replacementItems);
    }
    public abstract class PSMemberInfo
    {
        protected PSMemberInfo();
        public bool IsInstance { get; }
        public abstract PSMemberTypes MemberType { get; }
        public string Name { get; }
        public abstract string TypeNameOfValue { get; }
        public abstract object Value { get; set; }
        public abstract PSMemberInfo Copy();
        protected void SetMemberName(string name);
    }
    public abstract class PSMemberInfoCollection<T> : IEnumerable, IEnumerable<T> where T : PSMemberInfo
    {
        protected PSMemberInfoCollection();
        public abstract T this[string name] { get; }
        public abstract void Add(T member);
        public abstract void Add(T member, bool preValidated);
        public abstract IEnumerator<T> GetEnumerator();
        public abstract ReadOnlyPSMemberInfoCollection<T> Match(string name);
        public abstract ReadOnlyPSMemberInfoCollection<T> Match(string name, PSMemberTypes memberTypes);
        public abstract void Remove(string name);
        IEnumerator System.Collections.IEnumerable.GetEnumerator();
    }
    public class PSMemberSet : PSMemberInfo
    {
        public PSMemberSet(string name);
        public PSMemberSet(string name, IEnumerable<PSMemberInfo> members);
        public bool InheritMembers { get; }
        public PSMemberInfoCollection<PSMemberInfo> Members { get; }
        public override PSMemberTypes MemberType { get; }
        public PSMemberInfoCollection<PSMethodInfo> Methods { get; }
        public PSMemberInfoCollection<PSPropertyInfo> Properties { get; }
        public override string TypeNameOfValue { get; }
        public override object Value { get; set; }
        public override PSMemberInfo Copy();
        public override string ToString();
    }
    public enum PSMemberTypes
    {
        AliasProperty = 1,
        All = 8191,
        CodeMethod = 128,
        CodeProperty = 2,
        Dynamic = 4096,
        Event = 2048,
        MemberSet = 1024,
        Method = 64,
        Methods = 448,
        NoteProperty = 8,
        ParameterizedProperty = 512,
        Properties = 31,
        Property = 4,
        PropertySet = 32,
        ScriptMethod = 256,
        ScriptProperty = 16,
    }
    public enum PSMemberViewTypes
    {
        Adapted = 2,
        All = 7,
        Base = 4,
        Extended = 1,
    }
    public class PSMethod : PSMethodInfo
    {
        public override PSMemberTypes MemberType { get; }
        public override Collection<string> OverloadDefinitions { get; }
        public override string TypeNameOfValue { get; }
        public override PSMemberInfo Copy();
        public override object Invoke(params object[] arguments);
        public override string ToString();
    }
    public abstract class PSMethodInfo : PSMemberInfo
    {
        protected PSMethodInfo();
        public abstract Collection<string> OverloadDefinitions { get; }
        public sealed override object Value { get; set; }
        public abstract object Invoke(params object[] arguments);
    }
    public enum PSModuleAutoLoadingPreference
    {
        All = 2,
        ModuleQualified = 1,
        None = 0,
    }
    public sealed class PSModuleInfo
    {
        public PSModuleInfo(bool linkToGlobal);
        public PSModuleInfo(ScriptBlock scriptBlock);
        public ModuleAccessMode AccessMode { get; set; }
        public string Author { get; }
        public Version ClrVersion { get; }
        public string CompanyName { get; }
        public IEnumerable<string> CompatiblePSEditions { get; }
        public string Copyright { get; }
        public string Definition { get; }
        public string Description { get; set; }
        public Version DotNetFrameworkVersion { get; }
        public Dictionary<string, AliasInfo> ExportedAliases { get; }
        public Dictionary<string, CmdletInfo> ExportedCmdlets { get; }
        public Dictionary<string, CommandInfo> ExportedCommands { get; }
        public ReadOnlyCollection<string> ExportedDscResources { get; }
        public ReadOnlyCollection<string> ExportedFormatFiles { get; }
        public Dictionary<string, FunctionInfo> ExportedFunctions { get; }
        public ReadOnlyCollection<string> ExportedTypeFiles { get; }
        public Dictionary<string, PSVariable> ExportedVariables { get; }
        public Dictionary<string, FunctionInfo> ExportedWorkflows { get; }
        public IEnumerable<string> FileList { get; }
        public Guid Guid { get; }
        public string HelpInfoUri { get; }
        public Uri IconUri { get; }
        public Assembly ImplementingAssembly { get; }
        public Uri LicenseUri { get; }
        public bool LogPipelineExecutionDetails { get; set; }
        public string ModuleBase { get; }
        public IEnumerable<object> ModuleList { get; }
        public ModuleType ModuleType { get; }
        public string Name { get; }
        public ReadOnlyCollection<PSModuleInfo> NestedModules { get; }
        public ScriptBlock OnRemove { get; set; }
        public string Path { get; }
        public string PowerShellHostName { get; }
        public Version PowerShellHostVersion { get; }
        public Version PowerShellVersion { get; }
        public string Prefix { get; }
        public object PrivateData { get; set; }
        public ProcessorArchitecture ProcessorArchitecture { get; }
        public Uri ProjectUri { get; }
        public string ReleaseNotes { get; }
        public Uri RepositorySourceLocation { get; }
        public IEnumerable<string> RequiredAssemblies { get; }
        public ReadOnlyCollection<PSModuleInfo> RequiredModules { get; }
        public string RootModule { get; }
        public IEnumerable<string> Scripts { get; }
        public SessionState SessionState { get; set; }
        public IEnumerable<string> Tags { get; }
        public static bool UseAppDomainLevelModuleCache { get; set; }
        public Version Version { get; }
        public PSObject AsCustomObject();
        public static void ClearAppDomainLevelModulePathCache();
        public PSModuleInfo Clone();
        public static object GetAppDomainLevelModuleCache();
        public ReadOnlyDictionary<string, TypeDefinitionAst> GetExportedTypeDefinitions();
        public PSVariable GetVariableFromCallersModule(string variableName);
        public object Invoke(ScriptBlock sb, params object[] args);
        public ScriptBlock NewBoundScriptBlock(ScriptBlock scriptBlockToBind);
        public override string ToString();
    }
    public class PSNoteProperty : PSPropertyInfo
    {
        public PSNoteProperty(string name, object value);
        public override bool IsGettable { get; }
        public override bool IsSettable { get; }
        public override PSMemberTypes MemberType { get; }
        public override string TypeNameOfValue { get; }
        public override object Value { get; set; }
        public override PSMemberInfo Copy();
        public override string ToString();
    }
    public class PSNotImplementedException : NotImplementedException, IContainsErrorRecord
    {
        public PSNotImplementedException();
        protected PSNotImplementedException(SerializationInfo info, StreamingContext context);
        public PSNotImplementedException(string message);
        public PSNotImplementedException(string message, Exception innerException);
        public ErrorRecord ErrorRecord { get; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
    }
    public class PSNotSupportedException : NotSupportedException, IContainsErrorRecord
    {
        public PSNotSupportedException();
        protected PSNotSupportedException(SerializationInfo info, StreamingContext context);
        public PSNotSupportedException(string message);
        public PSNotSupportedException(string message, Exception innerException);
        public ErrorRecord ErrorRecord { get; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
    }
    public class PSObject : IComparable, IDynamicMetaObjectProvider, IFormattable, ISerializable
    {
        public const string AdaptedMemberSetName = "psadapted";
        public const string BaseObjectMemberSetName = "psbase";
        public const string ExtendedMemberSetName = "psextended";
        public PSObject();
        public PSObject(object obj);
        protected PSObject(SerializationInfo info, StreamingContext context);
        public object BaseObject { get; }
        public object ImmediateBaseObject { get; }
        public PSMemberInfoCollection<PSMemberInfo> Members { get; }
        public PSMemberInfoCollection<PSMethodInfo> Methods { get; }
        public PSMemberInfoCollection<PSPropertyInfo> Properties { get; }
        public Collection<string> TypeNames { get; }
        public static PSObject AsPSObject(object obj);
        public int CompareTo(object obj);
        public virtual PSObject Copy();
        public override bool Equals(object obj);
        public override int GetHashCode();
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context);
        public static implicit operator PSObject (bool valueToConvert);
        public static implicit operator PSObject (Hashtable valueToConvert);
        public static implicit operator PSObject (double valueToConvert);
        public static implicit operator PSObject (int valueToConvert);
        public static implicit operator PSObject (string valueToConvert);
        DynamicMetaObject System.Dynamic.IDynamicMetaObjectProvider.GetMetaObject(Expression parameter);
        public override string ToString();
        public string ToString(string format, IFormatProvider formatProvider);
    }
    public class PSObjectDisposedException : ObjectDisposedException, IContainsErrorRecord
    {
        protected PSObjectDisposedException(SerializationInfo info, StreamingContext context);
        public PSObjectDisposedException(string objectName);
        public PSObjectDisposedException(string message, Exception innerException);
        public PSObjectDisposedException(string objectName, string message);
        public ErrorRecord ErrorRecord { get; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
    }
    public class PSObjectPropertyDescriptor : PropertyDescriptor
    {
        public override AttributeCollection Attributes { get; }
        public override Type ComponentType { get; }
        public override bool IsReadOnly { get; }
        public override Type PropertyType { get; }
        public override bool CanResetValue(object component);
        public override object GetValue(object component);
        public override void ResetValue(object component);
        public override void SetValue(object component, object value);
        public override bool ShouldSerializeValue(object component);
    }
    public class PSObjectTypeDescriptionProvider : TypeDescriptionProvider
    {
        public PSObjectTypeDescriptionProvider();
        public event EventHandler<GettingValueExceptionEventArgs> GettingValueException;
        public event EventHandler<SettingValueExceptionEventArgs> SettingValueException;
        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance);
    }
    public class PSObjectTypeDescriptor : CustomTypeDescriptor
    {
        public PSObjectTypeDescriptor(PSObject instance);
        public PSObject Instance { get; }
        public event EventHandler<GettingValueExceptionEventArgs> GettingValueException;
        public event EventHandler<SettingValueExceptionEventArgs> SettingValueException;
        public override bool Equals(object obj);
        public override AttributeCollection GetAttributes();
        public override string GetClassName();
        public override string GetComponentName();
        public override TypeConverter GetConverter();
        public override EventDescriptor GetDefaultEvent();
        public override PropertyDescriptor GetDefaultProperty();
        public override object GetEditor(Type editorBaseType);
        public override EventDescriptorCollection GetEvents();
        public override EventDescriptorCollection GetEvents(Attribute[] attributes);
        public override int GetHashCode();
        public override PropertyDescriptorCollection GetProperties();
        public override PropertyDescriptorCollection GetProperties(Attribute[] attributes);
        public override object GetPropertyOwner(PropertyDescriptor pd);
    }
    public class PSParameterizedProperty : PSMethodInfo
    {
        public bool IsGettable { get; }
        public bool IsSettable { get; }
        public override PSMemberTypes MemberType { get; }
        public override Collection<string> OverloadDefinitions { get; }
        public override string TypeNameOfValue { get; }
        public override PSMemberInfo Copy();
        public override object Invoke(params object[] arguments);
        public void InvokeSet(object valueToSet, params object[] arguments);
        public override string ToString();
    }
    public sealed class PSParseError
    {
        public string Message { get; }
        public PSToken Token { get; }
    }
    public sealed class PSParser
    {
        public static Collection<PSToken> Tokenize(object[] script, out Collection<PSParseError> errors);
        public static Collection<PSToken> Tokenize(string script, out Collection<PSParseError> errors);
    }
    public sealed class PSPrimitiveDictionary : Hashtable
    {
        public PSPrimitiveDictionary();
        public PSPrimitiveDictionary(Hashtable other);
        public override object this[object key] { get; set; }
        public object this[string key] { get; set; }
        public override void Add(object key, object value);
        public void Add(string key, bool value);
        public void Add(string key, bool[] value);
        public void Add(string key, byte value);
        public void Add(string key, byte[] value);
        public void Add(string key, char value);
        public void Add(string key, char[] value);
        public void Add(string key, DateTime value);
        public void Add(string key, DateTime[] value);
        public void Add(string key, decimal value);
        public void Add(string key, decimal[] value);
        public void Add(string key, double value);
        public void Add(string key, double[] value);
        public void Add(string key, Guid value);
        public void Add(string key, Guid[] value);
        public void Add(string key, int value);
        public void Add(string key, int[] value);
        public void Add(string key, long value);
        public void Add(string key, long[] value);
        public void Add(string key, PSPrimitiveDictionary value);
        public void Add(string key, PSPrimitiveDictionary[] value);
        public void Add(string key, sbyte value);
        public void Add(string key, sbyte[] value);
        public void Add(string key, float value);
        public void Add(string key, float[] value);
        public void Add(string key, string value);
        public void Add(string key, string[] value);
        public void Add(string key, TimeSpan value);
        public void Add(string key, TimeSpan[] value);
        public void Add(string key, ushort value);
        public void Add(string key, ushort[] value);
        public void Add(string key, uint value);
        public void Add(string key, uint[] value);
        public void Add(string key, ulong value);
        public void Add(string key, ulong[] value);
        public void Add(string key, Uri value);
        public void Add(string key, Uri[] value);
        public void Add(string key, Version value);
        public void Add(string key, Version[] value);
        public override object Clone();
    }
    public class PSProperty : PSPropertyInfo
    {
        public override bool IsGettable { get; }
        public override bool IsSettable { get; }
        public override PSMemberTypes MemberType { get; }
        public override string TypeNameOfValue { get; }
        public override object Value { get; set; }
        public override PSMemberInfo Copy();
        public override string ToString();
    }
    public abstract class PSPropertyAdapter
    {
        protected PSPropertyAdapter();
        public abstract Collection<PSAdaptedProperty> GetProperties(object baseObject);
        public abstract PSAdaptedProperty GetProperty(object baseObject, string propertyName);
        public abstract string GetPropertyTypeName(PSAdaptedProperty adaptedProperty);
        public abstract object GetPropertyValue(PSAdaptedProperty adaptedProperty);
        public virtual Collection<string> GetTypeNameHierarchy(object baseObject);
        public abstract bool IsGettable(PSAdaptedProperty adaptedProperty);
        public abstract bool IsSettable(PSAdaptedProperty adaptedProperty);
        public abstract void SetPropertyValue(PSAdaptedProperty adaptedProperty, object value);
    }
    public abstract class PSPropertyInfo : PSMemberInfo
    {
        protected PSPropertyInfo();
        public abstract bool IsGettable { get; }
        public abstract bool IsSettable { get; }
    }
    public class PSPropertySet : PSMemberInfo
    {
        public PSPropertySet(string name, IEnumerable<string> referencedPropertyNames);
        public override PSMemberTypes MemberType { get; }
        public Collection<string> ReferencedPropertyNames { get; }
        public override string TypeNameOfValue { get; }
        public override object Value { get; set; }
        public override PSMemberInfo Copy();
        public override string ToString();
    }
    public class PSReference
    {
        public PSReference(object value);
        public object Value { get; set; }
    }
    public class PSScriptMethod : PSMethodInfo
    {
        public PSScriptMethod(string name, ScriptBlock script);
        public override PSMemberTypes MemberType { get; }
        public override Collection<string> OverloadDefinitions { get; }
        public ScriptBlock Script { get; }
        public override string TypeNameOfValue { get; }
        public override PSMemberInfo Copy();
        public override object Invoke(params object[] arguments);
        public override string ToString();
    }
    public class PSScriptProperty : PSPropertyInfo
    {
        public PSScriptProperty(string name, ScriptBlock getterScript);
        public PSScriptProperty(string name, ScriptBlock getterScript, ScriptBlock setterScript);
        public ScriptBlock GetterScript { get; }
        public override bool IsGettable { get; }
        public override bool IsSettable { get; }
        public override PSMemberTypes MemberType { get; }
        public ScriptBlock SetterScript { get; }
        public override string TypeNameOfValue { get; }
        public override object Value { get; set; }
        public override PSMemberInfo Copy();
        public override string ToString();
    }
    public class PSSecurityException : RuntimeException
    {
        public PSSecurityException();
        protected PSSecurityException(SerializationInfo info, StreamingContext context);
        public PSSecurityException(string message);
        public PSSecurityException(string message, Exception innerException);
        public override ErrorRecord ErrorRecord { get; }
        public override string Message { get; }
    }
    public class PSSerializer
    {
        public static object Deserialize(string source);
        public static object[] DeserializeAsList(string source);
        public static string Serialize(object source);
        public static string Serialize(object source, int depth);
    }
    public abstract class PSSessionTypeOption
    {
        protected PSSessionTypeOption();
        protected internal virtual PSSessionTypeOption ConstructObjectFromPrivateData(string privateData);
        protected internal virtual string ConstructPrivateData();
        protected internal virtual void CopyUpdatedValuesFrom(PSSessionTypeOption updated);
    }
    public class PSSnapInInfo
    {
        public string ApplicationBase { get; }
        public string AssemblyName { get; }
        public string Description { get; }
        public Collection<string> Formats { get; }
        public bool IsDefault { get; }
        public bool LogPipelineExecutionDetails { get; set; }
        public string ModuleName { get; }
        public string Name { get; }
        public Version PSVersion { get; }
        public Collection<string> Types { get; }
        public string Vendor { get; }
        public Version Version { get; }
        public override string ToString();
    }
    public class PSSnapInSpecification
    {
        public string Name { get; }
        public Version Version { get; }
    }
    public sealed class PSToken
    {
        public string Content { get; }
        public int EndColumn { get; }
        public int EndLine { get; }
        public int Length { get; }
        public int Start { get; }
        public int StartColumn { get; }
        public int StartLine { get; }
        public PSTokenType Type { get; }
        public static PSTokenType GetPSTokenType(Token token);
    }
    public enum PSTokenType
    {
        Attribute = 9,
        Command = 1,
        CommandArgument = 3,
        CommandParameter = 2,
        Comment = 15,
        GroupEnd = 13,
        GroupStart = 12,
        Keyword = 14,
        LineContinuation = 18,
        LoopLabel = 8,
        Member = 7,
        NewLine = 17,
        Number = 4,
        Operator = 11,
        Position = 19,
        StatementSeparator = 16,
        String = 5,
        Type = 10,
        Unknown = 0,
        Variable = 6,
    }
    public class PSTraceSource
    {
        public string Description { get; set; }
        public TraceListenerCollection Listeners { get; }
        public string Name { get; }
        public PSTraceSourceOptions Options { get; set; }
        public SourceSwitch Switch { get; set; }
    }
    public enum PSTraceSourceOptions
    {
        All = 32767,
        Assert = 16384,
        Constructor = 1,
        Data = 6167,
        Delegates = 32,
        Dispose = 2,
        Error = 512,
        Errors = 640,
        Events = 64,
        Exception = 128,
        ExecutionFlow = 8303,
        Finalizer = 4,
        Lock = 256,
        Method = 8,
        None = 0,
        Property = 16,
        Scope = 8192,
        Verbose = 2048,
        Warning = 1024,
        WriteLine = 4096,
    }
    public sealed class PSTransactionContext : IDisposable
    {
        public void Dispose();
    }
    public abstract class PSTransportOption : ICloneable
    {
        protected PSTransportOption();
        public object Clone();
        protected internal virtual void LoadFromDefaults(PSSessionType sessionType, bool keepAssigned);
    }
    public abstract class PSTypeConverter
    {
        protected PSTypeConverter();
        public virtual bool CanConvertFrom(PSObject sourceValue, Type destinationType);
        public abstract bool CanConvertFrom(object sourceValue, Type destinationType);
        public virtual bool CanConvertTo(PSObject sourceValue, Type destinationType);
        public abstract bool CanConvertTo(object sourceValue, Type destinationType);
        public virtual object ConvertFrom(PSObject sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase);
        public abstract object ConvertFrom(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase);
        public virtual object ConvertTo(PSObject sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase);
        public abstract object ConvertTo(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase);
    }
    public class PSTypeName
    {
        public PSTypeName(ITypeName typeName);
        public PSTypeName(TypeDefinitionAst typeDefinitionAst);
        public PSTypeName(string name);
        public PSTypeName(Type type);
        public string Name { get; }
        public Type Type { get; }
        public TypeDefinitionAst TypeDefinitionAst { get; }
        public override string ToString();
    }
    public class PSTypeNameAttribute : Attribute
    {
        public PSTypeNameAttribute(string psTypeName);
        public string PSTypeName { get; }
    }
    public class PSVariable
    {
        public PSVariable(string name);
        public PSVariable(string name, object value);
        public PSVariable(string name, object value, ScopedItemOptions options);
        public PSVariable(string name, object value, ScopedItemOptions options, Collection<Attribute> attributes);
        public Collection<Attribute> Attributes { get; }
        public virtual string Description { get; set; }
        public PSModuleInfo Module { get; }
        public string ModuleName { get; }
        public string Name { get; }
        public virtual ScopedItemOptions Options { get; set; }
        public virtual object Value { get; set; }
        public SessionStateEntryVisibility Visibility { get; set; }
        public virtual bool IsValidValue(object value);
    }
    public sealed class PSVariableIntrinsics
    {
        public PSVariable Get(string name);
        public object GetValue(string name);
        public object GetValue(string name, object defaultValue);
        public void Remove(PSVariable variable);
        public void Remove(string name);
        public void Set(PSVariable variable);
        public void Set(string name, object value);
    }
    public class PSVariableProperty : PSNoteProperty
    {
        public PSVariableProperty(PSVariable variable);
        public override bool IsGettable { get; }
        public override bool IsSettable { get; }
        public override PSMemberTypes MemberType { get; }
        public override string TypeNameOfValue { get; }
        public override object Value { get; set; }
        public override PSMemberInfo Copy();
        public override string ToString();
    }
    public sealed class PSVersionHashTable : Hashtable, IEnumerable
    {
        public override ICollection Keys { get; }
        IEnumerator System.Collections.IEnumerable.GetEnumerator();
    }
    public class ReadOnlyPSMemberInfoCollection<T> : IEnumerable, IEnumerable<T> where T : PSMemberInfo
    {
        public int Count { get; }
        public T this[int index] { get; }
        public T this[string name] { get; }
        public virtual IEnumerator<T> GetEnumerator();
        public ReadOnlyPSMemberInfoCollection<T> Match(string name);
        public ReadOnlyPSMemberInfoCollection<T> Match(string name, PSMemberTypes memberTypes);
        IEnumerator System.Collections.IEnumerable.GetEnumerator();
    }
    public class RedirectedException : RuntimeException
    {
        public RedirectedException();
        protected RedirectedException(SerializationInfo info, StreamingContext context);
        public RedirectedException(string message);
        public RedirectedException(string message, Exception innerException);
    }
    public class RegisterArgumentCompleterCommand : PSCmdlet
    {
        public RegisterArgumentCompleterCommand();
        public string[] CommandName { get; set; }
        public SwitchParameter Native { get; set; }
        public string ParameterName { get; set; }
        public ScriptBlock ScriptBlock { get; set; }
        protected override void EndProcessing();
    }
    public class RemoteCommandInfo : CommandInfo
    {
        public override string Definition { get; }
        public override ReadOnlyCollection<PSTypeName> OutputType { get; }
    }
    public class RemoteException : RuntimeException
    {
        public RemoteException();
        protected RemoteException(SerializationInfo info, StreamingContext context);
        public RemoteException(string message);
        public RemoteException(string message, Exception innerException);
        public override ErrorRecord ErrorRecord { get; }
        public PSObject SerializedRemoteException { get; }
        public PSObject SerializedRemoteInvocationInfo { get; }
    }
    public enum RemoteStreamOptions
    {
        AddInvocationInfo = 15,
        AddInvocationInfoToDebugRecord = 4,
        AddInvocationInfoToErrorRecord = 1,
        AddInvocationInfoToVerboseRecord = 8,
        AddInvocationInfoToWarningRecord = 2,
    }
    public enum RemotingBehavior
    {
        Custom = 2,
        None = 0,
        PowerShell = 1,
    }
    public enum RemotingCapability
    {
        None = 0,
        OwnedByCommand = 3,
        PowerShell = 1,
        SupportedByCommand = 2,
    }
    public abstract class Repository<T> where T : class
    {
        protected Repository(string identifier);
        public void Add(T item);
        public T GetItem(Guid instanceId);
        public List<T> GetItems();
        protected abstract Guid GetKey(T item);
        public void Remove(T item);
    }
    public enum ResolutionPurpose
    {
        Decryption = 1,
        Encryption = 0,
    }
    public enum ReturnContainers
    {
        ReturnAllContainers = 1,
        ReturnMatchingContainers = 0,
    }
    public enum RollbackSeverity
    {
        Error = 0,
        Never = 2,
        TerminatingError = 1,
    }
    public enum RunspaceMode
    {
        CurrentRunspace = 0,
        NewRunspace = 1,
    }
    public sealed class RunspacePoolStateInfo
    {
        public RunspacePoolStateInfo(RunspacePoolState state, Exception reason);
        public Exception Reason { get; }
        public RunspacePoolState State { get; }
    }
    public class RunspaceRepository : Repository<PSSession>
    {
        public List<PSSession> Runspaces { get; }
        protected override Guid GetKey(PSSession item);
    }
    public class RuntimeDefinedParameter
    {
        public RuntimeDefinedParameter();
        public RuntimeDefinedParameter(string name, Type parameterType, Collection<Attribute> attributes);
        public Collection<Attribute> Attributes { get; }
        public bool IsSet { get; set; }
        public string Name { get; set; }
        public Type ParameterType { get; set; }
        public object Value { get; set; }
    }
    public class RuntimeDefinedParameterDictionary : Dictionary<string, RuntimeDefinedParameter>
    {
        public RuntimeDefinedParameterDictionary();
        public object Data { get; set; }
        public string HelpFile { get; set; }
    }
    public class RuntimeException : SystemException, IContainsErrorRecord
    {
        public RuntimeException();
        protected RuntimeException(SerializationInfo info, StreamingContext context);
        public RuntimeException(string message);
        public RuntimeException(string message, Exception innerException);
        public RuntimeException(string message, Exception innerException, ErrorRecord errorRecord);
        public virtual ErrorRecord ErrorRecord { get; }
        public bool WasThrownFromThrowStatement { get; set; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
    }
    public enum ScopedItemOptions
    {
        AllScope = 8,
        Constant = 2,
        None = 0,
        Private = 4,
        ReadOnly = 1,
        Unspecified = 16,
    }
    public class ScriptBlock : ISerializable
    {
        protected ScriptBlock(SerializationInfo info, StreamingContext context);
        public Ast Ast { get; }
        public List<Attribute> Attributes { get; }
        public bool DebuggerHidden { get; set; }
        public string File { get; }
        public Guid Id { get; }
        public bool IsConfiguration { get; set; }
        public bool IsFilter { get; set; }
        public PSModuleInfo Module { get; }
        public PSToken StartPosition { get; }
        public void CheckRestrictedLanguage(IEnumerable<string> allowedCommands, IEnumerable<string> allowedVariables, bool allowEnvironmentVariables);
        public static ScriptBlock Create(string script);
        public ScriptBlock GetNewClosure();
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context);
        public PowerShell GetPowerShell(bool isTrustedInput, params object[] args);
        public PowerShell GetPowerShell(Dictionary<string, object> variables, out Dictionary<string, object> usingVariables, bool isTrustedInput, params object[] args);
        public PowerShell GetPowerShell(Dictionary<string, object> variables, out Dictionary<string, object> usingVariables, params object[] args);
        public PowerShell GetPowerShell(Dictionary<string, object> variables, params object[] args);
        public PowerShell GetPowerShell(params object[] args);
        public SteppablePipeline GetSteppablePipeline();
        public SteppablePipeline GetSteppablePipeline(CommandOrigin commandOrigin);
        public SteppablePipeline GetSteppablePipeline(CommandOrigin commandOrigin, object[] args);
        public Collection<PSObject> Invoke(params object[] args);
        public object InvokeReturnAsIs(params object[] args);
        public Collection<PSObject> InvokeWithContext(Dictionary<string, ScriptBlock> functionsToDefine, List<PSVariable> variablesToDefine, params object[] args);
        public Collection<PSObject> InvokeWithContext(IDictionary functionsToDefine, List<PSVariable> variablesToDefine, params object[] args);
        public override string ToString();
    }
    public class ScriptBlockToPowerShellNotSupportedException : RuntimeException
    {
        public ScriptBlockToPowerShellNotSupportedException();
        protected ScriptBlockToPowerShellNotSupportedException(SerializationInfo info, StreamingContext context);
        public ScriptBlockToPowerShellNotSupportedException(string message);
        public ScriptBlockToPowerShellNotSupportedException(string message, Exception innerException);
    }
    public class ScriptCallDepthException : SystemException, IContainsErrorRecord
    {
        public ScriptCallDepthException();
        protected ScriptCallDepthException(SerializationInfo info, StreamingContext context);
        public ScriptCallDepthException(string message);
        public ScriptCallDepthException(string message, Exception innerException);
        public int CallDepth { get; }
        public ErrorRecord ErrorRecord { get; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
    }
    public class ScriptInfo : CommandInfo
    {
        public override string Definition { get; }
        public override ReadOnlyCollection<PSTypeName> OutputType { get; }
        public ScriptBlock ScriptBlock { get; }
        public override string ToString();
    }
    public class ScriptRequiresException : RuntimeException
    {
        public ScriptRequiresException();
        protected ScriptRequiresException(SerializationInfo info, StreamingContext context);
        public ScriptRequiresException(string message);
        public ScriptRequiresException(string message, Exception innerException);
        public string CommandName { get; }
        public ReadOnlyCollection<string> MissingPSSnapIns { get; }
        public Version RequiresPSVersion { get; }
        public string RequiresShellId { get; }
        public string RequiresShellPath { get; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
    }
    public sealed class SecurityDescriptorCmdletProviderIntrinsics
    {
        public Collection<PSObject> Get(string path, AccessControlSections includeSections);
        public ObjectSecurity NewFromPath(string path, AccessControlSections includeSections);
        public ObjectSecurity NewOfType(string providerId, string type, AccessControlSections includeSections);
        public Collection<PSObject> Set(string path, ObjectSecurity sd);
    }
    public sealed class SemanticVersion : IComparable, IComparable<SemanticVersion>, IEquatable<SemanticVersion>
    {
        public SemanticVersion(int major);
        public SemanticVersion(int major, int minor);
        public SemanticVersion(int major, int minor, int patch);
        public SemanticVersion(int major, int minor, int patch, string label);
        public SemanticVersion(string version);
        public SemanticVersion(Version version);
        public string Label { get; }
        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }
        public int CompareTo(SemanticVersion value);
        public int CompareTo(object version);
        public bool Equals(SemanticVersion other);
        public override bool Equals(object obj);
        public override int GetHashCode();
        public static bool operator ==(SemanticVersion v1, SemanticVersion v2);
        public static bool operator >(SemanticVersion v1, SemanticVersion v2);
        public static bool operator >=(SemanticVersion v1, SemanticVersion v2);
        public static implicit operator Version (SemanticVersion semver);
        public static bool operator !=(SemanticVersion v1, SemanticVersion v2);
        public static bool operator <(SemanticVersion v1, SemanticVersion v2);
        public static bool operator <=(SemanticVersion v1, SemanticVersion v2);
        public static SemanticVersion Parse(string version);
        public override string ToString();
        public static bool TryParse(string version, out SemanticVersion result);
    }
    public enum SessionCapabilities
    {
        Language = 4,
        RemoteServer = 1,
        WorkflowServer = 2,
    }
    public sealed class SessionState
    {
        public SessionState();
        public List<string> Applications { get; }
        public DriveManagementIntrinsics Drive { get; }
        public CommandInvocationIntrinsics InvokeCommand { get; }
        public ProviderIntrinsics InvokeProvider { get; }
        public PSLanguageMode LanguageMode { get; set; }
        public PSModuleInfo Module { get; }
        public PathIntrinsics Path { get; }
        public CmdletProviderManagementIntrinsics Provider { get; }
        public PSVariableIntrinsics PSVariable { get; }
        public List<string> Scripts { get; }
        public bool UseFullLanguageModeInDebugger { get; }
        public static bool IsVisible(CommandOrigin origin, CommandInfo commandInfo);
        public static bool IsVisible(CommandOrigin origin, PSVariable variable);
        public static bool IsVisible(CommandOrigin origin, object valueToCheck);
        public static void ThrowIfNotVisible(CommandOrigin origin, object valueToCheck);
    }
    public enum SessionStateCategory
    {
        Alias = 1,
        Cmdlet = 9,
        CmdletProvider = 5,
        Command = 7,
        Drive = 4,
        Filter = 3,
        Function = 2,
        Resource = 8,
        Scope = 6,
        Variable = 0,
    }
    public enum SessionStateEntryVisibility
    {
        Private = 1,
        Public = 0,
    }
    public class SessionStateException : RuntimeException
    {
        public SessionStateException();
        protected SessionStateException(SerializationInfo info, StreamingContext context);
        public SessionStateException(string message);
        public SessionStateException(string message, Exception innerException);
        public override ErrorRecord ErrorRecord { get; }
        public string ItemName { get; }
        public SessionStateCategory SessionStateCategory { get; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
    }
    public class SessionStateUnauthorizedAccessException : SessionStateException
    {
        public SessionStateUnauthorizedAccessException();
        protected SessionStateUnauthorizedAccessException(SerializationInfo info, StreamingContext context);
        public SessionStateUnauthorizedAccessException(string message);
        public SessionStateUnauthorizedAccessException(string message, Exception innerException);
    }
    public class SettingValueExceptionEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public bool ShouldThrow { get; set; }
    }
    public class SetValueException : ExtendedTypeSystemException
    {
        public SetValueException();
        protected SetValueException(SerializationInfo info, StreamingContext context);
        public SetValueException(string message);
        public SetValueException(string message, Exception innerException);
    }
    public class SetValueInvocationException : SetValueException
    {
        public SetValueInvocationException();
        protected SetValueInvocationException(SerializationInfo info, StreamingContext context);
        public SetValueInvocationException(string message);
        public SetValueInvocationException(string message, Exception innerException);
    }
    public enum ShouldProcessReason
    {
        None = 0,
        WhatIf = 1,
    }
    public sealed class Signature
    {
        public bool IsOSBinary { get; }
        public string Path { get; }
        public SignatureType SignatureType { get; }
        public X509Certificate2 SignerCertificate { get; }
        public SignatureStatus Status { get; }
        public string StatusMessage { get; }
        public X509Certificate2 TimeStamperCertificate { get; }
    }
    public enum SignatureStatus
    {
        HashMismatch = 3,
        Incompatible = 6,
        NotSigned = 2,
        NotSupportedFileFormat = 5,
        NotTrusted = 4,
        UnknownError = 1,
        Valid = 0,
    }
    public enum SignatureType
    {
        Authenticode = 1,
        Catalog = 2,
        None = 0,
    }
    public enum SigningOption
    {
        AddFullCertificateChain = 1,
        AddFullCertificateChainExceptRoot = 2,
        AddOnlyCertificate = 0,
        Default = 2,
    }
    public enum SplitOptions
    {
        CultureInvariant = 4,
        ExplicitCapture = 128,
        IgnoreCase = 64,
        IgnorePatternWhitespace = 8,
        Multiline = 16,
        RegexMatch = 2,
        SimpleMatch = 1,
        Singleline = 32,
    }
    public sealed class StartRunspaceDebugProcessingEventArgs : EventArgs
    {
        public StartRunspaceDebugProcessingEventArgs(Runspace runspace);
        public Runspace Runspace { get; }
        public bool UseDefaultProcessing { get; set; }
    }
    public sealed class SteppablePipeline : IDisposable
    {
        public void Begin(bool expectInput);
        public void Begin(bool expectInput, EngineIntrinsics contextToRedirectTo);
        public void Begin(InternalCommand command);
        public void Dispose();
        public Array End();
        ~SteppablePipeline();
        public Array Process();
        public Array Process(PSObject input);
        public Array Process(object input);
    }
    public sealed class SupportsWildcardsAttribute : ParsingBaseAttribute
    {
        public SupportsWildcardsAttribute();
    }
    public struct SwitchParameter
    {
        public SwitchParameter(bool isPresent);
        public bool IsPresent { get; }
        public static SwitchParameter Present { get; }
        public override bool Equals(object obj);
        public override int GetHashCode();
        public static bool operator ==(bool first, SwitchParameter second);
        public static bool operator ==(SwitchParameter first, bool second);
        public static bool operator ==(SwitchParameter first, SwitchParameter second);
        public static implicit operator SwitchParameter (bool value);
        public static implicit operator bool (SwitchParameter switchParameter);
        public static bool operator !=(bool first, SwitchParameter second);
        public static bool operator !=(SwitchParameter first, bool second);
        public static bool operator !=(SwitchParameter first, SwitchParameter second);
        public bool ToBool();
        public override string ToString();
    }
    public sealed class TableControl : PSControl
    {
        public TableControl();
        public TableControl(TableControlRow tableControlRow);
        public TableControl(TableControlRow tableControlRow, IEnumerable<TableControlColumnHeader> tableControlColumnHeaders);
        public bool AutoSize { get; set; }
        public List<TableControlColumnHeader> Headers { get; set; }
        public bool HideTableHeaders { get; set; }
        public List<TableControlRow> Rows { get; set; }
        public static TableControlBuilder Create(bool outOfBand=false, bool autoSize=false, bool hideTableHeaders=false);
    }
    public sealed class TableControlBuilder
    {
        public TableControlBuilder AddHeader(Alignment alignment=(Alignment)(0), int width=0, string label=null);
        public TableControl EndTable();
        public TableControlBuilder GroupByProperty(string property, CustomControl customControl=null, string label=null);
        public TableControlBuilder GroupByScriptBlock(string scriptBlock, CustomControl customControl=null, string label=null);
        public TableRowDefinitionBuilder StartRowDefinition(bool wrap=false, IEnumerable<string> entrySelectedByType=null, IEnumerable<DisplayEntry> entrySelectedByCondition=null);
    }
    public sealed class TableControlColumn
    {
        public TableControlColumn();
        public TableControlColumn(Alignment alignment, DisplayEntry entry);
        public Alignment Alignment { get; set; }
        public DisplayEntry DisplayEntry { get; set; }
        public string FormatString { get; }
        public override string ToString();
    }
    public sealed class TableControlColumnHeader
    {
        public TableControlColumnHeader();
        public TableControlColumnHeader(string label, int width, Alignment alignment);
        public Alignment Alignment { get; set; }
        public string Label { get; set; }
        public int Width { get; set; }
    }
    public sealed class TableControlRow
    {
        public TableControlRow();
        public TableControlRow(IEnumerable<TableControlColumn> columns);
        public List<TableControlColumn> Columns { get; set; }
        public EntrySelectedBy SelectedBy { get; }
        public bool Wrap { get; set; }
    }
    public sealed class TableRowDefinitionBuilder
    {
        public TableRowDefinitionBuilder AddPropertyColumn(string propertyName, Alignment alignment=(Alignment)(0), string format=null);
        public TableRowDefinitionBuilder AddScriptBlockColumn(string scriptBlock, Alignment alignment=(Alignment)(0), string format=null);
        public TableControlBuilder EndRowDefinition();
    }
    public sealed class TerminateException : FlowControlException
    {
        public TerminateException();
    }
    public abstract class ValidateArgumentsAttribute : CmdletMetadataAttribute
    {
        protected ValidateArgumentsAttribute();
        protected abstract void Validate(object arguments, EngineIntrinsics engineIntrinsics);
    }
    public sealed class ValidateCountAttribute : ValidateArgumentsAttribute
    {
        public ValidateCountAttribute(int minLength, int maxLength);
        public int MaxLength { get; }
        public int MinLength { get; }
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics);
    }
    public class ValidateDriveAttribute : ValidateArgumentsAttribute
    {
        public ValidateDriveAttribute(params string[] validRootDrives);
        public IList<string> ValidRootDrives { get; }
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics);
    }
    public abstract class ValidateEnumeratedArgumentsAttribute : ValidateArgumentsAttribute
    {
        protected ValidateEnumeratedArgumentsAttribute();
        protected sealed override void Validate(object arguments, EngineIntrinsics engineIntrinsics);
        protected abstract void ValidateElement(object element);
    }
    public sealed class ValidateLengthAttribute : ValidateEnumeratedArgumentsAttribute
    {
        public ValidateLengthAttribute(int minLength, int maxLength);
        public int MaxLength { get; }
        public int MinLength { get; }
        protected override void ValidateElement(object element);
    }
    public sealed class ValidateNotNullAttribute : ValidateArgumentsAttribute
    {
        public ValidateNotNullAttribute();
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics);
    }
    public sealed class ValidateNotNullOrEmptyAttribute : ValidateArgumentsAttribute
    {
        public ValidateNotNullOrEmptyAttribute();
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics);
    }
    public sealed class ValidatePatternAttribute : ValidateEnumeratedArgumentsAttribute
    {
        public ValidatePatternAttribute(string regexPattern);
        public string ErrorMessage { get; set; }
        public RegexOptions Options { get; set; }
        public string RegexPattern { get; }
        protected override void ValidateElement(object element);
    }
    public sealed class ValidateRangeAttribute : ValidateEnumeratedArgumentsAttribute
    {
        public ValidateRangeAttribute(object minRange, object maxRange);
        public object MaxRange { get; }
        public object MinRange { get; }
        protected override void ValidateElement(object element);
    }
    public sealed class ValidateScriptAttribute : ValidateEnumeratedArgumentsAttribute
    {
        public ValidateScriptAttribute(ScriptBlock scriptBlock);
        public string ErrorMessage { get; set; }
        public ScriptBlock ScriptBlock { get; }
        protected override void ValidateElement(object element);
    }
    public sealed class ValidateSetAttribute : ValidateEnumeratedArgumentsAttribute
    {
        public ValidateSetAttribute(params string[] validValues);
        public string ErrorMessage { get; set; }
        public bool IgnoreCase { get; set; }
        public IList<string> ValidValues { get; }
        protected override void ValidateElement(object element);
    }
    public sealed class ValidateUserDriveAttribute : ValidateDriveAttribute
    {
        public ValidateUserDriveAttribute();
    }
    public class ValidationMetadataException : MetadataException
    {
        public ValidationMetadataException();
        protected ValidationMetadataException(SerializationInfo info, StreamingContext context);
        public ValidationMetadataException(string message);
        public ValidationMetadataException(string message, Exception innerException);
    }
    public enum VariableAccessMode
    {
        Read = 0,
        ReadWrite = 2,
        Write = 1,
    }
    public class VariableBreakpoint : Breakpoint
    {
        public VariableAccessMode AccessMode { get; }
        public string Variable { get; }
        public override string ToString();
    }
    public class VariablePath
    {
        public VariablePath(string path);
        public string DriveName { get; }
        public bool IsDriveQualified { get; }
        public bool IsGlobal { get; }
        public bool IsLocal { get; }
        public bool IsPrivate { get; }
        public bool IsScript { get; }
        public bool IsUnqualified { get; }
        public bool IsUnscopedVariable { get; }
        public bool IsVariable { get; }
        public string UserPath { get; }
        public override string ToString();
    }
    public class VerbInfo
    {
        public VerbInfo();
        public string Group { get; set; }
        public string Verb { get; set; }
    }
    public class VerboseRecord : InformationalRecord
    {
        public VerboseRecord(PSObject record);
        public VerboseRecord(string message);
    }
    public static class VerbsCommon
    {
        public const string Add = "Add";
        public const string Clear = "Clear";
        public const string Close = "Close";
        public const string Copy = "Copy";
        public const string Enter = "Enter";
        public const string Exit = "Exit";
        public const string Find = "Find";
        public const string Format = "Format";
        public const string Get = "Get";
        public const string Hide = "Hide";
        public const string Join = "Join";
        public const string Lock = "Lock";
        public const string Move = "Move";
        public const string New = "New";
        public const string Open = "Open";
        public const string Optimize = "Optimize";
        public const string Pop = "Pop";
        public const string Push = "Push";
        public const string Redo = "Redo";
        public const string Remove = "Remove";
        public const string Rename = "Rename";
        public const string Reset = "Reset";
        public const string Resize = "Resize";
        public const string Search = "Search";
        public const string Select = "Select";
        public const string Set = "Set";
        public const string Show = "Show";
        public const string Skip = "Skip";
        public const string Split = "Split";
        public const string Step = "Step";
        public const string Switch = "Switch";
        public const string Undo = "Undo";
        public const string Unlock = "Unlock";
        public const string Watch = "Watch";
    }
    public static class VerbsCommunications
    {
        public const string Connect = "Connect";
        public const string Disconnect = "Disconnect";
        public const string Read = "Read";
        public const string Receive = "Receive";
        public const string Send = "Send";
        public const string Write = "Write";
    }
    public static class VerbsData
    {
        public const string Backup = "Backup";
        public const string Checkpoint = "Checkpoint";
        public const string Compare = "Compare";
        public const string Compress = "Compress";
        public const string Convert = "Convert";
        public const string ConvertFrom = "ConvertFrom";
        public const string ConvertTo = "ConvertTo";
        public const string Dismount = "Dismount";
        public const string Edit = "Edit";
        public const string Expand = "Expand";
        public const string Export = "Export";
        public const string Group = "Group";
        public const string Import = "Import";
        public const string Initialize = "Initialize";
        public const string Limit = "Limit";
        public const string Merge = "Merge";
        public const string Mount = "Mount";
        public const string Out = "Out";
        public const string Publish = "Publish";
        public const string Restore = "Restore";
        public const string Save = "Save";
        public const string Sync = "Sync";
        public const string Unpublish = "Unpublish";
        public const string Update = "Update";
    }
    public static class VerbsDiagnostic
    {
        public const string Debug = "Debug";
        public const string Measure = "Measure";
        public const string Ping = "Ping";
        public const string Repair = "Repair";
        public const string Resolve = "Resolve";
        public const string Test = "Test";
        public const string Trace = "Trace";
    }
    public static class VerbsLifecycle
    {
        public const string Approve = "Approve";
        public const string Assert = "Assert";
        public const string Complete = "Complete";
        public const string Confirm = "Confirm";
        public const string Deny = "Deny";
        public const string Disable = "Disable";
        public const string Enable = "Enable";
        public const string Install = "Install";
        public const string Invoke = "Invoke";
        public const string Register = "Register";
        public const string Request = "Request";
        public const string Restart = "Restart";
        public const string Resume = "Resume";
        public const string Start = "Start";
        public const string Stop = "Stop";
        public const string Submit = "Submit";
        public const string Suspend = "Suspend";
        public const string Uninstall = "Uninstall";
        public const string Unregister = "Unregister";
        public const string Wait = "Wait";
    }
    public static class VerbsOther
    {
        public const string Use = "Use";
    }
    public static class VerbsSecurity
    {
        public const string Block = "Block";
        public const string Grant = "Grant";
        public const string Protect = "Protect";
        public const string Revoke = "Revoke";
        public const string Unblock = "Unblock";
        public const string Unprotect = "Unprotect";
    }
    public class WarningRecord : InformationalRecord
    {
        public WarningRecord(PSObject record);
        public WarningRecord(string message);
        public WarningRecord(string fullyQualifiedWarningId, PSObject record);
        public WarningRecord(string fullyQualifiedWarningId, string message);
        public string FullyQualifiedWarningId { get; }
    }
    public enum WhereOperatorSelectionMode
    {
        Default = 0,
        First = 1,
        Last = 2,
        SkipUntil = 3,
        Split = 5,
        Until = 4,
    }
    public sealed class WideControl : PSControl
    {
        public WideControl();
        public WideControl(IEnumerable<WideControlEntryItem> wideEntries);
        public WideControl(IEnumerable<WideControlEntryItem> wideEntries, uint columns);
        public WideControl(uint columns);
        public bool AutoSize { get; set; }
        public uint Columns { get; }
        public List<WideControlEntryItem> Entries { get; }
        public static WideControlBuilder Create(bool outOfBand=false, bool autoSize=false, uint columns=(uint)0);
    }
    public sealed class WideControlBuilder
    {
        public WideControlBuilder AddPropertyEntry(string propertyName, string format=null, IEnumerable<string> entrySelectedByType=null, IEnumerable<DisplayEntry> entrySelectedByCondition=null);
        public WideControlBuilder AddScriptBlockEntry(string scriptBlock, string format=null, IEnumerable<string> entrySelectedByType=null, IEnumerable<DisplayEntry> entrySelectedByCondition=null);
        public WideControl EndWideControl();
        public WideControlBuilder GroupByProperty(string property, CustomControl customControl=null, string label=null);
        public WideControlBuilder GroupByScriptBlock(string scriptBlock, CustomControl customControl=null, string label=null);
    }
    public sealed class WideControlEntryItem
    {
        public WideControlEntryItem(DisplayEntry entry);
        public WideControlEntryItem(DisplayEntry entry, IEnumerable<string> selectedBy);
        public DisplayEntry DisplayEntry { get; }
        public EntrySelectedBy EntrySelectedBy { get; }
        public string FormatString { get; }
        public List<string> SelectedBy { get; }
    }
    public enum WildcardOptions
    {
        Compiled = 1,
        CultureInvariant = 4,
        IgnoreCase = 2,
        None = 0,
    }
    public sealed class WildcardPattern
    {
        public WildcardPattern(string pattern);
        public WildcardPattern(string pattern, WildcardOptions options);
        public static bool ContainsWildcardCharacters(string pattern);
        public static string Escape(string pattern);
        public static WildcardPattern Get(string pattern, WildcardOptions options);
        public bool IsMatch(string input);
        public string ToWql();
        public static string Unescape(string pattern);
    }
    public class WildcardPatternException : RuntimeException
    {
        public WildcardPatternException();
        protected WildcardPatternException(SerializationInfo info, StreamingContext context);
        public WildcardPatternException(string message);
        public WildcardPatternException(string message, Exception innerException);
    }
    public class WorkflowInfo : FunctionInfo
    {
        public WorkflowInfo(string name, string definition, ScriptBlock workflow, string xamlDefinition, WorkflowInfo[] workflowsCalled);
        public WorkflowInfo(string name, string definition, ScriptBlock workflow, string xamlDefinition, WorkflowInfo[] workflowsCalled, PSModuleInfo module);
        public override string Definition { get; }
        public string NestedXamlDefinition { get; set; }
        public ReadOnlyCollection<WorkflowInfo> WorkflowsCalled { get; }
        public string XamlDefinition { get; }
        protected internal override void Update(FunctionInfo function, bool force, ScopedItemOptions options, string helpFile);
    }
}
namespace System.Management.Automation.Host
{
    public struct BufferCell
    {
        public BufferCell(char character, ConsoleColor foreground, ConsoleColor background, BufferCellType bufferCellType);
        public ConsoleColor BackgroundColor { get; set; }
        public BufferCellType BufferCellType { get; set; }
        public char Character { get; set; }
        public ConsoleColor ForegroundColor { get; set; }
        public override bool Equals(object obj);
        public override int GetHashCode();
        public static bool operator ==(BufferCell first, BufferCell second);
        public static bool operator !=(BufferCell first, BufferCell second);
        public override string ToString();
    }
    public enum BufferCellType
    {
        Complete = 0,
        Leading = 1,
        Trailing = 2,
    }
    public sealed class ChoiceDescription
    {
        public ChoiceDescription(string label);
        public ChoiceDescription(string label, string helpMessage);
        public string HelpMessage { get; set; }
        public string Label { get; }
    }
    public enum ControlKeyStates
    {
        CapsLockOn = 128,
        EnhancedKey = 256,
        LeftAltPressed = 2,
        LeftCtrlPressed = 8,
        NumLockOn = 32,
        RightAltPressed = 1,
        RightCtrlPressed = 4,
        ScrollLockOn = 64,
        ShiftPressed = 16,
    }
    public struct Coordinates
    {
        public Coordinates(int x, int y);
        public int X { get; set; }
        public int Y { get; set; }
        public override bool Equals(object obj);
        public override int GetHashCode();
        public static bool operator ==(Coordinates first, Coordinates second);
        public static bool operator !=(Coordinates first, Coordinates second);
        public override string ToString();
    }
    public class FieldDescription
    {
        public FieldDescription(string name);
        public Collection<Attribute> Attributes { get; }
        public PSObject DefaultValue { get; set; }
        public string HelpMessage { get; set; }
        public bool IsMandatory { get; set; }
        public string Label { get; set; }
        public string Name { get; }
        public string ParameterAssemblyFullName { get; }
        public string ParameterTypeFullName { get; }
        public string ParameterTypeName { get; }
        public void SetParameterType(Type parameterType);
    }
    public class HostException : RuntimeException
    {
        public HostException();
        protected HostException(SerializationInfo info, StreamingContext context);
        public HostException(string message);
        public HostException(string message, Exception innerException);
        public HostException(string message, Exception innerException, string errorId, ErrorCategory errorCategory);
    }
    public interface IHostSupportsInteractiveSession
    {
        bool IsRunspacePushed { get; }
        Runspace Runspace { get; }
        void PopRunspace();
        void PushRunspace(Runspace runspace);
    }
    public interface IHostUISupportsMultipleChoiceSelection
    {
        Collection<int> PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, IEnumerable<int> defaultChoices);
    }
    public struct KeyInfo
    {
        public KeyInfo(int virtualKeyCode, char ch, ControlKeyStates controlKeyState, bool keyDown);
        public char Character { get; set; }
        public ControlKeyStates ControlKeyState { get; set; }
        public bool KeyDown { get; set; }
        public int VirtualKeyCode { get; set; }
        public override bool Equals(object obj);
        public override int GetHashCode();
        public static bool operator ==(KeyInfo first, KeyInfo second);
        public static bool operator !=(KeyInfo first, KeyInfo second);
        public override string ToString();
    }
    public class PromptingException : HostException
    {
        public PromptingException();
        protected PromptingException(SerializationInfo info, StreamingContext context);
        public PromptingException(string message);
        public PromptingException(string message, Exception innerException);
        public PromptingException(string message, Exception innerException, string errorId, ErrorCategory errorCategory);
    }
    public abstract class PSHost
    {
        protected PSHost();
        public abstract CultureInfo CurrentCulture { get; }
        public abstract CultureInfo CurrentUICulture { get; }
        public virtual bool DebuggerEnabled { get; set; }
        public abstract Guid InstanceId { get; }
        public abstract string Name { get; }
        public virtual PSObject PrivateData { get; }
        public abstract PSHostUserInterface UI { get; }
        public abstract Version Version { get; }
        public abstract void EnterNestedPrompt();
        public abstract void ExitNestedPrompt();
        public abstract void NotifyBeginApplication();
        public abstract void NotifyEndApplication();
        public abstract void SetShouldExit(int exitCode);
    }
    public abstract class PSHostRawUserInterface
    {
        protected PSHostRawUserInterface();
        public abstract ConsoleColor BackgroundColor { get; set; }
        public abstract Size BufferSize { get; set; }
        public abstract Coordinates CursorPosition { get; set; }
        public abstract int CursorSize { get; set; }
        public abstract ConsoleColor ForegroundColor { get; set; }
        public abstract bool KeyAvailable { get; }
        public abstract Size MaxPhysicalWindowSize { get; }
        public abstract Size MaxWindowSize { get; }
        public abstract Coordinates WindowPosition { get; set; }
        public abstract Size WindowSize { get; set; }
        public abstract string WindowTitle { get; set; }
        public abstract void FlushInputBuffer();
        public abstract BufferCell[,] GetBufferContents(Rectangle rectangle);
        public virtual int LengthInBufferCells(char source);
        public virtual int LengthInBufferCells(string source);
        public virtual int LengthInBufferCells(string source, int offset);
        public BufferCell[,] NewBufferCellArray(int width, int height, BufferCell contents);
        public BufferCell[,] NewBufferCellArray(Size size, BufferCell contents);
        public BufferCell[,] NewBufferCellArray(string[] contents, ConsoleColor foregroundColor, ConsoleColor backgroundColor);
        public KeyInfo ReadKey();
        public abstract KeyInfo ReadKey(ReadKeyOptions options);
        public abstract void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill);
        public abstract void SetBufferContents(Coordinates origin, BufferCell[,] contents);
        public abstract void SetBufferContents(Rectangle rectangle, BufferCell fill);
    }
    public abstract class PSHostUserInterface
    {
        protected PSHostUserInterface();
        public abstract PSHostRawUserInterface RawUI { get; }
        public virtual bool SupportsVirtualTerminal { get; }
        public abstract Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions);
        public abstract int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice);
        public abstract PSCredential PromptForCredential(string caption, string message, string userName, string targetName);
        public abstract PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options);
        public abstract string ReadLine();
        public abstract SecureString ReadLineAsSecureString();
        public abstract void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value);
        public abstract void Write(string value);
        public abstract void WriteDebugLine(string message);
        public abstract void WriteErrorLine(string value);
        public virtual void WriteInformation(InformationRecord record);
        public virtual void WriteLine();
        public virtual void WriteLine(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value);
        public abstract void WriteLine(string value);
        public abstract void WriteProgress(long sourceId, ProgressRecord record);
        public abstract void WriteVerboseLine(string message);
        public abstract void WriteWarningLine(string message);
    }
    public enum ReadKeyOptions
    {
        AllowCtrlC = 1,
        IncludeKeyDown = 4,
        IncludeKeyUp = 8,
        NoEcho = 2,
    }
    public struct Rectangle
    {
        public Rectangle(int left, int top, int right, int bottom);
        public Rectangle(Coordinates upperLeft, Coordinates lowerRight);
        public int Bottom { get; set; }
        public int Left { get; set; }
        public int Right { get; set; }
        public int Top { get; set; }
        public override bool Equals(object obj);
        public override int GetHashCode();
        public static bool operator ==(Rectangle first, Rectangle second);
        public static bool operator !=(Rectangle first, Rectangle second);
        public override string ToString();
    }
    public struct Size
    {
        public Size(int width, int height);
        public int Height { get; set; }
        public int Width { get; set; }
        public override bool Equals(object obj);
        public override int GetHashCode();
        public static bool operator ==(Size first, Size second);
        public static bool operator !=(Size first, Size second);
        public override string ToString();
    }
}
namespace System.Management.Automation.Internal
{
    public static class AlternateDataStreamUtilities
    {
    }
    public class AlternateStreamData
    {
        public AlternateStreamData();
        public string FileName { get; set; }
        public long Length { get; set; }
        public string Stream { get; set; }
    }
    public static class AutomationNull
    {
        public static PSObject Value { get; }
    }
    public static class ClassOps
    {
        public static void CallBaseCtor(object target, ConstructorInfo ci, object[] args);
        public static object CallMethodNonVirtually(object target, MethodInfo mi, object[] args);
        public static void CallVoidMethodNonVirtually(object target, MethodInfo mi, object[] args);
        public static void ValidateSetProperty(Type type, string propertyName, object value);
    }
    public abstract class CmdletMetadataAttribute : Attribute
    {
    }
    public sealed class CommonParameters
    {
        public SwitchParameter Debug { get; set; }
        public ActionPreference ErrorAction { get; set; }
        public string ErrorVariable { get; set; }
        public ActionPreference InformationAction { get; set; }
        public string InformationVariable { get; set; }
        public int OutBuffer { get; set; }
        public string OutVariable { get; set; }
        public string PipelineVariable { get; set; }
        public SwitchParameter Verbose { get; set; }
        public ActionPreference WarningAction { get; set; }
        public string WarningVariable { get; set; }
    }
    public static class DebuggerUtils
    {
        public const string GetPSCallStackOverrideFunction = "function Get-PSCallStack\r\n        {\r\n            [CmdletBinding()]\r\n            param()\r\n\r\n            if ($PSWorkflowDebugger -ne $null)\r\n            {\r\n                foreach ($frame in $PSWorkflowDebugger.GetCallStack())\r\n                {\r\n                    Write-Output $frame\r\n                }\r\n            }\r\n\r\n            Set-StrictMode -Off\r\n        }";
        public const string RemoveVariableFunction = "function Remove-DebuggerVariable\r\n        {\r\n            [CmdletBinding()]\r\n            param(\r\n                [Parameter(Position=0)]\r\n                [string[]]\r\n                $Name\r\n            )\r\n\r\n            foreach ($item in $Name)\r\n            {\r\n                microsoft.powershell.utility\\remove-variable -name $item -scope global\r\n            }\r\n\r\n            Set-StrictMode -Off\r\n        }";
        public const string SetVariableFunction = "function Set-DebuggerVariable\r\n        {\r\n            [CmdletBinding()]\r\n            param(\r\n                [Parameter(Position=0)]\r\n                [HashTable]\r\n                $Variables\r\n            )\r\n\r\n            foreach($key in $Variables.Keys)\r\n            {\r\n                microsoft.powershell.utility\\set-variable -Name $key -Value $Variables[$key] -Scope global\r\n            }\r\n\r\n            Set-StrictMode -Off\r\n        }";
        public static void EndMonitoringRunspace(Debugger debugger, PSMonitorRunspaceInfo runspaceInfo);
        public static IEnumerable<string> GetWorkflowDebuggerFunctions();
        public static bool ShouldAddCommandToHistory(string command);
        public static void StartMonitoringRunspace(Debugger debugger, PSMonitorRunspaceInfo runspaceInfo);
    }
    public interface IAstToWorkflowConverter
    {
        WorkflowInfo CompileWorkflow(string name, string definition, InitialSessionState initialSessionState);
        List<WorkflowInfo> CompileWorkflows(ScriptBlockAst ast, PSModuleInfo definingModule);
        List<WorkflowInfo> CompileWorkflows(ScriptBlockAst ast, PSModuleInfo definingModule, InitialSessionState initialSessionState, out ParseException parsingErrors);
        List<WorkflowInfo> CompileWorkflows(ScriptBlockAst ast, PSModuleInfo definingModule, InitialSessionState initialSessionState, out ParseException parsingErrors, string rootWorkflowName);
        List<WorkflowInfo> CompileWorkflows(ScriptBlockAst ast, PSModuleInfo definingModule, InitialSessionState initialSessionState, Nullable<PSLanguageMode> languageMode, out ParseException parsingErrors);
        List<WorkflowInfo> CompileWorkflows(ScriptBlockAst ast, PSModuleInfo definingModule, string rootWorkflowName);
        List<ParseError> ValidateAst(FunctionDefinitionAst ast);
    }
    public abstract class InternalCommand
    {
        public CommandOrigin CommandOrigin { get; }
    }
    public static class InternalTestHooks
    {
        public static void SetTestHook(string property, bool value);
    }
    public abstract class ParsingBaseAttribute : CmdletMetadataAttribute
    {
    }
    public sealed class PSEmbeddedMonitorRunspaceInfo : PSMonitorRunspaceInfo
    {
        public PSEmbeddedMonitorRunspaceInfo(Runspace runspace, PSMonitorRunspaceType runspaceType, PowerShell command, Guid parentDebuggerId);
        public PowerShell Command { get; }
        public Guid ParentDebuggerId { get; }
    }
    public abstract class PSMonitorRunspaceInfo
    {
        protected PSMonitorRunspaceInfo(Runspace runspace, PSMonitorRunspaceType runspaceType);
        public Runspace Runspace { get; }
        public PSMonitorRunspaceType RunspaceType { get; }
    }
    public enum PSMonitorRunspaceType
    {
        InvokeCommand = 1,
        Standalone = 0,
        WorkflowInlineScript = 2,
    }
    public sealed class PSStandaloneMonitorRunspaceInfo : PSMonitorRunspaceInfo
    {
        public PSStandaloneMonitorRunspaceInfo(Runspace runspace);
    }
    public class ScriptBlockMemberMethodWrapper
    {
        public static readonly object[] _emptyArgumentArray;
        public void InvokeHelper(object instance, object sessionStateInternal, object[] args);
        public T InvokeHelperT<T>(object instance, object sessionStateInternal, object[] args);
    }
    public static class SecuritySupport
    {
        public static bool IsProductBinary(string file);
    }
    public class SessionStateKeeper
    {
        public object GetSessionState();
    }
    public sealed class ShouldProcessParameters
    {
        public SwitchParameter Confirm { get; set; }
        public SwitchParameter WhatIf { get; set; }
    }
    public sealed class TransactionParameters
    {
        public SwitchParameter UseTransaction { get; set; }
    }
}
namespace System.Management.Automation.Language
{
    public class ArrayExpressionAst : ExpressionAst
    {
        public ArrayExpressionAst(IScriptExtent extent, StatementBlockAst statementBlock);
        public override Type StaticType { get; }
        public StatementBlockAst SubExpression { get; }
        public override Ast Copy();
    }
    public class ArrayLiteralAst : ExpressionAst
    {
        public ArrayLiteralAst(IScriptExtent extent, IList<ExpressionAst> elements);
        public ReadOnlyCollection<ExpressionAst> Elements { get; }
        public override Type StaticType { get; }
        public override Ast Copy();
    }
    public sealed class ArrayTypeName : ITypeName
    {
        public ArrayTypeName(IScriptExtent extent, ITypeName elementType, int rank);
        public string AssemblyName { get; }
        public ITypeName ElementType { get; }
        public IScriptExtent Extent { get; }
        public string FullName { get; }
        public bool IsArray { get; }
        public bool IsGeneric { get; }
        public string Name { get; }
        public int Rank { get; }
        public override bool Equals(object obj);
        public override int GetHashCode();
        public Type GetReflectionAttributeType();
        public Type GetReflectionType();
        public override string ToString();
    }
    public class AssignmentStatementAst : PipelineBaseAst
    {
        public AssignmentStatementAst(IScriptExtent extent, ExpressionAst left, TokenKind @operator, StatementAst right, IScriptExtent errorPosition);
        public IScriptExtent ErrorPosition { get; }
        public ExpressionAst Left { get; }
        public TokenKind Operator { get; }
        public StatementAst Right { get; }
        public override Ast Copy();
        public IEnumerable<ExpressionAst> GetAssignmentTargets();
    }
    public abstract class Ast
    {
        protected Ast(IScriptExtent extent);
        public IScriptExtent Extent { get; }
        public Ast Parent { get; }
        public abstract Ast Copy();
        public Ast Find(Func<Ast, bool> predicate, bool searchNestedScriptBlocks);
        public IEnumerable<Ast> FindAll(Func<Ast, bool> predicate, bool searchNestedScriptBlocks);
        public object SafeGetValue();
        public override string ToString();
        public void Visit(AstVisitor astVisitor);
        public object Visit(ICustomAstVisitor astVisitor);
    }
    public enum AstVisitAction
    {
        Continue = 0,
        SkipChildren = 1,
        StopVisit = 2,
    }
    public abstract class AstVisitor
    {
        protected AstVisitor();
        public virtual AstVisitAction VisitArrayExpression(ArrayExpressionAst arrayExpressionAst);
        public virtual AstVisitAction VisitArrayLiteral(ArrayLiteralAst arrayLiteralAst);
        public virtual AstVisitAction VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst);
        public virtual AstVisitAction VisitAttribute(AttributeAst attributeAst);
        public virtual AstVisitAction VisitAttributedExpression(AttributedExpressionAst attributedExpressionAst);
        public virtual AstVisitAction VisitBinaryExpression(BinaryExpressionAst binaryExpressionAst);
        public virtual AstVisitAction VisitBlockStatement(BlockStatementAst blockStatementAst);
        public virtual AstVisitAction VisitBreakStatement(BreakStatementAst breakStatementAst);
        public virtual AstVisitAction VisitCatchClause(CatchClauseAst catchClauseAst);
        public virtual AstVisitAction VisitCommand(CommandAst commandAst);
        public virtual AstVisitAction VisitCommandExpression(CommandExpressionAst commandExpressionAst);
        public virtual AstVisitAction VisitCommandParameter(CommandParameterAst commandParameterAst);
        public virtual AstVisitAction VisitConstantExpression(ConstantExpressionAst constantExpressionAst);
        public virtual AstVisitAction VisitContinueStatement(ContinueStatementAst continueStatementAst);
        public virtual AstVisitAction VisitConvertExpression(ConvertExpressionAst convertExpressionAst);
        public virtual AstVisitAction VisitDataStatement(DataStatementAst dataStatementAst);
        public virtual AstVisitAction VisitDoUntilStatement(DoUntilStatementAst doUntilStatementAst);
        public virtual AstVisitAction VisitDoWhileStatement(DoWhileStatementAst doWhileStatementAst);
        public virtual AstVisitAction VisitErrorExpression(ErrorExpressionAst errorExpressionAst);
        public virtual AstVisitAction VisitErrorStatement(ErrorStatementAst errorStatementAst);
        public virtual AstVisitAction VisitExitStatement(ExitStatementAst exitStatementAst);
        public virtual AstVisitAction VisitExpandableStringExpression(ExpandableStringExpressionAst expandableStringExpressionAst);
        public virtual AstVisitAction VisitFileRedirection(FileRedirectionAst redirectionAst);
        public virtual AstVisitAction VisitForEachStatement(ForEachStatementAst forEachStatementAst);
        public virtual AstVisitAction VisitForStatement(ForStatementAst forStatementAst);
        public virtual AstVisitAction VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst);
        public virtual AstVisitAction VisitHashtable(HashtableAst hashtableAst);
        public virtual AstVisitAction VisitIfStatement(IfStatementAst ifStmtAst);
        public virtual AstVisitAction VisitIndexExpression(IndexExpressionAst indexExpressionAst);
        public virtual AstVisitAction VisitInvokeMemberExpression(InvokeMemberExpressionAst methodCallAst);
        public virtual AstVisitAction VisitMemberExpression(MemberExpressionAst memberExpressionAst);
        public virtual AstVisitAction VisitMergingRedirection(MergingRedirectionAst redirectionAst);
        public virtual AstVisitAction VisitNamedAttributeArgument(NamedAttributeArgumentAst namedAttributeArgumentAst);
        public virtual AstVisitAction VisitNamedBlock(NamedBlockAst namedBlockAst);
        public virtual AstVisitAction VisitParamBlock(ParamBlockAst paramBlockAst);
        public virtual AstVisitAction VisitParameter(ParameterAst parameterAst);
        public virtual AstVisitAction VisitParenExpression(ParenExpressionAst parenExpressionAst);
        public virtual AstVisitAction VisitPipeline(PipelineAst pipelineAst);
        public virtual AstVisitAction VisitReturnStatement(ReturnStatementAst returnStatementAst);
        public virtual AstVisitAction VisitScriptBlock(ScriptBlockAst scriptBlockAst);
        public virtual AstVisitAction VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst);
        public virtual AstVisitAction VisitStatementBlock(StatementBlockAst statementBlockAst);
        public virtual AstVisitAction VisitStringConstantExpression(StringConstantExpressionAst stringConstantExpressionAst);
        public virtual AstVisitAction VisitSubExpression(SubExpressionAst subExpressionAst);
        public virtual AstVisitAction VisitSwitchStatement(SwitchStatementAst switchStatementAst);
        public virtual AstVisitAction VisitThrowStatement(ThrowStatementAst throwStatementAst);
        public virtual AstVisitAction VisitTrap(TrapStatementAst trapStatementAst);
        public virtual AstVisitAction VisitTryStatement(TryStatementAst tryStatementAst);
        public virtual AstVisitAction VisitTypeConstraint(TypeConstraintAst typeConstraintAst);
        public virtual AstVisitAction VisitTypeExpression(TypeExpressionAst typeExpressionAst);
        public virtual AstVisitAction VisitUnaryExpression(UnaryExpressionAst unaryExpressionAst);
        public virtual AstVisitAction VisitUsingExpression(UsingExpressionAst usingExpressionAst);
        public virtual AstVisitAction VisitVariableExpression(VariableExpressionAst variableExpressionAst);
        public virtual AstVisitAction VisitWhileStatement(WhileStatementAst whileStatementAst);
    }
    public abstract class AstVisitor2 : AstVisitor
    {
        protected AstVisitor2();
        public virtual AstVisitAction VisitBaseCtorInvokeMemberExpression(BaseCtorInvokeMemberExpressionAst baseCtorInvokeMemberExpressionAst);
        public virtual AstVisitAction VisitConfigurationDefinition(ConfigurationDefinitionAst configurationDefinitionAst);
        public virtual AstVisitAction VisitDynamicKeywordStatement(DynamicKeywordStatementAst dynamicKeywordStatementAst);
        public virtual AstVisitAction VisitFunctionMember(FunctionMemberAst functionMemberAst);
        public virtual AstVisitAction VisitPropertyMember(PropertyMemberAst propertyMemberAst);
        public virtual AstVisitAction VisitTypeDefinition(TypeDefinitionAst typeDefinitionAst);
        public virtual AstVisitAction VisitUsingStatement(UsingStatementAst usingStatementAst);
    }
    public class AttributeAst : AttributeBaseAst
    {
        public AttributeAst(IScriptExtent extent, ITypeName typeName, IEnumerable<ExpressionAst> positionalArguments, IEnumerable<NamedAttributeArgumentAst> namedArguments);
        public ReadOnlyCollection<NamedAttributeArgumentAst> NamedArguments { get; }
        public ReadOnlyCollection<ExpressionAst> PositionalArguments { get; }
        public override Ast Copy();
    }
    public abstract class AttributeBaseAst : Ast
    {
        protected AttributeBaseAst(IScriptExtent extent, ITypeName typeName);
        public ITypeName TypeName { get; }
    }
    public class AttributedExpressionAst : ExpressionAst
    {
        public AttributedExpressionAst(IScriptExtent extent, AttributeBaseAst attribute, ExpressionAst child);
        public AttributeBaseAst Attribute { get; }
        public ExpressionAst Child { get; }
        public override Ast Copy();
    }
    public class BaseCtorInvokeMemberExpressionAst : InvokeMemberExpressionAst
    {
        public BaseCtorInvokeMemberExpressionAst(IScriptExtent baseKeywordExtent, IScriptExtent baseCallExtent, IEnumerable<ExpressionAst> arguments);
    }
    public class BinaryExpressionAst : ExpressionAst
    {
        public BinaryExpressionAst(IScriptExtent extent, ExpressionAst left, TokenKind @operator, ExpressionAst right, IScriptExtent errorPosition);
        public IScriptExtent ErrorPosition { get; }
        public ExpressionAst Left { get; }
        public TokenKind Operator { get; }
        public ExpressionAst Right { get; }
        public override Type StaticType { get; }
        public override Ast Copy();
    }
    public class BlockStatementAst : StatementAst
    {
        public BlockStatementAst(IScriptExtent extent, Token kind, StatementBlockAst body);
        public StatementBlockAst Body { get; }
        public Token Kind { get; }
        public override Ast Copy();
    }
    public class BreakStatementAst : StatementAst
    {
        public BreakStatementAst(IScriptExtent extent, ExpressionAst label);
        public ExpressionAst Label { get; }
        public override Ast Copy();
    }
    public class CatchClauseAst : Ast
    {
        public CatchClauseAst(IScriptExtent extent, IEnumerable<TypeConstraintAst> catchTypes, StatementBlockAst body);
        public StatementBlockAst Body { get; }
        public ReadOnlyCollection<TypeConstraintAst> CatchTypes { get; }
        public bool IsCatchAll { get; }
        public override Ast Copy();
    }
    public static class CodeGeneration
    {
        public static string EscapeBlockCommentContent(string value);
        public static string EscapeFormatStringContent(string value);
        public static string EscapeSingleQuotedStringContent(string value);
        public static string EscapeVariableName(string value);
    }
    public class CommandAst : CommandBaseAst
    {
        public CommandAst(IScriptExtent extent, IEnumerable<CommandElementAst> commandElements, TokenKind invocationOperator, IEnumerable<RedirectionAst> redirections);
        public ReadOnlyCollection<CommandElementAst> CommandElements { get; }
        public DynamicKeyword DefiningKeyword { get; set; }
        public TokenKind InvocationOperator { get; }
        public override Ast Copy();
        public string GetCommandName();
    }
    public abstract class CommandBaseAst : StatementAst
    {
        protected CommandBaseAst(IScriptExtent extent, IEnumerable<RedirectionAst> redirections);
        public ReadOnlyCollection<RedirectionAst> Redirections { get; }
    }
    public abstract class CommandElementAst : Ast
    {
        protected CommandElementAst(IScriptExtent extent);
    }
    public class CommandExpressionAst : CommandBaseAst
    {
        public CommandExpressionAst(IScriptExtent extent, ExpressionAst expression, IEnumerable<RedirectionAst> redirections);
        public ExpressionAst Expression { get; }
        public override Ast Copy();
    }
    public class CommandParameterAst : CommandElementAst
    {
        public CommandParameterAst(IScriptExtent extent, string parameterName, ExpressionAst argument, IScriptExtent errorPosition);
        public ExpressionAst Argument { get; }
        public IScriptExtent ErrorPosition { get; }
        public string ParameterName { get; }
        public override Ast Copy();
    }
    public sealed class CommentHelpInfo
    {
        public CommentHelpInfo();
        public string Component { get; }
        public string Description { get; }
        public ReadOnlyCollection<string> Examples { get; }
        public string ForwardHelpCategory { get; }
        public string ForwardHelpTargetName { get; }
        public string Functionality { get; }
        public ReadOnlyCollection<string> Inputs { get; }
        public ReadOnlyCollection<string> Links { get; }
        public string MamlHelpFile { get; }
        public string Notes { get; }
        public ReadOnlyCollection<string> Outputs { get; }
        public IDictionary<string, string> Parameters { get; }
        public string RemoteHelpRunspace { get; }
        public string Role { get; }
        public string Synopsis { get; }
        public string GetCommentBlock();
    }
    public class ConfigurationDefinitionAst : StatementAst
    {
        public ConfigurationDefinitionAst(IScriptExtent extent, ScriptBlockExpressionAst body, ConfigurationType type, ExpressionAst instanceName);
        public ScriptBlockExpressionAst Body { get; }
        public ConfigurationType ConfigurationType { get; }
        public ExpressionAst InstanceName { get; }
        public override Ast Copy();
    }
    public enum ConfigurationType
    {
        Meta = 1,
        Resource = 0,
    }
    public class ConstantExpressionAst : ExpressionAst
    {
        public ConstantExpressionAst(IScriptExtent extent, object value);
        public override Type StaticType { get; }
        public object Value { get; }
        public override Ast Copy();
    }
    public class ContinueStatementAst : StatementAst
    {
        public ContinueStatementAst(IScriptExtent extent, ExpressionAst label);
        public ExpressionAst Label { get; }
        public override Ast Copy();
    }
    public class ConvertExpressionAst : AttributedExpressionAst
    {
        public ConvertExpressionAst(IScriptExtent extent, TypeConstraintAst typeConstraint, ExpressionAst child);
        public override Type StaticType { get; }
        public TypeConstraintAst Type { get; }
        public override Ast Copy();
    }
    public class DataStatementAst : StatementAst
    {
        public DataStatementAst(IScriptExtent extent, string variableName, IEnumerable<ExpressionAst> commandsAllowed, StatementBlockAst body);
        public StatementBlockAst Body { get; }
        public ReadOnlyCollection<ExpressionAst> CommandsAllowed { get; }
        public string Variable { get; }
        public override Ast Copy();
    }
    public abstract class DefaultCustomAstVisitor : ICustomAstVisitor
    {
        protected DefaultCustomAstVisitor();
        public virtual object VisitArrayExpression(ArrayExpressionAst arrayExpressionAst);
        public virtual object VisitArrayLiteral(ArrayLiteralAst arrayLiteralAst);
        public virtual object VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst);
        public virtual object VisitAttribute(AttributeAst attributeAst);
        public virtual object VisitAttributedExpression(AttributedExpressionAst attributedExpressionAst);
        public virtual object VisitBinaryExpression(BinaryExpressionAst binaryExpressionAst);
        public virtual object VisitBlockStatement(BlockStatementAst blockStatementAst);
        public virtual object VisitBreakStatement(BreakStatementAst breakStatementAst);
        public virtual object VisitCatchClause(CatchClauseAst catchClauseAst);
        public virtual object VisitCommand(CommandAst commandAst);
        public virtual object VisitCommandExpression(CommandExpressionAst commandExpressionAst);
        public virtual object VisitCommandParameter(CommandParameterAst commandParameterAst);
        public virtual object VisitConstantExpression(ConstantExpressionAst constantExpressionAst);
        public virtual object VisitContinueStatement(ContinueStatementAst continueStatementAst);
        public virtual object VisitConvertExpression(ConvertExpressionAst convertExpressionAst);
        public virtual object VisitDataStatement(DataStatementAst dataStatementAst);
        public virtual object VisitDoUntilStatement(DoUntilStatementAst doUntilStatementAst);
        public virtual object VisitDoWhileStatement(DoWhileStatementAst doWhileStatementAst);
        public virtual object VisitErrorExpression(ErrorExpressionAst errorExpressionAst);
        public virtual object VisitErrorStatement(ErrorStatementAst errorStatementAst);
        public virtual object VisitExitStatement(ExitStatementAst exitStatementAst);
        public virtual object VisitExpandableStringExpression(ExpandableStringExpressionAst expandableStringExpressionAst);
        public virtual object VisitFileRedirection(FileRedirectionAst fileRedirectionAst);
        public virtual object VisitForEachStatement(ForEachStatementAst forEachStatementAst);
        public virtual object VisitForStatement(ForStatementAst forStatementAst);
        public virtual object VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst);
        public virtual object VisitHashtable(HashtableAst hashtableAst);
        public virtual object VisitIfStatement(IfStatementAst ifStmtAst);
        public virtual object VisitIndexExpression(IndexExpressionAst indexExpressionAst);
        public virtual object VisitInvokeMemberExpression(InvokeMemberExpressionAst invokeMemberExpressionAst);
        public virtual object VisitMemberExpression(MemberExpressionAst memberExpressionAst);
        public virtual object VisitMergingRedirection(MergingRedirectionAst mergingRedirectionAst);
        public virtual object VisitNamedAttributeArgument(NamedAttributeArgumentAst namedAttributeArgumentAst);
        public virtual object VisitNamedBlock(NamedBlockAst namedBlockAst);
        public virtual object VisitParamBlock(ParamBlockAst paramBlockAst);
        public virtual object VisitParameter(ParameterAst parameterAst);
        public virtual object VisitParenExpression(ParenExpressionAst parenExpressionAst);
        public virtual object VisitPipeline(PipelineAst pipelineAst);
        public virtual object VisitReturnStatement(ReturnStatementAst returnStatementAst);
        public virtual object VisitScriptBlock(ScriptBlockAst scriptBlockAst);
        public virtual object VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst);
        public virtual object VisitStatementBlock(StatementBlockAst statementBlockAst);
        public virtual object VisitStringConstantExpression(StringConstantExpressionAst stringConstantExpressionAst);
        public virtual object VisitSubExpression(SubExpressionAst subExpressionAst);
        public virtual object VisitSwitchStatement(SwitchStatementAst switchStatementAst);
        public virtual object VisitThrowStatement(ThrowStatementAst throwStatementAst);
        public virtual object VisitTrap(TrapStatementAst trapStatementAst);
        public virtual object VisitTryStatement(TryStatementAst tryStatementAst);
        public virtual object VisitTypeConstraint(TypeConstraintAst typeConstraintAst);
        public virtual object VisitTypeExpression(TypeExpressionAst typeExpressionAst);
        public virtual object VisitUnaryExpression(UnaryExpressionAst unaryExpressionAst);
        public virtual object VisitUsingExpression(UsingExpressionAst usingExpressionAst);
        public virtual object VisitVariableExpression(VariableExpressionAst variableExpressionAst);
        public virtual object VisitWhileStatement(WhileStatementAst whileStatementAst);
    }
    public abstract class DefaultCustomAstVisitor2 : DefaultCustomAstVisitor, ICustomAstVisitor, ICustomAstVisitor2
    {
        protected DefaultCustomAstVisitor2();
        public virtual object VisitBaseCtorInvokeMemberExpression(BaseCtorInvokeMemberExpressionAst baseCtorInvokeMemberExpressionAst);
        public virtual object VisitConfigurationDefinition(ConfigurationDefinitionAst configurationAst);
        public virtual object VisitDynamicKeywordStatement(DynamicKeywordStatementAst dynamicKeywordAst);
        public virtual object VisitFunctionMember(FunctionMemberAst functionMemberAst);
        public virtual object VisitPropertyMember(PropertyMemberAst propertyMemberAst);
        public virtual object VisitTypeDefinition(TypeDefinitionAst typeDefinitionAst);
        public virtual object VisitUsingStatement(UsingStatementAst usingStatement);
    }
    public class DoUntilStatementAst : LoopStatementAst
    {
        public DoUntilStatementAst(IScriptExtent extent, string label, PipelineBaseAst condition, StatementBlockAst body);
        public override Ast Copy();
    }
    public class DoWhileStatementAst : LoopStatementAst
    {
        public DoWhileStatementAst(IScriptExtent extent, string label, PipelineBaseAst condition, StatementBlockAst body);
        public override Ast Copy();
    }
    public class DynamicKeyword
    {
        public DynamicKeyword();
        public DynamicKeywordBodyMode BodyMode { get; set; }
        public bool DirectCall { get; set; }
        public bool HasReservedProperties { get; set; }
        public string ImplementingModule { get; set; }
        public Version ImplementingModuleVersion { get; set; }
        public bool IsReservedKeyword { get; set; }
        public string Keyword { get; set; }
        public bool MetaStatement { get; set; }
        public DynamicKeywordNameMode NameMode { get; set; }
        public Dictionary<string, DynamicKeywordParameter> Parameters { get; }
        public Func<DynamicKeywordStatementAst, ParseError[]> PostParse { get; set; }
        public Func<DynamicKeyword, ParseError[]> PreParse { get; set; }
        public Dictionary<string, DynamicKeywordProperty> Properties { get; }
        public string ResourceName { get; set; }
        public Func<DynamicKeywordStatementAst, ParseError[]> SemanticCheck { get; set; }
        public static void AddKeyword(DynamicKeyword keywordToAdd);
        public static bool ContainsKeyword(string name);
        public DynamicKeyword Copy();
        public static List<DynamicKeyword> GetKeyword();
        public static DynamicKeyword GetKeyword(string name);
        public static void Pop();
        public static void Push();
        public static void RemoveKeyword(string name);
        public static void Reset();
    }
    public enum DynamicKeywordBodyMode
    {
        Command = 0,
        Hashtable = 2,
        ScriptBlock = 1,
    }
    public enum DynamicKeywordNameMode
    {
        NameRequired = 2,
        NoName = 0,
        OptionalName = 4,
        SimpleNameRequired = 1,
        SimpleOptionalName = 3,
    }
    public class DynamicKeywordParameter : DynamicKeywordProperty
    {
        public DynamicKeywordParameter();
        public bool Switch { get; set; }
    }
    public class DynamicKeywordProperty
    {
        public DynamicKeywordProperty();
        public List<string> Attributes { get; }
        public bool IsKey { get; set; }
        public bool Mandatory { get; set; }
        public string Name { get; set; }
        public Tuple<int, int> Range { get; set; }
        public string TypeConstraint { get; set; }
        public Dictionary<string, string> ValueMap { get; }
        public List<string> Values { get; }
    }
    public class DynamicKeywordStatementAst : StatementAst
    {
        public DynamicKeywordStatementAst(IScriptExtent extent, IEnumerable<CommandElementAst> commandElements);
        public ReadOnlyCollection<CommandElementAst> CommandElements { get; }
        public override Ast Copy();
    }
    public class ErrorExpressionAst : ExpressionAst
    {
        public ReadOnlyCollection<Ast> NestedAst { get; }
        public override Ast Copy();
    }
    public class ErrorStatementAst : PipelineBaseAst
    {
        public ReadOnlyCollection<Ast> Bodies { get; }
        public ReadOnlyCollection<Ast> Conditions { get; }
        public Dictionary<string, Tuple<Token, Ast>> Flags { get; }
        public Token Kind { get; }
        public ReadOnlyCollection<Ast> NestedAst { get; }
        public override Ast Copy();
    }
    public class ExitStatementAst : StatementAst
    {
        public ExitStatementAst(IScriptExtent extent, PipelineBaseAst pipeline);
        public PipelineBaseAst Pipeline { get; }
        public override Ast Copy();
    }
    public class ExpandableStringExpressionAst : ExpressionAst
    {
        public ExpandableStringExpressionAst(IScriptExtent extent, string value, StringConstantType type);
        public ReadOnlyCollection<ExpressionAst> NestedExpressions { get; }
        public override Type StaticType { get; }
        public StringConstantType StringConstantType { get; }
        public string Value { get; }
        public override Ast Copy();
    }
    public abstract class ExpressionAst : CommandElementAst
    {
        protected ExpressionAst(IScriptExtent extent);
        public virtual Type StaticType { get; }
    }
    public class FileRedirectionAst : RedirectionAst
    {
        public FileRedirectionAst(IScriptExtent extent, RedirectionStream stream, ExpressionAst file, bool append);
        public bool Append { get; }
        public ExpressionAst Location { get; }
        public override Ast Copy();
    }
    public class FileRedirectionToken : RedirectionToken
    {
        public bool Append { get; }
        public RedirectionStream FromStream { get; }
    }
    public enum ForEachFlags
    {
        None = 0,
        Parallel = 1,
    }
    public class ForEachStatementAst : LoopStatementAst
    {
        public ForEachStatementAst(IScriptExtent extent, string label, ForEachFlags flags, ExpressionAst throttleLimit, VariableExpressionAst variable, PipelineBaseAst expression, StatementBlockAst body);
        public ForEachStatementAst(IScriptExtent extent, string label, ForEachFlags flags, VariableExpressionAst variable, PipelineBaseAst expression, StatementBlockAst body);
        public ForEachFlags Flags { get; }
        public ExpressionAst ThrottleLimit { get; }
        public VariableExpressionAst Variable { get; }
        public override Ast Copy();
    }
    public class ForStatementAst : LoopStatementAst
    {
        public ForStatementAst(IScriptExtent extent, string label, PipelineBaseAst initializer, PipelineBaseAst condition, PipelineBaseAst iterator, StatementBlockAst body);
        public PipelineBaseAst Initializer { get; }
        public PipelineBaseAst Iterator { get; }
        public override Ast Copy();
    }
    public class FunctionDefinitionAst : StatementAst
    {
        public FunctionDefinitionAst(IScriptExtent extent, bool isFilter, bool isWorkflow, string name, IEnumerable<ParameterAst> parameters, ScriptBlockAst body);
        public ScriptBlockAst Body { get; }
        public bool IsFilter { get; }
        public bool IsWorkflow { get; }
        public string Name { get; }
        public ReadOnlyCollection<ParameterAst> Parameters { get; }
        public override Ast Copy();
        public CommentHelpInfo GetHelpContent();
        public CommentHelpInfo GetHelpContent(Dictionary<Ast, Token[]> scriptBlockTokenCache);
    }
    public class FunctionMemberAst : MemberAst
    {
        public FunctionMemberAst(IScriptExtent extent, FunctionDefinitionAst functionDefinitionAst, TypeConstraintAst returnType, IEnumerable<AttributeAst> attributes, MethodAttributes methodAttributes);
        public ReadOnlyCollection<AttributeAst> Attributes { get; }
        public ScriptBlockAst Body { get; }
        public bool IsConstructor { get; }
        public bool IsHidden { get; }
        public bool IsPrivate { get; }
        public bool IsPublic { get; }
        public bool IsStatic { get; }
        public MethodAttributes MethodAttributes { get; }
        public override string Name { get; }
        public ReadOnlyCollection<ParameterAst> Parameters { get; }
        public TypeConstraintAst ReturnType { get; }
        public override Ast Copy();
    }
    public sealed class GenericTypeName : ITypeName
    {
        public GenericTypeName(IScriptExtent extent, ITypeName genericTypeName, IEnumerable<ITypeName> genericArguments);
        public string AssemblyName { get; }
        public IScriptExtent Extent { get; }
        public string FullName { get; }
        public ReadOnlyCollection<ITypeName> GenericArguments { get; }
        public bool IsArray { get; }
        public bool IsGeneric { get; }
        public string Name { get; }
        public ITypeName TypeName { get; }
        public override bool Equals(object obj);
        public override int GetHashCode();
        public Type GetReflectionAttributeType();
        public Type GetReflectionType();
        public override string ToString();
    }
    public class HashtableAst : ExpressionAst
    {
        public HashtableAst(IScriptExtent extent, IEnumerable<Tuple<ExpressionAst, StatementAst>> keyValuePairs);
        public ReadOnlyCollection<Tuple<ExpressionAst, StatementAst>> KeyValuePairs { get; }
        public override Type StaticType { get; }
        public override Ast Copy();
    }
    public interface IAstPostVisitHandler
    {
        void PostVisit(Ast ast);
    }
    public interface ICustomAstVisitor
    {
        object VisitArrayExpression(ArrayExpressionAst arrayExpressionAst);
        object VisitArrayLiteral(ArrayLiteralAst arrayLiteralAst);
        object VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst);
        object VisitAttribute(AttributeAst attributeAst);
        object VisitAttributedExpression(AttributedExpressionAst attributedExpressionAst);
        object VisitBinaryExpression(BinaryExpressionAst binaryExpressionAst);
        object VisitBlockStatement(BlockStatementAst blockStatementAst);
        object VisitBreakStatement(BreakStatementAst breakStatementAst);
        object VisitCatchClause(CatchClauseAst catchClauseAst);
        object VisitCommand(CommandAst commandAst);
        object VisitCommandExpression(CommandExpressionAst commandExpressionAst);
        object VisitCommandParameter(CommandParameterAst commandParameterAst);
        object VisitConstantExpression(ConstantExpressionAst constantExpressionAst);
        object VisitContinueStatement(ContinueStatementAst continueStatementAst);
        object VisitConvertExpression(ConvertExpressionAst convertExpressionAst);
        object VisitDataStatement(DataStatementAst dataStatementAst);
        object VisitDoUntilStatement(DoUntilStatementAst doUntilStatementAst);
        object VisitDoWhileStatement(DoWhileStatementAst doWhileStatementAst);
        object VisitErrorExpression(ErrorExpressionAst errorExpressionAst);
        object VisitErrorStatement(ErrorStatementAst errorStatementAst);
        object VisitExitStatement(ExitStatementAst exitStatementAst);
        object VisitExpandableStringExpression(ExpandableStringExpressionAst expandableStringExpressionAst);
        object VisitFileRedirection(FileRedirectionAst fileRedirectionAst);
        object VisitForEachStatement(ForEachStatementAst forEachStatementAst);
        object VisitForStatement(ForStatementAst forStatementAst);
        object VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst);
        object VisitHashtable(HashtableAst hashtableAst);
        object VisitIfStatement(IfStatementAst ifStmtAst);
        object VisitIndexExpression(IndexExpressionAst indexExpressionAst);
        object VisitInvokeMemberExpression(InvokeMemberExpressionAst invokeMemberExpressionAst);
        object VisitMemberExpression(MemberExpressionAst memberExpressionAst);
        object VisitMergingRedirection(MergingRedirectionAst mergingRedirectionAst);
        object VisitNamedAttributeArgument(NamedAttributeArgumentAst namedAttributeArgumentAst);
        object VisitNamedBlock(NamedBlockAst namedBlockAst);
        object VisitParamBlock(ParamBlockAst paramBlockAst);
        object VisitParameter(ParameterAst parameterAst);
        object VisitParenExpression(ParenExpressionAst parenExpressionAst);
        object VisitPipeline(PipelineAst pipelineAst);
        object VisitReturnStatement(ReturnStatementAst returnStatementAst);
        object VisitScriptBlock(ScriptBlockAst scriptBlockAst);
        object VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst);
        object VisitStatementBlock(StatementBlockAst statementBlockAst);
        object VisitStringConstantExpression(StringConstantExpressionAst stringConstantExpressionAst);
        object VisitSubExpression(SubExpressionAst subExpressionAst);
        object VisitSwitchStatement(SwitchStatementAst switchStatementAst);
        object VisitThrowStatement(ThrowStatementAst throwStatementAst);
        object VisitTrap(TrapStatementAst trapStatementAst);
        object VisitTryStatement(TryStatementAst tryStatementAst);
        object VisitTypeConstraint(TypeConstraintAst typeConstraintAst);
        object VisitTypeExpression(TypeExpressionAst typeExpressionAst);
        object VisitUnaryExpression(UnaryExpressionAst unaryExpressionAst);
        object VisitUsingExpression(UsingExpressionAst usingExpressionAst);
        object VisitVariableExpression(VariableExpressionAst variableExpressionAst);
        object VisitWhileStatement(WhileStatementAst whileStatementAst);
    }
    public interface ICustomAstVisitor2 : ICustomAstVisitor
    {
        object VisitBaseCtorInvokeMemberExpression(BaseCtorInvokeMemberExpressionAst baseCtorInvokeMemberExpressionAst);
        object VisitConfigurationDefinition(ConfigurationDefinitionAst configurationDefinitionAst);
        object VisitDynamicKeywordStatement(DynamicKeywordStatementAst dynamicKeywordAst);
        object VisitFunctionMember(FunctionMemberAst functionMemberAst);
        object VisitPropertyMember(PropertyMemberAst propertyMemberAst);
        object VisitTypeDefinition(TypeDefinitionAst typeDefinitionAst);
        object VisitUsingStatement(UsingStatementAst usingStatement);
    }
    public class IfStatementAst : StatementAst
    {
        public IfStatementAst(IScriptExtent extent, IEnumerable<Tuple<PipelineBaseAst, StatementBlockAst>> clauses, StatementBlockAst elseClause);
        public ReadOnlyCollection<Tuple<PipelineBaseAst, StatementBlockAst>> Clauses { get; }
        public StatementBlockAst ElseClause { get; }
        public override Ast Copy();
    }
    public class IndexExpressionAst : ExpressionAst
    {
        public IndexExpressionAst(IScriptExtent extent, ExpressionAst target, ExpressionAst index);
        public ExpressionAst Index { get; }
        public ExpressionAst Target { get; }
        public override Ast Copy();
    }
    public class InputRedirectionToken : RedirectionToken
    {
    }
    public class InvokeMemberExpressionAst : MemberExpressionAst
    {
        public InvokeMemberExpressionAst(IScriptExtent extent, ExpressionAst expression, CommandElementAst method, IEnumerable<ExpressionAst> arguments, bool @static);
        public ReadOnlyCollection<ExpressionAst> Arguments { get; }
        public override Ast Copy();
    }
    public interface IScriptExtent
    {
        int EndColumnNumber { get; }
        int EndLineNumber { get; }
        int EndOffset { get; }
        IScriptPosition EndScriptPosition { get; }
        string File { get; }
        int StartColumnNumber { get; }
        int StartLineNumber { get; }
        int StartOffset { get; }
        IScriptPosition StartScriptPosition { get; }
        string Text { get; }
    }
    public interface IScriptPosition
    {
        int ColumnNumber { get; }
        string File { get; }
        string Line { get; }
        int LineNumber { get; }
        int Offset { get; }
        string GetFullScript();
    }
    public interface ITypeName
    {
        string AssemblyName { get; }
        IScriptExtent Extent { get; }
        string FullName { get; }
        bool IsArray { get; }
        bool IsGeneric { get; }
        string Name { get; }
        Type GetReflectionAttributeType();
        Type GetReflectionType();
    }
    public abstract class LabeledStatementAst : StatementAst
    {
        protected LabeledStatementAst(IScriptExtent extent, string label, PipelineBaseAst condition);
        public PipelineBaseAst Condition { get; }
        public string Label { get; }
    }
    public class LabelToken : Token
    {
        public string LabelText { get; }
    }
    public abstract class LoopStatementAst : LabeledStatementAst
    {
        protected LoopStatementAst(IScriptExtent extent, string label, PipelineBaseAst condition, StatementBlockAst body);
        public StatementBlockAst Body { get; }
    }
    public abstract class MemberAst : Ast
    {
        protected MemberAst(IScriptExtent extent);
        public abstract string Name { get; }
    }
    public class MemberExpressionAst : ExpressionAst
    {
        public MemberExpressionAst(IScriptExtent extent, ExpressionAst expression, CommandElementAst member, bool @static);
        public ExpressionAst Expression { get; }
        public CommandElementAst Member { get; }
        public bool Static { get; }
        public override Ast Copy();
    }
    public class MergingRedirectionAst : RedirectionAst
    {
        public MergingRedirectionAst(IScriptExtent extent, RedirectionStream from, RedirectionStream to);
        public RedirectionStream ToStream { get; }
        public override Ast Copy();
    }
    public class MergingRedirectionToken : RedirectionToken
    {
        public RedirectionStream FromStream { get; }
        public RedirectionStream ToStream { get; }
    }
    public enum MethodAttributes
    {
        Hidden = 64,
        None = 0,
        Private = 2,
        Public = 1,
        Static = 16,
    }
    public class NamedAttributeArgumentAst : Ast
    {
        public NamedAttributeArgumentAst(IScriptExtent extent, string argumentName, ExpressionAst argument, bool expressionOmitted);
        public ExpressionAst Argument { get; }
        public string ArgumentName { get; }
        public bool ExpressionOmitted { get; }
        public override Ast Copy();
    }
    public class NamedBlockAst : Ast
    {
        public NamedBlockAst(IScriptExtent extent, TokenKind blockName, StatementBlockAst statementBlock, bool unnamed);
        public TokenKind BlockKind { get; }
        public ReadOnlyCollection<StatementAst> Statements { get; }
        public ReadOnlyCollection<TrapStatementAst> Traps { get; }
        public bool Unnamed { get; }
        public override Ast Copy();
    }
    public class NullString
    {
        public static NullString Value { get; }
        public override string ToString();
    }
    public class NumberToken : Token
    {
        public object Value { get; }
    }
    public class ParamBlockAst : Ast
    {
        public ParamBlockAst(IScriptExtent extent, IEnumerable<AttributeAst> attributes, IEnumerable<ParameterAst> parameters);
        public ReadOnlyCollection<AttributeAst> Attributes { get; }
        public ReadOnlyCollection<ParameterAst> Parameters { get; }
        public override Ast Copy();
    }
    public class ParameterAst : Ast
    {
        public ParameterAst(IScriptExtent extent, VariableExpressionAst name, IEnumerable<AttributeBaseAst> attributes, ExpressionAst defaultValue);
        public ReadOnlyCollection<AttributeBaseAst> Attributes { get; }
        public ExpressionAst DefaultValue { get; }
        public VariableExpressionAst Name { get; }
        public Type StaticType { get; }
        public override Ast Copy();
    }
    public class ParameterBindingResult
    {
        public object ConstantValue { get; }
        public ParameterMetadata Parameter { get; }
        public CommandElementAst Value { get; }
    }
    public class ParameterToken : Token
    {
        public string ParameterName { get; }
        public bool UsedColon { get; }
    }
    public class ParenExpressionAst : ExpressionAst
    {
        public ParenExpressionAst(IScriptExtent extent, PipelineBaseAst pipeline);
        public PipelineBaseAst Pipeline { get; }
        public override Ast Copy();
    }
    public class ParseError
    {
        public ParseError(IScriptExtent extent, string errorId, string message);
        public string ErrorId { get; }
        public IScriptExtent Extent { get; }
        public bool IncompleteInput { get; }
        public string Message { get; }
        public override string ToString();
    }
    public sealed class Parser
    {
        public static ScriptBlockAst ParseFile(string fileName, out Token[] tokens, out ParseError[] errors);
        public static ScriptBlockAst ParseInput(string input, out Token[] tokens, out ParseError[] errors);
        public static ScriptBlockAst ParseInput(string input, string fileName, out Token[] tokens, out ParseError[] errors);
    }
    public class PipelineAst : PipelineBaseAst
    {
        public PipelineAst(IScriptExtent extent, IEnumerable<CommandBaseAst> pipelineElements);
        public PipelineAst(IScriptExtent extent, CommandBaseAst commandAst);
        public ReadOnlyCollection<CommandBaseAst> PipelineElements { get; }
        public override Ast Copy();
        public override ExpressionAst GetPureExpression();
    }
    public abstract class PipelineBaseAst : StatementAst
    {
        protected PipelineBaseAst(IScriptExtent extent);
        public virtual ExpressionAst GetPureExpression();
    }
    public enum PropertyAttributes
    {
        Hidden = 64,
        Literal = 32,
        None = 0,
        Private = 2,
        Public = 1,
        Static = 16,
    }
    public class PropertyMemberAst : MemberAst
    {
        public PropertyMemberAst(IScriptExtent extent, string name, TypeConstraintAst propertyType, IEnumerable<AttributeAst> attributes, PropertyAttributes propertyAttributes, ExpressionAst initialValue);
        public ReadOnlyCollection<AttributeAst> Attributes { get; }
        public ExpressionAst InitialValue { get; }
        public bool IsHidden { get; }
        public bool IsPrivate { get; }
        public bool IsPublic { get; }
        public bool IsStatic { get; }
        public override string Name { get; }
        public PropertyAttributes PropertyAttributes { get; }
        public TypeConstraintAst PropertyType { get; }
        public override Ast Copy();
    }
    public abstract class RedirectionAst : Ast
    {
        protected RedirectionAst(IScriptExtent extent, RedirectionStream from);
        public RedirectionStream FromStream { get; }
    }
    public enum RedirectionStream
    {
        All = 0,
        Debug = 5,
        Error = 2,
        Information = 6,
        Output = 1,
        Verbose = 4,
        Warning = 3,
    }
    public abstract class RedirectionToken : Token
    {
    }
    public sealed class ReflectionTypeName : ITypeName
    {
        public ReflectionTypeName(Type type);
        public string AssemblyName { get; }
        public IScriptExtent Extent { get; }
        public string FullName { get; }
        public bool IsArray { get; }
        public bool IsGeneric { get; }
        public string Name { get; }
        public override bool Equals(object obj);
        public override int GetHashCode();
        public Type GetReflectionAttributeType();
        public Type GetReflectionType();
        public override string ToString();
    }
    public class ReturnStatementAst : StatementAst
    {
        public ReturnStatementAst(IScriptExtent extent, PipelineBaseAst pipeline);
        public PipelineBaseAst Pipeline { get; }
        public override Ast Copy();
    }
    public class ScriptBlockAst : Ast
    {
        public ScriptBlockAst(IScriptExtent extent, IEnumerable<AttributeAst> attributes, ParamBlockAst paramBlock, StatementBlockAst statements, bool isFilter, bool isConfiguration);
        public ScriptBlockAst(IScriptExtent extent, IEnumerable<UsingStatementAst> usingStatements, IEnumerable<AttributeAst> attributes, ParamBlockAst paramBlock, NamedBlockAst beginBlock, NamedBlockAst processBlock, NamedBlockAst endBlock, NamedBlockAst dynamicParamBlock);
        public ScriptBlockAst(IScriptExtent extent, IEnumerable<UsingStatementAst> usingStatements, IEnumerable<AttributeAst> attributes, ParamBlockAst paramBlock, StatementBlockAst statements, bool isFilter, bool isConfiguration);
        public ScriptBlockAst(IScriptExtent extent, IEnumerable<UsingStatementAst> usingStatements, ParamBlockAst paramBlock, NamedBlockAst beginBlock, NamedBlockAst processBlock, NamedBlockAst endBlock, NamedBlockAst dynamicParamBlock);
        public ScriptBlockAst(IScriptExtent extent, IEnumerable<UsingStatementAst> usingStatements, ParamBlockAst paramBlock, StatementBlockAst statements, bool isFilter, bool isConfiguration);
        public ScriptBlockAst(IScriptExtent extent, List<UsingStatementAst> usingStatements, ParamBlockAst paramBlock, StatementBlockAst statements, bool isFilter);
        public ScriptBlockAst(IScriptExtent extent, ParamBlockAst paramBlock, NamedBlockAst beginBlock, NamedBlockAst processBlock, NamedBlockAst endBlock, NamedBlockAst dynamicParamBlock);
        public ScriptBlockAst(IScriptExtent extent, ParamBlockAst paramBlock, StatementBlockAst statements, bool isFilter);
        public ScriptBlockAst(IScriptExtent extent, ParamBlockAst paramBlock, StatementBlockAst statements, bool isFilter, bool isConfiguration);
        public ReadOnlyCollection<AttributeAst> Attributes { get; }
        public NamedBlockAst BeginBlock { get; }
        public NamedBlockAst DynamicParamBlock { get; }
        public NamedBlockAst EndBlock { get; }
        public ParamBlockAst ParamBlock { get; }
        public NamedBlockAst ProcessBlock { get; }
        public ScriptRequirements ScriptRequirements { get; }
        public ReadOnlyCollection<UsingStatementAst> UsingStatements { get; }
        public override Ast Copy();
        public CommentHelpInfo GetHelpContent();
        public ScriptBlock GetScriptBlock();
    }
    public class ScriptBlockExpressionAst : ExpressionAst
    {
        public ScriptBlockExpressionAst(IScriptExtent extent, ScriptBlockAst scriptBlock);
        public ScriptBlockAst ScriptBlock { get; }
        public override Type StaticType { get; }
        public override Ast Copy();
    }
    public sealed class ScriptExtent : IScriptExtent
    {
        public ScriptExtent(ScriptPosition startPosition, ScriptPosition endPosition);
        public int EndColumnNumber { get; }
        public int EndLineNumber { get; }
        public int EndOffset { get; }
        public IScriptPosition EndScriptPosition { get; }
        public string File { get; }
        public int StartColumnNumber { get; }
        public int StartLineNumber { get; }
        public int StartOffset { get; }
        public IScriptPosition StartScriptPosition { get; }
        public string Text { get; }
    }
    public sealed class ScriptPosition : IScriptPosition
    {
        public ScriptPosition(string scriptName, int scriptLineNumber, int offsetInLine, string line);
        public ScriptPosition(string scriptName, int scriptLineNumber, int offsetInLine, string line, string fullScript);
        public int ColumnNumber { get; }
        public string File { get; }
        public string Line { get; }
        public int LineNumber { get; }
        public int Offset { get; }
        public string GetFullScript();
    }
    public class ScriptRequirements
    {
        public ScriptRequirements();
        public bool IsElevationRequired { get; }
        public string RequiredApplicationId { get; }
        public ReadOnlyCollection<string> RequiredAssemblies { get; }
        public ReadOnlyCollection<ModuleSpecification> RequiredModules { get; }
        public ReadOnlyCollection<string> RequiredPSEditions { get; }
        public Version RequiredPSVersion { get; }
        public ReadOnlyCollection<PSSnapInSpecification> RequiresPSSnapIns { get; }
    }
    public abstract class StatementAst : Ast
    {
        protected StatementAst(IScriptExtent extent);
    }
    public class StatementBlockAst : Ast
    {
        public StatementBlockAst(IScriptExtent extent, IEnumerable<StatementAst> statements, IEnumerable<TrapStatementAst> traps);
        public ReadOnlyCollection<StatementAst> Statements { get; }
        public ReadOnlyCollection<TrapStatementAst> Traps { get; }
        public override Ast Copy();
    }
    public class StaticBindingError
    {
        public ParameterBindingException BindingException { get; }
        public CommandElementAst CommandElement { get; }
    }
    public class StaticBindingResult
    {
        public Dictionary<string, StaticBindingError> BindingExceptions { get; }
        public Dictionary<string, ParameterBindingResult> BoundParameters { get; }
    }
    public static class StaticParameterBinder
    {
        public static StaticBindingResult BindCommand(CommandAst commandAst);
        public static StaticBindingResult BindCommand(CommandAst commandAst, bool resolve);
        public static StaticBindingResult BindCommand(CommandAst commandAst, bool resolve, string[] desiredParameters);
    }
    public class StringConstantExpressionAst : ConstantExpressionAst
    {
        public StringConstantExpressionAst(IScriptExtent extent, string value, StringConstantType stringConstantType);
        public override Type StaticType { get; }
        public StringConstantType StringConstantType { get; }
        public new string Value { get; }
        public override Ast Copy();
    }
    public enum StringConstantType
    {
        BareWord = 4,
        DoubleQuoted = 2,
        DoubleQuotedHereString = 3,
        SingleQuoted = 0,
        SingleQuotedHereString = 1,
    }
    public class StringExpandableToken : StringToken
    {
        public ReadOnlyCollection<Token> NestedTokens { get; }
    }
    public class StringLiteralToken : StringToken
    {
    }
    public abstract class StringToken : Token
    {
        public string Value { get; }
    }
    public class SubExpressionAst : ExpressionAst
    {
        public SubExpressionAst(IScriptExtent extent, StatementBlockAst statementBlock);
        public StatementBlockAst SubExpression { get; }
        public override Ast Copy();
    }
    public enum SwitchFlags
    {
        CaseSensitive = 16,
        Exact = 8,
        File = 1,
        None = 0,
        Parallel = 32,
        Regex = 2,
        Wildcard = 4,
    }
    public class SwitchStatementAst : LabeledStatementAst
    {
        public SwitchStatementAst(IScriptExtent extent, string label, PipelineBaseAst condition, SwitchFlags flags, IEnumerable<Tuple<ExpressionAst, StatementBlockAst>> clauses, StatementBlockAst @default);
        public ReadOnlyCollection<Tuple<ExpressionAst, StatementBlockAst>> Clauses { get; }
        public StatementBlockAst Default { get; }
        public SwitchFlags Flags { get; }
        public override Ast Copy();
    }
    public class ThrowStatementAst : StatementAst
    {
        public ThrowStatementAst(IScriptExtent extent, PipelineBaseAst pipeline);
        public bool IsRethrow { get; }
        public PipelineBaseAst Pipeline { get; }
        public override Ast Copy();
    }
    public class Token
    {
        public IScriptExtent Extent { get; }
        public bool HasError { get; }
        public TokenKind Kind { get; }
        public string Text { get; }
        public TokenFlags TokenFlags { get; }
        public override string ToString();
    }
    public enum TokenFlags
    {
        AssignmentOperator = 8192,
        AttributeName = 4194304,
        BinaryOperator = 256,
        BinaryPrecedenceAdd = 4,
        BinaryPrecedenceBitwise = 2,
        BinaryPrecedenceComparison = 3,
        BinaryPrecedenceFormat = 6,
        BinaryPrecedenceLogical = 1,
        BinaryPrecedenceMask = 7,
        BinaryPrecedenceMultiply = 5,
        BinaryPrecedenceRange = 7,
        CanConstantFold = 8388608,
        CaseSensitiveOperator = 1024,
        CommandName = 524288,
        DisallowedInRestrictedMode = 131072,
        Keyword = 16,
        MemberName = 1048576,
        None = 0,
        ParseModeInvariant = 32768,
        PrefixOrPostfixOperator = 262144,
        ScriptBlockBlockName = 32,
        SpecialOperator = 4096,
        StatementDoesntSupportAttributes = 16777216,
        TokenInError = 65536,
        TypeName = 2097152,
        UnaryOperator = 512,
    }
    public enum TokenKind
    {
        Ampersand = 28,
        And = 53,
        AndAnd = 26,
        As = 94,
        Assembly = 165,
        AtCurly = 23,
        AtParen = 22,
        Band = 56,
        Base = 168,
        Begin = 119,
        Bnot = 52,
        Bor = 57,
        Break = 120,
        Bxor = 58,
        Catch = 121,
        Ccontains = 87,
        Ceq = 76,
        Cge = 78,
        Cgt = 79,
        Cin = 89,
        Class = 122,
        Cle = 81,
        Clike = 82,
        Clt = 80,
        Cmatch = 84,
        Cne = 77,
        Cnotcontains = 88,
        Cnotin = 90,
        Cnotlike = 83,
        Cnotmatch = 85,
        Colon = 99,
        ColonColon = 34,
        Comma = 30,
        Command = 166,
        Comment = 10,
        Configuration = 155,
        Continue = 123,
        Creplace = 86,
        Csplit = 91,
        Data = 124,
        Define = 125,
        Divide = 38,
        DivideEquals = 46,
        Do = 126,
        DollarParen = 24,
        Dot = 35,
        DotDot = 33,
        DynamicKeyword = 156,
        Dynamicparam = 127,
        Else = 128,
        ElseIf = 129,
        End = 130,
        EndOfInput = 11,
        Enum = 161,
        Equals = 42,
        Exclaim = 36,
        Exit = 131,
        Filter = 132,
        Finally = 133,
        For = 134,
        Foreach = 135,
        Format = 50,
        From = 136,
        Function = 137,
        Generic = 7,
        HereStringExpandable = 15,
        HereStringLiteral = 14,
        Hidden = 167,
        Icontains = 71,
        Identifier = 6,
        Ieq = 60,
        If = 138,
        Ige = 62,
        Igt = 63,
        Iin = 73,
        Ile = 65,
        Ilike = 66,
        Ilt = 64,
        Imatch = 68,
        In = 139,
        Ine = 61,
        InlineScript = 154,
        Inotcontains = 72,
        Inotin = 74,
        Inotlike = 67,
        Inotmatch = 69,
        Interface = 160,
        Ireplace = 70,
        Is = 92,
        IsNot = 93,
        Isplit = 75,
        Join = 59,
        Label = 5,
        LBracket = 20,
        LCurly = 18,
        LineContinuation = 9,
        LParen = 16,
        Minus = 41,
        MinusEquals = 44,
        MinusMinus = 31,
        Module = 163,
        Multiply = 37,
        MultiplyEquals = 45,
        Namespace = 162,
        NewLine = 8,
        Not = 51,
        Number = 4,
        Or = 54,
        OrOr = 27,
        Parallel = 152,
        Param = 140,
        Parameter = 3,
        Pipe = 29,
        Plus = 40,
        PlusEquals = 43,
        PlusPlus = 32,
        PostfixMinusMinus = 96,
        PostfixPlusPlus = 95,
        Private = 158,
        Process = 141,
        Public = 157,
        RBracket = 21,
        RCurly = 19,
        RedirectInStd = 49,
        Redirection = 48,
        Rem = 39,
        RemainderEquals = 47,
        Return = 142,
        RParen = 17,
        Semi = 25,
        Sequence = 153,
        Shl = 97,
        Shr = 98,
        SplattedVariable = 2,
        Static = 159,
        StringExpandable = 13,
        StringLiteral = 12,
        Switch = 143,
        Throw = 144,
        Trap = 145,
        Try = 146,
        Type = 164,
        Unknown = 0,
        Until = 147,
        Using = 148,
        Var = 149,
        Variable = 1,
        While = 150,
        Workflow = 151,
        Xor = 55,
    }
    public static class TokenTraits
    {
        public static TokenFlags GetTraits(this TokenKind kind);
        public static bool HasTrait(this TokenKind kind, TokenFlags flag);
        public static string Text(this TokenKind kind);
    }
    public class TrapStatementAst : StatementAst
    {
        public TrapStatementAst(IScriptExtent extent, TypeConstraintAst trapType, StatementBlockAst body);
        public StatementBlockAst Body { get; }
        public TypeConstraintAst TrapType { get; }
        public override Ast Copy();
    }
    public class TryStatementAst : StatementAst
    {
        public TryStatementAst(IScriptExtent extent, StatementBlockAst body, IEnumerable<CatchClauseAst> catchClauses, StatementBlockAst @finally);
        public StatementBlockAst Body { get; }
        public ReadOnlyCollection<CatchClauseAst> CatchClauses { get; }
        public StatementBlockAst Finally { get; }
        public override Ast Copy();
    }
    public enum TypeAttributes
    {
        Class = 1,
        Enum = 4,
        Interface = 2,
        None = 0,
    }
    public class TypeConstraintAst : AttributeBaseAst
    {
        public TypeConstraintAst(IScriptExtent extent, ITypeName typeName);
        public TypeConstraintAst(IScriptExtent extent, Type type);
        public override Ast Copy();
    }
    public class TypeDefinitionAst : StatementAst
    {
        public TypeDefinitionAst(IScriptExtent extent, string name, IEnumerable<AttributeAst> attributes, IEnumerable<MemberAst> members, TypeAttributes typeAttributes, IEnumerable<TypeConstraintAst> baseTypes);
        public ReadOnlyCollection<AttributeAst> Attributes { get; }
        public ReadOnlyCollection<TypeConstraintAst> BaseTypes { get; }
        public bool IsClass { get; }
        public bool IsEnum { get; }
        public bool IsInterface { get; }
        public ReadOnlyCollection<MemberAst> Members { get; }
        public string Name { get; }
        public TypeAttributes TypeAttributes { get; }
        public override Ast Copy();
    }
    public class TypeExpressionAst : ExpressionAst
    {
        public TypeExpressionAst(IScriptExtent extent, ITypeName typeName);
        public override Type StaticType { get; }
        public ITypeName TypeName { get; }
        public override Ast Copy();
    }
    public sealed class TypeName : ITypeName
    {
        public TypeName(IScriptExtent extent, string name);
        public TypeName(IScriptExtent extent, string name, string assembly);
        public string AssemblyName { get; }
        public IScriptExtent Extent { get; }
        public string FullName { get; }
        public bool IsArray { get; }
        public bool IsGeneric { get; }
        public string Name { get; }
        public override bool Equals(object obj);
        public override int GetHashCode();
        public Type GetReflectionAttributeType();
        public Type GetReflectionType();
        public override string ToString();
    }
    public class UnaryExpressionAst : ExpressionAst
    {
        public UnaryExpressionAst(IScriptExtent extent, TokenKind tokenKind, ExpressionAst child);
        public ExpressionAst Child { get; }
        public override Type StaticType { get; }
        public TokenKind TokenKind { get; }
        public override Ast Copy();
    }
    public class UsingExpressionAst : ExpressionAst
    {
        public UsingExpressionAst(IScriptExtent extent, ExpressionAst expressionAst);
        public ExpressionAst SubExpression { get; }
        public override Ast Copy();
        public static VariableExpressionAst ExtractUsingVariable(UsingExpressionAst usingExpressionAst);
    }
    public class UsingStatementAst : StatementAst
    {
        public UsingStatementAst(IScriptExtent extent, HashtableAst moduleSpecification);
        public UsingStatementAst(IScriptExtent extent, StringConstantExpressionAst aliasName, HashtableAst moduleSpecification);
        public UsingStatementAst(IScriptExtent extent, UsingStatementKind kind, StringConstantExpressionAst name);
        public UsingStatementAst(IScriptExtent extent, UsingStatementKind kind, StringConstantExpressionAst aliasName, StringConstantExpressionAst resolvedAliasAst);
        public StringConstantExpressionAst Alias { get; }
        public HashtableAst ModuleSpecification { get; }
        public StringConstantExpressionAst Name { get; }
        public UsingStatementKind UsingStatementKind { get; }
        public override Ast Copy();
    }
    public enum UsingStatementKind
    {
        Assembly = 0,
        Command = 1,
        Module = 2,
        Namespace = 3,
        Type = 4,
    }
    public class VariableExpressionAst : ExpressionAst
    {
        public VariableExpressionAst(IScriptExtent extent, VariablePath variablePath, bool splatted);
        public VariableExpressionAst(IScriptExtent extent, string variableName, bool splatted);
        public bool Splatted { get; }
        public VariablePath VariablePath { get; }
        public override Ast Copy();
        public bool IsConstantVariable();
    }
    public class VariableToken : Token
    {
        public string Name { get; }
        public VariablePath VariablePath { get; }
    }
    public class WhileStatementAst : LoopStatementAst
    {
        public WhileStatementAst(IScriptExtent extent, string label, PipelineBaseAst condition, StatementBlockAst body);
        public override Ast Copy();
    }
}
namespace System.Management.Automation.Provider
{
    public abstract class CmdletProvider : IResourceSupplier
    {
        protected CmdletProvider();
        public PSCredential Credential { get; }
        public PSTransactionContext CurrentPSTransaction { get; }
        protected object DynamicParameters { get; }
        public Collection<string> Exclude { get; }
        public string Filter { get; }
        public SwitchParameter Force { get; }
        public PSHost Host { get; }
        public Collection<string> Include { get; }
        public CommandInvocationIntrinsics InvokeCommand { get; }
        public ProviderIntrinsics InvokeProvider { get; }
        protected internal ProviderInfo ProviderInfo { get; }
        protected PSDriveInfo PSDriveInfo { get; }
        public SessionState SessionState { get; }
        public bool Stopping { get; }
        public virtual string GetResourceString(string baseName, string resourceId);
        public bool ShouldContinue(string query, string caption);
        public bool ShouldContinue(string query, string caption, ref bool yesToAll, ref bool noToAll);
        public bool ShouldProcess(string target);
        public bool ShouldProcess(string target, string action);
        public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption);
        public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption, out ShouldProcessReason shouldProcessReason);
        protected virtual ProviderInfo Start(ProviderInfo providerInfo);
        protected virtual object StartDynamicParameters();
        protected virtual void Stop();
        protected internal virtual void StopProcessing();
        public void ThrowTerminatingError(ErrorRecord errorRecord);
        public bool TransactionAvailable();
        public void WriteDebug(string text);
        public void WriteError(ErrorRecord errorRecord);
        public void WriteInformation(InformationRecord record);
        public void WriteInformation(object messageData, string[] tags);
        public void WriteItemObject(object item, string path, bool isContainer);
        public void WriteProgress(ProgressRecord progressRecord);
        public void WritePropertyObject(object propertyValue, string path);
        public void WriteSecurityDescriptorObject(ObjectSecurity securityDescriptor, string path);
        public void WriteVerbose(string text);
        public void WriteWarning(string text);
    }
    public sealed class CmdletProviderAttribute : Attribute
    {
        public CmdletProviderAttribute(string providerName, ProviderCapabilities providerCapabilities);
        public ProviderCapabilities ProviderCapabilities { get; }
        public string ProviderName { get; }
    }
    public abstract class ContainerCmdletProvider : ItemCmdletProvider
    {
        protected ContainerCmdletProvider();
        protected virtual bool ConvertPath(string path, string filter, ref string updatedPath, ref string updatedFilter);
        protected virtual void CopyItem(string path, string copyPath, bool recurse);
        protected virtual object CopyItemDynamicParameters(string path, string destination, bool recurse);
        protected virtual void GetChildItems(string path, bool recurse);
        protected virtual void GetChildItems(string path, bool recurse, uint depth);
        protected virtual object GetChildItemsDynamicParameters(string path, bool recurse);
        protected virtual void GetChildNames(string path, ReturnContainers returnContainers);
        protected virtual object GetChildNamesDynamicParameters(string path);
        protected virtual bool HasChildItems(string path);
        protected virtual void NewItem(string path, string itemTypeName, object newItemValue);
        protected virtual object NewItemDynamicParameters(string path, string itemTypeName, object newItemValue);
        protected virtual void RemoveItem(string path, bool recurse);
        protected virtual object RemoveItemDynamicParameters(string path, bool recurse);
        protected virtual void RenameItem(string path, string newName);
        protected virtual object RenameItemDynamicParameters(string path, string newName);
    }
    public abstract class DriveCmdletProvider : CmdletProvider
    {
        protected DriveCmdletProvider();
        protected virtual Collection<PSDriveInfo> InitializeDefaultDrives();
        protected virtual PSDriveInfo NewDrive(PSDriveInfo drive);
        protected virtual object NewDriveDynamicParameters();
        protected virtual PSDriveInfo RemoveDrive(PSDriveInfo drive);
    }
    public interface ICmdletProviderSupportsHelp
    {
        string GetHelpMaml(string helpItemName, string path);
    }
    public interface IContentCmdletProvider
    {
        void ClearContent(string path);
        object ClearContentDynamicParameters(string path);
        IContentReader GetContentReader(string path);
        object GetContentReaderDynamicParameters(string path);
        IContentWriter GetContentWriter(string path);
        object GetContentWriterDynamicParameters(string path);
    }
    public interface IContentReader : IDisposable
    {
        void Close();
        IList Read(long readCount);
        void Seek(long offset, SeekOrigin origin);
    }
    public interface IContentWriter : IDisposable
    {
        void Close();
        void Seek(long offset, SeekOrigin origin);
        IList Write(IList content);
    }
    public interface IDynamicPropertyCmdletProvider : IPropertyCmdletProvider
    {
        void CopyProperty(string sourcePath, string sourceProperty, string destinationPath, string destinationProperty);
        object CopyPropertyDynamicParameters(string sourcePath, string sourceProperty, string destinationPath, string destinationProperty);
        void MoveProperty(string sourcePath, string sourceProperty, string destinationPath, string destinationProperty);
        object MovePropertyDynamicParameters(string sourcePath, string sourceProperty, string destinationPath, string destinationProperty);
        void NewProperty(string path, string propertyName, string propertyTypeName, object value);
        object NewPropertyDynamicParameters(string path, string propertyName, string propertyTypeName, object value);
        void RemoveProperty(string path, string propertyName);
        object RemovePropertyDynamicParameters(string path, string propertyName);
        void RenameProperty(string path, string sourceProperty, string destinationProperty);
        object RenamePropertyDynamicParameters(string path, string sourceProperty, string destinationProperty);
    }
    public interface IPropertyCmdletProvider
    {
        void ClearProperty(string path, Collection<string> propertyToClear);
        object ClearPropertyDynamicParameters(string path, Collection<string> propertyToClear);
        void GetProperty(string path, Collection<string> providerSpecificPickList);
        object GetPropertyDynamicParameters(string path, Collection<string> providerSpecificPickList);
        void SetProperty(string path, PSObject propertyValue);
        object SetPropertyDynamicParameters(string path, PSObject propertyValue);
    }
    public interface ISecurityDescriptorCmdletProvider
    {
        void GetSecurityDescriptor(string path, AccessControlSections includeSections);
        ObjectSecurity NewSecurityDescriptorFromPath(string path, AccessControlSections includeSections);
        ObjectSecurity NewSecurityDescriptorOfType(string type, AccessControlSections includeSections);
        void SetSecurityDescriptor(string path, ObjectSecurity securityDescriptor);
    }
    public abstract class ItemCmdletProvider : DriveCmdletProvider
    {
        protected ItemCmdletProvider();
        protected virtual void ClearItem(string path);
        protected virtual object ClearItemDynamicParameters(string path);
        protected virtual string[] ExpandPath(string path);
        protected virtual void GetItem(string path);
        protected virtual object GetItemDynamicParameters(string path);
        protected virtual void InvokeDefaultAction(string path);
        protected virtual object InvokeDefaultActionDynamicParameters(string path);
        protected abstract bool IsValidPath(string path);
        protected virtual bool ItemExists(string path);
        protected virtual object ItemExistsDynamicParameters(string path);
        protected virtual void SetItem(string path, object value);
        protected virtual object SetItemDynamicParameters(string path, object value);
    }
    public abstract class NavigationCmdletProvider : ContainerCmdletProvider
    {
        protected NavigationCmdletProvider();
        protected virtual string GetChildName(string path);
        protected virtual string GetParentPath(string path, string root);
        protected virtual bool IsItemContainer(string path);
        protected virtual string MakePath(string parent, string child);
        protected string MakePath(string parent, string child, bool childIsLeaf);
        protected virtual void MoveItem(string path, string destination);
        protected virtual object MoveItemDynamicParameters(string path, string destination);
        protected virtual string NormalizeRelativePath(string path, string basePath);
    }
    public enum ProviderCapabilities
    {
        Credentials = 32,
        Exclude = 2,
        ExpandWildcards = 8,
        Filter = 4,
        Include = 1,
        None = 0,
        ShouldProcess = 16,
        Transactions = 64,
    }
}
namespace System.Management.Automation.Remoting
{
    public class CmdletMethodInvoker<T>
    {
        public CmdletMethodInvoker();
        public Func<Cmdlet, T> Action { get; set; }
        public Exception ExceptionThrownOnCmdletThread { get; set; }
        public ManualResetEventSlim Finished { get; set; }
        public T MethodResult { get; set; }
        public object SyncObject { get; set; }
    }
    public class OriginInfo
    {
        public OriginInfo(string computerName, Guid runspaceID);
        public OriginInfo(string computerName, Guid runspaceID, Guid instanceID);
        public Guid InstanceID { get; set; }
        public string PSComputerName { get; }
        public Guid RunspaceID { get; }
        public override string ToString();
    }
    public enum ProxyAccessType
    {
        AutoDetect = 4,
        IEConfig = 1,
        None = 0,
        NoProxyServer = 8,
        WinHttpConfig = 2,
    }
    public sealed class PSCertificateDetails
    {
        public PSCertificateDetails(string subject, string issuerName, string issuerThumbprint);
        public string IssuerName { get; }
        public string IssuerThumbprint { get; }
        public string Subject { get; }
    }
    public class PSDirectException : RuntimeException
    {
        public PSDirectException(string message);
    }
    public sealed class PSIdentity : IIdentity
    {
        public PSIdentity(string authType, bool isAuthenticated, string userName, PSCertificateDetails cert);
        public string AuthenticationType { get; }
        public PSCertificateDetails CertificateDetails { get; }
        public bool IsAuthenticated { get; }
        public string Name { get; }
    }
    public sealed class PSPrincipal : IPrincipal
    {
        public PSPrincipal(PSIdentity identity, WindowsIdentity windowsIdentity);
        public PSIdentity Identity { get; }
        IIdentity System.Security.Principal.IPrincipal.Identity { get; }
        public WindowsIdentity WindowsIdentity { get; }
        public bool IsInRole(string role);
    }
    public class PSRemotingDataStructureException : RuntimeException
    {
        public PSRemotingDataStructureException();
        protected PSRemotingDataStructureException(SerializationInfo info, StreamingContext context);
        public PSRemotingDataStructureException(string message);
        public PSRemotingDataStructureException(string message, Exception innerException);
    }
    public class PSRemotingTransportException : RuntimeException
    {
        public PSRemotingTransportException();
        protected PSRemotingTransportException(SerializationInfo info, StreamingContext context);
        public PSRemotingTransportException(string message);
        public PSRemotingTransportException(string message, Exception innerException);
        public int ErrorCode { get; set; }
        public string TransportMessage { get; set; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
        protected void SetDefaultErrorRecord();
    }
    public class PSRemotingTransportRedirectException : PSRemotingTransportException
    {
        public PSRemotingTransportRedirectException();
        protected PSRemotingTransportRedirectException(SerializationInfo info, StreamingContext context);
        public PSRemotingTransportRedirectException(string message);
        public PSRemotingTransportRedirectException(string message, Exception innerException);
        public string RedirectLocation { get; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
    }
    public sealed class PSSenderInfo : ISerializable
    {
        public PSSenderInfo(PSPrincipal userPrincipal, string httpUrl);
        public PSPrimitiveDictionary ApplicationArguments { get; }
        public string ConfigurationName { get; }
        public string ConnectionString { get; }
        public PSPrincipal UserInfo { get; }
        public void GetObjectData(SerializationInfo info, StreamingContext context);
    }
    public abstract class PSSessionConfiguration : IDisposable
    {
        protected PSSessionConfiguration();
        public void Dispose();
        protected virtual void Dispose(bool isDisposing);
        public virtual PSPrimitiveDictionary GetApplicationPrivateData(PSSenderInfo senderInfo);
        public abstract InitialSessionState GetInitialSessionState(PSSenderInfo senderInfo);
        public virtual InitialSessionState GetInitialSessionState(PSSessionConfigurationData sessionConfigurationData, PSSenderInfo senderInfo, string configProviderId);
        public virtual Nullable<int> GetMaximumReceivedDataSizePerCommand(PSSenderInfo senderInfo);
        public virtual Nullable<int> GetMaximumReceivedObjectSize(PSSenderInfo senderInfo);
    }
    public sealed class PSSessionConfigurationData
    {
        public static bool IsServerManager;
        public List<string> ModulesToImport { get; }
        public string PrivateData { get; }
    }
    public sealed class PSSessionOption
    {
        public PSSessionOption();
        public PSPrimitiveDictionary ApplicationArguments { get; set; }
        public TimeSpan CancelTimeout { get; set; }
        public CultureInfo Culture { get; set; }
        public TimeSpan IdleTimeout { get; set; }
        public bool IncludePortInSPN { get; set; }
        public int MaxConnectionRetryCount { get; set; }
        public int MaximumConnectionRedirectionCount { get; set; }
        public Nullable<int> MaximumReceivedDataSizePerCommand { get; set; }
        public Nullable<int> MaximumReceivedObjectSize { get; set; }
        public bool NoCompression { get; set; }
        public bool NoEncryption { get; set; }
        public bool NoMachineProfile { get; set; }
        public TimeSpan OpenTimeout { get; set; }
        public TimeSpan OperationTimeout { get; set; }
        public OutputBufferingMode OutputBufferingMode { get; set; }
        public ProxyAccessType ProxyAccessType { get; set; }
        public AuthenticationMechanism ProxyAuthentication { get; set; }
        public PSCredential ProxyCredential { get; set; }
        public bool SkipCACheck { get; set; }
        public bool SkipCNCheck { get; set; }
        public bool SkipRevocationCheck { get; set; }
        public CultureInfo UICulture { get; set; }
        public bool UseUTF16 { get; set; }
    }
    public enum SessionType
    {
        Default = 2,
        Empty = 0,
        RestrictedRemoteServer = 1,
    }
    public sealed class WSManPluginManagedEntryInstanceWrapper : IDisposable
    {
        public WSManPluginManagedEntryInstanceWrapper();
        public void Dispose();
        ~WSManPluginManagedEntryInstanceWrapper();
        public IntPtr GetEntryDelegate();
    }
    public sealed class WSManPluginManagedEntryWrapper
    {
        public static int InitPlugin(IntPtr wkrPtrs);
        public static void PSPluginOperationShutdownCallback(object operationContext, bool timedOut);
        public static void ShutdownPlugin(IntPtr pluginContext);
        public static void WSManPluginCommand(IntPtr pluginContext, IntPtr requestDetails, int flags, IntPtr shellContext, string commandLine, IntPtr arguments);
        public static void WSManPluginConnect(IntPtr pluginContext, IntPtr requestDetails, int flags, IntPtr shellContext, IntPtr commandContext, IntPtr inboundConnectInformation);
        public static void WSManPluginReceive(IntPtr pluginContext, IntPtr requestDetails, int flags, IntPtr shellContext, IntPtr commandContext, IntPtr streamSet);
        public static void WSManPluginReleaseCommandContext(IntPtr pluginContext, IntPtr shellContext, IntPtr commandContext);
        public static void WSManPluginReleaseShellContext(IntPtr pluginContext, IntPtr shellContext);
        public static void WSManPluginSend(IntPtr pluginContext, IntPtr requestDetails, int flags, IntPtr shellContext, IntPtr commandContext, string stream, IntPtr inboundData);
        public static void WSManPluginShell(IntPtr pluginContext, IntPtr requestDetails, int flags, string extraInfo, IntPtr startupInfo, IntPtr inboundShellInformation);
        public static void WSManPluginSignal(IntPtr pluginContext, IntPtr requestDetails, int flags, IntPtr shellContext, IntPtr commandContext, string code);
        public static void WSManPSShutdown(IntPtr shutdownContext);
    }
}
namespace System.Management.Automation.Remoting.Internal
{
    public class PSStreamObject
    {
        public PSStreamObject(PSStreamObjectType objectType, object value);
        public PSStreamObjectType ObjectType { get; set; }
        public void WriteStreamObject(Cmdlet cmdlet, bool overrideInquire=false);
    }
    public enum PSStreamObjectType
    {
        BlockingError = 5,
        Debug = 8,
        Error = 2,
        Exception = 12,
        Information = 11,
        MethodExecutor = 3,
        Output = 1,
        Progress = 9,
        ShouldMethod = 6,
        Verbose = 10,
        Warning = 4,
        WarningRecord = 7,
    }
}
namespace System.Management.Automation.Remoting.WSMan
{
    public sealed class ActiveSessionsChangedEventArgs : EventArgs
    {
        public ActiveSessionsChangedEventArgs(int activeSessionsCount);
        public int ActiveSessionsCount { get; }
    }
    public static class WSManServerChannelEvents
    {
        public static event EventHandler<ActiveSessionsChangedEventArgs> ActiveSessionsChanged;
        public static event EventHandler ShuttingDown;
    }
}
namespace System.Management.Automation.Runspaces
{
    public sealed class AliasPropertyData : TypeMemberData
    {
        public AliasPropertyData(string name, string referencedMemberName);
        public AliasPropertyData(string name, string referencedMemberName, Type type);
        public bool IsHidden { get; set; }
        public Type MemberType { get; set; }
        public string ReferencedMemberName { get; set; }
    }
    public enum AuthenticationMechanism
    {
        Basic = 1,
        Credssp = 4,
        Default = 0,
        Digest = 5,
        Kerberos = 6,
        Negotiate = 2,
        NegotiateWithImplicitCredential = 3,
    }
    public sealed class CodeMethodData : TypeMemberData
    {
        public CodeMethodData(string name, MethodInfo methodToCall);
        public MethodInfo CodeReference { get; set; }
    }
    public sealed class CodePropertyData : TypeMemberData
    {
        public CodePropertyData(string name, MethodInfo getMethod);
        public CodePropertyData(string name, MethodInfo getMethod, MethodInfo setMethod);
        public MethodInfo GetCodeReference { get; set; }
        public bool IsHidden { get; set; }
        public MethodInfo SetCodeReference { get; set; }
    }
    public sealed class Command
    {
        public Command(string command);
        public Command(string command, bool isScript);
        public Command(string command, bool isScript, bool useLocalScope);
        public CommandOrigin CommandOrigin { get; set; }
        public string CommandText { get; }
        public bool IsEndOfStatement { get; }
        public bool IsScript { get; }
        public PipelineResultTypes MergeUnclaimedPreviousCommandResults { get; set; }
        public CommandParameterCollection Parameters { get; }
        public bool UseLocalScope { get; }
        public void MergeMyResults(PipelineResultTypes myResult, PipelineResultTypes toResult);
        public override string ToString();
    }
    public sealed class CommandCollection : Collection<Command>
    {
        public void Add(string command);
        public void AddScript(string scriptContents);
        public void AddScript(string scriptContents, bool useLocalScope);
    }
    public sealed class CommandParameter
    {
        public CommandParameter(string name);
        public CommandParameter(string name, object value);
        public string Name { get; }
        public object Value { get; }
    }
    public sealed class CommandParameterCollection : Collection<CommandParameter>
    {
        public CommandParameterCollection();
        public void Add(string name);
        public void Add(string name, object value);
    }
    public abstract class ConstrainedSessionStateEntry : InitialSessionStateEntry
    {
        protected ConstrainedSessionStateEntry(string name, SessionStateEntryVisibility visibility);
        public SessionStateEntryVisibility Visibility { get; set; }
    }
    public sealed class ContainerConnectionInfo : RunspaceConnectionInfo
    {
        public override AuthenticationMechanism AuthenticationMechanism { get; set; }
        public override string CertificateThumbprint { get; set; }
        public override string ComputerName { get; set; }
        public override PSCredential Credential { get; set; }
        public static ContainerConnectionInfo CreateContainerConnectionInfo(string containerId, bool runAsAdmin, string configurationName);
        public void CreateContainerProcess();
        public bool TerminateContainerProcess();
    }
    public sealed class FormatTable
    {
        public FormatTable(IEnumerable<string> formatFiles);
        public void AppendFormatData(IEnumerable<ExtendedTypeDefinition> formatData);
        public static FormatTable LoadDefaultFormatFiles();
        public void PrependFormatData(IEnumerable<ExtendedTypeDefinition> formatData);
    }
    public class FormatTableLoadException : RuntimeException
    {
        public FormatTableLoadException();
        protected FormatTableLoadException(SerializationInfo info, StreamingContext context);
        public FormatTableLoadException(string message);
        public FormatTableLoadException(string message, Exception innerException);
        public Collection<string> Errors { get; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
        protected void SetDefaultErrorRecord();
    }
    public class InitialSessionState
    {
        protected InitialSessionState();
        public virtual InitialSessionStateEntryCollection<SessionStateAssemblyEntry> Assemblies { get; }
        public virtual AuthorizationManager AuthorizationManager { get; set; }
        public virtual InitialSessionStateEntryCollection<SessionStateCommandEntry> Commands { get; }
        public bool DisableFormatUpdates { get; set; }
        public virtual InitialSessionStateEntryCollection<SessionStateVariableEntry> EnvironmentVariables { get; }
        public ExecutionPolicy ExecutionPolicy { get; set; }
        public virtual InitialSessionStateEntryCollection<SessionStateFormatEntry> Formats { get; }
        public PSLanguageMode LanguageMode { get; set; }
        public ReadOnlyCollection<ModuleSpecification> Modules { get; }
        public virtual InitialSessionStateEntryCollection<SessionStateProviderEntry> Providers { get; }
        public virtual HashSet<string> StartupScripts { get; }
        public PSThreadOptions ThreadOptions { get; set; }
        public bool ThrowOnRunspaceOpenError { get; set; }
        public string TranscriptDirectory { get; set; }
        public virtual InitialSessionStateEntryCollection<SessionStateTypeEntry> Types { get; }
        public bool UseFullLanguageModeInDebugger { get; set; }
        public virtual InitialSessionStateEntryCollection<SessionStateVariableEntry> Variables { get; }
        public InitialSessionState Clone();
        public static InitialSessionState Create();
        public static InitialSessionState Create(string snapInName);
        public static InitialSessionState Create(string[] snapInNameCollection, out PSConsoleLoadException warning);
        public static InitialSessionState CreateDefault();
        public static InitialSessionState CreateDefault2();
        public static InitialSessionState CreateFrom(string snapInPath, out PSConsoleLoadException warnings);
        public static InitialSessionState CreateFrom(string[] snapInPathCollection, out PSConsoleLoadException warnings);
        public static InitialSessionState CreateFromSessionConfigurationFile(string path);
        public static InitialSessionState CreateFromSessionConfigurationFile(string path, Func<string, bool> roleVerifier);
        public static InitialSessionState CreateRestricted(SessionCapabilities sessionCapabilities);
        public void ImportPSModule(IEnumerable<ModuleSpecification> modules);
        public void ImportPSModule(string[] name);
        public void ImportPSModulesFromPath(string path);
        public PSSnapInInfo ImportPSSnapIn(string name, out PSSnapInException warning);
    }
    public abstract class InitialSessionStateEntry
    {
        protected InitialSessionStateEntry(string name);
        public PSModuleInfo Module { get; }
        public string Name { get; }
        public PSSnapInInfo PSSnapIn { get; }
        public abstract InitialSessionStateEntry Clone();
    }
    public sealed class InitialSessionStateEntryCollection<T> : IEnumerable, IEnumerable<T> where T : InitialSessionStateEntry
    {
        public InitialSessionStateEntryCollection();
        public InitialSessionStateEntryCollection(IEnumerable<T> items);
        public int Count { get; }
        public T this[int index] { get; }
        public Collection<T> this[string name] { get; }
        public void Add(IEnumerable<T> items);
        public void Add(T item);
        public void Clear();
        public InitialSessionStateEntryCollection<T> Clone();
        public void Remove(string name, object type);
        public void RemoveItem(int index);
        public void RemoveItem(int index, int count);
        public void Reset();
        IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator();
        IEnumerator System.Collections.IEnumerable.GetEnumerator();
    }
    public class InvalidPipelineStateException : SystemException
    {
        public InvalidPipelineStateException();
        public InvalidPipelineStateException(string message);
        public InvalidPipelineStateException(string message, Exception innerException);
        public PipelineState CurrentState { get; }
        public PipelineState ExpectedState { get; }
    }
    public class InvalidRunspacePoolStateException : SystemException
    {
        public InvalidRunspacePoolStateException();
        protected InvalidRunspacePoolStateException(SerializationInfo info, StreamingContext context);
        public InvalidRunspacePoolStateException(string message);
        public InvalidRunspacePoolStateException(string message, Exception innerException);
        public RunspacePoolState CurrentState { get; }
        public RunspacePoolState ExpectedState { get; }
    }
    public class InvalidRunspaceStateException : SystemException
    {
        public InvalidRunspaceStateException();
        protected InvalidRunspaceStateException(SerializationInfo info, StreamingContext context);
        public InvalidRunspaceStateException(string message);
        public InvalidRunspaceStateException(string message, Exception innerException);
        public RunspaceState CurrentState { get; }
        public RunspaceState ExpectedState { get; }
    }
    public class MemberSetData : TypeMemberData
    {
        public MemberSetData(string name, Collection<TypeMemberData> members);
        public bool InheritMembers { get; set; }
        public bool IsHidden { get; set; }
        public Collection<TypeMemberData> Members { get; }
    }
    public sealed class NamedPipeConnectionInfo : RunspaceConnectionInfo
    {
        public NamedPipeConnectionInfo();
        public NamedPipeConnectionInfo(int processId);
        public NamedPipeConnectionInfo(int processId, string appDomainName);
        public NamedPipeConnectionInfo(int processId, string appDomainName, int openTimeout);
        public string AppDomainName { get; set; }
        public override AuthenticationMechanism AuthenticationMechanism { get; set; }
        public override string CertificateThumbprint { get; set; }
        public override string ComputerName { get; set; }
        public override PSCredential Credential { get; set; }
        public int ProcessId { get; set; }
    }
    public sealed class NotePropertyData : TypeMemberData
    {
        public NotePropertyData(string name, object value);
        public bool IsHidden { get; set; }
        public object Value { get; set; }
    }
    public enum OutputBufferingMode
    {
        Block = 2,
        Drop = 1,
        None = 0,
    }
    public abstract class Pipeline : IDisposable
    {
        public CommandCollection Commands { get; }
        public abstract PipelineReader<object> Error { get; }
        public virtual bool HadErrors { get; }
        public abstract PipelineWriter Input { get; }
        public long InstanceId { get; }
        public abstract bool IsNested { get; }
        public abstract PipelineReader<PSObject> Output { get; }
        public abstract PipelineStateInfo PipelineStateInfo { get; }
        public abstract Runspace Runspace { get; }
        public bool SetPipelineSessionState { get; set; }
        public abstract event EventHandler<PipelineStateEventArgs> StateChanged;
        public abstract Collection<PSObject> Connect();
        public abstract void ConnectAsync();
        public abstract Pipeline Copy();
        public void Dispose();
        protected virtual void Dispose(bool disposing);
        public Collection<PSObject> Invoke();
        public abstract Collection<PSObject> Invoke(IEnumerable input);
        public abstract void InvokeAsync();
        public abstract void Stop();
        public abstract void StopAsync();
    }
    public abstract class PipelineReader<T>
    {
        protected PipelineReader();
        public abstract int Count { get; }
        public abstract bool EndOfPipeline { get; }
        public abstract bool IsOpen { get; }
        public abstract int MaxCapacity { get; }
        public abstract WaitHandle WaitHandle { get; }
        public abstract event EventHandler DataReady;
        public abstract void Close();
        public abstract Collection<T> NonBlockingRead();
        public abstract Collection<T> NonBlockingRead(int maxRequested);
        public abstract T Peek();
        public abstract T Read();
        public abstract Collection<T> Read(int count);
        public abstract Collection<T> ReadToEnd();
    }
    public enum PipelineResultTypes
    {
        All = 7,
        Debug = 5,
        Error = 2,
        Information = 6,
        None = 0,
        Null = 8,
        Output = 1,
        Verbose = 4,
        Warning = 3,
    }
    public enum PipelineState
    {
        Completed = 4,
        Disconnected = 6,
        Failed = 5,
        NotStarted = 0,
        Running = 1,
        Stopped = 3,
        Stopping = 2,
    }
    public sealed class PipelineStateEventArgs : EventArgs
    {
        public PipelineStateInfo PipelineStateInfo { get; }
    }
    public sealed class PipelineStateInfo
    {
        public Exception Reason { get; }
        public PipelineState State { get; }
    }
    public abstract class PipelineWriter
    {
        protected PipelineWriter();
        public abstract int Count { get; }
        public abstract bool IsOpen { get; }
        public abstract int MaxCapacity { get; }
        public abstract WaitHandle WaitHandle { get; }
        public abstract void Close();
        public abstract void Flush();
        public abstract int Write(object obj);
        public abstract int Write(object obj, bool enumerateCollection);
    }
    public sealed class PowerShellProcessInstance : IDisposable
    {
        public PowerShellProcessInstance();
        public PowerShellProcessInstance(Version powerShellVersion, PSCredential credential, ScriptBlock initializationScript, bool useWow64);
        public bool HasExited { get; }
        public Process Process { get; }
        public void Dispose();
    }
    public sealed class PropertySetData : TypeMemberData
    {
        public PropertySetData(IEnumerable<string> referencedProperties);
        public bool IsHidden { get; set; }
        public Collection<string> ReferencedProperties { get; }
    }
    public class PSConsoleLoadException : SystemException, IContainsErrorRecord
    {
        public PSConsoleLoadException();
        protected PSConsoleLoadException(SerializationInfo info, StreamingContext context);
        public PSConsoleLoadException(string message);
        public PSConsoleLoadException(string message, Exception innerException);
        public ErrorRecord ErrorRecord { get; }
        public override string Message { get; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
    }
    public sealed class PSSession
    {
        public PSPrimitiveDictionary ApplicationPrivateData { get; }
        public RunspaceAvailability Availability { get; }
        public string ComputerName { get; }
        public TargetMachineType ComputerType { get; set; }
        public string ConfigurationName { get; }
        public string ContainerId { get; }
        public int Id { get; }
        public Guid InstanceId { get; }
        public string Name { get; set; }
        public Runspace Runspace { get; }
        public Nullable<Guid> VMId { get; }
        public string VMName { get; }
        public override string ToString();
    }
    public enum PSSessionConfigurationAccessMode
    {
        Disabled = 0,
        Local = 1,
        Remote = 2,
    }
    public enum PSSessionType
    {
        DefaultRemoteShell = 0,
        Workflow = 1,
    }
    public class PSSnapInException : RuntimeException
    {
        public PSSnapInException();
        protected PSSnapInException(SerializationInfo info, StreamingContext context);
        public PSSnapInException(string message);
        public PSSnapInException(string message, Exception innerException);
        public override ErrorRecord ErrorRecord { get; }
        public override string Message { get; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
    }
    public enum PSThreadOptions
    {
        Default = 0,
        ReuseThread = 2,
        UseCurrentThread = 3,
        UseNewThread = 1,
    }
    public class RemotingDebugRecord : DebugRecord
    {
        public RemotingDebugRecord(string message, OriginInfo originInfo);
        public OriginInfo OriginInfo { get; }
    }
    public class RemotingErrorRecord : ErrorRecord
    {
        public RemotingErrorRecord(ErrorRecord errorRecord, OriginInfo originInfo);
        protected RemotingErrorRecord(SerializationInfo info, StreamingContext context);
        public OriginInfo OriginInfo { get; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
    }
    public class RemotingInformationRecord : InformationRecord
    {
        public RemotingInformationRecord(InformationRecord record, OriginInfo originInfo);
        public OriginInfo OriginInfo { get; }
    }
    public class RemotingProgressRecord : ProgressRecord
    {
        public RemotingProgressRecord(ProgressRecord progressRecord, OriginInfo originInfo);
        public OriginInfo OriginInfo { get; }
    }
    public class RemotingVerboseRecord : VerboseRecord
    {
        public RemotingVerboseRecord(string message, OriginInfo originInfo);
        public OriginInfo OriginInfo { get; }
    }
    public class RemotingWarningRecord : WarningRecord
    {
        public RemotingWarningRecord(string message, OriginInfo originInfo);
        public OriginInfo OriginInfo { get; }
    }
    public abstract class Runspace : IDisposable
    {
        public static bool CanUseDefaultRunspace { get; }
        public abstract RunspaceConnectionInfo ConnectionInfo { get; }
        public virtual Debugger Debugger { get; }
        public static Runspace DefaultRunspace { get; set; }
        public Nullable<DateTime> DisconnectedOn { get; }
        public abstract PSEventManager Events { get; }
        public Nullable<DateTime> ExpiresOn { get; }
        public int Id { get; }
        public abstract InitialSessionState InitialSessionState { get; }
        public Guid InstanceId { get; }
        public abstract JobManager JobManager { get; }
        public string Name { get; set; }
        public abstract RunspaceConnectionInfo OriginalConnectionInfo { get; }
        public abstract RunspaceAvailability RunspaceAvailability { get; protected set; }
        public bool RunspaceIsRemote { get; }
        public abstract RunspaceStateInfo RunspaceStateInfo { get; }
        public SessionStateProxy SessionStateProxy { get; }
        public abstract PSThreadOptions ThreadOptions { get; set; }
        public abstract Version Version { get; }
        public abstract event EventHandler<RunspaceAvailabilityEventArgs> AvailabilityChanged;
        public abstract event EventHandler<RunspaceStateEventArgs> StateChanged;
        public abstract void Close();
        public abstract void CloseAsync();
        public abstract void Connect();
        public abstract void ConnectAsync();
        public abstract Pipeline CreateDisconnectedPipeline();
        public abstract PowerShell CreateDisconnectedPowerShell();
        public abstract Pipeline CreateNestedPipeline();
        public abstract Pipeline CreateNestedPipeline(string command, bool addToHistory);
        public abstract Pipeline CreatePipeline();
        public abstract Pipeline CreatePipeline(string command);
        public abstract Pipeline CreatePipeline(string command, bool addToHistory);
        public abstract void Disconnect();
        public abstract void DisconnectAsync();
        public void Dispose();
        protected virtual void Dispose(bool disposing);
        public abstract PSPrimitiveDictionary GetApplicationPrivateData();
        public abstract RunspaceCapability GetCapabilities();
        public static Runspace GetRunspace(RunspaceConnectionInfo connectionInfo, Guid sessionId, Nullable<Guid> commandId, PSHost host, TypeTable typeTable);
        public static Runspace[] GetRunspaces(RunspaceConnectionInfo connectionInfo);
        public static Runspace[] GetRunspaces(RunspaceConnectionInfo connectionInfo, PSHost host);
        public static Runspace[] GetRunspaces(RunspaceConnectionInfo connectionInfo, PSHost host, TypeTable typeTable);
        protected abstract void OnAvailabilityChanged(RunspaceAvailabilityEventArgs e);
        public abstract void Open();
        public abstract void OpenAsync();
        public virtual void ResetRunspaceState();
        protected void UpdateRunspaceAvailability(RunspaceState runspaceState, bool raiseEvent);
    }
    public enum RunspaceAvailability
    {
        Available = 1,
        AvailableForNestedCommand = 2,
        Busy = 3,
        None = 0,
        RemoteDebug = 4,
    }
    public sealed class RunspaceAvailabilityEventArgs : EventArgs
    {
        public RunspaceAvailability RunspaceAvailability { get; }
    }
    public enum RunspaceCapability
    {
        Default = 0,
        NamedPipeTransport = 2,
        SSHTransport = 8,
        SupportsDisconnect = 1,
        VMSocketTransport = 4,
    }
    public class RunspaceConfigurationAttributeException : SystemException, IContainsErrorRecord
    {
        public RunspaceConfigurationAttributeException();
        protected RunspaceConfigurationAttributeException(SerializationInfo info, StreamingContext context);
        public RunspaceConfigurationAttributeException(string message);
        public RunspaceConfigurationAttributeException(string message, Exception innerException);
        public string AssemblyName { get; }
        public string Error { get; }
        public ErrorRecord ErrorRecord { get; }
        public override string Message { get; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
    }
    public abstract class RunspaceConnectionInfo
    {
        protected const int MaxPort = 65535;
        protected const int MinPort = 0;
        protected RunspaceConnectionInfo();
        public abstract AuthenticationMechanism AuthenticationMechanism { get; set; }
        public int CancelTimeout { get; set; }
        public abstract string CertificateThumbprint { get; set; }
        public abstract string ComputerName { get; set; }
        public abstract PSCredential Credential { get; set; }
        public CultureInfo Culture { get; set; }
        public int IdleTimeout { get; set; }
        public int MaxIdleTimeout { get; }
        public int OpenTimeout { get; set; }
        public int OperationTimeout { get; set; }
        public CultureInfo UICulture { get; set; }
        public virtual void SetSessionOptions(PSSessionOption options);
    }
    public static class RunspaceFactory
    {
        public static Runspace CreateOutOfProcessRunspace(TypeTable typeTable);
        public static Runspace CreateOutOfProcessRunspace(TypeTable typeTable, PowerShellProcessInstance processInstance);
        public static Runspace CreateRunspace();
        public static Runspace CreateRunspace(PSHost host);
        public static Runspace CreateRunspace(PSHost host, InitialSessionState initialSessionState);
        public static Runspace CreateRunspace(PSHost host, RunspaceConnectionInfo connectionInfo);
        public static Runspace CreateRunspace(InitialSessionState initialSessionState);
        public static Runspace CreateRunspace(RunspaceConnectionInfo connectionInfo);
        public static Runspace CreateRunspace(RunspaceConnectionInfo connectionInfo, PSHost host, TypeTable typeTable);
        public static Runspace CreateRunspace(RunspaceConnectionInfo connectionInfo, PSHost host, TypeTable typeTable, PSPrimitiveDictionary applicationArguments);
        public static Runspace CreateRunspace(RunspaceConnectionInfo connectionInfo, PSHost host, TypeTable typeTable, PSPrimitiveDictionary applicationArguments, string name);
        public static RunspacePool CreateRunspacePool();
        public static RunspacePool CreateRunspacePool(int minRunspaces, int maxRunspaces);
        public static RunspacePool CreateRunspacePool(int minRunspaces, int maxRunspaces, PSHost host);
        public static RunspacePool CreateRunspacePool(int minRunspaces, int maxRunspaces, InitialSessionState initialSessionState, PSHost host);
        public static RunspacePool CreateRunspacePool(int minRunspaces, int maxRunspaces, RunspaceConnectionInfo connectionInfo);
        public static RunspacePool CreateRunspacePool(int minRunspaces, int maxRunspaces, RunspaceConnectionInfo connectionInfo, PSHost host);
        public static RunspacePool CreateRunspacePool(int minRunspaces, int maxRunspaces, RunspaceConnectionInfo connectionInfo, PSHost host, TypeTable typeTable);
        public static RunspacePool CreateRunspacePool(int minRunspaces, int maxRunspaces, RunspaceConnectionInfo connectionInfo, PSHost host, TypeTable typeTable, PSPrimitiveDictionary applicationArguments);
        public static RunspacePool CreateRunspacePool(InitialSessionState initialSessionState);
    }
    public class RunspaceOpenModuleLoadException : RuntimeException
    {
        public RunspaceOpenModuleLoadException();
        protected RunspaceOpenModuleLoadException(SerializationInfo info, StreamingContext context);
        public RunspaceOpenModuleLoadException(string message);
        public RunspaceOpenModuleLoadException(string message, Exception innerException);
        public PSDataCollection<ErrorRecord> ErrorRecords { get; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
    }
    public sealed class RunspacePool : IDisposable
    {
        public TimeSpan CleanupInterval { get; set; }
        public RunspaceConnectionInfo ConnectionInfo { get; }
        public InitialSessionState InitialSessionState { get; }
        public Guid InstanceId { get; }
        public bool IsDisposed { get; }
        public RunspacePoolAvailability RunspacePoolAvailability { get; }
        public RunspacePoolStateInfo RunspacePoolStateInfo { get; }
        public PSThreadOptions ThreadOptions { get; set; }
        public event EventHandler<RunspacePoolStateChangedEventArgs> StateChanged;
        public IAsyncResult BeginClose(AsyncCallback callback, object state);
        public IAsyncResult BeginConnect(AsyncCallback callback, object state);
        public IAsyncResult BeginDisconnect(AsyncCallback callback, object state);
        public IAsyncResult BeginOpen(AsyncCallback callback, object state);
        public void Close();
        public void Connect();
        public Collection<PowerShell> CreateDisconnectedPowerShells();
        public void Disconnect();
        public void Dispose();
        public void EndClose(IAsyncResult asyncResult);
        public void EndConnect(IAsyncResult asyncResult);
        public void EndDisconnect(IAsyncResult asyncResult);
        public void EndOpen(IAsyncResult asyncResult);
        public PSPrimitiveDictionary GetApplicationPrivateData();
        public int GetAvailableRunspaces();
        public RunspacePoolCapability GetCapabilities();
        public int GetMaxRunspaces();
        public int GetMinRunspaces();
        public static RunspacePool[] GetRunspacePools(RunspaceConnectionInfo connectionInfo);
        public static RunspacePool[] GetRunspacePools(RunspaceConnectionInfo connectionInfo, PSHost host);
        public static RunspacePool[] GetRunspacePools(RunspaceConnectionInfo connectionInfo, PSHost host, TypeTable typeTable);
        public void Open();
        public bool SetMaxRunspaces(int maxRunspaces);
        public bool SetMinRunspaces(int minRunspaces);
    }
    public enum RunspacePoolAvailability
    {
        Available = 1,
        Busy = 2,
        None = 0,
    }
    public enum RunspacePoolCapability
    {
        Default = 0,
        SupportsDisconnect = 1,
    }
    public enum RunspacePoolState
    {
        BeforeOpen = 0,
        Broken = 5,
        Closed = 3,
        Closing = 4,
        Connecting = 8,
        Disconnected = 7,
        Disconnecting = 6,
        Opened = 2,
        Opening = 1,
    }
    public sealed class RunspacePoolStateChangedEventArgs : EventArgs
    {
        public RunspacePoolStateInfo RunspacePoolStateInfo { get; }
    }
    public enum RunspaceState
    {
        BeforeOpen = 0,
        Broken = 5,
        Closed = 3,
        Closing = 4,
        Connecting = 8,
        Disconnected = 7,
        Disconnecting = 6,
        Opened = 2,
        Opening = 1,
    }
    public sealed class RunspaceStateEventArgs : EventArgs
    {
        public RunspaceStateInfo RunspaceStateInfo { get; }
    }
    public sealed class RunspaceStateInfo
    {
        public Exception Reason { get; }
        public RunspaceState State { get; }
        public override string ToString();
    }
    public sealed class ScriptMethodData : TypeMemberData
    {
        public ScriptMethodData(string name, ScriptBlock scriptToInvoke);
        public ScriptBlock Script { get; set; }
    }
    public sealed class ScriptPropertyData : TypeMemberData
    {
        public ScriptPropertyData(string name, ScriptBlock getScriptBlock);
        public ScriptPropertyData(string name, ScriptBlock getScriptBlock, ScriptBlock setScriptBlock);
        public ScriptBlock GetScriptBlock { get; set; }
        public bool IsHidden { get; set; }
        public ScriptBlock SetScriptBlock { get; set; }
    }
    public sealed class SessionStateAliasEntry : SessionStateCommandEntry
    {
        public SessionStateAliasEntry(string name, string definition);
        public SessionStateAliasEntry(string name, string definition, string description);
        public SessionStateAliasEntry(string name, string definition, string description, ScopedItemOptions options);
        public string Definition { get; }
        public string Description { get; }
        public ScopedItemOptions Options { get; }
        public override InitialSessionStateEntry Clone();
    }
    public sealed class SessionStateApplicationEntry : SessionStateCommandEntry
    {
        public SessionStateApplicationEntry(string path);
        public string Path { get; }
        public override InitialSessionStateEntry Clone();
    }
    public sealed class SessionStateAssemblyEntry : InitialSessionStateEntry
    {
        public SessionStateAssemblyEntry(string name);
        public SessionStateAssemblyEntry(string name, string fileName);
        public string FileName { get; }
        public override InitialSessionStateEntry Clone();
    }
    public sealed class SessionStateCmdletEntry : SessionStateCommandEntry
    {
        public SessionStateCmdletEntry(string name, Type implementingType, string helpFileName);
        public string HelpFileName { get; }
        public Type ImplementingType { get; }
        public override InitialSessionStateEntry Clone();
    }
    public abstract class SessionStateCommandEntry : ConstrainedSessionStateEntry
    {
        protected SessionStateCommandEntry(string name);
        protected internal SessionStateCommandEntry(string name, SessionStateEntryVisibility visibility);
        public CommandTypes CommandType { get; }
    }
    public sealed class SessionStateFormatEntry : InitialSessionStateEntry
    {
        public SessionStateFormatEntry(ExtendedTypeDefinition typeDefinition);
        public SessionStateFormatEntry(FormatTable formattable);
        public SessionStateFormatEntry(string fileName);
        public string FileName { get; }
        public ExtendedTypeDefinition FormatData { get; }
        public FormatTable Formattable { get; }
        public override InitialSessionStateEntry Clone();
    }
    public sealed class SessionStateFunctionEntry : SessionStateCommandEntry
    {
        public SessionStateFunctionEntry(string name, string definition);
        public SessionStateFunctionEntry(string name, string definition, ScopedItemOptions options, string helpFile);
        public SessionStateFunctionEntry(string name, string definition, string helpFile);
        public string Definition { get; }
        public string HelpFile { get; }
        public ScopedItemOptions Options { get; }
        public override InitialSessionStateEntry Clone();
    }
    public sealed class SessionStateProviderEntry : ConstrainedSessionStateEntry
    {
        public SessionStateProviderEntry(string name, Type implementingType, string helpFileName);
        public string HelpFileName { get; }
        public Type ImplementingType { get; }
        public override InitialSessionStateEntry Clone();
    }
    public class SessionStateProxy
    {
        public virtual List<string> Applications { get; }
        public virtual DriveManagementIntrinsics Drive { get; }
        public virtual CommandInvocationIntrinsics InvokeCommand { get; }
        public virtual ProviderIntrinsics InvokeProvider { get; }
        public virtual PSLanguageMode LanguageMode { get; set; }
        public virtual PSModuleInfo Module { get; }
        public virtual PathIntrinsics Path { get; }
        public virtual CmdletProviderManagementIntrinsics Provider { get; }
        public virtual PSVariableIntrinsics PSVariable { get; }
        public virtual List<string> Scripts { get; }
        public virtual object GetVariable(string name);
        public virtual void SetVariable(string name, object value);
    }
    public sealed class SessionStateScriptEntry : SessionStateCommandEntry
    {
        public SessionStateScriptEntry(string path);
        public string Path { get; }
        public override InitialSessionStateEntry Clone();
    }
    public sealed class SessionStateTypeEntry : InitialSessionStateEntry
    {
        public SessionStateTypeEntry(TypeData typeData, bool isRemove);
        public SessionStateTypeEntry(TypeTable typeTable);
        public SessionStateTypeEntry(string fileName);
        public string FileName { get; }
        public bool IsRemove { get; }
        public TypeData TypeData { get; }
        public TypeTable TypeTable { get; }
        public override InitialSessionStateEntry Clone();
    }
    public sealed class SessionStateVariableEntry : ConstrainedSessionStateEntry
    {
        public SessionStateVariableEntry(string name, object value, string description);
        public SessionStateVariableEntry(string name, object value, string description, ScopedItemOptions options);
        public SessionStateVariableEntry(string name, object value, string description, ScopedItemOptions options, Attribute attribute);
        public SessionStateVariableEntry(string name, object value, string description, ScopedItemOptions options, Collection<Attribute> attributes);
        public Collection<Attribute> Attributes { get; }
        public string Description { get; }
        public ScopedItemOptions Options { get; }
        public object Value { get; }
        public override InitialSessionStateEntry Clone();
    }
    public sealed class SSHConnectionInfo : RunspaceConnectionInfo
    {
        public SSHConnectionInfo(string userName, string computerName, string keyFilePath);
        public SSHConnectionInfo(string userName, string computerName, string keyFilePath, int port);
        public override AuthenticationMechanism AuthenticationMechanism { get; set; }
        public override string CertificateThumbprint { get; set; }
        public override string ComputerName { get; set; }
        public override PSCredential Credential { get; set; }
        public string UserName { get; }
    }
    public enum TargetMachineType
    {
        Container = 2,
        RemoteMachine = 0,
        VirtualMachine = 1,
    }
    public sealed class TypeData
    {
        public TypeData(string typeName);
        public TypeData(Type type);
        public string DefaultDisplayProperty { get; set; }
        public PropertySetData DefaultDisplayPropertySet { get; set; }
        public PropertySetData DefaultKeyPropertySet { get; set; }
        public bool InheritPropertySerializationSet { get; set; }
        public bool IsOverride { get; set; }
        public Dictionary<string, TypeMemberData> Members { get; }
        public PropertySetData PropertySerializationSet { get; set; }
        public uint SerializationDepth { get; set; }
        public string SerializationMethod { get; set; }
        public string StringSerializationSource { get; set; }
        public TypeMemberData StringSerializationSourceProperty { get; set; }
        public Type TargetTypeForDeserialization { get; set; }
        public Type TypeAdapter { get; set; }
        public Type TypeConverter { get; set; }
        public string TypeName { get; }
        public TypeData Copy();
    }
    public abstract class TypeMemberData
    {
        public string Name { get; protected set; }
    }
    public sealed class TypeTable
    {
        public TypeTable(IEnumerable<string> typeFiles);
        public void AddType(TypeData typeData);
        public TypeTable Clone(bool unshared);
        public static List<string> GetDefaultTypeFiles();
        public static TypeTable LoadDefaultTypeFiles();
        public void RemoveType(string typeName);
    }
    public class TypeTableLoadException : RuntimeException
    {
        public TypeTableLoadException();
        protected TypeTableLoadException(SerializationInfo info, StreamingContext context);
        public TypeTableLoadException(string message);
        public TypeTableLoadException(string message, Exception innerException);
        public Collection<string> Errors { get; }
        public override void GetObjectData(SerializationInfo info, StreamingContext context);
        protected void SetDefaultErrorRecord();
    }
    public sealed class VMConnectionInfo : RunspaceConnectionInfo
    {
        public override AuthenticationMechanism AuthenticationMechanism { get; set; }
        public override string CertificateThumbprint { get; set; }
        public override string ComputerName { get; set; }
        public string ConfigurationName { get; set; }
        public override PSCredential Credential { get; set; }
        public Guid VMGuid { get; set; }
    }
    public sealed class WSManConnectionInfo : RunspaceConnectionInfo
    {
        public const string HttpScheme = "http";
        public const string HttpsScheme = "https";
        public WSManConnectionInfo();
        public WSManConnectionInfo(bool useSsl, string computerName, int port, string appName, string shellUri, PSCredential credential);
        public WSManConnectionInfo(bool useSsl, string computerName, int port, string appName, string shellUri, PSCredential credential, int openTimeout);
        public WSManConnectionInfo(PSSessionType configurationType);
        public WSManConnectionInfo(string scheme, string computerName, int port, string appName, string shellUri, PSCredential credential);
        public WSManConnectionInfo(string scheme, string computerName, int port, string appName, string shellUri, PSCredential credential, int openTimeout);
        public WSManConnectionInfo(Uri uri);
        public WSManConnectionInfo(Uri uri, string shellUri, PSCredential credential);
        public WSManConnectionInfo(Uri uri, string shellUri, string certificateThumbprint);
        public string AppName { get; set; }
        public override AuthenticationMechanism AuthenticationMechanism { get; set; }
        public override string CertificateThumbprint { get; set; }
        public override string ComputerName { get; set; }
        public Uri ConnectionUri { get; set; }
        public override PSCredential Credential { get; set; }
        public bool EnableNetworkAccess { get; set; }
        public bool IncludePortInSPN { get; set; }
        public int MaxConnectionRetryCount { get; set; }
        public int MaximumConnectionRedirectionCount { get; set; }
        public Nullable<int> MaximumReceivedDataSizePerCommand { get; set; }
        public Nullable<int> MaximumReceivedObjectSize { get; set; }
        public bool NoEncryption { get; set; }
        public bool NoMachineProfile { get; set; }
        public OutputBufferingMode OutputBufferingMode { get; set; }
        public int Port { get; set; }
        public ProxyAccessType ProxyAccessType { get; set; }
        public AuthenticationMechanism ProxyAuthentication { get; set; }
        public PSCredential ProxyCredential { get; set; }
        public string Scheme { get; set; }
        public string ShellUri { get; set; }
        public bool SkipCACheck { get; set; }
        public bool SkipCNCheck { get; set; }
        public bool SkipRevocationCheck { get; set; }
        public bool UseCompression { get; set; }
        public bool UseUTF16 { get; set; }
        public WSManConnectionInfo Copy();
        public override void SetSessionOptions(PSSessionOption options);
    }
}
namespace System.Management.Automation.Tracing
{
    public abstract class BaseChannelWriter : IDisposable
    {
        protected BaseChannelWriter();
        public virtual PowerShellTraceKeywords Keywords { get; set; }
        public virtual void Dispose();
        public virtual bool TraceCritical(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args);
        public virtual bool TraceDebug(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args);
        public virtual bool TraceError(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args);
        public virtual bool TraceInformational(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args);
        public virtual bool TraceLogAlways(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args);
        public virtual bool TraceVerbose(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args);
        public virtual bool TraceWarning(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args);
    }
    public delegate void CallbackNoParameter();
    public delegate void CallbackWithState(object state);
    public abstract class EtwActivity
    {
        protected EtwActivity();
        public bool IsEnabled { get; }
        protected virtual Guid ProviderId { get; }
        protected virtual EventDescriptor TransferEvent { get; }
        public static event EventHandler<EtwEventArgs> EventWritten;
        public void Correlate();
        public AsyncCallback Correlate(AsyncCallback callback);
        public CallbackNoParameter Correlate(CallbackNoParameter callback);
        public CallbackWithState Correlate(CallbackWithState callback);
        public void CorrelateWithActivity(Guid parentActivityId);
        public static Guid CreateActivityId();
        public static Guid GetActivityId();
        public bool IsProviderEnabled(byte levels, long keywords);
        public static bool SetActivityId(Guid activityId);
        protected void WriteEvent(EventDescriptor ed, params object[] payload);
    }
    public sealed class EtwEvent : Attribute
    {
        public EtwEvent(long eventId);
        public long EventId { get; }
    }
    public class EtwEventArgs : EventArgs
    {
        public EtwEventArgs(EventDescriptor descriptor, bool success, object[] payload);
        public EventDescriptor Descriptor { get; }
        public object[] Payload { get; }
        public bool Success { get; }
    }
    public class EtwEventCorrelator : IEtwEventCorrelator
    {
        public EtwEventCorrelator(EventProvider transferProvider, EventDescriptor transferEvent);
        public Guid CurrentActivityId { get; set; }
        public IEtwActivityReverter StartActivity();
        public IEtwActivityReverter StartActivity(Guid relatedActivityId);
    }
    public interface IEtwActivityReverter : IDisposable
    {
        void RevertCurrentActivityId();
    }
    public interface IEtwEventCorrelator
    {
        Guid CurrentActivityId { get; set; }
        IEtwActivityReverter StartActivity();
        IEtwActivityReverter StartActivity(Guid relatedActivityId);
    }
    public sealed class NullWriter : BaseChannelWriter
    {
        public static BaseChannelWriter Instance { get; }
    }
    public sealed class PowerShellChannelWriter : BaseChannelWriter
    {
        public override PowerShellTraceKeywords Keywords { get; set; }
        public override void Dispose();
        public override bool TraceCritical(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args);
        public override bool TraceDebug(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args);
        public override bool TraceError(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args);
        public override bool TraceInformational(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args);
        public override bool TraceLogAlways(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args);
        public override bool TraceVerbose(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args);
        public override bool TraceWarning(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args);
    }
    public enum PowerShellTraceChannel
    {
        Analytic = 17,
        Debug = 18,
        None = 0,
        Operational = 16,
    }
    public enum PowerShellTraceEvent
    {
        AnalyticTransferEventRunspacePool = 12039,
        AppDomainUnhandledException = 32777,
        AppDomainUnhandledExceptionAnalytic = 32775,
        AppName = 12034,
        ComputerName = 12035,
        ErrorRecord = 45057,
        Exception = 45058,
        HostNameResolve = 4097,
        Job = 45060,
        LoadingPSCustomShellAssembly = 32865,
        LoadingPSCustomShellType = 32866,
        None = 0,
        OperationalTransferEventRunspacePool = 8196,
        PerformanceTrackConsoleStartupStart = 40961,
        PerformanceTrackConsoleStartupStop = 40962,
        PowerShellObject = 45059,
        ReceivedRemotingFragment = 32867,
        ReportContext = 32851,
        ReportOperationComplete = 32852,
        RunspaceConstructor = 8193,
        RunspacePoolConstructor = 8194,
        RunspacePoolOpen = 8195,
        RunspacePort = 12033,
        Scheme = 12036,
        SchemeResolve = 4098,
        SentRemotingFragment = 32868,
        SerializerDepthOverride = 28675,
        SerializerEnumerationFailed = 28679,
        SerializerMaxDepthWhenSerializing = 28682,
        SerializerModeOverride = 28676,
        SerializerPropertyGetterFailed = 28678,
        SerializerScriptPropertyWithoutRunspace = 28677,
        SerializerSpecificPropertyMissing = 28684,
        SerializerToStringFailed = 28680,
        SerializerWorkflowLoadFailure = 28674,
        SerializerWorkflowLoadSuccess = 28673,
        SerializerXmlExceptionWhenDeserializing = 28683,
        ServerClientReceiveRequest = 32856,
        ServerCloseOperation = 32857,
        ServerCreateCommandSession = 32853,
        ServerCreateRemoteSession = 32850,
        ServerReceivedData = 32855,
        ServerSendData = 32849,
        ServerStopCommand = 32854,
        ShellResolve = 4099,
        TestAnalytic = 12037,
        TraceMessage = 45061,
        TraceMessage2 = 49153,
        TraceMessageGuid = 49154,
        TraceWSManConnectionInfo = 45062,
        TransportError = 32784,
        TransportErrorAnalytic = 32776,
        TransportReceivedObject = 32769,
        UriRedirection = 32805,
        WSManCloseCommand = 32801,
        WSManCloseCommandCallbackReceived = 32802,
        WSManCloseShell = 32787,
        WSManCloseShellCallbackReceived = 32788,
        WSManConnectionInfoDump = 12038,
        WSManCreateCommand = 32793,
        WSManCreateCommandCallbackReceived = 32800,
        WSManCreateShell = 32785,
        WSManCreateShellCallbackReceived = 32786,
        WSManPluginShutdown = 32869,
        WSManReceiveShellOutputExtended = 32791,
        WSManReceiveShellOutputExtendedCallbackReceived = 32792,
        WSManSendShellInputExtended = 32789,
        WSManSendShellInputExtendedCallbackReceived = 32790,
        WSManSignal = 32803,
        WSManSignalCallbackReceived = 32804,
    }
    public enum PowerShellTraceKeywords : ulong
    {
        Cmdlets = (ulong)32,
        Host = (ulong)16,
        ManagedPlugIn = (ulong)256,
        None = (ulong)0,
        Pipeline = (ulong)2,
        Protocol = (ulong)4,
        Runspace = (ulong)1,
        Serializer = (ulong)64,
        Session = (ulong)128,
        Transport = (ulong)8,
        UseAlwaysAnalytic = (ulong)4611686018427387904,
        UseAlwaysDebug = (ulong)2305843009213693952,
        UseAlwaysOperational = (ulong)9223372036854775808,
    }
    public enum PowerShellTraceLevel
    {
        Critical = 1,
        Debug = 20,
        Error = 2,
        Informational = 4,
        LogAlways = 0,
        Verbose = 5,
        Warning = 3,
    }
    public enum PowerShellTraceOperationCode
    {
        Close = 11,
        Connect = 12,
        Constructor = 16,
        Create = 15,
        Disconnect = 13,
        Dispose = 17,
        EventHandler = 18,
        Exception = 19,
        Method = 20,
        Negotiate = 14,
        None = 0,
        Open = 10,
        Receive = 22,
        Send = 21,
        SerializationSettings = 24,
        WinDCStart = 28,
        WinDCStop = 29,
        WinExtension = 30,
        WinInfo = 25,
        WinReply = 31,
        WinResume = 32,
        WinStart = 26,
        WinStop = 27,
        WinSuspend = 33,
        WorkflowLoad = 23,
    }
    public sealed class PowerShellTraceSource : IDisposable
    {
        public BaseChannelWriter AnalyticChannel { get; }
        public BaseChannelWriter DebugChannel { get; }
        public PowerShellTraceKeywords Keywords { get; }
        public BaseChannelWriter OperationalChannel { get; }
        public PowerShellTraceTask Task { get; set; }
        public void Dispose();
        public bool TraceErrorRecord(ErrorRecord errorRecord);
        public bool TraceException(Exception exception);
        public bool TraceJob(Job job);
        public bool TracePowerShellObject(PSObject powerShellObject);
        public bool TraceWSManConnectionInfo(WSManConnectionInfo connectionInfo);
        public void WriteISEDebuggerStepIntoEvent(params object[] args);
        public void WriteISEDebuggerStepOutEvent(params object[] args);
        public void WriteISEDebuggerStepOverEvent(params object[] args);
        public void WriteISEDisableAllBreakpointsEvent(params object[] args);
        public void WriteISEDisableBreakpointEvent(params object[] args);
        public void WriteISEEnableAllBreakpointsEvent(params object[] args);
        public void WriteISEEnableBreakpointEvent(params object[] args);
        public void WriteISEExecuteScriptEvent(params object[] args);
        public void WriteISEExecuteSelectionEvent(params object[] args);
        public void WriteISEHitBreakpointEvent(params object[] args);
        public void WriteISERemoveAllBreakpointsEvent(params object[] args);
        public void WriteISERemoveBreakpointEvent(params object[] args);
        public void WriteISEResumeDebuggerEvent(params object[] args);
        public void WriteISESetBreakpointEvent(params object[] args);
        public void WriteISEStopCommandEvent(params object[] args);
        public void WriteISEStopDebuggerEvent(params object[] args);
        public bool WriteMessage(string message);
        public bool WriteMessage(string message, Guid instanceId);
        public bool WriteMessage(string message1, string message2);
        public void WriteMessage(string className, string methodName, Guid workflowId, Job job, string message, params string[] parameters);
        public void WriteMessage(string className, string methodName, Guid workflowId, string activityName, Guid activityId, string message, params string[] parameters);
        public void WriteMessage(string className, string methodName, Guid workflowId, string message, params string[] parameters);
        public void WriteScheduledJobCompleteEvent(params object[] args);
        public void WriteScheduledJobErrorEvent(params object[] args);
        public void WriteScheduledJobStartEvent(params object[] args);
    }
    public static class PowerShellTraceSourceFactory
    {
        public static PowerShellTraceSource GetTraceSource();
        public static PowerShellTraceSource GetTraceSource(PowerShellTraceTask task);
        public static PowerShellTraceSource GetTraceSource(PowerShellTraceTask task, PowerShellTraceKeywords keywords);
    }
    public enum PowerShellTraceTask
    {
        CreateRunspace = 1,
        ExecuteCommand = 2,
        None = 0,
        PowerShellConsoleStartup = 4,
        Serialization = 3,
    }
    public sealed class Tracer : EtwActivity
    {
        public const long KeywordAll = (long)4294967295;
        public const byte LevelCritical = (byte)1;
        public const byte LevelError = (byte)2;
        public const byte LevelInformational = (byte)4;
        public const byte LevelVerbose = (byte)5;
        public const byte LevelWarning = (byte)3;
        public Tracer();
        protected override Guid ProviderId { get; }
        protected override EventDescriptor TransferEvent { get; }
        public void AbortingWorkflowExecution(Guid workflowId, string reason);
        public void ActivityExecutionFinished(string activityName);
        public void ActivityExecutionQueued(Guid workflowId, string activityName);
        public void ActivityExecutionStarted(string activityName, string activityTypeName);
        public void BeginContainerParentJobExecution(Guid containerParentJobInstanceId);
        public void BeginCreateNewJob(Guid trackingId);
        public void BeginJobLogic(Guid workflowJobJobInstanceId);
        public void BeginProxyChildJobEventHandler(Guid proxyChildJobInstanceId);
        public void BeginProxyJobEventHandler(Guid proxyJobInstanceId);
        public void BeginProxyJobExecution(Guid proxyJobInstanceId);
        public void BeginRunGarbageCollection();
        public void BeginStartWorkflowApplication(Guid trackingId);
        public void BeginWorkflowExecution(Guid workflowJobJobInstanceId);
        public void CancellingWorkflowExecution(Guid workflowId);
        public void ChildWorkflowJobAddition(Guid workflowJobInstanceId, Guid containerParentJobInstanceId);
        public void DebugMessage(Exception exception);
        public void DebugMessage(string message);
        public void EndContainerParentJobExecution(Guid containerParentJobInstanceId);
        public void EndCreateNewJob(Guid trackingId);
        public void EndJobLogic(Guid workflowJobJobInstanceId);
        public void EndpointDisabled(string endpointName, string disabledBy);
        public void EndpointEnabled(string endpointName, string enabledBy);
        public void EndpointModified(string endpointName, string modifiedBy);
        public void EndpointRegistered(string endpointName, string endpointType, string registeredBy);
        public void EndpointUnregistered(string endpointName, string unregisteredBy);
        public void EndProxyChildJobEventHandler(Guid proxyChildJobInstanceId);
        public void EndProxyJobEventHandler(Guid proxyJobInstanceId);
        public void EndProxyJobExecution(Guid proxyJobInstanceId);
        public void EndRunGarbageCollection();
        public void EndStartWorkflowApplication(Guid trackingId);
        public void EndWorkflowExecution(Guid workflowJobJobInstanceId);
        public void ErrorImportingWorkflowFromXaml(Guid workflowId, string errorDescription);
        public void ForcedWorkflowShutdownError(Guid workflowId, string errorDescription);
        public void ForcedWorkflowShutdownFinished(Guid workflowId);
        public void ForcedWorkflowShutdownStarted(Guid workflowId);
        public static string GetExceptionString(Exception exception);
        public void ImportedWorkflowFromXaml(Guid workflowId, string xamlFile);
        public void ImportingWorkflowFromXaml(Guid workflowId, string xamlFile);
        public void JobCreationComplete(Guid jobId, Guid workflowId);
        public void JobError(int jobId, Guid workflowId, string errorDescription);
        public void JobRemoved(Guid parentJobId, Guid childJobId, Guid workflowId);
        public void JobRemoveError(Guid parentJobId, Guid childJobId, Guid workflowId, string error);
        public void JobStateChanged(int jobId, Guid workflowId, string newState, string oldState);
        public void LoadingWorkflowForExecution(Guid workflowId);
        public void OutOfProcessRunspaceStarted(string command);
        public void ParameterSplattingWasPerformed(string parameters, string computers);
        public void ParentJobCreated(Guid jobId);
        public void PersistenceStoreMaxSizeReached();
        public void PersistingWorkflow(Guid workflowId, string persistPath);
        public void ProxyJobRemoteJobAssociation(Guid proxyJobInstanceId, Guid containerParentJobInstanceId);
        public void RemoveJobStarted(Guid jobId);
        public void RunspaceAvailabilityChanged(string runspaceId, string availability);
        public void RunspaceStateChanged(string runspaceId, string newState, string oldState);
        public void TrackingGuidContainerParentJobCorrelation(Guid trackingId, Guid containerParentJobInstanceId);
        public void UnloadingWorkflow(Guid workflowId);
        public void WorkflowActivityExecutionFailed(Guid workflowId, string activityName, string failureDescription);
        public void WorkflowActivityValidated(Guid workflowId, string activityDisplayName, string activityType);
        public void WorkflowActivityValidationFailed(Guid workflowId, string activityDisplayName, string activityType);
        public void WorkflowCleanupPerformed(Guid workflowId);
        public void WorkflowDeletedFromDisk(Guid workflowId, string path);
        public void WorkflowEngineStarted(string endpointName);
        public void WorkflowExecutionAborted(Guid workflowId);
        public void WorkflowExecutionCancelled(Guid workflowId);
        public void WorkflowExecutionError(Guid workflowId, string errorDescription);
        public void WorkflowExecutionFinished(Guid workflowId);
        public void WorkflowExecutionStarted(Guid workflowId, string managedNodes);
        public void WorkflowJobCreated(Guid parentJobId, Guid childJobId, Guid childWorkflowId);
        public void WorkflowLoadedForExecution(Guid workflowId);
        public void WorkflowLoadedFromDisk(Guid workflowId, string path);
        public void WorkflowManagerCheckpoint(string checkpointPath, string configProviderId, string userName, string path);
        public void WorkflowPersisted(Guid workflowId);
        public void WorkflowPluginRequestedToShutdown(string endpointName);
        public void WorkflowPluginRestarted(string endpointName);
        public void WorkflowPluginStarted(string endpointName, string user, string hostingMode, string protocol, string configuration);
        public void WorkflowQuotaViolated(string endpointName, string configName, string allowedValue, string valueInQuestion);
        public void WorkflowResumed(Guid workflowId);
        public void WorkflowResuming(Guid workflowId);
        public void WorkflowRunspacePoolCreated(Guid workflowId, string managedNode);
        public void WorkflowStateChanged(Guid workflowId, string newState, string oldState);
        public void WorkflowUnloaded(Guid workflowId);
        public void WorkflowValidationError(Guid workflowId);
        public void WorkflowValidationFinished(Guid workflowId);
        public void WorkflowValidationStarted(Guid workflowId);
        public void WriteTransferEvent(Guid currentActivityId, Guid parentActivityId);
    }
}
