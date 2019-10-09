# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe 'Test for cmdlet to support Ordered Attribute on hash literal nodes' -Tags "CI" {
    BeforeAll {
        If (-not $IsCoreCLR) {
            Get-WmiObject -Query "select * from win32_environment where name='TestWmiInstance'"  | Remove-WmiObject
        }
    }
    AfterAll {
        If (-not $IsCoreCLR) {
            Get-WmiObject -Query "select * from win32_environment where name='TestWmiInstance'"  | Remove-WmiObject
        }
    }

    It 'New-Object - Property Parameter Must take IDictionary' {
        $a = new-object psobject -property ([ordered]@{one=1;two=2})
        $a | Should -Not -BeNullOrEmpty
        $a.one | Should -Be 1
    }

    Context 'Select-Xml cmdlet - Namespace parameter must take IDictionary' {
        $script:a = $null

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

        { $script:a = select-xml -content $helpXml -xpath "//command:name" -namespace (
                        [ordered]@{command="http://schemas.microsoft.com/maml/dev/command/2004/10";
                                   maml="http://schemas.microsoft.com/maml/2004/10";
                                   dev="http://schemas.microsoft.com/maml/dev/2004/10"})  } | Should -Not -Throw

        It '$a should not be $null' { $script:a | Should -Not -BeNullOrEmpty }
   }

    It 'Set-WmiInstance cmdlet - Argument parameter must take IDictionary' -skip:$IsCoreCLR {

        $script:a = $null

        { $script:a = set-wmiinstance -class win32_environment -argument ([ordered]@{Name="TestWmiInstance";
                        VariableValue="testvalu234e";
                        UserName="<SYSTEM>"}) } | Should -Not -Throw
        $script:a | Should -Not -BeNullOrEmpty
        $script:a.Name | Should -BeExactly "TestWmiInstance"
    }

    Context 'Select-Object cmdlet - Property parameter (Calculated properties) must take IDictionary' {

        $script:a = $null

        {$script:a = Get-ChildItem | select-object -property Name, (
                    [ordered]@{Name="IsDirectory";
                               Expression ={$_.PSIsContainer}})} | Should -Not -Throw

        It '$a should not be $null'  { $script:a | Should -Not -BeNullOrEmpty }
    }
}
