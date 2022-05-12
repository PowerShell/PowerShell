# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Compare-Object" -Tags "CI" {
	BeforeAll {
		$nl = [Environment]::NewLine

		$content1 = "line 1" + $nl + "line 2"
		$content2 = "line 1" + $nl + "line 2.1"
		$content3 = "line 1" + $nl + "line 2" + $nl + "line 3"
		$content4 = "line 1" + $nl + "line 2.1" + $nl + "Line 3"

		$file1 = Join-Path -Path $TestDrive -ChildPath "test1.txt"
		$file2 = Join-Path -Path $TestDrive -ChildPath "test2.txt"
		$file3 = Join-Path -Path $TestDrive -ChildPath "test3.txt"
		$file4 = Join-Path -Path $TestDrive -ChildPath "test4.txt"

		$null = New-Item $file1 -ItemType file -Value $content1 -Force
		$null = New-Item $file2 -ItemType file -Value $content2 -Force
		$null = New-Item $file3 -ItemType file -Value $content3 -Force
		$null = New-Item $file4 -ItemType file -Value $content4 -Force
	}

    It "Should be able to compare the same object using the referenceObject and differenceObject switches" {
	{ Compare-Object -ReferenceObject $(Get-Content $file1) -DifferenceObject $(Get-Content $file2) } | Should -Not -Throw
    }

    It "Should not throw when referenceobject switch is not used" {
	{ Compare-Object $(Get-Content $file1) -DifferenceObject $(Get-Content $file2) } | Should -Not -Throw
    }

    It "Should not throw when differenceobject switch is not used" {
	{ Compare-Object -ReferenceObject $(Get-Content $file1) $(Get-Content $file2) } | Should -Not -Throw
    }

    It "Should indicate data that exists only in the reference dataset" {
	$actualOutput = Compare-Object -ReferenceObject $(Get-Content $file3) -DifferenceObject $(Get-Content $file4)

	$actualOutput[1].SideIndicator | Should -BeExactly "<="
    }

    It "Should indicate data that exists only in the difference dataset" {
	$actualOutput = Compare-Object -ReferenceObject $(Get-Content $file3) -DifferenceObject $(Get-Content $file4)

	$actualOutput[1].SideIndicator | Should -BeExactly "<="
    }

    It "Should indicate data that exists in both datasets when the includeEqual switch is used" {
	$actualOutput = Compare-Object -ReferenceObject $(Get-Content $file3) -DifferenceObject $(Get-Content $file4) -IncludeEqual

	$actualOutput.Length           | Should -Be 4
	$actualOutput[0].SideIndicator | Should -BeExactly "=="
	$actualOutput[1].SideIndicator | Should -BeExactly "=="
    }

    It "Should be able to use the casesensitive switch" {
	{ Compare-Object -ReferenceObject $(Get-Content $file3) -DifferenceObject $(Get-Content $file4) -CaseSensitive } | Should -Not -Throw
    }

    It "Should correctly indicate that different cases are different when the casesensitive switch is used" {
	$caOutput  = Compare-Object -ReferenceObject $(Get-Content $file3) -DifferenceObject $(Get-Content $file4) -CaseSensitive
	$ncaOutput = Compare-Object -ReferenceObject $(Get-Content $file3) -DifferenceObject $(Get-Content $file4)

	$caOutput.Length | Should -Be 4

	$ncaOutput[1].SideIndicator | Should -Not -Be $caOutput[1].SideIndicator
	$ncaOutput[2].SideIndicator | Should -Not -Be $caOutput[2].SideIndicator
	$ncaOutput[3].SideIndicator | Should -Not -Be $caOutput[3].SideIndicator

    }

    It "Should throw when reference set is null" {
	{ Compare-Object -ReferenceObject $anonexistentvariable -DifferenceObject $(Get-Content $file4) } | Should -Throw
    }

    It "Should throw when difference set is null" {
	{ Compare-Object -ReferenceObject $(Get-Content $file3) -DifferenceObject $anonexistentvariable } | Should -Throw
    }

    It "Should only display equal lines when excludeDifferent switch is used without the includeequal switch" {
    $actualOutput = Compare-Object -ReferenceObject $(Get-Content $file3) -DifferenceObject $(Get-Content $file4) -ExcludeDifferent

    $actualOutput.Length | Should -Be 2
    }

    It "Should only display equal lines when excludeDifferent switch is used alongside the includeequal switch" {
	$actualOutput = Compare-Object -ReferenceObject $(Get-Content $file3) -DifferenceObject $(Get-Content $file4) -IncludeEqual -ExcludeDifferent

	$actualOutput.Length | Should -Be 2
    }

    It "Should give a 0 array when using excludedifferent switch when also setting the includeequal switch to false" {
	$actualOutput = Compare-Object -ReferenceObject $(Get-Content $file3) -DifferenceObject $(Get-Content $file4) -ExcludeDifferent -IncludeEqual:$false

	$actualOutput.Length | Should -Be 0
    }

    It "Should be able to pass objects to pipeline using the passthru switch" {
	{ Compare-Object -ReferenceObject $(Get-Content $file3) -DifferenceObject $(Get-Content $file4) -PassThru | Format-Wide } | Should -Not -Throw
    }

    It "Should be able to specify the property of two objects to compare" {
	$actualOutput = Compare-Object -ReferenceObject $file3 -DifferenceObject $TestDrive -Property Length
	$actualOutput[0].Length | Should -BeNullOrEmpty
	$actualOutput[1].Length | Should -BeGreaterThan 0
	$actualOutput[0].Length | Should -Not -Be $actualOutput[1].Length
    }

    It "Should be able to specify the syncwindow without error" {
	{ Compare-Object -ReferenceObject $(Get-Content $file3) -DifferenceObject $(Get-Content $file4) -SyncWindow 5 } | Should -Not -Throw
	{ Compare-Object -ReferenceObject $(Get-Content $file3) -DifferenceObject $(Get-Content $file4) -SyncWindow 8 } | Should -Not -Throw
    }

    It "Should have the expected output when changing the syncwindow" {
	$var1 = 1..15
	$var2 = 15..1

	$actualOutput = Compare-Object -ReferenceObject $var1 -DifferenceObject $var2 -SyncWindow 6

	$actualOutput[0].InputObject | Should -Be 15
	$actualOutput[1].InputObject | Should -Be 1
	$actualOutput[2].InputObject | Should -Be 1
	$actualOutput[3].InputObject | Should -Be 15

	$actualOutput[0].SideIndicator | Should -BeExactly "=>"
	$actualOutput[1].SideIndicator | Should -BeExactly "<="
	$actualOutput[2].SideIndicator | Should -BeExactly "=>"
	$actualOutput[3].SideIndicator | Should -BeExactly "<="
    }
}

Describe "Compare-Object DRT basic functionality" -Tags "CI" {
		if(-not ([System.Management.Automation.PSTypeName]'Employee').Type)
		{
			Add-Type -TypeDefinition @"
    public class Employee
    {
        public Employee(){}
        public Employee(string firstName, string lastName, int yearsInMS)
        {
            FirstName = firstName;
            LastName  = lastName;
            YearsInMS = yearsInMS;
        }
        public string FirstName;
        public string LastName;
        public int YearsInMS;
    }
    public class EmployeeComparable : Employee, System.IComparable
    {
        public EmployeeComparable(
            string firstName, string lastName, int yearsInMS)
            : base(firstName, lastName, yearsInMS)
        {}

        public int CompareTo(object obj)
        {
            EmployeeComparable ec = obj as EmployeeComparable;
            if (null == ec)
                return -1;
            if (FirstName != ec.FirstName)
                return -1;
            if (LastName != ec.LastName)
                return -1;
            if (YearsInMS != ec.YearsInMS)
                return -1;
            return 0;
        }
    }

    public class EmployeeDefinesSideIndicator : EmployeeComparable
    {
        public EmployeeDefinesSideIndicator(
            string firstName, string lastName, int yearsInMS)
            : base(firstName, lastName, yearsInMS)
        {}

        public string SideIndicator
        {
            get { throw new System.ArgumentException("get_SideIndicator"); }
            set { throw new System.ArgumentException("get_SideIndicator"); }
        }
    }
"@
		}
		else
		{
			Add-Type -TypeDefinition @"
    public class EmployeeComparable : Employee, System.IComparable
    {
        public EmployeeComparable(
            string firstName, string lastName, int yearsInMS)
            : base(firstName, lastName, yearsInMS)
        {}

        public int CompareTo(object obj)
        {
            EmployeeComparable ec = obj as EmployeeComparable;
            if (null == ec)
                return -1;
            if (FirstName != ec.FirstName)
                return -1;
            if (LastName != ec.LastName)
                return -1;
            if (YearsInMS != ec.YearsInMS)
                return -1;
            return 0;
        }
    }

    public class EmployeeDefinesSideIndicator : EmployeeComparable
    {
        public EmployeeDefinesSideIndicator(
            string firstName, string lastName, int yearsInMS)
            : base(firstName, lastName, yearsInMS)
        {}

        public string SideIndicator
        {
            get { throw new System.ArgumentException("get_SideIndicator"); }
            set { throw new System.ArgumentException("get_SideIndicator"); }
        }
    }
"@
	}
	It "Compare-Object with 1 referenceObject and 1 differenceObject should work"{
		$empsReference = @([EmployeeComparable]::New("john","smith",5))
		$empsDifference = @([EmployeeComparable]::New("mary","jane",5))
		$boolvalues=@($true,$false)
		foreach($recordEqual in $boolvalues)
		{
			foreach($excludeDifferent in $boolvalues)
			{
				foreach($passthru in $boolvalues)
				{
					$result = Compare-Object -ReferenceObject $empsReference -IncludeEqual:$recordEqual -ExcludeDifferent:$excludeDifferent -PassThru:$passthru -DifferenceObject $empsDifference

					if(!$excludeDifferent)
					{
						$result.Count | Should -Be 2
						if($passthru)
						{
							$result[0] | Should -Be $empsDifference
							$result[1] | Should -Be $empsReference
						}
						else
						{
							$result[0].InputObject | Should -Be $empsDifference
							$result[1].InputObject | Should -Be $empsReference
							$result[0].SideIndicator | Should -BeExactly "=>"
							$result[1].SideIndicator | Should -BeExactly "<="
						}
					}
					else
					{
						$result.Count | Should -Be 0
					}
				}
			}
		}
	}

	It "Compare-Object with 2 referenceObjects and 1 differenceObject should work"{
		$empsReference = @([EmployeeComparable]::New("john","smith",5),[EmployeeComparable]::New("mary","jane",3))
		$empsDifference = @([EmployeeComparable]::New("john","smith",5))
		$boolvalues=@($true,$false)
		foreach($recordEqual in $boolvalues)
		{
			foreach($excludeDifferent in $boolvalues)
			{
				foreach($passthru in $boolvalues)
				{
					$result = Compare-Object -ReferenceObject $empsReference -IncludeEqual:$recordEqual -ExcludeDifferent:$excludeDifferent -PassThru:$passthru -DifferenceObject $empsDifference
					if($recordEqual)
					{
						if(!$excludeDifferent)
						{
							$result.Count | Should -Be 2
							if($passthru)
							{
								$result[0] | Should -Be $empsReference[0]
								$result[1] | Should -Be $empsReference[1]
							}
							else
							{
								$result[0].InputObject | Should -Be $empsReference[0]
								$result[1].InputObject | Should -Be $empsReference[1]
								$result[0].SideIndicator | Should -BeExactly "=="
								$result[1].SideIndicator | Should -BeExactly "<="
							}
						}
						else
						{
							if($passthru)
							{
								$result | Should -Be $empsReference[0]
							}
							else
							{
								$result.InputObject | Should -Be $empsReference[0]
								$result.SideIndicator | Should -BeExactly "=="
							}
						}
					}
					else
					{
						if(!$excludeDifferent)
						{
							if($passthru)
							{
								$result | Should -Be $empsReference[1]
							}
							else
							{
								$result.InputObject | Should -Be $empsReference[1]
								$result.SideIndicator | Should -BeExactly "<="
							}
						}
						else
						{
							$result.Count | Should -Be 0
						}
					}
				}
			}
		}
	}

	It "Compare-Object with 0 SyncWindow should work"{
		$empsReference = @([EmployeeComparable]::New("john","smith",5))
		$empsDifference = @([EmployeeComparable]::New("john","smith",5),[EmployeeComparable]::New("mary","jane",3))
		$boolvalues=@($true,$false)
		foreach($recordEqual in $boolvalues)
		{
			foreach($excludeDifferent in $boolvalues)
			{
				foreach($passthru in $boolvalues)
				{
					$result = Compare-Object -ReferenceObject $empsReference -IncludeEqual:$recordEqual -ExcludeDifferent:$excludeDifferent -PassThru:$passthru -DifferenceObject $empsDifference -SyncWindow:0
					if($recordEqual)
					{
						if(!$excludeDifferent)
						{
							$result.Count | Should -Be 2
							if($passthru)
							{
								$result[0] | Should -Be $empsReference
								$result[1] | Should -Be $empsDifference[1]
							}
							else
							{
								$result[0].InputObject | Should -Be $empsReference
								$result[1].InputObject | Should -Be $empsDifference[1]
								$result[0].SideIndicator | Should -BeExactly "=="
								$result[1].SideIndicator | Should -BeExactly "=>"
							}
						}
						else
						{
							if($passthru)
							{
								$result | Should -Be $empsReference
							}
							else
							{
								$result.InputObject | Should -Be $empsReference
								$result.SideIndicator | Should -BeExactly "=="
							}
						}
					}
					else
					{
						if(!$excludeDifferent)
						{
							if($passthru)
							{
								$result | Should -Be $empsDifference[1]
							}
							else
							{
								$result.InputObject | Should -Be $empsDifference[1]
								$result.SideIndicator | Should -BeExactly "=>"
							}
						}
						else
						{
							$result.Count | Should -Be 0
						}
					}
				}
			}
		}
	}

	It "Compare-Object with SyncWindow should work"{
		$empsReference = @([EmployeeComparable]::New("mary","jane",3),[EmployeeComparable]::New("john","smith",5),[EmployeeComparable]::New("jack", "black", 15),[EmployeeComparable]::New("jim", "bob", 1))
		$empsDifference = @([EmployeeComparable]::New("jack", "black", 15),[EmployeeComparable]::New("jim", "bob", 1),[EmployeeComparable]::New("mary","jane",3),[EmployeeComparable]::New("john","smith",5))
		$boolvalues=@($true,$false)
		foreach($recordEqual in $boolvalues)
		{
			foreach($excludeDifferent in $boolvalues)
			{
				foreach($passthru in $boolvalues)
				{
					$result = Compare-Object -ReferenceObject $empsReference -IncludeEqual:$recordEqual -ExcludeDifferent:$excludeDifferent -PassThru:$passthru -DifferenceObject $empsDifference -SyncWindow:2

					if($recordEqual)
					{
						$result.Count | Should -Be 4
						if($passthru)
						{
							$result[0] | Should -Be $empsReference[0]
							$result[1] | Should -Be $empsReference[2]
							$result[2] | Should -Be $empsReference[1]
							$result[3] | Should -Be $empsReference[3]
						}
						else
						{
							$result[0].InputObject | Should -Be $empsReference[0]
							$result[0].SideIndicator | Should -BeExactly "=="
							$result[1].InputObject | Should -Be $empsReference[2]
							$result[1].SideIndicator | Should -BeExactly "=="
							$result[2].InputObject | Should -Be $empsReference[1]
							$result[2].SideIndicator | Should -BeExactly "=="
							$result[3].InputObject | Should -Be $empsReference[3]
							$result[3].SideIndicator | Should -BeExactly "=="
						}
					}
					else
					{
						$result.Count | Should -Be 0
					}
				}
			}
		}
	}

	It "Compare-Object with Script Block Property Parameter should work"{
		$a = [version]"1.2.3.4"
		$b = [version]"5.6.7.8"
		$result = Compare-Object $a $b -IncludeEqual -Property {$_.Major},{$_.Minor}
		$result[0] | Select-Object -ExpandProperty "*Major" | Should -Be 5
		$result[0] | Select-Object -ExpandProperty "*Minor" | Should -Be 6
		$result[0].SideIndicator | Should -BeExactly "=>"
		$result[1] | Select-Object -ExpandProperty "*Major" | Should -Be 1
		$result[1] | Select-Object -ExpandProperty "*Minor" | Should -Be 2
		$result[1].SideIndicator | Should -BeExactly "<="
	}

	It "Compare-Object with no ReferenceObject nor DifferenceObject: output nothing, no error and should work"{
		$result = Compare-Object @() @()
		$result | Should -BeNullOrEmpty
	}

	It "Compare-Object with no DifferenceObject should work"{
		$result = Compare-Object @() @("diffObject")
		$result.InputObject | Should -BeExactly "diffObject"
		$result.SideIndicator | Should -BeExactly "=>"
	}

	It "Compare-Object with no ReferenceObject should work"{
		$result = Compare-Object @("refObject") @()
		$result.InputObject | Should -BeExactly "refObject"
		$result.SideIndicator | Should -BeExactly "<="
	}
}
