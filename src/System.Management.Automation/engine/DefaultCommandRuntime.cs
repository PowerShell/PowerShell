// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable 1634, 1691

using System.Collections;
using System.Collections.Generic;
using System.Management.Automation.Host;

namespace System.Management.Automation
{
    /// <summary>
    /// Default implementation of ICommandRuntime for running Cmdlets standalone.
    /// </summary>
    internal class DefaultCommandRuntime : ICommandRuntime2
    {
        private readonly List<object> _output;
        /// <summary>
        /// Constructs an instance of the default ICommandRuntime object
        /// that will write objects into the list that was passed.
        /// </summary>
        public DefaultCommandRuntime(List<object> outputList)
        {
            ArgumentNullException.ThrowIfNull(outputList);

            _output = outputList;
        }

        /// <summary>
        /// Return the instance of PSHost - null by default.
        /// </summary>
        public PSHost Host { get; set; }

        #region Write
        /// <summary>
        /// Implementation of WriteDebug - just discards the input.
        /// </summary>
        /// <param name="text">Text to write.</param>
        public void WriteDebug(string text) { }

        /// <summary>
        /// Default implementation of WriteError - if the error record contains
        /// an exception then that exception will be thrown. If not, then an
        /// InvalidOperationException will be constructed and thrown.
        /// </summary>
        /// <param name="errorRecord">Error record instance to process.</param>
        public void WriteError(ErrorRecord errorRecord)
        {
            if (errorRecord.Exception != null)
                throw errorRecord.Exception;
            else
                throw new InvalidOperationException(errorRecord.ToString());
        }

        /// <summary>
        /// Default implementation of WriteObject - adds the object to the list
        /// passed to the objects constructor.
        /// </summary>
        /// <param name="sendToPipeline">Object to write.</param>
        public void WriteObject(object sendToPipeline)
        {
            _output.Add(sendToPipeline);
        }

        /// <summary>
        /// Default implementation of the enumerated WriteObject. Either way, the
        /// objects are added to the list passed to this object in the constuctor.
        /// </summary>
        /// <param name="sendToPipeline">Object to write.</param>
        /// <param name="enumerateCollection">If true, the collection is enumerated, otherwise
        /// it's written as a scalar.
        /// </param>
        public void WriteObject(object sendToPipeline, bool enumerateCollection)
        {
            if (enumerateCollection)
            {
                IEnumerator e = LanguagePrimitives.GetEnumerator(sendToPipeline);
                if (e == null)
                {
                    _output.Add(sendToPipeline);
                }
                else
                {
                    while (e.MoveNext())
                    {
                        _output.Add(e.Current);
                    }
                }
            }
            else
            {
                _output.Add(sendToPipeline);
            }
        }

        /// <summary>
        /// Default implementation - just discards it's arguments.
        /// </summary>
        /// <param name="progressRecord">Progress record to write.</param>
        public void WriteProgress(ProgressRecord progressRecord) { }

        /// <summary>
        /// Default implementation - just discards it's arguments.
        /// </summary>
        /// <param name="sourceId">Source ID to write for.</param>
        /// <param name="progressRecord">Record to write.</param>
        public void WriteProgress(Int64 sourceId, ProgressRecord progressRecord) { }

        /// <summary>
        /// Default implementation - just discards it's arguments.
        /// </summary>
        /// <param name="text">Text to write.</param>
        public void WriteVerbose(string text) { }

        /// <summary>
        /// Default implementation - just discards it's arguments.
        /// </summary>
        /// <param name="text">Text to write.</param>
        public void WriteWarning(string text) { }

        /// <summary>
        /// Default implementation - just discards it's arguments.
        /// </summary>
        /// <param name="text">Text to write.</param>
        public void WriteCommandDetail(string text) { }

        /// <summary>
        /// Default implementation - just discards it's arguments.
        /// </summary>
        /// <param name="informationRecord">Record to write.</param>
        public void WriteInformation(InformationRecord informationRecord) { }

        #endregion Write

        #region Should
        /// <summary>
        /// Default implementation - always returns true.
        /// </summary>
        /// <param name="target">Ignored.</param>
        /// <returns>True.</returns>
        public bool ShouldProcess(string target) { return true; }

        /// <summary>
        /// Default implementation - always returns true.
        /// </summary>
        /// <param name="target">Ignored.</param>
        /// <param name="action">Ignored.</param>
        /// <returns>True.</returns>
        public bool ShouldProcess(string target, string action) { return true; }

        /// <summary>
        /// Default implementation - always returns true.
        /// </summary>
        /// <param name="verboseDescription">Ignored.</param>
        /// <param name="verboseWarning">Ignored.</param>
        /// <param name="caption">Ignored.</param>
        /// <returns>True.</returns>
        public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption) { return true; }

        /// <summary>
        /// Default implementation - always returns true.
        /// </summary>
        /// <param name="verboseDescription">Ignored.</param>
        /// <param name="verboseWarning">Ignored.</param>
        /// <param name="caption">Ignored.</param>
        /// <param name="shouldProcessReason">Ignored.</param>
        /// <returns>True.</returns>
        public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption, out ShouldProcessReason shouldProcessReason) { shouldProcessReason = ShouldProcessReason.None; return true; }

        /// <summary>
        /// Default implementation - always returns true.
        /// </summary>
        /// <param name="query">Ignored.</param>
        /// <param name="caption">Ignored.</param>
        /// <returns>True.</returns>
        public bool ShouldContinue(string query, string caption) { return true; }

        /// <summary>
        /// Default implementation - always returns true.
        /// </summary>
        /// <param name="query">Ignored.</param>
        /// <param name="caption">Ignored.</param>
        /// <param name="yesToAll">Ignored.</param>
        /// <param name="noToAll">Ignored.</param>
        /// <returns>True.</returns>
        public bool ShouldContinue(string query, string caption, ref bool yesToAll, ref bool noToAll) { return true; }

        /// <summary>
        /// Default implementation - always returns true.
        /// </summary>
        /// <param name="query">Ignored.</param>
        /// <param name="caption">Ignored.</param>
        /// <param name="hasSecurityImpact">Ignored.</param>
        /// <param name="yesToAll">Ignored.</param>
        /// <param name="noToAll">Ignored.</param>
        /// <returns>True.</returns>
        public bool ShouldContinue(string query, string caption, bool hasSecurityImpact, ref bool yesToAll, ref bool noToAll) { return true; }

        #endregion Should

        #region Transaction Support
        /// <summary>
        /// Returns true if a transaction is available and active.
        /// </summary>
        public bool TransactionAvailable() { return false; }

        /// <summary>
        /// Gets an object that surfaces the current PowerShell transaction.
        /// When this object is disposed, PowerShell resets the active transaction.
        /// </summary>
        public PSTransactionContext CurrentPSTransaction
        {
            get
            {
                string error = TransactionStrings.CmdletRequiresUseTx;

                // We want to throw in this situation, and want to use a
                // property because it mimics the C# using(TransactionScope ...) syntax
#pragma warning suppress 56503
                throw new InvalidOperationException(error);
            }
        }
        #endregion Transaction Support

        #region Misc
        /// <summary>
        /// Implementation of the dummy default ThrowTerminatingError API - it just
        /// does what the base implementation does anyway - rethrow the exception
        /// if it exists, otherwise throw an invalid operation exception.
        /// </summary>
        /// <param name="errorRecord">The error record to throw.</param>
        [System.Diagnostics.CodeAnalysis.DoesNotReturn]
        public void ThrowTerminatingError(ErrorRecord errorRecord)
        {
            if (errorRecord.Exception != null)
            {
                throw errorRecord.Exception;
            }
            else
            {
                throw new System.InvalidOperationException(errorRecord.ToString());
            }
        }
        #endregion
    }
}
