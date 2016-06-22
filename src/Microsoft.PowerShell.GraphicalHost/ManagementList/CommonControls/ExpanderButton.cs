//-----------------------------------------------------------------------
// <copyright file="ExpanderButton.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.Management.UI.Internal
{
    using System.Windows;
    using System.Windows.Automation;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls.Primitives;

    /// <summary>
    /// Represents a toggle button used to expand or collapse elements.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public partial class ExpanderButton : ToggleButton
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExpanderButton" /> class.
        /// </summary>
        public ExpanderButton()
        {
            // This constructor intentionally left blank
        }

        /// <summary>
        /// Invoked whenever the effective value of any dependency property on this <see cref="ExpanderButton"/> has been updated. The specific dependency property that changed is reported in the arguments parameter. Overrides <see cref="FrameworkElement.OnPropertyChanged"/>.
        /// </summary>
        /// <param name="e">The event data that describes the property that changed, as well as old and new values.</param>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == ExpanderButton.IsCheckedProperty)
            {
                this.OnIsCheckedChanged(e);
            }
        }

        /// <summary>
        /// Called when the <see cref="ToggleButton.IsChecked"/> property changes.
        /// </summary>
        /// <param name="args">The event data that describes the property that changed, as well as old and new values.</param>
        protected void OnIsCheckedChanged(DependencyPropertyChangedEventArgs args)
        {
            if (AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged))
            {
                var peer = UIElementAutomationPeer.CreatePeerForElement(this);

                if (peer != null)
                {
                    var oldValue = (bool?)args.OldValue;
                    var newValue = (bool?)args.NewValue;

                    peer.RaisePropertyChangedEvent(
                        ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                        (oldValue == true) ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
                        (newValue == true) ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
                }
            }
        }
    }
}
