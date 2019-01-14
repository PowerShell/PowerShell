// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    /// <summary>
    /// Holds the state of a Monad Shell session.
    /// </summary>
    internal sealed partial class SessionStateInternal
    {
        #region cmdlets

        /// <summary>
        /// Gets the value of the specified cmdlet from the cmdlet table.
        /// </summary>
        /// <param name="cmdletName">
        /// The name of the cmdlet value to retrieve.
        /// </param>
        /// <returns>
        /// The CmdletInfo representing the cmdlet.
        /// </returns>
        internal CmdletInfo GetCmdlet(string cmdletName)
        {
            return GetCmdlet(cmdletName, CommandOrigin.Internal);
        }

        /// <summary>
        /// Gets the value of the specified cmdlet from the cmdlet table.
        /// </summary>
        /// <param name="cmdletName">
        /// The name of the cmdlet value to retrieve.
        /// </param>
        /// <param name="origin">
        /// The origin of hte command trying to retrieve this cmdlet.
        /// </param>
        /// <returns>
        /// The CmdletInfo representing the cmdlet.
        /// </returns>
        internal CmdletInfo GetCmdlet(string cmdletName, CommandOrigin origin)
        {
            CmdletInfo result = null;
            if (string.IsNullOrEmpty(cmdletName))
            {
                return null;
            }

            // Use the scope enumerator to find the alias using the
            // appropriate scoping rules

            SessionStateScopeEnumerator scopeEnumerator =
                new SessionStateScopeEnumerator(_currentScope);

            foreach (SessionStateScope scope in scopeEnumerator)
            {
                result = scope.GetCmdlet(cmdletName);

                if (result != null)
                {
                    // Now check the visibility of the cmdlet...
                    SessionState.ThrowIfNotVisible(origin, result);

                    // Make sure the cmdlet isn't private or if it is that the current
                    // scope is the same scope the cmdlet was retrieved from.

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
        /// Gets the value of the specified cmdlet from the cmdlet table.
        /// </summary>
        /// <param name="cmdletName">
        /// The name of the cmdlet value to retrieve.
        /// </param>
        /// <param name="scopeID">
        /// A scope identifier that is either one of the "special" scopes like
        /// "global", "script", "local", or "private, or a numeric ID of a relative scope
        /// to the current scope.
        /// </param>
        /// <returns>
        /// The CmdletInfo representing the cmdlet.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// If <paramref name="scopeID"/> is less than zero, or not
        /// a number and not "script", "global", "local", or "private"
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="scopeID"/> is less than zero or greater than the number of currently
        /// active scopes.
        /// </exception>
        internal CmdletInfo GetCmdletAtScope(string cmdletName, string scopeID)
        {
            CmdletInfo result = null;
            if (string.IsNullOrEmpty(cmdletName))
            {
                return null;
            }

            SessionStateScope scope = GetScopeByID(scopeID);
            result = scope.GetCmdlet(cmdletName);

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
        /// Gets an IEnumerable for the cmdlet table.
        /// </summary>
        internal IDictionary<string, List<CmdletInfo>> GetCmdletTable()
        {
            Dictionary<string, List<CmdletInfo>> result =
                new Dictionary<string, List<CmdletInfo>>(StringComparer.OrdinalIgnoreCase);

            SessionStateScopeEnumerator scopeEnumerator =
                new SessionStateScopeEnumerator(_currentScope);

            foreach (SessionStateScope scope in scopeEnumerator)
            {
                foreach (KeyValuePair<string, List<CmdletInfo>> entry in scope.CmdletTable)
                {
                    if (!result.ContainsKey(entry.Key))
                    {
                        // Make sure the cmdlet isn't private or if it is that the current
                        // scope is the same scope the alias was retrieved from.

                        List<CmdletInfo> toBeAdded = new List<CmdletInfo>();
                        foreach (CmdletInfo cmdletInfo in entry.Value)
                        {
                            if ((cmdletInfo.Options & ScopedItemOptions.Private) == 0 ||
                                scope == _currentScope)
                            {
                                toBeAdded.Add(cmdletInfo);
                            }
                        }

                        result.Add(entry.Key, toBeAdded);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets an IEnumerable for the cmdlet table for a given scope.
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
        internal IDictionary<string, List<CmdletInfo>> GetCmdletTableAtScope(string scopeID)
        {
            Dictionary<string, List<CmdletInfo>> result =
                new Dictionary<string, List<CmdletInfo>>(StringComparer.OrdinalIgnoreCase);

            SessionStateScope scope = GetScopeByID(scopeID);

            foreach (KeyValuePair<string, List<CmdletInfo>> entry in scope.CmdletTable)
            {
                // Make sure the alias isn't private or if it is that the current
                // scope is the same scope the alias was retrieved from.
                List<CmdletInfo> toBeAdded = new List<CmdletInfo>();
                foreach (CmdletInfo cmdletInfo in entry.Value)
                {
                    if ((cmdletInfo.Options & ScopedItemOptions.Private) == 0 ||
                        scope == _currentScope)
                    {
                        toBeAdded.Add(cmdletInfo);
                    }
                }

                result.Add(entry.Key, toBeAdded);
            }

            return result;
        }

        internal void RemoveCmdlet(string name, int index, bool force)
        {
            RemoveCmdlet(name, index, force, CommandOrigin.Internal);
        }

        /// <summary>
        /// Removes a cmdlet from the function table.
        /// </summary>
        /// <param name="name">
        /// The name of the cmdlet to remove.
        /// </param>
        /// <param name="index">
        /// The name of the cmdlet to remove.
        /// </param>
        /// <param name="origin">
        /// THe origin of the caller of this API
        /// </param>
        /// <param name="force">
        /// If true, the cmdlet is removed even if it is ReadOnly.
        /// </param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="name"/> is null or empty.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the function is constant.
        /// </exception>
        internal void RemoveCmdlet(string name, int index, bool force, CommandOrigin origin)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException("name");
            }

            // Use the scope enumerator to find an existing function

            SessionStateScopeEnumerator scopeEnumerator =
                new SessionStateScopeEnumerator(_currentScope);

            foreach (SessionStateScope scope in scopeEnumerator)
            {
                CmdletInfo cmdletInfo =
                    scope.GetCmdlet(name);

                if (cmdletInfo != null)
                {
                    // Make sure the cmdlet isn't private or if it is that the current
                    // scope is the same scope the cmdlet was retrieved from.

                    if ((cmdletInfo.Options & ScopedItemOptions.Private) != 0 &&
                        scope != _currentScope)
                    {
                        cmdletInfo = null;
                    }
                    else
                    {
                        scope.RemoveCmdlet(name, index, force);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Removes a cmdlet entry from the cmdlet table.
        /// </summary>
        /// <param name="name">
        /// The name of the cmdlet entry to remove.
        /// </param>
        /// <param name="force">
        /// If true, the cmdlet is removed even if it is ReadOnly.
        /// </param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="name"/> is null or empty.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the function is constant.
        /// </exception>
        internal void RemoveCmdletEntry(string name, bool force)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException("name");
            }

            // Use the scope enumerator to find an existing function

            SessionStateScopeEnumerator scopeEnumerator =
                new SessionStateScopeEnumerator(_currentScope);

            foreach (SessionStateScope scope in scopeEnumerator)
            {
                CmdletInfo cmdletInfo =
                    scope.GetCmdlet(name);

                if (cmdletInfo != null)
                {
                    // Make sure the cmdlet isn't private or if it is that the current
                    // scope is the same scope the cmdlet was retrieved from.

                    if ((cmdletInfo.Options & ScopedItemOptions.Private) != 0 &&
                        scope != _currentScope)
                    {
                        cmdletInfo = null;
                    }
                    else
                    {
                        scope.RemoveCmdletEntry(name, force);
                        break;
                    }
                }
            }
        }

        #endregion cmdlets
    }
}

