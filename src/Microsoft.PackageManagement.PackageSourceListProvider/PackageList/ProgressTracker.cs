#if !UNIX

// 
//  Copyright (c) Microsoft Corporation. All rights reserved. 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//  


namespace Microsoft.PackageManagement.PackageSourceListProvider
{
    using Microsoft.PackageManagement.Internal.Implementation;

    internal class ProgressTracker
    {
        internal int ProgressID;
        internal int StartPercent;
        internal int EndPercent;

        internal ProgressTracker(int progressID) : this(progressID, 0, 100) { }

        internal ProgressTracker(int progressID, int startPercent, int endPercent)
        {
            ProgressID = progressID;
            StartPercent = startPercent;
            EndPercent = endPercent;
        }

        internal int ConvertPercentToProgress(double percent)
        {
            // for example, if startprogress is 50, end progress is 100, and if we want to complete 50% of that,
            // then the progress returned would be 75
            return StartPercent + (int)((EndPercent - StartPercent) * percent);
        }

        internal static ProgressTracker StartProgress(ProgressTracker parentTracker, string message, Request request)
        {
            if (request == null)
            {
                return null;
            }

            // if parent tracker is null, use 0 for parent id, else use the progressid of parent tracker
            return new ProgressTracker(request.StartProgress(parentTracker == null ? 0 : parentTracker.ProgressID, message));
        }
    }
}

#endif