# Package Installation Instructions

## macOS 10.12+

### Installation via Homebrew (preferred)

[Homebrew][brew] is the missing package manager for macOS.
If the `brew` command is not found,
you need to install Homebrew following [their instructions][brew].

Once you've installed Homebrew, installing PowerShell is easy.
First, install [Homebrew-Cask][cask], so you can install more packages:

```sh
brew tap caskroom/cask
```

Now, you can install PowerShell:

```sh
brew cask install powershell
```

When new versions of PowerShell are released,
simply update Homebrew's formulae and upgrade PowerShell:

```sh
brew update
brew cask reinstall powershell
```

> Note: because of [this issue in Cask](https://github.com/caskroom/homebrew-cask/issues/29301), you currently have to do a reinstall to upgrade.

[brew]: http://brew.sh/
[cask]: https://caskroom.github.io/

### Installation via Direct Download

Using macOS 10.12+, download the PKG package
`powershell-6.0.0-osx.10.12-x64.pkg`
from the [releases][] page onto the macOS machine.

Either double-click the file and follow the prompts,
or install it from the terminal:

```sh
sudo installer -pkg powershell-6.0.0-osx.10.12-x64.pkg -target /
```

### Installation via Binary Archive

```sh
# Download the powershell '.tar.gz' archive
curl -L -o /tmp/powershell.tar.gz https://github.com/PowerShell/PowerShell/releases/download/v6.0.0/powershell-6.0.0-osx-x64.tar.gz

# Create the target folder where powershell will be placed
sudo mkdir -p /usr/local/microsoft/powershell/6.0.0

# Expand powershell to the target folder
sudo tar zxf /tmp/powershell.tar.gz -C /usr/local/microsoft/powershell/6.0.0

# Set execute permissions
sudo chmod +x /usr/local/microsoft/powershell/6.0.0/pwsh

# Create the symbolic link that points to pwsh
sudo ln -s /usr/local/microsoft/powershell/6.0.0/pwsh /usr/local/bin/pwsh
```

### Uninstallation

If you installed PowerShell with Homebrew, uninstallation is easy:

```sh
brew cask uninstall powershell
```

If you installed PowerShell via direct download,
PowerShell must be removed manually:

```sh
sudo rm -rf /usr/local/microsoft /Applications/PowerShell.app
sudo rm -f /usr/local/bin/pwsh /usr/local/share/man/man1/pwsh.1.gz
sudo pkgutil --forget com.microsoft.powershell
```

If you installed PowerShell via binary archive, PowerShell must be removed manually.

```sh
sudo rm -rf /usr/local/microsoft
sudo rm -f /usr/local/bin/pwsh
```

To uninstall the additional PowerShell paths (such as the user profile path)
please see the [paths](linux.md#path) section below in this document
and remove the desired the paths with `sudo rm`.
(Note: this is not necessary if you installed with Homebrew.)
