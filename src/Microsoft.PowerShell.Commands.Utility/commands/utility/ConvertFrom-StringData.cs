/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Management.Automation;
using System.Collections;
using System.Text.RegularExpressions;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Class comment
    /// </summary>
    [Cmdlet(VerbsData.ConvertFrom, "StringData", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113288", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(Hashtable))]
    public sealed class ConvertFromStringDataCommand : PSCmdlet
    {
        private string _stringData;

        /// <summary>
        /// The list of properties to display
        /// These take the form of an MshExpression
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
        /// 
        /// </summary>
        protected override void ProcessRecord()
        {
            Hashtable result = new Hashtable(StringComparer.OrdinalIgnoreCase);

            if (String.IsNullOrEmpty(_stringData))
            {
                WriteObject(result);
                return;
            }

            string[] lines = _stringData.Split('\n');

            foreach (string line in lines)
            {
                string s = line.Trim();

                if (String.IsNullOrEmpty(s) || s[0] == '#')
                    continue;

                int index = s.IndexOf('=');
                if (index <= 0)
                {
                    throw PSTraceSource.NewInvalidOperationException(
                        ConvertFromStringData.InvalidDataLine,
                        line);
                }

                string name = s.Substring(0, index);
                name = name.Trim();

                if (result.ContainsKey(name))
                {
                    throw PSTraceSource.NewInvalidOperationException(
                        ConvertFromStringData.DataItemAlreadyDefined,
                        line,
                        name);
                }

                string value = s.Substring(index + 1);
                value = value.Trim();

                value = Regex.Unescape(value);

                result.Add(name, value);
            }

            WriteObject(result);
        }
    }
}

