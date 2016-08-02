/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    /// <summary>
    /// class to hold results
    /// NOTE: we should make it an PSObject eventually
    /// </summary>
    internal class MshExpressionResult
    {
        internal MshExpressionResult(object res, MshExpression re, Exception e)
        {
            Result = res;
            ResolvedExpression = re;
            Exception = e;
        }

        internal object Result { get; } = null;

        internal MshExpression ResolvedExpression { get; } = null;

        internal Exception Exception { get; } = null;
    }


    internal class MshExpression
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="s">expression</param>
        /// <exception cref="ArgumentNullException"></exception>
        internal MshExpression(string s)
            : this(s, false)
        {
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="s">expression</param>
        /// <param name="isResolved"><c>true</c> if no further attempts should be made to resolve wildcards</param>
        /// <exception cref="ArgumentNullException"></exception>
        internal MshExpression(string s, bool isResolved)
        {
            if (string.IsNullOrEmpty(s))
            {
                throw PSTraceSource.NewArgumentNullException("s");
            }
            _stringValue = s;
            _isResolved = isResolved;
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="scriptBlock"></param>
        /// <exception cref="ArgumentNullException"></exception>
        internal MshExpression(ScriptBlock scriptBlock)
        {
            if (scriptBlock == null)
            {
                throw PSTraceSource.NewArgumentNullException("scriptBlock");
            }
            Script = scriptBlock;
        }

        public ScriptBlock Script { get; } = null;

        public override string ToString()
        {
            if (Script != null)
                return Script.ToString();

            return _stringValue;
        }

        internal List<MshExpression> ResolveNames(PSObject target)
        {
            return ResolveNames(target, true);
        }

        internal bool HasWildCardCharacters
        {
            get
            {
                if (Script != null)
                    return false;
                return WildcardPattern.ContainsWildcardCharacters(_stringValue);
            }
        }

        internal List<MshExpression> ResolveNames(PSObject target, bool expand)
        {
            List<MshExpression> retVal = new List<MshExpression>();

            if (_isResolved)
            {
                retVal.Add(this);
                return retVal;
            }

            if (Script != null)
            {
                // script block, just add it to the list and be done
                MshExpression ex = new MshExpression(Script);

                ex._isResolved = true;
                retVal.Add(ex);
                return retVal;
            }

            // we have a string value
            IEnumerable<PSMemberInfo> members = null;
            if (HasWildCardCharacters)
            {
                // get the members first: this will expand the globbing on each parameter
                members = target.Members.Match(_stringValue,
                                            PSMemberTypes.Properties | PSMemberTypes.PropertySet);
            }
            else
            {
                // we have no globbing: try an exact match, because this is quicker.
                PSMemberInfo x = target.Members[_stringValue];

                List<PSMemberInfo> temp = new List<PSMemberInfo>();
                if (x != null)
                {
                    temp.Add(x);
                }
                members = temp;
            }

            // we now have a list of members, we have to expand property sets
            // and remove duplicates
            List<PSMemberInfo> temporaryMemberList = new List<PSMemberInfo>();

            foreach (PSMemberInfo member in members)
            {
                // it can be a property set
                PSPropertySet propertySet = member as PSPropertySet;
                if (propertySet != null)
                {
                    if (expand)
                    {
                        // NOTE: we expand the property set under the
                        // assumption that it contains property names that
                        // do not require any further expansion
                        Collection<string> references = propertySet.ReferencedPropertyNames;

                        for (int j = 0; j < references.Count; j++)
                        {
                            ReadOnlyPSMemberInfoCollection<PSPropertyInfo> propertyMembers =
                                                target.Properties.Match(references[j]);
                            for (int jj = 0; jj < propertyMembers.Count; jj++)
                            {
                                temporaryMemberList.Add(propertyMembers[jj]);
                            }
                        }
                    }
                    continue;
                }
                // it can be a property
                if (member is PSPropertyInfo)
                {
                    temporaryMemberList.Add(member);
                }
            }

            Hashtable hash = new Hashtable();

            // build the list of unique values: remove the possible duplicates
            // from property set expansion
            foreach (PSMemberInfo m in temporaryMemberList)
            {
                if (!hash.ContainsKey(m.Name))
                {
                    MshExpression ex = new MshExpression(m.Name);

                    ex._isResolved = true;
                    retVal.Add(ex);
                    hash.Add(m.Name, null);
                }
            }

            return retVal;
        }

        internal List<MshExpressionResult> GetValues(PSObject target)
        {
            return GetValues(target, true, true);
        }

        internal List<MshExpressionResult> GetValues(PSObject target, bool expand, bool eatExceptions)
        {
            List<MshExpressionResult> retVal = new List<MshExpressionResult>();

            // process the script case
            if (Script != null)
            {
                MshExpression scriptExpression = new MshExpression(Script);
                MshExpressionResult r = scriptExpression.GetValue(target, eatExceptions);
                retVal.Add(r);
                return retVal;
            }

            // process the expression
            List<MshExpression> resolvedExpressionList = this.ResolveNames(target, expand);

            foreach (MshExpression re in resolvedExpressionList)
            {
                MshExpressionResult r = re.GetValue(target, eatExceptions);
                retVal.Add(r);
            }

            return retVal;
        }

        #region Private Members

        private MshExpressionResult GetValue(PSObject target, bool eatExceptions)
        {
            try
            {
                object result;

                if (Script != null)
                {
                    result = Script.DoInvokeReturnAsIs(
                        useLocalScope: true,
                        errorHandlingBehavior: ScriptBlock.ErrorHandlingBehavior.WriteToExternalErrorPipe,
                        dollarUnder: target,
                        input: AutomationNull.Value,
                        scriptThis: AutomationNull.Value,
                        args: Utils.EmptyArray<object>());
                }
                else
                {
                    PSMemberInfo member = target.Properties[_stringValue];
                    if (member == null)
                    {
                        return new MshExpressionResult(null, this, null);
                    }
                    result = member.Value;
                }

                return new MshExpressionResult(result, this, null);
            }
            catch (RuntimeException e)
            {
                if (eatExceptions)
                {
                    return new MshExpressionResult(null, this, e);
                }
                else
                {
                    throw;
                }
            }
        }


        // private members
        private string _stringValue;
        private bool _isResolved = false;

        #endregion Private Members
    }
}

