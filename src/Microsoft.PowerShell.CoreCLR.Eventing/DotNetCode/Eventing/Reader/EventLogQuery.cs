// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*============================================================
**
**
** Purpose:
** This public class allows a user to define events of interest.
** An instance of this class is passed to an EventReader to actually
** obtain the EventRecords.   The EventLogQuery can be as
** simple specifying that all events are of interest, or it can contain
** query / xpath expressions that indicate exactly what characteristics
** events should have.
**
============================================================*/

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// Allows a user to define events of interest. An instance of this
    /// class is passed to an EventReader to actually obtain the EventRecords.
    /// The EventLogQuery can be as simple specifying that all events are of
    /// interest, or it can contain query / xpath expressions that indicate exactly
    /// what characteristics events should have.
    /// </summary>
    public class EventLogQuery
    {
        private string _query;
        private string _path;
        private EventLogSession _session;
        private PathType _pathType;
        private bool _tolerateErrors = false;
        private bool _reverseDirection = false;

        public EventLogQuery(string path, PathType pathType)
            : this(path, pathType, null)
        {
        }

        public EventLogQuery(string path, PathType pathType, string query)
        {
            _session = EventLogSession.GlobalSession;
            _path = path;   // can be null
            _pathType = pathType;

            if (query == null)
            {
                if (path == null)
                    throw new ArgumentNullException("path");
            }
            else
            {
                _query = query;
            }
        }

        public EventLogSession Session
        {
            get
            {
                return _session;
            }

            set
            {
                _session = value;
            }
        }

        public bool TolerateQueryErrors
        {
            get
            {
                return _tolerateErrors;
            }

            set
            {
                _tolerateErrors = value;
            }
        }

        public bool ReverseDirection
        {
            get
            {
                return _reverseDirection;
            }

            set
            {
                _reverseDirection = value;
            }
        }

        internal string Path
        {
            get
            {
                return _path;
            }
        }

        internal PathType ThePathType
        {
            get
            {
                return _pathType;
            }
        }

        internal string Query
        {
            get
            {
                return _query;
            }
        }
    }
}
