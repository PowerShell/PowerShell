$ERRORACTIONPREFERENCE = $WARNINGPREFERENCE = [System.Management.Automation.ActionPreference]:: STOP
Set-PSRepository PSGallery -InstallationPolicy:Trusted
INSTALL-MODULE PESTER
IPMO PESTER
UPDATE-HELP -M:Microsoft.PowerShell.Core -UI:EN-US -SO:NUL -EA:ST | SHOULD -THROW -ErrorId:UnableToRetrieveHelpInfoXml,Microsoft.PowerShell.Commands.UpdateHelpCommand
