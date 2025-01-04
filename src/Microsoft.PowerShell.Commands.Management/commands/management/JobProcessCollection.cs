// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable
#if !UNIX
using System;
using System.Runtime.InteropServices;
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
    private nint _jobObjectHandle;

    /// <summary>
    /// The completion port handle that is used to monitor job events.
    /// </summary>
    private nint _completionPortHandle;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobProcessCollection"/> class.
    /// </summary>
    public JobProcessCollection()
    { }

    /// <summary>
    /// Initializes the job and IO completion port and adds the process to the
    /// /// job object.
    /// </summary>
    /// /// <param name="process">The process to add ot the job.</param>
    /// <returns>Whether the job creation and assignment worked or not.</returns>
    public bool AssignProcessToJobObject(SafeProcessHandle process)
    {
        if (!InitializeJob())
        {
            return false;
        }

        // // Add the process to the job object
        return Interop.Windows.AssignProcessToJobObject(
            _jobObjectHandle,
            process.DangerousGetHandle());
    }

    /// <summary>
    /// Blocks the current thread until all processes in the job have exited.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public void WaitForExit(CancellationToken cancellationToken)
    {
        if (_completionPortHandle == nint.Zero)
        {
            return;
        }

        using var cancellationRegistration = cancellationToken.Register(() =>
        {
            Interop.Windows.PostQueuedCompletionStatus(
                _completionPortHandle,
                Interop.Windows.JOB_OBJECT_MSG_ACTIVE_PROCESS_ZERO,
                nint.Zero,
                nint.Zero);
        });

        const int INFINITE = -1;
        int completionCode = 0;
        do
        {
            Interop.Windows.GetQueuedCompletionStatus(
                _completionPortHandle,
                out completionCode,
                out _,
                out _,
                INFINITE);
        }
        while (completionCode != Interop.Windows.JOB_OBJECT_MSG_ACTIVE_PROCESS_ZERO);
        cancellationToken.ThrowIfCancellationRequested();
    }

    private bool InitializeJob()
    {
        if (_initStatus is not null)
        {
            return (bool)_initStatus;
        }

        if (_jobObjectHandle == nint.Zero)
        {
            _jobObjectHandle = Interop.Windows.CreateJobObject(nint.Zero, nint.Zero);
            if (_jobObjectHandle == nint.Zero)
            {
                _initStatus = false;
                return false;
            }
        }

        if (_completionPortHandle == nint.Zero)
        {
            _completionPortHandle = Interop.Windows.CreateIoCompletionPort(
                -1,
                nint.Zero,
                nint.Zero,
                1);
            if (_completionPortHandle == nint.Zero)
            {
                _initStatus = false;
                return false;
            }
        }

        var completionPort = new Interop.Windows.JOBOBJECT_ASSOCIATE_COMPLETION_PORT()
        {
            CompletionKey = _jobObjectHandle,
            CompletionPort = _completionPortHandle,
        };

        _initStatus = Interop.Windows.SetInformationJobObject(
            _jobObjectHandle,
            ref completionPort);

        return (bool)_initStatus;
    }

    ~JobProcessCollection()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (_jobObjectHandle != nint.Zero)
        {
            Interop.Windows.CloseHandle(_jobObjectHandle);
            _jobObjectHandle = nint.Zero;
        }

        if (_completionPortHandle != nint.Zero)
        {
            Interop.Windows.CloseHandle(_completionPortHandle);
            _completionPortHandle = nint.Zero;
        }

        GC.SuppressFinalize(this);
    }
}
#endif
