//-----------------------------------------------------------------------
// <copyright file="AutomationTextBlockAutomationPeer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Provides an automation peer for AutomationTextBlock.
// </summary>
//-----------------------------------------------------------------------

namespace Microsoft.Management.UI.Internal
{
    #region Using Directives

    using System.Diagnostics.CodeAnalysis;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;

    #endregion

    /// <summary>
    /// Provides an automation peer for AutomationTextBlock.
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    internal class AutomationTextBlockAutomationPeer : TextBlockAutomationPeer
    {
        #region Structors

        /// <summary>
        /// Initializes a new instance of the <see cref="Microsoft.Management.UI.Internal.AutomationTextBlockAutomationPeer" /> class.
        /// </summary>
        /// <param name="owner">The owner of the automation peer.</param>
        public AutomationTextBlockAutomationPeer(TextBlock owner)
            : base(owner)
        {
            // This constructor intentionally left blank
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Gets a value that indicates whether the element is understood by the user as interactive or as contributing to the logical structure of the control in the GUI. Called by IsControlElement().
        /// </summary>
        /// <returns>This method always returns true.</returns>
        protected override bool IsControlElementCore()
        {
            return true;
        }

        /// <summary>
        /// Gets the class name.
        /// </summary>
        /// <returns>The class name.</returns>
        protected override string GetClassNameCore()
        {
            return this.Owner.GetType().Name;
        }

        #endregion
    }
}
