# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "New-Object" -Tags "CI" {
    It "Support 'ComObject' parameter on platforms" {
        if ($IsLinux -or $IsMacOS ) {
            { New-Object -ComObject "Shell.Application" } | Should -Throw -ErrorId "NamedParameterNotFound,Microsoft.PowerShell.Commands.NewObjectCommand"
        } else {
            # It works on NanoServer and IoT too
            (Get-Command "New-Object").Parameters.ContainsKey("ComObject") | Should -BeTrue
        }
    }

    It "should create an object with 4 fields" {
        $o = New-Object psobject
        $val = $o.GetType()

        $val.IsPublic       | Should -Not -BeNullOrEmpty
        $val.Name           | Should -Not -BeNullOrEmpty
        $val.IsSerializable | Should -Not -BeNullOrEmpty
        $val.BaseType       | Should -Not -BeNullOrEmpty

        $val.IsPublic       | Should -BeTrue
        $val.IsSerializable | Should -BeFalse
        $val.Name           | Should -Be 'PSCustomObject'
        $val.BaseType       | Should -Be 'System.Object'
    }

    It "should create an object with using Property switch" {
        $hash = @{
            FirstVal = 'test1'
            SecondVal = 'test2'
        }
        $o = New-Object psobject -Property $hash

        $o.FirstVal     | Should -Be 'test1'
        $o.SecondVal    | Should -Be 'test2'
    }

    It "should create a .Net object with using ArgumentList switch" {
        $o = New-Object -TypeName System.Version -ArgumentList "1.2.3.4"
        $o.GetType() | Should -Be ([System.Version])

        $o      | Should -BeExactly "1.2.3.4"
    }
}

Describe "New-Object DRT basic functionality" -Tags "CI" {
    It "New-Object with int array should work"{
        $result = New-Object -TypeName int[] -Arg 10
        $result.Count | Should -Be 10
    }

    It "New-Object with char should work"{
        $result = New-Object -TypeName char
        $result.Count | Should -Be 1
        $defaultChar = [char]0
        ([char]$result) | Should -Be $defaultChar
    }

    It "New-Object with default Coordinates should work"{
        $result = New-Object -TypeName System.Management.Automation.Host.Coordinates
        $result.Count | Should -Be 1
        $result.X | Should -Be 0
        $result.Y | Should -Be 0
    }

    It "New-Object with specified Coordinates should work"{
        $result = New-Object -TypeName System.Management.Automation.Host.Coordinates -ArgumentList 1,2
        $result.Count | Should -Be 1
        $result.X | Should -Be 1
        $result.Y | Should -Be 2
    }

    It "New-Object with Employ should work"{
        if(-not ([System.Management.Automation.PSTypeName]'Employee').Type)
        {
            Add-Type -TypeDefinition "public class Employee{public Employee(string firstName,string lastName,int yearsInMS){FirstName = firstName;LastName=lastName;YearsInMS = yearsInMS;}public string FirstName;public string LastName;public int YearsInMS;}"
        }
        $result = New-Object -TypeName Employee -ArgumentList "Mary", "Soe", 11
        $result.Count | Should -Be 1
        $result.FirstName | Should -BeExactly "Mary"
        $result.LastName | Should -BeExactly "Soe"
        $result.YearsInMS | Should -Be 11
    }

    It "New-Object with invalid type should throw Exception"{
        $e = { New-Object -TypeName LiarType -ErrorAction Stop } | Should -Throw -ErrorId "TypeNotFound,Microsoft.PowerShell.Commands.NewObjectCommand" -PassThru
        $e.CategoryInfo | Should -Match "PSArgumentException"
    }

    It "New-Object with invalid argument should throw Exception"{
        $e = { New-Object -TypeName System.Management.Automation.PSVariable -ArgumentList "A", 1, None, "asd" -ErrorAction Stop } |
	        Should -Throw -ErrorId "ConstructorInvokedThrowException,Microsoft.PowerShell.Commands.NewObjectCommand" -PassThru
        $e.CategoryInfo | Should -Match "MethodException"
    }

    It "New-Object with abstract class should throw Exception"{
        Add-Type -TypeDefinition "public abstract class AbstractEmployee{public AbstractEmployee(){}}"
        $e = { New-Object -TypeName AbstractEmployee -ErrorAction Stop } |
		Should -Throw -ErrorId "ConstructorInvokedThrowException,Microsoft.PowerShell.Commands.NewObjectCommand" -PassThru
        $e.CategoryInfo | Should -Match "MethodInvocationException"
    }

    It "New-Object with bad argument for class constructor should throw Exception"{
        if(-not ([System.Management.Automation.PSTypeName]'Employee').Type)
        {
            Add-Type -TypeDefinition "public class Employee{public Employee(string firstName,string lastName,int yearsInMS){FirstName = firstName;LastName=lastName;YearsInMS = yearsInMS;}public string FirstName;public string LastName;public int YearsInMS;}"
        }
        $e = { New-Object -TypeName Employee -ArgumentList 11 -ErrorAction Stop } | Should -Throw -ErrorId "ConstructorInvokedThrowException,Microsoft.PowerShell.Commands.NewObjectCommand" -PassThru
        $e.CategoryInfo | Should -Match "MethodException"
    }

    #This case will throw "Execution OK" now, just mark as pending now
    It "New-Object with not init class constructor should throw Exception" -Pending{
        if(-not ([System.Management.Automation.PSTypeName]'Employee').Type)
        {
           Add-Type -TypeDefinition "public class Employee{public Employee(string firstName,string lastName,int yearsInMS){FirstName = firstName;LastName=lastName;YearsInMS = yearsInMS;}public string FirstName;public string LastName;public int YearsInMS;}"
        }
        { New-Object -TypeName Employee -ErrorAction Stop } | Should -Throw -ErrorId "CannotFindAppropriateCtor,Microsoft.PowerShell.Commands.NewObjectCommand"
    }

    It "New-Object with Private Nested class should throw Exception"{
        Add-Type -TypeDefinition "public class WeirdEmployee{public WeirdEmployee(){}private class PrivateNestedWeirdEmployee{public PrivateNestedWeirdEmployee(){}}}"
        $e = { New-Object -TypeName WeirdEmployee+PrivateNestedWeirdEmployee -ErrorAction Stop } | Should -Throw -ErrorId "TypeNotFound,Microsoft.PowerShell.Commands.NewObjectCommand" -PassThru
        $e.CategoryInfo | Should -Match "PSArgumentException"
    }

    It "New-Object with TypeName and Property parameter should work"{
        $result = New-Object -TypeName PSObject -Property @{foo=123}
        $result.foo | Should -Be 123
    }
}

try
{
    $defaultParamValues = $PSDefaultParameterValues.Clone()
    $PSDefaultParameterValues["it:skip"] = ![System.Management.Automation.Platform]::IsWindowsDesktop

    Describe "New-Object COM functionality" -Tags "CI" {
        $testCases = @(
            @{
                Name   = 'Microsoft.Update.AutoUpdate'
                Property = 'Settings'
                Type = 'Object'
            }
            @{
                Name   = 'Microsoft.Update.SystemInfo'
                Property = 'RebootRequired'
                Type = 'Bool'
            }
        )

        It "Should be able to create <Name> with property <Property> of Type <Type>" -TestCases $testCases {
            param($Name, $Property, $Type)
            $comObject = New-Object -ComObject $name
            $comObject.$Property | Should -Not -BeNullOrEmpty
            $comObject.$Property | Should -BeOfType $Type
        }

        It "Should fail with correct error when creating a COM object that dose not exist" {
            {New-Object -ComObject 'doesnotexist'} | Should -Throw -ErrorId 'NoCOMClassIdentified,Microsoft.PowerShell.Commands.NewObjectCommand'
        }
    }
}
finally
{
    $global:PSdefaultParameterValues = $defaultParamValues
}
