Describe "New-Object" -Tags "CI" {
    It "should create an object with 4 fields" {
        $o = New-Object psobject
	    $val = $o.GetType()

	    $val.IsPublic       | Should Not BeNullOrEmpty
	    $val.Name           | Should Not BeNullOrEmpty
	    $val.IsSerializable | Should Not BeNullOrEmpty
	    $val.BaseType       | Should Not BeNullOrEmpty

	    $val.IsPublic       | Should Be $true
	    $val.IsSerializable | Should Be $false
	    $val.Name           | Should Be 'PSCustomObject'
	    $val.BaseType       | Should Be 'System.Object'
    }

    It "should create an object with using Property switch" {
        $hash = @{
            FirstVal = 'test1'
            SecondVal = 'test2'
        }
	    $o = New-Object psobject -Property $hash

	    $o.FirstVal     | Should Be 'test1'
	    $o.SecondVal    | Should Be 'test2'
    }

    It "should create a .Net object with using ArgumentList switch" {
	    $o = New-Object -TypeName System.Version -ArgumentList "1.2.3.4"
        $o.GetType() | Should Be ([System.Version])

	    $o      | Should Be "1.2.3.4"
    }
}

Describe "New-Object DRT basic functionality" -Tags "CI" {
	It "New-Object with int array should work"{
		$result = New-Object -TypeName int[] -Arg 10
		$result.Count | Should Be 10
	}
	
	It "New-Object with char should work"{
		$result = New-Object -TypeName char
		$result.Count | Should Be 1
		$defaultChar = [char]0
		([char]$result) | Should Be $defaultChar
	}
	
	It "New-Object with default Coordinates should work"{
		$result = New-Object -TypeName System.Management.Automation.Host.Coordinates
		$result.Count | Should Be 1
		$result.X | Should Be 0
		$result.Y | Should Be 0
	}
	
	It "New-Object with specified Coordinates should work"{
		$result = New-Object -TypeName System.Management.Automation.Host.Coordinates -ArgumentList 1,2
		$result.Count | Should Be 1
		$result.X | Should Be 1
		$result.Y | Should Be 2
	}
	
	It "New-Object with Employ should work"{
		if(-not ([System.Management.Automation.PSTypeName]'Employee').Type)
		{
			Add-Type -TypeDefinition "public class Employee{public Employee(string firstName,string lastName,int yearsInMS){FirstName = firstName;LastName=lastName;YearsInMS = yearsInMS;}public string FirstName;public string LastName;public int YearsInMS;}"
		}
		$result = New-Object -TypeName Employee -ArgumentList "Mary", "Soe", 11
		$result.Count | Should Be 1
		$result.FirstName | Should Be "Mary"
		$result.LastName | Should Be "Soe"
		$result.YearsInMS | Should Be 11
	}
	
	It "New-Object with invalid type should throw Exception"{
		try
		{
			New-Object -TypeName LiarType -EA Stop
			Throw "Execution OK"
		}
		catch
		{
			$_.CategoryInfo| Should Match "PSArgumentException"
			$_.FullyQualifiedErrorId | Should be "TypeNotFound,Microsoft.PowerShell.Commands.NewObjectCommand"
		}
	}
	
	It "New-Object with invalid argument should throw Exception"{
		try
		{
			New-Object -TypeName System.Management.Automation.PSVariable -ArgumentList "A", 1, None, "asd" -EA Stop
			Throw "Execution OK"
		}
		catch
		{
			$_.CategoryInfo| Should Match "MethodException"
			$_.FullyQualifiedErrorId | Should be "ConstructorInvokedThrowException,Microsoft.PowerShell.Commands.NewObjectCommand"
		}
	}
	
	It "New-Object with abstract class should throw Exception"{
		Add-Type -TypeDefinition "public abstract class AbstractEmployee{public AbstractEmployee(){}}"
		try
		{
			New-Object -TypeName AbstractEmployee -EA Stop
			Throw "Execution OK"
		}
		catch
		{
			$_.CategoryInfo| Should Match "MethodInvocationException"
			$_.FullyQualifiedErrorId | Should be "ConstructorInvokedThrowException,Microsoft.PowerShell.Commands.NewObjectCommand"
		}
	}
	
	It "New-Object with bad argument for class constructor should throw Exception"{
		if(-not ([System.Management.Automation.PSTypeName]'Employee').Type)
		{
			Add-Type -TypeDefinition "public class Employee{public Employee(string firstName,string lastName,int yearsInMS){FirstName = firstName;LastName=lastName;YearsInMS = yearsInMS;}public string FirstName;public string LastName;public int YearsInMS;}"
		}
		try
		{
			New-Object -TypeName Employee -ArgumentList 11 -EA Stop
			Throw "Execution OK"
		}
		catch
		{
			$_.CategoryInfo| Should Match "MethodException"
			$_.FullyQualifiedErrorId | Should be "ConstructorInvokedThrowException,Microsoft.PowerShell.Commands.NewObjectCommand"
		}
	}
	
	#This case will throw "Execution OK" now, just mark as pending now
	It "New-Object with not init class constructor should throw Exception" -Pending{
		if(-not ([System.Management.Automation.PSTypeName]'Employee').Type)
		{
			Add-Type -TypeDefinition "public class Employee{public Employee(string firstName,string lastName,int yearsInMS){FirstName = firstName;LastName=lastName;YearsInMS = yearsInMS;}public string FirstName;public string LastName;public int YearsInMS;}"
		}
		try
		{
			New-Object -TypeName Employee -EA Stop
			Throw "Execution OK"
		}
		catch
		{
			$_.FullyQualifiedErrorId | Should be "CannotFindAppropriateCtor,Microsoft.PowerShell.Commands.NewObjectCommand"
		}
	}
	
	It "New-Object with Private Nested class should throw Exception"{
		Add-Type -TypeDefinition "public class WeirdEmployee{public WeirdEmployee(){}private class PrivateNestedWeirdEmployee{public PrivateNestedWeirdEmployee(){}}}"
		try
		{
			New-Object -TypeName WeirdEmployee+PrivateNestedWeirdEmployee -EA Stop
			Throw "Execution OK"
		}
		catch
		{
			$_.CategoryInfo| Should Match "PSArgumentException"
			$_.FullyQualifiedErrorId | Should be "TypeNotFound,Microsoft.PowerShell.Commands.NewObjectCommand"
		}
	}
	
	It "New-Object with TypeName and Property parameter should work"{
		$result = New-Object -TypeName PSObject -property @{foo=123}
		$result.foo | Should Be 123
	}
}
