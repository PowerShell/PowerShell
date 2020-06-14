# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe 'Test for cmdlet to support Ordered Attribute on hash literal nodes' -Tags "CI" {
    It 'New-Object - Property Parameter Must take IDictionary' {
        $a = New-Object psobject -Property ([ordered]@{one=1;two=2})
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

        { $script:a = Select-Xml -Content $helpXml -XPath "//command:name" -Namespace (
                        [ordered]@{command="http://schemas.microsoft.com/maml/dev/command/2004/10";
                                   maml="http://schemas.microsoft.com/maml/2004/10";
                                   dev="http://schemas.microsoft.com/maml/dev/2004/10"})  } | Should -Not -Throw

        It '$a should not be $null' { $script:a | Should -Not -BeNullOrEmpty }
   }

    Context 'New-CimInstance cmdlet' {
        BeforeAll {
            If ($IsWindows) {
                Get-CimInstance -ClassName Win32_Environment -Filter "name='TestCimInstance'" | Remove-CimInstance
            }
        }
        AfterAll {
            If ($IsWindows) {
                Get-CimInstance -ClassName Win32_Environment -Filter "name='TestCimInstance'" | Remove-CimInstance
            }
        }

        It 'Property parameter must take IDictionary' -Skip:(-not $IsWindows) {

            $script:a = $null

            { $script:a = New-CimInstance -ClassName Win32_Environment -Property ([ordered]@{
                Name="TestCimInstance";
                VariableValue="testvalu234e";
                UserName=[System.Environment]::UserName
            }) -ClientOnly } | Should -Not -Throw
            $script:a | Should -Not -BeNullOrEmpty
            $script:a.Name | Should -BeExactly "TestCimInstance"
        }
    }

    Context 'Select-Object cmdlet - Property parameter (Calculated properties) must take IDictionary' {

        $script:a = $null

        {$script:a = Get-ChildItem | Select-Object -Property Name, (
                    [ordered]@{Name="IsDirectory";
                               Expression ={$_.PSIsContainer}})} | Should -Not -Throw

        It '$a should not be $null'  { $script:a | Should -Not -BeNullOrEmpty }
    }
}
