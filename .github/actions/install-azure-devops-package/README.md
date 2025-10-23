# Install Azure DevOps Universal Package Action

This composite GitHub Action downloads and optionally extracts Universal Packages from Azure DevOps Artifacts feeds.

## Features

- Downloads Universal Packages from Azure DevOps Artifacts feeds
- Supports both organization and project scoped feeds
- Optional package extraction with customizable paths
- Environment variable setting for integration with subsequent steps
- Flexible package file name patterns with version substitution

## Inputs

| Input | Description | Required | Default |
|-------|-------------|----------|---------|
| `feed` | Azure DevOps Artifacts feed name | ✅ | - |
| `package` | Package name to download | ✅ | - |
| `version` | Package version to download | ✅ | - |
| `organization` | Azure DevOps organization URL | ✅ | `https://dev.azure.com/powershell/` |
| `project` | Azure DevOps project ID | ✅ | - |
| `scope` | Package scope (project or organization) | ❌ | `project` |
| `destination` | Destination path for package download | ❌ | `${{ github.workspace }}\packages` |
| `packageFileName` | Expected package file name pattern (supports `{version}` substitution) | ❌ | - |
| `extractSubFolder` | Subfolder name to create for extraction | ❌ | - |
| `environmentVariable` | Environment variable name to set with extraction path | ❌ | - |

## Outputs

| Output | Description |
|--------|-------------|
| `packagePath` | Path where the package was downloaded |
| `extractionPath` | Path where the package was extracted |

## Prerequisites

- The `AZURE_DEVOPS_EXT_PAT` secret must be configured in your repository
- The Azure DevOps Personal Access Token must have permissions to read from the specified artifacts feed

## Usage Examples

### Basic Package Download

```yaml
- name: Download Package
  uses: ./.github/actions/install-azure-devops-package
  with:
    feed: 'MyFeed'
    package: 'my-package'
    version: '1.0.0'
    project: 'my-project-id'
```

### Download and Extract with Environment Variable

```yaml
- name: Install DSC Package
  uses: ./.github/actions/install-azure-devops-package
  with:
    feed: 'PowerShell-Universal'
    package: 'microsoft.dsc-windows'
    version: '3.2.0-preview.6'
    project: '2972bb5c-f20c-4a60-8bd9-00ffe9987edc'
    packageFileName: 'DSC-{version}-x86_64-pc-windows-msvc.zip'
    extractSubFolder: 'DSC'
    environmentVariable: 'DSC_ROOT'
```

### Using Custom Organization and Destination

```yaml
- name: Download Package
  uses: ./.github/actions/install-azure-devops-package
  with:
    feed: 'MyFeed'
    package: 'my-package'
    version: '2.1.0'
    organization: 'https://dev.azure.com/myorg/'
    project: 'my-project-id'
    destination: '${{ github.workspace }}\custom-packages'
    scope: 'organization'
```

## Environment Setup

Make sure to set up the Azure DevOps Personal Access Token as a repository secret:

1. Go to your repository's Settings → Secrets and variables → Actions
1. Add a new secret named `AZURE_DEVOPS_EXT_PAT`
1. Set the value to your Azure DevOps Personal Access Token with appropriate permissions

## Original Use Case Migration

The original step from the Windows test action:

```yaml
- name: Install Universal Package from Azure DevOps Feed
  shell: pwsh
  env:
    AZURE_DEVOPS_EXT_PAT: ${{ secrets.AZURE_DEVOPS_EXT_PAT }}
  run: |-
    # ... PowerShell script ...
```

Can be replaced with:

```yaml
- name: Install Universal Package from Azure DevOps Feed
  uses: ./.github/actions/install-azure-devops-package
  with:
    feed: 'PowerShell-Universal'
    package: 'microsoft.dsc-windows'
    version: '3.2.0-preview.6'
    project: '2972bb5c-f20c-4a60-8bd9-00ffe9987edc'
    packageFileName: 'DSC-{version}-x86_64-pc-windows-msvc.zip'
    extractSubFolder: 'DSC'
    environmentVariable: 'DSC_ROOT'
```

## Error Handling

The action includes error handling for:

- Missing Azure DevOps CLI installation
- Failed package downloads
- Missing expected package files
- Directory creation failures
- Archive extraction errors

## Security Considerations

- The `AZURE_DEVOPS_EXT_PAT` environment variable is used for authentication
- Ensure your Personal Access Token has minimal required permissions
- Consider using fine-grained tokens scoped to specific feeds and projects
