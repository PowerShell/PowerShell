// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation.Internal
{
    using System;
    using System.Threading;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Management.Automation.Runspaces;

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings

    /// <summary>
    /// Base class representing a FIFO memory based object stream.
    /// The purpose of this abstraction is to provide the
    /// semantics of a unidirectional stream of objects
    /// between two threads using a dynamic memory buffer.
    /// </summary>
    internal abstract class ObjectStreamBase : IDisposable
    {
        #region Public events
        /// <summary>
        /// Event fired when data is added to the buffer.
        /// </summary>
        internal event EventHandler DataReady = null;

        /// <summary>
        /// Raises DataReadyEvent.
        /// </summary>
        /// <param name="source">
        /// Source of the event
        /// </param>
        /// <param name="args">
        /// Event args
        /// </param>
        internal void FireDataReadyEvent(object source, EventArgs args)
        {
            DataReady.SafeInvoke(source, args);
        }

        #endregion Public events

        #region Virtual Properties

        /// <summary>
        /// Get the capacity of the stream.
        /// </summary>
        /// <value>
        /// The capacity of the stream.
        /// </value>
        /// <remarks>
        /// The capacity is the number of objects the stream may contain at one time.  Once this
        /// limit is reached, attempts to write into the stream block until buffer space
        /// becomes available.
        /// MaxCapacity cannot change, so we can skip the lock.
        /// </remarks>
        internal abstract int MaxCapacity { get; }

        /// <summary>
        /// Waitable handle for callers to wait on until data ready to read.
        /// </summary>
        /// <remarks>
        /// The handle is set when data becomes available to read or
        /// when a partial read has completed.  If multiple readers
        /// are used, setting the handle does not guarantee that
        /// a read operation will return data. If using multiple
        /// reader threads, <see cref="NonBlockingRead"/> for
        /// performing non-blocking reads.
        /// </remarks>
        internal virtual WaitHandle ReadHandle
        {
            get
            {
#pragma warning disable 56503
                // disabled compiler warning as PSDataCollectionStream doesn't override this
                // and I didn't want code duplication.
                throw PSTraceSource.NewNotSupportedException();
#pragma warning restore 56503
            }
        }

        /// <summary>
        /// Waitable handle for callers to block until buffer space becomes available.
        /// </summary>
        /// <remarks>
        /// The handle is set when space becomes available for writing. For multiple
        /// writer threads writing to a bounded stream, the writer may still block
        /// if another thread fills the stream to capacity.
        /// </remarks>
        internal virtual WaitHandle WriteHandle
        {
            get
            {
#pragma warning disable 56503
                // disabled compiler warning as PSDataCollectionStream doesn't override this
                // and I didn't want code duplication.
                throw PSTraceSource.NewNotSupportedException();
#pragma warning restore 56503
            }
        }

        /// <summary>
        /// Determine if we are at the end of the stream.
        /// </summary>
        /// <remarks>
        /// EndOfPipeline is defined as the stream being closed and containing
        /// zero objects.  Readers check this to determine if any objects
        /// are in the stream.  Writers should check <see cref="IsOpen"/> to determine
        /// if the stream can be written to.
        /// </remarks>
        internal abstract bool EndOfPipeline { get; }

        /// <summary>
        /// Check if the stream is open for further writes.
        /// </summary>
        /// <returns>True if the stream is open, false if not.</returns>
        /// <remarks>
        /// IsOpen returns true until the first call to Close(). Writers should
        /// check IsOpen to determine if a write operation can be made.  Note that
        /// writers need to catch <see cref="PipelineClosedException"/>.
        /// <seealso cref="EndOfPipeline"/>
        /// </remarks>
        internal abstract bool IsOpen { get; }

        /// <summary>
        /// Returns the number of objects in the stream.
        /// </summary>
        internal abstract int Count { get; }

        /// <summary>
        /// Return a PipelineReader(object) for this stream.
        /// </summary>
        internal abstract PipelineReader<object> ObjectReader { get; }

        /// <summary>
        /// Return a PipelineReader(PSObject) for this stream.
        /// </summary>
        internal abstract PipelineReader<PSObject> PSObjectReader { get; }

        // 913921-2005/07/08 ObjectWriter can be retrieved on a closed stream
        /// <summary>
        /// Return an PipelineWriter for this stream.
        /// </summary>
        internal abstract PipelineWriter ObjectWriter { get; }

        #endregion

        #region Read Abstractions

        /// <summary>
        /// Read a single object from the stream.
        /// </summary>
        /// <returns>The next object in the stream or AutomationNull if EndOfPipeline is reached.</returns>
        /// <remarks>This method blocks if the stream is empty</remarks>
        internal virtual object Read()
        {
            throw PSTraceSource.NewNotSupportedException();
        }

        /// <summary>
        /// Read at most <paramref name="count"/> objects.
        /// </summary>
        /// <param name="count">The maximum number of objects to read.</param>
        /// <returns>The objects read.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="count"/> is less than 0
        /// </exception>
        /// <remarks>
        /// This method blocks if the number of objects in the stream is less than <paramref name="count"/>
        /// and the stream is not closed.
        ///
        /// If there are multiple reader threads, the objects returned
        /// to blocking reads Read(int count) and ReadToEnd()
        /// are not necessarily single blocks of objects added to the
        /// stream in that order.  For example, if ABCDEF are added to the
        /// stream, one reader may get ABDE and the other may get CF.
        /// Each reader reads items from the stream as they become available.
        /// Otherwise, if a maximum _capacity has been imposed, the writer
        /// and reader could become mutually deadlocked.
        ///
        /// When there are multiple blocked readers, any of the readers
        /// may get the next object(s) added.
        /// </remarks>
        internal virtual Collection<object> Read(int count)
        {
            throw PSTraceSource.NewNotSupportedException();
        }

        /// <summary>
        /// Blocks until the pipeline closes and reads all objects.
        /// </summary>
        /// <returns>A collection of zero or more objects.</returns>
        /// <remarks>
        /// If the stream is empty, a collection of size zero is returned.
        ///
        /// If there are multiple reader threads, the objects returned
        /// to blocking reads Read(int count) and ReadToEnd()
        /// are not necessarily single blocks of objects added to the
        /// stream in that order.  For example, if ABCDEF are added to the
        /// stream, one reader may get ABDE and the other may get CF.
        /// Each reader reads items from the stream as they become available.
        /// Otherwise, if a maximum _capacity has been imposed, the writer
        /// and reader could become mutually deadlocked.
        ///
        /// When there are multiple blocked readers, any of the readers
        /// may get the next object(s) added.
        /// </remarks>
        internal virtual Collection<object> ReadToEnd()
        {
            throw PSTraceSource.NewNotSupportedException();
        }

        /// <summary>
        /// Reads objects currently in the stream, but does not block.
        /// </summary>
        /// <returns>An array of zero or more objects.</returns>
        /// <remarks>
        /// This method performs a read of objects currently in the
        /// stream. The method will block until exclusive access to the
        /// stream is acquired.  If there are no objects in the stream,
        /// an empty array is returned.
        /// </remarks>
        /// <param name="maxRequested">
        /// Return no more than maxRequested objects.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="maxRequested"/> is less than 0
        /// </exception>
        internal virtual Collection<object> NonBlockingRead(int maxRequested)
        {
            throw PSTraceSource.NewNotSupportedException();
        }

        /// <summary>
        /// Peek the next object.
        /// </summary>
        /// <returns>
        /// The next object in the stream or AutomationNull.Value if the stream is empty
        /// </returns>
        /// <exception cref="PipelineClosedException">The ObjectStream is closed.</exception>
        internal virtual object Peek()
        {
            throw PSTraceSource.NewNotSupportedException();
        }

        #endregion

        #region Write Abstractions

        /// <summary>
        /// Writes a object to the current position in the stream and
        /// advances the position within the stream by one object.
        /// </summary>
        /// <param name="value">The object to write to the stream.</param>
        /// <returns>
        /// One, if the write was successful, otherwise;
        /// zero if the stream was closed before the object could be written,
        /// or if the object was AutomationNull.Value.
        /// </returns>
        /// <exception cref="PipelineClosedException">
        /// The stream is closed
        /// </exception>
        /// <remarks>
        /// AutomationNull.Value is ignored
        /// </remarks>
        internal virtual int Write(object value)
        {
            return Write(value, false);
        }

        /// <summary>
        /// Write objects to the underlying stream.
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
        /// The underlying stream is closed
        /// </exception>
        /// <remarks>
        /// If the enumeration contains elements equal to
        /// AutomationNull.Value, they are ignored.
        /// This can cause the return value to be less than the size of
        /// the collection.
        /// </remarks>
        internal virtual int Write(object obj, bool enumerateCollection)
        {
            throw PSTraceSource.NewNotSupportedException();
        }

        #endregion

        #region Close / Flush

        /// <summary>
        /// Close the stream.
        /// </summary>
        /// <remarks>
        /// Causes subsequent calls to IsOpen to return false and calls to
        /// a write operation to throw PipelineClosedException.
        /// All calls to Close() after the first call are silently ignored.
        /// </remarks>
        internal virtual void Close()
        {
            throw PSTraceSource.NewNotSupportedException();
        }

        /// <summary>
        /// Flush the data from the stream.  Closed streams may be flushed.
        /// </summary>
        internal virtual void Flush()
        {
            throw PSTraceSource.NewNotSupportedException();
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Public method for dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Release all resources.
        /// </summary>
        /// <param name="disposing">If true, release all managed resources.</param>
        protected abstract void Dispose(bool disposing);

        #endregion IDisposable
    }

    /// <summary>
    /// A FIFO memory based object stream.
    /// The purpose of this stream class is to provide the
    /// semantics of a unidirectional stream of objects
    /// between two threads using a dynamic memory buffer.
    /// </summary>
    /// <remarks>
    /// The stream may be bound or unbounded.  Bounded streams
    /// are created via passing a capacity to the constructor.
    /// Unbounded streams are created using the default constructor.
    ///
    /// The capacity of the stream can not be changed after
    /// construction.
    ///
    /// For bounded streams, attempts to write to the stream when
    /// the capacity has been reached causes the writer to block
    /// until objects are read.
    ///
    /// For unbounded streams, writers only block for the amount
    /// of time needed to acquire exclusive access to the
    /// stream.  Note that unbounded streams have a capacity of
    /// of Int32.MaxValue objects.  In theory, if this limit were
    /// reached, the stream would function as a bounded stream.
    ///
    /// This class is safe for multi-threaded use with the following
    /// side-effects:
    ///
    /// > For bounded streams, write operations are not guaranteed to
    /// be atomic.  If a write operation causes the capacity to be
    /// reached without writing all data, a partial write occurs and
    /// the writer blocks until data is read from the stream.
    ///
    /// > When multiple writer or reader threads are used, the order
    /// the reader or writer acquires a lock on the stream is
    /// undefined.  This means that the first call to write does not
    /// guarantee the writer will acquire a write lock first.  The first
    /// call to read does not guarantee the reader will acquire the
    /// read lock first.
    ///
    /// > Reads and writes may occur in any order. With a bounded
    /// stream, write operations between threads may also result in
    /// interleaved write operations.
    ///
    /// The result is that the order of data is only guaranteed if there is a
    /// single writer.
    /// </remarks>
    // 897230-2003/10/29-JonN marked sealed
    // 905990-2005/05/10-JonN Removed IDisposable
    internal sealed class ObjectStream : ObjectStreamBase, IDisposable
    {
        #region Private Fields
        /// <summary>
        /// Objects in the stream.
        /// </summary>
        // PERF-2003/08/22-JonN We should probably use Queue instead
        // PERF-2004/06/30-JonN Probably more efficient to use type
        //  Collection<object> as the underlying store
        private readonly List<object> _objects;

        /// <summary>
        /// Is the stream open or closed for writing?
        /// </summary>
        private bool _isOpen;

        #region Synchronization handles
        /// <summary>
        /// Read handle - signaled when data is ready to read.
        /// </summary>
        /// <remarks>
        /// This event may, on occasion, be signalled even when there is
        /// no data available.  If this happens, just wait again.
        /// Never wait on this event alone.  Since this is an AutoResetEvent,
        /// there is no way to definitely release all blocked threads when
        /// the stream is closed for reading.  Instead, use WaitAny on
        /// this handle and also _readClosedHandle.
        /// </remarks>
        private readonly AutoResetEvent _readHandle;

        /// <summary>
        /// Handle returned to callers for blocking on data ready.
        /// </summary>
        private ManualResetEvent _readWaitHandle;

        /// <summary>
        /// When this handle is set, the stream is closed for reading,
        /// so all blocked readers should be released.
        /// </summary>
        private readonly ManualResetEvent _readClosedHandle;

        /// <summary>
        /// Write handle - signaled with the number of objects in the
        /// stream becomes less than the maximum number of objects
        /// allowed in the stream.  <see cref="_capacity"/>
        /// </summary>
        /// <remarks>
        /// This event may, on occasion, be signalled even when there is
        /// no write buffer available.  If this happens, just wait again.
        /// Never wait on this event alone.  Since this is an AutoResetEvent,
        /// there is no way to definitely release all blocked threads when
        /// the stream is closed for writing.  Instead, use WaitAny on
        /// this handle and also _writeClosedHandle.
        /// </remarks>
        private readonly AutoResetEvent _writeHandle;

        /// <summary>
        /// Handle returned to callers for blocking until buffer space
        /// is available for write.
        /// </summary>
        private ManualResetEvent _writeWaitHandle;

        /// <summary>
        /// When this handle is set, the stream is closed for writing,
        /// so all blocked readers should be released.
        /// </summary>
        private readonly ManualResetEvent _writeClosedHandle;
        #endregion Synchronization handles

        /// <summary>
        /// The object reader for this stream.
        /// </summary>
        /// <remarks>
        /// This field is allocated on first demand and
        /// returned on subsequent calls.
        /// </remarks>
        private PipelineReader<object> _reader = null;

        /// <summary>
        /// The PSObject reader for this stream.
        /// </summary>
        /// <remarks>
        /// This field is allocated on first demand and
        /// returned on subsequent calls.
        /// </remarks>
        private PipelineReader<PSObject> _mshreader = null;

        /// <summary>
        /// The object writer for this stream.
        /// </summary>
        /// <remarks>
        /// This field is allocated on first demand and
        /// returned on subsequent calls.
        /// </remarks>
        private PipelineWriter _writer = null;

        /// <summary>
        /// Maximum number of objects allowed in the stream
        /// Note that this is not permitted to be more than Int32.MaxValue,
        /// since the underlying list has this limitation.
        /// </summary>
        private readonly int _capacity = int.MaxValue;

        /// <summary>
        /// This object is used to acquire an exclusive lock on the stream.
        /// </summary>
        /// <remarks>
        /// Note that we lock _monitorObject rather than "this" so that
        /// we are protected from outside code interfering in our
        /// critical section.  Thanks to Wintellect for the hint.
        /// </remarks>
        private readonly object _monitorObject = new object();

        /// <summary>
        /// Indicates if this stream has already been disposed.
        /// </summary>
        private bool _disposed = false;

        #endregion Private Fields

        #region Ctor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <remarks>
        /// Constructs a stream with a maximum size of Int32.Max
        /// </remarks>
        internal ObjectStream()
            : this(int.MaxValue)
        {
        }

        /// <summary>
        /// Allocate the stream with an initial size.
        /// </summary>
        /// <param name="capacity">
        /// The maximum number of objects to allow in the buffer at a time.
        /// Note that this is not permitted to be more than Int32.MaxValue,
        /// since the underlying list has this limitation
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="_capacity"/> is less than or equal to zero
        /// <paramref name="_capacity"/> is greater than Int32.MaxValue
        /// </exception>
        internal ObjectStream(int capacity)
        {
            if (capacity <= 0 || capacity > int.MaxValue)
            {
                throw PSTraceSource.NewArgumentOutOfRangeException(nameof(capacity), capacity);
            }

            // the maximum number of objects to allow in the stream at a given time.
            _capacity = capacity;

            // event is not signaled since there is no data to read
            _readHandle = new AutoResetEvent(false);

            // event is signaled since there is available buffer space
            _writeHandle = new AutoResetEvent(true);

            // event is not signaled since the thread is still readable
            _readClosedHandle = new ManualResetEvent(false);

            // event is signaled since the thread is still writeable
            _writeClosedHandle = new ManualResetEvent(false);

            // the FIFO set of objects in the stream
            _objects = new List<object>();

            // Is the stream open?
            _isOpen = true;
        }

        #endregion Ctor

        #region internal properties

        /// <summary>
        /// Get the capacity of the stream.
        /// </summary>
        /// <value>
        /// The capacity of the stream.
        /// </value>
        /// <remarks>
        /// The capacity is the number of objects the stream may contain at one time.  Once this
        /// limit is reached, attempts to write into the stream block until buffer space
        /// becomes available.
        /// MaxCapacity cannot change, so we can skip the lock.
        /// </remarks>
        internal override int MaxCapacity
        {
            get
            {
                return _capacity;
            }
        }

        /// <summary>
        /// Waitable handle for callers to wait on until data ready to read.
        /// </summary>
        /// <remarks>
        /// The handle is set when data becomes available to read or
        /// when a partial read has completed.  If multiple readers
        /// are used, setting the handle does not guarantee that
        /// a read operation will return data. If using multiple
        /// reader threads, <see cref="NonBlockingRead"/> for
        /// performing non-blocking reads.
        /// </remarks>
        internal override WaitHandle ReadHandle
        {
            get
            {
                WaitHandle handle = null;

                lock (_monitorObject)
                {
                    // Create the handle signaled if there are objects in the stream
                    // or the stream has been closed.  The closed scenario addresses
                    // Pipeline readers that execute asynchronously.  Since the pipeline
                    // may complete with zero objects before the caller objects this
                    // handle, it will block indefinitely unless it is set.
                    _readWaitHandle ??= new ManualResetEvent(_objects.Count > 0 || !_isOpen);

                    handle = _readWaitHandle;
                }

                return handle;
            }
        }

        /// <summary>
        /// Waitable handle for callers to block until buffer space becomes available.
        /// </summary>
        /// <remarks>
        /// The handle is set when space becomes available for writing. For multiple
        /// writer threads writing to a bounded stream, the writer may still block
        /// if another thread fills the stream to capacity.
        /// </remarks>
        internal override WaitHandle WriteHandle
        {
            get
            {
                WaitHandle handle = null;

                lock (_monitorObject)
                {
                    _writeWaitHandle ??= new ManualResetEvent(_objects.Count < _capacity || !_isOpen);

                    handle = _writeWaitHandle;
                }

                return handle;
            }
        }

        /// <summary>
        /// Return a PipelineReader(object) for this stream.
        /// </summary>
        internal override PipelineReader<object> ObjectReader
        {
            get
            {
                PipelineReader<object> reader = null;

                lock (_monitorObject)
                {
                    // Always return an object reader, even if the stream
                    // is closed. This is to address requesting the object reader
                    // after calling Pipeline.Execute(). NOTE: If Execute completes
                    // without writing data to the output queue, the
                    // stream will be in the EndOfPipeline state because the
                    // stream is closed and has zero data.  Since this is a valid
                    // and expected execution path, we don't want to throw an exception.
                    _reader ??= new ObjectReader(this);

                    reader = _reader;
                }

                return reader;
            }
        }

        /// <summary>
        /// Return a PipelineReader(PSObject) for this stream.
        /// </summary>
        internal override PipelineReader<PSObject> PSObjectReader
        {
            get
            {
                PipelineReader<PSObject> reader = null;

                lock (_monitorObject)
                {
                    // Always return an object reader, even if the stream
                    // is closed. This is to address requesting the object reader
                    // after calling Pipeline.Execute(). NOTE: If Execute completes
                    // without writing data to the output queue, the
                    // stream will be in the EndOfPipeline state because the
                    // stream is closed and has zero data.  Since this is a valid
                    // and expected execution path, we don't want to throw an exception.
                    _mshreader ??= new PSObjectReader(this);

                    reader = _mshreader;
                }

                return reader;
            }
        }

        // 913921-2005/07/08 ObjectWriter can be retrieved on a closed stream
        /// <summary>
        /// Return an PipelineWriter for this stream.
        /// </summary>
        internal override PipelineWriter ObjectWriter
        {
            get
            {
                PipelineWriter writer = null;

                lock (_monitorObject)
                {
                    _writer ??= new ObjectWriter(this) as PipelineWriter;

                    writer = _writer;
                }

                return writer;
            }
        }

        /// <summary>
        /// Determine if we are at the end of the stream.
        /// </summary>
        /// <remarks>
        /// EndOfPipeline is defined as the stream being closed and containing
        /// zero objects.  Readers check this to determine if any objects
        /// are in the stream.  Writers should check <see cref="IsOpen"/> to determine
        /// if the stream can be written to.
        /// </remarks>
        internal override bool EndOfPipeline
        {
            get
            {
                bool endOfStream = true;

                lock (_monitorObject)
                {
                    endOfStream = (_objects.Count == 0 && !_isOpen);
                }

                return endOfStream;
            }
        }

        /// <summary>
        /// Check if the stream is open for further writes.
        /// </summary>
        /// <returns>True if the stream is open, false if not.</returns>
        /// <remarks>
        /// IsOpen returns true until the first call to Close(). Writers should
        /// check IsOpen to determine if a write operation can be made.  Note that
        /// writers need to catch <see cref="PipelineClosedException"/>.
        /// <seealso cref="EndOfPipeline"/>
        /// </remarks>
        internal override bool IsOpen
        {
            get
            {
                bool isOpen = true;
                // 2003/09/02-JonN Hitesh says that the access
                // of a bool variable is atomic so there is no need
                // for the lock.
                lock (_monitorObject)
                {
                    isOpen = _isOpen;
                }

                return isOpen;
            }
        }

        /// <summary>
        /// Returns the number of objects in the stream.
        /// </summary>
        internal override int Count
        {
            get
            {
                int count = 0;

                lock (_monitorObject)
                {
                    count = _objects.Count;
                }

                return count;
            }
        }

        #endregion internal properties

        #region private locking code

        /// <summary>
        /// Wait for data to be readable.
        /// </summary>
        /// <returns>True if EndOfPipeline is not reached.</returns>
        /// <remarks>
        /// WaitRead does not guarantee that data is present in the stream,
        /// only that data was added when the event was signaled.  Since there may be
        /// multiple readers, data may be removed from the stream
        /// before the caller has a chance to read the data.
        /// This method should never be called within a lock(_monitorObject).
        /// </remarks>
        private bool WaitRead()
        {
            if (!EndOfPipeline)
            {
                try
                {
                    WaitHandle[] ha = { _readHandle, _readClosedHandle };
                    WaitHandle.WaitAny(ha); // ignore return value
                }
                catch (ObjectDisposedException)
                {
                    // Since the _readHandle must be acquired outside
                    // a lock there's a chance that it was
                    // disposed after checking EndOfPipeline
                }
            }

            return !EndOfPipeline;
        }

        /// <summary>
        /// Wait for data to be writeable.
        /// </summary>
        /// <returns>True if the stream is writeable, otherwise; false.</returns>
        /// <remarks>
        /// WaitWrite does not guarantee that buffer space will be available in the stream
        /// when the caller attempts to write, only that buffer space was available
        /// when the event was signaled.
        /// This method should never be called within a lock(_monitorObject).
        /// </remarks>
        private bool WaitWrite()
        {
            if (IsOpen)
            {
                try
                {
                    WaitHandle[] ha = { _writeHandle, _writeClosedHandle };
                    WaitHandle.WaitAny(ha); // ignore return value
                }
                catch (ObjectDisposedException)
                {
                    // Since the _writeHandle must be acquired outside
                    // a lock there's a chance that it was
                    // disposed after checking IsOpen
                }
            }

            return IsOpen;
        }

        /// <summary>
        /// Utility method to signal handles and raise events
        /// in the consistent order.
        /// NOTE: Release the lock before raising events; otherwise,
        /// there is a possible deadlock during the readable event.
        /// </summary>
        /// <remarks>
        /// RaiseEvents is fairly idempotent, although it will signal
        /// DataReady every time.
        /// </remarks>
        private void RaiseEvents()
        {
            bool unblockReaders = true;
            bool unblockWriters = true;
            bool endOfStream = false;
            try
            {
                lock (_monitorObject)
                {
                    // External readers block only for open streams
                    // with no stored objects.  External writers block
                    // only for open streams with no free buffer space.
                    unblockReaders = (!_isOpen || (_objects.Count > 0));
                    unblockWriters = (!_isOpen || (_objects.Count < _capacity));
                    endOfStream = (!_isOpen && (_objects.Count == 0));

                    // I would prefer to set the ManualResetEvents outside
                    // of the lock, so that the unblocked thread would not
                    // immediately be re-blocked.  However, I am not
                    // confident that multiple sets/resets might not get
                    // out of order and leave the handle in the wrong state.
                    if (_readWaitHandle != null)
                    {
                        try
                        {
                            if (unblockReaders)
                            {
                                _readWaitHandle.Set();
                            }
                            else
                            {
                                _readWaitHandle.Reset();
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                        }
                    }

                    if (_writeWaitHandle != null)
                    {
                        try
                        {
                            if (unblockWriters)
                            {
                                _writeWaitHandle.Set();
                            }
                            else
                            {
                                _writeWaitHandle.Reset();
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                        }
                    }
                }
            }
            finally
            {
                // We prefer to set the AutoResetEvents outside of the lock,
                // so that the unblocked thread will not immediately be
                // re-blocked.  This works because setting the handle
                // is idempotent.
                if (unblockReaders)
                {
                    try
                    {
                        _readHandle.Set();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }

                if (unblockWriters)
                {
                    try
                    {
                        _writeHandle.Set();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }

                if (endOfStream)
                {
                    try
                    {
                        _readClosedHandle.Set();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
            }

            // This causes a synchronous call to the
            // client-provided handler.
            if (unblockReaders)
            {
                FireDataReadyEvent(this, EventArgs.Empty);
            }
#if (false)
            //
            // NOTE: Event delegates are called only after the internal
            // AutoResetEvents are set to ensure that an exception in an
            // event delegate does not leave the reset events in a bad
            // state.
            if (unblockWriters && WriteReady != null)
            {
                WriteReady (this, new EventArgs ());
            }
#endif
        }

        #endregion private locking code

        #region internal methods

        /// <summary>
        /// Flush the data from the stream.  Closed streams may be flushed.
        /// </summary>
        internal override void Flush()
        {
            bool raiseEvents = false;

            try
            {
                lock (_monitorObject)
                {
                    if (_objects.Count > 0)
                    {
                        raiseEvents = true;
                        _objects.Clear();
                    }
                }
            }
            finally
            {
                if (raiseEvents)
                {
                    RaiseEvents();
                }
            }
        }

        /// <summary>
        /// Close the stream.
        /// </summary>
        /// <remarks>
        /// Causes subsequent calls to IsOpen to return false and calls to
        /// a write operation to throw PipelineClosedException.
        /// All calls to Close() after the first call are silently ignored.
        /// </remarks>
        internal override void Close()
        {
            bool raiseEvents = false;

            try
            {
                lock (_monitorObject)
                {
                    // if we transition from open to closed,
                    // signal any blocking readers or writers
                    // to ensure the close is seen.
                    if (_isOpen)
                    {
                        raiseEvents = true;
                        _isOpen = false;
                    }
                }
            }
            finally
            {
                if (raiseEvents)
                {
                    // RaiseEvents does not manage _writeClosedHandle
                    try
                    {
                        _writeClosedHandle.Set();
                    }
                    catch (ObjectDisposedException)
                    {
                    }

                    RaiseEvents();
                }
            }
        }

        #endregion internal methods

        #region Read Methods

        /// <summary>
        /// Read a single object from the stream.
        /// </summary>
        /// <returns>The next object in the stream or AutomationNull if EndOfPipeline is reached.</returns>
        /// <remarks>This method blocks if the stream is empty</remarks>
        internal override object Read()
        {
            Collection<object> result = Read(1);
            if (result.Count == 1)
            {
                return result[0];
            }

            Diagnostics.Assert(result.Count == 0, "Invalid number of objects returned");

            return AutomationNull.Value;
        }

        /// <summary>
        /// Read at most <paramref name="count"/> objects.
        /// </summary>
        /// <param name="count">The maximum number of objects to read.</param>
        /// <returns>The objects read.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="count"/> is less than 0
        /// </exception>
        /// <remarks>
        /// This method blocks if the number of objects in the stream is less than <paramref name="count"/>
        /// and the stream is not closed.
        ///
        /// If there are multiple reader threads, the objects returned
        /// to blocking reads Read(int count) and ReadToEnd()
        /// are not necessarily single blocks of objects added to the
        /// stream in that order.  For example, if ABCDEF are added to the
        /// stream, one reader may get ABDE and the other may get CF.
        /// Each reader reads items from the stream as they become available.
        /// Otherwise, if a maximum _capacity has been imposed, the writer
        /// and reader could become mutually deadlocked.
        ///
        /// When there are multiple blocked readers, any of the readers
        /// may get the next object(s) added.
        /// </remarks>
        internal override Collection<object> Read(int count)
        {
            if (count < 0)
            {
                throw PSTraceSource.NewArgumentOutOfRangeException(nameof(count), count);
            }

            if (count == 0)
            {
                return new Collection<object>();
            }

            Collection<object> results = new Collection<object>();

            bool raiseEvents = false;
            while ((count > 0) && WaitRead())
            {
                try
                {
                    lock (_monitorObject)
                    {
                        // double check to ensure data is ready
                        if (_objects.Count == 0)
                        {
                            continue;    // wait some more
                        }

                        raiseEvents = true;
                        // NTRAID#Windows Out Of Band Releases-925566-2005/12/07-JonN
                        int objectsAdded = 0;
                        foreach (object o in _objects)
                        {
                            results.Add(o);
                            objectsAdded++;
                            if (--count <= 0)
                                break;
                        }

                        _objects.RemoveRange(0, objectsAdded);
                    }
                }
                finally
                {
                    // Raise the appropriate read/write events outside the lock. This is
                    // inside the while loop to ensure writers can have a chance to
                    // write otherwise the reader will starve.
                    // NOTE: This must occur in the finally block to ensure
                    // the AutoResetEvents are left in the appropriate state, even
                    // for error paths.
                    if (raiseEvents)
                    {
                        RaiseEvents();
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Blocks until the pipeline closes and reads all objects.
        /// </summary>
        /// <returns>A collection of zero or more objects.</returns>
        /// <remarks>
        /// If the stream is empty, a collection of size zero is returned.
        ///
        /// If there are multiple reader threads, the objects returned
        /// to blocking reads Read(int count) and ReadToEnd()
        /// are not necessarily single blocks of objects added to the
        /// stream in that order.  For example, if ABCDEF are added to the
        /// stream, one reader may get ABDE and the other may get CF.
        /// Each reader reads items from the stream as they become available.
        /// Otherwise, if a maximum _capacity has been imposed, the writer
        /// and reader could become mutually deadlocked.
        ///
        /// When there are multiple blocked readers, any of the readers
        /// may get the next object(s) added.
        /// </remarks>
        internal override Collection<object> ReadToEnd()
        {
            // NTRAID#Windows Out Of Band Releases-925566-2005/12/07-JonN
            return Read(int.MaxValue);
        }

        /// <summary>
        /// Reads objects currently in the stream, but does not block.
        /// </summary>
        /// <returns>An array of zero or more objects.</returns>
        /// <remarks>
        /// This method performs a read of objects currently in the
        /// stream. The method will block until exclusive access to the
        /// stream is acquired.  If there are no objects in the stream,
        /// an empty array is returned.
        /// </remarks>
        /// <param name="maxRequested">
        /// Return no more than maxRequested objects.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="maxRequested"/> is less than 0
        /// </exception>
        internal override Collection<object> NonBlockingRead(int maxRequested)
        {
            Collection<object> results = null;
            bool raiseEvents = false;

            if (maxRequested == 0)
            {
                return new Collection<object>();
            }

            if (maxRequested < 0)
            {
                throw PSTraceSource.NewArgumentOutOfRangeException(nameof(maxRequested), maxRequested);
            }

            try
            {
                lock (_monitorObject)
                {
                    int readCount = _objects.Count;
                    if (readCount > maxRequested)
                    {
                        // 2004/06/30-JonN Safe cast since 0 < maxRequested < readCount
                        readCount = (int)maxRequested;
                    }

                    if (readCount > 0)
                    {
                        results = new Collection<object>();
                        for (int i = 0; i < readCount; i++)
                        {
                            results.Add(_objects[i]);
                        }

                        raiseEvents = true;
                        _objects.RemoveRange(0, readCount);
                    }
                }
            }
            finally
            {
                if (raiseEvents)
                {
                    RaiseEvents();
                }
            }

            return results ?? new Collection<object>();
        }

        /// <summary>
        /// Peek the next object.
        /// </summary>
        /// <returns>
        /// The next object in the stream or AutomationNull.Value if the stream is empty
        /// </returns>
        /// <exception cref="PipelineClosedException">The ObjectStream is closed.</exception>
        internal override object Peek()
        {
            object result = null;

            lock (_monitorObject)
            {
                if (EndOfPipeline || _objects.Count == 0)
                {
                    result = AutomationNull.Value;
                }
                else
                {
                    result = _objects[0];
                }
            }

            return result;
        }

        #endregion Read Methods

        #region Write Methods

        /// <summary>
        /// Write objects to the underlying stream.
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
        /// The underlying stream is closed
        /// </exception>
        /// <remarks>
        /// If the enumeration contains elements equal to
        /// AutomationNull.Value, they are ignored.
        /// This can cause the return value to be less than the size of
        /// the collection.
        /// </remarks>
        internal override int Write(object obj, bool enumerateCollection)
        {
            // it is permitted to write null objects

            if (obj == AutomationNull.Value)
            {
                // NTRAID#Windows Out Of Band Releases-926213-2005/12/16-JonN
                // We no longer break on AutomationNull.Value,
                // we just ignore it
                return 0;
            }

            if (!IsOpen)
            {
                // NTRAID#Windows Out Of Band Releases-925742-2005/12/07-JonN
                string message = PipelineStrings.WriteToClosedPipeline;
                Exception e = new PipelineClosedException(message);
                throw e;
            }

            // We want to write the objects as one block, not individually
            // We do not want to hold the stream locked during the enumeration
            List<object> a = new List<object>();

            IEnumerable enumerable = null;
            if (enumerateCollection)
            {
                enumerable = LanguagePrimitives.GetEnumerable(obj);
            }

            if (enumerable == null)
                a.Add(obj);
            else
            {
                foreach (object o in enumerable)
                {
                    // 879023-2003/10/28-JonN
                    //  Outputting stops when receiving a AutomationNull.Value
                    // 2003/10/28-JonN There is a window where another
                    //  thread could modify the array to contain
                    //  AutomationNull.Value, but I'm not going to deal with it.
                    if (AutomationNull.Value == o)
                    {
                        // NTRAID#Windows Out Of Band Releases-926213-2005/12/16-JonN
                        // We no longer break on AutomationNull.Value,
                        // we just ignore it
                        continue;
                    }

                    a.Add(o);
                }
            }

            int objectsWritten = 0;
            int objectsToWrite = a.Count;

            while (objectsToWrite > 0)
            {
                bool raiseEvents = false;

                // wait for buffer available
                // false indicates EndOfPipeline
                if (!WaitWrite())
                {
                    break;
                }

                try
                {
                    lock (_monitorObject)
                    {
                        if (!IsOpen)
                        {
                            // NOTE: lock is released in finally
                            break;
                        }

                        // determine the maximum number of objects that
                        // can be written to the stream. Note: performing
                        // subtraction to ensure we don't have an
                        // overflow exception
                        int freeSpace = _capacity - _objects.Count;
                        if (freeSpace <= 0)
                        {
                            // NOTE: lock is released in finally
                            continue;
                        }

                        int writeCount = objectsToWrite;
                        if (writeCount > freeSpace)
                        {
                            // Note that we have already established that
                            // 0 < freeSpace < writeCount,
                            // so the cast is safe.
                            writeCount = freeSpace;
                        }

                        try
                        {
                            // determine if we can write to the stream in
                            // a single call.
                            if (writeCount == a.Count)
                            {
                                System.Management.Automation.Diagnostics.Assert
                                    (objectsWritten == 0, "objectsWritten == 0");
                                _objects.AddRange(a);
                                objectsWritten += writeCount;
                                objectsToWrite -= writeCount;
                                System.Management.Automation.Diagnostics.Assert
                                    (objectsToWrite == 0, "objectsToWrite == 0");
                            }
                            else
                            {
                                System.Management.Automation.Diagnostics.Assert
                                    (writeCount > 0, "writeCount > 0");
                                List<object> a2 = a.GetRange(objectsWritten, writeCount);
                                _objects.AddRange(a2);
                                objectsWritten += writeCount;
                                objectsToWrite -= writeCount;
                            }
                        }
                        finally
                        {
                            raiseEvents = true;
                        }
                    }
                }
                finally
                {
                    if (raiseEvents)
                    {
                        RaiseEvents();
                    }
                }
            }

            return objectsWritten;
        }

        #endregion Write Methods

        // 905990-2005/05/10-JonN Removed IDisposable

        #region Design For Testability
        /// <summary>
        /// These methods are necessary to provide the ObjectStreamTest BVT
        /// access to the internal handler DataReady.  The EventInfo
        /// reflection class does not give access to internal events.
        /// </summary>
        /// <param name="eventHandler"></param>
        private void DFT_AddHandler_OnDataReady(EventHandler eventHandler)
        {
            DataReady += eventHandler;
        }

        private void DFT_RemoveHandler_OnDataReady(EventHandler eventHandler)
        {
            DataReady -= eventHandler;
        }
        #endregion Design For Testability

        #region IDisposable

        /// <summary>
        /// Release all resources.
        /// </summary>
        /// <param name="disposing">If true, release all managed resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            lock (_monitorObject)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
            }

            if (disposing)
            {
                _readHandle.Dispose();
                _writeHandle.Dispose();
                _writeClosedHandle.Dispose();
                _readClosedHandle.Dispose();
                _readWaitHandle?.Dispose();
                _writeWaitHandle?.Dispose();

                if (_reader != null)
                {
                    _reader.Close();
                    _reader.WaitHandle.Dispose();
                }

                if (_writer != null)
                {
                    _writer.Close();
                    _writer.WaitHandle.Dispose();
                }
            }
        }

        #endregion IDisposable
    }

    /// <summary>
    /// An object stream using a PSDataCollection as the object store.
    /// This stream lets user to supply a custom PSDataCollection instance
    /// to use as the stream's object store.
    ///
    /// This stream is designed to be used with the user supplied
    /// PSDataBuffers. For internal only purposes use ObjectStream.
    /// </summary>
    internal sealed class PSDataCollectionStream<T> : ObjectStreamBase
    {
        #region Private Fields

        private readonly PSDataCollection<T> _objects;
        private readonly Guid _psInstanceId;
        private bool _isOpen;
        private PipelineWriter _writer;
        private PipelineReader<object> _objectReader;
        private PipelineReader<PSObject> _psobjectReader;
        private PipelineReader<object> _objectReaderForPipeline;
        private PipelineReader<PSObject> _psobjectReaderForPipeline;
        private readonly object _syncObject = new object();
        private bool _disposed = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the stream and uses the supplied <paramref name="storeToUse"/>
        /// as the object store.
        /// </summary>
        /// <param name="psInstanceId">
        /// Guid of Powershell instance creating this stream.
        /// </param>
        /// <param name="storeToUse">
        /// A PSDataCollection instance.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// 1. storeToUse is null
        /// </exception>
        internal PSDataCollectionStream(Guid psInstanceId, PSDataCollection<T> storeToUse)
        {
            if (storeToUse == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(storeToUse));
            }

            _objects = storeToUse;
            _psInstanceId = psInstanceId;
            _isOpen = true;
            // increment ref count for the store. PowerShell engine
            // is about to use this store.
            storeToUse.AddRef();

            storeToUse.DataAdded += HandleDataAdded;
            storeToUse.Completed += HandleClosed;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the PSDataCollection used to store data for this stream.
        /// </summary>
        internal PSDataCollection<T> ObjectStore
        {
            get
            {
                return _objects;
            }
        }

        #endregion

        #region Virtual Implementation

        /// <summary>
        /// Returns the number of objects in the stream.
        /// </summary>
        internal override int Count
        {
            get
            {
                return _objects.Count;
            }
        }

        /// <summary>
        /// This is not supported.
        /// </summary>
        internal override bool EndOfPipeline
        {
            get
            {
                bool endOfStream = true;

                lock (_syncObject)
                {
                    endOfStream = (_objects.Count == 0 && !_isOpen);
                }

                return endOfStream;
            }
        }

        /// <summary>
        /// Check if the stream is open for further writes.
        /// </summary>
        /// <returns>True if the stream is open, false if not.</returns>
        /// <remarks>
        /// IsOpen returns true until the first call to Close(). Writers should
        /// check IsOpen to determine if a write operation can be made.
        /// </remarks>
        internal override bool IsOpen
        {
            get
            {
                // check both the current stream and the object store.
                return _isOpen && _objects.IsOpen;
            }
        }

        /// <summary>
        /// This is not supported.
        /// </summary>
        internal override int MaxCapacity
        {
            get
            {
#pragma warning disable 56503
                throw PSTraceSource.NewNotSupportedException();
#pragma warning restore 56503
            }
        }

        /// <summary>
        /// Return a PipelineReader(object) for this stream.
        /// </summary>
        internal override PipelineReader<object> ObjectReader
        {
            get
            {
                if (_objectReader == null)
                {
                    lock (_syncObject)
                    {
                        _objectReader ??= new PSDataCollectionReader<T, object>(this);
                    }
                }

                return _objectReader;
            }
        }

        /// <summary>
        /// Creates an Object Reader for the pipeline.
        /// </summary>
        /// <param name="computerName">Computer name that the pipeline specifies.</param>
        /// <param name="runspaceId">Runspace id that the pipeline specifies.</param>
        /// <remarks>the computer name and runspace id are associated with the
        /// reader so as to enable cmdlets to identify which computer name runspace does
        /// the object that this stream writes belongs to</remarks>
        internal PipelineReader<object> GetObjectReaderForPipeline(string computerName, Guid runspaceId)
        {
            if (_objectReaderForPipeline == null)
            {
                lock (_syncObject)
                {
                    _objectReaderForPipeline ??=
                        new PSDataCollectionPipelineReader<T, object>(this, computerName, runspaceId);
                }
            }

            return _objectReaderForPipeline;
        }

        /// <summary>
        /// Return a PipelineReader(PSObject) for this stream.
        /// </summary>
        internal override PipelineReader<PSObject> PSObjectReader
        {
            get
            {
                if (_psobjectReader == null)
                {
                    lock (_syncObject)
                    {
                        _psobjectReader ??= new PSDataCollectionReader<T, PSObject>(this);
                    }
                }

                return _psobjectReader;
            }
        }

        /// <summary>
        /// Creates a PSObject Reader for this pipeline.
        /// </summary>
        /// <param name="computerName">Computer name that the pipeline specifies.</param>
        /// <param name="runspaceId">Runspace id that the pipeline specifies.</param>
        /// <remarks>the computer name and runspace id are associated with the
        /// reader so as to enable cmdlets to identify which computer name runspace does
        /// the object that this stream writes belongs to</remarks>
        internal PipelineReader<PSObject> GetPSObjectReaderForPipeline(string computerName, Guid runspaceId)
        {
            if (_psobjectReaderForPipeline == null)
            {
                lock (_syncObject)
                {
                    _psobjectReaderForPipeline ??=
                        new PSDataCollectionPipelineReader<T, PSObject>(this, computerName, runspaceId);
                }
            }

            return _psobjectReaderForPipeline;
        }

        /// <summary>
        /// The object writer for this stream.
        /// </summary>
        /// <remarks>
        /// This field is allocated on first demand and
        /// returned on subsequent calls.
        /// </remarks>
        internal override PipelineWriter ObjectWriter
        {
            get
            {
                if (_writer == null)
                {
                    lock (_syncObject)
                    {
                        _writer ??= new PSDataCollectionWriter<T>(this) as PipelineWriter;
                    }
                }

                return _writer;
            }
        }

        /// <summary>
        /// Read handle associated with this stream.
        /// </summary>
        internal override WaitHandle ReadHandle
        {
            get
            {
                return _objects.WaitHandle;
            }
        }

        #endregion

        #region Write Abstractions

        /// <summary>
        /// Write object(s) to the databuffer.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="enumerateCollection"></param>
        /// <returns></returns>
        internal override int Write(object obj, bool enumerateCollection)
        {
            // it is permitted to write null objects

            if (obj == AutomationNull.Value)
            {
                // We no longer break on AutomationNull.Value,
                // we just ignore it
                return 0;
            }

            if (!IsOpen)
            {
                // NTRAID#Windows Out Of Band Releases-925742-2005/12/07-JonN
                string message = PSDataBufferStrings.WriteToClosedBuffer;
                Exception e = new PipelineClosedException(message);
                throw e;
            }

            // We want to write the objects as one block, not individually
            // We do not want to hold the stream locked during the enumeration
            Collection<T> objectsToAdd = new Collection<T>();

            IEnumerable enumerable = null;
            if (enumerateCollection)
            {
                enumerable = LanguagePrimitives.GetEnumerable(obj);
            }

            if (enumerable == null)
            {
                objectsToAdd.Add((T)LanguagePrimitives.ConvertTo(obj,
                    typeof(T), Globalization.CultureInfo.InvariantCulture));
            }
            else
            {
                foreach (object o in enumerable)
                {
                    //  Outputting stops when receiving a AutomationNull.Value
                    //  There is a window where another thread could modify the
                    //  array to contain AutomationNull.Value,
                    //  but I'm not going to deal with it.
                    if (AutomationNull.Value == o)
                    {
                        // We no longer break on AutomationNull.Value,
                        // we just ignore it
                        continue;
                    }

                    objectsToAdd.Add((T)LanguagePrimitives.ConvertTo(obj,
                        typeof(T), Globalization.CultureInfo.InvariantCulture));
                }
            }

            _objects.InternalAddRange(_psInstanceId, objectsToAdd);

            return objectsToAdd.Count;
        }

        #endregion

        #region Virtual Method Implementation

        /// <summary>
        /// This stream do not perform close operations on the buffer.
        /// It is upto the user to close the buffers as and when needed.
        /// However this method notifies the buffer by decrementing the
        /// ref count.
        /// </summary>
        internal override void Close()
        {
            bool raiseEvents = false;
            lock (_syncObject)
            {
                // Make sure decrementref is called only once. if this
                // stream is already closed..no need to decrement again.
                // isOpen controls if this current stream is open or not.
                if (_isOpen)
                {
                    // Decrement ref count (and other event handlers) for the store.
                    // PowerShell engine is done using this store.
                    _objects.DecrementRef();
                    _objects.DataAdded -= HandleDataAdded;
                    _objects.Completed -= HandleClosed;

                    raiseEvents = true;
                    _isOpen = false;
                }
            }

            if (raiseEvents)
            {
                FireDataReadyEvent(this, EventArgs.Empty);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleClosed(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleDataAdded(object sender, DataAddedEventArgs e)
        {
            FireDataReadyEvent(this, EventArgs.Empty);
        }

        #endregion Event Handlers

        #region IDisposable

        /// <summary>
        /// Release all resources.
        /// </summary>
        /// <param name="disposing">If true, release all resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            lock (_syncObject)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
            }

            if (disposing)
            {
                _objects.Dispose();

                Close();

                if (_objectReaderForPipeline != null)
                {
                    ((PSDataCollectionPipelineReader<T, object>)_objectReaderForPipeline).Dispose();
                }

                if (_psobjectReaderForPipeline != null)
                {
                    ((PSDataCollectionPipelineReader<T, PSObject>)_psobjectReaderForPipeline).Dispose();
                }
            }
        }
        #endregion IDisposable
    }
}
