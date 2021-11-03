// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Net;
using System.Reflection;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A completer for HTTP version names.
    /// </summary>
    internal sealed class HttpVersionCompletionsAttribute : ArgumentCompletionsAttribute
    {
        public static readonly string[] AllowedVersions;

        static HttpVersionCompletionsAttribute()
        {
            FieldInfo[] fields = typeof(HttpVersion).GetFields(BindingFlags.Static | BindingFlags.Public);

            var versions = new List<string>(fields.Length - 1);

            for (int i = 0; i < fields.Length; i++)
            {
                // skip field Unknown and not Version type
                if (fields[i].Name == nameof(HttpVersion.Unknown) || fields[i].FieldType != typeof(Version))
                {
                    continue;
                }

                var version = (Version?)fields[i].GetValue(null);

                if (version is not null)
                {
                    versions.Add(version.ToString());
                }
            }

            AllowedVersions = versions.ToArray();
        }

        /// <inheritdoc/>
        public HttpVersionCompletionsAttribute() : base(AllowedVersions)
        {
        } 
    }
}
