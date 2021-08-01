// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;

namespace Microsoft.PowerShell.Commands
{
    internal static class HttpVersionUtils
    {
        public static readonly IReadOnlyCollection<string> AllowedVersions;

        static HttpVersionUtils()
        {
            var fields = typeof(HttpVersion).GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

            if (fields.Length == 0)
            {
                AllowedVersions = Array.Empty<string>();
                return;
            }

            var versions = new List<string>(fields.Length - 1);

            for (int i = 0; i < fields.Length; i++)
            {
                // skip field Unknown and not Version type
                if (fields[i].Name == nameof(HttpVersion.Unknown) || fields[i].FieldType != typeof(Version))
                    continue;

                var version = (Version)fields[i].GetValue(null);

                versions.Add(version.ToString());
            }

            AllowedVersions = versions;
        }
    }
}
