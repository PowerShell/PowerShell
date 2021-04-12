// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MicroBenchmarks
{
    public static class Categories
    {        
        /// <summary>
        /// benchmarks belonging to this category are executed for CI jobs
        /// </summary>
        public const string Parser = "Parser";

        /// <summary>
        /// benchmarks belonging to this category are executed for CI jobs
        /// </summary>
        public const string Runtime = "Runtime";

        public const string Libraries = "Libraries";
    }
}
