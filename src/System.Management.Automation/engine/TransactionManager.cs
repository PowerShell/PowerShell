// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#pragma warning disable 1634, 1691

using System.Collections.Generic;
using System.Transactions;
using System.Management.Automation.Internal;

namespace System.Management.Automation
{
    /// <summary>
    /// The status of a PowerShell transaction.
    /// </summary>
    public enum PSTransactionStatus
    {
        /// <summary>
        /// The transaction has been rolled back.
        /// </summary>
        RolledBack = 0,

        /// <summary>
        /// The transaction has been committed.
        /// </summary>
        Committed = 1,

        /// <summary>
        /// The transaction is currently active.
        /// </summary>
        Active = 2
    }

    /// <summary>
    /// Represents an active transaction.
    /// </summary>
    public sealed class PSTransaction : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the PSTransaction class.
        /// </summary>
        internal PSTransaction(RollbackSeverity rollbackPreference, TimeSpan timeout)
        {
            _transaction = new CommittableTransaction(timeout);
            RollbackPreference = rollbackPreference;
            _subscriberCount = 1;
        }

        /// <summary>
        /// Initializes a new instance of the PSTransaction class using a CommittableTransaction.
        /// </summary>
        internal PSTransaction(CommittableTransaction transaction, RollbackSeverity severity)
        {
            _transaction = transaction;
            RollbackPreference = severity;
            _subscriberCount = 1;
        }

        private CommittableTransaction _transaction;

        /// <summary>
        /// Gets the rollback preference for this transaction.
        /// </summary>
        public RollbackSeverity RollbackPreference { get; }

        /// <summary>
        /// Gets the number of subscribers to this transaction.
        /// </summary>
        public int SubscriberCount
        {
            get
            {
                // Verify the transaction hasn't been rolled back beneath us
                if (this.IsRolledBack)
                {
                    this.SubscriberCount = 0;
                }

                return _subscriberCount;
            }

            set { _subscriberCount = value; }
        }

        private int _subscriberCount;

        /// <summary>
        /// Returns the status of this transaction.
        /// </summary>
        public PSTransactionStatus Status
        {
            get
            {
                if (IsRolledBack)
                {
                    return PSTransactionStatus.RolledBack;
                }
                else if (IsCommitted)
                {
                    return PSTransactionStatus.Committed;
                }
                else
                {
                    return PSTransactionStatus.Active;
                }
            }
        }

        /// <summary>
        /// Activates the transaction held by this PSTransaction.
        /// </summary>
        internal void Activate()
        {
            Transaction.Current = _transaction;
        }

        /// <summary>
        /// Commits the transaction held by this PSTransaction.
        /// </summary>
        internal void Commit()
        {
            _transaction.Commit();
            IsCommitted = true;
        }

        /// <summary>
        /// Rolls back the transaction held by this PSTransaction.
        /// </summary>
        internal void Rollback()
        {
            _transaction.Rollback();
            _isRolledBack = true;
        }

        /// <summary>
        /// Determines whether this PSTransaction has been
        /// rolled back or not.
        /// </summary>
        internal bool IsRolledBack
        {
            get
            {
                // Check if it's been aborted underneath us
                if (
                    (!_isRolledBack) &&
                    (_transaction != null) &&
                    (_transaction.TransactionInformation.Status == TransactionStatus.Aborted))
                {
                    _isRolledBack = true;
                }

                return _isRolledBack;
            }

            set
            {
                _isRolledBack = value;
            }
        }

        private bool _isRolledBack = false;

        /// <summary>
        /// Determines whether this PSTransaction
        /// has been committed or not.
        /// </summary>
        internal bool IsCommitted { get; set; } = false;

        /// <summary>
        /// Destructor for the PSTransaction class.
        /// </summary>
        ~PSTransaction()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes the PSTransaction object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the PSTransaction object, which disposes the
        /// underlying transaction.
        /// <param name="disposing">
        /// Whether to actually dispose the object.
        /// </param>
        /// </summary>
        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_transaction != null)
                {
                    _transaction.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// Supports the transaction management infrastructure for the PowerShell engine.
    /// </summary>
    public sealed class PSTransactionContext : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the PSTransactionManager class.
        /// </summary>
        internal PSTransactionContext(PSTransactionManager transactionManager)
        {
            _transactionManager = transactionManager;
            transactionManager.SetActive();
        }

        private PSTransactionManager _transactionManager;

        /// <summary>
        /// Destructor for the PSTransactionManager class.
        /// </summary>
        ~PSTransactionContext()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes the PSTransactionContext object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the PSTransactionContext object, which resets the
        /// active PSTransaction.
        /// <param name="disposing">
        /// Whether to actually dispose the object.
        /// </param>
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _transactionManager.ResetActive();
            }
        }
    }

    /// <summary>
    /// The severity of error that causes PowerShell to automatically
    /// rollback the transaction.
    /// </summary>
    public enum RollbackSeverity
    {
        /// <summary>
        /// Non-terminating errors or worse.
        /// </summary>
        Error,

        /// <summary>
        /// Terminating errors or worse.
        /// </summary>
        TerminatingError,

        /// <summary>
        /// Do not rollback the transaction on error.
        /// </summary>
        Never
    }
}

namespace System.Management.Automation.Internal
{
    /// <summary>
    /// Supports the transaction management infrastructure for the PowerShell engine.
    /// </summary>
    internal sealed class PSTransactionManager : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the PSTransactionManager class.
        /// </summary>
        internal PSTransactionManager()
        {
            _transactionStack = new Stack<PSTransaction>();
            _transactionStack.Push(null);
        }

        /// <summary>
        /// Called by engine APIs to ensure they are protected from
        /// ambient transactions.
        /// </summary>
        internal static IDisposable GetEngineProtectionScope()
        {
            if (s_engineProtectionEnabled && (Transaction.Current != null))
            {
                return new System.Transactions.TransactionScope(
                    System.Transactions.TransactionScopeOption.Suppress);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Called by the transaction manager to enable engine
        /// protection the first time a transaction is activated.
        /// Engine protection APIs remain protected from this point on.
        /// </summary>
        internal static void EnableEngineProtection()
        {
            s_engineProtectionEnabled = true;
        }

        private static bool s_engineProtectionEnabled = false;

        /// <summary>
        /// Gets the rollback preference for the active transaction.
        /// </summary>
        internal RollbackSeverity RollbackPreference
        {
            get
            {
                PSTransaction currentTransaction = _transactionStack.Peek();

                if (currentTransaction == null)
                {
                    string error = TransactionStrings.NoTransactionActive;

                    // This is not an expected condition, and is just protective
                    // coding.
#pragma warning suppress 56503
                    throw new InvalidOperationException(error);
                }

                return currentTransaction.RollbackPreference;
            }
        }

        /// <summary>
        /// Creates a new Transaction if none are active. Otherwise, increments
        /// the subscriber count for the active transaction.
        /// </summary>
        internal void CreateOrJoin()
        {
            CreateOrJoin(RollbackSeverity.Error, TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Creates a new Transaction if none are active. Otherwise, increments
        /// the subscriber count for the active transaction.
        /// </summary>
        internal void CreateOrJoin(RollbackSeverity rollbackPreference, TimeSpan timeout)
        {
            PSTransaction currentTransaction = _transactionStack.Peek();

            // There is a transaction on the stack
            if (currentTransaction != null)
            {
                // If you are already in a transaction that has been aborted, or committed,
                // create it.
                if (currentTransaction.IsRolledBack || currentTransaction.IsCommitted)
                {
                    // Clean up the "used" one
                    _transactionStack.Pop().Dispose();

                    // And add a new one to the stack
                    _transactionStack.Push(new PSTransaction(rollbackPreference, timeout));
                }
                else
                {
                    // This is a usable one. Add a subscriber to it.
                    currentTransaction.SubscriberCount++;
                }
            }
            else
            {
                // Add a new transaction to the stack
                _transactionStack.Push(new PSTransaction(rollbackPreference, timeout));
            }
        }

        /// <summary>
        /// Creates a new Transaction that should be managed independently of
        /// any parent transactions.
        /// </summary>
        internal void CreateNew()
        {
            CreateNew(RollbackSeverity.Error, TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Creates a new Transaction that should be managed independently of
        /// any parent transactions.
        /// </summary>
        internal void CreateNew(RollbackSeverity rollbackPreference, TimeSpan timeout)
        {
            _transactionStack.Push(new PSTransaction(rollbackPreference, timeout));
        }

        /// <summary>
        /// Completes the current transaction. If only one subscriber is active, this
        /// commits the transaction. Otherwise, it reduces the subscriber count by one.
        /// </summary>
        internal void Commit()
        {
            PSTransaction currentTransaction = _transactionStack.Peek();

            // Should not be able to commit a transaction that is not active
            if (currentTransaction == null)
            {
                string error = TransactionStrings.NoTransactionActiveForCommit;
                throw new InvalidOperationException(error);
            }

            // If you are already in a transaction that has been aborted
            if (currentTransaction.IsRolledBack)
            {
                string error = TransactionStrings.TransactionRolledBackForCommit;
                throw new TransactionAbortedException(error);
            }

            // If you are already in a transaction that has been committed
            if (currentTransaction.IsCommitted)
            {
                string error = TransactionStrings.CommittedTransactionForCommit;
                throw new InvalidOperationException(error);
            }

            if (currentTransaction.SubscriberCount == 1)
            {
                currentTransaction.Commit();
                currentTransaction.SubscriberCount = 0;
            }
            else
            {
                currentTransaction.SubscriberCount--;
            }

            // Now that we've committed, go back to the last available transaction
            while ((_transactionStack.Count > 2) &&
                (_transactionStack.Peek().IsRolledBack || _transactionStack.Peek().IsCommitted))
            {
                _transactionStack.Pop().Dispose();
            }
        }

        /// <summary>
        /// Aborts the current transaction, no matter how many subscribers are part of it.
        /// </summary>
        internal void Rollback()
        {
            Rollback(false);
        }

        /// <summary>
        /// Aborts the current transaction, no matter how many subscribers are part of it.
        /// </summary>
        internal void Rollback(bool suppressErrors)
        {
            PSTransaction currentTransaction = _transactionStack.Peek();

            // Should not be able to roll back a transaction that is not active
            if (currentTransaction == null)
            {
                string error = TransactionStrings.NoTransactionActiveForRollback;
                throw new InvalidOperationException(error);
            }

            // If you are already in a transaction that has been aborted
            if (currentTransaction.IsRolledBack)
            {
                if (!suppressErrors)
                {
                    // Otherwise, you should not be able to roll it back.
                    string error = TransactionStrings.TransactionRolledBackForRollback;
                    throw new TransactionAbortedException(error);
                }
            }

            // See if they've already committed the transaction
            if (currentTransaction.IsCommitted)
            {
                if (!suppressErrors)
                {
                    string error = TransactionStrings.CommittedTransactionForRollback;
                    throw new InvalidOperationException(error);
                }
            }

            // Roll back the transaction if it hasn't been rolled back
            currentTransaction.SubscriberCount = 0;
            currentTransaction.Rollback();

            // Now that we've rolled back, go back to the last available transaction
            while ((_transactionStack.Count > 2) &&
                (_transactionStack.Peek().IsRolledBack || _transactionStack.Peek().IsCommitted))
            {
                _transactionStack.Pop().Dispose();
            }
        }

        /// <summary>
        /// Sets the base transaction; any transactions created thereafter will be nested to this instance.
        /// </summary>
        internal void SetBaseTransaction(CommittableTransaction transaction, RollbackSeverity severity)
        {
            if (this.HasTransaction)
            {
                throw new InvalidOperationException(TransactionStrings.BaseTransactionMustBeFirst);
            }

            PSTransaction currentTransaction = _transactionStack.Peek();

            // If there is a "used" transaction at the top of the stack, clean it up
            while (_transactionStack.Peek() != null &&
                (_transactionStack.Peek().IsRolledBack || _transactionStack.Peek().IsCommitted))
            {
                _transactionStack.Pop().Dispose();
            }

            _baseTransaction = new PSTransaction(transaction, severity);
            _transactionStack.Push(_baseTransaction);
        }

        /// <summary>
        /// Removes the transaction added by SetBaseTransaction.
        /// </summary>
        internal void ClearBaseTransaction()
        {
            if (_baseTransaction == null)
            {
                throw new InvalidOperationException(TransactionStrings.BaseTransactionNotSet);
            }

            if (_transactionStack.Peek() != _baseTransaction)
            {
                throw new InvalidOperationException(TransactionStrings.BaseTransactionNotActive);
            }

            _transactionStack.Pop().Dispose();
            _baseTransaction = null;
        }

        private Stack<PSTransaction> _transactionStack;
        private PSTransaction _baseTransaction;

        /// <summary>
        /// Returns the current engine transaction.
        /// </summary>
        internal PSTransaction GetCurrent()
        {
            return _transactionStack.Peek();
        }

        /// <summary>
        /// Activates the current transaction, both in the engine, and in the Ambient.
        /// </summary>
        internal void SetActive()
        {
            PSTransactionManager.EnableEngineProtection();

            PSTransaction currentTransaction = _transactionStack.Peek();

            // Should not be able to activate a transaction that is not active
            if (currentTransaction == null)
            {
                string error = TransactionStrings.NoTransactionForActivation;
                throw new InvalidOperationException(error);
            }

            // If you are already in a transaction that has been aborted, you should
            // not be able to activate it.
            if (currentTransaction.IsRolledBack)
            {
                string error = TransactionStrings.NoTransactionForActivationBecauseRollback;
                throw new TransactionAbortedException(error);
            }

            _previousActiveTransaction = Transaction.Current;
            currentTransaction.Activate();
        }

        private Transaction _previousActiveTransaction;

        /// <summary>
        /// Deactivates the current transaction in the engine, and restores the
        /// ambient transaction.
        /// </summary>
        internal void ResetActive()
        {
            // Even if you are in a transaction that has been aborted, you
            // should still be able to restore the current transaction.

            Transaction.Current = _previousActiveTransaction;
            _previousActiveTransaction = null;
        }

        /// <summary>
        /// Determines if you have a transaction that you can set active and work on.
        /// </summary>
        internal bool HasTransaction
        {
            get
            {
                PSTransaction currentTransaction = _transactionStack.Peek();

                if ((currentTransaction != null) &&
                    (!currentTransaction.IsCommitted) &&
                    (!currentTransaction.IsRolledBack))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Determines if the last transaction has been committed.
        /// </summary>
        internal bool IsLastTransactionCommitted
        {
            get
            {
                PSTransaction currentTransaction = _transactionStack.Peek();

                if (currentTransaction != null)
                {
                    return currentTransaction.IsCommitted;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Determines if the last transaction has been rolled back.
        /// </summary>
        internal bool IsLastTransactionRolledBack
        {
            get
            {
                PSTransaction currentTransaction = _transactionStack.Peek();

                if (currentTransaction != null)
                {
                    return currentTransaction.IsRolledBack;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Destructor for the PSTransactionManager class.
        /// </summary>
        ~PSTransactionManager()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes the PSTransactionManager object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the PSTransactionContext object, which resets the
        /// active PSTransaction.
        /// <param name="disposing">
        /// Whether to actually dispose the object.
        /// </param>
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "baseTransaction", Justification = "baseTransaction should not be disposed since we do not own it - it belongs to the caller")]
        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                ResetActive();

                while (_transactionStack.Peek() != null)
                {
                    PSTransaction currentTransaction = _transactionStack.Pop();

                    if (currentTransaction != _baseTransaction)
                    {
                        currentTransaction.Dispose();
                    }
                }
            }
        }
    }
}

