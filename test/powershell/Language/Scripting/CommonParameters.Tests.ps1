# Copyright (c) Microsoft Corporation.
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
            #$ps.error.count | Should -Be 1

            $ps.InvocationStateInfo.State | Should -BeExactly 'Failed'
        }
    }

    Context "SupportShouldprocess" {
        $script = '
                function get-foo
                {
                    [CmdletBinding(SupportsShouldProcess=$true)]
                    param()

                    if($PSCmdlet.shouldprocess("foo", "foo action"))
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

                    if($PSCmdlet.shouldprocess("foo", "foo action"))
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

                    if($PSCmdlet.shouldprocess("foo", "foo action"))
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

                    if($PSCmdlet.shouldprocess("foo", "foo action"))
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

                    if($PSCmdlet.shouldprocess("foo", "foo action"))
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

                    if($PSCmdlet.shouldcontinue("foo", "foo action"))
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
