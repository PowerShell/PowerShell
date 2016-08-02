/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// Class that encapsulates the information carried by the RunspaceInitInfo PSRP message
    /// </summary>
    internal class RunspacePoolInitInfo
    {
        /// <summary>
        /// Min Runspaces setting on the server runspace pool
        /// </summary>
        internal int MinRunspaces
        {
            get
            {
                return _minRunspaces;
            }
        }

        /// <summary>
        /// Max Runspaces setting on the server runspace pool
        /// </summary>
        internal int MaxRunspaces
        {
            get
            {
                return _maxRunspaces;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="minRS"></param>
        /// <param name="maxRS"></param>
        internal RunspacePoolInitInfo(int minRS, int maxRS)
        {
            _minRunspaces = minRS;
            _maxRunspaces = maxRS;
        }

        private int _minRunspaces;
        private int _maxRunspaces;
    }
}