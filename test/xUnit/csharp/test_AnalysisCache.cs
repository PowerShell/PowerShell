// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using System.Reflection;
using Xunit;

namespace PSTests.Parallel
{
    public static class AnalysisCacheTests
    {
        [Fact]
        public static void QueueSerializationDoesNotKeepInstanceAliveUntilExit()
        {
            // Serialize reads another process's cache into a temporary AnalysisCacheData, and that
            // instance can queue a save from its delayed Cleanup. It must not be referenced by ProcessExit.
            var instance = (AnalysisCacheData)Activator.CreateInstance(typeof(AnalysisCacheData), nonPublic: true);
            FieldInfo saveToDisk = typeof(AnalysisCacheData).GetField("_saveCacheToDisk", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(saveToDisk);

            try
            {
                instance.QueueSerialization();

                FieldInfo processExit = typeof(AppDomain).GetField("ProcessExit", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(processExit);
                var handlers = (EventHandler)processExit.GetValue(AppDomain.CurrentDomain);
                Assert.NotNull(handlers);

                foreach (Delegate handler in handlers.GetInvocationList())
                {
                    Assert.NotSame(instance, handler.Target);
                }
            }
            finally
            {
                // Keep the delayed save from writing this empty instance to the cache file.
                saveToDisk.SetValue(instance, false);
            }
        }
    }
}
