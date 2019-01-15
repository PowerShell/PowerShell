// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Management.Automation.Runspaces;
using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    /// <summary>
    /// Holds the state of a Monad Shell session.
    /// </summary>
    internal sealed partial class SessionStateInternal
    {
        #region aliases

        /// <summary>
        /// Add a new alias entry to this session state object...
        /// </summary>
        /// <param name="entry">The entry to add.</param>
        /// <param name="scopeID">
        /// A scope identifier that is either one of the "special" scopes like
        /// "global", "script", "local", or "private, or a numeric ID of a relative scope
        /// to the current scope.
        /// </param>
        internal void AddSessionStateEntry(SessionStateAliasEntry entry, string scopeID)
        {
            AliasInfo alias = new AliasInfo(entry.Name, entry.Definition, this.ExecutionContext, entry.Options)
            {
                Visibility = entry.Visibility,
                Module = entry.Module,
                Description = entry.Description
            };

            // Create alias in the global scope...
            this.SetAliasItemAtScope(alias, scopeID, true, CommandOrigin.Internal);
        }

        /// <summary>
        /// Gets an IEnumerable for the alias table.
        /// </summary>
        internal IDictionary<string, AliasInfo> GetAliasTable()
        {
            Dictionary<string, AliasInfo> result =
                new Dictionary<string, AliasInfo>(StringComparer.OrdinalIgnoreCase);

            SessionStateScopeEnumerator scopeEnumerator =
                new SessionStateScopeEnumerator(_currentScope);

            foreach (SessionStateScope scope in scopeEnumerator)
            {
                foreach (AliasInfo entry in scope.AliasTable)
                {
                    if (!result.ContainsKey(entry.Name))
                    {
                        // Make sure the alias isn't private or if it is that the current
                        // scope is the same scope the alias was retrieved from.

                        if ((entry.Options & ScopedItemOptions.Private) == 0 ||
                            scope == _currentScope)
                        {
                            result.Add(entry.Name, entry);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets an IEnumerable for the alias table for a given scope.
        /// </summary>
        /// <param name="scopeID">
        /// A scope identifier that is either one of the "special" scopes like
        /// "global", "script", "local", or "private, or a numeric ID of a relative scope
        /// to the current scope.
        /// </param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="scopeID"/> is less than zero, or not
        /// a number and not "script", "global", "local", or "private"
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="scopeID"/> is less than zero or greater than the number of currently
        /// active scopes.
        /// </exception>
        internal IDictionary<string, AliasInfo> GetAliasTableAtScope(string scopeID)
        {
            Dictionary<string, AliasInfo> result =
                new Dictionary<string, AliasInfo>(StringComparer.OrdinalIgnoreCase);

            SessionStateScope scope = GetScopeByID(scopeID);

            foreach (AliasInfo entry in scope.AliasTable)
            {
                // Make sure the alias isn't private or if it is that the current
                // scope is the same scope the alias was retrieved from.

                if ((entry.Options & ScopedItemOptions.Private) == 0 ||
                    scope == _currentScope)
                {
                    result.Add(entry.Name, entry);
                }
            }

            return result;
        }

        /// <summary>
        /// List of aliases to export from this session state object...
        /// </summary>
        internal List<AliasInfo> ExportedAliases { get; } = new List<AliasInfo>();

        /// <summary>
        /// Gets the value of the specified alias from the alias table.
        /// </summary>
        /// <param name="aliasName">
        /// The name of the alias value to retrieve.
        /// </param>
        /// <param name="origin">
        /// The origin of the command calling this API.
        /// </param>
        /// <returns>
        /// The AliasInfo representing the alias.
        /// </returns>
        internal AliasInfo GetAlias(string aliasName, CommandOrigin origin)
        {
            AliasInfo result = null;
            if (string.IsNullOrEmpty(aliasName))
            {
                return null;
            }

            // Use the scope enumerator to find the alias using the
            // appropriate scoping rules

            SessionStateScopeEnumerator scopeEnumerator =
                new SessionStateScopeEnumerator(_currentScope);

            foreach (SessionStateScope scope in scopeEnumerator)
            {
                result = scope.GetAlias(aliasName);

                if (result != null)
                {
                    // Now check the visibility of the variable...
                    SessionState.ThrowIfNotVisible(origin, result);

                    // Make sure the alias isn't private or if it is that the current
                    // scope is the same scope the alias was retrieved from.

                    if ((result.Options & ScopedItemOptions.Private) != 0 &&
                        scope != _currentScope)
                    {
                        result = null;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the value of the specified alias from the alias table.
        /// </summary>
        /// <param name="aliasName">
        /// The name of the alias value to retrieve.
        /// </param>
        /// <returns>
        /// The AliasInfo representing the alias.
        /// </returns>
        internal AliasInfo GetAlias(string aliasName)
        {
            return GetAlias(aliasName, CommandOrigin.Internal);
        }

        /// <summary>
        /// Gets the value of the specified alias from the alias table.
        /// </summary>
        /// <param name="aliasName">
        /// The name of the alias value to retrieve.
        /// </param>
        /// <param name="scopeID">
        /// A scope identifier that is either one of the "special" scopes like
        /// "global", "script", "local", or "private, or a numeric ID of a relative scope
        /// to the current scope.
        /// </param>
        /// <returns>
        /// The AliasInfo representing the alias.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// If <paramref name="scopeID"/> is less than zero, or not
        /// a number and not "script", "global", "local", or "private"
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="scopeID"/> is less than zero or greater than the number of currently
        /// active scopes.
        /// </exception>
        internal AliasInfo GetAliasAtScope(string aliasName, string scopeID)
        {
            AliasInfo result = null;
            if (string.IsNullOrEmpty(aliasName))
            {
                return null;
            }

            SessionStateScope scope = GetScopeByID(scopeID);
            result = scope.GetAlias(aliasName);

            // Make sure the alias isn't private or if it is that the current
            // scope is the same scope the alias was retrieved from.

            if (result != null &&
                (result.Options & ScopedItemOptions.Private) != 0 &&
                 scope != _currentScope)
            {
                result = null;
            }

            return result;
        }

        /// <summary>
        /// Sets the alias with specified name to the specified value in the current scope.
        /// </summary>
        /// <param name="aliasName">
        /// The name of the alias to set.
        /// </param>
        /// <param name="value">
        /// The value to set the alias to.
        /// </param>
        /// <param name="force">
        /// If true, the value will be set even if the alias is ReadOnly.
        /// </param>
        /// <param name="origin">
        /// THe origin of the caller of this API
        /// </param>
        /// <returns>
        /// The resulting AliasInfo for the alias that was set.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// If <paramref name="aliasName"/> or <paramref name="value"/> is null or empty.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the alias is read-only or constant.
        /// </exception>
        internal AliasInfo SetAliasValue(string aliasName, string value, bool force, CommandOrigin origin)
        {
            if (string.IsNullOrEmpty(aliasName))
            {
                throw PSTraceSource.NewArgumentException("aliasName");
            }

            if (string.IsNullOrEmpty(value))
            {
                throw PSTraceSource.NewArgumentException("value");
            }

            AliasInfo info = _currentScope.SetAliasValue(aliasName, value, this.ExecutionContext, force, origin);

            return info;
        }

        /// <summary>
        /// Sets the alias with specified name to the specified value in the current scope.
        /// BUGBUG: this overload only exists for the test suites. They should be cleaned up
        /// and this overload removed.
        /// </summary>
        /// <param name="aliasName">
        /// The name of the alias to set.
        /// </param>
        /// <param name="value">
        /// The value to set the alias to.
        /// </param>
        /// <param name="force">
        /// If true, the value will be set even if the alias is ReadOnly.
        /// </param>
        /// <returns>
        /// The resulting AliasInfo for the alias that was set.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// If <paramref name="aliasName"/> or <paramref name="value"/> is null or empty.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the alias is read-only or constant.
        /// </exception>
        internal AliasInfo SetAliasValue(string aliasName, string value, bool force)
        {
            return SetAliasValue(aliasName, value, force, CommandOrigin.Internal);
        }

        /// <summary>
        /// Sets the alias with specified name to the specified value in the current scope.
        /// </summary>
        /// <param name="aliasName">
        /// The name of the alias to set.
        /// </param>
        /// <param name="value">
        /// The value to set the alias to.
        /// </param>
        /// <param name="options">
        /// The options to set on the alias.
        /// </param>
        /// <param name="force">
        /// If true, the value will be set even if the alias is ReadOnly.
        /// </param>
        /// <param name="origin">
        /// The origin of the caller of this API
        /// </param>
        /// <returns>
        /// The resulting AliasInfo for the alias that was set.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// If <paramref name="aliasName"/> or <paramref name="value"/> is null or empty.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the alias is read-only or constant.
        /// </exception>
        internal AliasInfo SetAliasValue(
            string aliasName,
            string value,
            ScopedItemOptions options,
            bool force,
            CommandOrigin origin)
        {
            if (string.IsNullOrEmpty(aliasName))
            {
                throw PSTraceSource.NewArgumentException("aliasName");
            }

            if (string.IsNullOrEmpty(value))
            {
                throw PSTraceSource.NewArgumentException("value");
            }

            AliasInfo info = _currentScope.SetAliasValue(aliasName, value, options, this.ExecutionContext, force, origin);

            return info;
        }

        /// <summary>
        /// Sets the alias with specified name to the specified value in the current scope.
        /// BUGBUG: this api only exists for the test suites. They should be fixed and it should be removed.
        /// </summary>
        /// <param name="aliasName">
        /// The name of the alias to set.
        /// </param>
        /// <param name="value">
        /// The value to set the alias to.
        /// </param>
        /// <param name="options">
        /// The options to set on the alias.
        /// </param>
        /// <param name="force">
        /// If true, the value will be set even if the alias is ReadOnly.
        /// </param>
        /// <returns>
        /// The resulting AliasInfo for the alias that was set.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// If <paramref name="aliasName"/> or <paramref name="value"/> is null or empty.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the alias is read-only or constant.
        /// </exception>
        internal AliasInfo SetAliasValue(
            string aliasName,
            string value,
            ScopedItemOptions options,
            bool force)
        {
            return SetAliasValue(aliasName, value, options, force, CommandOrigin.Internal);
        }

        /// <summary>
        /// Sets the alias with specified name to the specified value in the current scope.
        /// </summary>
        /// <param name="alias">
        /// The AliasInfo representing the alias.
        /// </param>
        /// <param name="force">
        /// If true, the alias will be set even if there is an existing ReadOnly
        /// alias.
        /// </param>
        /// <param name="origin">
        /// Specifies the origin of the command setting the alias.
        /// </param>
        /// <returns>
        /// The resulting AliasInfo for the alias that was set.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="alias"/> is null.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the alias is read-only or constant.
        /// </exception>
        internal AliasInfo SetAliasItem(AliasInfo alias, bool force, CommandOrigin origin)
        {
            if (alias == null)
            {
                throw PSTraceSource.NewArgumentNullException("alias");
            }

            AliasInfo info = _currentScope.SetAliasItem(alias, force, origin);

            return info;
        }

        /// <summary>
        /// Sets the alias with specified name to the specified value in the current scope.
        /// </summary>
        /// <param name="alias">
        /// The AliasInfo representing the alias.
        /// </param>
        /// <param name="scopeID">
        /// A scope identifier that is either one of the "special" scopes like
        /// "global", "script", "local", or "private, or a numeric ID of a relative scope
        /// to the current scope.
        /// </param>
        /// <param name="force">
        /// If true, the alias will be set even if there is an existing ReadOnly
        /// alias.
        /// </param>
        /// <param name="origin">
        /// Specifies the command origin of the calling command.
        /// </param>
        /// <returns>
        /// The resulting AliasInfo for the alias that was set.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// If <paramref name="scopeID"/> is less than zero, or not
        /// a number and not "script", "global", "local", or "private"
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="scopeID"/> is less than zero or greater than the number of currently
        /// active scopes.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="alias"/> is null.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the alias is read-only or constant.
        /// </exception>
        internal AliasInfo SetAliasItemAtScope(AliasInfo alias, string scopeID, bool force, CommandOrigin origin)
        {
            if (alias == null)
            {
                throw PSTraceSource.NewArgumentNullException("alias");
            }

            // If the "private" scope was specified, make sure the options contain
            // the Private flag

            if (string.Equals(scopeID, StringLiterals.Private, StringComparison.OrdinalIgnoreCase))
            {
                alias.Options |= ScopedItemOptions.Private;
            }

            SessionStateScope scope = GetScopeByID(scopeID);

            AliasInfo info = scope.SetAliasItem(alias, force, origin);

            return info;
        }

        /// <summary>
        /// Sets the alias with specified name to the specified value in the current scope.
        /// </summary>
        /// <param name="alias">
        /// The AliasInfo representing the alias.
        /// </param>
        /// <param name="scopeID">
        /// A scope identifier that is either one of the "special" scopes like
        /// "global", "script", "local", or "private, or a numeric ID of a relative scope
        /// to the current scope.
        /// </param>
        /// <param name="force">
        /// If true, the alias will be set even if there is an existing ReadOnly
        /// alias.
        /// </param>
        /// <returns>
        /// The resulting AliasInfo for the alias that was set.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// If <paramref name="scopeID"/> is less than zero, or not
        /// a number and not "script", "global", "local", or "private"
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="scopeID"/> is less than zero or greater than the number of currently
        /// active scopes.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="alias"/> is null.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the alias is read-only or constant.
        /// </exception>
        internal AliasInfo SetAliasItemAtScope(AliasInfo alias, string scopeID, bool force)
        {
            return SetAliasItemAtScope(alias, scopeID, force, CommandOrigin.Internal);
        }

        /// <summary>
        /// Removes the specified alias.
        /// </summary>
        /// <param name="aliasName">
        /// The name of the alias to remove.
        /// </param>
        /// <param name="force">
        /// If true the alias will be removed even if its ReadOnly.
        /// </param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="aliasName"/> is null or empty.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the alias is constant.
        /// </exception>
        internal void RemoveAlias(string aliasName, bool force)
        {
            if (string.IsNullOrEmpty(aliasName))
            {
                throw PSTraceSource.NewArgumentException("aliasName");
            }

            // Use the scope enumerator to find an existing function

            SessionStateScopeEnumerator scopeEnumerator =
                new SessionStateScopeEnumerator(_currentScope);

            foreach (SessionStateScope scope in scopeEnumerator)
            {
                AliasInfo alias =
                    scope.GetAlias(aliasName);

                if (alias != null)
                {
                    // Make sure the alias isn't private or if it is that the current
                    // scope is the same scope the alias was retrieved from.

                    if ((alias.Options & ScopedItemOptions.Private) != 0 &&
                        scope != _currentScope)
                    {
                        alias = null;
                    }
                    else
                    {
                        scope.RemoveAlias(aliasName, force);

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the aliases by command name (used by metadata-driven help)
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        internal IEnumerable<string> GetAliasesByCommandName(string command)
        {
            SessionStateScopeEnumerator scopeEnumerator =
                new SessionStateScopeEnumerator(_currentScope);

            foreach (SessionStateScope scope in scopeEnumerator)
            {
                foreach (string alias in scope.GetAliasesByCommandName(command))
                {
                    yield return alias;
                }
            }

            yield break;
        }

        #endregion aliases
    }
}

