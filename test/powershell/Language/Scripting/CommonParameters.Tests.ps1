# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Common parameters support for script cmdlets" -Tags "CI" {
    BeforeEach {
        $rs = [system.management.automation.runspaces.runspacefactory]::CreateRunspace()
        $rs.open()
        $powerShell = [System.Management.Automation.PowerShell]::Create()
        $powerShell.Runspace = $rs
    }

    AfterEach {
            $powerShell.Dispose()
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
            [void] $powerShell.AddScript($script + $command)
            $asyncResult = $powerShell.BeginInvoke()
            $output = $powerShell.EndInvoke($asyncResult)

            $output[0] | Should -BeExactly "output foo"
            $powerShell.Streams.Debug.Count | Should -Be 0
        }

        It 'get-foo -debug' {
            $command = 'get-foo -debug'
            [void] $powerShell.AddScript($script + $command)
            $asyncResult = $powerShell.BeginInvoke()
            $output = $powerShell.EndInvoke($asyncResult)

            $output[0] | Should -BeExactly "output foo"
            $powerShell.Streams.Debug[0].Message | Should -BeExactly "debug foo"
            $powerShell.InvocationStateInfo.State | Should -BeExactly 'Completed'
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
            [void] $powerShell.AddScript($script + $command)
            $asyncResult = $powerShell.BeginInvoke()
            $output = $powerShell.EndInvoke($asyncResult)

            $output[0] | Should -BeExactly "output foo"
            $powerShell.streams.verbose.Count | Should -Be 0
        }

        It 'get-foo -verbose' {
            $command = 'get-foo -verbose'

            [void] $powerShell.AddScript($script + $command)
            $asyncResult = $powerShell.BeginInvoke()
            $output = $powerShell.EndInvoke($asyncResult)

            $output[0] | Should -BeExactly "output foo"
            $powerShell.Streams.verbose[0].Message | Should -BeExactly "verbose foo"
            $powerShell.InvocationStateInfo.State | Should -BeExactly 'Completed'
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
            [void] $powerShell.AddScript($script + $command)
            $asyncResult = $powerShell.BeginInvoke()
            $output = $powerShell.EndInvoke($asyncResult)

            $output[0] | Should -BeExactly "output foo"
            $powerShell.Streams.error[0].ToString() | Should -Match "error foo"
        }

        It 'erroraction continue' {

            $command = 'get-foo -erroraction Continue'
            [void] $powerShell.AddScript($script + $command)
            $asyncResult = $powerShell.BeginInvoke()
            $output = $powerShell.EndInvoke($asyncResult)

            $output[0] | Should -BeExactly "output foo"
            $powerShell.Streams.error[0].ToString() | Should -Match "error foo"
        }

        It 'erroraction SilentlyContinue' {

            $command = 'get-foo -erroraction SilentlyContinue'
            [void] $powerShell.AddScript($script + $command)
            $asyncResult = $powerShell.BeginInvoke()
            $output = $powerShell.EndInvoke($asyncResult)

            $output[0] | Should -BeExactly "output foo"
            $powerShell.streams.error.count | Should -Be 0
        }

        It 'erroraction Stop' {

            $command = 'get-foo -erroraction Stop'

            [void] $powerShell.AddScript($script + $command)
            $asyncResult = $powerShell.BeginInvoke()

            { $powerShell.EndInvoke($asyncResult) } | Should -Throw -ErrorId "ActionPreferenceStopException"
            # Exception: "Command execution stopped because the preference variable "ErrorActionPreference" or common parameter is set to Stop: error foo"

            # BUG in runspace api.
            #$powerShell.error.count | Should -Be 1

            $powerShell.InvocationStateInfo.State | Should -BeExactly 'Failed'
        }
    }

    Context "ProgressAction" {
        It "Ignores progress actions on advanced script function with no variables" {
            $powerShell.AddScript(
@'
function test-function {
    [CmdletBinding()]param()

    Write-Progress "progress foo"
}
test-function -ProgressAction Ignore
'@).Invoke()

            $powerShell.Streams.Progress.Count | Should -Be 0
            $powerShell.Streams.Error | ForEach-Object {
                Write-Error -ErrorRecord $_ -ErrorAction Stop
            }
        }

        It "Ignores progress actions on advanced script function with variables" {
            $powerShell.AddScript(
@'
function test-function {
    [CmdletBinding()]param([string]$path)

    switch($false) { default { "echo $path" } }

    Write-Progress "progress foo"
}
test-function -ProgressAction Ignore
'@).Invoke()

            $powerShell.Streams.Progress.Count | Should -Be 0
            $powerShell.Streams.Error | ForEach-Object {
                Write-Error -ErrorRecord $_ -ErrorAction Stop
            }
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
            $powerShell = [system.management.automation.powershell]::Create()
            [void] $powerShell.AddScript($script + $command)
            $powerShell.RunspacePool = $rp
            $asyncResult = $powerShell.BeginInvoke()
            $output = $powerShell.EndInvoke($asyncResult)

            $output[0] | Should -BeExactly 'foo action'
        }

        It 'shouldprocess support -whatif' {

            $command = 'get-foo -whatif'
            $powerShell = [system.management.automation.powershell]::Create()
            [void] $powerShell.AddScript($script + $command)
            $powerShell.RunspacePool = $rp
            $asyncResult = $powerShell.BeginInvoke()
            $output = $powerShell.EndInvoke($asyncResult)

            $powerShell.InvocationStateInfo.State | Should -BeExactly 'Completed'
        }

        It 'shouldprocess support -confirm under the non-interactive host' {

            $command = 'get-foo -confirm'
            [void] $powerShell.AddScript($script + $command)

            $asyncResult = $powerShell.BeginInvoke()
            $powerShell.EndInvoke($asyncResult)

            $powerShell.Streams.Error.Count | Should -Be 1 # the host does not implement it.
            $powerShell.InvocationStateInfo.State | Should -BeExactly 'Completed'
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
            [void] $powerShell.AddScript($script + $command)
            $asyncResult = $powerShell.BeginInvoke()
            $output = $powerShell.EndInvoke($asyncResult)

            $output[0] | Should -BeExactly 'foo action'
        }

        It 'get-foo -confirm' {
            $command = 'get-foo -confirm'
            [void] $powerShell.AddScript($script + $command)
            $asyncResult = $powerShell.BeginInvoke()
            $output = $powerShell.EndInvoke($asyncResult)

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
            [void] $powerShell.AddScript($script + $command)
            $asyncResult = $powerShell.BeginInvoke()
            $output = $powerShell.EndInvoke($asyncResult)

            $output[0] | Should -BeExactly 'foo action'
        }

        It 'get-foo -confirm' {
            $command = 'get-foo -confirm'

            [void] $powerShell.AddScript($script + $command)
            $asyncResult = $powerShell.BeginInvoke()
            $powerShell.EndInvoke($asyncResult)

            $powerShell.Streams.Error.Count | Should -Be 1  # the host does not implement it.
            $powerShell.InvocationStateInfo.State | Should -BeExactly 'Completed'
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
            [void] $powerShell.AddScript($script + $command)
            $asyncResult = $powerShell.BeginInvoke()
            $output = $powerShell.EndInvoke($asyncResult)

            $output[0] | Should -BeExactly 'foo action'
        }

        It 'get-foo -confirm' {
            $command = 'get-foo -confirm'
            [void] $powerShell.AddScript($script + $command)

            $asyncResult = $powerShell.BeginInvoke()
            $powerShell.EndInvoke($asyncResult)

            $powerShell.Streams.Error.Count | Should -Be 1  # the host does not implement it.
            $powerShell.InvocationStateInfo.State | Should -BeExactly 'Completed'
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
            [void] $powerShell.AddScript($script + $command)
            $asyncResult = $powerShell.BeginInvoke()
            $powerShell.EndInvoke($asyncResult)

            $powerShell.Streams.Error.Count | Should -Be 1 # the host does not implement it.
            $powerShell.InvocationStateInfo.State | Should -BeExactly 'Completed'
        }

        It 'get-foo -confirm' {
            $command = 'get-foo -confirm'
            [void] $powerShell.AddScript($script + $command)
            $asyncResult = $powerShell.BeginInvoke()
            $powerShell.EndInvoke($asyncResult)

            $powerShell.Streams.Error.Count | Should -Be 1 # the host does not implement it.
            $powerShell.InvocationStateInfo.State | Should -BeExactly 'Completed'
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
            [void] $powerShell.AddScript($script + $command)

            $asyncResult = $powerShell.BeginInvoke()
            $powerShell.EndInvoke($asyncResult)

            $powerShell.Streams.Error.Count | Should -Be 1   # the host does not implement it.
            $powerShell.InvocationStateInfo.State | Should -BeExactly 'Completed'
        }
    }
}
