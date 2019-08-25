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
    [Cmdlet(VerbsDiagnostic.Test, "Connection", DefaultParameterSetName = DefaultPingSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135266")]
    [OutputType(typeof(PingStatus), ParameterSetName = new[] { DefaultPingSet })]
    [OutputType(typeof(PingReply), ParameterSetName = new[] { RepeatPingSet, MtuSizeDetectSet })]
    [OutputType(typeof(bool), ParameterSetName = new[] { DefaultPingSet, RepeatPingSet, TcpPortSet })]
    [OutputType(typeof(int), ParameterSetName = new[] { MtuSizeDetectSet })]
    [OutputType(typeof(TraceStatus), ParameterSetName = new[] { TraceRouteSet })]
    public class TestConnectionCommand : PSCmdlet, IDisposable
    {
        private const string DefaultPingSet = "DefaultPing";
        private const string RepeatPingSet = "RepeatPing";
        private const string TraceRouteSet = "TraceRoute";
        private const string TcpPortSet = "TcpPort";
        private const string MtuSizeDetectSet = "MtuSizeDetect";

        #region Parameters

        /// <summary>
        /// Do ping test.
        /// </summary>
        [Parameter(ParameterSetName = DefaultPingSet)]
        [Parameter(ParameterSetName = RepeatPingSet)]
        public SwitchParameter Ping { get; set; } = true;

        /// <summary>
        /// Force using IPv4 protocol.
        /// </summary>
        [Parameter]
        public SwitchParameter IPv4 { get; set; }

        /// <summary>
        /// Force using IPv6 protocol.
        /// </summary>
        [Parameter]
        public SwitchParameter IPv6 { get; set; }

        /// <summary>
        /// Do reverse DNS lookup to get names for IP addresses.
        /// </summary>
        [Parameter]
        public SwitchParameter ResolveDestination { get; set; }

        /// <summary>
        /// Source from which to do a test (ping, trace route, ...).
        /// The default is Local Host.
        /// Remoting is not yet implemented internally in the cmdlet.
        /// </summary>
        [Parameter(ParameterSetName = DefaultPingSet)]
        [Parameter(ParameterSetName = RepeatPingSet)]
        [Parameter(ParameterSetName = TraceRouteSet)]
        [Parameter(ParameterSetName = TcpPortSet)]
        public string Source { get; } = Dns.GetHostName();

        /// <summary>
        /// The number of times the Ping data packets can be forwarded by routers.
        /// As gateways and routers transmit packets through a network,
        /// they decrement the Time-to-Live (TTL) value found in the packet header.
        /// The default (from Windows) is 128 hops.
        /// </summary>
        [Parameter(ParameterSetName = DefaultPingSet)]
        [Parameter(ParameterSetName = RepeatPingSet)]
        [Parameter(ParameterSetName = TraceRouteSet)]
        [ValidateRange(0, sMaxHops)]
        [Alias("Ttl", "TimeToLive", "Hops")]
        public int MaxHops { get; set; } = sMaxHops;

        private const int sMaxHops = 128;

        /// <summary>
        /// Count of attempts.
        /// The default (from Windows) is 4 times.
        /// </summary>
        [Parameter(ParameterSetName = DefaultPingSet)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int Count { get; set; } = 4;

        /// <summary>
        /// Delay between attempts.
        /// The default (from Windows) is 1 second.
        /// </summary>
        [Parameter(ParameterSetName = DefaultPingSet)]
        [Parameter(ParameterSetName = RepeatPingSet)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int Delay { get; set; } = 1;

        /// <summary>
        /// Buffer size to send.
        /// The default (from Windows) is 32 bites.
        /// Max value is 65500 (limit from Windows API).
        /// </summary>
        [Parameter(ParameterSetName = DefaultPingSet)]
        [Parameter(ParameterSetName = RepeatPingSet)]
        [Alias("Size", "Bytes", "BS")]
        [ValidateRange(0, 65500)]
        public int BufferSize { get; set; } = DefaultSendBufferSize;

        /// <summary>
        /// Don't fragment ICMP packages.
        /// Currently CoreFX not supports this on Unix.
        /// </summary>
        [Parameter(ParameterSetName = DefaultPingSet)]
        [Parameter(ParameterSetName = RepeatPingSet)]
        public SwitchParameter DontFragment { get; set; }

        /// <summary>
        /// Continue ping until user press Ctrl-C
        /// or Int.MaxValue threshold reached.
        /// </summary>
        [Parameter(ParameterSetName = RepeatPingSet)]
        public SwitchParameter Continues { get; set; }

        /// <summary>
        /// Set short output kind ('bool' for Ping, 'int' for MTU size ...).
        /// Default is to return typed result object(s).
        /// </summary>
        [Parameter]
        public SwitchParameter Quiet;

        /// <summary>
        /// Time-out value in seconds.
        /// If a response is not received in this time, no response is assumed.
        /// It is not the cmdlet timeout! It is a timeout for waiting one ping response.
        /// The default (from Windows) is 5 second.
        /// </summary>
        [Parameter]
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
        [Parameter(Mandatory = true, ParameterSetName = MtuSizeDetectSet)]
        public SwitchParameter MTUSizeDetect { get; set; }

        /// <summary>
        /// Do traceroute test.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = TraceRouteSet)]
        public SwitchParameter Traceroute { get; set; }

        /// <summary>
        /// Do tcp connection test.
        /// </summary>
        [ValidateRange(0, 65535)]
        [Parameter(Mandatory = true, ParameterSetName = TcpPortSet)]
        public int TCPPort { get; set; }

        #endregion Parameters

        /// <summary>
        /// Init the cmdlet.
        /// </summary>
        protected override void BeginProcessing()
        {
            _sender.PingCompleted += OnPingComplete;

            switch (ParameterSetName)
            {
                case RepeatPingSet:
                    Count = int.MaxValue;
                    break;
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
                    case DefaultPingSet:
                    case RepeatPingSet:
                        ProcessPing(targetName);
                        break;
                    case MtuSizeDetectSet:
                        ProcessMTUSize(targetName);
                        break;
                    case TraceRouteSet:
                        ProcessTraceroute(targetName);
                        break;
                    case TcpPortSet:
                        ProcessConnectionByTCPPort(targetName);
                        break;
                }
            }
        }

        /// <summary>
        /// On receiving the StopProcessing() request, the cmdlet will immediately cancel any in-progress ping request.
        /// This allows a cancellation to occur during a ping request without having to wait for a potentially very
        /// long timeout.
        /// </summary>
        protected override void StopProcessing()
        {
            _sender?.SendAsyncCancel();
        }

        #region ConnectionTest

        private void ProcessConnectionByTCPPort(string targetNameOrAddress)
        {
            string resolvedTargetName;
            IPAddress targetAddress;
            if (!InitProcessPing(targetNameOrAddress, out resolvedTargetName, out targetAddress))
            {
                return;
            }

            WriteConnectionTestHeader(resolvedTargetName, targetAddress.ToString());

            TcpClient client = new TcpClient();

            try
            {
                Task connectionTask = client.ConnectAsync(targetAddress, TCPPort);
                string targetString = targetAddress.ToString();

                for (var i = 1; i <= TimeoutSeconds; i++)
                {
                    WriteConnectionTestProgress(targetNameOrAddress, targetString, i);

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
                WriteConnectionTestFooter();
            }

            WriteObject(false);
        }

        private void WriteConnectionTestHeader(string resolvedTargetName, string targetAddress)
        {
            _testConnectionProgressBarActivity = StringUtil.Format(TestConnectionResources.ConnectionTestStart, resolvedTargetName, targetAddress);
            ProgressRecord record = new ProgressRecord(s_ProgressId, _testConnectionProgressBarActivity, ProgressRecordSpace);
            WriteProgress(record);
        }

        private void WriteConnectionTestProgress(string targetNameOrAddress, string targetAddress, int timeout)
        {
            var msg = StringUtil.Format(
                TestConnectionResources.ConnectionTestDescription,
                targetNameOrAddress,
                targetAddress,
                timeout);
            ProgressRecord record = new ProgressRecord(s_ProgressId, _testConnectionProgressBarActivity, msg);
            WriteProgress(record);
        }

        private void WriteConnectionTestFooter()
        {
            var record = new ProgressRecord(s_ProgressId, _testConnectionProgressBarActivity, ProgressRecordSpace)
            {
                RecordType = ProgressRecordType.Completed
            };
            WriteProgress(record);
        }

        #endregion ConnectionTest

        #region TracerouteTest
        private void ProcessTraceroute(string targetNameOrAddress)
        {
            byte[] buffer = GetSendBuffer(BufferSize);

            string resolvedTargetName;
            IPAddress targetAddress;
            if (!InitProcessPing(targetNameOrAddress, out resolvedTargetName, out targetAddress))
            {
                return;
            }

            WriteTraceRouteHeader(resolvedTargetName, targetAddress.ToString());

            int currentHop = 1;
            PingOptions pingOptions = new PingOptions(currentHop, DontFragment.IsPresent);
            PingReply reply = null;
            int timeout = TimeoutSeconds * 1000;
            var timer = new Stopwatch();

            do
            {
                // Clear the stored router name for every hop
                string routerName = null;
                TraceStatus hopResult = null;
                pingOptions.Ttl = currentHop;
                currentHop++;

                var hopReplies = new List<TraceStatus>(DefaultTraceRoutePingCount);

                // In the specific case we don't use 'Count' property.
                // If we change 'DefaultTraceRoutePingCount' we should change 'ConsoleTraceRouteReply' resource string.
                for (uint i = 1; i <= DefaultTraceRoutePingCount; i++)
                {
                    try
                    {
                        reply = SendCancellablePing(targetAddress, timeout, buffer, pingOptions, timer);

                        // Only get router name if we haven't already retrieved it
                        if (routerName == null)
                        {
                            if (ResolveDestination.IsPresent)
                            {
                                try
                                {
                                    routerName = reply.Status == IPStatus.Success
                                        ? Dns.GetHostEntry(reply.Address).HostName
                                        : reply.Address?.ToString();
                                }
                                catch
                                {
                                    // Swallow hostname resolution errors and continue with trace
                                }
                            }
                            else
                            {
                                routerName = reply.Address?.ToString();
                            }
                        }

                        var status = new PingStatus(
                            Source,
                            routerName,
                            reply,
                            pingOptions,
                            latency: reply.Status == IPStatus.Success ? reply.RoundtripTime : timer.ElapsedMilliseconds,
                            buffer.Length,
                            pingNum: i);
                        hopResult = new TraceStatus(currentHop, status, Source, resolvedTargetName, targetAddress);

                        if (!Quiet.IsPresent)
                        {
                            WriteObject(hopResult);
                        }

                        timer.Reset();
                    }
                    catch (PingException ex)
                    {
                        string message = StringUtil.Format(
                            TestConnectionResources.NoPingResult,
                            resolvedTargetName,
                            ex.Message);
                        Exception pingException = new PingException(message, ex.InnerException);
                        ErrorRecord errorRecord = new ErrorRecord(
                            pingException,
                            TestConnectionExceptionId,
                            ErrorCategory.ResourceUnavailable,
                            resolvedTargetName);
                        WriteError(errorRecord);

                        continue;
                    }

                    hopReplies.Add(hopResult);
                    // We use short delay because it is impossible DoS with trace route.
                    Thread.Sleep(50);
                }

                WriteTraceRouteProgress(hopReplies);

            } while (reply != null
                && currentHop <= sMaxHops
                && (reply.Status == IPStatus.TtlExpired || reply.Status == IPStatus.TimedOut));

            WriteTraceRouteFooter();

            if (Quiet.IsPresent)
            {
                WriteObject(currentHop <= sMaxHops);
            }
        }

        private void WriteTraceRouteHeader(string resolvedTargetName, string targetAddress)
        {
            _testConnectionProgressBarActivity = StringUtil.Format(
                TestConnectionResources.TraceRouteStart,
                resolvedTargetName,
                targetAddress,
                MaxHops);

            WriteVerbose(_testConnectionProgressBarActivity);

            ProgressRecord record = new ProgressRecord(s_ProgressId, _testConnectionProgressBarActivity, ProgressRecordSpace);
            WriteProgress(record);
        }

        private string _testConnectionProgressBarActivity;

        private void WriteTraceRouteProgress(IList<TraceStatus> traceRouteReplies)
        {
            string msg;
            if (traceRouteReplies[2].Status == IPStatus.Success)
            {
                var routerAddress = traceRouteReplies[2].HopAddress.ToString();
                var routerName = traceRouteReplies[2].Hostname ?? routerAddress;
                var roundtripTime0 = traceRouteReplies[0].Status == IPStatus.TimedOut
                    ? "*"
                    : traceRouteReplies[0].Latency.ToString();
                var roundtripTime1 = traceRouteReplies[1].Status == IPStatus.TimedOut
                    ? "*"
                    : traceRouteReplies[1].Latency.ToString();
                msg = StringUtil.Format(
                    TestConnectionResources.TraceRouteReply,
                    traceRouteReplies[0].Hop,
                    roundtripTime0,
                    roundtripTime1,
                    traceRouteReplies[2].Latency.ToString(),
                    routerName,
                    routerAddress);
            }
            else
            {
                msg = StringUtil.Format(TestConnectionResources.TraceRouteTimeOut, traceRouteReplies[0].Hop);
            }

            WriteVerbose(msg);

            ProgressRecord record = new ProgressRecord(s_ProgressId, _testConnectionProgressBarActivity, msg);
            WriteProgress(record);
        }

        private void WriteTraceRouteFooter()
        {
            WriteVerbose(TestConnectionResources.TraceRouteComplete);

            var record = new ProgressRecord(s_ProgressId, _testConnectionProgressBarActivity, ProgressRecordSpace)
            {
                RecordType = ProgressRecordType.Completed
            };
            WriteProgress(record);
        }

        #endregion TracerouteTest

        #region MTUSizeTest
        private void ProcessMTUSize(string targetNameOrAddress)
        {
            PingReply reply, replyResult = null;
            string resolvedTargetName;
            IPAddress targetAddress;
            if (!InitProcessPing(targetNameOrAddress, out resolvedTargetName, out targetAddress))
            {
                return;
            }

            WriteMTUSizeHeader(resolvedTargetName, targetAddress.ToString());

            // Cautious! Algorithm is sensitive to changing boundary values.
            int HighMTUSize = 10000;
            int CurrentMTUSize = 1473;
            int LowMTUSize = targetAddress.AddressFamily == AddressFamily.InterNetworkV6 ? 1280 : 68;
            int timeout = TimeoutSeconds * 1000;

            try
            {
                PingOptions pingOptions = new PingOptions(MaxHops, true);
                int retry = 1;

                while (LowMTUSize < (HighMTUSize - 1))
                {
                    byte[] buffer = GetSendBuffer(CurrentMTUSize);

                    WriteMTUSizeProgress(CurrentMTUSize, retry);

                    WriteDebug(StringUtil.Format(
                        "LowMTUSize: {0}, CurrentMTUSize: {1}, HighMTUSize: {2}",
                        LowMTUSize,
                        CurrentMTUSize,
                        HighMTUSize));

                    reply = SendCancellablePing(targetAddress, timeout, buffer, pingOptions);

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
                            Exception pingException = new PingException(message);
                            ErrorRecord errorRecord = new ErrorRecord(
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
                ErrorRecord errorRecord = new ErrorRecord(
                    pingException,
                    TestConnectionExceptionId,
                    ErrorCategory.ResourceUnavailable,
                    targetAddress);
                WriteError(errorRecord);
                return;
            }

            WriteMTUSizeFooter();

            if (Quiet.IsPresent)
            {
                WriteObject(CurrentMTUSize);
            }
            else
            {
                var res = PSObject.AsPSObject(replyResult);

                PSMemberInfo sourceProperty = new PSNoteProperty("Source", Source);
                res.Members.Add(sourceProperty);
                PSMemberInfo destinationProperty = new PSNoteProperty("Destination", targetNameOrAddress);
                res.Members.Add(destinationProperty);
                PSMemberInfo mtuSizeProperty = new PSNoteProperty("MTUSize", CurrentMTUSize);
                res.Members.Add(mtuSizeProperty);
                res.TypeNames.Insert(0, "PingReply#MTUSize");

                WriteObject(res);
            }
        }

        private void WriteMTUSizeHeader(string resolvedTargetName, string targetAddress)
        {
            _testConnectionProgressBarActivity = StringUtil.Format(
                TestConnectionResources.MTUSizeDetectStart,
                resolvedTargetName,
                targetAddress,
                BufferSize);

            ProgressRecord record = new ProgressRecord(s_ProgressId, _testConnectionProgressBarActivity, ProgressRecordSpace);
            WriteProgress(record);
        }

        private void WriteMTUSizeProgress(int currentMTUSize, int retry)
        {
            var msg = StringUtil.Format(TestConnectionResources.MTUSizeDetectDescription, currentMTUSize, retry);

            ProgressRecord record = new ProgressRecord(s_ProgressId, _testConnectionProgressBarActivity, msg);
            WriteProgress(record);
        }

        private void WriteMTUSizeFooter()
        {
            var record = new ProgressRecord(s_ProgressId, _testConnectionProgressBarActivity, ProgressRecordSpace)
            {
                RecordType = ProgressRecordType.Completed
            };
            WriteProgress(record);
        }

        #endregion MTUSizeTest

        #region PingTest

        private void ProcessPing(string targetNameOrAddress)
        {
            string resolvedTargetName;
            IPAddress targetAddress;
            if (!InitProcessPing(targetNameOrAddress, out resolvedTargetName, out targetAddress))
            {
                return;
            }

            if (!Continues.IsPresent)
            {
                WritePingHeader(resolvedTargetName, targetAddress.ToString());
            }

            bool quietResult = true;
            byte[] buffer = GetSendBuffer(BufferSize);

            PingReply reply;
            PingOptions pingOptions = new PingOptions(MaxHops, DontFragment.IsPresent);
            int timeout = TimeoutSeconds * 1000;
            int delay = Delay * 1000;

            for (uint i = 1; i <= Count; i++)
            {
                try
                {
                    reply = SendCancellablePing(targetAddress, timeout, buffer, pingOptions);
                }
                catch (PingException ex)
                {
                    string message = StringUtil.Format(TestConnectionResources.NoPingResult, resolvedTargetName, ex.Message);
                    Exception pingException = new PingException(message, ex.InnerException);
                    ErrorRecord errorRecord = new ErrorRecord(
                        pingException,
                        TestConnectionExceptionId,
                        ErrorCategory.ResourceUnavailable,
                        resolvedTargetName);
                    WriteError(errorRecord);

                    quietResult = false;
                    continue;
                }

                if (Continues.IsPresent)
                {
                    WriteObject(reply);
                }
                else
                {
                    if (Quiet.IsPresent)
                    {
                        // Return 'true' only if all pings have completed successfully.
                        quietResult &= reply.Status == IPStatus.Success;
                    }
                    else
                    {
                        WriteObject(new PingStatus(Source, resolvedTargetName, reply, i));
                    }

                    WritePingProgress(reply);
                }

                // Delay between ping but not after last ping.
                if (i < Count && Delay > 0)
                {
                    Thread.Sleep(delay);
                }
            }

            if (!Continues.IsPresent)
            {
                WritePingFooter();
            }

            if (Quiet.IsPresent)
            {
                WriteObject(quietResult);
            }
        }

        private void WritePingHeader(string resolvedTargetName, string targetAddress)
        {
            _testConnectionProgressBarActivity = StringUtil.Format(
                TestConnectionResources.MTUSizeDetectStart,
                resolvedTargetName,
                targetAddress,
                BufferSize);

            WriteVerbose(_testConnectionProgressBarActivity);

            ProgressRecord record = new ProgressRecord(
                s_ProgressId,
                _testConnectionProgressBarActivity,
                ProgressRecordSpace);
            WriteProgress(record);
        }

        private void WritePingProgress(PingReply reply)
        {
            string msg;
            if (reply.Status != IPStatus.Success)
            {
                msg = TestConnectionResources.PingTimeOut;
            }
            else
            {
                msg = StringUtil.Format(
                    TestConnectionResources.PingReply,
                    reply.Address.ToString(),
                    reply.Buffer.Length,
                    reply.RoundtripTime,
                    reply.Options?.Ttl);
            }

            WriteVerbose(msg);

            ProgressRecord record = new ProgressRecord(s_ProgressId, _testConnectionProgressBarActivity, msg);
            WriteProgress(record);
        }

        private void WritePingFooter()
        {
            WriteVerbose(TestConnectionResources.PingComplete);

            var record = new ProgressRecord(s_ProgressId, _testConnectionProgressBarActivity, ProgressRecordSpace)
            {
                RecordType = ProgressRecordType.Completed
            };
            WriteProgress(record);
        }
        #endregion PingTest

        private bool InitProcessPing(string targetNameOrAddress, out string resolvedTargetName, out IPAddress targetAddress)
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
                        hostEntry = Dns.GetHostEntry(hostEntry.HostName);
                    }
                }
                catch (Exception ex)
                {
                    string message = StringUtil.Format(
                        TestConnectionResources.NoPingResult,
                        resolvedTargetName,
                        TestConnectionResources.CannotResolveTargetName);
                    Exception pingException = new PingException(message, ex);
                    ErrorRecord errorRecord = new ErrorRecord(
                        pingException,
                        TestConnectionExceptionId,
                        ErrorCategory.ResourceUnavailable,
                        resolvedTargetName);
                    WriteError(errorRecord);
                    return false;
                }

                if (IPv6 || IPv4)
                {
                    AddressFamily addressFamily = IPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;

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
                        Exception pingException = new PingException(message, null);
                        ErrorRecord errorRecord = new ErrorRecord(
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
            if (!this.disposed)
            {
                if (disposing)
                {
                    _sender.Dispose();
                    _pingComplete?.Dispose();
                }

                disposed = true;
            }
        }

        // Count of pings sent per each trace route hop.
        // Default = 3 (from Windows).
        // If we change 'DefaultTraceRoutePingCount' we should change 'ConsoleTraceRouteReply' resource string.
        private const int DefaultTraceRoutePingCount = 3;

        /// Create the default send buffer once and cache it.
        private const int DefaultSendBufferSize = 32;
        private static byte[] s_DefaultSendBuffer = null;

        private bool disposed = false;

        private readonly Ping _sender = new Ping();
        private readonly ManualResetEventSlim _pingComplete = new ManualResetEventSlim();
        private PingCompletedEventArgs _pingCompleteArgs;

        private PingReply SendCancellablePing(
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

        private static void OnPingComplete(object sender, PingCompletedEventArgs e)
        {
            ((TestConnectionCommand)e.UserState)._pingCompleteArgs = e;
            ((TestConnectionCommand)e.UserState)._pingComplete.Set();
        }

        // Random value for WriteProgress Activity Id.
        private static readonly int s_ProgressId = 174593053;

        // Empty message string for Progress Bar.
        private const string ProgressRecordSpace = " ";

        private const string TestConnectionExceptionId = "TestConnectionException";

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
            /// <param name="options">The PingOptions specified when the ping was sent.</param>
            /// <param name="latency">The latency of the ping.</param>
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
                Destination = destination ?? reply.Address.ToString();
            }

            // These values should only be set if this PingStatus was created as part of a traceroute.
            private readonly int _bufferSize = -1;
            private readonly PingOptions _options;
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
            public IPAddress Address { get => Reply.Status == IPStatus.Success ? Reply.Address : null; }

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
            /// Initializes a new instance of the <see cref="PingMtuStatus"/> class.
            /// </summary>
            /// <param name="source">The source machine name or IP of the ping.</param>
            /// <param name="destination">The destination machine name of the ping.</param>
            /// <param name="reply">The response from the ping attempt.</param>
            internal PingMtuStatus(string source, string destination, PingReply reply)
                : base(source, destination, reply, 1)
            {
            }

            /// <summary>
            /// Gets the maximum transmission unit size on the network path between the source and destination.
            /// </summary>
            public int MtuSize { get => BufferSize; }
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
            /// It is considered successful if the individual ping reports either Success or TtlExpired;
            /// TtlExpired is the expected response from an intermediate traceroute hop.
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
            /// Gets the PingOptions used to send the ping to the trace hop.
            /// </summary>
            public PingOptions Options { get => _status.Options; }
        }

        /// <summary>
        /// Finalizer for IDisposable class.
        /// </summary>
        ~TestConnectionCommand()
        {
            Dispose(disposing: false);
        }
    }
}
