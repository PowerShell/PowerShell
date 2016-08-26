/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Diagnostics;

namespace Microsoft.PowerShell.Workflow
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Management.Automation;
    using System.Activities;
    using System.Timers;
    using System.Management.Automation.Tracing;
    using System.Collections;
    using System.Runtime.Serialization;

    /// <summary>
    /// Workflow timer types.
    /// </summary>
    internal enum WorkflowTimerType
    {
        RunningTimer = 1,

        ElapsedTimer = 2,
    }

    internal delegate void WorkflowTimerElapsedHandler(PSTimer sender, ElapsedEventArgs e);

    internal sealed class PSTimer : IDisposable
    {
        private bool disposed;

        private Timer timer;
        private readonly object syncLock = new object();

        internal WorkflowTimerType TimerType { get; private set; }
        private bool IsRecurring;
        private bool IsOneTimeTimer;
        private TimeSpan Interval;

        private TimeSpan RemainingTime;
        private DateTime? StartedAtForFirstTime;
        private DateTime? StartedTime;
        private bool IsRunning;
        internal bool TimerReachedAlready = false;

        private event WorkflowTimerElapsedHandler Handler;

        internal PSTimer(WorkflowTimerType type, bool isRecurring, bool isOneTimeTimer, TimeSpan interval, WorkflowTimerElapsedHandler handler)
        {
            Debug.Assert(!(isRecurring == true && isOneTimeTimer == true), "Timer cannot be recurring and one-time-timer at the same time.");

            this.TimerType = type;
            this.IsRecurring = isRecurring;
            this.IsOneTimeTimer = isOneTimeTimer;

            this.Interval = interval;
            this.RemainingTime = interval;
            this.StartedAtForFirstTime = null;
            this.StartedTime = null;
            this.IsRunning = false;

            this.Handler = handler;
        }

        internal PSTimer(Dictionary<string, object> data, WorkflowTimerElapsedHandler handler)
        {
            this.TimerType = (WorkflowTimerType)data["TimerType"];
            this.IsRecurring = (bool)data["IsRecurring"];
            this.IsOneTimeTimer = (bool)data["IsOneTimeTimer"];
            this.Interval = (TimeSpan)data["Interval"];

            if (IsRecurring == false && IsOneTimeTimer == false)
            {
                this.RemainingTime = (TimeSpan)data["RemainingTime"];
            }
            else if (IsRecurring == false && IsOneTimeTimer == true)
            {
                DateTime tmpStartedAtForFirstTime = (DateTime)data["StartedAtForFirstTime"];
                TimeSpan diff = Interval - DateTime.UtcNow.Subtract(tmpStartedAtForFirstTime);
                if (diff <= TimeSpan.FromSeconds(0))
                {
                    this.TimerReachedAlready = true;
                    this.RemainingTime = TimeSpan.FromSeconds(2);
                }
                else if (diff < TimeSpan.FromSeconds(2))
                    this.RemainingTime = TimeSpan.FromSeconds(2);
                else
                    this.RemainingTime = diff;
            }
            else
            {
                this.RemainingTime = Interval;
            }

            this.StartedAtForFirstTime = null;
            this.StartedTime = null;
            this.IsRunning = false;

            this.Handler = handler;

        }

        internal Dictionary<string, object> GetSerializedData()
        {
            Dictionary<string, object> data = new Dictionary<string, object>();

            data.Add("TimerType", TimerType);
            data.Add("IsRecurring", IsRecurring);
            data.Add("IsOneTimeTimer", IsOneTimeTimer);
            data.Add("Interval", Interval);

            if (IsRecurring == false && IsOneTimeTimer == false)
            {
                if (IsRunning)
                {
                    Debug.Assert(StartedTime.HasValue, "Started time should have value.");
                    TimeSpan tmpRemainingTime = RemainingTime - DateTime.UtcNow.Subtract((DateTime)StartedTime);
                    if (tmpRemainingTime < TimeSpan.FromMilliseconds(0))
                        tmpRemainingTime = TimeSpan.FromMilliseconds(0);

                    data.Add("RemainingTime", tmpRemainingTime);
                }
                else
                {
                    data.Add("RemainingTime", RemainingTime);
                }
            }
            else if (IsRecurring == false && IsOneTimeTimer == true)
            {
                // one time timer
                if (StartedAtForFirstTime.HasValue)
                    data.Add("StartedAtForFirstTime", StartedAtForFirstTime);
                else
                    data.Add("StartedAtForFirstTime", DateTime.UtcNow);
            }

            return data;
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (disposed)
                return;

            if (!IsRunning)
                return;

            lock (syncLock)
            {
                if (!IsRunning)
                    return;

                if (Handler != null)
                    Handler(this, e);

                if (IsRecurring == false)
                    IsRunning = false;
            }
        }

        internal void Start()
        {
            if (disposed)
                return;

            if (IsRunning)
                return;

            if (Interval <= TimeSpan.FromMilliseconds(0))
                return;

            lock (syncLock)
            {
                if (IsRunning)
                    return;

                if (timer == null)
                {
                    timer = new System.Timers.Timer(this.RemainingTime.TotalMilliseconds);
                    timer.AutoReset = IsRecurring;
                    timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
                    StartedAtForFirstTime = DateTime.UtcNow;
                }
                else
                {
                    timer.Interval = this.RemainingTime.TotalMilliseconds;
                }

                timer.Start();
                IsRunning = true;
                StartedTime = DateTime.UtcNow;
            }
        }

        internal void Stop()
        {
            if (disposed)
                return;

            if (!IsRunning)
                return;

            lock (syncLock)
            {
                if (!IsRunning)
                    return;

                if (timer == null)
                    return;

                timer.Stop();
                IsRunning = false;

                Debug.Assert(StartedTime.HasValue, "Started time should have value.");
                if (this.IsRecurring == false)
                {
                    RemainingTime -= DateTime.UtcNow.Subtract((DateTime)StartedTime);
                    if (RemainingTime < TimeSpan.FromMilliseconds(0))
                        RemainingTime = TimeSpan.FromMilliseconds(0);
                }
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if (this.disposed || !disposing)
                return;

            lock (syncLock)
            {
                if (this.disposed)
                    return;

                Stop();

                if (timer != null)
                {
                    timer.Elapsed -= this.timer_Elapsed;
                    this.Handler = null;
                    this.timer.Dispose();
                }

                this.disposed = true;
            }
        }
    }

    /// <summary>
    /// Define all the workflow related timers.
    /// </summary>
    public sealed class PSWorkflowTimer : IDisposable
    {
        private readonly PowerShellTraceSource Tracer = PowerShellTraceSourceFactory.GetTraceSource();
        private static readonly Tracer StructuredTracer = new Tracer();
        private readonly PSWorkflowInstance _instance;

        private bool disposed;
        private readonly object syncLock = new object();

        private Dictionary<WorkflowTimerType, PSTimer> _timers;

        /// <summary>
        /// Default Constructor
        /// </summary>
        internal PSWorkflowTimer(PSWorkflowInstance instance)
        {
            _instance = instance;
            _timers = new Dictionary<WorkflowTimerType, PSTimer>();
        }

        /// <summary>
        /// Creates a workflow timer for a workflow instance based on a BLOB
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="deserializedTimers"></param>
        public PSWorkflowTimer(PSWorkflowInstance instance, object deserializedTimers)
        {
            _instance = instance;
            _timers = new Dictionary<WorkflowTimerType, PSTimer>();

            if (deserializedTimers == null) throw new ArgumentNullException("deserializedTimers");
            
            List<object> deserializedTimerList = (List<object>)deserializedTimers;
            foreach (object data in deserializedTimerList)
            {
                Debug.Assert(data != null, "Timer data should not have been null.");
                if (data != null)
                {
                    Debug.Assert(data is Dictionary<string, object>, "The timer data should be of type Dictionary<string, object>.");
                    if (data is Dictionary<string, object>)
                    {
                        PSTimer timer = new PSTimer((Dictionary<string, object>)data, Timer_WorkflowTimerElapsed);
                        _timers.Add(timer.TimerType, timer);
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves timers as a BLOB
        /// </summary>
        /// <returns></returns>
        public object GetSerializedData()
        {
            if (disposed)
                return null;

            List<object> serializedTimers = new List<object>();
            foreach (PSTimer timer in _timers.Values)
            {
                serializedTimers.Add(timer.GetSerializedData());
            }

            return serializedTimers;
        }

        internal void SetupTimer(WorkflowTimerType timerType, TimeSpan interval)
        {
            if (disposed)
                return;

            if (_timers.ContainsKey(timerType))
                return;

            if (timerType == WorkflowTimerType.ElapsedTimer)
            {
                _timers.Add(timerType, new PSTimer(timerType, false, true, interval, Timer_WorkflowTimerElapsed));
            }
            else if (timerType == WorkflowTimerType.RunningTimer)
            {
                _timers.Add(timerType, new PSTimer(timerType, false, false, interval, Timer_WorkflowTimerElapsed));
            }
        }

        internal bool CheckIfTimerHasReachedAlready(WorkflowTimerType timerType)
        {
            if (disposed)
                return false;

            if (_timers.ContainsKey(timerType) && _timers[WorkflowTimerType.ElapsedTimer].TimerReachedAlready)
                return true;

            return false;
        }

        internal void StartTimer(WorkflowTimerType timerType)
        {
            if (disposed)
                return;

            if (_timers.ContainsKey(timerType))
            {
                _timers[timerType].Start();
            }
        }

        internal void StopTimer(WorkflowTimerType timerType)
        {
            if (disposed)
                return;

            if (_timers.ContainsKey(timerType))
            {
                _timers[timerType].Stop();
            }
        }

        private void Timer_WorkflowTimerElapsed(PSTimer sender, ElapsedEventArgs e)
        {
            if (disposed)
                return;

            StructuredTracer.Correlate();
            Tracer.WriteMessage("PSWorkflowTimer Elapsed: " + sender.TimerType);

            if (disposed)
                return;

            switch (sender.TimerType)
            {
                case WorkflowTimerType.RunningTimer:
                    sender.Stop();
                    TerminateWorkflow(Resources.RunningTimeReached);
                    break;
                case WorkflowTimerType.ElapsedTimer:
                    sender.Stop();
                    TerminateAndRemoveWorkflow(Resources.ElapsedTimeReached);
                    break;
            }
        }

        private readonly object syncElapsedLock = new object();
        private bool _elapsedTimerCalled = false;
        // Terminate workflow
        private void TerminateWorkflow(string reason)
        {
            if (disposed) return;
            if (_elapsedTimerCalled) return;

            lock (syncElapsedLock)
            {

                if (disposed) return;
                if (_elapsedTimerCalled) return;

                try
                {
                    Debug.Assert(_instance.PSWorkflowJob != null, "PSWorkflowJob should be set before calling terminate workflow");
                    _instance.PSWorkflowJob.StopJob(true, reason);
                }
                catch (Exception e)
                {
                    // logging the exception in background thread
                    Tracer.TraceException(e);
                }
            }
        }

        // Terminate and Remove workflow
        private void TerminateAndRemoveWorkflow(string reason)
        {
            _elapsedTimerCalled = true;
            if (disposed) return;

            lock (syncElapsedLock)
            {
                if (disposed) return;
                try
                {
                    _instance.PSWorkflowJob.StopJob(true, Resources.ElapsedTimeReached);
                }
                catch (Exception e)
                {
                    // logging the exception in background thread
                    Tracer.TraceException(e);
                }
            }
        }

        /// <summary>
        /// Dispose implementation.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected virtual implementation of Dispose.
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if (this.disposed || !disposing)
                return;
            
            lock (syncLock)
            {
                if (this.disposed)
                    return;

                foreach (PSTimer wt in _timers.Values)
                {
                    wt.Dispose();
                }
                _timers.Clear();

                this.disposed = true;
            }
        }
    }
}
