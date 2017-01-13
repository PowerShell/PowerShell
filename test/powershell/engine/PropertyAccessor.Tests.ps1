#
# Functional tests to verify basic conditions for IO to the PowerShellProperties.json files
# The properties files are supported on non-Windows OSes, but the tests are specific to
# Windows so that file IO can be verified using supported cmdlets.
#

try {
    # Skip these tests when run against "InBox" PowerShell
    $IsInbox = $PSHOME.EndsWith('\WindowsPowerShell\v1.0', [System.StringComparison]::OrdinalIgnoreCase)
    $productName = "PowerShell"

    #skip all tests on non-windows platform
    $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
    $IsNotSkipped = ($IsWindows -and !$IsInbox) # Only execute for PowerShell Core on Windows
    $PSDefaultParameterValues["it:skip"] = !$IsNotSkipped

    Describe "User-Specific PowerShellProperties.json Modifications" -Tags "CI" {

        BeforeAll {
            if ($IsNotSkipped) {
                # Discover the user-specific PowerShellProperties.json file
                $userSettingsDir = [System.IO.Path]::Combine($env:USERPROFILE, "Documents", $productName)
                $userPropertiesFile = Join-Path $userSettingsDir "PowerShellProperties.json"

                # Save the file for restoration after the tests are complete
                $backupPropertiesFile = ""
                if (Test-Path $userPropertiesFile) {
                    $backupPropertiesFile = Join-Path $userSettingsDir "ORIGINAL_PowerShellProperties.json"
                    Copy-Item -Path $userPropertiesFile -Destination $backupPropertiesFile -Force -ErrorAction Continue
                }
                elseif (-not (Test-Path $userSettingsDir)) {
                    # create the directory if it does not already exist
                    $null = New-Item -Type Directory -Path $userSettingsDir -Force -ErrorAction SilentlyContinue
                }

                # Save the original Process ExecutionPolicy. The tests assume that it is Undefined
                $processExecutionPolicy = Get-ExecutionPolicy -Scope Process
                Set-ExecutionPolicy -Scope Process -ExecutionPolicy Undefined
            }
        }

        BeforeEach {
            if ($IsNotSkipped) {
                Set-Content -Path $userPropertiesFile -Value '{"Microsoft.PowerShell:ExecutionPolicy":"RemoteSigned"}'
            }
        }

        AfterAll {
            if ($IsNotSkipped) {
                if (-not $backupPropertiesFile)
                {
                    # Remove PowerShellProperties.json if it did not exist before the tests
                    Remove-Item -Path $userPropertiesFile -Force -ErrorAction SilentlyContinue
                }
                else
                {
                    # Restore the original PowerShellProperties.json file if it existed before the test pass.
                    Move-Item -Path $backupPropertiesFile -Destination $userPropertiesFile -Force -ErrorAction Continue
                }

                # Restore the original Process ExecutionPolicy
                Set-ExecutionPolicy -Scope Process -ExecutionPolicy $processExecutionPolicy
            }
        }

        It "Verify Queries to Missing File Return Default Value" {
            Remove-Item $userPropertiesFile -Force

            Get-ExecutionPolicy -Scope CurrentUser | Should Be "Undefined"

            # Verify the file was not created during the test
            try {
                $propFile = Get-Item $userPropertiesFile -ErrorAction Stop
                throw "Properties file genererated during read operation"
            }
            catch {
                $_.FullyQualifiedErrorId | Should Be "PathNotFound,Microsoft.PowerShell.Commands.GetItemCommand"
            }
        }

        It "Verify Queries for Non-Existant Properties Return Default Value" {
            # Create a valid file with no values
            Set-Content -Path $userPropertiesFile -Value "{}"

            Get-ExecutionPolicy -Scope CurrentUser | Should Be "Undefined"
        }

        It "Verify Writes Update Properties" {
            Get-Content -Path $userPropertiesFile | Should Be '{"Microsoft.PowerShell:ExecutionPolicy":"RemoteSigned"}'
            Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy Bypass
            Get-Content -Path $userPropertiesFile | Should Be '{"Microsoft.PowerShell:ExecutionPolicy":"Bypass"}'
        }

        It "Verify Writes Create the File if Not Present" {
            Remove-Item $userPropertiesFile -Force
            Test-Path $userPropertiesFile | Should Be $false
            Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy Bypass
            Get-Content -Path $userPropertiesFile | Should Be '{"Microsoft.PowerShell:ExecutionPolicy":"Bypass"}'
        }
    }
}
finally {
    $global:PSDefaultParameterValues = $originalDefaultParameterValues
}
