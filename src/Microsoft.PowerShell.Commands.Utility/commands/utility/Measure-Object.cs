// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Class output by Measure-Object.
    /// </summary>
    public abstract class MeasureInfo
    {
        /// <summary>
        /// Property name.
        /// </summary>
        public string Property { get; set; }
    }

    /// <summary>
    /// Class output by Measure-Object.
    /// </summary>
    public sealed class GenericMeasureInfo : MeasureInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenericMeasureInfo"/> class.
        /// </summary>
        public GenericMeasureInfo()
        {
            Average = Sum = Maximum = Minimum = StandardDeviation = null;
        }

        /// <summary>
        /// Keeping track of number of objects with a certain property.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// The average of property values.
        /// </summary>
        public double? Average { get; set; }

        /// <summary>
        /// The sum of property values.
        /// </summary>
        public double? Sum { get; set; }

        /// <summary>
        /// The max of property values.
        /// </summary>
        public double? Maximum { get; set; }

        /// <summary>
        /// The min of property values.
        /// </summary>
        public double? Minimum { get; set; }

        /// <summary>
        /// The Standard Deviation of property values.
        /// </summary>
        public double? StandardDeviation { get; set; }
    }

    /// <summary>
    /// Class output by Measure-Object.
    /// </summary>
    /// <remarks>
    /// This class is created to make 'Measure-Object -MAX -MIN' work with ANYTHING that supports 'CompareTo'.
    /// GenericMeasureInfo class is shipped with PowerShell V2. Fixing this bug requires, changing the type of
    /// Maximum and Minimum properties which would be a breaking change. Hence created a new class to not
    /// have an appcompat issues with PS V2.
    /// </remarks>
    public sealed class GenericObjectMeasureInfo : MeasureInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenericObjectMeasureInfo"/> class.
        /// Default ctor.
        /// </summary>
        public GenericObjectMeasureInfo()
        {
            Average = Sum = StandardDeviation = null;
            Maximum = Minimum = null;
        }

        /// <summary>
        /// Keeping track of number of objects with a certain property.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// The average of property values.
        /// </summary>
        public double? Average { get; set; }

        /// <summary>
        /// The sum of property values.
        /// </summary>
        public double? Sum { get; set; }

        /// <summary>
        /// The max of property values.
        /// </summary>
        public object Maximum { get; set; }

        /// <summary>
        /// The min of property values.
        /// </summary>
        public object Minimum { get; set; }

        /// <summary>
        /// The Standard Deviation of property values.
        /// </summary>
        public double? StandardDeviation { get; set; }
    }

    /// <summary>
    /// Class output by Measure-Object.
    /// </summary>
    public sealed class TextMeasureInfo : MeasureInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextMeasureInfo"/> class.
        /// Default ctor.
        /// </summary>
        public TextMeasureInfo()
        {
            Lines = Words = Characters = null;
        }

        /// <summary>
        /// Keeping track of number of objects with a certain property.
        /// </summary>
        public int? Lines { get; set; }

        /// <summary>
        /// The average of property values.
        /// </summary>
        public int? Words { get; set; }

        /// <summary>
        /// The sum of property values.
        /// </summary>
        public int? Characters { get; set; }
    }

    /// <summary>
    /// Measure object cmdlet.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Measure, "Object", DefaultParameterSetName = GenericParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096617", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(GenericMeasureInfo), typeof(TextMeasureInfo), typeof(GenericObjectMeasureInfo))]
    public sealed class MeasureObjectCommand : PSCmdlet
    {
        /// <summary>
        /// Dictionary to be used by Measure-Object implementation.
        /// Keys are strings. Keys are compared with OrdinalIgnoreCase.
        /// </summary>
        /// <typeparam name="TValue">Value type.</typeparam>
        private sealed class MeasureObjectDictionary<TValue> : Dictionary<string, TValue>
            where TValue : new()
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="MeasureObjectDictionary{TValue}"/> class.
            /// Default ctor.
            /// </summary>
            internal MeasureObjectDictionary() : base(StringComparer.OrdinalIgnoreCase)
            {
            }

            /// <summary>
            /// Attempt to look up the value associated with the
            /// the specified key. If a value is not found, associate
            /// the key with a new value created via the value type's
            /// default constructor.
            /// </summary>
            /// <param name="key">The key to look up.</param>
            /// <returns>
            /// The existing value, or a newly-created value.
            /// </returns>
            public TValue EnsureEntry(string key)
            {
                TValue val;
                if (!TryGetValue(key, out val))
                {
                    val = new TValue();
                    this[key] = val;
                }

                return val;
            }
        }

        /// <summary>
        /// Convenience class to track statistics without having
        /// to maintain two sets of MeasureInfo and constantly checking
        /// what mode we're in.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
        private sealed class Statistics
        {
            // Common properties
            internal int count = 0;

            // Generic/Numeric statistics
            internal double sum = 0.0;
            internal double sumPrevious = 0.0;
            internal double variance = 0.0;
            internal object max = null;
            internal object min = null;

            // Text statistics
            internal int characters = 0;
            internal int words = 0;
            internal int lines = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MeasureObjectCommand"/> class.
        /// Default constructor.
        /// </summary>
        public MeasureObjectCommand()
            : base()
        {
        }

        #region Command Line Switches

        #region Common parameters in both sets

        /// <summary>
        /// Incoming object.
        /// </summary>
        /// <value></value>
        [Parameter(ValueFromPipeline = true)]
        public PSObject InputObject { get; set; } = AutomationNull.Value;

        /// <summary>
        /// Properties to be examined.
        /// </summary>
        /// <value></value>
        [ValidateNotNullOrEmpty]
        [Parameter(Position = 0)]
        public PSPropertyExpression[] Property { get; set; }

        #endregion Common parameters in both sets

        /// <summary>
        /// Set to true if Standard Deviation is to be returned.
        /// </summary>
        [Parameter(ParameterSetName = GenericParameterSet)]
        public SwitchParameter StandardDeviation
        {
            get
            {
                return _measureStandardDeviation;
            }

            set
            {
                _measureStandardDeviation = value;
            }
        }

        private bool _measureStandardDeviation;

        /// <summary>
        /// Set to true is Sum is to be returned.
        /// </summary>
        /// <value></value>
        [Parameter(ParameterSetName = GenericParameterSet)]
        public SwitchParameter Sum
        {
            get
            {
                return _measureSum;
            }

            set
            {
                _measureSum = value;
            }
        }

        private bool _measureSum;

        /// <summary>
        /// Gets or sets the value indicating if all statistics should be returned.
        /// </summary>
        /// <value></value>
        [Parameter(ParameterSetName = GenericParameterSet)]
        public SwitchParameter AllStats
        {
            get
            {
                return _allStats;
            }

            set
            {
                _allStats = value;
            }
        }

        private bool _allStats;

        /// <summary>
        /// Set to true is Average is to be returned.
        /// </summary>
        /// <value></value>
        [Parameter(ParameterSetName = GenericParameterSet)]
        public SwitchParameter Average
        {
            get
            {
                return _measureAverage;
            }

            set
            {
                _measureAverage = value;
            }
        }

        private bool _measureAverage;

        /// <summary>
        /// Set to true is Max is to be returned.
        /// </summary>
        /// <value></value>
        [Parameter(ParameterSetName = GenericParameterSet)]
        public SwitchParameter Maximum
        {
            get
            {
                return _measureMax;
            }

            set
            {
                _measureMax = value;
            }
        }

        private bool _measureMax;

        /// <summary>
        /// Set to true is Min is to be returned.
        /// </summary>
        /// <value></value>
        [Parameter(ParameterSetName = GenericParameterSet)]
        public SwitchParameter Minimum
        {
            get
            {
                return _measureMin;
            }

            set
            {
                _measureMin = value;
            }
        }

        private bool _measureMin;

        #region TextMeasure ParameterSet
        /// <summary>
        /// </summary>
        /// <value></value>
        [Parameter(ParameterSetName = TextParameterSet)]
        public SwitchParameter Line
        {
            get
            {
                return _measureLines;
            }

            set
            {
                _measureLines = value;
            }
        }

        private bool _measureLines = false;

        /// <summary>
        /// </summary>
        /// <value></value>
        [Parameter(ParameterSetName = TextParameterSet)]
        public SwitchParameter Word
        {
            get
            {
                return _measureWords;
            }

            set
            {
                _measureWords = value;
            }
        }

        private bool _measureWords = false;

        /// <summary>
        /// </summary>
        /// <value></value>
        [Parameter(ParameterSetName = TextParameterSet)]
        public SwitchParameter Character
        {
            get
            {
                return _measureCharacters;
            }

            set
            {
                _measureCharacters = value;
            }
        }

        private bool _measureCharacters = false;

        /// <summary>
        /// </summary>
        /// <value></value>
        [Parameter(ParameterSetName = TextParameterSet)]
        public SwitchParameter IgnoreWhiteSpace
        {
            get
            {
                return _ignoreWhiteSpace;
            }

            set
            {
                _ignoreWhiteSpace = value;
            }
        }

        private bool _ignoreWhiteSpace;

        #endregion TextMeasure ParameterSet
        #endregion Command Line Switches

        /// <summary>
        /// Which parameter set the Cmdlet is in.
        /// </summary>
        private bool IsMeasuringGeneric
        {
            get
            {
                return string.Equals(ParameterSetName, GenericParameterSet, StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Does the begin part of the cmdlet.
        /// </summary>
        protected override void BeginProcessing()
        {
            // Sets all other generic parameters to true to get all statistics.
            if (_allStats)
            {
                _measureSum = _measureStandardDeviation = _measureAverage = _measureMax = _measureMin = true;
            }

            // finally call the base class.
            base.BeginProcessing();
        }

        /// <summary>
        /// Collect data about each record that comes in.
        /// Side effects: Updates totalRecordCount.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (InputObject == null || InputObject == AutomationNull.Value)
            {
                return;
            }

            _totalRecordCount++;

            if (Property == null)
                AnalyzeValue(null, InputObject.BaseObject);
            else
                AnalyzeObjectProperties(InputObject);
        }

        /// <summary>
        /// Analyze an object on a property-by-property basis instead
        /// of as a simple value.
        /// Side effects: Updates statistics.
        /// </summary>
        /// <param name="inObj">The object to analyze.</param>
        private void AnalyzeObjectProperties(PSObject inObj)
        {
            // Keep track of which properties are counted for an
            // input object so that repeated properties won't be
            // counted twice.
            MeasureObjectDictionary<object> countedProperties = new();

            // First iterate over the user-specified list of
            // properties...
            foreach (var expression in Property)
            {
                List<PSPropertyExpression> resolvedNames = expression.ResolveNames(inObj);
                if (resolvedNames == null || resolvedNames.Count == 0)
                {
                    // Insert a blank entry so we can track
                    // property misses in EndProcessing.
                    if (!expression.HasWildCardCharacters)
                    {
                        string propertyName = expression.ToString();
                        _statistics.EnsureEntry(propertyName);
                    }

                    continue;
                }

                // Each property value can potentially refer
                // to multiple properties via globbing. Iterate over
                // the actual property names.
                foreach (PSPropertyExpression resolvedName in resolvedNames)
                {
                    string propertyName = resolvedName.ToString();
                    // skip duplicated properties
                    if (countedProperties.ContainsKey(propertyName))
                    {
                        continue;
                    }

                    List<PSPropertyExpressionResult> tempExprRes = resolvedName.GetValues(inObj);
                    if (tempExprRes == null || tempExprRes.Count == 0)
                    {
                        // Shouldn't happen - would somehow mean
                        // that the property went away between when
                        // we resolved it and when we tried to get its
                        // value.
                        continue;
                    }

                    AnalyzeValue(propertyName, tempExprRes[0].Result);

                    // Remember resolved propertyNames that have been counted
                    countedProperties[propertyName] = null;
                }
            }
        }

        /// <summary>
        /// Analyze a value for generic/text statistics.
        /// Side effects: Updates statistics. May set nonNumericError.
        /// </summary>
        /// <param name="propertyName">The property this value corresponds to.</param>
        /// <param name="objValue">The value to analyze.</param>
        private void AnalyzeValue(string propertyName, object objValue)
        {
            propertyName ??= thisObject;

            Statistics stat = _statistics.EnsureEntry(propertyName);

            // Update common properties.
            stat.count++;

            if (_measureCharacters || _measureWords || _measureLines)
            {
                string strValue = (objValue == null) ? string.Empty : objValue.ToString();
                AnalyzeString(strValue, stat);
            }

            if (_measureAverage || _measureSum || _measureStandardDeviation)
            {
                double numValue = 0.0;
                if (!LanguagePrimitives.TryConvertTo(objValue, out numValue))
                {
                    _nonNumericError = true;
                    ErrorRecord errorRecord = new(
                        PSTraceSource.NewInvalidOperationException(MeasureObjectStrings.NonNumericInputObject, objValue),
                        "NonNumericInputObject",
                        ErrorCategory.InvalidType,
                        objValue);
                    WriteError(errorRecord);
                    return;
                }

                AnalyzeNumber(numValue, stat);
            }

            // Measure-Object -MAX -MIN should work with ANYTHING that supports CompareTo
            if (_measureMin)
            {
                stat.min = Compare(objValue, stat.min, true);
            }

            if (_measureMax)
            {
                stat.max = Compare(objValue, stat.max, false);
            }
        }

        /// <summary>
        /// Compare is a helper function used to find the min/max between the supplied input values.
        /// </summary>
        /// <param name="objValue">
        /// Current input value.
        /// </param>
        /// <param name="statMinOrMaxValue">
        /// Current minimum or maximum value in the statistics.
        /// </param>
        /// <param name="isMin">
        /// Indicates if minimum or maximum value has to be found.
        /// If true is passed in then the minimum of the two values would be returned.
        /// If false is passed in then maximum of the two values will be returned.</param>
        /// <returns></returns>
        private static object Compare(object objValue, object statMinOrMaxValue, bool isMin)
        {
            object currentValue = objValue;
            object statValue = statMinOrMaxValue;

            double temp;
            currentValue = ((objValue != null) && LanguagePrimitives.TryConvertTo<double>(objValue, out temp)) ? temp : currentValue;
            statValue = ((statValue != null) && LanguagePrimitives.TryConvertTo<double>(statValue, out temp)) ? temp : statValue;

            if (currentValue != null && statValue != null && !currentValue.GetType().Equals(statValue.GetType()))
            {
                currentValue = PSObject.AsPSObject(currentValue).ToString();
                statValue = PSObject.AsPSObject(statValue).ToString();
            }

            if (statValue == null)
            {
                return objValue;
            }

            int comparisonResult = LanguagePrimitives.Compare(statValue, currentValue, ignoreCase: false, CultureInfo.CurrentCulture);
            return (isMin ? comparisonResult : -comparisonResult) > 0
                ? objValue
                : statMinOrMaxValue;
        }

        /// <summary>
        /// Class contains util static functions.
        /// </summary>
        private static class TextCountUtilities
        {
            /// <summary>
            /// Count chars in inStr.
            /// </summary>
            /// <param name="inStr">String whose chars are counted.</param>
            /// <param name="ignoreWhiteSpace">True to discount white space.</param>
            /// <returns>Number of chars in inStr.</returns>
            internal static int CountChar(string inStr, bool ignoreWhiteSpace)
            {
                if (string.IsNullOrEmpty(inStr))
                {
                    return 0;
                }

                if (!ignoreWhiteSpace)
                {
                    return inStr.Length;
                }

                int len = 0;
                foreach (char c in inStr)
                {
                    if (!char.IsWhiteSpace(c))
                    {
                        len++;
                    }
                }

                return len;
            }

            /// <summary>
            /// Count words in inStr.
            /// </summary>
            /// <param name="inStr">String whose words are counted.</param>
            /// <returns>Number of words in inStr.</returns>
            internal static int CountWord(string inStr)
            {
                if (string.IsNullOrEmpty(inStr))
                {
                    return 0;
                }

                int wordCount = 0;
                bool wasAWhiteSpace = true;
                foreach (char c in inStr)
                {
                    if (char.IsWhiteSpace(c))
                    {
                        wasAWhiteSpace = true;
                    }
                    else
                    {
                        if (wasAWhiteSpace)
                        {
                            wordCount++;
                        }

                        wasAWhiteSpace = false;
                    }
                }

                return wordCount;
            }

            /// <summary>
            /// Count lines in inStr.
            /// </summary>
            /// <param name="inStr">String whose lines are counted.</param>
            /// <returns>Number of lines in inStr.</returns>
            internal static int CountLine(string inStr)
            {
                if (string.IsNullOrEmpty(inStr))
                {
                    return 0;
                }

                int numberOfLines = 0;
                foreach (char c in inStr)
                {
                    if (c == '\n')
                    {
                        numberOfLines++;
                    }
                }
                // 'abc\nd' has two lines
                // but 'abc\n' has one line
                if (inStr[inStr.Length - 1] != '\n')
                {
                    numberOfLines++;
                }

                return numberOfLines;
            }
        }

        /// <summary>
        /// Update text statistics.
        /// </summary>
        /// <param name="strValue">The text to analyze.</param>
        /// <param name="stat">The Statistics object to update.</param>
        private void AnalyzeString(string strValue, Statistics stat)
        {
            if (_measureCharacters)
                stat.characters += TextCountUtilities.CountChar(strValue, _ignoreWhiteSpace);
            if (_measureWords)
                stat.words += TextCountUtilities.CountWord(strValue);
            if (_measureLines)
                stat.lines += TextCountUtilities.CountLine(strValue);
        }

        /// <summary>
        /// Update number statistics.
        /// </summary>
        /// <param name="numValue">The number to analyze.</param>
        /// <param name="stat">The Statistics object to update.</param>
        private void AnalyzeNumber(double numValue, Statistics stat)
        {
            if (_measureSum || _measureAverage || _measureStandardDeviation)
            {
                stat.sumPrevious = stat.sum;
                stat.sum += numValue;
            }

            if (_measureStandardDeviation && stat.count > 1)
            {
                // Based off of iterative method of calculating variance on
                // "https://en.wikipedia.org/wiki/Algorithms_for_calculating_variance#Welford's_online_algorithm"
                double avgPrevious = stat.sumPrevious / (stat.count - 1);
                double avg = stat.sum / stat.count;
                stat.variance += (((numValue - avgPrevious) * (numValue - avg)) - stat.variance) / stat.count;
            }
        }

        /// <summary>
        /// WriteError when a property is not found.
        /// </summary>
        /// <param name="propertyName">The missing property.</param>
        /// <param name="errorId">The error ID to write.</param>
        private void WritePropertyNotFoundError(string propertyName, string errorId)
        {
            Diagnostics.Assert(Property != null, "no property and no InputObject should have been addressed");
            ErrorRecord errorRecord = new(
                    PSTraceSource.NewArgumentException(propertyName),
                    errorId,
                    ErrorCategory.ObjectNotFound,
                    null);
            errorRecord.ErrorDetails = new ErrorDetails(
                this, "MeasureObjectStrings", "PropertyNotFound", propertyName);
            WriteError(errorRecord);
        }

        /// <summary>
        /// Output collected statistics.
        /// Side effects: Updates statistics. Writes objects to stream.
        /// </summary>
        protected override void EndProcessing()
        {
            // Fix for 917114: If Property is not set,
            // and we aren't passed any records at all,
            // output 0s to emulate wc behavior.
            if (_totalRecordCount == 0 && Property == null)
            {
                _statistics.EnsureEntry(thisObject);
            }

            foreach (string propertyName in _statistics.Keys)
            {
                Statistics stat = _statistics[propertyName];
                if (stat.count == 0 && Property != null)
                {
                    if (Context.IsStrictVersion(2))
                    {
                        string errorId = (IsMeasuringGeneric) ? "GenericMeasurePropertyNotFound" : "TextMeasurePropertyNotFound";
                        WritePropertyNotFoundError(propertyName, errorId);
                    }

                    continue;
                }

                MeasureInfo mi = null;
                if (IsMeasuringGeneric)
                {
                    double temp;
                    if ((stat.min == null || LanguagePrimitives.TryConvertTo<double>(stat.min, out temp)) &&
                        (stat.max == null || LanguagePrimitives.TryConvertTo<double>(stat.max, out temp)))
                    {
                        mi = CreateGenericMeasureInfo(stat, true);
                    }
                    else
                    {
                        mi = CreateGenericMeasureInfo(stat, false);
                    }
                }
                else
                    mi = CreateTextMeasureInfo(stat);

                // Set common properties.
                if (Property != null)
                    mi.Property = propertyName;

                WriteObject(mi);
            }
        }

        /// <summary>
        /// Create a MeasureInfo object for generic stats.
        /// </summary>
        /// <param name="stat">The statistics to use.</param>
        /// <param name="shouldUseGenericMeasureInfo"></param>
        /// <returns>A new GenericMeasureInfo object.</returns>
        private MeasureInfo CreateGenericMeasureInfo(Statistics stat, bool shouldUseGenericMeasureInfo)
        {
            double? sum = null;
            double? average = null;
            double? StandardDeviation = null;
            object max = null;
            object min = null;

            if (!_nonNumericError)
            {
                if (_measureSum)
                    sum = stat.sum;

                if (_measureAverage && stat.count > 0)
                    average = stat.sum / stat.count;

                if (_measureStandardDeviation)
                {
                    StandardDeviation = Math.Sqrt(stat.variance);
                }
            }

            if (_measureMax)
            {
                if (shouldUseGenericMeasureInfo && (stat.max != null))
                {
                    double temp;
                    LanguagePrimitives.TryConvertTo<double>(stat.max, out temp);
                    max = temp;
                }
                else
                {
                    max = stat.max;
                }
            }

            if (_measureMin)
            {
                if (shouldUseGenericMeasureInfo && (stat.min != null))
                {
                    double temp;
                    LanguagePrimitives.TryConvertTo<double>(stat.min, out temp);
                    min = temp;
                }
                else
                {
                    min = stat.min;
                }
            }

            if (shouldUseGenericMeasureInfo)
            {
                GenericMeasureInfo gmi = new();
                gmi.Count = stat.count;
                gmi.Sum = sum;
                gmi.Average = average;
                gmi.StandardDeviation = StandardDeviation;
                if (max != null)
                {
                    gmi.Maximum = (double)max;
                }

                if (min != null)
                {
                    gmi.Minimum = (double)min;
                }

                return gmi;
            }
            else
            {
                GenericObjectMeasureInfo gomi = new();
                gomi.Count = stat.count;
                gomi.Sum = sum;
                gomi.Average = average;
                gomi.Maximum = max;
                gomi.Minimum = min;

                return gomi;
            }
        }

        /// <summary>
        /// Create a MeasureInfo object for text stats.
        /// </summary>
        /// <param name="stat">The statistics to use.</param>
        /// <returns>A new TextMeasureInfo object.</returns>
        private TextMeasureInfo CreateTextMeasureInfo(Statistics stat)
        {
            TextMeasureInfo tmi = new();

            if (_measureCharacters)
                tmi.Characters = stat.characters;
            if (_measureWords)
                tmi.Words = stat.words;
            if (_measureLines)
                tmi.Lines = stat.lines;

            return tmi;
        }

        /// <summary>
        /// The observed statistics keyed by property name.
        /// If Property is not set, then the key used will be the value of thisObject.
        /// </summary>
        private readonly MeasureObjectDictionary<Statistics> _statistics = new();

        /// <summary>
        /// Whether or not a numeric conversion error occurred.
        /// If true, then average/sum/standard deviation will not be output.
        /// </summary>
        private bool _nonNumericError = false;

        /// <summary>
        /// The total number of records encountered.
        /// </summary>
        private int _totalRecordCount = 0;

        /// <summary>
        /// Parameter set name for measuring objects.
        /// </summary>
        private const string GenericParameterSet = "GenericMeasure";

        /// <summary>
        /// Parameter set name for measuring text.
        /// </summary>
        private const string TextParameterSet = "TextMeasure";

        /// <summary>
        /// Key that statistics are stored under when Property is not set.
        /// </summary>
        private const string thisObject = "$_";
    }
}
