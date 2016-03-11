/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.Management.Infrastructure.Internal;
using Microsoft.Management.Infrastructure.Internal.Operations;
using Microsoft.Management.Infrastructure.Options.Internal;

namespace Microsoft.Management.Infrastructure.Options
{
    /// <summary>
    /// Represents options of a CIM operation.
    /// </summary>
    public class CimOperationOptions : IDisposable
#if(!_CORECLR)
        //
        // Only implement these interfaces on FULL CLR and not Core CLR
        //
        , ICloneable
#endif
    {
        private readonly Lazy<Native.OperationOptionsHandle> _operationOptionsHandle;
        private Native.OperationOptionsHandle OperationOptionsHandleOnDemand
        {
            get
            {
                this.AssertNotDisposed();
                return this._operationOptionsHandle.Value;
            }
        }
        internal Native.OperationOptionsHandle OperationOptionsHandle
        {
            get
            {
                if (this._operationOptionsHandle.IsValueCreated)
                {
                    return this._operationOptionsHandle.Value;
                }
                return null;
            }
        }

        private readonly Native.OperationCallbacks _operationCallback;
        internal Native.OperationCallbacks OperationCallback
        {
            get
            {
                this.AssertNotDisposed();
                return this._operationCallback;
            }
        }

        
        /// <summary>
        /// Creates a new <see cref="CimOperationOptions"/> instance (where the server has to understand all the options).
        /// </summary>
        public CimOperationOptions()
            : this(mustUnderstand: false)
        {
        }

        /// <summary>
        /// Creates a new <see cref="CimOperationOptions"/> instance.
        /// </summary>
        /// <param name="mustUnderstand">Indicates whether the server has to understand all the options.</param>
        public CimOperationOptions(bool mustUnderstand)
        {
            var operationCallbacks = new Native.OperationCallbacks();
            this._operationCallback = operationCallbacks;
            _writeMessageCallback = null;
            _writeProgressCallback = null;
            _writeErrorCallback = null;
            _promptUserCallback = null;
            this._operationOptionsHandle = new Lazy<Native.OperationOptionsHandle>(
                    delegate
                    {
                        Native.OperationOptionsHandle operationOptionsHandle;
                        Native.MiResult result = Native.ApplicationMethods.NewOperationOptions(
                            CimApplication.Handle, mustUnderstand, out operationOptionsHandle);
                        CimException.ThrowIfMiResultFailure(result);
                        return operationOptionsHandle;
                    });
        }

        /// <summary>
        /// Instantiates a deep copy of <paramref name="optionsToClone"/>
        /// </summary>
        /// <param name="optionsToClone">options to clone</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="optionsToClone"/> is <c>null</c></exception>
        public CimOperationOptions(CimOperationOptions optionsToClone)
        {
            if (optionsToClone == null)
            {
                throw new ArgumentNullException("optionsToClone");
            }

            this._operationCallback = optionsToClone.GetOperationCallbacks();
            _writeMessageCallback = optionsToClone._writeMessageCallback;
            _writeProgressCallback = optionsToClone._writeProgressCallback;
            _writeErrorCallback = optionsToClone._writeErrorCallback;
            _promptUserCallback = optionsToClone._promptUserCallback;
            this._operationOptionsHandle = new Lazy<Native.OperationOptionsHandle>(
                    delegate
                    {
                        Native.OperationOptionsHandle tmp;
                        Native.MiResult result = Native.OperationOptionsMethods.Clone(optionsToClone.OperationOptionsHandle, out tmp);
                        CimException.ThrowIfMiResultFailure(result);
                        return tmp;
                    });
        }
        /// <summary>
        /// Sets operation timeout
        /// </summary>
        /// <value></value>
        public TimeSpan Timeout
        {
            set
            {
                this.AssertNotDisposed();
                Native.MiResult result = Native.OperationOptionsMethods.SetTimeout(this.OperationOptionsHandleOnDemand, value);
                CimException.ThrowIfMiResultFailure(result);
            }
            get
            {
                this.AssertNotDisposed();
                TimeSpan tempTimeout;
                Native.MiResult result = Native.OperationOptionsMethods.GetTimeout(this.OperationOptionsHandleOnDemand, out tempTimeout);
                CimException.ThrowIfMiResultFailure(result);
                return tempTimeout;
            }
        }

        /// <summary>
        /// Sets resource URI prefix
        /// </summary>
        /// <value></value>
        public Uri ResourceUriPrefix
        {
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                this.AssertNotDisposed();

                Native.MiResult result = Native.OperationOptionsMethods.SetResourceUriPrefix(this.OperationOptionsHandleOnDemand, value.ToString());
                CimException.ThrowIfMiResultFailure(result);
            }

            get
            {
                this.AssertNotDisposed();
                string tmp;
                Native.MiResult result = Native.OperationOptionsMethods.GetResourceUriPrefix(this.OperationOptionsHandleOnDemand, out tmp);
                CimException.ThrowIfMiResultFailure(result);
                return new Uri(tmp);
            }
        }

        /// <summary>
        /// Sets resource URI 
        /// </summary>
        /// <value></value>
        public Uri ResourceUri
        {
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                this.AssertNotDisposed();

                Native.MiResult result = Native.OperationOptionsMethods.SetResourceUri(this.OperationOptionsHandleOnDemand, value.ToString());
                CimException.ThrowIfMiResultFailure(result);
            }

            get
            {
                this.AssertNotDisposed();
                string tmp;
                Native.MiResult result = Native.OperationOptionsMethods.GetResourceUri(this.OperationOptionsHandleOnDemand, out tmp);
                CimException.ThrowIfMiResultFailure(result);
                return new Uri(tmp);
            }
        }        

 

        /// <summary>
        /// Sets whether to use machine ID
        /// </summary>
        /// <value></value>
        public bool UseMachineId
        {
            set
            {
                this.AssertNotDisposed();
                Native.MiResult result = Native.OperationOptionsMethods.SetUseMachineID(this.OperationOptionsHandleOnDemand, value);
                CimException.ThrowIfMiResultFailure(result);
            }

            get
            {
                this.AssertNotDisposed();
                bool tmp;
                Native.MiResult result = Native.OperationOptionsMethods.GetUseMachineID(this.OperationOptionsHandleOnDemand, out tmp);
                CimException.ThrowIfMiResultFailure(result);
                return tmp;
            }
        }

        /// <summary>
        /// Sets a custom transport option
        /// </summary>
        /// <param name="optionName"></param>
        /// <param name="optionValue"></param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="optionName"/> is <c>null</c></exception>
        public void SetOption(string optionName, string optionValue)
        {
            if (string.IsNullOrWhiteSpace(optionName))
            {
                throw new ArgumentNullException("optionName");
            }
            this.AssertNotDisposed();

            Native.MiResult result = Native.OperationOptionsMethods.SetOption(this.OperationOptionsHandleOnDemand, optionName, optionValue);
            CimException.ThrowIfMiResultFailure(result);
        }

        /// <summary>
        /// Sets regular prompt user
        /// </summary>
        /// <param name="callbackMode"></param>
        /// <param name="automaticConfirmation"></param>
        public void SetPromptUserRegularMode(CimCallbackMode callbackMode, bool automaticConfirmation)
        {
            this.AssertNotDisposed();

            Native.MiResult result = Native.OperationOptionsMethods.SetPromptUserRegularMode(this.OperationOptionsHandleOnDemand,
                (Native.MiCallbackMode)callbackMode, automaticConfirmation);
            CimException.ThrowIfMiResultFailure(result);
        }        

        /// <summary>
        /// Sets a custom transport option
        /// </summary>
        /// <param name="optionName"></param>
        /// <param name="optionValue"></param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="optionName"/> is <c>null</c></exception>
        public void SetOption(string optionName, UInt32 optionValue)
        {
            if (string.IsNullOrWhiteSpace(optionName))
            {
                throw new ArgumentNullException("optionName");
            }
            this.AssertNotDisposed();

            Native.MiResult result = Native.OperationOptionsMethods.SetOption(this.OperationOptionsHandleOnDemand, optionName, optionValue);
            CimException.ThrowIfMiResultFailure(result);
        }

        #region PSSEMANTICS
        internal void WriteMessageCallbackInternal(
            Native.OperationCallbackProcessingContext callbackProcessingContext,
            Native.OperationHandle operationHandle,
            UInt32 channel,
            string message)
        {
            if (_writeMessageCallback != null)
            {
                var callbacksReceiverBase = (CimAsyncCallbacksReceiverBase) callbackProcessingContext.ManagedOperationContext;
                callbacksReceiverBase.CallIntoUserCallback(
                    callbackProcessingContext,
                    () => _writeMessageCallback(channel, message));
            }
        }

        private WriteMessageCallback _writeMessageCallback;

        private void WriteProgressCallbackInternal(
            Native.OperationCallbackProcessingContext callbackProcessingContext,
            Native.OperationHandle operationHandle,
            string activity,
            string currentOperation,
            string statusDescription,
            UInt32 percentageCompleted,
            UInt32 secondsRemaining)
        {
            if (_writeProgressCallback != null)
            {
                var callbacksReceiverBase = (CimAsyncCallbacksReceiverBase) callbackProcessingContext.ManagedOperationContext;
                callbacksReceiverBase.CallIntoUserCallback(
                    callbackProcessingContext,
                    () => _writeProgressCallback(activity, currentOperation, statusDescription, percentageCompleted, secondsRemaining));
            }
        }

        private WriteProgressCallback _writeProgressCallback;

        internal void WriteErrorCallbackInternal(
            Native.OperationCallbackProcessingContext callbackProcessingContext,
            Native.OperationHandle operationHandle,
            Native.InstanceHandle instanceHandle,
            out Native.MIResponseType response)
        {
            response = Native.MIResponseType.MIResponseTypeYes;
            if (_writeErrorCallback != null)
            {
                Debug.Assert(instanceHandle != null, "Caller should verify instance != null");
                CimInstance cimInstance = null;
                try
                {
                    if (!instanceHandle.IsInvalid)
                    {
                        cimInstance = new CimInstance(instanceHandle.Clone(), null);
                        var callbacksReceiverBase = (CimAsyncCallbacksReceiverBase) callbackProcessingContext.ManagedOperationContext;
                        CimResponseType userResponse = CimResponseType.None;
                        callbacksReceiverBase.CallIntoUserCallback(
                            callbackProcessingContext,
                            delegate { userResponse = _writeErrorCallback(cimInstance); });
                        response = (Native.MIResponseType) userResponse;
                    }
                }
                finally
                {
                    if (cimInstance != null)
                    {
                        cimInstance.Dispose();
                    }
                }
            }
        }

        private WriteErrorCallback _writeErrorCallback;

        internal void PromptUserCallbackInternal(
            Native.OperationCallbackProcessingContext callbackProcessingContext,
            Native.OperationHandle operationHandle,
            string message,
            Native.MiPromptType promptType,
            out Native.MIResponseType response)
        {
            response = Native.MIResponseType.MIResponseTypeYes;
            if (_promptUserCallback != null)
            {
                var callbacksReceiverBase = (CimAsyncCallbacksReceiverBase) callbackProcessingContext.ManagedOperationContext;
                CimResponseType userResponse = CimResponseType.None;
                callbacksReceiverBase.CallIntoUserCallback(
                    callbackProcessingContext,
                    delegate { userResponse = _promptUserCallback(message, (CimPromptType) promptType); });
                response = (Native.MIResponseType) userResponse;
            }
        }

        private PromptUserCallback _promptUserCallback;

        /// <summary>
        /// Set Write Error Mode
        /// </summary>
        /// <value></value>
        public CimCallbackMode WriteErrorMode
        {
            set
            {
                this.AssertNotDisposed();

                Native.MiResult result = Native.OperationOptionsMethods.SetWriteErrorModeOption(
                    this._operationOptionsHandle.Value, (Native.MiCallbackMode)value);
                CimException.ThrowIfMiResultFailure(result);
            }
            get
            {
                this.AssertNotDisposed();
                Native.MiCallbackMode mode;
                Native.MiResult result = Native.OperationOptionsMethods.GetWriteErrorModeOption(
                    this._operationOptionsHandle.Value, out mode);
                CimException.ThrowIfMiResultFailure(result);
                return (CimCallbackMode)mode;
            }
        }

        /// <summary>
        /// Set Prompt User Mode
        /// </summary>
        /// <value></value>
        public CimCallbackMode PromptUserMode
        {
            set
            {
                this.AssertNotDisposed();

                Native.MiResult result = Native.OperationOptionsMethods.SetPromptUserModeOption(
                    this._operationOptionsHandle.Value, (Native.MiCallbackMode)value);
                CimException.ThrowIfMiResultFailure(result);
            }
            get
            {
                this.AssertNotDisposed();
                Native.MiCallbackMode mode;
                Native.MiResult result = Native.OperationOptionsMethods.GetPromptUserModeOption(
                    this._operationOptionsHandle.Value, out mode);
                CimException.ThrowIfMiResultFailure(result);
                return (CimCallbackMode)mode;
            }
        }

        /// <summary>
        /// Sets WriteMessageCallback.
        /// </summary>
        /// <value></value>
        [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Justification = "See the justification on CimSessionOptions.Timeout")]
        public WriteMessageCallback WriteMessage
        {
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                this.AssertNotDisposed();
                _writeMessageCallback = value;
                OperationCallback.WriteMessageCallback = this.WriteMessageCallbackInternal;
            }
        }

        /// <summary>
        /// Sets WriteProgressCallback.
        /// </summary>
        /// <value></value>
        [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Justification = "See the justification on CimSessionOptions.Timeout")]
        public WriteProgressCallback WriteProgress
        {
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                this.AssertNotDisposed();
                _writeProgressCallback = value;
                OperationCallback.WriteProgressCallback = this.WriteProgressCallbackInternal;
            }
        }

        /// <summary>
        /// Sets WriteErrorCallback.
        /// </summary>
        /// <value></value>
        [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Justification = "See the justification on CimSessionOptions.Timeout")]
        public WriteErrorCallback WriteError
        {
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                this.AssertNotDisposed();
                _writeErrorCallback = value;
                OperationCallback.WriteErrorCallback = this.WriteErrorCallbackInternal;
            }
        }

        /// <summary>
        /// Sets PromptUserCallback.
        /// </summary>
        /// <value></value>
        [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Justification = "See the justification on CimSessionOptions.Timeout")]
        public PromptUserCallback PromptUser
        {
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                this.AssertNotDisposed();
                _promptUserCallback = value;
                OperationCallback.PromptUserCallback = this.PromptUserCallbackInternal;
            }
        }

        /// <summary>
        /// Enable the Channel for this particualr value.
        /// </summary>
        /// <param name="channelNumber"></param>
        public void EnableChannel(UInt32 channelNumber)
        {
            this.AssertNotDisposed();

            Native.MiResult result = Native.OperationOptionsMethods.SetEnableChannelOption(this._operationOptionsHandle.Value, channelNumber);
            CimException.ThrowIfMiResultFailure(result);
        }

        /// <summary>
        /// Disable the Channel for this particualr value.
        /// </summary>
        /// <param name="channelNumber"></param>
        public void DisableChannel(UInt32 channelNumber)
        {
            this.AssertNotDisposed();

            Native.MiResult result = Native.OperationOptionsMethods.SetDisableChannelOption(this._operationOptionsHandle.Value, channelNumber);
            CimException.ThrowIfMiResultFailure(result);
        }
#endregion

        /// <summary>
        /// Sets a custom server or CIM provider option
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="optionName"/> is <c>null</c></exception>
        public void SetCustomOption(string optionName, bool optionValue, bool mustComply)
        {
            this.SetCustomOption(optionName, optionValue, CimType.Boolean, mustComply);
        }

        /// <summary>
        /// Sets a custom server or CIM provider option
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="optionName"/> is <c>null</c></exception>
        public void SetCustomOption(string optionName, Byte optionValue, bool mustComply)
        {
            this.SetCustomOption(optionName, optionValue, CimType.UInt8, mustComply);
        }

        /// <summary>
        /// Sets a custom server or CIM provider option
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="optionName"/> is <c>null</c></exception>
        public void SetCustomOption(string optionName, SByte optionValue, bool mustComply)
        {
            this.SetCustomOption(optionName, optionValue, CimType.SInt8, mustComply);
        }

        /// <summary>
        /// Sets a custom server or CIM provider option
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="optionName"/> is <c>null</c></exception>
        public void SetCustomOption(string optionName, UInt16 optionValue, bool mustComply)
        {
            this.SetCustomOption(optionName, optionValue, CimType.UInt16, mustComply);
        }

        /// <summary>
        /// Sets a custom server or CIM provider option
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="optionName"/> is <c>null</c></exception>
        public void SetCustomOption(string optionName, Int16 optionValue, bool mustComply)
        {
            this.SetCustomOption(optionName, optionValue, CimType.SInt16, mustComply);
        }

        /// <summary>
        /// Sets a custom server or CIM provider option
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="optionName"/> is <c>null</c></exception>
        public void SetCustomOption(string optionName, UInt32 optionValue, bool mustComply)
        {
            this.SetCustomOption(optionName, optionValue, CimType.UInt32, mustComply);
        }

        /// <summary>
        /// Sets a custom server or CIM provider option
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="optionName"/> is <c>null</c></exception>
        public void SetCustomOption(string optionName, Int32 optionValue, bool mustComply)
        {
            this.SetCustomOption(optionName, optionValue, CimType.SInt32, mustComply);
        }

        /// <summary>
        /// Sets a custom server or CIM provider option
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="optionName"/> is <c>null</c></exception>
        public void SetCustomOption(string optionName, UInt64 optionValue, bool mustComply)
        {
            this.SetCustomOption(optionName, optionValue, CimType.UInt32, mustComply);
        }

        /// <summary>
        /// Sets a custom server or CIM provider option
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="optionName"/> is <c>null</c></exception>
        public void SetCustomOption(string optionName, Int64 optionValue, bool mustComply)
        {
            this.SetCustomOption(optionName, optionValue, CimType.SInt32, mustComply);
        }

        /// <summary>
        /// Sets a custom server or CIM provider option
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="optionName"/> is <c>null</c></exception>
        public void SetCustomOption(string optionName, Single optionValue, bool mustComply)
        {
            this.SetCustomOption(optionName, optionValue, CimType.Real32, mustComply);
        }

        /// <summary>
        /// Sets a custom server or CIM provider option
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="optionName"/> is <c>null</c></exception>
        public void SetCustomOption(string optionName, Double optionValue, bool mustComply)
        {
            this.SetCustomOption(optionName, optionValue, CimType.Real64, mustComply);
        }

        /// <summary>
        /// Sets a custom server or CIM provider option
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="optionName"/> is <c>null</c></exception>
        public void SetCustomOption(string optionName, char optionValue, bool mustComply)
        {
            this.SetCustomOption(optionName, optionValue, CimType.Char16, mustComply);
        }

        /// <summary>
        /// Sets a custom server or CIM provider option
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="optionName"/> or <paramref name="optionValue"/> is <c>null</c></exception>
        public void SetCustomOption(string optionName, string optionValue, bool mustComply)
        {
            this.SetCustomOption(optionName, optionValue, CimType.String, mustComply);
        }

        /// <summary>
        /// Sets a custom server or CIM provider option
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="optionName"/> is <c>null</c></exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="optionName"/> or <paramref name="optionValue"/> is <c>null</c></exception>
        public void SetCustomOption(string optionName, object optionValue, CimType cimType, bool mustComply)
        {
            if (string.IsNullOrWhiteSpace(optionName))
            {
                throw new ArgumentNullException("optionName");
            }
            if (optionValue == null)
            {
                throw new ArgumentNullException("optionValue");
            }
            this.AssertNotDisposed();

            object nativeLayerValue = CimInstance.ConvertToNativeLayer(optionValue);
            try
            {
                Native.InstanceMethods.ThrowIfMismatchedType(cimType.ToMiType(), nativeLayerValue);
            }
            catch (InvalidCastException e)
            {
                throw new ArgumentException(e.Message, "optionValue", e);
            }
            catch (FormatException e)
            {
                throw new ArgumentException(e.Message, "optionValue", e);
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException(e.Message, "optionValue", e);
            }

            Native.MiResult result = Native.OperationOptionsMethods.SetCustomOption(
                this.OperationOptionsHandleOnDemand, 
                optionName, 
                nativeLayerValue,
                cimType.ToMiType(), 
                mustComply);
            CimException.ThrowIfMiResultFailure(result);
        }

        /// <summary>
        /// CancellationToken that can be used to cancel the operation.
        /// </summary>
        public CancellationToken? CancellationToken { get; set; }

        /// <summary>
        /// Asks the operation to only return key properties (see <see cref="CimFlags.Key"/>) of resulting CIM instances.
        /// Example of a CIM operation that supports this option: <see cref="CimSession.EnumerateInstances(string, string)"/>.
        /// </summary>
        public bool KeysOnly { get; set; }

        /// <summary>
        /// Asks the operation to only return class name (see <see cref="CimFlags.Key"/>) of resulting CIM class.
        /// Example of a CIM operation that supports this option: <see cref="CimSession.EnumerateInstances(string, string)"/>.
        /// </summary>
        public bool ClassNamesOnly { get; set; }

        /// <summary>
        /// Operation flags.
        /// </summary>
        public CimOperationFlags Flags { get; set; }

        /// <summary>
        /// report operation started flag
        /// </summary>
        public bool ReportOperationStarted { get { return (Flags & CimOperationFlags.ReportOperationStarted) == CimOperationFlags.ReportOperationStarted; } }

        /// <summary>
        /// Enables streaming of method results. 
        /// See 
        /// <see cref="CimSession.InvokeMethodAsync(string, string, string, CimMethodParametersCollection)"/>,
        /// <see cref="CimSession.InvokeMethodAsync(string, CimInstance, string, CimMethodParametersCollection)"/>,
        /// <see cref="CimMethodStreamedResult" />,
        /// <see cref="CimMethodResult" />,
        /// <see cref="CimMethodResultBase" />.
        /// </summary> 
        public bool EnableMethodResultStreaming { get; set; }

        /// <summary>
        /// When <see cref="ShortenLifetimeOfResults"/> is set to <c>true</c>, then
        /// returned results (for example <see cref="CimInstance"/> objects) are 
        /// valid only for a short duration.  This can improve performance by allowing
        /// to avoid copying of data from the transport layer.
        /// 
        /// Shorter lifetime means that:
        /// - argument of IObserver.OnNext is disposed when OnNext returns
        /// - previous value of IEnumerator.Current is disposed after calling IEnumerator.MoveNext 
        /// - value of IEnumerator.Current is disposed after calling IEnumerator.Dispose
        /// </summary>
        public bool ShortenLifetimeOfResults { get; set; } 

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
            if (Interlocked.CompareExchange(ref this._disposed, 1, 0) == 0)
            {
                if (disposing)
                {
                    Native.OperationOptionsHandle tmpHandle = this.OperationOptionsHandle;
                    if (tmpHandle != null)
                    {
                        tmpHandle.Dispose();
                    }
                }
            }
        }

        public bool IsDisposed
        {
            get
            {
                return this._disposed == 1;
            }
        }
        
        internal void AssertNotDisposed()
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(this.ToString());
            }
        }

        private int _disposed;

        #endregion


        #region ICloneable Members

 #if(!_CORECLR)
      
        object ICloneable.Clone()
        {
            return new CimOperationOptions(this);
        }
#endif
        #endregion
    }
}

namespace Microsoft.Management.Infrastructure.Options.Internal
{
    internal static class OperationOptionsExtensionMethods
    {
        static internal Native.OperationCallbacks GetOperationCallbacks(this CimOperationOptions operationOptions)
        {
            var operationCallbacks = new Native.OperationCallbacks();
            if (operationOptions!= null)
            {
                    operationCallbacks.WriteErrorCallback = operationOptions.OperationCallback.WriteErrorCallback;
                    operationCallbacks.WriteMessageCallback = operationOptions.OperationCallback.WriteMessageCallback;
                    operationCallbacks.WriteProgressCallback = operationOptions.OperationCallback.WriteProgressCallback;
                    operationCallbacks.PromptUserCallback = operationOptions.OperationCallback.PromptUserCallback;
            }
            return operationCallbacks;
        }

        static internal Native.OperationCallbacks GetOperationCallbacks(
            this CimOperationOptions operationOptions, 
            CimAsyncCallbacksReceiverBase acceptCallbacksReceiver)
        {
            Native.OperationCallbacks operationCallbacks = operationOptions.GetOperationCallbacks();
            
            if (acceptCallbacksReceiver != null)
            {
                acceptCallbacksReceiver.RegisterAcceptedAsyncCallbacks(operationCallbacks, operationOptions);
            }

            return operationCallbacks;
        }

        static internal Native.MiOperationFlags GetOperationFlags(this CimOperationOptions operationOptions)
        {
            return operationOptions != null 
                       ? operationOptions.Flags.ToNative() 
                       : CimOperationFlags.None.ToNative();
        }

        static internal Native.OperationOptionsHandle GetOperationOptionsHandle(this CimOperationOptions operationOptions)
        {
            return operationOptions != null 
                       ? operationOptions.OperationOptionsHandle
                       : null;
        }

        static internal bool GetKeysOnly(this CimOperationOptions operationOptions)
        {
            return operationOptions != null && operationOptions.KeysOnly;
        }

        static internal bool GetClassNamesOnly(this CimOperationOptions operationOptions)
        {
            return operationOptions != null && operationOptions.ClassNamesOnly;
        }

        static internal CancellationToken? GetCancellationToken(this CimOperationOptions operationOptions)
        {
            return operationOptions != null 
                       ? operationOptions.CancellationToken 
                       : null;
        }

        static internal bool GetShortenLifetimeOfResults(this CimOperationOptions operationOptions)
        {
            return operationOptions != null 
                       ? operationOptions.ShortenLifetimeOfResults 
                       : false;
        }

        static internal bool GetReportOperationStarted(this CimOperationOptions operationOptions)
        {
            return operationOptions != null
                       ? operationOptions.ReportOperationStarted
                       : false;
        }
    }
}
