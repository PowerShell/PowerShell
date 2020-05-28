// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region StyleCop Suppression - generated code
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace Microsoft.Management.UI.Internal
{
    [Localizability(LocalizationCategory.None)]
    partial class AddFilterRulePicker
    {
        //
        // CancelAddFilterRules routed command
        //
        /// <summary>
        /// Closes the picker and unchecks all items in the panel.
        /// </summary>
        public static readonly RoutedCommand CancelAddFilterRulesCommand = new RoutedCommand("CancelAddFilterRules",typeof(AddFilterRulePicker));

        static private void CancelAddFilterRulesCommand_CommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            AddFilterRulePicker obj = (AddFilterRulePicker) sender;
            obj.OnCancelAddFilterRulesExecuted( e );
        }

        /// <summary>
        /// Called when CancelAddFilterRules executes.
        /// </summary>
        /// <remarks>
        /// Closes the picker and unchecks all items in the panel.
        /// </remarks>
        protected virtual void OnCancelAddFilterRulesExecuted(ExecutedRoutedEventArgs e)
        {
            OnCancelAddFilterRulesExecutedImplementation(e);
        }

        partial void OnCancelAddFilterRulesExecutedImplementation(ExecutedRoutedEventArgs e);

        //
        // OkAddFilterRules routed command
        //
        /// <summary>
        /// Closes the picker and calls AddFilterRulesCommand with the collection of checked items from the picker.
        /// </summary>
        public static readonly RoutedCommand OkAddFilterRulesCommand = new RoutedCommand("OkAddFilterRules",typeof(AddFilterRulePicker));

        static private void OkAddFilterRulesCommand_CommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            AddFilterRulePicker obj = (AddFilterRulePicker) sender;
            obj.OnOkAddFilterRulesCanExecute( e );
        }

        static private void OkAddFilterRulesCommand_CommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            AddFilterRulePicker obj = (AddFilterRulePicker) sender;
            obj.OnOkAddFilterRulesExecuted( e );
        }

        /// <summary>
        /// Called to determine if OkAddFilterRules can execute.
        /// </summary>
        protected virtual void OnOkAddFilterRulesCanExecute(CanExecuteRoutedEventArgs e)
        {
            OnOkAddFilterRulesCanExecuteImplementation(e);
        }

        partial void OnOkAddFilterRulesCanExecuteImplementation(CanExecuteRoutedEventArgs e);

        /// <summary>
        /// Called when OkAddFilterRules executes.
        /// </summary>
        /// <remarks>
        /// Closes the picker and calls AddFilterRulesCommand with the collection of checked items from the picker.
        /// </remarks>
        protected virtual void OnOkAddFilterRulesExecuted(ExecutedRoutedEventArgs e)
        {
            OnOkAddFilterRulesExecutedImplementation(e);
        }

        partial void OnOkAddFilterRulesExecutedImplementation(ExecutedRoutedEventArgs e);

        //
        // AddFilterRulesCommand dependency property
        //
        /// <summary>
        /// Identifies the AddFilterRulesCommand dependency property.
        /// </summary>
        public static readonly DependencyProperty AddFilterRulesCommandProperty = DependencyProperty.Register( "AddFilterRulesCommand", typeof(ICommand), typeof(AddFilterRulePicker), new PropertyMetadata( null, AddFilterRulesCommandProperty_PropertyChanged) );

        /// <summary>
        /// Gets or sets the command used to communicate that the action has occurred.
        /// </summary>
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets the command used to communicate that the action has occurred.")]
        [Localizability(LocalizationCategory.None)]
        public ICommand AddFilterRulesCommand
        {
            get
            {
                return (ICommand) GetValue(AddFilterRulesCommandProperty);
            }
            set
            {
                SetValue(AddFilterRulesCommandProperty,value);
            }
        }

        static private void AddFilterRulesCommandProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            AddFilterRulePicker obj = (AddFilterRulePicker) o;
            obj.OnAddFilterRulesCommandChanged( new PropertyChangedEventArgs<ICommand>((ICommand)e.OldValue, (ICommand)e.NewValue) );
        }

        /// <summary>
        /// Occurs when AddFilterRulesCommand property changes.
        /// </summary>
        public event EventHandler<PropertyChangedEventArgs<ICommand>> AddFilterRulesCommandChanged;

        /// <summary>
        /// Called when AddFilterRulesCommand property changes.
        /// </summary>
        protected virtual void OnAddFilterRulesCommandChanged(PropertyChangedEventArgs<ICommand> e)
        {
            OnAddFilterRulesCommandChangedImplementation(e);
            RaisePropertyChangedEvent(AddFilterRulesCommandChanged, e);
        }

        partial void OnAddFilterRulesCommandChangedImplementation(PropertyChangedEventArgs<ICommand> e);

        //
        // AddFilterRulesCommandTarget dependency property
        //
        /// <summary>
        /// Identifies the AddFilterRulesCommandTarget dependency property.
        /// </summary>
        public static readonly DependencyProperty AddFilterRulesCommandTargetProperty = DependencyProperty.Register( "AddFilterRulesCommandTarget", typeof(IInputElement), typeof(AddFilterRulePicker), new PropertyMetadata( null, AddFilterRulesCommandTargetProperty_PropertyChanged) );

        /// <summary>
        /// Gets or sets a target of the Command.
        /// </summary>
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets a target of the Command.")]
        [Localizability(LocalizationCategory.None)]
        public IInputElement AddFilterRulesCommandTarget
        {
            get
            {
                return (IInputElement) GetValue(AddFilterRulesCommandTargetProperty);
            }
            set
            {
                SetValue(AddFilterRulesCommandTargetProperty,value);
            }
        }

        static private void AddFilterRulesCommandTargetProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            AddFilterRulePicker obj = (AddFilterRulePicker) o;
            obj.OnAddFilterRulesCommandTargetChanged( new PropertyChangedEventArgs<IInputElement>((IInputElement)e.OldValue, (IInputElement)e.NewValue) );
        }

        /// <summary>
        /// Occurs when AddFilterRulesCommandTarget property changes.
        /// </summary>
        public event EventHandler<PropertyChangedEventArgs<IInputElement>> AddFilterRulesCommandTargetChanged;

        /// <summary>
        /// Called when AddFilterRulesCommandTarget property changes.
        /// </summary>
        protected virtual void OnAddFilterRulesCommandTargetChanged(PropertyChangedEventArgs<IInputElement> e)
        {
            OnAddFilterRulesCommandTargetChangedImplementation(e);
            RaisePropertyChangedEvent(AddFilterRulesCommandTargetChanged, e);
        }

        partial void OnAddFilterRulesCommandTargetChangedImplementation(PropertyChangedEventArgs<IInputElement> e);

        //
        // IsOpen dependency property
        //
        /// <summary>
        /// Identifies the IsOpen dependency property.
        /// </summary>
        public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register( "IsOpen", typeof(bool), typeof(AddFilterRulePicker), new PropertyMetadata( BooleanBoxes.FalseBox, IsOpenProperty_PropertyChanged) );

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
            AddFilterRulePicker obj = (AddFilterRulePicker) o;
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
        // Static constructor
        //

        /// <summary>
        /// Called when the type is initialized.
        /// </summary>
        static AddFilterRulePicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AddFilterRulePicker), new FrameworkPropertyMetadata(typeof(AddFilterRulePicker)));
            CommandManager.RegisterClassCommandBinding( typeof(AddFilterRulePicker), new CommandBinding( AddFilterRulePicker.CancelAddFilterRulesCommand, CancelAddFilterRulesCommand_CommandExecuted ));
            CommandManager.RegisterClassCommandBinding( typeof(AddFilterRulePicker), new CommandBinding( AddFilterRulePicker.OkAddFilterRulesCommand, OkAddFilterRulesCommand_CommandExecuted, OkAddFilterRulesCommand_CommandCanExecute ));
            StaticConstructorImplementation();
        }

        static partial void StaticConstructorImplementation();

    }
}
#endregion
