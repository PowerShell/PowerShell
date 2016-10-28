namespace System.Management.Automation
{
    /// <summary>
    /// EventArgs for the DynamicScriptBlockCreationEvent event
    /// </summary>
    public class DynamicScriptBlockCreationEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor for event args object
        /// </summary>
        /// <param name="script">The string to compile</param>
        internal DynamicScriptBlockCreationEventArgs(string script)
        {
            Script = script;
        }

        /// <summary>
        /// The string to compile
        /// </summary>
        public string Script { get; }
    }
}
