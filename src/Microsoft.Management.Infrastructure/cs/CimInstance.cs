/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.Management.Infrastructure.Generic;
using Microsoft.Management.Infrastructure.Internal;
using Microsoft.Management.Infrastructure.Internal.Data;
using Microsoft.Management.Infrastructure.Serialization;
using System.IO;
#if(!_CORECLR)
using Microsoft.Win32;
#endif

namespace Microsoft.Management.Infrastructure
{
    /// <summary>
    /// Represents an CIM instance.
    /// </summary>
#if(!_CORECLR)
    [Serializable]
#endif 
    public sealed class CimInstance : IDisposable
#if(!_CORECLR)
        //
        // Only implement these interfaces on FULL CLR and not Core CLR
        //
        , ICloneable, ISerializable
#endif
    {
        private readonly SharedInstanceHandle _myHandle;
        private CimSystemProperties _systemProperties = null;

        internal Native.InstanceHandle InstanceHandle
        {
            get
            {
                this.AssertNotDisposed();
                return this._myHandle.Handle;
            }
        }

        #region Constructors

        internal CimInstance(Native.InstanceHandle handle, SharedInstanceHandle parentHandle)
        {
            Debug.Assert(handle != null, "Caller should verify that instanceHandle != null");
            handle.AssertValidInternalState();
            this._myHandle = new SharedInstanceHandle(handle, parentHandle);
        }

        /// <summary>
        /// Instantiates a deep copy of <paramref name="cimInstanceToClone"/>
        /// </summary>
        /// <param name="cimInstanceToClone">Instance to clone</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="cimInstanceToClone"/> is <c>null</c></exception>
        public CimInstance(CimInstance cimInstanceToClone)
        {
            if (cimInstanceToClone == null)
            {
                throw new ArgumentNullException("cimInstanceToClone");
            }

            Native.InstanceHandle clonedHandle = cimInstanceToClone.InstanceHandle.Clone();
            this._myHandle = new SharedInstanceHandle(clonedHandle);
        }

        /// <summary>
        /// Instantiates an empty <see cref="CimInstance"/>.
        /// </summary>
        /// <remarks>
        /// This constructor provides a way to create CIM instances, without communicating with a CIM server.
        /// This constructor is typically used when the client knows all the key properties (<see cref="CimFlags.Key"/>)
        /// of the instance and wants to pass the instance as an argument of a CimSession method 
        /// (for example as a "sourceInstance" parameter of <see cref="CimSession.EnumerateAssociatedInstances(string, CimInstance, string, string, string, string)"/>).
        /// <see cref="CimSession.EnumerateInstances(string,string)"/> or <see cref="CimSession.GetInstance(string, CimInstance)"/>.
        /// </remarks>
        /// <param name="className"></param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="className"/> is null or when it doesn't follow the format specified by DSP0004</exception>
        public CimInstance(string className)
        :this(className, null)
        {
        }

        /// <summary>
        /// Instantiates an empty <see cref="CimInstance"/>.
        /// </summary>
        /// <remarks>
        /// This constructor provides a way to create CIM instances, without communicating with a CIM server.
        /// This constructor is typically used when the client knows all the key properties (<see cref="CimFlags.Key"/>)
        /// of the instance and wants to pass the instance as an argument of a CimSession method 
        /// (for example as a "sourceInstance" parameter of <see cref="CimSession.EnumerateAssociatedInstances(string, CimInstance, string, string, string, string)"/>).
        /// <see cref="CimSession.EnumerateInstances(string,string)"/> or <see cref="CimSession.GetInstance(string, CimInstance)"/>.
        /// </remarks>
        /// <param name="className"></param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="className"/> is null or when it doesn't follow the format specified by DSP0004</exception>
        public CimInstance(string className, string namespaceName)
        {
            if (className == null)
            {
                throw new ArgumentNullException("className");
            }

            Native.InstanceHandle tmpHandle;
            Native.MiResult result = Native.ApplicationMethods.NewInstance(CimApplication.Handle, className, null, out tmpHandle);
            switch (result)
            {
                case Native.MiResult.INVALID_PARAMETER:
                    throw new ArgumentOutOfRangeException("className");

                default:
                    CimException.ThrowIfMiResultFailure(result);
                    this._myHandle = new SharedInstanceHandle(tmpHandle);
                    break; 
            }
            if( namespaceName != null)
            {
                result = Native.InstanceMethods.SetNamespace(this._myHandle.Handle, namespaceName);
                CimException.ThrowIfMiResultFailure(result);
            }
        }        

        /// <summary>
        /// Instantiates an empty <see cref="CimInstance"/>.
        /// </summary>
        /// <param name="cimClass"></param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="cimClass"/> is null or when it doesn't follow the format specified by DSP0004</exception>
        public CimInstance(CimClass cimClass)
        {
            if (cimClass == null)
            {
                throw new ArgumentNullException("cimClass");
            }

            Native.InstanceHandle tmpHandle;
            Native.MiResult result = Native.ApplicationMethods.NewInstance(CimApplication.Handle, cimClass.CimSystemProperties.ClassName, cimClass.ClassHandle, out tmpHandle);
            if (result == Native.MiResult.INVALID_PARAMETER)
            {
                throw new ArgumentOutOfRangeException("cimClass");
            }
            CimException.ThrowIfMiResultFailure(result);
            this._myHandle = new SharedInstanceHandle(tmpHandle);

            result = Native.InstanceMethods.SetNamespace(this._myHandle.Handle, cimClass.CimSystemProperties.Namespace);
            CimException.ThrowIfMiResultFailure(result);
            result = Native.InstanceMethods.SetServerName(this._myHandle.Handle, cimClass.CimSystemProperties.ServerName);
            CimException.ThrowIfMiResultFailure(result);
        }        

        #endregion Constructors

        #region Properties
 
        public CimClass CimClass
        {
            get
            {
                this.AssertNotDisposed();

                Native.ClassHandle classHandle;
                Native.MiResult result = Native.InstanceMethods.GetClass(this.InstanceHandle, out classHandle);
                return ((classHandle == null) || (result != Native.MiResult.OK))
                           ? null
                           : new CimClass(classHandle);
            }
        }                  

        /// <summary>
        /// Properties of this CimInstance
        /// </summary>
        public CimKeyedCollection<CimProperty> CimInstanceProperties
        {
            get
            {
                this.AssertNotDisposed();
                return new CimPropertiesCollection(this._myHandle, this);
            }
        }        

        /// <summary>
        /// System Properties of this CimInstance
        /// </summary>
        public CimSystemProperties CimSystemProperties
        {
            get
            {
                this.AssertNotDisposed();
                if(_systemProperties == null) 
                {
                    CimSystemProperties tmpSystemProperties = new CimSystemProperties();
                    
                    // ComputerName
                    string tmpComputerName;
                    Native.MiResult result = Native.InstanceMethods.GetServerName(this.InstanceHandle, out tmpComputerName);
                    CimException.ThrowIfMiResultFailure(result);

                    //ClassName
                    string tmpClassName;
                    result = Native.InstanceMethods.GetClassName(this.InstanceHandle, out tmpClassName);
                    CimException.ThrowIfMiResultFailure(result);

                    //Namespace 
                    string tmpNamespace;
                    result = Native.InstanceMethods.GetNamespace(this.InstanceHandle, out tmpNamespace);
                    CimException.ThrowIfMiResultFailure(result);
                    tmpSystemProperties.UpdateCimSystemProperties(tmpNamespace, tmpComputerName, tmpClassName);

                    //Path
                    tmpSystemProperties.UpdateSystemPath(CimInstance.GetCimSystemPath(tmpSystemProperties, null)); 
                    _systemProperties = tmpSystemProperties;
                }
                return _systemProperties;
            }
        }        

        #endregion Properties

        #region Helpers

        /// <summary>
        /// Constructs the object path from the CimInstance.
        /// </summary>

        internal static string GetCimSystemPath(CimSystemProperties sysProperties, IEnumerator cimPropertiesEnumerator )
        {
            //Path should be supported by MI APIs, it is not currently supported by APIs
            // until that decision is taken we are reporting null for path.
            return null;
            /*
            string objectNamespace = sysProperties.Namespace;
            string objectComputerName = sysProperties.ServerName;
            string objectClassName = sysProperties.ClassName;
            StringBuilder strPath = new StringBuilder();
            if( objectComputerName != null )
            {
                strPath.Append("//");
                strPath.Append(objectComputerName);
                strPath.Append("/");
            }
            if( objectNamespace != null)
            {
                strPath.Append(objectNamespace);
                strPath.Append(":");
            }
            strPath.Append(objectClassName);
            //Now find the key properties
            IEnumerator cimProperties = cimPropertiesEnumerator;
            bool bFirst = true;
            while(cimProperties.MoveNext())
            {
                //Handle for both instance and class
                CimProperty instProp = cimProperties.Current as CimProperty;
                if( instProp != null)
                {
                    if(instProp.Value != null)
                    {
                        GetPathForProperty((long)instProp.Flags, instProp.Name, instProp.Value, ref bFirst, ref strPath);   
                    }
                }
                else
                {
                    CimPropertyDeclaration classProp = cimProperties.Current as CimPropertyDeclaration;
                    if( classProp == null)
                    {
                        // this is not expected, should we throw from here?
                        return null;
                    }
                    if( classProp.Value != null )
                    {
                        GetPathForProperty((long)classProp.Flags, classProp.Name, classProp.Value, ref bFirst, ref strPath); 
                    }
                }
            }
            return strPath.ToString();
            */

        }
/*
        private static void  GetPathForProperty(long cimflags, string propName, object propValue, ref bool bFirst, ref StringBuilder strPath)
        {
            long r = cimflags;
            long l = (long) CimFlags.Key;
            
            if( (r & l) != 0)
            {
                if(bFirst)
                {
                    bFirst = false;
                    strPath.Append(".");                          
                }
                else
                {
                    strPath.Append(",");
                }
                strPath.Append(propName);
                strPath.Append("=");
                strPath.Append("\"");
                CimInstance innerInst = propValue as CimInstance;
                string propValueStr; 
                if(innerInst == null )
                {
                    propValueStr = (propValue).ToString();
                }
                else
                {
                    propValueStr = GetCimSystemPath(innerInst.SystemProperties, innerInst.Properties.GetEnumerator());
                }
                strPath.Append(PutEscapeCharacterBack(propValueStr));
                strPath.Append("\"");   
            }
        }
        
        private static string PutEscapeCharacterBack(string propValue)
        {
            StringBuilder strPath = new StringBuilder();
            for(int i = 0 ;i < propValue.Length;i++)
            {
                if( propValue[i] == '\"' || propValue[i] == '\\')
                {
                    strPath.Append("\\");
                }
                strPath.Append(propValue[i]);
            }
            return strPath.ToString();
        }
        */
        
        

        //
        // TODO: THIS IS UNUSED METHOD. NEED TO REMOVE.
        //
#if(!_CORECLR)
        private static bool bRegistryRead = false;
        private static bool bNotSupportedAPIBehavior = false;
        private static string tempFileName = "NotSupportedAPIsCallstack.txt";
        private static readonly object _logThreadSafetyLock = new object();
        private static StreamWriter _streamWriter;

        internal static void NotSupportedAPIBehaviorLog(string propertyName)
        {
            if( bRegistryRead == false )
            {
                lock(_logThreadSafetyLock)
                {
                    if(bRegistryRead == false)
                    {
                        try
                        {
                            object obj = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Management Infrastructure","NotSupportedAPIBehavior",null);
                            bRegistryRead = true;
                            if( obj != null)
                            {
                                if( obj.ToString() == "1" )
                                {
                                    string tempPathDirectory = Path.GetTempPath();
                                    tempFileName = tempPathDirectory + "\\" + DateTime.Now.Ticks + "-NotSupportedAPIsCallstack.txt";
                                    _streamWriter = File.AppendText(tempFileName);
                                    _streamWriter.AutoFlush = true;
                                    bNotSupportedAPIBehavior = true;                                
                                }
                            }
                        }
                        catch(Exception)
                        {
                            bRegistryRead = true;
                        }
                    }
                }
            }
            if(bNotSupportedAPIBehavior)
            {
                _streamWriter.WriteLine(propertyName);

            }
        }
#endif
        internal static object ConvertToNativeLayer(object value, CimType cimType)
        {
            var cimInstance = value as CimInstance;
            if (cimInstance != null)
            {
                return cimInstance.InstanceHandle;
            }

            var arrayOfCimInstances = value as CimInstance[];
            if (arrayOfCimInstances != null)
            {
                Native.InstanceHandle[] arrayOfInstanceHandles = new Native.InstanceHandle[arrayOfCimInstances.Length];
                for (int i = 0; i < arrayOfCimInstances.Length; i++)
                {
                    CimInstance inst = arrayOfCimInstances[i];
                    if (inst == null)
                    {
                        arrayOfInstanceHandles[i] = null;
                    }
                    else
                    {
                        arrayOfInstanceHandles[i] = inst.InstanceHandle;
                    }
                }
                return arrayOfInstanceHandles;
            }

            return (cimType != CimType.Unknown) ? CimProperty.ConvertToNativeLayer(value, cimType) : value;
        }

        internal static object ConvertToNativeLayer(object value)
        {
            return ConvertToNativeLayer(value, CimType.Unknown);
        }

        internal static object ConvertFromNativeLayer(
            object value, 
            SharedInstanceHandle sharedParentHandle = null, 
            CimInstance parent = null,
            bool clone = false)
        {
            var instanceHandle = value as Native.InstanceHandle;
            if (instanceHandle != null)
            {
                CimInstance instance = new CimInstance(
                    clone ? instanceHandle.Clone() : instanceHandle, 
                    sharedParentHandle);
                if (parent != null)
                {
                    instance.SetCimSessionComputerName(parent.GetCimSessionComputerName());
                    instance.SetCimSessionInstanceId(parent.GetCimSessionInstanceId());
                }
                return instance;
            }

            var arrayOfInstanceHandles = value as Native.InstanceHandle[];
            if (arrayOfInstanceHandles != null)
            {
                CimInstance[] arrayOfInstances = new CimInstance[arrayOfInstanceHandles.Length];
                for (int i = 0; i < arrayOfInstanceHandles.Length; i++)
                {
                    Native.InstanceHandle h = arrayOfInstanceHandles[i];
                    if (h == null)
                    {
                        arrayOfInstances[i] = null;
                    }
                    else
                    {
                        arrayOfInstances[i] = new CimInstance(
                            clone ? h.Clone() : h,
                            sharedParentHandle);
                        if (parent != null)
                        {
                            arrayOfInstances[i].SetCimSessionComputerName(parent.GetCimSessionComputerName());
                            arrayOfInstances[i].SetCimSessionInstanceId(parent.GetCimSessionInstanceId());
                        }
                    }
                }
                return arrayOfInstances;
            }

            return value;
        }

        #endregion Helpers

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
                this._myHandle.Release();
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

        #region .NET serialization

        private const string serializationId_MiXml = "MI_XML";
        private const string serializationId_CimSessionComputerName = "CSCN";

#if(!_CORECLR)
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            using (CimSerializer cimSerializer = CimSerializer.Create())
            {
                byte[] serializedBytes = cimSerializer.Serialize(this, InstanceSerializationOptions.IncludeClasses);
                string serializedString = Encoding.Unicode.GetString(serializedBytes);
                info.AddValue(serializationId_MiXml, serializedString);                
            }
            info.AddValue(serializationId_CimSessionComputerName, this.GetCimSessionComputerName());
        }

        private CimInstance(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            string serializedString = info.GetString(serializationId_MiXml);     
            byte[] serializedBytes = Encoding.Unicode.GetBytes(serializedString);
            using (CimDeserializer cimDeserializer = CimDeserializer.Create())
            {
                uint offset = 0;
                Native.InstanceHandle deserializedInstanceHandle = cimDeserializer.DeserializeInstanceHandle(
                    serializedBytes,
                    ref offset,
                    cimClasses: null);
                this._myHandle = new SharedInstanceHandle(deserializedInstanceHandle);                
            }
            this.SetCimSessionComputerName(info.GetString(serializationId_CimSessionComputerName));
        }
#endif // !_CORECLR

        #endregion

        #region ICloneable Members

#if(!_CORECLR)
        object ICloneable.Clone()
        {
            return new CimInstance(this);
        }
#endif // !_CORECLR

        #endregion

        #region Utility functions
        /// <summary>
        /// get cimsession instance id
        /// </summary>
        /// <returns></returns>
        public Guid GetCimSessionInstanceId()
        {
            return this._CimSessionInstanceID;
        }
        /// <summary>
        /// set cimsession instance id
        /// </summary>
        /// <param name="instanceID"></param>
        internal void SetCimSessionInstanceId(Guid instanceID)
        {
            this._CimSessionInstanceID = instanceID;
        }
        /// <summary>
        /// cimsession id that generated the instance,
        /// Guid.Empty means no session generated this instance
        /// </summary>
        private Guid _CimSessionInstanceID = Guid.Empty;

        /// <summary>
        /// get the computername of the session
        /// </summary>
        /// <returns></returns>
        public String GetCimSessionComputerName()
        {
            return this._CimSessionComputerName;
        }
        /// <summary>
        /// set the computername of the session
        /// </summary>
        /// <param name="computerName"></param>
        internal void SetCimSessionComputerName(String computerName)
        {
            this._CimSessionComputerName = computerName;
        }
        /// <summary>
        /// computername of a cimsession, which genereated the instance,
        /// null means no session generated this instance
        /// </summary>
        private string _CimSessionComputerName = null;
        #endregion

        public override string ToString()
        {
            CimProperty captionProperty = this.CimInstanceProperties["Caption"];
            string captionValue = null;
            if (captionProperty != null)
            {
                captionValue = captionProperty.Value as string;
            }

            string keyValues = string.Join(", ", this.CimInstanceProperties.Where(p => CimFlags.Key == (p.Flags & CimFlags.Key)));

            string toStringValue;
            if (string.IsNullOrEmpty(keyValues) && (string.IsNullOrEmpty(captionValue)))
            {
                toStringValue = this.CimSystemProperties.ClassName;
            }
            else if (string.IsNullOrEmpty(captionValue))
            {
                toStringValue = string.Format(
                    CultureInfo.InvariantCulture,
                    Strings.CimInstanceToStringNoCaption,
                    this.CimSystemProperties.ClassName,
                    keyValues);
            }
            else if (string.IsNullOrEmpty(keyValues))
            {
                toStringValue = string.Format(
                    CultureInfo.InvariantCulture,
                    Strings.CimInstanceToStringNoKeys,
                    this.CimSystemProperties.ClassName,
                    captionValue);
            }
            else
            {
                toStringValue = string.Format(
                    CultureInfo.InvariantCulture,
                    Strings.CimInstanceToStringFullData,
                    this.CimSystemProperties.ClassName,
                    keyValues,
                    captionValue);
            }
            return toStringValue;
        }
    }
}

namespace Microsoft.Management.Infrastructure.Internal
{
    internal static class InstanceHandleExtensionMethods
    {
        public static Native.InstanceHandle Clone(this Native.InstanceHandle handleToClone)
        {
            if (handleToClone == null)
            {
                return null;
            }
            handleToClone.AssertValidInternalState();

            Native.InstanceHandle clonedHandle;
            Native.MiResult result = Native.InstanceMethods.Clone(handleToClone, out clonedHandle);
            CimException.ThrowIfMiResultFailure(result);
            return clonedHandle;
        }
    }
}
