// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    /// <summary>
    /// Control taht shows cmdlets in a module and details for a selected cmdlet.
    /// </summary>
    public partial class ShowModuleControl : UserControl
    {
        /// <summary>
        /// Field used for the Owner parameter.
        /// </summary>
        private Window owner;

        /// <summary>
        /// Initializes a new instance of the ShowModuleControl class.
        /// </summary>
        public ShowModuleControl()
        {
            InitializeComponent();

            // See comment in method summary to understand why this event is handled
            this.CommandList.PreviewMouseMove += this.CommandList_PreviewMouseMove;

            // See comment in method summary to understand why this event is handled
            this.CommandList.SelectionChanged += this.CommandList_SelectionChanged;
        }

        /// <summary>
        /// Gets or sets the owner of the container.
        /// </summary>
        public Window Owner
        {
            get { return this.owner; }
            set { this.owner = value; }
        }

        #region Events Handlers
        /// <summary>
        /// WPF has an interesting feature in list selection where if you hold the mouse button down,
        /// it will select the item under it, but if you keep the mouse button down and move the mouse
        /// (if the list supported drag and drop, the mouse action would be the same as dragging) it
        /// will select other list items.
        /// If the first selection change causes  details for the item to be displayed and resizes the list
        /// the selection can skip to another list item it happend to be over as the list got resized.
        /// In summary, resizing the list on selection can cause a selection bug. If the user selects an
        /// item in the end of the list the next item downwards can be selected.
        /// The WPF drag-and-select feature is not a standard win32 list behavior, and we can do without it
        /// since it causes this problem.
        /// WPF sets up this behavior by using a mouse capture. We undo the behavior in the handler below
        /// which removes the behavior.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void CommandList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (this.CommandList.IsMouseCaptured)
            {
                this.CommandList.ReleaseMouseCapture();
            }
        }

        /// <summary>
        /// Ensures the selected item is scrolled into view and that the list is focused.
        /// An item could be out of the view if the selection was changed in the object model
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void CommandList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.CommandList.SelectedItem == null)
            {
                return;
            }

            this.CommandList.ScrollIntoView(this.CommandList.SelectedItem);
        }
        #endregion Events Handlers
    }
}
