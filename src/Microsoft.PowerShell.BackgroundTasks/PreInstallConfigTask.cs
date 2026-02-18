// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Microsoft.Win32;

namespace Microsoft.PowerShell.BackgroundTasks
{
    /// <summary>
    /// PreInstallConfigTask runs after PowerShell MSIX package is provisioned (OEM scenario)
    /// or installed for the first time, before the user launches the app.
    /// </summary>
    public sealed class PreInstallConfigTask : IBackgroundTask
    {
        private const string EventSourceName = "PowerShell-PreInstallTask";
        private const string LogSubPath = @"Microsoft\PowerShell\OEM";
        private const string RegistryKeyPath = @"SOFTWARE\Microsoft\PowerShell\OEM\PreInstallTasks";

        /// <summary>
        /// Entry point for the background task.
        /// </summary>
        /// <param name="taskInstance">Task instance provided by Windows</param>
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral? deferral = null;
            CancellationTokenSource cts = new CancellationTokenSource();

            try
            {
                // Get deferral to allow async operations
                deferral = taskInstance.GetDeferral();

                // Register cancellation handler
                taskInstance.Canceled += (sender, reason) =>
                {
                    LogMessage($"Task canceled: {reason}");
                    cts.Cancel();
                };

                LogMessage("PreInstallConfigTask started");

                // Run initialization tasks
                await InitializeDefaultConfigurationAsync(cts.Token);

                LogMessage("PreInstallConfigTask completed successfully");
            }
            catch (TaskCanceledException)
            {
                LogMessage("PreInstallConfigTask was canceled");
            }
            catch (Exception ex)
            {
                // Log error but don't throw - failed tasks shouldn't block deployment
                LogError($"PreInstallConfigTask failed: {ex.Message}", ex);
            }
            finally
            {
                deferral?.Complete();
                cts.Dispose();
            }
        }

        /// <summary>
        /// Perform initialization tasks after installation.
        /// </summary>
        private async Task InitializeDefaultConfigurationAsync(CancellationToken cancellationToken)
        {
            // Create marker file for verification
            await CreateMarkerFileAsync(cancellationToken);

            // Create registry entries for tracking
            CreateRegistryEntries();

            // Initialize default PowerShell configuration directories
            InitializeConfigurationDirectories();

            // Placeholder for future initialization tasks:
            // - Set up default module paths
            // - Create default profile templates
            // - Configure telemetry settings
            // - Initialize package cache
        }

        /// <summary>
        /// Creates a marker file to verify task execution.
        /// </summary>
        private static async Task CreateMarkerFileAsync(CancellationToken cancellationToken)
        {
            try
            {
                string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string logDirectory = Path.Combine(programData, LogSubPath);
                string logPath = Path.Combine(logDirectory, "PreInstallTask.log");

                Directory.CreateDirectory(logDirectory);

                string logEntry = $"[{DateTime.UtcNow:O}] PreInstallConfigTask executed\n" +
                                  $"User Context: {Environment.UserName}\n" +
                                  $"Domain: {Environment.UserDomainName}\n" +
                                  $"Machine Name: {Environment.MachineName}\n" +
                                  $"OS Version: {Environment.OSVersion}\n" +
                                  $"CLR Version: {Environment.Version}\n\n";

                await File.AppendAllTextAsync(logPath, logEntry, cancellationToken);

                LogMessage($"Marker file created at: {logPath}");
            }
            catch (Exception ex)
            {
                LogError($"Failed to create marker file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Creates registry entries to track task execution.
        /// </summary>
        private static void CreateRegistryEntries()
        {
            try
            {
                using (RegistryKey? key = Registry.LocalMachine.CreateSubKey(RegistryKeyPath))
                {
                    if (key != null)
                    {
                        key.SetValue("LastRun", DateTime.UtcNow.ToString("O"), RegistryValueKind.String);
                        key.SetValue("Status", "Completed", RegistryValueKind.String);
                        key.SetValue("Version", "1.0", RegistryValueKind.String);

                        LogMessage("Registry entries created successfully");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to create registry entries: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Initializes default PowerShell configuration directories.
        /// </summary>
        private static void InitializeConfigurationDirectories()
        {
            try
            {
                string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string psConfigPath = Path.Combine(programData, "PowerShell");

                // Create standard PowerShell directories if they don't exist
                Directory.CreateDirectory(psConfigPath);
                Directory.CreateDirectory(Path.Combine(psConfigPath, "Modules"));
                Directory.CreateDirectory(Path.Combine(psConfigPath, "ModuleAnalysisCache"));

                LogMessage("Configuration directories initialized");
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize configuration directories: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Logs a message to the marker file.
        /// </summary>
        private static void LogMessage(string message)
        {
            try
            {
                string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string logPath = Path.Combine(programData, LogSubPath, "PreInstallTask.log");

                Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n");
            }
            catch
            {
                // Ignore logging errors
            }
        }

        /// <summary>
        /// Logs an error to the marker file.
        /// </summary>
        private static void LogError(string message, Exception ex)
        {
            try
            {
                string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string logPath = Path.Combine(programData, LogSubPath, "PreInstallTask.log");

                Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
                File.AppendAllText(logPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {message}\n" +
                    $"Exception: {ex}\n\n");
            }
            catch
            {
                // Ignore logging errors
            }
        }
    }
}
