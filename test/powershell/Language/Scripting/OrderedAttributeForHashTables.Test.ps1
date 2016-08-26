Describe 'Test for cmdlet to support Ordered Attribute on hash literal nodes' -Tags "CI" {

    It 'New-Object - Property Parameter Must take IDictionary' {
	   $a = new-object psobject -property ([ordered]@{one=1;two=2})

       $a | Should Not Be $null
       $a.one | Should Be 1
    }
       

    Context 'Select-Xml cmdlet - Namespace parameter must take IDictionary' {
       
       $a = $null
       
       try
       {
            $helpXml = @'
<?xml version="1.0" encoding="utf-8" ?>

<helpItems schema="maml">

<command:command xmlns:maml="http://schemas.microsoft.com/maml/2004/10" xmlns:command="http://schemas.microsoft.com/maml/dev/command/2004/10" xmlns:dev="http://schemas.microsoft.com/maml/dev/2004/10">
    <command:details>
        <command:name>
            Stop-Transcript
        </command:name>
    </command:details>
</command:command>


</helpItems>
'@

            iex ('$a = select-xml -content $helpXml  `
                -namespace ([ordered]@{command="http://schemas.microsoft.com/maml/dev/command/2004/10"; `
                                       maml="http://schemas.microsoft.com/maml/2004/10"; `
                                       dev="http://schemas.microsoft.com/maml/dev/2004/10"})  `
                -xpath "//command:name"')
       }
       catch
       {
           It 'should not throw exception' { $false | Should be $true }
       }
       It '$a should not be $null' { $a | Should Not Be $null }
   }
       
       
    <#Context 'Set-WmiInstance cmdlet - Argument parameter must take IDictionary' { 
       
           $a = $null       
           try
           {
	         #iex ('$a = set-wmiinstance -class win32_environment `
              #  -argument ([ordered]@{Name="TestWmiInstance234452425";VariableValue="testvalu234e";UserName="<SYSTEM>"})')       
           }
           catch
           {
               It 'should not throw exception' { $false | Should be $true }
           }
           #Assert($a -ne $null -and $a.Name -eq 'TestWmiInstance234452425' ) "Set-WmiInstance cmdlet does not accept IDictionary for Argument parameter"
    }#>
       
    Context 'Select-Object cmdlet - Property parameter (Calculated properties) must take IDictionary' {

           $a = $null       
           try
           {
	         iex ('$a = dir | select-object -property Name, `
                        ([ordered]@{Name="IsDirectory";Expression ={$_.PSIsContainer}})')       
           }
           catch
           {
               It 'should not throw exception' { $false | Should be $true }
           }
           It '$a should not be $null'  { $a | Should Not Be $null }
    }
}
