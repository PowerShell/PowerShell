// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell
{
    internal sealed partial
    class ConsoleHost
        :
        PSHost,
        IDisposable
    {
        internal
        bool
        IsTranscribing
        {
            get
            {
                // no locking because the compare should be atomic

                return _isTranscribing;
            }

            set
            {
                _isTranscribing = value;
            }
        }

        private bool _isTranscribing;

        /*
        internal void StartTranscribing(string transcriptFilename, bool shouldAppend)
        {
            // lock so as not to contend with IsTranscribing and StopTranscribing

            lock (transcriptionStateLock)
            {
                Dbg.Assert(transcriptionWriter == null, "writer should not exist");
                this.transcriptFileName = transcriptFilename;

                transcriptionWriter = new StreamWriter(transcriptFilename, shouldAppend, new System.Text.UnicodeEncoding());

                transcriptionWriter.AutoFlush = true;

                string format = ConsoleHostStrings.TranscriptPrologue;
                string line =
                    StringUtil.Format(
                        format,
                        DateTime.Now,
                        Environment.UserDomainName,
                        Environment.UserName,
                        Environment.MachineName,
                        Environment.OSVersion.VersionString);

                transcriptionWriter.WriteLine(line);

                // record that we are transcribing...
                isTranscribing = true;

            }
        }
        */
        private string _transcriptFileName = string.Empty;

        internal
        string
        StopTranscribing()
        {
            lock (_transcriptionStateLock)
            {
                if (_transcriptionWriter == null)
                {
                    return null;
                }

                // The filestream *must* be closed at the end of this method.
                // If it isn't and there is a pending IO error, the finalizer will
                // dispose the stream resulting in an IO exception on the finalizer thread
                // which will crash the process...
                try
                {
                    _transcriptionWriter.WriteLine(
                        StringUtil.Format(ConsoleHostStrings.TranscriptEpilogue, DateTime.Now));
                }
                finally
                {
                    try
                    {
                        _transcriptionWriter.Dispose();
                    }
                    finally
                    {
                        _transcriptionWriter = null;
                        _isTranscribing = false;
                    }
                }

                return _transcriptFileName;
            }
        }

        internal
        void
        WriteToTranscript(string text)
        {
            lock (_transcriptionStateLock)
            {
                if (_isTranscribing && _transcriptionWriter != null)
                {
                    _transcriptionWriter.Write(text);
                }
            }
        }

        private StreamWriter _transcriptionWriter;
        private object _transcriptionStateLock = new object();
    }
}   // namespace

