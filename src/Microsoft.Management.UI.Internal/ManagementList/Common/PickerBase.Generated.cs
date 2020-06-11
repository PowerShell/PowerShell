// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region StyleCop Suppression - generated code
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Microsoft.Management.UI.Internal
{

    /// <summary>
    /// This control provides basic functionality for Picker-like controls.
    /// </summary>
    /// <remarks>
    ///
    ///
    /// If a custom template is provided for this control, then the template MUST provide the following template parts:
    ///
    ///     PART_DropDown - A required template part which must be of type DismissiblePopup.  The dropdown which hosts the picker.
    ///     PART_DropDownButton - A required template part which must be of type ToggleButton.  The ToggleButton which controls whether the dropdown is open.
    ///
    /// </remarks>
    [TemplatePart(Name="PART_DropDown", Type=typeof(DismissiblePopup))]
    [TemplatePart(Name="PART_DropDownButton", Type=typeof(ToggleButton))]
    [Localizability(LocalizationCategory.None)]
    partial class PickerBase
    {
        //
        // Fields
        //
        private DismissiblePopup dropDown;
        private ToggleButton dropDownButton;

        //
        // CloseDropDown routed command
        //
        /// <summary>
        /// Informs the PickerBase that it should close the dropdown.
        /// </summary>
        public static readonly RoutedCommand CloseDropDownCommand = new RoutedCommand("CloseDropDown",typeof(PickerBase));

        static private void CloseDropDownCommand_CommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            PickerBase obj = (PickerBase) sender;
            obj.OnCloseDropDownExecuted( e );
        }

        /// <summary>
        /// Called when CloseDropDown executes.
        /// </summary>
        /// <remarks>
        /// Informs the PickerBase that it should close the dropdown.
        /// </remarks>
        protected virtual void OnCloseDropDownExecuted(ExecutedRoutedEventArgs e)
        {
            OnCloseDropDownExecutedImplementation(e);
        }

        partial void OnCloseDropDownExecutedImplementation(ExecutedRoutedEventArgs e);

        //
        // DropDownButtonTemplate dependency property
        //
        /// <summary>
        /// Identifies the DropDownButtonTemplate dependency property.
        /// </summary>
        public static readonly DependencyProperty DropDownButtonTemplateProperty = DependencyProperty.Register( "DropDownButtonTemplate", typeof(ControlTemplate), typeof(PickerBase), new PropertyMetadata( null, DropDownButtonTemplateProperty_PropertyChanged) );

        /// <summary>
        /// Gets or sets a value that controls the visual tree of the DropDown button.
        /// </summary>
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets a value that controls the visual tree of the DropDown button.")]
        [Localizability(LocalizationCategory.None)]
        public ControlTemplate DropDownButtonTemplate
        {
            get
            {
                return (ControlTemplate) GetValue(DropDownButtonTemplateProperty);
            }
            set
            {
                SetValue(DropDownButtonTemplateProperty,value);
            }
        }

        static private void DropDownButtonTemplateProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            PickerBase obj = (PickerBase) o;
            obj.OnDropDownButtonTemplateChanged( new PropertyChangedEventArgs<ControlTemplate>((ControlTemplate)e.OldValue, (ControlTemplate)e.NewValue) );
        }

        /// <summary>
        /// Occurs when DropDownButtonTemplate property changes.
        /// </summary>
        public event EventHandler<PropertyChangedEventArgs<ControlTemplate>> DropDownButtonTemplateChanged;

        /// <summary>
        /// Called when DropDownButtonTemplate property changes.
        /// </summary>
        protected virtual void OnDropDownButtonTemplateChanged(PropertyChangedEventArgs<ControlTemplate> e)
        {
            OnDropDownButtonTemplateChangedImplementation(e);
            RaisePropertyChangedEvent(DropDownButtonTemplateChanged, e);
        }

        partial void OnDropDownButtonTemplateChangedImplementation(PropertyChangedEventArgs<ControlTemplate> e);

        //
        // DropDownStyle dependency property
        //
        /// <summary>
        /// Identifies the DropDownStyle dependency property.
        /// </summary>
        public static readonly DependencyProperty DropDownStyleProperty = DependencyProperty.Register( "DropDownStyle", typeof(Style), typeof(PickerBase), new PropertyMetadata( null, DropDownStyleProperty_PropertyChanged) );

        /// <summary>
        /// Gets or sets the style of the drop-down.
        /// </summary>
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets the style of the drop-down.")]
        [Localizability(LocalizationCategory.None)]
        public Style DropDownStyle
        {
            get
            {
                return (Style) GetValue(DropDownStyleProperty);
            }
            set
            {
                SetValue(DropDownStyleProperty,value);
            }
        }

        static private void DropDownStyleProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            PickerBase obj = (PickerBase) o;
            obj.OnDropDownStyleChanged( new PropertyChangedEventArgs<Style>((Style)e.OldValue, (Style)e.NewValue) );
        }

        /// <summary>
        /// Occurs when DropDownStyle property changes.
        /// </summary>
        public event EventHandler<PropertyChangedEventArgs<Style>> DropDownStyleChanged;

        /// <summary>
        /// Called when DropDownStyle property changes.
        /// </summary>
        protected virtual void OnDropDownStyleChanged(PropertyChangedEventArgs<Style> e)
        {
            OnDropDownStyleChangedImplementation(e);
            RaisePropertyChangedEvent(DropDownStyleChanged, e);
        }

        partial void OnDropDownStyleChangedImplementation(PropertyChangedEventArgs<Style> e);

        //
        // IsOpen dependency property
        //
        /// <summary>
        /// Identifies the IsOpen dependency property.
        /// </summary>
        public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register( "IsOpen", typeof(bool), typeof(PickerBase), new PropertyMetadata( BooleanBoxes.FalseBox, IsOpenProperty_PropertyChanged) );

        /// <summary>
        /// Gets or sets a value indicating whether the Popup is visible.
        /// </summary>
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets a value indicating whether the Popup is visible.")]
        [Localizability(LocalizationCategory.None)]
        public bool IsOpen
        {
            get
            {
                return (bool) GetValue(IsOpenProperty);
            }
            set
            {
                SetValue(IsOpenProperty,BooleanBoxes.Box(value));
            }
        }

        static private void IsOpenProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            PickerBase obj = (PickerBase) o;
            obj.OnIsOpenChanged( new PropertyChangedEventArgs<bool>((bool)e.OldValue, (bool)e.NewValue) );
        }

        /// <summary>
        /// Occurs when IsOpen property changes.
        /// </summary>
        public event EventHandler<PropertyChangedEventArgs<bool>> IsOpenChanged;

        /// <summary>
        /// Called when IsOpen property changes.
        /// </summary>
        protected virtual void OnIsOpenChanged(PropertyChangedEventArgs<bool> e)
        {
            OnIsOpenChangedImplementation(e);
            RaisePropertyChangedEvent(IsOpenChanged, e);
        }

        partial void OnIsOpenChangedImplementation(PropertyChangedEventArgs<bool> e);

        /// <summary>
        /// Called when a property changes.
        /// </summary>
        private void RaisePropertyChangedEvent<T>(EventHandler<PropertyChangedEventArgs<T>> eh, PropertyChangedEventArgs<T> e)
        {
            if (eh != null)
            {
                eh(this,e);
            }
        }

        //
        // OnApplyTemplate
        //

        /// <summary>
        /// Called when ApplyTemplate is called.
        /// </summary>
        public override void OnApplyTemplate()
        {
            PreOnApplyTemplate();
            base.OnApplyTemplate();
            this.dropDown = WpfHelp.GetTemplateChild<DismissiblePopup>(this,"PART_DropDown");
            this.dropDownButton = WpfHelp.GetTemplateChild<ToggleButton>(this,"PART_DropDownButton");
            PostOnApplyTemplate();
        }

        partial void PreOnApplyTemplate();

        partial void PostOnApplyTemplate();

        //
        // Static constructor
        //

        /// <summary>
        /// Called when the type is initialized.
        /// </summary>
        static PickerBase()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PickerBase), new FrameworkPropertyMetadata(typeof(PickerBase)));
            CommandManager.RegisterClassCommandBinding( typeof(PickerBase), new CommandBinding( PickerBase.CloseDropDownCommand, CloseDropDownCommand_CommandExecuted ));
            StaticConstructorImplementation();
        }

        static partial void StaticConstructorImplementation();

    }
}
#endregion
