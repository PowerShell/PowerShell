using System;
using System.Security;
using System.Runtime.InteropServices;
using System.Management.Automation;
using System.Diagnostics;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Remoting;
using System.Management.Automation.Tracing;

namespace Microsoft.PowerShell.Workflow
{
    static class WorkflowUtils
    {
        /// <summary>
        /// CompareConnectionUri compares two connection URIs
        /// by doing a comparison of elements.
        /// </summary>
        /// <param name="connectionInfo1">Connection info 1</param>
        /// <param name="connectionInfo2">Connection info 2</param>
        /// <returns>True if they match else false.</returns>
        internal static bool CompareConnectionUri(WSManConnectionInfo connectionInfo1, WSManConnectionInfo connectionInfo2)
        {
            Debug.Assert(connectionInfo1 != null && connectionInfo2 != null, "Connections should be != null");
            Debug.Assert(!string.IsNullOrEmpty(connectionInfo1.Scheme) && !string.IsNullOrEmpty(connectionInfo1.ComputerName)
                && !string.IsNullOrEmpty(connectionInfo1.AppName), "Connection URI elements should be != null or empty");
            Debug.Assert(!string.IsNullOrEmpty(connectionInfo2.Scheme) && !string.IsNullOrEmpty(connectionInfo2.ComputerName)
                && !string.IsNullOrEmpty(connectionInfo2.AppName), "Connection URI elements should be != null or empty");

            if (String.Compare(connectionInfo2.Scheme, connectionInfo1.Scheme, StringComparison.OrdinalIgnoreCase) != 0)
            {
                return false;
            }
            if (String.Compare(connectionInfo2.ComputerName, connectionInfo1.ComputerName, StringComparison.OrdinalIgnoreCase) != 0)
            {
                return false;
            }
            if (String.Compare(connectionInfo2.AppName, connectionInfo1.AppName, StringComparison.OrdinalIgnoreCase) != 0)
            {
                return false;
            }
            if (connectionInfo2.Port != connectionInfo1.Port)
            {
                return false;
            }

            return true;
        }  // CompareConnectionUri ... 

        /// <summary>
        /// CompareShellUri compares two shell URIs
        /// by doing a string of elements.
        /// </summary>
        /// <param name="shellUri1">Shell Uri 1</param>
        /// <param name="shellUri2">Shell Uri 2</param>
        /// <returns>True if they match else false.</returns>
        internal static bool CompareShellUri(String shellUri1, String shellUri2)
        {
            Debug.Assert(!string.IsNullOrEmpty(shellUri1) && !string.IsNullOrEmpty(shellUri2), "Shell Uris should be != null or empty");

            if (String.Compare(shellUri1, shellUri2, StringComparison.OrdinalIgnoreCase) != 0)
            {
                return false;
            }

            return true;
        } // CompareShellUri ... 


        /// <summary>
        /// CompareAuthentication compares two authentication mechanisms.
        /// </summary>
        /// <param name="authentication1">Authentication mechanism 1</param>
        /// <param name="authentication2">Authentication mechanism 2</param>
        /// <returns>True if they match else false.</returns>
        internal static bool CompareAuthentication(AuthenticationMechanism authentication1, AuthenticationMechanism authentication2)
        {
            return authentication1 == authentication2;
        }  // CompareAuthentication ... 

        /// <summary>
        /// CompareCredentials compares two PSCredential credentials
        /// by doing a username and password comparison .
        /// </summary>
        /// <param name="credential1">Credential 1</param>
        /// <param name="credential2">Credential 2</param>
        /// <returns>True if they match else false.</returns>
        internal static bool CompareCredential(PSCredential credential1, PSCredential credential2)
        {
            if (credential1 == null && credential2 == null)
            {
                return true;
            }

            // check credentials if present
            if (credential1 == null ^ credential2 == null)
            {
                return false;
            }

            Debug.Assert(credential1 != null && credential2 != null 
                && credential1.UserName != null && credential2.UserName != null, "Credentials should be != null");

            // check the username
            if (string.Compare(credential1.UserName, credential2.UserName, StringComparison.OrdinalIgnoreCase) != 0)
            {
                return false;
            }

            // check the password
            if (!WorkflowUtils.ComparePassword(credential1.Password, credential2.Password))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// ComparePassword uses native functions to perform a string match on two SecureString passwords
        /// by doing a strict byte level comparison is done on the two strings.
        /// The use of ReadByte allows the function to execute without marking the assembly as unsafe.
        /// </summary>
        /// <param name="secureString1">Password 1</param>
        /// <param name="secureString2">Password 2</param>
        /// <returns>True if they match else false.</returns>
        internal static bool ComparePassword(SecureString secureString1, SecureString secureString2)
        {
            if (secureString1 == null && secureString2 == null)
            {
                return true;
            }

            if (secureString1 == null ^ secureString2 == null)
            {
                return false;
            }

            Debug.Assert(secureString1 != null && secureString2 != null, "SecureStrings should be != null");

            if (secureString1.Length != secureString2.Length)
            {
                return false;
            }

            IntPtr bstr1 = IntPtr.Zero;
            IntPtr bstr2 = IntPtr.Zero;

            try
            {
                bstr1 = Marshal.SecureStringToBSTR(secureString1);
                bstr2 = Marshal.SecureStringToBSTR(secureString2);

                int offset = 0;
                Byte leftHigh, leftLow, rightHigh, rightLow;
                bool notDone = true;

                do
                {
                    leftLow = Marshal.ReadByte(bstr1, offset + 1);
                    rightLow = Marshal.ReadByte(bstr2, offset + 1);
                    leftHigh = Marshal.ReadByte(bstr1, offset);
                    rightHigh = Marshal.ReadByte(bstr2, offset);
                    offset += 2;
                    if (leftLow != rightLow || leftHigh != rightHigh)
                    {
                        return false;
                    }
                    notDone = leftLow != 0 || leftHigh != 0; // terminator - 2 null characters (0x00)?
                    leftLow = rightLow = leftHigh = rightHigh = 0;
                } while (notDone); 

                return true;
            }
            catch (Exception e)
            {
                using (PowerShellTraceSource tracer = PowerShellTraceSourceFactory.GetTraceSource())
                {
                    // SecureStringToBSTR or ReadByte threw exceptions

                    tracer.WriteMessage("Getting an exception while comparing credentials...");
                    tracer.TraceException(e);

                    return false;
                }
            }
            finally
            {
                if (IntPtr.Zero != bstr1)
                {
                    Marshal.ZeroFreeBSTR(bstr1);
                }
                if (IntPtr.Zero != bstr2)
                {
                    Marshal.ZeroFreeBSTR(bstr2);
                }
            }
        } // ComparePassword ...

        /// <summary>
        /// CompareCertificateThumbprint compares two certificate thumbprints
        /// by doing a string comparison.
        /// </summary>
        /// <param name="certificateThumbprint1">Certificate Thumbprint 1</param>
        /// <param name="certificateThumbprint2">Certificate Thumbprint 2</param>
        /// <returns>True if they match else false.</returns>
        internal static bool CompareCertificateThumbprint(String certificateThumbprint1, String certificateThumbprint2)
        {
            if (certificateThumbprint1 == null && certificateThumbprint2 == null)
            {
                return true;
            }

            if (certificateThumbprint1 == null ^ certificateThumbprint2 == null)
            {
                return false;
            }

            Debug.Assert(certificateThumbprint1 != null && certificateThumbprint2 != null, "Certificate thumbprints should be != null");

            if (string.Compare(certificateThumbprint1, certificateThumbprint2, StringComparison.OrdinalIgnoreCase) != 0)
            {
                return false;
            }

            return true;
        } // CompareCertificateThumbprint ... 

        /// <summary>
        /// CompareProxySettings compares the proxy settings for two wsman connections 
        /// by doing a comparison of elements.
        /// </summary>
        /// <param name="connectionInfo1">Connection info 1</param>
        /// <param name="connectionInfo2">Connection info 2</param>
        /// <returns>True if they match else false.</returns>
        internal static bool CompareProxySettings(WSManConnectionInfo connectionInfo1, WSManConnectionInfo connectionInfo2)
        {
            Debug.Assert(connectionInfo1 != null && connectionInfo2 != null, "Connections should be != null");

            if (connectionInfo1.ProxyAccessType != connectionInfo2.ProxyAccessType)
            {
                return false;
            }

            if (connectionInfo1.ProxyAccessType == ProxyAccessType.None)
            {
                return true; //stop here if no proxy access type
            }

            if (connectionInfo1.ProxyAuthentication != connectionInfo2.ProxyAuthentication)
            {
                return false;
            }

            // check the proxy credentials password
            if (!WorkflowUtils.CompareCredential(connectionInfo1.ProxyCredential, connectionInfo2.ProxyCredential))
            {
                return false;
            }

            return true;
        }  // CompareProxySettings ... 

        /// <summary>
        /// CompareOtherWSManSettings compares the rest of the wsman settings for two wsman connections 
        /// by doing a comparison of elements.
        /// </summary>
        /// <param name="connectionInfo1">Connection info 1</param>
        /// <param name="connectionInfo2">Connection info 2</param>
        /// <returns>True if they match else false.</returns>
        internal static bool CompareOtherWSManSettings(WSManConnectionInfo connectionInfo1, WSManConnectionInfo connectionInfo2)
        {
            Debug.Assert(connectionInfo1 != null && connectionInfo2 != null, "Connections should be != null");

            if (connectionInfo1.SkipCACheck != connectionInfo2.SkipCACheck)
            {
                return false;
            }
            if (connectionInfo1.SkipCNCheck != connectionInfo2.SkipCNCheck)
            {
                return false;
            }
            if (connectionInfo1.SkipRevocationCheck != connectionInfo2.SkipRevocationCheck)
            {
                return false;
            }
            if (connectionInfo1.UseCompression != connectionInfo2.UseCompression)
            {
                return false;
            }
            if (connectionInfo1.UseUTF16 != connectionInfo2.UseUTF16)
            {
                return false;
            }
            if (connectionInfo1.MaximumConnectionRedirectionCount != connectionInfo2.MaximumConnectionRedirectionCount)
            {
                return false;
            }
            if (connectionInfo1.MaximumReceivedDataSizePerCommand != connectionInfo2.MaximumReceivedDataSizePerCommand)
            {
                return false;
            }
            if (connectionInfo1.MaximumReceivedObjectSize != connectionInfo2.MaximumReceivedObjectSize)
            {
                return false;
            }
            if (connectionInfo1.NoEncryption != connectionInfo2.NoEncryption)
            {
                return false;
            }
            if (connectionInfo1.NoMachineProfile != connectionInfo2.NoMachineProfile)
            {
                return false;
            }
            if (connectionInfo1.OutputBufferingMode != connectionInfo2.OutputBufferingMode)
            {
                return false;
            }

            return true;
        }  // CompareOtherWSManSettings ... 
    
    } // WorkflowUtils ...
}