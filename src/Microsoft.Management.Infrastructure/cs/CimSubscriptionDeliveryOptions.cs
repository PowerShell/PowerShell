/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Management.Infrastructure.Internal;

namespace Microsoft.Management.Infrastructure.Options
{
    /// <summary>
    /// Represents options of <see cref="CimSubscriptionDelivery"/>
    /// </summary>
    public class CimSubscriptionDeliveryOptions : IDisposable
#if(!_CORECLR)
        //
        // Only implement these interfaces on FULL CLR and not Core CLR
        //
        , ICloneable
#endif
    {
        #region Constructors

        private Native.SubscriptionDeliveryOptionsHandle _subscriptionDeliveryOptionsHandle;
        internal Native.SubscriptionDeliveryOptionsHandle SubscriptionDeliveryOptionsHandle 
        { 
            get
            {
                this.AssertNotDisposed();
                return this._subscriptionDeliveryOptionsHandle;
            }
        }

        /// <summary>
        /// Creates a new <see cref="CimSubscriptionDeliveryOptions"/>
        /// </summary>
        public CimSubscriptionDeliveryOptions() : this(types: CimSubscriptionDeliveryType.None)
        {
        }

        /// <summary>
        /// Creates a new <see cref="CimSubscriptionDeliveryOptions"/>
        /// </summary>
        /// <param name="types"></param>
        public CimSubscriptionDeliveryOptions(CimSubscriptionDeliveryType types)
        {
            Initialize(types);
        }

        private void Initialize(CimSubscriptionDeliveryType types)
        {

            Native.SubscriptionDeliveryOptionsHandle tmp;
            Native.MiResult result = Native.ApplicationMethods.NewSubscriptionDeliveryOptions(CimApplication.Handle, (Native.MiSubscriptionDeliveryType)types, out tmp);
            CimException.ThrowIfMiResultFailure(result);
            this._subscriptionDeliveryOptionsHandle = tmp;
        }

        /// <summary>
        /// Instantiates a deep copy of <paramref name="optionsToClone"/>
        /// </summary>
        /// <param name="optionsToClone">options to clone</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="optionsToClone"/> is <c>null</c></exception>
        public CimSubscriptionDeliveryOptions(CimSubscriptionDeliveryOptions optionsToClone)
        {
            if (optionsToClone == null)
            {
                throw new ArgumentNullException("optionsToClone");
            }
            Native.SubscriptionDeliveryOptionsHandle tmp;
            Native.MiResult result = Native.SubscriptionDeliveryOptionsMethods.Clone(optionsToClone.SubscriptionDeliveryOptionsHandle, out tmp);
            CimException.ThrowIfMiResultFailure(result);
            this._subscriptionDeliveryOptionsHandle = tmp;
        }
        #endregion Constructors

        #region Options

        /// <summary>
        /// Sets a string
        /// </summary>
        /// <param name="optionName"></param>
        /// <param name="optionValue"></param>
        /// <param name="flags"></param>        
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="optionName"/> is <c>null</c></exception>
        public void SetString(string optionName, string optionValue, UInt32 flags)
        {
            if (string.IsNullOrWhiteSpace(optionName))
            {
                throw new ArgumentNullException("optionName");
            }
            this.AssertNotDisposed();

            Native.MiResult result = Native.SubscriptionDeliveryOptionsMethods.SetString(this._subscriptionDeliveryOptionsHandle, optionName, optionValue, flags);
            CimException.ThrowIfMiResultFailure(result);
        }

        /// <summary>
        /// Sets a custom option
        /// </summary>
        /// <param name="optionName"></param>
        /// <param name="optionValue"></param>
        /// <param name="flags"></param>           
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="optionName"/> is <c>null</c></exception>
        public void SetNumber(string optionName, UInt32 optionValue, UInt32 flags)
        {
            if (string.IsNullOrWhiteSpace(optionName))
            {
                throw new ArgumentNullException("optionName");
            }
            this.AssertNotDisposed();

            Native.MiResult result = Native.SubscriptionDeliveryOptionsMethods.SetNumber(this._subscriptionDeliveryOptionsHandle, optionName, optionValue, flags);
            CimException.ThrowIfMiResultFailure(result);
        }

        /// <summary>
        /// Sets a custom option
        /// </summary>
        /// <param name="optionName"></param>
        /// <param name="optionValue"></param>
        /// <param name="flags"></param>           
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="optionName"/> is <c>null</c></exception>
        public void SetDateTime(string optionName, DateTime optionValue, UInt32 flags)
        {
            if (string.IsNullOrWhiteSpace(optionName))
            {
                throw new ArgumentNullException("optionName");
            }
            this.AssertNotDisposed();

            Native.MiResult result = Native.SubscriptionDeliveryOptionsMethods.SetDateTime(this._subscriptionDeliveryOptionsHandle, optionName, optionValue, flags);
            CimException.ThrowIfMiResultFailure(result);
        }

        /// <summary>
        /// Sets a custom option
        /// </summary>
        /// <param name="optionName"></param>
        /// <param name="optionValue"></param>
        /// <param name="flags"></param>           
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="optionName"/> is <c>null</c></exception>
        public void SetDateTime(string optionName, TimeSpan optionValue, UInt32 flags)
        {
            if (string.IsNullOrWhiteSpace(optionName))
            {
                throw new ArgumentNullException("optionName");
            }
            this.AssertNotDisposed();

            Native.MiResult result = Native.SubscriptionDeliveryOptionsMethods.SetDateTime(this._subscriptionDeliveryOptionsHandle, optionName, optionValue, flags);
            CimException.ThrowIfMiResultFailure(result);
        }

        /// <summary>
        /// Sets a custom option
        /// </summary>
        /// <param name="optionName"></param>
        /// <param name="optionValue"></param>
        /// <param name="flags"></param>           
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="optionName"/> is <c>null</c></exception>
        public void SetInterval(string optionName, TimeSpan optionValue, UInt32 flags)
        {
            if (string.IsNullOrWhiteSpace(optionName))
            {
                throw new ArgumentNullException("optionName");
            }
            this.AssertNotDisposed();

            Native.MiResult result = Native.SubscriptionDeliveryOptionsMethods.SetInterval(this._subscriptionDeliveryOptionsHandle, optionName, optionValue, flags);
            CimException.ThrowIfMiResultFailure(result);
        }
        /// <summary>
        /// AddCredentials
        /// </summary>
        /// <param name="optionName"></param>
        /// <param name="optionValue"></param>
        /// <param name="flags"></param>           
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="optionName"/> is <c>null</c></exception>
        public void AddCredentials(string optionName, CimCredential optionValue, UInt32 flags)
        {
            if (string.IsNullOrWhiteSpace(optionName) || optionValue == null)
            {
                throw new ArgumentNullException("optionName");
            }   
            if( optionValue == null )
            {
                throw new ArgumentNullException("optionValue");
            }
            this.AssertNotDisposed();

            Native.MiResult result = Native.SubscriptionDeliveryOptionsMethods.AddCredentials(this._subscriptionDeliveryOptionsHandle, optionName, optionValue.GetCredential(), flags);
            CimException.ThrowIfMiResultFailure(result);
        }        


        #endregion Options

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
                this._subscriptionDeliveryOptionsHandle.Dispose();
                this._subscriptionDeliveryOptionsHandle = null;
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

        #region ICloneable Members

#if(!_CORECLR)
        object ICloneable.Clone()
        {
            return new CimSubscriptionDeliveryOptions(this);
        }
#endif // !_CORECLR

        #endregion
    }
}

namespace Microsoft.Management.Infrastructure.Options.Internal
{
    internal static class CimSubscriptionDeliveryOptionssExtensionMethods
    {
        static internal Native.SubscriptionDeliveryOptionsHandle GetSubscriptionDeliveryOptionsHandle(this CimSubscriptionDeliveryOptions deliveryOptions)
        {
            return deliveryOptions != null ? deliveryOptions.SubscriptionDeliveryOptionsHandle : null;
        }
    }
}

