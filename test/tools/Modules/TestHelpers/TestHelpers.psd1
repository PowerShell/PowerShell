#
# Module manifest for module 'TestHelpers'
#

@{

# Script module or binary module file associated with this manifest.
RootModule = 'TestHelpers.psm1'

# Version number of this module.
ModuleVersion = '1.0'

# ID used to uniquely identify this module
GUID = 'cc1c8e94-51d1-4bc1-b508-62bc09f02f54'

# Company or vendor of this module
CompanyName = 'Microsoft Corporation'

# Copyright statement for this module
Copyright = 'Copyright (C) Microsoft Corporation, All rights reserved.'

# Description of the functionality provided by this module
Description = 'Temporary module contains functions for using in tests'

# Functions to export from this module
FunctionsToExport = 'Wait-UntilTrue', 'Test-IsElevated', 'ShouldBeErrorId'

# Cmdlets to export from this module
#CmdletsToExport = '*'

# Variables to export from this module
#VariablesToExport = '*'

# Aliases to export from this module
#AliasesToExport = '*'

}
