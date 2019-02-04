// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation.Provider;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using Dbg = System.Management.Automation;

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings
#pragma warning disable 56500

namespace System.Management.Automation
{
    /// <summary>
    /// Holds the state of a Monad Shell session.
    /// </summary>
    internal sealed partial class SessionStateInternal
    {
        #region variables

        /// <summary>
        /// Add an new SessionStateVariable entry to this session state object...
        /// </summary>
        /// <param name="entry">The entry to add.</param>
        internal void AddSessionStateEntry(SessionStateVariableEntry entry)
        {
            PSVariable v = new PSVariable(entry.Name, entry.Value,
                    entry.Options, entry.Attributes, entry.Description);
            v.Visibility = entry.Visibility;
            this.SetVariableAtScope(v, "global", true, CommandOrigin.Internal);
        }

        /// <summary>
        /// Get a variable out of session state. This interface supports
        /// the scope specifiers like "global:foobar"
        /// </summary>
        /// <param name="name">
        /// name of variable to look up
        /// </param>
        /// <param name="origin">
        /// Origin of the command making this request.
        /// </param>
        /// <returns>
        /// The specified variable.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="name"/> is null.
        /// </exception>
        internal PSVariable GetVariable(string name, CommandOrigin origin)
        {
            if (name == null)
            {
                throw PSTraceSource.NewArgumentNullException("name");
            }

            VariablePath variablePath = new VariablePath(name, VariablePathFlags.Variable | VariablePathFlags.Unqualified);
            SessionStateScope scope = null;

            PSVariable resultItem = GetVariableItem(variablePath, out scope, origin);

            return resultItem;
        }

        /// <summary>
        /// Get a variable out of session state. This interface supports
        /// the scope specifiers like "global:foobar"
        /// </summary>
        /// <param name="name">
        /// name of variable to look up
        /// </param>
        /// <returns>
        /// The specified variable.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="name"/> is null.
        /// </exception>
        internal PSVariable GetVariable(string name)
        {
            return GetVariable(name, CommandOrigin.Internal);
        }

        /// <summary>
        /// Get a variable out of session state. This interface supports
        /// the "namespace:name" syntax so you can do things like
        /// "env:PATH" or "global:foobar"
        /// </summary>
        /// <param name="name">
        /// name of variable to look up
        /// </param>
        /// <returns>
        /// The value of the specified variable.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="name"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="name"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="name"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="name"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal object GetVariableValue(string name)
        {
            if (name == null)
            {
                throw PSTraceSource.NewArgumentNullException("name");
            }

            VariablePath variablePath = new VariablePath(name);
            CmdletProviderContext context = null;
            SessionStateScope scope = null;

            object resultItem = GetVariableValue(variablePath, out context, out scope);

            return resultItem;
        }

        /// <summary>
        /// Get a variable out of session state. This interface supports
        /// the "namespace:name" syntax so you can do things like
        /// "env:PATH" or "global:foobar"
        /// </summary>
        /// <param name="name">
        /// name of variable to look up
        /// </param>
        /// <param name="defaultValue">
        /// value to return if you can't find Name or it returns null.
        /// </param>
        /// <returns>
        /// The value of the specified variable.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="name"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="name"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="name"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="name"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal object GetVariableValue(string name, object defaultValue)
        {
            object returnObject = GetVariableValue(name) ?? defaultValue;
            return returnObject;
        }

        /// <summary>
        /// Looks up the specified variable and returns the context under which
        /// the variable was found as well as the variable itself.
        /// </summary>
        /// <param name="variablePath">
        /// The VariablePath helper for the variable.
        /// </param>
        /// <param name="scope">
        /// The scope the variable was found in. Null if the variable wasn't found.
        /// </param>
        /// <param name="context">
        /// Returns the context under which the variable was found. The context will
        /// have the drive data already set. This will be null if the variable was
        /// not found.
        /// </param>
        /// <returns>
        /// The variable if it was found or null if it was not.
        /// </returns>
        /// <remarks>
        /// The <paramref name="variablePath" /> is first parsed to see if it contains a drive
        /// specifier or special scope.  If a special scope is found ("LOCAL" or "GLOBAL")
        /// then only that scope is searched for the variable. If any other drive specifier
        /// is found the lookup goes in the following order.
        ///     - current scope
        ///     - each consecutive parent scope until the variable is found.
        ///     - global scope
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="variablePath"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="variablePath"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="variablePath"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="variablePath"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal object GetVariableValue(
            VariablePath variablePath,
            out CmdletProviderContext context,
            out SessionStateScope scope)
        {
            context = null;
            scope = null;

            object result = null;
            if (variablePath.IsVariable)
            {
                PSVariable variable = GetVariableItem(variablePath, out scope);
                if (variable != null)
                {
                    result = variable.Value;
                }
            }
            else
            {
                result = GetVariableValueFromProvider(variablePath, out context, out scope, _currentScope.ScopeOrigin);
            }

            return result;
        }

        /// <summary>
        /// Looks up the specified variable and returns the context under which
        /// the variable was found as well as the variable itself.
        /// </summary>
        /// <param name="variablePath">
        /// The VariablePath helper for the variable.
        /// </param>
        /// <param name="scope">
        /// The scope the variable was found in. Null if the variable wasn't found.
        /// </param>
        /// <param name="context">
        /// Returns the context under which the variable was found. The context will
        /// have the drive data already set. This will be null if the variable was
        /// not found.
        /// </param>
        /// <param name="origin">
        /// The origin of the caller of this API
        /// </param>
        /// <returns>
        /// The variable if it was found or null if it was not.
        /// </returns>
        /// <remarks>
        /// The <paramref name="variablePath" /> is first parsed to see if it contains a drive
        /// specifier or special scope.  If a special scope is found ("LOCAL" or "GLOBAL")
        /// then only that scope is searched for the variable. If any other drive specifier
        /// is found the lookup goes in the following order.
        ///     - current scope
        ///     - each consecutive parent scope until the variable is found.
        ///     - global scope
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="variablePath"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="variablePath"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="variablePath"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="variablePath"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
#pragma warning disable 0162
        internal object GetVariableValueFromProvider(
            VariablePath variablePath,
            out CmdletProviderContext context,
            out SessionStateScope scope,
            CommandOrigin origin)
        {
            scope = null;

            if (variablePath == null)
            {
                throw PSTraceSource.NewArgumentNullException("variablePath");
            }

            Dbg.Diagnostics.Assert(
                !variablePath.IsVariable,
                "This method can only be used to retrieve provider content");

            context = null;

            DriveScopeItemSearcher searcher =
                new DriveScopeItemSearcher(
                    this,
                    variablePath);

            object result = null;

            do // false loop
            {
                if (!searcher.MoveNext())
                {
                    break;
                }

                PSDriveInfo drive = ((IEnumerator<PSDriveInfo>)searcher).Current;

                if (drive == null)
                {
                    break;
                }

                // Create a new CmdletProviderContext and set the drive data

                context = new CmdletProviderContext(this.ExecutionContext, origin);

                context.Drive = drive;

#if true
                // PSVariable get/set is the get/set of content in the provider

                Collection<IContentReader> readers = null;

                try
                {
                    readers =
                        GetContentReader(new string[] { variablePath.QualifiedName }, context);
                }
                // If the item is not found we just return null like the normal
                // variable semantics.
                catch (ItemNotFoundException)
                {
                    break;
                }
                catch (DriveNotFoundException)
                {
                    break;
                }
                catch (ProviderNotFoundException)
                {
                    break;
                }
                catch (NotImplementedException notImplemented)
                {
                    // First get the provider for the path.

                    ProviderInfo providerInfo = null;
                    string unused =
                        this.Globber.GetProviderPath(variablePath.QualifiedName, out providerInfo);

                    throw NewProviderInvocationException(
                        "ProviderCannotBeUsedAsVariable",
                        SessionStateStrings.ProviderCannotBeUsedAsVariable,
                        providerInfo,
                        variablePath.QualifiedName,
                        notImplemented,
                        false);
                }
                catch (NotSupportedException notSupported)
                {
                    // First get the provider for the path.

                    ProviderInfo providerInfo = null;
                    string unused =
                        this.Globber.GetProviderPath(variablePath.QualifiedName, out providerInfo);

                    throw NewProviderInvocationException(
                        "ProviderCannotBeUsedAsVariable",
                        SessionStateStrings.ProviderCannotBeUsedAsVariable,
                        providerInfo,
                        variablePath.QualifiedName,
                        notSupported,
                        false);
                }

                if (readers == null || readers.Count == 0)
                {
                    // The drive was found but the path was wrong or something so return null.
                    // We don't want to continue searching if the provider didn't support content
                    // or the path wasn't found.
                    break;
                }

                if (readers.Count > 1)
                {
                    // Since more than one path was resolved, this is an error.

                    // Before throwing exception. Close the readers to avoid sharing violation.
                    foreach (IContentReader r in readers)
                    {
                        r.Close();
                    }

                    PSArgumentException argException =
                        PSTraceSource.NewArgumentException(
                            "path",
                            SessionStateStrings.VariablePathResolvedToMultiple,
                            variablePath.QualifiedName);

                    // First get the provider for the path.

                    ProviderInfo providerInfo = null;
                    string unused =
                        this.Globber.GetProviderPath(variablePath.QualifiedName, out providerInfo);

                    throw NewProviderInvocationException(
                        "ProviderVariableSyntaxInvalid",
                        SessionStateStrings.ProviderVariableSyntaxInvalid,
                        providerInfo,
                        variablePath.QualifiedName,
                        argException);
                }

                IContentReader reader = readers[0];

                try
                {
                    // Read all the content
                    IList resultList = reader.Read(-1);

                    if (resultList != null)
                    {
                        if (resultList.Count == 0)
                        {
                            result = null;
                        }
                        else if (resultList.Count == 1)
                        {
                            result = resultList[0];
                        }
                        else
                        {
                            result = resultList;
                        }
                    }
                }
                catch (Exception e) // Third-party callout, catch-all OK
                {
                    // First get the provider for the path.
                    ProviderInfo providerInfo = null;
                    string unused =
                        this.Globber.GetProviderPath(variablePath.QualifiedName, out providerInfo);

                    ProviderInvocationException providerException =
                        new ProviderInvocationException(
                            "ProviderContentReadError",
                            SessionStateStrings.ProviderContentReadError,
                            providerInfo,
                            variablePath.QualifiedName,
                            e);

                    throw providerException;
                }
                finally
                {
                    reader.Close();
                }

#else
                    try
                    {
                        GetItem(variablePath.LookupPath.ToString(), context);
                    }
                    catch (ItemNotFoundException)
                    {
                        break;
                    }

                    Collection<PSObject> items = context.GetAccumulatedObjects ();

                    if (items != null &&
                        items.Count > 0)
                    {
                        result = items[0];

                        if (!items[0].basObjectIsEmpty)
                        {
                            result = items[0].BaseObject;
                        }

                        try
                        {
                            DictionaryEntry entry = (DictionaryEntry)result;
                            result = entry.Value;
                        }
                            // Since DictionaryEntry is a value type we have to
                            // try the cast and catch the exception to determine
                            // if it is a DictionaryEntry type.
                        catch (InvalidCastException)
                        {
                        }
                    }

#endif
                break;
            } while (false);

            return result;
        }
#pragma warning restore 0162

        /// <summary>
        /// Looks up the specified variable and returns the context under which
        /// the variable was found as well as the variable itself.
        /// </summary>
        /// <param name="variablePath">
        /// The VariablePath helper for the variable.
        /// </param>
        /// <param name="scope">
        /// The scope the variable was found in. Null if the variable wasn't found.
        /// </param>
        /// <param name="origin">
        /// Origin of the command requesting this variable
        /// </param>
        /// <returns>
        /// The variable if it was found or null if it was not.
        /// </returns>
        /// <remarks>
        /// The <paramref name="variablePath" /> is first parsed to see if it contains a drive
        /// specifier or special scope.  If a special scope is found ("LOCAL" or "GLOBAL")
        /// then only that scope is searched for the variable.
        ///     - current scope
        ///     - each consecutive parent scope until the variable is found.
        ///     - global scope
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="variablePath"/> is null.
        /// </exception>
        internal PSVariable GetVariableItem(
            VariablePath variablePath,
            out SessionStateScope scope,
            CommandOrigin origin)
        {
            scope = null;

            if (variablePath == null)
            {
                throw PSTraceSource.NewArgumentNullException("variablePath");
            }

            Dbg.Diagnostics.Assert(variablePath.IsVariable, "Can't get variable w/ non-variable path");

            VariableScopeItemSearcher searcher =
                new VariableScopeItemSearcher(this, variablePath, origin);

            PSVariable result = null;

            if (searcher.MoveNext())
            {
                result = ((IEnumerator<PSVariable>)searcher).Current;
                scope = searcher.CurrentLookupScope;
            }

            return result;
        }

        /// <summary>
        /// Looks up the specified variable and returns the context under which
        /// the variable was found as well as the variable itself.
        /// </summary>
        /// <param name="variablePath">
        /// The VariablePath helper for the variable.
        /// </param>
        /// <param name="scope">
        /// The scope the variable was found in. Null if the variable wasn't found.
        /// </param>
        /// <returns>
        /// The variable if it was found or null if it was not.
        /// </returns>
        /// <remarks>
        /// The <paramref name="variablePath" /> is first parsed to see if it contains a drive
        /// specifier or special scope.  If a special scope is found ("LOCAL" or "GLOBAL")
        /// then only that scope is searched for the variable.
        ///     - current scope
        ///     - each consecutive parent scope until the variable is found.
        ///     - global scope
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="variablePath"/> is null.
        /// </exception>
        internal PSVariable GetVariableItem(
            VariablePath variablePath,
            out SessionStateScope scope)
        {
            return GetVariableItem(variablePath, out scope, CommandOrigin.Internal);
        }

        /// <summary>
        /// Get a variable out of session state. This interface supports
        /// the "namespace:name" syntax so you can do things like
        /// "env:PATH" or "global:foobar"
        /// </summary>
        /// <param name="name">
        /// name of variable to look up
        /// </param>
        /// <param name="scopeID">
        /// The ID of the scope to lookup the variable in.
        /// </param>
        /// <returns>
        /// The value of the specified variable.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="name"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="scopeID"/> is less than zero, or not
        /// a number and not "script", "global", "local", or "private"
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="scopeID"/> is less than zero or greater than the number of currently
        /// active scopes.
        /// </exception>
        internal PSVariable GetVariableAtScope(string name, string scopeID)
        {
            if (name == null)
            {
                throw PSTraceSource.NewArgumentNullException("name");
            }

            VariablePath variablePath = new VariablePath(name);

            SessionStateScope lookupScope = null;

            // The lookup scope from above is ignored and the scope is retrieved by
            // ID.

            lookupScope = GetScopeByID(scopeID);

            PSVariable resultItem = null;

            if (variablePath.IsVariable)
            {
                resultItem = lookupScope.GetVariable(variablePath.QualifiedName);
            }

            return resultItem;
        }

        /// <summary>
        /// Get a variable out of session state. This interface supports
        /// the "namespace:name" syntax so you can do things like
        /// "env:PATH" or "global:foobar"
        /// </summary>
        /// <param name="name">
        /// name of variable to look up
        /// </param>
        /// <param name="scopeID">
        /// The ID of the scope to lookup the variable in.
        /// </param>
        /// <returns>
        /// The value of the specified variable.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="name"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="scopeID"/> is less than zero, or not
        /// a number and not "script", "global", "local", or "private"
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="scopeID"/> is less than zero or greater than the number of currently
        /// active scopes.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="name"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="name"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="name"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal object GetVariableValueAtScope(string name, string scopeID)
        {
            if (name == null)
            {
                throw PSTraceSource.NewArgumentNullException("name");
            }

            VariablePath variablePath = new VariablePath(name);

            SessionStateScope lookupScope = null;

            // The lookup scope from above is ignored and the scope is retrieved by
            // ID.

            lookupScope = GetScopeByID(scopeID);

            object resultItem = null;

            if (variablePath.IsVariable)
            {
                resultItem = lookupScope.GetVariable(variablePath.QualifiedName);
            }
            else
            {
                PSDriveInfo drive = lookupScope.GetDrive(variablePath.DriveName);

                if (drive != null)
                {
                    CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
                    context.Drive = drive;

#if true
                    // PSVariable get/set is the get/set of content in the provider

                    Collection<IContentReader> readers = null;

                    try
                    {
                        readers =
                            GetContentReader(new string[] { variablePath.QualifiedName }, context);
                    }
                    // If the item is not found we just return null like the normal
                    // variable semantics.
                    catch (ItemNotFoundException)
                    {
                        return null;
                    }
                    catch (DriveNotFoundException)
                    {
                        return null;
                    }
                    catch (ProviderNotFoundException)
                    {
                        return null;
                    }
                    catch (NotImplementedException notImplemented)
                    {
                        // First get the provider for the path.

                        ProviderInfo providerInfo = null;
                        string unused =
                            this.Globber.GetProviderPath(variablePath.QualifiedName, out providerInfo);

                        throw NewProviderInvocationException(
                            "ProviderCannotBeUsedAsVariable",
                            SessionStateStrings.ProviderCannotBeUsedAsVariable,
                            providerInfo,
                            variablePath.QualifiedName,
                            notImplemented,
                            false);
                    }
                    catch (NotSupportedException notSupported)
                    {
                        // First get the provider for the path.

                        ProviderInfo providerInfo = null;
                        string unused =
                            this.Globber.GetProviderPath(variablePath.QualifiedName, out providerInfo);

                        throw NewProviderInvocationException(
                            "ProviderCannotBeUsedAsVariable",
                            SessionStateStrings.ProviderCannotBeUsedAsVariable,
                            providerInfo,
                            variablePath.QualifiedName,
                            notSupported,
                            false);
                    }

                    if (readers == null || readers.Count == 0)
                    {
                        // The drive was found but the path was wrong or something so return null.
                        // We don't want to continue searching if the provider didn't support content
                        // or the path wasn't found.
                        // Any errors should have been written to the error pipeline.
                        return null;
                    }

                    if (readers.Count > 1)
                    {
                        foreach (IContentReader closeReader in readers)
                        {
                            closeReader.Close();
                        }

                        // Since more than one path was resolved, this is an error.

                        PSArgumentException argException =
                            PSTraceSource.NewArgumentException(
                                "path",
                                SessionStateStrings.VariablePathResolvedToMultiple,
                                name);

                        // First get the provider for the path.

                        ProviderInfo providerInfo = null;
                        string unused =
                            this.Globber.GetProviderPath(variablePath.QualifiedName, out providerInfo);

                        throw NewProviderInvocationException(
                            "ProviderVariableSyntaxInvalid",
                            SessionStateStrings.ProviderVariableSyntaxInvalid,
                            providerInfo,
                            variablePath.QualifiedName,
                            argException);
                    }

                    IContentReader reader = readers[0];

                    try
                    {
                        // Read all the content
                        IList resultList = reader.Read(-1);

                        if (resultList != null)
                        {
                            if (resultList.Count == 0)
                            {
                                resultItem = null;
                            }
                            else if (resultList.Count == 1)
                            {
                                resultItem = resultList[0];
                            }
                            else
                            {
                                resultItem = resultList;
                            }
                        }
                    }
                    catch (Exception e) // Third-party callout, catch-all OK
                    {
                        // First get the provider for the path.

                        ProviderInfo providerInfo = null;
                        string unused =
                            this.Globber.GetProviderPath(variablePath.QualifiedName, out providerInfo);

                        ProviderInvocationException providerException =
                            new ProviderInvocationException(
                                "ProviderContentReadError",
                                SessionStateStrings.ProviderContentReadError,
                                providerInfo,
                                variablePath.QualifiedName,
                                e);

                        throw providerException;
                    }
                    finally
                    {
                        reader.Close();
                    }
#else
                        GetItem (variablePath.LookupPath.ToString (), context);

                        Collection<PSObject> results = context.GetAccumulatedObjects ();

                        if (results != null &
                            results.Count > 0)
                        {
                            // Only return the first value. If the caller wants globbing
                            // they need to call the GetItem method directly.

                            if (!results[0].basObjectIsEmpty)
                            {
                                resultItem = results[0].BaseObject;
                            }
                            else
                            {
                                resultItem = results[0];
                            }
                        }
#endif
                }
            }

            // If we get a PSVariable or DictionaryEntry returned then we have to
            // grab the value from it and return that instead.

            if (resultItem != null)
            {
                PSVariable variable = resultItem as PSVariable;

                if (variable != null)
                {
                    resultItem = variable.Value;
                }
                else
                {
                    try
                    {
                        DictionaryEntry entry = (DictionaryEntry)resultItem;
                        resultItem = entry.Value;
                    }
                    catch (InvalidCastException)
                    {
                    }
                }
            }

            return resultItem;
        }

        internal object GetAutomaticVariableValue(AutomaticVariable variable)
        {
            var scopeEnumerator = new SessionStateScopeEnumerator(CurrentScope);
            object result = AutomationNull.Value;
            foreach (var scope in scopeEnumerator)
            {
                result = scope.GetAutomaticVariableValue(variable);
                if (result != AutomationNull.Value)
                {
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Set a variable in session state. This interface supports
        /// the "namespace:name" syntax so you can do things like
        /// "$env:PATH = 'c:\windows'" or "$global:foobar = 13"
        /// </summary>
        /// <param name="name">
        /// The name of the item to set.
        /// </param>
        /// <param name="newValue">
        /// The new value of the item being set.
        /// </param>
        /// <param name="origin">
        /// The origin of the caller of this API...
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="name"/> is null.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the variable is read-only or constant.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="name"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="name"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="name"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal void SetVariableValue(string name, object newValue, CommandOrigin origin)
        {
            if (name == null)
            {
                throw PSTraceSource.NewArgumentNullException("name");
            }

            VariablePath variablePath = new VariablePath(name);

            SetVariable(variablePath, newValue, true, origin);
        }

        /// <summary>
        /// Set a variable in session state. This interface supports
        /// the "namespace:name" syntax so you can do things like
        /// "$env:PATH = 'c:\windows'" or "$global:foobar = 13"
        ///
        /// BUGBUG: this overload exists because a lot of tests in the
        /// testsuite use it. Those tests should eventually be fixed and this overload
        /// should be removed.
        /// </summary>
        /// <param name="name">
        /// The name of the item to set.
        /// </param>
        /// <param name="newValue">
        /// The new value of the item being set.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="name"/> is null.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the variable is read-only or constant.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="name"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="name"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="name"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal void SetVariableValue(string name, object newValue)
        {
            SetVariableValue(name, newValue, CommandOrigin.Internal);
        }

        /// <summary>
        /// Set a variable in session state. This interface supports
        /// the scope specifiers like "$global:foobar = 13"
        /// </summary>
        /// <param name="variable">
        /// The variable to be set.
        /// </param>
        /// <param name="force">
        /// If true, the variable is set even if it is ReadOnly.
        /// </param>
        /// <param name="origin">
        /// The origin of the caller of this API
        /// </param>
        /// <returns>
        /// A PSVariable object if <paramref name="variablePath"/> refers to a variable.
        /// An PSObject if <paramref name="variablePath"/> refers to a provider path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="variable"/> is null.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the variable is read-only or constant.
        /// </exception>
        internal object SetVariable(PSVariable variable, bool force, CommandOrigin origin)
        {
            if (variable == null || string.IsNullOrEmpty(variable.Name))
            {
                throw PSTraceSource.NewArgumentException("variable");
            }

            VariablePath variablePath = new VariablePath(variable.Name, VariablePathFlags.Variable | VariablePathFlags.Unqualified);

            return SetVariable(variablePath, variable, false, force, origin);
        }

        /// <summary>
        /// Set a variable using a pre-parsed variablePath object instead of a string.
        /// </summary>
        /// <param name="variablePath">
        /// A pre-parsed variable path object for the variable in question.
        /// </param>
        /// <param name="newValue">
        /// The value to set.
        /// </param>
        /// <param name="asValue">
        /// If true, sets the variable value to newValue. If false, newValue must
        /// be a PSVariable object and the item will be set rather than the value.
        /// </param>
        /// <param name="origin">
        /// The origin of the caller
        /// </param>
        /// <returns>
        /// A PSVariable object if <paramref name="variablePath"/> refers to a variable.
        /// An PSObject if <paramref name="variablePath"/> refers to a provider path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="variablePath"/> is null.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the variable is read-only or constant.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="variablePath"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="variablePath"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="variablePath"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal object SetVariable(
            VariablePath variablePath,
            object newValue,
            bool asValue,
            CommandOrigin origin)
        {
            return SetVariable(variablePath, newValue, asValue, false, origin);
        }

        /// <summary>
        /// Set a variable using a pre-parsed variablePath object instead of a string.
        /// </summary>
        /// <param name="variablePath">
        /// A pre-parsed variable path object for the variable in question.
        /// </param>
        /// <param name="newValue">
        /// The value to set.
        /// </param>
        /// <param name="asValue">
        /// If true, sets the variable value to newValue. If false, newValue must
        /// be a PSVariable object and the item will be set rather than the value.
        /// </param>
        /// <param name="force">
        /// If true, the variable is set even if it is ReadOnly.
        /// </param>
        /// <param name="origin">
        /// The origin of the caller
        /// </param>
        /// <returns>
        /// A PSVariable object if <paramref name="variablePath"/> refers to a variable.
        /// An PSObject if <paramref name="variablePath"/> refers to a provider path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="variablePath"/> is null.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the variable is read-only or constant.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="variablePath"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="variablePath"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="variablePath"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal object SetVariable(
            VariablePath variablePath,
            object newValue,
            bool asValue,
            bool force,
            CommandOrigin origin)
        {
            object result = null;
            if (variablePath == null)
            {
                throw PSTraceSource.NewArgumentNullException("variablePath");
            }

            CmdletProviderContext context = null;
            SessionStateScope scope = null;

            if (variablePath.IsVariable)
            {
                // Make sure to set the variable in the appropriate scope

                if (variablePath.IsLocal || variablePath.IsUnscopedVariable)
                {
                    scope = _currentScope;
                }
                else if (variablePath.IsScript)
                {
                    scope = _currentScope.ScriptScope;
                }
                else if (variablePath.IsGlobal)
                {
                    scope = GlobalScope;
                }
                else if (variablePath.IsPrivate)
                {
                    scope = _currentScope;
                }

                PSVariable varResult =
                    scope.SetVariable(
                        variablePath.QualifiedName,
                        newValue,
                        asValue,
                        force,
                        this,
                        origin);

                // If the name is scoped as private we need to mark the
                // variable as private

                if (variablePath.IsPrivate && varResult != null)
                {
                    varResult.Options = varResult.Options | ScopedItemOptions.Private;
                }

                result = varResult;
            }
            else
            {
                // Use GetVariable to get the correct context for the set operation.

                // NTRAID#Windows OS Bugs-896768-2004/07/06-JeffJon
                // There is probably a more efficient way to do this.

                GetVariableValue(variablePath, out context, out scope);
#if true
                // PSVariable get/set is the get/set of content in the provider

                Collection<IContentWriter> writers = null;

                try
                {
                    if (context != null)
                    {
                        try
                        {
                            CmdletProviderContext clearContentContext = new CmdletProviderContext(context);

                            // First clear the content if it is supported.
                            ClearContent(new string[] { variablePath.QualifiedName }, clearContentContext);
                        }
                        catch (NotSupportedException)
                        {
                        }
                        catch (ItemNotFoundException)
                        {
                        }

                        writers =
                            GetContentWriter(
                                new string[] { variablePath.QualifiedName },
                                context);
                        context.ThrowFirstErrorOrDoNothing(true);
                    }
                    else
                    {
                        try
                        {
                            // First clear the content if it is supported.
                            ClearContent(new string[] { variablePath.QualifiedName }, false, false);
                        }
                        catch (NotSupportedException)
                        {
                        }
                        catch (ItemNotFoundException)
                        {
                        }

                        writers =
                            GetContentWriter(
                                new string[] { variablePath.QualifiedName }, false, false);
                    }
                }
                catch (NotImplementedException notImplemented)
                {
                    // First get the provider for the path.

                    ProviderInfo providerInfo = null;
                    string unused =
                        this.Globber.GetProviderPath(variablePath.QualifiedName, out providerInfo);

                    throw NewProviderInvocationException(
                        "ProviderCannotBeUsedAsVariable",
                        SessionStateStrings.ProviderCannotBeUsedAsVariable,
                        providerInfo,
                        variablePath.QualifiedName,
                        notImplemented,
                        false);
                }
                catch (NotSupportedException notSupported)
                {
                    // First get the provider for the path.

                    ProviderInfo providerInfo = null;
                    string unused =
                        this.Globber.GetProviderPath(variablePath.QualifiedName, out providerInfo);

                    throw NewProviderInvocationException(
                        "ProviderCannotBeUsedAsVariable",
                        SessionStateStrings.ProviderCannotBeUsedAsVariable,
                        providerInfo,
                        variablePath.QualifiedName,
                        notSupported,
                        false);
                }

                if (writers == null || writers.Count == 0)
                {
                    ItemNotFoundException itemNotFound =
                        new ItemNotFoundException(
                            variablePath.QualifiedName,
                            "PathNotFound",
                            SessionStateStrings.PathNotFound);

                    throw itemNotFound;
                }

                if (writers.Count > 1)
                {
                    // Since more than one path was resolved, this is an error.
                    foreach (IContentWriter w in writers)
                    {
                        w.Close();
                    }

                    PSArgumentException argException =
                        PSTraceSource.NewArgumentException(
                            "path",
                            SessionStateStrings.VariablePathResolvedToMultiple,
                            variablePath.QualifiedName);

                    // First get the provider for the path.

                    ProviderInfo providerInfo = null;
                    string unused =
                        this.Globber.GetProviderPath(variablePath.QualifiedName, out providerInfo);

                    throw NewProviderInvocationException(
                        "ProviderVariableSyntaxInvalid",
                        SessionStateStrings.ProviderVariableSyntaxInvalid,
                        providerInfo,
                        variablePath.QualifiedName,
                        argException);
                }

                IContentWriter writer = writers[0];

                IList content = newValue as IList ?? new object[] { newValue };

                try
                {
                    writer.Write(content);
                }
                catch (Exception e) // Third-party callout, catch-all OK
                {
                    // First get the provider for the path.

                    ProviderInfo providerInfo = null;
                    string unused =
                        this.Globber.GetProviderPath(variablePath.QualifiedName, out providerInfo);

                    ProviderInvocationException providerException =
                        new ProviderInvocationException(
                            "ProviderContentWriteError",
                            SessionStateStrings.ProviderContentWriteError,
                            providerInfo,
                            variablePath.QualifiedName,
                            e);

                    throw providerException;
                }
                finally
                {
                    writer.Close();
                }
#else
                    if (context != null)
                    {
                        context.Force = force;
                        SetItem (variablePath.LookupPath.ToString (), newValue, context);

                        context.ThrowFirstErrorOrDoNothing(true);
                    }
                    else
                    {
                        Collection<PSObject> setItemResult =
                            SetItem (variablePath.LookupPath.ToString (), newValue);

                        if (setItemResult != null &&
                            setItemResult.Count > 0)
                        {
                            result = setItemResult[0];
                        }
                    }
#endif
            }

            return result;
        }

        /// <summary>
        /// Set a variable in session state.
        /// </summary>
        /// <param name="variable">
        /// The variable to set
        /// </param>
        /// <param name="scopeID">
        /// The ID of the scope to do the lookup in. The ID is either a zero based index
        /// of the scope tree with the current scope being zero, its parent scope
        /// being 1 and so on, or "global", "local", "private", or "script"
        /// </param>
        /// <param name="force">
        /// If true, the variable is set even if it is ReadOnly.
        /// </param>
        /// <param name="origin">
        /// The origin of the caller
        /// </param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="variable"/> is null or its name is null or empty.
        /// or
        /// If <paramref name="scopeID"/> is less than zero, or not
        /// a number and not "script", "global", "local", or "private"
        /// </exception>
        /// <returns>
        /// A PSVariable object if <paramref name="variable"/> refers to a variable.
        /// An PSObject if <paramref name="variable"/> refers to a provider path.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="scopeID"/> is less than zero or greater than the number of currently
        /// active scopes.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the variable is read-only or constant.
        /// </exception>
        internal object SetVariableAtScope(PSVariable variable, string scopeID, bool force, CommandOrigin origin)
        {
            if (variable == null || string.IsNullOrEmpty(variable.Name))
            {
                throw PSTraceSource.NewArgumentException("variable");
            }

            SessionStateScope lookupScope = GetScopeByID(scopeID);

            return
                lookupScope.SetVariable(
                    variable.Name,
                    variable,
                    false,
                    force,
                    this,
                    origin);
        }

        #region NewVariable

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        /// <param name="variable">
        /// The variable to create
        /// </param>
        /// <param name="force">
        /// If true, the variable is created even if it is ReadOnly.
        /// </param>
        /// <returns>
        /// A PSVariable representing the variable that was created.
        /// </returns>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the variable is read-only or constant.
        /// </exception>
        internal object NewVariable(PSVariable variable, bool force)
        {
            if (variable == null || string.IsNullOrEmpty(variable.Name))
            {
                throw PSTraceSource.NewArgumentException("variable");
            }

            return
                this.CurrentScope.NewVariable(
                    variable,
                    force,
                    this);
        }

        /// <summary>
        /// Creates a new variable in the specified scope.
        /// </summary>
        /// <param name="variable">
        /// The variable to create
        /// </param>
        /// <param name="scopeID">
        /// The ID of the scope to do the lookup in. The ID is either a zero based index
        /// of the scope tree with the current scope being zero, its parent scope
        /// being 1 and so on, or "global", "local", "private", or "script"
        /// </param>
        /// <param name="force">
        /// If true, the variable is set even if it is ReadOnly.
        /// </param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="variable"/> is null or its name is null or empty.
        /// or
        /// If <paramref name="scopeID"/> is less than zero, or not
        /// a number and not "script", "global", "local", or "private"
        /// </exception>
        /// <returns>
        /// A PSVariable representing the variable that was created.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="scopeID"/> is less than zero or greater than the number of currently
        /// active scopes.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the variable is read-only or constant.
        /// </exception>
        internal object NewVariableAtScope(PSVariable variable, string scopeID, bool force)
        {
            if (variable == null || string.IsNullOrEmpty(variable.Name))
            {
                throw PSTraceSource.NewArgumentException("variable");
            }

            // The lookup scope from above is ignored and the scope is retrieved by
            // ID.

            SessionStateScope lookupScope = GetScopeByID(scopeID);

            return
                lookupScope.NewVariable(
                    variable,
                    force,
                    this);
        }

        #endregion NewVariable

        /// <summary>
        /// Removes a variable from the variable table.
        /// </summary>
        /// <param name="name">
        /// The name of the variable to remove.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="name"/> is null.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// if the variable is constant.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="name"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="name"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="name"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal void RemoveVariable(string name)
        {
            RemoveVariable(name, false);
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
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="name"/> is null.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// if the variable is constant.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="name"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="name"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="name"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal void RemoveVariable(string name, bool force)
        {
            if (name == null)
            {
                throw PSTraceSource.NewArgumentNullException("name");
            }

            VariablePath variablePath = new VariablePath(name);
            SessionStateScope scope = null;

            if (variablePath.IsVariable)
            {
                if (GetVariableItem(variablePath, out scope) != null)
                {
                    scope.RemoveVariable(variablePath.QualifiedName, force);
                }
            }
            else
            {
                CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
                context.Force = force;

                RemoveItem(new string[] { variablePath.QualifiedName }, false, context);
                context.ThrowFirstErrorOrDoNothing();
            }
        }

        /// <summary>
        /// Removes a variable from the variable table.
        /// </summary>
        /// <param name="variable">
        /// The variable to remove.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="variable"/> is null.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// if the variable is constant.
        /// </exception>
        internal void RemoveVariable(PSVariable variable)
        {
            RemoveVariable(variable, false);
        }

        /// <summary>
        /// Removes a variable from the variable table.
        /// </summary>
        /// <param name="variable">
        /// The variable to remove.
        /// </param>
        /// <param name="force">
        /// If true, the variable will be removed even if its ReadOnly.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="variable"/> is null.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// if the variable is constant.
        /// </exception>
        internal void RemoveVariable(PSVariable variable, bool force)
        {
            if (variable == null)
            {
                throw PSTraceSource.NewArgumentNullException("variable");
            }

            VariablePath variablePath = new VariablePath(variable.Name);

            SessionStateScope scope = null;

            if (GetVariableItem(variablePath, out scope) != null)
            {
                scope.RemoveVariable(variablePath.QualifiedName, force);
            }
        }

        /// <summary>
        /// Remove a variable from session state. This interface supports
        /// the "namespace:name" syntax so you can do things like
        /// "env:PATH" or "global:foobar"
        /// </summary>
        /// <param name="name">
        /// name of variable to remove
        /// </param>
        /// <param name="scopeID">
        /// The ID of the scope to lookup the variable in.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="name"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="scopeID"/> is less than zero or greater than the number of currently
        /// active scopes.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// if the variable is constant.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If <paramref name="name"/> refers to an MSH path (not a variable)
        /// and the provider throws an exception.
        /// </exception>
        internal void RemoveVariableAtScope(string name, string scopeID)
        {
            RemoveVariableAtScope(name, scopeID, false);
        }

        /// <summary>
        /// Remove a variable from session state. This interface supports
        /// the "namespace:name" syntax so you can do things like
        /// "env:PATH" or "global:foobar"
        /// </summary>
        /// <param name="name">
        /// name of variable to remove
        /// </param>
        /// <param name="scopeID">
        /// The ID of the scope to lookup the variable in.
        /// </param>
        /// <param name="force">
        /// If true, the variable will be removed even if its ReadOnly.
        /// </param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="name"/> is null or empty.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="scopeID"/> is less than zero or greater than the number of currently
        /// active scopes.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// if the variable is constant.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If <paramref name="name"/> refers to an MSH path (not a variable)
        /// and the provider throws an exception.
        /// </exception>
        internal void RemoveVariableAtScope(string name, string scopeID, bool force)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException("name");
            }

            VariablePath variablePath = new VariablePath(name);

            SessionStateScope lookupScope = null;

            // The lookup scope from above is ignored and the scope is retrieved by
            // ID.

            lookupScope = GetScopeByID(scopeID);

            if (variablePath.IsVariable)
            {
                lookupScope.RemoveVariable(variablePath.QualifiedName, force);
            }
            else
            {
                PSDriveInfo drive = lookupScope.GetDrive(variablePath.DriveName);

                if (drive != null)
                {
                    CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
                    context.Drive = drive;
                    context.Force = force;

                    RemoveItem(new string[] { variablePath.QualifiedName }, false, context);
                    context.ThrowFirstErrorOrDoNothing();
                }
            }
        }

        /// <summary>
        /// Remove a variable from session state.
        /// </summary>
        /// <param name="variable">
        /// The variable to remove
        /// </param>
        /// <param name="scopeID">
        /// The ID of the scope to lookup the variable in.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="variable"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="scopeID"/> is less than zero or greater than the number of currently
        /// active scopes.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// if the variable is constant.
        /// </exception>
        internal void RemoveVariableAtScope(PSVariable variable, string scopeID)
        {
            RemoveVariableAtScope(variable, scopeID, false);
        }

        /// <summary>
        /// Remove a variable from session state.
        /// </summary>
        /// <param name="variable">
        /// The variable to remove
        /// </param>
        /// <param name="scopeID">
        /// The ID of the scope to lookup the variable in.
        /// </param>
        /// <param name="force">
        /// If true, the variable will be removed even if its ReadOnly.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="variable"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="scopeID"/> is less than zero or greater than the number of currently
        /// active scopes.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// if the variable is constant.
        /// </exception>
        internal void RemoveVariableAtScope(PSVariable variable, string scopeID, bool force)
        {
            if (variable == null)
            {
                throw PSTraceSource.NewArgumentNullException("variable");
            }

            VariablePath variablePath = new VariablePath(variable.Name);

            // The lookup scope is retrieved by ID.

            SessionStateScope lookupScope = GetScopeByID(scopeID);

            lookupScope.RemoveVariable(variablePath.QualifiedName, force);
        }

        /// <summary>
        /// Gets a flattened view of the variables that are visible using
        /// the current scope as a reference and filtering the variables in
        /// the other scopes based on the scoping rules.
        /// </summary>
        /// <returns>
        /// An IDictionary representing the visible variables.
        /// </returns>
        internal IDictionary<string, PSVariable> GetVariableTable()
        {
            SessionStateScopeEnumerator scopeEnumerator =
                new SessionStateScopeEnumerator(_currentScope);

            Dictionary<string, PSVariable> result =
                new Dictionary<string, PSVariable>(StringComparer.OrdinalIgnoreCase);

            foreach (SessionStateScope scope in scopeEnumerator)
            {
                GetScopeVariableTable(scope, result, includePrivate: scope == _currentScope);
            }

            return result;
        }

        private void GetScopeVariableTable(SessionStateScope scope, Dictionary<string, PSVariable> result, bool includePrivate)
        {
            foreach (KeyValuePair<string, PSVariable> entry in scope.Variables)
            {
                if (!result.ContainsKey(entry.Key))
                {
                    // Also check to ensure that the variable isn't private
                    // and in a different scope

                    PSVariable var = entry.Value;

                    if (!var.IsPrivate || includePrivate)
                    {
                        result.Add(entry.Key, var);
                    }
                }
            }

            foreach (var dottedScope in scope.DottedScopes)
            {
                dottedScope.GetVariableTable(result, includePrivate);
            }

            if (scope.LocalsTuple != null)
            {
                scope.LocalsTuple.GetVariableTable(result, includePrivate);
            }
        }

        /// <summary>
        /// Gets a flattened view of the variables that are visible using
        /// the current scope as a reference and filtering the variables in
        /// the other scopes based on the scoping rules.
        /// </summary>
        /// <returns>
        /// An IDictionary representing the visible variables.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// If <paramref name="scopeID"/> is less than zero, or not
        /// a number and not "script", "global", "local", or "private"
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="scopeID"/> is less than zero or greater than the number of currently
        /// active scopes.
        /// </exception>
        internal IDictionary<string, PSVariable> GetVariableTableAtScope(string scopeID)
        {
            var result = new Dictionary<string, PSVariable>(StringComparer.OrdinalIgnoreCase);
            GetScopeVariableTable(GetScopeByID(scopeID), result, includePrivate: true);
            return result;
        }

        /// <summary>
        /// List of variables to export from this session state object...
        /// </summary>
        internal List<PSVariable> ExportedVariables { get; } = new List<PSVariable>();

        #endregion variables
    }
}

#pragma warning restore 56500
