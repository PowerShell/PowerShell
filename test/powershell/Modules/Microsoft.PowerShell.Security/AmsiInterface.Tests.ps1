# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

try
{
    $defaultParamValues = $PSDefaultParameterValues.Clone()
    $PSDefaultParameterValues["it:Skip"] = !$IsWindows

    Describe "AMSI scan should detect suspicious content" -Tags 'Feature','RequireAdminOnWindows' {

        BeforeAll {
            [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook("UseDebugAmsiImplementation", $true)
        }

        AfterAll {
            [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook("UseDebugAmsiImplementation", $false)
        }

        It "Verifies AMSI scan detects debug suspicious content" {

            $EICAR_STRING_B64 = "awZ8EmMWc3JjaAdvY2lrBgcbY20aBHBwGgROF3Z6cHJhHmBncn13cmF3HnJ9Z3plemFmYB5ndmBnHnV6f3YSF3sYexk= "
            $bytes = [System.Convert]::FromBase64String($EICAR_STRING_B64)
            $EICAR_STRING = -join ($bytes | ForEach-Object { [char]($_ -bxor 0x33) })
            { Invoke-Expression -Command "echo '$EICAR_STRING'" } |
                Should -Throw -ErrorId "ScriptContainedMaliciousContent,Microsoft.PowerShell.Commands.InvokeExpressionCommand"
        }
    }
}
finally
{
    if ($defaultParamValues -ne $null)
    {
        $Global:PSDefaultParameterValues = $defaultParamValues
    }
}
