namespace Microsoft.PowerShell {
  public static partial class AdapterCodeMethods {
    // Methods
    public static string ConvertDNWithBinaryToString(System.Management.Automation.PSObject deInstance, System.Management.Automation.PSObject dnWithBinaryInstance) { return default(string); }
    public static long ConvertLargeIntegerToInt64(System.Management.Automation.PSObject deInstance, System.Management.Automation.PSObject largeIntegerInstance) { return default(long); }
  }
  public sealed partial class DeserializingTypeConverter : System.Management.Automation.PSTypeConverter {
    // Constructors
    public DeserializingTypeConverter() { }
     
    // Methods
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
    // Fields
    AllSigned = 2,
    Bypass = 4,
    Default = 3,
    RemoteSigned = 1,
    Restricted = 3,
    Undefined = 5,
    Unrestricted = 0,
  }
  public enum ExecutionPolicyScope {
    // Fields
    CurrentUser = 1,
    LocalMachine = 2,
    MachinePolicy = 4,
    Process = 0,
    UserPolicy = 3,
  }
  public sealed partial class PSAuthorizationManager : System.Management.Automation.AuthorizationManager {
    // Constructors
    public PSAuthorizationManager(string shellId) : base (default(string)) { }
     
    // Methods
    protected internal override bool ShouldRun(System.Management.Automation.CommandInfo commandInfo, System.Management.Automation.CommandOrigin origin, System.Management.Automation.Host.PSHost host, out System.Exception reason) { reason = default(System.Exception); return default(bool); }
  }
#if SNAPINS
  [System.ComponentModel.RunInstallerAttribute(true)]
  public sealed partial class PSCorePSSnapIn : System.Management.Automation.PSSnapIn {
    // Constructors
    public PSCorePSSnapIn() { }
     
    // Properties
    public override string Description { get { return default(string); } }
    public override string DescriptionResource { get { return default(string); } }
    public override string[] Formats { get { return default(string[]); } }
    public override string Name { get { return default(string); } }
    public override string[] Types { get { return default(string[]); } }
    public override string Vendor { get { return default(string); } }
    public override string VendorResource { get { return default(string); } }
     
    // Methods
  }
#endif
  public static partial class ToStringCodeMethods {
    // Methods
    public static string PropertyValueCollection(System.Management.Automation.PSObject instance) { return default(string); }
    public static string Type(System.Management.Automation.PSObject instance) { return default(string); }
    public static string XmlNode(System.Management.Automation.PSObject instance) { return default(string); }
    public static string XmlNodeList(System.Management.Automation.PSObject instance) { return default(string); }
  }
}
namespace Microsoft.PowerShell.Cim {
  public sealed partial class CimInstanceAdapter : System.Management.Automation.PSPropertyAdapter {
    // Constructors
    public CimInstanceAdapter() { }
     
    // Methods
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
    // Fields
    Default = 0,
    ReportErrors = 1,
    SilentlyContinue = 2,
  }
  public abstract partial class CmdletAdapter<TObjectInstance> where TObjectInstance : class {
    // Constructors
    protected CmdletAdapter() { }
     
    // Properties
    public string ClassName { get { return default(string); } }
    public string ClassVersion { get { return default(string); } }
    public System.Management.Automation.PSCmdlet Cmdlet { get { return default(System.Management.Automation.PSCmdlet); } }
    public System.Version ModuleVersion { get { return default(System.Version); } }
    public System.Collections.Generic.IDictionary<string, string> PrivateData { get { return default(System.Collections.Generic.IDictionary<string, string>); } }
     
    // Methods
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
  public sealed partial class MethodInvocationInfo {
    // Constructors
    public MethodInvocationInfo(string name, System.Collections.Generic.IEnumerable<Microsoft.PowerShell.Cmdletization.MethodParameter> parameters, Microsoft.PowerShell.Cmdletization.MethodParameter returnValue) { }
     
    // Properties
    public string MethodName { get { return default(string); } }
    public System.Collections.ObjectModel.KeyedCollection<string, Microsoft.PowerShell.Cmdletization.MethodParameter> Parameters { get { return default(System.Collections.ObjectModel.KeyedCollection<string, Microsoft.PowerShell.Cmdletization.MethodParameter>); } }
    public Microsoft.PowerShell.Cmdletization.MethodParameter ReturnValue { get { return default(Microsoft.PowerShell.Cmdletization.MethodParameter); } }
     
    // Methods
  }
  public sealed partial class MethodParameter {
    // Constructors
    public MethodParameter() { }
     
    // Properties
    public Microsoft.PowerShell.Cmdletization.MethodParameterBindings Bindings { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(Microsoft.PowerShell.Cmdletization.MethodParameterBindings); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public bool IsValuePresent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public System.Type ParameterType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Type); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public string ParameterTypeName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public object Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(object); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
     
    // Methods
  }
  [System.FlagsAttribute]
  public enum MethodParameterBindings {
    // Fields
    Error = 4,
    In = 1,
    Out = 2,
  }
  public abstract partial class QueryBuilder {
    // Constructors
    protected QueryBuilder() { }
     
    // Methods
    public virtual void AddQueryOption(string optionName, object optionValue) { }
    public virtual void ExcludeByProperty(string propertyName, System.Collections.IEnumerable excludedPropertyValues, bool wildcardsEnabled, Microsoft.PowerShell.Cmdletization.BehaviorOnNoMatch behaviorOnNoMatch) { }
    public virtual void FilterByAssociatedInstance(object associatedInstance, string associationName, string sourceRole, string resultRole, Microsoft.PowerShell.Cmdletization.BehaviorOnNoMatch behaviorOnNoMatch) { }
    public virtual void FilterByMaxPropertyValue(string propertyName, object maxPropertyValue, Microsoft.PowerShell.Cmdletization.BehaviorOnNoMatch behaviorOnNoMatch) { }
    public virtual void FilterByMinPropertyValue(string propertyName, object minPropertyValue, Microsoft.PowerShell.Cmdletization.BehaviorOnNoMatch behaviorOnNoMatch) { }
    public virtual void FilterByProperty(string propertyName, System.Collections.IEnumerable allowedPropertyValues, bool wildcardsEnabled, Microsoft.PowerShell.Cmdletization.BehaviorOnNoMatch behaviorOnNoMatch) { }
  }
}
#if XML_SERIALZATION
namespace Microsoft.PowerShell.Cmdletization.Xml {
  [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17361")]
  [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.microsoft.com/cmdlets-over-objects/2009/11")]
  public enum ConfirmImpact {
    // Fields
    High = 3,
    Low = 1,
    Medium = 2,
    None = 0,
  }
  [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17361")]
  [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.microsoft.com/cmdlets-over-objects/2009/11", IncludeInSchema=false)]
  public enum ItemsChoiceType {
    // Fields
    ExcludeQuery = 0,
    MaxValueQuery = 1,
    MinValueQuery = 2,
    RegularQuery = 3,
  }
}
#endif
namespace Microsoft.PowerShell.Commands {
  [System.Management.Automation.CmdletAttribute("Add", "History", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113279")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(Microsoft.PowerShell.Commands.HistoryInfo)})]
  public partial class AddHistoryCommand : System.Management.Automation.PSCmdlet {
    // Constructors
    public AddHistoryCommand() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute(Position=0, ValueFromPipeline=true)]
    public System.Management.Automation.PSObject[] InputObject { get { return default(System.Management.Automation.PSObject[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Passthru { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    // Methods
    protected override void BeginProcessing() { }
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Add", "PSSnapin", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113281")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.PSSnapInInfo)})]
  public sealed partial class AddPSSnapinCommand : Microsoft.PowerShell.Commands.PSSnapInCommandBase {
    // Constructors
    public AddPSSnapinCommand() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=true, ValueFromPipelineByPropertyName=true)]
    public string[] Name { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter PassThru { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    // Methods
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.AliasInfo)}, ProviderCmdlet="Copy-Item")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.AliasInfo)}, ProviderCmdlet="Get-ChildItem")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.AliasInfo)}, ProviderCmdlet="New-Item")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.AliasInfo)}, ProviderCmdlet="Rename-Item")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.AliasInfo)}, ProviderCmdlet="Set-Item")]
  [System.Management.Automation.Provider.CmdletProviderAttribute("Alias", (System.Management.Automation.Provider.ProviderCapabilities)(16))]
  public sealed partial class AliasProvider : Microsoft.PowerShell.Commands.SessionStateProviderBase {
    // Fields
    public const string ProviderName = "Alias";
     
    // Constructors
    public AliasProvider() { }
     
    // Methods
    protected override System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo> InitializeDefaultDrives() { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo>); }
    protected override object NewItemDynamicParameters(string path, string type, object newItemValue) { return default(object); }
    protected override object SetItemDynamicParameters(string path, object value) { return default(object); }
  }
  public partial class AliasProviderDynamicParameters {
    // Constructors
    public AliasProviderDynamicParameters() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.ScopedItemOptions Options { get { return default(System.Management.Automation.ScopedItemOptions); } set { } }
     
    // Methods
  }
  public partial class AlternateStreamData {
    // Constructors
    public AlternateStreamData() { }
     
    // Properties
    public string FileName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public long Length { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(long); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public string Stream { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
     
    // Methods
  }
  [System.Management.Automation.CmdletAttribute("Clear", "History", SupportsShouldProcess=true, DefaultParameterSetName="IDParameter", HelpUri="http://go.microsoft.com/fwlink/?LinkID=135199")]
  public partial class ClearHistoryCommand : System.Management.Automation.PSCmdlet {
    // Constructors
    public ClearHistoryCommand() { }
     
    // Properties
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
     
    // Methods
    protected override void BeginProcessing() { }
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Connect", "PSSession", SupportsShouldProcess=true, DefaultParameterSetName="Name", HelpUri="http://go.microsoft.com/fwlink/?LinkID=210604", RemotingCapability=(System.Management.Automation.RemotingCapability)(3))]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.Runspaces.PSSession)})]
  public partial class ConnectPSSessionCommand : Microsoft.PowerShell.Commands.PSRunspaceCmdlet, System.IDisposable {
    // Constructors
    public ConnectPSSessionCommand() { }
     
    // Properties
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
     
    // Methods
    protected override void BeginProcessing() { }
    public void Dispose() { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  public abstract partial class ConsoleCmdletsBase : System.Management.Automation.PSCmdlet {
    // Constructors
    protected ConsoleCmdletsBase() { }
  }
  [System.Management.Automation.CmdletAttribute("Disable", "PSRemoting", SupportsShouldProcess=true, ConfirmImpact=(System.Management.Automation.ConfirmImpact)(3), HelpUri="http://go.microsoft.com/fwlink/?LinkID=144298")]
  public sealed partial class DisablePSRemotingCommand : System.Management.Automation.PSCmdlet {
    // Constructors
    public DisablePSRemotingCommand() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    // Methods
    protected override void BeginProcessing() { }
    protected override void EndProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Disable", "PSSessionConfiguration", SupportsShouldProcess=true, ConfirmImpact=(System.Management.Automation.ConfirmImpact)(3), HelpUri="http://go.microsoft.com/fwlink/?LinkID=144299")]
  public sealed partial class DisablePSSessionConfigurationCommand : System.Management.Automation.PSCmdlet {
    // Constructors
    public DisablePSSessionConfigurationCommand() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position=0, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string[] Name { get { return default(string[]); } set { } }
     
    // Methods
    protected override void BeginProcessing() { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Disconnect", "PSSession", SupportsShouldProcess=true, DefaultParameterSetName="Session", HelpUri="http://go.microsoft.com/fwlink/?LinkID=210605", RemotingCapability=(System.Management.Automation.RemotingCapability)(3))]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.Runspaces.PSSession)})]
  public partial class DisconnectPSSessionCommand : Microsoft.PowerShell.Commands.PSRunspaceCmdlet, System.IDisposable {
    // Constructors
    public DisconnectPSSessionCommand() { }
     
    // Properties
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
     
    // Methods
    protected override void BeginProcessing() { }
    public void Dispose() { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Enable", "PSRemoting", SupportsShouldProcess=true, ConfirmImpact=(System.Management.Automation.ConfirmImpact)(3), HelpUri="http://go.microsoft.com/fwlink/?LinkID=144300")]
  public sealed partial class EnablePSRemotingCommand : System.Management.Automation.PSCmdlet {
    // Constructors
    public EnablePSRemotingCommand() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter SkipNetworkProfileCheck { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    // Methods
    protected override void BeginProcessing() { }
    protected override void EndProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Enable", "PSSessionConfiguration", SupportsShouldProcess=true, ConfirmImpact=(System.Management.Automation.ConfirmImpact)(3), HelpUri="http://go.microsoft.com/fwlink/?LinkID=144301")]
  public sealed partial class EnablePSSessionConfigurationCommand : System.Management.Automation.PSCmdlet {
    // Constructors
    public EnablePSSessionConfigurationCommand() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position=0, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string[] Name { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string SecurityDescriptorSddl { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter SkipNetworkProfileCheck { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    // Methods
    protected override void BeginProcessing() { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Enter", "PSSession", DefaultParameterSetName="ComputerName", HelpUri="http://go.microsoft.com/fwlink/?LinkID=135210", RemotingCapability=(System.Management.Automation.RemotingCapability)(3))]
  public partial class EnterPSSessionCommand : Microsoft.PowerShell.Commands.PSRemotingBaseCmdlet {
    // Constructors
    public EnterPSSessionCommand() { }
     
    // Properties
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
     
    // Methods
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.Provider.CmdletProviderAttribute("Environment", (System.Management.Automation.Provider.ProviderCapabilities)(16))]
  public sealed partial class EnvironmentProvider : Microsoft.PowerShell.Commands.SessionStateProviderBase {
    // Fields
    public const string ProviderName = "Environment";
     
    // Constructors
    public EnvironmentProvider() { }
     
    // Methods
    protected override System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo> InitializeDefaultDrives() { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo>); }
  }
  [System.Management.Automation.CmdletAttribute("Exit", "PSSession", HelpUri="http://go.microsoft.com/fwlink/?LinkID=135212")]
  public partial class ExitPSSessionCommand : Microsoft.PowerShell.Commands.PSRemotingCmdlet {
    // Constructors
    public ExitPSSessionCommand() { }
     
    // Methods
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Export", "Console", SupportsShouldProcess=true, HelpUri="http://go.microsoft.com/fwlink/?LinkID=113298")]
  public sealed partial class ExportConsoleCommand : Microsoft.PowerShell.Commands.ConsoleCmdletsBase {
    // Constructors
    public ExportConsoleCommand() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "NoOverwrite"})]
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter NoClobber { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "PSPath"})]
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=false, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    public string Path { get { return default(string); } set { } }
     
    // Methods
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Export", "ModuleMember", HelpUri="http://go.microsoft.com/fwlink/?LinkID=141551")]
  public sealed partial class ExportModuleMemberCommand : System.Management.Automation.PSCmdlet {
    // Constructors
    public ExportModuleMemberCommand() { }
     
    // Properties
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
     
    // Methods
    protected override void ProcessRecord() { }
  }
  public partial class FileSystemClearContentDynamicParameters {
    // Constructors
    public FileSystemClearContentDynamicParameters() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute]
    public string Stream { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
     
    // Methods
  }
  public enum FileSystemCmdletProviderEncoding {
    // Fields
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
  public partial class FileSystemContentDynamicParametersBase {
    // Constructors
    public FileSystemContentDynamicParametersBase() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute]
    public Microsoft.PowerShell.Commands.FileSystemCmdletProviderEncoding Encoding { get { return default(Microsoft.PowerShell.Commands.FileSystemCmdletProviderEncoding); } set { } }
    public System.Text.Encoding EncodingType { get { return default(System.Text.Encoding); } }
    [System.Management.Automation.ParameterAttribute]
    public string Stream { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public bool UsingByteEncoding { get { return default(bool); } }
    public bool WasStreamTypeSpecified { get { return default(bool); } }
     
    // Methods
  }
  public partial class FileSystemContentReaderDynamicParameters : Microsoft.PowerShell.Commands.FileSystemContentDynamicParametersBase {
    // Constructors
    public FileSystemContentReaderDynamicParameters() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute]
    public string Delimiter { get { return default(string); } set { } }
    public bool DelimiterSpecified { get { return default(bool); } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Raw { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Wait { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    // Methods
  }
  public partial class FileSystemContentWriterDynamicParameters : Microsoft.PowerShell.Commands.FileSystemContentDynamicParametersBase {
    // Constructors
    public FileSystemContentWriterDynamicParameters() { }
  }
  public partial class FileSystemItemProviderDynamicParameters {
    // Constructors
    public FileSystemItemProviderDynamicParameters() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute]
    public System.Nullable<System.DateTime> NewerThan { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Nullable<System.DateTime>); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Nullable<System.DateTime> OlderThan { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Nullable<System.DateTime>); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
     
    // Methods
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
  public sealed partial class FileSystemProvider : System.Management.Automation.Provider.NavigationCmdletProvider, System.Management.Automation.Provider.ICmdletProviderSupportsHelp, System.Management.Automation.Provider.IContentCmdletProvider, System.Management.Automation.Provider.IPropertyCmdletProvider, System.Management.Automation.Provider.ISecurityDescriptorCmdletProvider {
    // Fields
    public const string ProviderName = "FileSystem";
     
    // Constructors
    public FileSystemProvider() { }
     
    // Methods
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
  public partial class FileSystemProviderGetItemDynamicParameters {
    // Constructors
    public FileSystemProviderGetItemDynamicParameters() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string[] Stream { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string[]); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
     
    // Methods
  }
  public partial class FileSystemProviderRemoveItemDynamicParameters {
    // Constructors
    public FileSystemProviderRemoveItemDynamicParameters() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string[] Stream { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string[]); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
     
    // Methods
  }
  [System.Management.Automation.CmdletAttribute("ForEach", "Object", SupportsShouldProcess=true, DefaultParameterSetName="ScriptBlockSet", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113300", RemotingCapability=(System.Management.Automation.RemotingCapability)(0))]
  public sealed partial class ForEachObjectCommand : System.Management.Automation.PSCmdlet {
    // Constructors
    public ForEachObjectCommand() { }
     
    // Properties
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
     
    // Methods
    protected override void BeginProcessing() { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Format", "Default")]
  public partial class FormatDefaultCommand : Microsoft.PowerShell.Commands.Internal.Format.FrontEndCommandBase {
    // Constructors
    public FormatDefaultCommand() { }
  }
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.FunctionInfo)}, ProviderCmdlet="Copy-Item")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.FunctionInfo)}, ProviderCmdlet="Get-ChildItem")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.FunctionInfo)}, ProviderCmdlet="Get-Item")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.FunctionInfo)}, ProviderCmdlet="New-Item")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.FunctionInfo)}, ProviderCmdlet="Rename-Item")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.FunctionInfo)}, ProviderCmdlet="Set-Item")]
  [System.Management.Automation.Provider.CmdletProviderAttribute("Function", (System.Management.Automation.Provider.ProviderCapabilities)(16))]
  public sealed partial class FunctionProvider : Microsoft.PowerShell.Commands.SessionStateProviderBase {
    // Fields
    public const string ProviderName = "Function";
     
    // Constructors
    public FunctionProvider() { }
     
    // Methods
    protected override System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo> InitializeDefaultDrives() { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo>); }
    protected override object NewItemDynamicParameters(string path, string type, object newItemValue) { return default(object); }
    protected override object SetItemDynamicParameters(string path, object value) { return default(object); }
  }
  public partial class FunctionProviderDynamicParameters {
    // Constructors
    public FunctionProviderDynamicParameters() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.ScopedItemOptions Options { get { return default(System.Management.Automation.ScopedItemOptions); } set { } }
     
    // Methods
  }
  [System.Management.Automation.CmdletAttribute("Get", "Command", DefaultParameterSetName="CmdletSet", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113309")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.AliasInfo), typeof(System.Management.Automation.ApplicationInfo), typeof(System.Management.Automation.FunctionInfo), typeof(System.Management.Automation.CmdletInfo), typeof(System.Management.Automation.ExternalScriptInfo), typeof(System.Management.Automation.FilterInfo), typeof(System.Management.Automation.WorkflowInfo), typeof(string)})]
  public sealed partial class GetCommandCommand : System.Management.Automation.PSCmdlet {
    // Constructors
    public GetCommandCommand() { }
     
    // Properties
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
     
    // Methods
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
  }
  public static partial class GetHelpCodeMethods {
    // Methods
    public static string GetHelpUri(System.Management.Automation.PSObject commandInfoPSObject) { return default(string); }
  }
  [System.Management.Automation.CmdletAttribute("Get", "Help", DefaultParameterSetName="AllUsersView", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113316")]
  public sealed partial class GetHelpCommand : System.Management.Automation.PSCmdlet {
    // Constructors
    public GetHelpCommand() { }
     
    // Properties
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
     
    // Methods
    protected override void BeginProcessing() { }
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Get", "History", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113317")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(Microsoft.PowerShell.Commands.HistoryInfo)})]
  public partial class GetHistoryCommand : System.Management.Automation.PSCmdlet {
    // Constructors
    public GetHistoryCommand() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute(Position=1)]
    [System.Management.Automation.ValidateRangeAttribute(0, 32767)]
    public int Count { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(Position=0, ValueFromPipeline=true)]
    [System.Management.Automation.ValidateRangeAttribute((long)1, (long)9223372036854775807)]
    public long[] Id { get { return default(long[]); } set { } }
     
    // Methods
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Get", "Job", DefaultParameterSetName="SessionIdParameterSet", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113328")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.Job)})]
  public partial class GetJobCommand : Microsoft.PowerShell.Commands.JobCmdletBase {
    // Constructors
    public GetJobCommand() { }
     
    // Properties
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
     
    // Methods
    protected System.Collections.Generic.List<System.Management.Automation.Job> FindJobs() { return default(System.Collections.Generic.List<System.Management.Automation.Job>); }
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Get", "Module", DefaultParameterSetName="Loaded", HelpUri="http://go.microsoft.com/fwlink/?LinkID=141552")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.PSModuleInfo)})]
  public sealed partial class GetModuleCommand : Microsoft.PowerShell.Commands.ModuleCmdletBase, System.IDisposable {
    // Constructors
    public GetModuleCommand() { }
     
    // Properties
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
     
    // Methods
    public void Dispose() { }
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Get", "PSSession", DefaultParameterSetName="Name", HelpUri="http://go.microsoft.com/fwlink/?LinkID=135219", RemotingCapability=(System.Management.Automation.RemotingCapability)(3))]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.Runspaces.PSSession)})]
  public partial class GetPSSessionCommand : Microsoft.PowerShell.Commands.PSRunspaceCmdlet, System.IDisposable {
    // Constructors
    public GetPSSessionCommand() { }
     
    // Properties
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
     
    // Methods
    public void Dispose() { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Get", "PSSessionConfiguration", HelpUri="http://go.microsoft.com/fwlink/?LinkID=144304")]
  [System.Management.Automation.OutputTypeAttribute(new string[]{ "Microsoft.PowerShell.Commands.PSSessionConfigurationCommands#PSSessionConfiguration"})]
  public sealed partial class GetPSSessionConfigurationCommand : System.Management.Automation.PSCmdlet {
    // Constructors
    public GetPSSessionConfigurationCommand() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=false)]
    public string[] Name { get { return default(string[]); } set { } }
     
    // Methods
    protected override void BeginProcessing() { }
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Get", "PSSnapin", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113330")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.PSSnapInInfo)})]
  public sealed partial class GetPSSnapinCommand : Microsoft.PowerShell.Commands.PSSnapInCommandBase {
    // Constructors
    public GetPSSnapinCommand() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=false)]
    public string[] Name { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=false)]
    public System.Management.Automation.SwitchParameter Registered { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    // Methods
    protected override void BeginProcessing() { }
  }
  public partial class HelpCategoryInvalidException : System.ArgumentException, System.Management.Automation.IContainsErrorRecord {
    // Constructors
    public HelpCategoryInvalidException() { }
#if RUNTIME_SERIALIZATION
    protected HelpCategoryInvalidException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public HelpCategoryInvalidException(string helpCategory) { }
    public HelpCategoryInvalidException(string helpCategory, System.Exception innerException) { }
     
    // Properties
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public string HelpCategory { get { return default(string); } }
    public override string Message { get { return default(string); } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
  }
  public partial class HelpNotFoundException :
#if RUNTIME_SERIALIZATION
    System.SystemException
#else
    System.Exception
#endif
    , System.Management.Automation.IContainsErrorRecord
  {
    // Constructors
    public HelpNotFoundException() { }
#if RUNTIME_SERIALIZATION
    protected HelpNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public HelpNotFoundException(string helpTopic) { }
    public HelpNotFoundException(string helpTopic, System.Exception innerException) { }
     
    // Properties
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public string HelpTopic { get { return default(string); } }
    public override string Message { get { return default(string); } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
  }
  public partial class HistoryInfo {
    internal HistoryInfo() { }
    // Properties
    public string CommandLine { get { return default(string); } }
    public System.DateTime EndExecutionTime { get { return default(System.DateTime); } }
    public System.Management.Automation.Runspaces.PipelineState ExecutionStatus { get { return default(System.Management.Automation.Runspaces.PipelineState); } }
    public long Id { get { return default(long); } }
    public System.DateTime StartExecutionTime { get { return default(System.DateTime); } }
     
    // Methods
    public Microsoft.PowerShell.Commands.HistoryInfo Clone() { return default(Microsoft.PowerShell.Commands.HistoryInfo); }
    public override string ToString() { return default(string); }
  }
  [System.Management.Automation.CmdletAttribute("Import", "Module", DefaultParameterSetName="Name", HelpUri="http://go.microsoft.com/fwlink/?LinkID=141553")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.PSModuleInfo)})]
  public sealed partial class ImportModuleCommand : Microsoft.PowerShell.Commands.ModuleCmdletBase, System.IDisposable {
    // Constructors
    public ImportModuleCommand() { }
     
    // Properties
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
     
    // Methods
    protected override void BeginProcessing() { }
    public void Dispose() { }
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Invoke", "Command", DefaultParameterSetName="InProcess", HelpUri="http://go.microsoft.com/fwlink/?LinkID=135225", RemotingCapability=(System.Management.Automation.RemotingCapability)(3))]
  public partial class InvokeCommandCommand : Microsoft.PowerShell.Commands.PSExecutionCmdlet, System.IDisposable {
    // Constructors
    public InvokeCommandCommand() { }
     
    // Properties
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
     
    // Methods
    protected override void BeginProcessing() { }
    public void Dispose() { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Invoke", "History", SupportsShouldProcess=true, HelpUri="http://go.microsoft.com/fwlink/?LinkID=113344")]
  public partial class InvokeHistoryCommand : System.Management.Automation.PSCmdlet {
    // Constructors
    public InvokeHistoryCommand() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute(Position=0, ValueFromPipelineByPropertyName=true)]
    public string Id { get { return default(string); } set { } }
     
    // Methods
    protected override void EndProcessing() { }
  }
  public partial class JobCmdletBase : Microsoft.PowerShell.Commands.PSRemotingCmdlet {
    // Constructors
    public JobCmdletBase() { }
     
    // Properties
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
     
    // Methods
    protected override void BeginProcessing() { }
  }
  public partial class ModuleCmdletBase : System.Management.Automation.PSCmdlet {
    // Constructors
    public ModuleCmdletBase() { }
     
    // Properties
    protected bool AddToAppDomainLevelCache { get { return default(bool); } set { } }
    protected object[] BaseArgumentList { get { return default(object[]); } set { } }
    protected bool BaseDisableNameChecking { get { return default(bool); } set { } }
     
    // Methods
    protected internal void ImportModuleMembers(System.Management.Automation.PSModuleInfo sourceModule, string prefix) { }
    protected internal void ImportModuleMembers(System.Management.Automation.PSModuleInfo sourceModule, string prefix, Microsoft.PowerShell.Commands.ModuleCmdletBase.ImportModuleOptions options) { }
     
    // Nested Types
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    protected internal partial struct ImportModuleOptions {
    }
  }
  public partial class ModuleSpecification {
    // Constructors
    public ModuleSpecification(System.Collections.Hashtable moduleSpecification) { }
    public ModuleSpecification(string moduleName) { }
     
    // Properties
    public System.Nullable<System.Guid> Guid { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Nullable<System.Guid>); } }
    public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
    public System.Version Version { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Version); } }
     
    // Methods
  }
  [System.Management.Automation.CmdletAttribute("New", "Module", DefaultParameterSetName="ScriptBlock", HelpUri="http://go.microsoft.com/fwlink/?LinkID=141554")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.PSModuleInfo)})]
  public sealed partial class NewModuleCommand : Microsoft.PowerShell.Commands.ModuleCmdletBase {
    // Constructors
    public NewModuleCommand() { }
     
    // Properties
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
     
    // Methods
    protected override void EndProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("New", "ModuleManifest", SupportsShouldProcess=true, HelpUri="http://go.microsoft.com/fwlink/?LinkID=141555")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(string)})]
  public sealed partial class NewModuleManifestCommand : System.Management.Automation.PSCmdlet {
    // Constructors
    public NewModuleManifestCommand() { }
     
    // Properties
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
     
    // Methods
    protected override void BeginProcessing() { }
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("New", "PSSession", DefaultParameterSetName="ComputerName", HelpUri="http://go.microsoft.com/fwlink/?LinkID=135237", RemotingCapability=(System.Management.Automation.RemotingCapability)(3))]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.Runspaces.PSSession)})]
  public partial class NewPSSessionCommand : Microsoft.PowerShell.Commands.PSRemotingBaseCmdlet, System.IDisposable {
    // Constructors
    public NewPSSessionCommand() { }
     
    // Properties
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
     
    // Methods
    protected override void BeginProcessing() { }
    public void Dispose() { }
    protected void Dispose(bool disposing) { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("New", "PSSessionConfigurationFile", HelpUri="http://go.microsoft.com/fwlink/?LinkID=217036")]
  public partial class NewPSSessionConfigurationFileCommand : System.Management.Automation.PSCmdlet {
    // Constructors
    public NewPSSessionConfigurationFileCommand() { }
     
    // Properties
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
     
    // Methods
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("New", "PSSessionOption", HelpUri="http://go.microsoft.com/fwlink/?LinkID=144305", RemotingCapability=(System.Management.Automation.RemotingCapability)(0))]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.Remoting.PSSessionOption)})]
  public sealed partial class NewPSSessionOptionCommand : System.Management.Automation.PSCmdlet {
    // Constructors
    public NewPSSessionOptionCommand() { }
     
    // Properties
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
     
    // Methods
    protected override void BeginProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("New", "PSTransportOption", HelpUri="http://go.microsoft.com/fwlink/?LinkID=210608", RemotingCapability=(System.Management.Automation.RemotingCapability)(0))]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(Microsoft.PowerShell.Commands.WSManConfigurationOption)})]
  public sealed partial class NewPSTransportOptionCommand : System.Management.Automation.PSCmdlet {
    // Constructors
    public NewPSTransportOptionCommand() { }
     
    // Properties
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
     
    // Methods
    protected override void ProcessRecord() { }
  }
  public abstract partial class ObjectEventRegistrationBase : System.Management.Automation.PSCmdlet {
    // Constructors
    protected ObjectEventRegistrationBase() { }
     
    // Properties
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
     
    // Methods
    protected override void BeginProcessing() { }
    protected override void EndProcessing() { }
    protected abstract object GetSourceObject();
    protected abstract string GetSourceObjectEventName();
  }
  public enum OpenMode {
    // Fields
    Add = 0,
    New = 1,
    Overwrite = 2,
  }
  [System.Management.Automation.CmdletAttribute("Out", "Default", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113362", RemotingCapability=(System.Management.Automation.RemotingCapability)(0))]
  public partial class OutDefaultCommand : Microsoft.PowerShell.Commands.Internal.Format.FrontEndCommandBase {
    // Constructors
    public OutDefaultCommand() { }
     
    // Methods
    protected override void BeginProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Out", "Host", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113365", RemotingCapability=(System.Management.Automation.RemotingCapability)(0))]
  public partial class OutHostCommand : Microsoft.PowerShell.Commands.Internal.Format.FrontEndCommandBase {
    // Constructors
    public OutHostCommand() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Paging { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    // Methods
    protected override void BeginProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Out", "LineOutput")]
  public partial class OutLineOutputCommand : Microsoft.PowerShell.Commands.Internal.Format.FrontEndCommandBase {
    // Constructors
    public OutLineOutputCommand() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0)]
    public object LineOutput { get { return default(object); } set { } }
     
    // Methods
    protected override void BeginProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Out", "Null", SupportsShouldProcess=false, HelpUri="http://go.microsoft.com/fwlink/?LinkID=113366", RemotingCapability=(System.Management.Automation.RemotingCapability)(0))]
  public partial class OutNullCommand : System.Management.Automation.PSCmdlet {
    // Constructors
    public OutNullCommand() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute(ValueFromPipeline=true)]
    public System.Management.Automation.PSObject InputObject { get { return default(System.Management.Automation.PSObject); } set { } }
     
    // Methods
    protected override void ProcessRecord() { }
  }
  public enum OutTarget {
    // Fields
    Default = 0,
    Host = 1,
    Job = 2,
  }
  public abstract partial class PSExecutionCmdlet : Microsoft.PowerShell.Commands.PSRemotingBaseCmdlet {
    // Fields
    protected const string FilePathComputerNameParameterSet = "FilePathComputerName";
    protected const string FilePathSessionParameterSet = "FilePathRunspace";
    protected const string FilePathUriParameterSet = "FilePathUri";
    protected const string LiteralFilePathComputerNameParameterSet = "LiteralFilePathComputerName";
     
    // Constructors
    protected PSExecutionCmdlet() { }
     
    // Properties
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
     
    // Methods
    protected override void BeginProcessing() { }
    protected void CloseAllInputStreams() { }
    protected virtual void CreateHelpersForSpecifiedComputerNames() { }
    protected void CreateHelpersForSpecifiedRunspaces() { }
    protected void CreateHelpersForSpecifiedUris() { }
    protected System.Management.Automation.ScriptBlock GetScriptBlockFromFile(string filePath, bool isLiteralPath) { return default(System.Management.Automation.ScriptBlock); }
  }
  public abstract partial class PSRemotingBaseCmdlet : Microsoft.PowerShell.Commands.PSRemotingCmdlet {
    // Fields
    protected const string UriParameterSet = "Uri";
     
    // Constructors
    protected PSRemotingBaseCmdlet() { }
     
    // Properties
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
     
    // Methods
    protected override void BeginProcessing() { }
    protected void ValidateComputerName(string[] computerNames) { }
    protected void ValidateRemoteRunspacesSpecified() { }
  }
  public abstract partial class PSRemotingCmdlet : System.Management.Automation.PSCmdlet {
    // Fields
    protected const string ComputerNameParameterSet = "ComputerName";
    protected const string DefaultPowerShellRemoteShellAppName = "WSMan";
    protected const string DefaultPowerShellRemoteShellName = "http://schemas.microsoft.com/powershell/Microsoft.PowerShell";
    protected const string SessionParameterSet = "Session";
     
    // Constructors
    protected PSRemotingCmdlet() { }
     
    // Methods
    protected override void BeginProcessing() { }
    protected string ResolveAppName(string appName) { return default(string); }
    protected string ResolveComputerName(string computerName) { return default(string); }
    protected void ResolveComputerNames(string[] computerNames, out string[] resolvedComputerNames) { resolvedComputerNames = default(string[]); }
    protected string ResolveShell(string shell) { return default(string); }
  }
  public abstract partial class PSRunspaceCmdlet : Microsoft.PowerShell.Commands.PSRemotingCmdlet {
    // Fields
    protected const string IdParameterSet = "Id";
    protected const string InstanceIdParameterSet = "InstanceId";
    protected const string NameParameterSet = "Name";
     
    // Constructors
    protected PSRunspaceCmdlet() { }
     
    // Properties
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
     
    // Methods
    protected System.Collections.Generic.Dictionary<System.Guid, System.Management.Automation.Runspaces.PSSession> GetMatchingRunspaces(bool writeobject, bool writeErrorOnNoMatch) { return default(System.Collections.Generic.Dictionary<System.Guid, System.Management.Automation.Runspaces.PSSession>); }
    protected System.Collections.Generic.Dictionary<System.Guid, System.Management.Automation.Runspaces.PSSession> GetMatchingRunspacesByName(bool writeobject, bool writeErrorOnNoMatch) { return default(System.Collections.Generic.Dictionary<System.Guid, System.Management.Automation.Runspaces.PSSession>); }
    protected System.Collections.Generic.Dictionary<System.Guid, System.Management.Automation.Runspaces.PSSession> GetMatchingRunspacesByRunspaceId(bool writeobject, bool writeErrorOnNoMatch) { return default(System.Collections.Generic.Dictionary<System.Guid, System.Management.Automation.Runspaces.PSSession>); }
  }
  public partial class PSSessionConfigurationCommandBase : System.Management.Automation.PSCmdlet {
    internal PSSessionConfigurationCommandBase() { }
    // Properties
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
     
    // Methods
  }
  public abstract partial class PSSnapInCommandBase : System.Management.Automation.PSCmdlet, System.IDisposable {
    // Constructors
    protected PSSnapInCommandBase() { }
     
    // Properties
    protected internal bool ShouldGetAll { get { return default(bool); } set { } }
     
    // Methods
    public void Dispose() { }
    protected override void EndProcessing() { }
    protected internal System.Collections.ObjectModel.Collection<System.Management.Automation.PSSnapInInfo> GetSnapIns(string pattern) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSSnapInInfo>); }
  }
  [System.Management.Automation.CmdletAttribute("Receive", "Job", DefaultParameterSetName="Location", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113372", RemotingCapability=(System.Management.Automation.RemotingCapability)(2))]
  public partial class ReceiveJobCommand : Microsoft.PowerShell.Commands.JobCmdletBase, System.IDisposable {
    // Fields
    protected const string LocationParameterSet = "Location";
     
    // Constructors
    public ReceiveJobCommand() { }
     
    // Properties
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
     
    // Methods
    protected override void BeginProcessing() { }
    public void Dispose() { }
    protected void Dispose(bool disposing) { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Receive", "PSSession", SupportsShouldProcess=true, DefaultParameterSetName="Session", HelpUri="http://go.microsoft.com/fwlink/?LinkID=217037", RemotingCapability=(System.Management.Automation.RemotingCapability)(3))]
  public partial class ReceivePSSessionCommand : Microsoft.PowerShell.Commands.PSRemotingCmdlet {
    // Constructors
    public ReceivePSSessionCommand() { }
     
    // Properties
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
     
    // Methods
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Register", "PSSessionConfiguration", DefaultParameterSetName="NameParameterSet", SupportsShouldProcess=true, ConfirmImpact=(System.Management.Automation.ConfirmImpact)(3), HelpUri="http://go.microsoft.com/fwlink/?LinkID=144306")]
  public sealed partial class RegisterPSSessionConfigurationCommand : Microsoft.PowerShell.Commands.PSSessionConfigurationCommandBase {
    // Constructors
    public RegisterPSSessionConfigurationCommand() { }
     
    // Properties
    [System.Management.Automation.AliasAttribute(new string[]{ "PA"})]
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    [System.Management.Automation.ValidateSetAttribute(new string[]{ "x86", "amd64"})]
    public string ProcessorArchitecture { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="NameParameterSet")]
    public System.Management.Automation.Runspaces.PSSessionType SessionType { get { return default(System.Management.Automation.Runspaces.PSSessionType); } set { } }
     
    // Methods
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
  public sealed partial class RegistryProvider : System.Management.Automation.Provider.NavigationCmdletProvider, System.Management.Automation.Provider.IDynamicPropertyCmdletProvider, System.Management.Automation.Provider.IPropertyCmdletProvider, System.Management.Automation.Provider.ISecurityDescriptorCmdletProvider {
    // Fields
    public const string ProviderName = "Registry";
     
    // Constructors
    public RegistryProvider() { }
     
    // Methods
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
  public partial class RegistryProviderSetItemDynamicParameter {
    // Constructors
    public RegistryProviderSetItemDynamicParameter() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    public Microsoft.Win32.RegistryValueKind Type { get { return default(Microsoft.Win32.RegistryValueKind); } set { } }
     
    // Methods
  }
#endif
  [System.Management.Automation.CmdletAttribute("Remove", "Job", SupportsShouldProcess=true, DefaultParameterSetName="SessionIdParameterSet", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113377")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.Job)}, ParameterSetName=new string[]{ "JobParameterSet"})]
  public partial class RemoveJobCommand : Microsoft.PowerShell.Commands.JobCmdletBase, System.IDisposable {
    // Constructors
    public RemoveJobCommand() { }
     
    // Properties
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
     
    // Methods
    public void Dispose() { }
    protected void Dispose(bool disposing) { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Remove", "Module", SupportsShouldProcess=true, HelpUri="http://go.microsoft.com/fwlink/?LinkID=141556")]
  public sealed partial class RemoveModuleCommand : Microsoft.PowerShell.Commands.ModuleCmdletBase {
    // Constructors
    public RemoveModuleCommand() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="ModuleInfo", ValueFromPipeline=true, Position=0)]
    public System.Management.Automation.PSModuleInfo[] ModuleInfo { get { return default(System.Management.Automation.PSModuleInfo[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ParameterSetName="name", ValueFromPipeline=true, Position=0)]
    public string[] Name { get { return default(string[]); } set { } }
     
    // Methods
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Remove", "PSSession", SupportsShouldProcess=true, DefaultParameterSetName="Id", HelpUri="http://go.microsoft.com/fwlink/?LinkID=135250", RemotingCapability=(System.Management.Automation.RemotingCapability)(3))]
  public partial class RemovePSSessionCommand : Microsoft.PowerShell.Commands.PSRunspaceCmdlet {
    // Constructors
    public RemovePSSessionCommand() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true, ParameterSetName="Session")]
    public System.Management.Automation.Runspaces.PSSession[] Session { get { return default(System.Management.Automation.Runspaces.PSSession[]); } set { } }
     
    // Methods
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Remove", "PSSnapin", SupportsShouldProcess=true, HelpUri="http://go.microsoft.com/fwlink/?LinkID=113378")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.PSSnapInInfo)})]
  public sealed partial class RemovePSSnapinCommand : Microsoft.PowerShell.Commands.PSSnapInCommandBase {
    // Constructors
    public RemovePSSnapinCommand() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute(Position=0, Mandatory=true, ValueFromPipelineByPropertyName=true)]
    public string[] Name { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter PassThru { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    // Methods
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Resume", "Job", SupportsShouldProcess=true, DefaultParameterSetName="SessionIdParameterSet", HelpUri="http://go.microsoft.com/fwlink/?LinkID=210611")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.Job)})]
  public partial class ResumeJobCommand : Microsoft.PowerShell.Commands.JobCmdletBase, System.IDisposable {
    // Constructors
    public ResumeJobCommand() { }
     
    // Properties
    public override string[] Command { get { return default(string[]); } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true, ParameterSetName="JobParameterSet")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Management.Automation.Job[] Job { get { return default(System.Management.Automation.Job[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="__AllParameterSets")]
    public System.Management.Automation.SwitchParameter Wait { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.SwitchParameter); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
     
    // Methods
    public void Dispose() { }
    protected void Dispose(bool disposing) { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Save", "Help", DefaultParameterSetName="Path", HelpUri="http://go.microsoft.com/fwlink/?LinkID=210612")]
  public sealed partial class SaveHelpCommand : Microsoft.PowerShell.Commands.UpdatableHelpCommandBase {
    // Constructors
    public SaveHelpCommand() { }
     
    // Properties
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
     
    // Methods
    protected override void BeginProcessing() { }
    protected override void ProcessRecord() { }
  }
  public enum SessionFilterState {
    // Fields
    All = 0,
    Broken = 4,
    Closed = 3,
    Disconnected = 2,
    Opened = 1,
  }
  public abstract partial class SessionStateProviderBase : System.Management.Automation.Provider.ContainerCmdletProvider, System.Management.Automation.Provider.IContentCmdletProvider {
    // Constructors
    protected SessionStateProviderBase() { }
     
    // Methods
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
  public partial class SessionStateProviderBaseContentReaderWriter : System.IDisposable, System.Management.Automation.Provider.IContentReader, System.Management.Automation.Provider.IContentWriter {
    internal SessionStateProviderBaseContentReaderWriter() { }
    // Methods
    public void Close() { }
    public void Dispose() { }
    public System.Collections.IList Read(long readCount) { return default(System.Collections.IList); }
    public void Seek(long offset, System.IO.SeekOrigin origin) { }
    public System.Collections.IList Write(System.Collections.IList content) { return default(System.Collections.IList); }
  }
  [System.Management.Automation.CmdletAttribute("Set", "PSDebug", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113398")]
  public sealed partial class SetPSDebugCommand : System.Management.Automation.PSCmdlet {
    // Constructors
    public SetPSDebugCommand() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute(ParameterSetName="off")]
    public System.Management.Automation.SwitchParameter Off { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="on")]
    public System.Management.Automation.SwitchParameter Step { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="on")]
    public System.Management.Automation.SwitchParameter Strict { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName="on")]
    [System.Management.Automation.ValidateRangeAttribute(0, 2)]
    public int Trace { get { return default(int); } set { } }
     
    // Methods
    protected override void BeginProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Set", "PSSessionConfiguration", DefaultParameterSetName="NameParameterSet", SupportsShouldProcess=true, ConfirmImpact=(System.Management.Automation.ConfirmImpact)(3), HelpUri="http://go.microsoft.com/fwlink/?LinkID=144307")]
  public sealed partial class SetPSSessionConfigurationCommand : Microsoft.PowerShell.Commands.PSSessionConfigurationCommandBase {
    // Constructors
    public SetPSSessionConfigurationCommand() { }
     
    // Methods
    protected override void BeginProcessing() { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Set", "StrictMode", DefaultParameterSetName="Version", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113450")]
  public partial class SetStrictModeCommand : System.Management.Automation.PSCmdlet {
    // Constructors
    public SetStrictModeCommand() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Off", Mandatory=true)]
    public System.Management.Automation.SwitchParameter Off { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    //Internal: [Microsoft.PowerShell.Commands.SetStrictModeCommand.ArgumentToVersionTransformationAttribute]
    //Internal: [Microsoft.PowerShell.Commands.SetStrictModeCommand.ValidateVersionAttribute]
    [System.Management.Automation.AliasAttribute(new string[]{ "v"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName="Version", Mandatory=true)]
    public System.Version Version { get { return default(System.Version); } set { } }
     
    // Methods
    protected override void EndProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Start", "Job", DefaultParameterSetName="ComputerName", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113405")]
  //Internal: [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.PSRemotingJob)})]
  public partial class StartJobCommand : Microsoft.PowerShell.Commands.PSExecutionCmdlet, System.IDisposable {
    // Constructors
    public StartJobCommand() { }
     
    // Properties
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
     
    // Methods
    protected override void BeginProcessing() { }
    protected override void CreateHelpersForSpecifiedComputerNames() { }
    public void Dispose() { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Stop", "Job", SupportsShouldProcess=true, DefaultParameterSetName="SessionIdParameterSet", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113413")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.Job)})]
  public partial class StopJobCommand : Microsoft.PowerShell.Commands.JobCmdletBase, System.IDisposable {
    // Constructors
    public StopJobCommand() { }
     
    // Properties
    public override string[] Command { get { return default(string[]); } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true, ParameterSetName="JobParameterSet")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Management.Automation.Job[] Job { get { return default(System.Management.Automation.Job[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter PassThru { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    // Methods
    public void Dispose() { }
    protected void Dispose(bool disposing) { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Suspend", "Job", SupportsShouldProcess=true, DefaultParameterSetName="SessionIdParameterSet", HelpUri="http://go.microsoft.com/fwlink/?LinkID=210613")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.Job)})]
  public partial class SuspendJobCommand : Microsoft.PowerShell.Commands.JobCmdletBase, System.IDisposable {
    // Constructors
    public SuspendJobCommand() { }
     
    // Properties
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
     
    // Methods
    public void Dispose() { }
    protected void Dispose(bool disposing) { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Test", "ModuleManifest", HelpUri="http://go.microsoft.com/fwlink/?LinkID=141557")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.PSModuleInfo)})]
  public sealed partial class TestModuleManifestCommand : Microsoft.PowerShell.Commands.ModuleCmdletBase {
    // Constructors
    public TestModuleManifestCommand() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ValueFromPipeline=true, Position=0, ValueFromPipelineByPropertyName=true)]
    public string Path { get { return default(string); } set { } }
     
    // Methods
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Test", "PSSessionConfigurationFile", HelpUri="http://go.microsoft.com/fwlink/?LinkID=217039")]
  [System.Management.Automation.OutputTypeAttribute(new string[]{ "bool"})]
  public partial class TestPSSessionConfigurationFileCommand : System.Management.Automation.PSCmdlet {
    // Constructors
    public TestPSSessionConfigurationFileCommand() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute(Mandatory=true, ValueFromPipeline=true, Position=0, ValueFromPipelineByPropertyName=true)]
    public string Path { get { return default(string); } set { } }
     
    // Methods
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.CmdletAttribute("Unregister", "PSSessionConfiguration", SupportsShouldProcess=true, ConfirmImpact=(System.Management.Automation.ConfirmImpact)(3), HelpUri="http://go.microsoft.com/fwlink/?LinkID=144308")]
  public sealed partial class UnregisterPSSessionConfigurationCommand : System.Management.Automation.PSCmdlet {
    // Constructors
    public UnregisterPSSessionConfigurationCommand() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=true, Position=0, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string Name { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter NoServiceRestart { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    // Methods
    protected override void BeginProcessing() { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
  }
  public partial class UpdatableHelpCommandBase : System.Management.Automation.PSCmdlet {
    internal UpdatableHelpCommandBase() { }
    // Properties
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
     
    // Methods
    protected override void EndProcessing() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Update", "Help", DefaultParameterSetName="Path", HelpUri="http://go.microsoft.com/fwlink/?LinkID=210614")]
  public sealed partial class UpdateHelpCommand : Microsoft.PowerShell.Commands.UpdatableHelpCommandBase {
    // Constructors
    public UpdateHelpCommand() { }
     
    // Properties
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
     
    // Methods
    protected override void BeginProcessing() { }
    protected override void ProcessRecord() { }
  }
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.PSVariable)}, ProviderCmdlet="Copy-Item")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.PSVariable)}, ProviderCmdlet="Get-Item")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.PSVariable)}, ProviderCmdlet="New-Item")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.PSVariable)}, ProviderCmdlet="Rename-Item")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.PSVariable)}, ProviderCmdlet="Set-Item")]
  [System.Management.Automation.Provider.CmdletProviderAttribute("Variable", (System.Management.Automation.Provider.ProviderCapabilities)(16))]
  public sealed partial class VariableProvider : Microsoft.PowerShell.Commands.SessionStateProviderBase {
    // Fields
    public const string ProviderName = "Variable";
     
    // Constructors
    public VariableProvider() { }
     
    // Methods
    protected override System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo> InitializeDefaultDrives() { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo>); }
  }
  [System.Management.Automation.CmdletAttribute("Wait", "Job", DefaultParameterSetName="SessionIdParameterSet", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113422")]
  [System.Management.Automation.OutputTypeAttribute(new System.Type[]{ typeof(System.Management.Automation.Job)})]
  public partial class WaitJobCommand : Microsoft.PowerShell.Commands.JobCmdletBase, System.IDisposable {
    // Constructors
    public WaitJobCommand() { }
     
    // Properties
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
     
    // Methods
    protected override void BeginProcessing() { }
    public void Dispose() { }
    protected override void EndProcessing() { }
    protected override void ProcessRecord() { }
    protected override void StopProcessing() { }
  }
  [System.Management.Automation.CmdletAttribute("Where", "Object", DefaultParameterSetName="EqualSet", HelpUri="http://go.microsoft.com/fwlink/?LinkID=113423", RemotingCapability=(System.Management.Automation.RemotingCapability)(0))]
  public sealed partial class WhereObjectCommand : System.Management.Automation.PSCmdlet {
    // Constructors
    public WhereObjectCommand() { }
     
    // Properties
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
     
    // Methods
    protected override void BeginProcessing() { }
    protected override void ProcessRecord() { }
  }
  public partial class WSManConfigurationOption : System.Management.Automation.PSTransportOption {
    internal WSManConfigurationOption() { }
    // Properties
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
     
    // Methods
    protected internal override void LoadFromDefaults(System.Management.Automation.Runspaces.PSSessionType sessionType, bool keepAssigned) { }
  }
}
namespace Microsoft.PowerShell.Commands.Internal {
#if SYSTEM_SECURITY
  public sealed partial class TransactedRegistryAccessRule : System.Security.AccessControl.AccessRule {
    // Constructors
    public TransactedRegistryAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.RegistryRights registryRights, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AccessControlType type) { }
     
    // Properties
    public System.Security.AccessControl.RegistryRights RegistryRights { get { return default(System.Security.AccessControl.RegistryRights); } }
     
    // Methods
  }
  public sealed partial class TransactedRegistryAuditRule : System.Security.AccessControl.AuditRule {
    internal TransactedRegistryAuditRule() { }
    // Properties
    public System.Security.AccessControl.RegistryRights RegistryRights { get { return default(System.Security.AccessControl.RegistryRights); } }
     
    // Methods
  }
#endif
#if WIN32_REGISTRY
  [System.Runtime.InteropServices.ComVisibleAttribute(true)]
  public sealed partial class TransactedRegistryKey : System.MarshalByRefObject, System.IDisposable {
    internal TransactedRegistryKey() { }
    // Properties
    public string Name { get { return default(string); } }
    public int SubKeyCount { get { return default(int); } }
    public int ValueCount { get { return default(int); } }
     
    // Methods
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
  public sealed partial class TransactedRegistrySecurity : System.Security.AccessControl.NativeObjectSecurity {
    // Constructors
    public TransactedRegistrySecurity() { }
     
    // Properties
    public override System.Type AccessRightType { get { return default(System.Type); } }
    public override System.Type AccessRuleType { get { return default(System.Type); } }
    public override System.Type AuditRuleType { get { return default(System.Type); } }
     
    // Methods
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
  public abstract partial class FrontEndCommandBase : System.Management.Automation.PSCmdlet, System.IDisposable {
    // Constructors
    protected FrontEndCommandBase() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute(ValueFromPipeline=true)]
    public System.Management.Automation.PSObject InputObject { get { return default(System.Management.Automation.PSObject); } set { } }
     
    // Methods
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
  public partial class OuterFormatShapeCommandBase : Microsoft.PowerShell.Commands.Internal.Format.FrontEndCommandBase {
    // Constructors
    public OuterFormatShapeCommandBase() { }
     
    // Properties
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
     
    // Methods
    protected override void BeginProcessing() { }
  }
  public partial class OuterFormatTableAndListBase : Microsoft.PowerShell.Commands.Internal.Format.OuterFormatShapeCommandBase {
    // Constructors
    public OuterFormatTableAndListBase() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute(Position=0)]
    public object[] Property { get { return default(object[]); } set { } }
     
    // Methods
  }
  public partial class OuterFormatTableBase : Microsoft.PowerShell.Commands.Internal.Format.OuterFormatTableAndListBase {
    // Constructors
    public OuterFormatTableBase() { }
     
    // Properties
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter AutoSize { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter HideTableHeaders { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Wrap { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    // Methods
  }
}
namespace Microsoft.PowerShell.Commands.Management {
#if TRANSACTIONS
  public partial class TransactedString : System.Transactions.IEnlistmentNotification {
    // Constructors
    public TransactedString() { }
    public TransactedString(string value) { }
     
    // Properties
    public int Length { get { return default(int); } }
     
    // Methods
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
    // Fields
    Continue = 2,
    Ignore = 4,
    Inquire = 3,
    SilentlyContinue = 0,
    Stop = 1,
  }
  public partial class ActionPreferenceStopException : System.Management.Automation.RuntimeException {
    // Constructors
    public ActionPreferenceStopException() { }
#if RUNTIME_SERIALIZATION
    protected ActionPreferenceStopException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public ActionPreferenceStopException(string message) { }
    public ActionPreferenceStopException(string message, System.Exception innerException) { }
     
    // Properties
    public override System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
     
#if RUNTIME_SERIALIZATION
    // Methods
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
  }
  [System.AttributeUsageAttribute((AttributeTargets)388, AllowMultiple=false)]
  public sealed partial class AliasAttribute : System.Management.Automation.Internal.ParsingBaseAttribute {
    // Constructors
    public AliasAttribute(params string[] aliasNames) { }
     
    // Properties
    public System.Collections.Generic.IList<string> AliasNames { get { return default(System.Collections.Generic.IList<string>); } }
     
    // Methods
  }
  public partial class AliasInfo : System.Management.Automation.CommandInfo {
    internal AliasInfo() { }
    // Properties
    public override string Definition { get { return default(string); } }
    public string Description { get { return default(string); } set { } }
    public System.Management.Automation.ScopedItemOptions Options { get { return default(System.Management.Automation.ScopedItemOptions); } set { } }
    public override System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName> OutputType { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName>); } }
    public System.Management.Automation.CommandInfo ReferencedCommand { get { return default(System.Management.Automation.CommandInfo); } }
    public System.Management.Automation.CommandInfo ResolvedCommand { get { return default(System.Management.Automation.CommandInfo); } }
     
    // Methods
  }
  public enum Alignment {
    // Fields
    Center = 2,
    Left = 1,
    Right = 3,
    Undefined = 0,
  }
  [System.AttributeUsageAttribute((AttributeTargets)384)]
  public sealed partial class AllowEmptyCollectionAttribute : System.Management.Automation.Internal.CmdletMetadataAttribute {
    // Constructors
    public AllowEmptyCollectionAttribute() { }
  }
  [System.AttributeUsageAttribute((AttributeTargets)384)]
  public sealed partial class AllowEmptyStringAttribute : System.Management.Automation.Internal.CmdletMetadataAttribute {
    // Constructors
    public AllowEmptyStringAttribute() { }
  }
  [System.AttributeUsageAttribute((AttributeTargets)384)]
  public sealed partial class AllowNullAttribute : System.Management.Automation.Internal.CmdletMetadataAttribute {
    // Constructors
    public AllowNullAttribute() { }
  }
  public partial class ApplicationFailedException : System.Management.Automation.RuntimeException {
    // Constructors
    public ApplicationFailedException() { }
#if RUNTIME_SERIALIZATION
    protected ApplicationFailedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public ApplicationFailedException(string message) { }
    public ApplicationFailedException(string message, System.Exception innerException) { }
  }
  public partial class ApplicationInfo : System.Management.Automation.CommandInfo {
    internal ApplicationInfo() { }
    // Properties
    public override string Definition { get { return default(string); } }
    public string Extension { get { return default(string); } }
    public override System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName> OutputType { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName>); } }
    public string Path { get { return default(string); } }
    public override System.Management.Automation.SessionStateEntryVisibility Visibility { get { return default(System.Management.Automation.SessionStateEntryVisibility); } set { } }
     
    // Methods
  }
  [System.AttributeUsageAttribute((AttributeTargets)384)]
  public abstract partial class ArgumentTransformationAttribute : System.Management.Automation.Internal.CmdletMetadataAttribute {
    // Constructors
    protected ArgumentTransformationAttribute() { }
     
    // Methods
    public abstract object Transform(System.Management.Automation.EngineIntrinsics engineIntrinsics, object inputData);
  }
  public partial class ArgumentTransformationMetadataException : System.Management.Automation.MetadataException {
    // Constructors
    public ArgumentTransformationMetadataException() { }
#if RUNTIME_SERIALIZATION
    protected ArgumentTransformationMetadataException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public ArgumentTransformationMetadataException(string message) { }
    public ArgumentTransformationMetadataException(string message, System.Exception innerException) { }
  }
  public partial class AuthorizationManager {
    // Constructors
    public AuthorizationManager(string shellId) { }
     
    // Methods
    protected internal virtual bool ShouldRun(System.Management.Automation.CommandInfo commandInfo, System.Management.Automation.CommandOrigin origin, System.Management.Automation.Host.PSHost host, out System.Exception reason) { reason = default(System.Exception); return default(bool); }
  }
#if SYSTEM_DIAGNOSTICS
  public partial class BackgroundDispatcher : System.Management.Automation.IBackgroundDispatcher {
    // Constructors
    public BackgroundDispatcher(System.Diagnostics.Eventing.EventProvider transferProvider, System.Diagnostics.Eventing.EventDescriptor transferEvent) { }
     
    // Methods
    public System.IAsyncResult BeginInvoke(System.Threading.WaitCallback callback, object state, System.AsyncCallback completionCallback, object asyncState) { return default(System.IAsyncResult); }
    public void EndInvoke(System.IAsyncResult asyncResult) { }
    public bool QueueUserWorkItem(System.Threading.WaitCallback callback) { return default(bool); }
    public bool QueueUserWorkItem(System.Threading.WaitCallback callback, object state) { return default(bool); }
  }
#endif
  public abstract partial class Breakpoint {
    internal Breakpoint() { }
    // Properties
    public System.Management.Automation.ScriptBlock Action { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.ScriptBlock); } }
    public bool Enabled { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } }
    public int HitCount { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(int); } }
    public int Id { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(int); } }
    public string Script { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
     
    // Methods
  }
  public partial class BreakpointUpdatedEventArgs : System.EventArgs {
    internal BreakpointUpdatedEventArgs() { }
    // Properties
    public System.Management.Automation.Breakpoint Breakpoint { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Breakpoint); } }
    public System.Management.Automation.BreakpointUpdateType UpdateType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.BreakpointUpdateType); } }
     
    // Methods
  }
  public enum BreakpointUpdateType {
    // Fields
    Disabled = 3,
    Enabled = 2,
    Removed = 1,
    Set = 0,
  }
  public sealed partial class CallStackFrame {
    internal CallStackFrame() { }
    // Properties
    public string FunctionName { get { return default(string); } }
    public System.Management.Automation.InvocationInfo InvocationInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.InvocationInfo); } }
    public System.Management.Automation.Language.IScriptExtent Position { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.IScriptExtent); } }
    public int ScriptLineNumber { get { return default(int); } }
    public string ScriptName { get { return default(string); } }
     
    // Methods
    public System.Collections.Generic.Dictionary<string, System.Management.Automation.PSVariable> GetFrameVariables() { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.PSVariable>); }
    public string GetScriptLocation() { return default(string); }
    public override string ToString() { return default(string); }
  }
  public sealed partial class ChildItemCmdletProviderIntrinsics {
    internal ChildItemCmdletProviderIntrinsics() { }
    // Methods
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Get(string path, bool recurse) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Get(string[] path, bool recurse, bool force, bool literalPath) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<string> GetNames(string path, System.Management.Automation.ReturnContainers returnContainers, bool recurse) { return default(System.Collections.ObjectModel.Collection<string>); }
    public System.Collections.ObjectModel.Collection<string> GetNames(string[] path, System.Management.Automation.ReturnContainers returnContainers, bool recurse, bool force, bool literalPath) { return default(System.Collections.ObjectModel.Collection<string>); }
    public bool HasChild(string path) { return default(bool); }
    public bool HasChild(string path, bool force, bool literalPath) { return default(bool); }
  }
  public abstract partial class Cmdlet : System.Management.Automation.Internal.InternalCommand {
    // Constructors
    protected Cmdlet() { }
     
    // Properties
    public System.Management.Automation.ICommandRuntime CommandRuntime { get { return default(System.Management.Automation.ICommandRuntime); } set { } }
    public System.Management.Automation.PSTransactionContext CurrentPSTransaction { get { return default(System.Management.Automation.PSTransactionContext); } }
    public bool Stopping { get { return default(bool); } }
     
    // Methods
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
  public sealed partial class CmdletAttribute : System.Management.Automation.CmdletCommonMetadataAttribute {
    // Constructors
    public CmdletAttribute(string verbName, string nounName) { }
     
    // Properties
    public string NounName { get { return default(string); } }
    public string VerbName { get { return default(string); } }
     
    // Methods
  }
  [System.AttributeUsageAttribute((AttributeTargets)4)]
  public partial class CmdletBindingAttribute : System.Management.Automation.CmdletCommonMetadataAttribute {
    // Constructors
    public CmdletBindingAttribute() { }
     
    // Properties
    public bool PositionalBinding { get { return default(bool); } set { } }
     
    // Methods
  }
  [System.AttributeUsageAttribute((AttributeTargets)4)]
  public abstract partial class CmdletCommonMetadataAttribute : System.Management.Automation.Internal.CmdletMetadataAttribute {
    // Constructors
    protected CmdletCommonMetadataAttribute() { }
     
    // Properties
    public System.Management.Automation.ConfirmImpact ConfirmImpact { get { return default(System.Management.Automation.ConfirmImpact); } set { } }
    public string DefaultParameterSetName { get { return default(string); } set { } }
    public string HelpUri { get { return default(string); } set { } }
    public System.Management.Automation.RemotingCapability RemotingCapability { get { return default(System.Management.Automation.RemotingCapability); } set { } }
    public bool SupportsPaging { get { return default(bool); } set { } }
    public bool SupportsShouldProcess { get { return default(bool); } set { } }
    public bool SupportsTransactions { get { return default(bool); } set { } }
     
    // Methods
  }
  public partial class CmdletInfo : System.Management.Automation.CommandInfo {
    // Constructors
    public CmdletInfo(string name, System.Type implementingType) { }
     
    // Properties
    public string DefaultParameterSet { get { return default(string); } }
    public override string Definition { get { return default(string); } }
    public string HelpFile { get { return default(string); } }
    public System.Type ImplementingType { get { return default(System.Type); } }
    public string Noun { get { return default(string); } }
    public System.Management.Automation.ScopedItemOptions Options { get { return default(System.Management.Automation.ScopedItemOptions); } set { } }
    public override System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName> OutputType { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName>); } }
    public System.Management.Automation.PSSnapInInfo PSSnapIn { get { return default(System.Management.Automation.PSSnapInInfo); } }
    public string Verb { get { return default(string); } }
     
    // Methods
  }
  public partial class CmdletInvocationException : System.Management.Automation.RuntimeException {
    // Constructors
    public CmdletInvocationException() { }
#if RUNTIME_SERIALIZATION
    protected CmdletInvocationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public CmdletInvocationException(string message) { }
    public CmdletInvocationException(string message, System.Exception innerException) { }
     
    // Properties
    public override System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
     
#if RUNTIME_SERIALIZATION
    // Methods
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
  }
  public partial class CmdletProviderInvocationException : System.Management.Automation.CmdletInvocationException {
    // Constructors
    public CmdletProviderInvocationException() { }
#if RUNTIME_SERIALIZATION
    protected CmdletProviderInvocationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public CmdletProviderInvocationException(string message) { }
    public CmdletProviderInvocationException(string message, System.Exception innerException) { }
     
    // Properties
    public System.Management.Automation.ProviderInfo ProviderInfo { get { return default(System.Management.Automation.ProviderInfo); } }
    public System.Management.Automation.ProviderInvocationException ProviderInvocationException { get { return default(System.Management.Automation.ProviderInvocationException); } }
     
    // Methods
  }
  public sealed partial class CmdletProviderManagementIntrinsics {
    internal CmdletProviderManagementIntrinsics() { }
    // Methods
    public System.Collections.ObjectModel.Collection<System.Management.Automation.ProviderInfo> Get(string name) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.ProviderInfo>); }
    public System.Collections.Generic.IEnumerable<System.Management.Automation.ProviderInfo> GetAll() { return default(System.Collections.Generic.IEnumerable<System.Management.Automation.ProviderInfo>); }
    public System.Management.Automation.ProviderInfo GetOne(string name) { return default(System.Management.Automation.ProviderInfo); }
  }
  public partial class CommandBreakpoint : System.Management.Automation.Breakpoint {
    internal CommandBreakpoint() { }
    // Properties
    public string Command { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
     
    // Methods
    public override string ToString() { return default(string); }
  }
  public partial class CommandCompletion {
    internal CommandCompletion() { }
    // Properties
    public System.Collections.ObjectModel.Collection<System.Management.Automation.CompletionResult> CompletionMatches { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.CompletionResult>); } }
    public int CurrentMatchIndex { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(int); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public int ReplacementIndex { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(int); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public int ReplacementLength { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(int); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
     
    // Methods
    public static System.Management.Automation.CommandCompletion CompleteInput(System.Management.Automation.Language.Ast ast, System.Management.Automation.Language.Token[] tokens, System.Management.Automation.Language.IScriptPosition positionOfCursor, System.Collections.Hashtable options) { return default(System.Management.Automation.CommandCompletion); }
    public static System.Management.Automation.CommandCompletion CompleteInput(System.Management.Automation.Language.Ast ast, System.Management.Automation.Language.Token[] tokens, System.Management.Automation.Language.IScriptPosition cursorPosition, System.Collections.Hashtable options, System.Management.Automation.PowerShell powershell) { return default(System.Management.Automation.CommandCompletion); }
    public static System.Management.Automation.CommandCompletion CompleteInput(string input, int cursorIndex, System.Collections.Hashtable options) { return default(System.Management.Automation.CommandCompletion); }
    public static System.Management.Automation.CommandCompletion CompleteInput(string input, int cursorIndex, System.Collections.Hashtable options, System.Management.Automation.PowerShell powershell) { return default(System.Management.Automation.CommandCompletion); }
    public System.Management.Automation.CompletionResult GetNextResult(bool forward) { return default(System.Management.Automation.CompletionResult); }
    public static System.Tuple<System.Management.Automation.Language.Ast, System.Management.Automation.Language.Token[], System.Management.Automation.Language.IScriptPosition> MapStringInputToParsedInput(string input, int cursorIndex) { return default(System.Tuple<System.Management.Automation.Language.Ast, System.Management.Automation.Language.Token[], System.Management.Automation.Language.IScriptPosition>); }
  }
  public abstract partial class CommandInfo {
    internal CommandInfo() { }
    // Properties
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
     
    // Methods
    public System.Management.Automation.ParameterMetadata ResolveParameter(string name) { return default(System.Management.Automation.ParameterMetadata); }
    public override string ToString() { return default(string); }
  }
  public partial class CommandInvocationIntrinsics {
    internal CommandInvocationIntrinsics() { }
    // Properties
    public System.EventHandler<System.Management.Automation.CommandLookupEventArgs> CommandNotFoundAction { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.EventHandler<System.Management.Automation.CommandLookupEventArgs>); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public bool HasErrors { get { return default(bool); } set { } }
    public System.EventHandler<System.Management.Automation.CommandLookupEventArgs> PostCommandLookupAction { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.EventHandler<System.Management.Automation.CommandLookupEventArgs>); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public System.EventHandler<System.Management.Automation.CommandLookupEventArgs> PreCommandLookupAction { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.EventHandler<System.Management.Automation.CommandLookupEventArgs>); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
     
    // Methods
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
  public partial class CommandLookupEventArgs : System.EventArgs {
    internal CommandLookupEventArgs() { }
    // Properties
    public System.Management.Automation.CommandInfo Command { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.CommandInfo); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public string CommandName { get { return default(string); } }
    public System.Management.Automation.CommandOrigin CommandOrigin { get { return default(System.Management.Automation.CommandOrigin); } }
    public System.Management.Automation.ScriptBlock CommandScriptBlock { get { return default(System.Management.Automation.ScriptBlock); } set { } }
    public bool StopSearch { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
     
    // Methods
  }
  [System.Diagnostics.DebuggerDisplayAttribute("CommandName = {_commandName}; Type = {CommandType}")]
  public sealed partial class CommandMetadata {
    // Constructors
    public CommandMetadata(System.Management.Automation.CommandInfo commandInfo) { }
    public CommandMetadata(System.Management.Automation.CommandInfo commandInfo, bool shouldGenerateCommonParameters) { }
    public CommandMetadata(System.Management.Automation.CommandMetadata other) { }
    public CommandMetadata(string path) { }
    public CommandMetadata(System.Type commandType) { }
     
    // Properties
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
     
    // Methods
    public static System.Collections.Generic.Dictionary<string, System.Management.Automation.CommandMetadata> GetRestrictedCommands(System.Management.Automation.SessionCapabilities sessionCapabilities) { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.CommandMetadata>); }
  }
  public partial class CommandNotFoundException : System.Management.Automation.RuntimeException {
    // Constructors
    public CommandNotFoundException() { }
#if RUNTIME_SERIALIZATION
    protected CommandNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public CommandNotFoundException(string message) { }
    public CommandNotFoundException(string message, System.Exception innerException) { }
     
    // Properties
    public string CommandName { get { return default(string); } set { } }
    public override System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
     
#if RUNTIME_SERIALIZATION
    // Methods
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
  }
  public enum CommandOrigin {
    // Fields
    Internal = 1,
    Runspace = 0,
  }
  public partial class CommandParameterInfo {
    internal CommandParameterInfo() { }
    // Properties
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
     
    // Methods
  }
  public partial class CommandParameterSetInfo {
    internal CommandParameterSetInfo() { }
    // Properties
    public bool IsDefault { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } }
    public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.CommandParameterInfo> Parameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.CommandParameterInfo>); } }
     
    // Methods
    public override string ToString() { return default(string); }
  }
  [System.FlagsAttribute]
  public enum CommandTypes {
    // Fields
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
  public static partial class CompletionCompleters {
    // Methods
    public static System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult> CompleteCommand(string commandName) { return default(System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult>); }
    public static System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult> CompleteCommand(string commandName, string moduleName, System.Management.Automation.CommandTypes commandTypes=(System.Management.Automation.CommandTypes)(255)) { return default(System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult>); }
    public static System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult> CompleteFilename(string fileName) { return default(System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult>); }
    public static System.Collections.Generic.List<System.Management.Automation.CompletionResult> CompleteOperator(string wordToComplete) { return default(System.Collections.Generic.List<System.Management.Automation.CompletionResult>); }
    public static System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult> CompleteType(string typeName) { return default(System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult>); }
    public static System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult> CompleteVariable(string variableName) { return default(System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult>); }
  }
  public partial class CompletionResult {
    // Constructors
    public CompletionResult(string completionText) { }
    public CompletionResult(string completionText, string listItemText, System.Management.Automation.CompletionResultType resultType, string toolTip) { }
     
    // Properties
    public string CompletionText { get { return default(string); } }
    public string ListItemText { get { return default(string); } }
    public System.Management.Automation.CompletionResultType ResultType { get { return default(System.Management.Automation.CompletionResultType); } }
    public string ToolTip { get { return default(string); } }
     
    // Methods
  }
  public enum CompletionResultType {
    // Fields
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
    // Fields
    High = 3,
    Low = 1,
    Medium = 2,
    None = 0,
  }
  public sealed partial class ContainerParentJob : System.Management.Automation.Job2 {
    // Constructors
    public ContainerParentJob(string command) { }
    public ContainerParentJob(string command, string name) { }
    public ContainerParentJob(string command, string name, System.Guid instanceId) { }
    public ContainerParentJob(string command, string name, System.Guid instanceId, string jobType) { }
    public ContainerParentJob(string command, string name, System.Management.Automation.JobIdentifier jobId) { }
    public ContainerParentJob(string command, string name, System.Management.Automation.JobIdentifier jobId, string jobType) { }
    public ContainerParentJob(string command, string name, string jobType) { }
     
    // Properties
    public override bool HasMoreData { get { return default(bool); } }
    public override string Location { get { return default(string); } }
    public override string StatusMessage { get { return default(string); } }
     
    // Methods
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
  public sealed partial class ContentCmdletProviderIntrinsics {
    internal ContentCmdletProviderIntrinsics() { }
    // Methods
    public void Clear(string path) { }
    public void Clear(string[] path, bool force, bool literalPath) { }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.Provider.IContentReader> GetReader(string path) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.Provider.IContentReader>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.Provider.IContentReader> GetReader(string[] path, bool force, bool literalPath) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.Provider.IContentReader>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.Provider.IContentWriter> GetWriter(string path) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.Provider.IContentWriter>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.Provider.IContentWriter> GetWriter(string[] path, bool force, bool literalPath) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.Provider.IContentWriter>); }
  }
  public partial class ConvertThroughString : System.Management.Automation.PSTypeConverter {
    // Constructors
    public ConvertThroughString() { }
     
    // Methods
    public override bool CanConvertFrom(object sourceValue, System.Type destinationType) { return default(bool); }
    public override bool CanConvertTo(object sourceValue, System.Type destinationType) { return default(bool); }
    public override object ConvertFrom(object sourceValue, System.Type destinationType, System.IFormatProvider formatProvider, bool ignoreCase) { return default(object); }
    public override object ConvertTo(object sourceValue, System.Type destinationType, System.IFormatProvider formatProvider, bool ignoreCase) { return default(object); }
  }
  public enum CopyContainers {
    // Fields
    CopyChildrenOfTargetContainer = 1,
    CopyTargetContainer = 0,
  }
  [System.AttributeUsageAttribute((AttributeTargets)384, AllowMultiple=false)]
  public sealed partial class CredentialAttribute : System.Management.Automation.ArgumentTransformationAttribute {
    // Constructors
    public CredentialAttribute() { }
     
    // Methods
    public override object Transform(System.Management.Automation.EngineIntrinsics engineIntrinsics, object inputData) { return default(object); }
  }
#if SNAPINS
  public abstract partial class CustomPSSnapIn : System.Management.Automation.PSSnapInInstaller {
    // Constructors
    protected CustomPSSnapIn() { }
     
    // Properties
    public virtual System.Collections.ObjectModel.Collection<System.Management.Automation.Runspaces.CmdletConfigurationEntry> Cmdlets { get { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.Runspaces.CmdletConfigurationEntry>); } }
    public virtual System.Collections.ObjectModel.Collection<System.Management.Automation.Runspaces.FormatConfigurationEntry> Formats { get { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.Runspaces.FormatConfigurationEntry>); } }
    public virtual System.Collections.ObjectModel.Collection<System.Management.Automation.Runspaces.ProviderConfigurationEntry> Providers { get { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.Runspaces.ProviderConfigurationEntry>); } }
    public virtual System.Collections.ObjectModel.Collection<System.Management.Automation.Runspaces.TypeConfigurationEntry> Types { get { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.Runspaces.TypeConfigurationEntry>); } }
     
    // Methods
  }
#endif
  public sealed partial class DataAddedEventArgs : System.EventArgs {
    internal DataAddedEventArgs() { }
    // Properties
    public int Index { get { return default(int); } }
    public System.Guid PowerShellInstanceId { get { return default(System.Guid); } }
     
    // Methods
  }
  public sealed partial class DataAddingEventArgs : System.EventArgs {
    internal DataAddingEventArgs() { }
    // Properties
    public object ItemAdded { get { return default(object); } }
    public System.Guid PowerShellInstanceId { get { return default(System.Guid); } }
     
    // Methods
  }
  public sealed partial class Debugger {
    internal Debugger() { }
    // Events
    public event System.EventHandler<System.Management.Automation.BreakpointUpdatedEventArgs> BreakpointUpdated { add { } remove { } }
    public event System.EventHandler<System.Management.Automation.DebuggerStopEventArgs> DebuggerStop { add { } remove { } }
     
    // Methods
  }
  public enum DebuggerResumeAction {
    // Fields
    Continue = 0,
    StepInto = 1,
    StepOut = 2,
    StepOver = 3,
    Stop = 4,
  }
  public partial class DebuggerStopEventArgs : System.EventArgs {
    internal DebuggerStopEventArgs() { }
    // Properties
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Breakpoint> Breakpoints { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Breakpoint>); } }
    public System.Management.Automation.InvocationInfo InvocationInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.InvocationInfo); } }
    public System.Management.Automation.DebuggerResumeAction ResumeAction { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.DebuggerResumeAction); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
     
    // Methods
  }
#if RUNTIME_SERIALIZATION
  [System.Runtime.Serialization.DataContractAttribute]
#endif
  public partial class DebugRecord : System.Management.Automation.InformationalRecord {
    // Constructors
    public DebugRecord(System.Management.Automation.PSObject record) { }
    public DebugRecord(string message) { }
  }
  public sealed partial class DefaultParameterDictionary : System.Collections.Hashtable {
    // Constructors
    public DefaultParameterDictionary() { }
    public DefaultParameterDictionary(System.Collections.IDictionary dictionary) { }
     
    // Properties
    public override object this[object key] { get { return default(object); } set { } }
     
    // Methods
    public override void Add(object key, object value) { }
    public bool ChangeSinceLastCheck() { return default(bool); }
    public override void Clear() { }
    public override void Remove(object key) { }
  }
  public sealed partial class DisplayEntry {
    // Constructors
    public DisplayEntry(string value, System.Management.Automation.DisplayEntryValueType type) { }
     
    // Properties
    public string Value { get { return default(string); } }
    public System.Management.Automation.DisplayEntryValueType ValueType { get { return default(System.Management.Automation.DisplayEntryValueType); } }
     
    // Methods
    public override string ToString() { return default(string); }
  }
  public enum DisplayEntryValueType {
    // Fields
    Property = 0,
    ScriptBlock = 1,
  }
  public sealed partial class DriveManagementIntrinsics {
    internal DriveManagementIntrinsics() { }
    // Properties
    public System.Management.Automation.PSDriveInfo Current { get { return default(System.Management.Automation.PSDriveInfo); } }
     
    // Methods
    public System.Management.Automation.PSDriveInfo Get(string driveName) { return default(System.Management.Automation.PSDriveInfo); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo> GetAll() { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo> GetAllAtScope(string scope) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo> GetAllForProvider(string providerName) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo>); }
    public System.Management.Automation.PSDriveInfo GetAtScope(string driveName, string scope) { return default(System.Management.Automation.PSDriveInfo); }
    public System.Management.Automation.PSDriveInfo New(System.Management.Automation.PSDriveInfo drive, string scope) { return default(System.Management.Automation.PSDriveInfo); }
    public void Remove(string driveName, bool force, string scope) { }
  }
  public partial class DriveNotFoundException : System.Management.Automation.SessionStateException {
    // Constructors
    public DriveNotFoundException() { }
#if RUNTIME_SERIALIZATION
    protected DriveNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public DriveNotFoundException(string message) { }
    public DriveNotFoundException(string message, System.Exception innerException) { }
  }
  public partial class EngineIntrinsics {
    internal EngineIntrinsics() { }
    // Properties
    public System.Management.Automation.PSEventManager Events { get { return default(System.Management.Automation.PSEventManager); } }
    public System.Management.Automation.Host.PSHost Host { get { return default(System.Management.Automation.Host.PSHost); } }
    public System.Management.Automation.CommandInvocationIntrinsics InvokeCommand { get { return default(System.Management.Automation.CommandInvocationIntrinsics); } }
    public System.Management.Automation.ProviderIntrinsics InvokeProvider { get { return default(System.Management.Automation.ProviderIntrinsics); } }
    public System.Management.Automation.SessionState SessionState { get { return default(System.Management.Automation.SessionState); } }
     
    // Methods
  }
  public enum ErrorCategory {
    // Fields
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
  public partial class ErrorCategoryInfo {
    internal ErrorCategoryInfo() { }
    // Properties
    public string Activity { get { return default(string); } set { } }
    public System.Management.Automation.ErrorCategory Category { get { return default(System.Management.Automation.ErrorCategory); } }
    public string Reason { get { return default(string); } set { } }
    public string TargetName { get { return default(string); } set { } }
    public string TargetType { get { return default(string); } set { } }
     
    // Methods
    public string GetMessage() { return default(string); }
    public string GetMessage(System.Globalization.CultureInfo uiCultureInfo) { return default(string); }
    public override string ToString() { return default(string); }
  }
  public partial class ErrorDetails : System.Runtime.Serialization.ISerializable {
    // Constructors
    public ErrorDetails(System.Management.Automation.Cmdlet cmdlet, string baseName, string resourceId, params object[] args) { }
    public ErrorDetails(System.Management.Automation.IResourceSupplier resourceSupplier, string baseName, string resourceId, params object[] args) { }
    public ErrorDetails(System.Reflection.Assembly assembly, string baseName, string resourceId, params object[] args) { }
    protected ErrorDetails(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public ErrorDetails(string message) { }
     
    // Properties
    public string Message { get { return default(string); } }
    public string RecommendedAction { get { return default(string); } set { } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
#endif
    public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public override string ToString() { return default(string); }
  }
  public partial class ErrorRecord : System.Runtime.Serialization.ISerializable
  {
    // Constructors
    public ErrorRecord(System.Exception exception, string errorId, System.Management.Automation.ErrorCategory errorCategory, object targetObject) { }
    public ErrorRecord(System.Management.Automation.ErrorRecord errorRecord, System.Exception replaceParentContainsErrorRecordException) { }
    protected ErrorRecord(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
     
    // Properties
    public System.Management.Automation.ErrorCategoryInfo CategoryInfo { get { return default(System.Management.Automation.ErrorCategoryInfo); } }
    public System.Management.Automation.ErrorDetails ErrorDetails { get { return default(System.Management.Automation.ErrorDetails); } set { } }
    public System.Exception Exception { get { return default(System.Exception); } }
    public string FullyQualifiedErrorId { get { return default(string); } }
    public System.Management.Automation.InvocationInfo InvocationInfo { get { return default(System.Management.Automation.InvocationInfo); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<int> PipelineIterationInfo { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<int>); } }
    public string ScriptStackTrace { get { return default(string); } }
    public object TargetObject { get { return default(object); } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
#endif
    public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public override string ToString() { return default(string); }
  }
  public sealed partial class ExtendedTypeDefinition {
    // Constructors
    public ExtendedTypeDefinition(string typeName) { }
    public ExtendedTypeDefinition(string typeName, System.Collections.Generic.IEnumerable<System.Management.Automation.FormatViewDefinition> viewDefinitions) { }
     
    // Properties
    public System.Collections.Generic.List<System.Management.Automation.FormatViewDefinition> FormatViewDefinition { get { return default(System.Collections.Generic.List<System.Management.Automation.FormatViewDefinition>); } }
    public string TypeName { get { return default(string); } }
     
    // Methods
    public override string ToString() { return default(string); }
  }
  public partial class ExtendedTypeSystemException : System.Management.Automation.RuntimeException {
    // Constructors
    public ExtendedTypeSystemException() { }
#if RUNTIME_SERIALIZATION
    protected ExtendedTypeSystemException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public ExtendedTypeSystemException(string message) { }
    public ExtendedTypeSystemException(string message, System.Exception innerException) { }
  }
  public partial class ExternalScriptInfo : System.Management.Automation.CommandInfo {
    internal ExternalScriptInfo() { }
    // Properties
    public override string Definition { get { return default(string); } }
    public System.Text.Encoding OriginalEncoding { get { return default(System.Text.Encoding); } }
    public override System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName> OutputType { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName>); } }
    public string Path { get { return default(string); } }
    public System.Management.Automation.ScriptBlock ScriptBlock { get { return default(System.Management.Automation.ScriptBlock); } }
    public string ScriptContents { get { return default(string); } }
    public override System.Management.Automation.SessionStateEntryVisibility Visibility { get { return default(System.Management.Automation.SessionStateEntryVisibility); } set { } }
     
    // Methods
    public void ValidateScriptInfo(System.Management.Automation.Host.PSHost host) { }
  }
  public partial class FilterInfo : System.Management.Automation.FunctionInfo {
    internal FilterInfo() { }
  }
  public sealed partial class FlagsExpression<T> where T : struct, System.IConvertible {
    // Constructors
    public FlagsExpression(object[] expression) { }
    public FlagsExpression(string expression) { }
     
    // Methods
    public bool Evaluate(T value) { return default(bool); }
  }
  public sealed partial class FormatViewDefinition {
    // Constructors
    public FormatViewDefinition(string name, System.Management.Automation.PSControl control) { }
     
    // Properties
    public System.Management.Automation.PSControl Control { get { return default(System.Management.Automation.PSControl); } }
    public string Name { get { return default(string); } }
     
    // Methods
    public override string ToString() { return default(string); }
  }
  public partial class ForwardedEventArgs : System.EventArgs {
    internal ForwardedEventArgs() { }
    // Properties
    public System.Management.Automation.PSObject SerializedRemoteEventArgs { get { return default(System.Management.Automation.PSObject); } }
     
    // Methods
  }
  public partial class FunctionInfo : System.Management.Automation.CommandInfo {
    internal FunctionInfo() { }
    // Properties
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
     
    // Methods
    protected internal virtual void Update(System.Management.Automation.FunctionInfo newFunction, bool force, System.Management.Automation.ScopedItemOptions options, string helpFile) { }
  }
#if RUNTIME_SERIALIZATION
  public delegate bool GetSymmetricEncryptionKey(System.Runtime.Serialization.StreamingContext context, out byte[] key, out byte[] iv);
#endif
  public partial class GettingValueExceptionEventArgs : System.EventArgs {
    internal GettingValueExceptionEventArgs() { }
    // Properties
    public System.Exception Exception { get { return default(System.Exception); } }
    public bool ShouldThrow { get { return default(bool); } set { } }
    public object ValueReplacement { get { return default(object); } set { } }
     
    // Methods
  }
  public partial class GetValueException : System.Management.Automation.ExtendedTypeSystemException {
    // Constructors
    public GetValueException() { }
#if RUNTIME_SERIALIZATION
    protected GetValueException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public GetValueException(string message) { }
    public GetValueException(string message, System.Exception innerException) { }
  }
  public partial class GetValueInvocationException : System.Management.Automation.GetValueException {
    // Constructors
    public GetValueInvocationException() { }
#if RUNTIME_SERIALIZATION
    protected GetValueInvocationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public GetValueInvocationException(string message) { }
    public GetValueInvocationException(string message, System.Exception innerException) { }
  }
  public partial class HaltCommandException
#if RUNTIME_SERIALIZATION
        : System.SystemException
#else
        : System.Exception
#endif
  {
    // Constructors
    public HaltCommandException() { }
#if RUNTIME_SERIALIZATION
    protected HaltCommandException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public HaltCommandException(string message) { }
    public HaltCommandException(string message, System.Exception innerException) { }
  }
  public partial interface IBackgroundDispatcher {
    // Methods
    System.IAsyncResult BeginInvoke(System.Threading.WaitCallback callback, object state, System.AsyncCallback completionCallback, object asyncState);
    void EndInvoke(System.IAsyncResult asyncResult);
    bool QueueUserWorkItem(System.Threading.WaitCallback callback);
    bool QueueUserWorkItem(System.Threading.WaitCallback callback, object state);
  }
  public partial interface ICommandRuntime {
    // Properties
    System.Management.Automation.PSTransactionContext CurrentPSTransaction { get; }
    System.Management.Automation.Host.PSHost Host { get; }
     
    // Methods
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
    // Properties
    System.Management.Automation.ErrorRecord ErrorRecord { get; }
     
    // Methods
  }
  public partial interface IDynamicParameters {
    // Methods
    object GetDynamicParameters();
  }
  public partial interface IModuleAssemblyInitializer {
    // Methods
    void OnImport();
  }
  public partial class IncompleteParseException : System.Management.Automation.ParseException {
    // Constructors
    public IncompleteParseException() { }
#if RUNTIME_SERIALIZATION
    protected IncompleteParseException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public IncompleteParseException(string message) { }
    public IncompleteParseException(string message, System.Exception innerException) { }
  }
#if RUNTIME_SERIALIZATION
  [System.Runtime.Serialization.DataContractAttribute]
#endif
  public abstract partial class InformationalRecord {
    internal InformationalRecord() { }
    // Properties
    public System.Management.Automation.InvocationInfo InvocationInfo { get { return default(System.Management.Automation.InvocationInfo); } }
    public string Message { get { return default(string); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<int> PipelineIterationInfo { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<int>); } }
     
    // Methods
    public override string ToString() { return default(string); }
  }
  public partial class InvalidJobStateException
#if RUNTIME_SERIALIZATION
        : System.SystemException
#else
        : System.Exception
#endif
    {
    // Constructors
    public InvalidJobStateException() { }
    public InvalidJobStateException(System.Management.Automation.JobState currentState, string actionMessage) { }
#if RUNTIME_SERIALIZATION
    protected InvalidJobStateException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public InvalidJobStateException(string message) { }
    public InvalidJobStateException(string message, System.Exception innerException) { }
     
    // Properties
    public System.Management.Automation.JobState CurrentState { get { return default(System.Management.Automation.JobState); } }
     
    // Methods
  }
  public partial class InvalidPowerShellStateException
#if RUNTIME_SERIALIZATION
        : System.SystemException
#else
        : System.Exception
#endif
    {
    // Constructors
    public InvalidPowerShellStateException() { }
#if RUNTIME_SERIALIZATION
    protected InvalidPowerShellStateException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public InvalidPowerShellStateException(string message) { }
    public InvalidPowerShellStateException(string message, System.Exception innerException) { }
     
    // Properties
    public System.Management.Automation.PSInvocationState CurrentState { get { return default(System.Management.Automation.PSInvocationState); } }
     
    // Methods
  }
  [System.Diagnostics.DebuggerDisplayAttribute("Command = {_commandInfo}")]
  public partial class InvocationInfo {
    internal InvocationInfo() { }
    // Properties
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
     
    // Methods
  }
  public partial interface IResourceSupplier {
    // Methods
    string GetResourceString(string baseName, string resourceId);
  }
  public sealed partial class ItemCmdletProviderIntrinsics {
    internal ItemCmdletProviderIntrinsics() { }
    // Methods
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
  public partial class ItemNotFoundException : System.Management.Automation.SessionStateException {
    // Constructors
    public ItemNotFoundException() { }
#if RUNTIME_SERIALIZATION
    protected ItemNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public ItemNotFoundException(string message) { }
    public ItemNotFoundException(string message, System.Exception innerException) { }
  }
  public abstract partial class Job : System.IDisposable {
    // Constructors
    protected Job() { }
    protected Job(string command) { }
    protected Job(string command, string name) { }
    protected Job(string command, string name, System.Collections.Generic.IList<System.Management.Automation.Job> childJobs) { }
    protected Job(string command, string name, System.Guid instanceId) { }
    protected Job(string command, string name, System.Management.Automation.JobIdentifier token) { }
     
    // Properties
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
     
    // Methods
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
  public abstract partial class Job2 : System.Management.Automation.Job {
    // Constructors
    protected Job2() { }
    protected Job2(string command) { }
    protected Job2(string command, string name) { }
    protected Job2(string command, string name, System.Collections.Generic.IList<System.Management.Automation.Job> childJobs) { }
    protected Job2(string command, string name, System.Guid instanceId) { }
    protected Job2(string command, string name, System.Management.Automation.JobIdentifier token) { }
     
    // Properties
    public System.Collections.Generic.List<System.Management.Automation.Runspaces.CommandParameterCollection> StartParameters { get { return default(System.Collections.Generic.List<System.Management.Automation.Runspaces.CommandParameterCollection>); } set { } }
    protected object SyncRoot { get { return default(object); } }
     
    // Events
    public event System.EventHandler<System.ComponentModel.AsyncCompletedEventArgs> ResumeJobCompleted { add { } remove { } }
    public event System.EventHandler<System.ComponentModel.AsyncCompletedEventArgs> StartJobCompleted { add { } remove { } }
    public event System.EventHandler<System.ComponentModel.AsyncCompletedEventArgs> StopJobCompleted { add { } remove { } }
    public event System.EventHandler<System.ComponentModel.AsyncCompletedEventArgs> SuspendJobCompleted { add { } remove { } }
    public event System.EventHandler<System.ComponentModel.AsyncCompletedEventArgs> UnblockJobCompleted { add { } remove { } }
     
    // Methods
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
  public sealed partial class JobDataAddedEventArgs : System.EventArgs {
    internal JobDataAddedEventArgs() { }
    // Properties
    public System.Management.Automation.PowerShellStreamType DataType { get { return default(System.Management.Automation.PowerShellStreamType); } }
    public int Index { get { return default(int); } }
    public System.Management.Automation.Job SourceJob { get { return default(System.Management.Automation.Job); } }
     
    // Methods
  }
  public partial class JobDefinition : System.Runtime.Serialization.ISerializable
  {
    // Constructors
    protected JobDefinition(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public JobDefinition(System.Type jobSourceAdapterType, string command, string name) { }
     
    // Properties
    public string Command { get { return default(string); } }
    public System.Management.Automation.CommandInfo CommandInfo { get { return default(System.Management.Automation.CommandInfo); } }
    public System.Guid InstanceId { get { return default(System.Guid); } set { } }
    public System.Type JobSourceAdapterType { get { return default(System.Type); } }
    public string JobSourceAdapterTypeName { get { return default(string); } set { } }
    public string ModuleName { get { return default(string); } set { } }
    public string Name { get { return default(string); } set { } }
     
    // Methods
    public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public virtual void Load(System.IO.Stream stream) { }
    public virtual void Save(System.IO.Stream stream) { }
  }
  public partial class JobFailedException : System.Exception {
    // Constructors
    public JobFailedException() { }
    public JobFailedException(System.Exception innerException, System.Management.Automation.Language.ScriptExtent displayScriptPosition) { }
    protected JobFailedException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    public JobFailedException(string message) { }
    public JobFailedException(string message, System.Exception innerException) { }
     
    // Properties
    public System.Management.Automation.Language.ScriptExtent DisplayScriptPosition { get { return default(System.Management.Automation.Language.ScriptExtent); } }
    public override string Message { get { return default(string); } }
    public System.Exception Reason { get { return default(System.Exception); } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
  }
  public sealed partial class JobIdentifier {
    internal JobIdentifier() { }
  }
  public partial class JobInvocationInfo : System.Runtime.Serialization.ISerializable {
    // Constructors
    protected JobInvocationInfo() { }
    public JobInvocationInfo(System.Management.Automation.JobDefinition definition, System.Collections.Generic.Dictionary<string, object> parameters) { }
    public JobInvocationInfo(System.Management.Automation.JobDefinition definition, System.Collections.Generic.IEnumerable<System.Collections.Generic.Dictionary<string, object>> parameterCollectionList) { }
    public JobInvocationInfo(System.Management.Automation.JobDefinition definition, System.Collections.Generic.IEnumerable<System.Management.Automation.Runspaces.CommandParameterCollection> parameters) { }
    public JobInvocationInfo(System.Management.Automation.JobDefinition definition, System.Management.Automation.Runspaces.CommandParameterCollection parameters) { }
    protected JobInvocationInfo(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
     
    // Properties
    public string Command { get { return default(string); } set { } }
    public System.Management.Automation.JobDefinition Definition { get { return default(System.Management.Automation.JobDefinition); } set { } }
    public System.Guid InstanceId { get { return default(System.Guid); } }
    public string Name { get { return default(string); } set { } }
    public System.Collections.Generic.List<System.Management.Automation.Runspaces.CommandParameterCollection> Parameters { get { return default(System.Collections.Generic.List<System.Management.Automation.Runspaces.CommandParameterCollection>); } }
     
    // Methods
    public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public virtual void Load(System.IO.Stream stream) { }
    public virtual void Save(System.IO.Stream stream) { }
  }
  public sealed partial class JobManager {
    internal JobManager() { }
    // Methods
    public bool IsRegistered(string typeName) { return default(bool); }
    public System.Management.Automation.Job2 NewJob(System.Management.Automation.JobDefinition definition) { return default(System.Management.Automation.Job2); }
    public System.Management.Automation.Job2 NewJob(System.Management.Automation.JobInvocationInfo specification) { return default(System.Management.Automation.Job2); }
    public void PersistJob(System.Management.Automation.Job2 job, System.Management.Automation.JobDefinition definition) { }
  }
  public partial class JobRepository : System.Management.Automation.Repository<System.Management.Automation.Job> {
    internal JobRepository() : base (default(string)) { }
    // Properties
    public System.Collections.Generic.List<System.Management.Automation.Job> Jobs { get { return default(System.Collections.Generic.List<System.Management.Automation.Job>); } }
     
    // Methods
    public System.Management.Automation.Job GetJob(System.Guid instanceId) { return default(System.Management.Automation.Job); }
    protected override System.Guid GetKey(System.Management.Automation.Job item) { return default(System.Guid); }
  }
  public abstract partial class JobSourceAdapter {
    // Constructors
    protected JobSourceAdapter() { }
     
    // Properties
    public string Name { get { return default(string); } set { } }
     
    // Methods
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
    // Fields
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
  public sealed partial class JobStateEventArgs : System.EventArgs {
    // Constructors
    public JobStateEventArgs(System.Management.Automation.JobStateInfo jobStateInfo) { }
    public JobStateEventArgs(System.Management.Automation.JobStateInfo jobStateInfo, System.Management.Automation.JobStateInfo previousJobStateInfo) { }
     
    // Properties
    public System.Management.Automation.JobStateInfo JobStateInfo { get { return default(System.Management.Automation.JobStateInfo); } }
    public System.Management.Automation.JobStateInfo PreviousJobStateInfo { get { return default(System.Management.Automation.JobStateInfo); } }
     
    // Methods
  }
  public sealed partial class JobStateInfo {
    // Constructors
    public JobStateInfo(System.Management.Automation.JobState state) { }
    public JobStateInfo(System.Management.Automation.JobState state, System.Exception reason) { }
     
    // Properties
    public System.Exception Reason { get { return default(System.Exception); } }
    public System.Management.Automation.JobState State { get { return default(System.Management.Automation.JobState); } }
     
    // Methods
    public override string ToString() { return default(string); }
  }
  public enum JobThreadOptions {
    // Fields
    Default = 0,
    UseNewThread = 2,
    UseThreadPoolThread = 1,
  }
  public static partial class LanguagePrimitives {
    // Methods
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
  public partial class LineBreakpoint : System.Management.Automation.Breakpoint {
    internal LineBreakpoint() { }
    // Properties
    public int Column { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(int); } }
    public int Line { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(int); } }
     
    // Methods
    public override string ToString() { return default(string); }
  }
  public sealed partial class ListControl : System.Management.Automation.PSControl {
    // Constructors
    public ListControl() { }
    public ListControl(System.Collections.Generic.IEnumerable<System.Management.Automation.ListControlEntry> entries) { }
     
    // Properties
    public System.Collections.Generic.List<System.Management.Automation.ListControlEntry> Entries { get { return default(System.Collections.Generic.List<System.Management.Automation.ListControlEntry>); } }
     
    // Methods
    public override string ToString() { return default(string); }
  }
  public sealed partial class ListControlEntry {
    // Constructors
    public ListControlEntry() { }
    public ListControlEntry(System.Collections.Generic.IEnumerable<System.Management.Automation.ListControlEntryItem> listItems) { }
    public ListControlEntry(System.Collections.Generic.IEnumerable<System.Management.Automation.ListControlEntryItem> listItems, System.Collections.Generic.IEnumerable<string> selectedBy) { }
     
    // Properties
    public System.Collections.Generic.List<System.Management.Automation.ListControlEntryItem> Items { get { return default(System.Collections.Generic.List<System.Management.Automation.ListControlEntryItem>); } }
    public System.Collections.Generic.List<string> SelectedBy { get { return default(System.Collections.Generic.List<string>); } }
     
    // Methods
  }
  public sealed partial class ListControlEntryItem {
    // Constructors
    public ListControlEntryItem(string label, System.Management.Automation.DisplayEntry entry) { }
     
    // Properties
    public System.Management.Automation.DisplayEntry DisplayEntry { get { return default(System.Management.Automation.DisplayEntry); } }
    public string Label { get { return default(string); } }
     
    // Methods
  }
  public partial class MetadataException : System.Management.Automation.RuntimeException {
    // Constructors
    public MetadataException() { }
    protected MetadataException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public MetadataException(string message) { }
    public MetadataException(string message, System.Exception innerException) { }
  }
  public partial class MethodException : System.Management.Automation.ExtendedTypeSystemException {
    // Constructors
    public MethodException() { }
    protected MethodException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public MethodException(string message) { }
    public MethodException(string message, System.Exception innerException) { }
  }
  public partial class MethodInvocationException : System.Management.Automation.MethodException {
    // Constructors
    public MethodInvocationException() { }
    protected MethodInvocationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public MethodInvocationException(string message) { }
    public MethodInvocationException(string message, System.Exception innerException) { }
  }
  public enum ModuleAccessMode {
    // Fields
    Constant = 2,
    ReadOnly = 1,
    ReadWrite = 0,
  }
  public enum ModuleType {
    // Fields
    Binary = 1,
    Cim = 3,
    Manifest = 2,
    Script = 0,
    Workflow = 4,
  }
  [System.AttributeUsageAttribute((AttributeTargets)4, AllowMultiple=true)]
  public sealed partial class OutputTypeAttribute : System.Management.Automation.Internal.CmdletMetadataAttribute {
    // Constructors
    public OutputTypeAttribute(params string[] type) { }
    public OutputTypeAttribute(params System.Type[] type) { }
     
    // Properties
    public string[] ParameterSetName { get { return default(string[]); } set { } }
    public string ProviderCmdlet { get { return default(string); } set { } }
    public System.Management.Automation.PSTypeName[] Type { get { return default(System.Management.Automation.PSTypeName[]); } }
     
    // Methods
  }
  public sealed partial class PagingParameters {
    internal PagingParameters() { }
    // Properties
    [System.Management.Automation.ParameterAttribute]
    public ulong First { get { return default(ulong); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter IncludeTotalCount { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.SwitchParameter); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    [System.Management.Automation.ParameterAttribute]
    public ulong Skip { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(ulong); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
     
    // Methods
    public System.Management.Automation.PSObject NewTotalCount(ulong totalCount, double accuracy) { return default(System.Management.Automation.PSObject); }
  }
  [System.AttributeUsageAttribute((AttributeTargets)384, AllowMultiple=true)]
  public sealed partial class ParameterAttribute : System.Management.Automation.Internal.ParsingBaseAttribute {
    // Fields
    public const string AllParameterSets = "__AllParameterSets";
     
    // Constructors
    public ParameterAttribute() { }
     
    // Properties
    public string HelpMessage { get { return default(string); } set { } }
    public string HelpMessageBaseName { get { return default(string); } set { } }
    public string HelpMessageResourceId { get { return default(string); } set { } }
    public bool Mandatory { get { return default(bool); } set { } }
    public string ParameterSetName { get { return default(string); } set { } }
    public int Position { get { return default(int); } set { } }
    public bool ValueFromPipeline { get { return default(bool); } set { } }
    public bool ValueFromPipelineByPropertyName { get { return default(bool); } set { } }
    public bool ValueFromRemainingArguments { get { return default(bool); } set { } }
     
    // Methods
  }
  public partial class ParameterBindingException : System.Management.Automation.RuntimeException {
    // Constructors
    public ParameterBindingException() { }
    protected ParameterBindingException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public ParameterBindingException(string message) { }
    public ParameterBindingException(string message, System.Exception innerException) { }
     
    // Properties
    public System.Management.Automation.InvocationInfo CommandInvocation { get { return default(System.Management.Automation.InvocationInfo); } }
    public string ErrorId { get { return default(string); } }
    public long Line { get { return default(long); } }
    public override string Message { get { return default(string); } }
    public long Offset { get { return default(long); } }
    public string ParameterName { get { return default(string); } }
    public System.Type ParameterType { get { return default(System.Type); } }
    public System.Type TypeSpecified { get { return default(System.Type); } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
  }
  public sealed partial class ParameterMetadata {
    // Constructors
    public ParameterMetadata(System.Management.Automation.ParameterMetadata other) { }
    public ParameterMetadata(string name) { }
    public ParameterMetadata(string name, System.Type parameterType) { }
     
    // Properties
    public System.Collections.ObjectModel.Collection<string> Aliases { get { return default(System.Collections.ObjectModel.Collection<string>); } }
    public System.Collections.ObjectModel.Collection<System.Attribute> Attributes { get { return default(System.Collections.ObjectModel.Collection<System.Attribute>); } }
    public bool IsDynamic { get { return default(bool); } set { } }
    public string Name { get { return default(string); } set { } }
    public System.Collections.Generic.Dictionary<string, System.Management.Automation.ParameterSetMetadata> ParameterSets { get { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.ParameterSetMetadata>); } }
    public System.Type ParameterType { get { return default(System.Type); } set { } }
    public bool SwitchParameter { get { return default(bool); } }
     
    // Methods
    public static System.Collections.Generic.Dictionary<string, System.Management.Automation.ParameterMetadata> GetParameterMetadata(System.Type type) { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.ParameterMetadata>); }
  }
  public sealed partial class ParameterSetMetadata {
    internal ParameterSetMetadata() { }
    // Properties
    public string HelpMessage { get { return default(string); } set { } }
    public string HelpMessageBaseName { get { return default(string); } set { } }
    public string HelpMessageResourceId { get { return default(string); } set { } }
    public bool IsMandatory { get { return default(bool); } set { } }
    public int Position { get { return default(int); } set { } }
    public bool ValueFromPipeline { get { return default(bool); } set { } }
    public bool ValueFromPipelineByPropertyName { get { return default(bool); } set { } }
    public bool ValueFromRemainingArguments { get { return default(bool); } set { } }
     
    // Methods
  }
  public partial class ParentContainsErrorRecordException
#if RUNTIME_SERIALIZATION
        : System.SystemException
#else
        : System.Exception
#endif
    {
    // Constructors
    public ParentContainsErrorRecordException() { }
    public ParentContainsErrorRecordException(System.Exception wrapperException) { }
#if RUNTIME_SERIALIZATION
    protected ParentContainsErrorRecordException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public ParentContainsErrorRecordException(string message) { }
    public ParentContainsErrorRecordException(string message, System.Exception innerException) { }
     
    // Properties
    public override string Message { get { return default(string); } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
  }
  public partial class ParseException : System.Management.Automation.RuntimeException {
    // Constructors
    public ParseException() { }
    public ParseException(System.Management.Automation.Language.ParseError[] errors) { }
    protected ParseException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public ParseException(string message) { }
    public ParseException(string message, System.Exception innerException) { }
     
    // Properties
    public System.Management.Automation.Language.ParseError[] Errors { get { return default(System.Management.Automation.Language.ParseError[]); } }
    public override string Message { get { return default(string); } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
  }
  public partial class ParsingMetadataException : System.Management.Automation.MetadataException {
    // Constructors
    public ParsingMetadataException() { }
    protected ParsingMetadataException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public ParsingMetadataException(string message) { }
    public ParsingMetadataException(string message, System.Exception innerException) { }
  }
  public sealed partial class PathInfo {
    internal PathInfo() { }
    // Properties
    public System.Management.Automation.PSDriveInfo Drive { get { return default(System.Management.Automation.PSDriveInfo); } }
    public string Path { get { return default(string); } }
    public System.Management.Automation.ProviderInfo Provider { get { return default(System.Management.Automation.ProviderInfo); } }
    public string ProviderPath { get { return default(string); } }
     
    // Methods
    public override string ToString() { return default(string); }
  }
  public sealed partial class PathInfoStack : System.Collections.Generic.Stack<System.Management.Automation.PathInfo> {
    internal PathInfoStack() { }
    // Properties
    public string Name { get { return default(string); } }
     
    // Methods
  }
  public sealed partial class PathIntrinsics {
    internal PathIntrinsics() { }
    // Properties
    public System.Management.Automation.PathInfo CurrentFileSystemLocation { get { return default(System.Management.Automation.PathInfo); } }
    public System.Management.Automation.PathInfo CurrentLocation { get { return default(System.Management.Automation.PathInfo); } }
     
    // Methods
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
  public partial class PipelineClosedException : System.Management.Automation.RuntimeException {
    // Constructors
    public PipelineClosedException() { }
    protected PipelineClosedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public PipelineClosedException(string message) { }
    public PipelineClosedException(string message, System.Exception innerException) { }
  }
  public partial class PipelineDepthException
#if RUNTIME_SERIALIZATION
        : System.SystemException
#else
        : System.Exception
#endif
        , System.Management.Automation.IContainsErrorRecord
    {
    // Constructors
    public PipelineDepthException() { }
    protected PipelineDepthException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public PipelineDepthException(string message) { }
    public PipelineDepthException(string message, System.Exception innerException) { }
     
    // Properties
    public int CallDepth { get { return default(int); } }
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
  }
  public partial class PipelineStoppedException : System.Management.Automation.RuntimeException {
    // Constructors
    public PipelineStoppedException() { }
    protected PipelineStoppedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public PipelineStoppedException(string message) { }
    public PipelineStoppedException(string message, System.Exception innerException) { }
  }
  public sealed partial class PowerShell : System.IDisposable {
    internal PowerShell() { }
    // Properties
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
     
    // Methods
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
  public sealed partial class PowerShellStreams<TInput, TOutput> : System.IDisposable {
    // Constructors
    public PowerShellStreams() { }
    public PowerShellStreams(System.Management.Automation.PSDataCollection<TInput> pipelineInput) { }
     
    // Properties
    public System.Management.Automation.PSDataCollection<System.Management.Automation.DebugRecord> DebugStream { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.DebugRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.ErrorRecord> ErrorStream { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.ErrorRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<TInput> InputStream { get { return default(System.Management.Automation.PSDataCollection<TInput>); } set { } }
    public System.Management.Automation.PSDataCollection<TOutput> OutputStream { get { return default(System.Management.Automation.PSDataCollection<TOutput>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.ProgressRecord> ProgressStream { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.ProgressRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.VerboseRecord> VerboseStream { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.VerboseRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.WarningRecord> WarningStream { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.WarningRecord>); } set { } }
     
    // Methods
    public void CloseAll() { }
    public void Dispose() { }
  }
  public enum PowerShellStreamType {
    // Fields
    Debug = 5,
    Error = 2,
    Input = 0,
    Output = 1,
    Progress = 6,
    Verbose = 4,
    Warning = 3,
  }
  [System.Runtime.Serialization.DataContractAttribute]
  public partial class ProgressRecord {
    // Constructors
    public ProgressRecord(int activityId, string activity, string statusDescription) { }
     
    // Properties
    public string Activity { get { return default(string); } set { } }
    public int ActivityId { get { return default(int); } }
    public string CurrentOperation { get { return default(string); } set { } }
    public int ParentActivityId { get { return default(int); } set { } }
    public int PercentComplete { get { return default(int); } set { } }
    public System.Management.Automation.ProgressRecordType RecordType { get { return default(System.Management.Automation.ProgressRecordType); } set { } }
    public int SecondsRemaining { get { return default(int); } set { } }
    public string StatusDescription { get { return default(string); } set { } }
     
    // Methods
    public override string ToString() { return default(string); }
  }
  public enum ProgressRecordType {
    // Fields
    Completed = 1,
    Processing = 0,
  }
  public sealed partial class PropertyCmdletProviderIntrinsics {
    internal PropertyCmdletProviderIntrinsics() { }
    // Methods
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
  public partial class PropertyNotFoundException : System.Management.Automation.ExtendedTypeSystemException {
    // Constructors
    public PropertyNotFoundException() { }
    protected PropertyNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public PropertyNotFoundException(string message) { }
    public PropertyNotFoundException(string message, System.Exception innerException) { }
  }
  public static partial class ProviderCmdlet {
    // Fields
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
  public partial class ProviderInfo {
    // Constructors
    protected ProviderInfo(System.Management.Automation.ProviderInfo providerInfo) { }
     
    // Properties
    public System.Management.Automation.Provider.ProviderCapabilities Capabilities { get { return default(System.Management.Automation.Provider.ProviderCapabilities); } }
    public string Description { get { return default(string); } set { } }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo> Drives { get { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo>); } }
    public string HelpFile { get { return default(string); } }
    public string Home { get { return default(string); } set { } }
    public System.Type ImplementingType { get { return default(System.Type); } }
    public System.Management.Automation.PSModuleInfo Module { get { return default(System.Management.Automation.PSModuleInfo); } }
    public string ModuleName { get { return default(string); } }
    public string Name { get { return default(string); } }
    public System.Management.Automation.PSSnapInInfo PSSnapIn { get { return default(System.Management.Automation.PSSnapInInfo); } }
     
    // Methods
    public override string ToString() { return default(string); }
  }
  public sealed partial class ProviderIntrinsics {
    internal ProviderIntrinsics() { }
    // Properties
    public System.Management.Automation.ChildItemCmdletProviderIntrinsics ChildItem { get { return default(System.Management.Automation.ChildItemCmdletProviderIntrinsics); } }
    public System.Management.Automation.ContentCmdletProviderIntrinsics Content { get { return default(System.Management.Automation.ContentCmdletProviderIntrinsics); } }
    public System.Management.Automation.ItemCmdletProviderIntrinsics Item { get { return default(System.Management.Automation.ItemCmdletProviderIntrinsics); } }
    public System.Management.Automation.PropertyCmdletProviderIntrinsics Property { get { return default(System.Management.Automation.PropertyCmdletProviderIntrinsics); } }
    public System.Management.Automation.SecurityDescriptorCmdletProviderIntrinsics SecurityDescriptor { get { return default(System.Management.Automation.SecurityDescriptorCmdletProviderIntrinsics); } }
     
    // Methods
  }
  public partial class ProviderInvocationException : System.Management.Automation.RuntimeException {
    // Constructors
    public ProviderInvocationException() { }
    protected ProviderInvocationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public ProviderInvocationException(string message) { }
    public ProviderInvocationException(string message, System.Exception innerException) { }
     
    // Properties
    public override System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override string Message { get { return default(string); } }
    public System.Management.Automation.ProviderInfo ProviderInfo { get { return default(System.Management.Automation.ProviderInfo); } }
     
    // Methods
  }
  public partial class ProviderNameAmbiguousException : System.Management.Automation.ProviderNotFoundException {
    // Constructors
    public ProviderNameAmbiguousException() { }
    protected ProviderNameAmbiguousException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public ProviderNameAmbiguousException(string message) { }
    public ProviderNameAmbiguousException(string message, System.Exception innerException) { }
     
    // Properties
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.ProviderInfo> PossibleMatches { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.ProviderInfo>); } }
     
    // Methods
  }
  public partial class ProviderNotFoundException : System.Management.Automation.SessionStateException {
    // Constructors
    public ProviderNotFoundException() { }
    protected ProviderNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public ProviderNotFoundException(string message) { }
    public ProviderNotFoundException(string message, System.Exception innerException) { }
  }
  public sealed partial class ProxyCommand {
    internal ProxyCommand() { }
    // Methods
    public static string Create(System.Management.Automation.CommandMetadata commandMetadata) { return default(string); }
    public static string Create(System.Management.Automation.CommandMetadata commandMetadata, string helpComment) { return default(string); }
    public static string GetBegin(System.Management.Automation.CommandMetadata commandMetadata) { return default(string); }
    public static string GetCmdletBindingAttribute(System.Management.Automation.CommandMetadata commandMetadata) { return default(string); }
    public static string GetEnd(System.Management.Automation.CommandMetadata commandMetadata) { return default(string); }
    public static string GetHelpComments(System.Management.Automation.PSObject help) { return default(string); }
    public static string GetParamBlock(System.Management.Automation.CommandMetadata commandMetadata) { return default(string); }
    public static string GetProcess(System.Management.Automation.CommandMetadata commandMetadata) { return default(string); }
  }
  public partial class PSAdaptedProperty : System.Management.Automation.PSProperty {
    // Constructors
    public PSAdaptedProperty(string name, object tag) { }
     
    // Properties
    public object BaseObject { get { return default(object); } }
    public object Tag { get { return default(object); } }
     
    // Methods
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
  }
  public partial class PSAliasProperty : System.Management.Automation.PSPropertyInfo {
    // Constructors
    public PSAliasProperty(string name, string referencedMemberName) { }
    public PSAliasProperty(string name, string referencedMemberName, System.Type conversionType) { }
     
    // Properties
    public System.Type ConversionType { get { return default(System.Type); } }
    public override bool IsGettable { get { return default(bool); } }
    public override bool IsSettable { get { return default(bool); } }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public string ReferencedMemberName { get { return default(string); } }
    public override string TypeNameOfValue { get { return default(string); } }
    public override object Value { get { return default(object); } set { } }
     
    // Methods
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString() { return default(string); }
  }
  public partial class PSArgumentException : System.ArgumentException, System.Management.Automation.IContainsErrorRecord {
    // Constructors
    public PSArgumentException() { }
    protected PSArgumentException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public PSArgumentException(string message) { }
    public PSArgumentException(string message, System.Exception innerException) { }
    public PSArgumentException(string message, string paramName) { }
     
    // Properties
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override string Message { get { return default(string); } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
  }
  public partial class PSArgumentNullException : System.ArgumentNullException, System.Management.Automation.IContainsErrorRecord {
    // Constructors
    public PSArgumentNullException() { }
    protected PSArgumentNullException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public PSArgumentNullException(string paramName) { }
    public PSArgumentNullException(string message, System.Exception innerException) { }
    public PSArgumentNullException(string paramName, string message) { }
     
    // Properties
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override string Message { get { return default(string); } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
  }
  public partial class PSArgumentOutOfRangeException : System.ArgumentOutOfRangeException, System.Management.Automation.IContainsErrorRecord {
    // Constructors
    public PSArgumentOutOfRangeException() { }
    protected PSArgumentOutOfRangeException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public PSArgumentOutOfRangeException(string paramName) { }
    public PSArgumentOutOfRangeException(string message, System.Exception innerException) { }
    public PSArgumentOutOfRangeException(string paramName, object actualValue, string message) { }
     
    // Properties
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
  }
  public sealed partial class PSChildJobProxy : System.Management.Automation.Job2 {
    internal PSChildJobProxy() { }
    // Properties
    public override bool HasMoreData { get { return default(bool); } }
    public override string Location { get { return default(string); } }
    public override string StatusMessage { get { return default(string); } }
     
    // Events
    public event System.EventHandler<System.Management.Automation.JobDataAddedEventArgs> JobDataAdded { add { } remove { } }
     
    // Methods
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
  public abstract partial class PSCmdlet : System.Management.Automation.Cmdlet {
    // Constructors
    protected PSCmdlet() { }
     
    // Properties
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
     
    // Methods
    public System.Management.Automation.PathInfo CurrentProviderLocation(string providerId) { return default(System.Management.Automation.PathInfo); }
    public System.Collections.ObjectModel.Collection<string> GetResolvedProviderPathFromPSPath(string path, out System.Management.Automation.ProviderInfo provider) { provider = default(System.Management.Automation.ProviderInfo); return default(System.Collections.ObjectModel.Collection<string>); }
    public string GetUnresolvedProviderPathFromPSPath(string path) { return default(string); }
    public object GetVariableValue(string name) { return default(object); }
    public object GetVariableValue(string name, object defaultValue) { return default(object); }
  }
  public partial class PSCodeMethod : System.Management.Automation.PSMethodInfo {
    // Constructors
    public PSCodeMethod(string name, System.Reflection.MethodInfo codeReference) { }
     
    // Properties
    public System.Reflection.MethodInfo CodeReference { get { return default(System.Reflection.MethodInfo); } }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public override System.Collections.ObjectModel.Collection<string> OverloadDefinitions { get { return default(System.Collections.ObjectModel.Collection<string>); } }
    public override string TypeNameOfValue { get { return default(string); } }
     
    // Methods
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
    public override object Invoke(params object[] arguments) { return default(object); }
    public override string ToString() { return default(string); }
  }
  public partial class PSCodeProperty : System.Management.Automation.PSPropertyInfo {
    // Constructors
    public PSCodeProperty(string name, System.Reflection.MethodInfo getterCodeReference) { }
    public PSCodeProperty(string name, System.Reflection.MethodInfo getterCodeReference, System.Reflection.MethodInfo setterCodeReference) { }
     
    // Properties
    public System.Reflection.MethodInfo GetterCodeReference { get { return default(System.Reflection.MethodInfo); } }
    public override bool IsGettable { get { return default(bool); } }
    public override bool IsSettable { get { return default(bool); } }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public System.Reflection.MethodInfo SetterCodeReference { get { return default(System.Reflection.MethodInfo); } }
    public override string TypeNameOfValue { get { return default(string); } }
    public override object Value { get { return default(object); } set { } }
     
    // Methods
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString() { return default(string); }
  }
  public sealed partial class PSCommand {
    // Constructors
    public PSCommand() { }
     
    // Properties
    public System.Management.Automation.Runspaces.CommandCollection Commands { get { return default(System.Management.Automation.Runspaces.CommandCollection); } }
     
    // Methods
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
  public abstract partial class PSControl {
    // Constructors
    protected PSControl() { }
  }
  public sealed partial class PSCredential : System.Runtime.Serialization.ISerializable
  {
    // Constructors
    public PSCredential(string userName, System.Security.SecureString password) { }
     
    // Properties
    public static System.Management.Automation.PSCredential Empty { get { return default(System.Management.Automation.PSCredential); } }
#if RUNTIME_SERIALIZATION
    public static System.Management.Automation.GetSymmetricEncryptionKey GetSymmetricEncryptionKeyDelegate { get { return default(System.Management.Automation.GetSymmetricEncryptionKey); } set { } }
#endif
    public System.Security.SecureString Password { get { return default(System.Security.SecureString); } }
    public string UserName { get { return default(string); } }
     
    // Methods
    public System.Net.NetworkCredential GetNetworkCredential() { return default(System.Net.NetworkCredential); }
    public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public static explicit operator System.Net.NetworkCredential (System.Management.Automation.PSCredential credential) { return default(System.Net.NetworkCredential); }
  }
  [System.FlagsAttribute]
  public enum PSCredentialTypes {
    // Fields
    Default = 3,
    Domain = 2,
    Generic = 1,
  }
  [System.FlagsAttribute]
  public enum PSCredentialUIOptions {
    // Fields
    AlwaysPrompt = 2,
    Default = 1,
    None = 0,
    ReadOnlyUserName = 3,
    ValidateUserNameSyntax = 1,
  }
  public partial class PSCustomObject {
    internal PSCustomObject() { }
    // Methods
    public override string ToString() { return default(string); }
  }
  public partial class PSDataCollection<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IList<T>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList, System.IDisposable, System.Runtime.Serialization.ISerializable {
    // Constructors
    public PSDataCollection() { }
    public PSDataCollection(System.Collections.Generic.IEnumerable<T> items) { }
    public PSDataCollection(int capacity) { }
    protected PSDataCollection(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
     
    // Properties
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
     
    // Methods
    public void Add(T item) { }
    public void Clear() { }
    public void Complete() { }
    public bool Contains(T item) { return default(bool); }
    public void CopyTo(T[] array, int arrayIndex) { }
    public void Dispose() { }
    protected void Dispose(bool disposing) { }
    public System.Collections.Generic.IEnumerator<T> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<T>); }
    public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
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
  public sealed partial class PSDataStreams {
    internal PSDataStreams() { }
    // Properties
    public System.Management.Automation.PSDataCollection<System.Management.Automation.DebugRecord> Debug { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.DebugRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.ErrorRecord> Error { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.ErrorRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.ProgressRecord> Progress { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.ProgressRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.VerboseRecord> Verbose { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.VerboseRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.WarningRecord> Warning { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.WarningRecord>); } set { } }
     
    // Methods
    public void ClearStreams() { }
  }
  public partial class PSDebugContext {
    internal PSDebugContext() { }
    // Properties
    public System.Management.Automation.Breakpoint[] Breakpoints { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Breakpoint[]); } }
    public System.Management.Automation.InvocationInfo InvocationInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.InvocationInfo); } }
     
    // Methods
  }
  [System.AttributeUsageAttribute((AttributeTargets)384)]
  public sealed partial class PSDefaultValueAttribute : System.Management.Automation.Internal.ParsingBaseAttribute {
    // Constructors
    public PSDefaultValueAttribute() { }
     
    // Properties
    public string Help { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public object Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(object); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
     
    // Methods
  }
  public partial class PSDriveInfo : System.IComparable {
    // Constructors
    protected PSDriveInfo(System.Management.Automation.PSDriveInfo driveInfo) { }
    public PSDriveInfo(string name, System.Management.Automation.ProviderInfo provider, string root, string description, System.Management.Automation.PSCredential credential) { }
    public PSDriveInfo(string name, System.Management.Automation.ProviderInfo provider, string root, string description, System.Management.Automation.PSCredential credential, bool persist) { }
    public PSDriveInfo(string name, System.Management.Automation.ProviderInfo provider, string root, string description, System.Management.Automation.PSCredential credential, string displayRoot) { }
     
    // Properties
    public System.Management.Automation.PSCredential Credential { get { return default(System.Management.Automation.PSCredential); } }
    public string CurrentLocation { get { return default(string); } set { } }
    public string Description { get { return default(string); } set { } }
    public string DisplayRoot { get { return default(string); } }
    public string Name { get { return default(string); } }
    public System.Management.Automation.ProviderInfo Provider { get { return default(System.Management.Automation.ProviderInfo); } }
    public string Root { get { return default(string); } }
     
    // Methods
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
  public partial class PSDynamicMember : System.Management.Automation.PSMemberInfo {
    internal PSDynamicMember() { }
    // Properties
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public override string TypeNameOfValue { get { return default(string); } }
    public override object Value { get { return default(object); } set { } }
     
    // Methods
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString() { return default(string); }
  }
  public sealed partial class PSEngineEvent {
    internal PSEngineEvent() { }
    // Fields
    public const string Exiting = "PowerShell.Exiting";
    public const string OnIdle = "PowerShell.OnIdle";
  }
  public partial class PSEvent : System.Management.Automation.PSMemberInfo {
    internal PSEvent() { }
    // Properties
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public override string TypeNameOfValue { get { return default(string); } }
    public sealed override object Value { get { return default(object); } set { } }
     
    // Methods
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString() { return default(string); }
  }
  public partial class PSEventArgs : System.EventArgs {
    internal PSEventArgs() { }
    // Properties
    public string ComputerName { get { return default(string); } }
    public int EventIdentifier { get { return default(int); } }
    public System.Management.Automation.PSObject MessageData { get { return default(System.Management.Automation.PSObject); } }
    public System.Guid RunspaceId { get { return default(System.Guid); } }
    public object Sender { get { return default(object); } }
    public object[] SourceArgs { get { return default(object[]); } }
    public System.EventArgs SourceEventArgs { get { return default(System.EventArgs); } }
    public string SourceIdentifier { get { return default(string); } }
    public System.DateTime TimeGenerated { get { return default(System.DateTime); } }
     
    // Methods
  }
  public partial class PSEventArgsCollection : System.Collections.Generic.IEnumerable<System.Management.Automation.PSEventArgs>, System.Collections.IEnumerable {
    // Constructors
    public PSEventArgsCollection() { }
     
    // Properties
    public int Count { get { return default(int); } }
    public System.Management.Automation.PSEventArgs this[int index] { get { return default(System.Management.Automation.PSEventArgs); } }
    public object SyncRoot { get { return default(object); } }
     
    // Events
    public event System.Management.Automation.PSEventReceivedEventHandler PSEventReceived { add { } remove { } }
     
    // Methods
    public System.Collections.Generic.IEnumerator<System.Management.Automation.PSEventArgs> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<System.Management.Automation.PSEventArgs>); }
    public void RemoveAt(int index) { }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
  }
  public partial class PSEventHandler {
    // Fields
    protected System.Management.Automation.PSEventManager eventManager;
    protected System.Management.Automation.PSObject extraData;
    protected object sender;
    protected string sourceIdentifier;
     
    // Constructors
    public PSEventHandler() { }
    public PSEventHandler(System.Management.Automation.PSEventManager eventManager, object sender, string sourceIdentifier, System.Management.Automation.PSObject extraData) { }
  }
  public partial class PSEventJob : System.Management.Automation.Job {
    // Constructors
    public PSEventJob(System.Management.Automation.PSEventManager eventManager, System.Management.Automation.PSEventSubscriber subscriber, System.Management.Automation.ScriptBlock action, string name) { }
     
    // Properties
    public override bool HasMoreData { get { return default(bool); } }
    public override string Location { get { return default(string); } }
    public System.Management.Automation.PSModuleInfo Module { get { return default(System.Management.Automation.PSModuleInfo); } }
    public override string StatusMessage { get { return default(string); } }
     
    // Methods
    public override void StopJob() { }
  }
  public abstract partial class PSEventManager {
    // Constructors
    protected PSEventManager() { }
     
    // Properties
    public System.Management.Automation.PSEventArgsCollection ReceivedEvents { get { return default(System.Management.Automation.PSEventArgsCollection); } }
    public abstract System.Collections.Generic.List<System.Management.Automation.PSEventSubscriber> Subscribers { get; }
     
    // Methods
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
  public partial class PSEventSubscriber : System.IEquatable<System.Management.Automation.PSEventSubscriber> {
    internal PSEventSubscriber() { }
    // Properties
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
     
    // Methods
    public bool Equals(System.Management.Automation.PSEventSubscriber other) { return default(bool); }
    public override int GetHashCode() { return default(int); }
  }
  public partial class PSEventUnsubscribedEventArgs : System.EventArgs {
    internal PSEventUnsubscribedEventArgs() { }
    // Properties
    public System.Management.Automation.PSEventSubscriber EventSubscriber { get { return default(System.Management.Automation.PSEventSubscriber); } }
     
    // Methods
  }
  public delegate void PSEventUnsubscribedEventHandler(object sender, System.Management.Automation.PSEventUnsubscribedEventArgs e);
#if SNAPINS
  public abstract partial class PSInstaller : System.Configuration.Install.Installer {
    // Constructors
    protected PSInstaller() { }
     
    // Methods
    public sealed override void Install(System.Collections.IDictionary stateSaver) { }
    public sealed override void Rollback(System.Collections.IDictionary savedState) { }
    public sealed override void Uninstall(System.Collections.IDictionary savedState) { }
  }
#endif
  public partial class PSInvalidCastException : System.InvalidCastException, System.Management.Automation.IContainsErrorRecord {
    // Constructors
    public PSInvalidCastException() { }
    protected PSInvalidCastException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public PSInvalidCastException(string message) { }
    public PSInvalidCastException(string message, System.Exception innerException) { }
     
    // Properties
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
  }
  public partial class PSInvalidOperationException : System.InvalidOperationException, System.Management.Automation.IContainsErrorRecord {
    // Constructors
    public PSInvalidOperationException() { }
#if RUNTIME_SERIALIZATION
    protected PSInvalidOperationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public PSInvalidOperationException(string message) { }
#endif
    public PSInvalidOperationException(string message, System.Exception innerException) { }
     
    // Properties
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
  }
  public sealed partial class PSInvocationSettings {
    // Constructors
    public PSInvocationSettings() { }
     
    // Properties
    public bool AddToHistory { get { return default(bool); } set { } }
#if COM_APARTMENT_STATE
    public System.Threading.ApartmentState ApartmentState { get { return default(System.Threading.ApartmentState); } set { } }
#endif
    public System.Nullable<System.Management.Automation.ActionPreference> ErrorActionPreference { get { return default(System.Nullable<System.Management.Automation.ActionPreference>); } set { } }
    public bool FlowImpersonationPolicy { get { return default(bool); } set { } }
    public System.Management.Automation.Host.PSHost Host { get { return default(System.Management.Automation.Host.PSHost); } set { } }
    public System.Management.Automation.RemoteStreamOptions RemoteStreamOptions { get { return default(System.Management.Automation.RemoteStreamOptions); } set { } }
     
    // Methods
  }
  public enum PSInvocationState {
    // Fields
    Completed = 4,
    Disconnected = 6,
    Failed = 5,
    NotStarted = 0,
    Running = 1,
    Stopped = 3,
    Stopping = 2,
  }
  public sealed partial class PSInvocationStateChangedEventArgs : System.EventArgs {
    internal PSInvocationStateChangedEventArgs() { }
    // Properties
    public System.Management.Automation.PSInvocationStateInfo InvocationStateInfo { get { return default(System.Management.Automation.PSInvocationStateInfo); } }
     
    // Methods
  }
  public sealed partial class PSInvocationStateInfo {
    internal PSInvocationStateInfo() { }
    // Properties
    public System.Exception Reason { get { return default(System.Exception); } }
    public System.Management.Automation.PSInvocationState State { get { return default(System.Management.Automation.PSInvocationState); } }
     
    // Methods
  }
  public sealed partial class PSJobProxy : System.Management.Automation.Job2 {
    internal PSJobProxy() { }
    // Properties
    public override bool HasMoreData { get { return default(bool); } }
    public override string Location { get { return default(string); } }
    public System.Guid RemoteJobInstanceId { get { return default(System.Guid); } }
    public bool RemoveRemoteJobOnCompletion { get { return default(bool); } set { } }
    public System.Management.Automation.Runspaces.Runspace Runspace { get { return default(System.Management.Automation.Runspaces.Runspace); } set { } }
    public System.Management.Automation.Runspaces.RunspacePool RunspacePool { get { return default(System.Management.Automation.Runspaces.RunspacePool); } set { } }
    public override string StatusMessage { get { return default(string); } }
     
    // Events
    public event System.EventHandler<System.ComponentModel.AsyncCompletedEventArgs> RemoveJobCompleted { add { } remove { } }
     
    // Methods
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
    // Fields
    ConstrainedLanguage = 3,
    FullLanguage = 0,
    NoLanguage = 2,
    RestrictedLanguage = 1,
  }
  public partial class PSListModifier {
    // Constructors
    public PSListModifier() { }
    public PSListModifier(System.Collections.Hashtable hash) { }
    public PSListModifier(System.Collections.ObjectModel.Collection<object> removeItems, System.Collections.ObjectModel.Collection<object> addItems) { }
    public PSListModifier(object replacementItems) { }
     
    // Properties
    public System.Collections.ObjectModel.Collection<object> Add { get { return default(System.Collections.ObjectModel.Collection<object>); } }
    public System.Collections.ObjectModel.Collection<object> Remove { get { return default(System.Collections.ObjectModel.Collection<object>); } }
    public System.Collections.ObjectModel.Collection<object> Replace { get { return default(System.Collections.ObjectModel.Collection<object>); } }
     
    // Methods
    public void ApplyTo(System.Collections.IList collectionToUpdate) { }
    public void ApplyTo(object collectionToUpdate) { }
  }
  public partial class PSListModifier<T> : System.Management.Automation.PSListModifier {
    // Constructors
    public PSListModifier() { }
    public PSListModifier(System.Collections.Hashtable hash) { }
    public PSListModifier(System.Collections.ObjectModel.Collection<object> removeItems, System.Collections.ObjectModel.Collection<object> addItems) { }
    public PSListModifier(object replacementItems) { }
  }
  public abstract partial class PSMemberInfo {
    // Constructors
    protected PSMemberInfo() { }
     
    // Properties
    public bool IsInstance { get { return default(bool); } }
    public abstract System.Management.Automation.PSMemberTypes MemberType { get; }
    public string Name { get { return default(string); } }
    public abstract string TypeNameOfValue { get; }
    public abstract object Value { get; set; }
     
    // Methods
    public abstract System.Management.Automation.PSMemberInfo Copy();
    protected void SetMemberName(string name) { }
  }
  public abstract partial class PSMemberInfoCollection<T> : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable where T : System.Management.Automation.PSMemberInfo {
    // Constructors
    protected PSMemberInfoCollection() { }
     
    // Properties
    public abstract T this[string name] { get; }
     
    // Methods
    public abstract void Add(T member);
    public abstract void Add(T member, bool preValidated);
    public abstract System.Collections.Generic.IEnumerator<T> GetEnumerator();
    public abstract System.Management.Automation.ReadOnlyPSMemberInfoCollection<T> Match(string name);
    public abstract System.Management.Automation.ReadOnlyPSMemberInfoCollection<T> Match(string name, System.Management.Automation.PSMemberTypes memberTypes);
    public abstract void Remove(string name);
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
  }
  public partial class PSMemberSet : System.Management.Automation.PSMemberInfo {
    // Constructors
    public PSMemberSet(string name) { }
    public PSMemberSet(string name, System.Collections.Generic.IEnumerable<System.Management.Automation.PSMemberInfo> members) { }
     
    // Properties
    public bool InheritMembers { get { return default(bool); } }
    public System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSMemberInfo> Members { get { return default(System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSMemberInfo>); } }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSMethodInfo> Methods { get { return default(System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSMethodInfo>); } }
    public System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSPropertyInfo> Properties { get { return default(System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSPropertyInfo>); } }
    public override string TypeNameOfValue { get { return default(string); } }
    public override object Value { get { return default(object); } set { } }
     
    // Methods
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString() { return default(string); }
  }
  //Internal: [System.ComponentModel.TypeConverterAttribute(typeof(System.Management.Automation.LanguagePrimitives.EnumMultipleTypeConverter))]
  [System.FlagsAttribute]
  public enum PSMemberTypes {
    // Fields
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
    // Fields
    Adapted = 2,
    All = 7,
    Base = 4,
    Extended = 1,
  }
  public partial class PSMethod : System.Management.Automation.PSMethodInfo {
    internal PSMethod() { }
    // Properties
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public override System.Collections.ObjectModel.Collection<string> OverloadDefinitions { get { return default(System.Collections.ObjectModel.Collection<string>); } }
    public override string TypeNameOfValue { get { return default(string); } }
     
    // Methods
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
    public override object Invoke(params object[] arguments) { return default(object); }
    public override string ToString() { return default(string); }
  }
  public abstract partial class PSMethodInfo : System.Management.Automation.PSMemberInfo {
    // Constructors
    protected PSMethodInfo() { }
     
    // Properties
    public abstract System.Collections.ObjectModel.Collection<string> OverloadDefinitions { get; }
    public sealed override object Value { get { return default(object); } set { } }
     
    // Methods
    public abstract object Invoke(params object[] arguments);
  }
  public enum PSModuleAutoLoadingPreference {
    // Fields
    All = 2,
    ModuleQualified = 1,
    None = 0,
  }
  public sealed partial class PSModuleInfo {
    // Constructors
    public PSModuleInfo(bool linkToGlobal) { }
    public PSModuleInfo(System.Management.Automation.ScriptBlock scriptBlock) { }
     
    // Properties
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
     
    // Methods
    public System.Management.Automation.PSObject AsCustomObject() { return default(System.Management.Automation.PSObject); }
    public static void ClearAppDomainLevelModulePathCache() { }
    public System.Management.Automation.PSModuleInfo Clone() { return default(System.Management.Automation.PSModuleInfo); }
    public object Invoke(System.Management.Automation.ScriptBlock sb, params object[] args) { return default(object); }
    public System.Management.Automation.ScriptBlock NewBoundScriptBlock(System.Management.Automation.ScriptBlock scriptBlockToBind) { return default(System.Management.Automation.ScriptBlock); }
    public override string ToString() { return default(string); }
  }
  public partial class PSNoteProperty : System.Management.Automation.PSPropertyInfo {
    // Constructors
    public PSNoteProperty(string name, object value) { }
     
    // Properties
    public override bool IsGettable { get { return default(bool); } }
    public override bool IsSettable { get { return default(bool); } }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public override string TypeNameOfValue { get { return default(string); } }
    public override object Value { get { return default(object); } set { } }
     
    // Methods
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString() { return default(string); }
  }
  public partial class PSNotImplementedException : System.NotImplementedException, System.Management.Automation.IContainsErrorRecord {
    // Constructors
    public PSNotImplementedException() { }
#if RUNTIME_SERIALIZATION
    protected PSNotImplementedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public PSNotImplementedException(string message) { }
    public PSNotImplementedException(string message, System.Exception innerException) { }
     
    // Properties
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
  }
  public partial class PSNotSupportedException : System.NotSupportedException, System.Management.Automation.IContainsErrorRecord {
    // Constructors
    public PSNotSupportedException() { }
#if RUNTIME_SERIALIZATION
    protected PSNotSupportedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public PSNotSupportedException(string message) { }
    public PSNotSupportedException(string message, System.Exception innerException) { }
     
    // Properties
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
  }
#if COMPONENT_MODEL
  [System.ComponentModel.TypeDescriptionProviderAttribute(typeof(System.Management.Automation.PSObjectTypeDescriptionProvider))]
#endif
  public partial class PSObject : System.Dynamic.IDynamicMetaObjectProvider, System.IComparable, System.IFormattable, System.Runtime.Serialization.ISerializable {
    // Fields
    public const string AdaptedMemberSetName = "psadapted";
    public const string BaseObjectMemberSetName = "psbase";
    public const string ExtendedMemberSetName = "psextended";
     
    // Constructors
    public PSObject() { }
    public PSObject(object obj) { }
    protected PSObject(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
     
    // Properties
    public object BaseObject { get { return default(object); } }
    public object ImmediateBaseObject { get { return default(object); } }
    public System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSMemberInfo> Members { get { return default(System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSMemberInfo>); } }
    public System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSMethodInfo> Methods { get { return default(System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSMethodInfo>); } }
    public System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSPropertyInfo> Properties { get { return default(System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSPropertyInfo>); } }
    public System.Collections.ObjectModel.Collection<string> TypeNames { get { return default(System.Collections.ObjectModel.Collection<string>); } }
     
    // Methods
    public static System.Management.Automation.PSObject AsPSObject(object obj) { return default(System.Management.Automation.PSObject); }
    public int CompareTo(object obj) { return default(int); }
    public virtual System.Management.Automation.PSObject Copy() { return default(System.Management.Automation.PSObject); }
    public override bool Equals(object obj) { return default(bool); }
    public override int GetHashCode() { return default(int); }
    public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public static implicit operator System.Management.Automation.PSObject (bool valueToConvert) { return default(System.Management.Automation.PSObject); }
    public static implicit operator System.Management.Automation.PSObject (System.Collections.Hashtable valueToConvert) { return default(System.Management.Automation.PSObject); }
    public static implicit operator System.Management.Automation.PSObject (double valueToConvert) { return default(System.Management.Automation.PSObject); }
    public static implicit operator System.Management.Automation.PSObject (int valueToConvert) { return default(System.Management.Automation.PSObject); }
    public static implicit operator System.Management.Automation.PSObject (string valueToConvert) { return default(System.Management.Automation.PSObject); }
    System.Dynamic.DynamicMetaObject System.Dynamic.IDynamicMetaObjectProvider.GetMetaObject(System.Linq.Expressions.Expression parameter) { return default(System.Dynamic.DynamicMetaObject); }
    public override string ToString() { return default(string); }
    public string ToString(string format, System.IFormatProvider formatProvider) { return default(string); }
  }
  public partial class PSObjectDisposedException : System.ObjectDisposedException, System.Management.Automation.IContainsErrorRecord {
    // Constructors
#if RUNTIME_SERIALIZATION
    protected PSObjectDisposedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public PSObjectDisposedException(string objectName) : base(objectName) { }
    public PSObjectDisposedException(string message, System.Exception innerException) : base(message, innerException) { }
    public PSObjectDisposedException(string objectName, string message) : base (objectName, message) { }
     
    // Properties
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
  }
#if COMPONENT_MODEL
  public partial class PSObjectPropertyDescriptor : System.ComponentModel.PropertyDescriptor {
    // Properties
    public override System.ComponentModel.AttributeCollection Attributes { get { return default(System.ComponentModel.AttributeCollection); } }
    public override System.Type ComponentType { get { return default(System.Type); } }
    public override bool IsReadOnly { get { return default(bool); } }
    public override System.Type PropertyType { get { return default(System.Type); } }
     
    // Methods
    public override bool CanResetValue(object component) { return default(bool); }
    public override object GetValue(object component) { return default(object); }
    public override void ResetValue(object component) { }
    public override void SetValue(object component, object value) { }
    public override bool ShouldSerializeValue(object component) { return default(bool); }
  }
  public partial class PSObjectTypeDescriptionProvider : System.ComponentModel.TypeDescriptionProvider {
    // Constructors
    public PSObjectTypeDescriptionProvider() { }
     
    // Events
    public event System.EventHandler<System.Management.Automation.GettingValueExceptionEventArgs> GettingValueException { add { } remove { } }
    public event System.EventHandler<System.Management.Automation.SettingValueExceptionEventArgs> SettingValueException { add { } remove { } }
     
    // Methods
    public override System.ComponentModel.ICustomTypeDescriptor GetTypeDescriptor(System.Type objectType, object instance) { return default(System.ComponentModel.ICustomTypeDescriptor); }
  }
  public partial class PSObjectTypeDescriptor : System.ComponentModel.CustomTypeDescriptor {
    // Constructors
    public PSObjectTypeDescriptor(System.Management.Automation.PSObject instance) { }
     
    // Properties
    public System.Management.Automation.PSObject Instance { get { return default(System.Management.Automation.PSObject); } }
     
    // Events
    public event System.EventHandler<System.Management.Automation.GettingValueExceptionEventArgs> GettingValueException { add { } remove { } }
    public event System.EventHandler<System.Management.Automation.SettingValueExceptionEventArgs> SettingValueException { add { } remove { } }
     
    // Methods
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
  public partial class PSParameterizedProperty : System.Management.Automation.PSMethodInfo {
    internal PSParameterizedProperty() { }
    // Properties
    public bool IsGettable { get { return default(bool); } }
    public bool IsSettable { get { return default(bool); } }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public override System.Collections.ObjectModel.Collection<string> OverloadDefinitions { get { return default(System.Collections.ObjectModel.Collection<string>); } }
    public override string TypeNameOfValue { get { return default(string); } }
     
    // Methods
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
    public override object Invoke(params object[] arguments) { return default(object); }
    public void InvokeSet(object valueToSet, params object[] arguments) { }
    public override string ToString() { return default(string); }
  }
  public sealed partial class PSParseError {
    internal PSParseError() { }
    // Properties
    public string Message { get { return default(string); } }
    public System.Management.Automation.PSToken Token { get { return default(System.Management.Automation.PSToken); } }
     
    // Methods
  }
  public sealed partial class PSParser {
    internal PSParser() { }
    // Methods
    public static System.Collections.ObjectModel.Collection<System.Management.Automation.PSToken> Tokenize(object[] script, out System.Collections.ObjectModel.Collection<System.Management.Automation.PSParseError> errors) { errors = default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSParseError>); return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSToken>); }
    public static System.Collections.ObjectModel.Collection<System.Management.Automation.PSToken> Tokenize(string script, out System.Collections.ObjectModel.Collection<System.Management.Automation.PSParseError> errors) { errors = default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSParseError>); return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSToken>); }
  }
  public sealed partial class PSPrimitiveDictionary : System.Collections.Hashtable {
    // Constructors
    public PSPrimitiveDictionary() { }
    public PSPrimitiveDictionary(System.Collections.Hashtable other) { }
     
    // Properties
    public override object this[object key] { get { return default(object); } set { } }
    public object this[string key] { get { return default(object); } set { } }
     
    // Methods
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
  public partial class PSProperty : System.Management.Automation.PSPropertyInfo {
    internal PSProperty() { }
    // Properties
    public override bool IsGettable { get { return default(bool); } }
    public override bool IsSettable { get { return default(bool); } }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public override string TypeNameOfValue { get { return default(string); } }
    public override object Value { get { return default(object); } set { } }
     
    // Methods
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString() { return default(string); }
  }
  public abstract partial class PSPropertyAdapter {
    // Constructors
    protected PSPropertyAdapter() { }
     
    // Methods
    public abstract System.Collections.ObjectModel.Collection<System.Management.Automation.PSAdaptedProperty> GetProperties(object baseObject);
    public abstract System.Management.Automation.PSAdaptedProperty GetProperty(object baseObject, string propertyName);
    public abstract string GetPropertyTypeName(System.Management.Automation.PSAdaptedProperty adaptedProperty);
    public abstract object GetPropertyValue(System.Management.Automation.PSAdaptedProperty adaptedProperty);
    public virtual System.Collections.ObjectModel.Collection<string> GetTypeNameHierarchy(object baseObject) { return default(System.Collections.ObjectModel.Collection<string>); }
    public abstract bool IsGettable(System.Management.Automation.PSAdaptedProperty adaptedProperty);
    public abstract bool IsSettable(System.Management.Automation.PSAdaptedProperty adaptedProperty);
    public abstract void SetPropertyValue(System.Management.Automation.PSAdaptedProperty adaptedProperty, object value);
  }
  public abstract partial class PSPropertyInfo : System.Management.Automation.PSMemberInfo {
    // Constructors
    protected PSPropertyInfo() { }
     
    // Properties
    public abstract bool IsGettable { get; }
    public abstract bool IsSettable { get; }
     
    // Methods
  }
  public partial class PSPropertySet : System.Management.Automation.PSMemberInfo {
    // Constructors
    public PSPropertySet(string name, System.Collections.Generic.IEnumerable<string> referencedPropertyNames) { }
     
    // Properties
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public System.Collections.ObjectModel.Collection<string> ReferencedPropertyNames { get { return default(System.Collections.ObjectModel.Collection<string>); } }
    public override string TypeNameOfValue { get { return default(string); } }
    public override object Value { get { return default(object); } set { } }
     
    // Methods
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString() { return default(string); }
  }
  public partial class PSReference {
    // Constructors
    public PSReference(object value) { }
     
    // Properties
    public object Value { get { return default(object); } set { } }
     
    // Methods
  }
  public partial class PSScriptMethod : System.Management.Automation.PSMethodInfo {
    // Constructors
    public PSScriptMethod(string name, System.Management.Automation.ScriptBlock script) { }
     
    // Properties
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public override System.Collections.ObjectModel.Collection<string> OverloadDefinitions { get { return default(System.Collections.ObjectModel.Collection<string>); } }
    public System.Management.Automation.ScriptBlock Script { get { return default(System.Management.Automation.ScriptBlock); } }
    public override string TypeNameOfValue { get { return default(string); } }
     
    // Methods
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
    public override object Invoke(params object[] arguments) { return default(object); }
    public override string ToString() { return default(string); }
  }
  public partial class PSScriptProperty : System.Management.Automation.PSPropertyInfo {
    // Constructors
    public PSScriptProperty(string name, System.Management.Automation.ScriptBlock getterScript) { }
    public PSScriptProperty(string name, System.Management.Automation.ScriptBlock getterScript, System.Management.Automation.ScriptBlock setterScript) { }
     
    // Properties
    public System.Management.Automation.ScriptBlock GetterScript { get { return default(System.Management.Automation.ScriptBlock); } }
    public override bool IsGettable { get { return default(bool); } }
    public override bool IsSettable { get { return default(bool); } }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public System.Management.Automation.ScriptBlock SetterScript { get { return default(System.Management.Automation.ScriptBlock); } }
    public override string TypeNameOfValue { get { return default(string); } }
    public override object Value { get { return default(object); } set { } }
     
    // Methods
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString() { return default(string); }
  }
  public partial class PSSecurityException : System.Management.Automation.RuntimeException {
    // Constructors
    public PSSecurityException() { }
#if RUNTIME_SERIALIZATION
    protected PSSecurityException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public PSSecurityException(string message) { }
    public PSSecurityException(string message, System.Exception innerException) { }
     
    // Properties
    public override System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override string Message { get { return default(string); } }
     
    // Methods
  }
  public partial class PSSerializer {
    internal PSSerializer() { }
    // Methods
    public static object Deserialize(string source) { return default(object); }
    public static object[] DeserializeAsList(string source) { return default(object[]); }
    public static string Serialize(object source) { return default(string); }
    public static string Serialize(object source, int depth) { return default(string); }
  }
  public abstract partial class PSSessionTypeOption {
    // Constructors
    protected PSSessionTypeOption() { }
     
    // Methods
    protected internal virtual System.Management.Automation.PSSessionTypeOption ConstructObjectFromPrivateData(string privateData) { return default(System.Management.Automation.PSSessionTypeOption); }
    protected internal virtual string ConstructPrivateData() { return default(string); }
    protected internal virtual void CopyUpdatedValuesFrom(System.Management.Automation.PSSessionTypeOption updated) { }
  }
#if SNAPINS
  public abstract partial class PSSnapIn : System.Management.Automation.PSSnapInInstaller {
    // Constructors
    protected PSSnapIn() { }
     
    // Properties
    public virtual string[] Formats { get { return default(string[]); } }
    public virtual string[] Types { get { return default(string[]); } }
     
    // Methods
  }
#endif
  public partial class PSSnapInInfo {
    internal PSSnapInInfo() { }
    // Properties
    public string ApplicationBase { get { return default(string); } }
    public string AssemblyName { get { return default(string); } }
    public string Description { get { return default(string); } }
    public System.Collections.ObjectModel.Collection<string> Formats { get { return default(System.Collections.ObjectModel.Collection<string>); } }
    public bool IsDefault { get { return default(bool); } }
    public bool LogPipelineExecutionDetails { get { return default(bool); } set { } }
    public string ModuleName { get { return default(string); } }
    public string Name { get { return default(string); } }
    public System.Version PSVersion { get { return default(System.Version); } }
    public System.Collections.ObjectModel.Collection<string> Types { get { return default(System.Collections.ObjectModel.Collection<string>); } }
    public string Vendor { get { return default(string); } }
    public System.Version Version { get { return default(System.Version); } }
     
    // Methods
    public override string ToString() { return default(string); }
  }
#if SNAPINS
  public abstract partial class PSSnapInInstaller : System.Management.Automation.PSInstaller {
    // Constructors
    protected PSSnapInInstaller() { }
     
    // Properties
    public abstract string Description { get; }
    public virtual string DescriptionResource { get { return default(string); } }
    public abstract string Name { get; }
    public abstract string Vendor { get; }
    public virtual string VendorResource { get { return default(string); } }
     
    // Methods
  }
#endif
  public partial class PSSnapInSpecification {
    internal PSSnapInSpecification() { }
    // Properties
    public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
    public System.Version Version { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Version); } }
     
    // Methods
  }
  public sealed partial class PSToken {
    internal PSToken() { }
    // Properties
    public string Content { get { return default(string); } }
    public int EndColumn { get { return default(int); } }
    public int EndLine { get { return default(int); } }
    public int Length { get { return default(int); } }
    public int Start { get { return default(int); } }
    public int StartColumn { get { return default(int); } }
    public int StartLine { get { return default(int); } }
    public System.Management.Automation.PSTokenType Type { get { return default(System.Management.Automation.PSTokenType); } }
     
    // Methods
    public static System.Management.Automation.PSTokenType GetPSTokenType(System.Management.Automation.Language.Token token) { return default(System.Management.Automation.PSTokenType); }
  }
  public enum PSTokenType {
    // Fields
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
  public partial class PSTraceSource {
    internal PSTraceSource() { }
    // Properties
    public System.Collections.Specialized.StringDictionary Attributes { get { return default(System.Collections.Specialized.StringDictionary); } }
    public string Description { get { return default(string); } set { } }
    public System.Diagnostics.TraceListenerCollection Listeners { get { return default(System.Diagnostics.TraceListenerCollection); } }
    public string Name { get { return default(string); } }
    public System.Management.Automation.PSTraceSourceOptions Options { get { return default(System.Management.Automation.PSTraceSourceOptions); } set { } }
    public System.Diagnostics.SourceSwitch Switch { get { return default(System.Diagnostics.SourceSwitch); } set { } }
     
    // Methods
  }
  [System.FlagsAttribute]
  public enum PSTraceSourceOptions {
    // Fields
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
  public sealed partial class PSTransaction : System.IDisposable {
    internal PSTransaction() { }
    // Properties
    public System.Management.Automation.RollbackSeverity RollbackPreference { get { return default(System.Management.Automation.RollbackSeverity); } }
    public System.Management.Automation.PSTransactionStatus Status { get { return default(System.Management.Automation.PSTransactionStatus); } }
    public int SubscriberCount { get { return default(int); } set { } }
     
    // Methods
    public void Dispose() { }
    public void Dispose(bool disposing) { }
    ~PSTransaction() { }
  }
  public sealed partial class PSTransactionContext : System.IDisposable {
    internal PSTransactionContext() { }
    // Methods
    public void Dispose() { }
    ~PSTransactionContext() { }
  }
  public enum PSTransactionStatus {
    // Fields
    Active = 2,
    Committed = 1,
    RolledBack = 0,
  }
  public abstract partial class PSTransportOption
#if CLONEABLE
        : System.ICloneable
#endif
  {
    // Constructors
    protected PSTransportOption() { }
     
    // Methods
#if CLONEABLE
    public object Clone() { return default(object); }
#endif
    protected internal virtual void LoadFromDefaults(System.Management.Automation.Runspaces.PSSessionType sessionType, bool keepAssigned) { }
  }
  public abstract partial class PSTypeConverter {
    // Constructors
    protected PSTypeConverter() { }
     
    // Methods
    public virtual bool CanConvertFrom(System.Management.Automation.PSObject sourceValue, System.Type destinationType) { return default(bool); }
    public abstract bool CanConvertFrom(object sourceValue, System.Type destinationType);
    public virtual bool CanConvertTo(System.Management.Automation.PSObject sourceValue, System.Type destinationType) { return default(bool); }
    public abstract bool CanConvertTo(object sourceValue, System.Type destinationType);
    public virtual object ConvertFrom(System.Management.Automation.PSObject sourceValue, System.Type destinationType, System.IFormatProvider formatProvider, bool ignoreCase) { return default(object); }
    public abstract object ConvertFrom(object sourceValue, System.Type destinationType, System.IFormatProvider formatProvider, bool ignoreCase);
    public virtual object ConvertTo(System.Management.Automation.PSObject sourceValue, System.Type destinationType, System.IFormatProvider formatProvider, bool ignoreCase) { return default(object); }
    public abstract object ConvertTo(object sourceValue, System.Type destinationType, System.IFormatProvider formatProvider, bool ignoreCase);
  }
  public partial class PSTypeName {
    // Constructors
    public PSTypeName(string name) { }
    public PSTypeName(System.Type type) { }
     
    // Properties
    public string Name { get { return default(string); } }
    public System.Type Type { get { return default(System.Type); } }
     
    // Methods
    public override string ToString() { return default(string); }
  }
  [System.AttributeUsageAttribute((AttributeTargets)384, AllowMultiple=false)]
  public partial class PSTypeNameAttribute : System.Attribute {
    // Constructors
    public PSTypeNameAttribute(string psTypeName) { }
     
    // Properties
    public string PSTypeName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
     
    // Methods
  }
  public partial class PSVariable {
    // Constructors
    public PSVariable(string name) { }
    public PSVariable(string name, object value) { }
    public PSVariable(string name, object value, System.Management.Automation.ScopedItemOptions options) { }
    public PSVariable(string name, object value, System.Management.Automation.ScopedItemOptions options, System.Collections.ObjectModel.Collection<System.Attribute> attributes) { }
     
    // Properties
    public System.Collections.ObjectModel.Collection<System.Attribute> Attributes { get { return default(System.Collections.ObjectModel.Collection<System.Attribute>); } }
    public virtual string Description { get { return default(string); } set { } }
    public System.Management.Automation.PSModuleInfo Module { get { return default(System.Management.Automation.PSModuleInfo); } }
    public string ModuleName { get { return default(string); } }
    public string Name { get { return default(string); } }
    public virtual System.Management.Automation.ScopedItemOptions Options { get { return default(System.Management.Automation.ScopedItemOptions); } set { } }
    public virtual object Value { get { return default(object); } set { } }
    public System.Management.Automation.SessionStateEntryVisibility Visibility { get { return default(System.Management.Automation.SessionStateEntryVisibility); } set { } }
     
    // Methods
    public virtual bool IsValidValue(object value) { return default(bool); }
  }
  public sealed partial class PSVariableIntrinsics {
    internal PSVariableIntrinsics() { }
    // Methods
    public System.Management.Automation.PSVariable Get(string name) { return default(System.Management.Automation.PSVariable); }
    public object GetValue(string name) { return default(object); }
    public object GetValue(string name, object defaultValue) { return default(object); }
    public void Remove(System.Management.Automation.PSVariable variable) { }
    public void Remove(string name) { }
    public void Set(System.Management.Automation.PSVariable variable) { }
    public void Set(string name, object value) { }
  }
  public partial class PSVariableProperty : System.Management.Automation.PSNoteProperty {
    // Constructors
    public PSVariableProperty(System.Management.Automation.PSVariable variable) : base (default(string), default(object)) { }
     
    // Properties
    public override bool IsGettable { get { return default(bool); } }
    public override bool IsSettable { get { return default(bool); } }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public override string TypeNameOfValue { get { return default(string); } }
    public override object Value { get { return default(object); } set { } }
     
    // Methods
    public override System.Management.Automation.PSMemberInfo Copy() { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString() { return default(string); }
  }
  public partial class ReadOnlyPSMemberInfoCollection<T> : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable where T : System.Management.Automation.PSMemberInfo {
    internal ReadOnlyPSMemberInfoCollection() { }
    // Properties
    public int Count { get { return default(int); } }
    public T this[int index] { get { return default(T); } }
    public T this[string name] { get { return default(T); } }
     
    // Methods
    public virtual System.Collections.Generic.IEnumerator<T> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<T>); }
    public System.Management.Automation.ReadOnlyPSMemberInfoCollection<T> Match(string name) { return default(System.Management.Automation.ReadOnlyPSMemberInfoCollection<T>); }
    public System.Management.Automation.ReadOnlyPSMemberInfoCollection<T> Match(string name, System.Management.Automation.PSMemberTypes memberTypes) { return default(System.Management.Automation.ReadOnlyPSMemberInfoCollection<T>); }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
  }
  public partial class RedirectedException : System.Management.Automation.RuntimeException {
    // Constructors
    public RedirectedException() { }
#if RUNTIME_SERIALIZATION
    protected RedirectedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public RedirectedException(string message) { }
    public RedirectedException(string message, System.Exception innerException) { }
  }
  public partial class RemoteCommandInfo : System.Management.Automation.CommandInfo {
    internal RemoteCommandInfo() { }
    // Properties
    public override string Definition { get { return default(string); } }
    public override System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName> OutputType { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName>); } }
     
    // Methods
  }
  public partial class RemoteException : System.Management.Automation.RuntimeException {
    // Constructors
    public RemoteException() { }
#if RUNTIME_SERIALIZATION
    protected RemoteException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public RemoteException(string message) { }
    public RemoteException(string message, System.Exception innerException) { }
     
    // Properties
    public override System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public System.Management.Automation.PSObject SerializedRemoteException { get { return default(System.Management.Automation.PSObject); } }
    public System.Management.Automation.PSObject SerializedRemoteInvocationInfo { get { return default(System.Management.Automation.PSObject); } }
     
    // Methods
  }
  [System.FlagsAttribute]
  public enum RemoteStreamOptions {
    // Fields
    AddInvocationInfo = 15,
    AddInvocationInfoToDebugRecord = 4,
    AddInvocationInfoToErrorRecord = 1,
    AddInvocationInfoToVerboseRecord = 8,
    AddInvocationInfoToWarningRecord = 2,
  }
  public enum RemotingBehavior {
    // Fields
    Custom = 2,
    None = 0,
    PowerShell = 1,
  }
  public enum RemotingCapability {
    // Fields
    None = 0,
    OwnedByCommand = 3,
    PowerShell = 1,
    SupportedByCommand = 2,
  }
  public abstract partial class Repository<T> where T : class {
    // Constructors
    protected Repository(string identifier) { }
     
    // Methods
    public void Add(T item) { }
    public T GetItem(System.Guid instanceId) { return default(T); }
    public System.Collections.Generic.List<T> GetItems() { return default(System.Collections.Generic.List<T>); }
    protected abstract System.Guid GetKey(T item);
    public void Remove(T item) { }
  }
  public enum ReturnContainers {
    // Fields
    ReturnAllContainers = 1,
    ReturnMatchingContainers = 0,
  }
  public enum RollbackSeverity {
    // Fields
    Error = 0,
    Never = 2,
    TerminatingError = 1,
  }
  public partial class RunspaceInvoke : System.IDisposable {
    // Constructors
    public RunspaceInvoke() { }
    public RunspaceInvoke(System.Management.Automation.Runspaces.Runspace runspace) { }
    public RunspaceInvoke(System.Management.Automation.Runspaces.RunspaceConfiguration runspaceConfiguration) { }
    public RunspaceInvoke(string consoleFilePath) { }
     
    // Methods
    public void Dispose() { }
    protected virtual void Dispose(bool disposing) { }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Invoke(string script) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Invoke(string script, System.Collections.IEnumerable input) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Invoke(string script, System.Collections.IEnumerable input, out System.Collections.IList errors) { errors = default(System.Collections.IList); return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
  }
  public enum RunspaceMode {
    // Fields
    CurrentRunspace = 0,
    NewRunspace = 1,
  }
  public sealed partial class RunspacePoolStateInfo {
    // Constructors
    public RunspacePoolStateInfo(System.Management.Automation.Runspaces.RunspacePoolState state, System.Exception reason) { }
     
    // Properties
    public System.Exception Reason { get { return default(System.Exception); } }
    public System.Management.Automation.Runspaces.RunspacePoolState State { get { return default(System.Management.Automation.Runspaces.RunspacePoolState); } }
     
    // Methods
  }
  public partial class RunspaceRepository : System.Management.Automation.Repository<System.Management.Automation.Runspaces.PSSession> {
    internal RunspaceRepository() : base (default(string)) { }
    // Properties
    public System.Collections.Generic.List<System.Management.Automation.Runspaces.PSSession> Runspaces { get { return default(System.Collections.Generic.List<System.Management.Automation.Runspaces.PSSession>); } }
     
    // Methods
    protected override System.Guid GetKey(System.Management.Automation.Runspaces.PSSession item) { return default(System.Guid); }
  }
  public partial class RuntimeDefinedParameter {
    // Constructors
    public RuntimeDefinedParameter() { }
    public RuntimeDefinedParameter(string name, System.Type parameterType, System.Collections.ObjectModel.Collection<System.Attribute> attributes) { }
     
    // Properties
    public System.Collections.ObjectModel.Collection<System.Attribute> Attributes { get { return default(System.Collections.ObjectModel.Collection<System.Attribute>); } }
    public bool IsSet { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public string Name { get { return default(string); } set { } }
    public System.Type ParameterType { get { return default(System.Type); } set { } }
    public object Value { get { return default(object); } set { } }
     
    // Methods
  }
  public partial class RuntimeDefinedParameterDictionary : System.Collections.Generic.Dictionary<string, System.Management.Automation.RuntimeDefinedParameter> {
    // Constructors
    public RuntimeDefinedParameterDictionary() { }
     
    // Properties
    public object Data { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(object); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public string HelpFile { get { return default(string); } set { } }
     
    // Methods
  }
  public partial class RuntimeException
#if RUNTIME_SERIALIZATION
        : System.SystemException
#else
        : System.Exception
#endif
        , System.Management.Automation.IContainsErrorRecord
    {
    // Constructors
    public RuntimeException() { }
#if RUNTIME_SERIALIZATION
    protected RuntimeException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public RuntimeException(string message) { }
    public RuntimeException(string message, System.Exception innerException) { }
    public RuntimeException(string message, System.Exception innerException, System.Management.Automation.ErrorRecord errorRecord) { }
     
    // Properties
    public virtual System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override string StackTrace { get { return default(string); } }
    public bool WasThrownFromThrowStatement { get { return default(bool); } set { } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
  }
  [System.FlagsAttribute]
  public enum ScopedItemOptions {
    // Fields
    AllScope = 8,
    Constant = 2,
    None = 0,
    Private = 4,
    ReadOnly = 1,
    Unspecified = 16,
  }
  public partial class ScriptBlock : System.Runtime.Serialization.ISerializable {
    // Constructors
    protected ScriptBlock(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
     
    // Properties
    public System.Management.Automation.Language.Ast Ast { get { return default(System.Management.Automation.Language.Ast); } }
    public System.Collections.Generic.List<System.Attribute> Attributes { get { return default(System.Collections.Generic.List<System.Attribute>); } }
    public string File { get { return default(string); } }
    public bool IsFilter { get { return default(bool); } set { } }
    public System.Management.Automation.PSModuleInfo Module { get { return default(System.Management.Automation.PSModuleInfo); } }
    public System.Management.Automation.PSToken StartPosition { get { return default(System.Management.Automation.PSToken); } }
     
    // Methods
    public void CheckRestrictedLanguage(System.Collections.Generic.IEnumerable<string> allowedCommands, System.Collections.Generic.IEnumerable<string> allowedVariables, bool allowEnvironmentVariables) { }
    public static System.Management.Automation.ScriptBlock Create(string script) { return default(System.Management.Automation.ScriptBlock); }
    public System.Management.Automation.ScriptBlock GetNewClosure() { return default(System.Management.Automation.ScriptBlock); }
    public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public System.Management.Automation.PowerShell GetPowerShell(System.Collections.Generic.Dictionary<string, object> variables, out System.Collections.Generic.Dictionary<string, object> usingVariables, params object[] args) { usingVariables = default(System.Collections.Generic.Dictionary<string, object>); return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell GetPowerShell(System.Collections.Generic.Dictionary<string, object> variables, params object[] args) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell GetPowerShell(params object[] args) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.SteppablePipeline GetSteppablePipeline() { return default(System.Management.Automation.SteppablePipeline); }
    public System.Management.Automation.SteppablePipeline GetSteppablePipeline(System.Management.Automation.CommandOrigin commandOrigin) { return default(System.Management.Automation.SteppablePipeline); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Invoke(params object[] args) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public object InvokeReturnAsIs(params object[] args) { return default(object); }
    public override string ToString() { return default(string); }
  }
  public partial class ScriptBlockToPowerShellNotSupportedException : System.Management.Automation.RuntimeException {
    // Constructors
    public ScriptBlockToPowerShellNotSupportedException() { }
#if RUNTIME_SERIALIZATION
    protected ScriptBlockToPowerShellNotSupportedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public ScriptBlockToPowerShellNotSupportedException(string message) { }
    public ScriptBlockToPowerShellNotSupportedException(string message, System.Exception innerException) { }
  }
  public partial class ScriptCallDepthException
#if RUNTIME_SERIALIZATION
        : System.SystemException
#else
        : System.Exception
#endif
        , System.Management.Automation.IContainsErrorRecord
    {
    // Constructors
    public ScriptCallDepthException() { }
#if RUNTIME_SERIALIZATION
    protected ScriptCallDepthException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public ScriptCallDepthException(string message) { }
    public ScriptCallDepthException(string message, System.Exception innerException) { }
     
    // Properties
    public int CallDepth { get { return default(int); } }
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
  }
  public partial class ScriptInfo : System.Management.Automation.CommandInfo {
    internal ScriptInfo() { }
    // Properties
    public override string Definition { get { return default(string); } }
    public override System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName> OutputType { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName>); } }
    public System.Management.Automation.ScriptBlock ScriptBlock { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.ScriptBlock); } }
     
    // Methods
    public override string ToString() { return default(string); }
  }
  public partial class ScriptRequiresException : System.Management.Automation.RuntimeException {
    // Constructors
    public ScriptRequiresException() { }
#if RUNTIME_SERIALIZATION
    protected ScriptRequiresException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public ScriptRequiresException(string message) { }
    public ScriptRequiresException(string message, System.Exception innerException) { }
     
    // Properties
    public string CommandName { get { return default(string); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<string> MissingPSSnapIns { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<string>); } }
    public System.Version RequiresPSVersion { get { return default(System.Version); } }
    public string RequiresShellId { get { return default(string); } }
    public string RequiresShellPath { get { return default(string); } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
  }
  public sealed partial class SecurityDescriptorCmdletProviderIntrinsics {
    internal SecurityDescriptorCmdletProviderIntrinsics() { }
    // Methods
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Get(string path, System.Security.AccessControl.AccessControlSections includeSections) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Security.AccessControl.ObjectSecurity NewFromPath(string path, System.Security.AccessControl.AccessControlSections includeSections) { return default(System.Security.AccessControl.ObjectSecurity); }
    public System.Security.AccessControl.ObjectSecurity NewOfType(string providerId, string type, System.Security.AccessControl.AccessControlSections includeSections) { return default(System.Security.AccessControl.ObjectSecurity); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Set(string path, System.Security.AccessControl.ObjectSecurity sd) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
  }
  [System.FlagsAttribute]
  public enum SessionCapabilities {
    // Fields
    Language = 4,
    RemoteServer = 1,
    WorkflowServer = 2,
  }
  public sealed partial class SessionState {
    // Constructors
    public SessionState() { }
     
    // Properties
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
     
    // Methods
    public static bool IsVisible(System.Management.Automation.CommandOrigin origin, System.Management.Automation.CommandInfo commandInfo) { return default(bool); }
    public static bool IsVisible(System.Management.Automation.CommandOrigin origin, System.Management.Automation.PSVariable variable) { return default(bool); }
    public static bool IsVisible(System.Management.Automation.CommandOrigin origin, object valueToCheck) { return default(bool); }
    public static void ThrowIfNotVisible(System.Management.Automation.CommandOrigin origin, object valueToCheck) { }
  }
  public enum SessionStateCategory {
    // Fields
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
    // Fields
    Private = 1,
    Public = 0,
  }
  public partial class SessionStateException : System.Management.Automation.RuntimeException {
    // Constructors
    public SessionStateException() { }
#if RUNTIME_SERIALIZATION
    protected SessionStateException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public SessionStateException(string message) { }
    public SessionStateException(string message, System.Exception innerException) { }
     
    // Properties
    public override System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public string ItemName { get { return default(string); } }
    public System.Management.Automation.SessionStateCategory SessionStateCategory { get { return default(System.Management.Automation.SessionStateCategory); } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
  }
  public partial class SessionStateOverflowException : System.Management.Automation.SessionStateException {
    // Constructors
    public SessionStateOverflowException() { }
#if RUNTIME_SERIALIZATION
    protected SessionStateOverflowException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public SessionStateOverflowException(string message) { }
    public SessionStateOverflowException(string message, System.Exception innerException) { }
  }
  public partial class SessionStateUnauthorizedAccessException : System.Management.Automation.SessionStateException {
    // Constructors
    public SessionStateUnauthorizedAccessException() { }
#if RUNTIME_SERIALIZATION
    protected SessionStateUnauthorizedAccessException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public SessionStateUnauthorizedAccessException(string message) { }
    public SessionStateUnauthorizedAccessException(string message, System.Exception innerException) { }
  }
  public partial class SettingValueExceptionEventArgs : System.EventArgs {
    internal SettingValueExceptionEventArgs() { }
    // Properties
    public System.Exception Exception { get { return default(System.Exception); } }
    public bool ShouldThrow { get { return default(bool); } set { } }
     
    // Methods
  }
  public partial class SetValueException : System.Management.Automation.ExtendedTypeSystemException {
    // Constructors
    public SetValueException() { }
#if RUNTIME_SERIALIZATION
    protected SetValueException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public SetValueException(string message) { }
    public SetValueException(string message, System.Exception innerException) { }
  }
  public partial class SetValueInvocationException : System.Management.Automation.SetValueException {
    // Constructors
    public SetValueInvocationException() { }
#if RUNTIME_SERIALIZATION
    protected SetValueInvocationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public SetValueInvocationException(string message) { }
    public SetValueInvocationException(string message, System.Exception innerException) { }
  }
  [System.FlagsAttribute]
  public enum ShouldProcessReason {
    // Fields
    None = 0,
    WhatIf = 1,
  }
  public sealed partial class Signature {
    internal Signature() { }
    // Properties
    public string Path { get { return default(string); } }
    public System.Security.Cryptography.X509Certificates.X509Certificate2 SignerCertificate { get { return default(System.Security.Cryptography.X509Certificates.X509Certificate2); } }
    public System.Management.Automation.SignatureStatus Status { get { return default(System.Management.Automation.SignatureStatus); } }
    public string StatusMessage { get { return default(string); } }
    public System.Security.Cryptography.X509Certificates.X509Certificate2 TimeStamperCertificate { get { return default(System.Security.Cryptography.X509Certificates.X509Certificate2); } }
     
    // Methods
  }
  public enum SignatureStatus {
    // Fields
    HashMismatch = 3,
    Incompatible = 6,
    NotSigned = 2,
    NotSupportedFileFormat = 5,
    NotTrusted = 4,
    UnknownError = 1,
    Valid = 0,
  }
  public enum SigningOption {
    // Fields
    AddFullCertificateChain = 1,
    AddFullCertificateChainExceptRoot = 2,
    AddOnlyCertificate = 0,
    Default = 2,
  }
  [System.FlagsAttribute]
  public enum SplitOptions {
    // Fields
    CultureInvariant = 4,
    ExplicitCapture = 128,
    IgnoreCase = 64,
    IgnorePatternWhitespace = 8,
    Multiline = 16,
    RegexMatch = 2,
    SimpleMatch = 1,
    Singleline = 32,
  }
  public sealed partial class SteppablePipeline : System.IDisposable {
    internal SteppablePipeline() { }
    // Methods
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
  public sealed partial class SupportsWildcardsAttribute : System.Management.Automation.Internal.ParsingBaseAttribute {
    // Constructors
    public SupportsWildcardsAttribute() { }
  }
  [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
  public partial struct SwitchParameter {
    // Constructors
    public SwitchParameter(bool isPresent) { throw new System.NotImplementedException(); }
     
    // Properties
    public bool IsPresent { get { return default(bool); } }
    public static System.Management.Automation.SwitchParameter Present { get { return default(System.Management.Automation.SwitchParameter); } }
     
    // Methods
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
  public sealed partial class TableControl : System.Management.Automation.PSControl {
    // Constructors
    public TableControl() { }
    public TableControl(System.Management.Automation.TableControlRow tableControlRow) { }
    public TableControl(System.Management.Automation.TableControlRow tableControlRow, System.Collections.Generic.IEnumerable<System.Management.Automation.TableControlColumnHeader> tableControlColumnHeaders) { }
     
    // Properties
    public System.Collections.Generic.List<System.Management.Automation.TableControlColumnHeader> Headers { get { return default(System.Collections.Generic.List<System.Management.Automation.TableControlColumnHeader>); } }
    public System.Collections.Generic.List<System.Management.Automation.TableControlRow> Rows { get { return default(System.Collections.Generic.List<System.Management.Automation.TableControlRow>); } }
     
    // Methods
    public override string ToString() { return default(string); }
  }
  public sealed partial class TableControlColumn {
    // Constructors
    public TableControlColumn(System.Management.Automation.Alignment alignment, System.Management.Automation.DisplayEntry entry) { }
     
    // Properties
    public System.Management.Automation.Alignment Alignment { get { return default(System.Management.Automation.Alignment); } }
    public System.Management.Automation.DisplayEntry DisplayEntry { get { return default(System.Management.Automation.DisplayEntry); } }
     
    // Methods
    public override string ToString() { return default(string); }
  }
  public sealed partial class TableControlColumnHeader {
    // Constructors
    public TableControlColumnHeader(string label, int width, System.Management.Automation.Alignment alignment) { }
     
    // Properties
    public System.Management.Automation.Alignment Alignment { get { return default(System.Management.Automation.Alignment); } }
    public string Label { get { return default(string); } }
    public int Width { get { return default(int); } }
     
    // Methods
  }
  public sealed partial class TableControlRow {
    // Constructors
    public TableControlRow() { }
    public TableControlRow(System.Collections.Generic.IEnumerable<System.Management.Automation.TableControlColumn> columns) { }
     
    // Properties
    public System.Collections.Generic.List<System.Management.Automation.TableControlColumn> Columns { get { return default(System.Collections.Generic.List<System.Management.Automation.TableControlColumn>); } }
     
    // Methods
  }
  [System.AttributeUsageAttribute((AttributeTargets)384)]
  public abstract partial class ValidateArgumentsAttribute : System.Management.Automation.Internal.CmdletMetadataAttribute {
    // Constructors
    protected ValidateArgumentsAttribute() { }
     
    // Methods
    protected abstract void Validate(object arguments, System.Management.Automation.EngineIntrinsics engineIntrinsics);
  }
  [System.AttributeUsageAttribute((AttributeTargets)384)]
  public sealed partial class ValidateCountAttribute : System.Management.Automation.ValidateArgumentsAttribute {
    // Constructors
    public ValidateCountAttribute(int minLength, int maxLength) { }
     
    // Properties
    public int MaxLength { get { return default(int); } }
    public int MinLength { get { return default(int); } }
     
    // Methods
    protected override void Validate(object arguments, System.Management.Automation.EngineIntrinsics engineIntrinsics) { }
  }
  [System.AttributeUsageAttribute((AttributeTargets)384)]
  public abstract partial class ValidateEnumeratedArgumentsAttribute : System.Management.Automation.ValidateArgumentsAttribute {
    // Constructors
    protected ValidateEnumeratedArgumentsAttribute() { }
     
    // Methods
    protected sealed override void Validate(object arguments, System.Management.Automation.EngineIntrinsics engineIntrinsics) { }
    protected abstract void ValidateElement(object element);
  }
  [System.AttributeUsageAttribute((AttributeTargets)384)]
  public sealed partial class ValidateLengthAttribute : System.Management.Automation.ValidateEnumeratedArgumentsAttribute {
    // Constructors
    public ValidateLengthAttribute(int minLength, int maxLength) { }
     
    // Properties
    public int MaxLength { get { return default(int); } }
    public int MinLength { get { return default(int); } }
     
    // Methods
    protected override void ValidateElement(object element) { }
  }
  [System.AttributeUsageAttribute((AttributeTargets)384)]
  public sealed partial class ValidateNotNullAttribute : System.Management.Automation.ValidateArgumentsAttribute {
    // Constructors
    public ValidateNotNullAttribute() { }
     
    // Methods
    protected override void Validate(object arguments, System.Management.Automation.EngineIntrinsics engineIntrinsics) { }
  }
  [System.AttributeUsageAttribute((AttributeTargets)384)]
  public sealed partial class ValidateNotNullOrEmptyAttribute : System.Management.Automation.ValidateArgumentsAttribute {
    // Constructors
    public ValidateNotNullOrEmptyAttribute() { }
     
    // Methods
    protected override void Validate(object arguments, System.Management.Automation.EngineIntrinsics engineIntrinsics) { }
  }
  [System.AttributeUsageAttribute((AttributeTargets)384)]
  public sealed partial class ValidatePatternAttribute : System.Management.Automation.ValidateEnumeratedArgumentsAttribute {
    // Constructors
    public ValidatePatternAttribute(string regexPattern) { }
     
    // Properties
    public System.Text.RegularExpressions.RegexOptions Options { get { return default(System.Text.RegularExpressions.RegexOptions); } set { } }
    public string RegexPattern { get { return default(string); } }
     
    // Methods
    protected override void ValidateElement(object element) { }
  }
  [System.AttributeUsageAttribute((AttributeTargets)384)]
  public sealed partial class ValidateRangeAttribute : System.Management.Automation.ValidateEnumeratedArgumentsAttribute {
    // Constructors
    public ValidateRangeAttribute(object minRange, object maxRange) { }
     
    // Properties
    public object MaxRange { get { return default(object); } }
    public object MinRange { get { return default(object); } }
     
    // Methods
    protected override void ValidateElement(object element) { }
  }
  public sealed partial class ValidateScriptAttribute : System.Management.Automation.ValidateEnumeratedArgumentsAttribute {
    // Constructors
    public ValidateScriptAttribute(System.Management.Automation.ScriptBlock scriptBlock) { }
     
    // Properties
    public System.Management.Automation.ScriptBlock ScriptBlock { get { return default(System.Management.Automation.ScriptBlock); } }
     
    // Methods
    protected override void ValidateElement(object element) { }
  }
  [System.AttributeUsageAttribute((AttributeTargets)384)]
  public sealed partial class ValidateSetAttribute : System.Management.Automation.ValidateEnumeratedArgumentsAttribute {
    // Constructors
    public ValidateSetAttribute(params string[] validValues) { }
     
    // Properties
    public bool IgnoreCase { get { return default(bool); } set { } }
    public System.Collections.Generic.IList<string> ValidValues { get { return default(System.Collections.Generic.IList<string>); } }
     
    // Methods
    protected override void ValidateElement(object element) { }
  }
  public partial class ValidationMetadataException : System.Management.Automation.MetadataException {
    // Constructors
    public ValidationMetadataException() { }
#if RUNTIME_SERIALIZATION
    protected ValidationMetadataException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public ValidationMetadataException(string message) { }
    public ValidationMetadataException(string message, System.Exception innerException) { }
  }
  public enum VariableAccessMode {
    // Fields
    Read = 0,
    ReadWrite = 2,
    Write = 1,
  }
  public partial class VariableBreakpoint : System.Management.Automation.Breakpoint {
    internal VariableBreakpoint() { }
    // Properties
    public System.Management.Automation.VariableAccessMode AccessMode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.VariableAccessMode); } }
    public string Variable { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
     
    // Methods
    public override string ToString() { return default(string); }
  }
  public partial class VariablePath {
    // Constructors
    public VariablePath(string path) { }
     
    // Properties
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
     
    // Methods
    public override string ToString() { return default(string); }
  }
  [System.Runtime.Serialization.DataContractAttribute]
  public partial class VerboseRecord : System.Management.Automation.InformationalRecord {
    // Constructors
    public VerboseRecord(System.Management.Automation.PSObject record) { }
    public VerboseRecord(string message) { }
  }
  public static partial class VerbsCommon {
    // Fields
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
  public static partial class VerbsCommunications {
    // Fields
    public const string Connect = "Connect";
    public const string Disconnect = "Disconnect";
    public const string Read = "Read";
    public const string Receive = "Receive";
    public const string Send = "Send";
    public const string Write = "Write";
  }
  public static partial class VerbsData {
    // Fields
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
  public static partial class VerbsDiagnostic {
    // Fields
    public const string Debug = "Debug";
    public const string Measure = "Measure";
    public const string Ping = "Ping";
    public const string Repair = "Repair";
    public const string Resolve = "Resolve";
    public const string Test = "Test";
    public const string Trace = "Trace";
  }
  public static partial class VerbsLifecycle {
    // Fields
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
  public static partial class VerbsOther {
    // Fields
    public const string Use = "Use";
  }
  public static partial class VerbsSecurity {
    // Fields
    public const string Block = "Block";
    public const string Grant = "Grant";
    public const string Protect = "Protect";
    public const string Revoke = "Revoke";
    public const string Unblock = "Unblock";
    public const string Unprotect = "Unprotect";
  }
  [System.Runtime.Serialization.DataContractAttribute]
  public partial class WarningRecord : System.Management.Automation.InformationalRecord {
    // Constructors
    public WarningRecord(System.Management.Automation.PSObject record) { }
    public WarningRecord(string message) { }
    public WarningRecord(string fullyQualifiedWarningId, System.Management.Automation.PSObject record) { }
    public WarningRecord(string fullyQualifiedWarningId, string message) { }
     
    // Properties
    public string FullyQualifiedWarningId { get { return default(string); } }
     
    // Methods
  }
  public sealed partial class WideControl : System.Management.Automation.PSControl {
    // Constructors
    public WideControl() { }
    public WideControl(System.Collections.Generic.IEnumerable<System.Management.Automation.WideControlEntryItem> wideEntries) { }
    public WideControl(System.Collections.Generic.IEnumerable<System.Management.Automation.WideControlEntryItem> wideEntries, uint columns) { }
    public WideControl(uint columns) { }
     
    // Properties
    public System.Management.Automation.Alignment Alignment { get { return default(System.Management.Automation.Alignment); } }
    public uint Columns { get { return default(uint); } }
    public System.Collections.Generic.List<System.Management.Automation.WideControlEntryItem> Entries { get { return default(System.Collections.Generic.List<System.Management.Automation.WideControlEntryItem>); } }
     
    // Methods
    public override string ToString() { return default(string); }
  }
  public sealed partial class WideControlEntryItem {
    // Constructors
    public WideControlEntryItem(System.Management.Automation.DisplayEntry entry) { }
    public WideControlEntryItem(System.Management.Automation.DisplayEntry entry, System.Collections.Generic.IEnumerable<string> selectedBy) { }
     
    // Properties
    public System.Management.Automation.DisplayEntry DisplayEntry { get { return default(System.Management.Automation.DisplayEntry); } }
    public System.Collections.Generic.List<string> SelectedBy { get { return default(System.Collections.Generic.List<string>); } }
     
    // Methods
  }
  [System.FlagsAttribute]
  public enum WildcardOptions {
    // Fields
    Compiled = 1,
    CultureInvariant = 4,
    IgnoreCase = 2,
    None = 0,
  }
  public sealed partial class WildcardPattern {
    // Constructors
    public WildcardPattern(string pattern) { }
    public WildcardPattern(string pattern, System.Management.Automation.WildcardOptions options) { }
     
    // Methods
    public static bool ContainsWildcardCharacters(string pattern) { return default(bool); }
    public static string Escape(string pattern) { return default(string); }
    public bool IsMatch(string input) { return default(bool); }
    public string ToWql() { return default(string); }
    public static string Unescape(string pattern) { return default(string); }
  }
  public partial class WildcardPatternException : System.Management.Automation.RuntimeException {
    // Constructors
    public WildcardPatternException() { }
#if RUNTIME_SERIALIZATION
    protected WildcardPatternException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public WildcardPatternException(string message) { }
    public WildcardPatternException(string message, System.Exception innerException) { }
  }
  public partial class WorkflowInfo : System.Management.Automation.FunctionInfo {
    // Constructors
    public WorkflowInfo(string name, string definition, System.Management.Automation.ScriptBlock workflow, string xamlDefinition, System.Management.Automation.WorkflowInfo[] workflowsCalled) { }
    public WorkflowInfo(string name, string definition, System.Management.Automation.ScriptBlock workflow, string xamlDefinition, System.Management.Automation.WorkflowInfo[] workflowsCalled, System.Management.Automation.PSModuleInfo module) { }
     
    // Properties
    public override string Definition { get { return default(string); } }
    public string NestedXamlDefinition { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.WorkflowInfo> WorkflowsCalled { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.WorkflowInfo>); } }
    public string XamlDefinition { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
     
    // Methods
    protected internal override void Update(System.Management.Automation.FunctionInfo function, bool force, System.Management.Automation.ScopedItemOptions options, string helpFile) { }
  }
}
namespace System.Management.Automation.Host {
  [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
  public partial struct BufferCell {
    // Constructors
    public BufferCell(char character, System.ConsoleColor foreground, System.ConsoleColor background, System.Management.Automation.Host.BufferCellType bufferCellType) { throw new System.NotImplementedException(); }
     
    // Properties
    public System.ConsoleColor BackgroundColor { get { return default(System.ConsoleColor); } set { } }
    public System.Management.Automation.Host.BufferCellType BufferCellType { get { return default(System.Management.Automation.Host.BufferCellType); } set { } }
    public char Character { get { return default(char); } set { } }
    public System.ConsoleColor ForegroundColor { get { return default(System.ConsoleColor); } set { } }
     
    // Methods
    public override bool Equals(object obj) { return default(bool); }
    public override int GetHashCode() { return default(int); }
    public static bool operator ==(System.Management.Automation.Host.BufferCell first, System.Management.Automation.Host.BufferCell second) { return default(bool); }
    public static bool operator !=(System.Management.Automation.Host.BufferCell first, System.Management.Automation.Host.BufferCell second) { return default(bool); }
    public override string ToString() { return default(string); }
  }
  public enum BufferCellType {
    // Fields
    Complete = 0,
    Leading = 1,
    Trailing = 2,
  }
  public sealed partial class ChoiceDescription {
    // Constructors
    public ChoiceDescription(string label) { }
    public ChoiceDescription(string label, string helpMessage) { }
     
    // Properties
    public string HelpMessage { get { return default(string); } set { } }
    public string Label { get { return default(string); } }
     
    // Methods
  }
  [System.FlagsAttribute]
  public enum ControlKeyStates {
    // Fields
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
    // Constructors
    public Coordinates(int x, int y) { throw new System.NotImplementedException(); }
     
    // Properties
    public int X { get { return default(int); } set { } }
    public int Y { get { return default(int); } set { } }
     
    // Methods
    public override bool Equals(object obj) { return default(bool); }
    public override int GetHashCode() { return default(int); }
    public static bool operator ==(System.Management.Automation.Host.Coordinates first, System.Management.Automation.Host.Coordinates second) { return default(bool); }
    public static bool operator !=(System.Management.Automation.Host.Coordinates first, System.Management.Automation.Host.Coordinates second) { return default(bool); }
    public override string ToString() { return default(string); }
  }
  public partial class FieldDescription {
    // Constructors
    public FieldDescription(string name) { }
     
    // Properties
    public System.Collections.ObjectModel.Collection<System.Attribute> Attributes { get { return default(System.Collections.ObjectModel.Collection<System.Attribute>); } }
    public System.Management.Automation.PSObject DefaultValue { get { return default(System.Management.Automation.PSObject); } set { } }
    public string HelpMessage { get { return default(string); } set { } }
    public bool IsMandatory { get { return default(bool); } set { } }
    public string Label { get { return default(string); } set { } }
    public string Name { get { return default(string); } }
    public string ParameterAssemblyFullName { get { return default(string); } }
    public string ParameterTypeFullName { get { return default(string); } }
    public string ParameterTypeName { get { return default(string); } }
     
    // Methods
    public void SetParameterType(System.Type parameterType) { }
  }
  public partial class HostException : System.Management.Automation.RuntimeException {
    // Constructors
    public HostException() { }
#if RUNTIME_SERIALIZATION
    protected HostException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public HostException(string message) { }
    public HostException(string message, System.Exception innerException) { }
    public HostException(string message, System.Exception innerException, string errorId, System.Management.Automation.ErrorCategory errorCategory) { }
  }
  public partial interface IHostSupportsInteractiveSession {
    // Properties
    bool IsRunspacePushed { get; }
    System.Management.Automation.Runspaces.Runspace Runspace { get; }
     
    // Methods
    void PopRunspace();
    void PushRunspace(System.Management.Automation.Runspaces.Runspace runspace);
  }
  public partial interface IHostUISupportsMultipleChoiceSelection {
    // Methods
    System.Collections.ObjectModel.Collection<int> PromptForChoice(string caption, string message, System.Collections.ObjectModel.Collection<System.Management.Automation.Host.ChoiceDescription> choices, System.Collections.Generic.IEnumerable<int> defaultChoices);
  }
  [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
  public partial struct KeyInfo {
    // Constructors
    public KeyInfo(int virtualKeyCode, char ch, System.Management.Automation.Host.ControlKeyStates controlKeyState, bool keyDown) { throw new System.NotImplementedException(); }
     
    // Properties
    public char Character { get { return default(char); } set { } }
    public System.Management.Automation.Host.ControlKeyStates ControlKeyState { get { return default(System.Management.Automation.Host.ControlKeyStates); } set { } }
    public bool KeyDown { get { return default(bool); } set { } }
    public int VirtualKeyCode { get { return default(int); } set { } }
     
    // Methods
    public override bool Equals(object obj) { return default(bool); }
    public override int GetHashCode() { return default(int); }
    public static bool operator ==(System.Management.Automation.Host.KeyInfo first, System.Management.Automation.Host.KeyInfo second) { return default(bool); }
    public static bool operator !=(System.Management.Automation.Host.KeyInfo first, System.Management.Automation.Host.KeyInfo second) { return default(bool); }
    public override string ToString() { return default(string); }
  }
  public partial class PromptingException : System.Management.Automation.Host.HostException {
    // Constructors
    public PromptingException() { }
#if RUNTIME_SERIALIZATION
    protected PromptingException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public PromptingException(string message) { }
    public PromptingException(string message, System.Exception innerException) { }
    public PromptingException(string message, System.Exception innerException, string errorId, System.Management.Automation.ErrorCategory errorCategory) { }
  }
  public abstract partial class PSHost {
    // Constructors
    protected PSHost() { }
     
    // Properties
    public abstract System.Globalization.CultureInfo CurrentCulture { get; }
    public abstract System.Globalization.CultureInfo CurrentUICulture { get; }
    public abstract System.Guid InstanceId { get; }
    public abstract string Name { get; }
    public virtual System.Management.Automation.PSObject PrivateData { get { return default(System.Management.Automation.PSObject); } }
    public abstract System.Management.Automation.Host.PSHostUserInterface UI { get; }
    public abstract System.Version Version { get; }
     
    // Methods
    public abstract void EnterNestedPrompt();
    public abstract void ExitNestedPrompt();
    public abstract void NotifyBeginApplication();
    public abstract void NotifyEndApplication();
    public abstract void SetShouldExit(int exitCode);
  }
  public abstract partial class PSHostRawUserInterface {
    // Constructors
    protected PSHostRawUserInterface() { }
     
    // Properties
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
     
    // Methods
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
  public abstract partial class PSHostUserInterface {
    // Constructors
    protected PSHostUserInterface() { }
     
    // Properties
    public abstract System.Management.Automation.Host.PSHostRawUserInterface RawUI { get; }
     
    // Methods
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
    // Fields
    AllowCtrlC = 1,
    IncludeKeyDown = 4,
    IncludeKeyUp = 8,
    NoEcho = 2,
  }
  [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
  public partial struct Rectangle {
    // Constructors
    public Rectangle(int left, int top, int right, int bottom) { throw new System.NotImplementedException(); }
    public Rectangle(System.Management.Automation.Host.Coordinates upperLeft, System.Management.Automation.Host.Coordinates lowerRight) { throw new System.NotImplementedException(); }
     
    // Properties
    public int Bottom { get { return default(int); } set { } }
    public int Left { get { return default(int); } set { } }
    public int Right { get { return default(int); } set { } }
    public int Top { get { return default(int); } set { } }
     
    // Methods
    public override bool Equals(object obj) { return default(bool); }
    public override int GetHashCode() { return default(int); }
    public static bool operator ==(System.Management.Automation.Host.Rectangle first, System.Management.Automation.Host.Rectangle second) { return default(bool); }
    public static bool operator !=(System.Management.Automation.Host.Rectangle first, System.Management.Automation.Host.Rectangle second) { return default(bool); }
    public override string ToString() { return default(string); }
  }
  [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
  public partial struct Size {
    // Constructors
    public Size(int width, int height) { throw new System.NotImplementedException(); }
     
    // Properties
    public int Height { get { return default(int); } set { } }
    public int Width { get { return default(int); } set { } }
     
    // Methods
    public override bool Equals(object obj) { return default(bool); }
    public override int GetHashCode() { return default(int); }
    public static bool operator ==(System.Management.Automation.Host.Size first, System.Management.Automation.Host.Size second) { return default(bool); }
    public static bool operator !=(System.Management.Automation.Host.Size first, System.Management.Automation.Host.Size second) { return default(bool); }
    public override string ToString() { return default(string); }
  }
}
namespace System.Management.Automation.Internal {
  public static partial class AlternateDataStreamUtilities {
  }
  public static partial class AutomationNull {
    // Properties
    public static System.Management.Automation.PSObject Value { get { return default(System.Management.Automation.PSObject); } }
     
    // Methods
  }
  [System.AttributeUsageAttribute((AttributeTargets)32767)]
  public abstract partial class CmdletMetadataAttribute : System.Attribute {
    internal CmdletMetadataAttribute() { }
  }
  public sealed partial class CommonParameters {
    internal CommonParameters() { }
    // Properties
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
     
    // Methods
  }
  public partial interface IAstToWorkflowConverter {
    // Methods
    System.Management.Automation.WorkflowInfo CompileWorkflow(string name, string definition, System.Management.Automation.Runspaces.InitialSessionState initialSessionState);
    System.Collections.Generic.List<System.Management.Automation.WorkflowInfo> CompileWorkflows(System.Management.Automation.Language.ScriptBlockAst ast, System.Management.Automation.PSModuleInfo definingModule);
    System.Collections.Generic.List<System.Management.Automation.Language.ParseError> ValidateAst(System.Management.Automation.Language.FunctionDefinitionAst ast);
  }
  [System.Diagnostics.DebuggerDisplayAttribute("Command = {commandInfo}")]
  public abstract partial class InternalCommand {
    internal InternalCommand() { }
    // Properties
    public System.Management.Automation.CommandOrigin CommandOrigin { get { return default(System.Management.Automation.CommandOrigin); } }
     
    // Methods
  }
  [System.AttributeUsageAttribute((AttributeTargets)32767)]
  public abstract partial class ParsingBaseAttribute : System.Management.Automation.Internal.CmdletMetadataAttribute {
    internal ParsingBaseAttribute() { }
  }
  public sealed partial class ShouldProcessParameters {
    internal ShouldProcessParameters() { }
    // Properties
    [System.Management.Automation.AliasAttribute(new string[]{ "cf"})]
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Confirm { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[]{ "wi"})]
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter WhatIf { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    // Methods
  }
  public sealed partial class TransactionParameters {
    internal TransactionParameters() { }
    // Properties
    [System.Management.Automation.AliasAttribute(new string[]{ "usetx"})]
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter UseTransaction { get { return default(System.Management.Automation.SwitchParameter); } set { } }
     
    // Methods
  }
}
namespace System.Management.Automation.Language {
  public partial class ArrayExpressionAst : System.Management.Automation.Language.ExpressionAst {
    // Constructors
    public ArrayExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.StatementBlockAst statementBlock) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public override System.Type StaticType { get { return default(System.Type); } }
    public System.Management.Automation.Language.StatementBlockAst SubExpression { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.StatementBlockAst); } }
     
    // Methods
  }
  public partial class ArrayLiteralAst : System.Management.Automation.Language.ExpressionAst {
    // Constructors
    public ArrayLiteralAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IList<System.Management.Automation.Language.ExpressionAst> elements) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst> Elements { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst>); } }
    public override System.Type StaticType { get { return default(System.Type); } }
     
    // Methods
  }
  public sealed partial class ArrayTypeName : System.Management.Automation.Language.ITypeName {
    // Constructors
    public ArrayTypeName(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ITypeName elementType, int rank) { }
     
    // Properties
    public string AssemblyName { get { return default(string); } }
    public System.Management.Automation.Language.ITypeName ElementType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ITypeName); } }
    public System.Management.Automation.Language.IScriptExtent Extent { get { return default(System.Management.Automation.Language.IScriptExtent); } }
    public string FullName { get { return default(string); } }
    public bool IsArray { get { return default(bool); } }
    public bool IsGeneric { get { return default(bool); } }
    public string Name { get { return default(string); } }
    public int Rank { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(int); } }
     
    // Methods
    public System.Type GetReflectionAttributeType() { return default(System.Type); }
    public System.Type GetReflectionType() { return default(System.Type); }
    public override string ToString() { return default(string); }
  }
  public partial class AssignmentStatementAst : System.Management.Automation.Language.PipelineBaseAst {
    // Constructors
    public AssignmentStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ExpressionAst left, System.Management.Automation.Language.TokenKind @operator, System.Management.Automation.Language.StatementAst right, System.Management.Automation.Language.IScriptExtent errorPosition) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Management.Automation.Language.IScriptExtent ErrorPosition { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.IScriptExtent); } }
    public System.Management.Automation.Language.ExpressionAst Left { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
    public System.Management.Automation.Language.TokenKind Operator { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.TokenKind); } }
    public System.Management.Automation.Language.StatementAst Right { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.StatementAst); } }
     
    // Methods
  }
  public abstract partial class Ast {
    // Constructors
    protected Ast(System.Management.Automation.Language.IScriptExtent extent) { }
     
    // Properties
    public System.Management.Automation.Language.IScriptExtent Extent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.IScriptExtent); } }
    public System.Management.Automation.Language.Ast Parent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.Ast); } }
     
    // Methods
    public System.Management.Automation.Language.Ast Find(System.Func<System.Management.Automation.Language.Ast, bool> predicate, bool searchNestedScriptBlocks) { return default(System.Management.Automation.Language.Ast); }
    public System.Collections.Generic.IEnumerable<System.Management.Automation.Language.Ast> FindAll(System.Func<System.Management.Automation.Language.Ast, bool> predicate, bool searchNestedScriptBlocks) { return default(System.Collections.Generic.IEnumerable<System.Management.Automation.Language.Ast>); }
    public override string ToString() { return default(string); }
    public void Visit(System.Management.Automation.Language.AstVisitor astVisitor) { }
    public object Visit(System.Management.Automation.Language.ICustomAstVisitor astVisitor) { return default(object); }
  }
  public enum AstVisitAction {
    // Fields
    Continue = 0,
    SkipChildren = 1,
    StopVisit = 2,
  }
  public abstract partial class AstVisitor {
    // Constructors
    protected AstVisitor() { }
     
    // Methods
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
  public partial class AttributeAst : System.Management.Automation.Language.AttributeBaseAst {
    // Constructors
    public AttributeAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ITypeName typeName, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.ExpressionAst> positionalArguments, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.NamedAttributeArgumentAst> namedArguments) : base (default(System.Management.Automation.Language.IScriptExtent), default(System.Management.Automation.Language.ITypeName)) { }
     
    // Properties
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.NamedAttributeArgumentAst> NamedArguments { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.NamedAttributeArgumentAst>); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst> PositionalArguments { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst>); } }
     
    // Methods
  }
  public abstract partial class AttributeBaseAst : System.Management.Automation.Language.Ast {
    // Constructors
    protected AttributeBaseAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ITypeName typeName) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Management.Automation.Language.ITypeName TypeName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ITypeName); } }
     
    // Methods
  }
  public partial class AttributedExpressionAst : System.Management.Automation.Language.ExpressionAst {
    // Constructors
    public AttributedExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.AttributeBaseAst attribute, System.Management.Automation.Language.ExpressionAst child) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Management.Automation.Language.AttributeBaseAst Attribute { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.AttributeBaseAst); } }
    public System.Management.Automation.Language.ExpressionAst Child { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
     
    // Methods
  }
  public partial class BinaryExpressionAst : System.Management.Automation.Language.ExpressionAst {
    // Constructors
    public BinaryExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ExpressionAst left, System.Management.Automation.Language.TokenKind @operator, System.Management.Automation.Language.ExpressionAst right, System.Management.Automation.Language.IScriptExtent errorPosition) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Management.Automation.Language.IScriptExtent ErrorPosition { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.IScriptExtent); } }
    public System.Management.Automation.Language.ExpressionAst Left { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
    public System.Management.Automation.Language.TokenKind Operator { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.TokenKind); } }
    public System.Management.Automation.Language.ExpressionAst Right { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
    public override System.Type StaticType { get { return default(System.Type); } }
     
    // Methods
  }
  public partial class BlockStatementAst : System.Management.Automation.Language.StatementAst {
    // Constructors
    public BlockStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.Token kind, System.Management.Automation.Language.StatementBlockAst body) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Management.Automation.Language.StatementBlockAst Body { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.StatementBlockAst); } }
    public System.Management.Automation.Language.Token Kind { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.Token); } }
     
    // Methods
  }
  public partial class BreakStatementAst : System.Management.Automation.Language.StatementAst {
    // Constructors
    public BreakStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ExpressionAst label) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Management.Automation.Language.ExpressionAst Label { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
     
    // Methods
  }
  public partial class CatchClauseAst : System.Management.Automation.Language.Ast {
    // Constructors
    public CatchClauseAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.TypeConstraintAst> catchTypes, System.Management.Automation.Language.StatementBlockAst body) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Management.Automation.Language.StatementBlockAst Body { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.StatementBlockAst); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.TypeConstraintAst> CatchTypes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.TypeConstraintAst>); } }
    public bool IsCatchAll { get { return default(bool); } }
     
    // Methods
  }
  public partial class CommandAst : System.Management.Automation.Language.CommandBaseAst {
    // Constructors
    public CommandAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.CommandElementAst> commandElements, System.Management.Automation.Language.TokenKind invocationOperator, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.RedirectionAst> redirections) : base (default(System.Management.Automation.Language.IScriptExtent), default(System.Collections.Generic.IEnumerable<System.Management.Automation.Language.RedirectionAst>)) { }
     
    // Properties
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.CommandElementAst> CommandElements { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.CommandElementAst>); } }
    public System.Management.Automation.Language.TokenKind InvocationOperator { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.TokenKind); } }
     
    // Methods
    public string GetCommandName() { return default(string); }
  }
  public abstract partial class CommandBaseAst : System.Management.Automation.Language.StatementAst {
    // Constructors
    protected CommandBaseAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.RedirectionAst> redirections) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.RedirectionAst> Redirections { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.RedirectionAst>); } }
     
    // Methods
  }
  public abstract partial class CommandElementAst : System.Management.Automation.Language.Ast {
    // Constructors
    protected CommandElementAst(System.Management.Automation.Language.IScriptExtent extent) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
  }
  public partial class CommandExpressionAst : System.Management.Automation.Language.CommandBaseAst {
    // Constructors
    public CommandExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ExpressionAst expression, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.RedirectionAst> redirections) : base (default(System.Management.Automation.Language.IScriptExtent), default(System.Collections.Generic.IEnumerable<System.Management.Automation.Language.RedirectionAst>)) { }
     
    // Properties
    public System.Management.Automation.Language.ExpressionAst Expression { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
     
    // Methods
  }
  public partial class CommandParameterAst : System.Management.Automation.Language.CommandElementAst {
    // Constructors
    public CommandParameterAst(System.Management.Automation.Language.IScriptExtent extent, string parameterName, System.Management.Automation.Language.ExpressionAst argument, System.Management.Automation.Language.IScriptExtent errorPosition) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Management.Automation.Language.ExpressionAst Argument { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
    public System.Management.Automation.Language.IScriptExtent ErrorPosition { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.IScriptExtent); } }
    public string ParameterName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
     
    // Methods
  }
  public sealed partial class CommentHelpInfo {
    // Constructors
    public CommentHelpInfo() { }
     
    // Properties
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
     
    // Methods
    public string GetCommentBlock() { return default(string); }
  }
  public partial class ConstantExpressionAst : System.Management.Automation.Language.ExpressionAst {
    // Constructors
    public ConstantExpressionAst(System.Management.Automation.Language.IScriptExtent extent, object value) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public override System.Type StaticType { get { return default(System.Type); } }
    public object Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(object); } }
     
    // Methods
  }
  public partial class ContinueStatementAst : System.Management.Automation.Language.StatementAst {
    // Constructors
    public ContinueStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ExpressionAst label) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Management.Automation.Language.ExpressionAst Label { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
     
    // Methods
  }
  public partial class ConvertExpressionAst : System.Management.Automation.Language.AttributedExpressionAst {
    // Constructors
    public ConvertExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.TypeConstraintAst typeConstraint, System.Management.Automation.Language.ExpressionAst child) : base (default(System.Management.Automation.Language.IScriptExtent), default(System.Management.Automation.Language.AttributeBaseAst), default(System.Management.Automation.Language.ExpressionAst)) { }
     
    // Properties
    public override System.Type StaticType { get { return default(System.Type); } }
    public System.Management.Automation.Language.TypeConstraintAst Type { get { return default(System.Management.Automation.Language.TypeConstraintAst); } }
     
    // Methods
  }
  public partial class DataStatementAst : System.Management.Automation.Language.StatementAst {
    // Constructors
    public DataStatementAst(System.Management.Automation.Language.IScriptExtent extent, string variableName, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.ExpressionAst> commandsAllowed, System.Management.Automation.Language.StatementBlockAst body) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Management.Automation.Language.StatementBlockAst Body { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.StatementBlockAst); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst> CommandsAllowed { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst>); } }
    public string Variable { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
     
    // Methods
  }
  public partial class DoUntilStatementAst : System.Management.Automation.Language.LoopStatementAst {
    // Constructors
    public DoUntilStatementAst(System.Management.Automation.Language.IScriptExtent extent, string label, System.Management.Automation.Language.PipelineBaseAst condition, System.Management.Automation.Language.StatementBlockAst body) : base (default(System.Management.Automation.Language.IScriptExtent), default(string), default(System.Management.Automation.Language.PipelineBaseAst), default(System.Management.Automation.Language.StatementBlockAst)) { }
  }
  public partial class DoWhileStatementAst : System.Management.Automation.Language.LoopStatementAst {
    // Constructors
    public DoWhileStatementAst(System.Management.Automation.Language.IScriptExtent extent, string label, System.Management.Automation.Language.PipelineBaseAst condition, System.Management.Automation.Language.StatementBlockAst body) : base (default(System.Management.Automation.Language.IScriptExtent), default(string), default(System.Management.Automation.Language.PipelineBaseAst), default(System.Management.Automation.Language.StatementBlockAst)) { }
  }
  public partial class ErrorExpressionAst : System.Management.Automation.Language.ExpressionAst {
    internal ErrorExpressionAst() : base (default(System.Management.Automation.Language.IScriptExtent)) { }
    // Properties
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Ast> NestedAst { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Ast>); } }
     
    // Methods
  }
  public partial class ErrorStatementAst : System.Management.Automation.Language.PipelineBaseAst {
    internal ErrorStatementAst() : base (default(System.Management.Automation.Language.IScriptExtent)) { }
    // Properties
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Ast> Bodies { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Ast>); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Ast> Conditions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Ast>); } }
    public System.Collections.Generic.Dictionary<string, System.Tuple<System.Management.Automation.Language.Token, System.Management.Automation.Language.Ast>> Flags { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.Generic.Dictionary<string, System.Tuple<System.Management.Automation.Language.Token, System.Management.Automation.Language.Ast>>); } }
    public System.Management.Automation.Language.Token Kind { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.Token); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Ast> NestedAst { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Ast>); } }
     
    // Methods
  }
  public partial class ExitStatementAst : System.Management.Automation.Language.StatementAst {
    // Constructors
    public ExitStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.PipelineBaseAst pipeline) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Management.Automation.Language.PipelineBaseAst Pipeline { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.PipelineBaseAst); } }
     
    // Methods
  }
  public partial class ExpandableStringExpressionAst : System.Management.Automation.Language.ExpressionAst {
    // Constructors
    public ExpandableStringExpressionAst(System.Management.Automation.Language.IScriptExtent extent, string value, System.Management.Automation.Language.StringConstantType type) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst> NestedExpressions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst>); } }
    public override System.Type StaticType { get { return default(System.Type); } }
    public System.Management.Automation.Language.StringConstantType StringConstantType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.StringConstantType); } }
    public string Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
     
    // Methods
  }
  public abstract partial class ExpressionAst : System.Management.Automation.Language.CommandElementAst {
    // Constructors
    protected ExpressionAst(System.Management.Automation.Language.IScriptExtent extent) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public virtual System.Type StaticType { get { return default(System.Type); } }
     
    // Methods
  }
  public partial class FileRedirectionAst : System.Management.Automation.Language.RedirectionAst {
    // Constructors
    public FileRedirectionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.RedirectionStream stream, System.Management.Automation.Language.ExpressionAst file, bool append) : base (default(System.Management.Automation.Language.IScriptExtent), default(System.Management.Automation.Language.RedirectionStream)) { }
     
    // Properties
    public bool Append { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } }
    public System.Management.Automation.Language.ExpressionAst Location { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
     
    // Methods
  }
  public partial class FileRedirectionToken : System.Management.Automation.Language.RedirectionToken {
    internal FileRedirectionToken() { }
    // Properties
    public bool Append { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } }
    public System.Management.Automation.Language.RedirectionStream FromStream { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.RedirectionStream); } }
     
    // Methods
  }
  [System.FlagsAttribute]
  public enum ForEachFlags {
    // Fields
    None = 0,
    Parallel = 1,
  }
  public partial class ForEachStatementAst : System.Management.Automation.Language.LoopStatementAst {
    // Constructors
    public ForEachStatementAst(System.Management.Automation.Language.IScriptExtent extent, string label, System.Management.Automation.Language.ForEachFlags flags, System.Management.Automation.Language.VariableExpressionAst variable, System.Management.Automation.Language.PipelineBaseAst expression, System.Management.Automation.Language.StatementBlockAst body) : base (default(System.Management.Automation.Language.IScriptExtent), default(string), default(System.Management.Automation.Language.PipelineBaseAst), default(System.Management.Automation.Language.StatementBlockAst)) { }
     
    // Properties
    public System.Management.Automation.Language.ForEachFlags Flags { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ForEachFlags); } }
    public System.Management.Automation.Language.VariableExpressionAst Variable { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.VariableExpressionAst); } }
     
    // Methods
  }
  public partial class ForStatementAst : System.Management.Automation.Language.LoopStatementAst {
    // Constructors
    public ForStatementAst(System.Management.Automation.Language.IScriptExtent extent, string label, System.Management.Automation.Language.PipelineBaseAst initializer, System.Management.Automation.Language.PipelineBaseAst condition, System.Management.Automation.Language.PipelineBaseAst iterator, System.Management.Automation.Language.StatementBlockAst body) : base (default(System.Management.Automation.Language.IScriptExtent), default(string), default(System.Management.Automation.Language.PipelineBaseAst), default(System.Management.Automation.Language.StatementBlockAst)) { }
     
    // Properties
    public System.Management.Automation.Language.PipelineBaseAst Initializer { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.PipelineBaseAst); } }
    public System.Management.Automation.Language.PipelineBaseAst Iterator { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.PipelineBaseAst); } }
     
    // Methods
  }
  public partial class FunctionDefinitionAst : System.Management.Automation.Language.StatementAst {
    // Constructors
    public FunctionDefinitionAst(System.Management.Automation.Language.IScriptExtent extent, bool isFilter, bool isWorkflow, string name, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.ParameterAst> parameters, System.Management.Automation.Language.ScriptBlockAst body) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Management.Automation.Language.ScriptBlockAst Body { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ScriptBlockAst); } }
    public bool IsFilter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } }
    public bool IsWorkflow { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } }
    public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ParameterAst> Parameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ParameterAst>); } }
     
    // Methods
    public System.Management.Automation.Language.CommentHelpInfo GetHelpContent() { return default(System.Management.Automation.Language.CommentHelpInfo); }
  }
  public sealed partial class GenericTypeName : System.Management.Automation.Language.ITypeName {
    // Constructors
    public GenericTypeName(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ITypeName genericTypeName, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.ITypeName> genericArguments) { }
     
    // Properties
    public string AssemblyName { get { return default(string); } }
    public System.Management.Automation.Language.IScriptExtent Extent { get { return default(System.Management.Automation.Language.IScriptExtent); } }
    public string FullName { get { return default(string); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ITypeName> GenericArguments { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ITypeName>); } }
    public bool IsArray { get { return default(bool); } }
    public bool IsGeneric { get { return default(bool); } }
    public string Name { get { return default(string); } }
    public System.Management.Automation.Language.ITypeName TypeName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ITypeName); } }
     
    // Methods
    public System.Type GetReflectionAttributeType() { return default(System.Type); }
    public System.Type GetReflectionType() { return default(System.Type); }
    public override string ToString() { return default(string); }
  }
  public partial class HashtableAst : System.Management.Automation.Language.ExpressionAst {
    // Constructors
    public HashtableAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Tuple<System.Management.Automation.Language.ExpressionAst, System.Management.Automation.Language.StatementAst>> keyValuePairs) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Tuple<System.Management.Automation.Language.ExpressionAst, System.Management.Automation.Language.StatementAst>> KeyValuePairs { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Tuple<System.Management.Automation.Language.ExpressionAst, System.Management.Automation.Language.StatementAst>>); } }
    public override System.Type StaticType { get { return default(System.Type); } }
     
    // Methods
  }
  public partial interface ICustomAstVisitor {
    // Methods
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
  public partial class IfStatementAst : System.Management.Automation.Language.StatementAst {
    // Constructors
    public IfStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Tuple<System.Management.Automation.Language.PipelineBaseAst, System.Management.Automation.Language.StatementBlockAst>> clauses, System.Management.Automation.Language.StatementBlockAst elseClause) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Tuple<System.Management.Automation.Language.PipelineBaseAst, System.Management.Automation.Language.StatementBlockAst>> Clauses { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Tuple<System.Management.Automation.Language.PipelineBaseAst, System.Management.Automation.Language.StatementBlockAst>>); } }
    public System.Management.Automation.Language.StatementBlockAst ElseClause { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.StatementBlockAst); } }
     
    // Methods
  }
  public partial class IndexExpressionAst : System.Management.Automation.Language.ExpressionAst {
    // Constructors
    public IndexExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ExpressionAst target, System.Management.Automation.Language.ExpressionAst index) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Management.Automation.Language.ExpressionAst Index { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
    public System.Management.Automation.Language.ExpressionAst Target { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
     
    // Methods
  }
  public partial class InputRedirectionToken : System.Management.Automation.Language.RedirectionToken {
    internal InputRedirectionToken() { }
  }
  public partial class InvokeMemberExpressionAst : System.Management.Automation.Language.MemberExpressionAst {
    // Constructors
    public InvokeMemberExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ExpressionAst expression, System.Management.Automation.Language.CommandElementAst method, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.ExpressionAst> arguments, bool @static) : base (default(System.Management.Automation.Language.IScriptExtent), default(System.Management.Automation.Language.ExpressionAst), default(System.Management.Automation.Language.CommandElementAst), default(bool)) { }
     
    // Properties
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst> Arguments { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst>); } }
     
    // Methods
  }
  public partial interface IScriptExtent {
    // Properties
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
     
    // Methods
  }
  public partial interface IScriptPosition {
    // Properties
    int ColumnNumber { get; }
    string File { get; }
    string Line { get; }
    int LineNumber { get; }
    int Offset { get; }
     
    // Methods
    string GetFullScript();
  }
  public partial interface ITypeName {
    // Properties
    string AssemblyName { get; }
    System.Management.Automation.Language.IScriptExtent Extent { get; }
    string FullName { get; }
    bool IsArray { get; }
    bool IsGeneric { get; }
    string Name { get; }
     
    // Methods
    System.Type GetReflectionAttributeType();
    System.Type GetReflectionType();
  }
  public abstract partial class LabeledStatementAst : System.Management.Automation.Language.StatementAst {
    // Constructors
    protected LabeledStatementAst(System.Management.Automation.Language.IScriptExtent extent, string label, System.Management.Automation.Language.PipelineBaseAst condition) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Management.Automation.Language.PipelineBaseAst Condition { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.PipelineBaseAst); } }
    public string Label { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
     
    // Methods
  }
  public partial class LabelToken : System.Management.Automation.Language.Token {
    internal LabelToken() { }
    // Properties
    public string LabelText { get { return default(string); } }
     
    // Methods
  }
  public abstract partial class LoopStatementAst : System.Management.Automation.Language.LabeledStatementAst {
    // Constructors
    protected LoopStatementAst(System.Management.Automation.Language.IScriptExtent extent, string label, System.Management.Automation.Language.PipelineBaseAst condition, System.Management.Automation.Language.StatementBlockAst body) : base (default(System.Management.Automation.Language.IScriptExtent), default(string), default(System.Management.Automation.Language.PipelineBaseAst)) { }
     
    // Properties
    public System.Management.Automation.Language.StatementBlockAst Body { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.StatementBlockAst); } }
     
    // Methods
  }
  public partial class MemberExpressionAst : System.Management.Automation.Language.ExpressionAst {
    // Constructors
    public MemberExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ExpressionAst expression, System.Management.Automation.Language.CommandElementAst member, bool @static) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Management.Automation.Language.ExpressionAst Expression { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
    public System.Management.Automation.Language.CommandElementAst Member { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.CommandElementAst); } }
    public bool Static { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } }
     
    // Methods
  }
  public partial class MergingRedirectionAst : System.Management.Automation.Language.RedirectionAst {
    // Constructors
    public MergingRedirectionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.RedirectionStream from, System.Management.Automation.Language.RedirectionStream to) : base (default(System.Management.Automation.Language.IScriptExtent), default(System.Management.Automation.Language.RedirectionStream)) { }
     
    // Properties
    public System.Management.Automation.Language.RedirectionStream ToStream { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.RedirectionStream); } }
     
    // Methods
  }
  public partial class MergingRedirectionToken : System.Management.Automation.Language.RedirectionToken {
    internal MergingRedirectionToken() { }
    // Properties
    public System.Management.Automation.Language.RedirectionStream FromStream { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.RedirectionStream); } }
    public System.Management.Automation.Language.RedirectionStream ToStream { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.RedirectionStream); } }
     
    // Methods
  }
  public partial class NamedAttributeArgumentAst : System.Management.Automation.Language.Ast {
    // Constructors
    public NamedAttributeArgumentAst(System.Management.Automation.Language.IScriptExtent extent, string argumentName, System.Management.Automation.Language.ExpressionAst argument, bool expressionOmitted) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Management.Automation.Language.ExpressionAst Argument { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
    public string ArgumentName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
    public bool ExpressionOmitted { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } }
     
    // Methods
  }
  public partial class NamedBlockAst : System.Management.Automation.Language.Ast {
    // Constructors
    public NamedBlockAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.TokenKind blockName, System.Management.Automation.Language.StatementBlockAst statementBlock, bool unnamed) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Management.Automation.Language.TokenKind BlockKind { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.TokenKind); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.StatementAst> Statements { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.StatementAst>); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.TrapStatementAst> Traps { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.TrapStatementAst>); } }
    public bool Unnamed { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } }
     
    // Methods
  }
  public partial class NullString {
    internal NullString() { }
    // Properties
    public static System.Management.Automation.Language.NullString Value { get { return default(System.Management.Automation.Language.NullString); } }
     
    // Methods
    public override string ToString() { return default(string); }
  }
  public partial class NumberToken : System.Management.Automation.Language.Token {
    internal NumberToken() { }
    // Properties
    public object Value { get { return default(object); } }
     
    // Methods
  }
  public partial class ParamBlockAst : System.Management.Automation.Language.Ast {
    // Constructors
    public ParamBlockAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.AttributeAst> attributes, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.ParameterAst> parameters) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.AttributeAst> Attributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.AttributeAst>); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ParameterAst> Parameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ParameterAst>); } }
     
    // Methods
  }
  public partial class ParameterAst : System.Management.Automation.Language.Ast {
    // Constructors
    public ParameterAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.VariableExpressionAst name, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.AttributeBaseAst> attributes, System.Management.Automation.Language.ExpressionAst defaultValue) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.AttributeBaseAst> Attributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.AttributeBaseAst>); } }
    public System.Management.Automation.Language.ExpressionAst DefaultValue { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
    public System.Management.Automation.Language.VariableExpressionAst Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.VariableExpressionAst); } }
    public System.Type StaticType { get { return default(System.Type); } }
     
    // Methods
  }
  public partial class ParameterToken : System.Management.Automation.Language.Token {
    internal ParameterToken() { }
    // Properties
    public string ParameterName { get { return default(string); } }
    public bool UsedColon { get { return default(bool); } }
     
    // Methods
  }
  public partial class ParenExpressionAst : System.Management.Automation.Language.ExpressionAst {
    // Constructors
    public ParenExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.PipelineBaseAst pipeline) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Management.Automation.Language.PipelineBaseAst Pipeline { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.PipelineBaseAst); } }
     
    // Methods
  }
  public partial class ParseError {
    // Constructors
    public ParseError(System.Management.Automation.Language.IScriptExtent extent, string errorId, string message) { }
     
    // Properties
    public string ErrorId { get { return default(string); } }
    public System.Management.Automation.Language.IScriptExtent Extent { get { return default(System.Management.Automation.Language.IScriptExtent); } }
    public bool IncompleteInput { get { return default(bool); } }
    public string Message { get { return default(string); } }
     
    // Methods
    public override string ToString() { return default(string); }
  }
  public sealed partial class Parser {
    internal Parser() { }
    // Methods
    public static System.Management.Automation.Language.ScriptBlockAst ParseFile(string fileName, out System.Management.Automation.Language.Token[] tokens, out System.Management.Automation.Language.ParseError[] errors) { tokens = default(System.Management.Automation.Language.Token[]); errors = default(System.Management.Automation.Language.ParseError[]); return default(System.Management.Automation.Language.ScriptBlockAst); }
    public static System.Management.Automation.Language.ScriptBlockAst ParseInput(string input, out System.Management.Automation.Language.Token[] tokens, out System.Management.Automation.Language.ParseError[] errors) { tokens = default(System.Management.Automation.Language.Token[]); errors = default(System.Management.Automation.Language.ParseError[]); return default(System.Management.Automation.Language.ScriptBlockAst); }
  }
  public partial class PipelineAst : System.Management.Automation.Language.PipelineBaseAst {
    // Constructors
    public PipelineAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.CommandBaseAst> pipelineElements) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
    public PipelineAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.CommandBaseAst commandAst) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.CommandBaseAst> PipelineElements { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.CommandBaseAst>); } }
     
    // Methods
    public override System.Management.Automation.Language.ExpressionAst GetPureExpression() { return default(System.Management.Automation.Language.ExpressionAst); }
  }
  public abstract partial class PipelineBaseAst : System.Management.Automation.Language.StatementAst {
    // Constructors
    protected PipelineBaseAst(System.Management.Automation.Language.IScriptExtent extent) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Methods
    public virtual System.Management.Automation.Language.ExpressionAst GetPureExpression() { return default(System.Management.Automation.Language.ExpressionAst); }
  }
  public abstract partial class RedirectionAst : System.Management.Automation.Language.Ast {
    // Constructors
    protected RedirectionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.RedirectionStream from) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Management.Automation.Language.RedirectionStream FromStream { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.RedirectionStream); } }
     
    // Methods
  }
  public enum RedirectionStream {
    // Fields
    All = 0,
    Debug = 5,
    Error = 2,
    Host = 6,
    Output = 1,
    Verbose = 4,
    Warning = 3,
  }
  public abstract partial class RedirectionToken : System.Management.Automation.Language.Token {
    internal RedirectionToken() { }
  }
  public sealed partial class ReflectionTypeName : System.Management.Automation.Language.ITypeName {
    // Constructors
    public ReflectionTypeName(System.Type type) { }
     
    // Properties
    public string AssemblyName { get { return default(string); } }
    public System.Management.Automation.Language.IScriptExtent Extent { get { return default(System.Management.Automation.Language.IScriptExtent); } }
    public string FullName { get { return default(string); } }
    public bool IsArray { get { return default(bool); } }
    public bool IsGeneric { get { return default(bool); } }
    public string Name { get { return default(string); } }
     
    // Methods
    public System.Type GetReflectionAttributeType() { return default(System.Type); }
    public System.Type GetReflectionType() { return default(System.Type); }
    public override string ToString() { return default(string); }
  }
  public partial class ReturnStatementAst : System.Management.Automation.Language.StatementAst {
    // Constructors
    public ReturnStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.PipelineBaseAst pipeline) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Management.Automation.Language.PipelineBaseAst Pipeline { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.PipelineBaseAst); } }
     
    // Methods
  }
  public partial class ScriptBlockAst : System.Management.Automation.Language.Ast {
    // Constructors
    public ScriptBlockAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ParamBlockAst paramBlock, System.Management.Automation.Language.NamedBlockAst beginBlock, System.Management.Automation.Language.NamedBlockAst processBlock, System.Management.Automation.Language.NamedBlockAst endBlock, System.Management.Automation.Language.NamedBlockAst dynamicParamBlock) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
    public ScriptBlockAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ParamBlockAst paramBlock, System.Management.Automation.Language.StatementBlockAst statements, bool isFilter) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Management.Automation.Language.NamedBlockAst BeginBlock { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.NamedBlockAst); } }
    public System.Management.Automation.Language.NamedBlockAst DynamicParamBlock { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.NamedBlockAst); } }
    public System.Management.Automation.Language.NamedBlockAst EndBlock { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.NamedBlockAst); } }
    public System.Management.Automation.Language.ParamBlockAst ParamBlock { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ParamBlockAst); } }
    public System.Management.Automation.Language.NamedBlockAst ProcessBlock { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.NamedBlockAst); } }
    public System.Management.Automation.Language.ScriptRequirements ScriptRequirements { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ScriptRequirements); } }
     
    // Methods
    public System.Management.Automation.Language.CommentHelpInfo GetHelpContent() { return default(System.Management.Automation.Language.CommentHelpInfo); }
    public System.Management.Automation.ScriptBlock GetScriptBlock() { return default(System.Management.Automation.ScriptBlock); }
  }
  public partial class ScriptBlockExpressionAst : System.Management.Automation.Language.ExpressionAst {
    // Constructors
    public ScriptBlockExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ScriptBlockAst scriptBlock) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Management.Automation.Language.ScriptBlockAst ScriptBlock { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ScriptBlockAst); } }
    public override System.Type StaticType { get { return default(System.Type); } }
     
    // Methods
  }
  public sealed partial class ScriptExtent : System.Management.Automation.Language.IScriptExtent {
    // Constructors
    public ScriptExtent(System.Management.Automation.Language.ScriptPosition startPosition, System.Management.Automation.Language.ScriptPosition endPosition) { }
     
    // Properties
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
     
    // Methods
  }
  public sealed partial class ScriptPosition : System.Management.Automation.Language.IScriptPosition {
    // Constructors
    public ScriptPosition(string scriptName, int scriptLineNumber, int offsetInLine, string line) { }
     
    // Properties
    public int ColumnNumber { get { return default(int); } }
    public string File { get { return default(string); } }
    public string Line { get { return default(string); } }
    public int LineNumber { get { return default(int); } }
    public int Offset { get { return default(int); } }
     
    // Methods
    public string GetFullScript() { return default(string); }
  }
  public partial class ScriptRequirements {
    // Constructors
    public ScriptRequirements() { }
     
    // Properties
    public string RequiredApplicationId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<string> RequiredAssemblies { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<string>); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<Microsoft.PowerShell.Commands.ModuleSpecification> RequiredModules { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<Microsoft.PowerShell.Commands.ModuleSpecification>); } }
    public System.Version RequiredPSVersion { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Version); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSSnapInSpecification> RequiresPSSnapIns { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSSnapInSpecification>); } }
     
    // Methods
  }
  public abstract partial class StatementAst : System.Management.Automation.Language.Ast {
    // Constructors
    protected StatementAst(System.Management.Automation.Language.IScriptExtent extent) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
  }
  public partial class StatementBlockAst : System.Management.Automation.Language.Ast {
    // Constructors
    public StatementBlockAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.StatementAst> statements, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.TrapStatementAst> traps) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.StatementAst> Statements { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.StatementAst>); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.TrapStatementAst> Traps { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.TrapStatementAst>); } }
     
    // Methods
  }
  public partial class StringConstantExpressionAst : System.Management.Automation.Language.ConstantExpressionAst {
    // Constructors
    public StringConstantExpressionAst(System.Management.Automation.Language.IScriptExtent extent, string value, System.Management.Automation.Language.StringConstantType stringConstantType) : base (default(System.Management.Automation.Language.IScriptExtent), default(object)) { }
     
    // Properties
    public override System.Type StaticType { get { return default(System.Type); } }
    public System.Management.Automation.Language.StringConstantType StringConstantType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.StringConstantType); } }
    public new string Value { get { return default(string); } }
     
    // Methods
  }
  public enum StringConstantType {
    // Fields
    BareWord = 4,
    DoubleQuoted = 2,
    DoubleQuotedHereString = 3,
    SingleQuoted = 0,
    SingleQuotedHereString = 1,
  }
  public partial class StringExpandableToken : System.Management.Automation.Language.StringToken {
    internal StringExpandableToken() { }
    // Properties
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Token> NestedTokens { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Token>); } }
     
    // Methods
  }
  public partial class StringLiteralToken : System.Management.Automation.Language.StringToken {
    internal StringLiteralToken() { }
  }
  public abstract partial class StringToken : System.Management.Automation.Language.Token {
    internal StringToken() { }
    // Properties
    public string Value { get { return default(string); } }
     
    // Methods
  }
  public partial class SubExpressionAst : System.Management.Automation.Language.ExpressionAst {
    // Constructors
    public SubExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.StatementBlockAst statementBlock) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Management.Automation.Language.StatementBlockAst SubExpression { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.StatementBlockAst); } }
     
    // Methods
  }
  [System.FlagsAttribute]
  public enum SwitchFlags {
    // Fields
    CaseSensitive = 16,
    Exact = 8,
    File = 1,
    None = 0,
    Parallel = 32,
    Regex = 2,
    Wildcard = 4,
  }
  public partial class SwitchStatementAst : System.Management.Automation.Language.LabeledStatementAst {
    // Constructors
    public SwitchStatementAst(System.Management.Automation.Language.IScriptExtent extent, string label, System.Management.Automation.Language.PipelineBaseAst condition, System.Management.Automation.Language.SwitchFlags flags, System.Collections.Generic.IEnumerable<System.Tuple<System.Management.Automation.Language.ExpressionAst, System.Management.Automation.Language.StatementBlockAst>> clauses, System.Management.Automation.Language.StatementBlockAst @default) : base (default(System.Management.Automation.Language.IScriptExtent), default(string), default(System.Management.Automation.Language.PipelineBaseAst)) { }
     
    // Properties
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Tuple<System.Management.Automation.Language.ExpressionAst, System.Management.Automation.Language.StatementBlockAst>> Clauses { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Tuple<System.Management.Automation.Language.ExpressionAst, System.Management.Automation.Language.StatementBlockAst>>); } }
    public System.Management.Automation.Language.StatementBlockAst Default { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.StatementBlockAst); } }
    public System.Management.Automation.Language.SwitchFlags Flags { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.SwitchFlags); } }
     
    // Methods
  }
  public partial class ThrowStatementAst : System.Management.Automation.Language.StatementAst {
    // Constructors
    public ThrowStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.PipelineBaseAst pipeline) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public bool IsRethrow { get { return default(bool); } }
    public System.Management.Automation.Language.PipelineBaseAst Pipeline { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.PipelineBaseAst); } }
     
    // Methods
  }
  public partial class Token {
    internal Token() { }
    // Properties
    public System.Management.Automation.Language.IScriptExtent Extent { get { return default(System.Management.Automation.Language.IScriptExtent); } }
    public bool HasError { get { return default(bool); } }
    public System.Management.Automation.Language.TokenKind Kind { get { return default(System.Management.Automation.Language.TokenKind); } }
    public string Text { get { return default(string); } }
    public System.Management.Automation.Language.TokenFlags TokenFlags { get { return default(System.Management.Automation.Language.TokenFlags); } }
     
    // Methods
    public override string ToString() { return default(string); }
  }
  [System.FlagsAttribute]
  public enum TokenFlags {
    // Fields
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
    // Fields
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
  public static partial class TokenTraits {
    // Methods
    public static System.Management.Automation.Language.TokenFlags GetTraits(this System.Management.Automation.Language.TokenKind kind) { return default(System.Management.Automation.Language.TokenFlags); }
    public static bool HasTrait(this System.Management.Automation.Language.TokenKind kind, System.Management.Automation.Language.TokenFlags flag) { return default(bool); }
    public static string Text(this System.Management.Automation.Language.TokenKind kind) { return default(string); }
  }
  public partial class TrapStatementAst : System.Management.Automation.Language.StatementAst {
    // Constructors
    public TrapStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.TypeConstraintAst trapType, System.Management.Automation.Language.StatementBlockAst body) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Management.Automation.Language.StatementBlockAst Body { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.StatementBlockAst); } }
    public System.Management.Automation.Language.TypeConstraintAst TrapType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.TypeConstraintAst); } }
     
    // Methods
  }
  public partial class TryStatementAst : System.Management.Automation.Language.StatementAst {
    // Constructors
    public TryStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.StatementBlockAst body, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.CatchClauseAst> catchClauses, System.Management.Automation.Language.StatementBlockAst @finally) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Management.Automation.Language.StatementBlockAst Body { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.StatementBlockAst); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.CatchClauseAst> CatchClauses { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.CatchClauseAst>); } }
    public System.Management.Automation.Language.StatementBlockAst Finally { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.StatementBlockAst); } }
     
    // Methods
  }
  public partial class TypeConstraintAst : System.Management.Automation.Language.AttributeBaseAst {
    // Constructors
    public TypeConstraintAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ITypeName typeName) : base (default(System.Management.Automation.Language.IScriptExtent), default(System.Management.Automation.Language.ITypeName)) { }
    public TypeConstraintAst(System.Management.Automation.Language.IScriptExtent extent, System.Type type) : base (default(System.Management.Automation.Language.IScriptExtent), default(System.Management.Automation.Language.ITypeName)) { }
  }
  public partial class TypeExpressionAst : System.Management.Automation.Language.ExpressionAst {
    // Constructors
    public TypeExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ITypeName typeName) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public override System.Type StaticType { get { return default(System.Type); } }
    public System.Management.Automation.Language.ITypeName TypeName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ITypeName); } }
     
    // Methods
  }
  public sealed partial class TypeName : System.Management.Automation.Language.ITypeName {
    // Constructors
    public TypeName(System.Management.Automation.Language.IScriptExtent extent, string name) { }
    public TypeName(System.Management.Automation.Language.IScriptExtent extent, string name, string assembly) { }
     
    // Properties
    public string AssemblyName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } }
    public System.Management.Automation.Language.IScriptExtent Extent { get { return default(System.Management.Automation.Language.IScriptExtent); } }
    public string FullName { get { return default(string); } }
    public bool IsArray { get { return default(bool); } }
    public bool IsGeneric { get { return default(bool); } }
    public string Name { get { return default(string); } }
     
    // Methods
    public System.Type GetReflectionAttributeType() { return default(System.Type); }
    public System.Type GetReflectionType() { return default(System.Type); }
    public override string ToString() { return default(string); }
  }
  public partial class UnaryExpressionAst : System.Management.Automation.Language.ExpressionAst {
    // Constructors
    public UnaryExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.TokenKind tokenKind, System.Management.Automation.Language.ExpressionAst child) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Management.Automation.Language.ExpressionAst Child { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
    public override System.Type StaticType { get { return default(System.Type); } }
    public System.Management.Automation.Language.TokenKind TokenKind { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.TokenKind); } }
     
    // Methods
  }
  public partial class UsingExpressionAst : System.Management.Automation.Language.ExpressionAst {
    // Constructors
    public UsingExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ExpressionAst expressionAst) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public System.Management.Automation.Language.ExpressionAst SubExpression { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.Language.ExpressionAst); } }
     
    // Methods
    public static System.Management.Automation.Language.VariableExpressionAst ExtractUsingVariable(System.Management.Automation.Language.UsingExpressionAst usingExpressionAst) { return default(System.Management.Automation.Language.VariableExpressionAst); }
  }
  public partial class VariableExpressionAst : System.Management.Automation.Language.ExpressionAst {
    // Constructors
    public VariableExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.VariablePath variablePath, bool splatted) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
    public VariableExpressionAst(System.Management.Automation.Language.IScriptExtent extent, string variableName, bool splatted) : base (default(System.Management.Automation.Language.IScriptExtent)) { }
     
    // Properties
    public bool Splatted { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } }
    public System.Management.Automation.VariablePath VariablePath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.VariablePath); } }
     
    // Methods
    public bool IsConstantVariable() { return default(bool); }
  }
  public partial class VariableToken : System.Management.Automation.Language.Token {
    internal VariableToken() { }
    // Properties
    public string Name { get { return default(string); } }
    public System.Management.Automation.VariablePath VariablePath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Management.Automation.VariablePath); } }
     
    // Methods
  }
  public partial class WhileStatementAst : System.Management.Automation.Language.LoopStatementAst {
    // Constructors
    public WhileStatementAst(System.Management.Automation.Language.IScriptExtent extent, string label, System.Management.Automation.Language.PipelineBaseAst condition, System.Management.Automation.Language.StatementBlockAst body) : base (default(System.Management.Automation.Language.IScriptExtent), default(string), default(System.Management.Automation.Language.PipelineBaseAst), default(System.Management.Automation.Language.StatementBlockAst)) { }
  }
}
#if PERF_COUNTERS
namespace System.Management.Automation.PerformanceData {
  [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
  public partial struct CounterInfo {
    // Constructors
    public CounterInfo(int id, System.Diagnostics.PerformanceData.CounterType type) { throw new System.NotImplementedException(); }
    public CounterInfo(int id, System.Diagnostics.PerformanceData.CounterType type, string name) { throw new System.NotImplementedException(); }
     
    // Properties
    public int Id { get { return default(int); } }
    public string Name { get { return default(string); } }
    public System.Diagnostics.PerformanceData.CounterType Type { get { return default(System.Diagnostics.PerformanceData.CounterType); } }
     
    // Methods
  }
  public abstract partial class CounterSetInstanceBase : System.IDisposable {
    // Fields
    protected System.Collections.Concurrent.ConcurrentDictionary<int, System.Diagnostics.PerformanceData.CounterType> _counterIdToTypeMapping;
    protected System.Collections.Concurrent.ConcurrentDictionary<string, int> _counterNameToIdMapping;
    protected System.Management.Automation.PerformanceData.CounterSetRegistrarBase _counterSetRegistrarBase;
     
    // Constructors
    protected CounterSetInstanceBase(System.Management.Automation.PerformanceData.CounterSetRegistrarBase counterSetRegistrarInst) { }
     
    // Methods
    public abstract void Dispose();
    public abstract bool GetCounterValue(int counterId, bool isNumerator, out long counterValue);
    public abstract bool GetCounterValue(string counterName, bool isNumerator, out long counterValue);
    protected bool RetrieveTargetCounterIdIfValid(int counterId, bool isNumerator, out int targetCounterId) { targetCounterId = default(int); return default(bool); }
    public abstract bool SetCounterValue(int counterId, long counterValue, bool isNumerator);
    public abstract bool SetCounterValue(string counterName, long counterValue, bool isNumerator);
    public abstract bool UpdateCounterByValue(int counterId, long stepAmount, bool isNumerator);
    public abstract bool UpdateCounterByValue(string counterName, long stepAmount, bool isNumerator);
  }
  public abstract partial class CounterSetRegistrarBase {
    // Fields
    protected System.Management.Automation.PerformanceData.CounterSetInstanceBase _counterSetInstanceBase;
     
    // Constructors
    protected CounterSetRegistrarBase(System.Guid providerId, System.Guid counterSetId, System.Diagnostics.PerformanceData.CounterSetInstanceType counterSetInstType, System.Management.Automation.PerformanceData.CounterInfo[] counterInfoArray, string counterSetName=null) { }
    protected CounterSetRegistrarBase(System.Management.Automation.PerformanceData.CounterSetRegistrarBase srcCounterSetRegistrarBase) { }
     
    // Properties
    public System.Management.Automation.PerformanceData.CounterInfo[] CounterInfoArray { get { return default(System.Management.Automation.PerformanceData.CounterInfo[]); } }
    public System.Guid CounterSetId { get { return default(System.Guid); } }
    public System.Management.Automation.PerformanceData.CounterSetInstanceBase CounterSetInstance { get { return default(System.Management.Automation.PerformanceData.CounterSetInstanceBase); } }
    public System.Diagnostics.PerformanceData.CounterSetInstanceType CounterSetInstType { get { return default(System.Diagnostics.PerformanceData.CounterSetInstanceType); } }
    public string CounterSetName { get { return default(string); } }
    public System.Guid ProviderId { get { return default(System.Guid); } }
     
    // Methods
    protected abstract System.Management.Automation.PerformanceData.CounterSetInstanceBase CreateCounterSetInstance();
    public abstract void DisposeCounterSetInstance();
  }
  public partial class PSCounterSetInstance : System.Management.Automation.PerformanceData.CounterSetInstanceBase {
    // Constructors
    public PSCounterSetInstance(System.Management.Automation.PerformanceData.CounterSetRegistrarBase counterSetRegBaseObj) : base (default(System.Management.Automation.PerformanceData.CounterSetRegistrarBase)) { }
     
    // Methods
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
  public partial class PSCounterSetRegistrar : System.Management.Automation.PerformanceData.CounterSetRegistrarBase {
    // Constructors
    public PSCounterSetRegistrar(System.Guid providerId, System.Guid counterSetId, System.Diagnostics.PerformanceData.CounterSetInstanceType counterSetInstType, System.Management.Automation.PerformanceData.CounterInfo[] counterInfoArray, string counterSetName=null) : base (default(System.Guid), default(System.Guid), default(System.Diagnostics.PerformanceData.CounterSetInstanceType), default(System.Management.Automation.PerformanceData.CounterInfo[]), default(string)) { }
    public PSCounterSetRegistrar(System.Management.Automation.PerformanceData.PSCounterSetRegistrar srcPSCounterSetRegistrar) : base (default(System.Guid), default(System.Guid), default(System.Diagnostics.PerformanceData.CounterSetInstanceType), default(System.Management.Automation.PerformanceData.CounterInfo[]), default(string)) { }
     
    // Methods
    protected override System.Management.Automation.PerformanceData.CounterSetInstanceBase CreateCounterSetInstance() { return default(System.Management.Automation.PerformanceData.CounterSetInstanceBase); }
    public override void DisposeCounterSetInstance() { }
  }
  public partial class PSPerfCountersMgr {
    internal PSPerfCountersMgr() { }
    // Properties
    public static System.Management.Automation.PerformanceData.PSPerfCountersMgr Instance { get { return default(System.Management.Automation.PerformanceData.PSPerfCountersMgr); } }
     
    // Methods
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
  public abstract partial class CmdletProvider : System.Management.Automation.IResourceSupplier {
    // Constructors
    protected CmdletProvider() { }
     
    // Properties
    public System.Management.Automation.PSCredential Credential { get { return default(System.Management.Automation.PSCredential); } }
    public System.Management.Automation.PSTransactionContext CurrentPSTransaction { get { return default(System.Management.Automation.PSTransactionContext); } }
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
     
    // Methods
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
  public sealed partial class CmdletProviderAttribute : System.Attribute {
    // Constructors
    public CmdletProviderAttribute(string providerName, System.Management.Automation.Provider.ProviderCapabilities providerCapabilities) { }
     
    // Properties
    public System.Management.Automation.Provider.ProviderCapabilities ProviderCapabilities { get { return default(System.Management.Automation.Provider.ProviderCapabilities); } }
    public string ProviderName { get { return default(string); } }
     
    // Methods
  }
  public abstract partial class ContainerCmdletProvider : System.Management.Automation.Provider.ItemCmdletProvider {
    // Constructors
    protected ContainerCmdletProvider() { }
     
    // Methods
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
  public abstract partial class DriveCmdletProvider : System.Management.Automation.Provider.CmdletProvider {
    // Constructors
    protected DriveCmdletProvider() { }
     
    // Methods
    protected virtual System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo> InitializeDefaultDrives() { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo>); }
    protected virtual System.Management.Automation.PSDriveInfo NewDrive(System.Management.Automation.PSDriveInfo drive) { return default(System.Management.Automation.PSDriveInfo); }
    protected virtual object NewDriveDynamicParameters() { return default(object); }
    protected virtual System.Management.Automation.PSDriveInfo RemoveDrive(System.Management.Automation.PSDriveInfo drive) { return default(System.Management.Automation.PSDriveInfo); }
  }
  public partial interface ICmdletProviderSupportsHelp {
    // Methods
    string GetHelpMaml(string helpItemName, string path);
  }
  public partial interface IContentCmdletProvider {
    // Methods
    void ClearContent(string path);
    object ClearContentDynamicParameters(string path);
    System.Management.Automation.Provider.IContentReader GetContentReader(string path);
    object GetContentReaderDynamicParameters(string path);
    System.Management.Automation.Provider.IContentWriter GetContentWriter(string path);
    object GetContentWriterDynamicParameters(string path);
  }
  public partial interface IContentReader : System.IDisposable {
    // Methods
    void Close();
    System.Collections.IList Read(long readCount);
    void Seek(long offset, System.IO.SeekOrigin origin);
  }
  public partial interface IContentWriter : System.IDisposable {
    // Methods
    void Close();
    void Seek(long offset, System.IO.SeekOrigin origin);
    System.Collections.IList Write(System.Collections.IList content);
  }
  public partial interface IDynamicPropertyCmdletProvider : System.Management.Automation.Provider.IPropertyCmdletProvider {
    // Methods
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
    // Methods
    void ClearProperty(string path, System.Collections.ObjectModel.Collection<string> propertyToClear);
    object ClearPropertyDynamicParameters(string path, System.Collections.ObjectModel.Collection<string> propertyToClear);
    void GetProperty(string path, System.Collections.ObjectModel.Collection<string> providerSpecificPickList);
    object GetPropertyDynamicParameters(string path, System.Collections.ObjectModel.Collection<string> providerSpecificPickList);
    void SetProperty(string path, System.Management.Automation.PSObject propertyValue);
    object SetPropertyDynamicParameters(string path, System.Management.Automation.PSObject propertyValue);
  }
  public partial interface ISecurityDescriptorCmdletProvider {
    // Methods
    void GetSecurityDescriptor(string path, System.Security.AccessControl.AccessControlSections includeSections);
    System.Security.AccessControl.ObjectSecurity NewSecurityDescriptorFromPath(string path, System.Security.AccessControl.AccessControlSections includeSections);
    System.Security.AccessControl.ObjectSecurity NewSecurityDescriptorOfType(string type, System.Security.AccessControl.AccessControlSections includeSections);
    void SetSecurityDescriptor(string path, System.Security.AccessControl.ObjectSecurity securityDescriptor);
  }
  public abstract partial class ItemCmdletProvider : System.Management.Automation.Provider.DriveCmdletProvider {
    // Constructors
    protected ItemCmdletProvider() { }
     
    // Methods
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
  public abstract partial class NavigationCmdletProvider : System.Management.Automation.Provider.ContainerCmdletProvider {
    // Constructors
    protected NavigationCmdletProvider() { }
     
    // Methods
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
    // Fields
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
  public partial class OriginInfo {
    // Constructors
    public OriginInfo(string computerName, System.Guid runspaceID) { }
    public OriginInfo(string computerName, System.Guid runspaceID, System.Guid instanceID) { }
     
    // Properties
    public System.Guid InstanceID { get { return default(System.Guid); } set { } }
    public string PSComputerName { get { return default(string); } }
    public System.Guid RunspaceID { get { return default(System.Guid); } }
     
    // Methods
    public override string ToString() { return default(string); }
  }
  public enum ProxyAccessType {
    // Fields
    AutoDetect = 4,
    IEConfig = 1,
    None = 0,
    NoProxyServer = 8,
    WinHttpConfig = 2,
  }
  public sealed partial class PSCertificateDetails {
    // Constructors
    public PSCertificateDetails(string subject, string issuerName, string issuerThumbprint) { }
     
    // Properties
    public string IssuerName { get { return default(string); } }
    public string IssuerThumbprint { get { return default(string); } }
    public string Subject { get { return default(string); } }
     
    // Methods
  }
  public sealed partial class PSIdentity : System.Security.Principal.IIdentity {
    // Constructors
    public PSIdentity(string authType, bool isAuthenticated, string userName, System.Management.Automation.Remoting.PSCertificateDetails cert) { }
     
    // Properties
    public string AuthenticationType { get { return default(string); } }
    public System.Management.Automation.Remoting.PSCertificateDetails CertificateDetails { get { return default(System.Management.Automation.Remoting.PSCertificateDetails); } }
    public bool IsAuthenticated { get { return default(bool); } }
    public string Name { get { return default(string); } }
     
    // Methods
  }
  public sealed partial class PSPrincipal : System.Security.Principal.IPrincipal {
    // Constructors
    public PSPrincipal(System.Management.Automation.Remoting.PSIdentity identity, System.Security.Principal.WindowsIdentity windowsIdentity) { }
     
    // Properties
    public System.Management.Automation.Remoting.PSIdentity Identity { get { return default(System.Management.Automation.Remoting.PSIdentity); } }
    System.Security.Principal.IIdentity System.Security.Principal.IPrincipal.Identity { get { return default(System.Security.Principal.IIdentity); } }
    public System.Security.Principal.WindowsIdentity WindowsIdentity { get { return default(System.Security.Principal.WindowsIdentity); } }
     
    // Methods
    public bool IsInRole(string role) { return default(bool); }
  }
  public partial class PSRemotingDataStructureException : System.Management.Automation.RuntimeException {
    // Constructors
    public PSRemotingDataStructureException() { }
#if RUNTIME_SERIALIZATION
    protected PSRemotingDataStructureException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public PSRemotingDataStructureException(string message) { }
    public PSRemotingDataStructureException(string message, System.Exception innerException) { }
  }
  public partial class PSRemotingTransportException : System.Management.Automation.RuntimeException {
    // Constructors
    public PSRemotingTransportException() { }
#if RUNTIME_SERIALIZATION
    protected PSRemotingTransportException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public PSRemotingTransportException(string message) { }
    public PSRemotingTransportException(string message, System.Exception innerException) { }
     
    // Properties
    public int ErrorCode { get { return default(int); } set { } }
    public string TransportMessage { get { return default(string); } set { } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    protected void SetDefaultErrorRecord() { }
  }
  public partial class PSRemotingTransportRedirectException : System.Management.Automation.Remoting.PSRemotingTransportException {
    // Constructors
    public PSRemotingTransportRedirectException() { }
#if RUNTIME_SERIALIZATION
    protected PSRemotingTransportRedirectException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public PSRemotingTransportRedirectException(string message) { }
    public PSRemotingTransportRedirectException(string message, System.Exception innerException) { }
     
    // Properties
    public string RedirectLocation { get { return default(string); } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
  }
  public sealed partial class PSSenderInfo : System.Runtime.Serialization.ISerializable {
    // Constructors
    public PSSenderInfo(System.Management.Automation.Remoting.PSPrincipal userPrincipal, string httpUrl) { }
     
    // Properties
    public System.Management.Automation.PSPrimitiveDictionary ApplicationArguments { get { return default(System.Management.Automation.PSPrimitiveDictionary); } }
#if TIMEZONE
    public System.TimeZone ClientTimeZone { get { return default(System.TimeZone); } }
#endif
    public string ConnectionString { get { return default(string); } }
    public System.Management.Automation.Remoting.PSPrincipal UserInfo { get { return default(System.Management.Automation.Remoting.PSPrincipal); } }
     
    // Methods
    public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
  }
  public abstract partial class PSSessionConfiguration : System.IDisposable {
    // Constructors
    protected PSSessionConfiguration() { }
     
    // Methods
    public void Dispose() { }
    protected virtual void Dispose(bool isDisposing) { }
    public virtual System.Management.Automation.PSPrimitiveDictionary GetApplicationPrivateData(System.Management.Automation.Remoting.PSSenderInfo senderInfo) { return default(System.Management.Automation.PSPrimitiveDictionary); }
    public abstract System.Management.Automation.Runspaces.InitialSessionState GetInitialSessionState(System.Management.Automation.Remoting.PSSenderInfo senderInfo);
    public virtual System.Management.Automation.Runspaces.InitialSessionState GetInitialSessionState(System.Management.Automation.Remoting.PSSessionConfigurationData sessionConfigurationData, System.Management.Automation.Remoting.PSSenderInfo senderInfo, string configProviderId) { return default(System.Management.Automation.Runspaces.InitialSessionState); }
    public virtual System.Nullable<int> GetMaximumReceivedDataSizePerCommand(System.Management.Automation.Remoting.PSSenderInfo senderInfo) { return default(System.Nullable<int>); }
    public virtual System.Nullable<int> GetMaximumReceivedObjectSize(System.Management.Automation.Remoting.PSSenderInfo senderInfo) { return default(System.Nullable<int>); }
  }
  public sealed partial class PSSessionConfigurationData {
    internal PSSessionConfigurationData() { }
    // Fields
    public static bool IsServerManager;
     
    // Properties
    public System.Collections.Generic.List<string> ModulesToImport { get { return default(System.Collections.Generic.List<string>); } }
    public string PrivateData { get { return default(string); } }
     
    // Methods
  }
  public sealed partial class PSSessionOption {
    // Constructors
    public PSSessionOption() { }
     
    // Properties
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
     
    // Methods
  }
  public enum SessionType {
    // Fields
    Default = 2,
    Empty = 0,
    RestrictedRemoteServer = 1,
  }
}
namespace System.Management.Automation.Remoting.WSMan {
  public static partial class WSManServerChannelEvents {
    // Events
    public static event System.EventHandler ShuttingDown { add { } remove { } }
     
    // Methods
  }
}
namespace System.Management.Automation.Runspaces {
  public sealed partial class AliasPropertyData : System.Management.Automation.Runspaces.TypeMemberData {
    // Constructors
    public AliasPropertyData(string name, string referencedMemberName) { }
    public AliasPropertyData(string name, string referencedMemberName, System.Type type) { }
     
    // Properties
    public bool IsHidden { get { return default(bool); } set { } }
    public System.Type MemberType { get { return default(System.Type); } set { } }
    public string ReferencedMemberName { get { return default(string); } set { } }
     
    // Methods
  }
  public sealed partial class AssemblyConfigurationEntry : System.Management.Automation.Runspaces.RunspaceConfigurationEntry {
    // Constructors
    public AssemblyConfigurationEntry(string name, string fileName) : base (default(string)) { }
     
    // Properties
    public string FileName { get { return default(string); } }
     
    // Methods
  }
  public enum AuthenticationMechanism {
    // Fields
    Basic = 1,
    Credssp = 4,
    Default = 0,
    Digest = 5,
    Kerberos = 6,
    Negotiate = 2,
    NegotiateWithImplicitCredential = 3,
  }
  public sealed partial class CmdletConfigurationEntry : System.Management.Automation.Runspaces.RunspaceConfigurationEntry {
    // Constructors
    public CmdletConfigurationEntry(string name, System.Type implementingType, string helpFileName) : base (default(string)) { }
     
    // Properties
    public string HelpFileName { get { return default(string); } }
    public System.Type ImplementingType { get { return default(System.Type); } }
     
    // Methods
  }
  public sealed partial class CodeMethodData : System.Management.Automation.Runspaces.TypeMemberData {
    // Constructors
    public CodeMethodData(string name, System.Reflection.MethodInfo methodToCall) { }
     
    // Properties
    public System.Reflection.MethodInfo CodeReference { get { return default(System.Reflection.MethodInfo); } set { } }
     
    // Methods
  }
  public sealed partial class CodePropertyData : System.Management.Automation.Runspaces.TypeMemberData {
    // Constructors
    public CodePropertyData(string name, System.Reflection.MethodInfo getMethod) { }
    public CodePropertyData(string name, System.Reflection.MethodInfo getMethod, System.Reflection.MethodInfo setMethod) { }
     
    // Properties
    public System.Reflection.MethodInfo GetCodeReference { get { return default(System.Reflection.MethodInfo); } set { } }
    public bool IsHidden { get { return default(bool); } set { } }
    public System.Reflection.MethodInfo SetCodeReference { get { return default(System.Reflection.MethodInfo); } set { } }
     
    // Methods
  }
  public sealed partial class Command {
    // Constructors
    public Command(string command) { }
    public Command(string command, bool isScript) { }
    public Command(string command, bool isScript, bool useLocalScope) { }
     
    // Properties
    public string CommandText { get { return default(string); } }
    public bool IsScript { get { return default(bool); } }
    public System.Management.Automation.Runspaces.PipelineResultTypes MergeUnclaimedPreviousCommandResults { get { return default(System.Management.Automation.Runspaces.PipelineResultTypes); } set { } }
    public System.Management.Automation.Runspaces.CommandParameterCollection Parameters { get { return default(System.Management.Automation.Runspaces.CommandParameterCollection); } }
    public bool UseLocalScope { get { return default(bool); } }
     
    // Methods
    public void MergeMyResults(System.Management.Automation.Runspaces.PipelineResultTypes myResult, System.Management.Automation.Runspaces.PipelineResultTypes toResult) { }
    public override string ToString() { return default(string); }
  }
  public sealed partial class CommandCollection : System.Collections.ObjectModel.Collection<System.Management.Automation.Runspaces.Command> {
    internal CommandCollection() { }
    // Methods
    public void Add(string command) { }
    public void AddScript(string scriptContents) { }
    public void AddScript(string scriptContents, bool useLocalScope) { }
  }
  public sealed partial class CommandParameter {
    // Constructors
    public CommandParameter(string name) { }
    public CommandParameter(string name, object value) { }
     
    // Properties
    public string Name { get { return default(string); } }
    public object Value { get { return default(object); } }
     
    // Methods
  }
  public sealed partial class CommandParameterCollection : System.Collections.ObjectModel.Collection<System.Management.Automation.Runspaces.CommandParameter> {
    // Constructors
    public CommandParameterCollection() { }
     
    // Methods
    public void Add(string name) { }
    public void Add(string name, object value) { }
  }
  public abstract partial class ConstrainedSessionStateEntry : System.Management.Automation.Runspaces.InitialSessionStateEntry {
    // Constructors
    protected ConstrainedSessionStateEntry(string name, System.Management.Automation.SessionStateEntryVisibility visibility) : base (default(string)) { }
     
    // Properties
    public System.Management.Automation.SessionStateEntryVisibility Visibility { get { return default(System.Management.Automation.SessionStateEntryVisibility); } set { } }
     
    // Methods
  }
  public sealed partial class FormatConfigurationEntry : System.Management.Automation.Runspaces.RunspaceConfigurationEntry {
    // Constructors
    public FormatConfigurationEntry(System.Management.Automation.ExtendedTypeDefinition typeDefinition) : base (default(string)) { }
    public FormatConfigurationEntry(string fileName) : base (default(string)) { }
    public FormatConfigurationEntry(string name, string fileName) : base (default(string)) { }
     
    // Properties
    public string FileName { get { return default(string); } }
    public System.Management.Automation.ExtendedTypeDefinition FormatData { get { return default(System.Management.Automation.ExtendedTypeDefinition); } }
     
    // Methods
  }
  public sealed partial class FormatTable {
    // Constructors
    public FormatTable(System.Collections.Generic.IEnumerable<string> formatFiles) { }
     
    // Methods
    public void AppendFormatData(System.Collections.Generic.IEnumerable<System.Management.Automation.ExtendedTypeDefinition> formatData) { }
    public static System.Management.Automation.Runspaces.FormatTable LoadDefaultFormatFiles() { return default(System.Management.Automation.Runspaces.FormatTable); }
    public void PrependFormatData(System.Collections.Generic.IEnumerable<System.Management.Automation.ExtendedTypeDefinition> formatData) { }
  }
  public partial class FormatTableLoadException : System.Management.Automation.RuntimeException {
    // Constructors
    public FormatTableLoadException() { }
#if RUNTIME_SERIALIZATION
    protected FormatTableLoadException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public FormatTableLoadException(string message) { }
    public FormatTableLoadException(string message, System.Exception innerException) { }
     
    // Properties
    public System.Collections.ObjectModel.Collection<string> Errors { get { return default(System.Collections.ObjectModel.Collection<string>); } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    protected void SetDefaultErrorRecord() { }
  }
  public partial class InitialSessionState {
    // Constructors
    protected InitialSessionState() { }
     
    // Properties
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
     
    // Methods
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
    public System.Management.Automation.PSSnapInInfo ImportPSSnapIn(string name, out System.Management.Automation.Runspaces.PSSnapInException warning) { warning = default(System.Management.Automation.Runspaces.PSSnapInException); return default(System.Management.Automation.PSSnapInInfo); }
  }
  public abstract partial class InitialSessionStateEntry {
    // Constructors
    protected InitialSessionStateEntry(string name) { }
     
    // Properties
    public System.Management.Automation.PSModuleInfo Module { get { return default(System.Management.Automation.PSModuleInfo); } }
    public string Name { get { return default(string); } }
    public System.Management.Automation.PSSnapInInfo PSSnapIn { get { return default(System.Management.Automation.PSSnapInInfo); } }
     
    // Methods
    public abstract System.Management.Automation.Runspaces.InitialSessionStateEntry Clone();
  }
  public sealed partial class InitialSessionStateEntryCollection<T> : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable where T : System.Management.Automation.Runspaces.InitialSessionStateEntry {
    // Constructors
    public InitialSessionStateEntryCollection() { }
    public InitialSessionStateEntryCollection(System.Collections.Generic.IEnumerable<T> items) { }
     
    // Properties
    public int Count { get { return default(int); } }
    public T this[int index] { get { return default(T); } }
    public System.Collections.ObjectModel.Collection<T> this[string name] { get { return default(System.Collections.ObjectModel.Collection<T>); } }
     
    // Methods
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
  public partial class InvalidPipelineStateException
#if RUNTIME_SERIALIZATION
        : System.SystemException
#else
        : System.Exception
#endif
    {
    // Constructors
    public InvalidPipelineStateException() { }
    public InvalidPipelineStateException(string message) { }
    public InvalidPipelineStateException(string message, System.Exception innerException) { }
     
    // Properties
    public System.Management.Automation.Runspaces.PipelineState CurrentState { get { return default(System.Management.Automation.Runspaces.PipelineState); } }
    public System.Management.Automation.Runspaces.PipelineState ExpectedState { get { return default(System.Management.Automation.Runspaces.PipelineState); } }
     
    // Methods
  }
  public partial class InvalidRunspacePoolStateException
#if RUNTIME_SERIALIZATION
        : System.SystemException
#else
        : System.Exception
#endif
    {
    // Constructors
    public InvalidRunspacePoolStateException() { }
#if RUNTIME_SERIALIZATION
    protected InvalidRunspacePoolStateException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public InvalidRunspacePoolStateException(string message) { }
    public InvalidRunspacePoolStateException(string message, System.Exception innerException) { }
     
    // Properties
    public System.Management.Automation.Runspaces.RunspacePoolState CurrentState { get { return default(System.Management.Automation.Runspaces.RunspacePoolState); } }
    public System.Management.Automation.Runspaces.RunspacePoolState ExpectedState { get { return default(System.Management.Automation.Runspaces.RunspacePoolState); } }
     
    // Methods
  }
  public partial class InvalidRunspaceStateException
#if RUNTIME_SERIALIZATION
        : System.SystemException
#else
        : System.Exception
#endif
    {
    // Constructors
    public InvalidRunspaceStateException() { }
#if RUNTIME_SERIALIZATION
    protected InvalidRunspaceStateException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public InvalidRunspaceStateException(string message) { }
    public InvalidRunspaceStateException(string message, System.Exception innerException) { }
     
    // Properties
    public System.Management.Automation.Runspaces.RunspaceState CurrentState { get { return default(System.Management.Automation.Runspaces.RunspaceState); } }
    public System.Management.Automation.Runspaces.RunspaceState ExpectedState { get { return default(System.Management.Automation.Runspaces.RunspaceState); } }
     
    // Methods
  }
  public sealed partial class NotePropertyData : System.Management.Automation.Runspaces.TypeMemberData {
    // Constructors
    public NotePropertyData(string name, object value) { }
     
    // Properties
    public bool IsHidden { get { return default(bool); } set { } }
    public object Value { get { return default(object); } set { } }
     
    // Methods
  }
  public enum OutputBufferingMode {
    // Fields
    Block = 2,
    Drop = 1,
    None = 0,
  }
  public abstract partial class Pipeline : System.IDisposable {
    internal Pipeline() { }
    // Properties
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
     
    // Methods
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
  public abstract partial class PipelineReader<T> {
    // Constructors
    protected PipelineReader() { }
     
    // Properties
    public abstract int Count { get; }
    public abstract bool EndOfPipeline { get; }
    public abstract bool IsOpen { get; }
    public abstract int MaxCapacity { get; }
    public abstract System.Threading.WaitHandle WaitHandle { get; }
     
    // Events
    public abstract event System.EventHandler DataReady;
     
    // Methods
    public abstract void Close();
    public abstract System.Collections.ObjectModel.Collection<T> NonBlockingRead();
    public abstract System.Collections.ObjectModel.Collection<T> NonBlockingRead(int maxRequested);
    public abstract T Peek();
    public abstract T Read();
    public abstract System.Collections.ObjectModel.Collection<T> Read(int count);
    public abstract System.Collections.ObjectModel.Collection<T> ReadToEnd();
  }
  [System.FlagsAttribute]
  public enum PipelineResultTypes {
    // Fields
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
    // Fields
    Completed = 4,
    Disconnected = 6,
    Failed = 5,
    NotStarted = 0,
    Running = 1,
    Stopped = 3,
    Stopping = 2,
  }
  public sealed partial class PipelineStateEventArgs : System.EventArgs {
    internal PipelineStateEventArgs() { }
    // Properties
    public System.Management.Automation.Runspaces.PipelineStateInfo PipelineStateInfo { get { return default(System.Management.Automation.Runspaces.PipelineStateInfo); } }
     
    // Methods
  }
  public sealed partial class PipelineStateInfo {
    internal PipelineStateInfo() { }
    // Properties
    public System.Exception Reason { get { return default(System.Exception); } }
    public System.Management.Automation.Runspaces.PipelineState State { get { return default(System.Management.Automation.Runspaces.PipelineState); } }
     
    // Methods
  }
  public abstract partial class PipelineWriter {
    // Constructors
    protected PipelineWriter() { }
     
    // Properties
    public abstract int Count { get; }
    public abstract bool IsOpen { get; }
    public abstract int MaxCapacity { get; }
    public abstract System.Threading.WaitHandle WaitHandle { get; }
     
    // Methods
    public abstract void Close();
    public abstract void Flush();
    public abstract int Write(object obj);
    public abstract int Write(object obj, bool enumerateCollection);
  }
  public sealed partial class PowerShellProcessInstance : System.IDisposable {
    // Constructors
    public PowerShellProcessInstance() { }
    public PowerShellProcessInstance(System.Version powerShellVersion, System.Management.Automation.PSCredential credential, System.Management.Automation.ScriptBlock initializationScript, bool useWow64) { }
     
    // Properties
    public bool HasExited { get { return default(bool); } }
    public System.Diagnostics.Process Process { get { return default(System.Diagnostics.Process); } }
     
    // Methods
    public void Dispose() { }
  }
  public sealed partial class PropertySetData {
    // Constructors
    public PropertySetData(System.Collections.Generic.IEnumerable<string> referencedProperties) { }
     
    // Properties
    public System.Collections.ObjectModel.Collection<string> ReferencedProperties { get { return default(System.Collections.ObjectModel.Collection<string>); } }
     
    // Methods
  }
  public sealed partial class ProviderConfigurationEntry : System.Management.Automation.Runspaces.RunspaceConfigurationEntry {
    // Constructors
    public ProviderConfigurationEntry(string name, System.Type implementingType, string helpFileName) : base (default(string)) { }
     
    // Properties
    public string HelpFileName { get { return default(string); } }
    public System.Type ImplementingType { get { return default(System.Type); } }
     
    // Methods
  }
  public partial class PSConsoleLoadException
#if RUNTIME_SERIALIZATION
        : System.SystemException
#else
        : System.Exception
#endif
        , System.Management.Automation.IContainsErrorRecord
    {
    // Constructors
    public PSConsoleLoadException() { }
#if RUNTIME_SERIALIZATION
    protected PSConsoleLoadException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public PSConsoleLoadException(string message) { }
    public PSConsoleLoadException(string message, System.Exception innerException) { }
     
    // Properties
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override string Message { get { return default(string); } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
  }
  public sealed partial class PSSession {
    internal PSSession() { }
    // Properties
    public System.Management.Automation.PSPrimitiveDictionary ApplicationPrivateData { get { return default(System.Management.Automation.PSPrimitiveDictionary); } }
    public System.Management.Automation.Runspaces.RunspaceAvailability Availability { get { return default(System.Management.Automation.Runspaces.RunspaceAvailability); } }
    public string ComputerName { get { return default(string); } }
    public string ConfigurationName { get { return default(string); } }
    public int Id { get { return default(int); } }
    public System.Guid InstanceId { get { return default(System.Guid); } }
    public string Name { get { return default(string); } set { } }
    public System.Management.Automation.Runspaces.Runspace Runspace { get { return default(System.Management.Automation.Runspaces.Runspace); } }
     
    // Methods
    public override string ToString() { return default(string); }
  }
  public enum PSSessionConfigurationAccessMode {
    // Fields
    Disabled = 0,
    Local = 1,
    Remote = 2,
  }
  public enum PSSessionType {
    // Fields
    DefaultRemoteShell = 0,
    Workflow = 1,
  }
  public partial class PSSnapInException : System.Management.Automation.RuntimeException {
    // Constructors
    public PSSnapInException() { }
#if RUNTIME_SERIALIZATION
    protected PSSnapInException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public PSSnapInException(string message) { }
    public PSSnapInException(string message, System.Exception innerException) { }
     
    // Properties
    public override System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override string Message { get { return default(string); } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
  }
  public enum PSThreadOptions {
    // Fields
    Default = 0,
    ReuseThread = 2,
    UseCurrentThread = 3,
    UseNewThread = 1,
  }
  [System.Runtime.Serialization.DataContractAttribute]
  public partial class RemotingDebugRecord : System.Management.Automation.DebugRecord {
    // Constructors
    public RemotingDebugRecord(string message, System.Management.Automation.Remoting.OriginInfo originInfo) : base (default(string)) { }
     
    // Properties
    public System.Management.Automation.Remoting.OriginInfo OriginInfo { get { return default(System.Management.Automation.Remoting.OriginInfo); } }
     
    // Methods
  }
  public partial class RemotingErrorRecord : System.Management.Automation.ErrorRecord {
    // Constructors
    public RemotingErrorRecord(System.Management.Automation.ErrorRecord errorRecord, System.Management.Automation.Remoting.OriginInfo originInfo) : base (default(System.Exception), default(string), default(System.Management.Automation.ErrorCategory), default(object)) { }
#if RUNTIME_SERIALIZATION
    protected RemotingErrorRecord(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base (default(System.Exception), default(string), default(System.Management.Automation.ErrorCategory), default(object)) { }
#endif
     
    // Properties
    public System.Management.Automation.Remoting.OriginInfo OriginInfo { get { return default(System.Management.Automation.Remoting.OriginInfo); } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
  }
  [System.Runtime.Serialization.DataContractAttribute]
  public partial class RemotingProgressRecord : System.Management.Automation.ProgressRecord {
    // Constructors
    public RemotingProgressRecord(System.Management.Automation.ProgressRecord progressRecord, System.Management.Automation.Remoting.OriginInfo originInfo) : base (default(int), default(string), default(string)) { }
     
    // Properties
    public System.Management.Automation.Remoting.OriginInfo OriginInfo { get { return default(System.Management.Automation.Remoting.OriginInfo); } }
     
    // Methods
  }
  [System.Runtime.Serialization.DataContractAttribute]
  public partial class RemotingVerboseRecord : System.Management.Automation.VerboseRecord {
    // Constructors
    public RemotingVerboseRecord(string message, System.Management.Automation.Remoting.OriginInfo originInfo) : base (default(string)) { }
     
    // Properties
    public System.Management.Automation.Remoting.OriginInfo OriginInfo { get { return default(System.Management.Automation.Remoting.OriginInfo); } }
     
    // Methods
  }
  [System.Runtime.Serialization.DataContractAttribute]
  public partial class RemotingWarningRecord : System.Management.Automation.WarningRecord {
    // Constructors
    public RemotingWarningRecord(string message, System.Management.Automation.Remoting.OriginInfo originInfo) : base (default(string)) { }
     
    // Properties
    public System.Management.Automation.Remoting.OriginInfo OriginInfo { get { return default(System.Management.Automation.Remoting.OriginInfo); } }
     
    // Methods
  }
  public abstract partial class Runspace : System.IDisposable {
    internal Runspace() { }
    // Properties
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
    public abstract System.Management.Automation.Runspaces.RunspaceConfiguration RunspaceConfiguration { get; }
    public abstract System.Management.Automation.Runspaces.RunspaceStateInfo RunspaceStateInfo { get; }
    public System.Management.Automation.Runspaces.SessionStateProxy SessionStateProxy { get { return default(System.Management.Automation.Runspaces.SessionStateProxy); } }
    public abstract System.Management.Automation.Runspaces.PSThreadOptions ThreadOptions { get; set; }
    public abstract System.Version Version { get; }
     
    // Events
    public abstract event System.EventHandler<System.Management.Automation.Runspaces.RunspaceAvailabilityEventArgs> AvailabilityChanged;
    public abstract event System.EventHandler<System.Management.Automation.Runspaces.RunspaceStateEventArgs> StateChanged;
     
    // Methods
    public void ClearBaseTransaction() { }
    public abstract void Close();
    public abstract void CloseAsync();
    public abstract void Connect();
    public abstract void ConnectAsync();
    public abstract System.Management.Automation.Runspaces.Pipeline CreateDisconnectedPipeline();
    public abstract System.Management.Automation.PowerShell CreateDisconnectedPowerShell();
    public abstract System.Management.Automation.Runspaces.Pipeline CreateNestedPipeline();
    public abstract System.Management.Automation.Runspaces.Pipeline CreateNestedPipeline(string command, bool addToHistory);
    public abstract System.Management.Automation.Runspaces.Pipeline CreatePipeline();
    public abstract System.Management.Automation.Runspaces.Pipeline CreatePipeline(string command);
    public abstract System.Management.Automation.Runspaces.Pipeline CreatePipeline(string command, bool addToHistory);
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
    // Fields
    Available = 1,
    AvailableForNestedCommand = 2,
    Busy = 3,
    None = 0,
  }
  public sealed partial class RunspaceAvailabilityEventArgs : System.EventArgs {
    internal RunspaceAvailabilityEventArgs() { }
    // Properties
    public System.Management.Automation.Runspaces.RunspaceAvailability RunspaceAvailability { get { return default(System.Management.Automation.Runspaces.RunspaceAvailability); } }
     
    // Methods
  }
  public enum RunspaceCapability {
    // Fields
    Default = 0,
    SupportsDisconnect = 1,
  }
  public abstract partial class RunspaceConfiguration {
    // Constructors
    protected RunspaceConfiguration() { }
     
    // Properties
    public virtual System.Management.Automation.Runspaces.RunspaceConfigurationEntryCollection<System.Management.Automation.Runspaces.AssemblyConfigurationEntry> Assemblies { get { return default(System.Management.Automation.Runspaces.RunspaceConfigurationEntryCollection<System.Management.Automation.Runspaces.AssemblyConfigurationEntry>); } }
    public virtual System.Management.Automation.AuthorizationManager AuthorizationManager { get { return default(System.Management.Automation.AuthorizationManager); } }
    public virtual System.Management.Automation.Runspaces.RunspaceConfigurationEntryCollection<System.Management.Automation.Runspaces.CmdletConfigurationEntry> Cmdlets { get { return default(System.Management.Automation.Runspaces.RunspaceConfigurationEntryCollection<System.Management.Automation.Runspaces.CmdletConfigurationEntry>); } }
    public virtual System.Management.Automation.Runspaces.RunspaceConfigurationEntryCollection<System.Management.Automation.Runspaces.FormatConfigurationEntry> Formats { get { return default(System.Management.Automation.Runspaces.RunspaceConfigurationEntryCollection<System.Management.Automation.Runspaces.FormatConfigurationEntry>); } }
    public virtual System.Management.Automation.Runspaces.RunspaceConfigurationEntryCollection<System.Management.Automation.Runspaces.ScriptConfigurationEntry> InitializationScripts { get { return default(System.Management.Automation.Runspaces.RunspaceConfigurationEntryCollection<System.Management.Automation.Runspaces.ScriptConfigurationEntry>); } }
    public virtual System.Management.Automation.Runspaces.RunspaceConfigurationEntryCollection<System.Management.Automation.Runspaces.ProviderConfigurationEntry> Providers { get { return default(System.Management.Automation.Runspaces.RunspaceConfigurationEntryCollection<System.Management.Automation.Runspaces.ProviderConfigurationEntry>); } }
    public virtual System.Management.Automation.Runspaces.RunspaceConfigurationEntryCollection<System.Management.Automation.Runspaces.ScriptConfigurationEntry> Scripts { get { return default(System.Management.Automation.Runspaces.RunspaceConfigurationEntryCollection<System.Management.Automation.Runspaces.ScriptConfigurationEntry>); } }
    public abstract string ShellId { get; }
    public virtual System.Management.Automation.Runspaces.RunspaceConfigurationEntryCollection<System.Management.Automation.Runspaces.TypeConfigurationEntry> Types { get { return default(System.Management.Automation.Runspaces.RunspaceConfigurationEntryCollection<System.Management.Automation.Runspaces.TypeConfigurationEntry>); } }
     
    // Methods
    public System.Management.Automation.PSSnapInInfo AddPSSnapIn(string name, out System.Management.Automation.Runspaces.PSSnapInException warning) { warning = default(System.Management.Automation.Runspaces.PSSnapInException); return default(System.Management.Automation.PSSnapInInfo); }
    public static System.Management.Automation.Runspaces.RunspaceConfiguration Create() { return default(System.Management.Automation.Runspaces.RunspaceConfiguration); }
    public static System.Management.Automation.Runspaces.RunspaceConfiguration Create(string assemblyName) { return default(System.Management.Automation.Runspaces.RunspaceConfiguration); }
    public static System.Management.Automation.Runspaces.RunspaceConfiguration Create(string consoleFilePath, out System.Management.Automation.Runspaces.PSConsoleLoadException warnings) { warnings = default(System.Management.Automation.Runspaces.PSConsoleLoadException); return default(System.Management.Automation.Runspaces.RunspaceConfiguration); }
    public System.Management.Automation.PSSnapInInfo RemovePSSnapIn(string name, out System.Management.Automation.Runspaces.PSSnapInException warning) { warning = default(System.Management.Automation.Runspaces.PSSnapInException); return default(System.Management.Automation.PSSnapInInfo); }
  }
  public partial class RunspaceConfigurationAttributeException
#if RUNTIME_SERIALIZATION
        : System.SystemException
#else
        : System.Exception
#endif
        , System.Management.Automation.IContainsErrorRecord
    {
    // Constructors
    public RunspaceConfigurationAttributeException() { }
#if RUNTIME_SERIALIZATION
    protected RunspaceConfigurationAttributeException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public RunspaceConfigurationAttributeException(string message) { }
    public RunspaceConfigurationAttributeException(string message, System.Exception innerException) { }
     
    // Properties
    public string AssemblyName { get { return default(string); } }
    public string Error { get { return default(string); } }
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override string Message { get { return default(string); } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
  }
  public abstract partial class RunspaceConfigurationEntry {
    // Constructors
    protected RunspaceConfigurationEntry(string name) { }
     
    // Properties
    public bool BuiltIn { get { return default(bool); } }
    public string Name { get { return default(string); } }
    public System.Management.Automation.PSSnapInInfo PSSnapIn { get { return default(System.Management.Automation.PSSnapInInfo); } }
     
    // Methods
  }
  public sealed partial class RunspaceConfigurationEntryCollection<T> : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable where T : System.Management.Automation.Runspaces.RunspaceConfigurationEntry {
    // Constructors
    public RunspaceConfigurationEntryCollection() { }
    public RunspaceConfigurationEntryCollection(System.Collections.Generic.IEnumerable<T> items) { }
     
    // Properties
    public int Count { get { return default(int); } }
    public T this[int index] { get { return default(T); } }
     
    // Methods
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
  public sealed partial class RunspaceConfigurationTypeAttribute : System.Attribute {
    // Constructors
    public RunspaceConfigurationTypeAttribute(string runspaceConfigurationType) { }
     
    // Properties
    public string RunspaceConfigurationType { get { return default(string); } }
     
    // Methods
  }
  public partial class RunspaceConfigurationTypeException
#if RUNTIME_SERIALIZATION
        : System.SystemException
#else
        : System.Exception
#endif
        , System.Management.Automation.IContainsErrorRecord
    {
    // Constructors
    public RunspaceConfigurationTypeException() { }
#if RUNTIME_SERIALIZATION
    protected RunspaceConfigurationTypeException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public RunspaceConfigurationTypeException(string message) { }
    public RunspaceConfigurationTypeException(string message, System.Exception innerException) { }
     
    // Properties
    public string AssemblyName { get { return default(string); } }
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override string Message { get { return default(string); } }
    public string TypeName { get { return default(string); } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
  }
  public abstract partial class RunspaceConnectionInfo {
    // Constructors
    protected RunspaceConnectionInfo() { }
     
    // Properties
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
     
    // Methods
    public virtual void SetSessionOptions(System.Management.Automation.Remoting.PSSessionOption options) { }
  }
  public static partial class RunspaceFactory {
    // Methods
    public static System.Management.Automation.Runspaces.Runspace CreateOutOfProcessRunspace(System.Management.Automation.Runspaces.TypeTable typeTable) { return default(System.Management.Automation.Runspaces.Runspace); }
    public static System.Management.Automation.Runspaces.Runspace CreateOutOfProcessRunspace(System.Management.Automation.Runspaces.TypeTable typeTable, System.Management.Automation.Runspaces.PowerShellProcessInstance processInstance) { return default(System.Management.Automation.Runspaces.Runspace); }
    public static System.Management.Automation.Runspaces.Runspace CreateRunspace() { return default(System.Management.Automation.Runspaces.Runspace); }
    public static System.Management.Automation.Runspaces.Runspace CreateRunspace(System.Management.Automation.Host.PSHost host) { return default(System.Management.Automation.Runspaces.Runspace); }
    public static System.Management.Automation.Runspaces.Runspace CreateRunspace(System.Management.Automation.Host.PSHost host, System.Management.Automation.Runspaces.InitialSessionState initialSessionState) { return default(System.Management.Automation.Runspaces.Runspace); }
    public static System.Management.Automation.Runspaces.Runspace CreateRunspace(System.Management.Automation.Host.PSHost host, System.Management.Automation.Runspaces.RunspaceConfiguration runspaceConfiguration) { return default(System.Management.Automation.Runspaces.Runspace); }
    public static System.Management.Automation.Runspaces.Runspace CreateRunspace(System.Management.Automation.Host.PSHost host, System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo) { return default(System.Management.Automation.Runspaces.Runspace); }
    public static System.Management.Automation.Runspaces.Runspace CreateRunspace(System.Management.Automation.Runspaces.InitialSessionState initialSessionState) { return default(System.Management.Automation.Runspaces.Runspace); }
    public static System.Management.Automation.Runspaces.Runspace CreateRunspace(System.Management.Automation.Runspaces.RunspaceConfiguration runspaceConfiguration) { return default(System.Management.Automation.Runspaces.Runspace); }
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
  public partial class RunspaceOpenModuleLoadException : System.Management.Automation.RuntimeException {
    // Constructors
    public RunspaceOpenModuleLoadException() { }
#if RUNTIME_SERIALIZATION
    protected RunspaceOpenModuleLoadException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public RunspaceOpenModuleLoadException(string message) { }
    public RunspaceOpenModuleLoadException(string message, System.Exception innerException) { }
     
    // Properties
    public System.Management.Automation.PSDataCollection<System.Management.Automation.ErrorRecord> ErrorRecords { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.ErrorRecord>); } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
  }
  public sealed partial class RunspacePool : System.IDisposable {
    internal RunspacePool() { }
    // Properties
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
     
    // Methods
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
    // Fields
    Available = 1,
    Busy = 2,
    None = 0,
  }
  public enum RunspacePoolCapability {
    // Fields
    Default = 0,
    SupportsDisconnect = 1,
  }
  public enum RunspacePoolState {
    // Fields
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
  public sealed partial class RunspacePoolStateChangedEventArgs : System.EventArgs {
    internal RunspacePoolStateChangedEventArgs() { }
    // Properties
    public System.Management.Automation.RunspacePoolStateInfo RunspacePoolStateInfo { get { return default(System.Management.Automation.RunspacePoolStateInfo); } }
     
    // Methods
  }
  public enum RunspaceState {
    // Fields
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
  public sealed partial class RunspaceStateEventArgs : System.EventArgs {
    internal RunspaceStateEventArgs() { }
    // Properties
    public System.Management.Automation.Runspaces.RunspaceStateInfo RunspaceStateInfo { get { return default(System.Management.Automation.Runspaces.RunspaceStateInfo); } }
     
    // Methods
  }
  public sealed partial class RunspaceStateInfo {
    internal RunspaceStateInfo() { }
    // Properties
    public System.Exception Reason { get { return default(System.Exception); } }
    public System.Management.Automation.Runspaces.RunspaceState State { get { return default(System.Management.Automation.Runspaces.RunspaceState); } }
     
    // Methods
    public override string ToString() { return default(string); }
  }
  public sealed partial class ScriptConfigurationEntry : System.Management.Automation.Runspaces.RunspaceConfigurationEntry {
    // Constructors
    public ScriptConfigurationEntry(string name, string definition) : base (default(string)) { }
     
    // Properties
    public string Definition { get { return default(string); } }
     
    // Methods
  }
  public sealed partial class ScriptMethodData : System.Management.Automation.Runspaces.TypeMemberData {
    // Constructors
    public ScriptMethodData(string name, System.Management.Automation.ScriptBlock scriptToInvoke) { }
     
    // Properties
    public System.Management.Automation.ScriptBlock Script { get { return default(System.Management.Automation.ScriptBlock); } set { } }
     
    // Methods
  }
  public sealed partial class ScriptPropertyData : System.Management.Automation.Runspaces.TypeMemberData {
    // Constructors
    public ScriptPropertyData(string name, System.Management.Automation.ScriptBlock getScriptBlock) { }
    public ScriptPropertyData(string name, System.Management.Automation.ScriptBlock getScriptBlock, System.Management.Automation.ScriptBlock setScriptBlock) { }
     
    // Properties
    public System.Management.Automation.ScriptBlock GetScriptBlock { get { return default(System.Management.Automation.ScriptBlock); } set { } }
    public bool IsHidden { get { return default(bool); } set { } }
    public System.Management.Automation.ScriptBlock SetScriptBlock { get { return default(System.Management.Automation.ScriptBlock); } set { } }
     
    // Methods
  }
  public sealed partial class SessionStateAliasEntry : System.Management.Automation.Runspaces.SessionStateCommandEntry {
    // Constructors
    public SessionStateAliasEntry(string name, string definition) : base (default(string)) { }
    public SessionStateAliasEntry(string name, string definition, string description) : base (default(string)) { }
    public SessionStateAliasEntry(string name, string definition, string description, System.Management.Automation.ScopedItemOptions options) : base (default(string)) { }
     
    // Properties
    public string Definition { get { return default(string); } }
    public string Description { get { return default(string); } }
    public System.Management.Automation.ScopedItemOptions Options { get { return default(System.Management.Automation.ScopedItemOptions); } }
     
    // Methods
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone() { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }
  }
  public sealed partial class SessionStateApplicationEntry : System.Management.Automation.Runspaces.SessionStateCommandEntry {
    // Constructors
    public SessionStateApplicationEntry(string path) : base (default(string)) { }
     
    // Properties
    public string Path { get { return default(string); } }
     
    // Methods
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone() { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }
  }
  public sealed partial class SessionStateAssemblyEntry : System.Management.Automation.Runspaces.InitialSessionStateEntry {
    // Constructors
    public SessionStateAssemblyEntry(string name) : base (default(string)) { }
    public SessionStateAssemblyEntry(string name, string fileName) : base (default(string)) { }
     
    // Properties
    public string FileName { get { return default(string); } }
     
    // Methods
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone() { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }
  }
  public sealed partial class SessionStateCmdletEntry : System.Management.Automation.Runspaces.SessionStateCommandEntry {
    // Constructors
    public SessionStateCmdletEntry(string name, System.Type implementingType, string helpFileName) : base (default(string)) { }
     
    // Properties
    public string HelpFileName { get { return default(string); } }
    public System.Type ImplementingType { get { return default(System.Type); } }
     
    // Methods
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone() { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }
  }
  public abstract partial class SessionStateCommandEntry : System.Management.Automation.Runspaces.ConstrainedSessionStateEntry {
    // Constructors
    protected SessionStateCommandEntry(string name) : base (default(string), default(System.Management.Automation.SessionStateEntryVisibility)) { }
    protected internal SessionStateCommandEntry(string name, System.Management.Automation.SessionStateEntryVisibility visibility) : base (default(string), default(System.Management.Automation.SessionStateEntryVisibility)) { }
     
    // Properties
    public System.Management.Automation.CommandTypes CommandType { get { return default(System.Management.Automation.CommandTypes); } }
     
    // Methods
  }
  public sealed partial class SessionStateFormatEntry : System.Management.Automation.Runspaces.InitialSessionStateEntry {
    // Constructors
    public SessionStateFormatEntry(System.Management.Automation.ExtendedTypeDefinition typeDefinition) : base (default(string)) { }
    public SessionStateFormatEntry(System.Management.Automation.Runspaces.FormatTable formattable) : base (default(string)) { }
    public SessionStateFormatEntry(string fileName) : base (default(string)) { }
     
    // Properties
    public string FileName { get { return default(string); } }
    public System.Management.Automation.ExtendedTypeDefinition FormatData { get { return default(System.Management.Automation.ExtendedTypeDefinition); } }
    public System.Management.Automation.Runspaces.FormatTable Formattable { get { return default(System.Management.Automation.Runspaces.FormatTable); } }
     
    // Methods
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone() { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }
  }
  public sealed partial class SessionStateFunctionEntry : System.Management.Automation.Runspaces.SessionStateCommandEntry {
    // Constructors
    public SessionStateFunctionEntry(string name, string definition) : base (default(string)) { }
    public SessionStateFunctionEntry(string name, string definition, System.Management.Automation.ScopedItemOptions options, string helpFile) : base (default(string)) { }
    public SessionStateFunctionEntry(string name, string definition, string helpFile) : base (default(string)) { }
     
    // Properties
    public string Definition { get { return default(string); } }
    public string HelpFile { get { return default(string); } }
    public System.Management.Automation.ScopedItemOptions Options { get { return default(System.Management.Automation.ScopedItemOptions); } }
     
    // Methods
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone() { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }
  }
  public sealed partial class SessionStateProviderEntry : System.Management.Automation.Runspaces.ConstrainedSessionStateEntry {
    // Constructors
    public SessionStateProviderEntry(string name, System.Type implementingType, string helpFileName) : base (default(string), default(System.Management.Automation.SessionStateEntryVisibility)) { }
     
    // Properties
    public string HelpFileName { get { return default(string); } }
    public System.Type ImplementingType { get { return default(System.Type); } }
     
    // Methods
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone() { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }
  }
  public partial class SessionStateProxy {
    internal SessionStateProxy() { }
    // Properties
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
     
    // Methods
    public virtual object GetVariable(string name) { return default(object); }
    public virtual void SetVariable(string name, object value) { }
  }
  public sealed partial class SessionStateScriptEntry : System.Management.Automation.Runspaces.SessionStateCommandEntry {
    // Constructors
    public SessionStateScriptEntry(string path) : base (default(string)) { }
     
    // Properties
    public string Path { get { return default(string); } }
     
    // Methods
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone() { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }
  }
  public sealed partial class SessionStateTypeEntry : System.Management.Automation.Runspaces.InitialSessionStateEntry {
    // Constructors
    public SessionStateTypeEntry(System.Management.Automation.Runspaces.TypeData typeData, bool isRemove) : base (default(string)) { }
    public SessionStateTypeEntry(System.Management.Automation.Runspaces.TypeTable typeTable) : base (default(string)) { }
    public SessionStateTypeEntry(string fileName) : base (default(string)) { }
     
    // Properties
    public string FileName { get { return default(string); } }
    public bool IsRemove { get { return default(bool); } }
    public System.Management.Automation.Runspaces.TypeData TypeData { get { return default(System.Management.Automation.Runspaces.TypeData); } }
    public System.Management.Automation.Runspaces.TypeTable TypeTable { get { return default(System.Management.Automation.Runspaces.TypeTable); } }
     
    // Methods
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone() { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }
  }
  public sealed partial class SessionStateVariableEntry : System.Management.Automation.Runspaces.ConstrainedSessionStateEntry {
    // Constructors
    public SessionStateVariableEntry(string name, object value, string description) : base (default(string), default(System.Management.Automation.SessionStateEntryVisibility)) { }
    public SessionStateVariableEntry(string name, object value, string description, System.Management.Automation.ScopedItemOptions options) : base (default(string), default(System.Management.Automation.SessionStateEntryVisibility)) { }
    public SessionStateVariableEntry(string name, object value, string description, System.Management.Automation.ScopedItemOptions options, System.Attribute attribute) : base (default(string), default(System.Management.Automation.SessionStateEntryVisibility)) { }
    public SessionStateVariableEntry(string name, object value, string description, System.Management.Automation.ScopedItemOptions options, System.Collections.ObjectModel.Collection<System.Attribute> attributes) : base (default(string), default(System.Management.Automation.SessionStateEntryVisibility)) { }
     
    // Properties
    public System.Collections.ObjectModel.Collection<System.Attribute> Attributes { get { return default(System.Collections.ObjectModel.Collection<System.Attribute>); } }
    public string Description { get { return default(string); } }
    public System.Management.Automation.ScopedItemOptions Options { get { return default(System.Management.Automation.ScopedItemOptions); } }
    public object Value { get { return default(object); } }
     
    // Methods
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone() { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }
  }
  public sealed partial class SessionStateWorkflowEntry : System.Management.Automation.Runspaces.SessionStateCommandEntry {
    // Constructors
    public SessionStateWorkflowEntry(string name, string definition) : base (default(string)) { }
    public SessionStateWorkflowEntry(string name, string definition, System.Management.Automation.ScopedItemOptions options, string helpFile) : base (default(string)) { }
    public SessionStateWorkflowEntry(string name, string definition, string helpFile) : base (default(string)) { }
     
    // Properties
    public string Definition { get { return default(string); } }
    public string HelpFile { get { return default(string); } }
    public System.Management.Automation.ScopedItemOptions Options { get { return default(System.Management.Automation.ScopedItemOptions); } }
     
    // Methods
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone() { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }
  }
  public sealed partial class TypeConfigurationEntry : System.Management.Automation.Runspaces.RunspaceConfigurationEntry {
    // Constructors
    public TypeConfigurationEntry(System.Management.Automation.Runspaces.TypeData typeData, bool isRemove) : base (default(string)) { }
    public TypeConfigurationEntry(string fileName) : base (default(string)) { }
    public TypeConfigurationEntry(string name, string fileName) : base (default(string)) { }
     
    // Properties
    public string FileName { get { return default(string); } }
    public bool IsRemove { get { return default(bool); } }
    public System.Management.Automation.Runspaces.TypeData TypeData { get { return default(System.Management.Automation.Runspaces.TypeData); } }
     
    // Methods
  }
  public sealed partial class TypeData {
    // Constructors
    public TypeData(string typeName) { }
    public TypeData(System.Type type) { }
     
    // Properties
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
     
    // Methods
    public System.Management.Automation.Runspaces.TypeData Copy() { return default(System.Management.Automation.Runspaces.TypeData); }
  }
  public abstract partial class TypeMemberData {
    internal TypeMemberData() { }
    // Properties
    public string Name { get { return default(string); } }
     
    // Methods
  }
  public sealed partial class TypeTable {
    // Constructors
    public TypeTable(System.Collections.Generic.IEnumerable<string> typeFiles) { }
     
    // Methods
    public void AddType(System.Management.Automation.Runspaces.TypeData typeData) { }
    public System.Management.Automation.Runspaces.TypeTable Clone(bool unshared) { return default(System.Management.Automation.Runspaces.TypeTable); }
    public static System.Collections.Generic.List<string> GetDefaultTypeFiles() { return default(System.Collections.Generic.List<string>); }
    public static System.Management.Automation.Runspaces.TypeTable LoadDefaultTypeFiles() { return default(System.Management.Automation.Runspaces.TypeTable); }
    public void RemoveType(string typeName) { }
  }
  public partial class TypeTableLoadException : System.Management.Automation.RuntimeException {
    // Constructors
    public TypeTableLoadException() { }
#if RUNTIME_SERIALIZATION
    protected TypeTableLoadException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    public TypeTableLoadException(string message) { }
    public TypeTableLoadException(string message, System.Exception innerException) { }
     
    // Properties
    public System.Collections.ObjectModel.Collection<string> Errors { get { return default(System.Collections.ObjectModel.Collection<string>); } }
     
    // Methods
#if RUNTIME_SERIALIZATION
    [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
#endif
    protected void SetDefaultErrorRecord() { }
  }
  public sealed partial class WSManConnectionInfo : System.Management.Automation.Runspaces.RunspaceConnectionInfo {
    // Fields
    public const string HttpScheme = "http";
    public const string HttpsScheme = "https";
     
    // Constructors
    public WSManConnectionInfo() { }
    public WSManConnectionInfo(bool useSsl, string computerName, int port, string appName, string shellUri, System.Management.Automation.PSCredential credential) { }
    public WSManConnectionInfo(bool useSsl, string computerName, int port, string appName, string shellUri, System.Management.Automation.PSCredential credential, int openTimeout) { }
    public WSManConnectionInfo(System.Management.Automation.Runspaces.PSSessionType configurationType) { }
    public WSManConnectionInfo(string scheme, string computerName, int port, string appName, string shellUri, System.Management.Automation.PSCredential credential) { }
    public WSManConnectionInfo(string scheme, string computerName, int port, string appName, string shellUri, System.Management.Automation.PSCredential credential, int openTimeout) { }
    public WSManConnectionInfo(System.Uri uri) { }
    public WSManConnectionInfo(System.Uri uri, string shellUri, System.Management.Automation.PSCredential credential) { }
    public WSManConnectionInfo(System.Uri uri, string shellUri, string certificateThumbprint) { }
     
    // Properties
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
     
    // Methods
    public System.Management.Automation.Runspaces.WSManConnectionInfo Copy() { return default(System.Management.Automation.Runspaces.WSManConnectionInfo); }
    public override void SetSessionOptions(System.Management.Automation.Remoting.PSSessionOption options) { }
  }
}
namespace System.Management.Automation.Security {
  public enum SystemEnforcementMode {
    // Fields
    Audit = 1,
    Enforce = 2,
    None = 0,
  }
  public sealed partial class SystemPolicy {
    internal SystemPolicy() { }
    // Methods
    public static System.Management.Automation.Security.SystemEnforcementMode GetLockdownPolicy(string path, System.Runtime.InteropServices.SafeHandle handle) { return default(System.Management.Automation.Security.SystemEnforcementMode); }
    public static System.Management.Automation.Security.SystemEnforcementMode GetSystemLockdownPolicy() { return default(System.Management.Automation.Security.SystemEnforcementMode); }
  }
}
namespace System.Management.Automation.Sqm {
  public static partial class PSSQMAPI {
    // Methods
    public static void IncrementData(System.Management.Automation.CmdletInfo cmdlet) { }
    public static void IncrementData(System.Management.Automation.CommandTypes cmdType) { }
    public static void IncrementDataPoint(uint dataPoint) { }
    public static void IncrementWorkflowActivityPresent(string activityName) { }
    public static void IncrementWorkflowCommonParameterPresent(string parameterName) { }
    public static void IncrementWorkflowExecuted(string workflowName) { }
    public static void IncrementWorkflowSpecificParameterType(System.Type parameterType) { }
    public static void IncrementWorkflowStateData(System.Guid parentJobInstanceId, System.Management.Automation.JobState state) { }
    public static void IncrementWorkflowType(string workflowType) { }
    public static void InitiateWorkflowStateDataTracking(System.Management.Automation.Job parentJob) { }
    public static void LogAllDataSuppressExceptions() { }
    public static void NoteRunspaceEnd(System.Guid rsInstanceId) { }
    public static void NoteRunspaceStart(System.Guid rsInstanceId) { }
    public static void NoteSessionConfigurationIdleTimeout(int idleTimeout) { }
    public static void NoteSessionConfigurationOutputBufferingMode(string optBufferingMode) { }
    public static void NoteWorkflowCommonParametersValues(string parameterName, uint data) { }
    public static void NoteWorkflowEnd(System.Guid workflowInstanceId) { }
    public static void NoteWorkflowEndpointConfiguration(string quotaName, uint data) { }
    public static void NoteWorkflowOutputStreamSize(uint size, string streamType) { }
    public static void NoteWorkflowStart(System.Guid workflowInstanceId) { }
    public static void UpdateExecutionPolicy(string shellId, Microsoft.PowerShell.ExecutionPolicy executionPolicy) { }
    public static void UpdateWorkflowsConcurrentExecution(uint numberWorkflows) { }
  }
  public enum PSSqmDataPoint : uint {
    // Fields
    Alias = (uint)8337,
    AllCmdlets = (uint)9829,
    Application = (uint)8342,
    Cmdlet = (uint)8338,
    ExecutionPolicy = (uint)8344,
    ExternalScript = (uint)8339,
    Filter = (uint)8340,
    Function = (uint)8341,
    NewObjectCom = (uint)8345,
    None = (uint)0,
    RunspaceDuration = (uint)9830,
    Script = (uint)8343,
    SessionConfigurationIdleTimeout = (uint)8351,
    SessionConfigurationOutputBufferingMode = (uint)8376,
    WorkflowActivities = (uint)9825,
    WorkflowActivitiesCount = (uint)9817,
    WorkflowCommonParametersPresent = (uint)9824,
    WorkflowCommonParametersSpecific = (uint)9869,
    WorkflowCount = (uint)9826,
    WorkflowDomain = (uint)9818,
    WorkflowDuration = (uint)9820,
    WorkflowEndpoint = (uint)9881,
    WorkflowOutputStream = (uint)9882,
    WorkflowProcessConcurrentCount = (uint)9879,
    WorkflowProcessDuration = (uint)9821,
    WorkflowSpecificParametersCount = (uint)9822,
    WorkflowSpecificParameterTypes = (uint)9827,
    WorkflowState = (uint)9880,
    WorkflowType = (uint)9878,
  }
}
#if ETW_AND_TRACING
namespace System.Management.Automation.Tracing {
  public abstract partial class BaseChannelWriter : System.IDisposable {
    // Constructors
    protected BaseChannelWriter() { }
     
    // Properties
    public virtual System.Management.Automation.Tracing.PowerShellTraceKeywords Keywords { get { return default(System.Management.Automation.Tracing.PowerShellTraceKeywords); } set { } }
     
    // Methods
    public virtual void Dispose() { }
    public virtual bool TraceCritical(System.Management.Automation.Tracing.PowerShellTraceEvent traceEvent, System.Management.Automation.Tracing.PowerShellTraceOperationCode operationCode, System.Management.Automation.Tracing.PowerShellTraceTask task, params object[] args) { return default(bool); }
    public virtual bool TraceDebug(System.Management.Automation.Tracing.PowerShellTraceEvent traceEvent, System.Management.Automation.Tracing.PowerShellTraceOperationCode operationCode, System.Management.Automation.Tracing.PowerShellTraceTask task, params object[] args) { return default(bool); }
    public virtual bool TraceError(System.Management.Automation.Tracing.PowerShellTraceEvent traceEvent, System.Management.Automation.Tracing.PowerShellTraceOperationCode operationCode, System.Management.Automation.Tracing.PowerShellTraceTask task, params object[] args) { return default(bool); }
    public virtual bool TraceInformational(System.Management.Automation.Tracing.PowerShellTraceEvent traceEvent, System.Management.Automation.Tracing.PowerShellTraceOperationCode operationCode, System.Management.Automation.Tracing.PowerShellTraceTask task, params object[] args) { return default(bool); }
    public virtual bool TraceLogAlways(System.Management.Automation.Tracing.PowerShellTraceEvent traceEvent, System.Management.Automation.Tracing.PowerShellTraceOperationCode operationCode, System.Management.Automation.Tracing.PowerShellTraceTask task, params object[] args) { return default(bool); }
    public virtual bool TraceVerbose(System.Management.Automation.Tracing.PowerShellTraceEvent traceEvent, System.Management.Automation.Tracing.PowerShellTraceOperationCode operationCode, System.Management.Automation.Tracing.PowerShellTraceTask task, params object[] args) { return default(bool); }
    public virtual bool TraceWarning(System.Management.Automation.Tracing.PowerShellTraceEvent traceEvent, System.Management.Automation.Tracing.PowerShellTraceOperationCode operationCode, System.Management.Automation.Tracing.PowerShellTraceTask task, params object[] args) { return default(bool); }
  }
  public delegate void CallbackNoParameter();
  public delegate void CallbackWithState(object state);
  public delegate void CallbackWithStateAndArgs(object state, System.Timers.ElapsedEventArgs args);
  public abstract partial class EtwActivity {
    // Constructors
    protected EtwActivity() { }
     
    // Properties
    public bool IsEnabled { get { return default(bool); } }
    protected virtual System.Guid ProviderId { get { return default(System.Guid); } }
    protected virtual System.Diagnostics.Eventing.EventDescriptor TransferEvent { get { return default(System.Diagnostics.Eventing.EventDescriptor); } }
     
    // Events
    public static event System.EventHandler<System.Management.Automation.Tracing.EtwEventArgs> EventWritten { add { } remove { } }
     
    // Methods
    public void Correlate() { }
    public System.AsyncCallback Correlate(System.AsyncCallback callback) { return default(System.AsyncCallback); }
    public System.Management.Automation.Tracing.CallbackNoParameter Correlate(System.Management.Automation.Tracing.CallbackNoParameter callback) { return default(System.Management.Automation.Tracing.CallbackNoParameter); }
    public System.Management.Automation.Tracing.CallbackWithState Correlate(System.Management.Automation.Tracing.CallbackWithState callback) { return default(System.Management.Automation.Tracing.CallbackWithState); }
    public System.Management.Automation.Tracing.CallbackWithStateAndArgs Correlate(System.Management.Automation.Tracing.CallbackWithStateAndArgs callback) { return default(System.Management.Automation.Tracing.CallbackWithStateAndArgs); }
    public void CorrelateWithActivity(System.Guid parentActivityId) { }
    public static System.Guid CreateActivityId() { return default(System.Guid); }
    public static System.Guid GetActivityId() { return default(System.Guid); }
    public bool IsProviderEnabled(byte levels, long keywords) { return default(bool); }
    public static bool SetActivityId(System.Guid activityId) { return default(bool); }
    protected void WriteEvent(System.Diagnostics.Eventing.EventDescriptor ed, params object[] payload) { }
  }
  [System.AttributeUsageAttribute((AttributeTargets)64)]
  public sealed partial class EtwEvent : System.Attribute {
    // Constructors
    public EtwEvent(long eventId) { }
     
    // Properties
    public long EventId { get { return default(long); } }
     
    // Methods
  }
  public partial class EtwEventArgs : System.EventArgs {
    // Constructors
    public EtwEventArgs(System.Diagnostics.Eventing.EventDescriptor descriptor, bool success, object[] payload) { }
     
    // Properties
    public System.Diagnostics.Eventing.EventDescriptor Descriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Diagnostics.Eventing.EventDescriptor); } }
    public object[] Payload { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(object[]); } }
    public bool Success { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } }
     
    // Methods
  }
  public partial class EtwEventCorrelator : System.Management.Automation.Tracing.IEtwEventCorrelator {
    // Constructors
    public EtwEventCorrelator(System.Diagnostics.Eventing.EventProvider transferProvider, System.Diagnostics.Eventing.EventDescriptor transferEvent) { }
     
    // Properties
    public System.Guid CurrentActivityId { get { return default(System.Guid); } set { } }
     
    // Methods
    public System.Management.Automation.Tracing.IEtwActivityReverter StartActivity() { return default(System.Management.Automation.Tracing.IEtwActivityReverter); }
    public System.Management.Automation.Tracing.IEtwActivityReverter StartActivity(System.Guid relatedActivityId) { return default(System.Management.Automation.Tracing.IEtwActivityReverter); }
  }
  public partial interface IEtwActivityReverter : System.IDisposable {
    // Methods
    void RevertCurrentActivityId();
  }
  public partial interface IEtwEventCorrelator {
    // Properties
    System.Guid CurrentActivityId { get; set; }
     
    // Methods
    System.Management.Automation.Tracing.IEtwActivityReverter StartActivity();
    System.Management.Automation.Tracing.IEtwActivityReverter StartActivity(System.Guid relatedActivityId);
  }
  public sealed partial class NullWriter : System.Management.Automation.Tracing.BaseChannelWriter {
    internal NullWriter() { }
    // Properties
    public static System.Management.Automation.Tracing.BaseChannelWriter Instance { get { return default(System.Management.Automation.Tracing.BaseChannelWriter); } }
     
    // Methods
  }
  public sealed partial class PowerShellChannelWriter : System.Management.Automation.Tracing.BaseChannelWriter {
    internal PowerShellChannelWriter() { }
    // Properties
    public override System.Management.Automation.Tracing.PowerShellTraceKeywords Keywords { get { return default(System.Management.Automation.Tracing.PowerShellTraceKeywords); } set { } }
     
    // Methods
    public override void Dispose() { }
    public override bool TraceCritical(System.Management.Automation.Tracing.PowerShellTraceEvent traceEvent, System.Management.Automation.Tracing.PowerShellTraceOperationCode operationCode, System.Management.Automation.Tracing.PowerShellTraceTask task, params object[] args) { return default(bool); }
    public override bool TraceDebug(System.Management.Automation.Tracing.PowerShellTraceEvent traceEvent, System.Management.Automation.Tracing.PowerShellTraceOperationCode operationCode, System.Management.Automation.Tracing.PowerShellTraceTask task, params object[] args) { return default(bool); }
    public override bool TraceError(System.Management.Automation.Tracing.PowerShellTraceEvent traceEvent, System.Management.Automation.Tracing.PowerShellTraceOperationCode operationCode, System.Management.Automation.Tracing.PowerShellTraceTask task, params object[] args) { return default(bool); }
    public override bool TraceInformational(System.Management.Automation.Tracing.PowerShellTraceEvent traceEvent, System.Management.Automation.Tracing.PowerShellTraceOperationCode operationCode, System.Management.Automation.Tracing.PowerShellTraceTask task, params object[] args) { return default(bool); }
    public override bool TraceLogAlways(System.Management.Automation.Tracing.PowerShellTraceEvent traceEvent, System.Management.Automation.Tracing.PowerShellTraceOperationCode operationCode, System.Management.Automation.Tracing.PowerShellTraceTask task, params object[] args) { return default(bool); }
    public override bool TraceVerbose(System.Management.Automation.Tracing.PowerShellTraceEvent traceEvent, System.Management.Automation.Tracing.PowerShellTraceOperationCode operationCode, System.Management.Automation.Tracing.PowerShellTraceTask task, params object[] args) { return default(bool); }
    public override bool TraceWarning(System.Management.Automation.Tracing.PowerShellTraceEvent traceEvent, System.Management.Automation.Tracing.PowerShellTraceOperationCode operationCode, System.Management.Automation.Tracing.PowerShellTraceTask task, params object[] args) { return default(bool); }
  }
  public enum PowerShellTraceChannel {
    // Fields
    Analytic = 17,
    Debug = 18,
    None = 0,
    Operational = 16,
  }
  public enum PowerShellTraceEvent {
    // Fields
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
  [System.FlagsAttribute]
  public enum PowerShellTraceKeywords : ulong {
    // Fields
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
  public enum PowerShellTraceLevel {
    // Fields
    Critical = 1,
    Debug = 20,
    Error = 2,
    Informational = 4,
    LogAlways = 0,
    Verbose = 5,
    Warning = 3,
  }
  public enum PowerShellTraceOperationCode {
    // Fields
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
  public sealed partial class PowerShellTraceSource : System.IDisposable {
    internal PowerShellTraceSource() { }
    // Properties
    public System.Management.Automation.Tracing.BaseChannelWriter AnalyticChannel { get { return default(System.Management.Automation.Tracing.BaseChannelWriter); } }
    public System.Management.Automation.Tracing.BaseChannelWriter DebugChannel { get { return default(System.Management.Automation.Tracing.BaseChannelWriter); } }
    public System.Management.Automation.Tracing.PowerShellTraceKeywords Keywords { get { return default(System.Management.Automation.Tracing.PowerShellTraceKeywords); } }
    public System.Management.Automation.Tracing.BaseChannelWriter OperationalChannel { get { return default(System.Management.Automation.Tracing.BaseChannelWriter); } }
    public System.Management.Automation.Tracing.PowerShellTraceTask Task { get { return default(System.Management.Automation.Tracing.PowerShellTraceTask); } set { } }
     
    // Methods
    public void Dispose() { }
    public bool TraceErrorRecord(System.Management.Automation.ErrorRecord errorRecord) { return default(bool); }
    public bool TraceException(System.Exception exception) { return default(bool); }
    public bool TraceJob(System.Management.Automation.Job job) { return default(bool); }
    public bool TracePowerShellObject(System.Management.Automation.PSObject powerShellObject) { return default(bool); }
    public bool TraceWSManConnectionInfo(System.Management.Automation.Runspaces.WSManConnectionInfo connectionInfo) { return default(bool); }
    public bool WriteMessage(string message) { return default(bool); }
    public bool WriteMessage(string message, System.Guid instanceId) { return default(bool); }
    public bool WriteMessage(string message1, string message2) { return default(bool); }
    public void WriteMessage(string className, string methodName, System.Guid workflowId, System.Management.Automation.Job job, string message, params string[] parameters) { }
    public void WriteMessage(string className, string methodName, System.Guid workflowId, string activityName, System.Guid activityId, string message, params string[] parameters) { }
    public void WriteMessage(string className, string methodName, System.Guid workflowId, string message, params string[] parameters) { }
    public void WriteScheduledJobCompleteEvent(params object[] args) { }
    public void WriteScheduledJobErrorEvent(params object[] args) { }
    public void WriteScheduledJobStartEvent(params object[] args) { }
  }
  public static partial class PowerShellTraceSourceFactory {
    // Methods
    public static System.Management.Automation.Tracing.PowerShellTraceSource GetTraceSource() { return default(System.Management.Automation.Tracing.PowerShellTraceSource); }
    public static System.Management.Automation.Tracing.PowerShellTraceSource GetTraceSource(System.Management.Automation.Tracing.PowerShellTraceTask task) { return default(System.Management.Automation.Tracing.PowerShellTraceSource); }
    public static System.Management.Automation.Tracing.PowerShellTraceSource GetTraceSource(System.Management.Automation.Tracing.PowerShellTraceTask task, System.Management.Automation.Tracing.PowerShellTraceKeywords keywords) { return default(System.Management.Automation.Tracing.PowerShellTraceSource); }
  }
  public enum PowerShellTraceTask {
    // Fields
    CreateRunspace = 1,
    ExecuteCommand = 2,
    None = 0,
    PowerShellConsoleStartup = 4,
    Serialization = 3,
  }
  public sealed partial class Tracer : System.Management.Automation.Tracing.EtwActivity {
    // Fields
    public const long KeywordAll = (long)4294967295;
    public const byte LevelCritical = (byte)1;
    public const byte LevelError = (byte)2;
    public const byte LevelInformational = (byte)4;
    public const byte LevelVerbose = (byte)5;
    public const byte LevelWarning = (byte)3;
     
    // Constructors
    public Tracer() { }
     
    // Properties
    protected override System.Guid ProviderId { get { return default(System.Guid); } }
    protected override System.Diagnostics.Eventing.EventDescriptor TransferEvent { get { return default(System.Diagnostics.Eventing.EventDescriptor); } }
     
    // Methods
    [System.Management.Automation.Tracing.EtwEvent((long)45112)]
    public void AbortingWorkflowExecution(System.Guid workflowId, string reason) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45119)]
    public void ActivityExecutionFinished(string activityName) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45079)]
    public void ActivityExecutionQueued(System.Guid workflowId, string activityName) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45080)]
    public void ActivityExecutionStarted(string activityName, string activityTypeName) { }
    [System.Management.Automation.Tracing.EtwEvent((long)46348)]
    public void BeginContainerParentJobExecution(System.Guid containerParentJobInstanceId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)46339)]
    public void BeginCreateNewJob(System.Guid trackingId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)46342)]
    public void BeginJobLogic(System.Guid workflowJobJobInstanceId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)46354)]
    public void BeginProxyChildJobEventHandler(System.Guid proxyChildJobInstanceId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)46352)]
    public void BeginProxyJobEventHandler(System.Guid proxyJobInstanceId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)46350)]
    public void BeginProxyJobExecution(System.Guid proxyJobInstanceId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)46356)]
    public void BeginRunGarbageCollection() { }
    [System.Management.Automation.Tracing.EtwEvent((long)46337)]
    public void BeginStartWorkflowApplication(System.Guid trackingId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)46344)]
    public void BeginWorkflowExecution(System.Guid workflowJobJobInstanceId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45111)]
    public void CancellingWorkflowExecution(System.Guid workflowId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)46346)]
    public void ChildWorkflowJobAddition(System.Guid workflowJobInstanceId, System.Guid containerParentJobInstanceId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)49152)]
    public void DebugMessage(System.Exception exception) { }
    [System.Management.Automation.Tracing.EtwEvent((long)49152)]
    public void DebugMessage(string message) { }
    [System.Management.Automation.Tracing.EtwEvent((long)46349)]
    public void EndContainerParentJobExecution(System.Guid containerParentJobInstanceId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)46340)]
    public void EndCreateNewJob(System.Guid trackingId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)46343)]
    public void EndJobLogic(System.Guid workflowJobJobInstanceId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45124)]
    public void EndpointDisabled(string endpointName, string disabledBy) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45125)]
    public void EndpointEnabled(string endpointName, string enabledBy) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45122)]
    public void EndpointModified(string endpointName, string modifiedBy) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45121)]
    public void EndpointRegistered(string endpointName, string endpointType, string registeredBy) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45123)]
    public void EndpointUnregistered(string endpointName, string unregisteredBy) { }
    [System.Management.Automation.Tracing.EtwEvent((long)46355)]
    public void EndProxyChildJobEventHandler(System.Guid proxyChildJobInstanceId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)46353)]
    public void EndProxyJobEventHandler(System.Guid proxyJobInstanceId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)46351)]
    public void EndProxyJobExecution(System.Guid proxyJobInstanceId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)46357)]
    public void EndRunGarbageCollection() { }
    [System.Management.Automation.Tracing.EtwEvent((long)46338)]
    public void EndStartWorkflowApplication(System.Guid trackingId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)46345)]
    public void EndWorkflowExecution(System.Guid workflowJobJobInstanceId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45083)]
    public void ErrorImportingWorkflowFromXaml(System.Guid workflowId, string errorDescription) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45116)]
    public void ForcedWorkflowShutdownError(System.Guid workflowId, string errorDescription) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45115)]
    public void ForcedWorkflowShutdownFinished(System.Guid workflowId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45114)]
    public void ForcedWorkflowShutdownStarted(System.Guid workflowId) { }
    public static string GetExceptionString(System.Exception exception) { return default(string); }
    [System.Management.Automation.Tracing.EtwEvent((long)45082)]
    public void ImportedWorkflowFromXaml(System.Guid workflowId, string xamlFile) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45081)]
    public void ImportingWorkflowFromXaml(System.Guid workflowId, string xamlFile) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45106)]
    public void JobCreationComplete(System.Guid jobId, System.Guid workflowId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45102)]
    public void JobError(int jobId, System.Guid workflowId, string errorDescription) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45107)]
    public void JobRemoved(System.Guid parentJobId, System.Guid childJobId, System.Guid workflowId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45108)]
    public void JobRemoveError(System.Guid parentJobId, System.Guid childJobId, System.Guid workflowId, string error) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45101)]
    public void JobStateChanged(int jobId, System.Guid workflowId, string newState, string oldState) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45109)]
    public void LoadingWorkflowForExecution(System.Guid workflowId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45126)]
    public void OutOfProcessRunspaceStarted(string command) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45127)]
    public void ParameterSplattingWasPerformed(string parameters, string computers) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45105)]
    public void ParentJobCreated(System.Guid jobId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)46358)]
    public void PersistenceStoreMaxSizeReached() { }
    [System.Management.Automation.Tracing.EtwEvent((long)45117)]
    public void PersistingWorkflow(System.Guid workflowId, string persistPath) { }
    [System.Management.Automation.Tracing.EtwEvent((long)46347)]
    public void ProxyJobRemoteJobAssociation(System.Guid proxyJobInstanceId, System.Guid containerParentJobInstanceId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45100)]
    public void RemoveJobStarted(System.Guid jobId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45090)]
    public void RunspaceAvailabilityChanged(string runspaceId, string availability) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45091)]
    public void RunspaceStateChanged(string runspaceId, string newState, string oldState) { }
    [System.Management.Automation.Tracing.EtwEvent((long)46341)]
    public void TrackingGuidContainerParentJobCorrelation(System.Guid trackingId, System.Guid containerParentJobInstanceId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45113)]
    public void UnloadingWorkflow(System.Guid workflowId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45089)]
    public void WorkflowActivityExecutionFailed(System.Guid workflowId, string activityName, string failureDescription) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45087)]
    public void WorkflowActivityValidated(System.Guid workflowId, string activityDisplayName, string activityType) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45088)]
    public void WorkflowActivityValidationFailed(System.Guid workflowId, string activityDisplayName, string activityType) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45096)]
    public void WorkflowCleanupPerformed(System.Guid workflowId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45098)]
    public void WorkflowDeletedFromDisk(System.Guid workflowId, string path) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45128)]
    public void WorkflowEngineStarted(string endpointName) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45095)]
    public void WorkflowExecutionAborted(System.Guid workflowId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45094)]
    public void WorkflowExecutionCancelled(System.Guid workflowId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45120)]
    public void WorkflowExecutionError(System.Guid workflowId, string errorDescription) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45110)]
    public void WorkflowExecutionFinished(System.Guid workflowId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45064)]
    public void WorkflowExecutionStarted(System.Guid workflowId, string managedNodes) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45104)]
    public void WorkflowJobCreated(System.Guid parentJobId, System.Guid childJobId, System.Guid childWorkflowId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45092)]
    public void WorkflowLoadedForExecution(System.Guid workflowId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45097)]
    public void WorkflowLoadedFromDisk(System.Guid workflowId, string path) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45129)]
    public void WorkflowManagerCheckpoint(string checkpointPath, string configProviderId, string userName, string path) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45118)]
    public void WorkflowPersisted(System.Guid workflowId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45072)]
    public void WorkflowPluginRequestedToShutdown(string endpointName) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45073)]
    public void WorkflowPluginRestarted(string endpointName) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45063)]
    public void WorkflowPluginStarted(string endpointName, string user, string hostingMode, string protocol, string configuration) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45075)]
    public void WorkflowQuotaViolated(string endpointName, string configName, string allowedValue, string valueInQuestion) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45076)]
    public void WorkflowResumed(System.Guid workflowId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45074)]
    public void WorkflowResuming(System.Guid workflowId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45078)]
    public void WorkflowRunspacePoolCreated(System.Guid workflowId, string managedNode) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45065)]
    public void WorkflowStateChanged(System.Guid workflowId, string newState, string oldState) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45093)]
    public void WorkflowUnloaded(System.Guid workflowId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45086)]
    public void WorkflowValidationError(System.Guid workflowId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45085)]
    public void WorkflowValidationFinished(System.Guid workflowId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)45084)]
    public void WorkflowValidationStarted(System.Guid workflowId) { }
    [System.Management.Automation.Tracing.EtwEvent((long)7941)]
    public void WriteTransferEvent(System.Guid currentActivityId, System.Guid parentActivityId) { }
  }
}
#endif
