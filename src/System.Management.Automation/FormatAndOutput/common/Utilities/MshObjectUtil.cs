// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Text;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    /// <summary>
    /// Class containing miscellaneous helpers to deal with
    /// PSObject manipulation.
    /// </summary>
    internal static class PSObjectHelper
    {
        #region tracer
        [TraceSource("PSObjectHelper", "PSObjectHelper")]
        private static readonly PSTraceSource s_tracer = PSTraceSource.GetTracer("PSObjectHelper", "PSObjectHelper");
        #endregion tracer

        internal const char Ellipsis = '\u2026';
        internal const string EllipsisStr = "\u2026";

        internal static string PSObjectIsOfExactType(Collection<string> typeNames)
        {
            if (typeNames.Count != 0)
                return typeNames[0];
            return null;
        }

        internal static bool PSObjectIsEnum(Collection<string> typeNames)
        {
            if (typeNames.Count < 2 || string.IsNullOrEmpty(typeNames[1]))
                return false;
            return string.Equals(typeNames[1], "System.Enum", StringComparison.Ordinal);
        }

        /// <summary>
        /// Retrieve the display name. It looks for a well known property and,
        /// if not found, it uses some heuristics to get a "close" match.
        /// </summary>
        /// <param name="target">Shell object to process.</param>
        /// <param name="expressionFactory">Expression factory to create PSPropertyExpression.</param>
        /// <returns>Resolved PSPropertyExpression; null if no match was found.</returns>
        internal static PSPropertyExpression GetDisplayNameExpression(PSObject target, PSPropertyExpressionFactory expressionFactory)
        {
            // first try to get the expression from the object (types.ps1xml data)
            PSPropertyExpression expressionFromObject = GetDefaultNameExpression(target);
            if (expressionFromObject != null)
            {
                return expressionFromObject;
            }

            // we failed the default display name, let's try some well known names
            // trying to get something potentially useful
            string[] knownPatterns = new string[] {
                "name", "id", "key", "*key", "*name", "*id",
            };

            // go over the patterns, looking for the first match
            foreach (string pattern in knownPatterns)
            {
                PSPropertyExpression ex = new PSPropertyExpression(pattern);
                List<PSPropertyExpression> exprList = ex.ResolveNames(target);

                while ((exprList.Count > 0) && (
                    exprList[0].ToString().Equals(RemotingConstants.ComputerNameNoteProperty, StringComparison.OrdinalIgnoreCase) ||
                    exprList[0].ToString().Equals(RemotingConstants.ShowComputerNameNoteProperty, StringComparison.OrdinalIgnoreCase) ||
                    exprList[0].ToString().Equals(RemotingConstants.RunspaceIdNoteProperty, StringComparison.OrdinalIgnoreCase) ||
                    exprList[0].ToString().Equals(RemotingConstants.SourceJobInstanceId, StringComparison.OrdinalIgnoreCase)))
                {
                    exprList.RemoveAt(0);
                }

                if (exprList.Count == 0)
                    continue;

                // if more than one match, just return the first one
                return exprList[0];
            }

            // we did not find anything
            return null;
        }

        /// <summary>
        /// It gets the display name value.
        /// </summary>
        /// <param name="target">Shell object to process.</param>
        /// <param name="expressionFactory">Expression factory to create PSPropertyExpression.</param>
        /// <returns>PSPropertyExpressionResult if successful; null otherwise.</returns>
        internal static PSPropertyExpressionResult GetDisplayName(PSObject target, PSPropertyExpressionFactory expressionFactory)
        {
            // get the expression to evaluate
            PSPropertyExpression ex = GetDisplayNameExpression(target, expressionFactory);
            if (ex == null)
                return null;

            // evaluate the expression
            List<PSPropertyExpressionResult> resList = ex.GetValues(target);

            if (resList.Count == 0 || resList[0].Exception != null)
            {
                // no results or the retrieval on the first one failed
                return null;
            }
            // return something only if the first match was successful
            return resList[0];
        }

        /// <summary>
        /// This is necessary only to consider IDictionaries as IEnumerables, since LanguagePrimitives.GetEnumerable does not.
        /// </summary>
        /// <param name="obj">Object to extract the IEnumerable from.</param>
        internal static IEnumerable GetEnumerable(object obj)
        {
            PSObject mshObj = obj as PSObject;
            if (mshObj != null)
            {
                obj = mshObj.BaseObject;
            }

            if (obj is IDictionary)
            {
                return (IEnumerable)obj;
            }

            return LanguagePrimitives.GetEnumerable(obj);
        }

        private static string GetSmartToStringDisplayName(object x, PSPropertyExpressionFactory expressionFactory)
        {
            PSPropertyExpressionResult r = PSObjectHelper.GetDisplayName(PSObjectHelper.AsPSObject(x), expressionFactory);
            if ((r != null) && (r.Exception == null))
            {
                return PSObjectHelper.AsPSObject(r.Result).ToString();
            }
            else
            {
                return PSObjectHelper.AsPSObject(x).ToString();
            }
        }

        private static string GetObjectName(object x, PSPropertyExpressionFactory expressionFactory)
        {
            string objName;

            // check if the underlying object is of primitive type
            // if so just return its value
            if (x is PSObject &&
                (LanguagePrimitives.IsBoolOrSwitchParameterType((((PSObject)x).BaseObject).GetType()) ||
                LanguagePrimitives.IsNumeric(((((PSObject)x).BaseObject).GetType()).GetTypeCode()) ||
                LanguagePrimitives.IsNull(x)))
            {
                objName = x.ToString();
            }
            else if (x == null)
            {
                // use PowerShell's $null variable to indicate that the value is null...
                objName = "$null";
            }
            else
            {
                MethodInfo toStringMethod = x.GetType().GetMethod("ToString", Type.EmptyTypes);
                // TODO:CORECLR double check with CORE CLR that x.GetType() == toStringMethod.ReflectedType
                // Check if the given object "x" implements "toString" method. Do that by comparing "DeclaringType" which 'Gets the class that declares this member' and the object type
                if (toStringMethod.DeclaringType == x.GetType())
                {
                    objName = PSObjectHelper.AsPSObject(x).ToString();
                }
                else
                {
                    PSPropertyExpressionResult r = PSObjectHelper.GetDisplayName(PSObjectHelper.AsPSObject(x), expressionFactory);
                    if ((r != null) && (r.Exception == null))
                    {
                        objName = PSObjectHelper.AsPSObject(r.Result).ToString();
                    }
                    else
                    {
                        objName = PSObjectHelper.AsPSObject(x).ToString();
                        if (objName == string.Empty)
                        {
                            var baseObj = PSObject.Base(x);
                            if (baseObj != null)
                            {
                                objName = baseObj.ToString();
                            }
                        }
                    }
                }
            }

            return objName;
        }

        /// <summary>
        /// Helper to convert an PSObject into a string
        /// It takes into account enumerations (use display name)
        /// </summary>
        /// <param name="so">Shell object to process.</param>
        /// <param name="expressionFactory">Expression factory to create PSPropertyExpression.</param>
        /// <param name="enumerationLimit">Limit on IEnumerable enumeration.</param>
        /// <param name="formatErrorObject">Stores errors during string conversion.</param>
        /// <param name="formatFloat">Determine if to format floating point numbers using current culture.</param>
        /// <returns>String representation.</returns>
        internal static string SmartToString(PSObject so, PSPropertyExpressionFactory expressionFactory, int enumerationLimit, StringFormatError formatErrorObject, bool formatFloat = false)
        {
            if (so == null)
                return string.Empty;

            try
            {
                IEnumerable e = PSObjectHelper.GetEnumerable(so);
                if (e != null)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append('{');

                    bool first = true;
                    int enumCount = 0;
                    IEnumerator enumerator = e.GetEnumerator();
                    if (enumerator != null)
                    {
                        IBlockingEnumerator<object> be = enumerator as IBlockingEnumerator<object>;
                        if (be != null)
                        {
                            while (be.MoveNext(false))
                            {
                                if (LocalPipeline.GetExecutionContextFromTLS().CurrentPipelineStopping)
                                {
                                    throw new PipelineStoppedException();
                                }

                                if (enumerationLimit >= 0)
                                {
                                    if (enumCount == enumerationLimit)
                                    {
                                        sb.Append(Ellipsis);
                                        break;
                                    }

                                    enumCount++;
                                }

                                if (!first)
                                {
                                    sb.Append(", ");
                                }

                                sb.Append(GetObjectName(be.Current, expressionFactory));
                                if (first)
                                    first = false;
                            }
                        }
                        else
                        {
                            foreach (object x in e)
                            {
                                if (LocalPipeline.GetExecutionContextFromTLS().CurrentPipelineStopping)
                                {
                                    throw new PipelineStoppedException();
                                }

                                if (enumerationLimit >= 0)
                                {
                                    if (enumCount == enumerationLimit)
                                    {
                                        sb.Append(Ellipsis);
                                        break;
                                    }

                                    enumCount++;
                                }

                                if (!first)
                                {
                                    sb.Append(", ");
                                }

                                sb.Append(GetObjectName(x, expressionFactory));
                                if (first)
                                    first = false;
                            }
                        }
                    }

                    sb.Append('}');
                    return sb.ToString();
                }

                if (formatFloat && so.BaseObject is not null)
                {
                    // format numbers using the current culture
                    if (so.BaseObject is double dbl)
                    {
                        return dbl.ToString("F");
                    }
                    else if (so.BaseObject is float f)
                    {
                        return f.ToString("F");
                    }
                    else if (so.BaseObject is decimal d)
                    {
                        return d.ToString("F");
                    }
                }

                return so.ToString();
            }
            catch (Exception e) when (e is ExtendedTypeSystemException || e is InvalidOperationException)
            {
                // These exceptions are being caught and handled by returning an empty string when
                // the object cannot be stringified due to ETS or an instance in the collection has been modified
                s_tracer.TraceWarning($"SmartToString method: Exception during conversion to string, emitting empty string: {e.Message}");

                if (formatErrorObject != null)
                {
                    formatErrorObject.sourceObject = so;
                    formatErrorObject.exception = e;
                }

                return string.Empty;
            }
        }

        private static readonly PSObject s_emptyPSObject = new PSObject(string.Empty);

        internal static PSObject AsPSObject(object obj)
        {
            return (obj == null) ? s_emptyPSObject : PSObject.AsPSObject(obj);
        }

        /// <summary>
        /// Format an object using a provided format string directive.
        /// </summary>
        /// <param name="directive">Format directive object to use.</param>
        /// <param name="val">Object to format.</param>
        /// <param name="enumerationLimit">Limit on IEnumerable enumeration.</param>
        /// <param name="formatErrorObject">Formatting error object, if present.</param>
        /// <param name="expressionFactory">Expression factory to create PSPropertyExpression.</param>
        /// <returns>String representation.</returns>
        internal static string FormatField(FieldFormattingDirective directive, object val, int enumerationLimit,
            StringFormatError formatErrorObject, PSPropertyExpressionFactory expressionFactory)
        {
            PSObject so = PSObjectHelper.AsPSObject(val);
            bool isTable = false;
            if (directive is not null)
            {
                isTable = directive.isTable;
                if (!string.IsNullOrEmpty(directive.formatString))
                {
                    // we have a formatting directive, apply it
                    // NOTE: with a format directive, we do not make any attempt
                    // to deal with IEnumerable
                    try
                    {
                        // use some heuristics to determine if we have "composite formatting"
                        // 2004/11/16-JonN This is heuristic but should be safe enough
                        if (directive.formatString.Contains("{0") || directive.formatString.Contains('}'))
                        {
                            // we do have it, just use it
                            return string.Format(CultureInfo.CurrentCulture, directive.formatString, so);
                        }
                        // we fall back to the PSObject's IFormattable.ToString()
                        // pass a null IFormatProvider
                        return so.ToString(directive.formatString, formatProvider: null);
                    }
                    catch (Exception e) // 2004/11/17-JonN This covers exceptions thrown in
                                        // string.Format and PSObject.ToString().
                                        // I think we can swallow these.
                    {
                        // NOTE: we catch all the exceptions, since we do not know
                        // what the underlying object access would throw
                        if (formatErrorObject is not null)
                        {
                            formatErrorObject.sourceObject = so;
                            formatErrorObject.exception = e;
                            formatErrorObject.formatString = directive.formatString;
                            return string.Empty;
                        }
                    }
                }
            }

            // we do not have a formatting directive or we failed the formatting (fallback)
            // but we did not report as an error;
            // this call would deal with IEnumerable if the object implements it
            return PSObjectHelper.SmartToString(so, expressionFactory, enumerationLimit, formatErrorObject, isTable);
        }

        private static PSMemberSet MaskDeserializedAndGetStandardMembers(PSObject so)
        {
            Diagnostics.Assert(so != null, "Shell object to process cannot be null");
            var typeNames = so.InternalTypeNames;
            Collection<string> typeNamesWithoutDeserializedPrefix = Deserializer.MaskDeserializationPrefix(typeNames);
            if (typeNamesWithoutDeserializedPrefix == null)
            {
                return null;
            }

            TypeTable typeTable = so.GetTypeTable();
            if (typeTable == null)
            {
                return null;
            }

            PSMemberInfoInternalCollection<PSMemberInfo> members =
                typeTable.GetMembers<PSMemberInfo>(new ConsolidatedString(typeNamesWithoutDeserializedPrefix));
            return members[TypeTable.PSStandardMembers] as PSMemberSet;
        }

        private static List<PSPropertyExpression> GetDefaultPropertySet(PSMemberSet standardMembersSet)
        {
            if (standardMembersSet != null)
            {
                PSPropertySet defaultDisplayPropertySet = standardMembersSet.Members[TypeTable.DefaultDisplayPropertySet] as PSPropertySet;
                if (defaultDisplayPropertySet != null)
                {
                    List<PSPropertyExpression> retVal = new List<PSPropertyExpression>();
                    foreach (string prop in defaultDisplayPropertySet.ReferencedPropertyNames)
                    {
                        if (!string.IsNullOrEmpty(prop))
                        {
                            retVal.Add(new PSPropertyExpression(prop));
                        }
                    }

                    return retVal;
                }
            }

            return new List<PSPropertyExpression>();
        }

        /// <summary>
        /// Helper to retrieve the default property set of a shell object.
        /// </summary>
        /// <param name="so">Shell object to process.</param>
        /// <returns>Resolved expression; empty list if not found.</returns>
        internal static List<PSPropertyExpression> GetDefaultPropertySet(PSObject so)
        {
            List<PSPropertyExpression> retVal = GetDefaultPropertySet(so.PSStandardMembers);
            if (retVal.Count == 0)
            {
                retVal = GetDefaultPropertySet(MaskDeserializedAndGetStandardMembers(so));
            }

            return retVal;
        }

        private static PSPropertyExpression GetDefaultNameExpression(PSMemberSet standardMembersSet)
        {
            if (standardMembersSet != null)
            {
                PSNoteProperty defaultDisplayProperty = standardMembersSet.Members[TypeTable.DefaultDisplayProperty] as PSNoteProperty;
                if (defaultDisplayProperty != null)
                {
                    string expressionString = defaultDisplayProperty.Value.ToString();
                    if (string.IsNullOrEmpty(expressionString))
                    {
                        // invalid data, the PSObject is empty
                        return null;
                    }
                    else
                    {
                        return new PSPropertyExpression(expressionString);
                    }
                }
            }

            return null;
        }

        private static PSPropertyExpression GetDefaultNameExpression(PSObject so)
        {
            PSPropertyExpression retVal = GetDefaultNameExpression(so.PSStandardMembers) ??
                                   GetDefaultNameExpression(MaskDeserializedAndGetStandardMembers(so));

            return retVal;
        }

        /// <summary>
        /// Helper to retrieve the value of an PSPropertyExpression and to format it.
        /// </summary>
        /// <param name="so">Shell object to process.</param>
        /// <param name="enumerationLimit">Limit on IEnumerable enumeration.</param>
        /// <param name="ex">Expression to use for retrieval.</param>
        /// <param name="directive">Format directive to use for formatting.</param>
        /// <param name="formatErrorObject"></param>
        /// <param name="expressionFactory">Expression factory to create PSPropertyExpression.</param>
        /// <param name="result">Not null if an error condition arose.</param>
        /// <returns>Formatted string.</returns>
        internal static string GetExpressionDisplayValue(
            PSObject so,
            int enumerationLimit,
            PSPropertyExpression ex,
            FieldFormattingDirective directive,
            StringFormatError formatErrorObject,
            PSPropertyExpressionFactory expressionFactory,
            out PSPropertyExpressionResult result)
        {
            result = null;
            List<PSPropertyExpressionResult> resList = ex.GetValues(so);

            if (resList.Count == 0)
            {
                return string.Empty;
            }

            result = resList[0];
            if (result.Exception != null)
            {
                return string.Empty;
            }

            return PSObjectHelper.FormatField(directive, result.Result, enumerationLimit, formatErrorObject, expressionFactory);
        }

        /// <summary>
        /// Queries PSObject and determines if ComputerName property should be shown.
        /// </summary>
        /// <param name="so"></param>
        /// <returns></returns>
        internal static bool ShouldShowComputerNameProperty(PSObject so)
        {
            bool result = false;
            if (so != null)
            {
                try
                {
                    PSPropertyInfo computerNameProperty = so.Properties[RemotingConstants.ComputerNameNoteProperty];
                    PSPropertyInfo showComputerNameProperty = so.Properties[RemotingConstants.ShowComputerNameNoteProperty];

                    // if computer name property exists then this must be a remote object. see
                    // if it can be displayed.
                    if ((computerNameProperty != null) && (showComputerNameProperty != null))
                    {
                        LanguagePrimitives.TryConvertTo<bool>(showComputerNameProperty.Value, out result);
                    }
                }
                catch (ArgumentException)
                {
                    // ignore any exceptions thrown retrieving the *ComputerName properties
                    // from the object
                }
                catch (ExtendedTypeSystemException)
                {
                    // ignore any exceptions thrown retrieving the *ComputerName properties
                    // from the object
                }
            }

            return result;
        }
    }

    internal abstract class FormattingError
    {
        internal object sourceObject;
    }

    internal sealed class PSPropertyExpressionError : FormattingError
    {
        internal PSPropertyExpressionResult result;
    }

    internal sealed class StringFormatError : FormattingError
    {
        internal string formatString;
        internal Exception exception;
    }

    internal delegate ScriptBlock CreateScriptBlockFromString(string scriptBlockString);

    /// <summary>
    /// Helper class to create PSPropertyExpression's from format.ps1xml data structures.
    /// </summary>
    internal sealed class PSPropertyExpressionFactory
    {
        /// <exception cref="ParseException"></exception>
        internal void VerifyScriptBlockText(string scriptText)
        {
            ScriptBlock.Create(scriptText);
        }

        /// <summary>
        /// Create an expression from an expression token.
        /// </summary>
        /// <param name="et">Expression token to use.</param>
        /// <returns>Constructed expression.</returns>
        /// <exception cref="ParseException"></exception>
        internal PSPropertyExpression CreateFromExpressionToken(ExpressionToken et)
        {
            return CreateFromExpressionToken(et, null);
        }

        /// <summary>
        /// Create an expression from an expression token.
        /// </summary>
        /// <param name="et">Expression token to use.</param>
        /// <param name="loadingInfo">The context from which the file was loaded.</param>
        /// <returns>Constructed expression.</returns>
        /// <exception cref="ParseException"></exception>
        internal PSPropertyExpression CreateFromExpressionToken(ExpressionToken et, DatabaseLoadingInfo loadingInfo)
        {
            if (et.isScriptBlock)
            {
                // we cache script blocks from expression tokens
                if (_expressionCache != null)
                {
                    PSPropertyExpression value;
                    if (_expressionCache.TryGetValue(et, out value))
                    {
                        // got a hit on the cache, just return
                        return value;
                    }
                }
                else
                {
                    _expressionCache = new Dictionary<ExpressionToken, PSPropertyExpression>();
                }

                bool isFullyTrusted = false;
                bool isProductCode = false;
                if (loadingInfo != null)
                {
                    isFullyTrusted = loadingInfo.isFullyTrusted;
                    isProductCode = loadingInfo.isProductCode;
                }

                // no hit, we build one and we cache
                ScriptBlock sb = ScriptBlock.CreateDelayParsedScriptBlock(et.expressionValue, isProductCode: isProductCode);
                sb.DebuggerStepThrough = true;

                if (isFullyTrusted)
                {
                    sb.LanguageMode = PSLanguageMode.FullLanguage;
                }

                PSPropertyExpression ex = new PSPropertyExpression(sb);

                _expressionCache.Add(et, ex);

                return ex;
            }

            // we do not cache if it is just a property name
            return new PSPropertyExpression(et.expressionValue);
        }

        private Dictionary<ExpressionToken, PSPropertyExpression> _expressionCache;
    }
}
