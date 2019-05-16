# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe 'Line Continuance' -Tags 'CI' {
    BeforeAll {
        function ExecuteCommand {
            param ([string]$command)
            [powershell]::Create().AddScript($command).Invoke()
        }

        $whitespace = "`t `f`v$([char]0x00a0)$([char]0x0085)"

        $implicitContinuanceWithNamedParametersEnabled = $EnabledExperimentalFeatures.Contains('PSImplicitLineContinuanceForNamedParameters')
    }

    Context 'Lines ending with a backtick that parse and execute without error' {
        It 'Lines ending with a single backtick' {
            $script = @'
'Hello' + `
    ' world'
'@
            ExecuteCommand $script | Should -BeExactly 'Hello world'
        }

        It 'Lines ending with a single backtick followed only by a CR (old-style Mac line ending)' {
            $script = "'Hello' + ```r' world'"
            ExecuteCommand $script | Should -Be 'Hello world'
        }

        It 'Lines ending with a single backtick followed by whitespace' {
            $script = @'
# The first line of this command ends with trailing whitespace
'Hello' + `
'@ + $whitespace + @'

    ' world'
'@
            ExecuteCommand $script | Should -Be 'Hello world'
        }

        It 'Lines ending with a single backtick followed by whitespace and a comment' {
            $script = @'
'Hello' + ` # You can place comments after whitespace following backticks
    ' world'
'@
            ExecuteCommand $script | Should -Be 'Hello world'
        }

        It 'Lines ending with a single backtick followed by a comment line and then the continued line' {
            $script = @'
'Hello' + `
# You can place comments in the middle of a continued line
    ' world'
'@
            ExecuteCommand $script | Should -Be 'Hello world'
        }
    }

    Context 'Lines ending with a backtick that do not parse' {
        It 'Lines ending with a single backtick followed immediately by a comment' {
            $script = @'
'Hello' + `# This is not a valid comment
    ' world'
'@
            $err = { ExecuteCommand $script } | Should -Throw -ErrorId 'ParseException' -PassThru
            $err.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -BeExactly 'ExpectedValueExpression'
        }

        It 'Lines ending with two backticks' {
            $script = @'
'Hello' + ``
    ' world'
'@
            $err = { ExecuteCommand $script } | Should -Throw -ErrorId 'ParseException' -PassThru
            $err.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -BeExactly 'ExpectedValueExpression'
        }
    }

    Context 'Lines ending with a pipe that parse and execute without error' {
        It 'Lines ending with a pipe' {
            $script = @'
'Hello' |
    ForEach-Object {"$_ world"}
'@
            ExecuteCommand $script | Should -Be 'Hello world'
        }

        It 'Lines ending with a pipe followed only by a CR (old-style Mac line ending)' {
            $script = "'Hello' |`r    ForEach-Object {`"`$_ world`"}"
            ExecuteCommand $script | Should -Be 'Hello world'
        }

        It 'Lines ending with a pipe followed by whitespace' {
            $script = @'
# The next line ends with trailing whitespace
'Hello' |
'@ + $whitespace + @'

    ForEach-Object {"$_ world"}
'@
            ExecuteCommand $script | Should -Be 'Hello world'
        }

        It 'Lines ending with a pipe followed by a comment' {
            $script = @'
'Hello' |# You can place comments after whitespace following pipes
    ForEach-Object {"$_ world"}
'@
            ExecuteCommand $script | Should -Be 'Hello world'
        }

        It 'Lines ending with a pipe followed by a comment line and then the continued command' {
            $script = @'
'Hello' |
# You can place comments in the middle of a continued pipeline
    ForEach-Object {"$_ world"}
'@
            ExecuteCommand $script | Should -Be 'Hello world'
        }
    }

    Context 'Lines ending with a pipe that do not parse' {
        It 'Lines ending with a single pipe followed by an empty line' {
            $script = @'
'Hello' |

'@
            $err = { ExecuteCommand $script } | Should -Throw -ErrorId 'ParseException' -PassThru
            $err.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -BeExactly 'EmptyPipeElement'
        }

        It 'Lines ending with a single pipe followed by a line that starts with a pipe' {
            $script = @'
'Hello' |
    |

'@
            $err = { ExecuteCommand $script } | Should -Throw -ErrorId 'ParseException' -PassThru
            $err.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -BeExactly 'EmptyPipeElement'
        }
    }

    Context 'Parsing and executing without error while using a pipe at the beginning of a line to continue the previous line' {
        It 'Line continuance using a pipe at the start of a subsequent line' {
            $script = @'
'Hello'
    | ForEach-Object {"$_ world"}
'@
            ExecuteCommand $script | Should -BeExactly 'Hello world'
        }

        It 'Line continuance using a pipe at the start of a subsequent line after a CR (old-style Mac line ending)' {
            $script = "'Hello'`r    | ForEach-Object {`"`$_ world`"}"
            ExecuteCommand $script | Should -BeExactly 'Hello world'
        }

        It 'Longer line continuance using pipes at the start of subsequent lines' {
            $script = @'
1..10
    | ForEach-Object {($_ -shl 1) + $_}
    | Where-Object {$_ % 2 -eq 0}
    | Sort-Object -Descending
'@
            ExecuteCommand $script | Should -Be @(30, 24, 18, 12, 6)
        }

        It 'Line continuance using a comment line followed by a pipe at the start of a subsequent line' {
            $script = @'
'Hello'
    # You can place comments before continued pipelines
    | ForEach-Object {"$_ world"}
'@
            ExecuteCommand $script | Should -BeExactly 'Hello world'
        }

        It 'Longer line continuance using pipes at the start of subsequent lines (with comments)' {
            $script = @'
1..10
    # Times three
    | ForEach-Object {($_ -shl 1) + $_}
    # Even only
    | Where-Object {$_ % 2 -eq 0}
    # Reverse order
    | Sort-Object -Descending
'@
            ExecuteCommand $script | Should -Be @(30, 24, 18, 12, 6)
        }

        It 'Line continuance using a pipe on a line by itself' {
            $script = @'
'Hello'
    |
    ForEach-Object {"$_ world"}
'@
            ExecuteCommand $script | Should -BeExactly 'Hello world'
        }


        It 'Longer line continuance using pipes on lines by themselves' {
            $script = @'
1..10
    |
    ForEach-Object {($_ -shl 1) + $_}
    |
    Where-Object {$_ % 2 -eq 0}
    |
    Sort-Object -Descending
'@
            ExecuteCommand $script | Should -Be @(30, 24, 18, 12, 6)
        }

        It 'Line continuance using a pipe on a line by itself (with comments)' {
            $script = @'
'Hello'
    |
    # You can place comments before continued pipelines
    ForEach-Object {"$_ world"}
'@
            ExecuteCommand $script | Should -BeExactly 'Hello world'
        }

        It 'Longer line continuance using pipes on lines by themselves (with comments)' {
            $script = @'
1..10
    |
    # Times three
    ForEach-Object {($_ -shl 1) + $_}
    |
    # Even only
    Where-Object {$_ % 2 -eq 0}
    |
    # Reverse order
    Sort-Object -Descending
'@
            ExecuteCommand $script | Should -Be @(30, 24, 18, 12, 6)
        }
    }

    Context 'Lines starting with a pipe that do not parse' {
        It 'Lines starting with a single pipe with nothing after it' {
            $script = @'
'Hello'
    | # Nothing to see here, move along

'@
            $err = { ExecuteCommand $script } | Should -Throw -ErrorId 'ParseException' -PassThru
            $err.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -BeExactly 'EmptyPipeElement'
        }

        It 'Lines starting with a single pipe with blank lines after it' {
            $script = @'
'Hello'
    |


'@
            $err = { ExecuteCommand $script } | Should -Throw -ErrorId 'ParseException' -PassThru
            $err.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -BeExactly 'EmptyPipeElement'
        }

        It 'Lines starting with a single pipe that have a blank line before it' {
            $script = @'
'Hello'

    | ForEach-Object {"$_ world"}

'@
            $err = { ExecuteCommand $script } | Should -Throw -ErrorId 'ParseException' -PassThru
            $err.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -BeExactly 'EmptyPipeElement'
        }

        It 'Lines starting with a single pipe that have a line with whitespace before it' {
            $script = @"
'Hello'
$whitespace
    | ForEach-Object {"`$_ world"}
"@
        $err = { ExecuteCommand $script } | Should -Throw -ErrorId 'ParseException' -PassThru
            $err.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -BeExactly 'EmptyPipeElement'
        }

        It 'Lines starting with a single pipe that have a line with nothing but a backtick before it' {
            $script = @'
'Hello'
    `
    | ForEach-Object {"$_ world"}

'@
            $err = { ExecuteCommand $script } | Should -Throw -ErrorId 'ParseException' -PassThru
            $err.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -BeExactly 'EmptyPipeElement'
        }

    }

    Context 'Parsing and executing without error while using a named parameter at the beginning of a line to continue the previous line' {
        BeforeAll {
            if (!$implicitContinuanceWithNamedParametersEnabled) {
                Write-Verbose 'Tests skipped. This set of tests requires the experimental feature ''PSImplicitLineContinuanceForNamedParameters'' to be enabled.' -Verbose
                $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
                $PSDefaultParameterValues["it:skip"] = $true
                return
            }
        }

        AfterAll {
            if (!$implicitContinuanceWithNamedParametersEnabled) {
                $global:PSDefaultParameterValues = $originalDefaultParameterValues
                return
            }
        }

        It 'Line continuance using named parameters at the start of subsequent lines' {
            $script = @'
Get-Date
    -Year 2019
    -Month 5
    -Day 15
    -Hour 11
    -Minute 22
    -Second 15
    -Millisecond 0
'@
            ExecuteCommand $script | Should -Be ([DateTime]'2019-05-15T11:22:15')
        }

        It 'Line continuance using named parameters at the start of subsequent lines after a CR (old-style Mac line ending)' {
            $script = "Get-Date`r    -Year 2019`r    -Month 5`r    -Day 15`r    -Hour 11`r    -Minute 22`r    -Second 15`r    -Millisecond 0"
            ExecuteCommand $script | Should -BeExactly ([DateTime]'2019-05-15T11:22:15')
        }

        It 'Line continuance using named parameters and pipes at the start of subsequent lines' {
            $script = @'
Get-Process
    -Id $PID
    | ForEach-Object Name
'@
            ExecuteCommand $script | Should -BeExactly 'pwsh'
        }

        It 'Line continuance using a comment line followed by named parameter at the start of a subsequent line' {
            $script = @'
Get-Process
    # You can place comments before named parameters
    -Id $pid
'@
            (ExecuteCommand $script).Id | Should -Be $PID
        }

        It 'Longer line continuance using named parameters at the start of subsequent lines (with comments)' {
            $script = @'
Get-Command
    # The command type
    -CommandType Cmdlet
    # The command name
    -Name Get-Date
    # Show verbose output
    -Verbose
'@
            (ExecuteCommand $script).Name | Should -BeExactly 'Get-Date'
        }

        It 'Hiding a command with line continuance using named parameters at the start of subsequent lines' {
            $script = @'
function -Syntax {42}
Get-Command
    -Name Get-Date
    -Syntax
'@
            @(ExecuteCommand $script)[-1] | Should -BeOfType string
        }

        It 'Invoking a command hidden by line continuance using named parameters' {
            $script = @'
function -Syntax {42}
Get-Command
    -Name Get-Date
  & -Syntax
'@
            @(ExecuteCommand $script)[-1] | Should -BeOfType int
        }
    }

    Context 'Lines starting with a named parameter that fail because they are used incorrectly' {
        BeforeAll {
            if (!$implicitContinuanceWithNamedParametersEnabled) {
                Write-Verbose 'Tests skipped. This set of tests requires the experimental feature ''PSImplicitLineContinuanceForNamedParameters'' to be enabled.' -Verbose
                $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
                $PSDefaultParameterValues["it:skip"] = $true
                return
            }
        }

        AfterAll {
            if (!$implicitContinuanceWithNamedParametersEnabled) {
                $global:PSDefaultParameterValues = $originalDefaultParameterValues
                return
            }
        }

        It 'Lines starting with a named parameter that have the value associated with that parameter on a subsequent line' {
            $script = @'
try {
    Get-Process
        -Id
        $PID
} catch {
    throw
}
'@
            $err = { ExecuteCommand $script } | Should -Throw -ErrorId 'ParameterBindingException' -PassThru
            $err.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -BeExactly 'MissingArgument,Microsoft.PowerShell.Commands.GetProcessCommand'
        }

        It 'Lines starting with a named parameter that have a line with whitespace before it' {
            $script = @"
try {
    Get-Date
    $whitespace
        -Year 2019
} catch {
    throw
}
"@
            $err = { ExecuteCommand $script } | Should -Throw -ErrorId 'CommandNotFoundException' -PassThru
        }

        It 'Lines starting with a named parameter that have a line with nothing but a backtick before it' {
            $script = @'
try {
    Get-Date
        `
        -Year 2019
} catch {
    throw
}
'@
            $err = { ExecuteCommand $script } | Should -Throw -ErrorId 'CommandNotFoundException' -PassThru
        }

    }

    Context 'Parsing and executing without error while using splatting at the beginning of a line to continue the previous line' {
        BeforeAll {
            if (!$implicitContinuanceWithNamedParametersEnabled) {
                Write-Verbose 'Tests skipped. This set of tests requires the experimental feature ''PSImplicitLineContinuanceForNamedParameters'' to be enabled.' -Verbose
                $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
                $PSDefaultParameterValues["it:skip"] = $true
                return
            }
        }

        AfterAll {
            if (!$implicitContinuanceWithNamedParametersEnabled) {
                $global:PSDefaultParameterValues = $originalDefaultParameterValues
                return
            }
        }

        It 'Line continuance using splatting at the start a subsequent line' {
            $script = @'
$parameters = @{
    Year = 2019
    Month = 5
    Day = 15
    Hour = 11
    Minute = 22
    Second = 15
    Millisecond = 0
}
Get-Date
    @parameters
'@
            ExecuteCommand $script | Should -Be ([DateTime]'2019-05-15T11:22:15')
        }

        It 'Line continuance using splatting at the start of a subsequent line after a CR (old-style Mac line ending)' {
            $script = "`$parameters = @{`r    Year = 2019`r    Month = 5`r    Day = 15`r    Hour = 11`r    Minute = 22`r    Second = 15`r    Millisecond = 0`r}`rGet-Date`r    @parameters"
            ExecuteCommand $script | Should -BeExactly ([DateTime]'2019-05-15T11:22:15')
        }

        It 'Line continuance using named parameters, splatting and pipes at the start of subsequent lines' {
            $script = @'
$parameters = @{
    Month = 5
    Day = 15
    Hour = 11
    Minute = 22
    Second = 15
    Millisecond = 0
}
Get-Date
    -Year 2019
    @parameters
    | ForEach-Object Year
'@
            ExecuteCommand $script | Should -Be 2019
        }

        It 'Line continuance using a comment line followed by splatting at the start of a subsequent line' {
            $script = @'
$parameters = @{
    Id = $PID
}
Get-Process
    # You can place comments before splatting
    @parameters
'@
            (ExecuteCommand $script).Id | Should -Be $PID
        }
    }

    Context 'Lines starting with splatting that do not parse' {
        BeforeAll {
            if (!$implicitContinuanceWithNamedParametersEnabled) {
                Write-Verbose 'Tests skipped. This set of tests requires the experimental feature ''PSImplicitLineContinuanceForNamedParameters'' to be enabled.' -Verbose
                $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
                $PSDefaultParameterValues["it:skip"] = $true
                return
            }
        }

        AfterAll {
            if (!$implicitContinuanceWithNamedParametersEnabled) {
                $global:PSDefaultParameterValues = $originalDefaultParameterValues
                return
            }
        }

        It 'Lines starting with splatting that have a line with whitespace before it' {
            $script = @"
try {
    $parameters = @{
        Year = 2019
    }
    Get-Date
    $whitespace
        @parameters
} catch {
    throw
}
"@
            $err = { ExecuteCommand $script } | Should -Throw -ErrorId 'ParseException' -PassThru
            $err.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -BeExactly 'SplattingNotPermitted'
        }

        It 'Lines starting with splatting that have a line with nothing but a backtick before it' {
            $script = @'
try {
    $parameters = @{
        Year = 2019
    }
    Get-Date
        `
        @parameters
} catch {
    throw
}
'@
            $err = { ExecuteCommand $script } | Should -Throw -ErrorId 'ParseException' -PassThru
            $err.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -BeExactly 'SplattingNotPermitted'
        }

    }
}
