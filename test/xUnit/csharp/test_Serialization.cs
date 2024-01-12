// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Xunit;

namespace PSTests.Parallel
{
    public static class SerializationTests
    {
        [Fact]
        public static void TestSerializerEnumerate()
        {
            var source = new List<object> { 1, 2, 3 };
            var expected = $"<Objs Version=\"1.1.0.1\" xmlns=\"http://schemas.microsoft.com/powershell/2004/04\">{Environment.NewLine}  <I32>1</I32>{Environment.NewLine}  <I32>2</I32>{Environment.NewLine}  <I32>3</I32>{Environment.NewLine}</Objs>";
            var serialized = PSSerializer.Serialize(source, depth: 2, enumerate: true);
            Assert.Equal(expected, serialized);
            var deserialized = PSSerializer.Deserialize(serialized);
            Assert.IsType<object[]>(deserialized);
            var array = ((IEnumerable)deserialized).Cast<object>().ToArray();
            Assert.Equal(3, array.Length);
            Assert.Equal(1, array[0]);
            Assert.Equal(2, array[1]);
            Assert.Equal(3, array[2]);
        }

        [Fact]
        public static void TestSerializerWithoutEnumerate()
        {
            var source = new List<object> { 1, 2, 3 };
            var expected = $"<Objs Version=\"1.1.0.1\" xmlns=\"http://schemas.microsoft.com/powershell/2004/04\">{Environment.NewLine}  <Obj RefId=\"0\">{Environment.NewLine}    <TN RefId=\"0\">{Environment.NewLine}      <T>System.Collections.Generic.List`1[[System.Object, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]</T>{Environment.NewLine}      <T>System.Object</T>{Environment.NewLine}    </TN>{Environment.NewLine}    <LST>{Environment.NewLine}      <I32>1</I32>{Environment.NewLine}      <I32>2</I32>{Environment.NewLine}      <I32>3</I32>{Environment.NewLine}    </LST>{Environment.NewLine}  </Obj>{Environment.NewLine}</Objs>";
            var serialized = PSSerializer.Serialize(source, depth: 2, enumerate: false);
            Assert.Equal(expected, serialized);
            var deserialized = PSSerializer.Deserialize(serialized);
            Assert.IsType<PSObject>(deserialized);
            var baseObject = PSObject.AsPSObject(deserialized).BaseObject;
            Assert.IsType<ArrayList>(baseObject);
            var arrayList = (ArrayList)baseObject;
            Assert.Equal(3, arrayList.Count);
            Assert.Equal(1, arrayList[0]);
            Assert.Equal(2, arrayList[1]);
            Assert.Equal(3, arrayList[2]);
        }
    }
}
