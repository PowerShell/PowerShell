using Xunit;
using System;
using System.Collections;
using Microsoft.Management.Infrastructure;
using System.Runtime.InteropServices;

namespace Microsoft.PowerShell.UnitTest
{
// CimInstance
    public class CimInstanceTests
    {
        [Fact]
        public static void TestCimInstanceLifeCycle()
        {
            string expected = String.Format("Cannot access a disposed object.{0}Object name: 'Microsoft.Management.Infrastructure.CimInstance'.", Environment.NewLine);
            CimInstance actual = new CimInstance("MyClass");
            Assert.True(null != actual);

            actual.Dispose();

            Exception ex = Assert.Throws<System.ObjectDisposedException>(() => actual.CimInstanceProperties);
            Assert.Equal(expected, ex.Message);
        }

        public static void VerifyCimProperties(object expectedValue, CimInstance inst, string propertyName)
        {
            ICollection expectedCollection  = expectedValue as ICollection;
            ICollection actualCollection = inst.CimInstanceProperties[propertyName].Value as ICollection;
            Assert.Equal(expectedCollection, actualCollection);
        }

        [Fact]
        public static void TestCimInstanceCanAccessMI_Value_IntegerTypes()
        {
            using (CimInstance instance = new CimInstance("TestWithPropertiesClass"))
            {

                SByte exSInt8   = 64;
                Int16 exSInt16  = -16;
                Int32 exSInt32  = (Int32)(-365);
                Int64 exSInt64  = Int64.MaxValue;
                Byte exUInt8    = 53;
                UInt16 exUInt16 = UInt16.MaxValue;
                UInt32 exUInt32 = 23;
                UInt64 exUInt64 = 487;

                byte[] expectedUint8A    = new byte[] { (byte)64, Byte.MaxValue };
                UInt16[] expectedUint16A = new UInt16[] { 4, UInt16.MaxValue };
                UInt32[] expectedUint32A = new UInt32[] { UInt16.MaxValue, UInt32.MaxValue };
                UInt64[] expectedUint64A = new UInt64[] { UInt32.MaxValue, UInt64.MaxValue };
                sbyte[] expectedSint8A   = new sbyte[] { -64, SByte.MaxValue };
                Int16[] expectedSint16A  = new Int16[] { -40, Int16.MaxValue };
                Int32[] expectedSint32A  = new Int32[] { -400, Int32.MaxValue };
                Int64[] expectedSint64A  = new Int64[] { Int32.MaxValue, Int64.MaxValue };

                instance.CimInstanceProperties.Add(CimProperty.Create("SInt8", exSInt8, CimType.SInt8, CimFlags.None));
                instance.CimInstanceProperties.Add(CimProperty.Create("SInt8A", expectedSint8A, CimType.SInt8Array, CimFlags.None));
                instance.CimInstanceProperties.Add(CimProperty.Create("SInt16", exSInt16, CimType.SInt16, CimFlags.None));
                instance.CimInstanceProperties.Add(CimProperty.Create("SInt16A", expectedSint16A, CimType.SInt16Array, CimFlags.None));
                instance.CimInstanceProperties.Add(CimProperty.Create("SInt32", exSInt32, CimType.SInt32, CimFlags.None));
                instance.CimInstanceProperties.Add(CimProperty.Create("SInt32A", expectedSint32A, CimType.SInt32Array, CimFlags.None));
                instance.CimInstanceProperties.Add(CimProperty.Create("SInt64", exSInt64, CimType.SInt64, CimFlags.None));
                instance.CimInstanceProperties.Add(CimProperty.Create("SInt64A", expectedSint64A, CimType.SInt64Array, CimFlags.None));

                instance.CimInstanceProperties.Add(CimProperty.Create("UInt8", exUInt8, CimType.UInt8, CimFlags.None));
                instance.CimInstanceProperties.Add(CimProperty.Create("UInt8A", expectedUint8A, CimType.UInt8Array, CimFlags.None));
                instance.CimInstanceProperties.Add(CimProperty.Create("UInt16", exUInt16, CimType.UInt16, CimFlags.None));
                instance.CimInstanceProperties.Add(CimProperty.Create("UInt16A", expectedUint16A, CimType.UInt16Array, CimFlags.None));
                instance.CimInstanceProperties.Add(CimProperty.Create("UInt32", exUInt32, CimType.UInt32, CimFlags.None));
                instance.CimInstanceProperties.Add(CimProperty.Create("UInt32A", expectedUint32A, CimType.UInt32Array, CimFlags.None));
                instance.CimInstanceProperties.Add(CimProperty.Create("UInt64", exUInt64, CimType.UInt64, CimFlags.None));
                instance.CimInstanceProperties.Add(CimProperty.Create("UInt64A", expectedUint64A, CimType.UInt64Array, CimFlags.None));

                VerifyCimProperties(exUInt8, instance, "UInt8");
                VerifyCimProperties(expectedUint8A, instance, "UInt8A");
                VerifyCimProperties(exUInt32, instance, "UInt32");
                VerifyCimProperties(expectedUint32A, instance, "UInt32A");
                VerifyCimProperties(exUInt64, instance, "UInt64");
                VerifyCimProperties(expectedUint64A, instance, "UInt64A");
                VerifyCimProperties(exSInt8, instance, "SInt8");
                VerifyCimProperties(expectedSint8A, instance, "SInt8A");
                VerifyCimProperties(exSInt16, instance, "SInt16");
                VerifyCimProperties(expectedSint16A, instance, "SInt16A");
                VerifyCimProperties(exSInt32, instance, "SInt32");
                VerifyCimProperties(expectedSint32A, instance, "SInt32A");
                VerifyCimProperties(exSInt64, instance, "SInt64");
                VerifyCimProperties(expectedSint64A, instance, "SInt64A");
            }
        }

        [Fact]
        public static void TestCimInstanceBooleanTypes()
        {
            using (CimInstance c = new CimInstance("TestBooleans"))
            {
                bool[] expectedBoolArray = new bool[] {true, false, true };
                c.CimInstanceProperties.Add(CimProperty.Create("BoolArray", expectedBoolArray , CimFlags.None));
                c.CimInstanceProperties.Add(CimProperty.Create("BoolValue", false, CimFlags.None));


                //Assert.Equal(false, c.CimInstanceProperties["BoolValue"].Value);
                Assert.Equal(expectedBoolArray, c.CimInstanceProperties["BoolArray"].Value);
            }

        }

        [Fact]
        public static void TestCimInstanceRealTypes()
        {
            double[] expectedDoubleArray = new double[] { 9.9D, 3.14d, 0.5d };
            float[] expectedFloatArray = new float[] { 4.8f, 0.4f, 11.1f };
            using (CimInstance c = new CimInstance("TestDoubles"))
            {
                c.CimInstanceProperties.Add(CimProperty.Create("doubleValue", 9.9D, CimFlags.None));
                c.CimInstanceProperties.Add(CimProperty.Create("doubleArray", expectedDoubleArray, CimFlags.None));

                Assert.Equal(9.9d, c.CimInstanceProperties["doubleValue"].Value);
                Assert.Equal(expectedDoubleArray, c.CimInstanceProperties["doubleArray"].Value);


                c.CimInstanceProperties.Add(CimProperty.Create("floatValue", 1.5f, CimFlags.None));
                c.CimInstanceProperties.Add(CimProperty.Create("floatArray", expectedFloatArray, CimFlags.None));

                Assert.Equal(1.5f, c.CimInstanceProperties["floatValue"].Value);
                Assert.Equal(expectedFloatArray, c.CimInstanceProperties["floatArray"].Value);
            }
        }

        [Fact]
        public void TestCimInstanceCharTypes()
        {
            using (CimInstance c = new CimInstance("TestChars"))
            {
                char[] expectedCharArray = new char[] {'x', 'y', 'z'};
                c.CimInstanceProperties.Add(CimProperty.Create("CharValue", 'y', CimFlags.None));
                c.CimInstanceProperties.Add(CimProperty.Create("CharArray", expectedCharArray , CimFlags.None));

                Assert.Equal('y', c.CimInstanceProperties["CharValue"].Value);
                Assert.Equal(expectedCharArray, c.CimInstanceProperties["CharArray"].Value);
            }
        }

        [Fact]
        public static void TestCimInstanceCanAccessStringMiValues()
        {
            string[] stringArray = new String[] { "testString", "another test String" };
            using (CimInstance c = new CimInstance("StringsTests"))
            {
                c.CimInstanceProperties.Add(CimProperty.Create("stringValue", "test string", CimFlags.None));
                c.CimInstanceProperties.Add(CimProperty.Create("stringArray", stringArray, CimFlags.None));

                Assert.Equal("test string", c.CimInstanceProperties["stringValue"].Value);
                Assert.Equal(stringArray, c.CimInstanceProperties["stringArray"].Value);
            }
        }

        [Fact]
        public static void TestCimInstanceDateTimeType()
        {

            System.DateTime expectedDateValue = new DateTime(1983, 02, 28);
            System.DateTime expectedDateTimeValue = new DateTime(1983, 02, 28, 8, 5, 4);
            System.DateTime[] expectedDateTimeArray = new DateTime[]
            {
                new DateTime(2009, 3, 12),
                new DateTime(2965, 12, 31),
                new DateTime(1999, 2, 10, 17, 35, 30, DateTimeKind.Unspecified)
            };

            object[] objectArray = new object[expectedDateTimeArray.Length];
            Array.Copy(expectedDateTimeArray, objectArray, expectedDateTimeArray.Length);

            using (CimInstance c = new CimInstance("DateTimeTests"))
            {
                c.CimInstanceProperties.Add(CimProperty.Create("DateValue", expectedDateValue, CimFlags.None));
                c.CimInstanceProperties.Add(CimProperty.Create("DateTimeValue", expectedDateTimeValue, CimFlags.None));
                c.CimInstanceProperties.Add(CimProperty.Create("DateTimeArray", expectedDateTimeArray, CimFlags.None));

                Assert.Equal(expectedDateValue, c.CimInstanceProperties["DateValue"].Value);
                Assert.Equal(expectedDateTimeValue, c.CimInstanceProperties["DateTimeValue"].Value);
                // the DateTimeArray value return type is "object[]"
                Assert.Equal(objectArray, c.CimInstanceProperties["DateTimeArray"].Value);
            }
        }

        [Fact]
        public static void TestCimInstanceFromCimInstance()
        {
            using (CimInstance firstInstance = new CimInstance("InstanceFromClassName"))
            {
                CimInstance secondInstance = new CimInstance(firstInstance);
                Assert.Equal("InstanceFromClassName", secondInstance.ToString());
            }
        }

        [Fact]
        public static void TestCimInstanceInstanceType()
        {
            CimInstance innerInstance = new CimInstance("FirstInnerInstance");
            CimInstance innerInstance2 = new CimInstance("SecondInnerInstance");
            CimInstance[] cimInstanceArray = new CimInstance[]{ innerInstance, innerInstance2 };

            using (CimInstance outerInstance = new CimInstance("OuterInstance"))
            {

                // Add the inner instance as a property of the outer instance
                outerInstance.CimInstanceProperties.Add(CimProperty.Create("InstanceProperty", innerInstance, CimFlags.None));

                var outerInstanceActual = outerInstance.CimInstanceProperties["InstanceProperty"];

                Assert.Equal("FirstInnerInstance", outerInstanceActual.Value.ToString());


                outerInstance.CimInstanceProperties.Add(CimProperty.Create("InstanceArray", cimInstanceArray, CimType.InstanceArray, CimFlags.None));

                var instanceActual = outerInstance.CimInstanceProperties["InstanceArray"];
                Assert.Equal(typeof(CimInstance[]), instanceActual.Value.GetType());

                // TODO: there is an issue with the tes runner where neither of these methods of iterating over a collection actually works.
                //ICollection instanceArrayActualCollection = instanceActual.Value as ICollection;
                //IEnumerator ienum = instanceArrayActualCollection.GetEnumerator();
                //int i = 0;
                //while ( ienum.MoveNext())
                //{
                //    Assert.Equal(cimInstanceArray[i++].ToString(), ienum.Current.ToString());
                //}
                //foreach ( var actual in instanceArrayActualCollection )
                //{
                //    //var expected = cimInstanceArray[i++].ToString();
                //    //Console.WriteLine("exp: " + expected);
                //    Console.WriteLine("act: " + actual.ToString());
                //    //Assert.Equal(expected, actual.ToString());
                //}
            }
        }
    }
}
