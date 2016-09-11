function New-PSWorkflowSession
{
    <#
    .EXTERNALHELP Microsoft.PowerShell.Workflow.ServiceCore.dll-help.xml
    #>
    [CmdletBinding(DefaultParameterSetName='ComputerName', HelpUri='https://go.microsoft.com/fwlink/?LinkID=238268', RemotingCapability='OwnedByCommand')]
    [OutputType([System.Management.Automation.Runspaces.PSSession])]
    param(
        [Parameter(ParameterSetName='ComputerName', Position=0, ValueFromPipeline=$true, ValueFromPipelineByPropertyName=$true)]
        [Alias('Cn')]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $ComputerName,
        
        [Parameter(ParameterSetName='ComputerName', ValueFromPipelineByPropertyName=$true)]
        [Object]
        $Credential,

        [string[]]
        $Name,

        [Parameter(ParameterSetName='ComputerName')]
        [ValidateRange(1, 65535)]
        [int]
        $Port,

        [Parameter(ParameterSetName='ComputerName')]
        [switch]
        $UseSSL,

        [Parameter(ParameterSetName='ComputerName', ValueFromPipelineByPropertyName=$true)]
        [string]
        $ApplicationName,

        [Parameter(ParameterSetName='ComputerName')]
        [int]
        $ThrottleLimit,
                
        [Parameter(ParameterSetName='ComputerName')]
        [ValidateNotNull()]
        [System.Management.Automation.Remoting.PSSessionOption]
        $SessionOption,

        [Parameter(ParameterSetName='ComputerName')]
        [System.Management.Automation.Runspaces.AuthenticationMechanism]
        $Authentication,

        [Parameter(ParameterSetName='ComputerName')]
        [string]
        $CertificateThumbprint,

        [Parameter(ParameterSetName='ComputerName')]
        [switch]
        $EnableNetworkAccess
    )
    Process
    {
        New-PSSession -ConfigurationName Microsoft.PowerShell.Workflow @PSBoundParameters
    }
}

Set-Alias -Name nwsn -Value New-PSWorkflowSession

Export-ModuleMember -Function New-PSWorkflowSession -Alias nwsn
