// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Management.Automation;
using System.Text.RegularExpressions;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Class comment.
    /// </summary>
    [Cmdlet(VerbsData.ConvertFrom, "StringData", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096602", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(Hashtable))]
    public sealed class ConvertFromStringDataCommand : PSCmdlet
    {
        private string _stringData;

        /// <summary>
        /// The list of properties to display.
        /// These take the form of an PSPropertyExpression.
        /// </summary>
        /// <value></value>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        [AllowEmptyString]
        public string StringData
        {
            get
            {
                return _stringData;
            }

            set
            {
                _stringData = value;
            }
        }

        /// <summary>
        /// Gets or sets the delimiter.
        /// </summary>
        [Parameter(Position = 1)]
        public char Delimiter { get; set; } = '=';

        /// <summary>
        /// Gets or sets the AsLiteral property.
        /// </summary>
        [Parameter]
        public SwitchParameter AsLiteral { get; set; }

        /// <summary>
        /// </summary>
        protected override void ProcessRecord()
        {
            Hashtable result = new(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(_stringData))
            {
                WriteObject(result);
                return;
            }

            if (AsLiteral.IsPresent)
            {
                _stringData = Regex.Escape(_stringData);
            }

            string[] lines = _stringData.Split('\n', StringSplitOptions.TrimEntries);

            foreach (string line in lines)
            {
                if (string.IsNullOrEmpty(line) || line[0] == '#')
                    continue;

                int index = line.IndexOf(Delimiter);
                if (index <= 0)
                {
                    throw PSTraceSource.NewInvalidOperationException(
                        ConvertFromStringData.InvalidDataLine,
                        line);
                }

                string name = line.Substring(0, index);
                name = name.Trim();

                if (result.ContainsKey(name))
                {
                    throw PSTraceSource.NewInvalidOperationException(
                        ConvertFromStringData.DataItemAlreadyDefined,
                        line,
                        name);
                }

                string value = line.Substring(index + 1);
                value = value.Trim();

                value = Regex.Unescape(value);

                result.Add(name, value);
            }

            WriteObject(result);
        }
    }
}
