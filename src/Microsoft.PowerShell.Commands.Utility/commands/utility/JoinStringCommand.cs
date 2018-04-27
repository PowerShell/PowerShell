using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands.Utility.commands.utility
{
    /// <summary>
    /// Implements the Join-String command
    /// </summary>
    [Cmdlet(VerbsCommon.Join, "String")]
    [OutputType(typeof(string))]
    [Alias("join")]
    public class JoinStringCommand : Cmdlet
    {
        #region Parameters
        /// <summary>
        /// What to join with
        /// </summary>
        [Parameter(Position = 0)]
        public string Separator = Environment.NewLine;
        
        /// <summary>
        /// Number of items to join in a batch
        /// </summary>
        [Parameter(Position = 1)]
        public int Count;
        
        /// <summary>
        /// The strings to join
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public string[] InputString;
        #endregion Parameters

        #region Fields
        /// <summary>
        /// List used to cache items for joining
        /// </summary>
        private List<string> ItemCache = new List<string>();
        #endregion Fields

        #region Methods
        /// <summary>
        /// Processes each input item
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (string item in InputString)
            {
                ItemCache.Add(item);
                if ((Count > 0) && (ItemCache.Count >= Count))
                {
                    WriteObject(String.Join(Separator, ItemCache));
                    ItemCache.Clear();
                }
            }
        }

        /// <summary>
        /// Finalizes execution, joining and emitting remaining objects
        /// </summary>
        protected override void EndProcessing()
        {
            if (ItemCache.Count > 0)
            {
                WriteObject(String.Join(Separator, ItemCache));
                ItemCache.Clear();
            }
        }
        #endregion Methods
    }
}
