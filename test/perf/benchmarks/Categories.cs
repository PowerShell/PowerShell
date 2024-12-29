// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace MicroBenchmarks
{
    public static class Categories
    {
        /// <summary>
        /// Benchmarks belonging to this category are executed for CI jobs.
        /// </summary>
        public const string Components = "Components";

        /// <summary>
        /// Benchmarks belonging to this category are executed for CI jobs.
        /// </summary>
        public const string Engine = "Engine";

        /// <summary>
        /// Benchmarks belonging to this category are targeting internal APIs.
        /// </summary>
        public const string Internal = "Internal";

        /// <summary>
        /// Benchmarks belonging to this category are targeting public APIs.
        /// </summary>
        public const string Public = "Public";
    }
}
