// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace System.Management.Automation.Language
{
    /// <summary>
    /// This type is introduced to provide a way to pass null into a .NET method that has a string parameter.
    /// </summary>
    public class NullString
    {
        #region private_members

        // Private member for instance.

        #endregion private_members

        #region public_property

        /// <summary>
        /// This overrides ToString() method and returns null.
        /// </summary>
        public override string ToString()
        {
            return null;
        }

        /// <summary>
        /// This returns the singleton instance of NullString.
        /// </summary>
        public static NullString Value { get; } = new NullString();

        #endregion public_property

        #region private Constructor

        /// <summary>
        /// This is a private constructor, meaning no outsiders have access.
        /// </summary>
        private NullString()
        {
        }

        #endregion private Constructor
    }
}