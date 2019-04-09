// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace System.Management.Automation
{
    /// <summary>
    /// An object that represents a stack of paths.
    /// </summary>
    public sealed class PathInfoStack : Stack<PathInfo>
    {
        /// <summary>
        /// Constructor for the PathInfoStack class.
        /// </summary>
        /// <param name="stackName">
        /// The name of the stack.
        /// </param>
        /// <param name="locationStack">
        /// A stack object containing PathInfo objects
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="locationStack"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="stackName"/> is null or empty.
        /// </exception>
        internal PathInfoStack(string stackName, Stack<PathInfo> locationStack) : base()
        {
            if (locationStack == null)
            {
                throw PSTraceSource.NewArgumentNullException("locationStack");
            }

            if (string.IsNullOrEmpty(stackName))
            {
                throw PSTraceSource.NewArgumentException("stackName");
            }

            Name = stackName;

            // Since the Stack<T> constructor takes an IEnumerable and
            // not a Stack<T> the stack actually gets enumerated in the
            // wrong order.  I have to push them on manually in the
            // appropriate order.

            PathInfo[] stackContents = new PathInfo[locationStack.Count];
            locationStack.CopyTo(stackContents, 0);

            for (int index = stackContents.Length - 1; index >= 0; --index)
            {
                this.Push(stackContents[index]);
            }
        }

        /// <summary>
        /// Gets the name of the stack.
        /// </summary>
        public string Name { get; } = null;
    }
}
