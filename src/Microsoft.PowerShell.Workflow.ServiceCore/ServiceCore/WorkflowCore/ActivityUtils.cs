using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Remoting;
using System.Management.Automation.Runspaces;
using Microsoft.PowerShell.Workflow;

namespace Microsoft.PowerShell.Activities
{
    static class ActivityUtils
    {
        // Extension method to add IsNullOrEmpty to arrays.
        internal static bool IsNullOrEmpty(this System.Collections.ICollection c)
        {
            return (c == null || c.Count == 0);
        }

        private static int DefaultMaximumConnectionRedirectionCount = 5;

        internal static List<WSManConnectionInfo> GetConnectionInfo(string[] PSComputerName, string[] PSConnectionUri,
            string PSCertificateThumbprint, string PSConfigurationName,
            bool? PSUseSsl, uint? PSPort, string PSApplicationName,
            PSCredential PSCredential, AuthenticationMechanism PSAuthentication,
            bool PSAllowRedirection, System.Management.Automation.Remoting.PSSessionOption options)
        {
            List<WSManConnectionInfo> connections = new List<WSManConnectionInfo>();

            string[] machineList = null;
            bool connectByComputerName = false;

            // Connect by computername
            if ((! PSComputerName.IsNullOrEmpty()) && (PSConnectionUri.IsNullOrEmpty()))
            {
                machineList = PSComputerName;
                connectByComputerName = true;
            }
            else if ((PSComputerName.IsNullOrEmpty()) && (! PSConnectionUri.IsNullOrEmpty()))
            {
                machineList = PSConnectionUri;
            }
            else
            {
                throw new ArgumentException(Resources.CannotSupplyUriAndComputername);
            }

            // Go through each machine in the list an update its properties
            foreach (string machine in machineList)
            {
                if (!string.IsNullOrEmpty(machine))
                {
                    WSManConnectionInfo connectionInfo = new WSManConnectionInfo();

                    if (PSPort.HasValue)
                    {
                        connectionInfo.Port = (int)PSPort.Value;
                    }

                    if (PSUseSsl.HasValue && (PSUseSsl.Value))
                    {
                        connectionInfo.Scheme = WSManConnectionInfo.HttpsScheme;
                    }

                    if (!String.IsNullOrEmpty(PSConfigurationName))
                    {
                        connectionInfo.ShellUri = PSConfigurationName;
                    }

                    if (!String.IsNullOrEmpty(PSApplicationName))
                    {
                        connectionInfo.AppName = PSApplicationName;
                    }

                    if (connectByComputerName)
                    {
                        connectionInfo.ComputerName = machine;
                    }
                    else
                    {
                        connectionInfo.ConnectionUri = (Uri)LanguagePrimitives.ConvertTo(machine, typeof(Uri), System.Globalization.CultureInfo.InvariantCulture);
                    }

                    if (PSCredential != null)
                    {
                        connectionInfo.Credential = PSCredential;
                    }

                    if (!String.IsNullOrEmpty(PSCertificateThumbprint))
                    {
                        connectionInfo.CertificateThumbprint = PSCertificateThumbprint;
                    }

                    if (PSAuthentication != AuthenticationMechanism.Default)
                    {
                        connectionInfo.AuthenticationMechanism = PSAuthentication;
                    }

                    connectionInfo.MaximumConnectionRedirectionCount = PSAllowRedirection ? DefaultMaximumConnectionRedirectionCount : 0;

                    if (options != null)
                    {
                        connectionInfo.SetSessionOptions(options);
                    }

                    connections.Add(connectionInfo);
                }
                else
                {
                    // add a null connection to account for "" or $null in PSComputerName parameter
                    connections.Add(null);
                }
            }

            return connections;
        }
    }
}