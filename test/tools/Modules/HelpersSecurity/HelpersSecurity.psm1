# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

if ($IsWindows)
{
    Import-Module HelpersCommon

    $code = @'

    #region Using directives

    using System;
    using System.Globalization;
    using System.Reflection;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Security;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Management.Automation;

    #endregion

    /// <summary>Adds a new type to the Application Domain</summary>
    [Cmdlet("Invoke", "LanguageModeTestingSupportCmdlet")]
    public sealed class InvokeLanguageModeTestingSupportCmdlet : PSCmdlet
    {
        [Parameter()]
        public SwitchParameter EnableFullLanguageMode
        {
            get { return enableFullLanguageMode; }
            set { enableFullLanguageMode = value; }
        }
        private SwitchParameter enableFullLanguageMode;

        [Parameter()]
        public SwitchParameter SetLockdownMode
        {
            get { return setLockdownMode; }
            set { setLockdownMode = value; }
        }
        private SwitchParameter setLockdownMode;

        [Parameter()]
        public SwitchParameter RevertLockdownMode
        {
            get { return revertLockdownMode; }
            set { revertLockdownMode = value; }
        }
        private SwitchParameter revertLockdownMode;

        protected override void BeginProcessing()
        {
            if (enableFullLanguageMode)
            {
                SessionState.LanguageMode = PSLanguageMode.FullLanguage;
            }

            if (setLockdownMode)
            {
                Environment.SetEnvironmentVariable("__PSLockdownPolicy", "0x80000007", EnvironmentVariableTarget.Machine);
            }

            if (revertLockdownMode)
            {
                Environment.SetEnvironmentVariable("__PSLockdownPolicy", null, EnvironmentVariableTarget.Machine);
            }
        }
    }
'@

    if (-not (Get-Command Invoke-LanguageModeTestingSupportCmdlet -ErrorAction Ignore))
    {
        $moduleName = Get-RandomFileName
        $moduleDirectory = join-path $TestDrive\Modules $moduleName
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
