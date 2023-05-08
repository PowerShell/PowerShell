// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable 1634, 1691
#pragma warning disable 56506

namespace System.Management.Automation
{
    /// <summary>
    /// Defines the attribute used to designate a cmdlet parameter as one that
    /// should accept credentials.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class CredentialAttribute : ArgumentTransformationAttribute
    {
        /// <summary>
        /// Transforms the input data to an PSCredential.
        /// </summary>
        /// <param name="engineIntrinsics">
        /// The engine APIs for the context under which the transformation is being
        /// made.
        /// </param>
        /// <param name="inputData">
        /// If Null, the transformation prompts for both Username and Password
        /// If a string, the transformation uses the input for a username, and prompts
        ///    for a Password
        /// If already an PSCredential, the transform does nothing.
        /// </param>
        /// <returns>An PSCredential object representing the inputData.</returns>
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            PSCredential cred = null;
            string userName = null;
            bool shouldPrompt = false;

            if ((engineIntrinsics == null) ||
               (engineIntrinsics.Host == null) ||
               (engineIntrinsics.Host.UI == null))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(engineIntrinsics));
            }

            if (inputData == null)
            {
                shouldPrompt = true;
            }
            else
            {
                // Try to coerce the input as an PSCredential
                cred = LanguagePrimitives.FromObjectAs<PSCredential>(inputData);

                // Try to coerce the username from the string
                if (cred == null)
                {
                    shouldPrompt = true;
                    userName = LanguagePrimitives.FromObjectAs<string>(inputData);

                    // If we couldn't get the username (as a string,)
                    // throw an exception
                    if (userName == null)
                    {
                        throw new PSArgumentException("userName");
                    }
                }
            }

            if (shouldPrompt)
            {
                string caption = null;
                string prompt = null;

                caption = CredentialAttributeStrings.CredentialAttribute_Prompt_Caption;

                prompt = CredentialAttributeStrings.CredentialAttribute_Prompt;

                cred = engineIntrinsics.Host.UI.PromptForCredential(
                           caption,
                           prompt,
                           userName,
                           string.Empty);
            }

            return cred;
        }

        /// <summary/>
        public override bool TransformNullOptionalParameters { get { return false; } }
    }
}

#pragma warning restore 56506

