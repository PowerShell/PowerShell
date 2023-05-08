// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Provides a base automation peer for FrameworkElement controls.
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class ExtendedFrameworkElementAutomationPeer : FrameworkElementAutomationPeer
    {
        #region Fields

        /// <summary>
        /// Gets or sets the control type of the element that is associated with this automation peer.
        /// </summary>
        private AutomationControlType controlType = AutomationControlType.Custom;

        /// <summary>
        /// Gets or sets a value that indicates whether the control should show in the logical tree.
        /// </summary>
        private bool isControlElement = true;

        #endregion

        #region Structors

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedFrameworkElementAutomationPeer" /> class.
        /// </summary>
        /// <param name="owner">The owner of the automation peer.</param>
        public ExtendedFrameworkElementAutomationPeer(FrameworkElement owner)
            : base(owner)
        {
            // This constructor intentionally left blank
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedFrameworkElementAutomationPeer" /> class.
        /// </summary>
        /// <param name="owner">The owner of the automation peer.</param>
        /// <param name="controlType">The control type of the element that is associated with the automation peer.</param>
        public ExtendedFrameworkElementAutomationPeer(FrameworkElement owner, AutomationControlType controlType)
            : this(owner)
        {
            this.controlType = controlType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedFrameworkElementAutomationPeer" /> class.
        /// </summary>
        /// <param name="owner">The owner of the automation peer.</param>
        /// <param name="controlType">The control type of the element that is associated with the automation peer.</param>
        /// <param name="isControlElement">Whether the element should show in the logical tree.</param>
        public ExtendedFrameworkElementAutomationPeer(FrameworkElement owner, AutomationControlType controlType, bool isControlElement)
            : this(owner, controlType)
        {
            this.isControlElement = isControlElement;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Gets the class name.
        /// </summary>
        /// <returns>The class name.</returns>
        protected override string GetClassNameCore()
        {
            return this.Owner.GetType().Name;
        }

        /// <summary>
        /// Gets the control type of the element that is associated with the automation peer.
        /// </summary>
        /// <returns>Returns the control type of the element that is associated with the automation peer.</returns>
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return this.controlType;
        }

        /// <summary>
        /// Gets a value that indicates whether the element is understood by the user as interactive or as contributing to the logical structure of the control in the GUI. Called by IsControlElement().
        /// </summary>
        /// <returns>This method always returns true.</returns>
        protected override bool IsControlElementCore()
        {
            return this.isControlElement;
        }

        #endregion
    }
}
