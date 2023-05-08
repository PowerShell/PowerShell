// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;

namespace System.Management.Automation
{
    /// <summary>
    /// This is the interface between the NativeCommandProcessor and the
    /// parameter binders required to bind parameters to a native command.
    /// </summary>
    internal class NativeCommandParameterBinderController : ParameterBinderController
    {
        #region ctor

        /// <summary>
        /// Initializes the cmdlet parameter binder controller for
        /// the specified native command and engine context.
        /// </summary>
        /// <param name="command">
        /// The command that the parameters will be bound to.
        /// </param>
        internal NativeCommandParameterBinderController(NativeCommand command)
            : base(command.MyInvocation, command.Context, new NativeCommandParameterBinder(command))
        {
        }

        #endregion ctor

        /// <summary>
        /// Gets the command arguments in string form.
        /// </summary>
        internal string Arguments
        {
            get
            {
                return ((NativeCommandParameterBinder)DefaultParameterBinder).Arguments;
            }
        }

        /// <summary>
        /// Gets the value of the command arguments as an array of strings.
        /// </summary>
        internal string[] ArgumentList
        {
            get
            {
                return ((NativeCommandParameterBinder)DefaultParameterBinder).ArgumentList;
            }
        }

        /// <summary>
        /// Gets the value indicating what type of native argument binding to use.
        /// </summary>
        internal NativeArgumentPassingStyle ArgumentPassingStyle
        {
            get
            {
                return ((NativeCommandParameterBinder)DefaultParameterBinder).ArgumentPassingStyle;
            }
        }

        /// <summary>
        /// Passes the binding directly through to the parameter binder.
        /// It does no verification against metadata.
        /// </summary>
        /// <param name="argument">
        /// The name and value of the variable to bind.
        /// </param>
        /// <param name="flags">
        /// Ignored.
        /// </param>
        /// <returns>
        /// True if the parameter was successfully bound. Any error condition produces an exception.
        /// </returns>
        internal override bool BindParameter(
            CommandParameterInternal argument,
            ParameterBindingFlags flags)
        {
            Diagnostics.Assert(false, "Unreachable code");

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Binds the specified parameters to the native command.
        /// </summary>
        /// <param name="parameters">
        /// The parameters to bind.
        /// </param>
        /// <remarks>
        /// For any parameters that do not have a name, they are added to the command
        /// line arguments for the command
        /// </remarks>
        internal override Collection<CommandParameterInternal> BindParameters(Collection<CommandParameterInternal> parameters)
        {
            ((NativeCommandParameterBinder)DefaultParameterBinder).BindParameters(parameters);

            Diagnostics.Assert(s_emptyReturnCollection.Count == 0, "This list shouldn't be used for anything as it's shared.");

            return s_emptyReturnCollection;
        }

        private static readonly Collection<CommandParameterInternal> s_emptyReturnCollection = new Collection<CommandParameterInternal>();
    }
}
