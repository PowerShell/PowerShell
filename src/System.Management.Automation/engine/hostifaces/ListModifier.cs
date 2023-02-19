// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Management.Automation
{
    /// <summary>
    /// PSListModifier is a simple helper class created by the update-list cmdlet.
    /// The update-list cmdlet will either return an instance of this class, or
    /// it will internally use an instance of this class to implement the updates.
    ///
    /// Cmdlets can also take a PSListModifier as a parameter.  Usage might look like:
    ///
    ///     Get-Mailbox | Set-Mailbox -Alias @{Add='jim'}
    ///
    /// Alias would take a PSListModifier and the Cmdlet code would be responsible
    /// for apply updates (possibly using PSListModifier.ApplyTo or else using custom logic).
    /// </summary>
    public class PSListModifier
    {
        /// <summary>
        /// Create a new PSListModifier with empty lists for Add/Remove.
        /// </summary>
        public PSListModifier()
        {
            _itemsToAdd = new Collection<object>();
            _itemsToRemove = new Collection<object>();
            _replacementItems = new Collection<object>();
        }

        /// <summary>
        /// Create a new PSListModifier with the specified add and remove lists.
        /// </summary>
        /// <param name="removeItems">The items to remove.</param>
        /// <param name="addItems">The items to add.</param>
        public PSListModifier(Collection<object> removeItems, Collection<object> addItems)
        {
            _itemsToAdd = addItems ?? new Collection<object>();
            _itemsToRemove = removeItems ?? new Collection<object>();
            _replacementItems = new Collection<object>();
        }

        /// <summary>
        /// Create a new PSListModifier to replace a given list with replaceItems.
        /// </summary>
        /// <param name="replacementItems">The item(s) to replace an existing list with.</param>
        public PSListModifier(object replacementItems)
        {
            _itemsToAdd = new Collection<object>();
            _itemsToRemove = new Collection<object>();
            if (replacementItems == null)
            {
                _replacementItems = new Collection<object>();
            }
            else if (replacementItems is Collection<object>)
            {
                _replacementItems = (Collection<object>)replacementItems;
            }
            else if (replacementItems is IList<object>)
            {
                _replacementItems = new Collection<object>((IList<object>)replacementItems);
            }
            else if (replacementItems is IList)
            {
                _replacementItems = new Collection<object>();
                foreach (object item in (IList)replacementItems)
                {
                    _replacementItems.Add(item);
                }
            }
            else
            {
                _replacementItems = new Collection<object>();
                _replacementItems.Add(replacementItems);
            }
        }

        /// <summary>
        /// Create a new PSListModifier with the specified add and remove lists (in the hash.)
        /// </summary>
        /// <param name="hash">A hashtable, where the value for key Add is the list to add
        /// and the value for Remove is the list to remove.</param>
        public PSListModifier(Hashtable hash)
        {
            if (hash == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(hash));
            }

            _itemsToAdd = new Collection<object>();
            _itemsToRemove = new Collection<object>();
            _replacementItems = new Collection<object>();

            foreach (DictionaryEntry entry in hash)
            {
                if (entry.Key is string)
                {
                    string key = entry.Key as string;
                    bool isAdd = key.Equals(AddKey, StringComparison.OrdinalIgnoreCase);
                    bool isRemove = key.Equals(RemoveKey, StringComparison.OrdinalIgnoreCase);
                    bool isReplace = key.Equals(ReplaceKey, StringComparison.OrdinalIgnoreCase);

                    if (!isAdd && !isRemove && !isReplace)
                    {
                        throw PSTraceSource.NewArgumentException(nameof(hash), PSListModifierStrings.ListModifierDisallowedKey, key);
                    }

                    Collection<object> collection;
                    if (isRemove)
                    {
                        collection = _itemsToRemove;
                    }
                    else if (isAdd)
                    {
                        collection = _itemsToAdd;
                    }
                    else
                    {
                        collection = _replacementItems;
                    }

                    IEnumerable enumerable = LanguagePrimitives.GetEnumerable(entry.Value);
                    if (enumerable != null)
                    {
                        foreach (object obj in enumerable)
                        {
                            collection.Add(obj);
                        }
                    }
                    else
                    {
                        collection.Add(entry.Value);
                    }
                }
                else
                {
                    throw PSTraceSource.NewArgumentException(nameof(hash), PSListModifierStrings.ListModifierDisallowedKey, entry.Key);
                }
            }
        }

        /// <summary>
        /// The list of items to add when ApplyTo is called.
        /// </summary>
        public Collection<object> Add
        {
            get { return _itemsToAdd; }
        }

        private readonly Collection<object> _itemsToAdd;

        /// <summary>
        /// The list of items to remove when ApplyTo is called.
        /// </summary>
        public Collection<object> Remove
        {
            get { return _itemsToRemove; }
        }

        private readonly Collection<object> _itemsToRemove;

        /// <summary>
        /// The list of items to replace an existing list with.
        /// </summary>
        public Collection<object> Replace
        {
            get { return _replacementItems; }
        }

        private readonly Collection<object> _replacementItems;

        /// <summary>
        /// Update the given collection with the items in Add and Remove.
        /// </summary>
        /// <param name="collectionToUpdate">The collection to update.</param>
        public void ApplyTo(IList collectionToUpdate)
        {
            if (collectionToUpdate == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(collectionToUpdate));
            }

            if (_replacementItems.Count > 0)
            {
                collectionToUpdate.Clear();
                foreach (object obj in _replacementItems)
                {
                    collectionToUpdate.Add(PSObject.Base(obj));
                }
            }
            else
            {
                foreach (object obj in _itemsToRemove)
                {
                    collectionToUpdate.Remove(PSObject.Base(obj));
                }

                foreach (object obj in _itemsToAdd)
                {
                    collectionToUpdate.Add(PSObject.Base(obj));
                }
            }
        }

        /// <summary>
        /// Update the given collection with the items in Add and Remove.
        /// </summary>
        /// <param name="collectionToUpdate">The collection to update.</param>
        public void ApplyTo(object collectionToUpdate)
        {
            ArgumentNullException.ThrowIfNull(collectionToUpdate);

            collectionToUpdate = PSObject.Base(collectionToUpdate);

            if (!(collectionToUpdate is IList list))
            {
                throw PSTraceSource.NewInvalidOperationException(PSListModifierStrings.UpdateFailed);
            }

            ApplyTo(list);
        }

        internal Hashtable ToHashtable()
        {
            Hashtable result = new Hashtable(2);

            if (_itemsToAdd.Count > 0)
            {
                result.Add(AddKey, _itemsToAdd);
            }

            if (_itemsToRemove.Count > 0)
            {
                result.Add(RemoveKey, _itemsToRemove);
            }

            if (_replacementItems.Count > 0)
            {
                result.Add(ReplaceKey, _replacementItems);
            }

            return result;
        }

        internal const string AddKey = "Add";
        internal const string RemoveKey = "Remove";
        internal const string ReplaceKey = "Replace";
    }

    /// <summary>
    /// A generic version of PSListModifier that exists for the sole purpose of making
    /// cmdlets that accept a PSListModifier more usable.  Users that look at the syntax
    /// of the command will see something like PSListModifier[Mailbox] and know they need
    /// to pass in Mailboxes.
    /// </summary>
    /// <typeparam name="T">The list element type</typeparam>
    public class PSListModifier<T> : PSListModifier
    {
        /// <summary>
        /// Create a new PSListModifier with empty lists for Add/Remove.
        /// </summary>
        public PSListModifier()
            : base()
        {
        }

        /// <summary>
        /// Create a new PSListModifier with the specified add and remove lists.
        /// </summary>
        /// <param name="removeItems">The items to remove.</param>
        /// <param name="addItems">The items to add.</param>
        public PSListModifier(Collection<object> removeItems, Collection<object> addItems)
            : base(removeItems, addItems)
        {
        }

        /// <summary>
        /// Create a new PSListModifier to replace a given list with replaceItems.
        /// </summary>
        /// <param name="replacementItems">The items to replace an existing list with.</param>
        public PSListModifier(object replacementItems)
            : base(replacementItems)
        {
        }

        /// <summary>
        /// Create a new PSListModifier with the specified add and remove lists (in the hash.)
        /// </summary>
        /// <param name="hash">A hashtable, where the value for key Add is the list to add
        /// and the value for Remove is the list to remove.</param>
        public PSListModifier(Hashtable hash)
            : base(hash)
        {
        }
    }
}
