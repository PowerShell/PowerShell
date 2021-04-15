﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
