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

        [Event(1, Opcode = EventOpcode.Start, ActivityOptions = EventActivityOptions.Recursive)]
        public void SequencePoint(Guid scriptBlockId, Guid runspaceInstanceId, Guid parentScriptBlockId, int sequencePointPosition)
        {
            // We could use:
            // WriteEvent(eventId: 1, ScriptBlockId, SequencePointPosition);
            // but we care about performance.
            if (IsEnabled())
            {
                unsafe
                {
                    EventData* eventPayload = stackalloc EventData[4];

                    eventPayload[0] = new EventData
                    {
                        Size = sizeof(Guid),
                        DataPointer = ((IntPtr)(&scriptBlockId))
                    };
                    eventPayload[1] = new EventData
                    {
                        Size = sizeof(Guid),
                        DataPointer = ((IntPtr)(&runspaceInstanceId))
                    };
                    eventPayload[2] = new EventData
                    {
                        Size = sizeof(Guid),
                        DataPointer = ((IntPtr)(&parentScriptBlockId))
                    };
                    eventPayload[3] = new EventData
                    {
                        Size = sizeof(int),
                        DataPointer = ((IntPtr)(&sequencePointPosition))
                    };

                    WriteEventCore(eventId: 1, eventDataCount: 4, eventPayload);
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
            foreach (var compiledScriptBlock in CompiledScriptBlockData.GetCompiledScriptBlockList())
            {
                ScriptBlockRundown(compiledScriptBlock.Id, compiledScriptBlock.Ast.Body.Extent.Text);

                if (compiledScriptBlock.SequencePoints is null)
                {
                    // Why do we get script blocks without sequence points?
                    // See a comment in Compiler.cs line 2035.
                    continue;
                }

                for (var position = 0; position < compiledScriptBlock.SequencePoints.Length; position++)
                {
                    var sequencePoint = compiledScriptBlock.SequencePoints[position];

                    SequencePointRundown(
                        compiledScriptBlock.Id,
                        compiledScriptBlock.SequencePoints.Length,
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
                CompiledScriptBlockData.InitCompiledScriptBlockTable();
            }
            else if (command.Command == EventCommand.Disable)
            {
                CompiledScriptBlockData.ClearCompiledScriptBlockTable();
            }
        }

        protected override void Dispose(bool disposing)
        {
            CompiledScriptBlockData.ClearCompiledScriptBlockTable();
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
            /// Unique identifer of the runspace.
            /// </summary>
            public Guid RunspaceId;

            /// <summary>
            /// Unique identifer of the parent script block.
            /// </summary>
            public Guid ParentScriptBlockId;

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
                        RunspaceId = (Guid)payload[1]!,
                        ParentScriptBlockId = (Guid)payload[2]!,
                        SequencePointPosition = (int)payload[3]!
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
    [Cmdlet(VerbsDiagnostic.Measure, "Script", HelpUri = "", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(ProfileEventRecord))]
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

                // 1. We have only start timestamp for a sequence point.
                // To evaluate a duration of the sequence point
                // we take a start timestamp from next sequence point
                // that is actually a stop timestamp for the previous sequence point.
                //
                // 2. We handle events separately for each runspace.
                //
                // 3. Last event of every runspace we output as-is
                // without evaluating a duration because we have not a stop event.
                //
                Dictionary<Guid, InternalProfiler.SequencePointProfileEventData> runspaceCurrentEvent = new();

                for (var i = 0; i < events.Count; i++)
                {
                    var nextEvent = events[i];

                    if (runspaceCurrentEvent.TryGetValue(nextEvent.RunspaceId, out var currentEvent))
                    {
                        runspaceCurrentEvent[nextEvent.RunspaceId] = nextEvent;
                    }
                    else
                    {
                        // It is first event in the runspace.
                        runspaceCurrentEvent.Add(nextEvent.RunspaceId, nextEvent);
                        continue;
                    }

                    if (metaData.TryGetValue(currentEvent.ScriptId, out var compiledScriptBlockData))
                    {
                        var extent = compiledScriptBlockData.SequencePoints[currentEvent.SequencePointPosition];
                        var result = new ProfileEventRecord
                        {
                            StartTime = currentEvent.Timestamp.TimeOfDay,
                            Duration = nextEvent.Timestamp - currentEvent.Timestamp,
                            Source = extent.Text,
                            Extent = extent,
                            RunspaceId = currentEvent.RunspaceId,
                            ParentScriptBlockId = currentEvent.ParentScriptBlockId,
                            ScriptBlockId = currentEvent.ScriptId
                        };

                        WriteObject(result);
                    }
                }

                foreach (var currentEvent in runspaceCurrentEvent.Values)
                {
                    if (metaData.TryGetValue(currentEvent.ScriptId, out var compiledScriptBlockData))
                    {
                        var extent = compiledScriptBlockData.SequencePoints[currentEvent.SequencePointPosition];
                        var result = new ProfileEventRecord
                        {
                            StartTime = currentEvent.Timestamp.TimeOfDay,
                            Duration = TimeSpan.Zero,
                            Source = extent.Text,
                            Extent = extent,
                            RunspaceId = currentEvent.RunspaceId,
                            ParentScriptBlockId = currentEvent.ParentScriptBlockId,
                            ScriptBlockId = currentEvent.ScriptId
                        };

                        WriteObject(result);
                    }
                }
            }
        }

        /// <summary>
        /// Measure-ScriptBlock output type.
        /// </summary>
        internal struct ProfileEventRecord
        {
            /// <summary>
            /// StartTime of event.
            /// </summary>
            public TimeSpan StartTime;

            /// <summary>
            /// Duration of event.
            /// </summary>
            public TimeSpan Duration;

            /// <summary>
            /// Script text.
            /// </summary>
            public string Source;

            /// <summary>
            /// Script Extent.
            /// </summary>
            public ScriptExtentEventData Extent;

            /// <summary>
            /// Unique identifer of the runspace.
            /// </summary>
            public Guid RunspaceId;

            /// <summary>
            /// Unique identifer of the parent script block.
            /// </summary>
            public Guid ParentScriptBlockId;

            /// <summary>
            /// Unique identifer of the script block.
            /// </summary>
            public Guid ScriptBlockId;
        }
    }
}
