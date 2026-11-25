// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation;

internal sealed partial class SessionStateScope
{
    /// <summary>
    /// Imports a module export, sharing its scoped setting entry within the same runspace.
    /// Imports from another runspace receive an independent entry with source provenance.
    /// </summary>
    internal PSVariable ImportVariable(PSVariable variable, bool force, SessionStateInternal sourceState, SessionStateInternal targetState)
    {
        if (!ScopedProperties.TryGetConfigurationSetting(variable.Name, out var setting))
        {
            return NewVariable(variable, force, sourceState);
        }

        if (variable is not ScopedSettingVariable { Entry: { } entry }
            || !ReferenceEquals(entry.Setting, setting)
            || !ReferenceEquals(variable.SessionState?.ExecutionContext, targetState.ExecutionContext))
        {
            PSVariable importSource = variable;
            if (variable is ScopedSettingVariable { Entry: IScopedMutableSettingEntry mutableEntry })
            {
                importSource = new PSVariable(variable.Name, mutableEntry.RawValue, variable.Options, variable.Attributes)
                {
                    Description = variable.Description,
                };
                importSource.CopyMetadataFrom(variable, initializing: true);
            }

            var imported = (ScopedSettingVariable)NewVariable(importSource, force, targetState);
            imported.ModuleImportSource = variable;
            return imported;
        }

        var existing = GetScopedSettingVariableForWrite(setting, out var frame);
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
        return BindScopedSettingVariable(entry, frame);
    }

    /// <summary>
    /// Identifies an imported binding for module removal, including imports that copied the exported variable.
    /// </summary>
    internal static bool IsImportedVariable(PSVariable candidate, PSVariable exported)
        => ReferenceEquals(candidate, exported)
            || (candidate is ScopedSettingVariable { ModuleImportSource: { } source } && ReferenceEquals(source, exported));

}
