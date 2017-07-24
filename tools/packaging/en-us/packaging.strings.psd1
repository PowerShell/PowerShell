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
}
