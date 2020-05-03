// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region StyleCop Suppression - generated code
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Microsoft.Management.UI.Internal
{

    /// <summary>
    /// Represents a control that parses search text to return a filter expression.
    /// </summary>
    [Localizability(LocalizationCategory.None)]
    partial class SearchBox
    {
        //
        // ClearText routed command
        //
        /// <summary>
        /// Clears the search text.
        /// </summary>
        public static readonly RoutedCommand ClearTextCommand = new RoutedCommand("ClearText",typeof(SearchBox));

        static private void ClearTextCommand_CommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            SearchBox obj = (SearchBox) sender;
            obj.OnClearTextCanExecute( e );
        }

        static private void ClearTextCommand_CommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            SearchBox obj = (SearchBox) sender;
            obj.OnClearTextExecuted( e );
        }

        /// <summary>
        /// Called to determine if ClearText can execute.
        /// </summary>
        protected virtual void OnClearTextCanExecute(CanExecuteRoutedEventArgs e)
        {
            OnClearTextCanExecuteImplementation(e);
        }

        partial void OnClearTextCanExecuteImplementation(CanExecuteRoutedEventArgs e);

        /// <summary>
        /// Called when ClearText executes.
        /// </summary>
        /// <remarks>
        /// Clears the search text.
        /// </remarks>
        protected virtual void OnClearTextExecuted(ExecutedRoutedEventArgs e)
        {
            OnClearTextExecutedImplementation(e);
        }

        partial void OnClearTextExecutedImplementation(ExecutedRoutedEventArgs e);

        //
        // BackgroundText dependency property
        //
        /// <summary>
        /// Identifies the BackgroundText dependency property.
        /// </summary>
        public static readonly DependencyProperty BackgroundTextProperty = DependencyProperty.Register( "BackgroundText", typeof(string), typeof(SearchBox), new PropertyMetadata( UICultureResources.SearchBox_BackgroundText, BackgroundTextProperty_PropertyChanged) );

        /// <summary>
        /// Gets or sets the background text of the search box.
        /// </summary>
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets the background text of the search box.")]
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
            SearchBox obj = (SearchBox) o;
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
        // Text dependency property
        //
        /// <summary>
        /// Identifies the Text dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register( "Text", typeof(string), typeof(SearchBox), new PropertyMetadata( string.Empty, TextProperty_PropertyChanged) );

        /// <summary>
        /// Gets or sets the text contents of the search box.
        /// </summary>
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets the text contents of the search box.")]
        [Localizability(LocalizationCategory.Text, Modifiability=Modifiability.Modifiable, Readability=Readability.Readable)]
        public string Text
        {
            get
            {
                return (string) GetValue(TextProperty);
            }
            set
            {
                SetValue(TextProperty,value);
            }
        }

        static private void TextProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            SearchBox obj = (SearchBox) o;
            obj.OnTextChanged( new PropertyChangedEventArgs<string>((string)e.OldValue, (string)e.NewValue) );
        }

        /// <summary>
        /// Occurs when Text property changes.
        /// </summary>
        public event EventHandler<PropertyChangedEventArgs<string>> TextChanged;

        /// <summary>
        /// Called when Text property changes.
        /// </summary>
        protected virtual void OnTextChanged(PropertyChangedEventArgs<string> e)
        {
            OnTextChangedImplementation(e);
            RaisePropertyChangedEvent(TextChanged, e);
        }

        partial void OnTextChangedImplementation(PropertyChangedEventArgs<string> e);

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
        static SearchBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SearchBox), new FrameworkPropertyMetadata(typeof(SearchBox)));
            CommandManager.RegisterClassCommandBinding( typeof(SearchBox), new CommandBinding( SearchBox.ClearTextCommand, ClearTextCommand_CommandExecuted, ClearTextCommand_CommandCanExecute ));
            StaticConstructorImplementation();
        }

        static partial void StaticConstructorImplementation();

    }
}
#endregion
