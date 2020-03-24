// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Represents a toggle button used to expand or collapse elements.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public partial class ExpanderButton : ToggleButton
    {
        /// <summary>
        /// Tooltip to show to expand.
        /// </summary>
        protected virtual string ExpandToolTip
        {
            get { return XamlLocalizableResources.AutoResXGen_ManagementList2_ToolTip_32; }
        }

        /// <summary>
        /// Tooltip to show to collapse.
        /// </summary>
        protected virtual string CollapseToolTip
        {
            get { return XamlLocalizableResources.CollapsingTabControl_ExpandButton_AutomationName; }
        }

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

            SetToolTip();
            ToolTip toolTip = (ToolTip)this.ToolTip;
            if (toolTip.IsOpen)
            {
                // need to reset so content changes if already open
                toolTip.IsOpen = false;
                toolTip.IsOpen = true;
            }
        }

        /// <summary>
        /// Called when it has keyboard focus.
        /// </summary>
        /// <param name="args">The event data that describes getting keyboard focus.</param>
        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs args)
        {
            SetToolTip();
            ((ToolTip)this.ToolTip).IsOpen = true;
        }

        /// <summary>
        /// Called when it lost keyboard focus.
        /// </summary>
        /// <param name="args">The event data that describes losing keyboard focus.</param>
        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs args)
        {
            if (this.ToolTip is ToolTip toolTip)
            {
                toolTip.IsOpen = false;
            }
        }

        private void SetToolTip()
        {
            ToolTip toolTip;
            if (this.ToolTip is ToolTip)
            {
                toolTip = (ToolTip)this.ToolTip;
            }
            else
            {
                toolTip = new ToolTip();
            }

            toolTip.Content = (this.IsChecked == true) ? CollapseToolTip : ExpandToolTip;
            toolTip.PlacementTarget = this;
            toolTip.Placement = PlacementMode.Bottom;
            this.ToolTip = toolTip;
        }
    }
}
