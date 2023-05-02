// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;

using Debug = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class implements `Get-SecureRandom` cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "SecureRandom", DefaultParameterSetName = GetRandomCommandBase.RandomNumberParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2235055", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(int), typeof(long), typeof(double))]
    public sealed class GetSecureRandomCommand : GetRandomCommandBase
    {
        // nothing unique from base class
    }
}
