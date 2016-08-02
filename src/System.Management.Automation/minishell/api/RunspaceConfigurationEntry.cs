/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

#pragma warning disable 1634, 1691
#pragma warning disable 56506

namespace System.Management.Automation.Runspaces
{
    /// <summary>
    /// Define class for runspace configuration entry. 
    /// </summary>
    /// <remarks>
    /// This abstract class is to be derived internally by Monad for different 
    /// runspace configuration entries only. Developers should not derive from 
    /// this class. 
    /// </remarks>
#if CORECLR
    internal
#else
    public
#endif
    abstract class RunspaceConfigurationEntry
    {
        /// <summary>
        /// Initiate an instance of runspace configuration entry. 
        /// </summary>
        /// <param name="name">Name for the runspace configuration entry</param>
        /// <!--
        /// This is meant to be called by derived class only. It doesn't make sense to 
        /// directly create an instance of this class. 
        /// -->
        protected RunspaceConfigurationEntry(string name)
        {
            if (String.IsNullOrEmpty(name) || String.IsNullOrEmpty(name.Trim()))
            {
                throw PSTraceSource.NewArgumentNullException("name");
            }

            Name = name.Trim();
        }

        /// <summary>
        /// Initiate an instance of runspace configuration entry. 
        /// </summary>
        /// <param name="name">Name for the runspace configuration entry</param>
        /// <param name="psSnapin">The name of the PSSnapin the entry comes from.</param>
        /// <!--
        /// This is meant to be called by derived class only. It doesn't make sense to 
        /// directly create an instance of this class. 
        /// -->
        internal RunspaceConfigurationEntry(string name, PSSnapInInfo psSnapin)
        {
            if (String.IsNullOrEmpty(name) || String.IsNullOrEmpty(name.Trim()))
            {
                throw PSTraceSource.NewArgumentNullException("name");
            }

            Name = name.Trim();

            if (psSnapin == null)
            {
                throw PSTraceSource.NewArgumentException("psSnapin");
            }

            PSSnapIn = psSnapin;
        }


        /// <summary>
        /// Gets name of configuration entry
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets name of PSSnapin that this configuration entry belongs to. 
        /// </summary>
        public PSSnapInInfo PSSnapIn { get; } = null;

        internal bool _builtIn = false;
        /// <summary>
        /// Get whether this entry is a built-in entry.
        /// </summary>
        public bool BuiltIn
        {
            get
            {
                return _builtIn;
            }
        }

        internal UpdateAction _action = UpdateAction.None;
        internal UpdateAction Action
        {
            get
            {
                return _action;
            }
        }
    }

    /// <summary>
    /// Defines class for type configuration entry.
    /// </summary>
#if CORECLR
    internal
#else
    public
#endif
    sealed class TypeConfigurationEntry : RunspaceConfigurationEntry
    {
        /// <summary>
        /// Initiate an instance for type configuration entry.
        /// </summary>
        /// <param name="name">Name of the type configuration entry</param>
        /// <param name="fileName">File name that contains the types configuration information.</param>
        /// <exception cref="ArgumentException">when <paramref name="fileName"/> is null or empty</exception>
        public TypeConfigurationEntry(string name, string fileName)
            : base(name)
        {
            if (String.IsNullOrEmpty(fileName) || String.IsNullOrEmpty(fileName.Trim()))
            {
                throw PSTraceSource.NewArgumentException("fileName");
            }

            FileName = fileName.Trim();
        }

        /// <summary>
        /// Initiate an instance for type configuration entry.
        /// </summary>
        /// <param name="typeData">TypeData instance</param>
        /// <param name="isRemove">Specify the operation with the typedata</param>
        public TypeConfigurationEntry(TypeData typeData, bool isRemove)
            : base("*")
        {
            if (typeData == null)
            {
                throw PSTraceSource.NewArgumentException("typeData");
            }

            TypeData = typeData;
            IsRemove = isRemove;
        }

        /// <summary>
        /// Initiate an instance for type configuration entry.
        /// </summary>
        /// <param name="name">Name of the type configuration entry</param>
        /// <param name="fileName">File name that contains the types configuration information.</param>
        /// <param name="psSnapinInfo">PSSnapin from which type info comes.</param>
        /// <exception cref="ArgumentException">when <paramref name="fileName"/> is null, empty or does not end in .ps1xml</exception>
        internal TypeConfigurationEntry(string name, string fileName, PSSnapInInfo psSnapinInfo)
            : base(name, psSnapinInfo)
        {
            if (String.IsNullOrEmpty(fileName) || String.IsNullOrEmpty(fileName.Trim()))
            {
                throw PSTraceSource.NewArgumentException("fileName");
            }

            FileName = fileName.Trim();
        }

        /// <summary>
        /// Initiate an instance for type configuration entry.
        /// </summary>
        /// <param name="fileName">File name that contains the types configuration information.</param>
        /// <exception cref="ArgumentException">when <paramref name="fileName"/> is null, empty or does not end in .ps1xml</exception>
        public TypeConfigurationEntry(string fileName)
            : base(fileName)
        {
            if (String.IsNullOrEmpty(fileName) || String.IsNullOrEmpty(fileName.Trim()))
            {
                throw PSTraceSource.NewArgumentException("fileName");
            }
#pragma warning suppress 56506
            FileName = fileName.Trim();
        }

        /// <summary>
        /// Gets file name that contains the types configuration information.
        /// </summary>
        /// <value></value>
        public string FileName { get; }

        /// <summary>
        /// Get the strong type data contains the type configuration information
        /// </summary>
        public TypeData TypeData { get; }

        /// <summary>
        /// Set to true if the strong type data is to be removed
        /// </summary>
        public bool IsRemove { get; }
    }

    /// <summary>
    /// Defines class for type configuration entry.
    /// </summary>
#if CORECLR
    internal
#else
    public
#endif
    sealed class FormatConfigurationEntry : RunspaceConfigurationEntry
    {
        /// <summary>
        /// Initiate an instance for type configuration entry.
        /// </summary>
        /// <param name="name">Name of the format configuration entry</param>
        /// <param name="fileName">File name that contains the format configuration information.</param>
        /// <exception cref="ArgumentException">when <paramref name="fileName"/> is null or empty</exception>
        public FormatConfigurationEntry(string name, string fileName)
            : base(name)
        {
            if (String.IsNullOrEmpty(fileName) || String.IsNullOrEmpty(fileName.Trim()))
            {
                throw PSTraceSource.NewArgumentException("fileName");
            }

            FileName = fileName.Trim();
        }

        /// <summary>
        /// Initiate an instance for Format configuration entry.
        /// </summary>
        /// <param name="name">Name of the Format configuration entry</param>
        /// <param name="fileName">File name that contains the Formats configuration information.</param>
        /// <param name="psSnapinInfo">PSSnapin from which the format comes.</param>
        /// <exception cref="ArgumentException">when <paramref name="fileName"/> is null, empty or does not end in .ps1xml</exception>
        internal FormatConfigurationEntry(string name, string fileName, PSSnapInInfo psSnapinInfo)
            : base(name, psSnapinInfo)
        {
            if (String.IsNullOrEmpty(fileName) || String.IsNullOrEmpty(fileName.Trim()))
            {
                throw PSTraceSource.NewArgumentException("fileName");
            }

            FileName = fileName.Trim();
        }

        /// <summary>
        /// Initiate an instance for type configuration entry.
        /// </summary>
        /// <param name="fileName">File name that contains the format configuration information.</param>
        /// <exception cref="ArgumentException">when <paramref name="fileName"/> is null or empty</exception>
        public FormatConfigurationEntry(string fileName)
            : base(fileName)
        {
            if (String.IsNullOrEmpty(fileName) || String.IsNullOrEmpty(fileName.Trim()))
            {
                throw PSTraceSource.NewArgumentException("fileName");
            }
#pragma warning suppress 56506
            FileName = fileName.Trim();
        }

        /// <summary>
        /// Initiate an instance for type configuration entry.
        /// </summary>
        /// <param name="typeDefinition"></param>
        public FormatConfigurationEntry(ExtendedTypeDefinition typeDefinition)
            : base("*")
        {
            if (typeDefinition == null)
            {
                throw PSTraceSource.NewArgumentNullException("typeDefinition");
            }
            FormatData = typeDefinition;
        }

        /// <summary>
        /// Gets file name that contains the format configuration information.
        /// </summary>
        /// <value>File name that contains the format configuration information.</value>
        public string FileName { get; }

        /// <summary>
        /// Get the typeDefinition that contains the format configuration information
        /// </summary>
        public ExtendedTypeDefinition FormatData { get; }
    }

    /// <summary>
    /// Class to define configuration data for cmdlets
    /// </summary>
#if CORECLR
    internal
#else
    public
#endif
    sealed class CmdletConfigurationEntry : RunspaceConfigurationEntry
    {
        /// <summary>
        /// Initiate an instance for cmdlet configuration entry.
        /// </summary>
        /// <param name="name">Name of the cmdlet configuration entry</param>
        /// <param name="implementingType">Class that include implementation of the cmdlet</param>
        /// <param name="helpFileName">Name of the help file that include help information for the cmdlet</param>
        public CmdletConfigurationEntry(string name, Type implementingType, string helpFileName)
            : base(name)
        {
            if (implementingType == null)
            {
                throw PSTraceSource.NewArgumentNullException("implementingType");
            }

            ImplementingType = implementingType;

            if (!String.IsNullOrEmpty(helpFileName))
            {
                HelpFileName = helpFileName.Trim();
            }
            else
            {
                HelpFileName = helpFileName;
            }
        }

        /// <summary>
        /// Initiate an instance for cmdlet configuration entry.
        /// </summary>
        /// <param name="name">Name of the cmdlet configuration entry</param>
        /// <param name="implementingType">Class that include implementation of the cmdlet</param>
        /// <param name="psSnapinInfo">PSSnapin from which the cmdlet comes.</param>
        /// <param name="helpFileName">Name of the help file that include help information for the cmdlet</param>
        internal CmdletConfigurationEntry(string name, Type implementingType, string helpFileName, PSSnapInInfo psSnapinInfo)
            : base(name, psSnapinInfo)
        {
            if (implementingType == null)
            {
                throw PSTraceSource.NewArgumentNullException("implementingType");
            }

            ImplementingType = implementingType;

            if (!String.IsNullOrEmpty(helpFileName))
            {
                HelpFileName = helpFileName.Trim();
            }
            else
            {
                HelpFileName = helpFileName;
            }
        }

        /// <summary>
        /// Get class that include implementation of the cmdlet
        /// </summary>
        public Type ImplementingType { get; }

        /// <summary>
        /// Get name of the help file that include help information for the cmdlet
        /// </summary>
        /// <value></value>
        public string HelpFileName { get; }
    }

    /// <summary>
    /// Define class for provider configuration entry
    /// </summary>
#if CORECLR
    internal
#else
    public
#endif
    sealed class ProviderConfigurationEntry : RunspaceConfigurationEntry
    {
        /// <summary>
        /// Initiate an instance for provider configuration entry.
        /// </summary>
        /// <param name="name">Name of the provider configuration entry</param>
        /// <param name="implementingType">Class that include implementation of the provider</param>
        /// <param name="helpFileName">Name of the help file that include help information for the provider</param>
        public ProviderConfigurationEntry(string name, Type implementingType, string helpFileName)
            : base(name)
        {
            if (implementingType == null)
            {
                throw PSTraceSource.NewArgumentNullException("implementingType");
            }

            ImplementingType = implementingType;

            if (!String.IsNullOrEmpty(helpFileName))
            {
                HelpFileName = helpFileName.Trim();
            }
            else
            {
                HelpFileName = helpFileName;
            }
        }

        /// <summary>
        /// Initiate an instance for provider configuration entry.
        /// </summary>
        /// <param name="name">Name of the provider configuration entry</param>
        /// <param name="implementingType">Class that include implementation of the provider</param>
        /// <param name="helpFileName">Name of the help file that include help information for the provider</param>
        /// <param name="psSnapinInfo">PSSnapin from which provider comes from.</param>
        internal ProviderConfigurationEntry(string name, Type implementingType, string helpFileName, PSSnapInInfo psSnapinInfo)
            : base(name, psSnapinInfo)
        {
            if (implementingType == null)
            {
                throw PSTraceSource.NewArgumentNullException("implementingType");
            }

            ImplementingType = implementingType;

            if (!String.IsNullOrEmpty(helpFileName))
            {
                HelpFileName = helpFileName.Trim();
            }
            else
            {
                HelpFileName = helpFileName;
            }
        }

        /// <summary>
        /// Get class that include implementation of the provider.
        /// </summary>
        /// <value></value>
        public Type ImplementingType { get; }

        /// <summary>
        /// Get name of the help file that include help information for the provider
        /// </summary>
        public string HelpFileName { get; }
    }

    /// <summary>
    /// Define class for script configuration entry
    /// </summary>
#if CORECLR
    internal
#else
    public
#endif
    sealed class ScriptConfigurationEntry : RunspaceConfigurationEntry
    {
        /// <summary>
        /// Initiate an instance for script configuration entry.
        /// </summary>
        /// <param name="name">Name of the script configuration entry</param>
        /// <param name="definition">Content of the script</param>
        public ScriptConfigurationEntry(string name, string definition)
            : base(name)
        {
            if (String.IsNullOrEmpty(definition) || String.IsNullOrEmpty(definition.Trim()))
            {
                throw PSTraceSource.NewArgumentNullException("definition");
            }

            Definition = definition.Trim();
        }

        /// <summary>
        /// Get content for the script.
        /// </summary>
        public string Definition { get; }
    }

    /// <summary>
    /// Configuration data for assemblies.
    /// </summary>
#if CORECLR
    internal
#else
    public
#endif
    sealed class AssemblyConfigurationEntry : RunspaceConfigurationEntry
    {
        /// <summary>
        /// Initiate an instance for assembly configuration entry.
        /// </summary>
        /// <param name="name">Strong name of the assembly</param>
        /// <param name="fileName">Name of the assembly file</param>
        public AssemblyConfigurationEntry(string name, string fileName)
            : base(name)
        {
            if (String.IsNullOrEmpty(fileName) || String.IsNullOrEmpty(fileName.Trim()))
            {
                throw PSTraceSource.NewArgumentNullException("fileName");
            }

            FileName = fileName.Trim();
        }

        /// <summary>
        /// Initiate an instance for assembly configuration entry.
        /// </summary>
        /// <param name="name">Strong name of the assembly</param>
        /// <param name="fileName">Name of the assembly file</param>
        /// <param name="psSnapinInfo">PSSnapin information.</param>
        /// <exception cref="ArgumentException">when <paramref name="fileName"/> is null, empty or does not end in .ps1xml</exception>
        internal AssemblyConfigurationEntry(string name, string fileName, PSSnapInInfo psSnapinInfo)
            : base(name, psSnapinInfo)
        {
            if (String.IsNullOrEmpty(fileName) || String.IsNullOrEmpty(fileName.Trim()))
            {
                throw PSTraceSource.NewArgumentNullException("fileName");
            }

            FileName = fileName.Trim();
        }

        /// <summary>
        /// Get name of the assembly file
        /// </summary>
        /// <value>Name of the assembly file</value>
        public string FileName { get; }
    }

    internal enum UpdateAction
    {
        Add,
        Remove,
        None
    }
}


#pragma warning restore 56506