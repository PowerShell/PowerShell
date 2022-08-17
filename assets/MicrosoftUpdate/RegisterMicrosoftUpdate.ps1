# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

param(
    [ValidateSet('Registration', 'Sleep', 'Fail')]
    $Scenario = 'Registration'
)

$sleep = 300
switch ($Scenario) {
    'Sleep' {
        $sleep = 10
        $jobScript = { Start-Sleep -Seconds 600 }
    }
    'Fail' {
        $jobScript = { throw "This job script should fail" }
    }
    default {
        $jobScript = {
            # This registers Microsoft Update via a predifened GUID with the Windows Update Agent.
            # https://docs.microsoft.com/en-us/windows/win32/wua_sdk/opt-in-to-microsoft-update

            $serviceManager = (New-Object -ComObject Microsoft.Update.ServiceManager)
            $isRegistered = $serviceManager.QueryServiceRegistration('7971f918-a847-4430-9279-4a52d1efe18d').Service.IsRegisteredWithAu

            if (!$isRegistered) {
                Write-Verbose -Verbose "Opting into Microsoft Update as the Autmatic Update Service"
                # 7 is the combination of asfAllowPendingRegistration, asfAllowOnlineRegistration, asfRegisterServiceWithAU
                # AU means Automatic Updates
                $null = $serviceManager.AddService2('7971f918-a847-4430-9279-4a52d1efe18d', 7, '').IsPendingRegistrationWithAu
            }
            else {
                Write-Verbose -Verbose "Microsoft Update is already registered for Automatic Updates"
            }

            $isRegistered = $serviceManager.QueryServiceRegistration('7971f918-a847-4430-9279-4a52d1efe18d').Service.IsRegisteredWithAu

            # Return if it was successful, which is the opposite of Pending.
            return $isRegistered
        }
    }
}

Write-Verbose "Running job script: $jobScript" -Verbose
$job = Start-ThreadJob -ScriptBlock $jobScript

Write-Verbose "Waiting on Job for $sleep seconds" -Verbose
$null = Wait-Job -Job $job -Timeout $sleep

if ($job.State -ne 'Running') {
    Write-Verbose "Job finished.  State: $($job.State)" -Verbose
    $result = Receive-Job -Job $job -Verbose
    Write-Verbose "Result: $result" -Verbose
    if ($result) {
        Write-Verbose "Registration succeeded" -Verbose
        exit 0
    }
    else {
        Write-Verbose "Registration failed" -Verbose
        exit 1
    }
}
else {
    Write-Verbose "Job timed out" -Verbose
    Write-Verbose "Stopping Job.  State: $($job.State)" -Verbose
    Stop-Job -Job $job
    exit 258
}
