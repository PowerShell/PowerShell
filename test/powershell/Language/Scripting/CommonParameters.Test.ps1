Describe "Common parameters support for script cmdlets" -Tags "CI" {
    $rp = [system.management.automation.runspaces.runspacefactory]::createrunspacepool(1, 1)
    $rp.open()

    Context "Debug" {

        $script = "
            function get-foo
            {
                [CmdletBinding()]
                param()
        
                write-output 'output foo'
                write-debug  'debug foo'
            }"
        
        $command = 'get-foo'

        $ps = [system.management.automation.powershell]::Create()
        [void] $ps.AddScript($script + $command)
        $ps.RunspacePool = $rp
        $asyncResult = $ps.BeginInvoke()
        $output = $ps.EndInvoke($asyncResult)

        It '$output[0]' { $output[0] | Should be "output foo" }
        It '$ps.Streams.Debug.Count' { $ps.Streams.Debug.Count | Should Be 0 }
        
        $command = 'get-foo -debug'    
        $ps = [system.management.automation.powershell]::Create()
        [void] $ps.AddScript($script + $command)
        $ps.RunspacePool = $rp
        $asyncResult = $ps.BeginInvoke()
        $output = $ps.EndInvoke($asyncResult)
        
        It '$output[0]' { $output[0] | Should Be "output foo" }
        It '$ps.Streams.Debug[0].Message' { $ps.Streams.Debug[0].Message | Should Be "debug foo" }
        It '$ps.InvocationStateInfo.State' { $ps.InvocationStateInfo.State | Should Be 'Completed' }
    }

    Context "verbose" {

        $script = "
            function get-foo
            {
                [CmdletBinding()]
                param()
            
                write-output 'output foo'
                write-verbose  'verbose foo'
            }"

        $command = 'get-foo'
    
        $ps = [system.management.automation.powershell]::Create()
        [void] $ps.AddScript($script + $command)
        $ps.RunspacePool = $rp
        $asyncResult = $ps.BeginInvoke()
        $output = $ps.EndInvoke($asyncResult)

        It '$output[0]' { $output[0] | Should Be "output foo" }
        It '$ps.streams.verbose.Count' { $ps.streams.verbose.Count | Should Be 0 }

        $command = 'get-foo -verbose'

        $ps = [system.management.automation.powershell]::Create()
        [void] $ps.AddScript($script + $command)
        $ps.RunspacePool = $rp
        $asyncResult = $ps.BeginInvoke()
        $output = $ps.EndInvoke($asyncResult)

        It '$output[0]' { $output[0] | Should Be "output foo" }
        It '$ps.Streams.verbose[0].Message' { $ps.Streams.verbose[0].Message | Should Be "verbose foo" }
        It '$ps.InvocationStateInfo.State' { $ps.InvocationStateInfo.State | Should Be 'Completed' }
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
            $ps = [system.management.automation.powershell]::Create()
            [void] $ps.AddScript($script + $command)
            $ps.RunspacePool = $rp
            $asyncResult = $ps.BeginInvoke()
            $output = $ps.EndInvoke($asyncResult)

            $output[0] | Should Be "output foo"
            ($ps.Streams.error[0].ToString() -like "error foo") | Should Be $true
        }

        It 'erroraction continue' {

            $command = 'get-foo -erroraction Continue'
            $ps = [system.management.automation.powershell]::Create()
            [void] $ps.AddScript($script + $command)
            $ps.RunspacePool = $rp
            $asyncResult = $ps.BeginInvoke()
            $output = $ps.EndInvoke($asyncResult)

            $output[0] | Should Be "output foo"
            ($ps.Streams.error[0].ToString() -like "error foo") | Should Be $true
        }

        It 'erroraction SilentlyContinue' {

            $command = 'get-foo -erroraction SilentlyContinue'
            $ps = [system.management.automation.powershell]::Create()
            [void] $ps.AddScript($script + $command)
            $ps.RunspacePool = $rp
            $asyncResult = $ps.BeginInvoke()
            $output = $ps.EndInvoke($asyncResult)

            $output[0] | Should Be "output foo"
            $ps.streams.error.count | Should Be 0
        }

        It 'erroraction Stop' {

            $command = 'get-foo -erroraction Stop'

            $ps = [system.management.automation.powershell]::Create()
            [void] $ps.AddScript($script + $command)
            $ps.RunspacePool = $rp
            $asyncResult = $ps.BeginInvoke()
            $failed = $true
            try
            {
                $ps.EndInvoke($asyncResult)
                $failed = $false
            }
            catch {
                $_.FullyQualifiedErrorId | Should Be "ActionPreferenceStopException"
            } # Exception: "Command execution stopped because the preference variable "ErrorActionPreference" or common parameter is set to Stop: error foo"

            $failed | Should Be $true

            # BUG in runspace api.
            #$ps.error.count | Should Be 1

            $ps.InvocationStateInfo.State | Should Be 'Failed'
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

            $output[0] | Should Be 'foo action'
        }

        It 'shouldprocess support -whatif' {

            $command = 'get-foo -whatif'
            $ps = [system.management.automation.powershell]::Create()
            [void] $ps.AddScript($script + $command)
            $ps.RunspacePool = $rp
            $asyncResult = $ps.BeginInvoke()
            $output = $ps.EndInvoke($asyncResult)

            $ps.InvocationStateInfo.State | Should Be 'Completed'
        }

        It 'shouldprocess support -confirm under the non-interactive host' {

            $command = 'get-foo -confirm'

            $ps = [system.management.automation.powershell]::Create()
            [void] $ps.AddScript($script + $command)
            $ps.RunspacePool = $rp

            $asyncResult = $ps.BeginInvoke()
            $ps.EndInvoke($asyncResult)
            
            $ps.Streams.Error.Count | Should Be 1 # the host does not implement it.
            $ps.InvocationStateInfo.State | Should Be 'Completed'
        }
    }

    It 'confirmimpact support: none' {

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

        $command = 'get-foo'

        $ps = [system.management.automation.powershell]::Create()
        [void] $ps.AddScript($script + $command)
        $ps.RunspacePool = $rp
        $asyncResult = $ps.BeginInvoke()
        $output = $ps.EndInvoke($asyncResult)

        $output[0] | Should Be 'foo action'

        $command = 'get-foo -confirm'
        $ps = [system.management.automation.powershell]::Create()
        [void] $ps.AddScript($script + $command)
        $ps.RunspacePool = $rp
        $asyncResult = $ps.BeginInvoke()
        $output = $ps.EndInvoke($asyncResult)

        $output[0] | Should Be 'foo action'
    }

    Context 'confirmimpact support: low under the non-interactive host' {

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

        $command = 'get-foo'

        $ps = [system.management.automation.powershell]::Create()
        [void] $ps.AddScript($script + $command)
        $ps.RunspacePool = $rp
        $asyncResult = $ps.BeginInvoke()
        $output = $ps.EndInvoke($asyncResult)

        It '$output[0]' { $output[0] | Should Be 'foo action' }

        $command = 'get-foo -confirm'

        $ps = [system.management.automation.powershell]::Create()
        [void] $ps.AddScript($script + $command)
        $ps.RunspacePool = $rp

        $asyncResult = $ps.BeginInvoke()
        $ps.EndInvoke($asyncResult)

        It '$ps.Streams.Error.Count' { $ps.Streams.Error.Count | Should Be 1 }  # the host does not implement it.
        It '$ps.InvocationStateInfo.State' { $ps.InvocationStateInfo.State | Should Be 'Completed' }
    }

    Context 'confirmimpact support: Medium under the non-interactive host' {

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

        $command = 'get-foo'

        $ps = [system.management.automation.powershell]::Create()
        [void] $ps.AddScript($script + $command)
        $ps.RunspacePool = $rp
        $asyncResult = $ps.BeginInvoke()
        $output = $ps.EndInvoke($asyncResult)

        It '$output[0]' { $output[0] | Should Be 'foo action' }

        $command = 'get-foo -confirm'
        $ps = [system.management.automation.powershell]::Create()
        [void] $ps.AddScript($script + $command)
        $ps.RunspacePool = $rp

        $asyncResult = $ps.BeginInvoke()
        $ps.EndInvoke($asyncResult)

        It '$ps.Streams.Error.Count' { $ps.Streams.Error.Count | Should Be 1}  # the host does not implement it.
        It '$ps.InvocationStateInfo.State' { $ps.InvocationStateInfo.State | Should Be 'Completed' }
    }


    Context 'confirmimpact support: High under the non-interactive host' {

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

        $command = 'get-foo'
        $ps = [system.management.automation.powershell]::Create()
        [void] $ps.AddScript($script + $command)
        $ps.RunspacePool = $rp

        $asyncResult = $ps.BeginInvoke()
        $ps.EndInvoke($asyncResult)

        It '$ps.Streams.Error.Count' { $ps.Streams.Error.Count | Should Be 1 } # the host does not implement it.
        It '$ps.InvocationStateInfo.State' { $ps.InvocationStateInfo.State | Should Be 'Completed' }


        $command = 'get-foo -confirm'
        $ps = [system.management.automation.powershell]::Create()
        [void] $ps.AddScript($script + $command)
        $ps.RunspacePool = $rp

        $asyncResult = $ps.BeginInvoke()
        $ps.EndInvoke($asyncResult)

        It '$ps.Streams.Error.Count' { $ps.Streams.Error.Count | Should Be 1 } # the host does not implement it.
        It '$ps.InvocationStateInfo.State' { $ps.InvocationStateInfo.State | Should be 'Completed' }
    }

    Context 'ShouldContinue Support under the non-interactive host' {

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

        $command = 'get-foo'
        $ps = [system.management.automation.powershell]::Create()
        [void] $ps.AddScript($script + $command)
        $ps.RunspacePool = $rp

        $asyncResult = $ps.BeginInvoke()
        $ps.EndInvoke($asyncResult)

        It '$ps.Streams.Error.Count' { $ps.Streams.Error.Count | Should Be 1 }   # the host does not implement it.
        It '$ps.InvocationStateInfo.State' { $ps.InvocationStateInfo.State | Should Be 'Completed' }
    }
}