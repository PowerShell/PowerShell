param (
    ${DLSrv} = "DisplayLinkService",
    ${WGSrv} = "WindowsGongService"
)

begin {
    Start-Service -Name $DLSrv
    Start-Service -Name $WGSrv
}

process {
    Get-Service -Name $DLSrv, $WGSrv
}

End {
    write-host "$SrvCnt started."
    read-host
}