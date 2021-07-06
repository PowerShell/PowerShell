// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.PerformanceData;

namespace System.Management.Automation.PerformanceData
{
    /// <summary>
    /// A struct that encapsulates the information pertaining to a given counter
    /// like name,type and id.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
    public struct CounterInfo
    {
        #region Private Members

        #endregion

        #region Constructors
        /// <summary>
        /// Constructor.
        /// </summary>
        public CounterInfo(int id, CounterType type, string name)
        {
            Id = id;
            Type = type;
            Name = name;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public CounterInfo(int id, CounterType type)
        {
            Id = id;
            Type = type;
            Name = null;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Getter for Counter Name property.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Getter for Counter Id property.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Getter for Counter Type property.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public CounterType Type { get; }

        #endregion
    }

    /// <summary>
    /// An abstract class that forms the base class for any CounterSetRegistrar type.
    /// Any client that needs to register a new type of perf counter category with the
    /// PSPerfCountersMgr, should create an instance of CounterSetRegistrarBase's
    /// derived non-abstract type.
    /// The created instance is then passed to PSPerfCounterMgr's AddCounterSetInstance()
    /// method.
    /// </summary>
    public abstract class CounterSetRegistrarBase
    {
        #region Private Members

        #endregion

        #region Protected Members
        /// <summary>
        /// A reference to the encapsulated counter set instance.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        protected CounterSetInstanceBase _counterSetInstanceBase;

        /// <summary>
        /// Method that creates an instance of the CounterSetInstanceBase's derived type.
        /// This method is invoked by the PSPerfCountersMgr to retrieve the appropriate
        /// instance of CounterSet to register with its internal datastructure.
        /// </summary>
        protected abstract CounterSetInstanceBase CreateCounterSetInstance();

        #endregion

        #region Constructors
        /// <summary>
        /// Constructor that creates an instance of CounterSetRegistrarBase derived type
        /// based on Provider Id, counterSetId, counterSetInstanceType, a collection
        /// with counters information and an optional counterSetName.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        protected CounterSetRegistrarBase(
            Guid providerId,
            Guid counterSetId,
            CounterSetInstanceType counterSetInstType,
            CounterInfo[] counterInfoArray,
            string counterSetName = null)
        {
            ProviderId = providerId;
            CounterSetId = counterSetId;
            CounterSetInstType = counterSetInstType;
            CounterSetName = counterSetName;
            if ((counterInfoArray == null)
                || (counterInfoArray.Length == 0))
            {
                throw new ArgumentNullException(nameof(counterInfoArray));
            }

            CounterInfoArray = new CounterInfo[counterInfoArray.Length];

            for (int i = 0; i < counterInfoArray.Length; i++)
            {
                CounterInfoArray[i] =
                    new CounterInfo(
                        counterInfoArray[i].Id,
                        counterInfoArray[i].Type,
                        counterInfoArray[i].Name
                        );
            }

            this._counterSetInstanceBase = null;
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        protected CounterSetRegistrarBase(
            CounterSetRegistrarBase srcCounterSetRegistrarBase)
        {
            if (srcCounterSetRegistrarBase == null)
            {
                throw new ArgumentNullException(nameof(srcCounterSetRegistrarBase));
            }

            ProviderId = srcCounterSetRegistrarBase.ProviderId;
            CounterSetId = srcCounterSetRegistrarBase.CounterSetId;
            CounterSetInstType = srcCounterSetRegistrarBase.CounterSetInstType;
            CounterSetName = srcCounterSetRegistrarBase.CounterSetName;

            CounterInfo[] counterInfoArrayRef = srcCounterSetRegistrarBase.CounterInfoArray;
            CounterInfoArray = new CounterInfo[counterInfoArrayRef.Length];

            for (int i = 0; i < counterInfoArrayRef.Length; i++)
            {
                CounterInfoArray[i] =
                    new CounterInfo(
                        counterInfoArrayRef[i].Id,
                        counterInfoArrayRef[i].Type,
                        counterInfoArrayRef[i].Name);
            }
        }
        #endregion

        #region Properties

        /// <summary>
        /// Getter method for ProviderId property.
        /// </summary>
        public Guid ProviderId { get; }

        /// <summary>
        /// Getter method for CounterSetId property.
        /// </summary>
        public Guid CounterSetId { get; }

        /// <summary>
        /// Getter method for CounterSetName property.
        /// </summary>
        public string CounterSetName { get; }

        /// <summary>
        /// Getter method for CounterSetInstanceType property.
        /// </summary>
        public CounterSetInstanceType CounterSetInstType { get; }

        /// <summary>
        /// Getter method for array of counters information property.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public CounterInfo[] CounterInfoArray { get; }

        /// <summary>
        /// Getter method that returns an instance of the CounterSetInstanceBase's
        /// derived type.
        /// </summary>
        public CounterSetInstanceBase CounterSetInstance
        {
            get { return _counterSetInstanceBase ?? (_counterSetInstanceBase = CreateCounterSetInstance()); }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Method that disposes the referenced instance of the CounterSetInstanceBase's derived type.
        /// This method is invoked by the PSPerfCountersMgr to dispose the appropriate
        /// instance of CounterSet from its internal datastructure as part of PSPerfCountersMgr
        /// cleanup procedure.
        /// </summary>
        public abstract void DisposeCounterSetInstance();

        #endregion
    }

    /// <summary>
    /// PSCounterSetRegistrar implements the abstract methods of CounterSetRegistrarBase.
    /// Any client that needs to register a new type of perf counter category with the
    /// PSPerfCountersMgr, should create an instance of PSCounterSetRegistrar.
    /// The created instance is then passed to PSPerfCounterMgr's AddCounterSetInstance()
    /// method.
    /// </summary>
    public class PSCounterSetRegistrar : CounterSetRegistrarBase
    {
        #region Constructors
        /// <summary>
        /// Constructor that creates an instance of PSCounterSetRegistrar.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public PSCounterSetRegistrar(
            Guid providerId,
            Guid counterSetId,
            CounterSetInstanceType counterSetInstType,
            CounterInfo[] counterInfoArray,
            string counterSetName = null)
            : base(providerId, counterSetId, counterSetInstType, counterInfoArray, counterSetName)
        {
        }

        /// <summary>
        /// Copy Constructor.
        /// </summary>
        public PSCounterSetRegistrar(
            PSCounterSetRegistrar srcPSCounterSetRegistrar)
            : base(srcPSCounterSetRegistrar)
        {
            if (srcPSCounterSetRegistrar == null)
            {
                throw new ArgumentNullException(nameof(srcPSCounterSetRegistrar));
            }
        }

        #endregion

        #region CounterSetRegistrarBase Overrides

        #region Protected Methods

        /// <summary>
        /// Method that creates an instance of the CounterSetInstanceBase's derived type.
        /// </summary>
        protected override CounterSetInstanceBase CreateCounterSetInstance()
        {
            return new PSCounterSetInstance(this);
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Method that disposes the referenced instance of the CounterSetInstanceBase's derived type.
        /// </summary>
        public override void DisposeCounterSetInstance()
        {
            base._counterSetInstanceBase.Dispose();
        }

        #endregion

        #endregion
    }
}
