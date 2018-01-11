Import-module -Name "$PSScriptRoot\containerTestCommon.psm1" -Force
$script:linuxContainerTests = Get-LinuxContainer
$script:windowsContainerTests = Get-WindowsContainer
$script:skipLinux = Test-SkipLinux
$script:skipWindows = Test-SkipWindows

Describe "Build Linux Containers" -Tags 'Build', 'Linux' {
    BeforeAll {
        Set-RepoName 'pscontainertest'
    }

    it "$(Get-RepoName):<Name> builds from '<path>'" -TestCases $script:linuxContainerTests -Skip:$script:skipLinux {
        param(
            [Parameter(Mandatory=$true)]
            [string]
            $name,

            [Parameter(Mandatory=$true)]
            [string]
            $path
        )
        { Invoke-Docker -Command build -Params '--pull', '--quiet', '-t', "$(Get-RepoName):${Name}", $path -SuppressHostOutput} | should not throw
    }
}

Describe "Build Windows Containers" -Tags 'Build', 'Windows' {
    BeforeAll {
        Set-RepoName 'pscontainertest'
    }

    it "$(Get-RepoName):<Name> builds from '<path>'" -TestCases $script:windowsContainerTests  -skip:$script:skipWindows {
        param(
            [Parameter(Mandatory=$true)]
            [string]
            $name,

            [Parameter(Mandatory=$true)]
            [string]
            $path
        )

        { Invoke-Docker -Command build -Params @(
            '--pull'
            '--quiet'
            '-t'
            "$(Get-RepoName):${Name}"
            $path
        ) -SuppressHostOutput} | should not throw
    }
}

Describe "Linux Containers run PowerShell" -Tags 'Behavior', 'Linux' {
    BeforeAll{
        $testContext = Get-TestContext -type Linux
    }
    AfterAll{
        # prune unused volumes
        $null=Invoke-Docker -Command 'volume', 'prune' -Params '--force' -SuppressHostOutput
    }
    BeforeEach {
        Remove-Item $testContext.resolvedXmlPath -ErrorAction SilentlyContinue
        Remove-Item $testContext.resolvedLogPath -ErrorAction SilentlyContinue
    }

    it "Get PSVersion table from $(Get-RepoName):<Name>" -TestCases $script:linuxContainerTests -Skip:$script:skipLinux {
        param(
            [Parameter(Mandatory=$true)]
            [string]
            $name,

            [Parameter(Mandatory=$true)]
            [string]
            $path
        )

        Get-ContainerPowerShellVersion -TestContext $testContext -Name $Name -RepoName (Get-RepoName)  | should be '6.0.0-rc.2'
    }
}

Describe "Windows Containers run PowerShell" -Tags 'Behavior', 'Windows' {
    BeforeAll{
        $testContext = Get-TestContext -type Windows
    }
    BeforeEach {
        Remove-Item $testContext.resolvedXmlPath -ErrorAction SilentlyContinue
        Remove-Item $testContext.resolvedLogPath -ErrorAction SilentlyContinue
    }

    it "Get PSVersion table from $(Get-RepoName):<Name>" -TestCases $script:windowsContainerTests -skip:$script:skipWindows {
        param(
            [Parameter(Mandatory=$true)]
            [string]
            $name,

            [Parameter(Mandatory=$true)]
            [string]
            $path
        )

        Get-ContainerPowerShellVersion -TestContext $testContext -Name $Name -RepoName (Get-RepoName)  | should be '6.0.0-rc.2'
    }
}
