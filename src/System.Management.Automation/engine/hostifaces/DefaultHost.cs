// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;

using Dbg = System.Diagnostics;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// This is the default host implementing PSHost offering minimal host capabilities.
    /// Runspace is the primary user of this class.
    /// </summary>
    internal class DefaultHost : PSHost
    {
        #region ctor

        /// <summary>
        /// Creates an instance based on the current culture and current UI culture.
        /// </summary>
        /// <param name="currentCulture">Current culture for this host.</param>
        /// <param name="currentUICulture">Current UI culture for this host.</param>
        /// <exception/>
        internal DefaultHost(CultureInfo currentCulture, CultureInfo currentUICulture)
        {
            CurrentCulture = currentCulture;
            CurrentUICulture = currentUICulture;
        }

        #endregion ctor

        #region properties

        /// <summary>See base class</summary>
        public override string Name { get { return "Default Host"; } }

        /// <summary>See base class</summary>
        public override Version Version { get; } = PSVersionInfo.PSVersion;

        /// <summary>See base class</summary>
        public override Guid InstanceId { get; } = Guid.NewGuid();

        /// <summary>
        /// See base class
        /// This property is not supported.
        /// </summary>
        public override PSHostUserInterface UI { get { return null; } }

        /// <summary>
        /// See base class.
        /// </summary>
        public override CultureInfo CurrentCulture { get; } = null;

        /// <summary>
        /// See base class.
        /// </summary>
        public override CultureInfo CurrentUICulture { get; } = null;

        #endregion properties

        #region methods

        /// <summary>
        /// See base class.
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
        /// See base class.
        /// </summary>
        /// <value></value>
        /// <exception cref="NotSupportedException">
        /// On calling this method
        /// </exception>
        public override
        void
        EnterNestedPrompt()
        {
            throw PSTraceSource.NewNotSupportedException();
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <value></value>
        /// <exception cref="NotSupportedException">
        /// On calling this method
        /// </exception>
        public override
        void
        ExitNestedPrompt()
        {
            throw PSTraceSource.NewNotSupportedException();
        }

        /// <summary>
        /// See base class.
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
        /// See base class.
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

        #endregion private fields
    }
}
