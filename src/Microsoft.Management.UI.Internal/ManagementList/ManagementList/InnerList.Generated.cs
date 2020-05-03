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
    /// List control for Inner Applications.  This Control supports grouping, sorting, filtering and GUI Virtualization through DataBinding.
    /// </summary>
    [Localizability(LocalizationCategory.None)]
    partial class InnerList
    {
        //
        // Copy routed command
        //
        static private void CopyCommand_CommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            InnerList obj = (InnerList) sender;
            obj.OnCopyCanExecute( e );
        }

        static private void CopyCommand_CommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            InnerList obj = (InnerList) sender;
            obj.OnCopyExecuted( e );
        }

        /// <summary>
        /// Called to determine if Copy can execute.
        /// </summary>
        protected virtual void OnCopyCanExecute(CanExecuteRoutedEventArgs e)
        {
            OnCopyCanExecuteImplementation(e);
        }

        partial void OnCopyCanExecuteImplementation(CanExecuteRoutedEventArgs e);

        /// <summary>
        /// Called when Copy executes.
        /// </summary>
        /// <remarks>
        /// When executed, the currently selected items are copied to the clipboard.
        /// </remarks>
        protected virtual void OnCopyExecuted(ExecutedRoutedEventArgs e)
        {
            OnCopyExecutedImplementation(e);
        }

        partial void OnCopyExecutedImplementation(ExecutedRoutedEventArgs e);

        //
        // AutoGenerateColumns dependency property
        //
        /// <summary>
        /// Identifies the AutoGenerateColumns dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoGenerateColumnsProperty = DependencyProperty.Register( "AutoGenerateColumns", typeof(bool), typeof(InnerList), new PropertyMetadata( BooleanBoxes.FalseBox, AutoGenerateColumnsProperty_PropertyChanged) );

        /// <summary>
        /// Gets or sets a value indicating whether this list&apos;s columns should be automatically generated based on its data.
        /// </summary>
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets a value indicating whether this list's columns should be automatically generated based on its data.")]
        [Localizability(LocalizationCategory.None)]
        public bool AutoGenerateColumns
        {
            get
            {
                return (bool) GetValue(AutoGenerateColumnsProperty);
            }
            set
            {
                SetValue(AutoGenerateColumnsProperty,BooleanBoxes.Box(value));
            }
        }

        static private void AutoGenerateColumnsProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            InnerList obj = (InnerList) o;
            obj.OnAutoGenerateColumnsChanged( new PropertyChangedEventArgs<bool>((bool)e.OldValue, (bool)e.NewValue) );
        }

        /// <summary>
        /// Occurs when AutoGenerateColumns property changes.
        /// </summary>
        public event EventHandler<PropertyChangedEventArgs<bool>> AutoGenerateColumnsChanged;

        /// <summary>
        /// Called when AutoGenerateColumns property changes.
        /// </summary>
        protected virtual void OnAutoGenerateColumnsChanged(PropertyChangedEventArgs<bool> e)
        {
            OnAutoGenerateColumnsChangedImplementation(e);
            RaisePropertyChangedEvent(AutoGenerateColumnsChanged, e);
        }

        partial void OnAutoGenerateColumnsChangedImplementation(PropertyChangedEventArgs<bool> e);

        //
        // IsGroupsExpanded dependency property
        //
        /// <summary>
        /// Identifies the IsGroupsExpanded dependency property.
        /// </summary>
        public static readonly DependencyProperty IsGroupsExpandedProperty = DependencyProperty.Register( "IsGroupsExpanded", typeof(bool), typeof(InnerList), new PropertyMetadata( BooleanBoxes.FalseBox, IsGroupsExpandedProperty_PropertyChanged) );

        /// <summary>
        /// Gets or sets a value indicating whether is groups expanded or not.
        /// </summary>
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets a value indicating whether is groups expanded or not.")]
        [Localizability(LocalizationCategory.None)]
        public bool IsGroupsExpanded
        {
            get
            {
                return (bool) GetValue(IsGroupsExpandedProperty);
            }
            set
            {
                SetValue(IsGroupsExpandedProperty,BooleanBoxes.Box(value));
            }
        }

        static private void IsGroupsExpandedProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            InnerList obj = (InnerList) o;
            obj.OnIsGroupsExpandedChanged( new PropertyChangedEventArgs<bool>((bool)e.OldValue, (bool)e.NewValue) );
        }

        /// <summary>
        /// Occurs when IsGroupsExpanded property changes.
        /// </summary>
        public event EventHandler<PropertyChangedEventArgs<bool>> IsGroupsExpandedChanged;

        /// <summary>
        /// Called when IsGroupsExpanded property changes.
        /// </summary>
        protected virtual void OnIsGroupsExpandedChanged(PropertyChangedEventArgs<bool> e)
        {
            OnIsGroupsExpandedChangedImplementation(e);
            RaisePropertyChangedEvent(IsGroupsExpandedChanged, e);
        }

        partial void OnIsGroupsExpandedChangedImplementation(PropertyChangedEventArgs<bool> e);

        //
        // IsPrimarySortColumn dependency property
        //
        /// <summary>
        /// Identifies the IsPrimarySortColumn dependency property key.
        /// </summary>
        private static readonly DependencyPropertyKey IsPrimarySortColumnPropertyKey = DependencyProperty.RegisterAttachedReadOnly( "IsPrimarySortColumn", typeof(bool), typeof(InnerList), new PropertyMetadata( BooleanBoxes.FalseBox, IsPrimarySortColumnProperty_PropertyChanged) );
        /// <summary>
        /// Identifies the IsPrimarySortColumn dependency property.
        /// </summary>
        public static readonly DependencyProperty IsPrimarySortColumnProperty = IsPrimarySortColumnPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets whether a column is the primary sort in a list.
        /// </summary>
        /// <param name="element">The dependency object that the property is attached to.</param>
        /// <returns>
        /// The value of IsPrimarySortColumn that is attached to element.
        /// </returns>
        static public bool GetIsPrimarySortColumn(DependencyObject element)
        {
            return (bool) element.GetValue(IsPrimarySortColumnProperty);
        }

        /// <summary>
        /// Sets whether a column is the primary sort in a list.
        /// </summary>
        /// <param name="element">The dependency object that the property will be attached to.</param>
        /// <param name="value">The new value.</param>
        static private void SetIsPrimarySortColumn(DependencyObject element, bool value)
        {
            element.SetValue(IsPrimarySortColumnPropertyKey,BooleanBoxes.Box(value));
        }

        static private void IsPrimarySortColumnProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            IsPrimarySortColumnProperty_PropertyChangedImplementation(o, e);
        }

        static partial void IsPrimarySortColumnProperty_PropertyChangedImplementation(DependencyObject o, DependencyPropertyChangedEventArgs e);

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
        static InnerList()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(InnerList), new FrameworkPropertyMetadata(typeof(InnerList)));
            CommandManager.RegisterClassCommandBinding( typeof(InnerList), new CommandBinding( ApplicationCommands.Copy, CopyCommand_CommandExecuted, CopyCommand_CommandCanExecute ));
            StaticConstructorImplementation();
        }

        static partial void StaticConstructorImplementation();

    }
}
#endregion
