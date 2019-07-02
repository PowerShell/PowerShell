// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The implementation of the "Test-Connection" cmdlet.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, "Connection", DefaultParameterSetName = ParameterSetPing,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135266")]
    [OutputType(typeof(PingStatus),
        ParameterSetName = new[] { ParameterSetPing, ParameterSetPingContinue, ParameterSetDetectionOfMTUSize })]
    [OutputType(typeof(TraceStatus), ParameterSetName = new[] { ParameterSetTraceRoute })]
    [OutputType(typeof(bool),
        ParameterSetName = new[] { ParameterSetPing, ParameterSetPingContinue, ParameterSetConnectionByTCPPort })]
    [OutputType(typeof(int), ParameterSetName = new[] { ParameterSetDetectionOfMTUSize })]
    public class TestConnectionCommand : PSCmdlet, IDisposable
    {
        private const string ParameterSetPing = "PingCount";
        private const string ParameterSetPingContinue = "PingContinue";
        private const string ParameterSetTraceRoute = "TraceRoute";
        private const string ParameterSetConnectionByTCPPort = "ConnectionByTCPPort";
        private const string ParameterSetDetectionOfMTUSize = "DetectionOfMTUSize";
        private const int sMaxHops = 128;

        private readonly Ping _sender = new Ping();
        private readonly ManualResetEventSlim _pingComplete = new ManualResetEventSlim();
        private PingCompletedEventArgs _pingCompleteArgs;

        #region Parameters

        /// <summary>
        /// Do ping test.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSetPing)]
        [Parameter(ParameterSetName = ParameterSetPingContinue)]
        public SwitchParameter Ping { get; set; } = true;

        /// <summary>
        /// Force using IPv4 protocol.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSetPing)]
        [Parameter(ParameterSetName = ParameterSetPingContinue)]
        [Parameter(ParameterSetName = ParameterSetTraceRoute)]
        [Parameter(ParameterSetName = ParameterSetDetectionOfMTUSize)]
        [Parameter(ParameterSetName = ParameterSetConnectionByTCPPort)]
        public SwitchParameter IPv4 { get; set; }

        /// <summary>
        /// Force using IPv6 protocol.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSetPing)]
        [Parameter(ParameterSetName = ParameterSetPingContinue)]
        [Parameter(ParameterSetName = ParameterSetTraceRoute)]
        [Parameter(ParameterSetName = ParameterSetDetectionOfMTUSize)]
        [Parameter(ParameterSetName = ParameterSetConnectionByTCPPort)]
        public SwitchParameter IPv6 { get; set; }

        /// <summary>
        /// Do reverse DNS lookup to get names for IP addresses.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSetPing)]
        [Parameter(ParameterSetName = ParameterSetPingContinue)]
        [Parameter(ParameterSetName = ParameterSetTraceRoute)]
        [Parameter(ParameterSetName = ParameterSetDetectionOfMTUSize)]
        [Parameter(ParameterSetName = ParameterSetConnectionByTCPPort)]
        public SwitchParameter ResolveDestination { get; set; }

        /// <summary>
        /// Source from which to do a test (ping, trace route, ...).
        /// The default is Local Host.
        /// Remoting is not yet implemented internally in the cmdlet.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSetPing)]
        [Parameter(ParameterSetName = ParameterSetPingContinue)]
        [Parameter(ParameterSetName = ParameterSetTraceRoute)]
        [Parameter(ParameterSetName = ParameterSetConnectionByTCPPort)]
        public string Source { get; } = Dns.GetHostName();

        /// <summary>
        /// The number of times the Ping data packets can be forwarded by routers.
        /// As gateways and routers transmit packets through a network,
        /// they decrement the Time-to-Live (TTL) value found in the packet header.
        /// The default (from Windows) is 128 hops.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSetPing)]
        [Parameter(ParameterSetName = ParameterSetPingContinue)]
        [Parameter(ParameterSetName = ParameterSetTraceRoute)]
        [ValidateRange(0, sMaxHops)]
        [Alias("Ttl", "TimeToLive", "Hops")]
        public int MaxHops { get; set; } = sMaxHops;

        /// <summary>
        /// Count of attempts.
        /// The default (from Windows) is 4 times.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSetPing)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int Count { get; set; } = 4;

        /// <summary>
        /// Delay between attempts.
        /// The default (from Windows) is 1 second.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSetPing)]
        [Parameter(ParameterSetName = ParameterSetPingContinue)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int Delay { get; set; } = 1;

        /// <summary>
        /// Buffer size to send.
        /// The default (from Windows) is 32 bites.
        /// Max value is 65500 (limit from Windows API).
        /// </summary>
        [Parameter(ParameterSetName = ParameterSetPing)]
        [Parameter(ParameterSetName = ParameterSetPingContinue)]
        [Alias("Size", "Bytes", "BS")]
        [ValidateRange(0, 65500)]
        public int BufferSize { get; set; } = DefaultSendBufferSize;

        /// <summary>
        /// Don't fragment ICMP packages.
        /// Currently CoreFX not supports this on Unix.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSetPing)]
        [Parameter(ParameterSetName = ParameterSetPingContinue)]
        public SwitchParameter DontFragment { get; set; }

        /// <summary>
        /// Continue ping until user press Ctrl-C
        /// or Int.MaxValue threshold reached.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSetPingContinue)]
        [Alias("Continues")]
        public SwitchParameter Repeat { get; set; }

        /// <summary>
        /// Set short output kind ('bool' for Ping, 'int' for MTU size ...).
        /// Default is to return typed result object(s).
        /// </summary>
        [Parameter()]
        public SwitchParameter Quiet;

        /// <summary>
        /// Time-out value in seconds.
        /// If a response is not received in this time, no response is assumed.
        /// It is not the cmdlet timeout! It is a timeout for waiting one ping response.
        /// The default (from Windows) is 5 second.
        /// </summary>
        [Parameter()]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int TimeoutSeconds { get; set; } = 5;

        /// <summary>
        /// Destination - computer name or IP address.
        /// </summary>
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [Alias("ComputerName")]
        public string[] TargetName { get; set; }

        /// <summary>
        /// Detect MTU size.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = ParameterSetDetectionOfMTUSize)]
        public SwitchParameter MTUSizeDetect { get; set; }

        /// <summary>
        /// Do traceroute test.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = ParameterSetTraceRoute)]
        public SwitchParameter Traceroute { get; set; }

        /// <summary>
        /// Do tcp connection test.
        /// </summary>
        [ValidateRange(0, 65535)]
        [Parameter(Mandatory = true, ParameterSetName = ParameterSetConnectionByTCPPort)]
        public int TCPPort { get; set; }

        #endregion Parameters

        /// <summary>
        /// Init the cmdlet.
        /// </summary>
        protected override void BeginProcessing()
        {
            _sender.PingCompleted += OnPingCompleted;

            if (ParameterSetName == ParameterSetPingContinue)
            {
                Count = int.MaxValue;
            }
        }

        /// <summary>
        /// Process a connection test.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (var targetName in TargetName)
            {
                switch (ParameterSetName)
                {
                    case ParameterSetPing:
                    case ParameterSetPingContinue:
                        ProcessPing(targetName);
                        break;
                    case ParameterSetDetectionOfMTUSize:
                        ProcessMTUSize(targetName);
                        break;
                    case ParameterSetTraceRoute:
                        ProcessTraceroute(targetName);
                        break;
                    case ParameterSetConnectionByTCPPort:
                        ProcessConnectionByTCPPort(targetName);
                        break;
                }
            }
        }

        /// <summary>
        /// StopProcessing implementation so we can handle Ctrl+C properly.
        /// </summary>
        protected override void StopProcessing()
        {
            _sender?.SendAsyncCancel();
        }

        #region ConnectionTest

        private void ProcessConnectionByTCPPort(string targetNameOrAddress)
        {
            if (!InitProcessPing(targetNameOrAddress, out _, out IPAddress targetAddress))
            {
                return;
            }

            var client = new TcpClient();

            try
            {
                Task connectionTask = client.ConnectAsync(targetAddress, TCPPort);
                var targetString = targetAddress.ToString();

                for (var i = 1; i <= TimeoutSeconds; i++)
                {
                    Task timeoutTask = Task.Delay(millisecondsDelay: 1000);
                    Task.WhenAny(connectionTask, timeoutTask).Result.Wait();

                    if (timeoutTask.Status == TaskStatus.Faulted || timeoutTask.Status == TaskStatus.Canceled)
                    {
                        // Waiting is interrupted by Ctrl-C.
                        WriteObject(false);
                        return;
                    }

                    if (connectionTask.Status == TaskStatus.RanToCompletion)
                    {
                        WriteObject(true);
                        return;
                    }
                }
            }
            catch
            {
                // Silently ignore connection errors.
            }
            finally
            {
                client.Close();
            }

            WriteObject(false);
        }

        #endregion ConnectionTest

        #region TracerouteTest
        private void ProcessTraceroute(string targetNameOrAddress)
        {
            byte[] buffer = GetSendBuffer(BufferSize);

            if (!InitProcessPing(targetNameOrAddress, out string resolvedTargetName, out IPAddress targetAddress))
            {
                return;
            }

            WriteVerbose(StringUtil.Format(
                TestConnectionResources.TraceRouteStart,
                resolvedTargetName,
                targetAddress,
                MaxHops));

            int currentHop = 1;
            int timeout = TimeoutSeconds * 1000;
            string hostname;

            var pingOptions = new PingOptions(currentHop, DontFragment.IsPresent);

            var timer = new Stopwatch();
            PingReply reply;

            do
            {
                reply = null;
                hostname = null;
                pingOptions.Ttl = currentHop;

                // We don't allow -Count parameter for -TraceRoute.
                // If we change 'DefaultTraceRoutePingCount' we should change 'ConsoleTraceRouteReply' resource string.
                for (uint i = 1; i <= DefaultTraceRoutePingCount; i++)
                {
                    try
                    {
                        reply = DoPing(targetAddress, timeout, buffer, pingOptions, timer);

                        if (hostname == null
                            && ResolveDestination
                            && (reply.Status == IPStatus.Success || reply.Status == IPStatus.TtlExpired))
                        {
                            try
                            {
                                hostname = Dns.GetHostEntry(reply.Address).HostName;
                            }
                            catch
                            {
                                // Swallow hostname resolve exceptions and continue with trace
                            }
                        }

                        var status = new TraceStatus(
                            currentHop,
                            new PingStatus(
                                Source,
                                hostname,
                                reply,
                                pingOptions,
                                timer.ElapsedMilliseconds,
                                buffer.Length,
                                i),
                            Source,
                            resolvedTargetName ?? targetNameOrAddress,
                            targetAddress);

                        WriteObject(status);
                        timer.Reset();
                    }
                    catch (PingException ex)
                    {
                        string message = StringUtil.Format(
                            TestConnectionResources.NoPingResult,
                            resolvedTargetName,
                            ex.Message);
                        var pingException = new PingException(message, ex.InnerException);
                        var errorRecord = new ErrorRecord(
                            pingException,
                            TestConnectionExceptionId,
                            ErrorCategory.ResourceUnavailable,
                            resolvedTargetName);
                        WriteError(errorRecord);

                        continue;
                    }

                    // We use short delay because it is impossible DoS with trace route.
                    Thread.Sleep(200);
                }

                currentHop++;
            }
            while (reply != null
                && currentHop <= MaxHops
                && (reply.Status == IPStatus.TtlExpired || reply.Status == IPStatus.TimedOut));

            WriteVerbose(TestConnectionResources.TraceRouteComplete);

            if (Quiet.IsPresent)
            {
                WriteObject(currentHop <= MaxHops);
            }
        }

        #endregion TracerouteTest

        #region MTUSizeTest
        private void ProcessMTUSize(string targetNameOrAddress)
        {
            PingReply reply, replyResult = null;

            if (!InitProcessPing(targetNameOrAddress, out _, out IPAddress targetAddress))
            {
                return;
            }

            // Caution! Algorithm is sensitive to changing boundary values.
            int HighMTUSize = 10000;
            int CurrentMTUSize = 1473;
            int LowMTUSize = targetAddress.AddressFamily == AddressFamily.InterNetworkV6 ? 1280 : 68;
            int timeout = TimeoutSeconds * 1000;

            try
            {
                var pingOptions = new PingOptions(MaxHops, true);
                int retry = 1;

                while (LowMTUSize < (HighMTUSize - 1))
                {
                    byte[] buffer = GetSendBuffer(CurrentMTUSize);

                    WriteDebug(StringUtil.Format(
                        "LowMTUSize: {0}, CurrentMTUSize: {1}, HighMTUSize: {2}",
                        LowMTUSize,
                        CurrentMTUSize,
                        HighMTUSize));

                    reply = DoPing(targetAddress, timeout, buffer, pingOptions);

                    // Cautious! Algorithm is sensitive to changing boundary values.
                    if (reply.Status == IPStatus.PacketTooBig)
                    {
                        HighMTUSize = CurrentMTUSize;
                        retry = 1;
                    }
                    else if (reply.Status == IPStatus.Success)
                    {
                        LowMTUSize = CurrentMTUSize;
                        replyResult = reply;
                        retry = 1;
                    }
                    else
                    {
                        // Target host don't reply - try again up to 'Count'.
                        if (retry >= Count)
                        {
                            string message = StringUtil.Format(
                                TestConnectionResources.NoPingResult,
                                targetAddress,
                                reply.Status.ToString());
                            var pingException = new PingException(message);
                            var errorRecord = new ErrorRecord(
                                pingException,
                                TestConnectionExceptionId,
                                ErrorCategory.ResourceUnavailable,
                                targetAddress);
                            WriteError(errorRecord);
                            return;
                        }
                        else
                        {
                            retry++;
                            continue;
                        }
                    }

                    CurrentMTUSize = (LowMTUSize + HighMTUSize) / 2;

                    // Prevent DoS attack.
                    Thread.Sleep(100);
                }
            }
            catch (PingException ex)
            {
                string message = StringUtil.Format(TestConnectionResources.NoPingResult, targetAddress, ex.Message);
                var pingException = new PingException(message, ex.InnerException);
                var errorRecord = new ErrorRecord(
                    pingException,
                    TestConnectionExceptionId,
                    ErrorCategory.ResourceUnavailable,
                    targetAddress);
                WriteError(errorRecord);
                return;
            }

            if (Quiet.IsPresent)
            {
                WriteObject(CurrentMTUSize);
            }
            else
            {
                WriteObject(new PingMtuStatus(Source, targetNameOrAddress, CurrentMTUSize, replyResult));
            }
        }

        #endregion MTUSizeTest

        #region PingTest

        private void ProcessPing(string targetNameOrAddress)
        {
            if (!InitProcessPing(targetNameOrAddress, out string resolvedTargetName, out IPAddress targetAddress))
            {
                return;
            }

            if (!Repeat.IsPresent)
            {
                WriteVerbose(StringUtil.Format(
                    TestConnectionResources.MTUSizeDetectStart,
                    resolvedTargetName,
                    targetAddress,
                    BufferSize));
            }

            bool quietResult = true;
            byte[] buffer = GetSendBuffer(BufferSize);

            PingReply reply;
            var pingOptions = new PingOptions(MaxHops, DontFragment.IsPresent);
            int timeout = TimeoutSeconds * 1000;
            int delay = Delay * 1000;

            for (uint i = 1; i <= Count; i++)
            {
                try
                {
                    reply = DoPing(targetAddress, timeout, buffer, pingOptions);
                }
                catch (PingException ex)
                {
                    string message = StringUtil.Format(
                        TestConnectionResources.NoPingResult,
                        resolvedTargetName,
                        ex.Message);
                    var pingException = new PingException(message, ex.InnerException);
                    var errorRecord = new ErrorRecord(
                        pingException,
                        TestConnectionExceptionId,
                        ErrorCategory.ResourceUnavailable,
                        resolvedTargetName);
                    WriteError(errorRecord);

                    quietResult = false;
                    continue;
                }

                if (Quiet.IsPresent)
                {
                    // Return 'true' only if all pings have completed successfully.
                    quietResult &= reply.Status == IPStatus.Success;
                }
                else
                {
                    WriteObject(new PingStatus(Source, resolvedTargetName, reply, i));
                }

                // Delay between ping but not after last ping.
                if (i < Count && Delay > 0)
                {
                    Thread.Sleep(delay);
                }
            }

            if (!Repeat.IsPresent)
            {
                WriteVerbose(TestConnectionResources.PingComplete);
            }

            if (Quiet.IsPresent)
            {
                WriteObject(quietResult);
            }
        }

        #endregion PingTest

        private bool InitProcessPing(
            string targetNameOrAddress,
            out string resolvedTargetName,
            out IPAddress targetAddress)
        {
            resolvedTargetName = targetNameOrAddress;

            IPHostEntry hostEntry;
            if (IPAddress.TryParse(targetNameOrAddress, out targetAddress))
            {
                if (ResolveDestination)
                {
                    hostEntry = Dns.GetHostEntry(targetNameOrAddress);
                    resolvedTargetName = hostEntry.HostName;
                }
            }
            else
            {
                try
                {
                    hostEntry = Dns.GetHostEntry(targetNameOrAddress);

                    if (ResolveDestination)
                    {
                        resolvedTargetName = hostEntry.HostName;
                        //hostEntry = Dns.GetHostEntry(hostEntry.HostName);
                    }
                }
                catch (Exception ex)
                {
                    string message = StringUtil.Format(
                        TestConnectionResources.NoPingResult,
                        resolvedTargetName,
                        TestConnectionResources.CannotResolveTargetName);
                    var pingException = new PingException(message, ex);
                    var errorRecord = new ErrorRecord(
                        pingException,
                        TestConnectionExceptionId,
                        ErrorCategory.ResourceUnavailable,
                        resolvedTargetName);
                    WriteError(errorRecord);
                    return false;
                }

                if (IPv6.IsPresent || IPv4.IsPresent)
                {
                    var addressFamily = IPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;

                    foreach (var address in hostEntry.AddressList)
                    {
                        if (address.AddressFamily == addressFamily)
                        {
                            targetAddress = address;
                            break;
                        }
                    }

                    if (targetAddress == null)
                    {
                        string message = StringUtil.Format(
                            TestConnectionResources.NoPingResult,
                            resolvedTargetName,
                            TestConnectionResources.TargetAddressAbsent);
                        var pingException = new PingException(message, null);
                        var errorRecord = new ErrorRecord(
                            pingException,
                            TestConnectionExceptionId,
                            ErrorCategory.ResourceUnavailable,
                            resolvedTargetName);
                        WriteError(errorRecord);
                        return false;
                    }
                }
                else
                {
                    targetAddress = hostEntry.AddressList[0];
                }
            }

            return true;
        }

        // Users most often use the default buffer size so we cache the buffer.
        // Creates and filles a send buffer. This follows the ping.exe and CoreFX model.
        private byte[] GetSendBuffer(int bufferSize)
        {
            if (bufferSize == DefaultSendBufferSize && s_DefaultSendBuffer != null)
            {
                return s_DefaultSendBuffer;
            }

            byte[] sendBuffer = new byte[bufferSize];

            for (int i = 0; i < bufferSize; i++)
            {
                sendBuffer[i] = (byte)((int)'a' + i % 23);
            }

            if (bufferSize == DefaultSendBufferSize && s_DefaultSendBuffer == null)
            {
                s_DefaultSendBuffer = sendBuffer;
            }

            return sendBuffer;
        }

        private PingReply DoPing(
            IPAddress targetAddress,
            int timeout,
            byte[] buffer,
            PingOptions pingOptions,
            Stopwatch timer = null)
        {
            timer?.Start();
            _sender.SendAsync(targetAddress, timeout, buffer, pingOptions, this);
            _pingComplete.Wait();
            timer?.Stop();
            // Pause to let _sender's async flags to be reset properly so the next SendAsync call doesn't fail.
            Thread.Sleep(1);
            _pingComplete.Reset();

            if (_pingCompleteArgs.Cancelled)
            {
                // The only cancellation we have implemented is on pipeline stops.
                throw new PipelineStoppedException();
            }

            if (_pingCompleteArgs.Error != null)
            {
                throw new PingException(_pingCompleteArgs.Error.Message, _pingCompleteArgs.Error);
            }

            return _pingCompleteArgs.Reply;
        }

        private static void OnPingCompleted(object sender, PingCompletedEventArgs e)
        {
            ((TestConnectionCommand)e.UserState)._pingCompleteArgs = e;
            ((TestConnectionCommand)e.UserState)._pingComplete.Set();
        }

        /// <summary>
        /// IDisposable implementation.
        /// </summary>
        public void Dispose()
        {
            _sender?.Dispose();
            _pingComplete?.Dispose();
        }

        /// <summary>
        /// The class contains an information about a trace route attempt.
        /// </summary>
        public class TraceStatus
        {
            /// <summary>
            /// Creates a new instance of the TraceStatus class.
            /// </summary>
            /// <param name="hop">The hop number of this trace hop.</param>
            /// <param name="status">The PingStatus response from this trace hop.</param>
            /// <param name="source">The source computer name or IP address of the traceroute.</param>
            /// <param name="destination">The target destination of the traceroute.</param>
            /// <param name="destinationAddress">The target IPAddress of the overall traceroute.</param>
            internal TraceStatus(
                int hop,
                PingStatus status,
                string source,
                string destination,
                IPAddress destinationAddress)
            {
                _status = status;
                Hop = hop;
                Source = source;
                Target = destination;
                TargetAddress = destinationAddress;
            }

            private readonly PingStatus _status;

            /// <summary>
            /// Gets the number of the current hop / router.
            /// </summary>
            public int Hop { get; }

            /// <summary>
            /// Gets the hostname of the current hop point.
            /// </summary>
            /// <value></value>
            public string Hostname
            {
                get => _status.Destination != "0.0.0.0"
                    ? _status.Destination
                    : null;
            }

            /// <summary>
            /// Gets the sequence number of the ping in the sequence of pings to the hop point.
            /// </summary>
            public uint Ping { get => _status.Ping; }

            /// <summary>
            /// Gets the IP address of the current hop point.
            /// </summary>
            public IPAddress HopAddress { get => _status.Address; }

            /// <summary>
            /// Gets the latency values of each ping to the current hop point.
            /// </summary>
            public long Latency { get => _status.Latency; }

            /// <summary>
            /// Gets the status of the traceroute hop.
            /// It is considered successful if the individual pings report either Success or TtlExpired.
            /// </summary>
            public IPStatus Status
            {
                get => _status.Status == IPStatus.TtlExpired
                    ? IPStatus.Success
                    : _status.Status;
            }

            /// <summary>
            /// Gets the source address of the traceroute command.
            /// </summary>
            public string Source { get; }

            /// <summary>
            /// Gets the final destination hostname of the trace.
            /// </summary>
            public string Target { get; }

            /// <summary>
            /// Gets the final destination IP address of the trace.
            /// </summary>
            public IPAddress TargetAddress { get; }

            /// <summary>
            /// Gets the raw PingReply object received from the ping to this hop point.
            /// </summary>
            public PingReply Reply { get => _status.Reply; }

            /// <summary>
            /// Retrieves the PingOptions used to send the ping to the trace hop.
            /// </summary>
            public PingOptions Options { get => _status.Options; }
        }

        /// <summary>
        /// The class contains information about the source, the destination and ping results.
        /// </summary>
        public class PingStatus
        {
            /// <summary>
            /// Creates a new instance of the PingStatus class.
            /// This constructor allows manually specifying the initial values for the cases where the PingReply
            /// object may be missing some information, specifically in the instances where PingReply objects are
            /// utilised to perform a traceroute.
            /// </summary>
            /// <param name="source">The source machine name or IP of the ping.</param>
            /// <param name="destination">The destination machine name of the ping.</param>
            /// <param name="reply">The response from the ping attempt.</param>
            /// <param name="options">The PingOptions specified when the ping was sent.</param>
            /// <param name="latency">The manually measured latency of the ping attempt.</param>
            /// <param name="bufferSize">The buffer size.</param>
            /// <param name="pingNum">The sequence number in the sequence of pings to the hop point.</param>
            internal PingStatus(
                string source,
                string destination,
                PingReply reply,
                PingOptions options,
                long latency,
                int bufferSize,
                uint pingNum)
                : this(source, destination, reply, pingNum)
            {
                _options = options;
                _latency = latency;
                _bufferSize = bufferSize;
            }

            /// <summary>
            /// Creates a new instance of the PingStatus class.
            /// </summary>
            /// <param name="source">The source machine name or IP of the ping.</param>
            /// <param name="destination">The destination machine name of the ping.</param>
            /// <param name="reply">The response from the ping attempt.</param>
            /// <param name="pingNum">The sequence number of the ping in the sequence of pings to the target.</param>
            internal PingStatus(string source, string destination, PingReply reply, uint pingNum)
            {
                Ping = pingNum;
                Reply = reply;
                Source = source;
                Destination = destination ?? reply.Address.ToString();
            }

            // These values should only be set if this PingStatus was created as part of a traceroute.
            private readonly int _bufferSize = -1;
            private readonly long _latency = -1;
            private readonly PingOptions _options;

            /// <summary>
            /// Gets the sequence number of this ping in the sequence of pings to the <see cref="Destination"/>
            /// </summary>
            public uint Ping { get; }

            /// <summary>
            /// Gets the source from which the ping was sent.
            /// </summary>
            public string Source { get; }

            /// <summary>
            /// Gets the destination which was pinged.
            /// </summary>
            public string Destination { get; }

            /// <summary>
            /// Gets the target address of the ping.
            /// </summary>
            /// <value></value>
            public IPAddress Address { get => Reply.Address; }

            /// <summary>
            /// Gets the roundtrip time of the ping in milliseconds.
            /// </summary>
            /// <value></value>
            public long Latency { get => _latency >= 0 ? _latency : Reply.RoundtripTime; }

            /// <summary>
            /// Gets the returned status of the ping.
            /// </summary>
            public IPStatus Status { get => Reply.Status; }

            /// <summary>
            /// Gets the size in bytes of the buffer data sent in the ping.
            /// </summary>
            public int BufferSize { get => _bufferSize >= 0 ? _bufferSize : Reply.Buffer.Length; }

            /// <summary>
            /// Gets the reply object from this ping.
            /// </summary>
            public PingReply Reply { get; }

            /// <summary>
            /// Gets the options used when sending the ping.
            /// </summary>
            public PingOptions Options { get => _options ?? Reply.Options; }
        }

        /// <summary>
        /// The class contains information about the source, the destination and ping results.
        /// </summary>
        public class PingMtuStatus : PingStatus
        {
            /// <summary>
            /// Creates a new instance of the PingStatus class.
            /// This constructor permits setting the MtuSize.
            /// </summary>
            /// <param name="source">The source machine name or IP of the ping.</param>
            /// <param name="destination">The destination machine name of the ping.</param>
            /// <param name="mtuSize">The maximum transmission unit size determined.</param>
            /// <param name="reply">The response from the ping attempt.</param>
            internal PingMtuStatus(string source, string destination, int mtuSize, PingReply reply)
                : base(source, destination, reply, 1)
            {
                if (mtuSize <= 0)
                {
                    throw new PSArgumentException(nameof(mtuSize));
                }

                MtuSize = mtuSize;
            }

            /// <summary>
            /// Gets the maximum transmission unit size on the network path between the source and destination.
            /// </summary>
            public int MtuSize { get; } = -1;
        }

        // Count of pings sent per each trace route hop.
        // Default = 3 (from Windows).
        // If we change 'DefaultTraceRoutePingCount' we should change 'ConsoleTraceRouteReply' resource string.
        private const int DefaultTraceRoutePingCount = 3;

        /// Create the default send buffer once and cache it.
        private const int DefaultSendBufferSize = 32;
        private static byte[] s_DefaultSendBuffer = null;

        private const string TestConnectionExceptionId = "TestConnectionException";
    }
}
