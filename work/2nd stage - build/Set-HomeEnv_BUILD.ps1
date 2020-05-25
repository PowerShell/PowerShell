<# 
Start@Home - Script to build "at home" environment.
- keep only needed/wanted/nessecary software/services/stuff.
- stay offline as long as possible, to boot smoothly.
- trigger VPN second but last and manual-start-of-Logonskript last.
- do a quick drive check if after msoL is through.

last build: 20.11.18
UI221223
#>

# stop services
Stop-Service WindowsGongService
Stop-Service DisplayLinkService

# stop processes
Stop-Process -Name SetPoint
Stop-Process -Name lync
Stop-Process -Name CUCILync
Stop-Process -Name PLTHub
Stop-Process -Name iCloudServices
Stop-Process -Name SpotifyWebHelper
Stop-Process -Name iTunesHelper
Stop-Process -Name iPodService
Stop-Process -Name Outlook
Stop-Process -Name KEPInfo
Stop-Process -Name SelfService
Stop-Process -Name SelfServicePlugin
Stop-process -Name OfficeClickToRun

<# 
c/p

# Set execution policy to smth sensefull
# Set-ExecutionPolicy Bypass Process
#>