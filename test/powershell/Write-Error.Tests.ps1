Describe "Write-Error DRT Unit Tests" -Tags DRT{
    It "Should be works with command: write-error myerrortext" {
        Write-Error myerrortext -ErrorAction SilentlyContinue 
        $Error[0] | Should Not BeNullOrEmpty
        $Error[0].GetType().Name | Should Be 'ErrorRecord'
        
        #Exception verification
        $Error[0].Exception.GetType().Name | Should Be 'WriteErrorException'
        $Error[0].Exception.Message | Should Be 'myerrortext'
        $Error[0].Exception.Data.Count | Should Be 0
        $Error[0].Exception.InnerException | Should BeNullOrEmpty 
        
        #ErrorCategoryInfo verification
        $Error[0].CategoryInfo | Should Not BeNullOrEmpty
        $Error[0].CategoryInfo.Category | Should Be 'NotSpecified'
        $Error[0].CategoryInfo.Activity | Should Be 'Write-Error'
        $Error[0].CategoryInfo.Reason | Should Be 'WriteErrorException'
        $Error[0].CategoryInfo.TargetName | Should BeNullOrEmpty
        $Error[0].CategoryInfo.TargetType | Should BeNullOrEmpty
        $Error[0].CategoryInfo.GetMessage() | Should Be 'NotSpecified: (:) [Write-Error], WriteErrorException'

        #ErrorDetails verification
        $Error[0].ErrorDetails | Should BeNullOrEmpty

        #FullyQualifiedErrorId verification 
        $Error[0].FullyQualifiedErrorId | Should Be 'Microsoft.PowerShell.Commands.WriteErrorException'

        #InvocationInfo verification
        $Error[0].InvocationInfo | Should Not BeNullOrEmpty
        $Error[0].InvocationInfo.MyCommand.Name | Should BeNullOrEmpty     
    }

    #Blocked by issue #846
    It "Should be works with all parameters" -Pending { 
        $exception = New-Object -TypeName System.ArgumentNullException -ArgumentList paramname 
        Write-Error -Message myerrortext -Exception $exception -ErrorId myerrorid -Category syntaxerror -TargetObject TargetObject -CategoryActivity myactivity -CategoryReason myreason -CategoryTargetName mytargetname -CategoryTargetType mytargettype -RecommendedAction myrecommendedaction -ErrorAction SilentlyContinue
        $Error[0] | Should Not BeNullOrEmpty
        $Error[0].GetType().Name | Should Be 'ErrorRecord'

        #Exception verification
        $Error[0].Exception | Should Not BeNullOrEmpty
        $Error[0].Exception.GetType().Name | Should Be 'ArgumentNullException'        
        $Error[0].Exception.ParamName | Should Be 'paramname'
        $Error[0].Exception.Data.Count | Should Be 0
        $Error[0].Exception.InnerException | Should BeNullOrEmpty  
        
        #TargetObject verification 
        $Error[0].TargetObject | Should Be 'TargetObject'

        #FullyQualifiedErrorId verification
        $Error[0].FullyQualifiedErrorId | Should Be 'myerrorid'

        #ErrorCategoryInfo verification
        $Error[0].CategoryInfo | Should Not BeNullOrEmpty
        $Error[0].CategoryInfo.Category | Should Be 'SyntaxError'
        $Error[0].CategoryInfo.Activity | Should Be 'myactivity'
        $Error[0].CategoryInfo.Reason | Should Be 'myreason'
        $Error[0].CategoryInfo.TargetName | Should Be 'mytargetname'
        $Error[0].CategoryInfo.TargetType | Should Be 'mytargettype'
        $Error[0].CategoryInfo.GetMessage() | Should Be 'SyntaxError: (mytargetname:mytargettype) [myactivity], myreason'

        #ErrorDetails verification
        $Error[0].ErrorDetails | Should Not BeNullOrEmpty
        $Error[0].ErrorDetails.Message | Should Be 'myerrortext'
        $Error[0].ErrorDetails.RecommendedAction | Should Be 'myrecommendedaction'

        #InvocationInfo verification
        $Error[0].InvocationInfo | Should Not BeNullOrEmpty
        $Error[0].InvocationInfo.MyCommand.Name | Should BeNullOrEmpty  
    }

    #Blocked by issue #846
    It "Should be works with all parameters" -Pending {
        write-error -Activity fooAct -Reason fooReason -TargetName fooTargetName -TargetType fooTargetType -Message fooMessage
        $Error[0].CategoryInfo.Activity | Should Be 'fooAct'
        $Error[0].CategoryInfo.Reason | Should Be 'fooReason'
        $Error[0].CategoryInfo.TargetName | Should Be 'fooTargetName'
        $Error[0].CategoryInfo.TargetType | Should Be 'fooTargetType'
        $Error[0].CategoryInfo.GetMessage() | Should Be 'NotSpecified: (fooTargetName:fooTargetType) [fooAct], fooReason'
    }
}

Describe "Write-Error" {
    It "Should be able to throw" {
	Write-Error "test throw" -ErrorAction SilentlyContinue | Should Throw
    }

    It "Should throw a non-terminating error" {
	Write-Error "test throw" -ErrorAction SilentlyContinue

	1 + 1 | Should Be 2
    }

    It "Should trip an exception using the exception switch" {
	$var = 0
	try
	{
	    Write-Error -Exception -Message "test throw"
	}
	catch [System.Exception]
	{

	    $var++
	}
	finally
	{
	    $var | Should Be 1
	}
    }

    It "Should output the error message to the `$error automatic variable" {
	$theError = "Error: Too many input values."
	write-error -message $theError -category InvalidArgument -ErrorAction SilentlyContinue

	$error[0]| Should Be $theError
    }
}