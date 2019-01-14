// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#region Using directives

using System.Threading;
using Microsoft.Management.Infrastructure.Options;
using System;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    /// <summary>
    /// Base action class, implemented to write results to pipeline.
    /// </summary>
    internal abstract class CimBaseAction
    {
        /// <summary>
        /// Constructor method.
        /// </summary>
        public CimBaseAction()
        {
        }

        /// <summary>
        /// <para>
        /// Execute the write operation to given cmdlet object
        /// </para>
        /// </summary>
        /// <param name="cmdlet">
        /// cmdlet wrapper object, to which write result.
        /// <see cref="CmdletOperationBase"/> for details.
        /// </param>
        public virtual void Execute(CmdletOperationBase cmdlet)
        {
        }

        /// <summary>
        /// <para>
        /// <see cref="XOperationContextBase"/> object that related to current action.
        /// It may used by action, such as <see cref="CimWriteResultObject"/>,
        /// since later on action may require namespace, and proxy object to reuse
        /// <see cref="CimSession"/>, <see cref="CimOperationOptions"/> object.
        /// </para>
        /// </summary>
        protected XOperationContextBase Context
        {
            get
            {
                return this.context;
            }

            set
            {
                this.context = value;
            }
        }

        private XOperationContextBase context;
    }

    /// <summary>
    /// <para>
    /// Synchronous action class, implemented to write results to pipeline
    /// and block current thread until the action is completed.
    /// </para>
    /// </summary>
    internal class CimSyncAction : CimBaseAction, IDisposable
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public CimSyncAction()
        {
            this.completeEvent = new ManualResetEventSlim(false);
            this.responseType = CimResponseType.None;
        }

        /// <summary>
        /// <para>
        /// Block current thread until action completed
        /// </para>
        /// </summary>
        /// <returns>Response from user.</returns>
        public virtual CimResponseType GetResponse()
        {
            this.Block();
            return responseType;
        }

        /// <summary>
        /// <para>
        /// Set response result
        /// </para>
        /// </summary>
        internal CimResponseType ResponseType
        {
            set { this.responseType = value; }
        }

        /// <summary>
        /// <para>
        /// Call this method when the action is completed or
        /// the operation is terminated
        /// </para>
        /// </summary>
        internal virtual void OnComplete()
        {
            this.completeEvent.Set();
        }

        /// <summary>
        /// <para>
        /// block current thread.
        /// </para>
        /// </summary>
        protected virtual void Block()
        {
            this.completeEvent.Wait();
            this.completeEvent.Dispose();
        }

        #region members

        /// <summary>
        /// Action completed event.
        /// </summary>
        private ManualResetEventSlim completeEvent;

        /// <summary>
        /// Response result.
        /// </summary>
        protected CimResponseType responseType;

        #endregion

        #region IDisposable interface
        /// <summary>
        /// IDisposable interface.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// <para>
        /// Dispose() calls Dispose(true).
        /// Implement IDisposable. Do not make this method virtual.
        /// A derived class should not be able to override this method.
        /// </para>
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SuppressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// <para>
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// If disposing equals true, the method has been called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        /// If disposing equals false, the method has been called by the
        /// runtime from inside the finalizer and you should not reference
        /// other objects. Only unmanaged resources can be disposed.
        /// </para>
        /// </summary>
        /// <param name="disposing">Whether it is directly called.</param>
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this._disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    if (this.completeEvent != null)
                    {
                        this.completeEvent.Dispose();
                    }
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.

                // Note disposing has been done.
                _disposed = true;
            }
        }
        #endregion
    }
}
