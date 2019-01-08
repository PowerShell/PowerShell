// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;

namespace Microsoft.PowerShell.ScheduledJob
{
    /// <summary>
    /// Base class for ScheduledJob cmdlets.
    /// </summary>
    public abstract class ScheduleJobCmdletBase : PSCmdlet
    {
        #region Cmdlet Strings

        /// <summary>
        /// Scheduled job module name.
        /// </summary>
        protected const string ModuleName = "PSScheduledJob";

        #endregion

        #region Utility Methods

        /// <summary>
        /// Makes delegate callback call for each scheduledjob definition object found.
        /// </summary>
        /// <param name="itemFound">Callback delegate for each discovered item.</param>
        internal void FindAllJobDefinitions(
            Action<ScheduledJobDefinition> itemFound)
        {
            Dictionary<string, Exception> errors = ScheduledJobDefinition.RefreshRepositoryFromStore((definition) =>
                {
                    if (ValidateJobDefinition(definition))
                    {
                        itemFound(definition);
                    }
                });
            HandleAllLoadErrors(errors);
        }

        /// <summary>
        /// Returns a single ScheduledJobDefinition object from the local
        /// scheduled job definition repository corresponding to the provided id.
        /// </summary>
        /// <param name="id">Local repository scheduled job definition id.</param>
        /// <param name="writeErrorsAndWarnings">Errors/warnings are written to host.</param>
        /// <returns>ScheduledJobDefinition object.</returns>
        internal ScheduledJobDefinition GetJobDefinitionById(
            Int32 id,
            bool writeErrorsAndWarnings = true)
        {
            Dictionary<string, Exception> errors = ScheduledJobDefinition.RefreshRepositoryFromStore(null);
            HandleAllLoadErrors(errors);

            foreach (var definition in ScheduledJobDefinition.Repository.Definitions)
            {
                if (definition.Id == id &&
                    ValidateJobDefinition(definition))
                {
                    return definition;
                }
            }

            if (writeErrorsAndWarnings)
            {
                WriteDefinitionNotFoundByIdError(id);
            }

            return null;
        }

        /// <summary>
        /// Returns an array of ScheduledJobDefinition objects from the local
        /// scheduled job definition repository corresponding to the provided Ids.
        /// </summary>
        /// <param name="ids">Local repository scheduled job definition ids.</param>
        /// <param name="writeErrorsAndWarnings">Errors/warnings are written to host.</param>
        /// <returns>List of ScheduledJobDefinition objects.</returns>
        internal List<ScheduledJobDefinition> GetJobDefinitionsById(
            Int32[] ids,
            bool writeErrorsAndWarnings = true)
        {
            Dictionary<string, Exception> errors = ScheduledJobDefinition.RefreshRepositoryFromStore(null);
            HandleAllLoadErrors(errors);

            List<ScheduledJobDefinition> definitions = new List<ScheduledJobDefinition>();
            HashSet<Int32> findIds = new HashSet<Int32>(ids);
            foreach (var definition in ScheduledJobDefinition.Repository.Definitions)
            {
                if (findIds.Contains(definition.Id) &&
                    ValidateJobDefinition(definition))
                {
                    definitions.Add(definition);
                    findIds.Remove(definition.Id);
                }
            }

            if (writeErrorsAndWarnings)
            {
                foreach (int id in findIds)
                {
                    WriteDefinitionNotFoundByIdError(id);
                }
            }

            return definitions;
        }

        /// <summary>
        /// Makes delegate callback call for each scheduledjob definition object found.
        /// </summary>
        /// <param name="ids">Local repository scheduled job definition ids.</param>
        /// <param name="itemFound">Callback delegate for each discovered item.</param>
        /// <param name="writeErrorsAndWarnings">Errors/warnings are written to host.</param>
        internal void FindJobDefinitionsById(
            Int32[] ids,
            Action<ScheduledJobDefinition> itemFound,
            bool writeErrorsAndWarnings = true)
        {
            HashSet<Int32> findIds = new HashSet<Int32>(ids);
            Dictionary<string, Exception> errors = ScheduledJobDefinition.RefreshRepositoryFromStore((definition) =>
                {
                    if (findIds.Contains(definition.Id) &&
                        ValidateJobDefinition(definition))
                    {
                        itemFound(definition);
                        findIds.Remove(definition.Id);
                    }
                });

            HandleAllLoadErrors(errors);

            if (writeErrorsAndWarnings)
            {
                foreach (Int32 id in findIds)
                {
                    WriteDefinitionNotFoundByIdError(id);
                }
            }
        }

        /// <summary>
        /// Returns an array of ScheduledJobDefinition objects from the local
        /// scheduled job definition repository corresponding to the given name.
        /// </summary>
        /// <param name="name">Scheduled job definition name.</param>
        /// <param name="writeErrorsAndWarnings">Errors/warnings are written to host.</param>
        /// <returns>ScheduledJobDefinition object.</returns>
        internal ScheduledJobDefinition GetJobDefinitionByName(
            string name,
            bool writeErrorsAndWarnings = true)
        {
            Dictionary<string, Exception> errors = ScheduledJobDefinition.RefreshRepositoryFromStore(null);

            // Look for match.
            WildcardPattern namePattern = new WildcardPattern(name, WildcardOptions.IgnoreCase);
            foreach (var definition in ScheduledJobDefinition.Repository.Definitions)
            {
                if (namePattern.IsMatch(definition.Name) &&
                    ValidateJobDefinition(definition))
                {
                    return definition;
                }
            }

            // Look for load error.
            foreach (var error in errors)
            {
                if (namePattern.IsMatch(error.Key))
                {
                    HandleLoadError(error.Key, error.Value);
                }
            }

            if (writeErrorsAndWarnings)
            {
                WriteDefinitionNotFoundByNameError(name);
            }

            return null;
        }

        /// <summary>
        /// Returns an array of ScheduledJobDefinition objects from the local
        /// scheduled job definition repository corresponding to the given names.
        /// </summary>
        /// <param name="names">Scheduled job definition names.</param>
        /// <param name="writeErrorsAndWarnings">Errors/warnings are written to host.</param>
        /// <returns>List of ScheduledJobDefinition objects.</returns>
        internal List<ScheduledJobDefinition> GetJobDefinitionsByName(
            string[] names,
            bool writeErrorsAndWarnings = true)
        {
            Dictionary<string, Exception> errors = ScheduledJobDefinition.RefreshRepositoryFromStore(null);

            List<ScheduledJobDefinition> definitions = new List<ScheduledJobDefinition>();
            foreach (string name in names)
            {
                WildcardPattern namePattern = new WildcardPattern(name, WildcardOptions.IgnoreCase);

                // Look for match.
                bool nameFound = false;
                foreach (var definition in ScheduledJobDefinition.Repository.Definitions)
                {
                    if (namePattern.IsMatch(definition.Name) &&
                        ValidateJobDefinition(definition))
                    {
                        nameFound = true;
                        definitions.Add(definition);
                    }
                }

                // Look for load error.
                foreach (var error in errors)
                {
                    if (namePattern.IsMatch(error.Key))
                    {
                        HandleLoadError(error.Key, error.Value);
                    }
                }

                if (!nameFound && writeErrorsAndWarnings)
                {
                    WriteDefinitionNotFoundByNameError(name);
                }
            }

            return definitions;
        }

        /// <summary>
        /// Makes delegate callback call for each scheduledjob definition object found.
        /// </summary>
        /// <param name="names">Scheduled job definition names.</param>
        /// <param name="itemFound">Callback delegate for each discovered item.</param>
        /// <param name="writeErrorsAndWarnings">Errors/warnings are written to host.</param>
        internal void FindJobDefinitionsByName(
            string[] names,
            Action<ScheduledJobDefinition> itemFound,
            bool writeErrorsAndWarnings = true)
        {
            HashSet<string> notFoundNames = new HashSet<string>(names);
            Dictionary<string, WildcardPattern> patterns = new Dictionary<string, WildcardPattern>();
            foreach (string name in names)
            {
                if (!patterns.ContainsKey(name))
                {
                    patterns.Add(name, new WildcardPattern(name, WildcardOptions.IgnoreCase));
                }
            }

            Dictionary<string, Exception> errors = ScheduledJobDefinition.RefreshRepositoryFromStore((definition) =>
                {
                    foreach (var item in patterns)
                    {
                        if (item.Value.IsMatch(definition.Name) &&
                            ValidateJobDefinition(definition))
                        {
                            itemFound(definition);
                            if (notFoundNames.Contains(item.Key))
                            {
                                notFoundNames.Remove(item.Key);
                            }
                        }
                    }
                });

            // Look for load error.
            foreach (var error in errors)
            {
                foreach (var item in patterns)
                {
                    if (item.Value.IsMatch(error.Key))
                    {
                        HandleLoadError(error.Key, error.Value);
                    }
                }
            }

            if (writeErrorsAndWarnings)
            {
                foreach (var name in notFoundNames)
                {
                    WriteDefinitionNotFoundByNameError(name);
                }
            }
        }

        /// <summary>
        /// Writes a "Trigger not found" error to host.
        /// </summary>
        /// <param name="notFoundId">Trigger Id not found.</param>
        /// <param name="definitionName">ScheduledJobDefinition name.</param>
        /// <param name="errorObject">Error object.</param>
        internal void WriteTriggerNotFoundError(
            Int32 notFoundId,
            string definitionName,
            object errorObject)
        {
            string msg = StringUtil.Format(ScheduledJobErrorStrings.TriggerNotFound, notFoundId, definitionName);
            Exception reason = new RuntimeException(msg);
            ErrorRecord errorRecord = new ErrorRecord(reason, "ScheduledJobTriggerNotFound", ErrorCategory.ObjectNotFound, errorObject);
            WriteError(errorRecord);
        }

        /// <summary>
        /// Writes a "Definition not found for Id" error to host.
        /// </summary>
        /// <param name="defId">Definition Id.</param>
        internal void WriteDefinitionNotFoundByIdError(
            Int32 defId)
        {
            string msg = StringUtil.Format(ScheduledJobErrorStrings.DefinitionNotFoundById, defId);
            Exception reason = new RuntimeException(msg);
            ErrorRecord errorRecord = new ErrorRecord(reason, "ScheduledJobDefinitionNotFoundById", ErrorCategory.ObjectNotFound, null);
            WriteError(errorRecord);
        }

        /// <summary>
        /// Writes a "Definition not found for Name" error to host.
        /// </summary>
        /// <param name="name">Definition Name.</param>
        internal void WriteDefinitionNotFoundByNameError(
            string name)
        {
            string msg = StringUtil.Format(ScheduledJobErrorStrings.DefinitionNotFoundByName, name);
            Exception reason = new RuntimeException(msg);
            ErrorRecord errorRecord = new ErrorRecord(reason, "ScheduledJobDefinitionNotFoundByName", ErrorCategory.ObjectNotFound, null);
            WriteError(errorRecord);
        }

        /// <summary>
        /// Writes a "Load from job store" error to host.
        /// </summary>
        /// <param name="name">Scheduled job definition name.</param>
        /// <param name="error">Exception thrown during loading.</param>
        internal void WriteErrorLoadingDefinition(string name, Exception error)
        {
            string msg = StringUtil.Format(ScheduledJobErrorStrings.CantLoadDefinitionFromStore, name);
            Exception reason = new RuntimeException(msg, error);
            ErrorRecord errorRecord = new ErrorRecord(reason, "CantLoadScheduledJobDefinitionFromStore", ErrorCategory.InvalidOperation, null);
            WriteError(errorRecord);
        }

        /// <summary>
        /// Creates a Once job trigger with provided repetition interval and an
        /// infinite duration, and adds the trigger to the provided scheduled job
        /// definition object.
        /// </summary>
        /// <param name="definition">ScheduledJobDefinition.</param>
        /// <param name="repInterval">Rep interval.</param>
        /// <param name="save">Save definition change.</param>
        internal static void AddRepetitionJobTriggerToDefinition(
            ScheduledJobDefinition definition,
            TimeSpan repInterval,
            bool save)
        {
            if (definition == null)
            {
                throw new PSArgumentNullException("definition");
            }

            TimeSpan repDuration = TimeSpan.MaxValue;

            // Validate every interval value.
            if (repInterval < TimeSpan.Zero || repDuration < TimeSpan.Zero)
            {
                throw new PSArgumentException(ScheduledJobErrorStrings.InvalidRepetitionParamValues);
            }

            if (repInterval < TimeSpan.FromMinutes(1))
            {
                throw new PSArgumentException(ScheduledJobErrorStrings.InvalidRepetitionIntervalValue);
            }

            if (repInterval > repDuration)
            {
                throw new PSArgumentException(ScheduledJobErrorStrings.InvalidRepetitionInterval);
            }

            // Create job trigger.
            var trigger = ScheduledJobTrigger.CreateOnceTrigger(
                DateTime.Now,
                TimeSpan.Zero,
                repInterval,
                repDuration,
                0,
                true);

            definition.AddTriggers(new ScheduledJobTrigger[] { trigger }, save);
        }

        #endregion

        #region Private Methods

        private void HandleAllLoadErrors(Dictionary<string, Exception> errors)
        {
            foreach (var error in errors)
            {
                HandleLoadError(error.Key, error.Value);
            }
        }

        private void HandleLoadError(string name, Exception e)
        {
            if (e is System.IO.IOException ||
                e is System.Xml.XmlException ||
                e is System.TypeInitializationException ||
                e is System.Runtime.Serialization.SerializationException ||
                e is System.ArgumentNullException)
            {
                // Remove the corrupted scheduled job definition and
                // notify user with error message.
                ScheduledJobDefinition.RemoveDefinition(name);
                WriteErrorLoadingDefinition(name, e);
            }
        }

        private void ValidateJobDefinitions()
        {
            foreach (var definition in ScheduledJobDefinition.Repository.Definitions)
            {
                ValidateJobDefinition(definition);
            }
        }

        /// <summary>
        /// Validates the job definition object retrieved from store by syncing
        /// its data with the corresponding Task Scheduler task.  If no task
        /// is found then validation fails.
        /// </summary>
        /// <param name="definition"></param>
        /// <returns></returns>
        private bool ValidateJobDefinition(ScheduledJobDefinition definition)
        {
            Exception ex = null;
            try
            {
                definition.SyncWithWTS();
            }
            catch (System.IO.DirectoryNotFoundException e)
            {
                ex = e;
            }
            catch (System.IO.FileNotFoundException e)
            {
                ex = e;
            }
            catch (System.ArgumentNullException e)
            {
                ex = e;
            }

            if (ex != null)
            {
                WriteErrorLoadingDefinition(definition.Name, ex);
            }

            return (ex == null);
        }

        #endregion
    }
}
