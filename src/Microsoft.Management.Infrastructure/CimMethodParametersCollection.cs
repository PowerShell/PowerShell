/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Management.Infrastructure.Generic;
using Microsoft.Management.Infrastructure.Internal.Data;

namespace Microsoft.Management.Infrastructure
{
    /// <summary>
    /// A collection of method parameters
    /// </summary>
    public class CimMethodParametersCollection : CimKeyedCollection<CimMethodParameter>, IDisposable
    {
        private CimInstance _backingInstance;

        internal Native.InstanceHandle InstanceHandleForMethodInvocation
        {
            get
            {
                this.AssertNotDisposed();
                if (this._backingInstance.CimInstanceProperties.Count == 0)
                {
                    return null;
                }
                return this._backingInstance.InstanceHandle;
            }
        }

        /// <summary>
        /// Creates a new collection of method parameters
        /// </summary>
        public CimMethodParametersCollection()
        {
            this._backingInstance = new CimInstance(this.GetType().Name);
        }

        internal CimMethodParametersCollection(CimInstance backingInstance)
        {
            Debug.Assert(backingInstance != null, "Caller should verify backingInstance != null");
            this._backingInstance = backingInstance;
        }

        /// <summary>
        /// Adds a new parameter to the collection
        /// </summary>
        /// <param name="newParameter"></param>
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId="0#", Justification = "newParameter is more specific than newItem")]
        public override void Add(CimMethodParameter newParameter)
        {
            this.AssertNotDisposed();
            if (newParameter == null)
            {
                throw new ArgumentNullException("newParameter");
            }

            CimProperty backingProperty = CimProperty.Create(newParameter.Name, newParameter.Value, newParameter.CimType, newParameter.Flags);
            this._backingInstance.CimInstanceProperties.Add(backingProperty);
        }

        /// <summary>
        /// Number of parameters in the collection
        /// </summary>
        public override int Count
        {
            get
            {
                this.AssertNotDisposed();
                return this._backingInstance.CimInstanceProperties.Count;
            }
        }

        /// <summary>
        /// Gets a parameter with a given <paramref name="parameterName"/>
        /// </summary>
        /// <param name="parameterName"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId="0#", Justification = "parameterName is more specific than itemName")]
        public override CimMethodParameter this[string parameterName]
        {
            get
            {
                this.AssertNotDisposed();
                if (string.IsNullOrWhiteSpace(parameterName))
                {
                    throw new ArgumentNullException("parameterName");
                }

                CimProperty backingProperty = this._backingInstance.CimInstanceProperties[parameterName];
                return (backingProperty == null)
                    ? null
                    : new CimMethodParameterBackedByCimProperty(backingProperty, this._backingInstance.GetCimSessionComputerName(), this._backingInstance.GetCimSessionInstanceId());
            }
        }

        public override IEnumerator<CimMethodParameter> GetEnumerator()
        {
            this.AssertNotDisposed();
            return this._backingInstance
                .CimInstanceProperties
                .Select(p => new CimMethodParameterBackedByCimProperty(p, this._backingInstance.GetCimSessionComputerName(), this._backingInstance.GetCimSessionInstanceId()))
                .GetEnumerator();
        }

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
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                this._backingInstance.Dispose();
                this._backingInstance = null;
            }

            _disposed = true;
        }

        internal void AssertNotDisposed()
        {
            if (this._disposed)
            {
                throw new ObjectDisposedException(this.ToString());
            }
        }

        private bool _disposed;

        #endregion
    }
}