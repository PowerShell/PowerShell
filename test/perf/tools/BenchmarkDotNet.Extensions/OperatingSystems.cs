// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Extensions
{
    public enum OS : byte
    {
        Windows,
        Linux,
        OSX
    }

    /// <summary>
    /// Allows to enable given benchmark(s) for selected operating system(s) 
    /// </summary>
    public class AllowedOperatingSystemsAttribute : BenchmarkCategoryAttribute
    {
        /// <param name="comment">mandatory comment. Why this benchmark can't run on ALL OSes?</param>
        /// <param name="allowed">list of allowed operating systems</param>
        public AllowedOperatingSystemsAttribute(string comment, params OS[] allowed)
            : base(allowed.Select(platform => platform.ToString()).ToArray())
        {
            if (string.IsNullOrWhiteSpace(comment))
                throw new ArgumentNullException(nameof(comment), "Non-empty comment is mandatory!");
        }
    }

    public class OperatingSystemFilter : SimpleFilter
    {
        public OperatingSystemFilter() : base(Filter) { }

        private static bool Filter(BenchmarkCase benchmarkCase) 
            => benchmarkCase.Descriptor.Categories.All(category => !Enum.TryParse<OS>(category, out _))
            || benchmarkCase.Descriptor.Categories.Any(category => Enum.TryParse<OS>(category, out OS os) && IsCurrentOs(os));

        private static bool IsCurrentOs(OS os)
        {
            switch (os)
            {
                case OS.Windows:
                    return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                case OS.Linux:
                    return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
                case OS.OSX:
                    return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
                default:
                    throw new ArgumentOutOfRangeException(nameof(os), os, null);
            }
        }
    }
}