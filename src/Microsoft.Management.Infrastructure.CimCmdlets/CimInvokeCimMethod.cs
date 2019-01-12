// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#region Using directives

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Management.Automation;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    /// <summary>
    /// <para>
    /// Implements operations of invoke-cimmethod cmdlet.
    /// </para>
    /// </summary>
    internal sealed class CimInvokeCimMethod : CimAsyncOperation
    {
        /// <summary>
        /// Containing all necessary information originated from
        /// the parameters of <see cref="InvokeCimMethodCommand"/>
        /// </summary>
        internal class CimInvokeCimMethodContext : XOperationContextBase
        {
            /// <summary>
            /// <para>
            /// Constructor
            /// </para>
            /// </summary>
            /// <param name="theNamespace"></param>
            /// <param name="theCollection"></param>
            /// <param name="theProxy"></param>
            internal CimInvokeCimMethodContext(string theNamespace,
                string theMethodName,
                CimMethodParametersCollection theCollection,
                CimSessionProxy theProxy)
            {
                this.proxy = theProxy;
                this.methodName = theMethodName;
                this.collection = theCollection;
                this.nameSpace = theNamespace;
            }

            /// <summary>
            /// <para>namespace</para>
            /// </summary>
            internal string MethodName
            {
                get
                {
                    return this.methodName;
                }
            }

            private string methodName;

            /// <summary>
            /// <para>parameters collection</para>
            /// </summary>
            internal CimMethodParametersCollection ParametersCollection
            {
                get
                {
                    return this.collection;
                }
            }

            private CimMethodParametersCollection collection;
        }

        /// <summary>
        /// <para>
        /// Constructor
        /// </para>
        /// </summary>
        public CimInvokeCimMethod()
            : base()
        {
        }

        /// <summary>
        /// <para>
        /// Base on parametersetName to retrieve ciminstances
        /// </para>
        /// </summary>
        /// <param name="cmdlet"><see cref="GetCimInstanceCommand"/> object.</param>
        public void InvokeCimMethod(InvokeCimMethodCommand cmdlet)
        {
            IEnumerable<string> computerNames = ConstValue.GetComputerNames(cmdlet.ComputerName);
            string nameSpace;
            List<CimSessionProxy> proxys = new List<CimSessionProxy>();
            string action = string.Format(CultureInfo.CurrentUICulture, actionTemplate, cmdlet.MethodName);

            switch (cmdlet.ParameterSetName)
            {
                case CimBaseCommand.CimInstanceComputerSet:
                    foreach (string computerName in computerNames)
                    {
                        proxys.Add(CreateSessionProxy(computerName, cmdlet.CimInstance, cmdlet));
                    }

                    break;
                case CimBaseCommand.ClassNameComputerSet:
                case CimBaseCommand.CimClassComputerSet:
                case CimBaseCommand.ResourceUriComputerSet:
                case CimBaseCommand.QueryComputerSet:
                    foreach (string computerName in computerNames)
                    {
                        proxys.Add(CreateSessionProxy(computerName, cmdlet));
                    }

                    break;
                case CimBaseCommand.ClassNameSessionSet:
                case CimBaseCommand.CimClassSessionSet:
                case CimBaseCommand.QuerySessionSet:
                case CimBaseCommand.CimInstanceSessionSet:
                case CimBaseCommand.ResourceUriSessionSet:
                    foreach (CimSession session in cmdlet.CimSession)
                    {
                        CimSessionProxy proxy = CreateSessionProxy(session, cmdlet);
                        proxys.Add(proxy);
                    }

                    break;
                default:
                    break;
            }

            CimMethodParametersCollection paramsCollection =
                CreateParametersCollection(cmdlet.Arguments, cmdlet.CimClass, cmdlet.CimInstance, cmdlet.MethodName);

            // Invoke methods
            switch (cmdlet.ParameterSetName)
            {
                case CimBaseCommand.ClassNameComputerSet:
                case CimBaseCommand.ClassNameSessionSet:
                case CimBaseCommand.ResourceUriSessionSet:
                case CimBaseCommand.ResourceUriComputerSet:
                    {
                        string target = string.Format(CultureInfo.CurrentUICulture, targetClass, cmdlet.ClassName);
                        if(cmdlet.ResourceUri != null )
                        {
                            nameSpace = cmdlet.Namespace;
                        }
                        else
                        {
                            nameSpace = ConstValue.GetNamespace(cmdlet.Namespace);
                        }

                        foreach (CimSessionProxy proxy in proxys)
                        {
                            if (!cmdlet.ShouldProcess(target, action))
                            {
                                return;
                            }

                            proxy.InvokeMethodAsync(
                                nameSpace,
                                cmdlet.ClassName,
                                cmdlet.MethodName,
                                paramsCollection);
                        }
                    }

                    break;
                case CimBaseCommand.CimClassComputerSet:
                case CimBaseCommand.CimClassSessionSet:
                    {
                        string target = string.Format(CultureInfo.CurrentUICulture, targetClass, cmdlet.CimClass.CimSystemProperties.ClassName);
                        nameSpace = ConstValue.GetNamespace(cmdlet.CimClass.CimSystemProperties.Namespace);
                        foreach (CimSessionProxy proxy in proxys)
                        {
                            if (!cmdlet.ShouldProcess(target, action))
                            {
                                return;
                            }

                            proxy.InvokeMethodAsync(
                                nameSpace,
                                cmdlet.CimClass.CimSystemProperties.ClassName,
                                cmdlet.MethodName,
                                paramsCollection);
                        }
                    }

                    break;
                case CimBaseCommand.QueryComputerSet:
                case CimBaseCommand.QuerySessionSet:
                    nameSpace = ConstValue.GetNamespace(cmdlet.Namespace);
                    foreach (CimSessionProxy proxy in proxys)
                    {
                        // create context object
                        CimInvokeCimMethodContext context = new CimInvokeCimMethodContext(
                            nameSpace,
                            cmdlet.MethodName,
                            paramsCollection,
                            proxy);
                        proxy.ContextObject = context;
                        // firstly query instance and then invoke method upon returned instances
                        proxy.QueryInstancesAsync(nameSpace, ConstValue.GetQueryDialectWithDefault(cmdlet.QueryDialect), cmdlet.Query);
                    }

                    break;
                case CimBaseCommand.CimInstanceComputerSet:
                case CimBaseCommand.CimInstanceSessionSet:
                    {
                        string target = cmdlet.CimInstance.ToString();
                        if(cmdlet.ResourceUri != null )
                        {
                            nameSpace = cmdlet.Namespace;
                        }
                        else
                        {
                            nameSpace = ConstValue.GetNamespace(cmdlet.CimInstance.CimSystemProperties.Namespace);
                        }

                        foreach (CimSessionProxy proxy in proxys)
                        {
                            if (!cmdlet.ShouldProcess(target, action))
                            {
                                return;
                            }

                            proxy.InvokeMethodAsync(
                                nameSpace,
                                cmdlet.CimInstance,
                                cmdlet.MethodName,
                                paramsCollection);
                        }
                    }

                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// <para>
        /// Invoke cimmethod on given <see cref="CimInstance"/>
        /// </para>
        /// </summary>
        /// <param name="cimInstance"></param>
        public void InvokeCimMethodOnCimInstance(CimInstance cimInstance, XOperationContextBase context, CmdletOperationBase operation)
        {
            DebugHelper.WriteLogEx();
            CimInvokeCimMethodContext cimInvokeCimMethodContext = context as CimInvokeCimMethodContext;
            Debug.Assert(cimInvokeCimMethodContext != null, "CimInvokeCimMethod::InvokeCimMethodOnCimInstance should has CimInvokeCimMethodContext != NULL.");

            string action = string.Format(CultureInfo.CurrentUICulture, actionTemplate, cimInvokeCimMethodContext.MethodName);
            if (!operation.ShouldProcess(cimInstance.ToString(), action))
            {
                return;
            }

            CimSessionProxy proxy = CreateCimSessionProxy(cimInvokeCimMethodContext.Proxy);
            proxy.InvokeMethodAsync(
                cimInvokeCimMethodContext.Namespace,
                cimInstance,
                cimInvokeCimMethodContext.MethodName,
                cimInvokeCimMethodContext.ParametersCollection);
        }

        #region private methods

        /// <summary>
        /// <para>
        /// Set <see cref="CimSessionProxy"/> properties
        /// </para>
        /// </summary>
        /// <param name="proxy"></param>
        /// <param name="cmdlet"></param>
        private void SetSessionProxyProperties(
            ref CimSessionProxy proxy,
            InvokeCimMethodCommand cmdlet)
        {
            proxy.OperationTimeout = cmdlet.OperationTimeoutSec;
            if(cmdlet.ResourceUri != null )
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
            InvokeCimMethodCommand cmdlet)
        {
            CimSessionProxy proxy = CreateCimSessionProxy(computerName);
            SetSessionProxyProperties(ref proxy, cmdlet);
            return proxy;
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
            InvokeCimMethodCommand cmdlet)
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
            InvokeCimMethodCommand cmdlet)
        {
            CimSessionProxy proxy = CreateCimSessionProxy(session);
            SetSessionProxyProperties(ref proxy, cmdlet);
            return proxy;
        }

        /// <summary>
        /// <para>
        /// Create <see cref="CimMethodParametersCollection"/> with given key properties.
        /// And/or <see cref="CimClass"/> object.
        /// </para>
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="cimClass"></param>
        /// <param name="cimInstance"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">See CimProperty.Create.</exception>
        /// <exception cref="ArgumentException">CimProperty.Create.</exception>
        private CimMethodParametersCollection CreateParametersCollection(
            IDictionary parameters,
            CimClass cimClass,
            CimInstance cimInstance,
            string methodName)
        {
            DebugHelper.WriteLogEx();

            CimMethodParametersCollection collection = null;
            if (parameters == null)
            {
                return collection;
            }
            else if (parameters.Count == 0)
            {
                return collection;
            }

            collection = new CimMethodParametersCollection();
            IDictionaryEnumerator enumerator = parameters.GetEnumerator();
            while (enumerator.MoveNext())
            {
                string parameterName = enumerator.Key.ToString();

                CimFlags parameterFlags = CimFlags.In;
                object parameterValue = GetBaseObject(enumerator.Value);

                DebugHelper.WriteLog(@"Create parameter name= {0}, value= {1}, flags= {2}.", 4,
                    parameterName,
                    parameterValue,
                    parameterFlags);

                CimMethodParameter parameter = null;
                CimMethodDeclaration declaration = null;
                string className = null;
                if (cimClass != null)
                {
                    className = cimClass.CimSystemProperties.ClassName;
                    declaration = cimClass.CimClassMethods[methodName];
                    if (declaration == null)
                    {
                        throw new ArgumentException(string.Format(
                                CultureInfo.CurrentUICulture, Strings.InvalidMethod, methodName, className));
                    }
                }
                else if (cimInstance != null)
                {
                    className = cimInstance.CimClass.CimSystemProperties.ClassName;
                    declaration = cimInstance.CimClass.CimClassMethods[methodName];
                }

                if (declaration != null)
                {
                    CimMethodParameterDeclaration paramDeclaration = declaration.Parameters[parameterName];
                    if (paramDeclaration == null)
                    {
                        throw new ArgumentException(string.Format(
                            CultureInfo.CurrentUICulture, Strings.InvalidMethodParameter, parameterName, methodName, className));
                    }

                    parameter = CimMethodParameter.Create(
                        parameterName,
                        parameterValue,
                        paramDeclaration.CimType,
                        parameterFlags);
                    // FIXME: check in/out qualifier
                    // parameterFlags = paramDeclaration.Qualifiers;
                }
                else
                {
                    if (parameterValue == null)
                    {
                        // try the best to get the type while value is null
                        parameter = CimMethodParameter.Create(
                            parameterName,
                            parameterValue,
                            CimType.String,
                            parameterFlags);
                    }
                    else
                    {
                        CimType referenceType = CimType.Unknown;
                        object referenceObject = GetReferenceOrReferenceArrayObject(parameterValue, ref referenceType);
                        if (referenceObject != null)
                        {
                            parameter = CimMethodParameter.Create(
                                parameterName,
                                referenceObject,
                                referenceType,
                                parameterFlags);
                        }
                        else
                        {
                            parameter = CimMethodParameter.Create(
                                parameterName,
                                parameterValue,
                                parameterFlags);
                        }
                    }
                }

                if (parameter != null)
                    collection.Add(parameter);
            }

            return collection;
        }
        #endregion

        #region const strings
        /// <summary>
        /// Operation target.
        /// </summary>
        private const string targetClass = @"{0}";

        /// <summary>
        /// Action.
        /// </summary>
        private const string actionTemplate = @"Invoke-CimMethod: {0}";
        #endregion
    }
}
