# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

class RuntimeThisProbe {
    [bool] SameThis([object] $facade) { return [object]::ReferenceEquals($this, $facade.Invocation.This) }
}

Describe 'PSScopedProperties runtime views' -Tag 'CI' {
    BeforeAll {
        $originalDefaults = $PSDefaultParameterValues.Clone()
        $PSDefaultParameterValues['It:Skip'] = -not [ExperimentalFeature]::IsEnabled('PSScopedProperties')
    }

    AfterAll { $global:PSDefaultParameterValues = $originalDefaults }

    It 'keeps runtime containers stable, non-consuming, and exposes actual automatic values' {
        function Get-RuntimeIdentity {
            param([Parameter(ValueFromPipeline)] $Item)
            process {
                [pscustomobject]@{
                    InfoSame = [object]::ReferenceEquals($MyInvocation, $PS.Invocation.Info)
                    InputSame = [object]::ReferenceEquals($input, $PS.Invocation.Input)
                    ExecutionContextSame = [object]::ReferenceEquals($ExecutionContext, $PS.Session.ExecutionContext)
                    HostSame = [object]::ReferenceEquals($Host, $PS.Session.Host)
                    InputType = $PS.Invocation.Input.GetType().FullName
                    InputStillReadable = $Item
                    Display = "$($PS.Invocation) $($PS.Session)"
                }
            }
        }

        $observed = @('a', 'b' | Get-RuntimeIdentity)
        $observed.Count | Should -Be 2
        $observed[0].InfoSame | Should -BeTrue
        $observed[0].InputSame | Should -BeTrue
        $observed[0].ExecutionContextSame | Should -BeTrue
        $observed[0].HostSame | Should -BeTrue
        $observed[0].InputStillReadable | Should -Be 'a'
        $observed[0].Display | Should -Not -BeNullOrEmpty
    }

    It 'resolves invocation variables from active function and dotted script frames' {
        function Get-InvocationView {
            [CmdletBinding()]
            param($First, $Second)
            [pscustomobject]@{
                Args = @($PS.Invocation.Arguments)
                Bound = $PS.Invocation.BoundParameters
                InfoSame = [object]::ReferenceEquals($PS.Invocation.Info, $MyInvocation)
                CmdletSame = [object]::ReferenceEquals($PS.Invocation.Cmdlet, $PSCmdlet)
                FunctionName = $PS.Invocation.Info.MyCommand.Name
                ScriptRoot = $PS.Invocation.ScriptRoot
                CommandPath = $PS.Invocation.CommandPath
            }
        }

        $view = Get-InvocationView -First one -Second two
        $view.Args | Should -BeNullOrEmpty
        $view.Bound.First | Should -Be 'one'
        $view.Bound.Second | Should -Be 'two'
        $view.InfoSame | Should -BeTrue
        $view.CmdletSame | Should -BeTrue
        $view.FunctionName | Should -Be 'Get-InvocationView'

        function Get-ArgsView {
            [pscustomobject]@{
                Legacy = $args
                Facade = $PS.Invocation.Arguments
                Same = [object]::ReferenceEquals($args, $PS.Invocation.Arguments)
            }
        }
        $arguments = Get-ArgsView alpha beta
        $arguments.Legacy | Should -Be @('alpha', 'beta')
        $arguments.Facade | Should -Be @('alpha', 'beta')
        $arguments.Same | Should -BeTrue

        $dotScript = Join-Path $TestDrive 'runtime-dotted.ps1'
        $scriptText = '[pscustomobject]@{ Root=$PS.Invocation.ScriptRoot; Path=$PS.Invocation.CommandPath; InfoRoot=$PS.Invocation.Info.PSScriptRoot; InfoPath=$PS.Invocation.Info.PSCommandPath; LegacyInfoRoot=$MyInvocation.PSScriptRoot; LegacyInfoPath=$MyInvocation.PSCommandPath }'
        Set-Content -LiteralPath $dotScript -Value $scriptText
        $dot = . $dotScript
        $dot.Root | Should -Be (Split-Path -Parent $dotScript)
        $dot.Path | Should -Be $dotScript
        $dot.InfoRoot | Should -Be $dot.LegacyInfoRoot
        $dot.InfoPath | Should -Be $dot.LegacyInfoPath
    }

    It 'routes runtime reads through selected scopes and preserves legacy setters' {
        $savedExitCode = Get-Variable LASTEXITCODE -Scope Global -ErrorAction Ignore
        $savedExitCodeValue = if ($null -ne $savedExitCode) { $savedExitCode.Value }
        try {
            $global:LASTEXITCODE = 31
            $PS.AtScope('Global').LastExitCode | Should -Be 31
            $PS.LastExitCode = 32
            $LASTEXITCODE | Should -Be 32
            $global:LASTEXITCODE | Should -Be 31
            $PS.AtScope('Global').LastExitCode = 33
            $global:LASTEXITCODE | Should -Be 33
            $PS.LastExitCode | Should -Be 32
            { $PS.Reset('Runtime.ProcessId') } | Should -Throw
            { $PS.Reset('Invocation.Arguments') } | Should -Throw
        } finally {
            if ($null -ne $savedExitCode) {
                Set-Variable LASTEXITCODE $savedExitCodeValue -Scope Global
            } else {
                Remove-Variable LASTEXITCODE -Scope Global -Force -ErrorAction Ignore
            }
        }
    }

    It 'distinguishes absent optional runtime values from explicit null under strict modes' {
        $savedProfile = Get-Variable PROFILE -Scope Global -ErrorAction Ignore
        $savedProfileValue = if ($null -ne $savedProfile) { $savedProfile.Value }
        try {
            if ($null -ne $savedProfile) {
                $PS.Session.Profile | Should -Be $savedProfileValue
            }
            Remove-Variable PROFILE -Scope Global -Force -ErrorAction Ignore
            Set-StrictMode -Off
            $PS.Session.Profile | Should -BeNullOrEmpty
            "$($PS.Session.Profile)" | Should -BeExactly ''

            Set-StrictMode -Version 1
            { $PS.Session.Profile } | Should -Throw
            "$($PS.Session.Profile)" | Should -BeExactly ''

            Set-StrictMode -Version 2
            { $PS.Session.Profile } | Should -Throw
            { "$($PS.Session.Profile)" } | Should -Throw

            $global:PROFILE = $null
            $PS.Session.Profile | Should -BeNullOrEmpty
            "$($PS.Session.Profile)" | Should -BeExactly ''
        } finally {
            Set-StrictMode -Off
            Remove-Variable PROFILE -Scope Global -Force -ErrorAction Ignore
            if ($null -ne $savedProfile) { Set-Variable PROFILE $savedProfileValue -Scope Global }
        }
    }

    It 'uses PWD aliases and runspace location ownership, including caller shadowing' {
        $originalLocation = Get-Location
        Push-Location $originalLocation
        try {
            Set-Location $TestDrive
            $PS.Session.CurrentLocation.Path | Should -Be (Get-Location).Path
            function Read-ShadowedPwd {
                $PWD = 'shadow-value'
                $PS.Session.CurrentLocation | Should -BeExactly 'shadow-value'
                $PWD = Get-Location
                [object]::ReferenceEquals($PWD, $PS.Session.CurrentLocation) | Should -BeTrue
            }
            Read-ShadowedPwd
        } finally {
            Pop-Location
        }
        $PS.Session.CurrentLocation.Path | Should -Be $originalLocation.Path
    }

    It 'preserves class and ETS this ownership in methods, script properties, and script methods' {
        $instance = [RuntimeThisProbe]::new()
        $instance.SameThis($PS) | Should -BeTrue

        Add-Member -InputObject $instance -MemberType ScriptProperty -Name ScopedThis -Value {
            [object]::ReferenceEquals($this, $PS.Invocation.This)
        } -Force
        Add-Member -InputObject $instance -MemberType ScriptMethod -Name ScopedThisMethod -Value {
            [object]::ReferenceEquals($this, $PS.Invocation.This)
        } -Force
        $instance.ScopedThis | Should -BeTrue
        $instance.ScopedThisMethod() | Should -BeTrue
    }

    It 'keeps input identity and does not consume input during access or display' {
        function Test-InputView {
            $facadeInput = $PS.Invocation.Input
            $display = "$($PS) $($PS.Invocation)"
            [object]::ReferenceEquals($facadeInput, $input) | Should -BeTrue
            $display | Should -Not -BeNullOrEmpty
            $seen = [System.Collections.Generic.List[object]]::new()
            while ($facadeInput.MoveNext()) {
                $seen.Add($facadeInput.Current)
            }
            $seen | Should -Be @('record-1', 'record-2')
        }
        'record-1', 'record-2' | Test-InputView
    }

    It 'preserves foreach and switch enumerator identity and restores nested loop state' {
        $seen = [System.Collections.Generic.List[object]]::new()
        foreach ($outer in 1, 2) {
            $outerEnumerator = $PS.Invocation.ForEachEnumerator
            [object]::ReferenceEquals($outerEnumerator, $foreach) | Should -BeTrue
            foreach ($inner in 'x') {
                [object]::ReferenceEquals($PS.Invocation.ForEachEnumerator, $foreach) | Should -BeTrue
                $seen.Add("$outer$inner")
            }
            [object]::ReferenceEquals($PS.Invocation.ForEachEnumerator, $outerEnumerator) | Should -BeTrue
            try { foreach ($inner in 'throw') { throw 'unwind inner loop' } } catch {}
            [object]::ReferenceEquals($PS.Invocation.ForEachEnumerator, $outerEnumerator) | Should -BeTrue
            foreach ($inner in 'break') { break }
            [object]::ReferenceEquals($PS.Invocation.ForEachEnumerator, $outerEnumerator) | Should -BeTrue
        }
        $seen | Should -Be @('1x', '2x')

        switch -Regex ('alpha') {
            'alpha' {
                [object]::ReferenceEquals($PS.Invocation.SwitchEnumerator, $switch) | Should -BeTrue
                $PS.Invocation.SwitchEnumerator.Current | Should -Be 'alpha'
                $outerSwitch = $PS.Invocation.SwitchEnumerator
                try { switch ('inner') { default { throw 'unwind switch' } } } catch {}
                [object]::ReferenceEquals($PS.Invocation.SwitchEnumerator, $outerSwitch) | Should -BeTrue
                switch ('inner') { default { break } }
                [object]::ReferenceEquals($PS.Invocation.SwitchEnumerator, $outerSwitch) | Should -BeTrue
            }
        }

        foreach ($x in 1, 2) {
            try { if ($x -eq 1) { throw 'early' } } catch {}
        }
        $PS.Invocation.ForEachEnumerator | Should -BeNullOrEmpty
        $PS.Invocation.SwitchEnumerator | Should -BeNullOrEmpty
    }

    It 'keeps Matches from the last successful scalar, collection, and switch-regex match' {
        'abc' -match '(?<letters>a)' | Should -BeTrue
        [object]::ReferenceEquals($Matches, $PS.Invocation.Matches) | Should -BeTrue
        $PS.Invocation.Matches.letters | Should -Be 'a'
        'no-match' -match 'never' | Should -BeFalse
        $PS.Invocation.Matches.letters | Should -Be 'a'

        @('first-1', 'second-2') -match '(?<digit>\d)' | Out-Null
        $PS.Invocation.Matches.letters | Should -Be 'a'
        switch -Regex ('switch-3') { '(?<digit>\d)' { $PS.Invocation.Matches.digit | Should -Be '3' } }
    }

    It 'returns live diagnostic references and preserves error history identity' {
        $errors = $PS.Diagnostics.Errors
        [object]::ReferenceEquals($errors, $Error) | Should -BeTrue
        $count = $errors.Count
        Write-Error 'runtime-view-error' -ErrorAction Continue
        $PS.Diagnostics.Errors.Count | Should -BeGreaterThan $count
        [object]::ReferenceEquals($errors, $PS.Diagnostics.Errors) | Should -BeTrue
        $PS.Diagnostics.StackTrace | Should -Be $StackTrace
        $PS.Diagnostics.ToString() | Should -Not -BeNullOrEmpty
    }

    It 'keeps event values and event-local Error lists for a subscription lifetime' {
        $eventName = 'ScopedRuntimeEvent_' + [guid]::NewGuid().ToString('N')
        $secondEventName = $eventName + '_second'
        $received = [System.Collections.Generic.List[object]]::new()
        $secondReceived = [System.Collections.Generic.List[object]]::new()
        function Wait-RuntimeCallback($observations, $count, $job) {
            $deadline = [datetime]::UtcNow.AddSeconds(5)
            while ($observations.Count -lt $count -and [datetime]::UtcNow -lt $deadline) {
                Wait-Event -Timeout 1 | Out-Null
            }
            $observations.Count | Should -Be $count -Because ($job.Error | Out-String)
        }
        $action = {
            $Event.MessageData.Observations.Add([pscustomobject]@{
                RecordSame = [object]::ReferenceEquals($Event, $PS.Event.Record)
                Record = $PS.Event.Record
                SubscriberSame = [object]::ReferenceEquals($EventSubscriber, $PS.Event.Subscriber)
                ArgsSame = [object]::ReferenceEquals($EventArgs, $PS.Event.Arguments)
                SenderSame = [object]::ReferenceEquals($Sender, $PS.Event.Sender)
                Errors = $PS.Diagnostics.Errors
                ErrorCount = $PS.Diagnostics.Errors.Count
            })
            Write-Error 'event-local-error' -ErrorAction Continue
        }
        $secondAction = {
            $Event.MessageData.Observations.Add($PS.Diagnostics.Errors)
        }
        try {
            $firstJob = Register-EngineEvent -SourceIdentifier $eventName -Action $action
            $secondJob = Register-EngineEvent -SourceIdentifier $secondEventName -Action $secondAction
            New-Event -SourceIdentifier $eventName -MessageData @{ Observations = $received; Marker = 'one' } | Out-Null
            Wait-RuntimeCallback $received 1 $firstJob
            New-Event -SourceIdentifier $eventName -MessageData @{ Observations = $received; Marker = 'two' } | Out-Null
            Wait-RuntimeCallback $received 2 $firstJob
            New-Event -SourceIdentifier $secondEventName -MessageData @{ Observations = $secondReceived } | Out-Null
            Wait-RuntimeCallback $secondReceived 1 $secondJob
            $received.Count | Should -Be 2
            $received[0].RecordSame | Should -BeTrue
            $received[0].SubscriberSame | Should -BeTrue
            $received[0].ArgsSame | Should -BeTrue
            $received[0].SenderSame | Should -BeTrue
            [object]::ReferenceEquals($received[0].Errors, $received[1].Errors) | Should -BeTrue
            $received[1].ErrorCount | Should -BeGreaterThan $received[0].ErrorCount
            [object]::ReferenceEquals($received[0].Errors, $secondReceived[0]) | Should -BeFalse
            $persistedEvent = & $firstJob.Module { $PS.Event.Record }
            [object]::ReferenceEquals($persistedEvent, $received[1].Record) | Should -BeTrue
            [object]::ReferenceEquals($received[0].Errors, $Error) | Should -BeFalse
        } finally {
            Unregister-Event -SourceIdentifier $eventName -ErrorAction Ignore
            Unregister-Event -SourceIdentifier $secondEventName -ErrorAction Ignore
            Get-Event -SourceIdentifier $eventName -ErrorAction Ignore | Remove-Event -ErrorAction Ignore
            Get-Event -SourceIdentifier $secondEventName -ErrorAction Ignore | Remove-Event -ErrorAction Ignore
            $firstJob, $secondJob | Remove-Job -Force -ErrorAction Ignore
        }
    }

    It 'keeps optional producer values absent outside debugger, profile, prompt, and remoting lifetimes' {
        Set-StrictMode -Off
        $PS.Diagnostics.DebugContext | Should -BeNullOrEmpty
        $legacyProfile = Get-Variable PROFILE -ErrorAction Ignore
        if ($null -eq $legacyProfile) {
            $PS.Session.Profile | Should -BeNullOrEmpty
        } else {
            $PS.Session.Profile | Should -Be $legacyProfile.Value
        }
        $PS.Session.CurrentlyExecutingCommand | Should -BeNullOrEmpty
        $PS.Remoting.SenderInfo | Should -BeNullOrEmpty
        { $PS.Invocation.Cmdlet } | Should -Not -Throw
        { $PS.Invocation.ScriptRoot } | Should -Not -Throw
        { $PS.Invocation.CommandPath } | Should -Not -Throw
    }

    It 'keeps process, runspace, culture, platform, and language constant values live' {
        $PS.Runtime.ProcessId | Should -Be $PID
        $PS.Runtime.InstallationDirectory | Should -Be $PSHOME
        $PS.Runtime.Edition | Should -Be $PSEdition
        $PS.Runtime.ShellId | Should -Be $ShellId
        [object]::ReferenceEquals($PS.Runtime.VersionTable, $PSVersionTable) | Should -BeTrue
        $PS.Runtime.Culture | Should -Be $PSCulture
        $PS.Runtime.UICulture | Should -Be $PSUICulture
        $PS.Runtime.IsWindows | Should -Be ([bool]$IsWindows)
        $PS.Runtime.IsLinux | Should -Be ([bool]$IsLinux)
        $PS.Runtime.IsMacOS | Should -Be ([bool]$IsMacOS)
        $PS.Runtime.IsCoreCLR | Should -Be ([bool]$IsCoreCLR)
        $PS.Constants.True | Should -Be $true
        $PS.Constants.False | Should -Be $false
        $PS.Constants.Null | Should -BeNullOrEmpty
        $PS.LastStatementSuccessful | Should -Be $?
        { $PS.LastStatementSuccessful = $false } | Should -Throw
    }

    It 'tracks parser history tokens and success separately from exit status' {
        $tokenComparison = & {
            [pscustomobject]@{ LegacyFirst = $^; FacadeFirst = $PS.History.FirstToken; LegacyLast = $$; FacadeLast = $PS.History.LastToken }
        }
        $tokenComparison.FacadeFirst | Should -Be $tokenComparison.LegacyFirst
        $tokenComparison.FacadeLast | Should -Be $tokenComparison.LegacyLast
        $savedExitCode = Get-Variable LASTEXITCODE -Scope Global -ErrorAction Ignore
        $savedExitCodeValue = if ($null -ne $savedExitCode) { $savedExitCode.Value }
        try {
            $global:LASTEXITCODE = 7
            $PS.LastExitCode | Should -Be 7
            $null = 'success'
            $success = $PS.LastStatementSuccessful
            $success | Should -BeTrue
            Write-Error 'success-flag-failure' -ErrorAction SilentlyContinue
            $PS.LastStatementSuccessful | Should -BeFalse
            $PS.LastExitCode | Should -Be 7
        } finally {
            if ($null -ne $savedExitCode) {
                Set-Variable LASTEXITCODE $savedExitCodeValue -Scope Global
            } else {
                Remove-Variable LASTEXITCODE -Scope Global -Force -ErrorAction Ignore
            }
        }
    }

    It 'preserves runspace ownership and rejects foreign and expired bound views' {
        $captured = $PS.AtScope('Local').Session
        $runspace = [runspacefactory]::CreateRunspace()
        try {
            $runspace.Open()
            $powershell = [powershell]::Create()
            try {
                $powershell.Runspace = $runspace
                $runspace.SessionStateProxy.SetVariable('captured', $captured)
                $powershell.AddScript({ try { $captured.Host } catch { $_.Exception.Message } }) | Out-Null
                $foreign = $powershell.Invoke()
                $foreign | Should -Match 'different runspace'
            } finally { $powershell.Dispose() }
        } finally { $runspace.Dispose() }

        $expired = & { $PS.AtScope('Local').Session }
        { $expired.Host } | Should -Throw
        $PS.Session.Host | Should -Not -BeNullOrEmpty
    }
}
