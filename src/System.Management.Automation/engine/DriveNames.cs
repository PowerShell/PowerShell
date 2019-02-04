// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    /// <summary>
    /// Holds the strings used as the default drive names for all the
    /// default providers.
    /// </summary>
    internal static class DriveNames
    {
        /// <summary>
        /// The default VariableProvider drive name.
        /// </summary>
        internal const string VariableDrive = "Variable";

        /// <summary>
        /// The default EnvironmentProvider drive name.
        /// </summary>
        internal const string EnvironmentDrive = "Env";

        /// <summary>
        /// The default AliasProvider drive name.
        /// </summary>
        internal const string AliasDrive = "Alias";

        /// <summary>
        /// The default FunctionProvider drive name.
        /// </summary>
        internal const string FunctionDrive = "Function";

        /// <summary>
        /// The Temp drive name.
        /// </summary>
        internal const string TempDrive = "Temp";
    }
}
