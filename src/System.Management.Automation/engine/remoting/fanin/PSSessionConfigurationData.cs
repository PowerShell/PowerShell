// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

using Microsoft.PowerShell.Commands;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// </summary>
    public sealed class PSSessionConfigurationData
    {
        /// <summary>
        /// </summary>
#pragma warning disable CA2211 // Non-constant fields should not be visible
        public static bool IsServerManager;
#pragma warning restore CA2211 // Non-constant fields should not be visible

        #region Public Properties

        /// <summary>
        /// </summary>
        public List<string> ModulesToImport
        {
            get
            {
                return _modulesToImport;
            }
        }

        internal List<object> ModulesToImportInternal
        {
            get
            {
                return _modulesToImportInternal;
            }
        }

        /// <summary>
        /// </summary>
        public string PrivateData
        {
            get
            {
                return _privateData;
            }

            internal set
            {
                _privateData = value;
            }
        }

        #endregion Public Properties

        #region Internal Methods

        private PSSessionConfigurationData()
        {
        }

        internal static string Unescape(string s)
        {
            StringBuilder sb = new StringBuilder(s);
            sb.Replace("&lt;", "<");
            sb.Replace("&gt;", ">");
            sb.Replace("&quot;", "\"");
            sb.Replace("&apos;", "'");
            sb.Replace("&amp;", "&");
            return sb.ToString();
        }

        internal static PSSessionConfigurationData Create(string configurationData)
        {
            PSSessionConfigurationData configuration = new PSSessionConfigurationData();

            if (string.IsNullOrEmpty(configurationData))
            {
                return configuration;
            }

            configurationData = Unescape(configurationData);

            XmlReaderSettings readerSettings = new XmlReaderSettings
            {
                CheckCharacters = false,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                MaxCharactersInDocument = 10000,
                XmlResolver = null,
                ConformanceLevel = ConformanceLevel.Fragment
            };

            using (XmlReader reader = XmlReader.Create(new StringReader(configurationData), readerSettings))
            {
                // read the header <SessionConfigurationData>
                if (reader.ReadToFollowing(SessionConfigToken))
                {
                    bool isParamFound = reader.ReadToDescendant(ParamToken);
                    while (isParamFound)
                    {
                        if (!reader.MoveToAttribute(NameToken))
                        {
                            throw PSTraceSource.NewArgumentException(configurationData,
                                RemotingErrorIdStrings.NoAttributesFoundForParamElement,
                                NameToken, ValueToken, ParamToken);
                        }

                        string optionName = reader.Value;

                        if (string.Equals(optionName, PrivateDataToken, StringComparison.OrdinalIgnoreCase))
                        {
                            // this is a PrivateData element which we
                            // need to process
                            if (reader.ReadToFollowing(PrivateDataToken))
                            {
                                string privateData = reader.ReadOuterXml();

                                AssertValueNotAssigned(PrivateDataToken, configuration._privateData);
                                configuration._privateData = privateData;
                            }
                        }
                        else
                        {
                            if (!reader.MoveToAttribute(ValueToken))
                            {
                                throw PSTraceSource.NewArgumentException(configurationData,
                                                                         RemotingErrorIdStrings.NoAttributesFoundForParamElement,
                                                                         NameToken, ValueToken, ParamToken);
                            }

                            string optionValue = reader.Value;
                            configuration.Update(optionName, optionValue);
                        }

                        // move to next Param token.
                        isParamFound = reader.ReadToFollowing(ParamToken);
                    }
                }
            }

            configuration.CreateCollectionIfNecessary();

            return configuration;
        }

        #endregion Internal Methods

        #region Private Members

        private List<string> _modulesToImport;
        private List<object> _modulesToImportInternal;

        private string _privateData;

        /// <summary>
        /// Checks if the originalValue is empty. If not throws an exception.
        /// </summary>
        /// <param name="optionName"></param>
        /// <param name="originalValue"></param>
        /// <exception cref="ArgumentException">
        /// 1. "optionName" is already defined
        /// </exception>
        private static void AssertValueNotAssigned(string optionName, object originalValue)
        {
            if (originalValue != null)
            {
                throw PSTraceSource.NewArgumentException(optionName,
                    RemotingErrorIdStrings.DuplicateInitializationParameterFound, optionName, SessionConfigToken);
            }
        }

        /// <summary>
        /// Using optionName and optionValue updates the current object.
        /// </summary>
        /// <param name="optionName"></param>
        /// <param name="optionValue"></param>
        /// <exception cref="ArgumentException">
        /// 1. "optionName" is not valid in "InitializationParameters" section.
        /// 2. "startupscript" must specify a PowerShell script file that ends with extension ".ps1".
        /// </exception>
        private void Update(string optionName, string optionValue)
        {
            switch (optionName.ToLowerInvariant())
            {
                case ModulesToImportToken:
                    {
                        AssertValueNotAssigned(ModulesToImportToken, _modulesToImport);
                        _modulesToImport = new List<string>();
                        _modulesToImportInternal = new List<object>();
                        object[] modulesToImport = optionValue.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var module in modulesToImport)
                        {
                            var s = module as string;
                            if (s != null)
                            {
                                _modulesToImport.Add(s.Trim());

                                ModuleSpecification moduleSpec = null;
                                if (ModuleSpecification.TryParse(s, out moduleSpec))
                                {
                                    _modulesToImportInternal.Add(moduleSpec);
                                }
                                else
                                {
                                    _modulesToImportInternal.Add(s.Trim());
                                }
                            }
                        }
                    }

                    break;
                default:
                    {
                        Dbg.Assert(false, "Unknown option specified");
                    }

                    break;
            }
        }

        private void CreateCollectionIfNecessary()
        {
            _modulesToImport ??= new List<string>();
            _modulesToImportInternal ??= new List<object>();
        }

        private const string SessionConfigToken = "SessionConfigurationData";
        internal const string ModulesToImportToken = "modulestoimport";
        internal const string PrivateDataToken = "PrivateData";
        internal const string InProcActivityToken = "InProcActivity";
        private const string ParamToken = "Param";
        private const string NameToken = "Name";
        private const string ValueToken = "Value";

        #endregion Private Members
    }
}
