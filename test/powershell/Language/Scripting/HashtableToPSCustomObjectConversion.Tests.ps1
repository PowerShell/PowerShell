Describe "Tests for hashtable to PSCustomObject conversion" -Tags "CI" {

    Context 'New-Object cmdlet should accept empty hashtable or $null as Property argument' {
            
        { $script:a = new-object psobject -property $null } | Should not throw
        
        It '$a should not be $null' { $script:a | Should Not Be $null }
        It '$a type' { $script:a | should BeOfType "System.Management.automation.psobject" }
    }

    It 'New-Object cmdlet should throw terminating errors when user specifies a non-existent property or tries to assign incompatible values' {
       
        try
        { 
            $source = @"
                        public class SampleClass6
                        {
                            public SampleClass6(int x)
                            {
                            	a = x;
                            }
                            
                            public SampleClass6()
                            {
                            }
                                                          
                            public int a;
                            public int b;
                        }
"@
            add-type -typedefinition $source
            New-Object -TypeName SampleClass6 -Property @{aa=10;b=5}
            Throw "Exception expected, execution should not have reached here"            
        }
        catch {
            $_.Exception | Should BeOfType "InvalidOperationException"
            $_.Exception.Message | Should Match "aa"
            $_.FullyQualifiedErrorId | should be "InvalidOperationException,Microsoft.PowerShell.Commands.NewObjectCommand"           
        }
    }

    Context 'Hashtable conversion to PSCustomObject succeeds (Insertion Order is not retained)' {
        { $script:x = [pscustomobject][hashtable]@{one=1;two=2}} | Should Not  Throw
        It '$x is not $null' { $script:x | Should Not Be $null }
        It '$x type' { $script:x | should BeOfType "System.Management.automation.psobject" }
   }
       

    Context 'Hashtable conversion to PSCustomObject retains insertion order of hashtable keys when passed a hashliteral' {
       
        { $script:x = [pscustomobject]@{one=1;two=2} } | Should Not  Throw
       
        It '$x is not $null' { $script:x | Should Not Be $null } 
        It '$x type' { $script:x | should BeOfType "System.Management.automation.psobject" }
       
        $p = 0
        # Checks if the first property is One
        $script:x.psobject.Properties | foreach-object  `
                                {               
                                    if ($p -eq 0)  
                                    {               
                                        $p++;
                                        It '$_.Name' { $_.Name | Should Be 'one' }
                                     }
                                }
    }
       

    Context 'Hashtable(Stored in a variable) conversion to  PSCustomObject succeeds (Insertion Order is not retained)' {
        
        {
	           $ht = @{one=1;two=2}
               $script:x = [pscustomobject]$ht
        } | Should not throw
       
        It '$x is not $null' { $script:x | Should Not Be $null }
        It '$x type' { $script:x | should BeOfType "System.Management.automation.psobject" }
   }


    It 'Conversion from PSCustomObject to hashtable should fail' {
           
           try
           {
	           $x = [hashtable][pscustomobject]@{one=1;two=2}
               Throw "Exception expected, execution should not have reached here"
           }
           catch
           {
                $_.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should Be 'InvalidCastConstructorException'
           }
       }
       
       
    Context 'Conversion of Ordered hashtable to PSCustomObject should succeed' {
      
       { $script:x = [pscustomobject][ordered]@{one=1;two=2} } | Should Not Throw
       It '$x is not $null' { $script:x | Should Not Be $null }
       It '$x type' { $script:x | should BeOfType "System.Management.automation.psobject" }
       
       $p = 0
       # Checks if the first property is One
       $script:x.psobject.Properties | foreach-object  `
                                {               
                                    if ($p -eq 0)  
                                    {               
                                        $p++; 
                                        It 'Name' { $_.Name | Should Be 'one' }
                                     }
                                }
    }
       

    Context 'Creating an object of an existing type from hashtable should succeed' {       
       {
                $source = @"
                        public class SampleClass1
                        {                                                       
                            public SampleClass1(int x)
                            {
                            	a = x;
                            }
                            
                            public SampleClass1()
                            {
                            }
                                                          
                            public int a;
                            public int b;
                        }
"@
                 add-type -typedefinition $source
                 $script:x = [SampleClass1]@{a=10;b=5}            
       } | Should Not Throw
       It '$x is not $null' { $script:x | Should Not Be $null }
       It '$x.a' { $script:x.a | Should Be '10' }
   }
       

    It 'Creating an object of an existing type from hashtable should throw error when setting non-existent properties' {
      
       try
       { 
          $source = @"
                        public class SampleClass2
                        {
                            public SampleClass2(int x)
                            {
                            	a = x;
                            }
                            
                            public SampleClass2()
                            {
                            }
                                                          
                            public int a;
                            public int b;
                        }
"@
                 add-type -typedefinition $source
                 $x = [SampleClass2]@{blah=10;b=5 }
                 Throw "Exception expected, execution should not have reached here"
       }
       catch
       {
           $_.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should Be 'ObjectCreationError'
       }       
    }


    It 'Creating an object of an existing type from hashtable should throw error when setting incompatible values for properties' {       
       
       try
       { 
          $source = @"
                        public class SampleClass3
                        {
                            public SampleClass3(int x)
                            {
                            	a = x;
                            }
                            
                            public SampleClass3()
                            {
                            }
                                                          
                            public int a;
                            public int b;
                        }
"@
                 add-type -typedefinition $source
                 $x = [SampleClass3]@{a="foo";b=5}
                 Throw "Exception expected, execution should not have reached here"
       }
       catch
       {           
           $_.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should Be 'ObjectCreationError'
       }
    }

    #known issue 2069 
    It 'Creating an object of an existing type from hashtable should call the constructor taking a hashtable if such a constructor exists in the type' -skip:$IsCoreCLR {
       {
                $source = @"
                        public class SampleClass5
                        {                                                       
                            public SampleClass5(int x)
                            {
                            	a = x;
                            }
                            
                            public SampleClass5(System.Collections.Hashtable h)   
                            {
		                          a = 100;
		                          b = 200;
                            }
                            
                            public SampleClass5()
                            {
                            }
                                                          
                            public int a;
                            public int b;
                        }
"@
                 add-type -typedefinition $source
                 $script:x = [SampleClass5]@{a=10;b=5}
            
       } | Should Not Throw
       
       $script:x | Should Not Be $null
       ($script:x.a) | Should Be '100'
    }


    It 'Add a new type name to PSTypeNames property' {

	    $obj = [PSCustomObject] @{pstypename = 'Mytype'}
	    $obj.PSTypeNames[0] | Should Be 'Mytype'
    }

    Context 'Add an existing type name to PSTypeNames property' {

	    $obj = [PSCustomObject] @{pstypename = 'System.Object'}
	    It '$obj.PSTypeNames.Count' { $obj.PSTypeNames.Count | Should Be 3 }
	    It '$obj.PSTypeNames[0] type' { $obj.PSTypeNames[0] | Should Be 'System.Object' }
    }
}