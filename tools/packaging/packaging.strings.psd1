@{
    Description = @'
PowerShell is an automation and configuration management platform.
It consists of a cross-platform command-line shell and associated scripting language.
'@

    RedHatAfterInstallScript = @'
#!/bin/sh
if [ ! -f /etc/shells ] ; then
    echo "{0}" > /etc/shells
else
    grep -q "^{0}$" /etc/shells || echo "{0}" >> /etc/shells
fi
if [ -f /lib64/libssl.so.1.1 ] ; then
    ln -f -s /lib64/libssl.so.1.1 {1}/libssl.so.1.0.0
    ln -f -s /lib64/libcrypto.so.1.1.1 {1}/libcrypto.so.1.0.0
else
    ln -f -s /lib64/libssl.so.10 {1}/libssl.so.1.0.0
    ln -f -s /lib64/libcrypto.so.10 {1}/libcrypto.so.1.0.0
fi

'@

    RedHatAfterRemoveScript = @'
if [ "$1" = 0 ] ; then
    if [ -f /etc/shells ] ; then
        TmpFile=`/bin/mktemp /tmp/.powershellmXXXXXX`
        grep -v '^{0}$' /etc/shells > $TmpFile
        cp -f $TmpFile /etc/shells
        rm -f $TmpFile
        rm -f {1}/libssl.so.1.0.0
        rm -f {1}/libcrypto.so.1.0.0
    fi
fi
'@
    UbuntuAfterInstallScript = @'
#!/bin/sh
set -e
case "$1" in
    (configure)
        add-shell "{0}"
    ;;
    (abort-upgrade|abort-remove|abort-deconfigure)
        exit 0
    ;;
    (*)
        echo "postinst called with unknown argument '$1'" >&2
        exit 0
    ;;
esac

if [ -f /usr/lib/x86_64-linux-gnu/libssl.so.1.1 ] ; then
    ln -f -s /usr/lib/x86_64-linux-gnu/libssl.so.1.1 {1}/libssl.so.1.0.0
    ln -f -s /usr/lib/x86_64-linux-gnu/libcrypto.so.1.1 {1}/libcrypto.so.1.0.0
elif [ -f /usr/lib/x86_64-linux-gnu/libssl.so.1.0.2 ] ; then
    ln -f -s /usr/lib/x86_64-linux-gnu/libssl.so.1.0.2 {1}/libssl.so.1.0.0
    ln -f -s /usr/lib/x86_64-linux-gnu/libcrypto.so.1.0.2 {1}/libcrypto.so.1.0.0
else
    ln -f -s /lib64/libssl.so.10 {1}/libssl.so.1.0.0
    ln -f -s /lib64/libcrypto.so.10 {1}/libcrypto.so.1.0.0
fi

'@

    UbuntuAfterRemoveScript = @'
#!/bin/sh
set -e
case "$1" in
        (remove)
        remove-shell "{0}"
        rm -f {1}/libssl.so.1.0.0
        rm -f {1}/libcrypto.so.1.0.0
        ;;
esac
'@

    MacOSAfterInstallScript = @'
#!/bin/bash

if [ ! -f /etc/shells ] ; then
    echo "{0}" > /etc/shells
else
    grep -q "^{0}$" /etc/shells || echo "{0}" >> /etc/shells
fi
'@

    MacOSLauncherScript = @'
#!/usr/bin/env bash
open {0}
'@

    MacOSLauncherPlistTemplate = @'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>PowerShell.sh</string>
    <key>CFBundleGetInfoString</key>
    <string>{1}</string>
    <key>CFBundleIconFile</key>
    <string>{2}</string>
    <key>CFBundleIdentifier</key>
    <string>{0}</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundleName</key>
    <string>PowerShell</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>{1}</string>
    <key>CFBundleSupportedPlatforms</key>
    <array>
        <string>MacOSX</string>
    </array>
    <key>CFBundleVersion</key>
    <string>{1}</string>
</dict>
</plist>
'@

    # see https://developer.apple.com/library/content/documentation/DeveloperTools/Reference/DistributionDefinitionRef/Chapters/Distribution_XML_Ref.html
    OsxDistributionTemplate = @'
<?xml version="1.0" encoding="utf-8" standalone="yes"?>
<installer-gui-script minSpecVersion="1">
    <title>{0}</title>
    <options hostArchitectures="x86_64"/>
    <options customize="never" rootVolumeOnly="true"/>
    <background file="macDialog.png" scaling="tofit" alignment="bottomleft"/>
    <allowed-os-versions>
        <os-version min="{3}" />
    </allowed-os-versions>
    <options customize="never" require-scripts="false"/>
    <product id="{4}" version="{1}" />
    <choices-outline>
        <line choice="default">
            <line choice="powershell"/>
        </line>
    </choices-outline>
    <choice id="default"/>
    <choice id="powershell" visible="false">
        <pkg-ref id="{4}"/>
    </choice>
    <pkg-ref id="{4}" version="{1}" onConclusion="none">{2}</pkg-ref>
</installer-gui-script>
'@

    NuspecTemplate = @'
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2011/10/nuspec.xsd">
    <metadata>
        <id>{0}</id>
        <version>{1}</version>
        <authors>Microsoft</authors>
        <owners>Microsoft,PowerShell</owners>
        <requireLicenseAcceptance>false</requireLicenseAcceptance>
        <description>Runtime for hosting PowerShell</description>
        <projectUrl>https://github.com/PowerShell/PowerShell</projectUrl>
        <icon>{2}</icon>
        <license type="expression">MIT</license>
        <tags>PowerShell</tags>
        <language>en-US</language>
        <copyright>&#169; Microsoft Corporation. All rights reserved.</copyright>
        <contentFiles>
            <files include="**/*" buildAction="None" copyToOutput="true" flatten="false" />
        </contentFiles>
        <dependencies>
            <group targetFramework="net6.0"></group>
        </dependencies>
    </metadata>
</package>
'@

    NuGetConfigFile = @'
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="dotnet-core" value="https://dotnet.myget.org/F/dotnet-core/api/v3/index.json" />
    <add key="powershell-core" value="https://powershell.myget.org/F/powershell-core/api/v3/index.json" />
  </packageSources>
</configuration>
'@

    GlobalToolNuSpec = @'
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
    <metadata>
        <id>{0}</id>
        <version>{1}</version>
        <authors>Microsoft</authors>
        <owners>Microsoft,PowerShell</owners>
        <projectUrl>https://github.com/PowerShell/PowerShell</projectUrl>
        <icon>{2}</icon>
        <requireLicenseAcceptance>false</requireLicenseAcceptance>
        <description>PowerShell global tool</description>
        <license type="expression">MIT</license>
        <tags>PowerShell</tags>
        <language>en-US</language>
        <copyright>&#169; Microsoft Corporation. All rights reserved.</copyright>
        <packageTypes>
            <packageType name="DotnetTool" />
        </packageTypes>
    </metadata>
</package>
'@

    GlobalToolSettingsFile = @'
<?xml version="1.0" encoding="utf-8"?>
<DotNetCliTool Version="1">
    <Commands>
        <Command Name="pwsh" EntryPoint="{0}" Runner="dotnet" />
    </Commands>
</DotNetCliTool>
'@

}
