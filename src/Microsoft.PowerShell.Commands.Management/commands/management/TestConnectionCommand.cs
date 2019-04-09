// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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
    [Cmdlet(VerbsDiagnostic.Test, "Connection", DefaultParameterSetName = ParameterSetPingCount, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135266")]
    [OutputType(typeof(PingReport), ParameterSetName = new string[] { ParameterSetPingCount })]
    [OutputType(typeof(PingReply), ParameterSetName = new string[] { ParameterSetPingContinues, ParameterSetDetectionOfMTUSize })]
    [OutputType(typeof(bool), ParameterSetName = new string[] { ParameterSetPingCount, ParameterSetPingContinues, ParameterSetConnectionByTCPPort })]
    [OutputType(typeof(Int32), ParameterSetName = new string[] { ParameterSetDetectionOfMTUSize })]
    [OutputType(typeof(TraceRouteReply), ParameterSetName = new string[] { ParameterSetTraceRoute })]
    public class TestConnectionCommand : PSCmdlet
    {
        private const string ParameterSetPingCount = "PingCount";
        private const string ParameterSetPingContinues = "PingContinues";
        private const string ParameterSetTraceRoute = "TraceRoute";
        private const string ParameterSetConnectionByTCPPort = "ConnectionByTCPPort";
        private const string ParameterSetDetectionOfMTUSize = "DetectionOfMTUSize";

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

        private const int sMaxHops = 128;

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
        [Parameter(Mandatory = true,
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

        private void ProcessConnectionByTCPPort(String targetNameOrAddress)
        {
            string resolvedTargetName = null;
            IPAddress targetAddress = null;

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
            var msg = StringUtil.Format(TestConnectionResources.ConnectionTestDescription,
                                        targetNameOrAddress,
                                        targetAddress,
                                        timeout);
            ProgressRecord record = new ProgressRecord(s_ProgressId, _testConnectionProgressBarActivity, msg);
            WriteProgress(record);
        }

        private void WriteConnectionTestFooter()
        {
            ProgressRecord record = new ProgressRecord(s_ProgressId, _testConnectionProgressBarActivity, ProgressRecordSpace);
            record.RecordType = ProgressRecordType.Completed;
            WriteProgress(record);
        }

        #endregion ConnectionTest

        #region TracerouteTest
        private void ProcessTraceroute(String targetNameOrAddress)
        {
            string resolvedTargetName = null;
            IPAddress targetAddress = null;
            byte[] buffer = GetSendBuffer(BufferSize);

            if (!InitProcessPing(targetNameOrAddress, out resolvedTargetName, out targetAddress))
            {
                return;
            }

            WriteConsoleTraceRouteHeader(resolvedTargetName, targetAddress.ToString());

            TraceRouteResult traceRouteResult = new TraceRouteResult(Source, targetAddress, resolvedTargetName);

            Int32 currentHop = 1;
            Ping sender = new Ping();
            PingOptions pingOptions = new PingOptions(currentHop, DontFragment.IsPresent);
            PingReply reply = null;
            Int32 timeout = TimeoutSeconds * 1000;

            do
            {
                TraceRouteReply traceRouteReply = new TraceRouteReply();

                pingOptions.Ttl = traceRouteReply.Hop = currentHop;
                currentHop++;

                // In the specific case we don't use 'Count' property.
                // If we change 'DefaultTraceRoutePingCount' we should change 'ConsoleTraceRouteReply' resource string.
                for (int i = 1; i <= DefaultTraceRoutePingCount; i++)
                {
                    try
                    {
                        reply = sender.Send(targetAddress, timeout, buffer, pingOptions);

                        traceRouteReply.PingReplies.Add(reply);
                    }
                    catch (PingException ex)
                    {
                        string message = StringUtil.Format(TestConnectionResources.NoPingResult,
                                                           resolvedTargetName,
                                                           ex.Message);
                        Exception pingException = new System.Net.NetworkInformation.PingException(message, ex.InnerException);
                        ErrorRecord errorRecord = new ErrorRecord(pingException,
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

                if (ResolveDestination && reply.Status == IPStatus.Success)
                {
                    traceRouteReply.ReplyRouterName = Dns.GetHostEntry(reply.Address).HostName;
                }

                traceRouteReply.ReplyRouterAddress = reply.Address;

                WriteTraceRouteProgress(traceRouteReply);

                traceRouteResult.Replies.Add(traceRouteReply);
            } while (reply != null && currentHop <= sMaxHops && (reply.Status == IPStatus.TtlExpired || reply.Status == IPStatus.TimedOut));

            WriteTraceRouteFooter();

            if (Quiet.IsPresent)
            {
                WriteObject(currentHop <= sMaxHops);
            }
            else
            {
                WriteObject(traceRouteResult);
            }
        }

        private void WriteConsoleTraceRouteHeader(string resolvedTargetName, string targetAddress)
        {
            _testConnectionProgressBarActivity = StringUtil.Format(TestConnectionResources.TraceRouteStart, resolvedTargetName, targetAddress, MaxHops);

            WriteInformation(_testConnectionProgressBarActivity, s_PSHostTag);

            ProgressRecord record = new ProgressRecord(s_ProgressId, _testConnectionProgressBarActivity, ProgressRecordSpace);
            WriteProgress(record);
        }

        private string _testConnectionProgressBarActivity;
        private static string[] s_PSHostTag = new string[] { "PSHOST" };

        private void WriteTraceRouteProgress(TraceRouteReply traceRouteReply)
        {
            string msg = string.Empty;

            if (traceRouteReply.PingReplies[2].Status == IPStatus.TtlExpired || traceRouteReply.PingReplies[2].Status == IPStatus.Success)
            {
                var routerAddress = traceRouteReply.ReplyRouterAddress.ToString();
                var routerName = traceRouteReply.ReplyRouterName ?? routerAddress;
                var roundtripTime0 = traceRouteReply.PingReplies[0].Status == IPStatus.TimedOut ? "*" : traceRouteReply.PingReplies[0].RoundtripTime.ToString();
                var roundtripTime1 = traceRouteReply.PingReplies[1].Status == IPStatus.TimedOut ? "*" : traceRouteReply.PingReplies[1].RoundtripTime.ToString();
                msg = StringUtil.Format(TestConnectionResources.TraceRouteReply,
                                        traceRouteReply.Hop, roundtripTime0, roundtripTime1, traceRouteReply.PingReplies[2].RoundtripTime.ToString(),
                                        routerName, routerAddress);
            }
            else
            {
                msg = StringUtil.Format(TestConnectionResources.TraceRouteTimeOut, traceRouteReply.Hop);
            }

            WriteInformation(msg, s_PSHostTag);

            ProgressRecord record = new ProgressRecord(s_ProgressId, _testConnectionProgressBarActivity, msg);
            WriteProgress(record);
        }

        private void WriteTraceRouteFooter()
        {
            WriteInformation(TestConnectionResources.TraceRouteComplete, s_PSHostTag);

            ProgressRecord record = new ProgressRecord(s_ProgressId, _testConnectionProgressBarActivity, ProgressRecordSpace);
            record.RecordType = ProgressRecordType.Completed;
            WriteProgress(record);
        }

        /// <summary>
        /// The class contains an information about a trace route attempt.
        /// </summary>
        public class TraceRouteReply
        {
            internal TraceRouteReply()
            {
                PingReplies = new List<PingReply>(DefaultTraceRoutePingCount);
            }

            /// <summary>
            /// Number of current hop (router).
            /// </summary>
            public int Hop;

            /// <summary>
            /// List of ping replies for current hop (router).
            /// </summary>
            public List<PingReply> PingReplies;

            /// <summary>
            /// Router IP address.
            /// </summary>
            public IPAddress ReplyRouterAddress;

            /// <summary>
            /// Resolved router name.
            /// </summary>
            public string ReplyRouterName;
        }

        /// <summary>
        /// The class contains an information about the source, the destination and trace route results.
        /// </summary>
        public class TraceRouteResult
        {
            internal TraceRouteResult(string source, IPAddress destinationAddress, string destinationHost)
            {
                Source = source;
                DestinationAddress = destinationAddress;
                DestinationHost = destinationHost;
                Replies = new List<TraceRouteReply>();
            }

            /// <summary>
            /// Source from which to trace route.
            /// </summary>
            public string Source { get; }

            /// <summary>
            /// Destination to which to trace route.
            /// </summary>
            public IPAddress DestinationAddress { get; }

            /// <summary>
            /// Destination to which to trace route.
            /// </summary>
            public string DestinationHost { get; }

            /// <summary>
            /// </summary>
            public List<TraceRouteReply> Replies { get; }
        }

        #endregion TracerouteTest

        #region MTUSizeTest
        private void ProcessMTUSize(String targetNameOrAddress)
        {
            PingReply reply, replyResult = null;

            string resolvedTargetName = null;
            IPAddress targetAddress = null;

            if (!InitProcessPing(targetNameOrAddress, out resolvedTargetName, out targetAddress))
            {
                return;
            }

            WriteMTUSizeHeader(resolvedTargetName, targetAddress.ToString());

            // Cautious! Algorithm is sensitive to changing boundary values.
            int HighMTUSize = 10000;
            int CurrentMTUSize = 1473;
            int LowMTUSize = targetAddress.AddressFamily == AddressFamily.InterNetworkV6 ? 1280 : 68;
            Int32 timeout = TimeoutSeconds * 1000;

            try
            {
                Ping sender = new Ping();
                PingOptions pingOptions = new PingOptions(MaxHops, true);
                int retry = 1;

                while (LowMTUSize < (HighMTUSize - 1))
                {
                    byte[] buffer = GetSendBuffer(CurrentMTUSize);

                    WriteMTUSizeProgress(CurrentMTUSize, retry);

                    WriteDebug(StringUtil.Format("LowMTUSize: {0}, CurrentMTUSize: {1}, HighMTUSize: {2}", LowMTUSize, CurrentMTUSize, HighMTUSize));

                    reply = sender.Send(targetAddress, timeout, buffer, pingOptions);

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
                            string message = StringUtil.Format(TestConnectionResources.NoPingResult,
                                                               targetAddress,
                                                               reply.Status.ToString());
                            Exception pingException = new System.Net.NetworkInformation.PingException(message);
                            ErrorRecord errorRecord = new ErrorRecord(pingException,
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
                Exception pingException = new System.Net.NetworkInformation.PingException(message, ex.InnerException);
                ErrorRecord errorRecord = new ErrorRecord(pingException,
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
            _testConnectionProgressBarActivity = StringUtil.Format(TestConnectionResources.MTUSizeDetectStart,
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
            ProgressRecord record = new ProgressRecord(s_ProgressId, _testConnectionProgressBarActivity, ProgressRecordSpace);
            record.RecordType = ProgressRecordType.Completed;
            WriteProgress(record);
        }

        #endregion MTUSizeTest

        #region PingTest

        private void ProcessPing(String targetNameOrAddress)
        {
            string resolvedTargetName = null;
            IPAddress targetAddress = null;

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

            Ping sender = new Ping();
            PingOptions pingOptions = new PingOptions(MaxHops, DontFragment.IsPresent);
            PingReply reply = null;
            PingReport pingReport = new PingReport(Source, resolvedTargetName);
            Int32 timeout = TimeoutSeconds * 1000;
            Int32 delay = Delay * 1000;

            for (int i = 1; i <= Count; i++)
            {
                try
                {
                    reply = sender.Send(targetAddress, timeout, buffer, pingOptions);
                }
                catch (PingException ex)
                {
                    string message = StringUtil.Format(TestConnectionResources.NoPingResult, resolvedTargetName, ex.Message);
                    Exception pingException = new System.Net.NetworkInformation.PingException(message, ex.InnerException);
                    ErrorRecord errorRecord = new ErrorRecord(pingException,
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
            _testConnectionProgressBarActivity = StringUtil.Format(TestConnectionResources.MTUSizeDetectStart,
                                                                  resolvedTargetName,
                                                                  targetAddress,
                                                                  BufferSize);

            WriteInformation(_testConnectionProgressBarActivity, s_PSHostTag);

            ProgressRecord record = new ProgressRecord(s_ProgressId,
                                                       _testConnectionProgressBarActivity,
                                                       ProgressRecordSpace);
            WriteProgress(record);
        }

        private void WritePingProgress(PingReply reply)
        {
            string msg = string.Empty;
            if (reply.Status != IPStatus.Success)
            {
                msg = TestConnectionResources.PingTimeOut;
            }
            else
            {
                msg = StringUtil.Format(TestConnectionResources.PingReply,
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

            ProgressRecord record = new ProgressRecord(s_ProgressId, _testConnectionProgressBarActivity, ProgressRecordSpace);
            record.RecordType = ProgressRecordType.Completed;
            WriteProgress(record);
        }

        /// <summary>
        /// The class contains an information about the source, the destination and ping results.
        /// </summary>
        public class PingReport
        {
            internal PingReport(string source, string destination)
            {
                Source = source;
                Destination = destination;
                Replies = new List<PingReply>();
            }

            /// <summary>
            /// Source from which to ping.
            /// </summary>
            public string Source { get; }

            /// <summary>
            /// Destination to which to ping.
            /// </summary>
            public string Destination { get; }

            /// <summary>
            /// Ping results for every ping attempt.
            /// </summary>
            public List<PingReply> Replies { get; }
        }

        #endregion PingTest

        private bool InitProcessPing(String targetNameOrAddress, out string resolvedTargetName, out IPAddress targetAddress)
        {
            IPHostEntry hostEntry = null;

            resolvedTargetName = targetNameOrAddress;

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
                    string message = StringUtil.Format(TestConnectionResources.NoPingResult,
                                                       resolvedTargetName,
                                                       TestConnectionResources.CannotResolveTargetName);
                    Exception pingException = new System.Net.NetworkInformation.PingException(message, ex);
                    ErrorRecord errorRecord = new ErrorRecord(pingException,
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
                        string message = StringUtil.Format(TestConnectionResources.NoPingResult,
                                                           resolvedTargetName,
                                                           TestConnectionResources.TargetAddressAbsent);
                        Exception pingException = new System.Net.NetworkInformation.PingException(message, null);
                        ErrorRecord errorRecord = new ErrorRecord(pingException,
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
