# Adopters

<!--
Example entry:

```markdown
* [PowerShell](https://github.com/powershell/powershell) uses PowerShell for builds, test automation, and packaging.
  This includes over 11,000 test cases supported on various Linux distros, Windows, and macOS.
  More information about PowerShell is available at [PowerShell Docs](https://aka.ms/powershell-docs).
```
-->

This is a list of adopters of using PowerShell in production or in their products (in alphabetical order):

* [Azure Cloud Shell](https://shell.azure.com/) provides a batteries-included browser-based PowerShell environment used by Azure administrators to manage their environment.
  It includes up-to-date PowerShell modules for `Azure`, `AzureAD`, `Exchange`, `Teams`, and many more.
  More information about Azure Cloud Shell is available at [Azure Cloud Shell Overview.](https://docs.microsoft.com/azure/cloud-shell/overview)
* [Azure Functions - PowerShell](https://github.com/Azure/azure-functions-powershell-worker) is a serverless compute service to execute PowerShell scripts in the cloud without worrying about managing resources.
  In addition, Azure Functions provides client tools such as [`Az.Functions`](https://www.powershellgallery.com/packages/Az.Functions), a cross-platform PowerShell module to manage function apps and service plans in the cloud.
  For more information about Functions, please visit [functions overview](https://docs.microsoft.com/azure/azure-functions/functions-overview).
* [PowerShell Universal](https://ironmansoftware.com/powershell-universal) is a cross-platform web framework for PowerShell. 
  It provides the ability to create robust, interactive websites, REST APIs, and Electron-based desktop apps with PowerShell script. 
  More information about PowerShell Universal Dashboard is available at the [PowerShell Universal Dashboard Docs](https://docs.universaldashboard.io).
* [System Frontier](https://systemfrontier.com/solutions/powershell/) provides dynamically generated web GUIs and REST APIs for PowerShell and other scripting languages.  
  Enable non-admins like help desk and tier 1 support teams to execute secure web based tools on any platform `without admin rights`.  
  Configure flexible RBAC permissions from an intuitive interface, without a complex learning curve.  
  Script output along with all actions are audited. Manage up to 5,000 nodes for free with the [Community Edition](https://systemfrontier.com/solutions/community-edition/).
* [Amazon AWS](https://aws.com) supports PowerShell in a wide variety of its products including [AWS tools for PowerShell](https://github.com/aws/aws-tools-for-powershell),
  [AWS Lambda Support For PowerShell](https://github.com/aws/aws-lambda-dotnet/tree/master/PowerShell) and [AWS PowerShell Tools for `CodeBuild`](https://docs.aws.amazon.com/powershell/latest/reference/items/CodeBuild_cmdlets.html)
  as well as supporting PowerShell Core in both Windows and Linux EC2 Images.
* [Azure Resource Manager Deployment Scripts](https://docs.microsoft.com/azure/azure-resource-manager/templates/deployment-script-template) Complete the "last mile" of your Azure Resource Manager (ARM) template deployments with a Deployment Script, which enables you to run an arbitrary PowerShell script in the context of a deployment.
  Designed to let you complete tasks that should be part of a deployment, but are not possible in an ARM template today — for example, creating a Key Vault certificate or querying an external API for a new CIDR block. 
* [Azure Pipelines Hosted Agents](https://docs.microsoft.com/azure/devops/pipelines/agents/hosted?view=azure-devops) Windows, Ubuntu, and MacOS Agents used by Azure Pipelines customers have PowerShell pre-installed so that customers can make use of it for all their CI/CD needs.
* [GitHub Actions Virtual Environments for Hosted Runners](https://help.github.com/actions/reference/virtual-environments-for-github-hosted-runners) Windows, Ubuntu, and macOS virtual environments used by customers of GitHub Actions include PowerShell out of the box.
* [GitHub Actions Python builds](https://github.com/actions/python-versions) GitHub Actions uses PowerShell to automate building Python from source for its runners.
* [Microsoft HoloLens](https://www.microsoft.com/hololens) makes extensive use of PowerShell 7+ throughout the development cycle to automate tasks such as firmware assembly and automated testing.
* [Power BI](https://powerbi.microsoft.com/) provides PowerShell users a set of cmdlets in [MicrosoftPowerBIMgmt](https://docs.microsoft.com/powershell/power-bi) module to manage and automate the Power BI service.
  This is in addition to Power BI leveraging PowerShell internally for various engineering systems and infrastructure for its service.
* [Windows 10 IoT Core](https://docs.microsoft.com/windows/iot-core/windows-iot-core) is a small form factor Windows edition for IoT devices and now you can easily include the [PowerShell package](https://github.com/ms-iot/iot-adk-addonkit/blob/master/Tools/IoTCoreImaging/Docs/Import-PSCoreRelease.md#Import-PSCoreRelease) in your imaging process.
