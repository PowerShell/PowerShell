# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Get-Error" -Tags "CI" {
    It "Does not hang when serializing exception with array with type instances" {
        $ps = [PowerShell]::Create()
        $null = $ps.AddScript(@'
            class GetErrorWithTypeArray : Exception {
                [type[]]$Values
                [type]$Type

                GetErrorWithTypeArray ([string]$Message) : base($Message) {
                    $this.Values = [type[]]@([string], [int])
                    $this.Type = [bool]
                }
            }
            try { throw [GetErrorWithTypeArray]::new("") } catch {}
            Get-Error | Out-String
'@)

        $task = $ps.BeginInvoke()
        if (-not $task.AsyncWaitHandle.WaitOne(5000)) {
            $null = $ps.BeginStop($null, $null)
            throw "Timed out waiting for Get-Error to serialize"
        }

        $result = $ps.EndInvoke($task)
        $result.Count | Should -Be 1
        $result[0] | Should -BeOfType ([string])

        $formattedError = (@(
            $result[0] -split "\r?\n" | ForEach-Object {
                $_.TrimEnd()
            }
        ) -join ([Environment]::NewLine)).Trim()

        $formattedError | Should -Be @'
Exception        :
    Values  :
              [System.String]
              [System.Int32]
    Type    : [System.Boolean]
    HResult : -2146233088
CategoryInfo     : OperationStopped: (:) [], GetErrorWithTypeArray
InvocationInfo   :
    ScriptLineNumber : 10
    OffsetInLine     : 19
    HistoryId        : 1
    Line             : try { throw [GetErrorWithTypeArray]::new("") } catch {}

    Statement        : throw [GetErrorWithTypeArray]::new("")
    PositionMessage  : At line:10 char:19
                       +             try { throw [GetErrorWithTypeArray]::new("") } catch {}
                       +                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    CommandOrigin    : Internal
ScriptStackTrace : at <ScriptBlock>, <No file>: line 10
'@.Trim()
    }

    It "Formats strings and primitive types in an array" {
        $ps = [PowerShell]::Create()
        $null = $ps.AddScript(@'
            class GetErrorPrimitiveArray : Exception {
                [object[]]$Values

                GetErrorPrimitiveArray ([string]$Message) : base($Message) {
                    $this.Values = @(1, "alpha", 0.5)
                }
            }
            try { throw [GetErrorPrimitiveArray]::new("") } catch {}
            Get-Error | Out-String
'@)

        $result = $ps.Invoke()
        $result.Count | Should -Be 1
        $result[0] | Should -BeOfType ([string])

        $formattedError = (@(
            $result[0] -split "\r?\n" | ForEach-Object {
                $_.TrimEnd()
            }
        ) -join ([Environment]::NewLine)).Trim()

        $formattedError | Should -Be @'
Exception        :
    Type    : GetErrorPrimitiveArray
    Values  :
              1
              alpha
              0.5
    HResult : -2146233088
CategoryInfo     : OperationStopped: (:) [], GetErrorPrimitiveArray
InvocationInfo   :
    ScriptLineNumber : 8
    OffsetInLine     : 19
    HistoryId        : 1
    Line             : try { throw [GetErrorPrimitiveArray]::new("") } catch {}

    Statement        : throw [GetErrorPrimitiveArray]::new("")
    PositionMessage  : At line:8 char:19
                       +             try { throw [GetErrorPrimitiveArray]::new("") } catch {}
                       +                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    CommandOrigin    : Internal
ScriptStackTrace : at <ScriptBlock>, <No file>: line 8
'@.Trim()
    }
}
