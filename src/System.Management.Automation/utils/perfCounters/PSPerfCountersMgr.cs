// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation.Tracing;

namespace System.Management.Automation.PerformanceData
{
    /// <summary>
    /// Powershell Performance Counters Manager class shall provide a mechanism
    /// for components using SYstem.Management.Automation assembly to register
    /// performance counters with Performance Counters subsystem.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    public class PSPerfCountersMgr
    {
        #region Private Members
        private static PSPerfCountersMgr s_PSPerfCountersMgrInstance;
        private ConcurrentDictionary<Guid, CounterSetInstanceBase> _CounterSetIdToInstanceMapping;
        private ConcurrentDictionary<string, Guid> _CounterSetNameToIdMapping;
        private readonly PowerShellTraceSource _tracer = PowerShellTraceSourceFactory.GetTraceSource();

        #region Constructors

        private PSPerfCountersMgr()
        {
            _CounterSetIdToInstanceMapping = new ConcurrentDictionary<Guid, CounterSetInstanceBase>();
            _CounterSetNameToIdMapping = new ConcurrentDictionary<string, Guid>();
        }

        #endregion

        #endregion

        #region Destructor

        /// <summary>
        /// Destructor which will trigger the cleanup of internal data structures and
        /// disposal of counter set instances.
        /// </summary>
        ~PSPerfCountersMgr()
        {
            RemoveAllCounterSets();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Getter method to retrieve the singleton instance of the PSPerfCountersMgr.
        /// </summary>
        public static PSPerfCountersMgr Instance
        {
            get { return s_PSPerfCountersMgrInstance ?? (s_PSPerfCountersMgrInstance = new PSPerfCountersMgr()); }
        }

        /// <summary>
        /// Helper method to generate an instance name for a counter set.
        /// </summary>
        public string GetCounterSetInstanceName()
        {
            Process currentProcess = Process.GetCurrentProcess();
            string pid = string.Create(CultureInfo.InvariantCulture, $"{currentProcess.Id}");
            return pid;
        }

        /// <summary>
        /// Method to determine whether the counter set given by 'counterSetName' is
        /// registered with the system. If true, then counterSetId is populated.
        /// </summary>
        public bool IsCounterSetRegistered(string counterSetName, out Guid counterSetId)
        {
            counterSetId = new Guid();
            if (counterSetName == null)
            {
                ArgumentNullException argNullException = new ArgumentNullException("counterSetName");
                _tracer.TraceException(argNullException);
                return false;
            }

            return _CounterSetNameToIdMapping.TryGetValue(counterSetName, out counterSetId);
        }

        /// <summary>
        /// Method to determine whether the counter set given by 'counterSetId' is
        /// registered with the system. If true, then CounterSetInstance is populated.
        /// </summary>
        public bool IsCounterSetRegistered(Guid counterSetId, out CounterSetInstanceBase counterSetInst)
        {
            return _CounterSetIdToInstanceMapping.TryGetValue(counterSetId, out counterSetInst);
        }

        /// <summary>
        /// Method to register a counter set with the Performance Counters Manager.
        /// </summary>
        public bool AddCounterSetInstance(CounterSetRegistrarBase counterSetRegistrarInstance)
        {
            if (counterSetRegistrarInstance == null)
            {
                ArgumentNullException argNullException = new ArgumentNullException("counterSetRegistrarInstance");
                _tracer.TraceException(argNullException);
                return false;
            }

            Guid counterSetId = counterSetRegistrarInstance.CounterSetId;
            string counterSetName = counterSetRegistrarInstance.CounterSetName;
            CounterSetInstanceBase counterSetInst = null;

            if (this.IsCounterSetRegistered(counterSetId, out counterSetInst))
            {
                InvalidOperationException invalidOperationException = new InvalidOperationException(
                    string.Format(
                    CultureInfo.InvariantCulture,
                    "A Counter Set Instance with id '{0}' is already registered",
                    counterSetId));
                _tracer.TraceException(invalidOperationException);
                return false;
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(counterSetName))
                {
                    Guid retrievedCounterSetId;
                    // verify that there doesn't exist another counter set with the same name
                    if (this.IsCounterSetRegistered(counterSetName, out retrievedCounterSetId))
                    {
                        InvalidOperationException invalidOperationException =
                            new InvalidOperationException(
                                string.Format(
                                CultureInfo.InvariantCulture,
                                "A Counter Set Instance with name '{0}' is already registered",
                                counterSetName));
                        _tracer.TraceException(invalidOperationException);
                        return false;
                    }

                    _CounterSetNameToIdMapping.TryAdd(counterSetName, counterSetId);
                }

                _CounterSetIdToInstanceMapping.TryAdd(
                    counterSetId,
                    counterSetRegistrarInstance.CounterSetInstance);
            }
            catch (OverflowException overflowException)
            {
                _tracer.TraceException(overflowException);
                return false;
            }

            return true;
        }

        /// <summary>
        /// If IsNumerator is true, then updates the numerator component
        /// of target counter 'counterId' in Counter Set 'counterSetId'
        /// by 'stepAmount'.
        /// Otherwise, updates the denominator component by 'stepAmount'.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public bool UpdateCounterByValue(
            Guid counterSetId,
            int counterId,
            long stepAmount = 1,
            bool isNumerator = true)
        {
            CounterSetInstanceBase counterSetInst = null;
            if (this.IsCounterSetRegistered(counterSetId, out counterSetInst))
            {
                return counterSetInst.UpdateCounterByValue(counterId, stepAmount, isNumerator);
            }
            else
            {
                InvalidOperationException invalidOperationException =
                    new InvalidOperationException(
                        string.Format(
                        CultureInfo.InvariantCulture,
                        "No Counter Set Instance with id '{0}' is registered",
                        counterSetId));
                _tracer.TraceException(invalidOperationException);
                return false;
            }
        }

        /// <summary>
        /// If IsNumerator is true, then updates the numerator component
        /// of target counter 'counterName' in Counter Set 'counterSetId'
        /// by 'stepAmount'.
        /// Otherwise, updates the denominator component by 'stepAmount'.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public bool UpdateCounterByValue(
            Guid counterSetId,
            string counterName,
            long stepAmount = 1,
            bool isNumerator = true)
        {
            CounterSetInstanceBase counterSetInst = null;
            if (this.IsCounterSetRegistered(counterSetId, out counterSetInst))
            {
                return counterSetInst.UpdateCounterByValue(counterName, stepAmount, isNumerator);
            }
            else
            {
                InvalidOperationException invalidOperationException =
                    new InvalidOperationException(
                        string.Format(
                        CultureInfo.InvariantCulture,
                        "No Counter Set Instance with id '{0}' is registered",
                        counterSetId));
                _tracer.TraceException(invalidOperationException);
                return false;
            }
        }

        /// <summary>
        /// If IsNumerator is true, then updates the numerator component
        /// of target counter 'counterId' in Counter Set 'counterSetName'
        /// by 'stepAmount'.
        /// Otherwise, updates the denominator component by 'stepAmount'.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public bool UpdateCounterByValue(
            string counterSetName,
            int counterId,
            long stepAmount = 1,
            bool isNumerator = true)
        {
            if (counterSetName == null)
            {
                ArgumentNullException argNullException =
                    new ArgumentNullException("counterSetName");
                _tracer.TraceException(argNullException);
                return false;
            }

            Guid counterSetId;
            if (this.IsCounterSetRegistered(counterSetName, out counterSetId))
            {
                CounterSetInstanceBase counterSetInst = _CounterSetIdToInstanceMapping[counterSetId];
                return counterSetInst.UpdateCounterByValue(counterId, stepAmount, isNumerator);
            }
            else
            {
                InvalidOperationException invalidOperationException =
                    new InvalidOperationException(
                        string.Format(
                        CultureInfo.InvariantCulture,
                        "No Counter Set Instance with id '{0}' is registered",
                         counterSetId));
                _tracer.TraceException(invalidOperationException);

                return false;
            }
        }

        /// <summary>
        /// If IsNumerator is true, then updates the numerator component
        /// of target counter 'counterName' in Counter Set 'counterSetName'
        /// by 'stepAmount'.
        /// Otherwise, updates the denominator component by 'stepAmount'.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public bool UpdateCounterByValue(
            string counterSetName,
            string counterName,
            long stepAmount = 1,
            bool isNumerator = true)
        {
            Guid counterSetId;
            if (counterSetName == null)
            {
                ArgumentNullException argNullException =
                    new ArgumentNullException("counterSetName");
                _tracer.TraceException(argNullException);
                return false;
            }

            if (this.IsCounterSetRegistered(counterSetName, out counterSetId))
            {
                CounterSetInstanceBase counterSetInst = _CounterSetIdToInstanceMapping[counterSetId];
                return counterSetInst.UpdateCounterByValue(counterName, stepAmount, isNumerator);
            }
            else
            {
                InvalidOperationException invalidOperationException =
                    new InvalidOperationException(
                        string.Format(
                        CultureInfo.InvariantCulture,
                        "No Counter Set Instance with name {0} is registered",
                        counterSetName));
                _tracer.TraceException(invalidOperationException);
                return false;
            }
        }

        /// <summary>
        /// If IsNumerator is true, then sets the numerator component
        /// of target counter 'counterId' in Counter Set 'counterSetId'
        /// to 'counterValue'.
        /// Otherwise, updates the denominator component to 'counterValue'.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public bool SetCounterValue(
            Guid counterSetId,
            int counterId,
            long counterValue = 1,
            bool isNumerator = true)
        {
            CounterSetInstanceBase counterSetInst = null;
            if (this.IsCounterSetRegistered(counterSetId, out counterSetInst))
            {
                return counterSetInst.SetCounterValue(counterId, counterValue, isNumerator);
            }
            else
            {
                InvalidOperationException invalidOperationException =
                    new InvalidOperationException(
                        string.Format(
                        CultureInfo.InvariantCulture,
                        "No Counter Set Instance with id '{0}' is registered",
                        counterSetId));
                _tracer.TraceException(invalidOperationException);
                return false;
            }
        }

        /// <summary>
        /// If IsNumerator is true, then sets the numerator component
        /// of target counter 'counterName' in Counter Set 'counterSetId'
        /// to 'counterValue'.
        /// Otherwise, updates the denominator component to 'counterValue'.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public bool SetCounterValue(
            Guid counterSetId,
            string counterName,
            long counterValue = 1,
            bool isNumerator = true)
        {
            CounterSetInstanceBase counterSetInst = null;
            if (this.IsCounterSetRegistered(counterSetId, out counterSetInst))
            {
                return counterSetInst.SetCounterValue(counterName, counterValue, isNumerator);
            }
            else
            {
                InvalidOperationException invalidOperationException =
                 new InvalidOperationException(
                    string.Format(
                    CultureInfo.InvariantCulture,
                    "No Counter Set Instance with id '{0}' is registered",
                    counterSetId));
                _tracer.TraceException(invalidOperationException);
                return false;
            }
        }

        /// <summary>
        /// If IsNumerator is true, then sets the numerator component
        /// of target counter 'counterId' in Counter Set 'counterSetName'
        /// to 'counterValue'.
        /// Otherwise, updates the denominator component to 'counterValue'.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public bool SetCounterValue(
            string counterSetName,
            int counterId,
            long counterValue = 1,
            bool isNumerator = true)
        {
            if (counterSetName == null)
            {
                ArgumentNullException argNullException =
                    new ArgumentNullException("counterSetName");
                _tracer.TraceException(argNullException);
                return false;
            }

            Guid counterSetId;
            if (this.IsCounterSetRegistered(counterSetName, out counterSetId))
            {
                CounterSetInstanceBase counterSetInst = _CounterSetIdToInstanceMapping[counterSetId];
                return counterSetInst.SetCounterValue(counterId, counterValue, isNumerator);
            }
            else
            {
                InvalidOperationException invalidOperationException =
                    new InvalidOperationException(
                    string.Format(
                    CultureInfo.InvariantCulture,
                    "No Counter Set Instance with name '{0}' is registered",
                    counterSetName));
                _tracer.TraceException(invalidOperationException);
                return false;
            }
        }

        /// <summary>
        /// If IsNumerator is true, then sets the numerator component
        /// of target counter 'counterName' in Counter Set 'counterSetName'
        /// to 'counterValue'.
        /// Otherwise, updates the denominator component to 'counterValue'.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public bool SetCounterValue(
            string counterSetName,
            string counterName,
            long counterValue = 1,
            bool isNumerator = true)
        {
            if (counterSetName == null)
            {
                ArgumentNullException argNullException =
                    new ArgumentNullException("counterSetName");
                _tracer.TraceException(argNullException);
                return false;
            }

            Guid counterSetId;
            if (this.IsCounterSetRegistered(counterSetName, out counterSetId))
            {
                CounterSetInstanceBase counterSetInst = _CounterSetIdToInstanceMapping[counterSetId];
                return counterSetInst.SetCounterValue(counterName, counterValue, isNumerator);
            }
            else
            {
                InvalidOperationException invalidOperationException =
                    new InvalidOperationException(
                        string.Format(
                        CultureInfo.InvariantCulture,
                        "No Counter Set Instance with name '{0}' is registered",
                        counterSetName));
                _tracer.TraceException(invalidOperationException);
                return false;
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// NOTE: This method is provided solely for testing purposes.
        /// </summary>
        internal void RemoveAllCounterSets()
        {
            ICollection<Guid> counterSetIdKeys = _CounterSetIdToInstanceMapping.Keys;
            foreach (Guid counterSetId in counterSetIdKeys)
            {
                CounterSetInstanceBase currentCounterSetInstance = _CounterSetIdToInstanceMapping[counterSetId];
                currentCounterSetInstance.Dispose();
            }

            _CounterSetIdToInstanceMapping.Clear();
            _CounterSetNameToIdMapping.Clear();
        }

        #endregion
    }
}
