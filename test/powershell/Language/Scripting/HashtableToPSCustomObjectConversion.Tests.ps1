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
        @{ Name = 'Hashtable(Stored in a variable) conversion to  PSCustomObject succeeds (Insertion Order is not retained)';
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
        Invoke-expression $Cmd -OutVariable a
        $a = Get-Variable -Name a -ValueOnly        
        $a | should BeOfType $ExpectedType
    }       

    It 'Hashtable conversion to PSCustomObject retains insertion order of hashtable keys when passed a hashliteral' {
           
        $x = [pscustomobject]@{one=1;two=2}        
        $x | should BeOfType "System.Management.automation.psobject"
       
        $p = 0
        # Checks if the first property is One
        $x.psobject.Properties | foreach-object  `
                                {               
                                    if ($p -eq 0)  
                                    {               
                                        $p++;
                                        $_.Name | Should Be 'one'
                                     }
                                }
    } 
       
    It 'Conversion of Ordered hashtable to PSCustomObject should succeed' {
      
       $x = [pscustomobject][ordered]@{one=1;two=2}
       $x | should BeOfType "System.Management.automation.psobject"
       
       $p = 0
       # Checks if the first property is One
       $x.psobject.Properties | foreach-object  `
                                {               
                                    if ($p -eq 0)  
                                    {               
                                        $p++; 
                                        $_.Name | Should Be 'one'
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
        try
        {           
            Invoke-Expression $Cmd
            Throw "Exception expected, execution should not have reached here"
        } catch {
            
           if($InnerException)
           {
                $_.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should Be $ErrorID
           }
           else {
                $_.FullyQualifiedErrorId | Should Be $ErrorID
           }
       }       
    }    
       

    It  'Creating an object of an existing type from hashtable should succeed' {
        $result = [System.Management.Automation.Host.Coordinates]@{X=10;Y=33}
        $result.X | should be 10
    }
    
    It 'Creating an object of an existing type from hashtable should call the constructor taking a hashtable if such a constructor exists in the type' {
        
       $x = [SampleClass5]@{a=10;b=5}
       $x.a | Should Be '100'
    }

    It 'Add a new type name to PSTypeNames property' {

	    $obj = [PSCustomObject] @{pstypename = 'Mytype'}
	    $obj.PSTypeNames[0] | Should Be 'Mytype'
    }

    It 'Add an existing type name to PSTypeNames property' {

	    $obj = [PSCustomObject] @{pstypename = 'System.Object'}
	    $obj.PSTypeNames.Count | Should Be 3
	    $obj.PSTypeNames[0] | Should Be 'System.Object'
    }
    It "new-object should fail to create object for System.Management.Automation.PSCustomObject" {

        $errorObj = $null
        $obj = $null
		$ht = @{one=1;two=2}
        try
        {
            $obj = New-Object System.Management.Automation.PSCustomObject -property $ht
        }
        catch
        {
            $errorObj = $_
        }
        $obj | should be $null
        $errorObj.FullyQualifiedErrorId | should be "CannotFindAppropriateCtor,Microsoft.PowerShell.Commands.NewObjectCommand"
    }
}

