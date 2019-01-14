// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Management.Automation.Language;
using Microsoft.Management.Infrastructure;

namespace System.Management.Automation.Runspaces
{
    using System;
    using System.Collections.ObjectModel;
    using Debug = System.Management.Automation.Diagnostics;

    /// <summary>
    /// Define a parameter for <see cref="Command"/>
    /// </summary>
    public sealed class CommandParameter
    {
        #region Public constructors

        /// <summary>
        /// Create a named parameter with a null value.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <exception cref="ArgumentNullException">
        /// name is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Name length is zero after trimming whitespace.
        /// </exception>
        public CommandParameter(string name)
            : this(name, null)
        {
            if (name == null)
            {
                throw PSTraceSource.NewArgumentNullException("name");
            }
        }

        /// <summary>
        /// Create a named parameter.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="value">Parameter value.</param>
        /// <exception cref="ArgumentException">
        /// Name is non null and name length is zero after trimming whitespace.
        /// </exception>
        public CommandParameter(string name, object value)
        {
            if (name != null)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw PSTraceSource.NewArgumentException("name");
                }

                Name = name;
            }
            else
            {
                Name = null;
            }

            Value = value;
        }

        #endregion Public constructors

        #region Public properties

        /// <summary>
        /// Gets the parameter name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the value of the parameter.
        /// </summary>
        public object Value { get; }

        #endregion Public properties

        #region Private Fields

        #endregion Private Fields

        #region Conversion from and to CommandParameterInternal

        internal static CommandParameter FromCommandParameterInternal(CommandParameterInternal internalParameter)
        {
            if (internalParameter == null)
            {
                throw PSTraceSource.NewArgumentNullException("internalParameter");
            }

            // we want the name to preserve 1) dashes, 2) colons, 3) followed-by-space information
            string name = null;
            if (internalParameter.ParameterNameSpecified)
            {
                name = internalParameter.ParameterText;
                if (internalParameter.SpaceAfterParameter)
                {
                    name = name + " ";
                }

                Diagnostics.Assert(name != null, "'name' variable should be initialized at this point");
                Diagnostics.Assert(name[0].IsDash(), "first character in parameter name must be a dash");
                Diagnostics.Assert(name.Trim().Length != 1, "Parameter name has to have some non-whitespace characters in it");
            }

            if (internalParameter.ParameterAndArgumentSpecified)
            {
                return new CommandParameter(name, internalParameter.ArgumentValue);
            }

            if (name != null) // either a switch parameter or first part of parameter+argument
            {
                return new CommandParameter(name);
            }
            // either a positional argument or second part of parameter+argument
            return new CommandParameter(null, internalParameter.ArgumentValue);
        }

        internal static CommandParameterInternal ToCommandParameterInternal(CommandParameter publicParameter, bool forNativeCommand)
        {
            if (publicParameter == null)
            {
                throw PSTraceSource.NewArgumentNullException("publicParameter");
            }

            string name = publicParameter.Name;
            object value = publicParameter.Value;

            Debug.Assert((name == null) || (name.Trim().Length != 0), "Parameter name has to null or have some non-whitespace characters in it");

            if (name == null)
            {
                return CommandParameterInternal.CreateArgument(value);
            }

            string parameterText;
            if (!name[0].IsDash())
            {
                parameterText = forNativeCommand ? name : "-" + name;
                return CommandParameterInternal.CreateParameterWithArgument(
                    /*parameterAst*/null, name, parameterText,
                    /*argumentAst*/null, value,
                    true);
            }

            // if first character of name is '-', then we try to fake the original token
            // reconstructing dashes, colons and followed-by-space information

            // find the last non-whitespace character
            bool spaceAfterParameter = false;
            int endPosition = name.Length;
            while ((endPosition > 0) && char.IsWhiteSpace(name[endPosition - 1]))
            {
                spaceAfterParameter = true;
                endPosition--;
            }

            Debug.Assert(endPosition > 0, "parameter name should have some non-whitespace characters in it");

            // now make sure that parameterText doesn't have whitespace at the end,
            parameterText = name.Substring(0, endPosition);

            // parameterName should contain only the actual name of the parameter (no whitespace, colons, dashes)
            bool hasColon = (name[endPosition - 1] == ':');
            var parameterName = parameterText.Substring(1, parameterText.Length - (hasColon ? 2 : 1));

            // At this point we have rebuilt the token.  There are 3 strings that might be different:
            //           name = nameToken.Script = "-foo: " <- needed to fake FollowedBySpace=true (i.e. for "testecho.exe -a:b -c: d")
            // tokenString = nameToken.TokenText = "-foo:" <- needed to preserve full token text (i.e. for write-output)
            //                    nameToken.Data =  "foo" <- needed to preserve name of parameter so parameter binding works
            // Now we just need to use the token to build appropriate CommandParameterInternal object

            // is this a name+value pair, or is it just a name (of a parameter)?
            if (!hasColon && value == null)
            {
                // just a name
                return CommandParameterInternal.CreateParameter(parameterName, parameterText);
            }

            // name+value pair
            return CommandParameterInternal.CreateParameterWithArgument(
                /*parameterAst*/null, parameterName, parameterText,
                /*argumentAst*/null, value,
                spaceAfterParameter);
        }

        #endregion

        #region Serialization / deserialization for remoting

        /// <summary>
        /// Creates a CommandParameter object from a PSObject property bag.
        /// PSObject has to be in the format returned by ToPSObjectForRemoting method.
        /// </summary>
        /// <param name="parameterAsPSObject">PSObject to rehydrate.</param>
        /// <returns>
        /// CommandParameter rehydrated from a PSObject property bag
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the PSObject is null.
        /// </exception>
        /// <exception cref="System.Management.Automation.Remoting.PSRemotingDataStructureException">
        /// Thrown when the PSObject is not in the expected format
        /// </exception>
        internal static CommandParameter FromPSObjectForRemoting(PSObject parameterAsPSObject)
        {
            if (parameterAsPSObject == null)
            {
                throw PSTraceSource.NewArgumentNullException("parameterAsPSObject");
            }

            string name = RemotingDecoder.GetPropertyValue<string>(parameterAsPSObject, RemoteDataNameStrings.ParameterName);
            object value = RemotingDecoder.GetPropertyValue<object>(parameterAsPSObject, RemoteDataNameStrings.ParameterValue);
            return new CommandParameter(name, value);
        }

        /// <summary>
        /// Returns this object as a PSObject property bag
        /// that can be used in a remoting protocol data object.
        /// </summary>
        /// <returns>This object as a PSObject property bag.</returns>
        internal PSObject ToPSObjectForRemoting()
        {
            PSObject parameterAsPSObject = RemotingEncoder.CreateEmptyPSObject();
            parameterAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.ParameterName, this.Name));
            parameterAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.ParameterValue, this.Value));
            return parameterAsPSObject;
        }

        #endregion

        #region Win Blue Extensions

#if !CORECLR // PSMI Not Supported On CSS
        internal CimInstance ToCimInstance()
        {
            CimInstance c = InternalMISerializer.CreateCimInstance("PS_Parameter");
            CimProperty nameProperty = InternalMISerializer.CreateCimProperty("Name", this.Name,
                                                                                Microsoft.Management.Infrastructure.CimType.String);
            c.CimInstanceProperties.Add(nameProperty);
            Microsoft.Management.Infrastructure.CimType cimType = CimConverter.GetCimType(this.Value.GetType());
            CimProperty valueProperty;
            if (cimType == Microsoft.Management.Infrastructure.CimType.Unknown)
            {
                valueProperty = InternalMISerializer.CreateCimProperty("Value", (object)PSMISerializer.Serialize(this.Value),
                                                                                Microsoft.Management.Infrastructure.CimType.Instance);
            }
            else
            {
                valueProperty = InternalMISerializer.CreateCimProperty("Value", this.Value, cimType);
            }

            c.CimInstanceProperties.Add(valueProperty);
            return c;
        }
#endif

        #endregion Win Blue Extensions
    }

    /// <summary>
    /// Defines a collection of parameters.
    /// </summary>
    public sealed class CommandParameterCollection : Collection<CommandParameter>
    {
        // TODO: this class needs a mechanism to lock further changes

        /// <summary>
        /// Create a new empty instance of this collection type.
        /// </summary>
        public CommandParameterCollection()
        {
        }

        /// <summary>
        /// Add a parameter with given name and default null value.
        /// </summary>
        /// <param name="name">Name of the parameter.</param>
        /// <exception cref="ArgumentNullException">
        /// name is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Name length is zero after trimming whitespace.
        /// </exception>
        public void Add(string name)
        {
            Add(new CommandParameter(name));
        }

        /// <summary>
        /// Add a parameter with given name and value.
        /// </summary>
        /// <param name="name">Name of the parameter.</param>
        /// <param name="value">Value of the parameter.</param>
        /// <exception cref="ArgumentNullException">
        /// Both name and value are null. One of these must be non-null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Name is non null and name length is zero after trimming whitespace.
        /// </exception>
        public void Add(string name, object value)
        {
            Add(new CommandParameter(name, value));
        }
    }
}

