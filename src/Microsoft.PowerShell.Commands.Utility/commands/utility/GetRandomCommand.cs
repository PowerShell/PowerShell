/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Numerics;
using System.Threading;
using Debug = System.Management.Automation.Diagnostics;
using System.Security.Cryptography;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class implements get-random cmdlet.
    /// </summary>
    /// <!-- author: LukaszA -->
    [Cmdlet(VerbsCommon.Get, "Random", DefaultParameterSetName = GetRandomCommand.RandomNumberParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113446", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(Int32), typeof(Int64), typeof(Double))]
    public class GetRandomCommand : PSCmdlet
    {
        #region Parameter set handling

        private const string RandomNumberParameterSet = "RandomNumberParameterSet";
        private const string RandomListItemParameterSet = "RandomListItemParameterSet";

        private enum MyParameterSet
        {
            Unknown,
            RandomNumber,
            RandomListItem
        }

        private MyParameterSet _effectiveParameterSet;

        private MyParameterSet EffectiveParameterSet
        {
            get
            {
                // cache MyParameterSet enum instead of doing string comparison every time
                if (_effectiveParameterSet == MyParameterSet.Unknown)
                {
                    if ((this.MyInvocation.ExpectingInput) && (this.Maximum == null) && (this.Minimum == null))
                    {
                        _effectiveParameterSet = MyParameterSet.RandomListItem;
                    }
                    else if (ParameterSetName.Equals(GetRandomCommand.RandomListItemParameterSet, StringComparison.OrdinalIgnoreCase))
                    {
                        _effectiveParameterSet = MyParameterSet.RandomListItem;
                    }
                    else if (this.ParameterSetName.Equals(GetRandomCommand.RandomNumberParameterSet, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((this.Maximum != null) && (this.Maximum.GetType().IsArray))
                        {
                            this.InputObject = (object[])this.Maximum;
                            _effectiveParameterSet = MyParameterSet.RandomListItem;
                        }
                        else
                        {
                            _effectiveParameterSet = MyParameterSet.RandomNumber;
                        }
                    }
                    else
                    {
                        Debug.Assert(false, "Unrecognized parameter set");
                    }
                }

                return _effectiveParameterSet;
            }
        }

        #endregion Parameter set handling

        #region Error handling

        private void ThrowMinGreaterThanOrEqualMax(object min, object max)
        {
            if (min == null)
            {
                throw PSTraceSource.NewArgumentNullException("min");
            }

            if (max == null)
            {
                throw PSTraceSource.NewArgumentNullException("max");
            }

            ErrorRecord errorRecord = new ErrorRecord(
                new ArgumentException(String.Format(
                    CultureInfo.InvariantCulture, GetRandomCommandStrings.MinGreaterThanOrEqualMax, min, max)),
                "MinGreaterThanOrEqualMax",
                ErrorCategory.InvalidArgument,
                null);

            this.ThrowTerminatingError(errorRecord);
        }

        #endregion

        #region Random generator state

        private static ReaderWriterLockSlim s_runspaceGeneratorMapLock = new ReaderWriterLockSlim();

        // 1-to-1 mapping of runspaces and random number generators
        private static Dictionary<Guid, PolymorphicRandomNumberGenerator> s_runspaceGeneratorMap = new Dictionary<Guid, PolymorphicRandomNumberGenerator>();

        private static void CurrentRunspace_StateChanged(object sender, RunspaceStateEventArgs e)
        {
            switch (e.RunspaceStateInfo.State)
            {
                case RunspaceState.Broken:
                case RunspaceState.Closed:
                    try
                    {
                        GetRandomCommand.s_runspaceGeneratorMapLock.EnterWriteLock();
                        GetRandomCommand.s_runspaceGeneratorMap.Remove(((Runspace)sender).InstanceId);
                    }
                    finally
                    {
                        GetRandomCommand.s_runspaceGeneratorMapLock.ExitWriteLock();
                    }
                    break;
            }
        }

        private PolymorphicRandomNumberGenerator _generator;

        /// <summary>
        /// Gets and sets generator associated with the current runspace
        /// </summary>
        private PolymorphicRandomNumberGenerator Generator
        {
            get
            {
                if (_generator == null)
                {
                    Guid runspaceId = this.Context.CurrentRunspace.InstanceId;

                    bool needToInitialize = false;
                    try
                    {
                        GetRandomCommand.s_runspaceGeneratorMapLock.EnterReadLock();
                        needToInitialize = !GetRandomCommand.s_runspaceGeneratorMap.TryGetValue(runspaceId, out _generator);
                    }
                    finally
                    {
                        GetRandomCommand.s_runspaceGeneratorMapLock.ExitReadLock();
                    }

                    if (needToInitialize)
                    {
                        this.Generator = new PolymorphicRandomNumberGenerator();
                    }
                }

                return _generator;
            }
            set
            {
                _generator = value;
                Runspace myRunspace = this.Context.CurrentRunspace;

                try
                {
                    GetRandomCommand.s_runspaceGeneratorMapLock.EnterWriteLock();
                    if (!GetRandomCommand.s_runspaceGeneratorMap.ContainsKey(myRunspace.InstanceId))
                    {
                        // make sure we won't leave the generator around after runspace exits
                        myRunspace.StateChanged += CurrentRunspace_StateChanged;
                    }
                    GetRandomCommand.s_runspaceGeneratorMap[myRunspace.InstanceId] = _generator;
                }
                finally
                {
                    GetRandomCommand.s_runspaceGeneratorMapLock.ExitWriteLock();
                }
            }
        }

        #endregion

        #region Common parameters

        /// <summary>
        /// Seed used to reinitialize random numbers generator
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        public int? SetSeed { get; set; }

        #endregion Common parameters

        #region Parameters for RandomNumberParameterSet

        /// <summary>
        /// Maximum number to generate
        /// </summary>
        [Parameter(ParameterSetName = RandomNumberParameterSet, Position = 0)]
        public object Maximum { get; set; }

        /// <summary>
        /// Minimum number to generate
        /// </summary>
        [Parameter(ParameterSetName = RandomNumberParameterSet)]
        public object Minimum { get; set; }

        private bool IsInt(object o)
        {
            if (o == null || o is int)
            {
                return true;
            }
            return false;
        }

        private bool IsInt64(object o)
        {
            if (o == null || o is Int64)
            {
                return true;
            }
            return false;
        }

        private object ProcessOperand(object o)
        {
            if (o == null)
            {
                return null;
            }

            PSObject pso = PSObject.AsPSObject(o);
            object baseObject = pso.BaseObject;

            if (baseObject is string)
            {
                // The type argument passed in does not decide the number type we want to convert to. ScanNumber will return
                // int/long/double based on the string form number passed in.
                baseObject = System.Management.Automation.Language.Parser.ScanNumber((string)baseObject, typeof(int));
            }

            return baseObject;
        }

        private double ConvertToDouble(object o, double defaultIfNull)
        {
            if (o == null)
            {
                return defaultIfNull;
            }

            double result = (double)LanguagePrimitives.ConvertTo(o, typeof(double), CultureInfo.InvariantCulture);
            return result;
        }

        #endregion

        #region Parameters and variables for RandomListItemParameterSet

        private List<object> _chosenListItems;
        private int _numberOfProcessedListItems;

        /// <summary>
        /// List from which random elements are chosen
        /// </summary>
        [Parameter(ParameterSetName = RandomListItemParameterSet, ValueFromPipeline = true, Position = 0, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public object[] InputObject { get; set; }

        /// <summary>
        /// Number of items to output (number of list items or of numbers)
        /// </summary>
        [Parameter(ParameterSetName = GetRandomCommand.RandomListItemParameterSet)]
        [ValidateRange(1, int.MaxValue)]
        public int Count { get; set; }

        #endregion

        #region Cmdlet processing methods

        private double GetRandomDouble(double min, double max)
        {
            double randomNumber;
            double diff = max - min;

            // I couldn't find a better fix for bug #216893 then
            // to test and retry if a random number falls outside the bounds
            // because of floating-point-arithmetic inaccuracies.
            //
            // Performance in the normal case is not impacted much.
            // In low-precision situations we should converge to a solution quickly
            // (diff gets smaller at a quick pace).

            if (double.IsInfinity(diff))
            {
                do
                {
                    double r = this.Generator.NextDouble();
                    randomNumber = min + r * max - r * min;
                }
                while (randomNumber >= max);
            }
            else
            {
                do
                {
                    double r = this.Generator.NextDouble();
                    randomNumber = min + r * diff;
                    diff = diff * r;
                }
                while (randomNumber >= max);
            }

            return randomNumber;
        }

        /// <summary>
        /// Get a random Int64 type number
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        private Int64 GetRandomInt64(Int64 min, Int64 max)
        {
            // Randomly generate eight bytes and convert the byte array to UInt64
            var buffer = new byte[sizeof(UInt64)];
            UInt64 randomUint64;

            BigInteger bigIntegerDiff = (BigInteger)max - (BigInteger)min;

            // When the difference is less than int.MaxValue, use Random.Next(int, int)
            if (bigIntegerDiff <= int.MaxValue)
            {
                int randomDiff = this.Generator.Next(0, (int)(max - min));
                return min + randomDiff;
            }

            // The difference of two Int64 numbers would not exceed UInt64.MaxValue, so it can be represented by a UInt64 number.
            UInt64 uint64Diff = (UInt64)bigIntegerDiff;

            // Calculate the number of bits to represent the diff in type UInt64
            int bitsToRepresentDiff = 0;
            UInt64 diffCopy = uint64Diff;
            for (; diffCopy != 0; bitsToRepresentDiff++)
            {
                diffCopy >>= 1;
            }
            // Get the mask for the number of bits
            UInt64 mask = (0xffffffffffffffff >> (64 - bitsToRepresentDiff));
            do
            {
                // Randomly fill the buffer
                this.Generator.NextBytes(buffer);
                randomUint64 = BitConverter.ToUInt64(buffer, 0);
                // Get the last 'bitsToRepresentDiff' number of randon bits
                randomUint64 &= mask;
            } while (uint64Diff <= randomUint64);

            double result = min * 1.0 + randomUint64 * 1.0;
            return (Int64)result;
        }

        /// <summary>
        /// This method implements the BeginProcessing method for get-random command
        /// </summary>
        protected override void BeginProcessing()
        {
            if (this.SetSeed.HasValue)
            {
                this.Generator = new PolymorphicRandomNumberGenerator(this.SetSeed.Value);
            }

            if (this.EffectiveParameterSet == MyParameterSet.RandomNumber)
            {
                object maxOperand = ProcessOperand(this.Maximum);
                object minOperand = ProcessOperand(this.Minimum);

                if (IsInt(maxOperand) && IsInt(minOperand))
                {
                    int min = minOperand != null ? (int)minOperand : 0;
                    int max = maxOperand != null ? (int)maxOperand : int.MaxValue;

                    if (min >= max)
                    {
                        this.ThrowMinGreaterThanOrEqualMax(min, max);
                    }

                    int randomNumber = this.Generator.Next(min, max);
                    Debug.Assert(min <= randomNumber, "lower bound <= random number");
                    Debug.Assert(randomNumber < max, "random number < upper bound");

                    this.WriteObject(randomNumber);
                }
                else if ((IsInt64(maxOperand) || IsInt(maxOperand)) && (IsInt64(minOperand) || IsInt(minOperand)))
                {
                    Int64 min = minOperand != null ? ((minOperand is Int64) ? (Int64)minOperand : (int)minOperand) : 0;
                    Int64 max = maxOperand != null ? ((maxOperand is Int64) ? (Int64)maxOperand : (int)maxOperand) : Int64.MaxValue;

                    if (min >= max)
                    {
                        this.ThrowMinGreaterThanOrEqualMax(min, max);
                    }

                    Int64 randomNumber = this.GetRandomInt64(min, max);
                    Debug.Assert(min <= randomNumber, "lower bound <= random number");
                    Debug.Assert(randomNumber < max, "random number < upper bound");

                    this.WriteObject(randomNumber);
                }
                else
                {
                    double min = (minOperand is double) ? (double)minOperand : this.ConvertToDouble(this.Minimum, 0.0);
                    double max = (maxOperand is double) ? (double)maxOperand : this.ConvertToDouble(this.Maximum, double.MaxValue);

                    if (min >= max)
                    {
                        this.ThrowMinGreaterThanOrEqualMax(min, max);
                    }

                    double randomNumber = this.GetRandomDouble(min, max);
                    Debug.Assert(min <= randomNumber, "lower bound <= random number");
                    Debug.Assert(randomNumber < max, "random number < upper bound");

                    this.WriteObject(randomNumber);
                }
            }
            else if (this.EffectiveParameterSet == MyParameterSet.RandomListItem)
            {
                _chosenListItems = new List<object>();
                _numberOfProcessedListItems = 0;

                if (this.Count == 0) // -Count not specified
                {
                    this.Count = 1; // default to one random item by default
                }
            }
        }

        // rough proof that when choosing random K items out of N items
        // each item has got K/N probability of being included in the final list
        //
        // probability that a particular item in this.chosenListItems is NOT going to be replaced
        // when processing I-th input item [assumes I > K]:
        // P_one_step(I) = 1 - ((K / I) * ((K - 1) / K) + ((I - K) / I) = (I - 1) / I
        //                      <--A-->   <-----B----->   <-----C----->
        // A - probability that I-th element is going to be replacing an element from this.chosenListItems
        //     (see (1) in the code below)
        // B - probability that a particular element from this.chosenListItems is NOT going to be replaced
        //     (see (2) in the code below)
        // C - probability that I-th element is NOT going to be replacing an element from this.chosenListItems
        //     (see (1) in the code below)
        //
        // probability that a particular item in this.chosenListItems is NOT going to be replaced
        // when processing input items J through N [assumes J > K]
        // P_removal(J) = Multiply(for I = J to N) P(I) =
        //              = ((J - 1) / J) * (J / (J + 1)) * ... * ((N - 2) / (N - 1)) * ((N - 1) / N) =
        //              = (J - 1) / N
        //
        // probability that when processing an element it is going to be put into this.chosenListItems
        // P_insertion(I) = 1.0 when I <= K - see (3) in the code below
        // P_insertion(I) = K/N otherwise - see (1) in the code below
        //
        // probability that a given element is going to be a part of the final list
        // P_final(I)   = P_insertion(I) * P_removal(max(I + 1, K + 1))
        // [for I <= K] = 1.0 * ((K + 1) - 1) / N = K / N
        // [otherwise]  = (K / I) * ((I + 1) - 1) / N = K / N
        //
        // which proves that P_final(I) = K / N for all values of I.  QED.

        /// <summary>
        /// This method implements the ProcessRecord method for get-random command
        /// </summary>
        protected override void ProcessRecord()
        {
            if (this.EffectiveParameterSet == MyParameterSet.RandomListItem)
            {
                foreach (object item in this.InputObject)
                {
                    if (_numberOfProcessedListItems < this.Count) // (3)
                    {
                        Debug.Assert(_chosenListItems.Count == _numberOfProcessedListItems, "Initial K elements should all be included in this.chosenListItems");
                        _chosenListItems.Add(item);
                    }
                    else
                    {
                        Debug.Assert(_chosenListItems.Count == this.Count, "After processing K initial elements, the length of this.chosenItems should stay equal to K");
                        if (this.Generator.Next(_numberOfProcessedListItems + 1) < this.Count) // (1)
                        {
                            int indexToReplace = this.Generator.Next(_chosenListItems.Count); // (2)
                            _chosenListItems[indexToReplace] = item;
                        }
                    }

                    _numberOfProcessedListItems++;
                }
            }
        }

        /// <summary>
        /// This method implements the EndProcessing method for get-random command
        /// </summary>
        protected override void EndProcessing()
        {
            if (this.EffectiveParameterSet == MyParameterSet.RandomListItem)
            {
                // make sure the order is truly random
                // (all permutations with the same probability)
                // O(n) time
                int n = _chosenListItems.Count;
                for (int i = 0; i < n; i++)
                {
                    // randomly choose an item to go into the i-th position
                    int j = this.Generator.Next(i, n);

                    // swap j-th item into i-th position
                    if (i != j)
                    {
                        object tmp = _chosenListItems[i];
                        _chosenListItems[i] = _chosenListItems[j];
                        _chosenListItems[j] = tmp;
                    }
                }

                // output all items
                foreach (object chosenItem in _chosenListItems)
                {
                    this.WriteObject(chosenItem);
                }
            }
        }

        #endregion Processing methods
    }

    /// <summary>
    /// Provides an adapter API for random numbers that may be either cryptographically random, or
    /// generated with the regular pseudo-random number generator. Re-implementations of
    /// methods using the NextBytes() primitive based on the CLR implementation:
    ///     http://referencesource.microsoft.com/#mscorlib/system/random.cs
    /// </summary>
    internal class PolymorphicRandomNumberGenerator
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public PolymorphicRandomNumberGenerator()
        {
            _cryptographicGenerator = RandomNumberGenerator.Create();
            _pseudoGenerator = null;
        }

        internal PolymorphicRandomNumberGenerator(int seed)
        {
            _cryptographicGenerator = null;
            _pseudoGenerator = new Random(seed);
        }

        private Random _pseudoGenerator = null;
        private RandomNumberGenerator _cryptographicGenerator = null;

        /// <summary>
        /// Generates a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
        /// </summary>
        /// <returns>A random floating-point number that is greater than or equal to 0.0, and less than 1.0</returns>
        internal double NextDouble()
        {
            // According to the CLR source:
            //     "Including this division at the end gives us significantly improved random number distribution."
            return Next() * (1.0 / Int32.MaxValue);
        }

        /// <summary>
        /// Generates a non-negative random integer.
        /// </summary>
        /// <returns>A non-negative random integer.</returns>
        internal int Next()
        {
            int result;

            // The CLR implementation just fudges
            // Int32.MaxValue down to (Int32.MaxValue - 1). This implementation
            // errs on the side of correctness.
            do
            {
                result = InternalSample();
            }
            while (result == Int32.MaxValue);

            if (result < 0)
            {
                result += Int32.MaxValue;
            }

            return result;
        }

        /// <summary>
        /// Returns a random integer that is within a specified range.
        /// </summary>
        /// <param name="maxValue">The exclusive upper bound of the random number returned.</param>
        /// <returns></returns>
        internal int Next(int maxValue)
        {
            if (maxValue < 0)
            {
                throw new ArgumentOutOfRangeException("maxValue", GetRandomCommandStrings.MaxMustBeGreaterThanZeroApi);
            }

            return Next(0, maxValue);
        }

        /// <summary>
        /// Returns a random integer that is within a specified range.
        /// </summary>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number returned. maxValue must be greater than or equal to minValue</param>
        /// <returns></returns>
        public int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
            {
                throw new ArgumentOutOfRangeException("minValue", GetRandomCommandStrings.MinGreaterThanOrEqualMaxApi);
            }

            long range = (long)maxValue - (long)minValue;
            if (range <= int.MaxValue)
            {
                return ((int)(NextDouble() * range) + minValue);
            }
            else
            {
                double largeSample = InternalSampleLargeRange() * (1.0 / (2 * ((uint)Int32.MaxValue)));
                int result = (int)((long)(largeSample * range) + minValue);

                return result;
            }
        }

        /// <summary>
        /// Fills the elements of a specified array of bytes with random numbers.
        /// </summary>
        /// <param name="buffer">The array to be filled</param>
        internal void NextBytes(byte[] buffer)
        {
            if (_cryptographicGenerator != null)
            {
                _cryptographicGenerator.GetBytes(buffer);
            }
            else
            {
                _pseudoGenerator.NextBytes(buffer);
            }
        }

        /// <summary>
        /// Samples a random integer
        /// </summary>
        /// <returns>A random integer, using the full range of Int32</returns>
        private int InternalSample()
        {
            int result;
            byte[] data = new byte[sizeof(int)];

            NextBytes(data);
            result = BitConverter.ToInt32(data, 0);

            return result;
        }

        /// <summary>
        /// Samples a random int when the range is large. This does
        /// not need to be in the range of -Double.MaxValue .. Double.MaxValue,
        /// just 0.. (2 * Int32.MaxValue) - 1
        /// </summary>
        /// <returns></returns>
        private double InternalSampleLargeRange()
        {
            double result;

            do
            {
                result = InternalSample();
            } while (result == Int32.MaxValue);

            result += Int32.MaxValue;
            return result;
        }
    }
}
