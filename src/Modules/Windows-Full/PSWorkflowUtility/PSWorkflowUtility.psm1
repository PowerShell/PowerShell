workflow Invoke-AsWorkflow
{
    <#
    .EXTERNALHELP Microsoft.PowerShell.Workflow.ServiceCore.dll-help.xml
    #>
    [System.Security.SecurityCritical()]
    [CmdletBinding(DefaultParameterSetName='Command', HelpUri='https://go.microsoft.com/fwlink/?LinkId=238267')]
    param(
        [Parameter(Mandatory=$true,ParameterSetName="Command")]
        [ValidateNotNullOrEmpty()]
        [String]$CommandName,
        [Parameter(ParameterSetName="Command")]
        [HashTable]$Parameter,
        [Parameter(Mandatory=$true,ParameterSetName="Expression")]
        [String]$Expression
    )
    if($CommandName)
    {
        if($Parameter) 
		{
			InlineScript {& $using:CommandName @using:Parameter}
		}
        else 
		{
			InlineScript {& $using:CommandName}
		}
    }
    else
    {
        Invoke-Expression -Command $Expression
    }
}

Export-ModuleMember -Function Invoke-AsWorkflow
