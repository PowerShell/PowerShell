/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

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
        private PSDataCollection<TInput> inputStream;
                
        /// <summary>
        /// Output stream for returned objects.
        /// </summary>
        private PSDataCollection<TOutput> outputStream;

        /// <summary>
        /// Error stream for error messages.
        /// </summary>
        private PSDataCollection<ErrorRecord> errorStream;

        /// <summary>
        /// Warning stream for warning messages.
        /// </summary>
        private PSDataCollection<WarningRecord> warningStream;

        /// <summary>
        /// Progress stream for progress messages.
        /// </summary>
        private PSDataCollection<ProgressRecord> progressStream;

        /// <summary>
        /// Verbose stream for verbose messages.
        /// </summary>
        private PSDataCollection<VerboseRecord> verboseStream;

        /// <summary>
        /// Debug stream for debug messages.
        /// </summary>
        private PSDataCollection<DebugRecord> debugStream;

        /// <summary>
        /// Information stream for Information messages.
        /// </summary>
        private PSDataCollection<InformationRecord> informationStream;

        /// <summary>
        /// If the object is already disposed or not.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Private object for thread-safe exection.
        /// </summary>
        private readonly object syncLock = new object();

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PowerShellStreams()
        {
            this.inputStream = null;
            this.outputStream = null;
            this.errorStream = null;
            this.warningStream = null;
            this.progressStream = null;
            this.verboseStream = null;
            this.debugStream = null;
            this.informationStream = null;

            this.disposed = false;
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PowerShellStreams(PSDataCollection<TInput> pipelineInput)
        {
            // Populate the input collection if there is any...
            if (pipelineInput == null)
            {
                inputStream = new PSDataCollection<TInput>();
            }
            else
            {
                inputStream = pipelineInput;
            }

            inputStream.Complete();

            this.outputStream = new PSDataCollection<TOutput>();
            this.errorStream = new PSDataCollection<ErrorRecord>();
            this.warningStream = new PSDataCollection<WarningRecord>();
            this.progressStream = new PSDataCollection<ProgressRecord>();
            this.verboseStream = new PSDataCollection<VerboseRecord>();
            this.debugStream = new PSDataCollection<DebugRecord>();
            this.informationStream = new PSDataCollection<InformationRecord>();

            this.disposed = false;
        }

        /// <summary>
        /// Disope implementation.
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
            if (this.disposed)
                return;

            lock (syncLock)
            {
                if (!this.disposed)
                {
                    if (disposing)
                    {
                        this.inputStream.Dispose();
                        this.outputStream.Dispose();
                        this.errorStream.Dispose();
                        this.warningStream.Dispose();
                        this.progressStream.Dispose();
                        this.verboseStream.Dispose();
                        this.debugStream.Dispose();
                        this.informationStream.Dispose();

                        this.inputStream = null;
                        this.outputStream = null;
                        this.errorStream = null;
                        this.warningStream = null;
                        this.progressStream = null;
                        this.verboseStream = null;
                        this.debugStream = null;
                        this.informationStream = null;
                    }

                    this.disposed = true;
                }
            }
        }

        /// <summary>
        /// Gets input stream.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public PSDataCollection<TInput> InputStream
        {
            get { return this.inputStream; }
            set { this.inputStream = value; }
        }

        /// <summary>
        /// Gets output stream.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public PSDataCollection<TOutput> OutputStream
        {
            get { return this.outputStream; }
            set { this.outputStream = value; }
        }

        /// <summary>
        /// Gets error stream.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public PSDataCollection<ErrorRecord> ErrorStream
        {
            get { return this.errorStream; }
            set { this.errorStream = value; }
        }

        /// <summary>
        /// Gets warning stream.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public PSDataCollection<WarningRecord> WarningStream
        {
            get { return this.warningStream; }
            set { this.warningStream = value; }
        }

        /// <summary>
        /// Gets progress stream.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public PSDataCollection<ProgressRecord> ProgressStream
        {
            get { return this.progressStream; }
            set { this.progressStream = value; }
        }

        /// <summary>
        /// Gets verbose stream.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public PSDataCollection<VerboseRecord> VerboseStream
        {
            get { return this.verboseStream; }
            set { this.verboseStream = value; }
        }

        /// <summary>
        /// Get debug stream.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public PSDataCollection<DebugRecord> DebugStream
        {
            get { return this.debugStream; }
            set { this.debugStream = value; }
        }

        /// <summary>
        /// Gets Information stream.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public PSDataCollection<InformationRecord> InformationStream
        {
            get { return this.informationStream; }
            set { this.informationStream = value; }
        }

        /// <summary>
        /// Marking all the streams as completed so that no further data can be added and 
        /// jobs will know that there is no more data coming in.
        /// </summary>
        public void CloseAll()
        {
            if (this.disposed == false)
            {
                lock (syncLock)
                {
                    if (this.disposed == false)
                    {
                        this.outputStream.Complete();
                        this.errorStream.Complete();
                        this.warningStream.Complete();
                        this.progressStream.Complete();
                        this.verboseStream.Complete();
                        this.debugStream.Complete();
                        this.informationStream.Complete();
                    }
                }
            }
        }
    }
}
