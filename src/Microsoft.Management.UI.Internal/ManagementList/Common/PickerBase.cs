// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Implements a re-usable base component useful for showing
    /// Picker-like controls.
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public partial class PickerBase : HeaderedContentControl
    {
        /// <summary>
        /// Creates a new instance of the PickerBase class.
        /// </summary>
        public PickerBase()
        {
            // empty
        }

        partial void OnCloseDropDownExecutedImplementation(ExecutedRoutedEventArgs e)
        {
            this.IsOpen = false;
        }

        #region DropDownButtonTemplate Changed

        partial void OnDropDownButtonTemplateChangedImplementation(PropertyChangedEventArgs<ControlTemplate> e)
        {
            this.ApplyDropDownButtonTemplate();
        }

        private void ApplyDropDownButtonTemplate()
        {
            if (!this.IsLoaded)
            {
                this.ApplyTemplate();
                this.Loaded += this.PickerBase_Loaded_ApplyDropDownButtonTemplate;
                return;
            }

            if (this.DropDownButtonTemplate != null && !ReferenceEquals(this.dropDownButton.Template, this.DropDownButtonTemplate))
            {
                this.dropDownButton.Template = this.DropDownButtonTemplate;
            }
        }

        private void PickerBase_Loaded_ApplyDropDownButtonTemplate(object sender, RoutedEventArgs e)
        {
            this.Loaded -= this.PickerBase_Loaded_ApplyDropDownButtonTemplate;
            this.ApplyDropDownButtonTemplate();
        }

        #endregion DropDownButtonTemplate Changed

        #region DropDown IsOpen Handlers

        private void DropDown_Opened(object sender, EventArgs e)
        {
            this.FocusDropDown();
        }

        private void FocusDropDown()
        {
            if (!this.dropDown.IsLoaded)
            {
                this.dropDown.Loaded += this.DropDown_Loaded_FocusDropDown;
            }

            if (this.dropDown.Child != null && !this.dropDown.IsAncestorOf((DependencyObject)Keyboard.FocusedElement))
            {
                this.dropDown.Child.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            }
        }

        private void DropDown_Loaded_FocusDropDown(object sender, RoutedEventArgs e)
        {
            this.Loaded -= this.DropDown_Loaded_FocusDropDown;
            this.FocusDropDown();
        }

        private void DropDown_Closed(object sender, EventArgs e)
        {
            if (this.dropDown.IsKeyboardFocusWithin || Keyboard.FocusedElement == null)
            {
                this.dropDownButton.Focus();
            }
        }

        #endregion DropDown IsOpen Handlers

        #region Apply Template

        partial void PostOnApplyTemplate()
        {
            this.AttachToVisualTree();
            this.ApplyDropDownButtonTemplate();
        }

        partial void PreOnApplyTemplate()
        {
            this.DetachFromVisualTree();
        }

        private void AttachToVisualTree()
        {
            this.dropDown.Opened += this.DropDown_Opened;
            this.dropDown.Closed += this.DropDown_Closed;
        }

        private void DetachFromVisualTree()
        {
            if (this.dropDown != null)
            {
                this.dropDown.Opened -= this.DropDown_Opened;
                this.dropDown.Closed -= this.DropDown_Closed;
            }
        }

        #endregion Apply Template
    }
}
