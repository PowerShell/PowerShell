# Tests related to TFS item 1370183 [PSUpgrade] FileInfo (from Get-Item, et al) VersionInfo returns misleading binary version info
# Connect request https://connect.microsoft.com/PowerShell/feedback/details/1027483/fileinfo-from-get-item-et-al-versioninfo-returns-misleading-binary-version-info

Describe "Tests for new script properties of System.Diagnostics.FileVersionInfo" {
    
    It "FileVersionRaw should match 'File*Part' properties" {
         $hostFileVersion = Get-Process -Id $pid -FileVersionInfo
         $partsString = $hostFileVersion.FileMajorPart, $hostFileVersion.FileMinorPart, $hostFileVersion.FileBuildPart, $hostFileVersion.FilePrivatePart -join "."
         $hostFileVersion.FileVersionRaw -eq $partsString | Should Be $true
    }

    It "ProductVersionRaw should match 'Product*Part' properties" {
         $hostFileVersion = Get-Process -Id $pid -FileVersionInfo
         $partsString = $hostFileVersion.ProductMajorPart, $hostFileVersion.ProductMinorPart, $hostFileVersion.ProductBuildPart, $hostFileVersion.ProductPrivatePart -join "."
         $hostFileVersion.ProductVersionRaw -eq $partsString | Should Be $true
    }
}