#if !UNIX

/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using Microsoft.PowerShell.Commands.Internal;
using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Options;
using System.Linq;
using Dbg = System.Management.Automation;

#if CORECLR
using Microsoft.PowerShell.CoreClr.Stubs;
#else
//TODO:CORECLR System.DirectoryServices is not available on CORE CLR
using System.DirectoryServices;
//TODO:CORECLR System.Security.Permission is not available on CORE CLR
using System.Security.Permissions;
using System.Management; // We are not porting the library to CoreCLR
using Microsoft.WSMan.Management;
#endif

// FxCop suppressions for resource strings:
[module: SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", Scope = "resource", Target = "ComputerResources.resources", MessageId = "unjoined")]
[module: SuppressMessage("Microsoft.Naming", "CA1701:ResourceStringCompoundWordsShouldBeCasedCorrectly", Scope = "resource", Target = "ComputerResources.resources", MessageId = "UpTime")]

namespace Microsoft.PowerShell.Commands
{
#region Test-Connection

    /// <summary>
    /// This cmdlet is used to test whether a particular host is reachable across an 
    /// IP network. It works by sending ICMP "echo request" packets to the target 
    /// host and listening for ICMP "echo response" replies. This cmdlet prints a 
    /// statistical summary when finished.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, "Connection", DefaultParameterSetName = RegularParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135266", RemotingCapability = RemotingCapability.OwnedByCommand)]
    [OutputType(typeof(Boolean))]
    [OutputType(@"System.Management.ManagementObject#root\cimv2\Win32_PingStatus")]
    public class TestConnectionCommand : PSCmdlet
    {
#region "Parameters"

        private const string RegularParameterSet = "Default";
        private const string QuietParameterSet = "Quiet";
        private const string SourceParameterSet = "Source";

        /// <summary>
        /// 
        /// </summary>
        [Parameter(ParameterSetName = SourceParameterSet)]
        [Parameter(ParameterSetName = RegularParameterSet)]
        public SwitchParameter AsJob { get; set; } = false;

        /// <summary>
        /// The following is the definition of the input parameter "DcomAuthentication".
        /// Specifies the authentication level to be used with WMI connection. Valid 
        /// values are:
        /// 
        /// Unchanged = -1,
        /// Default = 0,
        /// None = 1,
        /// Connect = 2,
        /// Call = 3,
        /// Packet = 4,
        /// PacketIntegrity = 5,
        /// PacketPrivacy = 6.
        /// </summary>

        [Parameter]
        [Alias("Authentication")]
        public AuthenticationLevel DcomAuthentication { get; set; } = AuthenticationLevel.Packet;

        /// <summary>
        /// The authentication options for CIM_WSMan connection
        /// </summary>
        [Parameter]
        [ValidateSet(
            "Default",
            "Basic",
            "Negotiate", // can be used with and without credential (without -> PSRP mapped to NegotiateWithImplicitCredential)
            "CredSSP",
            "Digest",
            "Kerberos")] // can be used with and without credential (not sure about implications)
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public string WsmanAuthentication { get; set; } = "Default";

        /// <summary>
        /// Specify the protocol to use
        /// </summary>
        [Parameter]
        [ValidateSet(ComputerWMIHelper.DcomProtocol, ComputerWMIHelper.WsmanProtocol)]
        public string Protocol { get; set; } =
#if CORECLR
            //CoreClr does not support DCOM protocol
            // This change makes sure that the the command works seamlessly if user did not explicitly entered the protocol
            ComputerWMIHelper.WsmanProtocol;
#else
            ComputerWMIHelper.DcomProtocol;
#endif

        /// <summary>
        /// The following is the definition of the input parameter "BufferSize".
        /// Buffer size sent with the this command. The default value is 32.
        /// </summary>
        [Parameter]
        [Alias("Size", "Bytes", "BS")]
        [ValidateRange((int)0, (int)65500)]
        public Int32 BufferSize { get; set; } = 32;

        /// <summary>
        /// The following is the definition of the input parameter "ComputerName".
        /// Value of the address requested. The form of the value can be either the 
        /// computer name ("wxyz1234"), IPv4 address ("192.168.177.124"), or IPv6 
        /// address ("2010:836B:4179::836B:4179").
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [Alias("CN", "IPAddress", "__SERVER", "Server", "Destination")]
        public String[] ComputerName { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "Count".
        /// Number of echo requests to send.
        /// </summary>
        [Parameter]
        [ValidateRange(1, UInt32.MaxValue)]
        public Int32 Count { get; set; } = 4;

        /// <summary>
        /// The following is the definition of the input parameter "Credential".
        /// Specifies a user account that has permission to perform this action. Type a 
        /// user-name, such as "User01" or "Domain01\User01", or enter a PSCredential 
        /// object, such as one from the Get-Credential cmdlet
        /// </summary>
        [Parameter(ParameterSetName = SourceParameterSet, Mandatory = false)]
        [ValidateNotNullOrEmpty]
        [Credential]
        public PSCredential Credential { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "FromComputerName".
        /// Specifies the Computer names where the ping request is originated from.
        /// </summary>
        [Parameter(Position = 1, ParameterSetName = SourceParameterSet, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [Alias("FCN", "SRC")]
        public String[] Source { get; set; } = new string[] { "." };

        /// <summary>
        /// The following is the definition of the input parameter "Impersonation".
        /// Specifies the impersonation level to use when calling the WMI method. Valid 
        /// values are: 
        /// 
        /// Default = 0,
        /// Anonymous = 1,
        /// Identify = 2,
        /// Impersonate = 3,
        /// Delegate = 4.
        /// </summary>
        [Parameter]
        public ImpersonationLevel Impersonation { get; set; } = ImpersonationLevel.Impersonate;

        /// <summary>
        /// The following is the definition of the input parameter "ThrottleLimit".
        /// The number of concurrent computers on which the command will be allowed to 
        /// execute
        /// </summary>
        [Parameter(ParameterSetName = SourceParameterSet)]
        [Parameter(ParameterSetName = RegularParameterSet)]
        [ValidateRange(int.MinValue, (int)1000)]
        public Int32 ThrottleLimit
        {
            get { return throttlelimit; }
            set
            {
                throttlelimit = value;
                if (throttlelimit <= 0)
                    throttlelimit = 32;
            }
        }
        private Int32 throttlelimit = 32;

        /// <summary>
        /// The following is the definition of the input parameter "TimeToLive".
        /// Life span of the packet in seconds. The value is treated as an upper limit. 
        /// All routers must decrement this value by 1 (one). When this value becomes 0 
        /// (zero), the packet is dropped by the router. The default value is 80 
        /// seconds. The hops between routers rarely take this amount of time.
        /// </summary>
        [Parameter]
        [ValidateRange(1, (int)255)]
        [Alias("TTL")]
        public Int32 TimeToLive { get; set; } = 80;

        /// <summary>
        /// delay parameter
        /// </summary>
        [Parameter]
        [ValidateRange(1, 60)]
        public Int32 Delay { get; set; } = 1;

        /// <summary>
        /// quiet parameter
        /// </summary>
        [Parameter(ParameterSetName = QuietParameterSet)]
        public SwitchParameter Quiet
        {
            get { return quiet; }
            set { quiet = value; }
        }
        private bool quiet = false;

#endregion "parameters"
#region "Overrides"

#if !CORECLR
        ///// <summary>
        ///// To Store the output for each ping reply
        ///// </summary>
        private ManagementObjectSearcher searcher;

#endif
        private TransportProtocol _transportProtocol = TransportProtocol.DCOM;
        private readonly CancellationTokenSource cancel = new CancellationTokenSource();
        private Dictionary<string, bool> quietResults = new Dictionary<string, bool>();
        /// <summary>
        /// To begin processing Test-connection
        /// </summary>
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            // Verify parameter set

            bool haveProtocolParam = this.MyInvocation.BoundParameters.ContainsKey("Protocol");
            bool haveWsmanAuthenticationParam = this.MyInvocation.BoundParameters.ContainsKey("WsmanAuthentication");
            bool haveDcomAuthenticationParam = this.MyInvocation.BoundParameters.ContainsKey("DcomAuthentication");
            bool haveDcomImpersonation = this.MyInvocation.BoundParameters.ContainsKey("Impersonation");
            _transportProtocol = (this.Protocol.Equals(ComputerWMIHelper.WsmanProtocol, StringComparison.OrdinalIgnoreCase) || (haveWsmanAuthenticationParam && !haveProtocolParam)) ?
                                 TransportProtocol.WSMan : TransportProtocol.DCOM;

            if (haveWsmanAuthenticationParam && (haveDcomAuthenticationParam || haveDcomImpersonation))
            {
                string errMsg = StringUtil.Format(ComputerResources.StopCommandParamWSManAuthConflict, ComputerResources.StopCommandParamMessage);
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSArgumentException(errMsg),
                        "InvalidParameter",
                        ErrorCategory.InvalidArgument,
                        this));
            }

            if ((_transportProtocol == TransportProtocol.DCOM) && haveWsmanAuthenticationParam)
            {
                string errMsg = StringUtil.Format(ComputerResources.StopCommandWSManAuthProtcolConflict, ComputerResources.StopCommandParamMessage);
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSArgumentException(errMsg),
                        "InvalidParameter",
                        ErrorCategory.InvalidArgument,
                        this));
            }

            if ((_transportProtocol == TransportProtocol.WSMan) && (haveDcomAuthenticationParam || haveDcomImpersonation))
            {
                string errMsg = StringUtil.Format(ComputerResources.StopCommandAuthProtcolConflict, ComputerResources.StopCommandParamMessage);
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSArgumentException(errMsg),
                        "InvalidParameter",
                        ErrorCategory.InvalidArgument,
                        this));
            }

#if CORECLR
            if (this.MyInvocation.BoundParameters.ContainsKey("DcomAuthentication"))
            {
                string errMsg = StringUtil.Format(ComputerResources.InvalidParameterForCoreClr, "DcomAuthentication");
                PSArgumentException ex = new PSArgumentException(errMsg, ComputerResources.InvalidParameterForCoreClr);
                ThrowTerminatingError(new ErrorRecord(ex, "InvalidParameterForCoreClr", ErrorCategory.InvalidArgument, null));
            }

            if (this.MyInvocation.BoundParameters.ContainsKey("Impersonation"))
            {
                string errMsg = StringUtil.Format(ComputerResources.InvalidParameterForCoreClr, "Impersonation");
                PSArgumentException ex = new PSArgumentException(errMsg, ComputerResources.InvalidParameterForCoreClr);
                ThrowTerminatingError(new ErrorRecord(ex, "InvalidParameterForCoreClr", ErrorCategory.InvalidArgument, null));
            }

            if(this.Protocol.Equals(ComputerWMIHelper.DcomProtocol , StringComparison.OrdinalIgnoreCase))
            {
                InvalidOperationException ex = new InvalidOperationException(ComputerResources.InvalidParameterDCOMNotSupported);
                ThrowTerminatingError(new ErrorRecord(ex, "InvalidParameterDCOMNotSupported", ErrorCategory.InvalidOperation, null));
            }
#endif


            //testing
        }
        /// <summary>
        /// Process Record
        /// </summary>
        protected override void ProcessRecord()
        {
            switch (_transportProtocol)
            {
#if !CORECLR
                case TransportProtocol.DCOM:
                    processDCOMProtocolForTestConnection();
                    break;
#endif

                case TransportProtocol.WSMan:
                    ProcessWSManProtocolForTestConnection();
                    break;
            }
        }
        /// <summary>
        /// to implement ^C
        /// </summary> 
        protected override void StopProcessing()
        {
#if !CORECLR
            ManagementObjectSearcher stopSearcher = searcher;
            if (stopSearcher != null)
            {
                try
                {
                    stopSearcher.Dispose();
                }
                catch (ObjectDisposedException) { }
            }
#endif
            try
            {
                cancel.Cancel();
            }
            catch (ObjectDisposedException) { }
            catch (AggregateException) { }
        }
#endregion

#region "Private Methods "
        private string QueryString(string[] machinenames, bool escaperequired, bool selectrequired)
        {
            StringBuilder FilterString = new StringBuilder();
            if (selectrequired)
            {
                FilterString.Append("Select * from ");
                FilterString.Append(ComputerWMIHelper.WMI_Class_PingStatus);
                FilterString.Append(" where ");
            }
            FilterString.Append("((");
            for (int i = 0; i <= machinenames.Length - 1; i++)
            {
                FilterString.Append("Address='");
                string EscapeComp = machinenames[i].ToString();
                if (EscapeComp.Equals(".", StringComparison.CurrentCultureIgnoreCase))
                    EscapeComp = "localhost";
                if (escaperequired)
                {
                    EscapeComp = EscapeComp.Replace("\\", "\\\\'").ToString();
                    EscapeComp = EscapeComp.Replace("'", "\\'").ToString();
                }
                FilterString.Append(EscapeComp.ToString());
                FilterString.Append("'");
                if (i < machinenames.Length - 1)
                {
                    FilterString.Append(" Or ");
                }
            }
            FilterString.Append(")");
            FilterString.Append(" And ");
            FilterString.Append("TimeToLive=");
            FilterString.Append(TimeToLive);
            FilterString.Append(" And ");
            FilterString.Append("BufferSize=");
            FilterString.Append(BufferSize);
            FilterString.Append(")");
            return FilterString.ToString();
        }
        private void ProcessPingStatus(Object pingStatusObj)
        {
            Dbg.Diagnostics.Assert(pingStatusObj != null, "Caller should verify that pingStatus != null");
            //Dbg.Diagnostics.Assert(pingStatusObj.ClassPath.ClassName.Equals("Win32_PingStatus"), "Caller should verify that pingStatus is a Win32_PingStatus object");
            string destinationAddress = null;
            UInt32 primaryAddressResolutionStatus;
            UInt32 statusCode;
#if !CORECLR
            if (_transportProtocol == TransportProtocol.DCOM)
            {
                ManagementBaseObject pingStatus = (ManagementBaseObject)pingStatusObj;

                destinationAddress = (string)LanguagePrimitives.ConvertTo(
                    pingStatus.GetPropertyValue("Address"),
                    typeof(string),
                    CultureInfo.InvariantCulture);

                primaryAddressResolutionStatus = (UInt32)LanguagePrimitives.ConvertTo(
                    pingStatus.GetPropertyValue("PrimaryAddressResolutionStatus"),
                    typeof(UInt32),
                    CultureInfo.InvariantCulture);
                statusCode = (UInt32)LanguagePrimitives.ConvertTo(
                        pingStatus.GetPropertyValue("StatusCode"),
                        typeof(UInt32),
                        CultureInfo.InvariantCulture);
            }
            else
            {
#endif
                CimInstance pingStatus = (CimInstance)pingStatusObj;
                destinationAddress = (string)LanguagePrimitives.ConvertTo(
                        pingStatus.CimInstanceProperties["Address"].Value.ToString(),
                        typeof(string),
                        CultureInfo.InvariantCulture);
                primaryAddressResolutionStatus = (UInt32)LanguagePrimitives.ConvertTo(
                        pingStatus.CimInstanceProperties["PrimaryAddressResolutionStatus"].Value,
                        typeof(UInt32),
                        CultureInfo.InvariantCulture);
                statusCode = (UInt32)LanguagePrimitives.ConvertTo(
                        pingStatus.CimInstanceProperties["StatusCode"].Value,
                        typeof(UInt32),
                        CultureInfo.InvariantCulture);

#if !CORECLR
            }
#endif
            if (primaryAddressResolutionStatus != 0)
            {
                if (!quiet)
                {
                    Win32Exception win32Exception = new Win32Exception(unchecked((int)primaryAddressResolutionStatus));
                    string message = StringUtil.Format(ComputerResources.NoPingResult, destinationAddress, win32Exception.Message);
                    Exception pingException = new System.Net.NetworkInformation.PingException(message, win32Exception);
                    ErrorRecord errorRecord = new ErrorRecord(pingException, "TestConnectionException", ErrorCategory.ResourceUnavailable, destinationAddress);
                    WriteError(errorRecord);
                }
            }
            else
            {
                if (statusCode != 0)
                {
                    if (!quiet)
                    {
                        Win32Exception win32Exception = new Win32Exception(unchecked((int)statusCode));
                        string message = StringUtil.Format(ComputerResources.NoPingResult, destinationAddress, win32Exception.Message);
                        Exception pingException = new System.Net.NetworkInformation.PingException(message, win32Exception);
                        ErrorRecord errorRecord = new ErrorRecord(pingException, "TestConnectionException", ErrorCategory.ResourceUnavailable, destinationAddress);
                        WriteError(errorRecord);
                    }
                }
                else
                {
                    this.quietResults[destinationAddress] = true;
                    if (!quiet)
                    {
                        WriteObject(pingStatusObj);
                    }
                }
            }
        }

#if !CORECLR
        private void processDCOMProtocolForTestConnection()
        {
            ConnectionOptions options = ComputerWMIHelper.GetConnectionOptions(DcomAuthentication, this.Impersonation, this.Credential);
            if (AsJob)
            {
                string filter = QueryString(ComputerName, true, false);
                GetWmiObjectCommand WMICmd = new GetWmiObjectCommand();
                WMICmd.Filter = filter.ToString();
                WMICmd.Class = ComputerWMIHelper.WMI_Class_PingStatus;
                WMICmd.ComputerName = Source;
                WMICmd.Authentication = DcomAuthentication;
                WMICmd.Impersonation = Impersonation;
                WMICmd.ThrottleLimit = throttlelimit;
                PSWmiJob wmiJob = new PSWmiJob(WMICmd, Source, throttlelimit, this.MyInvocation.MyCommand.Name, Count);
                this.JobRepository.Add(wmiJob);
                WriteObject(wmiJob);
            }
            else
            {
                int sourceCount = 0;
                foreach (string fromcomp in Source)
                {
                    try
                    {
                        sourceCount++;
                        EnumerationOptions enumOptions = new EnumerationOptions();
                        enumOptions.UseAmendedQualifiers = true;
                        enumOptions.DirectRead = true;

                        int destCount = 0;
                        foreach (var tocomp in ComputerName)
                        {
                            destCount++;
                            string querystring = QueryString(new string[] { tocomp }, true, true);
                            ObjectQuery query = new ObjectQuery(querystring);
                            ManagementScope scope = new ManagementScope(ComputerWMIHelper.GetScopeString(fromcomp, ComputerWMIHelper.WMI_Path_CIM), options);
                            scope.Options.EnablePrivileges = true;
                            scope.Connect();

                            using (searcher = new ManagementObjectSearcher(scope, query, enumOptions))
                            {
                                for (int j = 0; j <= Count - 1; j++)
                                {
                                    using (ManagementObjectCollection mobj = searcher.Get())
                                    {
                                        int mobjCount = 0;
                                        foreach (ManagementBaseObject obj in mobj)
                                        {
                                            using (obj)
                                            {
                                                mobjCount++;

                                                ProcessPingStatus(obj);

                                                // to delay the request, if case to avoid the delay for the last pingrequest
                                                if (mobjCount < mobj.Count || j < Count - 1 || sourceCount < Source.Length || destCount < ComputerName.Length)
                                                    Thread.Sleep(Delay * 1000);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        searcher = null;
                    }
                    catch (ManagementException e)
                    {
                        ErrorRecord errorRecord = new ErrorRecord(e, "TestConnectionException", ErrorCategory.InvalidOperation, null);
                        WriteError(errorRecord);
                        continue;
                    }
                    catch (System.Runtime.InteropServices.COMException e)
                    {
                        ErrorRecord errorRecord = new ErrorRecord(e, "TestConnectionException", ErrorCategory.InvalidOperation, null);
                        WriteError(errorRecord);
                        continue;
                    }
                }
            }

            if (quiet)
            {
                foreach (string destinationAddress in this.ComputerName)
                {
                    bool destinationResult = false;
                    this.quietResults.TryGetValue(destinationAddress, out destinationResult);
                    WriteObject(destinationResult);
                }
            }
        }
#endif
        private void ProcessWSManProtocolForTestConnection()
        {
            if (AsJob)
            {
                // TODO:  Need job for MI.Net WSMan protocol
                // Early return of job object.
                throw new PSNotSupportedException();
            }

            var operationOptions = new CimOperationOptions
            {
                Timeout = TimeSpan.FromMilliseconds(2000),
                CancellationToken = cancel.Token
            };
            int destCount = 0;
            int sourceCount = 0;
            foreach (string sourceComp in Source)
            {
                try
                {
                    sourceCount++;
                    string sourceMachine;
                    if ((sourceComp.Equals("localhost", StringComparison.CurrentCultureIgnoreCase)) || (sourceComp.Equals(".", StringComparison.OrdinalIgnoreCase)))
                    {
                        sourceMachine = Dns.GetHostName();
                    }
                    else
                    {
                        sourceMachine = sourceComp;
                    }
                    foreach (var tocomp in ComputerName)
                    {
                        destCount++;
                        string querystring = QueryString(new string[] { tocomp }, true, true);

                        using (CimSession cimSession = RemoteDiscoveryHelper.CreateCimSession(sourceComp, this.Credential, WsmanAuthentication, cancel.Token, this))
                        {
                            for (int echoRequestCount = 0; echoRequestCount < Count; echoRequestCount++)
                            {
                                IEnumerable<CimInstance> mCollection = cimSession.QueryInstances(
                                                                     ComputerWMIHelper.CimOperatingSystemNamespace,
                                                                     ComputerWMIHelper.CimQueryDialect,
                                                                     querystring,
                                                                     operationOptions);
                                int total = mCollection.ToList().Count;
                                int cimInsCount = 1;
                                foreach (CimInstance obj in mCollection)
                                {
                                    ProcessPingStatus(obj);
                                    cimInsCount++;
                                    // to delay the request, if case to avoid the delay for the last pingrequest
                                    if (cimInsCount < total || echoRequestCount < Count - 1 || sourceCount < Source.Length || destCount < ComputerName.Length)
                                        Thread.Sleep(Delay * 1000);
                                }
                            }
                        }
                    }
                }
                catch (CimException ex)
                {
                    ErrorRecord errorRecord = new ErrorRecord(ex, "TestConnectionException", ErrorCategory.InvalidOperation, null);
                    WriteError(errorRecord);
                    continue;
                }
                catch (System.Runtime.InteropServices.COMException e)
                {
                    ErrorRecord errorRecord = new ErrorRecord(e, "TestConnectionException", ErrorCategory.InvalidOperation, null);
                    WriteError(errorRecord);
                    continue;
                }
            }

            if (quiet)
            {
                foreach (string destinationAddress in this.ComputerName)
                {
                    bool destinationResult = false;
                    this.quietResults.TryGetValue(destinationAddress, out destinationResult);
                    WriteObject(destinationResult);
                }
            }
        }

#endregion  "Private Methods "
    }
#endregion Test-Connection
#if !CORECLR

#region Enable-ComputerRestore

    /// <summary>
    /// Cmdlet for Enable-ComputerRestore
    /// </summary>
    [Cmdlet(VerbsLifecycle.Enable, "ComputerRestore", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135209")]
    public sealed class EnableComputerRestoreCommand : PSCmdlet, IDisposable
    {
#region Parameters
        /// <summary>
        /// Specifies the Drive on which the system restore will be enabled.
        /// The drive string should be of the form "C:\". 
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Drive { get; set; }

        #endregion Parameters
        private const string ErrorBase = "ComputerResources";
        private ManagementClass WMIClass;
#region "IDisposable Members"

        /// <summary>
        /// Dispose Method
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            // Use SuppressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose Method.
        /// </summary>
        /// <param name="disposing"></param>
        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (WMIClass != null)
                {
                    WMIClass.Dispose();
                }
            }
        }

#endregion "IDisposable Members"

#region Overrides

        /// <summary>
        /// To Enable the Restore Point of the drives
        /// </summary>
        protected override void BeginProcessing()
        {
            // system restore APIs are not supported on ARM platform
            if (ComputerWMIHelper.SkipSystemRestoreOperationForARMPlatform(this))
            {
                return;
            }

            ManagementScope scope = new ManagementScope(ComputerWMIHelper.WMI_Path_Default);
            scope.Connect();
            WMIClass = new ManagementClass(ComputerWMIHelper.WMI_Class_SystemRestore);
            WMIClass.Scope = scope;
            int retValue;
            //get the system drive
            string sysdrive = System.Environment.ExpandEnvironmentVariables("%SystemDrive%");
            sysdrive = String.Concat(new string[] { sysdrive, "\\" });


            if (ComputerWMIHelper.ContainsSystemDrive(Drive, sysdrive))
            {
                object[] input = { sysdrive };
                try
                {
                    retValue = Convert.ToInt32(WMIClass.InvokeMethod("Enable", input), System.Globalization.CultureInfo.CurrentCulture);
                    //if success (return value is 0 or if already enabled (error code is 1056 in XP and 0 in vista)
                    if ((retValue.Equals(0)) || (retValue.Equals(ComputerWMIHelper.ErrorCode_Service)))
                    {
                        string driveNew;
                        foreach (string drive in Drive) //for each input drive
                        {
                            if (!ShouldProcess(drive))
                            {
                                continue;
                            }
                            if (!drive.EndsWith("\\", StringComparison.CurrentCultureIgnoreCase))
                            {
                                driveNew = String.Concat(drive, "\\");
                            }
                            else
                                driveNew = drive;
                            if (!ComputerWMIHelper.IsValidDrive(driveNew))//if not valid drive,throw error
                            {
                                Exception Ex = new ArgumentException(StringUtil.Format(ComputerResources.InvalidDrive, drive));
                                WriteError(new ErrorRecord(Ex, "EnableComputerRestoreInvalidDrive", ErrorCategory.InvalidData, null));
                                continue;
                            }
                            //parameter for Enable method
                            //if the input drive is not system drive 
                            if (!driveNew.Equals(sysdrive, StringComparison.OrdinalIgnoreCase))
                            {
                                object[] inputDrive = { driveNew };
                                retValue = Convert.ToInt32(WMIClass.InvokeMethod("Enable", inputDrive), System.Globalization.CultureInfo.CurrentCulture);

                                //if not enabled, retry again
                                if (retValue.Equals(ComputerWMIHelper.ErrorCode_Interface))
                                {
                                    retValue = Convert.ToInt32(WMIClass.InvokeMethod("Enable", inputDrive), System.Globalization.CultureInfo.CurrentCulture);
                                }
                            }
                            //if not success and if it is not already enabled (error code is 1056 in XP) 
                            // Error 1717 - The interface is unknown. Even though this comes sometimes . The Drive is getting enabled.
                            if (!(retValue.Equals(0)) && !(retValue.Equals(ComputerWMIHelper.ErrorCode_Service)) && !(retValue.Equals(ComputerWMIHelper.ErrorCode_Interface)))
                            {
                                Exception Ex = new ArgumentException(StringUtil.Format(ComputerResources.NotEnabled, drive));
                                WriteError(new ErrorRecord(Ex, "EnableComputerRestoreNotEnabled", ErrorCategory.InvalidOperation, null));
                                continue;
                            }
                        }
                    }
                    else
                    {
                        ArgumentException Ex = new ArgumentException(StringUtil.Format(ComputerResources.NotEnabled, sysdrive));
                        WriteError(new ErrorRecord(Ex, "EnableComputerRestoreNotEnabled", ErrorCategory.InvalidOperation, null));
                    }
                }
                catch (ManagementException e)
                {
                    if ((e.ErrorCode.Equals(ManagementStatus.NotFound)) || (e.ErrorCode.Equals(ManagementStatus.InvalidClass)))
                    {
                        ErrorRecord er = new ErrorRecord(new ArgumentException(StringUtil.Format(ComputerResources.NotSupported)), null, ErrorCategory.InvalidOperation, null);
                        WriteError(er);
                    }
                    else
                    {
                        ErrorRecord errorRecord = new ErrorRecord(e, "GetWMIManagementException", ErrorCategory.InvalidOperation, null);
                        WriteError(errorRecord);
                    }
                }
                catch (COMException e)
                {
                    if (string.IsNullOrEmpty(e.Message))
                    {
                        Exception Ex = new ArgumentException(StringUtil.Format(ComputerResources.SystemRestoreServiceDisabled));
                        WriteError(new ErrorRecord(Ex, "ServiceDisabled", ErrorCategory.InvalidOperation, null));
                    }
                    else
                    {
                        ErrorRecord errorRecord = new ErrorRecord(e, "COMException", ErrorCategory.InvalidOperation, null);
                        WriteError(errorRecord);
                    }
                }
            }
            else
            {
                ArgumentException Ex = new ArgumentException(StringUtil.Format(ComputerResources.NoSystemDrive));
                WriteError(new ErrorRecord(Ex, "EnableComputerNoSystemDrive", ErrorCategory.InvalidArgument, null));
            }
        }//end of BeginProcessing

        /// <summary>
        /// to implement ^C
        /// </summary>
        protected override void StopProcessing()
        {
            if (WMIClass != null)
            {
                WMIClass.Dispose();
            }
        }

#endregion Overrides
    }//end of class
#endregion

#region Disable-ComputerRestore

    /// <summary>
    /// This cmdlet is to Disable Computer Restore points.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Disable, "ComputerRestore", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135207")]
    public sealed class DisableComputerRestoreCommand : PSCmdlet, IDisposable
    {
#region Parameters
        /// <summary>
        /// Specifies the Drive on which the system restore will be enabled.
        /// The drive string should be of the form "C:\". 
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Drive { get; set; }

        #endregion Parameters

        private ManagementClass WMIClass;
        private const string ErrorBase = "ComputerResources";
#region "IDisposable Members"

        /// <summary>
        /// Dispose Method
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            // Use SuppressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose Method.
        /// </summary>
        /// <param name="disposing"></param>
        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (WMIClass != null)
                {
                    WMIClass.Dispose();
                }
            }
        }

#endregion "IDisposable Members"

#region Overrides

        /// <summary>
        /// To Disable the Restore Point of the drives
        /// </summary>
        protected override void BeginProcessing()
        {
            // system restore APIs are not supported on ARM platform
            if (ComputerWMIHelper.SkipSystemRestoreOperationForARMPlatform(this))
            {
                return;
            }

            ManagementScope scope = new ManagementScope(ComputerWMIHelper.WMI_Path_Default);
            scope.Connect();
            WMIClass = new ManagementClass(ComputerWMIHelper.WMI_Class_SystemRestore);
            WMIClass.Scope = scope;
            string driveNew;
            foreach (string drive in Drive)
            {
                if (!ShouldProcess(drive))
                {
                    continue;
                }

                if (!drive.EndsWith("\\", StringComparison.CurrentCultureIgnoreCase))
                {
                    driveNew = String.Concat(drive, "\\");
                }
                else
                    driveNew = drive;


                if (!ComputerWMIHelper.IsValidDrive(driveNew))
                {
                    ErrorRecord er = new ErrorRecord(new ArgumentException(StringUtil.Format(ComputerResources.NotValidDrive, drive)), null, ErrorCategory.InvalidData, null);
                    WriteError(er);
                    continue;
                }
                else
                {
                    try
                    {
                        object[] input = { driveNew };
                        int retValue = Convert.ToInt32(WMIClass.InvokeMethod("Disable", input), System.Globalization.CultureInfo.CurrentCulture);
                        // Error 1717 - The interface is unknown. Even though this comes sometimes . The Drive is getting disabled.
                        if (!(retValue.Equals(0)) && !(retValue.Equals(ComputerWMIHelper.ErrorCode_Interface)))
                        {
                            ErrorRecord er = new ErrorRecord(new ArgumentException(StringUtil.Format(ComputerResources.NotDisabled, drive)), null, ErrorCategory.InvalidOperation, null);
                            WriteError(er);
                            continue;
                        }
                    }
                    catch (ManagementException e)
                    {
                        if ((e.ErrorCode.Equals(ManagementStatus.NotFound)) || (e.ErrorCode.Equals(ManagementStatus.InvalidClass)))
                        {
                            ErrorRecord er = new ErrorRecord(new ArgumentException(StringUtil.Format(ComputerResources.NotSupported)), null, ErrorCategory.InvalidOperation, null);
                            WriteError(er);
                        }
                        else
                        {
                            ErrorRecord errorRecord = new ErrorRecord(e, "GetWMIManagementException", ErrorCategory.InvalidOperation, null);
                            WriteError(errorRecord);
                        }
                    }
                    catch (COMException e)
                    {
                        if (string.IsNullOrEmpty(e.Message))
                        {
                            Exception Ex = new ArgumentException(StringUtil.Format(ComputerResources.SystemRestoreServiceDisabled));
                            WriteError(new ErrorRecord(Ex, "ServiceDisabled", ErrorCategory.InvalidOperation, null));
                        }
                        else
                        {
                            ErrorRecord errorRecord = new ErrorRecord(e, "COMException", ErrorCategory.InvalidOperation, null);
                            WriteError(errorRecord);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// to implement ^C
        /// </summary>
        protected override void StopProcessing()
        {
            if (WMIClass != null)
            {
                WMIClass.Dispose();
            }
        }

#endregion Overrides
    }//end of class
#endregion Disable-ComputerRestore

#region Checkpoint-Computer

    /// <summary>
    /// Creates the Restore Point for the Local computer 
    /// </summary>

    [Cmdlet(VerbsData.Checkpoint, "Computer", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135197")]
    public class CheckpointComputerCommand : PSCmdlet, IDisposable
    {
#region Parameters

        /// <summary>
        /// The description to be displayed so the user can easily identify a restore point. 
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Description { get; set; }


        /// <summary>
        /// The type of restore point. 
        /// </summary>
        [Parameter(Position = 1)]
        [Alias("RPT")]
        [ValidateSetAttribute(new string[] { "APPLICATION_INSTALL", "APPLICATION_UNINSTALL", "DEVICE_DRIVER_INSTALL", "MODIFY_SETTINGS", "CANCELLED_OPERATION" })]
        [ValidateNotNullOrEmpty]
        public string RestorePointType
        {
            get { return _restorepointtype; }
            set
            {
                // null check is not needed (because of ValidateNotNullOrEmpty),
                // but we have to include it to silence OACR
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException("value");
                }

                _restorepointtype = value;
                if (_restorepointtype.Equals("APPLICATION_INSTALL", StringComparison.OrdinalIgnoreCase))
                    intRestorePoint = 0;
                else if (_restorepointtype.Equals("APPLICATION_UNINSTALL", StringComparison.OrdinalIgnoreCase))
                    intRestorePoint = 1;
                else if (_restorepointtype.Equals("DEVICE_DRIVER_INSTALL", StringComparison.OrdinalIgnoreCase))
                    intRestorePoint = 10;
                else if (_restorepointtype.Equals("MODIFY_SETTINGS", StringComparison.OrdinalIgnoreCase))
                    intRestorePoint = 12;
                else if (_restorepointtype.Equals("CANCELLED_OPERATION", StringComparison.OrdinalIgnoreCase))
                    intRestorePoint = 13;
            }
        }
        private string _restorepointtype = "APPLICATION_INSTALL";
#endregion Parameters

#region private

        private DateTime lastTimeProgressWasWritten = DateTime.UtcNow;
        private int intRestorePoint = 0;
        private int ret = int.MaxValue;
        /// <summary>
        /// Shared Exception. Used when exception thrown from the Restore point thread.
        /// </summary>
        private static Exception exceptionfromnewthread = null;

        private void WriteProgress(string statusDescription, int? percentComplete)
        {
            ProgressRecordType recordType;
            if (percentComplete.HasValue && percentComplete.Value == 100)
            {
                recordType = ProgressRecordType.Completed;
            }
            else
            {
                recordType = ProgressRecordType.Processing;
            }

            if (recordType == ProgressRecordType.Processing)
            {
                TimeSpan timeSinceProgressWasWrittenLast = DateTime.UtcNow - lastTimeProgressWasWritten;
                if (timeSinceProgressWasWrittenLast < TimeSpan.FromMilliseconds(200))
                {
                    return;
                }
            }
            lastTimeProgressWasWritten = DateTime.UtcNow;

            string activityDescription = StringUtil.Format(ComputerResources.ProgressActivity);
            ProgressRecord progressRecord = new ProgressRecord(
                1905347723, // unique id 
                activityDescription,
                statusDescription);

            if (percentComplete.HasValue)
            {
                progressRecord.PercentComplete = percentComplete.Value;
            }

            progressRecord.RecordType = recordType;
            this.WriteProgress(progressRecord);
        }

        private void WriteProgress(DateTime starttime)
        {
            int percentageCompleted = ProgressRecord.GetPercentageComplete(starttime, TimeSpan.FromSeconds(90));
            if (percentageCompleted < 100)
            {
                WriteProgress(StringUtil.Format(ComputerResources.ProgressStatusCreatingRestorePoint, percentageCompleted), percentageCompleted);
            }
        }

        private DateTime startUtcTime, startLocalTime;
        private void WriteProgress()
        {
            while (true)
            {
                WriteProgress(this.startUtcTime);
                System.Threading.Thread.Sleep(1000);
                if (exceptionfromnewthread == null)
                {
                    if (ret == 0)
                    {
                        if (this.IsRestorePointCreated(Description, this.startLocalTime))
                        {
                            break;
                        }
                    }
                    else if (ret != int.MaxValue)
                    {
                        // Invocation is complete with error
                        break;
                    }
                }
                else
                {
                    // Exception is thrown, breaking the loop
                    break;
                }
            }

            WriteProgress(StringUtil.Format(ComputerResources.ProgressStatusCompleted), 100);
        }

        private void CreateRestorePoint()
        {
            ManagementClass WMIClass = null;
            try
            {
                ManagementScope scope = new ManagementScope(ComputerWMIHelper.WMI_Path_Default);
                scope.Connect();
                WMIClass = new ManagementClass(ComputerWMIHelper.WMI_Class_SystemRestore);
                WMIClass.Scope = scope;

                //create restore point
                ManagementBaseObject inParams = WMIClass.GetMethodParameters("CreateRestorePoint");
                object[] param = { Description, intRestorePoint, 100 }; // the event type will be always 100,Begin_System_Change
                ret = Convert.ToInt32(WMIClass.InvokeMethod("CreateRestorePoint", param), System.Globalization.CultureInfo.CurrentCulture);
            }
            catch (Exception ex)
            {
                // We catch all exceptions because we don't want the exception to be thrown from a separate worker thread
                exceptionfromnewthread = ex;
            }
            finally
            {
                if (WMIClass != null)
                {
                    WMIClass.Dispose();
                }
            }
        }

#endregion

#region "IDisposable Members"

        /// <summary>
        /// Dispose Method
        /// </summary>
        public void Dispose()
        {
            // Use SuppressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

#endregion "IDisposable Members"

        /// <summary>
        /// BeginProcessing method.
        /// </summary>
        protected override void BeginProcessing()
        {
            // system restore APIs are not supported on ARM platform
            if (ComputerWMIHelper.SkipSystemRestoreOperationForARMPlatform(this))
            {
                return;
            }

            //Setting Exception from the new thread always to null
            exceptionfromnewthread = null;

            //on vista, CANCELLED_OPERATION restorepointtype does not work
            if ((Environment.OSVersion.Version.Major >= 6) && (intRestorePoint == 13))
            {
                ErrorRecord er = new ErrorRecord(new InvalidOperationException(StringUtil.Format(ComputerResources.NotSupported)), null, ErrorCategory.InvalidOperation, null);
                ThrowTerminatingError(er);
            }

            if (!CanCreateNewRestorePoint(DateTime.Now)) { return; }

            this.startUtcTime = DateTime.UtcNow;
            this.startLocalTime = DateTime.Now;
            ThreadStart start = new ThreadStart(this.CreateRestorePoint);
            Thread thread = new Thread(start);
            thread.Start();
            WriteProgress();
            if (exceptionfromnewthread == null)
            {
                if (ret.Equals(1058))
                {
                    Exception Ex = new ArgumentException(StringUtil.Format(ComputerResources.ServiceDisabled));
                    WriteError(new ErrorRecord(Ex, "CheckpointComputerServiceDisabled", ErrorCategory.InvalidOperation, null));
                }
                else if (!(ret.Equals(0)) && !(ret.Equals(1058)))
                {
                    Exception Ex = new ArgumentException(StringUtil.Format(ComputerResources.RestorePointNotCreated));
                    WriteError(new ErrorRecord(Ex, "CheckpointComputerPointNotCreated", ErrorCategory.InvalidOperation, null));
                }
            }
            else
            {
                if (exceptionfromnewthread is System.Runtime.InteropServices.COMException)
                {
                    if (string.IsNullOrEmpty(exceptionfromnewthread.Message))
                    {
                        Exception e = new ArgumentException(StringUtil.Format(ComputerResources.SystemRestoreServiceDisabled));
                        WriteError(new ErrorRecord(e, "ServiceDisabled", ErrorCategory.InvalidOperation, null));
                    }
                    else
                    {
                        ErrorRecord errorRecord = new ErrorRecord(exceptionfromnewthread, "COMException", ErrorCategory.InvalidOperation, null);
                        WriteError(errorRecord);
                    }
                }
                else if (exceptionfromnewthread is ManagementException)
                {
                    if (((ManagementException)exceptionfromnewthread).ErrorCode.Equals(ManagementStatus.NotFound) || ((ManagementException)exceptionfromnewthread).ErrorCode.Equals(ManagementStatus.InvalidClass))
                    {
                        Exception e = new ArgumentException(StringUtil.Format(ComputerResources.NotSupported));
                        WriteError(new ErrorRecord(e, "CheckpointComputerNotSupported", ErrorCategory.InvalidOperation, null));
                    }
                    else
                    {
                        ErrorRecord errorRecord = new ErrorRecord(exceptionfromnewthread, "GetWMIManagementException", ErrorCategory.InvalidOperation, null);
                        WriteError(errorRecord);
                    }
                }
                else
                {
                    // For other exception we caught from the worker thread, we throw it out here
                    throw exceptionfromnewthread;
                }
            }
        }//End BeginProcessing()

        private bool CanCreateNewRestorePoint(DateTime startTime)
        {
            const string srRegistryKeyPath = @"Software\Microsoft\Windows NT\CurrentVersion\SystemRestore";
            const string srFrequencyKeyName = "SystemRestorePointCreationFrequency";
            Version osVersion = Environment.OSVersion.Version;
            int timeInterval = 1440; // Default value: 24 hours * 60 min/hr
            bool canCreate = true;

            // From Win8+, the default frequency of restore point creation is 24 hours, but the user
            // can create the DWORD value SystemRestorePointCreationFrequency under the registry key
            // HKLM\Software\Microsoft\Windows NT\CurrentVersion\SystemRestore, and the value of it
            // can change the frequency of restore point creation. There are three cases here:
            //   1. Such registry key doesn't exist. We use the default setting: 24 hours.
            //   2. Registry key value is 0. No frequency limitation, new restore point can be created anytime.
            //   3. Registry key value is integer N, the time interval is N minutes.
            if ((osVersion.Major > 6) || (osVersion.Major == 6 && osVersion.Minor >= 2))
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(srRegistryKeyPath))
                {
                    if (key != null)
                    {
                        object value = key.GetValue(srFrequencyKeyName);
                        if (value is int) { timeInterval = (int)value; }
                    }
                }

                var objectQuery = new ObjectQuery { QueryString = "select * from " + ComputerWMIHelper.WMI_Class_SystemRestore };
                var scope = new ManagementScope(ComputerWMIHelper.WMI_Path_Default);
                scope.Connect();

                try
                {
                    DateTime lastCreationTime = DateTime.MinValue;
                    using (var searcher = new ManagementObjectSearcher(scope, objectQuery))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            using (obj)
                            {
                                DateTime creationTime =
                                    ManagementDateTimeConverter.ToDateTime(obj.Properties["CreationTime"].Value.ToString());
                                if (creationTime > lastCreationTime)
                                {
                                    lastCreationTime = creationTime;
                                }
                            }
                        }
                    }

                    TimeSpan span = startTime.Subtract(lastCreationTime);
                    canCreate = (span.TotalMinutes >= timeInterval);
                }
                catch (Exception ex)
                {
                    // Fail to retrieve restore points
                    if (ex is ManagementException || ex is COMException)
                    {
                        // Something is wrong with System Restore (it may not be supported). We continue to let the
                        // call to "CreateRestorePoint" happen, and will handle the exception generated by that call.
                        canCreate = true;
                    }
                    else
                    {
                        // Not sure what happened, so we'd better terminate the execution and report the error.
                        string errorMsg = StringUtil.Format(ComputerResources.FailToRetrieveLastRestorePoint, ex.Message);
                        ThrowTerminatingError(
                            new ErrorRecord(new InvalidOperationException(errorMsg),
                                            "FailToRetrieveLastRestorePoint",
                                            ErrorCategory.InvalidOperation, null));
                    }
                }
            }

            if (!canCreate)
            {
                // A new restore point cannot be created yet, so we write out warning message.
                WriteWarning(StringUtil.Format(ComputerResources.CannotCreateRestorePointWarning, timeInterval));
            }

            return canCreate;
        }

        private bool IsRestorePointCreated(string description, DateTime starttime)
        {
            bool foundrestorepoint = false;
            ManagementScope scope = new ManagementScope(ComputerWMIHelper.WMI_Path_Default);
            scope.Connect();

            ObjectQuery objquery = new ObjectQuery();
            StringBuilder sb = new StringBuilder("select * from ");
            sb.Append(ComputerWMIHelper.WMI_Class_SystemRestore);
            sb.Append(" where  description = '");
            sb.Append(description.Replace("'", "\\'"));
            sb.Append("'");
            objquery.QueryString = sb.ToString();

            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, objquery))
                {
                    if (searcher.Get().Count > 0)
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            using (obj)
                            {
                                // we are adding 1 second to creationTime to account for the fact that milliseconds
                                // are not reported in CreationTime property - it is possible to have
                                // startTime = 2009-07-20 9:35:18.123
                                // creationTime = 2009-07-20 9:35:18
                                // which would indicate creationTime < startTime
                                DateTime creationTime = ManagementDateTimeConverter.ToDateTime(obj.Properties["CreationTime"].Value.ToString());
                                if (creationTime.AddSeconds(1.0) >= starttime)
                                {
                                    foundrestorepoint = true;
                                }
                            }
                        }
                    }
                }
            }
            catch (ManagementException)
            {
                foundrestorepoint = true;
            }
            catch (COMException)
            {
                foundrestorepoint = true;
            }
            return foundrestorepoint;
        }
    }
#endregion

#region Get-ComputerRestorePoint

    /// <summary>
    /// This cmdlet is to Get Computer Restore points.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "ComputerRestorePoint", DefaultParameterSetName = "ID", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135215")]
    [OutputType(@"System.Management.ManagementObject#root\default\SystemRestore")]
    public sealed class GetComputerRestorePointCommand : PSCmdlet, IDisposable
    {
#region Parameters

        /// <summary>
        /// This cmdlet is to get Computer Restore points.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = "ID")]
        [ValidateNotNullOrEmpty]
        [ValidateRangeAttribute((int)1, int.MaxValue)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public int[] RestorePoint { get; set; }

        /// <summary>
        /// This cmdlet is to get Computer Restore points.
        /// </summary>
        [Parameter(ParameterSetName = "LastStatus", Mandatory = true)]
        [ValidateNotNull]
        public SwitchParameter LastStatus { get; set; }

        #endregion

        private ManagementClass WMIClass;

#region "IDisposable Members"

        /// <summary>
        /// Dispose Method
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            // Use SuppressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose Method.
        /// </summary>
        /// <param name="disposing"></param>
        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (WMIClass != null)
                {
                    WMIClass.Dispose();
                }
            }
        }

#endregion "IDisposable Members"

#region overrides
        /// <summary>
        /// Gets the list of Computer Restore point.
        /// ID parameter id used to refer the sequence no. When given searched with particular 
        /// sequence no. and returns the restore point
        /// </summary>
        protected override void BeginProcessing()
        {
            // system restore APIs are not supported on ARM platform
            if (ComputerWMIHelper.SkipSystemRestoreOperationForARMPlatform(this))
            {
                return;
            }

            try
            {
                ManagementScope scope = new ManagementScope(ComputerWMIHelper.WMI_Path_Default);
                scope.Connect();
                WMIClass = new ManagementClass(ComputerWMIHelper.WMI_Class_SystemRestore);
                WMIClass.Scope = scope;

                if (ParameterSetName.Equals("LastStatus"))
                {
                    int restoreStatus = Convert.ToInt32(WMIClass.InvokeMethod("GetLastRestoreStatus", null), System.Globalization.CultureInfo.CurrentCulture);

                    if (restoreStatus.Equals(0))
                        WriteObject(ComputerResources.RestoreFailed);
                    else if (restoreStatus.Equals(1))
                        WriteObject(ComputerResources.RestoreSuceess);
                    else if (restoreStatus.Equals(2))
                        WriteObject(ComputerResources.RestoreInterrupted);
                }
                Dictionary<int, string> sequenceList = new Dictionary<int, string>();
                if (ParameterSetName.Equals("ID"))
                {
                    ObjectQuery objquery = new ObjectQuery();
                    // Dictionary<int,string>  sequenceList = new Dictionary<int,string>();
                    if (RestorePoint == null)
                    {
                        objquery.QueryString = "select * from " + ComputerWMIHelper.WMI_Class_SystemRestore;
                    }
                    else
                    {
                        //  sequenceList = new List<int>();
                        StringBuilder sb = new StringBuilder("select * from ");
                        sb.Append(ComputerWMIHelper.WMI_Class_SystemRestore);
                        sb.Append(" where  SequenceNumber = ");
                        for (int i = 0; i <= RestorePoint.Length - 1; i++)
                        {
                            sb.Append(RestorePoint[i]);
                            if (i < RestorePoint.Length - 1)
                                sb.Append(" OR SequenceNumber = ");
                            if (!sequenceList.ContainsKey(RestorePoint[i]))
                                sequenceList.Add(RestorePoint[i], "true");
                        }
                        objquery.QueryString = sb.ToString();
                    }

                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, objquery))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            using (obj)
                            {
                                WriteObject(obj);
                                if (RestorePoint != null)
                                {
                                    int sequenceNo = Convert.ToInt32(obj.Properties["SequenceNumber"].Value, System.Globalization.CultureInfo.CurrentCulture);
                                    sequenceList.Remove(sequenceNo);
                                }
                            }
                        }
                    }

                    if (sequenceList != null)
                    {
                        if (sequenceList.Count > 0)
                        {
                            foreach (int id in sequenceList.Keys)
                            {
                                string message = StringUtil.Format(ComputerResources.NoResorePoint, id);
                                ArgumentException e = new ArgumentException(message);
                                ErrorRecord errorrecord = new ErrorRecord(e, "NoRestorePoint", ErrorCategory.InvalidArgument, null);
                                WriteError(errorrecord);
                            }
                        }
                    }
                }
            }
            catch (ManagementException e)
            {
                if ((e.ErrorCode.Equals(ManagementStatus.NotFound)) || (e.ErrorCode.Equals(ManagementStatus.InvalidClass)))
                {
                    Exception Ex = new ArgumentException(StringUtil.Format(ComputerResources.NotSupported));
                    WriteError(new ErrorRecord(Ex, "GetComputerRestorePointNotSupported", ErrorCategory.InvalidOperation, null));
                }
                else
                {
                    ErrorRecord errorRecord = new ErrorRecord(e, "GetWMIManagementException", ErrorCategory.InvalidOperation, null);
                    WriteError(errorRecord);
                }
            }
            catch (COMException e)
            {
                if (string.IsNullOrEmpty(e.Message))
                {
                    Exception Ex = new ArgumentException(StringUtil.Format(ComputerResources.SystemRestoreServiceDisabled));
                    WriteError(new ErrorRecord(Ex, "ServiceDisabled", ErrorCategory.InvalidOperation, null));
                }
                else
                {
                    ErrorRecord errorRecord = new ErrorRecord(e, "COMException", ErrorCategory.InvalidOperation, null);
                    WriteError(errorRecord);
                }
            }
        }

        /// <summary>
        /// to implement ^C
        /// </summary>
        protected override void StopProcessing()
        {
            if (WMIClass != null)
            {
                WMIClass.Dispose();
            }
        }

#endregion overrides
    }

#endregion

#endif

#region Restart-Computer

    /// <summary>
    /// This exception is thrown when the timeout expires before a computer finishes restarting
    /// </summary>
    [Serializable]
    public sealed class RestartComputerTimeoutException : RuntimeException
    {
        /// <summary>
        /// Name of the computer that is restarting
        /// </summary>
        public string ComputerName { get; private set; }

        /// <summary>
        /// The timeout value specified by the user. It indicates the seconds to wait before timeout.
        /// </summary>
        public int Timeout { get; private set; }

        /// <summary>
        /// Construct a RestartComputerTimeoutException.
        /// </summary>
        /// <param name="computerName"></param>
        /// <param name="timeout"></param>
        /// <param name="message"></param>
        /// <param name="errorId"></param>
        internal RestartComputerTimeoutException(string computerName, int timeout, string message, string errorId)
            : base(message)
        {
            SetErrorId(errorId);
            SetErrorCategory(ErrorCategory.OperationTimeout);
            ComputerName = computerName;
            Timeout = timeout;
        }

        /// <summary>
        /// Construct a RestartComputerTimeoutException
        /// </summary>
        public RestartComputerTimeoutException() : base() { }

        /// <summary>
        /// Constructs a RestartComputerTimeoutException
        /// </summary>
        ///
        /// <param name="message">
        /// The message used in the exception.
        /// </param>
        public RestartComputerTimeoutException(string message) : base(message) { }

        /// <summary>
        /// Constructs a RestartComputerTimeoutException
        /// </summary>
        ///
        /// <param name="message">
        /// The message used in the exception.
        /// </param>
        ///
        /// <param name="innerException"> 
        /// An exception that led to this exception.
        /// </param>
        public RestartComputerTimeoutException(string message, Exception innerException) : base(message, innerException) { }

#region Serialization
        /// <summary>
        /// Serialization constructor for class RestartComputerTimeoutException
        /// </summary>
        /// 
        /// <param name="info"> 
        /// serialization information 
        /// </param>
        /// 
        /// <param name="context"> 
        /// streaming context 
        /// </param>
        private RestartComputerTimeoutException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            if (info == null)
            {
                throw new PSArgumentNullException("info");
            }

            ComputerName = info.GetString("ComputerName");
            Timeout = info.GetInt32("Timeout");
        }

        /// <summary>
        /// Serializes the RestartComputerTimeoutException.
        /// </summary>
        /// 
        /// <param name="info"> 
        /// serialization information 
        /// </param>
        /// 
        /// <param name="context"> 
        /// streaming context 
        /// </param>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new PSArgumentNullException("info");
            }

            base.GetObjectData(info, context);
            info.AddValue("ComputerName", ComputerName);
            info.AddValue("Timeout", Timeout);
        }
#endregion Serialization
    }

    /// <summary>
    /// Defines the services that Restart-Computer can wait on
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
    public enum WaitForServiceTypes
    {
        /// <summary>
        /// Wait for the WMI service to be ready
        /// </summary>
        Wmi = 0x0,

        /// <summary>
        /// Wait for the WinRM service to be ready
        /// </summary>
        WinRM = 0x1,

        /// <summary>
        /// Wait for the PowerShell to be ready
        /// </summary>
        PowerShell = 0x2,
    }

    /// <summary>
    /// Restarts  the computer 
    /// </summary>
    [Cmdlet(VerbsLifecycle.Restart, "Computer", SupportsShouldProcess = true, DefaultParameterSetName = DefaultParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135253", RemotingCapability = RemotingCapability.OwnedByCommand)]
    public class RestartComputerCommand : PSCmdlet, IDisposable
    {
#region "Parameters and PrivateData"

        private const string DefaultParameterSet = "DefaultSet";
        private const string AsJobParameterSet = "AsJobSet";

        /// <summary>
        /// Used to start a command remotely as a Job. The Job results are collected 
        /// and stored in the global cache on the client machine. 
        /// </summary>
        [Parameter(ParameterSetName = AsJobParameterSet)]
        public SwitchParameter AsJob { get; set; } = false;

        /// <summary>
        /// The following is the definition of the input parameter "Authentication".
        /// Specifies the authentication level to be used with WMI connection. Valid 
        /// values are:
        /// 
        /// Unchanged = -1,
        /// Default = 0,
        /// None = 1,
        /// Connect = 2,
        /// Call = 3,
        /// Packet = 4,
        /// PacketIntegrity = 5,
        /// PacketPrivacy = 6.
        /// </summary>
        [Parameter]
        [Alias("Authentication")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public AuthenticationLevel DcomAuthentication
        {
            get { return _dcomAuthentication; }
            set
            {
                _dcomAuthentication = value;
                _isDcomAuthenticationSpecified = true;
            }
        }
        private AuthenticationLevel _dcomAuthentication = AuthenticationLevel.Packet;
        private bool _isDcomAuthenticationSpecified = false;

        /// <summary>
        /// The following is the definition of the input parameter "Impersonation".
        /// Specifies the impersonation level to use when calling the WMI method. Valid 
        /// values are: 
        /// 
        /// Default = 0,
        /// Anonymous = 1,
        /// Identify = 2,
        /// Impersonate = 3,
        /// Delegate = 4.
        /// </summary>
        [Parameter]
        public ImpersonationLevel Impersonation
        {
            get { return _impersonation; }
            set
            {
                _impersonation = value;
                _isImpersonationSpecified = true;
            }
        }
        private ImpersonationLevel _impersonation = ImpersonationLevel.Impersonate;
        private bool _isImpersonationSpecified = false;

        /// <summary>
        /// The authentication options for CIM_WSMan connection
        /// </summary>
        [Parameter(ParameterSetName = DefaultParameterSet)]
        [ValidateSet(
            "Default",
            "Basic",
            "Negotiate", // can be used with and without credential (without -> PSRP mapped to NegotiateWithImplicitCredential)
            "CredSSP",
            "Digest",
            "Kerberos")] // can be used with and without credential (not sure about implications)
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public string WsmanAuthentication { get; set; }

        /// <summary>
        /// Specify the protocol to use
        /// </summary>
        [Parameter(ParameterSetName = DefaultParameterSet)]
        [ValidateSet(ComputerWMIHelper.DcomProtocol, ComputerWMIHelper.WsmanProtocol)]
        public string Protocol
        {
            get { return _protocol; }
            set
            {
                _protocol = value;
                _isProtocolSpecified = true;
            }
        }

        //CoreClr does not support DCOM protocol
        // This change makes sure that the the command works seamlessly if user did not explicitly entered the protocol
#if CORECLR
        private string _protocol = ComputerWMIHelper.WsmanProtocol;
#else
        private string _protocol = ComputerWMIHelper.DcomProtocol;
#endif
        private bool _isProtocolSpecified = false;

        /// <summary>
        /// Specifies the computer (s)Name on which this command is executed. 
        /// When this parameter is omitted, this cmdlet restarts the local computer.
        /// Type the NETBIOS name, IP address, or fully-qualified domain name of one 
        /// or more computers in a comma-separated list. To specify the local computer, type the computername or "localhost". 
        /// </summary>
        [Parameter(Position = 0, ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [Alias("CN", "__SERVER", "Server", "IPAddress")]
        public String[] ComputerName { get; set; } = new string[] { "." };

        private List<string> _validatedComputerNames = new List<string>();
        private readonly List<string> _waitOnComputers = new List<string>();
        private readonly HashSet<string> _uniqueComputerNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The following is the definition of the input parameter "Credential".
        /// Specifies a user account that has permission to perform this action. Type a 
        /// user-name, such as "User01" or "Domain01\User01", or enter a PSCredential 
        /// object, such as one from the Get-Credential cmdlet
        /// </summary>
        [Parameter(Position = 1)]
        [ValidateNotNullOrEmpty]
        [Credential]
        public PSCredential Credential { get; set; }

        /// <summary>
        /// Using Force in conjunction with Reboot on a 
        /// remote computer immediately reboots the remote computer.  
        /// </summary>
        [Parameter]
        [Alias("f")]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// Allows the user of the cmdlet to specify a throttling value
        /// for throttling the number of remote operations that can
        /// be executed simultaneously.
        /// </summary>
        [Parameter(ParameterSetName = AsJobParameterSet)]
        [ValidateRange(int.MinValue, (int)1000)]
        public Int32 ThrottleLimit
        {
            get { return _throttlelimit; }
            set
            {
                _throttlelimit = value;
                if (_throttlelimit <= 0)
                    _throttlelimit = 32;
            }
        }
        private Int32 _throttlelimit = 32;

        /// <summary>
        /// Specify the Wait parameter. Prompt will be blocked is the Timeout is not 0
        /// </summary>
        [Parameter(ParameterSetName = DefaultParameterSet)]
        public SwitchParameter Wait { get; set; }

        /// <summary>
        /// Specify the Timeout parameter. 
        /// Negative value indicates wait infinitely.
        /// Positive value indicates the seconds to wait before timeout.
        /// </summary>
        [Parameter(ParameterSetName = DefaultParameterSet)]
        [Alias("TimeoutSec")]
        [ValidateRange(-1, int.MaxValue)]
        public int Timeout
        {
            get { return _timeout; }
            set
            {
                _timeout = value;
                _timeoutSpecified = true;
            }
        }
        private int _timeout = -1;
        private bool _timeoutSpecified = false;

        /// <summary>
        /// Specify the For parameter.
        /// Wait for the specific service before unblocking the prompt.
        /// </summary>
        [Parameter(ParameterSetName = DefaultParameterSet)]
        public WaitForServiceTypes For
        {
            get { return _waitFor; }
            set
            {
                _waitFor = value;
                _waitForSpecified = true;
            }
        }
        private WaitForServiceTypes _waitFor = WaitForServiceTypes.PowerShell;
        private bool _waitForSpecified = false;

        /// <summary>
        /// Specify the Delay parameter.
        /// The specific time interval (in second) to wait between network pings or service queries.
        /// </summary>
        [Parameter(ParameterSetName = DefaultParameterSet)]
        [ValidateRange(1, Int16.MaxValue)]
        public Int16 Delay
        {
            get { return (Int16)_delay; }
            set
            {
                _delay = value;
                _delaySpecified = true;
            }
        }
        private int _delay = 5;
        private bool _delaySpecified = false;

        /// <summary>
        /// Script to test if the PowerShell is ready
        /// </summary>
        private const string TestPowershellScript = @"
$array = @($input)
$result = @{}
foreach ($computerName in $array[1])
{
    $ret = $null
    if ($array[0] -eq $null)
    {
        $ret = Invoke-Command -ComputerName $computerName {$true} -SessionOption (New-PSSessionOption -NoMachineProfile) -ErrorAction SilentlyContinue
    }
    else
    {
        $ret = Invoke-Command -ComputerName $computerName {$true} -SessionOption (New-PSSessionOption -NoMachineProfile) -ErrorAction SilentlyContinue -Credential $array[0]
    }

    if ($ret -eq $true)
    {
        $result[$computerName] = $true
    }
    else
    {
        $result[$computerName] = $false
    }
}
$result
";

        /// <summary>
        /// The indicator to use when show progress
        /// </summary>
        private string[] _indicator = { "|", "/", "-", "\\" };

        /// <summary>
        /// The activity id
        /// </summary>
        private int _activityId;

        /// <summary>
        /// After call 'Shutdown' on the target computer, wait a few
        /// seconds for the restart to begin.
        /// </summary>
        private const int SecondsToWaitForRestartToBegin = 25;

        /// <summary>
        /// Actual time out in seconds
        /// </summary>
        private int _timeoutInMilliseconds;

        /// <summary>
        /// Indicate to exit
        /// </summary>
        private bool _exit, _timeUp;
        private readonly CancellationTokenSource _cancel = new CancellationTokenSource();

        /// <summary>
        /// A waithandler to wait on. Current thread will wait on it during the delay interval.
        /// </summary>
        private readonly ManualResetEventSlim _waitHandler = new ManualResetEventSlim(false);
        private readonly Dictionary<string, ComputerInfo> _computerInfos = new Dictionary<string, ComputerInfo>(StringComparer.OrdinalIgnoreCase);

        // CLR 4.0 Port note - use https://msdn.microsoft.com/en-us/library/system.net.networkinformation.ipglobalproperties.hostname(v=vs.110).aspx
        private readonly string _shortLocalMachineName = Dns.GetHostName();

        // And for this, use PsUtils.GetHostname()
        private readonly string _fullLocalMachineName = Dns.GetHostEntryAsync("").Result.HostName;

        private int _percent;
        private string _status;
        private string _activity;
        private Timer _timer;
        private System.Management.Automation.PowerShell _powershell;

        private const string StageVerification = "VerifyStage";
        private const string WmiConnectionTest = "WMI";
        private const string WinrmConnectionTest = "WinRM";
        private const string PowerShellConnectionTest = "PowerShell";

#endregion "parameters and PrivateData"

#region "IDisposable Members"

        /// <summary>
        /// Dispose Method
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            // Use SuppressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose Method.
        /// </summary>
        /// <param name="disposing"></param>
        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_timer != null)
                {
                    _timer.Dispose();
                }

                _waitHandler.Dispose();
                _cancel.Dispose();
                if (_powershell != null)
                {
                    _powershell.Dispose();
                }
            }
        }

#endregion "IDisposable Members"

#region "Private Methods"

        /// <summary>
        /// Validate parameters for 'DefaultSet'
        /// 1. When the Wait is specified, the computername cannot contain the local machine
        /// 2. If the local machine is present, make sure it is at the end of the list (so the remote ones get restarted before the local machine reboot).
        /// </summary>
        private void ValidateComputerNames()
        {
            bool containLocalhost = false;
            _validatedComputerNames.Clear();

            foreach (string name in ComputerName)
            {
                ErrorRecord error = null;
                string targetComputerName = ComputerWMIHelper.ValidateComputerName(name, _shortLocalMachineName, _fullLocalMachineName, ref error);
                if (targetComputerName == null)
                {
                    if (error != null)
                    {
                        WriteError(error);
                    }
                    continue;
                }

                if (targetComputerName.Equals(ComputerWMIHelper.localhostStr, StringComparison.OrdinalIgnoreCase))
                {
                    containLocalhost = true;
                }
                else if (!_uniqueComputerNames.Contains(targetComputerName))
                {
                    _validatedComputerNames.Add(targetComputerName);
                    _uniqueComputerNames.Add(targetComputerName);
                }
            }

            if (Wait && containLocalhost)
            {
                // The local machine will be ignored, and an error will be emitted.
                InvalidOperationException ex = new InvalidOperationException(ComputerResources.CannotWaitLocalComputer);
                WriteError(new ErrorRecord(ex, "CannotWaitLocalComputer", ErrorCategory.InvalidOperation, null));
                containLocalhost = false;
            }

            // Add the localhost to the end of the list, so we will restart remote machines
            // before we restart the local one.
            if (containLocalhost)
            {
                _validatedComputerNames.Add(ComputerWMIHelper.localhostStr);
            }
        }

        /// <summary>
        /// Write out progress
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="status"></param>
        /// <param name="percent"></param>
        /// <param name="progressRecordType"></param>
        private void WriteProgress(string activity, string status, int percent, ProgressRecordType progressRecordType)
        {
            ProgressRecord progress = new ProgressRecord(_activityId, activity, status);
            progress.PercentComplete = percent;
            progress.RecordType = progressRecordType;
            WriteProgress(progress);
        }

        /// <summary>
        /// Calculate the progress percentage
        /// </summary>
        /// <param name="currentStage"></param>
        /// <returns></returns>
        private int CalculateProgressPercentage(string currentStage)
        {
            switch (currentStage)
            {
                case StageVerification:
                    return _waitFor.Equals(WaitForServiceTypes.Wmi) || _waitFor.Equals(WaitForServiceTypes.WinRM)
                               ? 33
                               : 20;
                case WmiConnectionTest:
                    return _waitFor.Equals(WaitForServiceTypes.Wmi) ? 66 : 40;
                case WinrmConnectionTest:
                    return _waitFor.Equals(WaitForServiceTypes.WinRM) ? 66 : 60;
                case PowerShellConnectionTest:
                    return 80;
                default:
                    break;
            }

            Dbg.Diagnostics.Assert(false, "CalculateProgressPercentage should never hit the default case");
            return 0;
        }

        /// <summary>
        /// Event handler for the timer
        /// </summary>
        /// <param name="s"></param>
        private void OnTimedEvent(object s)
        {
            _exit = _timeUp = true;
            _cancel.Cancel();
            _waitHandler.Set();

            if (_powershell != null)
            {
                _powershell.Stop();
                _powershell.Dispose();
            }
        }

        private class ComputerInfo
        {
            internal string LastBootUpTime;
            internal bool RebootComplete;
        }

#if !CORECLR
        private List<string> TestRestartStageUsingDcom(IEnumerable<string> computerNames, List<string> nextTestList, CancellationToken token, ConnectionOptions options)
        {
            var restartStageTestList = new List<string>();
            var query = new ObjectQuery("Select * from " + ComputerWMIHelper.WMI_Class_OperatingSystem);
            var enumOptions = new EnumerationOptions { UseAmendedQualifiers = true, DirectRead = true };

            foreach (var computer in computerNames)
            {
                try
                {
                    if (token.IsCancellationRequested) { break; }
                    var scope = new ManagementScope(ComputerWMIHelper.GetScopeString(computer, ComputerWMIHelper.WMI_Path_CIM), options);
                    using (var searcher = new ManagementObjectSearcher(scope, query, enumOptions))
                    {
                        if (token.IsCancellationRequested) { break; }

                        using (var mCollection = searcher.Get())
                        {
                            if (mCollection.Count > 0)
                            {
                                foreach (ManagementBaseObject os in mCollection)
                                {
                                    using (os)
                                    {
                                        string newLastBootUpTime = os.Properties["LastBootUpTime"].Value.ToString();
                                        string oldLastBootUpTime = _computerInfos[computer].LastBootUpTime;

                                        if (string.Compare(newLastBootUpTime, oldLastBootUpTime, StringComparison.OrdinalIgnoreCase) != 0)
                                        {
                                            _computerInfos[computer].RebootComplete = true;
                                            nextTestList.Add(computer);
                                        }
                                        else
                                        {
                                            restartStageTestList.Add(computer);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                restartStageTestList.Add(computer);
                            }
                        }
                    }
                }
                catch (ManagementException)
                {
                    restartStageTestList.Add(computer);
                }
                catch (COMException)
                {
                    restartStageTestList.Add(computer);
                }
                catch (UnauthorizedAccessException)
                {
                    restartStageTestList.Add(computer);
                }
            }

            return restartStageTestList;
        }
#endif

        private List<string> TestRestartStageUsingWsman(IEnumerable<string> computerNames, List<string> nextTestList, CancellationToken token)
        {
            var restartStageTestList = new List<string>();
            var operationOptions = new CimOperationOptions
            {
                Timeout = TimeSpan.FromMilliseconds(2000),
                CancellationToken = token
            };
            foreach (var computer in computerNames)
            {
                try
                {
                    if (token.IsCancellationRequested) { break; }
                    using (CimSession cimSession = RemoteDiscoveryHelper.CreateCimSession(computer, Credential, WsmanAuthentication, token, this))
                    {
                        bool itemRetrieved = false;
                        IEnumerable<CimInstance> mCollection = cimSession.QueryInstances(
                                                                 ComputerWMIHelper.CimOperatingSystemNamespace,
                                                                 ComputerWMIHelper.CimQueryDialect,
                                                                 "Select * from " + ComputerWMIHelper.WMI_Class_OperatingSystem,
                                                                 operationOptions);
                        foreach (CimInstance os in mCollection)
                        {
                            itemRetrieved = true;
                            string newLastBootUpTime = os.CimInstanceProperties["LastBootUpTime"].Value.ToString();
                            string oldLastBootUpTime = _computerInfos[computer].LastBootUpTime;

                            if (string.Compare(newLastBootUpTime, oldLastBootUpTime, StringComparison.OrdinalIgnoreCase) != 0)
                            {
                                _computerInfos[computer].RebootComplete = true;
                                nextTestList.Add(computer);
                            }
                            else
                            {
                                restartStageTestList.Add(computer);
                            }
                        }

                        if (!itemRetrieved)
                        {
                            restartStageTestList.Add(computer);
                        }
                    }
                }
                catch (CimException)
                {
                    restartStageTestList.Add(computer);
                }
                catch (Exception ex)
                {
                    CommandProcessorBase.CheckForSevereException(ex);
                    restartStageTestList.Add(computer);
                }
            }

            return restartStageTestList;
        }

#if !CORECLR
        private List<string> SetUpComputerInfoUsingDcom(IEnumerable<string> computerNames, ConnectionOptions options)
        {
            var validComputerNameList = new List<string>();
            var query = new ObjectQuery("Select * From " + ComputerWMIHelper.WMI_Class_OperatingSystem);
            var enumOptions = new EnumerationOptions { UseAmendedQualifiers = true, DirectRead = true };

            foreach (var computer in computerNames)
            {
                try
                {
                    var scope = new ManagementScope(ComputerWMIHelper.GetScopeString(computer, ComputerWMIHelper.WMI_Path_CIM), options);
                    using (var searcher = new ManagementObjectSearcher(scope, query, enumOptions))
                    {
                        using (var mCollection = searcher.Get())
                        {
                            if (mCollection.Count > 0)
                            {
                                foreach (ManagementBaseObject os in mCollection)
                                {
                                    using (os)
                                    {
                                        if (!_computerInfos.ContainsKey(computer))
                                        {
                                            var info = new ComputerInfo
                                            {
                                                LastBootUpTime = os.Properties["LastBootUpTime"].Value.ToString(),
                                                RebootComplete = false
                                            };
                                            _computerInfos.Add(computer, info);
                                            validComputerNameList.Add(computer);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                string errMsg = StringUtil.Format(ComputerResources.RestartComputerSkipped, computer, ComputerResources.CannotGetOperatingSystemObject);
                                var error = new ErrorRecord(new InvalidOperationException(errMsg), "RestartComputerSkipped",
                                                                ErrorCategory.OperationStopped, computer);
                                this.WriteError(error);
                            }
                        }
                    }
                }
                catch (ManagementException ex)
                {
                    string errMsg = StringUtil.Format(ComputerResources.RestartComputerSkipped, computer, ex.Message);
                    var error = new ErrorRecord(new InvalidOperationException(errMsg), "RestartComputerSkipped",
                                                        ErrorCategory.OperationStopped, computer);
                    this.WriteError(error);
                }
                catch (COMException ex)
                {
                    string errMsg = StringUtil.Format(ComputerResources.RestartComputerSkipped, computer, ex.Message);
                    var error = new ErrorRecord(new InvalidOperationException(errMsg), "RestartComputerSkipped",
                                                        ErrorCategory.OperationStopped, computer);
                    this.WriteError(error);
                }
                catch (UnauthorizedAccessException ex)
                {
                    string errMsg = StringUtil.Format(ComputerResources.RestartComputerSkipped, computer, ex.Message);
                    var error = new ErrorRecord(new InvalidOperationException(errMsg), "RestartComputerSkipped",
                                                        ErrorCategory.OperationStopped, computer);
                    this.WriteError(error);
                }
            }

            return validComputerNameList;
        }
#endif

        private List<string> SetUpComputerInfoUsingWsman(IEnumerable<string> computerNames, CancellationToken token)
        {
            var validComputerNameList = new List<string>();
            var operationOptions = new CimOperationOptions
            {
                Timeout = TimeSpan.FromMilliseconds(2000),
                CancellationToken = token
            };
            foreach (var computer in computerNames)
            {
                try
                {
                    using (CimSession cimSession = RemoteDiscoveryHelper.CreateCimSession(computer, Credential, WsmanAuthentication, token, this))
                    {
                        bool itemRetrieved = false;
                        IEnumerable<CimInstance> mCollection = cimSession.QueryInstances(
                                                                 ComputerWMIHelper.CimOperatingSystemNamespace,
                                                                 ComputerWMIHelper.CimQueryDialect,
                                                                 "Select * from " + ComputerWMIHelper.WMI_Class_OperatingSystem,
                                                                 operationOptions);
                        foreach (CimInstance os in mCollection)
                        {
                            itemRetrieved = true;
                            if (!_computerInfos.ContainsKey(computer))
                            {
                                var info = new ComputerInfo
                                {
                                    LastBootUpTime = os.CimInstanceProperties["LastBootUpTime"].Value.ToString(),
                                    RebootComplete = false
                                };
                                _computerInfos.Add(computer, info);
                                validComputerNameList.Add(computer);
                            }
                        }

                        if (!itemRetrieved)
                        {
                            string errMsg = StringUtil.Format(ComputerResources.RestartComputerSkipped, computer, ComputerResources.CannotGetOperatingSystemObject);
                            var error = new ErrorRecord(new InvalidOperationException(errMsg), "RestartComputerSkipped",
                                                            ErrorCategory.OperationStopped, computer);
                            this.WriteError(error);
                        }
                    }
                }
                catch (CimException ex)
                {
                    string errMsg = StringUtil.Format(ComputerResources.RestartComputerSkipped, computer, ex.Message);
                    var error = new ErrorRecord(new InvalidOperationException(errMsg), "RestartComputerSkipped",
                                                        ErrorCategory.OperationStopped, computer);
                    this.WriteError(error);
                }
                catch (Exception ex)
                {
                    CommandProcessorBase.CheckForSevereException(ex);
                    string errMsg = StringUtil.Format(ComputerResources.RestartComputerSkipped, computer, ex.Message);
                    var error = new ErrorRecord(new InvalidOperationException(errMsg), "RestartComputerSkipped",
                                                        ErrorCategory.OperationStopped, computer);
                    this.WriteError(error);
                }
            }

            return validComputerNameList;
        }

        private void WriteOutTimeoutError(IEnumerable<string> computerNames)
        {
            const string errorId = "RestartComputerTimeout";
            foreach (string computer in computerNames)
            {
                string errorMsg = StringUtil.Format(ComputerResources.RestartcomputerFailed, computer, ComputerResources.TimeoutError);
                var exception = new RestartComputerTimeoutException(computer, Timeout, errorMsg, errorId);
                var error = new ErrorRecord(exception, errorId, ErrorCategory.OperationTimeout, computer);
                WriteError(error);
            }
        }

#endregion "Private Methods"

#region "Internal Methods"

        internal static List<string> TestWmiConnectionUsingWsman(List<string> computerNames, List<string> nextTestList, CancellationToken token, PSCredential credential, string wsmanAuthentication, PSCmdlet cmdlet)
        {
            // Check if the WMI service "Winmgmt" is started
            const string wmiServiceQuery = "Select * from " + ComputerWMIHelper.WMI_Class_Service + " Where name = 'Winmgmt'";
            var wmiTestList = new List<string>();
            var operationOptions = new CimOperationOptions
            {
                Timeout = TimeSpan.FromMilliseconds(2000),
                CancellationToken = token
            };
            foreach (var computer in computerNames)
            {
                try
                {
                    if (token.IsCancellationRequested) { break; }
                    using (CimSession cimSession = RemoteDiscoveryHelper.CreateCimSession(computer, credential, wsmanAuthentication, token, cmdlet))
                    {
                        bool itemRetrieved = false;
                        IEnumerable<CimInstance> mCollection = cimSession.QueryInstances(
                                                                 ComputerWMIHelper.CimOperatingSystemNamespace,
                                                                 ComputerWMIHelper.CimQueryDialect,
                                                                 wmiServiceQuery,
                                                                 operationOptions);
                        foreach (CimInstance service in mCollection)
                        {
                            itemRetrieved = true;
                            if (LanguagePrimitives.IsTrue(service.CimInstanceProperties["Started"].Value))
                            {
                                nextTestList.Add(computer);
                            }
                            else
                            {
                                wmiTestList.Add(computer);
                            }
                        }

                        if (!itemRetrieved)
                        {
                            wmiTestList.Add(computer);
                        }
                    }
                }
                catch (CimException)
                {
                    wmiTestList.Add(computer);
                }
                catch (Exception ex)
                {
                    CommandProcessorBase.CheckForSevereException(ex);
                    wmiTestList.Add(computer);
                }
            }

            return wmiTestList;
        }

#if !CORECLR
        /// <summary>
        /// Test WinRM connectivity for the restarting machine
        /// </summary>
        /// <param name="computerNames"></param>
        /// <param name="nextTestList"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        internal static List<string> TestWinrmConnection(List<string> computerNames, List<string> nextTestList, CancellationToken token)
        {
            List<string> winrmTestList = new List<string>();
            IWSManEx wsmanObject = (IWSManEx)new WSManClass();
            int sessionFlags = (int)WSManSessionFlags.WSManFlagUseNoAuthentication | (int)WSManSessionFlags.WSManFlagUtf8;
            IWSManConnectionOptionsEx2 connObject = (IWSManConnectionOptionsEx2)wsmanObject.CreateConnectionOptions();

            foreach (string computerName in computerNames)
            {
                try
                {
                    if (token.IsCancellationRequested) { break; }
                    IWSManSession sessionObj = (IWSManSession)wsmanObject.CreateSession(computerName, sessionFlags, connObject);
                    if (token.IsCancellationRequested) { break; }
                    sessionObj.Timeout = 1500;
                    sessionObj.Identify(0);
                    nextTestList.Add(computerName);
                }
                catch (COMException)
                {
                    winrmTestList.Add(computerName);
                }
                catch (Exception ex)
                {
                    CommandProcessorBase.CheckForSevereException(ex);
                    winrmTestList.Add(computerName);
                }
            }
            return winrmTestList;
        }
#endif

        /// <summary>
        /// Test the PowerShell state for the restarting computer
        /// </summary>
        /// <param name="computerNames"></param>
        /// <param name="nextTestList"></param>
        /// <param name="powershell"></param>
        /// <param name="credential"></param>
        /// <returns></returns>
        internal static List<string> TestPowerShell(List<string> computerNames, List<string> nextTestList, System.Management.Automation.PowerShell powershell, PSCredential credential)
        {
            List<string> psList = new List<string>();

            try
            {
                Collection<PSObject> psObjectCollection = powershell.Invoke(new object[] { credential, computerNames.ToArray() });
                if (psObjectCollection == null)
                {
                    Dbg.Diagnostics.Assert(false, "This should never happen. Invoke should never return null.");
                }

                // If ^C or timeout happens when we are in powershell.Invoke(), the psObjectCollection might be empty 
                if (psObjectCollection.Count == 0)
                {
                    return computerNames;
                }

                object result = PSObject.Base(psObjectCollection[0]);
                Hashtable data = result as Hashtable;

                Dbg.Diagnostics.Assert(data != null, "data should never be null");
                Dbg.Diagnostics.Assert(data.Count == computerNames.Count, "data should contain results for all computers in computerNames");

                foreach (string computer in computerNames)
                {
                    if (LanguagePrimitives.IsTrue(data[computer]))
                    {
                        nextTestList.Add(computer);
                    }
                    else
                    {
                        psList.Add(computer);
                    }
                }
            }
            catch (PipelineStoppedException)
            {
                // powershell.Stop() is invoked because timeout expires, or Ctrl+C is pressed
            }
            catch (ObjectDisposedException)
            {
                // powershell.dispose() is invoked because timeout expires, or Ctrl+C is pressed
            }

            return psList;
        }

#if !CORECLR
        /// <summary>
        /// Restart one computer
        /// </summary>
        /// <param name="cmdlet"></param>
        /// <param name="isLocalhost"></param>
        /// <param name="computerName"></param>
        /// <param name="flags"></param>
        /// <param name="options"></param>
        /// <returns>
        /// True if the restart was successful
        /// False otherwise
        /// </returns>
        internal static bool RestartOneComputerUsingDcom(PSCmdlet cmdlet, bool isLocalhost, string computerName, object[] flags, ConnectionOptions options)
        {
            bool isSuccess = false;
            PlatformInvokes.TOKEN_PRIVILEGE currentPrivilegeState = new PlatformInvokes.TOKEN_PRIVILEGE();

            try
            {
                if (!(isLocalhost && PlatformInvokes.EnableTokenPrivilege(ComputerWMIHelper.SE_SHUTDOWN_NAME, ref currentPrivilegeState)) &&
                    !(!isLocalhost && PlatformInvokes.EnableTokenPrivilege(ComputerWMIHelper.SE_REMOTE_SHUTDOWN_NAME, ref currentPrivilegeState)))
                {
                    string message =
                        StringUtil.Format(ComputerResources.PrivilegeNotEnabled, computerName,
                            isLocalhost ? ComputerWMIHelper.SE_SHUTDOWN_NAME : ComputerWMIHelper.SE_REMOTE_SHUTDOWN_NAME);
                    ErrorRecord errorRecord = new ErrorRecord(new InvalidOperationException(message), "PrivilegeNotEnabled", ErrorCategory.InvalidOperation, null);
                    cmdlet.WriteError(errorRecord);
                    return false;
                }

                ManagementScope scope = new ManagementScope(ComputerWMIHelper.GetScopeString(isLocalhost ? "localhost" : computerName, ComputerWMIHelper.WMI_Path_CIM), options);
                EnumerationOptions enumOptions = new EnumerationOptions { UseAmendedQualifiers = true, DirectRead = true };
                ObjectQuery query = new ObjectQuery("select * from " + ComputerWMIHelper.WMI_Class_OperatingSystem);
                using (var searcher = new ManagementObjectSearcher(scope, query, enumOptions))
                {
                    foreach (ManagementObject operatingSystem in searcher.Get())
                    {
                        using (operatingSystem)
                        {
                            object result = operatingSystem.InvokeMethod("Win32shutdown", flags);
                            int retVal = Convert.ToInt32(result.ToString(), CultureInfo.CurrentCulture);
                            if (retVal != 0)
                            {
                                var ex = new Win32Exception(retVal);
                                string errMsg = StringUtil.Format(ComputerResources.RestartcomputerFailed, computerName, ex.Message);
                                ErrorRecord error = new ErrorRecord(
                                    new InvalidOperationException(errMsg), "RestartcomputerFailed", ErrorCategory.OperationStopped, computerName);
                                cmdlet.WriteError(error);
                            }
                            else
                            {
                                isSuccess = true;
                            }
                        }
                    }
                }
            }
            catch (ManagementException ex)
            {
                string errMsg = StringUtil.Format(ComputerResources.RestartcomputerFailed, computerName, ex.Message);
                ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "RestartcomputerFailed",
                                                    ErrorCategory.OperationStopped, computerName);
                cmdlet.WriteError(error);
            }
            catch (COMException ex)
            {
                string errMsg = StringUtil.Format(ComputerResources.RestartcomputerFailed, computerName, ex.Message);
                ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "RestartcomputerFailed",
                                                    ErrorCategory.OperationStopped, computerName);
                cmdlet.WriteError(error);
            }
            catch (UnauthorizedAccessException ex)
            {
                string errMsg = StringUtil.Format(ComputerResources.RestartcomputerFailed, computerName, ex.Message);
                ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "RestartcomputerFailed",
                                                    ErrorCategory.OperationStopped, computerName);
                cmdlet.WriteError(error);
            }
            finally
            {
                // Restore the previous privilege state if something unexpected happened
                PlatformInvokes.RestoreTokenPrivilege(
                    isLocalhost ? ComputerWMIHelper.SE_SHUTDOWN_NAME : ComputerWMIHelper.SE_REMOTE_SHUTDOWN_NAME, ref currentPrivilegeState);
            }

            return isSuccess;
        }
#endif

#endregion "Internal Methods"

#region "Overrides"

        /// <summary>
        /// BeginProcessing method.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (ParameterSetName.Equals(DefaultParameterSet, StringComparison.OrdinalIgnoreCase))
            {
                if (WsmanAuthentication != null && (_isDcomAuthenticationSpecified || _isImpersonationSpecified))
                {
                    string errorMsg = StringUtil.Format(ComputerResources.ParameterConfliction,
                                                        ComputerResources.ParameterUsage);
                    InvalidOperationException ex = new InvalidOperationException(errorMsg);
                    ThrowTerminatingError(new ErrorRecord(ex, "ParameterConfliction", ErrorCategory.InvalidOperation, null));
                }

                bool usingDcom = Protocol.Equals(ComputerWMIHelper.DcomProtocol, StringComparison.OrdinalIgnoreCase);
                bool usingWsman = Protocol.Equals(ComputerWMIHelper.WsmanProtocol, StringComparison.OrdinalIgnoreCase);

                if (_isProtocolSpecified && usingDcom && WsmanAuthentication != null)
                {
                    string errorMsg = StringUtil.Format(ComputerResources.InvalidParameterForDCOM,
                                                        ComputerResources.ParameterUsage);
                    InvalidOperationException ex = new InvalidOperationException(errorMsg);
                    ThrowTerminatingError(new ErrorRecord(ex, "InvalidParameterForDCOM", ErrorCategory.InvalidOperation, null));
                }

                if (_isProtocolSpecified && usingWsman && (_isDcomAuthenticationSpecified || _isImpersonationSpecified))
                {
                    string errorMsg = StringUtil.Format(ComputerResources.InvalidParameterForWSMan,
                                                        ComputerResources.ParameterUsage);
                    InvalidOperationException ex = new InvalidOperationException(errorMsg);
                    ThrowTerminatingError(new ErrorRecord(ex, "InvalidParameterForWSMan", ErrorCategory.InvalidOperation, null));
                }

                if (!_isProtocolSpecified && WsmanAuthentication != null)
                {
                    // Change the protocol to be WSMan if the WsmanAuthentication is specified
                    Protocol = ComputerWMIHelper.WsmanProtocol;
                }
            }

#if CORECLR
            if (this.MyInvocation.BoundParameters.ContainsKey("DcomAuthentication"))
            {
                string errMsg = StringUtil.Format(ComputerResources.InvalidParameterForCoreClr, "DcomAuthentication");
                PSArgumentException ex = new PSArgumentException(errMsg, ComputerResources.InvalidParameterForCoreClr);
                ThrowTerminatingError(new ErrorRecord(ex, "InvalidParameterForCoreClr", ErrorCategory.InvalidArgument, null));
            }

            if (this.MyInvocation.BoundParameters.ContainsKey("Impersonation"))
            {
                string errMsg = StringUtil.Format(ComputerResources.InvalidParameterForCoreClr, "Impersonation");
                PSArgumentException ex = new PSArgumentException(errMsg, ComputerResources.InvalidParameterForCoreClr);
                ThrowTerminatingError(new ErrorRecord(ex, "InvalidParameterForCoreClr", ErrorCategory.InvalidArgument, null));
            }

            // DCOM Authentication is not supported for CoreCLR. Throw an error
            // and request that the user specify WSMan Authentication.
            if (_isDcomAuthenticationSpecified || 
                Protocol.Equals(ComputerWMIHelper.DcomProtocol, StringComparison.OrdinalIgnoreCase))
            {
                InvalidOperationException ex = new InvalidOperationException(ComputerResources.InvalidParameterDCOMNotSupported);
                ThrowTerminatingError(new ErrorRecord(ex, "InvalidParameterDCOMNotSupported", ErrorCategory.InvalidOperation, null));
            }

            // TODO:CORECLR This should be re-visited if we decide to add double hop remoting to CoreCLR (outgoing connections)
            if (ParameterSetName.Equals(AsJobParameterSet, StringComparison.OrdinalIgnoreCase))
            {
                InvalidOperationException ex = new InvalidOperationException(ComputerResources.InvalidParameterSetAsJob);
                ThrowTerminatingError(new ErrorRecord(ex, "InvalidParameterSetAsJob", ErrorCategory.InvalidOperation, null));
            }
#endif

            // Timeout, For, Delay, Progress cannot be present if Wait is not present
            if ((_timeoutSpecified || _waitForSpecified || _delaySpecified) && !Wait)
            {
                InvalidOperationException ex = new InvalidOperationException(ComputerResources.RestartComputerInvalidParameter);
                ThrowTerminatingError(new ErrorRecord(ex, "RestartComputerInvalidParameter", ErrorCategory.InvalidOperation, null));
            }

            if (Wait)
            {
                _activityId = (new Random()).Next();
                if (_timeout == -1 || _timeout >= int.MaxValue / 1000)
                {
                    _timeoutInMilliseconds = int.MaxValue;
                }
                else
                {
                    _timeoutInMilliseconds = _timeout * 1000;
                }

                // We don't support combined service types for now
                switch (_waitFor)
                {
                    case WaitForServiceTypes.Wmi:
                    case WaitForServiceTypes.WinRM:
                        break;
                    case WaitForServiceTypes.PowerShell:
                        _powershell = System.Management.Automation.PowerShell.Create();
                        _powershell.AddScript(TestPowershellScript);
                        break;
                    default:
                        InvalidOperationException ex = new InvalidOperationException(ComputerResources.NoSupportForCombinedServiceType);
                        ErrorRecord error = new ErrorRecord(ex, "NoSupportForCombinedServiceType", ErrorCategory.InvalidOperation, (int)_waitFor);
                        ThrowTerminatingError(error);
                        break;
                }
            }
        }

        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        protected override void ProcessRecord()
        {
            // Validate parameters
            ValidateComputerNames();

            object[] flags = new object[] { 2, 0 };
            if (Force) flags[0] = 6;

#if CORECLR
            if (ParameterSetName.Equals(DefaultParameterSet, StringComparison.OrdinalIgnoreCase))
            {
#else // TODO:CORECLR Revisit if or when jobs are supported
            if (ParameterSetName.Equals(AsJobParameterSet, StringComparison.OrdinalIgnoreCase))
            {
                string[] names = _validatedComputerNames.ToArray();
                string strComputers = ComputerWMIHelper.GetMachineNames(names);
                if (!ShouldProcess(strComputers))
                    return;

                InvokeWmiMethod WmiInvokeCmd = new InvokeWmiMethod();
                WmiInvokeCmd.Path = ComputerWMIHelper.WMI_Class_OperatingSystem + "=@";
                WmiInvokeCmd.ComputerName = names;
                WmiInvokeCmd.Authentication = DcomAuthentication;
                WmiInvokeCmd.Impersonation = Impersonation;
                WmiInvokeCmd.Credential = Credential;
                WmiInvokeCmd.ThrottleLimit = ThrottleLimit;
                WmiInvokeCmd.Name = "Win32Shutdown";
                WmiInvokeCmd.EnableAllPrivileges = SwitchParameter.Present;
                WmiInvokeCmd.ArgumentList = flags;
                PSWmiJob wmiJob = new PSWmiJob(WmiInvokeCmd, names, ThrottleLimit, Job.GetCommandTextFromInvocationInfo(this.MyInvocation));
                this.JobRepository.Add(wmiJob);
                WriteObject(wmiJob);
            }
            else if (ParameterSetName.Equals(DefaultParameterSet, StringComparison.OrdinalIgnoreCase))
            {
                // CoreCLR does not support DCOM, so there is no point checking 
                // it here. It was already validated in BeginProcessing().

                bool dcomInUse = Protocol.Equals(ComputerWMIHelper.DcomProtocol, StringComparison.OrdinalIgnoreCase);
                ConnectionOptions options = ComputerWMIHelper.GetConnectionOptions(this.DcomAuthentication, this.Impersonation, this.Credential);
#endif
                if (Wait && _timeout != 0)
                {
                    _validatedComputerNames =
#if !CORECLR
                        dcomInUse ? SetUpComputerInfoUsingDcom(_validatedComputerNames, options) :
#endif
                        SetUpComputerInfoUsingWsman(_validatedComputerNames, _cancel.Token);
                }

                foreach (string computer in _validatedComputerNames)
                {
                    bool isLocal = false;
                    string compname;

                    if (computer.Equals("localhost", StringComparison.CurrentCultureIgnoreCase))
                    {
                        compname = _shortLocalMachineName;
                        isLocal = true;

#if !CORECLR
                        if (dcomInUse)
                        {
                            // The local machine will always at the end of the list. If the current target
                            // computer is the local machine, it's safe to set Username and Password to null.
                            options.Username = null;
                            options.SecurePassword = null;
                        }
#endif
                    }
                    else
                    {
                        compname = computer;
                    }

                    // Generate target and action strings
                    string action =
                        StringUtil.Format(
                            ComputerResources.RestartComputerAction,
                            isLocal ? ComputerResources.LocalShutdownPrivilege : ComputerResources.RemoteShutdownPrivilege);
                    string target =
                        isLocal ? StringUtil.Format(ComputerResources.DoubleComputerName, "localhost", compname) : compname;

                    if (!ShouldProcess(target, action))
                    {
                        continue;
                    }

                    bool isSuccess =
#if !CORECLR
                        dcomInUse ? RestartOneComputerUsingDcom(this, isLocal, compname, flags, options) :
#endif
                        ComputerWMIHelper.InvokeWin32ShutdownUsingWsman(this, isLocal, compname, flags, Credential, WsmanAuthentication, ComputerResources.RestartcomputerFailed, "RestartcomputerFailed", _cancel.Token);

                    if (isSuccess && Wait && _timeout != 0)
                    {
                        _waitOnComputers.Add(computer);
                    }
                }//end foreach

                if (_waitOnComputers.Count > 0)
                {
                    var restartStageTestList = new List<string>(_waitOnComputers);
                    var wmiTestList = new List<string>();
                    var winrmTestList = new List<string>();
                    var psTestList = new List<string>();
                    var allDoneList = new List<string>();

                    bool isForWmi = _waitFor.Equals(WaitForServiceTypes.Wmi);
                    bool isForWinRm = _waitFor.Equals(WaitForServiceTypes.WinRM);
                    bool isForPowershell = _waitFor.Equals(WaitForServiceTypes.PowerShell);

                    int indicatorIndex = 0;
                    int machineCompleteRestart = 0;
                    int actualDelay = SecondsToWaitForRestartToBegin;
                    bool first = true;
                    bool waitComplete = false;

                    _percent = 0;
                    _status = ComputerResources.WaitForRestartToBegin;
                    _activity = _waitOnComputers.Count == 1 ?
                        StringUtil.Format(ComputerResources.RestartSingleComputerActivity, _waitOnComputers[0]) :
                        ComputerResources.RestartMultipleComputersActivity;

                    _timer = new Timer(OnTimedEvent, null, _timeoutInMilliseconds, System.Threading.Timeout.Infinite);

                    while (true)
                    {
                        int loopCount = actualDelay * 4; // (delay * 1000)/250ms
                        while (loopCount > 0)
                        {
                            WriteProgress(_indicator[(indicatorIndex++) % 4] + _activity, _status, _percent, ProgressRecordType.Processing);

                            loopCount--;
                            _waitHandler.Wait(250);
                            if (_exit) { break; }
                        }

                        if (first)
                        {
                            actualDelay = _delay;
                            first = false;

                            if (_waitOnComputers.Count > 1)
                            {
                                _status = StringUtil.Format(ComputerResources.WaitForMultipleComputers, machineCompleteRestart, _waitOnComputers.Count);
                                WriteProgress(_indicator[(indicatorIndex++) % 4] + _activity, _status, _percent, ProgressRecordType.Processing);
                            }
                        }

                        do
                        {
                            // Test restart stage.
                            // We check if the target machine has already rebooted by querying the LastBootUpTime from the Win32_OperatingSystem object.
                            // So after this step, we are sure that both the Network and the WMI or WinRM service have already come up.
                            if (_exit) { break; }
                            if (restartStageTestList.Count > 0)
                            {
                                if (_waitOnComputers.Count == 1)
                                {
                                    _status = ComputerResources.VerifyRebootStage;
                                    _percent = CalculateProgressPercentage(StageVerification);
                                    WriteProgress(_indicator[(indicatorIndex++) % 4] + _activity, _status, _percent, ProgressRecordType.Processing);
                                }
                                List<string> nextTestList = (isForWmi || isForPowershell) ? wmiTestList : winrmTestList;
                                restartStageTestList =
#if !CORECLR
                                    dcomInUse ? TestRestartStageUsingDcom(restartStageTestList, nextTestList, _cancel.Token, options) :
#endif
                                    TestRestartStageUsingWsman(restartStageTestList, nextTestList, _cancel.Token);
                            }

                            // Test WMI service
                            if (_exit) { break; }
                            if (wmiTestList.Count > 0)
                            {
#if !CORECLR
                                if (dcomInUse)
                                {
                                    // CIM-DCOM is in use. In this case, restart stage checking is done by using WMIv1,
                                    // so the WMI service on the target machine is already up at this point.
                                    winrmTestList.AddRange(wmiTestList);
                                    wmiTestList.Clear();

                                    if (_waitOnComputers.Count == 1)
                                    {
                                        // This is to simulate the test for WMI service
                                        _status = ComputerResources.WaitForWMI;
                                        _percent = CalculateProgressPercentage(WmiConnectionTest);

                                        loopCount = actualDelay * 4; // (delay * 1000)/250ms
                                        while (loopCount > 0)
                                        {
                                            WriteProgress(_indicator[(indicatorIndex++) % 4] + _activity, _status, _percent, ProgressRecordType.Processing);

                                            loopCount--;
                                            _waitHandler.Wait(250);
                                            if (_exit) { break; }
                                        }
                                    }
                                }
                                else
#endif
                                // This statement block executes for both CLRs. 
                                // In the "full" CLR, it serves as the else case.
                                {
                                    if (_waitOnComputers.Count == 1)
                                    {
                                        _status = ComputerResources.WaitForWMI;
                                        _percent = CalculateProgressPercentage(WmiConnectionTest);
                                        WriteProgress(_indicator[(indicatorIndex++) % 4] + _activity, _status, _percent, ProgressRecordType.Processing);
                                    }
                                    wmiTestList = TestWmiConnectionUsingWsman(wmiTestList, winrmTestList, _cancel.Token, Credential, WsmanAuthentication, this);
                                }
                            }
                            if (isForWmi) { break; }

                            // Test WinRM service
                            if (_exit) { break; }
                            if (winrmTestList.Count > 0)
                            {
#if !CORECLR
                                if (dcomInUse)
                                {
                                    if (_waitOnComputers.Count == 1)
                                    {
                                        _status = ComputerResources.WaitForWinRM;
                                        _percent = CalculateProgressPercentage(WinrmConnectionTest);
                                        WriteProgress(_indicator[(indicatorIndex++) % 4] + _activity, _status, _percent, ProgressRecordType.Processing);
                                    }
                                    winrmTestList = TestWinrmConnection(winrmTestList, psTestList, _cancel.Token);
                                }
                                else
#endif
                                // This statement block executes for both CLRs. 
                                // In the "full" CLR, it serves as the else case.
                                {
                                    // CIM-WSMan in use. In this case, restart stage checking is done by using WMIv2,
                                    // so the WinRM service on the target machine is already up at this point.
                                    psTestList.AddRange(winrmTestList);
                                    winrmTestList.Clear();

                                    if (_waitOnComputers.Count == 1)
                                    {
                                        // This is to simulate the test for WinRM service
                                        _status = ComputerResources.WaitForWinRM;
                                        _percent = CalculateProgressPercentage(WinrmConnectionTest);

                                        loopCount = actualDelay * 4; // (delay * 1000)/250ms
                                        while (loopCount > 0)
                                        {
                                            WriteProgress(_indicator[(indicatorIndex++) % 4] + _activity, _status, _percent, ProgressRecordType.Processing);

                                            loopCount--;
                                            _waitHandler.Wait(250);
                                            if (_exit) { break; }
                                        }
                                    }
                                }
                            }
                            if (isForWinRm) { break; }

                            // Test PowerShell
                            if (_exit) { break; }
                            if (psTestList.Count > 0)
                            {
                                if (_waitOnComputers.Count == 1)
                                {
                                    _status = ComputerResources.WaitForPowerShell;
                                    _percent = CalculateProgressPercentage(PowerShellConnectionTest);
                                    WriteProgress(_indicator[(indicatorIndex++) % 4] + _activity, _status, _percent, ProgressRecordType.Processing);
                                }
                                psTestList = TestPowerShell(psTestList, allDoneList, _powershell, this.Credential);
                            }
                        } while (false);

                        // if time is up or Ctrl+c is typed, break out
                        if (_exit) { break; }

                        // Check if the restart completes
                        switch (_waitFor)
                        {
                            case WaitForServiceTypes.Wmi:
                                waitComplete = (winrmTestList.Count == _waitOnComputers.Count);
                                machineCompleteRestart = winrmTestList.Count;
                                break;
                            case WaitForServiceTypes.WinRM:
                                waitComplete = (psTestList.Count == _waitOnComputers.Count);
                                machineCompleteRestart = psTestList.Count;
                                break;
                            case WaitForServiceTypes.PowerShell:
                                waitComplete = (allDoneList.Count == _waitOnComputers.Count);
                                machineCompleteRestart = allDoneList.Count;
                                break;
                        }

                        // Wait is done or time is up
                        if (waitComplete || _exit)
                        {
                            if (waitComplete)
                            {
                                _status = ComputerResources.RestartComplete;
                                WriteProgress(_indicator[indicatorIndex % 4] + _activity, _status, 100, ProgressRecordType.Completed);
                                _timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                            }
                            break;
                        }

                        if (_waitOnComputers.Count > 1)
                        {
                            _status = StringUtil.Format(ComputerResources.WaitForMultipleComputers, machineCompleteRestart, _waitOnComputers.Count);
                            _percent = machineCompleteRestart * 100 / _waitOnComputers.Count;
                        }
                    }// end while(true)

                    if (_timeUp)
                    {
                        // The timeout expires. Write out timeout error messages for the computers that haven't finished restarting
                        do
                        {
                            if (restartStageTestList.Count > 0) { WriteOutTimeoutError(restartStageTestList); }
                            if (wmiTestList.Count > 0) { WriteOutTimeoutError(wmiTestList); }
                            // Wait for WMI. All computers that finished restarting are put in "winrmTestList"
                            if (isForWmi) { break; }

                            // Wait for WinRM. All computers that finished restarting are put in "psTestList"
                            if (winrmTestList.Count > 0) { WriteOutTimeoutError(winrmTestList); }
                            if (isForWinRm) { break; }

                            if (psTestList.Count > 0) { WriteOutTimeoutError(psTestList); }
                            // Wait for PowerShell. All computers that finished restarting are put in "allDoneList"
                        } while (false);
                    }
                }// end if(waitOnComputer.Count > 0)
            }//end DefaultParameter
        }//End Processrecord

        /// <summary>
        /// to implement ^C
        /// </summary>
        protected override void StopProcessing()
        {
            _exit = true;
            _cancel.Cancel();
            _waitHandler.Set();

            if (_timer != null)
            {
                _timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            }

            if (_powershell != null)
            {
                _powershell.Stop();
                _powershell.Dispose();
            }
        }

#endregion "Overrides"
    }

#endregion Restart-Computer

#region Stop-Computer

    /// <summary>
    /// cmdlet to stop computer
    /// </summary>
    [Cmdlet(VerbsLifecycle.Stop, "Computer", SupportsShouldProcess = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135263", RemotingCapability = RemotingCapability.SupportedByCommand)]
    public sealed class StopComputerCommand : PSCmdlet, IDisposable
    {
#region Private Members

#if !CORECLR
        private ManagementObjectSearcher _searcher;
#endif
        private readonly CancellationTokenSource _cancel = new CancellationTokenSource();
        private TransportProtocol _transportProtocol = TransportProtocol.DCOM;

#endregion

#region "Parameters"

        /// <summary>
        /// parameter
        /// </summary>
        [Parameter]
        public SwitchParameter AsJob { get; set; } = false;

        /// <summary>
        /// The following is the definition of the input parameter "DcomAuthentication".
        /// Specifies the authentication level to be used with WMI connection. Valid 
        /// values are:
        /// 
        /// Unchanged = -1,
        /// Default = 0,
        /// None = 1,
        /// Connect = 2,
        /// Call = 3,
        /// Packet = 4,
        /// PacketIntegrity = 5,
        /// PacketPrivacy = 6.
        /// </summary>
        [Parameter]
        [Alias("Authentication")]
        public AuthenticationLevel DcomAuthentication { get; set; } = AuthenticationLevel.Packet;

        /// <summary>
        /// The authentication options for CIM_WSMan connection
        /// </summary>
        [Parameter]
        [ValidateSet(
            "Default",
            "Basic",
            "Negotiate", // can be used with and without credential (without -> PSRP mapped to NegotiateWithImplicitCredential)
            "CredSSP",
            "Digest",
            "Kerberos")] // can be used with and without credential (not sure about implications)
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public string WsmanAuthentication { get; set; } = "Default";

        /// <summary>
        /// Specify the protocol to use
        /// </summary>
        [Parameter]
        [ValidateSet(ComputerWMIHelper.DcomProtocol, ComputerWMIHelper.WsmanProtocol)]
        public string Protocol { get; set; } = 
#if CORECLR
            //CoreClr does not support DCOM protocol
            // This change makes sure that the the command works seamlessly if user did not explicitly entered the protocol
            ComputerWMIHelper.WsmanProtocol;
#else
            ComputerWMIHelper.DcomProtocol;
#endif

        /// <summary>
        /// The following is the definition of the input parameter "ComputerName".
        /// Value of the address requested. The form of the value can be either the 
        /// computer name ("wxyz1234"), IPv4 address ("192.168.177.124"), or IPv6 
        /// address ("2010:836B:4179::836B:4179").
        /// </summary>
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [Alias("CN", "__SERVER", "Server", "IPAddress")]
        public String[] ComputerName { get; set; } = new string[] { "." };


        /// <summary>
        /// The following is the definition of the input parameter "Credential".
        /// Specifies a user account that has permission to perform this action. Type a 
        /// user-name, such as "User01" or "Domain01\User01", or enter a PSCredential 
        /// object, such as one from the Get-Credential cmdlet
        /// </summary>
        [Parameter(Position = 1)]
        [ValidateNotNullOrEmpty]
        [Credential]
        public PSCredential Credential { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "Impersonation".
        /// Specifies the impersonation level to use when calling the WMI method. Valid 
        /// values are: 
        /// 
        /// Default = 0,
        /// Anonymous = 1,
        /// Identify = 2,
        /// Impersonate = 3,
        /// Delegate = 4.
        /// </summary>
        [Parameter]
        public ImpersonationLevel Impersonation { get; set; } = ImpersonationLevel.Impersonate;

        /// <summary>
        /// The following is the definition of the input parameter "ThrottleLimit".
        /// The number of concurrent computers on which the command will be allowed to 
        /// execute
        /// </summary>
        [Parameter]
        [ValidateRange(int.MinValue, (int)1000)]
        public Int32 ThrottleLimit
        {
            get { return _throttlelimit; }
            set
            {
                _throttlelimit = value;
                if (_throttlelimit <= 0)
                    _throttlelimit = 32;
            }
        }
        private Int32 _throttlelimit = 32;

        /// <summary>
        /// The following is the definition of the input parameter "ThrottleLimit".
        /// The number of concurrent computers on which the command will be allowed to 
        /// execute
        /// </summary>
        [Parameter]
        public SwitchParameter Force { get; set; } = false;

        #endregion "parameters"

#region "IDisposable Members"

        /// <summary>
        /// Dispose Method
        /// </summary>
        public void Dispose()
        {
            try
            {
                _cancel.Dispose();
            }
            catch (ObjectDisposedException) { }
        }

#endregion "IDisposable Members"

#region "Overrides"

        /// <summary>
        /// BeginProcessing
        /// </summary>
        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            // Verify parameter set
            bool haveProtocolParam = this.MyInvocation.BoundParameters.ContainsKey("Protocol");
            bool haveWsmanAuthenticationParam = this.MyInvocation.BoundParameters.ContainsKey("WsmanAuthentication");
            bool haveDcomAuthenticationParam = this.MyInvocation.BoundParameters.ContainsKey("DcomAuthentication");
            bool haveDcomImpersonation = this.MyInvocation.BoundParameters.ContainsKey("Impersonation");
            _transportProtocol = (this.Protocol.Equals(ComputerWMIHelper.WsmanProtocol, StringComparison.OrdinalIgnoreCase) || (haveWsmanAuthenticationParam && !haveProtocolParam)) ?
                                 TransportProtocol.WSMan : TransportProtocol.DCOM;

            if (haveWsmanAuthenticationParam && (haveDcomAuthenticationParam || haveDcomImpersonation))
            {
                string errMsg = StringUtil.Format(ComputerResources.StopCommandParamWSManAuthConflict, ComputerResources.StopCommandParamMessage);
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSArgumentException(errMsg),
                        "InvalidParameter",
                        ErrorCategory.InvalidArgument,
                        this));
            }

            if ((_transportProtocol == TransportProtocol.DCOM) && haveWsmanAuthenticationParam)
            {
                string errMsg = StringUtil.Format(ComputerResources.StopCommandWSManAuthProtcolConflict, ComputerResources.StopCommandParamMessage);
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSArgumentException(errMsg),
                        "InvalidParameter",
                        ErrorCategory.InvalidArgument,
                        this));
            }

            if ((_transportProtocol == TransportProtocol.WSMan) && (haveDcomAuthenticationParam || haveDcomImpersonation))
            {
                string errMsg = StringUtil.Format(ComputerResources.StopCommandAuthProtcolConflict, ComputerResources.StopCommandParamMessage);
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSArgumentException(errMsg),
                        "InvalidParameter",
                        ErrorCategory.InvalidArgument,
                        this));
            }

#if CORECLR
            if (this.MyInvocation.BoundParameters.ContainsKey("DcomAuthentication"))
            {
                string errMsg = StringUtil.Format(ComputerResources.InvalidParameterForCoreClr, "DcomAuthentication");
                PSArgumentException ex = new PSArgumentException(errMsg, ComputerResources.InvalidParameterForCoreClr);
                ThrowTerminatingError(new ErrorRecord(ex, "InvalidParameterForCoreClr", ErrorCategory.InvalidArgument, null));
            }

            if (this.MyInvocation.BoundParameters.ContainsKey("Impersonation"))
            {
                string errMsg = StringUtil.Format(ComputerResources.InvalidParameterForCoreClr, "Impersonation");
                PSArgumentException ex = new PSArgumentException(errMsg, ComputerResources.InvalidParameterForCoreClr);
                ThrowTerminatingError(new ErrorRecord(ex, "InvalidParameterForCoreClr", ErrorCategory.InvalidArgument, null));
            }

            if(this.Protocol.Equals(ComputerWMIHelper.DcomProtocol , StringComparison.OrdinalIgnoreCase))
            {
                InvalidOperationException ex = new InvalidOperationException(ComputerResources.InvalidParameterDCOMNotSupported);
                ThrowTerminatingError(new ErrorRecord(ex, "InvalidParameterDCOMNotSupported", ErrorCategory.InvalidOperation, null));
            }
#endif
        }

        /// <summary>
        /// ProcessRecord
        /// </summary>
        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        protected override void ProcessRecord()
        {
            object[] flags = new object[] { 1, 0 };
            if (Force.IsPresent)
                flags[0] = 5;

            switch (_transportProtocol)
            {
#if !CORECLR
                case TransportProtocol.DCOM:
                    ProcessDCOMProtocol(flags);
                    break;
#endif

                case TransportProtocol.WSMan:
                    ProcessWSManProtocol(flags);
                    break;
            }
        }//End Processrecord

        /// <summary>
        /// to implement ^C
        /// </summary>
        protected override void StopProcessing()
        {
#if !CORECLR
            ManagementObjectSearcher searcher = _searcher;
            if (searcher != null)
            {
                try
                {
                    searcher.Dispose();
                }
                catch (ObjectDisposedException) { }
            }
#endif

            try
            {
                _cancel.Cancel();
            }
            catch (ObjectDisposedException) { }
            catch (AggregateException) { }
        }

#endregion "Overrides"

#region Private Methods

#if !CORECLR
        private void ProcessDCOMProtocol(object[] flags)
        {
            if (AsJob.IsPresent)
            {
                string strComputers = ComputerWMIHelper.GetMachineNames(ComputerName);
                if (!ShouldProcess(strComputers))
                    return;
                InvokeWmiMethod WmiInvokeCmd = new InvokeWmiMethod();
                WmiInvokeCmd.Path = ComputerWMIHelper.WMI_Class_OperatingSystem + "=@";
                WmiInvokeCmd.ComputerName = ComputerName;
                WmiInvokeCmd.Authentication = DcomAuthentication;
                WmiInvokeCmd.Impersonation = Impersonation;
                WmiInvokeCmd.Credential = Credential;
                WmiInvokeCmd.ThrottleLimit = _throttlelimit;
                WmiInvokeCmd.Name = "Win32Shutdown";
                WmiInvokeCmd.EnableAllPrivileges = SwitchParameter.Present;
                WmiInvokeCmd.ArgumentList = flags;
                PSWmiJob wmiJob = new PSWmiJob(WmiInvokeCmd, ComputerName, _throttlelimit, Job.GetCommandTextFromInvocationInfo(this.MyInvocation));
                this.JobRepository.Add(wmiJob);
                WriteObject(wmiJob);
            }
            else
            {
                string compname = string.Empty;
                string strLocal = string.Empty;

                foreach (string computer in ComputerName)
                {
                    if ((computer.Equals("localhost", StringComparison.CurrentCultureIgnoreCase)) || (computer.Equals(".", StringComparison.OrdinalIgnoreCase)))
                    {
                        compname = Dns.GetHostName();
                        strLocal = "localhost";
                    }
                    else
                    {
                        compname = computer;
                    }

                    if (!ShouldProcess(StringUtil.Format(ComputerResources.DoubleComputerName, strLocal, compname)))
                    {
                        continue;
                    }
                    else
                    {
                        try
                        {
                            ConnectionOptions options = ComputerWMIHelper.GetConnectionOptions(DcomAuthentication, this.Impersonation, this.Credential);
                            ManagementScope scope = new ManagementScope(ComputerWMIHelper.GetScopeString(computer, ComputerWMIHelper.WMI_Path_CIM), options);
                            EnumerationOptions enumOptions = new EnumerationOptions();
                            enumOptions.UseAmendedQualifiers = true;
                            enumOptions.DirectRead = true;
                            ObjectQuery query = new ObjectQuery("select * from " + ComputerWMIHelper.WMI_Class_OperatingSystem);
                            using (_searcher = new ManagementObjectSearcher(scope, query, enumOptions))
                            {
                                foreach (ManagementObject obj in _searcher.Get())
                                {
                                    using (obj)
                                    {
                                        object result = obj.InvokeMethod("Win32shutdown", flags);
                                        int retVal = Convert.ToInt32(result.ToString(), CultureInfo.CurrentCulture);
                                        if (retVal != 0)
                                        {
                                            ComputerWMIHelper.WriteNonTerminatingError(retVal, this, compname);
                                        }
                                    }
                                }
                            }
                            _searcher = null;
                        }
                        catch (ManagementException e)
                        {
                            ErrorRecord errorRecord = new ErrorRecord(e, "StopComputerException", ErrorCategory.InvalidOperation, compname);
                            WriteError(errorRecord);
                            continue;
                        }
                        catch (System.Runtime.InteropServices.COMException e)
                        {
                            ErrorRecord errorRecord = new ErrorRecord(e, "StopComputerException", ErrorCategory.InvalidOperation, compname);
                            WriteError(errorRecord);
                            continue;
                        }
                    }
                }
            }
        }
#endif

        private void ProcessWSManProtocol(object[] flags)
        {
            if (AsJob.IsPresent)
            {
                // TODO:  Need job for MI.Net WSMan protocol
                // Early return of job object.
                throw new PSNotSupportedException();
            }

            foreach (string computer in ComputerName)
            {
                string compname = string.Empty;
                string strLocal = string.Empty;
                bool isLocalHost = false;

                if (_cancel.Token.IsCancellationRequested) { break; }

                if ((computer.Equals("localhost", StringComparison.CurrentCultureIgnoreCase)) || (computer.Equals(".", StringComparison.OrdinalIgnoreCase)))
                {
                    compname = Dns.GetHostName();
                    strLocal = "localhost";
                    isLocalHost = true;
                }
                else
                {
                    compname = computer;
                }

                if (!ShouldProcess(StringUtil.Format(ComputerResources.DoubleComputerName, strLocal, compname)))
                {
                    continue;
                }
                else
                {
                    ComputerWMIHelper.InvokeWin32ShutdownUsingWsman(
                        this,
                        isLocalHost,
                        compname,
                        flags,
                        Credential,
                        WsmanAuthentication,
                        ComputerResources.StopcomputerFailed,
                        "StopComputerException",
                        _cancel.Token);
                }
            }
        }

#endregion
    }

#endregion

#if !CORECLR // TODO:CORECLR Enable once moved to MI .Net

#region Restore-Computer

    /// <summary>
    /// This cmdlet is to Restore Computer
    /// </summary>
    [Cmdlet(VerbsData.Restore, "Computer", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135254")]
    public sealed class RestoreComputerCommand : PSCmdlet, IDisposable
    {
#region Parameters
        /// <summary>
        /// Restorepoint parameter
        /// </summary>        

        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNull]
        [ValidateRangeAttribute((int)1, int.MaxValue)]
        [Alias("SequenceNumber", "SN", "RP")]
        public int RestorePoint { get; set; }

        #endregion

        private ManagementClass WMIClass;

#region "IDisposable Members"

        /// <summary>
        /// Dispose Method
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            // Use SuppressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose Method.
        /// </summary>
        /// <param name="disposing"></param>
        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (WMIClass != null)
                {
                    WMIClass.Dispose();
                }
            }
        }

#endregion "IDisposable Members"

#region overrides
        /// <summary>
        /// Restores the computer with 
        /// </summary>
        protected override void BeginProcessing()
        {
            // system restore APIs are not supported on ARM platform
            if (ComputerWMIHelper.SkipSystemRestoreOperationForARMPlatform(this))
            {
                return;
            }

            try
            {
                ConnectionOptions conn = ComputerWMIHelper.GetConnectionOptions(AuthenticationLevel.Packet, ImpersonationLevel.Impersonate, null);
                ManagementPath mPath = new ManagementPath();
                mPath.Path = ComputerWMIHelper.WMI_Path_Default;
                ManagementScope scope = new ManagementScope(mPath, conn);
                scope.Connect();
                WMIClass = new ManagementClass(ComputerWMIHelper.WMI_Class_SystemRestore);
                WMIClass.Scope = scope;

                //query to get the list of restore points
                ObjectQuery oquery = new ObjectQuery("select * from " + ComputerWMIHelper.WMI_Class_SystemRestore + " where SequenceNumber = " + RestorePoint);
                using (ManagementObjectSearcher R_results = new ManagementObjectSearcher(scope, oquery))
                {
                    //check if the entered restore point is a valid one
                    if (R_results.Get().Count == 0)
                    {
                        string message = StringUtil.Format(ComputerResources.InvalidRestorePoint, RestorePoint);
                        ArgumentException e = new ArgumentException(message);
                        ErrorRecord errorrecord = new ErrorRecord(e, "InvalidRestorePoint", ErrorCategory.InvalidArgument, null);
                        WriteError(errorrecord);
                        return;
                    }
                }
                //confirm with the user before restoring
                string computerName = Environment.MachineName;
                if (!ShouldProcess(computerName))
                {
                    return;
                }
                else
                {
                    //add the restorepoint parameter and invoke th emethod
                    Object[] arr = new Object[] { RestorePoint };
                    WMIClass.InvokeMethod("Restore", arr);
                    //Restore requires a Reboot and while reboot only the restore actually happens
                    //code to restart computer
                    mPath.Path = ComputerWMIHelper.WMI_Path_CIM;
                    scope.Path = mPath;
                    ManagementClass OsWMIClass = new ManagementClass(ComputerWMIHelper.WMI_Class_OperatingSystem);
                    OsWMIClass.Scope = scope;
                    ObjectQuery objQuery = new ObjectQuery("Select * from " + ComputerWMIHelper.WMI_Class_OperatingSystem);
                    using (ManagementObjectSearcher results = new ManagementObjectSearcher(scope, objQuery))
                    {
                        foreach (ManagementObject mobj in results.Get())
                        {
                            using (mobj)
                            {
                                string[] param = { "" };
                                mobj.InvokeMethod("Reboot", param);
                            }
                        }
                    }
                }
            }
            catch (ManagementException e)
            {
                if (e.ErrorCode.ToString().Equals("NotFound") || e.ErrorCode.ToString().Equals("InvalidClass"))
                {
                    Exception Ex = new ArgumentException(StringUtil.Format(ComputerResources.NotSupported));
                    WriteError(new ErrorRecord(Ex, "RestoreComputerNotSupported", ErrorCategory.InvalidOperation, null));
                }
                else
                {
                    ErrorRecord errorRecord = new ErrorRecord(e, "GetWMIManagementException", ErrorCategory.InvalidOperation, null);
                    WriteError(errorRecord);
                }
            }
            catch (COMException e)
            {
                if (string.IsNullOrEmpty(e.Message))
                {
                    Exception Ex = new ArgumentException(StringUtil.Format(ComputerResources.SystemRestoreServiceDisabled));
                    WriteError(new ErrorRecord(Ex, "ServiceDisabled", ErrorCategory.InvalidOperation, null));
                }
                else
                {
                    ErrorRecord errorRecord = new ErrorRecord(e, "COMException", ErrorCategory.InvalidOperation, null);
                    WriteError(errorRecord);
                }
            }
        }

        /// <summary>
        /// to implement ^C
        /// </summary>
        protected override void StopProcessing()
        {
            if (WMIClass != null)
            {
                WMIClass.Dispose();
            }
        }

#endregion overrides
    }
#endregion

#region Add-Computer

    /// <summary>
    /// Options for joining a computer to a domain
    /// </summary>
    [Flags]
    public enum JoinOptions
    {
        /// <summary>
        /// Create account on the domain
        /// </summary>
        AccountCreate = 0x2,

        /// <summary>
        /// Join operation is part of an upgrade
        /// </summary>
        Win9XUpgrade = 0x10,

        /// <summary>
        /// Perform an unsecure join
        /// </summary>
        UnsecuredJoin = 0x40,

        /// <summary>
        /// Indicate that the password passed to the join operation is the local machine account password, not a user password.
        /// It's valid only for unsecure join
        /// </summary>
        PasswordPass = 0x80,

        /// <summary>
        /// Writing SPN and DNSHostName attributes on the computer object should be deferred until the rename operation that 
        /// follows the join operation
        /// </summary>
        DeferSPNSet = 0x100,

        /// <summary>
        /// Join the target machine with a new name queried from the registry. This options is used if the rename has been called prior
        /// to rebooting the machine
        /// </summary>
        JoinWithNewName = 0x400,

        /// <summary>
        /// Use a readonly domain controller
        /// </summary>
        JoinReadOnly = 0x800,

        /// <summary>
        /// Invoke during install
        /// </summary>
        InstallInvoke = 0x40000
    }

    /// <summary>
    /// Adds the specified computer(s) to the Domain or Work Group. If the account 
    /// does not already exist on the domain, it also creates one (see notes for 
    /// implementation details).
    /// If the computer is already joined to a domain, it can be moved to a new 
    /// domain (see notes for implementation details).
    /// </summary>
    [SuppressMessage("Microsoft.PowerShell", "PS1004AcceptForceParameterWhenCallingShouldContinue")]
    [Cmdlet(VerbsCommon.Add, "Computer", SupportsShouldProcess = true, DefaultParameterSetName = DomainParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135194", RemotingCapability = RemotingCapability.SupportedByCommand)]
    [OutputType(typeof(ComputerChangeInfo))]
    public class AddComputerCommand : PSCmdlet
    {
#region parameter

        private const string DomainParameterSet = "Domain";
        private const string WorkgroupParameterSet = "Workgroup";

        /// <summary>
        /// Target computer names
        /// </summary>
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] ComputerName { get; set; } = { "localhost" };

        /// <summary>
        /// The local admin credential to the target computer
        /// </summary>
        [Parameter]
        [Credential]
        [ValidateNotNullOrEmpty]
        public PSCredential LocalCredential { get; set; }

        /// <summary>
        /// The domain credential used to unjoin a domain
        /// </summary>
        [Parameter(ParameterSetName = DomainParameterSet)]
        [Credential]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public PSCredential UnjoinDomainCredential { get; set; }

        /// <summary>
        /// The domain credential.
        /// In DomainParameterSet, it is for the domain to join to.
        /// In WorkgroupParameterSet, it is for the domain to disjoin from.
        /// </summary>
        [Parameter(ParameterSetName = DomainParameterSet, Mandatory = true)]
        [Parameter(ParameterSetName = WorkgroupParameterSet)]
        [Alias("DomainCredential")]
        [Credential]
        [ValidateNotNullOrEmpty]
        public PSCredential Credential { get; set; }

        /// <summary>
        /// Name of the domain to join
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = DomainParameterSet)]
        [Alias("DN", "Domain")]
        [ValidateNotNullOrEmpty]
        public String DomainName { get; set; }

        /// <summary>
        /// The organization unit (OU). It's the path on the AD under which the new account will
        /// be created
        /// </summary>
        [Parameter(ParameterSetName = DomainParameterSet)]
        [Alias("OU")]
        [ValidateNotNullOrEmpty]
        public string OUPath { get; set; }

        /// <summary>
        /// The name of a domain controller that performs the add.
        /// </summary>
        [Parameter(ParameterSetName = DomainParameterSet)]
        [Alias("DC")]
        [ValidateNotNullOrEmpty]
        public string Server { get; set; }

        /// <summary>
        /// Perform an unsecure join.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        [Parameter(ParameterSetName = DomainParameterSet)]
        public SwitchParameter Unsecure { get; set; }

        /// <summary>
        /// Additional options for the "join domain" operation
        /// </summary>
        [Parameter(ParameterSetName = DomainParameterSet)]
        public JoinOptions Options { get; set; } = JoinOptions.AccountCreate;

        /// <summary>
        /// Name of the workgroup to join in.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "WorkGroup")]
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = WorkgroupParameterSet)]
        [Alias("WGN")]
        [ValidateNotNullOrEmpty]
        public string WorkgroupName { get; set; }

        /// <summary>
        /// Restart the target computer
        /// </summary>
        [Parameter]
        public SwitchParameter Restart { get; set; } = false;

        /// <summary>
        /// Emit the output.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        /// <summary>
        /// New names for the target computers
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string NewName { get; set; }

        /// <summary>
        /// To suppress ShouldContinue
        /// </summary>
        [Parameter]
        public SwitchParameter Force
        {
            get { return _force; }
            set { _force = value; }
        }
        private bool _force;

        private int _joinDomainflags = 1;
        private bool _containsLocalHost = false;
        private string _newNameForLocalHost = null;

        private readonly string _shortLocalMachineName = Dns.GetHostName();
        private readonly string _fullLocalMachineName = Dns.GetHostEntry("").HostName;

#endregion parameter

#region private

        /// <summary>
        /// Unjoin the computer from its current domain
        /// <remarks>
        /// In the DomainParameterSet, the UnjoinDomainCredential is our first choice to unjoin a domain.
        /// But if the UnjoinDomainCredential is not specified, the DomainCredential will be our second 
        /// choice. This is to keep the backward compatibility. In Win7, we can do:
        ///      Add-Computer -DomainName domain1 -Credential $credForDomain1AndDomain2
        /// to switch the local machine that is currently in domain2 to domain1.
        /// 
        /// Since DomainCredential has an alias "Credential", the same command should still work for the
        /// new Add-Computer cmdlet.
        /// 
        /// In the WorkgroupParameterSet, the UnjoinDomainCredential is the only choice.
        /// </remarks>
        /// </summary>
        /// <param name="computerSystem"></param>
        /// <param name="computerName"></param>
        /// <param name="curDomainName"></param>
        /// <param name="dUserName"></param>
        /// <param name="dPassword"></param>
        /// <returns></returns>
        private int UnjoinDomain(ManagementObject computerSystem, string computerName, string curDomainName, string dUserName, string dPassword)
        {
            ManagementBaseObject unjoinDomainParameter = computerSystem.GetMethodParameters("UnjoinDomainOrWorkgroup");
            unjoinDomainParameter.SetPropertyValue("UserName", dUserName);
            unjoinDomainParameter.SetPropertyValue("Password", dPassword);
            unjoinDomainParameter.SetPropertyValue("FUnjoinOptions", 4); // default option, disable the active directory

            ManagementBaseObject result = computerSystem.InvokeMethod("UnjoinDomainOrWorkgroup", unjoinDomainParameter, null);
            Dbg.Diagnostics.Assert(result != null, "result cannot be null if the Unjoin method is invoked");
            int returnCode = Convert.ToInt32(result["ReturnValue"], CultureInfo.CurrentCulture);
            if (returnCode != 0)
            {
                var ex = new Win32Exception(returnCode);
                WriteErrorHelper(ComputerResources.FailToUnjoinDomain, "FailToUnjoinDomain", computerName,
                                 ErrorCategory.OperationStopped, false, computerName, curDomainName, ex.Message);
            }
            return returnCode;
        }

        /// <summary>
        /// Join a domain from a workgroup
        /// </summary>
        /// <remarks>
        /// If a computer is already in a domain, we first unjoin it from its current domain, and
        /// then do the join operation to the new domain. So when this method is invoked, we are 
        /// currently in a workgroup
        /// </remarks>
        /// <param name="computerSystem"></param>
        /// <param name="computerName"></param>
        /// <param name="oldDomainName"></param>
        /// <param name="curWorkgroupName"></param>
        /// <returns></returns>
        private int JoinDomain(ManagementObject computerSystem, string computerName, string oldDomainName, string curWorkgroupName)
        {
            string joinDomainUserName = Credential != null ? Credential.UserName : null;
            string joinDomainPassword = Credential != null ? Utils.GetStringFromSecureString(Credential.Password) : null;

            ManagementBaseObject joinDomainParameter = computerSystem.GetMethodParameters("JoinDomainOrWorkgroup");
            joinDomainParameter.SetPropertyValue("Name", DomainName);
            joinDomainParameter.SetPropertyValue("UserName", joinDomainUserName);
            joinDomainParameter.SetPropertyValue("Password", joinDomainPassword);
            joinDomainParameter.SetPropertyValue("AccountOU", OUPath);
            joinDomainParameter.SetPropertyValue("FJoinOptions", _joinDomainflags);

            ManagementBaseObject result = computerSystem.InvokeMethod("JoinDomainOrWorkgroup", joinDomainParameter, null);
            Dbg.Diagnostics.Assert(result != null, "result cannot be null if the Join method is invoked");
            int returnCode = Convert.ToInt32(result["ReturnValue"], CultureInfo.CurrentCulture);
            if (returnCode != 0)
            {
                var ex = new Win32Exception(returnCode);
                string errMsg;
                string errorId;
                if (oldDomainName != null)
                {
                    errMsg = StringUtil.Format(ComputerResources.FailToJoinNewDomainAfterUnjoinOldDomain,
                                                   computerName, oldDomainName, DomainName, ex.Message);
                    errorId = "FailToJoinNewDomainAfterUnjoinOldDomain";
                }
                else
                {
                    errMsg = StringUtil.Format(ComputerResources.FailToJoinDomainFromWorkgroup, computerName,
                                                   DomainName, curWorkgroupName, ex.Message);
                    errorId = "FailToJoinDomainFromWorkgroup";
                }

                WriteErrorHelper(errMsg, errorId, computerName, ErrorCategory.OperationStopped, false);
            }
            return returnCode;
        }

        /// <summary>
        /// Join in a new workgroup from the current workgroup
        /// </summary>
        /// <param name="computerSystem"></param>
        /// <param name="computerName"></param>
        /// <param name="oldDomainName"></param>
        /// <returns></returns>
        private int JoinWorkgroup(ManagementObject computerSystem, string computerName, string oldDomainName)
        {
            ManagementBaseObject joinWorkgroupParam = computerSystem.GetMethodParameters("JoinDomainOrWorkgroup");
            joinWorkgroupParam.SetPropertyValue("Name", WorkgroupName);
            joinWorkgroupParam.SetPropertyValue("UserName", null);
            joinWorkgroupParam.SetPropertyValue("Password", null);
            joinWorkgroupParam.SetPropertyValue("FJoinOptions", 0); // join a workgroup

            ManagementBaseObject result = computerSystem.InvokeMethod("JoinDomainOrWorkgroup", joinWorkgroupParam, null);
            Dbg.Diagnostics.Assert(result != null, "result cannot be null if the Join method is invoked");
            int returnCode = Convert.ToInt32(result["ReturnValue"], CultureInfo.CurrentCulture);
            if (returnCode != 0)
            {
                var ex = new Win32Exception(returnCode);
                string errMsg;
                if (oldDomainName != null)
                {
                    errMsg =
                        StringUtil.Format(ComputerResources.FailToSwitchFromDomainToWorkgroup, computerName,
                                          oldDomainName, WorkgroupName, ex.Message);
                }
                else
                {
                    errMsg = StringUtil.Format(ComputerResources.FailToJoinWorkGroup, computerName, WorkgroupName,
                                               ex.Message);
                }

                WriteErrorHelper(errMsg, "FailToJoinWorkGroup", computerName, ErrorCategory.OperationStopped, false);
            }
            return returnCode;
        }

        /// <summary>
        /// Rename the computer in workgroup
        /// </summary>
        /// <param name="computerSystem"></param>
        /// <param name="computerName"></param>
        /// <param name="newName"></param>
        /// <returns></returns>
        private int RenameComputer(ManagementObject computerSystem, string computerName, string newName)
        {
            string domainUserName = null;
            string domainPassword = null;

            if (DomainName != null && Credential != null)
            {
                // The rename operation happens after the computer is joined to the new domain, so we should provide
                // the domain user name and password to the rename operation
                domainUserName = Credential.UserName;
                domainPassword = Utils.GetStringFromSecureString(Credential.Password);
            }

            ManagementBaseObject renameParameter = computerSystem.GetMethodParameters("Rename");
            renameParameter.SetPropertyValue("Name", newName);
            renameParameter.SetPropertyValue("UserName", domainUserName);
            renameParameter.SetPropertyValue("Password", domainPassword);

            ManagementBaseObject result = computerSystem.InvokeMethod("Rename", renameParameter, null);
            Dbg.Diagnostics.Assert(result != null, "result cannot be null if the Rename method is invoked");
            int returnCode = Convert.ToInt32(result["ReturnValue"], CultureInfo.CurrentCulture);
            if (returnCode != 0)
            {
                var ex = new Win32Exception(returnCode);
                string errMsg;
                string errorId;
                if (WorkgroupName != null)
                {
                    errMsg = StringUtil.Format(ComputerResources.FailToRenameAfterJoinWorkgroup, computerName,
                                               WorkgroupName, newName, ex.Message);
                    errorId = "FailToRenameAfterJoinWorkgroup";
                }
                else
                {
                    errMsg = StringUtil.Format(ComputerResources.FailToRenameAfterJoinDomain, computerName, DomainName,
                                               newName, ex.Message);
                    errorId = "FailToRenameAfterJoinDomain";
                }

                WriteErrorHelper(errMsg, errorId, computerName, ErrorCategory.OperationStopped, false);
            }
            return returnCode;
        }

        /// <summary>
        /// Helper method to write out non-terminating errors
        /// </summary>
        /// <param name="resourceString"></param>
        /// <param name="errorId"></param>
        /// <param name="targetObj"></param>
        /// <param name="category"></param>
        /// <param name="terminating"></param>
        /// <param name="args"></param>
        private void WriteErrorHelper(string resourceString, string errorId, object targetObj, ErrorCategory category, bool terminating, params object[] args)
        {
            string errMsg;
            if (null == args || 0 == args.Length)
            {
                // Don't format in case the string contains literal curly braces
                errMsg = resourceString;
            }
            else
            {
                errMsg = StringUtil.Format(resourceString, args);
            }

            if (String.IsNullOrEmpty(errMsg))
            {
                Dbg.Diagnostics.Assert(false, "Could not load text for error record '" + errorId + "'");
            }

            ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), errorId,
                                                category, targetObj);
            if (terminating)
            {
                ThrowTerminatingError(error);
            }
            else
            {
                WriteError(error);
            }
        }

        private void DoAddComputerAction(string computer, string newName, bool isLocalhost, ConnectionOptions options, EnumerationOptions enumOptions, ObjectQuery computerSystemQuery)
        {
            int returnCode = 0;
            bool success = false;
            string computerName = isLocalhost ? _shortLocalMachineName : computer;

            if (ParameterSetName == DomainParameterSet)
            {
                string action = StringUtil.Format(ComputerResources.AddComputerActionDomain, DomainName);
                if (!ShouldProcess(computerName, action))
                {
                    return;
                }
            }
            else
            {
                string action = StringUtil.Format(ComputerResources.AddComputerActionWorkgroup, WorkgroupName);
                if (!ShouldProcess(computerName, action))
                {
                    return;
                }
            }

            // Check the length of the new name
            if (newName != null && newName.Length > ComputerWMIHelper.NetBIOSNameMaxLength)
            {
                string truncatedName = newName.Substring(0, ComputerWMIHelper.NetBIOSNameMaxLength);
                string query = StringUtil.Format(ComputerResources.TruncateNetBIOSName, truncatedName);
                string caption = ComputerResources.TruncateNetBIOSNameCaption;
                if (!Force && !ShouldContinue(query, caption))
                {
                    return;
                }
            }

            // If LocalCred is given, use the local credential for WMI connection. Otherwise, use 
            // the current caller's context (Username = null, Password = null)
            if (LocalCredential != null)
            {
                options.SecurePassword = LocalCredential.Password;
                options.Username = ComputerWMIHelper.GetLocalAdminUserName(computerName, LocalCredential);
            }

            // The local machine will always be processed in the very end. If the
            // current target computer is the local machine, it's the last one to
            // be processed, so we can safely set the Username and Password to be
            // null. We cannot use a user credential when connecting to the local
            // machine.
            if (isLocalhost)
            {
                options.Username = null;
                options.SecurePassword = null;
            }

            ManagementScope scope = new ManagementScope(ComputerWMIHelper.GetScopeString(computer, ComputerWMIHelper.WMI_Path_CIM), options);

            try
            {
                using (var searcher = new ManagementObjectSearcher(scope, computerSystemQuery, enumOptions))
                {
                    foreach (ManagementObject computerSystem in searcher.Get())
                    {
                        using (computerSystem)
                        {
                            // If we are not using the new computer name, check the length of the target machine name 
                            string hostName = (string)computerSystem["DNSHostName"];
                            if (newName == null && hostName.Length > ComputerWMIHelper.NetBIOSNameMaxLength)
                            {
                                string truncatedName = hostName.Substring(0, ComputerWMIHelper.NetBIOSNameMaxLength);
                                string query = StringUtil.Format(ComputerResources.TruncateNetBIOSName, truncatedName);
                                string caption = ComputerResources.TruncateNetBIOSNameCaption;
                                if (!Force && !ShouldContinue(query, caption))
                                {
                                    continue;
                                }
                            }

                            if (newName != null && hostName.Equals(newName, StringComparison.OrdinalIgnoreCase))
                            {
                                WriteErrorHelper(
                                    ComputerResources.NewNameIsOldName,
                                    "NewNameIsOldName",
                                    newName,
                                    ErrorCategory.InvalidArgument,
                                    false,
                                    computerName, newName);
                                continue;
                            }

                            if (ParameterSetName == DomainParameterSet)
                            {
                                if ((bool)computerSystem["PartOfDomain"])
                                {
                                    string curDomainName = (string)LanguagePrimitives.ConvertTo(computerSystem["Domain"], typeof(string), CultureInfo.InvariantCulture);
                                    string shortDomainName = "";
                                    if (curDomainName.Contains("."))
                                    {
                                        int dotIndex = curDomainName.IndexOf(".", StringComparison.OrdinalIgnoreCase);
                                        shortDomainName = curDomainName.Substring(0, dotIndex);
                                    }

                                    // If the target computer is already in the specified domain, throw an error
                                    if (curDomainName.Equals(DomainName, StringComparison.OrdinalIgnoreCase) || shortDomainName.Equals(DomainName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        WriteErrorHelper(ComputerResources.AddComputerToSameDomain,
                                                         "AddComputerToSameDomain", computerName,
                                                         ErrorCategory.InvalidOperation, false, computerName, DomainName);
                                        continue;
                                    }

                                    // Switch between domains
                                    // If the UnjoinDomainCredential is not specified, we assume the DomainCredential can be used for both removing
                                    // the computer from its current domain, and adding the computer to the new domain. This behavior is supported on
                                    // Win7, we don't want to break it.
                                    PSCredential credTobeUsed = UnjoinDomainCredential ?? Credential;
                                    string unjoinDomainUserName = credTobeUsed != null ? credTobeUsed.UserName : null;
                                    string unjoinDomainPassword = credTobeUsed != null ? Utils.GetStringFromSecureString(credTobeUsed.Password) : null;

                                    // Leave the current domain
                                    returnCode = UnjoinDomain(computerSystem, computerName, curDomainName, unjoinDomainUserName, unjoinDomainPassword);
                                    if (returnCode == 0)
                                    {
                                        // Successfully unjoin the old domain, join the computer to the new domain
                                        returnCode = JoinDomain(computerSystem, computerName, curDomainName, null);

                                        if (returnCode == 0 && newName != null)
                                        {
                                            // Rename the computer in the new domain
                                            returnCode = RenameComputer(computerSystem, computerName, newName);
                                        }
                                    }

                                    success = returnCode == 0;
                                }
                                else
                                {
                                    // Add a workgroup computer to domain
                                    string curWorkgroupName = (string)LanguagePrimitives.ConvertTo(computerSystem["Domain"], typeof(string), CultureInfo.InvariantCulture);
                                    returnCode = JoinDomain(computerSystem, computerName, null, curWorkgroupName);

                                    if (returnCode == 0 && newName != null)
                                    {
                                        returnCode = RenameComputer(computerSystem, computerName, newName);
                                    }

                                    success = returnCode == 0;
                                }
                            }
                            else // WorkgroupParameterSet
                            {
                                if ((bool)computerSystem["PartOfDomain"])
                                {
                                    // Remind the user to have local admin credential only if the computer is domain joined
                                    string shouldContinueMsg = ComputerResources.RemoveComputerConfirm;
                                    if (!Force && !ShouldContinue(shouldContinueMsg, null /* null = default caption */ ))
                                    {
                                        continue;
                                    }

                                    // Leave the current domain
                                    string curDomainName = (string)LanguagePrimitives.ConvertTo(computerSystem["Domain"], typeof(string), CultureInfo.InvariantCulture);
                                    string dUserName = Credential != null ? Credential.UserName : null;
                                    string dPassword = Credential != null ? Utils.GetStringFromSecureString(Credential.Password) : null;

                                    returnCode = UnjoinDomain(computerSystem, computerName, curDomainName, dUserName, dPassword);
                                    if (returnCode == 0)
                                    {
                                        // Join the specified workgroup
                                        returnCode = JoinWorkgroup(computerSystem, computerName, curDomainName);
                                        if (returnCode == 0 && newName != null)
                                        {
                                            // Rename the computer
                                            returnCode = RenameComputer(computerSystem, computerName, newName);
                                        }
                                    }

                                    success = returnCode == 0;
                                }
                                else // in workgroup
                                {
                                    string curWorkgroup = (string)LanguagePrimitives.ConvertTo(computerSystem["Domain"], typeof(string), CultureInfo.InvariantCulture);
                                    if (curWorkgroup.Equals(WorkgroupName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        WriteErrorHelper(ComputerResources.AddComputerToSameWorkgroup,
                                                         "AddComputerToSameWorkgroup", computerName,
                                                         ErrorCategory.InvalidOperation, false, computerName, WorkgroupName);
                                        continue;
                                    }

                                    // Join to another workgroup
                                    returnCode = JoinWorkgroup(computerSystem, computerName, null);
                                    if (returnCode == 0 && newName != null)
                                    {
                                        returnCode = RenameComputer(computerSystem, computerName, newName);
                                    }

                                    success = returnCode == 0;
                                }
                            }// end of else -- WorkgroupParameterSet

                            if (PassThru)
                            {
                                WriteObject(ComputerWMIHelper.GetComputerStatusObject(returnCode, computerName));
                            }
                        }
                    }// end of foreach

                    // If successful and the Restart parameter is specified, restart the computer
                    if (success && Restart)
                    {
                        object[] flags = new object[] { 6, 0 };
                        RestartComputerCommand.RestartOneComputerUsingDcom(this, isLocalhost, computerName, flags, options);
                    }

                    // If successful and the Restart parameter is not specified, write out warning
                    if (success && !Restart)
                    {
                        WriteWarning(StringUtil.Format(ComputerResources.RestartNeeded, null, computerName));
                    }
                }
            } // end of try
            catch (ManagementException ex)
            {
                WriteErrorHelper(ComputerResources.FailToConnectToComputer, "AddComputerException", computerName,
                                 ErrorCategory.OperationStopped, false, computerName, ex.Message);
            }
            catch (COMException ex)
            {
                WriteErrorHelper(ComputerResources.FailToConnectToComputer, "AddComputerException", computerName,
                                 ErrorCategory.OperationStopped, false, computerName, ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                WriteErrorHelper(ComputerResources.FailToConnectToComputer, "AddComputerException", computerName,
                                 ErrorCategory.OperationStopped, false, computerName, ex.Message);
            }
        }

        private string ValidateComputerName(string computer, bool validateNewName)
        {
            ErrorRecord error = null;
            string targetComputer = ComputerWMIHelper.ValidateComputerName(computer, _shortLocalMachineName, _fullLocalMachineName, ref error);
            if (targetComputer == null)
            {
                if (error != null)
                {
                    WriteError(error);
                }
                return null;
            }

            bool isLocalhost = targetComputer.Equals(ComputerWMIHelper.localhostStr, StringComparison.OrdinalIgnoreCase);

            if (validateNewName && NewName != null)
            {
                if (!ComputerWMIHelper.IsComputerNameValid(NewName))
                {
                    WriteErrorHelper(
                        ComputerResources.InvalidNewName,
                        "InvalidNewName",
                        NewName,
                        ErrorCategory.InvalidArgument,
                        false,
                        isLocalhost ? _shortLocalMachineName : targetComputer, NewName);

                    return null;
                }
            }

            return targetComputer;
        }

#endregion private

#region override

        /// <summary>
        /// BeginProcessing method
        /// </summary>
        protected override void BeginProcessing()
        {
            if (ParameterSetName == DomainParameterSet)
            {
                if ((Options & JoinOptions.PasswordPass) != 0 && (Options & JoinOptions.UnsecuredJoin) == 0)
                {
                    WriteErrorHelper(ComputerResources.InvalidJoinOptions, "InvalidJoinOptions", Options,
                                     ErrorCategory.InvalidArgument, true, JoinOptions.PasswordPass.ToString(),
                                     JoinOptions.UnsecuredJoin.ToString());
                }

                if ((Options & JoinOptions.AccountCreate) != 0)
                {
                    _joinDomainflags |= (int)JoinOptions.AccountCreate;
                }
                if ((Options & JoinOptions.Win9XUpgrade) != 0)
                {
                    _joinDomainflags |= (int)JoinOptions.Win9XUpgrade;
                }
                if ((Options & JoinOptions.UnsecuredJoin) != 0)
                {
                    _joinDomainflags |= (int)JoinOptions.UnsecuredJoin;
                }
                if ((Options & JoinOptions.PasswordPass) != 0)
                {
                    _joinDomainflags |= (int)JoinOptions.PasswordPass;
                }
                if ((Options & JoinOptions.DeferSPNSet) != 0)
                {
                    _joinDomainflags |= (int)JoinOptions.DeferSPNSet;
                }
                if ((Options & JoinOptions.JoinWithNewName) != 0)
                {
                    _joinDomainflags |= (int)JoinOptions.JoinWithNewName;
                }
                if ((Options & JoinOptions.JoinReadOnly) != 0)
                {
                    _joinDomainflags |= (int)JoinOptions.JoinReadOnly;
                }
                if ((Options & JoinOptions.InstallInvoke) != 0)
                {
                    _joinDomainflags |= (int)JoinOptions.InstallInvoke;
                }

                if (Unsecure)
                {
                    _joinDomainflags |= (int)(JoinOptions.UnsecuredJoin | JoinOptions.PasswordPass);
                }

                if (Server != null)
                {
                    // It's the name of a domain controller. We need to check if the specified domain controller actually exists
                    try
                    {
                        Dns.GetHostEntry(Server);
                    }
                    catch (Exception ex)
                    {
                        CommandsCommon.CheckForSevereException(this, ex);
                        WriteErrorHelper(ComputerResources.CannotResolveServerName, "AddressResolutionException",
                                         Server, ErrorCategory.InvalidArgument, true, Server);
                    }
                    DomainName = DomainName + "\\" + Server;
                }
            }
        }

        /// <summary>
        /// ProcessRecord method
        /// </summary>
        protected override void ProcessRecord()
        {
            if (NewName != null && ComputerName.Length != 1)
            {
                WriteErrorHelper(ComputerResources.CannotRenameMultipleComputers, "CannotRenameMultipleComputers",
                                 NewName, ErrorCategory.InvalidArgument, false);
                return;
            }

            // If LocalCred is not provided, we use the current caller's context
            ConnectionOptions options = new ConnectionOptions
            {
                Authentication = AuthenticationLevel.PacketPrivacy,
                Impersonation = ImpersonationLevel.Impersonate,
                EnablePrivileges = true
            };

            EnumerationOptions enumOptions = new EnumerationOptions { UseAmendedQualifiers = true, DirectRead = true };
            ObjectQuery computerSystemQuery = new ObjectQuery("select * from " + ComputerWMIHelper.WMI_Class_ComputerSystem);

            int oldJoinDomainFlags = _joinDomainflags;
            if (NewName != null && ParameterSetName == DomainParameterSet)
            {
                // We rename the computer after it's joined to the target domain, so writing SPN and DNSHostName attributes 
                // on the computer object should be deferred until the rename operation that follows the join operation
                _joinDomainflags |= (int)JoinOptions.DeferSPNSet;
            }

            try
            {
                foreach (string computer in ComputerName)
                {
                    string targetComputer = ValidateComputerName(computer, NewName != null);
                    if (targetComputer == null)
                    {
                        continue;
                    }

                    bool isLocalhost = targetComputer.Equals("localhost", StringComparison.OrdinalIgnoreCase);
                    if (isLocalhost)
                    {
                        if (!_containsLocalHost)
                            _containsLocalHost = true;
                        _newNameForLocalHost = NewName;

                        continue;
                    }

                    DoAddComputerAction(targetComputer, NewName, false, options, enumOptions, computerSystemQuery);
                }// end of foreach
            }
            finally
            {
                // Reverting the domainflags to previous status if DeferSPNSet is added to the domain flags.
                if (NewName != null && ParameterSetName == DomainParameterSet)
                {
                    _joinDomainflags = oldJoinDomainFlags;
                }
            }
        }// end of ProcessRecord

        /// <summary>
        /// EndProcessing method
        /// </summary>
        protected override void EndProcessing()
        {
            if (!_containsLocalHost) return;

            // If LocalCred is not provided, we use the current caller's context
            ConnectionOptions options = new ConnectionOptions
            {
                Authentication = AuthenticationLevel.PacketPrivacy,
                Impersonation = ImpersonationLevel.Impersonate,
                EnablePrivileges = true
            };

            EnumerationOptions enumOptions = new EnumerationOptions { UseAmendedQualifiers = true, DirectRead = true };
            ObjectQuery computerSystemQuery = new ObjectQuery("select * from " + ComputerWMIHelper.WMI_Class_ComputerSystem);

            DoAddComputerAction("localhost", _newNameForLocalHost, true, options, enumOptions, computerSystemQuery);
        }

#endregion override
    }//End Class

#endregion Add-Computer

#region RemoveComputer

    /// <summary>
    /// Removes the Specified Computer(s) from the relevant Domain or Work Group 
    /// </summary>

    [Cmdlet(VerbsCommon.Remove, "Computer", SupportsShouldProcess = true, DefaultParameterSetName = LocalParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135246", RemotingCapability = RemotingCapability.SupportedByCommand)]
    [OutputType(typeof(ComputerChangeInfo))]
    public class RemoveComputerCommand : PSCmdlet
    {
#region "Parameters and Private Data"

        private const string LocalParameterSet = "Local";
        private const string RemoteParameterSet = "Remote";

        /// <summary>
        /// The domain credential is used for authenticating to the domain controller.
        /// </summary>
        [Parameter(ParameterSetName = RemoteParameterSet, Mandatory = true)]
        [Parameter(Position = 0, ParameterSetName = LocalParameterSet)]
        [Alias("Credential")]
        [ValidateNotNullOrEmpty]
        [Credential]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public PSCredential UnjoinDomainCredential { get; set; }

        /// <summary>
        /// The local admin credential for authenticating to the target computer
        /// </summary>
        [Parameter(ParameterSetName = RemoteParameterSet)]
        [ValidateNotNullOrEmpty]
        [Credential]
        public PSCredential LocalCredential { get; set; }

        /// <summary>
        /// Restart parameter
        /// </summary>
        [Parameter]
        public SwitchParameter Restart { get; set; } = false;

        /// <summary>
        /// The target computer names to remove from the domain
        /// </summary>
        [Parameter(ParameterSetName = RemoteParameterSet, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] ComputerName { get; set; } = { "localhost" };

        /// <summary>
        /// Force parameter (to suppress the shouldprocess and shouldcontinue)
        /// </summary>
        [Parameter]
        public SwitchParameter Force
        {
            get { return _force; }
            set { _force = value; }
        }
        private bool _force;

        /// <summary>
        /// Only emit if passthru is specified. One bool/string pair for each 
        /// computer that was joined. Bool = success/failure. String = ComputerName.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        /// <summary>
        /// Specify the workgroup name to join in if the target machine is removed from
        /// the domain
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string WorkgroupName { get; set; } = "WORKGROUP";

        private bool _containsLocalHost = false;
        private readonly string _shortLocalMachineName = Dns.GetHostName();
        private readonly string _fullLocalMachineName = Dns.GetHostEntry("").HostName;

#endregion "Parameters and Private Data"

#region "Private Methods"

        private void DoRemoveComputerAction(string computer, bool isLocalhost, ConnectionOptions options, EnumerationOptions enumOptions, ObjectQuery computerSystemQuery)
        {
            bool successful = false;
            string computerName = isLocalhost ? _shortLocalMachineName : computer;

            if (!ShouldProcess(computerName))
            {
                return;
            }

            // If LocalCred is given, use the local credential for WMI connection
            if (LocalCredential != null)
            {
                options.SecurePassword = LocalCredential.Password;
                options.Username = ComputerWMIHelper.GetLocalAdminUserName(computerName, LocalCredential);
            }

            // The local machine will always be processed in the very end. If the
            // current target computer is the local machine, it's the last one to
            // be processed, so we can safely set the Username and Password to be
            // null. We cannot use a user credential when connecting to the local
            // machine.
            if (isLocalhost)
            {
                options.Username = null;
                options.SecurePassword = null;
            }

            ManagementScope scope = new ManagementScope(ComputerWMIHelper.GetScopeString(computer, ComputerWMIHelper.WMI_Path_CIM), options);
            try
            {
                using (var searcher = new ManagementObjectSearcher(scope, computerSystemQuery, enumOptions))
                {
                    foreach (ManagementObject computerSystem in searcher.Get())
                    {
                        using (computerSystem)
                        {
                            if (!(bool)computerSystem["PartOfDomain"])
                            {
                                // Not in a domain, throw out non-terminating error
                                string errMsg = StringUtil.Format(ComputerResources.ComputerNotInDomain, computerName);
                                ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "ComputerNotInDomain", ErrorCategory.InvalidOperation, computerName);
                                WriteError(error);
                                continue;
                            }

                            // Remind the user to have local admin credential only if the computer is domain joined
                            string shouldContinueMsg = ComputerResources.RemoveComputerConfirm;
                            if (!Force && !ShouldContinue(shouldContinueMsg, null /* null = default caption */ ))
                            {
                                continue;
                            }

                            int returnCode = 0;
                            string curDomainName = (string)LanguagePrimitives.ConvertTo(computerSystem["Domain"], typeof(string), CultureInfo.InvariantCulture);
                            string dUserName = UnjoinDomainCredential != null ? UnjoinDomainCredential.UserName : null;
                            string dPassword = UnjoinDomainCredential != null ? Utils.GetStringFromSecureString(UnjoinDomainCredential.Password) : null;

                            ManagementBaseObject unjoinParameter = computerSystem.GetMethodParameters("UnjoinDomainOrWorkgroup");
                            unjoinParameter.SetPropertyValue("UserName", dUserName);
                            unjoinParameter.SetPropertyValue("Password", dPassword);
                            unjoinParameter.SetPropertyValue("FUnjoinOptions", 4); // default option, disable the active directory account

                            ManagementBaseObject result = computerSystem.InvokeMethod("UnjoinDomainOrWorkgroup", unjoinParameter, null);
                            Dbg.Diagnostics.Assert(result != null, "result cannot be null if the Unjoin method is invoked");
                            returnCode = Convert.ToInt32(result["ReturnValue"], CultureInfo.CurrentCulture);

                            // Error code 1355 - The specified domain either does not exist or could not be contacted.
                            // This might happen when the old domain is gone or unreachable.
                            // Error code 53 - The network path was not found.
                            // This might happen when the network is not available.
                            if ((returnCode == 1355 || returnCode == 53) && Force)
                            {
                                // When -Force is specified, we unjoin the domain without disable the AD account
                                unjoinParameter.SetPropertyValue("FUnjoinOptions", 0);
                                result = computerSystem.InvokeMethod("UnjoinDomainOrWorkgroup", unjoinParameter, null);
                                Dbg.Diagnostics.Assert(result != null, "result cannot be null if the Unjoin method is invoked");
                                returnCode = Convert.ToInt32(result["ReturnValue"], CultureInfo.CurrentCulture);
                            }

                            if (returnCode != 0)
                            {
                                var ex = new Win32Exception(returnCode);
                                string errMsg = StringUtil.Format(ComputerResources.FailToUnjoinDomain, computerName, curDomainName, ex.Message);
                                ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "FailToUnjoinDomain", ErrorCategory.OperationStopped, computerName);
                                WriteError(error);
                            }
                            else
                            {
                                // Join into the specified workgroup if it's given
                                successful = true;
                                if (WorkgroupName != null)
                                {
                                    ManagementBaseObject joinParameter = computerSystem.GetMethodParameters("JoinDomainOrWorkgroup");
                                    joinParameter.SetPropertyValue("Name", WorkgroupName);
                                    joinParameter.SetPropertyValue("Password", null);
                                    joinParameter.SetPropertyValue("UserName", null);
                                    joinParameter.SetPropertyValue("FJoinOptions", 0); // Join in a workgroup

                                    result = computerSystem.InvokeMethod("JoinDomainOrWorkgroup", joinParameter, null);
                                    Dbg.Diagnostics.Assert(result != null, "result cannot be null if the Join method is invoked");
                                    returnCode = Convert.ToInt32(result["ReturnValue"], CultureInfo.CurrentCulture);
                                    if (returnCode != 0)
                                    {
                                        var ex = new Win32Exception(returnCode);
                                        string errMsg = StringUtil.Format(ComputerResources.FailToSwitchFromDomainToWorkgroup, computerName, curDomainName, WorkgroupName, ex.Message);
                                        ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "FailToJoinWorkGroup", ErrorCategory.OperationStopped, computerName);
                                        WriteError(error);
                                    }
                                }
                            }

                            if (PassThru)
                            {
                                WriteObject(ComputerWMIHelper.GetComputerStatusObject(returnCode, computerName));
                            }
                        }
                    }
                }

                // If successful and the Restart parameter is specified, restart the computer
                if (successful && Restart)
                {
                    object[] flags = new object[] { 6, 0 };
                    RestartComputerCommand.RestartOneComputerUsingDcom(this, isLocalhost, computerName, flags, options);
                }

                // If successful and the Restart parameter is not specified, write out warning
                if (successful && !Restart)
                {
                    WriteWarning(StringUtil.Format(ComputerResources.RestartNeeded, null, computerName));
                }
            }
            catch (ManagementException ex)
            {
                string errMsg = StringUtil.Format(ComputerResources.FailToConnectToComputer, computerName, ex.Message);
                ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "RemoveComputerException", ErrorCategory.OperationStopped, computerName);
                WriteError(error);
            }
            catch (COMException ex)
            {
                string errMsg = StringUtil.Format(ComputerResources.FailToConnectToComputer, computerName, ex.Message);
                ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "RemoveComputerException", ErrorCategory.OperationStopped, computerName);
                WriteError(error);
            }
            catch (UnauthorizedAccessException ex)
            {
                string errMsg = StringUtil.Format(ComputerResources.FailToConnectToComputer, computerName, ex.Message);
                ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "RemoveComputerException", ErrorCategory.OperationStopped, computerName);
                WriteError(error);
            }
        }

        private string ValidateComputerName(string computer)
        {
            ErrorRecord error = null;
            string targetComputer = ComputerWMIHelper.ValidateComputerName(computer, _shortLocalMachineName, _fullLocalMachineName, ref error);
            if (targetComputer == null)
            {
                if (error != null)
                {
                    WriteError(error);
                }
                return null;
            }

            return targetComputer;
        }

#endregion "Private Methods"

#region "Override Methods"

        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            // If both LocalCred and DomainCred are not provided, we use the default options
            ConnectionOptions options = new ConnectionOptions
            {
                Authentication = AuthenticationLevel.PacketPrivacy,
                Impersonation = ImpersonationLevel.Impersonate,
                EnablePrivileges = true
            };

            // For Remove-Computer, usually the domain credential is also the local admin credential
            // for the target computer. So use the local credential if given, otherwise use the domain
            // credential to connect to the target machine.
            // If the LocalCred is not given but the DomainCred is available, use the DomainCred for WMI connection.
            if (LocalCredential == null && UnjoinDomainCredential != null)
            {
                options.SecurePassword = UnjoinDomainCredential.Password;
                options.Username = UnjoinDomainCredential.UserName;
            }

            EnumerationOptions enumOptions = new EnumerationOptions { UseAmendedQualifiers = true, DirectRead = true };
            ObjectQuery computerSystemQuery = new ObjectQuery("select * from " + ComputerWMIHelper.WMI_Class_ComputerSystem);

            foreach (string computer in ComputerName)
            {
                string targetComputer = ValidateComputerName(computer);
                if (targetComputer == null)
                {
                    continue;
                }

                bool isLocalhost = targetComputer.Equals("localhost", StringComparison.OrdinalIgnoreCase);
                if (isLocalhost)
                {
                    if (!_containsLocalHost)
                        _containsLocalHost = true;

                    continue;
                }

                DoRemoveComputerAction(computer, false, options, enumOptions, computerSystemQuery);
            }
        }//End ProcessRecord()

        /// <summary>
        /// EndProcessing method: deal with the local computer in the end
        /// </summary>
        protected override void EndProcessing()
        {
            if (!_containsLocalHost) return;

            // If both LocalCred and DomainCred are not provided, we use the default options
            ConnectionOptions options = new ConnectionOptions
            {
                Authentication = AuthenticationLevel.PacketPrivacy,
                Impersonation = ImpersonationLevel.Impersonate,
                EnablePrivileges = true
            };

            // For Remove-Computer, usually the domain credential is also the local admin credential
            // for the target computer. So use the local credential if given, otherwise use the domain
            // credential to connect to the target machine.
            // If the LocalCred is not given but the DomainCred is available, use the DomainCred for WMI connection.
            if (LocalCredential == null && UnjoinDomainCredential != null)
            {
                options.SecurePassword = UnjoinDomainCredential.Password;
                options.Username = UnjoinDomainCredential.UserName;
            }

            EnumerationOptions enumOptions = new EnumerationOptions { UseAmendedQualifiers = true, DirectRead = true };
            ObjectQuery computerSystemQuery = new ObjectQuery("select * from " + ComputerWMIHelper.WMI_Class_ComputerSystem);

            DoRemoveComputerAction("localhost", true, options, enumOptions, computerSystemQuery);
        }

#endregion "Override Methods"
    }//End Class

#endregion Remove-Computer

#endif

#region Rename-Computer

    /// <summary>
    /// Renames a domain computer and its corresponding domain account or a 
    /// workgroup computer. Use this command to rename domain workstations and local 
    /// machines only. It cannot be used to rename Domain Controllers.
    /// </summary>

    [Cmdlet(VerbsCommon.Rename, "Computer", SupportsShouldProcess = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=219990", RemotingCapability = RemotingCapability.SupportedByCommand)]
    public class RenameComputerCommand : PSCmdlet
    {
#region Private Members

        private bool _containsLocalHost = false;
        private string _newNameForLocalHost = null;

        private TransportProtocol _transportProtocol = TransportProtocol.DCOM;

        private readonly string _shortLocalMachineName = Dns.GetHostName();
        private readonly string _fullLocalMachineName = Dns.GetHostEntryAsync("").Result.HostName;

#endregion

#region Parameters

        /// <summary>
        /// Target computers to rename
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string ComputerName { get; set; } = "localhost";

        /// <summary>
        /// Emit the output.
        /// </summary>
        //[Alias("Restart")]
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        /// <summary>
        /// The domain credential of the domain the target computer joined
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [Credential]
        public PSCredential DomainCredential { get; set; }

        /// <summary>
        /// The administrator credential of the target computer
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [Credential]
        public PSCredential LocalCredential { get; set; }

        /// <summary>
        /// New names for the target computers
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string NewName { get; set; }

        /// <summary>
        /// Suppress the ShouldContinue
        /// </summary>
        [Parameter]
        public SwitchParameter Force
        {
            get { return _force; }
            set { _force = value; }
        }
        private bool _force;

        /// <summary>
        /// To restart the target computer after rename it
        /// </summary>
        [Parameter]
        public SwitchParameter Restart
        {
            get { return _restart; }
            set { _restart = value; }
        }
        private bool _restart;

        /// <summary>
        /// The authentication options for CIM_WSMan connection
        /// </summary>
        [Parameter]
        [ValidateSet(
            "Default",
            "Basic",
            "Negotiate", // can be used with and without credential (without -> PSRP mapped to NegotiateWithImplicitCredential)
            "CredSSP",
            "Digest",
            "Kerberos")] // can be used with and without credential (not sure about implications)
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public string WsmanAuthentication { get; set; } = "Default";

        /// <summary>
        /// Specify the protocol to use
        /// </summary>
        [Parameter]
        [ValidateSet(ComputerWMIHelper.DcomProtocol, ComputerWMIHelper.WsmanProtocol)]
        public string Protocol { get; set; } =
#if CORECLR
            //CoreClr does not support DCOM protocol
            // This change makes sure that the the command works seamlessly if user did not explicitly entered the protocol
            ComputerWMIHelper.WsmanProtocol;
#else
             ComputerWMIHelper.DcomProtocol;
#endif

#endregion

#region "Private Methods"

        /// <summary>
        /// Check to see if the target computer is the local machine
        /// </summary>
        private string ValidateComputerName()
        {
            // Validate target name.
            ErrorRecord targetError = null;
            string targetComputer = ComputerWMIHelper.ValidateComputerName(ComputerName, _shortLocalMachineName, _fullLocalMachineName, ref targetError);
            if (targetComputer == null)
            {
                if (targetError != null)
                {
                    WriteError(targetError);
                }
                return null;
            }

            // Validate *new* name. Validate the format of the new name. Check if the old name is the same as the
            // new name later.
            if (!ComputerWMIHelper.IsComputerNameValid(NewName))
            {
                bool isLocalhost = targetComputer.Equals(ComputerWMIHelper.localhostStr, StringComparison.OrdinalIgnoreCase);
                string errMsg = StringUtil.Format(ComputerResources.InvalidNewName, isLocalhost ? _shortLocalMachineName : targetComputer, NewName);
                ErrorRecord error = new ErrorRecord(
                        new InvalidOperationException(errMsg), "InvalidNewName",
                        ErrorCategory.InvalidArgument, NewName);
                WriteError(error);
                return null;
            }

            return targetComputer;
        }

        private void DoRenameComputerAction(string computer, string newName, bool isLocalhost)
        {
            string computerName = isLocalhost ? _shortLocalMachineName : computer;

            if (!ShouldProcess(computerName))
            {
                return;
            }

            // Check the length of the new name
            if (newName != null && newName.Length > ComputerWMIHelper.NetBIOSNameMaxLength)
            {
                string truncatedName = newName.Substring(0, ComputerWMIHelper.NetBIOSNameMaxLength);
                string query = StringUtil.Format(ComputerResources.TruncateNetBIOSName, truncatedName);
                string caption = ComputerResources.TruncateNetBIOSNameCaption;
                if (!Force && !ShouldContinue(query, caption))
                {
                    return;
                }
            }

            switch (_transportProtocol)
            {
                case TransportProtocol.WSMan:
                    DoRenameComputerWsman(computer, computerName, newName, isLocalhost);
                    break;
#if !CORECLR
                case TransportProtocol.DCOM:
                    DoRenameComputerDcom(computer, computerName, newName, isLocalhost);
                    break;
#endif
            }
        }

        private void DoRenameComputerWsman(string computer, string computerName, string newName, bool isLocalhost)
        {
            bool successful = false;
            PSCredential credToUse = isLocalhost ? null : (LocalCredential ?? DomainCredential);

            try
            {
                using (CancellationTokenSource cancelTokenSource = new CancellationTokenSource())
                using (CimSession cimSession = RemoteDiscoveryHelper.CreateCimSession(computer, credToUse, WsmanAuthentication, cancelTokenSource.Token, this))
                {
                    var operationOptions = new CimOperationOptions
                    {
                        Timeout = TimeSpan.FromMilliseconds(10000),
                        CancellationToken = cancelTokenSource.Token,
                        //This prefix works against all versions of the WinRM server stack, both win8 and win7
                        ResourceUriPrefix = new Uri(ComputerWMIHelper.CimUriPrefix)
                    };

                    IEnumerable<CimInstance> mCollection = cimSession.QueryInstances(
                                                             ComputerWMIHelper.CimOperatingSystemNamespace,
                                                             ComputerWMIHelper.CimQueryDialect,
                                                             "Select * from " + ComputerWMIHelper.WMI_Class_ComputerSystem,
                                                             operationOptions);

                    foreach (CimInstance cimInstance in mCollection)
                    {
                        var oldName = cimInstance.CimInstanceProperties["DNSHostName"].Value.ToString();
                        if (oldName.Equals(newName, StringComparison.OrdinalIgnoreCase))
                        {
                            string errMsg = StringUtil.Format(ComputerResources.NewNameIsOldName, computerName, newName);
                            ErrorRecord error = new ErrorRecord(
                                    new InvalidOperationException(errMsg), "NewNameIsOldName",
                                    ErrorCategory.InvalidArgument, newName);
                            WriteError(error);

                            continue;
                        }

                        // If the target computer is in a domain, always use the DomainCred. If the DomainCred is not given,
                        // use null for UserName and Password, so the context of the caller will be used.
                        // If the target computer is not in a domain, just use null for the UserName and Password
                        string dUserName = null;
                        string dPassword = null;
                        if (((bool)cimInstance.CimInstanceProperties["PartOfDomain"].Value) && (DomainCredential != null))
                        {
                            dUserName = DomainCredential.UserName;
                            dPassword = Utils.GetStringFromSecureString(DomainCredential.Password);
                        }

                        var methodParameters = new CimMethodParametersCollection();
                        methodParameters.Add(CimMethodParameter.Create(
                            "Name",
                            newName,
                            Microsoft.Management.Infrastructure.CimType.String,
                            CimFlags.None));

                        methodParameters.Add(CimMethodParameter.Create(
                            "UserName",
                            dUserName,
                            Microsoft.Management.Infrastructure.CimType.String,
                            (dUserName == null) ? CimFlags.NullValue : CimFlags.None));

                        methodParameters.Add(
                            CimMethodParameter.Create(
                            "Password",
                            dPassword,
                            Microsoft.Management.Infrastructure.CimType.String,
                            (dPassword == null) ? CimFlags.NullValue : CimFlags.None));

                        CimMethodResult result = cimSession.InvokeMethod(
                            ComputerWMIHelper.CimOperatingSystemNamespace,
                            cimInstance,
                            "Rename",
                            methodParameters,
                            operationOptions);

                        int retVal = Convert.ToInt32(result.ReturnValue.Value, CultureInfo.CurrentCulture);
                        if (retVal != 0)
                        {
                            var ex = new Win32Exception(retVal);
                            string errMsg = StringUtil.Format(ComputerResources.FailToRename, computerName, newName, ex.Message);
                            ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "FailToRenameComputer", ErrorCategory.OperationStopped, computerName);
                            WriteError(error);
                        }
                        else
                        {
                            successful = true;
                        }

                        if (PassThru)
                        {
                            WriteObject(ComputerWMIHelper.GetRenameComputerStatusObject(retVal, newName, computerName));
                        }

                        if (successful)
                        {
                            if (_restart)
                            {
                                // If successful and the Restart parameter is specified, restart the computer
                                object[] flags = new object[] { 6, 0 };
                                ComputerWMIHelper.InvokeWin32ShutdownUsingWsman(
                                    this,
                                    isLocalhost,
                                    computerName,
                                    flags,
                                    credToUse,
                                    WsmanAuthentication,
                                    ComputerResources.RestartcomputerFailed,
                                    "RestartcomputerFailed",
                                    cancelTokenSource.Token);
                            }
                            else
                            {
                                WriteWarning(StringUtil.Format(ComputerResources.RestartNeeded, null, computerName));
                            }
                        }
                    } // end foreach
                } // end using
            }
            catch (CimException ex)
            {
                string errMsg = StringUtil.Format(ComputerResources.FailToConnectToComputer, computerName, ex.Message);
                ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "RenameComputerException",
                                                    ErrorCategory.OperationStopped, computerName);
                WriteError(error);
            }
            catch (Exception ex)
            {
                CommandProcessorBase.CheckForSevereException(ex);

                string errMsg = StringUtil.Format(ComputerResources.FailToConnectToComputer, computerName, ex.Message);
                ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "RenameComputerException",
                                                    ErrorCategory.OperationStopped, computerName);
                WriteError(error);
            }
        }

#if !CORECLR
        private void DoRenameComputerDcom(string computer, string computerName, string newName, bool isLocalhost)
        {
            EnumerationOptions enumOptions = new EnumerationOptions { UseAmendedQualifiers = true, DirectRead = true };
            ObjectQuery computerSystemQuery = new ObjectQuery("select * from " + ComputerWMIHelper.WMI_Class_ComputerSystem);

            // If both LocalCred and DomainCred are not provided, we use the default options
            ConnectionOptions options = new ConnectionOptions
            {
                Authentication = AuthenticationLevel.PacketPrivacy,
                Impersonation = ImpersonationLevel.Impersonate,
                EnablePrivileges = true
            };

            if (isLocalhost)
            {
                options.Username = null;
                options.SecurePassword = null;
            }
            // If the LocalCred is not given but the DomainCred is available, use the DomainCred for WMI connection.
            // If LocalCred is given, use the local credential for WMI connection
            else if (LocalCredential != null)
            {
                options.SecurePassword = LocalCredential.Password;
                options.Username = ComputerWMIHelper.GetLocalAdminUserName(computerName, LocalCredential);
            }
            else if (DomainCredential != null)
            {
                options.SecurePassword = DomainCredential.Password;
                options.Username = DomainCredential.UserName;
            }

            bool successful = false;
            ManagementScope scope = new ManagementScope(ComputerWMIHelper.GetScopeString(computer, ComputerWMIHelper.WMI_Path_CIM), options);
            try
            {
                using (var searcher = new ManagementObjectSearcher(scope, computerSystemQuery, enumOptions))
                {
                    foreach (ManagementObject computerSystem in searcher.Get())
                    {
                        using (computerSystem)
                        {
                            string oldName = (string)computerSystem["DNSHostName"];
                            if (oldName.Equals(newName, StringComparison.OrdinalIgnoreCase))
                            {
                                string errMsg = StringUtil.Format(ComputerResources.NewNameIsOldName, computerName, newName);
                                ErrorRecord error = new ErrorRecord(
                                        new InvalidOperationException(errMsg), "NewNameIsOldName",
                                        ErrorCategory.InvalidArgument, newName);
                                WriteError(error);
                                continue;
                            }

                            string dUserName = null;
                            string dPassword = null;
                            if ((bool)computerSystem["PartOfDomain"])
                            {
                                // If the target computer is in a domain, always use the DomainCred. If the DomainCred is not given,
                                // use null for UserName and Password, so the context of the caller will be used.
                                dUserName = DomainCredential != null ? DomainCredential.UserName : null;
                                dPassword = DomainCredential != null ? Utils.GetStringFromSecureString(DomainCredential.Password) : null;
                            }
                            // If the target computer is not in a domain, just use null for the UserName and Password

                            ManagementBaseObject renameParameter = computerSystem.GetMethodParameters("Rename");
                            renameParameter.SetPropertyValue("Name", newName);
                            renameParameter.SetPropertyValue("UserName", dUserName);
                            renameParameter.SetPropertyValue("Password", dPassword);

                            ManagementBaseObject result = computerSystem.InvokeMethod("Rename", renameParameter, null);
                            Dbg.Diagnostics.Assert(result != null, "result cannot be null if the Rename method is invoked");
                            int retVal = Convert.ToInt32(result["ReturnValue"], CultureInfo.CurrentCulture);
                            if (retVal != 0)
                            {
                                var ex = new Win32Exception(retVal);
                                string errMsg = StringUtil.Format(ComputerResources.FailToRename, computerName, newName, ex.Message);
                                ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "FailToRenameComputer", ErrorCategory.OperationStopped, computerName);
                                WriteError(error);
                            }
                            else
                            {
                                successful = true;
                            }

                            if (PassThru)
                            {
                                WriteObject(ComputerWMIHelper.GetRenameComputerStatusObject(retVal, newName, computerName));
                            }
                        }
                    }
                }

                // If successful and the Restart parameter is specified, restart the computer
                if (successful && _restart)
                {
                    object[] flags = new object[] { 6, 0 };
                    RestartComputerCommand.RestartOneComputerUsingDcom(this, isLocalhost, computerName, flags, options);
                }

                // If successful and the Restart parameter is not specified, write out warning
                if (successful && !_restart)
                {
                    WriteWarning(StringUtil.Format(ComputerResources.RestartNeeded, null, computerName));
                }
            }
            catch (ManagementException ex)
            {
                string errMsg = StringUtil.Format(ComputerResources.FailToConnectToComputer, computerName, ex.Message);
                ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "RenameComputerException",
                                                    ErrorCategory.OperationStopped, computerName);
                WriteError(error);
            }
            catch (COMException ex)
            {
                string errMsg = StringUtil.Format(ComputerResources.FailToConnectToComputer, computerName, ex.Message);
                ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "RenameComputerException",
                                                    ErrorCategory.OperationStopped, computerName);
                WriteError(error);
            }
            catch (UnauthorizedAccessException ex)
            {
                string errMsg = StringUtil.Format(ComputerResources.FailToConnectToComputer, computerName, ex.Message);
                ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "RenameComputerException",
                                                    ErrorCategory.OperationStopped, computerName);
                WriteError(error);
            }
        }
#endif

#endregion "Private Methods"

#region "Override Methods"

        /// <summary>
        /// Begin Processing
        /// </summary>
        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            bool haveWsmanAuthenticationParam = this.MyInvocation.BoundParameters.ContainsKey("WsmanAuthentication");
            bool haveProtocolParam = this.MyInvocation.BoundParameters.ContainsKey("Protocol");
            _transportProtocol = (this.Protocol.Equals(ComputerWMIHelper.WsmanProtocol, StringComparison.OrdinalIgnoreCase) || (haveWsmanAuthenticationParam && !haveProtocolParam)) ?
                                  TransportProtocol.WSMan : TransportProtocol.DCOM;

            // Verify parameter set
            if ((_transportProtocol == TransportProtocol.DCOM) && haveWsmanAuthenticationParam)
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSArgumentException(ComputerResources.RenameCommandWsmanAuthParamConflict),
                        "InvalidParameter",
                        ErrorCategory.InvalidArgument,
                        this));
            }

#if CORECLR
            // DCOM Authentication is not supported for CoreCLR. Throw an error
            // and request that the user specify WSMan Authentication.
            if (_transportProtocol == TransportProtocol.DCOM)
            {
                PSArgumentException ex = new PSArgumentException(ComputerResources.InvalidParameterDCOMNotSupported);
                ThrowTerminatingError(new ErrorRecord(ex, "InvalidParameterDCOMNotSupported", ErrorCategory.InvalidArgument, null));
            }
#endif
        }

        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            string targetComputer = ValidateComputerName();
            if (targetComputer == null) return;

            bool isLocalhost = targetComputer.Equals("localhost", StringComparison.OrdinalIgnoreCase);
            if (isLocalhost)
            {
                if (!_containsLocalHost)
                    _containsLocalHost = true;
                _newNameForLocalHost = NewName;

                return;
            }

            DoRenameComputerAction(targetComputer, NewName, false);
        }

        /// <summary>
        /// EndProcessing method
        /// </summary>
        protected override void EndProcessing()
        {
            if (!_containsLocalHost) return;

            DoRenameComputerAction("localhost", _newNameForLocalHost, true);
        }

#endregion "Override Methods"
    }

#endregion Rename-Computer

#if !CORECLR

#region Test-ComputerSecureChannel


    /// <summary>
    /// This cmdlet queries the status of trust relationships and will remove and 
    /// rebuild the trust if specified.
    /// </summary>

    [Cmdlet(VerbsDiagnostic.Test, "ComputerSecureChannel", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=137749")]
    [OutputType(typeof(Boolean))]
    public class TestComputerSecureChannelCommand : PSCmdlet
    {
#region Parameters

        /// <summary>
        /// Repair the secure channel between the local machine with the domain, if it's broken
        /// </summary>
        [Parameter]
        public SwitchParameter Repair { get; set; }

        /// <summary>
        /// The trusted domain controller to operate "Repair" on.
        /// </summary>
        [Parameter, ValidateNotNullOrEmpty]
        public string Server { get; set; }

        /// <summary>
        /// The domain credential for authenticating to the domain the local machine joined
        /// </summary>
        [Parameter, ValidateNotNullOrEmpty, Credential]
        public PSCredential Credential { get; set; }

        private const uint NETLOGON_CONTROL_REDISCOVER = 5;
        private const uint NETLOGON_CONTROL_TC_QUERY = 6;
        private const uint NETLOGON_INFO_2 = 2;

#endregion Parameters

#region "Private Methods"

        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", Justification = "Return results are used in debug asserts.")]
        private bool VerifySecureChannel(string domain, string localMachineName)
        {
            IntPtr queryInfo = IntPtr.Zero;
            IntPtr domainPtr = Marshal.StringToCoTaskMemAuto(domain);
            bool scInGoodState = false;

            try
            {
                int errorCode = SAMAPI.I_NetLogonControl2(null, NETLOGON_CONTROL_TC_QUERY, NETLOGON_INFO_2, ref domainPtr, out queryInfo);

                if (errorCode != 0)
                {
                    var ex = new Win32Exception(errorCode);
                    string errMsg = StringUtil.Format(ComputerResources.FailToTestSecureChannel, ex.Message);
                    ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "FailToTestSecureChannel",
                                                    ErrorCategory.OperationStopped, localMachineName);
                    ThrowTerminatingError(error);
                }

                var infoData = (SAMAPI.NetLogonInfo2)Marshal.PtrToStructure(queryInfo, typeof(SAMAPI.NetLogonInfo2));
                scInGoodState = infoData.PdcConnectionStatus == 0;
            }
            finally
            {
                if (domainPtr != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(domainPtr);
                }
                if (queryInfo != IntPtr.Zero)
                {
                    int freeResult = SAMAPI.NetApiBufferFree(queryInfo);
                    Dbg.Diagnostics.Assert(freeResult == 0, "NetApiBufferFree returned non-zero value");
                }
            }

            return scInGoodState;
        }

        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", Justification = "Return results are used in debug asserts.")]
        private bool ResetSecureChannel(string domain)
        {
            IntPtr queryInfo = IntPtr.Zero;
            IntPtr domainPtr = Marshal.StringToCoTaskMemAuto(domain);
            bool scInGoodState = false;

            try
            {
                int errorCode = SAMAPI.I_NetLogonControl2(null, NETLOGON_CONTROL_REDISCOVER, NETLOGON_INFO_2, ref domainPtr, out queryInfo);
                if (errorCode == 0)
                {
                    var infoData = (SAMAPI.NetLogonInfo2)Marshal.PtrToStructure(queryInfo, typeof(SAMAPI.NetLogonInfo2));
                    scInGoodState = infoData.TrustedDcName != null;
                }
            }
            finally
            {
                if (domainPtr != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(domainPtr);
                }
                if (queryInfo != IntPtr.Zero)
                {
                    int freeResult = SAMAPI.NetApiBufferFree(queryInfo);
                    Dbg.Diagnostics.Assert(freeResult == 0, "NetApiBufferFree returned non-zero value");
                }
            }

            return scInGoodState;
        }

#endregion "Private Methods"

#region "Override Methods"

        /// <summary>
        /// BeginProcessing method
        /// </summary>
        protected override void BeginProcessing()
        {
            if (Server != null)
            {
                if (Server.Length == 1 && Server[0] == '.')
                {
                    Server = "localhost";
                }

                try
                {
                    string resolveFullName = Dns.GetHostEntry(Server).HostName;
                }
                catch (Exception exception)
                {
                    CommandProcessorBase.CheckForSevereException(exception);

                    string errMsg = StringUtil.Format(ComputerResources.CannotResolveComputerName, Server, exception.Message);
                    ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "AddressResolutionException", ErrorCategory.InvalidArgument, Server);
                    ThrowTerminatingError(error);
                }
            }
        }


        /// <summary>
        /// ProcessRecord method.
        /// Suppress the message about NetApiBufferFree. The retuned results are
        /// actually used, but only in checked builds
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults")]
        protected override void ProcessRecord()
        {
            string localMachineName = Dns.GetHostName();
            string domain = null;
            Exception exception = null;

            if (!ShouldProcess(localMachineName))
            {
                return;
            }

            try
            {
                ManagementObject computerSystemInstance = new ManagementObject("Win32_ComputerSystem.Name=\"" + localMachineName + "\"");
                if (!(bool)computerSystemInstance["PartOfDomain"])
                {
                    string errMsg = ComputerResources.TestComputerNotInDomain;
                    ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "ComputerNotInDomain",
                                                        ErrorCategory.InvalidOperation, localMachineName);
                    ThrowTerminatingError(error);
                }
                domain = (string)LanguagePrimitives.ConvertTo(computerSystemInstance["Domain"], typeof(string), CultureInfo.InvariantCulture);
            }
            catch (ManagementException ex)
            {
                exception = ex;
            }
            catch (COMException ex)
            {
                exception = ex;
            }
            catch (UnauthorizedAccessException ex)
            {
                exception = ex;
            }
            if (exception != null)
            {
                string errMsg = StringUtil.Format(ComputerResources.FailToGetDomainInformation, exception.Message);
                ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "FailToGetDomainInformation",
                                                    ErrorCategory.OperationStopped, localMachineName);
                ThrowTerminatingError(error);
            }

            Dbg.Diagnostics.Assert(domain != null, "domain should not be null at this point");
            bool scInGoodState = false;
            string verboseMsg = null;
            if (Repair)
            {
                ResetComputerMachinePasswordCommand.
                    ResetMachineAccountPassword(domain, localMachineName, Server, Credential, this);

                scInGoodState = ResetSecureChannel(domain);
                verboseMsg = scInGoodState
                    ? StringUtil.Format(ComputerResources.RepairSecureChannelSucceed, domain)
                    : StringUtil.Format(ComputerResources.RepairSecureChannelFail, domain);
            }
            else
            {
                scInGoodState = VerifySecureChannel(domain, localMachineName);
                verboseMsg = scInGoodState
                    ? StringUtil.Format(ComputerResources.SecureChannelAlive, domain)
                    : StringUtil.Format(ComputerResources.SecureChannelBroken, domain);
            }

            WriteObject(scInGoodState);
            WriteVerbose(verboseMsg);
        }

#endregion "Override Methods"
    }//End Class


#endregion

#region Reset-ComputerMachinePassword
    /// <summary>
    /// Resets the computer machine password used to authenticate with DCs.
    /// </summary>

    [Cmdlet("Reset", "ComputerMachinePassword",
             SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135252")]
    public class ResetComputerMachinePasswordCommand : PSCmdlet
    {
#region "Parameter and PrivateData"

        /// <summary>
        /// The following is the definition of the input parameter "Server".
        /// Specifies the name of the domain controller to use for setting the machine 
        /// account password.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string Server { get; set; }

        /// <summary>
        /// The domain credential for authenticating to the domain the local machine joined
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [Credential]
        public PSCredential Credential { get; set; }

        private const uint STATUS_ACCESS_DENIED = 0xc0000022;
        private const uint STATUS_OBJECT_NAME_NOT_FOUND = 0xc0000034;

        private const uint SECRET_SET_VALUE = 0x00000001;
        private const uint SECRET_QUERY_VALUE = 0x00000002;

        // This number comes from the GenerateRandomPassword implementation of NetDom.exe.
        // There is a reason behind this number.
        private const int PasswordLength = 120;
        private const string SecretKey = "$MACHINE.ACC";

#endregion "Parameter and PrivateData"

#region "Private Methods"

        /// <summary>
        /// Throw out terminating error for LSA function invocations
        /// </summary>
        /// <param name="ret"></param>
        /// <param name="cmdlet"></param>
        private static void ThrowOutLsaError(uint ret, PSCmdlet cmdlet)
        {
            var ex = new Win32Exception(SAMAPI.LsaNtStatusToWinError((int)ret));
            string errMsg = StringUtil.Format(ComputerResources.FailToResetPasswordOnLocalMachine, ex.Message);
            ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "FailToResetPasswordOnLocalMachine",
                                                ErrorCategory.OperationStopped, Dns.GetHostName());
            cmdlet.ThrowTerminatingError(error);
        }

#endregion "Private Methods"

#region "Internal Methods"

        /// <summary>
        /// Reset machine account password
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="localMachineName"></param>
        /// <param name="server"></param>
        /// <param name="credential"></param>
        /// <param name="cmdlet"></param>
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", Justification = "Return results are used in debug asserts.")]
        internal static void ResetMachineAccountPassword(string domain, string localMachineName, string server, PSCredential credential, PSCmdlet cmdlet)
        {
            // Get domain directory entry and reset the password on the machine account of the local machine
            string newPassword = null;
            string domainOrServerName = server ?? domain;

            try
            {
                string dUserName = credential != null ? credential.UserName : null;
                string dPassword = credential != null ? Utils.GetStringFromSecureString(credential.Password) : null;

                using (var domainEntry = new DirectoryEntry(
                       "LDAP://" + domainOrServerName,
                       dUserName,
                       dPassword,
                       AuthenticationTypes.Secure))
                {
                    using (var searcher = new DirectorySearcher(domainEntry))
                    {
                        searcher.Filter = "(&(objectClass=computer)(|(cn=" + localMachineName + ")(dn=" + localMachineName + ")))";
                        SearchResult result = searcher.FindOne();

                        if (result == null)
                        {
                            string format = server != null
                                                ? ComputerResources.CannotFindMachineAccountFromServer
                                                : ComputerResources.CannotFindMachineAccountFromDomain;
                            string errMsg = StringUtil.Format(format, domainOrServerName);
                            ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "CannotFindMachineAccount",
                                                                ErrorCategory.OperationStopped, localMachineName);
                            cmdlet.ThrowTerminatingError(error);
                        }
                        else
                        {
                            // Generate a random password of length 120, and reset the password on the machine account
                            using (var targetEntry = result.GetDirectoryEntry())
                            {
                                newPassword = ComputerWMIHelper.GetRandomPassword(PasswordLength);
                                targetEntry.Invoke("SetPassword", new object[] { newPassword });
                                targetEntry.Properties["LockOutTime"].Value = 0;
                            }
                        }
                    }
                }
            }
            catch (DirectoryServicesCOMException ex)
            {
                string errMsg = StringUtil.Format(ComputerResources.FailToResetPasswordOnDomain, ex.Message);
                ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "FailToResetPasswordOnDomain",
                                                    ErrorCategory.OperationStopped, localMachineName);
                cmdlet.ThrowTerminatingError(error);
            }
            catch (TargetInvocationException ex)
            {
                string errMsg = StringUtil.Format(ComputerResources.FailToResetPasswordOnDomain, ex.InnerException.Message);
                ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "FailToResetPasswordOnDomain",
                                                    ErrorCategory.OperationStopped, localMachineName);
                cmdlet.ThrowTerminatingError(error);
            }
            catch (COMException ex)
            {
                string errMsg = StringUtil.Format(ComputerResources.FailToResetPasswordOnDomain, ex.Message);
                ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "FailToResetPasswordOnDomain",
                                                    ErrorCategory.OperationStopped, localMachineName);
                cmdlet.ThrowTerminatingError(error);
            }

            // Set the same password to the local machine
            Dbg.Diagnostics.Assert(newPassword != null, "the newPassword should not be null at this point");

            // A direct translation of function NetpManageMachineSecret2 in //depot/winmain/ds/netapi/netjoin/joinutl.c
            // Initialize the LSA_OBJECT_ATTRIBUTES
            var lsaAttr = new SAMAPI.LSA_OBJECT_ATTRIBUTES();
            lsaAttr.RootDirectory = IntPtr.Zero;
            lsaAttr.ObjectName = IntPtr.Zero;
            lsaAttr.Attributes = 0;
            lsaAttr.SecurityDescriptor = IntPtr.Zero;
            lsaAttr.SecurityQualityOfService = IntPtr.Zero;
            lsaAttr.Length = Marshal.SizeOf(typeof(SAMAPI.LSA_OBJECT_ATTRIBUTES));

            // Initialize the policy handle and secret handle
            IntPtr policyHandle = IntPtr.Zero;
            IntPtr secretHandle = IntPtr.Zero;

            // Initialize variables for LsaQuerySecret call
            IntPtr currentPassword = IntPtr.Zero;

            // Declare the key, newData and currentData
            var key = new SAMAPI.LSA_UNICODE_STRING { Buffer = IntPtr.Zero };
            var newData = new SAMAPI.LSA_UNICODE_STRING { Buffer = IntPtr.Zero };

            // Initialize the systemName for the localhost
            var localhost = new SAMAPI.LSA_UNICODE_STRING();
            localhost.Buffer = IntPtr.Zero;
            localhost.Length = 0;
            localhost.MaximumLength = 0;

            try
            {
                // Open the LSA policy
                uint ret = SAMAPI.LsaOpenPolicy(ref localhost, ref lsaAttr, (int)SAMAPI.LSA_ACCESS.AllAccess, out policyHandle);
                if (ret == STATUS_ACCESS_DENIED)
                {
                    string errMsg = ComputerResources.NeedAdminPrivilegeToResetPassword;
                    ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "UnauthorizedAccessException",
                                                        ErrorCategory.InvalidOperation, localMachineName);
                    cmdlet.ThrowTerminatingError(error);
                }
                if (ret != 0)
                {
                    ThrowOutLsaError(ret, cmdlet);
                }

                // Initialize secret key, new secret
                SAMAPI.InitLsaString(SecretKey, ref key);
                SAMAPI.InitLsaString(newPassword, ref newData);
                bool secretCreated = false;

                // Open the secret. If the secret is not found, create the secret
                ret = SAMAPI.LsaOpenSecret(policyHandle, ref key, SECRET_SET_VALUE | SECRET_QUERY_VALUE, out secretHandle);
                if (ret == STATUS_OBJECT_NAME_NOT_FOUND)
                {
                    ret = SAMAPI.LsaCreateSecret(policyHandle, ref key, SECRET_SET_VALUE, out secretHandle);
                    secretCreated = true;
                }
                if (ret != 0)
                {
                    ThrowOutLsaError(ret, cmdlet);
                }

                SAMAPI.LSA_UNICODE_STRING currentData;
                // Get the current password
                if (secretCreated)
                {
                    // Use the new password as the current one
                    currentData = newData;
                }
                else
                {
                    // Query for the current password
                    ret = SAMAPI.LsaQuerySecret(secretHandle, out currentPassword, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                    if (ret != 0)
                    {
                        ThrowOutLsaError(ret, cmdlet);
                    }

                    currentData = (SAMAPI.LSA_UNICODE_STRING)Marshal.PtrToStructure(currentPassword, typeof(SAMAPI.LSA_UNICODE_STRING));
                }

                ret = SAMAPI.LsaSetSecret(secretHandle, ref newData, ref currentData);
                if (ret != 0)
                {
                    ThrowOutLsaError(ret, cmdlet);
                }
            }
            finally
            {
                // Release pointers
                if (currentPassword != IntPtr.Zero)
                {
                    int releaseResult = SAMAPI.LsaFreeMemory(currentPassword);
                    Dbg.Diagnostics.Assert(releaseResult == 0, "LsaFreeMemory returned non-zero value");
                }

                // Release handles
                if (policyHandle != IntPtr.Zero)
                {
                    int releaseResult = SAMAPI.LsaClose(policyHandle);
                    Dbg.Diagnostics.Assert(releaseResult == 0, "LsaClose returned non-zero value");
                }

                if (secretHandle != IntPtr.Zero)
                {
                    int releaseResult = SAMAPI.LsaClose(secretHandle);
                    Dbg.Diagnostics.Assert(releaseResult == 0, "LsaClose returned non-zero value");
                }

                // Release LSA_UNICODE_STRING
                SAMAPI.FreeLsaString(ref key);
                SAMAPI.FreeLsaString(ref newData);
            }
        }

#endregion "Internal Methods"

#region "Override Methods"

        /// <summary>
        /// BeginProcessing method.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (Server != null)
            {
                if (Server.Length == 1 && Server[0] == '.')
                {
                    Server = "localhost";
                }

                try
                {
                    string resolveFullName = Dns.GetHostEntry(Server).HostName;
                }
                catch (Exception exception)
                {
                    CommandProcessorBase.CheckForSevereException(exception);

                    string errMsg = StringUtil.Format(ComputerResources.CannotResolveComputerName, Server, exception.Message);
                    ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "AddressResolutionException", ErrorCategory.InvalidArgument, Server);
                    ThrowTerminatingError(error);
                }
            }
        }//End BeginProcessing()

        /// <summary>
        /// ProcessRecord method
        /// Suppress the message about LsaFreeMemory and LsaClose. The retuned results are
        /// actually used, but only in checked builds
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults")]
        protected override void ProcessRecord()
        {
            // Not to use Environment.MachineName to avoid the injection attack
            string localMachineName = Dns.GetHostName();
            string domainName = null;
            Exception exception = null;

            if (!ShouldProcess(localMachineName))
            {
                return;
            }

            try
            {
                ManagementObject computerSystemInstance = new ManagementObject("Win32_ComputerSystem.Name=\"" + localMachineName + "\"");
                if (!(bool)computerSystemInstance["PartOfDomain"])
                {
                    string errMsg = ComputerResources.ResetComputerNotInDomain;
                    ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "ComputerNotInDomain",
                                                        ErrorCategory.InvalidOperation, localMachineName);
                    ThrowTerminatingError(error);
                }
                domainName = (string)LanguagePrimitives.ConvertTo(computerSystemInstance["Domain"], typeof(string), CultureInfo.InvariantCulture);
            }
            catch (ManagementException ex)
            {
                exception = ex;
            }
            catch (COMException ex)
            {
                exception = ex;
            }
            catch (UnauthorizedAccessException ex)
            {
                exception = ex;
            }
            if (exception != null)
            {
                string errMsg = StringUtil.Format(ComputerResources.FailToGetDomainInformation, exception.Message);
                ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "FailToGetDomainInformation",
                                                    ErrorCategory.OperationStopped, localMachineName);
                ThrowTerminatingError(error);
            }

            // Get domain directory entry and reset the password on the machine account of the local machine
            Dbg.Diagnostics.Assert(domainName != null, "domainOrServerName should not be null at this point");
            ResetMachineAccountPassword(domainName, localMachineName, Server, Credential, this);
        }

#endregion "Override Methods"
    }//End Class

#endregion Reset-ComputerMachinePassword

#region SAMCmdletsHelper

    /// <summary>
    /// the static class for calling the the NetJoinDomain function.
    /// </summary>
    internal static class SAMAPI
    {
        /// <summary>
        /// Structure for the LSA unicode string
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct LSA_UNICODE_STRING
        {
            internal ushort Length;
            internal ushort MaximumLength;
            [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
            internal IntPtr Buffer;
        }

        /// <summary>
        /// Structure for the LSA object attributes
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct LSA_OBJECT_ATTRIBUTES
        {
            internal int Length;
            internal IntPtr RootDirectory;
            internal IntPtr ObjectName;
            internal int Attributes;
            internal IntPtr SecurityDescriptor;
            internal IntPtr SecurityQualityOfService;
        }

        /// <summary>
        /// The LSA access mask
        /// </summary>
        internal enum LSA_ACCESS
        {
            Read = 0x20006,
            AllAccess = 0x00F0FFF,
            Execute = 0X20801,
            Write = 0X207F8
        }

        /// <summary>
        /// LsaOpenPolicy function
        /// </summary>
        /// <param name="systemName"></param>
        /// <param name="objectAttributes"></param>
        /// <param name="desiredAccess"></param>
        /// <param name="policyHandle"></param>
        /// <returns></returns>
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern uint LsaOpenPolicy(
           ref LSA_UNICODE_STRING systemName,
           ref LSA_OBJECT_ATTRIBUTES objectAttributes,
           uint desiredAccess,
           out IntPtr policyHandle);

        /// <summary>
        /// LsaOpenSecret function
        /// </summary>
        /// <param name="policyHandle"></param>
        /// <param name="secretName"></param>
        /// <param name="accessMask"></param>
        /// <param name="secretHandle"></param>
        /// <returns></returns>
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern uint LsaOpenSecret(
            IntPtr policyHandle,
            ref LSA_UNICODE_STRING secretName,
            uint accessMask,
            out IntPtr secretHandle);

        /// <summary>
        /// LsaCreateSecret function
        /// </summary>
        /// <param name="policyHandle"></param>
        /// <param name="secretName"></param>
        /// <param name="desiredAccess"></param>
        /// <param name="secretHandle"></param>
        /// <returns></returns>
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern uint LsaCreateSecret(
            IntPtr policyHandle,
            ref LSA_UNICODE_STRING secretName,
            uint desiredAccess,
            out IntPtr secretHandle);

        /// <summary>
        /// LsaQuerySecret function
        /// </summary>
        /// <param name="secretHandle"></param>
        /// <param name="currentValue"></param>
        /// <param name="currentValueSetTime"></param>
        /// <param name="oldValue"></param>
        /// <param name="oldValueSetTime"></param>
        /// <returns></returns>
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern uint LsaQuerySecret(
            IntPtr secretHandle,
            out IntPtr currentValue,
            IntPtr currentValueSetTime,
            IntPtr oldValue,
            IntPtr oldValueSetTime);

        /// <summary>
        /// LsaSetSecret function
        /// </summary>
        /// <param name="secretHandle"></param>
        /// <param name="currentValue"></param>
        /// <param name="oldValue"></param>
        /// <returns></returns>
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern uint LsaSetSecret(
            IntPtr secretHandle,
            ref LSA_UNICODE_STRING currentValue,
            ref LSA_UNICODE_STRING oldValue);

        /// <summary>
        /// LsaNtStatusToWinError function
        /// </summary>
        /// <param name="ntStatus"></param>
        /// <returns></returns>
        [DllImport("advapi32")]
        internal static extern int LsaNtStatusToWinError(int ntStatus);

        /// <summary>
        /// LsaClose function
        /// </summary>
        /// <param name="policyHandle"></param>
        /// <returns></returns>
        [DllImport("advapi32")]
        internal static extern int LsaClose(IntPtr policyHandle);

        /// <summary>
        /// LsaFreeMemory function
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        [DllImport("advapi32")]
        internal static extern int LsaFreeMemory(IntPtr buffer);

        /// <summary>
        /// Initialize a LSA_UNICODE_STRING
        /// </summary>
        /// <param name="s"></param>
        /// <param name="lus"></param>
        /// <returns></returns>
        internal static void InitLsaString(string s, ref LSA_UNICODE_STRING lus)
        {
            // Unicode strings max 32KB. The max value for MaximumLength should be ushort.MaxValue-1
            // because UnicodeEncoding.CharSize is 2. So the length of the string s should not be larger
            // than (ushort.MaxValue - 1)/UnicodeEncoding.CharSize - 1, which is 0x7ffe (32766)
            ushort maxLength = (ushort.MaxValue - 1) / UnicodeEncoding.CharSize - 1;
            if (s.Length > maxLength)
                throw new ArgumentException("String too long");
            lus.Buffer = Marshal.StringToHGlobalUni(s);
            lus.Length = (ushort)(s.Length * UnicodeEncoding.CharSize);
            lus.MaximumLength = (ushort)((s.Length + 1) * UnicodeEncoding.CharSize);
        }

        /// <summary>
        /// Free the LSA_UNICODE_STRING
        /// </summary>
        /// <param name="s"></param>
        internal static void FreeLsaString(ref LSA_UNICODE_STRING s)
        {
            if (s.Buffer == IntPtr.Zero) return;

            Marshal.FreeHGlobal(s.Buffer);
            s.Buffer = IntPtr.Zero;
        }

        /// <summary>
        /// The NETLOGON_INFO_2 struct used for function I_NetLogonControl2
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct NetLogonInfo2
        {
            internal uint Flags;
            /// <summary>
            /// Secure channel status with the primary domain controller
            /// </summary>
            internal uint PdcConnectionStatus;
            /// <summary>
            /// Name of the trusted domain controller
            /// </summary>
            internal string TrustedDcName;
            /// <summary>
            /// Secure channel status with the specified trusted domain controller
            /// </summary>
            internal uint TdcConnectionStatus;
        }

        /// <summary>
        /// To Reset a password for a computer in domain.
        /// </summary>
        [DllImport("netapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern int I_NetLogonControl2(
            [In] string lpServerName,
            uint lpFunctionCode,
            uint lpQueryLevel,
            ref IntPtr lpInputData,
            out IntPtr queryInformation);


        [DllImport("Netapi32.dll", SetLastError = true)]
        internal static extern int NetApiBufferFree(IntPtr Buffer);

        internal const int WorkGroupMachine = 2692;
        internal const int MaxMachineNameLength = 15;
    }

#endregion

#endif

#region "Public API"
    /// <summary>
    /// The object returned by SAM Computer cmdlets representing the status of the target machine.
    /// </summary>
    public sealed class ComputerChangeInfo
    {
        private const string MatchFormat = "{0}:{1}";

        /// <summary>
        /// The HasSucceeded which shows the operation was success or not
        /// </summary>
        public bool HasSucceeded { get; set; }

        /// <summary>
        /// The ComputerName on which the operation is done.
        /// </summary>
        public string ComputerName { get; set; }

        /// <summary>
        /// Returns the string representation of this object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return FormatLine(this.HasSucceeded.ToString(), this.ComputerName);
        }

        /// <summary>
        /// Formats a line for use in ToString.
        /// </summary>
        /// <param name="HasSucceeded"></param>
        /// <param name="computername"></param>
        /// <returns></returns>
        private string FormatLine(string HasSucceeded, string computername)
        {
            return StringUtil.Format(MatchFormat, HasSucceeded, computername);
        }
    }

    /// <summary>
    /// The object returned by Rename-Computer cmdlet representing the status of the target machine.
    /// </summary>
    public sealed class RenameComputerChangeInfo
    {
        private const string MatchFormat = "{0}:{1}:{2}";

        /// <summary>
        /// The status which shows the operation was success or failure.
        /// </summary>
        public bool HasSucceeded { get; set; }

        /// <summary>
        /// The NewComputerName which represents the target machine.
        /// </summary>
        public string NewComputerName { get; set; }

        /// <summary>
        /// The OldComputerName which represented the target machine.
        /// </summary>
        public string OldComputerName { get; set; }

        /// <summary>
        /// Returns the string representation of this object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return FormatLine(this.HasSucceeded.ToString(), this.NewComputerName, this.OldComputerName);
        }

        /// <summary>
        /// Formats a line for use in ToString.
        /// </summary>
        /// <param name="HasSucceeded"></param>
        /// <param name="newcomputername"></param>
        /// <param name="oldcomputername"></param>
        /// <returns></returns>
        private string FormatLine(string HasSucceeded, string newcomputername, string oldcomputername)
        {
            return StringUtil.Format(MatchFormat, HasSucceeded, newcomputername, oldcomputername);
        }
    }
#endregion "Public API"

#region Helper
    /// <summary>
    /// Helper Class used by Stop-Computer,Restart-Computer and Test-Connection
    /// Also Contain constants used by System Restore related Cmdlets.
    /// </summary>
    internal static class ComputerWMIHelper
    {
        /// <summary>
        /// The maximum length of a valid NetBIOS name
        /// </summary>
        internal const int NetBIOSNameMaxLength = 15;

        /// <summary>
        /// System Restore Class used by Cmdlets
        /// </summary>
        internal const string WMI_Class_SystemRestore = "SystemRestore";

        /// <summary>
        /// OperatingSystem WMI class used by Cmdlets
        /// </summary>
        internal const string WMI_Class_OperatingSystem = "Win32_OperatingSystem";

        /// <summary>
        /// Service WMI class used by Cmdlets
        /// </summary>
        internal const string WMI_Class_Service = "Win32_Service";

        /// <summary>
        /// Win32_ComputerSystem WMI class used by Cmdlets
        /// </summary>
        internal const string WMI_Class_ComputerSystem = "Win32_ComputerSystem";

        /// <summary>
        /// Ping Class used by Cmdlet.
        /// </summary>
        internal const string WMI_Class_PingStatus = "Win32_PingStatus";

        /// <summary>
        /// CIMV2 path
        /// </summary>
        internal const string WMI_Path_CIM = "\\root\\cimv2";

        /// <summary>
        /// Default path
        /// </summary>
        internal const string WMI_Path_Default = "\\root\\default";

        /// <summary>
        /// The error says The interface is unknown.
        /// </summary>
        internal const int ErrorCode_Interface = 1717;

        /// <summary>
        /// This error says An instance of the service is already running.
        /// </summary>
        internal const int ErrorCode_Service = 1056;

        /// <summary>
        /// The name of the privilege to shutdown a local system
        /// </summary>
        internal const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";

        /// <summary>
        /// The name of the privilege to shutdown a remote system
        /// </summary>
        internal const string SE_REMOTE_SHUTDOWN_NAME = "SeRemoteShutdownPrivilege";

        /// <summary>
        /// DCOM protocol
        /// </summary>
        internal const string DcomProtocol = "DCOM";

        /// <summary>
        /// WSMan protocol
        /// </summary>
        internal const string WsmanProtocol = "WSMan";

        /// <summary>
        /// CimUriPrefix
        /// </summary>
        internal const string CimUriPrefix = "http://schemas.microsoft.com/wbem/wsman/1/wmi/root/cimv2";

        /// <summary>
        /// CimOperatingSystemNamespace
        /// </summary>
        internal const string CimOperatingSystemNamespace = "root/cimv2";

        /// <summary>
        /// CimOperatingSystemShutdownMethod
        /// </summary>
        internal const string CimOperatingSystemShutdownMethod = "Win32shutdown";

        /// <summary>
        /// CimQueryDialect
        /// </summary>
        internal const string CimQueryDialect = "WQL";

        /// <summary>
        /// Local host name
        /// </summary>
        internal const string localhostStr = "localhost";


        /// <summary>
        /// Get the local admin user name from a local NetworkCredential
        /// </summary>
        /// <param name="computerName"></param>
        /// <param name="psLocalCredential"></param>
        /// <returns></returns>
        internal static string GetLocalAdminUserName(string computerName, PSCredential psLocalCredential)
        {
            string localUserName = null;

            // The format of local admin username should be "ComputerName\AdminName" 
            if (psLocalCredential.UserName.Contains("\\"))
            {
                localUserName = psLocalCredential.UserName;
            }
            else
            {
                int dotIndex = computerName.IndexOf(".", StringComparison.OrdinalIgnoreCase);
                if (dotIndex == -1)
                {
                    localUserName = computerName + "\\" + psLocalCredential.UserName;
                }
                else
                {
                    localUserName = computerName.Substring(0, dotIndex) + "\\" + psLocalCredential.UserName;
                }
            }

            return localUserName;
        }

        /// <summary>
        /// Generate a random password
        /// </summary>
        /// <param name="passwordLength"></param>
        /// <returns></returns>
        internal static string GetRandomPassword(int passwordLength)
        {
            const int charMin = 32, charMax = 122;
            const int allowedCharsCount = charMax - charMin + 1;
            byte[] randomBytes = new byte[passwordLength];
            char[] chars = new char[passwordLength];

            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            for (int i = 0; i < passwordLength; i++)
            {
                chars[i] = (char)(randomBytes[i] % allowedCharsCount + charMin);
            }

            return new string(chars);
        }

#if !CORECLR // TODO:CORECLR Remove once ported to MI .Net

        /// <summary>
        /// Get the Connection Options
        /// </summary>
        /// <param name="Authentication"></param>
        /// <param name="Impersonation"></param>
        /// <param name="Credential"></param>
        /// <returns></returns>
        internal static ConnectionOptions GetConnectionOptions(AuthenticationLevel Authentication, ImpersonationLevel Impersonation, PSCredential Credential)
        {
            ConnectionOptions options = new ConnectionOptions();
            options.Authentication = Authentication;
            options.EnablePrivileges = true;
            options.Impersonation = Impersonation;
            if (Credential != null)
            {
                options.Username = Credential.UserName;
                options.SecurePassword = Credential.Password;
            }
            return options;
        }

#endif

        /// <summary>
        /// Gets the Scope
        /// 
        /// </summary>
        /// <param name="computer"></param>
        /// <param name="namespaceParameter"></param>
        /// <returns></returns>
        internal static string GetScopeString(string computer, string namespaceParameter)
        {
            StringBuilder returnValue = new StringBuilder("\\\\");
            if (computer.Equals("::1", StringComparison.CurrentCultureIgnoreCase) || computer.Equals("[::1]", StringComparison.CurrentCultureIgnoreCase))
            {
                returnValue.Append("localhost");
            }
            else
            {
                returnValue.Append(computer);
            }
            returnValue.Append(namespaceParameter);
            return returnValue.ToString();
        }

        /// <summary>
        /// Returns true if it is a valid drive on the system.
        /// </summary>
        /// <param name="drive"></param>
        /// <returns></returns>
        internal static bool IsValidDrive(string drive)
        {
            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (DriveInfo logicalDrive in drives)
            {
                if (logicalDrive.DriveType.Equals(DriveType.Fixed))
                {
                    if (drive.ToString().Equals(logicalDrive.Name.ToString(), System.StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks whether string[] contains System Drive.
        /// </summary>
        /// <param name="drives"></param>
        /// <param name="sysdrive"></param>
        /// <returns></returns>
        internal static bool ContainsSystemDrive(string[] drives, string sysdrive)
        {
            string driveApp;
            foreach (string drive in drives)
            {
                if (!drive.EndsWith("\\", StringComparison.CurrentCultureIgnoreCase))
                {
                    driveApp = String.Concat(drive, "\\");
                }
                else
                    driveApp = drive;
                if (driveApp.Equals(sysdrive, StringComparison.CurrentCultureIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the given computernames in a string
        /// </summary>
        /// <param name="computerNames"></param>
        internal static string GetMachineNames(string[] computerNames)
        {
            string separator = ",";
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\International");
            if (regKey != null)
            {
                object sListValue = regKey.GetValue("sList");
                if (sListValue != null)
                {
                    separator = sListValue.ToString();
                }
            }

            string compname = string.Empty;
            StringBuilder strComputers = new StringBuilder();
            int i = 0;
            foreach (string computer in computerNames)
            {
                if (i > 0)
                {
                    strComputers.Append(separator);
                }
                else
                {
                    i++;
                }

                if ((computer.Equals("localhost", StringComparison.CurrentCultureIgnoreCase)) || (computer.Equals(".", StringComparison.OrdinalIgnoreCase)))
                {
                    compname = Dns.GetHostName();
                }
                else
                {
                    compname = computer;
                }

                strComputers.Append(compname);
            }

            return strComputers.ToString();
        }

        internal static ComputerChangeInfo GetComputerStatusObject(int errorcode, string computername)
        {
            ComputerChangeInfo computerchangeinfo = new ComputerChangeInfo();
            computerchangeinfo.ComputerName = computername;
            if (errorcode != 0)
            {
                computerchangeinfo.HasSucceeded = false;
            }
            else
            {
                computerchangeinfo.HasSucceeded = true;
            }
            return computerchangeinfo;
        }

        internal static RenameComputerChangeInfo GetRenameComputerStatusObject(int errorcode, string newcomputername, string oldcomputername)
        {
            RenameComputerChangeInfo renamecomputerchangeinfo = new RenameComputerChangeInfo();
            renamecomputerchangeinfo.OldComputerName = oldcomputername;
            renamecomputerchangeinfo.NewComputerName = newcomputername;
            if (errorcode != 0)
            {
                renamecomputerchangeinfo.HasSucceeded = false;
            }
            else
            {
                renamecomputerchangeinfo.HasSucceeded = true;
            }
            return renamecomputerchangeinfo;
        }


        internal static void WriteNonTerminatingError(int errorcode, PSCmdlet cmdlet, string computername)
        {
            Win32Exception ex = new Win32Exception(errorcode);
            string additionalmessage = String.Empty;
            if (ex.NativeErrorCode.Equals(0x00000035))
            {
                additionalmessage = StringUtil.Format(ComputerResources.NetworkPathNotFound, computername);
            }
            string message = StringUtil.Format(ComputerResources.OperationFailed, ex.Message, computername, additionalmessage);
            ErrorRecord er = new ErrorRecord(new InvalidOperationException(message), "InvalidOperationException", ErrorCategory.InvalidOperation, computername);
            cmdlet.WriteError(er);
        }

        /// <summary>
        /// Check whether the new computer name is valid
        /// </summary>
        /// <param name="computerName"></param>
        /// <returns></returns>
        internal static bool IsComputerNameValid(string computerName)
        {
            bool allDigits = true;

            if (computerName.Length >= 64)
                return false;

            foreach (char t in computerName)
            {
                if (t >= 'A' && t <= 'Z' ||
                    t >= 'a' && t <= 'z')
                {
                    allDigits = false;
                    continue;
                }
                else if (t >= '0' && t <= '9')
                {
                    continue;
                }
                else if (t == '-')
                {
                    allDigits = false;
                    continue;
                }
                else
                {
                    return false;
                }
            }

            return !allDigits;
        }

        /// <summary>
        /// System Restore APIs are not supported on the ARM platform. Skip the system restore operation is necessary.
        /// </summary>
        /// <param name="cmdlet"></param>
        /// <returns></returns>
        internal static bool SkipSystemRestoreOperationForARMPlatform(PSCmdlet cmdlet)
        {
            bool retValue = false;
            if (PsUtils.IsRunningOnProcessorArchitectureARM())
            {
                var ex = new InvalidOperationException(ComputerResources.SystemRestoreNotSupported);
                var er = new ErrorRecord(ex, "SystemRestoreNotSupported", ErrorCategory.InvalidOperation, null);
                cmdlet.WriteError(er);
                retValue = true;
            }
            return retValue;
        }

        /// <summary>
        /// Invokes the Win32Shutdown command on provided target computer using WSMan
        /// over a CIMSession.  The flags parameter determines the type of shutdown operation
        /// such as shutdown, reboot, force etc.
        /// </summary>
        /// <param name="cmdlet">Cmdlet host for reporting errors</param>
        /// <param name="isLocalhost">True if local host computer</param>
        /// <param name="computerName">Target computer</param>
        /// <param name="flags">Win32Shutdown flags</param>
        /// <param name="credential">Optional credential</param>
        /// <param name="authentication">Optional authentication</param>
        /// <param name="formatErrorMessage">Error message format string that takes two parameters</param>
        /// <param name="ErrorFQEID">Fully qualified error Id</param>
        /// <param name="cancelToken">Cancel token</param>
        /// <returns>True on success</returns>
        internal static bool InvokeWin32ShutdownUsingWsman(
            PSCmdlet cmdlet,
            bool isLocalhost,
            string computerName,
            object[] flags,
            PSCredential credential,
            string authentication,
            string formatErrorMessage,
            string ErrorFQEID,
            CancellationToken cancelToken)
        {
            Dbg.Diagnostics.Assert(flags.Length == 2, "Caller need to verify the flags passed in");

            bool isSuccess = false;
            string targetMachine = isLocalhost ? "localhost" : computerName;
            string authInUse = isLocalhost ? null : authentication;
            PSCredential credInUse = isLocalhost ? null : credential;
            var currentPrivilegeState = new PlatformInvokes.TOKEN_PRIVILEGE();
            var operationOptions = new CimOperationOptions
            {
                Timeout = TimeSpan.FromMilliseconds(10000),
                CancellationToken = cancelToken,
                //This prefix works against all versions of the WinRM server stack, both win8 and win7
                ResourceUriPrefix = new Uri(ComputerWMIHelper.CimUriPrefix)
            };

            try
            {
                if (!(isLocalhost && PlatformInvokes.EnableTokenPrivilege(ComputerWMIHelper.SE_SHUTDOWN_NAME, ref currentPrivilegeState)) &&
                    !(!isLocalhost && PlatformInvokes.EnableTokenPrivilege(ComputerWMIHelper.SE_REMOTE_SHUTDOWN_NAME, ref currentPrivilegeState)))
                {
                    string message =
                        StringUtil.Format(ComputerResources.PrivilegeNotEnabled, computerName,
                            isLocalhost ? ComputerWMIHelper.SE_SHUTDOWN_NAME : ComputerWMIHelper.SE_REMOTE_SHUTDOWN_NAME);
                    ErrorRecord errorRecord = new ErrorRecord(new InvalidOperationException(message), "PrivilegeNotEnabled", ErrorCategory.InvalidOperation, null);
                    cmdlet.WriteError(errorRecord);
                    return false;
                }

                using (CimSession cimSession = RemoteDiscoveryHelper.CreateCimSession(targetMachine, credInUse, authInUse, cancelToken, cmdlet))
                {
                    var methodParameters = new CimMethodParametersCollection();
                    methodParameters.Add(CimMethodParameter.Create(
                        "Flags",
                        flags[0],
                        Microsoft.Management.Infrastructure.CimType.SInt32,
                        CimFlags.None));

                    methodParameters.Add(CimMethodParameter.Create(
                        "Reserved",
                        flags[1],
                        Microsoft.Management.Infrastructure.CimType.SInt32,
                        CimFlags.None));

                    CimMethodResult result = cimSession.InvokeMethod(
                        ComputerWMIHelper.CimOperatingSystemNamespace,
                        ComputerWMIHelper.WMI_Class_OperatingSystem,
                        ComputerWMIHelper.CimOperatingSystemShutdownMethod,
                        methodParameters,
                        operationOptions);

                    int retVal = Convert.ToInt32(result.ReturnValue.Value, CultureInfo.CurrentCulture);
                    if (retVal != 0)
                    {
                        var ex = new Win32Exception(retVal);
                        string errMsg = StringUtil.Format(formatErrorMessage, computerName, ex.Message);
                        ErrorRecord error = new ErrorRecord(
                            new InvalidOperationException(errMsg), ErrorFQEID, ErrorCategory.OperationStopped, computerName);
                        cmdlet.WriteError(error);
                    }
                    else
                    {
                        isSuccess = true;
                    }
                }
            }
            catch (CimException ex)
            {
                string errMsg = StringUtil.Format(formatErrorMessage, computerName, ex.Message);
                ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), ErrorFQEID,
                                                    ErrorCategory.OperationStopped, computerName);
                cmdlet.WriteError(error);
            }
            catch (Exception ex)
            {
                CommandProcessorBase.CheckForSevereException(ex);
                string errMsg = StringUtil.Format(formatErrorMessage, computerName, ex.Message);
                ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), ErrorFQEID,
                                                    ErrorCategory.OperationStopped, computerName);
                cmdlet.WriteError(error);
            }
            finally
            {
                // Restore the previous privilege state if something unexpected happened
                PlatformInvokes.RestoreTokenPrivilege(
                    isLocalhost ? ComputerWMIHelper.SE_SHUTDOWN_NAME : ComputerWMIHelper.SE_REMOTE_SHUTDOWN_NAME, ref currentPrivilegeState);
            }

            return isSuccess;
        }

        /// <summary>
        /// Returns valid computer name or null on failure.
        /// </summary>
        /// <param name="nameToCheck">Computer name to validate</param>
        /// <param name="shortLocalMachineName"></param>
        /// <param name="fullLocalMachineName"></param>
        /// <param name="error"></param>
        /// <returns>Valid computer name</returns>
        internal static string ValidateComputerName(
            string nameToCheck,
            string shortLocalMachineName,
            string fullLocalMachineName,
            ref ErrorRecord error)
        {
            string validatedComputerName = null;

            if (nameToCheck.Equals(".", StringComparison.OrdinalIgnoreCase) ||
                nameToCheck.Equals(localhostStr, StringComparison.OrdinalIgnoreCase) ||
                nameToCheck.Equals(shortLocalMachineName, StringComparison.OrdinalIgnoreCase) ||
                nameToCheck.Equals(fullLocalMachineName, StringComparison.OrdinalIgnoreCase))
            {
                validatedComputerName = localhostStr;
            }
            else
            {
                bool isIPAddress = false;
                try
                {
                    IPAddress unused;
                    isIPAddress = IPAddress.TryParse(nameToCheck, out unused);
                }
                catch (Exception e)
                {
                    CommandProcessorBase.CheckForSevereException(e);
                }

                try
                {
                    string fqcn = Dns.GetHostEntryAsync(nameToCheck).Result.HostName;
                    if (fqcn.Equals(shortLocalMachineName, StringComparison.OrdinalIgnoreCase) ||
                        fqcn.Equals(fullLocalMachineName, StringComparison.OrdinalIgnoreCase))
                    {
                        // The IPv4 or IPv6 of the local machine is specified
                        validatedComputerName = localhostStr;
                    }
                    else
                    {
                        validatedComputerName = nameToCheck;
                    }
                }
                catch (Exception e)
                {
                    CommandProcessorBase.CheckForSevereException(e);

                    // If GetHostEntry() throw exception, then the target should not be the local machine
                    if (!isIPAddress)
                    {
                        // Return error if the computer name is not an IP address. Dns.GetHostEntry() may not work on IP addresses.
                        string errMsg = StringUtil.Format(ComputerResources.CannotResolveComputerName, nameToCheck, e.Message);
                        error = new ErrorRecord(
                            new InvalidOperationException(errMsg), "AddressResolutionException",
                            ErrorCategory.InvalidArgument, nameToCheck);

                        return null;
                    }
                    validatedComputerName = nameToCheck;
                }
            }

            return validatedComputerName;
        }
    }
#endregion Helper

#region Internal Enums

    internal enum TransportProtocol
    {
        DCOM = 1,
        WSMan = 2
    }

#endregion
}//End namespace

#endif
