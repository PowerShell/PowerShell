// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace System.Management.Automation.Subsystem
{
    /// <summary>
    /// Define the kinds of subsystems.
    /// </summary>
    public enum SubsystemKind
    {
        /// <summary>
        /// Component that provides predictive suggestions to commandline input.
        /// </summary>
        CommandPredictor = 1,
    }

    /// <summary>
    /// Define the base interface to implement a subsystem.
    /// The API contracts for specific subsystems are defined within the specific interfaces/abstract classes that implements this interface.
    /// </summary>
    /// <remarks>
    /// There are two purposes to have the internal member `Kind` declared in 'ISubsystem':
    /// 1. Make the mapping from an `ISubsystem` implementation to the `SubsystemKind` easy;
    /// 2. Make sure a user cannot directly implement 'ISubsystem', but have to derive from one of the concrete subsystem interface or abstract class.
    ///
    /// The internal member needs to have a default implementation defined by the specific subsystem interfaces or abstract class,
    /// because it should be the same for a specific kind of subsystem.
    /// </remarks>
    public interface ISubsystem
    {
        /// <summary>
        /// The unique identifier for a subsystem implementation.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// The name of a subsystem implementation.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The description of a subsystem implementation.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// A dictionary that contains the functions to define at the global scope.
        /// Key: function name; Value: function script.
        /// </summary>
        Dictionary<string, string> FunctionsToDefine { get; }

        /// <summary>
        /// Subsystem kind.
        /// </summary>
        internal SubsystemKind Kind { get; }
    }
}
