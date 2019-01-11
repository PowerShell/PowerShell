// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Management.Automation;
using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The base class for the */content commands that also take
    /// a passthrough parameter.
    /// </summary>
    public class PassThroughContentCommandBase : ContentCommandBase
    {
        #region Parameters

        /// <summary>
        /// Gets or sets the passthrough parameter to the command.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru
        {
            get
            {
                return _passThrough;
            }

            set
            {
                _passThrough = value;
            }
        }

        /// <summary>
        /// Determines if the provider for the specified path supports ShouldProcess.
        /// </summary>
        /// <value></value>
        protected override bool ProviderSupportsShouldProcess
        {
            get
            {
                return base.DoesProviderSupportShouldProcess(base.Path);
            }
        }

        #endregion Parameters

        #region parameter data

        /// <summary>
        /// Determines if the content returned from the provider should
        /// be passed through to the pipeline.
        /// </summary>
        private bool _passThrough;

        #endregion parameter data

        #region protected members

        /// <summary>
        /// Initializes a CmdletProviderContext instance to the current context of
        /// the command.
        /// </summary>
        /// <returns>
        /// A CmdletProviderContext instance initialized to the context of the current
        /// command.
        /// </returns>
        internal CmdletProviderContext GetCurrentContext()
        {
            CmdletProviderContext currentCommandContext = CmdletProviderContext;
            currentCommandContext.PassThru = PassThru;
            return currentCommandContext;
        }

        #endregion protected members
    }
}

