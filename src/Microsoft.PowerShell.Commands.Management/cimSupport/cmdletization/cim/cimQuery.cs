// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Text;

using Microsoft.Management.Infrastructure;
using Microsoft.PowerShell.Cim;
using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Cmdletization.Cim
{
    /// <summary>
    /// CimQuery supports building of queries against CIM object model.
    /// </summary>
    internal class CimQuery : QueryBuilder, ISessionBoundQueryBuilder<CimSession>
    {
        private readonly StringBuilder _wqlCondition;

        private CimInstance _associatedObject;
        private string _associationName;
        private string _resultRole;
        private string _sourceRole;

        internal readonly Dictionary<string, object> queryOptions = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        internal ClientSideQuery ClientSideQuery { get; private set; }

        internal CimQuery()
        {
            _wqlCondition = new StringBuilder();
            this.ClientSideQuery = new ClientSideQuery();
        }

        #region WQL processing

        private void AddWqlCondition(string condition)
        {
            _wqlCondition.Append(_wqlCondition.Length != 0 ? " AND " : " WHERE ");

            _wqlCondition.Append('(');
            _wqlCondition.Append(condition);
            _wqlCondition.Append(')');
        }

        private static string ObjectToWqlLiteral(object o)
        {
            if (LanguagePrimitives.IsNull(o))
            {
                return "null"; // based on an example at https://msdn.microsoft.com/library/aa394054(VS.85).aspx
            }

            o = CimValueConverter.ConvertFromDotNetToCim(o);
            PSObject pso = PSObject.AsPSObject(o);
            Type type = pso.BaseObject.GetType();
            TypeCode typeCode = LanguagePrimitives.GetTypeCode(type);

            if (typeCode == TypeCode.String)
            {
                string s = o.ToString();
                s = s.Replace("\\", "\\\\");
                s = s.Replace("'", "\\'");
                return "'" + s + "'";
            }

            if (typeCode == TypeCode.Char)
            {
                return ObjectToWqlLiteral(LanguagePrimitives.ConvertTo(o, typeof(string), CultureInfo.InvariantCulture));
            }

            if (typeCode == TypeCode.DateTime)
            {
                var dateTime = (DateTime)LanguagePrimitives.ConvertTo(o, typeof(DateTime), CultureInfo.InvariantCulture);
                string result = ClrFacade.ToDmtfDateTime(dateTime);
                return "'" + result + "'";
            }

            if (type == typeof(TimeSpan))
            {
                // WMIv1 does not support using interval literals in a WQL query
                return null;
            }

            if (LanguagePrimitives.IsNumeric(typeCode))
            {
                return (string)LanguagePrimitives.ConvertTo(o, typeof(string), CultureInfo.InvariantCulture);
            }

            if (LanguagePrimitives.IsBooleanType(type))
            {
                if ((bool)LanguagePrimitives.ConvertTo(o, typeof(bool), CultureInfo.InvariantCulture))
                {
                    return "TRUE"; // based on https://msdn.microsoft.com/library/aa394054(VS.85).aspx
                }

                return "FALSE"; // based on https://msdn.microsoft.com/library/aa394054(VS.85).aspx
            }

            throw CimValueConverter.GetInvalidCastException(
                null, /* inner exception */
                "InvalidCimQueryCast",
                o,
                CmdletizationResources.CimConversion_WqlQuery);
        }

        private static string WildcardToWqlLikeOperand(WildcardPattern wildcardPattern, out bool needsClientSideFiltering)
        {
            string nakedOperand = WildcardPatternToCimQueryParser.Parse(wildcardPattern, out needsClientSideFiltering);

            return ObjectToWqlLiteral(nakedOperand);
        }

        #endregion

        private static string GetMatchConditionForEqualityOperator(string propertyName, object propertyValue)
        {
            string condition;

            // comparison of 'char' is case-sensitive in WQL (comparison of 'string' is case-insensitive)
            if (propertyValue is char)
            {
                char c = (char)propertyValue;
                char lowerCase = char.ToLowerInvariant(c);
                char upperCase = char.ToUpperInvariant(c);
                string lowerCaseLiteral = CimQuery.ObjectToWqlLiteral(lowerCase);
                string upperCaseLiteral = CimQuery.ObjectToWqlLiteral(upperCase);
                Dbg.Assert(!string.IsNullOrWhiteSpace(lowerCaseLiteral), "All characters are assumed to have valid WQL literals (lower)");
                Dbg.Assert(!string.IsNullOrWhiteSpace(upperCaseLiteral), "All characters are assumed to have valid WQL literals (upper)");
                condition = string.Format(
                    CultureInfo.InvariantCulture,
                    "(({0} = {1}) OR ({0} = {2}))",
                    propertyName,
                    lowerCaseLiteral,
                    upperCaseLiteral);
                return condition;
            }

            string wqlLiteral = CimQuery.ObjectToWqlLiteral(propertyValue);
            if (string.IsNullOrWhiteSpace(wqlLiteral))
            {
                return null;
            }

            condition = string.Format(
                CultureInfo.InvariantCulture,
                "({0} = {1})",
                propertyName,
                wqlLiteral);
            return condition;
        }

        private static string GetMatchConditionForLikeOperator(string propertyName, object propertyValue)
        {
            var expectedPropertyValueAsString = (string)LanguagePrimitives.ConvertTo(propertyValue, typeof(string), CultureInfo.InvariantCulture);
            var expectedPropertyValueAsPowerShellWildcard = WildcardPattern.Get(expectedPropertyValueAsString, WildcardOptions.IgnoreCase);

            bool needsClientSideFiltering; // not used because for simplicity all results go through post-filtering
            var expectedPropertyValueAsWqlWildcard = CimQuery.WildcardToWqlLikeOperand(expectedPropertyValueAsPowerShellWildcard, out needsClientSideFiltering);

            string condition = string.Format(
                CultureInfo.InvariantCulture,
                "({0} LIKE {1})",
                propertyName,
                expectedPropertyValueAsWqlWildcard);
            return condition;
        }

        private static string GetMatchCondition(string propertyName, IEnumerable propertyValues, bool wildcardsEnabled)
        {
            List<string> individualConditions = propertyValues
                .Cast<object>()
                .Select(propertyValue => wildcardsEnabled
                                             ? GetMatchConditionForLikeOperator(propertyName, propertyValue)
                                             : GetMatchConditionForEqualityOperator(propertyName, propertyValue))
                .Where(individualCondition => !string.IsNullOrWhiteSpace(individualCondition))
                .ToList();
            if (individualConditions.Count == 0)
            {
                return null;
            }

            string result = string.Join(" OR ", individualConditions);
            return result;
        }

        #region Public inputs from cmdletization

        /// <summary>
        /// Modifies the query, so that it only returns objects with a given property value.
        /// </summary>
        /// <param name="propertyName">Property name to query on.</param>
        /// <param name="allowedPropertyValues">Property values to accept in the query.</param>
        /// <param name="wildcardsEnabled">
        ///   <c>true</c> if <paramref name="allowedPropertyValues"/> should be treated as a <see cref="System.String"/> containing a wildcard pattern;
        ///   <c>false otherwise</c>
        /// </param>
        /// <param name="behaviorOnNoMatch">
        /// Describes how to handle filters that didn't match any objects
        /// </param>
        public override void FilterByProperty(string propertyName, IEnumerable allowedPropertyValues, bool wildcardsEnabled, BehaviorOnNoMatch behaviorOnNoMatch)
        {
            this.ClientSideQuery.FilterByProperty(propertyName, allowedPropertyValues, wildcardsEnabled, behaviorOnNoMatch);

            string matchCondition = CimQuery.GetMatchCondition(propertyName, allowedPropertyValues, wildcardsEnabled);
            if (!string.IsNullOrWhiteSpace(matchCondition))
            {
                this.AddWqlCondition(matchCondition);
            }
        }

        /// <summary>
        /// Modifies the query, so that it does not return objects with a given property value.
        /// </summary>
        /// <param name="propertyName">Property name to query on.</param>
        /// <param name="excludedPropertyValues">Property values to reject in the query.</param>
        /// <param name="wildcardsEnabled">
        /// <c>true</c> if <paramref name="excludedPropertyValues"/> should be treated as a <see cref="System.String"/> containing a wildcard pattern;
        /// <c>false otherwise</c>
        /// </param>
        /// <param name="behaviorOnNoMatch">
        /// Describes how to handle filters that didn't match any objects
        /// </param>
        public override void ExcludeByProperty(string propertyName, IEnumerable excludedPropertyValues, bool wildcardsEnabled, BehaviorOnNoMatch behaviorOnNoMatch)
        {
            this.ClientSideQuery.ExcludeByProperty(propertyName, excludedPropertyValues, wildcardsEnabled, behaviorOnNoMatch);

            string positiveWqlCondition = CimQuery.GetMatchCondition(propertyName, excludedPropertyValues, wildcardsEnabled);
            if (!string.IsNullOrWhiteSpace(positiveWqlCondition))
            {
                string condition = string.Format(
                    CultureInfo.InvariantCulture,
                    "NOT ({0})",
                    positiveWqlCondition);
                this.AddWqlCondition(condition);
            }
        }

        /// <summary>
        /// Modifies the query, so that it returns only objects that have a property value greater than or equal to a <paramref name="minPropertyValue"/> threshold.
        /// </summary>
        /// <param name="propertyName">Property name to query on.</param>
        /// <param name="minPropertyValue">Minimum property value.</param>
        /// <param name="behaviorOnNoMatch">
        /// Describes how to handle filters that didn't match any objects
        /// </param>
        public override void FilterByMinPropertyValue(string propertyName, object minPropertyValue, BehaviorOnNoMatch behaviorOnNoMatch)
        {
            this.ClientSideQuery.FilterByMinPropertyValue(propertyName, minPropertyValue, behaviorOnNoMatch);

            string wqlLiteral = CimQuery.ObjectToWqlLiteral(minPropertyValue);
            if (!string.IsNullOrWhiteSpace(wqlLiteral))
            {
                string condition = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} >= {1}",
                    propertyName,
                    wqlLiteral);
                this.AddWqlCondition(condition);
            }
        }

        /// <summary>
        /// Modifies the query, so that it returns only objects that have a property value less than or equal to a <paramref name="maxPropertyValue"/> threshold.
        /// </summary>
        /// <param name="propertyName">Property name to query on.</param>
        /// <param name="maxPropertyValue">Maximum property value.</param>
        /// <param name="behaviorOnNoMatch">
        /// Describes how to handle filters that didn't match any objects
        /// </param>
        public override void FilterByMaxPropertyValue(string propertyName, object maxPropertyValue, BehaviorOnNoMatch behaviorOnNoMatch)
        {
            this.ClientSideQuery.FilterByMaxPropertyValue(propertyName, maxPropertyValue, behaviorOnNoMatch);

            string wqlLiteral = CimQuery.ObjectToWqlLiteral(maxPropertyValue);
            if (!string.IsNullOrWhiteSpace(wqlLiteral))
            {
                string condition = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} <= {1}",
                    propertyName,
                    CimQuery.ObjectToWqlLiteral(maxPropertyValue));
                this.AddWqlCondition(condition);
            }
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
        public override void FilterByAssociatedInstance(object associatedInstance, string associationName, string sourceRole, string resultRole, BehaviorOnNoMatch behaviorOnNoMatch)
        {
            this.ClientSideQuery.FilterByAssociatedInstance(associatedInstance, associationName, sourceRole, resultRole, behaviorOnNoMatch);
            _associatedObject = associatedInstance as CimInstance;
            _associationName = associationName;
            _resultRole = resultRole;
            _sourceRole = sourceRole;
        }

        /// <summary>
        /// Sets a query option.
        /// </summary>
        /// <param name="optionName"></param>
        /// <param name="optionValue"></param>
        public override void AddQueryOption(string optionName, object optionValue)
        {
            if (string.IsNullOrEmpty(optionName))
            {
                throw new ArgumentNullException("optionName");
            }

            if (optionValue == null)
            {
                throw new ArgumentNullException("optionValue");
            }

            this.queryOptions[optionName] = optionValue;
        }

        #endregion Cmdletization inputs

        #region Outputs for doing the query

        internal StartableJob GetQueryJob(CimJobContext jobContext)
        {
            if (_associationName == null)
            {
                return new QueryInstancesJob(jobContext, this, _wqlCondition.ToString());
            }
            else
            {
                return new EnumerateAssociatedInstancesJob(jobContext, this, _associatedObject, _associationName, _resultRole, _sourceRole);
            }
        }

        internal bool IsMatchingResult(CimInstance result)
        {
            Dbg.Assert(result != null, "Caller should verify result != null");
            return this.ClientSideQuery.IsResultMatchingClientSideQuery(result);
        }

        internal IEnumerable<ClientSideQuery.NotFoundError> GenerateNotFoundErrors()
        {
            return this.ClientSideQuery.GenerateNotFoundErrors();
        }

        #endregion

        CimSession ISessionBoundQueryBuilder<CimSession>.GetTargetSession()
        {
            if (_associatedObject != null)
            {
                return CimCmdletAdapter.GetSessionOfOriginFromCimInstance(_associatedObject);
            }

            return null;
        }

        /// <summary>
        /// Returns a string that represents the current CIM query.
        /// </summary>
        /// <returns>A string that represents the current CIM query.</returns>
        public override string ToString()
        {
            return _wqlCondition.ToString();
        }
    }
}
