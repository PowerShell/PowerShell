# set execution policy to bypass for the following
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process

# stop services
Stop-Service WindowsGongService
Stop-Service DisplayLinkService

# stop processes
Stop-Process -Name Outlook
Stop-Process -Name lync
Stop-Process -Name CUCILync
Stop-Process -Name iCloudServices
Stop-Process -Name SpotifyWebHelper
Stop-Process -Name iPodService
Stop-Process -Name KEPInfo
Stop-Process -Name SetPoint
Stop-Process -Name PLTHub