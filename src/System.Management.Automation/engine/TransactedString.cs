// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text;
using System.Transactions;

namespace Microsoft.PowerShell.Commands.Management
{
    /// <summary>
    /// Represents a string that can be used in transactions.
    /// </summary>
    public class TransactedString : IEnlistmentNotification
    {
        private StringBuilder _value;
        private StringBuilder _temporaryValue;
        private Transaction _enlistedTransaction = null;

        /// <summary>
        /// Constructor for the TransactedString class.
        /// </summary>
        public TransactedString() : this(string.Empty)
        {
        }

        /// <summary>
        /// Constructor for the TransactedString class.
        /// </summary>
        /// <param name="value">
        /// The initial value of the transacted string.
        /// </param>
        public TransactedString(string value)
        {
            _value = new StringBuilder(value);
            _temporaryValue = null;
        }

        /// <summary>
        /// Make the transacted changes permanent.
        /// </summary>
        void IEnlistmentNotification.Commit(Enlistment enlistment)
        {
            _value = new StringBuilder(_temporaryValue.ToString());
            _temporaryValue = null;
            _enlistedTransaction = null;
            enlistment.Done();
        }

        /// <summary>
        /// Discard the transacted changes.
        /// </summary>
        void IEnlistmentNotification.Rollback(Enlistment enlistment)
        {
            _temporaryValue = null;
            _enlistedTransaction = null;
            enlistment.Done();
        }

        /// <summary>
        /// Discard the transacted changes.
        /// </summary>
        void IEnlistmentNotification.InDoubt(Enlistment enlistment)
        {
            enlistment.Done();
        }

        void IEnlistmentNotification.Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }

        /// <summary>
        /// Append text to the transacted string.
        /// </summary>
        /// <param name="text">
        /// The text to append.
        /// </param>
        public void Append(string text)
        {
            ValidateTransactionOrEnlist();

            if (_enlistedTransaction != null)
            {
                _temporaryValue.Append(text);
            }
            else
            {
                _value.Append(text);
            }
        }

        /// <summary>
        /// Remove text from the transacted string.
        /// </summary>
        /// <param name="startIndex">
        /// The position in the string from which to start removing.
        /// </param>
        /// <param name="length">
        /// The length of text to remove.
        /// </param>
        public void Remove(int startIndex, int length)
        {
            ValidateTransactionOrEnlist();

            if (_enlistedTransaction != null)
            {
                _temporaryValue.Remove(startIndex, length);
            }
            else
            {
                _value.Remove(startIndex, length);
            }
        }

        /// <summary>
        /// Gets the length of the transacted string. If this is
        /// called within the transaction, it returns the length of
        /// the transacted value. Otherwise, it returns the length of
        /// the original value.
        /// </summary>
        public int Length
        {
            get
            {
                // If we're not in a transaction, or we are in a different transaction than the one we
                // enlisted to, return the publicly visible state.
                if (
                    (Transaction.Current == null) ||
                    (_enlistedTransaction != Transaction.Current))
                {
                    return _value.Length;
                }
                else
                {
                    return _temporaryValue.Length;
                }
            }
        }

        /// <summary>
        /// Gets the System.String that represents the transacted
        /// transacted string. If this is called within the
        /// transaction, it returns the transacted value.
        /// Otherwise, it returns the original value.
        /// </summary>
        public override string ToString()
        {
            // If we're not in a transaction, or we are in a different transaction than the one we
            // enlisted to, return the publicly visible state.
            if (
               (Transaction.Current == null) ||
               (_enlistedTransaction != Transaction.Current))
            {
                return _value.ToString();
            }
            else
            {
                return _temporaryValue.ToString();
            }
        }

        private void ValidateTransactionOrEnlist()
        {
            // We're in a transaction
            if (Transaction.Current != null)
            {
                // We haven't yet been called inside of a transaction. So enlist
                // in the transaction, and store our save point
                if (_enlistedTransaction == null)
                {
                    Transaction.Current.EnlistVolatile(this, EnlistmentOptions.None);
                    _enlistedTransaction = Transaction.Current;

                    _temporaryValue = new StringBuilder(_value.ToString());
                }
                // We're already enlisted in a transaction
                else
                {
                    // And we're in that transaction
                    if (Transaction.Current != _enlistedTransaction)
                    {
                        throw new InvalidOperationException("Cannot modify string. It has been modified by another transaction.");
                    }
                }
            }
            // We're not in a transaction
            else
            {
                // If we're not subscribed to a transaction, modify the underlying value
                if (_enlistedTransaction != null)
                {
                    throw new InvalidOperationException("Cannot modify string. It has been modified by another transaction.");
                }
            }
        }
    }
}

