// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives

using System.Collections.Generic;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    /// <summary>
    /// <para>
    /// Implements operations of get-AssociatedInstance cmdlet.
    /// </para>
    /// </summary>
    internal sealed class CimGetAssociatedInstance : CimAsyncOperation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CimGetAssociatedInstance"/> class.
        /// </summary>
        public CimGetAssociatedInstance()
            : base()
        {
        }

        /// <summary>
        /// <para>
        /// Base on parametersetName to retrieve associated ciminstances
        /// </para>
        /// </summary>
        /// <param name="cmdlet"><see cref="GetCimInstanceCommand"/> object.</param>
        public void GetCimAssociatedInstance(GetCimAssociatedInstanceCommand cmdlet)
        {
            IEnumerable<string> computerNames = ConstValue.GetComputerNames(cmdlet.ComputerName);
            // use the namespace from parameter
            string nameSpace = cmdlet.Namespace;
            if ((nameSpace == null) && (cmdlet.ResourceUri == null))
            {
                // try to use namespace of ciminstance, then fall back to default namespace
                nameSpace = ConstValue.GetNamespace(cmdlet.CimInstance.CimSystemProperties.Namespace);
            }

            List<CimSessionProxy> proxys = new();
            switch (cmdlet.ParameterSetName)
            {
                case CimBaseCommand.ComputerSetName:
                    foreach (string computerName in computerNames)
                    {
                        CimSessionProxy proxy = CreateSessionProxy(computerName, cmdlet.CimInstance, cmdlet);
                        proxys.Add(proxy);
                    }

                    break;
                case CimBaseCommand.SessionSetName:
                    foreach (CimSession session in cmdlet.CimSession)
                    {
                        CimSessionProxy proxy = CreateSessionProxy(session, cmdlet);
                        proxys.Add(proxy);
                    }

                    break;
                default:
                    return;
            }

            foreach (CimSessionProxy proxy in proxys)
            {
                proxy.EnumerateAssociatedInstancesAsync(
                    nameSpace,
                    cmdlet.CimInstance,
                    cmdlet.Association,
                    cmdlet.ResultClassName,
                    null,
                    null);
            }
        }

        #region private methods

        /// <summary>
        /// <para>
        /// Set <see cref="CimSessionProxy"/> properties
        /// </para>
        /// </summary>
        /// <param name="proxy"></param>
        /// <param name="cmdlet"></param>
        private static void SetSessionProxyProperties(
            ref CimSessionProxy proxy,
            GetCimAssociatedInstanceCommand cmdlet)
        {
            proxy.OperationTimeout = cmdlet.OperationTimeoutSec;
            proxy.KeyOnly = cmdlet.KeyOnly;
            if (cmdlet.ResourceUri != null)
            {
                proxy.ResourceUri = cmdlet.ResourceUri;
            }
        }

        /// <summary>
        /// <para>
        /// Create <see cref="CimSessionProxy"/> and set properties
        /// </para>
        /// </summary>
        /// <param name="computerName"></param>
        /// <param name="cimInstance"></param>
        /// <param name="cmdlet"></param>
        /// <returns></returns>
        private CimSessionProxy CreateSessionProxy(
            string computerName,
            CimInstance cimInstance,
            GetCimAssociatedInstanceCommand cmdlet)
        {
            CimSessionProxy proxy = CreateCimSessionProxy(computerName, cimInstance);
            SetSessionProxyProperties(ref proxy, cmdlet);
            return proxy;
        }

        /// <summary>
        /// Create <see cref="CimSessionProxy"/> and set properties.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="cmdlet"></param>
        /// <returns></returns>
        private CimSessionProxy CreateSessionProxy(
            CimSession session,
            GetCimAssociatedInstanceCommand cmdlet)
        {
            CimSessionProxy proxy = CreateCimSessionProxy(session);
            SetSessionProxyProperties(ref proxy, cmdlet);
            return proxy;
        }

        #endregion

    }
}
