// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;

namespace Microsoft.PowerShell.Cmdletization
{
    /// <summary>
    /// Describes whether to report errors when a given filter doesnt match any objects.
    /// </summary>
    public enum BehaviorOnNoMatch
    {
        /// <summary>
        /// Default behavior is to be consistent with the built-in cmdlets:
        /// - When a wildcard is specified, then no errors are reported (i.e. Get-Process -Name noSuchProcess*)
        /// - When no wildcard is specified, then errors are reported (i.e. Get-Process -Name noSuchProcess)
        ///
        /// Note that the following conventions are adopted:
        /// - Min/max queries
        ///   (<see cref="QueryBuilder.FilterByMinPropertyValue(string,object,BehaviorOnNoMatch)"/> and
        ///    <see cref="QueryBuilder.FilterByMaxPropertyValue(string,object,BehaviorOnNoMatch)"/>)
        ///   are treated as wildcards
        /// - Exclusions
        ///   (<see cref="QueryBuilder.ExcludeByProperty(string,System.Collections.IEnumerable,bool,BehaviorOnNoMatch)"/>)
        ///   are treated as wildcards
        /// - Associations
        ///   (<see cref="QueryBuilder.FilterByAssociatedInstance(object,string,string,string,BehaviorOnNoMatch)"/>)
        ///   are treated as not a wildcard.
        /// </summary>
        Default = 0,

        /// <summary>
        /// <c>ReportErrors</c> forces reporting of errors that in other circumstances would be reported if no objects matched the filters.
        /// </summary>
        ReportErrors,

        /// <summary>
        /// <c>SilentlyContinue</c> suppresses errors that in other circumstances would be reported if no objects matched the filters.
        /// </summary>
        SilentlyContinue,
    }

    /// <summary>
    /// QueryBuilder supports building of object model queries in an object-model-agnostic way.
    /// </summary>
    public abstract class QueryBuilder
    {
        /// <summary>
        /// Modifies the query, so that it only returns objects with a given property value.
        /// </summary>
        /// <param name="propertyName">Property name to query on.</param>
        /// <param name="allowedPropertyValues">Property values to accept in the query.</param>
        /// <param name="wildcardsEnabled">
        /// <see langword="true"/> if <paramref name="allowedPropertyValues"/> should be treated as a <see cref="string"/> containing a wildcard pattern;
        /// <see langword="false"/> otherwise.
        /// </param>
        /// <param name="behaviorOnNoMatch">
        /// Describes how to handle filters that didn't match any objects
        /// </param>
        public virtual void FilterByProperty(string propertyName, IEnumerable allowedPropertyValues, bool wildcardsEnabled, BehaviorOnNoMatch behaviorOnNoMatch)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Modifies the query, so that it does not return objects with a given property value.
        /// </summary>
        /// <param name="propertyName">Property name to query on.</param>
        /// <param name="excludedPropertyValues">Property values to reject in the query.</param>
        /// <param name="wildcardsEnabled">
        /// <see langword="true"/> if <paramref name="excludedPropertyValues"/> should be treated as a <see cref="string"/> containing a wildcard pattern;
        /// <see langword="false"/> otherwise.
        /// </param>
        /// <param name="behaviorOnNoMatch">
        /// Describes how to handle filters that didn't match any objects
        /// </param>
        public virtual void ExcludeByProperty(string propertyName, IEnumerable excludedPropertyValues, bool wildcardsEnabled, BehaviorOnNoMatch behaviorOnNoMatch)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Modifies the query, so that it returns only objects that have a property value greater than or equal to a <paramref name="minPropertyValue"/> threshold.
        /// </summary>
        /// <param name="propertyName">Property name to query on.</param>
        /// <param name="minPropertyValue">Minimum property value.</param>
        /// <param name="behaviorOnNoMatch">
        /// Describes how to handle filters that didn't match any objects
        /// </param>
        public virtual void FilterByMinPropertyValue(string propertyName, object minPropertyValue, BehaviorOnNoMatch behaviorOnNoMatch)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Modifies the query, so that it returns only objects that have a property value less than or equal to a <paramref name="maxPropertyValue"/> threshold.
        /// </summary>
        /// <param name="propertyName">Property name to query on.</param>
        /// <param name="maxPropertyValue">Maximum property value.</param>
        /// <param name="behaviorOnNoMatch">
        /// Describes how to handle filters that didn't match any objects
        /// </param>
        public virtual void FilterByMaxPropertyValue(string propertyName, object maxPropertyValue, BehaviorOnNoMatch behaviorOnNoMatch)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Modifies the query, so that it returns only objects associated with <paramref name="associatedInstance"/>
        /// </summary>
        /// <param name="associatedInstance">Object that query results have to be associated with.</param>
        /// <param name="associationName">Name of the association.</param>
        /// <param name="resultRole">Name of the role that <paramref name="associatedInstance"/> has in the association.</param>
        /// <param name="sourceRole">Name of the role that query results have in the association.</param>
        /// <param name="behaviorOnNoMatch">
        /// Describes how to handle filters that didn't match any objects
        /// </param>
        public virtual void FilterByAssociatedInstance(object associatedInstance, string associationName, string sourceRole, string resultRole, BehaviorOnNoMatch behaviorOnNoMatch)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets a query option.
        /// </summary>
        /// <param name="optionName"></param>
        /// <param name="optionValue"></param>
        public virtual void AddQueryOption(string optionName, object optionValue)
        {
            throw new NotImplementedException();
        }
    }
}
