// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Validators;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Extensions
{
    public class UniqueArgumentsValidator : IValidator
    {
        public bool TreatsWarningsAsErrors => true;

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
            => validationParameters.Benchmarks
                .Where(benchmark => benchmark.HasArguments || benchmark.HasParameters)
                .GroupBy(benchmark => (benchmark.Descriptor.Type, benchmark.Descriptor.WorkloadMethod, benchmark.Job))
                .Where(sameBenchmark =>
                {
                    int numberOfUniqueTestCases = sameBenchmark.Distinct(new BenchmarkArgumentsComparer()).Count();
                    int numberOfTestCases = sameBenchmark.Count();

                    return numberOfTestCases != numberOfUniqueTestCases;
                })
                .Select(duplicate => new ValidationError(true, $"Benchmark Arguments should be unique, {duplicate.Key.Type}.{duplicate.Key.WorkloadMethod} has duplicate arguments.", duplicate.First()));

        private class BenchmarkArgumentsComparer : IEqualityComparer<BenchmarkCase>
        {
            public bool Equals(BenchmarkCase x, BenchmarkCase y)
                => Enumerable.SequenceEqual(
                    x.Parameters.Items.Select(argument => argument.Value), 
                    y.Parameters.Items.Select(argument => argument.Value));

            public int GetHashCode(BenchmarkCase obj)
                => obj.Parameters.Items
                    .Where(item => item.Value != null)
                    .Aggregate(seed: 0, (hashCode, argument) => hashCode ^= argument.Value.GetHashCode());
        }
    }
}
