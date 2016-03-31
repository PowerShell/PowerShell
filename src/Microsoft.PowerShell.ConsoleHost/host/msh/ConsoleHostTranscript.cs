/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/



using System;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Management.Automation;
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

                return isTranscribing;
            }
            set
            {
                isTranscribing = value;
            }
        }
        private bool isTranscribing;


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
        private string transcriptFileName = String.Empty;



        internal
        string
        StopTranscribing()
        {
            lock (transcriptionStateLock)
            {

                if (transcriptionWriter == null)
                {
                    return null;
                }

                // The filestream *must* be closed at the end of this method.
                // If it isn't and there is a pending IO error, the finalizer will
                // dispose the stream resulting in an IO exception on the finalizer thread
                // which will crash the process...
                try
                {
                    transcriptionWriter.WriteLine(
                        StringUtil.Format(ConsoleHostStrings.TranscriptEpilogue, DateTime.Now));
                }
                finally
                {
                    try
                    {
                        transcriptionWriter.Dispose();
                    }
                    finally
                    {
                        transcriptionWriter = null;
                        isTranscribing = false;
                    }
                }

                return transcriptFileName;
            }
        }



        internal
        void
        WriteToTranscript(string text)
        {
            lock (transcriptionStateLock)
            {
                if (isTranscribing && transcriptionWriter != null)
                {
                    transcriptionWriter.Write(text);
                }
            }
        }



        private StreamWriter transcriptionWriter;
        private object transcriptionStateLock = new object();
    }
}   // namespace 


