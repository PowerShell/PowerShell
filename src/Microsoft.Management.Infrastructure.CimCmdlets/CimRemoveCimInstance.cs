// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#region Using directives

using System.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    /// <summary>
    /// Containing all necessary information originated from
    /// the parameters of <see cref="RemoveCimInstanceCommand"/>
    /// </summary>
    internal class CimRemoveCimInstanceContext : XOperationContextBase
    {
        /// <summary>
        /// <param>
        /// Constructor
        /// </param>
        /// </summary>
        /// <param name="theNamespace"></param>
        /// <param name="theProxy"></param>
        internal CimRemoveCimInstanceContext(string theNamespace,
            CimSessionProxy theProxy)
        {
            this.proxy = theProxy;
            this.nameSpace = theNamespace;
        }
    }

    /// <summary>
    /// <param>
    /// Implements operations of remove-ciminstance cmdlet.
    /// </param>
    /// </summary>
    internal sealed class CimRemoveCimInstance : CimGetInstance
    {
        /// <summary>
        /// <param>
        /// Constructor
        /// </param>
        /// </summary>
        public CimRemoveCimInstance()
            : base()
        {
        }

        /// <summary>
        /// <param>
        /// Base on parametersetName to retrieve ciminstances
        /// </param>
        /// </summary>
        /// <param name="cmdlet"><see cref="GetCimInstanceCommand"/>object.</param>
        public void RemoveCimInstance(RemoveCimInstanceCommand cmdlet)
        {
            DebugHelper.WriteLogEx();

            IEnumerable<string> computerNames = ConstValue.GetComputerNames(
                GetComputerName(cmdlet));
            List<CimSessionProxy> proxys = new List<CimSessionProxy>();
            switch (cmdlet.ParameterSetName)
            {
                case CimBaseCommand.CimInstanceComputerSet:
                    foreach (string computerName in computerNames)
                    {
                        proxys.Add(CreateSessionProxy(computerName, cmdlet.CimInstance, cmdlet));
                    }

                    break;
                case CimBaseCommand.CimInstanceSessionSet:
                    foreach (CimSession session in GetCimSession(cmdlet))
                    {
                        proxys.Add(CreateSessionProxy(session, cmdlet));
                    }

                    break;
                default:
                    break;
            }

            switch (cmdlet.ParameterSetName)
            {
                case CimBaseCommand.CimInstanceComputerSet:
                case CimBaseCommand.CimInstanceSessionSet:
                    string nameSpace = null;
                    if(cmdlet.ResourceUri != null )
                    {
                        nameSpace = GetCimInstanceParameter(cmdlet).CimSystemProperties.Namespace;
                    }
                    else
                    {
                        nameSpace = ConstValue.GetNamespace(GetCimInstanceParameter(cmdlet).CimSystemProperties.Namespace);
                    }

                    string target = cmdlet.CimInstance.ToString();
                    foreach (CimSessionProxy proxy in proxys)
                    {
                        if (!cmdlet.ShouldProcess(target, action))
                        {
                            return;
                        }

                        proxy.DeleteInstanceAsync(nameSpace, cmdlet.CimInstance);
                    }

                    break;
                case CimBaseCommand.QueryComputerSet:
                case CimBaseCommand.QuerySessionSet:
                    GetCimInstanceInternal(cmdlet);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// <param>
        /// Remove <see cref="CimInstance"/> from namespace specified in cmdlet
        /// </param>
        /// </summary>
        /// <param name="cimInstance"></param>
        internal void RemoveCimInstance(CimInstance cimInstance, XOperationContextBase context, CmdletOperationBase cmdlet)
        {
            DebugHelper.WriteLogEx();

            string target = cimInstance.ToString();
            if (!cmdlet.ShouldProcess(target, action))
            {
                return;
            }

            CimRemoveCimInstanceContext removeContext = context as CimRemoveCimInstanceContext;
            Debug.Assert(removeContext != null, "CimRemoveCimInstance::RemoveCimInstance should has CimRemoveCimInstanceContext != NULL.");

            CimSessionProxy proxy = CreateCimSessionProxy(removeContext.Proxy);
            proxy.DeleteInstanceAsync(removeContext.Namespace, cimInstance);
        }

        #region const strings
        /// <summary>
        /// action
        /// </summary>
        private const string action = @"Remove-CimInstance";
        #endregion
    }//End Class
}//End namespace
