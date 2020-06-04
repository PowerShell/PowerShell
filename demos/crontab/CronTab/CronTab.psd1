@{

# Script module or binary module file associated with this manifest.
RootModule = 'CronTab.psm1'

# Version number of this module.
ModuleVersion = '0.1.0.0'

# Supported PSEditions
CompatiblePSEditions = @('Core')

# ID used to uniquely identify this module
GUID = '508bb97f-de2e-482e-aae2-01caec0be8c7'

# Author of this module
Author = 'PowerShell'

# Company or vendor of this module
CompanyName = 'Microsoft Corporation'

# Copyright statement for this module
Copyright = 'Copyright (c) Microsoft Corporation.'

# Description of the functionality provided by this module
Description = 'Sample module for managing CronTab'

# Format files (.ps1xml) to be loaded when importing this module
FormatsToProcess = 'CronTab.ps1xml'

# Functions to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no functions to export.
FunctionsToExport = 'New-CronJob','Remove-CronJob','Get-CronJob','Get-CronTabUser'

# Private data to pass to the module specified in RootModule/ModuleToProcess. This may also contain a PSData hashtable with additional module metadata used by PowerShell.
PrivateData = @{

    PSData = @{

        # Tags applied to this module. These help with module discovery in online galleries.
        # Tags = @()

        # A URL to the license for this module.
        # LicenseUri = ''

        # A URL to the main website for this project.
        # ProjectUri = ''

        # A URL to an icon representing this module.
        # IconUri = ''

        # ReleaseNotes of this module
        # ReleaseNotes = ''

    } # End of PSData hashtable

} # End of PrivateData hashtable

# HelpInfo URI of this module
# HelpInfoURI = ''

}

