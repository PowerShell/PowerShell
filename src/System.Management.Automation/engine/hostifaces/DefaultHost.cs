/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Globalization;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Reflection;

using Dbg = System.Diagnostics;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// This is the default host implementing PSHost offering minimal host capabilities.
    /// Runspace is the primary user of this class.
    /// </summary>

    internal class DefaultHost
        :
        PSHost
    {
        #region ctor

        /// <summary>
        /// Creates an instance based on the current culture and current UI culture
        /// </summary>
        /// <param name="currentCulture">Current culture for this host</param>
        /// <param name="currentUICulture">Current UI culture for this host</param>
        /// <exception/>

        internal DefaultHost(CultureInfo currentCulture, CultureInfo currentUICulture)
        {
            _currentCulture = currentCulture;
            _currentUICulture = currentUICulture;
        }

        #endregion ctor

        #region properties

        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <value></value>
        /// <exception/>

        public override
        string
        Name
        {
            get
            {
                return "Default Host";
            }
        }

        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <value></value>
        /// <exception/>

        public override
        System.Version
        Version
        {
            get
            {
                return _ver;
            }
        }

        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <value></value>
        /// <exception/>

        public override
        System.Guid
        InstanceId
        {
            get
            {
                return _id;
            }
        }


        /// <summary>
        /// 
        /// See base class
        /// This property is not supported
        /// 
        /// </summary>
        /// <value></value>
        /// <exception/>

        public override
        PSHostUserInterface
        UI
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <value></value>
        /// <exception/>

        public override
        CultureInfo
        CurrentCulture
        {
            get
            {
                return _currentCulture;
            }
        }

        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <value></value>
        /// <exception/>

        public override
        CultureInfo
        CurrentUICulture
        {
            get
            {
                return _currentUICulture;
            }
        }

        #endregion properties


        #region methods

        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <value></value>
        /// <exception/>

        public override
        void
        SetShouldExit(int exitCode)
        {
            // No op
        }

        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <value></value>
        /// <exception cref="NotSupportedException">
        /// 
        /// On calling this method
        /// 
        /// </exception>

        public override
        void
        EnterNestedPrompt()
        {
            throw PSTraceSource.NewNotSupportedException();
        }

        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <value></value>
        /// <exception cref="NotSupportedException">
        /// 
        /// On calling this method
        /// 
        /// </exception>
        public override
        void
        ExitNestedPrompt()
        {
            throw PSTraceSource.NewNotSupportedException();
        }

        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <value></value>
        /// <exception/>

        public override
        void
        NotifyBeginApplication()
        {
            // No op
        }

        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <value></value>
        /// <exception/>

        public override
        void
        NotifyEndApplication()
        {
            // No op
        }
        #endregion methods

        #region private fields

        private CultureInfo _currentCulture = null;
        private CultureInfo _currentUICulture = null;
        private Guid _id = Guid.NewGuid();
        private Version _ver = PSVersionInfo.PSVersion;

        #endregion private fields
    }
}

