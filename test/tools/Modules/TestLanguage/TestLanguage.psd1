#
# Module manifest for module 'TestLanguage'
#

@{

# Script module or binary module file associated with this manifest.
RootModule = 'TestLanguage.psm1'

# Version number of this module.
ModuleVersion = '1.0'

# ID used to uniquely identify this module
GUID = 'a575af5e-2bd1-427f-b966-48640788896b'

# Company or vendor of this module
CompanyName = 'Microsoft Corporation'

# Copyright statement for this module
Copyright = 'Copyright (C) Microsoft Corporation, All rights reserved.'

# Description of the functionality provided by this module
Description = 'Temporary module for language tests'

# Functions to export from this module
FunctionsToExport = 'Get-ParseResults', 'Get-RuntimeError', 'ShouldBeParseError',
                    'Test-ErrorStmt', 'Test-Ast', 'Test-ErrorStmtForSwitchFlag'

# Cmdlets to export from this module
#CmdletsToExport = '*'

# Variables to export from this module
#VariablesToExport = '*'

# Aliases to export from this module
#AliasesToExport = '*'

}
