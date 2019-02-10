using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Xunit;
using Microsoft.PowerShell.Commands;

using ipCsv = Microsoft.PowerShell.Commands.ImportCsvCommand;

namespace PSTests.Parallel
{

    class TestClassWithCustomCtor
    {
        public TestClassWithCustomCtor(int firstParam, string secondParam)
        {
            A = firstParam;
            B = secondParam;
            C = "ConstructorCalled";
        }

        public int A { get; set; }
        public string B { get; set; }
        public string C { get; set; }
    }

    class TestClassWithDefaultCtor
    {
        public string Text { get; set; }
        public int Integer { get; set; }
        public DateTime Date { get; set; }
    }

    public class ClassPropertySetterTests : IDisposable
    {
        private readonly string _csvFile;
        private readonly DateTime _expectedDate = new DateTime(2016, 12, 24);
        public ClassPropertySetterTests()
        {

            _csvFile = Path.GetTempFileName();
            File.WriteAllText(_csvFile,
@"Integer,Text,Date
1,Some text,2016-12-24
2,Some other text,2016/12/24
3,More text,12/24/2016
");
        }

        void IDisposable.Dispose()
        {
            File.Delete(_csvFile);
        }

        [Fact]
        public void TestImportCsv()
        {
            var iss = InitialSessionState.CreateDefault2();
            iss.Commands.Add(new SessionStateCmdletEntry("Import-Csv", typeof(Microsoft.PowerShell.Commands.ImportCsvCommand), null));

            IList<TestClassWithDefaultCtor> result = null;
            using (var powerShell = PowerShell.Create(iss))
            {
                powerShell.AddCommand("Import-Csv")
                    .AddParameters(new Dictionary<string, object>
                    {
                        { nameof(ipCsv.Path), _csvFile},
                        { nameof(ipCsv.Delimiter), ","},
                        { nameof(ipCsv.ResultType), typeof(TestClassWithDefaultCtor)},
                    });

                result = powerShell.Invoke<TestClassWithDefaultCtor>().ToList();
            }

            Assert.Equal(3, result.Count);

            void AssertResult(TestClassWithDefaultCtor res, int i, DateTime dateTime, string text)
            {
                Assert.Equal(dateTime, res.Date);
                Assert.Equal(text, res.Text);
                Assert.Equal(i, res.Integer);
            }

            AssertResult(result[0], 1, _expectedDate, "Some text");
            AssertResult(result[1], 2, _expectedDate, "Some other text");
            AssertResult(result[2], 3, _expectedDate, "More text");
        }

        [Fact]
        public static void TestCreteObjectCreator()
        {
            var creatorFunc = ClassPropertySetter.GetCreatorDelegate(typeof(TestClassWithCustomCtor), new[]{"FirstParam", "SecondParam" }, useCurrentCultureInTypeConversion: true);
            Assert.NotNull(creatorFunc);
        }

        [Fact]
        public static void TestCreateObjectWithCtor()
        {
            var creatorFunc = ClassPropertySetter.GetCreatorDelegate(typeof(TestClassWithCustomCtor), new[] { "FirstParam", "SecondParam" }, useCurrentCultureInTypeConversion: true);
            var obj = creatorFunc(new[] {"10", "a string"});
            Assert.NotNull(obj);
            Assert.Equal(typeof(TestClassWithCustomCtor), obj.GetType());

            var testObj = (TestClassWithCustomCtor)obj;
            Assert.Equal("a string", testObj.B);
            Assert.Equal(10, testObj.A);
            Assert.Equal("ConstructorCalled", testObj.C);
        }

        [Fact]
        public static void TestCreateObjectWitCtorReversedParamOrder()
        {
            var creatorFunc = ClassPropertySetter.GetCreatorDelegate(typeof(TestClassWithCustomCtor), new[] { "SecondParam", "FirstParam" }, useCurrentCultureInTypeConversion: true);
            var obj = creatorFunc(new[] { "a string", "10" });
            Assert.NotNull(obj);
            Assert.Equal(typeof(TestClassWithCustomCtor), obj.GetType());

            var testObj = (TestClassWithCustomCtor)obj;
            Assert.Equal("a string", testObj.B);
            Assert.Equal(10, testObj.A);
            Assert.Equal("ConstructorCalled", testObj.C);
        }


        [Fact]
        public static void TestCreateObjectBySettingProperties()
        {
            var creatorFunc = ClassPropertySetter.GetCreatorDelegate(typeof(TestClassWithDefaultCtor), new[] { "Text", "Integer", "Date" }, useCurrentCultureInTypeConversion: true);
            var obj = creatorFunc(new[] { "C", "0xA", "2016-12-01" });
            Assert.NotNull(obj);
            Assert.Equal(typeof(TestClassWithDefaultCtor), obj.GetType());

            var testObj = (TestClassWithDefaultCtor)obj;
            Assert.Equal("C", testObj.Text);
            Assert.Equal(10, testObj.Integer);
            Assert.Equal(new DateTime(2016,12,01), testObj.Date);
        }

        [Fact]
        public static void TestCreateObjectBySettingPropertiesInScrambledOrder()
        {
            var creatorFunc = ClassPropertySetter.GetCreatorDelegate(
                typeof(TestClassWithDefaultCtor),
                new[] {  "Integer", "Date", "Text" },
                useCurrentCultureInTypeConversion: true);
            var obj = creatorFunc(new[] { "0xA", "2016-12-01", "C" });
            Assert.NotNull(obj);
            Assert.Equal(typeof(TestClassWithDefaultCtor), obj.GetType());

            var testObj = (TestClassWithDefaultCtor)obj;
            Assert.Equal("C", testObj.Text);
            Assert.Equal(10, testObj.Integer);
            Assert.Equal(new DateTime(2016, 12, 01), testObj.Date);
        }

        [Fact]
        public static void TestCreateObjectBySettingFewerProperties()
        {
            var creatorFunc = ClassPropertySetter.GetCreatorDelegate(typeof(TestClassWithDefaultCtor), new[] { "Text", "Integer"}, useCurrentCultureInTypeConversion: true);
            var obj = creatorFunc(new[] { "C", "0xA", "2016-12-01" });
            Assert.NotNull(obj);
            Assert.Equal(typeof(TestClassWithDefaultCtor), obj.GetType());

            var testObj = (TestClassWithDefaultCtor)obj;
            Assert.Equal("C", testObj.Text);
            Assert.Equal(10, testObj.Integer);
            Assert.Equal(DateTime.MinValue, testObj.Date);
        }

        [Fact]
        public static void TestCreateObjectBySettingMoreProperties()
        {
            Assert.Throws<ArgumentException>(() =>
                ClassPropertySetter.GetCreatorDelegate(
                    typeof(TestClassWithDefaultCtor),
                    new[] { "Text", "Integer", "Date", "ToMany" },
                    useCurrentCultureInTypeConversion: true));
        }
    }
}
