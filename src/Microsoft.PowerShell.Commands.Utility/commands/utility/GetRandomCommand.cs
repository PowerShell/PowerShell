// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading;

using Debug = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class implements `Get-Random` cmdlet.
    /// </summary>
    /// <!-- author: LukaszA -->
    [Cmdlet(VerbsCommon.Get, "Random", DefaultParameterSetName = GetRandomCommandBase.RandomNumberParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097016", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(int), typeof(long), typeof(double))]
    public sealed class GetRandomCommand : GetRandomCommandBase
    {
        /// <summary>
        /// Seed used to reinitialize random numbers generator.
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        public int? SetSeed { get; set; }

        /// <summary>
        /// This method implements the BeginProcessing method for get-random command.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (SetSeed.HasValue)
            {
                Generator = new PolymorphicRandomNumberGenerator(SetSeed.Value);
            }

            base.BeginProcessing();
        }
    }
}
