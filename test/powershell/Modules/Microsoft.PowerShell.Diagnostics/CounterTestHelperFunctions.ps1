# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
<############################################################################################
 # File: CounterTestHelperFunctions.ps1
 # Provides functions common to the performance counter Pester tests.
 ############################################################################################>

# Create a helper class providing facilities for translation of
# counter names and counter paths
$helperSource = @"
using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

    public class TestCounterHelper
    {
        private const long PDH_MORE_DATA = 0x800007D2L;
        private const int PDH_MAX_COUNTER_NAME = 1024;
        private const int PDH_MAX_COUNTER_PATH = 2048;
        private const string SubKeyPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Perflib\009";

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct PDH_COUNTER_PATH_ELEMENTS
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string MachineName;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string ObjectName;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string InstanceName;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string ParentInstance;

            public UInt32 InstanceIndex;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string CounterName;
        }

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        private static extern uint PdhMakeCounterPath(ref PDH_COUNTER_PATH_ELEMENTS pCounterPathElements,
                                                      StringBuilder szFullPathBuffer,
                                                      ref uint pcchBufferSize,
                                                      UInt32 dwFlags);

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        private static extern uint PdhParseCounterPath(string szFullPathBuffer,
                                                       IntPtr pCounterPathElements, //PDH_COUNTER_PATH_ELEMENTS
                                                       ref IntPtr pdwBufferSize,
                                                       uint dwFlags);

        [DllImport("pdh.dll", SetLastError=true, CharSet=CharSet.Unicode)]
        private static extern UInt32 PdhLookupPerfNameByIndex(string szMachineName,
                                                              uint dwNameIndex,
                                                              System.Text.StringBuilder szNameBuffer,
                                                              ref uint pcchNameBufferSize);

        private string[] _counters;

        public TestCounterHelper(string[] counters)
        {
            _counters = counters;
        }

        public string TranslateCounterName(string name)
        {
            var loweredName = name.ToLowerInvariant();

            for (var i = 1; i < _counters.Length - 1; i += 2)
            {
                if (_counters[i].ToLowerInvariant() == loweredName)
                {
                    try
                    {
                        var index = Convert.ToUInt32(_counters[i - 1], CultureInfo.InvariantCulture);
                        var sb = new StringBuilder(PDH_MAX_COUNTER_NAME);
                        var bufSize = (uint)sb.Capacity;
                        var result = PdhLookupPerfNameByIndex(null, index, sb, ref bufSize);

                        if (result == 0)
                            return sb.ToString().Substring(0, (int)bufSize - 1);
                    }
                    catch
                    {
                        // do nothing, we just won't translate
                    }

                    break;
                }
            }

            // return original path if translation failed
            return name;
        }

        public string TranslateCounterPath(string path)
        {
            var bufSize = new IntPtr(0);

            var result = PdhParseCounterPath(path,
                                             IntPtr.Zero,
                                             ref bufSize,
                                             0);
            if (result != 0 && result != PDH_MORE_DATA)
                return path;

            IntPtr structPointer = Marshal.AllocHGlobal(bufSize.ToInt32());

            try
            {
                result = PdhParseCounterPath(path,
                                             structPointer,
                                             ref bufSize,
                                             0);

                if (result == 0)
                {
                    var cpe = Marshal.PtrToStructure<PDH_COUNTER_PATH_ELEMENTS>(structPointer);

                    cpe.ObjectName = TranslateCounterName(cpe.ObjectName);
                    cpe.CounterName = TranslateCounterName(cpe.CounterName);

                    var sb = new StringBuilder(PDH_MAX_COUNTER_NAME);
                    var pathSize = (uint)sb.Capacity;

                    result = PdhMakeCounterPath(ref cpe, sb, ref pathSize, 0);

                    if (result == 0)
                        return sb.ToString().Substring(0, (int)pathSize - 1);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(structPointer);
            }

            // return original path if translation failed
            return path;
        }
    }
"@

if ( $IsWindows )
{
    Add-Type -TypeDefinition $helperSource
}

# Strip off machine name, if present, from counter path
function RemoveMachineName
{
    param (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [String]
        $path
    )

    if ($path.StartsWith("\\"))
    {
        return $path.SubString($path.IndexOf("\", 2))
    }
    else
    {
        return $path
    }
}

# Retrieve the counters array from the Registry
function GetCounters
{
    if ( $IsWindows )
    {
        $key = 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Perflib\CurrentLanguage'
        return (Get-ItemProperty -Path $key -Name Counter).Counter
    }
}

# Translate a counter name from English to a localized counter name
function TranslateCounterName
{
    param (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [String]
        $counterName
    )

    $counters = GetCounters
    if ($counters -and ($counters.Length -gt 1))
    {
        $counterHelper = New-Object -TypeName "TestCounterHelper" -ArgumentList (, $counters)
        return $counterHelper.TranslateCounterName($counterName)
    }

    return $counterName
}

# Translate a counter path from English to a localized counter path
function TranslateCounterPath
{
    param (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [String]
        $path
    )

    $counters = GetCounters
    if ($counters -and ($counters.Length -gt 1))
    {
        $counterHelper = New-Object -TypeName "TestCounterHelper" -ArgumentList (, $counters)
        $rv = $counterHelper.TranslateCounterPath($path)

        # if our original path had no machine name,
        # we don't want one on our translated path
        if (-not $path.StartsWith("\\"))
        {
            $rv = RemoveMachineName $rv
        }

        return $rv
    }

    return $path
}

# Compare two DateTime objects for relative equality.
#
# Exporting lops fractional milliseconds off the time stamp,
# so simply comparing the DateTime values isn't sufficient
function DateTimesAreEqualish
{
    param (
        [Parameter(Mandatory=$true)]
        [ValidateNotNull()]
        [DateTime]
        $dtA,
        [Parameter(Mandatory)]
        [ValidateNotNull()]
        [DateTime]
        $dtB
    )

    $span = $dtA - $dtB
    return ([math]::Floor([math]::Abs($span.TotalMilliseconds)) -eq 0)
}

# Compare the content of counter sets
function CompareCounterSets
{
    param (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        $setA,
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        $setB
    )

    $setA.Length | Should -Be $setB.Length

    # Depending on the kinds of counters used, the first record in
    # exported counters are likely to have embty items, so we'll
    # start comparing at the second item.
    #
    # Note that this is not a bug in either the cmdlets or the tests
    # script, but rather is the behavior of the underlying Windows
    # PDH functions that perform the actual exporting of counter data.
    for ($i = 1; $i -lt $setA.Length; $i++)
    {
        $setA[$i].CounterSamples.Length | Should -Be $setB[$i].CounterSamples.Length
        $samplesA = ($setA[$i].CounterSamples | Sort-Object -Property Path)
        $samplesB = ($setB[$i].CounterSamples | Sort-Object -Property Path)
        (DateTimesAreEqualish $setA[$i].TimeStamp $setB[$i].TimeStamp) | Should -BeTrue
        for ($j = 0; $j -lt $samplesA.Length; $j++)
        {
            $sampleA = $samplesA[$j]
            $sampleB = $samplesB[$j]
            (DateTimesAreEqualish $sampleA.TimeStamp $sampleB.TimeStamp) | Should -BeTrue
            $sampleA.Path | Should -BeExactly $sampleB.Path
            $sampleA.CookedValue | Should -Be $sampleB.CookedValue
        }
    }
}

function SkipCounterTests
{
    if ([System.Management.Automation.Platform]::IsLinux -or
        [System.Management.Automation.Platform]::IsMacOS -or
        [System.Management.Automation.Platform]::IsFreeBSD -or
        [System.Management.Automation.Platform]::IsIoT)
    {
        return $true
    }

    return $false
}
