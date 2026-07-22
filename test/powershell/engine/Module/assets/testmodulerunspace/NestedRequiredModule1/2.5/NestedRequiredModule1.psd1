
@{

# Version number of this module.
ModuleVersion = '2.5'

# ID used to uniquely identify this module
GUID = '369f0ee4-4cda-4ac3-a5c5-08e7bbc06e1a'

# Author of this module
Author = 'PowerShell'

# Company or vendor of this module
CompanyName = 'Microsoft Corporation'

# Copyright statement for this module
Copyright = 'Copyright (c) Microsoft Corporation.'

# Description of the functionality provided by this module
Description = 'NestedRequiredModule1 module'

# Modules to import as nested modules of the module specified in RootModule/ModuleToProcess
NestedModules = @('NestedRequiredModule1.psm1')

# Functions to export from this module
FunctionsToExport = @()

# Cmdlets to export from this module
CmdletsToExport = @()

# Variables to export from this module
VariablesToExport = @()

# Aliases to export from this module
AliasesToExport = @()

# Private data to pass to the module specified in RootModule/ModuleToProcess. This may also contain a PSData hashtable with additional module metadata used by PowerShell.
PrivateData = @{

    PSData = @{

        # Tags applied to this module. These help with module discovery in online galleries.
        Tags = 'Tag1', 'Tag2', 'Tag-NestedRequiredModule1-2.5'

        # A URL to the license for this module.
        LicenseUri = 'http://nestedrequiredmodule1.com/license'

        # A URL to the main website for this project.
        ProjectUri = 'http://nestedrequiredmodule1.com/'

        # A URL to an icon representing this module.
        IconUri = 'http://nestedrequiredmodule1.com/icon'

        # ReleaseNotes of this module
        ReleaseNotes = 'NestedRequiredModule1 release notes'

    } # End of PSData hashtable

} # End of PrivateData hashtable

}
