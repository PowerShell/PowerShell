// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Security;

namespace System.Management.Automation
{
    /// <summary>
    /// A SessionStateScope defines the scope of visibility for a set
    /// of virtual drives and their data.
    /// </summary>
    internal sealed class SessionStateScope
    {
        #region constructor

        /// <summary>
        /// Constructor for a session state scope.
        /// </summary>
        /// <param name="parentScope">
        /// The parent of this scope.  It can be null for the global scope.
        /// </param>
        internal SessionStateScope(SessionStateScope parentScope)
        {
            ScopeOrigin = CommandOrigin.Internal;
            Parent = parentScope;

            if (parentScope != null)
            {
                // Now copy the script: scope stack from the parent
                _scriptScope = parentScope.ScriptScope;
            }
            else
            {
                _scriptScope = this;
            }
        }

        #endregion constructor

        #region Internal properties

        /// <summary>
        /// Gets the parent scope of this scope.  May be null
        /// for the global scope.
        /// </summary>
        internal SessionStateScope Parent { get; set; }

        /// <summary>
        /// Defines the origin of the command that resulted in this scope
        /// being created.
        /// </summary>
        internal CommandOrigin ScopeOrigin { get; set; }

        /// <summary>
        /// The script scope for this scope. It may reference itself but may not
        /// be a null reference.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="value"/> is null when setting the property.
        /// </exception>
        internal SessionStateScope ScriptScope
        {
            get
            {
                return _scriptScope;
            }

            set
            {
                Diagnostics.Assert(value != null, "Caller to verify scope is not null");
                _scriptScope = value;
            }
        }

        private SessionStateScope _scriptScope;

        /// <summary>
        /// The version of strict mode for the interpreter.
        /// </summary>
        /// <value>Which version of strict mode is active for this scope and it's children.</value>
        internal Version StrictModeVersion { get; set; }

        /// <summary>
        /// Some local variables are stored in this tuple (for non-global scope, any variable assigned to,
        /// or parameters, or some predefined locals.)
        /// </summary>
        internal MutableTuple LocalsTuple { get; set; }

        /// <summary>
        /// When dotting a script, no new scope is created.  Automatic variables must go somewhere, so rather than store
        /// them in the scope they are dotted into, we just store them in a tuple like any other local variable so we
        /// can skip saving and restoring them as the scopes change, instead it's a simple push/pop of this stack.
        ///
        /// This works because in a dotted script block, the only locals in the tuple are the automatic variables, all
        /// other variables use the variable apis to find the variable and get/set it.
        /// </summary>
        internal Stack<MutableTuple> DottedScopes { get { return _dottedScopes; } }

        private readonly Stack<MutableTuple> _dottedScopes = new Stack<MutableTuple>();

        #region Drives
        /// <summary>
        /// Adds a new drive to the scope's drive collection.
        /// </summary>
        /// <param name="newDrive">
        /// The new drive to be added.
        /// </param>
        /// <remarks>
        /// This method assumes the drive has already been verified and
        /// the provider has already been notified.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="newDrive"/> is null.
        /// </exception>
        /// <exception cref="SessionStateException">
        /// If a drive of the same name already exists in this scope.
        /// </exception>
        internal void NewDrive(PSDriveInfo newDrive)
        {
            if (newDrive == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(newDrive));
            }

            // Ensure that multiple threads do not try to modify the
            // drive data at the same time.

            var driveInfos = GetDrives();
            if (driveInfos.ContainsKey(newDrive.Name))
            {
                SessionStateException e =
                    new SessionStateException(
                        newDrive.Name,
                        SessionStateCategory.Drive,
                        "DriveAlreadyExists",
                        SessionStateStrings.DriveAlreadyExists,
                        ErrorCategory.ResourceExists);

                throw e;
            }

            if (!newDrive.IsAutoMounted)
            {
                driveInfos.Add(newDrive.Name, newDrive);
            }
            else
            {
                var automountedDrives = GetAutomountedDrives();
                if (!automountedDrives.ContainsKey(newDrive.Name))
                {
                    automountedDrives.Add(newDrive.Name, newDrive);
                }
            }
        }

        /// <summary>
        /// Removes the specified drive from this scope.
        /// </summary>
        /// <param name="drive">
        /// The drive to be removed.
        /// </param>
        /// <remarks>
        /// This method assumes that the drive has already been validated for removal
        /// by the provider.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="drive"/> is null.
        /// </exception>
        internal void RemoveDrive(PSDriveInfo drive)
        {
            if (drive == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(drive));
            }

            if (_drives == null)
                return;

            var driveInfos = GetDrives();
            if (!driveInfos.Remove(drive.Name))
            {
                // Check to see if it is in the automounted drive collection.
                var automountedDrives = GetAutomountedDrives();
                PSDriveInfo automountedDrive;
                if (automountedDrives.TryGetValue(drive.Name, out automountedDrive))
                {
                    automountedDrive.IsAutoMountedManuallyRemoved = true;

                    // Remove ths persisted from the list of automounted drives.
                    if (drive.IsNetworkDrive)
                    {
                        automountedDrives.Remove(drive.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Removes all the drives from the scope.
        /// </summary>
        internal void RemoveAllDrives()
        {
            GetDrives().Clear();
            GetAutomountedDrives().Clear();
        }

        /// <summary>
        /// Retrieves the drive of the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the drive to retrieve.
        /// </param>
        /// <returns>
        /// An instance of a PSDriveInfo object with the specified name if one
        /// exists in this scope or null if one does not exist.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="name"/> is null.
        /// </exception>
        internal PSDriveInfo GetDrive(string name)
        {
            if (name == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(name));
            }

            PSDriveInfo result = null;

            var driveInfos = GetDrives();
            if (!driveInfos.TryGetValue(name, out result))
            {
                // The caller needs to determine what to do with
                // manually removed drives.
                GetAutomountedDrives().TryGetValue(name, out result);
            }

            return result;
        }

        /// <summary>
        /// Gets an IEnumerable for the drives in this scope.
        /// </summary>
        internal IEnumerable<PSDriveInfo> Drives
        {
            get
            {
                Collection<PSDriveInfo> result = new Collection<PSDriveInfo>();
                foreach (PSDriveInfo drive in GetDrives().Values)
                {
                    result.Add(drive);
                }

                // Now add automounted drives that have not been manually
                // removed by the user.

                foreach (PSDriveInfo drive in GetAutomountedDrives().Values)
                {
                    if (!drive.IsAutoMountedManuallyRemoved)
                    {
                        result.Add(drive);
                    }
                }

                return result;
            }
        }
        #endregion Drives

        #region Variables

        /// <summary>
        /// Gets an IDictionary for the variables in this scope.
        /// </summary>
        internal IDictionary<string, PSVariable> Variables { get { return GetPrivateVariables(); } }

        /// <summary>
        /// Gets the specified variable from the variable table.
        /// </summary>
        /// <param name="name">
        /// The name of the variable to retrieve.
        /// </param>
        /// <param name="origin">
        /// The origin of the command trying to retrieve this variable...
        /// </param>
        /// <returns>
        /// The PSVariable representing the variable specified.
        /// </returns>
        internal PSVariable GetVariable(string name, CommandOrigin origin)
        {
            PSVariable result;
            TryGetVariable(name, origin, false, out result);
            return result;
        }

        /// <summary>
        /// Gets the specified variable from the variable table.
        /// </summary>
        /// <param name="name">
        /// The name of the variable to retrieve.
        /// </param>
        /// <returns>
        /// The PSVariable representing the variable specified.
        /// </returns>
        internal PSVariable GetVariable(string name)
        {
            return GetVariable(name, ScopeOrigin);
        }

        /// <summary>
        /// Looks up a variable, returns true and the variable if found and is visible, throws if the found variable is not visible,
        /// and returns false if there is no variable with the given name in the current scope.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="origin">The command origin (where the scope was created), used to decide if the variable is visible.</param>
        /// <param name="fromNewOrSet">True if looking up the variable as part of a new or set variable operation.</param>
        /// <param name="variable">The variable, if one is found in scope.</param>
        /// <exception cref="SessionStateException">Thrown if the variable is not visible based on CommandOrigin.</exception>
        /// <returns>True if there is a variable in scope, false otherwise.</returns>
        internal bool TryGetVariable(string name, CommandOrigin origin, bool fromNewOrSet, out PSVariable variable)
        {
            Diagnostics.Assert(name != null, "The caller should verify the name");

            if (TryGetLocalVariableFromTuple(name, fromNewOrSet, out variable))
            {
                SessionState.ThrowIfNotVisible(origin, variable);
                return true;
            }

            if (GetPrivateVariables().TryGetValue(name, out variable))
            {
                SessionState.ThrowIfNotVisible(origin, variable);
                return true;
            }

            return false;
        }

        /// <summary>
        /// </summary>
        /// <param name="variable"></param>
        /// <returns></returns>
        internal object GetAutomaticVariableValue(AutomaticVariable variable)
        {
            int index = (int)variable;
            foreach (var dottedScope in _dottedScopes)
            {
                if (dottedScope.IsValueSet(index))
                {
                    return dottedScope.GetValue(index);
                }
            }

            // LocalsTuple should not be null, but the test infrastructure creates scopes
            // and doesn't set LocalsTuple
            if (LocalsTuple != null && LocalsTuple.IsValueSet(index))
            {
                return LocalsTuple.GetValue(index);
            }

            return AutomationNull.Value;
        }

        /// <summary>
        /// Sets a variable to the given value.
        /// </summary>
        /// <param name="name">
        /// The name of the variable to set.
        /// </param>
        /// <param name="value">
        /// The value for the variable
        /// </param>
        /// <param name="asValue">
        /// If true, sets the variable value to newValue. If false, newValue must
        /// be a PSVariable object and the item will be set rather than the value.
        /// </param>
        /// <param name="force">
        /// If true, the variable will be set even if it is readonly.
        /// </param>
        /// <param name="sessionState">
        /// Which SessionState this variable belongs to.
        /// </param>
        /// <param name="origin">
        /// The origin of the caller
        /// </param>
        /// <param name="fastPath">
        /// If true and the variable is being set in the global scope,
        /// then all of the normal variable lookup stuff is bypassed and
        /// the variable is added directly to the dictionary.
        /// </param>
        /// <returns>
        /// The PSVariable representing the variable that was set.
        /// </returns>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the variable is read-only or constant.
        /// </exception>
        internal PSVariable SetVariable(string name, object value, bool asValue, bool force, SessionStateInternal sessionState, CommandOrigin origin = CommandOrigin.Internal, bool fastPath = false)
        {
            Diagnostics.Assert(name != null, "The caller should verify the name");

            PSVariable variable;
            PSVariable variableToSet = value as PSVariable;

            // Set the variable directly in the table, bypassing all of the checks. This
            // can only be used for global scope otherwise the slow path is used.
            if (fastPath)
            {
                if (Parent != null)
                {
                    throw new NotImplementedException("fastPath");
                }

                variable = new PSVariable(name, variableToSet.Value, variableToSet.Options, variableToSet.Attributes) { Description = variableToSet.Description };
                GetPrivateVariables()[name] = variable;
                return variable;
            }

            bool varExists = TryGetVariable(name, origin, true, out variable);

            // Initialize the private variable dictionary if it's not yet
            if (_variables == null)
            {
                GetPrivateVariables();
            }

            if (!asValue && variableToSet != null)
            {
                if (varExists)
                {
                    // First check the variable to ensure that it
                    // is not constant or readonly

                    if (variable == null || variable.IsConstant || (!force && variable.IsReadOnly))
                    {
                        SessionStateUnauthorizedAccessException e =
                            new SessionStateUnauthorizedAccessException(
                                    name,
                                    SessionStateCategory.Variable,
                                    "VariableNotWritable",
                                    SessionStateStrings.VariableNotWritable);

                        throw e;
                    }

                    if (variable is LocalVariable
                        && (variableToSet.Attributes.Count > 0 || variableToSet.Options != variable.Options))
                    {
                        SessionStateUnauthorizedAccessException e =
                            new SessionStateUnauthorizedAccessException(
                                    name,
                                    SessionStateCategory.Variable,
                                    "VariableNotWritableRare",
                                    SessionStateStrings.VariableNotWritableRare);

                        throw e;
                    }

                    if (variable.IsReadOnly && force)
                    {
                        _variables.Remove(name);
                        varExists = false;
                        variable = new PSVariable(name, variableToSet.Value, variableToSet.Options, variableToSet.Attributes) { Description = variableToSet.Description };
                    }
                    else
                    {
                        // Since the variable already exists, copy
                        // the value, options, description, and attributes
                        // to it.
                        variable.Attributes.Clear();

                        variable.Value = variableToSet.Value;
                        variable.Options = variableToSet.Options;
                        variable.Description = variableToSet.Description;

                        foreach (Attribute attr in variableToSet.Attributes)
                        {
                            variable.Attributes.Add(attr);
                        }
                    }
                }
                else
                {
                    // Since the variable doesn't exist, use the new Variable
                    // object

                    variable = variableToSet;
                }
            }
            else if (variable != null)
            {
                variable.Value = value;
            }
            else
            {
                variable = (LocalsTuple?.TrySetVariable(name, value)) ?? new PSVariable(name, value);
            }

            CheckVariableChangeInConstrainedLanguage(variable);

            _variables[name] = variable;
            variable.SessionState = sessionState;
            return variable;
        }

        /// <summary>
        /// Sets a variable to scope without any checks.
        /// This is intended to be used only for global scope.
        /// </summary>
        /// <param name="variableToSet">PSVariable to set.</param>
        /// <param name="sessionState">SessionState for variable.</param>
        /// <returns></returns>
        internal void SetVariableForce(PSVariable variableToSet, SessionStateInternal sessionState)
        {
            if (Parent != null)
            {
                throw new NotImplementedException("SetVariableForce");
            }

            variableToSet.SessionState = sessionState;
            GetPrivateVariables()[variableToSet.Name] = variableToSet;
        }

        /// <summary>
        /// Sets a variable to the given value.
        /// </summary>
        /// <param name="newVariable">
        /// The new variable to create.
        /// </param>
        /// <param name="force">
        /// If true, the variable will be set even if it is readonly.
        /// </param>
        /// <param name="sessionState">
        /// Which SessionState this variable belongs to.
        /// </param>
        /// <returns>
        /// The PSVariable representing the variable that was set.
        /// </returns>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the variable is read-only or constant.
        /// </exception>
        internal PSVariable NewVariable(PSVariable newVariable, bool force, SessionStateInternal sessionState)
        {
            PSVariable variable;
            bool varExists = TryGetVariable(newVariable.Name, ScopeOrigin, true, out variable);

            if (varExists)
            {
                // First check the variable to ensure that it
                // is not constant or readonly

                if (variable == null || variable.IsConstant || (!force && variable.IsReadOnly))
                {
                    SessionStateUnauthorizedAccessException e =
                        new SessionStateUnauthorizedAccessException(
                                newVariable.Name,
                                SessionStateCategory.Variable,
                                "VariableNotWritable",
                                SessionStateStrings.VariableNotWritable);

                    throw e;
                }

                if (variable is LocalVariable)
                {
                    SessionStateUnauthorizedAccessException e =
                        new SessionStateUnauthorizedAccessException(
                                newVariable.Name,
                                SessionStateCategory.Variable,
                                "VariableNotWritableRare",
                                SessionStateStrings.VariableNotWritableRare);

                    throw e;
                }

                // If the new and old variable are the same then don't bother
                // doing the assignment and marking as "removed".
                // This can happen when a module variable is imported twice.
                if (!ReferenceEquals(newVariable, variable))
                {
                    // Mark the old variable as removed...
                    variable.WasRemoved = true;
                    variable = newVariable;
                }
            }
            else
            {
                // Since the variable doesn't exist, use the new Variable
                // object

                variable = newVariable;
            }

            CheckVariableChangeInConstrainedLanguage(variable);

            _variables[variable.Name] = variable;
            variable.SessionState = sessionState;
            return variable;
        }

        /// <summary>
        /// Removes a variable from the variable table.
        /// </summary>
        /// <param name="name">
        /// The name of the variable to remove.
        /// </param>
        /// <param name="force">
        /// If true, the variable will be removed even if its ReadOnly.
        /// </param>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// if the variable is constant.
        /// </exception>
        internal void RemoveVariable(string name, bool force)
        {
            Diagnostics.Assert(
                name != null,
                "The caller should verify the name");

            PSVariable variable = GetVariable(name);

            if (variable.IsConstant || (variable.IsReadOnly && !force))
            {
                SessionStateUnauthorizedAccessException e =
                    new SessionStateUnauthorizedAccessException(
                            name,
                            SessionStateCategory.Variable,
                            "VariableNotRemovable",
                            SessionStateStrings.VariableNotRemovable);

                throw e;
            }

            if (variable is LocalVariable)
            {
                SessionStateUnauthorizedAccessException e =
                    new SessionStateUnauthorizedAccessException(
                            name,
                            SessionStateCategory.Variable,
                            "VariableNotRemovableRare",
                            SessionStateStrings.VariableNotRemovableRare);

                throw e;
            }

            _variables.Remove(name);

            // Finally mark the variable itself has having been removed so
            // anyone holding a reference to it can be aware of this.
            variable.WasRemoved = true;
        }

        internal bool TrySetLocalParameterValue(string name, object value)
        {
            foreach (var dottedScope in _dottedScopes)
            {
                if (dottedScope.TrySetParameter(name, value))
                {
                    return true;
                }
            }

            return LocalsTuple != null && LocalsTuple.TrySetParameter(name, value);
        }

        /// <summary>
        /// For most scopes (global scope being the notable exception), most variables are known ahead of
        /// time and stored in a tuple.  The names of those variables are stored separately, this method
        /// determines if variable name is active in this scope, and if so, returns a wrapper around the
        /// tuple to access the property in the tuple for the given variable.
        /// </summary>
        internal bool TryGetLocalVariableFromTuple(string name, bool fromNewOrSet, out PSVariable result)
        {
            foreach (var dottedScope in _dottedScopes)
            {
                if (dottedScope.TryGetLocalVariable(name, fromNewOrSet, out result))
                {
                    return true;
                }
            }

            result = null;
            return LocalsTuple != null && LocalsTuple.TryGetLocalVariable(name, fromNewOrSet, out result);
        }

        #endregion variables

        #region Aliases

        /// <summary>
        /// Gets an IEnumerable for the aliases in this scope.
        /// </summary>
        internal IEnumerable<AliasInfo> AliasTable
        {
            get
            {
                return GetAliases().Values;
            }
        }

        /// <summary>
        /// Gets the specified alias from the alias table.
        /// </summary>
        /// <param name="name">
        /// The name of the alias to retrieve.
        /// </param>
        /// <returns>
        /// The string representing the value of the alias specified.
        /// </returns>
        internal AliasInfo GetAlias(string name)
        {
            Diagnostics.Assert(
                name != null,
                "The caller should verify the name");

            AliasInfo result;
            GetAliases().TryGetValue(name, out result);

            return result;
        }

        /// <summary>
        /// Sets an alias to the given value.
        /// </summary>
        /// <param name="name">
        /// The name of the alias to set.
        /// </param>
        /// <param name="value">
        /// The value for the alias
        /// </param>
        /// <param name="context">
        /// The execution context for this engine instance.
        /// </param>
        /// <param name="force">
        /// If true, the value will be set even if the alias is ReadOnly.
        /// </param>
        /// <param name="origin">
        /// Origin of the caller of this API
        /// </param>
        /// <returns>
        /// The string representing the value that was set.
        /// </returns>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// if the alias is read-only or constant.
        /// </exception>
        internal AliasInfo SetAliasValue(string name, string value, ExecutionContext context, bool force, CommandOrigin origin)
        {
            Diagnostics.Assert(
                name != null,
                "The caller should verify the name");

            var aliasInfos = GetAliases();
            AliasInfo aliasInfo;
            if (!aliasInfos.TryGetValue(name, out aliasInfo))
            {
                aliasInfos[name] = new AliasInfo(name, value, context);
            }
            else
            {
                // Make sure the alias isn't constant or readonly
                if ((aliasInfo.Options & ScopedItemOptions.Constant) != 0 ||
                    (!force && (aliasInfo.Options & ScopedItemOptions.ReadOnly) != 0))
                {
                    SessionStateUnauthorizedAccessException e =
                        new SessionStateUnauthorizedAccessException(
                                name,
                                SessionStateCategory.Alias,
                                "AliasNotWritable",
                                SessionStateStrings.AliasNotWritable);

                    throw e;
                }

                SessionState.ThrowIfNotVisible(origin, aliasInfo);
                RemoveAliasFromCache(aliasInfo.Name, aliasInfo.Definition);

                if (force)
                {
                    aliasInfos.Remove(name);
                    aliasInfo = new AliasInfo(name, value, context);
                    aliasInfos[name] = aliasInfo;
                }
                else
                {
                    aliasInfo.SetDefinition(value, false);
                }
            }

            AddAliasToCache(name, value);

            return aliasInfos[name];
        }

        /// <summary>
        /// Sets an alias to the given value.
        /// </summary>
        /// <param name="name">
        /// The name of the alias to set.
        /// </param>
        /// <param name="value">
        /// The value for the alias
        /// </param>
        /// <param name="context">
        /// The execution context for this engine instance.
        /// </param>
        /// <param name="options">
        /// The options to set on the alias.
        /// </param>
        /// <param name="force">
        /// If true, the value will be set even if the alias is ReadOnly.
        /// </param>
        /// <param name="origin">
        /// Origin of the caller of this API
        /// </param>
        /// <returns>
        /// The string representing the value that was set.
        /// </returns>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the alias is read-only or constant.
        /// </exception>
        internal AliasInfo SetAliasValue(
            string name,
            string value,
            ScopedItemOptions options,
            ExecutionContext context,
            bool force,
            CommandOrigin origin)
        {
            Diagnostics.Assert(
                name != null,
                "The caller should verify the name");

            var aliasInfos = GetAliases();
            AliasInfo aliasInfo;
            AliasInfo result;
            if (!aliasInfos.TryGetValue(name, out aliasInfo))
            {
                result = new AliasInfo(name, value, context, options);
                aliasInfos[name] = result;
            }
            else
            {
                // Make sure the alias isn't constant or readonly
                if ((aliasInfo.Options & ScopedItemOptions.Constant) != 0 ||
                    (!force && (aliasInfo.Options & ScopedItemOptions.ReadOnly) != 0))
                {
                    SessionStateUnauthorizedAccessException e =
                        new SessionStateUnauthorizedAccessException(
                                name,
                                SessionStateCategory.Alias,
                                "AliasNotWritable",
                                SessionStateStrings.AliasNotWritable);

                    throw e;
                }

                // Ensure we are not trying to set the alias to constant as this can only be
                // done at creation time.

                if ((options & ScopedItemOptions.Constant) != 0)
                {
                    SessionStateUnauthorizedAccessException e =
                        new SessionStateUnauthorizedAccessException(
                                name,
                                SessionStateCategory.Alias,
                                "AliasCannotBeMadeConstant",
                                SessionStateStrings.AliasCannotBeMadeConstant);

                    throw e;
                }

                if ((options & ScopedItemOptions.AllScope) == 0 &&
                    (aliasInfo.Options & ScopedItemOptions.AllScope) != 0)
                {
                    // user is trying to remove the AllScope option from the alias.
                    // Do not allow this (as per spec).

                    SessionStateUnauthorizedAccessException e =
                        new SessionStateUnauthorizedAccessException(
                                name,
                                SessionStateCategory.Alias,
                                "AliasAllScopeOptionCannotBeRemoved",
                                SessionStateStrings.AliasAllScopeOptionCannotBeRemoved);

                    throw e;
                }

                SessionState.ThrowIfNotVisible(origin, aliasInfo);
                RemoveAliasFromCache(aliasInfo.Name, aliasInfo.Definition);

                if (force)
                {
                    aliasInfos.Remove(name);
                    result = new AliasInfo(name, value, context, options);
                    aliasInfos[name] = result;
                }
                else
                {
                    result = aliasInfo;
                    aliasInfo.Options = options;
                    aliasInfo.SetDefinition(value, false);
                }
            }

            AddAliasToCache(name, value);

            return result;
        }

        /// <summary>
        /// Sets an alias to the given value.
        /// </summary>
        /// <param name="aliasToSet">
        /// The information about the alias to be set
        /// </param>
        /// <param name="force">
        /// If true, the alias will be set even if there is an existing ReadOnly
        /// alias.
        /// </param>
        /// <param name="origin">
        /// Specifies the command origin of the calling command.
        /// </param>
        /// <returns>
        /// The string representing the value that was set.
        /// </returns>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the alias is read-only or constant.
        /// </exception>
        internal AliasInfo SetAliasItem(AliasInfo aliasToSet, bool force, CommandOrigin origin = CommandOrigin.Internal)
        {
            Diagnostics.Assert(
                aliasToSet != null,
                "The caller should verify the aliasToSet");

            var aliasInfos = GetAliases();
            AliasInfo aliasInfo;
            if (aliasInfos.TryGetValue(aliasToSet.Name, out aliasInfo))
            {
                // An existing alias cannot be set if it is ReadOnly or Constant unless
                // force is specified, in which case an existing ReadOnly alias can
                // be set.

                SessionState.ThrowIfNotVisible(origin, aliasInfo);
                if ((aliasInfo.Options & ScopedItemOptions.Constant) != 0 ||
                    ((aliasInfo.Options & ScopedItemOptions.ReadOnly) != 0 && !force))
                {
                    SessionStateUnauthorizedAccessException e =
                        new SessionStateUnauthorizedAccessException(
                                aliasToSet.Name,
                                SessionStateCategory.Alias,
                                "AliasNotWritable",
                                SessionStateStrings.AliasNotWritable);

                    throw e;
                }

                if ((aliasToSet.Options & ScopedItemOptions.AllScope) == 0 &&
                    (aliasInfo.Options & ScopedItemOptions.AllScope) != 0)
                {
                    // user is trying to remove the AllScope option from the alias.
                    // Do not allow this (as per spec).

                    SessionStateUnauthorizedAccessException e =
                        new SessionStateUnauthorizedAccessException(
                                aliasToSet.Name,
                                SessionStateCategory.Alias,
                                "AliasAllScopeOptionCannotBeRemoved",
                                SessionStateStrings.AliasAllScopeOptionCannotBeRemoved);

                    throw e;
                }

                RemoveAliasFromCache(aliasInfo.Name, aliasInfo.Definition);
            }

            aliasInfos[aliasToSet.Name] = aliasToSet;

            AddAliasToCache(aliasToSet.Name, aliasToSet.Definition);

            return aliasToSet;
        }

        /// <summary>
        /// Removes a alias from the alias table.
        /// </summary>
        /// <param name="name">
        /// The name of the alias to remove.
        /// </param>
        /// <param name="force">
        /// If true, the alias will be removed even if it is ReadOnly.
        /// </param>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the alias is constant.
        /// </exception>
        internal void RemoveAlias(string name, bool force)
        {
            Diagnostics.Assert(
                name != null,
                "The caller should verify the name");

            // Make sure the alias isn't constant or readonly

            var aliasInfos = GetAliases();
            AliasInfo aliasInfo;
            if (aliasInfos.TryGetValue(name, out aliasInfo))
            {
                if ((aliasInfo.Options & ScopedItemOptions.Constant) != 0 ||
                    (!force && (aliasInfo.Options & ScopedItemOptions.ReadOnly) != 0))
                {
                    SessionStateUnauthorizedAccessException e =
                        new SessionStateUnauthorizedAccessException(
                                name,
                                SessionStateCategory.Alias,
                                "AliasNotRemovable",
                                SessionStateStrings.AliasNotRemovable);

                    throw e;
                }

                RemoveAliasFromCache(aliasInfo.Name, aliasInfo.Definition);
            }

            aliasInfos.Remove(name);
        }

        #endregion aliases

        #region Functions

        /// <summary>
        /// Gets an IEnumerable for the functions in this scope.
        /// </summary>
        internal Dictionary<string, FunctionInfo> FunctionTable
        {
            get
            {
                return GetFunctions();
            }
        }

        /// <summary>
        /// Gets the specified function from the function table.
        /// </summary>
        /// <param name="name">
        /// The name of the function to retrieve.
        /// </param>
        /// <returns>
        /// A FunctionInfo that is either a FilterInfo or FunctionInfo representing the
        /// function or filter.
        /// </returns>
        internal FunctionInfo GetFunction(string name)
        {
            Diagnostics.Assert(
                name != null,
                "The caller should verify the name");

            FunctionInfo result;
            GetFunctions().TryGetValue(name, out result);

            return result;
        }

        /// <summary>
        /// Sets an function to the given function declaration.
        /// </summary>
        /// <param name="name">
        /// The name of the function to set.
        /// </param>
        /// <param name="function">
        /// The script block that represents the code for the function.
        /// </param>
        /// <param name="force">
        /// If true, the function will be set even if its ReadOnly.
        /// </param>
        /// <param name="origin">
        /// The origin of the caller of this API
        /// </param>
        /// <param name="context">
        /// The execution context for the function/filter.
        /// </param>
        /// <returns>
        /// A FunctionInfo that is either a FilterInfo or FunctionInfo representing the
        /// function or filter.
        /// </returns>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the function is read-only or constant.
        /// </exception>
        internal FunctionInfo SetFunction(
            string name,
            ScriptBlock function,
            bool force,
            CommandOrigin origin,
            ExecutionContext context)
        {
            return SetFunction(name, function, null, ScopedItemOptions.Unspecified, force, origin, context);
        }
        /// <summary>
        /// Sets an function to the given function declaration.
        /// </summary>
        /// <param name="name">
        /// The name of the function to set.
        /// </param>
        /// <param name="function">
        /// The script block that represents the code for the function.
        /// </param>
        /// <param name="originalFunction">
        /// The original function (if any) from which the scriptblock was derived.
        /// </param>
        /// <param name="force">
        /// If true, the function will be set even if its ReadOnly.
        /// </param>
        /// <param name="origin">
        /// The origin of the caller of this API
        /// </param>
        /// <param name="context">
        /// The execution context for the function/filter.
        /// </param>
        /// <returns>
        /// A FunctionInfo that is either a FilterInfo or FunctionInfo representing the
        /// function or filter.
        /// </returns>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the function is read-only or constant.
        /// </exception>
        internal FunctionInfo SetFunction(
            string name,
            ScriptBlock function,
            FunctionInfo originalFunction,
            bool force,
            CommandOrigin origin,
            ExecutionContext context)
        {
            return SetFunction(name, function, originalFunction, ScopedItemOptions.Unspecified, force, origin, context);
        }

        /// <summary>
        /// Sets an function to the given function declaration.
        /// </summary>
        /// <param name="name">
        /// The name of the function to set.
        /// </param>
        /// <param name="function">
        /// The script block that the function should represent.
        /// </param>
        /// <param name="originalFunction">
        /// The original function (if any) from which the scriptblock was derived.
        /// </param>
        /// <param name="options">
        /// The options that should be applied to the function.
        /// </param>
        /// <param name="force">
        /// If true, the function will be set even if its ReadOnly.
        /// </param>
        /// <param name="origin">
        /// The origin of the caller of this API
        /// </param>
        /// <param name="context">
        /// The execution context for the function/filter.
        /// </param>
        /// <returns>
        /// A FunctionInfo that is either a FilterInfo or FunctionInfo representing the
        /// function or filter.
        /// </returns>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the function is read-only or constant.
        /// </exception>
        internal FunctionInfo SetFunction(
            string name,
            ScriptBlock function,
            FunctionInfo originalFunction,
            ScopedItemOptions options,
            bool force,
            CommandOrigin origin,
            ExecutionContext context)
        {
            return SetFunction(name, function, originalFunction, options, force, origin, context, null);
        }

        internal FunctionInfo SetFunction(
            string name,
            ScriptBlock function,
            FunctionInfo originalFunction,
            ScopedItemOptions options,
            bool force,
            CommandOrigin origin,
            ExecutionContext context,
            string helpFile)
        {
            return SetFunction(name, function, originalFunction, options, force, origin, context, helpFile, CreateFunction);
        }

        /// <summary>
        /// Sets an function to the given function declaration.
        /// </summary>
        /// <param name="name">
        /// The name of the function to set.
        /// </param>
        /// <param name="function">
        /// The script block that the function should represent.
        /// </param>
        /// <param name="originalFunction">
        /// The original function (if any) from which the scriptblock was derived.
        /// </param>
        /// <param name="options">
        /// The options that should be applied to the function.
        /// </param>
        /// <param name="force">
        /// If true, the function will be set even if its ReadOnly.
        /// </param>
        /// <param name="origin">
        /// The origin of the caller of this API
        /// </param>
        /// <param name="context">
        /// The execution context for the function/filter.
        /// </param>
        /// <param name="helpFile">
        /// The name of the help file associated with the function.
        /// </param>
        /// <param name="functionFactory">
        /// Function to create the FunctionInfo.
        /// </param>
        /// <returns>
        /// A FunctionInfo that is either a FilterInfo or FunctionInfo representing the
        /// function or filter.
        /// </returns>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the function is read-only or constant.
        /// </exception>
        internal FunctionInfo SetFunction(
            string name,
            ScriptBlock function,
            FunctionInfo originalFunction,
            ScopedItemOptions options,
            bool force,
            CommandOrigin origin,
            ExecutionContext context,
            string helpFile,
            Func<string, ScriptBlock, FunctionInfo, ScopedItemOptions, ExecutionContext, string, FunctionInfo> functionFactory)
        {
            Diagnostics.Assert(
                name != null,
                "The caller should verify the name");

            Dictionary<string, FunctionInfo> functionInfos = GetFunctions();
            FunctionInfo result;

            // Functions are equal only if they have the same name and if they come from the same module (if any).
            // If the function is not associated with a module then the info 'ModuleName' property is set to empty string.
            // If the new function has the same name of an existing function, but different module names, then the
            // existing table function is replaced with the new function.
            if (!functionInfos.TryGetValue(name, out FunctionInfo existingValue) ||
                (originalFunction != null &&
                    !existingValue.ModuleName.Equals(originalFunction.ModuleName, StringComparison.OrdinalIgnoreCase)))
            {
                // Add new function info to function table and return.
                result = functionFactory(name, function, originalFunction, options, context, helpFile);
                functionInfos[name] = result;

                if (IsFunctionOptionSet(result, ScopedItemOptions.AllScope))
                {
                    GetAllScopeFunctions()[name] = result;
                }

                return result;
            }

            // Update the existing function.

            // Make sure the function isn't constant or readonly.
            SessionState.ThrowIfNotVisible(origin, existingValue);

            if (IsFunctionOptionSet(existingValue, ScopedItemOptions.Constant) ||
                (!force && IsFunctionOptionSet(existingValue, ScopedItemOptions.ReadOnly)))
            {
                SessionStateUnauthorizedAccessException e =
                    new SessionStateUnauthorizedAccessException(
                            name,
                            SessionStateCategory.Function,
                            "FunctionNotWritable",
                            SessionStateStrings.FunctionNotWritable);

                throw e;
            }

            // Ensure we are not trying to set the function to constant as this can only be
            // done at creation time.
            if ((options & ScopedItemOptions.Constant) != 0)
            {
                SessionStateUnauthorizedAccessException e =
                    new SessionStateUnauthorizedAccessException(
                            name,
                            SessionStateCategory.Function,
                            "FunctionCannotBeMadeConstant",
                            SessionStateStrings.FunctionCannotBeMadeConstant);

                throw e;
            }

            // Ensure we are not trying to remove the AllScope option.
            if ((options & ScopedItemOptions.AllScope) == 0 &&
                IsFunctionOptionSet(existingValue, ScopedItemOptions.AllScope))
            {
                SessionStateUnauthorizedAccessException e =
                    new SessionStateUnauthorizedAccessException(
                            name,
                            SessionStateCategory.Function,
                            "FunctionAllScopeOptionCannotBeRemoved",
                            SessionStateStrings.FunctionAllScopeOptionCannotBeRemoved);

                throw e;
            }

            FunctionInfo existingFunction = existingValue;

            // If the function type changes (i.e.: function to workflow or back)
            // then we need to replace what was there.
            FunctionInfo newValue = functionFactory(name, function, originalFunction, options, context, helpFile);

            bool changesFunctionType = existingFunction.GetType() != newValue.GetType();

            // Since the options are set after the script block, we have to
            // forcefully apply the script block if the options will be
            // set to not being ReadOnly.
            if (changesFunctionType ||
                ((existingFunction.Options & ScopedItemOptions.ReadOnly) != 0 && force))
            {
                result = newValue;
                functionInfos[name] = newValue;
            }
            else
            {
                bool applyForce = force || (options & ScopedItemOptions.ReadOnly) == 0;
                existingFunction.Update(newValue, applyForce, options, helpFile);
                result = existingFunction;
            }

            return result;
        }

        /// <summary>
        /// Removes a function from the function table.
        /// </summary>
        /// <param name="name">
        /// The name of the function to remove.
        /// </param>
        /// <param name="force">
        /// If true, the function is removed even if it is ReadOnly.
        /// </param>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the function is constant.
        /// </exception>
        internal void RemoveFunction(string name, bool force)
        {
            Diagnostics.Assert(
                name != null,
                "The caller should verify the name");

            var functionInfos = GetFunctions();
            FunctionInfo function;
            if (functionInfos.TryGetValue(name, out function))
            {
                if (IsFunctionOptionSet(function, ScopedItemOptions.Constant) ||
                    (!force && IsFunctionOptionSet(function, ScopedItemOptions.ReadOnly)))
                {
                    SessionStateUnauthorizedAccessException e =
                        new SessionStateUnauthorizedAccessException(
                                name,
                                SessionStateCategory.Function,
                                "FunctionNotRemovable",
                                SessionStateStrings.FunctionNotRemovable);

                    throw e;
                }

                if (IsFunctionOptionSet(function, ScopedItemOptions.AllScope))
                {
                    GetAllScopeFunctions().Remove(name);
                }
            }

            functionInfos.Remove(name);
        }

        #endregion functions

        #region Cmdlets

        /// <summary>
        /// Gets an IEnumerable for the cmdlets in this scope.
        /// </summary>
        internal Dictionary<string, List<CmdletInfo>> CmdletTable
        {
            get
            {
                return _cmdlets;
            }
        }

        /// <summary>
        /// Gets the specified cmdlet from the cmdlet table.
        /// </summary>
        /// <param name="name">
        /// The name of the cmdlet to retrieve.
        /// </param>
        /// <returns>
        /// A CmdletInfo representing this cmdlet
        /// </returns>
        internal CmdletInfo GetCmdlet(string name)
        {
            Diagnostics.Assert(
                name != null,
                "The caller should verify the name");

            CmdletInfo result = null;

            List<CmdletInfo> cmdlets;

            if (_cmdlets.TryGetValue(name, out cmdlets))
            {
                if (cmdlets != null && cmdlets.Count > 0)
                {
                    result = cmdlets[0];
                }
            }

            return result;
        }

        /// <summary>
        /// Adds a cmdlet to the cmdlet cache.
        /// </summary>
        /// <param name="name">
        /// The name of the cmdlet to add.
        /// </param>
        /// <param name="cmdlet">
        /// The cmdlet that should be added.
        /// </param>
        /// <param name="origin">
        /// The origin of the caller of this API
        /// </param>
        /// <param name="context">
        /// The execution context for the cmdlet.
        /// </param>
        /// <returns>
        /// A CmdletInfo representing the cmdlet
        /// </returns>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the cmdlet is read-only or constant.
        /// </exception>
        ///
        internal CmdletInfo AddCmdletToCache(
            string name,
            CmdletInfo cmdlet,
            CommandOrigin origin,
            ExecutionContext context)
        {
            bool throwNotSupported = false;
            try
            {
                Diagnostics.Assert(
                    name != null,
                    "The caller should verify the name");

                List<CmdletInfo> cmdlets;
                if (!_cmdlets.TryGetValue(name, out cmdlets))
                {
                    cmdlets = new List<CmdletInfo>();
                    cmdlets.Add(cmdlet);
                    _cmdlets.Add(name, cmdlets);

                    if ((cmdlet.Options & ScopedItemOptions.AllScope) != 0)
                    {
                        _allScopeCmdlets[name].Insert(0, cmdlet);
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(cmdlet.ModuleName))
                    {
                        // Need to be sure that the existing cmdlet doesn't have the same snapin name
                        foreach (CmdletInfo cmdletInfo in cmdlets)
                        {
                            if (string.Equals(cmdlet.FullName, cmdletInfo.FullName,
                                              StringComparison.OrdinalIgnoreCase))
                            {
                                if (cmdlet.ImplementingType == cmdletInfo.ImplementingType)
                                {
                                    // It is already added in the cache. Do not add it again
                                    return null;
                                }
                                // Otherwise it's an error...
                                throwNotSupported = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        // If there's no module name, then see if there is a cmdlet that matches the type
                        foreach (CmdletInfo cmdletInfo in cmdlets)
                        {
                            if (cmdlet.ImplementingType == cmdletInfo.ImplementingType)
                            {
                                // It's already in the cache so don't need to add it again...
                                return null;
                            }

                            // Otherwise it's an error...
                            throwNotSupported = true;
                            break;
                        }
                    }
                    // Insert the cmdlet if a duplicate doesn't already exist
                    if (!throwNotSupported)
                    {
                        cmdlets.Insert(0, cmdlet);
                    }
                }
            }
            catch (ArgumentException)
            {
                throwNotSupported = true;
            }

            if (throwNotSupported)
            {
                PSNotSupportedException notSupported =
                    PSTraceSource.NewNotSupportedException(
                        DiscoveryExceptions.DuplicateCmdletName,
                        cmdlet.Name);

                throw notSupported;
            }

            return _cmdlets[name][0];
        }

        /// <summary>
        /// Removes a cmdlet from the cmdlet table.
        /// </summary>
        /// <param name="name">
        /// The name of the cmdlet to remove.
        /// </param>
        /// <param name="index">
        /// The index at which to remove the cmdlet
        /// If index is -1, remove all cmdlets with that name
        /// </param>
        /// <param name="force">
        /// If true, the cmdlet is removed even if it is ReadOnly.
        /// </param>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the cmdlet is constant.
        /// </exception>
        internal void RemoveCmdlet(string name, int index, bool force)
        {
            Diagnostics.Assert(
                name != null,
                "The caller should verify the name");

            List<CmdletInfo> cmdlets;
            if (_cmdlets.TryGetValue(name, out cmdlets))
            {
                CmdletInfo tempCmdlet = cmdlets[index];

                if ((tempCmdlet.Options & ScopedItemOptions.AllScope) != 0)
                {
                    _allScopeCmdlets[name].RemoveAt(index);
                }

                cmdlets.RemoveAt(index);

                // Remove the entry is the list is now empty
                if (cmdlets.Count == 0)
                {
                    // Remove the key
                    _cmdlets.Remove(name);
                    return;
                }
            }
        }

        /// <summary>
        /// Removes a cmdlet entry from the cmdlet table.
        /// </summary>
        /// <param name="name">
        /// The key for the cmdlet entry to remove.
        /// </param>
        /// <param name="force">
        /// If true, the cmdlet entry is removed even if it is ReadOnly.
        /// </param>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the cmdlet is constant.
        /// </exception>
        internal void RemoveCmdletEntry(string name, bool force)
        {
            Diagnostics.Assert(
                name != null,
                "The caller should verify the name");

            _cmdlets.Remove(name);
        }

        #endregion Cmdlets

        #region Types

        private Language.TypeResolutionState _typeResolutionState;

        internal Language.TypeResolutionState TypeResolutionState
        {
            get
            {
                if (_typeResolutionState != null)
                {
                    return _typeResolutionState;
                }

                return Parent != null ? Parent.TypeResolutionState : Language.TypeResolutionState.UsingSystem;
            }

            set
            {
                _typeResolutionState = value;
            }
        }

        internal IDictionary<string, Type> TypeTable { get; private set; }

        internal void AddType(string name, Type type)
        {
            TypeTable ??= new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

            TypeTable[name] = type;
        }

        internal Type LookupType(string name)
        {
            if (TypeTable == null)
            {
                return null;
            }

            Type result;
            TypeTable.TryGetValue(name, out result);

            return result;
        }

        #endregion Types

        #endregion Internal methods

        #region Private members

        private static bool IsFunctionOptionSet(FunctionInfo function, ScopedItemOptions options)
        {
            return (function.Options & options) != 0;
        }

        private static FunctionInfo CreateFunction(string name, ScriptBlock function, FunctionInfo originalFunction,
            ScopedItemOptions options, ExecutionContext context, string helpFile)
        {
            FunctionInfo newValue = null;

            if (options == ScopedItemOptions.Unspecified)
            {
                options = ScopedItemOptions.None;
            }

            // First use the copy constructors
            if (originalFunction is FilterInfo)
            {
                newValue = new FilterInfo(name, (FilterInfo)originalFunction);
            }
            else if (originalFunction is ConfigurationInfo)
            {
                newValue = new ConfigurationInfo(name, (ConfigurationInfo)originalFunction);
            }
            else if (originalFunction != null)
            {
                newValue = new FunctionInfo(name, originalFunction);
            }

            // Then use the creation constructors - workflows don't get here because the workflow info
            // is created during compilation.
            else if (function.IsFilter)
            {
                newValue = new FilterInfo(name, function, options, context, helpFile);
            }
            else if (function.IsConfiguration)
            {
                newValue = new ConfigurationInfo(name, function, options, context, helpFile, function.IsMetaConfiguration());
            }
            else
            {
                newValue = new FunctionInfo(name, function, options, context, helpFile);
            }

            return newValue;
        }

        /// <summary>
        /// Contains the virtual drives for this scope.
        /// </summary>
        // Initializing all of the session state items every time we create a new scope causes a measurable
        // performance degradation, so we use lazy initialization for all of them.
        private Dictionary<string, PSDriveInfo> GetDrives()
        {
            return _drives ??= new Dictionary<string, PSDriveInfo>(StringComparer.OrdinalIgnoreCase);
        }

        private Dictionary<string, PSDriveInfo> _drives;

        /// <summary>
        /// Contains the drives that have been automounted by the system.
        /// </summary>
        // Initializing all of the session state items every time we create a new scope causes a measurable
        // performance degradation, so we use lazy initialization for all of them.
        private Dictionary<string, PSDriveInfo> GetAutomountedDrives()
        {
            return _automountedDrives ??= new Dictionary<string, PSDriveInfo>(StringComparer.OrdinalIgnoreCase);
        }

        private Dictionary<string, PSDriveInfo> _automountedDrives;

        private Dictionary<string, PSVariable> _variables;

        private Dictionary<string, PSVariable> GetPrivateVariables()
        {
            if (_variables == null)
            {
                // Create the variables collection with the default parameters
                _variables = new Dictionary<string, PSVariable>(StringComparer.OrdinalIgnoreCase);

                // Create the default variables in the global scope.
                // If the variable must propagate to each new scope,
                // the AllScope option must be set.

                AddSessionStateScopeDefaultVariables();
            }

            return _variables;
        }

        /// <summary>
        /// Add the built-in variables defined by the session state scope.
        /// </summary>
        internal void AddSessionStateScopeDefaultVariables()
        {
            if (Parent == null)
            {
                // Create the default variables that are in every scope
                // These variables will automatically propagate to new
                // scopes since they are marked AllScope.

                _variables.Add(s_nullVar.Name, s_nullVar);
                _variables.Add(s_falseVar.Name, s_falseVar);
                _variables.Add(s_trueVar.Name, s_trueVar);
            }
            else
            {
                // Propagate all variables that are marked AllScope.
                foreach (PSVariable variable in Parent.GetPrivateVariables().Values)
                {
                    if (variable.IsAllScope)
                        _variables.Add(variable.Name, variable);
                }
            }
        }

        /// <summary>
        /// A collection of the aliases defined for the session.
        /// </summary>
        // Initializing all of the session state items every time we create a new scope causes a measurable
        // performance degradation, so we use lazy initialization for all of them.
        private Dictionary<string, AliasInfo> GetAliases()
        {
            if (_alias == null)
            {
                // Create the alias table
                _alias = new Dictionary<string, AliasInfo>(StringComparer.OrdinalIgnoreCase);

                if (Parent != null)
                {
                    // Propagate all aliases that are marked AllScope
                    foreach (AliasInfo newAlias in Parent.GetAliases().Values)
                    {
                        if ((newAlias.Options & ScopedItemOptions.AllScope) != 0)
                        {
                            _alias.Add(newAlias.Name, newAlias);
                        }
                    }
                }
            }

            return _alias;
        }

        private Dictionary<string, AliasInfo> _alias;

        /// <summary>
        /// A collection of the functions defined in this scope...
        /// </summary>
        // Initializing all of the session state items every time we create a new scope causes a measurable
        // performance degradation, so we use lazy initialization for all of them.
        private Dictionary<string, FunctionInfo> GetFunctions()
        {
            if (_functions == null)
            {
                // Create the functions table
                _functions = new Dictionary<string, FunctionInfo>(StringComparer.OrdinalIgnoreCase);

                if (Parent != null && Parent._allScopeFunctions != null)
                {
                    // Propagate all functions that are marked AllScope
                    foreach (FunctionInfo newFunc in Parent._allScopeFunctions.Values)
                    {
                        _functions.Add(newFunc.Name, newFunc);
                    }
                }
            }

            return _functions;
        }

        private Dictionary<string, FunctionInfo> _functions;

        /// <summary>
        /// All entries in this table should also be in the normal function
        /// table. The entries in this table are automatically propagated
        /// to new scopes.
        /// </summary>
        // Initializing all of the session state items every time we create a new scope causes a measurable
        // performance degradation, so we use lazy initialization for all of them.
        private Dictionary<string, FunctionInfo> GetAllScopeFunctions()
        {
            if (_allScopeFunctions == null)
            {
                if (Parent != null && Parent._allScopeFunctions != null)
                {
                    return Parent._allScopeFunctions;
                }

                // Create the "AllScope" functions table
                _allScopeFunctions = new Dictionary<string, FunctionInfo>(StringComparer.OrdinalIgnoreCase);
            }

            return _allScopeFunctions;
        }

        private Dictionary<string, FunctionInfo> _allScopeFunctions;

        // The value for the cmdlet cache is a list of CmdletInfo objects because of the following reason
        // Import-Module Mod1 -Cmdlet foo
        // Import-Module Mod2 -Cmdlet foo
        // Remove-Module Mod2
        // foo
        // The command "foo" from Mod1 is invoked.
        // If we do not maintain a list, we break this behavior as we would have over-written Mod1\foo with Mod2\foo and then Mod2 is removed, we have nothing.

        private readonly Dictionary<string, List<CmdletInfo>> _cmdlets = new Dictionary<string, List<CmdletInfo>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// All entries in this table should also be in the normal cmdlet
        /// table. The entries in this table are automatically propagated
        /// to new scopes.
        /// </summary>
        private readonly Dictionary<string, List<CmdletInfo>> _allScopeCmdlets = new Dictionary<string, List<CmdletInfo>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The variable that represents $true in the language.
        /// We don't need a new reference in each scope since it
        /// is ScopedItemOptions.Constant.
        /// </summary>
        private static readonly PSVariable s_trueVar =
            new PSVariable(
                StringLiterals.True,
                true,
                ScopedItemOptions.Constant | ScopedItemOptions.AllScope,
                "Boolean True");

        /// <summary>
        /// The variable that represents $false in the language.
        /// We don't need a new reference in each scope since it
        /// is ScopedItemOptions.Constant.
        /// </summary>
        private static readonly PSVariable s_falseVar =
            new PSVariable(
                StringLiterals.False,
                false,
                ScopedItemOptions.Constant | ScopedItemOptions.AllScope,
                "Boolean False");

        /// <summary>
        /// The variable that represents $null in the language.
        /// We don't need a new reference in each scope since it
        /// is ScopedItemOptions.Constant.
        /// </summary>
        private static readonly NullVariable s_nullVar =
            new NullVariable();

        #endregion Private members

        #region Alias mapping

        private readonly Dictionary<string, List<string>> _commandsToAliasesCache = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the aliases by command name (used by metadata-driven help)
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        internal IEnumerable<string> GetAliasesByCommandName(string command)
        {
            List<string> commandsToAliases;
            if (_commandsToAliasesCache.TryGetValue(command, out commandsToAliases))
            {
                foreach (string str in commandsToAliases)
                {
                    yield return str;
                }
            }

            yield break;
        }

        /// <summary>
        /// </summary>
        /// <param name="alias"></param>
        /// <param name="value"></param>
        private void AddAliasToCache(string alias, string value)
        {
            List<string> existingAliases;
            if (!_commandsToAliasesCache.TryGetValue(value, out existingAliases))
            {
                List<string> list = new List<string>();
                list.Add(alias);
                _commandsToAliasesCache.Add(value, list);
            }
            else
            {
                if (!existingAliases.Contains(alias, StringComparer.OrdinalIgnoreCase))
                {
                    existingAliases.Add(alias);
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="alias"></param>
        /// <param name="value"></param>
        private void RemoveAliasFromCache(string alias, string value)
        {
            List<string> list;
            if (!_commandsToAliasesCache.TryGetValue(value, out list))
            {
                return;
            }

            if (list.Count <= 1)
            {
                _commandsToAliasesCache.Remove(value);
            }
            else
            {
                string itemToRemove = list.Find(item => item.Equals(alias, StringComparison.OrdinalIgnoreCase));
                if (itemToRemove != null)
                {
                    list.Remove(itemToRemove);
                }
            }
        }

        private void CheckVariableChangeInConstrainedLanguage(PSVariable variable)
        {
            var context = LocalPipeline.GetExecutionContextFromTLS();
            if (context == null)
            {
                return;
            }

            if ((ExecutionContext.HasEverUsedConstrainedLanguage && context.LanguageMode == PSLanguageMode.ConstrainedLanguage) ||
                context.LanguageMode == PSLanguageMode.ConstrainedLanguageAudit)
            {
                switch (context.LanguageMode)
                {
                    case PSLanguageMode.ConstrainedLanguage:
                        if ((variable.Options & ScopedItemOptions.AllScope) == ScopedItemOptions.AllScope)
                        {
                            // Don't let people set AllScope variables in ConstrainedLanguage, as they can be used to
                            // interfere with the session state of trusted commands.
                            throw new PSNotSupportedException();
                        }

                        // Mark untrusted values for assignments to 'Global:' variables, and 'Script:' variables in
                        // a module scope, if it's necessary.
                        ExecutionContext.MarkObjectAsUntrustedForVariableAssignment(variable, this, context.EngineSessionState);
                        break;

                    case PSLanguageMode.ConstrainedLanguageAudit:
                        if ((variable.Options & ScopedItemOptions.AllScope) == ScopedItemOptions.AllScope)
                        {
                            SystemPolicy.LogWDACAuditMessage(
                                Title: "Session State Variables",
                                Message: $"Changing or creating the variable {variable.Name} scope to AllScope will be prevented in ConstrainedLanguage mode for unstrusted script.",
                                FQID: "AllScopeVariableNotAllowed");
                        }
                        break;
                }
            }
        }

        #endregion
    }
}
