// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives

using System.Collections.Generic;
using System.Management.Automation;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    /// <summary>
    /// Containing all information originated from
    /// the parameters of <see cref="GetCimClassCommand"/>
    /// </summary>
    internal class CimGetCimClassContext : XOperationContextBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CimGetCimClassContext"/> class.
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="propertyName"></param>
        /// <param name="qualifierName"></param>
        internal CimGetCimClassContext(
            string theClassName,
            string theMethodName,
            string thePropertyName,
            string theQualifierName)
        {
            this.ClassName = theClassName;
            this.MethodName = theMethodName;
            this.PropertyName = thePropertyName;
            this.QualifierName = theQualifierName;
        }

        /// <summary>
        /// <para>
        /// The following is the definition of the input parameter "ClassName".
        /// </para>
        /// <para>
        /// Wildcard expansion should be allowed.
        /// </para>
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// <para>
        /// The following is the definition of the input parameter "MethodName",
        /// Which may contains wildchar.
        /// Then Filter the <see cref="CimClass"/> by given methodname
        /// </para>
        /// </summary>
        internal string MethodName { get; }

        /// <summary>
        /// <para>
        /// The following is the definition of the input parameter "PropertyName",
        /// Which may contains wildchar.
        /// Filter the <see cref="CimClass"/> by given property name.
        /// </para>
        /// </summary>
        internal string PropertyName { get; }

        /// <summary>
        /// <para>
        /// The following is the definition of the input parameter "QualifierName",
        /// Which may contains wildchar.
        /// Filter the <see cref="CimClass"/> by given methodname
        /// </para>
        /// </summary>
        internal string QualifierName { get; }
    }

    /// <summary>
    /// <para>
    /// Implements operations of get-cimclass cmdlet.
    /// </para>
    /// </summary>
    internal sealed class CimGetCimClass : CimAsyncOperation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CimGetCimClass"/> class.
        /// </summary>
        public CimGetCimClass()
            : base()
        {
        }

        /// <summary>
        /// <para>
        /// Base on parametersetName to retrieve <see cref="CimClass"/>
        /// </para>
        /// </summary>
        /// <param name="cmdlet"><see cref="GetCimClassCommand"/> object.</param>
        public void GetCimClass(GetCimClassCommand cmdlet)
        {
            List<CimSessionProxy> proxys = new();
            string nameSpace = ConstValue.GetNamespace(cmdlet.Namespace);
            string className = cmdlet.ClassName ?? @"*";
            CimGetCimClassContext context = new(
                cmdlet.ClassName,
                cmdlet.MethodName,
                cmdlet.PropertyName,
                cmdlet.QualifierName);
            switch (cmdlet.ParameterSetName)
            {
                case CimBaseCommand.ComputerSetName:
                    {
                        IEnumerable<string> computerNames = ConstValue.GetComputerNames(
                                cmdlet.ComputerName);
                        foreach (string computerName in computerNames)
                        {
                            CimSessionProxy proxy = CreateSessionProxy(computerName, cmdlet);
                            proxy.ContextObject = context;
                            proxys.Add(proxy);
                        }
                    }

                    break;
                case CimBaseCommand.SessionSetName:
                    {
                        foreach (CimSession session in cmdlet.CimSession)
                        {
                            CimSessionProxy proxy = CreateSessionProxy(session, cmdlet);
                            proxy.ContextObject = context;
                            proxys.Add(proxy);
                        }
                    }

                    break;
                default:
                    return;
            }

            if (WildcardPattern.ContainsWildcardCharacters(className))
            {
                // retrieve all classes and then filter based on
                // classname, propertyname, methodname, and qualifiername
                foreach (CimSessionProxy proxy in proxys)
                {
                    proxy.EnumerateClassesAsync(nameSpace);
                }
            }
            else
            {
                foreach (CimSessionProxy proxy in proxys)
                {
                    proxy.GetClassAsync(nameSpace, className);
                }
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
            GetCimClassCommand cmdlet)
        {
            proxy.OperationTimeout = cmdlet.OperationTimeoutSec;
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
            GetCimClassCommand cmdlet)
        {
            CimSessionProxy proxy = new CimSessionProxyGetCimClass(computerName);
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
            GetCimClassCommand cmdlet)
        {
            CimSessionProxy proxy = new CimSessionProxyGetCimClass(session);
            this.SubscribeEventAndAddProxytoCache(proxy);
            SetSessionProxyProperties(ref proxy, cmdlet);
            return proxy;
        }

        #endregion

    }
}
