/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Diagnostics;

namespace Microsoft.Management.Infrastructure
{
    /// <summary>
    /// Represents a SubscriptionResult
    /// </summary>
    public class CimSubscriptionResult : IDisposable
    {
        private CimInstance _resultInstance;
        private readonly string _bookmark;
        private readonly string _machineId;

        internal CimSubscriptionResult(Native.InstanceHandle handle, string bookmark, string machineId)
        {
            Debug.Assert(handle != null, "Caller should verify backingInstance != null");
            this._resultInstance = new CimInstance(handle, null);
            this._bookmark = bookmark;
            this._machineId = machineId;
        }

        public string Bookmark
        {
            get
            {
                this.AssertNotDisposed();
                return this._bookmark;
            }
        }

        public string MachineId
        {
            get
            {
                this.AssertNotDisposed();
                return this._machineId;
            }
        }

        public CimInstance Instance
        {
            get
            {
                this.AssertNotDisposed();
                return this._resultInstance;
            }
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
                this._resultInstance.Dispose();
                this._resultInstance = null;
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