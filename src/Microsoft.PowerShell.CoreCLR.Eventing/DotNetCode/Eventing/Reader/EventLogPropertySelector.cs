// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*============================================================
**
**
** Purpose:
** Public class that encapsulates the information for fast
** access to Event Values of an EventLogRecord. Implements
** the EventPropertyContext abstract class.  An instance of this
** class is constructed and then passed to
** EventLogRecord.GetEventPropertyValues.
**
============================================================*/

using System.Collections.Generic;

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// Encapsulates the information for fast access to Event Values
    /// of an EventLogRecord. An instance of this class is constructed
    /// and then passed to EventLogRecord.GetEventPropertyValues.
    /// </summary>
    public class EventLogPropertySelector : IDisposable
    {
        //
        // access to the data member reference is safe, while
        // invoking methods on it is marked SecurityCritical as appropriate.
        //
        private EventLogHandle _renderContextHandleValues;

        [System.Security.SecurityCritical]
        public EventLogPropertySelector(IEnumerable<string> propertyQueries)
        {
            if (propertyQueries == null)
                throw new ArgumentNullException("propertyQueries");

            string[] paths;

            ICollection<string> coll = propertyQueries as ICollection<string>;
            if (coll != null)
            {
                paths = new string[coll.Count];
                coll.CopyTo(paths, 0);
            }
            else
            {
                List<string> queries;
                queries = new List<string>(propertyQueries);
                paths = queries.ToArray();
            }

            _renderContextHandleValues = NativeWrapper.EvtCreateRenderContext(paths.Length, paths, UnsafeNativeMethods.EvtRenderContextFlags.EvtRenderContextValues);
        }

        internal EventLogHandle Handle
        {
            // just returning reference to security critical type, the methods
            // of that type are protected by SecurityCritical as appropriate.
            get
            {
                return _renderContextHandleValues;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [System.Security.SecuritySafeCritical]
        protected virtual void Dispose(bool disposing)
        {
            if (_renderContextHandleValues != null && !_renderContextHandleValues.IsInvalid)
                _renderContextHandleValues.Dispose();
        }
    }
}
