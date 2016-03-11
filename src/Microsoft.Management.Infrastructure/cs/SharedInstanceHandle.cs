/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Diagnostics;

namespace Microsoft.Management.Infrastructure.Internal
{
    internal class SharedInstanceHandle
    {
        private readonly Native.InstanceHandle _handle;
        private readonly SharedInstanceHandle _parent;
        private readonly object _numberOfReferencesLock = new object();
        private int _numberOfReferences = 1;

        internal SharedInstanceHandle(Native.InstanceHandle handle)
        {
            Debug.Assert(handle != null, "Caller should verify that handle != null");
            handle.AssertValidInternalState();
            this._handle = handle;
        }

        internal SharedInstanceHandle(Native.InstanceHandle handle, SharedInstanceHandle parent)
            : this(handle)
        {
            this._parent = parent;
            if (this._parent != null)
            {
                this._parent.AddRef();
            }
        }

        internal Native.InstanceHandle Handle
        {
            get
            {
                lock (this._numberOfReferencesLock)
                {
                    if (this._numberOfReferences == 0)
                    {
                        throw new ObjectDisposedException(this.ToString());
                    }
                }
                return this._handle;
            }
        }

        internal void AddRef()
        {
            lock (this._numberOfReferencesLock)
            {
                if (this._numberOfReferences == 0)
                {
                    throw new ObjectDisposedException(this.ToString());
                }
                this._numberOfReferences++;
            }
        }

        internal void Release()
        {
            lock (this._numberOfReferencesLock)
            {
                this._numberOfReferences--;
                Debug.Assert(this._numberOfReferences >= 0, "SharedInstanceHandle should preserve the invariant that _numberOfReferences>=0");
                if (this._numberOfReferences == 0)
                {
                    this._handle.Dispose();
                    if (this._parent != null)
                    {
                        this._parent.Release();
                    }
                }
            }
        }
    }
}