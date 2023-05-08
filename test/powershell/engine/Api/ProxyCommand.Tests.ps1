# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
using namespace System.Management.Automation
using namespace System.Collections.ObjectModel

Describe 'ProxyCommand Tests' -Tags "CI" {
    BeforeAll {
        function NormalizeCRLF {
            param ($helpObj)

            if($helpObj.Synopsis.Contains("`r`n"))
            {
                $helpObjText = ($helpObj.Synopsis).replace("`r`n", [System.Environment]::NewLine).trim()
            }
            else
            {
                $helpObjText = ($helpObj.Synopsis).replace("`n", [System.Environment]::NewLine).trim()
            }

            return $helpObjText
        }

        function GetSectionText {
            param ($object)
            $texts = $object | Out-String -Stream | ForEach-Object {
                if (![string]::IsNullOrWhiteSpace($_)) { $_.Trim() }
            }
            $texts -join ""
        }

        function ProxyTest {
            [CmdletBinding(DefaultParameterSetName='Name')]
            param (
                [Parameter(ParameterSetName="Name", Mandatory=$true)]
                [ValidateSet("Orange", "Apple")]
                [string] $Name,

                [Parameter(ParameterSetName="Id", Mandatory=$true)]
                [ValidateRange(1,5)]
                [int] $Id,

                [Parameter(ValueFromPipeline)]
                [string] $Message
            )

            DynamicParam {
                $ParamAttrib  = [parameter]::new()
                $ParamAttrib.Mandatory  = $true

                $AttribColl = [Collection[System.Attribute]]::new()
                $AttribColl.Add($ParamAttrib)

                $RuntimeParam  = [RuntimeDefinedParameter]::new('LastName', [string], $AttribColl)
                $RuntimeParamDic  = [RuntimeDefinedParameterDictionary]::new()
                $RuntimeParamDic.Add('LastName',  $RuntimeParam)
                return  $RuntimeParamDic
            }

            Begin {
                $AllMessages = @()
                if ($PSCmdlet.ParameterSetName -eq "Name") {
                    $MyString = $Name, $PSBoundParameters['LastName'] -join ","
                } else {
                    $MyString = $Id, $PSBoundParameters['LastName'] -join ","
                }
            }

            Process {
                if ($Message) {
                    $AllMessages += $Message
                }
            }

            End {
                $MyString + " - " + ($AllMessages -join ";")
            }
        }
    }

    It "Test ProxyCommand.GetHelpComments" {
        $helpObj = Get-Help Get-Alias -Full
        $helpContent = [System.Management.Automation.ProxyCommand]::GetHelpComments($helpObj)

        $funcBody = @"
    <#
    {0}
    #>

    param({1})
"@
        $params = $helpObj.parameters.parameter
        $paramString = '${0}' -f $params[0].name
        for ($i = 1; $i -lt $params.Length; $i++) {
            $paramString += ',${0}' -f $params[$i].name
        }

        $bodyToUse = $funcBody -f $helpContent, $paramString
        $bodySB = [scriptblock]::Create($bodyToUse)
        Set-Item -Path function:\TestHelpComment -Value $bodySB
        $newHelpObj = Get-Help TestHelpComment -Full

        $helpObjText = NormalizeCRLF -helpObj $helpObj
        $newHelpObjText = NormalizeCRLF -helpObj $newHelpObj

        $helpObjText | Should -Be $newHelpObjText
        $oldDespText = GetSectionText $helpObj.description
        $newDespText = GetSectionText $newHelpObj.description
        $oldDespText | Should -Be $newDespText

        $oldParameters = @($helpObj.parameters.parameter)
        $newParameters = @($newHelpObj.parameters.parameter)
        $oldParameters.Length | Should -Be $newParameters.Length
        $oldParameters.name -join "," | Should -Be ($newParameters.name -join ",")

        $oldExamples = @($helpObj.examples.example)
        $newExamples = @($newHelpObj.examples.example)
        $oldExamples.Length | Should -Be $newExamples.Length
    }

    It "Test generate proxy command" {
        $cmdInfo = Get-Command -Name Get-Content
        $cmdMetadata = [CommandMetadata]::new($cmdInfo)
        $proxyBody = [ProxyCommand]::Create($cmdMetadata, "--DummyHelpContent--")
        $proxyBody | Should -Match '--DummyHelpContent--'

        $proxyBodySB = [scriptblock]::Create($proxyBody)
        Set-Item -Path function:\MyGetContent -Value $proxyBodySB

        $expectedContent = "Hello World"
        Set-Content -Path $TestDrive\content.txt -Value $expectedContent -Encoding Unicode
        $myContent = MyGetContent -Path $TestDrive\content.txt -Encoding Unicode
        $myContent | Should -Be $expectedContent
    }

    It "Test generate individual components" {
        $cmdInfo = Get-Command -Name ProxyTest
        $cmdMetadata = [CommandMetadata]::new($cmdInfo)
        $template = @"
{0}
param(
{1}
)

DynamicParam {{
{2}
}}

Begin {{
{3}
}}

Process {{
{4}
}}

End {{
{5}
}}
"@

        $cmdletBindig = [ProxyCommand]::GetCmdletBindingAttribute($cmdMetadata)
        $params = [ProxyCommand]::GetParamBlock($cmdMetadata)
        $dynamicParams = [ProxyCommand]::GetDynamicParam($cmdMetadata)
        $begin = [ProxyCommand]::GetBegin($cmdMetadata)
        $process = [ProxyCommand]::GetProcess($cmdMetadata)
        $end = [ProxyCommand]::GetEnd($cmdMetadata)

        $funcBody = $template -f $cmdletBindig, $params, $dynamicParams, $begin, $process, $end
        $bodySB = [scriptblock]::Create($funcBody)
        Set-Item -Path function:\MyProxyTest -Value $bodySB

        $cmdMyProxyTest = Get-Command MyProxyTest
        $dyParam = $cmdMyProxyTest.Parameters.GetEnumerator() | Where-Object { $_.Value.IsDynamic }
        $dyParam.Key | Should -Be 'LastName'

        $result = "Msg1", "Msg2" | MyProxyTest -Name Apple -LastName Last
        $result | Should -Be "Apple,Last - Msg1;Msg2"

        $result = "Msg1", "Msg2" | MyProxyTest -Id 3 -LastName Last
        $result | Should -Be "3,Last - Msg1;Msg2"
    }
}
