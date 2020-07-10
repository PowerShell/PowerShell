# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

configuration  scriptdsc
{
    param ($one, $two, $three)

    Import-DscResource -ModuleName UserConfigProv
    Log scriptDscLogResource
    {
        Message = "This is the message from the embedded log resource: one=$one two=$two three=$three"
    }

 }

Export-ModuleMember -Function scriptdsc
