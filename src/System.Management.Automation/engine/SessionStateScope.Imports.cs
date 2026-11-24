// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation;

internal sealed partial class SessionStateScope
{
    internal PSVariable ImportVariable(PSVariable variable, bool force, SessionStateInternal sourceState, SessionStateInternal targetState)
    {
        if (!ScopedProperties.IsLegacySetting(variable.Name))
        {
            return NewVariable(variable, force, sourceState);
        }

        if (variable is not ScopedSettingVariable { Entry: { } entry }
            || !ReferenceEquals(variable.SessionState?.ExecutionContext, targetState.ExecutionContext))
        {
            var imported = (ScopedSettingVariable)NewVariable(variable, force, targetState);
            imported.ModuleImportSource = variable;
            return imported;
        }

        var existing = GetScopedSettingVariableForWrite(variable.Name);
        if (existing is not null)
        {
            SessionState.ThrowIfNotVisible(ScopeOrigin, existing);
            existing.ThrowIfNotWritable(force);
            if (ReferenceEquals(existing, variable))
            {
                return existing;
            }
        }

        if (ExecutionContext.HasEverUsedConstrainedLanguage)
        {
            CheckVariableChangeInConstrainedLanguage(variable);
        }

        // Module exports share the original variable and its metadata within a runspace.
        TryGetDottedScopedVariableTarget(variable.Name, out var frame);
        return BindScopedSettingVariable(variable.Name, entry, frame);
    }

    internal static bool IsImportedVariable(PSVariable candidate, PSVariable exported)
        => ReferenceEquals(candidate, exported)
            || (candidate is ScopedSettingVariable { ModuleImportSource: { } source } && ReferenceEquals(source, exported));

}
