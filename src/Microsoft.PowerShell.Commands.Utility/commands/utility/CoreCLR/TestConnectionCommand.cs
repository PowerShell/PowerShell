using System.Management.Automation;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Test-Connection is a drop-in replacement for Test-Connection on non-Windows systems
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
#region Variable Decleration 
        private int count = 3;
        private string destination;
        private int timetolive = 64;
        private int timeout = 60;
        private int delay = 1;
        private bool quiet;
#endregion
#region Parameters
        /// <summary>Remote system to check</summary>
        [Parameter(ParameterSetName = "Default", 
            Mandatory = true, 
            Position = 0, 
            ValueFromPipelineByPropertyName = true, 
            HelpMessage = "Remote system to check")]
        [ValidateNotNullOrEmpty]
        [Alias(new string[] { "CN", "IPAddress", "__SERVER", "Server", "Destination" })]
        public string ComputerName
        {
            get
            {
                return this.destination;
            }
            set
            {
                this.destination = value;
            }
        }

        /// <summary>No response timeout</summary>
        [Parameter(ParameterSetName = "Default", 
            Mandatory = false, 
            HelpMessage = "No response timeout")]
        [Alias(new string[] { "expire" })]
        [ValidateRange(10,3600)]
        public int TimeOut
        {
            get
            {
                return this.timeout;
            }
            set
            {
                this.timeout = value;
            }
        }

        /// <summary>Number of pings to send</summary>
        [Parameter(ParameterSetName = "Default", 
            Mandatory = false, 
            HelpMessage = "Number of pings to send")]
        [ValidateRange(1, 4294967295)]
        public int Count
        {
            get
            {
                return this.count;
            }
            set
            {
                this.count = value;
            }
        }

        /// <summary>Time in seconds between pings</summary>
        [Parameter(ParameterSetName = "Default",
            Mandatory = false,
            HelpMessage = "Time in seconds between pings")]
        [ValidateRange(1, 60)]
        public int Delay
        {
            get
            {
                return this.delay;
            }
            set
            {
                this.delay = value;
            }
        }

        /// <summary>Return only boolean value representing success/failure</summary>
        [Parameter(ParameterSetName = "Default",
            Mandatory =false,
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
#endregion
#region Meat of the class
        /// <summary> Create the object </summary>
        protected override void ProcessRecord()
        {
            bool quietReply = false;
            try
            {
                IPHostEntry ipHostEntry = Dns.GetHostEntryAsync(this.destination).Result;
            
            //If we got this far, somethings going good
                try
			    {
				    for (int i = 0; i < this.count; i++)
				    {
					    Ping sender = new Ping();
					    PingReply reply = sender.SendPingAsync(this.destination, (timeout * 1000), new byte[] { 0xff }, new PingOptions(this.timetolive, true)).Result;
                    
					    //If things are good then set our bool to true, otherwise leave it false or whatever it currently is
					    if (reply.Status == IPStatus.Success)
					    {
						    quietReply = true;
					    }

					    //We want to be chatty
					    if (!this.quiet)
					    {
						    WriteObject(new PingResponse(reply.Status.ToString(), reply.Address.ToString(), reply.RoundtripTime));
					    }
				    }
				    //If we are passing -quiet we only want the one final pass/fail, so here it is
				    if (this.quiet)
				    {
					    WriteObject(quietReply);
				    }
			    }
			    catch (PingException pex)
			    {
				    WriteError(new ErrorRecord(pex, pex.Data.ToString(), ErrorCategory.NotSpecified, pex.Source));
			    }
			    catch (NullReferenceException nrefex)
			    {
				    WriteError(new ErrorRecord(nrefex, nrefex.Message, ErrorCategory.ObjectNotFound, nrefex.Source));
			    }
			    catch
			    {
				    //WriteError(new ErrorRecord(ex, ex.HResult.ToString(), ErrorCategory.NotSpecified, ex.Source));
                    String errObject = this.destination + " not able to be resolved.\nPlease check the address and try again";
                    WriteObject(errObject);
			    }
            }
            catch
            {
                String oString = this.destination + " not resolvable\n";
                WriteObject(oString);
            }
            Thread.Sleep(new TimeSpan(0, 0, this.delay));
        }
#endregion
    }

     /// <summary> Object for Ping Response</summary>
    public class PingResponse
    {
        /// <summary>Response Status</summary>
        public string Status { get; set; }
        /// <summary>Remote system being checked</summary>
        public string Destination { get; set; }
        /// <summary>Time or return in milliseconds (ms)</summary>
        public long Time { get; set; }
        /// <summary>Time-to-Live</summary>
        public int TTL { get; set; }

        /// <summary> Create the object </summary>
        public PingResponse(string Status)
        {
            this.Status = Status;
        }

        /// <summary> Create the object </summary>
        public PingResponse(string Status,string Address)
        {
            this.Status = Status;
            this.Destination = Address;
        }

        /// <summary> Create the object </summary>
        public PingResponse(string Status,string Address,long RoundTripTime)
        {
            this.Status = Status;
            this.Destination = Address;
            this.Time = RoundTripTime;
        }

        /// <summary> Create the object </summary>
        public PingResponse(string Status, string Address, long RoundTripTime, int TimeToLive)
        {
            this.Status = Status;
            this.Destination = Address;
            this.Time = RoundTripTime;
            this.TTL = TimeToLive;
        }
    }
}
