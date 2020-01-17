// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Threading;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Enumerable that allows utilizing custom filter predicates and tranform delegates.
    /// </summary>
    internal class FileSystemProviderEnumerable<TResult> : IEnumerable<TResult>
    {
        private DelegateEnumerator? _enumerator;
        private readonly FindTransform _transform;
        private readonly EnumerationOptions _options;
        private readonly string _directory;

        /// <summary>
        /// Enumerable that allows utilizing custom filter predicates and tranform delegates.
        /// </summary>
        internal FileSystemProviderEnumerable(string directory, FindTransform transform, EnumerationOptions? options = null)
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
            _transform = transform ?? throw new ArgumentNullException(nameof(transform));
            _options = options ?? new EnumerationOptions();

            // We need to create the enumerator up front to ensure that we throw I/O exceptions for
            // the root directory on creation of the enumerable.
            _enumerator = new DelegateEnumerator(this);
        }

        internal FindPredicate? ShouldIncludePredicate { get; set; }

        internal FindPredicate? ShouldRecursePredicate { get; set; }

        internal FindPredicate? ShouldOnPredicate { get; set; }

        internal OnDirectoryFinishedDelegate? OnDirectoryFinishedAction { get; set; }

        public IEnumerator<TResult> GetEnumerator()
        {
            return Interlocked.Exchange(ref _enumerator, null) ?? new DelegateEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Delegate for filtering out find results.
        /// </summary>
        internal delegate bool FindPredicate(ref FileSystemEntry entry);

        /// <summary>
        /// Delegate for calling whenever the end of a directory is reached.
        /// </summary>
        /// <param name="directory">The path of the directory that finished.</param>
        internal delegate void OnDirectoryFinishedDelegate(ReadOnlySpan<char> directory);

        /// <summary>
        /// Delegate for transforming raw find data into a result.
        /// </summary>
        internal delegate TResult FindTransform(ref FileSystemEntry entry);

        private sealed class DelegateEnumerator : FileSystemEnumerator<TResult>
        {
            private readonly FileSystemProviderEnumerable<TResult> _enumerable;

            internal DelegateEnumerator(FileSystemProviderEnumerable<TResult> enumerable)
                : base(enumerable._directory, enumerable._options)
            {
                _enumerable = enumerable;
            }

            protected override TResult TransformEntry(ref FileSystemEntry entry) => _enumerable._transform(ref entry);
            protected override bool ShouldRecurseIntoEntry(ref FileSystemEntry entry)
                => _enumerable.ShouldRecursePredicate?.Invoke(ref entry) ?? true;
            protected override bool ShouldIncludeEntry(ref FileSystemEntry entry)
                => _enumerable.ShouldIncludePredicate?.Invoke(ref entry) ?? true;
            protected override void OnDirectoryFinished(ReadOnlySpan<char> directory)
                => _enumerable.OnDirectoryFinishedAction?.Invoke(directory);
        }
    }
}
