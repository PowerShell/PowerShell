// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Provides an automation peer for <see cref="ExpanderButton"/>.
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class ExpanderButtonAutomationPeer : ToggleButtonAutomationPeer, IExpandCollapseProvider
    {
        #region Fields

        private ExpanderButton expanderButton;

        #endregion

        #region Structors

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpanderButtonAutomationPeer" /> class.
        /// </summary>
        /// <param name="owner">The owner of the automation peer.</param>
        public ExpanderButtonAutomationPeer(ExpanderButton owner)
            : base(owner)
        {
            this.expanderButton = owner;
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
        /// Gets the control pattern for the <see cref="ExpanderButton"/> that is associated with this <see cref="ExpanderButtonAutomationPeer"/>.
        /// </summary>
        /// <param name="patternInterface">Specifies the control pattern that is returned.</param>
        /// <returns>The control pattern for the <see cref="ExpanderButton"/> that is associated with this <see cref="ExpanderButtonAutomationPeer"/>.</returns>
        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.ExpandCollapse ||
                patternInterface == PatternInterface.Toggle)
            {
                return this;
            }

            return null;
        }

        #endregion

        #region IExpandCollapseProvider Implementations

        /// <summary>
        /// Gets the expand/collapse state of this <see cref="ExpanderButton"/> instance.
        /// </summary>
        ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState
        {
            get
            {
                if (this.expanderButton.IsChecked == true)
                {
                    return ExpandCollapseState.Expanded;
                }
                else
                {
                    return ExpandCollapseState.Collapsed;
                }
            }
        }

        /// <summary>
        /// Expands this instance of <see cref="ExpanderButton"/>.
        /// </summary>
        void IExpandCollapseProvider.Expand()
        {
            if (!this.IsEnabled())
            {
                throw new ElementNotEnabledException();
            }

            this.expanderButton.IsChecked = true;
        }

        /// <summary>
        /// Collapses this instance of <see cref="ExpanderButton"/>.
        /// </summary>
        void IExpandCollapseProvider.Collapse()
        {
            if (!this.IsEnabled())
            {
                throw new ElementNotEnabledException();
            }

            this.expanderButton.IsChecked = false;
        }

        #endregion
    }
}
