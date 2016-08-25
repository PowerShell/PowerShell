// 
//  Copyright (c) Microsoft Corporation. All rights reserved. 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//  

namespace Microsoft.PowerShell.PackageManagement.Cmdlets {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PackageManagement.Internal.Utility.Collections;
    using Microsoft.PackageManagement.Internal.Utility.Extensions;
    using Microsoft.PackageManagement.Internal.Utility.Platform;    
    using Utility;

    public delegate bool OnMainThread(Func<bool> onMainThreadDelegate);

    public abstract class AsyncCmdlet : PSCmdlet, IDynamicParameters, IDisposable {
        protected enum AsyncCmdletState {
        Unknown,
        GenerateParameters,
        BeginProcess,
        BeginProcessCompleted,
        ProcessRecord,
        ProcessRecordCompleted,
        EndProcess,
        EndProcessCompleted,
        StopProcess,
        StopProcessCompleted
    }

        private const BindingFlags BindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public;
        private ProgressTracker _activeProgressId;
        protected AsyncCmdletState CmdletState = AsyncCmdletState.Unknown;
        protected CancellationTokenSource CancellationEvent = new CancellationTokenSource();
        private bool _consumedDynamicParameters;
        private RuntimeDefinedParameterDictionary _dynamicParameters;
        protected bool ErrorState;
        private BlockingCollection<TaskCompletionSource<bool>> _heldMessages;
        private BlockingCollection<TaskCompletionSource<bool>> _messages;
        private int _nextProgressId = 1;
        private Stopwatch _stopwatch;
        private Dictionary<string, object> _unboundArguments;

        private readonly SortedSet<string> _errors = new SortedSet<string>();
        private readonly List<ProgressTracker> _progressTrackers = new List<ProgressTracker>();
        private readonly SortedSet<string> _warnings = new SortedSet<string>();

        private PipelineStoppedException _pipelineStopped;

        private ManualResetEvent ReentrantLock {
            get {
                //     ...
                //     In the future, I suspect that
                //     we need to make this 'RunSpace-static' (or 'host-static'?) too
                //     if the same set of cmdlets ends up loaded in another
                //     runspace, and we don't want a lock in one to affect
                //     another.

                return GetType().GetOrAdd(() => new ManualResetEvent(false), "ReentrancyLock");
            }
        }

#if DEBUG
        private static object __lock = new object();
        private void Log(string category, string text) {
            lock (__lock) {
                NativeMethods.OutputDebugString("[Cmdlet:{0}][Thread:{1}][{2}] {3}".format(GetType().Name, Thread.CurrentThread.ManagedThreadId, category, text));
            }
        }
#endif

        /// <summary>
        ///     Manages the reentrancy lock for cmdlets
        ///     This is abstracted here because (at this point)
        ///     we need this to be static, but per-cmdlet class.
        /// </summary>
        protected bool IsReentrantLocked {
            get {
                return ReentrantLock.WaitOne(0);
            }
            set {
                if (value) {
                    ReentrantLock.Set();
                } else {
                    ReentrantLock.Reset();
                }
            }
        }

        protected bool Confirm {
            get {
                return MyInvocation.BoundParameters.ContainsKey(Constants.Parameters.ConfirmParameter) && (SwitchParameter)MyInvocation.BoundParameters[Constants.Parameters.ConfirmParameter];
            }
        }

        public bool WhatIf {
            get {
                return MyInvocation.BoundParameters.ContainsKey(Constants.Parameters.WhatIfParameter) && (SwitchParameter)MyInvocation.BoundParameters[Constants.Parameters.WhatIfParameter];
            }
        }

        protected static bool IsInitialized {get; set;}

        protected bool IsInvocation {
            get {
                // this seems to be more reliable than checking the Invocation Line.
                return MyInvocation != null && MyInvocation.PipelineLength > 0;
            }
        }

        /// <summary>
        ///     The provider can query to see if the operation has been cancelled.
        ///     This provides for a gentle way for the caller to notify the callee that
        ///     they don't want any more results.
        /// </summary>
        /// <value>returns TRUE if the operation has been cancelled.</value>
        public bool IsCanceled {
            get {
                return Stopping || CancellationEvent == null || CancellationEvent.IsCancellationRequested;
            }
        }

        protected bool HasErrors {
            get {
                return _errors.Any();
            }
        }

        protected Dictionary<string, object> UnboundArguments {
            get {
                if (_unboundArguments == null && IsReentrantLocked) {
                    _unboundArguments = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                    try {
                        var context = TryGetProperty(this, "Context");
                        var processor = TryGetProperty(context, "CurrentCommandProcessor");
                        var parameterBinder = TryGetProperty(processor, "CmdletParameterBinderController");
                        var args = TryGetProperty(parameterBinder, "UnboundArguments") as IEnumerable;

                        if (args != null) {
                            var currentParameterName = string.Empty;
                            int i = 0;
                            foreach (var arg in args) {
                                var isParameterName = TryGetProperty(arg, "ParameterNameSpecified");
                                if (isParameterName != null && true.Equals(isParameterName)) {
                                    var parameterName = TryGetProperty(arg, "ParameterName");

                                    if (parameterName != null) {
                                        currentParameterName = parameterName.ToString();

                                        // add it now, just in case it's value isn't set (or it's a switch)
                                        _unboundArguments.AddOrSet(currentParameterName, (object)TryGetProperty(arg, "ArgumentValue"));
                                        continue;
                                    }
                                }

                                // not a parameter name.
                                // treat as a value
                                var parameterValue = TryGetProperty(arg, "ArgumentValue");

                                if (string.IsNullOrWhiteSpace(currentParameterName)) {
                                    _unboundArguments.AddOrSet("unbound_" + (i++), parameterValue);
                                } else {
                                    _unboundArguments.AddOrSet(currentParameterName, parameterValue);
                                }

                                // clear the current parameter name
                                currentParameterName = null;
                            }
                        }
                    } catch (Exception e) {
                        e.Dump();
                    }
                }
                return _unboundArguments;
            }
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual bool BeginProcessingAsync() {
            return false;
        }

        public virtual bool EndProcessingAsync() {
            return false;
        }

        public virtual bool StopProcessingAsync() {
            return false;
        }

        public virtual bool ProcessRecordAsync() {
            return false;
        }

        public string ResolveExistingFilePath(string filePath) {
            ProviderInfo providerInfo = null;
            var files = GetResolvedProviderPathFromPSPath(filePath, out providerInfo).ToArray();
            switch (files.Length) {
                case 0:
                    // none found
                    Error(Constants.Errors.FileNotFound, filePath);
                    break;

                case 1:
                    if (File.Exists(files[0])) {
                        return files[0];
                    }
                    Error(Constants.Errors.FileNotFound, filePath);
                    break;

                default:
                    Error(Constants.Errors.MoreThanOneFileMatched, files.JoinWithComma(), filePath);
                    break;
            }
            return null;
        }

        public string ResolveExistingFolderPath(string folderPath) {
            return ResolveExistingFolderPath(folderPath, false);
        }

        public string ResolveExistingFolderPath(string folderPath, bool isThrow) {
            ProviderInfo providerInfo = null;
            string[] files = null;

            // we may get an error if the path does not exist.
            // we should not error out because the provider may want to create that path.
            try
            {
                files = GetResolvedProviderPathFromPSPath(folderPath, out providerInfo).ToArray();
            }
            catch (ItemNotFoundException itemNotFoundEx)
            {
                if (isThrow) {
                    throw;
                }
                // this means path does not exist but the provider may want to create it so we should allow
                files = new string[1] { itemNotFoundEx.ItemName };
            }
            
            switch (files.Length) {
                case 0:
                    // none found
                    Error(Constants.Errors.FolderNotFound, folderPath);
                    break;

                case 1:
                    if (!Directory.Exists(files[0])) {
                        Verbose(String.Format(CultureInfo.CurrentCulture, Resources.Messages.FolderNotFound, files[0]));
                    }
                    return files[0];

                default:
                    Error(Constants.Errors.MoreThanOneFolderMatched, files.JoinWithComma(), folderPath);
                    break;
            }
            return null;
        }

        public string ResolvePath(string path) {
            return GetUnresolvedProviderPathFromPSPath(path);
        }

        public string DropMsgPrefix(string messageText) {
            if (string.IsNullOrWhiteSpace(messageText)) {
                return messageText;
            }
            return messageText.IndexOf("MSG:", StringComparison.OrdinalIgnoreCase) == 0 ? messageText.Substring(4) : messageText;
        }

        public bool Warning(string messageText) {
            return Warning(messageText, Constants.NoParameters);
        }

        internal bool Warning(ErrorMessage message) {
            if (message == null) {
                throw new ArgumentNullException("message");
            }
            return Warning(message.Resource, Constants.NoParameters);
        }

        internal bool Warning(ErrorMessage message, params object[] args) {
            if (message == null) {
                throw new ArgumentNullException("message");
            }

            return Warning(message.Resource, args);
        }

        public bool Warning(string messageText, params object[] args) {
            if (IsInvocation) {
                WriteWarning(FormatMessageString(messageText, args));
            }
            // rather than wait on the result of the async WriteVerbose,
            // we'll just return the stopping state.
            return IsCanceled;
        }

        internal bool Error(ErrorMessage errorMessage) {
            return Error(errorMessage.Resource, errorMessage.Category.ToString(), null, errorMessage.Resource);
        }

        internal bool Error(ErrorMessage errorMessage, params object[] args) {
            return Error(errorMessage.Resource, errorMessage.Category.ToString(), null, FormatMessageString(errorMessage.Resource, args));
        }

        public bool Error(string id, string category, string targetObjectValue, string messageText) {
            return Error(id, category, targetObjectValue, messageText, Constants.NoParameters);
        }

        public bool Error(string id, string category, string targetObjectValue, string messageText, params object[] args) {
            if (IsInvocation) {
                var errorMessage = FormatMessageString(messageText, args);

                if (!_errors.Contains(errorMessage)) {

                    if (!HasErrors) {
                        // we only *show* the very first error we get.
                        // any more, we just toss them in the collection and
                        // maybe we'll worry about them later.
                        ErrorCategory errorCategory;
                        if (!Enum.TryParse(category, true, out errorCategory)) {
                            errorCategory = ErrorCategory.NotSpecified;
                        }
                        _errors.Add(errorMessage);
                        try {
#if DEBUG
                            Log("ERROR",errorMessage);
#endif
                            //Replaced Wait() with ContinueWith(Cancel) to avoid hang if the error message comes in too early, e.g. GetDynamicParameters(), etc.
                            WriteError(new ErrorRecord(new Exception(errorMessage), DropMsgPrefix(id), errorCategory, string.IsNullOrWhiteSpace(targetObjectValue) ? (object)this : targetObjectValue)).Wait(3000);
                        } catch {
                            // this will throw if the provider thread abends before we get back our result.
                        }
                    }
                }
            }
            Cancel();

            // rather than wait on the result of the async'd message,
            // we'll just return the stopping state.
            return true;
        }

        internal bool NonTerminatingError(ErrorMessage errorMessage)
        {
            return NonTerminatingError(errorMessage.Resource, errorMessage.Category.ToString(), null, errorMessage.Resource);
        }

        internal bool NonTerminatingError(ErrorMessage errorMessage, params object[] args)
        {
            return NonTerminatingError(errorMessage.Resource, errorMessage.Category.ToString(), null, FormatMessageString(errorMessage.Resource, args));
        }

        public bool NonTerminatingError(string id, string category, string targetObjectValue, string messageText)
        {
            return NonTerminatingError(id, category, targetObjectValue, messageText, Constants.NoParameters);
        }

        public bool NonTerminatingError(string id, string category, string targetObjectValue, string messageText, params object[] args)
        {
            if (IsInvocation) {
                var errorMessage = FormatMessageString(messageText, args);

                if (!_errors.Contains(errorMessage)) {
                    ErrorCategory errorCategory;
                    if (!Enum.TryParse(category, true, out errorCategory)) {
                        errorCategory = ErrorCategory.NotSpecified;
                    }
                    try {
#if DEBUG
                        Log("NON TERMINATING ERROR", errorMessage);
#endif
                        WriteError(new ErrorRecord(new Exception(errorMessage), DropMsgPrefix(id), errorCategory, string.IsNullOrWhiteSpace(targetObjectValue) ? (object)this : targetObjectValue));
                    }
                    catch {
                       // this will throw if the provider thread abends before we get back our result.
                    }
               }
               _errors.Add(errorMessage);
            }
            // rather than wait on the result of the async'd message,
            // we'll just return the stopping state.
            return IsCanceled;
        }

        public bool Message(string messageText) {
            return Message(messageText, Constants.NoParameters);
        }

        public bool Message(string messageText, params object[] args) {
            // queue the message to run on the main thread.
            if (IsInvocation) {
                //  QueueMessage(() => Host.UI.WriteLine("{0}::{1}".format(code, message.formatWithIEnumerable(objects))));
                // Message is going to go to the verbose channel
                // and Verbose will only be output if VeryVerbose is true.
#if DEBUG
                Log("Message",FormatMessageString(messageText, args));
#endif

                WriteVerbose(FormatMessageString(messageText, args));
            }
            // rather than wait on the result of the async WriteVerbose,
            // we'll just return the stopping state.
            return IsCanceled;
        }

        public bool Verbose(string messageText) {
            return Verbose(messageText, Constants.NoParameters);
        }

        public bool Verbose(string messageText, params object[] args) {
            if (IsInvocation) {
                // Message is going to go to the verbose channel
                // and Verbose will only be output if VeryVerbose is true.
                WriteVerbose(FormatMessageString(messageText, args));
#if DEBUG
                Log("Verbose",FormatMessageString(messageText, args));
#endif
            }
            // rather than wait on the result of the async WriteVerbose,
            // we'll just return the stopping state.
            return IsCanceled;
        }

        public bool Debug(string messageText) {
            return Debug(messageText, Constants.NoParameters);
        }

        public bool Debug(string messageText, params object[] args) {
            if (IsInvocation) {
                if (_stopwatch == null) {
                    _stopwatch = new Stopwatch();
                    _stopwatch.Start();
                }

#if DEBUG
                Log("Debug",FormatMessageString(messageText, args));
#endif

                var task = WriteDebug("{0} {1}".format(_stopwatch.Elapsed, FormatMessageString(messageText, args)));
#if WAIT_FOR_DEBUG                
                if (_asyncCmdletState > AsyncCmdletState.BeginProcess) {
                    task.Wait();
                }
#endif 
            }

            // rather than wait on the result of the async WriteVerbose,
            // we'll just return the stopping state.
            return IsCanceled;
        }

        public int StartProgress(int parentActivityId, string message) {
            return StartProgress(parentActivityId, message, Constants.NoParameters);
        }

        public int StartProgress(int parentActivityId, string message, params object[] args) {
            if (IsInvocation) {
                lock (_progressTrackers) {
                    ProgressTracker parent = null;

                    if (parentActivityId <= 0) {
                        if (_activeProgressId != null) {
                            parent = _activeProgressId;
                        }
                    } else {
                        parent = _progressTrackers.FirstOrDefault(each => each.Id == parentActivityId);
                    }
                                        
                    var p = new ProgressTracker() {
                        Activity = FormatMessageString(message, args),
                        Id = _nextProgressId++,
                        Parent = parent
                    };
                    _activeProgressId = p;

                    if (parent != null) {
                        parent.Children.Add(p);
                    }
                    _progressTrackers.Add(p);

                    WriteProgress(new ProgressRecord(p.Id, p.Activity, " ") {
                        PercentComplete = 0,
                        RecordType = ProgressRecordType.Processing
                    });
                    return p.Id;
                }
            }
            return 0;
        }

        /// <summary>
        /// Write progress using powershell write progress directly
        /// </summary>
        /// <param name="activity">Specifies the first line of text in the heading above the status bar.</param>
        /// <param name="messageText">Corresponds to status on write-progress. Specifies the second line of text in the heading above the status bar. This text describes current state of the activity.</param>
        /// <param name="activityId">
        /// Corresponds to id on write-progress. Specifies an ID that distinguishes each progress bar from the others.
        /// Use this parameter when you are creating more than one progress bar in a single command.
        /// If the progress bars do not have different IDs, they are superimposed instead of being displayed in a series.
        /// </param>
        /// <param name="progressPercentage">Specifies the percentage of the activity that is completed. Use the value -1 if the percentage complete is unknown or not applicable.</param>
        /// <param name="secondsRemaining">Specifies the projected number of seconds remaining until the activity is completed. Use the value -1 if the number of seconds remaining is unknown or not applicable.</param>
        /// <param name="currentOperation">Specifies the line of text below the progress bar. This text describes the operation that is currently taking place.</param>
        /// <param name="parentActivityId">Identifies the parent activity of the current activity. Use the value -1 if the current activity has no parent activity.</param>
        /// <param name="completed">Indicates whether the progress bar is visible</param>
        /// <returns></returns>
        public bool Progress(string activity, string messageText, int activityId, int progressPercentage, int secondsRemaining, string currentOperation, int parentActivityId, bool completed)
        {
            lock (_progressTrackers)
            {
                if (IsInvocation)
                {
                    // make sure percent complete is not greater than 100
                    if (progressPercentage >= 100)
                    {
                        progressPercentage = 100;
                        completed = true;
                    }

                    if (progressPercentage < 0)
                    {
                        // set any negative to -1, which means percent complete not applicable
                        progressPercentage = -1;
                    }                    

                    if (string.IsNullOrWhiteSpace(messageText))
                    {
                        if (completed)
                        {
                            messageText = string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.Messages.Completed);
                        }
                        else
                        {
                            messageText = string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.Messages.Processing);
                        }
                    }

                    // call the powershell progressrecord directly
                    WriteProgress(new ProgressRecord(activityId, activity, messageText)
                    {
                        PercentComplete = progressPercentage,
                        RecordType = completed ? ProgressRecordType.Completed : ProgressRecordType.Processing,
                        SecondsRemaining = secondsRemaining,
                        CurrentOperation = currentOperation,
                        ParentActivityId = parentActivityId
                    });
                }
            }

            return IsCanceled;
        }

        public bool Progress(int activityId, int progressPercentage, string messageText) {
            return Progress(activityId, progressPercentage, messageText, Constants.NoParameters);
        }

        public bool Progress(int activityId, int progressPercentage, string messageText, params object[] args) {
            lock (_progressTrackers) {
                if (IsInvocation) {
                    var p = _progressTrackers.FirstOrDefault(each => each.Id == activityId);
                    if (p != null) {
                        if (progressPercentage >= 100) {
                            progressPercentage = 100;
                        }

                        WriteProgress(new ProgressRecord(p.Id, p.Activity, FormatMessageString(messageText, args)) {
                            ParentActivityId = p.Parent != null ? p.Parent.Id : 0,
                            PercentComplete = progressPercentage,
                            RecordType = ProgressRecordType.Processing
                        });

                        if (progressPercentage >= 100) {
                            return CompleteProgress(activityId, true);
                        }
                    }
                }
            }
            // rather than wait on the result of the async WriteVerbose,
            // we'll just return the stopping state.
            return IsCanceled;
        }

        public bool CompleteProgress(int activityId, bool isSuccessful) {
            lock (_progressTrackers) {
                if (IsInvocation) {

                    var p = _progressTrackers.FirstOrDefault(each => each.Id == activityId);
                    if (p != null) {
                        // need to clone to avoid collection modification while iterating
                        var cloneChildrenList = new List<ProgressTracker>(p.Children);

                        // complete all of this trackers kids.
                        foreach (var child in cloneChildrenList)
                        {
                            CompleteProgress(child.Id, isSuccessful);
                        }

                        if (p.Parent != null) {
                            p.Parent.Children.Remove(p);
                        }
                        _progressTrackers.Remove(p);
                        if (_messages == null) {
                            base.WriteProgress(new ProgressRecord(p.Id, p.Activity, "Completed.") {
                                ParentActivityId = p.Parent != null ? p.Parent.Id : 0,
                                PercentComplete = 100,
                                RecordType = ProgressRecordType.Completed
                            });
                        } else {
                            WriteProgress(new ProgressRecord(p.Id, p.Activity, "Completed.") {
                                ParentActivityId = p.Parent != null ? p.Parent.Id : 0,
                                PercentComplete = 100,
                                RecordType = ProgressRecordType.Completed
                            });
                        }
                    }
                }
            }

            // rather than wait on the result of the async WriteVerbose,
            // we'll just return the stopping state.
            return IsCanceled;
        }

        public virtual string GetMessageString(string messageText, string defaultText) {
            return null;
        }

        public string FormatMessageString(string messageText, params object[] args) {
            if (string.IsNullOrWhiteSpace(messageText)) {
                return string.Empty;
            }

            if (messageText.IndexOf(Microsoft.PackageManagement.Internal.Constants.MSGPrefix, StringComparison.CurrentCultureIgnoreCase) == 0) {
                messageText = GetMessageString(messageText.Substring(Microsoft.PackageManagement.Internal.Constants.MSGPrefix.Length), messageText) ?? messageText;
            }

            return args == null || args.Length == 0 ? messageText : messageText.format(args);
        }

        protected virtual void Init() {
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                if (CancellationEvent != null) {
                    CancellationEvent.Dispose();
                    CancellationEvent = null;
                }

                // According to http://msdn.microsoft.com/en-us/library/windows/desktop/ms714463(v=vs.85).aspx
                // Powershell will dispose the cmdlet if it implements IDisposable.

                if (_messages != null) {
                    _messages.Dispose();
                    _messages = null;
                }
            }
        }

        #region Dynamic Parameters

        public virtual RuntimeDefinedParameterDictionary DynamicParameterDictionary {
            get {
                return _dynamicParameters ?? (_dynamicParameters = new RuntimeDefinedParameterDictionary());
            }
        }

        public object GetDynamicParameters() {
            if (DynamicParameterDictionary.IsNullOrEmpty()) {
                // this is because in cmdletwith provider we check for the state to determine the error we should throw
                // so this state should be at least generate parameters and not unknown.
                // otherwise, command like: Install-Package -Provider nuget -Destination C:\test -Name jquery -Source wrongsource
                // will give us wrong error that the dynamic parameter destination does not exist even though the real error is
                // that the source is wrong.
                CmdletState = AsyncCmdletState.GenerateParameters;
                if (IsOverridden(Constants.Methods.GenerateDynamicParametersMethod)) {
                    AsyncRun(GenerateDynamicParameters);
                }
            }

            return DynamicParameterDictionary;
        }

        protected T GetDynamicParameterValue<T>(string parameterName) {
            if (DynamicParameterDictionary.ContainsKey(parameterName)) {
                var p = DynamicParameterDictionary[parameterName];
                if (p.IsSet) {
                    if (typeof (T) == typeof (string[])) {
                        if (p.Value == null) {
                            return default(T);
                        }

                        if (p.Value is string[]) {
                            return (T)p.Value;
                        }

                        if (p.Value is string) {
                            return (T)(object)new string[1] {p.Value.ToString()};
                        }

                        if (p.Value is IEnumerable) {
                            return (T)(object)((IEnumerable)p.Value).Cast<object>().Select(each => each.ToString()).ToArray();
                        }

                        // weird, can't get a collection from whatever is here.
                        return (T)(object)new string[1] {p.Value.ToString()};
                    }

                    if (typeof (T) == typeof (string)) {
                        if (p.Value == null) {
                            return default(T);
                        }
                        return (T)(object)p.Value.ToString();
                    }

                    return (T)p.Value;
                }
            }
            return default(T);
        }

        public virtual bool GenerateDynamicParameters() {
            return true;
        }

        public virtual bool ConsumeDynamicParameters() {
            return true;
        }

        #endregion

        #region cmdlet processing

        private void ProcessHeldMessages() {
            if (_heldMessages != null && _heldMessages.Count > 0) {
                foreach (var msg in _heldMessages.GetConsumingEnumerable().Where(msg => msg != null)) {
                    InvokeMessage(msg);
                }
                _heldMessages.Dispose();
            }
            _heldMessages = null;
        }

        private void AsyncRun(Func<bool> asyncAction) {
            try {
                using (_messages = new BlockingCollection<TaskCompletionSource<bool>>()) {
                    // spawn the activity off in another thread.
                    Task.Factory.StartNew(() => {
                        try {
                            if (!IsInitialized) {
                                Init();
                            }

                            return asyncAction();
                        } catch (Exception e) {
                            Error(Constants.Errors.UnhandledException, e.Message, e.GetType().Name, e.StackTrace);
                        } finally {
                            // when the task is done, mark the msg queue as complete
                            if (_messages != null) {
                                _messages.CompleteAdding();
                            }
                        }
                        return false;
                    }, TaskCreationOptions.LongRunning);


                    // process the queue of messages back in the main thread so that they
                    // can properly access the non-thread-safe-things in cmdlet
                    foreach (var message in _messages.GetBlockingEnumerable(CancellationEvent.Token)) {
                        InvokeMessage(message);
                    }
                }

                if (_pipelineStopped != null) {
                    throw _pipelineStopped;
                }
            } finally {
                _messages = null;
                _pipelineStopped = null;
            }

        }

        private bool IsOverridden(string functionName) {
            return GetType().GetMethod(functionName).DeclaringType != typeof (AsyncCmdlet);
        }

        protected override sealed void BeginProcessing() {
#if DEBUG
            Log("BeginProcessing","[{0}/{1}] �{2}�".format( MyInvocation.PipelineLength, MyInvocation.PipelinePosition, MyInvocation.Line));
#endif
            try {
                CmdletState = AsyncCmdletState.BeginProcess;

                ProcessHeldMessages();

                if (IsCanceled) {
                    return;
                }

                // let's not even bother doing all this if they didn't even
                // override the method.
                if (IsOverridden(Constants.Methods.BeginProcessingAsyncMethod)) {
                    // just before we kick stuff off, let's make sure we consume the dynamic parameters
                    if (!_consumedDynamicParameters) {
                        ConsumeDynamicParameters();
                        _consumedDynamicParameters = true;
                    }
                    // just use our async/message pump to handle this activity
                    AsyncRun(BeginProcessingAsync);
                }
            } finally {
                if (CmdletState == AsyncCmdletState.BeginProcess) {
                    // If the state was changed elsewhere, don't assume that we're in the next state
                    CmdletState = AsyncCmdletState.BeginProcessCompleted;
                }
            }
        }

        protected override sealed void ProcessRecord() {
#if DEBUG
            Log("ProcessRecord", "[{0}/{1}] �{2}�".format(  MyInvocation.PipelineLength, MyInvocation.PipelinePosition, MyInvocation.Line));
#endif

            try {
                CmdletState = AsyncCmdletState.ProcessRecord;
                ProcessHeldMessages();

                // let's not even bother doing all this if they didn't even
                // override the method.
                if (IsOverridden(Constants.Methods.ProcessRecordAsyncMethod)) {
                    // just before we kick stuff off, let's make sure we consume the dynamic parameters
                    if (!_consumedDynamicParameters) {
                        ConsumeDynamicParameters();
                        _consumedDynamicParameters = true;
                    }

                    // just use our async/message pump to handle this activity
                    AsyncRun(ProcessRecordAsync);
                }
            } finally {
                if (CmdletState == AsyncCmdletState.ProcessRecord) {
                    // If the state was changed elsewhere, don't assume that we're in the next state
                    CmdletState = AsyncCmdletState.ProcessRecordCompleted;
                }
            }
        }

        protected override sealed void EndProcessing() {
#if DEBUG
            Log("EndProcessing","[{0}/{1}] �{2}�".format(  MyInvocation.PipelineLength, MyInvocation.PipelinePosition, MyInvocation.Line));
#endif

            try {
                CmdletState = AsyncCmdletState.EndProcess;
                ProcessHeldMessages();

                // let's not even bother doing all this if they didn't even
                // override the method.
                if (IsOverridden(Constants.Methods.EndProcessingAsyncMethod)) {
                    // just before we kick stuff off, let's make sure we consume the dynamic parameters
                    if (!_consumedDynamicParameters) {
                        ConsumeDynamicParameters();
                        _consumedDynamicParameters = true;
                    }

                    // just use our async/message pump to handle this activity
                    AsyncRun(EndProcessingAsync);
                }

                // make sure that we mark progress complete.
                if (_progressTrackers.Any()) {
                    AllProgressComplete();
                }
            } finally {
                if (CmdletState == AsyncCmdletState.EndProcess) {
                    // If the state was changed elsewhere, don't assume that we're in the next state
                    CmdletState = AsyncCmdletState.EndProcessCompleted;
                }
            }
        }

        protected override sealed void StopProcessing() {
#if DEBUG
            Log("StopProcessing","[{0}/{1}] �{2}�".format(  MyInvocation.PipelineLength, MyInvocation.PipelinePosition, MyInvocation.Line));
#endif

            try {
                CmdletState = AsyncCmdletState.StopProcess;
                Cancel();
                // let's not even bother doing all this if they didn't even
                // override the method.
                if (IsOverridden(Constants.Methods.StopProcessingAsyncMethod)) {
                    // just use our async/message pump to handle this activity
                    AsyncRun(StopProcessingAsync);
                }
                if (_progressTrackers.Any()) {
                    AllProgressComplete();
                }
            } finally {
                if (CmdletState == AsyncCmdletState.StopProcess) {
                    CmdletState = AsyncCmdletState.StopProcessCompleted;
                }
            }
        }

        public void Cancel() {
            // notify anyone listening that we're stopping this call.
            if (CancellationEvent != null) {
                CancellationEvent.Cancel();
            }
        }

        #endregion

        #region progress

        public new Task<bool> WriteProgress(ProgressRecord progressRecord) {
            return QueueMessage(() => {
                if (!IsCanceled) {
                    base.WriteProgress(progressRecord);
                }
            });
        }

        public Task<bool> AllProgressComplete() {
            lock (_progressTrackers) {
                while (_progressTrackers.Any()) {
                    CompleteProgress(_progressTrackers.FirstOrDefault().Id, true);
                }
            }
            return IsCanceled.AsResultTask();
        }

        #endregion

        #region Async/Messaging

        protected bool IsProcessing {
            get {
                return CmdletState == AsyncCmdletState.BeginProcess || CmdletState == AsyncCmdletState.ProcessRecord || CmdletState == AsyncCmdletState.EndProcess;
            }
        }

        protected bool IsBeforeProcessing {
            get {
                return CmdletState < AsyncCmdletState.BeginProcess;
            }
        }

        protected bool IsAfterProcessing {
            get {
                return CmdletState > AsyncCmdletState.EndProcess;
            }
        }

        private void InvokeMessage(TaskCompletionSource<bool> message) {
            var func = message.Task.AsyncState as Func<bool>;
            if (func != null) {
                try {
                    message.SetResult(func());
                } catch (PipelineStoppedException pipelineStoppedException) {
                    _pipelineStopped = pipelineStoppedException;
                    Cancel();
                    message.SetException(pipelineStoppedException);
                } catch (Exception e) {
                    message.SetException(e);
                }
            } else {
                // this should have been a Func<bool>.
                // cancel it.
                message.SetCanceled();
            }
        }

        private void QueueHeldMessage(TaskCompletionSource<bool> message) {
            if (message != null) {
                _heldMessages = _heldMessages ?? new BlockingCollection<TaskCompletionSource<bool>>();
                _heldMessages.Add(message);
            }
        }

        protected void QueueHeldMessage(Func<bool> action) {
            if (IsProcessing) {
                // run it now...
                action();
            } else {
                QueueHeldMessage(new TaskCompletionSource<bool>(action));
            }
        }

        private Task<bool> QueueMessage(TaskCompletionSource<bool> message) {
            // if we're not actually into the processing step yet, we're gonna store this message
            // until later. It is possible that it never gets played...

            if (IsBeforeProcessing || IsAfterProcessing) {
                QueueHeldMessage(message);
                return message.Task;
            }

            if (_messages == null || _messages.IsCompleted) {
                // message queue isn't active. Just run the message now.
                InvokeMessage(message);
            } else {
                if (!_messages.IsCompleted) {
                    _messages.Add(message);
                }
            }
            return message.Task;
        }

        private Task<bool> QueueMessage(Func<bool> action) {
            return QueueMessage(new TaskCompletionSource<bool>(action));
        }

        private Task<bool> QueueMessage(Action action) {
            return QueueMessage(() => {
                action();
                return true;
            });
        }

        public Task<bool> ExecuteOnMainThread(Func<bool> onMainThreadDelegate) {
            var message = new TaskCompletionSource<bool>(onMainThreadDelegate);

            if (_messages == null || _messages.IsCompleted) {
                // message queue isn't active. Just run the message now.
                InvokeMessage(message);
            } else {
                if (!_messages.IsCompleted) {
                    _messages.Add(message);
                }
            }
            return message.Task;
        }

        #endregion

        #region PowerShell response streams

        public new Task<bool> WriteObject(object obj) {
            return QueueMessage(() => {
                if (!IsCanceled) {
                    try {
                        base.WriteObject(obj);
                    } catch (PipelineStoppedException pipelineStoppedException) {
                        // this can throw if the pipeline is stopped
                        // but that's ok, because it just means
                        // that we're done.
                        Cancel();
                        _pipelineStopped = pipelineStoppedException;
                    } catch {
                        // any other means that we're done anyway too.
                    }
                }
            });
        }

        public new Task<bool> WriteObject(object sendToPipeline, bool enumerateCollection) {
            return QueueMessage(() => {
                if (!IsCanceled) {
                    try {
                        base.WriteObject(sendToPipeline, enumerateCollection);
                    } catch (PipelineStoppedException pipelineStoppedException) {
                        // this can throw if the pipeline is stopped
                        // but that's ok, because it just means
                        // that we're done.
                        Cancel();
                        _pipelineStopped = pipelineStoppedException;
                    } catch {
                        // any other means that we're done anyway too.
                    }
                }
            });
        }

        public new Task<bool> WriteWarning(string text) {
            if (!IsInvocation) {
                return false.AsResultTask();
            }
            // ensure the same warning doesn't get played repeatedly.
            if (_warnings.Contains(text)) {
                return true.AsResultTask();
            }
            _warnings.Add(text);
            return QueueMessage(() => base.WriteWarning(text));
        }

        public new Task<bool> WriteDebug(string text) {
            return QueueMessage(() => {
                try {
                    base.WriteDebug(text);
                } catch (PipelineStoppedException pipelineStoppedException) {
                    // this can throw if the pipeline is stopped
                    // but that's ok, because it just means
                    // that we're done.
                    Cancel();
                    _pipelineStopped = pipelineStoppedException;
                } catch {
                    // any other means that we're done anyway too.
                }
            });
        }

        public new Task<bool> WriteError(ErrorRecord errorRecord) {
            if (!IsInvocation) {
                return false.AsResultTask();
            }
            return QueueMessage(() => {
                try {
                   // The cancel could be caused by Error.But this error can get lost because of the If(!IsCanceled) check.
                   // So call WriteError directly.
                    base.WriteError(errorRecord);
                    // if we're stopping, skip this call anyway.
                    //if (!IsCanceled) {
                    //    base.WriteError(errorRecord);
                    //}
                } catch (PipelineStoppedException pipelineStoppedException) {
                    // this can throw if the pipeline is stopped
                    // but that's ok, because it just means
                    // that we're done.
                    Cancel();
                    _pipelineStopped = pipelineStoppedException;
                } catch {
                    // any other means that we're done anyway too.
                }
            });
        }

        public new Task<bool> WriteVerbose(string text) {
            if (!IsInvocation) {
                return false.AsResultTask();
            }
            return QueueMessage(() => {
                try {
                    // if we're stopping, skip this call anyway.
                    base.WriteVerbose(text);
                } catch (PipelineStoppedException pipelineStoppedException) {
                    // this can throw if the pipeline is stopped
                    // but that's ok, because it just means
                    // that we're done.
                    Cancel();
                    _pipelineStopped = pipelineStoppedException;
                } catch {
                    // any other means that we're done anyway too.
                }
            });
        }

        #endregion

        #region CmdLet Interactivity

        internal class ShouldContinueResult
        {
            internal bool result = false;
            internal bool yesToAll = false;
            internal bool noToAll = false;
        }

        public new Task<bool> ShouldContinue(string query, string caption) {
            if (IsCanceled || !IsInvocation) {
                return false.AsResultTask();
            }
            return ExecuteOnMainThread(() => base.ShouldContinue(query, caption));
            // it is apparently OK to have this called during dynamic parameter generation
        }

        internal Task<ShouldContinueResult> ShouldContinue(string query, string caption, bool hasSecurityimpact)
        {
            var shouldContinueResult = new ShouldContinueResult
            {
                yesToAll = false,
                noToAll = false,
                result = false
            };

            if (IsCanceled || !IsInvocation )
            {
                return shouldContinueResult.AsResultTask();
            }

            return ExecuteOnMainThread(() =>
            {                   
                MethodInfo method = null;
                try {
                    method = base.GetType().GetMethod("ShouldContinue", new Type[] { typeof(string), typeof(string), typeof(bool), typeof(bool).MakeByRefType(), typeof(bool).MakeByRefType()});
                } catch {                    
                }                
                if(method != null) {
                    Object[] arguments = { query, caption, hasSecurityimpact, false, false };
                    try {
                        shouldContinueResult.result = (bool)method.Invoke(this, arguments);
                        shouldContinueResult.yesToAll = (bool)arguments[3];
                        shouldContinueResult.noToAll = (bool)arguments[4];
                    } catch  {                 
                    }
                } else {                
                    shouldContinueResult.result = base.ShouldContinue(query, caption, ref shouldContinueResult.yesToAll, ref shouldContinueResult.noToAll);
                }
                return shouldContinueResult.result;
            }).ContinueWith((a) => shouldContinueResult);
        }

        public new Task<bool> ShouldProcess(string target) {
            if (IsCanceled || !IsInvocation) {
                return false.AsResultTask();
            }

            return QueueMessage(() => base.ShouldProcess(target));
        }

        public new Task<bool> ShouldProcess(string target, string action) {
            if (IsCanceled || !IsInvocation) {
                return false.AsResultTask();
            }

            return QueueMessage(() => base.ShouldProcess(target, action));
        }

        public new Task<bool> ShouldProcess(string verboseDescription, string verboseWarning, string caption) {
            if (IsCanceled || !IsInvocation) {
                return false.AsResultTask();
            }

            return QueueMessage(() => base.ShouldProcess(verboseDescription, verboseWarning, caption));
        }

        public new Task<bool> ShouldProcess(string verboseDescription, string verboseWarning, string caption, out ShouldProcessReason shouldProcessReason) {
            if (IsCanceled || !IsInvocation) {
                shouldProcessReason = ShouldProcessReason.None;
                return false.AsResultTask();
            }

            // todo: Uh, this is gonna be tricky!?
            shouldProcessReason = ShouldProcessReason.None;
            return QueueMessage(() => base.ShouldProcess(verboseDescription, verboseWarning, caption));
        }

        #endregion

        #region Direct Property Access

        protected object TryGetProperty(object instance, string fieldName) {
            // any access of a null object returns null.
            if (instance == null || string.IsNullOrWhiteSpace(fieldName)) {
                return null;
            }
 
            var propertyInfo = instance.GetType().GetProperty(fieldName, BindingFlags);

            if (propertyInfo != null) {
                try {
                    return propertyInfo.GetValue(instance, null);
                } catch {
                }
            }

            // maybe it's a field
            var fieldInfo = instance.GetType().GetField(fieldName, BindingFlags);

            if (fieldInfo != null) {
                try {
                    return fieldInfo.GetValue(instance);
                } catch {
                }
            }

            // no match, return null.
            return null;
        }

        protected bool TrySetProperty(object instance, string fieldName, object value) {
            // any access of a null object returns null.
            if (instance == null || string.IsNullOrWhiteSpace(fieldName)) {
                return false;
            }

            var propertyInfo = instance.GetType().GetProperty(fieldName, BindingFlags);

            if (propertyInfo != null) {
                try {
                    propertyInfo.SetValue(instance, value, null);
                    return true;
                } catch {
                }
            }

            // maybe it's a field
            var fieldInfo = instance.GetType().GetField(fieldName, BindingFlags);

            if (fieldInfo != null) {
                try {
                    fieldInfo.SetValue(instance, value);
                    return true;
                } catch {
                }
            }

            return false;
        }

        #endregion
    }
}