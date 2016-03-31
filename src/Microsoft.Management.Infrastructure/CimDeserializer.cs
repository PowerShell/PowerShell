/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Management.Infrastructure.Internal;

namespace Microsoft.Management.Infrastructure.Serialization
{
    /// <summary>
    /// Represents an CIM deserializer.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId="Deserializer", Justification = "Deserializer is a valid word?")]
    public sealed class CimDeserializer : IDisposable
    {
        private readonly Native.DeserializerHandle _myHandle;

        #region Constructors

        private CimDeserializer(string format, uint flags)
        {
            Debug.Assert(!string.IsNullOrEmpty(format), "Caller should verify that format != null");

            Native.DeserializerHandle tmpHandle;
            Native.MiResult result = Native.ApplicationMethods.NewDeserializer(CimApplication.Handle, format, flags, out tmpHandle);
            if (result == Native.MiResult.INVALID_PARAMETER)
            {
                throw new ArgumentOutOfRangeException("format");
            }
            CimException.ThrowIfMiResultFailure(result);
            this._myHandle = tmpHandle;
        }

        /// <summary>
        /// Instantiates a default deserializer
        /// </summary>
        public static CimDeserializer Create()
        {
            return new CimDeserializer(format: "MI_XML", flags: 0);
        }

        /// <summary>
        /// Instantiates a custom deserializer
        /// </summary>
        /// <param name="format">Serialization format.  Currently only "MI_XML" is supported.</param>
        /// <param name="flags">Serialization flags.  Has to be zero.</param>
        public static CimDeserializer Create(string format, uint flags)
        {
            if (string.IsNullOrEmpty(format))
            {
                throw new ArgumentNullException("format");
            }

            return new CimDeserializer(format, flags);
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Deserializes a CIM instance
        /// </summary>
        /// <param name="serializedData">buffer with the serialized data</param>
        /// <param name="offset">
        /// Offset where to start reading the data.
        /// When the method returns, the offset will be pointing to the next byte after the deserialied instance.
        /// </param>
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId="1#", Justification = "Have to return 2 things.  Wrapping those 2 things in a class will result in a more, not less complexity")]
        public CimInstance DeserializeInstance(byte[] serializedData, ref uint offset)
        {
            return this.DeserializeInstance(serializedData, ref offset, null);
        }

        internal Native.InstanceHandle DeserializeInstanceHandle(byte[] serializedData, ref uint offset, IEnumerable<CimClass> cimClasses)
        {
            if (serializedData == null)
            {
                throw new ArgumentNullException("serializedData");
            }
            if (offset >= serializedData.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            this.AssertNotDisposed();

            Native.ClassHandle[] nativeClassHandles = null;
            if (cimClasses != null)
            {
                nativeClassHandles = cimClasses.Select(cimClass => cimClass.ClassHandle).ToArray();
            }

            UInt32 inputBufferUsed;
            Native.InstanceHandle deserializedInstance;
            Native.InstanceHandle cimError;
            Native.MiResult result = Native.DeserializerMethods.DeserializeInstance(
                this._myHandle,
                0, /* flags */
                serializedData,
                offset,
                nativeClassHandles,
                out deserializedInstance,
                out inputBufferUsed,
                out cimError);
            CimException.ThrowIfMiResultFailure(result, cimError);
            offset += inputBufferUsed;

            return deserializedInstance;
        }

        /// <summary>
        /// Deserializes a CIM instance
        /// </summary>
        /// <param name="serializedData">buffer with the serialized data</param>
        /// <param name="offset">
        /// Offset where to start reading the data.
        /// When the method returns, the offset will be pointing to the next byte after the deserialied instance.
        /// </param>
        /// <param name="cimClasses">Optional cache of classes... (I can't concisely explain what this does... maybe Paul can...)</param>
        /// <returns>Deserialized CIM instance</returns>
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId="1#", Justification = "Have to return 2 things.  Wrapping those 2 things in a class will result in a more, not less complexity")]
        public CimInstance DeserializeInstance(byte[] serializedData, ref uint offset, IEnumerable<CimClass> cimClasses)
        {
            Native.InstanceHandle instanceHandle = DeserializeInstanceHandle(serializedData, ref offset, cimClasses);
            return new CimInstance(instanceHandle, parentHandle: null);
        }

        /// <summary>
        /// Deserializes a CIM class
        /// </summary>
        /// <param name="serializedData">buffer with the serialized data</param>
        /// <param name="offset">
        /// Offset where to start reading the data.
        /// When the method returns, the offset will be pointing to the next byte after the deserialied class.
        /// </param>
        /// <returns>Deserialized CIM class</returns>
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId="1#", Justification = "Have to return 2 things.  Wrapping those 2 things in a class will result in a more, not less complexity")]
        public CimClass DeserializeClass(byte[] serializedData, ref uint offset)
        {
            return this.DeserializeClass(serializedData, ref offset, null);
        }

        internal Native.ClassHandle DeserializeClassHandle(byte[] serializedData, ref uint offset, CimClass parentClass, string computerName, string namespaceName)
        {
            if (serializedData == null)
            {
                throw new ArgumentNullException("serializedData");
            }
            if (offset >= serializedData.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            this.AssertNotDisposed();

            Native.ClassHandle nativeClassHandle = parentClass != null ? parentClass.ClassHandle : null;

            UInt32 inputBufferUsed;
            Native.ClassHandle deserializedClass;
            Native.InstanceHandle cimError;
            Native.MiResult result = Native.DeserializerMethods.DeserializeClass(
                this._myHandle,
                0, /* flags */
                serializedData,
                offset,
                nativeClassHandle,
                computerName,
                namespaceName,
                out deserializedClass,
                out inputBufferUsed,
                out cimError);
            CimException.ThrowIfMiResultFailure(result, cimError);
            offset += inputBufferUsed;

            return deserializedClass;
        }

        /// <summary>
        /// Deserializes a CIM class
        /// </summary>
        /// <param name="serializedData">buffer with the serialized data</param>
        /// <param name="offset">
        /// Offset where to start reading the data.
        /// When the method returns, the offset will be pointing to the next byte after the deserialied class.
        /// </param>
        /// <param name="parentClass">Optional parent class... (I can't concisely explain what this does and why this is needed... maybe Paul can...)</param>
        /// <returns>Deserialized CIM class</returns>
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "1#", Justification = "Have to return 2 things.  Wrapping those 2 things in a class will result in a more, not less complexity")]
        public CimClass DeserializeClass(byte[] serializedData, ref uint offset, CimClass parentClass)
        {
            return DeserializeClass(serializedData, ref offset, parentClass, null, null);
        }

        /// <summary>
        /// Deserializes a CIM class
        /// </summary>
        /// <param name="serializedData">buffer with the serialized data</param>
        /// <param name="offset">
        /// Offset where to start reading the data.
        /// When the method returns, the offset will be pointing to the next byte after the deserialied class.
        /// </param>
        /// <param name="parentClass">Optional parent class... (I can't concisely explain what this does and why this is needed... maybe Paul can...)</param>
        /// <returns>Deserialized CIM class</returns>
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId="1#", Justification = "Have to return 2 things.  Wrapping those 2 things in a class will result in a more, not less complexity")]
        public CimClass DeserializeClass(byte[] serializedData, ref uint offset, CimClass parentClass, string computerName, string namespaceName)
        {
            Native.ClassHandle classHandle = DeserializeClassHandle(serializedData, ref offset, parentClass, computerName, namespaceName);
            return new CimClass(classHandle);
        }

        /* Deserialize*ClassName is on the bubble 
          
        /// <summary>
        /// Deserializes a class name of a CIM instance
        /// </summary>
        /// <param name="serializedData">buffer with the serialized data</param>
        /// <param name="offset">Offset where to start reading the data.</param>
        /// <returns>Class name of a deserialized CIM instance</returns>
        public string DeserializeInstanceClassName(byte[] serializedData, uint offset)
        {
            if (serializedData == null)
            {
                throw new ArgumentNullException("serializedData");
            }
            if (offset >= serializedData.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            this.AssertNotDisposed();

            string className;
            Native.InstanceHandle cimError;
            Native.MiResult result = Native.DeserializerMethods.DeserializeInstance_GetClassName(
                this._myHandle,
                serializedData,
                offset,
                out className,
                out cimError);
            CimException.ThrowIfMiResultFailure(result, cimError);
            return className;
        }

        /// <summary>
        /// Deserializes a class name of a CIM class
        /// </summary>
        /// <param name="serializedData">buffer with the serialized data</param>
        /// <param name="offset">Offset where to start reading the data.</param>
        /// <returns>Class name of a deserialized CIM class</returns>
        public string DeserializeClassName(byte[] serializedData, uint offset)
        {
            if (serializedData == null)
            {
                throw new ArgumentNullException("serializedData");
            }
            if (offset >= serializedData.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            this.AssertNotDisposed();

            string className;
            Native.InstanceHandle cimError;
            Native.MiResult result = Native.DeserializerMethods.DeserializeClass_GetClassName(
                this._myHandle,
                serializedData,
                offset,
                out className,
                out cimError);
            CimException.ThrowIfMiResultFailure(result, cimError);
            return className;
        }

        /// <summary>
        /// Deserializes the name of the parent class of a CIM class
        /// </summary>
        /// <param name="serializedData">buffer with the serialized data</param>
        /// <param name="offset">Offset where to start reading the data.</param>
        /// <returns>Name of the parant class of a deserialized CIM class</returns>
        public string DeserializeParentClassName(byte[] serializedData, uint offset)
        {
            if (serializedData == null)
            {
                throw new ArgumentNullException("serializedData");
            }
            if (offset >= serializedData.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            this.AssertNotDisposed();

            string parentClassName;
            Native.InstanceHandle cimError;
            Native.MiResult result = Native.DeserializerMethods.DeserializeClass_GetParentClassName(
                this._myHandle,
                serializedData,
                offset,
                out parentClassName,
                out cimError);
            CimException.ThrowIfMiResultFailure(result, cimError);
            return parentClassName;
        }
         
         */

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