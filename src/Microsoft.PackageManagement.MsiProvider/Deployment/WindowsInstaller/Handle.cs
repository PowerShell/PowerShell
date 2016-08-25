//---------------------------------------------------------------------
// <copyright file="Handle.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Base class for Windows Installer handle types (Database, View, Record, SummaryInfo).
    /// </summary>
    /// <remarks><p>
    /// These classes implement the <see cref="IDisposable"/> interface, because they
    /// hold unmanaged resources (MSI handles) that should be properly disposed
    /// when no longer needed.
    /// </p></remarks>
    public abstract class InstallerHandle : MarshalByRefObject, IDisposable
    {
        private NativeMethods.MsiHandle handle;

        /// <summary>
        /// Constructs a handle object from a native integer handle.
        /// </summary>
        /// <param name="handle">Native integer handle.</param>
        /// <param name="ownsHandle">true to close the handle when this object is disposed or finalized</param>
        protected InstallerHandle(IntPtr handle, bool ownsHandle)
        {
            if (handle == IntPtr.Zero)
            {
                throw new InvalidHandleException();
            }

            this.handle = new NativeMethods.MsiHandle(handle, ownsHandle);
        }

        /// <summary>
        /// Gets the native integer handle.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public IntPtr Handle
        {
            get
            {
                if (this.IsClosed)
                {
                    throw new InvalidHandleException();
                }
                return this.handle;
            }
        }

        /// <summary>
        /// Checks if the handle is closed. When closed, method calls on the handle object may throw an <see cref="InvalidHandleException"/>.
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        public bool IsClosed
        {
            get
            {
                return this.handle.IsClosed;
            }
        }

        /// <summary>
        /// Closes the handle.  After closing a handle, further method calls may throw an <see cref="InvalidHandleException"/>.
        /// </summary>
        /// <remarks><p>
        /// The finalizer of this class will NOT close the handle if it is still open,
        /// because finalization can run on a separate thread from the application,
        /// resulting in potential problems if handles are closed from that thread.
        /// It is best that the handle be closed manually as soon as it is no longer needed,
        /// as leaving lots of unused handles open can degrade performance.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiclosehandle.asp">MsiCloseHandle</a>
        /// </p></remarks>
        /// <seealso cref="Close"/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Closes the handle.  After closing a handle, further method calls may throw an <see cref="InvalidHandleException"/>.
        /// </summary>
        /// <remarks><p>
        /// The finalizer of this class will NOT close the handle if it is still open,
        /// because finalization can run on a separate thread from the application,
        /// resulting in potential problems if handles are closed from that thread.
        /// It is best that the handle be closed manually as soon as it is no longer needed,
        /// as leaving lots of unused handles open can degrade performance.
        /// </p><p>
        /// This method is merely an alias for the <see cref="Dispose()"/> method.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiclosehandle.asp">MsiCloseHandle</a>
        /// </p></remarks>
        public void Close()
        {
            this.Dispose();
        }

        /// <summary>
        /// Tests whether this handle object is equal to another handle object.  Two handle objects are equal
        /// if their types are the same and their native integer handles are the same.
        /// </summary>
        /// <param name="obj">The handle object to compare with the current handle object.</param>
        /// <returns>true if the specified handle object is equal to the current handle object; otherwise false</returns>
        public override bool Equals(object obj)
        {
            return (obj != null && this.GetType() == obj.GetType() &&
                this.Handle == ((InstallerHandle) obj).Handle);
        }

        /// <summary>
        /// Gets a hash value for the handle object.
        /// </summary>
        /// <returns>A hash code for the handle object.</returns>
        /// <remarks><p>
        /// The hash code is derived from the native integer handle.
        /// </p></remarks>
        public override int GetHashCode()
        {
            return this.Handle.GetHashCode();
        }

        /// <summary>
        /// Gets an object that can be used internally for safe synchronization.
        /// </summary>
        internal object Sync
        {
            get
            {
                return this.handle;
            }
        }

        /// <summary>
        /// Closes the handle.  After closing a handle, further method calls may throw an <see cref="InvalidHandleException"/>.
        /// </summary>
        /// <param name="disposing">If true, the method has been called directly or indirectly by a user's code,
        /// so managed and unmanaged resources will be disposed. If false, the method has been called by the
        /// runtime from inside the finalizer, and only unmanaged resources will be disposed.</param>
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.handle.Dispose();
            }
        }
    }
}
