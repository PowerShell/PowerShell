using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Globalization;
using NativeObject;

namespace Microsoft.Management.Infrastructure.Native
{
    internal class InstanceMethods
    {
        // Fields
        //internal static ValueType modopt(DateTime) modopt(IsBoxed) maxValidCimTimestamp;
        public static DateTime maxValidCimTimestamp = new DateTime(9999, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc);

        // Methods
        static InstanceMethods()
        {
        }
        internal InstanceMethods()
        {
        }
        ///
        /// takes an MI Instance, and adds to it a string representing the name of the instance, the MiValue representing the data value type,
        /// a type flag, and an MI Flag
        internal static MiResult AddElement(InstanceHandle instance, string name, object managedValue, MiType type, MiFlags flags)
        {
            NativeObject.MI_Value val;
            ConvertToMiValue((MI_Type)type, managedValue, out val);

            NativeObject.MI_Type nativeType = (MI_Type)(uint)type;
            NativeObject.MI_Flags nativeFlags = (MI_Flags)(uint)flags;
            MiResult r = (MiResult)(uint)instance.mmiInstance.AddElement(name, val, nativeType, nativeFlags);

            return r;
        }
        internal static MiResult ClearElementAt(InstanceHandle instance, int index)
        {
            MiResult r = (MiResult)(uint)instance.mmiInstance.ClearElementAt( (uint)index);

            return r;
        }
        internal static MiResult Clone(InstanceHandle instanceHandleToClone, out InstanceHandle clonedInstanceHandle)
        {
            Debug.Assert(instanceHandleToClone != null, "Caller should verify that instanceHandleToClone != null");
            clonedInstanceHandle = null;
            NativeObject.MI_Instance ptrClonedInstance = NativeObject.MI_Instance.NewIndirectPtr();

            MiResult r = (MiResult)(uint)instanceHandleToClone.mmiInstance.Clone(out ptrClonedInstance);
            if ( r == MiResult.OK)
            {
                clonedInstanceHandle = new InstanceHandle(ptrClonedInstance);
            }

            return r;
        }

        // Convert from an MI_Value to a .Net object
        internal static object ConvertFromMiValue(MI_Type type, MI_Value miValue)
        {
            object val = null;
            switch (type.ToString())
            {
                case "MI_BOOLEAN":
                    val = (bool)miValue.Boolean;
                    break;
                case "MI_BOOLEANA":
                    val = (bool[])miValue.BooleanA;
                    break;
                case "MI_SINT64":
                    val = (Int64)miValue.Sint64;
                    break;
                case "MI_SINT64A":
                    val = (Int64[])miValue.Sint64A;
                    break;
                case "MI_SINT32":
                    val = (Int32)miValue.Sint32;
                    break;
                case "MI_SINT32A":
                    val = (Int32[])miValue.Sint32A;
                    break;
                case "MI_SINT16":
                    val = (Int16)miValue.Sint16;
                    break;
                case "MI_SINT16A":
                    val = (Int16[])miValue.Sint16A;
                    break;
                case "MI_SINT8":
                    val = (sbyte)miValue.Sint8;
                    break;
                case "MI_SINT8A":
                    val = (sbyte[])miValue.Sint8A;
                    break;
                case "MI_UINT64":
                    val = (UInt64)miValue.Uint64;
                    break;
                case "MI_UINT64A":
                    val = (UInt64[])miValue.Uint64A;
                    break;
                case "MI_UINT32":
                    val = (UInt32)miValue.Uint32;
                    break;
                case "MI_UINT32A":
                    val = (UInt32[])miValue.Uint32A;
                    break;
                case "MI_UINT16":
                    val = (byte)miValue.Uint16;
                    break;
                case "MI_UINT16A":
                    val = (ushort[])miValue.Uint16A;
                    break;
                case "MI_UINT8":
                    val = (byte)miValue.Uint8;
                    break;
                case "MI_UINT8A":
                    val = (byte[])miValue.Uint8A;
                    break;
                case "MI_REAL32":
                    val = (float)miValue.Real32;
                    break;
                case "MI_REAL32A":
                    val = (float[])miValue.Real32A;
                    break;
                case "MI_REAL64":
                    val = (double)miValue.Real64;
                    break;
                case "MI_REAL64A":
                    val = (double[])miValue.Real64A;
                    break;
                case "MI_CHAR16":
                    val  = (char)miValue.Char16;
                    break;
                case "MI_CHAR16A":
                    val = (char[])miValue.Char16A;
                    break;
                case "MI_STRING":
                    val  = (string)miValue.String;
                    break;
                case "MI_STRINGA":
                    val = (string[])miValue.StringA;
                    break;
                case "MI_DATETIME":
                    val = MiDateTimeToManagedObjectDateTime(miValue.Datetime);
                    break;
                case "MI_INSTANCE":
                case "MI_REFERENCE":
                    val = new InstanceHandle(miValue.Instance);
                    break;
                case "MI_DATETIMEA":
                    List<object> dateTimes = new List<object>();

                    foreach (var dt in miValue.DatetimeA)
                    {
                          dateTimes.Add( MiDateTimeToManagedObjectDateTime(dt) );
                    }

                    val = dateTimes.ToArray();
                    break;
                case "MI_REFERENCEA":
                case "MI_INSTANCEA":
                    List<InstanceHandle> instances = new List<InstanceHandle>();

                    foreach (var inst in miValue.InstanceA)
                    {
                        instances.Add( new InstanceHandle(miValue.Instance) );
                    }

                    val = instances.ToArray();
                    break;
                default:
                    Console.WriteLine("ERROR: MI_Value {0} is an unknown MI_Value " + type.ToString());
                    break;
            }
            return val;
        }

        internal static void ConvertArrayToMiValue(MI_Type type, object managedValue, out MI_Value miValue)
        {
            miValue = MI_Value.NewDirectPtr();

            byte[] a = managedValue as byte[]; //TODO: what if this is a boolean?
            if (a != null)
            {
                miValue.Uint8A = a;
                return;
            }

            ushort[] b = managedValue as ushort[];
            if (b != null)
            {
                miValue.Uint16A = b;
                return;
            }

            UInt32[] c = managedValue as UInt32[];
            if (c != null)
            {
                miValue.Uint32A = c;
                return;
            }

            UInt64[] d = managedValue as UInt64[];
            if (d != null)
            {
                miValue.Uint64A = d;
                return;
            }

            sbyte[] e = managedValue as sbyte[];
            if (e != null)
            {
                miValue.Sint8A = e;
                return;
            }

            Int16[] f = managedValue as Int16[];
            if (f != null)
            {
                miValue.Sint16A = f;
                return;
            }

            Int32[] g = managedValue as Int32[];
            if (g != null)
            {
                miValue.Sint32A = g;
                return;
            }

            Int64[] h = managedValue as Int64[];
            if (h != null)
            {
                miValue.Sint64A = h;
                return;
            }

            bool[] i = managedValue as bool[];
            if (i != null)
            {
                miValue.BooleanA = i;
                return;
            }

            double[] j = managedValue as double[];
            if (j != null)
            {
                miValue.Real64A = j;
                return;
            }

            float[] k = managedValue as float[];
            if (k != null)
            {
                miValue.Real32A = k;
                return;
            }

            char[] l = managedValue as char[];
            if (l != null)
            {
                miValue.Char16A = l;
                return;
            }

            string[] m = managedValue as string[];
            if (m != null)
            {
                miValue.StringA = m;
                return;
            }

            System.DateTime[] n = managedValue as System.DateTime[];
            if (n != null)
            {
                MI_Datetime[] dateTimeArray = new MI_Datetime[n.Length];

                for ( int index = 0; index < n.Length; index++ )
                {
                    dateTimeArray[index] = ConvertManagedObjectToMiDateTime(n[index]);
                }
                miValue.DatetimeA = dateTimeArray;
                return;
            }

            InstanceHandle[] o = managedValue as InstanceHandle[];
            if ( o != null )
            {
                NativeObject.MI_Instance[] instanceArray = new NativeObject.MI_Instance[o.Length];

                for ( int index = 0; index < o.Length; index++ )
                {

                    instanceArray[index] = CopyManagedItemToMiInstance(o[index]);
                }
                miValue.InstanceA = instanceArray;
                return;
            }
        }

        // Converts from an object to something that can be consumed by the native MI engine.
        internal static void ConvertToMiValue(MI_Type type, object managedValue, out NativeObject.MI_Value miValue)
        {
            miValue = MI_Value.NewDirectPtr();
            switch (type.ToString())
            {
                case "MI_BOOLEAN":
                    miValue.Boolean = Convert.ToBoolean(managedValue);
                    break;
                case "MI_SINT8":
                    miValue.Sint8 = Convert.ToSByte(managedValue);
                    break;
                case "MI_SINT16":
                    miValue.Sint16 = Convert.ToInt16(managedValue);
                    break;
                case "MI_SINT32":
                    miValue.Sint32 = Convert.ToInt32(managedValue);
                    break;
                case "MI_SINT64":
                    miValue.Sint64 = Convert.ToInt64(managedValue);
                    break;
                case "MI_UINT8":
                    miValue.Uint8 = Convert.ToByte(managedValue);
                    break;
                case "MI_UINT16":
                    miValue.Uint16 = Convert.ToUInt16(managedValue);
                    break;
                case "MI_UINT32":
                    miValue.Uint32 = Convert.ToUInt32(managedValue);
                    break;
                case "MI_UINT64":
                    miValue.Uint64 = Convert.ToUInt64(managedValue);
                    break;
                case "MI_REAL32":
                    miValue.Real32 = Convert.ToSingle(managedValue);
                    break;
                case "MI_REAL64":
                    miValue.Real64 = Convert.ToDouble(managedValue);
                    break;
                case "MI_CHAR16":
                    miValue.Char16 = Convert.ToChar(managedValue);
                    break;
                case "MI_STRING":
                    miValue.String = Convert.ToString(managedValue);
                    break;
                case "MI_DATETIME":
                    miValue.Datetime = ConvertManagedObjectToMiDateTime(managedValue);
                    break;
                case "MI_REFERENCE":
                case "MI_INSTANCE":
                    miValue.Instance = CopyManagedItemToMiInstance(managedValue);
                    break;
                case "MI_BOOLEANA":
                case "MI_SINT8A":
                case "MI_SINT16A":
                case "MI_SINT32A":
                case "MI_SINT64A":
                case "MI_UINT8A":
                case "MI_UINT16A":
                case "MI_UINT64A":
                case "MI_UINT32A":
                case "MI_REAL32A":
                case "MI_REAL64A":
                case "MI_CHAR16A":
                case "MI_STRINGA":
                case "MI_DATETIMEA":
                case "MI_INSTANCEA":
                case "MI_REFERENCEA":
                    ConvertArrayToMiValue(type, managedValue, out miValue);
                    break;
                default:
                    Console.WriteLine("ERROR: unknown MI_Type type.  Type {0} is unknown", type.ToString());
                    break;

            }
        }

        /// Recieves an instanceHandle, creates a pointer to that instancehandle.
        internal static NativeObject.MI_Instance CopyManagedItemToMiInstance(object managedValue)
        {
            InstanceHandle oldHandle = managedValue as InstanceHandle;

            if (oldHandle != null)
            {
                InstanceHandle newHandle = new InstanceHandle(oldHandle.mmiInstance);

                return newHandle.mmiInstance;
            }

            return null;
        }
        // Converts MI_DateTime to System.DateTime
        internal static object MiDateTimeToManagedObjectDateTime(MI_Datetime miDatetime)
        {
            if (miDatetime.isTimestamp)
            {
                // "Now" value defined in line 1934, page 53 of DSP0004, version 2.6.0
                if ((miDatetime.timestamp.year == 0) &&
                    (miDatetime.timestamp.month == 0) &&
                    (miDatetime.timestamp.day == 1) &&
                    (miDatetime.timestamp.hour == 0) &&
                    (miDatetime.timestamp.minute == 0) &&
                    (miDatetime.timestamp.second == 0) &&
                    (miDatetime.timestamp.microseconds == 0) &&
                    (miDatetime.timestamp.utc == 720))
                {
                    return DateTime.Now;
                }
                // "Infinite past" value defined in line 1935, page 54 of DSP0004, version 2.6.0
                else if ((miDatetime.timestamp.year == 0) &&
                         (miDatetime.timestamp.month == 1) &&
                         (miDatetime.timestamp.day == 1) &&
                         (miDatetime.timestamp.hour == 0) &&
                         (miDatetime.timestamp.minute == 0) &&
                         (miDatetime.timestamp.second == 0) &&
                         (miDatetime.timestamp.microseconds == 999999) &&
                         (miDatetime.timestamp.utc == 720))
                {
                    return DateTime.MinValue;
                }
                // "Infinite future" value defined in line 1936, page 54 of DSP0004, version 2.6.0
                else if ((miDatetime.timestamp.year == 9999) &&
                         (miDatetime.timestamp.month == 12) &&
                         (miDatetime.timestamp.day == 31) &&
                         (miDatetime.timestamp.hour == 11) &&
                         (miDatetime.timestamp.minute == 59) &&
                         (miDatetime.timestamp.second == 59) &&
                         (miDatetime.timestamp.microseconds == 999999) &&
                         (miDatetime.timestamp.utc == 720))
                {
                    return DateTime.MaxValue;
                }
                else
                {
                    //If CoreCLR
                    Calendar myCalendar = CultureInfo.InvariantCulture.Calendar;

                    DateTime managedDateTime = myCalendar.ToDateTime(
                                                            (int)miDatetime.timestamp.year,
                                                            (int)miDatetime.timestamp.month,
                                                            (int)miDatetime.timestamp.day,
                                                            (int)miDatetime.timestamp.hour,
                                                            (int)miDatetime.timestamp.minute,
                                                            (int)miDatetime.timestamp.second,
                                                            (int)miDatetime.timestamp.microseconds / 1000);
                    DateTime managedUtcDateTime = DateTime.SpecifyKind(managedDateTime, DateTimeKind.Utc); //TODO: C++/cli uses myDateTime.SpecifyKind(), which fails here.
                    // ^^CoreCLR
                    long microsecondsUnaccounted = miDatetime.timestamp.microseconds % 1000;
                    managedUtcDateTime = managedUtcDateTime.AddTicks(microsecondsUnaccounted * 10); // since 1 microsecond == 10 ticks
                    managedUtcDateTime = managedUtcDateTime.AddMinutes(-(miDatetime.timestamp.utc));

                    DateTime managedLocalDateTime = TimeZoneInfo.ConvertTime(managedUtcDateTime,TimeZoneInfo.Local);
                    return managedLocalDateTime;
                }
           }
           else
           {
               if ( TimeSpan.MaxValue.TotalDays < miDatetime.interval.days )
               {
                   return TimeSpan.MaxValue;
               }

               try
               {
                   TimeSpan managedTimeSpan = new TimeSpan(
                                                    (int)miDatetime.interval.days,
                                                    (int)miDatetime.interval.hours,
                                                    (int)miDatetime.interval.minutes,
                                                    (int)miDatetime.interval.seconds,
                                                    (int)miDatetime.interval.microseconds / 1000);
                   long microsecondsUnaccounted = miDatetime.interval.microseconds % 1000;
                   TimeSpan ticksUnaccountedTimeSpan = new TimeSpan(microsecondsUnaccounted * 10); // since 1 microsecond == 10 ticks
                   TimeSpan correctedTimeSpan = managedTimeSpan.Add(ticksUnaccountedTimeSpan);

                   DateTime dt = new DateTime();
                   DateTime managedDateTime = dt.AddDays(correctedTimeSpan.Days)
                                                .AddHours(correctedTimeSpan.Hours)
                                                .AddMinutes(correctedTimeSpan.Minutes)
                                                .AddSeconds(correctedTimeSpan.Seconds)
                                                .AddMilliseconds(correctedTimeSpan.Milliseconds)
                                                .AddTicks(microsecondsUnaccounted * 10);

                   DateTime returnDate = DateTime.SpecifyKind(managedDateTime, DateTimeKind.Unspecified);
                   return returnDate;
               }
               catch (ArgumentOutOfRangeException)
               {
                       return TimeSpan.MaxValue;
               }
           }
        }
        // Converts System.DateTime to MI_DateTime
        internal static MI_Datetime ConvertManagedObjectToMiDateTime(object managedValue)
        {
            Debug.Assert(managedValue != null, "Caller should verify managedValue != null");
            NativeObject.MI_Datetime miDatetime = new NativeObject.MI_Datetime();

                //long ticks = dt.Ticks;
                //TimeSpan timeSpan = new TimeSpan(ticks);

                if (managedValue is TimeSpan)
                {
                    System.TimeSpan timeSpan = (TimeSpan)managedValue;
                    if (timeSpan.Equals(TimeSpan.MaxValue))
                    {
                        // "Infinite duration" value defined in line 1944, page 54 of DSP0004, version 2.6.0
                        miDatetime.interval.days         = 99999999;
                        miDatetime.interval.hours        = 23;
                        miDatetime.interval.minutes      = 59;
                        miDatetime.interval.seconds      = 59;
                        miDatetime.interval.microseconds = 0;
                    }
                    else
                    {
                        long ticksUnaccounted = timeSpan.Ticks%10000; // since 10000 ticks == 1 millisecond

                        miDatetime.interval.days         = (uint)timeSpan.Days;
                        miDatetime.interval.hours        = (uint)timeSpan.Hours;
                        miDatetime.interval.minutes      = (uint)timeSpan.Minutes;
                        miDatetime.interval.seconds      = (uint)timeSpan.Seconds;
                        miDatetime.interval.microseconds = (uint)(timeSpan.Milliseconds * 1000 + ticksUnaccounted/10); // since 1 tick == 0.1 microsecond
                    }

                    miDatetime.isTimestamp = false;
                }
                else
                {
                    // TimeStamp is null.  check that datetime isn't max
                //    System.DateTime dateTime = Convert.ToDateTime(managedValue); //TODO: do we *really* need this?
                    System.DateTime dateTime = (DateTime)managedValue;

                    if (dateTime.Equals(DateTime.MaxValue))
                    {
                        // "Infinite future" value defined in line 1936, page 54 of DSP0004, version 2.6.0
                        miDatetime.timestamp.year         = 9999;
                        miDatetime.timestamp.month        = 12;
                        miDatetime.timestamp.day          = 31;
                        miDatetime.timestamp.hour         = 11;
                        miDatetime.timestamp.minute       = 59;
                        miDatetime.timestamp.second       = 59;
                        miDatetime.timestamp.microseconds = 999999;
                        miDatetime.timestamp.utc          = (-720);
                    }
                    else if (dateTime.Equals(DateTime.MinValue))
                    {
                        // "Infinite past" value defined in line 1935, page 54 of DSP0004, version 2.6.0
                        miDatetime.timestamp.year         = 0;
                        miDatetime.timestamp.month        = 1;
                        miDatetime.timestamp.day          = 1;
                        miDatetime.timestamp.hour         = 0;
                        miDatetime.timestamp.minute       = 0;
                        miDatetime.timestamp.second       = 0;
                        miDatetime.timestamp.microseconds = 999999;
                        miDatetime.timestamp.utc          = 720;
                    }
                    else if (DateTime.Compare(maxValidCimTimestamp, dateTime) <= 0)
                    {
                        // "Youngest useable timestamp" value defined in line 1930, page 53 of DSP0004, version 2.6.0
                        miDatetime.timestamp.year         = 9999;
                        miDatetime.timestamp.month        = 12;
                        miDatetime.timestamp.day          = 31;
                        miDatetime.timestamp.hour         = 11;
                        miDatetime.timestamp.minute       = 59;
                        miDatetime.timestamp.second       = 59;
                        miDatetime.timestamp.microseconds = 999998;
                        miDatetime.timestamp.utc          = (-720);
                    }
                    else
                    {
                        dateTime = TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Utc);
                        long ticksUnaccounted = dateTime.Ticks%10000;

                        miDatetime.timestamp.year         = (uint)dateTime.Year;
                        miDatetime.timestamp.month        = (uint)dateTime.Month;
                        miDatetime.timestamp.day          = (uint)dateTime.Day;
                        miDatetime.timestamp.hour         = (uint)dateTime.Hour;
                        miDatetime.timestamp.minute       = (uint)dateTime.Minute;
                        miDatetime.timestamp.second       = (uint)dateTime.Second;
                        miDatetime.timestamp.microseconds = (uint)dateTime.Millisecond * 1000 + (uint)ticksUnaccounted/10;
                        miDatetime.timestamp.utc          = 0;

                    }

                    miDatetime.isTimestamp = true;
                }
            return miDatetime;
        }

        //internal static unsafe void ConvertManagedObjectToMiDateTime(object managedValue, _MI_Datetime* pmiValue);
        //internal static unsafe object ConvertMiDateTimeToManagedObject(_MI_Datetime modopt(IsConst)* pmiValue);
        //internal static unsafe IEnumerable<DangerousHandleAccessor> ConvertToMiValue(MiType type, object managedValue, _MiValue* pmiValue);

        internal static MiResult GetClass(InstanceHandle instanceHandle, out ClassHandle classHandle)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetClassName(InstanceHandle instance, out string className)
        {
            MiResult r = (MiResult)(uint)instance.mmiInstance.GetClassName(out className);
            return r;
        }
        internal static MiResult GetElement_GetIndex(InstanceHandle instance, string name, out int index)
        {
            uint i;
            NativeObject.MI_Value v = null;
            MI_Type t;
            MI_Flags f = 0; //flags
            MiResult r = (MiResult)(uint)instance.mmiInstance.GetElement( name, out v, out t, out f, out i);
            index = (int)i;
            return r;
        }
        internal static MiResult GetElementAt_GetFlags(InstanceHandle instance, int index, out MiFlags flags)
        {
            Debug.Assert(instance.handle != IntPtr.Zero, "Caller should verify that instance is not null");
            Debug.Assert(index >= 0, "Caller should verify that index >=0");

            NativeObject.MI_Value val = MI_Value.NewDirectPtr();
            MI_Type nativeType;
            MI_Flags nativeFlags; //flags
            string name;

            MiResult r = (MiResult)(uint)instance.mmiInstance.GetElementAt( (uint)index, out name, out val, out nativeType, out nativeFlags);
            flags = (MiFlags)nativeFlags;
            return r;
        }
        internal static MiResult GetElementAt_GetName(InstanceHandle instance, int index, out string name)
        {
            NativeObject.MI_Value val = MI_Value.NewDirectPtr();
            MI_Type type;
            MI_Flags flags; //flags

            MiResult r = (MiResult)(uint)instance.mmiInstance.GetElementAt( (uint)index, out name, out val, out type, out flags);

            return r;
        }
        internal static MiResult GetElementAt_GetType(InstanceHandle instance, int index, out MiType type)
        {
            NativeObject.MI_Value val = MI_Value.NewDirectPtr();
            MI_Type nativeType;
            MI_Flags nativeFlags; //flags
            string name;

            MiResult r = (MiResult)(uint)instance.mmiInstance.GetElementAt( (uint)index, out name, out val, out nativeType, out nativeFlags);

            type = (MiType)nativeType;
            return r;
        }

        internal static MiResult GetElementAt_GetValue(InstanceHandle instance, int index, out object val)
        {
            Debug.Assert(0 <= index, "Caller should verify index > 0");
            string name;
            MI_Value miValue = MI_Value.NewDirectPtr();
            MI_Type type;
            MI_Flags flags;

            //IntPtr name;
            //IntPtr pVal= Marshal.AllocHGlobal(Marshal.SizeOf<MiValue>());
            //MiType type;
            //MiFlags flags;
            val = null;


            MiResult r = (MiResult)(uint)instance.mmiInstance.GetElementAt( (uint)index, out name, out miValue, out type, out flags);
            if (r == MiResult.OK && miValue != null)
            {
                if (!flags.HasFlag(MI_Flags.MI_FLAG_NULL))
                {
                    // ConvertToManaged
                    val = ConvertFromMiValue(type, miValue);
                }
                else
                {
                    val = null;
                }
            }

            return r;
        }
        internal static MiResult GetElementCount(InstanceHandle instance, out int elementCount)
        {
           uint count;
           MiResult r = (MiResult)(uint)instance.mmiInstance.GetElementCount( out count);
           elementCount = Convert.ToInt32(count);
           return r;
        }
        internal static MiResult GetNamespace(InstanceHandle instance, out string nameSpace)
        {
           MiResult r = (MiResult)(uint)instance.mmiInstance.GetNameSpace( out nameSpace);
           return r;
        }
        internal static MiResult GetServerName(InstanceHandle instance, out string serverName)
        {
           MiResult r = (MiResult)(uint)instance.mmiInstance.GetServerName(out serverName);
           return r;
        }
        //internal static unsafe void ReleaseMiValue(MiType type, _MiValue* pmiValue, IEnumerable<DangerousHandleAccessor> dangerousHandleAccessors);
        /*
         * This function should output a cleansed MiValue struct.  Structs are non-nullable types in C#, so
         * cannot point to null
         */
        internal static void ReleaseMiValue(MiType type, ref MiValue miValue)
        {
            miValue = new MiValue();
        }

        internal static MiResult SetElementAt_SetNotModifiedFlag(InstanceHandle handle, int index, [MarshalAs(UnmanagedType.U1)] bool notModifiedFlag)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetElementAt_SetValue(InstanceHandle instance, int index, object newValue)
        {
                throw new NotImplementedException();
                //MiResult r;
                //MiValue v = default(MiValue);
            //IntPtr t;
            //IntPtr f;
            //IntPtr noUse1;
            //// GetElementAt
            //r = instance.GetElementAt(instance.handle, (uint)index, out noUse1, out v, out t, out f);
            //if (MiResult.OK != r)
            //        return r;

            //// SetElementAt
            //r = instance.SetElementAt(ref instance.handle, (uint)index, v, (MiType)t, 0);
            //return r;
            // No need to release MiValues here as they do in managed C++ implementation.  MiValue is a struct and thus a value type.
        }
        internal static MiResult SetNamespace(InstanceHandle instance, string nameSpace)
        {
            return (MiResult)(uint)instance.mmiInstance.SetNameSpace( nameSpace);
        }

        internal static MiResult SetServerName(InstanceHandle instance, string serverName)
        {
            return (MiResult)(uint)instance.mmiInstance.SetServerName( serverName);
        }
        internal static void ThrowIfMismatchedType(MiType type, object managedValue)
        {
        // TODO: Strings are treated similar to primitive data types in C#.  They will be cleared out the same way. Only need to deal with the
        //        complex data types

        }
    }
}
