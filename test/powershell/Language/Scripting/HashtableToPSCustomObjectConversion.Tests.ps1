# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Tests for hashtable to PSCustomObject conversion" -Tags "CI" {
    BeforeAll {
        class SampleClass5 {
            [int]$a
            [int]$b
            SampleClass5([int]$x) { $this.a = $x }
            SampleClass5([hashtable]$h) { $this.a = 100; $this.b = 100 }
        }
    }

   $testdata = @(
        @{ Name = 'New-Object cmdlet should accept empty hashtable or $null as Property argument';
           Cmd = "new-object psobject -property `$null";
           ExpectedType = 'System.Management.automation.psobject'
        },
        @{ Name = 'Hashtable conversion to PSCustomObject succeeds (Insertion Order is not retained)';
           Cmd = "[pscustomobject][hashtable]`@{one=1;two=2}";
           ExpectedType = 'System.Management.automation.psobject'
        },
        @{ Name = 'Hashtable(Stored in a variable) conversion to PSCustomObject succeeds (Insertion Order is not retained)';
           Cmd = "`$ht = @{one=1;two=2};[pscustomobject]`$ht";
           ExpectedType = 'System.Management.automation.psobject'
        },
        @{ Name = 'New-Object cmdlet should accept `$null as Property argument for pscustomobject';
           Cmd = "new-object pscustomobject -property `$null";
           ExpectedType = 'System.Management.automation.psobject'
        },
	   @{ Name = 'New-Object cmdlet should accept empty hashtable as property argument for pscustomobject';
           Cmd = "`$ht = @{};new-object pscustomobject -property `$ht";
           ExpectedType = 'System.Management.automation.psobject'
        }
    )

    It 'Type Validation: <Name>' -TestCases:$testdata {
        param ($Name, $Cmd, $ExpectedType)
        Invoke-Expression $Cmd -OutVariable a
        $a = Get-Variable -Name a -ValueOnly
        $a | Should -BeOfType $ExpectedType
    }

    It 'Hashtable conversion to PSCustomObject retains insertion order of hashtable keys when passed a hashliteral' {

        $x = [pscustomobject]@{one=1;two=2}
        $x | Should -BeOfType System.Management.automation.psobject

        $p = 0
        # Checks if the first property is One
        $x.psobject.Properties | ForEach-Object  `
                                {
                                    if ($p -eq 0)
                                    {
                                        $p++;
                                        $_.Name | Should -BeExactly 'one'
                                     }
                                }
    }

    It 'Conversion of Ordered hashtable to PSCustomObject should succeed' {

       $x = [pscustomobject][ordered]@{one=1;two=2}
       $x | Should -BeOfType System.Management.automation.psobject

       $p = 0
       # Checks if the first property is One
       $x.psobject.Properties | ForEach-Object  `
                                {
                                    if ($p -eq 0)
                                    {
                                        $p++;
                                        $_.Name | Should -BeExactly 'one'
                                     }
                                }
    }

    $testdata1 = @(
            @{ Name = 'Creating an object of an existing type from hashtable should throw error when setting non-existent properties';
               Cmd = "[System.MAnagement.Automation.Host.Coordinates]`@{blah=10;Y=5 }";
               ErrorID = 'ObjectCreationError';
               InnerException = $true
            },
            @{ Name = 'Creating an object of an existing type from hashtable should throw error when setting incompatible values for properties';
               Cmd = "[System.MAnagement.Automation.Host.Coordinates]`@{X='foo';Y=5}";
               ErrorID = 'ObjectCreationError';
               InnerException = $true
            },
            @{ Name = 'Conversion from PSCustomObject to hashtable should fail';
               Cmd = "[hashtable][pscustomobject]`@{one=1;two=2}";
               ErrorID ='InvalidCastConstructorException';
               InnerException = $true
            },
            @{
               Name = 'New-Object cmdlet should throw terminating errors when user specifies a non-existent property or tries to assign incompatible values';
               Cmd = "New-Object -TypeName System.MAnagement.Automation.Host.Coordinates -Property `@{xx=10;y=5}";
               ErrorID ='InvalidOperationException,Microsoft.PowerShell.Commands.NewObjectCommand'
            }
        )

    It '<Name>' -TestCases:$testData1 {
        param ($Name, $Cmd, $ErrorID, $InnerException)
        $e = { Invoke-Expression $Cmd } | Should -Throw -PassThru

        if($InnerException)
        {
            $e.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -BeExactly $ErrorID
        } else {
            $e.FullyQualifiedErrorId | Should -BeExactly $ErrorID
        }
    }

    It  'Creating an object of an existing type from hashtable should succeed' {
        $result = [System.Management.Automation.Host.Coordinates]@{X=10;Y=33}
        $result.X | Should -Be 10
    }

    It 'Creating an object of an existing type from hashtable should call the constructor taking a hashtable if such a constructor exists in the type' {

       $x = [SampleClass5]@{a=10;b=5}
       $x.a | Should -BeExactly '100'
    }

    It 'Add a new type name to PSTypeNames property' {

	    $obj = [PSCustomObject] @{pstypename = 'Mytype'}
	    $obj.PSTypeNames[0] | Should -BeExactly 'Mytype'
    }

    It 'Add an existing type name to PSTypeNames property' {

	    $obj = [PSCustomObject] @{pstypename = 'System.Object'}
	    $obj.PSTypeNames.Count | Should -Be 3
	    $obj.PSTypeNames[0] | Should -BeExactly 'System.Object'
    }
    It "new-object should fail to create object for System.Management.Automation.PSCustomObject" {
        $obj = $null
        $ht = @{one=1;two=2}

        { $obj = New-Object System.Management.Automation.PSCustomObject -Property $ht } |
            Should -Throw -ErrorId "CannotFindAppropriateCtor,Microsoft.PowerShell.Commands.NewObjectCommand"
        $obj | Should -BeNullOrEmpty
    }
}

Describe "Error message with settable Property information" -Tag 'CI' {
    BeforeAll {
        Add-Type @"
namespace HashtableConversionTest {
    public class AType {
        public string Name;
        public string Path { get; set; }
        public string Id { get; }
    }
}
"@
    }

    It "Only settable properties are called out in the error message" {
        try {
            [HashtableConversionTest.AType]@{ key = 1 }
        } catch {
            $e = $_
        }

        $e.FullyQualifiedErrorId | Should -BeExactly "ObjectCreationError"
        $e.Exception.Message.Contains("key") | Should -BeTrue
        $e.Exception.Message.Contains("Name") | Should -BeTrue
        $e.Exception.Message.Contains("Path") | Should -BeTrue
        $e.Exception.Message.Contains("Id") | Should -BeFalse
    }

    It "Shows no property when there is no settable property" {
        try {
            [System.Collections.Specialized.OrderedDictionary]@{ key = 1 }
        } catch {
            $e = $_
        }

        $type = [psobject].Assembly.GetType("ExtendedTypeSystem")
        $property = $type.GetProperty("NoSettableProperty", @("NonPublic", "Static"))
        $resString = $property.GetValue($null) -f 'key', 'System.Collections.Specialized.OrderedDictionary'

        $e.Exception.Message | Should -BeLike "*$resString"
    }
}
