// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Threading;

namespace System.Management.Automation.Runspaces
{
    /// <summary>
    /// PipelineWriter allows the caller to provide an asynchronous stream of objects
    /// as input to a <see cref="System.Management.Automation.Runspaces.Pipeline"/>.
    /// </summary>
    /// <seealso cref="System.Management.Automation.Runspaces.Pipeline.Input"/>
    public abstract class PipelineWriter
    {
        /// <summary>
        /// Signaled when buffer space is available in the underlying stream.
        /// </summary>
        public abstract WaitHandle WaitHandle
        {
            get;
        }

        /// <summary>
        /// Check if the stream is open for further writes.
        /// </summary>
        /// <value>true if the underlying stream is open, otherwise false</value>
        /// <remarks>
        /// Attempting to write to the underlying stream if IsOpen is false throws
        /// a <see cref="PipelineClosedException"/>.
        /// </remarks>
        public abstract bool IsOpen
        {
            get;
        }

        /// <summary>
        /// Returns the number of objects currently in the underlying stream.
        /// </summary>
        public abstract int Count
        {
            get;
        }

        /// <summary>
        /// Get the capacity of the stream.
        /// </summary>
        /// <value>
        /// The capacity of the stream.
        /// </value>
        /// <remarks>
        /// The capacity is the number of objects that stream may contain at one time.  Once this
        /// limit is reached, attempts to write into the stream block until buffer space
        /// becomes available.
        /// </remarks>
        public abstract int MaxCapacity
        {
            get;
        }

        /// <summary>
        /// Close the stream.
        /// </summary>
        /// <remarks>
        /// Causes subsequent calls to IsOpen to return false and calls to
        /// a write operation to throw an ObjectDisposedException.
        /// All calls to Close() after the first call are silently ignored.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// The stream is already disposed
        /// </exception>
        public abstract void Close();

        /// <summary>
        /// Flush the buffered data from the stream.  Closed streams may be flushed,
        /// but disposed streams may not.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// The stream is already disposed
        /// </exception>
        public abstract void Flush();

        /// <summary>
        /// Write a single object into the underlying stream.
        /// </summary>
        /// <param name="obj">The object to add to the stream.</param>
        /// <returns>
        /// One, if the write was successful, otherwise;
        /// zero if the stream was closed before the object could be written,
        /// or if the object was AutomationNull.Value.
        /// </returns>
        /// <exception cref="PipelineClosedException">
        /// The underlying stream is already closed
        /// </exception>
        /// <remarks>
        /// AutomationNull.Value is ignored
        /// </remarks>
        public abstract int Write(object obj);

        /// <summary>
        /// Write multiple objects to the underlying stream.
        /// </summary>
        /// <param name="obj">Object or enumeration to read from.</param>
        /// <param name="enumerateCollection">
        /// If enumerateCollection is true, and <paramref name="obj"/>
        /// is an enumeration according to LanguagePrimitives.GetEnumerable,
        /// the objects in the enumeration will be unrolled and
        /// written separately.  Otherwise, <paramref name="obj"/>
        /// will be written as a single object.
        /// </param>
        /// <returns>The number of objects written.</returns>
        /// <exception cref="PipelineClosedException">
        /// The underlying stream is already closed
        /// </exception>
        /// <remarks>
        /// If the enumeration contains elements equal to
        /// AutomationNull.Value, they are ignored.
        /// This can cause the return value to be less than the size of
        /// the collection.
        /// </remarks>
        public abstract int Write(object obj, bool enumerateCollection);
    }

    internal class DiscardingPipelineWriter : PipelineWriter
    {
        private readonly ManualResetEvent _waitHandle = new ManualResetEvent(true);

        public override WaitHandle WaitHandle
        {
            get { return _waitHandle; }
        }

        private bool _isOpen = true;

        public override bool IsOpen
        {
            get { return _isOpen; }
        }

        private int _count = 0;

        public override int Count
        {
            get { return _count; }
        }

        public override int MaxCapacity
        {
            get { return int.MaxValue; }
        }

        public override void Close()
        {
            _isOpen = false;
        }

        public override void Flush()
        {
        }

        public override int Write(object obj)
        {
            const int numberOfObjectsWritten = 1;
            _count += numberOfObjectsWritten;
            return numberOfObjectsWritten;
        }

        public override int Write(object obj, bool enumerateCollection)
        {
            if (!enumerateCollection)
            {
                return this.Write(obj);
            }

            int numberOfObjectsWritten = 0;
            IEnumerable enumerable = LanguagePrimitives.GetEnumerable(obj);
            if (enumerable != null)
            {
                foreach (object o in enumerable)
                {
                    numberOfObjectsWritten++;
                }
            }
            else
            {
                numberOfObjectsWritten++;
            }

            _count += numberOfObjectsWritten;
            return numberOfObjectsWritten;
        }
    }
}
