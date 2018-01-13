namespace Microsoft.PowerShell {
  public sealed class DeserializingTypeConverter : System.Management.Automation.PSTypeConverter {
    public DeserializingTypeConverter() { }

    public override bool CanConvertFrom ( System.Management.Automation.PSObject sourceValue, System.Type destinationType ) { return default(bool); }
    public override bool CanConvertFrom ( object sourceValue, System.Type destinationType ) { return default(bool); }
    public override bool CanConvertTo ( object sourceValue, System.Type destinationType ) { return default(bool); }
    public override bool CanConvertTo ( System.Management.Automation.PSObject sourceValue, System.Type destinationType ) { return default(bool); }
    public override object ConvertFrom ( System.Management.Automation.PSObject sourceValue, System.Type destinationType, System.IFormatProvider formatProvider, bool ignoreCase ) { return default(object); }
    public override object ConvertFrom ( object sourceValue, System.Type destinationType, System.IFormatProvider formatProvider, bool ignoreCase ) { return default(object); }
    public override object ConvertTo ( object sourceValue, System.Type destinationType, System.IFormatProvider formatProvider, bool ignoreCase ) { return default(object); }
    public override object ConvertTo ( System.Management.Automation.PSObject sourceValue, System.Type destinationType, System.IFormatProvider formatProvider, bool ignoreCase ) { return default(object); }
    public static System.Guid GetFormatViewDefinitionInstanceId ( System.Management.Automation.PSObject instance ) { return default(System.Guid); }
    public static System.Management.Automation.PSObject GetInvocationInfo ( System.Management.Automation.PSObject instance ) { return default(System.Management.Automation.PSObject); }
    public static uint GetParameterSetMetadataFlags ( System.Management.Automation.PSObject instance ) { return default(uint); }

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

   public static class ProcessCodeMethods {
    public static object GetParentProcess ( System.Management.Automation.PSObject obj ) { return default(object); }

  }

  public sealed class PSAuthorizationManager : System.Management.Automation.AuthorizationManager {
    public PSAuthorizationManager(string shellId) : base (shellId) { }

    protected internal override bool ShouldRun ( System.Management.Automation.CommandInfo commandInfo, System.Management.Automation.CommandOrigin origin, System.Management.Automation.Host.PSHost host, out System.Exception reason ) { reason = default(System.Exception); return default(bool); }

  }

  public static class ToStringCodeMethods {
    public static string Type ( System.Management.Automation.PSObject instance ) { return default(string); }
    public static string XmlNode ( System.Management.Automation.PSObject instance ) { return default(string); }
    public static string XmlNodeList ( System.Management.Automation.PSObject instance ) { return default(string); }

  }

}
namespace Microsoft.PowerShell.Commands {
  [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(Microsoft.PowerShell.Commands.HistoryInfo)})]
    [System.Management.Automation.CmdletAttribute("Add", "History", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113279")]
   public class AddHistoryCommand : System.Management.Automation.PSCmdlet {
    public AddHistoryCommand() { }

    [System.Management.Automation.ParameterAttribute(Position = 0, ValueFromPipeline=true)]
    public System.Management.Automation.PSObject[] InputObject { get { return default(System.Management.Automation.PSObject[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Passthru { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    protected override void BeginProcessing (  ) { }
    protected override void ProcessRecord (  ) { }

  }

  [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.AliasInfo) }, ProviderCmdlet = "Set-Item")]
    [System.Management.Automation.Provider.CmdletProviderAttribute("Alias", (System.Management.Automation.Provider.ProviderCapabilities)16)]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.AliasInfo) }, ProviderCmdlet = "Rename-Item")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.AliasInfo) }, ProviderCmdlet = "Copy-Item")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.AliasInfo) }, ProviderCmdlet = "Get-ChildItem")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.AliasInfo) }, ProviderCmdlet = "New-Item")]
   public sealed class AliasProvider : Microsoft.PowerShell.Commands.SessionStateProviderBase {
    public AliasProvider() { }

    public const string ProviderName = "Alias";
    protected override System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo> InitializeDefaultDrives (  ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo>); }
    protected override object NewItemDynamicParameters ( string path, string type, object newItemValue ) { return default(object); }
    protected override object SetItemDynamicParameters ( string path, object value ) { return default(object); }

  }

  public class AliasProviderDynamicParameters {
    public AliasProviderDynamicParameters() { }

    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.ScopedItemOptions Options { get { return default(System.Management.Automation.ScopedItemOptions); } set { } }
  }

   [System.Management.Automation.CmdletAttribute("Clear", "History", DefaultParameterSetName = "IDParameter", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135199")]
   public class ClearHistoryCommand : System.Management.Automation.PSCmdlet {
    public ClearHistoryCommand() { }

    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CommandLineParameter", HelpMessage = "Specifies the name of a command in the session history")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string[] CommandLine { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 1, Mandatory=false, HelpMessage = "Clears the specified number of history entries")]
    [System.Management.Automation.ValidateRangeAttribute(1, 2147483647)]
    public int Count { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "IDParameter", HelpMessage = "Specifies the ID of a command in the session history.Clear history clears only the specified command")]
    [System.Management.Automation.ValidateRangeAttribute(1, 2147483647)]
    public System.Int32[] Id { get { return default(System.Int32[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=false, HelpMessage = "Specifies whether new entries to be cleared or the default old ones.")]
    public System.Management.Automation.SwitchParameter Newest { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    
    protected override void BeginProcessing (  ) { }
    protected override void ProcessRecord (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Connect", "PSSession", DefaultParameterSetName = "Name", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=210604", RemotingCapability = (System.Management.Automation.RemotingCapability)3)]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.Runspaces.PSSession)})]
   public class ConnectPSSessionCommand : Microsoft.PowerShell.Commands.PSRunspaceCmdlet, System.IDisposable {
    public ConnectPSSessionCommand() { }

    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriGuid")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUri")]
    public System.Management.Automation.SwitchParameter AllowRedirection { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerNameGuid", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName", ValueFromPipelineByPropertyName=true)]
    public string ApplicationName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerNameGuid")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriGuid")]
    public System.Management.Automation.Runspaces.AuthenticationMechanism Authentication { get { return default(System.Management.Automation.Runspaces.AuthenticationMechanism); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerNameGuid")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriGuid")]
    public string CertificateThumbprint { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"Cn"})]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ComputerName", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerNameGuid", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override string[] ComputerName { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerNameGuid", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUri", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriGuid", ValueFromPipelineByPropertyName=true)]
    public string ConfigurationName { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"URI","CU"})]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ConnectionUriGuid", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ConnectionUri", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Uri[] ConnectionUri { get { return default(System.Uri[]); } set { } }
    public override string[] ContainerId { get { return default(string[]); } set { } }
    [System.Management.Automation.CredentialAttribute]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerNameGuid")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriGuid")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    public System.Management.Automation.PSCredential Credential { get { return default(System.Management.Automation.PSCredential); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerNameGuid", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriGuid", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "InstanceId", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public override System.Guid[] InstanceId { get { return default(System.Guid[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Name", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override string[] Name { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerNameGuid")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ValidateRangeAttribute(1, 65535)]
    public int Port { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "Session", Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Management.Automation.Runspaces.PSSession[] Session { get { return default(System.Management.Automation.Runspaces.PSSession[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerNameGuid")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriGuid")]
    public System.Management.Automation.Remoting.PSSessionOption SessionOption { get { return default(System.Management.Automation.Remoting.PSSessionOption); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Session")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Id")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerNameGuid")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriGuid")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Name")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "InstanceId")]
    public int ThrottleLimit { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerNameGuid")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    public System.Management.Automation.SwitchParameter UseSSL { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    public override System.Guid[] VMId { get { return default(System.Guid[]); } }
    public override string[] VMName { get { return default(string[]); } }

    protected override void BeginProcessing (  ) { }
    public void Dispose (  ) { }
    protected override void EndProcessing (  ) { }
    protected override void ProcessRecord (  ) { }
    protected override void StopProcessing (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Debug", "Job", DefaultParameterSetName = "JobParameterSet", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkId=330208")]
   public sealed class DebugJobCommand : System.Management.Automation.PSCmdlet {
    public DebugJobCommand() { }

    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "JobIdParameterSet", Mandatory=true)]
    public int Id { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "JobInstanceIdParameterSet", Mandatory=true)]
    public System.Guid InstanceId { get { return default(System.Guid); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "JobParameterSet", Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    public System.Management.Automation.Job Job { get { return default(System.Management.Automation.Job); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "JobNameParameterSet", Mandatory=true)]
    public string Name { get { return default(string); } set { } }

    protected override void EndProcessing (  ) { }
    protected override void StopProcessing (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Disable", "PSRemoting", SupportsShouldProcess = true, ConfirmImpact = (System.Management.Automation.ConfirmImpact)2, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=144298")]
   public sealed class DisablePSRemotingCommand : System.Management.Automation.PSCmdlet {
    public DisablePSRemotingCommand() { }

    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    protected override void BeginProcessing (  ) { }
    protected override void EndProcessing (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Disable", "PSSessionConfiguration", SupportsShouldProcess = true, ConfirmImpact = (System.Management.Automation.ConfirmImpact)1, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=144299")]
   public sealed class DisablePSSessionConfigurationCommand : System.Management.Automation.PSCmdlet {
    public DisablePSSessionConfigurationCommand() { }

    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string[] Name { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter NoServiceRestart { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    protected override void BeginProcessing (  ) { }
    protected override void EndProcessing (  ) { }
    protected override void ProcessRecord (  ) { }

  }

  [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.Runspaces.PSSession)})]
    [System.Management.Automation.CmdletAttribute("Disconnect", "PSSession", DefaultParameterSetName = "Session", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=210605", RemotingCapability = (System.Management.Automation.RemotingCapability)3)]
   public class DisconnectPSSessionCommand : Microsoft.PowerShell.Commands.PSRunspaceCmdlet, System.IDisposable {
    public DisconnectPSSessionCommand() { }

    public override string[] ComputerName { get { return default(string[]); } set { } }
    public override string[] ContainerId { get { return default(string[]); } }

    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Id")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Name")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Session")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "InstanceId")]
    [System.Management.Automation.ValidateRangeAttribute(0, 2147483647)]
    public int IdleTimeoutSec { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Session")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Name")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Id")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "InstanceId")]
    public System.Management.Automation.Runspaces.OutputBufferingMode OutputBufferingMode { get { return default(System.Management.Automation.Runspaces.OutputBufferingMode); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "Session", Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Management.Automation.Runspaces.PSSession[] Session { get { return default(System.Management.Automation.Runspaces.PSSession[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Id")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Name")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Session")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "InstanceId")]
    public int ThrottleLimit { get { return default(int); } set { } }

    public override System.Guid[] VMId { get { return default(System.Guid[]); } }
    public override string[] VMName { get { return default(string[]); } }
    protected override void BeginProcessing (  ) { }
    public void Dispose (  ) { }
    protected override void EndProcessing (  ) { }
    protected override void ProcessRecord (  ) { }
    protected override void StopProcessing (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Enable", "PSRemoting", SupportsShouldProcess = true, ConfirmImpact = (System.Management.Automation.ConfirmImpact)2, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=144300")]
   public sealed class EnablePSRemotingCommand : System.Management.Automation.PSCmdlet {
    public EnablePSRemotingCommand() { }

    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter SkipNetworkProfileCheck { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    protected override void BeginProcessing (  ) { }
    protected override void EndProcessing (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Enable", "PSSessionConfiguration", SupportsShouldProcess = true, ConfirmImpact = (System.Management.Automation.ConfirmImpact)2, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=144301")]
   public sealed class EnablePSSessionConfigurationCommand : System.Management.Automation.PSCmdlet {
    public EnablePSSessionConfigurationCommand() { }

    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string[] Name { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter NoServiceRestart { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string SecurityDescriptorSddl { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter SkipNetworkProfileCheck { get { return default(System.Management.Automation.SwitchParameter); } set { } }

    protected override void BeginProcessing (  ) { }
    protected override void EndProcessing (  ) { }
    protected override void ProcessRecord (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Enter", "PSHostProcess", DefaultParameterSetName = "ProcessIdParameterSet", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=403736")]
   public sealed class EnterPSHostProcessCommand : System.Management.Automation.PSCmdlet {
    public EnterPSHostProcessCommand() { }

    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "ProcessParameterSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "ProcessIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "ProcessNameParameterSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "PSHostProcessInfoParameterSet")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string AppDomainName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "PSHostProcessInfoParameterSet", Mandatory=true, ValueFromPipeline=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public Microsoft.PowerShell.Commands.PSHostProcessInfo HostProcessInfo { get { return default(Microsoft.PowerShell.Commands.PSHostProcessInfo); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ProcessIdParameterSet", Mandatory=true)]
    [System.Management.Automation.ValidateRangeAttribute(0, 2147483647)]
    public int Id { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ProcessNameParameterSet", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string Name { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ProcessParameterSet", Mandatory=true, ValueFromPipeline=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public System.Diagnostics.Process Process { get { return default(System.Diagnostics.Process); } set { } }

    protected override void EndProcessing (  ) { }
    protected override void StopProcessing (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Enter", "PSSession", DefaultParameterSetName = "ComputerName", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135210", RemotingCapability = (System.Management.Automation.RemotingCapability)3)]
   public class EnterPSSessionCommand : Microsoft.PowerShell.Commands.PSRemotingBaseCmdlet {
    public EnterPSSessionCommand() { }

    [System.Management.Automation.AliasAttribute(new string[] {"Cn"})]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ComputerName", Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public new string ComputerName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMName", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Uri", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ContainerId", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMId", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName", ValueFromPipelineByPropertyName=true)]
    public string ConfigurationName { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"URI","CU"})]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "Uri", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public new System.Uri ConnectionUri { get { return default(System.Uri); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ContainerId", Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public new string ContainerId { get { return default(string); } set { } }
    [System.Management.Automation.CredentialAttribute]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "VMName", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Uri", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "VMId", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName", ValueFromPipelineByPropertyName=true)]
    public override System.Management.Automation.PSCredential Credential { get { return default(System.Management.Automation.PSCredential); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Uri")]
    public System.Management.Automation.SwitchParameter EnableNetworkAccess { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "SSHHost", Mandatory=true, ValueFromPipeline=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public new string HostName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "Id", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public int Id { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "InstanceId", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public System.Guid InstanceId { get { return default(System.Guid); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Name", ValueFromPipelineByPropertyName=true)]
    public string Name { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "Session", ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public new System.Management.Automation.Runspaces.PSSession Session { get { return default(System.Management.Automation.Runspaces.PSSession); } set { } }
    public override System.Collections.Hashtable[] SSHConnection { get { return default(System.Collections.Hashtable[]); } set { } }
    public new int ThrottleLimit { get { return default(int); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"VMGuid"})]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "VMId", Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public new System.Guid VMId { get { return default(System.Guid); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "VMName", Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public new string VMName { get { return default(string); } set { } }

    protected override void BeginProcessing (  ) { }
    protected override void EndProcessing (  ) { }
    protected override void ProcessRecord (  ) { }
    protected override void StopProcessing (  ) { }

  }

   [System.Management.Automation.Provider.CmdletProviderAttribute("Environment", (System.Management.Automation.Provider.ProviderCapabilities)16)]
   public sealed class EnvironmentProvider : Microsoft.PowerShell.Commands.SessionStateProviderBase {
    public EnvironmentProvider() { }

    public const string ProviderName = "Environment";
    protected override System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo> InitializeDefaultDrives (  ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo>); }

  }

   [System.Management.Automation.CmdletAttribute("Exit", "PSHostProcess", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=403737")]
   public sealed class ExitPSHostProcessCommand : System.Management.Automation.PSCmdlet {
    public ExitPSHostProcessCommand() { }

    protected override void ProcessRecord (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Exit", "PSSession", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135212")]
   public class ExitPSSessionCommand : Microsoft.PowerShell.Commands.PSRemotingCmdlet {
    public ExitPSSessionCommand() { }

    protected override void ProcessRecord (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Export", "ModuleMember", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=141551")]
   public sealed class ExportModuleMemberCommand : System.Management.Automation.PSCmdlet {
    public ExportModuleMemberCommand() { }

    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public string[] Alias { get { return default(string[]); } set { } }
    [System.Management.Automation.AllowEmptyCollectionAttribute]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    public string[] Cmdlet { get { return default(string[]); } set { } }
    [System.Management.Automation.AllowEmptyCollectionAttribute]
    [System.Management.Automation.ParameterAttribute(Position = 0, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    public string[] Function { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public string[] Variable { get { return default(string[]); } set { } }
    
    protected override void ProcessRecord (  ) { }

  }

  public class FileSystemClearContentDynamicParameters {
    public FileSystemClearContentDynamicParameters() { }

  }

  public class FileSystemContentDynamicParametersBase {
    public FileSystemContentDynamicParametersBase() { }

    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter AsByteStream { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ArgumentCompletionsAttribute(new string[] {"ascii","bigendianunicode","oem","unicode","utf7","utf8","utf8BOM","utf8NoBOM","utf32"})]
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Text.Encoding Encoding { get { return default(System.Text.Encoding); } set { } }
    public bool WasStreamTypeSpecified { get { return default(bool); } set { } }
  }

  public class FileSystemContentReaderDynamicParameters : Microsoft.PowerShell.Commands.FileSystemContentDynamicParametersBase {
    public FileSystemContentReaderDynamicParameters() { }

    [System.Management.Automation.ParameterAttribute]
    public string Delimiter { get { return default(string); } set { } }
    public bool DelimiterSpecified { get { return default(bool); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Raw { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Wait { get { return default(System.Management.Automation.SwitchParameter); } set { } }
  }

  public class FileSystemContentWriterDynamicParameters : Microsoft.PowerShell.Commands.FileSystemContentDynamicParametersBase {
    public FileSystemContentWriterDynamicParameters() { }

    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter NoNewline { get { return default(System.Management.Automation.SwitchParameter); } set { } }
  }

  public class FileSystemItemProviderDynamicParameters {
    public FileSystemItemProviderDynamicParameters() { }

    [System.Management.Automation.ParameterAttribute]
    public System.Nullable<System.DateTime> NewerThan { get { return default(System.Nullable<System.DateTime>); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Nullable<System.DateTime> OlderThan { get { return default(System.Nullable<System.DateTime>); } set { } }
  }

   [System.Management.Automation.Provider.CmdletProviderAttribute("FileSystem", (System.Management.Automation.Provider.ProviderCapabilities)52)]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Security.AccessControl.FileSecurity)}, ProviderCmdlet = "Set-Acl")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[2] { typeof(System.String), typeof(System.Management.Automation.PathInfo) }, ProviderCmdlet = "Resolve-Path")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.PathInfo)}, ProviderCmdlet = "Push-Location")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[2] { typeof(System.Byte), typeof(System.String) }, ProviderCmdlet = "Get-Content")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.IO.FileInfo)}, ProviderCmdlet = "Get-Item")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[2] { typeof(System.IO.FileInfo), typeof(System.IO.DirectoryInfo) }, ProviderCmdlet = "Get-ChildItem")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[2] { typeof(System.Security.AccessControl.FileSecurity), typeof(System.Security.AccessControl.DirectorySecurity) }, ProviderCmdlet = "Get-Acl")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[4] { typeof(System.Boolean), typeof(System.String), typeof(System.IO.FileInfo), typeof(System.IO.DirectoryInfo) }, ProviderCmdlet = "Get-Item")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[5] { typeof(System.Boolean), typeof(System.String), typeof(System.DateTime), typeof(System.IO.FileInfo), typeof(System.IO.DirectoryInfo) }, ProviderCmdlet = "Get-ItemProperty")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[2] { typeof(System.String), typeof(System.IO.FileInfo) }, ProviderCmdlet = "New-Item")]
   public sealed class FileSystemProvider : System.Management.Automation.Provider.NavigationCmdletProvider, System.Management.Automation.Provider.IContentCmdletProvider, System.Management.Automation.Provider.IPropertyCmdletProvider, System.Management.Automation.Provider.ISecurityDescriptorCmdletProvider, System.Management.Automation.Provider.ICmdletProviderSupportsHelp {
    public FileSystemProvider() { }

    public const string ProviderName = "FileSystem";
    public void ClearContent ( string path ) { }
    public object ClearContentDynamicParameters ( string path ) { return default(object); }
    public void ClearProperty ( string path, System.Collections.ObjectModel.Collection<string> propertiesToClear ) { }
    public object ClearPropertyDynamicParameters ( string path, System.Collections.ObjectModel.Collection<string> propertiesToClear ) { return default(object); }
    protected override bool ConvertPath ( string path, string filter, ref string updatedPath, ref string updatedFilter ) { return default(bool); }
    protected override void CopyItem ( string path, string destinationPath, bool recurse ) { }
    protected override object CopyItemDynamicParameters ( string path, string destination, bool recurse ) { return default(object); }
    protected override void GetChildItems ( string path, bool recurse, uint depth ) { }
    protected override object GetChildItemsDynamicParameters ( string path, bool recurse ) { return default(object); }
    protected override string GetChildName ( string path ) { return default(string); }
    protected override void GetChildNames ( string path, System.Management.Automation.ReturnContainers returnContainers ) { }
    protected override object GetChildNamesDynamicParameters ( string path ) { return default(object); }
    public System.Management.Automation.Provider.IContentReader GetContentReader ( string path ) { return default(System.Management.Automation.Provider.IContentReader); }
    public object GetContentReaderDynamicParameters ( string path ) { return default(object); }
    public System.Management.Automation.Provider.IContentWriter GetContentWriter ( string path ) { return default(System.Management.Automation.Provider.IContentWriter); }
    public object GetContentWriterDynamicParameters ( string path ) { return default(object); }
    public string GetHelpMaml ( string helpItemName, string path ) { return default(string); }
    protected override void GetItem ( string path ) { }
    protected override object GetItemDynamicParameters ( string path ) { return default(object); }
    protected override string GetParentPath ( string path, string root ) { return default(string); }
    public void GetProperty ( string path, System.Collections.ObjectModel.Collection<string> providerSpecificPickList ) { }
    public object GetPropertyDynamicParameters ( string path, System.Collections.ObjectModel.Collection<string> providerSpecificPickList ) { return default(object); }
    public void GetSecurityDescriptor ( string path, System.Security.AccessControl.AccessControlSections sections ) { }
    protected override bool HasChildItems ( string path ) { return default(bool); }
    protected override System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo> InitializeDefaultDrives (  ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo>); }
    protected override void InvokeDefaultAction ( string path ) { }
    protected override bool IsItemContainer ( string path ) { return default(bool); }
    protected override bool IsValidPath ( string path ) { return default(bool); }
    protected override bool ItemExists ( string path ) { return default(bool); }
    protected override object ItemExistsDynamicParameters ( string path ) { return default(object); }
    public static string Mode ( System.Management.Automation.PSObject instance ) { return default(string); }
    protected override void MoveItem ( string path, string destination ) { }
    protected override System.Management.Automation.PSDriveInfo NewDrive ( System.Management.Automation.PSDriveInfo drive ) { return default(System.Management.Automation.PSDriveInfo); }
    protected override void NewItem ( string path, string type, object value ) { }
    public System.Security.AccessControl.ObjectSecurity NewSecurityDescriptorFromPath ( string path, System.Security.AccessControl.AccessControlSections sections ) { return default(System.Security.AccessControl.ObjectSecurity); }
    public System.Security.AccessControl.ObjectSecurity NewSecurityDescriptorOfType ( string type, System.Security.AccessControl.AccessControlSections sections ) { return default(System.Security.AccessControl.ObjectSecurity); }
    protected override string NormalizeRelativePath ( string path, string basePath ) { return default(string); }
    protected override System.Management.Automation.PSDriveInfo RemoveDrive ( System.Management.Automation.PSDriveInfo drive ) { return default(System.Management.Automation.PSDriveInfo); }
    protected override void RemoveItem ( string path, bool recurse ) { }
    protected override object RemoveItemDynamicParameters ( string path, bool recurse ) { return default(object); }
    protected override void RenameItem ( string path, string newName ) { }
    public void SetProperty ( string path, System.Management.Automation.PSObject propertyToSet ) { }
    public object SetPropertyDynamicParameters ( string path, System.Management.Automation.PSObject propertyValue ) { return default(object); }
    public void SetSecurityDescriptor ( string path, System.Security.AccessControl.ObjectSecurity securityDescriptor ) { }
    protected override System.Management.Automation.ProviderInfo Start ( System.Management.Automation.ProviderInfo providerInfo ) { return default(System.Management.Automation.ProviderInfo); }

  }

  public class FileSystemProviderGetItemDynamicParameters {
    public FileSystemProviderGetItemDynamicParameters() { }

  }

  public class FileSystemProviderRemoveItemDynamicParameters {
    public FileSystemProviderRemoveItemDynamicParameters() { }

  }

   [System.Management.Automation.CmdletAttribute("ForEach", "Object", DefaultParameterSetName = "ScriptBlockSet", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113300", RemotingCapability = (System.Management.Automation.RemotingCapability)0)]
   public sealed class ForEachObjectCommand : System.Management.Automation.PSCmdlet {
    public ForEachObjectCommand() { }

    [System.Management.Automation.AliasAttribute(new string[] {"Args"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "PropertyAndMethodSet", ValueFromRemainingArguments=true)]
    public object[] ArgumentList { get { return default(object[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ScriptBlockSet")]
    public System.Management.Automation.ScriptBlock Begin { get { return default(System.Management.Automation.ScriptBlock); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ScriptBlockSet")]
    public System.Management.Automation.ScriptBlock End { get { return default(System.Management.Automation.ScriptBlock); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "PropertyAndMethodSet", ValueFromPipeline=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ScriptBlockSet", ValueFromPipeline=true)]
    public System.Management.Automation.PSObject InputObject { get { return default(System.Management.Automation.PSObject); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "PropertyAndMethodSet", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string MemberName { get { return default(string); } set { } }
    [System.Management.Automation.AllowEmptyCollectionAttribute]
    [System.Management.Automation.AllowNullAttribute]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ScriptBlockSet", Mandatory=true)]
    public System.Management.Automation.ScriptBlock[] Process { get { return default(System.Management.Automation.ScriptBlock[]); } set { } }
    [System.Management.Automation.AllowEmptyCollectionAttribute]
    [System.Management.Automation.AllowNullAttribute]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ScriptBlockSet", ValueFromRemainingArguments=true)]
    public System.Management.Automation.ScriptBlock[] RemainingScripts { get { return default(System.Management.Automation.ScriptBlock[]); } set { } }

    protected override void BeginProcessing (  ) { }
    protected override void EndProcessing (  ) { }
    protected override void ProcessRecord (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Format", "Default")]
   public class FormatDefaultCommand : Microsoft.PowerShell.Commands.Internal.Format.FrontEndCommandBase, System.IDisposable {
    public FormatDefaultCommand() { }

  }

  [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.FunctionInfo) }, ProviderCmdlet = "Copy-Item")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.FunctionInfo) }, ProviderCmdlet = "Set-Item")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.FunctionInfo) }, ProviderCmdlet = "Rename-Item")]
    [System.Management.Automation.Provider.CmdletProviderAttribute("Function", (System.Management.Automation.Provider.ProviderCapabilities)16)]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.FunctionInfo) }, ProviderCmdlet = "Get-ChildItem")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.FunctionInfo) }, ProviderCmdlet = "Get-Item")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.FunctionInfo) }, ProviderCmdlet = "New-Item")]
   public sealed class FunctionProvider : Microsoft.PowerShell.Commands.SessionStateProviderBase {
    public FunctionProvider() { }

    public const string ProviderName = "Function";
    protected override System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo> InitializeDefaultDrives (  ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo>); }
    protected override object NewItemDynamicParameters ( string path, string type, object newItemValue ) { return default(object); }
    protected override object SetItemDynamicParameters ( string path, object value ) { return default(object); }

  }

  public class FunctionProviderDynamicParameters {
    public FunctionProviderDynamicParameters() { }

    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.ScopedItemOptions Options { get { return default(System.Management.Automation.ScopedItemOptions); } set { } }
  }

   [System.Management.Automation.CmdletAttribute("Get", "Command", DefaultParameterSetName = "CmdletSet", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113309")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.AliasInfo), typeof(System.Management.Automation.ApplicationInfo), typeof(System.Management.Automation.FunctionInfo), typeof(System.Management.Automation.CmdletInfo), typeof(System.Management.Automation.ExternalScriptInfo), typeof(System.Management.Automation.FilterInfo), 
#if WORKFLOW
   typeof(System.Management.Automation.WorkflowInfo), 
#endif
   typeof(System.String), typeof(System.Management.Automation.PSObject) })]
   public sealed class GetCommandCommand : System.Management.Automation.PSCmdlet {
    public GetCommandCommand() { }

    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    public System.Management.Automation.SwitchParameter All { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"Args"})]
    [System.Management.Automation.AllowEmptyCollectionAttribute]
    [System.Management.Automation.AllowNullAttribute]
    [System.Management.Automation.ParameterAttribute(Position = 1, ValueFromRemainingArguments=true)]
    public object[] ArgumentList { get { return default(object[]); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"Type"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "AllCommandSet", ValueFromPipelineByPropertyName=true)]
    public System.Management.Automation.CommandTypes CommandType { get { return default(System.Management.Automation.CommandTypes); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    public Microsoft.PowerShell.Commands.ModuleSpecification[] FullyQualifiedModule { get { return default(Microsoft.PowerShell.Commands.ModuleSpecification[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    public System.Management.Automation.SwitchParameter ListImported { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"PSSnapin"})]
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    public string[] Module { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "AllCommandSet", ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string[] Name { get { return default(string[]); } set { } }
    [System.Management.Automation.ArgumentCompleterAttribute(typeof(Microsoft.PowerShell.Commands.NounArgumentCompleter))]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CmdletSet", ValueFromPipelineByPropertyName=true)]
    public string[] Noun { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string[] ParameterName { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Management.Automation.PSTypeName[] ParameterType { get { return default(System.Management.Automation.PSTypeName[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter ShowCommandInfo { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    public System.Management.Automation.SwitchParameter Syntax { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    public int TotalCount { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CmdletSet", ValueFromPipelineByPropertyName=true)]
    public string[] Verb { get { return default(string[]); } set { } }

    protected override void BeginProcessing (  ) { }
    protected override void EndProcessing (  ) { }
    protected override void ProcessRecord (  ) { }

  }

  public static class GetHelpCodeMethods {
    public static string GetHelpUri ( System.Management.Automation.PSObject commandInfoPSObject ) { return default(string); }

  }

   [System.Management.Automation.CmdletAttribute("Get", "Help", DefaultParameterSetName = "AllUsersView", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113316")]
   public sealed class GetHelpCommand : System.Management.Automation.PSCmdlet {
    public GetHelpCommand() { }

    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateSetAttribute(new string[] {"Alias","Cmdlet","Provider","General","FAQ","Glossary","HelpFile","ScriptCommand","Function","Filter","ExternalScript","All","DefaultHelp","Workflow","DscResource","Class","Configuration"}, IgnoreCase=true)]
    public string[] Category { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string[] Component { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "DetailedView", Mandatory=true)]
    public System.Management.Automation.SwitchParameter Detailed { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Examples", Mandatory=true)]
    public System.Management.Automation.SwitchParameter Examples { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "AllUsersView")]
    public System.Management.Automation.SwitchParameter Full { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string[] Functionality { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string Name { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Online", Mandatory=true)]
    public System.Management.Automation.SwitchParameter Online { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Parameters", Mandatory=true)]
    public string Parameter { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string Path { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string[] Role { get { return default(string[]); } set { } }

    protected override void BeginProcessing (  ) { }
    protected override void ProcessRecord (  ) { }

  }

  [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(Microsoft.PowerShell.Commands.HistoryInfo)})]
    [System.Management.Automation.CmdletAttribute("Get", "History", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113317")]
   public class GetHistoryCommand : System.Management.Automation.PSCmdlet {
    public GetHistoryCommand() { }

    [System.Management.Automation.ParameterAttribute(Position = 1)]
    [System.Management.Automation.ValidateRangeAttribute(0, 32767)]
    public int Count { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ValueFromPipeline=true)]
    [System.Management.Automation.ValidateRangeAttribute((System.Int64)1, (System.Int64)9223372036854775807)]
    public System.Int64[] Id { get { return default(System.Int64[]); } set { } }

    protected override void ProcessRecord (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Get", "Job", DefaultParameterSetName = "SessionIdParameterSet", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113328")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.Job)})]
   public class GetJobCommand : Microsoft.PowerShell.Commands.JobCmdletBase {
    public GetJobCommand() { }

    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SessionIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "InstanceIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "NameParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "StateParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CommandParameterSet")]
    public System.DateTime After { get { return default(System.DateTime); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "NameParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "InstanceIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SessionIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "StateParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CommandParameterSet")]
    public System.DateTime Before { get { return default(System.DateTime); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SessionIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "InstanceIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "NameParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "StateParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CommandParameterSet")]
    public System.Management.Automation.JobState ChildJobState { get { return default(System.Management.Automation.JobState); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "NameParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "InstanceIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SessionIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "StateParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CommandParameterSet")]
    public bool HasMoreData { get { return default(bool); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "SessionIdParameterSet", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override System.Int32[] Id { get { return default(System.Int32[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SessionIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "InstanceIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "NameParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "StateParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CommandParameterSet")]
    public System.Management.Automation.SwitchParameter IncludeChildJob { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "InstanceIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SessionIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "NameParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "StateParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CommandParameterSet")]
    public int Newest { get { return default(int); } set { } }

    protected override void ProcessRecord (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Get", "Module", DefaultParameterSetName = "Loaded", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=141552")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.PSModuleInfo)})]
   public sealed class GetModuleCommand : Microsoft.PowerShell.Commands.ModuleCmdletBase, System.IDisposable {
    public GetModuleCommand() { }

    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Loaded")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Available")]
    public System.Management.Automation.SwitchParameter All { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CimSession", Mandatory=false)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string CimNamespace { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CimSession", Mandatory=false)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public System.Uri CimResourceUri { get { return default(System.Uri); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CimSession", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public Microsoft.PowerShell.Commands.ModuleSpecification[] FullyQualifiedName { get { return default(Microsoft.PowerShell.Commands.ModuleSpecification[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "PsSession")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Available", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CimSession")]
    public System.Management.Automation.SwitchParameter ListAvailable { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "PsSession", ValueFromPipeline=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "Available", ValueFromPipeline=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "Loaded", ValueFromPipeline=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "CimSession", ValueFromPipeline=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string[] Name { get { return default(string[]); } set { } }
    [System.Management.Automation.ArgumentCompleterAttribute(typeof(Microsoft.PowerShell.Commands.PSEditionArgumentCompleter))]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "PsSession")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Available")]
    public string PSEdition { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "PsSession", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public System.Management.Automation.Runspaces.PSSession PSSession { get { return default(System.Management.Automation.Runspaces.PSSession); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "PsSession")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Available")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CimSession")]
    public System.Management.Automation.SwitchParameter Refresh { get { return default(System.Management.Automation.SwitchParameter); } set { } }

    public void Dispose (  ) { }
    protected override void ProcessRecord (  ) { }
    protected override void StopProcessing (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Get", "PSHostProcessInfo", DefaultParameterSetName = "ProcessNameParameterSet", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=517012")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(Microsoft.PowerShell.Commands.PSHostProcessInfo)})]
   public sealed class GetPSHostProcessInfoCommand : System.Management.Automation.PSCmdlet {
    public GetPSHostProcessInfoCommand() { }

    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ProcessIdParameterSet", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Int32[] Id { get { return default(System.Int32[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ProcessNameParameterSet")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string[] Name { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ProcessParameterSet", Mandatory=true, ValueFromPipeline=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Diagnostics.Process[] Process { get { return default(System.Diagnostics.Process[]); } set { } }

  }

   [System.Management.Automation.CmdletAttribute("Get", "PSSessionCapability", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=623709")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[2] { typeof(System.Management.Automation.CommandInfo), typeof(System.Management.Automation.Runspaces.InitialSessionState) })]
   public sealed class GetPSSessionCapabilityCommand : System.Management.Automation.PSCmdlet {
    public GetPSSessionCapabilityCommand() { }

    [System.Management.Automation.ParameterAttribute(Position = 0, Mandatory=true)]
    public string ConfigurationName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Full { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 1, Mandatory=true)]
    public string Username { get { return default(string); } set { } }
    
    protected override void BeginProcessing (  ) { }

  }

  [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.Runspaces.PSSession)})]
    [System.Management.Automation.CmdletAttribute("Get", "PSSession", DefaultParameterSetName = "Name", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135219", RemotingCapability = (System.Management.Automation.RemotingCapability)3)]
   public class GetPSSessionCommand : Microsoft.PowerShell.Commands.PSRunspaceCmdlet, System.IDisposable {
    public GetPSSessionCommand() { }

    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriInstanceId")]
    public System.Management.Automation.SwitchParameter AllowRedirection { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerInstanceId", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName", ValueFromPipelineByPropertyName=true)]
    public string ApplicationName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriInstanceId")]
    public System.Management.Automation.Runspaces.AuthenticationMechanism Authentication { get { return default(System.Management.Automation.Runspaces.AuthenticationMechanism); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriInstanceId")]
    public string CertificateThumbprint { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"Cn"})]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ComputerInstanceId", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ComputerName", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override string[] ComputerName { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMNameInstanceId", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerInstanceId", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUri", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriInstanceId", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ContainerId", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ContainerIdInstanceId", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMId", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMIdInstanceId", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMName", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName", ValueFromPipelineByPropertyName=true)]
    public string ConfigurationName { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"URI","CU"})]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ConnectionUriInstanceId", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ConnectionUri", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Uri[] ConnectionUri { get { return default(System.Uri[]); } set { } }
    [System.Management.Automation.CredentialAttribute]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriInstanceId")]
    public System.Management.Automation.PSCredential Credential { get { return default(System.Management.Automation.PSCredential); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMIdInstanceId", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriInstanceId", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "InstanceId", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ContainerIdInstanceId", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerInstanceId", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMNameInstanceId", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public override System.Guid[] InstanceId { get { return default(System.Guid[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Name", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ContainerId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override string[] Name { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ValidateRangeAttribute(1, 65535)]
    public int Port { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriInstanceId")]
    public System.Management.Automation.Remoting.PSSessionOption SessionOption { get { return default(System.Management.Automation.Remoting.PSSessionOption); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ContainerId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ContainerIdInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMIdInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMNameInstanceId")]
    public Microsoft.PowerShell.Commands.SessionFilterState State { get { return default(Microsoft.PowerShell.Commands.SessionFilterState); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriInstanceId")]
    public int ThrottleLimit { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerInstanceId")]
    public System.Management.Automation.SwitchParameter UseSSL { get { return default(System.Management.Automation.SwitchParameter); } set { } }

    protected override void BeginProcessing (  ) { }
    public void Dispose (  ) { }
    protected override void EndProcessing (  ) { }
    protected override void ProcessRecord (  ) { }
    protected override void StopProcessing (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Get", "PSSessionConfiguration", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=144304")]
   [System.Management.Automation.OutputTypeAttribute(new string[] { "Microsoft.PowerShell.Commands.PSSessionConfigurationCommands#PSSessionConfiguration"})]
   public sealed class GetPSSessionConfigurationCommand : System.Management.Automation.PSCmdlet {
    public GetPSSessionConfigurationCommand() { }

    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, Mandatory=false)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string[] Name { get { return default(string[]); } set { } }
    protected override void BeginProcessing (  ) { }
    protected override void ProcessRecord (  ) { }

  }

    [System.SerializableAttribute]
   public class HelpCategoryInvalidException : System.ArgumentException, System.Management.Automation.IContainsErrorRecord {
    public HelpCategoryInvalidException(string helpCategory) { }
    public HelpCategoryInvalidException() { }
    public HelpCategoryInvalidException(string helpCategory, System.Exception innerException) { }
    protected HelpCategoryInvalidException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public string HelpCategory { get { return default(string); } }
    public override string Message { get { return default(string); } }
    public override void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

  }

    [System.SerializableAttribute]
   public class HelpNotFoundException : System.SystemException, System.Management.Automation.IContainsErrorRecord {
    public HelpNotFoundException(string helpTopic) { }
    public HelpNotFoundException() { }
    public HelpNotFoundException(string helpTopic, System.Exception innerException) { }
    protected HelpNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public string HelpTopic { get { return default(string); } }
    public override string Message { get { return default(string); } }
    public override void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

  }

  public class HistoryInfo {
    internal HistoryInfo() { }
    public string CommandLine { get { return default(string); } }
    public System.DateTime EndExecutionTime { get { return default(System.DateTime); } }
    public System.Management.Automation.Runspaces.PipelineState ExecutionStatus { get { return default(System.Management.Automation.Runspaces.PipelineState); } }
    public System.Int64 Id { get { return default(System.Int64); } }
    public System.DateTime StartExecutionTime { get { return default(System.DateTime); } }
    public Microsoft.PowerShell.Commands.HistoryInfo Clone (  ) { return default(Microsoft.PowerShell.Commands.HistoryInfo); }
    public override string ToString (  ) { return default(string); }

  }

  [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.PSModuleInfo)})]
    [System.Management.Automation.CmdletAttribute("Import", "Module", DefaultParameterSetName = "Name", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=141553")]
   public sealed class ImportModuleCommand : Microsoft.PowerShell.Commands.ModuleCmdletBase, System.IDisposable  {
    public ImportModuleCommand() { }

    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullAttribute]
    public string[] Alias { get { return default(string[]); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"Args"})]
    [System.Management.Automation.ParameterAttribute]
    public object[] ArgumentList { get { return default(object[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter AsCustomObject { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "Assembly", Mandatory=true, ValueFromPipeline=true)]
    public System.Reflection.Assembly[] Assembly { get { return default(System.Reflection.Assembly[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CimSession", Mandatory=false)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string CimNamespace { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CimSession", Mandatory=false)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public System.Uri CimResourceUri { get { return default(System.Uri); } set { } }
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullAttribute]
    public string[] Cmdlet { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter DisableNameChecking { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "FullyQualifiedName", Mandatory=true, ValueFromPipeline=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "FullyQualifiedNameAndPSSession", Mandatory=true, ValueFromPipeline=true)]
    public Microsoft.PowerShell.Commands.ModuleSpecification[] FullyQualifiedName { get { return default(Microsoft.PowerShell.Commands.ModuleSpecification[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullAttribute]
    public string[] Function { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Global { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Name")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "PSSession")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CimSession")]
    public string MaximumVersion { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"Version"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Name")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "PSSession")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CimSession")]
    public System.Version MinimumVersion { get { return default(System.Version); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ModuleInfo", Mandatory=true, ValueFromPipeline=true)]
    public System.Management.Automation.PSModuleInfo[] ModuleInfo { get { return default(System.Management.Automation.PSModuleInfo[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "PSSession", Mandatory=true, ValueFromPipeline=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "Name", Mandatory=true, ValueFromPipeline=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "CimSession", Mandatory=true, ValueFromPipeline=true)]
    public string[] Name { get { return default(string[]); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"NoOverwrite"})]
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter NoClobber { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter PassThru { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullAttribute]
    public string Prefix { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FullyQualifiedNameAndPSSession", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "PSSession", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public System.Management.Automation.Runspaces.PSSession PSSession { get { return default(System.Management.Automation.Runspaces.PSSession); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "PSSession")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Name")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CimSession")]
    public System.Version RequiredVersion { get { return default(System.Version); } set { } }
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateSetAttribute(new string[] {"Local","Global"})]
    public string Scope { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullAttribute]
    public string[] Variable { get { return default(string[]); } set { } }


    protected override void BeginProcessing (  ) { }
    public void Dispose (  ) { }
    protected override void ProcessRecord (  ) { }
    protected override void StopProcessing (  ) { }

  }

  public static class InternalSymbolicLinkLinkCodeMethods {
    public static string GetLinkType ( System.Management.Automation.PSObject instance ) { return default(string); }
    public static System.Collections.Generic.IEnumerable<System.String> GetTarget ( System.Management.Automation.PSObject instance ) { return default(System.Collections.Generic.IEnumerable<System.String>); }

  }

   [System.Management.Automation.CmdletAttribute("Invoke", "Command", DefaultParameterSetName = "InProcess", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135225", RemotingCapability = (System.Management.Automation.RemotingCapability)3)]
   public class InvokeCommandCommand : Microsoft.PowerShell.Commands.PSExecutionCmdlet, System.IDisposable {
    public InvokeCommandCommand() { }

    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Uri")]
    public override System.Management.Automation.SwitchParameter AllowRedirection { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathComputerName", ValueFromPipelineByPropertyName=true)]
    public override string ApplicationName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Session")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Uri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathRunspace")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ContainerId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathVMId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathVMName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathContainerId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SSHHost")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SSHHostHashParam")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathSSHHost")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathSSHHostHash")]
    public System.Management.Automation.SwitchParameter AsJob { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Uri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathUri")]
    public override System.Management.Automation.Runspaces.AuthenticationMechanism Authentication { get { return default(System.Management.Automation.Runspaces.AuthenticationMechanism); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"Cn"})]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "FilePathComputerName")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override string[] ComputerName { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ContainerId", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Uri", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathComputerName", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathUri", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMId", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMName", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathContainerId", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathVMId", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathVMName", ValueFromPipelineByPropertyName=true)]
    public override string ConfigurationName { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"URI","CU"})]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "Uri")]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "FilePathUri")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override System.Uri[] ConnectionUri { get { return default(System.Uri[]); } set { } }
    [System.Management.Automation.CredentialAttribute]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMId", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Uri", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathComputerName", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathUri", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMName", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathVMId", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathVMName", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    public override System.Management.Automation.PSCredential Credential { get { return default(System.Management.Automation.PSCredential); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Uri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathUri")]
    public override System.Management.Automation.SwitchParameter EnableNetworkAccess { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"PSPath"})]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "FilePathComputerName", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "FilePathRunspace", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "FilePathUri", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "FilePathVMId", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "FilePathVMName", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "FilePathContainerId", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathSSHHost", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathSSHHostHash", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public override string FilePath { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"HCN"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SSHHostHashParam")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SSHHost")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathContainerId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathVMName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathVMId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathSSHHostHash")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ContainerId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathRunspace")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Uri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Session")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathSSHHost")]
    public System.Management.Automation.SwitchParameter HideComputerName { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SSHHost", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathSSHHost", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override string[] HostName { get { return default(string[]); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"Disconnected"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Uri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathUri")]
    public System.Management.Automation.SwitchParameter InDisconnectedSession { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Session")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Uri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathRunspace")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ContainerId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathContainerId")]
    public string JobName { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"IdentityFilePath"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathSSHHost")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SSHHost")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override string KeyFilePath { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "InProcess")]
    public System.Management.Automation.SwitchParameter NoNewScope { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SSHHost")]
    [System.Management.Automation.ValidateRangeAttribute(1, 65535)]
    public override int Port { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SSHHost")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Session")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Uri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathRunspace")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ContainerId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathVMId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathVMName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathContainerId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SSHHostHashParam")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathSSHHost")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathSSHHostHash")]
    public System.Management.Automation.SwitchParameter RemoteDebug { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathContainerId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ContainerId")]
    public override System.Management.Automation.SwitchParameter RunAsAdministrator { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"Command"})]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "Uri", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "Session", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "ComputerName", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "InProcess", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "VMId", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "VMName", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "ContainerId", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SSHHost", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SSHHostHashParam", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public override System.Management.Automation.ScriptBlock ScriptBlock { get { return default(System.Management.Automation.ScriptBlock); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "Session")]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "FilePathRunspace")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override System.Management.Automation.Runspaces.PSSession[] Session { get { return default(System.Management.Automation.Runspaces.PSSession[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathComputerName")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string[] SessionName { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Uri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathUri")]
    public override System.Management.Automation.Remoting.PSSessionOption SessionOption { get { return default(System.Management.Automation.Remoting.PSSessionOption); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SSHHostHashParam", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathSSHHostHash", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override System.Collections.Hashtable[] SSHConnection { get { return default(System.Collections.Hashtable[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathSSHHost")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SSHHost")]
    [System.Management.Automation.ValidateSetAttribute(new string[] {"true"})]
    public override System.Management.Automation.SwitchParameter SSHTransport { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Session")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Uri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathRunspace")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathUri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ContainerId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathVMId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathVMName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathContainerId")]
    public override int ThrottleLimit { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathSSHHost")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SSHHost")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override string UserName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    public override System.Management.Automation.SwitchParameter UseSSL { get { return default(System.Management.Automation.SwitchParameter); } set { } }

    protected override void BeginProcessing (  ) { }
    public void Dispose (  ) { }
    protected override void EndProcessing (  ) { }
    protected override void ProcessRecord (  ) { }
    protected override void StopProcessing (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Invoke", "History", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113344")]
   public class InvokeHistoryCommand : System.Management.Automation.PSCmdlet {
    public InvokeHistoryCommand() { }

    [System.Management.Automation.ParameterAttribute(Position = 0, ValueFromPipelineByPropertyName=true)]
    public string Id { get { return default(string); } set { } }
    protected override void EndProcessing (  ) { }

  }

  public class JobCmdletBase : Microsoft.PowerShell.Commands.PSRemotingCmdlet {
    public JobCmdletBase() { }

    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CommandParameterSet", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public virtual string[] Command { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "FilterParameterSet", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public virtual System.Collections.Hashtable Filter { get { return default(System.Collections.Hashtable); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "SessionIdParameterSet", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public virtual System.Int32[] Id { get { return default(System.Int32[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "InstanceIdParameterSet", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Guid[] InstanceId { get { return default(System.Guid[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "NameParameterSet", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string[] Name { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "StateParameterSet", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    public virtual System.Management.Automation.JobState State { get { return default(System.Management.Automation.JobState); } set { } }

    protected override void BeginProcessing (  ) { }

  }

  public class ModuleCmdletBase : System.Management.Automation.PSCmdlet {
    public ModuleCmdletBase() { }

    protected internal void ImportModuleMembers ( System.Management.Automation.PSModuleInfo sourceModule, string prefix ) { }
    protected internal void ImportModuleMembers ( System.Management.Automation.PSModuleInfo sourceModule, string prefix, Microsoft.PowerShell.Commands.ModuleCmdletBase.ImportModuleOptions options ) { }

    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    protected internal partial struct ImportModuleOptions { }
  }

  public class ModuleSpecification {
    public ModuleSpecification() { }
    public ModuleSpecification(string moduleName) { }
    public ModuleSpecification(System.Collections.Hashtable moduleSpecification) { }

    public System.Nullable<System.Guid> Guid { get { return default(System.Nullable<System.Guid>); } set { } }
    public string MaximumVersion { get { return default(string); } set { } }
    public string Name { get { return default(string); } set { } }
    public System.Version RequiredVersion { get { return default(System.Version); } set { } }
    public System.Version Version { get { return default(System.Version); } set { } }
    public override string ToString (  ) { return default(string); }
    public static bool TryParse ( string input, out Microsoft.PowerShell.Commands.ModuleSpecification result ) { result = default(Microsoft.PowerShell.Commands.ModuleSpecification); return default(bool); }

  }

   [System.Management.Automation.CmdletAttribute("New", "Module", DefaultParameterSetName = "ScriptBlock", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=141554")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.PSModuleInfo)})]
   public sealed class NewModuleCommand : Microsoft.PowerShell.Commands.ModuleCmdletBase {
    public NewModuleCommand() { }

    [System.Management.Automation.AliasAttribute(new string[] {"Args"})]
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
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "Name", Mandatory=true, ValueFromPipeline=true)]
    public string Name { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter ReturnResult { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "Name", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ScriptBlock", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public System.Management.Automation.ScriptBlock ScriptBlock { get { return default(System.Management.Automation.ScriptBlock); } set { } }

    protected override void EndProcessing (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("New", "ModuleManifest", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=141555")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.String)})]
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
    [System.Management.Automation.AllowEmptyCollectionAttribute]
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateSetAttribute(new string[] {"Desktop","Core"})]
    public string[] CompatiblePSEditions { get { return default(string[]); } set { } }
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
    public string[] DscResourcesToExport { get { return default(string[]); } set { } }
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
    [System.Management.Automation.ParameterAttribute(Mandatory=false)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Uri IconUri { get { return default(System.Uri); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=false)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Uri LicenseUri { get { return default(System.Uri); } set { } }
    [System.Management.Automation.AllowEmptyCollectionAttribute]
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
    [System.Management.Automation.ParameterAttribute(Position = 0, Mandatory=true)]
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
    [System.Management.Automation.ParameterAttribute(Mandatory=false)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Uri ProjectUri { get { return default(System.Uri); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=false)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string ReleaseNotes { get { return default(string); } set { } }
    [System.Management.Automation.AllowEmptyCollectionAttribute]
    [System.Management.Automation.ParameterAttribute]
    public string[] RequiredAssemblies { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public object[] RequiredModules { get { return default(object[]); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"ModuleToProcess"})]
    [System.Management.Automation.AllowEmptyStringAttribute]
    [System.Management.Automation.ParameterAttribute]
    public string RootModule { get { return default(string); } set { } }
    [System.Management.Automation.AllowEmptyCollectionAttribute]
    [System.Management.Automation.ParameterAttribute]
    public string[] ScriptsToProcess { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Mandatory=false)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string[] Tags { get { return default(string[]); } set { } }
    [System.Management.Automation.AllowEmptyCollectionAttribute]
    [System.Management.Automation.ParameterAttribute]
    public string[] TypesToProcess { get { return default(string[]); } set { } }
    [System.Management.Automation.AllowEmptyCollectionAttribute]
    [System.Management.Automation.ParameterAttribute]
    public string[] VariablesToExport { get { return default(string[]); } set { } }

    protected override void EndProcessing (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("New", "PSRoleCapabilityFile", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=623708")]
   public class NewPSRoleCapabilityFileCommand : System.Management.Automation.PSCmdlet {
    public NewPSRoleCapabilityFileCommand() { }

    [System.Management.Automation.ParameterAttribute]
    public System.Collections.IDictionary[] AliasDefinitions { get { return default(System.Collections.IDictionary[]); } set { } }
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
    public System.Collections.IDictionary EnvironmentVariables { get { return default(System.Collections.IDictionary); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string[] FormatsToProcess { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Collections.IDictionary[] FunctionDefinitions { get { return default(System.Collections.IDictionary[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Guid Guid { get { return default(System.Guid); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public object[] ModulesToImport { get { return default(object[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, Mandatory=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string Path { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string[] ScriptsToProcess { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string[] TypesToProcess { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public object VariableDefinitions { get { return default(object); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string[] VisibleAliases { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public object[] VisibleCmdlets { get { return default(object[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string[] VisibleExternalCommands { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public object[] VisibleFunctions { get { return default(object[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string[] VisibleProviders { get { return default(string[]); } set { } }

    protected override void ProcessRecord (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("New", "PSSession", DefaultParameterSetName = "ComputerName", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135237", RemotingCapability = (System.Management.Automation.RemotingCapability)3)]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.Runspaces.PSSession)})]
   public class NewPSSessionCommand : Microsoft.PowerShell.Commands.PSRemotingBaseCmdlet, System.IDisposable {
    public NewPSSessionCommand() { }

    [System.Management.Automation.AliasAttribute(new string[] {"Cn"})]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ComputerName", ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override string[] ComputerName { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Uri", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ContainerId", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMId", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMName", ValueFromPipelineByPropertyName=true)]
    public string ConfigurationName { get { return default(string); } set { } }
    [System.Management.Automation.CredentialAttribute]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Uri", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMId", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMName", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    public override System.Management.Automation.PSCredential Credential { get { return default(System.Management.Automation.PSCredential); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Session")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Uri")]
    public System.Management.Automation.SwitchParameter EnableNetworkAccess { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string[] Name { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "Session", ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override System.Management.Automation.Runspaces.PSSession[] Session { get { return default(System.Management.Automation.Runspaces.PSSession[]); } set { } }

    protected override void BeginProcessing (  ) { }
    public void Dispose (  ) { }
    protected override void EndProcessing (  ) { }
    protected override void ProcessRecord (  ) { }
    protected override void StopProcessing (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("New", "PSSessionConfigurationFile", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=217036")]
   public class NewPSSessionConfigurationFileCommand : System.Management.Automation.PSCmdlet {
    public NewPSSessionConfigurationFileCommand() { }

    [System.Management.Automation.ParameterAttribute]
    public System.Collections.IDictionary[] AliasDefinitions { get { return default(System.Collections.IDictionary[]); } set { } }
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
    public System.Collections.IDictionary EnvironmentVariables { get { return default(System.Collections.IDictionary); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public Microsoft.PowerShell.ExecutionPolicy ExecutionPolicy { get { return default(Microsoft.PowerShell.ExecutionPolicy); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string[] FormatsToProcess { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Full { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Collections.IDictionary[] FunctionDefinitions { get { return default(System.Collections.IDictionary[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string GroupManagedServiceAccount { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Guid Guid { get { return default(System.Guid); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.PSLanguageMode LanguageMode { get { return default(System.Management.Automation.PSLanguageMode); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public object[] ModulesToImport { get { return default(object[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter MountUserDrive { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, Mandatory=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string Path { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Version PowerShellVersion { get { return default(System.Version); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Collections.IDictionary RequiredGroups { get { return default(System.Collections.IDictionary); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Collections.IDictionary RoleDefinitions { get { return default(System.Collections.IDictionary); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter RunAsVirtualAccount { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string[] RunAsVirtualAccountGroups { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullAttribute]
    public System.Version SchemaVersion { get { return default(System.Version); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string[] ScriptsToProcess { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.Remoting.SessionType SessionType { get { return default(System.Management.Automation.Remoting.SessionType); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string TranscriptDirectory { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string[] TypesToProcess { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public long UserDriveMaximumSize { get { return default(long); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public object VariableDefinitions { get { return default(object); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string[] VisibleAliases { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public object[] VisibleCmdlets { get { return default(object[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string[] VisibleExternalCommands { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public object[] VisibleFunctions { get { return default(object[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string[] VisibleProviders { get { return default(string[]); } set { } }

    protected override void ProcessRecord (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("New", "PSSessionOption", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=144305", RemotingCapability = (System.Management.Automation.RemotingCapability)0)]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.Remoting.PSSessionOption)})]
   public sealed class NewPSSessionOptionCommand : System.Management.Automation.PSCmdlet {
    public NewPSSessionOptionCommand() { }

    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullAttribute]
    public System.Management.Automation.PSPrimitiveDictionary ApplicationArguments { get { return default(System.Management.Automation.PSPrimitiveDictionary); } set { } }
    [System.Management.Automation.AliasAttribute(new string[1] { "CancelTimeoutMSec" })]
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateRangeAttribute(0, 2147483647)]
    public int CancelTimeout { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullAttribute]
    public System.Globalization.CultureInfo Culture { get { return default(System.Globalization.CultureInfo); } set { } }
    [System.Management.Automation.AliasAttribute(new string[1] { "IdleTimeoutMSec" })]
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateRangeAttribute(-1, 2147483647)]
    public int IdleTimeout { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter IncludePortInSPN { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateRangeAttribute(0, 2147483647)]
    public int MaxConnectionRetryCount { get { return default(int); } set { } }
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
    [System.Management.Automation.AliasAttribute(new string[1] { "OpenTimeoutMSec" })]
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateRangeAttribute(0, 2147483647)]
    public int OpenTimeout { get { return default(int); } set { } }
    [System.Management.Automation.AliasAttribute(new string[1] { "OperationTimeoutMSec" })]
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
    protected override void BeginProcessing (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("New", "PSTransportOption", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=210608", RemotingCapability = (System.Management.Automation.RemotingCapability)0)]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(Microsoft.PowerShell.Commands.WSManConfigurationOption)})]
   public sealed class NewPSTransportOptionCommand : System.Management.Automation.PSCmdlet {
    public NewPSTransportOptionCommand() { }

    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateRangeAttribute((System.Int32)60, (System.Int32)2147483)]
    public System.Nullable<int> IdleTimeoutSec { get { return default(System.Nullable<int>); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateRangeAttribute((System.Int32)1, (System.Int32)2147483647)]
    public System.Nullable<int> MaxConcurrentCommandsPerSession { get { return default(System.Nullable<int>); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateRangeAttribute((System.Int32)1, (System.Int32)100)]
    public System.Nullable<int> MaxConcurrentUsers { get { return default(System.Nullable<int>); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateRangeAttribute((System.Int32)60, (System.Int32)2147483)]
    public System.Nullable<int> MaxIdleTimeoutSec { get { return default(System.Nullable<int>); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateRangeAttribute((System.Int32)5, (System.Int32)2147483647)]
    public System.Nullable<int> MaxMemoryPerSessionMB { get { return default(System.Nullable<int>); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateRangeAttribute((System.Int32)1, (System.Int32)2147483647)]
    public System.Nullable<int> MaxProcessesPerSession { get { return default(System.Nullable<int>); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateRangeAttribute((System.Int32)1, (System.Int32)2147483647)]
    public System.Nullable<int> MaxSessions { get { return default(System.Nullable<int>); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateRangeAttribute((System.Int32)1, (System.Int32)2147483647)]
    public System.Nullable<int> MaxSessionsPerUser { get { return default(System.Nullable<int>); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    public System.Nullable<System.Management.Automation.Runspaces.OutputBufferingMode> OutputBufferingMode { get { return default(System.Nullable<System.Management.Automation.Runspaces.OutputBufferingMode>); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateRangeAttribute((System.Int32)0, (System.Int32)1209600)]
    public System.Nullable<int> ProcessIdleTimeoutSec { get { return default(System.Nullable<int>); } set { } }
    protected override void ProcessRecord (  ) { }

  }

  public class NounArgumentCompleter {
    public NounArgumentCompleter() { }

    public System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult> CompleteArgument ( string commandName, string parameterName, string wordToComplete, System.Management.Automation.Language.CommandAst commandAst, System.Collections.IDictionary fakeBoundParameters ) { return default(System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult>); }

  }

  public abstract class ObjectEventRegistrationBase : System.Management.Automation.PSCmdlet {
    protected ObjectEventRegistrationBase() { }

    [System.Management.Automation.ParameterAttribute(Position = 101)]
    public System.Management.Automation.ScriptBlock Action { get { return default(System.Management.Automation.ScriptBlock); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Forward { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public int MaxTriggerCount { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.PSObject MessageData { get { return default(System.Management.Automation.PSObject); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 100)]
    public string SourceIdentifier { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter SupportEvent { get { return default(System.Management.Automation.SwitchParameter); } set { } }

    protected override void BeginProcessing (  ) { }
    protected override void EndProcessing (  ) { }
    protected virtual object GetSourceObject (  ) { return default(object); }
    protected virtual string GetSourceObjectEventName (  ) { return default(string); }

  }

  public enum OpenMode {
    Add = 0,
    New = 1,
    Overwrite = 2,
  }

   [System.Management.Automation.CmdletAttribute("Out", "Default", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113362", RemotingCapability = (System.Management.Automation.RemotingCapability)0)]
   public class OutDefaultCommand : Microsoft.PowerShell.Commands.Internal.Format.FrontEndCommandBase, System.IDisposable {
    public OutDefaultCommand() { }

    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Transcript { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    protected override void BeginProcessing (  ) { }
    protected override void EndProcessing (  ) { }
    protected override void InternalDispose (  ) { }
    protected override void ProcessRecord (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Out", "Host", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113365", RemotingCapability = (System.Management.Automation.RemotingCapability)0)]
   public class OutHostCommand : Microsoft.PowerShell.Commands.Internal.Format.FrontEndCommandBase, System.IDisposable {
    public OutHostCommand() { }

    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Paging { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    protected override void BeginProcessing (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Out", "LineOutput")]
   public class OutLineOutputCommand : Microsoft.PowerShell.Commands.Internal.Format.FrontEndCommandBase, System.IDisposable {
    public OutLineOutputCommand() { }

    [System.Management.Automation.ParameterAttribute]
    public object LineOutput { get { return default(object); } set { } }
    protected override void BeginProcessing (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Out", "Null", SupportsShouldProcess = false, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113366", RemotingCapability = (System.Management.Automation.RemotingCapability)0)]
   public class OutNullCommand : System.Management.Automation.PSCmdlet {
    public OutNullCommand() { }

    [System.Management.Automation.ParameterAttribute(ValueFromPipeline=true)]
    public System.Management.Automation.PSObject InputObject { get { return default(System.Management.Automation.PSObject); } set { } }
    protected override void ProcessRecord (  ) { }

  }

  public enum OutTarget {
    Default = 0,
    Host = 1,
    Job = 2,
  }

  public class PSEditionArgumentCompleter {
    public PSEditionArgumentCompleter() { }

    public System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult> CompleteArgument ( string commandName, string parameterName, string wordToComplete, System.Management.Automation.Language.CommandAst commandAst, System.Collections.IDictionary fakeBoundParameters ) { return default(System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult>); }

  }

  public abstract class PSExecutionCmdlet : Microsoft.PowerShell.Commands.PSRemotingBaseCmdlet {
    protected PSExecutionCmdlet() { }

    [System.Management.Automation.AliasAttribute(new string[] {"Args"})]
    [System.Management.Automation.ParameterAttribute]
    public virtual object[] ArgumentList { get { return default(object[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathVMName", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Uri", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathComputerName", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathUri", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ContainerId", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMId", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMName", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathContainerId", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathVMId", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName", ValueFromPipelineByPropertyName=true)]
    public virtual string ConfigurationName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ContainerId", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathContainerId", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override string[] ContainerId { get { return default(string[]); } set { } }
    public virtual System.Management.Automation.SwitchParameter EnableNetworkAccess { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "FilePathComputerName", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "FilePathRunspace", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "FilePathUri", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public virtual string FilePath { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipeline=true)]
    public virtual System.Management.Automation.PSObject InputObject { get { return default(System.Management.Automation.PSObject); } set { } }
    public virtual System.Management.Automation.ScriptBlock ScriptBlock { get { return default(System.Management.Automation.ScriptBlock); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"VMGuid"})]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "VMId", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "FilePathVMId", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override System.Guid[] VMId { get { return default(System.Guid[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMName", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathVMName", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override string[] VMName { get { return default(string[]); } set { } }

    protected override void BeginProcessing (  ) { }
    protected virtual void CreateHelpersForSpecifiedComputerNames (  ) { }
    protected virtual void CreateHelpersForSpecifiedContainerSession (  ) { }
    protected virtual void CreateHelpersForSpecifiedVMSession (  ) { }

  }

  public sealed class PSHostProcessInfo {
    public string AppDomainName { get { return default(string); } set { } }
    public int ProcessId { get { return default(int); } set { } }
    public string ProcessName { get { return default(string); } set { } }
  }

  public abstract class PSRemotingBaseCmdlet : Microsoft.PowerShell.Commands.PSRemotingCmdlet {
    protected PSRemotingBaseCmdlet() { }

    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Uri")]
    public virtual System.Management.Automation.SwitchParameter AllowRedirection { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName", ValueFromPipelineByPropertyName=true)]
    public virtual string ApplicationName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Uri")]
    public virtual System.Management.Automation.Runspaces.AuthenticationMechanism Authentication { get { return default(System.Management.Automation.Runspaces.AuthenticationMechanism); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Uri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    public virtual string CertificateThumbprint { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"Cn"})]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ComputerName", ValueFromPipelineByPropertyName=true)]
    public virtual string[] ComputerName { get { return default(string[]); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"URI","CU"})]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "Uri", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public virtual System.Uri[] ConnectionUri { get { return default(System.Uri[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ContainerId", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public virtual string[] ContainerId { get { return default(string[]); } set { } }
    [System.Management.Automation.CredentialAttribute]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Uri", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMId", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMName", ValueFromPipelineByPropertyName=true)]
    public virtual System.Management.Automation.PSCredential Credential { get { return default(System.Management.Automation.PSCredential); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "SSHHost", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public virtual string[] HostName { get { return default(string[]); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"IdentityFilePath"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SSHHost")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public virtual string KeyFilePath { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SSHHost")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ValidateRangeAttribute(1, 65535)]
    public virtual int Port { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ContainerId")]
    public virtual System.Management.Automation.SwitchParameter RunAsAdministrator { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "Session", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public virtual System.Management.Automation.Runspaces.PSSession[] Session { get { return default(System.Management.Automation.Runspaces.PSSession[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Uri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ValidateNotNullAttribute]
    public virtual System.Management.Automation.Remoting.PSSessionOption SessionOption { get { return default(System.Management.Automation.Remoting.PSSessionOption); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SSHHostHashParam", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public virtual System.Collections.Hashtable[] SSHConnection { get { return default(System.Collections.Hashtable[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SSHHost")]
    [System.Management.Automation.ValidateSetAttribute(new string[] {"true"})]
    public virtual System.Management.Automation.SwitchParameter SSHTransport { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Session")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Uri")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ContainerId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMName")]
    public virtual int ThrottleLimit { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SSHHost")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public virtual string UserName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    public virtual System.Management.Automation.SwitchParameter UseSSL { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"VMGuid"})]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "VMId", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public virtual System.Guid[] VMId { get { return default(System.Guid[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMName", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public virtual string[] VMName { get { return default(string[]); } set { } }

    protected override void BeginProcessing (  ) { }

  }

  public abstract class PSRemotingCmdlet : System.Management.Automation.PSCmdlet {
    protected PSRemotingCmdlet() { }

    protected override void BeginProcessing (  ) { }

  }

  public abstract class PSRunspaceCmdlet : Microsoft.PowerShell.Commands.PSRemotingCmdlet {
    protected PSRunspaceCmdlet() { }

    [System.Management.Automation.AliasAttribute(new string[] {"Cn"})]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ComputerName", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public virtual string[] ComputerName { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ContainerIdInstanceId", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ContainerId", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public virtual string[] ContainerId { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "Id", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public System.Int32[] Id { get { return default(System.Int32[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "InstanceId", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public virtual System.Guid[] InstanceId { get { return default(System.Guid[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Name", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public virtual string[] Name { get { return default(string[]); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"VMGuid"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMId", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMIdInstanceId", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public virtual System.Guid[] VMId { get { return default(System.Guid[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMName", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMNameInstanceId", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public virtual string[] VMName { get { return default(string[]); } set { } }
  }

  public class PSSessionConfigurationCommandBase : System.Management.Automation.PSCmdlet {
    internal PSSessionConfigurationCommandBase() { }

    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.Runspaces.PSSessionConfigurationAccessMode AccessMode { get { return default(System.Management.Automation.Runspaces.PSSessionConfigurationAccessMode); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "NameParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "AssemblyNameParameterSet")]
    public string ApplicationBase { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "AssemblyNameParameterSet", Mandatory=true)]
    public string AssemblyName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 2, ParameterSetName = "AssemblyNameParameterSet", Mandatory=true)]
    public string ConfigurationTypeName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AllowNullAttribute]
    [System.Management.Automation.ParameterAttribute]
    public System.Nullable<double> MaximumReceivedDataSizePerCommandMB { get { return default(System.Nullable<double>); } set { } }
    [System.Management.Automation.AllowNullAttribute]
    [System.Management.Automation.ParameterAttribute]
    public System.Nullable<double> MaximumReceivedObjectSizeMB { get { return default(System.Nullable<double>); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "NameParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "AssemblyNameParameterSet")]
    public object[] ModulesToImport { get { return default(object[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "AssemblyNameParameterSet", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "SessionConfigurationFile", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "NameParameterSet", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string Name { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter NoServiceRestart { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SessionConfigurationFile", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string Path { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"PowerShellVersion"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "AssemblyNameParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "NameParameterSet")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Version PSVersion { get { return default(System.Version); } set { } }
    [System.Management.Automation.CredentialAttribute]
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.PSCredential RunAsCredential { get { return default(System.Management.Automation.PSCredential); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string SecurityDescriptorSddl { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "AssemblyNameParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "NameParameterSet")]
    public System.Management.Automation.PSSessionTypeOption SessionTypeOption { get { return default(System.Management.Automation.PSSessionTypeOption); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter ShowSecurityDescriptorUI { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string StartupScript { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.Runspaces.PSThreadOptions ThreadOptions { get { return default(System.Management.Automation.Runspaces.PSThreadOptions); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.PSTransportOption TransportOption { get { return default(System.Management.Automation.PSTransportOption); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter UseSharedProcess { get { return default(System.Management.Automation.SwitchParameter); } set { } }
  }

   [System.Management.Automation.CmdletAttribute("Receive", "Job", DefaultParameterSetName = "Location", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113372", RemotingCapability = (System.Management.Automation.RemotingCapability)2)]
   public class ReceiveJobCommand : Microsoft.PowerShell.Commands.JobCmdletBase, System.IDisposable {
    public ReceiveJobCommand() { }

    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter AutoRemoveJob { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    public override string[] Command { get { return default(string[]); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"Cn"})]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "ComputerName", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string[] ComputerName { get { return default(string[]); } set { } }
    public override System.Collections.Hashtable Filter { get { return default(System.Collections.Hashtable); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ComputerName", Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "Session", Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "Location", Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    public System.Management.Automation.Job[] Job { get { return default(System.Management.Automation.Job[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Keep { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "Location")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string[] Location { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter NoRecurse { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "Session", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public System.Management.Automation.Runspaces.PSSession[] Session { get { return default(System.Management.Automation.Runspaces.PSSession[]); } set { } }
    public override System.Management.Automation.JobState State { get { return default(System.Management.Automation.JobState); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Wait { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter WriteEvents { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter WriteJobInResults { get { return default(System.Management.Automation.SwitchParameter); } set { } }

    protected override void BeginProcessing (  ) { }
    public void Dispose (  ) { }
    protected override void EndProcessing (  ) { }
    protected override void ProcessRecord (  ) { }
    protected override void StopProcessing (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Receive", "PSSession", DefaultParameterSetName = "Session", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=217037", RemotingCapability = (System.Management.Automation.RemotingCapability)3)]
   public class ReceivePSSessionCommand : Microsoft.PowerShell.Commands.PSRemotingCmdlet {
    public ReceivePSSessionCommand() { }

    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriSessionName")]
    public System.Management.Automation.SwitchParameter AllowRedirection { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerSessionName", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerInstanceId", ValueFromPipelineByPropertyName=true)]
    public string ApplicationName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerSessionName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriSessionName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerInstanceId")]
    public System.Management.Automation.Runspaces.AuthenticationMechanism Authentication { get { return default(System.Management.Automation.Runspaces.AuthenticationMechanism); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerSessionName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriSessionName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriInstanceId")]
    public string CertificateThumbprint { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"Cn"})]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ComputerSessionName", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ComputerInstanceId", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string ComputerName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerInstanceId", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerSessionName", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriSessionName", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriInstanceId", ValueFromPipelineByPropertyName=true)]
    public string ConfigurationName { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"URI","CU"})]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ConnectionUriInstanceId", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ConnectionUriSessionName", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Uri ConnectionUri { get { return default(System.Uri); } set { } }
    [System.Management.Automation.CredentialAttribute]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerSessionName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriSessionName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerInstanceId")]
    public System.Management.Automation.PSCredential Credential { get { return default(System.Management.Automation.PSCredential); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "Id", Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    public int Id { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriInstanceId", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerInstanceId", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "InstanceId", Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Guid InstanceId { get { return default(System.Guid); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Id")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "InstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SessionName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerSessionName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriSessionName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Session")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string JobName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "SessionName", Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerSessionName", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriSessionName", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string Name { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerSessionName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Id")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "InstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SessionName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Session")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriSessionName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriInstanceId")]
    public Microsoft.PowerShell.Commands.OutTarget OutTarget { get { return default(Microsoft.PowerShell.Commands.OutTarget); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerSessionName")]
    [System.Management.Automation.ValidateRangeAttribute(1, 65535)]
    public int Port { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "Session", Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Management.Automation.Runspaces.PSSession Session { get { return default(System.Management.Automation.Runspaces.PSSession); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerSessionName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerInstanceId")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriSessionName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ConnectionUriInstanceId")]
    public System.Management.Automation.Remoting.PSSessionOption SessionOption { get { return default(System.Management.Automation.Remoting.PSSessionOption); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerSessionName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerInstanceId")]
    public System.Management.Automation.SwitchParameter UseSSL { get { return default(System.Management.Automation.SwitchParameter); } set { } }

    protected override void ProcessRecord (  ) { }
    protected override void StopProcessing (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Register", "PSSessionConfiguration", DefaultParameterSetName = "NameParameterSet", SupportsShouldProcess = true, ConfirmImpact = (System.Management.Automation.ConfirmImpact)2, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=144306")]
   public sealed class RegisterPSSessionConfigurationCommand : Microsoft.PowerShell.Commands.PSSessionConfigurationCommandBase {
    public RegisterPSSessionConfigurationCommand() { }

    [System.Management.Automation.AliasAttribute(new string[] {"PA"})]
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    [System.Management.Automation.ValidateSetAttribute(new string[] {"x86","amd64"})]
    public string ProcessorArchitecture { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "NameParameterSet")]
    public System.Management.Automation.Runspaces.PSSessionType SessionType { get { return default(System.Management.Automation.Runspaces.PSSessionType); } set { } }

    protected override void BeginProcessing (  ) { }
    protected override void EndProcessing (  ) { }
    protected override void ProcessRecord (  ) { }

  }

  [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.Job)}, ParameterSetName = new string[1] { "JobParameterSet" })]
    [System.Management.Automation.CmdletAttribute("Remove", "Job", DefaultParameterSetName = "SessionIdParameterSet", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113377")]
   public class RemoveJobCommand : Microsoft.PowerShell.Commands.JobCmdletBase, System.IDisposable {
    public RemoveJobCommand() { }

    [System.Management.Automation.AliasAttribute(new string[] {"F"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "NameParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "JobParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "InstanceIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SessionIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilterParameterSet")]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "JobParameterSet", Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Management.Automation.Job[] Job { get { return default(System.Management.Automation.Job[]); } set { } }

    public void Dispose (  ) { }
    protected override void EndProcessing (  ) { }
    protected override void ProcessRecord (  ) { }
    protected override void StopProcessing (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Remove", "Module", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=141556")]
   public sealed class RemoveModuleCommand : Microsoft.PowerShell.Commands.ModuleCmdletBase {
    public RemoveModuleCommand() { }

    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "FullyQualifiedName", Mandatory=true, ValueFromPipeline=true)]
    public Microsoft.PowerShell.Commands.ModuleSpecification[] FullyQualifiedName { get { return default(Microsoft.PowerShell.Commands.ModuleSpecification[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ModuleInfo", Mandatory=true, ValueFromPipeline=true)]
    public System.Management.Automation.PSModuleInfo[] ModuleInfo { get { return default(System.Management.Automation.PSModuleInfo[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "name", Mandatory=true, ValueFromPipeline=true)]
    public string[] Name { get { return default(string[]); } set { } }

    protected override void EndProcessing (  ) { }
    protected override void ProcessRecord (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Remove", "PSSession", DefaultParameterSetName = "Id", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135250", RemotingCapability = (System.Management.Automation.RemotingCapability)3)]
   public class RemovePSSessionCommand : Microsoft.PowerShell.Commands.PSRunspaceCmdlet {
    public RemovePSSessionCommand() { }

    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ContainerId", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override string[] ContainerId { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "Session", Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    public System.Management.Automation.Runspaces.PSSession[] Session { get { return default(System.Management.Automation.Runspaces.PSSession[]); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"VMGuid"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMId", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override System.Guid[] VMId { get { return default(System.Guid[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "VMName", Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public override string[] VMName { get { return default(string[]); } set { } }

    protected override void ProcessRecord (  ) { }

  }

  [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.Job)})]
   public class ResumeJobCommand : Microsoft.PowerShell.Commands.JobCmdletBase, System.IDisposable {
    public ResumeJobCommand() { }

    public override string[] Command { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "JobParameterSet", Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Management.Automation.Job[] Job { get { return default(System.Management.Automation.Job[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "__AllParameterSets")]
    public System.Management.Automation.SwitchParameter Wait { get { return default(System.Management.Automation.SwitchParameter); } set { } }

    public void Dispose (  ) { }
    protected override void EndProcessing (  ) { }
    protected override void ProcessRecord (  ) { }
    protected override void StopProcessing (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Save", "Help", DefaultParameterSetName = "Path", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=210612")]
   public sealed class SaveHelpCommand : Microsoft.PowerShell.Commands.UpdatableHelpCommandBase {
    public SaveHelpCommand() { }

    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "Path", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public string[] DestinationPath { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "LiteralPath", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Path", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public Microsoft.PowerShell.Commands.ModuleSpecification[] FullyQualifiedModule { get { return default(Microsoft.PowerShell.Commands.ModuleSpecification[]); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"PSPath"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "LiteralPath", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public string[] LiteralPath { get { return default(string[]); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"Name"})]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "LiteralPath", ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "Path", ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public System.Management.Automation.PSModuleInfo[] Module { get { return default(System.Management.Automation.PSModuleInfo[]); } set { } }

    protected override void ProcessRecord (  ) { }

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

    public virtual void ClearContent ( string path ) { }
    public virtual object ClearContentDynamicParameters ( string path ) { return default(object); }
    protected override void ClearItem ( string path ) { }
    protected override void CopyItem ( string path, string copyPath, bool recurse ) { }
    protected override void GetChildItems ( string path, bool recurse ) { }
    protected override void GetChildNames ( string path, System.Management.Automation.ReturnContainers returnContainers ) { }
    public virtual System.Management.Automation.Provider.IContentReader GetContentReader ( string path ) { return default(System.Management.Automation.Provider.IContentReader); }
    public virtual object GetContentReaderDynamicParameters ( string path ) { return default(object); }
    public virtual System.Management.Automation.Provider.IContentWriter GetContentWriter ( string path ) { return default(System.Management.Automation.Provider.IContentWriter); }
    public virtual object GetContentWriterDynamicParameters ( string path ) { return default(object); }
    protected override void GetItem ( string name ) { }
    protected override bool HasChildItems ( string path ) { return default(bool); }
    protected override bool IsValidPath ( string path ) { return default(bool); }
    protected override bool ItemExists ( string path ) { return default(bool); }
    protected override void NewItem ( string path, string type, object newItem ) { }
    protected override void RemoveItem ( string path, bool recurse ) { }
    protected override void RenameItem ( string name, string newName ) { }
    protected override void SetItem ( string name, object value ) { }

  }

  public class SessionStateProviderBaseContentReaderWriter : System.Management.Automation.Provider.IContentReader, System.IDisposable, System.Management.Automation.Provider.IContentWriter {
    internal SessionStateProviderBaseContentReaderWriter() { }
    public void Close (  ) { }
    public void Dispose (  ) { }
    public System.Collections.IList Read ( System.Int64 readCount ) { return default(System.Collections.IList); }
    public void Seek ( System.Int64 offset, System.IO.SeekOrigin origin ) { }
    public System.Collections.IList Write ( System.Collections.IList content ) { return default(System.Collections.IList); }

  }

   [System.Management.Automation.CmdletAttribute("Set", "PSDebug", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113398")]
   public sealed class SetPSDebugCommand : System.Management.Automation.PSCmdlet {
    public SetPSDebugCommand() { }

    [System.Management.Automation.ParameterAttribute(ParameterSetName = "off")]
    public System.Management.Automation.SwitchParameter Off { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "on")]
    public System.Management.Automation.SwitchParameter Step { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "on")]
    public System.Management.Automation.SwitchParameter Strict { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "on")]
    [System.Management.Automation.ValidateRangeAttribute(0, 2)]
    public int Trace { get { return default(int); } set { } }

    protected override void BeginProcessing (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Set", "PSSessionConfiguration", DefaultParameterSetName = "NameParameterSet", SupportsShouldProcess = true, ConfirmImpact = (System.Management.Automation.ConfirmImpact)2, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=144307")]
   public sealed class SetPSSessionConfigurationCommand : Microsoft.PowerShell.Commands.PSSessionConfigurationCommandBase {
    public SetPSSessionConfigurationCommand() { }

    protected override void BeginProcessing (  ) { }
    protected override void EndProcessing (  ) { }
    protected override void ProcessRecord (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Set", "StrictMode", DefaultParameterSetName = "Version", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113450")]
   public class SetStrictModeCommand : System.Management.Automation.PSCmdlet {
    public SetStrictModeCommand() { }

    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Off", Mandatory=true)]
    public System.Management.Automation.SwitchParameter Off { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"v"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Version", Mandatory=true)]
    public System.Version Version { get { return default(System.Version); } set { } }

    protected override void EndProcessing (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Start", "Job", DefaultParameterSetName = "ComputerName", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113405")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.PSRemotingJob)})]
   public class StartJobCommand : Microsoft.PowerShell.Commands.PSExecutionCmdlet, System.IDisposable {
    public StartJobCommand() { }

    public override System.Management.Automation.SwitchParameter AllowRedirection { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    public override string ApplicationName { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"Args"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "LiteralFilePathComputerName")]
    public override object[] ArgumentList { get { return default(object[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "LiteralFilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathComputerName")]
    public override System.Management.Automation.Runspaces.AuthenticationMechanism Authentication { get { return default(System.Management.Automation.Runspaces.AuthenticationMechanism); } set { } }
    public override string CertificateThumbprint { get { return default(string); } set { } }
    public override string[] ComputerName { get { return default(string[]); } set { } }
    public override string ConfigurationName { get { return default(string); } set { } }
    public override System.Uri[] ConnectionUri { get { return default(System.Uri[]); } set { } }
    public override string[] ContainerId { get { return default(string[]); } set { } }
    [System.Management.Automation.CredentialAttribute]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "LiteralFilePathComputerName")]
    public override System.Management.Automation.PSCredential Credential { get { return default(System.Management.Automation.PSCredential); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "DefinitionName", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string DefinitionName { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "DefinitionName")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string DefinitionPath { get { return default(string); } set { } }
    public override System.Management.Automation.SwitchParameter EnableNetworkAccess { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "FilePathComputerName", Mandatory=true)]
    public override string FilePath { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "LiteralFilePathComputerName")]
    public virtual System.Management.Automation.ScriptBlock InitializationScript { get { return default(System.Management.Automation.ScriptBlock); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathComputerName", ValueFromPipeline=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName", ValueFromPipeline=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "LiteralFilePathComputerName", ValueFromPipeline=true)]
    public override System.Management.Automation.PSObject InputObject { get { return default(System.Management.Automation.PSObject); } set { } }
    public override string KeyFilePath { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"PSPath"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "LiteralFilePathComputerName", Mandatory=true)]
    public string LiteralPath { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "LiteralFilePathComputerName", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathComputerName", ValueFromPipelineByPropertyName=true)]
    public virtual string Name { get { return default(string); } set { } }
    public override int Port { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "LiteralFilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathComputerName")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public virtual System.Version PSVersion { get { return default(System.Version); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilePathComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ComputerName")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "LiteralFilePathComputerName")]
    public virtual System.Management.Automation.SwitchParameter RunAs32 { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    public override System.Management.Automation.SwitchParameter RunAsAdministrator { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"Command"})]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ComputerName", Mandatory=true)]
    public override System.Management.Automation.ScriptBlock ScriptBlock { get { return default(System.Management.Automation.ScriptBlock); } set { } }
    public override System.Management.Automation.Runspaces.PSSession[] Session { get { return default(System.Management.Automation.Runspaces.PSSession[]); } set { } }
    public override System.Management.Automation.Remoting.PSSessionOption SessionOption { get { return default(System.Management.Automation.Remoting.PSSessionOption); } set { } }
    public override System.Collections.Hashtable[] SSHConnection { get { return default(System.Collections.Hashtable[]); } set { } }
    public override System.Management.Automation.SwitchParameter SSHTransport { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    public override int ThrottleLimit { get { return default(int); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 2, ParameterSetName = "DefinitionName")]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string Type { get { return default(string); } set { } }
    public override string UserName { get { return default(string); } set { } }
    public override System.Management.Automation.SwitchParameter UseSSL { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    public override System.Guid[] VMId { get { return default(System.Guid[]); } set { } }
    public override string[] VMName { get { return default(string[]); } set { } }

    protected override void BeginProcessing (  ) { }
    protected override void CreateHelpersForSpecifiedComputerNames (  ) { }
    public void Dispose (  ) { }
    protected override void EndProcessing (  ) { }
    protected override void ProcessRecord (  ) { }

  }

  [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.Job)})]
    [System.Management.Automation.CmdletAttribute("Stop", "Job", DefaultParameterSetName = "SessionIdParameterSet", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113413")]
   public class StopJobCommand : Microsoft.PowerShell.Commands.JobCmdletBase, System.IDisposable {
    public StopJobCommand() { }

    public override string[] Command { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "JobParameterSet", Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Management.Automation.Job[] Job { get { return default(System.Management.Automation.Job[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter PassThru { get { return default(System.Management.Automation.SwitchParameter); } set { } }

    public void Dispose (  ) { }
    protected override void EndProcessing (  ) { }
    protected override void ProcessRecord (  ) { }
    protected override void StopProcessing (  ) { }

  }

  public class SuspendJobCommand : Microsoft.PowerShell.Commands.JobCmdletBase, System.IDisposable {
    public SuspendJobCommand() { }

    public override string[] Command { get { return default(string[]); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"F"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "FilterParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "JobParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "NameParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "SessionIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "InstanceIdParameterSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "StateParameterSet")]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "JobParameterSet", Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Management.Automation.Job[] Job { get { return default(System.Management.Automation.Job[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Wait { get { return default(System.Management.Automation.SwitchParameter); } set { } }

    public void Dispose (  ) { }
    protected override void EndProcessing (  ) { }
    protected override void ProcessRecord (  ) { }
    protected override void StopProcessing (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Test", "ModuleManifest", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=141557")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.PSModuleInfo)})]
   public sealed class TestModuleManifestCommand : Microsoft.PowerShell.Commands.ModuleCmdletBase {
    public TestModuleManifestCommand() { }

    [System.Management.Automation.ParameterAttribute(Position = 0, Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    public string Path { get { return default(string); } set { } }

    protected override void ProcessRecord (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Test", "PSSessionConfigurationFile", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=217039")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Boolean)})]
   public class TestPSSessionConfigurationFileCommand : System.Management.Automation.PSCmdlet {
    public TestPSSessionConfigurationFileCommand() { }

    [System.Management.Automation.ParameterAttribute(Position = 0, Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    public string Path { get { return default(string); } set { } }

    protected override void ProcessRecord (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Unregister", "PSSessionConfiguration", SupportsShouldProcess = true, ConfirmImpact = (System.Management.Automation.ConfirmImpact)1, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=144308")]
   public sealed class UnregisterPSSessionConfigurationCommand : System.Management.Automation.PSCmdlet {
    public UnregisterPSSessionConfigurationCommand() { }

    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, Mandatory=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string Name { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter NoServiceRestart { get { return default(System.Management.Automation.SwitchParameter); } set { } }

    protected override void BeginProcessing (  ) { }
    protected override void EndProcessing (  ) { }
    protected override void ProcessRecord (  ) { }

  }

  public class UpdatableHelpCommandBase : System.Management.Automation.PSCmdlet {
    internal UpdatableHelpCommandBase() { }

    [System.Management.Automation.CredentialAttribute]
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.PSCredential Credential { get { return default(System.Management.Automation.PSCredential); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 2)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public System.Globalization.CultureInfo[] UICulture { get { return default(System.Globalization.CultureInfo[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter UseDefaultCredentials { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    
    protected override void EndProcessing (  ) { }
    protected override void StopProcessing (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Update", "Help", DefaultParameterSetName = "Path", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=210614")]
   public sealed class UpdateHelpCommand : Microsoft.PowerShell.Commands.UpdatableHelpCommandBase {
    public UpdateHelpCommand() { }

    [System.Management.Automation.ParameterAttribute(ParameterSetName = "Path", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "LiteralPath", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public Microsoft.PowerShell.Commands.ModuleSpecification[] FullyQualifiedModule { get { return default(Microsoft.PowerShell.Commands.ModuleSpecification[]); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"PSPath"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "LiteralPath", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public string[] LiteralPath { get { return default(string[]); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"Name"})]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "LiteralPath", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "Path", ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullAttribute]
    public string[] Module { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Recurse { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "Path")]
    [System.Management.Automation.ValidateNotNullAttribute]
    public string[] SourcePath { get { return default(string[]); } set { } }
    
    protected override void BeginProcessing (  ) { }
    protected override void ProcessRecord (  ) { }

  }

  [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.PSVariable)}, ProviderCmdlet = "Rename-Item")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.PSVariable)}, ProviderCmdlet = "Set-Item")]
    [System.Management.Automation.Provider.CmdletProviderAttribute("Variable", (System.Management.Automation.Provider.ProviderCapabilities)16)]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.PSVariable)}, ProviderCmdlet = "Copy-Item")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.PSVariable)}, ProviderCmdlet = "Get-Item")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.PSVariable)}, ProviderCmdlet = "New-Item")]
   public sealed class VariableProvider : Microsoft.PowerShell.Commands.SessionStateProviderBase {
    public VariableProvider() { }

    public const string ProviderName = "Variable";
    protected override System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo> InitializeDefaultDrives (  ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo>); }

  }

   [System.Management.Automation.CmdletAttribute("Wait", "Job", DefaultParameterSetName = "SessionIdParameterSet", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113422")]
   [System.Management.Automation.OutputTypeAttribute(new System.Type[] { typeof(System.Management.Automation.Job)})]
   public class WaitJobCommand : Microsoft.PowerShell.Commands.JobCmdletBase, System.IDisposable {
    public WaitJobCommand() { }

    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Any { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    public override string[] Command { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "JobParameterSet", Mandatory=true, ValueFromPipeline=true, ValueFromPipelineByPropertyName=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public System.Management.Automation.Job[] Job { get { return default(System.Management.Automation.Job[]); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"TimeoutSec"})]
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateRangeAttribute(-1, 2147483647)]
    public int Timeout { get { return default(int); } set { } }

    protected override void BeginProcessing (  ) { }
    public void Dispose (  ) { }
    protected override void EndProcessing (  ) { }
    protected override void ProcessRecord (  ) { }
    protected override void StopProcessing (  ) { }

  }

   [System.Management.Automation.CmdletAttribute("Where", "Object", DefaultParameterSetName = "EqualSet", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113423", RemotingCapability = (System.Management.Automation.RemotingCapability)0)]
   public sealed class WhereObjectCommand : System.Management.Automation.PSCmdlet {
    public WhereObjectCommand() { }

    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CaseSensitiveContainsSet", Mandatory=true)]
    public System.Management.Automation.SwitchParameter CContains { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CaseSensitiveEqualSet", Mandatory=true)]
    public System.Management.Automation.SwitchParameter CEQ { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CaseSensitiveGreaterOrEqualSet", Mandatory=true)]
    public System.Management.Automation.SwitchParameter CGE { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CaseSensitiveGreaterThanSet", Mandatory=true)]
    public System.Management.Automation.SwitchParameter CGT { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CaseSensitiveInSet", Mandatory=true)]
    public System.Management.Automation.SwitchParameter CIn { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CaseSensitiveLessOrEqualSet", Mandatory=true)]
    public System.Management.Automation.SwitchParameter CLE { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CaseSensitiveLikeSet", Mandatory=true)]
    public System.Management.Automation.SwitchParameter CLike { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CaseSensitiveLessThanSet", Mandatory=true)]
    public System.Management.Automation.SwitchParameter CLT { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CaseSensitiveMatchSet", Mandatory=true)]
    public System.Management.Automation.SwitchParameter CMatch { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CaseSensitiveNotEqualSet", Mandatory=true)]
    public System.Management.Automation.SwitchParameter CNE { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CaseSensitiveNotContainsSet", Mandatory=true)]
    public System.Management.Automation.SwitchParameter CNotContains { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CaseSensitiveNotInSet", Mandatory=true)]
    public System.Management.Automation.SwitchParameter CNotIn { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CaseSensitiveNotLikeSet", Mandatory=true)]
    public System.Management.Automation.SwitchParameter CNotLike { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "CaseSensitiveNotMatchSet", Mandatory=true)]
    public System.Management.Automation.SwitchParameter CNotMatch { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"IContains"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "ContainsSet", Mandatory=true)]
    public System.Management.Automation.SwitchParameter Contains { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"IEQ"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "EqualSet")]
    public System.Management.Automation.SwitchParameter EQ { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ScriptBlockSet", Mandatory=true)]
    public System.Management.Automation.ScriptBlock FilterScript { get { return default(System.Management.Automation.ScriptBlock); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"IGE"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "GreaterOrEqualSet", Mandatory=true)]
    public System.Management.Automation.SwitchParameter GE { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"IGT"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "GreaterThanSet", Mandatory=true)]
    public System.Management.Automation.SwitchParameter GT { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"IIn"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "InSet", Mandatory=true)]
    public System.Management.Automation.SwitchParameter In { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ValueFromPipeline=true)]
    public System.Management.Automation.PSObject InputObject { get { return default(System.Management.Automation.PSObject); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "IsSet", Mandatory=true)]
    public System.Management.Automation.SwitchParameter Is { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "IsNotSet", Mandatory=true)]
    public System.Management.Automation.SwitchParameter IsNot { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"ILE"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "LessOrEqualSet", Mandatory=true)]
    public System.Management.Automation.SwitchParameter LE { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"ILike"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "LikeSet", Mandatory=true)]
    public System.Management.Automation.SwitchParameter Like { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"ILT"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "LessThanSet", Mandatory=true)]
    public System.Management.Automation.SwitchParameter LT { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"IMatch"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "MatchSet", Mandatory=true)]
    public System.Management.Automation.SwitchParameter Match { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"INE"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "NotEqualSet", Mandatory=true)]
    public System.Management.Automation.SwitchParameter NE { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"INotContains"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "NotContainsSet", Mandatory=true)]
    public System.Management.Automation.SwitchParameter NotContains { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"INotIn"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "NotInSet", Mandatory=true)]
    public System.Management.Automation.SwitchParameter NotIn { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"INotLike"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "NotLikeSet", Mandatory=true)]
    public System.Management.Automation.SwitchParameter NotLike { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] {"INotMatch"})]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "NotMatchSet", Mandatory=true)]
    public System.Management.Automation.SwitchParameter NotMatch { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "CaseSensitiveInSet", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "IsSet", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "CaseSensitiveNotInSet", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "NotInSet", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "EqualSet", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "InSet", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "CaseSensitiveNotContainsSet", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "NotContainsSet", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "CaseSensitiveContainsSet", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "ContainsSet", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "CaseSensitiveNotMatchSet", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "NotMatchSet", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "CaseSensitiveMatchSet", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "MatchSet", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "IsNotSet", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "CaseSensitiveNotLikeSet", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "CaseSensitiveLikeSet", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "LikeSet", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "CaseSensitiveLessOrEqualSet", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "LessOrEqualSet", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "CaseSensitiveGreaterOrEqualSet", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "GreaterOrEqualSet", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "CaseSensitiveLessThanSet", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "LessThanSet", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "CaseSensitiveGreaterThanSet", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "GreaterThanSet", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "CaseSensitiveNotEqualSet", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "NotEqualSet", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "CaseSensitiveEqualSet", Mandatory=true)]
    [System.Management.Automation.ParameterAttribute(Position = 0, ParameterSetName = "NotLikeSet", Mandatory=true)]
    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
    public string Property { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "CaseSensitiveInSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "CaseSensitiveNotInSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "NotInSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "EqualSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "InSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "CaseSensitiveNotContainsSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "NotContainsSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "CaseSensitiveContainsSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "ContainsSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "CaseSensitiveNotMatchSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "NotMatchSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "CaseSensitiveMatchSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "MatchSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "CaseSensitiveNotLikeSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "NotLikeSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "CaseSensitiveLikeSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "LikeSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "CaseSensitiveLessOrEqualSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "LessOrEqualSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "CaseSensitiveGreaterOrEqualSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "GreaterOrEqualSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "CaseSensitiveLessThanSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "LessThanSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "CaseSensitiveGreaterThanSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "GreaterThanSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "CaseSensitiveNotEqualSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "NotEqualSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "CaseSensitiveEqualSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "IsSet")]
    [System.Management.Automation.ParameterAttribute(Position = 1, ParameterSetName = "IsNotSet")]
    public object Value { get { return default(object); } set { } }
    
    protected override void BeginProcessing (  ) { }
    protected override void ProcessRecord (  ) { }

  }

  public class WSManConfigurationOption : System.Management.Automation.PSTransportOption {
    internal WSManConfigurationOption() { }
    public System.Nullable<int> IdleTimeoutSec { get { return default(System.Nullable<int>); } set { } }
    public System.Nullable<int> MaxConcurrentCommandsPerSession { get { return default(System.Nullable<int>); } set { } }
    public System.Nullable<int> MaxConcurrentUsers { get { return default(System.Nullable<int>); } set { } }
    public System.Nullable<int> MaxIdleTimeoutSec { get { return default(System.Nullable<int>); } set { } }
    public System.Nullable<int> MaxMemoryPerSessionMB { get { return default(System.Nullable<int>); } set { } }
    public System.Nullable<int> MaxProcessesPerSession { get { return default(System.Nullable<int>); } set { } }
    public System.Nullable<int> MaxSessions { get { return default(System.Nullable<int>); } set { } }
    public System.Nullable<int> MaxSessionsPerUser { get { return default(System.Nullable<int>); } set { } }
    public System.Nullable<System.Management.Automation.Runspaces.OutputBufferingMode> OutputBufferingMode { get { return default(System.Nullable<System.Management.Automation.Runspaces.OutputBufferingMode>); } set { } }
    public System.Nullable<int> ProcessIdleTimeoutSec { get { return default(System.Nullable<int>); } set { } }
    protected internal override void LoadFromDefaults ( System.Management.Automation.Runspaces.PSSessionType sessionType, bool keepAssigned ) { }

  }

}
namespace Microsoft.PowerShell.Commands.Internal {
  public static class RemotingErrorResources {
    public static string CouldNotResolveRoleDefinitionPrincipal { get { return default(string); } }
    public static string WinRMRestartWarning { get { return default(string); } }
  }

}
namespace Microsoft.PowerShell.Commands.Internal.Format {
  public abstract class FrontEndCommandBase : System.Management.Automation.PSCmdlet, System.IDisposable {
    protected FrontEndCommandBase() { }

    [System.Management.Automation.ParameterAttribute(ValueFromPipeline=true)]
    public System.Management.Automation.PSObject InputObject { get { return default(System.Management.Automation.PSObject); } set { } }
    protected override void BeginProcessing (  ) { }
    public virtual void Dispose (  ) { }
    protected virtual void Dispose ( bool disposing ) { }
    protected override void EndProcessing (  ) { }
    protected virtual System.Management.Automation.PSObject InputObjectCall (  ) { return default(System.Management.Automation.PSObject); }
    protected virtual void InternalDispose (  ) { }
    protected virtual System.Management.Automation.PSCmdlet OuterCmdletCall (  ) { return default(System.Management.Automation.PSCmdlet); }
    protected override void ProcessRecord (  ) { }
    protected override void StopProcessing (  ) { }
    protected virtual void WriteObjectCall ( object value ) { }

  }

  public class OuterFormatShapeCommandBase : Microsoft.PowerShell.Commands.Internal.Format.FrontEndCommandBase, System.IDisposable {
    public OuterFormatShapeCommandBase() { }

    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter DisplayError { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateSetAttribute(new string[] { "CoreOnly", "EnumOnly", "Both" }, IgnoreCase = true)]
    public string Expand { get { return default(string); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public object GroupBy { get { return default(object); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter ShowError { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public string View { get { return default(string); } set { } }
    protected override void BeginProcessing (  ) { }

  }

  public class OuterFormatTableAndListBase : Microsoft.PowerShell.Commands.Internal.Format.OuterFormatShapeCommandBase, System.IDisposable {
    public OuterFormatTableAndListBase() { }

    [System.Management.Automation.ParameterAttribute(Position = 0)]
    public object[] Property { get { return default(object[]); } set { } }
  }

  public class OuterFormatTableBase : Microsoft.PowerShell.Commands.Internal.Format.OuterFormatTableAndListBase, System.IDisposable {
    public OuterFormatTableBase() { }

    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter AutoSize { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter HideTableHeaders { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Wrap { get { return default(System.Management.Automation.SwitchParameter); } set { } }
  }

}
namespace Microsoft.PowerShell.CoreClr.Stubs {
  public enum AuthenticationLevel {
    Call = 3,
    Connect = 2,
    Default = 0,
    None = 1,
    Packet = 4,
    PacketIntegrity = 5,
    PacketPrivacy = 6,
    Unchanged = -1,
  }

  public enum ImpersonationLevel {
    Anonymous = 1,
    Default = 0,
    Delegate = 4,
    Identify = 2,
    Impersonate = 3,
  }

}
namespace Microsoft.PowerShell.Cim {
  public sealed class CimInstanceAdapter : System.Management.Automation.PSPropertyAdapter {
    public CimInstanceAdapter() { }

    public override System.Collections.ObjectModel.Collection<System.Management.Automation.PSAdaptedProperty> GetProperties ( object baseObject ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSAdaptedProperty>); }
    public override System.Management.Automation.PSAdaptedProperty GetProperty ( object baseObject, string propertyName ) { return default(System.Management.Automation.PSAdaptedProperty); }
    public override string GetPropertyTypeName ( System.Management.Automation.PSAdaptedProperty adaptedProperty ) { return default(string); }
    public override object GetPropertyValue ( System.Management.Automation.PSAdaptedProperty adaptedProperty ) { return default(object); }
    public override System.Collections.ObjectModel.Collection<System.String> GetTypeNameHierarchy ( object baseObject ) { return default(System.Collections.ObjectModel.Collection<System.String>); }
    public override bool IsGettable ( System.Management.Automation.PSAdaptedProperty adaptedProperty ) { return default(bool); }
    public override bool IsSettable ( System.Management.Automation.PSAdaptedProperty adaptedProperty ) { return default(bool); }
    public override void SetPropertyValue ( System.Management.Automation.PSAdaptedProperty adaptedProperty, object value ) { }

  }

}
namespace System.Management.Automation {
    internal class OutputProcessingStateEventArgs { }
    internal class PSRemotingJob { }


  public enum ActionPreference {
    Continue = 2,
    Ignore = 4,
    Inquire = 3,
    SilentlyContinue = 0,
    Stop = 1,
    Suspend = 5,
  }

    [System.SerializableAttribute]
   public class ActionPreferenceStopException : System.Management.Automation.RuntimeException {
    public ActionPreferenceStopException() { }
    protected ActionPreferenceStopException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public ActionPreferenceStopException(string message) { }
    public ActionPreferenceStopException(string message, System.Exception innerException) { }

    public override System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

  }

   [System.AttributeUsageAttribute((System.AttributeTargets)388, AllowMultiple = false)]
   public sealed class AliasAttribute : System.Management.Automation.Internal.ParsingBaseAttribute {
    public AliasAttribute(string[] aliasNames) { }

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

   [System.AttributeUsageAttribute((System.AttributeTargets)384)]
   public sealed class AllowEmptyCollectionAttribute : System.Management.Automation.Internal.CmdletMetadataAttribute {
    public AllowEmptyCollectionAttribute() { }

  }

   [System.AttributeUsageAttribute((System.AttributeTargets)384)]
   public sealed class AllowEmptyStringAttribute : System.Management.Automation.Internal.CmdletMetadataAttribute {
    public AllowEmptyStringAttribute() { }

  }

   [System.AttributeUsageAttribute((System.AttributeTargets)384)]
   public sealed class AllowNullAttribute : System.Management.Automation.Internal.CmdletMetadataAttribute {
    public AllowNullAttribute() { }

  }

    [System.SerializableAttribute]
   public class ApplicationFailedException : System.Management.Automation.RuntimeException {
    protected ApplicationFailedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
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
    public override string Source { get { return default(string); } }
    public override System.Version Version { get { return default(System.Version); } }
    public override System.Management.Automation.SessionStateEntryVisibility Visibility { get { return default(System.Management.Automation.SessionStateEntryVisibility); } set { } }
  }

   [System.AttributeUsageAttribute((System.AttributeTargets)384)]
   public class ArgumentCompleterAttribute : System.Attribute {
    public ArgumentCompleterAttribute(System.Type type) { }
    public ArgumentCompleterAttribute(System.Management.Automation.ScriptBlock scriptBlock) { }

    public System.Management.Automation.ScriptBlock ScriptBlock { get { return default(System.Management.Automation.ScriptBlock); } set { } }
    public System.Type Type { get { return default(System.Type); } set { } }
  }

   [System.AttributeUsageAttribute((System.AttributeTargets)384)]
   public class ArgumentCompletionsAttribute : System.Attribute {
    public ArgumentCompletionsAttribute(string[] completions) { }

    public System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult> CompleteArgument ( string commandName, string parameterName, string wordToComplete, System.Management.Automation.Language.CommandAst commandAst, System.Collections.IDictionary fakeBoundParameters ) { return default(System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult>); }

  }

   [System.AttributeUsageAttribute((System.AttributeTargets)384)]
   public abstract class ArgumentTransformationAttribute : System.Management.Automation.Internal.CmdletMetadataAttribute {
    protected ArgumentTransformationAttribute() { }

    public virtual bool TransformNullOptionalParameters { get { return default(bool); } }
    public virtual object Transform ( System.Management.Automation.EngineIntrinsics engineIntrinsics, object inputData ) { return default(object); }

  }

    [System.SerializableAttribute]
   public class ArgumentTransformationMetadataException : System.Management.Automation.MetadataException {
    protected ArgumentTransformationMetadataException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public ArgumentTransformationMetadataException() { }
    public ArgumentTransformationMetadataException(string message) { }
    public ArgumentTransformationMetadataException(string message, System.Exception innerException) { }

  }

  public class AuthorizationManager {
    public AuthorizationManager(string shellId) { }

    protected internal virtual bool ShouldRun ( System.Management.Automation.CommandInfo commandInfo, System.Management.Automation.CommandOrigin origin, System.Management.Automation.Host.PSHost host, out System.Exception reason ) { reason = default(System.Exception); return default(bool); }

  }

  public sealed class BreakException : System.Management.Automation.LoopFlowException {
  }

  public abstract class Breakpoint {
    public System.Management.Automation.ScriptBlock Action { get { return default(System.Management.Automation.ScriptBlock); } set { } }
    public bool Enabled { get { return default(bool); } set { } }
    public int HitCount { get { return default(int); } set { } }
    public int Id { get { return default(int); } set { } }
    public string Script { get { return default(string); } set { } }
  }

  public class BreakpointUpdatedEventArgs : System.EventArgs {
    internal BreakpointUpdatedEventArgs() { }
    public System.Management.Automation.Breakpoint Breakpoint { get { return default(System.Management.Automation.Breakpoint); } set { } }
    public int BreakpointCount { get { return default(int); } set { } }
    public System.Management.Automation.BreakpointUpdateType UpdateType { get { return default(System.Management.Automation.BreakpointUpdateType); } set { } }
  }

  public enum BreakpointUpdateType {
    Disabled = 3,
    Enabled = 2,
    Removed = 1,
    Set = 0,
  }

  public abstract class CachedValidValuesGeneratorBase {
    protected CachedValidValuesGeneratorBase(int cacheExpirationInSeconds) { }

    public virtual string[] GenerateValidValues (  ) { return default(string[]); }
    public virtual string[] GetValidValues (  ) { return default(string[]); }

  }

  public sealed class CallStackFrame {
    internal CallStackFrame() { }
    public CallStackFrame(System.Management.Automation.InvocationInfo invocationInfo) { }

    public string FunctionName { get { return default(string); } }
    public System.Management.Automation.InvocationInfo InvocationInfo { get { return default(System.Management.Automation.InvocationInfo); } set { } }
    public System.Management.Automation.Language.IScriptExtent Position { get { return default(System.Management.Automation.Language.IScriptExtent); } set { } }
    public int ScriptLineNumber { get { return default(int); } }
    public string ScriptName { get { return default(string); } }
    public System.Collections.Generic.Dictionary<System.String,System.Management.Automation.PSVariable> GetFrameVariables (  ) { return default(System.Collections.Generic.Dictionary<System.String,System.Management.Automation.PSVariable>); }
    public string GetScriptLocation (  ) { return default(string); }
    public override string ToString (  ) { return default(string); }

  }

  public sealed class ChildItemCmdletProviderIntrinsics {
    internal ChildItemCmdletProviderIntrinsics() { }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Get ( string path, bool recurse ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Get ( string[] path, bool recurse, uint depth, bool force, bool literalPath ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Get ( string[] path, bool recurse, bool force, bool literalPath ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.String> GetNames ( string path, System.Management.Automation.ReturnContainers returnContainers, bool recurse ) { return default(System.Collections.ObjectModel.Collection<System.String>); }
    public System.Collections.ObjectModel.Collection<System.String> GetNames ( string[] path, System.Management.Automation.ReturnContainers returnContainers, bool recurse, bool force, bool literalPath ) { return default(System.Collections.ObjectModel.Collection<System.String>); }
    public System.Collections.ObjectModel.Collection<System.String> GetNames ( string[] path, System.Management.Automation.ReturnContainers returnContainers, bool recurse, uint depth, bool force, bool literalPath ) { return default(System.Collections.ObjectModel.Collection<System.String>); }
    public bool HasChild ( string path ) { return default(bool); }
    public bool HasChild ( string path, bool force, bool literalPath ) { return default(bool); }

  }

  public abstract class Cmdlet : System.Management.Automation.Internal.InternalCommand {
    protected Cmdlet() { }

    public System.Management.Automation.ICommandRuntime CommandRuntime { get { return default(System.Management.Automation.ICommandRuntime); } set { } }
    public System.Collections.Generic.HashSet<string> CommonParameters { get { return default(System.Collections.Generic.HashSet<string>); } }
    public System.Management.Automation.PSTransactionContext CurrentPSTransaction { get { return default(System.Management.Automation.PSTransactionContext); } }
    public System.Collections.Generic.HashSet<string> OptionalCommonParameters { get { return default(System.Collections.Generic.HashSet<string>); } }
    public bool Stopping { get { return default(bool); } }
    protected virtual void BeginProcessing (  ) { }
    protected virtual void EndProcessing (  ) { }
    public virtual string GetResourceString ( string baseName, string resourceId ) { return default(string); }
    public System.Collections.IEnumerable Invoke (  ) { return default(System.Collections.IEnumerable); }
    public IEnumerable Invoke<IEnumerable> (  ) { return default(IEnumerable); }
    protected virtual void ProcessRecord (  ) { }
    public bool ShouldContinue ( string query, string caption, bool hasSecurityImpact, ref bool yesToAll, ref bool noToAll ) { return default(bool); }
    public bool ShouldContinue ( string query, string caption, ref bool yesToAll, ref bool noToAll ) { return default(bool); }
    public bool ShouldContinue ( string query, string caption ) { return default(bool); }
    public bool ShouldProcess ( string verboseDescription, string verboseWarning, string caption, out System.Management.Automation.ShouldProcessReason shouldProcessReason ) { shouldProcessReason = default(System.Management.Automation.ShouldProcessReason); return default(bool); }
    public bool ShouldProcess ( string target ) { return default(bool); }
    public bool ShouldProcess ( string target, string action ) { return default(bool); }
    public bool ShouldProcess ( string verboseDescription, string verboseWarning, string caption ) { return default(bool); }
    protected virtual void StopProcessing (  ) { }
    public void ThrowTerminatingError ( System.Management.Automation.ErrorRecord errorRecord ) { }
    public bool TransactionAvailable (  ) { return default(bool); }
    public void WriteCommandDetail ( string text ) { }
    public void WriteDebug ( string text ) { }
    public void WriteError ( System.Management.Automation.ErrorRecord errorRecord ) { }
    public void WriteInformation ( object messageData, string[] tags ) { }
    public void WriteInformation ( System.Management.Automation.InformationRecord informationRecord ) { }
    public void WriteObject ( object sendToPipeline, bool enumerateCollection ) { }
    public void WriteObject ( object sendToPipeline ) { }
    public void WriteProgress ( System.Management.Automation.ProgressRecord progressRecord ) { }
    public void WriteVerbose ( string text ) { }
    public void WriteWarning ( string text ) { }

  }

   [System.AttributeUsageAttribute((System.AttributeTargets)4)]
   public sealed class CmdletAttribute : System.Management.Automation.CmdletCommonMetadataAttribute {
    public CmdletAttribute(string verbName, string nounName) { }

    public string NounName { get { return default(string); } }
    public string VerbName { get { return default(string); } }
  }

   [System.AttributeUsageAttribute((System.AttributeTargets)4)]
   public class CmdletBindingAttribute : System.Management.Automation.CmdletCommonMetadataAttribute {
    public CmdletBindingAttribute() { }

    public bool PositionalBinding { get { return default(bool); } set { } }
  }

   [System.AttributeUsageAttribute((System.AttributeTargets)4)]
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
    public string HelpFile { get { return default(string); } set { } }
    public System.Type ImplementingType { get { return default(System.Type); } }
    public string Noun { get { return default(string); } }
    public System.Management.Automation.ScopedItemOptions Options { get { return default(System.Management.Automation.ScopedItemOptions); } set { } }
    public override System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName> OutputType { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName>); } }
    public System.Management.Automation.PSSnapInInfo PSSnapIn { get { return default(System.Management.Automation.PSSnapInInfo); } }
    public string Verb { get { return default(string); } }
    public override System.Version Version { get { return default(System.Version); } }
  }

    [System.SerializableAttribute]
   public class CmdletInvocationException : System.Management.Automation.RuntimeException {
    public CmdletInvocationException() { }
    public CmdletInvocationException(string message) { }
    public CmdletInvocationException(string message, System.Exception innerException) { }
    protected CmdletInvocationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

    public override System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

  }

    [System.SerializableAttribute]
   public class CmdletProviderInvocationException : System.Management.Automation.CmdletInvocationException {
    public CmdletProviderInvocationException() { }
    protected CmdletProviderInvocationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public CmdletProviderInvocationException(string message) { }
    public CmdletProviderInvocationException(string message, System.Exception innerException) { }

    public System.Management.Automation.ProviderInfo ProviderInfo { get { return default(System.Management.Automation.ProviderInfo); } }
    public System.Management.Automation.ProviderInvocationException ProviderInvocationException { get { return default(System.Management.Automation.ProviderInvocationException); } }
  }

  public sealed class CmdletProviderManagementIntrinsics {
    internal CmdletProviderManagementIntrinsics() { }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.ProviderInfo> Get ( string name ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.ProviderInfo>); }
    public System.Collections.Generic.IEnumerable<System.Management.Automation.ProviderInfo> GetAll (  ) { return default(System.Collections.Generic.IEnumerable<System.Management.Automation.ProviderInfo>); }
    public System.Management.Automation.ProviderInfo GetOne ( string name ) { return default(System.Management.Automation.ProviderInfo); }

  }

  public class CmsMessageRecipient {
    public CmsMessageRecipient(string identifier) { }
    public CmsMessageRecipient(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { }

    public System.Security.Cryptography.X509Certificates.X509Certificate2Collection Certificates { get { return default(System.Security.Cryptography.X509Certificates.X509Certificate2Collection); } set { } }
    public void Resolve ( System.Management.Automation.SessionState sessionState, System.Management.Automation.ResolutionPurpose purpose, out System.Management.Automation.ErrorRecord error ) { error = default(System.Management.Automation.ErrorRecord); }

  }

  public class CommandBreakpoint : System.Management.Automation.Breakpoint {
    public string Command { get { return default(string); } set { } }
    public override string ToString (  ) { return default(string); }

  }

  public class CommandCompletion {
    internal CommandCompletion() { }
    public CommandCompletion(System.Collections.ObjectModel.Collection<System.Management.Automation.CompletionResult> matches, int currentMatchIndex, int replacementIndex, int replacementLength) { }

    public System.Collections.ObjectModel.Collection<System.Management.Automation.CompletionResult> CompletionMatches { get { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.CompletionResult>); } set { } }
    public int CurrentMatchIndex { get { return default(int); } set { } }
    public int ReplacementIndex { get { return default(int); } set { } }
    public int ReplacementLength { get { return default(int); } set { } }
    public static System.Management.Automation.CommandCompletion CompleteInput ( string input, int cursorIndex, System.Collections.Hashtable options, System.Management.Automation.PowerShell powershell ) { return default(System.Management.Automation.CommandCompletion); }
    public static System.Management.Automation.CommandCompletion CompleteInput ( string input, int cursorIndex, System.Collections.Hashtable options ) { return default(System.Management.Automation.CommandCompletion); }
    public static System.Management.Automation.CommandCompletion CompleteInput ( System.Management.Automation.Language.Ast ast, System.Management.Automation.Language.Token[] tokens, System.Management.Automation.Language.IScriptPosition positionOfCursor, System.Collections.Hashtable options ) { return default(System.Management.Automation.CommandCompletion); }
    public static System.Management.Automation.CommandCompletion CompleteInput ( System.Management.Automation.Language.Ast ast, System.Management.Automation.Language.Token[] tokens, System.Management.Automation.Language.IScriptPosition cursorPosition, System.Collections.Hashtable options, System.Management.Automation.PowerShell powershell ) { return default(System.Management.Automation.CommandCompletion); }
    public System.Management.Automation.CompletionResult GetNextResult ( bool forward ) { return default(System.Management.Automation.CompletionResult); }
    public static System.Tuple<System.Management.Automation.Language.Ast,System.Management.Automation.Language.Token[],System.Management.Automation.Language.IScriptPosition> MapStringInputToParsedInput ( string input, int cursorIndex ) { return default(System.Tuple<System.Management.Automation.Language.Ast,System.Management.Automation.Language.Token[],System.Management.Automation.Language.IScriptPosition>); }

  }

  public abstract class CommandInfo {
    internal CommandInfo() { }
    public System.Management.Automation.CommandTypes CommandType { get { return default(System.Management.Automation.CommandTypes); } set { } }
    public abstract string Definition { get; }
    public System.Management.Automation.PSModuleInfo Module { get { return default(System.Management.Automation.PSModuleInfo); } set { } }
    public string ModuleName { get { return default(string); } }
    public string Name { get { return default(string); } set { } }
    public abstract System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName> OutputType { get;  }
    public virtual System.Collections.Generic.Dictionary<string, System.Management.Automation.ParameterMetadata> Parameters { get { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.ParameterMetadata>); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.CommandParameterSetInfo> ParameterSets { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.CommandParameterSetInfo>); } }
    public System.Management.Automation.RemotingCapability RemotingCapability { get { return default(System.Management.Automation.RemotingCapability); } }
    public virtual string Source { get { return default(string); } }
    public virtual System.Version Version { get { return default(System.Version); } }
    public virtual System.Management.Automation.SessionStateEntryVisibility Visibility { get { return default(System.Management.Automation.SessionStateEntryVisibility); } set { } }
    public System.Management.Automation.ParameterMetadata ResolveParameter ( string name ) { return default(System.Management.Automation.ParameterMetadata); }
    public override string ToString (  ) { return default(string); }

  }

  public class CommandInvocationIntrinsics {
    internal CommandInvocationIntrinsics() { }
    public System.EventHandler<System.Management.Automation.CommandLookupEventArgs> CommandNotFoundAction { get { return default(System.EventHandler<System.Management.Automation.CommandLookupEventArgs>); } set { } }
    public bool HasErrors { get { return default(bool); } set { } }
    public System.EventHandler<System.Management.Automation.CommandLookupEventArgs> PostCommandLookupAction { get { return default(System.EventHandler<System.Management.Automation.CommandLookupEventArgs>); } set { } }
    public System.EventHandler<System.Management.Automation.CommandLookupEventArgs> PreCommandLookupAction { get { return default(System.EventHandler<System.Management.Automation.CommandLookupEventArgs>); } set { } }
    public string ExpandString ( string source ) { return default(string); }
    public System.Management.Automation.CmdletInfo GetCmdlet ( string commandName ) { return default(System.Management.Automation.CmdletInfo); }
    public System.Management.Automation.CmdletInfo GetCmdletByTypeName ( string cmdletTypeName ) { return default(System.Management.Automation.CmdletInfo); }
    public System.Collections.Generic.List<System.Management.Automation.CmdletInfo> GetCmdlets ( string pattern ) { return default(System.Collections.Generic.List<System.Management.Automation.CmdletInfo>); }
    public System.Collections.Generic.List<System.Management.Automation.CmdletInfo> GetCmdlets (  ) { return default(System.Collections.Generic.List<System.Management.Automation.CmdletInfo>); }
    public System.Management.Automation.CommandInfo GetCommand ( string commandName, System.Management.Automation.CommandTypes type, object[] arguments ) { return default(System.Management.Automation.CommandInfo); }
    public System.Management.Automation.CommandInfo GetCommand ( string commandName, System.Management.Automation.CommandTypes type ) { return default(System.Management.Automation.CommandInfo); }
    public System.Collections.Generic.List<System.String> GetCommandName ( string name, bool nameIsPattern, bool returnFullName ) { return default(System.Collections.Generic.List<System.String>); }
    public System.Collections.Generic.IEnumerable<System.Management.Automation.CommandInfo> GetCommands ( string name, System.Management.Automation.CommandTypes commandTypes, bool nameIsPattern ) { return default(System.Collections.Generic.IEnumerable<System.Management.Automation.CommandInfo>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> InvokeScript ( string script ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> InvokeScript ( string script, object[] args ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> InvokeScript ( System.Management.Automation.SessionState sessionState, System.Management.Automation.ScriptBlock scriptBlock, object[] args ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> InvokeScript ( bool useLocalScope, System.Management.Automation.ScriptBlock scriptBlock, System.Collections.IList input, object[] args ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> InvokeScript ( string script, bool useNewScope, System.Management.Automation.Runspaces.PipelineResultTypes writeToPipeline, System.Collections.IList input, object[] args ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Management.Automation.ScriptBlock NewScriptBlock ( string scriptText ) { return default(System.Management.Automation.ScriptBlock); }

  }

  public class CommandLookupEventArgs : System.EventArgs {
    internal CommandLookupEventArgs() { }
    public System.Management.Automation.CommandInfo Command { get { return default(System.Management.Automation.CommandInfo); } set { } }
    public string CommandName { get { return default(string); } }
    public System.Management.Automation.CommandOrigin CommandOrigin { get { return default(System.Management.Automation.CommandOrigin); } }
    public System.Management.Automation.ScriptBlock CommandScriptBlock { get { return default(System.Management.Automation.ScriptBlock); } set { } }
    public bool StopSearch { get { return default(bool); } set { } }
  }

   [System.Diagnostics.DebuggerDisplayAttribute("CommandName = {Name}; Type = {CommandType}")]
   public sealed class CommandMetadata {
    public CommandMetadata(System.Type commandType) { }
    public CommandMetadata(System.Management.Automation.CommandInfo commandInfo) { }
    public CommandMetadata(System.Management.Automation.CommandInfo commandInfo, bool shouldGenerateCommonParameters) { }
    public CommandMetadata(string path) { }
    public CommandMetadata(System.Management.Automation.CommandMetadata other) { }

    public System.Type CommandType { get { return default(System.Type); } set { } }
    public System.Management.Automation.ConfirmImpact ConfirmImpact { get { return default(System.Management.Automation.ConfirmImpact); } set { } }
    public string DefaultParameterSetName { get { return default(string); } set { } }
    public string HelpUri { get { return default(string); } set { } }
    public string Name { get { return default(string); } set { } }
    public System.Collections.Generic.Dictionary<string, System.Management.Automation.ParameterMetadata> Parameters { get { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.ParameterMetadata>); } set { } }
    public bool PositionalBinding { get { return default(bool); } set { } }
    public System.Management.Automation.RemotingCapability RemotingCapability { get { return default(System.Management.Automation.RemotingCapability); } set { } }
    public bool SupportsPaging { get { return default(bool); } set { } }
    public bool SupportsShouldProcess { get { return default(bool); } set { } }
    public bool SupportsTransactions { get { return default(bool); } set { } }
    public static System.Collections.Generic.Dictionary<System.String,System.Management.Automation.CommandMetadata> GetRestrictedCommands ( System.Management.Automation.SessionCapabilities sessionCapabilities ) { return default(System.Collections.Generic.Dictionary<System.String,System.Management.Automation.CommandMetadata>); }

  }

    [System.SerializableAttribute]
   public class CommandNotFoundException : System.Management.Automation.RuntimeException {
    public CommandNotFoundException() { }
    public CommandNotFoundException(string message) { }
    public CommandNotFoundException(string message, System.Exception innerException) { }
    protected CommandNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

    public string CommandName { get { return default(string); } set { } }
    public override System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

  }

  public enum CommandOrigin {
    Internal = 1,
    Runspace = 0,
  }

  public class CommandParameterInfo {
    internal CommandParameterInfo() { }
    public System.Collections.ObjectModel.ReadOnlyCollection<string> Aliases { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<string>); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Attribute> Attributes { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Attribute>); } set { } }
    public string HelpMessage { get { return default(string); } set { } }
    public bool IsDynamic { get { return default(bool); } }
    public bool IsMandatory { get { return default(bool); } set { } }
    public string Name { get { return default(string); } }
    public System.Type ParameterType { get { return default(System.Type); } }
    public int Position { get { return default(int); } set { } }
    public bool ValueFromPipeline { get { return default(bool); } set { } }
    public bool ValueFromPipelineByPropertyName { get { return default(bool); } set { } }
    public bool ValueFromRemainingArguments { get { return default(bool); } set { } }
  }

  public class CommandParameterSetInfo {
    internal CommandParameterSetInfo() { }
    public bool IsDefault { get { return default(bool); } set { } }
    public string Name { get { return default(string); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.CommandParameterInfo> Parameters { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.CommandParameterInfo>); } set { } }
    public override string ToString (  ) { return default(string); }

  }

    [System.FlagsAttribute]
   public enum CommandTypes {
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

  public static class CompletionCompleters {
    public static System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult> CompleteCommand ( string commandName ) { return default(System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult>); }
    public static System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult> CompleteCommand ( string commandName, string moduleName, System.Management.Automation.CommandTypes commandTypes ) { return default(System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult>); }
    public static System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult> CompleteFilename ( string fileName ) { return default(System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult>); }
    public static System.Collections.Generic.List<System.Management.Automation.CompletionResult> CompleteOperator ( string wordToComplete ) { return default(System.Collections.Generic.List<System.Management.Automation.CompletionResult>); }
    public static System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult> CompleteType ( string typeName ) { return default(System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult>); }
    public static System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult> CompleteVariable ( string variableName ) { return default(System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult>); }

  }

  public class CompletionResult {
    public CompletionResult(string completionText, string listItemText, System.Management.Automation.CompletionResultType resultType, string toolTip) { }
    public CompletionResult(string completionText) { }

    public string CompletionText { get { return default(string); } }
    public string ListItemText { get { return default(string); } }
    public System.Management.Automation.CompletionResultType ResultType { get { return default(System.Management.Automation.CompletionResultType); } }
    public string ToolTip { get { return default(string); } }
  }

  public enum CompletionResultType {
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

  public class ConfigurationInfo : System.Management.Automation.FunctionInfo {
    public bool IsMetaConfiguration { get { return default(bool); } set { } }
  }

  public enum ConfirmImpact {
    High = 3,
    Low = 1,
    Medium = 2,
    None = 0,
  }

  public sealed class ContainerParentJob : System.Management.Automation.Job2, System.IDisposable {
    public ContainerParentJob(string command, string name) { }
    public ContainerParentJob(string command) { }
    public ContainerParentJob(string command, string name, System.Management.Automation.JobIdentifier jobId) { }
    public ContainerParentJob(string command, string name, System.Guid instanceId) { }
    public ContainerParentJob(string command, string name, System.Management.Automation.JobIdentifier jobId, string jobType) { }
    public ContainerParentJob(string command, string name, System.Guid instanceId, string jobType) { }
    public ContainerParentJob(string command, string name, string jobType) { }

    public override bool HasMoreData { get { return default(bool); } }
    public override string Location { get { return default(string); } }
    public override string StatusMessage { get { return default(string); } }
    public void AddChildJob ( System.Management.Automation.Job2 childJob ) { }
    protected override void Dispose ( bool disposing ) { }
    public override void ResumeJob (  ) { }
    public override void ResumeJobAsync (  ) { }
    public override void StartJob (  ) { }
    public override void StartJobAsync (  ) { }
    public override void StopJob (  ) { }
    public override void StopJob ( bool force, string reason ) { }
    public override void StopJobAsync (  ) { }
    public override void StopJobAsync ( bool force, string reason ) { }
    public override void SuspendJob ( bool force, string reason ) { }
    public override void SuspendJob (  ) { }
    public override void SuspendJobAsync ( bool force, string reason ) { }
    public override void SuspendJobAsync (  ) { }
    public override void UnblockJob (  ) { }
    public override void UnblockJobAsync (  ) { }

  }

  public sealed class ContentCmdletProviderIntrinsics {
    internal ContentCmdletProviderIntrinsics() { }
    public void Clear ( string path ) { }
    public void Clear ( string[] path, bool force, bool literalPath ) { }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.Provider.IContentReader> GetReader ( string path ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.Provider.IContentReader>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.Provider.IContentReader> GetReader ( string[] path, bool force, bool literalPath ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.Provider.IContentReader>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.Provider.IContentWriter> GetWriter ( string path ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.Provider.IContentWriter>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.Provider.IContentWriter> GetWriter ( string[] path, bool force, bool literalPath ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.Provider.IContentWriter>); }

  }

  public sealed class ContinueException : System.Management.Automation.LoopFlowException {
  }

  public class ConvertThroughString : System.Management.Automation.PSTypeConverter {
    public ConvertThroughString() { }

    public override bool CanConvertFrom ( object sourceValue, System.Type destinationType ) { return default(bool); }
    public override bool CanConvertTo ( object sourceValue, System.Type destinationType ) { return default(bool); }
    public override object ConvertFrom ( object sourceValue, System.Type destinationType, System.IFormatProvider formatProvider, bool ignoreCase ) { return default(object); }
    public override object ConvertTo ( object sourceValue, System.Type destinationType, System.IFormatProvider formatProvider, bool ignoreCase ) { return default(object); }

  }

  public enum CopyContainers {
    CopyChildrenOfTargetContainer = 1,
    CopyTargetContainer = 0,
  }

   [System.AttributeUsageAttribute((System.AttributeTargets)384, AllowMultiple = false)]
   public sealed class CredentialAttribute : System.Management.Automation.ArgumentTransformationAttribute {
    public CredentialAttribute() { }

    public override bool TransformNullOptionalParameters { get { return default(bool); } }
    public override object Transform ( System.Management.Automation.EngineIntrinsics engineIntrinsics, object inputData ) { return default(object); }

  }

  public sealed class CustomControl : System.Management.Automation.PSControl {
    public System.Collections.Generic.List<System.Management.Automation.CustomControlEntry> Entries { get { return default(System.Collections.Generic.List<System.Management.Automation.CustomControlEntry>); } set { } }
    public static System.Management.Automation.CustomControlBuilder Create ( bool outOfBand ) { return default(System.Management.Automation.CustomControlBuilder); }

  }

  public sealed class CustomControlBuilder {
    public System.Management.Automation.CustomControl EndControl (  ) { return default(System.Management.Automation.CustomControl); }
    public System.Management.Automation.CustomControlBuilder GroupByProperty ( string property, System.Management.Automation.CustomControl customControl, string label ) { return default(System.Management.Automation.CustomControlBuilder); }
    public System.Management.Automation.CustomControlBuilder GroupByScriptBlock ( string scriptBlock, System.Management.Automation.CustomControl customControl, string label ) { return default(System.Management.Automation.CustomControlBuilder); }
    public System.Management.Automation.CustomEntryBuilder StartEntry ( System.Collections.Generic.IEnumerable<string> entrySelectedByType, System.Collections.Generic.IEnumerable<System.Management.Automation.DisplayEntry> entrySelectedByCondition ) { return default(System.Management.Automation.CustomEntryBuilder); }

  }

  public sealed class CustomControlEntry {
    public System.Collections.Generic.List<System.Management.Automation.CustomItemBase> CustomItems { get { return default(System.Collections.Generic.List<System.Management.Automation.CustomItemBase>); } set { } }
    public System.Management.Automation.EntrySelectedBy SelectedBy { get { return default(System.Management.Automation.EntrySelectedBy); } set { } }
  }

  public sealed class CustomEntryBuilder {
    public System.Management.Automation.CustomEntryBuilder AddCustomControlExpressionBinding ( System.Management.Automation.CustomControl customControl, bool enumerateCollection, string selectedByType, string selectedByScript ) { return default(System.Management.Automation.CustomEntryBuilder); }
    public System.Management.Automation.CustomEntryBuilder AddNewline ( int count ) { return default(System.Management.Automation.CustomEntryBuilder); }
    public System.Management.Automation.CustomEntryBuilder AddPropertyExpressionBinding ( string property, bool enumerateCollection, string selectedByType, string selectedByScript, System.Management.Automation.CustomControl customControl ) { return default(System.Management.Automation.CustomEntryBuilder); }
    public System.Management.Automation.CustomEntryBuilder AddScriptBlockExpressionBinding ( string scriptBlock, bool enumerateCollection, string selectedByType, string selectedByScript, System.Management.Automation.CustomControl customControl ) { return default(System.Management.Automation.CustomEntryBuilder); }
    public System.Management.Automation.CustomEntryBuilder AddText ( string text ) { return default(System.Management.Automation.CustomEntryBuilder); }
    public System.Management.Automation.CustomControlBuilder EndEntry (  ) { return default(System.Management.Automation.CustomControlBuilder); }
    public System.Management.Automation.CustomEntryBuilder EndFrame (  ) { return default(System.Management.Automation.CustomEntryBuilder); }
    public System.Management.Automation.CustomEntryBuilder StartFrame ( uint leftIndent, uint rightIndent, uint firstLineHanging, uint firstLineIndent ) { return default(System.Management.Automation.CustomEntryBuilder); }

  }

  public abstract class CustomItemBase {
    protected CustomItemBase() { }

  }

  public sealed class CustomItemExpression : System.Management.Automation.CustomItemBase {
    public System.Management.Automation.CustomControl CustomControl { get { return default(System.Management.Automation.CustomControl); } set { } }
    public bool EnumerateCollection { get { return default(bool); } set { } }
    public System.Management.Automation.DisplayEntry Expression { get { return default(System.Management.Automation.DisplayEntry); } set { } }
    public System.Management.Automation.DisplayEntry ItemSelectionCondition { get { return default(System.Management.Automation.DisplayEntry); } set { } }
  }

  public sealed class CustomItemFrame : System.Management.Automation.CustomItemBase {
    public System.Collections.Generic.List<System.Management.Automation.CustomItemBase> CustomItems { get { return default(System.Collections.Generic.List<System.Management.Automation.CustomItemBase>); } set { } }
    public uint FirstLineHanging { get { return default(uint); } set { } }
    public uint FirstLineIndent { get { return default(uint); } set { } }
    public uint LeftIndent { get { return default(uint); } set { } }
    public uint RightIndent { get { return default(uint); } set { } }
  }

  public sealed class CustomItemNewline : System.Management.Automation.CustomItemBase {
    public CustomItemNewline() { }

    public int Count { get { return default(int); } set { } }
  }

  public sealed class CustomItemText : System.Management.Automation.CustomItemBase {
    public CustomItemText() { }

    public string Text { get { return default(string); } set { } }
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

  public abstract class Debugger {
    internal Debugger() { }

    public event System.EventHandler<System.Management.Automation.BreakpointUpdatedEventArgs> BreakpointUpdated { add { } remove { } }
    public event System.EventHandler<System.EventArgs> CancelRunspaceDebugProcessing { add { } remove { } }
    public event System.EventHandler<System.Management.Automation.DebuggerStopEventArgs> DebuggerStop { add { } remove { } }
    public event System.EventHandler<System.EventArgs> NestedDebuggingCancelledEvent { add { } remove { } }
    public event System.EventHandler<System.Management.Automation.ProcessRunspaceDebugEndEventArgs> RunspaceDebugProcessingCompleted { add { } remove { } }
    public event System.EventHandler<System.Management.Automation.StartRunspaceDebugProcessingEventArgs> StartRunspaceDebugProcessing { add { } remove { } }

    public System.Management.Automation.DebugModes DebugMode { get { return default(System.Management.Automation.DebugModes); } set { } }
    public bool InBreakpoint { get { return default(bool); } }
    public System.Guid InstanceId { get { return default(System.Guid); } }
    public bool IsActive { get { return default(bool); } }
    public virtual void CancelDebuggerProcessing (  ) { }
    public virtual System.Collections.Generic.IEnumerable<System.Management.Automation.CallStackFrame> GetCallStack (  ) { return default(System.Collections.Generic.IEnumerable<System.Management.Automation.CallStackFrame>); }
    public virtual System.Management.Automation.DebuggerStopEventArgs GetDebuggerStopArgs (  ) { return default(System.Management.Automation.DebuggerStopEventArgs); }
    public virtual System.Management.Automation.DebuggerCommandResults ProcessCommand ( System.Management.Automation.PSCommand command, System.Management.Automation.PSDataCollection<System.Management.Automation.PSObject> output ) { return default(System.Management.Automation.DebuggerCommandResults); }
    public virtual void ResetCommandProcessorSource (  ) { }
    public virtual void SetBreakpoints ( System.Collections.Generic.IEnumerable<System.Management.Automation.Breakpoint> breakpoints ) { }
    public virtual void SetDebuggerAction ( System.Management.Automation.DebuggerResumeAction resumeAction ) { }
    public virtual void SetDebuggerStepMode ( bool enabled ) { }
    public virtual void SetDebugMode ( System.Management.Automation.DebugModes mode ) { }
    public virtual void SetParent ( System.Management.Automation.Debugger parent, System.Collections.Generic.IEnumerable<System.Management.Automation.Breakpoint> breakPoints, System.Nullable<System.Management.Automation.DebuggerResumeAction> startAction, System.Management.Automation.Host.PSHost host, System.Management.Automation.PathInfo path, System.Collections.Generic.Dictionary<string, System.Management.Automation.DebugSource> functionSourceMap ) { }
    public virtual void SetParent ( System.Management.Automation.Debugger parent, System.Collections.Generic.IEnumerable<System.Management.Automation.Breakpoint> breakPoints, System.Nullable<System.Management.Automation.DebuggerResumeAction> startAction, System.Management.Automation.Host.PSHost host, System.Management.Automation.PathInfo path ) { }
    public virtual void StopProcessCommand (  ) { }

  }

  public sealed class DebuggerCommandResults {
    public DebuggerCommandResults(System.Nullable<System.Management.Automation.DebuggerResumeAction> resumeAction, bool evaluatedByDebugger) { }

    public bool EvaluatedByDebugger { get { return default(bool); } set { } }
    public System.Nullable<System.Management.Automation.DebuggerResumeAction> ResumeAction { get { return default(System.Nullable<System.Management.Automation.DebuggerResumeAction>); } set { } }
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
    public DebuggerStopEventArgs(System.Management.Automation.InvocationInfo invocationInfo, System.Collections.ObjectModel.Collection<System.Management.Automation.Breakpoint> breakpoints, System.Management.Automation.DebuggerResumeAction resumeAction) { }

    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Breakpoint> Breakpoints { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Breakpoint>); } set { } }
    public System.Management.Automation.InvocationInfo InvocationInfo { get { return default(System.Management.Automation.InvocationInfo); } set { } }
    public System.Management.Automation.DebuggerResumeAction ResumeAction { get { return default(System.Management.Automation.DebuggerResumeAction); } set { } }
  }

    [System.FlagsAttribute]
   public enum DebugModes {
    Default = 1,
    LocalScript = 2,
    None = 0,
    RemoteScript = 4,
  }

    [System.Runtime.Serialization.DataContractAttribute]
   public class DebugRecord : System.Management.Automation.InformationalRecord {
    public DebugRecord(string message) { }
    public DebugRecord(System.Management.Automation.PSObject record) { }

  }

  public sealed class DebugSource {
    public DebugSource(string script, string scriptFile, string xamlDefinition) { }

    public string Script { get { return default(string); } set { } }
    public string ScriptFile { get { return default(string); } set { } }
    public string XamlDefinition { get { return default(string); } set { } }
  }

   [System.Reflection.DefaultMemberAttribute("Item")]
   public sealed class DefaultParameterDictionary : System.Collections.Hashtable {
    public DefaultParameterDictionary() { }
    public DefaultParameterDictionary(System.Collections.IDictionary dictionary) { }

    public object Item { get { return default(object); } set { } }
    public override void Add ( object key, object value ) { }
    public bool ChangeSinceLastCheck (  ) { return default(bool); }
    public override void Clear (  ) { }
    public override bool Contains ( object key ) { return default(bool); }
    public override bool ContainsKey ( object key ) { return default(bool); }
    public override void Remove ( object key ) { }

  }

  public sealed class DisplayEntry {
    public DisplayEntry(string value, System.Management.Automation.DisplayEntryValueType type) { }

    public string Value { get { return default(string); } set { } }
    public System.Management.Automation.DisplayEntryValueType ValueType { get { return default(System.Management.Automation.DisplayEntryValueType); } set { } }
    public override string ToString (  ) { return default(string); }

  }

  public enum DisplayEntryValueType {
    Property = 0,
    ScriptBlock = 1,
  }

  public sealed class DriveManagementIntrinsics {
    internal DriveManagementIntrinsics() { }
    public System.Management.Automation.PSDriveInfo Current { get { return default(System.Management.Automation.PSDriveInfo); } }
    public System.Management.Automation.PSDriveInfo Get ( string driveName ) { return default(System.Management.Automation.PSDriveInfo); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo> GetAll (  ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo> GetAllAtScope ( string scope ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo> GetAllForProvider ( string providerName ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo>); }
    public System.Management.Automation.PSDriveInfo GetAtScope ( string driveName, string scope ) { return default(System.Management.Automation.PSDriveInfo); }
    public System.Management.Automation.PSDriveInfo New ( System.Management.Automation.PSDriveInfo drive, string scope ) { return default(System.Management.Automation.PSDriveInfo); }
    public void Remove ( string driveName, bool force, string scope ) { }

  }

    [System.SerializableAttribute]
   public class DriveNotFoundException : System.Management.Automation.SessionStateException {
    public DriveNotFoundException() { }
    public DriveNotFoundException(string message) { }
    public DriveNotFoundException(string message, System.Exception innerException) { }
    protected DriveNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

  }

   [System.AttributeUsageAttribute((System.AttributeTargets)4)]
   public class DscLocalConfigurationManagerAttribute : System.Management.Automation.Internal.CmdletMetadataAttribute {
    public DscLocalConfigurationManagerAttribute() { }

  }

   [System.AttributeUsageAttribute((System.AttributeTargets)384)]
   public class DscPropertyAttribute : System.Management.Automation.Internal.CmdletMetadataAttribute {
    public DscPropertyAttribute() { }

    public bool Key { get { return default(bool); } set { } }
    public bool Mandatory { get { return default(bool); } set { } }
    public bool NotConfigurable { get { return default(bool); } set { } }
  }

   [System.AttributeUsageAttribute((System.AttributeTargets)4)]
   public class DscResourceAttribute : System.Management.Automation.Internal.CmdletMetadataAttribute {
    public DscResourceAttribute() { }

    public System.Management.Automation.DSCResourceRunAsCredential RunAsCredential { get { return default(System.Management.Automation.DSCResourceRunAsCredential); } set { } }
  }

  public class DscResourceInfo {
    public string CompanyName { get { return default(string); } set { } }
    public string FriendlyName { get { return default(string); } set { } }
    public string HelpFile { get { return default(string); } set { } }
    public System.Management.Automation.ImplementedAsType ImplementedAs { get { return default(System.Management.Automation.ImplementedAsType); } set { } }
    public System.Management.Automation.PSModuleInfo Module { get { return default(System.Management.Automation.PSModuleInfo); } set { } }
    public string Name { get { return default(string); } set { } }
    public string ParentPath { get { return default(string); } set { } }
    public string Path { get { return default(string); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.DscResourcePropertyInfo> Properties { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.DscResourcePropertyInfo>); } set { } }
    public string ResourceType { get { return default(string); } set { } }
    public void UpdateProperties ( System.Collections.Generic.IList<System.Management.Automation.DscResourcePropertyInfo> properties ) { }

  }

  public sealed class DscResourcePropertyInfo {
    public bool IsMandatory { get { return default(bool); } set { } }
    public string Name { get { return default(string); } set { } }
    public string PropertyType { get { return default(string); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<string> Values { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<string>); } set { } }
  }

  public enum DSCResourceRunAsCredential {
    Default = 0,
    Mandatory = 2,
    NotSupported = 1,
    Optional = 0,
  }

   [System.AttributeUsageAttribute((System.AttributeTargets)1)]
   public class DynamicClassImplementationAssemblyAttribute : System.Attribute {
    public DynamicClassImplementationAssemblyAttribute() { }

    public string ScriptFile { get { return default(string); } set { } }
  }

  public class EngineIntrinsics {
    internal EngineIntrinsics() { }
    public System.Management.Automation.PSEventManager Events { get { return default(System.Management.Automation.PSEventManager); } }
    public System.Management.Automation.Host.PSHost Host { get { return default(System.Management.Automation.Host.PSHost); } }
    public System.Management.Automation.CommandInvocationIntrinsics InvokeCommand { get { return default(System.Management.Automation.CommandInvocationIntrinsics); } }
    public System.Management.Automation.ProviderIntrinsics InvokeProvider { get { return default(System.Management.Automation.ProviderIntrinsics); } }
    public System.Management.Automation.SessionState SessionState { get { return default(System.Management.Automation.SessionState); } }
  }

  public sealed class EntrySelectedBy {
    public EntrySelectedBy() { }

    public System.Collections.Generic.List<System.Management.Automation.DisplayEntry> SelectionCondition { get { return default(System.Collections.Generic.List<System.Management.Automation.DisplayEntry>); } set { } }
    public System.Collections.Generic.List<string> TypeNames { get { return default(System.Collections.Generic.List<string>); } set { } }
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
    public string GetMessage (  ) { return default(string); }
    public string GetMessage ( System.Globalization.CultureInfo uiCultureInfo ) { return default(string); }
    public override string ToString (  ) { return default(string); }

  }

    [System.SerializableAttribute]
   public class ErrorDetails {
    public ErrorDetails(string message) { }
    public ErrorDetails(System.Management.Automation.Cmdlet cmdlet, string baseName, string resourceId, object[] args) { }
    public ErrorDetails(System.Management.Automation.IResourceSupplier resourceSupplier, string baseName, string resourceId, object[] args) { }
    public ErrorDetails(System.Reflection.Assembly assembly, string baseName, string resourceId, object[] args) { }
    protected ErrorDetails(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

    public string Message { get { return default(string); } }
    public string RecommendedAction { get { return default(string); } set { } }
    public virtual void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }
    public override string ToString (  ) { return default(string); }

  }

    [System.SerializableAttribute]
   public class ErrorRecord {
    public ErrorRecord(System.Exception exception, string errorId, System.Management.Automation.ErrorCategory errorCategory, object targetObject) { }
    protected ErrorRecord(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public ErrorRecord(System.Management.Automation.ErrorRecord errorRecord, System.Exception replaceParentContainsErrorRecordException) { }

    public System.Management.Automation.ErrorCategoryInfo CategoryInfo { get { return default(System.Management.Automation.ErrorCategoryInfo); } }
    public System.Management.Automation.ErrorDetails ErrorDetails { get { return default(System.Management.Automation.ErrorDetails); } set { } }
    public System.Exception Exception { get { return default(System.Exception); } }
    public string FullyQualifiedErrorId { get { return default(string); } }
    public System.Management.Automation.InvocationInfo InvocationInfo { get { return default(System.Management.Automation.InvocationInfo); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<int> PipelineIterationInfo { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<int>); } }
    public string ScriptStackTrace { get { return default(string); } }
    public object TargetObject { get { return default(object); } }
    public virtual void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }
    public override string ToString (  ) { return default(string); }

  }

  public class ExitException : System.Management.Automation.FlowControlException {
    public object Argument { get { return default(object); } set { } }
  }

  public sealed class ExtendedTypeDefinition {
    public ExtendedTypeDefinition(string typeName, System.Collections.Generic.IEnumerable<System.Management.Automation.FormatViewDefinition> viewDefinitions) { }
    public ExtendedTypeDefinition(string typeName) { }

    public System.Collections.Generic.List<System.Management.Automation.FormatViewDefinition> FormatViewDefinition { get { return default(System.Collections.Generic.List<System.Management.Automation.FormatViewDefinition>); } set { } }
    public string TypeName { get { return default(string); } }
    public System.Collections.Generic.List<string> TypeNames { get { return default(System.Collections.Generic.List<string>); } set { } }
    public override string ToString (  ) { return default(string); }

  }

    [System.SerializableAttribute]
   public class ExtendedTypeSystemException : System.Management.Automation.RuntimeException {
    public ExtendedTypeSystemException() { }
    public ExtendedTypeSystemException(string message) { }
    public ExtendedTypeSystemException(string message, System.Exception innerException) { }
    protected ExtendedTypeSystemException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

  }

  public class ExternalScriptInfo : System.Management.Automation.CommandInfo {
    internal ExternalScriptInfo() { }
    public override string Definition { get { return default(string); } }
    public System.Text.Encoding OriginalEncoding { get { return default(System.Text.Encoding); } }
    public override System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName> OutputType { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName>); } }
    public string Path { get { return default(string); } }
    public System.Management.Automation.ScriptBlock ScriptBlock { get { return default(System.Management.Automation.ScriptBlock); } set { } }
    public string ScriptContents { get { return default(string); } }
    public override string Source { get { return default(string); } }
    public override System.Management.Automation.SessionStateEntryVisibility Visibility { get { return default(System.Management.Automation.SessionStateEntryVisibility); } set { } }
    public void ValidateScriptInfo ( System.Management.Automation.Host.PSHost host ) { }

  }

  public class FilterInfo : System.Management.Automation.FunctionInfo {
    internal FilterInfo() { }
  }

  public sealed class FlagsExpression<T>  where T : struct, System.IConvertible {
    public FlagsExpression(string expression) { }
    public FlagsExpression(object[] expression) { }

    public bool Evaluate ( T value ) { return default(bool); }

  }

  public abstract class FlowControlException : System.SystemException {
  }

   [System.Diagnostics.DebuggerDisplayAttribute("{Name}")]
   public sealed class FormatViewDefinition {
    public FormatViewDefinition(string name, System.Management.Automation.PSControl control) { }

    public System.Management.Automation.PSControl Control { get { return default(System.Management.Automation.PSControl); } set { } }
    public string Name { get { return default(string); } set { } }
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
    public string HelpFile { get { return default(string); } set { } }
    public string Noun { get { return default(string); } }
    public System.Management.Automation.ScopedItemOptions Options { get { return default(System.Management.Automation.ScopedItemOptions); } set { } }
    public override System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName> OutputType { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName>); } }
    public System.Management.Automation.ScriptBlock ScriptBlock { get { return default(System.Management.Automation.ScriptBlock); } }
    public string Verb { get { return default(string); } }
    protected internal void Update ( System.Management.Automation.FunctionInfo newFunction, bool force, System.Management.Automation.ScopedItemOptions options, string helpFile ) { }

  }

  public delegate bool GetSymmetricEncryptionKey(System.Runtime.Serialization.StreamingContext context, out byte[] key, out byte[] iv);

  public class GettingValueExceptionEventArgs : System.EventArgs {
    internal GettingValueExceptionEventArgs() { }
    public System.Exception Exception { get { return default(System.Exception); } }
    public bool ShouldThrow { get { return default(bool); } set { } }
    public object ValueReplacement { get { return default(object); } set { } }
  }

    [System.SerializableAttribute]
   public class GetValueException : System.Management.Automation.ExtendedTypeSystemException {
    public GetValueException() { }
    public GetValueException(string message) { }
    public GetValueException(string message, System.Exception innerException) { }
    protected GetValueException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

  }

    [System.SerializableAttribute]
   public class GetValueInvocationException : System.Management.Automation.GetValueException {
    public GetValueInvocationException() { }
    public GetValueInvocationException(string message) { }
    public GetValueInvocationException(string message, System.Exception innerException) { }
    protected GetValueInvocationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

  }

    [System.SerializableAttribute]
   public class HaltCommandException : System.SystemException {
    public HaltCommandException() { }
    public HaltCommandException(string message) { }
    public HaltCommandException(string message, System.Exception innerException) { }
    protected HaltCommandException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

  }

   [System.AttributeUsageAttribute((System.AttributeTargets)992)]
   public sealed class HiddenAttribute : System.Management.Automation.Internal.ParsingBaseAttribute {
    public HiddenAttribute() { }

  }

  public class HostInformationMessage {
    public HostInformationMessage() { }

    public System.Nullable<System.ConsoleColor> BackgroundColor { get { return default(System.Nullable<System.ConsoleColor>); } set { } }
    public System.Nullable<System.ConsoleColor> ForegroundColor { get { return default(System.Nullable<System.ConsoleColor>); } set { } }
    public string Message { get { return default(string); } set { } }
    public System.Nullable<bool> NoNewLine { get { return default(System.Nullable<bool>); } set { } }
    public override string ToString (  ) { return default(string); }

  }

  public static class HostUtilities {
    public const string CreatePSEditFunction = @"
            param (
                [string] $PSEditFunction
            )

            if ($PSVersionTable.PSVersion -lt ([version] '3.0'))
            {
                throw (new-object System.NotSupportedException)
            }

            Register-EngineEvent -SourceIdentifier PSISERemoteSessionOpenFile -Forward

            if ((Test-Path -Path 'function:\global:PSEdit') -eq $false)
            {
                Set-Item -Path 'function:\global:PSEdit' -Value $PSEditFunction
            }
        ";
    public const string PSEditFunction = @"
            param (
                [Parameter(Mandatory=$true)] [String[]] $FileName
            )

            foreach ($file in $FileName)
            {
                Get-ChildItem $file -File | ForEach-Object {
                    $filePathName = $_.FullName

                    # Get file contents
                    $contentBytes = Get-Content -Path $filePathName -Raw -Encoding Byte

                    # Notify client for file open.
                    New-Event -SourceIdentifier PSISERemoteSessionOpenFile -EventArguments @($filePathName, $contentBytes) > $null
                }
            }
        ";
    public const string RemoteSessionOpenFileEvent = "PSISERemoteSessionOpenFile";
    public const string RemovePSEditFunction = @"
            if ($PSVersionTable.PSVersion -lt ([version] '3.0'))
            {
                throw (new-object System.NotSupportedException)
            }

            if ((Test-Path -Path 'function:\global:PSEdit') -eq $true)
            {
                Remove-Item -Path 'function:\global:PSEdit' -Force
            }

            Get-EventSubscriber -SourceIdentifier PSISERemoteSessionOpenFile -EA Ignore | Remove-Event
        ";
    public static System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> InvokeOnRunspace ( System.Management.Automation.PSCommand command, System.Management.Automation.Runspaces.Runspace runspace ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }

  }

  public partial interface IArgumentCompleter {
     System.Collections.Generic.IEnumerable<System.Management.Automation.CompletionResult> CompleteArgument ( string commandName, string parameterName, string wordToComplete, System.Management.Automation.Language.CommandAst commandAst, System.Collections.IDictionary fakeBoundParameters );

  }

  public partial interface ICommandRuntime {
    System.Management.Automation.PSTransactionContext CurrentPSTransaction { get; }

    System.Management.Automation.Host.PSHost Host { get; }

     bool ShouldContinue ( string query, string caption, ref bool yesToAll, ref bool noToAll );
     bool ShouldContinue ( string query, string caption );
     bool ShouldProcess ( string verboseDescription, string verboseWarning, string caption, out System.Management.Automation.ShouldProcessReason shouldProcessReason );
     bool ShouldProcess ( string verboseDescription, string verboseWarning, string caption );
     bool ShouldProcess ( string target, string action );
     bool ShouldProcess ( string target );
     void ThrowTerminatingError ( System.Management.Automation.ErrorRecord errorRecord );
     bool TransactionAvailable (  );
     void WriteCommandDetail ( string text );
     void WriteDebug ( string text );
     void WriteError ( System.Management.Automation.ErrorRecord errorRecord );
     void WriteObject ( object sendToPipeline, bool enumerateCollection );
     void WriteObject ( object sendToPipeline );
     void WriteProgress ( System.Int64 sourceId, System.Management.Automation.ProgressRecord progressRecord );
     void WriteProgress ( System.Management.Automation.ProgressRecord progressRecord );
     void WriteVerbose ( string text );
     void WriteWarning ( string text );

  }

  public partial interface ICommandRuntime2 {
     bool ShouldContinue ( string query, string caption, bool hasSecurityImpact, ref bool yesToAll, ref bool noToAll );
     void WriteInformation ( System.Management.Automation.InformationRecord informationRecord );

  }

  public partial interface IContainsErrorRecord {
    System.Management.Automation.ErrorRecord ErrorRecord { get; }

  }

  public partial interface IDynamicParameters {
     object GetDynamicParameters (  );

  }

  public partial interface IJobDebugger {
    System.Management.Automation.Debugger Debugger { get; }

    bool IsAsync { get; }

  }

  public partial interface IModuleAssemblyCleanup {
     void OnRemove ( System.Management.Automation.PSModuleInfo psModuleInfo );

  }

  public partial interface IModuleAssemblyInitializer {
     void OnImport (  );

  }

  public enum ImplementedAsType {
    Binary = 2,
    Composite = 3,
    None = 0,
    PowerShell = 1,
  }

    [System.SerializableAttribute]
   public class IncompleteParseException : System.Management.Automation.ParseException {
    protected IncompleteParseException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public IncompleteParseException() { }
    public IncompleteParseException(string message) { }
    public IncompleteParseException(string message, System.Exception innerException) { }

  }

    [System.Runtime.Serialization.DataContractAttribute]
   public abstract class InformationalRecord {
    internal InformationalRecord() { }
    public System.Management.Automation.InvocationInfo InvocationInfo { get { return default(System.Management.Automation.InvocationInfo); } }
    public string Message { get { return default(string); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<int> PipelineIterationInfo { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<int>); } }
    public override string ToString (  ) { return default(string); }

  }

    [System.Runtime.Serialization.DataContractAttribute]
   public class InformationRecord {
    public InformationRecord(object messageData, string source) { }

    [System.Runtime.Serialization.DataMemberAttribute]
    public string Computer { get { return default(string); } set { } }
    [System.Runtime.Serialization.DataMemberAttribute]
    public uint ManagedThreadId { get { return default(uint); } set { } }
    [System.Runtime.Serialization.DataMemberAttribute]
    public object MessageData { get { return default(object); } set { } }
    public uint NativeThreadId { get { return default(uint); } set { } }
    [System.Runtime.Serialization.DataMemberAttribute]
    public uint ProcessId { get { return default(uint); } set { } }
    [System.Runtime.Serialization.DataMemberAttribute]
    public string Source { get { return default(string); } set { } }
    [System.Runtime.Serialization.DataMemberAttribute]
    public System.Collections.Generic.List<string> Tags { get { return default(System.Collections.Generic.List<string>); } set { } }
    [System.Runtime.Serialization.DataMemberAttribute]
    public System.DateTime TimeGenerated { get { return default(System.DateTime); } set { } }
    [System.Runtime.Serialization.DataMemberAttribute]
    public string User { get { return default(string); } set { } }
    public override string ToString (  ) { return default(string); }

  }

    [System.SerializableAttribute]
   public class InvalidJobStateException : System.SystemException {
    public InvalidJobStateException() { }
    public InvalidJobStateException(string message) { }
    public InvalidJobStateException(string message, System.Exception innerException) { }
    public InvalidJobStateException(System.Management.Automation.JobState currentState, string actionMessage) { }
    protected InvalidJobStateException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

    public System.Management.Automation.JobState CurrentState { get { return default(System.Management.Automation.JobState); } }
  }

    [System.SerializableAttribute]
   public class InvalidPowerShellStateException : System.SystemException {
    public InvalidPowerShellStateException() { }
    public InvalidPowerShellStateException(string message) { }
    public InvalidPowerShellStateException(string message, System.Exception innerException) { }
    protected InvalidPowerShellStateException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

    public System.Management.Automation.PSInvocationState CurrentState { get { return default(System.Management.Automation.PSInvocationState); } }
  }

   [System.Diagnostics.DebuggerDisplayAttribute("Command = {MyCommand}")]
   public class InvocationInfo {
    internal InvocationInfo() { }
    public System.Collections.Generic.Dictionary<string, object> BoundParameters { get { return default(System.Collections.Generic.Dictionary<string, object>); } set { } }
    public System.Management.Automation.CommandOrigin CommandOrigin { get { return default(System.Management.Automation.CommandOrigin); } set { } }
    public System.Management.Automation.Language.IScriptExtent DisplayScriptPosition { get { return default(System.Management.Automation.Language.IScriptExtent); } set { } }
    public bool ExpectingInput { get { return default(bool); } set { } }
    public System.Int64 HistoryId { get { return default(System.Int64); } set { } }
    public string InvocationName { get { return default(string); } set { } }
    public string Line { get { return default(string); } }
    public System.Management.Automation.CommandInfo MyCommand { get { return default(System.Management.Automation.CommandInfo); } }
    public int OffsetInLine { get { return default(int); } }
    public int PipelineLength { get { return default(int); } set { } }
    public int PipelinePosition { get { return default(int); } set { } }
    public string PositionMessage { get { return default(string); } }
    public string PSCommandPath { get { return default(string); } }
    public string PSScriptRoot { get { return default(string); } }
    public int ScriptLineNumber { get { return default(int); } }
    public string ScriptName { get { return default(string); } }
    public System.Collections.Generic.List<object> UnboundArguments { get { return default(System.Collections.Generic.List<object>); } set { } }
    public static System.Management.Automation.InvocationInfo Create ( System.Management.Automation.CommandInfo commandInfo, System.Management.Automation.Language.IScriptExtent scriptPosition ) { return default(System.Management.Automation.InvocationInfo); }

  }

  public partial interface IResourceSupplier {
     string GetResourceString ( string baseName, string resourceId );

  }

  public sealed class ItemCmdletProviderIntrinsics {
    internal ItemCmdletProviderIntrinsics() { }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Clear ( string[] path, bool force, bool literalPath ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Clear ( string path ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Copy ( string[] path, string destinationPath, bool recurse, System.Management.Automation.CopyContainers copyContainers, bool force, bool literalPath ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Copy ( string path, string destinationPath, bool recurse, System.Management.Automation.CopyContainers copyContainers ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public bool Exists ( string path, bool force, bool literalPath ) { return default(bool); }
    public bool Exists ( string path ) { return default(bool); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Get ( string path ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Get ( string[] path, bool force, bool literalPath ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public void Invoke ( string path ) { }
    public void Invoke ( string[] path, bool literalPath ) { }
    public bool IsContainer ( string path ) { return default(bool); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Move ( string[] path, string destination, bool force, bool literalPath ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Move ( string path, string destination ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> New ( string[] path, string name, string itemTypeName, object content, bool force ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> New ( string path, string name, string itemTypeName, object content ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public void Remove ( string[] path, bool recurse, bool force, bool literalPath ) { }
    public void Remove ( string path, bool recurse ) { }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Rename ( string path, string newName, bool force ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Rename ( string path, string newName ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Set ( string[] path, object value, bool force, bool literalPath ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Set ( string path, object value ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }

  }

    [System.SerializableAttribute]
   public class ItemNotFoundException : System.Management.Automation.SessionStateException {
    public ItemNotFoundException() { }
    public ItemNotFoundException(string message) { }
    public ItemNotFoundException(string message, System.Exception innerException) { }
    protected ItemNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

  }

  public partial interface IValidateSetValuesGenerator {
     string[] GetValidValues (  );

  }

  public abstract class Job : System.IDisposable {
    protected Job() { }
    protected Job(string command) { }
    protected Job(string command, string name) { }
    protected Job(string command, string name, System.Collections.Generic.IList<System.Management.Automation.Job> childJobs) { }
    protected Job(string command, string name, System.Management.Automation.JobIdentifier token) { }
    protected Job(string command, string name, System.Guid instanceId) { }

    public event System.EventHandler<System.Management.Automation.JobStateEventArgs> StateChanged { add {} remove {}}

    public System.Collections.Generic.IList<System.Management.Automation.Job> ChildJobs { get { return default(System.Collections.Generic.IList<System.Management.Automation.Job>); } }
    public string Command { get { return default(string); } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.DebugRecord> Debug { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.DebugRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.ErrorRecord> Error { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.ErrorRecord>); } set { } }
    public System.Threading.WaitHandle Finished { get { return default(System.Threading.WaitHandle); } }
    public abstract bool HasMoreData { get; }
    public int Id { get { return default(int); } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.InformationRecord> Information { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.InformationRecord>); } set { } }
    public System.Guid InstanceId { get { return default(System.Guid); } }
    public System.Management.Automation.JobStateInfo JobStateInfo { get { return default(System.Management.Automation.JobStateInfo); } set { } }
    public abstract string Location { get; }
    public string Name { get { return default(string); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.PSObject> Output { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.PSObject>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.ProgressRecord> Progress { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.ProgressRecord>); } set { } }
    public System.Nullable<System.DateTime> PSBeginTime { get { return default(System.Nullable<System.DateTime>); } set { } }
    public System.Nullable<System.DateTime> PSEndTime { get { return default(System.Nullable<System.DateTime>); } set { } }
    public string PSJobTypeName { get { return default(string); } set { } }
    public abstract string StatusMessage { get; }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.VerboseRecord> Verbose { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.VerboseRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.WarningRecord> Warning { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.WarningRecord>); } set { } }
    public virtual void Dispose (  ) { }
    protected virtual void Dispose ( bool disposing ) { }
    protected virtual void DoLoadJobStreams (  ) { }
    protected virtual void DoUnloadJobStreams (  ) { }
    public void LoadJobStreams (  ) { }
    public virtual void StopJob (  ) { }
    public void UnloadJobStreams (  ) { }

  }

  public abstract class Job2 : System.Management.Automation.Job, System.IDisposable {
    protected Job2() { }
    protected Job2(string command) { }
    protected Job2(string command, string name) { }
    protected Job2(string command, string name, System.Collections.Generic.IList<System.Management.Automation.Job> childJobs) { }
    protected Job2(string command, string name, System.Management.Automation.JobIdentifier token) { }
    protected Job2(string command, string name, System.Guid instanceId) { }

    public event System.EventHandler<System.ComponentModel.AsyncCompletedEventArgs> ResumeJobCompleted { add { } remove { } }
    public event System.EventHandler<System.ComponentModel.AsyncCompletedEventArgs> StartJobCompleted { add { } remove { } }
    public event System.EventHandler<System.ComponentModel.AsyncCompletedEventArgs> StopJobCompleted { add { } remove { } }
    public event System.EventHandler<System.ComponentModel.AsyncCompletedEventArgs> SuspendJobCompleted { add { } remove { } }
    public event System.EventHandler<System.ComponentModel.AsyncCompletedEventArgs> UnblockJobCompleted { add { } remove { } }

    public System.Collections.Generic.List<System.Management.Automation.Runspaces.CommandParameterCollection> StartParameters { get { return default(System.Collections.Generic.List<System.Management.Automation.Runspaces.CommandParameterCollection>); } set { } }
    protected virtual void OnResumeJobCompleted ( System.ComponentModel.AsyncCompletedEventArgs eventArgs ) { }
    protected virtual void OnStartJobCompleted ( System.ComponentModel.AsyncCompletedEventArgs eventArgs ) { }
    protected virtual void OnStopJobCompleted ( System.ComponentModel.AsyncCompletedEventArgs eventArgs ) { }
    protected virtual void OnSuspendJobCompleted ( System.ComponentModel.AsyncCompletedEventArgs eventArgs ) { }
    protected virtual void OnUnblockJobCompleted ( System.ComponentModel.AsyncCompletedEventArgs eventArgs ) { }
    public virtual void ResumeJob (  ) { }
    public virtual void ResumeJobAsync (  ) { }
    public virtual void StartJob (  ) { }
    public virtual void StartJobAsync (  ) { }
    public virtual void StopJob ( bool force, string reason ) { }
    public virtual void StopJobAsync (  ) { }
    public virtual void StopJobAsync ( bool force, string reason ) { }
    public virtual void SuspendJob (  ) { }
    public virtual void SuspendJob ( bool force, string reason ) { }
    public virtual void SuspendJobAsync (  ) { }
    public virtual void SuspendJobAsync ( bool force, string reason ) { }
    public virtual void UnblockJob (  ) { }
    public virtual void UnblockJobAsync (  ) { }

  }

  public sealed class JobDataAddedEventArgs : System.EventArgs {
    internal JobDataAddedEventArgs() { }
    public System.Management.Automation.PowerShellStreamType DataType { get { return default(System.Management.Automation.PowerShellStreamType); } }
    public int Index { get { return default(int); } }
    public System.Management.Automation.Job SourceJob { get { return default(System.Management.Automation.Job); } }
  }

    [System.SerializableAttribute]
   public class JobDefinition {
    public JobDefinition(System.Type jobSourceAdapterType, string command, string name) { }
    protected JobDefinition(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

    public string Command { get { return default(string); } }
    public System.Management.Automation.CommandInfo CommandInfo { get { return default(System.Management.Automation.CommandInfo); } }
    public System.Guid InstanceId { get { return default(System.Guid); } set { } }
    public System.Type JobSourceAdapterType { get { return default(System.Type); } }
    public string JobSourceAdapterTypeName { get { return default(string); } set { } }
    public string ModuleName { get { return default(string); } set { } }
    public string Name { get { return default(string); } set { } }
    public virtual void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }
    public void Load ( System.IO.Stream stream ) { }
    public void Save ( System.IO.Stream stream ) { }

  }

    [System.SerializableAttribute]
   public class JobFailedException : System.SystemException {
    public JobFailedException() { }
    public JobFailedException(string message) { }
    public JobFailedException(string message, System.Exception innerException) { }
    public JobFailedException(System.Exception innerException, System.Management.Automation.Language.ScriptExtent displayScriptPosition) { }
    protected JobFailedException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }

    public System.Management.Automation.Language.ScriptExtent DisplayScriptPosition { get { return default(System.Management.Automation.Language.ScriptExtent); } }
    public override string Message { get { return default(string); } }
    public System.Exception Reason { get { return default(System.Exception); } }
    public override void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

  }

  public sealed class JobIdentifier {
    internal JobIdentifier() { }
  }

    [System.SerializableAttribute]
   public class JobInvocationInfo {
    protected JobInvocationInfo() { }
    public JobInvocationInfo(System.Management.Automation.JobDefinition definition, System.Collections.Generic.Dictionary<string, object> parameters) { }
    public JobInvocationInfo(System.Management.Automation.JobDefinition definition, System.Collections.Generic.IEnumerable<System.Collections.Generic.Dictionary<string, object>> parameterCollectionList) { }
    public JobInvocationInfo(System.Management.Automation.JobDefinition definition, System.Management.Automation.Runspaces.CommandParameterCollection parameters) { }
    public JobInvocationInfo(System.Management.Automation.JobDefinition definition, System.Collections.Generic.IEnumerable<System.Management.Automation.Runspaces.CommandParameterCollection> parameters) { }
    protected JobInvocationInfo(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

    public string Command { get { return default(string); } set { } }
    public System.Management.Automation.JobDefinition Definition { get { return default(System.Management.Automation.JobDefinition); } set { } }
    public System.Guid InstanceId { get { return default(System.Guid); } }
    public string Name { get { return default(string); } set { } }
    public System.Collections.Generic.List<System.Management.Automation.Runspaces.CommandParameterCollection> Parameters { get { return default(System.Collections.Generic.List<System.Management.Automation.Runspaces.CommandParameterCollection>); } }
    public virtual void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }
    public void Load ( System.IO.Stream stream ) { }
    public void Save ( System.IO.Stream stream ) { }

  }

  public sealed class JobManager {
    internal JobManager() { }
    public bool IsRegistered ( string typeName ) { return default(bool); }
    public System.Management.Automation.Job2 NewJob ( System.Management.Automation.JobInvocationInfo specification ) { return default(System.Management.Automation.Job2); }
    public System.Management.Automation.Job2 NewJob ( System.Management.Automation.JobDefinition definition ) { return default(System.Management.Automation.Job2); }
    public void PersistJob ( System.Management.Automation.Job2 job, System.Management.Automation.JobDefinition definition ) { }

  }

  public class JobRepository : System.Management.Automation.Repository<System.Management.Automation.Job> {
    internal JobRepository() : base (default(string)) { }
    public System.Collections.Generic.List<System.Management.Automation.Job> Jobs { get { return default(System.Collections.Generic.List<System.Management.Automation.Job>); } }
    public System.Management.Automation.Job GetJob ( System.Guid instanceId ) { return default(System.Management.Automation.Job); }
    protected override System.Guid GetKey ( System.Management.Automation.Job item ) { return default(System.Guid); }

  }

  public abstract class JobSourceAdapter {
    protected JobSourceAdapter() { }

    public string Name { get { return default(string); } set { } }
    public virtual System.Management.Automation.Job2 GetJobByInstanceId ( System.Guid instanceId, bool recurse ) { return default(System.Management.Automation.Job2); }
    public virtual System.Management.Automation.Job2 GetJobBySessionId ( int id, bool recurse ) { return default(System.Management.Automation.Job2); }
    public virtual System.Collections.Generic.IList<System.Management.Automation.Job2> GetJobs (  ) { return default(System.Collections.Generic.IList<System.Management.Automation.Job2>); }
    public virtual System.Collections.Generic.IList<System.Management.Automation.Job2> GetJobsByCommand ( string command, bool recurse ) { return default(System.Collections.Generic.IList<System.Management.Automation.Job2>); }
    public virtual System.Collections.Generic.IList<System.Management.Automation.Job2> GetJobsByFilter ( System.Collections.Generic.Dictionary<string, object> filter, bool recurse ) { return default(System.Collections.Generic.IList<System.Management.Automation.Job2>); }
    public virtual System.Collections.Generic.IList<System.Management.Automation.Job2> GetJobsByName ( string name, bool recurse ) { return default(System.Collections.Generic.IList<System.Management.Automation.Job2>); }
    public virtual System.Collections.Generic.IList<System.Management.Automation.Job2> GetJobsByState ( System.Management.Automation.JobState state, bool recurse ) { return default(System.Collections.Generic.IList<System.Management.Automation.Job2>); }
    public System.Management.Automation.Job2 NewJob ( System.Management.Automation.JobDefinition definition ) { return default(System.Management.Automation.Job2); }
    public virtual System.Management.Automation.Job2 NewJob ( string definitionName, string definitionPath ) { return default(System.Management.Automation.Job2); }
    public virtual System.Management.Automation.Job2 NewJob ( System.Management.Automation.JobInvocationInfo specification ) { return default(System.Management.Automation.Job2); }
    public virtual void PersistJob ( System.Management.Automation.Job2 job ) { }
    public virtual void RemoveJob ( System.Management.Automation.Job2 job ) { }
    public void StoreJobIdForReuse ( System.Management.Automation.Job2 job, bool recurse ) { }

  }

  public enum JobState {
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
    public override string ToString (  ) { return default(string); }

  }

  public enum JobThreadOptions {
    Default = 0,
    UseNewThread = 2,
    UseThreadPoolThread = 1,
  }

  public static class LanguagePrimitives {
    public static int Compare ( object first, object second ) { return default(int); }
    public static int Compare ( object first, object second, bool ignoreCase ) { return default(int); }
    public static int Compare ( object first, object second, bool ignoreCase, System.IFormatProvider formatProvider ) { return default(int); }
    public static object ConvertPSObjectToType ( System.Management.Automation.PSObject valueToConvert, System.Type resultType, bool recursion, System.IFormatProvider formatProvider, bool ignoreUnknownMembers ) { return default(object); }
    public static object ConvertTo ( object valueToConvert, System.Type resultType, System.IFormatProvider formatProvider ) { return default(object); }
    public static T ConvertTo<T> ( object valueToConvert ) { return default(T); }
    public static object ConvertTo ( object valueToConvert, System.Type resultType ) { return default(object); }
    public static string ConvertTypeNameToPSTypeName ( string typeName ) { return default(string); }
    public static bool Equals ( object first, object second, bool ignoreCase ) { return default(bool); }
    public new static bool Equals ( object first, object second ) { return default(bool); }
    public static bool Equals ( object first, object second, bool ignoreCase, System.IFormatProvider formatProvider ) { return default(bool); }
    public static System.Collections.IEnumerable GetEnumerable ( object obj ) { return default(System.Collections.IEnumerable); }
    public static System.Collections.IEnumerator GetEnumerator ( object obj ) { return default(System.Collections.IEnumerator); }
    public static System.Management.Automation.PSDataCollection<System.Management.Automation.PSObject> GetPSDataCollection ( object inputValue ) { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.PSObject>); }
    public static bool IsObjectEnumerable ( object obj ) { return default(bool); }
    public static bool IsTrue ( object obj ) { return default(bool); }
    public static bool TryConvertTo ( object valueToConvert, System.Type resultType, System.IFormatProvider formatProvider, out object result ) { result = default(object); return default(bool); }
    public static bool TryConvertTo ( object valueToConvert, System.Type resultType, out object result ) {result = default(object);  return default(bool); }
    public static bool TryConvertTo<T> ( object valueToConvert, System.IFormatProvider formatProvider, out T result ) { result = default(T); return default(bool); }
    public static bool TryConvertTo<T> ( object valueToConvert, out T result ) { result = default(T); return default(bool); }

  }

  public class LineBreakpoint : System.Management.Automation.Breakpoint {
    internal LineBreakpoint() { }
    public int Column { get { return default(int); } set { } }
    public int Line { get { return default(int); } set { } }
    public override string ToString (  ) { return default(string); }

  }

  public sealed class ListControl : System.Management.Automation.PSControl {
    public ListControl() { }
    public ListControl(System.Collections.Generic.IEnumerable<System.Management.Automation.ListControlEntry> entries) { }

    public System.Collections.Generic.List<System.Management.Automation.ListControlEntry> Entries { get { return default(System.Collections.Generic.List<System.Management.Automation.ListControlEntry>); } set { } }
    public static System.Management.Automation.ListControlBuilder Create ( bool outOfBand ) { return default(System.Management.Automation.ListControlBuilder); }

  }

  public class ListControlBuilder {
    public System.Management.Automation.ListControl EndList (  ) { return default(System.Management.Automation.ListControl); }
    public System.Management.Automation.ListControlBuilder GroupByProperty ( string property, System.Management.Automation.CustomControl customControl, string label ) { return default(System.Management.Automation.ListControlBuilder); }
    public System.Management.Automation.ListControlBuilder GroupByScriptBlock ( string scriptBlock, System.Management.Automation.CustomControl customControl, string label ) { return default(System.Management.Automation.ListControlBuilder); }
    public System.Management.Automation.ListEntryBuilder StartEntry ( System.Collections.Generic.IEnumerable<string> entrySelectedByType, System.Collections.Generic.IEnumerable<System.Management.Automation.DisplayEntry> entrySelectedByCondition ) { return default(System.Management.Automation.ListEntryBuilder); }

  }

  public sealed class ListControlEntry {
    public ListControlEntry() { }
    public ListControlEntry(System.Collections.Generic.IEnumerable<System.Management.Automation.ListControlEntryItem> listItems) { }
    public ListControlEntry(System.Collections.Generic.IEnumerable<System.Management.Automation.ListControlEntryItem> listItems, System.Collections.Generic.IEnumerable<string> selectedBy) { }

    public System.Management.Automation.EntrySelectedBy EntrySelectedBy { get { return default(System.Management.Automation.EntrySelectedBy); } set { } }
    public System.Collections.Generic.List<System.Management.Automation.ListControlEntryItem> Items { get { return default(System.Collections.Generic.List<System.Management.Automation.ListControlEntryItem>); } set { } }
    public System.Collections.Generic.List<string> SelectedBy { get { return default(System.Collections.Generic.List<string>); } }
  }

  public sealed class ListControlEntryItem {
    public ListControlEntryItem(string label, System.Management.Automation.DisplayEntry entry) { }

    public System.Management.Automation.DisplayEntry DisplayEntry { get { return default(System.Management.Automation.DisplayEntry); } set { } }
    public string FormatString { get { return default(string); } set { } }
    public System.Management.Automation.DisplayEntry ItemSelectionCondition { get { return default(System.Management.Automation.DisplayEntry); } set { } }
    public string Label { get { return default(string); } set { } }
  }

  public class ListEntryBuilder {
    public System.Management.Automation.ListEntryBuilder AddItemProperty ( string property, string label, string format ) { return default(System.Management.Automation.ListEntryBuilder); }
    public System.Management.Automation.ListEntryBuilder AddItemScriptBlock ( string scriptBlock, string label, string format ) { return default(System.Management.Automation.ListEntryBuilder); }
    public System.Management.Automation.ListControlBuilder EndEntry (  ) { return default(System.Management.Automation.ListControlBuilder); }

  }

  public abstract class LoopFlowException : System.Management.Automation.FlowControlException {
    public string Label { get { return default(string); } set { } }
  }

    [System.SerializableAttribute]
   public class MetadataException : System.Management.Automation.RuntimeException {
    protected MetadataException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public MetadataException() { }
    public MetadataException(string message) { }
    public MetadataException(string message, System.Exception innerException) { }

  }

    [System.SerializableAttribute]
   public class MethodException : System.Management.Automation.ExtendedTypeSystemException {
    public MethodException() { }
    public MethodException(string message) { }
    public MethodException(string message, System.Exception innerException) { }
    protected MethodException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

  }

    [System.SerializableAttribute]
   public class MethodInvocationException : System.Management.Automation.MethodException {
    public MethodInvocationException() { }
    public MethodInvocationException(string message) { }
    public MethodInvocationException(string message, System.Exception innerException) { }
    protected MethodInvocationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

  }

  public enum ModuleAccessMode {
    Constant = 2,
    ReadOnly = 1,
    ReadWrite = 0,
  }

  public class ModuleIntrinsics {
    public static string GetModulePath ( string currentProcessModulePath, string hklmMachineModulePath, string hkcuUserModulePath ) { return default(string); }

  }

  public enum ModuleType {
    Binary = 1,
    Cim = 3,
    Manifest = 2,
    Script = 0,
    Workflow = 4,
  }

   [System.AttributeUsageAttribute((System.AttributeTargets)4, AllowMultiple = true)]
   public sealed class OutputTypeAttribute : System.Management.Automation.Internal.CmdletMetadataAttribute {
    public OutputTypeAttribute(System.Type[] type) { }
    public OutputTypeAttribute(string[] type) { }

    public string[] ParameterSetName { get { return default(string[]); } set { } }
    public string ProviderCmdlet { get { return default(string); } set { } }
    public System.Management.Automation.PSTypeName[] Type { get { return default(System.Management.Automation.PSTypeName[]); } set { } }
  }

  public sealed class PagingParameters {
    internal PagingParameters() { }
    [System.Management.Automation.ParameterAttribute]
    public System.UInt64 First { get { return default(System.UInt64); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter IncludeTotalCount { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute]
    public System.UInt64 Skip { get { return default(System.UInt64); } set { } }

    public System.Management.Automation.PSObject NewTotalCount ( System.UInt64 totalCount, System.Double accuracy ) { return default(System.Management.Automation.PSObject); }

  }

   [System.AttributeUsageAttribute((System.AttributeTargets)384, AllowMultiple = true)]
   public sealed class ParameterAttribute : System.Management.Automation.Internal.ParsingBaseAttribute {
    public ParameterAttribute() { }

    public const string AllParameterSets = "__AllParameterSets";
    public bool DontShow { get { return default(bool); } set { } }
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

    [System.SerializableAttribute]
   public class ParameterBindingException : System.Management.Automation.RuntimeException {
    protected ParameterBindingException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public ParameterBindingException() { }
    public ParameterBindingException(string message) { }
    public ParameterBindingException(string message, System.Exception innerException) { }

    public System.Management.Automation.InvocationInfo CommandInvocation { get { return default(System.Management.Automation.InvocationInfo); } }
    public string ErrorId { get { return default(string); } }
    public System.Int64 Line { get { return default(System.Int64); } }
    public override string Message { get { return default(string); } }
    public System.Int64 Offset { get { return default(System.Int64); } }
    public string ParameterName { get { return default(string); } }
    public System.Type ParameterType { get { return default(System.Type); } }
    public System.Type TypeSpecified { get { return default(System.Type); } }
    public override void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

  }

  public sealed class ParameterMetadata {
    public ParameterMetadata(string name) { }
    public ParameterMetadata(string name, System.Type parameterType) { }
    public ParameterMetadata(System.Management.Automation.ParameterMetadata other) { }

    public System.Collections.ObjectModel.Collection<string> Aliases { get { return default(System.Collections.ObjectModel.Collection<string>); } }
    public System.Collections.ObjectModel.Collection<System.Attribute> Attributes { get { return default(System.Collections.ObjectModel.Collection<System.Attribute>); } }
    public bool IsDynamic { get { return default(bool); } set { } }
    public string Name { get { return default(string); } set { } }
    public System.Collections.Generic.Dictionary<string, System.Management.Automation.ParameterSetMetadata> ParameterSets { get { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.ParameterSetMetadata>); } }
    public System.Type ParameterType { get { return default(System.Type); } set { } }
    public bool SwitchParameter { get { return default(bool); } }
    public static System.Collections.Generic.Dictionary<System.String,System.Management.Automation.ParameterMetadata> GetParameterMetadata ( System.Type type ) { return default(System.Collections.Generic.Dictionary<System.String,System.Management.Automation.ParameterMetadata>); }

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

    [System.SerializableAttribute]
   public class ParentContainsErrorRecordException : System.SystemException {
    public ParentContainsErrorRecordException(System.Exception wrapperException) { }
    public ParentContainsErrorRecordException(string message) { }
    public ParentContainsErrorRecordException() { }
    public ParentContainsErrorRecordException(string message, System.Exception innerException) { }
    protected ParentContainsErrorRecordException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

    public override string Message { get { return default(string); } }
    public override void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

  }

    [System.SerializableAttribute]
   public class ParseException : System.Management.Automation.RuntimeException {
    protected ParseException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public ParseException() { }
    public ParseException(string message) { }
    public ParseException(string message, System.Exception innerException) { }
    public ParseException(System.Management.Automation.Language.ParseError[] errors) { }

    public System.Management.Automation.Language.ParseError[] Errors { get { return default(System.Management.Automation.Language.ParseError[]); } }
    public override string Message { get { return default(string); } }
    public override void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

  }

    [System.SerializableAttribute]
   public class ParsingMetadataException : System.Management.Automation.MetadataException {
    protected ParsingMetadataException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
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
    public override string ToString (  ) { return default(string); }

  }

  public sealed class PathInfoStack : System.Collections.Generic.Stack<System.Management.Automation.PathInfo> {
    internal PathInfoStack() { }
    public string Name { get { return default(string); } }
  }

  public sealed class PathIntrinsics {
    internal PathIntrinsics() { }
    public System.Management.Automation.PathInfo CurrentFileSystemLocation { get { return default(System.Management.Automation.PathInfo); } }
    public System.Management.Automation.PathInfo CurrentLocation { get { return default(System.Management.Automation.PathInfo); } }
    public string Combine ( string parent, string child ) { return default(string); }
    public System.Management.Automation.PathInfo CurrentProviderLocation ( string providerName ) { return default(System.Management.Automation.PathInfo); }
    public System.Collections.ObjectModel.Collection<System.String> GetResolvedProviderPathFromProviderPath ( string path, string providerId ) { return default(System.Collections.ObjectModel.Collection<System.String>); }
    public System.Collections.ObjectModel.Collection<System.String> GetResolvedProviderPathFromPSPath ( string path, out System.Management.Automation.ProviderInfo provider ) { provider = default(System.Management.Automation.ProviderInfo); return default(System.Collections.ObjectModel.Collection<System.String>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PathInfo> GetResolvedPSPathFromPSPath ( string path ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PathInfo>); }
    public string GetUnresolvedProviderPathFromPSPath ( string path, out System.Management.Automation.ProviderInfo provider, out System.Management.Automation.PSDriveInfo drive ) { provider = default(System.Management.Automation.ProviderInfo); drive = default(System.Management.Automation.PSDriveInfo); return default(string); }
    public string GetUnresolvedProviderPathFromPSPath ( string path ) { return default(string); }
    public bool IsProviderQualified ( string path ) { return default(bool); }
    public bool IsPSAbsolute ( string path, out string driveName ) { driveName = default(string); return default(bool); }
    public bool IsValid ( string path ) { return default(bool); }
    public System.Management.Automation.PathInfoStack LocationStack ( string stackName ) { return default(System.Management.Automation.PathInfoStack); }
    public string NormalizeRelativePath ( string path, string basePath ) { return default(string); }
    public string ParseChildName ( string path ) { return default(string); }
    public string ParseParent ( string path, string root ) { return default(string); }
    public System.Management.Automation.PathInfo PopLocation ( string stackName ) { return default(System.Management.Automation.PathInfo); }
    public void PushCurrentLocation ( string stackName ) { }
    public System.Management.Automation.PathInfoStack SetDefaultLocationStack ( string stackName ) { return default(System.Management.Automation.PathInfoStack); }
    public System.Management.Automation.PathInfo SetLocation ( string path ) { return default(System.Management.Automation.PathInfo); }

  }

    [System.SerializableAttribute]
   public class PipelineClosedException : System.Management.Automation.RuntimeException {
    public PipelineClosedException() { }
    public PipelineClosedException(string message) { }
    public PipelineClosedException(string message, System.Exception innerException) { }
    protected PipelineClosedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

  }

    [System.SerializableAttribute]
   public class PipelineDepthException : System.SystemException, System.Management.Automation.IContainsErrorRecord {
    public PipelineDepthException() { }
    public PipelineDepthException(string message) { }
    public PipelineDepthException(string message, System.Exception innerException) { }
    protected PipelineDepthException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

    public int CallDepth { get { return default(int); } }
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

  }

    [System.SerializableAttribute]
   public class PipelineStoppedException : System.Management.Automation.RuntimeException {
    public PipelineStoppedException() { }
    protected PipelineStoppedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public PipelineStoppedException(string message) { }
    public PipelineStoppedException(string message, System.Exception innerException) { }

  }

  public static class Platform {
    public static bool IsCoreCLR { get { return default(bool); } }
    public static bool IsIoT { get { return default(bool); } }
    public static bool IsLinux { get { return default(bool); } }
    public static bool IsMacOS { get { return default(bool); } }
    public static bool IsNanoServer { get { return default(bool); } }
    public static bool IsWindows { get { return default(bool); } }
    public static bool IsWindowsDesktop { get { return default(bool); } }
    public static string SelectProductNameForDirectory ( System.Management.Automation.Platform.XDG_Type dirpath ) { return default(string); }
    public class XDG_Type { }

  }

  public sealed class PowerShell : System.IDisposable {
    internal PowerShell() { }
    public event System.EventHandler<System.Management.Automation.PSInvocationStateChangedEventArgs> InvocationStateChanged { add { } remove { } }

    public System.Management.Automation.PSCommand Commands { get { return default(System.Management.Automation.PSCommand); } set { } }
    public bool HadErrors { get { return default(bool); } set { } }
    public string HistoryString { get { return default(string); } set { } }
    public System.Guid InstanceId { get { return default(System.Guid); } set { } }
    public System.Management.Automation.PSInvocationStateInfo InvocationStateInfo { get { return default(System.Management.Automation.PSInvocationStateInfo); } set { } }
    public bool IsNested { get { return default(bool); } set { } }
    public bool IsRunspaceOwner { get { return default(bool); } set { } }
    public System.Management.Automation.Runspaces.Runspace Runspace { get { return default(System.Management.Automation.Runspaces.Runspace); } set { } }
    public System.Management.Automation.Runspaces.RunspacePool RunspacePool { get { return default(System.Management.Automation.Runspaces.RunspacePool); } set { } }
    public System.Management.Automation.PSDataStreams Streams { get { return default(System.Management.Automation.PSDataStreams); } }
    public System.Management.Automation.PowerShell AddArgument ( object value ) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell AddCommand ( string cmdlet, bool useLocalScope ) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell AddCommand ( string cmdlet ) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell AddCommand ( System.Management.Automation.CommandInfo commandInfo ) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell AddParameter ( string parameterName, object value ) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell AddParameter ( string parameterName ) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell AddParameters ( System.Collections.IList parameters ) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell AddParameters ( System.Collections.IDictionary parameters ) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell AddScript ( string script, bool useLocalScope ) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell AddScript ( string script ) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell AddStatement (  ) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PSJobProxy AsJobProxy (  ) { return default(System.Management.Automation.PSJobProxy); }
    public System.IAsyncResult BeginInvoke (  ) { return default(System.IAsyncResult); }
    public System.IAsyncResult BeginInvoke<T> ( System.Management.Automation.PSDataCollection<T> input, System.Management.Automation.PSInvocationSettings settings, System.AsyncCallback callback, object state ) { return default(System.IAsyncResult); }
    public System.IAsyncResult BeginInvoke<TInput, TOutput> ( System.Management.Automation.PSDataCollection<TInput> input, System.Management.Automation.PSDataCollection<TOutput> output ) { return default(System.IAsyncResult); }
    public System.IAsyncResult BeginInvoke<TInput, TOutput> ( System.Management.Automation.PSDataCollection<TInput> input, System.Management.Automation.PSDataCollection<TOutput> output, System.Management.Automation.PSInvocationSettings settings, System.AsyncCallback callback, object state ) { return default(System.IAsyncResult); }
    public System.IAsyncResult BeginInvoke<T> ( System.Management.Automation.PSDataCollection<T> input ) { return default(System.IAsyncResult); }
    public System.IAsyncResult BeginStop ( System.AsyncCallback callback, object state ) { return default(System.IAsyncResult); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Connect (  ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.IAsyncResult ConnectAsync (  ) { return default(System.IAsyncResult); }
    public System.IAsyncResult ConnectAsync ( System.Management.Automation.PSDataCollection<System.Management.Automation.PSObject> output, System.AsyncCallback invocationCallback, object state ) { return default(System.IAsyncResult); }
    public static System.Management.Automation.PowerShell Create ( System.Management.Automation.Runspaces.InitialSessionState initialSessionState ) { return default(System.Management.Automation.PowerShell); }
    public static System.Management.Automation.PowerShell Create (  ) { return default(System.Management.Automation.PowerShell); }
    public static System.Management.Automation.PowerShell Create ( System.Management.Automation.RunspaceMode runspace ) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell CreateNestedPowerShell (  ) { return default(System.Management.Automation.PowerShell); }
    public void Dispose (  ) { }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.PSObject> EndInvoke ( System.IAsyncResult asyncResult ) { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.PSObject>); }
    public void EndStop ( System.IAsyncResult asyncResult ) { }
    public System.Collections.ObjectModel.Collection<T> Invoke<T> ( System.Collections.IEnumerable input, System.Management.Automation.PSInvocationSettings settings ) { return default(System.Collections.ObjectModel.Collection<T>); }
    public System.Collections.ObjectModel.Collection<T> Invoke<T> (  ) { return default(System.Collections.ObjectModel.Collection<T>); }
    public void Invoke<T> ( System.Collections.IEnumerable input, System.Collections.Generic.IList<T> output ) { }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Invoke ( System.Collections.IEnumerable input, System.Management.Automation.PSInvocationSettings settings ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public void Invoke<T> ( System.Collections.IEnumerable input, System.Collections.Generic.IList<T> output, System.Management.Automation.PSInvocationSettings settings ) { }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Invoke ( System.Collections.IEnumerable input ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<T> Invoke<T> ( System.Collections.IEnumerable input ) { return default(System.Collections.ObjectModel.Collection<T>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Invoke (  ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public void Invoke<TInput,TOutput> ( System.Management.Automation.PSDataCollection<TInput> input, System.Management.Automation.PSDataCollection<TOutput> output, System.Management.Automation.PSInvocationSettings settings ) { }
    public void Stop (  ) { }

  }

  public class PowerShellAssemblyLoadContextInitializer {
    public PowerShellAssemblyLoadContextInitializer() { }

    public static void SetPowerShellAssemblyLoadContext ( string basePaths ) { }

  }

  public sealed class PowerShellStreams<TInput,TOutput> : System.IDisposable {
    public PowerShellStreams() { }
    public PowerShellStreams(System.Management.Automation.PSDataCollection<TInput> pipelineInput) { }

    public System.Management.Automation.PSDataCollection<System.Management.Automation.DebugRecord> DebugStream { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.DebugRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.ErrorRecord> ErrorStream { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.ErrorRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.InformationRecord> InformationStream { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.InformationRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<TInput> InputStream { get { return default(System.Management.Automation.PSDataCollection<TInput>); } set { } }
    public System.Management.Automation.PSDataCollection<TOutput> OutputStream { get { return default(System.Management.Automation.PSDataCollection<TOutput>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.ProgressRecord> ProgressStream { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.ProgressRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.VerboseRecord> VerboseStream { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.VerboseRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.WarningRecord> WarningStream { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.WarningRecord>); } set { } }
    public void CloseAll (  ) { }
    public void Dispose (  ) { }

  }

  public enum PowerShellStreamType {
    Debug = 5,
    Error = 2,
    Information = 7,
    Input = 0,
    Output = 1,
    Progress = 6,
    Verbose = 4,
    Warning = 3,
  }

  public sealed class ProcessRunspaceDebugEndEventArgs : System.EventArgs {
    public ProcessRunspaceDebugEndEventArgs(System.Management.Automation.Runspaces.Runspace runspace) { }

    public System.Management.Automation.Runspaces.Runspace Runspace { get { return default(System.Management.Automation.Runspaces.Runspace); } set { } }
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
    public override string ToString (  ) { return default(string); }

  }

  public enum ProgressRecordType {
    Completed = 1,
    Processing = 0,
  }

  public sealed class PropertyCmdletProviderIntrinsics {
    internal PropertyCmdletProviderIntrinsics() { }
    public void Clear ( string path, System.Collections.ObjectModel.Collection<string> propertyToClear ) { }
    public void Clear ( string[] path, System.Collections.ObjectModel.Collection<string> propertyToClear, bool force, bool literalPath ) { }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Copy ( string[] sourcePath, string sourceProperty, string destinationPath, string destinationProperty, bool force, bool literalPath ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Copy ( string sourcePath, string sourceProperty, string destinationPath, string destinationProperty ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Get ( string path, System.Collections.ObjectModel.Collection<string> providerSpecificPickList ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Get ( string[] path, System.Collections.ObjectModel.Collection<string> providerSpecificPickList, bool literalPath ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Move ( string[] sourcePath, string sourceProperty, string destinationPath, string destinationProperty, bool force, bool literalPath ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Move ( string sourcePath, string sourceProperty, string destinationPath, string destinationProperty ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> New ( string[] path, string propertyName, string propertyTypeName, object value, bool force, bool literalPath ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> New ( string path, string propertyName, string propertyTypeName, object value ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public void Remove ( string path, string propertyName ) { }
    public void Remove ( string[] path, string propertyName, bool force, bool literalPath ) { }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Rename ( string path, string sourceProperty, string destinationProperty ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Rename ( string[] path, string sourceProperty, string destinationProperty, bool force, bool literalPath ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Set ( string[] path, System.Management.Automation.PSObject propertyValue, bool force, bool literalPath ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Set ( string path, System.Management.Automation.PSObject propertyValue ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }

  }

    [System.SerializableAttribute]
   public class PropertyNotFoundException : System.Management.Automation.ExtendedTypeSystemException {
    public PropertyNotFoundException() { }
    public PropertyNotFoundException(string message) { }
    public PropertyNotFoundException(string message, System.Exception innerException) { }
    protected PropertyNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

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
    public System.Management.Automation.PSModuleInfo Module { get { return default(System.Management.Automation.PSModuleInfo); } set { } }
    public string ModuleName { get { return default(string); } }
    public string Name { get { return default(string); } }
    public System.Management.Automation.PSSnapInInfo PSSnapIn { get { return default(System.Management.Automation.PSSnapInInfo); } }
    public bool VolumeSeparatedByColon { get { return default(bool); } set { } }
    public override string ToString (  ) { return default(string); }

  }

  public sealed class ProviderIntrinsics {
    internal ProviderIntrinsics() { }
    public System.Management.Automation.ChildItemCmdletProviderIntrinsics ChildItem { get { return default(System.Management.Automation.ChildItemCmdletProviderIntrinsics); } }
    public System.Management.Automation.ContentCmdletProviderIntrinsics Content { get { return default(System.Management.Automation.ContentCmdletProviderIntrinsics); } }
    public System.Management.Automation.ItemCmdletProviderIntrinsics Item { get { return default(System.Management.Automation.ItemCmdletProviderIntrinsics); } }
    public System.Management.Automation.PropertyCmdletProviderIntrinsics Property { get { return default(System.Management.Automation.PropertyCmdletProviderIntrinsics); } }
    public System.Management.Automation.SecurityDescriptorCmdletProviderIntrinsics SecurityDescriptor { get { return default(System.Management.Automation.SecurityDescriptorCmdletProviderIntrinsics); } }
  }

    [System.SerializableAttribute]
   public class ProviderInvocationException : System.Management.Automation.RuntimeException {
    public ProviderInvocationException() { }
    protected ProviderInvocationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public ProviderInvocationException(string message) { }
    public ProviderInvocationException(string message, System.Exception innerException) { }

    public override System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override string Message { get { return default(string); } }
    public System.Management.Automation.ProviderInfo ProviderInfo { get { return default(System.Management.Automation.ProviderInfo); } }
  }

    [System.SerializableAttribute]
   public class ProviderNameAmbiguousException : System.Management.Automation.ProviderNotFoundException {
    public ProviderNameAmbiguousException() { }
    public ProviderNameAmbiguousException(string message) { }
    public ProviderNameAmbiguousException(string message, System.Exception innerException) { }
    protected ProviderNameAmbiguousException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.ProviderInfo> PossibleMatches { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.ProviderInfo>); } }
  }

    [System.SerializableAttribute]
   public class ProviderNotFoundException : System.Management.Automation.SessionStateException {
    public ProviderNotFoundException() { }
    public ProviderNotFoundException(string message) { }
    public ProviderNotFoundException(string message, System.Exception innerException) { }
    protected ProviderNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

  }

  public sealed class ProxyCommand {
    internal ProxyCommand() { }
    public static string Create ( System.Management.Automation.CommandMetadata commandMetadata ) { return default(string); }
    public static string Create ( System.Management.Automation.CommandMetadata commandMetadata, string helpComment ) { return default(string); }
    public static string Create ( System.Management.Automation.CommandMetadata commandMetadata, string helpComment, bool generateDynamicParameters ) { return default(string); }
    public static string GetBegin ( System.Management.Automation.CommandMetadata commandMetadata ) { return default(string); }
    public static string GetCmdletBindingAttribute ( System.Management.Automation.CommandMetadata commandMetadata ) { return default(string); }
    public static string GetDynamicParam ( System.Management.Automation.CommandMetadata commandMetadata ) { return default(string); }
    public static string GetEnd ( System.Management.Automation.CommandMetadata commandMetadata ) { return default(string); }
    public static string GetHelpComments ( System.Management.Automation.PSObject help ) { return default(string); }
    public static string GetParamBlock ( System.Management.Automation.CommandMetadata commandMetadata ) { return default(string); }
    public static string GetProcess ( System.Management.Automation.CommandMetadata commandMetadata ) { return default(string); }

  }

  public class PSAdaptedProperty : System.Management.Automation.PSProperty {
    public PSAdaptedProperty(string name, object tag) { }

    public object BaseObject { get { return default(object); } }
    public object Tag { get { return default(object); } }
    public override System.Management.Automation.PSMemberInfo Copy (  ) { return default(System.Management.Automation.PSMemberInfo); }

  }

  public class PSAliasProperty : System.Management.Automation.PSPropertyInfo {
    public PSAliasProperty(string name, string referencedMemberName) { }
    public PSAliasProperty(string name, string referencedMemberName, System.Type conversionType) { }

    public System.Type ConversionType { get { return default(System.Type); } set { } }
    public override bool IsGettable { get { return default(bool); } }
    public override bool IsSettable { get { return default(bool); } }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public string ReferencedMemberName { get { return default(string); } }
    public override string TypeNameOfValue { get { return default(string); } }
    public override object Value { get { return default(object); } }
    public override System.Management.Automation.PSMemberInfo Copy (  ) { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString (  ) { return default(string); }

  }

    [System.SerializableAttribute]
   public class PSArgumentException : System.ArgumentException, System.Management.Automation.IContainsErrorRecord {
    public PSArgumentException() { }
    public PSArgumentException(string message) { }
    public PSArgumentException(string message, string paramName) { }
    protected PSArgumentException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public PSArgumentException(string message, System.Exception innerException) { }

    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override string Message { get { return default(string); } }
    public override void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

  }

    [System.SerializableAttribute]
   public class PSArgumentNullException : System.ArgumentNullException, System.Management.Automation.IContainsErrorRecord {
    public PSArgumentNullException() { }
    public PSArgumentNullException(string paramName) { }
    public PSArgumentNullException(string message, System.Exception innerException) { }
    public PSArgumentNullException(string paramName, string message) { }
    protected PSArgumentNullException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override string Message { get { return default(string); } }
    public override void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

  }

    [System.SerializableAttribute]
   public class PSArgumentOutOfRangeException : System.ArgumentOutOfRangeException, System.Management.Automation.IContainsErrorRecord {
    public PSArgumentOutOfRangeException() { }
    public PSArgumentOutOfRangeException(string paramName) { }
    public PSArgumentOutOfRangeException(string paramName, object actualValue, string message) { }
    protected PSArgumentOutOfRangeException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public PSArgumentOutOfRangeException(string message, System.Exception innerException) { }

    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

  }

  public sealed class PSChildJobProxy : System.Management.Automation.Job2, System.IDisposable {
    internal PSChildJobProxy() { }
    public event System.EventHandler<System.Management.Automation.JobDataAddedEventArgs> JobDataAdded { add { } remove { } }

    public override bool HasMoreData { get { return default(bool); } }
    public override string Location { get { return default(string); } }
    public override string StatusMessage { get { return default(string); } }
    protected override void Dispose ( bool disposing ) { }
    public override void ResumeJob (  ) { }
    public override void ResumeJobAsync (  ) { }
    public override void StartJob (  ) { }
    public override void StartJobAsync (  ) { }
    public override void StopJob ( bool force, string reason ) { }
    public override void StopJob (  ) { }
    public override void StopJobAsync (  ) { }
    public override void StopJobAsync ( bool force, string reason ) { }
    public override void SuspendJob (  ) { }
    public override void SuspendJob ( bool force, string reason ) { }
    public override void SuspendJobAsync (  ) { }
    public override void SuspendJobAsync ( bool force, string reason ) { }
    public override void UnblockJob (  ) { }
    public override void UnblockJobAsync (  ) { }

  }

  public sealed class PSClassInfo {
    public string HelpFile { get { return default(string); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSClassMemberInfo> Members { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSClassMemberInfo>); } set { } }
    public System.Management.Automation.PSModuleInfo Module { get { return default(System.Management.Automation.PSModuleInfo); } set { } }
    public string Name { get { return default(string); } set { } }
    public void UpdateMembers ( System.Collections.Generic.IList<System.Management.Automation.PSClassMemberInfo> members ) { }

  }

  public sealed class PSClassMemberInfo {
    public string DefaultValue { get { return default(string); } set { } }
    public string Name { get { return default(string); } set { } }
    public string TypeName { get { return default(string); } set { } }
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
    public System.Management.Automation.PathInfo CurrentProviderLocation ( string providerId ) { return default(System.Management.Automation.PathInfo); }
    public System.Collections.ObjectModel.Collection<System.String> GetResolvedProviderPathFromPSPath ( string path, out System.Management.Automation.ProviderInfo provider ) { provider = default(System.Management.Automation.ProviderInfo); return default(System.Collections.ObjectModel.Collection<System.String>); }
    public string GetUnresolvedProviderPathFromPSPath ( string path ) { return default(string); }
    public object GetVariableValue ( string name ) { return default(object); }
    public object GetVariableValue ( string name, object defaultValue ) { return default(object); }

  }

  public class PSCodeMethod : System.Management.Automation.PSMethodInfo {
    public PSCodeMethod(string name, System.Reflection.MethodInfo codeReference) { }

    public System.Reflection.MethodInfo CodeReference { get { return default(System.Reflection.MethodInfo); } set { } }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public override System.Collections.ObjectModel.Collection<string> OverloadDefinitions { get { return default(System.Collections.ObjectModel.Collection<string>); } }
    public override string TypeNameOfValue { get { return default(string); } }
    public override System.Management.Automation.PSMemberInfo Copy (  ) { return default(System.Management.Automation.PSMemberInfo); }
    public override object Invoke ( object[] arguments ) { return default(object); }
    public override string ToString (  ) { return default(string); }

  }

  public class PSCodeProperty : System.Management.Automation.PSPropertyInfo {
    public PSCodeProperty(string name, System.Reflection.MethodInfo getterCodeReference) { }
    public PSCodeProperty(string name, System.Reflection.MethodInfo getterCodeReference, System.Reflection.MethodInfo setterCodeReference) { }

    public System.Reflection.MethodInfo GetterCodeReference { get { return default(System.Reflection.MethodInfo); } set { } }
    public override bool IsGettable { get { return default(bool); } }
    public override bool IsSettable { get { return default(bool); } }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public System.Reflection.MethodInfo SetterCodeReference { get { return default(System.Reflection.MethodInfo); } set { } }
    public override string TypeNameOfValue { get { return default(string); } }
    public override object Value { get { return default(object); } }
    public override System.Management.Automation.PSMemberInfo Copy (  ) { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString (  ) { return default(string); }

  }

  public sealed class PSCommand {
    public PSCommand() { }

    public System.Management.Automation.Runspaces.CommandCollection Commands { get { return default(System.Management.Automation.Runspaces.CommandCollection); } }
    public System.Management.Automation.PSCommand AddArgument ( object value ) { return default(System.Management.Automation.PSCommand); }
    public System.Management.Automation.PSCommand AddCommand ( string command ) { return default(System.Management.Automation.PSCommand); }
    public System.Management.Automation.PSCommand AddCommand ( string cmdlet, bool useLocalScope ) { return default(System.Management.Automation.PSCommand); }
    public System.Management.Automation.PSCommand AddCommand ( System.Management.Automation.Runspaces.Command command ) { return default(System.Management.Automation.PSCommand); }
    public System.Management.Automation.PSCommand AddParameter ( string parameterName, object value ) { return default(System.Management.Automation.PSCommand); }
    public System.Management.Automation.PSCommand AddParameter ( string parameterName ) { return default(System.Management.Automation.PSCommand); }
    public System.Management.Automation.PSCommand AddScript ( string script ) { return default(System.Management.Automation.PSCommand); }
    public System.Management.Automation.PSCommand AddScript ( string script, bool useLocalScope ) { return default(System.Management.Automation.PSCommand); }
    public System.Management.Automation.PSCommand AddStatement (  ) { return default(System.Management.Automation.PSCommand); }
    public void Clear (  ) { }
    public System.Management.Automation.PSCommand Clone (  ) { return default(System.Management.Automation.PSCommand); }

  }

  public abstract class PSControl {
    protected PSControl() { }

    public System.Management.Automation.PSControlGroupBy GroupBy { get { return default(System.Management.Automation.PSControlGroupBy); } set { } }
    public bool OutOfBand { get { return default(bool); } set { } }
  }

  public sealed class PSControlGroupBy {
    public PSControlGroupBy() { }

    public System.Management.Automation.CustomControl CustomControl { get { return default(System.Management.Automation.CustomControl); } set { } }
    public System.Management.Automation.DisplayEntry Expression { get { return default(System.Management.Automation.DisplayEntry); } set { } }
    public string Label { get { return default(string); } set { } }
  }

    [System.SerializableAttribute]
   public sealed class PSCredential : System.Runtime.Serialization.ISerializable {
    public PSCredential(string userName, System.Security.SecureString password) { }
    public PSCredential(System.Management.Automation.PSObject pso) { }

    public System.Management.Automation.PSCredential Empty { get { return default(System.Management.Automation.PSCredential); } }
    public System.Management.Automation.GetSymmetricEncryptionKey GetSymmetricEncryptionKeyDelegate { get { return default(System.Management.Automation.GetSymmetricEncryptionKey); } set { } }
    public System.Security.SecureString Password { get { return default(System.Security.SecureString); } }
    public string UserName { get { return default(string); } }
    public System.Net.NetworkCredential GetNetworkCredential (  ) { return default(System.Net.NetworkCredential); }
    public void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }
    public static explicit operator System.Net.NetworkCredential ( System.Management.Automation.PSCredential credential ) { return default(System.Net.NetworkCredential); }

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
    public override string ToString (  ) { return default(string); }

  }

    [System.SerializableAttribute]
   public class PSDataCollection<T> : System.Collections.Generic.IList<T>, System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Collections.IList, System.Collections.ICollection, System.IDisposable, System.Runtime.Serialization.ISerializable {
    public PSDataCollection() { }
    public PSDataCollection(System.Collections.Generic.IEnumerable<T> items) { }
    public PSDataCollection(int capacity) { }
    protected PSDataCollection(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

    public event System.EventHandler Completed { add { } remove { } }
    public event System.EventHandler<System.Management.Automation.DataAddedEventArgs> DataAdded { add { } remove { } }
    public event System.EventHandler<System.Management.Automation.DataAddingEventArgs> DataAdding { add { } remove { } }
    public event System.EventHandler<System.EventArgs> IdleEvent { add { } remove { } }

    public bool BlockingEnumerator { get { return default(bool); } set { } }
    public int Count { get { return default(int); } }
    public int DataAddedCount { get { return default(int); } set { } }
    public bool EnumeratorNeverBlocks { get { return default(bool); } set { } }
    public bool IsAutoGenerated { get { return default(bool); } set { } }
    public bool IsOpen { get { return default(bool); } }
    public bool IsReadOnly { get { return default(bool); } }
    object System.Collections.IList.this[int index] { get { return default(object); } set { } }
    public T this[int index] { get { return default(T); } set { } }
    public bool SerializeInput { get { return default(bool); } set { } }
    bool System.Collections.IList.IsFixedSize { get { return default(bool); } }
    bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
    object System.Collections.ICollection.SyncRoot { get { return default(object); } }

    public void Add ( T item ) { }
    public int Add ( object item ) { return default(int); }
    public void Clear (  ) { }
    public void Complete (  ) { }
    public bool Contains ( T item ) { return default(bool); }
    public bool Contains ( object item ) { return default(bool); }
    public void CopyTo ( T[] array, int arrayIndex ) { }
    void System.Collections.ICollection.CopyTo ( System.Array array, int arrayIndex ) { }
    public void Dispose (  ) { }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
    public System.Collections.Generic.IEnumerator<T> GetEnumerator (  ) { return default(System.Collections.Generic.IEnumerator<T>); }
    public virtual void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }
    public int IndexOf ( object item ) { return default(int); }
    public int IndexOf ( T item ) { return default(int); }
    public void Insert ( int index, T item ) { }
    public void Insert ( int index, object item ) { }
    public void InsertItem ( System.Guid psInstanceId, int index, T item ) { }
    public static implicit operator System.Management.Automation.PSDataCollection<T> ( bool valueToConvert ) { return default(System.Management.Automation.PSDataCollection<T>); }
    public static implicit operator System.Management.Automation.PSDataCollection<T> ( string valueToConvert ) { return default(System.Management.Automation.PSDataCollection<T>); }
    public static implicit operator System.Management.Automation.PSDataCollection<T> ( int valueToConvert ) { return default(System.Management.Automation.PSDataCollection<T>); }
    public static implicit operator System.Management.Automation.PSDataCollection<T> ( System.Byte valueToConvert ) { return default(System.Management.Automation.PSDataCollection<T>); }
    public static implicit operator System.Management.Automation.PSDataCollection<T> ( System.Collections.Hashtable valueToConvert ) { return default(System.Management.Automation.PSDataCollection<T>); }
    public static implicit operator System.Management.Automation.PSDataCollection<T> ( T valueToConvert ) { return default(System.Management.Automation.PSDataCollection<T>); }
    public static implicit operator System.Management.Automation.PSDataCollection<T> ( object[] arrayToConvert ) { return default(System.Management.Automation.PSDataCollection<T>); }
    public System.Collections.ObjectModel.Collection<T> ReadAll (  ) { return default(System.Collections.ObjectModel.Collection<T>); }
    public bool Remove ( T item ) { return default(bool); }
    public void Remove ( object item ) { }
    public void RemoveAt ( int index ) { }
    public void RemoveItem ( int index ) { }

  }

  public sealed class PSDataStreams {
    internal PSDataStreams() { }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.DebugRecord> Debug { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.DebugRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.ErrorRecord> Error { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.ErrorRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.InformationRecord> Information { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.InformationRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.ProgressRecord> Progress { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.ProgressRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.VerboseRecord> Verbose { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.VerboseRecord>); } set { } }
    public System.Management.Automation.PSDataCollection<System.Management.Automation.WarningRecord> Warning { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.WarningRecord>); } set { } }
    public void ClearStreams (  ) { }

  }

  public class PSDebugContext {
    internal PSDebugContext() { }
    public PSDebugContext(System.Management.Automation.InvocationInfo invocationInfo, System.Collections.Generic.List<System.Management.Automation.Breakpoint> breakpoints) { }

    public System.Management.Automation.Breakpoint[] Breakpoints { get { return default(System.Management.Automation.Breakpoint[]); } set { } }
    public System.Management.Automation.InvocationInfo InvocationInfo { get { return default(System.Management.Automation.InvocationInfo); } set { } }
  }

   [System.AttributeUsageAttribute((System.AttributeTargets)384)]
   public sealed class PSDefaultValueAttribute : System.Management.Automation.Internal.ParsingBaseAttribute {
    public PSDefaultValueAttribute() { }

    public string Help { get { return default(string); } set { } }
    public object Value { get { return default(object); } set { } }
  }

  public class PSDriveInfo {
    protected PSDriveInfo(System.Management.Automation.PSDriveInfo driveInfo) { }
    public PSDriveInfo(string name, System.Management.Automation.ProviderInfo provider, string root, string description, System.Management.Automation.PSCredential credential) { }
    public PSDriveInfo(string name, System.Management.Automation.ProviderInfo provider, string root, string description, System.Management.Automation.PSCredential credential, string displayRoot) { }
    public PSDriveInfo(string name, System.Management.Automation.ProviderInfo provider, string root, string description, System.Management.Automation.PSCredential credential, bool persist) { }

    public System.Management.Automation.PSCredential Credential { get { return default(System.Management.Automation.PSCredential); } }
    public string CurrentLocation { get { return default(string); } set { } }
    public string Description { get { return default(string); } set { } }
    public string DisplayRoot { get { return default(string); } set { } }
    public System.Nullable<System.Int64> MaximumSize { get { return default(System.Nullable<System.Int64>); } set { } }
    public string Name { get { return default(string); } }
    public System.Management.Automation.ProviderInfo Provider { get { return default(System.Management.Automation.ProviderInfo); } }
    public string Root { get { return default(string); } set { } }
    public bool VolumeSeparatedByColon { get { return default(bool); } set { } }
    public int CompareTo ( object obj ) { return default(int); }
    public int CompareTo ( System.Management.Automation.PSDriveInfo drive ) { return default(int); }
    public bool Equals ( System.Management.Automation.PSDriveInfo drive ) { return default(bool); }
    public override bool Equals ( object obj ) { return default(bool); }
    public override int GetHashCode (  ) { return default(int); }
    public static bool operator == ( System.Management.Automation.PSDriveInfo drive1, System.Management.Automation.PSDriveInfo drive2 ) { return default(bool); }
    public static bool operator > ( System.Management.Automation.PSDriveInfo drive1, System.Management.Automation.PSDriveInfo drive2 ) { return default(bool); }
    public static bool operator != ( System.Management.Automation.PSDriveInfo drive1, System.Management.Automation.PSDriveInfo drive2 ) { return default(bool); }
    public static bool operator < ( System.Management.Automation.PSDriveInfo drive1, System.Management.Automation.PSDriveInfo drive2 ) { return default(bool); }
    public override string ToString (  ) { return default(string); }

  }

  public class PSDynamicMember : System.Management.Automation.PSMemberInfo {
    internal PSDynamicMember() { }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public override string TypeNameOfValue { get { return default(string); } }
    public override object Value { get { return default(object); } }
    public override System.Management.Automation.PSMemberInfo Copy (  ) { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString (  ) { return default(string); }

  }

  public sealed class PSEngineEvent {
    internal PSEngineEvent() { }
    public const string Exiting = "PowerShell.Exiting";
    public const string OnIdle = "PowerShell.OnIdle";
    public const string WorkflowJobStartEvent = "PowerShell.WorkflowJobStartEvent";
  }

  public class PSEvent : System.Management.Automation.PSMemberInfo {
    internal PSEvent() { }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public override string TypeNameOfValue { get { return default(string); } }
    public override object Value { get { return default(object); } }
    public override System.Management.Automation.PSMemberInfo Copy (  ) { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString (  ) { return default(string); }

  }

  public class PSEventArgs : System.EventArgs {
    internal PSEventArgs() { }
    public string ComputerName { get { return default(string); } set { } }
    public int EventIdentifier { get { return default(int); } set { } }
    public System.Management.Automation.PSObject MessageData { get { return default(System.Management.Automation.PSObject); } }
    public System.Guid RunspaceId { get { return default(System.Guid); } set { } }
    public object Sender { get { return default(object); } }
    public object[] SourceArgs { get { return default(object[]); } }
    public System.EventArgs SourceEventArgs { get { return default(System.EventArgs); } }
    public string SourceIdentifier { get { return default(string); } }
    public System.DateTime TimeGenerated { get { return default(System.DateTime); } set { } }
  }

   [System.Reflection.DefaultMemberAttribute("Item")]
   public class PSEventArgsCollection : System.Collections.Generic.IEnumerable<System.Management.Automation.PSEventArgs> {
    public PSEventArgsCollection() { }

    public event System.Management.Automation.PSEventReceivedEventHandler PSEventReceived { add { } remove { } }

    public int Count { get { return default(int); } }
    public System.Management.Automation.PSEventArgs Item { get { return default(System.Management.Automation.PSEventArgs); } }
    public object SyncRoot { get { return default(object); } }
    public System.Collections.Generic.IEnumerator<System.Management.Automation.PSEventArgs> GetEnumerator (  ) { return default(System.Collections.Generic.IEnumerator<System.Management.Automation.PSEventArgs>); }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
    public void RemoveAt ( int index ) { }

  }

  public class PSEventHandler {
    public PSEventHandler() { }
    public PSEventHandler(System.Management.Automation.PSEventManager eventManager, object sender, string sourceIdentifier, System.Management.Automation.PSObject extraData) { }

  }

  public class PSEventJob : System.Management.Automation.Job, System.IDisposable {
    public PSEventJob(System.Management.Automation.PSEventManager eventManager, System.Management.Automation.PSEventSubscriber subscriber, System.Management.Automation.ScriptBlock action, string name) { }

    public override bool HasMoreData { get { return default(bool); } }
    public override string Location { get { return default(string); } }
    public System.Management.Automation.PSModuleInfo Module { get { return default(System.Management.Automation.PSModuleInfo); } }
    public override string StatusMessage { get { return default(string); } }
    public override void StopJob (  ) { }

  }

  public abstract class PSEventManager {
    protected PSEventManager() { }

    public abstract event System.EventHandler<System.Management.Automation.PSEventArgs> ForwardEvent;

    public System.Management.Automation.PSEventArgsCollection ReceivedEvents { get { return default(System.Management.Automation.PSEventArgsCollection); } }
    public abstract System.Collections.Generic.List<System.Management.Automation.PSEventSubscriber> Subscribers { get; }
    protected virtual System.Management.Automation.PSEventArgs CreateEvent ( string sourceIdentifier, object sender, object[] args, System.Management.Automation.PSObject extraData ) { return default(System.Management.Automation.PSEventArgs); }
    public System.Management.Automation.PSEventArgs GenerateEvent ( string sourceIdentifier, object sender, object[] args, System.Management.Automation.PSObject extraData ) { return default(System.Management.Automation.PSEventArgs); }
    public System.Management.Automation.PSEventArgs GenerateEvent ( string sourceIdentifier, object sender, object[] args, System.Management.Automation.PSObject extraData, bool processInCurrentThread, bool waitForCompletionInCurrentThread ) { return default(System.Management.Automation.PSEventArgs); }
    public virtual System.Collections.Generic.IEnumerable<System.Management.Automation.PSEventSubscriber> GetEventSubscribers ( string sourceIdentifier ) { return default(System.Collections.Generic.IEnumerable<System.Management.Automation.PSEventSubscriber>); }
    protected internal virtual void ProcessNewEvent ( System.Management.Automation.PSEventArgs newEvent, bool processInCurrentThread, bool waitForCompletionWhenInCurrentThread ) { }
    protected virtual void ProcessNewEvent ( System.Management.Automation.PSEventArgs newEvent, bool processInCurrentThread ) { }
    public virtual System.Management.Automation.PSEventSubscriber SubscribeEvent ( object source, string eventName, string sourceIdentifier, System.Management.Automation.PSObject data, System.Management.Automation.ScriptBlock action, bool supportEvent, bool forwardEvent ) { return default(System.Management.Automation.PSEventSubscriber); }
    public virtual System.Management.Automation.PSEventSubscriber SubscribeEvent ( object source, string eventName, string sourceIdentifier, System.Management.Automation.PSObject data, System.Management.Automation.ScriptBlock action, bool supportEvent, bool forwardEvent, int maxTriggerCount ) { return default(System.Management.Automation.PSEventSubscriber); }
    public virtual System.Management.Automation.PSEventSubscriber SubscribeEvent ( object source, string eventName, string sourceIdentifier, System.Management.Automation.PSObject data, System.Management.Automation.PSEventReceivedEventHandler handlerDelegate, bool supportEvent, bool forwardEvent ) { return default(System.Management.Automation.PSEventSubscriber); }
    public virtual System.Management.Automation.PSEventSubscriber SubscribeEvent ( object source, string eventName, string sourceIdentifier, System.Management.Automation.PSObject data, System.Management.Automation.PSEventReceivedEventHandler handlerDelegate, bool supportEvent, bool forwardEvent, int maxTriggerCount ) { return default(System.Management.Automation.PSEventSubscriber); }
    public virtual void UnsubscribeEvent ( System.Management.Automation.PSEventSubscriber subscriber ) { }

  }

  public delegate void PSEventReceivedEventHandler(object sender, System.Management.Automation.PSEventArgs e);

  public class PSEventSubscriber {
    internal PSEventSubscriber() { }
    public event System.Management.Automation.PSEventUnsubscribedEventHandler Unsubscribed { add { } remove { } }

    public System.Management.Automation.PSEventJob Action { get { return default(System.Management.Automation.PSEventJob); } }
    public string EventName { get { return default(string); } }
    public bool ForwardEvent { get { return default(bool); } }
    public System.Management.Automation.PSEventReceivedEventHandler HandlerDelegate { get { return default(System.Management.Automation.PSEventReceivedEventHandler); } }
    public string SourceIdentifier { get { return default(string); } }
    public object SourceObject { get { return default(object); } }
    public int SubscriptionId { get { return default(int); } set { } }
    public bool SupportEvent { get { return default(bool); } }
    public bool Equals ( System.Management.Automation.PSEventSubscriber other ) { return default(bool); }
    public override int GetHashCode (  ) { return default(int); }

  }

  public class PSEventUnsubscribedEventArgs : System.EventArgs {
    internal PSEventUnsubscribedEventArgs() { }
    public System.Management.Automation.PSEventSubscriber EventSubscriber { get { return default(System.Management.Automation.PSEventSubscriber); } set { } }
  }

  public delegate void PSEventUnsubscribedEventHandler(object sender, System.Management.Automation.PSEventArgs e);

    [System.SerializableAttribute]
   public class PSInvalidCastException : System.InvalidCastException, System.Management.Automation.IContainsErrorRecord {
    protected PSInvalidCastException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public PSInvalidCastException() { }
    public PSInvalidCastException(string message) { }
    public PSInvalidCastException(string message, System.Exception innerException) { }

    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

  }

    [System.SerializableAttribute]
   public class PSInvalidOperationException : System.InvalidOperationException, System.Management.Automation.IContainsErrorRecord {
    public PSInvalidOperationException() { }
    protected PSInvalidOperationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public PSInvalidOperationException(string message) { }
    public PSInvalidOperationException(string message, System.Exception innerException) { }

    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

  }

  public sealed class PSInvocationSettings {
    public PSInvocationSettings() { }

    public bool AddToHistory { get { return default(bool); } set { } }
    public System.Nullable<System.Management.Automation.ActionPreference> ErrorActionPreference { get { return default(System.Nullable<System.Management.Automation.ActionPreference>); } set { } }
    public bool ExposeFlowControlExceptions { get { return default(bool); } set { } }
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

  public sealed class PSJobProxy : System.Management.Automation.Job2, System.IDisposable {
    internal PSJobProxy() { }
    public event System.EventHandler<System.ComponentModel.AsyncCompletedEventArgs> RemoveJobCompleted { add { } remove { } }

    public override bool HasMoreData { get { return default(bool); } }
    public override string Location { get { return default(string); } }
    public System.Guid RemoteJobInstanceId { get { return default(System.Guid); } }
    public bool RemoveRemoteJobOnCompletion { get { return default(bool); } set { } }
    public System.Management.Automation.Runspaces.Runspace Runspace { get { return default(System.Management.Automation.Runspaces.Runspace); } set { } }
    public System.Management.Automation.Runspaces.RunspacePool RunspacePool { get { return default(System.Management.Automation.Runspaces.RunspacePool); } set { } }
    public override string StatusMessage { get { return default(string); } }
    public static System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy> Create ( System.Management.Automation.Runspaces.Runspace runspace, System.Collections.Hashtable filter, bool receiveImmediately ) { return default(System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy>); }
    public static System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy> Create ( System.Management.Automation.Runspaces.Runspace runspace, System.Collections.Hashtable filter ) { return default(System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy>); }
    public static System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy> Create ( System.Management.Automation.Runspaces.Runspace runspace ) { return default(System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy>); }
    public static System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy> Create ( System.Management.Automation.Runspaces.RunspacePool runspacePool, System.Collections.Hashtable filter, System.EventHandler<System.Management.Automation.JobDataAddedEventArgs> dataAdded, System.EventHandler<System.Management.Automation.JobStateEventArgs> stateChanged ) { return default(System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy>); }
    public static System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy> Create ( System.Management.Automation.Runspaces.RunspacePool runspacePool, System.Collections.Hashtable filter, bool receiveImmediately ) { return default(System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy>); }
    public static System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy> Create ( System.Management.Automation.Runspaces.RunspacePool runspacePool, System.Collections.Hashtable filter ) { return default(System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy>); }
    public static System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy> Create ( System.Management.Automation.Runspaces.Runspace runspace, System.Collections.Hashtable filter, System.EventHandler<System.Management.Automation.JobDataAddedEventArgs> dataAdded, System.EventHandler<System.Management.Automation.JobStateEventArgs> stateChanged ) { return default(System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy>); }
    public static System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy> Create ( System.Management.Automation.Runspaces.RunspacePool runspacePool ) { return default(System.Collections.Generic.ICollection<System.Management.Automation.PSJobProxy>); }
    protected override void Dispose ( bool disposing ) { }
    public void ReceiveJob (  ) { }
    public void ReceiveJob ( System.EventHandler<System.Management.Automation.JobDataAddedEventArgs> dataAdded, System.EventHandler<System.Management.Automation.JobStateEventArgs> stateChanged ) { }
    public void RemoveJob ( bool removeRemoteJob ) { }
    public void RemoveJob ( bool removeRemoteJob, bool force ) { }
    public void RemoveJobAsync ( bool removeRemoteJob, bool force ) { }
    public void RemoveJobAsync ( bool removeRemoteJob ) { }
    public override void ResumeJob (  ) { }
    public override void ResumeJobAsync (  ) { }
    public override void StartJob (  ) { }
    public void StartJob ( System.Management.Automation.PSDataCollection<object> input ) { }
    public void StartJob ( System.EventHandler<System.Management.Automation.JobDataAddedEventArgs> dataAdded, System.EventHandler<System.Management.Automation.JobStateEventArgs> stateChanged, System.Management.Automation.PSDataCollection<object> input ) { }
    public void StartJobAsync ( System.EventHandler<System.Management.Automation.JobDataAddedEventArgs> dataAdded, System.EventHandler<System.Management.Automation.JobStateEventArgs> stateChanged, System.Management.Automation.PSDataCollection<object> input ) { }
    public override void StartJobAsync (  ) { }
    public void StartJobAsync ( System.Management.Automation.PSDataCollection<object> input ) { }
    public override void StopJob ( bool force, string reason ) { }
    public override void StopJob (  ) { }
    public override void StopJobAsync (  ) { }
    public override void StopJobAsync ( bool force, string reason ) { }
    public override void SuspendJob (  ) { }
    public override void SuspendJob ( bool force, string reason ) { }
    public override void SuspendJobAsync (  ) { }
    public override void SuspendJobAsync ( bool force, string reason ) { }
    public override void UnblockJob (  ) { }
    public override void UnblockJobAsync (  ) { }

  }

  public sealed class PSJobStartEventArgs : System.EventArgs {
    public PSJobStartEventArgs(System.Management.Automation.Job job, System.Management.Automation.Debugger debugger, bool isAsync) { }

    public System.Management.Automation.Debugger Debugger { get { return default(System.Management.Automation.Debugger); } set { } }
    public bool IsAsync { get { return default(bool); } set { } }
    public System.Management.Automation.Job Job { get { return default(System.Management.Automation.Job); } set { } }
  }

  public enum PSLanguageMode {
    ConstrainedLanguage = 3,
    FullLanguage = 0,
    NoLanguage = 2,
    RestrictedLanguage = 1,
  }

  public class PSListModifier {
    public PSListModifier() { }
    public PSListModifier(System.Collections.ObjectModel.Collection<object> removeItems, System.Collections.ObjectModel.Collection<object> addItems) { }
    public PSListModifier(object replacementItems) { }
    public PSListModifier(System.Collections.Hashtable hash) { }

    public System.Collections.ObjectModel.Collection<object> Add { get { return default(System.Collections.ObjectModel.Collection<object>); } }
    public System.Collections.ObjectModel.Collection<object> Remove { get { return default(System.Collections.ObjectModel.Collection<object>); } }
    public System.Collections.ObjectModel.Collection<object> Replace { get { return default(System.Collections.ObjectModel.Collection<object>); } }
    public void ApplyTo ( System.Collections.IList collectionToUpdate ) { }
    public void ApplyTo ( object collectionToUpdate ) { }

  }

  public class PSListModifier<T> {
    public PSListModifier() { }
    public PSListModifier(System.Collections.ObjectModel.Collection<object> removeItems, System.Collections.ObjectModel.Collection<object> addItems) { }
    public PSListModifier(object replacementItems) { }
    public PSListModifier(System.Collections.Hashtable hash) { }

  }

  public abstract class PSMemberInfo {
    protected PSMemberInfo() { }

    public bool IsInstance { get { return default(bool); } set { } }
    public abstract System.Management.Automation.PSMemberTypes MemberType { get; }
    public string Name { get { return default(string); } }
    public abstract string TypeNameOfValue { get; }
    public abstract object Value { get; }
    public virtual System.Management.Automation.PSMemberInfo Copy (  ) { return default(System.Management.Automation.PSMemberInfo); }
 
  }

   public abstract class PSMemberInfoCollection<T> : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable where T : System.Management.Automation.PSMemberInfo {
    protected PSMemberInfoCollection() { }

    public abstract T this[int index] { get; }
    public virtual void Add ( T member ) { }
    public virtual void Add ( T member, bool preValidated ) { }
    public virtual System.Collections.Generic.IEnumerator<T> GetEnumerator (  ) { return default(System.Collections.Generic.IEnumerator<T>); }
    public virtual ReadOnlyPSMemberInfoCollection<T> Match ( string name ) { return default(ReadOnlyPSMemberInfoCollection<T>); }
    public virtual ReadOnlyPSMemberInfoCollection<T> Match ( string name, System.Management.Automation.PSMemberTypes memberTypes ) { return default(ReadOnlyPSMemberInfoCollection<T>); }
    public virtual void Remove ( string name ) { }
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
    public override object Value { get { return default(object); } }
    public override System.Management.Automation.PSMemberInfo Copy (  ) { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString (  ) { return default(string); }

  }

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
    public override System.Management.Automation.PSMemberInfo Copy (  ) { return default(System.Management.Automation.PSMemberInfo); }
    public override object Invoke ( object[] arguments ) { return default(object); }
    public override string ToString (  ) { return default(string); }

  }

  public abstract class PSMethodInfo : System.Management.Automation.PSMemberInfo {
    protected PSMethodInfo() { }

    public abstract System.Collections.ObjectModel.Collection<string> OverloadDefinitions { get; }
    public sealed override object Value { get { return default(object); } }
    public abstract object Invoke ( params object[] arguments );

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
    public string Author { get { return default(string); } set { } }
    public System.Version ClrVersion { get { return default(System.Version); } set { } }
    public string CompanyName { get { return default(string); } set { } }
    public System.Collections.Generic.IEnumerable<string> CompatiblePSEditions { get { return default(System.Collections.Generic.IEnumerable<string>); } }
    public string Copyright { get { return default(string); } set { } }
    public string Definition { get { return default(string); } }
    public string Description { get { return default(string); } set { } }
    public System.Version DotNetFrameworkVersion { get { return default(System.Version); } set { } }
    public System.Collections.Generic.Dictionary<string, System.Management.Automation.AliasInfo> ExportedAliases { get { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.AliasInfo>); } }
    public System.Collections.Generic.Dictionary<string, System.Management.Automation.CmdletInfo> ExportedCmdlets { get { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.CmdletInfo>); } }
    public System.Collections.Generic.Dictionary<string, System.Management.Automation.CommandInfo> ExportedCommands { get { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.CommandInfo>); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<string> ExportedDscResources { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<string>); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<string> ExportedFormatFiles { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<string>); } set { } }
    public System.Collections.Generic.Dictionary<string, System.Management.Automation.FunctionInfo> ExportedFunctions { get { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.FunctionInfo>); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<string> ExportedTypeFiles { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<string>); } set { } }
    public System.Collections.Generic.Dictionary<string, System.Management.Automation.PSVariable> ExportedVariables { get { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.PSVariable>); } }
    public System.Collections.Generic.Dictionary<string, System.Management.Automation.FunctionInfo> ExportedWorkflows { get { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.FunctionInfo>); } }
    public System.Collections.Generic.IEnumerable<string> FileList { get { return default(System.Collections.Generic.IEnumerable<string>); } }
    public System.Guid Guid { get { return default(System.Guid); } set { } }
    public string HelpInfoUri { get { return default(string); } set { } }
    public System.Uri IconUri { get { return default(System.Uri); } set { } }
    public System.Reflection.Assembly ImplementingAssembly { get { return default(System.Reflection.Assembly); } set { } }
    public System.Uri LicenseUri { get { return default(System.Uri); } set { } }
    public bool LogPipelineExecutionDetails { get { return default(bool); } set { } }
    public string ModuleBase { get { return default(string); } }
    public System.Collections.Generic.IEnumerable<object> ModuleList { get { return default(System.Collections.Generic.IEnumerable<object>); } }
    public System.Management.Automation.ModuleType ModuleType { get { return default(System.Management.Automation.ModuleType); } set { } }
    public string Name { get { return default(string); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSModuleInfo> NestedModules { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSModuleInfo>); } }
    public System.Management.Automation.ScriptBlock OnRemove { get { return default(System.Management.Automation.ScriptBlock); } set { } }
    public string Path { get { return default(string); } set { } }
    public string PowerShellHostName { get { return default(string); } set { } }
    public System.Version PowerShellHostVersion { get { return default(System.Version); } set { } }
    public System.Version PowerShellVersion { get { return default(System.Version); } set { } }
    public string Prefix { get { return default(string); } set { } }
    public object PrivateData { get { return default(object); } set { } }
    public System.Reflection.ProcessorArchitecture ProcessorArchitecture { get { return default(System.Reflection.ProcessorArchitecture); } set { } }
    public System.Uri ProjectUri { get { return default(System.Uri); } set { } }
    public string ReleaseNotes { get { return default(string); } set { } }
    public System.Uri RepositorySourceLocation { get { return default(System.Uri); } set { } }
    public System.Collections.Generic.IEnumerable<string> RequiredAssemblies { get { return default(System.Collections.Generic.IEnumerable<string>); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSModuleInfo> RequiredModules { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSModuleInfo>); } }
    public string RootModule { get { return default(string); } set { } }
    public System.Collections.Generic.IEnumerable<string> Scripts { get { return default(System.Collections.Generic.IEnumerable<string>); } }
    public System.Management.Automation.SessionState SessionState { get { return default(System.Management.Automation.SessionState); } set { } }
    public System.Collections.Generic.IEnumerable<string> Tags { get { return default(System.Collections.Generic.IEnumerable<string>); } }
    public bool UseAppDomainLevelModuleCache { get { return default(bool); } set { } }
    public System.Version Version { get { return default(System.Version); } set { } }
    public System.Management.Automation.PSObject AsCustomObject (  ) { return default(System.Management.Automation.PSObject); }
    public static void ClearAppDomainLevelModulePathCache (  ) { }
    public System.Management.Automation.PSModuleInfo Clone (  ) { return default(System.Management.Automation.PSModuleInfo); }
    public System.Collections.ObjectModel.ReadOnlyDictionary<System.String,System.Management.Automation.Language.TypeDefinitionAst> GetExportedTypeDefinitions (  ) { return default(System.Collections.ObjectModel.ReadOnlyDictionary<System.String,System.Management.Automation.Language.TypeDefinitionAst>); }
    public System.Management.Automation.PSVariable GetVariableFromCallersModule ( string variableName ) { return default(System.Management.Automation.PSVariable); }
    public object Invoke ( System.Management.Automation.ScriptBlock sb, object[] args ) { return default(object); }
    public System.Management.Automation.ScriptBlock NewBoundScriptBlock ( System.Management.Automation.ScriptBlock scriptBlockToBind ) { return default(System.Management.Automation.ScriptBlock); }
    public override string ToString (  ) { return default(string); }

  }

  public class PSNoteProperty : System.Management.Automation.PSPropertyInfo {
    public PSNoteProperty(string name, object value) { }

    public override bool IsGettable { get { return default(bool); } }
    public override bool IsSettable { get { return default(bool); } }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public override string TypeNameOfValue { get { return default(string); } }
    public override object Value { get { return default(object); } }
    public override System.Management.Automation.PSMemberInfo Copy (  ) { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString (  ) { return default(string); }

  }

    [System.SerializableAttribute]
   public class PSNotImplementedException : System.NotImplementedException, System.Management.Automation.IContainsErrorRecord {
    public PSNotImplementedException() { }
    protected PSNotImplementedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public PSNotImplementedException(string message) { }
    public PSNotImplementedException(string message, System.Exception innerException) { }

    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

  }

    [System.SerializableAttribute]
   public class PSNotSupportedException : System.NotSupportedException, System.Management.Automation.IContainsErrorRecord {
    public PSNotSupportedException() { }
    protected PSNotSupportedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public PSNotSupportedException(string message) { }
    public PSNotSupportedException(string message, System.Exception innerException) { }

    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

  }

    [System.SerializableAttribute]
   public class PSObject : System.IFormattable, System.IComparable, System.Runtime.Serialization.ISerializable, System.Dynamic.IDynamicMetaObjectProvider {
    public PSObject() { }
    public PSObject(object obj) { }
    protected PSObject(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

    public const string AdaptedMemberSetName = "psadapted";
    public const string BaseObjectMemberSetName = "psbase";
    public const string ExtendedMemberSetName = "psextended";
    public object BaseObject { get { return default(object); } }
    public object ImmediateBaseObject { get { return default(object); } }
    public System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSMemberInfo> Members { get { return default(System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSMemberInfo>); } }
    public System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSMethodInfo> Methods { get { return default(System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSMethodInfo>); } }
    public System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSPropertyInfo> Properties { get { return default(System.Management.Automation.PSMemberInfoCollection<System.Management.Automation.PSPropertyInfo>); } }
    public System.Collections.ObjectModel.Collection<string> TypeNames { get { return default(System.Collections.ObjectModel.Collection<string>); } }
    public static System.Management.Automation.PSObject AsPSObject ( object obj ) { return default(System.Management.Automation.PSObject); }
    public int CompareTo ( object obj ) { return default(int); }
    public System.Management.Automation.PSObject Copy (  ) { return default(System.Management.Automation.PSObject); }
    public override bool Equals ( object obj ) { return default(bool); }
    public override int GetHashCode (  ) { return default(int); }
    public virtual void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }
    public static implicit operator System.Management.Automation.PSObject ( System.Double valueToConvert ) { return default(System.Management.Automation.PSObject); }
    public static implicit operator System.Management.Automation.PSObject ( int valueToConvert ) { return default(System.Management.Automation.PSObject); }
    public static implicit operator System.Management.Automation.PSObject ( bool valueToConvert ) { return default(System.Management.Automation.PSObject); }
    public static implicit operator System.Management.Automation.PSObject ( System.Collections.Hashtable valueToConvert ) { return default(System.Management.Automation.PSObject); }
    public static implicit operator System.Management.Automation.PSObject ( string valueToConvert ) { return default(System.Management.Automation.PSObject); }
    System.Dynamic.DynamicMetaObject System.Dynamic.IDynamicMetaObjectProvider.GetMetaObject(System.Linq.Expressions.Expression parameter) { return default(System.Dynamic.DynamicMetaObject); }
    public string ToString ( string format, System.IFormatProvider formatProvider ) { return default(string); }
    public override string ToString (  ) { return default(string); }

  }

    [System.SerializableAttribute]
   public class PSObjectDisposedException : System.ObjectDisposedException, System.Management.Automation.IContainsErrorRecord {
    public PSObjectDisposedException(string objectName) : base (objectName) { }
    public PSObjectDisposedException(string objectName, string message) : base(objectName, message) { }
    public PSObjectDisposedException(string message, System.Exception innerException) :base(message,innerException) { }
    protected PSObjectDisposedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base ( default(string)) { }

    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

  }

#if COMPONENT_MODEL
  public class PSObjectPropertyDescriptor : System.ComponentModel.PropertyDescriptor {
    internal PSObjectPropertyDescriptor(string propertyName, Type propertyType, bool isReadOnly, System.ComponentModel.AttributeCollection propertyAttributes) : base (propertyName, default(System.ComponentModel.AttributeCollection)) { }
    public event System.EventHandler<System.Management.Automation.GettingValueExceptionEventArgs> GettingValueException { add { } remove { } }
    public event System.EventHandler<System.Management.Automation.SettingValueExceptionEventArgs> SettingValueException { add { } remove { } }

    public override System.ComponentModel.AttributeCollection Attributes { get { return default(System.ComponentModel.AttributeCollection); } }
    public override System.Type ComponentType { get { return default(System.Type); } }
    public override bool IsReadOnly { get { return default(bool); } }
    public override System.Type PropertyType { get { return default(System.Type); } }
    public override bool CanResetValue ( object component ) { return default(bool); }
    public override object GetValue ( object component ) { return default(object); }
    public override void ResetValue ( object component ) { }
    public override void SetValue ( object component, object value ) { }
    public override bool ShouldSerializeValue ( object component ) { return default(bool); }

  }
#endif

  public class PSObjectTypeDescriptionProvider : System.ComponentModel.TypeDescriptionProvider {
    public PSObjectTypeDescriptionProvider() { }

    public event System.EventHandler<System.Management.Automation.GettingValueExceptionEventArgs> GettingValueException { add { } remove { } }
    public event System.EventHandler<System.Management.Automation.SettingValueExceptionEventArgs> SettingValueException { add { } remove { } }

    public override System.ComponentModel.ICustomTypeDescriptor GetTypeDescriptor ( System.Type objectType, object instance ) { return default(System.ComponentModel.ICustomTypeDescriptor); }

  }

  public class PSObjectTypeDescriptor : System.ComponentModel.CustomTypeDescriptor {
    public PSObjectTypeDescriptor(System.Management.Automation.PSObject instance) { }

    public event System.EventHandler<System.Management.Automation.GettingValueExceptionEventArgs> GettingValueException { add { } remove { } }
    public event System.EventHandler<System.Management.Automation.SettingValueExceptionEventArgs> SettingValueException { add { } remove { } }

    public System.Management.Automation.PSObject Instance { get { return default(System.Management.Automation.PSObject); } }
    public override bool Equals ( object obj ) { return default(bool); }
    public override System.ComponentModel.AttributeCollection GetAttributes (  ) { return default(System.ComponentModel.AttributeCollection); }
    public override string GetClassName (  ) { return default(string); }
    public override string GetComponentName (  ) { return default(string); }
    public override System.ComponentModel.TypeConverter GetConverter (  ) { return default(System.ComponentModel.TypeConverter); }
    public override System.ComponentModel.EventDescriptor GetDefaultEvent (  ) { return default(System.ComponentModel.EventDescriptor); }
    public override System.ComponentModel.PropertyDescriptor GetDefaultProperty (  ) { return default(System.ComponentModel.PropertyDescriptor); }
    public override object GetEditor ( System.Type editorBaseType ) { return default(object); }
    public override System.ComponentModel.EventDescriptorCollection GetEvents (  ) { return default(System.ComponentModel.EventDescriptorCollection); }
    public override System.ComponentModel.EventDescriptorCollection GetEvents ( System.Attribute[] attributes ) { return default(System.ComponentModel.EventDescriptorCollection); }
    public override int GetHashCode (  ) { return default(int); }
    public override System.ComponentModel.PropertyDescriptorCollection GetProperties (  ) { return default(System.ComponentModel.PropertyDescriptorCollection); }
    public override System.ComponentModel.PropertyDescriptorCollection GetProperties ( System.Attribute[] attributes ) { return default(System.ComponentModel.PropertyDescriptorCollection); }
    public override object GetPropertyOwner ( System.ComponentModel.PropertyDescriptor pd ) { return default(object); }

  }

  public class PSParameterizedProperty : System.Management.Automation.PSMethodInfo {
    internal PSParameterizedProperty() { }
    public bool IsGettable { get { return default(bool); } }
    public bool IsSettable { get { return default(bool); } }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public override System.Collections.ObjectModel.Collection<string> OverloadDefinitions { get { return default(System.Collections.ObjectModel.Collection<string>); } }
    public override string TypeNameOfValue { get { return default(string); } }
    public override System.Management.Automation.PSMemberInfo Copy (  ) { return default(System.Management.Automation.PSMemberInfo); }
    public override object Invoke ( object[] arguments ) { return default(object); }
    public void InvokeSet ( object valueToSet, object[] arguments ) { }
    public override string ToString (  ) { return default(string); }

  }

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

    [System.SerializableAttribute]
    [System.Reflection.DefaultMemberAttribute("Item")]
   public sealed class PSPrimitiveDictionary : System.Collections.Hashtable {
    public PSPrimitiveDictionary() { }
    public PSPrimitiveDictionary(System.Collections.Hashtable other) { }

    public object Item { get { return default(object); } set { } }
    public void Add ( string key, System.Int32[] value ) { }
    public void Add ( string key, System.Int64[] value ) { }
    public void Add ( string key, System.SByte value ) { }
    public void Add ( string key, System.SByte[] value ) { }
    public void Add ( string key, System.Single value ) { }
    public void Add ( string key, System.Single[] value ) { }
    public void Add ( string key, string value ) { }
    public void Add ( string key, string[] value ) { }
    public void Add ( string key, System.TimeSpan value ) { }
    public void Add ( string key, System.Int64 value ) { }
    public void Add ( string key, System.TimeSpan[] value ) { }
    public void Add ( string key, System.UInt16[] value ) { }
    public void Add ( string key, uint value ) { }
    public void Add ( string key, System.UInt32[] value ) { }
    public void Add ( string key, System.UInt64 value ) { }
    public void Add ( string key, System.UInt64[] value ) { }
    public void Add ( string key, System.Uri value ) { }
    public void Add ( string key, System.Uri[] value ) { }
    public void Add ( string key, System.Version value ) { }
    public void Add ( string key, System.UInt16 value ) { }
    public void Add ( string key, System.Version[] value ) { }
    public void Add ( string key, int value ) { }
    public void Add ( string key, System.Guid value ) { }
    public void Add ( string key, System.Management.Automation.PSPrimitiveDictionary[] value ) { }
    public override void Add ( object key, object value ) { }
    public void Add ( string key, bool value ) { }
    public void Add ( string key, System.Boolean[] value ) { }
    public void Add ( string key, System.Guid[] value ) { }
    public void Add ( string key, System.Byte[] value ) { }
    public void Add ( string key, char value ) { }
    public void Add ( string key, System.Byte value ) { }
    public void Add ( string key, System.DateTime value ) { }
    public void Add ( string key, System.DateTime[] value ) { }
    public void Add ( string key, System.Decimal value ) { }
    public void Add ( string key, System.Decimal[] value ) { }
    public void Add ( string key, System.Double value ) { }
    public void Add ( string key, System.Double[] value ) { }
    public void Add ( string key, char[] value ) { }
    public void Add ( string key, System.Management.Automation.PSPrimitiveDictionary value ) { }
    public override object Clone (  ) { return default(object); }

  }

  public class PSProperty : System.Management.Automation.PSPropertyInfo {
    internal PSProperty() { }
    public override bool IsGettable { get { return default(bool); } }
    public override bool IsSettable { get { return default(bool); } }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public override string TypeNameOfValue { get { return default(string); } }
    public override object Value { get { return default(object); } }
    public override System.Management.Automation.PSMemberInfo Copy (  ) { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString (  ) { return default(string); }

  }

  public abstract class PSPropertyAdapter {
    protected PSPropertyAdapter() { }

    public virtual System.Collections.ObjectModel.Collection<System.Management.Automation.PSAdaptedProperty> GetProperties ( object baseObject ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSAdaptedProperty>); }
    public virtual System.Management.Automation.PSAdaptedProperty GetProperty ( object baseObject, string propertyName ) { return default(System.Management.Automation.PSAdaptedProperty); }
    public virtual string GetPropertyTypeName ( System.Management.Automation.PSAdaptedProperty adaptedProperty ) { return default(string); }
    public virtual object GetPropertyValue ( System.Management.Automation.PSAdaptedProperty adaptedProperty ) { return default(object); }
    public virtual System.Collections.ObjectModel.Collection<System.String> GetTypeNameHierarchy ( object baseObject ) { return default(System.Collections.ObjectModel.Collection<System.String>); }
    public virtual bool IsGettable ( System.Management.Automation.PSAdaptedProperty adaptedProperty ) { return default(bool); }
    public virtual bool IsSettable ( System.Management.Automation.PSAdaptedProperty adaptedProperty ) { return default(bool); }
    public virtual void SetPropertyValue ( System.Management.Automation.PSAdaptedProperty adaptedProperty, object value ) { }

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
    public override object Value { get { return default(object); } }
    public override System.Management.Automation.PSMemberInfo Copy (  ) { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString (  ) { return default(string); }

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
    public override System.Management.Automation.PSMemberInfo Copy (  ) { return default(System.Management.Automation.PSMemberInfo); }
    public override object Invoke ( object[] arguments ) { return default(object); }
    public override string ToString (  ) { return default(string); }

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
    public override object Value { get { return default(object); } }
    public override System.Management.Automation.PSMemberInfo Copy (  ) { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString (  ) { return default(string); }

  }

    [System.SerializableAttribute]
   public class PSSecurityException : System.Management.Automation.RuntimeException {
    public PSSecurityException() { }
    protected PSSecurityException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public PSSecurityException(string message) { }
    public PSSecurityException(string message, System.Exception innerException) { }

    public override System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override string Message { get { return default(string); } }
  }

  public class PSSerializer {
    internal PSSerializer() { }
    public static object Deserialize ( string source ) { return default(object); }
    public static object[] DeserializeAsList ( string source ) { return default(object[]); }
    public static string Serialize ( object source ) { return default(string); }
    public static string Serialize ( object source, int depth ) { return default(string); }

  }

  public abstract class PSSessionTypeOption {
    protected PSSessionTypeOption() { }

    protected internal virtual System.Management.Automation.PSSessionTypeOption ConstructObjectFromPrivateData ( string privateData ) { return default(System.Management.Automation.PSSessionTypeOption); }
    protected internal virtual string ConstructPrivateData (  ) { return default(string); }
    protected internal virtual void CopyUpdatedValuesFrom ( System.Management.Automation.PSSessionTypeOption updated ) { }

  }

  public class PSSnapInInfo {
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
    public override string ToString (  ) { return default(string); }

  }

    [System.SerializableAttribute]
   public class PSSnapInSpecification {
    public string Name { get { return default(string); } set { } }
    public System.Version Version { get { return default(System.Version); } set { } }
  }

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
    public static System.Management.Automation.PSTokenType GetPSTokenType ( System.Management.Automation.Language.Token token ) { return default(System.Management.Automation.PSTokenType); }

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

  public sealed class PSTransactionContext : System.IDisposable {
    internal PSTransactionContext() { }
    public void Dispose (  ) { }

  }

  public abstract class PSTransportOption {
    protected PSTransportOption() { }

    public virtual object Clone (  ) { return default(object); }
    protected internal virtual void LoadFromDefaults ( System.Management.Automation.Runspaces.PSSessionType sessionType, bool keepAssigned ) { }

  }

  public abstract class PSTypeConverter {
    protected PSTypeConverter() { }

    public virtual bool CanConvertFrom ( object sourceValue, System.Type destinationType ) { return default(bool); }
    public virtual bool CanConvertFrom ( System.Management.Automation.PSObject sourceValue, System.Type destinationType ) { return default(bool); }
    public virtual bool CanConvertTo ( object sourceValue, System.Type destinationType ) { return default(bool); }
    public virtual bool CanConvertTo ( System.Management.Automation.PSObject sourceValue, System.Type destinationType ) { return default(bool); }
    public virtual object ConvertFrom ( object sourceValue, System.Type destinationType, System.IFormatProvider formatProvider, bool ignoreCase ) { return default(object); }
    public virtual object ConvertFrom ( System.Management.Automation.PSObject sourceValue, System.Type destinationType, System.IFormatProvider formatProvider, bool ignoreCase ) { return default(object); }
    public virtual object ConvertTo ( object sourceValue, System.Type destinationType, System.IFormatProvider formatProvider, bool ignoreCase ) { return default(object); }
    public virtual object ConvertTo ( System.Management.Automation.PSObject sourceValue, System.Type destinationType, System.IFormatProvider formatProvider, bool ignoreCase ) { return default(object); }

  }

  public class PSTypeName {
    public PSTypeName(System.Type type) { }
    public PSTypeName(string name) { }
    public PSTypeName(System.Management.Automation.Language.TypeDefinitionAst typeDefinitionAst) { }
    public PSTypeName(System.Management.Automation.Language.ITypeName typeName) { }

    public string Name { get { return default(string); } }
    public System.Type Type { get { return default(System.Type); } }
    public System.Management.Automation.Language.TypeDefinitionAst TypeDefinitionAst { get { return default(System.Management.Automation.Language.TypeDefinitionAst); } set { } }
    public override string ToString (  ) { return default(string); }

  }

   [System.AttributeUsageAttribute((System.AttributeTargets)384, AllowMultiple = false)]
   public class PSTypeNameAttribute : System.Attribute {
    public PSTypeNameAttribute(string psTypeName) { }

    public string PSTypeName { get { return default(string); } set { } }
  }

  public class PSVariable {
    public PSVariable(string name) { }
    public PSVariable(string name, object value) { }
    public PSVariable(string name, object value, System.Management.Automation.ScopedItemOptions options) { }
    public PSVariable(string name, object value, System.Management.Automation.ScopedItemOptions options, System.Collections.ObjectModel.Collection<System.Attribute> attributes) { }

    public System.Collections.ObjectModel.Collection<System.Attribute> Attributes { get { return default(System.Collections.ObjectModel.Collection<System.Attribute>); } }
    public string Description { get { return default(string); } set { } }
    public System.Management.Automation.PSModuleInfo Module { get { return default(System.Management.Automation.PSModuleInfo); } set { } }
    public string ModuleName { get { return default(string); } }
    public string Name { get { return default(string); } }
    public System.Management.Automation.ScopedItemOptions Options { get { return default(System.Management.Automation.ScopedItemOptions); } set { } }
    public object Value { get { return default(object); } set { } }
    public System.Management.Automation.SessionStateEntryVisibility Visibility { get { return default(System.Management.Automation.SessionStateEntryVisibility); } set { } }
    public bool IsValidValue ( object value ) { return default(bool); }

  }

  public sealed class PSVariableIntrinsics {
    public System.Management.Automation.PSVariable Get ( string name ) { return default(System.Management.Automation.PSVariable); }
    public object GetValue ( string name ) { return default(object); }
    public object GetValue ( string name, object defaultValue ) { return default(object); }
    public void Remove ( string name ) { }
    public void Remove ( System.Management.Automation.PSVariable variable ) { }
    public void Set ( string name, object value ) { }
    public void Set ( System.Management.Automation.PSVariable variable ) { }

  }

  public class PSVariableProperty : System.Management.Automation.PSNoteProperty {
    public PSVariableProperty(System.Management.Automation.PSVariable variable) : base(default(string), default(object)) { }

    public override bool IsGettable { get { return default(bool); } }
    public override bool IsSettable { get { return default(bool); } }
    public override System.Management.Automation.PSMemberTypes MemberType { get { return default(System.Management.Automation.PSMemberTypes); } }
    public override string TypeNameOfValue { get { return default(string); } }
    public override object Value { get { return default(object); } }
    public override System.Management.Automation.PSMemberInfo Copy (  ) { return default(System.Management.Automation.PSMemberInfo); }
    public override string ToString (  ) { return default(string); }

  }

  public sealed class PSVersionHashTable : System.Collections.Hashtable {
    public override System.Collections.ICollection Keys { get { return default(System.Collections.ICollection); } }
  }

   public class ReadOnlyPSMemberInfoCollection<T> : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable where T : System.Management.Automation.PSMemberInfo {
    internal ReadOnlyPSMemberInfoCollection() { }
    public int Count { get { return default(int); } }
    public T this[int index] { get { return default(T); } }
    public System.Management.Automation.ReadOnlyPSMemberInfoCollection<T> Match ( string name ) { return default(System.Management.Automation.ReadOnlyPSMemberInfoCollection<T>); }
    public System.Management.Automation.ReadOnlyPSMemberInfoCollection<T> Match ( string name, System.Management.Automation.PSMemberTypes memberTypes ) { return default(System.Management.Automation.ReadOnlyPSMemberInfoCollection<T>); }

    public virtual System.Collections.Generic.IEnumerator<T> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<T>); }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
 
  }

    [System.SerializableAttribute]
   public class RedirectedException : System.Management.Automation.RuntimeException {
    public RedirectedException() { }
    public RedirectedException(string message) { }
    public RedirectedException(string message, System.Exception innerException) { }
    protected RedirectedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

  }

   [System.Management.Automation.CmdletAttribute("Register", "ArgumentCompleter", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=528576")]
   public class RegisterArgumentCompleterCommand : System.Management.Automation.PSCmdlet {
    public RegisterArgumentCompleterCommand() { }

    [System.Management.Automation.ParameterAttribute(ParameterSetName = "PowerShellSet")]
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "NativeSet", Mandatory=true)]
    public string[] CommandName { get { return default(string[]); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "NativeSet")]
    public System.Management.Automation.SwitchParameter Native { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.ParameterAttribute(ParameterSetName = "PowerShellSet", Mandatory=true)]
    public string ParameterName { get { return default(string); } set { } }
    [System.Management.Automation.AllowNullAttribute]
    [System.Management.Automation.ParameterAttribute(Mandatory=true)]
    public System.Management.Automation.ScriptBlock ScriptBlock { get { return default(System.Management.Automation.ScriptBlock); } set { } }
    
    protected override void EndProcessing (  ) { }

  }

  public class RemoteCommandInfo : System.Management.Automation.CommandInfo {
    internal RemoteCommandInfo() { }
    public override string Definition { get { return default(string); } }
    public override System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName> OutputType { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName>); } }
  }

    [System.SerializableAttribute]
   public class RemoteException : System.Management.Automation.RuntimeException {
    public RemoteException() { }
    public RemoteException(string message) { }
    public RemoteException(string message, System.Exception innerException) { }
    protected RemoteException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

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

  public abstract class Repository<T> where T : class  {
    protected Repository(string identifier) { }

    public void Add ( T item ) { }
    public T GetItem ( System.Guid instanceId ) { return default(T); }
    public System.Collections.Generic.List<T> GetItems (  ) { return default(System.Collections.Generic.List<T>); }
    protected virtual System.Guid GetKey ( T item ) { return default(System.Guid); }
    public void Remove ( T item ) { }

  }

  public enum ResolutionPurpose {
    Decryption = 1,
    Encryption = 0,
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
    protected override System.Guid GetKey ( System.Management.Automation.Runspaces.PSSession item ) { return default(System.Guid); }

  }

  public class RuntimeDefinedParameter {
    public RuntimeDefinedParameter() { }
    public RuntimeDefinedParameter(string name, System.Type parameterType, System.Collections.ObjectModel.Collection<System.Attribute> attributes) { }

    public System.Collections.ObjectModel.Collection<System.Attribute> Attributes { get { return default(System.Collections.ObjectModel.Collection<System.Attribute>); } }
    public bool IsSet { get { return default(bool); } set { } }
    public string Name { get { return default(string); } set { } }
    public System.Type ParameterType { get { return default(System.Type); } set { } }
    public object Value { get { return default(object); } set { } }
  }

    [System.SerializableAttribute]
   public class RuntimeDefinedParameterDictionary : System.Collections.Generic.Dictionary<string, System.Management.Automation.RuntimeDefinedParameter> {
    public RuntimeDefinedParameterDictionary() { }

    public object Data { get { return default(object); } set { } }
    public string HelpFile { get { return default(string); } set { } }
  }

    [System.SerializableAttribute]
   public class RuntimeException : System.SystemException, System.Management.Automation.IContainsErrorRecord {
    public RuntimeException() { }
    protected RuntimeException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public RuntimeException(string message) { }
    public RuntimeException(string message, System.Exception innerException) { }
    public RuntimeException(string message, System.Exception innerException, System.Management.Automation.ErrorRecord errorRecord) { }

    public virtual System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public bool WasThrownFromThrowStatement { get { return default(bool); } set { } }
    public override void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

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

    [System.SerializableAttribute]
   public class ScriptBlock {
    protected ScriptBlock(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

    public System.Management.Automation.Language.Ast Ast { get { return default(System.Management.Automation.Language.Ast); } }
    public System.Collections.Generic.List<System.Attribute> Attributes { get { return default(System.Collections.Generic.List<System.Attribute>); } }
    public bool DebuggerHidden { get { return default(bool); } set { } }
    public string File { get { return default(string); } }
    public System.Guid Id { get { return default(System.Guid); } }
    public bool IsConfiguration { get { return default(bool); } set { } }
    public bool IsFilter { get { return default(bool); } set { } }
    public System.Management.Automation.PSModuleInfo Module { get { return default(System.Management.Automation.PSModuleInfo); } }
    public System.Management.Automation.PSToken StartPosition { get { return default(System.Management.Automation.PSToken); } }
    public void CheckRestrictedLanguage ( System.Collections.Generic.IEnumerable<string> allowedCommands, System.Collections.Generic.IEnumerable<string> allowedVariables, bool allowEnvironmentVariables ) { }
    public static System.Management.Automation.ScriptBlock Create ( string script ) { return default(System.Management.Automation.ScriptBlock); }
    public System.Management.Automation.ScriptBlock GetNewClosure (  ) { return default(System.Management.Automation.ScriptBlock); }
    public virtual void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }
    public System.Management.Automation.PowerShell GetPowerShell ( System.Collections.Generic.Dictionary<string, object> variables, out System.Collections.Generic.Dictionary<string,object> usingVariables, bool isTrustedInput, object[] args ) { usingVariables = default(System.Collections.Generic.Dictionary<string,object>); return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell GetPowerShell ( object[] args ) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell GetPowerShell ( bool isTrustedInput, object[] args ) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell GetPowerShell ( System.Collections.Generic.Dictionary<string, object> variables, object[] args ) { return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.PowerShell GetPowerShell ( System.Collections.Generic.Dictionary<string, object> variables, out System.Collections.Generic.Dictionary<string,object>usingVariables, object[] args ) { usingVariables = default(System.Collections.Generic.Dictionary<string,object>); return default(System.Management.Automation.PowerShell); }
    public System.Management.Automation.SteppablePipeline GetSteppablePipeline ( System.Management.Automation.CommandOrigin commandOrigin ) { return default(System.Management.Automation.SteppablePipeline); }
    public System.Management.Automation.SteppablePipeline GetSteppablePipeline ( System.Management.Automation.CommandOrigin commandOrigin, object[] args ) { return default(System.Management.Automation.SteppablePipeline); }
    public System.Management.Automation.SteppablePipeline GetSteppablePipeline (  ) { return default(System.Management.Automation.SteppablePipeline); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Invoke ( object[] args ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public object InvokeReturnAsIs ( object[] args ) { return default(object); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> InvokeWithContext ( System.Collections.Generic.Dictionary<string, System.Management.Automation.ScriptBlock> functionsToDefine, System.Collections.Generic.List<System.Management.Automation.PSVariable> variablesToDefine, object[] args ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> InvokeWithContext ( System.Collections.IDictionary functionsToDefine, System.Collections.Generic.List<System.Management.Automation.PSVariable> variablesToDefine, object[] args ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public override string ToString (  ) { return default(string); }

  }

    [System.SerializableAttribute]
   public class ScriptBlockToPowerShellNotSupportedException : System.Management.Automation.RuntimeException {
    public ScriptBlockToPowerShellNotSupportedException() { }
    public ScriptBlockToPowerShellNotSupportedException(string message) { }
    public ScriptBlockToPowerShellNotSupportedException(string message, System.Exception innerException) { }
    protected ScriptBlockToPowerShellNotSupportedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

  }

    [System.SerializableAttribute]
   public class ScriptCallDepthException : System.SystemException, System.Management.Automation.IContainsErrorRecord {
    public ScriptCallDepthException() { }
    public ScriptCallDepthException(string message) { }
    public ScriptCallDepthException(string message, System.Exception innerException) { }
    protected ScriptCallDepthException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

    public int CallDepth { get { return default(int); } }
    public System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

  }

  public class ScriptInfo : System.Management.Automation.CommandInfo {
    internal ScriptInfo() { }
    public override string Definition { get { return default(string); } }
    public override System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName> OutputType { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSTypeName>); } }
    public System.Management.Automation.ScriptBlock ScriptBlock { get { return default(System.Management.Automation.ScriptBlock); } set { } }
    public override string ToString (  ) { return default(string); }

  }

    [System.SerializableAttribute]
   public class ScriptRequiresException : System.Management.Automation.RuntimeException {
    public ScriptRequiresException() { }
    public ScriptRequiresException(string message) { }
    public ScriptRequiresException(string message, System.Exception innerException) { }
    protected ScriptRequiresException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

    public string CommandName { get { return default(string); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<string> MissingPSSnapIns { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<string>); } }
    public System.Version RequiresPSVersion { get { return default(System.Version); } }
    public string RequiresShellId { get { return default(string); } }
    public string RequiresShellPath { get { return default(string); } }
    public override void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

  }

  public sealed class SecurityDescriptorCmdletProviderIntrinsics {
    internal SecurityDescriptorCmdletProviderIntrinsics() { }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Get ( string path, System.Security.AccessControl.AccessControlSections includeSections ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public System.Security.AccessControl.ObjectSecurity NewFromPath ( string path, System.Security.AccessControl.AccessControlSections includeSections ) { return default(System.Security.AccessControl.ObjectSecurity); }
    public System.Security.AccessControl.ObjectSecurity NewOfType ( string providerId, string type, System.Security.AccessControl.AccessControlSections includeSections ) { return default(System.Security.AccessControl.ObjectSecurity); }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Set ( string path, System.Security.AccessControl.ObjectSecurity sd ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }

  }

  public sealed class SemanticVersion : System.IComparable, System.IComparable<System.Management.Automation.SemanticVersion>, System.IEquatable<System.Management.Automation.SemanticVersion> {
    public SemanticVersion(string version) { }
    public SemanticVersion(int major, int minor, int patch, string preReleaseLabel, string buildLabel) { }
    public SemanticVersion(int major, int minor, int patch, string label) { }
    public SemanticVersion(int major, int minor, int patch) { }
    public SemanticVersion(int major, int minor) { }
    public SemanticVersion(int major) { }
    public SemanticVersion(System.Version version) { }

    public string BuildLabel { get { return default(string); } }
    public int Major { get { return default(int); } }
    public int Minor { get { return default(int); } }
    public int Patch { get { return default(int); } }
    public string PreReleaseLabel { get { return default(string); } }
    public static int Compare ( System.Management.Automation.SemanticVersion versionA, System.Management.Automation.SemanticVersion versionB ) { return default(int); }
    public int CompareTo ( System.Management.Automation.SemanticVersion value ) { return default(int); }
    public int CompareTo ( object version ) { return default(int); }
    public override bool Equals ( object obj ) { return default(bool); }
    public bool Equals ( System.Management.Automation.SemanticVersion other ) { return default(bool); }
    public override int GetHashCode (  ) { return default(int); }
    public static bool operator == ( System.Management.Automation.SemanticVersion v1, System.Management.Automation.SemanticVersion v2 ) { return default(bool); }
    public static bool operator > ( System.Management.Automation.SemanticVersion v1, System.Management.Automation.SemanticVersion v2 ) { return default(bool); }
    public static bool operator >= ( System.Management.Automation.SemanticVersion v1, System.Management.Automation.SemanticVersion v2 ) { return default(bool); }
    public static implicit operator System.Version ( System.Management.Automation.SemanticVersion semver ) { return default(System.Version); }
    public static bool operator != ( System.Management.Automation.SemanticVersion v1, System.Management.Automation.SemanticVersion v2 ) { return default(bool); }
    public static bool operator < ( System.Management.Automation.SemanticVersion v1, System.Management.Automation.SemanticVersion v2 ) { return default(bool); }
    public static bool operator <= ( System.Management.Automation.SemanticVersion v1, System.Management.Automation.SemanticVersion v2 ) { return default(bool); }
    public static System.Management.Automation.SemanticVersion Parse ( string version ) { return default(System.Management.Automation.SemanticVersion); }
    public override string ToString (  ) { return default(string); }
    public static bool TryParse ( string version, out System.Management.Automation.SemanticVersion result ) { result = default(System.Management.Automation.SemanticVersion); return default(bool); }

  }

    [System.FlagsAttribute]
   public enum SessionCapabilities {
    Language = 4,
    RemoteServer = 1,
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
    public static bool IsVisible ( System.Management.Automation.CommandOrigin origin, System.Management.Automation.PSVariable variable ) { return default(bool); }
    public static bool IsVisible ( System.Management.Automation.CommandOrigin origin, object valueToCheck ) { return default(bool); }
    public static bool IsVisible ( System.Management.Automation.CommandOrigin origin, System.Management.Automation.CommandInfo commandInfo ) { return default(bool); }
    public static void ThrowIfNotVisible ( System.Management.Automation.CommandOrigin origin, object valueToCheck ) { }

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

    [System.SerializableAttribute]
   public class SessionStateException : System.Management.Automation.RuntimeException {
    public SessionStateException() { }
    public SessionStateException(string message) { }
    public SessionStateException(string message, System.Exception innerException) { }
    protected SessionStateException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

    public override System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public string ItemName { get { return default(string); } }
    public System.Management.Automation.SessionStateCategory SessionStateCategory { get { return default(System.Management.Automation.SessionStateCategory); } }
    public override void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

  }

    [System.SerializableAttribute]
   public class SessionStateUnauthorizedAccessException : System.Management.Automation.SessionStateException {
    public SessionStateUnauthorizedAccessException() { }
    public SessionStateUnauthorizedAccessException(string message) { }
    public SessionStateUnauthorizedAccessException(string message, System.Exception innerException) { }
    protected SessionStateUnauthorizedAccessException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

  }

  public class SettingValueExceptionEventArgs : System.EventArgs {
    internal SettingValueExceptionEventArgs() { }
    public System.Exception Exception { get { return default(System.Exception); } }
    public bool ShouldThrow { get { return default(bool); } set { } }
  }

    [System.SerializableAttribute]
   public class SetValueException : System.Management.Automation.ExtendedTypeSystemException {
    public SetValueException() { }
    public SetValueException(string message) { }
    public SetValueException(string message, System.Exception innerException) { }
    protected SetValueException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

  }

    [System.SerializableAttribute]
   public class SetValueInvocationException : System.Management.Automation.SetValueException {
    public SetValueInvocationException() { }
    public SetValueInvocationException(string message) { }
    public SetValueInvocationException(string message, System.Exception innerException) { }
    protected SetValueInvocationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

  }

    [System.FlagsAttribute]
   public enum ShouldProcessReason {
    None = 0,
    WhatIf = 1,
  }

  public sealed class Signature {
    internal Signature() { }
    public bool IsOSBinary { get { return default(bool); } set { } }
    public string Path { get { return default(string); } }
    public System.Management.Automation.SignatureType SignatureType { get { return default(System.Management.Automation.SignatureType); } set { } }
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

  public enum SignatureType {
    Authenticode = 1,
    Catalog = 2,
    None = 0,
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

  public sealed class StartRunspaceDebugProcessingEventArgs : System.EventArgs {
    public StartRunspaceDebugProcessingEventArgs(System.Management.Automation.Runspaces.Runspace runspace) { }

    public System.Management.Automation.Runspaces.Runspace Runspace { get { return default(System.Management.Automation.Runspaces.Runspace); } set { } }
    public bool UseDefaultProcessing { get { return default(bool); } set { } }
  }

  public sealed class SteppablePipeline : System.IDisposable {
    internal SteppablePipeline() { }
    public void Begin ( bool expectInput ) { }
    public void Begin ( bool expectInput, System.Management.Automation.EngineIntrinsics contextToRedirectTo ) { }
    public void Begin ( System.Management.Automation.Internal.InternalCommand command ) { }
    public void Dispose (  ) { }
    public System.Array End (  ) { return default(System.Array); }
    ~SteppablePipeline() { }
    public System.Array Process ( object input ) { return default(System.Array); }
    public System.Array Process ( System.Management.Automation.PSObject input ) { return default(System.Array); }
    public System.Array Process (  ) { return default(System.Array); }

  }

   [System.AttributeUsageAttribute((System.AttributeTargets)384)]
   public sealed class SupportsWildcardsAttribute : System.Management.Automation.Internal.ParsingBaseAttribute {
    public SupportsWildcardsAttribute() { }

  }

  public partial struct SwitchParameter {
    public SwitchParameter(bool isPresent) { }

    public bool IsPresent { get { return default(bool); } }
    public System.Management.Automation.SwitchParameter Present { get { return default(System.Management.Automation.SwitchParameter); } }
    public override bool Equals ( object obj ) { return default(bool); }
    public override int GetHashCode (  ) { return default(int); }
    public static bool operator == ( System.Management.Automation.SwitchParameter first, System.Management.Automation.SwitchParameter second ) { return default(bool); }
    public static bool operator == ( System.Management.Automation.SwitchParameter first, bool second ) { return default(bool); }
    public static bool operator == ( bool first, System.Management.Automation.SwitchParameter second ) { return default(bool); }
    public static implicit operator bool ( System.Management.Automation.SwitchParameter switchParameter ) { return default(bool); }
    public static implicit operator System.Management.Automation.SwitchParameter ( bool value ) { return default(System.Management.Automation.SwitchParameter); }
    public static bool operator != ( System.Management.Automation.SwitchParameter first, System.Management.Automation.SwitchParameter second ) { return default(bool); }
    public static bool operator != ( System.Management.Automation.SwitchParameter first, bool second ) { return default(bool); }
    public static bool operator != ( bool first, System.Management.Automation.SwitchParameter second ) { return default(bool); }
    public bool ToBool (  ) { return default(bool); }
    public override string ToString (  ) { return default(string); }

  }

  public sealed class TableControl : System.Management.Automation.PSControl {
    public TableControl() { }
    public TableControl(System.Management.Automation.TableControlRow tableControlRow) { }
    public TableControl(System.Management.Automation.TableControlRow tableControlRow, System.Collections.Generic.IEnumerable<System.Management.Automation.TableControlColumnHeader> tableControlColumnHeaders) { }

    public bool AutoSize { get { return default(bool); } set { } }
    public System.Collections.Generic.List<System.Management.Automation.TableControlColumnHeader> Headers { get { return default(System.Collections.Generic.List<System.Management.Automation.TableControlColumnHeader>); } set { } }
    public bool HideTableHeaders { get { return default(bool); } set { } }
    public System.Collections.Generic.List<System.Management.Automation.TableControlRow> Rows { get { return default(System.Collections.Generic.List<System.Management.Automation.TableControlRow>); } set { } }
    public static System.Management.Automation.TableControlBuilder Create ( bool outOfBand, bool autoSize, bool hideTableHeaders ) { return default(System.Management.Automation.TableControlBuilder); }

  }

  public sealed class TableControlBuilder {
    public System.Management.Automation.TableControlBuilder AddHeader ( System.Management.Automation.Alignment alignment, int width, string label ) { return default(System.Management.Automation.TableControlBuilder); }
    public System.Management.Automation.TableControl EndTable (  ) { return default(System.Management.Automation.TableControl); }
    public System.Management.Automation.TableControlBuilder GroupByProperty ( string property, System.Management.Automation.CustomControl customControl, string label ) { return default(System.Management.Automation.TableControlBuilder); }
    public System.Management.Automation.TableControlBuilder GroupByScriptBlock ( string scriptBlock, System.Management.Automation.CustomControl customControl, string label ) { return default(System.Management.Automation.TableControlBuilder); }
    public System.Management.Automation.TableRowDefinitionBuilder StartRowDefinition ( bool wrap, System.Collections.Generic.IEnumerable<string> entrySelectedByType, System.Collections.Generic.IEnumerable<System.Management.Automation.DisplayEntry> entrySelectedByCondition ) { return default(System.Management.Automation.TableRowDefinitionBuilder); }

  }

  public sealed class TableControlColumn {
    public TableControlColumn() { }
    public TableControlColumn(System.Management.Automation.Alignment alignment, System.Management.Automation.DisplayEntry entry) { }

    public System.Management.Automation.Alignment Alignment { get { return default(System.Management.Automation.Alignment); } set { } }
    public System.Management.Automation.DisplayEntry DisplayEntry { get { return default(System.Management.Automation.DisplayEntry); } set { } }
    public string FormatString { get { return default(string); } set { } }
    public override string ToString (  ) { return default(string); }

  }

  public sealed class TableControlColumnHeader {
    public TableControlColumnHeader() { }
    public TableControlColumnHeader(string label, int width, System.Management.Automation.Alignment alignment) { }

    public System.Management.Automation.Alignment Alignment { get { return default(System.Management.Automation.Alignment); } set { } }
    public string Label { get { return default(string); } set { } }
    public int Width { get { return default(int); } set { } }
  }

  public sealed class TableControlRow {
    public TableControlRow() { }
    public TableControlRow(System.Collections.Generic.IEnumerable<System.Management.Automation.TableControlColumn> columns) { }

    public System.Collections.Generic.List<System.Management.Automation.TableControlColumn> Columns { get { return default(System.Collections.Generic.List<System.Management.Automation.TableControlColumn>); } set { } }
    public System.Management.Automation.EntrySelectedBy SelectedBy { get { return default(System.Management.Automation.EntrySelectedBy); } set { } }
    public bool Wrap { get { return default(bool); } set { } }
  }

  public sealed class TableRowDefinitionBuilder {
    public System.Management.Automation.TableRowDefinitionBuilder AddPropertyColumn ( string propertyName, System.Management.Automation.Alignment alignment, string format ) { return default(System.Management.Automation.TableRowDefinitionBuilder); }
    public System.Management.Automation.TableRowDefinitionBuilder AddScriptBlockColumn ( string scriptBlock, System.Management.Automation.Alignment alignment, string format ) { return default(System.Management.Automation.TableRowDefinitionBuilder); }
    public System.Management.Automation.TableControlBuilder EndRowDefinition (  ) { return default(System.Management.Automation.TableControlBuilder); }

  }

  public sealed class TerminateException : System.Management.Automation.FlowControlException {
    public TerminateException() { }

  }

  public enum TypeInferenceRuntimePermissions {
    AllowSafeEval = 1,
    None = 0,
  }

   [System.AttributeUsageAttribute((System.AttributeTargets)384)]
   public abstract class ValidateArgumentsAttribute : System.Management.Automation.Internal.CmdletMetadataAttribute {
    protected ValidateArgumentsAttribute() { }

    protected virtual void Validate ( object arguments, System.Management.Automation.EngineIntrinsics engineIntrinsics ) { }

  }

   [System.AttributeUsageAttribute((System.AttributeTargets)384)]
   public sealed class ValidateCountAttribute : System.Management.Automation.ValidateArgumentsAttribute {
    public ValidateCountAttribute(int minLength, int maxLength) { }

    public int MaxLength { get { return default(int); } }
    public int MinLength { get { return default(int); } }
    protected override void Validate ( object arguments, System.Management.Automation.EngineIntrinsics engineIntrinsics ) { }

  }

   [System.AttributeUsageAttribute((System.AttributeTargets)384)]
   public class ValidateDriveAttribute : System.Management.Automation.ValidateArgumentsAttribute {
    public ValidateDriveAttribute(string[] validRootDrives) { }

    public System.Collections.Generic.IList<string> ValidRootDrives { get { return default(System.Collections.Generic.IList<string>); } }
    protected override void Validate ( object arguments, System.Management.Automation.EngineIntrinsics engineIntrinsics ) { }

  }

   [System.AttributeUsageAttribute((System.AttributeTargets)384)]
   public abstract class ValidateEnumeratedArgumentsAttribute : System.Management.Automation.ValidateArgumentsAttribute {
    protected ValidateEnumeratedArgumentsAttribute() { }

    protected override void Validate ( object arguments, System.Management.Automation.EngineIntrinsics engineIntrinsics ) { }
    protected virtual void ValidateElement ( object element ) { }

  }

   [System.AttributeUsageAttribute((System.AttributeTargets)384)]
   public sealed class ValidateLengthAttribute : System.Management.Automation.ValidateEnumeratedArgumentsAttribute {
    public ValidateLengthAttribute(int minLength, int maxLength) { }

    public int MaxLength { get { return default(int); } }
    public int MinLength { get { return default(int); } }
    protected override void ValidateElement ( object element ) { }

  }

   [System.AttributeUsageAttribute((System.AttributeTargets)384)]
   public sealed class ValidateNotNullAttribute : System.Management.Automation.ValidateArgumentsAttribute {
    public ValidateNotNullAttribute() { }

    protected override void Validate ( object arguments, System.Management.Automation.EngineIntrinsics engineIntrinsics ) { }

  }

   [System.AttributeUsageAttribute((System.AttributeTargets)384)]
   public sealed class ValidateNotNullOrEmptyAttribute : System.Management.Automation.ValidateArgumentsAttribute {
    public ValidateNotNullOrEmptyAttribute() { }

    protected override void Validate ( object arguments, System.Management.Automation.EngineIntrinsics engineIntrinsics ) { }

  }

   [System.AttributeUsageAttribute((System.AttributeTargets)384)]
   public sealed class ValidatePatternAttribute : System.Management.Automation.ValidateEnumeratedArgumentsAttribute {
    public ValidatePatternAttribute(string regexPattern) { }

    public string ErrorMessage { get { return default(string); } set { } }
    public System.Text.RegularExpressions.RegexOptions Options { get { return default(System.Text.RegularExpressions.RegexOptions); } set { } }
    public string RegexPattern { get { return default(string); } }
    protected override void ValidateElement ( object element ) { }

  }

   [System.AttributeUsageAttribute((System.AttributeTargets)384)]
   public sealed class ValidateRangeAttribute : System.Management.Automation.ValidateEnumeratedArgumentsAttribute {
    public ValidateRangeAttribute(object minRange, object maxRange) { }
    public ValidateRangeAttribute(System.Management.Automation.ValidateRangeKind kind) { }

    public object MaxRange { get { return default(object); } }
    public object MinRange { get { return default(object); } }
    protected override void ValidateElement ( object element ) { }

  }

  public enum ValidateRangeKind {
    Negative = 2,
    NonNegative = 1,
    NonPositive = 3,
    Positive = 0,
  }

  public sealed class ValidateScriptAttribute : System.Management.Automation.ValidateEnumeratedArgumentsAttribute {
    public ValidateScriptAttribute(System.Management.Automation.ScriptBlock scriptBlock) { }

    public string ErrorMessage { get { return default(string); } set { } }
    public System.Management.Automation.ScriptBlock ScriptBlock { get { return default(System.Management.Automation.ScriptBlock); } }
    protected override void ValidateElement ( object element ) { }

  }

   [System.AttributeUsageAttribute((System.AttributeTargets)384)]
   public sealed class ValidateSetAttribute : System.Management.Automation.ValidateEnumeratedArgumentsAttribute {
    public ValidateSetAttribute(string[] validValues) { }
    public ValidateSetAttribute(System.Type valuesGeneratorType) { }

    public string ErrorMessage { get { return default(string); } set { } }
    public bool IgnoreCase { get { return default(bool); } set { } }
    public System.Collections.Generic.IList<string> ValidValues { get { return default(System.Collections.Generic.IList<string>); } }
    protected override void ValidateElement ( object element ) { }

  }

   [System.AttributeUsageAttribute((System.AttributeTargets)384)]
   public sealed class ValidateUserDriveAttribute : System.Management.Automation.ValidateDriveAttribute {
    public ValidateUserDriveAttribute() : base (default(string[])) { }

  }

    [System.SerializableAttribute]
   public class ValidationMetadataException : System.Management.Automation.MetadataException {
    protected ValidationMetadataException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
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
    public System.Management.Automation.VariableAccessMode AccessMode { get { return default(System.Management.Automation.VariableAccessMode); } set { } }
    public string Variable { get { return default(string); } set { } }
    public override string ToString (  ) { return default(string); }

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
    public override string ToString (  ) { return default(string); }

  }

  public class VerbInfo {
    public VerbInfo() { }

    public string AliasPrefix { get { return default(string); } set { } }
    public string Description { get { return default(string); } set { } }
    public string Group { get { return default(string); } set { } }
    public string Verb { get { return default(string); } set { } }
  }

    [System.Runtime.Serialization.DataContractAttribute]
   public class VerboseRecord : System.Management.Automation.InformationalRecord {
    public VerboseRecord(string message) { }
    public VerboseRecord(System.Management.Automation.PSObject record) { }

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
    public const string Build = "Build";
    public const string Complete = "Complete";
    public const string Confirm = "Confirm";
    public const string Deny = "Deny";
    public const string Deploy = "Deploy";
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
    public WarningRecord(string message) { }
    public WarningRecord(System.Management.Automation.PSObject record) { }
    public WarningRecord(string fullyQualifiedWarningId, string message) { }
    public WarningRecord(string fullyQualifiedWarningId, System.Management.Automation.PSObject record) { }

    public string FullyQualifiedWarningId { get { return default(string); } }
  }

  public enum WhereOperatorSelectionMode {
    Default = 0,
    First = 1,
    Last = 2,
    SkipUntil = 3,
    Split = 5,
    Until = 4,
  }

  public sealed class WideControl : System.Management.Automation.PSControl {
    public WideControl() { }
    public WideControl(System.Collections.Generic.IEnumerable<System.Management.Automation.WideControlEntryItem> wideEntries) { }
    public WideControl(System.Collections.Generic.IEnumerable<System.Management.Automation.WideControlEntryItem> wideEntries, uint columns) { }
    public WideControl(uint columns) { }

    public bool AutoSize { get { return default(bool); } set { } }
    public uint Columns { get { return default(uint); } set { } }
    public System.Collections.Generic.List<System.Management.Automation.WideControlEntryItem> Entries { get { return default(System.Collections.Generic.List<System.Management.Automation.WideControlEntryItem>); } set { } }
    public static System.Management.Automation.WideControlBuilder Create ( bool outOfBand, bool autoSize, uint columns ) { return default(System.Management.Automation.WideControlBuilder); }

  }

  public sealed class WideControlBuilder {
    public System.Management.Automation.WideControlBuilder AddPropertyEntry ( string propertyName, string format, System.Collections.Generic.IEnumerable<string> entrySelectedByType, System.Collections.Generic.IEnumerable<System.Management.Automation.DisplayEntry> entrySelectedByCondition ) { return default(System.Management.Automation.WideControlBuilder); }
    public System.Management.Automation.WideControlBuilder AddScriptBlockEntry ( string scriptBlock, string format, System.Collections.Generic.IEnumerable<string> entrySelectedByType, System.Collections.Generic.IEnumerable<System.Management.Automation.DisplayEntry> entrySelectedByCondition ) { return default(System.Management.Automation.WideControlBuilder); }
    public System.Management.Automation.WideControl EndWideControl (  ) { return default(System.Management.Automation.WideControl); }
    public System.Management.Automation.WideControlBuilder GroupByProperty ( string property, System.Management.Automation.CustomControl customControl, string label ) { return default(System.Management.Automation.WideControlBuilder); }
    public System.Management.Automation.WideControlBuilder GroupByScriptBlock ( string scriptBlock, System.Management.Automation.CustomControl customControl, string label ) { return default(System.Management.Automation.WideControlBuilder); }

  }

  public sealed class WideControlEntryItem {
    public WideControlEntryItem(System.Management.Automation.DisplayEntry entry) { }
    public WideControlEntryItem(System.Management.Automation.DisplayEntry entry, System.Collections.Generic.IEnumerable<string> selectedBy) { }

    public System.Management.Automation.DisplayEntry DisplayEntry { get { return default(System.Management.Automation.DisplayEntry); } set { } }
    public System.Management.Automation.EntrySelectedBy EntrySelectedBy { get { return default(System.Management.Automation.EntrySelectedBy); } set { } }
    public string FormatString { get { return default(string); } set { } }
    public System.Collections.Generic.List<string> SelectedBy { get { return default(System.Collections.Generic.List<string>); } }
  }

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

    public static bool ContainsWildcardCharacters ( string pattern ) { return default(bool); }
    public static string Escape ( string pattern ) { return default(string); }
    public static System.Management.Automation.WildcardPattern Get ( string pattern, System.Management.Automation.WildcardOptions options ) { return default(System.Management.Automation.WildcardPattern); }
    public bool IsMatch ( string input ) { return default(bool); }
    public string ToWql (  ) { return default(string); }
    public static string Unescape ( string pattern ) { return default(string); }

  }

    [System.SerializableAttribute]
   public class WildcardPatternException : System.Management.Automation.RuntimeException {
    public WildcardPatternException() { }
    public WildcardPatternException(string message) { }
    public WildcardPatternException(string message, System.Exception innerException) { }
    protected WildcardPatternException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

  }

#if WORKFLOW
  public class WorkflowInfo : System.Management.Automation.FunctionInfo {
    public WorkflowInfo(string name, string definition, System.Management.Automation.ScriptBlock workflow, string xamlDefinition, System.Management.Automation.WorkflowInfo[] workflowsCalled) { }
    public WorkflowInfo(string name, string definition, System.Management.Automation.ScriptBlock workflow, string xamlDefinition, System.Management.Automation.WorkflowInfo[] workflowsCalled, System.Management.Automation.PSModuleInfo module) { }

    public override string Definition { get { return default(string); } }
    public string NestedXamlDefinition { get { return default(string); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.WorkflowInfo> WorkflowsCalled { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.WorkflowInfo>); } }
    public string XamlDefinition { get { return default(string); } set { } }
    protected internal void Update ( System.Management.Automation.FunctionInfo function, bool force, System.Management.Automation.ScopedItemOptions options, string helpFile ) { }

  }
#endif

}
namespace System.Management.Automation.Provider {
  public abstract class CmdletProvider {
    protected CmdletProvider() { }

    public System.Management.Automation.PSCredential Credential { get { return default(System.Management.Automation.PSCredential); } }
    public System.Management.Automation.PSTransactionContext CurrentPSTransaction { get { return default(System.Management.Automation.PSTransactionContext); } }
    public System.Collections.ObjectModel.Collection<string> Exclude { get { return default(System.Collections.ObjectModel.Collection<string>); } }
    public string Filter { get { return default(string); } }
    public System.Management.Automation.SwitchParameter Force { get { return default(System.Management.Automation.SwitchParameter); } }
    public System.Management.Automation.Host.PSHost Host { get { return default(System.Management.Automation.Host.PSHost); } }
    public System.Collections.ObjectModel.Collection<string> Include { get { return default(System.Collections.ObjectModel.Collection<string>); } }
    public System.Management.Automation.CommandInvocationIntrinsics InvokeCommand { get { return default(System.Management.Automation.CommandInvocationIntrinsics); } }
    public System.Management.Automation.ProviderIntrinsics InvokeProvider { get { return default(System.Management.Automation.ProviderIntrinsics); } }
    public System.Management.Automation.SessionState SessionState { get { return default(System.Management.Automation.SessionState); } }
    public bool Stopping { get { return default(bool); } }
    public virtual string GetResourceString ( string baseName, string resourceId ) { return default(string); }
    public bool ShouldContinue ( string query, string caption, ref bool yesToAll, ref bool noToAll ) { return default(bool); }
    public bool ShouldContinue ( string query, string caption ) { return default(bool); }
    public bool ShouldProcess ( string target ) { return default(bool); }
    public bool ShouldProcess ( string target, string action ) { return default(bool); }
    public bool ShouldProcess ( string verboseDescription, string verboseWarning, string caption ) { return default(bool); }
    public bool ShouldProcess ( string verboseDescription, string verboseWarning, string caption, out System.Management.Automation.ShouldProcessReason shouldProcessReason ) { shouldProcessReason = default(System.Management.Automation.ShouldProcessReason); return default(bool); }
    protected virtual System.Management.Automation.ProviderInfo Start ( System.Management.Automation.ProviderInfo providerInfo ) { return default(System.Management.Automation.ProviderInfo); }
    protected virtual object StartDynamicParameters (  ) { return default(object); }
    protected virtual void Stop (  ) { }
    protected internal virtual void StopProcessing (  ) { }
    public void ThrowTerminatingError ( System.Management.Automation.ErrorRecord errorRecord ) { }
    public bool TransactionAvailable (  ) { return default(bool); }
    public void WriteDebug ( string text ) { }
    public void WriteError ( System.Management.Automation.ErrorRecord errorRecord ) { }
    public void WriteInformation ( System.Management.Automation.InformationRecord record ) { }
    public void WriteInformation ( object messageData, string[] tags ) { }
    public void WriteItemObject ( object item, string path, bool isContainer ) { }
    public void WriteProgress ( System.Management.Automation.ProgressRecord progressRecord ) { }
    public void WritePropertyObject ( object propertyValue, string path ) { }
    public void WriteSecurityDescriptorObject ( System.Security.AccessControl.ObjectSecurity securityDescriptor, string path ) { }
    public void WriteVerbose ( string text ) { }
    public void WriteWarning ( string text ) { }

  }

   [System.AttributeUsageAttribute((System.AttributeTargets)4, AllowMultiple = false, Inherited = false)]
   public sealed class CmdletProviderAttribute : System.Attribute {
    public CmdletProviderAttribute(string providerName, System.Management.Automation.Provider.ProviderCapabilities providerCapabilities) { }

    public System.Management.Automation.Provider.ProviderCapabilities ProviderCapabilities { get { return default(System.Management.Automation.Provider.ProviderCapabilities); } }
    public string ProviderName { get { return default(string); } }
  }

  public abstract class ContainerCmdletProvider : System.Management.Automation.Provider.ItemCmdletProvider {
    protected ContainerCmdletProvider() { }

    protected virtual bool ConvertPath ( string path, string filter, ref string updatedPath, ref string updatedFilter ) { return default(bool); }
    protected virtual void CopyItem ( string path, string copyPath, bool recurse ) { }
    protected virtual object CopyItemDynamicParameters ( string path, string destination, bool recurse ) { return default(object); }
    protected virtual void GetChildItems ( string path, bool recurse, uint depth ) { }
    protected virtual void GetChildItems ( string path, bool recurse ) { }
    protected virtual object GetChildItemsDynamicParameters ( string path, bool recurse ) { return default(object); }
    protected virtual void GetChildNames ( string path, System.Management.Automation.ReturnContainers returnContainers ) { }
    protected virtual object GetChildNamesDynamicParameters ( string path ) { return default(object); }
    protected virtual bool HasChildItems ( string path ) { return default(bool); }
    protected virtual void NewItem ( string path, string itemTypeName, object newItemValue ) { }
    protected virtual object NewItemDynamicParameters ( string path, string itemTypeName, object newItemValue ) { return default(object); }
    protected virtual void RemoveItem ( string path, bool recurse ) { }
    protected virtual object RemoveItemDynamicParameters ( string path, bool recurse ) { return default(object); }
    protected virtual void RenameItem ( string path, string newName ) { }
    protected virtual object RenameItemDynamicParameters ( string path, string newName ) { return default(object); }

  }

  public abstract class DriveCmdletProvider : System.Management.Automation.Provider.CmdletProvider {
    protected DriveCmdletProvider() { }

    protected virtual System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo> InitializeDefaultDrives (  ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSDriveInfo>); }
    protected virtual System.Management.Automation.PSDriveInfo NewDrive ( System.Management.Automation.PSDriveInfo drive ) { return default(System.Management.Automation.PSDriveInfo); }
    protected virtual object NewDriveDynamicParameters (  ) { return default(object); }
    protected virtual System.Management.Automation.PSDriveInfo RemoveDrive ( System.Management.Automation.PSDriveInfo drive ) { return default(System.Management.Automation.PSDriveInfo); }

  }

  public partial interface ICmdletProviderSupportsHelp {
     string GetHelpMaml ( string helpItemName, string path );

  }

  public partial interface IContentCmdletProvider {
     void ClearContent ( string path );
     object ClearContentDynamicParameters ( string path );
     System.Management.Automation.Provider.IContentReader GetContentReader ( string path );
     object GetContentReaderDynamicParameters ( string path );
     System.Management.Automation.Provider.IContentWriter GetContentWriter ( string path );
     object GetContentWriterDynamicParameters ( string path );

  }

  public partial interface IContentReader : System.IDisposable {
     void Close (  );
     System.Collections.IList Read ( System.Int64 readCount );
     void Seek ( System.Int64 offset, System.IO.SeekOrigin origin );

  }

  public partial interface IContentWriter : System.IDisposable {
     void Close (  );
     void Seek ( System.Int64 offset, System.IO.SeekOrigin origin );
     System.Collections.IList Write ( System.Collections.IList content );

  }

  public partial interface IDynamicPropertyCmdletProvider {
     void CopyProperty ( string sourcePath, string sourceProperty, string destinationPath, string destinationProperty );
     object CopyPropertyDynamicParameters ( string sourcePath, string sourceProperty, string destinationPath, string destinationProperty );
     void MoveProperty ( string sourcePath, string sourceProperty, string destinationPath, string destinationProperty );
     object MovePropertyDynamicParameters ( string sourcePath, string sourceProperty, string destinationPath, string destinationProperty );
     void NewProperty ( string path, string propertyName, string propertyTypeName, object value );
     object NewPropertyDynamicParameters ( string path, string propertyName, string propertyTypeName, object value );
     void RemoveProperty ( string path, string propertyName );
     object RemovePropertyDynamicParameters ( string path, string propertyName );
     void RenameProperty ( string path, string sourceProperty, string destinationProperty );
     object RenamePropertyDynamicParameters ( string path, string sourceProperty, string destinationProperty );

  }

  public partial interface IPropertyCmdletProvider {
     void ClearProperty ( string path, System.Collections.ObjectModel.Collection<string> propertyToClear );
     object ClearPropertyDynamicParameters ( string path, System.Collections.ObjectModel.Collection<string> propertyToClear );
     void GetProperty ( string path, System.Collections.ObjectModel.Collection<string> providerSpecificPickList );
     object GetPropertyDynamicParameters ( string path, System.Collections.ObjectModel.Collection<string> providerSpecificPickList );
     void SetProperty ( string path, System.Management.Automation.PSObject propertyValue );
     object SetPropertyDynamicParameters ( string path, System.Management.Automation.PSObject propertyValue );

  }

  public partial interface ISecurityDescriptorCmdletProvider {
     void GetSecurityDescriptor ( string path, System.Security.AccessControl.AccessControlSections includeSections );
     System.Security.AccessControl.ObjectSecurity NewSecurityDescriptorFromPath ( string path, System.Security.AccessControl.AccessControlSections includeSections );
     System.Security.AccessControl.ObjectSecurity NewSecurityDescriptorOfType ( string type, System.Security.AccessControl.AccessControlSections includeSections );
     void SetSecurityDescriptor ( string path, System.Security.AccessControl.ObjectSecurity securityDescriptor );

  }

  public abstract class ItemCmdletProvider : System.Management.Automation.Provider.DriveCmdletProvider {
    protected ItemCmdletProvider() { }

    protected virtual void ClearItem ( string path ) { }
    protected virtual object ClearItemDynamicParameters ( string path ) { return default(object); }
    protected virtual string[] ExpandPath ( string path ) { return default(string[]); }
    protected virtual void GetItem ( string path ) { }
    protected virtual object GetItemDynamicParameters ( string path ) { return default(object); }
    protected virtual void InvokeDefaultAction ( string path ) { }
    protected virtual object InvokeDefaultActionDynamicParameters ( string path ) { return default(object); }
    protected virtual bool IsValidPath ( string path ) { return default(bool); }
    protected virtual bool ItemExists ( string path ) { return default(bool); }
    protected virtual object ItemExistsDynamicParameters ( string path ) { return default(object); }
    protected virtual void SetItem ( string path, object value ) { }
    protected virtual object SetItemDynamicParameters ( string path, object value ) { return default(object); }

  }

  public abstract class NavigationCmdletProvider : System.Management.Automation.Provider.ContainerCmdletProvider {
    protected NavigationCmdletProvider() { }

    protected virtual string GetChildName ( string path ) { return default(string); }
    protected virtual string GetParentPath ( string path, string root ) { return default(string); }
    protected virtual bool IsItemContainer ( string path ) { return default(bool); }
    protected virtual string MakePath ( string parent, string child ) { return default(string); }
    protected virtual void MoveItem ( string path, string destination ) { }
    protected virtual object MoveItemDynamicParameters ( string path, string destination ) { return default(object); }
    protected virtual string NormalizeRelativePath ( string path, string basePath ) { return default(string); }

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
  public class CmdletMethodInvoker<T> {
    public CmdletMethodInvoker() { }

    public System.Func<System.Management.Automation.Cmdlet, T> Action { get { return default(System.Func<System.Management.Automation.Cmdlet, T>); } set { } }
    public System.Exception ExceptionThrownOnCmdletThread { get { return default(System.Exception); } set { } }
    public System.Threading.ManualResetEventSlim Finished { get { return default(System.Threading.ManualResetEventSlim); } set { } }
    public T MethodResult { get { return default(T); } set { } }
    public object SyncObject { get { return default(object); } set { } }
  }

    [System.SerializableAttribute]
 [System.Runtime.Serialization.DataContractAttribute]
   public class OriginInfo {
    public OriginInfo(string computerName, System.Guid runspaceID) { }
    public OriginInfo(string computerName, System.Guid runspaceID, System.Guid instanceID) { }

    public System.Guid InstanceID { get { return default(System.Guid); } set { } }
    public string PSComputerName { get { return default(string); } }
    public System.Guid RunspaceID { get { return default(System.Guid); } }
    public override string ToString (  ) { return default(string); }

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

    [System.SerializableAttribute]
   public class PSDirectException : System.Management.Automation.RuntimeException {
    public PSDirectException(string message) { }

  }

  public sealed class PSIdentity {
    public PSIdentity(string authType, bool isAuthenticated, string userName, System.Management.Automation.Remoting.PSCertificateDetails cert) { }

    public string AuthenticationType { get { return default(string); } }
    public System.Management.Automation.Remoting.PSCertificateDetails CertificateDetails { get { return default(System.Management.Automation.Remoting.PSCertificateDetails); } }
    public bool IsAuthenticated { get { return default(bool); } }
    public string Name { get { return default(string); } }
  }

  public sealed class PSPrincipal {
    public PSPrincipal(System.Management.Automation.Remoting.PSIdentity identity, System.Security.Principal.WindowsIdentity windowsIdentity) { }

    public System.Management.Automation.Remoting.PSIdentity Identity { get { return default(System.Management.Automation.Remoting.PSIdentity); } }
    public System.Security.Principal.WindowsIdentity WindowsIdentity { get { return default(System.Security.Principal.WindowsIdentity); } }
    public bool IsInRole ( string role ) { return default(bool); }

  }

    [System.SerializableAttribute]
   public class PSRemotingDataStructureException : System.Management.Automation.RuntimeException {
    public PSRemotingDataStructureException() { }
    public PSRemotingDataStructureException(string message) { }
    public PSRemotingDataStructureException(string message, System.Exception innerException) { }
    protected PSRemotingDataStructureException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

  }

    [System.SerializableAttribute]
   public class PSRemotingTransportException : System.Management.Automation.RuntimeException {
    public PSRemotingTransportException() { }
    public PSRemotingTransportException(string message) { }
    public PSRemotingTransportException(string message, System.Exception innerException) { }
    protected PSRemotingTransportException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

    public int ErrorCode { get { return default(int); } set { } }
    public string TransportMessage { get { return default(string); } set { } }
    public override void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

  }

    [System.SerializableAttribute]
   public class PSRemotingTransportRedirectException : System.Management.Automation.Remoting.PSRemotingTransportException {
    public PSRemotingTransportRedirectException() { }
    public PSRemotingTransportRedirectException(string message) { }
    public PSRemotingTransportRedirectException(string message, System.Exception innerException) { }
    protected PSRemotingTransportRedirectException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

    public string RedirectLocation { get { return default(string); } }
    public override void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

  }

    [System.SerializableAttribute]
   public sealed class PSSenderInfo {
    public PSSenderInfo(System.Management.Automation.Remoting.PSPrincipal userPrincipal, string httpUrl) { }

    public System.Management.Automation.PSPrimitiveDictionary ApplicationArguments { get { return default(System.Management.Automation.PSPrimitiveDictionary); } set { } }
    public System.TimeZoneInfo ClientTimeZone { get { return default(System.TimeZoneInfo); } set { } }
    public string ConfigurationName { get { return default(string); } set { } }
    public string ConnectionString { get { return default(string); } }
    public System.Management.Automation.Remoting.PSPrincipal UserInfo { get { return default(System.Management.Automation.Remoting.PSPrincipal); } }
    public void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

  }

  public abstract class PSSessionConfiguration : System.IDisposable {
    protected PSSessionConfiguration() { }

    public virtual void Dispose (  ) { }
    protected virtual void Dispose ( bool isDisposing ) { }
    public virtual System.Management.Automation.PSPrimitiveDictionary GetApplicationPrivateData ( System.Management.Automation.Remoting.PSSenderInfo senderInfo ) { return default(System.Management.Automation.PSPrimitiveDictionary); }
    public virtual System.Management.Automation.Runspaces.InitialSessionState GetInitialSessionState ( System.Management.Automation.Remoting.PSSenderInfo senderInfo ) { return default(System.Management.Automation.Runspaces.InitialSessionState); }
    public virtual System.Management.Automation.Runspaces.InitialSessionState GetInitialSessionState ( System.Management.Automation.Remoting.PSSessionConfigurationData sessionConfigurationData, System.Management.Automation.Remoting.PSSenderInfo senderInfo, string configProviderId ) { return default(System.Management.Automation.Runspaces.InitialSessionState); }
    public virtual System.Nullable<System.Int32> GetMaximumReceivedDataSizePerCommand ( System.Management.Automation.Remoting.PSSenderInfo senderInfo ) { return default(System.Nullable<System.Int32>); }
    public virtual System.Nullable<System.Int32> GetMaximumReceivedObjectSize ( System.Management.Automation.Remoting.PSSenderInfo senderInfo ) { return default(System.Nullable<System.Int32>); }

  }

  public sealed class PSSessionConfigurationData {
    internal PSSessionConfigurationData() { }
    public System.Collections.Generic.List<string> ModulesToImport { get { return default(System.Collections.Generic.List<string>); } }
    public string PrivateData { get { return default(string); } set { } }
  }

  public sealed class PSSessionOption {
    public PSSessionOption() { }

    public System.Management.Automation.PSPrimitiveDictionary ApplicationArguments { get { return default(System.Management.Automation.PSPrimitiveDictionary); } set { } }
    public System.TimeSpan CancelTimeout { get { return default(System.TimeSpan); } set { } }
    public System.Globalization.CultureInfo Culture { get { return default(System.Globalization.CultureInfo); } set { } }
    public System.TimeSpan IdleTimeout { get { return default(System.TimeSpan); } set { } }
    public bool IncludePortInSPN { get { return default(bool); } set { } }
    public int MaxConnectionRetryCount { get { return default(int); } set { } }
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

  public sealed class WSManPluginManagedEntryInstanceWrapper : System.IDisposable {
    public WSManPluginManagedEntryInstanceWrapper() { }

    public void Dispose (  ) { }
    ~WSManPluginManagedEntryInstanceWrapper() { }
    public System.IntPtr GetEntryDelegate (  ) { return default(System.IntPtr); }

  }

  public sealed class WSManPluginManagedEntryWrapper {
    public static int InitPlugin ( System.IntPtr wkrPtrs ) { return default(int); }
    public static void PSPluginOperationShutdownCallback ( object operationContext, bool timedOut ) { }
    public static void ShutdownPlugin ( System.IntPtr pluginContext ) { }
    public static void WSManPluginCommand ( System.IntPtr pluginContext, System.IntPtr requestDetails, int flags, System.IntPtr shellContext, string commandLine, System.IntPtr arguments ) { }
    public static void WSManPluginConnect ( System.IntPtr pluginContext, System.IntPtr requestDetails, int flags, System.IntPtr shellContext, System.IntPtr commandContext, System.IntPtr inboundConnectInformation ) { }
    public static void WSManPluginReceive ( System.IntPtr pluginContext, System.IntPtr requestDetails, int flags, System.IntPtr shellContext, System.IntPtr commandContext, System.IntPtr streamSet ) { }
    public static void WSManPluginReleaseCommandContext ( System.IntPtr pluginContext, System.IntPtr shellContext, System.IntPtr commandContext ) { }
    public static void WSManPluginReleaseShellContext ( System.IntPtr pluginContext, System.IntPtr shellContext ) { }
    public static void WSManPluginSend ( System.IntPtr pluginContext, System.IntPtr requestDetails, int flags, System.IntPtr shellContext, System.IntPtr commandContext, string stream, System.IntPtr inboundData ) { }
    public static void WSManPluginShell ( System.IntPtr pluginContext, System.IntPtr requestDetails, int flags, string extraInfo, System.IntPtr startupInfo, System.IntPtr inboundShellInformation ) { }
    public static void WSManPluginSignal ( System.IntPtr pluginContext, System.IntPtr requestDetails, int flags, System.IntPtr shellContext, System.IntPtr commandContext, string code ) { }
    public static void WSManPSShutdown ( System.IntPtr shutdownContext ) { }

  }

}
namespace System.Management.Automation.Remoting.WSMan {
  public sealed class ActiveSessionsChangedEventArgs : System.EventArgs {
    public ActiveSessionsChangedEventArgs(int activeSessionsCount) { }

    public int ActiveSessionsCount { get { return default(int); } set { } }
  }

  public static class WSManServerChannelEvents {
    public static event System.EventHandler<System.Management.Automation.Remoting.WSMan.ActiveSessionsChangedEventArgs> ActiveSessionsChanged { add { } remove { } }
    public static event System.EventHandler ShuttingDown { add { } remove { } }

  }

}
namespace System.Management.Automation.Remoting.Internal {
  public class PSStreamObject {
    public PSStreamObject(System.Management.Automation.Remoting.Internal.PSStreamObjectType objectType, object value) { }

    public System.Management.Automation.Remoting.Internal.PSStreamObjectType ObjectType { get { return default(System.Management.Automation.Remoting.Internal.PSStreamObjectType); } set { } }
    public void WriteStreamObject ( System.Management.Automation.Cmdlet cmdlet, bool overrideInquire ) { }

  }

  public enum PSStreamObjectType {
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
namespace System.Management.Automation.Host {
  public partial struct BufferCell {
    public BufferCell(char character, System.ConsoleColor foreground, System.ConsoleColor background, System.Management.Automation.Host.BufferCellType bufferCellType) { }

    public System.ConsoleColor BackgroundColor { get { return default(System.ConsoleColor); } set { } }
    public System.Management.Automation.Host.BufferCellType BufferCellType { get { return default(System.Management.Automation.Host.BufferCellType); } set { } }
    public char Character { get { return default(char); } set { } }
    public System.ConsoleColor ForegroundColor { get { return default(System.ConsoleColor); } set { } }
    public override bool Equals ( object obj ) { return default(bool); }
    public override int GetHashCode (  ) { return default(int); }
    public static bool operator == ( System.Management.Automation.Host.BufferCell first, System.Management.Automation.Host.BufferCell second ) { return default(bool); }
    public static bool operator != ( System.Management.Automation.Host.BufferCell first, System.Management.Automation.Host.BufferCell second ) { return default(bool); }
    public override string ToString (  ) { return default(string); }

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

  public partial struct Coordinates {
    public Coordinates(int x, int y) { }

    public int X { get { return default(int); } set { } }
    public int Y { get { return default(int); } set { } }
    public override bool Equals ( object obj ) { return default(bool); }
    public override int GetHashCode (  ) { return default(int); }
    public static bool operator == ( System.Management.Automation.Host.Coordinates first, System.Management.Automation.Host.Coordinates second ) { return default(bool); }
    public static bool operator != ( System.Management.Automation.Host.Coordinates first, System.Management.Automation.Host.Coordinates second ) { return default(bool); }
    public override string ToString (  ) { return default(string); }

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
    public void SetParameterType ( System.Type parameterType ) { }

  }

    [System.SerializableAttribute]
   public class HostException : System.Management.Automation.RuntimeException {
    public HostException() { }
    public HostException(string message) { }
    public HostException(string message, System.Exception innerException) { }
    public HostException(string message, System.Exception innerException, string errorId, System.Management.Automation.ErrorCategory errorCategory) { }
    protected HostException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

  }

  public partial interface IHostSupportsInteractiveSession {
    bool IsRunspacePushed { get; }

    System.Management.Automation.Runspaces.Runspace Runspace { get; }

     void PopRunspace (  );
     void PushRunspace ( System.Management.Automation.Runspaces.Runspace runspace );

  }

  public partial interface IHostUISupportsMultipleChoiceSelection {
     System.Collections.ObjectModel.Collection<System.Int32> PromptForChoice ( string caption, string message, System.Collections.ObjectModel.Collection<System.Management.Automation.Host.ChoiceDescription> choices, System.Collections.Generic.IEnumerable<int> defaultChoices );

  }

  public partial struct KeyInfo {
    public KeyInfo(int virtualKeyCode, char ch, System.Management.Automation.Host.ControlKeyStates controlKeyState, bool keyDown) { }

    public char Character { get { return default(char); } set { } }
    public System.Management.Automation.Host.ControlKeyStates ControlKeyState { get { return default(System.Management.Automation.Host.ControlKeyStates); } set { } }
    public bool KeyDown { get { return default(bool); } set { } }
    public int VirtualKeyCode { get { return default(int); } set { } }
    public override bool Equals ( object obj ) { return default(bool); }
    public override int GetHashCode (  ) { return default(int); }
    public static bool operator == ( System.Management.Automation.Host.KeyInfo first, System.Management.Automation.Host.KeyInfo second ) { return default(bool); }
    public static bool operator != ( System.Management.Automation.Host.KeyInfo first, System.Management.Automation.Host.KeyInfo second ) { return default(bool); }
    public override string ToString (  ) { return default(string); }

  }

    [System.SerializableAttribute]
   public class PromptingException : System.Management.Automation.Host.HostException {
    public PromptingException() { }
    public PromptingException(string message) { }
    public PromptingException(string message, System.Exception innerException) { }
    public PromptingException(string message, System.Exception innerException, string errorId, System.Management.Automation.ErrorCategory errorCategory) { }
    protected PromptingException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

  }

  public abstract class PSHost {
    protected PSHost() { }

    public abstract System.Globalization.CultureInfo CurrentCulture { get; }
    public abstract System.Globalization.CultureInfo CurrentUICulture { get; }
    public bool DebuggerEnabled { get { return default(bool); } set { } }
    public abstract System.Guid InstanceId { get; }
    public abstract string Name { get; }
    public System.Management.Automation.PSObject PrivateData { get { return default(System.Management.Automation.PSObject); } }
    public abstract System.Management.Automation.Host.PSHostUserInterface UI { get; }
    public abstract System.Version Version { get; }
    public virtual void EnterNestedPrompt (  ) { }
    public virtual void ExitNestedPrompt (  ) { }
    public virtual void NotifyBeginApplication (  ) { }
    public virtual void NotifyEndApplication (  ) { }
    public virtual void SetShouldExit ( int exitCode ) { }

  }

  public abstract class PSHostRawUserInterface {
    protected PSHostRawUserInterface() { }

    public abstract System.ConsoleColor BackgroundColor { get; }
    public abstract System.Management.Automation.Host.Size BufferSize { get; }
    public abstract System.Management.Automation.Host.Coordinates CursorPosition { get; }
    public abstract int CursorSize { get; }
    public abstract System.ConsoleColor ForegroundColor { get; }
    public abstract bool KeyAvailable { get; }
    public abstract System.Management.Automation.Host.Size MaxPhysicalWindowSize { get; }
    public abstract System.Management.Automation.Host.Size MaxWindowSize { get; }
    public abstract System.Management.Automation.Host.Coordinates WindowPosition { get; }
    public abstract System.Management.Automation.Host.Size WindowSize { get; }
    public abstract string WindowTitle { get; }
    public virtual void FlushInputBuffer (  ) { }
    public virtual System.Management.Automation.Host.BufferCell[,] GetBufferContents ( System.Management.Automation.Host.Rectangle rectangle ) { return default(System.Management.Automation.Host.BufferCell[,]); }
    public virtual int LengthInBufferCells ( char source ) { return default(int); }
    public virtual int LengthInBufferCells ( string source ) { return default(int); }
    public virtual int LengthInBufferCells ( string source, int offset ) { return default(int); }
    public System.Management.Automation.Host.BufferCell[,] NewBufferCellArray ( string[] contents, System.ConsoleColor foregroundColor, System.ConsoleColor backgroundColor ) { return default(System.Management.Automation.Host.BufferCell[,]); }
    public System.Management.Automation.Host.BufferCell[,] NewBufferCellArray ( System.Management.Automation.Host.Size size, System.Management.Automation.Host.BufferCell contents ) { return default(System.Management.Automation.Host.BufferCell[,]); }
    public System.Management.Automation.Host.BufferCell[,] NewBufferCellArray ( int width, int height, System.Management.Automation.Host.BufferCell contents ) { return default(System.Management.Automation.Host.BufferCell[,]); }
    public System.Management.Automation.Host.KeyInfo ReadKey (  ) { return default(System.Management.Automation.Host.KeyInfo); }
    public virtual System.Management.Automation.Host.KeyInfo ReadKey ( System.Management.Automation.Host.ReadKeyOptions options ) { return default(System.Management.Automation.Host.KeyInfo); }
    public virtual void ScrollBufferContents ( System.Management.Automation.Host.Rectangle source, System.Management.Automation.Host.Coordinates destination, System.Management.Automation.Host.Rectangle clip, System.Management.Automation.Host.BufferCell fill ) { }
    public virtual void SetBufferContents ( System.Management.Automation.Host.Coordinates origin, System.Management.Automation.Host.BufferCell[,] contents ) { }
    public virtual void SetBufferContents ( System.Management.Automation.Host.Rectangle rectangle, System.Management.Automation.Host.BufferCell fill ) { }

  }

  public abstract class PSHostUserInterface {
    protected PSHostUserInterface() { }

    public abstract System.Management.Automation.Host.PSHostRawUserInterface RawUI { get; }
    public bool SupportsVirtualTerminal { get { return default(bool); } }
    public virtual System.Collections.Generic.Dictionary<System.String,System.Management.Automation.PSObject> Prompt ( string caption, string message, System.Collections.ObjectModel.Collection<System.Management.Automation.Host.FieldDescription> descriptions ) { return default(System.Collections.Generic.Dictionary<System.String,System.Management.Automation.PSObject>); }
    public virtual int PromptForChoice ( string caption, string message, System.Collections.ObjectModel.Collection<System.Management.Automation.Host.ChoiceDescription> choices, int defaultChoice ) { return default(int); }
    public virtual System.Management.Automation.PSCredential PromptForCredential ( string caption, string message, string userName, string targetName, System.Management.Automation.PSCredentialTypes allowedCredentialTypes, System.Management.Automation.PSCredentialUIOptions options ) { return default(System.Management.Automation.PSCredential); }
    public virtual System.Management.Automation.PSCredential PromptForCredential ( string caption, string message, string userName, string targetName ) { return default(System.Management.Automation.PSCredential); }
    public virtual string ReadLine (  ) { return default(string); }
    public virtual System.Security.SecureString ReadLineAsSecureString (  ) { return default(System.Security.SecureString); }
    public virtual void Write ( System.ConsoleColor foregroundColor, System.ConsoleColor backgroundColor, string value ) { }
    public virtual void Write ( string value ) { }
    public virtual void WriteDebugLine ( string message ) { }
    public virtual void WriteErrorLine ( string value ) { }
    public virtual void WriteInformation ( System.Management.Automation.InformationRecord record ) { }
    public virtual void WriteLine (  ) { }
    public virtual void WriteLine ( System.ConsoleColor foregroundColor, System.ConsoleColor backgroundColor, string value ) { }
    public virtual void WriteLine ( string value ) { }
    public virtual void WriteProgress ( System.Int64 sourceId, System.Management.Automation.ProgressRecord record ) { }
    public virtual void WriteVerboseLine ( string message ) { }
    public virtual void WriteWarningLine ( string message ) { }

  }

    [System.FlagsAttribute]
   public enum ReadKeyOptions {
    AllowCtrlC = 1,
    IncludeKeyDown = 4,
    IncludeKeyUp = 8,
    NoEcho = 2,
  }

  public partial struct Rectangle {
    public Rectangle(int left, int top, int right, int bottom) { }
    public Rectangle(System.Management.Automation.Host.Coordinates upperLeft, System.Management.Automation.Host.Coordinates lowerRight) { }

    public int Bottom { get { return default(int); } set { } }
    public int Left { get { return default(int); } set { } }
    public int Right { get { return default(int); } set { } }
    public int Top { get { return default(int); } set { } }
    public override bool Equals ( object obj ) { return default(bool); }
    public override int GetHashCode (  ) { return default(int); }
    public static bool operator == ( System.Management.Automation.Host.Rectangle first, System.Management.Automation.Host.Rectangle second ) { return default(bool); }
    public static bool operator != ( System.Management.Automation.Host.Rectangle first, System.Management.Automation.Host.Rectangle second ) { return default(bool); }
    public override string ToString (  ) { return default(string); }

  }

  public partial struct Size {
    public Size(int width, int height) { }

    public int Height { get { return default(int); } set { } }
    public int Width { get { return default(int); } set { } }
    public override bool Equals ( object obj ) { return default(bool); }
    public override int GetHashCode (  ) { return default(int); }
    public static bool operator == ( System.Management.Automation.Host.Size first, System.Management.Automation.Host.Size second ) { return default(bool); }
    public static bool operator != ( System.Management.Automation.Host.Size first, System.Management.Automation.Host.Size second ) { return default(bool); }
    public override string ToString (  ) { return default(string); }

  }

}
namespace System.Management.Automation.Runspaces {
   [System.Diagnostics.DebuggerDisplayAttribute("AliasProperty: {Name,nq} = {ReferencedMemberName,nq}")]
   public sealed class AliasPropertyData : System.Management.Automation.Runspaces.TypeMemberData {
    public AliasPropertyData(string name, string referencedMemberName) { }
    public AliasPropertyData(string name, string referencedMemberName, System.Type type) { }

    public bool IsHidden { get { return default(bool); } set { } }
    public System.Type MemberType { get { return default(System.Type); } set { } }
    public string ReferencedMemberName { get { return default(string); } set { } }
  }

  public enum AuthenticationMechanism {
    Basic = 1,
    Credssp = 4,
    Default = 0,
    Digest = 5,
    Kerberos = 6,
    Negotiate = 2,
    NegotiateWithImplicitCredential = 3,
  }

   [System.Diagnostics.DebuggerDisplayAttribute("CodeMethod: {Name,nq}")]
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

    public System.Management.Automation.CommandOrigin CommandOrigin { get { return default(System.Management.Automation.CommandOrigin); } set { } }
    public string CommandText { get { return default(string); } }
    public bool IsEndOfStatement { get { return default(bool); } set { } }
    public bool IsScript { get { return default(bool); } }
    public System.Management.Automation.Runspaces.PipelineResultTypes MergeUnclaimedPreviousCommandResults { get { return default(System.Management.Automation.Runspaces.PipelineResultTypes); } set { } }
    public System.Management.Automation.Runspaces.CommandParameterCollection Parameters { get { return default(System.Management.Automation.Runspaces.CommandParameterCollection); } }
    public bool UseLocalScope { get { return default(bool); } }
    public void MergeMyResults ( System.Management.Automation.Runspaces.PipelineResultTypes myResult, System.Management.Automation.Runspaces.PipelineResultTypes toResult ) { }
    public override string ToString (  ) { return default(string); }

  }

  public sealed class CommandCollection : System.Collections.ObjectModel.Collection<System.Management.Automation.Runspaces.Command> {
    internal CommandCollection() { }
    public void Add ( string command ) { }
    public void AddScript ( string scriptContents ) { }
    public void AddScript ( string scriptContents, bool useLocalScope ) { }

  }

  public sealed class CommandParameter {
    public CommandParameter(string name) { }
    public CommandParameter(string name, object value) { }

    public string Name { get { return default(string); } }
    public object Value { get { return default(object); } }
  }

  public sealed class CommandParameterCollection : System.Collections.ObjectModel.Collection<System.Management.Automation.Runspaces.CommandParameter> {
    public CommandParameterCollection() { }

    public void Add ( string name ) { }
    public void Add ( string name, object value ) { }

  }

  public abstract class ConstrainedSessionStateEntry : System.Management.Automation.Runspaces.InitialSessionStateEntry {
    protected ConstrainedSessionStateEntry(string name, System.Management.Automation.SessionStateEntryVisibility visibility) : base(name) { }

    public System.Management.Automation.SessionStateEntryVisibility Visibility { get { return default(System.Management.Automation.SessionStateEntryVisibility); } set { } }
  }

  public sealed class ContainerConnectionInfo : System.Management.Automation.Runspaces.RunspaceConnectionInfo {
    public override System.Management.Automation.Runspaces.AuthenticationMechanism AuthenticationMechanism { get { return default(System.Management.Automation.Runspaces.AuthenticationMechanism); } }
    public override string CertificateThumbprint { get { return default(string); } }
    public override string ComputerName { get { return default(string); } }
    public override System.Management.Automation.PSCredential Credential { get { return default(System.Management.Automation.PSCredential); } }
    public static System.Management.Automation.Runspaces.ContainerConnectionInfo CreateContainerConnectionInfo ( string containerId, bool runAsAdmin, string configurationName ) { return default(System.Management.Automation.Runspaces.ContainerConnectionInfo); }
    public void CreateContainerProcess (  ) { }
    public bool TerminateContainerProcess (  ) { return default(bool); }

  }

  public sealed class FormatTable {
    public FormatTable(System.Collections.Generic.IEnumerable<string> formatFiles) { }

    public void AppendFormatData ( System.Collections.Generic.IEnumerable<System.Management.Automation.ExtendedTypeDefinition> formatData ) { }
    public static System.Management.Automation.Runspaces.FormatTable LoadDefaultFormatFiles (  ) { return default(System.Management.Automation.Runspaces.FormatTable); }
    public void PrependFormatData ( System.Collections.Generic.IEnumerable<System.Management.Automation.ExtendedTypeDefinition> formatData ) { }

  }

    [System.SerializableAttribute]
   public class FormatTableLoadException : System.Management.Automation.RuntimeException {
    public FormatTableLoadException() { }
    public FormatTableLoadException(string message) { }
    public FormatTableLoadException(string message, System.Exception innerException) { }
    protected FormatTableLoadException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

    public System.Collections.ObjectModel.Collection<string> Errors { get { return default(System.Collections.ObjectModel.Collection<string>); } }
    public override void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

  }

  public class InitialSessionState {
    protected InitialSessionState() { }

    public System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<System.Management.Automation.Runspaces.SessionStateAssemblyEntry> Assemblies { get { return default(System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<System.Management.Automation.Runspaces.SessionStateAssemblyEntry>); } }
    public System.Management.Automation.AuthorizationManager AuthorizationManager { get { return default(System.Management.Automation.AuthorizationManager); } set { } }
    public System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<System.Management.Automation.Runspaces.SessionStateCommandEntry> Commands { get { return default(System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<System.Management.Automation.Runspaces.SessionStateCommandEntry>); } }
    public bool DisableFormatUpdates { get { return default(bool); } set { } }
    public System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<System.Management.Automation.Runspaces.SessionStateVariableEntry> EnvironmentVariables { get { return default(System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<System.Management.Automation.Runspaces.SessionStateVariableEntry>); } }
    public Microsoft.PowerShell.ExecutionPolicy ExecutionPolicy { get { return default(Microsoft.PowerShell.ExecutionPolicy); } set { } }
    public System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<System.Management.Automation.Runspaces.SessionStateFormatEntry> Formats { get { return default(System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<System.Management.Automation.Runspaces.SessionStateFormatEntry>); } }
    public System.Management.Automation.PSLanguageMode LanguageMode { get { return default(System.Management.Automation.PSLanguageMode); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<Microsoft.PowerShell.Commands.ModuleSpecification> Modules { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<Microsoft.PowerShell.Commands.ModuleSpecification>); } }
    public System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<System.Management.Automation.Runspaces.SessionStateProviderEntry> Providers { get { return default(System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<System.Management.Automation.Runspaces.SessionStateProviderEntry>); } }
    public System.Collections.Generic.HashSet<string> StartupScripts { get { return default(System.Collections.Generic.HashSet<string>); } }
    public System.Management.Automation.Runspaces.PSThreadOptions ThreadOptions { get { return default(System.Management.Automation.Runspaces.PSThreadOptions); } set { } }
    public bool ThrowOnRunspaceOpenError { get { return default(bool); } set { } }
    public string TranscriptDirectory { get { return default(string); } set { } }
    public System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<System.Management.Automation.Runspaces.SessionStateTypeEntry> Types { get { return default(System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<System.Management.Automation.Runspaces.SessionStateTypeEntry>); } }
    public bool UseFullLanguageModeInDebugger { get { return default(bool); } set { } }
    public System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<System.Management.Automation.Runspaces.SessionStateVariableEntry> Variables { get { return default(System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<System.Management.Automation.Runspaces.SessionStateVariableEntry>); } }
    public System.Management.Automation.Runspaces.InitialSessionState Clone (  ) { return default(System.Management.Automation.Runspaces.InitialSessionState); }
    public static System.Management.Automation.Runspaces.InitialSessionState Create (  ) { return default(System.Management.Automation.Runspaces.InitialSessionState); }
    public static System.Management.Automation.Runspaces.InitialSessionState Create ( string snapInName ) { return default(System.Management.Automation.Runspaces.InitialSessionState); }
    public static System.Management.Automation.Runspaces.InitialSessionState Create ( string[] snapInNameCollection, out System.Management.Automation.Runspaces.PSConsoleLoadException warning ) { warning = default(System.Management.Automation.Runspaces.PSConsoleLoadException); return default(System.Management.Automation.Runspaces.InitialSessionState); }
    public static System.Management.Automation.Runspaces.InitialSessionState CreateDefault (  ) { return default(System.Management.Automation.Runspaces.InitialSessionState); }
    public static System.Management.Automation.Runspaces.InitialSessionState CreateDefault2 (  ) { return default(System.Management.Automation.Runspaces.InitialSessionState); }
    public static System.Management.Automation.Runspaces.InitialSessionState CreateFrom ( string snapInPath, out System.Management.Automation.Runspaces.PSConsoleLoadException warnings ) { warnings = default(System.Management.Automation.Runspaces.PSConsoleLoadException); return default(System.Management.Automation.Runspaces.InitialSessionState); }
    public static System.Management.Automation.Runspaces.InitialSessionState CreateFrom ( string[] snapInPathCollection, out System.Management.Automation.Runspaces.PSConsoleLoadException warnings ) { warnings = default(System.Management.Automation.Runspaces.PSConsoleLoadException); return default(System.Management.Automation.Runspaces.InitialSessionState); }
    public static System.Management.Automation.Runspaces.InitialSessionState CreateFromSessionConfigurationFile ( string path, System.Func<string, bool> roleVerifier ) { return default(System.Management.Automation.Runspaces.InitialSessionState); }
    public static System.Management.Automation.Runspaces.InitialSessionState CreateFromSessionConfigurationFile ( string path ) { return default(System.Management.Automation.Runspaces.InitialSessionState); }
    public static System.Management.Automation.Runspaces.InitialSessionState CreateRestricted ( System.Management.Automation.SessionCapabilities sessionCapabilities ) { return default(System.Management.Automation.Runspaces.InitialSessionState); }
    public void ImportPSModule ( System.Collections.Generic.IEnumerable<Microsoft.PowerShell.Commands.ModuleSpecification> modules ) { }
    public void ImportPSModule ( string[] name ) { }
    public void ImportPSModulesFromPath ( string path ) { }
    public System.Management.Automation.PSSnapInInfo ImportPSSnapIn ( string name, out System.Management.Automation.Runspaces.PSSnapInException warning ) { warning = default(System.Management.Automation.Runspaces.PSSnapInException); return default(System.Management.Automation.PSSnapInInfo); }

  }

  public abstract class InitialSessionStateEntry {
    protected InitialSessionStateEntry(string name) { }

    public System.Management.Automation.PSModuleInfo Module { get { return default(System.Management.Automation.PSModuleInfo); } set { } }
    public string Name { get { return default(string); } set { } }
    public System.Management.Automation.PSSnapInInfo PSSnapIn { get { return default(System.Management.Automation.PSSnapInInfo); } set { } }
    public virtual System.Management.Automation.Runspaces.InitialSessionStateEntry Clone (  ) { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }

  }

   public sealed class InitialSessionStateEntryCollection<T> : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable where T : System.Management.Automation.Runspaces.InitialSessionStateEntry {
    public InitialSessionStateEntryCollection() { }
    public InitialSessionStateEntryCollection(System.Collections.Generic.IEnumerable<T> items) { }

    public int Count { get { return default(int); } }
    public T this[int index] { get { return default(T); } }
    public System.Collections.ObjectModel.Collection<T> this[string name] { get { return default(System.Collections.ObjectModel.Collection<T>); } }
    public void Add ( T item ) { }
    public void Add ( System.Collections.Generic.IEnumerable<T> items ) { }
    public void Clear (  ) { }
    public System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<T> Clone (  ) { return default(System.Management.Automation.Runspaces.InitialSessionStateEntryCollection<T>); }
    public void Remove ( string name, object type ) { }
    public void RemoveItem ( int index ) { }
    public void RemoveItem ( int index, int count ) { }
    public void Reset (  ) { }
    System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { return default(System.Collections.Generic.IEnumerator<T>); }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
 
  }

    [System.SerializableAttribute]
   public class InvalidPipelineStateException : System.SystemException {
    public InvalidPipelineStateException() { }
    public InvalidPipelineStateException(string message) { }
    public InvalidPipelineStateException(string message, System.Exception innerException) { }

    public System.Management.Automation.Runspaces.PipelineState CurrentState { get { return default(System.Management.Automation.Runspaces.PipelineState); } }
    public System.Management.Automation.Runspaces.PipelineState ExpectedState { get { return default(System.Management.Automation.Runspaces.PipelineState); } }
  }

    [System.SerializableAttribute]
   public class InvalidRunspacePoolStateException : System.SystemException {
    public InvalidRunspacePoolStateException() { }
    public InvalidRunspacePoolStateException(string message) { }
    public InvalidRunspacePoolStateException(string message, System.Exception innerException) { }
    protected InvalidRunspacePoolStateException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

    public System.Management.Automation.Runspaces.RunspacePoolState CurrentState { get { return default(System.Management.Automation.Runspaces.RunspacePoolState); } }
    public System.Management.Automation.Runspaces.RunspacePoolState ExpectedState { get { return default(System.Management.Automation.Runspaces.RunspacePoolState); } }
  }

    [System.SerializableAttribute]
   public class InvalidRunspaceStateException : System.SystemException {
    public InvalidRunspaceStateException() { }
    public InvalidRunspaceStateException(string message) { }
    public InvalidRunspaceStateException(string message, System.Exception innerException) { }
    protected InvalidRunspaceStateException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

    public System.Management.Automation.Runspaces.RunspaceState CurrentState { get { return default(System.Management.Automation.Runspaces.RunspaceState); } set { } }
    public System.Management.Automation.Runspaces.RunspaceState ExpectedState { get { return default(System.Management.Automation.Runspaces.RunspaceState); } set { } }
  }

  public class MemberSetData : System.Management.Automation.Runspaces.TypeMemberData {
    public MemberSetData(string name, System.Collections.ObjectModel.Collection<System.Management.Automation.Runspaces.TypeMemberData> members) { }

    public bool InheritMembers { get { return default(bool); } set { } }
    public bool IsHidden { get { return default(bool); } set { } }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.Runspaces.TypeMemberData> Members { get { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.Runspaces.TypeMemberData>); } set { } }
  }

  public sealed class NamedPipeConnectionInfo : System.Management.Automation.Runspaces.RunspaceConnectionInfo {
    public NamedPipeConnectionInfo() { }
    public NamedPipeConnectionInfo(int processId) { }
    public NamedPipeConnectionInfo(int processId, string appDomainName) { }
    public NamedPipeConnectionInfo(int processId, string appDomainName, int openTimeout) { }

    public string AppDomainName { get { return default(string); } set { } }
    public override System.Management.Automation.Runspaces.AuthenticationMechanism AuthenticationMechanism { get { return default(System.Management.Automation.Runspaces.AuthenticationMechanism); } }
    public override string CertificateThumbprint { get { return default(string); } }
    public override string ComputerName { get { return default(string); } }
    public override System.Management.Automation.PSCredential Credential { get { return default(System.Management.Automation.PSCredential); } }
    public int ProcessId { get { return default(int); } set { } }
  }

   [System.Diagnostics.DebuggerDisplayAttribute("NoteProperty: {Name,nq} = {Value,nq}")]
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

  public abstract class Pipeline : System.IDisposable {
    internal Pipeline() { }
    public abstract event System.EventHandler<System.Management.Automation.Runspaces.PipelineStateEventArgs> StateChanged;

    public System.Management.Automation.Runspaces.CommandCollection Commands { get { return default(System.Management.Automation.Runspaces.CommandCollection); } set { } }
    public abstract System.Management.Automation.Runspaces.PipelineReader<object> Error { get; }
    public bool HadErrors { get { return default(bool); } }
    public abstract System.Management.Automation.Runspaces.PipelineWriter Input { get; }
    public System.Int64 InstanceId { get { return default(System.Int64); } }
    public abstract bool IsNested { get; }
    public abstract System.Management.Automation.Runspaces.PipelineReader<System.Management.Automation.PSObject> Output { get; }
    public abstract System.Management.Automation.Runspaces.PipelineStateInfo PipelineStateInfo { get; }
    public abstract System.Management.Automation.Runspaces.Runspace Runspace { get; }
    public bool SetPipelineSessionState { get { return default(bool); } set { } }
    public virtual System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Connect (  ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public virtual void ConnectAsync (  ) { }
    public virtual System.Management.Automation.Runspaces.Pipeline Copy (  ) { return default(System.Management.Automation.Runspaces.Pipeline); }
    protected virtual void Dispose ( bool disposing ) { }
    public virtual void Dispose (  ) { }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Invoke (  ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public virtual System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject> Invoke ( System.Collections.IEnumerable input ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PSObject>); }
    public virtual void InvokeAsync (  ) { }
    public virtual void Stop (  ) { }
    public virtual void StopAsync (  ) { }

  }

  public abstract class PipelineReader<T> {
    protected PipelineReader() { }

    public abstract event System.EventHandler DataReady;

    public abstract int Count { get; }
    public abstract bool EndOfPipeline { get; }
    public abstract bool IsOpen { get; }
    public abstract int MaxCapacity { get; }
    public abstract System.Threading.WaitHandle WaitHandle { get; }
    public virtual void Close (  ) { }
    public virtual System.Collections.ObjectModel.Collection<T> NonBlockingRead (  ) { return default(System.Collections.ObjectModel.Collection<T>); }
    public virtual System.Collections.ObjectModel.Collection<T> NonBlockingRead ( int maxRequested ) { return default(System.Collections.ObjectModel.Collection<T>); }
    public virtual T Peek (  ) { return default(T); }
    public virtual System.Collections.ObjectModel.Collection<T> Read ( int count ) { return default(System.Collections.ObjectModel.Collection<T>); }
    public virtual T Read (  ) { return default(T); }
    public virtual System.Collections.ObjectModel.Collection<T> ReadToEnd (  ) { return default(System.Collections.ObjectModel.Collection<T>); }

  }

    [System.FlagsAttribute]
   public enum PipelineResultTypes {
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

  public enum PipelineState {
    Completed = 4,
    Disconnected = 6,
    Failed = 5,
    NotStarted = 0,
    Running = 1,
    Stopped = 3,
    Stopping = 2,
  }

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
    public virtual void Close (  ) { }
    public virtual void Flush (  ) { }
    public virtual int Write ( object obj ) { return default(int); }
    public virtual int Write ( object obj, bool enumerateCollection ) { return default(int); }

  }

  public sealed class PowerShellProcessInstance : System.IDisposable {
    public PowerShellProcessInstance(System.Version powerShellVersion, System.Management.Automation.PSCredential credential, System.Management.Automation.ScriptBlock initializationScript, bool useWow64) { }
    public PowerShellProcessInstance() { }

    public bool HasExited { get { return default(bool); } }
    public System.Diagnostics.Process Process { get { return default(System.Diagnostics.Process); } }
    public void Dispose (  ) { }

  }

   [System.Diagnostics.DebuggerDisplayAttribute("PropertySet: {Name,nq}")]
   public sealed class PropertySetData : System.Management.Automation.Runspaces.TypeMemberData {
    public PropertySetData(System.Collections.Generic.IEnumerable<string> referencedProperties) { }

    public bool IsHidden { get { return default(bool); } set { } }
    public System.Collections.ObjectModel.Collection<string> ReferencedProperties { get { return default(System.Collections.ObjectModel.Collection<string>); } set { } }
  }

    [System.SerializableAttribute]
   public class PSConsoleLoadException : System.SystemException, System.Management.Automation.IContainsErrorRecord {
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
    public System.Management.Automation.Runspaces.TargetMachineType ComputerType { get { return default(System.Management.Automation.Runspaces.TargetMachineType); } set { } }
    public string ConfigurationName { get { return default(string); } }
    public string ContainerId { get { return default(string); } }
    public int Id { get { return default(int); } }
    public System.Guid InstanceId { get { return default(System.Guid); } }
    public string Name { get { return default(string); } set { } }
    public System.Management.Automation.Runspaces.Runspace Runspace { get { return default(System.Management.Automation.Runspaces.Runspace); } }
    public System.Nullable<System.Guid> VMId { get { return default(System.Nullable<System.Guid>); } }
    public string VMName { get { return default(string); } }
    public override string ToString (  ) { return default(string); }

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

    [System.SerializableAttribute]
   public class PSSnapInException : System.Management.Automation.RuntimeException {
    public PSSnapInException() { }
    public PSSnapInException(string message) { }
    public PSSnapInException(string message, System.Exception innerException) { }
    protected PSSnapInException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

    public override System.Management.Automation.ErrorRecord ErrorRecord { get { return default(System.Management.Automation.ErrorRecord); } }
    public override string Message { get { return default(string); } }
    public override void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

  }

  public enum PSThreadOptions {
    Default = 0,
    ReuseThread = 2,
    UseCurrentThread = 3,
    UseNewThread = 1,
  }

    [System.Runtime.Serialization.DataContractAttribute]
   public class RemotingDebugRecord : System.Management.Automation.DebugRecord {
    public RemotingDebugRecord(string message, System.Management.Automation.Remoting.OriginInfo originInfo) : base(message) { }

    public System.Management.Automation.Remoting.OriginInfo OriginInfo { get { return default(System.Management.Automation.Remoting.OriginInfo); } }
  }

    [System.SerializableAttribute]
   public class RemotingErrorRecord : System.Management.Automation.ErrorRecord {
    public RemotingErrorRecord(System.Management.Automation.ErrorRecord errorRecord, System.Management.Automation.Remoting.OriginInfo originInfo) : base (default(System.Exception), default(string), default(System.Management.Automation.ErrorCategory), default(object)) { }
    protected RemotingErrorRecord(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base (default(System.Exception), default(string), default(System.Management.Automation.ErrorCategory), default(object)) { }

    public System.Management.Automation.Remoting.OriginInfo OriginInfo { get { return default(System.Management.Automation.Remoting.OriginInfo); } }
    public override void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

  }

    [System.Runtime.Serialization.DataContractAttribute]
   public class RemotingInformationRecord : System.Management.Automation.InformationRecord {
    public RemotingInformationRecord(System.Management.Automation.InformationRecord record, System.Management.Automation.Remoting.OriginInfo originInfo) : base (default(object), default(string)) { }

    public System.Management.Automation.Remoting.OriginInfo OriginInfo { get { return default(System.Management.Automation.Remoting.OriginInfo); } }
  }

    [System.Runtime.Serialization.DataContractAttribute]
   public class RemotingProgressRecord : System.Management.Automation.ProgressRecord {
    public RemotingProgressRecord(System.Management.Automation.ProgressRecord progressRecord, System.Management.Automation.Remoting.OriginInfo originInfo) : base (default(int), default(string), default(string) ) { }

    public System.Management.Automation.Remoting.OriginInfo OriginInfo { get { return default(System.Management.Automation.Remoting.OriginInfo); } }
  }

    [System.Runtime.Serialization.DataContractAttribute]
   public class RemotingVerboseRecord : System.Management.Automation.VerboseRecord {
    public RemotingVerboseRecord(string message, System.Management.Automation.Remoting.OriginInfo originInfo) : base(message) { }

    public System.Management.Automation.Remoting.OriginInfo OriginInfo { get { return default(System.Management.Automation.Remoting.OriginInfo); } }
  }

    [System.Runtime.Serialization.DataContractAttribute]
   public class RemotingWarningRecord : System.Management.Automation.WarningRecord {
    public RemotingWarningRecord(string message, System.Management.Automation.Remoting.OriginInfo originInfo) : base(message) { }

    public System.Management.Automation.Remoting.OriginInfo OriginInfo { get { return default(System.Management.Automation.Remoting.OriginInfo); } }
  }

  public abstract class Runspace : System.IDisposable {
    internal Runspace() { }
    public abstract event System.EventHandler<System.Management.Automation.Runspaces.RunspaceAvailabilityEventArgs> AvailabilityChanged;
    public abstract event System.EventHandler<System.Management.Automation.Runspaces.RunspaceStateEventArgs> StateChanged;

    public bool CanUseDefaultRunspace { get { return default(bool); } }
    public abstract System.Management.Automation.Runspaces.RunspaceConnectionInfo ConnectionInfo { get; }
    public System.Management.Automation.Debugger Debugger { get { return default(System.Management.Automation.Debugger); } }
    public System.Management.Automation.Runspaces.Runspace DefaultRunspace { get { return default(System.Management.Automation.Runspaces.Runspace); } set { } }
    public System.Nullable<System.DateTime> DisconnectedOn { get { return default(System.Nullable<System.DateTime>); } set { } }
    public abstract System.Management.Automation.PSEventManager Events { get; }
    public System.Nullable<System.DateTime> ExpiresOn { get { return default(System.Nullable<System.DateTime>); } set { } }
    public int Id { get { return default(int); } set { } }
    public abstract System.Management.Automation.Runspaces.InitialSessionState InitialSessionState { get; }
    public System.Guid InstanceId { get { return default(System.Guid); } set { } }
    public abstract System.Management.Automation.JobManager JobManager { get; }
    public string Name { get { return default(string); } set { } }
    public abstract System.Management.Automation.Runspaces.RunspaceConnectionInfo OriginalConnectionInfo { get; }
    public abstract System.Management.Automation.Runspaces.RunspaceAvailability RunspaceAvailability { get; }
    public bool RunspaceIsRemote { get { return default(bool); } }
    public abstract System.Management.Automation.Runspaces.RunspaceStateInfo RunspaceStateInfo { get; }
    public System.Management.Automation.Runspaces.SessionStateProxy SessionStateProxy { get { return default(System.Management.Automation.Runspaces.SessionStateProxy); } }
    public abstract System.Management.Automation.Runspaces.PSThreadOptions ThreadOptions { get; }
    public abstract System.Version Version { get; }
    public virtual void Close (  ) { }
    public virtual void CloseAsync (  ) { }
    public virtual void Connect (  ) { }
    public virtual void ConnectAsync (  ) { }
    public virtual System.Management.Automation.Runspaces.Pipeline CreateDisconnectedPipeline (  ) { return default(System.Management.Automation.Runspaces.Pipeline); }
    public virtual System.Management.Automation.PowerShell CreateDisconnectedPowerShell (  ) { return default(System.Management.Automation.PowerShell); }
    public virtual System.Management.Automation.Runspaces.Pipeline CreateNestedPipeline ( string command, bool addToHistory ) { return default(System.Management.Automation.Runspaces.Pipeline); }
    public virtual System.Management.Automation.Runspaces.Pipeline CreateNestedPipeline (  ) { return default(System.Management.Automation.Runspaces.Pipeline); }
    public virtual System.Management.Automation.Runspaces.Pipeline CreatePipeline ( string command, bool addToHistory ) { return default(System.Management.Automation.Runspaces.Pipeline); }
    public virtual System.Management.Automation.Runspaces.Pipeline CreatePipeline ( string command ) { return default(System.Management.Automation.Runspaces.Pipeline); }
    public virtual System.Management.Automation.Runspaces.Pipeline CreatePipeline (  ) { return default(System.Management.Automation.Runspaces.Pipeline); }
    public virtual void Disconnect (  ) { }
    public virtual void DisconnectAsync (  ) { }
    protected virtual void Dispose ( bool disposing ) { }
    public virtual void Dispose (  ) { }
    public virtual System.Management.Automation.PSPrimitiveDictionary GetApplicationPrivateData (  ) { return default(System.Management.Automation.PSPrimitiveDictionary); }
    public virtual System.Management.Automation.Runspaces.RunspaceCapability GetCapabilities (  ) { return default(System.Management.Automation.Runspaces.RunspaceCapability); }
    public static System.Management.Automation.Runspaces.Runspace GetRunspace ( System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo, System.Guid sessionId, System.Nullable<System.Guid> commandId, System.Management.Automation.Host.PSHost host, System.Management.Automation.Runspaces.TypeTable typeTable ) { return default(System.Management.Automation.Runspaces.Runspace); }
    public static System.Management.Automation.Runspaces.Runspace[] GetRunspaces ( System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo, System.Management.Automation.Host.PSHost host ) { return default(System.Management.Automation.Runspaces.Runspace[]); }
    public static System.Management.Automation.Runspaces.Runspace[] GetRunspaces ( System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo ) { return default(System.Management.Automation.Runspaces.Runspace[]); }
    public static System.Management.Automation.Runspaces.Runspace[] GetRunspaces ( System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo, System.Management.Automation.Host.PSHost host, System.Management.Automation.Runspaces.TypeTable typeTable ) { return default(System.Management.Automation.Runspaces.Runspace[]); }
    protected virtual void OnAvailabilityChanged ( System.Management.Automation.Runspaces.RunspaceAvailabilityEventArgs e ) { }
    public virtual void Open (  ) { }
    public virtual void OpenAsync (  ) { }
    public virtual void ResetRunspaceState (  ) { }

  }

  public enum RunspaceAvailability {
    Available = 1,
    AvailableForNestedCommand = 2,
    Busy = 3,
    None = 0,
    RemoteDebug = 4,
  }

  public sealed class RunspaceAvailabilityEventArgs : System.EventArgs {
    internal RunspaceAvailabilityEventArgs() { }
    public System.Management.Automation.Runspaces.RunspaceAvailability RunspaceAvailability { get { return default(System.Management.Automation.Runspaces.RunspaceAvailability); } }
  }

  public enum RunspaceCapability {
    Default = 0,
    NamedPipeTransport = 2,
    SSHTransport = 8,
    SupportsDisconnect = 1,
    VMSocketTransport = 4,
  }

  public abstract class RunspaceConnectionInfo {
    protected RunspaceConnectionInfo() { }

    public abstract System.Management.Automation.Runspaces.AuthenticationMechanism AuthenticationMechanism { get; }
    public int CancelTimeout { get { return default(int); } set { } }
    public abstract string CertificateThumbprint { get; }
    public abstract string ComputerName { get; }
    public abstract System.Management.Automation.PSCredential Credential { get; }
    public System.Globalization.CultureInfo Culture { get { return default(System.Globalization.CultureInfo); } set { } }
    public int IdleTimeout { get { return default(int); } set { } }
    public int MaxIdleTimeout { get { return default(int); } set { } }
    public int OpenTimeout { get { return default(int); } set { } }
    public int OperationTimeout { get { return default(int); } set { } }
    public System.Globalization.CultureInfo UICulture { get { return default(System.Globalization.CultureInfo); } set { } }
    public virtual void SetSessionOptions ( System.Management.Automation.Remoting.PSSessionOption options ) { }

  }

  public static class RunspaceFactory {
    public static System.Management.Automation.Runspaces.Runspace CreateOutOfProcessRunspace ( System.Management.Automation.Runspaces.TypeTable typeTable, System.Management.Automation.Runspaces.PowerShellProcessInstance processInstance ) { return default(System.Management.Automation.Runspaces.Runspace); }
    public static System.Management.Automation.Runspaces.Runspace CreateOutOfProcessRunspace ( System.Management.Automation.Runspaces.TypeTable typeTable ) { return default(System.Management.Automation.Runspaces.Runspace); }
    public static System.Management.Automation.Runspaces.Runspace CreateRunspace ( System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo ) { return default(System.Management.Automation.Runspaces.Runspace); }
    public static System.Management.Automation.Runspaces.Runspace CreateRunspace ( System.Management.Automation.Host.PSHost host, System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo ) { return default(System.Management.Automation.Runspaces.Runspace); }
    public static System.Management.Automation.Runspaces.Runspace CreateRunspace ( System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo, System.Management.Automation.Host.PSHost host, System.Management.Automation.Runspaces.TypeTable typeTable, System.Management.Automation.PSPrimitiveDictionary applicationArguments, string name ) { return default(System.Management.Automation.Runspaces.Runspace); }
    public static System.Management.Automation.Runspaces.Runspace CreateRunspace ( System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo, System.Management.Automation.Host.PSHost host, System.Management.Automation.Runspaces.TypeTable typeTable, System.Management.Automation.PSPrimitiveDictionary applicationArguments ) { return default(System.Management.Automation.Runspaces.Runspace); }
    public static System.Management.Automation.Runspaces.Runspace CreateRunspace ( System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo, System.Management.Automation.Host.PSHost host, System.Management.Automation.Runspaces.TypeTable typeTable ) { return default(System.Management.Automation.Runspaces.Runspace); }
    public static System.Management.Automation.Runspaces.Runspace CreateRunspace (  ) { return default(System.Management.Automation.Runspaces.Runspace); }
    public static System.Management.Automation.Runspaces.Runspace CreateRunspace ( System.Management.Automation.Host.PSHost host, System.Management.Automation.Runspaces.InitialSessionState initialSessionState ) { return default(System.Management.Automation.Runspaces.Runspace); }
    public static System.Management.Automation.Runspaces.Runspace CreateRunspace ( System.Management.Automation.Runspaces.InitialSessionState initialSessionState ) { return default(System.Management.Automation.Runspaces.Runspace); }
    public static System.Management.Automation.Runspaces.Runspace CreateRunspace ( System.Management.Automation.Host.PSHost host ) { return default(System.Management.Automation.Runspaces.Runspace); }
    public static System.Management.Automation.Runspaces.RunspacePool CreateRunspacePool ( int minRunspaces, int maxRunspaces, System.Management.Automation.Host.PSHost host ) { return default(System.Management.Automation.Runspaces.RunspacePool); }
    public static System.Management.Automation.Runspaces.RunspacePool CreateRunspacePool ( int minRunspaces, int maxRunspaces, System.Management.Automation.Runspaces.InitialSessionState initialSessionState, System.Management.Automation.Host.PSHost host ) { return default(System.Management.Automation.Runspaces.RunspacePool); }
    public static System.Management.Automation.Runspaces.RunspacePool CreateRunspacePool ( int minRunspaces, int maxRunspaces ) { return default(System.Management.Automation.Runspaces.RunspacePool); }
    public static System.Management.Automation.Runspaces.RunspacePool CreateRunspacePool ( int minRunspaces, int maxRunspaces, System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo, System.Management.Automation.Host.PSHost host ) { return default(System.Management.Automation.Runspaces.RunspacePool); }
    public static System.Management.Automation.Runspaces.RunspacePool CreateRunspacePool ( int minRunspaces, int maxRunspaces, System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo, System.Management.Automation.Host.PSHost host, System.Management.Automation.Runspaces.TypeTable typeTable ) { return default(System.Management.Automation.Runspaces.RunspacePool); }
    public static System.Management.Automation.Runspaces.RunspacePool CreateRunspacePool ( int minRunspaces, int maxRunspaces, System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo, System.Management.Automation.Host.PSHost host, System.Management.Automation.Runspaces.TypeTable typeTable, System.Management.Automation.PSPrimitiveDictionary applicationArguments ) { return default(System.Management.Automation.Runspaces.RunspacePool); }
    public static System.Management.Automation.Runspaces.RunspacePool CreateRunspacePool (  ) { return default(System.Management.Automation.Runspaces.RunspacePool); }
    public static System.Management.Automation.Runspaces.RunspacePool CreateRunspacePool ( System.Management.Automation.Runspaces.InitialSessionState initialSessionState ) { return default(System.Management.Automation.Runspaces.RunspacePool); }
    public static System.Management.Automation.Runspaces.RunspacePool CreateRunspacePool ( int minRunspaces, int maxRunspaces, System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo ) { return default(System.Management.Automation.Runspaces.RunspacePool); }

  }

    [System.SerializableAttribute]
   public class RunspaceOpenModuleLoadException : System.Management.Automation.RuntimeException {
    public RunspaceOpenModuleLoadException() { }
    public RunspaceOpenModuleLoadException(string message) { }
    public RunspaceOpenModuleLoadException(string message, System.Exception innerException) { }
    protected RunspaceOpenModuleLoadException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

    public System.Management.Automation.PSDataCollection<System.Management.Automation.ErrorRecord> ErrorRecords { get { return default(System.Management.Automation.PSDataCollection<System.Management.Automation.ErrorRecord>); } }
    public override void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

  }

  internal class RunspaceCreatedEventArgs { }

  public sealed class RunspacePool : System.IDisposable {
    internal RunspacePool() { }
    internal event System.EventHandler<System.Management.Automation.PSEventArgs> ForwardEvent { add { } remove { } }
    internal event System.EventHandler<System.Management.Automation.PSEventArgs> InternalForwardEvent { add { } remove { } }
    internal event System.EventHandler<System.Management.Automation.Runspaces.RunspaceCreatedEventArgs> InternalRunspaceCreated { add { } remove { } }
    internal event System.EventHandler<System.Management.Automation.Runspaces.RunspacePoolStateChangedEventArgs> InternalStateChanged { add { } remove { } }
    internal event System.EventHandler<System.Management.Automation.Runspaces.RunspaceCreatedEventArgs> RunspaceCreated { add { } remove { } }
    public event System.EventHandler<System.Management.Automation.Runspaces.RunspacePoolStateChangedEventArgs> StateChanged { add { } remove { } }

    public System.TimeSpan CleanupInterval { get { return default(System.TimeSpan); } set { } }
    public System.Management.Automation.Runspaces.RunspaceConnectionInfo ConnectionInfo { get { return default(System.Management.Automation.Runspaces.RunspaceConnectionInfo); } }
    public System.Management.Automation.Runspaces.InitialSessionState InitialSessionState { get { return default(System.Management.Automation.Runspaces.InitialSessionState); } }
    public System.Guid InstanceId { get { return default(System.Guid); } }
    public bool IsDisposed { get { return default(bool); } }
    public System.Management.Automation.Runspaces.RunspacePoolAvailability RunspacePoolAvailability { get { return default(System.Management.Automation.Runspaces.RunspacePoolAvailability); } }
    public System.Management.Automation.RunspacePoolStateInfo RunspacePoolStateInfo { get { return default(System.Management.Automation.RunspacePoolStateInfo); } }
    public System.Management.Automation.Runspaces.PSThreadOptions ThreadOptions { get { return default(System.Management.Automation.Runspaces.PSThreadOptions); } set { } }
    public System.IAsyncResult BeginClose ( System.AsyncCallback callback, object state ) { return default(System.IAsyncResult); }
    public System.IAsyncResult BeginConnect ( System.AsyncCallback callback, object state ) { return default(System.IAsyncResult); }
    public System.IAsyncResult BeginDisconnect ( System.AsyncCallback callback, object state ) { return default(System.IAsyncResult); }
    public System.IAsyncResult BeginOpen ( System.AsyncCallback callback, object state ) { return default(System.IAsyncResult); }
    public void Close (  ) { }
    public void Connect (  ) { }
    public System.Collections.ObjectModel.Collection<System.Management.Automation.PowerShell> CreateDisconnectedPowerShells (  ) { return default(System.Collections.ObjectModel.Collection<System.Management.Automation.PowerShell>); }
    public void Disconnect (  ) { }
    public void Dispose (  ) { }
    public void EndClose ( System.IAsyncResult asyncResult ) { }
    public void EndConnect ( System.IAsyncResult asyncResult ) { }
    public void EndDisconnect ( System.IAsyncResult asyncResult ) { }
    public void EndOpen ( System.IAsyncResult asyncResult ) { }
    public System.Management.Automation.PSPrimitiveDictionary GetApplicationPrivateData (  ) { return default(System.Management.Automation.PSPrimitiveDictionary); }
    public int GetAvailableRunspaces (  ) { return default(int); }
    public System.Management.Automation.Runspaces.RunspacePoolCapability GetCapabilities (  ) { return default(System.Management.Automation.Runspaces.RunspacePoolCapability); }
    public int GetMaxRunspaces (  ) { return default(int); }
    public int GetMinRunspaces (  ) { return default(int); }
    public static System.Management.Automation.Runspaces.RunspacePool[] GetRunspacePools ( System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo ) { return default(System.Management.Automation.Runspaces.RunspacePool[]); }
    public static System.Management.Automation.Runspaces.RunspacePool[] GetRunspacePools ( System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo, System.Management.Automation.Host.PSHost host ) { return default(System.Management.Automation.Runspaces.RunspacePool[]); }
    public static System.Management.Automation.Runspaces.RunspacePool[] GetRunspacePools ( System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo, System.Management.Automation.Host.PSHost host, System.Management.Automation.Runspaces.TypeTable typeTable ) { return default(System.Management.Automation.Runspaces.RunspacePool[]); }
    public void Open (  ) { }
    public bool SetMaxRunspaces ( int maxRunspaces ) { return default(bool); }
    public bool SetMinRunspaces ( int minRunspaces ) { return default(bool); }

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
    public override string ToString (  ) { return default(string); }

  }

   [System.Diagnostics.DebuggerDisplayAttribute("ScriptMethod: {Name,nq}")]
   public sealed class ScriptMethodData : System.Management.Automation.Runspaces.TypeMemberData {
    public ScriptMethodData(string name, System.Management.Automation.ScriptBlock scriptToInvoke) { }

    public System.Management.Automation.ScriptBlock Script { get { return default(System.Management.Automation.ScriptBlock); } set { } }
  }

   [System.Diagnostics.DebuggerDisplayAttribute("ScriptProperty: {Name,nq}")]
   public sealed class ScriptPropertyData : System.Management.Automation.Runspaces.TypeMemberData {
    public ScriptPropertyData(string name, System.Management.Automation.ScriptBlock getScriptBlock) { }
    public ScriptPropertyData(string name, System.Management.Automation.ScriptBlock getScriptBlock, System.Management.Automation.ScriptBlock setScriptBlock) { }

    public System.Management.Automation.ScriptBlock GetScriptBlock { get { return default(System.Management.Automation.ScriptBlock); } set { } }
    public bool IsHidden { get { return default(bool); } set { } }
    public System.Management.Automation.ScriptBlock SetScriptBlock { get { return default(System.Management.Automation.ScriptBlock); } set { } }
  }

  public sealed class SessionStateAliasEntry : System.Management.Automation.Runspaces.SessionStateCommandEntry {
    public SessionStateAliasEntry(string name, string definition) : base (name) { }
    public SessionStateAliasEntry(string name, string definition, string description) : base (name) { }
    public SessionStateAliasEntry(string name, string definition, string description, System.Management.Automation.ScopedItemOptions options) : base (name) { }

    public string Definition { get { return default(string); } }
    public string Description { get { return default(string); } }
    public System.Management.Automation.ScopedItemOptions Options { get { return default(System.Management.Automation.ScopedItemOptions); } }
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone (  ) { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }

  }

  public sealed class SessionStateApplicationEntry : System.Management.Automation.Runspaces.SessionStateCommandEntry {
    public SessionStateApplicationEntry(string path) : base (default(string)) { }

    public string Path { get { return default(string); } }
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone (  ) { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }

  }

  public sealed class SessionStateAssemblyEntry : System.Management.Automation.Runspaces.InitialSessionStateEntry {
    public SessionStateAssemblyEntry(string name, string fileName) : base (name) { }
    public SessionStateAssemblyEntry(string name) : base (name) { }

    public string FileName { get { return default(string); } }
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone (  ) { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }

  }

  public sealed class SessionStateCmdletEntry : System.Management.Automation.Runspaces.SessionStateCommandEntry {
    public SessionStateCmdletEntry(string name, System.Type implementingType, string helpFileName) : base (name) { }

    public string HelpFileName { get { return default(string); } }
    public System.Type ImplementingType { get { return default(System.Type); } }
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone (  ) { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }

  }

  public abstract class SessionStateCommandEntry : System.Management.Automation.Runspaces.ConstrainedSessionStateEntry {
    protected SessionStateCommandEntry(string name) : base (name, default(System.Management.Automation.SessionStateEntryVisibility)) { }

    public System.Management.Automation.CommandTypes CommandType { get { return default(System.Management.Automation.CommandTypes); } set { } }
  }

  public sealed class SessionStateFormatEntry : System.Management.Automation.Runspaces.InitialSessionStateEntry {
    public SessionStateFormatEntry(string fileName) : base(fileName) { }
    public SessionStateFormatEntry(System.Management.Automation.Runspaces.FormatTable formattable) : base(default(string)) { }
    public SessionStateFormatEntry(System.Management.Automation.ExtendedTypeDefinition typeDefinition) : base(default(string)) { }

    public string FileName { get { return default(string); } }
    public System.Management.Automation.ExtendedTypeDefinition FormatData { get { return default(System.Management.Automation.ExtendedTypeDefinition); } }
    public System.Management.Automation.Runspaces.FormatTable Formattable { get { return default(System.Management.Automation.Runspaces.FormatTable); } }
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone (  ) { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }

  }

  public sealed class SessionStateFunctionEntry : System.Management.Automation.Runspaces.SessionStateCommandEntry {
    public SessionStateFunctionEntry(string name, string definition, System.Management.Automation.ScopedItemOptions options, string helpFile) : base (name) { }
    public SessionStateFunctionEntry(string name, string definition, string helpFile) : base (name) { }
    public SessionStateFunctionEntry(string name, string definition) : base (name) { }

    public string Definition { get { return default(string); } }
    public string HelpFile { get { return default(string); } set { } }
    public System.Management.Automation.ScopedItemOptions Options { get { return default(System.Management.Automation.ScopedItemOptions); } }
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone (  ) { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }

  }

  public sealed class SessionStateProviderEntry : System.Management.Automation.Runspaces.ConstrainedSessionStateEntry {
    public SessionStateProviderEntry(string name, System.Type implementingType, string helpFileName) : base (name, default(System.Management.Automation.SessionStateEntryVisibility)) { }

    public string HelpFileName { get { return default(string); } }
    public System.Type ImplementingType { get { return default(System.Type); } }
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone (  ) { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }

  }

  public class SessionStateProxy {
    internal SessionStateProxy() { }
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
    public object GetVariable ( string name ) { return default(object); }
    public void SetVariable ( string name, object value ) { }

  }

  public sealed class SessionStateScriptEntry : System.Management.Automation.Runspaces.SessionStateCommandEntry {
    public SessionStateScriptEntry(string path): base(default(string)) { }

    public string Path { get { return default(string); } }
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone (  ) { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }

  }

  public sealed class SessionStateTypeEntry : System.Management.Automation.Runspaces.InitialSessionStateEntry {
    public SessionStateTypeEntry(string fileName) : base (default(string)) { }
    public SessionStateTypeEntry(System.Management.Automation.Runspaces.TypeTable typeTable) : base (default(string)) { }
    public SessionStateTypeEntry(System.Management.Automation.Runspaces.TypeData typeData, bool isRemove) : base (default(string)) { }

    public string FileName { get { return default(string); } }
    public bool IsRemove { get { return default(bool); } }
    public System.Management.Automation.Runspaces.TypeData TypeData { get { return default(System.Management.Automation.Runspaces.TypeData); } }
    public System.Management.Automation.Runspaces.TypeTable TypeTable { get { return default(System.Management.Automation.Runspaces.TypeTable); } }
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone (  ) { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }

  }

  public sealed class SessionStateVariableEntry : System.Management.Automation.Runspaces.ConstrainedSessionStateEntry {
    public SessionStateVariableEntry(string name, object value, string description) : base(name, default(System.Management.Automation.SessionStateEntryVisibility)) { }
    public SessionStateVariableEntry(string name, object value, string description, System.Management.Automation.ScopedItemOptions options) : base(name, default(System.Management.Automation.SessionStateEntryVisibility)) { }
    public SessionStateVariableEntry(string name, object value, string description, System.Management.Automation.ScopedItemOptions options, System.Collections.ObjectModel.Collection<System.Attribute> attributes) : base(name, default(System.Management.Automation.SessionStateEntryVisibility)) { }
    public SessionStateVariableEntry(string name, object value, string description, System.Management.Automation.ScopedItemOptions options, System.Attribute attribute) : base(name, default(System.Management.Automation.SessionStateEntryVisibility)) { }

    public System.Collections.ObjectModel.Collection<System.Attribute> Attributes { get { return default(System.Collections.ObjectModel.Collection<System.Attribute>); } }
    public string Description { get { return default(string); } }
    public System.Management.Automation.ScopedItemOptions Options { get { return default(System.Management.Automation.ScopedItemOptions); } }
    public object Value { get { return default(object); } }
    public override System.Management.Automation.Runspaces.InitialSessionStateEntry Clone (  ) { return default(System.Management.Automation.Runspaces.InitialSessionStateEntry); }

  }

  public sealed class SSHConnectionInfo : System.Management.Automation.Runspaces.RunspaceConnectionInfo {
    public SSHConnectionInfo(string userName, string computerName, string keyFilePath) { }
    public SSHConnectionInfo(string userName, string computerName, string keyFilePath, int port) { }

    public override System.Management.Automation.Runspaces.AuthenticationMechanism AuthenticationMechanism { get { return default(System.Management.Automation.Runspaces.AuthenticationMechanism); } }
    public override string CertificateThumbprint { get { return default(string); } }
    public override string ComputerName { get { return default(string); } }
    public override System.Management.Automation.PSCredential Credential { get { return default(System.Management.Automation.PSCredential); } }
    public string UserName { get { return default(string); } set { } }
  }

  public enum TargetMachineType {
    Container = 2,
    RemoteMachine = 0,
    VirtualMachine = 1,
  }

  public sealed class TypeData {
    public TypeData(string typeName) { }
    public TypeData(System.Type type) { }

    public string DefaultDisplayProperty { get { return default(string); } set { } }
    public System.Management.Automation.Runspaces.PropertySetData DefaultDisplayPropertySet { get { return default(System.Management.Automation.Runspaces.PropertySetData); } set { } }
    public System.Management.Automation.Runspaces.PropertySetData DefaultKeyPropertySet { get { return default(System.Management.Automation.Runspaces.PropertySetData); } set { } }
    public bool InheritPropertySerializationSet { get { return default(bool); } set { } }
    public bool IsOverride { get { return default(bool); } set { } }
    public System.Collections.Generic.Dictionary<string, System.Management.Automation.Runspaces.TypeMemberData> Members { get { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.Runspaces.TypeMemberData>); } set { } }
    public System.Management.Automation.Runspaces.PropertySetData PropertySerializationSet { get { return default(System.Management.Automation.Runspaces.PropertySetData); } set { } }
    public uint SerializationDepth { get { return default(uint); } set { } }
    public string SerializationMethod { get { return default(string); } set { } }
    public string StringSerializationSource { get { return default(string); } set { } }
    public System.Management.Automation.Runspaces.TypeMemberData StringSerializationSourceProperty { get { return default(System.Management.Automation.Runspaces.TypeMemberData); } set { } }
    public System.Type TargetTypeForDeserialization { get { return default(System.Type); } set { } }
    public System.Type TypeAdapter { get { return default(System.Type); } set { } }
    public System.Type TypeConverter { get { return default(System.Type); } set { } }
    public string TypeName { get { return default(string); } set { } }
    public System.Management.Automation.Runspaces.TypeData Copy (  ) { return default(System.Management.Automation.Runspaces.TypeData); }

  }

  public abstract class TypeMemberData {
    internal TypeMemberData() { }
    public string Name { get { return default(string); } set { } }
  }

  public sealed class TypeTable {
    public TypeTable(System.Collections.Generic.IEnumerable<string> typeFiles) { }

    public void AddType ( System.Management.Automation.Runspaces.TypeData typeData ) { }
    public System.Management.Automation.Runspaces.TypeTable Clone ( bool unshared ) { return default(System.Management.Automation.Runspaces.TypeTable); }
    public static System.Collections.Generic.List<System.String> GetDefaultTypeFiles (  ) { return default(System.Collections.Generic.List<System.String>); }
    public static System.Management.Automation.Runspaces.TypeTable LoadDefaultTypeFiles (  ) { return default(System.Management.Automation.Runspaces.TypeTable); }
    public void RemoveType ( string typeName ) { }

  }

    [System.SerializableAttribute]
   public class TypeTableLoadException : System.Management.Automation.RuntimeException {
    public TypeTableLoadException() { }
    public TypeTableLoadException(string message) { }
    public TypeTableLoadException(string message, System.Exception innerException) { }
    protected TypeTableLoadException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }

    public System.Collections.ObjectModel.Collection<string> Errors { get { return default(System.Collections.ObjectModel.Collection<string>); } }
    public override void GetObjectData ( System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context ) { }

  }

  public sealed class VMConnectionInfo : System.Management.Automation.Runspaces.RunspaceConnectionInfo {
    public override System.Management.Automation.Runspaces.AuthenticationMechanism AuthenticationMechanism { get { return default(System.Management.Automation.Runspaces.AuthenticationMechanism); } }
    public override string CertificateThumbprint { get { return default(string); } }
    public override string ComputerName { get { return default(string); } }
    public string ConfigurationName { get { return default(string); } set { } }
    public override System.Management.Automation.PSCredential Credential { get { return default(System.Management.Automation.PSCredential); } }
    public System.Guid VMGuid { get { return default(System.Guid); } set { } }
  }

  public sealed class WSManConnectionInfo : System.Management.Automation.Runspaces.RunspaceConnectionInfo {
    public WSManConnectionInfo(string scheme, string computerName, int port, string appName, string shellUri, System.Management.Automation.PSCredential credential, int openTimeout) { }
    public WSManConnectionInfo(string scheme, string computerName, int port, string appName, string shellUri, System.Management.Automation.PSCredential credential) { }
    public WSManConnectionInfo(bool useSsl, string computerName, int port, string appName, string shellUri, System.Management.Automation.PSCredential credential) { }
    public WSManConnectionInfo(bool useSsl, string computerName, int port, string appName, string shellUri, System.Management.Automation.PSCredential credential, int openTimeout) { }
    public WSManConnectionInfo() { }
    public WSManConnectionInfo(System.Uri uri, string shellUri, System.Management.Automation.PSCredential credential) { }
    public WSManConnectionInfo(System.Uri uri, string shellUri, string certificateThumbprint) { }
    public WSManConnectionInfo(System.Uri uri) { }
    public WSManConnectionInfo(System.Management.Automation.Runspaces.PSSessionType configurationType) { }

    public const string HttpScheme = "http";
    public const string HttpsScheme = "https";
    public string AppName { get { return default(string); } set { } }
    public override System.Management.Automation.Runspaces.AuthenticationMechanism AuthenticationMechanism { get { return default(System.Management.Automation.Runspaces.AuthenticationMechanism); } }
    public override string CertificateThumbprint { get { return default(string); } }
    public override string ComputerName { get { return default(string); } }
    public System.Uri ConnectionUri { get { return default(System.Uri); } set { } }
    public override System.Management.Automation.PSCredential Credential { get { return default(System.Management.Automation.PSCredential); } }
    public bool EnableNetworkAccess { get { return default(bool); } set { } }
    public bool IncludePortInSPN { get { return default(bool); } set { } }
    public int MaxConnectionRetryCount { get { return default(int); } set { } }
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
    public System.Management.Automation.Runspaces.WSManConnectionInfo Copy (  ) { return default(System.Management.Automation.Runspaces.WSManConnectionInfo); }
    public override void SetSessionOptions ( System.Management.Automation.Remoting.PSSessionOption options ) { }

  }

}
namespace System.Management.Automation.Language {
  public class ArrayExpressionAst : System.Management.Automation.Language.ExpressionAst {
    public ArrayExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.StatementBlockAst statementBlock) : base(extent) { }

    public override System.Type StaticType { get { return default(System.Type); } }
    public System.Management.Automation.Language.StatementBlockAst SubExpression { get { return default(System.Management.Automation.Language.StatementBlockAst); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class ArrayLiteralAst : System.Management.Automation.Language.ExpressionAst {
    public ArrayLiteralAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IList<System.Management.Automation.Language.ExpressionAst> elements) : base(extent) { }

    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst> Elements { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst>); } set { } }
    public override System.Type StaticType { get { return default(System.Type); } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public sealed class ArrayTypeName : System.Management.Automation.Language.ITypeName {
    public ArrayTypeName(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ITypeName elementType, int rank) { }

    public string AssemblyName { get { return default(string); } }
    public System.Management.Automation.Language.ITypeName ElementType { get { return default(System.Management.Automation.Language.ITypeName); } set { } }
    public System.Management.Automation.Language.IScriptExtent Extent { get { return default(System.Management.Automation.Language.IScriptExtent); } }
    public string FullName { get { return default(string); } }
    public bool IsArray { get { return default(bool); } }
    public bool IsGeneric { get { return default(bool); } }
    public string Name { get { return default(string); } }
    public int Rank { get { return default(int); } set { } }
    public override bool Equals ( object obj ) { return default(bool); }
    public override int GetHashCode (  ) { return default(int); }
    public System.Type GetReflectionAttributeType (  ) { return default(System.Type); }
    public System.Type GetReflectionType (  ) { return default(System.Type); }
    public override string ToString (  ) { return default(string); }

  }

  public class AssignmentStatementAst : System.Management.Automation.Language.PipelineBaseAst {
    public AssignmentStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ExpressionAst left, System.Management.Automation.Language.TokenKind @operator, System.Management.Automation.Language.StatementAst right, System.Management.Automation.Language.IScriptExtent errorPosition) : base(extent) { }

    public System.Management.Automation.Language.IScriptExtent ErrorPosition { get { return default(System.Management.Automation.Language.IScriptExtent); } set { } }
    public System.Management.Automation.Language.ExpressionAst Left { get { return default(System.Management.Automation.Language.ExpressionAst); } set { } }
    public System.Management.Automation.Language.TokenKind Operator { get { return default(System.Management.Automation.Language.TokenKind); } set { } }
    public System.Management.Automation.Language.StatementAst Right { get { return default(System.Management.Automation.Language.StatementAst); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }
    public System.Collections.Generic.IEnumerable<System.Management.Automation.Language.ExpressionAst> GetAssignmentTargets (  ) { return default(System.Collections.Generic.IEnumerable<System.Management.Automation.Language.ExpressionAst>); }

  }

  public abstract class Ast {
    protected Ast(System.Management.Automation.Language.IScriptExtent extent) { }

    public System.Management.Automation.Language.IScriptExtent Extent { get { return default(System.Management.Automation.Language.IScriptExtent); } set { } }
    public System.Management.Automation.Language.Ast Parent { get { return default(System.Management.Automation.Language.Ast); } set { } }
    public virtual System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }
    public System.Management.Automation.Language.Ast Find ( System.Func<System.Management.Automation.Language.Ast, bool> predicate, bool searchNestedScriptBlocks ) { return default(System.Management.Automation.Language.Ast); }
    public System.Collections.Generic.IEnumerable<System.Management.Automation.Language.Ast> FindAll ( System.Func<System.Management.Automation.Language.Ast, bool> predicate, bool searchNestedScriptBlocks ) { return default(System.Collections.Generic.IEnumerable<System.Management.Automation.Language.Ast>); }
    public object SafeGetValue (  ) { return default(object); }
    public override string ToString (  ) { return default(string); }
    public void Visit ( System.Management.Automation.Language.AstVisitor astVisitor ) { }
    public object Visit ( System.Management.Automation.Language.ICustomAstVisitor astVisitor ) { return default(object); }

  }

  public enum AstVisitAction {
    Continue = 0,
    SkipChildren = 1,
    StopVisit = 2,
  }

  public abstract class AstVisitor {
    protected AstVisitor() { }

    public virtual System.Management.Automation.Language.AstVisitAction VisitArrayExpression ( System.Management.Automation.Language.ArrayExpressionAst arrayExpressionAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitArrayLiteral ( System.Management.Automation.Language.ArrayLiteralAst arrayLiteralAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitAssignmentStatement ( System.Management.Automation.Language.AssignmentStatementAst assignmentStatementAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitAttribute ( System.Management.Automation.Language.AttributeAst attributeAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitAttributedExpression ( System.Management.Automation.Language.AttributedExpressionAst attributedExpressionAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitBinaryExpression ( System.Management.Automation.Language.BinaryExpressionAst binaryExpressionAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitBlockStatement ( System.Management.Automation.Language.BlockStatementAst blockStatementAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitBreakStatement ( System.Management.Automation.Language.BreakStatementAst breakStatementAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitCatchClause ( System.Management.Automation.Language.CatchClauseAst catchClauseAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitCommand ( System.Management.Automation.Language.CommandAst commandAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitCommandExpression ( System.Management.Automation.Language.CommandExpressionAst commandExpressionAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitCommandParameter ( System.Management.Automation.Language.CommandParameterAst commandParameterAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitConstantExpression ( System.Management.Automation.Language.ConstantExpressionAst constantExpressionAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitContinueStatement ( System.Management.Automation.Language.ContinueStatementAst continueStatementAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitConvertExpression ( System.Management.Automation.Language.ConvertExpressionAst convertExpressionAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitDataStatement ( System.Management.Automation.Language.DataStatementAst dataStatementAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitDoUntilStatement ( System.Management.Automation.Language.DoUntilStatementAst doUntilStatementAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitDoWhileStatement ( System.Management.Automation.Language.DoWhileStatementAst doWhileStatementAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitErrorExpression ( System.Management.Automation.Language.ErrorExpressionAst errorExpressionAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitErrorStatement ( System.Management.Automation.Language.ErrorStatementAst errorStatementAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitExitStatement ( System.Management.Automation.Language.ExitStatementAst exitStatementAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitExpandableStringExpression ( System.Management.Automation.Language.ExpandableStringExpressionAst expandableStringExpressionAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitFileRedirection ( System.Management.Automation.Language.FileRedirectionAst redirectionAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitForEachStatement ( System.Management.Automation.Language.ForEachStatementAst forEachStatementAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitForStatement ( System.Management.Automation.Language.ForStatementAst forStatementAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitFunctionDefinition ( System.Management.Automation.Language.FunctionDefinitionAst functionDefinitionAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitHashtable ( System.Management.Automation.Language.HashtableAst hashtableAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitIfStatement ( System.Management.Automation.Language.IfStatementAst ifStmtAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitIndexExpression ( System.Management.Automation.Language.IndexExpressionAst indexExpressionAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitInvokeMemberExpression ( System.Management.Automation.Language.InvokeMemberExpressionAst methodCallAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitMemberExpression ( System.Management.Automation.Language.MemberExpressionAst memberExpressionAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitMergingRedirection ( System.Management.Automation.Language.MergingRedirectionAst redirectionAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitNamedAttributeArgument ( System.Management.Automation.Language.NamedAttributeArgumentAst namedAttributeArgumentAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitNamedBlock ( System.Management.Automation.Language.NamedBlockAst namedBlockAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitParamBlock ( System.Management.Automation.Language.ParamBlockAst paramBlockAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitParameter ( System.Management.Automation.Language.ParameterAst parameterAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitParenExpression ( System.Management.Automation.Language.ParenExpressionAst parenExpressionAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitPipeline ( System.Management.Automation.Language.PipelineAst pipelineAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitReturnStatement ( System.Management.Automation.Language.ReturnStatementAst returnStatementAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitScriptBlock ( System.Management.Automation.Language.ScriptBlockAst scriptBlockAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitScriptBlockExpression ( System.Management.Automation.Language.ScriptBlockExpressionAst scriptBlockExpressionAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitStatementBlock ( System.Management.Automation.Language.StatementBlockAst statementBlockAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitStringConstantExpression ( System.Management.Automation.Language.StringConstantExpressionAst stringConstantExpressionAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitSubExpression ( System.Management.Automation.Language.SubExpressionAst subExpressionAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitSwitchStatement ( System.Management.Automation.Language.SwitchStatementAst switchStatementAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitThrowStatement ( System.Management.Automation.Language.ThrowStatementAst throwStatementAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitTrap ( System.Management.Automation.Language.TrapStatementAst trapStatementAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitTryStatement ( System.Management.Automation.Language.TryStatementAst tryStatementAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitTypeConstraint ( System.Management.Automation.Language.TypeConstraintAst typeConstraintAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitTypeExpression ( System.Management.Automation.Language.TypeExpressionAst typeExpressionAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitUnaryExpression ( System.Management.Automation.Language.UnaryExpressionAst unaryExpressionAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitUsingExpression ( System.Management.Automation.Language.UsingExpressionAst usingExpressionAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitVariableExpression ( System.Management.Automation.Language.VariableExpressionAst variableExpressionAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitWhileStatement ( System.Management.Automation.Language.WhileStatementAst whileStatementAst ) { return default(System.Management.Automation.Language.AstVisitAction); }

  }

  public abstract class AstVisitor2 : System.Management.Automation.Language.AstVisitor {
    protected AstVisitor2() { }

    public virtual System.Management.Automation.Language.AstVisitAction VisitBaseCtorInvokeMemberExpression ( System.Management.Automation.Language.BaseCtorInvokeMemberExpressionAst baseCtorInvokeMemberExpressionAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitConfigurationDefinition ( System.Management.Automation.Language.ConfigurationDefinitionAst configurationDefinitionAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitDynamicKeywordStatement ( System.Management.Automation.Language.DynamicKeywordStatementAst dynamicKeywordStatementAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitFunctionMember ( System.Management.Automation.Language.FunctionMemberAst functionMemberAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitPropertyMember ( System.Management.Automation.Language.PropertyMemberAst propertyMemberAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitTypeDefinition ( System.Management.Automation.Language.TypeDefinitionAst typeDefinitionAst ) { return default(System.Management.Automation.Language.AstVisitAction); }
    public virtual System.Management.Automation.Language.AstVisitAction VisitUsingStatement ( System.Management.Automation.Language.UsingStatementAst usingStatementAst ) { return default(System.Management.Automation.Language.AstVisitAction); }

  }

  public class AttributeAst : System.Management.Automation.Language.AttributeBaseAst {
    public AttributeAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ITypeName typeName, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.ExpressionAst> positionalArguments, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.NamedAttributeArgumentAst> namedArguments) : base(extent, default(System.Management.Automation.Language.ITypeName)) { }

    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.NamedAttributeArgumentAst> NamedArguments { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.NamedAttributeArgumentAst>); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst> PositionalArguments { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst>); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public abstract class AttributeBaseAst : System.Management.Automation.Language.Ast {
    protected AttributeBaseAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ITypeName typeName) : base(extent) { }

    public System.Management.Automation.Language.ITypeName TypeName { get { return default(System.Management.Automation.Language.ITypeName); } set { } }
  }

  public class AttributedExpressionAst : System.Management.Automation.Language.ExpressionAst {
    public AttributedExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.AttributeBaseAst attribute, System.Management.Automation.Language.ExpressionAst child) : base(extent) { }

    public System.Management.Automation.Language.AttributeBaseAst Attribute { get { return default(System.Management.Automation.Language.AttributeBaseAst); } set { } }
    public System.Management.Automation.Language.ExpressionAst Child { get { return default(System.Management.Automation.Language.ExpressionAst); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class BaseCtorInvokeMemberExpressionAst : System.Management.Automation.Language.InvokeMemberExpressionAst {
    public BaseCtorInvokeMemberExpressionAst(System.Management.Automation.Language.IScriptExtent baseKeywordExtent, System.Management.Automation.Language.IScriptExtent baseCallExtent, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.ExpressionAst> arguments) : base(baseKeywordExtent, default(System.Management.Automation.Language.ExpressionAst), default(System.Management.Automation.Language.CommandElementAst), arguments, default(bool)) { }

  }

  public class BinaryExpressionAst : System.Management.Automation.Language.ExpressionAst {
    public BinaryExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ExpressionAst left, System.Management.Automation.Language.TokenKind @operator, System.Management.Automation.Language.ExpressionAst right, System.Management.Automation.Language.IScriptExtent errorPosition) : base(extent) { }

    public System.Management.Automation.Language.IScriptExtent ErrorPosition { get { return default(System.Management.Automation.Language.IScriptExtent); } set { } }
    public System.Management.Automation.Language.ExpressionAst Left { get { return default(System.Management.Automation.Language.ExpressionAst); } set { } }
    public System.Management.Automation.Language.TokenKind Operator { get { return default(System.Management.Automation.Language.TokenKind); } set { } }
    public System.Management.Automation.Language.ExpressionAst Right { get { return default(System.Management.Automation.Language.ExpressionAst); } set { } }
    public override System.Type StaticType { get { return default(System.Type); } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class BlockStatementAst : System.Management.Automation.Language.StatementAst {
    public BlockStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.Token kind, System.Management.Automation.Language.StatementBlockAst body) : base(extent) { }

    public System.Management.Automation.Language.StatementBlockAst Body { get { return default(System.Management.Automation.Language.StatementBlockAst); } set { } }
    public System.Management.Automation.Language.Token Kind { get { return default(System.Management.Automation.Language.Token); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class BreakStatementAst : System.Management.Automation.Language.StatementAst {
    public BreakStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ExpressionAst label) : base(extent) { }

    public System.Management.Automation.Language.ExpressionAst Label { get { return default(System.Management.Automation.Language.ExpressionAst); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class CatchClauseAst : System.Management.Automation.Language.Ast {
    public CatchClauseAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.TypeConstraintAst> catchTypes, System.Management.Automation.Language.StatementBlockAst body) : base(extent) { }

    public System.Management.Automation.Language.StatementBlockAst Body { get { return default(System.Management.Automation.Language.StatementBlockAst); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.TypeConstraintAst> CatchTypes { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.TypeConstraintAst>); } set { } }
    public bool IsCatchAll { get { return default(bool); } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public static class CodeGeneration {
    public static string EscapeBlockCommentContent ( string value ) { return default(string); }
    public static string EscapeFormatStringContent ( string value ) { return default(string); }
    public static string EscapeSingleQuotedStringContent ( string value ) { return default(string); }
    public static string EscapeVariableName ( string value ) { return default(string); }

  }

  public class CommandAst : System.Management.Automation.Language.CommandBaseAst {
    public CommandAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.CommandElementAst> commandElements, System.Management.Automation.Language.TokenKind invocationOperator, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.RedirectionAst> redirections) : base(extent, redirections) { }

    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.CommandElementAst> CommandElements { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.CommandElementAst>); } set { } }
    public System.Management.Automation.Language.DynamicKeyword DefiningKeyword { get { return default(System.Management.Automation.Language.DynamicKeyword); } set { } }
    public System.Management.Automation.Language.TokenKind InvocationOperator { get { return default(System.Management.Automation.Language.TokenKind); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }
    public string GetCommandName (  ) { return default(string); }

  }

  public abstract class CommandBaseAst : System.Management.Automation.Language.StatementAst {
    protected CommandBaseAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.RedirectionAst> redirections) : base(extent) { }

    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.RedirectionAst> Redirections { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.RedirectionAst>); } set { } }
  }

  public abstract class CommandElementAst : System.Management.Automation.Language.Ast {
    protected CommandElementAst(System.Management.Automation.Language.IScriptExtent extent) : base(extent) { }

  }

  public class CommandExpressionAst : System.Management.Automation.Language.CommandBaseAst {
    public CommandExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ExpressionAst expression, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.RedirectionAst> redirections) : base(extent, redirections) { }

    public System.Management.Automation.Language.ExpressionAst Expression { get { return default(System.Management.Automation.Language.ExpressionAst); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class CommandParameterAst : System.Management.Automation.Language.CommandElementAst {
    public CommandParameterAst(System.Management.Automation.Language.IScriptExtent extent, string parameterName, System.Management.Automation.Language.ExpressionAst argument, System.Management.Automation.Language.IScriptExtent errorPosition) : base(extent) { }

    public System.Management.Automation.Language.ExpressionAst Argument { get { return default(System.Management.Automation.Language.ExpressionAst); } set { } }
    public System.Management.Automation.Language.IScriptExtent ErrorPosition { get { return default(System.Management.Automation.Language.IScriptExtent); } set { } }
    public string ParameterName { get { return default(string); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public sealed class CommentHelpInfo {
    public CommentHelpInfo() { }

    public string Component { get { return default(string); } set { } }
    public string Description { get { return default(string); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<string> Examples { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<string>); } set { } }
    public string ForwardHelpCategory { get { return default(string); } set { } }
    public string ForwardHelpTargetName { get { return default(string); } set { } }
    public string Functionality { get { return default(string); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<string> Inputs { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<string>); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<string> Links { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<string>); } set { } }
    public string MamlHelpFile { get { return default(string); } set { } }
    public string Notes { get { return default(string); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<string> Outputs { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<string>); } set { } }
    public System.Collections.Generic.IDictionary<string, string> Parameters { get { return default(System.Collections.Generic.IDictionary<string, string>); } set { } }
    public string RemoteHelpRunspace { get { return default(string); } set { } }
    public string Role { get { return default(string); } set { } }
    public string Synopsis { get { return default(string); } set { } }
    public string GetCommentBlock (  ) { return default(string); }

  }

  public class ConfigurationDefinitionAst : System.Management.Automation.Language.StatementAst {
    public ConfigurationDefinitionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ScriptBlockExpressionAst body, System.Management.Automation.Language.ConfigurationType type, System.Management.Automation.Language.ExpressionAst instanceName) : base(extent) { }

    public System.Management.Automation.Language.ScriptBlockExpressionAst Body { get { return default(System.Management.Automation.Language.ScriptBlockExpressionAst); } set { } }
    public System.Management.Automation.Language.ConfigurationType ConfigurationType { get { return default(System.Management.Automation.Language.ConfigurationType); } set { } }
    public System.Management.Automation.Language.ExpressionAst InstanceName { get { return default(System.Management.Automation.Language.ExpressionAst); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public enum ConfigurationType {
    Meta = 1,
    Resource = 0,
  }

  public class ConstantExpressionAst : System.Management.Automation.Language.ExpressionAst {
    public ConstantExpressionAst(System.Management.Automation.Language.IScriptExtent extent, object value) : base(extent) { }

    public override System.Type StaticType { get { return default(System.Type); } }
    public object Value { get { return default(object); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class ContinueStatementAst : System.Management.Automation.Language.StatementAst {
    public ContinueStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ExpressionAst label) : base(extent) { }

    public System.Management.Automation.Language.ExpressionAst Label { get { return default(System.Management.Automation.Language.ExpressionAst); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class ConvertExpressionAst : System.Management.Automation.Language.AttributedExpressionAst {
    public ConvertExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.TypeConstraintAst typeConstraint, System.Management.Automation.Language.ExpressionAst child) : base(extent, default(System.Management.Automation.Language.AttributeBaseAst), default(System.Management.Automation.Language.ExpressionAst)) { }
    public override System.Type StaticType { get { return default(System.Type); } }
    public System.Management.Automation.Language.TypeConstraintAst Type { get { return default(System.Management.Automation.Language.TypeConstraintAst); } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class DataStatementAst : System.Management.Automation.Language.StatementAst {
    public DataStatementAst(System.Management.Automation.Language.IScriptExtent extent, string variableName, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.ExpressionAst> commandsAllowed, System.Management.Automation.Language.StatementBlockAst body) : base(extent) { }

    public System.Management.Automation.Language.StatementBlockAst Body { get { return default(System.Management.Automation.Language.StatementBlockAst); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst> CommandsAllowed { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst>); } set { } }
    public string Variable { get { return default(string); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public abstract class DefaultCustomAstVisitor {
    protected DefaultCustomAstVisitor() { }

    public virtual object VisitArrayExpression ( System.Management.Automation.Language.ArrayExpressionAst arrayExpressionAst ) { return default(object); }
    public virtual object VisitArrayLiteral ( System.Management.Automation.Language.ArrayLiteralAst arrayLiteralAst ) { return default(object); }
    public virtual object VisitAssignmentStatement ( System.Management.Automation.Language.AssignmentStatementAst assignmentStatementAst ) { return default(object); }
    public virtual object VisitAttribute ( System.Management.Automation.Language.AttributeAst attributeAst ) { return default(object); }
    public virtual object VisitAttributedExpression ( System.Management.Automation.Language.AttributedExpressionAst attributedExpressionAst ) { return default(object); }
    public virtual object VisitBinaryExpression ( System.Management.Automation.Language.BinaryExpressionAst binaryExpressionAst ) { return default(object); }
    public virtual object VisitBlockStatement ( System.Management.Automation.Language.BlockStatementAst blockStatementAst ) { return default(object); }
    public virtual object VisitBreakStatement ( System.Management.Automation.Language.BreakStatementAst breakStatementAst ) { return default(object); }
    public virtual object VisitCatchClause ( System.Management.Automation.Language.CatchClauseAst catchClauseAst ) { return default(object); }
    public virtual object VisitCommand ( System.Management.Automation.Language.CommandAst commandAst ) { return default(object); }
    public virtual object VisitCommandExpression ( System.Management.Automation.Language.CommandExpressionAst commandExpressionAst ) { return default(object); }
    public virtual object VisitCommandParameter ( System.Management.Automation.Language.CommandParameterAst commandParameterAst ) { return default(object); }
    public virtual object VisitConstantExpression ( System.Management.Automation.Language.ConstantExpressionAst constantExpressionAst ) { return default(object); }
    public virtual object VisitContinueStatement ( System.Management.Automation.Language.ContinueStatementAst continueStatementAst ) { return default(object); }
    public virtual object VisitConvertExpression ( System.Management.Automation.Language.ConvertExpressionAst convertExpressionAst ) { return default(object); }
    public virtual object VisitDataStatement ( System.Management.Automation.Language.DataStatementAst dataStatementAst ) { return default(object); }
    public virtual object VisitDoUntilStatement ( System.Management.Automation.Language.DoUntilStatementAst doUntilStatementAst ) { return default(object); }
    public virtual object VisitDoWhileStatement ( System.Management.Automation.Language.DoWhileStatementAst doWhileStatementAst ) { return default(object); }
    public virtual object VisitErrorExpression ( System.Management.Automation.Language.ErrorExpressionAst errorExpressionAst ) { return default(object); }
    public virtual object VisitErrorStatement ( System.Management.Automation.Language.ErrorStatementAst errorStatementAst ) { return default(object); }
    public virtual object VisitExitStatement ( System.Management.Automation.Language.ExitStatementAst exitStatementAst ) { return default(object); }
    public virtual object VisitExpandableStringExpression ( System.Management.Automation.Language.ExpandableStringExpressionAst expandableStringExpressionAst ) { return default(object); }
    public virtual object VisitFileRedirection ( System.Management.Automation.Language.FileRedirectionAst fileRedirectionAst ) { return default(object); }
    public virtual object VisitForEachStatement ( System.Management.Automation.Language.ForEachStatementAst forEachStatementAst ) { return default(object); }
    public virtual object VisitForStatement ( System.Management.Automation.Language.ForStatementAst forStatementAst ) { return default(object); }
    public virtual object VisitFunctionDefinition ( System.Management.Automation.Language.FunctionDefinitionAst functionDefinitionAst ) { return default(object); }
    public virtual object VisitHashtable ( System.Management.Automation.Language.HashtableAst hashtableAst ) { return default(object); }
    public virtual object VisitIfStatement ( System.Management.Automation.Language.IfStatementAst ifStmtAst ) { return default(object); }
    public virtual object VisitIndexExpression ( System.Management.Automation.Language.IndexExpressionAst indexExpressionAst ) { return default(object); }
    public virtual object VisitInvokeMemberExpression ( System.Management.Automation.Language.InvokeMemberExpressionAst invokeMemberExpressionAst ) { return default(object); }
    public virtual object VisitMemberExpression ( System.Management.Automation.Language.MemberExpressionAst memberExpressionAst ) { return default(object); }
    public virtual object VisitMergingRedirection ( System.Management.Automation.Language.MergingRedirectionAst mergingRedirectionAst ) { return default(object); }
    public virtual object VisitNamedAttributeArgument ( System.Management.Automation.Language.NamedAttributeArgumentAst namedAttributeArgumentAst ) { return default(object); }
    public virtual object VisitNamedBlock ( System.Management.Automation.Language.NamedBlockAst namedBlockAst ) { return default(object); }
    public virtual object VisitParamBlock ( System.Management.Automation.Language.ParamBlockAst paramBlockAst ) { return default(object); }
    public virtual object VisitParameter ( System.Management.Automation.Language.ParameterAst parameterAst ) { return default(object); }
    public virtual object VisitParenExpression ( System.Management.Automation.Language.ParenExpressionAst parenExpressionAst ) { return default(object); }
    public virtual object VisitPipeline ( System.Management.Automation.Language.PipelineAst pipelineAst ) { return default(object); }
    public virtual object VisitReturnStatement ( System.Management.Automation.Language.ReturnStatementAst returnStatementAst ) { return default(object); }
    public virtual object VisitScriptBlock ( System.Management.Automation.Language.ScriptBlockAst scriptBlockAst ) { return default(object); }
    public virtual object VisitScriptBlockExpression ( System.Management.Automation.Language.ScriptBlockExpressionAst scriptBlockExpressionAst ) { return default(object); }
    public virtual object VisitStatementBlock ( System.Management.Automation.Language.StatementBlockAst statementBlockAst ) { return default(object); }
    public virtual object VisitStringConstantExpression ( System.Management.Automation.Language.StringConstantExpressionAst stringConstantExpressionAst ) { return default(object); }
    public virtual object VisitSubExpression ( System.Management.Automation.Language.SubExpressionAst subExpressionAst ) { return default(object); }
    public virtual object VisitSwitchStatement ( System.Management.Automation.Language.SwitchStatementAst switchStatementAst ) { return default(object); }
    public virtual object VisitThrowStatement ( System.Management.Automation.Language.ThrowStatementAst throwStatementAst ) { return default(object); }
    public virtual object VisitTrap ( System.Management.Automation.Language.TrapStatementAst trapStatementAst ) { return default(object); }
    public virtual object VisitTryStatement ( System.Management.Automation.Language.TryStatementAst tryStatementAst ) { return default(object); }
    public virtual object VisitTypeConstraint ( System.Management.Automation.Language.TypeConstraintAst typeConstraintAst ) { return default(object); }
    public virtual object VisitTypeExpression ( System.Management.Automation.Language.TypeExpressionAst typeExpressionAst ) { return default(object); }
    public virtual object VisitUnaryExpression ( System.Management.Automation.Language.UnaryExpressionAst unaryExpressionAst ) { return default(object); }
    public virtual object VisitUsingExpression ( System.Management.Automation.Language.UsingExpressionAst usingExpressionAst ) { return default(object); }
    public virtual object VisitVariableExpression ( System.Management.Automation.Language.VariableExpressionAst variableExpressionAst ) { return default(object); }
    public virtual object VisitWhileStatement ( System.Management.Automation.Language.WhileStatementAst whileStatementAst ) { return default(object); }

  }

  public abstract class DefaultCustomAstVisitor2 : System.Management.Automation.Language.DefaultCustomAstVisitor, System.Management.Automation.Language.ICustomAstVisitor2 {
    protected DefaultCustomAstVisitor2() { }

    public virtual object VisitBaseCtorInvokeMemberExpression ( System.Management.Automation.Language.BaseCtorInvokeMemberExpressionAst baseCtorInvokeMemberExpressionAst ) { return default(object); }
    public virtual object VisitConfigurationDefinition ( System.Management.Automation.Language.ConfigurationDefinitionAst configurationAst ) { return default(object); }
    public virtual object VisitDynamicKeywordStatement ( System.Management.Automation.Language.DynamicKeywordStatementAst dynamicKeywordAst ) { return default(object); }
    public virtual object VisitFunctionMember ( System.Management.Automation.Language.FunctionMemberAst functionMemberAst ) { return default(object); }
    public virtual object VisitPropertyMember ( System.Management.Automation.Language.PropertyMemberAst propertyMemberAst ) { return default(object); }
    public virtual object VisitTypeDefinition ( System.Management.Automation.Language.TypeDefinitionAst typeDefinitionAst ) { return default(object); }
    public virtual object VisitUsingStatement ( System.Management.Automation.Language.UsingStatementAst usingStatement ) { return default(object); }

  }

  public class DoUntilStatementAst : System.Management.Automation.Language.LoopStatementAst {
    public DoUntilStatementAst(System.Management.Automation.Language.IScriptExtent extent, string label, System.Management.Automation.Language.PipelineBaseAst condition, System.Management.Automation.Language.StatementBlockAst body) : base(extent,label,condition,body) { }

    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class DoWhileStatementAst : System.Management.Automation.Language.LoopStatementAst {
    public DoWhileStatementAst(System.Management.Automation.Language.IScriptExtent extent, string label, System.Management.Automation.Language.PipelineBaseAst condition, System.Management.Automation.Language.StatementBlockAst body) : base(extent,label,condition,body) { }

    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class DynamicKeyword {
    public DynamicKeyword() { }

    public System.Management.Automation.Language.DynamicKeywordBodyMode BodyMode { get { return default(System.Management.Automation.Language.DynamicKeywordBodyMode); } set { } }
    public bool DirectCall { get { return default(bool); } set { } }
    public bool HasReservedProperties { get { return default(bool); } set { } }
    public string ImplementingModule { get { return default(string); } set { } }
    public System.Version ImplementingModuleVersion { get { return default(System.Version); } set { } }
    public bool IsReservedKeyword { get { return default(bool); } set { } }
    public string Keyword { get { return default(string); } set { } }
    public bool MetaStatement { get { return default(bool); } set { } }
    public System.Management.Automation.Language.DynamicKeywordNameMode NameMode { get { return default(System.Management.Automation.Language.DynamicKeywordNameMode); } set { } }
    public System.Collections.Generic.Dictionary<string, System.Management.Automation.Language.DynamicKeywordParameter> Parameters { get { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.Language.DynamicKeywordParameter>); } }
    public System.Func<System.Management.Automation.Language.DynamicKeywordStatementAst, System.Management.Automation.Language.ParseError[]> PostParse { get { return default(System.Func<System.Management.Automation.Language.DynamicKeywordStatementAst, System.Management.Automation.Language.ParseError[]>); } set { } }
    public System.Func<System.Management.Automation.Language.DynamicKeyword, System.Management.Automation.Language.ParseError[]> PreParse { get { return default(System.Func<System.Management.Automation.Language.DynamicKeyword, System.Management.Automation.Language.ParseError[]>); } set { } }
    public System.Collections.Generic.Dictionary<string, System.Management.Automation.Language.DynamicKeywordProperty> Properties { get { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.Language.DynamicKeywordProperty>); } }
    public string ResourceName { get { return default(string); } set { } }
    public System.Func<System.Management.Automation.Language.DynamicKeywordStatementAst, System.Management.Automation.Language.ParseError[]> SemanticCheck { get { return default(System.Func<System.Management.Automation.Language.DynamicKeywordStatementAst, System.Management.Automation.Language.ParseError[]>); } set { } }
    public static void AddKeyword ( System.Management.Automation.Language.DynamicKeyword keywordToAdd ) { }
    public static bool ContainsKeyword ( string name ) { return default(bool); }
    public System.Management.Automation.Language.DynamicKeyword Copy (  ) { return default(System.Management.Automation.Language.DynamicKeyword); }
    public static System.Management.Automation.Language.DynamicKeyword GetKeyword ( string name ) { return default(System.Management.Automation.Language.DynamicKeyword); }
    public static System.Collections.Generic.List<System.Management.Automation.Language.DynamicKeyword> GetKeyword (  ) { return default(System.Collections.Generic.List<System.Management.Automation.Language.DynamicKeyword>); }
    public static void Pop (  ) { }
    public static void Push (  ) { }
    public static void RemoveKeyword ( string name ) { }
    public static void Reset (  ) { }

  }

  public enum DynamicKeywordBodyMode {
    Command = 0,
    Hashtable = 2,
    ScriptBlock = 1,
  }

  public enum DynamicKeywordNameMode {
    NameRequired = 2,
    NoName = 0,
    OptionalName = 4,
    SimpleNameRequired = 1,
    SimpleOptionalName = 3,
  }

  public class DynamicKeywordParameter : System.Management.Automation.Language.DynamicKeywordProperty {
    public DynamicKeywordParameter() { }

    public bool Switch { get { return default(bool); } set { } }
  }

  public class DynamicKeywordProperty {
    public DynamicKeywordProperty() { }

    public System.Collections.Generic.List<string> Attributes { get { return default(System.Collections.Generic.List<string>); } }
    public bool IsKey { get { return default(bool); } set { } }
    public bool Mandatory { get { return default(bool); } set { } }
    public string Name { get { return default(string); } set { } }
    public System.Tuple<int, int> Range { get { return default(System.Tuple<int, int>); } set { } }
    public string TypeConstraint { get { return default(string); } set { } }
    public System.Collections.Generic.Dictionary<string, string> ValueMap { get { return default(System.Collections.Generic.Dictionary<string, string>); } }
    public System.Collections.Generic.List<string> Values { get { return default(System.Collections.Generic.List<string>); } }
  }

  public class DynamicKeywordStatementAst : System.Management.Automation.Language.StatementAst {
    public DynamicKeywordStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.CommandElementAst> commandElements) : base(extent) { }

    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.CommandElementAst> CommandElements { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.CommandElementAst>); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class ErrorExpressionAst : System.Management.Automation.Language.ExpressionAst {
    public ErrorExpressionAst(System.Management.Automation.Language.IScriptExtent extent) : base (extent) { }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Ast> NestedAst { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Ast>); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class ErrorStatementAst : System.Management.Automation.Language.PipelineBaseAst {
    public ErrorStatementAst(System.Management.Automation.Language.IScriptExtent extent) : base (extent) { }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Ast> Bodies { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Ast>); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Ast> Conditions { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Ast>); } set { } }
    public System.Collections.Generic.Dictionary<string, System.Tuple<System.Management.Automation.Language.Token, System.Management.Automation.Language.Ast>> Flags { get { return default(System.Collections.Generic.Dictionary<string, System.Tuple<System.Management.Automation.Language.Token, System.Management.Automation.Language.Ast>>); } set { } }
    public System.Management.Automation.Language.Token Kind { get { return default(System.Management.Automation.Language.Token); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Ast> NestedAst { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Ast>); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class ExitStatementAst : System.Management.Automation.Language.StatementAst {
    public ExitStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.PipelineBaseAst pipeline) : base(extent) { }

    public System.Management.Automation.Language.PipelineBaseAst Pipeline { get { return default(System.Management.Automation.Language.PipelineBaseAst); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class ExpandableStringExpressionAst : System.Management.Automation.Language.ExpressionAst {
    public ExpandableStringExpressionAst(System.Management.Automation.Language.IScriptExtent extent, string value, System.Management.Automation.Language.StringConstantType type) : base(extent) { }

    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst> NestedExpressions { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst>); } set { } }
    public override System.Type StaticType { get { return default(System.Type); } }
    public System.Management.Automation.Language.StringConstantType StringConstantType { get { return default(System.Management.Automation.Language.StringConstantType); } set { } }
    public string Value { get { return default(string); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public abstract class ExpressionAst : System.Management.Automation.Language.CommandElementAst {
    protected ExpressionAst(System.Management.Automation.Language.IScriptExtent extent) : base(extent) { }

    public virtual System.Type StaticType { get { return default(System.Type); } }
  }

  public class FileRedirectionAst : System.Management.Automation.Language.RedirectionAst {
    public FileRedirectionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.RedirectionStream stream, System.Management.Automation.Language.ExpressionAst file, bool append) : base(extent, default(System.Management.Automation.Language.RedirectionStream)) { }

    public bool Append { get { return default(bool); } set { } }
    public System.Management.Automation.Language.ExpressionAst Location { get { return default(System.Management.Automation.Language.ExpressionAst); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class FileRedirectionToken : System.Management.Automation.Language.RedirectionToken {
    internal FileRedirectionToken() { }
    public bool Append { get { return default(bool); } set { } }
    public System.Management.Automation.Language.RedirectionStream FromStream { get { return default(System.Management.Automation.Language.RedirectionStream); } set { } }
  }

    [System.FlagsAttribute]
   public enum ForEachFlags {
    None = 0,
    Parallel = 1,
  }

  public class ForEachStatementAst : System.Management.Automation.Language.LoopStatementAst {
    public ForEachStatementAst(System.Management.Automation.Language.IScriptExtent extent, string label, System.Management.Automation.Language.ForEachFlags flags, System.Management.Automation.Language.VariableExpressionAst variable, System.Management.Automation.Language.PipelineBaseAst expression, System.Management.Automation.Language.StatementBlockAst body) : base(extent, label, default(System.Management.Automation.Language.PipelineBaseAst), default(System.Management.Automation.Language.StatementBlockAst)) { }
    public ForEachStatementAst(System.Management.Automation.Language.IScriptExtent extent, string label, System.Management.Automation.Language.ForEachFlags flags, System.Management.Automation.Language.ExpressionAst throttleLimit, System.Management.Automation.Language.VariableExpressionAst variable, System.Management.Automation.Language.PipelineBaseAst expression, System.Management.Automation.Language.StatementBlockAst body) : base(extent, label, default(System.Management.Automation.Language.PipelineBaseAst), default(System.Management.Automation.Language.StatementBlockAst)) { }

    public System.Management.Automation.Language.ForEachFlags Flags { get { return default(System.Management.Automation.Language.ForEachFlags); } set { } }
    public System.Management.Automation.Language.ExpressionAst ThrottleLimit { get { return default(System.Management.Automation.Language.ExpressionAst); } set { } }
    public System.Management.Automation.Language.VariableExpressionAst Variable { get { return default(System.Management.Automation.Language.VariableExpressionAst); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class ForStatementAst : System.Management.Automation.Language.LoopStatementAst {
    public ForStatementAst(System.Management.Automation.Language.IScriptExtent extent, string label, System.Management.Automation.Language.PipelineBaseAst initializer, System.Management.Automation.Language.PipelineBaseAst condition, System.Management.Automation.Language.PipelineBaseAst iterator, System.Management.Automation.Language.StatementBlockAst body) : base(extent,label, condition, body) { }

    public System.Management.Automation.Language.PipelineBaseAst Initializer { get { return default(System.Management.Automation.Language.PipelineBaseAst); } set { } }
    public System.Management.Automation.Language.PipelineBaseAst Iterator { get { return default(System.Management.Automation.Language.PipelineBaseAst); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class FunctionDefinitionAst : System.Management.Automation.Language.StatementAst {
    public FunctionDefinitionAst(System.Management.Automation.Language.IScriptExtent extent, bool isFilter, bool isWorkflow, string name, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.ParameterAst> parameters, System.Management.Automation.Language.ScriptBlockAst body) : base(extent) { }

    public System.Management.Automation.Language.ScriptBlockAst Body { get { return default(System.Management.Automation.Language.ScriptBlockAst); } set { } }
    public bool IsFilter { get { return default(bool); } set { } }
    public bool IsWorkflow { get { return default(bool); } set { } }
    public string Name { get { return default(string); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ParameterAst> Parameters { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ParameterAst>); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }
    public System.Management.Automation.Language.CommentHelpInfo GetHelpContent ( System.Collections.Generic.Dictionary<System.Management.Automation.Language.Ast, System.Management.Automation.Language.Token[]> scriptBlockTokenCache ) { return default(System.Management.Automation.Language.CommentHelpInfo); }
    public System.Management.Automation.Language.CommentHelpInfo GetHelpContent (  ) { return default(System.Management.Automation.Language.CommentHelpInfo); }

  }

  public class FunctionMemberAst : System.Management.Automation.Language.MemberAst {
    public FunctionMemberAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.FunctionDefinitionAst functionDefinitionAst, System.Management.Automation.Language.TypeConstraintAst returnType, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.AttributeAst> attributes, System.Management.Automation.Language.MethodAttributes methodAttributes) : base(extent) { }

    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.AttributeAst> Attributes { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.AttributeAst>); } set { } }
    public System.Management.Automation.Language.ScriptBlockAst Body { get { return default(System.Management.Automation.Language.ScriptBlockAst); } }
    public bool IsConstructor { get { return default(bool); } }
    public bool IsHidden { get { return default(bool); } }
    public bool IsPrivate { get { return default(bool); } }
    public bool IsPublic { get { return default(bool); } }
    public bool IsStatic { get { return default(bool); } }
    public System.Management.Automation.Language.MethodAttributes MethodAttributes { get { return default(System.Management.Automation.Language.MethodAttributes); } set { } }
    public override string Name { get { return default(string); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ParameterAst> Parameters { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ParameterAst>); } }
    public System.Management.Automation.Language.TypeConstraintAst ReturnType { get { return default(System.Management.Automation.Language.TypeConstraintAst); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public sealed class GenericTypeName : System.Management.Automation.Language.ITypeName {
    public GenericTypeName(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ITypeName genericTypeName, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.ITypeName> genericArguments) { }

    public string AssemblyName { get { return default(string); } }
    public System.Management.Automation.Language.IScriptExtent Extent { get { return default(System.Management.Automation.Language.IScriptExtent); } }
    public string FullName { get { return default(string); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ITypeName> GenericArguments { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ITypeName>); } set { } }
    public bool IsArray { get { return default(bool); } }
    public bool IsGeneric { get { return default(bool); } }
    public string Name { get { return default(string); } }
    public System.Management.Automation.Language.ITypeName TypeName { get { return default(System.Management.Automation.Language.ITypeName); } set { } }
    public override bool Equals ( object obj ) { return default(bool); }
    public override int GetHashCode (  ) { return default(int); }
    public System.Type GetReflectionAttributeType (  ) { return default(System.Type); }
    public System.Type GetReflectionType (  ) { return default(System.Type); }
    public override string ToString (  ) { return default(string); }

  }

  public class HashtableAst : System.Management.Automation.Language.ExpressionAst {
    public HashtableAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Tuple<System.Management.Automation.Language.ExpressionAst, System.Management.Automation.Language.StatementAst>> keyValuePairs) : base(extent) { }

    public System.Collections.ObjectModel.ReadOnlyCollection<System.Tuple<System.Management.Automation.Language.ExpressionAst, System.Management.Automation.Language.StatementAst>> KeyValuePairs { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Tuple<System.Management.Automation.Language.ExpressionAst, System.Management.Automation.Language.StatementAst>>); } set { } }
    public override System.Type StaticType { get { return default(System.Type); } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public partial interface IAstPostVisitHandler {
     void PostVisit ( System.Management.Automation.Language.Ast ast );

  }

  public partial interface ICustomAstVisitor {
     object VisitArrayExpression ( System.Management.Automation.Language.ArrayExpressionAst arrayExpressionAst );
     object VisitArrayLiteral ( System.Management.Automation.Language.ArrayLiteralAst arrayLiteralAst );
     object VisitAssignmentStatement ( System.Management.Automation.Language.AssignmentStatementAst assignmentStatementAst );
     object VisitAttribute ( System.Management.Automation.Language.AttributeAst attributeAst );
     object VisitAttributedExpression ( System.Management.Automation.Language.AttributedExpressionAst attributedExpressionAst );
     object VisitBinaryExpression ( System.Management.Automation.Language.BinaryExpressionAst binaryExpressionAst );
     object VisitBlockStatement ( System.Management.Automation.Language.BlockStatementAst blockStatementAst );
     object VisitBreakStatement ( System.Management.Automation.Language.BreakStatementAst breakStatementAst );
     object VisitCatchClause ( System.Management.Automation.Language.CatchClauseAst catchClauseAst );
     object VisitCommand ( System.Management.Automation.Language.CommandAst commandAst );
     object VisitCommandExpression ( System.Management.Automation.Language.CommandExpressionAst commandExpressionAst );
     object VisitCommandParameter ( System.Management.Automation.Language.CommandParameterAst commandParameterAst );
     object VisitConstantExpression ( System.Management.Automation.Language.ConstantExpressionAst constantExpressionAst );
     object VisitContinueStatement ( System.Management.Automation.Language.ContinueStatementAst continueStatementAst );
     object VisitConvertExpression ( System.Management.Automation.Language.ConvertExpressionAst convertExpressionAst );
     object VisitDataStatement ( System.Management.Automation.Language.DataStatementAst dataStatementAst );
     object VisitDoUntilStatement ( System.Management.Automation.Language.DoUntilStatementAst doUntilStatementAst );
     object VisitDoWhileStatement ( System.Management.Automation.Language.DoWhileStatementAst doWhileStatementAst );
     object VisitErrorExpression ( System.Management.Automation.Language.ErrorExpressionAst errorExpressionAst );
     object VisitErrorStatement ( System.Management.Automation.Language.ErrorStatementAst errorStatementAst );
     object VisitExitStatement ( System.Management.Automation.Language.ExitStatementAst exitStatementAst );
     object VisitExpandableStringExpression ( System.Management.Automation.Language.ExpandableStringExpressionAst expandableStringExpressionAst );
     object VisitFileRedirection ( System.Management.Automation.Language.FileRedirectionAst fileRedirectionAst );
     object VisitForEachStatement ( System.Management.Automation.Language.ForEachStatementAst forEachStatementAst );
     object VisitForStatement ( System.Management.Automation.Language.ForStatementAst forStatementAst );
     object VisitFunctionDefinition ( System.Management.Automation.Language.FunctionDefinitionAst functionDefinitionAst );
     object VisitHashtable ( System.Management.Automation.Language.HashtableAst hashtableAst );
     object VisitIfStatement ( System.Management.Automation.Language.IfStatementAst ifStmtAst );
     object VisitIndexExpression ( System.Management.Automation.Language.IndexExpressionAst indexExpressionAst );
     object VisitInvokeMemberExpression ( System.Management.Automation.Language.InvokeMemberExpressionAst invokeMemberExpressionAst );
     object VisitMemberExpression ( System.Management.Automation.Language.MemberExpressionAst memberExpressionAst );
     object VisitMergingRedirection ( System.Management.Automation.Language.MergingRedirectionAst mergingRedirectionAst );
     object VisitNamedAttributeArgument ( System.Management.Automation.Language.NamedAttributeArgumentAst namedAttributeArgumentAst );
     object VisitNamedBlock ( System.Management.Automation.Language.NamedBlockAst namedBlockAst );
     object VisitParamBlock ( System.Management.Automation.Language.ParamBlockAst paramBlockAst );
     object VisitParameter ( System.Management.Automation.Language.ParameterAst parameterAst );
     object VisitParenExpression ( System.Management.Automation.Language.ParenExpressionAst parenExpressionAst );
     object VisitPipeline ( System.Management.Automation.Language.PipelineAst pipelineAst );
     object VisitReturnStatement ( System.Management.Automation.Language.ReturnStatementAst returnStatementAst );
     object VisitScriptBlock ( System.Management.Automation.Language.ScriptBlockAst scriptBlockAst );
     object VisitScriptBlockExpression ( System.Management.Automation.Language.ScriptBlockExpressionAst scriptBlockExpressionAst );
     object VisitStatementBlock ( System.Management.Automation.Language.StatementBlockAst statementBlockAst );
     object VisitStringConstantExpression ( System.Management.Automation.Language.StringConstantExpressionAst stringConstantExpressionAst );
     object VisitSubExpression ( System.Management.Automation.Language.SubExpressionAst subExpressionAst );
     object VisitSwitchStatement ( System.Management.Automation.Language.SwitchStatementAst switchStatementAst );
     object VisitThrowStatement ( System.Management.Automation.Language.ThrowStatementAst throwStatementAst );
     object VisitTrap ( System.Management.Automation.Language.TrapStatementAst trapStatementAst );
     object VisitTryStatement ( System.Management.Automation.Language.TryStatementAst tryStatementAst );
     object VisitTypeConstraint ( System.Management.Automation.Language.TypeConstraintAst typeConstraintAst );
     object VisitTypeExpression ( System.Management.Automation.Language.TypeExpressionAst typeExpressionAst );
     object VisitUnaryExpression ( System.Management.Automation.Language.UnaryExpressionAst unaryExpressionAst );
     object VisitUsingExpression ( System.Management.Automation.Language.UsingExpressionAst usingExpressionAst );
     object VisitVariableExpression ( System.Management.Automation.Language.VariableExpressionAst variableExpressionAst );
     object VisitWhileStatement ( System.Management.Automation.Language.WhileStatementAst whileStatementAst );

  }

  public partial interface ICustomAstVisitor2 {
     object VisitBaseCtorInvokeMemberExpression ( System.Management.Automation.Language.BaseCtorInvokeMemberExpressionAst baseCtorInvokeMemberExpressionAst );
     object VisitConfigurationDefinition ( System.Management.Automation.Language.ConfigurationDefinitionAst configurationDefinitionAst );
     object VisitDynamicKeywordStatement ( System.Management.Automation.Language.DynamicKeywordStatementAst dynamicKeywordAst );
     object VisitFunctionMember ( System.Management.Automation.Language.FunctionMemberAst functionMemberAst );
     object VisitPropertyMember ( System.Management.Automation.Language.PropertyMemberAst propertyMemberAst );
     object VisitTypeDefinition ( System.Management.Automation.Language.TypeDefinitionAst typeDefinitionAst );
     object VisitUsingStatement ( System.Management.Automation.Language.UsingStatementAst usingStatement );

  }

  public class IfStatementAst : System.Management.Automation.Language.StatementAst {
    public IfStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Tuple<System.Management.Automation.Language.PipelineBaseAst, System.Management.Automation.Language.StatementBlockAst>> clauses, System.Management.Automation.Language.StatementBlockAst elseClause) : base(extent) { }

    public System.Collections.ObjectModel.ReadOnlyCollection<System.Tuple<System.Management.Automation.Language.PipelineBaseAst, System.Management.Automation.Language.StatementBlockAst>> Clauses { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Tuple<System.Management.Automation.Language.PipelineBaseAst, System.Management.Automation.Language.StatementBlockAst>>); } set { } }
    public System.Management.Automation.Language.StatementBlockAst ElseClause { get { return default(System.Management.Automation.Language.StatementBlockAst); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class IndexExpressionAst : System.Management.Automation.Language.ExpressionAst {
    public IndexExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ExpressionAst target, System.Management.Automation.Language.ExpressionAst index) : base(extent) { }

    public System.Management.Automation.Language.ExpressionAst Index { get { return default(System.Management.Automation.Language.ExpressionAst); } set { } }
    public System.Management.Automation.Language.ExpressionAst Target { get { return default(System.Management.Automation.Language.ExpressionAst); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class InputRedirectionToken : System.Management.Automation.Language.RedirectionToken {
    internal InputRedirectionToken() { }
  }

  public class InvokeMemberExpressionAst : System.Management.Automation.Language.MemberExpressionAst {
    public InvokeMemberExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ExpressionAst expression, System.Management.Automation.Language.CommandElementAst method, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.ExpressionAst> arguments, bool @static) : base(extent, expression, default(System.Management.Automation.Language.CommandElementAst), default(bool)) { }

    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst> Arguments { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ExpressionAst>); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

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

     string GetFullScript (  );

  }

  public partial interface ITypeName {
    string AssemblyName { get; }

    System.Management.Automation.Language.IScriptExtent Extent { get; }

    string FullName { get; }

    bool IsArray { get; }

    bool IsGeneric { get; }

    string Name { get; }

     System.Type GetReflectionAttributeType (  );
     System.Type GetReflectionType (  );

  }

  public abstract class LabeledStatementAst : System.Management.Automation.Language.StatementAst {
    protected LabeledStatementAst(System.Management.Automation.Language.IScriptExtent extent, string label, System.Management.Automation.Language.PipelineBaseAst condition) : base(extent) { }

    public System.Management.Automation.Language.PipelineBaseAst Condition { get { return default(System.Management.Automation.Language.PipelineBaseAst); } set { } }
    public string Label { get { return default(string); } set { } }
  }

  public class LabelToken : System.Management.Automation.Language.Token {
    internal LabelToken() { }
    public string LabelText { get { return default(string); } }
  }

  public abstract class LoopStatementAst : System.Management.Automation.Language.LabeledStatementAst {
    protected LoopStatementAst(System.Management.Automation.Language.IScriptExtent extent, string label, System.Management.Automation.Language.PipelineBaseAst condition, System.Management.Automation.Language.StatementBlockAst body) : base(extent, label, condition) { }

    public System.Management.Automation.Language.StatementBlockAst Body { get { return default(System.Management.Automation.Language.StatementBlockAst); } set { } }
  }

  public abstract class MemberAst : System.Management.Automation.Language.Ast {
    protected MemberAst(System.Management.Automation.Language.IScriptExtent extent) : base(extent) { }

    public abstract string Name { get; }
  }

  public class MemberExpressionAst : System.Management.Automation.Language.ExpressionAst {
    public MemberExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ExpressionAst expression, System.Management.Automation.Language.CommandElementAst member, bool @static) : base(extent) { }

    public System.Management.Automation.Language.ExpressionAst Expression { get { return default(System.Management.Automation.Language.ExpressionAst); } set { } }
    public System.Management.Automation.Language.CommandElementAst Member { get { return default(System.Management.Automation.Language.CommandElementAst); } set { } }
    public bool Static { get { return default(bool); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class MergingRedirectionAst : System.Management.Automation.Language.RedirectionAst {
    public MergingRedirectionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.RedirectionStream from, System.Management.Automation.Language.RedirectionStream to) : base(extent, from) { }

    public System.Management.Automation.Language.RedirectionStream ToStream { get { return default(System.Management.Automation.Language.RedirectionStream); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class MergingRedirectionToken : System.Management.Automation.Language.RedirectionToken {
    internal MergingRedirectionToken() { }
    public System.Management.Automation.Language.RedirectionStream FromStream { get { return default(System.Management.Automation.Language.RedirectionStream); } set { } }
    public System.Management.Automation.Language.RedirectionStream ToStream { get { return default(System.Management.Automation.Language.RedirectionStream); } set { } }
  }

    [System.FlagsAttribute]
   public enum MethodAttributes {
    Hidden = 64,
    None = 0,
    Private = 2,
    Public = 1,
    Static = 16,
  }

  public class NamedAttributeArgumentAst : System.Management.Automation.Language.Ast {
    public NamedAttributeArgumentAst(System.Management.Automation.Language.IScriptExtent extent, string argumentName, System.Management.Automation.Language.ExpressionAst argument, bool expressionOmitted) : base(extent) { }

    public System.Management.Automation.Language.ExpressionAst Argument { get { return default(System.Management.Automation.Language.ExpressionAst); } set { } }
    public string ArgumentName { get { return default(string); } set { } }
    public bool ExpressionOmitted { get { return default(bool); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class NamedBlockAst : System.Management.Automation.Language.Ast {
    public NamedBlockAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.TokenKind blockName, System.Management.Automation.Language.StatementBlockAst statementBlock, bool unnamed) : base(extent) { }

    public System.Management.Automation.Language.TokenKind BlockKind { get { return default(System.Management.Automation.Language.TokenKind); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.StatementAst> Statements { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.StatementAst>); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.TrapStatementAst> Traps { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.TrapStatementAst>); } set { } }
    public bool Unnamed { get { return default(bool); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class NullString {
    internal NullString() { }
    public System.Management.Automation.Language.NullString Value { get { return default(System.Management.Automation.Language.NullString); } }
    public override string ToString (  ) { return default(string); }

  }

  public class NumberToken : System.Management.Automation.Language.Token {
    internal NumberToken() { }
    public object Value { get { return default(object); } }
  }

  public class ParamBlockAst : System.Management.Automation.Language.Ast {
    public ParamBlockAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.AttributeAst> attributes, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.ParameterAst> parameters) : base(extent) { }

    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.AttributeAst> Attributes { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.AttributeAst>); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ParameterAst> Parameters { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.ParameterAst>); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class ParameterAst : System.Management.Automation.Language.Ast {
    public ParameterAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.VariableExpressionAst name, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.AttributeBaseAst> attributes, System.Management.Automation.Language.ExpressionAst defaultValue) : base(extent) { }

    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.AttributeBaseAst> Attributes { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.AttributeBaseAst>); } set { } }
    public System.Management.Automation.Language.ExpressionAst DefaultValue { get { return default(System.Management.Automation.Language.ExpressionAst); } set { } }
    public System.Management.Automation.Language.VariableExpressionAst Name { get { return default(System.Management.Automation.Language.VariableExpressionAst); } set { } }
    public System.Type StaticType { get { return default(System.Type); } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class ParameterBindingResult {
    public object ConstantValue { get { return default(object); } set { } }
    public System.Management.Automation.ParameterMetadata Parameter { get { return default(System.Management.Automation.ParameterMetadata); } set { } }
    public System.Management.Automation.Language.CommandElementAst Value { get { return default(System.Management.Automation.Language.CommandElementAst); } set { } }
  }

  public class ParameterToken : System.Management.Automation.Language.Token {
    internal ParameterToken() { }
    public string ParameterName { get { return default(string); } }
    public bool UsedColon { get { return default(bool); } }
  }

  public class ParenExpressionAst : System.Management.Automation.Language.ExpressionAst {
    public ParenExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.PipelineBaseAst pipeline) : base(extent) { }

    public System.Management.Automation.Language.PipelineBaseAst Pipeline { get { return default(System.Management.Automation.Language.PipelineBaseAst); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class ParseError {
    public ParseError(System.Management.Automation.Language.IScriptExtent extent, string errorId, string message) { }

    public string ErrorId { get { return default(string); } }
    public System.Management.Automation.Language.IScriptExtent Extent { get { return default(System.Management.Automation.Language.IScriptExtent); } }
    public bool IncompleteInput { get { return default(bool); } }
    public string Message { get { return default(string); } }
    public override string ToString (  ) { return default(string); }

  }

  public sealed class Parser {
    internal Parser() { }
    public static System.Management.Automation.Language.ScriptBlockAst ParseFile ( string fileName, out System.Management.Automation.Language.Token[] tokens, out System.Management.Automation.Language.ParseError[] errors ) { tokens = default(System.Management.Automation.Language.Token[]); errors = default(System.Management.Automation.Language.ParseError[]); return default(System.Management.Automation.Language.ScriptBlockAst); }
    public static System.Management.Automation.Language.ScriptBlockAst ParseInput ( string input, out System.Management.Automation.Language.Token[] tokens, out System.Management.Automation.Language.ParseError[] errors ) { tokens = default(System.Management.Automation.Language.Token[]); errors = default(System.Management.Automation.Language.ParseError[]); return default(System.Management.Automation.Language.ScriptBlockAst); }
    public static System.Management.Automation.Language.ScriptBlockAst ParseInput ( string input, string fileName, out System.Management.Automation.Language.Token[] tokens, out System.Management.Automation.Language.ParseError[] errors ) { tokens = default(System.Management.Automation.Language.Token[]); errors = default(System.Management.Automation.Language.ParseError[]); return default(System.Management.Automation.Language.ScriptBlockAst); }

  }

  public class PipelineAst : System.Management.Automation.Language.PipelineBaseAst {
    public PipelineAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.CommandBaseAst> pipelineElements, bool background) : base(extent) { }
    public PipelineAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.CommandBaseAst> pipelineElements) : base(extent) { }
    public PipelineAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.CommandBaseAst commandAst, bool background) : base(extent) { }
    public PipelineAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.CommandBaseAst commandAst) : base(extent) { }

    public bool Background { get { return default(bool); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.CommandBaseAst> PipelineElements { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.CommandBaseAst>); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }
    public override System.Management.Automation.Language.ExpressionAst GetPureExpression (  ) { return default(System.Management.Automation.Language.ExpressionAst); }

  }

  public abstract class PipelineBaseAst : System.Management.Automation.Language.StatementAst {
    protected PipelineBaseAst(System.Management.Automation.Language.IScriptExtent extent) : base(extent) { }

    public virtual System.Management.Automation.Language.ExpressionAst GetPureExpression (  ) { return default(System.Management.Automation.Language.ExpressionAst); }

  }

    [System.FlagsAttribute]
   public enum PropertyAttributes {
    Hidden = 64,
    Literal = 32,
    None = 0,
    Private = 2,
    Public = 1,
    Static = 16,
  }

  public class PropertyMemberAst : System.Management.Automation.Language.MemberAst {
    public PropertyMemberAst(System.Management.Automation.Language.IScriptExtent extent, string name, System.Management.Automation.Language.TypeConstraintAst propertyType, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.AttributeAst> attributes, System.Management.Automation.Language.PropertyAttributes propertyAttributes, System.Management.Automation.Language.ExpressionAst initialValue) : base(extent) { }

    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.AttributeAst> Attributes { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.AttributeAst>); } set { } }
    public System.Management.Automation.Language.ExpressionAst InitialValue { get { return default(System.Management.Automation.Language.ExpressionAst); } set { } }
    public bool IsHidden { get { return default(bool); } }
    public bool IsPrivate { get { return default(bool); } }
    public bool IsPublic { get { return default(bool); } }
    public bool IsStatic { get { return default(bool); } }
    public override string Name { get { return default(string); } }
    public System.Management.Automation.Language.PropertyAttributes PropertyAttributes { get { return default(System.Management.Automation.Language.PropertyAttributes); } set { } }
    public System.Management.Automation.Language.TypeConstraintAst PropertyType { get { return default(System.Management.Automation.Language.TypeConstraintAst); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public abstract class RedirectionAst : System.Management.Automation.Language.Ast {
    protected RedirectionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.RedirectionStream from) : base(extent) { }

    public System.Management.Automation.Language.RedirectionStream FromStream { get { return default(System.Management.Automation.Language.RedirectionStream); } set { } }
  }

  public enum RedirectionStream {
    All = 0,
    Debug = 5,
    Error = 2,
    Information = 6,
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
    public override bool Equals ( object obj ) { return default(bool); }
    public override int GetHashCode (  ) { return default(int); }
    public System.Type GetReflectionAttributeType (  ) { return default(System.Type); }
    public System.Type GetReflectionType (  ) { return default(System.Type); }
    public override string ToString (  ) { return default(string); }

  }

  public class ReturnStatementAst : System.Management.Automation.Language.StatementAst {
    public ReturnStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.PipelineBaseAst pipeline) : base(extent) { }

    public System.Management.Automation.Language.PipelineBaseAst Pipeline { get { return default(System.Management.Automation.Language.PipelineBaseAst); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class ScriptBlockAst : System.Management.Automation.Language.Ast {
    public ScriptBlockAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.UsingStatementAst> usingStatements, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.AttributeAst> attributes, System.Management.Automation.Language.ParamBlockAst paramBlock, System.Management.Automation.Language.NamedBlockAst beginBlock, System.Management.Automation.Language.NamedBlockAst processBlock, System.Management.Automation.Language.NamedBlockAst endBlock, System.Management.Automation.Language.NamedBlockAst dynamicParamBlock) : base(extent) { }
    public ScriptBlockAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.UsingStatementAst> usingStatements, System.Management.Automation.Language.ParamBlockAst paramBlock, System.Management.Automation.Language.NamedBlockAst beginBlock, System.Management.Automation.Language.NamedBlockAst processBlock, System.Management.Automation.Language.NamedBlockAst endBlock, System.Management.Automation.Language.NamedBlockAst dynamicParamBlock) : base(extent) { }
    public ScriptBlockAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ParamBlockAst paramBlock, System.Management.Automation.Language.NamedBlockAst beginBlock, System.Management.Automation.Language.NamedBlockAst processBlock, System.Management.Automation.Language.NamedBlockAst endBlock, System.Management.Automation.Language.NamedBlockAst dynamicParamBlock) : base(extent) { }
    public ScriptBlockAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.List<System.Management.Automation.Language.UsingStatementAst> usingStatements, System.Management.Automation.Language.ParamBlockAst paramBlock, System.Management.Automation.Language.StatementBlockAst statements, bool isFilter) : base(extent) { }
    public ScriptBlockAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ParamBlockAst paramBlock, System.Management.Automation.Language.StatementBlockAst statements, bool isFilter) : base(extent) { }
    public ScriptBlockAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ParamBlockAst paramBlock, System.Management.Automation.Language.StatementBlockAst statements, bool isFilter, bool isConfiguration) : base(extent) { }
    public ScriptBlockAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.UsingStatementAst> usingStatements, System.Management.Automation.Language.ParamBlockAst paramBlock, System.Management.Automation.Language.StatementBlockAst statements, bool isFilter, bool isConfiguration) : base(extent) { }
    public ScriptBlockAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.AttributeAst> attributes, System.Management.Automation.Language.ParamBlockAst paramBlock, System.Management.Automation.Language.StatementBlockAst statements, bool isFilter, bool isConfiguration) : base(extent) { }
    public ScriptBlockAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.UsingStatementAst> usingStatements, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.AttributeAst> attributes, System.Management.Automation.Language.ParamBlockAst paramBlock, System.Management.Automation.Language.StatementBlockAst statements, bool isFilter, bool isConfiguration) : base(extent) { }

    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.AttributeAst> Attributes { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.AttributeAst>); } set { } }
    public System.Management.Automation.Language.NamedBlockAst BeginBlock { get { return default(System.Management.Automation.Language.NamedBlockAst); } set { } }
    public System.Management.Automation.Language.NamedBlockAst DynamicParamBlock { get { return default(System.Management.Automation.Language.NamedBlockAst); } set { } }
    public System.Management.Automation.Language.NamedBlockAst EndBlock { get { return default(System.Management.Automation.Language.NamedBlockAst); } set { } }
    public System.Management.Automation.Language.ParamBlockAst ParamBlock { get { return default(System.Management.Automation.Language.ParamBlockAst); } set { } }
    public System.Management.Automation.Language.NamedBlockAst ProcessBlock { get { return default(System.Management.Automation.Language.NamedBlockAst); } set { } }
    public System.Management.Automation.Language.ScriptRequirements ScriptRequirements { get { return default(System.Management.Automation.Language.ScriptRequirements); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.UsingStatementAst> UsingStatements { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.UsingStatementAst>); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }
    public System.Management.Automation.Language.CommentHelpInfo GetHelpContent (  ) { return default(System.Management.Automation.Language.CommentHelpInfo); }
    public System.Management.Automation.ScriptBlock GetScriptBlock (  ) { return default(System.Management.Automation.ScriptBlock); }

  }

  public class ScriptBlockExpressionAst : System.Management.Automation.Language.ExpressionAst {
    public ScriptBlockExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ScriptBlockAst scriptBlock) : base(extent) { }

    public System.Management.Automation.Language.ScriptBlockAst ScriptBlock { get { return default(System.Management.Automation.Language.ScriptBlockAst); } set { } }
    public override System.Type StaticType { get { return default(System.Type); } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public sealed class ScriptExtent {
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

  public sealed class ScriptPosition {
    public ScriptPosition(string scriptName, int scriptLineNumber, int offsetInLine, string line) { }
    public ScriptPosition(string scriptName, int scriptLineNumber, int offsetInLine, string line, string fullScript) { }

    public int ColumnNumber { get { return default(int); } }
    public string File { get { return default(string); } }
    public string Line { get { return default(string); } }
    public int LineNumber { get { return default(int); } }
    public int Offset { get { return default(int); } }
    public string GetFullScript (  ) { return default(string); }

  }

  public class ScriptRequirements {
    public ScriptRequirements() { }

    public bool IsElevationRequired { get { return default(bool); } set { } }
    public string RequiredApplicationId { get { return default(string); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<string> RequiredAssemblies { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<string>); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<Microsoft.PowerShell.Commands.ModuleSpecification> RequiredModules { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<Microsoft.PowerShell.Commands.ModuleSpecification>); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<string> RequiredPSEditions { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<string>); } set { } }
    public System.Version RequiredPSVersion { get { return default(System.Version); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSSnapInSpecification> RequiresPSSnapIns { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.PSSnapInSpecification>); } set { } }
  }

  public abstract class StatementAst : System.Management.Automation.Language.Ast {
    protected StatementAst(System.Management.Automation.Language.IScriptExtent extent) : base(extent) { }

  }

  public class StatementBlockAst : System.Management.Automation.Language.Ast {
    public StatementBlockAst(System.Management.Automation.Language.IScriptExtent extent, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.StatementAst> statements, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.TrapStatementAst> traps) : base(extent) { }

    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.StatementAst> Statements { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.StatementAst>); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.TrapStatementAst> Traps { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.TrapStatementAst>); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class StaticBindingError {
    public System.Management.Automation.ParameterBindingException BindingException { get { return default(System.Management.Automation.ParameterBindingException); } set { } }
    public System.Management.Automation.Language.CommandElementAst CommandElement { get { return default(System.Management.Automation.Language.CommandElementAst); } set { } }
  }

  public class StaticBindingResult {
    public System.Collections.Generic.Dictionary<string, System.Management.Automation.Language.StaticBindingError> BindingExceptions { get { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.Language.StaticBindingError>); } }
    public System.Collections.Generic.Dictionary<string, System.Management.Automation.Language.ParameterBindingResult> BoundParameters { get { return default(System.Collections.Generic.Dictionary<string, System.Management.Automation.Language.ParameterBindingResult>); } }
  }

  public static class StaticParameterBinder {
    public static System.Management.Automation.Language.StaticBindingResult BindCommand ( System.Management.Automation.Language.CommandAst commandAst ) { return default(System.Management.Automation.Language.StaticBindingResult); }
    public static System.Management.Automation.Language.StaticBindingResult BindCommand ( System.Management.Automation.Language.CommandAst commandAst, bool resolve ) { return default(System.Management.Automation.Language.StaticBindingResult); }
    public static System.Management.Automation.Language.StaticBindingResult BindCommand ( System.Management.Automation.Language.CommandAst commandAst, bool resolve, string[] desiredParameters ) { return default(System.Management.Automation.Language.StaticBindingResult); }

  }

  public class StringConstantExpressionAst : System.Management.Automation.Language.ConstantExpressionAst {
    public StringConstantExpressionAst(System.Management.Automation.Language.IScriptExtent extent, string value, System.Management.Automation.Language.StringConstantType stringConstantType) : base(extent, value) { }


    public override System.Type StaticType { get { return default(System.Type); } }
    public System.Management.Automation.Language.StringConstantType StringConstantType { get { return default(System.Management.Automation.Language.StringConstantType); } set { } }
    public new string Value { get { return default(string); } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

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
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Token> NestedTokens { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.Token>); } set { } }
  }

  public class StringLiteralToken : System.Management.Automation.Language.StringToken {
    internal StringLiteralToken() { }
  }

  public abstract class StringToken : System.Management.Automation.Language.Token {
    internal StringToken() { }
    public string Value { get { return default(string); } }
  }

  public class SubExpressionAst : System.Management.Automation.Language.ExpressionAst {
    public SubExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.StatementBlockAst statementBlock): base (extent)  { }

    public System.Management.Automation.Language.StatementBlockAst SubExpression { get { return default(System.Management.Automation.Language.StatementBlockAst); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

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
    public SwitchStatementAst(System.Management.Automation.Language.IScriptExtent extent, string label, System.Management.Automation.Language.PipelineBaseAst condition, System.Management.Automation.Language.SwitchFlags flags, System.Collections.Generic.IEnumerable<System.Tuple<System.Management.Automation.Language.ExpressionAst, System.Management.Automation.Language.StatementBlockAst>> clauses, System.Management.Automation.Language.StatementBlockAst @default): base (extent, label, condition)  { }

    public System.Collections.ObjectModel.ReadOnlyCollection<System.Tuple<System.Management.Automation.Language.ExpressionAst, System.Management.Automation.Language.StatementBlockAst>> Clauses { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Tuple<System.Management.Automation.Language.ExpressionAst, System.Management.Automation.Language.StatementBlockAst>>); } set { } }
    public System.Management.Automation.Language.StatementBlockAst Default { get { return default(System.Management.Automation.Language.StatementBlockAst); } set { } }
    public System.Management.Automation.Language.SwitchFlags Flags { get { return default(System.Management.Automation.Language.SwitchFlags); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class ThrowStatementAst : System.Management.Automation.Language.StatementAst {
    public ThrowStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.PipelineBaseAst pipeline): base (extent)  { }

    public bool IsRethrow { get { return default(bool); } }
    public System.Management.Automation.Language.PipelineBaseAst Pipeline { get { return default(System.Management.Automation.Language.PipelineBaseAst); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class Token {
    internal Token() { }
    public System.Management.Automation.Language.IScriptExtent Extent { get { return default(System.Management.Automation.Language.IScriptExtent); } }
    public bool HasError { get { return default(bool); } }
    public System.Management.Automation.Language.TokenKind Kind { get { return default(System.Management.Automation.Language.TokenKind); } }
    public string Text { get { return default(string); } }
    public System.Management.Automation.Language.TokenFlags TokenFlags { get { return default(System.Management.Automation.Language.TokenFlags); } set { } }
    public override string ToString (  ) { return default(string); }

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
    StatementDoesntSupportAttributes = 16777216,
    TokenInError = 65536,
    TypeName = 2097152,
    UnaryOperator = 512,
  }

  public enum TokenKind {
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

   public static class TokenTraits {
    public static System.Management.Automation.Language.TokenFlags GetTraits ( System.Management.Automation.Language.TokenKind kind ) { return default(System.Management.Automation.Language.TokenFlags); }
    public static bool HasTrait ( System.Management.Automation.Language.TokenKind kind, System.Management.Automation.Language.TokenFlags flag ) { return default(bool); }
    public static string Text ( System.Management.Automation.Language.TokenKind kind ) { return default(string); }

  }

  public class TrapStatementAst : System.Management.Automation.Language.StatementAst {
    public TrapStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.TypeConstraintAst trapType, System.Management.Automation.Language.StatementBlockAst body): base (extent)  { }

    public System.Management.Automation.Language.StatementBlockAst Body { get { return default(System.Management.Automation.Language.StatementBlockAst); } set { } }
    public System.Management.Automation.Language.TypeConstraintAst TrapType { get { return default(System.Management.Automation.Language.TypeConstraintAst); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class TryStatementAst : System.Management.Automation.Language.StatementAst {
    public TryStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.StatementBlockAst body, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.CatchClauseAst> catchClauses, System.Management.Automation.Language.StatementBlockAst @finally): base (extent)  { }

    public System.Management.Automation.Language.StatementBlockAst Body { get { return default(System.Management.Automation.Language.StatementBlockAst); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.CatchClauseAst> CatchClauses { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.CatchClauseAst>); } set { } }
    public System.Management.Automation.Language.StatementBlockAst Finally { get { return default(System.Management.Automation.Language.StatementBlockAst); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

    [System.FlagsAttribute]
   public enum TypeAttributes {
    Class = 1,
    Enum = 4,
    Interface = 2,
    None = 0,
  }

  public class TypeConstraintAst : System.Management.Automation.Language.AttributeBaseAst {
    public TypeConstraintAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ITypeName typeName): base (extent, typeName)  { }
    public TypeConstraintAst(System.Management.Automation.Language.IScriptExtent extent, System.Type type): base (extent, default(System.Management.Automation.Language.ITypeName))  { }

    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class TypeDefinitionAst : System.Management.Automation.Language.StatementAst {
    public TypeDefinitionAst(System.Management.Automation.Language.IScriptExtent extent, string name, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.AttributeAst> attributes, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.MemberAst> members, System.Management.Automation.Language.TypeAttributes typeAttributes, System.Collections.Generic.IEnumerable<System.Management.Automation.Language.TypeConstraintAst> baseTypes): base (extent)  { }

    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.AttributeAst> Attributes { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.AttributeAst>); } set { } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.TypeConstraintAst> BaseTypes { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.TypeConstraintAst>); } set { } }
    public bool IsClass { get { return default(bool); } }
    public bool IsEnum { get { return default(bool); } }
    public bool IsInterface { get { return default(bool); } }
    public System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.MemberAst> Members { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Management.Automation.Language.MemberAst>); } set { } }
    public string Name { get { return default(string); } set { } }
    public System.Management.Automation.Language.TypeAttributes TypeAttributes { get { return default(System.Management.Automation.Language.TypeAttributes); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class TypeExpressionAst : System.Management.Automation.Language.ExpressionAst {
    public TypeExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ITypeName typeName): base (extent)  { }

    public override System.Type StaticType { get { return default(System.Type); } }
    public System.Management.Automation.Language.ITypeName TypeName { get { return default(System.Management.Automation.Language.ITypeName); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public sealed class TypeName : System.Management.Automation.Language.ITypeName {
    public TypeName(System.Management.Automation.Language.IScriptExtent extent, string name) { }
    public TypeName(System.Management.Automation.Language.IScriptExtent extent, string name, string assembly) { }

    public string AssemblyName { get { return default(string); } set { } }
    public System.Management.Automation.Language.IScriptExtent Extent { get { return default(System.Management.Automation.Language.IScriptExtent); } }
    public string FullName { get { return default(string); } }
    public bool IsArray { get { return default(bool); } }
    public bool IsGeneric { get { return default(bool); } }
    public string Name { get { return default(string); } }
    public override bool Equals ( object obj ) { return default(bool); }
    public override int GetHashCode (  ) { return default(int); }
    public System.Type GetReflectionAttributeType (  ) { return default(System.Type); }
    public System.Type GetReflectionType (  ) { return default(System.Type); }
    public override string ToString (  ) { return default(string); }

  }

  public class UnaryExpressionAst : System.Management.Automation.Language.ExpressionAst {
    public UnaryExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.TokenKind tokenKind, System.Management.Automation.Language.ExpressionAst child): base (extent)  { }

    public System.Management.Automation.Language.ExpressionAst Child { get { return default(System.Management.Automation.Language.ExpressionAst); } set { } }
    public override System.Type StaticType { get { return default(System.Type); } }
    public System.Management.Automation.Language.TokenKind TokenKind { get { return default(System.Management.Automation.Language.TokenKind); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public class UsingExpressionAst : System.Management.Automation.Language.ExpressionAst {
    public UsingExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.ExpressionAst expressionAst): base (extent)  { }

    public System.Management.Automation.Language.ExpressionAst SubExpression { get { return default(System.Management.Automation.Language.ExpressionAst); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }
    public static System.Management.Automation.Language.VariableExpressionAst ExtractUsingVariable ( System.Management.Automation.Language.UsingExpressionAst usingExpressionAst ) { return default(System.Management.Automation.Language.VariableExpressionAst); }

  }

  public class UsingStatementAst : System.Management.Automation.Language.StatementAst {
    public UsingStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.UsingStatementKind kind, System.Management.Automation.Language.StringConstantExpressionAst name): base (extent)  { }
    public UsingStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.HashtableAst moduleSpecification): base (extent)  { }
    public UsingStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.UsingStatementKind kind, System.Management.Automation.Language.StringConstantExpressionAst aliasName, System.Management.Automation.Language.StringConstantExpressionAst resolvedAliasAst): base (extent)  { }
    public UsingStatementAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.Language.StringConstantExpressionAst aliasName, System.Management.Automation.Language.HashtableAst moduleSpecification): base (extent)  { }

    public System.Management.Automation.Language.StringConstantExpressionAst Alias { get { return default(System.Management.Automation.Language.StringConstantExpressionAst); } set { } }
    public System.Management.Automation.Language.HashtableAst ModuleSpecification { get { return default(System.Management.Automation.Language.HashtableAst); } set { } }
    public System.Management.Automation.Language.StringConstantExpressionAst Name { get { return default(System.Management.Automation.Language.StringConstantExpressionAst); } set { } }
    public System.Management.Automation.Language.UsingStatementKind UsingStatementKind { get { return default(System.Management.Automation.Language.UsingStatementKind); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

  public enum UsingStatementKind {
    Assembly = 0,
    Command = 1,
    Module = 2,
    Namespace = 3,
    Type = 4,
  }

  public class VariableExpressionAst : System.Management.Automation.Language.ExpressionAst {
    public VariableExpressionAst(System.Management.Automation.Language.IScriptExtent extent, string variableName, bool splatted) : base (extent) { }
    public VariableExpressionAst(System.Management.Automation.Language.IScriptExtent extent, System.Management.Automation.VariablePath variablePath, bool splatted) : base (extent) { }

    public bool Splatted { get { return default(bool); } set { } }
    public System.Management.Automation.VariablePath VariablePath { get { return default(System.Management.Automation.VariablePath); } set { } }
    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }
    public bool IsConstantVariable (  ) { return default(bool); }

  }

  public class VariableToken : System.Management.Automation.Language.Token {
    internal VariableToken() { }
    public string Name { get { return default(string); } }
    public System.Management.Automation.VariablePath VariablePath { get { return default(System.Management.Automation.VariablePath); } set { } }
  }

  public class WhileStatementAst : System.Management.Automation.Language.LoopStatementAst {
    public WhileStatementAst(System.Management.Automation.Language.IScriptExtent extent, string label, System.Management.Automation.Language.PipelineBaseAst condition, System.Management.Automation.Language.StatementBlockAst body) : base (extent, label, condition, body) { }

    public override System.Management.Automation.Language.Ast Copy (  ) { return default(System.Management.Automation.Language.Ast); }

  }

}
namespace System.Management.Automation.Tracing {
  public abstract class EtwActivity {
    protected EtwActivity() { }

    public static System.Guid CreateActivityId (  ) { return default(System.Guid); }
    public static System.Guid GetActivityId (  ) { return default(System.Guid); }
    public static bool SetActivityId ( System.Guid activityId ) { return default(bool); }

  }

    [System.FlagsAttribute]
   public enum PowerShellTraceKeywords : ulong {
    Cmdlets = 32,
    Host = 16,
    ManagedPlugIn = 256,
    None = 0,
    Pipeline = 2,
    Protocol = 4,
    Runspace = 1,
    Serializer = 64,
    Session = 128,
    Transport = 8,
    UseAlwaysAnalytic = 4611686018427387904,
    UseAlwaysDebug = 2305843009213693952,
    UseAlwaysOperational = 9223372036854775808,
  }

  public sealed class PowerShellTraceSource : System.IDisposable {
    public void Dispose (  ) { }
    public bool TraceException ( System.Exception exception ) { return default(bool); }
    public bool WriteMessage ( string message ) { return default(bool); }
    public bool WriteMessage ( string message1, string message2 ) { return default(bool); }
    public bool WriteMessage ( string message, System.Guid instanceId ) { return default(bool); }
    public void WriteMessage ( string className, string methodName, System.Guid workflowId, string message, string[] parameters ) { }
    public void WriteMessage ( string className, string methodName, System.Guid workflowId, System.Management.Automation.Job job, string message, string[] parameters ) { }

  }

  public static class PowerShellTraceSourceFactory {
    public static System.Management.Automation.Tracing.PowerShellTraceSource GetTraceSource (  ) { return default(System.Management.Automation.Tracing.PowerShellTraceSource); }
    public static System.Management.Automation.Tracing.PowerShellTraceSource GetTraceSource ( System.Management.Automation.Tracing.PowerShellTraceTask task ) { return default(System.Management.Automation.Tracing.PowerShellTraceSource); }
    public static System.Management.Automation.Tracing.PowerShellTraceSource GetTraceSource ( System.Management.Automation.Tracing.PowerShellTraceTask task, System.Management.Automation.Tracing.PowerShellTraceKeywords keywords ) { return default(System.Management.Automation.Tracing.PowerShellTraceSource); }

  }

  public enum PowerShellTraceTask {
    CreateRunspace = 1,
    ExecuteCommand = 2,
    None = 0,
    PowerShellConsoleStartup = 4,
    Serialization = 3,
  }

  public sealed class Tracer : System.Management.Automation.Tracing.EtwActivity {
    public Tracer() { }

    public void BeginContainerParentJobExecution ( System.Guid containerParentJobInstanceId ) { }
    public void BeginProxyChildJobEventHandler ( System.Guid proxyChildJobInstanceId ) { }
    public void BeginProxyJobEventHandler ( System.Guid proxyJobInstanceId ) { }
    public void BeginProxyJobExecution ( System.Guid proxyJobInstanceId ) { }
    public void EndContainerParentJobExecution ( System.Guid containerParentJobInstanceId ) { }
    public void EndpointDisabled ( string endpointName, string disabledBy ) { }
    public void EndpointEnabled ( string endpointName, string enabledBy ) { }
    public void EndpointModified ( string endpointName, string modifiedBy ) { }
    public void EndpointRegistered ( string endpointName, string endpointType, string registeredBy ) { }
    public void EndpointUnregistered ( string endpointName, string unregisteredBy ) { }
    public void EndProxyJobEventHandler ( System.Guid proxyJobInstanceId ) { }
    public void EndProxyJobExecution ( System.Guid proxyJobInstanceId ) { }
    public void ProxyJobRemoteJobAssociation ( System.Guid proxyJobInstanceId, System.Guid containerParentJobInstanceId ) { }

  }

}
namespace System.Management.Automation.Internal {
  public static class AutomationNull {
    public static System.Management.Automation.PSObject Value { get { return default(System.Management.Automation.PSObject); } }
  }

  public static class ClassOps {
    public static void CallBaseCtor ( object target, System.Reflection.ConstructorInfo ci, object[] args ) { }
    public static object CallMethodNonVirtually ( object target, System.Reflection.MethodInfo mi, object[] args ) { return default(object); }
    public static void CallVoidMethodNonVirtually ( object target, System.Reflection.MethodInfo mi, object[] args ) { }
    public static void ValidateSetProperty ( System.Type type, string propertyName, object value ) { }

  }

   [System.AttributeUsageAttribute((System.AttributeTargets)32767)]
   public abstract class CmdletMetadataAttribute : System.Attribute {
     internal CmdletMetadataAttribute() { }
  }

  public sealed class CommonParameters {
    internal CommonParameters() { }
    [System.Management.Automation.AliasAttribute(new string[] { "db" })]
    [System.Management.Automation.ParameterAttribute]    
    public System.Management.Automation.SwitchParameter Debug { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] { "ea" })]
    [System.Management.Automation.ParameterAttribute]  
    public System.Management.Automation.ActionPreference ErrorAction { get { return default(System.Management.Automation.ActionPreference); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] { "ev" })]
    [System.Management.Automation.Internal.CommonParameters.ValidateVariableName]
    [System.Management.Automation.ParameterAttribute]  
    public string ErrorVariable { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] { "infa" })]
    [System.Management.Automation.ParameterAttribute]  
    public System.Management.Automation.ActionPreference InformationAction { get { return default(System.Management.Automation.ActionPreference); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] { "iv" })]
    [System.Management.Automation.Internal.CommonParameters.ValidateVariableName]
    [System.Management.Automation.ParameterAttribute]  
    public string InformationVariable { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] { "ob" })]
    [System.Management.Automation.ParameterAttribute]
    [System.Management.Automation.ValidateRangeAttribute(0, 2147483647)]
    public int OutBuffer { get { return default(int); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] { "ov" })]
    [System.Management.Automation.Internal.CommonParameters.ValidateVariableName]
    [System.Management.Automation.ParameterAttribute]  
    public string OutVariable { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] { "pv" })]
    [System.Management.Automation.Internal.CommonParameters.ValidateVariableName]
    [System.Management.Automation.ParameterAttribute]  
    public string PipelineVariable { get { return default(string); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] { "vb" })]
    [System.Management.Automation.ParameterAttribute]  
    public System.Management.Automation.SwitchParameter Verbose { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] { "wa" })]
    [System.Management.Automation.ParameterAttribute]  
    public System.Management.Automation.ActionPreference WarningAction { get { return default(System.Management.Automation.ActionPreference); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] { "wv" })]
    [System.Management.Automation.Internal.CommonParameters.ValidateVariableName]
    [System.Management.Automation.ParameterAttribute]  
    public string WarningVariable { get { return default(string); } set { } }
    internal class ValidateVariableName : System.Attribute { }
  }

  public static class DebuggerUtils {
    public const string GetPSCallStackOverrideFunction = @"function Get-PSCallStack
        {
            [CmdletBinding()]
            param()

            if ($null -ne $PSWorkflowDebugger)
            {
                foreach ($frame in $PSWorkflowDebugger.GetCallStack())
                {
                    Write-Output $frame
                }
            }

            Set-StrictMode -Off
        }";
    public const string RemoveVariableFunction = @"function Remove-DebuggerVariable
        {
            [CmdletBinding()]
            param(
                [Parameter(Position=0)]
                [string[]]
                $Name
            )

            foreach ($item in $Name)
            {
                microsoft.powershell.utility\remove-variable -name $item -scope global
            }

            Set-StrictMode -Off
        }";
    public const string SetVariableFunction = @"function Set-DebuggerVariable
        {
            [CmdletBinding()]
            param(
                [Parameter(Position=0)]
                [HashTable]
                $Variables
            )

            foreach($key in $Variables.Keys)
            {
                microsoft.powershell.utility\set-variable -Name $key -Value $Variables[$key] -Scope global
            }

            Set-StrictMode -Off
        }";
    public static void EndMonitoringRunspace ( System.Management.Automation.Debugger debugger, System.Management.Automation.Internal.PSMonitorRunspaceInfo runspaceInfo ) { }
    public static System.Collections.Generic.IEnumerable<System.String> GetWorkflowDebuggerFunctions (  ) { return default(System.Collections.Generic.IEnumerable<System.String>); }
    public static bool ShouldAddCommandToHistory ( string command ) { return default(bool); }
    public static void StartMonitoringRunspace ( System.Management.Automation.Debugger debugger, System.Management.Automation.Internal.PSMonitorRunspaceInfo runspaceInfo ) { }

  }

   [System.Diagnostics.DebuggerDisplayAttribute("Command = {commandInfo}")]
   public abstract class InternalCommand {
    internal InternalCommand() { }
    public System.Management.Automation.CommandOrigin CommandOrigin { get { return default(System.Management.Automation.CommandOrigin); } }
  }

  public static class InternalTestHooks {
    public static void SetTestHook ( string property, object value ) { }

  }

   [System.AttributeUsageAttribute((System.AttributeTargets)32767)]
   public abstract class ParsingBaseAttribute : System.Management.Automation.Internal.CmdletMetadataAttribute {
     internal ParsingBaseAttribute() { }
  }

  public sealed class PSEmbeddedMonitorRunspaceInfo : System.Management.Automation.Internal.PSMonitorRunspaceInfo {
    public PSEmbeddedMonitorRunspaceInfo(System.Management.Automation.Runspaces.Runspace runspace, System.Management.Automation.Internal.PSMonitorRunspaceType runspaceType, System.Management.Automation.PowerShell command, System.Guid parentDebuggerId) : base ( runspace, runspaceType) { }

    public System.Management.Automation.PowerShell Command { get { return default(System.Management.Automation.PowerShell); } set { } }
    public System.Guid ParentDebuggerId { get { return default(System.Guid); } set { } }
  }

  public abstract class PSMonitorRunspaceInfo {
    protected PSMonitorRunspaceInfo(System.Management.Automation.Runspaces.Runspace runspace, System.Management.Automation.Internal.PSMonitorRunspaceType runspaceType) { }

    public System.Management.Automation.Runspaces.Runspace Runspace { get { return default(System.Management.Automation.Runspaces.Runspace); } set { } }
    public System.Management.Automation.Internal.PSMonitorRunspaceType RunspaceType { get { return default(System.Management.Automation.Internal.PSMonitorRunspaceType); } set { } }
  }

  public enum PSMonitorRunspaceType {
    InvokeCommand = 1,
    Standalone = 0,
    WorkflowInlineScript = 2,
  }

  public sealed class PSStandaloneMonitorRunspaceInfo : System.Management.Automation.Internal.PSMonitorRunspaceInfo {
    public PSStandaloneMonitorRunspaceInfo(System.Management.Automation.Runspaces.Runspace runspace) : base(runspace, default(System.Management.Automation.Internal.PSMonitorRunspaceType)) { }

  }

  public class ScriptBlockMemberMethodWrapper {
    public void InvokeHelper ( object instance, object sessionStateInternal, object[] args ) { }
    public T InvokeHelperT<T> ( object instance, object sessionStateInternal, object[] args ) { return default(T); }

  }

  public static class SecuritySupport {
    public static bool IsProductBinary ( string file ) { return default(bool); }

  }

  public class SessionStateKeeper {
    public object GetSessionState (  ) { return default(object); }

  }

  public sealed class ShouldProcessParameters {
    internal ShouldProcessParameters() { }
    [System.Management.Automation.AliasAttribute(new string[] { "cf" })]
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter Confirm { get { return default(System.Management.Automation.SwitchParameter); } set { } }
    [System.Management.Automation.AliasAttribute(new string[] { "wi" })]
    [System.Management.Automation.ParameterAttribute]
    public System.Management.Automation.SwitchParameter WhatIf { get { return default(System.Management.Automation.SwitchParameter); } set { } }
  }


}
