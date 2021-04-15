// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Engine.Scripting
{
    [BenchmarkCategory(Categories.Engine, Categories.Public)]
    public class ScriptBlock_Public
    {
        [ParamsSource(nameof(ValuesForScript))]
        public string Script { get; set; }

        private Runspace runspace;
        private ScriptBlock scriptBlock;

        public IEnumerable<string> ValuesForScript => new[]
        {
            "'string'.Trim()",
            "[System.IO.Path]::HasExtension('')",
        };

        [GlobalSetup]
        public void GlobalSetup()
        {
            runspace = RunspaceFactory.CreateRunspace(InitialSessionState.CreateDefault2());
            runspace.Open();
            Runspace.DefaultRunspace = runspace;
            scriptBlock = ScriptBlock.Create(Script);
        }

        [Benchmark(Description = "Simple method invocation")]
        public Collection<PSObject> ScriptBlock_Invoke()
        {
            return scriptBlock.Invoke();
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            runspace.Dispose();
            Runspace.DefaultRunspace = null;
        }
    }
}
