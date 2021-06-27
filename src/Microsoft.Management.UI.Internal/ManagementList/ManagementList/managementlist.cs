// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Microsoft.Management.UI.Internal
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public partial class ManagementList : Control
    {
        #region Properties

        private ReadOnlyCollection<object> logicalChildren;

        private PropertiesTextContainsFilterRule defaultFullTextSearchRule = new PropertiesTextContainsFilterRule();

        private ObservableCollection<StateDescriptor<ManagementList>> views = new ObservableCollection<StateDescriptor<ManagementList>>();

        /// <summary>
        /// Gets the collection of saved views.
        /// </summary>
        public ObservableCollection<StateDescriptor<ManagementList>> Views
        {
            get { return this.views; }
        }

        private IStateDescriptorFactory<ManagementList> savedViewFactory;

        /// <summary>
        /// Gets or sets a factory used to create new views.
        /// </summary>
        public IStateDescriptorFactory<ManagementList> SavedViewFactory
        {
            get
            {
                if (this.savedViewFactory == null)
                {
                    this.savedViewFactory = new ManagementListStateDescriptorFactory();
                }

                return this.savedViewFactory;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                this.savedViewFactory = value;
            }
        }

        #endregion Properties

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the ManagementList class.
        /// </summary>
        public ManagementList()
        {
            this.SearchBox = new SearchBox();
            this.SearchBox.Parser.FullTextRule = this.defaultFullTextSearchRule;

            this.List = new InnerList();
            this.FilterRulePanel = new FilterRulePanel();
            this.AddFilterRulePicker = new AddFilterRulePicker();
            this.Evaluator = new ItemsControlFilterEvaluator();

            // Register the programmatically-added controls as logical children \\
            this.logicalChildren = new ReadOnlyCollection<object>(new object[]
            {
                this.AddFilterRulePicker,
                this.List
            });

            foreach (var logicalChild in this.logicalChildren)
            {
                this.AddLogicalChild(logicalChild);
            }
        }

        #endregion Ctor

        /// <summary>
        /// Moves focus to the SearchBox when Ctrl+E is pressed.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (this.IsSearchShown &&
                e.Key == Key.E &&
                Keyboard.Modifiers == ModifierKeys.Control)
            {
                this.SearchBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
                e.Handled = true;
            }
            else
            {
                base.OnKeyDown(e);
            }
        }

        /// <summary>
        /// Gets an enumerator for logical child elements of this element.
        /// </summary>
        protected override IEnumerator LogicalChildren
        {
            get
            {
                return this.logicalChildren.GetEnumerator();
            }
        }

        partial void OnEvaluatorChangedImplementation(PropertyChangedEventArgs<ItemsControlFilterEvaluator> e)
        {
            // Unregister the previous evaluator \\
            if (e.OldValue != null)
            {
                e.OldValue.RemoveFilterExpressionProvider(this.SearchBox);
                e.OldValue.RemoveFilterExpressionProvider(this.FilterRulePanel);
                e.OldValue.FilterExpressionChanged -= this.Evaluator_FilterExpressionChanged;
                e.NewValue.PropertyChanged -= this.Evaluator_PropertyChanged;
            }

            // Register the new evaluator \\
            e.NewValue.FilterTarget = this.List;
            e.NewValue.AddFilterExpressionProvider(this.SearchBox);
            e.NewValue.AddFilterExpressionProvider(this.FilterRulePanel);
            e.NewValue.FilterExpressionChanged += this.Evaluator_FilterExpressionChanged;
            e.NewValue.PropertyChanged += this.Evaluator_PropertyChanged;
        }

        private void Evaluator_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Our UI components depend on whether our commands can execute, which in turn depend on evaluator properties.
            // To ensure these are always up-to-date, re-evaluate whether the commands can execute when evaluator properties change.
            if (e.PropertyName == "FilterStatus" ||
                e.PropertyName == "HasFilterExpression")
            {
                CommandManager.InvalidateRequerySuggested();
            }

            // If the filter has been applied or cleared, update the scroll position \\
            bool filteredItemsHaveChanged = e.PropertyName == "FilterStatus" &&
                (this.Evaluator.FilterStatus == FilterStatus.Applied || this.Evaluator.FilterStatus == FilterStatus.NotApplied);

            if (filteredItemsHaveChanged && this.List.Items.Count > 0)
            {
                // If no items are selected, select the first item \\
                if (this.List.SelectedIndex < 0)
                {
                    this.List.SelectedIndex = 0;
                }

                this.List.ScrollIntoViewFromTop(this.List.SelectedItem);
            }
        }

        #region Registration Helpers

        /// <summary>
        /// Adds the specified column.
        /// Default filter rules for the column will be added if the filter is shown.
        /// A default search rule will be added if the search box is shown and the column's data type is searchable.
        /// </summary>
        /// <param name="column">The column to add.</param>
        /// <exception cref="ArgumentNullException">The specified value is a null reference.</exception>
        public void AddColumn(InnerListColumn column)
        {
            if (column == null)
            {
                throw new ArgumentNullException("column");
            }

            this.AddColumn(column, this.IsFilterShown);
        }

        /// <summary>
        /// Adds the specified columns, and optionally default filter and search rules.
        /// </summary>
        /// <param name="column">The column to add.</param>
        /// <param name="addDefaultFilterRules">Whether to add default filter rules for the specified column.</param>
        /// <exception cref="ArgumentNullException">The specified value is a null reference.</exception>
        public void AddColumn(InnerListColumn column, bool addDefaultFilterRules)
        {
            if (column == null)
            {
                throw new ArgumentNullException("column");
            }

            this.List.Columns.Add(column);

            if (addDefaultFilterRules)
            {
                Type genericSelectorRuleType = typeof(PropertyValueSelectorFilterRule<>).MakeGenericType(column.DataDescription.DataType);
                ConstructorInfo selectorRuleConstructorInfo = genericSelectorRuleType.GetConstructor(new Type[] { typeof(string), typeof(string) });
                SelectorFilterRule selectorRule = (SelectorFilterRule)selectorRuleConstructorInfo.Invoke(new object[] { column.DataDescription.PropertyName, column.DataDescription.DisplayName });

                if (addDefaultFilterRules)
                {
                    this.AddFilterRulePicker.ColumnFilterRules.Add(new AddFilterRulePickerItem(new FilterRulePanelItem(selectorRule, selectorRule.DisplayName)));
                }

                // Automatically add a search rule if the search box is shown \\
                if (this.IsSearchShown)
                {
                    this.SearchBox.Parser.TryAddSearchableRule<TextContainsFilterRule>(selectorRule);

                    // Add this property to the full-text search properties \\
                    this.defaultFullTextSearchRule.PropertyNames.Add(column.DataDescription.PropertyName);
                }
            }
        }

        /// <summary>
        /// Adds the specified rule, using the rule's display name as its group name.
        /// </summary>
        /// <param name="rule">The rule to add.</param>
        /// <exception cref="ArgumentNullException">The specified value is a null reference.</exception>
        public void AddRule(FilterRule rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException("rule");
            }

            this.AddFilterRulePicker.ShortcutFilterRules.Add(new AddFilterRulePickerItem(new FilterRulePanelItem(rule, rule.DisplayName)));
        }

        /// <summary>
        /// Clears all of the current columns, searchable rules and filter rules.
        /// </summary>
        public void ResetView()
        {
            this.List.Columns.Clear();
            this.defaultFullTextSearchRule.PropertyNames.Clear();
            this.AddFilterRulePicker.ShortcutFilterRules.Clear();
            this.AddFilterRulePicker.ColumnFilterRules.Clear();
            this.SearchBox.Text = string.Empty;
            this.SearchBox.Parser.ClearSearchableRules();
            this.SearchBox.Parser.FullTextRule = this.defaultFullTextSearchRule;
        }

        #endregion

        #region StartFilter

        partial void OnStartFilterCanExecuteImplementation(CanExecuteRoutedEventArgs e)
        {
            // Allow filtering if there is a filter expression or filtering has been triggered \\
            e.CanExecute = this.Evaluator.HasFilterExpression ||
                this.Evaluator.FilterStatus != FilterStatus.NotApplied;
        }

        partial void OnStartFilterExecutedImplementation(ExecutedRoutedEventArgs e)
        {
            this.Evaluator.StartFilter();
        }

        #endregion

        #region OnStopFilter

        partial void OnStopFilterCanExecuteImplementation(CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.Evaluator.FilterStatus == FilterStatus.InProgress;
        }

        partial void OnStopFilterExecutedImplementation(ExecutedRoutedEventArgs e)
        {
            this.Evaluator.StopFilter();
        }

        #endregion

        #region OnClearFilter

        partial void OnClearFilterCanExecuteImplementation(CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.FilterRulePanel.FilterRulePanelItems.Count > 0;
        }

        partial void OnClearFilterExecutedImplementation(ExecutedRoutedEventArgs e)
        {
            for (int i = this.FilterRulePanel.FilterRulePanelItems.Count - 1; i >= 0; i--)
            {
                FilterRulePanel.RemoveRuleCommand.TryExecute(this.FilterRulePanel.FilterRulePanelItems[i], this.FilterRulePanel);
            }

            SearchBox.ClearTextCommand.TryExecute(null, this.SearchBox);

            this.CurrentView = null;

            this.Evaluator.StartFilter();
        }

        #endregion

        #region View Manager Callbacks

        [SuppressMessage("Performance", "CA1822: Mark members as static", Justification = "Potential breaking change")]
        partial void OnSaveViewCanExecuteImplementation(CanExecuteRoutedEventArgs e)
        {
            string viewName = (string)e.Parameter;
            bool isNotEmpty = !string.IsNullOrEmpty(viewName) && (viewName.Trim().Length != 0);
            e.CanExecute = isNotEmpty;
        }

        partial void OnSaveViewExecutedImplementation(ExecutedRoutedEventArgs e)
        {
            string viewName = (string)e.Parameter;

            this.viewSaver.IsOpen = false;

            StateDescriptor<ManagementList> sd = null;

            if ((sd = this.DoesViewAlreadyExist(viewName)) == null)
            {
                sd = this.SavedViewFactory.Create();
                sd.Name = viewName;
                this.Views.Add(sd);
            }

            sd.SaveState(this);

            this.RaiseEvent(new RoutedEventArgs(ViewsChangedEvent));

            this.CurrentView = sd;
        }

        private StateDescriptor<ManagementList> DoesViewAlreadyExist(string viewName)
        {
            foreach (StateDescriptor<ManagementList> sd in this.Views)
            {
                if (sd.Name.Equals(viewName, StringComparison.CurrentCultureIgnoreCase))
                {
                    return sd;
                }
            }

            return null;
        }

        private void ViewManager_ItemSelected(object sender, DataRoutedEventArgs<object> e)
        {
            if (e.Data == null)
            {
                throw new ArgumentException("e.Data is null", "e");
            }

            StateDescriptor<ManagementList> sd = (StateDescriptor<ManagementList>)e.Data;
            sd.RestoreState(this);

            this.CurrentView = sd;
        }

        private void ViewManager_ItemDeleted(object sender, DataRoutedEventArgs<object> e)
        {
            if (e.Data == null)
            {
                throw new ArgumentException("e.Data is null", "e");
            }

            StateDescriptor<ManagementList> sd = (StateDescriptor<ManagementList>)e.Data;
            this.Views.Remove(sd);

            this.RaiseEvent(new RoutedEventArgs(ViewsChangedEvent));

            if (object.ReferenceEquals(sd, this.CurrentView))
            {
                this.CurrentView = null;
            }
        }

        #endregion View Manager Callbacks

        #region OnApplyTemplate

        partial void PreOnApplyTemplate()
        {
            if (this.viewManager != null)
            {
                this.viewManager.ItemSelected -= this.ViewManager_ItemSelected;
                this.viewManager.ItemDeleted -= this.ViewManager_ItemDeleted;
            }
        }

        partial void PostOnApplyTemplate()
        {
            this.viewManager.ItemSelected += this.ViewManager_ItemSelected;
            this.viewManager.ItemDeleted += this.ViewManager_ItemDeleted;
        }

        #endregion OnApplyTemplate

        #region Hooks For Changing Filter State Due To Events

        private void Evaluator_FilterExpressionChanged(object sender, EventArgs e)
        {
            // For non-live mode, stop filtering if the user has cleared the filter (rules and search text).
            // This allows the user to clear search results without having to click the Search button on an empty filter.
            // This happens automatically in live mode.
            if (this.Evaluator.StartFilterOnExpressionChanged == false &&
                this.FilterRulePanel.HasFilterExpression == false &&
                this.SearchBox.HasFilterExpression == false)
            {
                this.Evaluator.StopFilter();
            }
        }

        #endregion Hooks For Changing Filter State Due To Events
    }
}
