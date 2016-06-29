#region Download the package from GitHub to Ubuntu (14/16)
# TODO: Update the url
# TODO: Update to apt-get, if that is available
curl https://github.com/PowerShell/PowerShell/releases/download/v0.5.0/powershell_0.5.0-1_amd64.deb
#endregion

#region Install PowerShell and its dependencies
# TODO: Fix the version
sudo apt-get install libunwind8 libicu52
sudo dpkg -i powershell_0.5.0-1_amd64.deb
#endregion

#region Launch PowerShell
# TODO: Launch a new terminal
clear
PowerShell
#endregion