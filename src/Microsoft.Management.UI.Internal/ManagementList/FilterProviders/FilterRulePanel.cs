// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The FilterRulePanel allows users to construct and display a complex query built using <see cref="FilterRule"/>s.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The FilterRulePanel manages two primary entities: <see cref="FilterRulePanelItem"/>s and DataTemplates.
    /// /// </para>
    /// <para>
    /// <see cref="FilterRulePanelItem" />s are the data classes that store the state for each item in the panel.
    /// They are added and removed to/from the panel using the AddRulesCommand and the RemoveRuleCommand commands.
    /// </para>
    /// <para>
    /// For a FilterRule to display in the panel it must have a DataTemplate registered. To add and remove
    /// DataTemplates, use the AddFilterRulePanelItemContentTemplate and RemoveFilterRulePanelItemContentTemplate methods.
    /// </para>
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public partial class FilterRulePanel : Control, IFilterExpressionProvider
    {
        #region Properties

        #region Filter Rule Panel Items

        /// <summary>
        /// Gets the collection of FilterRulePanelItems that are currently
        /// displayed in the panel.
        /// </summary>
        public ReadOnlyCollection<FilterRulePanelItem> FilterRulePanelItems
        {
            get
            {
                return this.Controller.FilterRulePanelItems;
            }
        }

        #endregion Filter Rule Panel Items

        #region Filter Expression

        /// <summary>
        /// Gets a FilterExpression representing the current
        /// relational organization of FilterRules for this provider.
        /// </summary>
        public FilterExpressionNode FilterExpression
        {
            get
            {
                return this.Controller.FilterExpression;
            }
        }

        #endregion Filter Expression

        #region Controller

        private FilterRulePanelController controller = new FilterRulePanelController();

        /// <summary>
        /// Gets the FilterRulePanelController associated with this FilterRulePanel.
        /// </summary>
        public FilterRulePanelController Controller
        {
            get
            {
                return this.controller;
            }
        }

        #endregion Controller

        #region Filter Rule Template Selector

        private FilterRuleTemplateSelector filterRuleTemplateSelector;

        /// <summary>
        /// Gets a FilterRuleTemplateSelector that stores
        /// the templates used for items in the panel.
        /// </summary>
        public DataTemplateSelector FilterRuleTemplateSelector
        {
            get
            {
                return this.filterRuleTemplateSelector;
            }
        }

        #endregion Filter Rule Template Selector

        /// <summary>
        /// Gets a value indicating whether this provider currently has a non-empty filter expression.
        /// </summary>
        public bool HasFilterExpression
        {
            get
            {
                return this.Controller.HasFilterExpression;
            }
        }

        #endregion Properties

        #region Events

        /// <summary>
        /// Raised when a FilterRulePanelItem has been added or removed.
        /// </summary>
        public event EventHandler FilterExpressionChanged;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the FilterRulePanel class.
        /// </summary>
        public FilterRulePanel()
        {
            this.InitializeTemplates();

            this.Controller.FilterExpressionChanged += this.Controller_FilterExpressionChanged;
        }

        #endregion Ctor

        #region Public Methods

        #region Content Templates

        /// <summary>
        /// Associates a DataTemplate with a Type so that objects of that Type
        /// that are displayed in FilterRulePanel use the specified DataTemplate.
        /// </summary>
        /// <param name="type">
        /// The type to associate the DataTemplate with.
        /// </param>
        /// <param name="dataTemplate">
        /// The DataTemplate to associate the type with.
        /// </param>
        public void AddFilterRulePanelItemContentTemplate(Type type, DataTemplate dataTemplate)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (dataTemplate == null)
            {
                throw new ArgumentNullException("dataTemplate");
            }

            this.filterRuleTemplateSelector.TemplateDictionary.Add(new KeyValuePair<Type, DataTemplate>(type, dataTemplate));
        }

        /// <summary>
        /// Removes the Type and associated DataTemplate from usage when displaying objects
        /// of that type in the FilterRulePanel.
        /// </summary>
        /// <param name="type">
        /// The type to remove.
        /// </param>
        public void RemoveFilterRulePanelItemContentTemplate(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            this.filterRuleTemplateSelector.TemplateDictionary.Remove(type);
        }

        /// <summary>
        /// Gets a DataTemplate associated with a type.
        /// </summary>
        /// <param name="type">A Type whose DataTemplate will be returned.</param>
        /// <param name="dataTemplate">A DataTemplate registered for type.</param>
        /// <returns>Returns true if there is a DataTemplate registered for type, false otherwise.</returns>
        public bool TryGetContentTemplate(Type type, out DataTemplate dataTemplate)
        {
            dataTemplate = null;
            return this.filterRuleTemplateSelector.TemplateDictionary.TryGetValue(type, out dataTemplate);
        }

        /// <summary>
        /// Removes all the registered content templates.
        /// </summary>
        public void ClearContentTemplates()
        {
            this.filterRuleTemplateSelector.TemplateDictionary.Clear();
        }

        #endregion Content Templates

        #region Notify Filter Expression Changed

        /// <summary>
        /// Notifies any listeners that the filter expression has changed.
        /// </summary>
        protected virtual void NotifyFilterExpressionChanged()
        {
            #pragma warning disable IDE1005 // IDE1005: Delegate invocation can be simplified.
            EventHandler eh = this.FilterExpressionChanged;
            if (eh != null)
            {
                eh(this, new EventArgs());
            }
            #pragma warning restore IDE1005
        }

        private void Controller_FilterExpressionChanged(object sender, EventArgs e)
        {
            this.NotifyFilterExpressionChanged();
        }

        #endregion Notify Filter Expression Changed

        #endregion Public Methods

        #region Private Methods

        #region Add Rules Command Callback

        partial void OnAddRulesExecutedImplementation(ExecutedRoutedEventArgs e)
        {
            Debug.Assert(e != null, "not null");

            if (e.Parameter == null)
            {
                throw new ArgumentException("e.Parameter is null.", "e");
            }

            List<FilterRulePanelItem> itemsToAdd = new List<FilterRulePanelItem>();

            IList selectedItems = (IList)e.Parameter;
            foreach (object item in selectedItems)
            {
                FilterRulePanelItem newItem = item as FilterRulePanelItem;
                if (newItem == null)
                {
                    throw new ArgumentException(
                        "e.Parameter contains a value which is not a valid FilterRulePanelItem object.",
                        "e");
                }

                itemsToAdd.Add(newItem);
            }

            foreach (FilterRulePanelItem item in itemsToAdd)
            {
                this.AddFilterRuleInternal(item);
            }
        }

        #endregion Add Rules Command Callback

        #region Remove Rule Command Callback

        partial void OnRemoveRuleExecutedImplementation(ExecutedRoutedEventArgs e)
        {
            Debug.Assert(e != null, "not null");

            if (e.Parameter == null)
            {
                throw new ArgumentException("e.Parameter is null.", "e");
            }

            FilterRulePanelItem item = e.Parameter as FilterRulePanelItem;
            if (item == null)
            {
                throw new ArgumentException("e.Parameter is not a valid FilterRulePanelItem object.", "e");
            }

            this.RemoveFilterRuleInternal(item);
        }

        #endregion Remove Rule Command Callback

        #region InitializeTemplates

        private void InitializeTemplates()
        {
            this.filterRuleTemplateSelector = new FilterRuleTemplateSelector();

            this.InitializeTemplatesForInputTypes();

            List<KeyValuePair<Type, string>> defaultTemplates = new List<KeyValuePair<Type, string>>()
            {
                new KeyValuePair<Type, string>(typeof(SelectorFilterRule), "CompositeRuleTemplate"),
                new KeyValuePair<Type, string>(typeof(SingleValueComparableValueFilterRule<>), "ComparableValueRuleTemplate"),
                new KeyValuePair<Type, string>(typeof(IsEmptyFilterRule), "NoInputTemplate"),
                new KeyValuePair<Type, string>(typeof(IsNotEmptyFilterRule), "NoInputTemplate"),
                new KeyValuePair<Type, string>(typeof(FilterRulePanelItemType), "FilterRulePanelGroupItemTypeTemplate"),
                new KeyValuePair<Type, string>(typeof(ValidatingValue<>), "ValidatingValueTemplate"),
                new KeyValuePair<Type, string>(typeof(ValidatingSelectorValue<>), "ValidatingSelectorValueTemplate"),
                new KeyValuePair<Type, string>(typeof(IsBetweenFilterRule<>), "IsBetweenRuleTemplate"),
                new KeyValuePair<Type, string>(typeof(object), "CatchAllTemplate")
            };

            defaultTemplates.ForEach(templateInfo => this.AddFilterRulePanelItemContentTemplate(templateInfo.Key, templateInfo.Value));
        }

        private void InitializeTemplatesForInputTypes()
        {
            List<Type> inputTypes = new List<Type>()
            {
                typeof(sbyte),
                typeof(byte),
                typeof(short),
                typeof(int),
                typeof(long),
                typeof(ushort),
                typeof(uint),
                typeof(ulong),
                typeof(char),
                typeof(Single),
                typeof(double),
                typeof(decimal),
                typeof(bool),
                typeof(Enum),
                typeof(DateTime),
                typeof(string)
            };

            inputTypes.ForEach(type => this.AddFilterRulePanelItemContentTemplate(type, "InputValueTemplate"));
        }

        private void AddFilterRulePanelItemContentTemplate(Type type, string resourceName)
        {
            Debug.Assert(type != null, "not null");
            Debug.Assert(!string.IsNullOrEmpty(resourceName), "not null");

            var templateInfo = new ComponentResourceKey(typeof(FilterRulePanel), resourceName);

            DataTemplate template = (DataTemplate)this.TryFindResource(templateInfo);
            Debug.Assert(template != null, "not null");

            this.AddFilterRulePanelItemContentTemplate(type, template);
        }

        #endregion InitializeTemplates

        #region Add/Remove FilterRules to Controller

        private void AddFilterRuleInternal(FilterRulePanelItem item)
        {
            Debug.Assert(item != null, "not null");

            FilterRulePanelItem newItem = new FilterRulePanelItem(item.Rule.DeepCopy(), item.GroupId);
            this.Controller.AddFilterRulePanelItem(newItem);
        }

        private void RemoveFilterRuleInternal(FilterRulePanelItem item)
        {
            Debug.Assert(item != null, "not null");
            this.Controller.RemoveFilterRulePanelItem(item);
        }

        #endregion Add/Remove FilterRules to Controller

        #endregion Private Methods
    }
}
