//
//    Copyright (C) Microsoft.  All rights reserved.
//

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation.Language;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Security;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices.ComTypes;

#if CORECLR
using System.Runtime.Loader; /* used in facade APIs related to assembly operations */
using System.Management.Automation.Host;          /* used in facade API 'GetUninitializedObject' */
using Microsoft.PowerShell.CoreClr.Stubs;         /* used in facade API 'GetFileSecurityZone' */
using System.Management.Automation.Internal;
using System.Text.RegularExpressions;
#else
using System.Runtime.Serialization;           /* used in facade API 'GetUninitializedObject' */
#endif
#if !UNIX
using System.ComponentModel;                  /* used in the facade API RetrieveProcessUserName */
using Microsoft.PowerShell.Commands.Internal; /* used in the facade APIs related to 'SafeProcessHandle' and 'RetreiveUserName' */
#endif

namespace System.Management.Automation
{
    /// <summary>
    /// ClrFacade contains all diverging code (different implementation for FullCLR and CoreCLR using if/def).
    /// It exposes common APIs that can be used by the rest of the code base.
    /// </summary>
    internal static class ClrFacade
    {
        /// <summary>
        /// We need it to avoid calling lookups inside dynamic assemblies with PS Types, so we exclude it from GetAssemblies().
        /// We use this convention for names to archive it.
        /// </summary>
        internal static readonly char FIRST_CHAR_PSASSEMBLY_MARK = (char)0x29f9;

        #region Type

        /// <summary>
        /// Facade for Type.GetMember(string, BindingFlags) to return multiple matched Public Static methods
        /// </summary>
        internal static MemberInfo[] GetMethods(Type targetType, string methodName)
        {
#if CORECLR
            const BindingFlags flagsToUse = BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Static;
            return targetType.GetMethods(methodName, flagsToUse);
#else
            const BindingFlags flagsToUse = BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod;
            return targetType.GetMember(methodName, flagsToUse);
#endif
        }

        #endregion Type

        #region Process

        /// <summary>
        /// Facade for ProcessModule FileVersionInfo
        /// </summary>
        /// <param name="processModule"></param>
        /// <returns>FileVersionInfo</returns>
        internal static FileVersionInfo GetProcessModuleFileVersionInfo(ProcessModule processModule)
        {
#if CORECLR
            return FileVersionInfo.GetVersionInfo(processModule.FileName);
#else
            return processModule.FileVersionInfo;
#endif
        }

        /// <summary>
        /// Facade for Process.Handle to get SafeHandle
        /// </summary>
        /// <param name="process"></param>
        /// <returns>SafeHandle</returns>
        internal static SafeHandle GetSafeProcessHandle(Process process)
        {
#if CORECLR
            return process.SafeHandle;
#else
            return new SafeProcessHandle(process.Handle);
#endif
        }

        /// <summary>
        /// Facade for Process.Handle to get raw handle
        /// </summary>
        internal static IntPtr GetRawProcessHandle(Process process)
        {
#if CORECLR
            try
            {
                return process.SafeHandle.DangerousGetHandle();
            }
            catch (InvalidOperationException)
            {
                // It's possible that the process has already exited when we try to get its handle.
                // In that case, InvalidOperationException will be thrown from Process.SafeHandle,
                // and we return the invalid zero pointer.
                return IntPtr.Zero;
            }
#else
            return process.Handle;
#endif
        }

#region Facade for AddProcessProperties

        /// <summary>
        /// Ensures the 'UserName' and 'HandleCount' Properties exist the Process object.
        /// </summary>
        /// <param name="process"></param>
        /// <param name="includeUserName"></param>
        /// <returns></returns>
        internal static PSObject AddProcessProperties(bool includeUserName, Process process)
        {
            PSObject processAsPsobj = includeUserName ? AddUserNameToProcess(process) : PSObject.AsPSObject(process);
#if CORECLR
            // In CoreCLR, the System.Diagnostics.Process.HandleCount property does not exist.
            // I am adding a note property HandleCount and temporarily setting it to zero.
            // This issue will be fix for RTM and it is tracked by 5024994: Get-process does not populate the Handles field.
            PSMemberInfo hasHandleCount = processAsPsobj.Properties["HandleCount"];
            if (hasHandleCount == null)
            {
                PSNoteProperty noteProperty = new PSNoteProperty("HandleCount", 0);
                processAsPsobj.Properties.Add(noteProperty, true);
                processAsPsobj.TypeNames.Insert(0, "System.Diagnostics.Process#HandleCount");
            }
#endif
            return processAsPsobj;
        }
        /// <summary>
        /// New PSTypeName added to the process object
        /// </summary>
        private const string TypeNameForProcessWithUserName = "System.Diagnostics.Process#IncludeUserName";

        /// <summary>
        /// Add the 'UserName' NoteProperty to the Process object
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        private static PSObject AddUserNameToProcess(Process process)
        {
            // Return null if we failed to get the owner information
            string userName = ClrFacade.RetrieveProcessUserName(process);

            PSObject processAsPsobj = PSObject.AsPSObject(process);
            PSNoteProperty noteProperty = new PSNoteProperty("UserName", userName);

            processAsPsobj.Properties.Add(noteProperty, true);
            processAsPsobj.TypeNames.Insert(0, TypeNameForProcessWithUserName);

            return processAsPsobj;
        }


        /// <summary>
        /// Retrieve the UserName through PInvoke
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        private static string RetrieveProcessUserName(Process process)
        {
            string userName = null;
#if UNIX
            userName = Platform.NonWindowsGetUserFromPid(process.Id);
#else
            IntPtr tokenUserInfo = IntPtr.Zero;
            IntPtr processTokenHandler = IntPtr.Zero;

            const uint TOKEN_QUERY = 0x0008;

            try
            {
                do
                {
                    int error;
                    if (!Win32Native.OpenProcessToken(ClrFacade.GetSafeProcessHandle(process), TOKEN_QUERY, out processTokenHandler)) { break; }

                    // Set the default length to be 256, so it will be sufficient for most cases
                    int tokenInfoLength = 256;
                    tokenUserInfo = Marshal.AllocHGlobal(tokenInfoLength);
                    if (!Win32Native.GetTokenInformation(processTokenHandler, Win32Native.TOKEN_INFORMATION_CLASS.TokenUser, tokenUserInfo, tokenInfoLength, out tokenInfoLength))
                    {
                        error = Marshal.GetLastWin32Error();
                        if (error == Win32Native.ERROR_INSUFFICIENT_BUFFER)
                        {
                            Marshal.FreeHGlobal(tokenUserInfo);
                            tokenUserInfo = Marshal.AllocHGlobal(tokenInfoLength);

                            if (!Win32Native.GetTokenInformation(processTokenHandler, Win32Native.TOKEN_INFORMATION_CLASS.TokenUser, tokenUserInfo, tokenInfoLength, out tokenInfoLength)) { break; }
                        }
                        else
                        {
                            break;
                        }
                    }

                    var tokenUser = ClrFacade.PtrToStructure<Win32Native.TOKEN_USER>(tokenUserInfo);

                    // Set the default length to be 256, so it will be sufficient for most cases
                    int userNameLength = 256, domainNameLength = 256;
                    var userNameStr = new StringBuilder(userNameLength);
                    var domainNameStr = new StringBuilder(domainNameLength);
                    Win32Native.SID_NAME_USE accountType;

                    if (!Win32Native.LookupAccountSid(null, tokenUser.User.Sid, userNameStr, ref userNameLength, domainNameStr, ref domainNameLength, out accountType))
                    {
                        error = Marshal.GetLastWin32Error();
                        if (error == Win32Native.ERROR_INSUFFICIENT_BUFFER)
                        {
                            userNameStr.EnsureCapacity(userNameLength);
                            domainNameStr.EnsureCapacity(domainNameLength);

                            if (!Win32Native.LookupAccountSid(null, tokenUser.User.Sid, userNameStr, ref userNameLength, domainNameStr, ref domainNameLength, out accountType)) { break; }
                        }
                        else
                        {
                            break;
                        }
                    }

                    userName = domainNameStr + "\\" + userNameStr;
                } while (false);
            }
            catch (NotSupportedException)
            {
                // The Process not started yet, or it's a process from a remote machine
            }
            catch (InvalidOperationException)
            {
                // The Process has exited, Process.Handle will raise this exception
            }
            catch (Win32Exception)
            {
                // We might get an AccessDenied error
            }
            catch (Exception)
            {
                // I don't expect to get other exceptions,
            }
            finally
            {
                if (tokenUserInfo != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(tokenUserInfo);
                }

                if (processTokenHandler != IntPtr.Zero)
                {
                    Win32Native.CloseHandle(processTokenHandler);
                }
            }

#endif
            return userName;
        }

#endregion

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
                    Marshal.ZeroFreeCoTaskMemUnicode(unmanagedMemory);
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
            Marshal.StructureToPtr<T>(structure, ptr, deleteOld);
#else
            Marshal.StructureToPtr(structure, ptr, deleteOld);
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

        internal static IEnumerable<Assembly> GetAssemblies(TypeResolutionState typeResolutionState, TypeName typeName)
        {
#if CORECLR
            string typeNameToSearch = typeResolutionState.GetAlternateTypeName(typeName.Name) ?? typeName.Name;
            return GetAssemblies(typeNameToSearch);
#else
            return GetAssemblies();
#endif
        }

        /// <summary>
        /// Facade for AppDomain.GetAssemblies
        /// </summary>
        /// <param name="namespaceQualifiedTypeName">
        /// In CoreCLR context, if it's for string-to-type conversion and the namespace qualified type name is known, pass it in so that
        /// powershell can load the necessary TPA if the target type is from an unloaded TPA.
        /// </param>
        internal static IEnumerable<Assembly> GetAssemblies(string namespaceQualifiedTypeName = null)
        {
#if CORECLR
            return PSAssemblyLoadContext.GetAssemblies(namespaceQualifiedTypeName);
#else
            return AppDomain.CurrentDomain.GetAssemblies().Where(a => !(a.FullName.Length > 0 && a.FullName[0] == FIRST_CHAR_PSASSEMBLY_MARK));
#endif
        }

        /// <summary>
        /// Facade for Assembly.LoadFrom
        /// </summary>
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom")]
        internal static Assembly LoadFrom(string assemblyPath)
        {
#if CORECLR
            return PSAssemblyLoadContext.LoadFrom(assemblyPath);
#else
            return Assembly.LoadFrom(assemblyPath);
#endif
        }

        /// <summary>
        /// Facade for EnumBuilder.CreateTypeInfo
        /// </summary>
        /// <remarks>
        /// In Core PowerShell, we need to track the dynamic assemblies that powershell generates.
        /// </remarks>
        internal static void CreateEnumType(EnumBuilder enumBuilder)
        {
#if CORECLR
            // Create the enum type and add the dynamic assembly to assembly cache.
            TypeInfo enumTypeinfo = enumBuilder.CreateTypeInfo();
            PSAssemblyLoadContext.TryAddAssemblyToCache(enumTypeinfo.Assembly);
#else
            enumBuilder.CreateTypeInfo();
#endif
        }

#if CORECLR
        /// <summary>
        /// Probe (look for) the assembly file with the specified short name.
        /// </summary>
        /// <remarks>
        /// In Core PowerShell, we need to analyze the metadata of assembly files for binary modules. Sometimes we
        /// need to find an assembly file that is referenced by the assembly file that is being processed. To find
        /// the reference assembly file, we need to probe the PSBase and the additional searching path if it's specified.
        /// </remarks>
        internal static string ProbeAssemblyPath(string assemblyShortName, string additionalSearchPath = null)
        {
            if (string.IsNullOrWhiteSpace(assemblyShortName))
            {
                throw new ArgumentNullException("assemblyShortName");
            }

            return PSAssemblyLoadContext.ProbeAssemblyFileForMetadataAnalysis(assemblyShortName, additionalSearchPath);
        }

        /// <summary>
        /// Get the namespace-qualified type names of all available CoreCLR .NET types.
        /// This is used for type name auto-completion in PS engine.
        /// </summary>
        internal static IEnumerable<string> GetAvailableCoreClrDotNetTypes()
        {
            return PSAssemblyLoadContext.GetAvailableDotNetTypes();
        }

        /// <summary>
        /// Load assembly from byte stream.
        /// </summary>
        internal static Assembly LoadFrom(Stream assembly)
        {
            return PSAssemblyLoadContext.LoadFrom(assembly);
        }

        /// <summary>
        /// Add the AssemblyLoad handler
        /// </summary>
        internal static void AddAssemblyLoadHandler(Action<Assembly> handler)
        {
            PSAssemblyLoadContext.AssemblyLoad += handler;
        }

        private static PowerShellAssemblyLoadContext PSAssemblyLoadContext
        {
            get
            {
                if (PowerShellAssemblyLoadContext.Instance == null)
                {
                    throw new InvalidOperationException(ParserStrings.LoadContextNotInitialized);
                }
                return PowerShellAssemblyLoadContext.Instance;
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

        #endregion Assembly

        #region Encoding

        /// <summary>
        /// Facade for Encoding.Default
        /// </summary>
        internal static Encoding GetDefaultEncoding()
        {
            if (s_defaultEncoding == null)
            {
#if CORECLR     // Encoding.Default is not in CoreCLR
                // As suggested by CoreCLR team (tarekms), use latin1 (ISO-8859-1, CodePage 28591) as the default encoding.
                // We will revisit this if it causes any failures when running tests on Core PS.
                s_defaultEncoding = Encoding.GetEncoding(28591);
#else
                s_defaultEncoding = Encoding.Default;
#endif
            }
            return s_defaultEncoding;
        }
        private static volatile Encoding s_defaultEncoding;

        /// <summary>
        /// Facade for getting OEM encoding
        /// </summary>
        internal static Encoding GetOEMEncoding()
        {
            if (s_oemEncoding == null)
            {
#if CORECLR     // The OEM code page '437' is not supported by CoreCLR.
                // Use the default encoding (ISO-8859-1, CodePage 28591) as the OEM encoding in OneCore powershell.
                s_oemEncoding = GetDefaultEncoding();
#else
                uint oemCp = NativeMethods.GetOEMCP();
                s_oemEncoding = Encoding.GetEncoding((int)oemCp);
#endif
            }
            return s_oemEncoding;
        }
        private static volatile Encoding s_oemEncoding;

        #endregion Encoding

        #region Security

        /// <summary>
        /// Facade to get the SecurityZone information of a file.
        /// </summary>
        internal static SecurityZone GetFileSecurityZone(string filePath)
        {
            Diagnostics.Assert(Path.IsPathRooted(filePath), "Caller makes sure the path is rooted.");
            Diagnostics.Assert(Utils.NativeFileExists(filePath), "Caller makes sure the file exists.");
#if CORECLR
            string sysRoot = System.Environment.GetEnvironmentVariable("SystemRoot");
            string urlmonPath = Path.Combine(sysRoot, @"System32\urlmon.dll");
            if (Utils.NativeFileExists(urlmonPath))
            {
                return MapSecurityZoneWithUrlmon(filePath);
            }
            return MapSecurityZoneWithoutUrlmon(filePath);
#else
            return MapSecurityZoneWithUrlmon(filePath);
#endif
        }

#if CORECLR
        #region WithoutUrlmon

        /// <summary>
        /// Map the file to SecurityZone without using urlmon.dll.
        /// This is needed on NanoServer because urlmon.dll is not in OneCore.
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
        private static SecurityZone MapSecurityZoneWithoutUrlmon(string filePath)
        {
            SecurityZone reval = ReadFromZoneIdentifierDataStream(filePath);
            if (reval != SecurityZone.NoZone) { return reval; }

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
                return hostName.IndexOf('.') == -1 ? SecurityZone.Intranet : SecurityZone.Internet;
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
            try
            {
                FileStream zoneDataSteam = AlternateDataStreamUtilities.CreateFileStream(
                                            filePath, "Zone.Identifier", FileMode.Open,
                                            FileAccess.Read, FileShare.Read);

                // If we successfully get the zone data stream, try to read the ZoneId information
                using (StreamReader zoneDataReader = new StreamReader(zoneDataSteam, GetDefaultEncoding()))
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
            }
            catch (FileNotFoundException)
            {
                // FileNotFoundException may be thrown by AlternateDataStreamUtilities.CreateFileStream when the data stream 'Zone.Identifier'
                // does not exist, or when the underlying file system doesn't support alternate data stream.
            }

            return SecurityZone.NoZone;
        }
        #endregion WithoutUrlmon
#endif

        /// <summary>
        /// Map the file to SecurityZone using urlmon.dll, depending on 'IInternetSecurityManager::MapUrlToZone'.
        /// </summary>
        private static SecurityZone MapSecurityZoneWithUrlmon(string filePath)
        {
            uint zoneId;
            object curSecMgr = null;
            const UInt32 MUTZ_DONT_USE_CACHE = 0x00001000;

            int hr = NativeMethods.CoInternetCreateSecurityManager(null, out curSecMgr, 0);
            if (hr != NativeMethods.S_OK)
            {
                // Returns an error value if it's not S_OK
                throw new System.ComponentModel.Win32Exception(hr);
            }

            try
            {
                NativeMethods.IInternetSecurityManager ism = (NativeMethods.IInternetSecurityManager)curSecMgr;
                hr = ism.MapUrlToZone(filePath, out zoneId, MUTZ_DONT_USE_CACHE);
                if (hr == NativeMethods.S_OK)
                {
                    SecurityZone result;
                    return LanguagePrimitives.TryConvertTo(zoneId, out result) ? result : SecurityZone.NoZone;
                }
                return SecurityZone.NoZone;
            }
            finally
            {
                if (curSecMgr != null)
                {
                    Marshal.ReleaseComObject(curSecMgr);
                }
            }
        }

        #endregion Security

        #region Culture

        /// <summary>
        /// Facade for CultureInfo.GetCultureInfo(string).
        /// </summary>
        internal static CultureInfo GetCultureInfo(string cultureName)
        {
#if CORECLR
            return new CultureInfo(cultureName);
#else
            return CultureInfo.GetCultureInfo(cultureName);
#endif
        }

        /// <summary>
        /// Facade for setting CurrentCulture for the CurrentThread
        /// </summary>
        internal static void SetCurrentThreadCulture(CultureInfo cultureInfo)
        {
#if CORECLR
            CultureInfo.CurrentCulture = cultureInfo;
#else
            // Setters for 'CultureInfo.CurrentCulture' is introduced in .NET 4.6
            Thread.CurrentThread.CurrentCulture = cultureInfo;
#endif
        }

        /// <summary>
        /// Facade for setting CurrentUICulture for the CurrentThread
        /// </summary>
        internal static void SetCurrentThreadUiCulture(CultureInfo uiCultureInfo)
        {
#if CORECLR
            CultureInfo.CurrentUICulture = uiCultureInfo;
#else
            // Setters for 'CultureInfo.CurrentUICulture' is introduced in .NET 4.6
            Thread.CurrentThread.CurrentUICulture = uiCultureInfo;
#endif
        }

        #endregion Culture

        #region Misc

        /// <summary>
        /// Facade for Convert.ToBase64String(bytes, Base64FormattingOptions.InsertLineBreaks)
        /// Inserts line breaks after every 76 characters in the string representation.
        /// </summary>
        internal static string ToBase64StringWithLineBreaks(byte[] bytes)
        {
#if CORECLR
            // Inserts line breaks after every 76 characters in the string representation.
            string encodedRawString = Convert.ToBase64String(bytes);
            if (encodedRawString.Length <= 76)
                return encodedRawString;
            
            StringBuilder builder = new StringBuilder(encodedRawString.Length);
            int index = 0, remainingLen = encodedRawString.Length;
            while (remainingLen > 76)
            {
                builder.Append(encodedRawString, index, 76);
                builder.Append(System.Environment.NewLine);

                index += 76;
                remainingLen -= 76;
            }

            builder.Append(encodedRawString, index, remainingLen);
            return builder.ToString();
#else
            return Convert.ToBase64String(bytes, Base64FormattingOptions.InsertLineBreaks);
#endif
        }

        /// <summary>
        /// Facade for RemotingServices.IsTransparentProxy(object)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsTransparentProxy(object obj)
        {
#if CORECLR // Namespace System.Runtime.Remoting is not in CoreCLR
            return false;
#else
            return System.Runtime.Remoting.RemotingServices.IsTransparentProxy(obj);
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
            string UtcString = String.Empty;
            // Fill up the UTC field in the DMTF date with the current zones UTC value
            TimeZoneInfo curZone = TimeZoneInfo.Local;
            TimeSpan tickOffset = curZone.GetUtcOffset(date);
            long OffsetMins = (tickOffset.Ticks / TimeSpan.TicksPerMinute);
            IFormatProvider frmInt32 = (IFormatProvider)CultureInfo.InvariantCulture.GetFormat(typeof(Int32));

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

            dmtfDateTime = (dmtfDateTime + date.Month.ToString(frmInt32).PadLeft(2, '0'));
            dmtfDateTime = (dmtfDateTime + date.Day.ToString(frmInt32).PadLeft(2, '0'));
            dmtfDateTime = (dmtfDateTime + date.Hour.ToString(frmInt32).PadLeft(2, '0'));
            dmtfDateTime = (dmtfDateTime + date.Minute.ToString(frmInt32).PadLeft(2, '0'));
            dmtfDateTime = (dmtfDateTime + date.Second.ToString(frmInt32).PadLeft(2, '0'));
            dmtfDateTime = (dmtfDateTime + ".");

            // Construct a DateTime with with the precision to Second as same as the passed DateTime and so get
            // the ticks difference so that the microseconds can be calculated
            DateTime dtTemp = new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, 0);
            Int64 microsec = ((date.Ticks - dtTemp.Ticks) * 1000) / TimeSpan.TicksPerMillisecond;

            // fill the microseconds field
            String strMicrosec = microsec.ToString((IFormatProvider)CultureInfo.InvariantCulture.GetFormat(typeof(Int64)));
            if (strMicrosec.Length > 6)
            {
                strMicrosec = strMicrosec.Substring(0, 6);
            }
            dmtfDateTime = dmtfDateTime + strMicrosec.PadLeft(6, '0');
            // adding the UTC offset
            dmtfDateTime = dmtfDateTime + UtcString;

            return dmtfDateTime;
#else
            return ManagementDateTimeConverter.ToDmtfDateTime(date);
#endif
        }

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
        /// 2. Classes must have a default constructor implemented for GetConstructor to work.
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
            PSAssemblyLoadContext.SetProfileOptimizationRootImpl(directoryPath);
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
            PSAssemblyLoadContext.StartProfileOptimizationImpl(profile);
#else
            System.Runtime.ProfileOptimization.StartProfile(profile);
#endif
        }

        #endregion Misc

        /// <summary>
        /// Native methods that are used by facade methods
        /// </summary>
        private static class NativeMethods
        {
            /// <summary>
            /// Pinvoke for GetOEMCP to get the OEM code page.
            /// </summary>
            [DllImport(PinvokeDllNames.GetOEMCPDllName, SetLastError = false, CharSet = CharSet.Unicode)]
            internal static extern uint GetOEMCP();

            public const int S_OK = 0x00000000;

            /// <summary>
            /// Pinvoke to create an IInternetSecurityManager interface..
            /// </summary>
            [DllImport("urlmon.dll", ExactSpelling = true)]
            internal static extern int CoInternetCreateSecurityManager([MarshalAs(UnmanagedType.Interface)] object pIServiceProvider,
                                                                       [MarshalAs(UnmanagedType.Interface)] out object ppISecurityManager,
                                                                       int dwReserved);

            /// <summary>
            /// IInternetSecurityManager interface
            /// </summary>
            [ComImport, ComVisible(false), Guid("79EAC9EE-BAF9-11CE-8C82-00AA004BA90B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            internal interface IInternetSecurityManager
            {
                [return: MarshalAs(UnmanagedType.I4)]
                [PreserveSig]
                int SetSecuritySite([In] IntPtr pSite);

                [return: MarshalAs(UnmanagedType.I4)]
                [PreserveSig]
                int GetSecuritySite([Out] IntPtr pSite);

                [return: MarshalAs(UnmanagedType.I4)]
                [PreserveSig]
                int MapUrlToZone([In, MarshalAs(UnmanagedType.LPWStr)] string pwszUrl, out uint pdwZone, uint dwFlags);

                [return: MarshalAs(UnmanagedType.I4)]
                [PreserveSig]
                int GetSecurityId([MarshalAs(UnmanagedType.LPWStr)] string pwszUrl,
                                  [MarshalAs(UnmanagedType.LPArray)] byte[] pbSecurityId,
                                  ref uint pcbSecurityId, uint dwReserved);

                [return: MarshalAs(UnmanagedType.I4)]
                [PreserveSig]
                int ProcessUrlAction([In, MarshalAs(UnmanagedType.LPWStr)] string pwszUrl,
                                     uint dwAction, out byte pPolicy, uint cbPolicy,
                                     byte pContext, uint cbContext, uint dwFlags,
                                     uint dwReserved);

                [return: MarshalAs(UnmanagedType.I4)]
                [PreserveSig]
                int QueryCustomPolicy([In, MarshalAs(UnmanagedType.LPWStr)] string pwszUrl,
                                      ref Guid guidKey, ref byte ppPolicy, ref uint pcbPolicy,
                                      ref byte pContext, uint cbContext, uint dwReserved);

                [return: MarshalAs(UnmanagedType.I4)]
                [PreserveSig]
                int SetZoneMapping(uint dwZone, [In, MarshalAs(UnmanagedType.LPWStr)] string lpszPattern, uint dwFlags);

                [return: MarshalAs(UnmanagedType.I4)]
                [PreserveSig]
                int GetZoneMappings(uint dwZone, out IEnumString ppenumString, uint dwFlags);
            }
        }
    }
}
