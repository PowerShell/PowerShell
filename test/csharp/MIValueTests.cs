using System;
using System.Threading.Tasks;
using Xunit;
using NativeObject;

namespace MI_NewTest
{
    [Collection("AssemblyContext")]
    public class MIValueTests
    {
        private MI_Application application = null;
        private MI_Value value = new MI_Value();
        private MI_Instance instance = null;

        public MIValueTests()
        {
            MI_Instance extendedError = null;
            var res = MI_Application.Initialize("MIValueTests", out extendedError, out this.application);
            MIAssert.Succeeded(res);

            res = this.application.NewInstance("TestClass", MI_ClassDecl.Null, out this.instance);
            MIAssert.Succeeded(res);
        }

        ~MIValueTests()
        {
            if (!this.instance.IsNull)
            {
                this.instance.Delete();
            }

            this.value.Dispose();

            if (this.application != null)
            {
                var shutdownTask = Task.Factory.StartNew(() => this.application.Close() );
                bool completed = shutdownTask.Wait(TimeSpan.FromSeconds(5));
                Assert.True(completed, "MI_Application did not complete shutdown in the expected time - did you leave an object open?");
                MIAssert.Succeeded(shutdownTask.Result);
            }
        }

        private void TestValueRoundtrip()
        {
            Assert.True(this.value.Type.HasValue, "Expect value to have a value before calling helper");
            string propertyName = this.value.Type.ToString();
            var res = this.instance.AddElement(propertyName, this.value, this.value.Type.Value, MI_Flags.MI_FLAG_BORROW);
            MIAssert.Succeeded(res, "Expect add element to succeed");

            MI_Value returnedValue = null;
            MI_Type elementType;
            MI_Flags elementFlags = 0;
            UInt32 elementIndex = 0;
            res = instance.GetElement(propertyName, out returnedValue, out elementType, out elementFlags, out elementIndex);
            MIAssert.Succeeded(res, "Expect to get element by name");
            var testproperty = new TestMIProperty(returnedValue, elementType, elementFlags);
            MIAssert.MIPropertiesEqual(new TestMIProperty(this.value, this.value.Type.Value, MI_Flags.None), testproperty, propertyName);
        }

        [Fact]
        public void MIValue_BooleanTypes_Test()
        {
            this.value.Boolean = true;
            this.TestValueRoundtrip();

            this.value.BooleanA = new bool[] { false, false, true };
            this.TestValueRoundtrip();
        }

        [Fact]
        public void MIValue_Char16Types_Test()
        {
            this.value.Char16 = Char.MaxValue;
            this.TestValueRoundtrip();
            this.value.Char16A = new char[] { 'x', 'y', 'z' };
            this.TestValueRoundtrip();
        }

        [Fact]
        public void MIValue_DatetimeTypes_Test()
        {
            this.value.Datetime = new MI_Datetime()
            {
                isTimestamp = true,
                timestamp = new MI_Timestamp()
                {
                    year = 2013,
                    month = 6,
                    day = 2
                }
            };
            this.TestValueRoundtrip();

            this.value.DatetimeA = new MI_Datetime[] {
                new MI_Datetime()
                {
                    isTimestamp = true,
                    timestamp = new MI_Timestamp()
                    {
                        year = 1992,
                        month = 10,
                        day = 9
                    }
                },
                new MI_Datetime()
                {
                    isTimestamp = true,
                    timestamp = new MI_Timestamp()
                    {
                        year = 1988,
                        month = 9,
                        day = 11
                    }
                }
            };
            
            this.TestValueRoundtrip();
        }

        [Fact]
        public void MIValue_FloatingPointTypes_Test()
        {
            this.value.Real32 = .99f;
            this.TestValueRoundtrip();
            this.value.Real32A = new float[] { 3.14f, 2.79f, -5.1f };
            this.TestValueRoundtrip();

            this.value.Real64 = .00001;
            this.TestValueRoundtrip();
            this.value.Real64A = new double[] { double.MaxValue, 8, double.MinValue };
            this.TestValueRoundtrip();
        }

        [Fact]
        public void MIValue_InstanceTypes_Test()
        {
            MI_Instance InnerInstance = null;
            var res = this.application.NewInstance("TestClass", MI_ClassDecl.Null, out InnerInstance);
            MIAssert.Succeeded(res);

            MI_Instance InnerInstance2 = null;
            res = this.application.NewInstance("TestClass", MI_ClassDecl.Null, out InnerInstance2);
            MIAssert.Succeeded(res);

            try
            {
                MI_Value innerValue1 = new MI_Value();
                innerValue1.String = "This is a property";
                res = InnerInstance.AddElement("InnerInstanceProperty1", innerValue1, innerValue1.Type.Value, MI_Flags.MI_FLAG_BORROW);
                MIAssert.Succeeded(res);

                MI_Value innerValue2 = new MI_Value();
                innerValue2.String = "This is another property";
                res = InnerInstance.AddElement("InnerInstanceProperty2", innerValue2, innerValue2.Type.Value, MI_Flags.MI_FLAG_BORROW);
                MIAssert.Succeeded(res);

                MI_Value innerValue3 = new MI_Value();
                innerValue3.String = "Still another property";
                res = InnerInstance2.AddElement("InnerInstance2Property1", innerValue3, innerValue3.Type.Value, MI_Flags.MI_FLAG_BORROW);
                MIAssert.Succeeded(res);

                MI_Value innerValue4 = new MI_Value();
                innerValue4.String = "Okay, bored now";
                res = InnerInstance2.AddElement("InnerInstance2Property2", innerValue4, innerValue4.Type.Value, MI_Flags.MI_FLAG_BORROW);
                MIAssert.Succeeded(res);

                this.value.Instance = InnerInstance;
                this.TestValueRoundtrip();

                this.value.InstanceA = new MI_Instance[] { InnerInstance, InnerInstance2 };
                this.TestValueRoundtrip();
            }
            finally
            {
                InnerInstance.Delete();
                InnerInstance2.Delete();
            }
        }

        [Fact]
        public void MIValue_IntegerTypes_Test()
        {
            this.value.Sint8 = 64;
            this.TestValueRoundtrip();
            this.value.Sint8A = new sbyte[] { -64, SByte.MaxValue };
            this.TestValueRoundtrip();

            this.value.Uint8 = 52;
            this.TestValueRoundtrip();
            this.value.Uint8A = new byte[] { 3, Byte.MaxValue };
            this.TestValueRoundtrip();

            this.value.Sint16 = -10;
            this.TestValueRoundtrip();
            this.value.Sint16A = new Int16[] { -20, Int16.MinValue };
            this.TestValueRoundtrip();

            this.value.Uint16 = 400;
            this.TestValueRoundtrip();
            this.value.Uint16A = new UInt16[] { UInt16.MaxValue, UInt16.MinValue };
            this.TestValueRoundtrip();

            this.value.Sint32 = -365;
            this.TestValueRoundtrip();
            this.value.Sint32A = new Int32[] { Int32.MaxValue, -400 };
            this.TestValueRoundtrip();

            this.value.Sint64 = Int64.MinValue;
            this.TestValueRoundtrip();
            this.value.Sint64A = new Int64[] { Int64.MaxValue, Int64.MinValue };
            this.TestValueRoundtrip();

            this.value.Uint32 = 487;
            this.TestValueRoundtrip();
            this.value.Uint32A = new UInt32[] { UInt16.MaxValue, UInt32.MaxValue };
            this.TestValueRoundtrip();

            this.value.Uint64 = 487000;
            this.TestValueRoundtrip();
            this.value.Uint64A = new UInt64[] { 80, UInt64.MaxValue };
            this.TestValueRoundtrip();
        }

        [Fact]
        public void MIValue_IntervalTypes_Test()
        {
            this.value.Datetime = new MI_Datetime()
            {
                isTimestamp = false,
                interval = new MI_Interval()
                {
                    days = 3,
                    hours = 4,
                    seconds = 5
                }
            };
            this.TestValueRoundtrip();

            this.value.DatetimeA = new MI_Datetime[] {
                new MI_Datetime()
                {
                    isTimestamp = false,
                    interval = new MI_Interval()
                    {
                        microseconds = 3
                    }
                },
                new MI_Datetime()
                {
                    isTimestamp = false,
                    interval = new MI_Interval()
                    {
                        minutes = 56,
                        microseconds = 2
                    }
                }
            };

            this.TestValueRoundtrip();
        }

        [Fact]
        public void MIValue_ReferenceTypes_Test()
        {
            MI_Instance InnerInstance = null;
            var res = this.application.NewInstance("TestClass", MI_ClassDecl.Null, out InnerInstance);
            MIAssert.Succeeded(res);

            MI_Instance InnerInstance2 = null;
            res = this.application.NewInstance("TestClass", MI_ClassDecl.Null, out InnerInstance2);
            MIAssert.Succeeded(res);

            try
            {
                MI_Value innerValue1 = new MI_Value();
                innerValue1.String = "This is a property";
                res = InnerInstance.AddElement("InnerInstanceProperty1", innerValue1, innerValue1.Type.Value, MI_Flags.MI_FLAG_BORROW);
                MIAssert.Succeeded(res);

                MI_Value innerValue2 = new MI_Value();
                innerValue2.String = "This is another property";
                res = InnerInstance.AddElement("InnerInstanceProperty2", innerValue2, innerValue2.Type.Value, MI_Flags.MI_FLAG_BORROW);
                MIAssert.Succeeded(res);

                MI_Value innerValue3 = new MI_Value();
                innerValue3.String = "Still another property";
                res = InnerInstance2.AddElement("InnerInstance2Property1", innerValue3, innerValue3.Type.Value, MI_Flags.MI_FLAG_BORROW);
                MIAssert.Succeeded(res);

                MI_Value innerValue4 = new MI_Value();
                innerValue4.String = "Okay, bored now";
                res = InnerInstance2.AddElement("InnerInstance2Property2", innerValue4, innerValue4.Type.Value, MI_Flags.MI_FLAG_BORROW);
                MIAssert.Succeeded(res);

                this.value.Reference = InnerInstance;
                this.TestValueRoundtrip();

                this.value.ReferenceA = new MI_Instance[] { InnerInstance, InnerInstance2 };
                this.TestValueRoundtrip();
            }
            finally
            {
                InnerInstance.Delete();
                InnerInstance2.Delete();
            }
        }

        [Fact]
        public void MIValue_StringTypes_Test()
        {
            const string expectedString = "Foobar";
            string[] expectedStrings = new string[] { "Foobar", "Bazzity" };

            this.value.String = expectedString;
            this.TestValueRoundtrip();
            this.value.StringA = expectedStrings;
            this.TestValueRoundtrip();
        }

        [Fact]
        public void MIValue_PreventsBadCast_Test()
        {
            this.value.String = "Foobar";
            foreach(MI_Type enumValue in Enum.GetValues(typeof(MI_Type)))
            {
                if(enumValue != MI_Type.MI_STRING)
                {
                    Exception ex1 = Assert.Throws<InvalidCastException>(()=>this.value.GetValue(enumValue));
                    string expectedMessage1 = "Specified cast is not valid.";
                    string expectedMessage2 = String.Format("Expect the MI_Value to protect its data for {0}", enumValue.ToString());
                    string[] expectedErrors = new string[] { expectedMessage1, expectedMessage2 };

                    Assert.Contains(ex1.Message.ToString(), expectedErrors);
                }
            }

            this.value.Uint8 = 5;
            Exception ex2 = Assert.Throws<InvalidCastException>(()=>this.value.GetValue(MI_Type.MI_STRING));
            string expectedMessage3 = String.Format("Expect the MI_Value to protect its data for {0}", MI_Type.MI_STRING.ToString());
            Assert.Equal(ex2.Message, "Specified cast is not valid.");
        }
    }
}
