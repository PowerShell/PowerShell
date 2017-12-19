/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Globalization;
using System.Reflection;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    /// <summary>
    /// class containing miscellaneous helpers to deal with
    /// PSObject manipulation
    /// </summary>
    internal static class PSObjectHelper
    {
        internal const string ellipses = "...";

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
            return String.Equals(typeNames[1], "System.Enum", StringComparison.Ordinal);
        }

        /// <summary>
        /// WriteError adds a note property called WriteErrorStream to the error
        /// record wrapped in an PSObject and set its value to true. When F and O detects
        /// this note exists and its value is set to true, WriteErrorLine will be used
        /// to emit the error; otherwise, F and O actions are regular.
        /// </summary>
        /// <param name="so"></param>
        /// <returns></returns>
        internal static bool IsWriteErrorStream(PSObject so)
        {
            return IsStreamType(so, "WriteErrorStream");
        }

        /// <summary>
        /// Checks for WriteWarningStream property on object, indicating that
        /// it is a warning stream.  Used by F and O.
        /// </summary>
        /// <param name="so"></param>
        /// <returns></returns>
        internal static bool IsWriteWarningStream(PSObject so)
        {
            return IsStreamType(so, "WriteWarningStream");
        }

        /// <summary>
        /// Checks for WriteVerboseStream property on object, indicating that
        /// it is a verbose stream.  Used by F and O.
        /// </summary>
        /// <param name="so"></param>
        /// <returns></returns>
        internal static bool IsWriteVerboseStream(PSObject so)
        {
            return IsStreamType(so, "WriteVerboseStream");
        }

        /// <summary>
        /// Checks for WriteDebugStream property on object, indicating that
        /// it is a debug stream.  Used by F and O.
        /// </summary>
        /// <param name="so"></param>
        /// <returns></returns>
        internal static bool IsWriteDebugStream(PSObject so)
        {
            return IsStreamType(so, "WriteDebugStream");
        }

        /// <summary>
        /// Checks for WriteInformationStream property on object, indicating that
        /// it is an informational stream.  Used by F and O.
        /// </summary>
        /// <param name="so"></param>
        /// <returns></returns>
        internal static bool IsWriteInformationStream(PSObject so)
        {
            return IsStreamType(so, "WriteInformationStream");
        }

        internal static bool IsStreamType(PSObject so, string streamFlag)
        {
            try
            {
                PSPropertyInfo streamProperty = so.Properties[streamFlag];
                if (streamProperty != null && streamProperty.Value is bool)
                {
                    return (bool)streamProperty.Value;
                }

                return false;
            }
            catch (ExtendedTypeSystemException)
            {
                return false;
            }
        }

        /// <summary>
        /// Retrieve the display name. It looks for a well known property and,
        /// if not found, it uses some heuristics to get a "close" match
        /// </summary>
        /// <param name="target">shell object to process</param>
        /// <param name="expressionFactory">expression factory to create MshExpression</param>
        /// <returns>resolved MshExpression; null if no match was found</returns>
        internal static MshExpression GetDisplayNameExpression(PSObject target, MshExpressionFactory expressionFactory)
        {
            // first try to get the expression from the object (types.ps1xml data)
            MshExpression expressionFromObject = GetDefaultNameExpression(target);
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
                MshExpression ex = new MshExpression(pattern);
                List<MshExpression> exprList = ex.ResolveNames(target);

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
        /// it gets the display name value
        /// </summary>
        /// <param name="target">shell object to process</param>
        /// <param name="expressionFactory">expression factory to create MshExpression</param>
        /// <returns>MshExpressionResult if successful; null otherwise</returns>
        internal static MshExpressionResult GetDisplayName(PSObject target, MshExpressionFactory expressionFactory)
        {
            // get the expression to evaluate
            MshExpression ex = GetDisplayNameExpression(target, expressionFactory);
            if (ex == null)
                return null;

            // evaluate the expression
            List<MshExpressionResult> resList = ex.GetValues(target);

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
        /// <param name="obj">object to extract the IEnumerable from</param>
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

        private static string GetSmartToStringDisplayName(object x, MshExpressionFactory expressionFactory)
        {
            MshExpressionResult r = PSObjectHelper.GetDisplayName(PSObjectHelper.AsPSObject(x), expressionFactory);
            if ((r != null) && (r.Exception == null))
            {
                return PSObjectHelper.AsPSObject(r.Result).ToString();
            }
            else
            {
                return PSObjectHelper.AsPSObject(x).ToString();
            }
        }

        private static string GetObjectName(object x, MshExpressionFactory expressionFactory)
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
                MethodInfo toStringMethod = x.GetType().GetMethod("ToString", PSTypeExtensions.EmptyTypes);
                // TODO:CORECLR double check with CORE CLR that x.GetType() == toStringMethod.ReflectedType
                // Check if the given object "x" implements "toString" method. Do that by comparing "DeclaringType" which 'Gets the class that declares this member' and the object type
                if (toStringMethod.DeclaringType == x.GetType())
                {
                    objName = PSObjectHelper.AsPSObject(x).ToString();
                }
                else
                {
                    MshExpressionResult r = PSObjectHelper.GetDisplayName(PSObjectHelper.AsPSObject(x), expressionFactory);
                    if ((r != null) && (r.Exception == null))
                    {
                        objName = PSObjectHelper.AsPSObject(r.Result).ToString(); ;
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
        /// helper to convert an PSObject into a string
        /// It takes into account enumerations (use display name)
        /// </summary>
        /// <param name="so">shell object to process</param>
        /// <param name="expressionFactory">expression factory to create MshExpression</param>
        /// <param name="enumerationLimit">limit on IEnumerable enumeration</param>
        /// <param name="formatErrorObject">stores errors during string conversion</param>
        /// <returns>string representation</returns>
        internal static string SmartToString(PSObject so, MshExpressionFactory expressionFactory, int enumerationLimit, StringFormatError formatErrorObject)
        {
            if (so == null)
                return "";

            try
            {
                IEnumerable e = PSObjectHelper.GetEnumerable(so);
                if (e != null)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("{");

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
                                        sb.Append(ellipses);
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
                                        sb.Append(ellipses);
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
                    sb.Append("}");
                    return sb.ToString();
                }

                // take care of the case there is no base object
                return so.ToString();
            }
            catch (ExtendedTypeSystemException e)
            {
                // NOTE: we catch all the exceptions, since we do not know
                // what the underlying object access would throw
                if (formatErrorObject != null)
                {
                    formatErrorObject.sourceObject = so;
                    formatErrorObject.exception = e;
                }
                return "";
            }
        }

        private static readonly PSObject s_emptyPSObject = new PSObject("");

        internal static PSObject AsPSObject(object obj)
        {
            return (obj == null) ? s_emptyPSObject : PSObject.AsPSObject(obj);
        }

        /// <summary>
        /// format an object using a provided format string directive
        /// </summary>
        /// <param name="directive">format directive object to use</param>
        /// <param name="val">object to format</param>
        /// <param name="enumerationLimit">limit on IEnumerable enumeration</param>
        /// <param name="formatErrorObject">formatting error object, if present</param>
        /// <param name="expressionFactory">expression factory to create MshExpression</param>
        /// <returns>string representation</returns>
        internal static string FormatField(FieldFormattingDirective directive, object val, int enumerationLimit,
            StringFormatError formatErrorObject, MshExpressionFactory expressionFactory)
        {
            PSObject so = PSObjectHelper.AsPSObject(val);
            if (directive != null && !string.IsNullOrEmpty(directive.formatString))
            {
                // we have a formatting directive, apply it
                // NOTE: with a format directive, we do not make any attempt
                // to deal with IEnumerable
                try
                {
                    // use some heuristics to determine if we have "composite formatting"
                    // 2004/11/16-JonN This is heuristic but should be safe enough
                    if (directive.formatString.Contains("{0") || directive.formatString.Contains("}"))
                    {
                        // we do have it, just use it
                        return String.Format(CultureInfo.CurrentCulture, directive.formatString, so);
                    }
                    // we fall back to the PSObject's IFormattable.ToString()
                    // pass a null IFormatProvider
                    return so.ToString(directive.formatString, null);
                }
                catch (Exception e) // 2004/11/17-JonN This covers exceptions thrown in
                                    // String.Format and PSObject.ToString().
                                    // I think we can swallow these.
                {
                    // NOTE: we catch all the exceptions, since we do not know
                    // what the underlying object access would throw
                    if (formatErrorObject != null)
                    {
                        formatErrorObject.sourceObject = so;
                        formatErrorObject.exception = e;
                        formatErrorObject.formatString = directive.formatString;
                        return "";
                    }
                }
            }
            // we do not have a formatting directive or we failed the formatting (fallback)
            // but we did not report as an error;
            // this call would deal with IEnumerable if the object implements it
            return PSObjectHelper.SmartToString(so, expressionFactory, enumerationLimit, formatErrorObject);
        }

        private static PSMemberSet MaskDeserializedAndGetStandardMembers(PSObject so)
        {
            Diagnostics.Assert(null != so, "Shell Object to process cannot be null");
            var typeNames = so.InternalTypeNames;
            Collection<string> typeNamesWithoutDeserializedPrefix = Deserializer.MaskDeserializationPrefix(typeNames);
            if (null == typeNamesWithoutDeserializedPrefix)
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

        private static List<MshExpression> GetDefaultPropertySet(PSMemberSet standardMembersSet)
        {
            if (null != standardMembersSet)
            {
                PSPropertySet defaultDisplayPropertySet = standardMembersSet.Members[TypeTable.DefaultDisplayPropertySet] as PSPropertySet;
                if (null != defaultDisplayPropertySet)
                {
                    List<MshExpression> retVal = new List<MshExpression>();
                    foreach (string prop in defaultDisplayPropertySet.ReferencedPropertyNames)
                    {
                        if (!string.IsNullOrEmpty(prop))
                        {
                            retVal.Add(new MshExpression(prop));
                        }
                    }
                    return retVal;
                }
            }

            return new List<MshExpression>();
        }

        /// <summary>
        /// helper to retrieve the default property set of a shell object
        /// </summary>
        /// <param name="so">shell object to process</param>
        /// <returns>resolved expression; empty list if not found</returns>
        internal static List<MshExpression> GetDefaultPropertySet(PSObject so)
        {
            List<MshExpression> retVal = GetDefaultPropertySet(so.PSStandardMembers);
            if (retVal.Count == 0)
            {
                retVal = GetDefaultPropertySet(MaskDeserializedAndGetStandardMembers(so));
            }

            return retVal;
        }

        private static MshExpression GetDefaultNameExpression(PSMemberSet standardMembersSet)
        {
            if (null != standardMembersSet)
            {
                PSNoteProperty defaultDisplayProperty = standardMembersSet.Members[TypeTable.DefaultDisplayProperty] as PSNoteProperty;
                if (null != defaultDisplayProperty)
                {
                    string expressionString = defaultDisplayProperty.Value.ToString();
                    if (string.IsNullOrEmpty(expressionString))
                    {
                        // invalid data, the PSObject is empty
                        return null;
                    }
                    else
                    {
                        return new MshExpression(expressionString);
                    }
                }
            }

            return null;
        }

        private static MshExpression GetDefaultNameExpression(PSObject so)
        {
            MshExpression retVal = GetDefaultNameExpression(so.PSStandardMembers) ??
                                   GetDefaultNameExpression(MaskDeserializedAndGetStandardMembers(so));

            return retVal;
        }

        /// <summary>
        /// helper to retrieve the value of an MshExpression and to format it
        /// </summary>
        /// <param name="so">shell object to process</param>
        /// <param name="enumerationLimit">limit on IEnumerable enumeration</param>
        /// <param name="ex">expression to use for retrieval</param>
        /// <param name="directive">format directive to use for formatting</param>
        /// <param name="formatErrorObject"></param>
        /// <param name="expressionFactory">expression factory to create MshExpression</param>
        /// <param name="result"> not null if an error condition arose</param>
        /// <returns>formatted string</returns>
        internal static string GetExpressionDisplayValue(
            PSObject so,
            int enumerationLimit,
            MshExpression ex,
            FieldFormattingDirective directive,
            StringFormatError formatErrorObject,
            MshExpressionFactory expressionFactory,
            out MshExpressionResult result)
        {
            result = null;
            List<MshExpressionResult> resList = ex.GetValues(so);

            if (resList.Count == 0)
            {
                return "";
            }

            result = resList[0];
            if (result.Exception != null)
            {
                return "";
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
            if (null != so)
            {
                try
                {
                    PSPropertyInfo computerNameProperty = so.Properties[RemotingConstants.ComputerNameNoteProperty];
                    PSPropertyInfo showComputerNameProperty = so.Properties[RemotingConstants.ShowComputerNameNoteProperty];

                    // if computer name property exists then this must be a remote object. see
                    // if it can be displayed.
                    if ((null != computerNameProperty) && (null != showComputerNameProperty))
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

    internal sealed class MshExpressionError : FormattingError
    {
        internal MshExpressionResult result;
    }


    internal sealed class StringFormatError : FormattingError
    {
        internal string formatString;
        internal Exception exception;
    }

    internal delegate ScriptBlock CreateScriptBlockFromString(string scriptBlockString);

    /// <summary>
    /// helper class to create MshExpression's from format.ps1xml data structures
    /// </summary>
    internal sealed class MshExpressionFactory
    {
        /// <exception cref="ParseException"></exception>
        internal void VerifyScriptBlockText(string scriptText)
        {
            ScriptBlock.Create(scriptText);
        }

        /// <summary>
        /// create an expression from an expression token
        /// </summary>
        /// <param name="et">expression token to use</param>
        /// <returns>constructed expression</returns>
        /// <exception cref="ParseException"></exception>
        internal MshExpression CreateFromExpressionToken(ExpressionToken et)
        {
            return CreateFromExpressionToken(et, null);
        }

        /// <summary>
        /// create an expression from an expression token
        /// </summary>
        /// <param name="et">expression token to use</param>
        /// <param name="loadingInfo">The context from which the file was loaded</param>
        /// <returns>constructed expression</returns>
        /// <exception cref="ParseException"></exception>
        internal MshExpression CreateFromExpressionToken(ExpressionToken et, DatabaseLoadingInfo loadingInfo)
        {
            if (et.isScriptBlock)
            {
                // we cache script blocks from expression tokens
                if (_expressionCache != null)
                {
                    MshExpression value;
                    if (_expressionCache.TryGetValue(et, out value))
                    {
                        // got a hit on the cache, just return
                        return value;
                    }
                }
                else
                {
                    _expressionCache = new Dictionary<ExpressionToken, MshExpression>();
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

                MshExpression ex = new MshExpression(sb);

                _expressionCache.Add(et, ex);

                return ex;
            }

            // we do not cache if it is just a property name
            return new MshExpression(et.expressionValue);
        }

        private Dictionary<ExpressionToken, MshExpression> _expressionCache;
    }
}

