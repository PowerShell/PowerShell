// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands.ShowCommandExtension
{
    /// <summary>
    /// Implements a facade around CommandInfo and its deserialized counterpart.
    /// </summary>
    public class ShowCommandCommandInfo
    {
        /// <summary>
        /// Creates an instance of the ShowCommandCommandInfo class based on a CommandInfo object.
        /// </summary>
        /// <param name="other">
        /// The object to wrap.
        /// </param>
        public ShowCommandCommandInfo(CommandInfo other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

            this.Name = other.Name;
            this.ModuleName = other.ModuleName;
            this.CommandType = other.CommandType;
            this.Definition = other.Definition;

            // In a runspace with restricted security settings we catch
            // PSSecurityException when accessing ParameterSets because
            // ExternalScript commands may be evaluated.
            try
            {
                this.ParameterSets =
                    other.ParameterSets
                        .Select(x => new ShowCommandParameterSetInfo(x))
                        .ToList()
                        .AsReadOnly();
            }
            catch (PSSecurityException)
            {
                // Since we can't access the parameter sets of this command,
                // populate the ParameterSets property with an empty list
                // so that consumers don't trip on a null value.
                this.ParameterSets = new List<ShowCommandParameterSetInfo>().AsReadOnly();
            }
            catch (ParseException)
            {
                // Could not parse the given command so don't continue initializing it
                this.ParameterSets = new List<ShowCommandParameterSetInfo>().AsReadOnly();
            }

            if (other.Module != null)
            {
                this.Module = new ShowCommandModuleInfo(other.Module);
            }
        }

        /// <summary>
        /// Creates an instance of the ShowCommandCommandInfo class based on a PSObject object.
        /// </summary>
        /// <param name="other">
        /// The object to wrap.
        /// </param>
        public ShowCommandCommandInfo(PSObject other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

            this.Name = other.Members["Name"].Value as string;
            this.ModuleName = other.Members["ModuleName"].Value as string;
            this.Definition = other.Members["Definition"].Value as string;
            this.ParameterSets = other.Members["ParameterSets"].Value as ICollection<ShowCommandParameterSetInfo>;
            if (this.ParameterSets != null)
            {
                // Simple case - the objects are still live because they came from in-proc. Just cast them back
                this.CommandType = (CommandTypes)(other.Members["CommandType"].Value);
                this.Module = other.Members["Module"].Value as ShowCommandModuleInfo;
            }
            else
            {
                // Objects came in their deserialized form - recreate the object graph
                this.CommandType = (CommandTypes)((other.Members["CommandType"].Value as PSObject).BaseObject);

                var parameterSets = (other.Members["ParameterSets"].Value as PSObject).BaseObject as System.Collections.ArrayList;
                this.ParameterSets = GetObjectEnumerable(parameterSets).Cast<PSObject>().Select(x => new ShowCommandParameterSetInfo(x)).ToList().AsReadOnly();

                if (other.Members["Module"] != null && other.Members["Module"].Value as PSObject != null)
                {
                    this.Module = new ShowCommandModuleInfo(other.Members["Module"].Value as PSObject);
                }
            }
        }

        /// <summary>
        /// Builds a strongly typed IEnumerable{object} out of an IEnumerable.
        /// </summary>
        /// <param name="enumerable">
        /// The object to enumerate.
        /// </param>
        internal static IEnumerable<object> GetObjectEnumerable(System.Collections.IEnumerable enumerable)
        {
            foreach (object obj in enumerable)
            {
                yield return obj;
            }
        }

        /// <summary>
        /// A string representing the definition of the command.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// A string representing module the command belongs to.
        /// </summary>
        public string ModuleName { get; private set; }

        /// <summary>
        /// A reference to the module the command came from.
        /// </summary>
        public ShowCommandModuleInfo Module { get; private set; }

        /// <summary>
        /// An enumeration of the command types this command belongs to.
        /// </summary>
        public CommandTypes CommandType { get; private set; }

        /// <summary>
        /// A string representing the definition of the command.
        /// </summary>
        public string Definition { get; private set; }

        /// <summary>
        /// A string representing the definition of the command.
        /// </summary>
        public ICollection<ShowCommandParameterSetInfo> ParameterSets { get; private set; }
    }
}
