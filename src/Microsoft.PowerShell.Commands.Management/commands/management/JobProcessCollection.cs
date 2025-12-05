// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable
#if !UNIX
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.PowerShell.Commands;

/// <summary>
/// JobProcessCollection is a helper class used by Start-Process -Wait cmdlet to monitor the
/// child processes created by the main process hosted by the Start-process cmdlet.
/// </summary>
internal sealed class JobProcessCollection : IDisposable
{
    /// <summary>
    /// Stores the initialisation state of the job and completion port.
    /// </summary>
    private bool? _initStatus;

    /// <summary>
    /// JobObjectHandle is a reference to the job object used to track
    /// the child processes created by the main process hosted by the Start-Process cmdlet.
    /// </summary>
    private Interop.Windows.SafeJobHandle? _jobObject;

    /// <summary>
    /// The completion port handle that is used to monitor job events.
    /// </summary>
    private Interop.Windows.SafeIoCompletionPort? _completionPort;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobProcessCollection"/> class.
    /// </summary>
    public JobProcessCollection()
    { }

    /// <summary>
    /// Initializes the job and IO completion port and adds the process to the
    /// job object.
    /// </summary>
    /// <param name="process">The process to add to the job.</param>
    /// <returns>Whether the job creation and assignment worked or not.</returns>
    public bool AssignProcessToJobObject(SafeProcessHandle process)
        => InitializeJob() && Interop.Windows.AssignProcessToJobObject(_jobObject, process);

    /// <summary>
    /// Blocks the current thread until all processes in the job have exited.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public void WaitForExit(CancellationToken cancellationToken)
    {
        if (_completionPort is null)
        {
            return;
        }

        using var cancellationRegistration = cancellationToken.Register(() =>
        {
            Interop.Windows.PostQueuedCompletionStatus(
                _completionPort,
                Interop.Windows.JOB_OBJECT_MSG_ACTIVE_PROCESS_ZERO);
        });

        int completionCode = 0;
        do
        {
            Interop.Windows.GetQueuedCompletionStatus(
                _completionPort,
                Interop.Windows.INFINITE,
                out completionCode);
        }
        while (completionCode != Interop.Windows.JOB_OBJECT_MSG_ACTIVE_PROCESS_ZERO);
        cancellationToken.ThrowIfCancellationRequested();
    }

    [MemberNotNullWhen(true, [nameof(_jobObject), nameof(_completionPort)])]
    private bool InitializeJob()
    {
        if (_initStatus.HasValue)
        {
            return _initStatus.Value;
        }

        if (_jobObject is null)
        {
            _jobObject = Interop.Windows.CreateJobObject();
            if (_jobObject.IsInvalid)
            {
                _initStatus = false;
                _jobObject.Dispose();
                _jobObject = null;
                return false;
            }
        }

        if (_completionPort is null)
        {
            _completionPort = Interop.Windows.CreateIoCompletionPort();
            if (_completionPort.IsInvalid)
            {
                _initStatus = false;
                _completionPort.Dispose();
                _completionPort = null;
                return false;
            }
        }

        _initStatus = Interop.Windows.SetInformationJobObject(
            _jobObject,
            _completionPort);

        return _initStatus.Value;
    }

    public void Dispose()
    {
        _jobObject?.Dispose();
        _completionPort?.Dispose();
    }
}
#endif
