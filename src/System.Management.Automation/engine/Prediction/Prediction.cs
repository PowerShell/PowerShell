// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace System.Management.Automation.Subsystem
{
    /// <summary>
    /// Class used to store a command line prediction result.
    /// </summary>
    public class PredictionResult : CompletionResult
    {
        /// <summary>
        /// Initializes a new instance of the PredictionResult class.
        /// </summary>
        public PredictionResult(string predictionText, string listItemText, string toolTip)
            : base(predictionText, listItemText, CompletionResultType.Text, toolTip)
        {
        }

        /// <summary>
        /// Initializes a new instance of the PredictionResult class if the result is a string.
        /// </summary>
        public PredictionResult(string predictionText) : base(predictionText)
        {
        }
    }

    /// <summary>
    /// Class used to store a command line prediction result.
    /// </summary>
    public interface IPrediction : ISubsystem
    {
        /// <summary>
        /// Default implementation.
        /// Prediction plugin doesn't need to implement any required functions.
        /// </summary>
        IReadOnlyDictionary<string, string> ISubsystem.FunctionsToDefine => null;

        /// <summary>
        /// Default implementation.
        /// </summary>
        SubsystemKind ISubsystem.Kind => SubsystemKind.Prediction;

        /// <summary>
        /// Prediction plugin contract.
        /// </summary>
        /// <param name="userInput">The user input text.</param>
        /// <param name="powershell">The powershell for the plugin to use to collect context data.</param>
        /// <param name="maximumResults">The maximum number of most relevant results expected to be returned. -1 means unlimited.</param>
        Task<Collection<PredictionResult>> CommandLineSuggestion(string userInput, PowerShell powershell, int maximumResults = -1);
    }
}
