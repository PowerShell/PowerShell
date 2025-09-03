using BenchmarkDotNet.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Running;


public class PartitionFilter : IFilter
{
    private readonly int? _partitionsCount;
    private readonly int? _partitionIndex; // indexed from 0
    private int _counter = 0;
 
    public PartitionFilter(int? partitionCount, int? partitionIndex)
    {
        _partitionsCount = partitionCount;
        _partitionIndex = partitionIndex;
    }
 
    public bool Predicate(BenchmarkCase benchmarkCase)
    {
        if (!_partitionsCount.HasValue || !_partitionIndex.HasValue)
            return true; // the filter is not enabled so it does not filter anything out and can be added to RecommendedConfig

        return _counter++ % _partitionsCount.Value == _partitionIndex.Value; // will return true only for benchmarks that belong to it’s partition
    }
}
