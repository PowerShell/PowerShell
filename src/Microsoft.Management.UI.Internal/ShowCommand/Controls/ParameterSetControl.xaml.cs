// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;

using Microsoft.Management.UI.Internal;
using Microsoft.PowerShell.Commands.ShowCommandExtension;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    /// <summary>
    /// Interaction logic for ParameterSetControl.xaml.
    /// </summary>
    public partial class ParameterSetControl : UserControl
    {
        /// <summary>
        /// First focusable element in the generated UI.
        /// </summary>
        private UIElement firstFocusableElement;

        /// <summary>
        /// Field used for the CurrentParameterSetViewModel parameter.
        /// </summary>
        private ParameterSetViewModel currentParameterSetViewModel;

        #region Construction and Destructor
        /// <summary>
        /// Initializes a new instance of the ParameterSetControl class.
        /// </summary>
        public ParameterSetControl()
        {
            InitializeComponent();
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(this.ParameterSetControl_DataContextChanged);
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Focuses the first focusable element in this control.
        /// </summary>
        public void FocusFirstElement()
        {
            if (this.firstFocusableElement != null)
            {
                this.firstFocusableElement.Focus();
            }
        }

        #endregion

        #region Private Property
        /// <summary>
        /// Gets current ParameterSetViewModel.
        /// </summary>
        private ParameterSetViewModel CurrentParameterSetViewModel
        {
            get { return this.currentParameterSetViewModel; }
        }

        #endregion

        /// <summary>
        /// Creates a CheckBox for switch parameters.
        /// </summary>
        /// <param name="parameterViewModel">DataContext object.</param>
        /// <param name="rowNumber">Row number.</param>
        /// <returns>a CheckBox for switch parameters.</returns>
        private static CheckBox CreateCheckBox(ParameterViewModel parameterViewModel, int rowNumber)
        {
            CheckBox checkBox = new CheckBox();

            checkBox.SetBinding(Label.ContentProperty, new Binding("NameCheckLabel"));
            checkBox.DataContext = parameterViewModel;
            checkBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            checkBox.SetValue(Grid.ColumnProperty, 0);
            checkBox.SetValue(Grid.ColumnSpanProperty, 2);
            checkBox.SetValue(Grid.RowProperty, rowNumber);
            checkBox.IsThreeState = false;
            checkBox.Margin = new Thickness(8, rowNumber == 0 ? 7 : 5, 0, 5);
            checkBox.SetBinding(CheckBox.ToolTipProperty, new Binding("ToolTip"));
            checkBox.SetBinding(AutomationProperties.HelpTextProperty, new Binding("ToolTip"));
            Binding valueBinding = new Binding("Value");
            checkBox.SetBinding(CheckBox.IsCheckedProperty, valueBinding);

            //// Add AutomationProperties.AutomationId for Ui Automation test.
            checkBox.SetValue(
                System.Windows.Automation.AutomationProperties.AutomationIdProperty,
                string.Create(CultureInfo.CurrentCulture, $"chk{parameterViewModel.Name}"));

            checkBox.SetValue(
                System.Windows.Automation.AutomationProperties.NameProperty,
                parameterViewModel.Name);

            return checkBox;
        }

        /// <summary>
        /// Creates a ComboBox control for input type field.
        /// </summary>
        /// <param name="parameterViewModel">DataContext object.</param>
        /// <param name="rowNumber">Row number.</param>
        /// <param name="itemsSource">Control data source.</param>
        /// <returns>Return a ComboBox control.</returns>
        private static ComboBox CreateComboBoxControl(ParameterViewModel parameterViewModel, int rowNumber, IEnumerable itemsSource)
        {
            ComboBox comboBox = new ComboBox();

            comboBox.DataContext = parameterViewModel;
            comboBox.SetValue(Grid.ColumnProperty, 1);
            comboBox.SetValue(Grid.RowProperty, rowNumber);
            comboBox.Margin = new Thickness(2);
            comboBox.SetBinding(TextBox.ToolTipProperty, new Binding("ToolTip"));
            comboBox.ItemsSource = itemsSource;

            Binding selectedItemBinding = new Binding("Value");
            comboBox.SetBinding(ComboBox.SelectedItemProperty, selectedItemBinding);

            string automationId = string.Format(
                    CultureInfo.CurrentCulture,
                    "combox{0}",
                    parameterViewModel.Name);

            //// Add AutomationProperties.AutomationId for Ui Automation test.
            comboBox.SetValue(
                System.Windows.Automation.AutomationProperties.AutomationIdProperty,
                automationId);

            comboBox.SetValue(
                System.Windows.Automation.AutomationProperties.NameProperty,
                parameterViewModel.Name);

            return comboBox;
        }

        /// <summary>
        /// Creates a MultiSelectCombo control for input type field.
        /// </summary>
        /// <param name="parameterViewModel">DataContext object.</param>
        /// <param name="rowNumber">Row number.</param>
        /// <param name="itemsSource">Control data source.</param>
        /// <returns>Return a MultiSelectCombo control.</returns>
        private static MultipleSelectionControl CreateMultiSelectComboControl(ParameterViewModel parameterViewModel, int rowNumber, IEnumerable itemsSource)
        {
            MultipleSelectionControl multiControls = new MultipleSelectionControl();

            multiControls.DataContext = parameterViewModel;
            multiControls.SetValue(Grid.ColumnProperty, 1);
            multiControls.SetValue(Grid.RowProperty, rowNumber);
            multiControls.Margin = new Thickness(2);
            multiControls.comboxParameter.ItemsSource = itemsSource;
            multiControls.SetBinding(TextBox.ToolTipProperty, new Binding("ToolTip"));

            Binding valueBinding = new Binding("Value");
            valueBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            multiControls.comboxParameter.SetBinding(ComboBox.TextProperty, valueBinding);

            // Add AutomationProperties.AutomationId for Ui Automation test.
            multiControls.SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty, string.Create($"combox{parameterViewModel.Name}"));

            multiControls.comboxParameter.SetValue(
                System.Windows.Automation.AutomationProperties.NameProperty,
                parameterViewModel.Name);

            string buttonToolTipAndName = string.Format(
                CultureInfo.CurrentUICulture,
                ShowCommandResources.SelectMultipleValuesForParameterFormat,
                parameterViewModel.Name);

            multiControls.multipleValueButton.SetValue(Button.ToolTipProperty, buttonToolTipAndName);
            multiControls.multipleValueButton.SetValue(
                System.Windows.Automation.AutomationProperties.NameProperty,
                buttonToolTipAndName);

            return multiControls;
        }

        /// <summary>
        /// Creates a TextBox control for input type field.
        /// </summary>
        /// <param name="parameterViewModel">DataContext object.</param>
        /// <param name="rowNumber">Row number.</param>
        /// <returns>Return a TextBox control.</returns>
        private static TextBox CreateTextBoxControl(ParameterViewModel parameterViewModel, int rowNumber)
        {
            TextBox textBox = new TextBox();

            textBox.DataContext = parameterViewModel;
            textBox.SetValue(Grid.ColumnProperty, 1);
            textBox.SetValue(Grid.RowProperty, rowNumber);
            textBox.Margin = new Thickness(2);
            textBox.SetBinding(TextBox.ToolTipProperty, new Binding("ToolTip"));

            Binding valueBinding = new Binding("Value");
            valueBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            textBox.SetBinding(TextBox.TextProperty, valueBinding);

            //// Add AutomationProperties.AutomationId for UI Automation test.
            textBox.SetValue(
                System.Windows.Automation.AutomationProperties.AutomationIdProperty,
                string.Create(CultureInfo.CurrentCulture, $"txt{parameterViewModel.Name}"));

            textBox.SetValue(
                System.Windows.Automation.AutomationProperties.NameProperty,
                parameterViewModel.Name);

            ShowCommandParameterType parameterType = parameterViewModel.Parameter.ParameterType;

            if (parameterType.IsArray)
            {
                parameterType = parameterType.ElementType;
            }

            if (parameterType.IsScriptBlock || parameterType.ImplementsDictionary)
            {
                textBox.AcceptsReturn = true;
                textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                textBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                textBox.Loaded += ParameterSetControl.MultiLineTextBox_Loaded;
            }

            return textBox;
        }

        /// <summary>
        /// Called for a newly created multiline text box to increase its height and.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private static void MultiLineTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            TextBox senderTextBox = (TextBox)sender;
            senderTextBox.Loaded -= ParameterSetControl.MultiLineTextBox_Loaded;

            // This will set the height to about 3 lines since the total height of the
            // TextBox is a bit greater than a line's height
            senderTextBox.Height = senderTextBox.ActualHeight * 2;
        }

        #region Event Methods

        /// <summary>
        /// When user switch ParameterSet.It will trigger this event.
        /// This event method will renew generate all controls for current ParameterSet.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void ParameterSetControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.firstFocusableElement = null;
            this.MainGrid.Children.Clear();
            this.MainGrid.RowDefinitions.Clear();

            ParameterSetViewModel viewModel = e.NewValue as ParameterSetViewModel;
            if (viewModel == null)
            {
                return;
            }

            this.currentParameterSetViewModel = viewModel;

            for (int rowNumber = 0; rowNumber < viewModel.Parameters.Count; rowNumber++)
            {
                ParameterViewModel parameter = viewModel.Parameters[rowNumber];
                this.MainGrid.RowDefinitions.Add(this.CreateNewRow());

                if (parameter.Parameter.ParameterType.IsSwitch)
                {
                    this.AddControlToMainGrid(ParameterSetControl.CreateCheckBox(parameter, rowNumber));
                }
                else
                {
                    this.CreateAndAddLabel(parameter, rowNumber);
                    Control control = null;
                    if (parameter.Parameter.HasParameterSet)
                    {
                        // For ValidateSet parameter
                        ArrayList itemsSource = new ArrayList();
                        itemsSource.Add(string.Empty);

                        for (int i = 0; i < parameter.Parameter.ValidParamSetValues.Count; i++)
                        {
                            itemsSource.Add(parameter.Parameter.ValidParamSetValues[i]);
                        }

                        control = ParameterSetControl.CreateComboBoxControl(parameter, rowNumber, itemsSource);
                    }
                    else if (parameter.Parameter.ParameterType.IsEnum)
                    {
                        if (parameter.Parameter.ParameterType.HasFlagAttribute)
                        {
                            ArrayList itemsSource = new ArrayList();
                            itemsSource.Add(string.Empty);
                            itemsSource.AddRange(parameter.Parameter.ParameterType.EnumValues);
                            control = ParameterSetControl.CreateComboBoxControl(parameter, rowNumber, itemsSource);
                        }
                        else
                        {
                            control = ParameterSetControl.CreateMultiSelectComboControl(parameter, rowNumber, parameter.Parameter.ParameterType.EnumValues);
                        }
                    }
                    else if (parameter.Parameter.ParameterType.IsBoolean)
                    {
                        control = ParameterSetControl.CreateComboBoxControl(parameter, rowNumber, new string[] { string.Empty, "$True", "$False" });
                    }
                    else
                    {
                        // For input parameter
                        control = ParameterSetControl.CreateTextBoxControl(parameter, rowNumber);
                    }

                    if (control != null)
                    {
                        this.AddControlToMainGrid(control);
                    }
                }
            }
        }

        /// <summary>
        /// When user trigger click on anyone CheckBox. Get value from sender.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox senderCheck = (CheckBox)sender;
            ((ParameterViewModel)senderCheck.DataContext).Value = senderCheck.IsChecked.ToString();
        }

        #endregion

        #region Private Method

        /// <summary>
        /// Creates a RowDefinition for MainGrid.
        /// </summary>
        /// <returns>Return a RowDefinition object.</returns>
        private RowDefinition CreateNewRow()
        {
            RowDefinition row = new RowDefinition();
            row.Height = GridLength.Auto;
            return row;
        }

        /// <summary>
        /// Adds a control to MainGrid;.
        /// </summary>
        /// <param name="uiControl">Will adding UIControl.</param>
        private void AddControlToMainGrid(UIElement uiControl)
        {
            if (this.firstFocusableElement == null && uiControl is not Label)
            {
                this.firstFocusableElement = uiControl;
            }

            this.MainGrid.Children.Add(uiControl);
        }

        /// <summary>
        /// Creates a Lable control and add it to MainGrid.
        /// </summary>
        /// <param name="parameterViewModel">DataContext object.</param>
        /// <param name="rowNumber">Row number.</param>
        private void CreateAndAddLabel(ParameterViewModel parameterViewModel, int rowNumber)
        {
            Label label = this.CreateLabel(parameterViewModel, rowNumber);
            this.AddControlToMainGrid(label);
        }

        /// <summary>
        /// Creates a Label control for input type field.
        /// </summary>
        /// <param name="parameterViewModel">DataContext object.</param>
        /// <param name="rowNumber">Row number.</param>
        /// <returns>Return a Label control.</returns>
        private Label CreateLabel(ParameterViewModel parameterViewModel, int rowNumber)
        {
            Label label = new Label();

            label.SetBinding(Label.ContentProperty, new Binding("NameTextLabel"));
            label.DataContext = parameterViewModel;
            label.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            label.SetValue(Grid.ColumnProperty, 0);
            label.SetValue(Grid.RowProperty, rowNumber);
            label.Margin = new Thickness(2);
            label.SetBinding(Label.ToolTipProperty, new Binding("ToolTip"));

            //// Add AutomationProperties.AutomationId for Ui Automation test.
            label.SetValue(
                System.Windows.Automation.AutomationProperties.AutomationIdProperty,
                string.Create(CultureInfo.CurrentCulture, $"lbl{parameterViewModel.Name}"));

            return label;
        }
        #endregion
    }
}
