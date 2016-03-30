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
using Microsoft.Management.Infrastructure.Generic;
using Microsoft.Management.Infrastructure.Internal;
using Microsoft.Management.Infrastructure.Internal.Data;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Management.Infrastructure
{

    /// <summary>
    /// Represents an CIM Class.
    /// </summary>
    public sealed class CimClass : IDisposable
    {
        private CimSystemProperties _systemProperties = null;
        private Native.ClassHandle _classHandle;
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
        internal Native.ClassHandle ClassHandle
        {
            get
            {
                this.AssertNotDisposed();
                return this._classHandle;
            }
        }

        #region Constructors

        internal CimClass(Native.ClassHandle handle)
        {
            Debug.Assert(handle != null, "Caller should verify that instanceHandle != null");
            handle.AssertValidInternalState();

            this._classHandle = handle;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Name of the Super CIM class 
        /// </summary>
        public string CimSuperClassName
        {
            get
            {
                this.AssertNotDisposed();

                string tmp;
                Native.MiResult result = Native.ClassMethods.GetParentClassName(this._classHandle, out tmp);
                switch (result)
                {
                    case Native.MiResult.INVALID_SUPERCLASS:
                        return null;

                    default:
                        CimException.ThrowIfMiResultFailure(result);
                        return tmp;
                }                
            }
        }        

        /// <summary>
        /// Super class schema
        /// </summary>
        public CimClass CimSuperClass
        {
            get
            {
                this.AssertNotDisposed();

                Native.ClassHandle tmp;
                Native.MiResult result = Native.ClassMethods.GetParentClass(this._classHandle, out tmp);
                switch (result)
                {
                    case Native.MiResult.INVALID_SUPERCLASS:
                        return null;

                    default:
                        CimException.ThrowIfMiResultFailure(result);
                        return new CimClass(tmp);
                }                
            }
        }        

        /// <summary>
        /// Properties of this CimClass
        /// </summary>
        public CimReadOnlyKeyedCollection<CimPropertyDeclaration> CimClassProperties
        {
            get
            {
                this.AssertNotDisposed();
                return new CimClassPropertiesCollection(this._classHandle);
            }
        }        

        /// <summary>
        /// Qualifiers of this CimClass
        /// </summary>
        public CimReadOnlyKeyedCollection<CimQualifier> CimClassQualifiers
        {
            get
            {
                this.AssertNotDisposed();
                return new CimClassQualifierCollection(this._classHandle);
            }
        }        

        /// <summary>
        /// Qualifiers of this CimClass
        /// </summary>
        public CimReadOnlyKeyedCollection<CimMethodDeclaration> CimClassMethods
        {
            get
            {
                this.AssertNotDisposed();
                return new CimMethodDeclarationCollection(this._classHandle);
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
                    Native.MiResult result = Native.ClassMethods.GetServerName(this._classHandle, out tmpComputerName);
                    CimException.ThrowIfMiResultFailure(result);

                    //ClassName
                    string tmpClassName;
                    result = Native.ClassMethods.GetClassName(this._classHandle, out tmpClassName);
                    CimException.ThrowIfMiResultFailure(result);

                    //Namespace 
                    string tmpNamespace;
                    result = Native.ClassMethods.GetNamespace(this._classHandle, out tmpNamespace);
                    CimException.ThrowIfMiResultFailure(result);
                    tmpSystemProperties.UpdateCimSystemProperties(tmpNamespace, tmpComputerName, tmpClassName);

                    //Path
                    tmpSystemProperties.UpdateSystemPath(CimInstance.GetCimSystemPath(tmpSystemProperties, null));                    
                    _systemProperties = tmpSystemProperties;
                }
                return _systemProperties;
            }
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
                this._classHandle.Dispose();
                this._classHandle = null;
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

        public override int GetHashCode()
        {
            return Native.ClassMethods.GetClassHashCode(this.ClassHandle);
        }

        public override bool Equals(object obj)
        {
            return object.ReferenceEquals(this, obj);
        }

        public override string ToString()
        {
            return string.Format(
                    CultureInfo.InvariantCulture,
                    Strings.CimClassToString,
                    this.CimSystemProperties.Namespace,
                    this.CimSystemProperties.ClassName);
        }
    }
}

namespace Microsoft.Management.Infrastructure.Internal
{
    internal static class ClassHandleExtensionMethods
    {
        public static Native.ClassHandle Clone(this Native.ClassHandle handleToClone)
        {
            if (handleToClone == null)
            {
                return null;
            }
            handleToClone.AssertValidInternalState();

            Native.ClassHandle clonedHandle;
            Native.MiResult result = Native.ClassMethods.Clone(handleToClone, out clonedHandle);
            CimException.ThrowIfMiResultFailure(result);
            return clonedHandle;
        }
    }
}
