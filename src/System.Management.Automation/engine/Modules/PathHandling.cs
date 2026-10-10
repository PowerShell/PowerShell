// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation.Internal
{
    internal static class PathHandling
    {
        public static string NormalizeDirectorySeparators(string path)
        {
            return path.Replace(StringLiterals.AlternatePathSeparator, StringLiterals.DefaultPathSeparator);
        }
    }
}
