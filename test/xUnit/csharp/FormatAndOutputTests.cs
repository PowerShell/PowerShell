// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using Microsoft.PowerShell.Commands;
using Microsoft.PowerShell.Commands.Internal.Format;
using Xunit;

namespace PSTests.Parallel
{
    public static class FormatAndOutputTests
    {
        [Fact]
        public static void OutOfBandLookupTracksTheEntireMutableHierarchy()
        {
            var database = new TypeInfoDataBase();
            ViewDefinition first = AddOutOfBandView(database, "First");
            ViewDefinition second = AddOutOfBandView(database, "Second");
            var manager = new OutOfBandFormatViewManager();
            var factory = new PSPropertyExpressionFactory();
            var names = new Collection<string> { "First", "Second" };

            Assert.Same(first, manager.GetOutOfBandView(factory, database, names));
            Assert.Same(first, manager.GetOutOfBandView(factory, database, names));
            names[0] = "Unknown";
            Assert.Same(second, manager.GetOutOfBandView(factory, database, names));
            names[1] = "Unknown";
            Assert.Null(manager.GetOutOfBandView(factory, database, names));
            names[1] = "fIrSt";
            Assert.Same(first, manager.GetOutOfBandView(factory, database, names));
            names[0] = "Second";
            Assert.Same(second, manager.GetOutOfBandView(factory, database, names));
        }

        [Fact]
        public static void OutOfBandLookupInvalidatesHitsAndMissesOnDatabaseReplacement()
        {
            var manager = new OutOfBandFormatViewManager();
            var factory = new PSPropertyExpressionFactory();
            var names = new Collection<string> { "Deserialized.Example" };
            var database = new TypeInfoDataBase();
            Assert.Null(manager.GetOutOfBandView(factory, database, names));
            Assert.Null(manager.GetOutOfBandView(factory, database, names));

            var replacement = new TypeInfoDataBase();
            ViewDefinition view = AddOutOfBandView(replacement, "Example");
            Assert.Same(view, manager.GetOutOfBandView(factory, replacement, names));
            Assert.Null(manager.GetOutOfBandView(factory, database, names));
        }

        [Fact]
        public static void ConditionalOutOfBandLookupIsNotCached()
        {
            using var runspace = RunspaceFactory.CreateRunspace(InitialSessionState.CreateDefault2());
            runspace.Open();
            Runspace previous = Runspace.DefaultRunspace;
            Runspace.DefaultRunspace = runspace;
            try
            {
                runspace.SessionStateProxy.SetVariable("formatLookupCount", 0);
                var database = new TypeInfoDataBase();
                ViewDefinition view = AddOutOfBandView(database, "Example");
                view.appliesTo.referenceList[0].conditionToken = new ExpressionToken
                {
                    isScriptBlock = true,
                    expressionValue = "$global:formatLookupCount++; $true",
                };
                var factory = new PSPropertyExpressionFactory();
                var manager = new OutOfBandFormatViewManager();
                var names = new Collection<string> { "Example" };
                Assert.Same(view, manager.GetOutOfBandView(factory, database, names));
                Assert.Same(view, manager.GetOutOfBandView(factory, database, names));
                Assert.Equal(2, runspace.SessionStateProxy.GetVariable("formatLookupCount"));
            }
            finally
            {
                Runspace.DefaultRunspace = previous;
            }
        }

        [Fact]
        public static void NamedExpressionTokensReuseTheExpression()
        {
            var factory = new PSPropertyExpressionFactory();
            var token = new ExpressionToken { expressionValue = "Name" };
            Assert.Same(factory.CreateFromExpressionToken(token), factory.CreateFromExpressionToken(token));
        }

        [Fact]
        public static void FormattingPropertyExpressionReusesBindingWithoutReusingValues()
        {
            var expression = new PSPropertyExpression("Name");
            var first = new PSObject();
            first.Properties.Add(new PSNoteProperty("Name", "first"));
            var second = new PSObject();
            second.Properties.Add(new PSNoteProperty("Name", "second"));
            PSPropertyExpressionResult firstResult = expression.GetValueForFormatting(first);
            PSPropertyExpressionResult secondResult = expression.GetValueForFormatting(second);
            Assert.Equal("first", firstResult.Result);
            Assert.Equal("second", secondResult.Result);
            Assert.Same(firstResult.ResolvedExpression, secondResult.ResolvedExpression);

            second.Properties["Name"].Value = "changed";
            Assert.Equal("changed", expression.GetValueForFormatting(second).Result);
            second.Properties.Remove("Name");
            Assert.Null(expression.GetValueForFormatting(second));
            second.Properties.Add(new PSNoteProperty("NAME", "new casing"));
            PSPropertyExpressionResult renamed = expression.GetValueForFormatting(second);
            Assert.Equal("new casing", renamed.Result);
            Assert.Equal("NAME", renamed.ResolvedExpression.ToString());
        }

        [Fact]
        public static void FormattingPropertyExpressionPreservesExpansionAndHashtableFallback()
        {
            var target = new PSObject();
            target.Properties.Add(new PSNoteProperty("Alpha", 1));
            target.Properties.Add(new PSNoteProperty("Beta", 2));
            target.Members.Add(new PSPropertySet("Pair", new[] { "Alpha", "Beta" }));
            foreach (string name in new[] { "*", "Pair" })
            {
                var expression = new PSPropertyExpression(name);
                Assert.Equal(expression.GetValues(target)[0].Result, expression.GetValueForFormatting(target).Result);
            }

            var hashtable = new PSObject(new Hashtable { { "Count", 42 } });
            foreach (string name in new[] { "Count", "Keys", "Missing" })
            {
                var expression = new PSPropertyExpression(name);
                var results = expression.GetValues(hashtable);
                PSPropertyExpressionResult actual = expression.GetValueForFormatting(hashtable);
                if (results.Count == 0)
                {
                    Assert.Null(actual);
                }
                else
                {
                    Assert.Equal(results[0].Result, actual.Result);
                    Assert.Equal(results[0].Exception?.GetType(), actual.Exception?.GetType());
                }
            }
        }

        [Fact]
        public static void FormattingWildcardEvaluatesAllGetters()
        {
            var value = new CountingProperties();
            var expression = new PSPropertyExpression("*");
            expression.GetValueForFormatting(new PSObject(value));
            Assert.Equal(2, value.ReadCount);
        }

        [Fact]
        public static void FormattingScriptPropertiesAndExpressionsKeepTheirSideEffects()
        {
            using var runspace = RunspaceFactory.CreateRunspace(InitialSessionState.CreateDefault2());
            runspace.Open();
            Runspace previous = Runspace.DefaultRunspace;
            Runspace.DefaultRunspace = runspace;
            try
            {
                runspace.SessionStateProxy.SetVariable("formatEvaluationCount", 0);
                ScriptBlock getter = ScriptBlock.Create("$global:formatEvaluationCount++; $global:formatEvaluationCount");
                var target = new PSObject();
                target.Properties.Add(new PSScriptProperty("Name", getter));
                target.Properties.Add(new PSScriptProperty("Other", getter));
                target.Members.Add(new PSPropertySet("Pair", new[] { "Name", "Other" }));

                var expression = new PSPropertyExpression("Name");
                Assert.Equal(1, PSObject.Base(expression.GetValueForFormatting(target).Result));
                Assert.Equal(2, PSObject.Base(expression.GetValueForFormatting(target).Result));
                Assert.Equal(3, PSObject.Base(new PSPropertyExpression("Pair").GetValueForFormatting(target).Result));
                Assert.Equal(4, runspace.SessionStateProxy.GetVariable("formatEvaluationCount"));
                Assert.Equal(5, PSObject.Base(new PSPropertyExpression(getter).GetValueForFormatting(target).Result));

                var failing = new PSPropertyExpression(ScriptBlock.Create("throw 'formatting failed'"));
                Assert.IsAssignableFrom<RuntimeException>(failing.GetValueForFormatting(target).Exception);
            }
            finally
            {
                Runspace.DefaultRunspace = previous;
            }
        }

        [Fact]
        public static void FormattingPropertyExpressionPreservesDynamicAndGetterErrors()
        {
            var dynamicTarget = new PSObject(new DynamicProperties());
            var expression = new PSPropertyExpression("Unknown");
            Assert.Equal("Unknown", expression.GetValueForFormatting(dynamicTarget).Result);

            var throwingTarget = new PSObject(new ThrowingProperty());
            expression = new PSPropertyExpression("Value");
            PSPropertyExpressionResult result = expression.GetValueForFormatting(throwingTarget);
            Assert.Null(result.Result);
            Assert.Equal(expression.GetValues(throwingTarget)[0].Exception?.GetType(), result.Exception?.GetType());
        }

        [Theory]
        [InlineData("plain text")]
        [InlineData("\x1b[32mstyled\x1b[0m")]
        [InlineData("\x1b[?25lhidden\x1b[?25h")]
        [InlineData("\x1b]8;;https://example.com\x1b\\link\x1b]8;;\x1b\\")]
        [InlineData("malformed\x1b[32")]
        [InlineData("\u4e2d\x1b[31m\u6587\x1b[0m")]
        [InlineData("\x1b[31m\x1b[0m")]
        public static void DecoratedLengthAgreesWithPlainTextWithoutChangingRendering(string text)
        {
            string expected = ValueStringDecorated.AnsiRegex.Replace(text, string.Empty);
            var value = new ValueStringDecorated(text);
            var publicValue = new StringDecorated(text);
            Assert.Equal(expected.Length, value.ContentLength);
            Assert.Equal(expected.Length, value.ContentLength);
            Assert.Equal(expected.Length, publicValue.ContentLength);
            Assert.Equal(text, value.ToString(OutputRendering.Ansi));
            Assert.Equal(text, publicValue.ToString(OutputRendering.Ansi));
            Assert.Equal(expected, value.ToString(OutputRendering.PlainText));
            Assert.Equal(expected, publicValue.ToString(OutputRendering.PlainText));
            Assert.Equal(expected.Length, value.ContentLength);
        }

        [Theory]
        [InlineData("one two three", "one ", "two ", "three")]
        [InlineData(" one  two   ", " one ", " two ", "  ")]
        [InlineData("one\ttwo\t\tthree", "one\t", "two\t\t", "three")]
        [InlineData("\x1b[44mone two three\x1b[0m", "\x1b[44mone \x1b[0m", "\x1b[44mtwo \x1b[0m", "\x1b[44mthree\x1b[0m")]
        public static void WordWrappingPreservesWhitespaceAndAnsiResets(string text, string first, string second, string third)
        {
            CultureInfo previous = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            try
            {
                var lines = StringManipulationHelper.GenerateLines(new DisplayCells(), text, 5, 5);
                Assert.Equal(new[] { first, second, third }, lines.Cast<string>());
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Theory]
        [InlineData("plain\t\r\ntext")]
        [InlineData("\x1b[32mabc\x1b[0mdef")]
        [InlineData("a\u4e2d\x1b[31m\u6587\x1b[0mz")]
        [InlineData("\x1b]8;;https://example.com\x1b\\\u4e2dtext\x1b]8;;\x1b\\")]
        [InlineData("unknown\x1b[2J")]
        [InlineData("\x1b[31m\x1b[0m")]
        public static void DisplayCellLengthPreservesVisibleOffsetsAndWideCharacters(string text)
        {
            var cells = new DisplayCells();
            string plain = ValueStringDecorated.AnsiRegex.Replace(text, string.Empty);
            for (int offset = 0; offset <= plain.Length + 1; offset++)
            {
                int expected = 0;
                for (int index = offset; index < plain.Length; index++)
                {
                    expected += cells.Length(plain[index]);
                }

                Assert.Equal(expected, cells.Length(text, offset));
            }

            Assert.Throws<IndexOutOfRangeException>(() => cells.Length(text, -1));
            Assert.Equal(0, cells.Length(null, -1));
            Assert.Equal(0, cells.Length(string.Empty, -1));
        }

        private static ViewDefinition AddOutOfBandView(TypeInfoDataBase database, string typeName)
        {
            var view = new ViewDefinition { outOfBand = true, mainControl = new ListControlBody(), appliesTo = new AppliesTo() };
            view.appliesTo.referenceList.Add(new TypeReference { name = typeName });
            database.viewDefinitionsSection.viewDefinitionList.Add(view);
            return view;
        }

        public sealed class CountingProperties
        {
            public int ReadCount { get; private set; }

            public int Alpha => ++ReadCount;

            public int Beta => ++ReadCount;
        }

        public sealed class ThrowingProperty
        {
            public string Value => throw new InvalidOperationException("getter failed");
        }

        public sealed class DynamicProperties : DynamicObject
        {
            public override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                result = binder.Name;
                return true;
            }
        }
    }
}
