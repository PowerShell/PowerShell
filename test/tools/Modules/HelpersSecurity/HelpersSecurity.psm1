# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

if ($IsWindows)
{
    Import-Module HelpersCommon

    $code = @'

    #region Using directives

    using System;
    using System.Management.Automation;

    #endregion

    /// <summary>Adds a new type to the Application Domain</summary>
    [Cmdlet("Invoke", "LanguageModeTestingSupportCmdlet")]
    public sealed class InvokeLanguageModeTestingSupportCmdlet : PSCmdlet
    {
        [Parameter()]
        public SwitchParameter EnableFullLanguageMode { get; set; }

        [Parameter()]
        public SwitchParameter SetLockdownMode { get; set; }

        [Parameter()]
        public SwitchParameter RevertLockdownMode { get; set; }

        protected override void BeginProcessing()
        {
            if (EnableFullLanguageMode)
            {
                SessionState.LanguageMode = PSLanguageMode.FullLanguage;
            }

#if DEBUG
            if (SetLockdownMode)
            {
                Environment.SetEnvironmentVariable("__PSLockdownPolicy", "0x80000007", EnvironmentVariableTarget.Machine);
            }

            if (RevertLockdownMode)
            {
                Environment.SetEnvironmentVariable("__PSLockdownPolicy", null, EnvironmentVariableTarget.Machine);
            }
#else
            if (SetLockdownMode || RevertLockdownMode)
            {
                throw new InvalidOperationException("__PSLockdownPolicy is only available in DEBUG builds. This is a test hook and should not be used in production.");
            }
#endif
        }
    }
'@

    if (-not (Get-Command Invoke-LanguageModeTestingSupportCmdlet -ErrorAction Ignore))
    {
        $moduleName = Get-RandomFileName
        $moduleDirectory = Join-Path $TestDrive\Modules $moduleName
        if (-not (Test-Path $moduleDirectory))
        {
            $null = New-Item -ItemType Directory $moduleDirectory -Force
        }

        try
        {
            Add-Type -TypeDefinition $code -OutputAssembly $moduleDirectory\TestCmdletForConstrainedLanguage.dll -ErrorAction Ignore
        } catch {}

        Import-Module -Name $moduleDirectory\TestCmdletForConstrainedLanguage.dll
    }
}
