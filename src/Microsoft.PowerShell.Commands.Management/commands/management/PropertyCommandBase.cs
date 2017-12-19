/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System.Management.Automation;
using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The base class for the */property commands
    /// </summary>
    public class ItemPropertyCommandBase : CoreCommandWithCredentialsBase
    {
        #region Parameters

        /// <summary>
        /// Gets or sets the filter parameter
        /// </summary>
        [Parameter]
        public override string Filter
        {
            get
            {
                return base.Filter;
            } // get

            set
            {
                base.Filter = value;
            } // set
        } // Filter

        /// <summary>
        /// Gets or sets the include property
        /// </summary>
        [Parameter]
        public override string[] Include
        {
            get
            {
                return base.Include;
            } // get

            set
            {
                base.Include = value;
            } // set
        } // Include

        /// <summary>
        /// Gets or sets the exclude property
        /// </summary>
        [Parameter]
        public override string[] Exclude
        {
            get
            {
                return base.Exclude;
            } // get

            set
            {
                base.Exclude = value;
            } // set
        } // Exclude
        #endregion Parameters

        #region parameter data

        /// <summary>
        /// The path to the item
        /// </summary>
        internal string[] paths = new string[0];

        #endregion parameter data
    } // ItemPropertyCommandBase
} // namespace Microsoft.PowerShell.Commands
