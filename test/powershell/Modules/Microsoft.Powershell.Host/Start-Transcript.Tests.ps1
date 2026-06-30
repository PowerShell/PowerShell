# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Start-Transcript, Stop-Transcript tests" -tags "CI" {

    BeforeAll {

        function ValidateTranscription {
            param (
                [string] $scriptToExecute,
                [string] $outputFilePath,
                [switch] $append,
                [switch] $noClobber,
                [string] $expectedError
            )
            if($append -or $noClobber) {
                #Add sample text to the file
                $content = "This is sample text!"
                $content | Out-File -FilePath $outputFilePath
                Test-Path $outputFilePath | Should -BeTrue
            }

            try {
                #execute script
                $ps = [powershell]::Create()
                $ps.addscript($scriptToExecute).Invoke()
                $ps.commands.clear()

                if($expectedError) {
                    $ps.hadErrors | Should -BeTrue
                    $ps.Streams.Error.FullyQualifiedErrorId | Should -Be $expectedError
                } else {
                    $ps.addscript("Get-Date").Invoke()
                    $ps.commands.clear()
                    $ps.addscript("Stop-Transcript").Invoke()

                    Test-Path $outputFilePath | Should -BeTrue
                    $outputFilePath | Should -FileContentMatch "Get-Date"
                    if($append) {
                        $outputFilePath | Should -FileContentMatch $content
                    }
                }
            } finally {
                if ($null -ne $ps) {
                    $ps.Dispose()
                }
            }
        }
        ## function ends here

        $transcriptFilePath = Join-Path $TestDrive "transcriptdata.txt"
        Remove-Item $transcriptFilePath -Force -ErrorAction SilentlyContinue
    }

    AfterEach {
        Remove-Item $transcriptFilePath -ErrorAction SilentlyContinue
        [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook('ForcePromptForChoiceDefaultOption', $false)
    }

    It "Should create Transcript file at default path" {
        $script = "Start-Transcript"
        if ($IsWindows) {
            $defaultTranscriptFilePath = [io.path]::Combine($env:USERPROFILE, "Documents", "PowerShell_transcript*")
        } else {
            $defaultTranscriptFilePath = [io.path]::Combine($env:HOME, "PowerShell_transcript*")
        }

        try {
            #Make sure there is no stale data
            Remove-Item $defaultTranscriptFilePath -Force -ErrorAction SilentlyContinue
            ValidateTranscription -scriptToExecute $script -outputFilePath $defaultTranscriptFilePath
        } finally {
            #Remove test data
            Remove-Item $defaultTranscriptFilePath -ErrorAction SilentlyContinue
        }
    }
    It "Should create Transcript file with 'Path' parameter" {
        $script = "Start-Transcript -path $transcriptFilePath"
        ValidateTranscription -scriptToExecute $script -outputFilePath $transcriptFilePath
    }
    It "Should create Transcript file with 'LiteralPath' parameter" {
        $script = "Start-Transcript -LiteralPath $transcriptFilePath"
        ValidateTranscription -scriptToExecute $script -outputFilePath $transcriptFilePath
    }
    It "Should create Transcript file with 'OutputDirectory' parameter" {
        $script = "Start-Transcript -OutputDirectory $TestDrive"
        $outputFilePath = Join-Path $TestDrive "PowerShell_transcript*"
        ValidateTranscription -scriptToExecute $script -outputFilePath $outputFilePath
    }
    It "Should create Transcript file with 'Transcript' preference variable" {
        # Casting to PSObject is necessary because Set-Variable does not automatically wrap the value in a PSObject
        $script = "Set-Variable -Scope Global -Name Transcript -Value ([PSObject]'$transcriptFilePath'); Start-Transcript"
        ValidateTranscription -scriptToExecute $script -outputFilePath $transcriptFilePath
    }
    It "Should Append Transcript data in existing file if 'Append' parameter is used with Path parameter" {
        $script = "Start-Transcript -path $transcriptFilePath -Append"
        ValidateTranscription -scriptToExecute $script -outputFilePath $transcriptFilePath -append
    }
    It "Should return an error if the file exists and NoClobber is used" {
        $script = "Start-Transcript -path $transcriptFilePath -NoClobber"
        $expectedError = "NoClobber,Microsoft.PowerShell.Commands.StartTranscriptCommand"
        ValidateTranscription -scriptToExecute $script -outputFilePath $transcriptFilePath -noClobber -expectedError $expectedError
    }
    It "Should return an error if the path resolves to an existing directory" {
        $script = "Start-Transcript -path $TestDrive"
        $expectedError = "CannotStartTranscription,Microsoft.PowerShell.Commands.StartTranscriptCommand"
        ValidateTranscription -scriptToExecute $script -outputFilePath $null -expectedError $expectedError
    }
    It "Should return an error if file path is invalid" {
        $fileName = (Get-Random).ToString()
        $inputPath = Join-Path $TestDrive $fileName
        $null = New-Item -Path $inputPath -ItemType File -Force -ErrorAction SilentlyContinue
        $script = "Start-Transcript -OutputDirectory $inputPath"
        $expectedError = "CannotStartTranscription,Microsoft.PowerShell.Commands.StartTranscriptCommand"
        ValidateTranscription -scriptToExecute $script -outputFilePath $null -expectedError $expectedError
    }
    It "Should not delete the file if it already exist" {
        # Create an existing file
        $transcriptFilePath = Join-Path $TestDrive ([System.IO.Path]::GetRandomFileName())
        Out-File $transcriptFilePath

        $FileSystemWatcher = [System.IO.FileSystemWatcher]::new((Split-Path -Parent $transcriptFilePath), (Split-Path -Leaf $transcriptFilePath))

        $Job = Register-ObjectEvent -InputObject $FileSystemWatcher -EventName "Deleted" -SourceIdentifier "FileDeleted" -Action {
            return "FileDeleted"
        }

        try {
            Start-Transcript -Path $transcriptFilePath
            Stop-Transcript
        } finally {
            Unregister-Event -SourceIdentifier "FileDeleted"
        }

        # Nothing should have been returned by the FileSystemWatcher
        Receive-Job $job | Should -Be $null
    }
    It "Transcription should remain active if other runspace in the host get closed" {
        try {
            $ps = [powershell]::Create()
            $ps.addscript("Start-Transcript -path $transcriptFilePath").Invoke()
            $ps.addscript('$rs = [system.management.automation.runspaces.runspacefactory]::CreateRunspace()').Invoke()
            $ps.addscript('$rs.open()').Invoke()
            $ps.addscript('$rs.Dispose()').Invoke()
            $ps.addscript('Write-Host "After Dispose"').Invoke()
            $ps.addscript("Stop-Transcript").Invoke()
        } finally {
            if ($null -ne $ps) {
                $ps.Dispose()
            }
        }

        $transcriptFilePath | Should -Exist
        $transcriptFilePath | Should -FileContentMatch "After Dispose"
    }

    It "Transcription should be closed if the only runspace gets closed" {
        & "$PSHOME/pwsh" -c "start-transcript $transcriptFilePath; Write-Host ''Before Dispose'';"

        $transcriptFilePath | Should -Exist
        $transcriptFilePath | Should -FileContentMatch "Before Dispose"
        $transcriptFilePath | Should -FileContentMatch "PowerShell transcript end"
    }

    It "Transcription should record native command output" {
        $script = {
            Start-Transcript -Path $transcriptFilePath
            hostname
            Stop-Transcript
        }

        & $script

        $transcriptFilePath | Should -Exist
        $machineName = [System.Environment]::MachineName
        $transcriptFilePath | Should -FileContentMatch $machineName
    }

    It "Transcription should record Write-Information output when InformationAction is set to Continue" {
        [string]$message = New-Guid
        $script = {
            Start-Transcript -Path $transcriptFilePath
            Write-Information -Message $message -InformationAction Continue
            Stop-Transcript
        }

        & $script

        $transcriptFilePath | Should -Exist
        $transcriptFilePath | Should -Not -FileContentMatch "INFO: "
        $transcriptFilePath | Should -FileContentMatch $message
    }

    It "Transcription should not record Write-Information output when InformationAction is set to SilentlyContinue" {
        [string]$message = New-Guid
        $traceData = Join-Path $TESTDRIVE tracedata.txt
        $script = {
            Start-Transcript -Path $transcriptFilePath
            Trace-Command -File $traceData -Name param* { Write-Information -Message $message -InformationAction SilentlyContinue }
            Stop-Transcript
        }

        & $script

        $transcriptFilePath | Should -Exist
        $transcriptFilePath | Should -Not -FileContentMatch "INFO: "
        $transcriptFilePath | Should -Not -FileContentMatch $message -Because (get-content $transcriptFilePath,$traceData)
    }

    It "Transcription should not record Write-Information output when InformationAction is set to Ignore" {
        [string]$message = New-Guid
        $script = {
            Start-Transcript -Path $transcriptFilePath
            Write-Information -Message $message -InformationAction Ignore
            Stop-Transcript
        }

        & $script

        $transcriptFilePath | Should -Exist
        $transcriptFilePath | Should -Not -FileContentMatch "INFO: "
        $transcriptFilePath | Should -Not -FileContentMatch $message
    }

    It "Transcription should record Write-Information output in correct order when InformationAction is set to Inquire" {
        [string]$message = New-Guid
        $newLine = [System.Environment]::NewLine
        $expectedContent = "$message$($newLine)Confirm$($newLine)Continue with this operation?"
        $script = {
            [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook('ForcePromptForChoiceDefaultOption', $true)
            Start-Transcript -Path $transcriptFilePath
            Write-Information -Message $message -InformationAction Inquire
            Stop-Transcript
        }

        & $script

        $transcriptFilePath | Should -Exist
        $transcriptFilePath | Should -Not -FileContentMatch "INFO: "
        $transcriptFilePath | Should -FileContentMatchMultiline $expectedContent
    }

    It "Transcription should record Write-Host output when InformationAction is set to Continue" {
        [string]$message = New-Guid
        $script = {
            Start-Transcript -Path $transcriptFilePath
            Write-Host -Message $message -InformationAction Continue
            Stop-Transcript
        }

        & $script

        $transcriptFilePath | Should -Exist
        $transcriptFilePath | Should -FileContentMatch $message
    }

    It "Transcription should record Write-Host output when InformationAction is set to SilentlyContinue" {
        [string]$message = New-Guid
        $script = {
            Start-Transcript -Path $transcriptFilePath
            Write-Host -Message $message -InformationAction SilentlyContinue
            Stop-Transcript
        }

        & $script

        $transcriptFilePath | Should -Exist
        $transcriptFilePath | Should -FileContentMatch $message
    }

    It "Transcription should not record Write-Host output when InformationAction is set to Ignore" {
        [string]$message = New-Guid
        $script = {
            Start-Transcript -Path $transcriptFilePath
            Write-Host -Message $message -InformationAction Ignore
            Stop-Transcript
        }

        & $script

        $transcriptFilePath | Should -Exist
        $transcriptFilePath | Should -Not -FileContentMatch $message
    }

    It "Transcription should record Write-Host output in correct order when InformationAction is set to Inquire" {
        [string]$message = New-Guid
        $newLine = [System.Environment]::NewLine
        $expectedContent = "$message$($newLine)Confirm$($newLine)Continue with this operation?"
        $script = {
            [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook('ForcePromptForChoiceDefaultOption', $true)
            Start-Transcript -Path $transcriptFilePath
            Write-Host -Message $message -InformationAction Inquire
            Stop-Transcript
        }

        & $script

        $transcriptFilePath | Should -Exist
        $transcriptFilePath | Should -FileContentMatchMultiline $expectedContent
    }

    It "UseMinimalHeader should reduce length of transcript header" {
        $script = {
            Start-Transcript -Path $transcriptFilePath
            Stop-Transcript
        }

        $transcriptMinHeaderFilePath = $transcriptFilePath + "_minimal"
        $scriptMinHeader = {
            Start-Transcript -Path $transcriptMinHeaderFilePath -UseMinimalHeader
            Stop-Transcript
        }

        & $script
        $transcriptFilePath | Should -Exist
        $transcriptLength = (Get-Content -Path $transcriptFilePath -Raw).Length

        & $scriptMinHeader
        $transcriptMinHeaderFilePath | Should -Exist
        $transcriptMinHeaderLength = (Get-Content -Path $transcriptMinHeaderFilePath -Raw).Length
        Remove-Item $transcriptMinHeaderFilePath -ErrorAction SilentlyContinue

        $transcriptMinHeaderLength | Should -BeLessThan $transcriptLength
    }
}
