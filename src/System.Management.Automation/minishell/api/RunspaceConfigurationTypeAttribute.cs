/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

namespace System.Management.Automation.Runspaces
{
    /// <summary>
    /// Define the class for runspace configuration type attribute. 
    /// </summary>
    /// <!--
    /// This is an assembly attribute for the mini-shell assembly to tell 
    /// the type name for MiniShellConguration derived class.
    /// -->
    [AttributeUsage(AttributeTargets.Assembly)]
#if CORECLR
    internal
#else
    public
#endif
    sealed class RunspaceConfigurationTypeAttribute : Attribute
    {
        /// <summary>
        /// Initiate an instance of RunspaceConfigurationTypeAttribute.
        /// </summary>
        /// <param name="runspaceConfigurationType">Runspace configuration type</param>
        public RunspaceConfigurationTypeAttribute(string runspaceConfigurationType)
        {
            RunspaceConfigurationType = runspaceConfigurationType;
        }

        /// <summary>
        /// Get runspace configuration type
        /// </summary>
        public string RunspaceConfigurationType { get; }
    }
}
