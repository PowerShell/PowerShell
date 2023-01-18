// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    /// <summary>
    /// Containing all information originated from
    /// the parameters of <see cref="NewCimInstanceCommand"/>
    /// </summary>
    internal class CimNewCimInstanceContext : XOperationContextBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CimNewCimInstanceContext"/> class.
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="propertyName"></param>
        /// <param name="qualifierName"></param>
        internal CimNewCimInstanceContext(
            CimSessionProxy theProxy,
            string theNamespace)
        {
            this.proxy = theProxy;
            this.nameSpace = theNamespace;
        }
    }

    /// <summary>
    /// <para>
    /// Implements operations of new-ciminstance cmdlet.
    /// </para>
    /// </summary>
    internal sealed class CimNewCimInstance : CimAsyncOperation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CimNewCimInstance"/> class.
        /// <para>
        /// Constructor
        /// </para>
        /// </summary>
        public CimNewCimInstance()
            : base()
        {
        }

        /// <summary>
        /// <para>
        /// Base on parametersetName to create ciminstances,
        /// either remotely or locally
        /// </para>
        /// </summary>
        /// <param name="cmdlet"><see cref="GetCimInstanceCommand"/> object.</param>
        public void NewCimInstance(NewCimInstanceCommand cmdlet)
        {
            DebugHelper.WriteLogEx();

            string nameSpace;
            CimInstance cimInstance = null;
            try
            {
                switch (cmdlet.ParameterSetName)
                {
                    case CimBaseCommand.ClassNameComputerSet:
                    case CimBaseCommand.ClassNameSessionSet:
                        {
                            nameSpace = ConstValue.GetNamespace(cmdlet.Namespace);
                            cimInstance = CreateCimInstance(cmdlet.ClassName,
                                nameSpace,
                                cmdlet.Key,
                                cmdlet.Property,
                                cmdlet);
                        }

                        break;
                    case CimBaseCommand.ResourceUriSessionSet:
                    case CimBaseCommand.ResourceUriComputerSet:
                        {
                            nameSpace = cmdlet.Namespace; // passing null is ok for resourceUri set
                            cimInstance = CreateCimInstance("DummyClass",
                                nameSpace,
                                cmdlet.Key,
                                cmdlet.Property,
                                cmdlet);
                        }

                        break;
                    case CimBaseCommand.CimClassComputerSet:
                    case CimBaseCommand.CimClassSessionSet:
                        {
                            nameSpace = ConstValue.GetNamespace(cmdlet.CimClass.CimSystemProperties.Namespace);
                            cimInstance = CreateCimInstance(cmdlet.CimClass,
                                cmdlet.Property,
                                cmdlet);
                        }

                        break;
                    default:
                        return;
                }
            }
            catch (ArgumentNullException e)
            {
                cmdlet.ThrowTerminatingError(e, action);
                return;
            }
            catch (ArgumentException e)
            {
                cmdlet.ThrowTerminatingError(e, action);
                return;
            }

            // return if create client only ciminstance
            if (cmdlet.ClientOnly)
            {
                cmdlet.CmdletOperation.WriteObject(cimInstance, null);
                return;
            }

            string target = cimInstance.ToString();
            if (!cmdlet.ShouldProcess(target, action))
            {
                return;
            }

            // create ciminstance on server
            List<CimSessionProxy> proxys = new();

            switch (cmdlet.ParameterSetName)
            {
                case CimBaseCommand.ClassNameComputerSet:
                case CimBaseCommand.CimClassComputerSet:
                case CimBaseCommand.ResourceUriComputerSet:
                    {
                        IEnumerable<string> computerNames = ConstValue.GetComputerNames(
                            cmdlet.ComputerName);
                        foreach (string computerName in computerNames)
                        {
                            proxys.Add(CreateSessionProxy(computerName, cmdlet));
                        }
                    }

                    break;
                case CimBaseCommand.CimClassSessionSet:
                case CimBaseCommand.ClassNameSessionSet:
                case CimBaseCommand.ResourceUriSessionSet:
                    foreach (CimSession session in cmdlet.CimSession)
                    {
                        proxys.Add(CreateSessionProxy(session, cmdlet));
                    }

                    break;
            }

            foreach (CimSessionProxy proxy in proxys)
            {
                proxy.ContextObject = new CimNewCimInstanceContext(proxy, nameSpace);
                proxy.CreateInstanceAsync(nameSpace, cimInstance);
            }
        }

        #region Get CimInstance after creation (on server)

        /// <summary>
        /// <para>
        /// Get full <see cref="CimInstance"/> from server based on the key
        /// </para>
        /// </summary>
        /// <param name="cimInstance"></param>
        internal void GetCimInstance(CimInstance cimInstance, XOperationContextBase context)
        {
            DebugHelper.WriteLogEx();

            CimNewCimInstanceContext newCimInstanceContext = context as CimNewCimInstanceContext;
            if (newCimInstanceContext == null)
            {
                DebugHelper.WriteLog("Invalid (null) CimNewCimInstanceContext", 1);
                return;
            }

            CimSessionProxy proxy = CreateCimSessionProxy(newCimInstanceContext.Proxy);
            string nameSpace = cimInstance.CimSystemProperties.Namespace ?? newCimInstanceContext.Namespace;
            proxy.GetInstanceAsync(nameSpace, cimInstance);
        }

        #endregion

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
            NewCimInstanceCommand cmdlet)
        {
            proxy.OperationTimeout = cmdlet.OperationTimeoutSec;
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
        /// <param name="cmdlet"></param>
        /// <returns></returns>
        private CimSessionProxy CreateSessionProxy(
            string computerName,
            NewCimInstanceCommand cmdlet)
        {
            CimSessionProxy proxy = new CimSessionProxyNewCimInstance(computerName, this);
            this.SubscribeEventAndAddProxytoCache(proxy);
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
            NewCimInstanceCommand cmdlet)
        {
            CimSessionProxy proxy = new CimSessionProxyNewCimInstance(session, this);
            this.SubscribeEventAndAddProxytoCache(proxy);
            SetSessionProxyProperties(ref proxy, cmdlet);
            return proxy;
        }

        /// <summary>
        /// <para>
        /// Create <see cref="CimInstance"/> with given properties.
        /// </para>
        /// </summary>
        /// <param name="className"></param>
        /// <param name="key"></param>
        /// <param name="properties"></param>
        /// <param name="cmdlet"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">See CimProperty.Create.</exception>
        /// <exception cref="ArgumentException">CimProperty.Create.</exception>
        private CimInstance CreateCimInstance(
            string className,
            string cimNamespace,
            IEnumerable<string> key,
            IDictionary properties,
            NewCimInstanceCommand cmdlet)
        {
            CimInstance cimInstance = new(className, cimNamespace);
            if (properties == null)
            {
                return cimInstance;
            }

            List<string> keys = new();
            if (key != null)
            {
                foreach (string keyName in key)
                {
                    keys.Add(keyName);
                }
            }

            IDictionaryEnumerator enumerator = properties.GetEnumerator();
            while (enumerator.MoveNext())
            {
                CimFlags flag = CimFlags.None;
                string propertyName = enumerator.Key.ToString().Trim();
                if (keys.Contains(propertyName, StringComparer.OrdinalIgnoreCase))
                {
                    flag = CimFlags.Key;
                }

                object propertyValue = GetBaseObject(enumerator.Value);

                DebugHelper.WriteLog($"Create and add new property to ciminstance: name = {propertyName}; value = {propertyValue}; flags = {flag}", 5);

                PSReference cimReference = propertyValue as PSReference;
                if (cimReference != null)
                {
                    CimProperty newProperty = CimProperty.Create(propertyName, GetBaseObject(cimReference.Value), CimType.Reference, flag);
                    cimInstance.CimInstanceProperties.Add(newProperty);
                }
                else
                {
                    CimProperty newProperty = CimProperty.Create(
                        propertyName,
                        propertyValue,
                        flag);
                    cimInstance.CimInstanceProperties.Add(newProperty);
                }
            }

            return cimInstance;
        }

        /// <summary>
        /// <para>
        /// Create <see cref="CimInstance"/> with given properties.
        /// </para>
        /// </summary>
        /// <param name="cimClass"></param>
        /// <param name="properties"></param>
        /// <param name="cmdlet"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">See CimProperty.Create.</exception>
        /// <exception cref="ArgumentException">CimProperty.Create.</exception>
        private CimInstance CreateCimInstance(
            CimClass cimClass,
            IDictionary properties,
            NewCimInstanceCommand cmdlet)
        {
            CimInstance cimInstance = new(cimClass);
            if (properties == null)
            {
                return cimInstance;
            }

            List<string> notfoundProperties = new();
            foreach (string property in properties.Keys)
            {
                if (cimInstance.CimInstanceProperties[property] == null)
                {
                    notfoundProperties.Add(property);
                    cmdlet.ThrowInvalidProperty(notfoundProperties, cmdlet.CimClass.CimSystemProperties.ClassName, @"Property", action, properties);
                    return null;
                }

                object propertyValue = GetBaseObject(properties[property]);
                cimInstance.CimInstanceProperties[property].Value = propertyValue;
            }

            return cimInstance;
        }

        #endregion

        #region const strings
        /// <summary>
        /// Action.
        /// </summary>
        private const string action = @"New-CimInstance";
        #endregion
    }
}
