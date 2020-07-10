# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Line Continuance' -Tags 'CI' {
    BeforeAll {
        function ExecuteCommand {
            param ([string]$command)
            [powershell]::Create().AddScript($command).Invoke()
        }

        $whitespace = "`t `f`v$([char]0x00a0)$([char]0x0085)"
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
}
