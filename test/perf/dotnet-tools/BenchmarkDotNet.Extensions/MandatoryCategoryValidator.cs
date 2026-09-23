// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Extensions
{
    /// <summary>
    /// this class makes sure that every benchmark belongs to a mandatory category
    /// categories are used by the CI for filtering
    /// </summary>
    public class MandatoryCategoryValidator : IValidator
    {
        private readonly ImmutableHashSet<string> _mandatoryCategories;

        public bool TreatsWarningsAsErrors => true;

        public MandatoryCategoryValidator(ImmutableHashSet<string> categories) => _mandatoryCategories = categories;

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
            => validationParameters.Benchmarks
                .Where(benchmark => !benchmark.Descriptor.Categories.Any(category => _mandatoryCategories.Contains(category)))
                .Select(benchmark => benchmark.Descriptor.GetFilterName())
                .Distinct()
                .Select(benchmarkId =>
                    new ValidationError(
                        isCritical: TreatsWarningsAsErrors,
                        $"{benchmarkId} does not belong to one of the mandatory categories: {string.Join(", ", _mandatoryCategories)}. Use [BenchmarkCategory(Categories.$)]")
                );
    }
}
