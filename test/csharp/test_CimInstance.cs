using Xunit;
using System;
using Microsoft.Management.Infrastructure;

namespace Microsoft.PowerShell.UnitTest
{
// CimInstance
    public static class CimInstanceTests
    {
        [Fact]
        public static void TestCimInstanceLifeCycle()
        {
            string expected = "Cannot access a disposed object.\nObject name: 'Microsoft.Management.Infrastructure.CimInstance'.";
            CimInstance actual = new CimInstance("MyClass");
            Assert.True(null != actual);

            actual.Dispose();

            Exception ex = Assert.Throws<System.ObjectDisposedException>(() => actual.CimInstanceProperties);
            Assert.Equal(expected, ex.Message);
        }

        [Fact]
        public static void TestCimInstanceCanAccessAndToString()
        {
            CimInstance c = new CimInstance("TestWithPropertiesClass");
            uint v = 23;
            CimFlags f = CimFlags.Any;

            c.CimInstanceProperties.Add(CimProperty.Create("Prop1", v, f));

            Assert.Equal("Prop1 = 23", c.CimInstanceProperties["Prop1"].ToString());

            c.Dispose();
        }

        [Fact]
        public static void TestCimInstanceCanAccessBooleanFalse()
        {
            CimInstance c = new CimInstance("TestWithPropertiesClass");
            bool v = false;
            CimFlags f = CimFlags.Any;

            c.CimInstanceProperties.Add(CimProperty.Create("Prop1", v, f));

            string actual = string.Format("Prop1 = {0}", v);
            Assert.Equal(actual, c.CimInstanceProperties["Prop1"].ToString());

            c.Dispose();
        }

        [Fact]
        public static void TestCimInstanceCanAccessBooleanTrue()
        {
            CimInstance c = new CimInstance("TestWithPropertiesClass");
            bool v = true;
            CimFlags f = CimFlags.Any;

            c.CimInstanceProperties.Add(CimProperty.Create("Prop1", v, f));

            string actual = string.Format("Prop1 = {0}", v);
            Assert.Equal(actual, c.CimInstanceProperties["Prop1"].ToString());

            c.Dispose();
        }

        [Fact]
        public static void TestCimInstanceCanAccessBytes()
        {
            Byte v = 255;
            CimFlags f = CimFlags.Any;
            CimInstance c = new CimInstance("TestWithPropertiesClass");

            c.CimInstanceProperties.Add(CimProperty.Create("Property1", v, f));

            string actual = string.Format("{0} = {1}", "Property1", v);
            Assert.Equal(actual, c.CimInstanceProperties["Property1"].ToString());

            c.Dispose();
        }

        [Fact]
        public static void TestCimInstanceCanAccessShortBytes()
        {
            SByte v = -127;
            CimFlags f = CimFlags.Any;
            CimInstance c = new CimInstance("TestWithPropertiesClass");

            c.CimInstanceProperties.Add(CimProperty.Create("Property1", v, f));

            string actual = string.Format("{0} = {1}", "Property1", v);
            Assert.Equal(actual, c.CimInstanceProperties["Property1"].ToString());

            c.Dispose();
        }

        [Fact]
        public static void TestCimInstanceCanAccessInt32()
        {
            UInt32 v = 4294967295;
            CimFlags f = CimFlags.Any;
            CimInstance c = new CimInstance("TestWithPropertiesClass");

            c.CimInstanceProperties.Add(CimProperty.Create("Property1", v, f));

            string actual = string.Format("{0} = {1}", "Property1", v);
            Assert.Equal(actual, c.CimInstanceProperties["Property1"].ToString());

            c.Dispose();
        }

        [Fact]
        public static void TestCimInstanceCanAccessInt16()
        {
            Int16 v = 32766;
            CimFlags f = CimFlags.Any;
            CimInstance c = new CimInstance("TestWithPropertiesClass");

            c.CimInstanceProperties.Add(CimProperty.Create("Property1", v, f));

            string actual = string.Format("{0} = {1}", "Property1", v);
            Assert.Equal(actual, c.CimInstanceProperties["Property1"].ToString());

            c.Dispose();
        }

        [Fact]
        public static void TestCimInstanceCanAccessInt16Min()
        {
            Int16 v = -32768;
            CimFlags f = CimFlags.Any;
            CimInstance c = new CimInstance("TestWithPropertiesClass");

            c.CimInstanceProperties.Add(CimProperty.Create("Property1", v, f));

            string actual = string.Format("{0} = {1}", "Property1", v);
            Assert.Equal(actual, c.CimInstanceProperties["Property1"].ToString());

            c.Dispose();
        }

        [Fact]
        public static void TestCimInstanceCanAccessReal64()
        {
            double v = 9.9D;
            CimFlags f = CimFlags.Any;
            CimInstance c = new CimInstance("TestWithPropertiesClass");

            c.CimInstanceProperties.Add(CimProperty.Create("Property1", v, f));

            string actual = string.Format("{0} = {1}", "Property1", v);
            Assert.Equal(actual, c.CimInstanceProperties["Property1"].ToString());

            c.Dispose();
        }
        //[Fact]
        //public static void TestCimInstanceCanAccessStringMiValues()
        //{
        //    CimInstance c = new CimInstance("TestWithPropertiesClass");

        //    string v = "testString";
        //    CimFlags f = CimFlags.Any;

        //    c.CimInstanceProperties.Add(CimProperty.Create("Prop1", v, f));

        //    Assert.Equal("Prop1 = \"testString\"", c.CimInstanceProperties["Prop1"].ToString());
        //}
    }
}
