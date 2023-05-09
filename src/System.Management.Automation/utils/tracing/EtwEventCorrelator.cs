// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !UNIX

namespace System.Management.Automation.Tracing
{
    using System;
    using System.Diagnostics.Eventing;

    /// <summary>
    ///     An object that can be used to manage the ETW activity ID of the current thread.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Etw")]
#nullable enable
    public interface IEtwEventCorrelator
    {
        /// <summary>
        ///     Gets or sets the ETW activity ID of the current thread.
        /// </summary>
        /// <remarks>
        ///     <para>This method should only be used for advanced scenarios
        ///         or diagnostics.  Prefer using <see cref="StartActivity()"/>
        ///         or <see cref="StartActivity(Guid)"/> instead.</para>
        /// </remarks>
        Guid CurrentActivityId { get; set; }

        /// <summary>
        ///     Creates and sets a new activity ID for the current thread, optionally correlating
        ///     the new activity with another activity.
        /// </summary>
        /// <param name="relatedActivityId">The ID of an existing activity to be correlated with the
        ///     new activity or <see cref="Guid.Empty"/> if correlation is not desired.</param>
        /// <returns>An object which can be used to revert the activity ID of the current thread once
        ///     the new activity yields control of the current thread.</returns>
        IEtwActivityReverter StartActivity(Guid relatedActivityId);

        /// <summary>
        ///     Creates and sets a new activity ID for the current thread.  If the current thread
        ///     has an existing activity ID, it will be correlated with the new activity ID.
        /// </summary>
        /// <returns>An object which can be used to revert the activity ID of the current thread once
        ///     the new activity yields control of the current thread.</returns>
        IEtwActivityReverter StartActivity();
    }
#nullable restore

    /// <summary>
    ///     A simple implementation of <see cref="IEtwEventCorrelator"/>.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Etw")]
    public class EtwEventCorrelator :
        IEtwEventCorrelator
    {
        private readonly EventProvider _transferProvider;
        private readonly EventDescriptor _transferEvent;

        /// <summary>
        ///     Creates an <see cref="EtwEventCorrelator"/>.
        /// </summary>
        /// <param name="transferProvider">The <see cref="EventProvider"/> to use when logging transfer events
        ///     during activity correlation.</param>
        /// <param name="transferEvent">The <see cref="EventDescriptor"/> to use when logging transfer events
        ///     during activity correlation.</param>
        public EtwEventCorrelator(EventProvider transferProvider, EventDescriptor transferEvent)
        {
            ArgumentNullException.ThrowIfNull(transferProvider);

            _transferProvider = transferProvider;
            _transferEvent = transferEvent;
        }

        /// <summary>
        ///     Implements <see cref="IEtwEventCorrelator.CurrentActivityId"/>.
        /// </summary>
        public Guid CurrentActivityId
        {
            get
            {
                return EtwActivity.GetActivityId();
            }

            set
            {
                EventProvider.SetActivityId(ref value);
            }
        }

        /// <summary>
        ///     Implements <see cref="IEtwEventCorrelator.StartActivity(Guid)"/>.
        /// </summary>
        public IEtwActivityReverter StartActivity(Guid relatedActivityId)
        {
            var retActivity = new EtwActivityReverter(this, CurrentActivityId);
            CurrentActivityId = EventProvider.CreateActivityId();

            if (relatedActivityId != Guid.Empty)
            {
                var tempTransferEvent = _transferEvent;
                _transferProvider.WriteTransferEvent(in tempTransferEvent, relatedActivityId);
            }

            return retActivity;
        }

        /// <summary>
        ///     Implements <see cref="IEtwEventCorrelator.StartActivity()"/>.
        /// </summary>
        public IEtwActivityReverter StartActivity()
        {
            return StartActivity(CurrentActivityId);
        }
    }
}

#endif
