# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Early-access module feeds' {
    BeforeAll {
        Import-Module "$PSScriptRoot\..\..\build.psm1" -Force
        $script:originalEarlyAccessFeed = $env:EARLY_ACCESS_FEED
        $script:moduleFeed = 'https://pkgs.dev.azure.com/powershell-rel/PowerShell/_packaging/powershell-module-earlyaccess/nuget/v3/index.json'
        $script:powerShellFeed = 'https://pkgs.dev.azure.com/powershell/PowerShell/_packaging/PowerShell/nuget/v3/index.json'
    }

    Describe 'Early-access pipeline authentication' {
        BeforeAll {
            $template = Get-Content "$PSScriptRoot\..\..\.pipelines\templates\insert-nuget-config-azfeed.yml" -Raw
            $inlineMatch = [regex]::Match($template, '(?ms)    inlineScript: \|\r?\n(?<script>.*?)(?=  displayName:)')
            if (-not $inlineMatch.Success) {
                throw 'Azure CLI authentication script not found.'
            }

            $inline = $inlineMatch.Groups['script'].Value -replace '(?m)^      ', ''
            $inline = $inline.Replace('$(EARLY_ACCESS_FEED)', 'net11').
                Replace('$(AzDevopsFeedUserNameKVPAT)', 'test-user').
                Replace('$(powershellPackageReadPat)', 'test-pat')
            $script:authenticationScript = [scriptblock]::Create($inline)
            $script:originalLastExitCode = $global:LASTEXITCODE
            $script:originalIsEarlyAccess = $env:ISEARLYACCESS
            $script:originalUseEarlyAccessModules = $env:USEEARLYACCESSMODULES

            function az { param([Parameter(ValueFromRemainingArguments)] $Argument) }
        }

        BeforeEach {
            Mock az {
                $global:LASTEXITCODE = 0
                'test-entra-token'
            }
            Mock Write-Host {}
        }

        AfterAll {
            $global:LASTEXITCODE = $script:originalLastExitCode
            $env:ISEARLYACCESS = $script:originalIsEarlyAccess
            $env:USEEARLYACCESSMODULES = $script:originalUseEarlyAccessModules
        }

        It 'Authenticates only the enabled feeds (NET=<Net>, Modules=<Modules>)' -TestCases @(
            @{ Net = 'false'; Modules = 'true'; Count = 2 }
            @{ Net = 'true'; Modules = 'false'; Count = 2 }
            @{ Net = 'true'; Modules = 'true'; Count = 3 }
        ) {
            param($Net, $Modules, $Count)

            $env:ISEARLYACCESS = $Net
            $env:USEEARLYACCESSMODULES = $Modules
            & $script:authenticationScript

            Should -Invoke Write-Host -Times 1 -Exactly -ParameterFilter {
                $Object -eq '##vso[task.setsecret]test-entra-token'
            }
            Should -Invoke Write-Host -Times 1 -Exactly -ParameterFilter {
                $prefix = '##vso[task.setvariable variable=VSS_NUGET_EXTERNAL_FEED_ENDPOINTS;issecret=true]'
                if (-not "$Object".StartsWith($prefix)) { return $false }
                $credentials = ("$Object".Substring($prefix.Length) | ConvertFrom-Json).endpointCredentials
                $netCredentials = @($credentials | Where-Object endpoint -like '*powershell-net-11-early-access/nuget/v3/index.json')
                $moduleCredentials = @($credentials | Where-Object endpoint -like '*powershell-module-earlyaccess/nuget/v3/index.json')
                $credentials.Count -eq $Count -and
                $credentials[0].username -eq 'test-user' -and
                $credentials[0].password -eq 'test-pat' -and
                $netCredentials.Count -eq [int]($Net -eq 'true') -and
                $moduleCredentials.Count -eq [int]($Modules -eq 'true') -and
                @($credentials | Where-Object password -eq 'test-entra-token').Count -eq ($Count - 1)
            }
        }

        It 'Supports module-only authentication with no .NET feed selector' {
            $env:ISEARLYACCESS = 'false'
            $env:USEEARLYACCESSMODULES = 'true'
            $moduleOnlyScript = [scriptblock]::Create($script:authenticationScript.ToString().Replace("'net11'", "''"))

            { & $moduleOnlyScript } | Should -Not -Throw
        }

        It 'Fails without publishing credentials when token acquisition fails' {
            Mock az { $global:LASTEXITCODE = 1 }

            { & $script:authenticationScript } | Should -Throw '*Failed to get an Azure DevOps access token*'
            Should -Invoke Write-Host -Times 0 -Exactly
        }
    }

    It 'Maps secret feed credentials directly into the build environment in <Pipeline>' -TestCases @(
        @{ Pipeline = '.pipelines/templates/stages/PowerShell-vPack-Stages.yml' }
        @{ Pipeline = '.pipelines/MSIXBundle-vPack-Official.yml' }
    ) {
        param($Pipeline)

        $template = Get-Content "$PSScriptRoot/../../$Pipeline" -Raw
        $buildStep = [regex]::Match($template, '(?ms)^\s*displayName: [''"]?Build Windows Universal[^\r\n]*\r?\n(?<envIndent> +)env:\r?\n(?<env>.*?)(?=^\k<envIndent>\S|\z)')
        $buildStep.Success | Should -BeTrue
        $envIndent = $buildStep.Groups['envIndent'].Value + '  '
        $buildStep.Groups['env'].Value | Should -Match (
            '(?m)^' + $envIndent + 'VSS_NUGET_EXTERNAL_FEED_ENDPOINTS: \$\(VSS_NUGET_EXTERNAL_FEED_ENDPOINTS\)\r?$'
        )
    }

    BeforeEach {
        $env:EARLY_ACCESS_FEED = 'net11'
        Mock New-NugetConfigFile -ModuleName build {}
        Mock Set-PipelineVariable -ModuleName build {}
    }

    AfterAll {
        $env:EARLY_ACCESS_FEED = $script:originalEarlyAccessFeed
    }

    It 'Enables module access independently with Source=<Source>' -TestCases @(
        @{ Source = 'Private'; NormalFeed = 'https://pkgs.dev.azure.com/powershell/PowerShell/_packaging/PowerShell/nuget/v3/index.json' }
        @{ Source = 'EarlyAccess'; NormalFeed = 'https://pkgs.dev.azure.com/powershell/PowerShell/_packaging/PowerShell/nuget/v3/index.json' }
        @{ Source = 'Public'; NormalFeed = 'https://www.powershellgallery.com/api/v2/' }
        @{ Source = 'NuGetOnly'; NormalFeed = 'https://www.powershellgallery.com/api/v2/' }
    ) {
        param($Source, $NormalFeed)

        if ($Source -ne 'EarlyAccess') {
            $env:EARLY_ACCESS_FEED = ''
        }
        Switch-PSNugetConfig -Source $Source -UseEarlyAccessModules -UserName 'test-user' -ClearTextPAT 'test-pat'

        Should -Invoke New-NugetConfigFile -ModuleName build -Times 3 -Exactly
        Should -Invoke New-NugetConfigFile -ModuleName build -Times 1 -Exactly -ParameterFilter {
            $Destination -like '*/src/Modules/' -and
            $NugetPackageSource.Count -eq 2 -and
            $NugetPackageSource[0].Url -eq $script:moduleFeed -and
            $NugetPackageSource[1].Url -eq $NormalFeed -and
            -not $UserName -and -not $ClearTextPAT
        }
        Should -Invoke New-NugetConfigFile -ModuleName build -Times 1 -Exactly -ParameterFilter {
            $Destination -like '*/test/tools/Modules/' -and
            $NugetPackageSource.Count -eq 1 -and
            $NugetPackageSource[0].Url -eq $NormalFeed -and
            $UserName -eq 'test-user' -and $ClearTextPAT -eq 'test-pat'
        }
    }

    It 'Does not enable internal module sources in <Source> mode' -TestCases @(
        @{ Source = 'Public' }
        @{ Source = 'Private' }
        @{ Source = 'NuGetOnly' }
        @{ Source = 'EarlyAccess' }
    ) {
        param($Source)

        Switch-PSNugetConfig -Source $Source

        Should -Invoke New-NugetConfigFile -ModuleName build -Times 3 -Exactly
        Should -Invoke New-NugetConfigFile -ModuleName build -Times 0 -Exactly -ParameterFilter {
            $script:moduleFeed -in $NugetPackageSource.Url
        }
    }

    It 'Rejects an unknown .NET early-access feed before writing configuration' {
        $env:EARLY_ACCESS_FEED = 'unknown'

        { Switch-PSNugetConfig -Source EarlyAccess } | Should -Throw '*Unknown early access feed URL*'
        Should -Invoke New-NugetConfigFile -ModuleName build -Times 0 -Exactly
    }
}
