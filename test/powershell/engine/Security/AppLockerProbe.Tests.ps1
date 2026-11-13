# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "AppLocker policy test files at startup" -Tags 'CI' {
    BeforeAll {
        $skipTest = -not $IsWindows
        if (-not $skipTest) {
            Import-Module HelpersCommon

            $systemPolicy = [System.Management.Automation.Security.SystemPolicy]
            $flags = [System.Reflection.BindingFlags]'NonPublic, Static'
            $hasScriptPolicy = $systemPolicy.GetMethod('HasAppLockerOrSaferScriptPolicy', $flags).Invoke($null, $null)

            # When WDAC enforces lockdown, the AppLocker check is never reached.
            $null = $systemPolicy::GetSystemLockdownPolicy()
            $wldpPolicy = $systemPolicy.GetField('s_cachedWldpSystemPolicy', $flags).GetValue($null)
            $expectTestFiles = $hasScriptPolicy -and ($wldpPolicy -ne [System.Management.Automation.Security.SystemEnforcementMode]::Enforce)
        }
    }

    It "Writes __PSScriptPolicyTest files only when a script policy may exist" -Skip:$skipTest {
        $tempDir = Join-Path $TestDrive 'temp'
        $null = New-Item -ItemType Directory -Path $tempDir
        $watcher = [System.IO.FileSystemWatcher]::new($tempDir, '__PSScriptPolicyTest_*')
        $sourceId = 'AppLockerProbe_' + [guid]::NewGuid()
        $null = Register-ObjectEvent -InputObject $watcher -EventName Created -SourceIdentifier $sourceId
        $watcher.EnableRaisingEvents = $true

        $oldTemp = $env:TEMP
        $oldTmp = $env:TMP
        try {
            $env:TEMP = $tempDir
            $env:TMP = $tempDir
            Start-NativeExecution { & "$PSHOME/pwsh" -NoProfile -NonInteractive -Command exit }
            Start-Sleep -Milliseconds 500
            $created = @(Get-Event -SourceIdentifier $sourceId -ErrorAction Ignore)
        }
        finally {
            $env:TEMP = $oldTemp
            $env:TMP = $oldTmp
            Unregister-Event -SourceIdentifier $sourceId
            Remove-Event -SourceIdentifier $sourceId -ErrorAction Ignore
            $watcher.Dispose()
        }

        if ($expectTestFiles) {
            $created.Count | Should -BeGreaterThan 0
        }
        else {
            $created.Count | Should -Be 0
        }
    }
}
