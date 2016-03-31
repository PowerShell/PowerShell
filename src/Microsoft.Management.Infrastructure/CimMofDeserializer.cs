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
using Microsoft.Management.Infrastructure.Options;

namespace Microsoft.Management.Infrastructure.Serialization
{
    /// <summary>
    /// Schema validation option
    /// </summary>
    /// <seealso cref="CimOperationOptions"/>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "This is a direct representation of the native flags (which doesn't have None)")]
    internal enum MofDeserializerSchemaValidationOption : int
    {
        /* MOF schema validation option's value */
        Default = 0,
        Strict = 1,
        Loose = 2,
        IgnorePropertyType = 3,
        Ignore =4
    };

    /// <summary>
    /// Represents an CIM deserializer.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId="Deserializer", Justification = "Deserializer is a valid word?")]
    internal class CimMofDeserializer : IDisposable
    {
        private readonly Native.DeserializerHandle _myHandle;

        #region Constructors

        private CimMofDeserializer(string format, uint flags)
        {
            Debug.Assert(!string.IsNullOrEmpty(format), "Caller should verify that format != null");

            Native.DeserializerHandle tmpHandle;
            Native.MiResult result = Native.ApplicationMethodsInternal.NewDeserializerMOF(
                CimApplication.Handle, format, flags, out tmpHandle);
            if (result == Native.MiResult.INVALID_PARAMETER)
            {
                throw new ArgumentOutOfRangeException("format");
            }
            CimException.ThrowIfMiResultFailure(result);
            this._myHandle = tmpHandle;
            this.SchemaValidationOption = MofDeserializerSchemaValidationOption.Default;
        }

        /// <summary>
        /// delegate to set the GetClass Callback.
        /// </summary>
        /// <value></value>
        /// <returns>null if failed to find the class</returns>
        public delegate CimClass OnClassNeeded(
            string serverName,
            string namespaceName,
            string className);

        /// <summary>
        /// Create an internal delegate on demand
        /// </summary>
        /// <param name="wrappedcallback"></param>
        /// <returns></returns>
        internal static Native.DeserializerCallbacks.ClassObjectNeededCallbackDelegate CreateClassObjectNeededCallbackDelegate(
            OnClassNeeded wrappedcallback)
        {
            return delegate(string serverName,
                string namespaceName,
                string className,
                out Native.ClassHandle classHandle)
            {
                CimClass cimClass = null;
                classHandle = null;
                cimClass = wrappedcallback(serverName, namespaceName, className);
                if (cimClass != null)
                {
                    classHandle = cimClass.ClassHandle;
                }
                return (classHandle != null);
            };
        }

        /// <summary>
        /// delegate to get and free included file content
        /// </summary>
        /// <value></value>
        /// <returns>true means sucess; false means failed to load included file</returns>
        public delegate byte[] GetIncludedFileContent(string fileName);

        /// <summary>
        /// Create an internal delegate on demand
        /// </summary>
        /// <param name="wrappedcallback"></param>
        /// <returns></returns>
        internal static Native.DeserializerCallbacks.GetIncludedFileBufferCallbackDelegate CreateGetIncludedFileBufferCallback(
            GetIncludedFileContent wrappedcallback)
        {
            return delegate(string fileName,
                out byte[] fileBuffer)
            {
                fileBuffer = wrappedcallback(fileName);
                return (fileBuffer != null);
            };
        }

        /// <summary>
        /// Schema validation option for deserializing instance(s)
        /// </summary>
        public MofDeserializerSchemaValidationOption SchemaValidationOption
        {
            get;
            set;
        }

        private CimOperationOptions GetOperationOptions()
        {
            CimOperationOptions options = new CimOperationOptions();
            switch (SchemaValidationOption)
            {
                case MofDeserializerSchemaValidationOption.Default:
                    options.SetOption("SchemaValidation", "Default");
                    break;
                case MofDeserializerSchemaValidationOption.Ignore:
                    options.SetOption("SchemaValidation", "IgnoreSchema");
                    break;
                case MofDeserializerSchemaValidationOption.IgnorePropertyType:
                    options.SetOption("SchemaValidation", "IgnorePropertyType");
                    break;
                case MofDeserializerSchemaValidationOption.Loose:
                    options.SetOption("SchemaValidation", "Loose");
                    break;
                case MofDeserializerSchemaValidationOption.Strict:
                    options.SetOption("SchemaValidation", "Strict");
                    break;
            }
            return options;
        }

        /// <summary>
        /// Instantiates a default deserializer
        /// </summary>
        public static CimMofDeserializer Create()
        {
            return new CimMofDeserializer(format: "MI_MOF_CIMV2_EXTV1", flags: 0);
        }

        /// <summary>
        /// Instantiates a custom deserializer
        /// </summary>
        /// <param name="format">Serialization format.  Currently only "MI_XML" is supported.</param>
        /// <param name="flags">Serialization flags.  Has to be zero.</param>
        public static CimMofDeserializer Create(string format, uint flags)
        {
            if (string.IsNullOrEmpty(format))
            {
                throw new ArgumentNullException("format");
            }

            return new CimMofDeserializer(format, flags);
        }

        #endregion Constructors

        #region Methods

        internal Native.InstanceHandle[] DeserializeInstanceHandle(
            byte[] serializedData,
            ref uint offset,
            IEnumerable<CimClass> cimClasses,
            OnClassNeeded onClassNeededCallback,
            GetIncludedFileContent getIncludedFileCallback)
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
            Native.InstanceHandle[] deserializedInstance;
            Native.InstanceHandle cimError;
            Native.OperationOptionsHandle nativeOption = GetOperationOptions().OperationOptionsHandle;
            Native.DeserializerCallbacks callbacks = new Native.DeserializerCallbacks();
            if (onClassNeededCallback != null) callbacks.ClassObjectNeededCallback = CreateClassObjectNeededCallbackDelegate(onClassNeededCallback);
            if (getIncludedFileCallback != null) callbacks.GetIncludedFileBufferCallback = CreateGetIncludedFileBufferCallback(getIncludedFileCallback);
            Native.MiResult result = Native.DeserializerInternalMethods.DeserializeInstanceArray(
                this._myHandle,
                nativeOption,
                callbacks,
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
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "1#", Justification = "Have to return 2 things.  Wrapping those 2 things in a class will result in a more, not less complexity")]
        public IEnumerable<CimInstance> DeserializeInstances(byte[] serializedData, ref uint offset)
        {
            return this.DeserializeInstances(serializedData, ref offset, null, null, null);
        }

        /// <summary>
        /// Deserializes a CIM instance with callbacks
        /// </summary>
        /// <param name="serializedData">buffer with the serialized data</param>
        /// <param name="offset">
        /// Offset where to start reading the data.
        /// When the method returns, the offset will be pointing to the next byte after the deserialied instance.
        /// </param>
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "1#", Justification = "Have to return 2 things.  Wrapping those 2 things in a class will result in a more, not less complexity")]
        public IEnumerable<CimInstance> DeserializeInstances(byte[] serializedData, ref uint offset, OnClassNeeded onClassNeededCallback,
            GetIncludedFileContent getIncludedFileCallback)
        {
            return this.DeserializeInstances(serializedData, ref offset, null, onClassNeededCallback, getIncludedFileCallback);
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
        public IEnumerable<CimInstance> DeserializeInstances(
            byte[] serializedData,
            ref uint offset,
            IEnumerable<CimClass> cimClasses,
            OnClassNeeded onClassNeededCallback,
            GetIncludedFileContent getIncludedFileCallback)
        {
            List<CimInstance> instancelist = new List<CimInstance>();
            Native.InstanceHandle[] instanceHandles = DeserializeInstanceHandle(serializedData, ref offset, cimClasses, onClassNeededCallback, getIncludedFileCallback);
            foreach (Native.InstanceHandle handle in instanceHandles)
            {
                instancelist.Add(new CimInstance(handle, parentHandle: null));
            }
            return instancelist;
        }

        internal Native.ClassHandle[] DeserializeClassHandle(
            byte[] serializedData,
            ref uint offset,
            IEnumerable<CimClass> cimClasses,
            string computerName,
            string namespaceName,
            OnClassNeeded onClassNeededCallback,
            GetIncludedFileContent getIncludedFileCallback)
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
            Native.ClassHandle[] deserializedClasses;
            Native.InstanceHandle cimError;
            Native.OperationOptionsHandle nativeOption = GetOperationOptions().OperationOptionsHandle;
            Native.DeserializerCallbacks callbacks = new Native.DeserializerCallbacks();
            if (onClassNeededCallback != null) callbacks.ClassObjectNeededCallback = CreateClassObjectNeededCallbackDelegate(onClassNeededCallback);
            if (getIncludedFileCallback != null) callbacks.GetIncludedFileBufferCallback = CreateGetIncludedFileBufferCallback(getIncludedFileCallback);
            Native.MiResult result = Native.DeserializerInternalMethods.DeserializeClassArray(
                this._myHandle,
                nativeOption,
                callbacks,
                serializedData,
                offset,
                nativeClassHandles,
                computerName,
                namespaceName,
                out deserializedClasses,
                out inputBufferUsed,
                out cimError);
            CimException.ThrowIfMiResultFailure(result, cimError);
            offset += inputBufferUsed;
            return deserializedClasses;
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
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "1#", Justification = "Have to return 2 things.  Wrapping those 2 things in a class will result in a more, not less complexity")]
        public IEnumerable<CimClass> DeserializeClasses(byte[] serializedData, ref uint offset)
        {
            return this.DeserializeClasses(serializedData, ref offset, null);
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
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "1#", Justification = "Have to return 2 things.  Wrapping those 2 things in a class will result in a more, not less complexity")]
        public IEnumerable<CimClass> DeserializeClasses(byte[] serializedData, ref uint offset, IEnumerable<CimClass> classes)
        {
            return DeserializeClasses(serializedData, ref offset, classes, null, null, null, null);
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
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "1#", Justification = "Have to return 2 things.  Wrapping those 2 things in a class will result in a more, not less complexity")]
        public IEnumerable<CimClass> DeserializeClasses(byte[] serializedData,
            ref uint offset,
            IEnumerable<CimClass> classes,
            OnClassNeeded onClassNeededCallback,
            GetIncludedFileContent getIncludedFileCallback)
        {
            return DeserializeClasses(serializedData, ref offset, classes, null, null, onClassNeededCallback, getIncludedFileCallback);
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
        public IEnumerable<CimClass> DeserializeClasses(byte[] serializedData,
            ref uint offset,
            IEnumerable<CimClass> classes,
            string computerName,
            string namespaceName,
            OnClassNeeded onClassNeededCallback,
            GetIncludedFileContent getIncludedFileCallback)
        {
            Native.ClassHandle[] classHandles = DeserializeClassHandle(
                serializedData,
                ref offset,
                classes,
                computerName,
                namespaceName,
                onClassNeededCallback,
                getIncludedFileCallback);
            List<CimClass> classlist = new List<CimClass>();
            foreach (Native.ClassHandle handle in classHandles)
            {
                classlist.Add(new CimClass(handle));
            }
            return classlist;
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