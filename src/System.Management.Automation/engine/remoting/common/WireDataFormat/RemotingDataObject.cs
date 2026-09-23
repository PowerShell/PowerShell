// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// This is the object used by Runspace,pipeline,host to send data
    /// to remote end. Transport layer owns breaking this into fragments
    /// and sending to other end
    /// </summary>
    internal class RemoteDataObject<T>
    {
        #region Private Members

        private const int destinationOffset = 0;
        private const int dataTypeOffset = 4;
        private const int rsPoolIdOffset = 8;
        private const int psIdOffset = 24;
        private const int headerLength = 4 + 4 + 16 + 16;

        private const int SessionMask = 0x00010000;
        private const int RunspacePoolMask = 0x00021000;
        private const int PowerShellMask = 0x00041000;

        #endregion Private Members

        #region Constructors

        /// <summary>
        /// Constructs a RemoteDataObject from its
        /// individual components.
        /// </summary>
        /// <param name="destination">
        /// Destination this object is going to.
        /// </param>
        /// <param name="dataType">
        /// Payload type this object represents.
        /// </param>
        /// <param name="runspacePoolId">
        /// Runspace id this object belongs to.
        /// </param>
        /// <param name="powerShellId">
        /// PowerShell (pipeline) id this object belongs to.
        /// This may be null if the payload belongs to runspace.
        /// </param>
        /// <param name="data">
        /// Actual payload.
        /// </param>
        protected RemoteDataObject(RemotingDestination destination,
            RemotingDataType dataType,
            Guid runspacePoolId,
            Guid powerShellId,
            T data)
        {
            Destination = destination;
            DataType = dataType;
            RunspacePoolId = runspacePoolId;
            PowerShellId = powerShellId;
            Data = data;
        }

        #endregion Constructors

        #region Properties

        internal RemotingDestination Destination { get; }

        /// <summary>
        /// Gets the target (Runspace / Pipeline / Powershell / Host)
        /// the payload belongs to.
        /// </summary>
        internal RemotingTargetInterface TargetInterface
        {
            get
            {
                int dt = (int)DataType;

                // get the most used ones in the top.
                if ((dt & PowerShellMask) == PowerShellMask)
                {
                    return RemotingTargetInterface.PowerShell;
                }

                if ((dt & RunspacePoolMask) == RunspacePoolMask)
                {
                    return RemotingTargetInterface.RunspacePool;
                }

                if ((dt & SessionMask) == SessionMask)
                {
                    return RemotingTargetInterface.Session;
                }

                return RemotingTargetInterface.InvalidTargetInterface;
            }
        }

        internal RemotingDataType DataType { get; }

        internal Guid RunspacePoolId { get; }

        internal Guid PowerShellId { get; }

        internal T Data { get; }

        #endregion Properties

        /// <summary>
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="dataType"></param>
        /// <param name="runspacePoolId"></param>
        /// <param name="powerShellId"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static RemoteDataObject<T> CreateFrom(RemotingDestination destination,
            RemotingDataType dataType,
            Guid runspacePoolId,
            Guid powerShellId,
            T data)
        {
            return new RemoteDataObject<T>(destination, dataType, runspacePoolId, powerShellId, data);
        }

        /// <summary>
        /// Creates a RemoteDataObject by deserializing <paramref name="data"/>.
        /// </summary>
        /// <param name="serializedDataStream"></param>
        /// <param name="defragmentor">
        /// Defragmentor used to deserialize an object.
        /// </param>
        /// <returns></returns>
        internal static RemoteDataObject<T> CreateFrom(Stream serializedDataStream, Fragmentor defragmentor)
        {
            Dbg.Assert(serializedDataStream != null, "cannot construct a RemoteDataObject from null data");
            Dbg.Assert(defragmentor != null, "defragmentor cannot be null.");

            if ((serializedDataStream.Length - serializedDataStream.Position) < headerLength)
            {
                PSRemotingTransportException e =
                    new PSRemotingTransportException(PSRemotingErrorId.NotEnoughHeaderForRemoteDataObject,
                        RemotingErrorIdStrings.NotEnoughHeaderForRemoteDataObject,
                    headerLength + FragmentedRemoteObject.HeaderLength);
                throw e;
            }

            RemotingDestination destination = (RemotingDestination)DeserializeUInt(serializedDataStream);
            RemotingDataType dataType = (RemotingDataType)DeserializeUInt(serializedDataStream);
            Guid runspacePoolId = DeserializeGuid(serializedDataStream);
            Guid powerShellId = DeserializeGuid(serializedDataStream);

            object actualData = null;
            if ((serializedDataStream.Length - headerLength) > 0)
            {
                actualData = defragmentor.DeserializeToPSObject(serializedDataStream);
            }

            T deserializedObject = (T)LanguagePrimitives.ConvertTo(actualData, typeof(T),
                System.Globalization.CultureInfo.CurrentCulture);

            return new RemoteDataObject<T>(destination, dataType, runspacePoolId, powerShellId, deserializedObject);
        }

        #region Serialize / Deserialize

        /// <summary>
        /// Serializes the object into the stream specified. The serialization mechanism uses
        /// UTF8 encoding to encode data.
        /// </summary>
        /// <param name="streamToWriteTo"></param>
        /// <param name="fragmentor">
        /// fragmentor used to serialize and fragment the object.
        /// </param>
        internal virtual void Serialize(Stream streamToWriteTo, Fragmentor fragmentor)
        {
            Dbg.Assert(streamToWriteTo != null, "Stream to write to cannot be null.");
            Dbg.Assert(fragmentor != null, "Fragmentor cannot be null.");
            SerializeHeader(streamToWriteTo);

            if (Data != null)
            {
                fragmentor.SerializeToBytes(Data, streamToWriteTo);
            }

            return;
        }

        /// <summary>
        /// Serializes only the header portion of the object. ie., runspaceId,
        /// powerShellId, destination and dataType.
        /// </summary>
        /// <param name="streamToWriteTo">
        /// place where the serialized data is stored into.
        /// </param>
        /// <returns></returns>
        private void SerializeHeader(Stream streamToWriteTo)
        {
            Dbg.Assert(streamToWriteTo != null, "stream to write to cannot be null");

            // Serialize destination
            SerializeUInt((uint)Destination, streamToWriteTo);
            // Serialize data type
            SerializeUInt((uint)DataType, streamToWriteTo);
            // Serialize runspace guid
            SerializeGuid(RunspacePoolId, streamToWriteTo);
            // Serialize powershell guid
            SerializeGuid(PowerShellId, streamToWriteTo);

            return;
        }

        private static void SerializeUInt(uint data, Stream streamToWriteTo)
        {
            Dbg.Assert(streamToWriteTo != null, "stream to write to cannot be null");

            byte[] result = new byte[4]; // size of int

            int idx = 0;
            result[idx++] = (byte)(data & 0xFF);
            result[idx++] = (byte)((data >> 8) & 0xFF);
            result[idx++] = (byte)((data >> (2 * 8)) & 0xFF);
            result[idx++] = (byte)((data >> (3 * 8)) & 0xFF);

            streamToWriteTo.Write(result, 0, 4);
        }

        private static uint DeserializeUInt(Stream serializedDataStream)
        {
            Dbg.Assert(serializedDataStream.Length >= 4, "Not enough data to get Int.");

            uint result = 0;
            result |= (((uint)(serializedDataStream.ReadByte())) & 0xFF);
            result |= (((uint)(serializedDataStream.ReadByte() << 8)) & 0xFF00);
            result |= (((uint)(serializedDataStream.ReadByte() << (2 * 8))) & 0xFF0000);
            result |= (((uint)(serializedDataStream.ReadByte() << (3 * 8))) & 0xFF000000);

            return result;
        }

        private static void SerializeGuid(Guid guid, Stream streamToWriteTo)
        {
            Dbg.Assert(streamToWriteTo != null, "stream to write to cannot be null");

            byte[] guidArray = guid.ToByteArray();

            streamToWriteTo.Write(guidArray, 0, guidArray.Length);
        }

        private static Guid DeserializeGuid(Stream serializedDataStream)
        {
            Dbg.Assert(serializedDataStream.Length >= 16, "Not enough data to get Guid.");

            byte[] guidarray = new byte[16]; // Size of GUID.

            for (int idx = 0; idx < 16; idx++)
            {
                guidarray[idx] = (byte)serializedDataStream.ReadByte();
            }

            return new Guid(guidarray);
        }

        #endregion
    }

    internal sealed class RemoteDataObject : RemoteDataObject<object>
    {
        #region Constructors / Factory

        /// <summary>
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="dataType"></param>
        /// <param name="runspacePoolId"></param>
        /// <param name="powerShellId"></param>
        /// <param name="data"></param>
        private RemoteDataObject(RemotingDestination destination,
            RemotingDataType dataType,
            Guid runspacePoolId,
            Guid powerShellId,
            object data) : base(destination, dataType, runspacePoolId, powerShellId, data)
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="dataType"></param>
        /// <param name="runspacePoolId"></param>
        /// <param name="powerShellId"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static new RemoteDataObject CreateFrom(RemotingDestination destination,
            RemotingDataType dataType,
            Guid runspacePoolId,
            Guid powerShellId,
            object data)
        {
            return new RemoteDataObject(destination, dataType, runspacePoolId,
                powerShellId, data);
        }

        #endregion Constructors
    }
}
