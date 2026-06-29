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

        # Verify example titles are preserved exactly through the round-trip
        for ($i = 0; $i -lt $oldExamples.Length; $i++) {
            $newExamples[$i].title | Should -Be $oldExamples[$i].title -Because "example $($i+1) title should be preserved exactly"
        }
    }

    It "ProxyCommand.GetHelpComments preserves custom example titles" {
        function TitledExampleFunc {
            <#
                .SYNOPSIS
                A function with titled examples.

                .EXAMPLE Retrieving an item
                Get-Item -Path C:\Temp

                Retrieves the item at C:\Temp.

                .EXAMPLE Listing children
                Get-ChildItem -Path C:\Temp

                Lists folder contents.

                .EXAMPLE
                Get-Process

                An untitled example.
            #>
            param()
        }

        $helpObj = Get-Help TitledExampleFunc -Full
        $helpContent = [System.Management.Automation.ProxyCommand]::GetHelpComments($helpObj)
        $bodySB = [scriptblock]::Create(@"
<#
$helpContent
#>
param()
"@)
        Set-Item -Path function:\TitledExampleProxy -Value $bodySB
        $newHelpObj = Get-Help TitledExampleProxy -Full

        $oldExamples = @($helpObj.examples.example)
        $newExamples = @($newHelpObj.examples.example)
        $newExamples.Length | Should -Be $oldExamples.Length

        # Titled examples must preserve the custom title portion
        $newExamples[0].title | Should -BeLike '*Retrieving an item*'
        $newExamples[1].title | Should -BeLike '*Listing children*'
        # Untitled example must not gain a spurious colon
        $newExamples[2].title | Should -Not -BeLike '*:*'
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

    Context 'GetHelpComments preserves example titles' {
        BeforeAll {
            function HelpFuncForProxyTitles {
                <#
                  .SYNOPSIS
                  A function with titled examples for proxy testing.

                  .EXAMPLE Retrieving processes
                  Get-Process

                  Gets all processes

                  .EXAMPLE
                  Get-Service

                  Gets all services

                  .EXAMPLE Listing items in a directory
                  Get-ChildItem -Path C:\

                  Lists items in C:\
                #>
                param()
            }

            $script:helpComments = [System.Management.Automation.ProxyCommand]::GetHelpComments((Get-Help HelpFuncForProxyTitles))
        }

        It 'should emit titled .EXAMPLE for example with title' {
            $script:helpComments | Should -BeLike '*.EXAMPLE Retrieving processes*'
        }

        It 'should emit plain .EXAMPLE for untitled example' {
            # Get-Help injects a 'PS > ' prompt prefix in front of the code line, so the
            # regex must allow for that between the .EXAMPLE keyword and Get-Service.
            $script:helpComments | Should -Match '(?m)^\.EXAMPLE\s*$[\s\S]*?Get-Service'
        }

        It 'should emit titled .EXAMPLE for third titled example' {
            $script:helpComments | Should -BeLike '*.EXAMPLE Listing items in a directory*'
        }

        It 'round-trips titled examples through proxy comment generation' {
            # Define the function in the test's scope so Get-Help can find it.
            # Using '& $sb' would scope-isolate the function inside the scriptblock.
            $funcBody = "param()`n<#`n$($script:helpComments)`n#>"
            Set-Item -Path function:\ProxyRoundTripFunc -Value ([scriptblock]::Create($funcBody))
            $roundTrippedHelp = Get-Help ProxyRoundTripFunc
            $roundTrippedHelp.examples.example.Count | Should -Be 3
            $roundTrippedHelp.examples.example[0].title | Should -BeLike '*EXAMPLE 1: Retrieving processes*'
            $roundTrippedHelp.examples.example[1].title | Should -Not -BeLike '*:*' -Because 'untitled example should not have a colon'
            $roundTrippedHelp.examples.example[2].title | Should -BeLike '*EXAMPLE 3: Listing items in a directory*'
        }
    }

    Context 'GetHelpComments handles edge cases in example titles' {
        It 'should handle title containing a colon' {
            # NOTE: a second example is included to work around a pre-existing
            # behavior where Get-Help returns a single example as a PSObject
            # rather than PSObject[], which causes ProxyCommand.GetHelpComments
            # to skip the example loop entirely.
            function HelpFuncColonTitle {
                <#
                  .SYNOPSIS
                  Test colon in title.

                  .EXAMPLE Step 1: Initialize the module
                  Import-Module MyModule

                  Imports the module

                  .EXAMPLE
                  Get-Module

                  Lists modules
                #>
                param()
            }

            $helpComments = [System.Management.Automation.ProxyCommand]::GetHelpComments((Get-Help HelpFuncColonTitle))
            $helpComments | Should -BeLike '*.EXAMPLE Step 1: Initialize the module*'
        }

        It 'should handle title containing dashes' {
            # See note in 'should handle title containing a colon' about the
            # second example.
            function HelpFuncDashTitle {
                <#
                  .SYNOPSIS
                  Test dashes in title.

                  .EXAMPLE Using a non-standard path
                  Get-Item -Path C:\my-folder

                  Gets the item

                  .EXAMPLE
                  Get-Item -Path C:\

                  Gets the root
                #>
                param()
            }

            $helpComments = [System.Management.Automation.ProxyCommand]::GetHelpComments((Get-Help HelpFuncDashTitle))
            $helpComments | Should -BeLike '*.EXAMPLE Using a non-standard path*'
        }

        It 'should handle a titled example alongside an untitled one' {
            # See note in 'should handle title containing a colon' about why two
            # examples are required for ProxyCommand.GetHelpComments to emit any
            # example at all.
            function HelpFuncTitledPair {
                <#
                  .SYNOPSIS
                  Titled and untitled example.

                  .EXAMPLE The only titled example
                  Get-Date

                  Gets the current date

                  .EXAMPLE
                  Get-TimeZone

                  Gets the time zone
                #>
                param()
            }

            $helpComments = [System.Management.Automation.ProxyCommand]::GetHelpComments((Get-Help HelpFuncTitledPair))
            $helpComments | Should -BeLike '*.EXAMPLE The only titled example*'
            # Verify it round-trips. Use Set-Item so the function lives in the
            # current scope (a script block invoked with '&' would isolate it).
            $funcBody = "param()`n<#`n$helpComments`n#>"
            Set-Item -Path function:\TitledPairRoundTrip -Value ([scriptblock]::Create($funcBody))
            $rt = Get-Help TitledPairRoundTrip
            $rt.examples.example[0].title | Should -BeLike '*EXAMPLE 1: The only titled example*'
        }
    }
}
