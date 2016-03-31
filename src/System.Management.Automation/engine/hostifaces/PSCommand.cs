/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Management.Automation.Runspaces;
using Dbg = System.Management.Automation.Diagnostics;
using System.Collections.ObjectModel;

namespace System.Management.Automation
{

    /// <summary>
    /// Defines a PowerShell command / script object which can be used with 
    /// <see cref="PowerShell"/> object.
    /// </summary>
    public sealed class PSCommand
    {
        #region Private Fields

        private PowerShell owner;
        private CommandCollection commands;
        private Command currentCommand;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates an empty PSCommand; a command or script must be added to this PSCommand before it can be executed.
        /// </summary>
        public PSCommand()
        {
            Initialize(null, false, null);
        }

        /// <summary>
        /// Internal copy constructor
        /// </summary>
        /// <param name="commandToClone"></param>
        internal PSCommand(PSCommand commandToClone)
        {
            commands = new CommandCollection();
            foreach (Command command in commandToClone.Commands)
            {
                Command clone = command.Clone () ;
                // Attach the cloned Command to this instance.
                commands.Add(clone);
                currentCommand = clone;
            }
        }

        /// <summary>
        /// Creates a PSCommand from the specified command
        /// </summary>
        /// <param name="command">Command object to use</param>
        internal PSCommand(Command command)
        {
            currentCommand = command;
            commands = new CommandCollection();
            commands.Add(currentCommand);
        }

        #endregion

        #region Command / Parameter Construction

        /// <summary>
        /// Add a command to construct a command pipeline.
        /// For example, to construct a command string "get-process | sort-object",
        ///     <code>
        ///         PSCommand command = new PSCommand("get-process").AddCommand("sort-object");
        ///     </code>
        /// </summary>
        /// <param name="command">
        /// A string representing the command.
        /// </param>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its 
        /// current state.
        /// </exception>
        /// <returns>
        /// A PSCommand instance with <paramref name="cmdlet"/> added.
        /// </returns>
        /// <remarks>
        /// This method is not thread safe.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// cmdlet is null.
        /// </exception>
        public PSCommand AddCommand(string command)
        {
            if (null == command)
            {
                throw PSTraceSource.NewArgumentNullException("cmdlet");
            }
            if (owner != null)
            {
                owner.AssertChangesAreAccepted();
            }

            currentCommand = new Command(command, false);
            commands.Add(currentCommand);

            return this;
        }

        /// <summary>
        /// Add a cmdlet to construct a command pipeline.
        /// For example, to construct a command string "get-process | sort-object",
        ///     <code>
        ///         PSCommand command = new PSCommand("get-process").AddCommand("sort-object");
        ///     </code>
        /// </summary>
        /// <param name="cmdlet">
        /// A string representing cmdlet.
        /// </param>
        /// <param name="useLocalScope">
        /// if true local scope is used to run the script command.
        /// </param>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its 
        /// current state.
        /// </exception>
        /// <returns>
        /// A PSCommand instance with <paramref name="cmdlet"/> added.
        /// </returns>
        /// <remarks>
        /// This method is not thread safe.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// cmdlet is null.
        /// </exception>
        public PSCommand AddCommand(string cmdlet, bool useLocalScope)
        {
            if (null == cmdlet)
            {
                throw PSTraceSource.NewArgumentNullException("cmdlet");
            }
            if (owner != null)
            {
                owner.AssertChangesAreAccepted();
            }

            currentCommand = new Command(cmdlet, false, useLocalScope);
            commands.Add(currentCommand);

            return this;
        }

        /// <summary>
        /// Add a piece of script  to construct a command pipeline.
        /// For example, to construct a command string "get-process | foreach { $_.Name }"
        ///     <code>
        ///         PSCommand command = new PSCommand("get-process").
        ///                                     AddCommand("foreach { $_.Name }", true);
        ///     </code>
        /// </summary>
        /// <param name="script">
        /// A string representing the script.
        /// </param>
        /// <returns>
        /// A PSCommand instance with <paramref name="command"/> added.
        /// </returns>
        /// <remarks>
        /// This method is not thread-safe.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// command is null.
        /// </exception>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its 
        /// current state.
        /// </exception>
        public PSCommand AddScript(string script)
        {
            if (null == script)
            {
                throw PSTraceSource.NewArgumentNullException("script");
            }
            if (owner != null)
            {
                owner.AssertChangesAreAccepted();
            }

            currentCommand = new Command(script, true);
            commands.Add(currentCommand);

            return this;
        }

        /// <summary>
        /// Add a piece of script  to construct a command pipeline.
        /// For example, to construct a command string "get-process | foreach { $_.Name }"
        ///     <code>
        ///         PSCommand command = new PSCommand("get-process").
        ///                                     AddCommand("foreach { $_.Name }", true);
        ///     </code>
        /// </summary>
        /// <param name="script">
        /// A string representing the script.
        /// </param>
        /// <param name="useLocalScope">
        /// if true local scope is used to run the script command.
        /// </param>
        /// <returns>
        /// A PSCommand instance with <paramref name="command"/> added.
        /// </returns>
        /// <remarks>
        /// This method is not thread-safe.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// command is null.
        /// </exception>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its 
        /// current state.
        /// </exception>
        public PSCommand AddScript(string script, bool useLocalScope)
        {
            if (null == script)
            {
                throw PSTraceSource.NewArgumentNullException("script");
            }
            if (owner != null)
            {
                owner.AssertChangesAreAccepted();
            }

            currentCommand = new Command(script, true, useLocalScope);
            commands.Add(currentCommand);

            return this;
        }

        /// <summary>
        /// Add a <see cref="Command"/> element to the current command
        /// pipeline.
        /// </summary>
        /// <param name="command">
        /// Command to add.
        /// </param>
        /// <returns>
        /// A PSCommand instance with <paramref name="command"/> added.
        /// </returns>
        /// <remarks>
        /// This method is not thread-safe.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// command is null.
        /// </exception>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its 
        /// current state.
        /// </exception>
        public PSCommand AddCommand(Command command)
        {
            if (null == command)
            {
                throw PSTraceSource.NewArgumentNullException("command");
            }
            if (owner != null)
            {
                owner.AssertChangesAreAccepted();
            }

            currentCommand = command;
            commands.Add(currentCommand);

            return this;
        }

        /// <summary>
        /// Add a parameter to the last added command.
        /// For example, to construct a command string "get-process | select-object -property name"
        ///     <code>
        ///         PSCommand command = new PSCommand("get-process").
        ///                                     AddCommand("select-object").AddParameter("property","name");
        ///     </code>
        /// </summary>
        /// <param name="parameterName">
        /// Name of the parameter.
        /// </param>
        /// <param name="value">
        /// Value for the parameter.
        /// </param>
        /// <returns>
        /// A PSCommand instance with <paramref name="parameterName"/> added 
        /// to the parameter list of the last command.
        /// </returns>
        /// <remarks>
        /// This method is not thread safe.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Name is non null and name length is zero after trimming whitespace.
        /// </exception>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its 
        /// current state.
        /// </exception>
        public PSCommand AddParameter(string parameterName, object value)
        {
            if (null == currentCommand)
            {
                throw PSTraceSource.NewInvalidOperationException(PSCommandStrings.ParameterRequiresCommand,
                                                                 new object[] {"PSCommand"});
            }
            if (owner != null)
            {
                owner.AssertChangesAreAccepted();
            }
            currentCommand.Parameters.Add(parameterName, value);
            return this;
        }

        /// <summary>
        /// Adds a switch parameter to the last added command.
        /// For example, to construct a command string "get-process | sort-object -descending"
        ///     <code>
        ///         PSCommand command = new PSCommand("get-process").
        ///                                     AddCommand("sort-object").AddParameter("descending");
        ///     </code>
        /// </summary>
        /// <param name="parameterName">
        /// Name of the parameter.
        /// </param>
        /// <returns>
        /// A PSCommand instance with <paramref name="parameterName"/> added 
        /// to the parameter list of the last command.
        /// </returns>
        /// <remarks>
        /// This method is not thread safe.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Name is non null and name length is zero after trimming whitespace.
        /// </exception>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its 
        /// current state.
        /// </exception>
        public PSCommand AddParameter(string parameterName)
        {
            if (null == currentCommand)
            {
                throw PSTraceSource.NewInvalidOperationException(PSCommandStrings.ParameterRequiresCommand,
                                                                 new object[] {"PSCommand"});
            }
            if (owner != null)
            {
                owner.AssertChangesAreAccepted();
            }
            currentCommand.Parameters.Add(parameterName, true);
            return this;
        }

        /// <summary>
        /// Adds an argument to the last added command.
        /// For example, to construct a command string "get-process | select-object name"
        ///     <code>
        ///         PSCommand command = new PSCommand("get-process").
        ///                                     AddCommand("select-object").AddParameter("name");
        ///     </code>
        /// 
        /// This will add the value "name" to the positional parameter list of "select-object"
        /// cmdlet. When the command is invoked, this value will get bound to positional parameter 0
        /// of the "select-object" cmdlet which is "Property".
        /// </summary>
        /// <param name="value">
        /// Value for the parameter.
        /// </param>
        /// <returns>
        /// A PSCommand instance parameter value <paramref name="value"/> added 
        /// to the parameter list of the last command.
        /// </returns>
        /// <exception cref="InvalidPowerShellStateException">
        /// Powershell instance cannot be changed in its 
        /// current state.
        /// </exception>
        /// <remarks>
        /// This method is not thread safe.
        /// </remarks>
        public PSCommand AddArgument(object value)
        {
            if (null == currentCommand)
            {
                throw PSTraceSource.NewInvalidOperationException(PSCommandStrings.ParameterRequiresCommand,
                                                                 new object[] {"PSCommand"});
            }
            if (owner != null)
            {
                owner.AssertChangesAreAccepted();
            }
            currentCommand.Parameters.Add(null, value);
            return this;
        }

        /// <summary>
        /// Adds an additional statement for execution
        /// 
        /// For example,
        ///     <code>
        ///         Runspace rs = RunspaceFactory.CreateRunspace();
        ///         PowerShell ps = PowerShell.Create();
        /// 
        ///         ps.Runspace = rs;
        ///         ps.AddCommand("Get-Process").AddArgument("idle");
        ///         ps.AddStatement().AddCommand("Get-Service").AddArgument("audiosrv");
        ///         ps.Invoke();
        ///     </code>
        /// </summary>
        /// <returns>
        /// A PowerShell instance with the items in <paramref name="parameters"/> added 
        /// to the parameter list of the last command.
        /// </returns>
        public PSCommand AddStatement()
        {
            if (commands.Count == 0)
            {
                return this;
            }

            commands[commands.Count - 1].IsEndOfStatement = true;
            return this;
        }

        #endregion

        #region Properties and Methods

        /// <summary>
        /// Gets the collection of commands from this PSCommand
        /// instance.
        /// </summary>
        public CommandCollection Commands
        {
            get
            {
                return commands;
            }
        }

        /// <summary>
        /// The PowerShell instance this PSCommand is associated to, or null if it is an standalone command
        /// </summary>
        internal PowerShell Owner
        {
            get
            {
                return owner;
            }
            set
            {
                owner = value;
            }
        }

        /// <summary>
        /// Clears the command(s). 
        /// </summary>
        public void Clear()
        {
            commands.Clear();
            currentCommand = null;
        }

        /// <summary>
        /// Creates a shallow copy of the current PSCommand.
        /// </summary>
        /// <returns>
        /// A shallow copy of the current PSCommand
        /// </returns>
        public PSCommand Clone()
        {
            return new PSCommand(this);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes the instance. Called from the constructor.
        /// </summary>
        /// <param name="command">
        /// Command to initialize the instance with.
        /// </param>
        /// <param name="isScript">
        /// true if the <paramref name="command"/> is script,
        /// false otherwise.
        /// </param>
        /// <param name="useLocalScope">
        /// if true local scope is used to run the script command.
        /// </param>
        /// <remarks>
        /// Caller should check the input.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// command is null
        /// </exception>
        private void Initialize(string command, bool isScript, bool? useLocalScope)
        {
            commands = new CommandCollection();

            if (command != null)
            {
                currentCommand = new Command(command, isScript, useLocalScope);
                commands.Add(currentCommand);
            }
        }

        #endregion
    }
}