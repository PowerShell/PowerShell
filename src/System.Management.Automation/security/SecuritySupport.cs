// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable 1634, 1691
#pragma warning disable 56523

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Globalization;
using System.Management.Automation.Configuration;
using System.Management.Automation.Internal;
using System.Management.Automation.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using Microsoft.PowerShell;
using Microsoft.PowerShell.Commands;

using DWORD = System.UInt32;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// Defines the different Execution Policies supported by the
    /// PSAuthorizationManager class.
    /// </summary>
    public enum ExecutionPolicy
    {
        /// Unrestricted - No files must be signed.  If a file originates from the
        ///    internet, PowerShell provides a warning prompt to alert the user.  To
        ///    suppress this warning message, right-click on the file in File Explorer,
        ///    select "Properties," and then "Unblock."
        Unrestricted = 0,

        /// RemoteSigned - Only .ps1 and .ps1xml files originating from the internet
        ///    must be digitally signed.  If remote, signed, and executed, PowerShell
        ///    prompts to determine if files from the signing publisher should be
        ///    run or not.  This is the default setting.
        RemoteSigned = 1,

        /// AllSigned - All .ps1 and .ps1xml files must be digitally signed.  If
        ///    signed and executed, PowerShell prompts to determine if files from the
        ///    signing publisher should be run or not.
        AllSigned = 2,

        /// Restricted - All .ps1 files are blocked.  Ps1xml files must be digitally
        ///    signed, and by a trusted publisher.  If you haven't made a trust decision
        ///    on the publisher yet, prompting is done as in AllSigned mode.
        Restricted = 3,

        /// Bypass - No files must be signed, and internet origin is not verified
        Bypass = 4,

        /// Undefined - Not specified at this scope
        Undefined = 5,

        /// <summary>
        /// Default - The most restrictive policy available.
        /// </summary>
        Default = Restricted
    }

    /// <summary>
    /// Defines the available configuration scopes for an execution
    /// policy. They are in the following priority, with successive
    /// elements overriding the items that precede them:
    /// LocalMachine -> CurrentUser -> Runspace.
    /// </summary>
    public enum ExecutionPolicyScope
    {
        /// Execution policy is retrieved from the
        /// PSExecutionPolicyPreference environment variable.
        Process = 0,

        /// Execution policy is retrieved from the HKEY_CURRENT_USER
        /// registry hive for the current ShellId.
        CurrentUser = 1,

        /// Execution policy is retrieved from the HKEY_LOCAL_MACHINE
        /// registry hive for the current ShellId.
        LocalMachine = 2,

        /// Execution policy is retrieved from the current user's
        /// group policy setting.
        UserPolicy = 3,

        /// Execution policy is retrieved from the machine-wide
        /// group policy setting.
        MachinePolicy = 4
    }
}

namespace System.Management.Automation.Internal
{
    /// <summary>
    /// The SAFER policy associated with this file.
    /// </summary>
    internal enum SaferPolicy
    {
        /// Explicitly allowed through an Allow rule
        ExplicitlyAllowed = 0,

        /// Allowed because it has not been explicitly disallowed
        Allowed = 1,

        /// Disallowed by a rule or policy.
        Disallowed = 2
    }

    /// <summary>
    /// Security Support APIs.
    /// </summary>
    public static class SecuritySupport
    {
        #region execution policy

        internal static ExecutionPolicyScope[] ExecutionPolicyScopePreferences
        {
            get
            {
                return new ExecutionPolicyScope[] {
                        ExecutionPolicyScope.MachinePolicy,
                        ExecutionPolicyScope.UserPolicy,
                        ExecutionPolicyScope.Process,
                        ExecutionPolicyScope.CurrentUser,
                        ExecutionPolicyScope.LocalMachine
                    };
            }
        }

        internal static void SetExecutionPolicy(ExecutionPolicyScope scope, ExecutionPolicy policy, string shellId)
        {
#if UNIX
            throw new PlatformNotSupportedException();
#else
            string executionPolicy = "Restricted";

            switch (policy)
            {
                case ExecutionPolicy.Restricted:
                    executionPolicy = "Restricted";
                    break;
                case ExecutionPolicy.AllSigned:
                    executionPolicy = "AllSigned";
                    break;
                case ExecutionPolicy.RemoteSigned:
                    executionPolicy = "RemoteSigned";
                    break;
                case ExecutionPolicy.Unrestricted:
                    executionPolicy = "Unrestricted";
                    break;
                case ExecutionPolicy.Bypass:
                    executionPolicy = "Bypass";
                    break;
            }

            // Set the execution policy
            switch (scope)
            {
                case ExecutionPolicyScope.Process:

                    if (policy == ExecutionPolicy.Undefined)
                        executionPolicy = null;

                    Environment.SetEnvironmentVariable("PSExecutionPolicyPreference", executionPolicy);
                    break;

                case ExecutionPolicyScope.CurrentUser:

                    // They want to remove it
                    if (policy == ExecutionPolicy.Undefined)
                    {
                        PowerShellConfig.Instance.RemoveExecutionPolicy(ConfigScope.CurrentUser, shellId);
                    }
                    else
                    {
                        PowerShellConfig.Instance.SetExecutionPolicy(ConfigScope.CurrentUser, shellId, executionPolicy);
                    }

                    break;

                case ExecutionPolicyScope.LocalMachine:

                    // They want to remove it
                    if (policy == ExecutionPolicy.Undefined)
                    {
                        PowerShellConfig.Instance.RemoveExecutionPolicy(ConfigScope.AllUsers, shellId);
                    }
                    else
                    {
                        PowerShellConfig.Instance.SetExecutionPolicy(ConfigScope.AllUsers, shellId, executionPolicy);
                    }

                    break;
            }
#endif
        }

        internal static ExecutionPolicy GetExecutionPolicy(string shellId)
        {
            foreach (ExecutionPolicyScope scope in ExecutionPolicyScopePreferences)
            {
                ExecutionPolicy policy = GetExecutionPolicy(shellId, scope);
                if (policy != ExecutionPolicy.Undefined)
                    return policy;
            }

            return ExecutionPolicy.Restricted;
        }

        private static bool? _hasGpScriptParent;

        /// <summary>
        /// A value indicating that the current process was launched by GPScript.exe
        /// Used to determine execution policy when group policies are in effect.
        /// </summary>
        /// <remarks>
        /// This is somewhat expensive to determine and does not change within the lifetime of the current process
        /// </remarks>
        private static bool HasGpScriptParent
        {
            get
            {
                if (!_hasGpScriptParent.HasValue)
                {
                    _hasGpScriptParent = IsCurrentProcessLaunchedByGpScript();
                }

                return _hasGpScriptParent.Value;
            }
        }

        private static bool IsCurrentProcessLaunchedByGpScript()
        {
            Process currentProcess = Process.GetCurrentProcess();
            string gpScriptPath = IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                "gpscript.exe");

            bool foundGpScriptParent = false;
            try
            {
                while (currentProcess != null)
                {
                    if (string.Equals(gpScriptPath,
                            currentProcess.MainModule.FileName, StringComparison.OrdinalIgnoreCase))
                    {
                        foundGpScriptParent = true;
                        break;
                    }
                    else
                    {
                        currentProcess = PsUtils.GetParentProcess(currentProcess);
                    }
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // If you attempt to retrieve the MainModule of a 64-bit process
                // from a WOW64 (32-bit) process, the Win32 API has a fatal
                // flaw that causes this to return the error:
                //   "Only part of a ReadProcessMemory or WriteProcessMemory
                //   request was completed."
                // In this case, we just catch the exception and eat it.
                // The implication is that logon / logoff scripts that somehow
                // launch the Wow64 version of PowerShell will be subject
                // to the execution policy deployed by Group Policy (where
                // our goal here is to not have the Group Policy execution policy
                // affect logon / logoff scripts.
            }

            return foundGpScriptParent;
        }

        internal static ExecutionPolicy GetExecutionPolicy(string shellId, ExecutionPolicyScope scope)
        {
#if UNIX
            return ExecutionPolicy.Unrestricted;
#else
            switch (scope)
            {
                case ExecutionPolicyScope.Process:
                    {
                        string policy = Environment.GetEnvironmentVariable("PSExecutionPolicyPreference");

                        if (!string.IsNullOrEmpty(policy))
                            return ParseExecutionPolicy(policy);
                        else
                            return ExecutionPolicy.Undefined;
                    }

                case ExecutionPolicyScope.CurrentUser:
                case ExecutionPolicyScope.LocalMachine:
                    {
                        string policy = GetLocalPreferenceValue(shellId, scope);

                        if (!string.IsNullOrEmpty(policy))
                            return ParseExecutionPolicy(policy);
                        else
                            return ExecutionPolicy.Undefined;
                    }

                // TODO: Group Policy is only supported on Full systems, but !LINUX && CORECLR
                // will run there as well, so I don't think we should remove it.
                case ExecutionPolicyScope.UserPolicy:
                case ExecutionPolicyScope.MachinePolicy:
                    {
                        string groupPolicyPreference = GetGroupPolicyValue(shellId, scope);

                        // Be sure we aren't being called by Group Policy
                        // itself. A group policy should never block a logon /
                        // logoff script.
                        if (string.IsNullOrEmpty(groupPolicyPreference) || HasGpScriptParent)
                        {
                            return ExecutionPolicy.Undefined;
                        }

                        return ParseExecutionPolicy(groupPolicyPreference);
                    }
            }

            return ExecutionPolicy.Restricted;
#endif
        }

        internal static ExecutionPolicy ParseExecutionPolicy(string policy)
        {
            if (string.Equals(policy, "Bypass",
                                   StringComparison.OrdinalIgnoreCase))
            {
                return ExecutionPolicy.Bypass;
            }
            else if (string.Equals(policy, "Unrestricted",
                                   StringComparison.OrdinalIgnoreCase))
            {
                return ExecutionPolicy.Unrestricted;
            }
            else if (string.Equals(policy, "RemoteSigned",
                                   StringComparison.OrdinalIgnoreCase))
            {
                return ExecutionPolicy.RemoteSigned;
            }
            else if (string.Equals(policy, "AllSigned",
                              StringComparison.OrdinalIgnoreCase))
            {
                return ExecutionPolicy.AllSigned;
            }
            else if (string.Equals(policy, "Restricted",
                         StringComparison.OrdinalIgnoreCase))
            {
                return ExecutionPolicy.Restricted;
            }
            else
            {
                return ExecutionPolicy.Default;
            }
        }

        internal static string GetExecutionPolicy(ExecutionPolicy policy)
        {
            switch (policy)
            {
                case ExecutionPolicy.Bypass:
                    return "Bypass";
                case ExecutionPolicy.Unrestricted:
                    return "Unrestricted";
                case ExecutionPolicy.RemoteSigned:
                    return "RemoteSigned";
                case ExecutionPolicy.AllSigned:
                    return "AllSigned";
                case ExecutionPolicy.Restricted:
                    return "Restricted";
                default:
                    return "Restricted";
            }
        }

        /// <summary>
        /// Returns true if file has product binary signature.
        /// </summary>
        /// <param name="file">Name of file to check.</param>
        /// <returns>True when file has product binary signature.</returns>
        public static bool IsProductBinary(string file)
        {
            if (string.IsNullOrEmpty(file) || (!IO.File.Exists(file)))
            {
                return false;
            }

            // Check if it is in the product folder, if not, skip checking the catalog
            // and any other checks.
            var isUnderProductFolder = Utils.IsUnderProductFolder(file);
            if (!isUnderProductFolder)
            {
                return false;
            }

#if UNIX
            // There is no signature support on non-Windows platforms (yet), when
            // execution reaches here, we are sure the file is under product folder
            return true;
#else
            // Check the file signature
            Signature fileSignature = SignatureHelper.GetSignature(file, null);
            if ((fileSignature != null) && (fileSignature.IsOSBinary))
            {
                return true;
            }

            // WTGetSignatureInfo, via Microsoft.Security.Extensions, is used to verify catalog signature.
            // On Win7, catalog API is not available.
            // On OneCore SKUs like NanoServer/IoT, the API has a bug that makes it not able to find the
            // corresponding catalog file for a given product file, so it doesn't work properly.
            // In these cases, we just trust the 'isUnderProductFolder' check.
            if (Signature.CatalogApiAvailable.HasValue && !Signature.CatalogApiAvailable.Value)
            {
                // When execution reaches here, we are sure the file is under product folder
                return true;
            }

            return false;
#endif
        }

        /// <summary>
        /// Returns the value of the Execution Policy as retrieved
        /// from group policy.
        /// </summary>
        /// <returns>NULL if it is not defined at this level.</returns>
        private static string GetGroupPolicyValue(string shellId, ExecutionPolicyScope scope)
        {
            ConfigScope[] scopeKey = null;

            switch (scope)
            {
                case ExecutionPolicyScope.MachinePolicy:
                    scopeKey = Utils.SystemWideOnlyConfig;
                    break;

                case ExecutionPolicyScope.UserPolicy:
                    scopeKey = Utils.CurrentUserOnlyConfig;
                    break;
            }

            var scriptExecutionSetting = Utils.GetPolicySetting<ScriptExecution>(scopeKey);
            if (scriptExecutionSetting != null)
            {
                if (scriptExecutionSetting.EnableScripts == false)
                {
                    // Script execution is explicitly disabled
                    return "Restricted";
                }
                else if (scriptExecutionSetting.EnableScripts == true)
                {
                    // Script execution is explicitly enabled
                    return scriptExecutionSetting.ExecutionPolicy;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the value of the Execution Policy as retrieved
        /// from the local preference.
        /// </summary>
        /// <returns>NULL if it is not defined at this level.</returns>
        private static string GetLocalPreferenceValue(string shellId, ExecutionPolicyScope scope)
        {
            switch (scope)
            {
                // 1: Look up the current-user preference
                case ExecutionPolicyScope.CurrentUser:
                    return PowerShellConfig.Instance.GetExecutionPolicy(ConfigScope.CurrentUser, shellId);

                // 2: Look up the system-wide preference
                case ExecutionPolicyScope.LocalMachine:
                    return PowerShellConfig.Instance.GetExecutionPolicy(ConfigScope.AllUsers, shellId);
            }

            return null;
        }

        #endregion execution policy

        private static bool _saferIdentifyLevelApiSupported = true;

        /// <summary>
        /// Get the pass / fail result of calling the SAFER API.
        /// </summary>
        /// <param name="path">The path to the file in question.</param>
        /// <param name="handle">A file handle to the file in question, if available.</param>
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods")]
        internal static SaferPolicy GetSaferPolicy(string path, SafeHandle handle)
        {
            SaferPolicy status = SaferPolicy.Allowed;

            if (!_saferIdentifyLevelApiSupported)
            {
                return status;
            }

            SAFER_CODE_PROPERTIES codeProperties = new SAFER_CODE_PROPERTIES();
            IntPtr hAuthzLevel;

            // Prepare the code properties struct.
            codeProperties.cbSize = (uint)Marshal.SizeOf(typeof(SAFER_CODE_PROPERTIES));
            codeProperties.dwCheckFlags = (
                NativeConstants.SAFER_CRITERIA_IMAGEPATH |
                NativeConstants.SAFER_CRITERIA_IMAGEHASH |
                NativeConstants.SAFER_CRITERIA_AUTHENTICODE);
            codeProperties.ImagePath = path;

            if (handle != null)
            {
                codeProperties.hImageFileHandle = handle.DangerousGetHandle();
            }

            // turn off WinVerifyTrust UI
            codeProperties.dwWVTUIChoice = NativeConstants.WTD_UI_NONE;

            // Identify the level associated with the code
            if (NativeMethods.SaferIdentifyLevel(1, ref codeProperties, out hAuthzLevel, NativeConstants.SRP_POLICY_SCRIPT))
            {
                // We found an Authorization Level applicable to this application.
                IntPtr hRestrictedToken = IntPtr.Zero;
                try
                {
                    if (!NativeMethods.SaferComputeTokenFromLevel(
                                               hAuthzLevel,                    // Safer Level
                                               IntPtr.Zero,                    // Test current process' token
                                               ref hRestrictedToken,           // target token
                                               NativeConstants.SAFER_TOKEN_NULL_IF_EQUAL,
                                               IntPtr.Zero))
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        if ((lastError == NativeConstants.ERROR_ACCESS_DISABLED_BY_POLICY) ||
                            (lastError == NativeConstants.ERROR_ACCESS_DISABLED_NO_SAFER_UI_BY_POLICY))
                        {
                            status = SaferPolicy.Disallowed;
                        }
                        else
                        {
                            throw new System.ComponentModel.Win32Exception();
                        }
                    }
                    else
                    {
                        if (hRestrictedToken == IntPtr.Zero)
                        {
                            // This is not necessarily the "fully trusted" level,
                            // it means that the thread token is complies with the requested level
                            status = SaferPolicy.Allowed;
                        }
                        else
                        {
                            status = SaferPolicy.Disallowed;
                            NativeMethods.CloseHandle(hRestrictedToken);
                        }
                    }
                }
                finally
                {
                    NativeMethods.SaferCloseLevel(hAuthzLevel);
                }
            }
            else
            {
                int lastError = Marshal.GetLastWin32Error();
                if (lastError == NativeConstants.FUNCTION_NOT_SUPPORTED)
                {
                    _saferIdentifyLevelApiSupported = false;
                }
                else
                {
                    throw new System.ComponentModel.Win32Exception(lastError);
                }
            }

            return status;
        }

        /// <summary>
        /// Throw if file does not exist.
        /// </summary>
        /// <param name="filePath">Path to file.</param>
        /// <returns>Does not return a value.</returns>
        internal static void CheckIfFileExists(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(filePath);
            }
        }

        /// <summary>
        /// Check to see if the specified cert is suitable to be
        /// used as a code signing cert.
        /// </summary>
        /// <param name="c">Certificate object.</param>
        /// <returns>True on success, false otherwise.</returns>
        internal static bool CertIsGoodForSigning(X509Certificate2 c)
        {
            if (!c.HasPrivateKey)
            {
                return false;
            }

            return CertHasOid(c, CertificateFilterInfo.CodeSigningOid);
        }

        /// <summary>
        /// Check to see if the specified cert is suitable to be
        /// used as an encryption cert for PKI encryption. Note
        /// that this cert doesn't require the private key.
        /// </summary>
        /// <param name="c">Certificate object.</param>
        /// <returns>True on success, false otherwise.</returns>
        internal static bool CertIsGoodForEncryption(X509Certificate2 c)
        {
            return (
                CertHasOid(c, CertificateFilterInfo.DocumentEncryptionOid) &&
                (CertHasKeyUsage(c, X509KeyUsageFlags.DataEncipherment) ||
                 CertHasKeyUsage(c, X509KeyUsageFlags.KeyEncipherment)));
        }

        /// <summary>
        /// Check to see if the specified cert is expiring by the time.
        /// </summary>
        /// <param name="c">Certificate object.</param>
        /// <param name="expiring">Certificate expire time.</param>
        /// <returns>True on success, false otherwise.</returns>
        internal static bool CertExpiresByTime(X509Certificate2 c, DateTime expiring)
        {
            return c.NotAfter < expiring;
        }

        private static bool CertHasOid(X509Certificate2 c, string oid)
        {
            foreach (var extension in c.Extensions)
            {
                if (extension is X509EnhancedKeyUsageExtension ext)
                {
                    foreach (Oid ekuOid in ext.EnhancedKeyUsages)
                    {
                        if (ekuOid.Value == oid)
                        {
                            return true;
                        }
                    }
                    break;
                }
            }
            return false;
        }

        private static bool CertHasKeyUsage(X509Certificate2 c, X509KeyUsageFlags keyUsage)
        {
            foreach (X509Extension extension in c.Extensions)
            {
                if (extension is X509KeyUsageExtension keyUsageExtension)
                {
                    if ((keyUsageExtension.KeyUsages & keyUsage) == keyUsage)
                    {
                        return true;
                    }
                    break;
                }
            }
            return false;
        }

        /// <summary>
        /// Get the EKUs of a cert.
        /// </summary>
        /// <param name="cert">Certificate object.</param>
        /// <returns>A collection of cert eku strings.</returns>
        internal static Collection<string> GetCertEKU(X509Certificate2 cert)
        {
            Collection<string> ekus = new Collection<string>();
            IntPtr pCert = cert.Handle;
            int structSize = 0;
            IntPtr dummy = IntPtr.Zero;

            if (Security.NativeMethods.CertGetEnhancedKeyUsage(pCert, 0, dummy,
                                                      out structSize))
            {
                if (structSize > 0)
                {
                    IntPtr ekuBuffer = Marshal.AllocHGlobal(structSize);

                    try
                    {
                        if (Security.NativeMethods.CertGetEnhancedKeyUsage(pCert, 0,
                                                                  ekuBuffer,
                                                                  out structSize))
                        {
                            Security.NativeMethods.CERT_ENHKEY_USAGE ekuStruct =
                                Marshal.PtrToStructure<Security.NativeMethods.CERT_ENHKEY_USAGE>(ekuBuffer);
                            IntPtr ep = ekuStruct.rgpszUsageIdentifier;
                            IntPtr ekuptr;

                            for (int i = 0; i < ekuStruct.cUsageIdentifier; i++)
                            {
                                ekuptr = Marshal.ReadIntPtr(ep, i * Marshal.SizeOf(ep));
                                string eku = Marshal.PtrToStringAnsi(ekuptr);
                                ekus.Add(eku);
                            }
                        }
                        else
                        {
                            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(ekuBuffer);
                    }
                }
            }
            else
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            return ekus;
        }

        /// <summary>
        /// Convert an int to a DWORD.
        /// </summary>
        /// <param name="n">Signed int number.</param>
        /// <returns>DWORD.</returns>
        internal static DWORD GetDWORDFromInt(int n)
        {
            UInt32 result = BitConverter.ToUInt32(BitConverter.GetBytes(n), 0);
            return (DWORD)result;
        }

        /// <summary>
        /// Convert a DWORD to int.
        /// </summary>
        /// <param name="n">Number.</param>
        /// <returns>Int.</returns>
        internal static int GetIntFromDWORD(DWORD n)
        {
            long n64 = n - 0x100000000L;
            return (int)n64;
        }
    }

    /// <summary>
    /// Information used for filtering a set of certs.
    /// </summary>
    internal sealed class CertificateFilterInfo
    {
        internal CertificateFilterInfo()
        {
        }

        /// <summary>
        /// Gets or sets purpose of a certificate.
        /// </summary>
        internal CertificatePurpose Purpose
        {
            get;
            set;
        } = CertificatePurpose.NotSpecified;

        /// <summary>
        /// Gets or sets SSL Server Authentication.
        /// </summary>
        internal bool SSLServerAuthentication
        {
            get;

            set;
        }

        /// <summary>
        /// Gets or sets DNS name of a certificate.
        /// </summary>
        internal WildcardPattern DnsName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets EKU OID list of a certificate.
        /// </summary>
        internal List<WildcardPattern> Eku
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets validity time for a certificate.
        /// </summary>
        internal DateTime Expiring
        {
            get;
            set;
        } = DateTime.MinValue;

        internal const string CodeSigningOid = "1.3.6.1.5.5.7.3.3";
        internal const string OID_PKIX_KP_SERVER_AUTH = "1.3.6.1.5.5.7.3.1";

        // The OID arc 1.3.6.1.4.1.311.80 is assigned to PowerShell. If we need
        // new OIDs, we can assign them under this branch.
        internal const string DocumentEncryptionOid = "1.3.6.1.4.1.311.80.1";
        internal const string SubjectAlternativeNameOid = "2.5.29.17";
    }
}

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Defines the valid purposes by which
    /// we can filter certificates.
    /// </summary>
    internal enum CertificatePurpose
    {
        /// <summary>
        /// Certificates where a purpose has not been specified.
        /// </summary>
        NotSpecified = 0,

        /// <summary>
        /// Certificates that can be used to sign
        /// code and scripts.
        /// </summary>
        CodeSigning = 0x1,

        /// <summary>
        /// Certificates that can be used to encrypt
        /// data.
        /// </summary>
        DocumentEncryption = 0x2,

        /// <summary>
        /// Certificates that can be used for any
        /// purpose.
        /// </summary>
        All = 0xffff
    }
}

namespace System.Management.Automation
{
    using System.Management.Automation.Tracing;
    using System.Security.Cryptography.Pkcs;

    /// <summary>
    /// Utility class for CMS (Cryptographic Message Syntax) related operations.
    /// </summary>
    internal static class CmsUtils
    {
        internal static string Encrypt(byte[] contentBytes, CmsMessageRecipient[] recipients, SessionState sessionState, out ErrorRecord error)
        {
            error = null;

            if ((contentBytes == null) || (contentBytes.Length == 0))
            {
                return string.Empty;
            }

            // After review with the crypto board, NIST_AES256_CBC is more appropriate
            // than .NET's default 3DES. Also, when specified, uses szOID_RSAES_OAEP for key
            // encryption to prevent padding attacks.
            const string szOID_NIST_AES256_CBC = "2.16.840.1.101.3.4.1.42";

            ContentInfo content = new ContentInfo(contentBytes);
            EnvelopedCms cms = new EnvelopedCms(content,
                new AlgorithmIdentifier(
                    Oid.FromOidValue(szOID_NIST_AES256_CBC, OidGroup.EncryptionAlgorithm)));

            CmsRecipientCollection recipientCollection = new CmsRecipientCollection();
            foreach (CmsMessageRecipient recipient in recipients)
            {
                // Resolve the recipient, if it hasn't been done yet.
                if ((recipient.Certificates != null) && (recipient.Certificates.Count == 0))
                {
                    recipient.Resolve(sessionState, ResolutionPurpose.Encryption, out error);
                }

                if (error != null)
                {
                    return null;
                }

                foreach (X509Certificate2 certificate in recipient.Certificates)
                {
                    recipientCollection.Add(new CmsRecipient(certificate));
                }
            }

            cms.Encrypt(recipientCollection);

            byte[] encodedBytes = cms.Encode();
            string encodedContent = CmsUtils.GetAsciiArmor(encodedBytes);
            return encodedContent;
        }

        internal static readonly string BEGIN_CMS_SIGIL = "-----BEGIN CMS-----";
        internal static readonly string END_CMS_SIGIL = "-----END CMS-----";

        internal static readonly string BEGIN_CERTIFICATE_SIGIL = "-----BEGIN CERTIFICATE-----";
        internal static readonly string END_CERTIFICATE_SIGIL = "-----END CERTIFICATE-----";

        /// <summary>
        /// Adds Ascii armour to a byte stream in Base64 format.
        /// </summary>
        /// <param name="bytes">The bytes to encode.</param>
        internal static string GetAsciiArmor(byte[] bytes)
        {
            StringBuilder output = new StringBuilder();
            output.AppendLine(BEGIN_CMS_SIGIL);

            string encodedString = Convert.ToBase64String(bytes, Base64FormattingOptions.InsertLineBreaks);
            output.AppendLine(encodedString);
            output.Append(END_CMS_SIGIL);

            return output.ToString();
        }

        /// <summary>
        /// Removes Ascii armour from a byte stream.
        /// </summary>
        /// <param name="actualContent">The Ascii armored content.</param>
        /// <param name="beginMarker">The marker of the start of the Base64 content.</param>
        /// <param name="endMarker">The marker of the end of the Base64 content.</param>
        /// <param name="startIndex">The beginning of where the Ascii armor was detected.</param>
        /// <param name="endIndex">The end of where the Ascii armor was detected.</param>
        internal static byte[] RemoveAsciiArmor(string actualContent, string beginMarker, string endMarker, out int startIndex, out int endIndex)
        {
            byte[] messageBytes = null;
            startIndex = -1;
            endIndex = -1;

            startIndex = actualContent.IndexOf(beginMarker, StringComparison.OrdinalIgnoreCase);
            if (startIndex < 0)
            {
                return null;
            }

            endIndex = actualContent.IndexOf(endMarker, startIndex, StringComparison.OrdinalIgnoreCase) +
                 endMarker.Length;
            if (endIndex < endMarker.Length)
            {
                return null;
            }

            int startContent = startIndex + beginMarker.Length;
            int endContent = endIndex - endMarker.Length;
            string encodedContent = actualContent.Substring(startContent, endContent - startContent);
            encodedContent = System.Text.RegularExpressions.Regex.Replace(encodedContent, "\\s", string.Empty);
            messageBytes = Convert.FromBase64String(encodedContent);

            return messageBytes;
        }
    }

    /// <summary>
    /// Represents a message recipient for the Cms cmdlets.
    /// </summary>
    public class CmsMessageRecipient
    {
        /// <summary>
        /// Creates an instance of the CmsMessageRecipient class.
        /// </summary>
        internal CmsMessageRecipient() { }

        /// <summary>
        /// Creates an instance of the CmsMessageRecipient class.
        /// </summary>
        /// <param name="identifier">
        ///     The identifier of the CmsMessageRecipient.
        ///     Can be either:
        ///         - The path to a file containing the certificate
        ///         - The path to a directory containing the certificate
        ///         - The thumbprint of the certificate, used to find the certificate in the certificate store
        ///         - The Subject name of the recipient, used to find the certificate in the certificate store
        /// </param>
        public CmsMessageRecipient(string identifier)
        {
            _identifier = identifier;
            this.Certificates = new X509Certificate2Collection();
        }

        private readonly string _identifier;

        /// <summary>
        /// Creates an instance of the CmsMessageRecipient class.
        /// </summary>
        /// <param name="certificate">The certificate to use.</param>
        public CmsMessageRecipient(X509Certificate2 certificate)
        {
            _pendingCertificate = certificate;
            this.Certificates = new X509Certificate2Collection();
        }

        private readonly X509Certificate2 _pendingCertificate;

        /// <summary>
        /// Gets the certificate associated with this recipient.
        /// </summary>
        public X509Certificate2Collection Certificates
        {
            get;
            internal set;
        }

        /// <summary>
        /// Resolves the provided identifier into a collection of certificates.
        /// </summary>
        /// <param name="sessionState">A reference to an instance of Powershell's SessionState class.</param>
        /// <param name="purpose">The purpose for which this identifier is being resolved (Encryption / Decryption.</param>
        /// <param name="error">The error generated (if any) for this resolution.</param>
        public void Resolve(SessionState sessionState, ResolutionPurpose purpose, out ErrorRecord error)
        {
            error = null;

            // Process the certificate if that was supplied exactly
            if (_pendingCertificate != null)
            {
                ProcessResolvedCertificates(
                    purpose,
                    new X509Certificate2Collection(_pendingCertificate),
                    out error);
                if ((error != null) || (Certificates.Count != 0))
                {
                    return;
                }
            }

            if (_identifier != null)
            {
                // First try to resolve assuming that the cert was Base64 encoded.
                ResolveFromBase64Encoding(purpose, out error);
                if ((error != null) || (Certificates.Count != 0))
                {
                    return;
                }

                // Then try to resolve by path.
                ResolveFromPath(sessionState, purpose, out error);
                if ((error != null) || (Certificates.Count != 0))
                {
                    return;
                }

                // Then by cert store
                ResolveFromStoreById(purpose, out error);
                if ((error != null) || (Certificates.Count != 0))
                {
                    return;
                }
            }

            // Generate an error if no cert was found (and this is an encryption attempt).
            // If it is only decryption, then the system will always look in the 'My' store anyways, so
            // don't generate an error if they used wildcards. If they did not use wildcards,
            // then generate an error because they were expecting something specific.
            if ((purpose == ResolutionPurpose.Encryption) ||
                (!WildcardPattern.ContainsWildcardCharacters(_identifier)))
            {
                error = new ErrorRecord(
                    new ArgumentException(
                        string.Format(CultureInfo.InvariantCulture,
                            SecuritySupportStrings.NoCertificateFound, _identifier)),
                    "NoCertificateFound", ErrorCategory.ObjectNotFound, _identifier);
            }

            return;
        }

        private void ResolveFromBase64Encoding(ResolutionPurpose purpose, out ErrorRecord error)
        {
            error = null;
            int startIndex, endIndex;
            byte[] messageBytes = null;
            try
            {
                messageBytes = CmsUtils.RemoveAsciiArmor(_identifier, CmsUtils.BEGIN_CERTIFICATE_SIGIL, CmsUtils.END_CERTIFICATE_SIGIL, out startIndex, out endIndex);
            }
            catch (FormatException)
            {
                // Not Base-64 encoded
                return;
            }

            // Didn't have the sigil
            if (messageBytes == null)
            {
                return;
            }

            var certificatesToProcess = new X509Certificate2Collection();
            try
            {
                #pragma warning disable SYSLIB0057
                X509Certificate2 newCertificate = new X509Certificate2(messageBytes);
                #pragma warning restore SYSLIB0057

                certificatesToProcess.Add(newCertificate);
            }
            catch (Exception)
            {
                // User call-out, catch-all OK

                // Wasn't certificate data
                return;
            }

            // Now validate the certificate
            ProcessResolvedCertificates(purpose, certificatesToProcess, out error);
        }

        private void ResolveFromPath(SessionState sessionState, ResolutionPurpose purpose, out ErrorRecord error)
        {
            error = null;
            ProviderInfo pathProvider = null;
            Collection<string> resolvedPaths = null;

            try
            {
                resolvedPaths = sessionState.Path.GetResolvedProviderPathFromPSPath(_identifier, out pathProvider);
            }
            catch (SessionStateException)
            {
                // If we got an ItemNotFound / etc., then this didn't represent a valid path.
            }

            // If we got a resolved path, try to load certs from that path.
            if ((resolvedPaths != null) && (resolvedPaths.Count != 0))
            {
                // Ensure the path is from the file system provider
                if (!string.Equals(pathProvider.Name, "FileSystem", StringComparison.OrdinalIgnoreCase))
                {
                    error = new ErrorRecord(
                        new ArgumentException(
                            string.Format(CultureInfo.InvariantCulture,
                                SecuritySupportStrings.CertificatePathMustBeFileSystemPath, _identifier)),
                        "CertificatePathMustBeFileSystemPath", ErrorCategory.ObjectNotFound, pathProvider);
                    return;
                }

                // If this is a directory, add all certificates in it. This will be the primary
                // scenario for decryption via Group Protected PFX files
                // (http://social.technet.microsoft.com/wiki/contents/articles/13922.certificate-pfx-export-and-import-using-ad-ds-account-protection.aspx)
                List<string> pathsToAdd = new List<string>();
                List<string> pathsToRemove = new List<string>();
                foreach (string resolvedPath in resolvedPaths)
                {
                    if (System.IO.Directory.Exists(resolvedPath))
                    {
                        // It would be nice to limit this to *.pfx, *.cer, etc., but
                        // the crypto APIs support extracting certificates from arbitrary file types.
                        pathsToAdd.AddRange(System.IO.Directory.GetFiles(resolvedPath));
                        pathsToRemove.Add(resolvedPath);
                    }
                }

                // Update resolved paths
                foreach (string path in pathsToAdd)
                {
                    resolvedPaths.Add(path);
                }

                foreach (string path in pathsToRemove)
                {
                    resolvedPaths.Remove(path);
                }

                var certificatesToProcess = new X509Certificate2Collection();
                foreach (string path in resolvedPaths)
                {
                    X509Certificate2 certificate = null;

                    try
                    {
                        #pragma warning disable SYSLIB0057
                        certificate = new X509Certificate2(path);
                        #pragma warning restore SYSLIB0057
                    }
                    catch (Exception)
                    {
                        // User call-out, catch-all OK
                        continue;
                    }

                    certificatesToProcess.Add(certificate);
                }

                ProcessResolvedCertificates(purpose, certificatesToProcess, out error);
            }
        }

        private void ResolveFromStoreById(ResolutionPurpose purpose, out ErrorRecord error)
        {
            error = null;
            WildcardPattern subjectNamePattern = WildcardPattern.Get(_identifier, WildcardOptions.IgnoreCase);

            try
            {
                var certificatesToProcess = new X509Certificate2Collection();

                using (var storeCU = new X509Store("my", StoreLocation.CurrentUser))
                {
                    storeCU.Open(OpenFlags.ReadOnly);
                    X509Certificate2Collection storeCerts = storeCU.Certificates;

                    if (Platform.IsWindows)
                    {
                        using (var storeLM = new X509Store("my", StoreLocation.LocalMachine))
                        {
                            storeLM.Open(OpenFlags.ReadOnly);
                            storeCerts.AddRange(storeLM.Certificates);
                        }
                    }

                    certificatesToProcess.AddRange(storeCerts.Find(X509FindType.FindByThumbprint, _identifier, validOnly: false));

                    if (certificatesToProcess.Count == 0)
                    {
                        foreach (var cert in storeCerts)
                        {
                            if (subjectNamePattern.IsMatch(cert.Subject) || subjectNamePattern.IsMatch(cert.GetNameInfo(X509NameType.SimpleName, forIssuer: false)))
                            {
                                certificatesToProcess.Add(cert);
                            }
                        }
                    }

                    ProcessResolvedCertificates(purpose, certificatesToProcess, out error);
                }
            }
            catch (SessionStateException)
            {
            }
        }

        private void ProcessResolvedCertificates(ResolutionPurpose purpose, X509Certificate2Collection certificatesToProcess, out ErrorRecord error)
        {
            error = null;
            HashSet<string> processedThumbprints = new HashSet<string>();

            foreach (X509Certificate2 certificate in certificatesToProcess)
            {
                if (!SecuritySupport.CertIsGoodForEncryption(certificate))
                {
                    // If they specified a specific cert, generate an error if it isn't good
                    // for encryption.
                    if (!WildcardPattern.ContainsWildcardCharacters(_identifier))
                    {
                        error = new ErrorRecord(
                            new ArgumentException(
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    SecuritySupportStrings.CertificateCannotBeUsedForEncryption,
                                    certificate.Thumbprint,
                                    CertificateFilterInfo.DocumentEncryptionOid)),
                            "CertificateCannotBeUsedForEncryption",
                            ErrorCategory.InvalidData,
                            certificate);
                        return;
                    }
                    else
                    {
                        continue;
                    }
                }

                // When decrypting, only look for certs that have the private key
                if (purpose == ResolutionPurpose.Decryption)
                {
                    if (!certificate.HasPrivateKey)
                    {
                        continue;
                    }
                }

                if (processedThumbprints.Contains(certificate.Thumbprint))
                {
                    continue;
                }
                else
                {
                    processedThumbprints.Add(certificate.Thumbprint);
                }

                if (purpose == ResolutionPurpose.Encryption)
                {
                    // Only let wildcards expand to one recipient. Otherwise, data
                    // may be encrypted to the wrong person on accident.
                    if (Certificates.Count > 0)
                    {
                        error = new ErrorRecord(
                            new ArgumentException(
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    SecuritySupportStrings.IdentifierMustReferenceSingleCertificate,
                                    _identifier,
                                    arg1: "To")),
                            "IdentifierMustReferenceSingleCertificate",
                            ErrorCategory.LimitsExceeded,
                            certificatesToProcess);
                        Certificates.Clear();
                        return;
                    }
                }

                Certificates.Add(certificate);
            }
        }
    }

    /// <summary>
    /// Defines the purpose for resolution of a CmsMessageRecipient.
    /// </summary>
    public enum ResolutionPurpose
    {
        /// <summary>
        /// This message recipient is intended to be used for message encryption.
        /// </summary>
        Encryption,

        /// <summary>
        /// This message recipient is intended to be used for message decryption.
        /// </summary>
        Decryption
    }

    internal static class AmsiUtils
    {
        static AmsiUtils()
        {
#if !UNIX
            try
            {
                s_amsiInitFailed = !CheckAmsiInit();
            }
            catch (DllNotFoundException)
            {
                PSEtwLog.LogAmsiUtilStateEvent("DllNotFoundException", $"{s_amsiContext}-{s_amsiSession}");
                s_amsiInitFailed = true;
                return;
            }

            PSEtwLog.LogAmsiUtilStateEvent($"init-{s_amsiInitFailed}", $"{s_amsiContext}-{s_amsiSession}");
#endif
        }

        internal static int Init()
        {
            Diagnostics.Assert(s_amsiContext == IntPtr.Zero, "Init should be called just once");

            lock (s_amsiLockObject)
            {
                string appName;
                try
                {
                    appName = string.Concat("PowerShell_", Environment.ProcessPath, "_", PSVersionInfo.ProductVersion);
                }
                catch (Exception)
                {
                    // Fall back to 'Process.ProcessName' in case 'Environment.ProcessPath' throws exception.
                    Process currentProcess = Process.GetCurrentProcess();
                    appName = string.Concat("PowerShell_", currentProcess.ProcessName, ".exe_", PSVersionInfo.ProductVersion);
                }

                AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

                var hr = AmsiNativeMethods.AmsiInitialize(appName, ref s_amsiContext);
                return hr;
            }
        }

        /// <summary>
        /// Scans a string buffer for malware using the Antimalware Scan Interface (AMSI).
        /// Caller is responsible for calling AmsiCloseSession when a "session" (script)
        /// is complete, and for calling AmsiUninitialize when the runspace is being torn down.
        /// </summary>
        /// <param name="content">The string to be scanned.</param>
        /// <param name="sourceMetadata">Information about the source (filename, etc.).</param>
        /// <returns>AMSI_RESULT_DETECTED if malware was detected in the sample.</returns>
        internal static AmsiNativeMethods.AMSI_RESULT ScanContent(string content, string sourceMetadata)
        {
#if UNIX
            return AmsiNativeMethods.AMSI_RESULT.AMSI_RESULT_NOT_DETECTED;
#else
            return WinScanContent(content, sourceMetadata, warmUp: false);
#endif
        }

        internal static AmsiNativeMethods.AMSI_RESULT WinScanContent(
            string content,
            string sourceMetadata,
            bool warmUp)
        {
            if (string.IsNullOrEmpty(sourceMetadata))
            {
                sourceMetadata = string.Empty;
            }

            const string EICAR_STRING = "X5O!P%@AP[4\\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*";
            if (InternalTestHooks.UseDebugAmsiImplementation)
            {
                if (content.Contains(EICAR_STRING, StringComparison.Ordinal))
                {
                    return AmsiNativeMethods.AMSI_RESULT.AMSI_RESULT_DETECTED;
                }
            }

            // If we had a previous initialization failure, just return the neutral result.
            if (s_amsiInitFailed)
            {
                PSEtwLog.LogAmsiUtilStateEvent("ScanContent-InitFail", $"{s_amsiContext}-{s_amsiSession}");
                return AmsiNativeMethods.AMSI_RESULT.AMSI_RESULT_NOT_DETECTED;
            }

            lock (s_amsiLockObject)
            {
                if (s_amsiInitFailed)
                {
                    PSEtwLog.LogAmsiUtilStateEvent("ScanContent-InitFail", $"{s_amsiContext}-{s_amsiSession}");
                    return AmsiNativeMethods.AMSI_RESULT.AMSI_RESULT_NOT_DETECTED;
                }

                try
                {
                    if (!CheckAmsiInit())
                    {
                        return AmsiNativeMethods.AMSI_RESULT.AMSI_RESULT_NOT_DETECTED;
                    }

                    if (warmUp)
                    {
                        // We are warming up the AMSI component in console startup, and that means we initialize AMSI
                        // and create a AMSI session, but don't really scan anything.
                        return AmsiNativeMethods.AMSI_RESULT.AMSI_RESULT_NOT_DETECTED;
                    }

                    AmsiNativeMethods.AMSI_RESULT result = AmsiNativeMethods.AMSI_RESULT.AMSI_RESULT_CLEAN;

                    // Run AMSI content scan
                    int hr;
                    unsafe
                    {
                        fixed (char* buffer = content)
                        {
                            var buffPtr = new IntPtr(buffer);
                            hr = AmsiNativeMethods.AmsiScanBuffer(
                                s_amsiContext,
                                buffPtr,
                                (uint)(content.Length * sizeof(char)),
                                sourceMetadata,
                                s_amsiSession,
                                ref result);
                        }
                    }

                    if (!Utils.Succeeded(hr))
                    {
                        // If we got a failure, just return the neutral result ("AMSI_RESULT_NOT_DETECTED")
                        PSEtwLog.LogAmsiUtilStateEvent($"AmsiScanBuffer-{hr}", $"{s_amsiContext}-{s_amsiSession}");
                        return AmsiNativeMethods.AMSI_RESULT.AMSI_RESULT_NOT_DETECTED;
                    }

                    return result;
                }
                catch (DllNotFoundException)
                {
                    PSEtwLog.LogAmsiUtilStateEvent("DllNotFoundException", $"{s_amsiContext}-{s_amsiSession}");
                    return AmsiNativeMethods.AMSI_RESULT.AMSI_RESULT_NOT_DETECTED;
                }
            }
        }

        /// <Summary>
        /// Reports provided content to AMSI (Antimalware Scan Interface).
        /// </Summary>
        /// <param name="name">Name of content being reported.</param>
        /// <param name="content">Content being reported.</param>
        /// <returns>True if content was successfully reported.</returns>
        internal static bool ReportContent(
            string name,
            string content)
        {
#if UNIX
            return false;
#else
            return WinReportContent(name, content);
#endif
        }

        private static bool WinReportContent(
            string name,
            string content)
        {
            if (string.IsNullOrEmpty(name) ||
                string.IsNullOrEmpty(content) ||
                s_amsiInitFailed ||
                s_amsiNotifyFailed)
            {
                return false;
            }

            lock (s_amsiLockObject)
            {
                if (s_amsiNotifyFailed)
                {
                    return false;
                }

                try
                {
                    if (!CheckAmsiInit())
                    {
                        return false;
                    }

                    int hr;
                    AmsiNativeMethods.AMSI_RESULT result = AmsiNativeMethods.AMSI_RESULT.AMSI_RESULT_NOT_DETECTED;
                    unsafe
                    {
                        fixed (char* buffer = content)
                        {
                            var buffPtr = new IntPtr(buffer);
                            hr = AmsiNativeMethods.AmsiNotifyOperation(
                                amsiContext: s_amsiContext,
                                buffer: buffPtr,
                                length: (uint)(content.Length * sizeof(char)),
                                contentName: name,
                                ref result);
                        }
                    }

                    if (Utils.Succeeded(hr))
                    {
                        if (result == AmsiNativeMethods.AMSI_RESULT.AMSI_RESULT_DETECTED)
                        {
                            // If malware is detected, throw to prevent method invoke expression from running.
                            throw new PSSecurityException(ParserStrings.ScriptContainedMaliciousContent);
                        }

                        return true;
                    }

                    return false;
                }
                catch (DllNotFoundException)
                {
                    s_amsiNotifyFailed = true;
                    return false;
                }
                catch (System.EntryPointNotFoundException)
                {
                    s_amsiNotifyFailed = true;
                    return false;
                }
            }
        }

        private static bool CheckAmsiInit()
        {
            // Initialize AntiMalware Scan Interface, if not already initialized.
            // If we failed to initialize previously, just return the neutral result ("AMSI_RESULT_NOT_DETECTED")
            if (s_amsiContext == IntPtr.Zero)
            {
                int hr = Init();

                if (!Utils.Succeeded(hr))
                {
                    return false;
                }
            }

            // Initialize the session, if one isn't already started.
            // If we failed to initialize previously, just return the neutral result ("AMSI_RESULT_NOT_DETECTED")
            if (s_amsiSession == IntPtr.Zero)
            {
                int hr = AmsiNativeMethods.AmsiOpenSession(s_amsiContext, ref s_amsiSession);
                AmsiInitialized = true;

                if (!Utils.Succeeded(hr))
                {
                    return false;
                }
            }

            return true;
        }

        internal static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            if (AmsiInitialized && !AmsiUninitializeCalled)
            {
                Uninitialize();
            }
        }

        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private static IntPtr s_amsiContext = IntPtr.Zero;

        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private static IntPtr s_amsiSession = IntPtr.Zero;

        private static readonly bool s_amsiInitFailed = false;
        private static bool s_amsiNotifyFailed = false;
        private static readonly object s_amsiLockObject = new object();

        /// <summary>
        /// Reset the AMSI session (used to track related script invocations)
        /// </summary>
        internal static void CloseSession()
        {
#if !UNIX
            WinCloseSession();
#endif
        }

        internal static void WinCloseSession()
        {
            if (!s_amsiInitFailed)
            {
                if ((s_amsiContext != IntPtr.Zero) && (s_amsiSession != IntPtr.Zero))
                {
                    lock (s_amsiLockObject)
                    {
                        // Clean up the session if one was open.
                        if ((s_amsiContext != IntPtr.Zero) && (s_amsiSession != IntPtr.Zero))
                        {
                            AmsiNativeMethods.AmsiCloseSession(s_amsiContext, s_amsiSession);
                            s_amsiSession = IntPtr.Zero;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Uninitialize the AMSI interface.
        /// </summary>
        internal static void Uninitialize()
        {
#if !UNIX
            WinUninitialize();
#endif
        }

        internal static void WinUninitialize()
        {
            AmsiUninitializeCalled = true;
            if (!s_amsiInitFailed)
            {
                lock (s_amsiLockObject)
                {
                    if (s_amsiContext != IntPtr.Zero)
                    {
                        CloseSession();

                        // Unregister the event handler.
                        AppDomain.CurrentDomain.ProcessExit -= CurrentDomain_ProcessExit;

                        // Uninitialize the AMSI interface.
                        AmsiCleanedUp = true;
                        AmsiNativeMethods.AmsiUninitialize(s_amsiContext);
                        s_amsiContext = IntPtr.Zero;
                    }
                }
            }
        }

        public static bool AmsiUninitializeCalled = false;
        public static bool AmsiInitialized = false;
        public static bool AmsiCleanedUp = false;

        internal static class AmsiNativeMethods
        {
            internal enum AMSI_RESULT
            {
                /// AMSI_RESULT_CLEAN -> 0
                AMSI_RESULT_CLEAN = 0,

                /// AMSI_RESULT_NOT_DETECTED -> 1
                AMSI_RESULT_NOT_DETECTED = 1,

                /// Certain policies set by administrator blocked this content on this machine
                AMSI_RESULT_BLOCKED_BY_ADMIN_BEGIN = 0x4000,
                AMSI_RESULT_BLOCKED_BY_ADMIN_END = 0x4fff,

                /// AMSI_RESULT_DETECTED -> 32768
                AMSI_RESULT_DETECTED = 32768,
            }

            /// Return Type: HRESULT->LONG->int
            ///appName: LPCWSTR->WCHAR*
            ///amsiContext: HAMSICONTEXT*
            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            [DllImport("amsi.dll", EntryPoint = "AmsiInitialize", CallingConvention = CallingConvention.StdCall)]
            internal static extern int AmsiInitialize(
                [In][MarshalAs(UnmanagedType.LPWStr)] string appName, ref System.IntPtr amsiContext);

            /// Return Type: void
            ///amsiContext: HAMSICONTEXT->HAMSICONTEXT__*
            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            [DllImport("amsi.dll", EntryPoint = "AmsiUninitialize", CallingConvention = CallingConvention.StdCall)]
            internal static extern void AmsiUninitialize(System.IntPtr amsiContext);

            /// Return Type: HRESULT->LONG->int
            ///amsiContext: HAMSICONTEXT->HAMSICONTEXT__*
            ///amsiSession: HAMSISESSION*
            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            [DllImport("amsi.dll", EntryPoint = "AmsiOpenSession", CallingConvention = CallingConvention.StdCall)]
            internal static extern int AmsiOpenSession(System.IntPtr amsiContext, ref System.IntPtr amsiSession);

            /// Return Type: void
            ///amsiContext: HAMSICONTEXT->HAMSICONTEXT__*
            ///amsiSession: HAMSISESSION->HAMSISESSION__*
            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            [DllImport("amsi.dll", EntryPoint = "AmsiCloseSession", CallingConvention = CallingConvention.StdCall)]
            internal static extern void AmsiCloseSession(System.IntPtr amsiContext, System.IntPtr amsiSession);

            /// Return Type: HRESULT->LONG->int
            ///amsiContext: HAMSICONTEXT->HAMSICONTEXT__*
            ///buffer: PVOID->void*
            ///length: ULONG->unsigned int
            ///contentName: LPCWSTR->WCHAR*
            ///amsiSession: HAMSISESSION->HAMSISESSION__*
            ///result: AMSI_RESULT*
            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            [DllImport("amsi.dll", EntryPoint = "AmsiScanBuffer", CallingConvention = CallingConvention.StdCall)]
            internal static extern int AmsiScanBuffer(
            System.IntPtr amsiContext,
                System.IntPtr buffer,
                uint length,
                [In][MarshalAs(UnmanagedType.LPWStr)] string contentName,
                System.IntPtr amsiSession,
                ref AMSI_RESULT result);

            /// Return Type: HRESULT->LONG->int
            /// amsiContext: HAMSICONTEXT->HAMSICONTEXT__*
            /// buffer: PVOID->void*
            /// length: ULONG->unsigned int
            /// contentName: LPCWSTR->WCHAR*
            /// result: AMSI_RESULT*
            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            [DllImport("amsi.dll", EntryPoint = "AmsiNotifyOperation", CallingConvention = CallingConvention.StdCall)]
            internal static extern int AmsiNotifyOperation(
                System.IntPtr amsiContext,
                System.IntPtr buffer,
                uint length,
                [In][MarshalAs(UnmanagedType.LPWStr)] string contentName,
                ref AMSI_RESULT result);

            /// Return Type: HRESULT->LONG->int
            ///amsiContext: HAMSICONTEXT->HAMSICONTEXT__*
            ///string: LPCWSTR->WCHAR*
            ///contentName: LPCWSTR->WCHAR*
            ///amsiSession: HAMSISESSION->HAMSISESSION__*
            ///result: AMSI_RESULT*
            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            [DllImport("amsi.dll", EntryPoint = "AmsiScanString", CallingConvention = CallingConvention.StdCall)]
            internal static extern int AmsiScanString(
                System.IntPtr amsiContext, [In][MarshalAs(UnmanagedType.LPWStr)] string @string,
                [In][MarshalAs(UnmanagedType.LPWStr)] string contentName, System.IntPtr amsiSession, ref AMSI_RESULT result);
        }
    }
}
#pragma warning restore 56523
