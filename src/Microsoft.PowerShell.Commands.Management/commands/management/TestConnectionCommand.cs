// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    [Cmdlet(VerbsDiagnostic.Test, "Connection", DefaultParameterSetName = DefaultPingParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097144")]
    [OutputType(typeof(PingStatus), ParameterSetName = new string[] { DefaultPingParameterSet })]
    [OutputType(typeof(PingStatus), ParameterSetName = new string[] { RepeatPingParameterSet, MtuSizeDetectParameterSet })]
    [OutputType(typeof(bool), ParameterSetName = new string[] { DefaultPingParameterSet, RepeatPingParameterSet, TcpPortParameterSet })]
    [OutputType(typeof(PingMtuStatus), ParameterSetName = new string[] { MtuSizeDetectParameterSet })]
    [OutputType(typeof(int), ParameterSetName = new string[] { MtuSizeDetectParameterSet })]
    [OutputType(typeof(TraceStatus), ParameterSetName = new string[] { TraceRouteParameterSet })]
    [OutputType(typeof(TcpPortStatus), ParameterSetName = new string[] { TcpPortParameterSet })]
    public class TestConnectionCommand : PSCmdlet, IDisposable
    {
        #region Parameter Set Names
        private const string DefaultPingParameterSet = "DefaultPing";
        private const string RepeatPingParameterSet = "RepeatPing";
        private const string TraceRouteParameterSet = "TraceRoute";
        private const string TcpPortParameterSet = "TcpPort";
        private const string MtuSizeDetectParameterSet = "MtuSizeDetect";

        #endregion

        #region Cmdlet Defaults

        // Count of pings sent to each trace route hop. Default mimics Windows' defaults.
        // If this value changes, we need to update 'ConsoleTraceRouteReply' resource string.
        private const uint DefaultTraceRoutePingCount = 3;

        // Default size for the send buffer.
        private const int DefaultSendBufferSize = 32;

        private const int DefaultMaxHops = 128;

        private const string TestConnectionExceptionId = "TestConnectionException";

        #endregion

        #region Private Fields

        private static readonly byte[] s_DefaultSendBuffer = Array.Empty<byte>();

        private readonly CancellationTokenSource _dnsLookupCancel = new();

        private bool _disposed;

        private Ping? _sender;

        #endregion

        #region Parameters

        /// <summary>
        /// Gets or sets whether to do ping test.
        /// Default is true.
        /// </summary>
        [Parameter(ParameterSetName = DefaultPingParameterSet)]
        [Parameter(ParameterSetName = RepeatPingParameterSet)]
        public SwitchParameter Ping { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to force use of IPv4 protocol.
        /// </summary>
        [Parameter(ParameterSetName = DefaultPingParameterSet)]
        [Parameter(ParameterSetName = RepeatPingParameterSet)]
        [Parameter(ParameterSetName = TraceRouteParameterSet)]
        [Parameter(ParameterSetName = MtuSizeDetectParameterSet)]
        [Parameter(ParameterSetName = TcpPortParameterSet)]
        public SwitchParameter IPv4 { get; set; }

        /// <summary>
        /// Gets or sets whether to force use of IPv6 protocol.
        /// </summary>
        [Parameter(ParameterSetName = DefaultPingParameterSet)]
        [Parameter(ParameterSetName = RepeatPingParameterSet)]
        [Parameter(ParameterSetName = TraceRouteParameterSet)]
        [Parameter(ParameterSetName = MtuSizeDetectParameterSet)]
        [Parameter(ParameterSetName = TcpPortParameterSet)]
        public SwitchParameter IPv6 { get; set; }

        /// <summary>
        /// Gets or sets whether to do reverse DNS lookup to get names for IP addresses.
        /// </summary>
        [Parameter(ParameterSetName = DefaultPingParameterSet)]
        [Parameter(ParameterSetName = RepeatPingParameterSet)]
        [Parameter(ParameterSetName = TraceRouteParameterSet)]
        [Parameter(ParameterSetName = MtuSizeDetectParameterSet)]
        [Parameter(ParameterSetName = TcpPortParameterSet)]
        public SwitchParameter ResolveDestination { get; set; }

        /// <summary>
        /// Gets the source from which to run the selected test.
        /// The default is localhost.
        /// Remoting is not yet implemented internally in the cmdlet.
        /// </summary>
        [Parameter(ParameterSetName = DefaultPingParameterSet)]
        [Parameter(ParameterSetName = RepeatPingParameterSet)]
        [Parameter(ParameterSetName = TraceRouteParameterSet)]
        [Parameter(ParameterSetName = TcpPortParameterSet)]
        public string Source { get; } = Dns.GetHostName();

        /// <summary>
        /// Gets or sets the number of times the Ping data packets can be forwarded by routers.
        /// As gateways and routers transmit packets through a network, they decrement the Time-to-Live (TTL)
        /// value found in the packet header.
        /// The default (from Windows) is 128 hops.
        /// </summary>
        [Parameter(ParameterSetName = DefaultPingParameterSet)]
        [Parameter(ParameterSetName = RepeatPingParameterSet)]
        [Parameter(ParameterSetName = TraceRouteParameterSet)]
        [ValidateRange(1, DefaultMaxHops)]
        [Alias("Ttl", "TimeToLive", "Hops")]
        public int MaxHops { get; set; } = DefaultMaxHops;

        /// <summary>
        /// Gets or sets the number of ping attempts.
        /// The default (from Windows) is 4 times.
        /// </summary>
        [Parameter(ParameterSetName = DefaultPingParameterSet)]
        [Parameter(ParameterSetName = TcpPortParameterSet)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int Count { get; set; } = 4;

        /// <summary>
        /// Gets or sets the delay between ping attempts.
        /// The default (from Windows) is 1 second.
        /// </summary>
        [Parameter(ParameterSetName = DefaultPingParameterSet)]
        [Parameter(ParameterSetName = RepeatPingParameterSet)]
        [Parameter(ParameterSetName = TcpPortParameterSet)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int Delay { get; set; } = 1;

        /// <summary>
        /// Gets or sets the buffer size to send with the ping packet.
        /// The default (from Windows) is 32 bytes.
        /// Max value is 65500 (limitation imposed by Windows API).
        /// </summary>
        [Parameter(ParameterSetName = DefaultPingParameterSet)]
        [Parameter(ParameterSetName = RepeatPingParameterSet)]
        [Alias("Size", "Bytes", "BS")]
        [ValidateRange(0, 65500)]
        public int BufferSize { get; set; } = DefaultSendBufferSize;

        /// <summary>
        /// Gets or sets whether to prevent fragmentation of the ICMP packets.
        /// Currently CoreFX not supports this on Unix.
        /// </summary>
        [Parameter(ParameterSetName = DefaultPingParameterSet)]
        [Parameter(ParameterSetName = RepeatPingParameterSet)]
        public SwitchParameter DontFragment { get; set; }

        /// <summary>
        /// Gets or sets whether to continue pinging until user presses Ctrl-C (or Int.MaxValue threshold reached).
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = RepeatPingParameterSet)]
        [Parameter(ParameterSetName = TcpPortParameterSet)]
        [Alias("Continuous")]
        public SwitchParameter Repeat { get; set; }

        /// <summary>
        /// Gets or sets whether to enable quiet output mode, reducing output to a single simple value only.
        /// By default, PingStatus, PingMtuStatus, or TraceStatus objects are emitted.
        /// With this switch, standard ping and -Traceroute returns only true / false, and -MtuSize returns an integer.
        /// </summary>
        [Parameter]
        public SwitchParameter Quiet { get; set; }

        /// <summary>
        /// Gets or sets whether to enable detailed output mode while running a TCP connection test.
        /// Without this flag, the TCP test will return a boolean result.
        /// </summary>
        [Parameter(ParameterSetName = TcpPortParameterSet)]
        public SwitchParameter Detailed;

        /// <summary>
        /// Gets or sets the timeout value for an individual ping in seconds.
        /// If a response is not received in this time, no response is assumed.
        /// The default (from Windows) is 5 seconds.
        /// </summary>
        [Parameter]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int TimeoutSeconds { get; set; } = 5;

        /// <summary>
        /// Gets or sets the destination hostname or IP address.
        /// </summary>
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [Alias("ComputerName")]
        public string[]? TargetName { get; set; }

        /// <summary>
        /// Gets or sets whether to detect Maximum Transmission Unit size.
        /// When selected, only a single ping result is returned, indicating the maximum buffer size
        /// the route to the destination can support without fragmenting the ICMP packets.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = MtuSizeDetectParameterSet)]
        [Alias("MtuSizeDetect")]
        public SwitchParameter MtuSize { get; set; }

        /// <summary>
        /// Gets or sets whether to perform a traceroute test.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = TraceRouteParameterSet)]
        public SwitchParameter Traceroute { get; set; }

        /// <summary>
        /// Gets or sets whether to perform a TCP connection test.
        /// </summary>
        [ValidateRange(0, 65535)]
        [Parameter(Mandatory = true, ParameterSetName = TcpPortParameterSet)]
        public int TcpPort { get; set; }

        #endregion Parameters

        /// <summary>
        /// BeginProcessing implementation for TestConnectionCommand.
        /// Sets Count for different types of tests unless specified explicitly.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (Repeat)
            {
                Count = int.MaxValue;
            }
            else if (ParameterSetName == TcpPortParameterSet)
            {
                SetCountForTcpTest();
            }
        }

        /// <summary>
        /// Process a connection test.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (TargetName == null)
            {
                return;
            }

            foreach (var targetName in TargetName)
            {

                if (MtuSize)
                {
                    ProcessMTUSize(targetName);
                }
                else if (Traceroute)
                {
                    ProcessTraceroute(targetName);
                }
                else if (ParameterSetName == TcpPortParameterSet)
                {
                    ProcessConnectionByTCPPort(targetName);
                }
                else
                {
                    // None of the switch parameters are true: handle default ping or -Repeat
                    ProcessPing(targetName);
                }
            }
        }

        /// <summary>
        /// On receiving the StopProcessing() request, the cmdlet will immediately cancel any in-progress ping request.
        /// This allows a cancellation to occur during a ping request without having to wait for the timeout.
        /// </summary>
        protected override void StopProcessing()
        {
            _sender?.SendAsyncCancel();
            _dnsLookupCancel.Cancel();
        }

        #region ConnectionTest

        private void SetCountForTcpTest()
        {
            if (Repeat)
            {
                Count = int.MaxValue;
            }
            else if (!MyInvocation.BoundParameters.ContainsKey(nameof(Count)))
            {
                Count = 1;
            }
        }

        private void ProcessConnectionByTCPPort(string targetNameOrAddress)
        {
            if (!TryResolveNameOrAddress(targetNameOrAddress, out _, out IPAddress? targetAddress))
            {
                if (Quiet.IsPresent)
                {
                    WriteObject(false);
                }

                return;
            }

            int timeoutMilliseconds = TimeoutSeconds * 1000;
            int delayMilliseconds = Delay * 1000;

            for (var i = 1; i <= Count; i++)
            {
                long latency = 0;
                SocketError status = SocketError.SocketError;

                Stopwatch stopwatch = new Stopwatch();

                using var client = new TcpClient();

                try
                {
                    stopwatch.Start();

                    if (client.ConnectAsync(targetAddress, TcpPort).Wait(timeoutMilliseconds, _dnsLookupCancel.Token))
                    {
                        latency = stopwatch.ElapsedMilliseconds;
                        status = SocketError.Success;
                    }
                    else
                    {
                        status = SocketError.TimedOut;
                    }
                }
                catch (AggregateException ae)
                {
                    ae.Handle((ex) =>
                    {
                        if (ex is TaskCanceledException)
                        {
                            throw new PipelineStoppedException();
                        }
                        if (ex is SocketException socketException)
                        {
                            status = socketException.SocketErrorCode;
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    });
                }
                finally
                {
                    stopwatch.Reset();
                }

                if (!Detailed.IsPresent)
                {
                    WriteObject(status == SocketError.Success);
                    return;
                }
                else
                {
                    WriteObject(new TcpPortStatus(
                        i,
                        Source,
                        targetNameOrAddress,
                        targetAddress,
                        TcpPort,
                        latency,
                        status == SocketError.Success,
                        status
                    ));
                }

                if (i < Count)
                {
                    Task.Delay(delayMilliseconds).Wait(_dnsLookupCancel.Token);
                }
            }
        }

        #endregion ConnectionTest

        #region TracerouteTest

        private void ProcessTraceroute(string targetNameOrAddress)
        {
            byte[] buffer = GetSendBuffer(BufferSize);

            if (!TryResolveNameOrAddress(targetNameOrAddress, out string resolvedTargetName, out IPAddress? targetAddress))
            {
                if (!Quiet.IsPresent)
                {
                    WriteObject(false);
                }

                return;
            }

            int currentHop = 1;
            PingOptions pingOptions = new(currentHop, DontFragment.IsPresent);
            PingReply reply;
            PingReply discoveryReply;
            int timeout = TimeoutSeconds * 1000;
            Stopwatch timer = new();

            IPAddress hopAddress;
            do
            {
                pingOptions.Ttl = currentHop;

#if !UNIX
                // Get intermediate hop target. This needs to be done first, so that we can target it properly
                // and get useful responses.
                var discoveryAttempts = 0;
                bool addressIsValid = false;
                do
                {
                    discoveryReply = SendCancellablePing(targetAddress, timeout, buffer, pingOptions);
                    discoveryAttempts++;
                    addressIsValid = !(discoveryReply.Address.Equals(IPAddress.Any)
                        || discoveryReply.Address.Equals(IPAddress.IPv6Any));
                }
                while (discoveryAttempts <= DefaultTraceRoutePingCount && addressIsValid);

                // If we aren't able to get a valid address, just re-target the final destination of the trace.
                hopAddress = addressIsValid ? discoveryReply.Address : targetAddress;
#else
                // Unix Ping API returns nonsense "TimedOut" for ALL intermediate hops. No way around this
                // issue for traceroutes as we rely on information (intermediate addresses, etc.) that is
                // simply not returned to us by the API.
                // The only supported states on Unix seem to be Success and TimedOut. Workaround is to
                // keep targeting the final address; at the very least we will be able to tell the user
                // the required number of hops to reach the destination.
                hopAddress = targetAddress;
                discoveryReply = SendCancellablePing(targetAddress, timeout, buffer, pingOptions);
#endif
                var hopAddressString = discoveryReply.Address.ToString();

                string routerName = hopAddressString;
                try
                {
                    if (!TryResolveNameOrAddress(hopAddressString, out routerName, out _))
                    {
                        routerName = hopAddressString;
                    }
                }
                catch
                {
                    // Swallow hostname resolve exceptions and continue with traceroute
                }

                // In traceroutes we don't use 'Count' parameter.
                // If we change 'DefaultTraceRoutePingCount' we should change 'ConsoleTraceRouteReply' resource string.
                for (uint i = 1; i <= DefaultTraceRoutePingCount; i++)
                {
                    try
                    {
                        reply = SendCancellablePing(hopAddress, timeout, buffer, pingOptions, timer);

                        if (!Quiet.IsPresent)
                        {
                            var status = new PingStatus(
                                Source,
                                routerName,
                                reply,
                                reply.Status == IPStatus.Success
                                    ? reply.RoundtripTime
                                    : timer.ElapsedMilliseconds,

                                // If we use the empty buffer, then .NET actually uses a 32 byte buffer so we want to show
                                // as the result object the actual buffer size used instead of 0.
                                buffer.Length == 0 ? DefaultSendBufferSize : buffer.Length,
                                pingNum: i);
                            WriteObject(new TraceStatus(
                                currentHop,
                                status,
                                Source,
                                resolvedTargetName,
                                targetAddress));
                        }
                    }
                    catch (PingException ex)
                    {
                        string message = StringUtil.Format(
                            TestConnectionResources.NoPingResult,
                            resolvedTargetName,
                            ex.Message);
                        Exception pingException = new PingException(message, ex.InnerException);
                        ErrorRecord errorRecord = new(
                            pingException,
                            TestConnectionExceptionId,
                            ErrorCategory.ResourceUnavailable,
                            resolvedTargetName);
                        WriteError(errorRecord);

                        continue;
                    }

                    // We use short delay because it is impossible DoS with trace route.
                    Thread.Sleep(50);
                    timer.Reset();
                }

                currentHop++;
            } while (currentHop <= MaxHops
                && (discoveryReply.Status == IPStatus.TtlExpired
                    || discoveryReply.Status == IPStatus.TimedOut));

            if (Quiet.IsPresent)
            {
                WriteObject(currentHop <= MaxHops);
            }
            else if (currentHop > MaxHops)
            {
                var message = StringUtil.Format(
                    TestConnectionResources.MaxHopsExceeded,
                    resolvedTargetName,
                    MaxHops);
                var pingException = new PingException(message);
                WriteError(new ErrorRecord(
                    pingException,
                    TestConnectionExceptionId,
                    ErrorCategory.ConnectionError,
                    targetAddress));
            }
        }

        #endregion TracerouteTest

        #region MTUSizeTest
        private void ProcessMTUSize(string targetNameOrAddress)
        {
            PingReply? reply, replyResult = null;
            if (!TryResolveNameOrAddress(targetNameOrAddress, out string resolvedTargetName, out IPAddress? targetAddress))
            {
                if (Quiet.IsPresent)
                {
                    WriteObject(-1);
                }

                return;
            }

            // Caution! Algorithm is sensitive to changing boundary values.
            int HighMTUSize = 10000;
            int CurrentMTUSize = 1473;
            int LowMTUSize = targetAddress.AddressFamily == AddressFamily.InterNetworkV6 ? 1280 : 68;
            int timeout = TimeoutSeconds * 1000;

            PingReply? timeoutReply = null;

            try
            {
                PingOptions pingOptions = new(MaxHops, true);
                int retry = 1;

                while (LowMTUSize < (HighMTUSize - 1))
                {
                    byte[] buffer = GetSendBuffer(CurrentMTUSize);

                    WriteDebug(StringUtil.Format(
                        "LowMTUSize: {0}, CurrentMTUSize: {1}, HighMTUSize: {2}",
                        LowMTUSize,
                        CurrentMTUSize,
                        HighMTUSize));

                    reply = SendCancellablePing(targetAddress, timeout, buffer, pingOptions);

                    if (reply.Status == IPStatus.PacketTooBig || reply.Status == IPStatus.TimedOut)
                    {
                        HighMTUSize = CurrentMTUSize;
                        timeoutReply = reply;
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
                        // If the host didn't reply, try again up to the 'Count' value.
                        if (retry >= Count)
                        {
                            string message = StringUtil.Format(
                                TestConnectionResources.NoPingResult,
                                targetAddress,
                                reply.Status.ToString());
                            Exception pingException = new PingException(message);
                            ErrorRecord errorRecord = new(
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
                Exception pingException = new PingException(message, ex.InnerException);
                ErrorRecord errorRecord = new(
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
                if (replyResult is null)
                {
                    if (timeoutReply is not null)
                    {
                        Exception timeoutException = new TimeoutException(targetAddress.ToString());
                        ErrorRecord errorRecord = new(
                            timeoutException,
                            TestConnectionExceptionId,
                            ErrorCategory.ResourceUnavailable,
                            timeoutReply);
                        WriteError(errorRecord);
                    }
                    else
                    {
                        ArgumentNullException.ThrowIfNull(replyResult);
                    }
                }
                else
                {
                    WriteObject(new PingMtuStatus(
                        Source,
                        resolvedTargetName,
                        replyResult,
                        CurrentMTUSize));
                }

            }
        }

        #endregion MTUSizeTest

        #region PingTest

        private void ProcessPing(string targetNameOrAddress)
        {
            if (!TryResolveNameOrAddress(targetNameOrAddress, out string resolvedTargetName, out IPAddress? targetAddress))
            {
                if (Quiet.IsPresent)
                {
                    WriteObject(false);
                }

                return;
            }

            bool quietResult = true;
            byte[] buffer = GetSendBuffer(BufferSize);

            PingReply reply;
            PingOptions pingOptions = new(MaxHops, DontFragment.IsPresent);
            int timeout = TimeoutSeconds * 1000;
            int delay = Delay * 1000;

            for (int i = 1; i <= Count; i++)
            {
                try
                {
                    reply = SendCancellablePing(targetAddress, timeout, buffer, pingOptions);
                }
                catch (PingException ex)
                {
                    string message = StringUtil.Format(TestConnectionResources.NoPingResult, resolvedTargetName, ex.Message);
                    Exception pingException = new PingException(message, ex.InnerException);
                    ErrorRecord errorRecord = new(
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
                    WriteObject(new PingStatus(
                        Source,
                        resolvedTargetName,
                        reply,
                        reply.RoundtripTime,
                        buffer.Length == 0 ? DefaultSendBufferSize : buffer.Length,
                        pingNum: (uint)i));
                }

                // Delay between pings, but not after last ping.
                if (i < Count && Delay > 0)
                {
                    Thread.Sleep(delay);
                }
            }

            if (Quiet.IsPresent)
            {
                WriteObject(quietResult);
            }
        }

        #endregion PingTest

        private bool TryResolveNameOrAddress(
            string targetNameOrAddress,
            out string resolvedTargetName,
            [NotNullWhen(true)]
            out IPAddress? targetAddress)
        {
            resolvedTargetName = targetNameOrAddress;

            IPHostEntry hostEntry;
            if (IPAddress.TryParse(targetNameOrAddress, out targetAddress))
            {
                if ((IPv4 && targetAddress.AddressFamily != AddressFamily.InterNetwork)
                    || (IPv6 && targetAddress.AddressFamily != AddressFamily.InterNetworkV6))
                {
                    string message = StringUtil.Format(
                        TestConnectionResources.NoPingResult,
                        resolvedTargetName,
                        TestConnectionResources.TargetAddressAbsent);
                    Exception pingException = new PingException(message, null);
                    ErrorRecord errorRecord = new(
                        pingException,
                        TestConnectionExceptionId,
                        ErrorCategory.ResourceUnavailable,
                        resolvedTargetName);
                    WriteError(errorRecord);
                    return false;
                }

                if (ResolveDestination)
                {
                    hostEntry = GetCancellableHostEntry(targetNameOrAddress);
                    resolvedTargetName = hostEntry.HostName;
                }
                else
                {
                    resolvedTargetName = targetAddress.ToString();
                }
            }
            else
            {
                try
                {
                    hostEntry = GetCancellableHostEntry(targetNameOrAddress);

                    if (ResolveDestination)
                    {
                        resolvedTargetName = hostEntry.HostName;
                        hostEntry = GetCancellableHostEntry(hostEntry.HostName);
                    }
                }
                catch (PipelineStoppedException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (!Quiet.IsPresent)
                    {
                        string message = StringUtil.Format(
                            TestConnectionResources.NoPingResult,
                            resolvedTargetName,
                            TestConnectionResources.CannotResolveTargetName);
                        Exception pingException = new PingException(message, ex);
                        ErrorRecord errorRecord = new(
                            pingException,
                            TestConnectionExceptionId,
                            ErrorCategory.ResourceUnavailable,
                            resolvedTargetName);
                        WriteError(errorRecord);
                    }

                    return false;
                }

                if (IPv6 || IPv4)
                {
                    targetAddress = GetHostAddress(hostEntry);

                    if (targetAddress == null)
                    {
                        string message = StringUtil.Format(
                            TestConnectionResources.NoPingResult,
                            resolvedTargetName,
                            TestConnectionResources.TargetAddressAbsent);
                        Exception pingException = new PingException(message, null);
                        ErrorRecord errorRecord = new(
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

        private IPHostEntry GetCancellableHostEntry(string targetNameOrAddress)
        {
            var task = Dns.GetHostEntryAsync(targetNameOrAddress);
            var waitHandles = new[] { ((IAsyncResult)task).AsyncWaitHandle, _dnsLookupCancel.Token.WaitHandle };

            // WaitAny() returns the index of the first signal it gets; 1 is our cancellation token.
            if (WaitHandle.WaitAny(waitHandles) == 1)
            {
                throw new PipelineStoppedException();
            }

            return task.GetAwaiter().GetResult();
        }

        private IPAddress? GetHostAddress(IPHostEntry hostEntry)
        {
            AddressFamily addressFamily = IPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;

            foreach (var address in hostEntry.AddressList)
            {
                if (address.AddressFamily == addressFamily)
                {
                    return address;
                }
            }

            return null;
        }

        // Users most often use the default buffer size so we cache the buffer.
        // Creates and fills a send buffer. This follows the ping.exe and CoreFX model.
        private static byte[] GetSendBuffer(int bufferSize)
        {
            if (bufferSize == DefaultSendBufferSize)
            {
                return s_DefaultSendBuffer;
            }

            byte[] sendBuffer = new byte[bufferSize];

            for (int i = 0; i < bufferSize; i++)
            {
                sendBuffer[i] = (byte)((int)'a' + i % 23);
            }

            return sendBuffer;
        }

        /// <summary>
        /// IDisposable implementation, dispose of any disposable resources created by the cmdlet.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Implementation of IDisposable for both manual Dispose() and finalizer-called disposal of resources.
        /// </summary>
        /// <param name="disposing">
        /// Specified as true when Dispose() was called, false if this is called from the finalizer.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _sender?.Dispose();
                    _dnsLookupCancel.Dispose();
                }

                _disposed = true;
            }
        }

        // Uses the SendAsync() method to send pings, so that Ctrl+C can halt the request early if needed.
        private PingReply SendCancellablePing(
            IPAddress targetAddress,
            int timeout,
            byte[] buffer,
            PingOptions pingOptions,
            Stopwatch? timer = null)
        {
            try
            {
                _sender = new Ping();

                timer?.Start();
                // 'SendPingAsync' always uses the default synchronization context (threadpool).
                // This is what we want to avoid the deadlock resulted by async work being scheduled back to the
                // pipeline thread due to a change of the current synchronization context of the pipeline thread.
                return _sender.SendPingAsync(targetAddress, timeout, buffer, pingOptions).GetAwaiter().GetResult();
            }
            catch (PingException ex) when (ex.InnerException is TaskCanceledException)
            {
                // The only cancellation we have implemented is on pipeline stops via StopProcessing().
                throw new PipelineStoppedException();
            }
            finally
            {
                timer?.Stop();
                _sender?.Dispose();
                _sender = null;
            }
        }

        /// <summary>
        /// The class contains information about the TCP connection test.
        /// </summary>
        public class TcpPortStatus
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TcpPortStatus"/> class.
            /// </summary>
            /// <param name="id">The number of this test.</param>
            /// <param name="source">The source machine name or IP of the test.</param>
            /// <param name="target">The target machine name or IP of the test.</param>
            /// <param name="targetAddress">The resolved IP from the target.</param>
            /// <param name="port">The port used for the connection.</param>
            /// <param name="latency">The latency of the test.</param>
            /// <param name="connected">If the test connection succeeded.</param>
            /// <param name="status">Status of the underlying socket.</param>
            internal TcpPortStatus(int id, string source, string target, IPAddress targetAddress, int port, long latency, bool connected, SocketError status)
            {
                Id = id;
                Source = source;
                Target = target;
                TargetAddress = targetAddress;
                Port = port;
                Latency = latency;
                Connected = connected;
                Status = status;
            }

            /// <summary>
            /// Gets and sets the count of the test.
            /// </summary>
            public int Id { get; set; }

            /// <summary>
            /// Gets the source from which the test was sent.
            /// </summary>
            public string Source { get; }

            /// <summary>
            /// Gets the target name.
            /// </summary>
            public string Target { get; }

            /// <summary>
            /// Gets the resolved address for the target.
            /// </summary>
            public IPAddress TargetAddress { get; }

            /// <summary>
            /// Gets the port used for the test.
            /// </summary>
            public int Port { get; }

            /// <summary>
            /// Gets or sets the latancy of the connection.
            /// </summary>
            public long Latency { get; set; }

            /// <summary>
            /// Gets or sets the result of the test.
            /// </summary>
            public bool Connected { get; set; }

            /// <summary>
            /// Gets or sets the state of the socket after the test.
            /// </summary>
            public SocketError Status { get; set; }
        }

        /// <summary>
        /// The class contains information about the source, the destination and ping results.
        /// </summary>
        public class PingStatus
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PingStatus"/> class.
            /// This constructor allows manually specifying the initial values for the cases where the PingReply
            /// object may be missing some information, specifically in the instances where PingReply objects are
            /// utilised to perform a traceroute.
            /// </summary>
            /// <param name="source">The source machine name or IP of the ping.</param>
            /// <param name="destination">The destination machine name of the ping.</param>
            /// <param name="reply">The response from the ping attempt.</param>
            /// <param name="latency">The latency of the ping.</param>
            /// <param name="bufferSize">The buffer size.</param>
            /// <param name="pingNum">The sequence number in the sequence of pings to the hop point.</param>
            internal PingStatus(
                string source,
                string destination,
                PingReply reply,
                long latency,
                int bufferSize,
                uint pingNum)
                : this(source, destination, reply, pingNum)
            {
                _bufferSize = bufferSize;
                _latency = latency;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="PingStatus"/> class.
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
                Destination = destination;
            }

            // These values can be set manually to skirt issues with the Ping API on Unix platforms
            // so that we can return meaningful known data that is discarded by the API.
            private readonly int _bufferSize = -1;

            private readonly long _latency = -1;

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
            public IPAddress? Address { get => Reply.Status == IPStatus.Success ? Reply.Address : null; }

            /// <summary>
            /// Gets the target address of the ping if one is available, or "*" if it is not.
            /// </summary>
            public string DisplayAddress { get => Address?.ToString() ?? "*"; }

            /// <summary>
            /// Gets the roundtrip time of the ping in milliseconds.
            /// </summary>
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
        }

        /// <summary>
        /// The class contains information about the source, the destination and ping results.
        /// </summary>
        public class PingMtuStatus : PingStatus
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PingMtuStatus"/> class.
            /// </summary>
            /// <param name="source">The source machine name or IP of the ping.</param>
            /// <param name="destination">The destination machine name of the ping.</param>
            /// <param name="reply">The response from the ping attempt.</param>
            /// <param name="bufferSize">The buffer size from the successful ping attempt.</param>
            internal PingMtuStatus(string source, string destination, PingReply reply, int bufferSize)
                : base(source, destination, reply, 1)
            {
                MtuSize = bufferSize;
            }

            /// <summary>
            /// Gets the maximum transmission unit size on the network path between the source and destination.
            /// </summary>
            public int MtuSize { get; }
        }

        /// <summary>
        /// The class contains an information about a trace route attempt.
        /// </summary>
        public class TraceStatus
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TraceStatus"/> class.
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

                if (_status.Address == IPAddress.Any
                    || _status.Address == IPAddress.IPv6Any)
                {
                    Hostname = null;
                }
                else
                {
                    Hostname = _status.Destination;
                }
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
            public string? Hostname { get; }

            /// <summary>
            /// Gets the sequence number of the ping in the sequence of pings to the hop point.
            /// </summary>
            public uint Ping { get => _status.Ping; }

            /// <summary>
            /// Gets the IP address of the current hop point.
            /// </summary>
            public IPAddress? HopAddress { get => _status.Address; }

            /// <summary>
            /// Gets the latency values of each ping to the current hop point.
            /// </summary>
            public long Latency { get => _status.Latency; }

            /// <summary>
            /// Gets the status of the traceroute hop.
            /// </summary>
            public IPStatus Status { get => _status.Status; }

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
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="TestConnectionCommand"/> class.
        /// </summary>
        ~TestConnectionCommand()
        {
            Dispose(disposing: false);
        }
    }
}
