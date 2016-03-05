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