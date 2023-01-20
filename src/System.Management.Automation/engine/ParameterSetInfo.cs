// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

using Microsoft.PowerShell;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// The information about a parameter set and its parameters for a cmdlet.
    /// </summary>
    public class CommandParameterSetInfo
    {
        #region ctor

        /// <summary>
        /// Constructs the parameter set information using the specified parameter name,
        /// and type metadata.
        /// </summary>
        /// <param name="name">
        /// The formal name of the parameter.
        /// </param>
        /// <param name="isDefaultParameterSet">
        /// True if the parameter set is the default parameter set, or false otherwise.
        /// </param>
        /// <param name="parameterSetFlag">
        /// The bit that specifies the parameter set in the type metadata.
        /// </param>
        /// <param name="parameterMetadata">
        /// The type metadata about the cmdlet.
        /// </param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="name"/> is null or empty.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="parameterMetadata"/> is null.
        /// </exception>
        internal CommandParameterSetInfo(
            string name,
            bool isDefaultParameterSet,
            uint parameterSetFlag,
            MergedCommandParameterMetadata parameterMetadata)
        {
            IsDefault = true;
            Name = string.Empty;
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            if (parameterMetadata == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(parameterMetadata));
            }

            this.Name = name;
            this.IsDefault = isDefaultParameterSet;

            Initialize(parameterMetadata, parameterSetFlag);
        }
        #endregion ctor

        #region public members

        /// <summary>
        /// Gets the name of the parameter set.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets whether the parameter set is the default parameter set.
        /// </summary>
        public bool IsDefault { get; }

        /// <summary>
        /// Gets the parameter information for the parameters in this parameter set.
        /// </summary>
        public ReadOnlyCollection<CommandParameterInfo> Parameters { get; private set; }

        /// <summary>
        /// Gets the synopsis for the cmdlet as a string.
        /// </summary>
        public override string ToString()
        {
            Text.StringBuilder result = new Text.StringBuilder();

            GenerateParametersInDisplayOrder(
                parameter => AppendFormatCommandParameterInfo(parameter, result),
                (string str) =>
                {
                    if (result.Length > 0)
                    {
                        result.Append(' ');
                    }

                    result.Append('[');
                    result.Append(str);
                    result.Append(']');
                });

            return result.ToString();
        }

        /// <summary>
        /// GenerateParameters parameters in display order
        /// ie., Positional followed by
        ///      Named Mandatory (in alpha numeric) followed by
        ///      Named (in alpha numeric).
        ///
        /// Callers use <paramref name="parameterAction"/> and
        /// <paramref name="commonParameterAction"/> to handle
        /// syntax generation etc.
        /// </summary>
        /// <param name="parameterAction"></param>
        /// <param name="commonParameterAction"></param>
        /// <returns></returns>
        internal void GenerateParametersInDisplayOrder(
            Action<CommandParameterInfo> parameterAction,
            Action<string> commonParameterAction)
        {
            // First figure out the positions

            List<CommandParameterInfo> sortedPositionalParameters = new List<CommandParameterInfo>();
            List<CommandParameterInfo> namedMandatoryParameters = new List<CommandParameterInfo>();
            List<CommandParameterInfo> namedParameters = new List<CommandParameterInfo>();

            foreach (CommandParameterInfo parameter in Parameters)
            {
                if (parameter.Position == int.MinValue)
                {
                    // The parameter is a named parameter
                    if (parameter.IsMandatory)
                    {
                        namedMandatoryParameters.Add(parameter);
                    }
                    else
                    {
                        namedParameters.Add(parameter);
                    }
                }
                else
                {
                    // The parameter is positional so add it at the correct
                    // index (note we have to pad the list if the position is
                    // higher than the list count since we don't have any requirements
                    // that positional parameters start at zero and are consecutive.

                    if (parameter.Position >= sortedPositionalParameters.Count)
                    {
                        for (int fillerIndex = sortedPositionalParameters.Count;
                             fillerIndex <= parameter.Position;
                             ++fillerIndex)
                        {
                            sortedPositionalParameters.Add(null);
                        }
                    }

                    sortedPositionalParameters[parameter.Position] = parameter;
                }
            }

            foreach (CommandParameterInfo parameter in sortedPositionalParameters)
            {
                if (parameter == null)
                {
                    continue;
                }

                parameterAction(parameter);
            }

            // Now convert the named mandatory parameters into a string
            foreach (CommandParameterInfo parameter in namedMandatoryParameters)
            {
                if (parameter == null)
                {
                    continue;
                }

                parameterAction(parameter);
            }

            List<CommandParameterInfo> commonParameters = new List<CommandParameterInfo>();

            // Now convert the named parameters into a string
            foreach (CommandParameterInfo parameter in namedParameters)
            {
                if (parameter == null)
                {
                    continue;
                }

                // Hold off common parameters
                bool isCommon = Cmdlet.CommonParameters.Contains(parameter.Name, StringComparer.OrdinalIgnoreCase);
                if (!isCommon)
                {
                    parameterAction(parameter);
                }
                else
                {
                    commonParameters.Add(parameter);
                }
            }

            // If all common parameters are present, group them together
            if (commonParameters.Count == Cmdlet.CommonParameters.Count)
            {
                commonParameterAction(HelpDisplayStrings.CommonParameters);
            }
            // Else, convert to string as before
            else
            {
                foreach (CommandParameterInfo parameter in commonParameters)
                {
                    parameterAction(parameter);
                }
            }
        }

        #endregion public members

        #region private members

        private static void AppendFormatCommandParameterInfo(CommandParameterInfo parameter, Text.StringBuilder result)
        {
            if (result.Length > 0)
            {
                // Add a space between parameters
                result.Append(' ');
            }

            if (parameter.ParameterType == typeof(SwitchParameter))
            {
                result.Append(CultureInfo.InvariantCulture, parameter.IsMandatory ? $"-{parameter.Name}" : $"[-{parameter.Name}]");
            }
            else
            {
                string parameterTypeString = GetParameterTypeString(parameter.ParameterType, parameter.Attributes);

                if (parameter.IsMandatory)
                {
                    result.AppendFormat(CultureInfo.InvariantCulture,
                                        parameter.Position != int.MinValue ? $"[-{parameter.Name}] <{parameterTypeString}>" : $"-{parameter.Name} <{parameterTypeString}>");
                }
                else
                {
                    result.AppendFormat(CultureInfo.InvariantCulture,
                                        parameter.Position != int.MinValue ? $"[[-{parameter.Name}] <{parameterTypeString}>]" : $"[-{parameter.Name} <{parameterTypeString}>]");
                }
            }
        }

        internal static string GetParameterTypeString(Type type, IEnumerable<Attribute> attributes)
        {
            string parameterTypeString;
            PSTypeNameAttribute typeName;
            if (attributes != null && (typeName = attributes.OfType<PSTypeNameAttribute>().FirstOrDefault()) != null)
            {
                // If we have a PSTypeName specified on the class, we assume it has a more useful type than the actual
                // parameter type.  This is a reasonable assumption, the parameter binder does honor this attribute.
                //
                // This typename might be long, e.g.:
                //     Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Process
                //     System.Management.ManagementObject#root\cimv2\Win32_Process
                // To shorten this, we will drop the namespaces, both on the .Net side and the CIM/WMI side:
                //     CimInstance#Win32_Process
                // If our regex doesn't match, we'll just use the full name.
                var match = Regex.Match(typeName.PSTypeName, "(.*\\.)?(?<NetTypeName>.*)#(.*[/\\\\])?(?<CimClassName>.*)");
                if (match.Success)
                {
                    parameterTypeString = match.Groups["NetTypeName"].Value + "#" + match.Groups["CimClassName"].Value;
                }
                else
                {
                    parameterTypeString = typeName.PSTypeName;

                    // Drop the namespace from the typename, if any.
                    var lastDotIndex = parameterTypeString.LastIndexOf('.');
                    if (lastDotIndex != -1 && lastDotIndex + 1 < parameterTypeString.Length)
                    {
                        parameterTypeString = parameterTypeString.Substring(lastDotIndex + 1);
                    }
                }

                // If the type is really an array, but the typename didn't include [], then add it.
                if (type.IsArray && !parameterTypeString.Contains("[]", StringComparison.Ordinal))
                {
                    var t = type;
                    while (t.IsArray)
                    {
                        parameterTypeString += "[]";
                        t = t.GetElementType();
                    }
                }
            }
            else
            {
                Type parameterType = Nullable.GetUnderlyingType(type) ?? type;
                parameterTypeString = ToStringCodeMethods.Type(parameterType, true);
            }

            return parameterTypeString;
        }

        private void Initialize(MergedCommandParameterMetadata parameterMetadata, uint parameterSetFlag)
        {
            Diagnostics.Assert(
                parameterMetadata != null,
                "The parameterMetadata should never be null");

            Collection<CommandParameterInfo> processedParameters =
                new Collection<CommandParameterInfo>();

            // Get the parameters in the parameter set
            Collection<MergedCompiledCommandParameter> compiledParameters =
                parameterMetadata.GetParametersInParameterSet(parameterSetFlag);

            foreach (MergedCompiledCommandParameter parameter in compiledParameters)
            {
                if (parameter != null)
                {
                    processedParameters.Add(
                        new CommandParameterInfo(parameter.Parameter, parameterSetFlag));
                }
            }

            Parameters = new ReadOnlyCollection<CommandParameterInfo>(processedParameters);
        }

        #endregion private members
    }
}
