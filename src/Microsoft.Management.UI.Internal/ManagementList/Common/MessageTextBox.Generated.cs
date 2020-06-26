// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region StyleCop Suppression - generated code
using System;
using System.ComponentModel;
using System.Windows;

namespace Microsoft.Management.UI.Internal
{

    /// <summary>
    /// A TextBox which shows a user provided text when its empty.
    /// </summary>
    [Localizability(LocalizationCategory.None)]
    partial class MessageTextBox
    {
        //
        // BackgroundText dependency property
        //
        /// <summary>
        /// Identifies the BackgroundText dependency property.
        /// </summary>
        public static readonly DependencyProperty BackgroundTextProperty = DependencyProperty.Register( "BackgroundText", typeof(string), typeof(MessageTextBox), new PropertyMetadata( string.Empty, BackgroundTextProperty_PropertyChanged) );

        /// <summary>
        /// Gets or sets a value for text presented to user when TextBox is empty.
        /// </summary>
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets a value for text presented to user when TextBox is empty.")]
        [Localizability(LocalizationCategory.Text, Modifiability=Modifiability.Modifiable, Readability=Readability.Readable)]
        public string BackgroundText
        {
            get
            {
                return (string) GetValue(BackgroundTextProperty);
            }
            set
            {
                SetValue(BackgroundTextProperty,value);
            }
        }

        static private void BackgroundTextProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            MessageTextBox obj = (MessageTextBox) o;
            obj.OnBackgroundTextChanged( new PropertyChangedEventArgs<string>((string)e.OldValue, (string)e.NewValue) );
        }

        /// <summary>
        /// Occurs when BackgroundText property changes.
        /// </summary>
        public event EventHandler<PropertyChangedEventArgs<string>> BackgroundTextChanged;

        /// <summary>
        /// Called when BackgroundText property changes.
        /// </summary>
        protected virtual void OnBackgroundTextChanged(PropertyChangedEventArgs<string> e)
        {
            OnBackgroundTextChangedImplementation(e);
            RaisePropertyChangedEvent(BackgroundTextChanged, e);
        }

        partial void OnBackgroundTextChangedImplementation(PropertyChangedEventArgs<string> e);

        //
        // IsBackgroundTextShown dependency property
        //
        /// <summary>
        /// Identifies the IsBackgroundTextShown dependency property key.
        /// </summary>
        private static readonly DependencyPropertyKey IsBackgroundTextShownPropertyKey = DependencyProperty.RegisterReadOnly( "IsBackgroundTextShown", typeof(bool), typeof(MessageTextBox), new PropertyMetadata( BooleanBoxes.TrueBox, IsBackgroundTextShownProperty_PropertyChanged) );
        /// <summary>
        /// Identifies the IsBackgroundTextShown dependency property.
        /// </summary>
        public static readonly DependencyProperty IsBackgroundTextShownProperty = IsBackgroundTextShownPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets a value indicating if the background text is being shown.
        /// </summary>
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets a value indicating if the background text is being shown.")]
        [Localizability(LocalizationCategory.None)]
        public bool IsBackgroundTextShown
        {
            get
            {
                return (bool) GetValue(IsBackgroundTextShownProperty);
            }
            private set
            {
                SetValue(IsBackgroundTextShownPropertyKey,BooleanBoxes.Box(value));
            }
        }

        static private void IsBackgroundTextShownProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            MessageTextBox obj = (MessageTextBox) o;
            obj.OnIsBackgroundTextShownChanged( new PropertyChangedEventArgs<bool>((bool)e.OldValue, (bool)e.NewValue) );
        }

        /// <summary>
        /// Occurs when IsBackgroundTextShown property changes.
        /// </summary>
        public event EventHandler<PropertyChangedEventArgs<bool>> IsBackgroundTextShownChanged;

        /// <summary>
        /// Called when IsBackgroundTextShown property changes.
        /// </summary>
        protected virtual void OnIsBackgroundTextShownChanged(PropertyChangedEventArgs<bool> e)
        {
            OnIsBackgroundTextShownChangedImplementation(e);
            RaisePropertyChangedEvent(IsBackgroundTextShownChanged, e);
        }

        partial void OnIsBackgroundTextShownChangedImplementation(PropertyChangedEventArgs<bool> e);

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
        static MessageTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MessageTextBox), new FrameworkPropertyMetadata(typeof(MessageTextBox)));
            StaticConstructorImplementation();
        }

        static partial void StaticConstructorImplementation();

    }
}
#endregion
