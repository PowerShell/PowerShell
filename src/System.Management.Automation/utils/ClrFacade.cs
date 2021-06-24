// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace System.Management.Automation
{
    /// <summary>
    /// ClrFacade contains all diverging code (different implementation for FullCLR and CoreCLR using if/def).
    /// It exposes common APIs that can be used by the rest of the code base.
    /// </summary>
    internal static class ClrFacade
    {
        /// <summary>
        /// Initialize powershell AssemblyLoadContext and register the 'Resolving' event, if it's not done already.
        /// If powershell is hosted by a native host such as DSC, then PS ALC might be initialized via 'SetPowerShellAssemblyLoadContext' before loading S.M.A.
        /// </summary>
        static ClrFacade()
        {
            if (PowerShellAssemblyLoadContext.Instance == null)
            {
                PowerShellAssemblyLoadContext.InitializeSingleton(string.Empty);
            }
<<<<<<< HEAD
        }

        #region Assembly
=======
#else
            return process.Handle;
#endif
        }

#if CORECLR
        /// <summary>
        /// Facade for ProcessStartInfo.Environment
        /// </summary>
        internal static IDictionary<string, string> GetProcessEnvironment(ProcessStartInfo startInfo)
        {
            return startInfo.Environment;
        }
#else
        /// <summary>
        /// Facade for ProcessStartInfo.EnvironmentVariables
        /// </summary>
        internal static System.Collections.Specialized.StringDictionary GetProcessEnvironment(ProcessStartInfo startInfo)
        {
            return startInfo.EnvironmentVariables;
        }
#endif
#if CORECLR
        /// <summary>
        /// Converts the given SecureString to a string
        /// </summary>
        /// <param name="secureString"></param>
        /// <returns></returns>
        internal static string ConvertSecureStringToString(SecureString secureString)
        {
            string passwordInClearText;
            IntPtr unmanagedMemory = IntPtr.Zero;

            try
            {
                unmanagedMemory = SecureStringToCoTaskMemUnicode(secureString);
                passwordInClearText = Marshal.PtrToStringUni(unmanagedMemory);
            }
            finally
            {
                if (unmanagedMemory != IntPtr.Zero)
                {
                    ZeroFreeCoTaskMemUnicode(unmanagedMemory);
                }
            }
            return passwordInClearText;
        }
#endif

        #endregion Process

        #region Marshal

        /// <summary>
        /// Facade for Marshal.SizeOf
        /// </summary>
        internal static int SizeOf<T>()
        {
#if CORECLR
            // Marshal.SizeOf(Type) is obsolete in CoreCLR
            return Marshal.SizeOf<T>();
#else
            return Marshal.SizeOf(typeof(T));
#endif
        }

        /// <summary>
        /// Facade for Marshal.DestroyStructure
        /// </summary>
        internal static void DestroyStructure<T>(IntPtr ptr)
        {
#if CORECLR
            // Marshal.DestroyStructure(IntPtr, Type) is obsolete in CoreCLR
            Marshal.DestroyStructure<T>(ptr);
#else
            Marshal.DestroyStructure(ptr, typeof(T));
#endif
        }

        /// <summary>
        /// Facade for Marshal.PtrToStructure
        /// </summary>
        internal static T PtrToStructure<T>(IntPtr ptr)
        {
#if CORECLR
            // Marshal.PtrToStructure(IntPtr, Type) is obsolete in CoreCLR
            return Marshal.PtrToStructure<T>(ptr);
#else
            return (T)Marshal.PtrToStructure(ptr, typeof(T));
#endif
        }

        /// <summary>
        /// Wraps Marshal.StructureToPtr to hide differences between the CLRs.
        /// </summary>
        internal static void StructureToPtr<T>(
            T structure,
            IntPtr ptr,
            bool deleteOld)
        {
#if CORECLR
            Marshal.StructureToPtr<T>( structure, ptr, deleteOld );
#else
            Marshal.StructureToPtr(structure, ptr, deleteOld);
#endif
        }

        /// <summary>
        /// Needed to pair with the  SecureStringToCoTaskMemUnicode method which is member of 
        /// 'SecureStringMarshal' on CORE CLR, and member of 'Marshal' on Full CLR.
        /// </summary>
        internal static void ZeroFreeCoTaskMemUnicode(IntPtr unmanagedStr)
        {
#if CORECLR
            SecureStringMarshal.ZeroFreeCoTaskMemUnicode(unmanagedStr);
#else
            Marshal.ZeroFreeCoTaskMemUnicode(unmanagedStr);
#endif
        }

        /// <summary>
        /// Facade for SecureStringToCoTaskMemUnicode
        /// </summary>
        internal static IntPtr SecureStringToCoTaskMemUnicode(SecureString s)
        {
#if CORECLR
            return SecureStringMarshal.SecureStringToCoTaskMemUnicode(s);
#else
            return Marshal.SecureStringToCoTaskMemUnicode(s);
#endif
        }

        #endregion Marshal

        #region Assembly
        
        /// <summary>
        /// Facade for AssemblyName.GetAssemblyName(string)
        /// </summary>
        internal static AssemblyName GetAssemblyName(string assemblyPath)
        {
#if CORECLR // AssemblyName.GetAssemblyName(assemblyPath) is not in CoreCLR
            return AssemblyLoadContext.GetAssemblyName(assemblyPath);
#else
            return AssemblyName.GetAssemblyName(assemblyPath);
#endif
        }
>>>>>>> origin/source-depot

        internal static IEnumerable<Assembly> GetAssemblies(TypeResolutionState typeResolutionState, TypeName typeName)
        {
            string typeNameToSearch = typeResolutionState.GetAlternateTypeName(typeName.Name) ?? typeName.Name;
            return GetAssemblies(typeNameToSearch);
        }

        /// <summary>
        /// Facade for AppDomain.GetAssemblies.
        /// </summary>
        /// <param name="namespaceQualifiedTypeName">
        /// In CoreCLR context, if it's for string-to-type conversion and the namespace qualified type name is known, pass it in so that
        /// powershell can load the necessary TPA if the target type is from an unloaded TPA.
        /// </param>
        internal static IEnumerable<Assembly> GetAssemblies(string namespaceQualifiedTypeName = null)
        {
            return PSAssemblyLoadContext.GetAssembly(namespaceQualifiedTypeName) ?? GetPSVisibleAssemblies();
        }

        /// <summary>
        /// Return assemblies from the default load context and the 'individual' load contexts.
        /// The 'individual' load contexts are the ones holding assemblies loaded via 'Assembly.Load(byte[])' and 'Assembly.LoadFile'.
        /// Assemblies loaded in any custom load contexts are not consider visible to PowerShell to avoid type identity issues.
        /// </summary>
        private static IEnumerable<Assembly> GetPSVisibleAssemblies()
        {
            const string IndividualAssemblyLoadContext = "System.Runtime.Loader.IndividualAssemblyLoadContext";

            foreach (Assembly assembly in AssemblyLoadContext.Default.Assemblies)
            {
                if (!assembly.FullName.StartsWith(TypeDefiner.DynamicClassAssemblyFullNamePrefix, StringComparison.Ordinal))
                {
                    yield return assembly;
                }
            }
<<<<<<< HEAD

            foreach (AssemblyLoadContext context in AssemblyLoadContext.All)
            {
                if (IndividualAssemblyLoadContext.Equals(context.GetType().FullName, StringComparison.Ordinal))
                {
                    foreach (Assembly assembly in context.Assemblies)
                    {
                        yield return assembly;
                    }
                }
            }
=======
            
            return PSAssemblyLoadContext.ProbeAssemblyFileForMetadataAnalysis(assemblyShortName, additionalSearchPath);
>>>>>>> origin/source-depot
        }

        /// <summary>
        /// Get the namespace-qualified type names of all available .NET Core types shipped with PowerShell.
        /// This is used for type name auto-completion in PS engine.
        /// </summary>
        internal static IEnumerable<string> AvailableDotNetTypeNames => PSAssemblyLoadContext.AvailableDotNetTypeNames;

        /// <summary>
        /// Get the assembly names of all available .NET Core assemblies shipped with PowerShell.
        /// This is used for type name auto-completion in PS engine.
        /// </summary>
        internal static HashSet<string> AvailableDotNetAssemblyNames => PSAssemblyLoadContext.AvailableDotNetAssemblyNames;

<<<<<<< HEAD
        private static PowerShellAssemblyLoadContext PSAssemblyLoadContext => PowerShellAssemblyLoadContext.Instance;
=======
        /// <summary>
        /// Add the AssemblyLoad handler
        /// </summary>
        internal static void AddAssemblyLoadHandler(Action<Assembly> handler)
        {
            PSAssemblyLoadContext.AssemblyLoad += handler;
        }

        private static volatile PowerShellAssemblyLoadContext _psLoadContext;
        private static PowerShellAssemblyLoadContext PSAssemblyLoadContext
        {
            get
            {
                if (_psLoadContext == null)
                {
                    _psLoadContext = AssemblyLoadContext.Default as PowerShellAssemblyLoadContext;
                    if (_psLoadContext == null)
                    {
                        throw new InvalidOperationException(ParserStrings.InvalidAssemblyLoadContextInUse);
                    }
                }
                return _psLoadContext;
            }
        }
#endif

        /// <summary>
        /// Facade for Assembly.GetCustomAttributes
        /// </summary>
        internal static object[] GetCustomAttributes<T>(Assembly assembly)
        {
#if CORECLR // Assembly.GetCustomAttributes(Type, Boolean) is not in CORE CLR
            return assembly.GetCustomAttributes(typeof(T)).ToArray();
#else
            return assembly.GetCustomAttributes(typeof(T), false);
#endif
        }
>>>>>>> origin/source-depot

        #endregion Assembly

        #region Encoding

        /// <summary>
        /// Facade for getting default encoding.
        /// </summary>
        internal static Encoding GetDefaultEncoding()
        {
            if (s_defaultEncoding == null)
            {
                // load all available encodings
                EncodingRegisterProvider();
                s_defaultEncoding = new UTF8Encoding(false);
            }

            return s_defaultEncoding;
        }

        private static volatile Encoding s_defaultEncoding;

        /// <summary>
        /// Facade for getting OEM encoding
        /// OEM encodings work on all platforms, or rather codepage 437 is available on both Windows and Non-Windows.
        /// </summary>
        internal static Encoding GetOEMEncoding()
        {
            if (s_oemEncoding == null)
            {
                // load all available encodings
                EncodingRegisterProvider();
#if UNIX
                s_oemEncoding = new UTF8Encoding(false);
#else
                uint oemCp = NativeMethods.GetOEMCP();
                s_oemEncoding = Encoding.GetEncoding((int)oemCp);
#endif
            }

            return s_oemEncoding;
        }

        private static volatile Encoding s_oemEncoding;

        private static void EncodingRegisterProvider()
        {
            if (s_defaultEncoding == null && s_oemEncoding == null)
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            }
        }

        #endregion Encoding

#if !UNIX
        #region Security

        /// <summary>
        /// Facade to get the SecurityZone information of a file.
        /// </summary>
        internal static SecurityZone GetFileSecurityZone(string filePath)
        {
            Diagnostics.Assert(Path.IsPathRooted(filePath), "Caller makes sure the path is rooted.");
            Diagnostics.Assert(File.Exists(filePath), "Caller makes sure the file exists.");
            return MapSecurityZone(filePath);
        }

        /// <summary>
        /// Map the file to SecurityZone.
        /// </summary>
        /// <remarks>
        /// The algorithm is as follows:
        ///
        /// 1. Alternate data stream "Zone.Identifier" is checked first. If this alternate data stream has content, then the content is parsed to determine the SecurityZone.
        /// 2. If the alternate data stream "Zone.Identifier" doesn't exist, or its content is not expected, then the file path will be analyzed to determine the SecurityZone.
        ///
        /// For #1, the parsing rules are observed as follows:
        ///   A. Read content of the data stream line by line. Each line is trimmed.
        ///   B. Try to match the current line with '^\[ZoneTransfer\]'.
        ///        - if matching, then do step (#C) starting from the next line
        ///        - if not matching, then continue to do step (#B) with the next line.
        ///   C. Try to match the current line with '^ZoneId\s*=\s*(.*)'
        ///        - if matching, check if the ZoneId is valid. Then return the corresponding SecurityZone if the 'ZoneId' is valid, or 'NoZone' if invalid.
        ///        - if not matching, then continue to do step (#C) with the next line.
        ///   D. Reach EOF, then return 'NoZone'.
        /// After #1, if the returned SecurityZone is 'NoZone', then proceed with #2. Otherwise, return it as the mapping result.
        ///
        /// For #2, the analysis rules are observed as follows:
        ///   A. If the path is a UNC path, then
        ///       - if the host name of the UNC path is IP address, then mapping it to "Internet" zone.
        ///       - if the host name of the UNC path has dot (.) in it, then mapping it to "internet" zone.
        ///       - otherwise, mapping it to "intranet" zone.
        ///   B. If the path is not UNC path, then get the root drive,
        ///       - if the drive is CDRom, mapping it to "Untrusted" zone
        ///       - if the drive is Network, mapping it to "Intranet" zone
        ///       - otherwise, mapping it to "MyComputer" zone.
        ///
        /// The above algorithm has two changes comparing to the behavior of "Zone.CreateFromUrl" I observed:
        ///   (1) If a file downloaded from internet (ZoneId=3) is not on the local machine, "Zone.CreateFromUrl" won't respect the MOTW.
        ///       I think it makes more sense for powershell to always check the MOTW first, even for files not on local box.
        ///   (2) When it's a UNC path and is actually a loopback (\\127.0.0.1\c$\test.txt), "Zone.CreateFromUrl" returns "Internet", but
        ///       the above algorithm changes it to be "MyComputer" because it's actually the same computer.
        /// </remarks>
        private static SecurityZone MapSecurityZone(string filePath)
        {
            // WSL introduces a new filesystem path to access the Linux filesystem from Windows, like '\\wsl$\ubuntu'.
            // If the given file path is such a special case, we consider it's in 'MyComputer' zone.
            if (filePath.StartsWith(Utils.WslRootPath, StringComparison.OrdinalIgnoreCase))
            {
                return SecurityZone.MyComputer;
            }

            SecurityZone reval = ReadFromZoneIdentifierDataStream(filePath);
            if (reval != SecurityZone.NoZone)
            {
                return reval;
            }

            // If it reaches here, then we either couldn't get the ZoneId information, or the ZoneId is invalid.
            // In this case, we try to determine the SecurityZone by analyzing the file path.
            Uri uri = new Uri(filePath);
            if (uri.IsUnc)
            {
                if (uri.IsLoopback)
                {
                    return SecurityZone.MyComputer;
                }

                if (uri.HostNameType == UriHostNameType.IPv4 ||
                    uri.HostNameType == UriHostNameType.IPv6)
                {
                    return SecurityZone.Internet;
                }

                // This is also an observation of Zone.CreateFromUrl/Zone.SecurityZone. If the host name
                // has 'dot' in it, the file will be treated as in Internet security zone. Otherwise, it's
                // in Intranet security zone.
                string hostName = uri.Host;
                return hostName.Contains('.') ? SecurityZone.Internet : SecurityZone.Intranet;
            }

            string root = Path.GetPathRoot(filePath);
            DriveInfo drive = new DriveInfo(root);
            switch (drive.DriveType)
            {
                case DriveType.NoRootDirectory:
                case DriveType.Unknown:
                case DriveType.CDRom:
                    return SecurityZone.Untrusted;
                case DriveType.Network:
                    return SecurityZone.Intranet;
                default:
                    return SecurityZone.MyComputer;
            }
        }

        /// <summary>
        /// Read the 'Zone.Identifier' alternate data stream to determin SecurityZone of the file.
        /// </summary>
        private static SecurityZone ReadFromZoneIdentifierDataStream(string filePath)
        {
            if (!AlternateDataStreamUtilities.TryCreateFileStream(filePath, "Zone.Identifier", FileMode.Open, FileAccess.Read, FileShare.Read, out var zoneDataStream))
            {
                return SecurityZone.NoZone;
            }

            // If we successfully get the zone data stream, try to read the ZoneId information
            using (StreamReader zoneDataReader = new StreamReader(zoneDataStream, GetDefaultEncoding()))
            {
                string line = null;
                bool zoneTransferMatched = false;

                // After a lot experiments with Zone.CreateFromUrl/Zone.SecurityZone, the way it handles the alternate
                // data stream 'Zone.Identifier' is observed as follows:
                //    1. Read content of the data stream line by line. Each line is trimmed.
                //    2. Try to match the current line with '^\[ZoneTransfer\]'.
                //           - if matching, then do step #3 starting from the next line
                //           - if not matching, then continue to do step #2 with the next line.
                //    3. Try to match the current line with '^ZoneId\s*=\s*(.*)'
                //           - if matching, check if the ZoneId is valid. Then return the corresponding SecurityZone if valid, or 'NoZone' if invalid.
                //           - if not matching, then continue to do step #3 with the next line.
                //    4. Reach EOF, then return 'NoZone'.
                while ((line = zoneDataReader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (!zoneTransferMatched)
                    {
                        zoneTransferMatched = Regex.IsMatch(line, @"^\[ZoneTransfer\]", RegexOptions.IgnoreCase);
                    }
                    else
                    {
                        Match match = Regex.Match(line, @"^ZoneId\s*=\s*(.*)", RegexOptions.IgnoreCase);
                        if (!match.Success) { continue; }

                        // Match found. Validate ZoneId value.
                        string zoneIdRawValue = match.Groups[1].Value;
                        match = Regex.Match(zoneIdRawValue, @"^[+-]?\d+", RegexOptions.IgnoreCase);
                        if (!match.Success) { return SecurityZone.NoZone; }

                        string zoneId = match.Groups[0].Value;
                        SecurityZone result;
                        return LanguagePrimitives.TryConvertTo(zoneId, out result) ? result : SecurityZone.NoZone;
                    }
                }
            }

            return SecurityZone.NoZone;
        }

        #endregion Security
#endif

        #region Misc

        /// <summary>
        /// Facade for Directory.GetParent(string)
        /// </summary>
        internal static DirectoryInfo GetParent(string path)
        {
#if CORECLR
            // Implementation copied from .NET source code.
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            string fullPath = Path.GetFullPath(path);

            string s = Path.GetDirectoryName(fullPath);
            if (s == null)
                return null;
            return new DirectoryInfo(s);
#else
            return Directory.GetParent(path);
#endif
        }

        /// <summary>
        /// Facade for ManagementDateTimeConverter.ToDmtfDateTime(DateTime)
        /// </summary>
        internal static string ToDmtfDateTime(DateTime date)
        {
#if CORECLR
            // This implementation is copied from ManagementDateTimeConverter.ToDmtfDateTime(DateTime date) with a minor adjustment:
            // Use TimeZoneInfo.Local instead of TimeZone.CurrentTimeZone. System.TimeZone is not in CoreCLR.
            // According to MSDN, CurrentTimeZone property corresponds to the TimeZoneInfo.Local property, and
            // it's recommended to use TimeZoneInfo.Local whenever possible.

            const int maxsizeUtcDmtf = 999;
            string UtcString = string.Empty;
            // Fill up the UTC field in the DMTF date with the current zones UTC value
            TimeZoneInfo curZone = TimeZoneInfo.Local;
            TimeSpan tickOffset = curZone.GetUtcOffset(date);
            long OffsetMins = (tickOffset.Ticks / TimeSpan.TicksPerMinute);
            IFormatProvider frmInt32 = (IFormatProvider)CultureInfo.InvariantCulture.GetFormat(typeof(int));

            // If the offset is more than that what can be specified in DMTF format, then
            // convert the date to UniversalTime
            if (Math.Abs(OffsetMins) > maxsizeUtcDmtf)
            {
                date = date.ToUniversalTime();
                UtcString = "+000";
            }
            else
                if ((tickOffset.Ticks >= 0))
            {
                UtcString = "+" + ((tickOffset.Ticks / TimeSpan.TicksPerMinute)).ToString(frmInt32).PadLeft(3, '0');
            }
            else
            {
                string strTemp = OffsetMins.ToString(frmInt32);
                UtcString = "-" + strTemp.Substring(1, strTemp.Length - 1).PadLeft(3, '0');
            }

            string dmtfDateTime = date.Year.ToString(frmInt32).PadLeft(4, '0');

            dmtfDateTime += date.Month.ToString(frmInt32).PadLeft(2, '0');
            dmtfDateTime += date.Day.ToString(frmInt32).PadLeft(2, '0');
            dmtfDateTime += date.Hour.ToString(frmInt32).PadLeft(2, '0');
            dmtfDateTime += date.Minute.ToString(frmInt32).PadLeft(2, '0');
            dmtfDateTime += date.Second.ToString(frmInt32).PadLeft(2, '0');
            dmtfDateTime += ".";

            // Construct a DateTime with with the precision to Second as same as the passed DateTime and so get
            // the ticks difference so that the microseconds can be calculated
            DateTime dtTemp = new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, 0);
            Int64 microsec = ((date.Ticks - dtTemp.Ticks) * 1000) / TimeSpan.TicksPerMillisecond;

            // fill the microseconds field
            string strMicrosec = microsec.ToString((IFormatProvider)CultureInfo.InvariantCulture.GetFormat(typeof(Int64)));
            if (strMicrosec.Length > 6)
            {
                strMicrosec = strMicrosec.Substring(0, 6);
            }

            dmtfDateTime += strMicrosec.PadLeft(6, '0');
            // adding the UTC offset
            dmtfDateTime += UtcString;

            return dmtfDateTime;
#else
            return ManagementDateTimeConverter.ToDmtfDateTime(date);
#endif
        }

<<<<<<< HEAD
=======
        /// <summary>
        /// Manual implementation of the is 64bit processor check
        /// </summary>
        /// <returns></returns>
        internal static bool Is64BitOperatingSystem()
        {
#if CORECLR
            return (8 == IntPtr.Size); // Pointers are 8 bytes on 64-bit machines
#else
            return Environment.Is64BitOperatingSystem;
#endif
        }

        /// <summary>
        /// Facade for FormatterServices.GetUninitializedObject.
        /// 
        /// In CORECLR, there are two peculiarities with its implementation that affect our own:
        /// 1. Structures cannot be instantiated using GetConstructor, so they must be filtered out.
        /// 2. Classes must have a default constructor implemented for GetContructor to work.
        /// 
        /// See RemoteHostEncoder.IsEncodingAllowedForClassOrStruct for a list of the required types.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static object GetUninitializedObject(Type type)
        {
#if CORECLR
            switch (type.Name)
            {
                case "KeyInfo"://typeof(KeyInfo).Name:
                    return new KeyInfo(0, ' ', ControlKeyStates.RightAltPressed, false);
                case "Coordinates"://typeof(Coordinates).Name:
                    return new Coordinates(0, 0);
                case "Size"://typeof(Size).Name:
                    return new Size(0, 0);
                case "BufferCell"://typeof(BufferCell).Name:
                    return new BufferCell(' ', ConsoleColor.Black, ConsoleColor.Black, BufferCellType.Complete);
                case "Rectangle"://typeof(Rectangle).Name:
                    return new Rectangle(0, 0, 0, 0);
                default:
                    ConstructorInfo constructorInfoObj = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);
                    if (constructorInfoObj != null)
                    {
                        return constructorInfoObj.Invoke(new object[] { });
                    }
                    return new object();
            }
#else
            return FormatterServices.GetUninitializedObject(type);
#endif
        }

        /// <summary>
        /// Facade for setting WaitHandle.SafeWaitHandle.
        /// </summary>
        /// <param name="waitHandle"></param>
        /// <param name="value"></param>
        internal static void SetSafeWaitHandle(WaitHandle waitHandle, SafeWaitHandle value)
        {
#if CORECLR
            waitHandle.SetSafeWaitHandle(value);
#else
            waitHandle.SafeWaitHandle = value;
#endif
        }

        /// <summary>
        /// Facade for ProfileOptimization.SetProfileRoot
        /// </summary>
        /// <param name="directoryPath">The full path to the folder where profile files are stored for the current application domain.</param>
        internal static void SetProfileOptimizationRoot(string directoryPath)
        {
#if CORECLR
            System.Runtime.Loader.AssemblyLoadContext.Default.SetProfileOptimizationRoot(directoryPath);
#else
            System.Runtime.ProfileOptimization.SetProfileRoot(directoryPath);
#endif
        }

        /// <summary>
        /// Facade for ProfileOptimization.StartProfile
        /// </summary>
        /// <param name="profile">The file name of the profile to use.</param>
        internal static void StartProfileOptimization(string profile)
        {
#if CORECLR
            System.Runtime.Loader.AssemblyLoadContext.Default.StartProfileOptimization(profile);
#else
            System.Runtime.ProfileOptimization.StartProfile(profile);
#endif
        }

>>>>>>> origin/source-depot
        #endregion Misc

        /// <summary>
        /// Native methods that are used by facade methods.
        /// </summary>
        private static class NativeMethods
        {
            /// <summary>
            /// Pinvoke for GetOEMCP to get the OEM code page.
            /// </summary>
            [DllImport(PinvokeDllNames.GetOEMCPDllName, SetLastError = false, CharSet = CharSet.Unicode)]
            internal static extern uint GetOEMCP();
        }
    }
}
