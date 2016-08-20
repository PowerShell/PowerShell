### The techniques used in this demo are documented at
### https://azure.microsoft.com/en-us/documentation/articles/powershell-azure-resource-manager/

### Import AzureRM.Profile.NetCore.Preview and AzureRM.Resources.NetCore.Preview modules.
### AzureRM.NetCore.Preview is a wrapper module that pulls in these modules
###
### Because of issue https://github.com/PowerShell/PowerShell/issues/1618,
### currently you will not be able to use "Install-Module AzureRM.NetCore.Preview" from
### PowerShellGallery. You can use the following workaround until the issue is fixed:
###
### Install-Package -Name AzureRM.NetCore.Preview -Source https://www.powershellgallery.com/api/v2 -ProviderName NuGet -ExcludeVersion -Destination <Folder you want this to be installed>
###
### Ensure $ENV:PSMODULEPATH is updated with the location you used to install.
Import-Module AzureRM.NetCore.Preview

### Supply your Azure Credentials
Login-AzureRMAccount

### Get a name for Azure Resource Group
$resourceGroupName = "PSAzDemo" + (new-guid | % guid) -replace "-",""
$resourceGroupName

### Creating a New Azure Resource Group 
New-AzureRMResourceGroup -Name $resourceGroupName -Location "West US"

### Deploy an Ubuntu 14.04 VM using Resource Manager cmdlets
### Template is available is at 
### http://armviz.io/#/?load=https:%2F%2Fraw.githubusercontent.com%2FAzure%2Fazure-quickstart-templates%2Fmaster%2F101-vm-simple-linux%2Fazuredeploy.json
$dnsLabelPrefix = $resourceGroupName | % tolower
$dnsLabelPrefix
$password = Convertto-Securestring -String "PowerShellRocks!" -AsPlainText -Force
New-AzureRMResourceGroupDeployment -ResourceGroupName $resourceGroupName  -TemplateFile ./Compute-Linux.json -adminUserName psuser  -adminPassword $password  -dnsLabelPrefix $dnsLabelPrefix

### Monitor the status of the deployment
Get-AzureRMResourceGroupDeployment -ResourceGroupName $resourceGroupName 

### Discover the resources we created by the previous deployment
Find-AzureRMResource -ResourceGroupName $resourceGroupName  | select Name,ResourceType,Location

### Get the state of the VM we created
### Notice: The VM is in running state
Get-AzureRMResource -ResourceName MyUbuntuVM  -ResourceType Microsoft.Compute/virtualMachines -ResourceGroupName $resourceGroupName  -ODataQuery '$expand=instanceView' | % properties | % instanceview | % statuses

### Discover the Operations we can perform on the compute resource
### Notice: Operations like "Power Off Virtual Machine", "Start Virtual Machine", "Create Snapshot", "Delete Snapshot", "Delete Virtual Machine"
Get-AzureRMProviderOperation -OperationSearchString Microsoft.Compute/* | select  OperationName,Operation

### Power Off the Virtual Machine we created
Invoke-AzureRmResourceAction -ResourceGroupName $resourceGroupName  -ResourceType Microsoft.Compute/virtualMachines -ResourceName MyUbuntuVM -Action poweroff 

### Check the VM State again. It should be stopped now.
Get-AzureRMResource -ResourceName MyUbuntuVM  -ResourceType Microsoft.Compute/virtualMachines -ResourceGroupName $resourceGroupName  -ODataQuery '$expand=instanceView' | % properties | % instanceview | % statuses

### As you know, you may still be incurring charges even if the VM is in stopped state
### Deallocate the resource to avoid this charge
Invoke-AzureRmResourceAction -ResourceGroupName $resourceGroupName  -ResourceType Microsoft.Compute/virtualMachines -ResourceName MyUbuntuVM -Action deallocate 

### The following command removes the Virtual Machine
Remove-AzureRmResource -ResourceName MyUbuntuVM -ResourceType Microsoft.Compute/virtualMachines -ResourceGroupName $resourceGroupName 

### Look at the resources that still exists
Find-AzureRMResource -ResourceGroupName $resourceGroupName  | select Name,ResourceType,Location

### Remove the ResourceGroup which removes all the resources in the ResourceGroup
Remove-AzureRmResourceGroup -Name $resourceGroupName 
