// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading;

using Debug = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class implements Get-SecureRandom cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "SecureRandom", DefaultParameterSetName = GetSecureRandomCommand.RandomNumberParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(int), typeof(long), typeof(double))]
    public class GetSecureRandomCommand : PSCmdlet
    {
        #region Parameter set handling

        internal const string RandomNumberParameterSet = "RandomNumberParameterSet";
        private const string RandomListItemParameterSet = "RandomListItemParameterSet";
        private const string ShuffleParameterSet = "ShuffleParameterSet";

        private static readonly object[] _nullInArray = new object[] { null };

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
                    if ((MyInvocation.ExpectingInput) && (Maximum == null) && (Minimum == null))
                    {
                        _effectiveParameterSet = MyParameterSet.RandomListItem;
                    }
                    else if (ParameterSetName == GetRandomCommand.RandomListItemParameterSet
                        || ParameterSetName == GetRandomCommand.ShuffleParameterSet)
                    {
                        _effectiveParameterSet = MyParameterSet.RandomListItem;
                    }
                    else if (ParameterSetName.Equals(GetRandomCommand.RandomNumberParameterSet, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((Maximum != null) && (Maximum.GetType().IsArray))
                        {
                            InputObject = (object[])Maximum;
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

        private void ThrowMinGreaterThanOrEqualMax(object minValue, object maxValue)
        {
            if (minValue == null)
            {
                throw PSTraceSource.NewArgumentNullException("min");
            }

            if (maxValue == null)
            {
                throw PSTraceSource.NewArgumentNullException("max");
            }

            ErrorRecord errorRecord = new(
                new ArgumentException(string.Format(
                    CultureInfo.InvariantCulture, GetRandomCommandStrings.MinGreaterThanOrEqualMax, minValue, maxValue)),
                "MinGreaterThanOrEqualMax",
                ErrorCategory.InvalidArgument,
                null);

            ThrowTerminatingError(errorRecord);
        }

        #endregion

        #region Random generator state

        private static readonly ReaderWriterLockSlim s_runspaceGeneratorMapLock = new();

        // 1-to-1 mapping of runspaces and random number generators
        private static readonly Dictionary<Guid, PolymorphicRandomNumberGenerator> s_runspaceGeneratorMap = new();

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
        /// Gets and sets generator associated with the current runspace.
        /// </summary>
        internal PolymorphicRandomNumberGenerator Generator
        {
            get
            {
                if (_generator == null)
                {
                    Guid runspaceId = Context.CurrentRunspace.InstanceId;

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
                        Generator = new PolymorphicRandomNumberGenerator();
                    }
                }

                return _generator;
            }

            set
            {
                _generator = value;
                Runspace myRunspace = Context.CurrentRunspace;

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

        #region Parameters for RandomNumberParameterSet

        /// <summary>
        /// Maximum number to generate.
        /// </summary>
        [Parameter(ParameterSetName = RandomNumberParameterSet, Position = 0)]
        public object Maximum { get; set; }

        /// <summary>
        /// Minimum number to generate.
        /// </summary>
        [Parameter(ParameterSetName = RandomNumberParameterSet)]
        public object Minimum { get; set; }

        private static bool IsInt(object o)
        {
            if (o == null || o is int)
            {
                return true;
            }

            return false;
        }

        private static bool IsInt64(object o)
        {
            if (o == null || o is long)
            {
                return true;
            }

            return false;
        }

        private static object ProcessOperand(object o)
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

        private static double ConvertToDouble(object o, double defaultIfNull)
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
        /// List from which random elements are chosen.
        /// </summary>
        [Parameter(ParameterSetName = RandomListItemParameterSet, ValueFromPipeline = true, Position = 0, Mandatory = true)]
        [Parameter(ParameterSetName = ShuffleParameterSet, ValueFromPipeline = true, Position = 0, Mandatory = true)]
        [System.Management.Automation.AllowNull]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public object[] InputObject { get; set; }

        /// <summary>
        /// Number of items to output (number of list items or of numbers).
        /// </summary>
        [Parameter(ParameterSetName = RandomNumberParameterSet)]
        [Parameter(ParameterSetName = RandomListItemParameterSet)]
        [ValidateRange(1, int.MaxValue)]
        public int Count { get; set; } = 1;

        #endregion

        #region Shuffle parameter

        /// <summary>
        /// Gets or sets whether the command should return all input objects in randomized order.
        /// </summary>
        [Parameter(ParameterSetName = ShuffleParameterSet, Mandatory = true)]
        public SwitchParameter Shuffle { get; set; }

        #endregion

        #region Cmdlet processing methods

        private double GetRandomDouble(double minValue, double maxValue)
        {
            double randomNumber;
            double diff = maxValue - minValue;

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
                    double r = Generator.NextDouble();
                    randomNumber = minValue + r * maxValue - r * minValue;
                }
                while (randomNumber >= maxValue);
            }
            else
            {
                do
                {
                    double r = Generator.NextDouble();
                    randomNumber = minValue + r * diff;
                    diff *= r;
                }
                while (randomNumber >= maxValue);
            }

            return randomNumber;
        }

        /// <summary>
        /// Get a random Int64 type number.
        /// </summary>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        private long GetRandomInt64(long minValue, long maxValue)
        {
            // Randomly generate eight bytes and convert the byte array to UInt64
            var buffer = new byte[sizeof(ulong)];
            ulong randomUint64;

            BigInteger bigIntegerDiff = (BigInteger)maxValue - (BigInteger)minValue;

            // When the difference is less than int.MaxValue, use Random.Next(int, int)
            if (bigIntegerDiff <= int.MaxValue)
            {
                int randomDiff = Generator.Next(0, (int)(maxValue - minValue));
                return minValue + randomDiff;
            }

            // The difference of two Int64 numbers would not exceed UInt64.MaxValue, so it can be represented by a UInt64 number.
            ulong uint64Diff = (ulong)bigIntegerDiff;

            // Calculate the number of bits to represent the diff in type UInt64
            int bitsToRepresentDiff = 0;
            ulong diffCopy = uint64Diff;
            for (; diffCopy != 0; bitsToRepresentDiff++)
            {
                diffCopy >>= 1;
            }
            // Get the mask for the number of bits
            ulong mask = (0xffffffffffffffff >> (64 - bitsToRepresentDiff));
            do
            {
                // Randomly fill the buffer
                Generator.NextBytes(buffer);
                randomUint64 = BitConverter.ToUInt64(buffer, 0);

                // Get the last 'bitsToRepresentDiff' number of random bits
                randomUint64 &= mask;
            } while (uint64Diff <= randomUint64);

            double randomNumber = minValue * 1.0 + randomUint64 * 1.0;
            return (long)randomNumber;
        }

        /// <summary>
        /// This method implements the BeginProcessing method for Get-SecureRandom command.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (EffectiveParameterSet == MyParameterSet.RandomNumber)
            {
                object maxOperand = ProcessOperand(Maximum);
                object minOperand = ProcessOperand(Minimum);

                if (IsInt(maxOperand) && IsInt(minOperand))
                {
                    int minValue = minOperand != null ? (int)minOperand : 0;
                    int maxValue = maxOperand != null ? (int)maxOperand : int.MaxValue;

                    if (minValue >= maxValue)
                    {
                        ThrowMinGreaterThanOrEqualMax(minValue, maxValue);
                    }

                    for (int i = 0; i < Count; i++)
                    {
                        int randomNumber = Generator.Next(minValue, maxValue);
                        Debug.Assert(minValue <= randomNumber, "lower bound <= random number");
                        Debug.Assert(randomNumber < maxValue, "random number < upper bound");

                        WriteObject(randomNumber);
                    }
                }
                else if ((IsInt64(maxOperand) || IsInt(maxOperand)) && (IsInt64(minOperand) || IsInt(minOperand)))
                {
                    long minValue = minOperand != null ? ((minOperand is long) ? (long)minOperand : (int)minOperand) : 0;
                    long maxValue = maxOperand != null ? ((maxOperand is long) ? (long)maxOperand : (int)maxOperand) : long.MaxValue;

                    if (minValue >= maxValue)
                    {
                        ThrowMinGreaterThanOrEqualMax(minValue, maxValue);
                    }

                    for (int i = 0; i < Count; i++)
                    {
                        long randomNumber = GetRandomInt64(minValue, maxValue);
                        Debug.Assert(minValue <= randomNumber, "lower bound <= random number");
                        Debug.Assert(randomNumber < maxValue, "random number < upper bound");

                        WriteObject(randomNumber);
                    }
                }
                else
                {
                    double minValue = (minOperand is double) ? (double)minOperand : ConvertToDouble(Minimum, 0.0);
                    double maxValue = (maxOperand is double) ? (double)maxOperand : ConvertToDouble(Maximum, double.MaxValue);

                    if (minValue >= maxValue)
                    {
                        ThrowMinGreaterThanOrEqualMax(minValue, maxValue);
                    }

                    for (int i = 0; i < Count; i++)
                    {
                        double randomNumber = GetRandomDouble(minValue, maxValue);
                        Debug.Assert(minValue <= randomNumber, "lower bound <= random number");
                        Debug.Assert(randomNumber < maxValue, "random number < upper bound");

                        WriteObject(randomNumber);
                    }
                }
            }
            else if (EffectiveParameterSet == MyParameterSet.RandomListItem)
            {
                _chosenListItems = new List<object>();
                _numberOfProcessedListItems = 0;
            }
        }

        // rough proof that when choosing random K items out of N items
        // each item has got K/N probability of being included in the final list
        //
        // probability that a particular item in chosenListItems is NOT going to be replaced
        // when processing I-th input item [assumes I > K]:
        // P_one_step(I) = 1 - ((K / I) * ((K - 1) / K) + ((I - K) / I) = (I - 1) / I
        //                      <--A-->   <-----B----->   <-----C----->
        // A - probability that I-th element is going to be replacing an element from chosenListItems
        //     (see (1) in the code below)
        // B - probability that a particular element from chosenListItems is NOT going to be replaced
        //     (see (2) in the code below)
        // C - probability that I-th element is NOT going to be replacing an element from chosenListItems
        //     (see (1) in the code below)
        //
        // probability that a particular item in chosenListItems is NOT going to be replaced
        // when processing input items J through N [assumes J > K]
        // P_removal(J) = Multiply(for I = J to N) P(I) =
        //              = ((J - 1) / J) * (J / (J + 1)) * ... * ((N - 2) / (N - 1)) * ((N - 1) / N) =
        //              = (J - 1) / N
        //
        // probability that when processing an element it is going to be put into chosenListItems
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
        /// This method implements the ProcessRecord method for Get-SecureRandom command.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (EffectiveParameterSet == MyParameterSet.RandomListItem)
            {
                if (ParameterSetName == ShuffleParameterSet)
                {
                    // this allows for $null to be in an array passed to InputObject
                    foreach (object item in InputObject ?? _nullInArray)
                    {
                        _chosenListItems.Add(item);
                    }
                }
                else
                {
                    foreach (object item in InputObject ?? _nullInArray)
                    {
                        // (3)
                        if (_numberOfProcessedListItems < Count)
                        {
                            Debug.Assert(_chosenListItems.Count == _numberOfProcessedListItems, "Initial K elements should all be included in chosenListItems");
                            _chosenListItems.Add(item);
                        }
                        else
                        {
                            Debug.Assert(_chosenListItems.Count == Count, "After processing K initial elements, the length of chosenItems should stay equal to K");

                            // (1)
                            if (Generator.Next(_numberOfProcessedListItems + 1) < Count)
                            {
                                // (2)
                                int indexToReplace = Generator.Next(_chosenListItems.Count);
                                _chosenListItems[indexToReplace] = item;
                            }
                        }

                        _numberOfProcessedListItems++;
                    }
                }
            }
        }

        /// <summary>
        /// This method implements the EndProcessing method for Get-SecureRandom command.
        /// </summary>
        protected override void EndProcessing()
        {
            if (EffectiveParameterSet == MyParameterSet.RandomListItem)
            {
                // make sure the order is truly random
                // (all permutations with the same probability)
                // O(n) time
                int n = _chosenListItems.Count;
                for (int i = 0; i < n; i++)
                {
                    // randomly choose j from [i...n)
                    int j = Generator.Next(i, n);

                    WriteObject(_chosenListItems[j]);

                    // remove the output object from consideration in the next iteration.
                    if (i != j)
                    {
                        _chosenListItems[j] = _chosenListItems[i];
                    }
                }
            }
        }

        #endregion Processing methods
    }

    /// <summary>
    /// Provides an adapter API for random numbers that may be either cryptographically random, or
    /// generated with the regular pseudo-random number generator. Re-implementations of
    /// methods using the NextBytes() primitive based on the CLR implementation:
    ///     https://referencesource.microsoft.com/#mscorlib/system/random.cs.
    /// </summary>
    internal class PolymorphicRandomNumberGenerator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PolymorphicRandomNumberGenerator"/> class.
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

        private readonly Random _pseudoGenerator = null;
        private readonly RandomNumberGenerator _cryptographicGenerator = null;

        /// <summary>
        /// Generates a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
        /// </summary>
        /// <returns>A random floating-point number that is greater than or equal to 0.0, and less than 1.0.</returns>
        internal double NextDouble()
        {
            // According to the CLR source:
            //     "Including this division at the end gives us significantly improved random number distribution."
            return Next() * (1.0 / int.MaxValue);
        }

        /// <summary>
        /// Generates a non-negative random integer.
        /// </summary>
        /// <returns>A non-negative random integer.</returns>
        internal int Next()
        {
            int randomNumber;

            // The CLR implementation just fudges
            // Int32.MaxValue down to (Int32.MaxValue - 1). This implementation
            // errs on the side of correctness.
            do
            {
                randomNumber = InternalSample();
            }
            while (randomNumber == int.MaxValue);

            if (randomNumber < 0)
            {
                randomNumber += int.MaxValue;
            }

            return randomNumber;
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
                throw new ArgumentOutOfRangeException(nameof(maxValue), GetRandomCommandStrings.MaxMustBeGreaterThanZeroApi);
            }

            return Next(0, maxValue);
        }

        /// <summary>
        /// Returns a random integer that is within a specified range.
        /// </summary>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number returned. maxValue must be greater than or equal to minValue.</param>
        /// <returns></returns>
        public int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(minValue), GetRandomCommandStrings.MinGreaterThanOrEqualMaxApi);
            }

            int randomNumber = 0;

            long range = (long)maxValue - (long)minValue;
            if (range <= int.MaxValue)
            {
                randomNumber = ((int)(NextDouble() * range) + minValue);
            }
            else
            {
                double largeSample = InternalSampleLargeRange() * (1.0 / (2 * ((uint)int.MaxValue)));
                randomNumber = (int)((long)(largeSample * range) + minValue);
            }

            return randomNumber;
        }

        /// <summary>
        /// Fills the elements of a specified array of bytes with random numbers.
        /// </summary>
        /// <param name="buffer">The array to be filled.</param>
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
        /// Samples a random integer.
        /// </summary>
        /// <returns>A random integer, using the full range of Int32.</returns>
        private int InternalSample()
        {
            int randomNumber;
            byte[] data = new byte[sizeof(int)];

            NextBytes(data);
            randomNumber = BitConverter.ToInt32(data, 0);

            return randomNumber;
        }

        /// <summary>
        /// Samples a random int when the range is large. This does
        /// not need to be in the range of -Double.MaxValue .. Double.MaxValue,
        /// just 0.. (2 * Int32.MaxValue) - 1 .
        /// </summary>
        /// <returns></returns>
        private double InternalSampleLargeRange()
        {
            double randomNumber;

            do
            {
                randomNumber = InternalSample();
            } while (randomNumber == int.MaxValue);

            randomNumber += int.MaxValue;
            return randomNumber;
        }
    }
}
