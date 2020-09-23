// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Management.Automation.Internal;

namespace System.Management.Automation
{
    /// <summary>
    /// This class is a source of performance events for script block profiling.
    /// Guid is {092ae15a-d5fb-5d8d-9ffd-68891d24c5f6}.
    /// </summary>
    [EventSource(Name = "Microsoft-PowerShell-Profiler")]
    internal class ProfilerEventSource : EventSource
    {
        internal static ProfilerEventSource LogInstance = new ProfilerEventSource();

        [Event(1)]
        public void SequencePoint(Guid ScriptBlockId, int SequencePointPosition)
        {
            // We could use:
            // WriteEvent(eventId: 1, ScriptBlockId, SequencePointPosition);
            // but we care about performance.
            if (IsEnabled())
            {
                unsafe
                {
                    EventData* eventPayload = stackalloc EventData[2];

                    eventPayload[0] = new EventData
                    {
                        Size = sizeof(Guid),
                        DataPointer = ((IntPtr)(&ScriptBlockId))
                    };
                    eventPayload[1] = new EventData
                    {
                        Size = sizeof(int),
                        DataPointer = ((IntPtr)(&SequencePointPosition))
                    };

                    WriteEventCore(eventId: 1, eventDataCount: 2, eventPayload);
                }
            }
        }

        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            base.OnEventCommand(command);

            if (command.Command == EventCommand.Disable)
            {
                // At the end of the profile session, we send all the metadata.
                ProfilerRundownEventSource.LogInstance.WriteRundownEvents();
            }
        }
    }

    /// <summary>
    /// This class is a source of rundown (meta data) events for script block profiling.
    /// Guid is {f348266e-dde6-5590-4bc9-10e1e2b6fe16}.
    /// </summary>
    [EventSource(Name = "Microsoft-PowerShell-Profiler-Rundown")]
    internal class ProfilerRundownEventSource : EventSource
    {
        internal static ProfilerRundownEventSource LogInstance = new ProfilerRundownEventSource();

        [Event(2)]
        public void ScriptBlockRundown(
            Guid ScriptBlockId,
            string ScriptBlockText)
        {
            // It is not performance critical so we use the standard method overload.
            WriteEvent(
                eventId: 2,
                ScriptBlockId,
                ScriptBlockText);
        }

        [Event(3)]
        public void SequencePointRundown(
            Guid ScriptBlockId,
            int SequencePointCount,
            int SequencePoint,
            string File,
            int StartLineNumber,
            int StartColumnNumber,
            int EndLineNumber,
            int EndColumnNumber,
            string Text,
            int StartOffset,
            int EndOffset)
        {
            // It is not performance critical so we use the standard method overload.
            WriteEvent(
                eventId: 3,
                ScriptBlockId,
                SequencePointCount,
                SequencePoint,
                File,
                StartLineNumber,
                StartColumnNumber,
                EndLineNumber,
                EndColumnNumber,
                Text,
                StartOffset,
                EndOffset);
        }

        [NonEvent]
        internal void WriteRundownEvents()
        {
            foreach (var csb in CompiledScriptBlockData.GetCompiledScriptBlockData().Values)
            {
                ScriptBlockRundown(csb.Id, csb.Ast.Body.Extent.Text);

                for (var position = 0; position < csb.SequencePoints.Length; position++)
                {
                    var sequencePoint = csb.SequencePoints[position];

                    SequencePointRundown(
                        csb.Id,
                        csb.SequencePoints.Length,
                        position,
                        sequencePoint.File,
                        sequencePoint.StartLineNumber,
                        sequencePoint.StartColumnNumber,
                        sequencePoint.EndLineNumber,
                        sequencePoint.EndColumnNumber,
                        sequencePoint.Text,
                        sequencePoint.StartOffset,
                        sequencePoint.EndOffset);
                }
            }
        }

        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            base.OnEventCommand(command);

            if (command.Command == EventCommand.Enable)
            {
                CompiledScriptBlockData.ResetIdToScriptBlock();
            }
        }

        protected override void Dispose(bool disposing)
        {
            CompiledScriptBlockData.ResetIdToScriptBlock();
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Represents a span of text in a script.
    /// </summary>
    internal struct ScriptExtentEventData
    {
        /// <summary>
        /// The filename the extent includes, or null if the extent is not included in any file.
        /// </summary>
        public string File;

        /// <summary>
        /// The line number at the beginning of the extent, with the value 1 being the first line.
        /// </summary>
        public int StartLineNumber;

        /// <summary>
        /// The column number at the beginning of the extent, with the value 1 being the first column.
        /// </summary>
        public int StartColumnNumber;

        /// <summary>
        /// The line number at the end of the extent, with the value 1 being the first line.
        /// </summary>
        public int EndLineNumber;

        /// <summary>
        /// The column number at the end of the extent, with the value 1 being the first column.
        /// </summary>
        public int EndColumnNumber;

        /// <summary>
        /// The script text that the extent includes.
        /// </summary>
        public string Text;

        /// <summary>
        /// The starting offset of the extent.
        /// </summary>
        public int StartOffset;

        /// <summary>
        /// The ending offset of the extent.
        /// </summary>
        public int EndOffset;
    }

    /// <summary>
    /// This class is PowerShell script block profiler.
    /// </summary>
    internal class InternalProfiler : EventListener
    {
        /// <summary>
        /// Represents a SequencePoint profile event data.
        /// The event is raised at every sequence point start.
        /// The event must be as small as possible for performance.
        /// </summary>
        internal struct SequencePointProfileEventData
        {
            /// <summary>
            /// Start time of the SequencePoint.
            /// </summary>
            public DateTime Timestamp;

            /// <summary>
            /// Unique identifer of the script block.
            /// </summary>
            public Guid ScriptId;

            /// <summary>
            /// SequencePoint index number/position of the script block.
            /// </summary>
            public int SequencePointPosition;
        }

        /// <summary>
        /// Represents a SequencePoint rundown profile event data.
        /// The rundown event contains a meta data about script block sequence points.
        /// The rundown event is raised once for every script block sequence point
        /// at profile session end.
        /// </summary>
        internal struct CompiledScriptBlockRundownProfileEventData
        {
            /// <summary>
            /// Timestamp of first rundown profile event for the script block.
            /// </summary>
            public DateTime Timestamp;

            /// <summary>
            /// Unique identifer of the script block.
            /// </summary>
            public Guid ScriptId;

            /// <summary>
            /// Sequence points of the script block.
            /// </summary>
            public ScriptExtentEventData[] SequencePoints;
        }

        // Buffer to collect a performance event data.
        internal List<SequencePointProfileEventData> SequencePointProfileEvents = new List<SequencePointProfileEventData>(5000);

        // Buffer to collect a script block meta data.
        internal Dictionary<Guid, CompiledScriptBlockRundownProfileEventData> CompiledScriptBlockMetaData = new Dictionary<Guid, CompiledScriptBlockRundownProfileEventData>(5000);

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            var payload = eventData.Payload;
            if (payload is null)
            {
                // there is a bug in our custom EventSource.
                throw new ArgumentNullException(nameof(payload));
            }

            switch (eventData.EventId)
            {
                case 1:
                    // Performance event
                    SequencePointProfileEvents.Add(new SequencePointProfileEventData
                    {
                        Timestamp = eventData.TimeStamp,
                        ScriptId = (Guid)payload[0]!,
                        SequencePointPosition = (int)payload[1]!
                    });
                    break;
                case 2:
                    // Scriptblock rundown event
                    break;
                case 3:
                    // SequencePoint rundown event
                    ScriptExtentEventData sequencePoint;
                    var scriptId = (Guid)payload[0]!;
                    var sequencePointCount = (int)payload[1]!;
                    var pos = (int)payload[2]!;
                    sequencePoint.File = (string)payload[3]!;
                    sequencePoint.StartLineNumber = (int)payload[4]!;
                    sequencePoint.StartColumnNumber = (int)payload[5]!;
                    sequencePoint.EndLineNumber = (int)payload[6]!;
                    sequencePoint.EndColumnNumber = (int)payload[7]!;
                    sequencePoint.Text = (string)payload[8]!;
                    sequencePoint.StartOffset = (int)payload[9]!;
                    sequencePoint.EndOffset = (int)payload[10]!;

                    if (CompiledScriptBlockMetaData.TryGetValue(scriptId, out var sbe))
                    {
                        sbe.SequencePoints[pos] = sequencePoint;
                    }
                    else
                    {
                        sbe = new CompiledScriptBlockRundownProfileEventData()
                        {
                            Timestamp = eventData.TimeStamp,
                            ScriptId = scriptId,
                            SequencePoints = new ScriptExtentEventData[sequencePointCount]
                        };

                        sbe.SequencePoints[pos] = sequencePoint;

                        CompiledScriptBlockMetaData.TryAdd(sbe.ScriptId, sbe);
                    }
                    break;
            }
        }

        /// <summary>
        /// Start the profiler.
        /// </summary>
        public void EnableEvents()
        {
            EnableEvents(ProfilerRundownEventSource.LogInstance, EventLevel.LogAlways);
            EnableEvents(ProfilerEventSource.LogInstance, EventLevel.LogAlways);
        }

        /// <summary>
        /// Stop the profiler.
        /// </summary>
        public void DisableEvents()
        {
            DisableEvents(ProfilerEventSource.LogInstance);
            DisableEvents(ProfilerRundownEventSource.LogInstance);
        }
    }

    /// <summary>
    /// The cmdlet profiles a script block.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Measure, "Script", RemotingCapability = RemotingCapability.None)]
    public class MeasureScriptCommand : PSCmdlet
    {
        /// <summary>
        /// A script block to profile.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        public ScriptBlock ScriptBlock { get; set; } = null!;

        /// <summary>
        /// Process a profile data.
        /// </summary>
        protected override void EndProcessing()
        {
            using (var profiler = new InternalProfiler())
            {
                try
                {
                    profiler.EnableEvents();

                    ScriptBlock.InvokeWithPipe(
                        useLocalScope: false,
                        errorHandlingBehavior: ScriptBlock.ErrorHandlingBehavior.WriteToCurrentErrorPipe,
                        dollarUnder: null,
                        input: Array.Empty<object>(),
                        scriptThis: AutomationNull.Value,
                        outputPipe: new Pipe { NullPipe = true },
                        invocationInfo: null);
                }
                finally
                {
                    profiler.DisableEvents();
                }

                var events = profiler.SequencePointProfileEvents;
                if (events.Count == 0)
                {
                    return;
                }

                var metaData = profiler.CompiledScriptBlockMetaData;

                // We have only start timestamp for sequence point.
                // To evaluate a duration of the sequence point
                // we take a start timestamp from next sequence point
                // that is actually a stop timestamp for the previous sequence point.
                // For last sequence point we have not next timestamp
                // so we add a copy of the last sequence point as a workaround.
                events.Add(events[events.Count - 1]);

                for (var i = 0; i < events.Count - 1; i++)
                {
                    var profileDate = events[i];
                    if (metaData.TryGetValue(profileDate.ScriptId, out var compiledScriptBlockData))
                    {
                        var extent = compiledScriptBlockData.SequencePoints[profileDate.SequencePointPosition];

                        PSObject result = new PSObject();
                        result.Properties.Add(new PSNoteProperty("TimeStamp", profileDate.Timestamp.TimeOfDay));
                        result.Properties.Add(new PSNoteProperty("Duration", events[i + 1].Timestamp - profileDate.Timestamp));
                        result.Properties.Add(new PSNoteProperty("ExtentText", extent.Text));
                        result.Properties.Add(new PSNoteProperty("Extent", extent));

                        WriteObject(result);
                    }
                }
            }
        }
    }
}
