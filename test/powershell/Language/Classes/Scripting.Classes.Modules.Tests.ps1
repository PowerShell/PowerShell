Describe 'PSModuleInfo.GetExportedTypeDefinitions()' {
    It "doesn't throw for any module" {
        $discard = Get-Module -ListAvailable | % { $_.GetExportedTypeDefinitions() }
        $true | Should Be $true # we only verify that we didn't throw. This line contains a dummy Should to make pester happy.
    }
}

Describe 'use of a module from two runspaces' {
    function New-TestModule {
        param(
            [string]$Name, 
            [string]$Content
        )
        
        mkdir -Force "TestDrive:\$Name" > $null    
        $manifestParams = @{
            Path = "TestDrive:\$Name\$Name.psd1"
        }
        
        if ($Content) {
            Set-Content -Path TestDrive:\$Name\$Name.psm1 -Value $Content
            $manifestParams['RootModule'] = "$Name.psm1"
        }
        
        New-ModuleManifest @manifestParams

        $resolvedTestDrivePath = Split-Path ((ls TestDrive:\)[0].FullName)
        if (-not ($env:PSModulePath -like "*$resolvedTestDrivePath*")) {
            $env:PSModulePath += ";$resolvedTestDrivePath"
        }
    }

    $originalPSModulePath = $env:PSModulePath
    try {
        
        New-TestModule -Name 'Random' -Content @'
$script:random = Get-Random
class RandomWrapper
{
    [int] getRandom()
    {
        return $script:random
    }
}
'@

        It 'use different sessionStates for different modules' {
            $ps = 1..2 | % { $p = [powershell]::Create().AddScript(@'
Import-Module Random
'@)
                $p.Invoke() > $null
                $p
            }
            $res = 1..2 | % {
                0..1 | % {
                    $ps[$_].Commands.Clear()
                    # The idea: instance created inside the context, in one runspace. 
                    # Method is called on instance in the different runspace, but it should know about the origin.
                    $w = $ps[$_].AddScript('& (Get-Module Random) { [RandomWrapper]::new() }').Invoke()[0]
                    $w.getRandom()
                }
            }
            
            $res.Count | Should Be 4
            $res[0] | Should Not Be $res[1]
            $res[0] | Should Be $res[2]
            $res[1] | Should Be $res[3]
        }

    } finally {
        $env:PSModulePath = $originalPSModulePath
    }

}