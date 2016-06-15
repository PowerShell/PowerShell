/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Transactions;
using System.Threading;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerShell.Commands.Management
{
    /// <summary>
    /// Represents a a string that can be used in transactions.
    /// </summary>
    public class TransactedString : IEnlistmentNotification
    {
        StringBuilder m_Value;
        StringBuilder m_TemporaryValue;
        Transaction enlistedTransaction = null;

        /// <summary>
        /// Constructor for the TransactedString class.
        /// </summary>
        public TransactedString() : this("")
        {
        }

        /// <summary>
        /// Constructor for the TransactedString class.
        ///
        /// <param name="value">
        /// The initial value of the transacted string.
        /// </param>
        /// </summary>
        public TransactedString(string value)
        {
            m_Value = new StringBuilder(value);
            m_TemporaryValue = null;
        }

        /// <summary>
        /// Make the transacted changes permanent.
        /// </summary>
        void IEnlistmentNotification.Commit(Enlistment enlistment)
        {
            m_Value = new StringBuilder(m_TemporaryValue.ToString());
            m_TemporaryValue = null;
            enlistedTransaction = null;
            enlistment.Done();
        }

        /// <summary>
        /// Discard the transacted changes.
        /// </summary>
        void IEnlistmentNotification.Rollback(Enlistment enlistment)
        {
            m_TemporaryValue = null;
            enlistedTransaction = null;
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
        ///
        /// <param name="text">
        /// The text to append.
        /// </param>
        /// </summary>
        public void Append(string text)
        {
            ValidateTransactionOrEnlist();

            if(enlistedTransaction != null)
            {
                m_TemporaryValue.Append(text);
            }
            else
            {
                m_Value.Append(text);
            }
        }

        /// <summary>
        /// Remove text from the transacted string.
        ///
        /// <param name="startIndex">
        /// The position in the string from which to start removing.
        /// </param>
        /// <param name="length">
        /// The length of text to remove.
        /// </param>
        /// </summary>
        public void Remove(int startIndex, int length)
        {
            ValidateTransactionOrEnlist();

            if(enlistedTransaction != null)
            {
                m_TemporaryValue.Remove(startIndex, length);
            }
            else
            {
                m_Value.Remove(startIndex, length);
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
                if(
                    (Transaction.Current == null) ||
                    (enlistedTransaction != Transaction.Current))
                {
                    return m_Value.Length;
                }
                else
                {
                    return m_TemporaryValue.Length;
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
            if(
               (Transaction.Current == null) ||
               (enlistedTransaction != Transaction.Current))
            {
                return m_Value.ToString();
            }
            else
            {
                return m_TemporaryValue.ToString();
            }
        }

        private void ValidateTransactionOrEnlist()
        {
            // We're in a transaction
            if(Transaction.Current != null)
            {
                // We haven't yet been called inside of a transaction. So enlist
                // in the transaction, and store our save point
                if(enlistedTransaction == null)
                {
                    Transaction.Current.EnlistVolatile(this, EnlistmentOptions.None);
                    enlistedTransaction = Transaction.Current;

                    m_TemporaryValue = new StringBuilder(m_Value.ToString());
                }
                // We're already enlisted in a transaction
                else
                {
                    // And we're in that transaction
                    if(Transaction.Current != enlistedTransaction)
                    {
                        throw new InvalidOperationException("Cannot modify string. It has been modified by another transaction.");
                    }
                }
            }
            // We're not in a transaction
            else
            {
                // If we're not subscribed to a transaction, modify the underlying value
                if(enlistedTransaction != null)
                {
                    throw new InvalidOperationException("Cannot modify string. It has been modified by another transaction.");
                }
            }
        }
    }
} // namespace Microsoft.Test.Management.Automation

