// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region StyleCop Suppression - generated code
using System;
using System.ComponentModel;
using System.Windows;

namespace Microsoft.Management.UI.Internal
{

    /// <summary>
    /// A toggle button which controls is a popup is open or not.
    /// </summary>
    [Localizability(LocalizationCategory.None)]
    partial class PopupControlButton
    {
        //
        // IsPopupOpen dependency property
        //
        /// <summary>
        /// Identifies the IsPopupOpen dependency property.
        /// </summary>
        public static readonly DependencyProperty IsPopupOpenProperty = DependencyProperty.Register( "IsPopupOpen", typeof(bool), typeof(PopupControlButton), new FrameworkPropertyMetadata( BooleanBoxes.FalseBox, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, IsPopupOpenProperty_PropertyChanged) );

        /// <summary>
        /// Gets or sets a value indicating whether the popup is open or not.
        /// </summary>
        /// <remarks>
        /// The Popup.IsOpen property should be two-way bound to this property.
        /// </remarks>
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets a value indicating whether the popup is open or not.")]
        [Localizability(LocalizationCategory.None)]
        public bool IsPopupOpen
        {
            get
            {
                return (bool) GetValue(IsPopupOpenProperty);
            }
            set
            {
                SetValue(IsPopupOpenProperty,BooleanBoxes.Box(value));
            }
        }

        static private void IsPopupOpenProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            PopupControlButton obj = (PopupControlButton) o;
            obj.OnIsPopupOpenChanged( new PropertyChangedEventArgs<bool>((bool)e.OldValue, (bool)e.NewValue) );
        }

        /// <summary>
        /// Occurs when IsPopupOpen property changes.
        /// </summary>
        public event EventHandler<PropertyChangedEventArgs<bool>> IsPopupOpenChanged;

        /// <summary>
        /// Called when IsPopupOpen property changes.
        /// </summary>
        protected virtual void OnIsPopupOpenChanged(PropertyChangedEventArgs<bool> e)
        {
            OnIsPopupOpenChangedImplementation(e);
            RaisePropertyChangedEvent(IsPopupOpenChanged, e);
        }

        partial void OnIsPopupOpenChangedImplementation(PropertyChangedEventArgs<bool> e);

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

    }
}
#endregion
