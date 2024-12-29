// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq.Expressions;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Management.Automation.PSTasks;
using System.Management.Automation.Security;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

using CommonParamSet = System.Management.Automation.Internal.CommonParameters;
using Dbg = System.Management.Automation.Diagnostics;
using NotNullWhen = System.Diagnostics.CodeAnalysis.NotNullWhenAttribute;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A thin wrapper over a property-getting Callsite, to allow reuse when possible.
    /// </summary>
    internal struct DynamicPropertyGetter
    {
        private CallSite<Func<CallSite, object, object>> _getValueDynamicSite;

        // For the wildcard case, lets us know if we can reuse the callsite:
        private string _lastUsedPropertyName;

        public object GetValue(PSObject inputObject, string propertyName)
        {
            Dbg.Assert(!WildcardPattern.ContainsWildcardCharacters(propertyName), "propertyName should be pre-resolved by caller");

            // If wildcards are involved, the resolved property name could potentially
            // be different on every object... but probably not, so we'll attempt to
            // reuse the callsite if possible.
            if (!propertyName.Equals(_lastUsedPropertyName, StringComparison.OrdinalIgnoreCase))
            {
                _lastUsedPropertyName = propertyName;
                _getValueDynamicSite = CallSite<Func<CallSite, object, object>>.Create(
                        PSGetMemberBinder.Get(
                            propertyName,
                            classScope: (Type)null,
                            @static: false));
            }

            return _getValueDynamicSite.Target.Invoke(_getValueDynamicSite, inputObject);
        }
    }

    #region Built-in cmdlets that are used by or require direct access to the engine.

    /// <summary>
    /// Implements a cmdlet that applies a script block
    /// to each element of the pipeline.
    /// </summary>
    [Cmdlet("ForEach", "Object", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low,
        DefaultParameterSetName = ForEachObjectCommand.ScriptBlockSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096867",
        RemotingCapability = RemotingCapability.None)]
    public sealed class ForEachObjectCommand : PSCmdlet, IDisposable
    {
        #region Private Members

        private const string ParallelParameterSet = "ParallelParameterSet";
        private const string ScriptBlockSet = "ScriptBlockSet";
        private const string PropertyAndMethodSet = "PropertyAndMethodSet";

        #endregion

        #region Common Parameters

        /// <summary>
        /// This parameter specifies the current pipeline object.
        /// </summary>
        [Parameter(ValueFromPipeline = true, ParameterSetName = ForEachObjectCommand.ScriptBlockSet)]
        [Parameter(ValueFromPipeline = true, ParameterSetName = ForEachObjectCommand.PropertyAndMethodSet)]
        [Parameter(ValueFromPipeline = true, ParameterSetName = ForEachObjectCommand.ParallelParameterSet)]
        public PSObject InputObject
        {
            get { return _inputObject; }

            set { _inputObject = value; }
        }

        private PSObject _inputObject = AutomationNull.Value;

        #endregion

        #region ScriptBlockSet

        private readonly List<ScriptBlock> _scripts = new List<ScriptBlock>();

        /// <summary>
        /// Gets or sets the script block to apply in begin processing.
        /// </summary>
        [Parameter(ParameterSetName = ForEachObjectCommand.ScriptBlockSet)]
        public ScriptBlock Begin
        {
            get
            {
                return null;
            }

            set
            {
                _scripts.Insert(0, value);
            }
        }

        /// <summary>
        /// Gets or sets the script block to apply.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = ForEachObjectCommand.ScriptBlockSet)]
        [AllowNull]
        [AllowEmptyCollection]
        public ScriptBlock[] Process
        {
            get
            {
                return null;
            }

            set
            {
                if (value == null)
                {
                    _scripts.Add(null);
                }
                else
                {
                    _scripts.AddRange(value);
                }
            }
        }

        private ScriptBlock _endScript;
        private bool _setEndScript;

        /// <summary>
        /// Gets or sets the script block to apply in complete processing.
        /// </summary>
        [Parameter(ParameterSetName = ForEachObjectCommand.ScriptBlockSet)]
        public ScriptBlock End
        {
            get
            {
                return _endScript;
            }

            set
            {
                _endScript = value;
                _setEndScript = true;
            }
        }

        /// <summary>
        /// Gets or sets the remaining script blocks to apply.
        /// </summary>
        [Parameter(ParameterSetName = ForEachObjectCommand.ScriptBlockSet, ValueFromRemainingArguments = true)]
        [AllowNull]
        [AllowEmptyCollection]
        public ScriptBlock[] RemainingScripts
        {
            get
            {
                return null;
            }

            set
            {
                if (value == null)
                {
                    _scripts.Add(null);
                }
                else
                {
                    _scripts.AddRange(value);
                }
            }
        }

        private int _start, _end;

        #endregion ScriptBlockSet

        #region PropertyAndMethodSet

        /// <summary>
        /// Gets or sets the property or method name.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = ForEachObjectCommand.PropertyAndMethodSet)]
        [ValidateTrustedData]
        [ValidateNotNullOrEmpty]
        public string MemberName
        {
            get
            {
                return _propertyOrMethodName;
            }

            set
            {
                _propertyOrMethodName = value;
            }
        }

        private string _propertyOrMethodName;
        private string _targetString;
        private DynamicPropertyGetter _propGetter;

        /// <summary>
        /// The arguments passed to a method invocation.
        /// </summary>
        [Parameter(ParameterSetName = ForEachObjectCommand.PropertyAndMethodSet, ValueFromRemainingArguments = true)]
        [ValidateTrustedData]
        [Alias("Args")]
        public object[] ArgumentList
        {
            get { return _arguments; }

            set { _arguments = value; }
        }

        private object[] _arguments;

        #endregion PropertyAndMethodSet

        #region ParallelParameterSet

        /// <summary>
        /// Gets or sets a script block to run in parallel for each pipeline object.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = ForEachObjectCommand.ParallelParameterSet)]
        public ScriptBlock Parallel { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of concurrently running scriptblocks on separate threads.
        /// The default number is 5.
        /// </summary>
        [Parameter(ParameterSetName = ForEachObjectCommand.ParallelParameterSet)]
        [ValidateRange(1, Int32.MaxValue)]
        public int ThrottleLimit { get; set; } = 5;

        /// <summary>
        /// Gets or sets a timeout time in seconds, after which the parallel running scripts will be stopped
        /// The default value is 0, indicating no timeout.
        /// </summary>
        [Parameter(ParameterSetName = ForEachObjectCommand.ParallelParameterSet)]
        [ValidateRange(0, (Int32.MaxValue / 1000))]
        public int TimeoutSeconds { get; set; }

        /// <summary>
        /// Gets or sets a flag that returns a job object immediately for the parallel operation, instead of returning after
        /// all foreach processing is completed.
        /// </summary>
        [Parameter(ParameterSetName = ForEachObjectCommand.ParallelParameterSet)]
        public SwitchParameter AsJob { get; set; }

        /// <summary>
        /// Gets or sets a flag so that a new runspace object is created for each loop iteration, instead of reusing objects
        /// from the runspace pool.
        /// By default, runspaces are reused from a runspace pool.
        /// </summary>
        [Parameter(ParameterSetName = ForEachObjectCommand.ParallelParameterSet)]
        public SwitchParameter UseNewRunspace { get; set; }

        #endregion

        #region Overrides

        /// <summary>
        /// Execute the begin scriptblock at the start of processing.
        /// </summary>
        /// <exception cref="ParseException">Could not parse script.</exception>
        /// <exception cref="RuntimeException">See Pipeline.Invoke.</exception>
        /// <exception cref="ParameterBindingException">See Pipeline.Invoke.</exception>
        protected override void BeginProcessing()
        {
            switch (ParameterSetName)
            {
                case ForEachObjectCommand.ScriptBlockSet:
                    InitScriptBlockParameterSet();
                    break;

                case ForEachObjectCommand.ParallelParameterSet:
                    InitParallelParameterSet();
                    break;
            }
        }

        /// <summary>
        /// Execute the processing script blocks on the current pipeline object
        /// which is passed as it's only parameter.
        /// </summary>
        /// <exception cref="ParseException">Could not parse script.</exception>
        /// <exception cref="RuntimeException">See Pipeline.Invoke.</exception>
        /// <exception cref="ParameterBindingException">See Pipeline.Invoke.</exception>
        protected override void ProcessRecord()
        {
            switch (ParameterSetName)
            {
                case ForEachObjectCommand.ScriptBlockSet:
                    ProcessScriptBlockParameterSet();
                    break;

                case ForEachObjectCommand.PropertyAndMethodSet:
                    ProcessPropertyAndMethodParameterSet();
                    break;

                case ForEachObjectCommand.ParallelParameterSet:
                    ProcessParallelParameterSet();
                    break;
            }
        }

        /// <summary>
        /// Execute the end scriptblock when the pipeline is complete.
        /// </summary>
        /// <exception cref="ParseException">Could not parse script.</exception>
        /// <exception cref="RuntimeException">See Pipeline.Invoke.</exception>
        /// <exception cref="ParameterBindingException">See Pipeline.Invoke.</exception>
        protected override void EndProcessing()
        {
            switch (ParameterSetName)
            {
                case ForEachObjectCommand.ScriptBlockSet:
                    EndBlockParameterSet();
                    break;

                case ForEachObjectCommand.ParallelParameterSet:
                    EndParallelParameterSet();
                    break;
            }
        }

        /// <summary>
        /// Handle pipeline stop signal.
        /// </summary>
        protected override void StopProcessing()
        {
            switch (ParameterSetName)
            {
                case ForEachObjectCommand.ParallelParameterSet:
                    StopParallelProcessing();
                    break;
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose cmdlet instance.
        /// </summary>
        public void Dispose()
        {
            // Ensure all parallel task objects are disposed
            _taskTimer?.Dispose();
            _taskDataStreamWriter?.Dispose();
            _taskPool?.Dispose();
            _taskCollection?.Dispose();
        }

        #endregion

        #region Private Methods

        #region PSTasks

        private PSTaskPool _taskPool;
        private PSTaskDataStreamWriter _taskDataStreamWriter;
        private Dictionary<string, object> _usingValuesMap;
        private Timer _taskTimer;
        private PSTaskJob _taskJob;
        private PSDataCollection<System.Management.Automation.PSTasks.PSTask> _taskCollection;
        private Exception _taskCollectionException;
        private string _currentLocationPath;

        private void InitParallelParameterSet()
        {
            // The following common parameters are not (yet) supported in this parameter set.
            //  ErrorAction, WarningAction, InformationAction, ProgressAction, PipelineVariable.
            if (MyInvocation.BoundParameters.ContainsKey(nameof(CommonParamSet.ErrorAction)) ||
                MyInvocation.BoundParameters.ContainsKey(nameof(CommonParamSet.WarningAction)) ||
                MyInvocation.BoundParameters.ContainsKey(nameof(CommonParamSet.InformationAction)) ||
                MyInvocation.BoundParameters.ContainsKey(nameof(CommonParamSet.ProgressAction)) ||
                MyInvocation.BoundParameters.ContainsKey(nameof(CommonParamSet.PipelineVariable)))
            {
                ThrowTerminatingError(
                        new ErrorRecord(
                            new PSNotSupportedException(InternalCommandStrings.ParallelCommonParametersNotSupported),
                            "ParallelCommonParametersNotSupported",
                            ErrorCategory.NotImplemented,
                            this));
            }

            // Get the current working directory location, if available.
            try
            {
                _currentLocationPath = SessionState.Internal.CurrentLocation.Path;
            }
            catch (PSInvalidOperationException)
            {
            }

            var allowUsingExpression = this.Context.SessionState.LanguageMode != PSLanguageMode.NoLanguage;
            _usingValuesMap = ScriptBlockToPowerShellConverter.GetUsingValuesForEachParallel(
                scriptBlock: Parallel,
                isTrustedInput: allowUsingExpression,
                context: this.Context);

            // Validate using values map, which is a map of '$using:' variables referenced in the script.
            // Script block variables are not allowed since their behavior is undefined outside the runspace
            // in which they were created.
            foreach (object item in _usingValuesMap.Values)
            {
                if (item is ScriptBlock or PSObject { BaseObject: ScriptBlock })
                {
                    ThrowTerminatingError(
                        new ErrorRecord(
                            new PSArgumentException(InternalCommandStrings.ParallelUsingVariableCannotBeScriptBlock),
                            "ParallelUsingVariableCannotBeScriptBlock",
                            ErrorCategory.InvalidType,
                            this));
                }
            }

            if (AsJob)
            {
                // Set up for returning a job object.
                if (MyInvocation.BoundParameters.ContainsKey(nameof(TimeoutSeconds)))
                {
                    ThrowTerminatingError(
                        new ErrorRecord(
                            new PSArgumentException(InternalCommandStrings.ParallelCannotUseTimeoutWithJob),
                            "ParallelCannotUseTimeoutWithJob",
                            ErrorCategory.InvalidOperation,
                            this));
                }

                _taskJob = new PSTaskJob(
                    Parallel.ToString(),
                    ThrottleLimit,
                    UseNewRunspace);

                return;
            }

            // Set up for synchronous processing and data streaming.
            _taskCollection = new PSDataCollection<System.Management.Automation.PSTasks.PSTask>();
            _taskDataStreamWriter = new PSTaskDataStreamWriter(this);
            _taskPool = new PSTaskPool(ThrottleLimit, UseNewRunspace);
            _taskPool.PoolComplete += (sender, args) => _taskDataStreamWriter.Close();

            // Create timeout timer if requested.
            if (TimeoutSeconds != 0)
            {
                _taskTimer = new Timer(
                    callback: (_) => { _taskCollection.Complete(); _taskPool.StopAll(); },
                    state: null,
                    dueTime: TimeoutSeconds * 1000,
                    period: Timeout.Infinite);
            }

            // Task collection handler.
            System.Threading.ThreadPool.QueueUserWorkItem(
                (_) =>
                {
                    // As piped input are converted to PSTasks and added to the _taskCollection,
                    // transfer the task to the _taskPool on this dedicated thread.
                    // The _taskPool will block this thread when it is full, and allow more tasks to
                    // be added only when a currently running task completes and makes space in the pool.
                    // Continue adding any tasks appearing in _taskCollection until the collection is closed.
                    while (true)
                    {
                        // This handle will unblock the thread when a new task is available or the _taskCollection
                        // is closed.
                        _taskCollection.WaitHandle.WaitOne();

                        // Task collection open state is volatile.
                        // Record current task collection open state here, to be checked after processing.
                        bool isOpen = _taskCollection.IsOpen;

                        try
                        {
                            // Read all tasks in the collection.
                            foreach (var task in _taskCollection.ReadAll())
                            {
                                // This _taskPool method will block if the pool is full and will unblock
                                // only after a task completes making more space.
                                _taskPool.Add(task);
                            }
                        }
                        catch (Exception ex)
                        {
                            _taskCollection.Complete();
                            _taskCollectionException = ex;
                            _taskDataStreamWriter.Close();

                            break;
                        }

                        // Loop is exited only when task collection is closed and all task
                        // collection tasks are processed.
                        if (!isOpen)
                        {
                            break;
                        }
                    }

                    // We are done adding tasks and can close the task pool.
                    _taskPool.Close();
                });
        }

        private void ProcessParallelParameterSet()
        {
            // Validate piped InputObject
            if (_inputObject != null &&
                _inputObject.BaseObject is ScriptBlock)
            {
                WriteError(
                    new ErrorRecord(
                            new PSArgumentException(InternalCommandStrings.ParallelPipedInputObjectCannotBeScriptBlock),
                            "ParallelPipedInputObjectCannotBeScriptBlock",
                            ErrorCategory.InvalidType,
                            this));

                return;
            }

            if (AsJob)
            {
                // Add child task job.
                var taskChildJob = new PSTaskChildJob(
                    Parallel,
                    _usingValuesMap,
                    InputObject,
                    _currentLocationPath);

                _taskJob.AddJob(taskChildJob);

                return;
            }

            // Write any streaming data
            _taskDataStreamWriter.WriteImmediate();

            // Add to task collection for processing.
            if (_taskCollection.IsOpen)
            {
                try
                {
                    // Create a PSTask based on this piped input and add it to the task collection.
                    // A dedicated thread will add it to the PSTask pool in a performant manner.
                    _taskCollection.Add(
                        new System.Management.Automation.PSTasks.PSTask(
                            Parallel,
                            _usingValuesMap,
                            InputObject,
                            _currentLocationPath,
                            _taskDataStreamWriter));
                }
                catch (InvalidOperationException)
                {
                    // This exception is thrown if the task collection is closed, which should not happen.
                    Dbg.Assert(false, "Should not add to a closed PSTask collection");
                }
            }
        }

        private void EndParallelParameterSet()
        {
            if (AsJob)
            {
                // Start and return parent job object.
                _taskJob.Start();
                JobRepository.Add(_taskJob);
                WriteObject(_taskJob);

                return;
            }

            // Close task collection and wait for processing to complete while streaming data.
            _taskDataStreamWriter.WriteImmediate();
            _taskCollection.Complete();
            _taskDataStreamWriter.WaitAndWrite();

            // Check for an unexpected error from the _taskCollection handler thread and report here.
            var ex = _taskCollectionException;
            if (ex != null)
            {
                var msg = string.Format(CultureInfo.InvariantCulture, InternalCommandStrings.ParallelPipedInputProcessingError, ex);
                WriteError(
                    new ErrorRecord(
                        exception: new InvalidOperationException(msg),
                        errorId: "ParallelPipedInputProcessingError",
                        errorCategory: ErrorCategory.InvalidOperation,
                        targetObject: this));
            }
        }

        private void StopParallelProcessing()
        {
            _taskCollection?.Complete();
            _taskPool?.StopAll();
        }

        #endregion

        private void EndBlockParameterSet()
        {
            if (_endScript == null)
            {
                return;
            }

            var emptyArray = Array.Empty<object>();
            _endScript.InvokeUsingCmdlet(
                contextCmdlet: this,
                useLocalScope: false,
                errorHandlingBehavior: ScriptBlock.ErrorHandlingBehavior.WriteToCurrentErrorPipe,
                dollarUnder: AutomationNull.Value,
                input: emptyArray,
                scriptThis: AutomationNull.Value,
                args: emptyArray);
        }

        private void ProcessPropertyAndMethodParameterSet()
        {
            _targetString = string.Format(CultureInfo.InvariantCulture, InternalCommandStrings.ForEachObjectTarget, GetStringRepresentation(InputObject));

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
                    string propertyAction = string.Format(CultureInfo.InvariantCulture,
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
                            // PS C:\> $null | ForEach-object {$_.aa} | ForEach-Object {$_ + 3}
                            // 3
                            // so we also want
                            // PS C:\> $null | ForEach-object aa | ForEach-Object {$_ + 3}
                            // 3
                            // But if we don't write anything to the pipeline when _inputObject is null,
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
                if (GetValueFromIDictionaryInput())
                {
                    return;
                }

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
                            possibleMatches.Append(CultureInfo.InvariantCulture, $" {item.Name}");
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

                // member is a method
                if (member is PSMethodInfo)
                {
                    // first we check if the member is a ParameterizedProperty
                    PSParameterizedProperty targetParameterizedProperty = member as PSParameterizedProperty;
                    if (targetParameterizedProperty != null)
                    {
                        // should process
                        string propertyAction = string.Format(CultureInfo.InvariantCulture,
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
                        string methodAction = string.Format(CultureInfo.InvariantCulture,
                            InternalCommandStrings.ForEachObjectMethodActionWithoutArguments, targetMethod.Name);

                        if (ShouldProcess(_targetString, methodAction))
                        {
                            if (!BlockMethodInLanguageMode(InputObject))
                            {
                                object result = targetMethod.Invoke(Array.Empty<object>());
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
                else
                {
                    string resolvedPropertyName = null;
                    bool isBlindDynamicAccess = false;
                    if (member == null)
                    {
                        if ((_inputObject.BaseObject is IDynamicMetaObjectProvider) &&
                            !WildcardPattern.ContainsWildcardCharacters(_propertyOrMethodName))
                        {
                            // Let's just try a dynamic property access. Note that if it
                            // comes to depending on dynamic access, we are assuming it is a
                            // property; we don't have ETS info to tell us up front if it
                            // even exists or not, let alone if it is a method or something
                            // else.
                            //
                            // Note that this is "truly blind"--the name did not show up in
                            // GetDynamicMemberNames(), else it would show up as a dynamic
                            // member.

                            resolvedPropertyName = _propertyOrMethodName;
                            isBlindDynamicAccess = true;
                        }
                        else
                        {
                            errorRecord = GenerateNameParameterError("Name", InternalCommandStrings.PropertyOrMethodNotFound,
                                                                     "PropertyOrMethodNotFound", _inputObject,
                                                                     _propertyOrMethodName);
                        }
                    }
                    else
                    {
                        // member is [presumably] a property (note that it could be a
                        // dynamic property, if it shows up in GetDynamicMemberNames())
                        resolvedPropertyName = member.Name;
                    }

                    if (!string.IsNullOrEmpty(resolvedPropertyName))
                    {
                        // should process
                        string propertyAction = string.Format(CultureInfo.InvariantCulture,
                            InternalCommandStrings.ForEachObjectPropertyAction, resolvedPropertyName);

                        if (ShouldProcess(_targetString, propertyAction))
                        {
                            try
                            {
                                WriteToPipelineWithUnrolling(_propGetter.GetValue(InputObject, resolvedPropertyName));
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
                                // For normal property accesses, we do not generate an error
                                // here. The problem for truly blind dynamic accesses (the
                                // member did not show up in GetDynamicMemberNames) is that
                                // we can't tell the difference between "it failed because
                                // the property does not exist" (let's call this case 1) and
                                // "it failed because accessing it actually threw some
                                // exception" (let's call that case 2).
                                //
                                // PowerShell behavior for normal (non-dynamic) properties
                                // is different for these two cases: case 1 gets an error
                                // (which is possible because the ETS tells us up front if
                                // the property exists or not), and case 2 does not. (For
                                // normal properties, this catch block /is/ case 2.)
                                //
                                // For IDMOPs, we have the chance to attempt a "blind"
                                // access, but the cost is that we must have the same
                                // response to both cases (because we cannot distinguish
                                // between the two). So we have to make a choice: we can
                                // either swallow ALL errors (including "The property
                                // 'Blarg' does not exist"), or expose them all.
                                //
                                // Here, for truly blind dynamic access, we choose to
                                // preserve the behavior of showing "The property 'Blarg'
                                // does not exist" (case 1) errors than to suppress
                                // "FooException thrown when accessing Bloop property" (case
                                // 2) errors.

                                if (isBlindDynamicAccess)
                                {
                                    errorRecord = new ErrorRecord(ex,
                                                                  "DynamicPropertyAccessFailed_" + _propertyOrMethodName,
                                                                  ErrorCategory.InvalidOperation,
                                                                  InputObject);
                                }
                                else
                                {
                                    // When the property is not gettable or it throws an exception.
                                    // e.g. when trying to access an assembly's location property, since dynamic assemblies are not backed up by a file,
                                    // an exception will be thrown when accessing its location property. In this case, return null.
                                    WriteObject(null);
                                }
                            }
                        }
                    }
                }
            }

            if (errorRecord != null)
            {
                string propertyAction = string.Format(CultureInfo.InvariantCulture,
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
                        // But if we don't write anything to the pipeline when no member is found,
                        // the result 3 will not be generated.
                        WriteObject(null);
                    }
                }
            }
        }

        private void ProcessScriptBlockParameterSet()
        {
            for (int i = _start; i < _end; i++)
            {
                // Only execute scripts that aren't null. This isn't treated as an error
                // because it allows you to parameterize a command - for example you might allow
                // for actions before and after the main processing script. They could be null
                // by default and therefore ignored then filled in later...
                _scripts[i]?.InvokeUsingCmdlet(
                    contextCmdlet: this,
                    useLocalScope: false,
                    errorHandlingBehavior: ScriptBlock.ErrorHandlingBehavior.WriteToCurrentErrorPipe,
                    dollarUnder: InputObject,
                    input: new object[] { InputObject },
                    scriptThis: AutomationNull.Value,
                    args: Array.Empty<object>());
            }
        }

        private void InitScriptBlockParameterSet()
        {
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

            var emptyArray = Array.Empty<object>();
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
        /// Do method invocation with arguments.
        /// </summary>
        private void MethodCallWithArguments()
        {
            // resolve the name
            ReadOnlyPSMemberInfoCollection<PSMemberInfo> methods =
                _inputObject.Members.Match(
                    _propertyOrMethodName,
                    PSMemberTypes.Methods | PSMemberTypes.ParameterizedProperty);

            Dbg.Assert(methods != null, "The return value of Members.Match should never be null.");
            if (methods.Count > 1)
            {
                // write error record: method ambiguous
                StringBuilder possibleMatches = new StringBuilder();
                foreach (PSMemberInfo item in methods)
                {
                    possibleMatches.Append(CultureInfo.InvariantCulture, $" {item.Name}");
                }

                WriteError(GenerateNameParameterError(
                    "Name",
                    InternalCommandStrings.AmbiguousMethodName,
                    "AmbiguousMethodName",
                    _inputObject,
                    _propertyOrMethodName,
                    possibleMatches));
            }
            else if (methods.Count == 0 || methods[0] is not PSMethodInfo)
            {
                // write error record: method no found
                WriteError(GenerateNameParameterError(
                    "Name",
                    InternalCommandStrings.MethodNotFound,
                    "MethodNotFound",
                    _inputObject,
                    _propertyOrMethodName));
            }
            else
            {
                PSMethodInfo targetMethod = methods[0] as PSMethodInfo;
                Dbg.Assert(targetMethod != null, "targetMethod should not be null here.");

                // should process
                StringBuilder arglist = new StringBuilder(GetStringRepresentation(_arguments[0]));
                for (int i = 1; i < _arguments.Length; i++)
                {
                    arglist.Append(CultureInfo.InvariantCulture, $", {GetStringRepresentation(_arguments[i])}");
                }

                string methodAction = string.Format(CultureInfo.InvariantCulture,
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
                    WriteError(new ErrorRecord(ex, "MethodInvocationError", ErrorCategory.InvalidOperation, _inputObject));
                }
            }
        }

        /// <summary>
        /// Get the string representation of the passed-in object.
        /// </summary>
        /// <param name="obj">Source object.</param>
        /// <returns>String representation of the source object.</returns>
        private static string GetStringRepresentation(object obj)
        {
            string objInString;
            try
            {
                // The "ToString()" method could throw an exception
                objInString = LanguagePrimitives.IsNull(obj) ? "null" : obj.ToString();
            }
            catch (Exception)
            {
                objInString = null;
            }

            if (string.IsNullOrEmpty(objInString))
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
        /// <returns>True if success.</returns>
        private bool GetValueFromIDictionaryInput()
        {
            object target = PSObject.Base(_inputObject);
            IDictionary hash = target as IDictionary;

            try
            {
                if (hash != null && hash.Contains(_propertyOrMethodName))
                {
                    string keyAction = string.Format(
                        CultureInfo.InvariantCulture,
                        InternalCommandStrings.ForEachObjectKeyAction,
                        _propertyOrMethodName);
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
        /// <param name="obj">Source object.</param>
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
        /// <param name="list">Source list.</param>
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
        /// <param name="inputObject">Source object.</param>
        /// <returns>True if we are in restrictedLanguageMode.</returns>
        private bool BlockMethodInLanguageMode(object inputObject)
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
                object baseObject = PSObject.Base(inputObject);
                var objectType = baseObject.GetType();

                if (!CoreTypes.Contains(objectType))
                {
                    if (SystemPolicy.GetSystemLockdownPolicy() != SystemEnforcementMode.Audit)
                    {
                        PSInvalidOperationException exception =
                            new PSInvalidOperationException(ParserStrings.InvokeMethodConstrainedLanguage);

                        WriteError(new ErrorRecord(exception, "MethodInvocationNotSupportedInConstrainedLanguage", ErrorCategory.InvalidOperation, null));
                        return true;
                    }

                    SystemPolicy.LogWDACAuditMessage(
                        context: Context,
                        title: InternalCommandStrings.WDACLogTitle,
                        message: StringUtil.Format(InternalCommandStrings.WDACLogMessage, objectType.FullName),
                        fqid: "ForEachObjectCmdletMethodInvocationNotAllowed",
                        dropIntoDebugger: true);
                }
            }

            return false;
        }

        #endregion

        /// <summary>
        /// Generate the appropriate error record.
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
            if (args == null || args.Length == 0)
            {
                // Don't format in case the string contains literal curly braces
                message = resourceString;
            }
            else
            {
                message = StringUtil.Format(resourceString, args);
            }

            if (string.IsNullOrEmpty(message))
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
    }

    /// <summary>
    /// Implements a cmdlet that applys a script block
    /// to each element of the pipeline. If the result of that
    /// application is true, then the current pipeline object
    /// is passed on, otherwise it is dropped.
    /// </summary>
    [Cmdlet("Where", "Object", DefaultParameterSetName = "EqualSet",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096806", RemotingCapability = RemotingCapability.None)]
    public sealed class WhereObjectCommand : PSCmdlet
    {
        /// <summary>
        /// Gets or sets the current pipeline object.
        /// </summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject InputObject
        {
            get
            {
                return _inputObject;
            }

            set
            {
                _inputObject = value;
            }
        }

        private PSObject _inputObject = AutomationNull.Value;

        private ScriptBlock _script;
        /// <summary>
        /// Gets or sets the script block to apply.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ScriptBlockSet")]
        public ScriptBlock FilterScript
        {
            get
            {
                return _script;
            }

            set
            {
                _script = value;
            }
        }

        private string _property;

        /// <summary>
        /// Gets or sets the property to retrieve value.
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
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Not")]
        [ValidateNotNullOrEmpty]
        public string Property
        {
            get
            {
                return _property;
            }

            set
            {
                _property = value;
            }
        }

        private object _convertedValue;
        private object _value = true;
        private bool _valueNotSpecified = true;

        /// <summary>
        /// The value to compare against.
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
            get
            {
                return _value;
            }

            set
            {
                _value = value;
                _valueNotSpecified = false;
            }
        }

        #region binary operator parameters

        private TokenKind _binaryOperator = TokenKind.Ieq;

        // set to false if the user specified "-EQ" in the command line.
        // remain to be true if "EqualSet" is chosen by default.
        private bool _forceBooleanEvaluation = true;

        /// <summary>
        /// Gets or sets binary operator -Equal
        /// It's the default parameter set, so -EQ is not mandatory.
        /// </summary>
        [Parameter(ParameterSetName = "EqualSet")]
        [Alias("IEQ")]
        public SwitchParameter EQ
        {
            get
            {
                return _binaryOperator == TokenKind.Ieq;
            }

            set
            {
                _binaryOperator = TokenKind.Ieq;
                _forceBooleanEvaluation = false;
            }
        }

        /// <summary>
        /// Gets or sets case sensitive binary operator -ceq.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CaseSensitiveEqualSet")]
        public SwitchParameter CEQ
        {
            get
            {
                return _binaryOperator == TokenKind.Ceq;
            }

            set
            {
                _binaryOperator = TokenKind.Ceq;
            }
        }

        /// <summary>
        /// Gets or sets binary operator -NotEqual.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "NotEqualSet")]
        [Alias("INE")]
        public SwitchParameter NE
        {
            get
            {
                return _binaryOperator == TokenKind.Ine;
            }

            set
            {
                _binaryOperator = TokenKind.Ine;
            }
        }

        /// <summary>
        /// Gets or sets case sensitive binary operator -cne.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CaseSensitiveNotEqualSet")]
        public SwitchParameter CNE
        {
            get
            {
                return _binaryOperator == TokenKind.Cne;
            }

            set
            {
                _binaryOperator = TokenKind.Cne;
            }
        }

        /// <summary>
        /// Gets or sets binary operator -GreaterThan.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "GreaterThanSet")]
        [Alias("IGT")]
        public SwitchParameter GT
        {
            get
            {
                return _binaryOperator == TokenKind.Igt;
            }

            set
            {
                _binaryOperator = TokenKind.Igt;
            }
        }

        /// <summary>
        /// Gets or sets case sensitive binary operator -cgt.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CaseSensitiveGreaterThanSet")]
        public SwitchParameter CGT
        {
            get
            {
                return _binaryOperator == TokenKind.Cgt;
            }

            set
            {
                _binaryOperator = TokenKind.Cgt;
            }
        }

        /// <summary>
        /// Gets or sets binary operator -LessThan.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "LessThanSet")]
        [Alias("ILT")]
        public SwitchParameter LT
        {
            get
            {
                return _binaryOperator == TokenKind.Ilt;
            }

            set
            {
                _binaryOperator = TokenKind.Ilt;
            }
        }

        /// <summary>
        /// Gets or sets case sensitive binary operator -clt.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CaseSensitiveLessThanSet")]
        public SwitchParameter CLT
        {
            get
            {
                return _binaryOperator == TokenKind.Clt;
            }

            set
            {
                _binaryOperator = TokenKind.Clt;
            }
        }

        /// <summary>
        /// Gets or sets binary operator -GreaterOrEqual.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "GreaterOrEqualSet")]
        [Alias("IGE")]
        public SwitchParameter GE
        {
            get
            {
                return _binaryOperator == TokenKind.Ige;
            }

            set
            {
                _binaryOperator = TokenKind.Ige;
            }
        }

        /// <summary>
        /// Gets or sets case sensitive binary operator -cge.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CaseSensitiveGreaterOrEqualSet")]
        public SwitchParameter CGE
        {
            get
            {
                return _binaryOperator == TokenKind.Cge;
            }

            set
            {
                _binaryOperator = TokenKind.Cge;
            }
        }

        /// <summary>
        /// Gets or sets binary operator -LessOrEqual.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "LessOrEqualSet")]
        [Alias("ILE")]
        public SwitchParameter LE
        {
            get
            {
                return _binaryOperator == TokenKind.Ile;
            }

            set
            {
                _binaryOperator = TokenKind.Ile;
            }
        }

        /// <summary>
        /// Gets or sets case sensitive binary operator -cle.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CaseSensitiveLessOrEqualSet")]
        public SwitchParameter CLE
        {
            get
            {
                return _binaryOperator == TokenKind.Cle;
            }

            set
            {
                _binaryOperator = TokenKind.Cle;
            }
        }

        /// <summary>
        ///Gets or sets binary operator -Like.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "LikeSet")]
        [Alias("ILike")]
        public SwitchParameter Like
        {
            get
            {
                return _binaryOperator == TokenKind.Ilike;
            }

            set
            {
                _binaryOperator = TokenKind.Ilike;
            }
        }

        /// <summary>
        /// Gets or sets case sensitive binary operator -clike.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CaseSensitiveLikeSet")]
        public SwitchParameter CLike
        {
            get
            {
                return _binaryOperator == TokenKind.Clike;
            }

            set
            {
                _binaryOperator = TokenKind.Clike;
            }
        }

        /// <summary>
        /// Gets or sets binary operator -NotLike.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "NotLikeSet")]
        [Alias("INotLike")]
        public SwitchParameter NotLike
        {
            get
            {
                return false;
            }

            set
            {
                _binaryOperator = TokenKind.Inotlike;
            }
        }

        /// <summary>
        /// Gets or sets case sensitive binary operator -cnotlike.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CaseSensitiveNotLikeSet")]
        public SwitchParameter CNotLike
        {
            get
            {
                return _binaryOperator == TokenKind.Cnotlike;
            }

            set
            {
                _binaryOperator = TokenKind.Cnotlike;
            }
        }

        /// <summary>
        /// Get or sets binary operator -Match.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "MatchSet")]
        [Alias("IMatch")]
        public SwitchParameter Match
        {
            get
            {
                return _binaryOperator == TokenKind.Imatch;
            }

            set
            {
                _binaryOperator = TokenKind.Imatch;
            }
        }

        /// <summary>
        /// Gets or sets case sensitive binary operator -cmatch.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CaseSensitiveMatchSet")]
        public SwitchParameter CMatch
        {
            get
            {
                return _binaryOperator == TokenKind.Cmatch;
            }

            set
            {
                _binaryOperator = TokenKind.Cmatch;
            }
        }

        /// <summary>
        /// Gets or sets binary operator -NotMatch.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "NotMatchSet")]
        [Alias("INotMatch")]
        public SwitchParameter NotMatch
        {
            get
            {
                return _binaryOperator == TokenKind.Inotmatch;
            }

            set
            {
                _binaryOperator = TokenKind.Inotmatch;
            }
        }

        /// <summary>
        /// Gets or sets case sensitive binary operator -cnotmatch.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CaseSensitiveNotMatchSet")]
        public SwitchParameter CNotMatch
        {
            get
            {
                return _binaryOperator == TokenKind.Cnotmatch;
            }

            set
            {
                _binaryOperator = TokenKind.Cnotmatch;
            }
        }

        /// <summary>
        /// Gets or sets binary operator -Contains.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "ContainsSet")]
        [Alias("IContains")]
        public SwitchParameter Contains
        {
            get
            {
                return _binaryOperator == TokenKind.Icontains;
            }

            set
            {
                _binaryOperator = TokenKind.Icontains;
            }
        }

        /// <summary>
        /// Gets or sets case sensitive binary operator -ccontains.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CaseSensitiveContainsSet")]
        public SwitchParameter CContains
        {
            get
            {
                return _binaryOperator == TokenKind.Ccontains;
            }

            set
            {
                _binaryOperator = TokenKind.Ccontains;
            }
        }

        /// <summary>
        /// Gets or sets binary operator -NotContains.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "NotContainsSet")]
        [Alias("INotContains")]
        public SwitchParameter NotContains
        {
            get
            {
                return _binaryOperator == TokenKind.Inotcontains;
            }

            set
            {
                _binaryOperator = TokenKind.Inotcontains;
            }
        }

        /// <summary>
        /// Gets or sets case sensitive binary operator -cnotcontains.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CaseSensitiveNotContainsSet")]
        public SwitchParameter CNotContains
        {
            get
            {
                return _binaryOperator == TokenKind.Cnotcontains;
            }

            set
            {
                _binaryOperator = TokenKind.Cnotcontains;
            }
        }

        /// <summary>
        /// Gets or sets binary operator -In.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "InSet")]
        [Alias("IIn")]
        public SwitchParameter In
        {
            get
            {
                return _binaryOperator == TokenKind.In;
            }

            set
            {
                _binaryOperator = TokenKind.In;
            }
        }

        /// <summary>
        /// Gets or sets case sensitive binary operator -cin.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CaseSensitiveInSet")]
        public SwitchParameter CIn
        {
            get
            {
                return _binaryOperator == TokenKind.Cin;
            }

            set
            {
                _binaryOperator = TokenKind.Cin;
            }
        }

        /// <summary>
        /// Gets or sets binary operator -NotIn.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "NotInSet")]
        [Alias("INotIn")]
        public SwitchParameter NotIn
        {
            get
            {
                return _binaryOperator == TokenKind.Inotin;
            }

            set
            {
                _binaryOperator = TokenKind.Inotin;
            }
        }

        /// <summary>
        /// Gets or sets case sensitive binary operator -cnotin.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "CaseSensitiveNotInSet")]
        public SwitchParameter CNotIn
        {
            get
            {
                return _binaryOperator == TokenKind.Cnotin;
            }

            set
            {
                _binaryOperator = TokenKind.Cnotin;
            }
        }

        /// <summary>
        /// Gets or sets binary operator -Is.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "IsSet")]
        public SwitchParameter Is
        {
            get
            {
                return _binaryOperator == TokenKind.Is;
            }

            set
            {
                _binaryOperator = TokenKind.Is;
            }
        }

        /// <summary>
        /// Gets or sets binary operator -IsNot.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "IsNotSet")]
        public SwitchParameter IsNot
        {
            get
            {
                return _binaryOperator == TokenKind.IsNot;
            }

            set
            {
                _binaryOperator = TokenKind.IsNot;
            }
        }

        /// <summary>
        /// Gets or sets binary operator -Not.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "Not")]
        public SwitchParameter Not
        {
            get
            {
                return _binaryOperator == TokenKind.Not;
            }

            set
            {
                _binaryOperator = TokenKind.Not;
            }
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

        private static Func<object, object, object> GetCallSiteDelegateBoolean(ExpressionType expressionType, bool ignoreCase)
        {
            // flip 'lval' and 'rval' in the scenario '... | Where-Object property' so as to make it
            // equivalent to '... | Where-Object {$true -eq property}'. Because we want the property to
            // be compared under the bool context. So that '"string" | Where-Object Length' would behave
            // just like '"string" | Where-Object {$_.Length}'.
            var site = CallSite<Func<CallSite, object, object, object>>.Create(binder: PSBinaryOperationBinder.Get(expressionType, ignoreCase));
            return (x, y) => site.Target.Invoke(site, y, x);
        }

        private static Tuple<CallSite<Func<CallSite, object, IEnumerator>>, CallSite<Func<CallSite, object, object, object>>> GetContainsCallSites(bool ignoreCase)
        {
            var enumerableSite = CallSite<Func<CallSite, object, IEnumerator>>.Create(PSEnumerableBinder.Get());
            var equalSite =
                CallSite<Func<CallSite, object, object, object>>.Create(PSBinaryOperationBinder.Get(
                    ExpressionType.Equal, ignoreCase, scalarCompare: true));

            return Tuple.Create(enumerableSite, equalSite);
        }

        private void CheckLanguageMode()
        {
            if (Context.LanguageMode.Equals(PSLanguageMode.RestrictedLanguage))
            {
                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    InternalCommandStrings.OperationNotAllowedInRestrictedLanguageMode,
                    _binaryOperator);
                PSInvalidOperationException exception =
                    new PSInvalidOperationException(message);
                ThrowTerminatingError(new ErrorRecord(exception, "OperationNotAllowedInRestrictedLanguageMode", ErrorCategory.InvalidOperation, null));
            }
        }

        private object GetLikeRHSOperand(object operand)
        {
            if (!(operand is string val))
            {
                return operand;
            }

            var wildcardOptions = _binaryOperator == TokenKind.Ilike || _binaryOperator == TokenKind.Inotlike
                ? WildcardOptions.IgnoreCase
                : WildcardOptions.None;
            return WildcardPattern.Get(val, wildcardOptions);
        }

        /// <summary/>
        protected override void BeginProcessing()
        {
            if (_script != null)
            {
                return;
            }

            switch (_binaryOperator)
            {
                case TokenKind.Ieq:
                    if (!_forceBooleanEvaluation)
                    {
                        _operationDelegate = GetCallSiteDelegate(ExpressionType.Equal, ignoreCase: true);
                    }
                    else
                    {
                        _operationDelegate = GetCallSiteDelegateBoolean(ExpressionType.Equal, ignoreCase: true);
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
                case TokenKind.Not:
                    _operationDelegate = GetCallSiteDelegateBoolean(ExpressionType.NotEqual, ignoreCase: true);
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

        private DynamicPropertyGetter _propGetter;

        /// <summary>
        /// Execute the script block passing in the current pipeline object as
        /// it's only parameter.
        /// </summary>
        /// <exception cref="ParseException">Could not parse script.</exception>
        /// <exception cref="RuntimeException">See Pipeline.Invoke.</exception>
        /// <exception cref="ParameterBindingException">See Pipeline.Invoke.</exception>
        protected override void ProcessRecord()
        {
            if (_inputObject == AutomationNull.Value)
            {
                return;
            }

            if (_script != null)
            {
                object result = _script.DoInvokeReturnAsIs(
                    useLocalScope: false,
                    errorHandlingBehavior: ScriptBlock.ErrorHandlingBehavior.WriteToCurrentErrorPipe,
                    dollarUnder: InputObject,
                    input: new object[] { _inputObject },
                    scriptThis: AutomationNull.Value,
                    args: Array.Empty<object>());

                if (_toBoolSite.Target.Invoke(_toBoolSite, result))
                {
                    WriteObject(InputObject);
                }
            }
            else
            {
                // Both -Property and -Value need to be specified if the user specifies the binary operation
                if (_valueNotSpecified && ((_binaryOperator != TokenKind.Ieq && _binaryOperator != TokenKind.Not) || !_forceBooleanEvaluation))
                {
                    // The binary operation is specified explicitly by the user and the -Value parameter is
                    // not specified
                    ThrowTerminatingError(
                        ForEachObjectCommand.GenerateNameParameterError(
                            "Value",
                            InternalCommandStrings.ValueNotSpecifiedForWhereObject,
                            "ValueNotSpecifiedForWhereObject",
                            target: null));
                }

                // The binary operation needs to be specified if the user specifies both the -Property and -Value
                if (!_valueNotSpecified && (_binaryOperator == TokenKind.Ieq && _forceBooleanEvaluation))
                {
                    // The -Property and -Value are specified explicitly by the user but the binary operation is not
                    ThrowTerminatingError(
                        ForEachObjectCommand.GenerateNameParameterError(
                            "Operator",
                            InternalCommandStrings.OperatorNotSpecified,
                            "OperatorNotSpecified",
                            target: null));
                }

                bool strictModeWithError = false;
                object lvalue = GetValue(ref strictModeWithError);
                if (strictModeWithError)
                {
                    return;
                }

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
        /// Get the value based on the given property name.
        /// </summary>
        /// <returns>The value of the property.</returns>
        private object GetValue(ref bool error)
        {
            if (LanguagePrimitives.IsNull(InputObject))
            {
                if (Context.IsStrictVersion(2))
                {
                    WriteError(
                        ForEachObjectCommand.GenerateNameParameterError(
                            "InputObject",
                            InternalCommandStrings.InputObjectIsNull,
                            "InputObjectIsNull",
                            _inputObject,
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

            string resolvedPropertyName = null;
            bool isBlindDynamicAccess = false;

            ReadOnlyPSMemberInfoCollection<PSMemberInfo> members = GetMatchMembers();
            if (members.Count > 1)
            {
                StringBuilder possibleMatches = new StringBuilder();
                foreach (PSMemberInfo item in members)
                {
                    possibleMatches.Append(CultureInfo.InvariantCulture, $" {item.Name}");
                }

                WriteError(
                    ForEachObjectCommand.GenerateNameParameterError(
                        "Property",
                        InternalCommandStrings.AmbiguousPropertyOrMethodName,
                        "AmbiguousPropertyName",
                        _inputObject,
                        _property,
                        possibleMatches));
                error = true;
            }
            else if (members.Count == 0)
            {
                if ((InputObject.BaseObject is IDynamicMetaObjectProvider) &&
                    !WildcardPattern.ContainsWildcardCharacters(_property))
                {
                    // Let's just try a dynamic property access. Note that if it comes to
                    // depending on dynamic access, we are assuming it is a property; we
                    // don't have ETS info to tell us up front if it even exists or not,
                    // let alone if it is a method or something else.
                    //
                    // Note that this is "truly blind"--the name did not show up in
                    // GetDynamicMemberNames(), else it would show up as a dynamic member.

                    resolvedPropertyName = _property;
                    isBlindDynamicAccess = true;
                }
                else if (Context.IsStrictVersion(2))
                {
                    WriteError(ForEachObjectCommand.GenerateNameParameterError(
                        "Property",
                        InternalCommandStrings.PropertyNotFound,
                        "PropertyNotFound",
                        _inputObject,
                        _property));
                    error = true;
                }
            }
            else
            {
                resolvedPropertyName = members[0].Name;
            }

            if (!string.IsNullOrEmpty(resolvedPropertyName))
            {
                try
                {
                    return _propGetter.GetValue(_inputObject, resolvedPropertyName);
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
                    // For normal property accesses, we do not generate an error here. The problem
                    // for truly blind dynamic accesses (the member did not show up in
                    // GetDynamicMemberNames) is that we can't tell the difference between "it
                    // failed because the property does not exist" (let's call this case
                    // 1) and "it failed because accessing it actually threw some exception" (let's
                    // call that case 2).
                    //
                    // PowerShell behavior for normal (non-dynamic) properties is different for
                    // these two cases: case 1 gets an error (if strict mode is on) (which is
                    // possible because the ETS tells us up front if the property exists or not),
                    // and case 2 does not. (For normal properties, this catch block /is/ case 2.)
                    //
                    // For IDMOPs, we have the chance to attempt a "blind" access, but the cost is
                    // that we must have the same response to both cases (because we cannot
                    // distinguish between the two). So we have to make a choice: we can either
                    // swallow ALL errors (including "The property 'Blarg' does not exist"), or
                    // expose them all.
                    //
                    // Here, for truly blind dynamic access, we choose to preserve the behavior of
                    // showing "The property 'Blarg' does not exist" (case 1) errors than to
                    // suppress "FooException thrown when accessing Bloop property" (case
                    // 2) errors.
                    if (isBlindDynamicAccess && Context.IsStrictVersion(2))
                    {
                        WriteError(new ErrorRecord(ex,
                                                   "DynamicPropertyAccessFailed_" + _property,
                                                   ErrorCategory.InvalidOperation,
                                                   _inputObject));

                        error = true;
                    }
                    else
                    {
                        // When the property is not gettable or it throws an exception
                        return null;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Get the matched PSMembers.
        /// </summary>
        /// <returns>Matched PSMembers.</returns>
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
    [Cmdlet(VerbsCommon.Set, "PSDebug", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096959")]
    public sealed class SetPSDebugCommand : PSCmdlet
    {
        /// <summary>
        /// Gets or sets the script tracing level.
        /// </summary>
        [Parameter(ParameterSetName = "on")]
        [ValidateRange(0, 2)]
        public int Trace
        {
            get
            {
                return _trace;
            }

            set
            {
                _trace = value;
            }
        }

        private int _trace = -1;

        /// <summary>
        /// Gets or sets stepping on and off.
        /// </summary>
        [Parameter(ParameterSetName = "on")]
        public SwitchParameter Step
        {
            get
            {
                return (SwitchParameter)_step;
            }

            set
            {
                _step = value;
            }
        }

        private bool? _step;

        /// <summary>
        /// Gets or sets strict mode on and off.
        /// </summary>
        [Parameter(ParameterSetName = "on")]
        public SwitchParameter Strict
        {
            get
            {
                return (SwitchParameter)_strict;
            }

            set
            {
                _strict = value;
            }
        }

        private bool? _strict;

        /// <summary>
        /// Gets or sets all script debugging features off.
        /// </summary>
        [Parameter(ParameterSetName = "off")]
        public SwitchParameter Off
        {
            get
            {
                return _off;
            }

            set
            {
                _off = value;
            }
        }

        private bool _off;

        /// <summary>
        /// Execute the begin scriptblock at the start of processing.
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
                {
                    Context.EngineSessionState.GlobalScope.StrictModeVersion = new Version((bool)_strict ? 1 : 0, 0);
                }
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
    [Cmdlet(VerbsCommon.Set, "StrictMode", DefaultParameterSetName = "Version", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096804")]
    public class SetStrictModeCommand : PSCmdlet
    {
        /// <summary>
        /// Gets or sets strict mode off.
        /// </summary>
        [Parameter(ParameterSetName = "Off", Mandatory = true)]
        public SwitchParameter Off
        {
            get
            {
                return _off;
            }

            set
            {
                _off = value;
            }
        }

        private SwitchParameter _off;

        /// <summary>
        /// Handle 'latest', which we interpret to be the current version of PowerShell.
        /// </summary>
        private sealed class ArgumentToPSVersionTransformationAttribute : ArgumentToVersionTransformationAttribute
        {
            protected override bool TryConvertFromString(string versionString, [NotNullWhen(true)] out Version version)
            {
                if (string.Equals("latest", versionString, StringComparison.OrdinalIgnoreCase))
                {
                    version = PSVersionInfo.PSVersion;
                    return true;
                }

                return base.TryConvertFromString(versionString, out version);
            }
        }

        private sealed class ValidateVersionAttribute : ValidateArgumentsAttribute
        {
            protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
            {
                Version version = arguments as Version;
                if (!PSVersionInfo.IsValidPSVersion(version))
                {
                    // No conversion succeeded so throw and exception...
                    throw new ValidationMetadataException(
                        "InvalidPSVersion",
                        null,
                        Metadata.ValidateVersionFailure,
                        arguments);
                }
            }
        }

        /// <summary>
        /// Gets or sets strict mode in the current scope.
        /// </summary>
        [Parameter(ParameterSetName = "Version", Mandatory = true)]
        [ArgumentCompleter(typeof(StrictModeVersionArgumentCompleter))]
        [ArgumentToPSVersionTransformation]
        [ValidateVersion]
        [Alias("v")]
        public Version Version
        {
            get
            {
                return _version;
            }

            set
            {
                _version = value;
            }
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

    /// <summary>
    /// Provides argument completion for StrictMode Version parameter.
    /// </summary>
    public class StrictModeVersionArgumentCompleter : IArgumentCompleter
    {
        private static readonly string[] s_strictModeVersions = new string[] { "Latest", "3.0", "2.0", "1.0" };

        /// <summary>
        /// Returns completion results for version parameter.
        /// </summary>
        /// <param name="commandName">The command name.</param>
        /// <param name="parameterName">The parameter name.</param>
        /// <param name="wordToComplete">The word to complete.</param>
        /// <param name="commandAst">The command AST.</param>
        /// <param name="fakeBoundParameters">The fake bound parameters.</param>
        /// <returns>List of Completion Results.</returns>
        public IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var strictModeVersionPattern = WildcardPattern.Get(wordToComplete + "*", WildcardOptions.IgnoreCase);

            foreach (string version in s_strictModeVersions)
            {
                if (strictModeVersionPattern.IsMatch(version))
                {
                    yield return new CompletionResult(version);
                }
            }
        }
    }

    #endregion Set-StrictMode

    #endregion Built-in cmdlets that are used by or require direct access to the engine.
}
