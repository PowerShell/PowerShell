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

namespace Microsoft.PackageManagement.Internal.Utility.Async {
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    internal static class AsyncActionExtensions {
        public static T Timeout<T>(this T asyncAction, int seconds) where T : IAsyncAction {
            asyncAction.Timeout = TimeSpan.FromSeconds(seconds);
            return asyncAction;
        }

        public static T Responsiveness<T>(this T asyncAction, int seconds) where T : IAsyncAction {
            asyncAction.Responsiveness = TimeSpan.FromSeconds(seconds);
            return asyncAction;
        }

        public static T CancelWhen<T>(this T asyncAction, CancellationToken cancellationToken) where T : IAsyncAction {
            cancellationToken.Register(asyncAction.Cancel);
            return asyncAction;
        }

        public static T AbortWhen<T>(this T asyncAction, CancellationToken cancellationToken) where T : IAsyncAction {
            cancellationToken.Register(asyncAction.Abort);
            return asyncAction;
        }

        public static T Wait<T>(this T asyncAction) where T : IAsyncAction {
            asyncAction.CompleteEvent.WaitOne();
            return asyncAction;
        }

        public static T Wait<T>(this T asyncAction, int milliseconds) where T : IAsyncAction {
            asyncAction.CompleteEvent.WaitOne(milliseconds);
            return asyncAction;
        }

        public static T Wait<T>(this T asyncAction, TimeSpan timespan) where T : IAsyncAction {
            asyncAction.CompleteEvent.WaitOne(timespan);
            return asyncAction;
        }

        public static T OnCompletion<T>(this T asyncAction, Action onCompleteAction) where T : class, IAsyncAction {
            asyncAction.OnComplete += onCompleteAction;
            return asyncAction;
        }

        public static T OnCancellation<T>(this T asyncAction, Action onCancelAction) where T : class, IAsyncAction {
            asyncAction.OnCancel += onCancelAction;
            return asyncAction;
        }

        public static T OnAborted<T>(this T asyncAction, Action onAbortAction) where T : class, IAsyncAction {
            asyncAction.OnAbort += onAbortAction;
            return asyncAction;
        }

        public static Task<T> AsTask<T>(this IAsyncValue<T> asyncValue) {
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.None);
            asyncValue.OnCancel += tcs.SetCanceled;
            asyncValue.OnAbort += () => tcs.SetException(new Exception("aborted"));
            asyncValue.OnComplete += () => tcs.SetResult(asyncValue.Value);
            return tcs.Task;
        }
    }
}