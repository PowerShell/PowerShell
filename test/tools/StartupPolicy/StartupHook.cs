// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Reflection;
using System.Threading;

/// <summary>
/// Controls policy discovery before the PowerShell entry point runs.
/// The .NET startup-hook protocol requires this type in the global namespace.
/// </summary>
public static class StartupHook
{
    /// <summary>
    /// Holds policy discovery until the parent process supplies the platform result.
    /// </summary>
    public static void Initialize()
    {
        string home = Environment.GetEnvironmentVariable("PWSH_POLICY_TEST_HOME");
        string mode = Environment.GetEnvironmentVariable("PWSH_POLICY_TEST_MODE");
        Assembly engine = Assembly.LoadFrom(Path.Combine(home, "System.Management.Automation.dll"));
        Type policy = engine.GetType("System.Management.Automation.Security.SystemPolicy", throwOnError: true);
        FieldInfo policyLock = GetField(policy, "s_systemLockdownPolicyLock");
        FieldInfo systemPolicy = GetField(policy, "s_systemLockdownPolicy");
        FieldInfo wldpPolicy = GetField(policy, "s_cachedWldpSystemPolicy");
        FieldInfo saferPolicy = GetField(policy, "s_cachedSaferSystemPolicy");
        FieldInfo debugOverride = GetField(policy, "s_allowDebugOverridePolicy");
        object synchronization = policyLock.GetValue(null);
        var held = new ManualResetEventSlim();

        var controller = new Thread(() =>
        {
            lock (synchronization)
            {
                systemPolicy.SetValue(null, null);
                debugOverride.SetValue(null, false);
                Environment.SetEnvironmentVariable("PWSH_POLICY_TEST_COMPLETE", "0");
                Console.WriteLine("__POLICY_HELD__");
                held.Set();

                if (Console.ReadLine() != "release")
                {
                    throw new InvalidOperationException("The parent did not release policy discovery.");
                }

                wldpPolicy.SetValue(null, Enum.Parse(Nullable.GetUnderlyingType(wldpPolicy.FieldType), mode));
                saferPolicy.SetValue(null, Enum.Parse(Nullable.GetUnderlyingType(saferPolicy.FieldType), "Allowed"));
                Environment.SetEnvironmentVariable("PWSH_POLICY_TEST_COMPLETE", "1");
                Console.WriteLine("__POLICY_RELEASED__");
            }

            Type initialState = engine.GetType("System.Management.Automation.Runspaces.InitialSessionState", throwOnError: true);
            object languageMode = GetField(initialState, "systemLanguageMode").GetValue(null);
            Console.WriteLine("__INITIAL_LANGUAGE_MODE__|" + languageMode);
            held.Dispose();
        });
        controller.Start();
        if (!held.Wait(TimeSpan.FromSeconds(30)))
        {
            throw new TimeoutException("The policy controller did not acquire the startup gate.");
        }
    }

    private static FieldInfo GetField(Type type, string name)
    {
        return type.GetField(name, BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(type.FullName, name);
    }
}
