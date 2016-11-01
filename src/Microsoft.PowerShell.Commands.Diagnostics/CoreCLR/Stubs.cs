
#if CORECLR
using System.ComponentModel;
using Microsoft.PowerShell.CoreClr.Stubs;

namespace System.Runtime.ConstrainedExecution
{
	/// <summary>
	/// Specifies a method's behavior when called within a constrained execution region.
	/// </summary>
	[Serializable]
	internal enum Cer
	{
		/// <summary>
		/// The method, type, or assembly has no concept of a CER.
		/// It does not take advantage of CER guarantees.</summary>
		None,

		/// <summary>
		/// In the face of exceptional conditions, the method might fail.
		/// In this case, the method will report back to the calling method whether
		/// it succeeded or failed. The method must have a CER around the method body
		/// to ensure that it can report the return value.
		///</summary>
		MayFail,

		/// <summary>
		/// In the face of exceptional conditions, the method is guaranteed to succeed.
		/// You should always construct a CER around the method that is called,
		/// even when it is called from within a non-CER region. A method is successful
		/// if it accomplishes what is intended.
		/// For example, marking <see cref="P:System.Collections.ArrayList.Count" /> with
		/// ReliabilityContractAttribute(Cer.Success) implies that when it is run
		/// under a CER, it always returns a count of the number of elements in the
		/// <see cref="T:System.Collections.ArrayList" /> and it can never leave the
		/// internal fields in an undetermined state.</summary>
		Success
	}

	/// <summary>
	/// Specifies a reliability contract.
	/// </summary>
	[Serializable]
	internal enum Consistency
	{
		/// <summary>
		/// In the face of exceptional conditions, the CLR makes no guarantees
		/// regarding state consistency; that is, the condition might corrupt the process.
		/// </summary>
		MayCorruptProcess,

		/// <summary>
		/// In the face of exceptional conditions, the common language runtime (CLR)
		/// makes no guarantees regarding state consistency in the current application domain.
		/// </summary>
		MayCorruptAppDomain,

		/// <summary>
		/// In the face of exceptional conditions, the method is guaranteed to limit
		/// state corruption to the current instance.
		/// </summary>
		MayCorruptInstance,
		
		/// <summary>
		/// In the face of exceptional conditions, the method is guaranteed not to corrupt state.
		/// </summary>
		WillNotCorruptState
	}

	/// <summary>
	/// Defines a contract for reliability between the author of some code, and
	/// the developers who have a dependency on that code.
	/// </summary>
	[AttributeUsage(  AttributeTargets.Assembly
					| AttributeTargets.Class
					| AttributeTargets.Struct
					| AttributeTargets.Constructor
					| AttributeTargets.Method
					| AttributeTargets.Interface,
					Inherited = false)]
	internal sealed class ReliabilityContractAttribute : Attribute
	{
		private Consistency _consistency;
		private Cer _cer;

		/// <summary>
		/// Gets the value of the <see cref="T:System.Runtime.ConstrainedExecution.Consistency" />
		/// reliability contract. </summary>
		/// <returns>
		/// One of the <see cref="T:System.Runtime.ConstrainedExecution.Consistency" /> values.
		/// </returns>
		public Consistency ConsistencyGuarantee
		{
			get
			{
				return this._consistency;
			}
		}

		/// <summary>
		/// Gets the value that determines the behavior of a method, type, or assembly
		/// when called under a Constrained Execution Region (CER).
		/// </summary>
		/// <returns>
		/// One of the <see cref="T:System.Runtime.ConstrainedExecution.Cer" /> values.
		/// </returns>
		public Cer Cer
		{
			get
			{
				return this._cer;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:System.Runtime.ConstrainedExecution.ReliabilityContractAttribute" />
		/// class with the specified <see cref="T:System.Runtime.ConstrainedExecution.Consistency" />
		/// guarantee and <see cref="T:System.Runtime.ConstrainedExecution.Cer" /> value.
		/// </summary>
		/// <param name="consistencyGuarantee">
		/// One of the <see cref="T:System.Runtime.ConstrainedExecution.Consistency" /> values.
		/// </param>
		/// <param name="cer">
		/// One of the <see cref="T:System.Runtime.ConstrainedExecution.Cer" /> values
		/// </param>
		public ReliabilityContractAttribute(Consistency consistencyGuarantee, Cer cer)
		{
			this._consistency = consistencyGuarantee;
			this._cer = cer;
		}
	}
}

namespace System.Diagnostics
{
	/// <summary>
	/// Indicates whether the performance counter category can have multiple instances.
	/// </summary>
	/// <filterpriority>1</filterpriority>
	public enum PerformanceCounterCategoryType
	{
		/// <summary>
		/// The instance functionality for the performance counter category is unknown.
		/// </summary>
		Unknown = -1,

		/// <summary>
		/// The performance counter category can have only a single instance.
		/// </summary>
		SingleInstance,

		/// <summary>
		/// The performance counter category can have multiple instances.
		/// </summary>
		MultiInstance
	}

	/// <summary>
	/// Specifies the formula used to calculate the <see cref="M:System.Diagnostics.PerformanceCounter.NextValue" />
	/// method for a <see cref="T:System.Diagnostics.PerformanceCounter" /> instance.
	/// </summary>
	/// <filterpriority>2</filterpriority>
	[TypeConverter(typeof(AlphabeticalEnumConverter))]
	public enum PerformanceCounterType
	{
		/// <summary>
		/// An instantaneous counter that shows the most recently observed value.
		/// Used, for example, to maintain a simple count of items or operations.
		/// </summary>
		NumberOfItems32 = 65536,

		/// <summary>
		/// An instantaneous counter that shows the most recently observed value.
		/// Used, for example, to maintain a simple count of a very large number
		/// of items or operations. It is the same as NumberOfItems32 except that
		/// it uses larger fields to accommodate larger values.
		/// </summary>
		NumberOfItems64 = 65792,

		/// <summary>
		/// An instantaneous counter that shows the most recently observed value
		/// in hexadecimal format. Used, for example, to maintain a simple count
		/// of items or operations.</summary>
		NumberOfItemsHEX32 = 0,

		/// <summary>
		/// An instantaneous counter that shows the most recently observed value.
		/// Used, for example, to maintain a simple count of a very large number
		/// of items or operations. It is the same as NumberOfItemsHEX32 except
		/// that it uses larger fields to accommodate larger values.
		/// </summary>
		NumberOfItemsHEX64 = 256,

		/// <summary>
		/// A difference counter that shows the average number of operations completed
		/// during each second of the sample interval. Counters of this type measure
		/// time in ticks of the system clock.</summary>
		RateOfCountsPerSecond32 = 272696320,

		/// <summary>
		/// A difference counter that shows the average number of operations completed
		/// during each second of the sample interval. Counters of this type measure
		/// time in ticks of the system clock. This counter type is the same as the
		/// RateOfCountsPerSecond32 type, but it uses larger fields to accommodate
		/// larger values to track a high-volume number of items or operations per
		/// second, such as a byte-transmission rate.
		/// </summary>
		RateOfCountsPerSecond64 = 272696576,

		/// <summary>
		/// An average counter designed to monitor the average length of a queue
		/// to a resource over time. It shows the difference between the queue
		/// lengths observed during the last two sample intervals divided by the
		/// duration of the interval. This type of counter is typically used to
		/// track the number of items that are queued or waiting.
		/// </summary>
		CountPerTimeInterval32 = 4523008,

		/// <summary>
		/// An average counter that monitors the average length of a queue to a
		/// resource over time. Counters of this type display the difference
		/// between the queue lengths observed during the last two sample intervals,
		/// divided by the duration of the interval. This counter type is the same
		/// as CountPerTimeInterval32 except that it uses larger fields to
		/// accommodate larger values. This type of counter is typically used
		/// to track a high-volume or very large number of items that are queued or waiting.
		/// </summary>
		CountPerTimeInterval64 = 4523264,

		/// <summary>
		/// An instantaneous percentage counter that shows the ratio of a subset 
		/// to its set as a percentage. For example, it compares the number of bytes
		/// in use on a disk to the total number of bytes on the disk.
		/// Counters of this type display the current percentage only, not an average
		/// over time.
		/// </summary>
		RawFraction = 537003008,

		/// <summary>
		/// A base counter that stores the denominator of a counter that presents a
		/// general arithmetic fraction. Check that this value is greater than zero
		/// before using it as the denominator in a RawFraction value calculation.
		/// </summary>
		RawBase = 1073939459,

		/// <summary>
		/// An average counter that measures the time it takes, on average, to 
		/// complete a process or operation. Counters of this type display a
		/// ratio of the total elapsed time of the sample interval to the number
		/// of processes or operations completed during that time. This counter
		/// type measures time in ticks of the system clock.
		/// </summary>
		AverageTimer32 = 805438464,

		/// <summary>
		/// A base counter that is used in the calculation of time or count averages,
		///  such as AverageTimer32 and AverageCount64. Stores the denominator for
		/// calculating a counter to present "time per operation" or "count per operation".
		/// </summary>
		AverageBase = 1073939458,

		/// <summary>
		/// An average counter that shows how many items are processed, on average,
		/// during an operation. Counters of this type display a ratio of the items
		/// processed to the number of operations completed. The ratio is calculated
		/// by comparing the number of items processed during the last interval to
		/// the number of operations completed during the last interval.
		/// </summary>
		AverageCount64 = 1073874176,

		/// <summary>
		/// A percentage counter that shows the average ratio of hits to all
		/// operations during the last two sample intervals.
		/// </summary>
		SampleFraction = 549585920,

		/// <summary>
		/// An average counter that shows the average number of operations completed
		/// in one second. When a counter of this type samples the data, each sampling
		/// interrupt returns one or zero. The counter data is the number of ones that
		/// were sampled. It measures time in units of ticks of the system performance timer.
		/// </summary>
		SampleCounter = 4260864,

		/// <summary>
		/// A base counter that stores the number of sampling interrupts taken
		/// and is used as a denominator in the sampling fraction. The sampling 
		/// fraction is the number of samples that were 1 (or true) for a sample
		/// interrupt. Check that this value is greater than zero before using
		/// it as the denominator in a calculation of SampleFraction.
		/// </summary>
		SampleBase = 1073939457,

		/// <summary>
		/// A percentage counter that shows the average time that a component is
		/// active as a percentage of the total sample time.
		/// </summary>
		CounterTimer = 541132032,

		/// <summary>
		/// A percentage counter that displays the average percentage of active
		/// time observed during sample interval. The value of these counters is
		/// calculated by monitoring the percentage of time that the service was
		/// inactive and then subtracting that value from 100 percent.
		/// </summary>
		CounterTimerInverse = 557909248,

		/// <summary>A percentage counter that shows the active time of a component
		/// as a percentage of the total elapsed time of the sample interval.
		/// It measures time in units of 100 nanoseconds (ns). Counters of this
		/// type are designed to measure the activity of one component at a time.
		/// </summary>
		Timer100Ns = 542180608,

		/// <summary>
		/// A percentage counter that shows the average percentage of active time
		/// observed during the sample interval.
		/// </summary>
		Timer100NsInverse = 558957824,

		/// <summary>
		/// A difference timer that shows the total time between when the component
		/// or process started and the time when this value is calculated.
		/// </summary>
		ElapsedTime = 807666944,

		/// <summary>
		/// A percentage counter that displays the active time of one or more
		/// components as a percentage of the total time of the sample interval.
		/// Because the numerator records the active time of components operating
		/// simultaneously, the resulting percentage can exceed 100 percent.
		/// </summary>
		CounterMultiTimer = 574686464,

		/// <summary>
		/// A percentage counter that shows the active time of one or more components
		/// as a percentage of the total time of the sample interval. It derives
		/// the active time by measuring the time that the components were not
		/// active and subtracting the result from 100 percent by the number of
		/// objects monitored.
		/// </summary>
		CounterMultiTimerInverse = 591463680,

		/// <summary>
		/// A percentage counter that shows the active time of one or more components
		/// as a percentage of the total time of the sample interval. It measures
		/// time in 100 nanosecond (ns) units.</summary>
		CounterMultiTimer100Ns = 575735040,

		/// <summary>
		/// A percentage counter that shows the active time of one or more components
		/// as a percentage of the total time of the sample interval. Counters of
		/// this type measure time in 100 nanosecond (ns) units. They derive the
		/// active time by measuring the time that the components were not active
		/// and subtracting the result from multiplying 100 percent by the number
		/// of objects monitored.</summary>
		CounterMultiTimer100NsInverse = 592512256,

		/// <summary>
		/// A base counter that indicates the number of items sampled. It is used
		/// as the denominator in the calculations to get an average among the
		/// items sampled when taking timings of multiple, but similar items.
		/// Used with CounterMultiTimer, CounterMultiTimerInverse, CounterMultiTimer100Ns,
		/// and CounterMultiTimer100NsInverse.</summary>
		CounterMultiBase = 1107494144,

		/// <summary>
		/// A difference counter that shows the change in the measured attribute
		/// between the two most recent sample intervals.
		/// </summary>
		CounterDelta32 = 4195328,

		/// <summary>
		/// A difference counter that shows the change in the measured attribute
		/// between the two most recent sample intervals. It is the same as the
		/// CounterDelta32 counter type except that is uses larger fields to
		/// accomodate larger values.</summary>
		CounterDelta64 = 4195584
	}

	internal class AlphabeticalEnumConverter : EnumConverter
	{
		public AlphabeticalEnumConverter(Type type)
		  : base(type)
		{
		}

		public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
		{
			if (Values == null)
			{
				Array values = Enum.GetValues(EnumType);
				object[] array = new object[values.Length];

				for (int i = 0; i < array.Length; i++)
				{
					array[i] = ConvertTo(context, null, values.GetValue(i), typeof(string));
				}

				Array.Sort(array, values, 0, values.Length, System.Collections.Comparer.Default);
				Values = new TypeConverter.StandardValuesCollection(values);
			}

			return Values;
		}
	}
}
#endif
