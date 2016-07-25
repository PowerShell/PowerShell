Describe "FormatData" -tags "Feature" {

    Context "Export" {
        It "can export all types" {            
            try
            {                
                $expectAllFormat = Get-FormatData -typename *
                $expectAllFormat | Export-FormatData -path $TESTDRIVE\allformat.ps1xml -IncludeScriptBlock

                $sessionState = [System.Management.Automation.Runspaces.InitialSessionState]::CreateDefault()
                $sessionState.Formats.Clear()
                $sessionState.Types.Clear()

                $runspace = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspace($sessionState)
                $runspace.Open()

                $runspace.CreatePipeline("Update-FormatData -AppendPath $TESTDRIVE\allformat.ps1xml").Invoke()
                $actualAllFormat = $runspace.CreatePipeline("Get-FormatData -TypeName *").Invoke()

                $expectAllFormat.Count | Should Be $actualAllFormat.Count
                Compare-Object $expectAllFormat $actualAllFormat | Should Be $null
                $runspace.Close()
            }
            finally 
            {
                Remove-Item -Path $TESTDRIVE\allformat.ps1xml -Force -ErrorAction SilentlyContinue
            }
        }

        It "works with literal path" {
            $filename = 'TestDrive:\[formats.ps1xml'
            Get-FormatData -TypeName * | Export-FormatData -LiteralPath $filename
            (Test-Path -LiteralPath $filename) | Should Be $true
        }

        It "should overwrite the destination file" {
            $filename = 'TestDrive:\ExportFormatDataWithForce.ps1xml'
            $unexpected = "SHOULD BE OVERWRITTEN"
            $unexpected | Out-File -FilePath $filename -Force
            $file = Get-Item  $filename
            $file.IsReadOnly = $true
            Get-FormatData -TypeName * | Export-FormatData -Path $filename -Force

            $actual = @(Get-Content $filename)[0]
            $actual | Should Not Be $unexpected
        }

        It "should not overwrite the destination file with NoClobber" {    
            $filename = "TestDrive:\ExportFormatDataWithNoClobber.ps1xml"
            Get-FormatData -TypeName * | Export-FormatData -LiteralPath $filename

            try
            {
                Get-FormatData -TypeName * | Export-FormatData -LiteralPath $filename -NoClobber
            }
            catch
            {
                $exportFormatError = $_
            }

            $exportFormatError.FullyQualifiedErrorId | Should Be 'NoClobber,Microsoft.PowerShell.Commands.ExportFormatDataCommand'
        }
    }
}
