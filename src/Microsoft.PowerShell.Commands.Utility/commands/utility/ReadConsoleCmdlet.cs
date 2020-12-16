// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
    [Cmdlet(VerbsCommunications.Read, "Host", DefaultParameterSetName = "AsString", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096610")]
    [OutputType(typeof(string), typeof(SecureString))]
    public sealed class ReadHostCommand : PSCmdlet
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadHostCommand"/> class.
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
        /// Gets or sets to no echo the input as is typed. If set then the cmdlet returns a secure string.
        /// </summary>
        [Parameter(ParameterSetName = "AsSecureString")]
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

        /// <summary>
        /// Gets or sets whether the console will echo the input as is typed. If set then the cmdlet returns a regular string.
        /// </summary>
        [Parameter(ParameterSetName = "AsString")]
        public
        SwitchParameter
        MaskInput
        {
            get;
            set;
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
                    StringBuilder sb = new();

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

                FieldDescription fd = new(promptString);
                if (AsSecureString || MaskInput)
                {
                    fd.SetParameterType(typeof(SecureString));
                }
                else
                {
                    fd.SetParameterType(typeof(string));
                }

                Collection<FieldDescription> fdc = new();
                fdc.Add(fd);

                Dictionary<string, PSObject> result = Host.UI.Prompt(string.Empty, string.Empty, fdc);
                // Result can be null depending on the host implementation. One typical
                // example of a null return is for a canceled dialog.
                if (result != null)
                {
                    foreach (PSObject o in result.Values)
                    {
                        if (MaskInput && o?.BaseObject is SecureString secureString)
                        {
                            WriteObject(Utils.GetStringFromSecureString(secureString));
                        }
                        else
                        {
                            WriteObject(o);
                        }
                    }
                }
            }
            else
            {
                object result;
                if (AsSecureString || MaskInput)
                {
                    result = Host.UI.ReadLineAsSecureString();
                }
                else
                {
                    result = Host.UI.ReadLine();
                }

                if (MaskInput)
                {
                    WriteObject(Utils.GetStringFromSecureString((SecureString)result));
                }
                else
                {
                    WriteObject(result);
                }
            }
        }

        #endregion Cmdlet Overrides

        private object _prompt = null;
        private bool _safe = false;
    }
}
