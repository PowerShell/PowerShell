// Copyright (c) Microsoft Corporation.
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
    public sealed class RunspaceAttribute : ArgumentTransformationAttribute
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
            if (engineIntrinsics?.Host?.UI == null)
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
                    var runspacesByName = GetRunspaceUtils.GetRunspacesByName(new[] { name });
                    if (runspacesByName.Count == 1)
                    {
                        return runspacesByName[0];
                    }

                    break;

                case int id:
                    var runspacesById = GetRunspaceUtils.GetRunspacesById(new[] { id });
                    if (runspacesById.Count == 1)
                    {
                        return runspacesById[0];
                    }

                    break;

                case Guid guid:
                    var runspacesByGuid = GetRunspaceUtils.GetRunspacesByInstanceId(new[] { guid });
                    if (runspacesByGuid.Count == 1)
                    {
                        return runspacesByGuid[0];
                    }

                    break;

                default:
                    // Non-convertible type
                    break;
            }

            // If we couldn't get a single runspace, return the inputData
            return inputData;
        }

        /// <summary>
        /// Gets a flag indicating whether or not null optional parameters are transformed.
        /// </summary>
        public override bool TransformNullOptionalParameters { get { return false; } }
    }
}

#pragma warning restore 56506
