# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Write-Error Tests" -Tags "CI" {
    It "Should be works with command: write-error myerrortext" {
        $e = Write-Error myerrortext 2>&1
        $e | Should -BeOfType 'System.Management.Automation.ErrorRecord'

        #Exception verification
        $e.Exception | Should -BeOfType 'Microsoft.PowerShell.Commands.WriteErrorException'
        $e.Exception.Message | Should -Be 'myerrortext'
        $e.Exception.Data.Count | Should -Be 0
        $e.Exception.InnerException | Should -BeNullOrEmpty

        #ErrorCategoryInfo verification
        $e.CategoryInfo | Should -Not -BeNullOrEmpty
        $e.CategoryInfo.Category | Should -Be 'NotSpecified'
        $e.CategoryInfo.Activity | Should -Be 'Write-Error'
        $e.CategoryInfo.Reason | Should -Be 'WriteErrorException'
        $e.CategoryInfo.TargetName | Should -BeNullOrEmpty
        $e.CategoryInfo.TargetType | Should -BeNullOrEmpty
        $e.CategoryInfo.GetMessage() | Should -Be 'NotSpecified: (:) [Write-Error], WriteErrorException'

        #ErrorDetails verification
        $e.ErrorDetails | Should -BeNullOrEmpty

        #FullyQualifiedErrorId verification
        $e.FullyQualifiedErrorId | Should -BeExactly 'Microsoft.PowerShell.Commands.WriteErrorException'

        #InvocationInfo verification
        $e.InvocationInfo | Should -Not -BeNullOrEmpty
        $e.InvocationInfo.MyCommand.Name | Should -BeNullOrEmpty
    }

    It "Should be works with all parameters" {
        $exception = New-Object -TypeName System.ArgumentNullException -ArgumentList paramname
        $e = Write-Error -Message myerrortext -Exception $exception -ErrorId myerrorid -Category syntaxerror -TargetObject TargetObject -CategoryActivity myactivity -CategoryReason myreason -CategoryTargetName mytargetname -CategoryTargetType mytargettype -RecommendedAction myrecommendedaction 2>&1
        $e | Should -Not -BeNullOrEmpty
        $e | Should -BeOfType 'System.Management.Automation.ErrorRecord'

        #Exception verification
        $e.Exception | Should -BeOfType 'System.ArgumentNullException'
        $e.Exception.ParamName | Should -Be 'paramname'
        $e.Exception.Data.Count | Should -Be 0
        $e.Exception.InnerException | Should -BeNullOrEmpty

        #TargetObject verification
        $e.TargetObject | Should -Be 'TargetObject'

        #FullyQualifiedErrorId verification
        $e.FullyQualifiedErrorId | Should -BeExactly 'myerrorid'

        #ErrorCategoryInfo verification
        $e.CategoryInfo | Should -Not -BeNullOrEmpty
        $e.CategoryInfo.Category | Should -Be 'SyntaxError'
        $e.CategoryInfo.Activity | Should -Be 'myactivity'
        $e.CategoryInfo.Reason | Should -Be 'myreason'
        $e.CategoryInfo.TargetName | Should -Be 'mytargetname'
        $e.CategoryInfo.TargetType | Should -Be 'mytargettype'
        $e.CategoryInfo.GetMessage() | Should -Be 'SyntaxError: (mytargetname:mytargettype) [myactivity], myreason'

        #ErrorDetails verification
        $e.ErrorDetails | Should -Not -BeNullOrEmpty
        $e.ErrorDetails.Message | Should -Be 'myerrortext'
        $e.ErrorDetails.RecommendedAction | Should -Be 'myrecommendedaction'

        #InvocationInfo verification
        $e.InvocationInfo | Should -Not -BeNullOrEmpty
        $e.InvocationInfo.MyCommand.Name | Should -BeNullOrEmpty
    }

    It "Should be works with all parameters" {
        $e = write-error -Activity fooAct -Reason fooReason -TargetName fooTargetName -TargetType fooTargetType -Message fooMessage 2>&1
        $e.CategoryInfo.Activity | Should -Be 'fooAct'
        $e.CategoryInfo.Reason | Should -Be 'fooReason'
        $e.CategoryInfo.TargetName | Should -Be 'fooTargetName'
        $e.CategoryInfo.TargetType | Should -Be 'fooTargetType'
        $e.CategoryInfo.GetMessage() | Should -Be 'NotSpecified: (fooTargetName:fooTargetType) [fooAct], fooReason'
    }

    It "Should be able to throw with -ErrorAction stop" {
    	{ Write-Error "test throw" -ErrorAction Stop } | Should -Throw
    }

    It "Should throw a non-terminating error" {
        { Write-Error "test throw" -ErrorAction SilentlyContinue } | Should -Not -Throw
    }

    It "Should trip an exception using the exception switch" {
        { Write-Error -Exception -Message "test throw" } | Should -Throw -ErrorId "MissingArgument,Microsoft.PowerShell.Commands.WriteErrorCommand"
    }

    It "Should output the error message to the `$error automatic variable" {
        $theError = "Error: Too many input values."
        write-error -message $theError -category InvalidArgument -ErrorAction SilentlyContinue

        [string]$error[0]| Should -Be $theError
    }

    It "ErrorRecord should not be truncated or have inserted newlines when redirected from another process" {
        $longtext = "0123456789"
        while ($longtext.Length -lt [console]::WindowWidth) {
            $longtext += $longtext
        }
        $pwsh = $pshome + "/pwsh"
        $result = & $pwsh -c Write-Error -Message $longtext 2>&1
        $result.Count | Should -BeExactly 4
        $result[0] | Should -Match $longtext
    }
}
