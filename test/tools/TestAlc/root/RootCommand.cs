// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Test.Isolated.Root
{
    [Cmdlet("Test", "RootCommand")]
    public class TestRootCommand : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public Red Param { get; set; }

        protected override void ProcessRecord()
        {
            WriteObject(Param.Name);
        }
    }

    public class Red
    {
        public string Name { get; }

        public Red(string name)
        {
            Name = name;
        }
    }

    public class Yellow
    {
        public string Id { get; }

        public Yellow(string id)
        {
            Id = id;
        }
    }
}
