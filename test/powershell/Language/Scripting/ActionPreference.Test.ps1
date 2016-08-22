
Describe "Tests for (error, warning, etc) action preference" -Tags "CI" {

        Context 'Setting ErrorActionPreference to stop prevents user from getting the error exception' {
            $err = $null
            try
            {
                dir nosuchfile.nosuchextension -ea stop -ev err                
            }
            catch {}
             
            It '$err.Count' { $err.Count | Should Be 1 }
            It '$err[0] should not be $null' { $err[0] | Should Not Be $null }
            It '$err[0].GetType().Name' { $err[0].GetType().Name | Should Be ActionPreferenceStopException }
            It '$err[0].ErrorRecord' { $err[0].ErrorRecord | Should not BeNullOrEmpty }
            It '$err[0].ErrorRecord.Exception.GetType().Name' { $err[0].ErrorRecord.Exception.GetType().Name | Should Be ItemNotFoundException }
        }
        
        It 'ActionPreference Ignore Works' {            
            $errorCount = $error.Count
            Get-Process -Name asdfasdfsadfsadf -ErrorAction Ignore
            $errorCount2 = $error.Count

            $error.Count | Should Be $errorCount
        }
        
        Context 'action preference of Ignore cannot be set as a preference variable' {            
            $failed = $true
            try {
                $GLOBAL:errorActionPreference = "Ignore"
                Get-Process -Name asdfasdfasdf
                $failed = $false
             } catch {
                    It '$_.CategoryInfo.Reason' { $_.CategoryInfo.Reason | Should Be NotSupportedException }
             }

            It 'Expect exception' { $failed | Should be $true }
        }

        Context 'action preference of Suspend cannot be set as a preference variable' {
            $failed = $true
            try { 
                $GLOBAL:errorActionPreference = "Suspend"
                Get-Process -Name asdfasdfasdf
                $failed = $false
                } catch { 
                    It '$_.CategoryInfo.Reason' { $_.CategoryInfo.Reason | Should Be ArgumentTransformationMetadataException }
                }
                It 'Expect exception' { $failed | Should be $true }
        }

        It 'enum disambiguation works' {
            $errorCount = $error.Count
            Get-Process -Name asdfasdfsadfsadf -ErrorAction Ig

            $error.Count | Should Be $errorCount
        }

        It 'ErrorAction = Suspend works on Workflow' {
            workflow TestErrorActionSuspend { "Hello" }
    
            $r = TestErrorActionSuspend -ErrorAction Suspend
    
            ## suspend functionality itself tested in workflow tests
            $r | Should Be Hello
        }

        Context 'ErrorAction = Suspend does not work on functions' {

            function MyHelperFunction {
                [CmdletBinding()]
                param()        
                "Hello"
            }
    
            $failed = $true
            try
            {
                MyHelperFunction -ErrorAction Suspend
                $failed = $false
            } catch {
                It '$_.FullyQualifiedErrorId' { $_.FullyQualifiedErrorId | Should Be "ParameterBindingFailed,MyHelperFunction" }
            }
            It 'Expect exception' { $failed | Should be $true }
        }

        Context 'ErrorAction = Suspend does not work on cmdlets' {

            $failed = $true
            try
            {
                Get-Process -ErrorAction Suspend
                $failed = $false
            }
            catch {
                It '$_.FullyQualifiedErrorId' { $_.FullyQualifiedErrorId | Should Be "ParameterBindingFailed,Microsoft.PowerShell.Commands.GetProcessCommand" }
            }
            It 'Expect exception' { $failed | Should be $true }
        }

        Context 'WarningAction = Suspend does not work' {

            try
            {
                Get-Process -WarningAction Suspend
            }
            catch {
                It '$_.FullyQualifiedErrorId' { $_.FullyQualifiedErrorId | Should Be "ParameterBindingFailed,Microsoft.PowerShell.Commands.GetProcessCommand" }
            }
            It 'Expect exception' { $failed | Should be $true }
        }

        Context 'ErrorAction and WarningAction are the only action preferences do not support suspend' -Skip {

            $params = [System.Management.Automation.Internal.CommonParameters].GetProperties().Name | sls Action
            $suspendErrors = $null 
            $num=0  
                        
            $params | % {
                        $input=@{'InputObject' = 'Test';$_='Suspend'}
                        
                        try {
                            Write-Output @input
                            } catch {
                                It '$_.FullyQualifiedErrorId' { $_.FullyQualifiedErrorId | Should Be "ParameterBindingFailed,Microsoft.PowerShell.Commands.WriteOutputCommand" }
                                $num++
                            }
                    }   
            
            It 'number of action preferences' { $num | Should Be 2 }
        }
}
