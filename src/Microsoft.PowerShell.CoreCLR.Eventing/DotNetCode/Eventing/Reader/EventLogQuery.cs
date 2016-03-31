// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class: EventLogQuery
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

namespace System.Diagnostics.Eventing.Reader {

    /// <summary>
    /// Allows a user to define events of interest. An instance of this 
    /// class is passed to an EventReader to actually obtain the EventRecords.   
    /// The EventLogQuery can be as simple specifying that all events are of 
    /// interest, or it can contain query / xpath expressions that indicate exactly
    /// what characteristics events should have. 
    /// </summary>
    public class EventLogQuery {

        private string query;
        private string path;
        private EventLogSession session;
        private PathType pathType;
        private bool tolerateErrors = false;
        private bool reverseDirection = false;

        public EventLogQuery(string path, PathType pathType)
            : this(path, pathType, null) {
        }

        public EventLogQuery(string path, PathType pathType, string query) {

            this.session = EventLogSession.GlobalSession;
            this.path = path;   // can be null
            this.pathType = pathType;

            if (query == null) {
                if (path == null)
                    throw new ArgumentNullException("path");
            }
            else {
                this.query = query;
            }
        }

        public EventLogSession Session {
            get {
                return this.session;
            }
            set {
                this.session = value;
            }
        }

        public bool TolerateQueryErrors {
            get {
                return this.tolerateErrors;
            }
            set {
                this.tolerateErrors = value;
            }
        }

        public bool ReverseDirection {
            get {
                return this.reverseDirection;
            }
            set {
                this.reverseDirection = value;
            }
        }

        internal string Path {
            get {
                return this.path;
            }
        }

        internal PathType ThePathType {
            get {
                return this.pathType;
            }
        }

        internal string Query {
            get {
                return this.query;
            }
        }

    }
}
