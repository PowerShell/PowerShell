// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation.Internal;
using System.Management.Automation.Tracing;
using System.Text;
using System.Xml;

using Dbg = System.Management.Automation.Diagnostics;
using TypeTable = System.Management.Automation.Runspaces.TypeTable;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// This class is used to hold a fragment of remoting PSObject for transporting to remote computer.
    ///
    /// A large remoting PSObject will be broken into fragments. Each fragment has a ObjectId and a FragmentId.
    /// The first fragment has a StartFragment marker. The last fragment also an EndFragment marker.
    /// These fragments can be reassembled on the receiving
    /// end by sequencing the fragment ids.
    ///
    /// Currently control objects (Control-C for stopping a pipeline execution) is not
    /// really fragmented. These objects are small. They are just wrapped into a single
    /// fragment.
    /// </summary>
    internal class FragmentedRemoteObject
    {
        private byte[] _blob;
        private int _blobLength;

        /// <summary>
        /// SFlag stands for the IsStartFragment. It is the bit value in the binary encoding.
        /// </summary>
        internal const byte SFlag = 0x1;

        /// <summary>
        /// EFlag stands for the IsEndFragment. It is the bit value in the binary encoding.
        /// </summary>
        internal const byte EFlag = 0x2;

        /// <summary>
        /// HeaderLength is the total number of bytes in the binary encoding header.
        /// </summary>
        internal const int HeaderLength = 8 + 8 + 1 + 4;

        /// <summary>
        /// _objectIdOffset is the offset of the ObjectId in the binary encoding.
        /// </summary>
        private const int _objectIdOffset = 0;

        /// <summary>
        /// _fragmentIdOffset is the offset of the FragmentId in the binary encoding.
        /// </summary>
        private const int _fragmentIdOffset = 8;

        /// <summary>
        /// _flagsOffset is the offset of the byte in the binary encoding that contains the SFlag, EFlag and CFlag.
        /// </summary>
        private const int _flagsOffset = 16;

        /// <summary>
        /// _blobLengthOffset is the offset of the BlobLength in the binary encoding.
        /// </summary>
        private const int _blobLengthOffset = 17;

        /// <summary>
        /// _blobOffset is the offset of the Blob in the binary encoding.
        /// </summary>
        private const int _blobOffset = 21;

        #region Constructors

        /// <summary>
        /// Default Constructor.
        /// </summary>
        internal FragmentedRemoteObject()
        {
        }

        /// <summary>
        /// Used to construct a fragment of PSObject to be sent to remote computer.
        /// </summary>
        /// <param name="blob"></param>
        /// <param name="objectId">
        /// ObjectId of the fragment.
        /// Caller should make sure this is not less than 0.
        /// </param>
        /// <param name="fragmentId">
        /// FragmentId within the object.
        /// Caller should make sure this is not less than 0.
        /// </param>
        /// <param name="isEndFragment">
        /// true if this is a EndFragment.
        /// </param>
        internal FragmentedRemoteObject(byte[] blob, long objectId, long fragmentId,
            bool isEndFragment)
        {
            Dbg.Assert((blob != null) && (blob.Length != 0), "Cannot create a fragment for null or empty data.");
            Dbg.Assert(objectId >= 0, "Object Id cannot be < 0");
            Dbg.Assert(fragmentId >= 0, "Fragment Id cannot be < 0");

            ObjectId = objectId;
            FragmentId = fragmentId;

            IsStartFragment = fragmentId == 0;
            IsEndFragment = isEndFragment;

            _blob = blob;
            _blobLength = _blob.Length;
        }

        #endregion Constructors

        #region Data Fields being sent

        /// <summary>
        /// All fragments of the same PSObject have the same ObjectId.
        /// </summary>
        internal long ObjectId { get; set; }

        /// <summary>
        /// FragmentId starts from 0. It increases sequentially by an increment of 1.
        /// </summary>
        internal long FragmentId { get; set; }

        /// <summary>
        /// The first fragment of a PSObject.
        /// </summary>
        internal bool IsStartFragment { get; set; }

        /// <summary>
        /// The last fragment of a PSObject.
        /// </summary>
        internal bool IsEndFragment { get; set; }

        /// <summary>
        /// Blob length. This enables scenarios where entire byte[] is
        /// not filled for the fragment.
        /// </summary>
        internal int BlobLength
        {
            get
            {
                return _blobLength;
            }

            set
            {
                Dbg.Assert(value >= 0, "BlobLength cannot be less than 0.");
                _blobLength = value;
            }
        }

        /// <summary>
        /// This is the actual data in bytes form.
        /// </summary>
        internal byte[] Blob
        {
            get
            {
                return _blob;
            }

            set
            {
                Dbg.Assert(value != null, "Blob cannot be null");
                _blob = value;
            }
        }

        #endregion Data Fields being sent

        /// <summary>
        /// This method generate a binary encoding of the FragmentedRemoteObject as follows:
        /// ObjectId: 8 bytes as long, byte order is big-endian. this value can only be non-negative.
        /// FragmentId: 8 bytes as long, byte order is big-endian. this value can only be non-negative.
        /// FlagsByte: 1 byte:
        ///       0x1 if IsStartOfFragment is true: This is called S-flag.
        ///       0x2 if IsEndOfFragment is true: This is called the E-flag.
        ///       0x4 if IsControl is true: This is called the C-flag.
        ///
        ///       The other bits are reserved for future use.
        ///       Now they must be zero when sending,
        ///       and they are ignored when receiving.
        /// BlobLength: 4 bytes as int, byte order is big-endian. this value can only be non-negative.
        /// Blob: BlobLength number of bytes.
        ///
        ///     0                   1                   2                   3
        ///     0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
        ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        ///     |                                                               |
        ///     +-+-+-+-+-+-+-+-         ObjectId               +-+-+-+-+-+-+-+-+
        ///     |                                                               |
        ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        ///     |                                                               |
        ///     +-+-+-+-+-+-+-+-        FragmentId              +-+-+-+-+-+-+-+-+
        ///     |                                                               |
        ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        ///     |reserved |C|E|S|
        ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        ///     |                        BlobLength                             |
        ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        ///     |     Blob ...
        ///     +-+-+-+-+-+-+-+-
        /// </summary>
        /// <returns>
        /// The binary encoded FragmentedRemoteObject to be ready to pass to WinRS Send API.
        /// </returns>
        internal byte[] GetBytes()
        {
            const int objectIdSize = 8; // number of bytes of long
            const int fragmentIdSize = 8; // number of bytes of long
            const int flagsSize = 1; // 1 byte for IsEndOfFrag and IsControl
            const int blobLengthSize = 4; // number of bytes of int

            int totalLength = objectIdSize + fragmentIdSize + flagsSize + blobLengthSize + BlobLength;

            byte[] result = new byte[totalLength];

            int idx = 0;

            // release build will optimize the calculation of the constants

            // ObjectId
            idx = _objectIdOffset;
            result[idx++] = (byte)((ObjectId >> (7 * 8)) & 0x7F); // sign bit is 0
            result[idx++] = (byte)((ObjectId >> (6 * 8)) & 0xFF);
            result[idx++] = (byte)((ObjectId >> (5 * 8)) & 0xFF);
            result[idx++] = (byte)((ObjectId >> (4 * 8)) & 0xFF);
            result[idx++] = (byte)((ObjectId >> (3 * 8)) & 0xFF);
            result[idx++] = (byte)((ObjectId >> (2 * 8)) & 0xFF);
            result[idx++] = (byte)((ObjectId >> 8) & 0xFF);
            result[idx++] = (byte)(ObjectId & 0xFF);

            // FragmentId
            idx = _fragmentIdOffset;
            result[idx++] = (byte)((FragmentId >> (7 * 8)) & 0x7F); // sign bit is 0
            result[idx++] = (byte)((FragmentId >> (6 * 8)) & 0xFF);
            result[idx++] = (byte)((FragmentId >> (5 * 8)) & 0xFF);
            result[idx++] = (byte)((FragmentId >> (4 * 8)) & 0xFF);
            result[idx++] = (byte)((FragmentId >> (3 * 8)) & 0xFF);
            result[idx++] = (byte)((FragmentId >> (2 * 8)) & 0xFF);
            result[idx++] = (byte)((FragmentId >> 8) & 0xFF);
            result[idx++] = (byte)(FragmentId & 0xFF);

            // E-flag and S-Flag
            idx = _flagsOffset;
            byte s_flag = IsStartFragment ? SFlag : (byte)0;
            byte e_flag = IsEndFragment ? EFlag : (byte)0;

            result[idx++] = (byte)(s_flag | e_flag);

            // BlobLength
            idx = _blobLengthOffset;
            result[idx++] = (byte)((BlobLength >> (3 * 8)) & 0xFF);
            result[idx++] = (byte)((BlobLength >> (2 * 8)) & 0xFF);
            result[idx++] = (byte)((BlobLength >> 8) & 0xFF);
            result[idx++] = (byte)(BlobLength & 0xFF);

            Array.Copy(_blob, 0, result, _blobOffset, BlobLength);

            return result;
        }

        /// <summary>
        /// Extract the objectId from a byte array, starting at the index indicated by
        /// startIndex parameter.
        /// </summary>
        /// <param name="fragmentBytes"></param>
        /// <param name="startIndex"></param>
        /// <returns>
        /// The objectId.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If fragmentBytes is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If startIndex is negative or fragmentBytes is not large enough to hold the entire header of
        /// a binary encoded FragmentedRemoteObject.
        /// </exception>
        internal static long GetObjectId(byte[] fragmentBytes, int startIndex)
        {
            Dbg.Assert(fragmentBytes != null, "fragmentBytes cannot be null");
            Dbg.Assert(fragmentBytes.Length >= HeaderLength, "not enough data to decode object id");
            long objectId = 0;

            int idx = startIndex + _objectIdOffset;

            objectId = (((long)fragmentBytes[idx++]) << (7 * 8)) & 0x7F00000000000000;
            objectId += (((long)fragmentBytes[idx++]) << (6 * 8)) & 0xFF000000000000;
            objectId += (((long)fragmentBytes[idx++]) << (5 * 8)) & 0xFF0000000000;
            objectId += (((long)fragmentBytes[idx++]) << (4 * 8)) & 0xFF00000000;
            objectId += (((long)fragmentBytes[idx++]) << (3 * 8)) & 0xFF000000;
            objectId += (((long)fragmentBytes[idx++]) << (2 * 8)) & 0xFF0000;
            objectId += (((long)fragmentBytes[idx++]) << 8) & 0xFF00;
            objectId += ((long)fragmentBytes[idx++]) & 0xFF;

            return objectId;
        }

        /// <summary>
        /// Extract the FragmentId from the byte array, starting at the index indicated by
        /// startIndex parameter.
        /// </summary>
        /// <param name="fragmentBytes"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// If fragmentBytes is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If startIndex is negative or fragmentBytes is not large enough to hold the entire header of
        /// a binary encoded FragmentedRemoteObject.
        /// </exception>
        internal static long GetFragmentId(byte[] fragmentBytes, int startIndex)
        {
            Dbg.Assert(fragmentBytes != null, "fragmentBytes cannot be null");
            Dbg.Assert(fragmentBytes.Length >= HeaderLength, "not enough data to decode fragment id");
            long fragmentId = 0;
            int idx = startIndex + _fragmentIdOffset;

            fragmentId = (((long)fragmentBytes[idx++]) << (7 * 8)) & 0x7F00000000000000;
            fragmentId += (((long)fragmentBytes[idx++]) << (6 * 8)) & 0xFF000000000000;
            fragmentId += (((long)fragmentBytes[idx++]) << (5 * 8)) & 0xFF0000000000;
            fragmentId += (((long)fragmentBytes[idx++]) << (4 * 8)) & 0xFF00000000;
            fragmentId += (((long)fragmentBytes[idx++]) << (3 * 8)) & 0xFF000000;
            fragmentId += (((long)fragmentBytes[idx++]) << (2 * 8)) & 0xFF0000;
            fragmentId += (((long)fragmentBytes[idx++]) << 8) & 0xFF00;
            fragmentId += ((long)fragmentBytes[idx++]) & 0xFF;

            return fragmentId;
        }

        /// <summary>
        /// Extract the IsStartFragment value from the byte array, starting at the index indicated by
        /// startIndex parameter.
        /// </summary>
        /// <param name="fragmentBytes"></param>
        /// <param name="startIndex"></param>
        /// <returns>
        /// True is the S-flag is set in the encoding. Otherwise false.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If fragmentBytes is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If startIndex is negative or fragmentBytes is not large enough to hold the entire header of
        /// a binary encoded FragmentedRemoteObject.
        /// </exception>
        internal static bool GetIsStartFragment(byte[] fragmentBytes, int startIndex)
        {
            Dbg.Assert(fragmentBytes != null, "fragment cannot be null");
            Dbg.Assert(fragmentBytes.Length >= HeaderLength, "not enough data to decode if it is a start fragment.");

            if ((fragmentBytes[startIndex + _flagsOffset] & SFlag) != 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Extract the IsEndFragment value from the byte array, starting at the index indicated by
        /// startIndex parameter.
        /// </summary>
        /// <param name="fragmentBytes"></param>
        /// <param name="startIndex"></param>
        /// <returns>
        /// True if the E-flag is set in the encoding. Otherwise false.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If fragmentBytes is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If startIndex is negative or fragmentBytes is not large enough to hold the entire header of
        /// a binary encoded FragmentedRemoteObject.
        /// </exception>
        internal static bool GetIsEndFragment(byte[] fragmentBytes, int startIndex)
        {
            Dbg.Assert(fragmentBytes != null, "fragment cannot be null");
            Dbg.Assert(fragmentBytes.Length >= HeaderLength, "not enough data to decode if it is an end fragment.");

            if ((fragmentBytes[startIndex + _flagsOffset] & EFlag) != 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Extract the BlobLength value from the byte array, starting at the index indicated by
        /// startIndex parameter.
        /// </summary>
        /// <param name="fragmentBytes"></param>
        /// <param name="startIndex"></param>
        /// <returns>
        /// The BlobLength value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If fragmentBytes is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If startIndex is negative or fragmentBytes is not large enough to hold the entire header of
        /// a binary encoded FragmentedRemoteObject.
        /// </exception>
        internal static int GetBlobLength(byte[] fragmentBytes, int startIndex)
        {
            Dbg.Assert(fragmentBytes != null, "fragment cannot be null");
            Dbg.Assert(fragmentBytes.Length >= HeaderLength, "not enough data to decode blob length.");

            int blobLength = 0;
            int idx = startIndex + _blobLengthOffset;

            blobLength += (((int)fragmentBytes[idx++]) << (3 * 8)) & 0x7F000000;
            blobLength += (((int)fragmentBytes[idx++]) << (2 * 8)) & 0xFF0000;
            blobLength += (((int)fragmentBytes[idx++]) << 8) & 0xFF00;
            blobLength += ((int)fragmentBytes[idx++]) & 0xFF;

            return blobLength;
        }
    }

    /// <summary>
    /// A stream used to store serialized data. This stream holds serialized data in the
    /// form of fragments. Every "fragment size" data will hold a blob identifying the fragment.
    /// The blob has "ObjectId","FragmentId","Properties like Start,End","BlobLength"
    /// </summary>
    internal class SerializedDataStream : Stream, IDisposable
    {
        [TraceSource("SerializedDataStream", "SerializedDataStream")]
        private static readonly PSTraceSource s_trace = PSTraceSource.GetTracer("SerializedDataStream", "SerializedDataStream");
        #region Global Constants

        private static long s_objectIdSequenceNumber = 0;

        #endregion

        #region Private Data

        private bool _isEntered;
        private readonly FragmentedRemoteObject _currentFragment;
        private long _fragmentId;

        private readonly int _fragmentSize;
        private readonly object _syncObject;
        private bool _isDisposed;
        private readonly bool _notifyOnWriteFragmentImmediately;

        // MemoryStream does not dynamically resize as data is read. This will waste
        // lot of memory as data sent on the network will still be there in memory.
        // To avoid this a queue of memory streams (each stream is of fragmentsize)
        // is created..so after data is sent the MemoryStream is disposed there by
        // clearing resources.
        private readonly Queue<MemoryStream> _queuedStreams;
        private MemoryStream _writeStream;
        private MemoryStream _readStream;
        private int _writeOffset;
        private int _readOffSet;
        private long _length;

        /// <summary>
        /// Callback that is called once a fragmented data is available.
        /// </summary>
        /// <param name="data">
        /// Data that resulted in this callback.
        /// </param>
        /// <param name="isEndFragment">
        /// true if data represents EndFragment of an object.
        /// </param>
        internal delegate void OnDataAvailableCallback(byte[] data, bool isEndFragment);

        private OnDataAvailableCallback _onDataAvailableCallback;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a stream to hold serialized data.
        /// </summary>
        /// <param name="fragmentSize">
        /// fragmentSize to be used while creating fragment boundaries.
        /// </param>
        internal SerializedDataStream(int fragmentSize)
        {
            s_trace.WriteLine("Creating SerializedDataStream with fragmentsize : {0}", fragmentSize);
            Dbg.Assert(fragmentSize > 0, "fragmentsize should be greater than 0.");
            _syncObject = new object();
            _currentFragment = new FragmentedRemoteObject();
            _queuedStreams = new Queue<MemoryStream>();
            _fragmentSize = fragmentSize;
        }

        /// <summary>
        /// Use this constructor carefully. This will not write data into internal
        /// streams. Instead this will make the SerializedDataStream call the
        /// callback whenever a fragmented data is available. It is upto the caller
        /// to figure out what to do with the data.
        /// </summary>
        /// <param name="fragmentSize">
        /// fragmentSize to be used while creating fragment boundaries.
        /// </param>
        /// <param name="callbackToNotify">
        /// If this is not null, then callback will get notified whenever fragmented
        /// data is available. Read() will return null in this case always.
        /// </param>
        internal SerializedDataStream(int fragmentSize,
            OnDataAvailableCallback callbackToNotify) : this(fragmentSize)
        {
            if (callbackToNotify != null)
            {
                _notifyOnWriteFragmentImmediately = true;
                _onDataAvailableCallback = callbackToNotify;
            }
        }

        #endregion

        #region Internal methods / Protected overrides

        /// <summary>
        /// Start using the stream exclusively (to write data). The stream can be entered only once.
        /// If you want to Enter again, first Exit and then Enter.
        /// This method is not thread-safe.
        /// </summary>
        internal void Enter()
        {
            Dbg.Assert(!_isEntered, "Stream is already entered. You cannot enter into stream again.");
            _isEntered = true;
            _fragmentId = 0;

            // Initialize the current fragment
            _currentFragment.ObjectId = GetObjectId();
            _currentFragment.FragmentId = _fragmentId;
            _currentFragment.IsStartFragment = true;
            _currentFragment.BlobLength = 0;
            _currentFragment.Blob = new byte[_fragmentSize];
        }

        /// <summary>
        /// Notify that the stream is not used to write anymore.
        /// This method is not thread-safe.
        /// </summary>
        internal void Exit()
        {
            _isEntered = false;
            // write left over data
            if (_currentFragment.BlobLength > 0)
            {
                // this is endfragment...as we are in Exit
                _currentFragment.IsEndFragment = true;
                WriteCurrentFragmentAndReset();
            }
        }

        /// <summary>
        /// Writes a block of bytes to the current stream using data read from buffer.
        /// The base MemoryStream is written to only if "FragmentSize" is reached.
        /// </summary>
        /// <param name="buffer">
        /// The buffer to read data from.
        /// </param>
        /// <param name="offset">
        /// The byte offset in buffer at which to begin writing from.
        /// </param>
        /// <param name="count">
        /// The maximum number of bytes to write.
        /// </param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            Dbg.Assert(_isEntered, "Stream should be Entered before writing into.");

            int offsetToReadFrom = offset;
            int amountLeft = count;

            while (amountLeft > 0)
            {
                int dataLeftInTheFragment = _fragmentSize - FragmentedRemoteObject.HeaderLength - _currentFragment.BlobLength;
                if (dataLeftInTheFragment > 0)
                {
                    int amountToWriteIntoFragment = (amountLeft > dataLeftInTheFragment) ? dataLeftInTheFragment : amountLeft;
                    amountLeft -= amountToWriteIntoFragment;

                    // Write data into fragment
                    Array.Copy(buffer, offsetToReadFrom, _currentFragment.Blob, _currentFragment.BlobLength, amountToWriteIntoFragment);
                    _currentFragment.BlobLength += amountToWriteIntoFragment;
                    offsetToReadFrom += amountToWriteIntoFragment;

                    // write only if amountLeft is more than 0. I dont write if amountLeft is 0 as we are not
                    // sure if the fragment is EndFragment..we will know this only in Exit.
                    if (amountLeft > 0)
                    {
                        WriteCurrentFragmentAndReset();
                    }
                }
                else
                {
                    WriteCurrentFragmentAndReset();
                }
            }
        }

        /// <summary>
        /// Writes a byte to the current stream.
        /// </summary>
        /// <param name="value"></param>
        public override void WriteByte(byte value)
        {
            Dbg.Assert(_isEntered, "Stream should be Entered before writing into.");
            byte[] buffer = new byte[1];
            buffer[0] = value;
            Write(buffer, 0, 1);
        }

        /// <summary>
        /// Returns a byte[] which holds data of fragment size (or) serialized data of
        /// one object, which ever is greater. If data is not currently available, then
        /// the callback is registered and called whenever the data is available.
        /// </summary>
        /// <param name="callback">
        /// callback to call once the data becomes available.
        /// </param>
        /// <returns>
        /// a byte[] holding data read from the stream
        /// </returns>
        internal byte[] ReadOrRegisterCallback(OnDataAvailableCallback callback)
        {
            lock (_syncObject)
            {
                if (_length <= 0)
                {
                    _onDataAvailableCallback = callback;
                    return null;
                }

                int bytesToRead = _length > _fragmentSize ? _fragmentSize : (int)_length;
                byte[] result = new byte[bytesToRead];
                Read(result, 0, bytesToRead);
                return result;
            }
        }

        /// <summary>
        /// Read the currently accumulated data in queued memory streams.
        /// </summary>
        /// <returns></returns>
        internal byte[] Read()
        {
            lock (_syncObject)
            {
                if (_isDisposed)
                {
                    return null;
                }

                int bytesToRead = _length > _fragmentSize ? _fragmentSize : (int)_length;
                if (bytesToRead > 0)
                {
                    byte[] result = new byte[bytesToRead];
                    Read(result, 0, bytesToRead);
                    return result;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int offSetToWriteTo = offset;
            int dataWritten = 0;
            Collection<MemoryStream> memoryStreamsToDispose = new Collection<MemoryStream>();
            MemoryStream prevReadStream = null;

            lock (_syncObject)
            {
                // technically this should throw an exception..but remoting callstack
                // is optimized ie., we are not locking in every layer (in powershell)
                // to save on performance..as a result there may be cases where
                // upper layer is trying to add stuff and stream is disposed while
                // adding stuff.
                if (_isDisposed)
                {
                    return 0;
                }

                while (dataWritten < count)
                {
                    if (_readStream == null)
                    {
                        if (_queuedStreams.Count > 0)
                        {
                            _readStream = _queuedStreams.Dequeue();
                            if ((!_readStream.CanRead) || (prevReadStream == _readStream))
                            {
                                // if the stream is disposed CanRead returns false
                                // this will happen if a Write enqueues the stream
                                // and a Read reads the data without dequeuing
                                _readStream = null;
                                continue;
                            }
                        }
                        else
                        {
                            _readStream = _writeStream;
                        }

                        Dbg.Assert(_readStream.Length > 0, "Not enough data to read.");
                        _readOffSet = 0;
                    }

                    _readStream.Position = _readOffSet;
                    int result = _readStream.Read(buffer, offSetToWriteTo, count - dataWritten);
                    s_trace.WriteLine("Read {0} data from readstream: {1}", result, _readStream.GetHashCode());
                    dataWritten += result;
                    offSetToWriteTo += result;
                    _readOffSet += result;
                    _length -= result;

                    // dispose only if we dont read from the current write stream.
                    if ((_readStream.Capacity == _readOffSet) && (_readStream != _writeStream))
                    {
                        s_trace.WriteLine("Adding readstream {0} to dispose collection.", _readStream.GetHashCode());
                        memoryStreamsToDispose.Add(_readStream);
                        prevReadStream = _readStream;
                        _readStream = null;
                    }
                }
            }

            // Dispose the memory streams outside of the lock
            foreach (MemoryStream streamToDispose in memoryStreamsToDispose)
            {
                s_trace.WriteLine("Disposing stream: {0}", streamToDispose.GetHashCode());
                streamToDispose.Dispose();
            }

            return dataWritten;
        }

        private void WriteCurrentFragmentAndReset()
        {
            // log trace of the fragment
            PSEtwLog.LogAnalyticVerbose(
                PSEventId.SentRemotingFragment, PSOpcode.Send, PSTask.None,
                PSKeyword.Transport | PSKeyword.UseAlwaysAnalytic,
                (Int64)(_currentFragment.ObjectId),
                (Int64)(_currentFragment.FragmentId),
                _currentFragment.IsStartFragment ? 1 : 0,
                _currentFragment.IsEndFragment ? 1 : 0,
                (UInt32)(_currentFragment.BlobLength),
                new PSETWBinaryBlob(_currentFragment.Blob, 0, _currentFragment.BlobLength));

            // finally write into memory stream
            byte[] data = _currentFragment.GetBytes();
            int amountLeft = data.Length;
            int offSetToReadFrom = 0;

            // user asked us to notify immediately..so no need
            // to write into memory stream..instead give the
            // data directly to user and let him figure out what to do.
            // This will save write + read + dispose!!
            if (!_notifyOnWriteFragmentImmediately)
            {
                lock (_syncObject)
                {
                    // technically this should throw an exception..but remoting callstack
                    // is optimized ie., we are not locking in every layer (in powershell)
                    // to save on performance..as a result there may be cases where
                    // upper layer is trying to add stuff and stream is disposed while
                    // adding stuff.
                    if (_isDisposed)
                    {
                        return;
                    }

                    if (_writeStream == null)
                    {
                        _writeStream = new MemoryStream(_fragmentSize);
                        s_trace.WriteLine("Created write stream: {0}", _writeStream.GetHashCode());
                        _writeOffset = 0;
                    }

                    while (amountLeft > 0)
                    {
                        int dataLeftInWriteStream = _writeStream.Capacity - _writeOffset;
                        if (dataLeftInWriteStream == 0)
                        {
                            // enqueue the current write stream and create a new one.
                            EnqueueWriteStream();
                            dataLeftInWriteStream = _writeStream.Capacity - _writeOffset;
                        }

                        int amountToWriteIntoStream = (amountLeft > dataLeftInWriteStream) ? dataLeftInWriteStream : amountLeft;
                        amountLeft -= amountToWriteIntoStream;
                        // write data
                        _writeStream.Position = _writeOffset;
                        _writeStream.Write(data, offSetToReadFrom, amountToWriteIntoStream);
                        offSetToReadFrom += amountToWriteIntoStream;
                        _writeOffset += amountToWriteIntoStream;
                        _length += amountToWriteIntoStream;
                    }
                }
            }

            // call the callback since we have data available
            _onDataAvailableCallback?.Invoke(data, _currentFragment.IsEndFragment);

            // prepare a new fragment
            _currentFragment.FragmentId = ++_fragmentId;
            _currentFragment.IsStartFragment = false;
            _currentFragment.IsEndFragment = false;
            _currentFragment.BlobLength = 0;
            _currentFragment.Blob = new byte[_fragmentSize];
        }

        private void EnqueueWriteStream()
        {
            s_trace.WriteLine("Queuing write stream: {0} Length: {1} Capacity: {2}",
                _writeStream.GetHashCode(), _writeStream.Length, _writeStream.Capacity);
            _queuedStreams.Enqueue(_writeStream);

            _writeStream = new MemoryStream(_fragmentSize);
            _writeOffset = 0;
            s_trace.WriteLine("Created write stream: {0}", _writeStream.GetHashCode());
        }
        /// <summary>
        /// This method provides a thread safe way to get an object id.
        /// </summary>
        /// <returns>
        /// An object Id in integer.
        /// </returns>
        private static long GetObjectId()
        {
            return System.Threading.Interlocked.Increment(ref s_objectIdSequenceNumber);
        }

        #endregion

        #region Disposable Overrides

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_syncObject)
                {
                    foreach (MemoryStream streamToDispose in _queuedStreams)
                    {
                        // make sure we dispose only once.
                        if (streamToDispose.CanRead)
                        {
                            streamToDispose.Dispose();
                        }
                    }

                    if ((_readStream != null) && (_readStream.CanRead))
                    {
                        _readStream.Dispose();
                    }

                    if ((_writeStream != null) && (_writeStream.CanRead))
                    {
                        _writeStream.Dispose();
                    }

                    _isDisposed = true;
                }
            }
        }

        #endregion

        #region Stream Overrides

        /// <summary>
        /// </summary>
        public override bool CanRead { get { return true; } }

        /// <summary>
        /// </summary>
        public override bool CanSeek { get { return false; } }

        /// <summary>
        /// </summary>
        public override bool CanWrite { get { return true; } }
        /// <summary>
        /// Gets the length of the stream in bytes.
        /// </summary>
        public override long Length { get { return _length; } }

        /// <summary>
        /// </summary>
        public override long Position
        {
            get { throw new NotSupportedException(); }

            set { throw new NotSupportedException(); }
        }
        /// <summary>
        /// This is a No-Op intentionally as there is nothing
        /// to flush.
        /// </summary>
        public override void Flush()
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region IDisposable Members

        private bool _disposed = false;

        public new void Dispose()
        {
            if (!_disposed)
            {
                GC.SuppressFinalize(this);
                _disposed = true;
            }

            base.Dispose();
        }

        #endregion
    }

    /// <summary>
    /// This class performs the fragmentation as well as defragmentation operations of large objects to be sent
    /// to the other side. A large remoting PSObject will be broken into fragments. Each fragment has a ObjectId
    /// and a FragmentId. The last fragment also has an end of fragment marker. These fragments can be reassembled
    /// on the receiving end by sequencing the fragment ids.
    /// </summary>
    internal class Fragmentor
    {
        #region Global Constants
        private static readonly UTF8Encoding s_utf8Encoding = new UTF8Encoding();
        // This const defines the default depth to be used for serializing objects for remoting.
        private const int SerializationDepthForRemoting = 1;
        #endregion

        private int _fragmentSize;
        private readonly SerializationContext _serializationContext;

        #region Constructor

        /// <summary>
        /// Constructor which initializes fragmentor with FragmentSize.
        /// </summary>
        /// <param name="fragmentSize">
        /// size of each fragment
        /// </param>
        /// <param name="cryptoHelper"></param>
        internal Fragmentor(int fragmentSize, PSRemotingCryptoHelper cryptoHelper)
        {
            Dbg.Assert(fragmentSize > 0, "fragment size cannot be less than 0.");
            _fragmentSize = fragmentSize;
            _serializationContext = new SerializationContext(
                SerializationDepthForRemoting,
                SerializationOptions.RemotingOptions,
                cryptoHelper);
            DeserializationContext = new DeserializationContext(
                DeserializationOptions.RemotingOptions,
                cryptoHelper);
        }

        #endregion

        /// <summary>
        /// The method performs the fragmentation operation.
        /// All fragments of the same object have the same ObjectId.
        /// All fragments of the same object have the same ObjectId.
        /// Each fragment has its own Fragment Id. Fragment Id always starts from zero (0),
        /// and increments sequentially with an increment of 1.
        /// The last fragment is indicated by an End of Fragment marker.
        /// </summary>
        /// <param name="obj">
        /// The object to be fragmented. Caller should make sure this is not null.
        /// </param>
        /// <param name="dataToBeSent">
        /// Caller specified dataToStore to which the fragments are added
        /// one-by-one
        /// </param>
        internal void Fragment<T>(RemoteDataObject<T> obj, SerializedDataStream dataToBeSent)
        {
            Dbg.Assert(obj != null, "Cannot fragment a null object");
            Dbg.Assert(dataToBeSent != null, "SendDataCollection cannot be null");

            dataToBeSent.Enter();
            try
            {
                obj.Serialize(dataToBeSent, this);
            }
            finally
            {
                dataToBeSent.Exit();
            }
        }

        /// <summary>
        /// The deserialization context used by this fragmentor. DeserializationContext
        /// controls the amount of memory a deserializer can use and other things.
        /// </summary>
        internal DeserializationContext DeserializationContext { get; }

        /// <summary>
        /// The size limit of the fragmented object.
        /// </summary>
        internal int FragmentSize
        {
            get
            {
                return _fragmentSize;
            }

            set
            {
                Dbg.Assert(value > 0, "FragmentSize cannot be less than 0.");
                _fragmentSize = value;
            }
        }

        /// <summary>
        /// TypeTable used for Serialization/Deserialization.
        /// </summary>
        internal TypeTable TypeTable { get; set; }

        /// <summary>
        /// Serialize an PSObject into a byte array.
        /// </summary>
        internal void SerializeToBytes(object obj, Stream streamToWriteTo)
        {
            Dbg.Assert(obj != null, "Cannot serialize a null object");
            Dbg.Assert(streamToWriteTo != null, "Stream to write to cannot be null");

            XmlWriterSettings xmlSettings = new XmlWriterSettings();
            xmlSettings.CheckCharacters = false;
            xmlSettings.Indent = false;
            // we dont want the underlying stream to be closed as we expect
            // the stream to be usable after this call.
            xmlSettings.CloseOutput = false;
            xmlSettings.Encoding = UTF8Encoding.UTF8;
            xmlSettings.NewLineHandling = NewLineHandling.None;

            xmlSettings.OmitXmlDeclaration = true;
            xmlSettings.ConformanceLevel = ConformanceLevel.Fragment;

            using (XmlWriter xmlWriter = XmlWriter.Create(streamToWriteTo, xmlSettings))
            {
                Serializer serializer = new Serializer(xmlWriter, _serializationContext);
                serializer.TypeTable = TypeTable;
                serializer.Serialize(obj);
                serializer.Done();
                xmlWriter.Flush();
            }

            return;
        }

        /// <summary>
        /// Converts the bytes back to PSObject.
        /// </summary>
        /// <param name="serializedDataStream">
        /// The bytes to be deserialized.
        /// </param>
        /// <returns>
        /// The deserialized object.
        /// </returns>
        /// <exception cref="PSRemotingDataStructureException">
        /// If the deserialized object is null.
        /// </exception>
        internal PSObject DeserializeToPSObject(Stream serializedDataStream)
        {
            Dbg.Assert(serializedDataStream != null, "Cannot Deserialize null data");
            Dbg.Assert(serializedDataStream.Length != 0, "Cannot Deserialize empty data");

            object result = null;
            using (XmlReader xmlReader = XmlReader.Create(serializedDataStream, InternalDeserializer.XmlReaderSettingsForCliXml))
            {
                Deserializer deserializer = new Deserializer(xmlReader, DeserializationContext);
                deserializer.TypeTable = TypeTable;
                result = deserializer.Deserialize();
                deserializer.Done();
            }

            if (result == null)
            {
                // cannot be null.
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.DeserializedObjectIsNull);
            }

            return PSObject.AsPSObject(result);
        }
    }
}
