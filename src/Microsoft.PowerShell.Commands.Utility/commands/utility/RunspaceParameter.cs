// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#pragma warning disable 1634, 1691
#pragma warning disable 56506

using Microsoft.PowerShell.Commands;

namespace System.Management.Automation.Runspaces
{
    /// <summary>
    /// Defines the attribute used to designate a cmdlet parameter as one that
    /// should accept runspaces.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class RunspaceAttribute: ArgumentTransformationAttribute
    {
        /// <summary>
        /// Transforms the input data to a Runspace.
        /// </summary>
        /// <param name="engineIntrinsics">
        /// The engine APIs for the context under which the transformation is being
        /// made.
        /// </param>
        /// <param name="inputData">
        /// If a string, the transformation uses the input as the runspace name.
        /// If an int, the transformation uses the input as the runspace ID.
        /// If a guid, the transformation uses the input as the runspace GUID.
        /// If already a Runspace, the transform does nothing.
        /// </param>
        /// <returns>A runspace object representing the inputData.</returns>
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            if ((engineIntrinsics == null) ||
                (engineIntrinsics.Host == null) ||
                (engineIntrinsics.Host.UI == null))
            {
                throw PSTraceSource.NewArgumentNullException("engineIntrinsics");
            }

            if (inputData == null)
            {
                return null;
            }

            // Try to coerce the input as a runspace
            Runspace runspace = LanguagePrimitives.FromObjectAs<Runspace>(inputData);
            if (runspace != null)
            {
                return runspace;
            }

            // Try to coerce the runspace if the user provided a string, int, or guid
            switch (inputData)
            {
                case string name:
                    var nameRunspaces = GetRunspaceUtils.GetRunspacesByName(new string[] { name });
                    if (nameRunspaces.Count == 1)
                    {
                        return nameRunspaces[0];
                    }
                    break;

                case int id:
                    var idRunspaces = GetRunspaceUtils.GetRunspacesById(new int[] { id });
                    if (idRunspaces.Count == 1)
                    {
                        return idRunspaces[0];
                    }
                    break;

                case Guid guid:
                    var guidRunspaces = GetRunspaceUtils.GetRunspacesByInstanceId(new Guid[] { guid });
                    if (guidRunspaces.Count == 1)
                    {
                        return guidRunspaces[0];
                    }
                    break;
            }

            // If we couldn't get a single runspace, return the inputData
            return inputData;
        }

        /// <summary/>
        public override bool TransformNullOptionalParameters { get { return false; } }
    }
}

#pragma warning restore 56506
