/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Management.Infrastructure.Internal;

namespace Microsoft.Management.Infrastructure.Options
{
    /// <summary>
    /// Represents options of <see cref="CimSession"/>
    /// </summary>
    public class CimSessionOptions : IDisposable
#if(!_CORECLR)
        //
        // Only implement these interfaces on FULL CLR and not Core CLR
        //
        , ICloneable
#endif
    {
        private readonly Lazy<Native.DestinationOptionsHandle> _destinationOptionsHandle;
        internal Native.DestinationOptionsHandle DestinationOptionsHandleOnDemand 
        { 
            get
            {
                this.AssertNotDisposed();
                return this._destinationOptionsHandle.Value;
            }
        }
        internal Native.DestinationOptionsHandle DestinationOptionsHandle
        { 
            get
            {
                this.AssertNotDisposed();
                if (this._destinationOptionsHandle.IsValueCreated)
                {
                    return this._destinationOptionsHandle.Value;
                }
                return null;
            }
        }

        internal string Protocol { get; private set; }

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="CimSessionOptions"/> object that uses the default transport protocol
        /// </summary>
        public CimSessionOptions()
            : this(protocol: null, validateProtocol: false)
        {
        }

        /// <summary>
        /// Creates a new <see cref="CimSessionOptions"/> object that uses the specified transport protocol
        /// </summary>
        /// <param name="protocol">Protocol to use.  This string corresponds to a registry key at TODO/FIXME.</param>
        protected CimSessionOptions(string protocol)
            : this(protocol, validateProtocol: true)
        {
        }

        private CimSessionOptions(string protocol, bool validateProtocol)
        {
            if (validateProtocol)
            {
                if (string.IsNullOrWhiteSpace(protocol))
                {
                    throw new ArgumentNullException("protocol");
                }
            }

            this.Protocol = protocol;

            this._destinationOptionsHandle = new Lazy<Native.DestinationOptionsHandle>(
                    delegate
                    {
                        Native.DestinationOptionsHandle tmp;
                        Native.MiResult result = Native.ApplicationMethods.NewDestinationOptions(CimApplication.Handle, out tmp);
                        CimException.ThrowIfMiResultFailure(result);
                        return tmp;
                    });
        }

        /// <summary>
        /// Instantiates a deep copy of <paramref name="optionsToClone"/>
        /// </summary>
        /// <param name="optionsToClone">options to clone</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="optionsToClone"/> is <c>null</c></exception>
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "Old Code")]
        internal CimSessionOptions(CimSessionOptions optionsToClone)
        {
            if (optionsToClone == null)
            {
                throw new ArgumentNullException("optionsToClone");
            }

            this.Protocol = optionsToClone.Protocol;
            if (optionsToClone.DestinationOptionsHandle == null)
            {
                // underline DestinationOptions is not created yet, then create a new one
                this._destinationOptionsHandle = new Lazy<Native.DestinationOptionsHandle>(
                    delegate
                    {
                        Native.DestinationOptionsHandle tmp;
                        Native.MiResult result = Native.ApplicationMethods.NewDestinationOptions(CimApplication.Handle, out tmp);
                        CimException.ThrowIfMiResultFailure(result);
                        return tmp;
                    });
            }
            else
            {
                Native.DestinationOptionsHandle tmp;
                Native.MiResult result = Native.DestinationOptionsMethods.Clone(optionsToClone.DestinationOptionsHandle, out tmp);
                CimException.ThrowIfMiResultFailure(result);
                this._destinationOptionsHandle = new Lazy<Native.DestinationOptionsHandle>(() => tmp);
            }
            // Ensure the destinationOptions is created
            if (this.DestinationOptionsHandleOnDemand == null)
            {
                CimException.ThrowIfMiResultFailure(Native.MiResult.FAILED);
            }
        }
        #endregion Constructors

        #region Options

        /// <summary>
        /// Sets a custom option
        /// </summary>
        /// <param name="optionName"></param>
        /// <param name="optionValue"></param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="optionName"/> is <c>null</c></exception>
        public void SetCustomOption(string optionName, string optionValue)
        {
            if (string.IsNullOrWhiteSpace(optionName))
            {
                throw new ArgumentNullException("optionName");
            }
            this.AssertNotDisposed();

            Native.MiResult result = Native.DestinationOptionsMethods.SetCustomOption(this.DestinationOptionsHandleOnDemand, optionName, optionValue);
            CimException.ThrowIfMiResultFailure(result);
        }

        /// <summary>
        /// Sets a custom option
        /// </summary>
        /// <param name="optionName"></param>
        /// <param name="optionValue"></param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="optionName"/> is <c>null</c></exception>
        public void SetCustomOption(string optionName, UInt32 optionValue)
        {
            if (string.IsNullOrWhiteSpace(optionName))
            {
                throw new ArgumentNullException("optionName");
            }
            this.AssertNotDisposed();

            Native.MiResult result = Native.DestinationOptionsMethods.SetCustomOption(this.DestinationOptionsHandleOnDemand, optionName, optionValue);
            CimException.ThrowIfMiResultFailure(result);
        }

        /// <summary>
        /// Sets a Destination Credential
        /// </summary>
        /// <param name="credential"></param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="credential"/> is <c>null</c></exception>
        public void AddDestinationCredentials(CimCredential credential)
        {
            if (credential == null)
            {
                throw new ArgumentNullException("credential");
            }
            this.AssertNotDisposed();

            Native.MiResult result = Native.DestinationOptionsMethods.AddDestinationCredentials(this.DestinationOptionsHandleOnDemand, credential.GetCredential());
            CimException.ThrowIfMiResultFailure(result);
        }        

        /// <summary>
        /// Sets timeout
        /// </summary>
        /// <value></value>
        public TimeSpan Timeout
        {
            set
            {
                this.AssertNotDisposed();

                Native.MiResult result = Native.DestinationOptionsMethods.SetTimeout(this.DestinationOptionsHandleOnDemand, value);
                CimException.ThrowIfMiResultFailure(result);
            }

            get
            {
                this.AssertNotDisposed();

                TimeSpan timeout;
                Native.MiResult result = Native.DestinationOptionsMethods.GetTimeout(this.DestinationOptionsHandleOnDemand, out timeout);
                return (result == Native.MiResult.OK) ? timeout : TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Sets data culture.
        /// </summary>
        /// <value>Culture to use.  &lt;c&gt;null&lt;/c&gt; indicates the current thread&apos;s culture</value>
        public CultureInfo Culture
        {
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                this.AssertNotDisposed();

                Native.MiResult result = Native.DestinationOptionsMethods.SetDataLocale(
                    this.DestinationOptionsHandleOnDemand, value.Name);
                CimException.ThrowIfMiResultFailure(result);
            }

            get
            {
                this.AssertNotDisposed();

                string locale;
                Native.MiResult result = Native.DestinationOptionsMethods.GetDataLocale(
                    this.DestinationOptionsHandleOnDemand, out locale);
                return (result == Native.MiResult.OK) ? new CultureInfo(locale) : null;
            }
        }

        /// <summary>
        /// Sets UI culture.
        /// </summary>
        /// <value>Culture to use.  &lt;c&gt;null&lt;/c&gt; indicates the current thread&apos;s UI culture</value>
        public CultureInfo UICulture
        {
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                this.AssertNotDisposed();

                Native.MiResult result = Native.DestinationOptionsMethods.SetUILocale(
                    this.DestinationOptionsHandleOnDemand, value.Name);
                CimException.ThrowIfMiResultFailure(result);
            }

            get
            {
                this.AssertNotDisposed();
                string locale;
                Native.MiResult result = Native.DestinationOptionsMethods.GetUILocale(
                    this.DestinationOptionsHandleOnDemand, out locale);
                return (result == Native.MiResult.OK) ? new CultureInfo(locale) : null;
            }
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
                Native.DestinationOptionsHandle tmpHandle = this.DestinationOptionsHandle;
                if (tmpHandle != null)
                {
                    tmpHandle.Dispose();
                }
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
            return new CimSessionOptions(this);
        }
#endif // !_CORECLR

        #endregion
    }
}
