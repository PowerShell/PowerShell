# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Remote host method call tests" -Tag Feature {
    BeforeAll {
        if (-not ('RemoteHostCallsTest.TestHost' -as [type])) {
            Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Security;

namespace RemoteHostCallsTest
{
    public class TestHostUserInterface : PSHostUserInterface, IHostUISupportsMultipleChoiceSelection
    {
        public int[] ReceivedDefaultChoices;
        public Collection<int> ChoicesToReturn = new Collection<int>();

        public override PSHostRawUserInterface RawUI => null;
        public override string ReadLine() => throw new NotImplementedException();
        public override SecureString ReadLineAsSecureString() => throw new NotImplementedException();
        public override void Write(string value) { }
        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value) { }
        public override void WriteLine(string value) { }
        public override void WriteErrorLine(string value) { }
        public override void WriteDebugLine(string message) { }
        public override void WriteProgress(long sourceId, ProgressRecord record) { }
        public override void WriteVerboseLine(string message) { }
        public override void WriteWarningLine(string message) { }

        public override Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions)
            => throw new NotImplementedException();

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName)
            => throw new NotImplementedException();

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options)
            => throw new NotImplementedException();

        public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice)
            => throw new NotImplementedException();

        public Collection<int> PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, IEnumerable<int> defaultChoices)
        {
            ReceivedDefaultChoices = defaultChoices == null ? null : new List<int>(defaultChoices).ToArray();
            return ChoicesToReturn;
        }
    }

    public class TestHost : PSHost
    {
        private readonly TestHostUserInterface _ui = new TestHostUserInterface();
        private readonly Guid _instanceId = Guid.NewGuid();

        public override CultureInfo CurrentCulture => CultureInfo.CurrentCulture;
        public override CultureInfo CurrentUICulture => CultureInfo.CurrentUICulture;
        public override Guid InstanceId => _instanceId;
        public override string Name => "RemoteHostCallsTest";
        public override PSHostUserInterface UI => _ui;
        public override Version Version => new Version(1, 0);
        public override void EnterNestedPrompt() => throw new NotImplementedException();
        public override void ExitNestedPrompt() => throw new NotImplementedException();
        public override void NotifyBeginApplication() { }
        public override void NotifyEndApplication() { }
        public override void SetShouldExit(int exitCode) { }
    }
}
'@
        }

        $testHost = [RemoteHostCallsTest.TestHost]::new()

        # Connecting to the current process over the named pipe gives a remote
        # runspace whose host calls are marshalled back to our custom host.
        $connInfo = [System.Management.Automation.Runspaces.NamedPipeConnectionInfo]::new($PID)
        $runspace = [runspacefactory]::CreateRunspace($testHost, $connInfo)
        $runspace.Open()
    }

    AfterAll {
        if ($runspace) {
            $runspace.Dispose()
        }
    }

    Context "IHostUISupportsMultipleChoiceSelection.PromptForChoice" {
        BeforeEach {
            $testHost.UI.ReceivedDefaultChoices = $null
            $testHost.UI.ChoicesToReturn.Clear()
        }

        It "Passes default choices as <Name>" -TestCases @(
            @{ Name = 'int[]'; DefaultChoices = '[int[]]@(0, 2)'; Expected = @(0, 2) }
            @{ Name = 'List[int]'; DefaultChoices = '[System.Collections.Generic.List[int]]@(0, 2)'; Expected = @(0, 2) }
            @{ Name = 'Collection[int]'; DefaultChoices = '[System.Collections.ObjectModel.Collection[int]]@(0, 2)'; Expected = @(0, 2) }
            @{ Name = 'empty int[]'; DefaultChoices = '[int[]]@()'; Expected = @() }
            @{ Name = 'null'; DefaultChoices = '$null'; Expected = $null }
        ) {
            param ($DefaultChoices, $Expected)

            $testHost.UI.ChoicesToReturn.Add(1)
            $testHost.UI.ChoicesToReturn.Add(2)

            $ps = [PowerShell]::Create()
            try {
                $ps.Runspace = $runspace
                $null = $ps.AddScript(@"
`$choices = [System.Collections.ObjectModel.Collection[System.Management.Automation.Host.ChoiceDescription]]@('&a', '&b', '&c')
`$Host.UI.PromptForChoice('caption', 'message', `$choices, $DefaultChoices)
"@)
                $actual = $ps.Invoke()

                $ps.Streams.Error | Should -BeNullOrEmpty
                $actual | Should -Be @(1, 2)

                if ($null -eq $Expected) {
                    $null -eq $testHost.UI.ReceivedDefaultChoices | Should -BeTrue
                }
                else {
                    $null -eq $testHost.UI.ReceivedDefaultChoices | Should -BeFalse
                    $testHost.UI.ReceivedDefaultChoices -join ',' | Should -BeExactly ($Expected -join ',')
                }
            }
            finally {
                $ps.Dispose()
            }
        }
    }
}
