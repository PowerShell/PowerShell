# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# this function wraps native command Execution
# for more information, read https://mnaoumov.wordpress.com/2015/01/11/execution-of-external-commands-in-powershell-done-right/
function script:Start-NativeExecution {
    param(
        [Alias('sb')]
        [Parameter(Mandatory=$true)]
        [scriptblock]$ScriptBlock,
        [switch]$IgnoreExitcode,
        [switch]$VerboseOutputOnError
    )

    $backupEAP = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    Write-Verbose "Executing: $ScriptBlock"
    try {
        $cwd = Get-Location

        if ($VerboseOutputOnError.IsPresent) {
            $output = & $ScriptBlock 2>&1
        } else {
            & $ScriptBlock
        }

        # note, if $ScriptBlock doesn't have a native invocation, $LASTEXITCODE will
        # point to the obsolete value
        if ($LASTEXITCODE -ne 0 -and -not $IgnoreExitcode) {
            if ($VerboseOutputOnError.IsPresent -and $output) {
                $output | Out-String | Write-Verbose -Verbose
            }

            # Get caller location for easier debugging
            $caller = Get-PSCallStack -ErrorAction SilentlyContinue
            if ($caller) {
                $callerLocationParts = $caller[1].Location -split ":\s*line\s*"
                $callerFile = $callerLocationParts[0]
                $callerLine = $callerLocationParts[1]

                $errorMessage = "Execution of {$ScriptBlock} in '$cwd' by ${callerFile}: line $callerLine failed with exit code $LASTEXITCODE"
                throw $errorMessage
            }
            throw "Execution of {$ScriptBlock} in '$cwd' failed with exit code $LASTEXITCODE"
        }
    } finally {
        $ErrorActionPreference = $backupEAP
    }
}
