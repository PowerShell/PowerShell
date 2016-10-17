## Creating Windows10/Server2016 MSI Package

```
# Sync to the fork/branch code for the package
Import-Module .\build.psm1
Start-PSBootstrap -Package
Start-PSBuild -Clean -CrossGen -Runtime win10-x64 -Configuration Release
Start-PSPackage -Type msi
```


## Creating Windows8.1/2012R2 MSI Package

```
# Sync to the fork/branch code for the package
Import-Module .\build.psm1
Start-PSBootstrap -Package
Start-PSBuild -Clean -CrossGen -Runtime win81-x64 -Configuration Release
Start-PSPackage -Type msi -WindowsDownLevel win81-x64 
```
