#Requires -Version 7.0

# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

<#
.SYNOPSIS
    Registers Microsoft Update for automatic updates (best-effort, non-blocking).

.DESCRIPTION
    This script is called by the MSI installer to opt into Microsoft Update.
    It is designed to be:
    - Idempotent: exits early if already registered
    - Time-bounded: uses external process with timeout to avoid hangs
    - Non-fatal: always exits 0 so the MSI can complete

    In constrained language mode or WDAC/AppLocker environments, COM operations
    may fail. This script handles those cases gracefully.

.PARAMETER TestHook
    For testing purposes only. 'Hang' simulates a hang, 'Fail' simulates a failure.
#>

param(
    [ValidateSet('Hang', 'Fail')]
    $TestHook
)

# Microsoft Update service GUID
$MicrosoftUpdateServiceId = '7971f918-a847-4430-9279-4a52d1efe18d'
# Service registration flags: asfAllowPendingRegistration + asfAllowOnlineRegistration + asfRegisterServiceWithAU
$MicrosoftUpdateServiceRegistrationFlags = 7
$waitTimeoutSeconds = 120

# Helper function to release COM objects deterministically
function Release-ComObject {
    param($ComObject)
    if ($null -ne $ComObject) {
        try {
            [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($ComObject)
        }
        catch {
            # Intentionally suppressing errors - COM release can fail if object is already released
            # or inaccessible. This is expected and should not affect script success.
            Write-Verbose "COM object cleanup failed (expected/safe to ignore): $_" -Verbose:$false
        }
    }
}

# Idempotent pre-check: if already registered, exit immediately
# This runs outside the job/external process to minimize work when already registered
function Test-MicrosoftUpdateRegistered {
    $serviceManager = $null
    $registration = $null
    $service = $null
    try {
        $serviceManager = New-Object -ComObject Microsoft.Update.ServiceManager
        $registration = $serviceManager.QueryServiceRegistration($MicrosoftUpdateServiceId)
        $service = $registration.Service
        return $service.IsRegisteredWithAu
    }
    catch {
        # QueryServiceRegistration throws if the service isn't registered
        # or if COM operations fail. In either case, we should attempt registration.
        # Return $false to indicate not registered.
        return $false
    }
    finally {
        Release-ComObject $service
        Release-ComObject $registration
        Release-ComObject $serviceManager
    }
}

# Build the job script (check test hooks first before idempotency check)
switch ($TestHook) {
    'Hang' {
        $waitTimeoutSeconds = 10
        $jobScript = { Start-Sleep -Seconds 600 }
    }
    'Fail' {
        $jobScript = { throw "This job script should fail" }
    }
    default {
        # Check if already registered before doing any expensive work
        Write-Verbose "RegisterMicrosoftUpdate: checking if already registered..." -Verbose
        $alreadyRegistered = Test-MicrosoftUpdateRegistered

        if ($alreadyRegistered -eq $true) {
            Write-Verbose "RegisterMicrosoftUpdate: Microsoft Update is already registered, skipping" -Verbose
            exit 0
        }

        # Not registered, attempt to register using an external process with timeout
        # Using external process avoids issues with constrained language mode affecting jobs/runspaces
        Write-Verbose "RegisterMicrosoftUpdate: Microsoft Update not registered, attempting registration..." -Verbose

        # Normal path: register via COM in a job with timeout
        $jobScript = {
            param($ServiceId, $RegistrationFlags)
            $serviceManager = New-Object -ComObject Microsoft.Update.ServiceManager
            try {
                $null = $serviceManager.AddService2($ServiceId, $RegistrationFlags, '')
                Write-Host 'RegisterMicrosoftUpdate: registration succeeded'
                return $true
            }
            catch {
                Write-Host "RegisterMicrosoftUpdate: registration failed - $_"
                return $false
            }
            finally {
                if ($null -ne $serviceManager) {
                    try {
                        [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($serviceManager)
                    }
                    catch {
                        # Intentionally suppressing errors - COM cleanup failures are expected and safe
                        Write-Verbose "COM cleanup failed (expected/safe to ignore): $_" -Verbose:$false
                    }
                }
            }
        }
    }
}

try {
    Write-Verbose "RegisterMicrosoftUpdate: starting registration with $waitTimeoutSeconds second timeout" -Verbose

    # Start the job
    if ($TestHook) {
        $job = Start-ThreadJob -ScriptBlock $jobScript
    }
    else {
        $job = Start-ThreadJob -ScriptBlock $jobScript -ArgumentList $MicrosoftUpdateServiceId, $MicrosoftUpdateServiceRegistrationFlags
    }

    # Wait with timeout
    $completed = Wait-Job -Job $job -Timeout $waitTimeoutSeconds

    if ($completed) {
        $result = Receive-Job -Job $job
        Remove-Job -Job $job -Force

        if ($result -eq $true) {
            Write-Verbose "RegisterMicrosoftUpdate: completed successfully" -Verbose
        }
        else {
            Write-Verbose "RegisterMicrosoftUpdate: registration failed, continuing installation" -Verbose
        }
    }
    else {
        # Process timed out - stop the job and continue
        Write-Verbose "RegisterMicrosoftUpdate: timed out after $waitTimeoutSeconds seconds, continuing installation" -Verbose
        Stop-Job -Job $job
        Remove-Job -Job $job -Force
    }
}
catch {
    Write-Verbose "RegisterMicrosoftUpdate: unexpected error - $_, continuing installation" -Verbose
    # Ensure job cleanup to prevent resource leaks
    if ($job) {
        Stop-Job -Job $job -ErrorAction SilentlyContinue
        Remove-Job -Job $job -Force -ErrorAction SilentlyContinue
    }
}

# Always exit 0 so the MSI can complete
exit 0
