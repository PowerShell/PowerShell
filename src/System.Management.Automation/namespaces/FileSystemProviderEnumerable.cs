// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
    /// <typeparam name="TResult">The type of a result.</typeparam>
    internal class FileSystemProviderEnumerable<TResult> : IEnumerable<TResult>
    {
        private readonly FindTransform _transform;
        private readonly EnumerationOptions _options;
        private readonly string _directory;
        private DelegateEnumerator? _enumerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemProviderEnumerable{TResult}"/> class.
        /// The class allows utilizing custom filter predicates and tranform delegates.
        /// </summary>
        /// <param name="directory">The path of the starting directory.</param>
        /// <param name="transform">The delegate to transform internal data to a result.</param>
        /// <param name="options">The enumeration options.</param>
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

        internal ContinueOnErrorPredicate? ShouldContinueOnErrorPredicate { get; set; }

        public IEnumerator<TResult> GetEnumerator()
        {
            return Interlocked.Exchange(ref _enumerator, null) ?? new DelegateEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Delegate for filtering out find results.
        /// </summary>
        /// <param name="entry">A lower level view of System.IO.FileSystemInfo for fitering.</param>
        /// <returns>If true include the entry to result.</returns>
        internal delegate bool FindPredicate(ref FileSystemEntry entry);

        /// <summary>
        /// Delegate for calling whenever the end of a directory is reached.
        /// </summary>
        /// <param name="directory">The path of the directory that finished.</param>
        internal delegate void OnDirectoryFinishedDelegate(ReadOnlySpan<char> directory);

        /// <summary>
        /// Delegate for calling whenever on I/O error.
        /// </summary>
        /// <param name="error">I/O error code.</param>
        /// <returns>If true continue the enumeration. If false throw.</returns>
        internal delegate bool ContinueOnErrorPredicate(int error);

        /// <summary>
        /// Delegate for transforming raw find data into a result.
        /// </summary>
        /// <param name="entry">A lower level view of System.IO.FileSystemInfo for transforming to TResult.</param>
        /// <returns>Result of the transformation of TResult type.</returns>
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

            protected override bool ContinueOnError(int error)
                //// Here _enumerable can be still null because base constructor can throw 'access denied' before we assign _enumerable.
                => _enumerable?.ShouldContinueOnErrorPredicate?.Invoke(error) ?? false;
        }
    }
}
