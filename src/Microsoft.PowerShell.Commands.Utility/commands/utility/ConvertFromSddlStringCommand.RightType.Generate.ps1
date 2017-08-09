$RightTypes = @(
    ( "Full", [System.Security.AccessControl.FileSystemRights] ),
    ( "Full", [System.Security.AccessControl.RegistryRights] ),
    ( "Full", [System.DirectoryServices.ActiveDirectoryRights] ),
    ( "Full", [System.Security.AccessControl.MutexRights] ),
    ( "Full", [System.Security.AccessControl.SemaphoreRights] ),
    ( "Core", [System.Security.AccessControl.CryptoKeyRights] ),
    ( "Full", [System.Security.AccessControl.EventWaitHandleRights] )
);

$OutFileName = Join-Path $PSScriptRoot 'ConvertFromSddlStringCommand.RightType.cs';

#
# File preable

$OutFileContent = @"
using System.Collections.Generic;

namespace Microsoft.PowerShell.Commands
{
    public sealed partial class ConvertFromSddlStringCommand
    {
"@

#
# RightType enum

$OutFileContent += @"

        /// <summary>
        /// Types defining access control right flags.
        /// </summary>
        public enum RightType
        {
"@;

$RightTypes | ForEach-Object {
    if ($_[0] -eq "Core") {
        $OutFileContent += @"

#if !CORECLR
"@;
    }

    $OutFileContent += @"

            /// <summary>
            /// <see cref="$($_[1].FullName)"/> type rights.
            /// </summary>
            $($_[1].Name),

"@;

    if ($_[0] -eq "Core") {
        $OutFileContent += @"
#endif

"@;
    }
}

#
# RightTypeFlags dictionary

$OutFileContent += @"

        }

        private static readonly Dictionary<RightType, Dictionary<int, string>> RightTypeFlags =
            new Dictionary<RightType, Dictionary<int, string>>
            {
"@;

$RightTypes | ForEach-Object {
    if ($_[0] -eq "Core") {
        $OutFileContent += @"

#if !CORECLR
"@;
    }

    $OutFileContent += @"

                {
                    RightType.$($_[1].Name),
                    new Dictionary<int, string>
                    {
"@;

    [System.Enum]::GetValues($_[1]) | Select-Object -Unique | ForEach-Object {
        $OutFileContent += @"

                        {
"@ + (' {0:d}' -f $_) + ',' + (' "{0}" ' -f $_) + '},';
    }

    $OutFileContent += @"

                    }
                },
"@;

    if ($_[0] -eq "Core") {
        $OutFileContent += @"

#endif
"@;
    }
}

$OutFileContent += @"

            };
    }
}

"@;

$OutFileContent | Out-File $OutFileName -Encoding utf8
