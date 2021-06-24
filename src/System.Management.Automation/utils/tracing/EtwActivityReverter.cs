// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable
#if !UNIX

namespace System.Management.Automation.Tracing
{
    using System;

    /// <summary>
    ///     An object that can be used to revert the ETW activity ID of the current thread
    ///     to its original value.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Etw")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Reverter")]
    public interface IEtwActivityReverter :
        IDisposable
    {
        /// <summary>
        ///     Reverts the ETW activity ID of the current thread to its original value.
        /// </summary>
        /// <remarks>
        ///     <para>Calling <see cref="IDisposable.Dispose"/> has the same effect as
        ///         calling this method and is useful in the C# "using" syntax.</para>
        /// </remarks>
        void RevertCurrentActivityId();
    }

    internal class EtwActivityReverter :
        IEtwActivityReverter
    {
        private readonly IEtwEventCorrelator _correlator;
        private readonly Guid _oldActivityId;

        private bool _isDisposed;

        public EtwActivityReverter(IEtwEventCorrelator correlator, Guid oldActivityId)
        {
            _correlator = correlator;
            _oldActivityId = oldActivityId;
        }

        public void RevertCurrentActivityId()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _correlator.CurrentActivityId = _oldActivityId;
                _isDisposed = true;

                GC.SuppressFinalize(this);
            }
        }
    }
}

#endif
