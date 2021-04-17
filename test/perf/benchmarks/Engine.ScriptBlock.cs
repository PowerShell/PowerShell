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
    [BenchmarkCategory(Categories.Engine, Categories.Public)]
    public class Scripting
    {
        [ParamsSource(nameof(ValuesForScript))]
        public string Script { get; set; }

        private Runspace runspace;
        private ScriptBlock scriptBlock;

        public IEnumerable<string> ValuesForScript()
        {
            yield return @"'String'.GetType()";
            yield return @"[System.IO.Path]::HasExtension('')";

            // Test on COM method invocation.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                yield return @"$sh=New-Object -ComObject Shell.Application; $sh.Namespace('c:\')";
                yield return @"$fs=New-Object -ComObject scripting.filesystemobject; $fs.Drives";
            }
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            runspace = RunspaceFactory.CreateRunspace(InitialSessionState.CreateDefault2());
            runspace.Open();
            Runspace.DefaultRunspace = runspace;
            scriptBlock = ScriptBlock.Create(Script);
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            runspace.Dispose();
            Runspace.DefaultRunspace = null;
        }

        [Benchmark()]
        public Collection<PSObject> Invoke_Method()
        {
            return scriptBlock.Invoke();
        }
    }
}
