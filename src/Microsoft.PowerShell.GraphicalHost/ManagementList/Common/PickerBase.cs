//-----------------------------------------------------------------------
// <copyright file="PickerBase.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.Management.UI.Internal
{
    using System;
    using System.Windows;
    using System.Collections.Generic;
    using System.Text;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Data;
    using System.Windows.Controls.Primitives;
    using System.Diagnostics;
    using System.Windows.Threading;
    using System.Diagnostics.CodeAnalysis;

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
                this.Loaded += new RoutedEventHandler(this.PickerBase_Loaded_ApplyDropDownButtonTemplate);
                return;
            }

            if (null != this.DropDownButtonTemplate && !ReferenceEquals(this.dropDownButton.Template, this.DropDownButtonTemplate))
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
                this.dropDown.Loaded += new RoutedEventHandler(this.DropDown_Loaded_FocusDropDown);
            }

            if (null != this.dropDown.Child && !this.dropDown.IsAncestorOf((DependencyObject)Keyboard.FocusedElement))
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
            this.dropDown.Opened += new EventHandler(this.DropDown_Opened);
            this.dropDown.Closed += new EventHandler(this.DropDown_Closed);
        }

        private void DetachFromVisualTree()
        {
            if (null != this.dropDown)
            {
                this.dropDown.Opened -= this.DropDown_Opened;
                this.dropDown.Closed -= this.DropDown_Closed;
            }
        }

        #endregion Apply Template
    }
}
