// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !UNIX

using System.Collections.Generic;
using System.Diagnostics.Eventing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace System.Management.Automation.Tracing
{
    /// <summary>
    /// Attribute to represent an EtwEvent.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public sealed class EtwEvent : Attribute
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="eventId"></param>
        public EtwEvent(long eventId)
        {
            this.EventId = eventId;
        }

        /// <summary>
        /// EventId.
        /// </summary>
        public long EventId { get; }
    }

    /// <summary>
    /// Delegates that defines a call back with no parameter.
    /// </summary>
    public delegate void CallbackNoParameter();

    /// <summary>
    /// Delegates that defines a call back with one parameter (state)
    /// </summary>
    public delegate void CallbackWithState(object state);

    /// <summary>
    /// Delegates that defines a call back with two parameters; state and ElapsedEventArgs.
    /// It will be used in System.Timers.Timer scenarios.
    /// </summary>
    public delegate void CallbackWithStateAndArgs(object state, System.Timers.ElapsedEventArgs args);

    /// <summary>
    /// ETW events argument class.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    public class EtwEventArgs : EventArgs
    {
        /// <summary> Gets Event descriptor </summary>
        public EventDescriptor Descriptor
        {
            get;
            private set;
        }

        /// <summary> Gets whether the event is successfully written </summary>
        public bool Success { get; }

        /// <summary> Gets payload in the event </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public object[] Payload { get; }

        /// <summary>
        /// Creates a new instance of EtwEventArgs class.
        /// </summary>
        /// <param name="descriptor">Event descriptor.</param>
        /// <param name="success">Indicate whether the event is successfully written.</param>
        /// <param name="payload">Event payload.</param>
        public EtwEventArgs(EventDescriptor descriptor, bool success, object[] payload)
        {
            this.Descriptor = descriptor;
            this.Payload = payload;
            this.Success = success;
        }
    }

    /// <summary>
    /// This the abstract base class of all activity classes that represent an end-to-end scenario.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    public abstract class EtwActivity
    {
        /// <summary>
        /// This is a helper class that is used to wrap many multi-threading scenarios
        /// and makes correlation event to be logged easily.
        /// </summary>
        private sealed class CorrelatedCallback
        {
            private readonly CallbackNoParameter callbackNoParam;
            private readonly CallbackWithState callbackWithState;
            private readonly AsyncCallback asyncCallback;

            /// <summary>
            /// ParentActivityId.
            /// </summary>
            private readonly Guid parentActivityId;
            private readonly EtwActivity tracer;

            /// <summary>
            /// EtwCorrelator Constructor.
            /// </summary>
            /// <param name="tracer"></param>
            /// <param name="callback"></param>
            public CorrelatedCallback(EtwActivity tracer, CallbackNoParameter callback)
            {
                ArgumentNullException.ThrowIfNull(callback);

                ArgumentNullException.ThrowIfNull(tracer);

                this.tracer = tracer;
                this.parentActivityId = EtwActivity.GetActivityId();
                this.callbackNoParam = callback;
            }

            /// <summary>
            /// EtwCorrelator Constructor.
            /// </summary>
            /// <param name="tracer"></param>
            /// <param name="callback"></param>
            public CorrelatedCallback(EtwActivity tracer, CallbackWithState callback)
            {
                ArgumentNullException.ThrowIfNull(callback);

                ArgumentNullException.ThrowIfNull(tracer);

                this.tracer = tracer;
                this.parentActivityId = EtwActivity.GetActivityId();
                this.callbackWithState = callback;
            }

            /// <summary>
            /// EtwCorrelator Constructor.
            /// </summary>
            /// <param name="tracer"></param>
            /// <param name="callback"></param>
            public CorrelatedCallback(EtwActivity tracer, AsyncCallback callback)
            {
                ArgumentNullException.ThrowIfNull(callback);

                ArgumentNullException.ThrowIfNull(tracer);

                this.tracer = tracer;
                this.parentActivityId = EtwActivity.GetActivityId();
                this.asyncCallback = callback;
            }

            /// <summary>
            /// It is to be used in System.Timers.Timer scenarios.
            /// </summary>
            private readonly CallbackWithStateAndArgs callbackWithStateAndArgs;

            /// <summary>
            /// EtwCorrelator Constructor.
            /// </summary>
            /// <param name="tracer"></param>
            /// <param name="callback"></param>
            public CorrelatedCallback(EtwActivity tracer, CallbackWithStateAndArgs callback)
            {
                ArgumentNullException.ThrowIfNull(callback);

                ArgumentNullException.ThrowIfNull(tracer);

                this.tracer = tracer;
                this.parentActivityId = EtwActivity.GetActivityId();
                this.callbackWithStateAndArgs = callback;
            }

            /// <summary>
            /// This is the wrapper on the actual callback.
            /// </summary>
            public void Callback(object state, System.Timers.ElapsedEventArgs args)
            {
                Debug.Assert(callbackWithStateAndArgs != null, "callback is NULL.  There MUST always ba a valid callback!");

                Correlate();
                this.callbackWithStateAndArgs(state, args);
            }

            /// <summary>
            /// Correlate.
            /// </summary>
            private void Correlate()
            {
                tracer.CorrelateWithActivity(this.parentActivityId);
            }

            /// <summary>
            /// This is the wrapper on the actual callback.
            /// </summary>
            public void Callback()
            {
                Debug.Assert(callbackNoParam != null, "callback is NULL.  There MUST always ba a valid callback");

                Correlate();
                this.callbackNoParam();
            }

            /// <summary>
            /// This is the wrapper on the actual callback.
            /// </summary>
            public void Callback(object state)
            {
                Debug.Assert(callbackWithState != null, "callback is NULL.  There MUST always ba a valid callback!");

                Correlate();
                this.callbackWithState(state);
            }

            /// <summary>
            /// This is the wrapper on the actual callback.
            /// </summary>
            public void Callback(IAsyncResult asyncResult)
            {
                Debug.Assert(asyncCallback != null, "callback is NULL.  There MUST always ba a valid callback!");

                Correlate();
                this.asyncCallback(asyncResult);
            }
        }

        private static readonly Dictionary<Guid, EventProvider> providers = new Dictionary<Guid, EventProvider>();
        private static readonly object syncLock = new object();

        private static readonly EventDescriptor _WriteTransferEvent = new EventDescriptor(0x1f05, 0x1, 0x11, 0x5, 0x14, 0x0, (long)0x4000000000000000);

        private EventProvider currentProvider;

        /// <summary>
        /// Event handler for the class.
        /// </summary>
        public static event EventHandler<EtwEventArgs> EventWritten;

        /// <summary>
        /// Sets the activityId provided in the current thread.
        /// If current thread already has the same activityId it does
        /// nothing.
        /// </summary>
        /// <param name="activityId"></param>
        /// <returns>True when provided activity was set, false if current activity
        /// was found to be same and set was not needed.</returns>
        public static bool SetActivityId(Guid activityId)
        {
            if (GetActivityId() != activityId)
            {
                EventProvider.SetActivityId(ref activityId);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Creates a new ActivityId that can be used to set in the thread's context.
        /// </summary>
        /// <returns></returns>
        public static Guid CreateActivityId()
        {
            return EventProvider.CreateActivityId();
        }

        /// <summary>
        /// Returns the ActivityId set in current thread.
        /// </summary>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults")]
        public static Guid GetActivityId()
        {
            Guid activityId = Guid.Empty;
            Interop.Windows.GetEventActivityIdControl(ref activityId);
            return activityId;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        protected EtwActivity()
        {
        }

        /// <summary>
        /// CorrelateWithActivity (EventId: 0x1f05/7941)
        /// This method also sets a new activity id in current thread.
        /// And then correlates the new id with parentActivityId.
        /// </summary>
        public void CorrelateWithActivity(Guid parentActivityId)
        {
            EventProvider provider = GetProvider();
            if (!provider.IsEnabled())
                return;

            Guid activityId = CreateActivityId();
            SetActivityId(activityId);

            if (parentActivityId != Guid.Empty)
            {
                EventDescriptor transferEvent = TransferEvent;
                provider.WriteTransferEvent(in transferEvent, parentActivityId, activityId, parentActivityId);
            }
        }

        /// <summary>
        /// IsEnabled.
        /// </summary>
        public bool IsEnabled
        {
            get
            {
                return GetProvider().IsEnabled();
            }
        }

        /// <summary>
        /// Checks whether a provider matching certain levels and keyword is enabled.
        /// </summary>
        /// <param name="levels">Levels to check.</param>
        /// <param name="keywords">Keywords to check.</param>
        /// <returns>True, if any ETW listener is enabled else false.</returns>
        public bool IsProviderEnabled(byte levels, long keywords)
        {
            return GetProvider().IsEnabled(levels, keywords);
        }

        /// <summary>
        /// Correlates parent activity id set in the thread with a new activity id
        /// If parent activity id is not, it just sets a new activity in the current thread. And does not write the Transfer event.
        /// </summary>
        public void Correlate()
        {
            Guid parentActivity = GetActivityId();
            CorrelateWithActivity(parentActivity);
        }

        /// <summary>
        /// Wraps a callback with no params.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public CallbackNoParameter Correlate(CallbackNoParameter callback)
        {
            ArgumentNullException.ThrowIfNull(callback);

            return new CorrelatedCallback(this, callback).Callback;
        }

        /// <summary>
        /// Wraps a callback with one object param.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public CallbackWithState Correlate(CallbackWithState callback)
        {
            ArgumentNullException.ThrowIfNull(callback);

            return new CorrelatedCallback(this, callback).Callback;
        }

        /// <summary>
        /// Wraps a AsyncCallback with IAsyncResult param.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public AsyncCallback Correlate(AsyncCallback callback)
        {
            ArgumentNullException.ThrowIfNull(callback);

            return new CorrelatedCallback(this, callback).Callback;
        }

        /// <summary>
        /// Wraps a callback with one object param and one ElapsedEventArgs object
        /// This is meant to be used in System.Timers.Timer scenarios.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public CallbackWithStateAndArgs Correlate(CallbackWithStateAndArgs callback)
        {
            ArgumentNullException.ThrowIfNull(callback);

            return new CorrelatedCallback(this, callback).Callback;
        }

        /// <summary>
        /// The provider where the tracing messages will be written to.
        /// </summary>
        protected virtual Guid ProviderId
        {
            get
            {
                return PSEtwLogProvider.ProviderGuid;
            }
        }

        /// <summary>
        /// The event that is defined to be used to log transfer event.
        /// The derived class must override this property if they don't
        /// want to use the PowerShell's transfer event.
        /// </summary>
        protected virtual EventDescriptor TransferEvent
        {
            get
            {
                return _WriteTransferEvent;
            }
        }

        /// <summary>
        /// This is the main method that write the messages to the trace.
        /// All derived classes must use this method to write to the provider log.
        /// </summary>
        /// <param name="ed">EventDescriptor.</param>
        /// <param name="payload">Payload.</param>
        protected void WriteEvent(EventDescriptor ed, params object[] payload)
        {
            EventProvider provider = GetProvider();

            if (!provider.IsEnabled())
                return;

            if (payload != null)
            {
                for (int i = 0; i < payload.Length; i++)
                {
                    if (payload[i] == null)
                    {
                        payload[i] = string.Empty;
                    }
                }
            }

            bool success = provider.WriteEvent(in ed, payload);
            EventWritten?.Invoke(this, new EtwEventArgs(ed, success, payload));
        }

        private EventProvider GetProvider()
        {
            if (currentProvider != null)
                return currentProvider;

            lock (syncLock)
            {
                if (currentProvider != null)
                    return currentProvider;

                if (!providers.TryGetValue(ProviderId, out currentProvider))
                {
                    currentProvider = new EventProvider(ProviderId);
                    providers[ProviderId] = currentProvider;
                }
            }

            return currentProvider;
        }
    }
}

#endif
