// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.PowerShell.Cmdletization
{
    /// <summary>
    /// Describes how to handle the method parameter.
    /// </summary>
    [Flags]
    public enum MethodParameterBindings
    {
        /// <summary>
        /// Bind value of a method parameter based on arguments of a cmdlet parameter.
        /// </summary>
        In = 1,

        /// <summary>
        /// Method invocation is expected to set the value of the method parameter.  Cmdlet should emit the value of method parameter to the downstream pipe.
        /// </summary>
        Out = 2,

        /// <summary>
        /// Method invocation is expected to set the value of the method parameter.  Cmdlet should emit a non-terminating error when the value evaluates to $true.
        /// </summary>
        Error = 4,
    }

    /// <summary>
    /// Parameter of a method in an object model wrapped by <see cref="CmdletAdapter&lt;TObjectInstance&gt;"/>
    /// </summary>
    public sealed class MethodParameter
    {
        /// <summary>
        /// Name of the method parameter.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type of the parameter (as seen in the PowerShell layer on the client)
        /// </summary>
        public Type ParameterType { get; set; }

        /// <summary>
        /// Contents of the ETS type attribute in the CDXML file (or <see langword="null"/> if that attribute was not specified).
        /// The expectation is that the CmdletAdapter will stamp this value onto PSTypeNames of emitted objects.
        /// </summary>
        public string ParameterTypeName { get; set; }

        /// <summary>
        /// Bindings of the method parameter (in/out/error)
        /// </summary>
        public MethodParameterBindings Bindings { get; set; }

        /// <summary>
        /// Value of the argument of the method parameter.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Whether the value is 1) an explicit default (*) or 2) has been bound from cmdlet parameter
        /// (*) explicit default = whatever was in DefaultValue attribute in Cmdletization XML.
        /// </summary>
        public bool IsValuePresent { get; set; }
        // TODO/FIXME: this should be renamed to ValueExplicitlySpecified or something like this
    }
}
