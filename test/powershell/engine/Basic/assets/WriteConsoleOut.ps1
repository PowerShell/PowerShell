# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [string]
    $Value,

    [Parameter(Mandatory)]
    [string]
    $Encoding
)

$enc = [System.Text.Encoding]::GetEncoding($Encoding)
$data = $enc.GetBytes($Value)

$outStream = [System.Console]::OpenStandardOutput()
try {
    $outStream.Write($data, 0, $data.Length)
} finally {
    $outStream.Dispose()
}
