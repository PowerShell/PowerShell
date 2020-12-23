// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Resources;

namespace Microsoft.PowerShell.Commands.GetCounter
{
    public class PerformanceCounterSample
    {
        internal PerformanceCounterSample()
        {
        }

        internal PerformanceCounterSample(string path,
                               string instanceName,
                               double cookedValue,
                               ulong rawValue,
                               ulong secondValue,
                               uint multiCount,
                               PerformanceCounterType counterType,
                               uint defaultScale,
                               ulong timeBase,
                               DateTime timeStamp,
                               ulong timeStamp100nSec,
                               uint status)
        {
            Path = path;
            InstanceName = instanceName;
            CookedValue = cookedValue;
            RawValue = rawValue;
            SecondValue = secondValue;
            MultipleCount = multiCount;
            CounterType = counterType;
            DefaultScale = defaultScale;
            TimeBase = timeBase;
            Timestamp = timeStamp;
            Timestamp100NSec = timeStamp100nSec;
            Status = status;
        }

        public string Path { get; set; } = string.Empty;

        public string InstanceName { get; set; } = string.Empty;

        public double CookedValue { get; set; }

        public ulong RawValue { get; set; }

        public ulong SecondValue { get; set; }

        public uint MultipleCount { get; set; }

        public PerformanceCounterType CounterType { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.MinValue;

        public ulong Timestamp100NSec { get; set; }

        public uint Status { get; set; }

        public uint DefaultScale { get; set; }

        public ulong TimeBase { get; set; }
    }

    public class PerformanceCounterSampleSet
    {
        internal PerformanceCounterSampleSet()
        {
            _resourceMgr = Microsoft.PowerShell.Commands.Diagnostics.Common.CommonUtilities.GetResourceManager();
        }

        internal PerformanceCounterSampleSet(DateTime timeStamp,
                                    PerformanceCounterSample[] counterSamples,
                                    bool firstSet) : this()
        {
            Timestamp = timeStamp;
            CounterSamples = counterSamples;
        }

        public DateTime Timestamp { get; set; } = DateTime.MinValue;

        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
                    Scope = "member",
                    Target = "Microsoft.PowerShell.Commands.GetCounter.PerformanceCounterSample.CounterSamples",
                    Justification = "A string[] is required here because that is the type Powershell supports")]
        public PerformanceCounterSample[] CounterSamples { get; set; }

        private readonly ResourceManager _resourceMgr = null;
    }
}
