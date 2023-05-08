// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable 1634, 1691
#pragma warning disable 56506

using System;
using Dbg = System.Management.Automation;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Security;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This is the base class for all the providers that produce a view
    /// on session state data (Variables, Aliases, and Functions)
    /// </summary>
    public abstract class SessionStateProviderBase : ContainerCmdletProvider, IContentCmdletProvider
    {
        #region tracer

        /// <summary>
        /// An instance of the PSTraceSource class used for trace output.
        /// </summary>
        [Dbg.TraceSourceAttribute(
             "SessionStateProvider",
             "Providers that produce a view of session state data.")]
        private static readonly Dbg.PSTraceSource s_tracer =
            Dbg.PSTraceSource.GetTracer("SessionStateProvider",
             "Providers that produce a view of session state data.");

        #endregion tracer

        #region protected members

        /// <summary>
        /// Derived classes must override to get items from session state.
        /// </summary>
        /// <param name="name">
        /// The name of the item to get.
        /// </param>
        /// <returns>
        /// The item of the given name in the appropriate session state table.
        /// </returns>
        internal abstract object GetSessionStateItem(string name);

        /// <summary>
        /// Sets a session state item in the appropriate session state table.
        /// Derived classes must override this method to set the item in the
        /// proper table.
        /// </summary>
        /// <param name="name">
        /// The name of the item to set.
        /// </param>
        /// <param name="value">
        /// The new value for the item.
        /// </param>
        /// <param name="writeItem">
        /// If true, the item that was set should be written to WriteItemObject.
        /// </param>
        internal abstract void SetSessionStateItem(string name, object value, bool writeItem);

        /// <summary>
        /// Removes a session state item from the appropriate session state table.
        /// Derived classes must override this method to remove items from the
        /// proper table.
        /// </summary>
        /// <param name="name">
        /// The name of the item to remove.
        /// </param>
        internal abstract void RemoveSessionStateItem(string name);

        /// <summary>
        /// Gets all the items in the appropriate session state table.
        /// </summary>
        /// <returns>
        /// An IDictionary representing the items in the session state table.
        /// The key is the name of the item and the value is the value.
        /// </returns>
        internal abstract IDictionary GetSessionStateTable();

        /// <summary>
        /// Since items are often more than their value, this method should
        /// be overridden to provide the value for an item.
        /// </summary>
        /// <param name="item">
        /// The item to extract the value from.
        /// </param>
        /// <returns>
        /// The value of the specified item.
        /// </returns>
        /// <remarks>
        /// The default implementation will get
        /// the Value property of a DictionaryEntry
        /// </remarks>
        internal virtual object GetValueOfItem(object item)
        {
            Dbg.Diagnostics.Assert(
                item != null,
                "Caller should verify the item parameter");

            object value = item;

            if (item is DictionaryEntry)
            {
                value = ((DictionaryEntry)item).Value;
            }

            return value;
        }

        /// <summary>
        /// Determines if the item can be renamed. Derived classes that need
        /// to perform a check should override this method.
        /// </summary>
        /// <param name="item">
        /// The item to verify if it can be renamed.
        /// </param>
        /// <returns>
        /// true if the item can be renamed or false otherwise.
        /// </returns>
        internal virtual bool CanRenameItem(object item)
        {
            return true;
        }

        #endregion protected members

        #region ItemCmdletProvider overrides

        /// <summary>
        /// Gets an item from session state.
        /// </summary>
        /// <param name="name">
        /// Name of the item to get.
        /// </param>
        /// <remarks>
        /// The item instance is written to the WriteObject
        /// method.
        /// </remarks>
        protected override void GetItem(string name)
        {
            bool isContainer = false;
            object item = null;

            IDictionary table = GetSessionStateTable();
            if (table != null)
            {
                if (string.IsNullOrEmpty(name))
                {
                    isContainer = true;
                    item = table.Values;
                }
                else
                {
                    item = table[name];
                }
            }

            if (item != null)
            {
                if (SessionState.IsVisible(this.Context.Origin, item))
                {
                    WriteItemObject(item, name, isContainer);
                }
            }
        }

        /// <summary>
        /// Sets a session state item to a given value.
        /// </summary>
        /// <param name="name">
        /// Name of the item to set
        /// </param>
        /// <param name="value">
        /// The value to which to set the item
        /// </param>
        /// <returns>
        /// Nothing. The item that was set is written to the
        /// WriteObject method.
        /// </returns>
        protected override void SetItem(
            string name,
            object value)
        {
            if (string.IsNullOrEmpty(name))
            {
                WriteError(new ErrorRecord(
                    PSTraceSource.NewArgumentNullException(nameof(name)),
                    "SetItemNullName",
                    ErrorCategory.InvalidArgument,
                    name));
                return;
            }

            try
            {
                // Confirm the set item with the user

                string action = SessionStateProviderBaseStrings.SetItemAction;

                string resourceTemplate = SessionStateProviderBaseStrings.SetItemResourceTemplate;

                string resource =
                    string.Format(
                        Host.CurrentCulture,
                        resourceTemplate,
                        name,
                        value);

                if (ShouldProcess(resource, action))
                {
                    SetSessionStateItem(name, value, true);
                }
            }
            catch (SessionStateException e)
            {
                WriteError(
                    new ErrorRecord(
                        e.ErrorRecord,
                        e));
            }
            catch (PSArgumentException argException)
            {
                WriteError(
                    new ErrorRecord(
                        argException.ErrorRecord,
                        argException));
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="path"></param>
        protected override void ClearItem(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                WriteError(new ErrorRecord(
                    PSTraceSource.NewArgumentNullException(nameof(path)),
                    "ClearItemNullPath",
                    ErrorCategory.InvalidArgument,
                    path));
                return;
            }

            try
            {
                // Confirm the clear item with the user

                string action = SessionStateProviderBaseStrings.ClearItemAction;

                string resourceTemplate = SessionStateProviderBaseStrings.ClearItemResourceTemplate;

                string resource =
                    string.Format(
                        Host.CurrentCulture,
                        resourceTemplate,
                        path);

                if (ShouldProcess(resource, action))
                {
                    SetSessionStateItem(path, null, false);
                }
            }
            catch (SessionStateException e)
            {
                WriteError(
                    new ErrorRecord(
                        e.ErrorRecord,
                        e));
            }
            catch (PSArgumentException argException)
            {
                WriteError(
                    new ErrorRecord(
                        argException.ErrorRecord,
                        argException));
            }
        }

        #endregion ItemCmdletProvider overrides

        #region ContainerCmdletProvider overrides

        /// <summary>
        /// Gets the item(s) at the given path.
        /// </summary>
        /// <param name="path">
        /// The name of the item to retrieve, or all if empty or null.
        /// </param>
        /// <param name="recurse">
        /// Ignored.
        /// </param>
        protected override void GetChildItems(string path, bool recurse)
        {
            CommandOrigin origin = this.Context.Origin;
            if (string.IsNullOrEmpty(path))
            {
                IDictionary dictionary = null;

                try
                {
                    dictionary = GetSessionStateTable();
                }
                catch (SecurityException e)
                {
                    WriteError(
                        new ErrorRecord(
                            e,
                            "GetTableSecurityException",
                            ErrorCategory.ReadError,
                            path));
                    return;
                }

                // bug Windows7 #300974 says that we should sort
                List<DictionaryEntry> sortedEntries = new List<DictionaryEntry>(dictionary.Count + 1);
                foreach (DictionaryEntry entry in dictionary)
                {
                    sortedEntries.Add(entry);
                }

                sortedEntries.Sort(
                    (DictionaryEntry left, DictionaryEntry right) =>
                    {
                        string leftKey = (string)left.Key;
                        string rightKey = (string)right.Key;
                        IComparer<string> stringComparer = StringComparer.CurrentCultureIgnoreCase;
                        return stringComparer.Compare(leftKey, rightKey);
                    });

                // Now write out each object
                foreach (DictionaryEntry entry in sortedEntries)
                {
                    try
                    {
                        if (SessionState.IsVisible(origin, entry.Value))
                        {
                            WriteItemObject(entry.Value, (string)entry.Key, false);
                        }
                    }
                    catch (PSArgumentException argException)
                    {
                        WriteError(
                            new ErrorRecord(
                                argException.ErrorRecord,
                                argException));

                        return;
                    }
                    catch (SecurityException securityException)
                    {
                        WriteError(
                            new ErrorRecord(
                                securityException,
                                "GetItemSecurityException",
                                ErrorCategory.PermissionDenied,
                                (string)entry.Key));
                        return;
                    }
                }
            }
            else
            {
                object item = null;

                try
                {
                    item = GetSessionStateItem(path);
                }
                catch (PSArgumentException argException)
                {
                    WriteError(
                        new ErrorRecord(
                            argException.ErrorRecord,
                            argException));

                    return;
                }
                catch (SecurityException securityException)
                {
                    WriteError(
                        new ErrorRecord(
                            securityException,
                            "GetItemSecurityException",
                            ErrorCategory.PermissionDenied,
                            path));
                    return;
                }

                if (item != null)
                {
                    if (SessionState.IsVisible(origin, item))
                    {
                        WriteItemObject(item, path, false);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the name(s) of the item(s) at the given path.
        /// </summary>
        /// <param name="path">
        /// The name of the item to retrieve, or all if empty or null.
        /// </param>
        /// <param name="returnContainers">
        /// Ignored.
        /// </param>
        protected override void GetChildNames(string path, ReturnContainers returnContainers)
        {
            CommandOrigin origin = this.Context.Origin;
            if (string.IsNullOrEmpty(path))
            {
                IDictionary dictionary = null;

                try
                {
                    dictionary = GetSessionStateTable();
                }
                catch (SecurityException e)
                {
                    WriteError(
                        new ErrorRecord(
                            e,
                            "GetChildNamesSecurityException",
                            ErrorCategory.ReadError,
                            path));
                    return;
                }

                // Now write out each object's key...

                foreach (DictionaryEntry entry in dictionary)
                {
                    try
                    {
                        if (SessionState.IsVisible(origin, entry.Value))
                        {
                            WriteItemObject(entry.Key, (string)entry.Key, false);
                        }
                    }
                    catch (PSArgumentException argException)
                    {
                        WriteError(
                            new ErrorRecord(
                                argException.ErrorRecord,
                                argException));

                        return;
                    }
                    catch (SecurityException securityException)
                    {
                        WriteError(
                            new ErrorRecord(
                                securityException,
                                "GetItemSecurityException",
                                ErrorCategory.PermissionDenied,
                                (string)entry.Key));
                        return;
                    }
                }
            }
            else
            {
                object item = null;

                try
                {
                    item = GetSessionStateItem(path);
                }
                catch (SecurityException e)
                {
                    WriteError(
                        new ErrorRecord(
                            e,
                            "GetChildNamesSecurityException",
                            ErrorCategory.ReadError,
                            path));
                    return;
                }

                if (item != null)
                {
                    if (SessionState.IsVisible(origin, item))
                    {
                        WriteItemObject(path, path, false);
                    }
                }
            }
        }

        /// <summary>
        /// Determines if there are any items.
        /// </summary>
        /// <param name="path">
        /// The container to check to see if there are any children.
        /// </param>
        /// <returns>
        /// True if path is empty or null, false otherwise.
        /// </returns>
        protected override bool HasChildItems(string path)
        {
            bool result = false;

            if (string.IsNullOrEmpty(path))
            {
                try
                {
                    if (GetSessionStateTable().Count > 0)
                    {
                        result = true;
                    }
                }
                catch (SecurityException e)
                {
                    WriteError(
                        new ErrorRecord(
                            e,
                            "HasChildItemsSecurityException",
                            ErrorCategory.ReadError,
                            path));
                }
            }

            return result;
        }

        /// <summary>
        /// Determines if the specified item exists.
        /// </summary>
        /// <param name="path">
        /// The path to the item to check. If this is null or empty, the item
        /// container is used (and always exists).
        /// </param>
        /// <returns>
        /// True if the item exists, false otherwise.
        /// </returns>
        protected override bool ItemExists(string path)
        {
            bool result = false;

            if (string.IsNullOrEmpty(path))
            {
                result = true;
            }
            else
            {
                object item = null;

                try
                {
                    item = GetSessionStateItem(path);
                }
                catch (SecurityException e)
                {
                    WriteError(
                        new ErrorRecord(
                            e,
                            "ItemExistsSecurityException",
                            ErrorCategory.ReadError,
                            path));
                }

                if (item != null)
                {
                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Determines if the specified path is syntactically and semantically valid.
        /// </summary>
        /// <param name="path">
        /// The path to validate.
        /// </param>
        /// <returns>
        /// True if the path is valid, or false otherwise.
        /// </returns>
        /// <remarks>
        /// The path may not contain the following characters:
        /// . ( ) :
        /// </remarks>
        protected override bool IsValidPath(string path)
        {
            return !string.IsNullOrEmpty(path);
        }

        /// <summary>
        /// Removes the item at the specified path.
        /// </summary>
        /// <param name="path">
        /// The name of the item to be removed.
        /// </param>
        /// <param name="recurse">
        /// Ignored.
        /// </param>
        protected override void RemoveItem(string path, bool recurse)
        {
            if (string.IsNullOrEmpty(path))
            {
                Exception e =
                    PSTraceSource.NewArgumentException(nameof(path));
                WriteError(new ErrorRecord(
                    e,
                    "RemoveItemNullPath",
                    ErrorCategory.InvalidArgument,
                    path));
            }
            else
            {
                // Confirm the remove item with the user

                string action = SessionStateProviderBaseStrings.RemoveItemAction;

                string resourceTemplate = SessionStateProviderBaseStrings.RemoveItemResourceTemplate;

                string resource =
                    string.Format(
                        Host.CurrentCulture,
                        resourceTemplate,
                        path);

                if (ShouldProcess(resource, action))
                {
                    try
                    {
                        RemoveSessionStateItem(path);
                    }
                    catch (SessionStateException e)
                    {
                        WriteError(
                            new ErrorRecord(
                                e.ErrorRecord,
                                e));
                        return;
                    }
                    catch (SecurityException securityException)
                    {
                        WriteError(
                            new ErrorRecord(
                                securityException,
                                "RemoveItemSecurityException",
                                ErrorCategory.PermissionDenied,
                                path));
                        return;
                    }
                    catch (PSArgumentException argException)
                    {
                        WriteError(
                            new ErrorRecord(
                                argException.ErrorRecord,
                                argException));
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new item if one of the same name doesn't already exist.
        /// </summary>
        /// <param name="path">
        /// The name of the item to create.
        /// </param>
        /// <param name="type">
        /// Ignored.
        /// </param>
        /// <param name="newItem">
        /// The value of the new item.
        /// </param>
        protected override void NewItem(string path, string type, object newItem)
        {
            if (string.IsNullOrEmpty(path))
            {
                Exception e =
                    PSTraceSource.NewArgumentException(nameof(path));
                WriteError(new ErrorRecord(
                    e,
                    "NewItemNullPath",
                    ErrorCategory.InvalidArgument,
                    path));
                return;
            }

            if (newItem == null)
            {
                ArgumentNullException argException =
                    PSTraceSource.NewArgumentNullException("value");

                WriteError(
                    new ErrorRecord(
                        argException,
                        "NewItemValueNotSpecified",
                        ErrorCategory.InvalidArgument,
                        path));
                return;
            }

            if (ItemExists(path) && !Force)
            {
                PSArgumentException e =
                    (PSArgumentException)PSTraceSource.NewArgumentException(
                        nameof(path),
                        SessionStateStrings.NewItemAlreadyExists,
                        path);

                WriteError(
                    new ErrorRecord(
                        e.ErrorRecord,
                        e));
                return;
            }
            else
            {
                // Confirm the new item with the user

                string action = SessionStateProviderBaseStrings.NewItemAction;

                string resourceTemplate = SessionStateProviderBaseStrings.NewItemResourceTemplate;

                string resource =
                    string.Format(
                        Host.CurrentCulture,
                        resourceTemplate,
                        path,
                        type,
                        newItem);

                if (ShouldProcess(resource, action))
                {
                    SetItem(path, newItem);
                }
            }
        }

        /// <summary>
        /// Copies the specified item.
        /// </summary>
        /// <param name="path">
        /// The name of the item to copy.
        /// </param>
        /// <param name="copyPath">
        /// The name of the item to create.
        /// </param>
        /// <param name="recurse">
        /// Ignored.
        /// </param>
        protected override void CopyItem(string path, string copyPath, bool recurse)
        {
            if (string.IsNullOrEmpty(path))
            {
                Exception e =
                    PSTraceSource.NewArgumentException(nameof(path));
                WriteError(new ErrorRecord(
                    e,
                    "CopyItemNullPath",
                    ErrorCategory.InvalidArgument,
                    path));
                return;
            }

            // If copyPath is null or empty, that means we are trying to copy
            // the item to itself so it should be a no-op.

            if (string.IsNullOrEmpty(copyPath))
            {
                // Just get the item for -passthru
                GetItem(path);
                return;
            }

            object item = null;

            try
            {
                item = GetSessionStateItem(path);
            }
            catch (SecurityException e)
            {
                WriteError(
                    new ErrorRecord(
                        e,
                        "CopyItemSecurityException",
                        ErrorCategory.ReadError,
                        path));
                return;
            }

            if (item != null)
            {
                // Confirm the new item with the user

                string action = SessionStateProviderBaseStrings.CopyItemAction;

                string resourceTemplate = SessionStateProviderBaseStrings.CopyItemResourceTemplate;

                string resource =
                    string.Format(
                        Host.CurrentCulture,
                        resourceTemplate,
                        path,
                        copyPath);

                if (ShouldProcess(resource, action))
                {
                    try
                    {
                        SetSessionStateItem(copyPath, GetValueOfItem(item), true);
                    }
                    catch (SessionStateException e)
                    {
                        WriteError(
                            new ErrorRecord(
                                e.ErrorRecord,
                                e));
                        return;
                    }
                    catch (PSArgumentException argException)
                    {
                        WriteError(
                            new ErrorRecord(
                                argException.ErrorRecord,
                                argException));
                        return;
                    }
                }
            }
            else
            {
                PSArgumentException e =
                    (PSArgumentException)PSTraceSource.NewArgumentException(
                        nameof(path),
                        SessionStateStrings.CopyItemDoesntExist,
                        path);

                WriteError(
                    new ErrorRecord(
                        e.ErrorRecord,
                        e));
                return;
            }
        }

        /// <summary>
        /// Copies the specified item.
        /// </summary>
        /// <param name="name">
        /// The name of the item to copy.
        /// </param>
        /// <param name="newName">
        /// The new name of the item.
        /// </param>
        protected override void RenameItem(string name, string newName)
        {
            if (string.IsNullOrEmpty(name))
            {
                Exception e =
                    PSTraceSource.NewArgumentException(nameof(name));
                WriteError(new ErrorRecord(
                    e,
                    "RenameItemNullPath",
                    ErrorCategory.InvalidArgument,
                    name));
                return;
            }

            object item = null;

            try
            {
                item = GetSessionStateItem(name);
            }
            catch (SecurityException e)
            {
                WriteError(
                    new ErrorRecord(
                        e,
                        "RenameItemSecurityException",
                        ErrorCategory.ReadError,
                        name));
                return;
            }

            if (item != null)
            {
                if (ItemExists(newName) && !Force)
                {
                    PSArgumentException e =
                        (PSArgumentException)PSTraceSource.NewArgumentException(
                            nameof(newName),
                            SessionStateStrings.NewItemAlreadyExists,
                            newName);

                    WriteError(
                        new ErrorRecord(
                            e.ErrorRecord,
                            e));
                    return;
                }
                else
                {
                    try
                    {
                        if (CanRenameItem(item))
                        {
                            // Confirm the new item with the user

                            string action = SessionStateProviderBaseStrings.RenameItemAction;

                            string resourceTemplate = SessionStateProviderBaseStrings.RenameItemResourceTemplate;

                            string resource =
                                string.Format(
                                    Host.CurrentCulture,
                                    resourceTemplate,
                                    name,
                                    newName);

                            if (ShouldProcess(resource, action))
                            {
                                if (string.Equals(name, newName, StringComparison.OrdinalIgnoreCase))
                                {
                                    // This is a no-op. Just get the item for -passthru
                                    GetItem(newName);
                                    return;
                                }

                                try
                                {
                                    SetSessionStateItem(newName, item, true);
                                    RemoveSessionStateItem(name);
                                }
                                catch (SessionStateException e)
                                {
                                    WriteError(
                                        new ErrorRecord(
                                            e.ErrorRecord,
                                            e));
                                    return;
                                }
                                catch (PSArgumentException argException)
                                {
                                    WriteError(
                                        new ErrorRecord(
                                            argException.ErrorRecord,
                                            argException));
                                    return;
                                }
                                catch (SecurityException securityException)
                                {
                                    WriteError(
                                        new ErrorRecord(
                                            securityException,
                                            "RenameItemSecurityException",
                                            ErrorCategory.PermissionDenied,
                                            name));
                                    return;
                                }
                            }
                        }
                    }
                    catch (SessionStateException e)
                    {
                        WriteError(
                            new ErrorRecord(
                                e.ErrorRecord,
                                e));
                        return;
                    }
                }
            }
            else
            {
                PSArgumentException e =
                    (PSArgumentException)PSTraceSource.NewArgumentException(
                        nameof(name),
                        SessionStateStrings.RenameItemDoesntExist,
                        name);

                WriteError(
                    new ErrorRecord(
                        e.ErrorRecord,
                        e));
                return;
            }
        }

        #endregion ContainerCmdletProvider overrides

        #region IContentCmdletProvider methods

        /// <summary>
        /// Gets an instance of the content reader for this provider for the
        /// specified path.
        /// </summary>
        /// <param name="path">
        /// The path to get the content reader for.
        /// </param>
        /// <returns>
        /// An instance of an IContentReader for the given path.
        /// </returns>
        public IContentReader GetContentReader(string path)
        {
            return new SessionStateProviderBaseContentReaderWriter(path, this);
        }

        /// <summary>
        /// Gets an instance of the content writer for this provider for the
        /// specified path.
        /// </summary>
        /// <param name="path">
        /// The path to get the content writer for.
        /// </param>
        /// <returns>
        /// An instance of an IContentWriter for the given path.
        /// </returns>
        public IContentWriter GetContentWriter(string path)
        {
            return new SessionStateProviderBaseContentReaderWriter(path, this);
        }

        /// <summary>
        /// Always throws a NotSupportedException.
        /// </summary>
        /// <param name="path">
        /// ignored.
        /// </param>
        /// <exception cref="NotSupportedException">
        /// This exception is always thrown.
        /// </exception>
        public void ClearContent(string path)
        {
            throw
                PSTraceSource.NewNotSupportedException(
                    SessionStateStrings.IContent_Clear_NotSupported);
        }

        #region dynamic parameters

        // For now, none of the derived providers need dynamic parameters
        // so these methods just return null

        /// <summary>
        /// Always returns null.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Null.</returns>
        public object GetContentReaderDynamicParameters(string path) { return null; }

        /// <summary>
        /// Always returns null.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Null.</returns>
        public object GetContentWriterDynamicParameters(string path) { return null; }

        /// <summary>
        /// Always returns null.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Null.</returns>
        public object ClearContentDynamicParameters(string path) { return null; }

        #endregion
        #endregion
    }

    /// <summary>
    /// The content reader/writer for all providers deriving from SessionStateProviderBase.
    /// </summary>
    public class SessionStateProviderBaseContentReaderWriter : IContentReader, IContentWriter
    {
        /// <summary>
        /// Constructs a content reader/writer for the specified provider using the specified
        /// path to read or write the content.
        /// </summary>
        /// <param name="path">
        /// The path to the session state item which the content will be read or written.
        /// </param>
        /// <param name="provider">
        /// The SessionStateProviderBase derived provider that the content will be read or written
        /// from/to.
        /// </param>
        /// <exception cref="ArgumentException">
        /// if <paramref name="path"/> is null or empty.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="provider"/> is null.
        /// </exception>
        internal SessionStateProviderBaseContentReaderWriter(string path, SessionStateProviderBase provider)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentException(nameof(path));
            }

            if (provider == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(provider));
            }

            _path = path;
            _provider = provider;
        }

        private readonly string _path;
        private readonly SessionStateProviderBase _provider;

        /// <summary>
        /// Reads the content from the item.
        /// </summary>
        /// <param name="readCount">
        /// The number of "blocks" of data to be read from the item.
        /// </param>
        /// <returns>
        /// An array of the blocks of data read from the item.
        /// </returns>
        /// <remarks>
        /// A "block" of content is provider specific.  For the file system
        /// a "block" may be considered a byte, a character, or delimited string.
        /// </remarks>
        public IList Read(long readCount)
        {
            IList result = null;

            if (!_contentRead)
            {
                object item = _provider.GetSessionStateItem(_path);

                if (item != null)
                {
                    object getItemValueResult = _provider.GetValueOfItem(item);

                    if (getItemValueResult != null)
                    {
                        result = getItemValueResult as IList ?? new object[] { getItemValueResult };
                    }

                    _contentRead = true;
                }
            }

            return result;
        }

        private bool _contentRead;

        /// <summary>
        /// Writes content to the item.
        /// </summary>
        /// <param name="content">
        /// An array of content "blocks" to be written to the item.
        /// </param>
        /// <returns>
        /// The blocks of content that were successfully written to the item.
        /// </returns>
        /// <remarks>
        /// A "block" of content is provider specific.  For the file system
        /// a "block" may be considered a byte, a character, or delimited string.
        /// </remarks>
        public IList Write(IList content)
        {
            if (content == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(content));
            }

            // Unravel the IList if there is only one value
            object valueToSet = content;
            if (content.Count == 1)
            {
                valueToSet = content[0];
            }

            _provider.SetSessionStateItem(_path, valueToSet, false);

            return content;
        }

        /// <summary>
        /// None of the derived providers supports seeking for V1 so this
        /// always throws a NotSupportedException.
        /// </summary>
        /// <param name="offset">
        /// ignored
        /// </param>
        /// <param name="origin">
        /// ignored
        /// </param>
        /// <exception cref="NotSupportedException">
        /// This exception is always thrown.
        /// </exception>
        public void Seek(long offset, SeekOrigin origin)
        {
            throw
                PSTraceSource.NewNotSupportedException(
                    SessionStateStrings.IContent_Seek_NotSupported);
        }

        /// <summary>
        /// Closes the reader. None of the derived providers need to
        /// close their reader so do nothing.
        /// </summary>
        public void Close() { }

        /// <summary>
        /// Closes the reader. None of the derived providers need to
        /// close their reader so do nothing.
        /// </summary>
        public void Dispose() { Close(); GC.SuppressFinalize(this); }
    }
}

#pragma warning restore 56506
