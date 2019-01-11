// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace System.Management.Automation.Remoting.WSMan
{
    /// <summary>
    /// This class channels WSMan server specific notifications to subscribers.
    /// One example is shutting down.
    /// </summary>
    public static class WSManServerChannelEvents
    {
        #region public members

        /// <summary>
        /// Event raised when shutting down WSMan server.
        /// </summary>
        public static event EventHandler ShuttingDown;

        /// <summary>
        /// Event raised when active sessions in an endpoint are changed.
        /// </summary>
        public static event EventHandler<ActiveSessionsChangedEventArgs> ActiveSessionsChanged;

        #endregion public members

        #region internal members

        /// <summary>
        /// Raising shutting down WSMan server event.
        /// </summary>
        internal static void RaiseShuttingDownEvent()
        {
            EventHandler handler = ShuttingDown;
            if (handler != null)
            {
                handler(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Raising ActiveSessionsChanged event.
        /// </summary>
        internal static void RaiseActiveSessionsChangedEvent(ActiveSessionsChangedEventArgs eventArgs)
        {
            EventHandler<ActiveSessionsChangedEventArgs> handler = ActiveSessionsChanged;
            if (handler != null)
            {
                handler(null, eventArgs);
            }
        }

        #endregion internal members
    }

    /// <summary>
    /// Holds the event arguments when active sessions count changed.
    /// </summary>
    public sealed class ActiveSessionsChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new ActiveSessionsChangedEventArgs instance.
        /// </summary>
        /// <param name="activeSessionsCount"></param>
        public ActiveSessionsChangedEventArgs(int activeSessionsCount)
        {
            ActiveSessionsCount = activeSessionsCount;
        }

        /// <summary>
        /// ActiveSessionsCount.
        /// </summary>
        public int ActiveSessionsCount
        {
            get;
            internal set;
        }
    }
}
