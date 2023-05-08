// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region StyleCop Suppression - generated code
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace Microsoft.Management.UI.Internal
{

    /// <summary>
    /// A popup which child controls can signal to be dismissed.
    /// </summary>
    /// <remarks>
    /// If a control wants to dismiss the popup then they should execute the DismissPopupCommand on a target in the popup window.
    /// </remarks>
    [Localizability(LocalizationCategory.None)]
    partial class DismissiblePopup
    {
        //
        // DismissPopup routed command
        //
        /// <summary>
        /// A command which child controls can use to tell the popup to close.
        /// </summary>
        public static readonly RoutedCommand DismissPopupCommand = new RoutedCommand("DismissPopup",typeof(DismissiblePopup));

        static private void DismissPopupCommand_CommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            DismissiblePopup obj = (DismissiblePopup) sender;
            obj.OnDismissPopupExecuted( e );
        }

        /// <summary>
        /// Called when DismissPopup executes.
        /// </summary>
        /// <remarks>
        /// A command which child controls can use to tell the popup to close.
        /// </remarks>
        protected virtual void OnDismissPopupExecuted(ExecutedRoutedEventArgs e)
        {
            OnDismissPopupExecutedImplementation(e);
        }

        partial void OnDismissPopupExecutedImplementation(ExecutedRoutedEventArgs e);

        //
        // CloseOnEscape dependency property
        //
        /// <summary>
        /// Identifies the CloseOnEscape dependency property.
        /// </summary>
        public static readonly DependencyProperty CloseOnEscapeProperty = DependencyProperty.Register( "CloseOnEscape", typeof(bool), typeof(DismissiblePopup), new PropertyMetadata( BooleanBoxes.TrueBox, CloseOnEscapeProperty_PropertyChanged) );

        /// <summary>
        /// Gets or sets a value indicating whether the popup closes when ESC is pressed.
        /// </summary>
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets a value indicating whether the popup closes when ESC is pressed.")]
        [Localizability(LocalizationCategory.None)]
        public bool CloseOnEscape
        {
            get
            {
                return (bool) GetValue(CloseOnEscapeProperty);
            }
            set
            {
                SetValue(CloseOnEscapeProperty,BooleanBoxes.Box(value));
            }
        }

        static private void CloseOnEscapeProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            DismissiblePopup obj = (DismissiblePopup) o;
            obj.OnCloseOnEscapeChanged( new PropertyChangedEventArgs<bool>((bool)e.OldValue, (bool)e.NewValue) );
        }

        /// <summary>
        /// Occurs when CloseOnEscape property changes.
        /// </summary>
        public event EventHandler<PropertyChangedEventArgs<bool>> CloseOnEscapeChanged;

        /// <summary>
        /// Called when CloseOnEscape property changes.
        /// </summary>
        protected virtual void OnCloseOnEscapeChanged(PropertyChangedEventArgs<bool> e)
        {
            OnCloseOnEscapeChangedImplementation(e);
            RaisePropertyChangedEvent(CloseOnEscapeChanged, e);
        }

        partial void OnCloseOnEscapeChangedImplementation(PropertyChangedEventArgs<bool> e);

        //
        // FocusChildOnOpen dependency property
        //
        /// <summary>
        /// Identifies the FocusChildOnOpen dependency property.
        /// </summary>
        public static readonly DependencyProperty FocusChildOnOpenProperty = DependencyProperty.Register( "FocusChildOnOpen", typeof(bool), typeof(DismissiblePopup), new PropertyMetadata( BooleanBoxes.TrueBox, FocusChildOnOpenProperty_PropertyChanged) );

        /// <summary>
        /// Gets or sets a value indicating whether focus should be set on the child when the popup opens.
        /// </summary>
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets a value indicating whether focus should be set on the child when the popup opens.")]
        [Localizability(LocalizationCategory.None)]
        public bool FocusChildOnOpen
        {
            get
            {
                return (bool) GetValue(FocusChildOnOpenProperty);
            }
            set
            {
                SetValue(FocusChildOnOpenProperty,BooleanBoxes.Box(value));
            }
        }

        static private void FocusChildOnOpenProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            DismissiblePopup obj = (DismissiblePopup) o;
            obj.OnFocusChildOnOpenChanged( new PropertyChangedEventArgs<bool>((bool)e.OldValue, (bool)e.NewValue) );
        }

        /// <summary>
        /// Occurs when FocusChildOnOpen property changes.
        /// </summary>
        public event EventHandler<PropertyChangedEventArgs<bool>> FocusChildOnOpenChanged;

        /// <summary>
        /// Called when FocusChildOnOpen property changes.
        /// </summary>
        protected virtual void OnFocusChildOnOpenChanged(PropertyChangedEventArgs<bool> e)
        {
            OnFocusChildOnOpenChangedImplementation(e);
            RaisePropertyChangedEvent(FocusChildOnOpenChanged, e);
        }

        partial void OnFocusChildOnOpenChangedImplementation(PropertyChangedEventArgs<bool> e);

        //
        // SetFocusOnClose dependency property
        //
        /// <summary>
        /// Identifies the SetFocusOnClose dependency property.
        /// </summary>
        public static readonly DependencyProperty SetFocusOnCloseProperty = DependencyProperty.Register( "SetFocusOnClose", typeof(bool), typeof(DismissiblePopup), new PropertyMetadata( BooleanBoxes.FalseBox, SetFocusOnCloseProperty_PropertyChanged) );

        /// <summary>
        /// Indicates whether the focus returns to either a defined by the FocusOnCloseTarget dependency property UIElement or PlacementTarget or not.
        /// </summary>
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Indicates whether the focus returns to either a defined by the FocusOnCloseTarget dependency property UIElement or PlacementTarget or not.")]
        [Localizability(LocalizationCategory.None)]
        public bool SetFocusOnClose
        {
            get
            {
                return (bool) GetValue(SetFocusOnCloseProperty);
            }
            set
            {
                SetValue(SetFocusOnCloseProperty,BooleanBoxes.Box(value));
            }
        }

        static private void SetFocusOnCloseProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            DismissiblePopup obj = (DismissiblePopup) o;
            obj.OnSetFocusOnCloseChanged( new PropertyChangedEventArgs<bool>((bool)e.OldValue, (bool)e.NewValue) );
        }

        /// <summary>
        /// Occurs when SetFocusOnClose property changes.
        /// </summary>
        public event EventHandler<PropertyChangedEventArgs<bool>> SetFocusOnCloseChanged;

        /// <summary>
        /// Called when SetFocusOnClose property changes.
        /// </summary>
        protected virtual void OnSetFocusOnCloseChanged(PropertyChangedEventArgs<bool> e)
        {
            OnSetFocusOnCloseChangedImplementation(e);
            RaisePropertyChangedEvent(SetFocusOnCloseChanged, e);
        }

        partial void OnSetFocusOnCloseChangedImplementation(PropertyChangedEventArgs<bool> e);

        //
        // SetFocusOnCloseElement dependency property
        //
        /// <summary>
        /// Identifies the SetFocusOnCloseElement dependency property.
        /// </summary>
        public static readonly DependencyProperty SetFocusOnCloseElementProperty = DependencyProperty.Register( "SetFocusOnCloseElement", typeof(UIElement), typeof(DismissiblePopup), new PropertyMetadata( null, SetFocusOnCloseElementProperty_PropertyChanged) );

        /// <summary>
        /// If the SetFocusOnClose property is set True and this property is set to a valid UIElement, focus returns to this UIElement after the DismissiblePopup is closed.
        /// </summary>
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("If the SetFocusOnClose property is set True and this property is set to a valid UIElement, focus returns to this UIElement after the DismissiblePopup is closed.")]
        [Localizability(LocalizationCategory.None)]
        public UIElement SetFocusOnCloseElement
        {
            get
            {
                return (UIElement) GetValue(SetFocusOnCloseElementProperty);
            }
            set
            {
                SetValue(SetFocusOnCloseElementProperty,value);
            }
        }

        static private void SetFocusOnCloseElementProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            DismissiblePopup obj = (DismissiblePopup) o;
            obj.OnSetFocusOnCloseElementChanged( new PropertyChangedEventArgs<UIElement>((UIElement)e.OldValue, (UIElement)e.NewValue) );
        }

        /// <summary>
        /// Occurs when SetFocusOnCloseElement property changes.
        /// </summary>
        public event EventHandler<PropertyChangedEventArgs<UIElement>> SetFocusOnCloseElementChanged;

        /// <summary>
        /// Called when SetFocusOnCloseElement property changes.
        /// </summary>
        protected virtual void OnSetFocusOnCloseElementChanged(PropertyChangedEventArgs<UIElement> e)
        {
            OnSetFocusOnCloseElementChangedImplementation(e);
            RaisePropertyChangedEvent(SetFocusOnCloseElementChanged, e);
        }

        partial void OnSetFocusOnCloseElementChangedImplementation(PropertyChangedEventArgs<UIElement> e);

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
        // Static constructor
        //

        /// <summary>
        /// Called when the type is initialized.
        /// </summary>
        static DismissiblePopup()
        {
            CommandManager.RegisterClassCommandBinding( typeof(DismissiblePopup), new CommandBinding( DismissiblePopup.DismissPopupCommand, DismissPopupCommand_CommandExecuted ));
            StaticConstructorImplementation();
        }

        static partial void StaticConstructorImplementation();

    }
}
#endregion
