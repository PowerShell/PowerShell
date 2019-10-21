// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace System.Management.Automation.Internal
{
    /// <summary>
    /// This singleton provides a way to identify when a null literal was
    /// passed into a cmdlet that has explicit support for null literals
    /// (e.g. Where-Object when used with -is/-isnot).
    /// </summary>
    internal class NullLiteral
    {
        #region public_property

        /// <summary>
        /// This overrides ToString() method and returns null.
        /// </summary>
        public override string ToString()
        {
            return null;
        }

        /// <summary>
        /// This returns the singleton instance of NullLiteral.
        /// </summary>
        internal static NullLiteral Value { get; } = new NullLiteral();

        #endregion public_property

        #region private Constructor

        /// <summary>
        /// This is a private constructor, meaning no outsiders have access.
        /// </summary>
        private NullLiteral()
        {
        }

        #endregion private Constructor
    }
}
