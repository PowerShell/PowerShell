// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using System.Management.Automation.Language;

namespace System.Management.Automation
{
    /// <summary>
    /// The parameters for the paging support enabled by <see cref="CmdletCommonMetadataAttribute.SupportsPaging"/>.
    /// Includes: -IncludeTotalCount, -Skip [int], -First [int]
    /// </summary>
    public sealed class PagingParameters
    {
        #region ctor

        internal PagingParameters(MshCommandRuntime commandRuntime)
        {
            if (commandRuntime == null)
            {
                throw PSTraceSource.NewArgumentNullException("commandRuntime");
            }

            commandRuntime.PagingParameters = this;
        }

        #endregion ctor

        #region parameters

        /// <summary>
        /// Gets or sets the value of the -IncludeTotalCount parameter for all cmdlets that support paging.
        /// </summary>
        [Parameter]
        public SwitchParameter IncludeTotalCount { get; set; }

        /// <summary>
        /// Gets or sets the value of the -Skip parameter for all cmdlets that support paging.
        /// If the user doesn't specify anything, the default is <c>0</c>.
        /// </summary>
        [Parameter]
        public UInt64 Skip { get; set; }

        /// <summary>
        /// Gets or sets the value of the -First parameter for all cmdlets that support paging.
        /// If the user doesn't specify anything, the default is <see cref="System.UInt64.MaxValue"/>.
        /// </summary>
        [Parameter]
        public UInt64 First { get; set; } = UInt64.MaxValue;

        #endregion parameters

        #region emitting total count

        /// <summary>
        /// A helper method for creating an object that represents a total count
        /// of objects that the cmdlet would return without paging
        /// (this can be more than the size of the page specified in the <see cref="First"/> cmdlet parameter).
        /// </summary>
        /// <param name="totalCount">A total count of objects that the cmdlet would return without paging.</param>
        /// <param name="accuracy">
        /// accuracy of the <paramref name="totalCount"/> parameter.
        /// <c>1.0</c> means 100% accurate;
        /// <c>0.0</c> means that total count is unknown;
        /// anything in-between means that total count is estimated
        /// </param>
        /// <returns>An object that represents a total count of objects that the cmdlet would return without paging.</returns>
        public PSObject NewTotalCount(UInt64 totalCount, double accuracy)
        {
            PSObject result = new PSObject(totalCount);

            string toStringMethodBody = string.Format(
                CultureInfo.CurrentCulture,
                @"
                    $totalCount = $this.PSObject.BaseObject
                    switch ($this.Accuracy) {{
                        {{ $_ -ge 1.0 }} {{ '{0}' -f $totalCount }}
                        {{ $_ -le 0.0 }} {{ '{1}' -f $totalCount }}
                        default          {{ '{2}' -f $totalCount }}
                    }}
                ",
                CodeGeneration.EscapeSingleQuotedStringContent(CommandBaseStrings.PagingSupportAccurateTotalCountTemplate),
                CodeGeneration.EscapeSingleQuotedStringContent(CommandBaseStrings.PagingSupportUnknownTotalCountTemplate),
                CodeGeneration.EscapeSingleQuotedStringContent(CommandBaseStrings.PagingSupportEstimatedTotalCountTemplate));
            PSScriptMethod toStringMethod = new PSScriptMethod("ToString", ScriptBlock.Create(toStringMethodBody));
            result.Members.Add(toStringMethod);

            accuracy = Math.Max(0.0, Math.Min(1.0, accuracy));
            PSNoteProperty statusProperty = new PSNoteProperty("Accuracy", accuracy);
            result.Members.Add(statusProperty);

            return result;
        }

        #endregion emitting total count
    }
}

namespace System.Management.Automation.Internal
{
    /// <summary>
    /// The declaration of parameters for the ShouldProcess mechanisms. -Whatif, and -Confirm.
    /// </summary>
    public sealed class ShouldProcessParameters
    {
        #region ctor

        /// <summary>
        /// Constructs an instance with the specified command instance.
        /// </summary>
        /// <param name="commandRuntime">
        /// The instance of the command that the parameters should set the
        /// user feedback properties on when the parameters get bound.
        /// </param>
        internal ShouldProcessParameters(MshCommandRuntime commandRuntime)
        {
            if (commandRuntime == null)
            {
                throw PSTraceSource.NewArgumentNullException("commandRuntime");
            }

            _commandRuntime = commandRuntime;
        }
        #endregion ctor

        #region parameters

        /// <summary>
        /// Gets or sets the value of the -Whatif parameter for all cmdlets.
        /// </summary>
        [Parameter]
        [Alias("wi")]
        public SwitchParameter WhatIf
        {
            get
            {
                return _commandRuntime.WhatIf;
            }

            set
            {
                _commandRuntime.WhatIf = value;
            }
        }

        /// <summary>
        /// Gets or sets the value of the -Confirm parameter for all cmdlets.
        /// </summary>
        [Parameter]
        [Alias("cf")]
        public SwitchParameter Confirm
        {
            get
            {
                return _commandRuntime.Confirm;
            }

            set
            {
                _commandRuntime.Confirm = value;
            }
        }
        #endregion parameters

        private MshCommandRuntime _commandRuntime;
    }

    /// <summary>
    /// The declaration of parameters for the Transactions mechanisms. -UseTransaction, and -BypassTransaction.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Justification = "These are only exposed by way of the PowerShell core cmdlets that surface them.")]
    public sealed class TransactionParameters
    {
        #region ctor

        /// <summary>
        /// Constructs an instance with the specified command instance.
        /// </summary>
        /// <param name="commandRuntime">
        /// The instance of the command that the parameters should set the
        /// user feedback properties on when the parameters get bound.
        /// </param>
        internal TransactionParameters(MshCommandRuntime commandRuntime)
        {
            _commandRuntime = commandRuntime;
        }
        #endregion ctor

        #region parameters

        /// <summary>
        /// Gets or sets the value of the -UseTransaction parameter for all cmdlets.
        /// </summary>
        [Parameter]
        [Alias("usetx")]
        public SwitchParameter UseTransaction
        {
            get
            {
                return _commandRuntime.UseTransaction;
            }

            set
            {
                _commandRuntime.UseTransaction = value;
            }
        }

        #endregion parameters

        private MshCommandRuntime _commandRuntime;
    }
}

