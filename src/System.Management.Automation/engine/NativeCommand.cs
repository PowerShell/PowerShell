/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/
using System;
using System.Management.Automation;
using System.Diagnostics;
using System.Management.Automation.Internal;

namespace System.Management.Automation
{
    /// <summary>
    /// Derives InternalCommand for Native Commands
    /// </summary>
    internal sealed class NativeCommand : InternalCommand
    {
        private NativeCommandProcessor myCommandProcessor;
        internal NativeCommandProcessor MyCommandProcessor
        {
            get { return myCommandProcessor; }
            set { myCommandProcessor = value; }
        }

        /// <summary>
        /// Implement the stop functionality for native commands...
        /// </summary>
        internal override void DoStopProcessing()
        {
            try
            {
                if (myCommandProcessor != null)
                    myCommandProcessor.StopProcessing();
            }
            catch (Exception e)
            {
                CommandProcessorBase.CheckForSevereException(e);
                // Ignore exceptions here...
                ;
            }
        }
    }
}
