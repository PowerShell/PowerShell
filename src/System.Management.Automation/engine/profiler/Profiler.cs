// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics;
using System.Management.Automation.Interpreter;
using System.Management.Automation.Language;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)


namespace System.Management.Automation
{
    public static class Profiler
    {
        public static bool TracingEnabled => false;
        public static bool MinimalTracingEnabled => false;

        private static Hit _previousHit;

        public static List<Hit> Hits { get; } = new List<Hit>(1000);

        /// <summary>
        /// safdsadf
        /// </summary>
        public static List<string> TraceLines = new List<string>();


        public static void Restart()
        {
            Clear();
        }

        public static void Stop()
        {
            if (_previousHit != null)
            {
                var end = Stopwatch.GetTimestamp();
                _previousHit.End = end;
            }
        }

        public static void Clear()
        {
            Hits.Clear();
        }

        internal static void Trace(FunctionContext context)
        {
            if (_previousHit != null)
            {
                _previousHit.End = Stopwatch.GetTimestamp();
            }

            var hit = new Hit
            {
                Source = context._file,
                InFile = context._file != null,
                Line = context.CurrentPosition.StartLineNumber,
                Column = context.CurrentPosition.StartColumnNumber,
                Start = Stopwatch.GetTimestamp(),
                // so we can refer back to a position in the 
                // whole timeline and see who called us
                Index = Hits.Count,
            };

            Hits.Add(hit);
            _previousHit = hit;
        }

        internal static void TraceStart(InterpretedFrame frame)
        {
            throw new NotImplementedException();
        }

        internal static void TraceEnd(InterpretedFrame frame)
        {
            throw new NotImplementedException();
        }
    }

    public class Hit
    {
        private long _end;
        public string Source;
        public int Line;
        public int Column;
        public bool InFile;
        public long Start;
        public TimeSpan Duration;
        public int Index;


        public long End
        {
            get { return _end; }
            internal set
            {
                _end = value;
                Duration = TimeSpan.FromTicks(_end - Start);
            }
        }
    }

    public enum HitType
    {
        Start,
        End
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

