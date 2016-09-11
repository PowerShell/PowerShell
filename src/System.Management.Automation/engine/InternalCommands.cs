/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Diagnostics.CodeAnalysis;
using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{
    #region Built-in cmdlets that are used by or require direct access to the engine.

    /// <summary>
    /// Implements a cmdlet that applies a script block
    /// to each element of the pipeline.
    /// </summary>
    [SuppressMessage("Microsoft.PowerShell", "PS1012:CallShouldProcessOnlyIfDeclaringSupport")]
    [Cmdlet("ForEach", "Object", SupportsShouldProcess = true, DefaultParameterSetName = "ScriptBlockSet",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113300", RemotingCapability = RemotingCapability.None)]
    public sealed class ForEachObjectCommand : PSCmdlet
    {
        /// <summary>
        /// This parameter specifies the current pipeline object 
        /// </summary>
        [Parameter(ValueFromPipeline = true, ParameterSetName = "ScriptBlockSet")]
        [Parameter(ValueFromPipeline = true, ParameterSetName = "PropertyAndMethodSet")]
        public PSObject InputObject
        {
            set { _inputObject = value; }
            get { return _inputObject; }
        }
        private PSObject _inputObject = AutomationNull.Value;

        #region ScriptBlockSet

        private List<ScriptBlock> _scripts = new List<ScriptBlock>();

        /// <summary>
        /// The script block to apply in begin processing
        /// </summary>
        [Parameter(ParameterSetName = "ScriptBlockSet")]
        public ScriptBlock Begin
        {
            set
            {
                _scripts.Insert(0, value);
            }
            get
            {
                return null;
            }
        }

        /// <summary>
        /// The script block to apply
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ScriptBlockSet")]
        [AllowNull]
        [AllowEmptyCollection]
        public ScriptBlock[] Process
        {
            set
            {
                if (value == null)
                    _scripts.Add(null);
                else
                    _scripts.AddRange(value);
            }
            get
            {
                return null;
            }
        }

        private ScriptBlock _endScript;
        private bool _setEndScript;
        /// <summary>
        /// The script block to apply in complete processing
        /// </summary>
        [Parameter(ParameterSetName = "ScriptBlockSet")]
        public ScriptBlock End
        {
            set
            {
                _endScript = value;
                _setEndScript = true;
            }
            get
            {
                return _endScript;
            }
        }

        /// <summary>
        /// The remaining script blocks to apply
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        [Parameter(ParameterSetName = "ScriptBlockSet", ValueFromRemainingArguments = true)]
        [AllowNull]
        [AllowEmptyCollection]
        public ScriptBlock[] RemainingScripts
        {
            set
            {
                if (value == null)
                    _scripts.Add(null);
                else
                    _scripts.AddRange(value);
            }
            get { return null; }
        }

        private int _start, _end;

        #endregion ScriptBlockSet


        #region PropertyAndMethodSet

        /// <summary>
        /// The property or method name
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "PropertyAndMethodSet")]
        [ValidateNotNullOrEmpty]
        public string MemberName
        {
            set { _propertyOrMethodName = value; }
            get { return _propertyOrMethodName; }
        }
        private string _propertyOrMethodName;
        private string _targetString;

        /// <summary>
        /// The arguments passed to a method invocation
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        [Parameter(ParameterSetName = "PropertyAndMethodSet", ValueFromRemainingArguments = true)]
        [Alias("Args")]
        public object[] ArgumentList
        {
            set { _arguments = value; }
            get { return _arguments; }
        }
        private object[] _arguments;

        #endregion PropertyAndMethodSet


        /// <summary>
        /// Execute the begin scriptblock at the start of processing
        /// </summary>
        /// <exception cref="SessionStateOverflowException">
        /// The maximum scope depth would be exceeded
        /// </exception>
        /// <exception cref="ParseException">could not parse script</exception>
        /// <exception cref="RuntimeException">see Pipeline.Invoke</exception>
        /// <exception cref="ParameterBindingException">see Pipeline.Invoke</exception>
        protected override void BeginProcessing()
        {
            Dbg.Assert(ParameterSetName == "ScriptBlockSet" || ParameterSetName == "PropertyAndMethodSet", "ParameterSetName is neither 'ScriptBlockSet' nor 'PropertyAndMethodSet'");

            if (ParameterSetName != "ScriptBlockSet") return;

            // Win8: 176403: ScriptCmdlets sets the global WhatIf and Confirm preferences
            // This effects the new W8 foreach-object cmdlet with -whatif and -confirm
            // implemented. -whatif and -confirm needed only for PropertyAndMethodSet
            // parameter set. So erring out in cases where these are used with ScriptBlockSet.
            // Not using MshCommandRuntime, as those variables will be affected by ScriptCmdlet
            // infrastructure (wherein ScriptCmdlet modifies the global preferences).
            Dictionary<string, object> psBoundParameters = this.MyInvocation.BoundParameters;
            if (psBoundParameters != null)
            {
                SwitchParameter whatIf = false;
                SwitchParameter confirm = false;

                object argument;
                if (psBoundParameters.TryGetValue("whatif", out argument))
                {
                    whatIf = (SwitchParameter)argument;
                }

                if (psBoundParameters.TryGetValue("confirm", out argument))
                {
                    confirm = (SwitchParameter)argument;
                }

                if (whatIf || confirm)
                {
                    string message = InternalCommandStrings.NoShouldProcessForScriptBlockSet;

                    ErrorRecord errorRecord = new ErrorRecord(
                        new InvalidOperationException(message),
                        "NoShouldProcessForScriptBlockSet",
                        ErrorCategory.InvalidOperation,
                        null);
                    ThrowTerminatingError(errorRecord);
                }
            }

            // Calculate the start and end indexes for the processRecord script blocks
            _end = _scripts.Count;
            _start = _scripts.Count > 1 ? 1 : 0;

            // and set the end script if it wasn't explicitly set with a named parameter.
            if (!_setEndScript)
            {
                if (_scripts.Count > 2)
                {
                    _end = _scripts.Count - 1;
                    _endScript = _scripts[_end];
                }
            }

            // only process the start script if there is more than one script...
            if (_end < 2)
                return;

            if (_scripts[0] == null)
                return;

            var emptyArray = Utils.EmptyArray<object>();
            _scripts[0].InvokeUsingCmdlet(
                contextCmdlet: this,
                useLocalScope: false,
                errorHandlingBehavior: ScriptBlock.ErrorHandlingBehavior.WriteToCurrentErrorPipe,
                dollarUnder: AutomationNull.Value,
                input: emptyArray,
                scriptThis: AutomationNull.Value,
                args: emptyArray);
        }

        /// <summary>
        /// Execute the processing script blocks on the current pipeline object
        /// which is passed as it's only parameter.
        /// </summary>
        /// <exception cref="SessionStateOverflowException">
        /// The maximum scope depth would be exceeded
        /// </exception>
        /// <exception cref="ParseException">could not parse script</exception>
        /// <exception cref="RuntimeException">see Pipeline.Invoke</exception>
        /// <exception cref="ParameterBindingException">see Pipeline.Invoke</exception>
        protected override void ProcessRecord()
        {
            Dbg.Assert(ParameterSetName == "ScriptBlockSet" || ParameterSetName == "PropertyAndMethodSet", "ParameterSetName is neither 'ScriptBlockSet' nor 'PropertyAndMethodSet'");

            switch (ParameterSetName)
            {
                case "ScriptBlockSet":
                    for (int i = _start; i < _end; i++)
                    {
                        // Only execute scripts that aren't null. This isn't treated as an error
                        // because it allows you to parameterize a command - for example you might allow
                        // for actions before and after the main processing script. They could be null
                        // by default and therefore ignored then filled in later...
                        if (_scripts[i] != null)
                        {
                            _scripts[i].InvokeUsingCmdlet(
                                contextCmdlet: this,
                                useLocalScope: false,
                                errorHandlingBehavior: ScriptBlock.ErrorHandlingBehavior.WriteToCurrentErrorPipe,
                                dollarUnder: InputObject,
                                input: new object[] { InputObject },
                                scriptThis: AutomationNull.Value,
                                args: Utils.EmptyArray<object>());
                        }
                    }
                    break;
                case "PropertyAndMethodSet":

                    _targetString = String.Format(CultureInfo.InvariantCulture, InternalCommandStrings.ForEachObjectTarget, GetStringRepresentation(InputObject));

                    if (LanguagePrimitives.IsNull(InputObject))
                    {
                        if (_arguments != null && _arguments.Length > 0)
                        {
                            WriteError(GenerateNameParameterError("InputObject", ParserStrings.InvokeMethodOnNull,
                                                                  "InvokeMethodOnNull", _inputObject));
                        }
                        else
                        {
                            // should process
                            string propertyAction = String.Format(CultureInfo.InvariantCulture,
                                InternalCommandStrings.ForEachObjectPropertyAction, _propertyOrMethodName);

                            if (ShouldProcess(_targetString, propertyAction))
                            {
                                if (Context.IsStrictVersion(2))
                                {
                                    WriteError(GenerateNameParameterError("InputObject", InternalCommandStrings.InputObjectIsNull,
                                                                          "InputObjectIsNull", _inputObject));
                                }
                                else
                                {
                                    // we write null out because:
                                    // PS C:\> “$null | ForEach-object {$_.aa} | ForEach-Object {$_ + 3}”
                                    // 3
                                    // so we also want
                                    // PS C:\> “$null | ForEach-object aa | ForEach-Object {$_ + 3}”
                                    // 3
                                    // But if we don’t write anything to the pipeline when _inputObject is null,
                                    // the result 3 will not be generated.
                                    WriteObject(null);
                                }
                            }
                        }
                        return;
                    }

                    ErrorRecord errorRecord = null;

                    // if args exist, this is explicitly a method invocation
                    if (_arguments != null && _arguments.Length > 0)
                    {
                        MethodCallWithArguments();
                    }
                    // no arg provided
                    else
                    {
                        // if inputObject is of IDictionary, get the value
                        if (GetValueFromIDictionaryInput()) { return; }

                        PSMemberInfo member = null;
                        if (WildcardPattern.ContainsWildcardCharacters(_propertyOrMethodName))
                        {
                            // get the matched member(s)
                            ReadOnlyPSMemberInfoCollection<PSMemberInfo> members =
                                _inputObject.Members.Match(_propertyOrMethodName, PSMemberTypes.All);
                            Dbg.Assert(members != null, "The return value of Members.Match should never be null");

                            if (members.Count > 1)
                            {
                                // write error record: property method ambiguous
                                StringBuilder possibleMatches = new StringBuilder();
                                foreach (PSMemberInfo item in members)
                                {
                                    possibleMatches.AppendFormat(CultureInfo.InvariantCulture, " {0}", item.Name);
                                }

                                WriteError(GenerateNameParameterError("Name", InternalCommandStrings.AmbiguousPropertyOrMethodName,
                                                                      "AmbiguousPropertyOrMethodName", _inputObject,
                                                                      _propertyOrMethodName, possibleMatches));
                                return;
                            }
                            if (members.Count == 1)
                            {
                                member = members[0];
                            }
                        }
                        else
                        {
                            member = _inputObject.Members[_propertyOrMethodName];
                        }

                        if (member == null)
                        {
                            errorRecord = GenerateNameParameterError("Name", InternalCommandStrings.PropertyOrMethodNotFound,
                                                                     "PropertyOrMethodNotFound", _inputObject,
                                                                     _propertyOrMethodName);
                        }
                        else
                        {
                            // member is a method
                            if (member is PSMethodInfo)
                            {
                                // first we check if the member is a ParameterizedProperty
                                PSParameterizedProperty targetParameterizedProperty = member as PSParameterizedProperty;
                                if (targetParameterizedProperty != null)
                                {
                                    // should process
                                    string propertyAction = String.Format(CultureInfo.InvariantCulture,
                                        InternalCommandStrings.ForEachObjectPropertyAction, targetParameterizedProperty.Name);

                                    // ParameterizedProperty always take parameters, so we output the member.Value directly
                                    if (ShouldProcess(_targetString, propertyAction))
                                    {
                                        WriteObject(member.Value);
                                    }
                                    return;
                                }

                                PSMethodInfo targetMethod = member as PSMethodInfo;
                                Dbg.Assert(targetMethod != null, "targetMethod should not be null here.");
                                try
                                {
                                    // should process
                                    string methodAction = String.Format(CultureInfo.InvariantCulture,
                                        InternalCommandStrings.ForEachObjectMethodActionWithoutArguments, targetMethod.Name);

                                    if (ShouldProcess(_targetString, methodAction))
                                    {
                                        if (!BlockMethodInLanguageMode(InputObject))
                                        {
                                            object result = targetMethod.Invoke(Utils.EmptyArray<object>());
                                            WriteToPipelineWithUnrolling(result);
                                        }
                                    }
                                }
                                catch (PipelineStoppedException)
                                {
                                    // PipelineStoppedException can be caused by select-object
                                    throw;
                                }
                                catch (Exception ex)
                                {
                                    CommandProcessorBase.CheckForSevereException(ex);
                                    MethodException mex = ex as MethodException;
                                    if (mex != null && mex.ErrorRecord != null && mex.ErrorRecord.FullyQualifiedErrorId == "MethodCountCouldNotFindBest")
                                    {
                                        WriteObject(targetMethod.Value);
                                    }
                                    else
                                    {
                                        WriteError(new ErrorRecord(ex, "MethodInvocationError", ErrorCategory.InvalidOperation, _inputObject));
                                    }
                                }
                            }
                            // member is a property
                            else
                            {
                                // should process
                                string propertyAction = String.Format(CultureInfo.InvariantCulture,
                                    InternalCommandStrings.ForEachObjectPropertyAction, member.Name);

                                if (ShouldProcess(_targetString, propertyAction))
                                {
                                    try
                                    {
                                        WriteToPipelineWithUnrolling(member.Value);
                                    }
                                    catch (TerminateException) // The debugger is terminating the execution
                                    {
                                        throw;
                                    }
                                    catch (MethodException)
                                    {
                                        throw;
                                    }
                                    catch (PipelineStoppedException)
                                    {
                                        // PipelineStoppedException can be caused by select-object
                                        throw;
                                    }
                                    catch (Exception ex)
                                    {
                                        // When the property is not gettable or it throws an exception.
                                        // e.g. when trying to access an assembly's location property, since dynamic assemblies are not backed up by a file,
                                        // an exception will be thrown when accessing its location property. In this case, return null.
                                        CommandProcessorBase.CheckForSevereException(ex);
                                        WriteObject(null);
                                    }
                                }
                            } // end of member is a property
                        } // member is not null
                    } // no args provided

                    if (errorRecord != null)
                    {
                        string propertyAction = String.Format(CultureInfo.InvariantCulture,
                            InternalCommandStrings.ForEachObjectPropertyAction, _propertyOrMethodName);

                        if (ShouldProcess(_targetString, propertyAction))
                        {
                            if (Context.IsStrictVersion(2))
                            {
                                WriteError(errorRecord);
                            }
                            else
                            {
                                // we write null out because:
                                // PS C:\> "string" | ForEach-Object {$_.aa} | ForEach-Object {$_ + 3}
                                // 3
                                // so we also want
                                // PS C:\> "string" | ForEach-Object aa | ForEach-Object {$_ + 3}
                                // 3
                                // But if we don’t write anything to the pipeline when no member is found,
                                // the result 3 will not be generated.
                                WriteObject(null);
                            }
                        }
                    }

                    break;
            }
        }

        /// <summary>
        /// Do method invocation with arguments
        /// </summary>
        private void MethodCallWithArguments()
        {
            // resolve the name
            ReadOnlyPSMemberInfoCollection<PSMemberInfo> methods =
                _inputObject.Members.Match(_propertyOrMethodName,
                                           PSMemberTypes.Methods | PSMemberTypes.ParameterizedProperty);

            Dbg.Assert(methods != null, "The return value of Members.Match should never be null.");
            if (methods.Count > 1)
            {
                // write error record: method ambiguous
                StringBuilder possibleMatches = new StringBuilder();
                foreach (PSMemberInfo item in methods)
                {
                    possibleMatches.AppendFormat(CultureInfo.InvariantCulture, " {0}", item.Name);
                }
                WriteError(GenerateNameParameterError("Name", InternalCommandStrings.AmbiguousMethodName,
                                                      "AmbiguousMethodName", _inputObject,
                                                      _propertyOrMethodName, possibleMatches));
            }
            else if (methods.Count == 0 || !(methods[0] is PSMethodInfo))
            {
                // write error record: method no found
                WriteError(GenerateNameParameterError("Name", InternalCommandStrings.MethodNotFound,
                                                      "MethodNotFound", _inputObject, _propertyOrMethodName));
            }
            else
            {
                PSMethodInfo targetMethod = methods[0] as PSMethodInfo;
                Dbg.Assert(targetMethod != null, "targetMethod should not be null here.");

                // should process
                StringBuilder arglist = new StringBuilder(GetStringRepresentation(_arguments[0]));
                for (int i = 1; i < _arguments.Length; i++)
                {
                    arglist.AppendFormat(CultureInfo.InvariantCulture, ", {0}", GetStringRepresentation(_arguments[i]));
                }
                string methodAction = String.Format(CultureInfo.InvariantCulture,
                    InternalCommandStrings.ForEachObjectMethodActionWithArguments,
                    targetMethod.Name, arglist);

                try
                {
                    if (ShouldProcess(_targetString, methodAction))
                    {
                        if (!BlockMethodInLanguageMode(InputObject))
                        {
                            object result = targetMethod.Invoke(_arguments);
                            WriteToPipelineWithUnrolling(result);
                        }
                    }
                }
                catch (PipelineStoppedException)
                {
                    // PipelineStoppedException can be caused by select-object
                    throw;
                }
                catch (Exception ex)
                {
                    CommandProcessorBase.CheckForSevereException(ex);
                    WriteError(new ErrorRecord(ex, "MethodInvocationError", ErrorCategory.InvalidOperation, _inputObject));
                }
            }
        }

        /// <summary>
        /// Get the string representation of the passed-in object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private static string GetStringRepresentation(object obj)
        {
            string objInString;
            try
            {
                // The "ToString()" method could throw an exception
                objInString = LanguagePrimitives.IsNull(obj) ? "null" : obj.ToString();
            }
            catch (Exception e)
            {
                CommandProcessorBase.CheckForSevereException(e);
                objInString = null;
            }

            if (String.IsNullOrEmpty(objInString))
            {
                var psobj = obj as PSObject;
                objInString = psobj != null ? psobj.BaseObject.GetType().FullName : obj.GetType().FullName;
            }

            return objInString;
        }

        /// <summary>
        /// Get the value by taking _propertyOrMethodName as the key, if the 
        /// input object is a IDictionary.
        /// </summary>
        /// <returns></returns>
        private bool GetValueFromIDictionaryInput()
        {
            object target = PSObject.Base(_inputObject);
            IDictionary hash = target as IDictionary;

            try
            {
                if (hash != null && hash.Contains(_propertyOrMethodName))
                {
                    string keyAction = String.Format(CultureInfo.InvariantCulture,
                            InternalCommandStrings.ForEachObjectKeyAction, _propertyOrMethodName);
                    if (ShouldProcess(_targetString, keyAction))
                    {
                        object result = hash[_propertyOrMethodName];
                        WriteToPipelineWithUnrolling(result);
                    }
                    return true;
                }
            }
            catch (InvalidOperationException)
            {
                // Ignore invalid operation exception, it can happen if the dictionary
                // has keys that can't be compared to property.
            }
            return false;
        }

        /// <summary>
        /// Unroll the object to be output. If it's of type IEnumerator, unroll and output it 
        /// by calling WriteOutIEnumerator. If it's not, unroll and output it by calling WriteObject(obj, true)
        /// </summary>
        /// <param name="obj"></param>
        private void WriteToPipelineWithUnrolling(object obj)
        {
            IEnumerator objAsEnumerator = LanguagePrimitives.GetEnumerator(obj);
            if (objAsEnumerator != null)
            {
                WriteOutIEnumerator(objAsEnumerator);
            }
            else
            {
                WriteObject(obj, true);
            }
        }

        /// <summary>
        /// Unroll an IEnumerator and output all entries.
        /// </summary>
        /// <param name="list"></param>
        private void WriteOutIEnumerator(IEnumerator list)
        {
            if (list != null)
            {
                while (ParserOps.MoveNext(this.Context, null, list))
                {
                    object val = ParserOps.Current(null, list);

                    if (val != AutomationNull.Value)
                    {
                        WriteObject(val);
                    }
                }
            }
        }

        /// <summary>
        /// Check if the language mode is the restrictedLanguageMode before invoking a method. 
        /// Write out error message and return true if we are in restrictedLanguageMode.
        /// </summary>
        /// <returns></returns>
        private bool BlockMethodInLanguageMode(Object inputObject)
        {
            // Cannot invoke a method in RestrictedLanguage mode
            if (Context.LanguageMode == PSLanguageMode.RestrictedLanguage)
            {
                PSInvalidOperationException exception =
                    new PSInvalidOperationException(InternalCommandStrings.NoMethodInvocationInRestrictedLanguageMode);

                WriteError(new ErrorRecord(exception, "NoMethodInvocationInRestrictedLanguageMode", ErrorCategory.InvalidOperation, null));
                return true;
            }

            // Cannot invoke certain methods in ConstrainedLanguage mode
            if (Context.LanguageMode == PSLanguageMode.ConstrainedLanguage)
            {
                Object baseObject = PSObject.Base(inputObject);

                if (!CoreTypes.Contains(baseObject.GetType()))
                {
                    PSInvalidOperationException exception =
                        new PSInvalidOperationException(ParserStrings.InvokeMethodConstrainedLanguage);

                    WriteError(new ErrorRecord(exception, "MethodInvocationNotSupportedInConstrainedLanguage", ErrorCategory.InvalidOperation, null));
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Generate the appropriate error record
        /// </summary>
        /// <param name="paraName"></param>
        /// <param name="resourceString"></param>
        /// <param name="errorId"></param>
        /// <param name="target"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        internal static ErrorRecord GenerateNameParameterError(string paraName, string resourceString, string errorId, object target, params object[] args)
        {
            string message;
            if (null == args || 0 == args.Length)
            {
                // Don't format in case the string contains literal curly braces
                message = resourceString;
            }
            else
            {
                message = StringUtil.Format(resourceString, args);
            }

            if (String.IsNullOrEmpty(message))
            {
                Dbg.Assert(false, "Could not load text for error record '" + errorId + "'");
            }

            ErrorRecord errorRecord = new ErrorRecord(
                new PSArgumentException(message, paraName),
                errorId,
                ErrorCategory.InvalidArgument,
                target);

            return errorRecord;
        }


        /// <summary>
        /// Execute the end scriptblock when the pipeline is complete
        /// </summary>
        /// <exception cref="SessionStateOverflowException">
        /// The maximum scope depth would be exceeded
        /// </exception>
        /// <exception cref="ParseException">could not parse script</exception>
        /// <exception cref="RuntimeException">see Pipeline.Invoke</exception>
        /// <exception cref="ParameterBindingException">see Pipeline.Invoke</exception>
        protected override void EndProcessing()
        {
            if (ParameterSetName != "ScriptBlockSet") return;

            if (_endScript == null)
                return;

            var emptyArray = Utils.EmptyArray<object>();
            _endScript.InvokeUsingCmdlet(
                contextCmdlet: this,
                useLocalScope: false,
                errorHandlingBehavior: ScriptBlock.ErrorHandlingBehavior.WriteToCurrentErrorPipe,
                dollarUnder: AutomationNull.Value,
                input: emptyArray,
                scriptThis: AutomationNull.Value,
                args: emptyArray);
        }
    }

    /// <summary>
    /// Implements a cmdlet that applys a script block
    /// to each element of the pipeline. If the result of that
    /// application is true, then the current pipeline object
    /// is passed on, otherwise it is dropped.
    /// </summary>
    [Cmdlet("Where", "Object", DefaultParameterSetName = "EqualSet",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113423", RemotingCapability = RemotingCapability.None)]
    public sealed class WhereObjectCommand : PSCmdlet
    {
        /// <summary>
        /// This parameter specifies the current pipeline object 
        /// </summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject InputObject
        {
            set { _inputObject = value; }
            get { return _inputObject; }
        }
        private PSObject _inputObject = AutomationNull.Value;

        private ScriptBlock _script;
        /// <summary>
        /// The script block to apply
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ScriptBlockSet")]
        public ScriptBlock FilterScript
        {
            set
            {
                _script = value;
            }
            get
            {
                return _script;
            }
        }


        private string _property;
        /// <summary>
        /// The property to retrieve value
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "EqualSet")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "CaseSensitiveEqualSet")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "NotEqualSet")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "CaseSensitiveNotEqualSet")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "GreaterThanSet")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "CaseSensitiveGreaterThanSet")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "LessThanSet")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "CaseSensitiveLessThanSet")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "GreaterOrEqualSet")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "CaseSensitiveGreaterOrEqualSet")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "LessOrEqualSet")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "CaseSensitiveLessOrEqualSet")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "LikeSet")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "CaseSensitiveLikeSet")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "NotLikeSet")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "CaseSensitiveNotLikeSet")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "MatchSet")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "CaseSensitiveMatchSet")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "NotMatchSet")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "CaseSensitiveNotMatchSet")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ContainsSet")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "CaseSensitiveContainsSet")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "NotContainsSet")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "CaseSensitiveNotContainsSet")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "InSet")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "CaseSensitiveInSet")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "NotInSet")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "CaseSensitiveNotInSet")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "IsSet")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "IsNotSet")]
        [ValidateNotNullOrEmpty]
        public string Property
        {
            set { _property = value; }
            get { return _property; }
        }


        private object _convertedValue;
        private object _value = true;
        private bool _valueNotSpecified = true;
        /// <summary>
        /// The value to compare against
        /// </summary>
        [Parameter(Position = 1, ParameterSetName = "EqualSet")]
        [Parameter(Position = 1, ParameterSetName = "CaseSensitiveEqualSet")]
        [Parameter(Position = 1, ParameterSetName = "NotEqualSet")]
        [Parameter(Position = 1, ParameterSetName = "CaseSensitiveNotEqualSet")]
        [Parameter(Position = 1, ParameterSetName = "GreaterThanSet")]
        [Parameter(Position = 1, ParameterSetName = "CaseSensitiveGreaterThanSet")]
        [Parameter(Position = 1, ParameterSetName = "LessThanSet")]
        [Parameter(Position = 1, ParameterSetName = "CaseSensitiveLessThanSet")]
        [Parameter(Position = 1, ParameterSetName = "GreaterOrEqualSet")]
        [Parameter(Position = 1, ParameterSetName = "CaseSensitiveGreaterOrEqualSet")]
        [Parameter(Position = 1, ParameterSetName = "LessOrEqualSet")]
        [Parameter(Position = 1, ParameterSetName = "CaseSensitiveLessOrEqualSet")]
        [Parameter(Position = 1, ParameterSetName = "LikeSet")]
        [Parameter(Position = 1, ParameterSetName = "CaseSensitiveLikeSet")]
        [Parameter(Position = 1, ParameterSetName = "NotLikeSet")]
        [Parameter(Position = 1, ParameterSetName = "CaseSensitiveNotLikeSet")]
        [Parameter(Position = 1, ParameterSetName = "MatchSet")]
        [Parameter(Position = 1, ParameterSetName = "CaseSensitiveMatchSet")]
        [Parameter(Position = 1, ParameterSetName = "NotMatchSet")]
        [Parameter(Position = 1, ParameterSetName = "CaseSensitiveNotMatchSet")]
        [Parameter(Position = 1, ParameterSetName = "ContainsSet")]
        [Parameter(Position = 1, ParameterSetName = "CaseSensitiveContainsSet")]
        [Parameter(Position = 1, ParameterSetName = "NotContainsSet")]
        [Parameter(Position = 1, ParameterSetName = "CaseSensitiveNotContainsSet")]
        [Parameter(Position = 1, ParameterSetName = "InSet")]
        [Parameter(Position = 1, ParameterSetName = "CaseSensitiveInSet")]
        [Parameter(Position = 1, ParameterSetName = "NotInSet")]
        [Parameter(Position = 1, ParameterSetName = "CaseSensitiveNotInSet")]
        [Parameter(Position = 1, ParameterSetName = "IsSet")]
        [Parameter(Position = 1, ParameterSetName = "IsNotSet")]
        public object Value
        {
            set
            {
                _value = value;
                _valueNotSpecified = false;
            }
            get { return _value; }
        }

        #region binary operator parameters

        private TokenKind _binaryOperator = TokenKind.Ieq;

        // set to false if the user specified "-EQ" in the command line.
        // remain to be true if "EqualSet" is chosen by default.
        private bool _forceBooleanEvaluation = true;

        /// <summary>
        /// Binary operator -Equal
        /// It's the default parameter set, so -EQ is not mandatory.
        /// </summary>
        [Parameter(ParameterSetName = "EqualSet")]
        [Alias("IEQ")]
        public SwitchParameter EQ
        {
            set
            {
                _binaryOperator = TokenKind.Ieq;
                _forceBooleanEvaluation = false;
            }
            get { return _binaryOperator == TokenKind.Ieq; }
        }

        /// <summary>
        /// Case sensitive binary operator -ceq
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CaseSensitiveEqualSet")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "CEQ")]
        public SwitchParameter CEQ
        {
            set { _binaryOperator = TokenKind.Ceq; }
            get { return _binaryOperator == TokenKind.Ceq; }
        }

        /// <summary>
        /// Binary operator -NotEqual
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "NotEqualSet")]
        [Alias("INE")]
        public SwitchParameter NE
        {
            set { _binaryOperator = TokenKind.Ine; }
            get { return _binaryOperator == TokenKind.Ine; }
        }

        /// <summary>
        /// Case sensitive binary operator -cne
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CaseSensitiveNotEqualSet")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "CNE")]
        public SwitchParameter CNE
        {
            set { _binaryOperator = TokenKind.Cne; }
            get { return _binaryOperator == TokenKind.Cne; }
        }

        /// <summary>
        /// Binary operator -GreaterThan
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "GreaterThanSet")]
        [Alias("IGT")]
        public SwitchParameter GT
        {
            set { _binaryOperator = TokenKind.Igt; }
            get { return _binaryOperator == TokenKind.Igt; }
        }

        /// <summary>
        /// Case sensitive binary operator -cgt
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CaseSensitiveGreaterThanSet")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "CGT")]
        public SwitchParameter CGT
        {
            set { _binaryOperator = TokenKind.Cgt; }
            get { return _binaryOperator == TokenKind.Cgt; }
        }

        /// <summary>
        /// Binary operator -LessThan
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "LessThanSet")]
        [Alias("ILT")]
        public SwitchParameter LT
        {
            set { _binaryOperator = _binaryOperator = TokenKind.Ilt; }
            get { return _binaryOperator == TokenKind.Ilt; }
        }

        /// <summary>
        /// Case sensitive binary operator -clt
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CaseSensitiveLessThanSet")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "CLT")]
        public SwitchParameter CLT
        {
            set { _binaryOperator = TokenKind.Clt; }
            get { return _binaryOperator == TokenKind.Clt; }
        }

        /// <summary>
        /// Binary operator -GreaterOrEqual
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "GreaterOrEqualSet")]
        [Alias("IGE")]
        public SwitchParameter GE
        {
            set { _binaryOperator = TokenKind.Ige; }
            get { return _binaryOperator == TokenKind.Ige; }
        }

        /// <summary>
        /// Case sensitive binary operator -cge
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CaseSensitiveGreaterOrEqualSet")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "CGE")]
        public SwitchParameter CGE
        {
            set { _binaryOperator = TokenKind.Cge; }
            get { return _binaryOperator == TokenKind.Cge; }
        }

        /// <summary>
        /// Binary operator -LessOrEqual
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "LessOrEqualSet")]
        [Alias("ILE")]
        public SwitchParameter LE
        {
            set { _binaryOperator = TokenKind.Ile; }
            get { return _binaryOperator == TokenKind.Ile; }
        }

        /// <summary>
        /// Case sensitive binary operator -cle
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CaseSensitiveLessOrEqualSet")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "CLE")]
        public SwitchParameter CLE
        {
            set { _binaryOperator = TokenKind.Cle; }
            get { return _binaryOperator == TokenKind.Cle; }
        }

        /// <summary>
        /// Binary operator -Like
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "LikeSet")]
        [Alias("ILike")]
        public SwitchParameter Like
        {
            set { _binaryOperator = TokenKind.Ilike; }
            get { return _binaryOperator == TokenKind.Ilike; }
        }

        /// <summary>
        /// Case sensitive binary operator -clike
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CaseSensitiveLikeSet")]
        public SwitchParameter CLike
        {
            set { _binaryOperator = TokenKind.Clike; }
            get { return _binaryOperator == TokenKind.Clike; }
        }

        /// <summary>
        /// Binary operator -NotLike
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "NotLikeSet")]
        [Alias("INotLike")]
        public SwitchParameter NotLike
        {
            set { _binaryOperator = TokenKind.Inotlike; }
            get { return false; }
        }

        /// <summary>
        /// Case sensitive binary operator -cnotlike
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CaseSensitiveNotLikeSet")]
        public SwitchParameter CNotLike
        {
            set { _binaryOperator = TokenKind.Cnotlike; }
            get { return _binaryOperator == TokenKind.Cnotlike; }
        }

        /// <summary>
        /// Binary operator -Match
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "MatchSet")]
        [Alias("IMatch")]
        public SwitchParameter Match
        {
            set { _binaryOperator = TokenKind.Imatch; }
            get { return _binaryOperator == TokenKind.Imatch; }
        }

        /// <summary>
        /// Case sensitive binary operator -cmatch
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CaseSensitiveMatchSet")]
        public SwitchParameter CMatch
        {
            set { _binaryOperator = TokenKind.Cmatch; }
            get { return _binaryOperator == TokenKind.Cmatch; }
        }

        /// <summary>
        /// Binary operator -NotMatch
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "NotMatchSet")]
        [Alias("INotMatch")]
        public SwitchParameter NotMatch
        {
            set { _binaryOperator = TokenKind.Inotmatch; }
            get { return _binaryOperator == TokenKind.Inotmatch; }
        }

        /// <summary>
        /// Case sensitive binary operator -cnotmatch
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CaseSensitiveNotMatchSet")]
        public SwitchParameter CNotMatch
        {
            set { _binaryOperator = TokenKind.Cnotmatch; }
            get { return _binaryOperator == TokenKind.Cnotmatch; }
        }

        /// <summary>
        /// Binary operator -Contains
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "ContainsSet")]
        [Alias("IContains")]
        public SwitchParameter Contains
        {
            set { _binaryOperator = TokenKind.Icontains; }
            get { return _binaryOperator == TokenKind.Icontains; }
        }

        /// <summary>
        /// Case sensitive binary operator -ccontains
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CaseSensitiveContainsSet")]
        public SwitchParameter CContains
        {
            set { _binaryOperator = TokenKind.Ccontains; }
            get { return _binaryOperator == TokenKind.Ccontains; }
        }

        /// <summary>
        /// Binary operator -NotContains
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "NotContainsSet")]
        [Alias("INotContains")]
        public SwitchParameter NotContains
        {
            set { _binaryOperator = TokenKind.Inotcontains; }
            get { return _binaryOperator == TokenKind.Inotcontains; }
        }

        /// <summary>
        /// Case sensitive binary operator -cnotcontains
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CaseSensitiveNotContainsSet")]
        public SwitchParameter CNotContains
        {
            set { _binaryOperator = TokenKind.Cnotcontains; }
            get { return _binaryOperator == TokenKind.Cnotcontains; }
        }

        /// <summary>
        /// Binary operator -In
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "InSet")]
        [Alias("IIn")]
        public SwitchParameter In
        {
            set { _binaryOperator = TokenKind.In; }
            get { return _binaryOperator == TokenKind.In; }
        }

        /// <summary>
        /// Case sensitive binary operator -cin
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CaseSensitiveInSet")]
        public SwitchParameter CIn
        {
            set { _binaryOperator = TokenKind.Cin; }
            get { return _binaryOperator == TokenKind.Cin; }
        }

        /// <summary>
        /// Binary operator -NotIn
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "NotInSet")]
        [Alias("INotIn")]
        public SwitchParameter NotIn
        {
            set { _binaryOperator = TokenKind.Inotin; }
            get { return _binaryOperator == TokenKind.Inotin; }
        }

        /// <summary>
        /// Case sensitive binary operator -cnotin
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CaseSensitiveNotInSet")]
        public SwitchParameter CNotIn
        {
            set { _binaryOperator = TokenKind.Cnotin; }
            get { return _binaryOperator == TokenKind.Cnotin; }
        }

        /// <summary>
        /// Binary operator -Is
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "IsSet")]
        public SwitchParameter Is
        {
            set { _binaryOperator = TokenKind.Is; }
            get { return _binaryOperator == TokenKind.Is; }
        }

        /// <summary>
        /// Binary operator -IsNot
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "IsNotSet")]
        public SwitchParameter IsNot
        {
            set { _binaryOperator = TokenKind.IsNot; }
            get { return _binaryOperator == TokenKind.IsNot; }
        }

        #endregion binary operator parameters

        private readonly CallSite<Func<CallSite, object, bool>> _toBoolSite =
            CallSite<Func<CallSite, object, bool>>.Create(PSConvertBinder.Get(typeof(bool)));
        private Func<object, object, object> _operationDelegate;

        private static Func<object, object, object> GetCallSiteDelegate(ExpressionType expressionType, bool ignoreCase)
        {
            var site = CallSite<Func<CallSite, object, object, object>>.Create(PSBinaryOperationBinder.Get(expressionType, ignoreCase));
            return (x, y) => site.Target.Invoke(site, x, y);
        }

        private static Tuple<CallSite<Func<CallSite, object, IEnumerator>>, CallSite<Func<CallSite, object, object, object>>> GetContainsCallSites(bool ignoreCase)
        {
            var enumerableSite = CallSite<Func<CallSite, object, IEnumerator>>.Create(PSEnumerableBinder.Get());
            var eqSite =
                CallSite<Func<CallSite, object, object, object>>.Create(PSBinaryOperationBinder.Get(
                    ExpressionType.Equal, ignoreCase, scalarCompare: true));

            return Tuple.Create(enumerableSite, eqSite);
        }

        private void CheckLanguageMode()
        {
            if (Context.LanguageMode.Equals(PSLanguageMode.RestrictedLanguage))
            {
                string message = String.Format(CultureInfo.InvariantCulture,
                                               InternalCommandStrings.OperationNotAllowedInRestrictedLanguageMode,
                                               _binaryOperator);
                PSInvalidOperationException exception =
                    new PSInvalidOperationException(message);
                ThrowTerminatingError(new ErrorRecord(exception, "OperationNotAllowedInRestrictedLanguageMode", ErrorCategory.InvalidOperation, null));
            }
        }

        private object GetLikeRHSOperand(object operand)
        {
            var val = operand as string;
            if (val == null)
                return operand;

            var wildcardOptions = _binaryOperator == TokenKind.Ilike || _binaryOperator == TokenKind.Inotlike
                ? WildcardOptions.IgnoreCase
                : WildcardOptions.None;
            return WildcardPattern.Get(val, wildcardOptions);
        }

        /// <summary/>
        protected override void BeginProcessing()
        {
            if (_script != null)
                return;

            switch (_binaryOperator)
            {
                case TokenKind.Ieq:
                    if (!_forceBooleanEvaluation)
                    {
                        _operationDelegate = GetCallSiteDelegate(ExpressionType.Equal, ignoreCase: true);
                    }
                    else
                    {
                        // flip 'lval' and 'rval' in the scenario '... | Where-Object property' so as to make it
                        // equivalent to '... | Where-Object {$true -eq property}'. Because we want the property to
                        // be compared under the bool context. So that '"string" | Where-Object Length' would behave
                        // just like '"string" | Where-Object {$_.Length}'.
                        var site = CallSite<Func<CallSite, object, object, object>>.Create(PSBinaryOperationBinder.Get(ExpressionType.Equal, true));
                        _operationDelegate = (x, y) => site.Target.Invoke(site, y, x);
                    }
                    break;
                case TokenKind.Ceq:
                    _operationDelegate = GetCallSiteDelegate(ExpressionType.Equal, ignoreCase: false);
                    break;
                case TokenKind.Ine:
                    _operationDelegate = GetCallSiteDelegate(ExpressionType.NotEqual, ignoreCase: true);
                    break;
                case TokenKind.Cne:
                    _operationDelegate = GetCallSiteDelegate(ExpressionType.NotEqual, ignoreCase: false);
                    break;
                case TokenKind.Igt:
                    _operationDelegate = GetCallSiteDelegate(ExpressionType.GreaterThan, ignoreCase: true);
                    break;
                case TokenKind.Cgt:
                    _operationDelegate = GetCallSiteDelegate(ExpressionType.GreaterThan, ignoreCase: false);
                    break;
                case TokenKind.Ilt:
                    _operationDelegate = GetCallSiteDelegate(ExpressionType.LessThan, ignoreCase: true);
                    break;
                case TokenKind.Clt:
                    _operationDelegate = GetCallSiteDelegate(ExpressionType.LessThan, ignoreCase: false);
                    break;
                case TokenKind.Ige:
                    _operationDelegate = GetCallSiteDelegate(ExpressionType.GreaterThanOrEqual, ignoreCase: true);
                    break;
                case TokenKind.Cge:
                    _operationDelegate = GetCallSiteDelegate(ExpressionType.GreaterThanOrEqual, ignoreCase: false);
                    break;
                case TokenKind.Ile:
                    _operationDelegate = GetCallSiteDelegate(ExpressionType.LessThanOrEqual, ignoreCase: true);
                    break;
                case TokenKind.Cle:
                    _operationDelegate = GetCallSiteDelegate(ExpressionType.LessThanOrEqual, ignoreCase: false);
                    break;
                case TokenKind.Ilike:
                    _operationDelegate =
                        (lval, rval) => ParserOps.LikeOperator(Context, PositionUtilities.EmptyExtent, lval, rval, _binaryOperator);
                    break;
                case TokenKind.Clike:
                    _operationDelegate =
                        (lval, rval) => ParserOps.LikeOperator(Context, PositionUtilities.EmptyExtent, lval, rval, _binaryOperator);
                    break;
                case TokenKind.Inotlike:
                    _operationDelegate =
                        (lval, rval) => ParserOps.LikeOperator(Context, PositionUtilities.EmptyExtent, lval, rval, _binaryOperator);
                    break;
                case TokenKind.Cnotlike:
                    _operationDelegate =
                        (lval, rval) => ParserOps.LikeOperator(Context, PositionUtilities.EmptyExtent, lval, rval, _binaryOperator);
                    break;
                case TokenKind.Imatch:
                    CheckLanguageMode();
                    _operationDelegate =
                        (lval, rval) => ParserOps.MatchOperator(Context, PositionUtilities.EmptyExtent, lval, rval, notMatch: false, ignoreCase: true);
                    break;
                case TokenKind.Cmatch:
                    CheckLanguageMode();
                    _operationDelegate =
                        (lval, rval) => ParserOps.MatchOperator(Context, PositionUtilities.EmptyExtent, lval, rval, notMatch: false, ignoreCase: false);
                    break;
                case TokenKind.Inotmatch:
                    CheckLanguageMode();
                    _operationDelegate =
                        (lval, rval) => ParserOps.MatchOperator(Context, PositionUtilities.EmptyExtent, lval, rval, notMatch: true, ignoreCase: true);
                    break;
                case TokenKind.Cnotmatch:
                    CheckLanguageMode();
                    _operationDelegate =
                        (lval, rval) => ParserOps.MatchOperator(Context, PositionUtilities.EmptyExtent, lval, rval, notMatch: true, ignoreCase: false);
                    break;
                // the second to last parameter in ContainsOperator has flipped semantics compared to others.
                // "true" means "contains" while "false" means "notcontains"
                case TokenKind.Icontains:
                case TokenKind.Inotcontains:
                case TokenKind.In:
                case TokenKind.Inotin:
                    {
                        var sites = GetContainsCallSites(ignoreCase: true);
                        switch (_binaryOperator)
                        {
                            case TokenKind.Icontains:
                                _operationDelegate =
                                    (lval, rval) => ParserOps.ContainsOperatorCompiled(Context, sites.Item1, sites.Item2, lval, rval);
                                break;
                            case TokenKind.Inotcontains:
                                _operationDelegate =
                                    (lval, rval) => !ParserOps.ContainsOperatorCompiled(Context, sites.Item1, sites.Item2, lval, rval);
                                break;
                            case TokenKind.In:
                                _operationDelegate =
                                    (lval, rval) => ParserOps.ContainsOperatorCompiled(Context, sites.Item1, sites.Item2, rval, lval);
                                break;
                            case TokenKind.Inotin:
                                _operationDelegate =
                                    (lval, rval) => !ParserOps.ContainsOperatorCompiled(Context, sites.Item1, sites.Item2, rval, lval);
                                break;
                        }
                        break;
                    }
                case TokenKind.Ccontains:
                case TokenKind.Cnotcontains:
                case TokenKind.Cin:
                case TokenKind.Cnotin:
                    {
                        var sites = GetContainsCallSites(ignoreCase: false);
                        switch (_binaryOperator)
                        {
                            case TokenKind.Ccontains:
                                _operationDelegate =
                                    (lval, rval) => ParserOps.ContainsOperatorCompiled(Context, sites.Item1, sites.Item2, lval, rval);
                                break;
                            case TokenKind.Cnotcontains:
                                _operationDelegate =
                                    (lval, rval) => !ParserOps.ContainsOperatorCompiled(Context, sites.Item1, sites.Item2, lval, rval);
                                break;
                            case TokenKind.Cin:
                                _operationDelegate =
                                    (lval, rval) => ParserOps.ContainsOperatorCompiled(Context, sites.Item1, sites.Item2, rval, lval);
                                break;
                            case TokenKind.Cnotin:
                                _operationDelegate =
                                    (lval, rval) => !ParserOps.ContainsOperatorCompiled(Context, sites.Item1, sites.Item2, rval, lval);
                                break;
                        }
                        break;
                    }
                case TokenKind.Is:
                    _operationDelegate = (lval, rval) => ParserOps.IsOperator(Context, PositionUtilities.EmptyExtent, lval, rval);
                    break;
                case TokenKind.IsNot:
                    _operationDelegate = (lval, rval) => ParserOps.IsNotOperator(Context, PositionUtilities.EmptyExtent, lval, rval);
                    break;
            }

            _convertedValue = _value;
            if (!_valueNotSpecified)
            {
                switch (_binaryOperator)
                {
                    case TokenKind.Ilike:
                    case TokenKind.Clike:
                    case TokenKind.Inotlike:
                    case TokenKind.Cnotlike:
                        _convertedValue = GetLikeRHSOperand(_convertedValue);
                        break;

                    case TokenKind.Is:
                    case TokenKind.IsNot:
                        // users might input [int], [string] as they do when using scripts
                        var strValue = _convertedValue as string;
                        if (strValue != null)
                        {
                            var typeLength = strValue.Length;
                            if (typeLength > 2 && strValue[0] == '[' && strValue[typeLength - 1] == ']')
                            {
                                _convertedValue = strValue.Substring(1, typeLength - 2);
                            }

                            _convertedValue = LanguagePrimitives.ConvertTo<Type>(_convertedValue);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Execute the script block passing in the current pipeline object as
        /// it's only parameter.
        /// </summary>
        /// <exception cref="SessionStateOverflowException">
        /// The maximum scope depth would be exceeded
        /// </exception>
        /// <exception cref="ParseException">could not parse script</exception>
        /// <exception cref="RuntimeException">see Pipeline.Invoke</exception>
        /// <exception cref="ParameterBindingException">see Pipeline.Invoke</exception>
        protected override void ProcessRecord()
        {
            if (_inputObject == AutomationNull.Value)
                return;

            if (_script != null)
            {
                object result = _script.DoInvokeReturnAsIs(
                    useLocalScope: false,
                    errorHandlingBehavior: ScriptBlock.ErrorHandlingBehavior.WriteToCurrentErrorPipe,
                    dollarUnder: InputObject,
                    input: new object[] { _inputObject },
                    scriptThis: AutomationNull.Value,
                    args: Utils.EmptyArray<object>());

                if (_toBoolSite.Target.Invoke(_toBoolSite, result))
                {
                    WriteObject(InputObject);
                }
            }
            else
            {
                // Both -Property and -Value need to be specified if the user specifies the binary operation
                if (_valueNotSpecified && (_binaryOperator != TokenKind.Ieq || !_forceBooleanEvaluation))
                {
                    // The binary operation is specified explicitly by the user and the -Value parameter is
                    // not specified
                    ThrowTerminatingError(ForEachObjectCommand.
                            GenerateNameParameterError("Value",
                                                       InternalCommandStrings.ValueNotSpecifiedForWhereObject,
                                                       "ValueNotSpecifiedForWhereObject", null));
                }

                // The binary operation needs to be specified if the user specifies both the -Property and -Value
                if (!_valueNotSpecified && (_binaryOperator == TokenKind.Ieq && _forceBooleanEvaluation))
                {
                    // The -Property and -Value are specified explicitly by the user but the binary operation is not
                    ThrowTerminatingError(ForEachObjectCommand.
                            GenerateNameParameterError("Operator",
                                                       InternalCommandStrings.OperatorNotSpecified,
                                                       "OperatorNotSpecified", null));
                }

                bool strictModeWithError = false;
                object lvalue = GetValue(ref strictModeWithError);
                if (strictModeWithError) return;

                try
                {
                    object result = _operationDelegate.Invoke(lvalue, _convertedValue);
                    if (_toBoolSite.Target.Invoke(_toBoolSite, result))
                    {
                        WriteObject(InputObject);
                    }
                }
                catch (PipelineStoppedException)
                {
                    // PipelineStoppedException can be caused by select-object
                    throw;
                }
                catch (ArgumentException ae)
                {
                    ErrorRecord errorRecord = new ErrorRecord(
                        PSTraceSource.NewArgumentException("BinaryOperator", ParserStrings.BadOperatorArgument, _binaryOperator, ae.Message),
                        "BadOperatorArgument",
                        ErrorCategory.InvalidArgument,
                        _inputObject);
                    WriteError(errorRecord);
                }
                catch (Exception ex)
                {
                    CommandProcessorBase.CheckForSevereException(ex);

                    ErrorRecord errorRecord = new ErrorRecord(
                        PSTraceSource.NewInvalidOperationException(ParserStrings.OperatorFailed, _binaryOperator, ex.Message),
                        "OperatorFailed",
                        ErrorCategory.InvalidOperation,
                        _inputObject);
                    WriteError(errorRecord);
                }
            }
        }

        /// <summary>
        /// Get the value based on the given property name
        /// </summary>
        /// <returns>the value of the property</returns>
        private object GetValue(ref bool error)
        {
            if (LanguagePrimitives.IsNull(InputObject))
            {
                if (Context.IsStrictVersion(2))
                {
                    WriteError(
                        ForEachObjectCommand.
                            GenerateNameParameterError("InputObject",
                                                       InternalCommandStrings.InputObjectIsNull,
                                                       "InputObjectIsNull", _inputObject,
                                                       _property));
                    error = true;
                }
                return null;
            }

            // If the target is a hash table and it contains the requested key
            // return that, otherwise fall through and see if there is an
            // underlying member corresponding to the key...
            object target = PSObject.Base(_inputObject);
            IDictionary hash = target as IDictionary;
            try
            {
                if (hash != null && hash.Contains(_property))
                {
                    return hash[_property];
                }
            }
            catch (InvalidOperationException)
            {
                // Ignore invalid operation exception, it can happen if the dictionary
                // has keys that can't be compared to property.
            }

            ReadOnlyPSMemberInfoCollection<PSMemberInfo> members = GetMatchMembers();
            if (members.Count > 1)
            {
                StringBuilder possibleMatches = new StringBuilder();
                foreach (PSMemberInfo item in members)
                {
                    possibleMatches.AppendFormat(CultureInfo.InvariantCulture, " {0}", item.Name);
                }

                WriteError(
                    ForEachObjectCommand.
                        GenerateNameParameterError("Property",
                                                   InternalCommandStrings.AmbiguousPropertyOrMethodName,
                                                   "AmbiguousPropertyName", _inputObject,
                                                   _property, possibleMatches));
                error = true;
            }
            else if (members.Count == 0)
            {
                if (Context.IsStrictVersion(2))
                {
                    WriteError(ForEachObjectCommand.GenerateNameParameterError("Property",
                                                                               InternalCommandStrings.PropertyNotFound,
                                                                               "PropertyNotFound", _inputObject, _property));
                    error = true;
                }
            }
            else
            {
                try
                {
                    return members[0].Value;
                }
                catch (TerminateException)
                {
                    throw;
                }
                catch (MethodException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // When the property is not gettable or it throws an exception
                    CommandProcessorBase.CheckForSevereException(ex);
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// Get the matched PSMembers
        /// </summary>
        /// <returns></returns>
        private ReadOnlyPSMemberInfoCollection<PSMemberInfo> GetMatchMembers()
        {
            if (!WildcardPattern.ContainsWildcardCharacters(_property))
            {
                PSMemberInfoInternalCollection<PSMemberInfo> results = new PSMemberInfoInternalCollection<PSMemberInfo>();
                PSMemberInfo member = _inputObject.Members[_property];
                if (member != null)
                {
                    results.Add(member);
                }
                return new ReadOnlyPSMemberInfoCollection<PSMemberInfo>(results);
            }

            ReadOnlyPSMemberInfoCollection<PSMemberInfo> members = _inputObject.Members.Match(_property, PSMemberTypes.All);
            Dbg.Assert(members != null, "The return value of Members.Match should never be null.");
            return members;
        }
    }



    /// <summary>
    /// Implements a cmdlet that sets the script debugging options.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PSDebug", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113398")]
    public sealed class SetPSDebugCommand : PSCmdlet
    {
        /// <summary>
        /// Sets the script tracing level
        /// </summary>
        [Parameter(ParameterSetName = "on")]
        [ValidateRange(0, 2)]
        public int Trace
        {
            set { _trace = value; }
            get { return _trace; }
        }
        private int _trace = -1;

        /// <summary>
        /// Turns stepping on and off
        /// </summary>
        [Parameter(ParameterSetName = "on")]
        public SwitchParameter Step
        {
            set { _step = value; }
            get { return (SwitchParameter)_step; }
        }
        private bool? _step;

        /// <summary>
        /// Turns strict mode on and off.
        /// </summary>
        [Parameter(ParameterSetName = "on")]
        public SwitchParameter Strict
        {
            set { _strict = value; }
            get { return (SwitchParameter)_strict; }
        }
        private bool? _strict;

        /// <summary>
        /// Turns all script debugging features off.
        /// </summary>
        [Parameter(ParameterSetName = "off")]
        public SwitchParameter Off
        {
            get { return _off; }
            set { _off = value; }
        }
        private bool _off;

        /// <summary>
        /// Execute the begin scriptblock at the start of processing
        /// </summary>
        protected override void BeginProcessing()
        {
            // -off gets processed after the others so it takes precedence...
            if (_off)
            {
                Context.Debugger.DisableTracing();
                Context.EngineSessionState.GlobalScope.StrictModeVersion = null;
            }
            else
            {
                if (_trace >= 0 || _step != null)
                {
                    Context.Debugger.EnableTracing(_trace, _step);
                }
                // Version 0 is the same as off
                if (_strict != null)
                    Context.EngineSessionState.GlobalScope.StrictModeVersion = new Version((bool)_strict ? 1 : 0, 0);
            }
        }
    }

    #region Set-StrictMode

    /// <summary>
    /// Set-StrictMode causes the interpreter to throw an exception in the following cases:
    /// * Referencing an unassigned variable
    /// * Referencing a non-existent property of an object
    /// * Calling a function as a method (with parentheses and commas)
    /// * Using the variable expansion syntax in a string literal w/o naming a variable, i.e. "${}"
    ///
    /// Parameters:
    ///
    /// -Version allows the script author to specify which strict mode version to enforce.
    /// -Off turns strict mode off
    ///
    /// Note:
    ///
    /// Unlike Set-PSDebug -strict, Set-StrictMode is not engine-wide, and only 
    /// affects the scope it was defined in.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "StrictMode", DefaultParameterSetName = "Version", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113450")]
    public class SetStrictModeCommand : PSCmdlet
    {
        /// <summary>
        /// The following is the definition of the input parameter "Off".
        /// Turns strict mode off
        /// </summary>
        [Parameter(ParameterSetName = "Off", Mandatory = true)]
        public SwitchParameter Off
        {
            get { return _off; }
            set { _off = value; }
        }
        private SwitchParameter _off;

        /// <summary>
        /// To make it easier to specify a version, we add some conversions that wouldn't happen otherwise:
        ///   * A simple integer, i.e. 2
        ///   * A string without a dot, i.e. "2"
        ///   * The string 'latest', which we interpret to be the current version of PowerShell.
        /// </summary>
        private sealed class ArgumentToVersionTransformationAttribute : ArgumentTransformationAttribute
        {
            public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
            {
                object version = PSObject.Base(inputData);

                string versionStr = version as string;
                if (versionStr != null)
                {
                    if (versionStr.Equals("latest", StringComparison.OrdinalIgnoreCase))
                    {
                        return PSVersionInfo.PSVersion;
                    }
                    if (versionStr.Contains("."))
                    {
                        // If the string contains a '.', let the Version constructor handle the conversion.
                        return inputData;
                    }
                }
                if (version is double)
                {
                    // The conversion to int below is wrong, but the usual conversions will turn
                    // the double into a string, so just return the original object.
                    return inputData;
                }

                int majorVersion;
                if (LanguagePrimitives.TryConvertTo<int>(version, out majorVersion))
                {
                    return new Version(majorVersion, 0);
                }

                return inputData;
            }
        }

        private sealed class ValidateVersionAttribute : ValidateArgumentsAttribute
        {
            protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
            {
                Version version = arguments as Version;
                if (version == null || !PSVersionInfo.IsValidPSVersion(version))
                {
                    // No conversion succeeded so throw and exception...
                    throw new ValidationMetadataException("InvalidPSVersion",
                        null, Metadata.ValidateVersionFailure, arguments);
                }
            }
        }

        /// <summary>
        /// The following is the definition of the input parameter "Version".
        /// Turns strict mode in the current scope.
        /// </summary>
        [Parameter(ParameterSetName = "Version", Mandatory = true)]
        [ArgumentToVersionTransformation()]
        [ValidateVersion()]
        [Alias("v")]
        public Version Version
        {
            get { return _version; }
            set { _version = value; }
        }
        private Version _version;

        /// <summary>
        /// Set the correct version for strict mode checking in the current scope.
        /// </summary>
        protected override void EndProcessing()
        {
            if (_off.IsPresent)
            {
                _version = new Version(0, 0);
            }
            Context.EngineSessionState.CurrentScope.StrictModeVersion = _version;
        }
    }
    #endregion Set-StrictMode


    #endregion Built-in cmdlets that are used by or require direct access to the engine.
}