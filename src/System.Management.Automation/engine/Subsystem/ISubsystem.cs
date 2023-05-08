// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;

namespace System.Management.Automation.Subsystem
{
    /// <summary>
    /// Define the kinds of subsystems.
    /// </summary>
    /// <remarks>
    /// This enum uses power of 2 as the values for the enum elements, so as to make sure
    /// the bitwise 'or' operation of the elements always results in an invalid value.
    /// </remarks>
    public enum SubsystemKind : uint
    {
        /// <summary>
        /// Component that provides predictive suggestions to commandline input.
        /// </summary>
        CommandPredictor = 1,

        /// <summary>
        /// Cross platform desired state configuration component.
        /// </summary>
        CrossPlatformDsc = 2,

        /// <summary>
        /// Component that provides feedback when a command fails interactively.
        /// </summary>
        FeedbackProvider = 4,
    }

    /// <summary>
    /// Define the base interface to implement a subsystem.
    /// The API contracts for specific subsystems are defined within the specific interfaces/abstract classes that implements this interface.
    /// </summary>
    /// <remarks>
    /// A user should not directly implement <see cref="ISubsystem"/>, but instead should derive from one of the concrete subsystem interfaces or abstract classes.
    /// The instance of a type that only implements 'ISubsystem' cannot be registered to the <see cref="SubsystemManager"/>.
    /// </remarks>
    public interface ISubsystem
    {
        /// <summary>
        /// Gets the unique identifier for a subsystem implementation.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Gets the name of a subsystem implementation.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the description of a subsystem implementation.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets a dictionary that contains the functions to be defined at the global scope of a PowerShell session.
        /// Key: function name; Value: function script.
        /// </summary>
        Dictionary<string, string>? FunctionsToDefine { get; }
    }
}
