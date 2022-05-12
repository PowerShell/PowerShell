// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The base class for the */property commands.
    /// </summary>
    public class ItemPropertyCommandBase : CoreCommandWithCredentialsBase
    {
        #region Parameters

        /// <summary>
        /// Gets or sets the filter parameter.
        /// </summary>
        [Parameter]
        public override string Filter
        {
            get
            {
                return base.Filter;
            }

            set
            {
                base.Filter = value;
            }
        }

        /// <summary>
        /// Gets or sets the include property.
        /// </summary>
        [Parameter]
        public override string[] Include
        {
            get
            {
                return base.Include;
            }

            set
            {
                base.Include = value;
            }
        }

        /// <summary>
        /// Gets or sets the exclude property.
        /// </summary>
        [Parameter]
        public override string[] Exclude
        {
            get
            {
                return base.Exclude;
            }

            set
            {
                base.Exclude = value;
            }
        }
        #endregion Parameters

        #region parameter data

        /// <summary>
        /// The path to the item.
        /// </summary>
        internal string[] paths = Array.Empty<string>();

        #endregion parameter data
    }
}
