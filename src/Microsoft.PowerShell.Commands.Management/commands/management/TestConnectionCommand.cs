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
    [Cmdlet(VerbsDiagnostic.Test, "Connection", DefaultParameterSetName = ParameterSetPingCount,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135266")]
    [OutputType(typeof(PingStatus), ParameterSetName = new[] { ParameterSetPingCount })]
    [OutputType(typeof(PingReply), ParameterSetName = new[] { ParameterSetPingContinues, ParameterSetDetectionOfMTUSize })]
    [OutputType(typeof(bool), ParameterSetName = new[] { ParameterSetPingCount, ParameterSetPingContinues, ParameterSetConnectionByTCPPort })]
    [OutputType(typeof(int), ParameterSetName = new[] { ParameterSetDetectionOfMTUSize })]
    [OutputType(typeof(TraceStatus), ParameterSetName = new[] { ParameterSetTraceRoute })]
    public class TestConnectionCommand : PSCmdlet, IDisposable
    {
        private const string ParameterSetPingCount = "PingCount";
        private const string ParameterSetPingContinues = "PingContinues";
        private const string ParameterSetTraceRoute = "TraceRoute";
        private const string ParameterSetConnectionByTCPPort = "ConnectionByTCPPort";
        private const string ParameterSetDetectionOfMTUSize = "DetectionOfMTUSize";
        private const int sMaxHops = 128;

        #region Parameters

        /// <summary>
        /// Do ping test.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSetPingCount)]
        [Parameter(ParameterSetName = ParameterSetPingContinues)]
        public SwitchParameter Ping { get; set; } = true;

        /// <summary>
        /// Force using IPv4 protocol.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSetPingCount)]
        [Parameter(ParameterSetName = ParameterSetPingContinues)]
        [Parameter(ParameterSetName = ParameterSetTraceRoute)]
        [Parameter(ParameterSetName = ParameterSetDetectionOfMTUSize)]
        [Parameter(ParameterSetName = ParameterSetConnectionByTCPPort)]
        public SwitchParameter IPv4 { get; set; }

        /// <summary>
        /// Force using IPv6 protocol.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSetPingCount)]
        [Parameter(ParameterSetName = ParameterSetPingContinues)]
        [Parameter(ParameterSetName = ParameterSetTraceRoute)]
        [Parameter(ParameterSetName = ParameterSetDetectionOfMTUSize)]
        [Parameter(ParameterSetName = ParameterSetConnectionByTCPPort)]
        public SwitchParameter IPv6 { get; set; }

        /// <summary>
        /// Do reverse DNS lookup to get names for IP addresses.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSetPingCount)]
        [Parameter(ParameterSetName = ParameterSetPingContinues)]
        [Parameter(ParameterSetName = ParameterSetTraceRoute)]
        [Parameter(ParameterSetName = ParameterSetDetectionOfMTUSize)]
        [Parameter(ParameterSetName = ParameterSetConnectionByTCPPort)]
        public SwitchParameter ResolveDestination { get; set; }

        /// <summary>
        /// Source from which to do a test (ping, trace route, ...).
        /// The default is Local Host.
        /// Remoting is not yet implemented internally in the cmdlet.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSetPingCount)]
        [Parameter(ParameterSetName = ParameterSetPingContinues)]
        [Parameter(ParameterSetName = ParameterSetTraceRoute)]
        [Parameter(ParameterSetName = ParameterSetConnectionByTCPPort)]
        public string Source { get; } = Dns.GetHostName();

        /// <summary>
        /// The number of times the Ping data packets can be forwarded by routers.
        /// As gateways and routers transmit packets through a network,
        /// they decrement the Time-to-Live (TTL) value found in the packet header.
        /// The default (from Windows) is 128 hops.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSetPingCount)]
        [Parameter(ParameterSetName = ParameterSetPingContinues)]
        [Parameter(ParameterSetName = ParameterSetTraceRoute)]
        [ValidateRange(0, sMaxHops)]
        [Alias("Ttl", "TimeToLive", "Hops")]
        public int MaxHops { get; set; } = sMaxHops;

        /// <summary>
        /// Count of attempts.
        /// The default (from Windows) is 4 times.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSetPingCount)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int Count { get; set; } = 4;

        /// <summary>
        /// Delay between attempts.
        /// The default (from Windows) is 1 second.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSetPingCount)]
        [Parameter(ParameterSetName = ParameterSetPingContinues)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int Delay { get; set; } = 1;

        /// <summary>
        /// Buffer size to send.
        /// The default (from Windows) is 32 bites.
        /// Max value is 65500 (limit from Windows API).
        /// </summary>
        [Parameter(ParameterSetName = ParameterSetPingCount)]
        [Parameter(ParameterSetName = ParameterSetPingContinues)]
        [Alias("Size", "Bytes", "BS")]
        [ValidateRange(0, 65500)]
        public int BufferSize { get; set; } = DefaultSendBufferSize;

        /// <summary>
        /// Don't fragment ICMP packages.
        /// Currently CoreFX not supports this on Unix.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSetPingCount)]
        [Parameter(ParameterSetName = ParameterSetPingContinues)]
        public SwitchParameter DontFragment { get; set; }

        /// <summary>
        /// Continue ping until user press Ctrl-C
        /// or Int.MaxValue threshold reached.
        /// </summary>
        [Parameter(ParameterSetName = ParameterSetPingContinues)]
        public SwitchParameter Continues { get; set; }

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

        private readonly Ping _sender = new Ping();

        /// <summary>
        /// Init the cmdlet.
        /// </summary>
        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            switch (ParameterSetName)
            {
                case ParameterSetPingContinues:
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
                    case ParameterSetPingCount:
                    case ParameterSetPingContinues:
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

        #region ConnectionTest

        private void ProcessConnectionByTCPPort(string targetNameOrAddress)
        {

            if (!InitProcessPing(targetNameOrAddress, out string resolvedTargetName, out IPAddress targetAddress))
            {
                return;
            }

            WriteConnectionTestHeader(resolvedTargetName, targetAddress.ToString());

            var client = new TcpClient();

            try
            {
                Task connectionTask = client.ConnectAsync(targetAddress, TCPPort);
                var targetString = targetAddress.ToString();

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
            _testConnectionProgressBarActivity = StringUtil.Format(
                TestConnectionResources.ConnectionTestStart,
                resolvedTargetName,
                targetAddress);
            var record = new ProgressRecord(s_ProgressId, _testConnectionProgressBarActivity, ProgressRecordSpace);
            WriteProgress(record);
        }

        private void WriteConnectionTestProgress(string targetNameOrAddress, string targetAddress, int timeout)
        {
            var msg = StringUtil.Format(
                TestConnectionResources.ConnectionTestDescription,
                targetNameOrAddress,
                targetAddress,
                timeout);
            var record = new ProgressRecord(s_ProgressId, _testConnectionProgressBarActivity, msg);
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

            if (!InitProcessPing(targetNameOrAddress, out string resolvedTargetName, out IPAddress targetAddress))
            {
                return;
            }

            //WriteConsoleTraceRouteHeader(resolvedTargetName, targetAddress.ToString());

            int currentHop = 1;
            var pingOptions = new PingOptions(currentHop, DontFragment.IsPresent);
            var replies = new List<PingStatus>(DefaultTraceRoutePingCount);
            int timeout = TimeoutSeconds * 1000;
            string hostname = null;
            var timer = new Stopwatch();
            PingReply reply;

            do
            {
                reply = null;
                pingOptions.Ttl = currentHop;

                // We don't allow -Count parameter for -TraceRoute.
                // If we change 'DefaultTraceRoutePingCount' we should change 'ConsoleTraceRouteReply' resource string.
                for (int i = 1; i <= DefaultTraceRoutePingCount; i++)
                {
                    try
                    {
                        timer.Start();
                        reply = _sender.Send(targetAddress, timeout, buffer, pingOptions);
                        timer.Stop();

                        if (ResolveDestination && reply.Status == IPStatus.Success)
                        {
                            hostname = Dns.GetHostEntry(reply.Address).HostName;
                        }

                        replies.Add(new PingStatus(
                            Source,
                            hostname,
                            reply,
                            pingOptions,
                            timer.ElapsedMilliseconds,
                            buffer.Length));
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
                    catch
                    {
                        // Ignore host resolve exceptions.
                    }

                    // We use short delay because it is impossible DoS with trace route.
                    Thread.Sleep(200);
                }

                if (!Quiet.IsPresent)
                {
                    WriteObject(new TraceStatus(currentHop, replies, Source, targetNameOrAddress, targetAddress));
                }

                currentHop++;
                replies.Clear();
                //WriteTraceRouteProgress(traceRouteReply);
            }
            while (reply != null
                && currentHop <= sMaxHops
                && (reply.Status == IPStatus.TtlExpired || reply.Status == IPStatus.TimedOut));

            //WriteTraceRouteFooter();

            if (Quiet.IsPresent)
            {
                WriteObject(currentHop <= sMaxHops);
            }
        }

        private void WriteConsoleTraceRouteHeader(string resolvedTargetName, string targetAddress)
        {
            _testConnectionProgressBarActivity = StringUtil.Format(
                TestConnectionResources.TraceRouteStart,
                resolvedTargetName,
                targetAddress,
                MaxHops);

            WriteInformation(_testConnectionProgressBarActivity, s_PSHostTag);

            var record = new ProgressRecord(s_ProgressId, _testConnectionProgressBarActivity, ProgressRecordSpace);
            WriteProgress(record);
        }

        private string _testConnectionProgressBarActivity;
        private static readonly string[] s_PSHostTag = new string[] { "PSHOST" };

        private void WriteTraceRouteProgress(TraceStatus traceRouteReply)
        {
            string msg;
            if (traceRouteReply.Replies[2].Status == IPStatus.TtlExpired
                || traceRouteReply.Replies[2].Status == IPStatus.Success)
            {
                var routerAddress = traceRouteReply.ReplyRouterAddress.ToString();
                var routerName = traceRouteReply.ReplyRouterName ?? routerAddress;
                var roundtripTime0 = traceRouteReply.Replies[0].Status == IPStatus.TimedOut
                    ? "*"
                    : traceRouteReply.Replies[0].RoundtripTime.ToString();
                var roundtripTime1 = traceRouteReply.Replies[1].Status == IPStatus.TimedOut
                    ? "*"
                    : traceRouteReply.Replies[1].RoundtripTime.ToString();
                msg = StringUtil.Format(
                    TestConnectionResources.TraceRouteReply,
                    traceRouteReply.Hop,
                    roundtripTime0,
                    roundtripTime1,
                    traceRouteReply.Replies[2].RoundtripTime.ToString(),
                    routerName, routerAddress);
            }
            else
            {
                msg = StringUtil.Format(TestConnectionResources.TraceRouteTimeOut, traceRouteReply.Hop);
            }

            WriteInformation(msg, s_PSHostTag);

            var record = new ProgressRecord(s_ProgressId, _testConnectionProgressBarActivity, msg);
            WriteProgress(record);
        }

        private void WriteTraceRouteFooter()
        {
            WriteInformation(TestConnectionResources.TraceRouteComplete, s_PSHostTag);

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

            if (!InitProcessPing(targetNameOrAddress, out string resolvedTargetName, out IPAddress targetAddress))
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
                var pingOptions = new PingOptions(MaxHops, true);
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

                    reply = _sender.Send(targetAddress, timeout, buffer, pingOptions);

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

            var record = new ProgressRecord(s_ProgressId, _testConnectionProgressBarActivity, ProgressRecordSpace);
            WriteProgress(record);
        }

        private void WriteMTUSizeProgress(int currentMTUSize, int retry)
        {
            var msg = StringUtil.Format(TestConnectionResources.MTUSizeDetectDescription, currentMTUSize, retry);

            var record = new ProgressRecord(s_ProgressId, _testConnectionProgressBarActivity, msg);
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
            if (!InitProcessPing(targetNameOrAddress, out string resolvedTargetName, out IPAddress targetAddress))
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
            var pingOptions = new PingOptions(MaxHops, DontFragment.IsPresent);
            var pingReport = new PingStatus(Source, resolvedTargetName);
            int timeout = TimeoutSeconds * 1000;
            int delay = Delay * 1000;

            for (int i = 1; i <= Count; i++)
            {
                try
                {
                    reply = _sender.Send(targetAddress, timeout, buffer, pingOptions);
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
                        pingReport.Replies.Add(reply);
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
            else
            {
                WriteObject(pingReport);
            }
        }

        private void WritePingHeader(string resolvedTargetName, string targetAddress)
        {
            _testConnectionProgressBarActivity = StringUtil.Format(
                TestConnectionResources.MTUSizeDetectStart,
                resolvedTargetName,
                targetAddress,
                BufferSize);

            WriteInformation(_testConnectionProgressBarActivity, s_PSHostTag);

            var record = new ProgressRecord(
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

            WriteInformation(msg, s_PSHostTag);

            ProgressRecord record = new ProgressRecord(s_ProgressId, _testConnectionProgressBarActivity, msg);
            WriteProgress(record);
        }

        private void WritePingFooter()
        {
            WriteInformation(TestConnectionResources.PingComplete, s_PSHostTag);

            ProgressRecord record = new ProgressRecord(
                s_ProgressId,
                _testConnectionProgressBarActivity,
                ProgressRecordSpace)
            {
                RecordType = ProgressRecordType.Completed
            };
            WriteProgress(record);
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
                        hostEntry = Dns.GetHostEntry(hostEntry.HostName);
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

                if (IPv6 || IPv4)
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

        /// <summary>
        /// IDisposable implementation.
        /// </summary>
        public void Dispose()
        {
            _sender?.Dispose();
        }

        /// <summary>
        /// The class contains an information about a trace route attempt.
        /// </summary>
        public class TraceStatus
        {
            internal TraceStatus(
                int hop,
                IList<PingStatus> replies,
                string source,
                string destination,
                IPAddress destinationAddress)
            {
                Hop = hop;
                Replies = (PingStatus[])replies;
                Source = source;
                Destination = destination;
                DestinationAddress = destinationAddress;

                Latency = new long[replies.Count];
                for (int index = 0; index < replies.Count; index++)
                {
                    Latency[index] = replies[index].Latency;
                }
            }

            /// <summary>
            /// Number of current hop (router).
            /// </summary>
            public int Hop { get; }

            /// <summary>
            /// The source address of the trace route command.
            /// </summary>
            /// <value></value>
            public string Source { get; }

            /// <summary>
            /// The latency values of each ping to the current hop point.
            /// </summary>
            public long[] Latency { get; }

            /// <summary>
            /// List of ping replies from the current hop point.
            /// </summary>
            public PingStatus[] Replies { get; }

            /// <summary>
            /// The hostname of the current hop point.
            /// </summary>
            /// <value></value>
            public string HopName { get => Replies[0].Destination; }

            /// <summary>
            /// The IP address of the current hop point.
            /// </summary>
            public IPAddress HopAddress { get => Replies[0].Address; }

            /// <summary>
            /// The final destination hostname of the trace.
            /// </summary>
            /// <value></value>
            public string Destination { get; }

            /// <summary>
            /// The final destination IP address of the trace.
            /// </summary>
            public IPAddress DestinationAddress { get; }
        }

        /// <summary>
        /// The class contains information about the source, the destination and ping results.
        /// </summary>
        public class PingStatus
        {
            internal PingStatus(string source, string destination, uint mtuSize, PingReply reply)
                : this(source, destination, reply) => MtuSize = mtuSize;

            internal PingStatus(
                string source,
                string destination,
                PingReply reply,
                PingOptions options,
                long latency,
                int bufferSize)
                : this(source, destination, reply)
            {
                _options = options;
                _latency = latency;
                _bufferSize = bufferSize;
            }

            internal PingStatus(string source, string destination, PingReply reply)
            {
                _reply = reply;
                Source = source;
                Destination = destination ?? reply.Address.ToString();
            }

            private readonly PingReply _reply;

            /// <summary>
            /// Source from which to ping.
            /// </summary>
            public string Source { get; }

            /// <summary>
            /// The target address of the ping.
            /// </summary>
            /// <value></value>
            public IPAddress Address { get => _reply.Address; }

            /// <summary>
            /// Destination to which to ping.
            /// </summary>
            public string Destination { get; }

            private readonly long _latency = -1;
            /// <summary>
            /// The roundtrip time of the ping in milliseconds.
            /// </summary>
            /// <value></value>
            public long Latency { get => _latency >= 0 ? _latency : _reply.RoundtripTime; }

            private readonly int _bufferSize = -1;
            /// <summary>
            /// The size in bytes of the buffer data sent in the ping.
            /// </summary>
            public int BufferSize { get => _bufferSize >= 0 ? _bufferSize : _reply.Buffer.Length; }

            private readonly PingOptions _options;
            /// <summary>
            /// The options used when sending the ping.
            /// </summary>
            /// <value></value>
            public PingOptions Options { get => _options ?? _reply.Options; }

            /// <summary>
            /// The maximum transmission unit size on the network path between the source and destination.
            /// </summary>
            /// <value></value>
            public uint? MtuSize { get; }
        }

        // Count of pings sent per each trace route hop.
        // Default = 3 (from Windows).
        // If we change 'DefaultTraceRoutePingCount' we should change 'ConsoleTraceRouteReply' resource string.
        private const int DefaultTraceRoutePingCount = 3;

        /// Create the default send buffer once and cache it.
        private const int DefaultSendBufferSize = 32;
        private static byte[] s_DefaultSendBuffer = null;

        // Random value for WriteProgress Activity Id.
        private static readonly int s_ProgressId = 174593053;

        // Empty message string for Progress Bar.
        private const string ProgressRecordSpace = " ";

        private const string TestConnectionExceptionId = "TestConnectionException";
    }
}
