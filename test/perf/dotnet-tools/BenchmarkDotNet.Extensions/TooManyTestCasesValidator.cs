// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Extensions
{
    /// <summary>
    /// we need to tell our users that having more than 16 test cases per benchmark is a VERY BAD idea
    /// </summary>
    public class TooManyTestCasesValidator : IValidator
    {
        private const int Limit = 16;
        
        public static readonly IValidator FailOnError = new TooManyTestCasesValidator();
        
        public bool TreatsWarningsAsErrors => true;

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            var byDescriptor = validationParameters.Benchmarks.GroupBy(benchmark => (benchmark.Descriptor, benchmark.Job)); // descriptor = type + method

            return byDescriptor.Where(benchmarkCase => benchmarkCase.Count() > Limit).Select(group =>
                new ValidationError(
                    isCritical: true,
                    message: $"{group.Key.Descriptor.Type.Name}.{group.Key.Descriptor.WorkloadMethod.Name} has {group.Count()} test cases. It MUST NOT have more than {Limit} test cases. We don't have inifinite amount of time to run all the benchmarks!!",
                    benchmarkCase: group.First()));
        }
    }
}