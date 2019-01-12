// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Security;
using System.Text;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Retrieves input from the host virtual console and writes it to the pipeline output.
    /// </summary>

    [Cmdlet(VerbsCommunications.Read, "Host", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113371")]
    [OutputType(typeof(string), typeof(SecureString))]
    public sealed class ReadHostCommand : PSCmdlet
    {
        /// <summary>
        /// Constructs a new instance.
        /// </summary>

        public
        ReadHostCommand()
        {
            // empty, provided per design guidelines.
        }

        #region Parameters

        /// <summary>
        /// The objects to display on the host before collecting input.
        /// </summary>

        [Parameter(Position = 0, ValueFromRemainingArguments = true)]
        [AllowNull]
        public
        object
        Prompt
        {
            get
            {
                return _prompt;
            }

            set
            {
                _prompt = value;
            }
        }

        /// <summary>
        /// Set to no echo the input as is is typed.
        /// </summary>

        [Parameter]
        public
        SwitchParameter
        AsSecureString
        {
            get
            {
                return _safe;
            }

            set
            {
                _safe = value;
            }
        }
        #endregion Parameters

        #region Cmdlet Overrides

        /// <summary>
        /// Write the prompt, then collect a line of input from the host, then output it to the output stream.
        /// </summary>
        protected override void BeginProcessing()
        {
            PSHostUserInterface ui = Host.UI;
            string promptString;

            if (_prompt != null)
            {
                IEnumerator e = LanguagePrimitives.GetEnumerator(_prompt);
                if (e != null)
                {
                    StringBuilder sb = new StringBuilder();

                    while (e.MoveNext())
                    {
                        // The current object might itself be a collection, like a string array, as in read/console "foo","bar","baz"
                        // If it is, then the PSObject ToString() will take care of it.  We could go on unwrapping collections
                        // forever, but it's a pretty common use case to see a varags confused with an array.

                        string element = (string)LanguagePrimitives.ConvertTo(e.Current, typeof(string), CultureInfo.InvariantCulture);

                        if (!string.IsNullOrEmpty(element))
                        {
                            // Prepend a space if the stringbuilder isn't empty...
                            // We could consider using $OFS here but that's probably more
                            // effort than is really needed...
                            if (sb.Length > 0)
                                sb.Append(' ');
                            sb.Append(element);
                        }
                    }

                    promptString = sb.ToString();
                }
                else
                {
                    promptString = (string)LanguagePrimitives.ConvertTo(_prompt, typeof(string), CultureInfo.InvariantCulture);
                }

                FieldDescription fd = new FieldDescription(promptString);
                if (AsSecureString)
                {
                    fd.SetParameterType(typeof(System.Security.SecureString));
                }
                else
                {
                    fd.SetParameterType(typeof(string));
                }

                Collection<FieldDescription> fdc = new Collection<FieldDescription>();
                fdc.Add(fd);

                Dictionary<string, PSObject> result = Host.UI.Prompt(string.Empty, string.Empty, fdc);
                // Result can be null depending on the host implementation. One typical
                // example of a null return is for a canceled dialog.
                if (result != null)
                {
                    foreach (PSObject o in result.Values)
                    {
                        WriteObject(o);
                    }
                }
            }
            else
            {
                object result;
                if (AsSecureString)
                {
                    result = Host.UI.ReadLineAsSecureString();
                }
                else
                {
                    result = Host.UI.ReadLine();
                }

                WriteObject(result);
            }
        }

        #endregion Cmdlet Overrides

        private object _prompt = null;
        private bool _safe = false;
    }
}
