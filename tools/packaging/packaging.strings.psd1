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
'@

    RedHatAfterRemoveScript = @'
if [ "$1" = 0 ] ; then
    if [ -f /etc/shells ] ; then
        TmpFile=`/bin/mktemp /tmp/.powershellmXXXXXX`
        grep -v '^{0}$' /etc/shells > $TmpFile
        cp -f $TmpFile /etc/shells
        rm -f $TmpFile
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
'@
    UbuntuAfterRemoveScript = @'
#!/bin/sh
set -e
case "$1" in
        (remove)
        remove-shell "{0}"
        ;;
esac
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
    <product id="com.microsoft.powershell" version="{1}" />
    <choices-outline>
        <line choice="default">
            <line choice="powershell"/>
        </line>
    </choices-outline>
    <choice id="default"/>
    <choice id="powershell" visible="false">
        <pkg-ref id="com.microsoft.powershell"/>
    </choice>
    <pkg-ref id="com.microsoft.powershell" version="{1}" onConclusion="none">{2}</pkg-ref>
</installer-gui-script>
'@
}
