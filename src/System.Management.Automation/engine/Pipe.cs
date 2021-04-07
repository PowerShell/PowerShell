// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Management.Automation.Runspaces;

namespace System.Management.Automation.Internal
{
    /// <summary>
    /// Corresponds to -OutputVariable, -ErrorVariable, -WarningVariable, and -InformationVariable.
    /// </summary>
    internal enum VariableStreamKind
    {
        Output,
        Error,
        Warning,
        Information
    }

    /// <summary>
    /// Pipe provides a way to stitch two commands.
    /// </summary>
    /// <remarks>
    /// The Pipe class is not thread-safe, so methods such as
    /// AddItems and Retrieve should not be called simultaneously.
    /// ExternalReader and ExternalWriter can provide thread-safe buffering.
    /// </remarks>
    internal class Pipe
    {
        private readonly ExecutionContext _context;

        // If a pipeline object has been added, then
        // write objects to it, stepping one at a time...
        internal PipelineProcessor PipelineProcessor { get; }

        /// <summary>
        /// This is the downstream cmdlet in the "streamlet model"
        /// which is invoked during each call to Add/AddItems.
        /// </summary>
        internal CommandProcessorBase DownstreamCmdlet
        {
            get
            {
                return _downstreamCmdlet;
            }

            set
            {
                Diagnostics.Assert(_resultList == null, "Tried to set downstream cmdlet when _resultList not null");
                _downstreamCmdlet = value;
            }
        }

        private CommandProcessorBase _downstreamCmdlet;

        /// <summary>
        /// This is the upstream external object source.  If this is set,
        /// Retrieve() will attempt to read objects from the upstream source
        /// before indicating that the pipe is empty.
        /// <remarks>
        /// It is improper to change this once the pipeline has started
        /// executing, although the checks for this are in the
        /// PipelineProcessor class and not here.
        /// </remarks>
        /// </summary>
        internal PipelineReader<object> ExternalReader { get; set; }

        /// <summary>
        /// This is the downstream object recipient.  If this is set,
        /// Add() and AddItems() write to this recipient instead of
        /// to the internal queue.  This also disables the
        /// DownstreamCmdlet.
        /// <remarks>
        /// It is improper to change this once the pipeline has started
        /// executing, although the checks for this are in the
        /// PipelineProcessor class and not here.
        /// </remarks>
        /// </summary>
        internal PipelineWriter ExternalWriter
        {
            get
            {
                return _externalWriter;
            }

            set
            {
                Diagnostics.Assert(_resultList == null, "Tried to set Pipe ExternalWriter when resultList not null");
                _externalWriter = value;
            }
        }

        private PipelineWriter _externalWriter;

        /// <summary>
        /// For diagnostic purposes.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (_downstreamCmdlet != null)
                return _downstreamCmdlet.ToString();
            return base.ToString();
        }

        /// <summary>
        /// OutBufferCount configures the number of objects to buffer before calling the downstream Cmdlet.
        /// </summary>
        internal int OutBufferCount { get; set; } = 0;

        /// <summary>
        /// Gets whether the out variable list should be ignored.
        /// </summary>
        internal bool IgnoreOutVariableList { get; set; }

        /// <summary>
        /// If true, then all input added to this pipe will simply be discarded...
        /// </summary>
        internal bool NullPipe
        {
            get
            {
                return _nullPipe;
            }

            set
            {
                _isRedirected = true;
                _nullPipe = value;
            }
        }

        private bool _nullPipe;

        /// <summary>
        /// A queue that is shared between commands on either side of the pipe to transfer objects.
        /// </summary>
        internal Queue<object> ObjectQueue { get; }

        /// <summary>
        /// True if there are items in this pipe that need processing...
        /// <remarks>
        /// This does not take into account the presence of ExternalInput;
        /// it only indicates whether there is currently any data queued up
        /// or if there is data in the enumerator...
        /// </remarks>
        /// </summary>
        internal bool Empty
        {
            get
            {
                if (_enumeratorToProcess != null)
                    return _enumeratorToProcessIsEmpty;

                if (ObjectQueue != null)
                    return ObjectQueue.Count == 0;
                return true;
            }
        }

        /// <summary>
        /// Is true if there is someone consuming this pipe already, either through
        /// a Pipe object that processes it's output or there is downstream cmdlet...
        /// </summary>
        internal bool IsRedirected
        {
            get { return _downstreamCmdlet != null || _isRedirected; }
        }

        private bool _isRedirected;

        /// <summary>
        /// If non-null, output written to the pipe are also added to this list.
        /// </summary>
        private List<IList> _outVariableList;

        /// <summary>
        /// If non-null, errors written to the pipe are also added to this list.
        /// </summary>
        private List<IList> _errorVariableList;

        /// <summary>
        /// If non-null, warnings written to the pipe are also added to this list.
        /// </summary>
        private List<IList> _warningVariableList;

        /// <summary>
        /// If non-null, information objects written to the pipe are also added to this list.
        /// </summary>
        private List<IList> _informationVariableList;

        /// <summary>
        /// If non-null, the current object being written to the pipe is stored in
        /// this variable.
        /// </summary>
        private PSVariable _pipelineVariableObject;

        private static void AddToVarList(List<IList> varList, object obj)
        {
            if (varList != null && varList.Count > 0)
            {
                for (int i = 0; i < varList.Count; i++)
                {
                    varList[i].Add(obj);
                }
            }
        }

        internal void AppendVariableList(VariableStreamKind kind, object obj)
        {
            switch (kind)
            {
                case VariableStreamKind.Error:
                    AddToVarList(_errorVariableList, obj);
                    break;
                case VariableStreamKind.Warning:
                    AddToVarList(_warningVariableList, obj);
                    break;
                case VariableStreamKind.Output:
                    AddToVarList(_outVariableList, obj);
                    break;
                case VariableStreamKind.Information:
                    AddToVarList(_informationVariableList, obj);
                    break;
            }
        }

        internal void AddVariableList(VariableStreamKind kind, IList list)
        {
            switch (kind)
            {
                case VariableStreamKind.Error:
                    if (_errorVariableList == null)
                    {
                        _errorVariableList = new List<IList>();
                    }

                    _errorVariableList.Add(list);
                    break;
                case VariableStreamKind.Warning:
                    if (_warningVariableList == null)
                    {
                        _warningVariableList = new List<IList>();
                    }

                    _warningVariableList.Add(list);
                    break;
                case VariableStreamKind.Output:
                    if (_outVariableList == null)
                    {
                        _outVariableList = new List<IList>();
                    }

                    _outVariableList.Add(list);
                    break;
                case VariableStreamKind.Information:
                    if (_informationVariableList == null)
                    {
                        _informationVariableList = new List<IList>();
                    }

                    _informationVariableList.Add(list);
                    break;
            }
        }

        internal void SetPipelineVariable(PSVariable pipelineVariable)
        {
            _pipelineVariableObject = pipelineVariable;
        }

        internal void RemoveVariableList(VariableStreamKind kind, IList list)
        {
            switch (kind)
            {
                case VariableStreamKind.Error:
                    _errorVariableList.Remove(list);
                    break;
                case VariableStreamKind.Warning:
                    _warningVariableList.Remove(list);
                    break;
                case VariableStreamKind.Output:
                    _outVariableList.Remove(list);
                    break;
                case VariableStreamKind.Information:
                    _informationVariableList.Remove(list);
                    break;
            }
        }

        internal void RemovePipelineVariable()
        {
            if (_pipelineVariableObject != null)
            {
                _pipelineVariableObject.Value = null;
                _pipelineVariableObject = null;
            }
        }

        /// <summary>
        /// When a temporary pipe is used in the middle of execution, then we need to pass along
        /// the error and warning variable list to hold the errors and warnings get written out
        /// while the temporary pipe is being used.
        ///
        /// We don't need to pass along the out variable list because we don't care about the output
        /// generated in the middle of execution.
        /// </summary>
        internal void SetVariableListForTemporaryPipe(Pipe tempPipe)
        {
            CopyVariableToTempPipe(VariableStreamKind.Error, _errorVariableList, tempPipe);
            CopyVariableToTempPipe(VariableStreamKind.Warning, _warningVariableList, tempPipe);
            CopyVariableToTempPipe(VariableStreamKind.Information, _informationVariableList, tempPipe);
        }

        private static void CopyVariableToTempPipe(VariableStreamKind streamKind, List<IList> variableList, Pipe tempPipe)
        {
            if (variableList != null && variableList.Count > 0)
            {
                for (int i = 0; i < variableList.Count; i++)
                {
                    tempPipe.AddVariableList(streamKind, variableList[i]);
                }
            }
        }

        #region ctor

        /// <summary>
        /// Default constructor - Creates the object queue.
        /// </summary>
        /// <remarks>
        /// The initial Queue capacity is 1, but it will grow automatically.
        /// </remarks>
        internal Pipe()
        {
            ObjectQueue = new Queue<object>();
        }

        /// <summary>
        /// This overload causes output to be written into a List.
        /// </summary>
        /// <param name="resultList"></param>
        internal Pipe(List<object> resultList)
        {
            Diagnostics.Assert(resultList != null, "resultList cannot be null");
            _isRedirected = true;
            _resultList = resultList;
        }

        private readonly List<object> _resultList;

        /// <summary>
        /// This overload causes output to be
        /// written onto an Collection[PSObject] which is more useful
        /// in many circumstances than arraylist.
        /// </summary>
        /// <param name="resultCollection">The collection to write into.</param>
        internal Pipe(System.Collections.ObjectModel.Collection<PSObject> resultCollection)
        {
            Diagnostics.Assert(resultCollection != null, "resultCollection cannot be null");
            _isRedirected = true;
            _resultCollection = resultCollection;
        }

        private readonly System.Collections.ObjectModel.Collection<PSObject> _resultCollection;

        /// <summary>
        /// This pipe writes into another pipeline processor allowing
        /// pipelines to be chained together...
        /// </summary>
        /// <param name="context">The execution context object for this engine instance.</param>
        /// <param name="outputPipeline">The pipeline to write into...</param>
        internal Pipe(ExecutionContext context, PipelineProcessor outputPipeline)
        {
            Diagnostics.Assert(outputPipeline != null, "outputPipeline cannot be null");
            Diagnostics.Assert(outputPipeline != null, "context cannot be null");
            _isRedirected = true;
            _context = context;
            PipelineProcessor = outputPipeline;
        }

        /// <summary>
        /// Read from an enumerator instead of a pipeline reader...
        /// </summary>
        /// <param name="enumeratorToProcess">The enumerator to process...</param>
        internal Pipe(IEnumerator enumeratorToProcess)
        {
            Diagnostics.Assert(enumeratorToProcess != null, "enumeratorToProcess cannot be null");
            _enumeratorToProcess = enumeratorToProcess;

            // since there is an enumerator specified, we
            // assume that there is some stuff to read
            _enumeratorToProcessIsEmpty = false;
        }

        private readonly IEnumerator _enumeratorToProcess;
        private bool _enumeratorToProcessIsEmpty;

        #endregion ctor

        /// <summary>
        /// Writes an object to the pipe.  This could recursively call to the
        /// downstream cmdlet, or write the object to the external output.
        /// </summary>
        /// <param name="obj">The object to add to the pipe.</param>
        /// <remarks>
        /// AutomationNull.Value is ignored
        /// </remarks>
        /// <exception cref="PipelineStoppedException">
        /// a terminating error occurred, or the pipeline was otherwise stopped
        /// </exception>
        /// <exception cref="PipelineClosedException">
        /// The ExternalWriter stream is closed
        /// </exception>
        internal void Add(object obj)
        {
            if (obj == AutomationNull.Value)
                return;

            // OutVariable is appended for null pipes so that the following works:
            //     foo -OutVariable bar > $null
            AddToVarList(_outVariableList, obj);

            if (_nullPipe)
                return;

            // Store the current pipeline variable
            if (_pipelineVariableObject != null)
            {
                _pipelineVariableObject.Value = obj;
            }

            AddToPipe(obj);
        }

        internal void AddWithoutAppendingOutVarList(object obj)
        {
            if (obj == AutomationNull.Value || _nullPipe)
                return;

            AddToPipe(obj);
        }

        private void AddToPipe(object obj)
        {
            if (PipelineProcessor != null)
            {
                // Put the pipeline on the notification stack for stop.
                _context.PushPipelineProcessor(PipelineProcessor);
                PipelineProcessor.Step(obj);
                _context.PopPipelineProcessor(false);
            }
            else if (_resultCollection != null)
            {
                _resultCollection.Add(obj != null ? PSObject.AsPSObject(obj) : null);
            }
            else if (_resultList != null)
            {
                _resultList.Add(obj);
            }
            else if (_externalWriter != null)
            {
                _externalWriter.Write(obj);
            }
            else if (ObjectQueue != null)
            {
                ObjectQueue.Enqueue(obj);

                // This is the "streamlet" recursive call
                if (_downstreamCmdlet != null && ObjectQueue.Count > OutBufferCount)
                {
                    _downstreamCmdlet.DoExecute();
                }
            }
        }

        /// <summary>
        /// Writes a set of objects to the pipe.  This could recursively
        /// call to the downstream cmdlet, or write the objects to the
        /// external output.
        /// </summary>
        /// <param name="objects">
        /// Each of the objects are added to the pipe
        /// </param>
        /// <exception cref="PipelineStoppedException">
        /// The pipeline has already been stopped,
        /// or a terminating error occurred in a downstream cmdlet.
        /// </exception>
        /// <exception cref="PipelineClosedException">
        /// The ExternalWriter stream is closed
        /// </exception>
        internal void AddItems(object objects)
        {
            // Use the extended type system to try and get an enumerator for the object being added.
            // If we get an enumerator, then add the individual elements. If the object isn't
            // enumerable (i.e. the call returned null) then add the object to the pipe
            // as a single element.
            IEnumerator ie = LanguagePrimitives.GetEnumerator(objects);
            try
            {
                if (ie == null)
                {
                    Add(objects);
                }
                else
                {
                    while (ParserOps.MoveNext(_context, null, ie))
                    {
                        object o = ParserOps.Current(null, ie);

                        // Slip over any instance of AutomationNull.Value in the pipeline...
                        if (o == AutomationNull.Value)
                        {
                            continue;
                        }

                        Add(o);
                    }
                }
            }
            finally
            {
                // If our object came from GetEnumerator (and hence is not IEnumerator), then we need to dispose
                // Otherwise, we don't own the object, so don't dispose.
                var disposable = ie as IDisposable;
                if (disposable != null && objects is not IEnumerator)
                {
                    disposable.Dispose();
                }
            }

            if (_externalWriter != null)
                return;

            // If there are objects waiting for the downstream command
            // call it now
            if (_downstreamCmdlet != null && ObjectQueue != null && ObjectQueue.Count > OutBufferCount)
            {
                _downstreamCmdlet.DoExecute();
            }
        }

        /// <summary>
        /// Returns an object from the pipe. If pipe is empty returns null.
        /// This will try the ExternalReader if there are no queued objects.
        /// </summary>
        /// <returns>
        /// object that is retrieved, or AutomationNull.Value if none
        /// </returns>
        internal object Retrieve()
        {
            if (ObjectQueue != null && ObjectQueue.Count != 0)
            {
                return ObjectQueue.Dequeue();
            }
            else if (_enumeratorToProcess != null)
            {
                if (_enumeratorToProcessIsEmpty)
                    return AutomationNull.Value;

                if (!ParserOps.MoveNext(_context, null, _enumeratorToProcess))
                {
                    _enumeratorToProcessIsEmpty = true;
                    return AutomationNull.Value;
                }

                return ParserOps.Current(null, _enumeratorToProcess);
            }
            else if (ExternalReader != null)
            {
                try
                {
                    object o = ExternalReader.Read();
                    if (AutomationNull.Value == o)
                    {
                        // NOTICE-2004/06/08-JonN 963367
                        // The fix to this bug involves making one last
                        // attempt to read from the pipeline in DoComplete.
                        // We should be sure to not hit the ExternalReader
                        // again if it already reported completion.
                        ExternalReader = null;
                    }

                    return o;
                }
                catch (PipelineClosedException)
                {
                    return AutomationNull.Value;
                }
                catch (ObjectDisposedException)
                {
                    return AutomationNull.Value;
                }
            }
            else
                return AutomationNull.Value;
        }

        /// <summary>
        /// Removes all the objects from the Pipe.
        /// </summary>
        internal void Clear()
        {
            if (ObjectQueue != null)
                ObjectQueue.Clear();
        }

        /// <summary>
        /// Returns the currently queued items in the pipe.  Note that this will
        /// not block on ExternalInput, and it does not modify the contents of
        /// the pipe.
        /// </summary>
        /// <returns>Possibly empty array of objects, but not null.</returns>
        internal object[] ToArray()
        {
            if (ObjectQueue == null || ObjectQueue.Count == 0)
                return MshCommandRuntime.StaticEmptyArray;

            return ObjectQueue.ToArray();
        }
    }
}
