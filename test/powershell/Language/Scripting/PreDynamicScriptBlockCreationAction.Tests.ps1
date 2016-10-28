Describe "Tests for PreDynamicScriptBlockCreationAction" -Tags "CI" {
    It 'No action' { 
        iex "`$true" | Should Be $true
        [ScriptBlock]::Create("`$true") | Should Be $true
    }

    It "Action" {
        [System.Management.Automation.CommandInvocationIntrinsics]::PreDynamicScriptBlockCreationAction = {
            param($source, [System.Management.Automation.DynamicScriptBlockCreationEventArgs]$eventArgs)
            throw $eventArgs.Script
        }

        $guid = [System.Guid]::NewGuid().ToString()

        try
        {
            iex $guid
            Throw "Execution shouldn't reach here"
        }
        catch
        {
            $_.Exception.Message | Should Be $guid
        }
    }

    It "Action removed" {
        [System.Management.Automation.CommandInvocationIntrinsics]::PreDynamicScriptBlockCreationAction = $null
        iex "`$true" | Should Be $true
    }
}
