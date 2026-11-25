# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Import-Module HelpersRemoting
Import-Module HelpersCommon

Describe "Remote runspace pool should expose commands in endpoint configuration" -Tags 'Feature','RequireAdminOnWindows' {

    BeforeAll {

        $pendingTest = (Test-IsWinWow64)
        $skipTest = !$IsWindows -or !(Test-CanWriteToPsHome)

        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()

        if ($pendingTest) {
            $PSDefaultParameterValues["it:pending"] = $true
            return
        }

        if ($skipTest) {
            $PSDefaultParameterValues["it:skip"] = $true
            return
        }

        $configName = "restrictedV"
        $configPath = Join-Path $TestDrive ($configName + ".pssc")

        New-PSSessionConfigurationFile -Path $configPath -SessionType RestrictedRemoteServer -VisibleCmdlets 'Get-CimInstance'

        $null = Register-PSSessionConfiguration -Name $configName -Path $configPath -Force -ErrorAction SilentlyContinue

        $remoteRunspacePool = New-RemoteRunspacePool -ConfigurationName $configName
    }

    AfterAll {

        if ($pendingTest -or $skipTest) {
            $global:PSDefaultParameterValues = $originalDefaultParameterValues
            return
        }

        if ($remoteRunspacePool -ne $null)
        {
            $remoteRunspacePool.Dispose()
        }

        Unregister-PSSessionConfiguration -Name $configName -Force -ErrorAction SilentlyContinue
    }

    It "Verifies that the configured endpoint cmdlet is available in all runspace pool instances" -Skip:(!$IsWindows -or !(Test-CanWriteToPsHome)) {

        [powershell] $ps1 = [powershell]::Create()
        $ps1.RunspacePool = $remoteRunspacePool
        $null = $ps1.AddCommand('Get-Command').AddParameter('Name','Get-CimInstance')

        [powershell] $ps2 = [powershell]::Create()
        $ps2.RunspacePool = $remoteRunspacePool
        $null = $ps2.AddCommand('Get-Command').AddParameter('Name','Get-CimInstance')

        [powershell] $ps3 = [powershell]::Create()
        $ps3.RunspacePool = $remoteRunspacePool
        $null = $ps3.AddCommand('Get-Command').AddParameter('Name','Get-CimInstance')

        [powershell] $ps4 = [powershell]::Create()
        $ps4.RunspacePool = $remoteRunspacePool
        $null = $ps4.AddCommand('Get-Command').AddParameter('Name','Get-CimInstance')

        # Invoke all four simultaneously
        $a1 = $ps1.BeginInvoke()
        $a2 = $ps2.BeginInvoke()
        $a3 = $ps3.BeginInvoke()
        $a4 = $ps4.BeginInvoke()

        # Wait for completion
        $r1 = $ps1.EndInvoke($a1)
        $r2 = $ps2.EndInvoke($a2)
        $r3 = $ps3.EndInvoke($a3)
        $r4 = $ps4.EndInvoke($a4)

        $r1.Name | Should -BeExactly 'Get-CimInstance'
        $r2.Name | Should -BeExactly 'Get-CimInstance'
        $r3.Name | Should -BeExactly 'Get-CimInstance'
        $r4.Name | Should -BeExactly 'Get-CimInstance'
    }
}

Describe "Local runspace pool should load module type and format data in every runspace" -Tags 'CI' {

    BeforeAll {
        $moduleName = 'RunspacePoolTypeDataTest'
        $typeName = 'RunspacePoolTypeDataTest.Item'
        $moduleDir = Join-Path $TestDrive $moduleName
        $null = New-Item -ItemType Directory -Path $moduleDir -Force

        Set-Content -Path (Join-Path $moduleDir "$moduleName.types.ps1xml") -Value @"
<Types>
  <Type>
    <Name>$typeName</Name>
    <Members>
      <ScriptProperty>
        <Name>Doubled</Name>
        <GetScriptBlock>`$this.Value * 2</GetScriptBlock>
      </ScriptProperty>
    </Members>
  </Type>
</Types>
"@
        Set-Content -Path (Join-Path $moduleDir "$moduleName.format.ps1xml") -Value @"
<Configuration>
  <ViewDefinitions>
    <View>
      <Name>$typeName</Name>
      <ViewSelectedBy>
        <TypeName>$typeName</TypeName>
      </ViewSelectedBy>
      <ListControl>
        <ListEntries>
          <ListEntry>
            <ListItems>
              <ListItem>
                <PropertyName>Value</PropertyName>
              </ListItem>
            </ListItems>
          </ListEntry>
        </ListEntries>
      </ListControl>
    </View>
  </ViewDefinitions>
</Configuration>
"@
        Set-Content -Path (Join-Path $moduleDir "$moduleName.psm1") -Value "function Get-TestItem { [pscustomobject]@{ PSTypeName = '$typeName'; Value = 21 } }"

        $manifestPath = Join-Path $moduleDir "$moduleName.psd1"
        New-ModuleManifest -Path $manifestPath -RootModule "$moduleName.psm1" -FunctionsToExport 'Get-TestItem' `
            -TypesToProcess "$moduleName.types.ps1xml" -FormatsToProcess "$moduleName.format.ps1xml"

        # Imports the module and reports whether its type and format data are available in the current runspace.
        # The 'First' instance waits until the 'Second' instance is running (so its runspace has already been opened)
        # before importing the module; the 'Second' instance imports the module only after the 'First' one has.
        $script = {
            param ($Role, $ManifestPath, $TypeName, $SecondRunspaceOpened, $FirstImportDone)

            if ($Role -eq 'First') {
                if (-not $SecondRunspaceOpened.Wait(60000)) { throw 'Timed out waiting for the second runspace.' }
            }
            else {
                $SecondRunspaceOpened.Set()
                if (-not $FirstImportDone.Wait(60000)) { throw 'Timed out waiting for the first import.' }
            }

            try {
                Import-Module $ManifestPath -ErrorAction Stop
            }
            finally {
                if ($Role -eq 'First') { $FirstImportDone.Set() }
            }

            [pscustomobject]@{
                Role = $Role
                RunspaceId = [runspace]::DefaultRunspace.InstanceId
                Doubled = (Get-TestItem).Doubled
                HasTypeData = $null -ne (Get-TypeData -TypeName $TypeName)
                HasFormatData = $null -ne (Get-FormatData -TypeName $TypeName)
            }
        }
    }

    It "Loads the data in a runspace that was opened before another runspace in the pool imported the module" {
        $secondRunspaceOpened = [System.Threading.ManualResetEventSlim]::new($false)
        $firstImportDone = [System.Threading.ManualResetEventSlim]::new($false)
        $pool = [runspacefactory]::CreateRunspacePool(1, 2)
        $invocations = @()

        try {
            $pool.Open()

            $invocations = foreach ($role in 'First', 'Second') {
                $ps = [powershell]::Create()
                $ps.RunspacePool = $pool
                $null = $ps.AddScript($script).
                    AddArgument($role).
                    AddArgument($manifestPath).
                    AddArgument($typeName).
                    AddArgument($secondRunspaceOpened).
                    AddArgument($firstImportDone)
                [pscustomobject]@{ PowerShell = $ps; AsyncResult = $ps.BeginInvoke() }
            }

            $results = foreach ($invocation in $invocations) {
                $invocation.PowerShell.EndInvoke($invocation.AsyncResult)
            }
        }
        finally {
            $invocations | ForEach-Object { $_.PowerShell.Dispose() }
            $pool.Dispose()
            $secondRunspaceOpened.Dispose()
            $firstImportDone.Dispose()
        }

        $results | Should -HaveCount 2
        $results[0].RunspaceId | Should -Not -Be $results[1].RunspaceId

        foreach ($result in $results) {
            $result.Doubled | Should -Be 42 -Because "the type data should be loaded in the '$($result.Role)' runspace"
            $result.HasTypeData | Should -BeTrue -Because "the type data should be loaded in the '$($result.Role)' runspace"
            $result.HasFormatData | Should -BeTrue -Because "the format data should be loaded in the '$($result.Role)' runspace"
        }
    }
}
