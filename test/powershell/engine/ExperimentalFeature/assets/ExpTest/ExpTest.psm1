# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

using namespace System.Management.Automation

#region "Replace existing function"

function Invoke-AzureFunction
{
    [Experimental("ExpTest.FeatureOne", [ExperimentAction]::Hide)]
    param(
        [string] $Token,
        [string] $Command
    )

    "Invoke-AzureFunction Version ONE"
}

function Invoke-AzureFunctionV2
{
    [Experimental("ExpTest.FeatureOne", [ExperimentAction]::Show)]
    [Alias("Invoke-AzureFunction")]
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Token,

        [Parameter(Mandatory)]
        [string] $Command
    )

    "Invoke-AzureFunction Version TWO"
}

#endregion

#region "Make parameter set experimental"

function Get-GreetingMessage
{
    [CmdletBinding(DefaultParameterSetName = "Default")]
    param(
        [Parameter(Mandatory)]
        [string] $Name,

        ## If only one parameter attribute is declared for a parameter, then the parameter is
        ## hidden when the parameter attribute needs to be hide.
        [Parameter("ExpTest.FeatureOne", [ExperimentAction]::Show, ParameterSetName = "SwitchOneSet")]
        [switch] $SwitchOne,

        [Parameter("ExpTest.FeatureOne", [ExperimentAction]::Show, ParameterSetName = "SwitchTwoSet")]
        [switch] $SwitchTwo
    )

    $message = "Hello World $Name."

    if ([ExperimentalFeature]::IsEnabled("ExpTest.FeatureOne"))
    {
        if ($SwitchOne) { $message += "-SwitchOne is on." }
        if ($SwitchTwo) { $message += "-SwitchTwo is on." }
    }

    Write-Output $message
}

function Invoke-MyCommand
{
    param(
        [Parameter(Mandatory, ParameterSetName = "ComputerSet")]
        [string] $UserName,
        [Parameter(Mandatory, ParameterSetName = "ComputerSet")]
        [string] $ComputerName,

        [Parameter(Mandatory, ParameterSetName = "VMSet")]
        [string] $VMName,

        ## Enable web socket only if the feature is turned on.
        [Parameter("ExpTest.FeatureOne", [ExperimentAction]::Show, Mandatory, ParameterSetName = "WebSocketSet")]
        [string] $Token,
        [Parameter("ExpTest.FeatureOne", [ExperimentAction]::Show, Mandatory, ParameterSetName = "WebSocketSet")]
        [string] $WebSocketUrl,

        ## Add -ConfigurationName to parameter set "WebSocketSet" only if the feature is turned on.
        [Parameter(ParameterSetName = "ComputerSet")]
        [Parameter("ExpTest.FeatureOne", [ExperimentAction]::Show, ParameterSetName = "WebSocketSet")]
        [string] $ConfigurationName,

        ## Add -Port to parameter set "WebSocketSet" only if the feature is turned on.
        [Parameter(ParameterSetName = "VMSet")]
        [Parameter("ExpTest.FeatureOne", [ExperimentAction]::Show, ParameterSetName = "WebSocketSet")]
        [int] $Port,

        [int] $ThrottleLimit,
        [string] $Command
    )

    switch ($PSCmdlet.ParameterSetName)
    {
        "ComputerSet"  { "Invoke-MyCommand with ComputerSet" }
        "VMSet"        { "Invoke-MyCommand with VMSet" }
        "WebSocketSet" { "Invoke-MyCommand with WebSocketSet" }
    }
}

function Test-MyRemoting
{
    param(
        ## Replace one parameter with another one when the feature is turned on.
        [Parameter("ExpTest.FeatureOne", [ExperimentAction]::Hide)]
        [string] $SessionName,

        [Parameter("ExpTest.FeatureOne", [ExperimentAction]::Show)]
        [string] $ComputerName
    )
}

#endregion

#region "Use 'Experimental' attribute on parameters"

function Save-MyFile
{
    param(
        [Parameter(ParameterSetName = "UrlSet")]
        [switch] $ByUrl,

        [Parameter(ParameterSetName = "RadioSet")]
        [switch] $ByRadio,

        [string] $FileName,

        [Experimental("ExpTest.FeatureOne", [ExperimentAction]::Show)]
        [string] $Destination,

        [Experimental("ExpTest.FeatureOne", [ExperimentAction]::Hide)]
        [Parameter(ParameterSetName = "UrlSet")]
        [Parameter(ParameterSetName = "RadioSet")]
        [string] $Configuration
    )
}

#endregion

#region "Dynamic parameters"

function Test-MyDynamicParamOne
{
    [CmdletBinding()]
    param(
        [string] $Name
    )

    ## Use the parameter attribute to hide or show a dynamic parameter.
    DynamicParam {
        if ($Name -eq "Joe") {
            $runtimeParams = [RuntimeDefinedParameterDictionary]::new()

            $configFileAttributes = [System.Collections.ObjectModel.Collection[Attribute]]::new()
            $configFileAttributes.Add([Parameter]::new("ExpTest.FeatureOne", [ExperimentAction]::Show))
            $configFileAttributes.Add([ValidateNotNullOrEmpty]::new())
            $configFileParam = [RuntimeDefinedParameter]::new("ConfigFile", [string], $configFileAttributes)

            $configNameAttributes = [System.Collections.ObjectModel.Collection[Attribute]]::new()
            $configNameAttributes.Add([Parameter]::new("ExpTest.FeatureOne", [ExperimentAction]::Hide))
            $configNameAttributes.Add([ValidateNotNullOrEmpty]::new())
            $ConfigNameParam = [RuntimeDefinedParameter]::new("ConfigName", [string], $configNameAttributes)

            $runtimeParams.Add("ConfigFile", $configFileParam)
            $runtimeParams.Add("ConfigName", $ConfigNameParam)
            return $runtimeParams
        }
    }
}

function Test-MyDynamicParamTwo
{
    [CmdletBinding()]
    param(
        [string] $Name
    )

    ## Use the experimental attribute to hide or show a dynamic parameter.
    DynamicParam {
        if ($Name -eq "Joe") {
            $runtimeParams = [RuntimeDefinedParameterDictionary]::new()

            $configFileAttributes = [System.Collections.ObjectModel.Collection[Attribute]]::new()
            $configFileAttributes.Add([Experimental]::new("ExpTest.FeatureOne", [ExperimentAction]::Show))
            $configFileAttributes.Add([Parameter]::new())
            $configFileAttributes.Add([ValidateNotNullOrEmpty]::new())
            $configFileParam = [RuntimeDefinedParameter]::new("ConfigFile", [string], $configFileAttributes)

            $configNameAttributes = [System.Collections.ObjectModel.Collection[Attribute]]::new()
            $configNameAttributes.Add([Experimental]::new("ExpTest.FeatureOne", [ExperimentAction]::Hide))
            $configNameAttributes.Add([Parameter]::new())
            $configNameAttributes.Add([ValidateNotNullOrEmpty]::new())
            $ConfigNameParam = [RuntimeDefinedParameter]::new("ConfigName", [string], $configNameAttributes)

            $runtimeParams.Add("ConfigFile", $configFileParam)
            $runtimeParams.Add("ConfigName", $ConfigNameParam)
            return $runtimeParams
        }
    }
}

#endregion
