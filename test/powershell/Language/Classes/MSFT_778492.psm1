# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

$foo = 'MSFT_778492 script scope'

class MSFT_778492
{
    [string] F()
    {
        return $script:foo
    }
}

function Get-MSFT_778492
{
    [MSFT_778492]::new()
}
