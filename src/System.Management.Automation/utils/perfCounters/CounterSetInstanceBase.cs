// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.PerformanceData;
using System.Globalization;
using System.Management.Automation.Tracing;

namespace System.Management.Automation.PerformanceData
{
    /// <summary>
    /// An abstract class that forms the base class for any Counter Set type.
    /// A Counter Set Instance is required to register a given performance counter category
    /// with PSPerfCountersMgr.
    /// </summary>
    public abstract class CounterSetInstanceBase : IDisposable
    {
        private readonly PowerShellTraceSource _tracer = PowerShellTraceSourceFactory.GetTraceSource();
        #region Protected Members

        /// <summary>
        /// An instance of counterSetRegistrarBase type encapsulates all the information
        /// about a counter set and its associated counters.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        protected CounterSetRegistrarBase _counterSetRegistrarBase;

        // NOTE: Check whether the following dictionaries need to be concurrent
        // because there would be only 1 thread creating the instance,
        // and that instance would then be shared by multiple threads for data access.
        // Those threads won't modify/manipulate the dictionary, but they would only access it.

        /// <summary>
        /// Dictionary mapping counter name to id.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        protected ConcurrentDictionary<string, int> _counterNameToIdMapping;
        /// <summary>
        /// Dictionary mapping counter id to counter type.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        protected ConcurrentDictionary<int, CounterType> _counterIdToTypeMapping;

        #region Constructors
        /// <summary>
        /// Constructor.
        /// </summary>
        protected CounterSetInstanceBase(CounterSetRegistrarBase counterSetRegistrarInst)
        {
            this._counterSetRegistrarBase = counterSetRegistrarInst;
            _counterNameToIdMapping = new ConcurrentDictionary<string, int>();
            _counterIdToTypeMapping = new ConcurrentDictionary<int, CounterType>();

            CounterInfo[] counterInfoArray = this._counterSetRegistrarBase.CounterInfoArray;

            for (int i = 0; i < counterInfoArray.Length; i++)
            {
                this._counterIdToTypeMapping.TryAdd(counterInfoArray[i].Id, counterInfoArray[i].Type);
                if (!string.IsNullOrWhiteSpace(counterInfoArray[i].Name))
                {
                    this._counterNameToIdMapping.TryAdd(counterInfoArray[i].Name, counterInfoArray[i].Id);
                }
            }
        }

        #endregion

        /// <summary>
        /// Method that retrieves the target counter id.
        /// NOTE: If isNumerator is true, then input counter id is returned.
        /// But, if isNumerator is false, then a check is made on the input
        /// counter's type to ensure that denominator is indeed value for such a counter.
        /// </summary>
        protected bool RetrieveTargetCounterIdIfValid(int counterId, bool isNumerator, out int targetCounterId)
        {
            targetCounterId = counterId;
            if (isNumerator == false)
            {
                bool isDenominatorValid = false;
                CounterType counterType = this._counterIdToTypeMapping[counterId];
                switch (counterType)
                {
                    case CounterType.MultiTimerPercentageActive:
                    case CounterType.MultiTimerPercentageActive100Ns:
                    case CounterType.MultiTimerPercentageNotActive:
                    case CounterType.MultiTimerPercentageNotActive100Ns:
                    case CounterType.RawFraction32:
                    case CounterType.RawFraction64:
                    case CounterType.SampleFraction:
                    case CounterType.AverageCount64:
                    case CounterType.AverageTimer32:
                        isDenominatorValid = true;
                        break;
                }

                if (isDenominatorValid == false)
                {
                    InvalidOperationException invalidOperationException =
                        new InvalidOperationException(
                            string.Format(
                            CultureInfo.InvariantCulture,
                            "Denominator for update not valid for the given counter id {0}",
                            counterId));
                    _tracer.TraceException(invalidOperationException);
                    return false;
                }

                targetCounterId = counterId + 1;
            }

            return true;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// If isNumerator is true, then updates the numerator component
        /// of target counter 'counterId' by a value given by 'stepAmount'.
        /// Otherwise, updates the denominator component by 'stepAmount'.
        /// </summary>
        public abstract bool UpdateCounterByValue(
            int counterId,
            long stepAmount,
            bool isNumerator);

        /// <summary>
        /// If isNumerator is true, then updates the numerator component
        /// of target counter 'counterName' by a value given by 'stepAmount'.
        /// Otherwise, updates the denominator component by 'stepAmount'.
        /// </summary>
        public abstract bool UpdateCounterByValue(
            string counterName,
            long stepAmount,
            bool isNumerator);

        /// <summary>
        /// If isNumerator is true, then sets the numerator component of target
        /// counter 'counterId' to 'counterValue'.
        /// Otherwise, sets the denominator component to 'counterValue'.
        /// </summary>
        public abstract bool SetCounterValue(
            int counterId,
            long counterValue,
            bool isNumerator);

        /// <summary>
        /// If isNumerator is true, then sets the numerator component of target
        /// Counter 'counterName' to 'counterValue'.
        /// Otherwise, sets the denominator component to 'counterValue'.
        /// </summary>
        public abstract bool SetCounterValue(
            string counterName,
            long counterValue,
            bool isNumerator);

        /// <summary>
        /// This method retrieves the counter value associated with counter 'counterId'
        /// based on isNumerator parameter.
        /// </summary>
        public abstract bool GetCounterValue(int counterId, bool isNumerator, out long counterValue);

        /// <summary>
        /// This method retrieves the counter value associated with counter 'counterName'
        /// based on isNumerator parameter.
        /// </summary>
        public abstract bool GetCounterValue(string counterName, bool isNumerator, out long counterValue);

        /// <summary>
        /// An abstract method that will be implemented by the derived type
        /// so as to dispose the appropriate counter set instance.
        /// </summary>
        public abstract void Dispose();

        /*
        /// <summary>
        /// Resets the target counter 'counterId' to 0. If the given
        /// counter has both numerator and denominator components, then
        /// they both are set to 0.
        /// </summary>
        public bool ResetCounter(
            int counterId)
        {
            this.SetCounterValue(counterId, 0, true);
            this.SetCounterValue(counterId, 0, false);
        }

        /// <summary>
        /// Resets the target counter 'counterName' to 0. If the given
        /// counter has both numerator and denominator components, then
        /// they both are set to 0.
        /// </summary>
        public void ResetCounter(string counterName)
        {
            this.SetCounterValue(counterName, 0, true);
            this.SetCounterValue(counterName, 0, false);
        }
        */
        #endregion
    }

    /// <summary>
    /// PSCounterSetInstance is a thin wrapper
    /// on System.Diagnostics.PerformanceData.CounterSetInstance.
    /// </summary>
    public class PSCounterSetInstance : CounterSetInstanceBase
    {
        #region Private Members
        private bool _Disposed;
        private CounterSet _CounterSet;
        private CounterSetInstance _CounterSetInstance;
        private readonly PowerShellTraceSource _tracer = PowerShellTraceSourceFactory.GetTraceSource();

        #region Private Methods

        private void CreateCounterSetInstance()
        {
            _CounterSet =
                new CounterSet(
                    base._counterSetRegistrarBase.ProviderId,
                    base._counterSetRegistrarBase.CounterSetId,
                    base._counterSetRegistrarBase.CounterSetInstType);

            // Add the counters to the counter set definition.
            foreach (CounterInfo counterInfo in base._counterSetRegistrarBase.CounterInfoArray)
            {
                if (counterInfo.Name == null)
                {
                    _CounterSet.AddCounter(counterInfo.Id, counterInfo.Type);
                }
                else
                {
                    _CounterSet.AddCounter(counterInfo.Id, counterInfo.Type, counterInfo.Name);
                }
            }

            string instanceName = PSPerfCountersMgr.Instance.GetCounterSetInstanceName();
            // Create an instance of the counter set (contains the counter data).
            _CounterSetInstance = _CounterSet.CreateCounterSetInstance(instanceName);
        }

        private void UpdateCounterByValue(CounterData TargetCounterData, long stepAmount)
        {
            Debug.Assert(TargetCounterData != null);
            if (stepAmount == -1)
            {
                TargetCounterData.Decrement();
            }
            else if (stepAmount == 1)
            {
                TargetCounterData.Increment();
            }
            else
            {
                TargetCounterData.IncrementBy(stepAmount);
            }
        }

        #endregion

        #endregion

        #region Constructors
        /// <summary>
        /// Constructor for creating an instance of PSCounterSetInstance.
        /// </summary>
        public PSCounterSetInstance(CounterSetRegistrarBase counterSetRegBaseObj)
            : base(counterSetRegBaseObj)
        {
            CreateCounterSetInstance();
        }

        #endregion

        #region Destructor

        /// <summary>
        /// This destructor will run only if the Dispose method
        /// does not get called.
        /// It gives the base class opportunity to finalize.
        /// </summary>
        ~PSCounterSetInstance()
        {
            Dispose(false);
        }

        #endregion

        #region Protected Methods
        /// <summary>
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// If disposing equals true, the method has been called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        /// If disposing equals false, the method has been called by the
        /// runtime from inside the finalizer and you should not reference
        /// other objects. Only unmanaged resources can be disposed.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!_Disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    _CounterSetInstance.Dispose();
                    _CounterSet.Dispose();
                }
                // Note disposing has been done.
                _Disposed = true;
            }
        }

        #endregion

        #region IDisposable Overrides
        /// <summary>
        /// Dispose Method implementation for IDisposable interface.
        /// </summary>
        public override void Dispose()
        {
            this.Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SuppressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        #endregion

        #region CounterSetInstanceBase Overrides

        #region Public Methods

        /// <summary>
        /// If isNumerator is true, then updates the numerator component
        /// of target counter 'counterId' by a value given by 'stepAmount'.
        /// Otherwise, updates the denominator component by 'stepAmount'.
        /// </summary>
        public override bool UpdateCounterByValue(int counterId, long stepAmount, bool isNumerator)
        {
            if (_Disposed)
            {
                ObjectDisposedException objectDisposedException =
                    new ObjectDisposedException("PSCounterSetInstance");
                _tracer.TraceException(objectDisposedException);
                return false;
            }

            int targetCounterId;
            if (base.RetrieveTargetCounterIdIfValid(counterId, isNumerator, out targetCounterId))
            {
                CounterData targetCounterData = _CounterSetInstance.Counters[targetCounterId];
                if (targetCounterData != null)
                {
                    this.UpdateCounterByValue(targetCounterData, stepAmount);
                    return true;
                }
                else
                {
                    InvalidOperationException invalidOperationException =
                    new InvalidOperationException(
                        string.Format(
                        CultureInfo.InvariantCulture,
                        "Lookup for counter corresponding to counter id {0} failed",
                        counterId));
                    _tracer.TraceException(invalidOperationException);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// If isNumerator is true, then updates the numerator component
        /// of target counter 'counterName' by a value given by 'stepAmount'.
        /// Otherwise, updates the denominator component by 'stepAmount'.
        /// </summary>
        public override bool UpdateCounterByValue(string counterName, long stepAmount, bool isNumerator)
        {
            if (_Disposed)
            {
                ObjectDisposedException objectDisposedException =
                    new ObjectDisposedException("PSCounterSetInstance");
                _tracer.TraceException(objectDisposedException);
                return false;
            }
            // retrieve counter id associated with the counter name
            if (counterName == null)
            {
                ArgumentNullException argNullException = new ArgumentNullException(nameof(counterName));
                _tracer.TraceException(argNullException);
                return false;
            }

            try
            {
                int targetCounterId = this._counterNameToIdMapping[counterName];
                return this.UpdateCounterByValue(targetCounterId, stepAmount, isNumerator);
            }
            catch (KeyNotFoundException)
            {
                InvalidOperationException invalidOperationException =
                    new InvalidOperationException(
                    string.Format(
                    CultureInfo.InvariantCulture,
                    "Lookup for counter corresponding to counter name {0} failed",
                    counterName));
                _tracer.TraceException(invalidOperationException);
                return false;
            }
        }

        /// <summary>
        /// If isNumerator is true, then sets the numerator component
        /// of target counter 'counterId' to 'counterValue'.
        /// Otherwise, sets the denominator component to 'counterValue'.
        /// </summary>
        public override bool SetCounterValue(int counterId, long counterValue, bool isNumerator)
        {
            if (_Disposed)
            {
                ObjectDisposedException objectDisposedException =
                    new ObjectDisposedException("PSCounterSetInstance");
                _tracer.TraceException(objectDisposedException);
                return false;
            }

            int targetCounterId;
            if (base.RetrieveTargetCounterIdIfValid(counterId, isNumerator, out targetCounterId))
            {
                CounterData targetCounterData = _CounterSetInstance.Counters[targetCounterId];

                if (targetCounterData != null)
                {
                    targetCounterData.Value = counterValue;
                    return true;
                }
                else
                {
                    InvalidOperationException invalidOperationException =
                        new InvalidOperationException(
                            string.Format(
                            CultureInfo.InvariantCulture,
                            "Lookup for counter corresponding to counter id {0} failed",
                            counterId));
                    _tracer.TraceException(invalidOperationException);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// If isNumerator is true, then updates the numerator component
        /// of target counter 'counterName' by a value given by 'counterValue'.
        /// Otherwise, sets the denominator component to 'counterValue'.
        /// </summary>
        public override bool SetCounterValue(string counterName, long counterValue, bool isNumerator)
        {
            if (_Disposed)
            {
                ObjectDisposedException objectDisposedException =
                    new ObjectDisposedException("PSCounterSetInstance");
                _tracer.TraceException(objectDisposedException);
                return false;
            }

            // retrieve counter id associated with the counter name
            if (counterName == null)
            {
                ArgumentNullException argNullException = new ArgumentNullException(nameof(counterName));
                _tracer.TraceException(argNullException);
                return false;
            }

            try
            {
                int targetCounterId = this._counterNameToIdMapping[counterName];
                return this.SetCounterValue(targetCounterId, counterValue, isNumerator);
            }
            catch (KeyNotFoundException)
            {
                InvalidOperationException invalidOperationException =
                    new InvalidOperationException(
                    string.Format(
                    CultureInfo.InvariantCulture,
                    "Lookup for counter corresponding to counter name {0} failed",
                    counterName));
                _tracer.TraceException(invalidOperationException);
                return false;
            }
        }

        /// <summary>
        /// This method retrieves the counter value associated with counter 'counterId'
        /// based on isNumerator parameter.
        /// </summary>
        public override bool GetCounterValue(int counterId, bool isNumerator, out long counterValue)
        {
            counterValue = -1;
            if (_Disposed)
            {
                ObjectDisposedException objectDisposedException =
                    new ObjectDisposedException("PSCounterSetInstance");
                _tracer.TraceException(objectDisposedException);
                return false;
            }

            int targetCounterId;
            if (base.RetrieveTargetCounterIdIfValid(counterId, isNumerator, out targetCounterId))
            {
                CounterData targetCounterData = _CounterSetInstance.Counters[targetCounterId];

                if (targetCounterData != null)
                {
                    counterValue = targetCounterData.Value;
                    return true;
                }
                else
                {
                    InvalidOperationException invalidOperationException =
                        new InvalidOperationException(
                            string.Format(
                            CultureInfo.InvariantCulture,
                            "Lookup for counter corresponding to counter id {0} failed",
                            counterId));
                    _tracer.TraceException(invalidOperationException);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// This method retrieves the counter value associated with counter 'counterName'
        /// based on isNumerator parameter.
        /// </summary>
        public override bool GetCounterValue(string counterName, bool isNumerator, out long counterValue)
        {
            counterValue = -1;
            if (_Disposed)
            {
                ObjectDisposedException objectDisposedException =
                    new ObjectDisposedException("PSCounterSetInstance");
                _tracer.TraceException(objectDisposedException);
                return false;
            }

            // retrieve counter id associated with the counter name
            if (counterName == null)
            {
                ArgumentNullException argNullException = new ArgumentNullException(nameof(counterName));
                _tracer.TraceException(argNullException);
                return false;
            }

            try
            {
                int targetCounterId = this._counterNameToIdMapping[counterName];
                return this.GetCounterValue(targetCounterId, isNumerator, out counterValue);
            }
            catch (KeyNotFoundException)
            {
                InvalidOperationException invalidOperationException =
                    new InvalidOperationException(
                        string.Format(
                        CultureInfo.InvariantCulture,
                        "Lookup for counter corresponding to counter name {0} failed",
                        counterName));
                _tracer.TraceException(invalidOperationException);
                return false;
            }
        }

        #endregion
        #endregion
    }
}
