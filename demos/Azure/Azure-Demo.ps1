# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

### The techniques used in this demo are documented at
### https://azure.microsoft.com/documentation/articles/powershell-azure-resource-manager/

### Import AzureRM.Profile.NetCore.Preview and AzureRM.Resources.NetCore.Preview modules.
### AzureRM.NetCore.Preview is a wrapper module that pulls in these modules
###
### Because of issue https://github.com/PowerShell/PowerShell/issues/1618,
### currently you will not be able to use "Install-Module AzureRM.NetCore.Preview" from
### PowerShellGallery. You can use the following workaround until the issue is fixed:
###
### Install-Package -Name AzureRM.NetCore.Preview -Source https://www.powershellgallery.com/api/v2 -ProviderName NuGet -ExcludeVersion -Destination <Folder you want this to be installed>
###
### Ensure $env:PSModulePath is updated with the location you used to install.
Import-Module AzureRM.NetCore.Preview

### Supply your Azure Credentials
Login-AzureRmAccount

### Specify a name for Azure Resource Group
$resourceGroupName = "PSAzDemo" + (New-Guid | ForEach-Object guid) -replace "-",""
$resourceGroupName

### Create a new Azure Resource Group
New-AzureRmResourceGroup -Name $resourceGroupName -Location "West US"

### Deploy an Ubuntu 14.04 VM using Resource Manager cmdlets
### Template is available at
### http://armviz.io/#/?load=https:%2F%2Fraw.githubusercontent.com%2FAzure%2Fazure-quickstart-templates%2Fmaster%2F101-vm-simple-linux%2Fazuredeploy.json
$dnsLabelPrefix = $resourceGroupName | ForEach-Object tolower
$dnsLabelPrefix

#[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Demo/doc secret.")]
$password = ConvertTo-SecureString -String "PowerShellRocks!" -AsPlainText -Force
New-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -TemplateFile ./Compute-Linux.json -adminUserName psuser -adminPassword $password -dnsLabelPrefix $dnsLabelPrefix

### Monitor the status of the deployment
Get-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName

### Discover the resources we created by the previous deployment
Find-AzureRmResource -ResourceGroupName $resourceGroupName | Select-Object Name,ResourceType,Location

### Get the state of the VM we created
### Notice: The VM is in running state
Get-AzureRmResource -ResourceName MyUbuntuVM -ResourceType Microsoft.Compute/virtualMachines -ResourceGroupName $resourceGroupName -ODataQuery '$expand=instanceView' | ForEach-Object properties | ForEach-Object instanceview | ForEach-Object statuses

### Discover the operations we can perform on the compute resource
### Notice: Operations like "Power Off Virtual Machine", "Start Virtual Machine", "Create Snapshot", "Delete Snapshot", "Delete Virtual Machine"
Get-AzureRmProviderOperation -OperationSearchString Microsoft.Compute/* | Select-Object OperationName,Operation

### Power Off the Virtual Machine we created
Invoke-AzureRmResourceAction -ResourceGroupName $resourceGroupName -ResourceType Microsoft.Compute/virtualMachines -ResourceName MyUbuntuVM -Action poweroff

### Check the VM state again. It should be stopped now.
Get-AzureRmResource -ResourceName MyUbuntuVM -ResourceType Microsoft.Compute/virtualMachines -ResourceGroupName $resourceGroupName -ODataQuery '$expand=instanceView' | ForEach-Object properties | ForEach-Object instanceview | ForEach-Object statuses

### As you know, you may still be incurring charges even if the VM is in stopped state
### Deallocate the resource to avoid this charge
Invoke-AzureRmResourceAction -ResourceGroupName $resourceGroupName -ResourceType Microsoft.Compute/virtualMachines -ResourceName MyUbuntuVM -Action deallocate

### The following command removes the Virtual Machine
Remove-AzureRmResource -ResourceName MyUbuntuVM -ResourceType Microsoft.Compute/virtualMachines -ResourceGroupName $resourceGroupName

### Look at the resources that still exists
Find-AzureRmResource -ResourceGroupName $resourceGroupName | Select-Object Name,ResourceType,Location

### Remove the resource group and its resources
Remove-AzureRmResourceGroup -Name $resourceGroupName
