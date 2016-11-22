# Create a helper class providing access to Pdh functions
$helperSource = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
public class TestCounterHelper
{
    internal sealed class PdhSafeDataSourceHandle : SafeHandle
    {
        private PdhSafeDataSourceHandle() : base(IntPtr.Zero, true) { }

        public override bool IsInvalid
        {
            get
            {
                return handle == IntPtr.Zero;
            }
        }


        //[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected override bool ReleaseHandle()
        {
            return (PdhCloseLog(handle, 0) == 0);
        }
    }

    [DllImport("pdh.dll")]
    internal static extern uint PdhCloseLog(IntPtr logHandle, uint dwFlags);

    [DllImport("pdh.dll")]
    private static extern uint PdhTranslateLocaleCounter(string counterPath, IntPtr fullPathname, ref int pathLength);

    [DllImport("pdh.dll")]
    private static extern uint PdhExpandWildCardPath (PdhSafeDataSourceHandle dataSource, string wildcardPath, IntPtr expandedPathList, ref IntPtr listLength, uint flags);

    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    private static extern uint PdhBindInputDataSource(out PdhSafeDataSourceHandle phDataSource, string szLogFileNameList);

    public const long PDH_MORE_DATA = 0x800007D2L;
    public const uint MB_PRECOMPOSED = 0x00000001;  // use precomposed chars
    public const uint MB_COMPOSITE = 0x00000002;  // use composite chars
    public const uint MB_USEGLYPHCHARS = 0x00000004;
    public const uint CP_UTF8 = 65001;

    public TestCounterHelper()
    {
    }

    public string TranslateCounterPath(string englishPath)
    {
        int     strSize = 0;
        string  localizedPath = "";

        uint res = PdhTranslateLocaleCounter(englishPath, IntPtr.Zero, ref strSize);

        if (PDH_MORE_DATA != res && 0 != res)
            return "";

        if (PDH_MORE_DATA == res)
        {
            IntPtr localizedPathPtr = Marshal.AllocHGlobal(strSize * sizeof(char));

            try
            {
                res = PdhTranslateLocaleCounter(englishPath, localizedPathPtr, ref strSize);
                if (res == 0)
                {
                    localizedPath = Marshal.PtrToStringAnsi (localizedPathPtr);
                }
            }
            catch (Exception)
            {
                localizedPath = "";
            }
            finally
            {
                Marshal.FreeHGlobal(localizedPathPtr);
            }
        }

        return localizedPath;
    }

    public string ExpandWildCardPath (string wildcard)
    {
        PdhSafeDataSourceHandle dataSource;
        String  pathList = "";

        uint res = PdhBindInputDataSource(out dataSource, null);

        if (res != 0)
        {
            //Console.WriteLine("error in PdhBindInputDataSource: " + res);
            return null;
        }

        IntPtr pcchPathListLength = new IntPtr(0);

        res = PdhExpandWildCardPath (dataSource,
                                    wildcard,
                                    IntPtr.Zero,
                                    ref pcchPathListLength,
                                    0);

        if (res != PDH_MORE_DATA)
        {
            return null;
        }

        Int32 cChars = pcchPathListLength.ToInt32();
        IntPtr strPathList = Marshal.AllocHGlobal(cChars * sizeof(char));

        try
        {
            res = PdhExpandWildCardPath (dataSource, wildcard, strPathList, ref pcchPathListLength, 0);
            if (res == 0)
                pathList = Marshal.PtrToStringAnsi (strPathList, pcchPathListLength.ToInt32 ());
        }
        finally
        {
            Marshal.FreeHGlobal (strPathList);
        }

        return pathList;
    }
}
"@
Add-Type -TypeDefinition $helperSource


function TranslateCounterPath($counterName)
{
    $counterHelper = New-Object -TypeName "TestCounterHelper"
    return $counterHelper.TranslateCounterPath($counterName)
}

function TranslateCounter($counterName)
{
    $beginName = $false;
    $translatedName = TranslateCounterPath($counterName)
    $returnString = ""
    $translatedNameArray = $translatedName.ToCharArray()

    foreach ($translatedChar in $translatedNameArray)
    {
        if ($translatedChar -eq '\' -and $beginName -eq $false)
        {
            $beginName = $true;
            continue;
        }

        if ($translatedChar -eq '\')
        {
            return $returnString
        }

        $returnString += $translatedChar
    }
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

function CompareCounterSets($setA, $setB)
{
    $setA.Length | Should Be $setB.Length

    # the first item exported always seems to have several empty values
    # when it should not, so we'll start at the second item
    for ($i = 1; $i -lt $setA.Length; $i++)
    {
        (DateTimesAreEqualish $setA[$i].TimeStamp $setB[$i].TimeStamp) | Should Be $true
    }
}