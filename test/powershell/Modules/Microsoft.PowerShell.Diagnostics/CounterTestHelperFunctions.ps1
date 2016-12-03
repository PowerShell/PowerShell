<############################################################################################
 # File: CounterTestHelperFunctions.ps1
 # Provides functions common to the performance counter Pester tests.
 ############################################################################################>

# Create a helper class providing facilities for parsing out and
# for re-assembling counter paths
$helperSource = @"
using System;
using System.Collections;
using System.Runtime.InteropServices;

    public class PerformanceCounterPathElements
    {
        public string MachineName;
        public string ObjectName;
        public string InstanceName;
        public string ParentInstance;
        public UInt32 InstanceIndex;
        public string CounterName;
    }

    public class TestCounterHelper
    {
        private const long PDH_MORE_DATA = 0x800007D2L;

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
                                                      IntPtr szFullPathBuffer,
                                                      ref IntPtr pcchBufferSize,
                                                      UInt32 dwFlags);

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        private static extern uint PdhParseCounterPath(string szFullPathBuffer,
                                                       IntPtr pCounterPathElements, //PDH_COUNTER_PATH_ELEMENTS
                                                       ref IntPtr pdwBufferSize,
                                                       uint dwFlags);

        public TestCounterHelper()
        {
        }

        // Parse a counter path and place its constituent parts
        // into a Hashtable, for use in PowerShell script code
        public PerformanceCounterPathElements ParseCounterPath(string path)
        {
            PerformanceCounterPathElements rv = null;
            var bufSize = new IntPtr(0);

            var result = PdhParseCounterPath(path,
                                             IntPtr.Zero,
                                             ref bufSize,
                                             0);
            if (result != 0 && result != PDH_MORE_DATA)
                return null;

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

                    rv = new PerformanceCounterPathElements
                        {
                            MachineName = cpe.MachineName,
                            ObjectName = cpe.ObjectName,
                            InstanceName = cpe.InstanceName,
                            ParentInstance = cpe.ParentInstance,
                            InstanceIndex = cpe.InstanceIndex,
                            CounterName = cpe.CounterName
                        };
                }
            }
            finally
            {
                Marshal.FreeHGlobal(structPointer);
            }

            return rv;
        }

        // Build a counter path and from a Hashtable containing
        // its constituent parts.
        // Note: This is NOT a general-use function, but it is
        //       sufficient for the limited use within the
        //       performance-counter testing PowerShell scripts.
        public string MakeCounterPath(PerformanceCounterPathElements parts)
        {
            string rv = null;
            PDH_COUNTER_PATH_ELEMENTS cpe = new PDH_COUNTER_PATH_ELEMENTS
                {
                    MachineName = parts.MachineName,
                    ObjectName = parts.ObjectName,
                    InstanceName = parts.InstanceName,
                    ParentInstance = parts.ParentInstance,
                    InstanceIndex = parts.InstanceIndex,
                    CounterName = parts.CounterName
                };

            var bufSize = new IntPtr(0);
            var result = PdhMakeCounterPath(ref cpe, IntPtr.Zero, ref bufSize, 0);

            if (result != PDH_MORE_DATA)
                return null;

            var nChars = bufSize.ToInt32();
            var pathPtr = Marshal.AllocHGlobal(nChars * sizeof(Char));

            try
            {
                result = PdhMakeCounterPath(ref cpe, pathPtr, ref bufSize, 0);

                if (result == 0)
                    rv = Marshal.PtrToStringUni(pathPtr);
            }
            finally
            {
                Marshal.FreeHGlobal(pathPtr);
            }

            return rv;
        }
    }
"@

Add-Type -TypeDefinition $helperSource

# Given a performance-counter ID, look up and return the localized name
#
# This function came from an article in PowerShellMagazine by Tobias Weltner
# http://www.powershellmagazine.com/2013/07/19/querying-performance-counters-from-powershell/
Function Get-PerformanceCounterLocalName
{
    param
    (
        [UInt32]
        $ID,

        $ComputerName = $env:COMPUTERNAME
    )

    $code = '[DllImport("pdh.dll", SetLastError=true, CharSet=CharSet.Unicode)] public static extern UInt32 PdhLookupPerfNameByIndex(string szMachineName, uint dwNameIndex, System.Text.StringBuilder szNameBuffer, ref uint pcchNameBufferSize);'

    $Buffer = New-Object System.Text.StringBuilder(1024)
    [UInt32]$BufferSize = $Buffer.Capacity

    $t = Add-Type -MemberDefinition $code -PassThru -Name PerfCounter -Namespace Utility
    $rv = $t::PdhLookupPerfNameByIndex($ComputerName, $id, $Buffer, [Ref]$BufferSize)

    if ($rv -eq 0)
    {
        $Buffer.ToString().Substring(0, $BufferSize-1)
    }
    else
    {
        Throw 'Get-PerformanceCounterLocalName : Unable to retrieve localized name. Check computer name and performance counter ID.'
    }
}

# Given a performance-counter name, look up and return the counter ID
#
# This function is a slightly modified version of the function originally published on
# PowerShellMagazine by Tobias Weltner
# http://www.powershellmagazine.com/2013/07/19/querying-performance-counters-from-powershell/
function Get-PerformanceCounterID
{
    param
    (
        [Parameter(Mandatory=$true)]
        $Name
    )

    if ($script:perfhash -eq $null)
    {
        $script:perfhash = @{}
    }

    if (-not $script:perfHash.$Name)
    {
        $key = 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Perflib\CurrentLanguage'
        $counters = (Get-ItemProperty -Path $key -Name Counter).Counter
        $all = $counters.Count

        for ($i = 0; $i -lt $all; $i += 2)
        {
            if ([string]::Compare($counters[$i + 1], $Name, $true) -eq 0)
            {
                $script:perfHash.$($counters[$i + 1]) = $counters[$i]
            }
        }
    }

    $script:perfHash.$Name
}

# Translate a counter path from "English" to a localized counter path
function TranslateCounterPath($path)
{
    $counterHelper = New-Object -TypeName "TestCounterHelper"
    $rv = $path;

    # crack the counter path
    $parts = $counterHelper.ParseCounterPath($path);

    if ($parts)
    {
        $objectName = $null
        $counterName = $null
        $id = Get-PerformanceCounterID $parts.ObjectName
        if ($id -ne $null)
        {
            $objectName = Get-PerformanceCounterLocalName $id
        }

        $id = Get-PerformanceCounterID $parts.CounterName
        if ($id -ne $null)
        {
            $counterName = Get-PerformanceCounterLocalName $id
        }

        if ($objectName -and $counterName)
        {
            # build a new path from translated names
            $parts.ObjectName = $objectName
            $parts.CounterName = $counterName

            $rv = $counterHelper.MakeCounterPath($parts)
            if (-not $rv)
            {
                $rv = $path
            }
            else
            {
                # If our original path did not include a machine name,
                # we don't want one in our translated path.
                #
                # This should have been possible by setting $parts.MachineName
                # to $null above, but that fails in the C# code  when run in PowerShell.
                if (-not $path.StartsWith("\\"))
                {
                    $rv = $rv.SubString($rv.IndexOf("\", 2))
                }
            }
        }
    }

    return $rv
}

# Compare two DateTime objects for relative equality.
#
# Exporting lops fractional milliseconds off the time stamp,
# so simply comparing the DateTime values isn't sufficient
function DateTimesAreEqualish($dtA, $dtB)
{
    $span = $dtA - $dtB
    return ([math]::Floor([math]::Abs($span.TotalMilliseconds)) -eq  0)
}

# Compare the content of counter sets
function CompareCounterSets($setA, $setB)
{
    $setA.Length | Should Be $setB.Length

    # the first item exported always seems to have several empty values
    # when it should not, so we'll start at the second item
    for ($i = 1; $i -lt $setA.Length; $i++)
    {
        $setA[$i].CounterSamples.Length | Should Be $setB[$i].CounterSamples.Length
        $samplesA = ($setA[$i].CounterSamples | sort -Property Path)
        $samplesB = ($setB[$i].CounterSamples | sort -Property Path)
        (DateTimesAreEqualish $setA[$i].TimeStamp $setB[$i].TimeStamp) | Should Be $true
        for ($j = 0; $j -lt $samplesA.Length; $j++)
        {
            $sampleA = $samplesA[$j]
            $sampleB = $samplesB[$j]
            (DateTimesAreEqualish $sampleA.TimeStamp $sampleB.TimeStamp) | Should Be $true
            $sampleA.Path | Should Be $sampleB.Path
            $sampleA.CookedValue | Should Be $sampleB.CookedValue
        }
    }
}
