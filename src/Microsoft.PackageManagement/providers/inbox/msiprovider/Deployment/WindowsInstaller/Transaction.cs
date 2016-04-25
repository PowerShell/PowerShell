//---------------------------------------------------------------------
// <copyright file="Transaction.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;

    /// <summary>
    /// [MSI 4.5] Handle to a multi-session install transaction.
    /// </summary>
    /// <remarks><p>
    /// Win32 MSI APIs:
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msibegintransaction.asp">MsiBeginTransaction</a>
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msijointransaction.asp">MsiJoinTransaction</a>
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiendtransaction.asp">MsiEndTransaction</a>
    /// </p></remarks>
    internal class Transaction : InstallerHandle
    {
        private string name;
        private IntPtr ownerChangeEvent;
        private IList<EventHandler<EventArgs>> ownerChangeListeners;

        /// <summary>
        /// [MSI 4.5] Begins transaction processing of a multi-package installation.
        /// </summary>
        /// <param name="name">Name of the multi-package installation.</param>
        /// <param name="attributes">Select optional behavior when beginning the transaction.</param>
        /// <exception cref="InstallerException">The transaction could not be initialized.</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msibegintransaction.asp">MsiBeginTransaction</a>
        /// </p></remarks>
        public Transaction(string name, TransactionAttributes attributes)
            : this(name, Transaction.Begin(name, attributes), true)
        {
        }

        /// <summary>
        /// Internal constructor.
        /// </summary>
        /// <remarks>
        /// The second parameter is an array in order to receive multiple values from the initialization method.
        /// </remarks>
        private Transaction(string name, IntPtr[] handles, bool ownsHandle)
            : base(handles[0], ownsHandle)
        {
            this.name = name;
            this.ownerChangeEvent = handles[1];
            this.ownerChangeListeners = new List<EventHandler<EventArgs>>();
        }

        /// <summary>
        /// Creates a new Transaction object from an integer handle.
        /// </summary>
        /// <param name="handle">Integer transaction handle</param>
        /// <param name="ownsHandle">true to close the handle when this object is disposed</param>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static Transaction FromHandle(IntPtr handle, bool ownsHandle)
        {
            return new Transaction(handle.ToString(), new IntPtr[] { handle, IntPtr.Zero }, ownsHandle);
        }

        /// <summary>
        /// Gets the name of the transaction.
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }
        }

        /// <summary>
        /// Notifies listeners when the process that owns the transaction changed.
        /// </summary>
        public event EventHandler<EventArgs> OwnerChanged
        {
            add
            {
                this.ownerChangeListeners.Add(value);

                if (this.ownerChangeEvent != IntPtr.Zero && this.ownerChangeListeners.Count == 1)
                {
                    new Thread(this.WaitForOwnerChange).Start();
                }
            }
            remove
            {
                this.ownerChangeListeners.Remove(value);
            }
        }

        private void OnOwnerChanged()
        {
            EventArgs e = new EventArgs();
            foreach (EventHandler<EventArgs> handler in this.ownerChangeListeners)
            {
                handler(this, e);
            }
        }

        private void WaitForOwnerChange()
        {
            int ret = NativeMethods.WaitForSingleObject(this.ownerChangeEvent, -1);
            if (ret == 0)
            {
                this.OnOwnerChanged();
            }
            else
            {
                throw new InstallerException();
            }
        }

        /// <summary>
        /// Makes the current process the owner of the multi-package installation transaction.
        /// </summary>
        /// <param name="attributes">Select optional behavior when joining the transaction.</param>
        /// <exception cref="InvalidHandleException">The transaction handle is not valid.</exception>
        /// <exception cref="InstallerException">The transaction could not be joined.</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msijointransaction.asp">MsiJoinTransaction</a>
        /// </p></remarks>
        public void Join(TransactionAttributes attributes)
        {
            IntPtr hChangeOfOwnerEvent;
            uint ret = NativeMethods.MsiJoinTransaction((int) this.Handle, (int) attributes, out hChangeOfOwnerEvent);
            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }

            this.ownerChangeEvent = hChangeOfOwnerEvent;
            if (this.ownerChangeEvent != IntPtr.Zero && this.ownerChangeListeners.Count >= 1)
            {
                new Thread(this.WaitForOwnerChange).Start();
            }
        }

        /// <summary>
        /// Ends the install transaction and commits all changes to the system belonging to the transaction.
        /// </summary>
        /// <exception cref="InstallerException">The transaction could not be committed.</exception>
        /// <remarks><p>
        /// Runs any Commit Custom Actions and commits to the system any changes to Win32 or common language
        /// runtime assemblies. Deletes the rollback script, and after using this option, the transaction's
        /// changes can no longer be undone with a Rollback Installation.
        /// </p><p>
        /// This method can only be called by the current owner of the transaction.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiendtransaction.asp">MsiEndTransaction</a>
        /// </p></remarks>
        public void Commit()
        {
            this.End(true);
        }

        /// <summary>
        /// Ends the install transaction and undoes changes to the system belonging to the transaction.
        /// </summary>
        /// <exception cref="InstallerException">The transaction could not be rolled back.</exception>
        /// <remarks><p>
        /// This method can only be called by the current owner of the transaction.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiendtransaction.asp">MsiEndTransaction</a>
        /// </p></remarks>
        public void Rollback()
        {
            this.End(false);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static IntPtr[] Begin(string transactionName, TransactionAttributes attributes)
        {
            int hTransaction;
            IntPtr hChangeOfOwnerEvent;
            uint ret = NativeMethods.MsiBeginTransaction(transactionName, (int) attributes, out hTransaction, out hChangeOfOwnerEvent);
            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }

            return new IntPtr[] { (IntPtr) hTransaction, hChangeOfOwnerEvent };
        }

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        private void End(bool commit)
        {
            uint ret = NativeMethods.MsiEndTransaction(commit ? 1 : 0);
            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
        }
    }
}
