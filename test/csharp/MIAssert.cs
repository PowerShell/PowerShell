using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MI_NewTest
{
    using NativeObject;

    public static class MIAssert
    {
        public static void Succeeded(MI_Result code)
        {
            Assert.Equal(MI_Result.MI_RESULT_OK, code);
        }

        public static void Succeeded(MI_Result code, string message)
        {
            Assert.Equal(MI_Result.MI_RESULT_OK, code);
        }

        public static void Failed(MI_Result code)
        {
            Assert.NotEqual(MI_Result.MI_RESULT_OK, code);
        }

        public static void Failed(MI_Result code, string message)
        {
            Assert.NotEqual(MI_Result.MI_RESULT_OK, code);
        }

        public static void MIIntervalsEqual(MI_Interval expected, MI_Interval actual)
        {
            Assert.Equal(expected.days, actual.days);
            Assert.Equal(expected.hours, actual.hours);
            Assert.Equal(expected.minutes, actual.minutes);
            Assert.Equal(expected.seconds, actual.seconds);
            Assert.Equal(expected.microseconds, actual.microseconds);
            Assert.Equal(0u, expected.__padding1);
            Assert.Equal(0u, expected.__padding2);
            Assert.Equal(0u, expected.__padding3);
            Assert.Equal(0u, actual.__padding1);
            Assert.Equal(0u, actual.__padding2);
            Assert.Equal(0u, actual.__padding3);
        }

        public static void MIDatetimesEqual(MI_Datetime expected, MI_Datetime actual)
        {
            Assert.Equal(expected.isTimestamp, actual.isTimestamp);
            if (expected.isTimestamp)
            {
                Assert.Equal(expected.timestamp.day, actual.timestamp.day);
                Assert.Equal(expected.timestamp.month, actual.timestamp.month);
                Assert.Equal(expected.timestamp.year, actual.timestamp.year);
                Assert.Equal(expected.timestamp.second, actual.timestamp.second);
                Assert.Equal(expected.timestamp.hour, actual.timestamp.hour);
                Assert.Equal(expected.timestamp.minute, actual.timestamp.minute);
                Assert.Equal(expected.timestamp.microseconds, actual.timestamp.microseconds);
            }
            else
            {
                MIIntervalsEqual(expected.interval, actual.interval);
            }
        }

        public static void MIPropertiesEqual(TestMIProperty expectedProperty, TestMIProperty actualProperty, string propertyName)
        {
            Assert.Equal(expectedProperty.Type, actualProperty.Type);
            //Assert.Equal(expectedProperty.Flags, actualProperty.Flags);

            if (expectedProperty.Type == MI_Type.MI_DATETIME)
            {
                MI_Datetime expected = (MI_Datetime)expectedProperty.Value;
                var actual = (MI_Datetime)actualProperty.Value;
                if (expected.isTimestamp)
                {
                    MIAssert.MIDatetimesEqual(expected, actual);
                }
            }
            else if (expectedProperty.Type == MI_Type.MI_DATETIMEA)
            {
                MI_Datetime[] expected = (MI_Datetime[])expectedProperty.Value;
                var actual = (MI_Datetime[])actualProperty.Value;
                Assert.Equal(expected.Length, actual.Length);
                for (int i = 0; i < expected.Length; i++)
                {
                    MIAssert.MIDatetimesEqual(expected[i], (MI_Datetime)actual[i]);
                }
            }
            else if (expectedProperty.Type == MI_Type.MI_INSTANCE || expectedProperty.Type == MI_Type.MI_REFERENCE)
            {
                MI_Instance expected = (MI_Instance)expectedProperty.Value;
                MI_Instance actual = actualProperty.Value as MI_Instance;
                uint expectedElementCount;
                expected.GetElementCount(out expectedElementCount);
                uint actualElementCount;
                actual.GetElementCount(out actualElementCount);
                Assert.Equal(expectedElementCount, actualElementCount);
                for (uint i = 0; i < expectedElementCount; i++)
                {
                    MI_Flags expectedElementFlags;
                    MI_Type expectedElementType;
                    string expectedElementName = null;
                    MI_Value expectedElementValue = null;
                    expected.GetElementAt(i, out expectedElementName, out expectedElementValue, out expectedElementType, out expectedElementFlags);
                    
                    MI_Flags actualElementFlags;
                    MI_Value actualElementValue = null;
                    MI_Type actualElementType;
                    string actualElementName = null;
                    actual.GetElementAt(i, out actualElementName, out actualElementValue, out actualElementType, out actualElementFlags);

                    Assert.Equal(expectedElementName, actualElementName);
                    MIAssert.MIPropertiesEqual(new TestMIProperty(expectedElementValue, expectedElementType, expectedElementFlags),
                        new TestMIProperty(actualElementValue, actualElementType, actualElementFlags), propertyName);
                }
            }
            else if (expectedProperty.Type == MI_Type.MI_INSTANCEA || expectedProperty.Type == MI_Type.MI_REFERENCEA)
            {
                MI_Instance[] expectedArray = (MI_Instance[])expectedProperty.Value;
                MI_Instance[] actualArray = actualProperty.Value as MI_Instance[];
                Assert.Equal(expectedArray.Length, actualArray.Length);
                for (int j = 0; j < expectedArray.Length; j++)
                {
                    MI_Instance expected = expectedArray[j];
                    MI_Instance actual = actualArray[j];
                    uint expectedElementCount;
                    expected.GetElementCount(out expectedElementCount);
                    uint actualElementCount;
                    actual.GetElementCount(out actualElementCount);
                    Assert.Equal(expectedElementCount, actualElementCount);
                    for (uint i = 0; i < expectedElementCount; i++)
                    {
                        MI_Flags expectedElementFlags;
                        MI_Value expectedElementValue = null;
                        MI_Type expectedElementType;
                        string expectedElementName = null;
                        expected.GetElementAt(i, out expectedElementName, out expectedElementValue, out expectedElementType, out expectedElementFlags);

                        MI_Flags actualElementFlags;
                        MI_Value actualElementValue = null;
                        MI_Type actualElementType;
                        string actualElementName = null;
                        actual.GetElementAt(i, out actualElementName, out actualElementValue, out actualElementType, out actualElementFlags);

                        Assert.Equal(expectedElementName, actualElementName);
                        MIAssert.MIPropertiesEqual(new TestMIProperty(expectedElementValue, expectedElementType, expectedElementFlags),
                            new TestMIProperty(actualElementValue, actualElementType, actualElementFlags), propertyName);
                    }
                }
            }
            else if ((expectedProperty.Type & MI_Type.MI_ARRAY) == MI_Type.MI_ARRAY)
            {
                ICollection collectionValue = actualProperty.Value as ICollection;
                Assert.NotNull(collectionValue);
                Assert.Equal(expectedProperty.Value as ICollection, collectionValue);
            }
            else
            {
                Assert.Equal(expectedProperty.Value, actualProperty.Value);
            }
        }
    }
}
