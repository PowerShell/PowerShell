// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Reflection;

public static class StartupHook
{
    public static void Initialize()
    {
        try
        {
            // A current-user experimental feature list takes precedence over -SettingsFile.
            // Ignore it in the child process before ConsoleShell initializes the feature list.
            var automation = Assembly.Load("System.Management.Automation");
            var configType = automation.GetType("System.Management.Automation.Configuration.PowerShellConfig", throwOnError: true);
            var config = configType.GetField("Instance", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            configType.GetField("perUserConfigFile", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(config, null);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            Environment.Exit(1);
        }
    }
}
