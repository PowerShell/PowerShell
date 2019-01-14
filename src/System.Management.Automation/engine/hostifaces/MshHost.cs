// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Management.Automation.Runspaces;
using System.Diagnostics.CodeAnalysis;

namespace System.Management.Automation.Host
{
    /// <summary>
    /// Defines the properties and facilities providing by an application hosting an MSH <see
    /// cref="System.Management.Automation.Runspaces.Runspace"/>.
    /// </summary>
    /// <remarks>
    /// A hosting application derives from this class and
    /// overrides the abstract methods and properties.  The hosting application creates an instance of its derived class and
    /// passes it to the <see cref="System.Management.Automation.Runspaces.RunspaceFactory"/> CreateRunspace method.
    ///
    /// From the moment that the instance of the derived class (the "host class") is passed to CreateRunspace, the MSH runtime
    /// can call any of the methods of that class.  The instance must not be destroyed until after the Runspace is closed.
    ///
    /// There is a 1:1 relationship between the instance of the host class and the Runspace instance to which it is passed.  In
    /// other words, it is not legal to pass the same instance of the host class to more than one call to CreateRunspace.  (It
    /// is perfectly legal to call CreateRunspace more than once, as long as each call is supplied a unique instance of the host
    /// class.)
    ///
    /// Methods of the host class can be called by the Runspace or any cmdlet or script executed in that Runspace in any order
    /// and from any thread.  It is the responsibility of the hosting application to define the host class methods in a
    /// threadsafe fashion.  An implementation of the host class should not depend on method execution order.
    ///
    /// The instance of the host class that is passed to a Runspace is exposed by the Runspace to the cmdlets, scripts, and
    /// providers that are executed in that Runspace.  Scripts access the host class via the $Host built-in variable.  Cmdlets
    /// access the host via the Host property of the Cmdlet base class.
    /// </remarks>
    /// <seealso cref="System.Management.Automation.Runspaces.Runspace"/>
    /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface"/>
    /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface"/>

    public abstract class PSHost
    {
        /// <summary>
        /// The powershell spec states that 128 is the maximum nesting depth.
        /// </summary>
        internal const int MaximumNestedPromptLevel = 128;
        internal static bool IsStdOutputRedirected;

        /// <summary>
        /// Protected constructor which does nothing.  Provided per .Net design guidelines section 4.3.1.
        /// </summary>

        protected PSHost()
        {
            // do nothing
        }

        /// <summary>
        /// Gets the hosting application's identification in some user-friendly fashion. This name can be referenced by scripts and cmdlets
        /// to identify the host that is executing them.  The format of the value is not defined, but a short, simple string is
        /// recommended.
        /// </summary>
        /// <remarks>
        /// In implementing this member, you should return some sort of informative string describing the nature
        /// your hosting application. For the default console host shipped by Microsoft this is ConsoleHost.
        /// </remarks>
        /// <value>
        /// The name identifier of the hosting application.
        /// </value>
        /// <example>
        ///     <MSH>
        ///         if ($Host.Name -ieq "ConsoleHost") { write-host "I'm running in the Console Host" }
        ///     </MSH>
        /// </example>

        public abstract string Name
        {
            get;
        }

        /// <summary>
        /// Gets the version of the hosting application.  This value should remain invariant for a particular build of the
        /// host.  This value may be referenced by scripts and cmdlets.
        /// </summary>
        /// <remarks>
        /// When implementing this member, it should return the product version number for the product
        /// that is hosting the Monad engine.
        /// </remarks>
        /// <value>
        /// The version number of the hosting application.
        /// </value>

        public abstract System.Version Version
        {
            get;
        }

        /// <summary>
        /// Gets a GUID that uniquely identifies this instance of the host.  The value should remain invariant for the lifetime of
        /// this instance.
        /// </summary>

        public abstract System.Guid InstanceId
        {
            get;
        }

        /// <summary>
        /// Gets the hosting application's implementation of the
        /// <see cref="System.Management.Automation.Host.PSHostUserInterface"/> abstract base class. A host
        /// that does not want to support user interaction should return null.
        /// </summary>
        /// <value>
        /// A reference to an instance of the hosting application's implementation of a class derived from
        /// <see cref="System.Management.Automation.Host.PSHostUserInterface"/>, or null to indicate that user
        /// interaction is not supported.
        /// </value>
        /// <remarks>
        /// The implementation of this routine should return an instance of the appropriate
        /// implementation of PSHostUserInterface for this application. As an alternative,
        /// for simple scenarios, just returning null is sufficient.
        /// </remarks>

        public abstract System.Management.Automation.Host.PSHostUserInterface UI
        {
            get;
        }

        /// <summary>
        /// Gets the host's culture: the culture that the runspace should use to set the CurrentCulture on new threads.
        /// </summary>
        /// <value>
        /// A CultureInfo object representing the host's current culture.  Returning null is not allowed.
        /// </value>
        /// <remarks>
        /// The runspace will set the thread current culture to this value each time it starts a pipeline. Thus, cmdlets are
        /// encouraged to use Thread.CurrentThread.CurrentCulture.
        /// </remarks>

        public abstract System.Globalization.CultureInfo CurrentCulture
        {
            get;
        }

        /// <summary>
        /// Gets the host's UI culture: the culture that the runspace and cmdlets should use to do resource loading.
        ///
        /// The runspace will set the thread current ui culture to this value each time it starts a pipeline.
        /// </summary>
        /// <value>
        /// A CultureInfo object representing the host's current UI culture.  Returning null is not allowed.
        /// </value>

        public abstract System.Globalization.CultureInfo CurrentUICulture
        {
            get;
        }

        /// <summary>
        /// Request by the engine to end the current engine runspace (to shut down and terminate the host's root runspace).
        /// </summary>
        /// <remarks>
        /// This method is called by the engine to request the host shutdown the engine.  This is invoked by the exit keyword
        /// or by any other facility by which a runspace instance wishes to be shut down.
        ///
        /// To honor this request, the host should stop accepting and submitting commands to the engine and close the runspace.
        /// </remarks>
        /// <param name="exitCode">
        /// The exit code accompanying the exit keyword. Typically, after exiting a runspace, a host will also terminate. The
        /// exitCode parameter can be used to set the host's process exit code.
        /// </param>

        public abstract void SetShouldExit(int exitCode);

        /// <summary>
        /// Instructs the host to interrupt the currently running pipeline and start a new, "nested" input loop, where an input
        /// loop is the cycle of prompt, input, execute.
        /// </summary>
        /// <remarks>
        /// Typically called by the engine in response to some user action that suspends the currently executing pipeline, such
        /// as choosing the "suspend" option of a ConfirmProcessing call. Before calling this method, the engine should set
        /// various shell variables to the express the state of the interrupted input loop (current pipeline, current object in
        /// pipeline, depth of nested input loops, etc.)
        ///
        /// A non-interactive host may throw a "not implemented" exception here.
        ///
        /// If the UI property returns null, the engine should not call this method.
        /// <!--Was: ExecuteSubShell.  "subshell" implies a new child engine, which is not the case here.  This is called during the
        /// interruption of a pipeline to allow nested pipeline(s) to be run as a way to the user to suspend execution while he
        /// evaluates other commands.  It does not create a truly new engine instance with new session state.-->
        /// </remarks>
        /// <seealso cref="System.Management.Automation.Host.PSHost.ExitNestedPrompt"/>

        public abstract void EnterNestedPrompt();

        /// <summary>
        /// Causes the host to end the currently running input loop.  If the input loop was created by a prior call to
        /// EnterNestedPrompt, the enclosing pipeline will be resumed.  If the current input loop is the top-most loop, then the
        /// host will act as though SetShouldExit was called.
        /// </summary>
        /// <remarks>
        /// Typically called by the engine in response to some user action that resumes a suspended pipeline, such as with the
        /// 'continue-command' intrinsic cmdlet. Before calling this method, the engine should clear out the loop-specific
        /// variables that were set when the loop was created.
        ///
        /// If the UI Property returns a null, the engine should not call this method.
        /// </remarks>
        /// <seealso cref="EnterNestedPrompt"/>

        public abstract void ExitNestedPrompt();

        /// <summary>
        /// Used to allow the host to pass private data through a Runspace to cmdlets running inside that Runspace's
        /// runspace.  The type and nature of that data is entirely defined by the host, but there are some caveats:
        /// </summary>
        /// <returns>
        /// The default implementation returns null.
        /// </returns>
        /// <remarks>
        /// If the host is using an out-of-process Runspace, then the value of this property is serialized when crossing
        /// that process boundary in the same fashion as any object in a pipeline is serialized when crossing process boundaries.
        /// In this case, the BaseObject property of the value will be null.
        ///
        /// If the host is using an in-process Runspace, then the BaseObject property can be a non-null value a live object.
        /// No guarantees are made as to the app domain or thread that the BaseObject is accessed if it is accessed in the
        /// runspace. No guarantees of threadsafety or reentrancy are made.  The object set in the BaseObject property of
        /// the value returned by this method is responsible for ensuring its own threadsafety and re-entrance safety.
        /// Note that thread(s) accessing that object may not necessarily be the same from one access to the next.
        ///
        /// The return value should have value-semantics: that is, changes to the state of the instance returned are not
        /// reflected across processes.  Ex: if a cmdlet reads this property, then changes the state of the result, that
        /// change will not be visible to the host if the host is in another process.  Therefore, the implementation of
        /// get for this property should always return a unique instance.
        /// </remarks>

        public virtual PSObject PrivateData
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Called by the engine to notify the host that it is about to execute a "legacy" command line application.  A legacy
        /// application is defined as a console-mode executable that may do one or more of the following:
        /// . reads from stdin
        /// . writes to stdout
        /// . writes to stderr
        /// . uses any of the win32 console APIs.
        /// </summary>
        /// <remarks>
        /// Notifying the host allows the host to do such things as save off any state that might need to be restored when the
        /// legacy application terminates, set or remove break handler hooks, redirect stream handles, and so forth.
        ///
        /// The engine will always call this method and the NotifyEndApplication method in matching pairs.
        ///
        /// The engine may call this method several times in the course of a single pipeline.  For instance, the pipeline:
        ///
        /// foo.exe | bar-cmdlet | baz.exe
        ///
        /// Will result in a sequence of calls similar to the following:
        /// NotifyBeginApplication - called once when foo.exe is started
        /// NotifyBeginApplication - called once when baz.exe is started
        /// NotifyEndApplication - called once when baz.exe terminates
        /// NotifyEndApplication - called once when foo.exe terminates
        ///
        /// Note that the order in which the NotifyEndApplication call follows the corresponding call to NotifyBeginApplication
        /// with respect to any other call to NotifyBeginApplication is not defined, and should not be depended upon.  In other
        /// words, NotifyBeginApplication may be called several times before NotifyEndApplication is called.  The only thing
        /// that is guaranteed is that there will be an equal number of calls to NotifyEndApplication as to
        /// NotifyBeginApplication.
        /// </remarks>
        /// <seealso cref="System.Management.Automation.Host.PSHost.NotifyEndApplication"/>

        public abstract void NotifyBeginApplication();

        /// <summary>
        /// Called by the engine to notify the host that the execution of a legacy command has completed.
        /// </summary>
        /// <seealso cref="System.Management.Automation.Host.PSHost.NotifyBeginApplication"/>

        public abstract void NotifyEndApplication();

        /// <summary>
        /// Used by hosting applications to notify PowerShell engine that it is
        /// being hosted in a console based application and the Pipeline execution
        /// thread should call SetThreadUILanguage(0). This property is currently
        /// used by ConsoleHost only and in future releases we may consider
        /// exposing this publicly.
        /// </summary>
        internal bool ShouldSetThreadUILanguageToZero { get; set; }

        /// <summary>
        /// This property enables and disables the host debugger if debugging is supported.
        /// </summary>
        public virtual bool DebuggerEnabled
        {
            get { return false; }

            set { throw new PSNotImplementedException(); }
        }
    }

    /// <summary>
    /// This interface needs to be implemented by PSHost objects that want to support the PushRunspace
    /// and PopRunspace functionality.
    /// </summary>
    public interface IHostSupportsInteractiveSession
    {
        /// <summary>
        /// Called by the engine to notify the host that a runspace push has been requested.
        /// </summary>
        /// <seealso cref="System.Management.Automation.Host.IHostSupportsInteractiveSession.PushRunspace"/>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspace")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "runspace")]
        void PushRunspace(Runspace runspace);

        /// <summary>
        /// Called by the engine to notify the host that a runspace pop has been requested.
        /// </summary>
        /// <seealso cref="System.Management.Automation.Host.IHostSupportsInteractiveSession.PopRunspace"/>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspace")]
        void PopRunspace();

        /// <summary>
        /// True if a runspace is pushed; false otherwise.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspace")]
        bool IsRunspacePushed { get; }

        /// <summary>
        /// Returns the current runspace associated with this host.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspace")]
        Runspace Runspace { get; }
    }
}
