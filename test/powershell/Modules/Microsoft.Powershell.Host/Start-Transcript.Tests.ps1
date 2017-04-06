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
                Test-Path $outputFilePath | Should be $true
            }

            try {
                #execute script
                $ps = [powershell]::Create()
                $ps.addscript($scriptToExecute).Invoke()
                $ps.commands.clear()

                if($expectedError) {
                    $ps.hadErrors | Should be $true
                    $ps.Streams.Error.FullyQualifiedErrorId | Should be $expectedError
                } else {
                    $ps.addscript("Get-Date").Invoke()
                    $ps.commands.clear()
                    $ps.addscript("Stop-Transcript").Invoke()

                    Test-Path $outputFilePath | Should be $true
                    $outputFilePath | should contain "Get-Date"
                    if($append) {
                        $outputFilePath | Should contain $content
                    }
                }
            } finally {
                if ($ps -ne $null) {
                    $ps.Dispose()
                }
            }
        }
        ## function ends here

        $transcriptFilePath = join-path $TestDrive "transcriptdata.txt"
        Remove-Item $transcriptFilePath -Force -ErrorAction SilentlyContinue
    }


    AfterEach {
        Remove-Item $transcriptFilePath -ErrorAction SilentlyContinue
    }

    It "Should create Transcript file at default path" {
        $script = "Start-Transcript"
        if ($isWindows) {
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
        $outputFilePath = join-path $TestDrive "PowerShell_transcript*"
        ValidateTranscription -scriptToExecute $script -outputFilePath $outputFilePath
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
        $inputPath = join-path $TestDrive $fileName
        $null = New-Item -Path $inputPath -ItemType File -Force -ErrorAction SilentlyContinue
        $script = "Start-Transcript -OutputDirectory $inputPath"
        $expectedError = "CannotStartTranscription,Microsoft.PowerShell.Commands.StartTranscriptCommand"
        ValidateTranscription -scriptToExecute $script -outputFilePath $null -expectedError $expectedError
    }
}