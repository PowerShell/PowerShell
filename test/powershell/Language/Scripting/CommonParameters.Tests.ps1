# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Common parameters support for script cmdlets" -Tags "CI" {
    BeforeEach {
        $rs = [system.management.automation.runspaces.runspacefactory]::CreateRunspace()
        $rs.open()
        $ps = [System.Management.Automation.PowerShell]::Create()
        $ps.Runspace = $rs
    }

    AfterEach {
            $ps.Dispose()
            $rs.Dispose()
    }

    Context "Debug" {
        BeforeAll {
            $script = "
                function get-foo
                {
                    [CmdletBinding()]
                    param()

                    write-output 'output foo'
                    write-debug  'debug foo'
                }"
        }

        It "Debug get-foo" {
            $command = 'get-foo'
            [void] $ps.AddScript($script + $command)
            $asyncResult = $ps.BeginInvoke()
            $output = $ps.EndInvoke($asyncResult)

            $output[0] | Should -BeExactly "output foo"
            $ps.Streams.Debug.Count | Should -Be 0
        }

        It 'get-foo -debug' {
            $command = 'get-foo -debug'
            [void] $ps.AddScript($script + $command)
            $asyncResult = $ps.BeginInvoke()
            $output = $ps.EndInvoke($asyncResult)

            $output[0] | Should -BeExactly "output foo"
            $ps.Streams.Debug[0].Message | Should -BeExactly "debug foo"
            $ps.InvocationStateInfo.State | Should -BeExactly 'Completed'
        }
    }

    Context "verbose" {
        BeforeAll {
            $script = "
                function get-foo
                {
                    [CmdletBinding()]
                    param()

                    write-output 'output foo'
                    write-verbose  'verbose foo'
                }"
        }

        It 'get-foo' {
            $command = 'get-foo'
            [void] $ps.AddScript($script + $command)
            $asyncResult = $ps.BeginInvoke()
            $output = $ps.EndInvoke($asyncResult)

            $output[0] | Should -BeExactly "output foo"
            $ps.streams.verbose.Count | Should -Be 0
        }

        It 'get-foo -verbose' {
            $command = 'get-foo -verbose'

            [void] $ps.AddScript($script + $command)
            $asyncResult = $ps.BeginInvoke()
            $output = $ps.EndInvoke($asyncResult)

            $output[0] | Should -BeExactly "output foo"
            $ps.Streams.verbose[0].Message | Should -BeExactly "verbose foo"
            $ps.InvocationStateInfo.State | Should -BeExactly 'Completed'
        }
    }

    Context "erroraction" {
        BeforeAll {
            $script = "
                function get-foo
                {
                    [CmdletBinding()]
                    param()

                    write-error  'error foo'
                    write-output 'output foo'
                }"
            }

        It 'erroraction' {

            $command = 'get-foo'
            [void] $ps.AddScript($script + $command)
            $asyncResult = $ps.BeginInvoke()
            $output = $ps.EndInvoke($asyncResult)

            $output[0] | Should -BeExactly "output foo"
            $ps.Streams.error[0].ToString() | Should -Match "error foo"
        }

        It 'erroraction continue' {

            $command = 'get-foo -erroraction Continue'
            [void] $ps.AddScript($script + $command)
            $asyncResult = $ps.BeginInvoke()
            $output = $ps.EndInvoke($asyncResult)

            $output[0] | Should -BeExactly "output foo"
            $ps.Streams.error[0].ToString() | Should -Match "error foo"
        }

        It 'erroraction SilentlyContinue' {

            $command = 'get-foo -erroraction SilentlyContinue'
            [void] $ps.AddScript($script + $command)
            $asyncResult = $ps.BeginInvoke()
            $output = $ps.EndInvoke($asyncResult)

            $output[0] | Should -BeExactly "output foo"
            $ps.streams.error.count | Should -Be 0
        }

        It 'erroraction Stop' {

            $command = 'get-foo -erroraction Stop'

            [void] $ps.AddScript($script + $command)
            $asyncResult = $ps.BeginInvoke()

            { $ps.EndInvoke($asyncResult) } | Should -Throw -ErrorId "ActionPreferenceStopException"
            # Exception: "Command execution stopped because the preference variable "ErrorActionPreference" or common parameter is set to Stop: error foo"

            # BUG in runspace api.
            #$ps.error.count | Should Be 1

            $ps.InvocationStateInfo.State | Should -BeExactly 'Failed'
        }
    }

    Context 'Splat' {
        BeforeAll {
            $skipTest = -not $EnabledExperimentalFeatures.Contains('PSCommonSplatParameter')
            if ($skipTest) {
                Write-Verbose "Test Suite Skipped: These tests require the PSCommonSplatParameter experimental feature to be enabled" -Verbose
                $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
                $PSDefaultParameterValues["it:skip"] = $true
                return
            }

            function Test-Splat {
                [CmdletBinding(DefaultParameterSetName = 'One')]
                param(
                    [Parameter(Position = 0, Mandatory, ParameterSetName = 'One')]
                    [ValidateNotNullOrEmpty()]
                    [string]
                    $OneFish,

                    [Parameter(Position = 0, Mandatory, ParameterSetName = 'Two')]
                    [ValidateNotNullOrEmpty()]
                    [string[]]
                    $TwoFish,

                    [Parameter()]
                    [ValidateSet('Red', 'Blue')]
                    [string]
                    $Color
                )

                [pscustomobject]@{
                    ParameterSetName = $PSCmdlet.ParameterSetName
                    BoundParameters  = @{} + $PSCmdlet.MyInvocation.BoundParameters
                }
            }

            $testParameterSetOneResults = {
                $result.ParameterSetName | Should -BeExactly 'One'
                $result.BoundParameters.Contains('OneFish') | Should -BeTrue
                $result.BoundParameters.Contains('Color') | Should -BeTrue
                $result.BoundParameters.OneFish | Should -Be 1
                $result.BoundParameters.Color | Should -Be 'Red'
            }

            $testParameterSetTwoResults = {
                $result.ParameterSetName | Should -BeExactly 'Two'
                $result.BoundParameters.Contains('TwoFish') | Should -BeTrue
                $result.BoundParameters.Contains('Color') | Should -BeTrue
                $result.BoundParameters.TwoFish | Should -Be 2
                $result.BoundParameters.Color | Should -Be 'Blue'
            }
        }

        AfterAll {
            if ($skipTest) {
                $PSDefaultParameterValues = $originalDefaultParameterValues
            }
        }

        It 'binds a single splatted inline hashtable correctly' {
            $result = Test-Splat -Splat @{
                OneFish = 1
                Color = 'Red'
            }
            . $testParameterSetOneResults
        }

        It 'binds a splatted inline variable correctly' {
            $parameters = @{
                OneFish = 1
                Color = 'Red'
            }
            $result = Test-Splat -Splat $parameters
            . $testParameterSetOneResults
        }

        It 'binds a splatted property value correctly' {
            $commandParameters = @{
                'TestSplat' = @{
                    OneFish = 1
                    Color   = 'Red'
                }
            }
            $result = Test-Splat -Splat $commandParameters.TestSplat
            . $testParameterSetOneResults
        }

        It 'binds the splatted results of a command properly' {
            function Get-ParameterSet {
                @{
                    OneFish = 1
                    Color   = 'Red'
                }
            }
            $result = Test-Splat -Splat (Get-ParameterSet)
            . $testParameterSetOneResults
        }

        It 'binds mulitple splatted hashtables correctly' {
            function Get-ErrorActionParameter {
                @{ErrorAction = 'Continue'}
            }
            $commandParameters = @{
                'TestSplat' = @{
                    Color = 'Red'
                }
            }
            $result = Test-Splat -Splat @{OneFish = 1 },$commandParameters.TestSplat,(Get-ErrorActionParameter)
            . $testParameterSetOneResults
            $result.BoundParameters.Contains('ErrorAction') | Should -BeTrue
            $result.BoundParameters.ErrorAction | Should -Be 'Continue'
        }

        It 'supports invocation with -parameter:value syntax' {
            $result = Test-Splat -Splat:@{
                TwoFish = 2
                Color = 'Blue'
            }
            . $testParameterSetTwoResults
        }

        It 'supports invocation with a shorthand parameter name' {
            $result = Test-Splat -sp @{
                TwoFish = 2
                Color = 'Blue'
            }
            . $testParameterSetTwoResults
        }

        It 'supports invocation with -shorthandParameter:value syntax' {
            $result = Test-Splat -sp:@{
                TwoFish = 2
                Color = 'Blue'
            }
            . $testParameterSetTwoResults
        }

        It 'can be used when splatting with the splat operator' {
            $params = @{
                'Splat' = @{
                    TwoFish = 2
                    Color   = 'Blue'
                }
            }
            $result = Test-Splat @params
            . $testParameterSetTwoResults
        }

        It 'only supports dictionaries' {
            $errorRecord = $null
            try { Test-Splat -Splat @(1, 'Red') } catch {$errorRecord = $_}
            $errorRecord | Should -Not -BeNullOrEmpty
            $errorRecord.FullyQualifiedErrorId | Should -BeExactly 'CannotConvertArgument,Test-Splat'
            $errorRecord.Exception | Should -BeOfType System.Management.Automation.ParameterBindingException
        }

        It 'does not support splatting a "splat" parameter' {
            $errorRecord = $null
            try {
                $result = Test-Splat -Splat @{
                    OneFish = 1
                    Splat   = @{
                        Color = 'Red'
                    }
                }
            } catch {
                $errorRecord = $_
            }
            $errorRecord | Should -Not -BeNullOrEmpty
            $errorRecord.FullyQualifiedErrorId | Should -BeExactly 'ParameterAlreadyBound,Test-Splat'
            $errorRecord.Exception | Should -BeOfType System.Management.Automation.ParameterBindingException
        }
    }

    Context "SupportShouldprocess" {
        $script = '
                function get-foo
                {
                    [CmdletBinding(SupportsShouldProcess=$true)]
                    param()

                    if($pscmdlet.shouldprocess("foo", "foo action"))
                    {
                        write-output "foo action"
                    }
                }'

        It 'SupportShouldprocess' {

            $command = 'get-foo'
            $ps = [system.management.automation.powershell]::Create()
            [void] $ps.AddScript($script + $command)
            $ps.RunspacePool = $rp
            $asyncResult = $ps.BeginInvoke()
            $output = $ps.EndInvoke($asyncResult)

            $output[0] | Should -BeExactly 'foo action'
        }

        It 'shouldprocess support -whatif' {

            $command = 'get-foo -whatif'
            $ps = [system.management.automation.powershell]::Create()
            [void] $ps.AddScript($script + $command)
            $ps.RunspacePool = $rp
            $asyncResult = $ps.BeginInvoke()
            $output = $ps.EndInvoke($asyncResult)

            $ps.InvocationStateInfo.State | Should -BeExactly 'Completed'
        }

        It 'shouldprocess support -confirm under the non-interactive host' {

            $command = 'get-foo -confirm'
            [void] $ps.AddScript($script + $command)

            $asyncResult = $ps.BeginInvoke()
            $ps.EndInvoke($asyncResult)

            $ps.Streams.Error.Count | Should -Be 1 # the host does not implement it.
            $ps.InvocationStateInfo.State | Should -BeExactly 'Completed'
        }
    }

    Context 'confirmimpact support: none' {
        BeforeAll {
            $script = '
                function get-foo
                {
                    [CmdletBinding(supportsshouldprocess=$true, ConfirmImpact="none")]
                    param()

                    if($pscmdlet.shouldprocess("foo", "foo action"))
                    {
                        write-output "foo action"
                    }
                }'
        }

        It 'get-foo' {
            $command = 'get-foo'
            [void] $ps.AddScript($script + $command)
            $asyncResult = $ps.BeginInvoke()
            $output = $ps.EndInvoke($asyncResult)

            $output[0] | Should -BeExactly 'foo action'
        }

        It 'get-foo -confirm' {
            $command = 'get-foo -confirm'
            [void] $ps.AddScript($script + $command)
            $asyncResult = $ps.BeginInvoke()
            $output = $ps.EndInvoke($asyncResult)

            $output[0] | Should -BeExactly 'foo action'
        }
    }

    Context 'confirmimpact support: low under the non-interactive host' {
        BeforeAll {
            $script = '
                function get-foo
                {
                    [CmdletBinding(SupportsShouldProcess=$true, ConfirmImpact="low")]
                    param()

                    if($pscmdlet.shouldprocess("foo", "foo action"))
                    {
                        write-output "foo action"
                    }
                }'
        }
        It 'get-foo' {
            $command = 'get-foo'
            [void] $ps.AddScript($script + $command)
            $asyncResult = $ps.BeginInvoke()
            $output = $ps.EndInvoke($asyncResult)

            $output[0] | Should -BeExactly 'foo action'
        }

        It 'get-foo -confirm' {
            $command = 'get-foo -confirm'

            [void] $ps.AddScript($script + $command)
            $asyncResult = $ps.BeginInvoke()
            $ps.EndInvoke($asyncResult)

            $ps.Streams.Error.Count | Should -Be 1  # the host does not implement it.
            $ps.InvocationStateInfo.State | Should -BeExactly 'Completed'
        }
    }

    Context 'confirmimpact support: Medium under the non-interactive host' {
        BeforeAll {
            $script = '
                function get-foo
                {
                    [CmdletBinding(SupportsShouldProcess=$true, ConfirmImpact="medium")]
                    param()

                    if($pscmdlet.shouldprocess("foo", "foo action"))
                    {
                        write-output "foo action"
                    }
                }'
        }

        It 'get-foo' {
            $command = 'get-foo'
            [void] $ps.AddScript($script + $command)
            $asyncResult = $ps.BeginInvoke()
            $output = $ps.EndInvoke($asyncResult)

            $output[0] | Should -BeExactly 'foo action'
        }

        It 'get-foo -confirm' {
            $command = 'get-foo -confirm'
            [void] $ps.AddScript($script + $command)

            $asyncResult = $ps.BeginInvoke()
            $ps.EndInvoke($asyncResult)

            $ps.Streams.Error.Count | Should -Be 1  # the host does not implement it.
            $ps.InvocationStateInfo.State | Should -BeExactly 'Completed'
        }
    }

    Context 'confirmimpact support: High under the non-interactive host' {
        BeforeAll {
            $script = '
                function get-foo
                {
                    [CmdletBinding(SupportsShouldProcess=$true, ConfirmImpact="high")]
                    param()

                    if($pscmdlet.shouldprocess("foo", "foo action"))
                    {
                        write-output "foo action"
                    }
                }'
        }

        It 'get-foo' {
            $command = 'get-foo'
            [void] $ps.AddScript($script + $command)
            $asyncResult = $ps.BeginInvoke()
            $ps.EndInvoke($asyncResult)

            $ps.Streams.Error.Count | Should -Be 1 # the host does not implement it.
            $ps.InvocationStateInfo.State | Should -BeExactly 'Completed'
        }

        It 'get-foo -confirm' {
            $command = 'get-foo -confirm'
            [void] $ps.AddScript($script + $command)
            $asyncResult = $ps.BeginInvoke()
            $ps.EndInvoke($asyncResult)

            $ps.Streams.Error.Count | Should -Be 1 # the host does not implement it.
            $ps.InvocationStateInfo.State | Should -BeExactly 'Completed'
        }
    }

    Context 'ShouldContinue Support under the non-interactive host' {
        BeforeAll {
            $script = '
                function get-foo
                {
                    [CmdletBinding()]
                    param()

                    if($pscmdlet.shouldcontinue("foo", "foo action"))
                    {
                        write-output "foo action"
                    }
                }'
        }

        It 'get-foo' {
            $command = 'get-foo'
            [void] $ps.AddScript($script + $command)

            $asyncResult = $ps.BeginInvoke()
            $ps.EndInvoke($asyncResult)

            $ps.Streams.Error.Count | Should -Be 1   # the host does not implement it.
            $ps.InvocationStateInfo.State | Should -BeExactly 'Completed'
        }
    }
}
