// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// FormObject used in HtmlWebResponseObject.
    /// </summary>
    public class FormObject
    {
        /// <summary>
        /// Gets or private sets the Id property.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets or private sets the Method property.
        /// </summary>
        public string Method { get; }

        /// <summary>
        /// Gets or private sets the Action property.
        /// </summary>
        public string Action { get; }

        /// <summary>
        /// Gets or private sets the Fields property.
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
            string test;
            if (key != null && !Fields.TryGetValue(key, out test))
            {
                Fields[key] = value;
            }
        }
    }
}
