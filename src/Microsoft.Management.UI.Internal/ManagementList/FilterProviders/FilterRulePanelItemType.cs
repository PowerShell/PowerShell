// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The FilterRulePanelItemType enum is used to classify a <see cref="FilterRulePanelItem"/>'s
    /// hierarchical relationship within a <see cref="FilterRulePanel"/>.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public enum FilterRulePanelItemType
    {
        /// <summary>
        /// A FilterRulePanelItemType of FirstHeader indicates that a FilterRulePanelItem
        /// is the first item in the FilterRulePanel.
        /// </summary>
        FirstHeader = 0,

        /// <summary>
        /// A FilterRulePanelItemType of Header indicates that a FilterRulePanelItem with
        /// some GroupId is the first item in the FilterRulePanel with that GroupId.
        /// </summary>
        Header = 1,

        /// <summary>
        /// A FilterRulePanelItemType of Item indicates that a FilterRulePanelItem with
        /// some GroupId is not the first item in the FilterRulePanel with that GroupId.
        /// </summary>
        Item = 2
    }
}
