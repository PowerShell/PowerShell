// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using Xunit;

namespace PSTests.Parallel
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1121:UseBuiltInTypeAlias", Justification = "Intentionally not using built-in alias.")]
    public static class PSObjectIConvertibleTests
    {
        [Fact]
        public static void TestBool()
        {
            var value = (object)true;
            var wrappedValue = new PSObject(true);
            Assert.Equal(value, wrappedValue.ToBoolean(provider: null));
            Assert.Equal(typeof(bool).GetTypeCode(), wrappedValue.GetTypeCode());
        }

        [Fact]
        public static void TestChar()
        {
            var value = (object)'K';
            var wrappedValue = new PSObject(value);
            Assert.Equal(value, wrappedValue.ToChar(provider: null));
            Assert.Equal(typeof(char).GetTypeCode(), wrappedValue.GetTypeCode());
        }

        [Fact]
        public static void TestToSByte()
        {
            var value = (object)(SByte)1;
            var wrappedValue = new PSObject(value);
            Assert.Equal(value, wrappedValue.ToSByte(provider: null));
            Assert.Equal(typeof(SByte).GetTypeCode(), wrappedValue.GetTypeCode());
        }

        [Fact]
        public static void TestToByte()
        {
            var value = (object)(Byte)1;
            var wrappedValue = new PSObject(value);
            Assert.Equal(value, wrappedValue.ToByte(provider: null));
            Assert.Equal(typeof(Byte).GetTypeCode(), wrappedValue.GetTypeCode());
        }

        [Fact]
        public static void TestToInt16()
        {
            var value = (object)(Int16)1;
            var wrappedValue = new PSObject(value);
            Assert.Equal(value, wrappedValue.ToInt16(provider: null));
            Assert.Equal(typeof(Int16).GetTypeCode(), wrappedValue.GetTypeCode());
        }

        [Fact]
        public static void TestToUInt16()
        {
            var value = (object)(UInt16)1;
            var wrappedValue = new PSObject(value);
            Assert.Equal(value, wrappedValue.ToUInt16(provider: null));
            Assert.Equal(typeof(UInt16).GetTypeCode(), wrappedValue.GetTypeCode());
        }

        [Fact]
        public static void TestToInt32()
        {
            var value = (object)(Int32)1;
            var wrappedValue = new PSObject(value);
            Assert.Equal(value, wrappedValue.ToInt32(provider: null));
            Assert.Equal(typeof(Int32).GetTypeCode(), wrappedValue.GetTypeCode());
        }

        [Fact]
        public static void TestToUInt32()
        {
            var value = (object)(UInt32)1;
            var wrappedValue = new PSObject(value);
            Assert.Equal(value, wrappedValue.ToUInt32(provider: null));
            Assert.Equal(typeof(UInt32).GetTypeCode(), wrappedValue.GetTypeCode());
        }

        [Fact]
        public static void TestToInt64()
        {
            var value = (object)(Int64)1;
            var wrappedValue = new PSObject(value);
            Assert.Equal(value, wrappedValue.ToInt64(provider: null));
            Assert.Equal(typeof(Int64).GetTypeCode(), wrappedValue.GetTypeCode());
        }

        [Fact]
        public static void TestToUInt64()
        {
            var value = (object)(UInt64)1;
            var wrappedValue = new PSObject(value);
            Assert.Equal(value, wrappedValue.ToUInt64(provider: null));
            Assert.Equal(typeof(UInt64).GetTypeCode(), wrappedValue.GetTypeCode());
        }

        [Fact]
        public static void TestToSingle()
        {
            var value = (object)(Single)1;
            var wrappedValue = new PSObject(value);
            Assert.Equal(value, wrappedValue.ToSingle(provider: null));
            Assert.Equal(typeof(Single).GetTypeCode(), wrappedValue.GetTypeCode());
        }

        [Fact]
        public static void TestToDouble()
        {
            var value = (object)(Double)1;
            var wrappedValue = new PSObject(value);
            Assert.Equal(value, wrappedValue.ToDouble(provider: null));
            Assert.Equal(typeof(Double).GetTypeCode(), wrappedValue.GetTypeCode());
        }

        [Fact]
        public static void TestToDecimal()
        {
            var value = (object)(Decimal)1;
            var wrappedValue = new PSObject(value);
            Assert.Equal(value, wrappedValue.ToDecimal(provider: null));
            Assert.Equal(typeof(Decimal).GetTypeCode(), wrappedValue.GetTypeCode());
        }

        [Fact]
        public static void TestToDateTime()
        {
            var value = (object)DateTime.UtcNow;
            var wrappedValue = new PSObject(value);
            Assert.Equal(value, wrappedValue.ToDateTime(provider: null));
            Assert.Equal(typeof(DateTime).GetTypeCode(), wrappedValue.GetTypeCode());
        }

        [Fact]
        public static void TestToString()
        {
            var value = (object)"1";
            var wrappedValue = new PSObject(value);
            Assert.Equal(value, wrappedValue.ToString(provider: null));
            Assert.Equal(typeof(String).GetTypeCode(), wrappedValue.GetTypeCode());
        }

        [Fact]
        public static void TestToType()
        {
            var value = (object)1;
            var wrappedValue = new PSObject(value);
            Assert.Equal(typeof(bool), wrappedValue.ToType(typeof(bool), provider: null).GetType());
            Assert.Equal(typeof(char), wrappedValue.ToType(typeof(char), provider: null).GetType());
            Assert.Equal(typeof(SByte), wrappedValue.ToType(typeof(SByte), provider: null).GetType());
            Assert.Equal(typeof(Byte), wrappedValue.ToType(typeof(Byte), provider: null).GetType());
            Assert.Equal(typeof(Int16), wrappedValue.ToType(typeof(Int16), provider: null).GetType());
            Assert.Equal(typeof(UInt16), wrappedValue.ToType(typeof(UInt16), provider: null).GetType());
            Assert.Equal(typeof(Int32), wrappedValue.ToType(typeof(Int32), provider: null).GetType());
            Assert.Equal(typeof(UInt32), wrappedValue.ToType(typeof(UInt32), provider: null).GetType());
            Assert.Equal(typeof(Int64), wrappedValue.ToType(typeof(Int64), provider: null).GetType());
            Assert.Equal(typeof(UInt64), wrappedValue.ToType(typeof(UInt64), provider: null).GetType());
            Assert.Equal(typeof(Single), wrappedValue.ToType(typeof(Single), provider: null).GetType());
            Assert.Equal(typeof(Double), wrappedValue.ToType(typeof(Double), provider: null).GetType());
            Assert.Equal(typeof(Decimal), wrappedValue.ToType(typeof(Decimal), provider: null).GetType());
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToType(typeof(DateTime), provider: null));
            Assert.Equal(typeof(String), wrappedValue.ToType(typeof(String), provider: null).GetType());
        }

        [Fact]
        public static void TestNoLoopAndStackOverflow()
        {
            // PSObject.Base(wrappedValue) returns PSObject.
            // It is infinite loop.
            // so PSObject IConvertible implementation should throw
            // with InvalidCastException instead of StackOverflowException.
            var wrappedValue = new PSObject();
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToBoolean(provider: null));
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToChar(provider: null));
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToSByte(provider: null));
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToByte(provider: null));
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToInt16(provider: null));
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToUInt16(provider: null));
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToInt32(provider: null));
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToUInt32(provider: null));
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToInt64(provider: null));
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToUInt64(provider: null));
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToSingle(provider: null));
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToDouble(provider: null));
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToDecimal(provider: null));
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToDateTime(provider: null));
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToString(provider: null));

            Assert.Throws<InvalidCastException>(() => wrappedValue.ToType(typeof(bool), provider: null));
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToType(typeof(char), provider: null));
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToType(typeof(SByte), provider: null));
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToType(typeof(Byte), provider: null));
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToType(typeof(Int16), provider: null));
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToType(typeof(UInt16), provider: null));
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToType(typeof(Int32), provider: null));
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToType(typeof(UInt32), provider: null));
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToType(typeof(Int64), provider: null));
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToType(typeof(UInt64), provider: null));
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToType(typeof(Single), provider: null));
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToType(typeof(Double), provider: null));
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToType(typeof(Decimal), provider: null));
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToType(typeof(DateTime), provider: null));
            Assert.Throws<InvalidCastException>(() => wrappedValue.ToType(typeof(String), provider: null));

            Assert.Equal(TypeCode.Object, wrappedValue.GetTypeCode());
        }
    }
}
