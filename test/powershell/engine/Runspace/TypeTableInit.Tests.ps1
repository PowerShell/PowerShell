
using namespace System.Management.Automation.Internal
using namespace System.Management.Automation.Runspaces

#
# This test creates a runspace 2 ways -
# - Loading the typetable from the ps1xml files
# - Loading typedata from C# that was generated from those ps1xml files
#
# It then runs the script DumpTypeData.ps1 to get all the type data as strings - this makes comparisons easy.
Describe "Generated TypeData" {

    It "Compare PS1XML w/ generated typedata - ISS" {
        [InternalTestHooks]::SetTestHook("ReadEngineTypesXmlFiles", $true)

        $iss = [initialsessionstate]::CreateDefault()
        $iss.Formats.Clear()
        $iss.Types.Count | Should Be 3
        $iss.Types[0].FileName | Should Be "$PSHOME\GetEvent.types.ps1xml"
        $iss.Types[1].FileName | Should Be "$PSHOME\types.ps1xml"
        $iss.Types[2].FileName | Should Be "$PSHOME\typesv3.ps1xml"

        $ps = [PowerShell]::Create($iss)
        $null = $ps.AddCommand("$PSScriptRoot\DumpTypeData.ps1")
        $fromPS1XML = $ps.Invoke()

        [InternalTestHooks]::SetTestHook("ReadEngineTypesXmlFiles", $false)

        $iss = [initialsessionstate]::CreateDefault()
        $iss.Formats.Clear()
        $iss.Types.Count | Should Be 3

        $ps = [PowerShell]::Create($iss)
        $null = $ps.AddCommand("$PSScriptRoot\DumpTypeData.ps1")
        $fromTypeData = $ps.Invoke()

        $fromTypeData | Should Be $fromPS1XML
    }

    It "Compare PS1XML w/ generated typedata - RunspaceConfig" {
        [InternalTestHooks]::SetTestHook("ReadEngineTypesXmlFiles", $true)

        $rsc = [RunspaceConfiguration]::Create()
        $rsc.Formats.Reset()
        $rsc.Types.Count | Should Be 3
        $rsc.Types[0].FileName | Should Be "$PSHOME\GetEvent.types.ps1xml"
        $rsc.Types[1].FileName | Should Be "$PSHOME\types.ps1xml"
        $rsc.Types[2].FileName | Should Be "$PSHOME\typesv3.ps1xml"

        $rs = [runspacefactory]::CreateRunspace($rsc)
        $ps = [powershell]::Create()
        $ps.Runspace = $rs
        $rs.Open()

        $null = $ps.AddCommand("$PSScriptRoot\DumpTypeData.ps1")
        $fromPS1XML = $ps.Invoke()

        [InternalTestHooks]::SetTestHook("ReadEngineTypesXmlFiles", $false)
        $rsc = [RunspaceConfiguration]::Create()
        $rsc.Formats.Reset()
        $rsc.Types.Count | Should Be 3
        $rsc.Types[0].FileName | Should Be "$PSHOME\GetEvent.types.ps1xml"
        $rsc.Types[1].FileName | Should Be "$PSHOME\types.ps1xml"
        $rsc.Types[2].FileName | Should Be "$PSHOME\typesv3.ps1xml"

        $rs = [runspacefactory]::CreateRunspace($rsc)
        $ps = [powershell]::Create()
        $ps.Runspace = $rs
        $rs.Open()

        $null = $ps.AddCommand("$PSScriptRoot\DumpTypeData.ps1")
        $fromTypeData = $ps.Invoke()

        $fromTypeData | Should Be $fromPS1XML
    }

}

Describe "Dynamic Site Caching" {
    
    class RemoveTypeDataTestClass
    {
        [string]$Data = 'from class'
    }
    $instance = [RemoveTypeDataTestClass]::new()

    # Use ProviderPath instead of TestDrive b/c we need the path for other runspaces which won't have the TestDrive.
    $removePs1XmlFileName = "$((Get-PSDrive TestDrive).Root)\$([Guid]::NewGuid()).types.ps1xml"
    Set-Content -Path $removePs1XmlFileName -Value @"
<Types>
  <Type>
    <Name>$([System.Security.SecurityElement]::Escape($instance.GetType().FullName))</Name>
    <Members>
      <NoteProperty>
        <Name>Data</Name>
        <Value>from note property</Value>
      </NoteProperty>
    </Members>
  </Type>
</Types>
"@

    $typeData = [TypeData]::new([RemoveTypeDataTestClass])
    $typeData.Members.Add("Data", [NotePropertyData]::new("Data", "from note property"))

    It "Dynamic sites don't cache after Remove-TypeData - TypeData+Initial Runspace" {
        [InternalTestHooks]::SetTestHook("ReadEngineTypesXmlFiles", $false)

        $instance.Data | Should Be 'from class'
        Update-TypeData -TypeData $typeData
        $instance.Data | Should Be 'from note property'
        Remove-TypeData -TypeData $typeData
        $instance.Data | Should Be 'from class'
    }

    It "Dynamic sites don't cache after Remove-TypeData - ps1xml+Initial Runspace" {
        [InternalTestHooks]::SetTestHook("ReadEngineTypesXmlFiles", $false)

        $instance.Data | Should Be 'from class'
        Update-TypeData -AppendPath $removePs1XmlFileName
        $instance.Data | Should Be 'from note property'
        Remove-TypeData -Path $removePs1XmlFileName
        $instance.Data | Should Be 'from class'
    }

    It "Dynamic sites don't cache after Remove-TypeData - TypeData+ISS" {
        [InternalTestHooks]::SetTestHook("ReadEngineTypesXmlFiles", $false)

        $instance.Data | Should Be 'from class'

        $iss = [initialsessionstate]::CreateDefault()
        $iss.Formats.Clear()

        try
        {
            $ps = [PowerShell]::Create($iss)
            $null = $ps.AddCommand("Update-TypeData").AddParameter("TypeData", $typeData).Invoke()
            $ps.Commands.Clear()
            $ps.AddScript('param($a) $a.Data').AddArgument($instance).Invoke() | Should Be 'from note property'
            $ps.Commands.Clear()
            $null = $ps.AddCommand("Remove-TypeData").AddParameter("TypeData", $typeData).Invoke()
            $ps.AddScript('param($a) $a.Data').AddArgument($instance).Invoke() | Should Be 'from class'
        }
        finally
        {
            $ps.Dispose()
        }
    }

    It "Dynamic sites don't cache after Remove-TypeData - TypeData+RunspaceConfiguration" {
        [InternalTestHooks]::SetTestHook("ReadEngineTypesXmlFiles", $false)

        $instance.Data | Should Be 'from class'

        try
        {
            $rsc = [RunspaceConfiguration]::Create()
            $rsc.Formats.Reset()
            $rs = [runspacefactory]::CreateRunspace($rsc)
            $ps = [powershell]::Create()
            $ps.Runspace = $rs
            $rs.Open()

            $null = $ps.AddCommand("Update-TypeData").AddParameter("TypeData", $typeData).Invoke()
            $ps.Commands.Clear()
            $ps.AddScript('param($a) $a.Data').AddArgument($instance).Invoke() | Should Be 'from note property'
            $ps.Commands.Clear()
            $null = $ps.AddCommand("Remove-TypeData").AddParameter("TypeData", $typeData).Invoke()
            $ps.AddScript('param($a) $a.Data').AddArgument($instance).Invoke() | Should Be 'from class'
        }
        finally
        {
            $rs.Close()
            $ps.Dispose()
        }
    }

    It "Dynamic sites don't cache after Remove-TypeData - ps1xml+ISS" {
        [InternalTestHooks]::SetTestHook("ReadEngineTypesXmlFiles", $false)

        $instance.Data | Should Be 'from class'

        $iss = [initialsessionstate]::CreateDefault()
        $iss.Formats.Clear()

        try
        {
            $ps = [PowerShell]::Create($iss)
            $null = $ps.AddCommand("Update-TypeData").AddParameter("AppendPath", $removePs1XmlFileName).Invoke()
            $ps.Commands.Clear()
            $ps.AddScript('param($a) $a.Data').AddArgument($instance).Invoke() | Should Be 'from note property'
            $ps.Commands.Clear()
            $null = $ps.AddCommand("Remove-TypeData").AddParameter("Path", $removePs1XmlFileName).Invoke()
            $ps.AddScript('param($a) $a.Data').AddArgument($instance).Invoke() | Should Be 'from class'
        }
        finally
        {
            $ps.Dispose()
        }
    }

    It "Dynamic sites don't cache after Remove-TypeData - ps1xml+RunspaceConfiguration" {
        [InternalTestHooks]::SetTestHook("ReadEngineTypesXmlFiles", $false)

        $instance.Data | Should Be 'from class'

        try
        {
            $rsc = [RunspaceConfiguration]::Create()
            $rsc.Formats.Reset()
            $rs = [runspacefactory]::CreateRunspace($rsc)
            $ps = [powershell]::Create()
            $ps.Runspace = $rs
            $rs.Open()

            $null = $ps.AddCommand("Update-TypeData").AddParameter("AppendPath", $removePs1XmlFileName).Invoke()
            $ps.Commands.Clear()
            $ps.AddScript('param($a) $a.Data').AddArgument($instance).Invoke() | Should Be 'from note property'
            $ps.Commands.Clear()
            $null = $ps.AddCommand("Remove-TypeData").AddParameter("Path", $removePs1XmlFileName).Invoke()
            $ps.AddScript('param($a) $a.Data').AddArgument($instance).Invoke() | Should Be 'from class'
        }
        finally
        {
            $rs.Close()
            $ps.Dispose()
        }
    }

}
