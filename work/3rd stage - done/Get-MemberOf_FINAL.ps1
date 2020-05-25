<#
MemberOf of GUID from AD.

CN=ACC_I_99903432-1,OU=[File Share (ACC)],OU=Gruppen,OU=RWE Trading,OU=Gesellschaften,DC=group,DC=rwe,DC=com
CN=ACC_I_99903433-1,OU=[File Share (ACC)],OU=Gruppen,OU=RWE Trading,OU=Gesellschaften,DC=group,DC=rwe,DC=com
CN=FNC_INTERNET_USERS_RWEST,OU=[IDAM],OU=Gruppen,OU=RWE Trading,OU=Gesellschaften,DC=group,DC=rwe,DC=com
CN=FNC_RWE_Trading_Citrix_inubitToolset,OU=[Citrix],OU=Gruppen,OU=RWE Trading,OU=Gesellschaften,DC=group,DC=rwe,DC=com
CN=VL Deal Capture Traders,OU=Verteilerlisten,OU=Infrastruktur,DC=group,DC=rwe,DC=com
CN=VL LV-Hotline,OU=Verteilerlisten,OU=Infrastruktur,DC=group,DC=rwe,DC=com
CN=VL MFA Rest,OU=Verteilerlisten,OU=Infrastruktur,DC=group,DC=rwe,DC=com
CN=VL RWE Trading IT Support,OU=Verteilerlisten,OU=Infrastruktur,DC=group,DC=rwe,DC=com
CN=VL RWEST IT MI Info,OU=Verteilerlisten,OU=Infrastruktur,DC=group,DC=rwe,DC=com

Transform MemberOf grouped into plain text:
ACC = Fileshare, show location -> Description tag
FNC = Functiongroup, show Description
VL = DL, show DisplayName + mail

$who = GUID, mail, Name
$Clip = yes, no, what?
#>

function Get-MemberOf {
    [CmdletBinding(PositionalBinding)]
    param (
        
    )
    
    begin {
    }
    
    process {
    }
    
    end {
    }
}