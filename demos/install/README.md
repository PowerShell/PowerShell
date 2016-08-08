## Linux Install Instructions

Download `install.sh` locally onto the linux machine using `curl`
And execute the `install.sh` script

```
export GITHUB_TOKEN=”<Your GitHub Token>”
curl -s -i -H "Authorization: token $GITHUB_TOKEN" "https://raw.githubusercontent.com/PowerShell/PowerShell/master/demos/install/install.sh?token=AKHQDrg6m1rcyWLNneVE6NRIBz5HjZ_rks5XqimMwA%3D%3D" -o install.sh
chmod +x ./install.sh
./install.sh
```
