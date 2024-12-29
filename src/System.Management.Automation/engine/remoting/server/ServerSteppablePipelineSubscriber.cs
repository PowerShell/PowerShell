// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Dbg = System.Management.Automation.Diagnostics;

// Warning: Events StartSteppablePipeline and RunProcessRecord are never used
// They are actually used by the event manager in some dynamically generated IL
#pragma warning disable 0067

namespace System.Management.Automation
{
    /// <summary>
    /// Event handler argument.
    /// </summary>
    internal class ServerSteppablePipelineDriverEventArg : EventArgs
    {
        internal ServerSteppablePipelineDriver SteppableDriver;

        internal ServerSteppablePipelineDriverEventArg(ServerSteppablePipelineDriver driver)
        {
            this.SteppableDriver = driver;
        }
    }

    /// <summary>
    /// Steppable pipeline driver event handler class.
    /// </summary>
    internal class ServerSteppablePipelineSubscriber
    {
        #region Private data

        private readonly object _syncObject = new object();
        private bool _initialized = false;
        private PSLocalEventManager _eventManager;
        private PSEventSubscriber _startSubscriber;
        private PSEventSubscriber _processSubscriber;

        #endregion

        internal void SubscribeEvents(ServerSteppablePipelineDriver driver)
        {
            lock (_syncObject)
            {
                if (!_initialized)
                {
                    _eventManager = (object)driver.LocalPowerShell.Runspace.Events as PSLocalEventManager;

                    if (_eventManager != null)
                    {
                        _startSubscriber = _eventManager.SubscribeEvent(this, "StartSteppablePipeline", Guid.NewGuid().ToString(), null,
                            new PSEventReceivedEventHandler(this.HandleStartEvent), true, false, true);

                        _processSubscriber = _eventManager.SubscribeEvent(this, "RunProcessRecord", Guid.NewGuid().ToString(), null,
                            new PSEventReceivedEventHandler(this.HandleProcessRecord), true, false, true);
                    }

                    _initialized = true;
                }
            }
        }

        #region Events and Handlers

        public event EventHandler<EventArgs> StartSteppablePipeline;

        public event EventHandler<EventArgs> RunProcessRecord;

        /// <summary>
        /// Handles the start pipeline event, this is called by the event manager.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HandleStartEvent(object sender, PSEventArgs args)
        {
            ServerSteppablePipelineDriverEventArg driverArg = (object)args.SourceEventArgs as ServerSteppablePipelineDriverEventArg;
            ServerSteppablePipelineDriver driver = driverArg.SteppableDriver;

            Exception exceptionOccurred = null;

            try
            {
                using (ExecutionContextForStepping ctxt =
                    ExecutionContextForStepping.PrepareExecutionContext(
                        driver.LocalPowerShell.GetContextFromTLS(),
                        driver.LocalPowerShell.InformationalBuffers,
                        driver.RemoteHost))
                {
                    driver.SteppablePipeline = driver.LocalPowerShell.GetSteppablePipeline();
                    driver.SteppablePipeline.Begin(!driver.NoInput);
                }

                if (driver.NoInput)
                {
                    driver.HandleInputEndReceived(this, EventArgs.Empty);
                }
            }
            catch (Exception e)
            {
                // We need to catch this so that we can set the pipeline execution;
                // state to "failed" and send the exception as an error to the user.
                // Otherwise, the event manager will swallow this exception and
                // cause the client to not respond.
                exceptionOccurred = e;
            }

            if (exceptionOccurred != null)
            {
                driver.SetState(PSInvocationState.Failed, exceptionOccurred);
            }
        }

        /// <summary>
        /// Handles process record event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HandleProcessRecord(object sender, PSEventArgs args)
        {
            ServerSteppablePipelineDriverEventArg driverArg = (object)args.SourceEventArgs as ServerSteppablePipelineDriverEventArg;
            ServerSteppablePipelineDriver driver = driverArg.SteppableDriver;

            lock (driver.SyncObject)
            {
                // Make sure start event handler was called
                if (driver.SteppablePipeline == null)
                {
                    return;
                }

                // make sure only one thread does the processing
                if (driver.ProcessingInput)
                {
                    return;
                }

                driver.ProcessingInput = true;
                driver.Pulsed = false;
            }

            bool shouldDoComplete = false;
            Exception exceptionOccurred = null;
            try
            {
                using (ExecutionContextForStepping ctxt =
                ExecutionContextForStepping.PrepareExecutionContext(
                    driver.LocalPowerShell.GetContextFromTLS(),
                    driver.LocalPowerShell.InformationalBuffers,
                    driver.RemoteHost))
                {
                    bool isProcessCalled = false;
                    while (true)
                    {
                        if (driver.PipelineState != PSInvocationState.Running)
                        {
                            driver.SetState(driver.PipelineState, null);
                            return;
                        }

                        if (!driver.InputEnumerator.MoveNext())
                        {
                            shouldDoComplete = true;
                            if (!driver.NoInput || isProcessCalled)
                            {
                                // if there is noInput then we
                                // need to call process at least once
                                break;
                            }
                        }

                        isProcessCalled = true;
                        Array output;
                        if (driver.NoInput)
                        {
                            output = driver.SteppablePipeline.Process();
                        }
                        else
                        {
                            output = driver.SteppablePipeline.Process(driver.InputEnumerator.Current);
                        }

                        foreach (object o in output)
                        {
                            if (driver.PipelineState != PSInvocationState.Running)
                            {
                                driver.SetState(driver.PipelineState, null);
                                return;
                            }

                            // send the output data to the client
                            driver.DataStructureHandler.SendOutputDataToClient(PSObject.AsPSObject(o));
                        }

                        lock (driver.SyncObject)
                        {
                            driver.TotalObjectsProcessed++;
                            if (driver.TotalObjectsProcessed >= driver.Input.Count)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                exceptionOccurred = e;
            }
            finally
            {
                lock (driver.SyncObject)
                {
                    driver.ProcessingInput = false;
                    driver.CheckAndPulseForProcessing(false);
                }
                // Check if should perform stop
                if (driver.PipelineState == PSInvocationState.Stopping)
                {
                    driver.PerformStop();
                }
            }

            if (shouldDoComplete)
            {
                try
                {
                    using (ExecutionContextForStepping ctxt =
                    ExecutionContextForStepping.PrepareExecutionContext(
                        driver.LocalPowerShell.GetContextFromTLS(),
                        driver.LocalPowerShell.InformationalBuffers,
                        driver.RemoteHost))
                    {
                        Array output = driver.SteppablePipeline.End();
                        foreach (object o in output)
                        {
                            if (driver.PipelineState != PSInvocationState.Running)
                            {
                                driver.SetState(driver.PipelineState, null);
                                return;
                            }

                            // send the output data to the client
                            driver.DataStructureHandler.SendOutputDataToClient(PSObject.AsPSObject(o));
                        }

                        driver.SetState(PSInvocationState.Completed, null);
                        return;
                    }
                }
                catch (Exception e)
                {
                    exceptionOccurred = e;
                }
                finally
                {
                    // Check if should perform stop
                    if (driver.PipelineState == PSInvocationState.Stopping)
                    {
                        driver.PerformStop();
                    }
                }
            }

            if (exceptionOccurred != null)
            {
                driver.SetState(PSInvocationState.Failed, exceptionOccurred);
            }
        }

        /// <summary>
        /// Fires the start event.
        /// </summary>
        /// <param name="driver">Steppable pipeline driver.</param>
        internal void FireStartSteppablePipeline(ServerSteppablePipelineDriver driver)
        {
            lock (_syncObject)
            {
                _eventManager?.GenerateEvent(_startSubscriber.SourceIdentifier, this,
                    new object[1] { new ServerSteppablePipelineDriverEventArg(driver) }, null, true, false);
            }
        }

        /// <summary>
        /// Fires the process record event.
        /// </summary>
        /// <param name="driver">Steppable pipeline driver.</param>
        internal void FireHandleProcessRecord(ServerSteppablePipelineDriver driver)
        {
            lock (_syncObject)
            {
                _eventManager?.GenerateEvent(_processSubscriber.SourceIdentifier, this,
                    new object[1] { new ServerSteppablePipelineDriverEventArg(driver) }, null, true, false);
            }
        }

        #endregion
    }
}
