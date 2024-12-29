// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    /// <summary>
    /// Define all the output streams and one input stream for a workflow.
    /// </summary>
    public sealed class PowerShellStreams<TInput, TOutput> : IDisposable
    {
        /// <summary>
        /// Input stream for incoming objects.
        /// </summary>
        private PSDataCollection<TInput> _inputStream;

        /// <summary>
        /// Output stream for returned objects.
        /// </summary>
        private PSDataCollection<TOutput> _outputStream;

        /// <summary>
        /// Error stream for error messages.
        /// </summary>
        private PSDataCollection<ErrorRecord> _errorStream;

        /// <summary>
        /// Warning stream for warning messages.
        /// </summary>
        private PSDataCollection<WarningRecord> _warningStream;

        /// <summary>
        /// Progress stream for progress messages.
        /// </summary>
        private PSDataCollection<ProgressRecord> _progressStream;

        /// <summary>
        /// Verbose stream for verbose messages.
        /// </summary>
        private PSDataCollection<VerboseRecord> _verboseStream;

        /// <summary>
        /// Debug stream for debug messages.
        /// </summary>
        private PSDataCollection<DebugRecord> _debugStream;

        /// <summary>
        /// Information stream for Information messages.
        /// </summary>
        private PSDataCollection<InformationRecord> _informationStream;

        /// <summary>
        /// If the object is already disposed or not.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Private object for thread-safe execution.
        /// </summary>
        private readonly object _syncLock = new object();

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PowerShellStreams()
        {
            _inputStream = null;
            _outputStream = null;
            _errorStream = null;
            _warningStream = null;
            _progressStream = null;
            _verboseStream = null;
            _debugStream = null;
            _informationStream = null;

            _disposed = false;
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PowerShellStreams(PSDataCollection<TInput> pipelineInput)
        {
            // Populate the input collection if there is any...
            _inputStream = pipelineInput ?? new PSDataCollection<TInput>();

            _inputStream.Complete();

            _outputStream = new PSDataCollection<TOutput>();
            _errorStream = new PSDataCollection<ErrorRecord>();
            _warningStream = new PSDataCollection<WarningRecord>();
            _progressStream = new PSDataCollection<ProgressRecord>();
            _verboseStream = new PSDataCollection<VerboseRecord>();
            _debugStream = new PSDataCollection<DebugRecord>();
            _informationStream = new PSDataCollection<InformationRecord>();

            _disposed = false;
        }

        /// <summary>
        /// Dispose implementation.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected virtual implementation of Dispose.
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            lock (_syncLock)
            {
                if (!_disposed)
                {
                    if (disposing)
                    {
                        _inputStream.Dispose();
                        _outputStream.Dispose();
                        _errorStream.Dispose();
                        _warningStream.Dispose();
                        _progressStream.Dispose();
                        _verboseStream.Dispose();
                        _debugStream.Dispose();
                        _informationStream.Dispose();

                        _inputStream = null;
                        _outputStream = null;
                        _errorStream = null;
                        _warningStream = null;
                        _progressStream = null;
                        _verboseStream = null;
                        _debugStream = null;
                        _informationStream = null;
                    }

                    _disposed = true;
                }
            }
        }

        /// <summary>
        /// Gets input stream.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public PSDataCollection<TInput> InputStream
        {
            get { return _inputStream; }

            set { _inputStream = value; }
        }

        /// <summary>
        /// Gets output stream.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public PSDataCollection<TOutput> OutputStream
        {
            get { return _outputStream; }

            set { _outputStream = value; }
        }

        /// <summary>
        /// Gets error stream.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public PSDataCollection<ErrorRecord> ErrorStream
        {
            get { return _errorStream; }

            set { _errorStream = value; }
        }

        /// <summary>
        /// Gets warning stream.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public PSDataCollection<WarningRecord> WarningStream
        {
            get { return _warningStream; }

            set { _warningStream = value; }
        }

        /// <summary>
        /// Gets progress stream.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public PSDataCollection<ProgressRecord> ProgressStream
        {
            get { return _progressStream; }

            set { _progressStream = value; }
        }

        /// <summary>
        /// Gets verbose stream.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public PSDataCollection<VerboseRecord> VerboseStream
        {
            get { return _verboseStream; }

            set { _verboseStream = value; }
        }

        /// <summary>
        /// Get debug stream.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public PSDataCollection<DebugRecord> DebugStream
        {
            get { return _debugStream; }

            set { _debugStream = value; }
        }

        /// <summary>
        /// Gets Information stream.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public PSDataCollection<InformationRecord> InformationStream
        {
            get { return _informationStream; }

            set { _informationStream = value; }
        }

        /// <summary>
        /// Marking all the streams as completed so that no further data can be added and
        /// jobs will know that there is no more data coming in.
        /// </summary>
        public void CloseAll()
        {
            if (!_disposed)
            {
                lock (_syncLock)
                {
                    if (!_disposed)
                    {
                        _outputStream.Complete();
                        _errorStream.Complete();
                        _warningStream.Complete();
                        _progressStream.Complete();
                        _verboseStream.Complete();
                        _debugStream.Complete();
                        _informationStream.Complete();
                    }
                }
            }
        }
    }
}
