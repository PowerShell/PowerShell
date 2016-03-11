/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Management.Infrastructure.Internal;

namespace Microsoft.Management.Infrastructure.Serialization
{
    /// <summary>
    /// Options for serialization of <see cref="CimInstance"/>
    /// </summary>
    [Flags]
    [SuppressMessage("Microsoft.Design", "CA1028:EnumStorageShouldBeInt32", Justification = "The native layer being wrapped used an unsigned integer")]
    public enum InstanceSerializationOptions : uint
    {
        None = 0,
        IncludeClasses = 1,
    }
    
    /// <summary>
    /// Options for serialization of <see cref="CimClass"/>
    /// </summary>
    [Flags]
    [SuppressMessage("Microsoft.Design", "CA1028:EnumStorageShouldBeInt32", Justification = "The native layer being wrapped used an unsigned integer")]
    public enum ClassSerializationOptions : uint
    {
        None = 0,
        IncludeParentClasses = 1,
    }
    
    /// <summary>
    ///  Represents an CIM serializer.
    /// </summary>
    public sealed class CimSerializer : IDisposable
    {
        private readonly Native.SerializerHandle _myHandle;

        #region Constructors

        private CimSerializer(string format, uint flags)
        {
            Debug.Assert(!string.IsNullOrEmpty(format), "Caller should verify that format != null");

            Native.SerializerHandle tmpHandle;
            Native.MiResult result = Native.ApplicationMethods.NewSerializer(CimApplication.Handle, format, flags, out tmpHandle);
            if (result == Native.MiResult.INVALID_PARAMETER)
            {
                throw new ArgumentOutOfRangeException("format");
            }
            CimException.ThrowIfMiResultFailure(result);
            this._myHandle = tmpHandle;
        }

        /// <summary>
        /// Construcutor that creates CimSerializer object with handle
        /// </summary>
        /// <param name="handle"></param>
        internal CimSerializer(Native.SerializerHandle handle)
        {
            Debug.Assert(handle != null, "Caller should verify that handle != null");
            this._myHandle = handle;
        }

        /// <summary>
        /// Instantiates a default serializer
        /// </summary>
        public static CimSerializer Create()
        {
            return new CimSerializer(format: "MI_XML", flags: 0);
        }

        /// <summary>
        /// Instantiates a custom serializer
        /// </summary>
        /// <param name="format">Serialization format.  Currently only "MI_XML" is supported.</param>
        /// <param name="flags">Serialization flags.  Has to be zero.</param>
        public static CimSerializer Create(string format, uint flags)
        {
            if (string.IsNullOrEmpty(format))
            {
                throw new ArgumentNullException("format");
            }

            return new CimSerializer(format, flags);
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Serializes the <paramref name="cimInstance"/> into the <paramref name="buffer"/>.
        /// </summary>
        /// <param name="cimInstance">Instance to serialize</param>
        /// <param name="options">Serialization options</param>
        /// <param name="buffer">
        /// Buffer for storing the serialized data.  
        /// This can be <c>null</c> (i.e. when calling this method only to read back the required buffer length via <paramref name="offset"/> parameter)
        /// </param>
        /// <param name="offset">
        /// Offset in the buffer, where data should be written.  
        /// After the method returns, the offset is increased by the amount taken by the serialized representation of the <paramref name="cimInstance"/></param>
        /// <returns>
        /// <c>true</c> if the serialized data fit into the <paramref name="buffer"/>;
        /// <c>false</c> otherwise (in this case <paramref name="offset"/> will be equal to the minimal required buffer length)
        /// </returns>
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId="3#", Justification = "Have to return 2 things.  Wrapping those 2 things in a class will result in a more, not less complexity")]
        public bool Serialize(CimInstance cimInstance, InstanceSerializationOptions options, byte[] buffer, ref uint offset)
        {
            if (cimInstance == null)
            {
                throw new ArgumentNullException("cimInstance");
            }
            if (buffer == null)
            {
                if (offset != 0)
                {
                    throw new ArgumentNullException("buffer");
                }
            }
            else
            {
                if (offset > buffer.Length) // offset == buffer.Length points outside of buffer, but is ok
                {
                    throw new ArgumentOutOfRangeException("offset");
                }
                else if (offset == buffer.Length)
                {
                    buffer = null;
                }
            }
            this.AssertNotDisposed();

            bool doesDataFitIntoTheBuffer = true;
            uint numberOfBytesUsed;
            Native.MiResult result = Native.SerializerMethods.SerializeInstance(
                this._myHandle,
                (UInt32)options,
                cimInstance.InstanceHandle, 
                buffer,
                offset, 
                out numberOfBytesUsed);
            switch (result)
            {
                case Native.MiResult.FAILED:
                    if ((buffer == null) || ((offset + numberOfBytesUsed) > buffer.Length))
                    {
                        result = Native.MiResult.OK;
                        offset += numberOfBytesUsed;
                    }
                    doesDataFitIntoTheBuffer = false;
                    return doesDataFitIntoTheBuffer;

                case Native.MiResult.OK:
                    offset += numberOfBytesUsed;
                    doesDataFitIntoTheBuffer = true;
                    return doesDataFitIntoTheBuffer;

                default:
                    CimException.ThrowIfMiResultFailure(result);
                    Debug.Assert(false, "Should throw in the previous statement");
                    return doesDataFitIntoTheBuffer;
            }
        }

        /// <summary>
        /// Serializes the <paramref name="cimClass"/> into the <paramref name="buffer"/>.
        /// </summary>
        /// <param name="cimClass">Class to serialize</param>
        /// <param name="options">Serialization options</param>
        /// <param name="buffer">
        /// Buffer for storing the serialized data.  
        /// This can be <c>null</c> (i.e. when calling this method only to read back the required buffer length via <paramref name="offset"/> parameter)
        /// </param>
        /// <param name="offset">
        /// Offset in the buffer, where data should be written.  
        /// After the method returns, the offset is increased by the amount taken by the serialized representation of the <paramref name="cimClass"/></param>
        /// <returns>
        /// <c>true</c> if the serialized data fit into the <paramref name="buffer"/>;
        /// <c>false</c> otherwise (in this case <paramref name="offset"/> will be equal to the minimal required buffer length)
        /// </returns>
        /// 
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId="3#", Justification = "Have to return 2 values.  Wrapping the values in a class makes things more, not less complex")]
        public bool Serialize(CimClass cimClass, ClassSerializationOptions options, byte[] buffer, ref uint offset)
        {
            if (cimClass == null)
            {
                throw new ArgumentNullException("cimClass");
            }
            if (buffer == null)
            {
                if (offset != 0)
                {
                    throw new ArgumentNullException("buffer");
                }
            }
            else
            {
                if (offset > buffer.Length) // offset == buffer.Length points outside of buffer, but is ok
                {
                    throw new ArgumentOutOfRangeException("offset");
                }
                else if (offset == buffer.Length)
                {
                    buffer = null;
                }
            }
            this.AssertNotDisposed();

            bool doesDataFitIntoTheBuffer = true;

            uint numberOfBytesUsed;
            Native.MiResult result = Native.SerializerMethods.SerializeClass(
                this._myHandle,
                (UInt32)options,
                cimClass.ClassHandle, 
                buffer,
                offset, 
                out numberOfBytesUsed);
            switch (result)
            {
                case Native.MiResult.FAILED:
                    if ((buffer == null) || ((offset + numberOfBytesUsed) > buffer.Length))
                    {
                        result = Native.MiResult.OK;
                        offset += numberOfBytesUsed;
                    }
                    doesDataFitIntoTheBuffer = false;
                    return doesDataFitIntoTheBuffer;

                case Native.MiResult.OK:
                    offset += numberOfBytesUsed;
                    doesDataFitIntoTheBuffer = true;
                    return doesDataFitIntoTheBuffer;

                default:
                    CimException.ThrowIfMiResultFailure(result);
                    Debug.Assert(false, "Should throw in the previous statement");
                    return doesDataFitIntoTheBuffer;
            }
        }

        /// <summary>
        /// Serializes the <paramref name="cimInstance"/>
        /// </summary>
        /// <param name="cimInstance">Instance to serialize</param>
        /// <param name="options">Serialization options</param>
        /// <returns>Serialized representation of <paramref name="cimInstance"/></returns>
        public byte[] Serialize(CimInstance cimInstance, InstanceSerializationOptions options)
        {
            if (cimInstance == null)
            {
                throw new ArgumentNullException("cimInstance");
            }
            this.AssertNotDisposed();

            bool dataFitsIntoBuffer;
            uint requiredBufferSize = 0;
            dataFitsIntoBuffer = Serialize(cimInstance, options, null, ref requiredBufferSize);
            Debug.Assert(!dataFitsIntoBuffer, "Passing null buffer - data cannot possibly fit into this buffer");

            byte[] buffer = new byte[requiredBufferSize];
            uint offset = 0;
            dataFitsIntoBuffer = Serialize(cimInstance, options, buffer, ref offset);
            Debug.Assert(dataFitsIntoBuffer, "Newly allocated buffer should fit (1)");
            Debug.Assert(buffer.Length == offset, "Newly allocated buffer should fit exactly (2)");

            return buffer;
        }

        /// <summary>
        /// Serializes the <paramref name="cimClass"/>
        /// </summary>
        /// <param name="cimClass">Class to serialize</param>
        /// <param name="options">Serialization options</param>
        /// <returns>Serialized representation of <paramref name="cimClass"/></returns>
        public byte[] Serialize(CimClass cimClass, ClassSerializationOptions options)
        {
            if (cimClass == null)
            {
                throw new ArgumentNullException("cimClass");
            }
            this.AssertNotDisposed();

            bool dataFitsIntoBuffer;
            uint requiredBufferSize = 0;
            dataFitsIntoBuffer = Serialize(cimClass, options, null, ref requiredBufferSize);
            Debug.Assert(!dataFitsIntoBuffer, "Passing null buffer - data cannot possibly fit into this buffer");

            byte[] buffer = new byte[requiredBufferSize];
            uint offset = 0;
            dataFitsIntoBuffer = Serialize(cimClass, options, buffer, ref offset);
            Debug.Assert(dataFitsIntoBuffer, "Newly allocated buffer should fit (1)");
            Debug.Assert(buffer.Length == offset, "Newly allocated buffer should fit exactly (2)");

            return buffer;
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Releases resources associated with this object
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases resources associated with this object
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                this._myHandle.Dispose();
            }

            _disposed = true;
        }

        internal void AssertNotDisposed()
        {
            if (this._disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        private bool _disposed;

        #endregion
    }
}