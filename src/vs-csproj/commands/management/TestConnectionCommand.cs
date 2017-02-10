//#if UNIX

using System.Management.Automation;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This cmdlet is used to test whether a particular host is reachable across an
    /// IP network. It works by sending ICMP "echo request" packets to the target
    /// host and listening for ICMP "echo response" replies. This cmdlet prints a
    /// statistical summary when finished.
    /// </summary>
    /// <example>
    /// <para>Basic usage</para>
    /// <code>Test-Connection -IPAddress 8.8.8.8</code>
    /// </example>
    [Cmdlet(VerbsDiagnostic.Test, "Connection", DefaultParameterSetName = "Default")]
    [OutputType(typeof(PingResponse))]
    [OutputType(typeof(bool))]
    public sealed class TestConnectionCommand : PSCmdlet
    {
#region Variables and Constants
        const int timeout = 60; //Timeout value
#endregion
#region Parameters
        /// <summary>
        /// The following is the definition of the input parameter "ComputerName".
        /// Value of the address requested. The form of the value can be either the
        /// computer name ("wxyz1234"), IPv4 address ("192.168.177.124"), or IPv6
        /// address ("2010:836B:4179::836B:4179").
        /// </summary>
        [Parameter(Mandatory = true, 
            Position = 0, 
            ValueFromPipelineByPropertyName = true, 
            HelpMessage = "Remote system to check")]
        [ValidateNotNullOrEmpty]
        [Alias("CN", "IPAddress", "__SERVER", "Server", "Destination", "Computer")]
        public String ComputerName { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "TimeToLive".
        /// Life span of the packet in seconds. The value is treated as an upper limit.
        /// All routers must decrement this value by 1 (one). When this value becomes 0
        /// (zero), the packet is dropped by the router. The default value is 80
        /// seconds. The hops between routers rarely take this amount of time.
        /// </summary>
        [Parameter(Mandatory = false, 
            HelpMessage = "No response timeout")]
        [Alias("TTL")]
        [ValidateRange(1,(int)255)]
        public Int32 TimeToLive { get; set; } = 80;
        
        /// <summary>
        /// The following is the definition of the input parameter "Count".
        /// Number of echo requests to send.
        /// </summary>
        [Parameter(Mandatory = false, 
            HelpMessage = "Number of pings to send")]
        [ValidateRange(1, UInt32.MaxValue)]
        public int Count { get; set; } = 4;

        /// <summary>Time in seconds between pings</summary>
        [Parameter(Mandatory = false,
            HelpMessage = "Time in seconds between pings")]
        [ValidateRange(1, 60)]
        public int Delay { get; set; } = 1;

        /// <summary>Return only boolean value representing success/failure</summary>
        [Parameter(Mandatory =false,
            HelpMessage ="Return only boolean value representing success/failure")]
        public SwitchParameter Quiet
        {
            get
            {
                return (SwitchParameter)this.quiet;
            }
            set
            {
                this.quiet = (bool)value;
            }
        }
        private bool quiet = false;

        /// <summary>
        /// The following is the definition of the input parameter "BufferSize".
        /// Buffer size sent with the this command. The default value is 32.
        /// </summary>
        [Parameter]
        [Alias("Size", "Bytes", "BS")]
        [ValidateRange((int)0, (int)65500)]
        public Int32 BufferSize { get; set; } = 32;

#endregion
#region Meat of the class
        /// <summary> Create the object </summary>
        protected override void ProcessRecord()
        {
            bool quietReply = false;
            try
            {
                IPHostEntry ipHostEntry = Dns.GetHostEntryAsync(ComputerName).Result;
            
                //If we got this far, somethings going good
                try
			    {
				    for (int i = 0; i < Count; i++)
				    {
					    Ping sender = new Ping();
					    PingReply reply = sender.SendPingAsync(ComputerName, (timeout * 1000), new byte[] { 0xff }, new PingOptions(TimeToLive, true)).Result;
                    
					    //If things are good then set our bool to true, otherwise leave it false or whatever it currently is
					    if (reply.Status == IPStatus.Success)
					    {
						    quietReply = true;
					    }

					    //We want to be chatty
					    if (!this.quiet)
					    {
						    WriteObject(new PingResponse(reply.Status, reply.Address, reply.RoundtripTime));
					    }

                        Thread.Sleep(new TimeSpan(0, 0, Delay));
				    }
				    //If we are passing -quiet we only want the one final pass/fail, so here it is
				    if (this.quiet)
				    {
					    WriteObject(quietReply);
				    }
			    }
			    catch (PingException pex)
			    {
				    WriteError(new ErrorRecord(pex, pex.InnerException.ToString(), ErrorCategory.NotSpecified, pex.Source));
			    }
			    catch (NullReferenceException nrefex)
			    {
				    WriteError(new ErrorRecord(nrefex, nrefex.Message, ErrorCategory.ObjectNotFound, nrefex.Source));
			    }
			    catch(Exception ex)
			    {
                    WriteError(new ErrorRecord(ex, ex.InnerException.ToString(), ErrorCategory.NotSpecified,ex.Source));
			    }
            }
            catch(Exception ex)
            {
                WriteError(new ErrorRecord(ex, ex.InnerException.ToString(), ErrorCategory.NotSpecified,ex.Source));
            }
        }
#endregion
    }


    /// <summary> 
    ///     Rough implementation of System.Management.ManagementObject#root\cimv2\Win32_PingStatus
    ///     based on <https://msdn.microsoft.com/en-us/library/aa394350(v=vs.85).aspx> for use on Non-Windows platforms
    ///</summary>
    public class PingStatus
    {
        #region Class Properties
        public string PSComputerName { get; set; } = System.Environment.MachineName;
        /// <summary>Remote system being checked, either FQDN, IPv4, or IPv6 Address</summary>
        public String Address { get; set; }
        public UInt32 BufferSize { get; set; } = 32;
        public bool NoFragmentation { get; set; } = false;
        public UInt32 PrimaryAddressResolutionStatus { get; set; } = 0;
        public string ProtocolAddress { get; set; } = "";
        public string ProtocolAddressResolved { get; set; } = "";
        public UInt32 RecordRoute { get; set; } = 0;
        public bool ReplyInconsistency { get; set; } = false;
        /// <summary>Size of the buffer returned</summary>
        public UInt32 ReplySize { get; set; } = 32;
        public bool ResolvedAddressNames { get; set; } = false;
        /// <summary>Time or return in milliseconds (ms)</summary>
        public UInt32 ResponseTime { get; set; }
        /// <summary>The ResponseTimeToLive property indicates the time to live from moment the request is received.</summary>
        public UInt32 ResponseTimeToLive { get; set; }
        public string[] RouteRecord { get; set; }
        public string[] RouteRecordResolved { get; set; }
        public string SourceRoute { get; set; } = "";
        public UInt32 SourceRouteType { get; set; } = 0;
        /// <summary>Response Status</summary>
        public UInt32 StatusCode { get; set; }
        public UInt32 Timeout { get; set; } = 1000;
        public UInt32[] TimeStampRecord { get; set; };
        public string[] TimeStampRecordAddress { get; set; }
        public string[] TimeStampRecordAddressResolved { get; set; }
        public UInt32 TimeStampRoute { get; set; } = 0;
        public UInt32 TimeToLive { get; set; } = 80;
        public UInt32 TypeofService { get; set; } = 0;
#endregion
        /// <summary> Create the object </summary>
        public PingResponse(UInt32 StatusCode)
        {
            this.Status = Status;
        }

        /// <summary> Create the object </summary>
        public PingResponse(UInt32 StatusCode,String Address)
        {
            this.Status = Status;
            this.Address = Address;
        }

        /// <summary> Create the object </summary>
        public PingResponse(UInt32 StatusCode,String Address,UInt32 RoundTripTime)
        {
            this.Status = Status;
            this.Address = Address;
            this.ResponseTime = RoundTripTime;
        }

        /// <summary> Create the object </summary>
        public PingResponse(string Status, string Address, UInt32 RoundTripTime, Int32 TimeToLive)
        {
            this.Status = Status;
            this.Address = Address;
            this.ResponseTime = RoundTripTime;
            this.ResponseTimeToLive = TimeToLive;
        }
    }
}
#endif
