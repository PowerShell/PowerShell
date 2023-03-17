// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Collections.Generic;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// FormObject used in HtmlWebResponseObject.
    /// </summary>
    public class FormObject
    {
        /// <summary>
        /// Gets the Id property.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the Method property.
        /// </summary>
        public string Method { get; }

        /// <summary>
        /// Gets the Action property.
        /// </summary>
        public string Action { get; }

        /// <summary>
        /// Gets the Fields property.
        /// </summary>
        public Dictionary<string, string> Fields { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormObject"/> class.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="method"></param>
        /// <param name="action"></param>
        public FormObject(string id, string method, string action)
        {
            Id = id;
            Method = method;
            Action = action;
            Fields = new Dictionary<string, string>();
        }

        internal void AddField(string key, string value)
        {
            if (key is not null && !Fields.TryGetValue(key, out string? test))
            {
                Fields[key] = value;
            }
        }
    }
}
