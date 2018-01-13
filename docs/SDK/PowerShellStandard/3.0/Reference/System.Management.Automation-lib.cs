/*

First cut at removing apis that we might want to deprecate or not support

CIM - cim support
COM_APARTMENT_STATE - Missing from .Net, could come back
COMPONENT_MODEL - Missing from .Net, could come back (not very important though)
CONVERT_THROUGH_STRING - api used from ps1xml
DEFAULT_PARAM_DICTIONARY - odd api used by remoting
FILTER_INFO - would like to fold into FunctionInfo
FORMAT_API - incomplete in V3, bring back in 5.1
NEED_MMI_REF_ASSEM - missing types from Microsoft.Management.Infrastructure (cim)
PERF_COUNTERS - probably Windows specfic
PS1XML_SUPPORT - api used from ps1xml - not necessarily designed as a proper api
SYSTEM_DIAGNOSTICS - missing apis - probably not needed
TIMEZONE - missing .Net api
TRANSACTIONS - missing .Net apis, probably kill off anyway
V1_PIPELINE_API - want to remove, used internally but should move to V2 api
V2_PARSER_API - want to remove
WIN32_REGISTRY - Windows specific
WORKFLOW - missing apis, probably remove
WSMAN - Windows only
XML_SERIALIZATION - missing apis

*/

namespace Microsoft.PowerShell {
#if PS1XML_SUPPORT
  public static class AdapterCodeMethods {
    public static string ConvertDNWithBinaryToString(System.Management.Automation.PSObject deInstance, System.Management.Automation.PSObject dnWithBinaryInstance) { return default(string); }
    public static long ConvertLargeIntegerToInt64(System.Management.Automation.PSObject deInstance, System.Management.Automation.PSObject largeIntegerInstance) { return default(long); }
#endif
  public sealed class DeserializingTypeConverter : System.Management.Automation.PSTypeConverter {
    public DeserializingTypeConverter() { }
     
    public override bool CanConvertFrom(System.Management.Automation.PSObject sourceValue, System.Type destinationType) { return default(bool); }
    public override bool CanConvertFrom(object sourceValue, System.Type destinationType) { return default(bool); }
    public override bool CanConvertTo(System.Management.Automation.PSObject sourceValue, System.Type destinationType) { return default(bool); }
    public override bool CanConvertTo(object sourceValue, System.Type destinationType) { return default(bool); }
    public override object ConvertFrom(System.Management.Automation.PSObject sourceValue, System.Type destinationType, System.IFormatProvider formatProvider, bool ignoreCase) { return default(object); }
    public override object ConvertFrom(object sourceValue, System.Type destinationType, System.IFormatProvider formatProvider, bool ignoreCase) { return default(object); }
    public override object ConvertTo(System.Management.Automation.PSObject sourceValue, System.Type destinationType, System.IFormatProvider formatProvider, bool ignoreCase) { return default(object); }
    public override object ConvertTo(object sourceValue, System.Type destinationType, System.IFormatProvider formatProvider, bool ignoreCase) { return default(object); }
    public static System.Guid GetFormatViewDefinitionInstanceId(System.Management.Automation.PSObject instance) { return default(System.Guid); }
    public static uint GetParameterSetMetadataFlags(System.Management.Automation.PSObject instance) { return default(uint); }
  }
  public enum ExecutionPolicy {
    AllSigned = 2,
    Bypass = 4,
    Default = 3,
    RemoteSigned = 1,
    Restricted = 3,
    Undefined = 5,
    Unrestricted = 0,
  }
  public enum ExecutionPolicyScope {
    CurrentUser = 1,
    LocalMachine = 2,
    MachinePolicy = 4,
    Process = 0,
    UserPolicy = 3,
  }
  public sealed class PSAuthorizationManager : System.Management.Automation.AuthorizationManager {
    public PSAuthorizationManager(string shellId) : base (default(string)) { }
     
    protected internal override bool ShouldRun(System.Management.Automation.CommandInfo commandInfo, System.Management.Automation.CommandOrigin origin, System.Management.Automation.Host.PSHost host, out System.Exception reason) { reason = default(System.Exception); return default(bool); }
  }
#if PS1XML_SUPPORT
  public static class ToStringCodeMethods {
    public static string PropertyValueCollection(System.Management.Automation.PSObject instance) { return default(string); }
    public static string Type(System.Management.Automation.PSObject instance) { return default(string); }
    public static string XmlNode(System.Management.Automation.PSObject instance) { return default(string); }
    public static string XmlNodeList(System.Management.Automation.PSObject instance) { return default(string); }
  }
#endif
}
#if CIM
namespace Microsoft.PowerShell.Cim {
  public sealed class CimInstanceAdapter : System.Management.Automation.PSPropertyAdapter {
    public CimInstanceAdapter() { }
     
    public override System.Collections.ObjectModel.Collection<System.Management.Automation.PSAdaptedProperty> GetProperties(object baseObject) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSAdaptedProperty>); }
    public override System.Management.Automation.PSAdaptedProperty GetProperty(object baseObject, string propertyName) { return default(System.Management.Automation.PSAdaptedProperty); }
    public override string GetPropertyTypeName(System.Management.Automation.PSAdaptedProperty adaptedProperty) { return default(string); }
    public override object GetPropertyValue(System.Management.Automation.PSAdaptedProperty adaptedProperty) { return default(object); }
    public override System.Collections.ObjectModel.Collection<string> GetTypeNameHierarchy(object baseObject) { return default(System.Collections.ObjectModel.Collection<string>); }
    public override bool IsGettable(System.Management.Automation.PSAdaptedProperty adaptedProperty) { return default(bool); }
    public override bool IsSettable(System.Management.Automation.PSAdaptedProperty adaptedProperty) { return default(bool); }
    public override void SetPropertyValue(System.Management.Automation.PSAdaptedProperty adaptedProperty, object value) { }
  }
}
namespace Microsoft.PowerShell.Cmdletization {
  public enum BehaviorOnNoMatch {
    Default = 0,
    ReportErrors = 1,
    SilentlyContinue = 2,
  }
  public abstract class CmdletAdapter<TObjectInstance> where TObjectInstance : class {
    protected CmdletAdapter() { }
     
    public string ClassName { get { return default(string); } }
    public string ClassVersion { get { return default(string); } }
    public System.Management.Automation.PSCmdlet Cmdlet { get { return default(System.Management.Automation.PSCmdlet); } }
    public System.Version ModuleVersion { get { return default(System.Version); } }
    public System.Collections.Generic.IDictionary<string, string> PrivateData { get { return default(System.Collections.Generic.IDictionary<string, string>); } }
     
    public virtual void BeginProcessing() { }
    public virtual void EndProcessing() { }
    public virtual Microsoft.PowerShell.Cmdletization.QueryBuilder GetQueryBuilder() { return default(Microsoft.PowerShell.Cmdletization.QueryBuilder); }
    public void Initialize(System.Management.Automation.PSCmdlet cmdlet, string className, string classVersion, System.Version moduleVersion, System.Collections.Generic.IDictionary<string, string> privateData) { }
    public virtual void ProcessRecord(TObjectInstance objectInstance, Microsoft.PowerShell.Cmdletization.MethodInvocationInfo methodInvocationInfo, bool passThru) { }
    public virtual void ProcessRecord(Microsoft.PowerShell.Cmdletization.MethodInvocationInfo methodInvocationInfo) { }
    public virtual void ProcessRecord(Microsoft.PowerShell.Cmdletization.QueryBuilder query) { }
    public virtual void ProcessRecord(Microsoft.PowerShell.Cmdletization.QueryBuilder query, Microsoft.PowerShell.Cmdletization.MethodInvocationInfo methodInvocationInfo, bool passThru) { }
    public virtual void StopProcessing() { }
  }
  public sealed class MethodInvocationInfo {
    public MethodInvocationInfo(string name, System.Collections.Generic.IEnumerable<Microsoft.PowerShell.Cmdletization.MethodParameter> parameters, Microsoft.PowerShell.Cmdletization.MethodParameter returnValue) { }
     
    public string MethodName { get { return default(string); } }
    public System.Collections.ObjectModel.KeyedCollection<string, Microsoft.PowerShell.Cmdletization.MethodParameter> Parameters { get { return default(System.Collections.ObjectModel.KeyedCollection<string, Microsoft.PowerShell.Cmdletization.MethodParameter>); } }
    public Microsoft.PowerShell.Cmdletization.MethodParameter ReturnValue { get { return default(Microsoft.PowerShell.Cmdletization.MethodParameter); } }
     
  }
  public sealed class MethodParameter {
    public MethodParameter() { }
     
    public Microsoft.PowerShell.Cmdletization.MethodParameterBindings Bindings { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(Microsoft.PowerShell.Cmdletization.MethodParameterBindings); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public bool IsValuePresent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public System.Type ParameterType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Type); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public string ParameterTypeName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public object Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(object); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
     
  }
  [System.FlagsAttribute]
  public enum MethodParameterBindings {
    Error = 4,
    In = 1,
    Out = 2,
  }
  public abstract class QueryBuilder {
    protected QueryBuilder() { }
     
    public virtual void AddQueryOption(string optionName, object optionValue) { }
    public virtual void ExcludeByProperty(string propertyName, System.Collections.IEnumerable excludedPropertyValues, bool wildcardsEnabled, Microsoft.PowerShell.Cmdletization.BehaviorOnNoMatch behaviorOnNoMatch) { }
    public virtual void FilterByAssociatedInstance(object associatedInstance, string associationName, string sourceRole, string resultRole, Microsoft.PowerShell.Cmdletization.BehaviorOnNoMatch behaviorOnNoMatch) { }
    public virtual void FilterByMaxPropertyValue(string propertyName, object maxPropertyValue, Microsoft.PowerShell.Cmdletization.BehaviorOnNoMatch behaviorOnNoMatch) { }
    public virtual void FilterByMinPropertyValue(string propertyName, object minPropertyValue, Microsoft.PowerShell.Cmdletization.BehaviorOnNoMatch behaviorOnNoMatch) { }
    public virtual void FilterByProperty(string propertyName, System.Collections.IEnumerable allowedPropertyValues, bool wildcardsEnabled, Microsoft.PowerShell.Cmdletization.BehaviorOnNoMatch behaviorOnNoMatch) { }
  }
}
#endif
#if XML_SERIALIZATION
namespace Microsoft.PowerShell.Cmdletization.Xml {
  [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17361")]
  [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.microsoft.com/cmdlets-over-objects/2009/11")]
  public enum ConfirmImpact {
    High = 3,
    Low = 1,
    Medium = 2,
    None = 0,
  }
  [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17361")]
  [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.microsoft.com/cmdlets-over-objects/2009/11", IncludeInSchema=false)]
  public enum ItemsChoiceType {
    ExcludeQuery = 0,
    MaxValueQuery = 1,
    MinValueQuery = 2,
    RegularQuery = 3,
  }
}
#endif
namespace Microsoft.PowerShell.Commands {
// Remove attributes and methods and properties
  [System.Management.Automation.CmdletAttribute("Add", "History", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113279")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(Microsoft.PowerShell.Commands.HistoryInfo)})]
  public class AddHistoryCommand : System.Management.Automation.PSCmdlet {
    public AddHistoryCommand() { }
     
    [System.Management.Automation.ParameterAttribute(Position=0, ValueFromPipeline=true)]
    public System.Management.Automation.PSObject[] InputObject { get { return default(System.Management.Automation.PSObject[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Passthru { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    protected override void BeginProcessing() { }
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.AliasInfo)}, ProviderCmdlet="Copy-Item")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.AliasInfo)}, ProviderCmdlet="Get-ChildItem")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.AliasInfo)}, ProviderCmdlet="New-Item")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.AliasInfo)}, ProviderCmdlet="Rename-Item")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.AliasInfo)}, ProviderCmdlet="Set-Item")]
  [System.Management.Automation.Provider.CmdletProviderAttribute("Alias", (System.Management.Automation.Provider.ProviderCapabilities)(16))]
  public sealed class AliasProvider : Microsoft.PowerShell.Commands.SessionStateProviderBase {
    public const string ProviderName = "Alias";
     
    public AliasProvider() { }
     
    protected override System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo> InitializeDefaultDrives() { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo>); }
    protected override object NewItemDynamicParameters(string path, string type, object newItemValue) { return default(object); }
    protected override object SetItemDynamicParameters(string path, object value) { return default(object); }
  }
  public class AliasProviderDynamicParameters {
    public AliasProviderDynamicParameters() { }
     
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.ScopedItemOptions Options { get { return default(System.Management.Automation.ScopedItemOptions); } set { } }
     
  }
  public class AlternateStreamData {
    public AlternateStreamData() { }
     
    public string FileName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public long Length { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(long); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public string Stream { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
     
  }
  [System.Management.Automation.CmdletAttribute("Clear", "History", SupportsShouldProcess=true, DefaultParameterSetName="IDParameter", HelpUri="http://go.microsoft.com/fwlink/?LinkID=135199")]
  public class ClearHistoryCommand : System.Management.Automation.PSCmdlet {
    public ClearHistoryCommand() { }
     
    [System.Management.Automation.ParameterAttribute(ParameterSetName="CommandLineParameter", HelpMessage="Specifies the name of a command in the session history")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string[] CommandLine { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=false, Position=1, HelpMessage="Clears the specified number of history entries")]
    [System.Management.Automation.ValidateRangeAttribute(1, 2147483647)]
    public int Count { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="IDParameter", Position=0, HelpMessage="Specifies the ID of a command in the session history.Clear history clears only the specified command")]
    [System.Management.Automation.ValidateRangeAttribute(1, 2147483647)]
    public int[] Id { get { return default(int[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=false, HelpMessage="Specifies whether new entries to be cleared or the default old ones.")]
    public System.Management.Automation.SwitchParameter Newest { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    protected override void BeginProcessing() { }
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Connect", "PSSession", SupportsShouldProcess=true, DefaultParameterSetName="Name", HelpUri="http://go.microsoft.com/fwlink/?LinkID=210604", RemotingCapability=(System.Management.Automation.RemotingCapability)(3))]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.Runspaces.PSSession)})]
  public class ConnectPSSessionCommand : Microsoft.PowerShell.Commands.PSRunspaceCmdlet, System.IDisposable {
    public ConnectPSSessionCommand() { }
     
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUriGuid")]
    public System.Management.Automation.SwitchParameter AllowRedirection { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerNameGuid")]
    public string ApplicationName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerNameGuid")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUriGuid")]
    public System.Management.Automation.Runspaces.AuthenticationMechanism Authentication { get { return default(System.Management.Automation.Runspaces.AuthenticationMechanism); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerNameGuid")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUriGuid")]
    public string CertificateThumbprint { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "Cn"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerNameGuid", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position=0, ParameterSetName="ComputerName", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override string[] ComputerName { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerNameGuid")]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="ConnectionUriGuid")]
    public string ConfigurationName { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "URI", "CU"})]
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=true, ValueFromPipelineByPropertyName=true, ParameterSetName="ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=true, ValueFromPipelineByPropertyName=true, ParameterSetName="ConnectionUriGuid")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Uri[] ConnectionUri { get { return default(System.Uri[]); } set { } }
    [System.Management.Automation.CredentialAttribute]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerNameGuid")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUriGuid")]
    public System.Management.Automation.PSCredential Credential { get { return default(System.Management.Automation.PSCredential); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerNameGuid", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUriGuid", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="InstanceId", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public override System.Guid[] InstanceId { get { return default(System.Guid[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Name", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override string[] Name { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerNameGuid")]
    [System.Management.Automation.ValidateRangeAttribute(1, 65535)]
    public int Port { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=true, ValueFromPipelineByPropertyName=true, ValueFromPipeline=true, ParameterSetName="Session")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Management.Automation.Runspaces.PSSession[] Session { get { return default(System.Management.Automation.Runspaces.PSSession[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerNameGuid")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUriGuid")]
    public System.Management.Automation.Remoting.PSSessionOption SessionOption { get { return default(System.Management.Automation.Remoting.PSSessionOption); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerNameGuid")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUriGuid")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Id")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="InstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Name")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Session")]
    public int ThrottleLimit { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerNameGuid")]
    public System.Management.Automation.SwitchParameter UseSSL { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    protected override void BeginProcessing() { }
    public void Dispose() { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  public abstract class ConsoleCmdletsBase : System.Management.Automation.PSCmdlet {
    protected ConsoleCmdletsBase() { }
  }
  [System.Management.Automation.CmdletAttribute("Disable", "PSRemoting", SupportsShouldProcess=true, ConfirmImpact=(System.Management.Automation.ConfirmImpact)(3), HelpUri="http://go.microsoft.com/fwlink/?LinkID=144298")]
  public sealed class DisablePSRemotingCommand : System.Management.Automation.PSCmdlet {
    public DisablePSRemotingCommand() { }
     
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    protected override void BeginProcessing() { }
    protected override void EndProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Disable", "PSSessionConfiguration", SupportsShouldProcess=true, ConfirmImpact=(System.Management.Automation.ConfirmImpact)(3), HelpUri="http://go.microsoft.com/fwlink/?LinkID=144299")]
  public sealed class DisablePSSessionConfigurationCommand : System.Management.Automation.PSCmdlet {
    public DisablePSSessionConfigurationCommand() { }
     
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position=0, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string[] Name { get { return default(string[]); } set { } }
     
    protected override void BeginProcessing() { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Disconnect", "PSSession", SupportsShouldProcess=true, DefaultParameterSetName="Session", HelpUri="http://go.microsoft.com/fwlink/?LinkID=210605", RemotingCapability=(System.Management.Automation.RemotingCapability)(3))]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.Runspaces.PSSession)})]
  public class DisconnectPSSessionCommand : Microsoft.PowerShell.Commands.PSRunspaceCmdlet, System.IDisposable {
    public DisconnectPSSessionCommand() { }
     
    public override string[] ComputerName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string[]); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Id")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="InstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Name")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Session")]
    [System.Management.Automation.ValidateRangeAttribute(0, 2147483647)]
    public int IdleTimeoutSec { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Id")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="InstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Name")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Session")]
    public System.Management.Automation.Runspaces.OutputBufferingMode OutputBufferingMode { get { return default(System.Management.Automation.Runspaces.OutputBufferingMode); } set { } }
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=true, ValueFromPipelineByPropertyName=true, ValueFromPipeline=true, ParameterSetName="Session")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Management.Automation.Runspaces.PSSession[] Session { get { return default(System.Management.Automation.Runspaces.PSSession[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Id")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="InstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Name")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Session")]
    public int ThrottleLimit { get { return default(int); } set { } }
     
    protected override void BeginProcessing() { }
    public void Dispose() { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Enable", "PSRemoting", SupportsShouldProcess=true, ConfirmImpact=(System.Management.Automation.ConfirmImpact)(3), HelpUri="http://go.microsoft.com/fwlink/?LinkID=144300")]
  public sealed class EnablePSRemotingCommand : System.Management.Automation.PSCmdlet {
    public EnablePSRemotingCommand() { }
     
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter SkipNetworkProfileCheck { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    protected override void BeginProcessing() { }
    protected override void EndProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Enable", "PSSessionConfiguration", SupportsShouldProcess=true, ConfirmImpact=(System.Management.Automation.ConfirmImpact)(3), HelpUri="http://go.microsoft.com/fwlink/?LinkID=144301")]
  public sealed class EnablePSSessionConfigurationCommand : System.Management.Automation.PSCmdlet {
    public EnablePSSessionConfigurationCommand() { }
     
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position=0, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string[] Name { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string SecurityDescriptorSddl { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter SkipNetworkProfileCheck { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    protected override void BeginProcessing() { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Enter", "PSSession", DefaultParameterSetName="ComputerName", HelpUri="http://go.microsoft.com/fwlink/?LinkID=135210", RemotingCapability=(System.Management.Automation.RemotingCapability)(3))]
  public class EnterPSSessionCommand : Microsoft.PowerShell.Commands.PSRemotingBaseCmdlet {
    public EnterPSSessionCommand() { }
     
    [System.Management.Automation.AliasAttribute(new string[]{ "Cn"})]
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerName")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public new string ComputerName { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "URI", "CU"})]
    [System.Management.Automation.ParameterAttribute(Position=1, ValueFromPipelineByPropertyName=true, ParameterSetName="Uri")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public new System.Uri ConnectionUri { get { return default(System.Uri); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Uri")]
    public System.Management.Automation.SwitchParameter EnableNetworkAccess { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position=0, ValueFromPipelineByPropertyName=true, ParameterSetName="Id")]
    [System.Management.Automation.ValidateNotNullAttribute]
    public int Id { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="InstanceId")]
    [System.Management.Automation.ValidateNotNullAttribute]
    public System.Guid InstanceId { get { return default(System.Guid); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="Name")]
    public string Name { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(Position=0, ValueFromPipelineByPropertyName=true, ValueFromPipeline=true, ParameterSetName="Session")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public new System.Management.Automation.Runspaces.PSSession Session { get { return default(System.Management.Automation.Runspaces.PSSession); } set { } }
    public new int ThrottleLimit { get { return default(int); } set { } }
     
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.Provider.CmdletProviderAttribute("Environment", (System.Management.Automation.Provider.ProviderCapabilities)(16))]
  public sealed class EnvironmentProvider : Microsoft.PowerShell.Commands.SessionStateProviderBase {
    public const string ProviderName = "Environment";
     
    public EnvironmentProvider() { }
     
    protected override System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo> InitializeDefaultDrives() { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo>); }
  }
  [System.Management.Automation.CmdletAttribute("Exit", "PSSession", HelpUri="http://go.microsoft.com/fwlink/?LinkID=135212")]
  public class ExitPSSessionCommand : Microsoft.PowerShell.Commands.PSRemotingCmdlet {
    public ExitPSSessionCommand() { }
     
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Export", "Console", SupportsShouldProcess=true, HelpUri="http://go.microsoft.com/fwlink/?LinkID=113298")]
  public sealed class ExportConsoleCommand : Microsoft.PowerShell.Commands.ConsoleCmdletsBase {
    public ExportConsoleCommand() { }
     
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "NoOverwrite"})]
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter NoClobber { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "PSPath"})]
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=false, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    public string Path { get { return default(string); } set { } }
     
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Export", "ModuleMember", HelpUri="http://go.microsoft.com/fwlink/?LinkID=141551")]
  public sealed class ExportModuleMemberCommand : System.Management.Automation.PSCmdlet {
    public ExportModuleMemberCommand() { }
     
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public string[] Alias { get { return default(string[]); } set { } }
    [System.Management.Automation.AllowEmptyCollectionAttribute]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    public string[] Cmdlet { get { return default(string[]); } set { } }
    [System.Management.Automation.AllowEmptyCollectionAttribute]
    [System.Management.Automation.ParameterAttribute(ValueFromPipeline=true, ValueFromPipelineByPropertyName=true, Position=0)]
    public string[] Function { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public string[] Variable { get { return default(string[]); } set { } }
     
    protected override void ProcessRecord() { }
  }
  public class FileSystemClearContentDynamicParameters {
    public FileSystemClearContentDynamicParameters() { }
     
    [System.Management.Automation.ParameterAttribute]
    public string Stream { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
     
  }
  public enum FileSystemCmdletProviderEncoding {
    Ascii = 8,
    BigEndianUnicode = 4,
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
  public class FileSystemContentDynamicParametersBase {
    public FileSystemContentDynamicParametersBase() { }
     
    [System.Management.Automation.ParameterAttribute]
    public Microsoft.PowerShell.Commands.FileSystemCmdletProviderEncoding Encoding { get { return default(Microsoft.PowerShell.Commands.FileSystemCmdletProviderEncoding); } set { } }
    public System.Text.Encoding EncodingType { get { return default(System.Text.Encoding); } }
    [System.Management.Automation.ParameterAttribute]
    public string Stream { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public bool UsingByteEncoding { get { return default(bool); } }
    public bool WasStreamTypeSpecified { get { return default(bool); } }
     
  }
  public class FileSystemContentReaderDynamicParameters : Microsoft.PowerShell.Commands.FileSystemContentDynamicParametersBase {
    public FileSystemContentReaderDynamicParameters() { }
     
    [System.Management.Automation.ParameterAttribute]
    public string Delimiter { get { return default(string); } set { } }
    public bool DelimiterSpecified { get { return default(bool); } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Raw { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Wait { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
  }
  public class FileSystemContentWriterDynamicParameters : Microsoft.PowerShell.Commands.FileSystemContentDynamicParametersBase {
    public FileSystemContentWriterDynamicParameters() { }
  }
  public class FileSystemItemProviderDynamicParameters {
    public FileSystemItemProviderDynamicParameters() { }
     
    [System.Management.Automation.ParameterAttribute]
    public System.Nullable<System.DateTime> NewerThan { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Nullable<System.DateTime>); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Nullable<System.DateTime> OlderThan { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Nullable<System.DateTime>); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
     
  }
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(bool), typeof(string), typeof(System.DateTime), typeof(System.IO.FileInfo), typeof(System.IO.DirectoryInfo)}, ProviderCmdlet="Get-ItemProperty")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(bool), typeof(string), typeof(System.IO.FileInfo), typeof(System.IO.DirectoryInfo)}, ProviderCmdlet="Get-Item")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(string), typeof(System.IO.FileInfo)}, ProviderCmdlet="New-Item")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(string), typeof(System.Management.Automation.PathInfo)}, ProviderCmdlet="Resolve-Path")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Byte), typeof(string)}, ProviderCmdlet="Get-Content")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.IO.FileInfo), typeof(System.IO.DirectoryInfo)}, ProviderCmdlet="Get-ChildItem")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.IO.FileInfo)}, ProviderCmdlet="Get-Item")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.PathInfo)}, ProviderCmdlet="Push-Location")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Security.AccessControl.FileSecurity), typeof(System.Security.AccessControl.DirectorySecurity)}, ProviderCmdlet="Get-Acl")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Security.AccessControl.FileSecurity)}, ProviderCmdlet="Set-Acl")]
  [System.Management.Automation.Provider.CmdletProviderAttribute("FileSystem", (System.Management.Automation.Provider.ProviderCapabilities)(52))]
  public sealed class FileSystemProvider : System.Management.Automation.Provider.NavigationCmdletProvider, System.Management.Automation.Provider.ICmdletProviderSupportsHelp, System.Management.Automation.Provider.IContentCmdletProvider, System.Management.Automation.Provider.IPropertyCmdletProvider, System.Management.Automation.Provider.ISecurityDescriptorCmdletProvider {
    public const string ProviderName = "FileSystem";
     
    public FileSystemProvider() { }
     
    public void ClearContent(string path) { }
    public object ClearContentDynamicParameters(string path) { return default(object); }
    public void ClearProperty(string path, System.Collections.ObjectModel.Collection<string> propertiesToClear) { }
    public object ClearPropertyDynamicParameters(string path, System.Collections.ObjectModel.Collection<string> propertiesToClear) { return default(object); }
    protected override bool ConvertPath(string path, string filter, ref string updatedPath, ref string updatedFilter) { return default(bool); }
    protected override void CopyItem(string path, string destinationPath, bool recurse) { }
    protected override void GetChildItems(string path, bool recurse) { }
    protected override object GetChildItemsDynamicParameters(string path, bool recurse) { return default(object); }
    protected override string GetChildName(string path) { return default(string); }
    protected override void GetChildNames(string path, System.Management.Automation.ReturnContainers returnContainers) { }
    protected override object GetChildNamesDynamicParameters(string path) { return default(object); }
    public System.Management.Automation.Provider.IContentReader GetContentReader(string path) { return default(System.Management.Automation.Provider.IContentReader); }
    public object GetContentReaderDynamicParameters(string path) { return default(object); }
    public System.Management.Automation.Provider.IContentWriter GetContentWriter(string path) { return default(System.Management.Automation.Provider.IContentWriter); }
    public object GetContentWriterDynamicParameters(string path) { return default(object); }
    public string GetHelpMaml(string helpItemName, string path) { return default(string); }
    protected override void GetItem(string path) { }
    protected override object GetItemDynamicParameters(string path) { return default(object); }
    protected override string GetParentPath(string path, string root) { return default(string); }
    public void GetProperty(string path, System.Collections.ObjectModel.Collection<string> providerSpecificPickList) { }
    public object GetPropertyDynamicParameters(string path, System.Collections.ObjectModel.Collection<string> providerSpecificPickList) { return default(object); }
    public void GetSecurityDescriptor(string path, System.Security.AccessControl.AccessControlSections sections) { }
    protected override bool HasChildItems(string path) { return default(bool); }
    protected override System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo> InitializeDefaultDrives() { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo>); }
    protected override void InvokeDefaultAction(string path) { }
    protected override bool IsItemContainer(string path) { return default(bool); }
    protected override bool IsValidPath(string path) { return default(bool); }
    protected override bool ItemExists(string path) { return default(bool); }
    protected override object ItemExistsDynamicParameters(string path) { return default(object); }
    public static string Mode(System.Management.Automation.PSObject instance) { return default(string); }
    protected override void MoveItem(string path, string destination) { }
    protected override System.Management.Automation.PSDriveInfo NewDrive(System.Management.Automation.PSDriveInfo drive) { return default(System.Management.Automation.PSDriveInfo); }
    protected override void NewItem(string path, string type, object value) { }
    public System.Security.AccessControl.ObjectSecurity NewSecurityDescriptorFromPath(string path, System.Security.AccessControl.AccessControlSections sections) { return default(System.Security.AccessControl.ObjectSecurity); }
    public System.Security.AccessControl.ObjectSecurity NewSecurityDescriptorOfType(string type, System.Security.AccessControl.AccessControlSections sections) { return default(System.Security.AccessControl.ObjectSecurity); }
    protected override string NormalizeRelativePath(string path, string basePath) { return default(string); }
    protected override System.Management.Automation.PSDriveInfo RemoveDrive(System.Management.Automation.PSDriveInfo drive) { return default(System.Management.Automation.PSDriveInfo); }
    protected override void RemoveItem(string path, bool recurse) { }
    protected override object RemoveItemDynamicParameters(string path, bool recurse) { return default(object); }
    protected override void RenameItem(string path, string newName) { }
    public void SetProperty(string path, System.Management.Automation.PSObject propertyToSet) { }
    public object SetPropertyDynamicParameters(string path, System.Management.Automation.PSObject propertyValue) { return default(object); }
    public void SetSecurityDescriptor(string path, System.Security.AccessControl.ObjectSecurity securityDescriptor) { }
    protected override System.Management.Automation.ProviderInfo Start(System.Management.Automation.ProviderInfo providerInfo) { return default(System.Management.Automation.ProviderInfo); }
  }
  public class FileSystemProviderGetItemDynamicParameters {
    public FileSystemProviderGetItemDynamicParameters() { }
     
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string[] Stream { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string[]); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
     
  }
  public class FileSystemProviderRemoveItemDynamicParameters {
    public FileSystemProviderRemoveItemDynamicParameters() { }
     
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string[] Stream { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string[]); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
     
  }
  [System.Management.Automation.CmdletAttribute("ForEach", "Object", SupportsShouldProcess=true, DefaultParameterSetName="ScriptBlockSet", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113300", RemotingCapability=(System.Management.Automation.RemotingCapability)(0))]
  public sealed class ForEachObjectCommand : System.Management.Automation.PSCmdlet {
    public ForEachObjectCommand() { }
     
    [System.Management.Automation.AliasAttribute(new string[]{ "Args"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="PropertyAndMethodSet", ValueFromRemainingArguments=true)]
    public object[] ArgumentList { get { return default(object[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ScriptBlockSet")]
    public System.Management.Automation.ScriptBlock Begin { get { return default(System.Management.Automation.ScriptBlock); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ScriptBlockSet")]
    public System.Management.Automation.ScriptBlock End { get { return default(System.Management.Automation.ScriptBlock); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipeline=true, ParameterSetName="PropertyAndMethodSet")]
    [System.Management.Automation.ParameterAttribute(ValueFromPipeline=true, ParameterSetName="ScriptBlockSet")]
    public System.Management.Automation.PSObject InputObject { get { return default(System.Management.Automation.PSObject); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="PropertyAndMethodSet")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string MemberName { get { return default(string); } set { } }
    [System.Management.Automation.AllowEmptyCollectionAttribute]
    [System.Management.Automation.AllowNullAttribute]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="ScriptBlockSet")]
    public System.Management.Automation.ScriptBlock[] Process { get { return default(System.Management.Automation.ScriptBlock[]); } set { } }
    [System.Management.Automation.AllowEmptyCollectionAttribute]
    [System.Management.Automation.AllowNullAttribute]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ScriptBlockSet", ValueFromRemainingArguments=true)]
    public System.Management.Automation.ScriptBlock[] RemainingScripts { get { return default(System.Management.Automation.ScriptBlock[]); } set { } }
     
    protected override void BeginProcessing() { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Format", "Default")]
  public class FormatDefaultCommand : Microsoft.PowerShell.Commands.Internal.Format.FrontEndCommandBase {
    public FormatDefaultCommand() { }
  }
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.FunctionInfo)}, ProviderCmdlet="Copy-Item")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.FunctionInfo)}, ProviderCmdlet="Get-ChildItem")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.FunctionInfo)}, ProviderCmdlet="Get-Item")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.FunctionInfo)}, ProviderCmdlet="New-Item")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.FunctionInfo)}, ProviderCmdlet="Rename-Item")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.FunctionInfo)}, ProviderCmdlet="Set-Item")]
  [System.Management.Automation.Provider.CmdletProviderAttribute("Function", (System.Management.Automation.Provider.ProviderCapabilities)(16))]
  public sealed class FunctionProvider : Microsoft.PowerShell.Commands.SessionStateProviderBase {
    public const string ProviderName = "Function";
     
    public FunctionProvider() { }
     
    protected override System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo> InitializeDefaultDrives() { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo>); }
    protected override object NewItemDynamicParameters(string path, string type, object newItemValue) { return default(object); }
    protected override object SetItemDynamicParameters(string path, object value) { return default(object); }
  }
  public class FunctionProviderDynamicParameters {
    public FunctionProviderDynamicParameters() { }
     
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.ScopedItemOptions Options { get { return default(System.Management.Automation.ScopedItemOptions); } set { } }
     
  }
  [System.Management.Automation.CmdletAttribute("Get", "Command", DefaultParameterSetName="CmdletSet", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113309")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]
  {
      typeof(System.Management.Automation.AliasInfo),
      typeof(System.Management.Automation.ApplicationInfo),
      typeof(System.Management.Automation.FunctionInfo),
      typeof(System.Management.Automation.CmdletInfo),
      typeof(System.Management.Automation.ExternalScriptInfo),
#if FILTER_INFO
      typeof(System.Management.Automation.FilterInfo),
#endif
#if WORKFLOW
      typeof(System.Management.Automation.WorkflowInfo),
#endif
      typeof(string)
  })]
  public sealed class GetCommandCommand : System.Management.Automation.PSCmdlet {
    public GetCommandCommand() { }
     
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    public System.Management.Automation.SwitchParameter All { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "Args"})]
    [System.Management.Automation.AllowEmptyCollectionAttribute]
    [System.Management.Automation.AllowNullAttribute]
    [System.Management.Automation.ParameterAttribute(Position=1, ValueFromRemainingArguments=true)]
    public object[] ArgumentList { get { return default(object[]); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "Type"})]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="AllCommandSet")]
    public System.Management.Automation.CommandTypes CommandType { get { return default(System.Management.Automation.CommandTypes); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    public System.Management.Automation.SwitchParameter ListImported { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "PSSnapin"})]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    public string[] Module { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Position=0, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true, ParameterSetName="AllCommandSet")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string[] Name { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="CmdletSet")]
    public string[] Noun { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string[] ParameterName { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Management.Automation.PSTypeName[] ParameterType { get { return default(System.Management.Automation.PSTypeName[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    public System.Management.Automation.SwitchParameter Syntax { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    public int TotalCount { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="CmdletSet")]
    public string[] Verb { get { return default(string[]); } set { } }
     
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
  }
  public static class GetHelpCodeMethods {
    public static string GetHelpUri(System.Management.Automation.PSObject commandInfoPSObject) { return default(string); }
  }
  [System.Management.Automation.CmdletAttribute("Get", "Help", DefaultParameterSetName="AllUsersView", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113316")]
  public sealed class GetHelpCommand : System.Management.Automation.PSCmdlet {
    public GetHelpCommand() { }
     
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateSetAttribute(new string[]{ "Alias", "Cmdlet", "Provider", "General", "FAQ", "Glossary", "HelpFile", "ScriptCommand", "Function", "Filter", "ExternalScript", "All", "DefaultHelp", "Workflow"}, IgnoreCase=true)]
    public string[] Category { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string[] Component { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="DetailedView", Mandatory=true)]
    public System.Management.Automation.SwitchParameter Detailed { set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Examples", Mandatory=true)]
    public System.Management.Automation.SwitchParameter Examples { set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="AllUsersView")]
    public System.Management.Automation.SwitchParameter Full { set { } }
    [System.Management.Automation.ParameterAttribute]
    public string[] Functionality { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Position=0, ValueFromPipelineByPropertyName=true)]
    public string Name { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Online", Mandatory=true)]
    public System.Management.Automation.SwitchParameter Online { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Parameters", Mandatory=true)]
    public string Parameter { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string Path { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string[] Role { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ShowWindow", Mandatory=true)]
    public System.Management.Automation.SwitchParameter ShowWindow { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    protected override void BeginProcessing() { }
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Get", "History", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113317")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(Microsoft.PowerShell.Commands.HistoryInfo)})]
  public class GetHistoryCommand : System.Management.Automation.PSCmdlet {
    public GetHistoryCommand() { }
     
    [System.Management.Automation.ParameterAttribute(Position=1)]
    [System.Management.Automation.ValidateRangeAttribute(0, 32767)]
    public int Count { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(Position=0, ValueFromPipeline=true)]
    [System.Management.Automation.ValidateRangeAttribute((long)1, (long)9223372036854775807)]
    public long[] Id { get { return default(long[]); } set { } }
     
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Get", "Job", DefaultParameterSetName="SessionIdParameterSet", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113328")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.Job)})]
  public class GetJobCommand : Microsoft.PowerShell.Commands.JobCmdletBase {
    public GetJobCommand() { }
     
    [System.Management.Automation.ParameterAttribute(ParameterSetName="CommandParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="InstanceIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="NameParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="SessionIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="StateParameterSet")]
    public System.DateTime After { get { return default(System.DateTime); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="CommandParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="InstanceIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="NameParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="SessionIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="StateParameterSet")]
    public System.DateTime Before { get { return default(System.DateTime); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="CommandParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="InstanceIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="NameParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="SessionIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="StateParameterSet")]
    public System.Management.Automation.JobState ChildJobState { get { return default(System.Management.Automation.JobState); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="CommandParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="InstanceIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="NameParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="SessionIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="StateParameterSet")]
    public bool HasMoreData { get { return default(bool); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, Position=0, ParameterSetName="SessionIdParameterSet")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override int[] Id { get { return default(int[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="CommandParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="InstanceIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="NameParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="SessionIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="StateParameterSet")]
    public System.Management.Automation.SwitchParameter IncludeChildJob { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="CommandParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="InstanceIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="NameParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="SessionIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="StateParameterSet")]
    public int Newest { get { return default(int); } set { } }
     
    protected System.Collections.Generic.List<System.Management.Automation.Job> FindJobs() { return default(System.Collections.Generic.List<System.Management.Automation.Job>); }
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Get", "Module", DefaultParameterSetName="Loaded", HelpUri="http://go.microsoft.com/fwlink/?LinkID=141552")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.PSModuleInfo)})]
  public sealed class GetModuleCommand : Microsoft.PowerShell.Commands.ModuleCmdletBase, System.IDisposable {
    public GetModuleCommand() { }
     
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Available")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Loaded")]
    public System.Management.Automation.SwitchParameter All { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.SwitchParameter); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="CimSession", Mandatory=false)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string CimNamespace { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="CimSession", Mandatory=false)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public System.Uri CimResourceUri { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Uri); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
#if NEED_MMI_REF_ASSEM
    [System.Management.Automation.ParameterAttribute(ParameterSetName="CimSession", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public Microsoft.Management.Infrastructure.CimSession CimSession { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(Microsoft.Management.Infrastructure.CimSession); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
#endif
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Available", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="CimSession")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="PsSession")]
    public System.Management.Automation.SwitchParameter ListAvailable { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.SwitchParameter); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Available", ValueFromPipeline=true, Position=0)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="CimSession", ValueFromPipeline=true, Position=0)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Loaded", ValueFromPipeline=true, Position=0)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="PsSession", ValueFromPipeline=true, Position=0)]
    public string[] Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string[]); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="PsSession", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public System.Management.Automation.Runspaces.PSSession PSSession { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Runspaces.PSSession); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Available")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="CimSession")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="PsSession")]
    public System.Management.Automation.SwitchParameter Refresh { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.SwitchParameter); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
     
    public void Dispose() { }
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Get", "PSSession", DefaultParameterSetName="Name", HelpUri="http://go.microsoft.com/fwlink/?LinkID=135219", RemotingCapability=(System.Management.Automation.RemotingCapability)(3))]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.Runspaces.PSSession)})]
  public class GetPSSessionCommand : Microsoft.PowerShell.Commands.PSRunspaceCmdlet, System.IDisposable {
    public GetPSSessionCommand() { }
     
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUriInstanceId")]
    public System.Management.Automation.SwitchParameter AllowRedirection { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerName")]
    public string ApplicationName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUriInstanceId")]
    public System.Management.Automation.Runspaces.AuthenticationMechanism Authentication { get { return default(System.Management.Automation.Runspaces.AuthenticationMechanism); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUriInstanceId")]
    public string CertificateThumbprint { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "Cn"})]
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=true, ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=true, ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerName")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override string[] ComputerName { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="ConnectionUriInstanceId")]
    public string ConfigurationName { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "URI", "CU"})]
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=true, ValueFromPipelineByPropertyName=true, ParameterSetName="ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=true, ValueFromPipelineByPropertyName=true, ParameterSetName="ConnectionUriInstanceId")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Uri[] ConnectionUri { get { return default(System.Uri[]); } set { } }
    [System.Management.Automation.CredentialAttribute]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUriInstanceId")]
    public System.Management.Automation.PSCredential Credential { get { return default(System.Management.Automation.PSCredential); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerInstanceId", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUriInstanceId", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="InstanceId")]
    [System.Management.Automation.ValidateNotNullAttribute]
    public override System.Guid[] InstanceId { get { return default(System.Guid[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="Name")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override string[] Name { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ValidateRangeAttribute(1, 65535)]
    public int Port { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUriInstanceId")]
    public System.Management.Automation.Remoting.PSSessionOption SessionOption { get { return default(System.Management.Automation.Remoting.PSSessionOption); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUriInstanceId")]
    public Microsoft.PowerShell.Commands.SessionFilterState State { get { return default(Microsoft.PowerShell.Commands.SessionFilterState); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUriInstanceId")]
    public int ThrottleLimit { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    public System.Management.Automation.SwitchParameter UseSSL { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    public void Dispose() { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Get", "PSSessionConfiguration", HelpUri="http://go.microsoft.com/fwlink/?LinkID=144304")]
  [System.Management.Automation.OutputTypeAttribute(new string[]{ "Microsoft.PowerShell.Commands.PSSessionConfigurationCommands#PSSessionConfiguration"})]
  public sealed class GetPSSessionConfigurationCommand : System.Management.Automation.PSCmdlet {
    public GetPSSessionConfigurationCommand() { }
     
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=false)]
    public string[] Name { get { return default(string[]); } set { } }
     
    protected override void BeginProcessing() { }
    protected override void ProcessRecord() { }
  }
  public class HelpCategoryInvalidException : System.ArgumentException, System.Management.Automation.IContainsErrorRecord {
    public HelpCategoryInvalidException() { }
    public HelpCategoryInvalidException(string helpCategory) { }
    public HelpCategoryInvalidException(string helpCategory, System.Exception innerException) { }
     
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public string HelpCategory { get { return default(string); } }
    public override string Message { get { return default(string); } }
  }
  public class HelpNotFoundException : System.Exception, System.Management.Automation.IContainsErrorRecord
  {
    public HelpNotFoundException() { }
    public HelpNotFoundException(string helpTopic) { }
    public HelpNotFoundException(string helpTopic, System.Exception innerException) { }
     
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public string HelpTopic { get { return default(string); } }
    public override string Message { get { return default(string); } }
  }
  public class HistoryInfo {
    internal HistoryInfo() { }
    public string CommandLine { get { return default(string); } }
    public System.DateTime EndExecutionTime { get { return default(System.DateTime); } }
    public System.Management.Automation.Runspaces.PipelineState ExecutionStatus { get { return default(System.Management.Automation.Runspaces.PipelineState); } }
    public long Id { get { return default(long); } }
    public System.DateTime StartExecutionTime { get { return default(System.DateTime); } }
     
    public Microsoft.PowerShell.Commands.HistoryInfo Clone() { return default(Microsoft.PowerShell.Commands.HistoryInfo); }
    public override string ToString() { return default(string); }
  }
  [System.Management.Automation.CmdletAttribute("Import", "Module", DefaultParameterSetName="Name", HelpUri="http://go.microsoft.com/fwlink/?LinkID=141553")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.PSModuleInfo)})]
  public sealed class ImportModuleCommand : Microsoft.PowerShell.Commands.ModuleCmdletBase, System.IDisposable {
    public ImportModuleCommand() { }
     
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullAttribute]
    public string[] Alias { get { return default(string[]); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "Args"})]
    [System.Management.Automation.ParameterAttribute]
    public object[] ArgumentList { get { return default(object[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter AsCustomObject { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Assembly", Mandatory=true, ValueFromPipeline=true, Position=0)]
    public System.Reflection.Assembly[] Assembly { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Reflection.Assembly[]); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="CimSession", Mandatory=false)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string CimNamespace { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="CimSession", Mandatory=false)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public System.Uri CimResourceUri { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Uri); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
#if NEED_MMI_REF_ASSEM
    [System.Management.Automation.ParameterAttribute(ParameterSetName="CimSession", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public Microsoft.Management.Infrastructure.CimSession CimSession { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(Microsoft.Management.Infrastructure.CimSession); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
#endif
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullAttribute]
    public string[] Cmdlet { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter DisableNameChecking { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullAttribute]
    public string[] Function { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Global { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "Version"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="CimSession")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Name")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="PSSession")]
    public System.Version MinimumVersion { get { return default(System.Version); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ModuleInfo", Mandatory=true, ValueFromPipeline=true, Position=0)]
    public System.Management.Automation.PSModuleInfo[] ModuleInfo { get { return default(System.Management.Automation.PSModuleInfo[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="CimSession", Mandatory=true, ValueFromPipeline=true, Position=0)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Name", Mandatory=true, ValueFromPipeline=true, Position=0)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="PSSession", Mandatory=true, ValueFromPipeline=true, Position=0)]
    public string[] Name { get { return default(string[]); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "NoOverwrite"})]
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter NoClobber { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.SwitchParameter); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter PassThru { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullAttribute]
    public string Prefix { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="PSSession", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public System.Management.Automation.Runspaces.PSSession PSSession { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Runspaces.PSSession); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="CimSession")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Name")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="PSSession")]
    public System.Version RequiredVersion { get { return default(System.Version); } set { } }
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateSetAttribute(new string[]{ "Local", "Global"})]
    public string Scope { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullAttribute]
    public string[] Variable { get { return default(string[]); } set { } }
     
    protected override void BeginProcessing() { }
    public void Dispose() { }
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Invoke", "Command", DefaultParameterSetName="InProcess", HelpUri="http://go.microsoft.com/fwlink/?LinkID=135225", RemotingCapability=(System.Management.Automation.RemotingCapability)(3))]
  public class InvokeCommandCommand : Microsoft.PowerShell.Commands.PSExecutionCmdlet, System.IDisposable {
    public InvokeCommandCommand() { }
     
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilePathUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Uri")]
    public override System.Management.Automation.SwitchParameter AllowRedirection { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="FilePathComputerName")]
    public override string ApplicationName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilePathRunspace")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilePathUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Session")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Uri")]
    public System.Management.Automation.SwitchParameter AsJob { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilePathUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Uri")]
    public override System.Management.Automation.Runspaces.AuthenticationMechanism Authentication { get { return default(System.Management.Automation.Runspaces.AuthenticationMechanism); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "Cn"})]
    [System.Management.Automation.ParameterAttribute(Position=0, ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(Position=0, ParameterSetName="FilePathComputerName")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override string[] ComputerName { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="FilePathUri")]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="Uri")]
    public override string ConfigurationName { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "URI", "CU"})]
    [System.Management.Automation.ParameterAttribute(Position=0, ParameterSetName="FilePathUri")]
    [System.Management.Automation.ParameterAttribute(Position=0, ParameterSetName="Uri")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override System.Uri[] ConnectionUri { get { return default(System.Uri[]); } set { } }
    [System.Management.Automation.CredentialAttribute]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="FilePathUri")]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="Uri")]
    public override System.Management.Automation.PSCredential Credential { get { return default(System.Management.Automation.PSCredential); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilePathUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Uri")]
    public override System.Management.Automation.SwitchParameter EnableNetworkAccess { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "PSPath"})]
    [System.Management.Automation.ParameterAttribute(Position=1, Mandatory=true, ParameterSetName="FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(Position=1, Mandatory=true, ParameterSetName="FilePathRunspace")]
    [System.Management.Automation.ParameterAttribute(Position=1, Mandatory=true, ParameterSetName="FilePathUri")]
    [System.Management.Automation.ValidateNotNullAttribute]
    public override string FilePath { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "HCN"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilePathRunspace")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilePathUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Session")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Uri")]
    public System.Management.Automation.SwitchParameter HideComputerName { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "Disconnected"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilePathUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Uri")]
    public System.Management.Automation.SwitchParameter InDisconnectedSession { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilePathRunspace")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilePathUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Session")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Uri")]
    public string JobName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="InProcess")]
    public System.Management.Automation.SwitchParameter NoNewScope { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.SwitchParameter); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilePathComputerName")]
    [System.Management.Automation.ValidateRangeAttribute(1, 65535)]
    public override int Port { get { return default(int); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "Command"})]
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=true, ParameterSetName="InProcess")]
    [System.Management.Automation.ParameterAttribute(Position=1, Mandatory=true, ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(Position=1, Mandatory=true, ParameterSetName="Session")]
    [System.Management.Automation.ParameterAttribute(Position=1, Mandatory=true, ParameterSetName="Uri")]
    [System.Management.Automation.ValidateNotNullAttribute]
    public override System.Management.Automation.ScriptBlock ScriptBlock { get { return default(System.Management.Automation.ScriptBlock); } set { } }
    [System.Management.Automation.ParameterAttribute(Position=0, ParameterSetName="FilePathRunspace")]
    [System.Management.Automation.ParameterAttribute(Position=0, ParameterSetName="Session")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override System.Management.Automation.Runspaces.PSSession[] Session { get { return default(System.Management.Automation.Runspaces.PSSession[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilePathComputerName")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string[] SessionName { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilePathUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Uri")]
    public override System.Management.Automation.Remoting.PSSessionOption SessionOption { get { return default(System.Management.Automation.Remoting.PSSessionOption); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilePathRunspace")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilePathUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Session")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Uri")]
    public override int ThrottleLimit { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilePathComputerName")]
    public override System.Management.Automation.SwitchParameter UseSSL { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    protected override void BeginProcessing() { }
    public void Dispose() { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Invoke", "History", SupportsShouldProcess=true, HelpUri="http://go.microsoft.com/fwlink/?LinkID=113344")]
  public class InvokeHistoryCommand : System.Management.Automation.PSCmdlet {
    public InvokeHistoryCommand() { }
     
    [System.Management.Automation.ParameterAttribute(Position=0, ValueFromPipelineByPropertyName=true)]
    public string Id { get { return default(string); } set { } }
     
    protected override void EndProcessing() { }
  }
  public class JobCmdletBase : Microsoft.PowerShell.Commands.PSRemotingCmdlet {
    public JobCmdletBase() { }
     
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="CommandParameterSet")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public virtual string[] Command { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ValueFromPipelineByPropertyName=true, ParameterSetName="FilterParameterSet")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public virtual System.Collections.Hashtable Filter { get { return default(System.Collections.Hashtable); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, Position=0, Mandatory=true, ParameterSetName="SessionIdParameterSet")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public virtual int[] Id { get { return default(int[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, Position=0, Mandatory=true, ParameterSetName="InstanceIdParameterSet")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Guid[] InstanceId { get { return default(System.Guid[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, Position=0, Mandatory=true, ParameterSetName="NameParameterSet")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string[] Name { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ValueFromPipelineByPropertyName=true, ParameterSetName="StateParameterSet")]
    public virtual System.Management.Automation.JobState State { get { return default(System.Management.Automation.JobState); } set { } }
     
    protected override void BeginProcessing() { }
  }
  public class ModuleCmdletBase : System.Management.Automation.PSCmdlet {
    public ModuleCmdletBase() { }
     
    protected bool AddToAppDomainLevelCache { get { return default(bool); } set { } }
    protected object[] BaseArgumentList { get { return default(object[]); } set { } }
    protected bool BaseDisableNameChecking { get { return default(bool); } set { } }
     
    protected internal void ImportModuleMembers(System.Management.Automation.PSModuleInfo sourceModule, string prefix) { }
    protected internal void ImportModuleMembers(System.Management.Automation.PSModuleInfo sourceModule, string prefix, Microsoft.PowerShell.Commands.ModuleCmdletBase.ImportModuleOptions options) { }
     
    // Nested Types
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    protected internal partial struct ImportModuleOptions {
    }
  }
  public class ModuleSpecification {
    public ModuleSpecification(System.Collections.Hashtable moduleSpecification) { }
    public ModuleSpecification(string moduleName) { }
     
    public System.Nullable<System.Guid> Guid { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Nullable<System.Guid>); } }
    public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
    public System.Version Version { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Version); } }
     
  }
  [System.Management.Automation.CmdletAttribute("New", "Module", DefaultParameterSetName="ScriptBlock", HelpUri="http://go.microsoft.com/fwlink/?LinkID=141554")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.PSModuleInfo)})]
  public sealed class NewModuleCommand : Microsoft.PowerShell.Commands.ModuleCmdletBase {
    public NewModuleCommand() { }
     
    [System.Management.Automation.AliasAttribute(new string[]{ "Args"})]
    [System.Management.Automation.ParameterAttribute(ValueFromRemainingArguments=true)]
    public object[] ArgumentList { get { return default(object[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter AsCustomObject { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullAttribute]
    public string[] Cmdlet { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullAttribute]
    public string[] Function { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Name", Mandatory=true, ValueFromPipeline=true, Position=0)]
    public string Name { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter ReturnResult { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Name", Mandatory=true, Position=1)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ScriptBlock", Mandatory=true, Position=0)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public System.Management.Automation.ScriptBlock ScriptBlock { get { return default(System.Management.Automation.ScriptBlock); } set { } }
     
    protected override void EndProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("New", "ModuleManifest", SupportsShouldProcess=true, HelpUri="http://go.microsoft.com/fwlink/?LinkID=141555")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(string)})]
  public sealed class NewModuleManifestCommand : System.Management.Automation.PSCmdlet {
    public NewModuleManifestCommand() { }
     
    [System.Management.Automation.AllowEmptyCollectionAttribute]
    [System.Management.Automation.ParameterAttribute]
    public string[] AliasesToExport { get { return default(string[]); } set { } }
    [System.Management.Automation.AllowEmptyStringAttribute]
    [System.Management.Automation.ParameterAttribute]
    public string Author { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Version ClrVersion { get { return default(System.Version); } set { } }
    [System.Management.Automation.AllowEmptyCollectionAttribute]
    [System.Management.Automation.ParameterAttribute]
    public string[] CmdletsToExport { get { return default(string[]); } set { } }
    [System.Management.Automation.AllowEmptyStringAttribute]
    [System.Management.Automation.ParameterAttribute]
    public string CompanyName { get { return default(string); } set { } }
    [System.Management.Automation.AllowEmptyStringAttribute]
    [System.Management.Automation.ParameterAttribute]
    public string Copyright { get { return default(string); } set { } }
    [System.Management.Automation.AllowNullAttribute]
    [System.Management.Automation.ParameterAttribute]
    public string DefaultCommandPrefix { get { return default(string); } set { } }
    [System.Management.Automation.AllowEmptyStringAttribute]
    [System.Management.Automation.ParameterAttribute]
    public string Description { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Version DotNetFrameworkVersion { get { return default(System.Version); } set { } }
    [System.Management.Automation.AllowEmptyCollectionAttribute]
    [System.Management.Automation.ParameterAttribute]
    public string[] FileList { get { return default(string[]); } set { } }
    [System.Management.Automation.AllowEmptyCollectionAttribute]
    [System.Management.Automation.ParameterAttribute]
    public string[] FormatsToProcess { get { return default(string[]); } set { } }
    [System.Management.Automation.AllowEmptyCollectionAttribute]
    [System.Management.Automation.ParameterAttribute]
    public string[] FunctionsToExport { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Guid Guid { get { return default(System.Guid); } set { } }
    [System.Management.Automation.AllowNullAttribute]
    [System.Management.Automation.ParameterAttribute]
    public string HelpInfoUri { get { return default(string); } set { } }
    [System.Management.Automation.AllowEmptyCollectionAttribute]
    //Internal: [System.Management.Automation.ArgumentTypeConverterAttribute(new System.Type[]{ typeof(Microsoft.PowerShell.Commands.ModuleSpecification[])})]
    [System.Management.Automation.ParameterAttribute]
    public object[] ModuleList { get { return default(object[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullAttribute]
    public System.Version ModuleVersion { get { return default(System.Version); } set { } }
    [System.Management.Automation.AllowEmptyCollectionAttribute]
    [System.Management.Automation.ParameterAttribute]
    public object[] NestedModules { get { return default(object[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter PassThru { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0)]
    public string Path { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string PowerShellHostName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Version PowerShellHostVersion { get { return default(System.Version); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Version PowerShellVersion { get { return default(System.Version); } set { } }
    [System.Management.Automation.AllowNullAttribute]
    [System.Management.Automation.ParameterAttribute(Mandatory=false)]
    public object PrivateData { get { return default(object); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Reflection.ProcessorArchitecture ProcessorArchitecture { get { return default(System.Reflection.ProcessorArchitecture); } set { } }
    [System.Management.Automation.AllowEmptyCollectionAttribute]
    [System.Management.Automation.ParameterAttribute]
    public string[] RequiredAssemblies { get { return default(string[]); } set { } }
    //Internal: [System.Management.Automation.ArgumentTypeConverterAttribute(new System.Type[]{ typeof(Microsoft.PowerShell.Commands.ModuleSpecification[])})]
    [System.Management.Automation.ParameterAttribute]
    public object[] RequiredModules { get { return default(object[]); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "ModuleToProcess"})]
    [System.Management.Automation.AllowEmptyStringAttribute]
    [System.Management.Automation.ParameterAttribute]
    public string RootModule { get { return default(string); } set { } }
    [System.Management.Automation.AllowEmptyCollectionAttribute]
    [System.Management.Automation.ParameterAttribute]
    public string[] ScriptsToProcess { get { return default(string[]); } set { } }
    [System.Management.Automation.AllowEmptyCollectionAttribute]
    [System.Management.Automation.ParameterAttribute]
    public string[] TypesToProcess { get { return default(string[]); } set { } }
    [System.Management.Automation.AllowEmptyCollectionAttribute]
    [System.Management.Automation.ParameterAttribute]
    public string[] VariablesToExport { get { return default(string[]); } set { } }
     
    protected override void BeginProcessing() { }
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("New", "PSSession", DefaultParameterSetName="ComputerName", HelpUri="http://go.microsoft.com/fwlink/?LinkID=135237", RemotingCapability=(System.Management.Automation.RemotingCapability)(3))]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.Runspaces.PSSession)})]
  public class NewPSSessionCommand : Microsoft.PowerShell.Commands.PSRemotingBaseCmdlet, System.IDisposable {
    public NewPSSessionCommand() { }
     
    [System.Management.Automation.AliasAttribute(new string[]{ "Cn"})]
    [System.Management.Automation.ParameterAttribute(Position=0, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerName")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override string[] ComputerName { get { return default(string[]); } set { } }
    [System.Management.Automation.CredentialAttribute]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="Uri")]
    public override System.Management.Automation.PSCredential Credential { get { return default(System.Management.Automation.PSCredential); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter EnableNetworkAccess { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string[] Name { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Position=0, ValueFromPipelineByPropertyName=true, ValueFromPipeline=true, ParameterSetName="Session")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override System.Management.Automation.Runspaces.PSSession[] Session { get { return default(System.Management.Automation.Runspaces.PSSession[]); } set { } }
     
    protected override void BeginProcessing() { }
    public void Dispose() { }
    protected void Dispose(bool disposing) { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("New", "PSSessionConfigurationFile", HelpUri="http://go.microsoft.com/fwlink/?LinkID=217036")]
  public class NewPSSessionConfigurationFileCommand : System.Management.Automation.PSCmdlet {
    public NewPSSessionConfigurationFileCommand() { }
     
    [System.Management.Automation.ParameterAttribute]
    public System.Collections.Hashtable[] AliasDefinitions { get { return default(System.Collections.Hashtable[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string[] AssembliesToLoad { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string Author { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string CompanyName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string Copyright { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string Description { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public object EnvironmentVariables { get { return default(object); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public Microsoft.PowerShell.ExecutionPolicy ExecutionPolicy { get { return default(Microsoft.PowerShell.ExecutionPolicy); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string[] FormatsToProcess { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Collections.Hashtable[] FunctionDefinitions { get { return default(System.Collections.Hashtable[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Guid Guid { get { return default(System.Guid); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.PSLanguageMode LanguageMode { get { return default(System.Management.Automation.PSLanguageMode); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public object[] ModulesToImport { get { return default(object[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string Path { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Version PowerShellVersion { get { return default(System.Version); } set { } }
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullAttribute]
    public System.Version SchemaVersion { get { return default(System.Version); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string[] ScriptsToProcess { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.Remoting.SessionType SessionType { get { return default(System.Management.Automation.Remoting.SessionType); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string[] TypesToProcess { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public object VariableDefinitions { get { return default(object); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string[] VisibleAliases { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string[] VisibleCmdlets { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string[] VisibleFunctions { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string[] VisibleProviders { get { return default(string[]); } set { } }
     
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("New", "PSSessionOption", HelpUri="http://go.microsoft.com/fwlink/?LinkID=144305", RemotingCapability=(System.Management.Automation.RemotingCapability)(0))]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.Remoting.PSSessionOption)})]
  public sealed class NewPSSessionOptionCommand : System.Management.Automation.PSCmdlet {
    public NewPSSessionOptionCommand() { }
     
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullAttribute]
    public System.Management.Automation.PSPrimitiveDictionary ApplicationArguments { get { return default(System.Management.Automation.PSPrimitiveDictionary); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "CancelTimeoutMSec"})]
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateRangeAttribute(0, 2147483647)]
    public int CancelTimeout { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullAttribute]
    public System.Globalization.CultureInfo Culture { get { return default(System.Globalization.CultureInfo); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "IdleTimeoutMSec"})]
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateRangeAttribute(-1, 2147483647)]
    public int IdleTimeout { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter IncludePortInSPN { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public int MaximumReceivedDataSizePerCommand { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public int MaximumReceivedObjectSize { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public int MaximumRedirection { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter NoCompression { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter NoEncryption { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter NoMachineProfile { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "OpenTimeoutMSec"})]
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateRangeAttribute(0, 2147483647)]
    public int OpenTimeout { get { return default(int); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "OperationTimeoutMSec"})]
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateRangeAttribute(0, 2147483647)]
    public int OperationTimeout { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.Runspaces.OutputBufferingMode OutputBufferingMode { get { return default(System.Management.Automation.Runspaces.OutputBufferingMode); } set { } }
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Management.Automation.Remoting.ProxyAccessType ProxyAccessType { get { return default(System.Management.Automation.Remoting.ProxyAccessType); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.Runspaces.AuthenticationMechanism ProxyAuthentication { get { return default(System.Management.Automation.Runspaces.AuthenticationMechanism); } set { } }
    [System.Management.Automation.CredentialAttribute]
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Management.Automation.PSCredential ProxyCredential { get { return default(System.Management.Automation.PSCredential); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter SkipCACheck { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter SkipCNCheck { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter SkipRevocationCheck { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullAttribute]
    public System.Globalization.CultureInfo UICulture { get { return default(System.Globalization.CultureInfo); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter UseUTF16 { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    protected override void BeginProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("New", "PSTransportOption", HelpUri="http://go.microsoft.com/fwlink/?LinkID=210608", RemotingCapability=(System.Management.Automation.RemotingCapability)(0))]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(Microsoft.PowerShell.Commands.WSManConfigurationOption)})]
  public sealed class NewPSTransportOptionCommand : System.Management.Automation.PSCmdlet {
    public NewPSTransportOptionCommand() { }
     
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateRangeAttribute(60, 2147483)]
    public System.Nullable<int> IdleTimeoutSec { get { return default(System.Nullable<int>); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateRangeAttribute(1, 2147483647)]
    public System.Nullable<int> MaxConcurrentCommandsPerSession { get { return default(System.Nullable<int>); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateRangeAttribute(1, 100)]
    public System.Nullable<int> MaxConcurrentUsers { get { return default(System.Nullable<int>); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateRangeAttribute(60, 2147483)]
    public System.Nullable<int> MaxIdleTimeoutSec { get { return default(System.Nullable<int>); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateRangeAttribute(5, 2147483647)]
    public System.Nullable<int> MaxMemoryPerSessionMB { get { return default(System.Nullable<int>); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateRangeAttribute(1, 2147483647)]
    public System.Nullable<int> MaxProcessesPerSession { get { return default(System.Nullable<int>); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateRangeAttribute(1, 2147483647)]
    public System.Nullable<int> MaxSessions { get { return default(System.Nullable<int>); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateRangeAttribute(1, 2147483647)]
    public System.Nullable<int> MaxSessionsPerUser { get { return default(System.Nullable<int>); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    public System.Nullable<System.Management.Automation.Runspaces.OutputBufferingMode> OutputBufferingMode { get { return default(System.Nullable<System.Management.Automation.Runspaces.OutputBufferingMode>); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateRangeAttribute(0, 1209600)]
    public System.Nullable<int> ProcessIdleTimeoutSec { get { return default(System.Nullable<int>); } set { } }
     
    protected override void ProcessRecord() { }
  }
  public abstract class ObjectEventRegistrationBase : System.Management.Automation.PSCmdlet {
    protected ObjectEventRegistrationBase() { }
     
    [System.Management.Automation.ParameterAttribute(Position=101)]
    public System.Management.Automation.ScriptBlock Action { get { return default(System.Management.Automation.ScriptBlock); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Forward { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public int MaxTriggerCount { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.PSObject MessageData { get { return default(System.Management.Automation.PSObject); } set { } }
    protected System.Management.Automation.PSEventSubscriber NewSubscriber { get { return default(System.Management.Automation.PSEventSubscriber); } }
    [System.Management.Automation.ParameterAttribute(Position=100)]
    public string SourceIdentifier { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter SupportEvent { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    protected override void BeginProcessing() { }
    protected override void EndProcessing() { }
    protected abstract object GetSourceObject();
    protected abstract string GetSourceObjectEventName();
  }
  public enum OpenMode {
    Add = 0,
    New = 1,
    Overwrite = 2,
  }
  [System.Management.Automation.CmdletAttribute("Out", "Default", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113362", RemotingCapability=(System.Management.Automation.RemotingCapability)(0))]
  public class OutDefaultCommand : Microsoft.PowerShell.Commands.Internal.Format.FrontEndCommandBase {
    public OutDefaultCommand() { }
     
    protected override void BeginProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Out", "Host", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113365", RemotingCapability=(System.Management.Automation.RemotingCapability)(0))]
  public class OutHostCommand : Microsoft.PowerShell.Commands.Internal.Format.FrontEndCommandBase {
    public OutHostCommand() { }
     
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Paging { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    protected override void BeginProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Out", "LineOutput")]
  public class OutLineOutputCommand : Microsoft.PowerShell.Commands.Internal.Format.FrontEndCommandBase {
    public OutLineOutputCommand() { }
     
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0)]
    public object LineOutput { get { return default(object); } set { } }
     
    protected override void BeginProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Out", "Null", SupportsShouldProcess=false, HelpUri="http://go.microsoft.com/fwlink/?LinkID=113366", RemotingCapability=(System.Management.Automation.RemotingCapability)(0))]
  public class OutNullCommand : System.Management.Automation.PSCmdlet {
    public OutNullCommand() { }
     
    [System.Management.Automation.ParameterAttribute(ValueFromPipeline=true)]
    public System.Management.Automation.PSObject InputObject { get { return default(System.Management.Automation.PSObject); } set { } }
     
    protected override void ProcessRecord() { }
  }
  public enum OutTarget {
    Default = 0,
    Host = 1,
    Job = 2,
  }
  public abstract class PSExecutionCmdlet : Microsoft.PowerShell.Commands.PSRemotingBaseCmdlet {
    protected const string FilePathComputerNameParameterSet = "FilePathComputerName";
    protected const string FilePathSessionParameterSet = "FilePathRunspace";
    protected const string FilePathUriParameterSet = "FilePathUri";
    protected const string LiteralFilePathComputerNameParameterSet = "LiteralFilePathComputerName";
     
    protected PSExecutionCmdlet() { }
     
    [System.Management.Automation.AliasAttribute(new string[]{ "Args"})]
    [System.Management.Automation.ParameterAttribute]
    public virtual object[] ArgumentList { get { return default(object[]); } set { } }
    protected string[] DisconnectedSessionName { get { return default(string[]); } set { } }
    public virtual System.Management.Automation.SwitchParameter EnableNetworkAccess { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position=1, Mandatory=true, ParameterSetName="FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(Position=1, Mandatory=true, ParameterSetName="FilePathRunspace")]
    [System.Management.Automation.ParameterAttribute(Position=1, Mandatory=true, ParameterSetName="FilePathUri")]
    [System.Management.Automation.ValidateNotNullAttribute]
    public virtual string FilePath { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipeline=true)]
    public virtual System.Management.Automation.PSObject InputObject { get { return default(System.Management.Automation.PSObject); } set { } }
    protected bool InvokeAndDisconnect { get { return default(bool); } set { } }
    protected bool IsLiteralPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public virtual System.Management.Automation.ScriptBlock ScriptBlock { get { return default(System.Management.Automation.ScriptBlock); } set { } }
     
    protected override void BeginProcessing() { }
    protected void CloseAllInputStreams() { }
    protected virtual void CreateHelpersForSpecifiedComputerNames() { }
    protected void CreateHelpersForSpecifiedRunspaces() { }
    protected void CreateHelpersForSpecifiedUris() { }
    protected System.Management.Automation.ScriptBlock GetScriptBlockFromFile(string filePath, bool isLiteralPath) { return default(System.Management.Automation.ScriptBlock); }
  }
  public abstract class PSRemotingBaseCmdlet : Microsoft.PowerShell.Commands.PSRemotingCmdlet {
    protected const string UriParameterSet = "Uri";
     
    protected PSRemotingBaseCmdlet() { }
     
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Uri")]
    public virtual System.Management.Automation.SwitchParameter AllowRedirection { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerName")]
    public virtual string ApplicationName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Uri")]
    public virtual System.Management.Automation.Runspaces.AuthenticationMechanism Authentication { get { return default(System.Management.Automation.Runspaces.AuthenticationMechanism); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Uri")]
    public virtual string CertificateThumbprint { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "Cn"})]
    [System.Management.Automation.ParameterAttribute(Position=0, ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerName")]
    public virtual string[] ComputerName { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="Uri")]
    public virtual string ConfigurationName { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "URI", "CU"})]
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=true, ValueFromPipelineByPropertyName=true, ParameterSetName="Uri")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public virtual System.Uri[] ConnectionUri { get { return default(System.Uri[]); } set { } }
    [System.Management.Automation.CredentialAttribute]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="Uri")]
    public virtual System.Management.Automation.PSCredential Credential { get { return default(System.Management.Automation.PSCredential); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ValidateRangeAttribute(1, 65535)]
    public virtual int Port { get { return default(int); } set { } }
    protected string[] ResolvedComputerNames { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Position=0, ValueFromPipelineByPropertyName=true, ParameterSetName="Session")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public virtual System.Management.Automation.Runspaces.PSSession[] Session { get { return default(System.Management.Automation.Runspaces.PSSession[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Uri")]
    [System.Management.Automation.ValidateNotNullAttribute]
    public virtual System.Management.Automation.Remoting.PSSessionOption SessionOption { get { return default(System.Management.Automation.Remoting.PSSessionOption); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Session")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Uri")]
    public virtual int ThrottleLimit { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    public virtual System.Management.Automation.SwitchParameter UseSSL { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    protected override void BeginProcessing() { }
    protected void ValidateComputerName(string[] computerNames) { }
    protected void ValidateRemoteRunspacesSpecified() { }
  }
  public abstract class PSRemotingCmdlet : System.Management.Automation.PSCmdlet {
    protected const string ComputerNameParameterSet = "ComputerName";
    protected const string DefaultPowerShellRemoteShellAppName = "WSMan";
    protected const string DefaultPowerShellRemoteShellName = "http://schemas.microsoft.com/powershell/Microsoft.PowerShell";
    protected const string SessionParameterSet = "Session";
     
    protected PSRemotingCmdlet() { }
     
    protected override void BeginProcessing() { }
    protected string ResolveAppName(string appName) { return default(string); }
    protected string ResolveComputerName(string computerName) { return default(string); }
    protected void ResolveComputerNames(string[] computerNames, out string[] resolvedComputerNames) { resolvedComputerNames = default(string[]); }
    protected string ResolveShell(string shell) { return default(string); }
  }
  public abstract class PSRunspaceCmdlet : Microsoft.PowerShell.Commands.PSRemotingCmdlet {
    protected const string IdParameterSet = "Id";
    protected const string InstanceIdParameterSet = "InstanceId";
    protected const string NameParameterSet = "Name";
     
    protected PSRunspaceCmdlet() { }
     
    [System.Management.Automation.AliasAttribute(new string[]{ "Cn"})]
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=true, ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerName")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public virtual string[] ComputerName { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Position=0, ValueFromPipelineByPropertyName=true, Mandatory=true, ParameterSetName="Id")]
    [System.Management.Automation.ValidateNotNullAttribute]
    public int[] Id { get { return default(int[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ValueFromPipelineByPropertyName=true, ParameterSetName="InstanceId")]
    [System.Management.Automation.ValidateNotNullAttribute]
    public virtual System.Guid[] InstanceId { get { return default(System.Guid[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ValueFromPipelineByPropertyName=true, ParameterSetName="Name")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public virtual string[] Name { get { return default(string[]); } set { } }
     
    protected System.Collections.Generic.Dictionary<System.Guid, System.Management.Automation.Runspaces.PSSession> GetMatchingRunspaces(bool writeobject, bool writeErrorOnNoMatch) { return default(System.Collections.Generic.Dictionary<System.Guid, System.Management.Automation.Runspaces.PSSession>); }
    protected System.Collections.Generic.Dictionary<System.Guid, System.Management.Automation.Runspaces.PSSession> GetMatchingRunspacesByName(bool writeobject, bool writeErrorOnNoMatch) { return default(System.Collections.Generic.Dictionary<System.Guid, System.Management.Automation.Runspaces.PSSession>); }
    protected System.Collections.Generic.Dictionary<System.Guid, System.Management.Automation.Runspaces.PSSession> GetMatchingRunspacesByRunspaceId(bool writeobject, bool writeErrorOnNoMatch) { return default(System.Collections.Generic.Dictionary<System.Guid, System.Management.Automation.Runspaces.PSSession>); }
  }
  public class PSSessionConfigurationCommandBase : System.Management.Automation.PSCmdlet {
    internal PSSessionConfigurationCommandBase() { }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.Runspaces.PSSessionConfigurationAccessMode AccessMode { get { return default(System.Management.Automation.Runspaces.PSSessionConfigurationAccessMode); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="AssemblyNameParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="NameParameterSet")]
    public string ApplicationBase { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(Position=1, Mandatory=true, ParameterSetName="AssemblyNameParameterSet")]
    public string AssemblyName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(Position=2, Mandatory=true, ParameterSetName="AssemblyNameParameterSet")]
    public string ConfigurationTypeName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AllowNullAttribute]
    [System.Management.Automation.ParameterAttribute]
    public System.Nullable<double> MaximumReceivedDataSizePerCommandMB { get { return default(System.Nullable<double>); } set { } }
    [System.Management.Automation.AllowNullAttribute]
    [System.Management.Automation.ParameterAttribute]
    public System.Nullable<double> MaximumReceivedObjectSizeMB { get { return default(System.Nullable<double>); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="AssemblyNameParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="NameParameterSet")]
    public string[] ModulesToImport { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ValueFromPipelineByPropertyName=true, ParameterSetName="AssemblyNameParameterSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ValueFromPipelineByPropertyName=true, ParameterSetName="NameParameterSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ValueFromPipelineByPropertyName=true, ParameterSetName="SessionConfigurationFile")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string Name { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter NoServiceRestart { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="SessionConfigurationFile")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string Path { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "PowerShellVersion"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="AssemblyNameParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="NameParameterSet")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Version PSVersion { get { return default(System.Version); } set { } }
    [System.Management.Automation.CredentialAttribute]
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.PSCredential RunAsCredential { get { return default(System.Management.Automation.PSCredential); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string SecurityDescriptorSddl { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="AssemblyNameParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="NameParameterSet")]
    public System.Management.Automation.PSSessionTypeOption SessionTypeOption { get { return default(System.Management.Automation.PSSessionTypeOption); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter ShowSecurityDescriptorUI { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string StartupScript { get { return default(string); } set { } }
#if COM_APARTMENT_STATE
    [System.Management.Automation.ParameterAttribute]
    public System.Threading.ApartmentState ThreadApartmentState { get { return default(System.Threading.ApartmentState); } set { } }
#endif
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.Runspaces.PSThreadOptions ThreadOptions { get { return default(System.Management.Automation.Runspaces.PSThreadOptions); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.PSTransportOption TransportOption { get { return default(System.Management.Automation.PSTransportOption); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter UseSharedProcess { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
  }
  [System.Management.Automation.CmdletAttribute("Receive", "Job", DefaultParameterSetName="Location", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113372", RemotingCapability=(System.Management.Automation.RemotingCapability)(2))]
  public class ReceiveJobCommand : Microsoft.PowerShell.Commands.JobCmdletBase, System.IDisposable {
    protected const string LocationParameterSet = "Location";
     
    public ReceiveJobCommand() { }
     
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter AutoRemoveJob { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    public override string[] Command { get { return default(string[]); } }
    [System.Management.Automation.AliasAttribute(new string[]{ "Cn"})]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerName", Position=1)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string[] ComputerName { get { return default(string[]); } set { } }
    public override System.Collections.Hashtable Filter { get { return default(System.Collections.Hashtable); } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.SwitchParameter); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true, ParameterSetName="Location")]
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true, ParameterSetName="Session")]
    public System.Management.Automation.Job[] Job { get { return default(System.Management.Automation.Job[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Keep { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Location", Position=1)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string[] Location { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter NoRecurse { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="Session", Position=1)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public System.Management.Automation.Runspaces.PSSession[] Session { get { return default(System.Management.Automation.Runspaces.PSSession[]); } set { } }
    public override System.Management.Automation.JobState State { get { return default(System.Management.Automation.JobState); } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Wait { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter WriteEvents { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter WriteJobInResults { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    protected override void BeginProcessing() { }
    public void Dispose() { }
    protected void Dispose(bool disposing) { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Receive", "PSSession", SupportsShouldProcess=true, DefaultParameterSetName="Session", HelpUri="http://go.microsoft.com/fwlink/?LinkID=217037", RemotingCapability=(System.Management.Automation.RemotingCapability)(3))]
  public class ReceivePSSessionCommand : Microsoft.PowerShell.Commands.PSRemotingCmdlet {
    public ReceivePSSessionCommand() { }
     
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUriInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUriSessionName")]
    public System.Management.Automation.SwitchParameter AllowRedirection { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerSessionName")]
    public string ApplicationName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerSessionName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUriInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUriSessionName")]
    public System.Management.Automation.Runspaces.AuthenticationMechanism Authentication { get { return default(System.Management.Automation.Runspaces.AuthenticationMechanism); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerSessionName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUriInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUriSessionName")]
    public string CertificateThumbprint { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "Cn"})]
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=true, ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=true, ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerSessionName")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string ComputerName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerSessionName")]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="ConnectionUriInstanceId")]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="ConnectionUriSessionName")]
    public string ConfigurationName { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "URI", "CU"})]
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=true, ValueFromPipelineByPropertyName=true, ParameterSetName="ConnectionUriInstanceId")]
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=true, ValueFromPipelineByPropertyName=true, ParameterSetName="ConnectionUriSessionName")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Uri ConnectionUri { get { return default(System.Uri); } set { } }
    [System.Management.Automation.CredentialAttribute]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerSessionName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUriInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUriSessionName")]
    public System.Management.Automation.PSCredential Credential { get { return default(System.Management.Automation.PSCredential); } set { } }
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=true, ValueFromPipelineByPropertyName=true, ValueFromPipeline=true, ParameterSetName="Id")]
    public int Id { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="ConnectionUriInstanceId")]
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=true, ValueFromPipelineByPropertyName=true, ValueFromPipeline=true, ParameterSetName="InstanceId")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Guid InstanceId { get { return default(System.Guid); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerSessionName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUriInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUriSessionName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Id")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="InstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Session")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="SessionName")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string JobName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="ComputerSessionName")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="ConnectionUriSessionName")]
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=true, ValueFromPipelineByPropertyName=true, ValueFromPipeline=true, ParameterSetName="SessionName")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string Name { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerSessionName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUriInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUriSessionName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Id")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="InstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Session")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="SessionName")]
    public Microsoft.PowerShell.Commands.OutTarget OutTarget { get { return default(Microsoft.PowerShell.Commands.OutTarget); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerSessionName")]
    [System.Management.Automation.ValidateRangeAttribute(1, 65535)]
    public int Port { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=true, ValueFromPipelineByPropertyName=true, ValueFromPipeline=true, ParameterSetName="Session")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Management.Automation.Runspaces.PSSession Session { get { return default(System.Management.Automation.Runspaces.PSSession); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerSessionName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUriInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ConnectionUriSessionName")]
    public System.Management.Automation.Remoting.PSSessionOption SessionOption { get { return default(System.Management.Automation.Remoting.PSSessionOption); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerSessionName")]
    public System.Management.Automation.SwitchParameter UseSSL { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Register", "PSSessionConfiguration", DefaultParameterSetName="NameParameterSet", SupportsShouldProcess=true, ConfirmImpact=(System.Management.Automation.ConfirmImpact)(3), HelpUri="http://go.microsoft.com/fwlink/?LinkID=144306")]
  public sealed class RegisterPSSessionConfigurationCommand : Microsoft.PowerShell.Commands.PSSessionConfigurationCommandBase {
    public RegisterPSSessionConfigurationCommand() { }
     
    [System.Management.Automation.AliasAttribute(new string[]{ "PA"})]
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    [System.Management.Automation.ValidateSetAttribute(new string[]{ "x86", "amd64"})]
    public string ProcessorArchitecture { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="NameParameterSet")]
    public System.Management.Automation.Runspaces.PSSessionType SessionType { get { return default(System.Management.Automation.Runspaces.PSSessionType); } set { } }
     
    protected override void BeginProcessing() { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
  }
#if WIN32_REGISTRY
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(Microsoft.Win32.RegistryKey), typeof(string), typeof(int), typeof(long)}, ProviderCmdlet="Get-ItemProperty")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(Microsoft.Win32.RegistryKey), typeof(string)}, ProviderCmdlet="Get-ChildItem")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(Microsoft.Win32.RegistryKey)}, ProviderCmdlet="Get-ChildItem")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(Microsoft.Win32.RegistryKey)}, ProviderCmdlet="Get-Item")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(Microsoft.Win32.RegistryKey)}, ProviderCmdlet="Get-Item")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(Microsoft.Win32.RegistryKey)}, ProviderCmdlet="New-Item")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(string)}, ProviderCmdlet="Move-ItemProperty")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Security.AccessControl.RegistrySecurity)}, ProviderCmdlet="Get-Acl")]
  [System.Management.Automation.Provider.CmdletProviderAttribute("Registry", (System.Management.Automation.Provider.ProviderCapabilities)(80))]
  public sealed class RegistryProvider : System.Management.Automation.Provider.NavigationCmdletProvider, System.Management.Automation.Provider.IDynamicPropertyCmdletProvider, System.Management.Automation.Provider.IPropertyCmdletProvider, System.Management.Automation.Provider.ISecurityDescriptorCmdletProvider {
    public const string ProviderName = "Registry";
     
    public RegistryProvider() { }
     
    protected override void ClearItem(string path) { }
    public void ClearProperty(string path, System.Collections.ObjectModel.Collection<string> propertyToClear) { }
    public object ClearPropertyDynamicParameters(string path, System.Collections.ObjectModel.Collection<string> propertyToClear) { return default(object); }
    protected override void CopyItem(string path, string destination, bool recurse) { }
    public void CopyProperty(string sourcePath, string sourceProperty, string destinationPath, string destinationProperty) { }
    public object CopyPropertyDynamicParameters(string sourcePath, string sourceProperty, string destinationPath, string destinationProperty) { return default(object); }
    protected override void GetChildItems(string path, bool recurse) { }
    protected override string GetChildName(string path) { return default(string); }
    protected override void GetChildNames(string path, System.Management.Automation.ReturnContainers returnContainers) { }
    protected override void GetItem(string path) { }
    protected override string GetParentPath(string path, string root) { return default(string); }
    public void GetProperty(string path, System.Collections.ObjectModel.Collection<string> providerSpecificPickList) { }
    public object GetPropertyDynamicParameters(string path, System.Collections.ObjectModel.Collection<string> providerSpecificPickList) { return default(object); }
    public void GetSecurityDescriptor(string path, System.Security.AccessControl.AccessControlSections sections) { }
    protected override bool HasChildItems(string path) { return default(bool); }
    protected override System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo> InitializeDefaultDrives() { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo>); }
    protected override bool IsItemContainer(string path) { return default(bool); }
    protected override bool IsValidPath(string path) { return default(bool); }
    protected override bool ItemExists(string path) { return default(bool); }
    protected override void MoveItem(string path, string destination) { }
    public void MoveProperty(string sourcePath, string sourceProperty, string destinationPath, string destinationProperty) { }
    public object MovePropertyDynamicParameters(string sourcePath, string sourceProperty, string destinationPath, string destinationProperty) { return default(object); }
    protected override System.Management.Automation.PSDriveInfo NewDrive(System.Management.Automation.PSDriveInfo drive) { return default(System.Management.Automation.PSDriveInfo); }
    protected override void NewItem(string path, string type, object newItem) { }
    public void NewProperty(string path, string propertyName, string type, object value) { }
    public object NewPropertyDynamicParameters(string path, string propertyName, string type, object value) { return default(object); }
    public System.Security.AccessControl.ObjectSecurity NewSecurityDescriptorFromPath(string path, System.Security.AccessControl.AccessControlSections sections) { return default(System.Security.AccessControl.ObjectSecurity); }
    public System.Security.AccessControl.ObjectSecurity NewSecurityDescriptorOfType(string type, System.Security.AccessControl.AccessControlSections sections) { return default(System.Security.AccessControl.ObjectSecurity); }
    protected override void RemoveItem(string path, bool recurse) { }
    public void RemoveProperty(string path, string propertyName) { }
    public object RemovePropertyDynamicParameters(string path, string propertyName) { return default(object); }
    protected override void RenameItem(string path, string newName) { }
    public void RenameProperty(string path, string sourceProperty, string destinationProperty) { }
    public object RenamePropertyDynamicParameters(string path, string sourceProperty, string destinationProperty) { return default(object); }
    protected override void SetItem(string path, object value) { }
    protected override object SetItemDynamicParameters(string path, object value) { return default(object); }
    public void SetProperty(string path, System.Management.Automation.PSObject propertyValue) { }
    public object SetPropertyDynamicParameters(string path, System.Management.Automation.PSObject propertyValue) { return default(object); }
    public void SetSecurityDescriptor(string path, System.Security.AccessControl.ObjectSecurity securityDescriptor) { }
  }
  public class RegistryProviderSetItemDynamicParameter {
    public RegistryProviderSetItemDynamicParameter() { }
     
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    public Microsoft.Win32.RegistryValueKind Type { get { return default(Microsoft.Win32.RegistryValueKind); } set { } }
     
  }
#endif
  [System.Management.Automation.CmdletAttribute("Remove", "Job", SupportsShouldProcess=true, DefaultParameterSetName="SessionIdParameterSet", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113377")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.Job)}, ParameterSetName=new string[]{ "JobParameterSet"})]
  public class RemoveJobCommand : Microsoft.PowerShell.Commands.JobCmdletBase, System.IDisposable {
    public RemoveJobCommand() { }
     
    [System.Management.Automation.AliasAttribute(new string[]{ "F"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilterParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="InstanceIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="JobParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="NameParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="SessionIdParameterSet")]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true, ParameterSetName="JobParameterSet")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Management.Automation.Job[] Job { get { return default(System.Management.Automation.Job[]); } set { } }
     
    public void Dispose() { }
    protected void Dispose(bool disposing) { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Remove", "Module", SupportsShouldProcess=true, HelpUri="http://go.microsoft.com/fwlink/?LinkID=141556")]
  public sealed class RemoveModuleCommand : Microsoft.PowerShell.Commands.ModuleCmdletBase {
    public RemoveModuleCommand() { }
     
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="ModuleInfo", ValueFromPipeline=true, Position=0)]
    public System.Management.Automation.PSModuleInfo[] ModuleInfo { get { return default(System.Management.Automation.PSModuleInfo[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="name", ValueFromPipeline=true, Position=0)]
    public string[] Name { get { return default(string[]); } set { } }
     
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Remove", "PSSession", SupportsShouldProcess=true, DefaultParameterSetName="Id", HelpUri="http://go.microsoft.com/fwlink/?LinkID=135250", RemotingCapability=(System.Management.Automation.RemotingCapability)(3))]
  public class RemovePSSessionCommand : Microsoft.PowerShell.Commands.PSRunspaceCmdlet {
    public RemovePSSessionCommand() { }
     
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true, ParameterSetName="Session")]
    public System.Management.Automation.Runspaces.PSSession[] Session { get { return default(System.Management.Automation.Runspaces.PSSession[]); } set { } }
     
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Resume", "Job", SupportsShouldProcess=true, DefaultParameterSetName="SessionIdParameterSet", HelpUri="http://go.microsoft.com/fwlink/?LinkID=210611")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.Job)})]
  public class ResumeJobCommand : Microsoft.PowerShell.Commands.JobCmdletBase, System.IDisposable {
    public ResumeJobCommand() { }
     
    public override string[] Command { get { return default(string[]); } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true, ParameterSetName="JobParameterSet")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Management.Automation.Job[] Job { get { return default(System.Management.Automation.Job[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="__AllParameterSets")]
    public System.Management.Automation.SwitchParameter Wait { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.SwitchParameter); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
     
    public void Dispose() { }
    protected void Dispose(bool disposing) { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Save", "Help", DefaultParameterSetName="Path", HelpUri="http://go.microsoft.com/fwlink/?LinkID=210612")]
  public sealed class SaveHelpCommand : Microsoft.PowerShell.Commands.UpdatableHelpCommandBase {
    public SaveHelpCommand() { }
     
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="Path")]
    [System.Management.Automation.ValidateNotNullAttribute]
    public string[] DestinationPath { get { return default(string[]); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "PSPath"})]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="LiteralPath")]
    [System.Management.Automation.ValidateNotNullAttribute]
    public string[] LiteralPath { get { return default(string[]); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "Name"})]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="LiteralPath", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="Path", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public string[] Module { get { return default(string[]); } set { } }
     
    protected override void BeginProcessing() { }
    protected override void ProcessRecord() { }
  }
  public enum SessionFilterState {
    All = 0,
    Broken = 4,
    Closed = 3,
    Disconnected = 2,
    Opened = 1,
  }
  public abstract class SessionStateProviderBase : System.Management.Automation.Provider.ContainerCmdletProvider, System.Management.Automation.Provider.IContentCmdletProvider {
    protected SessionStateProviderBase() { }
     
    public void ClearContent(string path) { }
    public object ClearContentDynamicParameters(string path) { return default(object); }
    protected override void ClearItem(string path) { }
    protected override void CopyItem(string path, string copyPath, bool recurse) { }
    protected override void GetChildItems(string path, bool recurse) { }
    protected override void GetChildNames(string path, System.Management.Automation.ReturnContainers returnContainers) { }
    public System.Management.Automation.Provider.IContentReader GetContentReader(string path) { return default(System.Management.Automation.Provider.IContentReader); }
    public object GetContentReaderDynamicParameters(string path) { return default(object); }
    public System.Management.Automation.Provider.IContentWriter GetContentWriter(string path) { return default(System.Management.Automation.Provider.IContentWriter); }
    public object GetContentWriterDynamicParameters(string path) { return default(object); }
    protected override void GetItem(string name) { }
    protected override bool HasChildItems(string path) { return default(bool); }
    protected override bool IsValidPath(string path) { return default(bool); }
    protected override bool ItemExists(string path) { return default(bool); }
    protected override void NewItem(string path, string type, object newItem) { }
    protected override void RemoveItem(string path, bool recurse) { }
    protected override void RenameItem(string name, string newName) { }
    protected override void SetItem(string name, object value) { }
  }
  public class SessionStateProviderBaseContentReaderWriter : System.IDisposable, System.Management.Automation.Provider.IContentReader, System.Management.Automation.Provider.IContentWriter {
    internal SessionStateProviderBaseContentReaderWriter() { }
    public void Close() { }
    public void Dispose() { }
    public System.Collections.IList Read(long readCount) { return default(System.Collections.IList); }
    public void Seek(long offset, System.IO.SeekOrigin origin) { }
    public System.Collections.IList Write(System.Collections.IList content) { return default(System.Collections.IList); }
  }
  [System.Management.Automation.CmdletAttribute("Set", "PSDebug", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113398")]
  public sealed class SetPSDebugCommand : System.Management.Automation.PSCmdlet {
    public SetPSDebugCommand() { }
     
    [System.Management.Automation.ParameterAttribute(ParameterSetName="off")]
    public System.Management.Automation.SwitchParameter Off { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="on")]
    public System.Management.Automation.SwitchParameter Step { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="on")]
    public System.Management.Automation.SwitchParameter Strict { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="on")]
    [System.Management.Automation.ValidateRangeAttribute(0, 2)]
    public int Trace { get { return default(int); } set { } }
     
    protected override void BeginProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Set", "PSSessionConfiguration", DefaultParameterSetName="NameParameterSet", SupportsShouldProcess=true, ConfirmImpact=(System.Management.Automation.ConfirmImpact)(3), HelpUri="http://go.microsoft.com/fwlink/?LinkID=144307")]
  public sealed class SetPSSessionConfigurationCommand : Microsoft.PowerShell.Commands.PSSessionConfigurationCommandBase {
    public SetPSSessionConfigurationCommand() { }
     
    protected override void BeginProcessing() { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Set", "StrictMode", DefaultParameterSetName="Version", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113450")]
  public class SetStrictModeCommand : System.Management.Automation.PSCmdlet {
    public SetStrictModeCommand() { }
     
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Off", Mandatory=true)]
    public System.Management.Automation.SwitchParameter Off { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    //Internal: [Microsoft.PowerShell.Commands.SetStrictModeCommand.ArgumentToVersionTransformationAttribute]
    //Internal: [Microsoft.PowerShell.Commands.SetStrictModeCommand.ValidateVersionAttribute]
    [System.Management.Automation.AliasAttribute(new string[]{ "v"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Version", Mandatory=true)]
    public System.Version Version { get { return default(System.Version); } set { } }
     
    protected override void EndProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Start", "Job", DefaultParameterSetName="ComputerName", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113405")]
  //Internal: [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.PSRemotingJob)})]
  public class StartJobCommand : Microsoft.PowerShell.Commands.PSExecutionCmdlet, System.IDisposable {
    public StartJobCommand() { }
     
    public override System.Management.Automation.SwitchParameter AllowRedirection { get { return default(System.Management.Automation.SwitchParameter); } }
    public override string ApplicationName { get { return default(string); } }
    [System.Management.Automation.AliasAttribute(new string[]{ "Args"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="LiteralFilePathComputerName")]
    public override object[] ArgumentList { get { return default(object[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="LiteralFilePathComputerName")]
    public override System.Management.Automation.Runspaces.AuthenticationMechanism Authentication { get { return default(System.Management.Automation.Runspaces.AuthenticationMechanism); } set { } }
    public override string CertificateThumbprint { get { return default(string); } set { } }
    public override string[] ComputerName { get { return default(string[]); } }
    public override string ConfigurationName { get { return default(string); } set { } }
    public override System.Uri[] ConnectionUri { get { return default(System.Uri[]); } }
    [System.Management.Automation.CredentialAttribute]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="LiteralFilePathComputerName")]
    public override System.Management.Automation.PSCredential Credential { get { return default(System.Management.Automation.PSCredential); } set { } }
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=true, ParameterSetName="DefinitionName")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string DefinitionName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="DefinitionName")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string DefinitionPath { get { return default(string); } set { } }
    public override System.Management.Automation.SwitchParameter EnableNetworkAccess { get { return default(System.Management.Automation.SwitchParameter); } }
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=true, ParameterSetName="FilePathComputerName")]
    public override string FilePath { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="LiteralFilePathComputerName")]
    public virtual System.Management.Automation.ScriptBlock InitializationScript { get { return default(System.Management.Automation.ScriptBlock); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipeline=true, ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ValueFromPipeline=true, ParameterSetName="FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ValueFromPipeline=true, ParameterSetName="LiteralFilePathComputerName")]
    public override System.Management.Automation.PSObject InputObject { get { return default(System.Management.Automation.PSObject); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "PSPath"})]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="LiteralFilePathComputerName")]
    public string LiteralPath { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true, ParameterSetName="LiteralFilePathComputerName")]
    public virtual string Name { get { return default(string); } set { } }
    public override int Port { get { return default(int); } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="LiteralFilePathComputerName")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public virtual System.Version PSVersion { get { return default(System.Version); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="LiteralFilePathComputerName")]
    public virtual System.Management.Automation.SwitchParameter RunAs32 { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "Command"})]
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=true, ParameterSetName="ComputerName")]
    public override System.Management.Automation.ScriptBlock ScriptBlock { get { return default(System.Management.Automation.ScriptBlock); } set { } }
    public override System.Management.Automation.Runspaces.PSSession[] Session { get { return default(System.Management.Automation.Runspaces.PSSession[]); } }
    public override System.Management.Automation.Remoting.PSSessionOption SessionOption { get { return default(System.Management.Automation.Remoting.PSSessionOption); } set { } }
    public override int ThrottleLimit { get { return default(int); } }
    [System.Management.Automation.ParameterAttribute(Position=2, ParameterSetName="DefinitionName")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string Type { get { return default(string); } set { } }
    public override System.Management.Automation.SwitchParameter UseSSL { get { return default(System.Management.Automation.SwitchParameter); } }
     
    protected override void BeginProcessing() { }
    protected override void CreateHelpersForSpecifiedComputerNames() { }
    public void Dispose() { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Stop", "Job", SupportsShouldProcess=true, DefaultParameterSetName="SessionIdParameterSet", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113413")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.Job)})]
  public class StopJobCommand : Microsoft.PowerShell.Commands.JobCmdletBase, System.IDisposable {
    public StopJobCommand() { }
     
    public override string[] Command { get { return default(string[]); } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true, ParameterSetName="JobParameterSet")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Management.Automation.Job[] Job { get { return default(System.Management.Automation.Job[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter PassThru { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    public void Dispose() { }
    protected void Dispose(bool disposing) { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Suspend", "Job", SupportsShouldProcess=true, DefaultParameterSetName="SessionIdParameterSet", HelpUri="http://go.microsoft.com/fwlink/?LinkID=210613")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.Job)})]
  public class SuspendJobCommand : Microsoft.PowerShell.Commands.JobCmdletBase, System.IDisposable {
    public SuspendJobCommand() { }
     
    public override string[] Command { get { return default(string[]); } }
    [System.Management.Automation.AliasAttribute(new string[]{ "F"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="FilterParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="InstanceIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="JobParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="NameParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="SessionIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="StateParameterSet")]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true, ParameterSetName="JobParameterSet")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Management.Automation.Job[] Job { get { return default(System.Management.Automation.Job[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Wait { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    public void Dispose() { }
    protected void Dispose(bool disposing) { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Test", "ModuleManifest", HelpUri="http://go.microsoft.com/fwlink/?LinkID=141557")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.PSModuleInfo)})]
  public sealed class TestModuleManifestCommand : Microsoft.PowerShell.Commands.ModuleCmdletBase {
    public TestModuleManifestCommand() { }
     
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ValueFromPipeline=true, Position=0, ValueFromPipelineByPropertyName=true)]
    public string Path { get { return default(string); } set { } }
     
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Test", "PSSessionConfigurationFile", HelpUri="http://go.microsoft.com/fwlink/?LinkID=217039")]
  [System.Management.Automation.OutputTypeAttribute(new string[]{ "bool"})]
  public class TestPSSessionConfigurationFileCommand : System.Management.Automation.PSCmdlet {
    public TestPSSessionConfigurationFileCommand() { }
     
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ValueFromPipeline=true, Position=0, ValueFromPipelineByPropertyName=true)]
    public string Path { get { return default(string); } set { } }
     
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Unregister", "PSSessionConfiguration", SupportsShouldProcess=true, ConfirmImpact=(System.Management.Automation.ConfirmImpact)(3), HelpUri="http://go.microsoft.com/fwlink/?LinkID=144308")]
  public sealed class UnregisterPSSessionConfigurationCommand : System.Management.Automation.PSCmdlet {
    public UnregisterPSSessionConfigurationCommand() { }
     
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string Name { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter NoServiceRestart { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    protected override void BeginProcessing() { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
  }
  public class UpdatableHelpCommandBase : System.Management.Automation.PSCmdlet {
    internal UpdatableHelpCommandBase() { }
    [System.Management.Automation.CredentialAttribute]
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.PSCredential Credential { get { return default(System.Management.Automation.PSCredential); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position=2)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public System.Globalization.CultureInfo[] UICulture { get { return default(System.Globalization.CultureInfo[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter UseDefaultCredentials { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    protected override void EndProcessing() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Update", "Help", DefaultParameterSetName="Path", HelpUri="http://go.microsoft.com/fwlink/?LinkID=210614")]
  public sealed class UpdateHelpCommand : Microsoft.PowerShell.Commands.UpdatableHelpCommandBase {
    public UpdateHelpCommand() { }
     
    [System.Management.Automation.AliasAttribute(new string[]{ "PSPath"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="LiteralPath", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public string[] LiteralPath { get { return default(string[]); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "Name"})]
    [System.Management.Automation.ParameterAttribute(Position=0, ParameterSetName="LiteralPath", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(Position=0, ParameterSetName="Path", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public string[] Module { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Recurse { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="Path")]
    [System.Management.Automation.ValidateNotNullAttribute]
    public string[] SourcePath { get { return default(string[]); } set { } }
     
    protected override void BeginProcessing() { }
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.PSVariable)}, ProviderCmdlet="Copy-Item")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.PSVariable)}, ProviderCmdlet="Get-Item")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.PSVariable)}, ProviderCmdlet="New-Item")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.PSVariable)}, ProviderCmdlet="Rename-Item")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.PSVariable)}, ProviderCmdlet="Set-Item")]
  [System.Management.Automation.Provider.CmdletProviderAttribute("Variable", (System.Management.Automation.Provider.ProviderCapabilities)(16))]
  public sealed class VariableProvider : Microsoft.PowerShell.Commands.SessionStateProviderBase {
    public const string ProviderName = "Variable";
     
    public VariableProvider() { }
     
    protected override System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo> InitializeDefaultDrives() { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo>); }
  }
  [System.Management.Automation.CmdletAttribute("Wait", "Job", DefaultParameterSetName="SessionIdParameterSet", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113422")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.Job)})]
  public class WaitJobCommand : Microsoft.PowerShell.Commands.JobCmdletBase, System.IDisposable {
    public WaitJobCommand() { }
     
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Any { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.SwitchParameter); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public override string[] Command { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string[]); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.SwitchParameter); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true, ParameterSetName="JobParameterSet")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Management.Automation.Job[] Job { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Job[]); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "TimeoutSec"})]
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateRangeAttribute(-1, 2147483647)]
    public int Timeout { get { return default(int); } set { } }
     
    protected override void BeginProcessing() { }
    public void Dispose() { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Where", "Object", DefaultParameterSetName="EqualSet", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113423", RemotingCapability=(System.Management.Automation.RemotingCapability)(0))]
  public sealed class WhereObjectCommand : System.Management.Automation.PSCmdlet {
    public WhereObjectCommand() { }
     
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="CaseSensitiveContainsSet")]
    public System.Management.Automation.SwitchParameter CContains { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="CaseSensitiveEqualSet")]
    public System.Management.Automation.SwitchParameter CEQ { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="CaseSensitiveGreaterOrEqualSet")]
    public System.Management.Automation.SwitchParameter CGE { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="CaseSensitiveGreaterThanSet")]
    public System.Management.Automation.SwitchParameter CGT { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="CaseSensitiveInSet")]
    public System.Management.Automation.SwitchParameter CIn { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="CaseSensitiveLessOrEqualSet")]
    public System.Management.Automation.SwitchParameter CLE { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="CaseSensitiveLikeSet")]
    public System.Management.Automation.SwitchParameter CLike { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="CaseSensitiveLessThanSet")]
    public System.Management.Automation.SwitchParameter CLT { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="CaseSensitiveMatchSet")]
    public System.Management.Automation.SwitchParameter CMatch { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="CaseSensitiveNotEqualSet")]
    public System.Management.Automation.SwitchParameter CNE { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="CaseSensitiveNotContainsSet")]
    public System.Management.Automation.SwitchParameter CNotContains { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="CaseSensitiveNotInSet")]
    public System.Management.Automation.SwitchParameter CNotIn { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="CaseSensitiveNotLikeSet")]
    public System.Management.Automation.SwitchParameter CNotLike { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="CaseSensitiveNotMatchSet")]
    public System.Management.Automation.SwitchParameter CNotMatch { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "IContains"})]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="ContainsSet")]
    public System.Management.Automation.SwitchParameter Contains { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "IEQ"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="EqualSet")]
    public System.Management.Automation.SwitchParameter EQ { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="ScriptBlockSet")]
    public System.Management.Automation.ScriptBlock FilterScript { get { return default(System.Management.Automation.ScriptBlock); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "IGE"})]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="GreaterOrEqualSet")]
    public System.Management.Automation.SwitchParameter GE { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "IGT"})]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="GreaterThanSet")]
    public System.Management.Automation.SwitchParameter GT { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "IIn"})]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="InSet")]
    public System.Management.Automation.SwitchParameter In { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipeline=true)]
    public System.Management.Automation.PSObject InputObject { get { return default(System.Management.Automation.PSObject); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="IsSet")]
    public System.Management.Automation.SwitchParameter Is { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="IsNotSet")]
    public System.Management.Automation.SwitchParameter IsNot { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "ILE"})]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="LessOrEqualSet")]
    public System.Management.Automation.SwitchParameter LE { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "ILike"})]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="LikeSet")]
    public System.Management.Automation.SwitchParameter Like { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "ILT"})]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="LessThanSet")]
    public System.Management.Automation.SwitchParameter LT { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "IMatch"})]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="MatchSet")]
    public System.Management.Automation.SwitchParameter Match { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "INE"})]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="NotEqualSet")]
    public System.Management.Automation.SwitchParameter NE { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "INotContains"})]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="NotContainsSet")]
    public System.Management.Automation.SwitchParameter NotContains { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "INotIn"})]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="NotInSet")]
    public System.Management.Automation.SwitchParameter NotIn { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "INotLike"})]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="NotLikeSet")]
    public System.Management.Automation.SwitchParameter NotLike { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "INotMatch"})]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="NotMatchSet")]
    public System.Management.Automation.SwitchParameter NotMatch { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="CaseSensitiveContainsSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="CaseSensitiveEqualSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="CaseSensitiveGreaterOrEqualSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="CaseSensitiveGreaterThanSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="CaseSensitiveInSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="CaseSensitiveLessOrEqualSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="CaseSensitiveLessThanSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="CaseSensitiveLikeSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="CaseSensitiveMatchSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="CaseSensitiveNotContainsSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="CaseSensitiveNotEqualSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="CaseSensitiveNotInSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="CaseSensitiveNotLikeSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="CaseSensitiveNotMatchSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="ContainsSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="EqualSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="GreaterOrEqualSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="GreaterThanSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="InSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="IsNotSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="IsSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="LessOrEqualSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="LessThanSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="LikeSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="MatchSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="NotContainsSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="NotEqualSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="NotInSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="NotLikeSet")]
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ParameterSetName="NotMatchSet")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string Property { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="CaseSensitiveContainsSet")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="CaseSensitiveEqualSet")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="CaseSensitiveGreaterOrEqualSet")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="CaseSensitiveGreaterThanSet")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="CaseSensitiveInSet")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="CaseSensitiveLessOrEqualSet")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="CaseSensitiveLessThanSet")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="CaseSensitiveLikeSet")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="CaseSensitiveMatchSet")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="CaseSensitiveNotContainsSet")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="CaseSensitiveNotEqualSet")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="CaseSensitiveNotInSet")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="CaseSensitiveNotLikeSet")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="CaseSensitiveNotMatchSet")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="ContainsSet")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="EqualSet")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="GreaterOrEqualSet")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="GreaterThanSet")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="InSet")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="IsNotSet")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="IsSet")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="LessOrEqualSet")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="LessThanSet")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="LikeSet")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="MatchSet")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="NotContainsSet")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="NotEqualSet")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="NotInSet")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="NotLikeSet")]
    [System.Management.Automation.ParameterAttribute(Position=1, ParameterSetName="NotMatchSet")]
    public object Value { get { return default(object); } set { } }
     
    protected override void BeginProcessing() { }
    protected override void ProcessRecord() { }
  }
  public class WSManConfigurationOption : System.Management.Automation.PSTransportOption {
    internal WSManConfigurationOption() { }
    public System.Nullable<int> IdleTimeoutSec { get { return default(System.Nullable<int>); } }
    public System.Nullable<int> MaxConcurrentCommandsPerSession { get { return default(System.Nullable<int>); } }
    public System.Nullable<int> MaxConcurrentUsers { get { return default(System.Nullable<int>); } }
    public System.Nullable<int> MaxIdleTimeoutSec { get { return default(System.Nullable<int>); } }
    public System.Nullable<int> MaxMemoryPerSessionMB { get { return default(System.Nullable<int>); } }
    public System.Nullable<int> MaxProcessesPerSession { get { return default(System.Nullable<int>); } }
    public System.Nullable<int> MaxSessions { get { return default(System.Nullable<int>); } }
    public System.Nullable<int> MaxSessionsPerUser { get { return default(System.Nullable<int>); } }
    public System.Nullable<System.Management.Automation.Runspaces.OutputBufferingMode> OutputBufferingMode { get { return default(System.Nullable<System.Management.Automation.Runspaces.OutputBufferingMode>); } }
    public System.Nullable<int> ProcessIdleTimeoutSec { get { return default(System.Nullable<int>); } }
     
    protected internal override void LoadFromDefaults(System.Management.Automation.Runspaces.PSSessionType sessionType, bool keepAssigned) { }
  }
}
namespace Microsoft.PowerShell.Commands.Internal {
#if WIN32_REGISTRY
  public sealed class TransactedRegistryAccessRule : System.Security.AccessControl.AccessRule {
    public TransactedRegistryAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.RegistryRights registryRights, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AccessControlType type) { }
     
    public System.Security.AccessControl.RegistryRights RegistryRights { get { return default(System.Security.AccessControl.RegistryRights); } }
     
  }
  public sealed class TransactedRegistryAuditRule : System.Security.AccessControl.AuditRule {
    internal TransactedRegistryAuditRule() { }
    public System.Security.AccessControl.RegistryRights RegistryRights { get { return default(System.Security.AccessControl.RegistryRights); } }
     
  }
  [System.Runtime.InteropServices.ComVisibleAttribute(true)]
  public sealed class TransactedRegistryKey : System.MarshalByRefObject, System.IDisposable {
    internal TransactedRegistryKey() { }
    public string Name { get { return default(string); } }
    public int SubKeyCount { get { return default(int); } }
    public int ValueCount { get { return default(int); } }
     
    public void Close() { }
    public Microsoft.PowerShell.Commands.Internal.TransactedRegistryKey CreateSubKey(string subkey) { return default(Microsoft.PowerShell.Commands.Internal.TransactedRegistryKey); }
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public Microsoft.PowerShell.Commands.Internal.TransactedRegistryKey CreateSubKey(string subkey, Microsoft.Win32.RegistryKeyPermissionCheck permissionCheck) { return default(Microsoft.PowerShell.Commands.Internal.TransactedRegistryKey); }
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public Microsoft.PowerShell.Commands.Internal.TransactedRegistryKey CreateSubKey(string subkey, Microsoft.Win32.RegistryKeyPermissionCheck permissionCheck, Microsoft.PowerShell.Commands.Internal.TransactedRegistrySecurity registrySecurity) { return default(Microsoft.PowerShell.Commands.Internal.TransactedRegistryKey); }
    public void DeleteSubKey(string subkey) { }
    public void DeleteSubKey(string subkey, bool throwOnMissingSubKey) { }
    public void DeleteSubKeyTree(string subkey) { }
    public void DeleteValue(string name) { }
    public void DeleteValue(string name, bool throwOnMissingValue) { }
    public void Dispose() { }
    public void Flush() { }
    public Microsoft.PowerShell.Commands.Internal.TransactedRegistrySecurity GetAccessControl() { return default(Microsoft.PowerShell.Commands.Internal.TransactedRegistrySecurity); }
    public Microsoft.PowerShell.Commands.Internal.TransactedRegistrySecurity GetAccessControl(System.Security.AccessControl.AccessControlSections includeSections) { return default(Microsoft.PowerShell.Commands.Internal.TransactedRegistrySecurity); }
    public string[] GetSubKeyNames() { return default(string[]); }
    public object GetValue(string name) { return default(object); }
    public object GetValue(string name, object defaultValue) { return default(object); }
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public object GetValue(string name, object defaultValue, Microsoft.Win32.RegistryValueOptions options) { return default(object); }
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public Microsoft.Win32.RegistryValueKind GetValueKind(string name) { return default(Microsoft.Win32.RegistryValueKind); }
    public string[] GetValueNames() { return default(string[]); }
    public Microsoft.PowerShell.Commands.Internal.TransactedRegistryKey OpenSubKey(string name) { return default(Microsoft.PowerShell.Commands.Internal.TransactedRegistryKey); }
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public Microsoft.PowerShell.Commands.Internal.TransactedRegistryKey OpenSubKey(string name, Microsoft.Win32.RegistryKeyPermissionCheck permissionCheck) { return default(Microsoft.PowerShell.Commands.Internal.TransactedRegistryKey); }
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public Microsoft.PowerShell.Commands.Internal.TransactedRegistryKey OpenSubKey(string name, Microsoft.Win32.RegistryKeyPermissionCheck permissionCheck, System.Security.AccessControl.RegistryRights rights) { return default(Microsoft.PowerShell.Commands.Internal.TransactedRegistryKey); }
    public Microsoft.PowerShell.Commands.Internal.TransactedRegistryKey OpenSubKey(string name, bool writable) { return default(Microsoft.PowerShell.Commands.Internal.TransactedRegistryKey); }
    public void SetAccessControl(Microsoft.PowerShell.Commands.Internal.TransactedRegistrySecurity registrySecurity) { }
    public void SetValue(string name, object value) { }
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public void SetValue(string name, object value, Microsoft.Win32.RegistryValueKind valueKind) { }
    public override string ToString() { return default(string); }
  }
  public sealed class TransactedRegistrySecurity : System.Security.AccessControl.NativeObjectSecurity {
    public TransactedRegistrySecurity() { }
     
    public override System.Type AccessRightType { get { return default(System.Type); } }
    public override System.Type AccessRuleType { get { return default(System.Type); } }
    public override System.Type AuditRuleType { get { return default(System.Type); } }
     
    public override System.Security.AccessControl.AccessRule AccessRuleFactory(System.Security.Principal.IdentityReference identityReference, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AccessControlType type) { return default(System.Security.AccessControl.AccessRule); }
    public void AddAccessRule(Microsoft.PowerShell.Commands.Internal.TransactedRegistryAccessRule rule) { }
    public void AddAuditRule(Microsoft.PowerShell.Commands.Internal.TransactedRegistryAuditRule rule) { }
    public override System.Security.AccessControl.AuditRule AuditRuleFactory(System.Security.Principal.IdentityReference identityReference, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AuditFlags flags) { return default(System.Security.AccessControl.AuditRule); }
    public bool RemoveAccessRule(Microsoft.PowerShell.Commands.Internal.TransactedRegistryAccessRule rule) { return default(bool); }
    public void RemoveAccessRuleAll(Microsoft.PowerShell.Commands.Internal.TransactedRegistryAccessRule rule) { }
    public void RemoveAccessRuleSpecific(Microsoft.PowerShell.Commands.Internal.TransactedRegistryAccessRule rule) { }
    public bool RemoveAuditRule(Microsoft.PowerShell.Commands.Internal.TransactedRegistryAuditRule rule) { return default(bool); }
    public void RemoveAuditRuleAll(Microsoft.PowerShell.Commands.Internal.TransactedRegistryAuditRule rule) { }
    public void RemoveAuditRuleSpecific(Microsoft.PowerShell.Commands.Internal.TransactedRegistryAuditRule rule) { }
    public void ResetAccessRule(Microsoft.PowerShell.Commands.Internal.TransactedRegistryAccessRule rule) { }
    public void SetAccessRule(Microsoft.PowerShell.Commands.Internal.TransactedRegistryAccessRule rule) { }
    public void SetAuditRule(Microsoft.PowerShell.Commands.Internal.TransactedRegistryAuditRule rule) { }
  }
#endif
}
namespace Microsoft.PowerShell.Commands.Internal.Format {
  public abstract class FrontEndCommandBase : System.Management.Automation.PSCmdlet, System.IDisposable {
    protected FrontEndCommandBase() { }
     
    [System.Management.Automation.ParameterAttribute(ValueFromPipeline=true)]
    public System.Management.Automation.PSObject InputObject { get { return default(System.Management.Automation.PSObject); } set { } }
     
    protected override void BeginProcessing() { }
    public void Dispose() { }
    protected virtual void Dispose(bool disposing) { }
    protected override void EndProcessing() { }
    protected virtual System.Management.Automation.PSObject InputObjectCall() { return default(System.Management.Automation.PSObject); }
    protected virtual void InternalDispose() { }
    protected virtual System.Management.Automation.PSCmdlet OuterCmdletCall() { return default(System.Management.Automation.PSCmdlet); }
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
    protected virtual void WriteObjectCall(object value) { }
  }
  public class OuterFormatShapeCommandBase : Microsoft.PowerShell.Commands.Internal.Format.FrontEndCommandBase {
    public OuterFormatShapeCommandBase() { }
     
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter DisplayError { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateSetAttribute(new string[]{ "CoreOnly", "EnumOnly", "Both"}, IgnoreCase=true)]
    public string Expand { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public object GroupBy { get { return default(object); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter ShowError { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string View { get { return default(string); } set { } }
     
    protected override void BeginProcessing() { }
  }
  public class OuterFormatTableAndListBase : Microsoft.PowerShell.Commands.Internal.Format.OuterFormatShapeCommandBase {
    public OuterFormatTableAndListBase() { }
     
    [System.Management.Automation.ParameterAttribute(Position=0)]
    public object[] Property { get { return default(object[]); } set { } }
     
  }
  public class OuterFormatTableBase : Microsoft.PowerShell.Commands.Internal.Format.OuterFormatTableAndListBase {
    public OuterFormatTableBase() { }
     
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter AutoSize { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter HideTableHeaders { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Wrap { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
  }
}
namespace Microsoft.PowerShell.Commands.Management {
#if TRANSACTIONS
  public class TransactedString : System.Transactions.IEnlistmentNotification {
    public TransactedString() { }
    public TransactedString(string value) { }
     
    public int Length { get { return default(int); } }
     
    public void Append(string text) { }
    public void Remove(int startIndex, int length) { }
    void System.Transactions.IEnlistmentNotification.Commit(System.Transactions.Enlistment enlistment) { }
    void System.Transactions.IEnlistmentNotification.InDoubt(System.Transactions.Enlistment enlistment) { }
    void System.Transactions.IEnlistmentNotification.Prepare(System.Transactions.PreparingEnlistment preparingEnlistment) { }
    void System.Transactions.IEnlistmentNotification.Rollback(System.Transactions.Enlistment enlistment) { }
    public override string ToString() { return default(string); }
  }
#endif
}
namespace System.Management.Automation {
  public enum ActionPreference {
    Continue = 2,
    Ignore = 4,
    Inquire = 3,
    SilentlyContinue = 0,
    Stop = 1,
  }
  public class ActionPreferenceStopException : System.Management.Automation.RuntimeException {
    public ActionPreferenceStopException() { }
    public ActionPreferenceStopException(string message) { }
    public ActionPreferenceStopException(string message, System.Exception innerException) { }
     
    public override System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
  }
  [System.AttributeUsageAttribute((AttributeTargets)388, AllowMultiple=false)]
  public sealed class AliasAttribute : System.Management.Automation.Internal.ParsingBaseAttribute {
    public AliasAttribute(params string[] aliasNames) { }
     
    public System.Collections.Generic.IList<string> AliasNames { get { return default(System.Collections.Generic.IList<string>); } }
     
  }
  public class AliasInfo : System.Management.Automation.CommandInfo {
    internal AliasInfo() { }
    public override string Definition { get { return default(string); } }
    public string Description { get { return default(string); } set { } }
    public System.Management.Automation.ScopedItemOptions Options { get { return default(System.Management.Automation.ScopedItemOptions); } set { } }
    public override System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName> OutputType { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName>); } }
    public System.Management.Automation.CommandInfo ReferencedCommand { get { return default(System.Management.Automation.CommandInfo); } }
    public System.Management.Automation.CommandInfo ResolvedCommand { get { return default(System.Management.Automation.CommandInfo); } }
     
  }
  public enum Alignment {
    Center = 2,
    Left = 1,
    Right = 3,
    Undefined = 0,
  }
  [System.AttributeUsageAttribute((AttributeTargets)384)]
  public sealed class AllowEmptyCollectionAttribute : System.Management.Automation.Internal.CmdletMetadataAttribute {
    public AllowEmptyCollectionAttribute() { }
  }
  [System.AttributeUsageAttribute((AttributeTargets)384)]
  public sealed class AllowEmptyStringAttribute : System.Management.Automation.Internal.CmdletMetadataAttribute {
    public AllowEmptyStringAttribute() { }
  }
  [System.AttributeUsageAttribute((AttributeTargets)384)]
  public sealed class AllowNullAttribute : System.Management.Automation.Internal.CmdletMetadataAttribute {
    public AllowNullAttribute() { }
  }
  public class ApplicationFailedException : System.Management.Automation.RuntimeException {
    public ApplicationFailedException() { }
    public ApplicationFailedException(string message) { }
    public ApplicationFailedException(string message, System.Exception innerException) { }
  }
  public class ApplicationInfo : System.Management.Automation.CommandInfo {
    internal ApplicationInfo() { }
    public override string Definition { get { return default(string); } }
    public string Extension { get { return default(string); } }
    public override System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName> OutputType { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName>); } }
    public string Path { get { return default(string); } }
    public override System.Management.Automation.SessionStateEntryVisibility Visibility { get { return default(System.Management.Automation.SessionStateEntryVisibility); } set { } }
     
  }
  [System.AttributeUsageAttribute((AttributeTargets)384)]
  public abstract class ArgumentTransformationAttribute : System.Management.Automation.Internal.CmdletMetadataAttribute {
    protected ArgumentTransformationAttribute() { }
     
    public abstract object Transform(System.Management.Automation.EngineIntrinsics engineIntrinsics, object inputData);
  }
  public class ArgumentTransformationMetadataException : System.Management.Automation.MetadataException {
    public ArgumentTransformationMetadataException() { }
    public ArgumentTransformationMetadataException(string message) { }
    public ArgumentTransformationMetadataException(string message, System.Exception innerException) { }
  }
  public class AuthorizationManager {
    public AuthorizationManager(string shellId) { }
     
    protected internal virtual bool ShouldRun(System.Management.Automation.CommandInfo commandInfo, System.Management.Automation.CommandOrigin origin, System.Management.Automation.Host.PSHost host, out System.Exception reason) { reason = default(System.Exception); return default(bool); }
  }
#if SYSTEM_DIAGNOSTICS
  public class BackgroundDispatcher : System.Management.Automation.IBackgroundDispatcher {
    public BackgroundDispatcher(System.Diagnostics.Eventing.EventProvider transferProvider, System.Diagnostics.Eventing.EventDescriptor transferEvent) { }
     
    public System.IAsyncResult BeginInvoke(System.Threading.WaitCallback callback, object state, System.AsyncCallback completionCallback, object asyncState) { return default(System.IAsyncResult); }
    public void EndInvoke(System.IAsyncResult asyncResult) { }
    public bool QueueUserWorkItem(System.Threading.WaitCallback callback) { return default(bool); }
    public bool QueueUserWorkItem(System.Threading.WaitCallback callback, object state) { return default(bool); }
  }
#endif
  public abstract class Breakpoint {
    internal Breakpoint() { }
    public System.Management.Automation.ScriptBlock Action { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.ScriptBlock); } }
    public bool Enabled { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } }
    public int HitCount { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(int); } }
    public int Id { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(int); } }
    public string Script { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
     
  }
  public class BreakpointUpdatedEventArgs : System.EventArgs {
    internal BreakpointUpdatedEventArgs() { }
    public System.Management.Automation.Breakpoint Breakpoint { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Breakpoint); } }
    public System.Management.Automation.BreakpointUpdateType UpdateType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.BreakpointUpdateType); } }
     
  }
  public enum BreakpointUpdateType {
    Disabled = 3,
    Enabled = 2,
    Removed = 1,
    Set = 0,
  }
  public sealed class CallStackFrame {
    internal CallStackFrame() { }
    public string FunctionName { get { return default(string); } }
    public System.Management.Automation.InvocationInfo InvocationInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.InvocationInfo); } }
    public System.Management.Automation.Language.IScriptExtent Position { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.IScriptExtent); } }
    public int ScriptLineNumber { get { return default(int); } }
    public string ScriptName { get { return default(string); } }
     
    public System.Collections.Generic.Dictionary<string, System.Management.Automation.PSVariable> GetFrameVariables() { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.PSVariable>); }
    public string GetScriptLocation() { return default(string); }
    public override string ToString() { return default(string); }
  }
  public sealed class ChildItemCmdletProviderIntrinsics {
    internal ChildItemCmdletProviderIntrinsics() { }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Get(string path, bool recurse) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Get(string[] path, bool recurse, bool force, bool literalPath) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<string> GetNames(string path, System.Management.Automation.ReturnContainers returnContainers, bool recurse) { return default(System.Collections.ObjectModel.Collection<string>); }
    public System.Collections.ObjectModel.Collection<string> GetNames(string[] path, System.Management.Automation.ReturnContainers returnContainers, bool recurse, bool force, bool literalPath) { return default(System.Collections.ObjectModel.Collection<string>); }
    public bool HasChild(string path) { return default(bool); }
    public bool HasChild(string path, bool force, bool literalPath) { return default(bool); }
  }
  public abstract class Cmdlet : System.Management.Automation.Internal.InternalCommand {
    protected Cmdlet() { }
     
    public System.Management.Automation.ICommandRuntime CommandRuntime { get { return default(System.Management.Automation.ICommandRuntime); } set { } }
#if TRANSACTIONS
    public System.Management.Automation.PSTransactionContext CurrentPSTransaction { get { return default(System.Management.Automation.PSTransactionContext); } }
#endif
    public bool Stopping { get { return default(bool); } }
     
    protected virtual void BeginProcessing() { }
    protected virtual void EndProcessing() { }
    public virtual string GetResourceString(string baseName, string resourceId) { return default(string); }
    public System.Collections.IEnumerable Invoke() { return default(System.Collections.IEnumerable); }
    public System.Collections.Generic.IEnumerable<T> Invoke<T>() { return default(System.Collections.Generic.IEnumerable<T>); }
    protected virtual void ProcessRecord() { }
    public bool ShouldContinue(string query, string caption) { return default(bool); }
    public bool ShouldContinue(string query, string caption, ref bool yesToAll, ref bool noToAll) { return default(bool); }
    public bool ShouldProcess(string target) { return default(bool); }
    public bool ShouldProcess(string target, string action) { return default(bool); }
    public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption) { return default(bool); }
    public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption, out System.Management.Automation.ShouldProcessReason shouldProcessReason) { shouldProcessReason = default(System.Management.Automation.ShouldProcessReason); return default(bool); }
    protected virtual void StopProcessing() { }
    public void ThrowTerminatingError(System.Management.Automation.ErrorRecord errorRecord) { }
    public bool TransactionAvailable() { return default(bool); }
    public void WriteCommandDetail(string text) { }
    public void WriteDebug(string text) { }
    public void WriteError(System.Management.Automation.ErrorRecord errorRecord) { }
    public void WriteObject(object sendToPipeline) { }
    public void WriteObject(object sendToPipeline, bool enumerateCollection) { }
    public void WriteProgress(System.Management.Automation.ProgressRecord progressRecord) { }
    public void WriteVerbose(string text) { }
    public void WriteWarning(string text) { }
  }
  [System.AttributeUsageAttribute((AttributeTargets)4)]
  public sealed class CmdletAttribute : System.Management.Automation.CmdletCommonMetadataAttribute {
    public CmdletAttribute(string verbName, string nounName) { }
     
    public string NounName { get { return default(string); } }
    public string VerbName { get { return default(string); } }
     
  }
  [System.AttributeUsageAttribute((AttributeTargets)4)]
  public class CmdletBindingAttribute : System.Management.Automation.CmdletCommonMetadataAttribute {
    public CmdletBindingAttribute() { }
     
    public bool PositionalBinding { get { return default(bool); } set { } }
     
  }
  [System.AttributeUsageAttribute((AttributeTargets)4)]
  public abstract class CmdletCommonMetadataAttribute : System.Management.Automation.Internal.CmdletMetadataAttribute {
    protected CmdletCommonMetadataAttribute() { }
     
    public System.Management.Automation.ConfirmImpact ConfirmImpact { get { return default(System.Management.Automation.ConfirmImpact); } set { } }
    public string DefaultParameterSetName { get { return default(string); } set { } }
    public string HelpUri { get { return default(string); } set { } }
    public System.Management.Automation.RemotingCapability RemotingCapability { get { return default(System.Management.Automation.RemotingCapability); } set { } }
    public bool SupportsPaging { get { return default(bool); } set { } }
    public bool SupportsShouldProcess { get { return default(bool); } set { } }
    public bool SupportsTransactions { get { return default(bool); } set { } }
     
  }
  public class CmdletInfo : System.Management.Automation.CommandInfo {
    public CmdletInfo(string name, System.Type implementingType) { }
     
    public string DefaultParameterSet { get { return default(string); } }
    public override string Definition { get { return default(string); } }
    public string HelpFile { get { return default(string); } }
    public System.Type ImplementingType { get { return default(System.Type); } }
    public string Noun { get { return default(string); } }
    public System.Management.Automation.ScopedItemOptions Options { get { return default(System.Management.Automation.ScopedItemOptions); } set { } }
    public override System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName> OutputType { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName>); } }
    public string Verb { get { return default(string); } }
     
  }
  public class CmdletInvocationException : System.Management.Automation.RuntimeException {
    public CmdletInvocationException() { }
    public CmdletInvocationException(string message) { }
    public CmdletInvocationException(string message, System.Exception innerException) { }
     
    public override System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
  }
  public class CmdletProviderInvocationException : System.Management.Automation.CmdletInvocationException {
    public CmdletProviderInvocationException() { }
    public CmdletProviderInvocationException(string message) { }
    public CmdletProviderInvocationException(string message, System.Exception innerException) { }
     
    public System.Management.Automation.ProviderInfo ProviderInfo { get { return default(System.Management.Automation.ProviderInfo); } }
    public System.Management.Automation.ProviderInvocationException ProviderInvocationException { get { return default(System.Management.Automation.ProviderInvocationException); } }
     
  }
  public sealed class CmdletProviderManagementIntrinsics {
    internal CmdletProviderManagementIntrinsics() { }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.ProviderInfo> Get(string name) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.ProviderInfo>); }
    public System.Collections.Generic.IEnumerable<System.Management.Automation.ProviderInfo> GetAll() { return default(System.Collections.Generic.IEnumerable<System.Management.Automation.ProviderInfo>); }
    public System.Management.Automation.ProviderInfo GetOne(string name) { return default(System.Management.Automation.ProviderInfo); }
  }
  public class CommandBreakpoint : System.Management.Automation.Breakpoint {
    internal CommandBreakpoint() { }
    public string Command { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
     
    public override string ToString() { return default(string); }
  }
  public class CommandCompletion {
    internal CommandCompletion() { }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.CompletionResult> CompletionMatches { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.CompletionResult>); } }
    public int CurrentMatchIndex { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(int); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public int ReplacementIndex { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(int); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public int ReplacementLength { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(int); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
     
    public static System.Management.Automation.CommandCompletion CompleteInput(System.Management.Automation.Language.Ast ast, System.Management.Automation.Language.Token[] tokens, System.Management.Automation.Language.IScriptPosition positionOfCursor, System.Collections.Hashtable options) { return default(System.Management.Automation.CommandCompletion); }
    public static System.Management.Automation.CommandCompletion CompleteInput(System.Management.Automation.Language.Ast ast, System.Management.Automation.Language.Token[] tokens, System.Management.Automation.Language.IScriptPosition cursorPosition, System.Collections.Hashtable options, System.Management.Automation.PowerShell powershell) { return default(System.Management.Automation.CommandCompletion); }
    public static System.Management.Automation.CommandCompletion CompleteInput(string input, int cursorIndex, System.Collections.Hashtable options) { return default(System.Management.Automation.CommandCompletion); }
    public static System.Management.Automation.CommandCompletion CompleteInput(string input, int cursorIndex, System.Collections.Hashtable options, System.Management.Automation.PowerShell powershell) { return default(System.Management.Automation.CommandCompletion); }
    public System.Management.Automation.CompletionResult GetNextResult(bool forward) { return default(System.Management.Automation.CompletionResult); }
    public static System.Tuple<System.Management.Automation.Language.Ast, System.Management.Automation.Language.Token[], System.Management.Automation.Language.IScriptPosition> MapStringInputToParsedInput(string input, int cursorIndex) { return default(System.Tuple<System.Management.Automation.Language.Ast, System.Management.Automation.Language.Token[], System.Management.Automation.Language.IScriptPosition>); }
  }
  public abstract class CommandInfo {
    internal CommandInfo() { }
    public System.Management.Automation.CommandTypes CommandType { get { return default(System.Management.Automation.CommandTypes); } }
    public abstract string Definition { get; }
    public System.Management.Automation.PSModuleInfo Module { get { return default(System.Management.Automation.PSModuleInfo); } }
    public string ModuleName { get { return default(string); } }
    public string Name { get { return default(string); } }
    public abstract System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName> OutputType { get; }
    public virtual System.Collections.Generic.Dictionary<string, System.Management.Automation.ParameterMetadata> Parameters { get { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.ParameterMetadata>); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.CommandParameterSetInfo> ParameterSets { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.CommandParameterSetInfo>); } }
    public System.Management.Automation.RemotingCapability RemotingCapability { get { return default(System.Management.Automation.RemotingCapability); } }
    public virtual System.Management.Automation.SessionStateEntryVisibility Visibility { get { return default(System.Management.Automation.SessionStateEntryVisibility); } set { } }
     
    public System.Management.Automation.ParameterMetadata ResolveParameter(string name) { return default(System.Management.Automation.ParameterMetadata); }
    public override string ToString() { return default(string); }
  }
  public class CommandInvocationIntrinsics {
    internal CommandInvocationIntrinsics() { }
    public System.EventHandler<System.Management.Automation.CommandLookupEventArgs> CommandNotFoundAction { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.EventHandler<System.Management.Automation.CommandLookupEventArgs>); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public bool HasErrors { get { return default(bool); } set { } }
    public System.EventHandler<System.Management.Automation.CommandLookupEventArgs> PostCommandLookupAction { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.EventHandler<System.Management.Automation.CommandLookupEventArgs>); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public System.EventHandler<System.Management.Automation.CommandLookupEventArgs> PreCommandLookupAction { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.EventHandler<System.Management.Automation.CommandLookupEventArgs>); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
     
    public string ExpandString(string source) { return default(string); }
    public System.Management.Automation.CmdletInfo GetCmdlet(string commandName) { return default(System.Management.Automation.CmdletInfo); }
    public System.Management.Automation.CmdletInfo GetCmdletByTypeName(string cmdletTypeName) { return default(System.Management.Automation.CmdletInfo); }
    public System.Collections.Generic.List<System.Management.Automation.CmdletInfo> GetCmdlets() { return default(System.Collections.Generic.List<System.Management.Automation.CmdletInfo>); }
    public System.Collections.Generic.List<System.Management.Automation.CmdletInfo> GetCmdlets(string pattern) { return default(System.Collections.Generic.List<System.Management.Automation.CmdletInfo>); }
    public System.Management.Automation.CommandInfo GetCommand(string commandName, System.Management.Automation.CommandTypes type) { return default(System.Management.Automation.CommandInfo); }
    public System.Collections.Generic.List<string> GetCommandName(string name, bool nameIsPattern, bool returnFullName) { return default(System.Collections.Generic.List<string>); }
    public System.Collections.Generic.IEnumerable<System.Management.Automation.CommandInfo> GetCommands(string name, System.Management.Automation.CommandTypes commandTypes, bool nameIsPattern) { return default(System.Collections.Generic.IEnumerable<System.Management.Automation.CommandInfo>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> InvokeScript(bool useLocalScope, System.Management.Automation.ScriptBlock scriptBlock, System.Collections.IList input, params object[] args) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> InvokeScript(System.Management.Automation.SessionState sessionState, System.Management.Automation.ScriptBlock scriptBlock, params object[] args) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> InvokeScript(string script) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> InvokeScript(string script, bool useNewScope, System.Management.Automation.Runspaces.PipelineResultTypes writeToPipeline, System.Collections.IList input, params object[] args) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> InvokeScript(string script, params object[] args) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Management.Automation.ScriptBlock NewScriptBlock(string scriptText) { return default(System.Management.Automation.ScriptBlock); }
  }
  public class CommandLookupEventArgs : System.EventArgs {
    internal CommandLookupEventArgs() { }
    public System.Management.Automation.CommandInfo Command { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.CommandInfo); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public string CommandName { get { return default(string); } }
    public System.Management.Automation.CommandOrigin CommandOrigin { get { return default(System.Management.Automation.CommandOrigin); } }
    public System.Management.Automation.ScriptBlock CommandScriptBlock { get { return default(System.Management.Automation.ScriptBlock); } set { } }
    public bool StopSearch { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
     
  }
  [System.Diagnostics.DebuggerDisplayAttribute("CommandName = {_commandName}; Type = {CommandType}")]
  public sealed class CommandMetadata {
    public CommandMetadata(System.Management.Automation.CommandInfo commandInfo) { }
    public CommandMetadata(System.Management.Automation.CommandInfo commandInfo, bool shouldGenerateCommonParameters) { }
    public CommandMetadata(System.Management.Automation.CommandMetadata other) { }
    public CommandMetadata(string path) { }
    public CommandMetadata(System.Type commandType) { }
     
    public System.Type CommandType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Type); } }
    public System.Management.Automation.ConfirmImpact ConfirmImpact { get { return default(System.Management.Automation.ConfirmImpact); } set { } }
    public string DefaultParameterSetName { get { return default(string); } set { } }
    public string HelpUri { get { return default(string); } set { } }
    public string Name { get { return default(string); } set { } }
    public System.Collections.Generic.Dictionary<string, System.Management.Automation.ParameterMetadata> Parameters { get { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.ParameterMetadata>); } }
    public bool PositionalBinding { get { return default(bool); } set { } }
    public System.Management.Automation.RemotingCapability RemotingCapability { get { return default(System.Management.Automation.RemotingCapability); } set { } }
    public bool SupportsPaging { get { return default(bool); } set { } }
    public bool SupportsShouldProcess { get { return default(bool); } set { } }
    public bool SupportsTransactions { get { return default(bool); } set { } }
     
    public static System.Collections.Generic.Dictionary<string, System.Management.Automation.CommandMetadata> GetRestrictedCommands(System.Management.Automation.SessionCapabilities sessionCapabilities) { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.CommandMetadata>); }
  }
  public class CommandNotFoundException : System.Management.Automation.RuntimeException {
    public CommandNotFoundException() { }
    public CommandNotFoundException(string message) { }
    public CommandNotFoundException(string message, System.Exception innerException) { }
     
    public string CommandName { get { return default(string); } set { } }
    public override System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
  }
  public enum CommandOrigin {
    Internal = 1,
    Runspace = 0,
  }
  public class CommandParameterInfo {
    internal CommandParameterInfo() { }
    public System.Collections.ObjectModel.ReadOnlyCollection<string> Aliases { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<string>); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Attribute> Attributes { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Attribute>); } }
    public string HelpMessage { get { return default(string); } }
    public bool IsDynamic { get { return default(bool); } }
    public bool IsMandatory { get { return default(bool); } }
    public string Name { get { return default(string); } }
    public System.Type ParameterType { get { return default(System.Type); } }
    public int Position { get { return default(int); } }
    public bool ValueFromPipeline { get { return default(bool); } }
    public bool ValueFromPipelineByPropertyName { get { return default(bool); } }
    public bool ValueFromRemainingArguments { get { return default(bool); } }
     
  }
  public class CommandParameterSetInfo {
    internal CommandParameterSetInfo() { }
    public bool IsDefault { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } }
    public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.CommandParameterInfo> Parameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.CommandParameterInfo>); } }
     
    public override string ToString() { return default(string); }
  }
  [System.FlagsAttribute]
  public enum CommandTypes {
    Alias = 1,
    All = 255,
    Application = 32,
    Cmdlet = 8,
    ExternalScript = 16,
    Filter = 4,
    Function = 2,
    Script = 64,
    Workflow = 128,
  }
  public static class CompletionCompleters {
    public static System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult> CompleteCommand(string commandName) { return default(System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult>); }
    public static System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult> CompleteCommand(string commandName, string moduleName, System.Management.Automation.CommandTypes commandTypes=(System.Management.Automation.CommandTypes)(255)) { return default(System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult>); }
    public static System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult> CompleteFilename(string fileName) { return default(System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult>); }
    public static System.Collections.Generic.List<System.Management.Automation.CompletionResult> CompleteOperator(string wordToComplete) { return default(System.Collections.Generic.List<System.Management.Automation.CompletionResult>); }
    public static System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult> CompleteType(string typeName) { return default(System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult>); }
    public static System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult> CompleteVariable(string variableName) { return default(System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult>); }
  }
  public class CompletionResult {
    public CompletionResult(string completionText) { }
    public CompletionResult(string completionText, string listItemText, System.Management.Automation.CompletionResultType resultType, string toolTip) { }
     
    public string CompletionText { get { return default(string); } }
    public string ListItemText { get { return default(string); } }
    public System.Management.Automation.CompletionResultType ResultType { get { return default(System.Management.Automation.CompletionResultType); } }
    public string ToolTip { get { return default(string); } }
     
  }
  public enum CompletionResultType {
    Command = 2,
    History = 1,
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
  public enum ConfirmImpact {
    High = 3,
    Low = 1,
    Medium = 2,
    None = 0,
  }
  public sealed class ContainerParentJob : System.Management.Automation.Job2 {
    public ContainerParentJob(string command) { }
    public ContainerParentJob(string command, string name) { }
    public ContainerParentJob(string command, string name, System.Guid instanceId) { }
    public ContainerParentJob(string command, string name, System.Guid instanceId, string jobType) { }
    public ContainerParentJob(string command, string name, System.Management.Automation.JobIdentifier jobId) { }
    public ContainerParentJob(string command, string name, System.Management.Automation.JobIdentifier jobId, string jobType) { }
    public ContainerParentJob(string command, string name, string jobType) { }
     
    public override bool HasMoreData { get { return default(bool); } }
    public override string Location { get { return default(string); } }
    public override string StatusMessage { get { return default(string); } }
     
    public void AddChildJob(System.Management.Automation.Job2 childJob) { }
    protected override void Dispose(bool disposing) { }
    public override void ResumeJob() { }
    public override void ResumeJobAsync() { }
    public override void StartJob() { }
    public override void StartJobAsync() { }
    public override void StopJob() { }
    public override void StopJob(bool force, string reason) { }
    public override void StopJobAsync() { }
    public override void StopJobAsync(bool force, string reason) { }
    public override void SuspendJob() { }
    public override void SuspendJob(bool force, string reason) { }
    public override void SuspendJobAsync() { }
    public override void SuspendJobAsync(bool force, string reason) { }
    public override void UnblockJob() { }
    public override void UnblockJobAsync() { }
  }
  public sealed class ContentCmdletProviderIntrinsics {
    internal ContentCmdletProviderIntrinsics() { }
    public void Clear(string path) { }
    public void Clear(string[] path, bool force, bool literalPath) { }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.Provider.IContentReader> GetReader(string path) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.Provider.IContentReader>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.Provider.IContentReader> GetReader(string[] path, bool force, bool literalPath) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.Provider.IContentReader>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.Provider.IContentWriter> GetWriter(string path) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.Provider.IContentWriter>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.Provider.IContentWriter> GetWriter(string[] path, bool force, bool literalPath) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.Provider.IContentWriter>); }
  }
#if CONVERT_THROUGH_STRING
  public class ConvertThroughString : System.Management.Automation.PSTypeConverter {
    public ConvertThroughString() { }
     
    public override bool CanConvertFrom(object sourceValue, System.Type destinationType) { return default(bool); }
    public override bool CanConvertTo(object sourceValue, System.Type destinationType) { return default(bool); }
    public override object ConvertFrom(object sourceValue, System.Type destinationType, System.IFormatProvider formatProvider, bool ignoreCase) { return default(object); }
    public override object ConvertTo(object sourceValue, System.Type destinationType, System.IFormatProvider formatProvider, bool ignoreCase) { return default(object); }
  }
#endif
  public enum CopyContainers {
    CopyChildrenOfTargetContainer = 1,
    CopyTargetContainer = 0,
  }
  [System.AttributeUsageAttribute((AttributeTargets)384, AllowMultiple=false)]
  public sealed class CredentialAttribute : System.Management.Automation.ArgumentTransformationAttribute {
    public CredentialAttribute() { }
     
    public override object Transform(System.Management.Automation.EngineIntrinsics engineIntrinsics, object inputData) { return default(object); }
  }
  public sealed class DataAddedEventArgs : System.EventArgs {
    internal DataAddedEventArgs() { }
    public int Index { get { return default(int); } }
    public System.Guid PowerShellInstanceId { get { return default(System.Guid); } }
     
  }
  public sealed class DataAddingEventArgs : System.EventArgs {
    internal DataAddingEventArgs() { }
    public object ItemAdded { get { return default(object); } }
    public System.Guid PowerShellInstanceId { get { return default(System.Guid); } }
     
  }
  public sealed class Debugger {
    internal Debugger() { }
    // Events
    public event System.EventHandler<System.Management.Automation.BreakpointUpdatedEventArgs> BreakpointUpdated { add { } remove { } }
    public event System.EventHandler<System.Management.Automation.DebuggerStopEventArgs> DebuggerStop { add { } remove { } }
     
  }
  public enum DebuggerResumeAction {
    Continue = 0,
    StepInto = 1,
    StepOut = 2,
    StepOver = 3,
    Stop = 4,
  }
  public class DebuggerStopEventArgs : System.EventArgs {
    internal DebuggerStopEventArgs() { }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Breakpoint> Breakpoints { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Breakpoint>); } }
    public System.Management.Automation.InvocationInfo InvocationInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.InvocationInfo); } }
    public System.Management.Automation.DebuggerResumeAction ResumeAction { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.DebuggerResumeAction); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
     
  }
  public class DebugRecord : System.Management.Automation.InformationalRecord {
    public DebugRecord(System.Management.Automation.PSObject record) { }
    public DebugRecord(string message) { }
  }
#if DEFAULT_PARAM_DICTIONARY
  public sealed class DefaultParameterDictionary : System.Collections.Hashtable {
    public DefaultParameterDictionary() { }
    public DefaultParameterDictionary(System.Collections.IDictionary dictionary) { }
     
    public override object this[object key] { get { return default(object); } set { } }
     
    public override void Add(object key, object value) { }
    public bool ChangeSinceLastCheck() { return default(bool); }
    public override void Clear() { }
    public override void Remove(object key) { }
  }
#endif
#if FORMAT_API
  public sealed class DisplayEntry {
    public DisplayEntry(string value, System.Management.Automation.DisplayEntryValueType type) { }
     
    public string Value { get { return default(string); } }
    public System.Management.Automation.DisplayEntryValueType ValueType { get { return default(System.Management.Automation.DisplayEntryValueType); } }
     
    public override string ToString() { return default(string); }
  }
  public enum DisplayEntryValueType {
    Property = 0,
    ScriptBlock = 1,
  }
#endif
  public sealed class DriveManagementIntrinsics {
    internal DriveManagementIntrinsics() { }
    public System.Management.Automation.PSDriveInfo Current { get { return default(System.Management.Automation.PSDriveInfo); } }
     
    public System.Management.Automation.PSDriveInfo Get(string driveName) { return default(System.Management.Automation.PSDriveInfo); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo> GetAll() { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo> GetAllAtScope(string scope) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo> GetAllForProvider(string providerName) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo>); }
    public System.Management.Automation.PSDriveInfo GetAtScope(string driveName, string scope) { return default(System.Management.Automation.PSDriveInfo); }
    public System.Management.Automation.PSDriveInfo New(System.Management.Automation.PSDriveInfo drive, string scope) { return default(System.Management.Automation.PSDriveInfo); }
    public void Remove(string driveName, bool force, string scope) { }
  }
  public class DriveNotFoundException : System.Management.Automation.SessionStateException {
    public DriveNotFoundException() { }
    public DriveNotFoundException(string message) { }
    public DriveNotFoundException(string message, System.Exception innerException) { }
  }
  public class EngineIntrinsics {
    internal EngineIntrinsics() { }
    public System.Management.Automation.PSEventManager Events { get { return default(System.Management.Automation.PSEventManager); } }
    public System.Management.Automation.Host.PSHost Host { get { return default(System.Management.Automation.Host.PSHost); } }
    public System.Management.Automation.CommandInvocationIntrinsics InvokeCommand { get { return default(System.Management.Automation.CommandInvocationIntrinsics); } }
    public System.Management.Automation.ProviderIntrinsics InvokeProvider { get { return default(System.Management.Automation.ProviderIntrinsics); } }
    public System.Management.Automation.SessionState SessionState { get { return default(System.Management.Automation.SessionState); } }
     
  }
  public enum ErrorCategory {
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
  public class ErrorCategoryInfo {
    internal ErrorCategoryInfo() { }
    public string Activity { get { return default(string); } set { } }
    public System.Management.Automation.ErrorCategory Category { get { return default(System.Management.Automation.ErrorCategory); } }
    public string Reason { get { return default(string); } set { } }
    public string TargetName { get { return default(string); } set { } }
    public string TargetType { get { return default(string); } set { } }
     
    public string GetMessage() { return default(string); }
    public string GetMessage(System.Globalization.CultureInfo uiCultureInfo) { return default(string); }
    public override string ToString() { return default(string); }
  }
  public class ErrorDetails {
    public ErrorDetails(System.Management.Automation.Cmdlet cmdlet, string baseName, string resourceId, params object[] args) { }
    public ErrorDetails(System.Management.Automation.IResourceSupplier resourceSupplier, string baseName, string resourceId, params object[] args) { }
    public ErrorDetails(System.Reflection.Assembly assembly, string baseName, string resourceId, params object[] args) { }
    public ErrorDetails(string message) { }
     
    public string Message { get { return default(string); } }
    public string RecommendedAction { get { return default(string); } set { } }
     
    public override string ToString() { return default(string); }
  }
  public class ErrorRecord
  {
    public ErrorRecord(System.Exception exception, string errorId, System.Management.Automation.ErrorCategory errorCategory, object targetObject) { }
    public ErrorRecord(System.Management.Automation.ErrorRecord errorRecord, System.Exception replaceParentContainsErrorRecordException) { }
     
    public System.Management.Automation.ErrorCategoryInfo CategoryInfo { get { return default(System.Management.Automation.ErrorCategoryInfo); } }
    public System.Management.Automation.ErrorDetails ErrorDetails { get { return default(System.Management.Automation.ErrorDetails); } set { } }
    public System.Exception Exception { get { return default(System.Exception); } }
    public string FullyQualifiedErrorId { get { return default(string); } }
    public System.Management.Automation.InvocationInfo InvocationInfo { get { return default(System.Management.Automation.InvocationInfo); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<int> PipelineIterationInfo { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<int>); } }
    public string ScriptStackTrace { get { return default(string); } }
    public object TargetObject { get { return default(object); } }
     
    public override string ToString() { return default(string); }
  }
  public sealed class ExtendedTypeDefinition {
    public ExtendedTypeDefinition(string typeName) { }
    public ExtendedTypeDefinition(string typeName, System.Collections.Generic.IEnumerable<System.Management.Automation.FormatViewDefinition> viewDefinitions) { }
     
    public System.Collections.Generic.List<System.Management.Automation.FormatViewDefinition> FormatViewDefinition { get { return default(System.Collections.Generic.List<System.Management.Automation.FormatViewDefinition>); } }
    public string TypeName { get { return default(string); } }
     
    public override string ToString() { return default(string); }
  }
  public class ExtendedTypeSystemException : System.Management.Automation.RuntimeException {
    public ExtendedTypeSystemException() { }
    public ExtendedTypeSystemException(string message) { }
    public ExtendedTypeSystemException(string message, System.Exception innerException) { }
  }
  public class ExternalScriptInfo : System.Management.Automation.CommandInfo {
    internal ExternalScriptInfo() { }
    public override string Definition { get { return default(string); } }
    public System.Text.Encoding OriginalEncoding { get { return default(System.Text.Encoding); } }
    public override System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName> OutputType { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName>); } }
    public string Path { get { return default(string); } }
    public System.Management.Automation.ScriptBlock ScriptBlock { get { return default(System.Management.Automation.ScriptBlock); } }
    public string ScriptContents { get { return default(string); } }
    public override System.Management.Automation.SessionStateEntryVisibility Visibility { get { return default(System.Management.Automation.SessionStateEntryVisibility); } set { } }
     
    public void ValidateScriptInfo(System.Management.Automation.Host.PSHost host) { }
  }
#if FILTER_INFO
  public class FilterInfo : System.Management.Automation.FunctionInfo {
    internal FilterInfo() { }
  }
#endif
  public sealed class FlagsExpression<T> where T : struct, System.IConvertible {
    public FlagsExpression(object[] expression) { }
    public FlagsExpression(string expression) { }
     
    public bool Evaluate(T value) { return default(bool); }
  }
  public sealed class FormatViewDefinition {
    public FormatViewDefinition(string name, System.Management.Automation.PSControl control) { }
     
    public System.Management.Automation.PSControl Control { get { return default(System.Management.Automation.PSControl); } }
    public string Name { get { return default(string); } }
     
    public override string ToString() { return default(string); }
  }
  public class ForwardedEventArgs : System.EventArgs {
    internal ForwardedEventArgs() { }
    public System.Management.Automation.PSObject SerializedRemoteEventArgs { get { return default(System.Management.Automation.PSObject); } }
     
  }
  public class FunctionInfo : System.Management.Automation.CommandInfo {
    internal FunctionInfo() { }
    public bool CmdletBinding { get { return default(bool); } }
    public string DefaultParameterSet { get { return default(string); } }
    public override string Definition { get { return default(string); } }
    public string Description { get { return default(string); } set { } }
    public string HelpFile { get { return default(string); } }
    public string Noun { get { return default(string); } }
    public System.Management.Automation.ScopedItemOptions Options { get { return default(System.Management.Automation.ScopedItemOptions); } set { } }
    public override System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName> OutputType { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName>); } }
    public System.Management.Automation.ScriptBlock ScriptBlock { get { return default(System.Management.Automation.ScriptBlock); } }
    public string Verb { get { return default(string); } }
     
    protected internal virtual void Update(System.Management.Automation.FunctionInfo newFunction, bool force, System.Management.Automation.ScopedItemOptions options, string helpFile) { }
  }
  public class GettingValueExceptionEventArgs : System.EventArgs {
    internal GettingValueExceptionEventArgs() { }
    public System.Exception Exception { get { return default(System.Exception); } }
    public bool ShouldThrow { get { return default(bool); } set { } }
    public object ValueReplacement { get { return default(object); } set { } }
     
  }
  public class GetValueException : System.Management.Automation.ExtendedTypeSystemException {
    public GetValueException() { }
    public GetValueException(string message) { }
    public GetValueException(string message, System.Exception innerException) { }
  }
  public class GetValueInvocationException : System.Management.Automation.GetValueException {
    public GetValueInvocationException() { }
    public GetValueInvocationException(string message) { }
    public GetValueInvocationException(string message, System.Exception innerException) { }
  }
  public class HaltCommandException : System.Exception {
    public HaltCommandException() { }
    public HaltCommandException(string message) { }
    public HaltCommandException(string message, System.Exception innerException) { }
  }
  public partial interface IBackgroundDispatcher {
    System.IAsyncResult BeginInvoke(System.Threading.WaitCallback callback, object state, System.AsyncCallback completionCallback, object asyncState);
    void EndInvoke(System.IAsyncResult asyncResult);
    bool QueueUserWorkItem(System.Threading.WaitCallback callback);
    bool QueueUserWorkItem(System.Threading.WaitCallback callback, object state);
  }
  public partial interface ICommandRuntime {
#if TRANSACTIONS
    System.Management.Automation.PSTransactionContext CurrentPSTransaction { get; }
#endif
    System.Management.Automation.Host.PSHost Host { get; }
     
    bool ShouldContinue(string query, string caption);
    bool ShouldContinue(string query, string caption, ref bool yesToAll, ref bool noToAll);
    bool ShouldProcess(string target);
    bool ShouldProcess(string target, string action);
    bool ShouldProcess(string verboseDescription, string verboseWarning, string caption);
    bool ShouldProcess(string verboseDescription, string verboseWarning, string caption, out System.Management.Automation.ShouldProcessReason shouldProcessReason);
    void ThrowTerminatingError(System.Management.Automation.ErrorRecord errorRecord);
    bool TransactionAvailable();
    void WriteCommandDetail(string text);
    void WriteDebug(string text);
    void WriteError(System.Management.Automation.ErrorRecord errorRecord);
    void WriteObject(object sendToPipeline);
    void WriteObject(object sendToPipeline, bool enumerateCollection);
    void WriteProgress(long sourceId, System.Management.Automation.ProgressRecord progressRecord);
    void WriteProgress(System.Management.Automation.ProgressRecord progressRecord);
    void WriteVerbose(string text);
    void WriteWarning(string text);
  }
  public partial interface IContainsErrorRecord {
    System.Management.Automation.ErrorRecord ErrorRecord { get; }
     
  }
  public partial interface IDynamicParameters {
    object GetDynamicParameters();
  }
  public partial interface IModuleAssemblyInitializer {
    void OnImport();
  }
  public class IncompleteParseException : System.Management.Automation.ParseException {
    public IncompleteParseException() { }
    public IncompleteParseException(string message) { }
    public IncompleteParseException(string message, System.Exception innerException) { }
  }
  public abstract class InformationalRecord {
    internal InformationalRecord() { }
    public System.Management.Automation.InvocationInfo InvocationInfo { get { return default(System.Management.Automation.InvocationInfo); } }
    public string Message { get { return default(string); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<int> PipelineIterationInfo { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<int>); } }
     
    public override string ToString() { return default(string); }
  }
  public class InvalidJobStateException : System.Exception {
    public InvalidJobStateException() { }
    public InvalidJobStateException(System.Management.Automation.JobState currentState, string actionMessage) { }
    public InvalidJobStateException(string message) { }
    public InvalidJobStateException(string message, System.Exception innerException) { }
     
    public System.Management.Automation.JobState CurrentState { get { return default(System.Management.Automation.JobState); } }
  }
  public class InvalidPowerShellStateException : System.Exception {
    public InvalidPowerShellStateException() { }
    public InvalidPowerShellStateException(string message) { }
    public InvalidPowerShellStateException(string message, System.Exception innerException) { }
     
    public System.Management.Automation.PSInvocationState CurrentState { get { return default(System.Management.Automation.PSInvocationState); } }
  }
  [System.Diagnostics.DebuggerDisplayAttribute("Command = {_commandInfo}")]
  public class InvocationInfo {
    internal InvocationInfo() { }
    public System.Collections.Generic.Dictionary<string, object> BoundParameters { get { return default(System.Collections.Generic.Dictionary<string, object>); } }
    public System.Management.Automation.CommandOrigin CommandOrigin { get { return default(System.Management.Automation.CommandOrigin); } }
    public System.Management.Automation.Language.IScriptExtent DisplayScriptPosition { get { return default(System.Management.Automation.Language.IScriptExtent); } set { } }
    public bool ExpectingInput { get { return default(bool); } }
    public long HistoryId { get { return default(long); } }
    public string InvocationName { get { return default(string); } }
    public string Line { get { return default(string); } }
    public System.Management.Automation.CommandInfo MyCommand { get { return default(System.Management.Automation.CommandInfo); } }
    public int OffsetInLine { get { return default(int); } }
    public int PipelineLength { get { return default(int); } }
    public int PipelinePosition { get { return default(int); } }
    public string PositionMessage { get { return default(string); } }
    public string PSCommandPath { get { return default(string); } }
    public string PSScriptRoot { get { return default(string); } }
    public int ScriptLineNumber { get { return default(int); } }
    public string ScriptName { get { return default(string); } }
    public System.Collections.Generic.List<object> UnboundArguments { get { return default(System.Collections.Generic.List<object>); } }
     
  }
  public partial interface IResourceSupplier {
    string GetResourceString(string baseName, string resourceId);
  }
  public sealed class ItemCmdletProviderIntrinsics {
    internal ItemCmdletProviderIntrinsics() { }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Clear(string path) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Clear(string[] path, bool force, bool literalPath) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Copy(string path, string destinationPath, bool recurse, System.Management.Automation.CopyContainers copyContainers) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Copy(string[] path, string destinationPath, bool recurse, System.Management.Automation.CopyContainers copyContainers, bool force, bool literalPath) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public bool Exists(string path) { return default(bool); }
    public bool Exists(string path, bool force, bool literalPath) { return default(bool); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Get(string path) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Get(string[] path, bool force, bool literalPath) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public void Invoke(string path) { }
    public void Invoke(string[] path, bool literalPath) { }
    public bool IsContainer(string path) { return default(bool); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Move(string path, string destination) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Move(string[] path, string destination, bool force, bool literalPath) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> New(string path, string name, string itemTypeName, object content) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> New(string[] path, string name, string itemTypeName, object content, bool force) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public void Remove(string path, bool recurse) { }
    public void Remove(string[] path, bool recurse, bool force, bool literalPath) { }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Rename(string path, string newName) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Rename(string path, string newName, bool force) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Set(string path, object value) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Set(string[] path, object value, bool force, bool literalPath) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
  }
  public class ItemNotFoundException : System.Management.Automation.SessionStateException {
    public ItemNotFoundException() { }
    public ItemNotFoundException(string message) { }
    public ItemNotFoundException(string message, System.Exception innerException) { }
  }
  public abstract class Job : System.IDisposable {
    protected Job() { }
    protected Job(string command) { }
    protected Job(string command, string name) { }
    protected Job(string command, string name, System.Collections.Generic.IList<System.Management.Automation.Job> childJobs) { }
    protected Job(string command, string name, System.Guid instanceId) { }
    protected Job(string command, string name, System.Management.Automation.JobIdentifier token) { }
     
    public System.Collections.Generic.IList<System.Management.Automation.Job> ChildJobs { get { return default(System.Collections.Generic.IList<System.Management.Automation.Job>); } }
    public string Command { get { return default(string); } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.DebugRecord> Debug { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.DebugRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.ErrorRecord> Error { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.ErrorRecord>); } set { } }
    public System.Threading.WaitHandle Finished { get { return default(System.Threading.WaitHandle); } }
    public abstract bool HasMoreData { get; }
    public int Id { get { return default(int); } }
    public System.Guid InstanceId { get { return default(System.Guid); } }
    public System.Management.Automation.JobStateInfo JobStateInfo { get { return default(System.Management.Automation.JobStateInfo); } }
    public abstract string Location { get; }
    public string Name { get { return default(string); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.PSObject> Output { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.PSObject>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.ProgressRecord> Progress { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.ProgressRecord>); } set { } }
    public System.Nullable<System.DateTime> PSBeginTime { get { return default(System.Nullable<System.DateTime>); } protected set { } }
    public System.Nullable<System.DateTime> PSEndTime { get { return default(System.Nullable<System.DateTime>); } protected set { } }
    public string PSJobTypeName { get { return default(string); } protected internal set { } }
    public abstract string StatusMessage { get; }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.VerboseRecord> Verbose { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.VerboseRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.WarningRecord> Warning { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.WarningRecord>); } set { } }
     
    // Events
    public event System.EventHandler<System.Management.Automation.JobStateEventArgs> StateChanged { add { } remove { } }
     
    protected string AutoGenerateJobName() { return default(string); }
    public void Dispose() { }
    protected virtual void Dispose(bool disposing) { }
    protected virtual void DoLoadJobStreams() { }
    protected virtual void DoUnloadJobStreams() { }
    public void LoadJobStreams() { }
    protected void SetJobState(System.Management.Automation.JobState state) { }
    public abstract void StopJob();
    public void UnloadJobStreams() { }
  }
  public abstract class Job2 : System.Management.Automation.Job {
    protected Job2() { }
    protected Job2(string command) { }
    protected Job2(string command, string name) { }
    protected Job2(string command, string name, System.Collections.Generic.IList<System.Management.Automation.Job> childJobs) { }
    protected Job2(string command, string name, System.Guid instanceId) { }
    protected Job2(string command, string name, System.Management.Automation.JobIdentifier token) { }
     
    public System.Collections.Generic.List<System.Management.Automation.Runspaces.CommandParameterCollection> StartParameters { get { return default(System.Collections.Generic.List<System.Management.Automation.Runspaces.CommandParameterCollection>); } set { } }
    protected object SyncRoot { get { return default(object); } }
     
    // Events
    public event System.EventHandler<System.ComponentModel.AsyncCompletedEventArgs> ResumeJobCompleted { add { } remove { } }
    public event System.EventHandler<System.ComponentModel.AsyncCompletedEventArgs> StartJobCompleted { add { } remove { } }
    public event System.EventHandler<System.ComponentModel.AsyncCompletedEventArgs> StopJobCompleted { add { } remove { } }
    public event System.EventHandler<System.ComponentModel.AsyncCompletedEventArgs> SuspendJobCompleted { add { } remove { } }
    public event System.EventHandler<System.ComponentModel.AsyncCompletedEventArgs> UnblockJobCompleted { add { } remove { } }
     
    protected virtual void OnResumeJobCompleted(System.ComponentModel.AsyncCompletedEventArgs eventArgs) { }
    protected virtual void OnStartJobCompleted(System.ComponentModel.AsyncCompletedEventArgs eventArgs) { }
    protected virtual void OnStopJobCompleted(System.ComponentModel.AsyncCompletedEventArgs eventArgs) { }
    protected virtual void OnSuspendJobCompleted(System.ComponentModel.AsyncCompletedEventArgs eventArgs) { }
    protected virtual void OnUnblockJobCompleted(System.ComponentModel.AsyncCompletedEventArgs eventArgs) { }
    public abstract void ResumeJob();
    public abstract void ResumeJobAsync();
    protected void SetJobState(System.Management.Automation.JobState state, System.Exception reason) { }
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
  public sealed class JobDataAddedEventArgs : System.EventArgs {
    internal JobDataAddedEventArgs() { }
    public System.Management.Automation.PowerShellStreamType DataType { get { return default(System.Management.Automation.PowerShellStreamType); } }
    public int Index { get { return default(int); } }
    public System.Management.Automation.Job SourceJob { get { return default(System.Management.Automation.Job); } }
     
  }
  public class JobDefinition
  {
    public JobDefinition(System.Type jobSourceAdapterType, string command, string name) { }
     
    public string Command { get { return default(string); } }
    public System.Management.Automation.CommandInfo CommandInfo { get { return default(System.Management.Automation.CommandInfo); } }
    public System.Guid InstanceId { get { return default(System.Guid); } set { } }
    public System.Type JobSourceAdapterType { get { return default(System.Type); } }
    public string JobSourceAdapterTypeName { get { return default(string); } set { } }
    public string ModuleName { get { return default(string); } set { } }
    public string Name { get { return default(string); } set { } }
     
    public virtual void Load(System.IO.Stream stream) { }
    public virtual void Save(System.IO.Stream stream) { }
  }
  public class JobFailedException : System.Exception {
    public JobFailedException() { }
    public JobFailedException(System.Exception innerException, System.Management.Automation.Language.ScriptExtent displayScriptPosition) { }
    public JobFailedException(string message) { }
    public JobFailedException(string message, System.Exception innerException) { }
     
    public System.Management.Automation.Language.ScriptExtent DisplayScriptPosition { get { return default(System.Management.Automation.Language.ScriptExtent); } }
    public override string Message { get { return default(string); } }
    public System.Exception Reason { get { return default(System.Exception); } }
  }
  public sealed class JobIdentifier {
    internal JobIdentifier() { }
  }
  public class JobInvocationInfo {
    protected JobInvocationInfo() { }
    public JobInvocationInfo(System.Management.Automation.JobDefinition definition, System.Collections.Generic.Dictionary<string, object> parameters) { }
    public JobInvocationInfo(System.Management.Automation.JobDefinition definition, System.Collections.Generic.IEnumerable<System.Collections.Generic.Dictionary<string, object>> parameterCollectionList) { }
    public JobInvocationInfo(System.Management.Automation.JobDefinition definition, System.Collections.Generic.IEnumerable<System.Management.Automation.Runspaces.CommandParameterCollection> parameters) { }
    public JobInvocationInfo(System.Management.Automation.JobDefinition definition, System.Management.Automation.Runspaces.CommandParameterCollection parameters) { }
     
    public string Command { get { return default(string); } set { } }
    public System.Management.Automation.JobDefinition Definition { get { return default(System.Management.Automation.JobDefinition); } set { } }
    public System.Guid InstanceId { get { return default(System.Guid); } }
    public string Name { get { return default(string); } set { } }
    public System.Collections.Generic.List<System.Management.Automation.Runspaces.CommandParameterCollection> Parameters { get { return default(System.Collections.Generic.List<System.Management.Automation.Runspaces.CommandParameterCollection>); } }
     
    public virtual void Load(System.IO.Stream stream) { }
    public virtual void Save(System.IO.Stream stream) { }
  }
  public sealed class JobManager {
    internal JobManager() { }
    public bool IsRegistered(string typeName) { return default(bool); }
    public System.Management.Automation.Job2 NewJob(System.Management.Automation.JobDefinition definition) { return default(System.Management.Automation.Job2); }
    public System.Management.Automation.Job2 NewJob(System.Management.Automation.JobInvocationInfo specification) { return default(System.Management.Automation.Job2); }
    public void PersistJob(System.Management.Automation.Job2 job, System.Management.Automation.JobDefinition definition) { }
  }
  public class JobRepository : System.Management.Automation.Repository<System.Management.Automation.Job> {
    internal JobRepository() : base (default(string)) { }
    public System.Collections.Generic.List<System.Management.Automation.Job> Jobs { get { return default(System.Collections.Generic.List<System.Management.Automation.Job>); } }
     
    public System.Management.Automation.Job GetJob(System.Guid instanceId) { return default(System.Management.Automation.Job); }
    protected override System.Guid GetKey(System.Management.Automation.Job item) { return default(System.Guid); }
  }
  public abstract class JobSourceAdapter {
    protected JobSourceAdapter() { }
     
    public string Name { get { return default(string); } set { } }
     
    public abstract System.Management.Automation.Job2 GetJobByInstanceId(System.Guid instanceId, bool recurse);
    public abstract System.Management.Automation.Job2 GetJobBySessionId(int id, bool recurse);
    public abstract System.Collections.Generic.IList<System.Management.Automation.Job2> GetJobs();
    public abstract System.Collections.Generic.IList<System.Management.Automation.Job2> GetJobsByCommand(string command, bool recurse);
    public abstract System.Collections.Generic.IList<System.Management.Automation.Job2> GetJobsByFilter(System.Collections.Generic.Dictionary<string, object> filter, bool recurse);
    public abstract System.Collections.Generic.IList<System.Management.Automation.Job2> GetJobsByName(string name, bool recurse);
    public abstract System.Collections.Generic.IList<System.Management.Automation.Job2> GetJobsByState(System.Management.Automation.JobState state, bool recurse);
    public System.Management.Automation.Job2 NewJob(System.Management.Automation.JobDefinition definition) { return default(System.Management.Automation.Job2); }
    public abstract System.Management.Automation.Job2 NewJob(System.Management.Automation.JobInvocationInfo specification);
    public virtual System.Management.Automation.Job2 NewJob(string definitionName, string definitionPath) { return default(System.Management.Automation.Job2); }
    public virtual void PersistJob(System.Management.Automation.Job2 job) { }
    public abstract void RemoveJob(System.Management.Automation.Job2 job);
    protected System.Management.Automation.JobIdentifier RetrieveJobIdForReuse(System.Guid instanceId) { return default(System.Management.Automation.JobIdentifier); }
    public void StoreJobIdForReuse(System.Management.Automation.Job2 job, bool recurse) { }
  }
  public enum JobState {
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
  public sealed class JobStateEventArgs : System.EventArgs {
    public JobStateEventArgs(System.Management.Automation.JobStateInfo jobStateInfo) { }
    public JobStateEventArgs(System.Management.Automation.JobStateInfo jobStateInfo, System.Management.Automation.JobStateInfo previousJobStateInfo) { }
     
    public System.Management.Automation.JobStateInfo JobStateInfo { get { return default(System.Management.Automation.JobStateInfo); } }
    public System.Management.Automation.JobStateInfo PreviousJobStateInfo { get { return default(System.Management.Automation.JobStateInfo); } }
     
  }
  public sealed class JobStateInfo {
    public JobStateInfo(System.Management.Automation.JobState state) { }
    public JobStateInfo(System.Management.Automation.JobState state, System.Exception reason) { }
     
    public System.Exception Reason { get { return default(System.Exception); } }
    public System.Management.Automation.JobState State { get { return default(System.Management.Automation.JobState); } }
     
    public override string ToString() { return default(string); }
  }
  public enum JobThreadOptions {
    Default = 0,
    UseNewThread = 2,
    UseThreadPoolThread = 1,
  }
  public static class LanguagePrimitives {
    public static int Compare(object first, object second) { return default(int); }
    public static int Compare(object first, object second, bool ignoreCase) { return default(int); }
    public static int Compare(object first, object second, bool ignoreCase, System.IFormatProvider formatProvider) { return default(int); }
    public static object ConvertTo(object valueToConvert, System.Type resultType) { return default(object); }
    public static object ConvertTo(object valueToConvert, System.Type resultType, System.IFormatProvider formatProvider) { return default(object); }
    public static T ConvertTo<T>(object valueToConvert) { return default(T); }
    public static new bool Equals(object first, object second) { return default(bool); }
    public static bool Equals(object first, object second, bool ignoreCase) { return default(bool); }
    public static bool Equals(object first, object second, bool ignoreCase, System.IFormatProvider formatProvider) { return default(bool); }
    public static System.Collections.IEnumerable GetEnumerable(object obj) { return default(System.Collections.IEnumerable); }
    public static System.Collections.IEnumerator GetEnumerator(object obj) { return default(System.Collections.IEnumerator); }
    public static System.Management.Automation.PSDataCollection<System.Management.Automation.PSObject> GetPSDataCollection(object inputValue) { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.PSObject>); }
    public static bool IsTrue(object obj) { return default(bool); }
    public static bool TryConvertTo(object valueToConvert, System.Type resultType, System.IFormatProvider formatProvider, out object result) { result = default(object); return default(bool); }
    public static bool TryConvertTo(object valueToConvert, System.Type resultType, out object result) { result = default(object); return default(bool); }
    public static bool TryConvertTo<T>(object valueToConvert, out T result) { result = default(T); return default(bool); }
    public static bool TryConvertTo<T>(object valueToConvert, System.IFormatProvider formatProvider, out T result) { result = default(T); return default(bool); }
  }
  public class LineBreakpoint : System.Management.Automation.Breakpoint {
    internal LineBreakpoint() { }
    public int Column { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(int); } }
    public int Line { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(int); } }
     
    public override string ToString() { return default(string); }
  }
#if FORMAT_API
    public sealed class ListControl : System.Management.Automation.PSControl {
    public ListControl() { }
    public ListControl(System.Collections.Generic.IEnumerable<System.Management.Automation.ListControlEntry> entries) { }
     
    public System.Collections.Generic.List<System.Management.Automation.ListControlEntry> Entries { get { return default(System.Collections.Generic.List<System.Management.Automation.ListControlEntry>); } }
     
    public override string ToString() { return default(string); }
  }
  public sealed class ListControlEntry {
    public ListControlEntry() { }
    public ListControlEntry(System.Collections.Generic.IEnumerable<System.Management.Automation.ListControlEntryItem> listItems) { }
    public ListControlEntry(System.Collections.Generic.IEnumerable<System.Management.Automation.ListControlEntryItem> listItems, System.Collections.Generic.IEnumerable<string> selectedBy) { }
     
    public System.Collections.Generic.List<System.Management.Automation.ListControlEntryItem> Items { get { return default(System.Collections.Generic.List<System.Management.Automation.ListControlEntryItem>); } }
    public System.Collections.Generic.List<string> SelectedBy { get { return default(System.Collections.Generic.List<string>); } }
     
  }
  public sealed class ListControlEntryItem {
    public ListControlEntryItem(string label, System.Management.Automation.DisplayEntry entry) { }
     
    public System.Management.Automation.DisplayEntry DisplayEntry { get { return default(System.Management.Automation.DisplayEntry); } }
    public string Label { get { return default(string); } }
     
  }
#endif
  public class MetadataException : System.Management.Automation.RuntimeException {
    public MetadataException() { }
    public MetadataException(string message) { }
    public MetadataException(string message, System.Exception innerException) { }
  }
  public class MethodException : System.Management.Automation.ExtendedTypeSystemException {
    public MethodException() { }
    public MethodException(string message) { }
    public MethodException(string message, System.Exception innerException) { }
  }
  public class MethodInvocationException : System.Management.Automation.MethodException {
    public MethodInvocationException() { }
    public MethodInvocationException(string message) { }
    public MethodInvocationException(string message, System.Exception innerException) { }
  }
  public enum ModuleAccessMode {
    Constant = 2,
    ReadOnly = 1,
    ReadWrite = 0,
  }
  public enum ModuleType {
    Binary = 1,
    Cim = 3,
    Manifest = 2,
    Script = 0,
    Workflow = 4,
  }
  [System.AttributeUsageAttribute((AttributeTargets)4, AllowMultiple=true)]
  public sealed class OutputTypeAttribute : System.Management.Automation.Internal.CmdletMetadataAttribute {
    public OutputTypeAttribute(params string[] type) { }
    public OutputTypeAttribute(params System.Type[] type) { }
     
    public string[] ParameterSetName { get { return default(string[]); } set { } }
    public string ProviderCmdlet { get { return default(string); } set { } }
    public System.Management.Automation.PSTypeName[] Type { get { return default(System.Management.Automation.PSTypeName[]); } }
     
  }
  public sealed class PagingParameters {
    internal PagingParameters() { }
    [System.Management.Automation.ParameterAttribute]
    public ulong First { get { return default(ulong); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter IncludeTotalCount { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.SwitchParameter); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    [System.Management.Automation.ParameterAttribute]
    public ulong Skip { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(ulong); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
     
    public System.Management.Automation.PSObject NewTotalCount(ulong totalCount, double accuracy) { return default(System.Management.Automation.PSObject); }
  }
  [System.AttributeUsageAttribute((AttributeTargets)384, AllowMultiple=true)]
  public sealed class ParameterAttribute : System.Management.Automation.Internal.ParsingBaseAttribute {
    public const string AllParameterSets = "__AllParameterSets";
     
    public ParameterAttribute() { }
     
    public string HelpMessage { get { return default(string); } set { } }
    public string HelpMessageBaseName { get { return default(string); } set { } }
    public string HelpMessageResourceId { get { return default(string); } set { } }
    public bool Mandatory { get { return default(bool); } set { } }
    public string ParameterSetName { get { return default(string); } set { } }
    public int Position { get { return default(int); } set { } }
    public bool ValueFromPipeline { get { return default(bool); } set { } }
    public bool ValueFromPipelineByPropertyName { get { return default(bool); } set { } }
    public bool ValueFromRemainingArguments { get { return default(bool); } set { } }
     
  }
  public class ParameterBindingException : System.Management.Automation.RuntimeException {
    public ParameterBindingException() { }
    public ParameterBindingException(string message) { }
    public ParameterBindingException(string message, System.Exception innerException) { }
     
    public System.Management.Automation.InvocationInfo CommandInvocation { get { return default(System.Management.Automation.InvocationInfo); } }
    public string ErrorId { get { return default(string); } }
    public long Line { get { return default(long); } }
    public override string Message { get { return default(string); } }
    public long Offset { get { return default(long); } }
    public string ParameterName { get { return default(string); } }
    public System.Type ParameterType { get { return default(System.Type); } }
    public System.Type TypeSpecified { get { return default(System.Type); } }
  }
  public sealed class ParameterMetadata {
    public ParameterMetadata(System.Management.Automation.ParameterMetadata other) { }
    public ParameterMetadata(string name) { }
    public ParameterMetadata(string name, System.Type parameterType) { }
     
    public System.Collections.ObjectModel.Collection<string> Aliases { get { return default(System.Collections.ObjectModel.Collection<string>); } }
    public System.Collections.ObjectModel.Collection<System.Attribute> Attributes { get { return default(System.Collections.ObjectModel.Collection<System.Attribute>); } }
    public bool IsDynamic { get { return default(bool); } set { } }
    public string Name { get { return default(string); } set { } }
    public System.Collections.Generic.Dictionary<string, System.Management.Automation.ParameterSetMetadata> ParameterSets { get { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.ParameterSetMetadata>); } }
    public System.Type ParameterType { get { return default(System.Type); } set { } }
    public bool SwitchParameter { get { return default(bool); } }
     
    public static System.Collections.Generic.Dictionary<string, System.Management.Automation.ParameterMetadata> GetParameterMetadata(System.Type type) { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.ParameterMetadata>); }
  }
  public sealed class ParameterSetMetadata {
    internal ParameterSetMetadata() { }
    public string HelpMessage { get { return default(string); } set { } }
    public string HelpMessageBaseName { get { return default(string); } set { } }
    public string HelpMessageResourceId { get { return default(string); } set { } }
    public bool IsMandatory { get { return default(bool); } set { } }
    public int Position { get { return default(int); } set { } }
    public bool ValueFromPipeline { get { return default(bool); } set { } }
    public bool ValueFromPipelineByPropertyName { get { return default(bool); } set { } }
    public bool ValueFromRemainingArguments { get { return default(bool); } set { } }
     
  }
  public class ParentContainsErrorRecordException : System.Exception
    {
    public ParentContainsErrorRecordException() { }
    public ParentContainsErrorRecordException(System.Exception wrapperException) { }
    public ParentContainsErrorRecordException(string message) { }
    public ParentContainsErrorRecordException(string message, System.Exception innerException) { }
     
    public override string Message { get { return default(string); } }
  }
  public class ParseException : System.Management.Automation.RuntimeException {
    public ParseException() { }
    public ParseException(System.Management.Automation.Language.ParseError[] errors) { }
    public ParseException(string message) { }
    public ParseException(string message, System.Exception innerException) { }
     
    public System.Management.Automation.Language.ParseError[] Errors { get { return default(System.Management.Automation.Language.ParseError[]); } }
    public override string Message { get { return default(string); } }
  }
  public class ParsingMetadataException : System.Management.Automation.MetadataException {
    public ParsingMetadataException() { }
    public ParsingMetadataException(string message) { }
    public ParsingMetadataException(string message, System.Exception innerException) { }
  }
  public sealed class PathInfo {
    internal PathInfo() { }
    public System.Management.Automation.PSDriveInfo Drive { get { return default(System.Management.Automation.PSDriveInfo); } }
    public string Path { get { return default(string); } }
    public System.Management.Automation.ProviderInfo Provider { get { return default(System.Management.Automation.ProviderInfo); } }
    public string ProviderPath { get { return default(string); } }
     
    public override string ToString() { return default(string); }
  }
  public sealed class PathInfoStack : System.Collections.Generic.Stack<System.Management.Automation.PathInfo> {
    internal PathInfoStack() { }
    public string Name { get { return default(string); } }
     
  }
  public sealed class PathIntrinsics {
    internal PathIntrinsics() { }
    public System.Management.Automation.PathInfo CurrentFileSystemLocation { get { return default(System.Management.Automation.PathInfo); } }
    public System.Management.Automation.PathInfo CurrentLocation { get { return default(System.Management.Automation.PathInfo); } }
     
    public string Combine(string parent, string child) { return default(string); }
    public System.Management.Automation.PathInfo CurrentProviderLocation(string providerName) { return default(System.Management.Automation.PathInfo); }
    public System.Collections.ObjectModel.Collection<string> GetResolvedProviderPathFromProviderPath(string path, string providerId) { return default(System.Collections.ObjectModel.Collection<string>); }
    public System.Collections.ObjectModel.Collection<string> GetResolvedProviderPathFromPSPath(string path, out System.Management.Automation.ProviderInfo provider) { provider = default(System.Management.Automation.ProviderInfo); return default(System.Collections.ObjectModel.Collection<string>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PathInfo> GetResolvedPSPathFromPSPath(string path) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PathInfo>); }
    public string GetUnresolvedProviderPathFromPSPath(string path) { return default(string); }
    public string GetUnresolvedProviderPathFromPSPath(string path, out System.Management.Automation.ProviderInfo provider, out System.Management.Automation.PSDriveInfo drive) { provider = default(System.Management.Automation.ProviderInfo); drive = default(System.Management.Automation.PSDriveInfo); return default(string); }
    public bool IsProviderQualified(string path) { return default(bool); }
    public bool IsPSAbsolute(string path, out string driveName) { driveName = default(string); return default(bool); }
    public bool IsValid(string path) { return default(bool); }
    public System.Management.Automation.PathInfoStack LocationStack(string stackName) { return default(System.Management.Automation.PathInfoStack); }
    public string NormalizeRelativePath(string path, string basePath) { return default(string); }
    public string ParseChildName(string path) { return default(string); }
    public string ParseParent(string path, string root) { return default(string); }
    public System.Management.Automation.PathInfo PopLocation(string stackName) { return default(System.Management.Automation.PathInfo); }
    public void PushCurrentLocation(string stackName) { }
    public System.Management.Automation.PathInfoStack SetDefaultLocationStack(string stackName) { return default(System.Management.Automation.PathInfoStack); }
    public System.Management.Automation.PathInfo SetLocation(string path) { return default(System.Management.Automation.PathInfo); }
  }
  public class PipelineClosedException : System.Management.Automation.RuntimeException {
    public PipelineClosedException() { }
    public PipelineClosedException(string message) { }
    public PipelineClosedException(string message, System.Exception innerException) { }
  }
  public class PipelineDepthException : System.Exception, System.Management.Automation.IContainsErrorRecord
    {
    public PipelineDepthException() { }
    public PipelineDepthException(string message) { }
    public PipelineDepthException(string message, System.Exception innerException) { }
     
    public int CallDepth { get { return default(int); } }
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
  }
  public class PipelineStoppedException : System.Management.Automation.RuntimeException {
    public PipelineStoppedException() { }
    public PipelineStoppedException(string message) { }
    public PipelineStoppedException(string message, System.Exception innerException) { }
  }
  public sealed class PowerShell : System.IDisposable {
    internal PowerShell() { }
    public System.Management.Automation.PSCommand Commands { get { return default(System.Management.Automation.PSCommand); } set { } }
    public bool HadErrors { get { return default(bool); } }
    public string HistoryString { get { return default(string); } set { } }
    public System.Guid InstanceId { get { return default(System.Guid); } }
    public System.Management.Automation.PSInvocationStateInfo InvocationStateInfo { get { return default(System.Management.Automation.PSInvocationStateInfo); } }
    public bool IsNested { get { return default(bool); } }
    public bool IsRunspaceOwner { get { return default(bool); } }
    public System.Management.Automation.Runspaces.Runspace Runspace { get { return default(System.Management.Automation.Runspaces.Runspace); } set { } }
    public System.Management.Automation.Runspaces.RunspacePool RunspacePool { get { return default(System.Management.Automation.Runspaces.RunspacePool); } set { } }
    public System.Management.Automation.PSDataStreams Streams { get { return default(System.Management.Automation.PSDataStreams); } }
     
    // Events
    public event System.EventHandler<System.Management.Automation.PSInvocationStateChangedEventArgs> InvocationStateChanged { add { } remove { } }
     
    public System.Management.Automation.PowerShell AddArgument(object value) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell AddCommand(System.Management.Automation.CommandInfo commandInfo) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell AddCommand(string cmdlet) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell AddCommand(string cmdlet, bool useLocalScope) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell AddParameter(string parameterName) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell AddParameter(string parameterName, object value) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell AddParameters(System.Collections.IDictionary parameters) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell AddParameters(System.Collections.IList parameters) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell AddScript(string script) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell AddScript(string script, bool useLocalScope) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell AddStatement() { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PSJobProxy AsJobProxy() { return default(System.Management.Automation.PSJobProxy); }
    public System.IAsyncResult BeginInvoke() { return default(System.IAsyncResult); }
    public System.IAsyncResult BeginInvoke<T>(System.Management.Automation.PSDataCollection<T> input) { return default(System.IAsyncResult); }
    public System.IAsyncResult BeginInvoke<T>(System.Management.Automation.PSDataCollection<T> input, System.Management.Automation.PSInvocationSettings settings, System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
    public System.IAsyncResult BeginInvoke<TInput, TOutput>(System.Management.Automation.PSDataCollection<TInput> input, System.Management.Automation.PSDataCollection<TOutput> output) { return default(System.IAsyncResult); }
    public System.IAsyncResult BeginInvoke<TInput, TOutput>(System.Management.Automation.PSDataCollection<TInput> input, System.Management.Automation.PSDataCollection<TOutput> output, System.Management.Automation.PSInvocationSettings settings, System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
    public System.IAsyncResult BeginStop(System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Connect() { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.IAsyncResult ConnectAsync() { return default(System.IAsyncResult); }
    public static System.Management.Automation.PowerShell Create() { return default(System.Management.Automation.PowerShell); }
    public static System.Management.Automation.PowerShell Create(System.Management.Automation.RunspaceMode runspace) { return default(System.Management.Automation.PowerShell); }
    public static System.Management.Automation.PowerShell Create(System.Management.Automation.Runspaces.InitialSessionState initialSessionState) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell CreateNestedPowerShell() { return default(System.Management.Automation.PowerShell); }
    public void Dispose() { }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.PSObject> EndInvoke(System.IAsyncResult asyncResult) { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.PSObject>); }
    public void EndStop(System.IAsyncResult asyncResult) { }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Invoke() { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Invoke(System.Collections.IEnumerable input) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Invoke(System.Collections.IEnumerable input, System.Management.Automation.PSInvocationSettings settings) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<T> Invoke<T>() { return default(System.Collections.ObjectModel.Collection<T>); }
    public System.Collections.ObjectModel.Collection<T> Invoke<T>(System.Collections.IEnumerable input) { return default(System.Collections.ObjectModel.Collection<T>); }
    public void Invoke<T>(System.Collections.IEnumerable input, System.Collections.Generic.IList<T> output) { }
    public void Invoke<T>(System.Collections.IEnumerable input, System.Collections.Generic.IList<T> output, System.Management.Automation.PSInvocationSettings settings) { }
    public System.Collections.ObjectModel.Collection<T> Invoke<T>(System.Collections.IEnumerable input, System.Management.Automation.PSInvocationSettings settings) { return default(System.Collections.ObjectModel.Collection<T>); }
    public void Invoke<TInput, TOutput>(System.Management.Automation.PSDataCollection<TInput> input, System.Management.Automation.PSDataCollection<TOutput> output, System.Management.Automation.PSInvocationSettings settings) { }
    public void Stop() { }
  }
  public sealed class PowerShellStreams<TInput, TOutput> : System.IDisposable {
    public PowerShellStreams() { }
    public PowerShellStreams(System.Management.Automation.PSDataCollection<TInput> pipelineInput) { }
     
    public System.Management.Automation.PSDataCollection<System.Management.Automation.DebugRecord> DebugStream { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.DebugRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.ErrorRecord> ErrorStream { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.ErrorRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<TInput> InputStream { get { return default(System.Management.Automation.PSDataCollection<TInput>); } set { } }
    public System.Management.Automation.PSDataCollection<TOutput> OutputStream { get { return default(System.Management.Automation.PSDataCollection<TOutput>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.ProgressRecord> ProgressStream { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.ProgressRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.VerboseRecord> VerboseStream { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.VerboseRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.WarningRecord> WarningStream { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.WarningRecord>); } set { } }
     
    public void CloseAll() { }
    public void Dispose() { }
  }
  public enum PowerShellStreamType {
    Debug = 5,
    Error = 2,
    Input = 0,
    Output = 1,
    Progress = 6,
    Verbose = 4,
    Warning = 3,
  }
  [System.Runtime.Serialization.DataContractAttribute]
  public class ProgressRecord {
    public ProgressRecord(int activityId, string activity, string statusDescription) { }
     
    public string Activity { get { return default(string); } set { } }
    public int ActivityId { get { return default(int); } }
    public string CurrentOperation { get { return default(string); } set { } }
    public int ParentActivityId { get { return default(int); } set { } }
    public int PercentComplete { get { return default(int); } set { } }
    public System.Management.Automation.ProgressRecordType RecordType { get { return default(System.Management.Automation.ProgressRecordType); } set { } }
    public int SecondsRemaining { get { return default(int); } set { } }
    public string StatusDescription { get { return default(string); } set { } }
     
    public override string ToString() { return default(string); }
  }
  public enum ProgressRecordType {
    Completed = 1,
    Processing = 0,
  }
  public sealed class PropertyCmdletProviderIntrinsics {
    internal PropertyCmdletProviderIntrinsics() { }
    public void Clear(string path, System.Collections.ObjectModel.Collection<string> propertyToClear) { }
    public void Clear(string[] path, System.Collections.ObjectModel.Collection<string> propertyToClear, bool force, bool literalPath) { }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Copy(string sourcePath, string sourceProperty, string destinationPath, string destinationProperty) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Copy(string[] sourcePath, string sourceProperty, string destinationPath, string destinationProperty, bool force, bool literalPath) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Get(string path, System.Collections.ObjectModel.Collection<string> providerSpecificPickList) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Get(string[] path, System.Collections.ObjectModel.Collection<string> providerSpecificPickList, bool literalPath) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Move(string sourcePath, string sourceProperty, string destinationPath, string destinationProperty) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Move(string[] sourcePath, string sourceProperty, string destinationPath, string destinationProperty, bool force, bool literalPath) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> New(string path, string propertyName, string propertyTypeName, object value) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> New(string[] path, string propertyName, string propertyTypeName, object value, bool force, bool literalPath) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public void Remove(string path, string propertyName) { }
    public void Remove(string[] path, string propertyName, bool force, bool literalPath) { }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Rename(string path, string sourceProperty, string destinationProperty) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Rename(string[] path, string sourceProperty, string destinationProperty, bool force, bool literalPath) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Set(string path, System.Management.Automation.PSObject propertyValue) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Set(string[] path, System.Management.Automation.PSObject propertyValue, bool force, bool literalPath) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
  }
  public class PropertyNotFoundException : System.Management.Automation.ExtendedTypeSystemException {
    public PropertyNotFoundException() { }
    public PropertyNotFoundException(string message) { }
    public PropertyNotFoundException(string message, System.Exception innerException) { }
  }
  public static class ProviderCmdlet {
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
  public class ProviderInfo {
    protected ProviderInfo(System.Management.Automation.ProviderInfo providerInfo) { }
     
    public System.Management.Automation.Provider.ProviderCapabilities Capabilities { get { return default(System.Management.Automation.Provider.ProviderCapabilities); } }
    public string Description { get { return default(string); } set { } }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo> Drives { get { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo>); } }
    public string HelpFile { get { return default(string); } }
    public string Home { get { return default(string); } set { } }
    public System.Type ImplementingType { get { return default(System.Type); } }
    public System.Management.Automation.PSModuleInfo Module { get { return default(System.Management.Automation.PSModuleInfo); } }
    public string ModuleName { get { return default(string); } }
    public string Name { get { return default(string); } }
     
    public override string ToString() { return default(string); }
  }
  public sealed class ProviderIntrinsics {
    internal ProviderIntrinsics() { }
    public System.Management.Automation.ChildItemCmdletProviderIntrinsics ChildItem { get { return default(System.Management.Automation.ChildItemCmdletProviderIntrinsics); } }
    public System.Management.Automation.ContentCmdletProviderIntrinsics Content { get { return default(System.Management.Automation.ContentCmdletProviderIntrinsics); } }
    public System.Management.Automation.ItemCmdletProviderIntrinsics Item { get { return default(System.Management.Automation.ItemCmdletProviderIntrinsics); } }
    public System.Management.Automation.PropertyCmdletProviderIntrinsics Property { get { return default(System.Management.Automation.PropertyCmdletProviderIntrinsics); } }
    public System.Management.Automation.SecurityDescriptorCmdletProviderIntrinsics SecurityDescriptor { get { return default(System.Management.Automation.SecurityDescriptorCmdletProviderIntrinsics); } }
     
  }
  public class ProviderInvocationException : System.Management.Automation.RuntimeException {
    public ProviderInvocationException() { }
    public ProviderInvocationException(string message) { }
    public ProviderInvocationException(string message, System.Exception innerException) { }
     
    public override System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override string Message { get { return default(string); } }
    public System.Management.Automation.ProviderInfo ProviderInfo { get { return default(System.Management.Automation.ProviderInfo); } }
     
  }
  public class ProviderNameAmbiguousException : System.Management.Automation.ProviderNotFoundException {
    public ProviderNameAmbiguousException() { }
    public ProviderNameAmbiguousException(string message) { }
    public ProviderNameAmbiguousException(string message, System.Exception innerException) { }
     
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.ProviderInfo> PossibleMatches { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.ProviderInfo>); } }
     
  }
  public class ProviderNotFoundException : System.Management.Automation.SessionStateException {
    public ProviderNotFoundException() { }
    public ProviderNotFoundException(string message) { }
    public ProviderNotFoundException(string message, System.Exception innerException) { }
  }
  public sealed class ProxyCommand {
    internal ProxyCommand() { }
    public static string Create(System.Management.Automation.CommandMetadata commandMetadata) { return default(string); }
    public static string Create(System.Management.Automation.CommandMetadata commandMetadata, string helpComment) { return default(string); }
    public static string GetBegin(System.Management.Automation.CommandMetadata commandMetadata) { return default(string); }
    public static string GetCmdletBindingAttribute(System.Management.Automation.CommandMetadata commandMetadata) { return default(string); }
    public static string GetEnd(System.Management.Automation.CommandMetadata commandMetadata) { return default(string); }
    public static string GetHelpComments(System.Management.Automation.PSObject help) { return default(string); }
    public static string GetParamBlock(System.Management.Automation.CommandMetadata commandMetadata) { return default(string); }
    public static string GetProcess(System.Management.Automation.CommandMetadata commandMetadata) { return default(string); }
  }
  public class PSAdaptedProperty : System.Management.Automation.PSProperty {
    public PSAdaptedProperty(string name, object tag) { }
     
    public object BaseObject { get { return default(object); } }
    public object Tag { get { return default(object); } }
     
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
  }
  public class PSAliasProperty : System.Management.Automation.PSPropertyInfo {
    public PSAliasProperty(string name, string referencedMemberName) { }
    public PSAliasProperty(string name, string referencedMemberName, System.Type conversionType) { }
     
    public System.Type ConversionType { get { return default(System.Type); } }
    public override bool IsGettable { get { return default(bool); } }
    public override bool IsSettable { get { return default(bool); } }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public string ReferencedMemberName { get { return default(string); } }
    public override string TypeNameOfValue { get { return default(string); } }
    public override object Value { get { return default(object); } set { } }
     
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString() { return default(string); }
  }
  public class PSArgumentException : System.ArgumentException, System.Management.Automation.IContainsErrorRecord {
    public PSArgumentException() { }
    public PSArgumentException(string message) { }
    public PSArgumentException(string message, System.Exception innerException) { }
    public PSArgumentException(string message, string paramName) { }
     
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override string Message { get { return default(string); } }
  }
  public class PSArgumentNullException : System.ArgumentNullException, System.Management.Automation.IContainsErrorRecord {
    public PSArgumentNullException() { }
    public PSArgumentNullException(string paramName) { }
    public PSArgumentNullException(string message, System.Exception innerException) { }
    public PSArgumentNullException(string paramName, string message) { }
     
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override string Message { get { return default(string); } }
  }
  public class PSArgumentOutOfRangeException : System.ArgumentOutOfRangeException, System.Management.Automation.IContainsErrorRecord {
    public PSArgumentOutOfRangeException() { }
    public PSArgumentOutOfRangeException(string paramName) { }
    public PSArgumentOutOfRangeException(string message, System.Exception innerException) { }
    public PSArgumentOutOfRangeException(string paramName, object actualValue, string message) { }
     
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
  }
  public sealed class PSChildJobProxy : System.Management.Automation.Job2 {
    internal PSChildJobProxy() { }
    public override bool HasMoreData { get { return default(bool); } }
    public override string Location { get { return default(string); } }
    public override string StatusMessage { get { return default(string); } }
     
    // Events
    public event System.EventHandler<System.Management.Automation.JobDataAddedEventArgs> JobDataAdded { add { } remove { } }
     
    protected override void Dispose(bool disposing) { }
    public override void ResumeJob() { }
    public override void ResumeJobAsync() { }
    public override void StartJob() { }
    public override void StartJobAsync() { }
    public override void StopJob() { }
    public override void StopJob(bool force, string reason) { }
    public override void StopJobAsync() { }
    public override void StopJobAsync(bool force, string reason) { }
    public override void SuspendJob() { }
    public override void SuspendJob(bool force, string reason) { }
    public override void SuspendJobAsync() { }
    public override void SuspendJobAsync(bool force, string reason) { }
    public override void UnblockJob() { }
    public override void UnblockJobAsync() { }
  }
  public abstract class PSCmdlet : System.Management.Automation.Cmdlet {
    protected PSCmdlet() { }
     
    public System.Management.Automation.PSEventManager Events { get { return default(System.Management.Automation.PSEventManager); } }
    public System.Management.Automation.Host.PSHost Host { get { return default(System.Management.Automation.Host.PSHost); } }
    public System.Management.Automation.CommandInvocationIntrinsics InvokeCommand { get { return default(System.Management.Automation.CommandInvocationIntrinsics); } }
    public System.Management.Automation.ProviderIntrinsics InvokeProvider { get { return default(System.Management.Automation.ProviderIntrinsics); } }
    public System.Management.Automation.JobManager JobManager { get { return default(System.Management.Automation.JobManager); } }
    public System.Management.Automation.JobRepository JobRepository { get { return default(System.Management.Automation.JobRepository); } }
    public System.Management.Automation.InvocationInfo MyInvocation { get { return default(System.Management.Automation.InvocationInfo); } }
    public System.Management.Automation.PagingParameters PagingParameters { get { return default(System.Management.Automation.PagingParameters); } }
    public string ParameterSetName { get { return default(string); } }
    public System.Management.Automation.SessionState SessionState { get { return default(System.Management.Automation.SessionState); } }
     
    public System.Management.Automation.PathInfo CurrentProviderLocation(string providerId) { return default(System.Management.Automation.PathInfo); }
    public System.Collections.ObjectModel.Collection<string> GetResolvedProviderPathFromPSPath(string path, out System.Management.Automation.ProviderInfo provider) { provider = default(System.Management.Automation.ProviderInfo); return default(System.Collections.ObjectModel.Collection<string>); }
    public string GetUnresolvedProviderPathFromPSPath(string path) { return default(string); }
    public object GetVariableValue(string name) { return default(object); }
    public object GetVariableValue(string name, object defaultValue) { return default(object); }
  }
  public class PSCodeMethod : System.Management.Automation.PSMethodInfo {
    public PSCodeMethod(string name, System.Reflection.MethodInfo codeReference) { }
     
    public System.Reflection.MethodInfo CodeReference { get { return default(System.Reflection.MethodInfo); } }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public override System.Collections.ObjectModel.Collection<string> OverloadDefinitions { get { return default(System.Collections.ObjectModel.Collection<string>); } }
    public override string TypeNameOfValue { get { return default(string); } }
     
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
    public override object Invoke(params object[] arguments) { return default(object); }
    public override string ToString() { return default(string); }
  }
  public class PSCodeProperty : System.Management.Automation.PSPropertyInfo {
    public PSCodeProperty(string name, System.Reflection.MethodInfo getterCodeReference) { }
    public PSCodeProperty(string name, System.Reflection.MethodInfo getterCodeReference, System.Reflection.MethodInfo setterCodeReference) { }
     
    public System.Reflection.MethodInfo GetterCodeReference { get { return default(System.Reflection.MethodInfo); } }
    public override bool IsGettable { get { return default(bool); } }
    public override bool IsSettable { get { return default(bool); } }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public System.Reflection.MethodInfo SetterCodeReference { get { return default(System.Reflection.MethodInfo); } }
    public override string TypeNameOfValue { get { return default(string); } }
    public override object Value { get { return default(object); } set { } }
     
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString() { return default(string); }
  }
  public sealed class PSCommand {
    public PSCommand() { }
     
    public System.Management.Automation.Runspaces.CommandCollection Commands { get { return default(System.Management.Automation.Runspaces.CommandCollection); } }
     
    public System.Management.Automation.PSCommand AddArgument(object value) { return default(System.Management.Automation.PSCommand); }
    public System.Management.Automation.PSCommand AddCommand(System.Management.Automation.Runspaces.Command command) { return default(System.Management.Automation.PSCommand); }
    public System.Management.Automation.PSCommand AddCommand(string command) { return default(System.Management.Automation.PSCommand); }
    public System.Management.Automation.PSCommand AddCommand(string cmdlet, bool useLocalScope) { return default(System.Management.Automation.PSCommand); }
    public System.Management.Automation.PSCommand AddParameter(string parameterName) { return default(System.Management.Automation.PSCommand); }
    public System.Management.Automation.PSCommand AddParameter(string parameterName, object value) { return default(System.Management.Automation.PSCommand); }
    public System.Management.Automation.PSCommand AddScript(string script) { return default(System.Management.Automation.PSCommand); }
    public System.Management.Automation.PSCommand AddScript(string script, bool useLocalScope) { return default(System.Management.Automation.PSCommand); }
    public System.Management.Automation.PSCommand AddStatement() { return default(System.Management.Automation.PSCommand); }
    public void Clear() { }
    public System.Management.Automation.PSCommand Clone() { return default(System.Management.Automation.PSCommand); }
  }
  public abstract class PSControl {
    protected PSControl() { }
  }
  public sealed class PSCredential
  {
    public PSCredential(string userName, System.Security.SecureString password) { }
     
    public static System.Management.Automation.PSCredential Empty { get { return default(System.Management.Automation.PSCredential); } }
    public System.Security.SecureString Password { get { return default(System.Security.SecureString); } }
    public string UserName { get { return default(string); } }
     
    public System.Net.NetworkCredential GetNetworkCredential() { return default(System.Net.NetworkCredential); }
    public static explicit operator System.Net.NetworkCredential (System.Management.Automation.PSCredential credential) { return default(System.Net.NetworkCredential); }
  }
  [System.FlagsAttribute]
  public enum PSCredentialTypes {
    Default = 3,
    Domain = 2,
    Generic = 1,
  }
  [System.FlagsAttribute]
  public enum PSCredentialUIOptions {
    AlwaysPrompt = 2,
    Default = 1,
    None = 0,
    ReadOnlyUserName = 3,
    ValidateUserNameSyntax = 1,
  }
  public class PSCustomObject {
    internal PSCustomObject() { }
    public override string ToString() { return default(string); }
  }
  public class PSDataCollection<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IList<T>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList, System.IDisposable {
    public PSDataCollection() { }
    public PSDataCollection(System.Collections.Generic.IEnumerable<T> items) { }
    public PSDataCollection(int capacity) { }
     
    public bool BlockingEnumerator { get { return default(bool); } set { } }
    public int Count { get { return default(int); } }
    public int DataAddedCount { get { return default(int); } set { } }
    public bool EnumeratorNeverBlocks { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public bool IsAutoGenerated { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public bool IsOpen { get { return default(bool); } }
    public bool IsReadOnly { get { return default(bool); } }
    public T this[int index] { get { return default(T); } set { } }
    public bool SerializeInput { get { return default(bool); } set { } }
    bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
    object System.Collections.ICollection.SyncRoot { get { return default(object); } }
    bool System.Collections.IList.IsFixedSize { get { return default(bool); } }
    bool System.Collections.IList.IsReadOnly { get { return default(bool); } }
    object System.Collections.IList.this[int index] { get { return default(object); } set { } }
     
    // Events
    public event System.EventHandler Completed { add { } remove { } }
    public event System.EventHandler<System.Management.Automation.DataAddedEventArgs> DataAdded { add { } remove { } }
    public event System.EventHandler<System.Management.Automation.DataAddingEventArgs> DataAdding { add { } remove { } }
     
    public void Add(T item) { }
    public void Clear() { }
    public void Complete() { }
    public bool Contains(T item) { return default(bool); }
    public void CopyTo(T[] array, int arrayIndex) { }
    public void Dispose() { }
    protected void Dispose(bool disposing) { }
    public System.Collections.Generic.IEnumerator<T> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<T>); }
    public int IndexOf(T item) { return default(int); }
    public void Insert(int index, T item) { }
    protected virtual void InsertItem(System.Guid psInstanceId, int index, T item) { }
    public static implicit operator System.Management.Automation.PSDataCollection<T> (T valueToConvert) { return default(System.Management.Automation.PSDataCollection<T>); }
    public static implicit operator System.Management.Automation.PSDataCollection<T> (bool valueToConvert) { return default(System.Management.Automation.PSDataCollection<T>); }
    public static implicit operator System.Management.Automation.PSDataCollection<T> (byte valueToConvert) { return default(System.Management.Automation.PSDataCollection<T>); }
    public static implicit operator System.Management.Automation.PSDataCollection<T> (System.Collections.Hashtable valueToConvert) { return default(System.Management.Automation.PSDataCollection<T>); }
    public static implicit operator System.Management.Automation.PSDataCollection<T> (int valueToConvert) { return default(System.Management.Automation.PSDataCollection<T>); }
    public static implicit operator System.Management.Automation.PSDataCollection<T> (object[] arrayToConvert) { return default(System.Management.Automation.PSDataCollection<T>); }
    public static implicit operator System.Management.Automation.PSDataCollection<T> (string valueToConvert) { return default(System.Management.Automation.PSDataCollection<T>); }
    public System.Collections.ObjectModel.Collection<T> ReadAll() { return default(System.Collections.ObjectModel.Collection<T>); }
    public bool Remove(T item) { return default(bool); }
    public void RemoveAt(int index) { }
    protected virtual void RemoveItem(int index) { }
    void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
    int System.Collections.IList.Add(object value) { return default(int); }
    bool System.Collections.IList.Contains(object value) { return default(bool); }
    int System.Collections.IList.IndexOf(object value) { return default(int); }
    void System.Collections.IList.Insert(int index, object value) { }
    void System.Collections.IList.Remove(object value) { }
  }
  public sealed class PSDataStreams {
    internal PSDataStreams() { }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.DebugRecord> Debug { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.DebugRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.ErrorRecord> Error { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.ErrorRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.ProgressRecord> Progress { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.ProgressRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.VerboseRecord> Verbose { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.VerboseRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.WarningRecord> Warning { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.WarningRecord>); } set { } }
     
    public void ClearStreams() { }
  }
  public class PSDebugContext {
    internal PSDebugContext() { }
    public System.Management.Automation.Breakpoint[] Breakpoints { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Breakpoint[]); } }
    public System.Management.Automation.InvocationInfo InvocationInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.InvocationInfo); } }
     
  }
  [System.AttributeUsageAttribute((AttributeTargets)384)]
  public sealed class PSDefaultValueAttribute : System.Management.Automation.Internal.ParsingBaseAttribute {
    public PSDefaultValueAttribute() { }
     
    public string Help { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public object Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(object); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
     
  }
  public class PSDriveInfo : System.IComparable {
    protected PSDriveInfo(System.Management.Automation.PSDriveInfo driveInfo) { }
    public PSDriveInfo(string name, System.Management.Automation.ProviderInfo provider, string root, string description, System.Management.Automation.PSCredential credential) { }
    public PSDriveInfo(string name, System.Management.Automation.ProviderInfo provider, string root, string description, System.Management.Automation.PSCredential credential, bool persist) { }
    public PSDriveInfo(string name, System.Management.Automation.ProviderInfo provider, string root, string description, System.Management.Automation.PSCredential credential, string displayRoot) { }
     
    public System.Management.Automation.PSCredential Credential { get { return default(System.Management.Automation.PSCredential); } }
    public string CurrentLocation { get { return default(string); } set { } }
    public string Description { get { return default(string); } set { } }
    public string DisplayRoot { get { return default(string); } }
    public string Name { get { return default(string); } }
    public System.Management.Automation.ProviderInfo Provider { get { return default(System.Management.Automation.ProviderInfo); } }
    public string Root { get { return default(string); } }
     
    public int CompareTo(System.Management.Automation.PSDriveInfo drive) { return default(int); }
    public int CompareTo(object obj) { return default(int); }
    public bool Equals(System.Management.Automation.PSDriveInfo drive) { return default(bool); }
    public override bool Equals(object obj) { return default(bool); }
    public override int GetHashCode() { return default(int); }
    public static bool operator ==(System.Management.Automation.PSDriveInfo drive1, System.Management.Automation.PSDriveInfo drive2) { return default(bool); }
    public static bool operator >(System.Management.Automation.PSDriveInfo drive1, System.Management.Automation.PSDriveInfo drive2) { return default(bool); }
    public static bool operator !=(System.Management.Automation.PSDriveInfo drive1, System.Management.Automation.PSDriveInfo drive2) { return default(bool); }
    public static bool operator <(System.Management.Automation.PSDriveInfo drive1, System.Management.Automation.PSDriveInfo drive2) { return default(bool); }
    public override string ToString() { return default(string); }
  }
  public class PSDynamicMember : System.Management.Automation.PSMemberInfo {
    internal PSDynamicMember() { }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public override string TypeNameOfValue { get { return default(string); } }
    public override object Value { get { return default(object); } set { } }
     
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString() { return default(string); }
  }
  public sealed class PSEngineEvent {
    internal PSEngineEvent() { }
    public const string Exiting = "PowerShell.Exiting";
    public const string OnIdle = "PowerShell.OnIdle";
  }
  public class PSEvent : System.Management.Automation.PSMemberInfo {
    internal PSEvent() { }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public override string TypeNameOfValue { get { return default(string); } }
    public sealed override object Value { get { return default(object); } set { } }
     
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString() { return default(string); }
  }
  public class PSEventArgs : System.EventArgs {
    internal PSEventArgs() { }
    public string ComputerName { get { return default(string); } }
    public int EventIdentifier { get { return default(int); } }
    public System.Management.Automation.PSObject MessageData { get { return default(System.Management.Automation.PSObject); } }
    public System.Guid RunspaceId { get { return default(System.Guid); } }
    public object Sender { get { return default(object); } }
    public object[] SourceArgs { get { return default(object[]); } }
    public System.EventArgs SourceEventArgs { get { return default(System.EventArgs); } }
    public string SourceIdentifier { get { return default(string); } }
    public System.DateTime TimeGenerated { get { return default(System.DateTime); } }
     
  }
  public class PSEventArgsCollection : System.Collections.Generic.IEnumerable<System.Management.Automation.PSEventArgs>, System.Collections.IEnumerable {
    public PSEventArgsCollection() { }
     
    public int Count { get { return default(int); } }
    public System.Management.Automation.PSEventArgs this[int index] { get { return default(System.Management.Automation.PSEventArgs); } }
    public object SyncRoot { get { return default(object); } }
     
    // Events
    public event System.Management.Automation.PSEventReceivedEventHandler PSEventReceived { add { } remove { } }
     
    public System.Collections.Generic.IEnumerator<System.Management.Automation.PSEventArgs> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<System.Management.Automation.PSEventArgs>); }
    public void RemoveAt(int index) { }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
  }
  public class PSEventHandler {
    protected System.Management.Automation.PSEventManager eventManager;
    protected System.Management.Automation.PSObject extraData;
    protected object sender;
    protected string sourceIdentifier;
     
    public PSEventHandler() { }
    public PSEventHandler(System.Management.Automation.PSEventManager eventManager, object sender, string sourceIdentifier, System.Management.Automation.PSObject extraData) { }
  }
  public class PSEventJob : System.Management.Automation.Job {
    public PSEventJob(System.Management.Automation.PSEventManager eventManager, System.Management.Automation.PSEventSubscriber subscriber, System.Management.Automation.ScriptBlock action, string name) { }
     
    public override bool HasMoreData { get { return default(bool); } }
    public override string Location { get { return default(string); } }
    public System.Management.Automation.PSModuleInfo Module { get { return default(System.Management.Automation.PSModuleInfo); } }
    public override string StatusMessage { get { return default(string); } }
     
    public override void StopJob() { }
  }
  public abstract class PSEventManager {
    protected PSEventManager() { }
     
    public System.Management.Automation.PSEventArgsCollection ReceivedEvents { get { return default(System.Management.Automation.PSEventArgsCollection); } }
    public abstract System.Collections.Generic.List<System.Management.Automation.PSEventSubscriber> Subscribers { get; }
     
    protected abstract System.Management.Automation.PSEventArgs CreateEvent(string sourceIdentifier, object sender, object[] args, System.Management.Automation.PSObject extraData);
    public System.Management.Automation.PSEventArgs GenerateEvent(string sourceIdentifier, object sender, object[] args, System.Management.Automation.PSObject extraData) { return default(System.Management.Automation.PSEventArgs); }
    public abstract System.Collections.Generic.IEnumerable<System.Management.Automation.PSEventSubscriber> GetEventSubscribers(string sourceIdentifier);
    protected int GetNextEventId() { return default(int); }
    protected abstract void ProcessNewEvent(System.Management.Automation.PSEventArgs newEvent, bool processInCurrentThread);
    protected internal virtual void ProcessNewEvent(System.Management.Automation.PSEventArgs newEvent, bool processInCurrentThread, bool waitForCompletionWhenInCurrentThread) { }
    public abstract System.Management.Automation.PSEventSubscriber SubscribeEvent(object source, string eventName, string sourceIdentifier, System.Management.Automation.PSObject data, System.Management.Automation.PSEventReceivedEventHandler handlerDelegate, bool supportEvent, bool forwardEvent);
    public abstract System.Management.Automation.PSEventSubscriber SubscribeEvent(object source, string eventName, string sourceIdentifier, System.Management.Automation.PSObject data, System.Management.Automation.PSEventReceivedEventHandler handlerDelegate, bool supportEvent, bool forwardEvent, int maxTriggerCount);
    public abstract System.Management.Automation.PSEventSubscriber SubscribeEvent(object source, string eventName, string sourceIdentifier, System.Management.Automation.PSObject data, System.Management.Automation.ScriptBlock action, bool supportEvent, bool forwardEvent);
    public abstract System.Management.Automation.PSEventSubscriber SubscribeEvent(object source, string eventName, string sourceIdentifier, System.Management.Automation.PSObject data, System.Management.Automation.ScriptBlock action, bool supportEvent, bool forwardEvent, int maxTriggerCount);
    public abstract void UnsubscribeEvent(System.Management.Automation.PSEventSubscriber subscriber);
  }
  public delegate void PSEventReceivedEventHandler(object sender, System.Management.Automation.PSEventArgs e);
  public class PSEventSubscriber : System.IEquatable<System.Management.Automation.PSEventSubscriber> {
    internal PSEventSubscriber() { }
    public System.Management.Automation.PSEventJob Action { get { return default(System.Management.Automation.PSEventJob); } }
    public string EventName { get { return default(string); } }
    public bool ForwardEvent { get { return default(bool); } }
    public System.Management.Automation.PSEventReceivedEventHandler HandlerDelegate { get { return default(System.Management.Automation.PSEventReceivedEventHandler); } }
    public string SourceIdentifier { get { return default(string); } }
    public object SourceObject { get { return default(object); } }
    public int SubscriptionId { get { return default(int); } set { } }
    public bool SupportEvent { get { return default(bool); } }
     
    // Events
    public event System.Management.Automation.PSEventUnsubscribedEventHandler Unsubscribed { add { } remove { } }
     
    public bool Equals(System.Management.Automation.PSEventSubscriber other) { return default(bool); }
    public override int GetHashCode() { return default(int); }
  }
  public class PSEventUnsubscribedEventArgs : System.EventArgs {
    internal PSEventUnsubscribedEventArgs() { }
    public System.Management.Automation.PSEventSubscriber EventSubscriber { get { return default(System.Management.Automation.PSEventSubscriber); } }
     
  }
  public delegate void PSEventUnsubscribedEventHandler(object sender, System.Management.Automation.PSEventUnsubscribedEventArgs e);
  public class PSInvalidCastException : System.InvalidCastException, System.Management.Automation.IContainsErrorRecord {
    public PSInvalidCastException() { }
    public PSInvalidCastException(string message) { }
    public PSInvalidCastException(string message, System.Exception innerException) { }
     
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
  }
  public class PSInvalidOperationException : System.InvalidOperationException, System.Management.Automation.IContainsErrorRecord {
    public PSInvalidOperationException() { }
    public PSInvalidOperationException(string message) { }
    public PSInvalidOperationException(string message, System.Exception innerException) { }
     
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
  }
  public sealed class PSInvocationSettings {
    public PSInvocationSettings() { }
     
    public bool AddToHistory { get { return default(bool); } set { } }
#if COM_APARTMENT_STATE
    public System.Threading.ApartmentState ApartmentState { get { return default(System.Threading.ApartmentState); } set { } }
#endif
    public System.Nullable<System.Management.Automation.ActionPreference> ErrorActionPreference { get { return default(System.Nullable<System.Management.Automation.ActionPreference>); } set { } }
    public bool FlowImpersonationPolicy { get { return default(bool); } set { } }
    public System.Management.Automation.Host.PSHost Host { get { return default(System.Management.Automation.Host.PSHost); } set { } }
    public System.Management.Automation.RemoteStreamOptions RemoteStreamOptions { get { return default(System.Management.Automation.RemoteStreamOptions); } set { } }
     
  }
  public enum PSInvocationState {
    Completed = 4,
    Disconnected = 6,
    Failed = 5,
    NotStarted = 0,
    Running = 1,
    Stopped = 3,
    Stopping = 2,
  }
  public sealed class PSInvocationStateChangedEventArgs : System.EventArgs {
    internal PSInvocationStateChangedEventArgs() { }
    public System.Management.Automation.PSInvocationStateInfo InvocationStateInfo { get { return default(System.Management.Automation.PSInvocationStateInfo); } }
     
  }
  public sealed class PSInvocationStateInfo {
    internal PSInvocationStateInfo() { }
    public System.Exception Reason { get { return default(System.Exception); } }
    public System.Management.Automation.PSInvocationState State { get { return default(System.Management.Automation.PSInvocationState); } }
     
  }
  public sealed class PSJobProxy : System.Management.Automation.Job2 {
    internal PSJobProxy() { }
    public override bool HasMoreData { get { return default(bool); } }
    public override string Location { get { return default(string); } }
    public System.Guid RemoteJobInstanceId { get { return default(System.Guid); } }
    public bool RemoveRemoteJobOnCompletion { get { return default(bool); } set { } }
    public System.Management.Automation.Runspaces.Runspace Runspace { get { return default(System.Management.Automation.Runspaces.Runspace); } set { } }
    public System.Management.Automation.Runspaces.RunspacePool RunspacePool { get { return default(System.Management.Automation.Runspaces.RunspacePool); } set { } }
    public override string StatusMessage { get { return default(string); } }
     
    // Events
    public event System.EventHandler<System.ComponentModel.AsyncCompletedEventArgs> RemoveJobCompleted { add { } remove { } }
     
    public static System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy> Create(System.Management.Automation.Runspaces.Runspace runspace) { return default(System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy>); }
    public static System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy> Create(System.Management.Automation.Runspaces.Runspace runspace, System.Collections.Hashtable filter) { return default(System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy>); }
    public static System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy> Create(System.Management.Automation.Runspaces.Runspace runspace, System.Collections.Hashtable filter, bool receiveImmediately) { return default(System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy>); }
    public static System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy> Create(System.Management.Automation.Runspaces.Runspace runspace, System.Collections.Hashtable filter, System.EventHandler<System.Management.Automation.JobDataAddedEventArgs> dataAdded, System.EventHandler<System.Management.Automation.JobStateEventArgs> stateChanged) { return default(System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy>); }
    public static System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy> Create(System.Management.Automation.Runspaces.RunspacePool runspacePool) { return default(System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy>); }
    public static System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy> Create(System.Management.Automation.Runspaces.RunspacePool runspacePool, System.Collections.Hashtable filter) { return default(System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy>); }
    public static System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy> Create(System.Management.Automation.Runspaces.RunspacePool runspacePool, System.Collections.Hashtable filter, bool receiveImmediately) { return default(System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy>); }
    public static System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy> Create(System.Management.Automation.Runspaces.RunspacePool runspacePool, System.Collections.Hashtable filter, System.EventHandler<System.Management.Automation.JobDataAddedEventArgs> dataAdded, System.EventHandler<System.Management.Automation.JobStateEventArgs> stateChanged) { return default(System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy>); }
    protected override void Dispose(bool disposing) { }
    public void ReceiveJob() { }
    public void ReceiveJob(System.EventHandler<System.Management.Automation.JobDataAddedEventArgs> dataAdded, System.EventHandler<System.Management.Automation.JobStateEventArgs> stateChanged) { }
    public void RemoveJob(bool removeRemoteJob) { }
    public void RemoveJob(bool removeRemoteJob, bool force) { }
    public void RemoveJobAsync(bool removeRemoteJob) { }
    public void RemoveJobAsync(bool removeRemoteJob, bool force) { }
    public override void ResumeJob() { }
    public override void ResumeJobAsync() { }
    public override void StartJob() { }
    public void StartJob(System.EventHandler<System.Management.Automation.JobDataAddedEventArgs> dataAdded, System.EventHandler<System.Management.Automation.JobStateEventArgs> stateChanged, System.Management.Automation.PSDataCollection<object> input) { }
    public void StartJob(System.Management.Automation.PSDataCollection<object> input) { }
    public override void StartJobAsync() { }
    public void StartJobAsync(System.EventHandler<System.Management.Automation.JobDataAddedEventArgs> dataAdded, System.EventHandler<System.Management.Automation.JobStateEventArgs> stateChanged, System.Management.Automation.PSDataCollection<object> input) { }
    public void StartJobAsync(System.Management.Automation.PSDataCollection<object> input) { }
    public override void StopJob() { }
    public override void StopJob(bool force, string reason) { }
    public override void StopJobAsync() { }
    public override void StopJobAsync(bool force, string reason) { }
    public override void SuspendJob() { }
    public override void SuspendJob(bool force, string reason) { }
    public override void SuspendJobAsync() { }
    public override void SuspendJobAsync(bool force, string reason) { }
    public override void UnblockJob() { }
    public override void UnblockJobAsync() { }
  }
  public enum PSLanguageMode {
    ConstrainedLanguage = 3,
    FullLanguage = 0,
    NoLanguage = 2,
    RestrictedLanguage = 1,
  }
  public class PSListModifier {
    public PSListModifier() { }
    public PSListModifier(System.Collections.Hashtable hash) { }
    public PSListModifier(System.Collections.ObjectModel.Collection<object> removeItems, System.Collections.ObjectModel.Collection<object> addItems) { }
    public PSListModifier(object replacementItems) { }
     
    public System.Collections.ObjectModel.Collection<object> Add { get { return default(System.Collections.ObjectModel.Collection<object>); } }
    public System.Collections.ObjectModel.Collection<object> Remove { get { return default(System.Collections.ObjectModel.Collection<object>); } }
    public System.Collections.ObjectModel.Collection<object> Replace { get { return default(System.Collections.ObjectModel.Collection<object>); } }
     
    public void ApplyTo(System.Collections.IList collectionToUpdate) { }
    public void ApplyTo(object collectionToUpdate) { }
  }
  public class PSListModifier<T> : System.Management.Automation.PSListModifier {
    public PSListModifier() { }
    public PSListModifier(System.Collections.Hashtable hash) { }
    public PSListModifier(System.Collections.ObjectModel.Collection<object> removeItems, System.Collections.ObjectModel.Collection<object> addItems) { }
    public PSListModifier(object replacementItems) { }
  }
  public abstract class PSMemberInfo {
    protected PSMemberInfo() { }
     
    public bool IsInstance { get { return default(bool); } }
    public abstract System.Management.Automation.PSMemberTypes MemberType { get; }
    public string Name { get { return default(string); } }
    public abstract string TypeNameOfValue { get; }
    public abstract object Value { get; set; }
     
    public abstract System.Management.Automation.PSMemberInfo Copy();
    protected void SetMemberName(string name) { }
  }
  public abstract class PSMemberInfoCollection<T> : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable where T : System.Management.Automation.PSMemberInfo {
    protected PSMemberInfoCollection() { }
     
    public abstract T this[string name] { get; }
     
    public abstract void Add(T member);
    public abstract void Add(T member, bool preValidated);
    public abstract System.Collections.Generic.IEnumerator<T> GetEnumerator();
    public abstract System.Management.Automation.ReadOnlyPSMemberInfoCollection<T> Match(string name);
    public abstract System.Management.Automation.ReadOnlyPSMemberInfoCollection<T> Match(string name, System.Management.Automation.PSMemberTypes memberTypes);
    public abstract void Remove(string name);
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
  }
  public class PSMemberSet : System.Management.Automation.PSMemberInfo {
    public PSMemberSet(string name) { }
    public PSMemberSet(string name, System.Collections.Generic.IEnumerable<System.Management.Automation.PSMemberInfo> members) { }
     
    public bool InheritMembers { get { return default(bool); } }
    public System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSMemberInfo> Members { get { return default(System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSMemberInfo>); } }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSMethodInfo> Methods { get { return default(System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSMethodInfo>); } }
    public System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSPropertyInfo> Properties { get { return default(System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSPropertyInfo>); } }
    public override string TypeNameOfValue { get { return default(string); } }
    public override object Value { get { return default(object); } set { } }
     
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString() { return default(string); }
  }
  //Internal: [System.ComponentModel.TypeConverterAttribute(typeof(System.Management.Automation.LanguagePrimitives.EnumMultipleTypeConverter))]
  [System.FlagsAttribute]
  public enum PSMemberTypes {
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
  //Internal: [System.ComponentModel.TypeConverterAttribute(typeof(System.Management.Automation.LanguagePrimitives.EnumMultipleTypeConverter))]
  [System.FlagsAttribute]
  public enum PSMemberViewTypes {
    Adapted = 2,
    All = 7,
    Base = 4,
    Extended = 1,
  }
  public class PSMethod : System.Management.Automation.PSMethodInfo {
    internal PSMethod() { }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public override System.Collections.ObjectModel.Collection<string> OverloadDefinitions { get { return default(System.Collections.ObjectModel.Collection<string>); } }
    public override string TypeNameOfValue { get { return default(string); } }
     
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
    public override object Invoke(params object[] arguments) { return default(object); }
    public override string ToString() { return default(string); }
  }
  public abstract class PSMethodInfo : System.Management.Automation.PSMemberInfo {
    protected PSMethodInfo() { }
     
    public abstract System.Collections.ObjectModel.Collection<string> OverloadDefinitions { get; }
    public sealed override object Value { get { return default(object); } set { } }
     
    public abstract object Invoke(params object[] arguments);
  }
  public enum PSModuleAutoLoadingPreference {
    All = 2,
    ModuleQualified = 1,
    None = 0,
  }
  public sealed class PSModuleInfo {
    public PSModuleInfo(bool linkToGlobal) { }
    public PSModuleInfo(System.Management.Automation.ScriptBlock scriptBlock) { }
     
    public System.Management.Automation.ModuleAccessMode AccessMode { get { return default(System.Management.Automation.ModuleAccessMode); } set { } }
    public string Author { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
    public System.Version ClrVersion { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Version); } }
    public string CompanyName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
    public string Copyright { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
    public string Definition { get { return default(string); } }
    public string Description { get { return default(string); } set { } }
    public System.Version DotNetFrameworkVersion { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Version); } }
    public System.Collections.Generic.Dictionary<string, System.Management.Automation.AliasInfo> ExportedAliases { get { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.AliasInfo>); } }
    public System.Collections.Generic.Dictionary<string, System.Management.Automation.CmdletInfo> ExportedCmdlets { get { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.CmdletInfo>); } }
    public System.Collections.Generic.Dictionary<string, System.Management.Automation.CommandInfo> ExportedCommands { get { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.CommandInfo>); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<string> ExportedFormatFiles { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<string>); } }
    public System.Collections.Generic.Dictionary<string, System.Management.Automation.FunctionInfo> ExportedFunctions { get { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.FunctionInfo>); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<string> ExportedTypeFiles { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<string>); } }
    public System.Collections.Generic.Dictionary<string, System.Management.Automation.PSVariable> ExportedVariables { get { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.PSVariable>); } }
    public System.Collections.Generic.Dictionary<string, System.Management.Automation.FunctionInfo> ExportedWorkflows { get { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.FunctionInfo>); } }
    public System.Collections.Generic.IEnumerable<string> FileList { get { return default(System.Collections.Generic.IEnumerable<string>); } }
    public System.Guid Guid { get { return default(System.Guid); } }
    public string HelpInfoUri { get { return default(string); } }
    public bool LogPipelineExecutionDetails { get { return default(bool); } set { } }
    public string ModuleBase { get { return default(string); } }
    public System.Collections.Generic.IEnumerable<object> ModuleList { get { return default(System.Collections.Generic.IEnumerable<object>); } }
    public System.Management.Automation.ModuleType ModuleType { get { return default(System.Management.Automation.ModuleType); } }
    public string Name { get { return default(string); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSModuleInfo> NestedModules { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSModuleInfo>); } }
    public System.Management.Automation.ScriptBlock OnRemove { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.ScriptBlock); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public string Path { get { return default(string); } }
    public string PowerShellHostName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
    public System.Version PowerShellHostVersion { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Version); } }
    public System.Version PowerShellVersion { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Version); } }
    public object PrivateData { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(object); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public System.Reflection.ProcessorArchitecture ProcessorArchitecture { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Reflection.ProcessorArchitecture); } }
    public System.Collections.Generic.IEnumerable<string> RequiredAssemblies { get { return default(System.Collections.Generic.IEnumerable<string>); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSModuleInfo> RequiredModules { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSModuleInfo>); } }
    public string RootModule { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
    public System.Collections.Generic.IEnumerable<string> Scripts { get { return default(System.Collections.Generic.IEnumerable<string>); } }
    public System.Management.Automation.SessionState SessionState { get { return default(System.Management.Automation.SessionState); } set { } }
    public static bool UseAppDomainLevelModuleCache { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public System.Version Version { get { return default(System.Version); } }
     
    public System.Management.Automation.PSObject AsCustomObject() { return default(System.Management.Automation.PSObject); }
    public static void ClearAppDomainLevelModulePathCache() { }
    public System.Management.Automation.PSModuleInfo Clone() { return default(System.Management.Automation.PSModuleInfo); }
    public object Invoke(System.Management.Automation.ScriptBlock sb, params object[] args) { return default(object); }
    public System.Management.Automation.ScriptBlock NewBoundScriptBlock(System.Management.Automation.ScriptBlock scriptBlockToBind) { return default(System.Management.Automation.ScriptBlock); }
    public override string ToString() { return default(string); }
  }
  public class PSNoteProperty : System.Management.Automation.PSPropertyInfo {
    public PSNoteProperty(string name, object value) { }
     
    public override bool IsGettable { get { return default(bool); } }
    public override bool IsSettable { get { return default(bool); } }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public override string TypeNameOfValue { get { return default(string); } }
    public override object Value { get { return default(object); } set { } }
     
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString() { return default(string); }
  }
  public class PSNotImplementedException : System.NotImplementedException, System.Management.Automation.IContainsErrorRecord {
    public PSNotImplementedException() { }
    public PSNotImplementedException(string message) { }
    public PSNotImplementedException(string message, System.Exception innerException) { }
     
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
  }
  public class PSNotSupportedException : System.NotSupportedException, System.Management.Automation.IContainsErrorRecord {
    public PSNotSupportedException() { }
    public PSNotSupportedException(string message) { }
    public PSNotSupportedException(string message, System.Exception innerException) { }
     
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
  }
#if COMPONENT_MODEL
  [System.ComponentModel.TypeDescriptionProviderAttribute(typeof(System.Management.Automation.PSObjectTypeDescriptionProvider))]
#endif
  public class PSObject : System.Dynamic.IDynamicMetaObjectProvider, System.IComparable, System.IFormattable {
    public const string AdaptedMemberSetName = "psadapted";
    public const string BaseObjectMemberSetName = "psbase";
    public const string ExtendedMemberSetName = "psextended";
     
    public PSObject() { }
    public PSObject(object obj) { }
     
    public object BaseObject { get { return default(object); } }
    public object ImmediateBaseObject { get { return default(object); } }
    public System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSMemberInfo> Members { get { return default(System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSMemberInfo>); } }
    public System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSMethodInfo> Methods { get { return default(System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSMethodInfo>); } }
    public System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSPropertyInfo> Properties { get { return default(System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSPropertyInfo>); } }
    public System.Collections.ObjectModel.Collection<string> TypeNames { get { return default(System.Collections.ObjectModel.Collection<string>); } }
     
    public static System.Management.Automation.PSObject AsPSObject(object obj) { return default(System.Management.Automation.PSObject); }
    public int CompareTo(object obj) { return default(int); }
    public virtual System.Management.Automation.PSObject Copy() { return default(System.Management.Automation.PSObject); }
    public override bool Equals(object obj) { return default(bool); }
    public override int GetHashCode() { return default(int); }
    public static implicit operator System.Management.Automation.PSObject (bool valueToConvert) { return default(System.Management.Automation.PSObject); }
    public static implicit operator System.Management.Automation.PSObject (System.Collections.Hashtable valueToConvert) { return default(System.Management.Automation.PSObject); }
    public static implicit operator System.Management.Automation.PSObject (double valueToConvert) { return default(System.Management.Automation.PSObject); }
    public static implicit operator System.Management.Automation.PSObject (int valueToConvert) { return default(System.Management.Automation.PSObject); }
    public static implicit operator System.Management.Automation.PSObject (string valueToConvert) { return default(System.Management.Automation.PSObject); }
    System.Dynamic.DynamicMetaObject System.Dynamic.IDynamicMetaObjectProvider.GetMetaObject(System.Linq.Expressions.Expression parameter) { return default(System.Dynamic.DynamicMetaObject); }
    public override string ToString() { return default(string); }
    public string ToString(string format, System.IFormatProvider formatProvider) { return default(string); }
  }
  public class PSObjectDisposedException : System.ObjectDisposedException, System.Management.Automation.IContainsErrorRecord {
    public PSObjectDisposedException(string objectName) : base(objectName) { }
    public PSObjectDisposedException(string message, System.Exception innerException) : base(message, innerException) { }
    public PSObjectDisposedException(string objectName, string message) : base (objectName, message) { }
     
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
  }
#if COMPONENT_MODEL
  public class PSObjectPropertyDescriptor : System.ComponentModel.PropertyDescriptor {
    public override System.ComponentModel.AttributeCollection Attributes { get { return default(System.ComponentModel.AttributeCollection); } }
    public override System.Type ComponentType { get { return default(System.Type); } }
    public override bool IsReadOnly { get { return default(bool); } }
    public override System.Type PropertyType { get { return default(System.Type); } }
     
    public override bool CanResetValue(object component) { return default(bool); }
    public override object GetValue(object component) { return default(object); }
    public override void ResetValue(object component) { }
    public override void SetValue(object component, object value) { }
    public override bool ShouldSerializeValue(object component) { return default(bool); }
  }
  public class PSObjectTypeDescriptionProvider : System.ComponentModel.TypeDescriptionProvider {
    public PSObjectTypeDescriptionProvider() { }
     
    // Events
    public event System.EventHandler<System.Management.Automation.GettingValueExceptionEventArgs> GettingValueException { add { } remove { } }
    public event System.EventHandler<System.Management.Automation.SettingValueExceptionEventArgs> SettingValueException { add { } remove { } }
     
    public override System.ComponentModel.ICustomTypeDescriptor GetTypeDescriptor(System.Type objectType, object instance) { return default(System.ComponentModel.ICustomTypeDescriptor); }
  }
  public class PSObjectTypeDescriptor : System.ComponentModel.CustomTypeDescriptor {
    public PSObjectTypeDescriptor(System.Management.Automation.PSObject instance) { }
     
    public System.Management.Automation.PSObject Instance { get { return default(System.Management.Automation.PSObject); } }
     
    // Events
    public event System.EventHandler<System.Management.Automation.GettingValueExceptionEventArgs> GettingValueException { add { } remove { } }
    public event System.EventHandler<System.Management.Automation.SettingValueExceptionEventArgs> SettingValueException { add { } remove { } }
     
    public override bool Equals(object obj) { return default(bool); }
    public override System.ComponentModel.AttributeCollection GetAttributes() { return default(System.ComponentModel.AttributeCollection); }
    public override string GetClassName() { return default(string); }
    public override string GetComponentName() { return default(string); }
    public override System.ComponentModel.TypeConverter GetConverter() { return default(System.ComponentModel.TypeConverter); }
    public override System.ComponentModel.EventDescriptor GetDefaultEvent() { return default(System.ComponentModel.EventDescriptor); }
    public override System.ComponentModel.PropertyDescriptor GetDefaultProperty() { return default(System.ComponentModel.PropertyDescriptor); }
    public override object GetEditor(System.Type editorBaseType) { return default(object); }
    public override System.ComponentModel.EventDescriptorCollection GetEvents() { return default(System.ComponentModel.EventDescriptorCollection); }
    public override System.ComponentModel.EventDescriptorCollection GetEvents(System.Attribute[] attributes) { return default(System.ComponentModel.EventDescriptorCollection); }
    public override int GetHashCode() { return default(int); }
    public override System.ComponentModel.PropertyDescriptorCollection GetProperties() { return default(System.ComponentModel.PropertyDescriptorCollection); }
    public override System.ComponentModel.PropertyDescriptorCollection GetProperties(System.Attribute[] attributes) { return default(System.ComponentModel.PropertyDescriptorCollection); }
    public override object GetPropertyOwner(System.ComponentModel.PropertyDescriptor pd) { return default(object); }
  }
#endif
  public class PSParameterizedProperty : System.Management.Automation.PSMethodInfo {
    internal PSParameterizedProperty() { }
    public bool IsGettable { get { return default(bool); } }
    public bool IsSettable { get { return default(bool); } }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public override System.Collections.ObjectModel.Collection<string> OverloadDefinitions { get { return default(System.Collections.ObjectModel.Collection<string>); } }
    public override string TypeNameOfValue { get { return default(string); } }
     
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
    public override object Invoke(params object[] arguments) { return default(object); }
    public void InvokeSet(object valueToSet, params object[] arguments) { }
    public override string ToString() { return default(string); }
  }
#if V2_PARSER_API
  public sealed class PSParseError {
    internal PSParseError() { }
    public string Message { get { return default(string); } }
    public System.Management.Automation.PSToken Token { get { return default(System.Management.Automation.PSToken); } }
     
  }
  public sealed class PSParser {
    internal PSParser() { }
    public static System.Collections.ObjectModel.Collection<System.Management.Automation.PSToken> Tokenize(object[] script, out System.Collections.ObjectModel.Collection<System.Management.Automation.PSParseError> errors) { errors = default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSParseError>); return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSToken>); }
    public static System.Collections.ObjectModel.Collection<System.Management.Automation.PSToken> Tokenize(string script, out System.Collections.ObjectModel.Collection<System.Management.Automation.PSParseError> errors) { errors = default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSParseError>); return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSToken>); }
  }
#endif
  public sealed class PSPrimitiveDictionary : System.Collections.Hashtable {
    public PSPrimitiveDictionary() { }
    public PSPrimitiveDictionary(System.Collections.Hashtable other) { }
     
    public override object this[object key] { get { return default(object); } set { } }
    public object this[string key] { get { return default(object); } set { } }
     
    public override void Add(object key, object value) { }
    public void Add(string key, bool value) { }
    public void Add(string key, bool[] value) { }
    public void Add(string key, byte value) { }
    public void Add(string key, byte[] value) { }
    public void Add(string key, char value) { }
    public void Add(string key, char[] value) { }
    public void Add(string key, System.DateTime value) { }
    public void Add(string key, System.DateTime[] value) { }
    public void Add(string key, decimal value) { }
    public void Add(string key, decimal[] value) { }
    public void Add(string key, double value) { }
    public void Add(string key, double[] value) { }
    public void Add(string key, System.Guid value) { }
    public void Add(string key, System.Guid[] value) { }
    public void Add(string key, int value) { }
    public void Add(string key, int[] value) { }
    public void Add(string key, long value) { }
    public void Add(string key, long[] value) { }
    public void Add(string key, System.Management.Automation.PSPrimitiveDictionary value) { }
    public void Add(string key, System.Management.Automation.PSPrimitiveDictionary[] value) { }
    public void Add(string key, sbyte value) { }
    public void Add(string key, sbyte[] value) { }
    public void Add(string key, float value) { }
    public void Add(string key, float[] value) { }
    public void Add(string key, string value) { }
    public void Add(string key, string[] value) { }
    public void Add(string key, System.TimeSpan value) { }
    public void Add(string key, System.TimeSpan[] value) { }
    public void Add(string key, ushort value) { }
    public void Add(string key, ushort[] value) { }
    public void Add(string key, uint value) { }
    public void Add(string key, uint[] value) { }
    public void Add(string key, ulong value) { }
    public void Add(string key, ulong[] value) { }
    public void Add(string key, System.Uri value) { }
    public void Add(string key, System.Uri[] value) { }
    public void Add(string key, System.Version value) { }
    public void Add(string key, System.Version[] value) { }
    public override object Clone() { return default(object); }
  }
  public class PSProperty : System.Management.Automation.PSPropertyInfo {
    internal PSProperty() { }
    public override bool IsGettable { get { return default(bool); } }
    public override bool IsSettable { get { return default(bool); } }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public override string TypeNameOfValue { get { return default(string); } }
    public override object Value { get { return default(object); } set { } }
     
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString() { return default(string); }
  }
  public abstract class PSPropertyAdapter {
    protected PSPropertyAdapter() { }
     
    public abstract System.Collections.ObjectModel.Collection<System.Management.Automation.PSAdaptedProperty> GetProperties(object baseObject);
    public abstract System.Management.Automation.PSAdaptedProperty GetProperty(object baseObject, string propertyName);
    public abstract string GetPropertyTypeName(System.Management.Automation.PSAdaptedProperty adaptedProperty);
    public abstract object GetPropertyValue(System.Management.Automation.PSAdaptedProperty adaptedProperty);
    public virtual System.Collections.ObjectModel.Collection<string> GetTypeNameHierarchy(object baseObject) { return default(System.Collections.ObjectModel.Collection<string>); }
    public abstract bool IsGettable(System.Management.Automation.PSAdaptedProperty adaptedProperty);
    public abstract bool IsSettable(System.Management.Automation.PSAdaptedProperty adaptedProperty);
    public abstract void SetPropertyValue(System.Management.Automation.PSAdaptedProperty adaptedProperty, object value);
  }
  public abstract class PSPropertyInfo : System.Management.Automation.PSMemberInfo {
    protected PSPropertyInfo() { }
     
    public abstract bool IsGettable { get; }
    public abstract bool IsSettable { get; }
     
  }
  public class PSPropertySet : System.Management.Automation.PSMemberInfo {
    public PSPropertySet(string name, System.Collections.Generic.IEnumerable<string> referencedPropertyNames) { }
     
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public System.Collections.ObjectModel.Collection<string> ReferencedPropertyNames { get { return default(System.Collections.ObjectModel.Collection<string>); } }
    public override string TypeNameOfValue { get { return default(string); } }
    public override object Value { get { return default(object); } set { } }
     
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString() { return default(string); }
  }
  public class PSReference {
    public PSReference(object value) { }
     
    public object Value { get { return default(object); } set { } }
     
  }
  public class PSScriptMethod : System.Management.Automation.PSMethodInfo {
    public PSScriptMethod(string name, System.Management.Automation.ScriptBlock script) { }
     
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public override System.Collections.ObjectModel.Collection<string> OverloadDefinitions { get { return default(System.Collections.ObjectModel.Collection<string>); } }
    public System.Management.Automation.ScriptBlock Script { get { return default(System.Management.Automation.ScriptBlock); } }
    public override string TypeNameOfValue { get { return default(string); } }
     
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
    public override object Invoke(params object[] arguments) { return default(object); }
    public override string ToString() { return default(string); }
  }
  public class PSScriptProperty : System.Management.Automation.PSPropertyInfo {
    public PSScriptProperty(string name, System.Management.Automation.ScriptBlock getterScript) { }
    public PSScriptProperty(string name, System.Management.Automation.ScriptBlock getterScript, System.Management.Automation.ScriptBlock setterScript) { }
     
    public System.Management.Automation.ScriptBlock GetterScript { get { return default(System.Management.Automation.ScriptBlock); } }
    public override bool IsGettable { get { return default(bool); } }
    public override bool IsSettable { get { return default(bool); } }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public System.Management.Automation.ScriptBlock SetterScript { get { return default(System.Management.Automation.ScriptBlock); } }
    public override string TypeNameOfValue { get { return default(string); } }
    public override object Value { get { return default(object); } set { } }
     
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString() { return default(string); }
  }
  public class PSSecurityException : System.Management.Automation.RuntimeException {
    public PSSecurityException() { }
    public PSSecurityException(string message) { }
    public PSSecurityException(string message, System.Exception innerException) { }
     
    public override System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override string Message { get { return default(string); } }
     
  }
  public class PSSerializer {
    internal PSSerializer() { }
    public static object Deserialize(string source) { return default(object); }
    public static object[] DeserializeAsList(string source) { return default(object[]); }
    public static string Serialize(object source) { return default(string); }
    public static string Serialize(object source, int depth) { return default(string); }
  }
  public abstract class PSSessionTypeOption {
    protected PSSessionTypeOption() { }
     
    protected internal virtual System.Management.Automation.PSSessionTypeOption ConstructObjectFromPrivateData(string privateData) { return default(System.Management.Automation.PSSessionTypeOption); }
    protected internal virtual string ConstructPrivateData() { return default(string); }
    protected internal virtual void CopyUpdatedValuesFrom(System.Management.Automation.PSSessionTypeOption updated) { }
  }
#if V2_PARSER_API
  public sealed class PSToken {
    internal PSToken() { }
    public string Content { get { return default(string); } }
    public int EndColumn { get { return default(int); } }
    public int EndLine { get { return default(int); } }
    public int Length { get { return default(int); } }
    public int Start { get { return default(int); } }
    public int StartColumn { get { return default(int); } }
    public int StartLine { get { return default(int); } }
    public System.Management.Automation.PSTokenType Type { get { return default(System.Management.Automation.PSTokenType); } }
     
    public static System.Management.Automation.PSTokenType GetPSTokenType(System.Management.Automation.Language.Token token) { return default(System.Management.Automation.PSTokenType); }
  }
  public enum PSTokenType {
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
#endif
  public class PSTraceSource {
    internal PSTraceSource() { }
    public System.Collections.Specialized.StringDictionary Attributes { get { return default(System.Collections.Specialized.StringDictionary); } }
    public string Description { get { return default(string); } set { } }
    public System.Diagnostics.TraceListenerCollection Listeners { get { return default(System.Diagnostics.TraceListenerCollection); } }
    public string Name { get { return default(string); } }
    public System.Management.Automation.PSTraceSourceOptions Options { get { return default(System.Management.Automation.PSTraceSourceOptions); } set { } }
    public System.Diagnostics.SourceSwitch Switch { get { return default(System.Diagnostics.SourceSwitch); } set { } }
     
  }
  [System.FlagsAttribute]
  public enum PSTraceSourceOptions {
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
#if TRANSACTIONS
  public sealed class PSTransaction : System.IDisposable {
    internal PSTransaction() { }
    public System.Management.Automation.RollbackSeverity RollbackPreference { get { return default(System.Management.Automation.RollbackSeverity); } }
    public System.Management.Automation.PSTransactionStatus Status { get { return default(System.Management.Automation.PSTransactionStatus); } }
    public int SubscriberCount { get { return default(int); } set { } }
     
    public void Dispose() { }
    public void Dispose(bool disposing) { }
    ~PSTransaction() { }
  }
  public sealed class PSTransactionContext : System.IDisposable {
    internal PSTransactionContext() { }
    public void Dispose() { }
    ~PSTransactionContext() { }
  }
  public enum PSTransactionStatus {
    Active = 2,
    Committed = 1,
    RolledBack = 0,
  }
#endif
  public abstract class PSTransportOption {
    protected PSTransportOption() { }
     
    protected internal virtual void LoadFromDefaults(System.Management.Automation.Runspaces.PSSessionType sessionType, bool keepAssigned) { }
  }
  public abstract class PSTypeConverter {
    protected PSTypeConverter() { }
     
    public virtual bool CanConvertFrom(System.Management.Automation.PSObject sourceValue, System.Type destinationType) { return default(bool); }
    public abstract bool CanConvertFrom(object sourceValue, System.Type destinationType);
    public virtual bool CanConvertTo(System.Management.Automation.PSObject sourceValue, System.Type destinationType) { return default(bool); }
    public abstract bool CanConvertTo(object sourceValue, System.Type destinationType);
    public virtual object ConvertFrom(System.Management.Automation.PSObject sourceValue, System.Type destinationType, System.IFormatProvider formatProvider, bool ignoreCase) { return default(object); }
    public abstract object ConvertFrom(object sourceValue, System.Type destinationType, System.IFormatProvider formatProvider, bool ignoreCase);
    public virtual object ConvertTo(System.Management.Automation.PSObject sourceValue, System.Type destinationType, System.IFormatProvider formatProvider, bool ignoreCase) { return default(object); }
    public abstract object ConvertTo(object sourceValue, System.Type destinationType, System.IFormatProvider formatProvider, bool ignoreCase);
  }
  public class PSTypeName {
    public PSTypeName(string name) { }
    public PSTypeName(System.Type type) { }
     
    public string Name { get { return default(string); } }
    public System.Type Type { get { return default(System.Type); } }
     
    public override string ToString() { return default(string); }
  }
  [System.AttributeUsageAttribute((AttributeTargets)384, AllowMultiple=false)]
  public class PSTypeNameAttribute : System.Attribute {
    public PSTypeNameAttribute(string psTypeName) { }
     
    public string PSTypeName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
     
  }
  public class PSVariable {
    public PSVariable(string name) { }
    public PSVariable(string name, object value) { }
    public PSVariable(string name, object value, System.Management.Automation.ScopedItemOptions options) { }
    public PSVariable(string name, object value, System.Management.Automation.ScopedItemOptions options, System.Collections.ObjectModel.Collection<System.Attribute> attributes) { }
     
    public System.Collections.ObjectModel.Collection<System.Attribute> Attributes { get { return default(System.Collections.ObjectModel.Collection<System.Attribute>); } }
    public virtual string Description { get { return default(string); } set { } }
    public System.Management.Automation.PSModuleInfo Module { get { return default(System.Management.Automation.PSModuleInfo); } }
    public string ModuleName { get { return default(string); } }
    public string Name { get { return default(string); } }
    public virtual System.Management.Automation.ScopedItemOptions Options { get { return default(System.Management.Automation.ScopedItemOptions); } set { } }
    public virtual object Value { get { return default(object); } set { } }
    public System.Management.Automation.SessionStateEntryVisibility Visibility { get { return default(System.Management.Automation.SessionStateEntryVisibility); } set { } }
     
    public virtual bool IsValidValue(object value) { return default(bool); }
  }
  public sealed class PSVariableIntrinsics {
    internal PSVariableIntrinsics() { }
    public System.Management.Automation.PSVariable Get(string name) { return default(System.Management.Automation.PSVariable); }
    public object GetValue(string name) { return default(object); }
    public object GetValue(string name, object defaultValue) { return default(object); }
    public void Remove(System.Management.Automation.PSVariable variable) { }
    public void Remove(string name) { }
    public void Set(System.Management.Automation.PSVariable variable) { }
    public void Set(string name, object value) { }
  }
  public class PSVariableProperty : System.Management.Automation.PSNoteProperty {
    public PSVariableProperty(System.Management.Automation.PSVariable variable) : base (default(string), default(object)) { }
     
    public override bool IsGettable { get { return default(bool); } }
    public override bool IsSettable { get { return default(bool); } }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public override string TypeNameOfValue { get { return default(string); } }
    public override object Value { get { return default(object); } set { } }
     
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString() { return default(string); }
  }
  public class ReadOnlyPSMemberInfoCollection<T> : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable where T : System.Management.Automation.PSMemberInfo {
    internal ReadOnlyPSMemberInfoCollection() { }
    public int Count { get { return default(int); } }
    public T this[int index] { get { return default(T); } }
    public T this[string name] { get { return default(T); } }
     
    public virtual System.Collections.Generic.IEnumerator<T> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<T>); }
    public System.Management.Automation.ReadOnlyPSMemberInfoCollection<T> Match(string name) { return default(System.Management.Automation.ReadOnlyPSMemberInfoCollection<T>); }
    public System.Management.Automation.ReadOnlyPSMemberInfoCollection<T> Match(string name, System.Management.Automation.PSMemberTypes memberTypes) { return default(System.Management.Automation.ReadOnlyPSMemberInfoCollection<T>); }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
  }
  public class RedirectedException : System.Management.Automation.RuntimeException {
    public RedirectedException() { }
    public RedirectedException(string message) { }
    public RedirectedException(string message, System.Exception innerException) { }
  }
  public class RemoteCommandInfo : System.Management.Automation.CommandInfo {
    internal RemoteCommandInfo() { }
    public override string Definition { get { return default(string); } }
    public override System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName> OutputType { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName>); } }
     
  }
  public class RemoteException : System.Management.Automation.RuntimeException {
    public RemoteException() { }
    public RemoteException(string message) { }
    public RemoteException(string message, System.Exception innerException) { }
     
    public override System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public System.Management.Automation.PSObject SerializedRemoteException { get { return default(System.Management.Automation.PSObject); } }
    public System.Management.Automation.PSObject SerializedRemoteInvocationInfo { get { return default(System.Management.Automation.PSObject); } }
     
  }
  [System.FlagsAttribute]
  public enum RemoteStreamOptions {
    AddInvocationInfo = 15,
    AddInvocationInfoToDebugRecord = 4,
    AddInvocationInfoToErrorRecord = 1,
    AddInvocationInfoToVerboseRecord = 8,
    AddInvocationInfoToWarningRecord = 2,
  }
  public enum RemotingBehavior {
    Custom = 2,
    None = 0,
    PowerShell = 1,
  }
  public enum RemotingCapability {
    None = 0,
    OwnedByCommand = 3,
    PowerShell = 1,
    SupportedByCommand = 2,
  }
  public abstract class Repository<T> where T : class {
    protected Repository(string identifier) { }
     
    public void Add(T item) { }
    public T GetItem(System.Guid instanceId) { return default(T); }
    public System.Collections.Generic.List<T> GetItems() { return default(System.Collections.Generic.List<T>); }
    protected abstract System.Guid GetKey(T item);
    public void Remove(T item) { }
  }
  public enum ReturnContainers {
    ReturnAllContainers = 1,
    ReturnMatchingContainers = 0,
  }
  public enum RollbackSeverity {
    Error = 0,
    Never = 2,
    TerminatingError = 1,
  }
#if V1_PIPELINE_API
  public class RunspaceInvoke : System.IDisposable {
    public RunspaceInvoke() { }
    public RunspaceInvoke(System.Management.Automation.Runspaces.Runspace runspace) { }
    public RunspaceInvoke(System.Management.Automation.Runspaces.RunspaceConfiguration runspaceConfiguration) { }
    public RunspaceInvoke(string consoleFilePath) { }
     
    public void Dispose() { }
    protected virtual void Dispose(bool disposing) { }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Invoke(string script) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Invoke(string script, System.Collections.IEnumerable input) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Invoke(string script, System.Collections.IEnumerable input, out System.Collections.IList errors) { errors = default(System.Collections.IList); return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
  }
#endif
  public enum RunspaceMode {
    CurrentRunspace = 0,
    NewRunspace = 1,
  }
  public sealed class RunspacePoolStateInfo {
    public RunspacePoolStateInfo(System.Management.Automation.Runspaces.RunspacePoolState state, System.Exception reason) { }
     
    public System.Exception Reason { get { return default(System.Exception); } }
    public System.Management.Automation.Runspaces.RunspacePoolState State { get { return default(System.Management.Automation.Runspaces.RunspacePoolState); } }
     
  }
  public class RunspaceRepository : System.Management.Automation.Repository<System.Management.Automation.Runspaces.PSSession> {
    internal RunspaceRepository() : base (default(string)) { }
    public System.Collections.Generic.List<System.Management.Automation.Runspaces.PSSession> Runspaces { get { return default(System.Collections.Generic.List<System.Management.Automation.Runspaces.PSSession>); } }
     
    protected override System.Guid GetKey(System.Management.Automation.Runspaces.PSSession item) { return default(System.Guid); }
  }
  public class RuntimeDefinedParameter {
    public RuntimeDefinedParameter() { }
    public RuntimeDefinedParameter(string name, System.Type parameterType, System.Collections.ObjectModel.Collection<System.Attribute> attributes) { }
     
    public System.Collections.ObjectModel.Collection<System.Attribute> Attributes { get { return default(System.Collections.ObjectModel.Collection<System.Attribute>); } }
    public bool IsSet { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public string Name { get { return default(string); } set { } }
    public System.Type ParameterType { get { return default(System.Type); } set { } }
    public object Value { get { return default(object); } set { } }
     
  }
  public class RuntimeDefinedParameterDictionary : System.Collections.Generic.Dictionary<string, System.Management.Automation.RuntimeDefinedParameter> {
    public RuntimeDefinedParameterDictionary() { }
     
    public object Data { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(object); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public string HelpFile { get { return default(string); } set { } }
     
  }
  public class RuntimeException : System.Exception, System.Management.Automation.IContainsErrorRecord {
    public RuntimeException() { }
    public RuntimeException(string message) { }
    public RuntimeException(string message, System.Exception innerException) { }
    public RuntimeException(string message, System.Exception innerException, System.Management.Automation.ErrorRecord errorRecord) { }
     
    public virtual System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override string StackTrace { get { return default(string); } }
    public bool WasThrownFromThrowStatement { get { return default(bool); } set { } }
  }
  [System.FlagsAttribute]
  public enum ScopedItemOptions {
    AllScope = 8,
    Constant = 2,
    None = 0,
    Private = 4,
    ReadOnly = 1,
    Unspecified = 16,
  }
  public class ScriptBlock {
    public System.Management.Automation.Language.Ast Ast { get { return default(System.Management.Automation.Language.Ast); } }
    public System.Collections.Generic.List<System.Attribute> Attributes { get { return default(System.Collections.Generic.List<System.Attribute>); } }
    public string File { get { return default(string); } }
    public bool IsFilter { get { return default(bool); } set { } }
    public System.Management.Automation.PSModuleInfo Module { get { return default(System.Management.Automation.PSModuleInfo); } }
#if V2_PARSER_API
    public System.Management.Automation.PSToken StartPosition { get { return default(System.Management.Automation.PSToken); } }
#endif
     
    public void CheckRestrictedLanguage(System.Collections.Generic.IEnumerable<string> allowedCommands, System.Collections.Generic.IEnumerable<string> allowedVariables, bool allowEnvironmentVariables) { }
    public static System.Management.Automation.ScriptBlock Create(string script) { return default(System.Management.Automation.ScriptBlock); }
    public System.Management.Automation.ScriptBlock GetNewClosure() { return default(System.Management.Automation.ScriptBlock); }
    public System.Management.Automation.PowerShell GetPowerShell(System.Collections.Generic.Dictionary<string, object> variables, out System.Collections.Generic.Dictionary<string, object> usingVariables, params object[] args) { usingVariables = default(System.Collections.Generic.Dictionary<string, object>); return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell GetPowerShell(System.Collections.Generic.Dictionary<string, object> variables, params object[] args) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell GetPowerShell(params object[] args) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.SteppablePipeline GetSteppablePipeline() { return default(System.Management.Automation.SteppablePipeline); }
    public System.Management.Automation.SteppablePipeline GetSteppablePipeline(System.Management.Automation.CommandOrigin commandOrigin) { return default(System.Management.Automation.SteppablePipeline); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Invoke(params object[] args) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public object InvokeReturnAsIs(params object[] args) { return default(object); }
    public override string ToString() { return default(string); }
  }
  public class ScriptBlockToPowerShellNotSupportedException : System.Management.Automation.RuntimeException {
    public ScriptBlockToPowerShellNotSupportedException() { }
    public ScriptBlockToPowerShellNotSupportedException(string message) { }
    public ScriptBlockToPowerShellNotSupportedException(string message, System.Exception innerException) { }
  }
  public class ScriptCallDepthException : System.Exception, System.Management.Automation.IContainsErrorRecord {
    public ScriptCallDepthException() { }
    public ScriptCallDepthException(string message) { }
    public ScriptCallDepthException(string message, System.Exception innerException) { }
     
    public int CallDepth { get { return default(int); } }
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
  }
  public class ScriptInfo : System.Management.Automation.CommandInfo {
    internal ScriptInfo() { }
    public override string Definition { get { return default(string); } }
    public override System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName> OutputType { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName>); } }
    public System.Management.Automation.ScriptBlock ScriptBlock { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.ScriptBlock); } }
     
    public override string ToString() { return default(string); }
  }
  public class ScriptRequiresException : System.Management.Automation.RuntimeException {
    public ScriptRequiresException() { }
    public ScriptRequiresException(string message) { }
    public ScriptRequiresException(string message, System.Exception innerException) { }
     
    public string CommandName { get { return default(string); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<string> MissingPSSnapIns { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<string>); } }
    public System.Version RequiresPSVersion { get { return default(System.Version); } }
    public string RequiresShellId { get { return default(string); } }
    public string RequiresShellPath { get { return default(string); } }
  }
  public sealed class SecurityDescriptorCmdletProviderIntrinsics {
    internal SecurityDescriptorCmdletProviderIntrinsics() { }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Get(string path, System.Security.AccessControl.AccessControlSections includeSections) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Security.AccessControl.ObjectSecurity NewFromPath(string path, System.Security.AccessControl.AccessControlSections includeSections) { return default(System.Security.AccessControl.ObjectSecurity); }
    public System.Security.AccessControl.ObjectSecurity NewOfType(string providerId, string type, System.Security.AccessControl.AccessControlSections includeSections) { return default(System.Security.AccessControl.ObjectSecurity); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Set(string path, System.Security.AccessControl.ObjectSecurity sd) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
  }
  [System.FlagsAttribute]
  public enum SessionCapabilities {
    Language = 4,
    RemoteServer = 1,
    WorkflowServer = 2,
  }
  public sealed class SessionState {
    public SessionState() { }
     
    public System.Collections.Generic.List<string> Applications { get { return default(System.Collections.Generic.List<string>); } }
    public System.Management.Automation.DriveManagementIntrinsics Drive { get { return default(System.Management.Automation.DriveManagementIntrinsics); } }
    public System.Management.Automation.CommandInvocationIntrinsics InvokeCommand { get { return default(System.Management.Automation.CommandInvocationIntrinsics); } }
    public System.Management.Automation.ProviderIntrinsics InvokeProvider { get { return default(System.Management.Automation.ProviderIntrinsics); } }
    public System.Management.Automation.PSLanguageMode LanguageMode { get { return default(System.Management.Automation.PSLanguageMode); } set { } }
    public System.Management.Automation.PSModuleInfo Module { get { return default(System.Management.Automation.PSModuleInfo); } }
    public System.Management.Automation.PathIntrinsics Path { get { return default(System.Management.Automation.PathIntrinsics); } }
    public System.Management.Automation.CmdletProviderManagementIntrinsics Provider { get { return default(System.Management.Automation.CmdletProviderManagementIntrinsics); } }
    public System.Management.Automation.PSVariableIntrinsics PSVariable { get { return default(System.Management.Automation.PSVariableIntrinsics); } }
    public System.Collections.Generic.List<string> Scripts { get { return default(System.Collections.Generic.List<string>); } }
    public bool UseFullLanguageModeInDebugger { get { return default(bool); } }
     
    public static bool IsVisible(System.Management.Automation.CommandOrigin origin, System.Management.Automation.CommandInfo commandInfo) { return default(bool); }
    public static bool IsVisible(System.Management.Automation.CommandOrigin origin, System.Management.Automation.PSVariable variable) { return default(bool); }
    public static bool IsVisible(System.Management.Automation.CommandOrigin origin, object valueToCheck) { return default(bool); }
    public static void ThrowIfNotVisible(System.Management.Automation.CommandOrigin origin, object valueToCheck) { }
  }
  public enum SessionStateCategory {
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
  public enum SessionStateEntryVisibility {
    Private = 1,
    Public = 0,
  }
  public class SessionStateException : System.Management.Automation.RuntimeException {
    public SessionStateException() { }
    public SessionStateException(string message) { }
    public SessionStateException(string message, System.Exception innerException) { }
     
    public override System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public string ItemName { get { return default(string); } }
    public System.Management.Automation.SessionStateCategory SessionStateCategory { get { return default(System.Management.Automation.SessionStateCategory); } }
  }
  public class SessionStateOverflowException : System.Management.Automation.SessionStateException {
    public SessionStateOverflowException() { }
    public SessionStateOverflowException(string message) { }
    public SessionStateOverflowException(string message, System.Exception innerException) { }
  }
  public class SessionStateUnauthorizedAccessException : System.Management.Automation.SessionStateException {
    public SessionStateUnauthorizedAccessException() { }
    public SessionStateUnauthorizedAccessException(string message) { }
    public SessionStateUnauthorizedAccessException(string message, System.Exception innerException) { }
  }
  public class SettingValueExceptionEventArgs : System.EventArgs {
    internal SettingValueExceptionEventArgs() { }
    public System.Exception Exception { get { return default(System.Exception); } }
    public bool ShouldThrow { get { return default(bool); } set { } }
     
  }
  public class SetValueException : System.Management.Automation.ExtendedTypeSystemException {
    public SetValueException() { }
    public SetValueException(string message) { }
    public SetValueException(string message, System.Exception innerException) { }
  }
  public class SetValueInvocationException : System.Management.Automation.SetValueException {
    public SetValueInvocationException() { }
    public SetValueInvocationException(string message) { }
    public SetValueInvocationException(string message, System.Exception innerException) { }
  }
  [System.FlagsAttribute]
  public enum ShouldProcessReason {
    None = 0,
    WhatIf = 1,
  }
  public sealed class Signature {
    internal Signature() { }
    public string Path { get { return default(string); } }
    public System.Security.Cryptography.X509Certificates.X509Certificate2 SignerCertificate { get { return default(System.Security.Cryptography.X509Certificates.X509Certificate2); } }
    public System.Management.Automation.SignatureStatus Status { get { return default(System.Management.Automation.SignatureStatus); } }
    public string StatusMessage { get { return default(string); } }
    public System.Security.Cryptography.X509Certificates.X509Certificate2 TimeStamperCertificate { get { return default(System.Security.Cryptography.X509Certificates.X509Certificate2); } }
     
  }
  public enum SignatureStatus {
    HashMismatch = 3,
    Incompatible = 6,
    NotSigned = 2,
    NotSupportedFileFormat = 5,
    NotTrusted = 4,
    UnknownError = 1,
    Valid = 0,
  }
  public enum SigningOption {
    AddFullCertificateChain = 1,
    AddFullCertificateChainExceptRoot = 2,
    AddOnlyCertificate = 0,
    Default = 2,
  }
  [System.FlagsAttribute]
  public enum SplitOptions {
    CultureInvariant = 4,
    ExplicitCapture = 128,
    IgnoreCase = 64,
    IgnorePatternWhitespace = 8,
    Multiline = 16,
    RegexMatch = 2,
    SimpleMatch = 1,
    Singleline = 32,
  }
  public sealed class SteppablePipeline : System.IDisposable {
    internal SteppablePipeline() { }
    public void Begin(bool expectInput) { }
    public void Begin(bool expectInput, System.Management.Automation.EngineIntrinsics contextToRedirectTo) { }
    public void Begin(System.Management.Automation.Internal.InternalCommand command) { }
    public void Dispose() { }
    public System.Array End() { return default(System.Array); }
    ~SteppablePipeline() { }
    public System.Array Process() { return default(System.Array); }
    public System.Array Process(System.Management.Automation.PSObject input) { return default(System.Array); }
    public System.Array Process(object input) { return default(System.Array); }
  }
  [System.AttributeUsageAttribute((AttributeTargets)384)]
  public sealed class SupportsWildcardsAttribute : System.Management.Automation.Internal.ParsingBaseAttribute {
    public SupportsWildcardsAttribute() { }
  }
  [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
  public partial struct SwitchParameter {
    public SwitchParameter(bool isPresent) { throw new System.NotImplementedException(); }
     
    public bool IsPresent { get { return default(bool); } }
    public static System.Management.Automation.SwitchParameter Present { get { return default(System.Management.Automation.SwitchParameter); } }
     
    public override bool Equals(object obj) { return default(bool); }
    public override int GetHashCode() { return default(int); }
    public static bool operator ==(bool first, System.Management.Automation.SwitchParameter second) { return default(bool); }
    public static bool operator ==(System.Management.Automation.SwitchParameter first, bool second) { return default(bool); }
    public static bool operator ==(System.Management.Automation.SwitchParameter first, System.Management.Automation.SwitchParameter second) { return default(bool); }
    public static implicit operator System.Management.Automation.SwitchParameter (bool value) { return default(System.Management.Automation.SwitchParameter); }
    public static implicit operator bool (System.Management.Automation.SwitchParameter switchParameter) { return default(bool); }
    public static bool operator !=(bool first, System.Management.Automation.SwitchParameter second) { return default(bool); }
    public static bool operator !=(System.Management.Automation.SwitchParameter first, bool second) { return default(bool); }
    public static bool operator !=(System.Management.Automation.SwitchParameter first, System.Management.Automation.SwitchParameter second) { return default(bool); }
    public bool ToBool() { return default(bool); }
    public override string ToString() { return default(string); }
  }
#if FORMAT_API
  public sealed class TableControl : System.Management.Automation.PSControl {
    public TableControl() { }
    public TableControl(System.Management.Automation.TableControlRow tableControlRow) { }
    public TableControl(System.Management.Automation.TableControlRow tableControlRow, System.Collections.Generic.IEnumerable<System.Management.Automation.TableControlColumnHeader> tableControlColumnHeaders) { }
     
    public System.Collections.Generic.List<System.Management.Automation.TableControlColumnHeader> Headers { get { return default(System.Collections.Generic.List<System.Management.Automation.TableControlColumnHeader>); } }
    public System.Collections.Generic.List<System.Management.Automation.TableControlRow> Rows { get { return default(System.Collections.Generic.List<System.Management.Automation.TableControlRow>); } }
     
    public override string ToString() { return default(string); }
  }
  public sealed class TableControlColumn {
    public TableControlColumn(System.Management.Automation.Alignment alignment, System.Management.Automation.DisplayEntry entry) { }
     
    public System.Management.Automation.Alignment Alignment { get { return default(System.Management.Automation.Alignment); } }
    public System.Management.Automation.DisplayEntry DisplayEntry { get { return default(System.Management.Automation.DisplayEntry); } }
     
    public override string ToString() { return default(string); }
  }
  public sealed class TableControlColumnHeader {
    public TableControlColumnHeader(string label, int width, System.Management.Automation.Alignment alignment) { }
     
    public System.Management.Automation.Alignment Alignment { get { return default(System.Management.Automation.Alignment); } }
    public string Label { get { return default(string); } }
    public int Width { get { return default(int); } }
     
  }
  public sealed class TableControlRow {
    public TableControlRow() { }
    public TableControlRow(System.Collections.Generic.IEnumerable<System.Management.Automation.TableControlColumn> columns) { }
     
    public System.Collections.Generic.List<System.Management.Automation.TableControlColumn> Columns { get { return default(System.Collections.Generic.List<System.Management.Automation.TableControlColumn>); } }
     
  }
#endif
  [System.AttributeUsageAttribute((AttributeTargets)384)]
  public abstract class ValidateArgumentsAttribute : System.Management.Automation.Internal.CmdletMetadataAttribute {
    protected ValidateArgumentsAttribute() { }
     
    protected abstract void Validate(object arguments, System.Management.Automation.EngineIntrinsics engineIntrinsics);
  }
  [System.AttributeUsageAttribute((AttributeTargets)384)]
  public sealed class ValidateCountAttribute : System.Management.Automation.ValidateArgumentsAttribute {
    public ValidateCountAttribute(int minLength, int maxLength) { }
     
    public int MaxLength { get { return default(int); } }
    public int MinLength { get { return default(int); } }
     
    protected override void Validate(object arguments, System.Management.Automation.EngineIntrinsics engineIntrinsics) { }
  }
  [System.AttributeUsageAttribute((AttributeTargets)384)]
  public abstract class ValidateEnumeratedArgumentsAttribute : System.Management.Automation.ValidateArgumentsAttribute {
    protected ValidateEnumeratedArgumentsAttribute() { }
     
    protected sealed override void Validate(object arguments, System.Management.Automation.EngineIntrinsics engineIntrinsics) { }
    protected abstract void ValidateElement(object element);
  }
  [System.AttributeUsageAttribute((AttributeTargets)384)]
  public sealed class ValidateLengthAttribute : System.Management.Automation.ValidateEnumeratedArgumentsAttribute {
    public ValidateLengthAttribute(int minLength, int maxLength) { }
     
    public int MaxLength { get { return default(int); } }
    public int MinLength { get { return default(int); } }
     
    protected override void ValidateElement(object element) { }
  }
  [System.AttributeUsageAttribute((AttributeTargets)384)]
  public sealed class ValidateNotNullAttribute : System.Management.Automation.ValidateArgumentsAttribute {
    public ValidateNotNullAttribute() { }
     
    protected override void Validate(object arguments, System.Management.Automation.EngineIntrinsics engineIntrinsics) { }
  }
  [System.AttributeUsageAttribute((AttributeTargets)384)]
  public sealed class ValidateNotNullOrEmptyAttribute : System.Management.Automation.ValidateArgumentsAttribute {
    public ValidateNotNullOrEmptyAttribute() { }
     
    protected override void Validate(object arguments, System.Management.Automation.EngineIntrinsics engineIntrinsics) { }
  }
  [System.AttributeUsageAttribute((AttributeTargets)384)]
  public sealed class ValidatePatternAttribute : System.Management.Automation.ValidateEnumeratedArgumentsAttribute {
    public ValidatePatternAttribute(string regexPattern) { }
     
    public System.Text.RegularExpressions.RegexOptions Options { get { return default(System.Text.RegularExpressions.RegexOptions); } set { } }
    public string RegexPattern { get { return default(string); } }
     
    protected override void ValidateElement(object element) { }
  }
  [System.AttributeUsageAttribute((AttributeTargets)384)]
  public sealed class ValidateRangeAttribute : System.Management.Automation.ValidateEnumeratedArgumentsAttribute {
    public ValidateRangeAttribute(object minRange, object maxRange) { }
     
    public object MaxRange { get { return default(object); } }
    public object MinRange { get { return default(object); } }
     
    protected override void ValidateElement(object element) { }
  }
  public sealed class ValidateScriptAttribute : System.Management.Automation.ValidateEnumeratedArgumentsAttribute {
    public ValidateScriptAttribute(System.Management.Automation.ScriptBlock scriptBlock) { }
     
    public System.Management.Automation.ScriptBlock ScriptBlock { get { return default(System.Management.Automation.ScriptBlock); } }
     
    protected override void ValidateElement(object element) { }
  }
  [System.AttributeUsageAttribute((AttributeTargets)384)]
  public sealed class ValidateSetAttribute : System.Management.Automation.ValidateEnumeratedArgumentsAttribute {
    public ValidateSetAttribute(params string[] validValues) { }
     
    public bool IgnoreCase { get { return default(bool); } set { } }
    public System.Collections.Generic.IList<string> ValidValues { get { return default(System.Collections.Generic.IList<string>); } }
     
    protected override void ValidateElement(object element) { }
  }
  public class ValidationMetadataException : System.Management.Automation.MetadataException {
    public ValidationMetadataException() { }
    public ValidationMetadataException(string message) { }
    public ValidationMetadataException(string message, System.Exception innerException) { }
  }
  public enum VariableAccessMode {
    Read = 0,
    ReadWrite = 2,
    Write = 1,
  }
  public class VariableBreakpoint : System.Management.Automation.Breakpoint {
    internal VariableBreakpoint() { }
    public System.Management.Automation.VariableAccessMode AccessMode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.VariableAccessMode); } }
    public string Variable { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
     
    public override string ToString() { return default(string); }
  }
  public class VariablePath {
    public VariablePath(string path) { }
     
    public string DriveName { get { return default(string); } }
    public bool IsDriveQualified { get { return default(bool); } }
    public bool IsGlobal { get { return default(bool); } }
    public bool IsLocal { get { return default(bool); } }
    public bool IsPrivate { get { return default(bool); } }
    public bool IsScript { get { return default(bool); } }
    public bool IsUnqualified { get { return default(bool); } }
    public bool IsUnscopedVariable { get { return default(bool); } }
    public bool IsVariable { get { return default(bool); } }
    public string UserPath { get { return default(string); } }
     
    public override string ToString() { return default(string); }
  }
  [System.Runtime.Serialization.DataContractAttribute]
  public class VerboseRecord : System.Management.Automation.InformationalRecord {
    public VerboseRecord(System.Management.Automation.PSObject record) { }
    public VerboseRecord(string message) { }
  }
  public static class VerbsCommon {
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
  public static class VerbsCommunications {
    public const string Connect = "Connect";
    public const string Disconnect = "Disconnect";
    public const string Read = "Read";
    public const string Receive = "Receive";
    public const string Send = "Send";
    public const string Write = "Write";
  }
  public static class VerbsData {
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
  public static class VerbsDiagnostic {
    public const string Debug = "Debug";
    public const string Measure = "Measure";
    public const string Ping = "Ping";
    public const string Repair = "Repair";
    public const string Resolve = "Resolve";
    public const string Test = "Test";
    public const string Trace = "Trace";
  }
  public static class VerbsLifecycle {
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
  public static class VerbsOther {
    public const string Use = "Use";
  }
  public static class VerbsSecurity {
    public const string Block = "Block";
    public const string Grant = "Grant";
    public const string Protect = "Protect";
    public const string Revoke = "Revoke";
    public const string Unblock = "Unblock";
    public const string Unprotect = "Unprotect";
  }
  [System.Runtime.Serialization.DataContractAttribute]
  public class WarningRecord : System.Management.Automation.InformationalRecord {
    public WarningRecord(System.Management.Automation.PSObject record) { }
    public WarningRecord(string message) { }
    public WarningRecord(string fullyQualifiedWarningId, System.Management.Automation.PSObject record) { }
    public WarningRecord(string fullyQualifiedWarningId, string message) { }
     
    public string FullyQualifiedWarningId { get { return default(string); } }
     
  }
#if FORMAT_API
  public sealed class WideControl : System.Management.Automation.PSControl {
    public WideControl() { }
    public WideControl(System.Collections.Generic.IEnumerable<System.Management.Automation.WideControlEntryItem> wideEntries) { }
    public WideControl(System.Collections.Generic.IEnumerable<System.Management.Automation.WideControlEntryItem> wideEntries, uint columns) { }
    public WideControl(uint columns) { }
     
    public System.Management.Automation.Alignment Alignment { get { return default(System.Management.Automation.Alignment); } }
    public uint Columns { get { return default(uint); } }
    public System.Collections.Generic.List<System.Management.Automation.WideControlEntryItem> Entries { get { return default(System.Collections.Generic.List<System.Management.Automation.WideControlEntryItem>); } }
     
    public override string ToString() { return default(string); }
  }
  public sealed class WideControlEntryItem {
    public WideControlEntryItem(System.Management.Automation.DisplayEntry entry) { }
    public WideControlEntryItem(System.Management.Automation.DisplayEntry entry, System.Collections.Generic.IEnumerable<string> selectedBy) { }
     
    public System.Management.Automation.DisplayEntry DisplayEntry { get { return default(System.Management.Automation.DisplayEntry); } }
    public System.Collections.Generic.List<string> SelectedBy { get { return default(System.Collections.Generic.List<string>); } }
     
  }
#endif
  [System.FlagsAttribute]
  public enum WildcardOptions {
    Compiled = 1,
    CultureInvariant = 4,
    IgnoreCase = 2,
    None = 0,
  }
  public sealed class WildcardPattern {
    public WildcardPattern(string pattern) { }
    public WildcardPattern(string pattern, System.Management.Automation.WildcardOptions options) { }
     
    public static bool ContainsWildcardCharacters(string pattern) { return default(bool); }
    public static string Escape(string pattern) { return default(string); }
    public bool IsMatch(string input) { return default(bool); }
    public string ToWql() { return default(string); }
    public static string Unescape(string pattern) { return default(string); }
  }
  public class WildcardPatternException : System.Management.Automation.RuntimeException {
    public WildcardPatternException() { }
    public WildcardPatternException(string message) { }
    public WildcardPatternException(string message, System.Exception innerException) { }
  }
#if WORKFLOW
  public class WorkflowInfo : System.Management.Automation.FunctionInfo {
    public WorkflowInfo(string name, string definition, System.Management.Automation.ScriptBlock workflow, string xamlDefinition, System.Management.Automation.WorkflowInfo[] workflowsCalled) { }
    public WorkflowInfo(string name, string definition, System.Management.Automation.ScriptBlock workflow, string xamlDefinition, System.Management.Automation.WorkflowInfo[] workflowsCalled, System.Management.Automation.PSModuleInfo module) { }
     
    public override string Definition { get { return default(string); } }
    public string NestedXamlDefinition { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.WorkflowInfo> WorkflowsCalled { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.WorkflowInfo>); } }
    public string XamlDefinition { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
     
    protected internal override void Update(System.Management.Automation.FunctionInfo function, bool force, System.Management.Automation.ScopedItemOptions options, string helpFile) { }
  }
#endif
}
namespace System.Management.Automation.Host {
  [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
  public partial struct BufferCell {
    public BufferCell(char character, System.ConsoleColor foreground, System.ConsoleColor background, System.Management.Automation.Host.BufferCellType bufferCellType) { throw new System.NotImplementedException(); }
     
    public System.ConsoleColor BackgroundColor { get { return default(System.ConsoleColor); } set { } }
    public System.Management.Automation.Host.BufferCellType BufferCellType { get { return default(System.Management.Automation.Host.BufferCellType); } set { } }
    public char Character { get { return default(char); } set { } }
    public System.ConsoleColor ForegroundColor { get { return default(System.ConsoleColor); } set { } }
     
    public override bool Equals(object obj) { return default(bool); }
    public override int GetHashCode() { return default(int); }
    public static bool operator ==(System.Management.Automation.Host.BufferCell first, System.Management.Automation.Host.BufferCell second) { return default(bool); }
    public static bool operator !=(System.Management.Automation.Host.BufferCell first, System.Management.Automation.Host.BufferCell second) { return default(bool); }
    public override string ToString() { return default(string); }
  }
  public enum BufferCellType {
    Complete = 0,
    Leading = 1,
    Trailing = 2,
  }
  public sealed class ChoiceDescription {
    public ChoiceDescription(string label) { }
    public ChoiceDescription(string label, string helpMessage) { }
     
    public string HelpMessage { get { return default(string); } set { } }
    public string Label { get { return default(string); } }
     
  }
  [System.FlagsAttribute]
  public enum ControlKeyStates {
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
  [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
  public partial struct Coordinates {
    public Coordinates(int x, int y) { throw new System.NotImplementedException(); }
     
    public int X { get { return default(int); } set { } }
    public int Y { get { return default(int); } set { } }
     
    public override bool Equals(object obj) { return default(bool); }
    public override int GetHashCode() { return default(int); }
    public static bool operator ==(System.Management.Automation.Host.Coordinates first, System.Management.Automation.Host.Coordinates second) { return default(bool); }
    public static bool operator !=(System.Management.Automation.Host.Coordinates first, System.Management.Automation.Host.Coordinates second) { return default(bool); }
    public override string ToString() { return default(string); }
  }
  public class FieldDescription {
    public FieldDescription(string name) { }
     
    public System.Collections.ObjectModel.Collection<System.Attribute> Attributes { get { return default(System.Collections.ObjectModel.Collection<System.Attribute>); } }
    public System.Management.Automation.PSObject DefaultValue { get { return default(System.Management.Automation.PSObject); } set { } }
    public string HelpMessage { get { return default(string); } set { } }
    public bool IsMandatory { get { return default(bool); } set { } }
    public string Label { get { return default(string); } set { } }
    public string Name { get { return default(string); } }
    public string ParameterAssemblyFullName { get { return default(string); } }
    public string ParameterTypeFullName { get { return default(string); } }
    public string ParameterTypeName { get { return default(string); } }
     
    public void SetParameterType(System.Type parameterType) { }
  }
  public class HostException : System.Management.Automation.RuntimeException {
    public HostException() { }
    public HostException(string message) { }
    public HostException(string message, System.Exception innerException) { }
    public HostException(string message, System.Exception innerException, string errorId, System.Management.Automation.ErrorCategory errorCategory) { }
  }
  public partial interface IHostSupportsInteractiveSession {
    bool IsRunspacePushed { get; }
    System.Management.Automation.Runspaces.Runspace Runspace { get; }
     
    void PopRunspace();
    void PushRunspace(System.Management.Automation.Runspaces.Runspace runspace);
  }
  public partial interface IHostUISupportsMultipleChoiceSelection {
    System.Collections.ObjectModel.Collection<int> PromptForChoice(string caption, string message, System.Collections.ObjectModel.Collection<System.Management.Automation.Host.ChoiceDescription> choices, System.Collections.Generic.IEnumerable<int> defaultChoices);
  }
  [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
  public partial struct KeyInfo {
    public KeyInfo(int virtualKeyCode, char ch, System.Management.Automation.Host.ControlKeyStates controlKeyState, bool keyDown) { throw new System.NotImplementedException(); }
     
    public char Character { get { return default(char); } set { } }
    public System.Management.Automation.Host.ControlKeyStates ControlKeyState { get { return default(System.Management.Automation.Host.ControlKeyStates); } set { } }
    public bool KeyDown { get { return default(bool); } set { } }
    public int VirtualKeyCode { get { return default(int); } set { } }
     
    public override bool Equals(object obj) { return default(bool); }
    public override int GetHashCode() { return default(int); }
    public static bool operator ==(System.Management.Automation.Host.KeyInfo first, System.Management.Automation.Host.KeyInfo second) { return default(bool); }
    public static bool operator !=(System.Management.Automation.Host.KeyInfo first, System.Management.Automation.Host.KeyInfo second) { return default(bool); }
    public override string ToString() { return default(string); }
  }
  public class PromptingException : System.Management.Automation.Host.HostException {
    public PromptingException() { }
    public PromptingException(string message) { }
    public PromptingException(string message, System.Exception innerException) { }
    public PromptingException(string message, System.Exception innerException, string errorId, System.Management.Automation.ErrorCategory errorCategory) { }
  }
  public abstract class PSHost {
    protected PSHost() { }
     
    public abstract System.Globalization.CultureInfo CurrentCulture { get; }
    public abstract System.Globalization.CultureInfo CurrentUICulture { get; }
    public abstract System.Guid InstanceId { get; }
    public abstract string Name { get; }
    public virtual System.Management.Automation.PSObject PrivateData { get { return default(System.Management.Automation.PSObject); } }
    public abstract System.Management.Automation.Host.PSHostUserInterface UI { get; }
    public abstract System.Version Version { get; }
     
    public abstract void EnterNestedPrompt();
    public abstract void ExitNestedPrompt();
    public abstract void NotifyBeginApplication();
    public abstract void NotifyEndApplication();
    public abstract void SetShouldExit(int exitCode);
  }
  public abstract class PSHostRawUserInterface {
    protected PSHostRawUserInterface() { }
     
    public abstract System.ConsoleColor BackgroundColor { get; set; }
    public abstract System.Management.Automation.Host.Size BufferSize { get; set; }
    public abstract System.Management.Automation.Host.Coordinates CursorPosition { get; set; }
    public abstract int CursorSize { get; set; }
    public abstract System.ConsoleColor ForegroundColor { get; set; }
    public abstract bool KeyAvailable { get; }
    public abstract System.Management.Automation.Host.Size MaxPhysicalWindowSize { get; }
    public abstract System.Management.Automation.Host.Size MaxWindowSize { get; }
    public abstract System.Management.Automation.Host.Coordinates WindowPosition { get; set; }
    public abstract System.Management.Automation.Host.Size WindowSize { get; set; }
    public abstract string WindowTitle { get; set; }
     
    public abstract void FlushInputBuffer();
    public abstract System.Management.Automation.Host.BufferCell[,] GetBufferContents(System.Management.Automation.Host.Rectangle rectangle);
    public virtual int LengthInBufferCells(char source) { return default(int); }
    public virtual int LengthInBufferCells(string source) { return default(int); }
    public virtual int LengthInBufferCells(string source, int offset) { return default(int); }
    public System.Management.Automation.Host.BufferCell[,] NewBufferCellArray(int width, int height, System.Management.Automation.Host.BufferCell contents) { return default(System.Management.Automation.Host.BufferCell[,]); }
    public System.Management.Automation.Host.BufferCell[,] NewBufferCellArray(System.Management.Automation.Host.Size size, System.Management.Automation.Host.BufferCell contents) { return default(System.Management.Automation.Host.BufferCell[,]); }
    public System.Management.Automation.Host.BufferCell[,] NewBufferCellArray(string[] contents, System.ConsoleColor foregroundColor, System.ConsoleColor backgroundColor) { return default(System.Management.Automation.Host.BufferCell[,]); }
    public System.Management.Automation.Host.KeyInfo ReadKey() { return default(System.Management.Automation.Host.KeyInfo); }
    public abstract System.Management.Automation.Host.KeyInfo ReadKey(System.Management.Automation.Host.ReadKeyOptions options);
    public abstract void ScrollBufferContents(System.Management.Automation.Host.Rectangle source, System.Management.Automation.Host.Coordinates destination, System.Management.Automation.Host.Rectangle clip, System.Management.Automation.Host.BufferCell fill);
    public abstract void SetBufferContents(System.Management.Automation.Host.Coordinates origin, System.Management.Automation.Host.BufferCell[,] contents);
    public abstract void SetBufferContents(System.Management.Automation.Host.Rectangle rectangle, System.Management.Automation.Host.BufferCell fill);
  }
  public abstract class PSHostUserInterface {
    protected PSHostUserInterface() { }
     
    public abstract System.Management.Automation.Host.PSHostRawUserInterface RawUI { get; }
     
    public abstract System.Collections.Generic.Dictionary<string, System.Management.Automation.PSObject> Prompt(string caption, string message, System.Collections.ObjectModel.Collection<System.Management.Automation.Host.FieldDescription> descriptions);
    public abstract int PromptForChoice(string caption, string message, System.Collections.ObjectModel.Collection<System.Management.Automation.Host.ChoiceDescription> choices, int defaultChoice);
    public abstract System.Management.Automation.PSCredential PromptForCredential(string caption, string message, string userName, string targetName);
    public abstract System.Management.Automation.PSCredential PromptForCredential(string caption, string message, string userName, string targetName, System.Management.Automation.PSCredentialTypes allowedCredentialTypes, System.Management.Automation.PSCredentialUIOptions options);
    public abstract string ReadLine();
    public abstract System.Security.SecureString ReadLineAsSecureString();
    public abstract void Write(System.ConsoleColor foregroundColor, System.ConsoleColor backgroundColor, string value);
    public abstract void Write(string value);
    public abstract void WriteDebugLine(string message);
    public abstract void WriteErrorLine(string value);
    public virtual void WriteLine() { }
    public virtual void WriteLine(System.ConsoleColor foregroundColor, System.ConsoleColor backgroundColor, string value) { }
    public abstract void WriteLine(string value);
    public abstract void WriteProgress(long sourceId, System.Management.Automation.ProgressRecord record);
    public abstract void WriteVerboseLine(string message);
    public abstract void WriteWarningLine(string message);
  }
  [System.FlagsAttribute]
  public enum ReadKeyOptions {
    AllowCtrlC = 1,
    IncludeKeyDown = 4,
    IncludeKeyUp = 8,
    NoEcho = 2,
  }
  [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
  public partial struct Rectangle {
    public Rectangle(int left, int top, int right, int bottom) { throw new System.NotImplementedException(); }
    public Rectangle(System.Management.Automation.Host.Coordinates upperLeft, System.Management.Automation.Host.Coordinates lowerRight) { throw new System.NotImplementedException(); }
     
    public int Bottom { get { return default(int); } set { } }
    public int Left { get { return default(int); } set { } }
    public int Right { get { return default(int); } set { } }
    public int Top { get { return default(int); } set { } }
     
    public override bool Equals(object obj) { return default(bool); }
    public override int GetHashCode() { return default(int); }
    public static bool operator ==(System.Management.Automation.Host.Rectangle first, System.Management.Automation.Host.Rectangle second) { return default(bool); }
    public static bool operator !=(System.Management.Automation.Host.Rectangle first, System.Management.Automation.Host.Rectangle second) { return default(bool); }
    public override string ToString() { return default(string); }
  }
  [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
  public partial struct Size {
    public Size(int width, int height) { throw new System.NotImplementedException(); }
     
    public int Height { get { return default(int); } set { } }
    public int Width { get { return default(int); } set { } }
     
    public override bool Equals(object obj) { return default(bool); }
    public override int GetHashCode() { return default(int); }
    public static bool operator ==(System.Management.Automation.Host.Size first, System.Management.Automation.Host.Size second) { return default(bool); }
    public static bool operator !=(System.Management.Automation.Host.Size first, System.Management.Automation.Host.Size second) { return default(bool); }
    public override string ToString() { return default(string); }
  }
}
namespace System.Management.Automation.Internal {
  public static class AlternateDataStreamUtilities {
  }
  public static class AutomationNull {
    public static System.Management.Automation.PSObject Value { get { return default(System.Management.Automation.PSObject); } }
     
  }
  [System.AttributeUsageAttribute((AttributeTargets)32767)]
  public abstract class CmdletMetadataAttribute : System.Attribute {
    internal CmdletMetadataAttribute() { }
  }
  public sealed class CommonParameters {
    internal CommonParameters() { }
    [System.Management.Automation.AliasAttribute(new string[]{ "db"})]
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Debug { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "ea"})]
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.ActionPreference ErrorAction { get { return default(System.Management.Automation.ActionPreference); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "ev"})]
    //Internal: [System.Management.Automation.Internal.CommonParameters.ValidateVariableName]
    [System.Management.Automation.ParameterAttribute]
    public string ErrorVariable { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "ob"})]
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateRangeAttribute(0, 2147483647)]
    public int OutBuffer { get { return default(int); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "ov"})]
    //Internal: [System.Management.Automation.Internal.CommonParameters.ValidateVariableName]
    [System.Management.Automation.ParameterAttribute]
    public string OutVariable { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "vb"})]
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Verbose { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "wa"})]
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.ActionPreference WarningAction { get { return default(System.Management.Automation.ActionPreference); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "wv"})]
    //Internal: [System.Management.Automation.Internal.CommonParameters.ValidateVariableName]
    [System.Management.Automation.ParameterAttribute]
    public string WarningVariable { get { return default(string); } set { } }
     
  }
#if WORKFLOW
  public partial interface IAstToWorkflowConverter {
    System.Management.Automation.WorkflowInfo CompileWorkflow(string name, string definition, System.Management.Automation.Runspaces.InitialSessionState initialSessionState);
    System.Collections.Generic.List<System.Management.Automation.WorkflowInfo> CompileWorkflows(System.Management.Automation.Language.ScriptBlockAst ast, System.Management.Automation.PSModuleInfo definingModule);
    System.Collections.Generic.List<System.Management.Automation.Language.ParseError> ValidateAst(System.Management.Automation.Language.FunctionDefinitionAst ast);
  }
#endif
  [System.Diagnostics.DebuggerDisplayAttribute("Command = {commandInfo}")]
  public abstract class InternalCommand {
    internal InternalCommand() { }
    public System.Management.Automation.CommandOrigin CommandOrigin { get { return default(System.Management.Automation.CommandOrigin); } }
     
  }
  [System.AttributeUsageAttribute((AttributeTargets)32767)]
  public abstract class ParsingBaseAttribute : System.Management.Automation.Internal.CmdletMetadataAttribute {
    internal ParsingBaseAttribute() { }
  }
  public sealed class ShouldProcessParameters {
    internal ShouldProcessParameters() { }
    [System.Management.Automation.AliasAttribute(new string[]{ "cf"})]
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Confirm { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "wi"})]
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter WhatIf { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
  }
#if TRANSACTIONS
  public sealed class TransactionParameters {
    internal TransactionParameters() { }
    [System.Management.Automation.AliasAttribute(new string[]{ "usetx"})]
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter UseTransaction { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
  }
#endif
}
namespace System.Management.Automation.Language {
  public class ArrayExpressionAst : System.Management.Automation.Language.ExpressionAst {
    public ArrayExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.StatementBlockAst statementBlock) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public override System.Type StaticType { get { return default(System.Type); } }
    public System.Management.Automation.Language.StatementBlockAst SubExpression { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.StatementBlockAst); } }
     
  }
  public class ArrayLiteralAst : System.Management.Automation.Language.ExpressionAst {
    public ArrayLiteralAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IList<System.Management.Automation.Language.ExpressionAst> elements) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst> Elements { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst>); } }
    public override System.Type StaticType { get { return default(System.Type); } }
     
  }
  public sealed class ArrayTypeName : System.Management.Automation.Language.ITypeName {
    public ArrayTypeName(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ITypeName elementType, int rank) { }
     
    public string AssemblyName { get { return default(string); } }
    public System.Management.Automation.Language.ITypeName ElementType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ITypeName); } }
    public System.Management.Automation.Language.IScriptExtent Extent { get { return default(System.Management.Automation.Language.IScriptExtent); } }
    public string FullName { get { return default(string); } }
    public bool IsArray { get { return default(bool); } }
    public bool IsGeneric { get { return default(bool); } }
    public string Name { get { return default(string); } }
    public int Rank { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(int); } }
     
    public System.Type GetReflectionAttributeType() { return default(System.Type); }
    public System.Type GetReflectionType() { return default(System.Type); }
    public override string ToString() { return default(string); }
  }
  public class AssignmentStatementAst : System.Management.Automation.Language.PipelineBaseAst {
    public AssignmentStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ExpressionAst left, System.Management.Automation.Language.TokenKind @operator, System.Management.Automation.Language.StatementAst right, System.Management.Automation.Language.IScriptExtent errorPosition) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Management.Automation.Language.IScriptExtent ErrorPosition { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.IScriptExtent); } }
    public System.Management.Automation.Language.ExpressionAst Left { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
    public System.Management.Automation.Language.TokenKind Operator { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.TokenKind); } }
    public System.Management.Automation.Language.StatementAst Right { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.StatementAst); } }
     
  }
  public abstract class Ast {
    protected Ast(System.Management.Automation.Language.IScriptExtent extent) { }
     
    public System.Management.Automation.Language.IScriptExtent Extent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.IScriptExtent); } }
    public System.Management.Automation.Language.Ast Parent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.Ast); } }
     
    public System.Management.Automation.Language.Ast Find(System.Func<System.Management.Automation.Language.Ast, bool> predicate, bool searchNestedScriptBlocks) { return default(System.Management.Automation.Language.Ast); }
    public System.Collections.Generic.IEnumerable<System.Management.Automation.Language.Ast> FindAll(System.Func<System.Management.Automation.Language.Ast, bool> predicate, bool searchNestedScriptBlocks) { return default(System.Collections.Generic.IEnumerable<System.Management.Automation.Language.Ast>); }
    public override string ToString() { return default(string); }
    public void Visit(System.Management.Automation.Language.AstVisitor astVisitor) { }
    public object Visit(System.Management.Automation.Language.ICustomAstVisitor astVisitor) { return default(object); }
  }
  public enum AstVisitAction {
    Continue = 0,
    SkipChildren = 1,
    StopVisit = 2,
  }
  public abstract class AstVisitor {
    protected AstVisitor() { }
     
    public virtual System.Management.Automation.Language.AstVisitAction VisitArrayExpression(System.Management.Automation.Language.ArrayExpressionAst arrayExpressionAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitArrayLiteral(System.Management.Automation.Language.ArrayLiteralAst arrayLiteralAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitAssignmentStatement(System.Management.Automation.Language.AssignmentStatementAst assignmentStatementAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitAttribute(System.Management.Automation.Language.AttributeAst attributeAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitAttributedExpression(System.Management.Automation.Language.AttributedExpressionAst attributedExpressionAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitBinaryExpression(System.Management.Automation.Language.BinaryExpressionAst binaryExpressionAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitBlockStatement(System.Management.Automation.Language.BlockStatementAst blockStatementAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitBreakStatement(System.Management.Automation.Language.BreakStatementAst breakStatementAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitCatchClause(System.Management.Automation.Language.CatchClauseAst catchClauseAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitCommand(System.Management.Automation.Language.CommandAst commandAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitCommandExpression(System.Management.Automation.Language.CommandExpressionAst commandExpressionAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitCommandParameter(System.Management.Automation.Language.CommandParameterAst commandParameterAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitConstantExpression(System.Management.Automation.Language.ConstantExpressionAst constantExpressionAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitContinueStatement(System.Management.Automation.Language.ContinueStatementAst continueStatementAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitConvertExpression(System.Management.Automation.Language.ConvertExpressionAst convertExpressionAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitDataStatement(System.Management.Automation.Language.DataStatementAst dataStatementAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitDoUntilStatement(System.Management.Automation.Language.DoUntilStatementAst doUntilStatementAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitDoWhileStatement(System.Management.Automation.Language.DoWhileStatementAst doWhileStatementAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitErrorExpression(System.Management.Automation.Language.ErrorExpressionAst errorExpressionAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitErrorStatement(System.Management.Automation.Language.ErrorStatementAst errorStatementAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitExitStatement(System.Management.Automation.Language.ExitStatementAst exitStatementAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitExpandableStringExpression(System.Management.Automation.Language.ExpandableStringExpressionAst expandableStringExpressionAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitFileRedirection(System.Management.Automation.Language.FileRedirectionAst redirectionAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitForEachStatement(System.Management.Automation.Language.ForEachStatementAst forEachStatementAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitForStatement(System.Management.Automation.Language.ForStatementAst forStatementAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitFunctionDefinition(System.Management.Automation.Language.FunctionDefinitionAst functionDefinitionAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitHashtable(System.Management.Automation.Language.HashtableAst hashtableAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitIfStatement(System.Management.Automation.Language.IfStatementAst ifStmtAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitIndexExpression(System.Management.Automation.Language.IndexExpressionAst indexExpressionAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitInvokeMemberExpression(System.Management.Automation.Language.InvokeMemberExpressionAst methodCallAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitMemberExpression(System.Management.Automation.Language.MemberExpressionAst memberExpressionAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitMergingRedirection(System.Management.Automation.Language.MergingRedirectionAst redirectionAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitNamedAttributeArgument(System.Management.Automation.Language.NamedAttributeArgumentAst namedAttributeArgumentAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitNamedBlock(System.Management.Automation.Language.NamedBlockAst namedBlockAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitParamBlock(System.Management.Automation.Language.ParamBlockAst paramBlockAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitParameter(System.Management.Automation.Language.ParameterAst parameterAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitParenExpression(System.Management.Automation.Language.ParenExpressionAst parenExpressionAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitPipeline(System.Management.Automation.Language.PipelineAst pipelineAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitReturnStatement(System.Management.Automation.Language.ReturnStatementAst returnStatementAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitScriptBlock(System.Management.Automation.Language.ScriptBlockAst scriptBlockAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitScriptBlockExpression(System.Management.Automation.Language.ScriptBlockExpressionAst scriptBlockExpressionAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitStatementBlock(System.Management.Automation.Language.StatementBlockAst statementBlockAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitStringConstantExpression(System.Management.Automation.Language.StringConstantExpressionAst stringConstantExpressionAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitSubExpression(System.Management.Automation.Language.SubExpressionAst subExpressionAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitSwitchStatement(System.Management.Automation.Language.SwitchStatementAst switchStatementAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitThrowStatement(System.Management.Automation.Language.ThrowStatementAst throwStatementAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitTrap(System.Management.Automation.Language.TrapStatementAst trapStatementAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitTryStatement(System.Management.Automation.Language.TryStatementAst tryStatementAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitTypeConstraint(System.Management.Automation.Language.TypeConstraintAst typeConstraintAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitTypeExpression(System.Management.Automation.Language.TypeExpressionAst typeExpressionAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitUnaryExpression(System.Management.Automation.Language.UnaryExpressionAst unaryExpressionAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitUsingExpression(System.Management.Automation.Language.UsingExpressionAst usingExpressionAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitVariableExpression(System.Management.Automation.Language.VariableExpressionAst variableExpressionAst) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitWhileStatement(System.Management.Automation.Language.WhileStatementAst whileStatementAst) { return default(System.Management.Automation.Language.AstVisitAction); }
  }
  public class AttributeAst : System.Management.Automation.Language.AttributeBaseAst {
    public AttributeAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ITypeName typeName, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.ExpressionAst> positionalArguments, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.NamedAttributeArgumentAst> namedArguments) : base (default(System.Management.Automation.Language.IScriptExtent), default(System.Management.Automation.Language.ITypeName)) { }
     
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.NamedAttributeArgumentAst> NamedArguments { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.NamedAttributeArgumentAst>); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst> PositionalArguments { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst>); } }
     
  }
  public abstract class AttributeBaseAst : System.Management.Automation.Language.Ast {
    protected AttributeBaseAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ITypeName typeName) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Management.Automation.Language.ITypeName TypeName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ITypeName); } }
     
  }
  public class AttributedExpressionAst : System.Management.Automation.Language.ExpressionAst {
    public AttributedExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.AttributeBaseAst attribute, System.Management.Automation.Language.ExpressionAst child) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Management.Automation.Language.AttributeBaseAst Attribute { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.AttributeBaseAst); } }
    public System.Management.Automation.Language.ExpressionAst Child { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
     
  }
  public class BinaryExpressionAst : System.Management.Automation.Language.ExpressionAst {
    public BinaryExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ExpressionAst left, System.Management.Automation.Language.TokenKind @operator, System.Management.Automation.Language.ExpressionAst right, System.Management.Automation.Language.IScriptExtent errorPosition) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Management.Automation.Language.IScriptExtent ErrorPosition { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.IScriptExtent); } }
    public System.Management.Automation.Language.ExpressionAst Left { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
    public System.Management.Automation.Language.TokenKind Operator { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.TokenKind); } }
    public System.Management.Automation.Language.ExpressionAst Right { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
    public override System.Type StaticType { get { return default(System.Type); } }
     
  }
  public class BlockStatementAst : System.Management.Automation.Language.StatementAst {
    public BlockStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.Token kind, System.Management.Automation.Language.StatementBlockAst body) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Management.Automation.Language.StatementBlockAst Body { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.StatementBlockAst); } }
    public System.Management.Automation.Language.Token Kind { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.Token); } }
     
  }
  public class BreakStatementAst : System.Management.Automation.Language.StatementAst {
    public BreakStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ExpressionAst label) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Management.Automation.Language.ExpressionAst Label { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
     
  }
  public class CatchClauseAst : System.Management.Automation.Language.Ast {
    public CatchClauseAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.TypeConstraintAst> catchTypes, System.Management.Automation.Language.StatementBlockAst body) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Management.Automation.Language.StatementBlockAst Body { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.StatementBlockAst); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.TypeConstraintAst> CatchTypes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.TypeConstraintAst>); } }
    public bool IsCatchAll { get { return default(bool); } }
     
  }
  public class CommandAst : System.Management.Automation.Language.CommandBaseAst {
    public CommandAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.CommandElementAst> commandElements, System.Management.Automation.Language.TokenKind invocationOperator, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.RedirectionAst> redirections) : base (default(System.Management.Automation.Language.IScriptExtent), default(System.Collections.Generic.IEnumerable<System.Management.Automation.Language.RedirectionAst>)) { }
     
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.CommandElementAst> CommandElements { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.CommandElementAst>); } }
    public System.Management.Automation.Language.TokenKind InvocationOperator { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.TokenKind); } }
     
    public string GetCommandName() { return default(string); }
  }
  public abstract class CommandBaseAst : System.Management.Automation.Language.StatementAst {
    protected CommandBaseAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.RedirectionAst> redirections) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.RedirectionAst> Redirections { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.RedirectionAst>); } }
     
  }
  public abstract class CommandElementAst : System.Management.Automation.Language.Ast {
    protected CommandElementAst(System.Management.Automation.Language.IScriptExtent extent) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
  }
  public class CommandExpressionAst : System.Management.Automation.Language.CommandBaseAst {
    public CommandExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ExpressionAst expression, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.RedirectionAst> redirections) : base (default(System.Management.Automation.Language.IScriptExtent), default(System.Collections.Generic.IEnumerable<System.Management.Automation.Language.RedirectionAst>)) { }
     
    public System.Management.Automation.Language.ExpressionAst Expression { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
     
  }
  public class CommandParameterAst : System.Management.Automation.Language.CommandElementAst {
    public CommandParameterAst(System.Management.Automation.Language.IScriptExtent extent, string parameterName, System.Management.Automation.Language.ExpressionAst argument, System.Management.Automation.Language.IScriptExtent errorPosition) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Management.Automation.Language.ExpressionAst Argument { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
    public System.Management.Automation.Language.IScriptExtent ErrorPosition { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.IScriptExtent); } }
    public string ParameterName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
     
  }
  public sealed class CommentHelpInfo {
    public CommentHelpInfo() { }
     
    public string Component { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
    public string Description { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<string> Examples { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<string>); } }
    public string ForwardHelpCategory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
    public string ForwardHelpTargetName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
    public string Functionality { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<string> Inputs { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<string>); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<string> Links { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<string>); } }
    public string MamlHelpFile { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
    public string Notes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<string> Outputs { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<string>); } }
    public System.Collections.Generic.IDictionary<string, string> Parameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.Generic.IDictionary<string, string>); } }
    public string RemoteHelpRunspace { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
    public string Role { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
    public string Synopsis { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
     
    public string GetCommentBlock() { return default(string); }
  }
  public class ConstantExpressionAst : System.Management.Automation.Language.ExpressionAst {
    public ConstantExpressionAst(System.Management.Automation.Language.IScriptExtent extent, object value) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public override System.Type StaticType { get { return default(System.Type); } }
    public object Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(object); } }
     
  }
  public class ContinueStatementAst : System.Management.Automation.Language.StatementAst {
    public ContinueStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ExpressionAst label) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Management.Automation.Language.ExpressionAst Label { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
     
  }
  public class ConvertExpressionAst : System.Management.Automation.Language.AttributedExpressionAst {
    public ConvertExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.TypeConstraintAst typeConstraint, System.Management.Automation.Language.ExpressionAst child) : base (default(System.Management.Automation.Language.IScriptExtent), default(System.Management.Automation.Language.AttributeBaseAst), default(System.Management.Automation.Language.ExpressionAst)) { }
     
    public override System.Type StaticType { get { return default(System.Type); } }
    public System.Management.Automation.Language.TypeConstraintAst Type { get { return default(System.Management.Automation.Language.TypeConstraintAst); } }
     
  }
  public class DataStatementAst : System.Management.Automation.Language.StatementAst {
    public DataStatementAst(System.Management.Automation.Language.IScriptExtent extent, string variableName, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.ExpressionAst> commandsAllowed, System.Management.Automation.Language.StatementBlockAst body) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Management.Automation.Language.StatementBlockAst Body { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.StatementBlockAst); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst> CommandsAllowed { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst>); } }
    public string Variable { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
     
  }
  public class DoUntilStatementAst : System.Management.Automation.Language.LoopStatementAst {
    public DoUntilStatementAst(System.Management.Automation.Language.IScriptExtent extent, string label, System.Management.Automation.Language.PipelineBaseAst condition, System.Management.Automation.Language.StatementBlockAst body) : base (default(System.Management.Automation.Language.IScriptExtent), default(string), default(System.Management.Automation.Language.PipelineBaseAst), default(System.Management.Automation.Language.StatementBlockAst)) { }
  }
  public class DoWhileStatementAst : System.Management.Automation.Language.LoopStatementAst {
    public DoWhileStatementAst(System.Management.Automation.Language.IScriptExtent extent, string label, System.Management.Automation.Language.PipelineBaseAst condition, System.Management.Automation.Language.StatementBlockAst body) : base (default(System.Management.Automation.Language.IScriptExtent), default(string), default(System.Management.Automation.Language.PipelineBaseAst), default(System.Management.Automation.Language.StatementBlockAst)) { }
  }
  public class ErrorExpressionAst : System.Management.Automation.Language.ExpressionAst {
    internal ErrorExpressionAst() : base (default(System.Management.Automation.Language.IScriptExtent)) { }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Ast> NestedAst { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Ast>); } }
     
  }
  public class ErrorStatementAst : System.Management.Automation.Language.PipelineBaseAst {
    internal ErrorStatementAst() : base (default(System.Management.Automation.Language.IScriptExtent)) { }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Ast> Bodies { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Ast>); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Ast> Conditions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Ast>); } }
    public System.Collections.Generic.Dictionary<string, System.Tuple<System.Management.Automation.Language.Token, System.Management.Automation.Language.Ast>> Flags { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.Generic.Dictionary<string, System.Tuple<System.Management.Automation.Language.Token, System.Management.Automation.Language.Ast>>); } }
    public System.Management.Automation.Language.Token Kind { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.Token); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Ast> NestedAst { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Ast>); } }
     
  }
  public class ExitStatementAst : System.Management.Automation.Language.StatementAst {
    public ExitStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.PipelineBaseAst pipeline) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Management.Automation.Language.PipelineBaseAst Pipeline { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.PipelineBaseAst); } }
     
  }
  public class ExpandableStringExpressionAst : System.Management.Automation.Language.ExpressionAst {
    public ExpandableStringExpressionAst(System.Management.Automation.Language.IScriptExtent extent, string value, System.Management.Automation.Language.StringConstantType type) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst> NestedExpressions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst>); } }
    public override System.Type StaticType { get { return default(System.Type); } }
    public System.Management.Automation.Language.StringConstantType StringConstantType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.StringConstantType); } }
    public string Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
     
  }
  public abstract class ExpressionAst : System.Management.Automation.Language.CommandElementAst {
    protected ExpressionAst(System.Management.Automation.Language.IScriptExtent extent) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public virtual System.Type StaticType { get { return default(System.Type); } }
     
  }
  public class FileRedirectionAst : System.Management.Automation.Language.RedirectionAst {
    public FileRedirectionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.RedirectionStream stream, System.Management.Automation.Language.ExpressionAst file, bool append) : base (default(System.Management.Automation.Language.IScriptExtent), default(System.Management.Automation.Language.RedirectionStream)) { }
     
    public bool Append { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } }
    public System.Management.Automation.Language.ExpressionAst Location { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
     
  }
  public class FileRedirectionToken : System.Management.Automation.Language.RedirectionToken {
    internal FileRedirectionToken() { }
    public bool Append { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } }
    public System.Management.Automation.Language.RedirectionStream FromStream { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.RedirectionStream); } }
     
  }
  [System.FlagsAttribute]
  public enum ForEachFlags {
    None = 0,
    Parallel = 1,
  }
  public class ForEachStatementAst : System.Management.Automation.Language.LoopStatementAst {
    public ForEachStatementAst(System.Management.Automation.Language.IScriptExtent extent, string label, System.Management.Automation.Language.ForEachFlags flags, System.Management.Automation.Language.VariableExpressionAst variable, System.Management.Automation.Language.PipelineBaseAst expression, System.Management.Automation.Language.StatementBlockAst body) : base (default(System.Management.Automation.Language.IScriptExtent), default(string), default(System.Management.Automation.Language.PipelineBaseAst), default(System.Management.Automation.Language.StatementBlockAst)) { }
     
    public System.Management.Automation.Language.ForEachFlags Flags { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ForEachFlags); } }
    public System.Management.Automation.Language.VariableExpressionAst Variable { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.VariableExpressionAst); } }
     
  }
  public class ForStatementAst : System.Management.Automation.Language.LoopStatementAst {
    public ForStatementAst(System.Management.Automation.Language.IScriptExtent extent, string label, System.Management.Automation.Language.PipelineBaseAst initializer, System.Management.Automation.Language.PipelineBaseAst condition, System.Management.Automation.Language.PipelineBaseAst iterator, System.Management.Automation.Language.StatementBlockAst body) : base (default(System.Management.Automation.Language.IScriptExtent), default(string), default(System.Management.Automation.Language.PipelineBaseAst), default(System.Management.Automation.Language.StatementBlockAst)) { }
     
    public System.Management.Automation.Language.PipelineBaseAst Initializer { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.PipelineBaseAst); } }
    public System.Management.Automation.Language.PipelineBaseAst Iterator { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.PipelineBaseAst); } }
     
  }
  public class FunctionDefinitionAst : System.Management.Automation.Language.StatementAst {
    public FunctionDefinitionAst(System.Management.Automation.Language.IScriptExtent extent, bool isFilter, bool isWorkflow, string name, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.ParameterAst> parameters, System.Management.Automation.Language.ScriptBlockAst body) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Management.Automation.Language.ScriptBlockAst Body { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ScriptBlockAst); } }
    public bool IsFilter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } }
    public bool IsWorkflow { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } }
    public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ParameterAst> Parameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ParameterAst>); } }
     
    public System.Management.Automation.Language.CommentHelpInfo GetHelpContent() { return default(System.Management.Automation.Language.CommentHelpInfo); }
  }
  public sealed class GenericTypeName : System.Management.Automation.Language.ITypeName {
    public GenericTypeName(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ITypeName genericTypeName, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.ITypeName> genericArguments) { }
     
    public string AssemblyName { get { return default(string); } }
    public System.Management.Automation.Language.IScriptExtent Extent { get { return default(System.Management.Automation.Language.IScriptExtent); } }
    public string FullName { get { return default(string); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ITypeName> GenericArguments { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ITypeName>); } }
    public bool IsArray { get { return default(bool); } }
    public bool IsGeneric { get { return default(bool); } }
    public string Name { get { return default(string); } }
    public System.Management.Automation.Language.ITypeName TypeName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ITypeName); } }
     
    public System.Type GetReflectionAttributeType() { return default(System.Type); }
    public System.Type GetReflectionType() { return default(System.Type); }
    public override string ToString() { return default(string); }
  }
  public class HashtableAst : System.Management.Automation.Language.ExpressionAst {
    public HashtableAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Tuple<System.Management.Automation.Language.ExpressionAst, System.Management.Automation.Language.StatementAst>> keyValuePairs) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Tuple<System.Management.Automation.Language.ExpressionAst, System.Management.Automation.Language.StatementAst>> KeyValuePairs { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Tuple<System.Management.Automation.Language.ExpressionAst, System.Management.Automation.Language.StatementAst>>); } }
    public override System.Type StaticType { get { return default(System.Type); } }
     
  }
  public partial interface ICustomAstVisitor {
    object VisitArrayExpression(System.Management.Automation.Language.ArrayExpressionAst arrayExpressionAst);
    object VisitArrayLiteral(System.Management.Automation.Language.ArrayLiteralAst arrayLiteralAst);
    object VisitAssignmentStatement(System.Management.Automation.Language.AssignmentStatementAst assignmentStatementAst);
    object VisitAttribute(System.Management.Automation.Language.AttributeAst attributeAst);
    object VisitAttributedExpression(System.Management.Automation.Language.AttributedExpressionAst attributedExpressionAst);
    object VisitBinaryExpression(System.Management.Automation.Language.BinaryExpressionAst binaryExpressionAst);
    object VisitBlockStatement(System.Management.Automation.Language.BlockStatementAst blockStatementAst);
    object VisitBreakStatement(System.Management.Automation.Language.BreakStatementAst breakStatementAst);
    object VisitCatchClause(System.Management.Automation.Language.CatchClauseAst catchClauseAst);
    object VisitCommand(System.Management.Automation.Language.CommandAst commandAst);
    object VisitCommandExpression(System.Management.Automation.Language.CommandExpressionAst commandExpressionAst);
    object VisitCommandParameter(System.Management.Automation.Language.CommandParameterAst commandParameterAst);
    object VisitConstantExpression(System.Management.Automation.Language.ConstantExpressionAst constantExpressionAst);
    object VisitContinueStatement(System.Management.Automation.Language.ContinueStatementAst continueStatementAst);
    object VisitConvertExpression(System.Management.Automation.Language.ConvertExpressionAst convertExpressionAst);
    object VisitDataStatement(System.Management.Automation.Language.DataStatementAst dataStatementAst);
    object VisitDoUntilStatement(System.Management.Automation.Language.DoUntilStatementAst doUntilStatementAst);
    object VisitDoWhileStatement(System.Management.Automation.Language.DoWhileStatementAst doWhileStatementAst);
    object VisitErrorExpression(System.Management.Automation.Language.ErrorExpressionAst errorExpressionAst);
    object VisitErrorStatement(System.Management.Automation.Language.ErrorStatementAst errorStatementAst);
    object VisitExitStatement(System.Management.Automation.Language.ExitStatementAst exitStatementAst);
    object VisitExpandableStringExpression(System.Management.Automation.Language.ExpandableStringExpressionAst expandableStringExpressionAst);
    object VisitFileRedirection(System.Management.Automation.Language.FileRedirectionAst fileRedirectionAst);
    object VisitForEachStatement(System.Management.Automation.Language.ForEachStatementAst forEachStatementAst);
    object VisitForStatement(System.Management.Automation.Language.ForStatementAst forStatementAst);
    object VisitFunctionDefinition(System.Management.Automation.Language.FunctionDefinitionAst functionDefinitionAst);
    object VisitHashtable(System.Management.Automation.Language.HashtableAst hashtableAst);
    object VisitIfStatement(System.Management.Automation.Language.IfStatementAst ifStmtAst);
    object VisitIndexExpression(System.Management.Automation.Language.IndexExpressionAst indexExpressionAst);
    object VisitInvokeMemberExpression(System.Management.Automation.Language.InvokeMemberExpressionAst invokeMemberExpressionAst);
    object VisitMemberExpression(System.Management.Automation.Language.MemberExpressionAst memberExpressionAst);
    object VisitMergingRedirection(System.Management.Automation.Language.MergingRedirectionAst mergingRedirectionAst);
    object VisitNamedAttributeArgument(System.Management.Automation.Language.NamedAttributeArgumentAst namedAttributeArgumentAst);
    object VisitNamedBlock(System.Management.Automation.Language.NamedBlockAst namedBlockAst);
    object VisitParamBlock(System.Management.Automation.Language.ParamBlockAst paramBlockAst);
    object VisitParameter(System.Management.Automation.Language.ParameterAst parameterAst);
    object VisitParenExpression(System.Management.Automation.Language.ParenExpressionAst parenExpressionAst);
    object VisitPipeline(System.Management.Automation.Language.PipelineAst pipelineAst);
    object VisitReturnStatement(System.Management.Automation.Language.ReturnStatementAst returnStatementAst);
    object VisitScriptBlock(System.Management.Automation.Language.ScriptBlockAst scriptBlockAst);
    object VisitScriptBlockExpression(System.Management.Automation.Language.ScriptBlockExpressionAst scriptBlockExpressionAst);
    object VisitStatementBlock(System.Management.Automation.Language.StatementBlockAst statementBlockAst);
    object VisitStringConstantExpression(System.Management.Automation.Language.StringConstantExpressionAst stringConstantExpressionAst);
    object VisitSubExpression(System.Management.Automation.Language.SubExpressionAst subExpressionAst);
    object VisitSwitchStatement(System.Management.Automation.Language.SwitchStatementAst switchStatementAst);
    object VisitThrowStatement(System.Management.Automation.Language.ThrowStatementAst throwStatementAst);
    object VisitTrap(System.Management.Automation.Language.TrapStatementAst trapStatementAst);
    object VisitTryStatement(System.Management.Automation.Language.TryStatementAst tryStatementAst);
    object VisitTypeConstraint(System.Management.Automation.Language.TypeConstraintAst typeConstraintAst);
    object VisitTypeExpression(System.Management.Automation.Language.TypeExpressionAst typeExpressionAst);
    object VisitUnaryExpression(System.Management.Automation.Language.UnaryExpressionAst unaryExpressionAst);
    object VisitUsingExpression(System.Management.Automation.Language.UsingExpressionAst usingExpressionAst);
    object VisitVariableExpression(System.Management.Automation.Language.VariableExpressionAst variableExpressionAst);
    object VisitWhileStatement(System.Management.Automation.Language.WhileStatementAst whileStatementAst);
  }
  public class IfStatementAst : System.Management.Automation.Language.StatementAst {
    public IfStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Tuple<System.Management.Automation.Language.PipelineBaseAst, System.Management.Automation.Language.StatementBlockAst>> clauses, System.Management.Automation.Language.StatementBlockAst elseClause) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Tuple<System.Management.Automation.Language.PipelineBaseAst, System.Management.Automation.Language.StatementBlockAst>> Clauses { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Tuple<System.Management.Automation.Language.PipelineBaseAst, System.Management.Automation.Language.StatementBlockAst>>); } }
    public System.Management.Automation.Language.StatementBlockAst ElseClause { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.StatementBlockAst); } }
     
  }
  public class IndexExpressionAst : System.Management.Automation.Language.ExpressionAst {
    public IndexExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ExpressionAst target, System.Management.Automation.Language.ExpressionAst index) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Management.Automation.Language.ExpressionAst Index { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
    public System.Management.Automation.Language.ExpressionAst Target { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
     
  }
  public class InputRedirectionToken : System.Management.Automation.Language.RedirectionToken {
    internal InputRedirectionToken() { }
  }
  public class InvokeMemberExpressionAst : System.Management.Automation.Language.MemberExpressionAst {
    public InvokeMemberExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ExpressionAst expression, System.Management.Automation.Language.CommandElementAst method, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.ExpressionAst> arguments, bool @static) : base (default(System.Management.Automation.Language.IScriptExtent), default(System.Management.Automation.Language.ExpressionAst), default(System.Management.Automation.Language.CommandElementAst), default(bool)) { }
     
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst> Arguments { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst>); } }
     
  }
  public partial interface IScriptExtent {
    int EndColumnNumber { get; }
    int EndLineNumber { get; }
    int EndOffset { get; }
    System.Management.Automation.Language.IScriptPosition EndScriptPosition { get; }
    string File { get; }
    int StartColumnNumber { get; }
    int StartLineNumber { get; }
    int StartOffset { get; }
    System.Management.Automation.Language.IScriptPosition StartScriptPosition { get; }
    string Text { get; }
     
  }
  public partial interface IScriptPosition {
    int ColumnNumber { get; }
    string File { get; }
    string Line { get; }
    int LineNumber { get; }
    int Offset { get; }
     
    string GetFullScript();
  }
  public partial interface ITypeName {
    string AssemblyName { get; }
    System.Management.Automation.Language.IScriptExtent Extent { get; }
    string FullName { get; }
    bool IsArray { get; }
    bool IsGeneric { get; }
    string Name { get; }
     
    System.Type GetReflectionAttributeType();
    System.Type GetReflectionType();
  }
  public abstract class LabeledStatementAst : System.Management.Automation.Language.StatementAst {
    protected LabeledStatementAst(System.Management.Automation.Language.IScriptExtent extent, string label, System.Management.Automation.Language.PipelineBaseAst condition) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Management.Automation.Language.PipelineBaseAst Condition { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.PipelineBaseAst); } }
    public string Label { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
     
  }
  public class LabelToken : System.Management.Automation.Language.Token {
    internal LabelToken() { }
    public string LabelText { get { return default(string); } }
     
  }
  public abstract class LoopStatementAst : System.Management.Automation.Language.LabeledStatementAst {
    protected LoopStatementAst(System.Management.Automation.Language.IScriptExtent extent, string label, System.Management.Automation.Language.PipelineBaseAst condition, System.Management.Automation.Language.StatementBlockAst body) : base (default(System.Management.Automation.Language.IScriptExtent), default(string), default(System.Management.Automation.Language.PipelineBaseAst)) { }
     
    public System.Management.Automation.Language.StatementBlockAst Body { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.StatementBlockAst); } }
     
  }
  public class MemberExpressionAst : System.Management.Automation.Language.ExpressionAst {
    public MemberExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ExpressionAst expression, System.Management.Automation.Language.CommandElementAst member, bool @static) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Management.Automation.Language.ExpressionAst Expression { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
    public System.Management.Automation.Language.CommandElementAst Member { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.CommandElementAst); } }
    public bool Static { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } }
     
  }
  public class MergingRedirectionAst : System.Management.Automation.Language.RedirectionAst {
    public MergingRedirectionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.RedirectionStream from, System.Management.Automation.Language.RedirectionStream to) : base (default(System.Management.Automation.Language.IScriptExtent), default(System.Management.Automation.Language.RedirectionStream)) { }
     
    public System.Management.Automation.Language.RedirectionStream ToStream { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.RedirectionStream); } }
     
  }
  public class MergingRedirectionToken : System.Management.Automation.Language.RedirectionToken {
    internal MergingRedirectionToken() { }
    public System.Management.Automation.Language.RedirectionStream FromStream { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.RedirectionStream); } }
    public System.Management.Automation.Language.RedirectionStream ToStream { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.RedirectionStream); } }
     
  }
  public class NamedAttributeArgumentAst : System.Management.Automation.Language.Ast {
    public NamedAttributeArgumentAst(System.Management.Automation.Language.IScriptExtent extent, string argumentName, System.Management.Automation.Language.ExpressionAst argument, bool expressionOmitted) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Management.Automation.Language.ExpressionAst Argument { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
    public string ArgumentName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
    public bool ExpressionOmitted { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } }
     
  }
  public class NamedBlockAst : System.Management.Automation.Language.Ast {
    public NamedBlockAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.TokenKind blockName, System.Management.Automation.Language.StatementBlockAst statementBlock, bool unnamed) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Management.Automation.Language.TokenKind BlockKind { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.TokenKind); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.StatementAst> Statements { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.StatementAst>); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.TrapStatementAst> Traps { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.TrapStatementAst>); } }
    public bool Unnamed { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } }
     
  }
  public class NullString {
    internal NullString() { }
    public static System.Management.Automation.Language.NullString Value { get { return default(System.Management.Automation.Language.NullString); } }
     
    public override string ToString() { return default(string); }
  }
  public class NumberToken : System.Management.Automation.Language.Token {
    internal NumberToken() { }
    public object Value { get { return default(object); } }
     
  }
  public class ParamBlockAst : System.Management.Automation.Language.Ast {
    public ParamBlockAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.AttributeAst> attributes, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.ParameterAst> parameters) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.AttributeAst> Attributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.AttributeAst>); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ParameterAst> Parameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ParameterAst>); } }
     
  }
  public class ParameterAst : System.Management.Automation.Language.Ast {
    public ParameterAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.VariableExpressionAst name, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.AttributeBaseAst> attributes, System.Management.Automation.Language.ExpressionAst defaultValue) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.AttributeBaseAst> Attributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.AttributeBaseAst>); } }
    public System.Management.Automation.Language.ExpressionAst DefaultValue { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
    public System.Management.Automation.Language.VariableExpressionAst Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.VariableExpressionAst); } }
    public System.Type StaticType { get { return default(System.Type); } }
     
  }
  public class ParameterToken : System.Management.Automation.Language.Token {
    internal ParameterToken() { }
    public string ParameterName { get { return default(string); } }
    public bool UsedColon { get { return default(bool); } }
     
  }
  public class ParenExpressionAst : System.Management.Automation.Language.ExpressionAst {
    public ParenExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.PipelineBaseAst pipeline) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Management.Automation.Language.PipelineBaseAst Pipeline { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.PipelineBaseAst); } }
     
  }
  public class ParseError {
    public ParseError(System.Management.Automation.Language.IScriptExtent extent, string errorId, string message) { }
     
    public string ErrorId { get { return default(string); } }
    public System.Management.Automation.Language.IScriptExtent Extent { get { return default(System.Management.Automation.Language.IScriptExtent); } }
    public bool IncompleteInput { get { return default(bool); } }
    public string Message { get { return default(string); } }
     
    public override string ToString() { return default(string); }
  }
  public sealed class Parser {
    internal Parser() { }
    public static System.Management.Automation.Language.ScriptBlockAst ParseFile(string fileName, out System.Management.Automation.Language.Token[] tokens, out System.Management.Automation.Language.ParseError[] errors) { tokens = default(System.Management.Automation.Language.Token[]); errors = default(System.Management.Automation.Language.ParseError[]); return default(System.Management.Automation.Language.ScriptBlockAst); }
    public static System.Management.Automation.Language.ScriptBlockAst ParseInput(string input, out System.Management.Automation.Language.Token[] tokens, out System.Management.Automation.Language.ParseError[] errors) { tokens = default(System.Management.Automation.Language.Token[]); errors = default(System.Management.Automation.Language.ParseError[]); return default(System.Management.Automation.Language.ScriptBlockAst); }
  }
  public class PipelineAst : System.Management.Automation.Language.PipelineBaseAst {
    public PipelineAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.CommandBaseAst> pipelineElements) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
    public PipelineAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.CommandBaseAst commandAst) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.CommandBaseAst> PipelineElements { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.CommandBaseAst>); } }
     
    public override System.Management.Automation.Language.ExpressionAst GetPureExpression() { return default(System.Management.Automation.Language.ExpressionAst); }
  }
  public abstract class PipelineBaseAst : System.Management.Automation.Language.StatementAst {
    protected PipelineBaseAst(System.Management.Automation.Language.IScriptExtent extent) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public virtual System.Management.Automation.Language.ExpressionAst GetPureExpression() { return default(System.Management.Automation.Language.ExpressionAst); }
  }
  public abstract class RedirectionAst : System.Management.Automation.Language.Ast {
    protected RedirectionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.RedirectionStream from) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Management.Automation.Language.RedirectionStream FromStream { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.RedirectionStream); } }
     
  }
  public enum RedirectionStream {
    All = 0,
    Debug = 5,
    Error = 2,
    Host = 6,
    Output = 1,
    Verbose = 4,
    Warning = 3,
  }
  public abstract class RedirectionToken : System.Management.Automation.Language.Token {
    internal RedirectionToken() { }
  }
  public sealed class ReflectionTypeName : System.Management.Automation.Language.ITypeName {
    public ReflectionTypeName(System.Type type) { }
     
    public string AssemblyName { get { return default(string); } }
    public System.Management.Automation.Language.IScriptExtent Extent { get { return default(System.Management.Automation.Language.IScriptExtent); } }
    public string FullName { get { return default(string); } }
    public bool IsArray { get { return default(bool); } }
    public bool IsGeneric { get { return default(bool); } }
    public string Name { get { return default(string); } }
     
    public System.Type GetReflectionAttributeType() { return default(System.Type); }
    public System.Type GetReflectionType() { return default(System.Type); }
    public override string ToString() { return default(string); }
  }
  public class ReturnStatementAst : System.Management.Automation.Language.StatementAst {
    public ReturnStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.PipelineBaseAst pipeline) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Management.Automation.Language.PipelineBaseAst Pipeline { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.PipelineBaseAst); } }
     
  }
  public class ScriptBlockAst : System.Management.Automation.Language.Ast {
    public ScriptBlockAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ParamBlockAst paramBlock, System.Management.Automation.Language.NamedBlockAst beginBlock, System.Management.Automation.Language.NamedBlockAst processBlock, System.Management.Automation.Language.NamedBlockAst endBlock, System.Management.Automation.Language.NamedBlockAst dynamicParamBlock) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
    public ScriptBlockAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ParamBlockAst paramBlock, System.Management.Automation.Language.StatementBlockAst statements, bool isFilter) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Management.Automation.Language.NamedBlockAst BeginBlock { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.NamedBlockAst); } }
    public System.Management.Automation.Language.NamedBlockAst DynamicParamBlock { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.NamedBlockAst); } }
    public System.Management.Automation.Language.NamedBlockAst EndBlock { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.NamedBlockAst); } }
    public System.Management.Automation.Language.ParamBlockAst ParamBlock { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ParamBlockAst); } }
    public System.Management.Automation.Language.NamedBlockAst ProcessBlock { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.NamedBlockAst); } }
    public System.Management.Automation.Language.ScriptRequirements ScriptRequirements { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ScriptRequirements); } }
     
    public System.Management.Automation.Language.CommentHelpInfo GetHelpContent() { return default(System.Management.Automation.Language.CommentHelpInfo); }
    public System.Management.Automation.ScriptBlock GetScriptBlock() { return default(System.Management.Automation.ScriptBlock); }
  }
  public class ScriptBlockExpressionAst : System.Management.Automation.Language.ExpressionAst {
    public ScriptBlockExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ScriptBlockAst scriptBlock) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Management.Automation.Language.ScriptBlockAst ScriptBlock { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ScriptBlockAst); } }
    public override System.Type StaticType { get { return default(System.Type); } }
     
  }
  public sealed class ScriptExtent : System.Management.Automation.Language.IScriptExtent {
    public ScriptExtent(System.Management.Automation.Language.ScriptPosition startPosition, System.Management.Automation.Language.ScriptPosition endPosition) { }
     
    public int EndColumnNumber { get { return default(int); } }
    public int EndLineNumber { get { return default(int); } }
    public int EndOffset { get { return default(int); } }
    public System.Management.Automation.Language.IScriptPosition EndScriptPosition { get { return default(System.Management.Automation.Language.IScriptPosition); } }
    public string File { get { return default(string); } }
    public int StartColumnNumber { get { return default(int); } }
    public int StartLineNumber { get { return default(int); } }
    public int StartOffset { get { return default(int); } }
    public System.Management.Automation.Language.IScriptPosition StartScriptPosition { get { return default(System.Management.Automation.Language.IScriptPosition); } }
    public string Text { get { return default(string); } }
     
  }
  public sealed class ScriptPosition : System.Management.Automation.Language.IScriptPosition {
    public ScriptPosition(string scriptName, int scriptLineNumber, int offsetInLine, string line) { }
     
    public int ColumnNumber { get { return default(int); } }
    public string File { get { return default(string); } }
    public string Line { get { return default(string); } }
    public int LineNumber { get { return default(int); } }
    public int Offset { get { return default(int); } }
     
    public string GetFullScript() { return default(string); }
  }
  public class ScriptRequirements {
    public ScriptRequirements() { }
     
    public string RequiredApplicationId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<string> RequiredAssemblies { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<string>); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<Microsoft.PowerShell.Commands.ModuleSpecification> RequiredModules { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<Microsoft.PowerShell.Commands.ModuleSpecification>); } }
    public System.Version RequiredPSVersion { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Version); } }
  }
  public abstract class StatementAst : System.Management.Automation.Language.Ast {
    protected StatementAst(System.Management.Automation.Language.IScriptExtent extent) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
  }
  public class StatementBlockAst : System.Management.Automation.Language.Ast {
    public StatementBlockAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.StatementAst> statements, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.TrapStatementAst> traps) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.StatementAst> Statements { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.StatementAst>); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.TrapStatementAst> Traps { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.TrapStatementAst>); } }
     
  }
  public class StringConstantExpressionAst : System.Management.Automation.Language.ConstantExpressionAst {
    public StringConstantExpressionAst(System.Management.Automation.Language.IScriptExtent extent, string value, System.Management.Automation.Language.StringConstantType stringConstantType) : base (default(System.Management.Automation.Language.IScriptExtent), default(object)) { }
     
    public override System.Type StaticType { get { return default(System.Type); } }
    public System.Management.Automation.Language.StringConstantType StringConstantType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.StringConstantType); } }
    public new string Value { get { return default(string); } }
     
  }
  public enum StringConstantType {
    BareWord = 4,
    DoubleQuoted = 2,
    DoubleQuotedHereString = 3,
    SingleQuoted = 0,
    SingleQuotedHereString = 1,
  }
  public class StringExpandableToken : System.Management.Automation.Language.StringToken {
    internal StringExpandableToken() { }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Token> NestedTokens { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Token>); } }
     
  }
  public class StringLiteralToken : System.Management.Automation.Language.StringToken {
    internal StringLiteralToken() { }
  }
  public abstract class StringToken : System.Management.Automation.Language.Token {
    internal StringToken() { }
    public string Value { get { return default(string); } }
     
  }
  public class SubExpressionAst : System.Management.Automation.Language.ExpressionAst {
    public SubExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.StatementBlockAst statementBlock) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Management.Automation.Language.StatementBlockAst SubExpression { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.StatementBlockAst); } }
     
  }
  [System.FlagsAttribute]
  public enum SwitchFlags {
    CaseSensitive = 16,
    Exact = 8,
    File = 1,
    None = 0,
    Parallel = 32,
    Regex = 2,
    Wildcard = 4,
  }
  public class SwitchStatementAst : System.Management.Automation.Language.LabeledStatementAst {
    public SwitchStatementAst(System.Management.Automation.Language.IScriptExtent extent, string label, System.Management.Automation.Language.PipelineBaseAst condition, System.Management.Automation.Language.SwitchFlags flags, System.Collections.Generic.IEnumerable<System.Tuple<System.Management.Automation.Language.ExpressionAst, System.Management.Automation.Language.StatementBlockAst>> clauses, System.Management.Automation.Language.StatementBlockAst @default) : base (default(System.Management.Automation.Language.IScriptExtent), default(string), default(System.Management.Automation.Language.PipelineBaseAst)) { }
     
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Tuple<System.Management.Automation.Language.ExpressionAst, System.Management.Automation.Language.StatementBlockAst>> Clauses { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Tuple<System.Management.Automation.Language.ExpressionAst, System.Management.Automation.Language.StatementBlockAst>>); } }
    public System.Management.Automation.Language.StatementBlockAst Default { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.StatementBlockAst); } }
    public System.Management.Automation.Language.SwitchFlags Flags { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.SwitchFlags); } }
     
  }
  public class ThrowStatementAst : System.Management.Automation.Language.StatementAst {
    public ThrowStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.PipelineBaseAst pipeline) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public bool IsRethrow { get { return default(bool); } }
    public System.Management.Automation.Language.PipelineBaseAst Pipeline { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.PipelineBaseAst); } }
     
  }
  public class Token {
    internal Token() { }
    public System.Management.Automation.Language.IScriptExtent Extent { get { return default(System.Management.Automation.Language.IScriptExtent); } }
    public bool HasError { get { return default(bool); } }
    public System.Management.Automation.Language.TokenKind Kind { get { return default(System.Management.Automation.Language.TokenKind); } }
    public string Text { get { return default(string); } }
    public System.Management.Automation.Language.TokenFlags TokenFlags { get { return default(System.Management.Automation.Language.TokenFlags); } }
     
    public override string ToString() { return default(string); }
  }
  [System.FlagsAttribute]
  public enum TokenFlags {
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
    TokenInError = 65536,
    TypeName = 2097152,
    UnaryOperator = 512,
  }
  public enum TokenKind {
    Ampersand = 28,
    And = 53,
    AndAnd = 26,
    As = 94,
    AtCurly = 23,
    AtParen = 22,
    Band = 56,
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
    ColonColon = 34,
    Comma = 30,
    Comment = 10,
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
    Dynamicparam = 127,
    Else = 128,
    ElseIf = 129,
    End = 130,
    EndOfInput = 11,
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
    Multiply = 37,
    MultiplyEquals = 45,
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
    Process = 141,
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
    StringExpandable = 13,
    StringLiteral = 12,
    Switch = 143,
    Throw = 144,
    Trap = 145,
    Try = 146,
    Unknown = 0,
    Until = 147,
    Using = 148,
    Var = 149,
    Variable = 1,
    While = 150,
    Workflow = 151,
    Xor = 55,
  }
  public static class TokenTraits {
    public static System.Management.Automation.Language.TokenFlags GetTraits(this System.Management.Automation.Language.TokenKind kind) { return default(System.Management.Automation.Language.TokenFlags); }
    public static bool HasTrait(this System.Management.Automation.Language.TokenKind kind, System.Management.Automation.Language.TokenFlags flag) { return default(bool); }
    public static string Text(this System.Management.Automation.Language.TokenKind kind) { return default(string); }
  }
  public class TrapStatementAst : System.Management.Automation.Language.StatementAst {
    public TrapStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.TypeConstraintAst trapType, System.Management.Automation.Language.StatementBlockAst body) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Management.Automation.Language.StatementBlockAst Body { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.StatementBlockAst); } }
    public System.Management.Automation.Language.TypeConstraintAst TrapType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.TypeConstraintAst); } }
     
  }
  public class TryStatementAst : System.Management.Automation.Language.StatementAst {
    public TryStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.StatementBlockAst body, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.CatchClauseAst> catchClauses, System.Management.Automation.Language.StatementBlockAst @finally) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Management.Automation.Language.StatementBlockAst Body { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.StatementBlockAst); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.CatchClauseAst> CatchClauses { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.CatchClauseAst>); } }
    public System.Management.Automation.Language.StatementBlockAst Finally { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.StatementBlockAst); } }
     
  }
  public class TypeConstraintAst : System.Management.Automation.Language.AttributeBaseAst {
    public TypeConstraintAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ITypeName typeName) : base (default(System.Management.Automation.Language.IScriptExtent), default(System.Management.Automation.Language.ITypeName)) { }
    public TypeConstraintAst(System.Management.Automation.Language.IScriptExtent extent, System.Type type) : base (default(System.Management.Automation.Language.IScriptExtent), default(System.Management.Automation.Language.ITypeName)) { }
  }
  public class TypeExpressionAst : System.Management.Automation.Language.ExpressionAst {
    public TypeExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ITypeName typeName) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public override System.Type StaticType { get { return default(System.Type); } }
    public System.Management.Automation.Language.ITypeName TypeName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ITypeName); } }
     
  }
  public sealed class TypeName : System.Management.Automation.Language.ITypeName {
    public TypeName(System.Management.Automation.Language.IScriptExtent extent, string name) { }
    public TypeName(System.Management.Automation.Language.IScriptExtent extent, string name, string assembly) { }
     
    public string AssemblyName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
    public System.Management.Automation.Language.IScriptExtent Extent { get { return default(System.Management.Automation.Language.IScriptExtent); } }
    public string FullName { get { return default(string); } }
    public bool IsArray { get { return default(bool); } }
    public bool IsGeneric { get { return default(bool); } }
    public string Name { get { return default(string); } }
     
    public System.Type GetReflectionAttributeType() { return default(System.Type); }
    public System.Type GetReflectionType() { return default(System.Type); }
    public override string ToString() { return default(string); }
  }
  public class UnaryExpressionAst : System.Management.Automation.Language.ExpressionAst {
    public UnaryExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.TokenKind tokenKind, System.Management.Automation.Language.ExpressionAst child) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Management.Automation.Language.ExpressionAst Child { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
    public override System.Type StaticType { get { return default(System.Type); } }
    public System.Management.Automation.Language.TokenKind TokenKind { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.TokenKind); } }
     
  }
  public class UsingExpressionAst : System.Management.Automation.Language.ExpressionAst {
    public UsingExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ExpressionAst expressionAst) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public System.Management.Automation.Language.ExpressionAst SubExpression { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
     
    public static System.Management.Automation.Language.VariableExpressionAst ExtractUsingVariable(System.Management.Automation.Language.UsingExpressionAst usingExpressionAst) { return default(System.Management.Automation.Language.VariableExpressionAst); }
  }
  public class VariableExpressionAst : System.Management.Automation.Language.ExpressionAst {
    public VariableExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.VariablePath variablePath, bool splatted) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
    public VariableExpressionAst(System.Management.Automation.Language.IScriptExtent extent, string variableName, bool splatted) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    public bool Splatted { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } }
    public System.Management.Automation.VariablePath VariablePath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.VariablePath); } }
     
    public bool IsConstantVariable() { return default(bool); }
  }
  public class VariableToken : System.Management.Automation.Language.Token {
    internal VariableToken() { }
    public string Name { get { return default(string); } }
    public System.Management.Automation.VariablePath VariablePath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.VariablePath); } }
     
  }
  public class WhileStatementAst : System.Management.Automation.Language.LoopStatementAst {
    public WhileStatementAst(System.Management.Automation.Language.IScriptExtent extent, string label, System.Management.Automation.Language.PipelineBaseAst condition, System.Management.Automation.Language.StatementBlockAst body) : base (default(System.Management.Automation.Language.IScriptExtent), default(string), default(System.Management.Automation.Language.PipelineBaseAst), default(System.Management.Automation.Language.StatementBlockAst)) { }
  }
}
#if PERF_COUNTERS
namespace System.Management.Automation.PerformanceData {
  [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
  public partial struct CounterInfo {
    public CounterInfo(int id, System.Diagnostics.PerformanceData.CounterType type) { throw new System.NotImplementedException(); }
    public CounterInfo(int id, System.Diagnostics.PerformanceData.CounterType type, string name) { throw new System.NotImplementedException(); }
     
    public int Id { get { return default(int); } }
    public string Name { get { return default(string); } }
    public System.Diagnostics.PerformanceData.CounterType Type { get { return default(System.Diagnostics.PerformanceData.CounterType); } }
     
  }
  public abstract class CounterSetInstanceBase : System.IDisposable {
    protected System.Collections.Concurrent.ConcurrentDictionary<int, System.Diagnostics.PerformanceData.CounterType> _counterIdToTypeMapping;
    protected System.Collections.Concurrent.ConcurrentDictionary<string, int> _counterNameToIdMapping;
    protected System.Management.Automation.PerformanceData.CounterSetRegistrarBase _counterSetRegistrarBase;
     
    protected CounterSetInstanceBase(System.Management.Automation.PerformanceData.CounterSetRegistrarBase counterSetRegistrarInst) { }
     
    public abstract void Dispose();
    public abstract bool GetCounterValue(int counterId, bool isNumerator, out long counterValue);
    public abstract bool GetCounterValue(string counterName, bool isNumerator, out long counterValue);
    protected bool RetrieveTargetCounterIdIfValid(int counterId, bool isNumerator, out int targetCounterId) { targetCounterId = default(int); return default(bool); }
    public abstract bool SetCounterValue(int counterId, long counterValue, bool isNumerator);
    public abstract bool SetCounterValue(string counterName, long counterValue, bool isNumerator);
    public abstract bool UpdateCounterByValue(int counterId, long stepAmount, bool isNumerator);
    public abstract bool UpdateCounterByValue(string counterName, long stepAmount, bool isNumerator);
  }
  public abstract class CounterSetRegistrarBase {
    protected System.Management.Automation.PerformanceData.CounterSetInstanceBase _counterSetInstanceBase;
     
    protected CounterSetRegistrarBase(System.Guid providerId, System.Guid counterSetId, System.Diagnostics.PerformanceData.CounterSetInstanceType counterSetInstType, System.Management.Automation.PerformanceData.CounterInfo[] counterInfoArray, string counterSetName=null) { }
    protected CounterSetRegistrarBase(System.Management.Automation.PerformanceData.CounterSetRegistrarBase srcCounterSetRegistrarBase) { }
     
    public System.Management.Automation.PerformanceData.CounterInfo[] CounterInfoArray { get { return default(System.Management.Automation.PerformanceData.CounterInfo[]); } }
    public System.Guid CounterSetId { get { return default(System.Guid); } }
    public System.Management.Automation.PerformanceData.CounterSetInstanceBase CounterSetInstance { get { return default(System.Management.Automation.PerformanceData.CounterSetInstanceBase); } }
    public System.Diagnostics.PerformanceData.CounterSetInstanceType CounterSetInstType { get { return default(System.Diagnostics.PerformanceData.CounterSetInstanceType); } }
    public string CounterSetName { get { return default(string); } }
    public System.Guid ProviderId { get { return default(System.Guid); } }
     
    protected abstract System.Management.Automation.PerformanceData.CounterSetInstanceBase CreateCounterSetInstance();
    public abstract void DisposeCounterSetInstance();
  }
  public class PSCounterSetInstance : System.Management.Automation.PerformanceData.CounterSetInstanceBase {
    public PSCounterSetInstance(System.Management.Automation.PerformanceData.CounterSetRegistrarBase counterSetRegBaseObj) : base (default(System.Management.Automation.PerformanceData.CounterSetRegistrarBase)) { }
     
    public override void Dispose() { }
    protected virtual void Dispose(bool disposing) { }
    ~PSCounterSetInstance() { }
    public override bool GetCounterValue(int counterId, bool isNumerator, out long counterValue) { counterValue = default(long); return default(bool); }
    public override bool GetCounterValue(string counterName, bool isNumerator, out long counterValue) { counterValue = default(long); return default(bool); }
    public override bool SetCounterValue(int counterId, long counterValue, bool isNumerator) { return default(bool); }
    public override bool SetCounterValue(string counterName, long counterValue, bool isNumerator) { return default(bool); }
    public override bool UpdateCounterByValue(int counterId, long stepAmount, bool isNumerator) { return default(bool); }
    public override bool UpdateCounterByValue(string counterName, long stepAmount, bool isNumerator) { return default(bool); }
  }
  public class PSCounterSetRegistrar : System.Management.Automation.PerformanceData.CounterSetRegistrarBase {
    public PSCounterSetRegistrar(System.Guid providerId, System.Guid counterSetId, System.Diagnostics.PerformanceData.CounterSetInstanceType counterSetInstType, System.Management.Automation.PerformanceData.CounterInfo[] counterInfoArray, string counterSetName=null) : base (default(System.Guid), default(System.Guid), default(System.Diagnostics.PerformanceData.CounterSetInstanceType), default(System.Management.Automation.PerformanceData.CounterInfo[]), default(string)) { }
    public PSCounterSetRegistrar(System.Management.Automation.PerformanceData.PSCounterSetRegistrar srcPSCounterSetRegistrar) : base (default(System.Guid), default(System.Guid), default(System.Diagnostics.PerformanceData.CounterSetInstanceType), default(System.Management.Automation.PerformanceData.CounterInfo[]), default(string)) { }
     
    protected override System.Management.Automation.PerformanceData.CounterSetInstanceBase CreateCounterSetInstance() { return default(System.Management.Automation.PerformanceData.CounterSetInstanceBase); }
    public override void DisposeCounterSetInstance() { }
  }
  public class PSPerfCountersMgr {
    internal PSPerfCountersMgr() { }
    public static System.Management.Automation.PerformanceData.PSPerfCountersMgr Instance { get { return default(System.Management.Automation.PerformanceData.PSPerfCountersMgr); } }
     
    public bool AddCounterSetInstance(System.Management.Automation.PerformanceData.CounterSetRegistrarBase counterSetRegistrarInstance) { return default(bool); }
    ~PSPerfCountersMgr() { }
    public string GetCounterSetInstanceName() { return default(string); }
    public bool IsCounterSetRegistered(System.Guid counterSetId, out System.Management.Automation.PerformanceData.CounterSetInstanceBase counterSetInst) { counterSetInst = default(System.Management.Automation.PerformanceData.CounterSetInstanceBase); return default(bool); }
    public bool IsCounterSetRegistered(string counterSetName, out System.Guid counterSetId) { counterSetId = default(System.Guid); return default(bool); }
    public bool SetCounterValue(System.Guid counterSetId, int counterId, long counterValue=(long)1, bool isNumerator=true) { return default(bool); }
    public bool SetCounterValue(System.Guid counterSetId, string counterName, long counterValue=(long)1, bool isNumerator=true) { return default(bool); }
    public bool SetCounterValue(string counterSetName, int counterId, long counterValue=(long)1, bool isNumerator=true) { return default(bool); }
    public bool SetCounterValue(string counterSetName, string counterName, long counterValue=(long)1, bool isNumerator=true) { return default(bool); }
    public bool UpdateCounterByValue(System.Guid counterSetId, int counterId, long stepAmount=(long)1, bool isNumerator=true) { return default(bool); }
    public bool UpdateCounterByValue(System.Guid counterSetId, string counterName, long stepAmount=(long)1, bool isNumerator=true) { return default(bool); }
    public bool UpdateCounterByValue(string counterSetName, int counterId, long stepAmount=(long)1, bool isNumerator=true) { return default(bool); }
    public bool UpdateCounterByValue(string counterSetName, string counterName, long stepAmount=(long)1, bool isNumerator=true) { return default(bool); }
  }
}
#endif
namespace System.Management.Automation.Provider {
  public abstract class CmdletProvider : System.Management.Automation.IResourceSupplier {
    protected CmdletProvider() { }
     
    public System.Management.Automation.PSCredential Credential { get { return default(System.Management.Automation.PSCredential); } }
#if TRANSACTIONS
    public System.Management.Automation.PSTransactionContext CurrentPSTransaction { get { return default(System.Management.Automation.PSTransactionContext); } }
#endif
    protected object DynamicParameters { get { return default(object); } }
    public System.Collections.ObjectModel.Collection<string> Exclude { get { return default(System.Collections.ObjectModel.Collection<string>); } }
    public string Filter { get { return default(string); } }
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } }
    public System.Management.Automation.Host.PSHost Host { get { return default(System.Management.Automation.Host.PSHost); } }
    public System.Collections.ObjectModel.Collection<string> Include { get { return default(System.Collections.ObjectModel.Collection<string>); } }
    public System.Management.Automation.CommandInvocationIntrinsics InvokeCommand { get { return default(System.Management.Automation.CommandInvocationIntrinsics); } }
    public System.Management.Automation.ProviderIntrinsics InvokeProvider { get { return default(System.Management.Automation.ProviderIntrinsics); } }
    protected internal System.Management.Automation.ProviderInfo ProviderInfo { get { return default(System.Management.Automation.ProviderInfo); } }
    protected System.Management.Automation.PSDriveInfo PSDriveInfo { get { return default(System.Management.Automation.PSDriveInfo); } }
    public System.Management.Automation.SessionState SessionState { get { return default(System.Management.Automation.SessionState); } }
    public bool Stopping { get { return default(bool); } }
     
    public virtual string GetResourceString(string baseName, string resourceId) { return default(string); }
    public bool ShouldContinue(string query, string caption) { return default(bool); }
    public bool ShouldContinue(string query, string caption, ref bool yesToAll, ref bool noToAll) { return default(bool); }
    public bool ShouldProcess(string target) { return default(bool); }
    public bool ShouldProcess(string target, string action) { return default(bool); }
    public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption) { return default(bool); }
    public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption, out System.Management.Automation.ShouldProcessReason shouldProcessReason) { shouldProcessReason = default(System.Management.Automation.ShouldProcessReason); return default(bool); }
    protected virtual System.Management.Automation.ProviderInfo Start(System.Management.Automation.ProviderInfo providerInfo) { return default(System.Management.Automation.ProviderInfo); }
    protected virtual object StartDynamicParameters() { return default(object); }
    protected virtual void Stop() { }
    protected internal virtual void StopProcessing() { }
    public void ThrowTerminatingError(System.Management.Automation.ErrorRecord errorRecord) { }
    public bool TransactionAvailable() { return default(bool); }
    public void WriteDebug(string text) { }
    public void WriteError(System.Management.Automation.ErrorRecord errorRecord) { }
    public void WriteItemObject(object item, string path, bool isContainer) { }
    public void WriteProgress(System.Management.Automation.ProgressRecord progressRecord) { }
    public void WritePropertyObject(object propertyValue, string path) { }
    public void WriteSecurityDescriptorObject(System.Security.AccessControl.ObjectSecurity securityDescriptor, string path) { }
    public void WriteVerbose(string text) { }
    public void WriteWarning(string text) { }
  }
  [System.AttributeUsageAttribute((AttributeTargets)4, AllowMultiple=false, Inherited=false)]
  public sealed class CmdletProviderAttribute : System.Attribute {
    public CmdletProviderAttribute(string providerName, System.Management.Automation.Provider.ProviderCapabilities providerCapabilities) { }
     
    public System.Management.Automation.Provider.ProviderCapabilities ProviderCapabilities { get { return default(System.Management.Automation.Provider.ProviderCapabilities); } }
    public string ProviderName { get { return default(string); } }
     
  }
  public abstract class ContainerCmdletProvider : System.Management.Automation.Provider.ItemCmdletProvider {
    protected ContainerCmdletProvider() { }
     
    protected virtual bool ConvertPath(string path, string filter, ref string updatedPath, ref string updatedFilter) { return default(bool); }
    protected virtual void CopyItem(string path, string copyPath, bool recurse) { }
    protected virtual object CopyItemDynamicParameters(string path, string destination, bool recurse) { return default(object); }
    protected virtual void GetChildItems(string path, bool recurse) { }
    protected virtual object GetChildItemsDynamicParameters(string path, bool recurse) { return default(object); }
    protected virtual void GetChildNames(string path, System.Management.Automation.ReturnContainers returnContainers) { }
    protected virtual object GetChildNamesDynamicParameters(string path) { return default(object); }
    protected virtual bool HasChildItems(string path) { return default(bool); }
    protected virtual void NewItem(string path, string itemTypeName, object newItemValue) { }
    protected virtual object NewItemDynamicParameters(string path, string itemTypeName, object newItemValue) { return default(object); }
    protected virtual void RemoveItem(string path, bool recurse) { }
    protected virtual object RemoveItemDynamicParameters(string path, bool recurse) { return default(object); }
    protected virtual void RenameItem(string path, string newName) { }
    protected virtual object RenameItemDynamicParameters(string path, string newName) { return default(object); }
  }
  public abstract class DriveCmdletProvider : System.Management.Automation.Provider.CmdletProvider {
    protected DriveCmdletProvider() { }
     
    protected virtual System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo> InitializeDefaultDrives() { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo>); }
    protected virtual System.Management.Automation.PSDriveInfo NewDrive(System.Management.Automation.PSDriveInfo drive) { return default(System.Management.Automation.PSDriveInfo); }
    protected virtual object NewDriveDynamicParameters() { return default(object); }
    protected virtual System.Management.Automation.PSDriveInfo RemoveDrive(System.Management.Automation.PSDriveInfo drive) { return default(System.Management.Automation.PSDriveInfo); }
  }
  public partial interface ICmdletProviderSupportsHelp {
    string GetHelpMaml(string helpItemName, string path);
  }
  public partial interface IContentCmdletProvider {
    void ClearContent(string path);
    object ClearContentDynamicParameters(string path);
    System.Management.Automation.Provider.IContentReader GetContentReader(string path);
    object GetContentReaderDynamicParameters(string path);
    System.Management.Automation.Provider.IContentWriter GetContentWriter(string path);
    object GetContentWriterDynamicParameters(string path);
  }
  public partial interface IContentReader : System.IDisposable {
    void Close();
    System.Collections.IList Read(long readCount);
    void Seek(long offset, System.IO.SeekOrigin origin);
  }
  public partial interface IContentWriter : System.IDisposable {
    void Close();
    void Seek(long offset, System.IO.SeekOrigin origin);
    System.Collections.IList Write(System.Collections.IList content);
  }
  public partial interface IDynamicPropertyCmdletProvider : System.Management.Automation.Provider.IPropertyCmdletProvider {
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
  public partial interface IPropertyCmdletProvider {
    void ClearProperty(string path, System.Collections.ObjectModel.Collection<string> propertyToClear);
    object ClearPropertyDynamicParameters(string path, System.Collections.ObjectModel.Collection<string> propertyToClear);
    void GetProperty(string path, System.Collections.ObjectModel.Collection<string> providerSpecificPickList);
    object GetPropertyDynamicParameters(string path, System.Collections.ObjectModel.Collection<string> providerSpecificPickList);
    void SetProperty(string path, System.Management.Automation.PSObject propertyValue);
    object SetPropertyDynamicParameters(string path, System.Management.Automation.PSObject propertyValue);
  }
  public partial interface ISecurityDescriptorCmdletProvider {
    void GetSecurityDescriptor(string path, System.Security.AccessControl.AccessControlSections includeSections);
    System.Security.AccessControl.ObjectSecurity NewSecurityDescriptorFromPath(string path, System.Security.AccessControl.AccessControlSections includeSections);
    System.Security.AccessControl.ObjectSecurity NewSecurityDescriptorOfType(string type, System.Security.AccessControl.AccessControlSections includeSections);
    void SetSecurityDescriptor(string path, System.Security.AccessControl.ObjectSecurity securityDescriptor);
  }
  public abstract class ItemCmdletProvider : System.Management.Automation.Provider.DriveCmdletProvider {
    protected ItemCmdletProvider() { }
     
    protected virtual void ClearItem(string path) { }
    protected virtual object ClearItemDynamicParameters(string path) { return default(object); }
    protected virtual string[] ExpandPath(string path) { return default(string[]); }
    protected virtual void GetItem(string path) { }
    protected virtual object GetItemDynamicParameters(string path) { return default(object); }
    protected virtual void InvokeDefaultAction(string path) { }
    protected virtual object InvokeDefaultActionDynamicParameters(string path) { return default(object); }
    protected abstract bool IsValidPath(string path);
    protected virtual bool ItemExists(string path) { return default(bool); }
    protected virtual object ItemExistsDynamicParameters(string path) { return default(object); }
    protected virtual void SetItem(string path, object value) { }
    protected virtual object SetItemDynamicParameters(string path, object value) { return default(object); }
  }
  public abstract class NavigationCmdletProvider : System.Management.Automation.Provider.ContainerCmdletProvider {
    protected NavigationCmdletProvider() { }
     
    protected virtual string GetChildName(string path) { return default(string); }
    protected virtual string GetParentPath(string path, string root) { return default(string); }
    protected virtual bool IsItemContainer(string path) { return default(bool); }
    protected virtual string MakePath(string parent, string child) { return default(string); }
    protected virtual void MoveItem(string path, string destination) { }
    protected virtual object MoveItemDynamicParameters(string path, string destination) { return default(object); }
    protected virtual string NormalizeRelativePath(string path, string basePath) { return default(string); }
  }
  [System.FlagsAttribute]
  public enum ProviderCapabilities {
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
namespace System.Management.Automation.Remoting {
  [System.Runtime.Serialization.DataContractAttribute]
  public class OriginInfo {
    public OriginInfo(string computerName, System.Guid runspaceID) { }
    public OriginInfo(string computerName, System.Guid runspaceID, System.Guid instanceID) { }
     
    public System.Guid InstanceID { get { return default(System.Guid); } set { } }
    public string PSComputerName { get { return default(string); } }
    public System.Guid RunspaceID { get { return default(System.Guid); } }
     
    public override string ToString() { return default(string); }
  }
  public enum ProxyAccessType {
    AutoDetect = 4,
    IEConfig = 1,
    None = 0,
    NoProxyServer = 8,
    WinHttpConfig = 2,
  }
  public sealed class PSCertificateDetails {
    public PSCertificateDetails(string subject, string issuerName, string issuerThumbprint) { }
     
    public string IssuerName { get { return default(string); } }
    public string IssuerThumbprint { get { return default(string); } }
    public string Subject { get { return default(string); } }
     
  }
  public sealed class PSIdentity : System.Security.Principal.IIdentity {
    public PSIdentity(string authType, bool isAuthenticated, string userName, System.Management.Automation.Remoting.PSCertificateDetails cert) { }
     
    public string AuthenticationType { get { return default(string); } }
    public System.Management.Automation.Remoting.PSCertificateDetails CertificateDetails { get { return default(System.Management.Automation.Remoting.PSCertificateDetails); } }
    public bool IsAuthenticated { get { return default(bool); } }
    public string Name { get { return default(string); } }
     
  }
  public sealed class PSPrincipal : System.Security.Principal.IPrincipal {
    public PSPrincipal(System.Management.Automation.Remoting.PSIdentity identity, System.Security.Principal.WindowsIdentity windowsIdentity) { }
     
    public System.Management.Automation.Remoting.PSIdentity Identity { get { return default(System.Management.Automation.Remoting.PSIdentity); } }
    System.Security.Principal.IIdentity System.Security.Principal.IPrincipal.Identity { get { return default(System.Security.Principal.IIdentity); } }
    public System.Security.Principal.WindowsIdentity WindowsIdentity { get { return default(System.Security.Principal.WindowsIdentity); } }
     
    public bool IsInRole(string role) { return default(bool); }
  }
  public class PSRemotingDataStructureException : System.Management.Automation.RuntimeException {
    public PSRemotingDataStructureException() { }
    public PSRemotingDataStructureException(string message) { }
    public PSRemotingDataStructureException(string message, System.Exception innerException) { }
  }
  public class PSRemotingTransportException : System.Management.Automation.RuntimeException {
    public PSRemotingTransportException() { }
    public PSRemotingTransportException(string message) { }
    public PSRemotingTransportException(string message, System.Exception innerException) { }
     
    public int ErrorCode { get { return default(int); } set { } }
    public string TransportMessage { get { return default(string); } set { } }
     
    protected void SetDefaultErrorRecord() { }
  }
  public class PSRemotingTransportRedirectException : System.Management.Automation.Remoting.PSRemotingTransportException {
    public PSRemotingTransportRedirectException() { }
    public PSRemotingTransportRedirectException(string message) { }
    public PSRemotingTransportRedirectException(string message, System.Exception innerException) { }
     
    public string RedirectLocation { get { return default(string); } }
  }
  public sealed class PSSenderInfo {
    public PSSenderInfo(System.Management.Automation.Remoting.PSPrincipal userPrincipal, string httpUrl) { }
     
    public System.Management.Automation.PSPrimitiveDictionary ApplicationArguments { get { return default(System.Management.Automation.PSPrimitiveDictionary); } }
#if TIMEZONE
    public System.TimeZone ClientTimeZone { get { return default(System.TimeZone); } }
#endif
    public string ConnectionString { get { return default(string); } }
    public System.Management.Automation.Remoting.PSPrincipal UserInfo { get { return default(System.Management.Automation.Remoting.PSPrincipal); } }
  }
  public abstract class PSSessionConfiguration : System.IDisposable {
    protected PSSessionConfiguration() { }
     
    public void Dispose() { }
    protected virtual void Dispose(bool isDisposing) { }
    public virtual System.Management.Automation.PSPrimitiveDictionary GetApplicationPrivateData(System.Management.Automation.Remoting.PSSenderInfo senderInfo) { return default(System.Management.Automation.PSPrimitiveDictionary); }
    public abstract System.Management.Automation.Runspaces.InitialSessionState GetInitialSessionState(System.Management.Automation.Remoting.PSSenderInfo senderInfo);
    public virtual System.Management.Automation.Runspaces.InitialSessionState GetInitialSessionState(System.Management.Automation.Remoting.PSSessionConfigurationData sessionConfigurationData, System.Management.Automation.Remoting.PSSenderInfo senderInfo, string configProviderId) { return default(System.Management.Automation.Runspaces.InitialSessionState); }
    public virtual System.Nullable<int> GetMaximumReceivedDataSizePerCommand(System.Management.Automation.Remoting.PSSenderInfo senderInfo) { return default(System.Nullable<int>); }
    public virtual System.Nullable<int> GetMaximumReceivedObjectSize(System.Management.Automation.Remoting.PSSenderInfo senderInfo) { return default(System.Nullable<int>); }
  }
  public sealed class PSSessionConfigurationData {
    internal PSSessionConfigurationData() { }
    public static bool IsServerManager;
     
    public System.Collections.Generic.List<string> ModulesToImport { get { return default(System.Collections.Generic.List<string>); } }
    public string PrivateData { get { return default(string); } }
     
  }
  public sealed class PSSessionOption {
    public PSSessionOption() { }
     
    public System.Management.Automation.PSPrimitiveDictionary ApplicationArguments { get { return default(System.Management.Automation.PSPrimitiveDictionary); } set { } }
    public System.TimeSpan CancelTimeout { get { return default(System.TimeSpan); } set { } }
    public System.Globalization.CultureInfo Culture { get { return default(System.Globalization.CultureInfo); } set { } }
    public System.TimeSpan IdleTimeout { get { return default(System.TimeSpan); } set { } }
    public bool IncludePortInSPN { get { return default(bool); } set { } }
    public int MaximumConnectionRedirectionCount { get { return default(int); } set { } }
    public System.Nullable<int> MaximumReceivedDataSizePerCommand { get { return default(System.Nullable<int>); } set { } }
    public System.Nullable<int> MaximumReceivedObjectSize { get { return default(System.Nullable<int>); } set { } }
    public bool NoCompression { get { return default(bool); } set { } }
    public bool NoEncryption { get { return default(bool); } set { } }
    public bool NoMachineProfile { get { return default(bool); } set { } }
    public System.TimeSpan OpenTimeout { get { return default(System.TimeSpan); } set { } }
    public System.TimeSpan OperationTimeout { get { return default(System.TimeSpan); } set { } }
    public System.Management.Automation.Runspaces.OutputBufferingMode OutputBufferingMode { get { return default(System.Management.Automation.Runspaces.OutputBufferingMode); } set { } }
    public System.Management.Automation.Remoting.ProxyAccessType ProxyAccessType { get { return default(System.Management.Automation.Remoting.ProxyAccessType); } set { } }
    public System.Management.Automation.Runspaces.AuthenticationMechanism ProxyAuthentication { get { return default(System.Management.Automation.Runspaces.AuthenticationMechanism); } set { } }
    public System.Management.Automation.PSCredential ProxyCredential { get { return default(System.Management.Automation.PSCredential); } set { } }
    public bool SkipCACheck { get { return default(bool); } set { } }
    public bool SkipCNCheck { get { return default(bool); } set { } }
    public bool SkipRevocationCheck { get { return default(bool); } set { } }
    public System.Globalization.CultureInfo UICulture { get { return default(System.Globalization.CultureInfo); } set { } }
    public bool UseUTF16 { get { return default(bool); } set { } }
     
  }
  public enum SessionType {
    Default = 2,
    Empty = 0,
    RestrictedRemoteServer = 1,
  }
}
#if WSMAN
namespace System.Management.Automation.Remoting.WSMan {
  public static class WSManServerChannelEvents {
    // Events
    public static event System.EventHandler ShuttingDown { add { } remove { } }
     
  }
}
#endif
namespace System.Management.Automation.Runspaces {
  public sealed class AliasPropertyData : System.Management.Automation.Runspaces.TypeMemberData {
    public AliasPropertyData(string name, string referencedMemberName) { }
    public AliasPropertyData(string name, string referencedMemberName, System.Type type) { }
     
    public bool IsHidden { get { return default(bool); } set { } }
    public System.Type MemberType { get { return default(System.Type); } set { } }
    public string ReferencedMemberName { get { return default(string); } set { } }
     
  }
#if V1_PIPELINE_API
  public sealed class AssemblyConfigurationEntry : System.Management.Automation.Runspaces.RunspaceConfigurationEntry {
    public AssemblyConfigurationEntry(string name, string fileName) : base (default(string)) { }
     
    public string FileName { get { return default(string); } }
     
  }
#endif
  public enum AuthenticationMechanism {
    Basic = 1,
    Credssp = 4,
    Default = 0,
    Digest = 5,
    Kerberos = 6,
    Negotiate = 2,
    NegotiateWithImplicitCredential = 3,
  }
#if V1_PIPELINE_API
  public sealed class CmdletConfigurationEntry : System.Management.Automation.Runspaces.RunspaceConfigurationEntry {
    public CmdletConfigurationEntry(string name, System.Type implementingType, string helpFileName) : base (default(string)) { }
     
    public string HelpFileName { get { return default(string); } }
    public System.Type ImplementingType { get { return default(System.Type); } }
     
  }
#endif
  public sealed class CodeMethodData : System.Management.Automation.Runspaces.TypeMemberData {
    public CodeMethodData(string name, System.Reflection.MethodInfo methodToCall) { }
     
    public System.Reflection.MethodInfo CodeReference { get { return default(System.Reflection.MethodInfo); } set { } }
     
  }
  public sealed class CodePropertyData : System.Management.Automation.Runspaces.TypeMemberData {
    public CodePropertyData(string name, System.Reflection.MethodInfo getMethod) { }
    public CodePropertyData(string name, System.Reflection.MethodInfo getMethod, System.Reflection.MethodInfo setMethod) { }
     
    public System.Reflection.MethodInfo GetCodeReference { get { return default(System.Reflection.MethodInfo); } set { } }
    public bool IsHidden { get { return default(bool); } set { } }
    public System.Reflection.MethodInfo SetCodeReference { get { return default(System.Reflection.MethodInfo); } set { } }
     
  }
  public sealed class Command {
    public Command(string command) { }
    public Command(string command, bool isScript) { }
    public Command(string command, bool isScript, bool useLocalScope) { }
     
    public string CommandText { get { return default(string); } }
    public bool IsScript { get { return default(bool); } }
    public System.Management.Automation.Runspaces.PipelineResultTypes MergeUnclaimedPreviousCommandResults { get { return default(System.Management.Automation.Runspaces.PipelineResultTypes); } set { } }
    public System.Management.Automation.Runspaces.CommandParameterCollection Parameters { get { return default(System.Management.Automation.Runspaces.CommandParameterCollection); } }
    public bool UseLocalScope { get { return default(bool); } }
     
    public void MergeMyResults(System.Management.Automation.Runspaces.PipelineResultTypes myResult, System.Management.Automation.Runspaces.PipelineResultTypes toResult) { }
    public override string ToString() { return default(string); }
  }
  public sealed class CommandCollection : System.Collections.ObjectModel.Collection<System.Management.Automation.Runspaces.Command> {
    internal CommandCollection() { }
    public void Add(string command) { }
    public void AddScript(string scriptContents) { }
    public void AddScript(string scriptContents, bool useLocalScope) { }
  }
  public sealed class CommandParameter {
    public CommandParameter(string name) { }
    public CommandParameter(string name, object value) { }
     
    public string Name { get { return default(string); } }
    public object Value { get { return default(object); } }
     
  }
  public sealed class CommandParameterCollection : System.Collections.ObjectModel.Collection<System.Management.Automation.Runspaces.CommandParameter> {
    public CommandParameterCollection() { }
     
    public void Add(string name) { }
    public void Add(string name, object value) { }
  }
  public abstract class ConstrainedSessionStateEntry : System.Management.Automation.Runspaces.InitialSessionStateEntry {
    protected ConstrainedSessionStateEntry(string name, System.Management.Automation.SessionStateEntryVisibility visibility) : base (default(string)) { }
     
    public System.Management.Automation.SessionStateEntryVisibility Visibility { get { return default(System.Management.Automation.SessionStateEntryVisibility); } set { } }
     
  }
#if V1_PIPELINE_API
  public sealed class FormatConfigurationEntry : System.Management.Automation.Runspaces.RunspaceConfigurationEntry {
    public FormatConfigurationEntry(System.Management.Automation.ExtendedTypeDefinition typeDefinition) : base (default(string)) { }
    public FormatConfigurationEntry(string fileName) : base (default(string)) { }
    public FormatConfigurationEntry(string name, string fileName) : base (default(string)) { }
     
    public string FileName { get { return default(string); } }
    public System.Management.Automation.ExtendedTypeDefinition FormatData { get { return default(System.Management.Automation.ExtendedTypeDefinition); } }
     
  }
#endif
  public sealed class FormatTable {
    public FormatTable(System.Collections.Generic.IEnumerable<string> formatFiles) { }
     
    public void AppendFormatData(System.Collections.Generic.IEnumerable<System.Management.Automation.ExtendedTypeDefinition> formatData) { }
    public static System.Management.Automation.Runspaces.FormatTable LoadDefaultFormatFiles() { return default(System.Management.Automation.Runspaces.FormatTable); }
    public void PrependFormatData(System.Collections.Generic.IEnumerable<System.Management.Automation.ExtendedTypeDefinition> formatData) { }
  }
  public class FormatTableLoadException : System.Management.Automation.RuntimeException {
    public FormatTableLoadException() { }
    public FormatTableLoadException(string message) { }
    public FormatTableLoadException(string message, System.Exception innerException) { }
     
    public System.Collections.ObjectModel.Collection<string> Errors { get { return default(System.Collections.ObjectModel.Collection<string>); } }
     
    protected void SetDefaultErrorRecord() { }
  }
  public class InitialSessionState {
    protected InitialSessionState() { }
     
#if COM_APARTMENT_STATE
    public System.Threading.ApartmentState ApartmentState { get { return default(System.Threading.ApartmentState); } set { } }
#endif
    public virtual System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<System.Management.Automation.Runspaces.SessionStateAssemblyEntry> Assemblies { get { return default(System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<System.Management.Automation.Runspaces.SessionStateAssemblyEntry>); } }
    public virtual System.Management.Automation.AuthorizationManager AuthorizationManager { get { return default(System.Management.Automation.AuthorizationManager); } set { } }
    public virtual System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<System.Management.Automation.Runspaces.SessionStateCommandEntry> Commands { get { return default(System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<System.Management.Automation.Runspaces.SessionStateCommandEntry>); } }
    public bool DisableFormatUpdates { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public virtual System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<System.Management.Automation.Runspaces.SessionStateFormatEntry> Formats { get { return default(System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<System.Management.Automation.Runspaces.SessionStateFormatEntry>); } }
    public System.Management.Automation.PSLanguageMode LanguageMode { get { return default(System.Management.Automation.PSLanguageMode); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<Microsoft.PowerShell.Commands.ModuleSpecification> Modules { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<Microsoft.PowerShell.Commands.ModuleSpecification>); } }
    public virtual System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<System.Management.Automation.Runspaces.SessionStateProviderEntry> Providers { get { return default(System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<System.Management.Automation.Runspaces.SessionStateProviderEntry>); } }
    public System.Management.Automation.Runspaces.PSThreadOptions ThreadOptions { get { return default(System.Management.Automation.Runspaces.PSThreadOptions); } set { } }
    public bool ThrowOnRunspaceOpenError { get { return default(bool); } set { } }
    public virtual System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<System.Management.Automation.Runspaces.SessionStateTypeEntry> Types { get { return default(System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<System.Management.Automation.Runspaces.SessionStateTypeEntry>); } }
    public bool UseFullLanguageModeInDebugger { get { return default(bool); } set { } }
    public virtual System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<System.Management.Automation.Runspaces.SessionStateVariableEntry> Variables { get { return default(System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<System.Management.Automation.Runspaces.SessionStateVariableEntry>); } }
     
    public System.Management.Automation.Runspaces.InitialSessionState Clone() { return default(System.Management.Automation.Runspaces.InitialSessionState); }
    public static System.Management.Automation.Runspaces.InitialSessionState Create() { return default(System.Management.Automation.Runspaces.InitialSessionState); }
    public static System.Management.Automation.Runspaces.InitialSessionState Create(string snapInName) { return default(System.Management.Automation.Runspaces.InitialSessionState); }
    public static System.Management.Automation.Runspaces.InitialSessionState Create(string[] snapInNameCollection, out System.Management.Automation.Runspaces.PSConsoleLoadException warning) { warning = default(System.Management.Automation.Runspaces.PSConsoleLoadException); return default(System.Management.Automation.Runspaces.InitialSessionState); }
    public static System.Management.Automation.Runspaces.InitialSessionState CreateDefault() { return default(System.Management.Automation.Runspaces.InitialSessionState); }
    public static System.Management.Automation.Runspaces.InitialSessionState CreateDefault2() { return default(System.Management.Automation.Runspaces.InitialSessionState); }
    public static System.Management.Automation.Runspaces.InitialSessionState CreateFrom(string snapInPath, out System.Management.Automation.Runspaces.PSConsoleLoadException warnings) { warnings = default(System.Management.Automation.Runspaces.PSConsoleLoadException); return default(System.Management.Automation.Runspaces.InitialSessionState); }
    public static System.Management.Automation.Runspaces.InitialSessionState CreateFrom(string[] snapInPathCollection, out System.Management.Automation.Runspaces.PSConsoleLoadException warnings) { warnings = default(System.Management.Automation.Runspaces.PSConsoleLoadException); return default(System.Management.Automation.Runspaces.InitialSessionState); }
    public static System.Management.Automation.Runspaces.InitialSessionState CreateRestricted(System.Management.Automation.SessionCapabilities sessionCapabilities) { return default(System.Management.Automation.Runspaces.InitialSessionState); }
    public void ImportPSModule(string[] name) { }
    public void ImportPSModulesFromPath(string path) { }
  }
  public abstract class InitialSessionStateEntry {
    protected InitialSessionStateEntry(string name) { }
     
    public System.Management.Automation.PSModuleInfo Module { get { return default(System.Management.Automation.PSModuleInfo); } }
    public string Name { get { return default(string); } }
     
    public abstract System.Management.Automation.Runspaces.InitialSessionStateEntry Clone();
  }
  public sealed class InitialSessionStateEntryCollection<T> : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable where T : System.Management.Automation.Runspaces.InitialSessionStateEntry {
    public InitialSessionStateEntryCollection() { }
    public InitialSessionStateEntryCollection(System.Collections.Generic.IEnumerable<T> items) { }
     
    public int Count { get { return default(int); } }
    public T this[int index] { get { return default(T); } }
    public System.Collections.ObjectModel.Collection<T> this[string name] { get { return default(System.Collections.ObjectModel.Collection<T>); } }
     
    public void Add(T item) { }
    public void Add(System.Collections.Generic.IEnumerable<T> items) { }
    public void Clear() { }
    public System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<T> Clone() { return default(System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<T>); }
    public void Remove(string name, object type) { }
    public void RemoveItem(int index) { }
    public void RemoveItem(int index, int count) { }
    public void Reset() { }
    System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { return default(System.Collections.Generic.IEnumerator<T>); }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
  }
  public class InvalidPipelineStateException : System.Exception {
    public InvalidPipelineStateException() { }
    public InvalidPipelineStateException(string message) { }
    public InvalidPipelineStateException(string message, System.Exception innerException) { }
     
    public System.Management.Automation.Runspaces.PipelineState CurrentState { get { return default(System.Management.Automation.Runspaces.PipelineState); } }
    public System.Management.Automation.Runspaces.PipelineState ExpectedState { get { return default(System.Management.Automation.Runspaces.PipelineState); } }
  }
  public class InvalidRunspacePoolStateException : System.Exception {
    public InvalidRunspacePoolStateException() { }
    public InvalidRunspacePoolStateException(string message) { }
    public InvalidRunspacePoolStateException(string message, System.Exception innerException) { }
     
    public System.Management.Automation.Runspaces.RunspacePoolState CurrentState { get { return default(System.Management.Automation.Runspaces.RunspacePoolState); } }
    public System.Management.Automation.Runspaces.RunspacePoolState ExpectedState { get { return default(System.Management.Automation.Runspaces.RunspacePoolState); } }
     
  }
  public class InvalidRunspaceStateException : System.Exception {
    public InvalidRunspaceStateException() { }
    public InvalidRunspaceStateException(string message) { }
    public InvalidRunspaceStateException(string message, System.Exception innerException) { }
     
    public System.Management.Automation.Runspaces.RunspaceState CurrentState { get { return default(System.Management.Automation.Runspaces.RunspaceState); } }
    public System.Management.Automation.Runspaces.RunspaceState ExpectedState { get { return default(System.Management.Automation.Runspaces.RunspaceState); } }
     
  }
  public sealed class NotePropertyData : System.Management.Automation.Runspaces.TypeMemberData {
    public NotePropertyData(string name, object value) { }
     
    public bool IsHidden { get { return default(bool); } set { } }
    public object Value { get { return default(object); } set { } }
     
  }
  public enum OutputBufferingMode {
    Block = 2,
    Drop = 1,
    None = 0,
  }
#if V1_PIPELINE_API
  public abstract class Pipeline : System.IDisposable {
    internal Pipeline() { }
    public System.Management.Automation.Runspaces.CommandCollection Commands { get { return default(System.Management.Automation.Runspaces.CommandCollection); } }
    public abstract System.Management.Automation.Runspaces.PipelineReader<object> Error { get; }
    public virtual bool HadErrors { get { return default(bool); } }
    public abstract System.Management.Automation.Runspaces.PipelineWriter Input { get; }
    public long InstanceId { get { return default(long); } }
    public abstract bool IsNested { get; }
    public abstract System.Management.Automation.Runspaces.PipelineReader<System.Management.Automation.PSObject> Output { get; }
    public abstract System.Management.Automation.Runspaces.PipelineStateInfo PipelineStateInfo { get; }
    public abstract System.Management.Automation.Runspaces.Runspace Runspace { get; }
    public bool SetPipelineSessionState { get { return default(bool); } set { } }
     
    // Events
    public abstract event System.EventHandler<System.Management.Automation.Runspaces.PipelineStateEventArgs> StateChanged;
     
    public abstract System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Connect();
    public abstract void ConnectAsync();
    public abstract System.Management.Automation.Runspaces.Pipeline Copy();
    public void Dispose() { }
    protected virtual void Dispose(bool disposing) { }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Invoke() { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public abstract System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Invoke(System.Collections.IEnumerable input);
    public abstract void InvokeAsync();
    public abstract void Stop();
    public abstract void StopAsync();
  }
  public abstract class PipelineReader<T> {
    protected PipelineReader() { }
     
    public abstract int Count { get; }
    public abstract bool EndOfPipeline { get; }
    public abstract bool IsOpen { get; }
    public abstract int MaxCapacity { get; }
    public abstract System.Threading.WaitHandle WaitHandle { get; }
     
    // Events
    public abstract event System.EventHandler DataReady;
     
    public abstract void Close();
    public abstract System.Collections.ObjectModel.Collection<T> NonBlockingRead();
    public abstract System.Collections.ObjectModel.Collection<T> NonBlockingRead(int maxRequested);
    public abstract T Peek();
    public abstract T Read();
    public abstract System.Collections.ObjectModel.Collection<T> Read(int count);
    public abstract System.Collections.ObjectModel.Collection<T> ReadToEnd();
  }
#endif
  [System.FlagsAttribute]
  public enum PipelineResultTypes {
    All = 6,
    Debug = 5,
    Error = 2,
    None = 0,
    Null = 7,
    Output = 1,
    Verbose = 4,
    Warning = 3,
  }
  public enum PipelineState {
    Completed = 4,
    Disconnected = 6,
    Failed = 5,
    NotStarted = 0,
    Running = 1,
    Stopped = 3,
    Stopping = 2,
  }
#if V1_PIPELINE_API
    public sealed class PipelineStateEventArgs : System.EventArgs {
    internal PipelineStateEventArgs() { }
    public System.Management.Automation.Runspaces.PipelineStateInfo PipelineStateInfo { get { return default(System.Management.Automation.Runspaces.PipelineStateInfo); } }
     
  }
  public sealed class PipelineStateInfo {
    internal PipelineStateInfo() { }
    public System.Exception Reason { get { return default(System.Exception); } }
    public System.Management.Automation.Runspaces.PipelineState State { get { return default(System.Management.Automation.Runspaces.PipelineState); } }
     
  }
  public abstract class PipelineWriter {
    protected PipelineWriter() { }
     
    public abstract int Count { get; }
    public abstract bool IsOpen { get; }
    public abstract int MaxCapacity { get; }
    public abstract System.Threading.WaitHandle WaitHandle { get; }
     
    public abstract void Close();
    public abstract void Flush();
    public abstract int Write(object obj);
    public abstract int Write(object obj, bool enumerateCollection);
  }
  public sealed class PowerShellProcessInstance : System.IDisposable {
    public PowerShellProcessInstance() { }
    public PowerShellProcessInstance(System.Version powerShellVersion, System.Management.Automation.PSCredential credential, System.Management.Automation.ScriptBlock initializationScript, bool useWow64) { }
     
    public bool HasExited { get { return default(bool); } }
    public System.Diagnostics.Process Process { get { return default(System.Diagnostics.Process); } }
     
    public void Dispose() { }
  }
#endif // V1_PIPELINE_API
  public sealed class PropertySetData {
    public PropertySetData(System.Collections.Generic.IEnumerable<string> referencedProperties) { }
     
    public System.Collections.ObjectModel.Collection<string> ReferencedProperties { get { return default(System.Collections.ObjectModel.Collection<string>); } }
     
  }
#if V1_PIPELINE_API
  public sealed class ProviderConfigurationEntry : System.Management.Automation.Runspaces.RunspaceConfigurationEntry {
    public ProviderConfigurationEntry(string name, System.Type implementingType, string helpFileName) : base (default(string)) { }
     
    public string HelpFileName { get { return default(string); } }
    public System.Type ImplementingType { get { return default(System.Type); } }
     
  }
#endif
    public class PSConsoleLoadException : System.Exception, System.Management.Automation.IContainsErrorRecord {
    public PSConsoleLoadException() { }
    public PSConsoleLoadException(string message) { }
    public PSConsoleLoadException(string message, System.Exception innerException) { }
     
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override string Message { get { return default(string); } }
  }
  public sealed class PSSession {
    internal PSSession() { }
    public System.Management.Automation.PSPrimitiveDictionary ApplicationPrivateData { get { return default(System.Management.Automation.PSPrimitiveDictionary); } }
    public System.Management.Automation.Runspaces.RunspaceAvailability Availability { get { return default(System.Management.Automation.Runspaces.RunspaceAvailability); } }
    public string ComputerName { get { return default(string); } }
    public string ConfigurationName { get { return default(string); } }
    public int Id { get { return default(int); } }
    public System.Guid InstanceId { get { return default(System.Guid); } }
    public string Name { get { return default(string); } set { } }
    public System.Management.Automation.Runspaces.Runspace Runspace { get { return default(System.Management.Automation.Runspaces.Runspace); } }
     
    public override string ToString() { return default(string); }
  }
  public enum PSSessionConfigurationAccessMode {
    Disabled = 0,
    Local = 1,
    Remote = 2,
  }
  public enum PSSessionType {
    DefaultRemoteShell = 0,
    Workflow = 1,
  }
  public enum PSThreadOptions {
    Default = 0,
    ReuseThread = 2,
    UseCurrentThread = 3,
    UseNewThread = 1,
  }
  [System.Runtime.Serialization.DataContractAttribute]
  public class RemotingDebugRecord : System.Management.Automation.DebugRecord {
    public RemotingDebugRecord(string message, System.Management.Automation.Remoting.OriginInfo originInfo) : base (default(string)) { }
     
    public System.Management.Automation.Remoting.OriginInfo OriginInfo { get { return default(System.Management.Automation.Remoting.OriginInfo); } }
     
  }
  public class RemotingErrorRecord : System.Management.Automation.ErrorRecord {
    public RemotingErrorRecord(System.Management.Automation.ErrorRecord errorRecord, System.Management.Automation.Remoting.OriginInfo originInfo) : base (default(System.Exception), default(string), default(System.Management.Automation.ErrorCategory), default(object)) { }
     
    public System.Management.Automation.Remoting.OriginInfo OriginInfo { get { return default(System.Management.Automation.Remoting.OriginInfo); } }
  }
  [System.Runtime.Serialization.DataContractAttribute]
  public class RemotingProgressRecord : System.Management.Automation.ProgressRecord {
    public RemotingProgressRecord(System.Management.Automation.ProgressRecord progressRecord, System.Management.Automation.Remoting.OriginInfo originInfo) : base (default(int), default(string), default(string)) { }
     
    public System.Management.Automation.Remoting.OriginInfo OriginInfo { get { return default(System.Management.Automation.Remoting.OriginInfo); } }
     
  }
  [System.Runtime.Serialization.DataContractAttribute]
  public class RemotingVerboseRecord : System.Management.Automation.VerboseRecord {
    public RemotingVerboseRecord(string message, System.Management.Automation.Remoting.OriginInfo originInfo) : base (default(string)) { }
     
    public System.Management.Automation.Remoting.OriginInfo OriginInfo { get { return default(System.Management.Automation.Remoting.OriginInfo); } }
     
  }
  [System.Runtime.Serialization.DataContractAttribute]
  public class RemotingWarningRecord : System.Management.Automation.WarningRecord {
    public RemotingWarningRecord(string message, System.Management.Automation.Remoting.OriginInfo originInfo) : base (default(string)) { }
     
    public System.Management.Automation.Remoting.OriginInfo OriginInfo { get { return default(System.Management.Automation.Remoting.OriginInfo); } }
     
  }
  public abstract class Runspace : System.IDisposable {
    internal Runspace() { }
#if COM_APARTMENT_STATE
    public System.Threading.ApartmentState ApartmentState { get { return default(System.Threading.ApartmentState); } set { } }
#endif
    public abstract System.Management.Automation.Runspaces.RunspaceConnectionInfo ConnectionInfo { get; }
    public System.Management.Automation.Debugger Debugger { get { return default(System.Management.Automation.Debugger); } }
    public static System.Management.Automation.Runspaces.Runspace DefaultRunspace { get { return default(System.Management.Automation.Runspaces.Runspace); } set { } }
    public abstract System.Management.Automation.PSEventManager Events { get; }
    public abstract System.Management.Automation.Runspaces.InitialSessionState InitialSessionState { get; }
    public System.Guid InstanceId { get { return default(System.Guid); } }
    public abstract System.Management.Automation.JobManager JobManager { get; }
    public abstract System.Management.Automation.Runspaces.RunspaceConnectionInfo OriginalConnectionInfo { get; }
    public abstract System.Management.Automation.Runspaces.RunspaceAvailability RunspaceAvailability { get; protected set; }
#if V1_PIPELINE_API
    public abstract System.Management.Automation.Runspaces.RunspaceConfiguration RunspaceConfiguration { get; }
#endif
    public abstract System.Management.Automation.Runspaces.RunspaceStateInfo RunspaceStateInfo { get; }
    public System.Management.Automation.Runspaces.SessionStateProxy SessionStateProxy { get { return default(System.Management.Automation.Runspaces.SessionStateProxy); } }
    public abstract System.Management.Automation.Runspaces.PSThreadOptions ThreadOptions { get; set; }
    public abstract System.Version Version { get; }
     
    // Events
    public abstract event System.EventHandler<System.Management.Automation.Runspaces.RunspaceAvailabilityEventArgs> AvailabilityChanged;
    public abstract event System.EventHandler<System.Management.Automation.Runspaces.RunspaceStateEventArgs> StateChanged;
     
    public void ClearBaseTransaction() { }
    public abstract void Close();
    public abstract void CloseAsync();
    public abstract void Connect();
    public abstract void ConnectAsync();
#if V1_PIPELINE_API
    public abstract System.Management.Automation.Runspaces.Pipeline CreateDisconnectedPipeline();
#endif
    public abstract System.Management.Automation.PowerShell CreateDisconnectedPowerShell();
#if V1_PIPELINE_API
    public abstract System.Management.Automation.Runspaces.Pipeline CreateNestedPipeline();
    public abstract System.Management.Automation.Runspaces.Pipeline CreateNestedPipeline(string command, bool addToHistory);
    public abstract System.Management.Automation.Runspaces.Pipeline CreatePipeline();
    public abstract System.Management.Automation.Runspaces.Pipeline CreatePipeline(string command);
    public abstract System.Management.Automation.Runspaces.Pipeline CreatePipeline(string command, bool addToHistory);
#endif
    public abstract void Disconnect();
    public abstract void DisconnectAsync();
    public void Dispose() { }
    protected virtual void Dispose(bool disposing) { }
    public abstract System.Management.Automation.PSPrimitiveDictionary GetApplicationPrivateData();
    public abstract System.Management.Automation.Runspaces.RunspaceCapability GetCapabilities();
    public static System.Management.Automation.Runspaces.Runspace[] GetRunspaces(System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo) { return default(System.Management.Automation.Runspaces.Runspace[]); }
    public static System.Management.Automation.Runspaces.Runspace[] GetRunspaces(System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo, System.Management.Automation.Host.PSHost host) { return default(System.Management.Automation.Runspaces.Runspace[]); }
    public static System.Management.Automation.Runspaces.Runspace[] GetRunspaces(System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo, System.Management.Automation.Host.PSHost host, System.Management.Automation.Runspaces.TypeTable typeTable) { return default(System.Management.Automation.Runspaces.Runspace[]); }
    protected abstract void OnAvailabilityChanged(System.Management.Automation.Runspaces.RunspaceAvailabilityEventArgs e);
    public abstract void Open();
    public abstract void OpenAsync();
    public virtual void ResetRunspaceState() { }
#if TRANSACTIONS
    public void SetBaseTransaction(System.Transactions.CommittableTransaction transaction) { }
    public void SetBaseTransaction(System.Transactions.CommittableTransaction transaction, System.Management.Automation.RollbackSeverity severity) { }
#endif
    protected void UpdateRunspaceAvailability(System.Management.Automation.Runspaces.RunspaceState runspaceState, bool raiseEvent) { }
  }
  public enum RunspaceAvailability {
    Available = 1,
    AvailableForNestedCommand = 2,
    Busy = 3,
    None = 0,
  }
  public sealed class RunspaceAvailabilityEventArgs : System.EventArgs {
    internal RunspaceAvailabilityEventArgs() { }
    public System.Management.Automation.Runspaces.RunspaceAvailability RunspaceAvailability { get { return default(System.Management.Automation.Runspaces.RunspaceAvailability); } }
     
  }
  public enum RunspaceCapability {
    Default = 0,
    SupportsDisconnect = 1,
  }
#if V1_PIPELINE_API
  public abstract class RunspaceConfiguration {
    protected RunspaceConfiguration() { }
     
    public virtual System.Management.Automation.Runspaces.RunspaceConfigurationEntryCollection<System.Management.Automation.Runspaces.AssemblyConfigurationEntry> Assemblies { get { return default(System.Management.Automation.Runspaces.RunspaceConfigurationEntryCollection<System.Management.Automation.Runspaces.AssemblyConfigurationEntry>); } }
    public virtual System.Management.Automation.AuthorizationManager AuthorizationManager { get { return default(System.Management.Automation.AuthorizationManager); } }
    public virtual System.Management.Automation.Runspaces.RunspaceConfigurationEntryCollection<System.Management.Automation.Runspaces.CmdletConfigurationEntry> Cmdlets { get { return default(System.Management.Automation.Runspaces.RunspaceConfigurationEntryCollection<System.Management.Automation.Runspaces.CmdletConfigurationEntry>); } }
    public virtual System.Management.Automation.Runspaces.RunspaceConfigurationEntryCollection<System.Management.Automation.Runspaces.FormatConfigurationEntry> Formats { get { return default(System.Management.Automation.Runspaces.RunspaceConfigurationEntryCollection<System.Management.Automation.Runspaces.FormatConfigurationEntry>); } }
    public virtual System.Management.Automation.Runspaces.RunspaceConfigurationEntryCollection<System.Management.Automation.Runspaces.ScriptConfigurationEntry> InitializationScripts { get { return default(System.Management.Automation.Runspaces.RunspaceConfigurationEntryCollection<System.Management.Automation.Runspaces.ScriptConfigurationEntry>); } }
    public virtual System.Management.Automation.Runspaces.RunspaceConfigurationEntryCollection<System.Management.Automation.Runspaces.ProviderConfigurationEntry> Providers { get { return default(System.Management.Automation.Runspaces.RunspaceConfigurationEntryCollection<System.Management.Automation.Runspaces.ProviderConfigurationEntry>); } }
    public virtual System.Management.Automation.Runspaces.RunspaceConfigurationEntryCollection<System.Management.Automation.Runspaces.ScriptConfigurationEntry> Scripts { get { return default(System.Management.Automation.Runspaces.RunspaceConfigurationEntryCollection<System.Management.Automation.Runspaces.ScriptConfigurationEntry>); } }
    public abstract string ShellId { get; }
    public virtual System.Management.Automation.Runspaces.RunspaceConfigurationEntryCollection<System.Management.Automation.Runspaces.TypeConfigurationEntry> Types { get { return default(System.Management.Automation.Runspaces.RunspaceConfigurationEntryCollection<System.Management.Automation.Runspaces.TypeConfigurationEntry>); } }
     
    public System.Management.Automation.PSSnapInInfo AddPSSnapIn(string name, out System.Management.Automation.Runspaces.PSSnapInException warning) { warning = default(System.Management.Automation.Runspaces.PSSnapInException); return default(System.Management.Automation.PSSnapInInfo); }
    public static System.Management.Automation.Runspaces.RunspaceConfiguration Create() { return default(System.Management.Automation.Runspaces.RunspaceConfiguration); }
    public static System.Management.Automation.Runspaces.RunspaceConfiguration Create(string assemblyName) { return default(System.Management.Automation.Runspaces.RunspaceConfiguration); }
    public static System.Management.Automation.Runspaces.RunspaceConfiguration Create(string consoleFilePath, out System.Management.Automation.Runspaces.PSConsoleLoadException warnings) { warnings = default(System.Management.Automation.Runspaces.PSConsoleLoadException); return default(System.Management.Automation.Runspaces.RunspaceConfiguration); }
    public System.Management.Automation.PSSnapInInfo RemovePSSnapIn(string name, out System.Management.Automation.Runspaces.PSSnapInException warning) { warning = default(System.Management.Automation.Runspaces.PSSnapInException); return default(System.Management.Automation.PSSnapInInfo); }
  }
  public class RunspaceConfigurationAttributeException : System.Exception, System.Management.Automation.IContainsErrorRecord
    {
    public RunspaceConfigurationAttributeException() { }
    public RunspaceConfigurationAttributeException(string message) { }
    public RunspaceConfigurationAttributeException(string message, System.Exception innerException) { }
     
    public string AssemblyName { get { return default(string); } }
    public string Error { get { return default(string); } }
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override string Message { get { return default(string); } }
  }
  public abstract class RunspaceConfigurationEntry {
    protected RunspaceConfigurationEntry(string name) { }
     
    public bool BuiltIn { get { return default(bool); } }
    public string Name { get { return default(string); } }
    public System.Management.Automation.PSSnapInInfo PSSnapIn { get { return default(System.Management.Automation.PSSnapInInfo); } }
     
  }
  public sealed class RunspaceConfigurationEntryCollection<T> : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable where T : System.Management.Automation.Runspaces.RunspaceConfigurationEntry {
    public RunspaceConfigurationEntryCollection() { }
    public RunspaceConfigurationEntryCollection(System.Collections.Generic.IEnumerable<T> items) { }
     
    public int Count { get { return default(int); } }
    public T this[int index] { get { return default(T); } }
     
    public void Append(T item) { }
    public void Append(System.Collections.Generic.IEnumerable<T> items) { }
    public void Prepend(T item) { }
    public void Prepend(System.Collections.Generic.IEnumerable<T> items) { }
    public void RemoveItem(int index) { }
    public void RemoveItem(int index, int count) { }
    public void Reset() { }
    System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { return default(System.Collections.Generic.IEnumerator<T>); }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
    public void Update() { }
  }
  [System.AttributeUsageAttribute((AttributeTargets)1)]
  public sealed class RunspaceConfigurationTypeAttribute : System.Attribute {
    public RunspaceConfigurationTypeAttribute(string runspaceConfigurationType) { }
     
    public string RunspaceConfigurationType { get { return default(string); } }
     
  }
  public class RunspaceConfigurationTypeException : System.Exception, System.Management.Automation.IContainsErrorRecord
    {
    public RunspaceConfigurationTypeException() { }
    public RunspaceConfigurationTypeException(string message) { }
    public RunspaceConfigurationTypeException(string message, System.Exception innerException) { }
     
    public string AssemblyName { get { return default(string); } }
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override string Message { get { return default(string); } }
    public string TypeName { get { return default(string); } }
  }
#endif // V1_PIPELINE_API
  public abstract class RunspaceConnectionInfo {
    protected RunspaceConnectionInfo() { }
     
    public abstract System.Management.Automation.Runspaces.AuthenticationMechanism AuthenticationMechanism { get; set; }
    public int CancelTimeout { get { return default(int); } set { } }
    public abstract string CertificateThumbprint { get; set; }
    public abstract string ComputerName { get; set; }
    public abstract System.Management.Automation.PSCredential Credential { get; set; }
    public System.Globalization.CultureInfo Culture { get { return default(System.Globalization.CultureInfo); } set { } }
    public int IdleTimeout { get { return default(int); } set { } }
    public int OpenTimeout { get { return default(int); } set { } }
    public int OperationTimeout { get { return default(int); } set { } }
    public System.Globalization.CultureInfo UICulture { get { return default(System.Globalization.CultureInfo); } set { } }
     
    public virtual void SetSessionOptions(System.Management.Automation.Remoting.PSSessionOption options) { }
  }
  public static class RunspaceFactory {
    public static System.Management.Automation.Runspaces.Runspace CreateOutOfProcessRunspace(System.Management.Automation.Runspaces.TypeTable typeTable) { return default(System.Management.Automation.Runspaces.Runspace); }
#if V1_PIPELINE_API
    public static System.Management.Automation.Runspaces.Runspace CreateOutOfProcessRunspace(System.Management.Automation.Runspaces.TypeTable typeTable, System.Management.Automation.Runspaces.PowerShellProcessInstance processInstance) { return default(System.Management.Automation.Runspaces.Runspace); }
#endif // V1_PIPELINE_API
    public static System.Management.Automation.Runspaces.Runspace CreateRunspace() { return default(System.Management.Automation.Runspaces.Runspace); }
    public static System.Management.Automation.Runspaces.Runspace CreateRunspace(System.Management.Automation.Host.PSHost host) { return default(System.Management.Automation.Runspaces.Runspace); }
    public static System.Management.Automation.Runspaces.Runspace CreateRunspace(System.Management.Automation.Host.PSHost host, System.Management.Automation.Runspaces.InitialSessionState initialSessionState) { return default(System.Management.Automation.Runspaces.Runspace); }
#if V1_PIPELINE_API
    public static System.Management.Automation.Runspaces.Runspace CreateRunspace(System.Management.Automation.Host.PSHost host, System.Management.Automation.Runspaces.RunspaceConfiguration runspaceConfiguration) { return default(System.Management.Automation.Runspaces.Runspace); }
#endif // V1_PIPELINE_API
    public static System.Management.Automation.Runspaces.Runspace CreateRunspace(System.Management.Automation.Host.PSHost host, System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo) { return default(System.Management.Automation.Runspaces.Runspace); }
    public static System.Management.Automation.Runspaces.Runspace CreateRunspace(System.Management.Automation.Runspaces.InitialSessionState initialSessionState) { return default(System.Management.Automation.Runspaces.Runspace); }
#if V1_PIPELINE_API
    public static System.Management.Automation.Runspaces.Runspace CreateRunspace(System.Management.Automation.Runspaces.RunspaceConfiguration runspaceConfiguration) { return default(System.Management.Automation.Runspaces.Runspace); }
#endif // V1_PIPELINE_API
    public static System.Management.Automation.Runspaces.Runspace CreateRunspace(System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo) { return default(System.Management.Automation.Runspaces.Runspace); }
    public static System.Management.Automation.Runspaces.Runspace CreateRunspace(System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo, System.Management.Automation.Host.PSHost host, System.Management.Automation.Runspaces.TypeTable typeTable) { return default(System.Management.Automation.Runspaces.Runspace); }
    public static System.Management.Automation.Runspaces.Runspace CreateRunspace(System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo, System.Management.Automation.Host.PSHost host, System.Management.Automation.Runspaces.TypeTable typeTable, System.Management.Automation.PSPrimitiveDictionary applicationArguments) { return default(System.Management.Automation.Runspaces.Runspace); }
    public static System.Management.Automation.Runspaces.RunspacePool CreateRunspacePool() { return default(System.Management.Automation.Runspaces.RunspacePool); }
    public static System.Management.Automation.Runspaces.RunspacePool CreateRunspacePool(int minRunspaces, int maxRunspaces) { return default(System.Management.Automation.Runspaces.RunspacePool); }
    public static System.Management.Automation.Runspaces.RunspacePool CreateRunspacePool(int minRunspaces, int maxRunspaces, System.Management.Automation.Host.PSHost host) { return default(System.Management.Automation.Runspaces.RunspacePool); }
    public static System.Management.Automation.Runspaces.RunspacePool CreateRunspacePool(int minRunspaces, int maxRunspaces, System.Management.Automation.Runspaces.InitialSessionState initialSessionState, System.Management.Automation.Host.PSHost host) { return default(System.Management.Automation.Runspaces.RunspacePool); }
    public static System.Management.Automation.Runspaces.RunspacePool CreateRunspacePool(int minRunspaces, int maxRunspaces, System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo) { return default(System.Management.Automation.Runspaces.RunspacePool); }
    public static System.Management.Automation.Runspaces.RunspacePool CreateRunspacePool(int minRunspaces, int maxRunspaces, System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo, System.Management.Automation.Host.PSHost host) { return default(System.Management.Automation.Runspaces.RunspacePool); }
    public static System.Management.Automation.Runspaces.RunspacePool CreateRunspacePool(int minRunspaces, int maxRunspaces, System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo, System.Management.Automation.Host.PSHost host, System.Management.Automation.Runspaces.TypeTable typeTable) { return default(System.Management.Automation.Runspaces.RunspacePool); }
    public static System.Management.Automation.Runspaces.RunspacePool CreateRunspacePool(int minRunspaces, int maxRunspaces, System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo, System.Management.Automation.Host.PSHost host, System.Management.Automation.Runspaces.TypeTable typeTable, System.Management.Automation.PSPrimitiveDictionary applicationArguments) { return default(System.Management.Automation.Runspaces.RunspacePool); }
    public static System.Management.Automation.Runspaces.RunspacePool CreateRunspacePool(System.Management.Automation.Runspaces.InitialSessionState initialSessionState) { return default(System.Management.Automation.Runspaces.RunspacePool); }
  }
  public class RunspaceOpenModuleLoadException : System.Management.Automation.RuntimeException {
    public RunspaceOpenModuleLoadException() { }
    public RunspaceOpenModuleLoadException(string message) { }
    public RunspaceOpenModuleLoadException(string message, System.Exception innerException) { }
     
    public System.Management.Automation.PSDataCollection<System.Management.Automation.ErrorRecord> ErrorRecords { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.ErrorRecord>); } }
  }
  public sealed class RunspacePool : System.IDisposable {
    internal RunspacePool() { }
#if COM_APARTMENT_STATE
    public System.Threading.ApartmentState ApartmentState { get { return default(System.Threading.ApartmentState); } set { } }
#endif
    public System.TimeSpan CleanupInterval { get { return default(System.TimeSpan); } set { } }
    public System.Management.Automation.Runspaces.RunspaceConnectionInfo ConnectionInfo { get { return default(System.Management.Automation.Runspaces.RunspaceConnectionInfo); } }
    public System.Management.Automation.Runspaces.InitialSessionState InitialSessionState { get { return default(System.Management.Automation.Runspaces.InitialSessionState); } }
    public System.Guid InstanceId { get { return default(System.Guid); } }
    public bool IsDisposed { get { return default(bool); } }
    public System.Management.Automation.Runspaces.RunspacePoolAvailability RunspacePoolAvailability { get { return default(System.Management.Automation.Runspaces.RunspacePoolAvailability); } }
    public System.Management.Automation.RunspacePoolStateInfo RunspacePoolStateInfo { get { return default(System.Management.Automation.RunspacePoolStateInfo); } }
    public System.Management.Automation.Runspaces.PSThreadOptions ThreadOptions { get { return default(System.Management.Automation.Runspaces.PSThreadOptions); } set { } }
     
    // Events
    public event System.EventHandler<System.Management.Automation.Runspaces.RunspacePoolStateChangedEventArgs> StateChanged { add { } remove { } }
     
    public System.IAsyncResult BeginClose(System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
    public System.IAsyncResult BeginConnect(System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
    public System.IAsyncResult BeginDisconnect(System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
    public System.IAsyncResult BeginOpen(System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
    public void Close() { }
    public void Connect() { }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PowerShell> CreateDisconnectedPowerShells() { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PowerShell>); }
    public void Disconnect() { }
    public void Dispose() { }
    public void EndClose(System.IAsyncResult asyncResult) { }
    public void EndConnect(System.IAsyncResult asyncResult) { }
    public void EndDisconnect(System.IAsyncResult asyncResult) { }
    public void EndOpen(System.IAsyncResult asyncResult) { }
    public System.Management.Automation.PSPrimitiveDictionary GetApplicationPrivateData() { return default(System.Management.Automation.PSPrimitiveDictionary); }
    public int GetAvailableRunspaces() { return default(int); }
    public System.Management.Automation.Runspaces.RunspacePoolCapability GetCapabilities() { return default(System.Management.Automation.Runspaces.RunspacePoolCapability); }
    public int GetMaxRunspaces() { return default(int); }
    public int GetMinRunspaces() { return default(int); }
    public static System.Management.Automation.Runspaces.RunspacePool[] GetRunspacePools(System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo) { return default(System.Management.Automation.Runspaces.RunspacePool[]); }
    public static System.Management.Automation.Runspaces.RunspacePool[] GetRunspacePools(System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo, System.Management.Automation.Host.PSHost host) { return default(System.Management.Automation.Runspaces.RunspacePool[]); }
    public static System.Management.Automation.Runspaces.RunspacePool[] GetRunspacePools(System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo, System.Management.Automation.Host.PSHost host, System.Management.Automation.Runspaces.TypeTable typeTable) { return default(System.Management.Automation.Runspaces.RunspacePool[]); }
    public void Open() { }
    public bool SetMaxRunspaces(int maxRunspaces) { return default(bool); }
    public bool SetMinRunspaces(int minRunspaces) { return default(bool); }
  }
  public enum RunspacePoolAvailability {
    Available = 1,
    Busy = 2,
    None = 0,
  }
  public enum RunspacePoolCapability {
    Default = 0,
    SupportsDisconnect = 1,
  }
  public enum RunspacePoolState {
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
  public sealed class RunspacePoolStateChangedEventArgs : System.EventArgs {
    internal RunspacePoolStateChangedEventArgs() { }
    public System.Management.Automation.RunspacePoolStateInfo RunspacePoolStateInfo { get { return default(System.Management.Automation.RunspacePoolStateInfo); } }
     
  }
  public enum RunspaceState {
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
  public sealed class RunspaceStateEventArgs : System.EventArgs {
    internal RunspaceStateEventArgs() { }
    public System.Management.Automation.Runspaces.RunspaceStateInfo RunspaceStateInfo { get { return default(System.Management.Automation.Runspaces.RunspaceStateInfo); } }
     
  }
  public sealed class RunspaceStateInfo {
    internal RunspaceStateInfo() { }
    public System.Exception Reason { get { return default(System.Exception); } }
    public System.Management.Automation.Runspaces.RunspaceState State { get { return default(System.Management.Automation.Runspaces.RunspaceState); } }
     
    public override string ToString() { return default(string); }
  }
#if V1_PIPELINE_API
  public sealed class ScriptConfigurationEntry : System.Management.Automation.Runspaces.RunspaceConfigurationEntry {
    public ScriptConfigurationEntry(string name, string definition) : base (default(string)) { }
     
    public string Definition { get { return default(string); } }
     
  }
#endif
  public sealed class ScriptMethodData : System.Management.Automation.Runspaces.TypeMemberData {
    public ScriptMethodData(string name, System.Management.Automation.ScriptBlock scriptToInvoke) { }
     
    public System.Management.Automation.ScriptBlock Script { get { return default(System.Management.Automation.ScriptBlock); } set { } }
     
  }
  public sealed class ScriptPropertyData : System.Management.Automation.Runspaces.TypeMemberData {
    public ScriptPropertyData(string name, System.Management.Automation.ScriptBlock getScriptBlock) { }
    public ScriptPropertyData(string name, System.Management.Automation.ScriptBlock getScriptBlock, System.Management.Automation.ScriptBlock setScriptBlock) { }
     
    public System.Management.Automation.ScriptBlock GetScriptBlock { get { return default(System.Management.Automation.ScriptBlock); } set { } }
    public bool IsHidden { get { return default(bool); } set { } }
    public System.Management.Automation.ScriptBlock SetScriptBlock { get { return default(System.Management.Automation.ScriptBlock); } set { } }
     
  }
  public sealed class SessionStateAliasEntry : System.Management.Automation.Runspaces.SessionStateCommandEntry {
    public SessionStateAliasEntry(string name, string definition) : base (default(string)) { }
    public SessionStateAliasEntry(string name, string definition, string description) : base (default(string)) { }
    public SessionStateAliasEntry(string name, string definition, string description, System.Management.Automation.ScopedItemOptions options) : base (default(string)) { }
     
    public string Definition { get { return default(string); } }
    public string Description { get { return default(string); } }
    public System.Management.Automation.ScopedItemOptions Options { get { return default(System.Management.Automation.ScopedItemOptions); } }
     
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone() { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }
  }
  public sealed class SessionStateApplicationEntry : System.Management.Automation.Runspaces.SessionStateCommandEntry {
    public SessionStateApplicationEntry(string path) : base (default(string)) { }
     
    public string Path { get { return default(string); } }
     
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone() { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }
  }
  public sealed class SessionStateAssemblyEntry : System.Management.Automation.Runspaces.InitialSessionStateEntry {
    public SessionStateAssemblyEntry(string name) : base (default(string)) { }
    public SessionStateAssemblyEntry(string name, string fileName) : base (default(string)) { }
     
    public string FileName { get { return default(string); } }
     
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone() { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }
  }
  public sealed class SessionStateCmdletEntry : System.Management.Automation.Runspaces.SessionStateCommandEntry {
    public SessionStateCmdletEntry(string name, System.Type implementingType, string helpFileName) : base (default(string)) { }
     
    public string HelpFileName { get { return default(string); } }
    public System.Type ImplementingType { get { return default(System.Type); } }
     
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone() { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }
  }
  public abstract class SessionStateCommandEntry : System.Management.Automation.Runspaces.ConstrainedSessionStateEntry {
    protected SessionStateCommandEntry(string name) : base (default(string), default(System.Management.Automation.SessionStateEntryVisibility)) { }
    protected internal SessionStateCommandEntry(string name, System.Management.Automation.SessionStateEntryVisibility visibility) : base (default(string), default(System.Management.Automation.SessionStateEntryVisibility)) { }
     
    public System.Management.Automation.CommandTypes CommandType { get { return default(System.Management.Automation.CommandTypes); } }
     
  }
  public sealed class SessionStateFormatEntry : System.Management.Automation.Runspaces.InitialSessionStateEntry {
    public SessionStateFormatEntry(System.Management.Automation.ExtendedTypeDefinition typeDefinition) : base (default(string)) { }
    public SessionStateFormatEntry(System.Management.Automation.Runspaces.FormatTable formattable) : base (default(string)) { }
    public SessionStateFormatEntry(string fileName) : base (default(string)) { }
     
    public string FileName { get { return default(string); } }
    public System.Management.Automation.ExtendedTypeDefinition FormatData { get { return default(System.Management.Automation.ExtendedTypeDefinition); } }
    public System.Management.Automation.Runspaces.FormatTable Formattable { get { return default(System.Management.Automation.Runspaces.FormatTable); } }
     
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone() { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }
  }
  public sealed class SessionStateFunctionEntry : System.Management.Automation.Runspaces.SessionStateCommandEntry {
    public SessionStateFunctionEntry(string name, string definition) : base (default(string)) { }
    public SessionStateFunctionEntry(string name, string definition, System.Management.Automation.ScopedItemOptions options, string helpFile) : base (default(string)) { }
    public SessionStateFunctionEntry(string name, string definition, string helpFile) : base (default(string)) { }
     
    public string Definition { get { return default(string); } }
    public string HelpFile { get { return default(string); } }
    public System.Management.Automation.ScopedItemOptions Options { get { return default(System.Management.Automation.ScopedItemOptions); } }
     
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone() { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }
  }
  public sealed class SessionStateProviderEntry : System.Management.Automation.Runspaces.ConstrainedSessionStateEntry {
    public SessionStateProviderEntry(string name, System.Type implementingType, string helpFileName) : base (default(string), default(System.Management.Automation.SessionStateEntryVisibility)) { }
     
    public string HelpFileName { get { return default(string); } }
    public System.Type ImplementingType { get { return default(System.Type); } }
     
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone() { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }
  }
  public class SessionStateProxy {
    internal SessionStateProxy() { }
    public virtual System.Collections.Generic.List<string> Applications { get { return default(System.Collections.Generic.List<string>); } }
    public virtual System.Management.Automation.DriveManagementIntrinsics Drive { get { return default(System.Management.Automation.DriveManagementIntrinsics); } }
    public virtual System.Management.Automation.CommandInvocationIntrinsics InvokeCommand { get { return default(System.Management.Automation.CommandInvocationIntrinsics); } }
    public virtual System.Management.Automation.ProviderIntrinsics InvokeProvider { get { return default(System.Management.Automation.ProviderIntrinsics); } }
    public virtual System.Management.Automation.PSLanguageMode LanguageMode { get { return default(System.Management.Automation.PSLanguageMode); } set { } }
    public virtual System.Management.Automation.PSModuleInfo Module { get { return default(System.Management.Automation.PSModuleInfo); } }
    public virtual System.Management.Automation.PathIntrinsics Path { get { return default(System.Management.Automation.PathIntrinsics); } }
    public virtual System.Management.Automation.CmdletProviderManagementIntrinsics Provider { get { return default(System.Management.Automation.CmdletProviderManagementIntrinsics); } }
    public virtual System.Management.Automation.PSVariableIntrinsics PSVariable { get { return default(System.Management.Automation.PSVariableIntrinsics); } }
    public virtual System.Collections.Generic.List<string> Scripts { get { return default(System.Collections.Generic.List<string>); } }
     
    public virtual object GetVariable(string name) { return default(object); }
    public virtual void SetVariable(string name, object value) { }
  }
  public sealed class SessionStateScriptEntry : System.Management.Automation.Runspaces.SessionStateCommandEntry {
    public SessionStateScriptEntry(string path) : base (default(string)) { }
     
    public string Path { get { return default(string); } }
     
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone() { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }
  }
  public sealed class SessionStateTypeEntry : System.Management.Automation.Runspaces.InitialSessionStateEntry {
    public SessionStateTypeEntry(System.Management.Automation.Runspaces.TypeData typeData, bool isRemove) : base (default(string)) { }
    public SessionStateTypeEntry(System.Management.Automation.Runspaces.TypeTable typeTable) : base (default(string)) { }
    public SessionStateTypeEntry(string fileName) : base (default(string)) { }
     
    public string FileName { get { return default(string); } }
    public bool IsRemove { get { return default(bool); } }
    public System.Management.Automation.Runspaces.TypeData TypeData { get { return default(System.Management.Automation.Runspaces.TypeData); } }
    public System.Management.Automation.Runspaces.TypeTable TypeTable { get { return default(System.Management.Automation.Runspaces.TypeTable); } }
     
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone() { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }
  }
  public sealed class SessionStateVariableEntry : System.Management.Automation.Runspaces.ConstrainedSessionStateEntry {
    public SessionStateVariableEntry(string name, object value, string description) : base (default(string), default(System.Management.Automation.SessionStateEntryVisibility)) { }
    public SessionStateVariableEntry(string name, object value, string description, System.Management.Automation.ScopedItemOptions options) : base (default(string), default(System.Management.Automation.SessionStateEntryVisibility)) { }
    public SessionStateVariableEntry(string name, object value, string description, System.Management.Automation.ScopedItemOptions options, System.Attribute attribute) : base (default(string), default(System.Management.Automation.SessionStateEntryVisibility)) { }
    public SessionStateVariableEntry(string name, object value, string description, System.Management.Automation.ScopedItemOptions options, System.Collections.ObjectModel.Collection<System.Attribute> attributes) : base (default(string), default(System.Management.Automation.SessionStateEntryVisibility)) { }
     
    public System.Collections.ObjectModel.Collection<System.Attribute> Attributes { get { return default(System.Collections.ObjectModel.Collection<System.Attribute>); } }
    public string Description { get { return default(string); } }
    public System.Management.Automation.ScopedItemOptions Options { get { return default(System.Management.Automation.ScopedItemOptions); } }
    public object Value { get { return default(object); } }
     
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone() { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }
  }
  public sealed class SessionStateWorkflowEntry : System.Management.Automation.Runspaces.SessionStateCommandEntry {
    public SessionStateWorkflowEntry(string name, string definition) : base (default(string)) { }
    public SessionStateWorkflowEntry(string name, string definition, System.Management.Automation.ScopedItemOptions options, string helpFile) : base (default(string)) { }
    public SessionStateWorkflowEntry(string name, string definition, string helpFile) : base (default(string)) { }
     
    public string Definition { get { return default(string); } }
    public string HelpFile { get { return default(string); } }
    public System.Management.Automation.ScopedItemOptions Options { get { return default(System.Management.Automation.ScopedItemOptions); } }
     
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone() { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }
  }
#if V1_PIPELINE_API
  public sealed class TypeConfigurationEntry : System.Management.Automation.Runspaces.RunspaceConfigurationEntry {
    public TypeConfigurationEntry(System.Management.Automation.Runspaces.TypeData typeData, bool isRemove) : base (default(string)) { }
    public TypeConfigurationEntry(string fileName) : base (default(string)) { }
    public TypeConfigurationEntry(string name, string fileName) : base (default(string)) { }
     
    public string FileName { get { return default(string); } }
    public bool IsRemove { get { return default(bool); } }
    public System.Management.Automation.Runspaces.TypeData TypeData { get { return default(System.Management.Automation.Runspaces.TypeData); } }
  }
#endif
  public sealed class TypeData {
    public TypeData(string typeName) { }
    public TypeData(System.Type type) { }
     
    public string DefaultDisplayProperty { get { return default(string); } set { } }
    public System.Management.Automation.Runspaces.PropertySetData DefaultDisplayPropertySet { get { return default(System.Management.Automation.Runspaces.PropertySetData); } set { } }
    public System.Management.Automation.Runspaces.PropertySetData DefaultKeyPropertySet { get { return default(System.Management.Automation.Runspaces.PropertySetData); } set { } }
    public bool InheritPropertySerializationSet { get { return default(bool); } set { } }
    public bool IsOverride { get { return default(bool); } set { } }
    public System.Collections.Generic.Dictionary<string, System.Management.Automation.Runspaces.TypeMemberData> Members { get { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.Runspaces.TypeMemberData>); } }
    public System.Management.Automation.Runspaces.PropertySetData PropertySerializationSet { get { return default(System.Management.Automation.Runspaces.PropertySetData); } set { } }
    public uint SerializationDepth { get { return default(uint); } set { } }
    public string SerializationMethod { get { return default(string); } set { } }
    public string StringSerializationSource { get { return default(string); } set { } }
    public System.Type TargetTypeForDeserialization { get { return default(System.Type); } set { } }
    public System.Type TypeAdapter { get { return default(System.Type); } set { } }
    public System.Type TypeConverter { get { return default(System.Type); } set { } }
    public string TypeName { get { return default(string); } }
     
    public System.Management.Automation.Runspaces.TypeData Copy() { return default(System.Management.Automation.Runspaces.TypeData); }
  }
  public abstract class TypeMemberData {
    internal TypeMemberData() { }
    public string Name { get { return default(string); } }
  }
  public sealed class TypeTable {
    public TypeTable(System.Collections.Generic.IEnumerable<string> typeFiles) { }
     
    public void AddType(System.Management.Automation.Runspaces.TypeData typeData) { }
    public System.Management.Automation.Runspaces.TypeTable Clone(bool unshared) { return default(System.Management.Automation.Runspaces.TypeTable); }
    public static System.Collections.Generic.List<string> GetDefaultTypeFiles() { return default(System.Collections.Generic.List<string>); }
    public static System.Management.Automation.Runspaces.TypeTable LoadDefaultTypeFiles() { return default(System.Management.Automation.Runspaces.TypeTable); }
    public void RemoveType(string typeName) { }
  }
  public class TypeTableLoadException : System.Management.Automation.RuntimeException {
    public TypeTableLoadException() { }
    public TypeTableLoadException(string message) { }
    public TypeTableLoadException(string message, System.Exception innerException) { }
     
    public System.Collections.ObjectModel.Collection<string> Errors { get { return default(System.Collections.ObjectModel.Collection<string>); } }
     
    protected void SetDefaultErrorRecord() { }
  }
#if WSMAN
  public sealed class WSManConnectionInfo : System.Management.Automation.Runspaces.RunspaceConnectionInfo {
    public const string HttpScheme = "http";
    public const string HttpsScheme = "https";
     
    public WSManConnectionInfo() { }
    public WSManConnectionInfo(bool useSsl, string computerName, int port, string appName, string shellUri, System.Management.Automation.PSCredential credential) { }
    public WSManConnectionInfo(bool useSsl, string computerName, int port, string appName, string shellUri, System.Management.Automation.PSCredential credential, int openTimeout) { }
    public WSManConnectionInfo(System.Management.Automation.Runspaces.PSSessionType configurationType) { }
    public WSManConnectionInfo(string scheme, string computerName, int port, string appName, string shellUri, System.Management.Automation.PSCredential credential) { }
    public WSManConnectionInfo(string scheme, string computerName, int port, string appName, string shellUri, System.Management.Automation.PSCredential credential, int openTimeout) { }
    public WSManConnectionInfo(System.Uri uri) { }
    public WSManConnectionInfo(System.Uri uri, string shellUri, System.Management.Automation.PSCredential credential) { }
    public WSManConnectionInfo(System.Uri uri, string shellUri, string certificateThumbprint) { }
     
    public string AppName { get { return default(string); } set { } }
    public override System.Management.Automation.Runspaces.AuthenticationMechanism AuthenticationMechanism { get { return default(System.Management.Automation.Runspaces.AuthenticationMechanism); } set { } }
    public override string CertificateThumbprint { get { return default(string); } set { } }
    public override string ComputerName { get { return default(string); } set { } }
    public System.Uri ConnectionUri { get { return default(System.Uri); } set { } }
    public override System.Management.Automation.PSCredential Credential { get { return default(System.Management.Automation.PSCredential); } set { } }
    public bool EnableNetworkAccess { get { return default(bool); } set { } }
    public bool IncludePortInSPN { get { return default(bool); } set { } }
    public int MaximumConnectionRedirectionCount { get { return default(int); } set { } }
    public System.Nullable<int> MaximumReceivedDataSizePerCommand { get { return default(System.Nullable<int>); } set { } }
    public System.Nullable<int> MaximumReceivedObjectSize { get { return default(System.Nullable<int>); } set { } }
    public bool NoEncryption { get { return default(bool); } set { } }
    public bool NoMachineProfile { get { return default(bool); } set { } }
    public System.Management.Automation.Runspaces.OutputBufferingMode OutputBufferingMode { get { return default(System.Management.Automation.Runspaces.OutputBufferingMode); } set { } }
    public int Port { get { return default(int); } set { } }
    public System.Management.Automation.Remoting.ProxyAccessType ProxyAccessType { get { return default(System.Management.Automation.Remoting.ProxyAccessType); } set { } }
    public System.Management.Automation.Runspaces.AuthenticationMechanism ProxyAuthentication { get { return default(System.Management.Automation.Runspaces.AuthenticationMechanism); } set { } }
    public System.Management.Automation.PSCredential ProxyCredential { get { return default(System.Management.Automation.PSCredential); } set { } }
    public string Scheme { get { return default(string); } set { } }
    public string ShellUri { get { return default(string); } set { } }
    public bool SkipCACheck { get { return default(bool); } set { } }
    public bool SkipCNCheck { get { return default(bool); } set { } }
    public bool SkipRevocationCheck { get { return default(bool); } set { } }
    public bool UseCompression { get { return default(bool); } set { } }
    public bool UseUTF16 { get { return default(bool); } set { } }
     
    public System.Management.Automation.Runspaces.WSManConnectionInfo Copy() { return default(System.Management.Automation.Runspaces.WSManConnectionInfo); }
    public override void SetSessionOptions(System.Management.Automation.Remoting.PSSessionOption options) { }
  }
#endif
}
namespace System.Management.Automation.Security {
  public enum SystemEnforcementMode {
    Audit = 1,
    Enforce = 2,
    None = 0,
  }
  public sealed class SystemPolicy {
    internal SystemPolicy() { }
    public static System.Management.Automation.Security.SystemEnforcementMode GetLockdownPolicy(string path, System.Runtime.InteropServices.SafeHandle handle) { return default(System.Management.Automation.Security.SystemEnforcementMode); }
    public static System.Management.Automation.Security.SystemEnforcementMode GetSystemLockdownPolicy() { return default(System.Management.Automation.Security.SystemEnforcementMode); }
  }
}
