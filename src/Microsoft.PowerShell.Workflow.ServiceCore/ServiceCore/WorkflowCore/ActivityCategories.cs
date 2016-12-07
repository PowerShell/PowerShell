using System;
using System.Activities;
using System.ComponentModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using Microsoft.PowerShell.Workflow;

namespace Microsoft.PowerShell.Activities
{
    /// <summary>
    /// Category attribute for "Activity-Specific Parameters"
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public sealed class ParameterSpecificCategoryAttribute : CategoryAttribute
    {
        /// <summary>
        /// Creates the attribute.
        /// </summary>
        public ParameterSpecificCategoryAttribute() : base("") { }

        /// <summary>
        /// Gets a localized version of the attribute description.
        /// </summary>
        /// <param name="value">Not used.</param>
        /// <returns>A localized version of the attribute description</returns>
        protected override string GetLocalizedString(string value)
        {
            return Resources.ActivityParameterGroup;
        }
    }

    /// <summary>
    /// Category attribute for "Input and Output"
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public sealed class InputAndOutputCategoryAttribute : CategoryAttribute
    {
        /// <summary>
        /// Creates the attribute.
        /// </summary>
        public InputAndOutputCategoryAttribute() : base("") { }

        /// <summary>
        /// Gets a localized version of the attribute description.
        /// </summary>
        /// <param name="value">Not used.</param>
        /// <returns>A localized version of the attribute description</returns>
        protected override string GetLocalizedString(string value)
        {
            return Resources.InputAndOutputGroup;
        }
    }

    /// <summary>
    /// Category attribute for "Behavior"
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public sealed class BehaviorCategoryAttribute : CategoryAttribute
    {
        /// <summary>
        /// Creates the attribute.
        /// </summary>
        public BehaviorCategoryAttribute() : base("") { }

        /// <summary>
        /// Gets a localized version of the attribute description.
        /// </summary>
        /// <param name="value">Not used.</param>
        /// <returns>A localized version of the attribute description</returns>
        protected override string GetLocalizedString(string value)
        {
            return CategoryAttribute.Behavior.Category;
        }
    }

    /// <summary>
    /// Category attribute for "Connectivity"
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public sealed class ConnectivityCategoryAttribute : CategoryAttribute
    {
        /// <summary>
        /// Creates the attribute.
        /// </summary>
        public ConnectivityCategoryAttribute() : base("") { }

        /// <summary>
        /// Gets a localized version of the attribute description.
        /// </summary>
        /// <param name="value">Not used.</param>
        /// <returns>A localized version of the attribute description</returns>
        protected override string GetLocalizedString(string value)
        {
            return Resources.ConnectivityGroup;
        }
    }
}
