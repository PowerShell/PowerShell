// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    /// <summary>
    /// Containing all necessary information originated from
    /// the parameters of <see cref="InvokeCimMethodCommand"/>
    /// </summary>
    internal class CimSetCimInstanceContext : XOperationContextBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CimSetCimInstanceContext"/> class.
        /// </summary>
        /// <param name="theNamespace"></param>
        /// <param name="theCollection"></param>
        /// <param name="theProxy"></param>
        internal CimSetCimInstanceContext(string theNamespace,
            IDictionary theProperty,
            CimSessionProxy theProxy,
            string theParameterSetName,
            bool passThru)
        {
            this.proxy = theProxy;
            this.Property = theProperty;
            this.nameSpace = theNamespace;
            this.ParameterSetName = theParameterSetName;
            this.PassThru = passThru;
        }

        /// <summary>
        /// <para>property value</para>
        /// </summary>
        internal IDictionary Property { get; }

        /// <summary>
        /// <para>parameter set name</para>
        /// </summary>
        internal string ParameterSetName { get; }

        /// <summary>
        /// <para>PassThru value</para>
        /// </summary>
        internal bool PassThru { get; }
    }

    /// <summary>
    /// <para>
    /// Implements operations of set-ciminstance cmdlet.
    /// </para>
    /// </summary>
    internal sealed class CimSetCimInstance : CimGetInstance
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CimSetCimInstance"/> class.
        /// </summary>
        public CimSetCimInstance()
            : base()
        {
        }

        /// <summary>
        /// <para>
        /// Base on parametersetName to set ciminstances
        /// </para>
        /// </summary>
        /// <param name="cmdlet"><see cref="SetCimInstanceCommand"/> object.</param>
        public void SetCimInstance(SetCimInstanceCommand cmdlet)
        {
            IEnumerable<string> computerNames = ConstValue.GetComputerNames(
                GetComputerName(cmdlet));
            List<CimSessionProxy> proxies = new();
            switch (cmdlet.ParameterSetName)
            {
                case CimBaseCommand.CimInstanceComputerSet:
                    foreach (string computerName in computerNames)
                    {
                        // create CimSessionProxySetCimInstance object internally
                        proxies.Add(CreateSessionProxy(computerName, cmdlet.CimInstance, cmdlet, cmdlet.PassThru));
                    }

                    break;
                case CimBaseCommand.CimInstanceSessionSet:
                    foreach (CimSession session in GetCimSession(cmdlet))
                    {
                        // create CimSessionProxySetCimInstance object internally
                        proxies.Add(CreateSessionProxy(session, cmdlet, cmdlet.PassThru));
                    }

                    break;
                default:
                    break;
            }

            switch (cmdlet.ParameterSetName)
            {
                case CimBaseCommand.CimInstanceComputerSet:
                case CimBaseCommand.CimInstanceSessionSet:
                    string nameSpace = ConstValue.GetNamespace(GetCimInstanceParameter(cmdlet).CimSystemProperties.Namespace);
                    string target = cmdlet.CimInstance.ToString();
                    foreach (CimSessionProxy proxy in proxies)
                    {
                        if (!cmdlet.ShouldProcess(target, action))
                        {
                            return;
                        }

                        Exception exception = null;
                        CimInstance instance = cmdlet.CimInstance;
                        // For CimInstance parameter sets, Property is an optional parameter
                        if (cmdlet.Property != null)
                        {
                            if (!SetProperty(cmdlet.Property, ref instance, ref exception))
                            {
                                cmdlet.ThrowTerminatingError(exception, action);
                                return;
                            }
                        }

                        proxy.ModifyInstanceAsync(nameSpace, instance);
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
        /// <para>
        /// Set <see cref="CimInstance"/> with properties specified in cmdlet
        /// </para>
        /// </summary>
        /// <param name="cimInstance"></param>
        public void SetCimInstance(CimInstance cimInstance, CimSetCimInstanceContext context, CmdletOperationBase cmdlet)
        {
            DebugHelper.WriteLog("CimSetCimInstance::SetCimInstance", 4);

            if (!cmdlet.ShouldProcess(cimInstance.ToString(), action))
            {
                return;
            }

            Exception exception = null;
            if (!SetProperty(context.Property, ref cimInstance, ref exception))
            {
                cmdlet.ThrowTerminatingError(exception, action);
                return;
            }

            CimSessionProxy proxy = CreateCimSessionProxy(context.Proxy, context.PassThru);
            proxy.ModifyInstanceAsync(cimInstance.CimSystemProperties.Namespace, cimInstance);
        }

        #region private members

        /// <summary>
        /// <para>
        /// Set the properties value to be modified to the given
        /// <see cref="CimInstance"/>
        /// </para>
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="cimInstance"></param>
        /// <param name="terminationMessage"></param>
        /// <returns></returns>
        private bool SetProperty(IDictionary properties, ref CimInstance cimInstance, ref Exception exception)
        {
            DebugHelper.WriteLogEx();
            if (properties.Count == 0)
            {
                // simply ignore if empty properties was provided
                return true;
            }

            IDictionaryEnumerator enumerator = properties.GetEnumerator();
            while (enumerator.MoveNext())
            {
                object value = GetBaseObject(enumerator.Value);
                string key = enumerator.Key.ToString();
                DebugHelper.WriteLog("Input property name '{0}' with value '{1}'", 1, key, value);

                try
                {
                    CimProperty property = cimInstance.CimInstanceProperties[key];
                    // modify existing property value if found
                    if (property != null)
                    {
                        if ((property.Flags & CimFlags.ReadOnly) == CimFlags.ReadOnly)
                        {
                            // can not modify ReadOnly property
                            exception = new CimException(string.Format(CultureInfo.CurrentUICulture,
                                CimCmdletStrings.CouldNotModifyReadonlyProperty, key, cimInstance));
                            return false;
                        }
                        // allow modify the key property value as long as it is not readonly,
                        // then the modified ciminstance is stand for a different CimInstance
                        DebugHelper.WriteLog("Set property name '{0}' has old value '{1}'", 4, key, property.Value);
                        property.Value = value;
                    }
                    else // For dynamic instance, it is valid to add a new property
                    {
                        CimProperty newProperty;
                        if (value == null)
                        {
                            newProperty = CimProperty.Create(
                                key,
                                value,
                                CimType.String,
                                CimFlags.Property);
                        }
                        else
                        {
                            CimType referenceType = CimType.Unknown;
                            object referenceObject = GetReferenceOrReferenceArrayObject(value, ref referenceType);
                            if (referenceObject != null)
                            {
                                newProperty = CimProperty.Create(
                                    key,
                                    referenceObject,
                                    referenceType,
                                    CimFlags.Property);
                            }
                            else
                            {
                                newProperty = CimProperty.Create(
                                    key,
                                    value,
                                    CimFlags.Property);
                            }
                        }

                        try
                        {
                            cimInstance.CimInstanceProperties.Add(newProperty);
                        }
                        catch (CimException e)
                        {
                            if (e.NativeErrorCode == NativeErrorCode.Failed)
                            {
                                string errorMessage = string.Format(CultureInfo.CurrentUICulture,
                                    CimCmdletStrings.UnableToAddPropertyToInstance,
                                    newProperty.Name,
                                    cimInstance);
                                exception = new CimException(errorMessage, e);
                            }
                            else
                            {
                                exception = e;
                            }

                            return false;
                        }

                        DebugHelper.WriteLog("Add non-key property name '{0}' with value '{1}'.", 3, key, value);
                    }
                }
                catch (Exception e)
                {
                    DebugHelper.WriteLog("Exception {0}", 4, e);
                    exception = e;
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region const strings
        /// <summary>
        /// Action.
        /// </summary>
        private const string action = @"Set-CimInstance";
        #endregion
    }
}
