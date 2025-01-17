// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation;
using System.Reflection;

using Microsoft.Management.Infrastructure;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Cim
{
    /// <summary>
    /// Adapter that deals with CimInstance objects.
    /// </summary>
    /// <remarks>
    /// Implementing the PropertyOnlyAdapter for the time being as CimInstanceTypeAdapter currently
    /// supports only properties. If method support is needed in future, this should derive from
    /// Adapter class.
    /// </remarks>
    public sealed class CimInstanceAdapter : PSPropertyAdapter
    {
        private static PSAdaptedProperty GetCimPropertyAdapter(CimProperty property, object baseObject, string propertyName)
        {
            PSAdaptedProperty propertyToAdd = new(propertyName, property);
            propertyToAdd.baseObject = baseObject;
            // propertyToAdd.adapter = this;
            return propertyToAdd;
        }

        private static PSAdaptedProperty GetCimPropertyAdapter(CimProperty property, object baseObject)
        {
            try
            {
                string propertyName = property.Name;
                return GetCimPropertyAdapter(property, baseObject, propertyName);
            }
            catch (CimException)
            {
                // ignore "Name" property access failures and move on.
                return null;
            }
        }

        private static PSAdaptedProperty GetPSComputerNameAdapter(CimInstance cimInstance)
        {
            PSAdaptedProperty psComputerNameProperty = new(RemotingConstants.ComputerNameNoteProperty, cimInstance);
            psComputerNameProperty.baseObject = cimInstance;
            // psComputerNameProperty.adapter = this;
            return psComputerNameProperty;
        }

        /// <summary>
        /// </summary>
        /// <param name="baseObject"></param>
        /// <returns></returns>
        public override System.Collections.ObjectModel.Collection<PSAdaptedProperty> GetProperties(object baseObject)
        {
            // baseObject should never be null
            if (baseObject is not CimInstance cimInstance)
            {
                string msg = string.Format(CultureInfo.InvariantCulture,
                    CimInstanceTypeAdapterResources.BaseObjectNotCimInstance,
                    "baseObject",
                    typeof(CimInstance).ToString());
                throw new PSInvalidOperationException(msg);
            }

            Collection<PSAdaptedProperty> result = new();

            if (cimInstance.CimInstanceProperties != null)
            {
                foreach (CimProperty property in cimInstance.CimInstanceProperties)
                {
                    PSAdaptedProperty propertyToAdd = GetCimPropertyAdapter(property, baseObject);
                    if (propertyToAdd != null)
                    {
                        result.Add(propertyToAdd);
                    }
                }
            }

            PSAdaptedProperty psComputerNameProperty = GetPSComputerNameAdapter(cimInstance);
            if (psComputerNameProperty != null)
            {
                result.Add(psComputerNameProperty);
            }

            return result;
        }

        /// <summary>
        /// </summary>
        /// <param name="baseObject"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public override PSAdaptedProperty GetProperty(object baseObject, string propertyName)
        {
            if (propertyName == null)
            {
                throw new PSArgumentNullException(nameof(propertyName));
            }

            // baseObject should never be null
            if (baseObject is not CimInstance cimInstance)
            {
                string msg = string.Format(CultureInfo.InvariantCulture,
                    CimInstanceTypeAdapterResources.BaseObjectNotCimInstance,
                    "baseObject",
                    typeof(CimInstance).ToString());
                throw new PSInvalidOperationException(msg);
            }

            CimProperty cimProperty = cimInstance.CimInstanceProperties[propertyName];
            if (cimProperty != null)
            {
                PSAdaptedProperty prop = GetCimPropertyAdapter(cimProperty, baseObject, propertyName);
                return prop;
            }

            if (propertyName.Equals(RemotingConstants.ComputerNameNoteProperty, StringComparison.OrdinalIgnoreCase))
            {
                PSAdaptedProperty prop = GetPSComputerNameAdapter(cimInstance);
                return prop;
            }

            return null;
        }

        /// <inheritdoc/>
        public override PSAdaptedProperty GetFirstPropertyOrDefault(object baseObject, MemberNamePredicate predicate)
        {
            if (predicate == null)
            {
                throw new PSArgumentNullException(nameof(predicate));
            }

            // baseObject should never be null
            if (baseObject is not CimInstance cimInstance)
            {
                string msg = string.Format(
                    CultureInfo.InvariantCulture,
                    CimInstanceTypeAdapterResources.BaseObjectNotCimInstance,
                    "baseObject",
                    typeof(CimInstance).ToString());
                throw new PSInvalidOperationException(msg);
            }

            if (predicate(RemotingConstants.ComputerNameNoteProperty))
            {
                PSAdaptedProperty prop = GetPSComputerNameAdapter(cimInstance);
                return prop;
            }

            foreach (CimProperty cimProperty in cimInstance.CimInstanceProperties)
            {
                if (cimProperty != null && predicate(cimProperty.Name))
                {
                    PSAdaptedProperty prop = GetCimPropertyAdapter(cimProperty, baseObject, cimProperty.Name);
                    return prop;
                }
            }

            return null;
        }

        internal static string CimTypeToTypeNameDisplayString(CimType cimType)
        {
            switch (cimType)
            {
                case CimType.DateTime:
                case CimType.Instance:
                case CimType.Reference:
                case CimType.DateTimeArray:
                case CimType.InstanceArray:
                case CimType.ReferenceArray:
                    return "CimInstance#" + cimType.ToString();

                default:
                    return ToStringCodeMethods.Type(
                        CimConverter.GetDotNetType(cimType));
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="adaptedProperty"></param>
        /// <returns></returns>
        public override string GetPropertyTypeName(PSAdaptedProperty adaptedProperty)
        {
            ArgumentNullException.ThrowIfNull(adaptedProperty);

            if (adaptedProperty.Tag is CimProperty cimProperty)
            {
                return CimTypeToTypeNameDisplayString(cimProperty.CimType);
            }

            if (adaptedProperty.Name.Equals(RemotingConstants.ComputerNameNoteProperty, StringComparison.OrdinalIgnoreCase))
            {
                return ToStringCodeMethods.Type(typeof(string));
            }

            throw new ArgumentNullException(nameof(adaptedProperty));
        }

        /// <summary>
        /// </summary>
        /// <param name="adaptedProperty"></param>
        /// <returns></returns>
        public override object GetPropertyValue(PSAdaptedProperty adaptedProperty)
        {
            ArgumentNullException.ThrowIfNull(adaptedProperty);

            if (adaptedProperty.Tag is CimProperty cimProperty)
            {
                return cimProperty.Value;
            }

            if (adaptedProperty.Name.Equals(RemotingConstants.ComputerNameNoteProperty, StringComparison.OrdinalIgnoreCase))
            {
                CimInstance cimInstance = (CimInstance)adaptedProperty.Tag;
                return cimInstance.GetCimSessionComputerName();
            }

            throw new ArgumentNullException(nameof(adaptedProperty));
        }

        private static void AddTypeNameHierarchy(IList<string> typeNamesWithNamespace, IList<string> typeNamesWithoutNamespace, string namespaceName, string className)
        {
            if (!string.IsNullOrEmpty(namespaceName))
            {
                string fullTypeName = string.Create(CultureInfo.InvariantCulture, $"Microsoft.Management.Infrastructure.CimInstance#{namespaceName}/{className}");
                typeNamesWithNamespace.Add(fullTypeName);
            }

            typeNamesWithoutNamespace.Add(string.Create(CultureInfo.InvariantCulture, $"Microsoft.Management.Infrastructure.CimInstance#{className}"));
        }

        private static List<CimClass> GetInheritanceChain(CimInstance cimInstance)
        {
            List<CimClass> inheritanceChain = new();
            CimClass cimClass = cimInstance.CimClass;
            Dbg.Assert(cimClass != null, "CimInstance should always have ClassDecl");
            while (cimClass != null)
            {
                inheritanceChain.Add(cimClass);
                try
                {
                    cimClass = cimClass.CimSuperClass;
                }
                catch (CimException)
                {
                    break;
                }
            }

            return inheritanceChain;
        }

        /// <summary>
        /// </summary>
        /// <param name="baseObject"></param>
        /// <returns></returns>
        public override Collection<string> GetTypeNameHierarchy(object baseObject)
        {
            if (!(baseObject is CimInstance cimInstance))
            {
                throw new ArgumentNullException(nameof(baseObject));
            }

            var typeNamesWithNamespace = new List<string>();
            var typeNamesWithoutNamespace = new List<string>();

            IList<CimClass> inheritanceChain = GetInheritanceChain(cimInstance);
            if ((inheritanceChain == null) || (inheritanceChain.Count == 0))
            {
                AddTypeNameHierarchy(
                    typeNamesWithNamespace,
                    typeNamesWithoutNamespace,
                    cimInstance.CimSystemProperties.Namespace,
                    cimInstance.CimSystemProperties.ClassName);
            }
            else
            {
                foreach (CimClass cimClass in inheritanceChain)
                {
                    AddTypeNameHierarchy(
                        typeNamesWithNamespace,
                        typeNamesWithoutNamespace,
                        cimClass.CimSystemProperties.Namespace,
                        cimClass.CimSystemProperties.ClassName);
                    cimClass.Dispose();
                }
            }

            var result = new List<string>();
            result.AddRange(typeNamesWithNamespace);
            result.AddRange(typeNamesWithoutNamespace);

            if (baseObject != null)
            {
                for (Type type = baseObject.GetType(); type != null; type = type.BaseType)
                {
                    result.Add(type.FullName);
                }
            }

            return new Collection<string>(result);
        }

        /// <summary>
        /// </summary>
        /// <param name="adaptedProperty"></param>
        /// <returns></returns>
        public override bool IsGettable(PSAdaptedProperty adaptedProperty)
        {
            /* I was explicitly asked to only use MI_FLAG_READONLY for now
            // based on DSP0004, version 2.6.0, section "5.5.3.41 Read" (page 85, lines 2881-2884)
            bool readQualifierValue = this.GetPropertyQualifierValue(adaptedProperty, "Read", defaultValue: true);
            return readQualifierValue;
            */
            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="adaptedProperty"></param>
        /// <returns></returns>
        public override bool IsSettable(PSAdaptedProperty adaptedProperty)
        {
            /* I was explicitly asked to only use MI_FLAG_READONLY for now
            // based on DSP0004, version 2.6.0, section "5.5.3.55 Write" (pages 89-90, lines 3056-3061)
            bool writeQualifierValue = this.GetPropertyQualifierValue(adaptedProperty, "Write", defaultValue: false);
            return writeQualifierValue;
            */

            if (adaptedProperty == null)
            {
                return false;
            }

            if (!(adaptedProperty.Tag is CimProperty cimProperty))
            {
                return false;
            }

            bool isReadOnly = ((cimProperty.Flags & CimFlags.ReadOnly) == CimFlags.ReadOnly);
            bool isSettable = !isReadOnly;
            return isSettable;
        }

        /// <summary>
        /// </summary>
        /// <param name="adaptedProperty"></param>
        /// <param name="value"></param>
        public override void SetPropertyValue(PSAdaptedProperty adaptedProperty, object value)
        {
            ArgumentNullException.ThrowIfNull(adaptedProperty);

            if (!IsSettable(adaptedProperty))
            {
                throw new SetValueException("ReadOnlyCIMProperty",
                        null,
                        CimInstanceTypeAdapterResources.ReadOnlyCIMProperty,
                        adaptedProperty.Name);
            }

            CimProperty cimProperty = adaptedProperty.Tag as CimProperty;
            object valueToSet = value;
            if (valueToSet != null)
            {
                // Convert only if value is not null
                Type paramType;
                switch (cimProperty.CimType)
                {
                    case CimType.DateTime:
                        paramType = typeof(object);
                        break;
                    case CimType.DateTimeArray:
                        paramType = typeof(object[]);
                        break;
                    default:
                        paramType = CimConverter.GetDotNetType(cimProperty.CimType);
                        Dbg.Assert(paramType != null, "'default' case should only be used for well-defined CimType->DotNetType conversions");
                        break;
                }

                valueToSet = Adapter.PropertySetAndMethodArgumentConvertTo(
                    value, paramType, CultureInfo.InvariantCulture);
            }

            cimProperty.Value = valueToSet;
            return;
        }
    }
}
