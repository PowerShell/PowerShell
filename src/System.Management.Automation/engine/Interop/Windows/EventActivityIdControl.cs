// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

#if !UNIX
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Windows
    {
        internal enum ActivityControl : uint
        {
            /// <summary>
            /// Gets the ActivityId from thread local storage.
            /// </summary>
            Get = 1,

            /// <summary>
            /// Sets the ActivityId in the thread local storage.
            /// </summary>
            Set = 2,

            /// <summary>
            /// Creates a new activity id.
            /// </summary>
            Create = 3,

            /// <summary>
            /// Sets the activity id in thread local storage and returns the previous value.
            /// </summary>
            GetSet = 4,

            /// <summary>
            /// Creates a new activity id, sets thread local storage, and returns the previous value.
            /// </summary>
            CreateSet = 5
        }

        [LibraryImport("api-ms-win-eventing-provider-l1-1-0.dll")]
        internal static unsafe partial int EventActivityIdControl(ActivityControl controlCode, Guid* activityId);

        internal static unsafe int GetEventActivityIdControl(ref Guid activityId)
        {
            fixed (Guid* guidPtr = &activityId)
            {
                return EventActivityIdControl(ActivityControl.Get, guidPtr);
            }
        }
    }
}
#endif
