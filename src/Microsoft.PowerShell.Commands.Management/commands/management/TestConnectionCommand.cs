#if UNIX

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
    [OutputType(typeof(PingStatus))]
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
        [Alias("TimeToLive")]
        [ValidateRange(1,(int)255)]
        public Int32 TTL { get; set; } = 80;
        
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
						PingReply reply = sender.SendPingAsync(ComputerName, (timeout * 1000), new byte[] { 0xff }, new PingOptions(TTL, true)).Result;
                    
					    //If things are good then set our bool to true, otherwise leave it false or whatever it currently is
					    if (reply.Status == IPStatus.Success)
					    {
						    quietReply = true;
					    }

					    //We want to be chatty
					    if (!this.quiet)
					    {
                            //We are going to wrap our custom class into a PSObject and format it
							var outObj = new PingStatus(reply.Status, this.ComputerName, reply.Address, reply.RoundtripTime, TTL);
                            var pso = new PSObject(outObj);
                            //Make sure to set the object Type so that it matches with our formatters
							pso.TypeNames.Insert(0, "Microsoft.PowerShell.Commands.PingStatus");

                            //Write our object.
							WriteObject(pso,true);
                            
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
        /// <summary>Source computer name</summary>
        public string Source { get; set; } = System.Environment.MachineName;
        /// <summary>Remote system being checked IPAddress object</summary>
		public IPAddress Address { get; set; }
		/// <summary>Remote system being checked, either FQDN, IPv4, or IPv6 Address</summary>
		public string Destination { get; set; }
        /// <summary>Size of buffer sent</summary>
        public long BufferSize { get; set; } = 32;
        /// <summary>If TRUE "Do Not Fragment" flag is set</summary>
        public bool NoFragmentation { get; set; } = false;
        /// <summary>Status of the address resolution process</summary>
        public long PrimaryAddressResolutionStatus { get; set; } = 0;
        /// <summary>Address that the destination used to reply</summary>
        public string ProtocolAddress { get; set; } = "";
        /// <summary>Resolved address corresponding to the ProtocolAddress property</summary>
        public string ProtocolAddressResolved { get; set; } = "";
        /// <summary>How many hops should be recorded while the packet is en route</summary>
        public long RecordRoute { get; set; } = 0;
        /// <summary>Inconsistent reply data is present</summary>
        public bool ReplyInconsistency { get; set; } = false;
        /// <summary>Size of the buffer returned</summary>
        public long ReplySize { get; set; } = 32;
        /// <summary>Command resolves address names of output address values</summary>
        public bool ResolvedAddressNames { get; set; } = false;
        /// <summary>Time or return in milliseconds (ms)</summary>
        public long ResponseTime { get; set; }
        /// <summary>The ResponseTimeToLive property indicates the time to live from moment the request is received.</summary>
        public long ResponseTimeToLive { get; set; }
        /// <summary>Record of intermediate hops</summary>
        public string[] RouteRecord { get; set; }
        /// <summary>Resolved address that corresponds to the RouteRecord value</summary>
        public string[] RouteRecordResolved { get; set; }
        /// <summary>Comma-separated list of valid Source Routes</summary>
        public string SourceRoute { get; set; } = "";
        /// <summary>Type of source route option to be used on the host list specified in the SourceRoute property</summary>
        public long SourceRouteType { get; set; } = 0;
        /// <summary>Response Status</summary>
        public IPStatus StatusCode { get; set; }
        /// <summary>Timeout value in milliseconds</summary>
        public long Timeout { get; set; } = 1000;
        /// <summary>Record of time stamps for intermediate hops</summary>
        public long[] TimeStampRecord { get; set; }
        /// <summary>Intermediate hop that corresponds to the TimeStampRecord</summary>
        public string[] TimeStampRecordAddress { get; set; }
        /// <summary>Resolved address that corresponds to the TimeStampRecordAddress value</summary>
        public string[] TimeStampRecordAddressResolved { get; set; }
        /// <summary>How many hops should be recorded with time stamp information while the packet is en route</summary>
        public long TimeStampRoute { get; set; } = 0;
        /// <summary>Life span of the ping packet in seconds</summary>
        public long TimeToLive { get; set; } = 80;
        /// <summary>IPv4 Address Object</summary>
		public IPAddress Ipv4 { get; set; }
		/// <summary>IPv6 Address Object</summary>
		public IPAddress Ipv6 { get; set; }
		/// <summary>Type of service that is used</summary>
        public long TypeofService { get; set; } = 0;

#endregion
        /// <summary> Create the object </summary>
        public PingStatus(IPStatus StatusCode)
        {
            this.StatusCode = StatusCode;
        }

        /// <summary> Create the object </summary>
        public PingStatus(IPStatus StatusCode,String Destination)
        {
            this.StatusCode = StatusCode;
			this.Destination = Destination;
            ConfigureProperties();
        }

        /// <summary> Create the object </summary>
        public PingStatus(IPStatus StatusCode,String Destination,long RoundTripTime)
        {
            this.StatusCode = StatusCode;
            this.Destination = Destination;
            this.ResponseTime = RoundTripTime;
            ConfigureProperties();
        }

        /// <summary> Create the object </summary>
		public PingStatus(IPStatus StatusCode, string Destination, IPAddress ReplyAddress, long RoundTripTime, long TimeToLive)
        {
            this.StatusCode = StatusCode;
            this.Destination = Destination;
            this.ResponseTime = RoundTripTime;
            this.ResponseTimeToLive = TimeToLive;
			this.Address = ReplyAddress;
			ConfigureProperties();
        }

        private void ConfigureProperties()
		{
			if (Object.ReferenceEquals(null,this.Address))
			{
				this.Address = IPAddress.Parse(this.Destination);
			}

			if (this.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
			{
				this.Ipv4 = this.Address;
			}
			else if (this.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
			{
				this.Ipv6 = this.Address;
			}
        }
    }
}
#endif
