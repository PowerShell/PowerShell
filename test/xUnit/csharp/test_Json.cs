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
    public static class JsonTests
    {
        [Fact]
        public static void TestConvertToJsonBasic()
        {
            var context = new JsonObject.ConvertToJsonContext(maxDepth: 3, enumsAsStrings: false, compressOutput: true);
            string expected = "{\"name\":\"req\",\"type\":\"http\"}";
            OrderedDictionary hash = new OrderedDictionary {
                {"name", "req"},
                {"type", "http"}
            };
            string json = JsonObject.ConvertToJson2(hash, in context);
            Assert.Equal(expected, json);

            hash.Add("self", hash);
            Assert.Throws<System.Text.Json.JsonException>(() => JsonObject.ConvertToJson2(hash, context));
        }

        [Fact]
        public static void TestConvertToJsonWithEnum()
        {
            var context = new JsonObject.ConvertToJsonContext(maxDepth: 2, enumsAsStrings: false, compressOutput: true);
            string expected = "{\"type\":1}";
            Hashtable hash = new Hashtable {
                {"type", CommandTypes.Alias}
            };
            string json = JsonObject.ConvertToJson2(hash, in context);
            Assert.Equal(expected, json);

            context = new JsonObject.ConvertToJsonContext(maxDepth: 2, enumsAsStrings: true, compressOutput: true);
            json = JsonObject.ConvertToJson2(hash, in context);
            expected = "{\"type\":\"Alias\"}";
            Assert.Equal(expected, json);
        }

        [Fact]
        public static void TestConvertToJsonWithoutCompress()
        {
            var context = new JsonObject.ConvertToJsonContext(maxDepth: 2, enumsAsStrings: true, compressOutput: false);
            string expected = @"{
  ""type"": ""Alias""
}";
            Hashtable hash = new Hashtable {
                {"type", CommandTypes.Alias}
            };
            string json = JsonObject.ConvertToJson2(hash, in context);
            Assert.Equal(expected, json);
        }

        [Fact (Skip = "Cancellation is not implemented")]
        public static void TestConvertToJsonCancellation()
        {
            var source = new CancellationTokenSource();
            var context = new JsonObject.ConvertToJsonContext(
                maxDepth: 4,
                enumsAsStrings: true,
                compressOutput: false,
                source.Token,
                Newtonsoft.Json.StringEscapeHandling.Default,
                targetCmdlet: null);

            source.Cancel();
            Hashtable hash = new Hashtable {
                {"type", CommandTypes.Alias}
            };

            string json = JsonObject.ConvertToJson2(hash, in context);
            Assert.Null(json);
        }
    }
}
