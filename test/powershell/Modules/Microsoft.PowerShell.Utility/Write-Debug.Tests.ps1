# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Write-Debug tests" -Tags "CI" {
    It "Should not have added line breaks" {
        $text = "0123456789"
        try {
            while ($text.Length -lt [Console]::WindowWidth) {
                $text += $text
            }
        } catch {
            # Ignore errors if the console doesn't support WindowWidth
        }
        $origDebugPref = $DebugPreference
        $DebugPreference = "Continue"
        try {
            $out = Write-Debug $text 5>&1
            $out | Should -BeExactly $text
        }
        finally {
            $DebugPreference = $origDebugPref
        }
    }

    It "Should not prompt the user" {
        # This script generates an error if Write-Debug prompts the user
        # (i.e. if $DebugPreference is set to Inquire, the old v1 way)
        $p = [Diagnostics.Process]::new()
        $p.StartInfo.FileName = (Get-Process -Id $PID).Path
        $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes("Write-Debug -Message 'A debug message' -Debug"))
        $p.StartInfo.Arguments = "-EncodedCommand $encoded -ExecutionPolicy Bypass -NoLogo -NonInteractive -NoProfile -OutputFormat text"
        $p.StartInfo.UseShellExecute = $false
        $p.StartInfo.RedirectStandardError = $true
        $p.Start() | Out-Null
        $out = $p.StandardError.ReadToEnd()
        $out | Should -BeNullOrEmpty
    }

    It "'-Debug' should not trigger 'ShouldProcess'" {
        $pwsh = [PowerShell]::Create()
        $pwsh.AddScript(@'
function Test-DebugWithConfirm
{
    [CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Low')]
    Param ()

    PROCESS
    {
        Write-Debug -Message "Debug_Message1"
        If ($PSCmdlet.ShouldProcess('Doing the thing.','Proceed?','Ready to do the thing.'))
        {
            Write-Output 'success'
        }
        Write-Debug -Message "Debug_Message2"
    }

    END {}
}
'@)
        $pwsh.Invoke()
        $pwsh.Commands.Clear()
        $pwsh.Streams.ClearStreams()

        try {
            $result = $pwsh.AddScript("Test-DebugWithConfirm -Debug").Invoke()
            $result.Count | Should -BeExactly 1
            $result[0] | Should -BeExactly 'success'

            $pwsh.Streams.Error.Count | Should -BeExactly 0
            $pwsh.Streams.Debug.Count | Should -BeExactly 2
            $pwsh.Streams.Debug[0] | Should -BeExactly 'Debug_Message1'
            $pwsh.Streams.Debug[1] | Should -BeExactly 'Debug_Message2'
        }
        finally {
            $pwsh.Dispose()
        }
    }
}
