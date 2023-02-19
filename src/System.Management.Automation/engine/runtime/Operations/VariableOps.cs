// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;

// ReSharper disable UnusedMember.Local

namespace System.Management.Automation
{
    using Dbg = Diagnostics;
    using System.Collections.ObjectModel;

    internal static class VariableOps
    {
        internal static object SetVariableValue(VariablePath variablePath, object value, ExecutionContext executionContext, AttributeBaseAst[] attributeAsts)
        {
            SessionStateInternal sessionState = executionContext.EngineSessionState;
            CommandOrigin origin = sessionState.CurrentScope.ScopeOrigin;

            if (!variablePath.IsVariable)
            {
                sessionState.SetVariable(variablePath, value, true, origin);
                return value;
            }

            // Variable assignment is traced only if trace level 2 is specified.
            if (executionContext.PSDebugTraceLevel > 1)
            {
                executionContext.Debugger.TraceVariableSet(variablePath.UnqualifiedPath, value);
            }

            if (variablePath.IsUnscopedVariable)
            {
                variablePath = variablePath.CloneAndSetLocal();
            }

            SessionStateScope scope;
            PSVariable var = sessionState.GetVariableItem(variablePath, out scope, origin);

            if (var == null)
            {
                var attributes = attributeAsts == null
                                     ? new Collection<Attribute>()
                                     : GetAttributeCollection(attributeAsts);
                var = new PSVariable(variablePath.UnqualifiedPath, value, ScopedItemOptions.None, attributes);

                if (attributes.Count > 0)
                {
                    // When there are any attributes, it's possible the value was converted/transformed.
                    // Use 'GetValueRaw' here so the debugger check won't be triggered.
                    value = var.GetValueRaw();
                }

                // Marking untrusted values for assignments in 'ConstrainedLanguage' mode is done in
                // SessionStateScope.SetVariable.
                sessionState.SetVariable(variablePath, var, false, origin);

                if (executionContext._debuggingMode > 0)
                {
                    executionContext.Debugger.CheckVariableWrite(variablePath.UnqualifiedPath);
                }
            }
            else
            {
                if (attributeAsts != null)
                {
                    // Use bytewise operation directly instead of 'var.IsReadOnly || var.IsConstant' on
                    // a hot path (setting variable with type constraint) to get better performance.
                    if ((var.Options & (ScopedItemOptions.ReadOnly | ScopedItemOptions.Constant)) != ScopedItemOptions.None)
                    {
                        SessionStateUnauthorizedAccessException e =
                            new SessionStateUnauthorizedAccessException(
                                    var.Name,
                                    SessionStateCategory.Variable,
                                    "VariableNotWritable",
                                    SessionStateStrings.VariableNotWritable);
                        throw e;
                    }

                    var attributes = GetAttributeCollection(attributeAsts);
                    value = PSVariable.TransformValue(attributes, value);
                    if (!PSVariable.IsValidValue(attributes, value))
                    {
                        ValidationMetadataException e = new ValidationMetadataException(
                            "ValidateSetFailure",
                            null,
                            Metadata.InvalidValueFailure,
                            var.Name,
                            (value != null) ? value.ToString() : "$null");

                        throw e;
                    }

                    var.SetValueRaw(value, true);
                    // Don't update the PSVariable's attributes until we successfully set the value
                    var.Attributes.Clear();
                    var.AddParameterAttributesNoChecks(attributes);

                    if (executionContext._debuggingMode > 0)
                    {
                        executionContext.Debugger.CheckVariableWrite(variablePath.UnqualifiedPath);
                    }
                }
                else
                {
                    // The setter will handle checking for variable writes.
                    var.Value = value;
                }

                if (executionContext.LanguageMode == PSLanguageMode.ConstrainedLanguage)
                {
                    // Mark untrusted values for assignments to 'Global:' variables, and 'Script:' variables in
                    // a module scope, if it's necessary.
                    ExecutionContext.MarkObjectAsUntrustedForVariableAssignment(var, scope, sessionState);
                }
            }

            return value;
        }

        private static bool ThrowStrictModeUndefinedVariable(ExecutionContext executionContext, VariableExpressionAst varAst)
        {
            // In some limited cases, the compiler knows we don't want an error, like when we're backing up
            // $foreach and $switch, which might not be set.  In that case, the ast passed is null.
            if (varAst == null)
            {
                return false;
            }

            if (executionContext.IsStrictVersion(2))
            {
                return true;
            }

            if (executionContext.IsStrictVersion(1))
            {
                var parent = varAst.Parent;
                while (parent != null)
                {
                    if (parent is ExpandableStringExpressionAst)
                    {
                        return false;
                    }

                    parent = parent.Parent;
                }

                return true;
            }

            return false;
        }

        internal static object GetAutomaticVariableValue(int tupleIndex, ExecutionContext executionContext, VariableExpressionAst varAst)
        {
            Diagnostics.Assert(tupleIndex < SpecialVariables.AutomaticVariableTypes.Length, "caller to verify a valid tuple index is used");

            if (executionContext._debuggingMode > 0)
            {
                executionContext.Debugger.CheckVariableRead(SpecialVariables.AutomaticVariables[tupleIndex]);
            }

            object result = executionContext.EngineSessionState.GetAutomaticVariableValue((AutomaticVariable)tupleIndex);

            if (result == AutomationNull.Value)
            {
                if (ThrowStrictModeUndefinedVariable(executionContext, varAst))
                {
                    throw InterpreterError.NewInterpreterException(SpecialVariables.AutomaticVariables[tupleIndex], typeof(RuntimeException),
                        varAst.Extent, "VariableIsUndefined", ParserStrings.VariableIsUndefined, SpecialVariables.AutomaticVariables[tupleIndex]);
                }

                result = null;
            }

            return result;
        }

        internal static object GetVariableValue(VariablePath variablePath, ExecutionContext executionContext, VariableExpressionAst varAst)
        {
            if (!variablePath.IsVariable)
            {
                CmdletProviderContext contextOut;
                SessionStateScope scopeOut;
                SessionStateInternal ss = executionContext.EngineSessionState;
                return ss.GetVariableValueFromProvider(variablePath, out contextOut, out scopeOut, ss.CurrentScope.ScopeOrigin);
            }

            SessionStateInternal sessionState = executionContext.EngineSessionState;
            CommandOrigin origin = sessionState.CurrentScope.ScopeOrigin;

            SessionStateScope scope;
            PSVariable var = sessionState.GetVariableItem(variablePath, out scope, origin);

            if (var != null)
            {
                return var.Value;
            }

            if (sessionState.ExecutionContext._debuggingMode > 0)
            {
                sessionState.ExecutionContext.Debugger.CheckVariableRead(variablePath.UnqualifiedPath);
            }

            if (ThrowStrictModeUndefinedVariable(executionContext, varAst))
            {
                throw InterpreterError.NewInterpreterException(variablePath.UserPath, typeof(RuntimeException),
                    varAst.Extent, "VariableIsUndefined", ParserStrings.VariableIsUndefined, variablePath.UserPath);
            }

            return null;
        }

        internal static PSReference GetVariableAsRef(VariablePath variablePath, ExecutionContext executionContext, Type staticType)
        {
            Diagnostics.Assert(variablePath.IsVariable, "caller to verify varpath is a variable.");

            SessionStateInternal sessionState = executionContext.EngineSessionState;
            CommandOrigin origin = sessionState.CurrentScope.ScopeOrigin;

            SessionStateScope scope;
            PSVariable var = sessionState.GetVariableItem(variablePath, out scope, origin);

            if (var == null)
            {
                throw InterpreterError.NewInterpreterException(variablePath, typeof(RuntimeException), null,
                                                               "NonExistingVariableReference",
                                                               ParserStrings.NonExistingVariableReference);
            }

            object value = var.Value;
            if (staticType == null && value != null)
            {
                value = PSObject.Base(value);
                if (value != null)
                {
                    staticType = value.GetType();
                }
            }

            if (staticType == null)
            {
                var declaredType = var.Attributes.OfType<ArgumentTypeConverterAttribute>().FirstOrDefault();
                staticType = declaredType != null ? declaredType.TargetType : typeof(LanguagePrimitives.Null);
            }

            return PSReference.CreateInstance(var, staticType);
        }

        private static Collection<Attribute> GetAttributeCollection(AttributeBaseAst[] attributeAsts)
        {
            var result = new Collection<Attribute>();
            foreach (var attributeAst in attributeAsts)
            {
                result.Add(attributeAst.GetAttribute());
            }

            return result;
        }

        private static UsingResult GetUsingValueFromTuple(MutableTuple tuple, string usingExpressionKey, int index)
        {
            var boundParameters =
                tuple.GetAutomaticVariable(AutomaticVariable.PSBoundParameters) as PSBoundParametersDictionary;
            if (boundParameters != null)
            {
                var implicitUsingParameters = boundParameters.ImplicitUsingParameters;
                if (implicitUsingParameters != null)
                {
                    if (implicitUsingParameters.Contains(usingExpressionKey))
                    {
                        return new UsingResult { Value = implicitUsingParameters[usingExpressionKey] };
                    }
                    else if (implicitUsingParameters.Contains(index))
                    {
                        // Handle downlevel (V4) using variables by using index to look up using value.
                        return new UsingResult { Value = implicitUsingParameters[index] };
                    }
                }
            }

            return null;
        }

        private sealed class UsingResult
        {
            public object Value { get; set; }
        }

        internal static object GetUsingValue(MutableTuple tuple, string usingExpressionKey, int index, ExecutionContext context)
        {
            UsingResult result = GetUsingValueFromTuple(tuple, usingExpressionKey, index);
            if (result != null)
            {
                return result.Value;
            }

            var scope = context.EngineSessionState.CurrentScope;
            while (scope != null)
            {
                result = GetUsingValueFromTuple(scope.LocalsTuple, usingExpressionKey, index);
                if (result != null)
                {
                    return result.Value;
                }

                foreach (var dottedScope in scope.DottedScopes)
                {
                    result = GetUsingValueFromTuple(dottedScope, usingExpressionKey, index);
                    if (result != null)
                    {
                        return result.Value;
                    }
                }

                scope = scope.Parent;
            }

            // $PSBoundParameters is null or not the expected type (because someone may have assigned to it), so
            // we can't even guess if they were mis-using $using:foo
            throw InterpreterError.NewInterpreterException(null, typeof(RuntimeException),
                null, "UsingWithoutInvokeCommand", ParserStrings.UsingWithoutInvokeCommand);
        }
    }
}
