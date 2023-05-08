// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Represents a decorator that is always visible in the automation tree, indicating that its descendents belong to a logical group.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class AutomationGroup : ContentControl
    {
        /// <summary>
        /// Returns the <see cref="AutomationPeer"/> implementations for this control.
        /// </summary>
        /// <returns>The <see cref="AutomationPeer"/> implementations for this control.</returns>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ExtendedFrameworkElementAutomationPeer(this, AutomationControlType.Group, true);
        }
    }
}
