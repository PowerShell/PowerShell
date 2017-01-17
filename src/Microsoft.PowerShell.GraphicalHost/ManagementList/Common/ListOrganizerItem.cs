//-----------------------------------------------------------------------
// <copyright file="ListOrganizerItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.Management.UI.Internal
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;

    /// <content>
    /// Partial class implementation for ListOrganizerItem control.
    /// </content>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public partial class ListOrganizerItem : Control
    {
        private string startingText;
        private FrameworkElement templatedParent;

        /// <summary>
        /// Creates a new instance of the ListOrganizerItem class.
        /// </summary>
        public ListOrganizerItem()
        {
            // empty
        }

        /// <summary>
        /// Gets a value indicating whether the item is in edit mode.
        /// </summary>
        public bool IsInEditMode
        {
            get
            {
                return (null != this.renameButton) ? this.renameButton.IsChecked.Value : false;
            }
        }

        /// <summary>
        /// Selects the current item.
        /// </summary>
        public void Select()
        {
            if (!this.IsLoaded)
            {
                this.Loaded += new RoutedEventHandler(this.ListOrganizerItem_Loaded_SelectItem);
                this.ApplyTemplate();
                return;
            }

            CommandHelper.ExecuteCommand(this.linkButton.Command, this.linkButton.CommandParameter, this.linkButton.CommandTarget);
        }

        /// <summary>
        /// Allows modification of the item.
        /// </summary>
        public void Rename()
        {
            this.renameButton.IsChecked = true;
        }

        /// <summary>
        /// Deletes the item.
        /// </summary>
        public void Delete()
        {
            CommandHelper.ExecuteCommand(this.deleteButton.Command, this.deleteButton.CommandParameter, this.deleteButton.CommandTarget);
        }

        /// <summary>
        /// Provides class handling for the KeyDown routed event that
        /// occurs when the user presses a key while this control has focus.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (this.IsInEditMode)
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Insert:
                    this.Rename();
                    e.Handled = true;
                    break;

                case Key.Delete:
                    this.Delete();
                    e.Handled = true;
                    break;

                case Key.Enter:
                case Key.Space:
                    this.Select();
                    e.Handled = true;
                    break;

                default:
                    break;
            }
        }

        private void TemplatedParent_OnKeyDown(object sender, KeyEventArgs e)
        {
            this.OnKeyDown(e);
        }

        private void ListOrganizerItem_Loaded_SelectItem(object sender, RoutedEventArgs e)
        {
            this.Loaded -= this.ListOrganizerItem_Loaded_SelectItem;
            this.Select();
        }

        #region EditBox Event Handlers

        private void EditBox_LostFocus(object sender, RoutedEventArgs e)
        {
            this.ChangeFromEditToDisplayMode();
        }

        private void EditBox_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    this.ChangeFromEditToDisplayMode();
                    e.Handled = true;
                    break;

                case Key.Escape:
                    this.RevertTextAndChangeFromEditToDisplayMode();
                    e.Handled = true;
                    break;

                default:
                    break;
            }
        }

        private void RevertTextAndChangeFromEditToDisplayMode()
        {
            this.editBox.Text = this.startingText;
            this.ChangeFromEditToDisplayMode();
        }

        private void ChangeFromEditToDisplayMode()
        {
            // NOTE : This is to resolve a race condition where clicking
            // on the rename button causes the the edit box to change and
            // then have re-toggle.
            DependencyObject d = Mouse.DirectlyOver as DependencyObject;
            if (null == d || !(this.renameButton.IsAncestorOf(d) && Mouse.LeftButton == MouseButtonState.Pressed))
            {
                this.renameButton.IsChecked = false;
            }

            if (!this.IsKeyboardFocusWithin)
            {
                this.Focus();
            }
        }

        private void EditBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.editBox.IsVisible)
            {
                this.startingText = this.editBox.Text;

                this.editBox.Focus();
                this.editBox.SelectAll();
            }
        }

        #endregion EditBox Event Handlers

        partial void OnTextContentPropertyNameChangedImplementation(PropertyChangedEventArgs<string> e)
        {
            this.UpdateTextContentBindings();
        }

        private void UpdateTextContentBindings()
        {
            if (!this.IsLoaded)
            {
                this.Loaded += new RoutedEventHandler(this.ListOrganizerItem_Loaded_UpdateTextContentBindings);
                this.ApplyTemplate();
                return;
            }

            if (!String.IsNullOrEmpty(this.TextContentPropertyName))
            {
                Binding b = new Binding(this.TextContentPropertyName);
                b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

                this.linkButton.SetBinding(Button.ContentProperty, b);
                this.editBox.SetBinding(TextBox.TextProperty, b);
            }
            else
            {
                BindingOperations.ClearBinding(this.linkButton, Button.ContentProperty);
                BindingOperations.ClearBinding(this.editBox, TextBox.TextProperty);
            }
        }

        private void ListOrganizerItem_Loaded_UpdateTextContentBindings(object sender, RoutedEventArgs e)
        {
            this.Loaded -= this.ListOrganizerItem_Loaded_UpdateTextContentBindings;
            this.UpdateTextContentBindings();
        }

        #region OnApplyTemplate Helpers

        partial void PreOnApplyTemplate()
        {
            this.DetachFromVisualTree();
        }

        partial void PostOnApplyTemplate()
        {
            this.AttachToVisualTree();
        }

        private void AttachToVisualTree()
        {
            this.editBox.IsVisibleChanged += new DependencyPropertyChangedEventHandler(this.EditBox_IsVisibleChanged);
            this.editBox.KeyDown += new KeyEventHandler(this.EditBox_KeyDown);
            this.editBox.LostFocus += new RoutedEventHandler(this.EditBox_LostFocus);

            this.templatedParent = this.TemplatedParent as FrameworkElement;
            if (null != this.templatedParent)
            {
                this.templatedParent.KeyDown += new KeyEventHandler(this.TemplatedParent_OnKeyDown);
            }
        }

        private void DetachFromVisualTree()
        {
            if (this.editBox != null)
            {
                this.editBox.IsVisibleChanged -= this.EditBox_IsVisibleChanged;
                this.editBox.KeyDown -= this.EditBox_KeyDown;
                this.editBox.LostFocus -= this.EditBox_LostFocus;
            }

            if (null != this.templatedParent)
            {
                this.templatedParent.KeyDown -= this.TemplatedParent_OnKeyDown;
            }
        }

        #endregion OnApplyTemplate Helpers
    }
}
