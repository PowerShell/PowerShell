// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands.Utility.commands.utility
{
    /// <summary>
    /// Implements Get-SubString command
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "SubString", DefaultParameterSetName = "substring")]
    [OutputType(typeof(string))]
    public class GetSubStringCommand : Cmdlet
    {
        #region Parameters
        /// <summary>
        /// What string to trim with
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "trim")]
        public string Trim;

        /// <summary>
        /// What string to trim the start with
        /// </summary>
        [Parameter(ParameterSetName = "trimpartial")]
        public string TrimStart;

        /// <summary>
        /// What string to trim the end with
        /// </summary>
        [Parameter(ParameterSetName = "trimpartial")]
        public string TrimEnd;

        /// <summary>
        /// Where to start taking a substring
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "substring")]
        public int Start;

        /// <summary>
        /// How long the picked substring should be
        /// </summary>
        [Parameter(Position = 1, ParameterSetName = "substring")]
        public int Length;

        /// <summary>
        /// The strings to process
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public string[] InputString;
        #endregion Parameters

        #region Methods
        /// <summary>
        /// Processes the individual items that were input
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (string item in InputString)
            {
                switch (_ParameterSetName)
                {
                    case "substring":
                        if (Length > 0)
                            WriteObject(item.Substring(Start, Length));
                        else
                            WriteObject(item.Substring(Start));
                        break;
                    case "trim":
                        WriteObject(item.Trim(Trim.ToCharArray()));
                        break;
                    case "trimpartial":
                        if (!String.IsNullOrEmpty(TrimStart) && !String.IsNullOrEmpty(TrimEnd))
                            WriteObject(item.TrimStart(TrimStart.ToCharArray()).TrimEnd(TrimEnd.ToCharArray()));
                        else if (!String.IsNullOrEmpty(TrimStart))
                            WriteObject(item.TrimStart(TrimStart.ToCharArray()));
                        else
                            WriteObject(item.TrimEnd(TrimEnd.ToCharArray()));
                        break;
                    default:
                        break;
                }
            }
        }
        #endregion Methods
    }
}
