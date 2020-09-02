// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Runtime.CompilerServices;

namespace System.Management.Automation
{
    /// <summary>
    /// The parameter binder for shell functions.
    /// </summary>
    internal class ScriptParameterBinder : ParameterBinderBase
    {
        #region ctor

        /// <summary>
        /// Constructs a ScriptParameterBinder with the specified context.
        /// </summary>
        /// <param name="script">
        /// The script block representing the code being run
        /// </param>
        /// <param name="invocationInfo">
        /// The invocation information about the code that is being run.
        /// </param>
        /// <param name="context">
        /// The context under which the shell function is executing.
        /// </param>
        /// <param name="command">
        /// The command instance that represents the script in a pipeline. May be null.
        /// </param>
        /// <param name="localScope">
        /// If binding in a new local scope, the scope to set variables in.  If dotting, the value is null.
        /// </param>
        internal ScriptParameterBinder(
            ScriptBlock script,
            InvocationInfo invocationInfo,
            ExecutionContext context,
            InternalCommand command,
            SessionStateScope localScope) : base(invocationInfo, context, command)
        {
            Diagnostics.Assert(script != null, "caller to verify script is not null.");

            this.Script = script;
            this.LocalScope = localScope;
        }

        private readonly CallSite<Func<CallSite, object, object>> _copyMutableValueSite =
            CallSite<Func<CallSite, object, object>>.Create(PSVariableAssignmentBinder.Get());

        internal object CopyMutableValues(object o)
        {
            // The variable assignment binder copies mutable values and returns other values as is.
            return _copyMutableValueSite.Target.Invoke(_copyMutableValueSite, o);
        }

        #endregion ctor

        #region internal members

        #region Parameter default values

        /// <summary>
        /// Gets the default value for the specified parameter.
        /// </summary>
        /// <param name="name">
        /// The name of the parameter to get the default value of.
        /// </param>
        /// <returns>
        /// The default value of the specified parameter.
        /// </returns>
        /// <exception cref="Exception">See SessionStateInternal.GetVariableValue.</exception>
        internal override object GetDefaultParameterValue(string name)
        {
            RuntimeDefinedParameter runtimeDefinedParameter;
            if (Script.RuntimeDefinedParameters.TryGetValue(name, out runtimeDefinedParameter))
            {
                return GetDefaultScriptParameterValue(runtimeDefinedParameter);
            }

            return null;
        }

        #endregion Parameter default values

        #region Parameter binding

        /// <summary>
        /// Binds the parameters to local variables in the function scope.
        /// </summary>
        /// <param name="name">
        ///     The name of the parameter to bind the value to.
        /// </param>
        /// <param name="value">
        ///     The value to bind to the parameter. It should be assumed by
        ///     derived classes that the proper type coercion has already taken
        ///     place and that any prerequisite metadata has been satisfied.
        /// </param>
        /// <param name="parameterMetadata"></param>
        internal override void BindParameter(string name, object value, CompiledCommandParameter parameterMetadata)
        {
            if (value == AutomationNull.Value || value == UnboundParameter.Value)
            {
                value = null;
            }

            Diagnostics.Assert(name != null, "The caller should verify that name is not null");

            var varPath = new VariablePath(name, VariablePathFlags.Variable);

            // If the parameter was allocated in the LocalsTuple, we can avoid creating a PSVariable,
            if (LocalScope != null
                && varPath.IsAnyLocal()
                && LocalScope.TrySetLocalParameterValue(varPath.UnqualifiedPath, CopyMutableValues(value)))
            {
                return;
            }

            // Otherwise we'll fall through and enter a new PSVariable in the current scope.  This
            // is what normally happens when dotting (though the above may succeed if a parameter name
            // was an automatic variable like $PSBoundParameters.

            // First we need to make a variable instance and apply
            // any attributes from the script.

            PSVariable variable = new PSVariable(varPath.UnqualifiedPath, value,
                                                 varPath.IsPrivate ? ScopedItemOptions.Private : ScopedItemOptions.None);
            Context.EngineSessionState.SetVariable(varPath, variable, false, CommandOrigin.Internal);
            RuntimeDefinedParameter runtimeDefinedParameter;
            if (Script.RuntimeDefinedParameters.TryGetValue(name, out runtimeDefinedParameter))
            {
                // The attributes have already been checked and conversions run, so it is wrong
                // to do so again.
                variable.AddParameterAttributesNoChecks(runtimeDefinedParameter.Attributes);
            }
        }

        /// <summary>
        /// Return the default value of a script parameter, evaluating the parse tree if necessary.
        /// </summary>
        internal object GetDefaultScriptParameterValue(RuntimeDefinedParameter parameter, IDictionary implicitUsingParameters = null)
        {
            object result = parameter.Value;

            var compiledDefault = result as Compiler.DefaultValueExpressionWrapper;
            if (compiledDefault != null)
            {
                result = compiledDefault.GetValue(Context, Script.SessionStateInternal, implicitUsingParameters);
            }

            return result;
        }

        #endregion Parameter binding

        #endregion internal members

        #region private members

        /// <summary>
        /// The script that is being bound to.
        /// </summary>
        internal ScriptBlock Script { get; }

        internal SessionStateScope LocalScope { get; set; }

        #endregion private members
    }
}
