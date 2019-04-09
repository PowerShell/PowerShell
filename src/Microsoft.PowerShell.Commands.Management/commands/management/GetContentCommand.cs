// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Provider;
using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A command to get the content of an item at a specified path.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Content", DefaultParameterSetName = "Path", SupportsTransactions = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113310")]
    public class GetContentCommand : ContentCommandBase
    {
        #region Parameters

        /// <summary>
        /// The number of content items to retrieve per block.
        /// By default this value is 1 which means read one block
        /// at a time.  To read all blocks at once, set this value
        /// to a negative number.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public long ReadCount { get; set; } = 1;

        /// <summary>
        /// The number of content items to retrieve. By default this
        /// value is -1 which means read all the content.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Alias("First", "Head")]
        public long TotalCount
        {
            get
            {
                return _totalCount;
            }

            set
            {
                _totalCount = value;
                _totalCountSpecified = true;
            }
        }

        private bool _totalCountSpecified = false;

        /// <summary>
        /// The number of content items to retrieve from the back of the file.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Alias("Last")]
        public int Tail
        {
            set
            {
                _backCount = value;
                _tailSpecified = true;
            }

            get { return _backCount; }
        }

        private int _backCount = -1;
        private bool _tailSpecified = false;

        /// <summary>
        /// A virtual method for retrieving the dynamic parameters for a cmdlet. Derived cmdlets
        /// that require dynamic parameters should override this method and return the
        /// dynamic parameter object.
        /// </summary>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// An object representing the dynamic parameters for the cmdlet or null if there
        /// are none.
        /// </returns>
        internal override object GetDynamicParameters(CmdletProviderContext context)
        {
            if (Path != null && Path.Length > 0)
            {
                return InvokeProvider.Content.GetContentReaderDynamicParameters(Path[0], context);
            }

            return InvokeProvider.Content.GetContentReaderDynamicParameters(".", context);
        }

        #endregion Parameters

        #region parameter data

        /// <summary>
        /// The number of content items to retrieve.
        /// </summary>
        private long _totalCount = -1;

        #endregion parameter data

        #region Command code

        /// <summary>
        /// Gets the content of an item at the specified path.
        /// </summary>
        protected override void ProcessRecord()
        {
            // TotalCount and Tail should not be specified at the same time.
            // Throw out terminating error if this is the case.
            if (_totalCountSpecified && _tailSpecified)
            {
                string errMsg = StringUtil.Format(SessionStateStrings.GetContent_TailAndHeadCannotCoexist, "TotalCount", "Tail");
                ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "TailAndHeadCannotCoexist", ErrorCategory.InvalidOperation, null);
                WriteError(error);
                return;
            }

            if (TotalCount == 0)
            {
                // Don't read anything
                return;
            }

            // Get the content readers
            CmdletProviderContext currentContext = CmdletProviderContext;
            contentStreams = this.GetContentReaders(Path, currentContext);

            try
            {
                // Iterate through the content holders reading the content
                foreach (ContentHolder holder in contentStreams)
                {
                    long countRead = 0;

                    Dbg.Diagnostics.Assert(
                        holder.Reader != null,
                        "All holders should have a reader assigned");

                    if (_tailSpecified && !(holder.Reader is FileSystemContentReaderWriter))
                    {
                        string errMsg = SessionStateStrings.GetContent_TailNotSupported;
                        ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), "TailNotSupported", ErrorCategory.InvalidOperation, Tail);
                        WriteError(error);
                        continue;
                    }

                    // If Tail is negative, we are supposed to read all content out. This is same
                    // as reading forwards. So we read forwards in this case.
                    // If Tail is positive, we seek the right position. Or, if the seek failed
                    // because of an unsupported encoding, we scan forward to get the tail content.
                    if (Tail >= 0)
                    {
                        bool seekSuccess = false;

                        try
                        {
                            seekSuccess = SeekPositionForTail(holder.Reader);
                        }
                        catch (Exception e)
                        {
                            ProviderInvocationException providerException =
                                new ProviderInvocationException(
                                    "ProviderContentReadError",
                                    SessionStateStrings.ProviderContentReadError,
                                    holder.PathInfo.Provider,
                                    holder.PathInfo.Path,
                                    e);

                            // Log a provider health event
                            MshLog.LogProviderHealthEvent(
                                this.Context,
                                holder.PathInfo.Provider.Name,
                                providerException,
                                Severity.Warning);

                            WriteError(new ErrorRecord(
                                providerException.ErrorRecord,
                                providerException));

                            continue;
                        }

                        // If the seek was successful, we start to read forwards from that
                        // point. Otherwise, we need to scan forwards to get the tail content.
                        if (!seekSuccess && !ScanForwardsForTail(holder, currentContext))
                        {
                            continue;
                        }
                    }

                    if (TotalCount != 0)
                    {
                        IList results = null;

                        do
                        {
                            long countToRead = ReadCount;

                            // Make sure we only ask for the amount the user wanted
                            // I am using TotalCount - countToRead so that I don't
                            // have to worry about overflow

                            if ((TotalCount > 0) && (TotalCount - countToRead < countRead))
                            {
                                countToRead = TotalCount - countRead;
                            }

                            try
                            {
                                results = holder.Reader.Read(countToRead);
                            }
                            catch (Exception e) // Catch-all OK. 3rd party callout
                            {
                                ProviderInvocationException providerException =
                                    new ProviderInvocationException(
                                        "ProviderContentReadError",
                                        SessionStateStrings.ProviderContentReadError,
                                        holder.PathInfo.Provider,
                                        holder.PathInfo.Path,
                                        e);

                                // Log a provider health event
                                MshLog.LogProviderHealthEvent(
                                    this.Context,
                                    holder.PathInfo.Provider.Name,
                                    providerException,
                                    Severity.Warning);

                                WriteError(new ErrorRecord(
                                    providerException.ErrorRecord,
                                    providerException));

                                break;
                            }

                            if (results != null && results.Count > 0)
                            {
                                countRead += results.Count;
                                if (ReadCount == 1)
                                {
                                    // Write out the content as a single object
                                    WriteContentObject(results[0], countRead, holder.PathInfo, currentContext);
                                }
                                else
                                {
                                    // Write out the content as an array of objects
                                    WriteContentObject(results, countRead, holder.PathInfo, currentContext);
                                }
                            }
                        } while (results != null && results.Count > 0 && ((TotalCount < 0) || countRead < TotalCount));
                    }
                }
            }
            finally
            {
                // close all the content readers

                CloseContent(contentStreams, false);

                // Empty the content holder array
                contentStreams = new List<ContentHolder>();
            }
        }

        /// <summary>
        /// Scan forwards to get the tail content.
        /// </summary>
        /// <param name="holder"></param>
        /// <param name="currentContext"></param>
        /// <returns>
        /// true if no error occured
        /// false if there was an error
        /// </returns>
        private bool ScanForwardsForTail(ContentHolder holder, CmdletProviderContext currentContext)
        {
            var fsReader = holder.Reader as FileSystemContentReaderWriter;
            Dbg.Diagnostics.Assert(fsReader != null, "Tail is only supported for FileSystemContentReaderWriter");
            var tailResultQueue = new Queue<object>();
            IList results = null;
            ErrorRecord error = null;

            do
            {
                try
                {
                    results = fsReader.ReadWithoutWaitingChanges(ReadCount);
                }
                catch (Exception e)
                {
                    ProviderInvocationException providerException =
                        new ProviderInvocationException(
                            "ProviderContentReadError",
                            SessionStateStrings.ProviderContentReadError,
                            holder.PathInfo.Provider,
                            holder.PathInfo.Path,
                            e);

                    // Log a provider health event
                    MshLog.LogProviderHealthEvent(
                        this.Context,
                        holder.PathInfo.Provider.Name,
                        providerException,
                        Severity.Warning);

                    // Create and save the error record. The error record
                    // will be written outside the while loop.
                    // This is to make sure the accumulated results get written
                    // out before the error record when the 'scanForwardForTail' is true.
                    error = new ErrorRecord(
                        providerException.ErrorRecord,
                        providerException);

                    break;
                }

                if (results != null && results.Count > 0)
                {
                    foreach (object entry in results)
                    {
                        if (tailResultQueue.Count == Tail)
                            tailResultQueue.Dequeue();
                        tailResultQueue.Enqueue(entry);
                    }
                }
            } while (results != null && results.Count > 0);

            if (tailResultQueue.Count > 0)
            {
                // Respect the ReadCount parameter.
                // Output single object when ReadCount == 1; Output array otherwise
                int count = 0;
                if (ReadCount <= 0 || (ReadCount >= tailResultQueue.Count && ReadCount != 1))
                {
                    count = tailResultQueue.Count;
                    ArrayList outputList = new ArrayList();
                    while (tailResultQueue.Count > 0)
                    {
                        outputList.Add(tailResultQueue.Dequeue());
                    }
                    // Write out the content as an array of objects
                    WriteContentObject(outputList.ToArray(), count, holder.PathInfo, currentContext);
                }
                else if (ReadCount == 1)
                {
                    // Write out the content as single object
                    while (tailResultQueue.Count > 0)
                        WriteContentObject(tailResultQueue.Dequeue(), count++, holder.PathInfo, currentContext);
                }
                else // ReadCount < Queue.Count
                {
                    while (tailResultQueue.Count >= ReadCount)
                    {
                        ArrayList outputList = new ArrayList();
                        for (int idx = 0; idx < ReadCount; idx++, count++)
                            outputList.Add(tailResultQueue.Dequeue());
                        // Write out the content as an array of objects
                        WriteContentObject(outputList.ToArray(), count, holder.PathInfo, currentContext);
                    }

                    int remainder = tailResultQueue.Count;
                    if (remainder > 0)
                    {
                        ArrayList outputList = new ArrayList();
                        for (; remainder > 0; remainder--, count++)
                            outputList.Add(tailResultQueue.Dequeue());
                        // Write out the content as an array of objects
                        WriteContentObject(outputList.ToArray(), count, holder.PathInfo, currentContext);
                    }
                }
            }

            if (error != null)
            {
                WriteError(error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Seek position to the right place.
        /// </summary>
        /// <param name="reader">
        /// reader should be able to be casted to FileSystemContentReader
        /// </param>
        /// <returns>
        /// true if the stream pointer is moved to the right place
        /// false if we cannot seek
        /// </returns>
        private bool SeekPositionForTail(IContentReader reader)
        {
            var fsReader = reader as FileSystemContentReaderWriter;
            Dbg.Diagnostics.Assert(fsReader != null, "Tail is only supported for FileSystemContentReaderWriter");

            try
            {
                fsReader.SeekItemsBackward(Tail);
                return true;
            }
            catch (BackReaderEncodingNotSupportedException)
            {
                // Move to the head
                fsReader.Seek(0, SeekOrigin.Begin);
                return false;
            }
        }

        /// <summary>
        /// Be sure to clean up.
        /// </summary>
        protected override void EndProcessing()
        {
            Dispose(true);
        }
        #endregion Command code

    }
}

