/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Diagnostics;
using System.Management.Automation.Language;

namespace System.Management.Automation
{
    /// <summary>
    /// Represents a parameter to the Command.
    /// </summary>
    [DebuggerDisplay("{ParameterName}")]
    internal sealed class CommandParameterInternal
    {
        private class Parameter
        {
            internal IScriptExtent extent;
            internal string parameterName;
            internal string parameterText;
        }
        private class Argument
        {
            internal IScriptExtent extent;
            internal object value;
            internal bool splatted;
            internal bool arrayIsSingleArgumentForNativeCommand;
        }

        private Parameter _parameter;
        private Argument _argument;
        private bool _spaceAfterParameter;

        internal bool SpaceAfterParameter { get { return _spaceAfterParameter; } }
        internal bool ParameterNameSpecified { get { return _parameter != null; } }
        internal bool ArgumentSpecified { get { return _argument != null; } }
        internal bool ParameterAndArgumentSpecified { get { return ParameterNameSpecified && ArgumentSpecified; } }

        /// <summary>
        /// Gets and sets the string that represents parameter name, which does not include the '-' (dash).
        /// </summary>
        internal string ParameterName
        {
            get
            {
                Diagnostics.Assert(ParameterNameSpecified, "Caller must verify parameter name was specified");
                return _parameter.parameterName;
            }
            set
            {
                Diagnostics.Assert(ParameterNameSpecified, "Caller must verify parameter name was specified");
                _parameter.parameterName = value;
            }
        }

        /// <summary>
        /// The text of the parameter, which typically includes the leading '-' (dash) and, if specified, the trailing ':'.
        /// </summary>
        internal string ParameterText
        {
            get
            {
                Diagnostics.Assert(ParameterNameSpecified, "Caller must verify parameter name was specified");
                return _parameter.parameterText;
            }
        }

        /// <summary>
        /// The extent of the parameter, if one was specified.
        /// </summary>
        internal IScriptExtent ParameterExtent
        {
            get { return _parameter != null ? _parameter.extent : PositionUtilities.EmptyExtent; }
        }

        /// <summary>
        /// The extent of the optional argument, if one was specified.
        /// </summary>
        internal IScriptExtent ArgumentExtent
        {
            get { return _argument != null ? _argument.extent : PositionUtilities.EmptyExtent; }
        }

        /// <summary>
        /// The value of the optional argument, if one was specified, otherwise UnboundParameter.Value.
        /// </summary>
        internal object ArgumentValue
        {
            get { return _argument != null ? _argument.value : UnboundParameter.Value; }
        }

        /// <summary>
        /// If an argument was specified and is to be splatted, returns true, otherwise false.
        /// </summary>
        internal bool ArgumentSplatted
        {
            get { return _argument != null ? _argument.splatted : false; }
        }

        /// <summary>
        /// If an argument was specified and it was an array literal with no spaces around the
        /// commas, the value should be passed a single argument with it's commas if the command is
        /// a native command.
        /// </summary>
        internal bool ArrayIsSingleArgumentForNativeCommand
        {
            get { return _argument != null ? _argument.arrayIsSingleArgumentForNativeCommand : false; }
        }

        /// <summary>
        /// Set the argument value and extent.
        /// </summary>
        internal void SetArgumentValue(IScriptExtent extent, object value)
        {
            Diagnostics.Assert(extent != null, "Caller to verify extent argument");

            if (_argument == null)
            {
                _argument = new Argument();
            }
            _argument.value = value;
            _argument.extent = extent;
        }

        /// <summary>
        /// The extent to use when reporting generic errors.  The argument extent is used, if it is not empty, otherwise
        /// the parameter extent is used.  Some errors may prefer the parameter extent and should not use this method.
        /// </summary>
        internal IScriptExtent ErrorExtent
        {
            get
            {
                return _argument != null && _argument.extent != PositionUtilities.EmptyExtent
                           ? _argument.extent
                           : _parameter != null ? _parameter.extent : PositionUtilities.EmptyExtent;
            }
        }

        #region ctor

        /// <summary>
        /// Create a parameter when no argument has been specified.
        /// </summary>
        /// <param name="extent">The extent in script of the parameter.</param>
        /// <param name="parameterName">The parameter name (with no leading dash).</param>
        /// <param name="parameterText">The text of the parameter, as it did, or would, appear in script.</param>
        internal static CommandParameterInternal CreateParameter(
            IScriptExtent extent,
            string parameterName,
            string parameterText)
        {
            Diagnostics.Assert(extent != null, "Caller to verify extent argument");
            return new CommandParameterInternal
            {
                _parameter =
                           new Parameter { extent = extent, parameterName = parameterName, parameterText = parameterText }
            };
        }

        /// <summary>
        /// Create a positional argument to a command.
        /// </summary>
        /// <param name="extent">The extent of the argument value in the script.</param>
        /// <param name="value">The argument value.</param>
        /// <param name="splatted">True if the argument value is to be splatted, false otherwise.</param>
        /// <param name="arrayIsSingleArgumentForNativeCommand">If the command is native, pass the string with commas instead of multiple arguments</param>
        internal static CommandParameterInternal CreateArgument(
            IScriptExtent extent,
            object value,
            bool splatted = false,
            bool arrayIsSingleArgumentForNativeCommand = false)
        {
            Diagnostics.Assert(extent != null, "Caller to verify extent argument");
            return new CommandParameterInternal
            {
                _argument = new Argument
                {
                    extent = extent,
                    value = value,
                    splatted = splatted,
                    arrayIsSingleArgumentForNativeCommand = arrayIsSingleArgumentForNativeCommand
                }
            };
        }

        /// <summary>
        /// Create an named argument, where the parameter name is known.  This can happen when:
        ///     * The user uses the ':' syntax, as in
        ///         foo -bar:val
        ///     * Splatting, as in
        ///         $x = @{ bar = val } ; foo @x
        ///     * Via an API - when converting a CommandParameter to CommandParameterInternal.
        ///     * In the parameter binder when it resolves a positional argument
        ///     * Other random places that manually construct command processors and know their arguments.
        /// </summary>
        /// <param name="parameterExtent">The extent in script of the parameter.</param>
        /// <param name="parameterName">The parameter name (with no leading dash).</param>
        /// <param name="parameterText">The text of the parameter, as it did, or would, appear in script.</param>
        /// <param name="argumentExtent">The extent of the argument value in the script.</param>
        /// <param name="value">The argument value.</param>
        /// <param name="spaceAfterParameter">Used in native commands to correctly handle -foo:bar vs. -foo: bar</param>
        /// <param name="arrayIsSingleArgumentForNativeCommand">If the command is native, pass the string with commas instead of multiple arguments</param>
        internal static CommandParameterInternal CreateParameterWithArgument(
            IScriptExtent parameterExtent,
            string parameterName,
            string parameterText,
            IScriptExtent argumentExtent,
            object value,
            bool spaceAfterParameter,
            bool arrayIsSingleArgumentForNativeCommand = false)
        {
            Diagnostics.Assert(parameterExtent != null, "Caller to verify parameterExtent argument");
            Diagnostics.Assert(argumentExtent != null, "Caller to verify argumentExtent argument");
            return new CommandParameterInternal
            {
                _parameter = new Parameter { extent = parameterExtent, parameterName = parameterName, parameterText = parameterText },
                _argument = new Argument { extent = argumentExtent, value = value, arrayIsSingleArgumentForNativeCommand = arrayIsSingleArgumentForNativeCommand },
                _spaceAfterParameter = spaceAfterParameter
            };
        }

        #endregion  ctor

        internal bool IsDashQuestion()
        {
            return ParameterNameSpecified && (ParameterName.Equals("?", StringComparison.OrdinalIgnoreCase));
        }
    }
}
