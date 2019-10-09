// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Threading;
using System.Reflection;
using Microsoft.PowerShell.Commands;
using Xunit;

namespace PSTests.Parallel
{
    public static class UtilsTests
    {
        [SkippableFact]
        public static void TestIsWinPEHost()
        {
            Skip.IfNot(Platform.IsWindows);
            Assert.False(Utils.IsWinPEHost());
        }

        [Fact]
        public static void TestHistoryStack()
        {
            var historyStack = new HistoryStack<string>(20);
            Assert.Equal(0, historyStack.UndoCount);
            Assert.Equal(0, historyStack.RedoCount);

            historyStack.Push("first item");
            historyStack.Push("second item");
            Assert.Equal(2, historyStack.UndoCount);
            Assert.Equal(0, historyStack.RedoCount);

            Assert.Equal("second item", historyStack.Undo("second item"));
            Assert.Equal("first item", historyStack.Undo("first item"));
            Assert.Equal(0, historyStack.UndoCount);
            Assert.Equal(2, historyStack.RedoCount);

            Assert.Equal("first item", historyStack.Redo("first item"));
            Assert.Equal(1, historyStack.UndoCount);
            Assert.Equal(1, historyStack.RedoCount);

            // Pushing a new item should invalidate the RedoCount
            historyStack.Push("third item");
            Assert.Equal(2, historyStack.UndoCount);
            Assert.Equal(0, historyStack.RedoCount);

            // Check for the correct exception when the Redo/Undo stack is empty.
            Assert.Throws<InvalidOperationException>(() => historyStack.Redo("bar"));
            historyStack.Undo("third item");
            historyStack.Undo("first item");
            Assert.Equal(0, historyStack.UndoCount);
            Assert.Throws<InvalidOperationException>(() => historyStack.Undo("foo"));
        }

        [Fact]
        public static void TestBoundedStack()
        {
            uint capacity = 20;
            var boundedStack = new BoundedStack<string>(capacity);
            Assert.Throws<InvalidOperationException>(() => boundedStack.Pop());

            for (int i = 0; i < capacity; i++)
            {
                boundedStack.Push($"{i}");
            }

            for (int i = 0; i < capacity; i++)
            {
                var poppedItem = boundedStack.Pop();
                Assert.Equal($"{20 - 1 - i}", poppedItem);
            }

            Assert.Throws<InvalidOperationException>(() => boundedStack.Pop());
        }

        [Fact]
        public static void TestConvertToJsonBasic()
        {
            var context = new JsonObject.ConvertToJsonContext(maxDepth: 1, enumsAsStrings: false, compressOutput: true);
            string expected = "{\"name\":\"req\",\"type\":\"http\"}";
            OrderedDictionary hash = new OrderedDictionary {
                {"name", "req"},
                {"type", "http"}
            };
            string json = JsonObject.ConvertToJson(hash, in context);
            Assert.Equal(expected, json);

            hash.Add("self", hash);
            json = JsonObject.ConvertToJson(hash, context);
            expected = "{\"name\":\"req\",\"type\":\"http\",\"self\":{\"name\":\"req\",\"type\":\"http\",\"self\":\"System.Collections.Specialized.OrderedDictionary\"}}";
            Assert.Equal(expected, json);
        }

        [Fact]
        public static void TestConvertToJsonWithEnum()
        {
            var context = new JsonObject.ConvertToJsonContext(maxDepth: 1, enumsAsStrings: false, compressOutput: true);
            string expected = "{\"type\":1}";
            Hashtable hash = new Hashtable {
                {"type", CommandTypes.Alias}
            };
            string json = JsonObject.ConvertToJson(hash, in context);
            Assert.Equal(expected, json);

            context = new JsonObject.ConvertToJsonContext(maxDepth: 1, enumsAsStrings: true, compressOutput: true);
            json = JsonObject.ConvertToJson(hash, in context);
            expected = "{\"type\":\"Alias\"}";
            Assert.Equal(expected, json);
        }

        [Fact]
        public static void TestConvertToJsonWithoutCompress()
        {
            var context = new JsonObject.ConvertToJsonContext(maxDepth: 1, enumsAsStrings: true, compressOutput: false);
            string expected = @"{
  ""type"": ""Alias""
}";
            Hashtable hash = new Hashtable {
                {"type", CommandTypes.Alias}
            };
            string json = JsonObject.ConvertToJson(hash, in context);
            Assert.Equal(expected, json);
        }

        [Fact]
        public static void TestConvertToJsonCancellation()
        {
            var source = new CancellationTokenSource();
            var context = new JsonObject.ConvertToJsonContext(
                maxDepth: 1,
                enumsAsStrings: true,
                compressOutput: false,
                source.Token,
                Newtonsoft.Json.StringEscapeHandling.Default,
                targetCmdlet: null);

            source.Cancel();
            Hashtable hash = new Hashtable {
                {"type", CommandTypes.Alias}
            };

            string json = JsonObject.ConvertToJson(hash, in context);
            Assert.Null(json);
        }
    }
}
