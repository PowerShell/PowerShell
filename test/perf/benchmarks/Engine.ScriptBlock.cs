// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Engine
{
    [MemoryDiagnoser]
    [BenchmarkCategory(Categories.Engine, Categories.Internal)]
    public class PipelineInputBinding
    {
        private Runspace runspace;
        private ScriptBlock forEach;
        private ScriptBlock where;
        private ScriptBlock sink;
        private CmdletParameterBinderController binder;
        private readonly PSObject value = new PSObject(1);

        [GlobalSetup]
        public void Setup()
        {
            var state = InitialSessionState.CreateDefault2();
            state.Commands.Add(new SessionStateCmdletEntry("Test-PipelineSink", typeof(PipelineSinkCommand), null));
            runspace = RunspaceFactory.CreateRunspace(state);
            runspace.Open();
            Runspace.DefaultRunspace = runspace;
            forEach = ScriptBlock.Create("1..10000 | ForEach-Object {}");
            where = ScriptBlock.Create("1..10000 | Where-Object { $false }");
            sink = ScriptBlock.Create("1..10000 | Test-PipelineSink");
            forEach.Invoke();
            where.Invoke();
            sink.Invoke();
            binder = PipelineSinkCommand.Binder;
            PipelineSinkCommand.Binder = null;
        }

        [Benchmark]
        public void ForEachObject() => forEach.Invoke();

        [Benchmark]
        public void WhereObject() => where.Invoke();

        [Benchmark]
        public void CompiledSink() => sink.Invoke();

        [Benchmark(OperationsPerInvoke = 10000)]
        public void BindOnly()
        {
            for (int i = 0; i < 10000; i++)
            {
                binder.BindPipelineParameters(value);
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            PipelineSinkCommand.Binder = null;
            binder = null;
            runspace.Dispose();
            Runspace.DefaultRunspace = null;
        }

        [Cmdlet("Test", "PipelineSink")]
        public sealed class PipelineSinkCommand : PSCmdlet
        {
            internal static CmdletParameterBinderController Binder;

            [Parameter(ValueFromPipeline = true)]
            public PSObject Value { get; set; }

            protected override void BeginProcessing()
            {
                Binder = ((CommandProcessor)Context.CurrentCommandProcessor).CmdletParameterBinderController;
            }
        }
    }

    [BenchmarkCategory(Categories.Engine, Categories.Public)]
    public class Scripting
    {
        private Runspace runspace;
        private ScriptBlock scriptBlock;

        private void SetupRunspace()
        {
            // Unless you want to run commands from any built-in modules, using 'CreateDefault2' is enough.
            runspace = RunspaceFactory.CreateRunspace(InitialSessionState.CreateDefault2());
            runspace.Open();
            Runspace.DefaultRunspace = runspace;
        }

        #region Invoke-Method

        [ParamsSource(nameof(ValuesForScript))]
        public string InvokeMethodScript { get; set; }

        public IEnumerable<string> ValuesForScript()
        {
            yield return @"'String'.GetType()";
            yield return @"[System.IO.Path]::HasExtension('')";

            // Test on COM method invocation.
            if (Platform.IsWindows)
            {
                yield return @"$sh=New-Object -ComObject Shell.Application; $sh.Namespace('c:\')";
                yield return @"$fs=New-Object -ComObject scripting.filesystemobject; $fs.Drives";
            }
        }

        [GlobalSetup(Target = nameof(InvokeMethod))]
        public void GlobalSetup()
        {
            SetupRunspace();
            scriptBlock = ScriptBlock.Create(InvokeMethodScript);

            // Run it once to get the C# code jitted and the script compiled.
            // The first call to this takes relatively too long, which makes the BDN's heuristic incorrectly
            // believe that there is no need to run many ops in each iteration. However, the subsequent runs
            // of this method is much faster than the first run, and this causes 'MinIterationTime' warnings
            // to our benchmarks and make the benchmark results not reliable.
            // Calling this method once in 'GlobalSetup' is a workaround.
            // See https://github.com/dotnet/BenchmarkDotNet/issues/837#issuecomment-828600157
            scriptBlock.Invoke();
        }

        [Benchmark]
        public Collection<PSObject> InvokeMethod()
        {
            return scriptBlock.Invoke();
        }

        #endregion

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            runspace.Dispose();
            Runspace.DefaultRunspace = null;
        }
    }
}
