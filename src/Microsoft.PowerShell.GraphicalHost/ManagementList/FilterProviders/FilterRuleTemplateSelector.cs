//-----------------------------------------------------------------------
// <copyright file="FilterRuleTemplateSelector.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.Management.UI.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// The FilterRuleTemplateSelector class selects a template based upon the type of
    /// the item and the corresponding template that is registered in the TemplateDictionary.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class FilterRuleTemplateSelector : DataTemplateSelector
    {
        private Dictionary<Type, DataTemplate> templateDictionary = new Dictionary<Type, DataTemplate>();

        /// <summary>
        /// Gets the dictionary containing the type-template values.
        /// </summary>
        public IDictionary<Type, DataTemplate> TemplateDictionary
        {
            get { return this.templateDictionary; }
        }

        /// <summary>
        /// Selects a template based upon the type of the item and and the
        /// corresponding template that is registered in the TemplateDictionary.
        /// </summary>
        /// <param name="item">
        /// The item to return a template for.
        /// </param>
        /// <param name="container">
        /// The parameter is not used.
        /// </param>
        /// <returns>
        /// Returns a DataTemplate for item.
        /// </returns>
        public override DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container)
        {
            if (null == item)
            {
                return base.SelectTemplate(item, container);
            }

            Type type = item as Type;
            if (null == type)
            {
                type = item.GetType();
            }

            DataTemplate template;

            do
            {
                if (type.IsGenericType)
                {
                    type = type.GetGenericTypeDefinition();
                }

                if (this.TemplateDictionary.TryGetValue(type, out template))
                {
                    return template;
                }

                type = type.BaseType;
            }
            while (null != type);

            return base.SelectTemplate(item, container);
        }
    }
}
