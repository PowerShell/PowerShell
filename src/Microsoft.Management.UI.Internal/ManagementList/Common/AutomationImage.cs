// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Provides a <see cref="Image"/> control that is always visible in the automation tree.
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    [Description("Provides a System.Windows.Controls.Image control that is always visible in the automation tree.")]
    public class AutomationImage : Image
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomationImage" /> class.
        /// </summary>
        public AutomationImage()
        {
            // This constructor intentionally left blank
        }

        #endregion

        #region Overides

        /// <summary>
        /// Returns the <see cref="System.Windows.Automation.Peers.AutomationPeer"/> implementations for this control.
        /// </summary>
        /// <returns>The <see cref="System.Windows.Automation.Peers.AutomationPeer"/> implementations for this control.</returns>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new AutomationImageAutomationPeer(this);
        }

        #endregion
    }

    /// <summary>
    /// Provides an automation peer for AutomationImage.
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    internal class AutomationImageAutomationPeer : ImageAutomationPeer
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Microsoft.Management.UI.Internal.AutomationImageAutomationPeer" /> class.
        /// </summary>
        /// <param name="owner">The owner of the automation peer.</param>
        public AutomationImageAutomationPeer(Image owner)
            : base(owner)
        {
            // This constructor intentionally left blank
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Gets a value that indicates whether the element is understood by the user as interactive or as contributing to the logical structure of the control in the GUI. Called by IsControlElement().
        /// </summary>
        /// <returns>This method always returns false.</returns>
        protected override bool IsControlElementCore()
        {
            return false;
        }

        #endregion
    }
}
