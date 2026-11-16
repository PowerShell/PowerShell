// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Xunit;

namespace PSTests.Parallel
{
    public static class SecuritySupportTests
    {
        [Fact]
        public static void TestScanContent()
        {
            try
            {
                Assert.Equal(AmsiUtils.AmsiNativeMethods.AMSI_RESULT.AMSI_RESULT_NOT_DETECTED, AmsiUtils.ScanContent(string.Empty, string.Empty));
            }
            finally
            {
                AmsiUtils.Uninitialize();
            }
        }
    }

#if !UNIX
    public sealed class AmsiLifecycleTests : IDisposable
    {
        private readonly AssemblyLoadContext loadContext;
        private readonly Type amsiType;
        private int amsiLoadAttempts;
        private int failOnAmsiLoad;

        public AmsiLifecycleTests()
        {
            // Isolate static state and native bindings from the test host's AMSI instance.
            loadContext = new AssemblyLoadContext(nameof(AmsiLifecycleTests), isCollectible: true);
            Assembly assembly = loadContext.LoadFromAssemblyPath(typeof(PSObject).Assembly.Location);
            amsiType = assembly.GetType("System.Management.Automation.AmsiUtils", throwOnError: true);
            NativeLibrary.SetDllImportResolver(assembly, (name, _, _) =>
            {
                if (name == "amsi.dll" && ++amsiLoadAttempts == failOnAmsiLoad)
                {
                    throw new DllNotFoundException("Simulated AMSI initialization failure.");
                }

                return IntPtr.Zero;
            });
        }

        public void Dispose()
        {
            try
            {
                Invoke("Uninitialize");
            }
            finally
            {
                loadContext.Unload();
            }
        }

        [Theory]
        [InlineData("CloseSession", true)]
        [InlineData("CloseSession", false)]
        [InlineData("Uninitialize", null)]
        public void CleanupDoesNotInitializeAmsi(string method, bool? resetSession)
        {
            failOnAmsiLoad = 1;

            Invoke(method, resetSession.HasValue ? new object[] { resetSession.Value } : Array.Empty<object>());

            Assert.Equal(0, amsiLoadAttempts);
            Assert.Equal(IntPtr.Zero, ReadField<IntPtr>("s_amsiContext"));
            Assert.Equal(IntPtr.Zero, ReadField<IntPtr>("s_amsiSession"));
            Assert.False(ReadField<bool>("s_amsiInitFailed"));
        }

        [Theory]
        [InlineData("ScanContent")]
        [InlineData("ReportContent")]
        public void InitializationFailureIsSharedAndNotRetried(string method)
        {
            failOnAmsiLoad = 1;

            Invoke(method, "content", "source");

            Assert.True(ReadField<bool>("s_amsiInitFailed"));
            Assert.Equal(IntPtr.Zero, ReadField<IntPtr>("s_amsiContext"));
            Assert.Equal(IntPtr.Zero, ReadField<IntPtr>("s_amsiSession"));
            Assert.Equal(1, amsiLoadAttempts);

            Assert.Equal(1, Convert.ToInt32(Invoke("ScanContent", "content", "source")));
            Assert.False((bool)Invoke("ReportContent", "source", "content"));
            Assert.Equal(1, amsiLoadAttempts);
        }

        [Fact]
        public void SessionInitializationFailureStillAllowsContextCleanup()
        {
            // Let AmsiInitialize bind, but fail when AmsiOpenSession first binds.
            failOnAmsiLoad = 2;

            Invoke("WinScanContent", string.Empty, string.Empty, true);

            Assert.True(ReadField<bool>("s_amsiInitFailed"));
            Assert.NotEqual(IntPtr.Zero, ReadField<IntPtr>("s_amsiContext"));
            Assert.Equal(IntPtr.Zero, ReadField<IntPtr>("s_amsiSession"));
            Assert.Equal(2, amsiLoadAttempts);

            Invoke("ScanContent", "content", "source");
            Assert.Equal(2, amsiLoadAttempts);

            Invoke("CurrentDomain_ProcessExit", null, EventArgs.Empty);

            Assert.Equal(IntPtr.Zero, ReadField<IntPtr>("s_amsiContext"));
            Assert.True(ReadField<bool>("s_amsiInitFailed"));
        }

        [Fact]
        public void CleanupAndReinitializationPreserveHandleOwnership()
        {
            Invoke("Uninitialize");

            for (int i = 0; i < 2; i++)
            {
                Invoke("ScanContent", "1", "test");

                IntPtr context = ReadField<IntPtr>("s_amsiContext");
                Assert.NotEqual(IntPtr.Zero, context);
                Assert.NotEqual(IntPtr.Zero, ReadField<IntPtr>("s_amsiSession"));

                Invoke("CloseSession", true);
                Assert.Equal(context, ReadField<IntPtr>("s_amsiContext"));
                Assert.Equal(IntPtr.Zero, ReadField<IntPtr>("s_amsiSession"));

                Invoke("ScanContent", "1", "test");
                Assert.Equal(context, ReadField<IntPtr>("s_amsiContext"));
                Assert.NotEqual(IntPtr.Zero, ReadField<IntPtr>("s_amsiSession"));

                Invoke("CurrentDomain_ProcessExit", null, EventArgs.Empty);
                Assert.Equal(IntPtr.Zero, ReadField<IntPtr>("s_amsiContext"));
                Assert.Equal(IntPtr.Zero, ReadField<IntPtr>("s_amsiSession"));
            }
        }

        [Fact]
        public void FirstPipelineReusesUnusedWarmupSession()
        {
            Invoke("WinScanContent", string.Empty, string.Empty, true);

            IntPtr context = ReadField<IntPtr>("s_amsiContext");
            IntPtr session = ReadField<IntPtr>("s_amsiSession");
            Assert.NotEqual(IntPtr.Zero, context);
            Assert.NotEqual(IntPtr.Zero, session);

            Invoke("CloseSession", true);
            Assert.Equal(context, ReadField<IntPtr>("s_amsiContext"));
            Assert.Equal(session, ReadField<IntPtr>("s_amsiSession"));

            Invoke("ScanContent", "1", "test");
            Assert.Equal(session, ReadField<IntPtr>("s_amsiSession"));

            Invoke("CloseSession", true);
            Assert.Equal(context, ReadField<IntPtr>("s_amsiContext"));
            Assert.Equal(IntPtr.Zero, ReadField<IntPtr>("s_amsiSession"));
        }

        [Theory]
        [InlineData("ScanContent")]
        [InlineData("ReportContent")]
        public void LateWarmupSessionIsResetAfterUse(string method)
        {
            // The first pipeline is constructed before the background warmup runs.
            Invoke("CloseSession", true);
            Invoke("WinScanContent", string.Empty, string.Empty, true);

            IntPtr context = ReadField<IntPtr>("s_amsiContext");
            IntPtr session = ReadField<IntPtr>("s_amsiSession");
            Assert.NotEqual(IntPtr.Zero, context);
            Assert.NotEqual(IntPtr.Zero, session);

            Invoke(method, "1", "test");
            Assert.Equal(session, ReadField<IntPtr>("s_amsiSession"));

            Invoke("CloseSession", true);
            Assert.Equal(context, ReadField<IntPtr>("s_amsiContext"));
            Assert.Equal(IntPtr.Zero, ReadField<IntPtr>("s_amsiSession"));
        }

        [Fact]
        public void ProcessExitClosesUnusedWarmupSession()
        {
            Invoke("WinScanContent", string.Empty, string.Empty, true);
            Assert.NotEqual(IntPtr.Zero, ReadField<IntPtr>("s_amsiContext"));
            Assert.NotEqual(IntPtr.Zero, ReadField<IntPtr>("s_amsiSession"));

            Invoke("CurrentDomain_ProcessExit", null, EventArgs.Empty);

            Assert.Equal(IntPtr.Zero, ReadField<IntPtr>("s_amsiContext"));
            Assert.Equal(IntPtr.Zero, ReadField<IntPtr>("s_amsiSession"));
        }

        private object Invoke(string method, params object[] arguments)
        {
            return amsiType.GetMethod(method, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Invoke(null, arguments);
        }

        private T ReadField<T>(string name)
        {
            return (T)amsiType.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .GetValue(null);
        }
    }
#endif
}
