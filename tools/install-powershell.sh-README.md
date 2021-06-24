# install-powershell.sh

## Features of install-powershell.sh

* Can be called directly from git
* Optionally installs vs code and vs powershell extension (aka PowerShell IDE) using optional `-includeide` switch
* Defaults to completely automated operation (if appropriate permissions are available)
* Automatically looks up latest version via git tags
* Automatic selection of appropriate install sub-script
* Configures software installs for repositories when repositories are in place, otherwise pulls files from git releases.  As repository versions are made available, script will be updated to take advantage.
* User permission checking
* Sub-installers called from local file system if they exist, otherwise pulled from git
* Sub-installers can be called directly if auto-selection is not needed

## Minimum Requirements for install-powershell.sh

* bash shell
* `sed`
* native package manager available
* `curl` (auto-installed if missing)
* `tr`

## Parameters

* -includeide - installs VSCode and VSCode PowerShell extension (only relevant to machines with a desktop environment)
* -interactivetesting - do a quick launch test of VSCode (only relevant when used with -includeide)
* -skip-sudo-check - use sudo without verifying its availability (hard to accurately do on some distros)
* -preview - installs the latest preview release of PowerShell core side-by-side with any existing production releases

## Usage

### Direct from Github

```bash
bash <(wget -O - https://raw.githubusercontent.com/PowerShell/PowerShell/master/tools/install-powershell.sh) <ARGUMENTS>

wget -O - https://raw.githubusercontent.com/PowerShell/PowerShell/master/tools/install-powershell.sh | bash -s <ARGUMENTS>
```

### Local Copy

```bash
bash install-powershell.sh <ARGUMENTS>
```

## Examples

### Only Install PowerShell Core

```bash
bash <(wget -O - https://raw.githubusercontent.com/PowerShell/PowerShell/master/tools/install-powershell.sh)
```

### Install PowerShell Core with IDE

```bash
bash <(wget -O - https://raw.githubusercontent.com/PowerShell/PowerShell/master/tools/install-powershell.sh) -includeide
```

### Install PowerShell Core with IDE and do tests that require a human to interact with the installation process

```bash
bash <(wget -O - https://raw.githubusercontent.com/PowerShell/PowerShell/master/tools/install-powershell.sh) -includeide -interactivetesting
```

### Installation To do list

* Detect and wait when package manager is busy/locked? - at least Ubuntu (CentOS does this internally)
