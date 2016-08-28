/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

#pragma warning disable 1634, 1691
#pragma warning disable 56517

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Runspaces
{
    internal delegate void RunspaceConfigurationEntryUpdateEventHandler();

    /// <summary>
    /// Define class for runspace configuration entry collection. 
    /// </summary>
    /// <!--
    /// Runspace configuration entry collection is used for handling following 
    /// problems for runspace configuration entries. 
    /// 
    ///     1. synchronization. Since multiple runspaces may be sharing the same
    ///        runspace configuration, it is essential all the entry collections
    ///        (cmdlets, providers, assemblies, types, formats) are thread-safe. 
    ///     2. prepending/appending. Data for types and formats are order 
    ///        sensitive. It is required for supporting prepending/appending to 
    ///        the list and ability to remove the prepended/appended items. 
    ///     3. update. Update to the data needs to be communicated to other monad
    ///        components. For example, if cmdlets/providers list is updated, it 
    ///        has to be communicated to engine. 
    /// -->
#if CORECLR
    internal
#else
    public
#endif
    sealed class RunspaceConfigurationEntryCollection<T> : IEnumerable<T> where T : RunspaceConfigurationEntry
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public RunspaceConfigurationEntryCollection()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="items"></param>
        public RunspaceConfigurationEntryCollection(IEnumerable<T> items)
        {
            if (items == null)
            {
                throw PSTraceSource.NewArgumentNullException("item");
            }
            AddBuiltInItem(items);
        }

        private Collection<T> _data = new Collection<T>();
        private int _builtInEnd = 0; // this is the index of first after item. 

        private Collection<T> _updateList = new Collection<T>();
        internal Collection<T> UpdateList
        {
            get
            {
                return _updateList;
            }
        }

        /// <summary>
        /// Get items at a index position.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index]
        {
            get
            {
                lock (_syncObject)
                {
                    return _data[index];
                }
            }
        }

        /// <summary>
        /// Get number of items in the collection.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_syncObject)
                {
                    return _data.Count;
                }
            }
        }

        /// <summary>
        /// Reset items in the collection
        /// </summary>
        public void Reset()
        {
            lock (_syncObject)
            {
                for (int i = _data.Count - 1; i >= 0; i--)
                {
                    if (!_data[i].BuiltIn)
                    {
                        RecordRemove(_data[i]);

                        _data.RemoveAt(i);
                    }
                }

                _builtInEnd = _data.Count;
            }
        }

        /// <summary>
        /// Remove one item from the collection
        /// </summary>
        /// <param name="index"></param>
        /// <exception cref="PSArgumentOutOfRangeException">when <paramref name="index"/> is out of range.</exception>
        public void RemoveItem(int index)
        {
            lock (_syncObject)
            {
                if (index < 0 || index >= _data.Count)
                {
                    throw PSTraceSource.NewArgumentOutOfRangeException("index", index);
                }

                RecordRemove(_data[index]);

                _data.RemoveAt(index);

                if (index < _builtInEnd)
                    _builtInEnd--;

                return;
            }
        }

        /// <summary>
        /// Remove multiple items in the collection.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <exception cref="PSArgumentOutOfRangeException">when <paramref name="index"/> is out of range.</exception>
        public void RemoveItem(int index, int count)
        {
            lock (_syncObject)
            {
                if (index < 0 || index + count > _data.Count)
                {
                    throw PSTraceSource.NewArgumentOutOfRangeException("index", index);
                }

                for (int i = index + count - 1; i >= index; i--)
                {
                    RecordRemove(_data[i]);

                    _data.RemoveAt(i);
                }

                int _numBeforeBuiltInEnd = Math.Min(count, _builtInEnd - index);

                if (_numBeforeBuiltInEnd > 0)
                    _builtInEnd -= _numBeforeBuiltInEnd;

                return;
            }
        }

        /// <summary>
        /// Remove one item from the collection
        /// </summary>
        /// <param name="item"></param>
        internal void Remove(T item)
        {
            lock (_syncObject)
            {
                int index = _data.IndexOf(item);

                if (index < 0 || index >= _data.Count)
                {
                    Dbg.Assert(false, "Index from which to remove the item is out of range.");
                    throw PSTraceSource.NewArgumentOutOfRangeException("index", index);
                }

                RecordRemove(item);
                _data.Remove(item);

                if (index < _builtInEnd)
                    _builtInEnd--;

                return;
            }
        }

        /// <summary>
        /// Prepend an item to the collection.
        /// </summary>
        /// <param name="item"></param>
        public void Prepend(T item)
        {
            lock (_syncObject)
            {
                RecordAdd(item);

                item._builtIn = false;
                _data.Insert(0, item);
                _builtInEnd++;
            }
        }

        /// <summary>
        /// Prepend items into the collection
        /// </summary>
        /// <param name="items"></param>
        public void Prepend(IEnumerable<T> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            lock (_syncObject)
            {
                int i = 0;
                foreach (T t in items)
                {
                    RecordAdd(t);

                    t._builtIn = false;
                    _data.Insert(i++, t);
                    _builtInEnd++;
                }
            }
        }

        /// <summary>
        /// Append one item to the collection
        /// </summary>
        /// <param name="item"></param>
        public void Append(T item)
        {
            lock (_syncObject)
            {
                RecordAdd(item);

                item._builtIn = false;
                _data.Add(item);
            }
        }

        /// <summary>
        /// Append items to the collection.
        /// </summary>
        /// <param name="items"></param>
        public void Append(IEnumerable<T> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            lock (_syncObject)
            {
                foreach (T t in items)
                {
                    RecordAdd(t);

                    t._builtIn = false;
                    _data.Add(t);
                }
            }
        }

        internal void AddBuiltInItem(T item)
        {
            lock (_syncObject)
            {
                item._builtIn = true;

                RecordAdd(item);

                _data.Insert(_builtInEnd, item);
                _builtInEnd++;
            }
        }

        internal void AddBuiltInItem(IEnumerable<T> items)
        {
            lock (_syncObject)
            {
                foreach (T t in items)
                {
                    t._builtIn = true;

                    RecordAdd(t);

                    _data.Insert(_builtInEnd, t);
                    _builtInEnd++;
                }
            }
        }

        internal void RemovePSSnapIn(string PSSnapinName)
        {
            lock (_syncObject)
            {
                for (int i = _data.Count - 1; i >= 0; i--)
                {
                    if (_data[i].PSSnapIn != null)
                    {
                        if (_data[i].PSSnapIn.Name.Equals(PSSnapinName, StringComparison.Ordinal))
                        {
                            RecordRemove(_data[i]);

                            _data.RemoveAt(i);

                            if (i < _builtInEnd)
                                _builtInEnd--;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get enumerator for this collection.
        /// </summary>
        /// <returns></returns>
        /// <!--
        /// Enumerator work is not thread safe by default. Any code trying
        /// to do enumeration on this collection should lock it first.
        /// 
        /// Need to document this.
        /// -->
        IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        /// <summary>
        /// Get enumerator for this collection.
        /// </summary>
        /// <returns></returns>
        /// <!--
        /// Enumerator work is not thread safe by default. Any code trying
        /// to do enumeration on this collection should lock it first.
        /// 
        /// Need to document this.
        /// -->
        IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        /// <summary>
        /// Update others about the collection change. 
        /// </summary>
        public void Update()
        {
            Update(false);
        }

        internal void Update(bool force)
        {
            lock (_syncObject)
            {
                if (OnUpdate != null && (force || _updateList.Count > 0))
                {
                    OnUpdate();

                    // Here we need to clear the Action for each item
                    // since _updateList is sharing data with _data.
                    foreach (T t in _updateList)
                    {
                        t._action = UpdateAction.None;
                    }

                    _updateList.Clear();
                }
            }
        }

        private void RecordRemove(T t)
        {
            // if this item was added recently, simply remove the add action.
            if (t.Action == UpdateAction.Add)
            {
                t._action = UpdateAction.None;
                _updateList.Remove(t);
            }
            else
            {
                t._action = UpdateAction.Remove;
                _updateList.Add(t);
            }
        }

        private void RecordAdd(T t)
        {
            // if this item was removed recently, simply remove the add action.
            if (t.Action == UpdateAction.Remove)
            {
                t._action = UpdateAction.None;
                _updateList.Remove(t);
            }
            else
            {
                t._action = UpdateAction.Add;
                _updateList.Add(t);
            }
        }

        //object to use for locking
        private object _syncObject = new object();

        /// <summary>
        /// OnUpdate handler should lock the object itself. 
        /// </summary>
        internal event RunspaceConfigurationEntryUpdateEventHandler OnUpdate;
    }
}

#pragma warning restore 56517
