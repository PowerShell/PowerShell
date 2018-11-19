// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation.Language;

namespace System.Management.Automation
{
    internal abstract class HelpSystemBase
    {
        private static Type s_HelpSystemType;
        internal static Type InstanceType => s_HelpSystemType;

        internal static void RegisterHelpSystem(Type helpSystemType)
        {
            if (!(typeof(HelpSystemBase).IsAssignableFrom(helpSystemType)) || helpSystemType.IsPublic)
            {
                throw PSTraceSource.NewArgumentException("helpSystemType");
            }

            s_HelpSystemType = helpSystemType;
        }

        internal abstract void ResetHelpProviders();
        internal delegate void HelpProgressHandler(object sender, HelpProgressInfo arg);
        internal abstract event HelpProgressHandler OnProgress;
        private protected ArrayList _helpProviders = new ArrayList();

        /// <summary>
        /// return the list of help providers initialized
        /// </summary>
        /// <value>a list of help providers</value>
        internal ArrayList HelpProviders
        {
            get
            {
                return _helpProviders;
            }
        }

        private protected bool _verboseHelpErrors = false;

        /// <summary>
        /// VerboseHelpErrors is used in the case when end user is interested
        /// to know all errors happened during a help search. This property
        /// is false by default.
        ///
        /// If this property is turned on (by setting session variable "VerboseHelpError"),
        /// following two behaviours will be different,
        ///     a. Help errors will be written to error pipeline regardless the situation.
        ///        (Normally, help errors will be written to error pipeline if there is no
        ///         help found and there is no wildcard in help search target).
        ///     b. Some additional warnings, including maml processing warnings, will be
        ///        written to error pipeline.
        /// </summary>
        /// <value></value>
        internal bool VerboseHelpErrors
        {
            get
            {
                return _verboseHelpErrors;
            }
        }

        private protected  Collection<ErrorRecord> _lastErrors = new Collection<ErrorRecord>();

        /// <summary>
        /// This is for tracking the last set of errors happened during the help
        /// search.
        /// </summary>
        /// <value></value>
        internal Collection<ErrorRecord> LastErrors
        {
            get
            {
                return _lastErrors;
            }
        }

        private protected readonly Lazy<Dictionary<Ast, Token[]>> _scriptBlockTokenCache = new Lazy<Dictionary<Ast, Token[]>>(isThreadSafe: true);

        internal void ClearScriptBlockTokenCache()
        {
            if (_scriptBlockTokenCache.IsValueCreated)
            {
                _scriptBlockTokenCache.Value.Clear();
            }
        }

        internal abstract IEnumerable<HelpInfo> ExactMatchHelp(HelpRequest helpRequest);

        internal abstract IEnumerable<HelpInfo> GetHelp(HelpRequest helpRequest);

    }

    internal class HelpSystemDummy : HelpSystemBase
    {
        static HelpSystemDummy()
        {
            RegisterHelpSystem(typeof(HelpSystemDummy));
        }

        internal HelpSystemDummy(ExecutionContext context)
        {
            OnProgress = HelpSystem_OnProgress;
        }

        internal override void ResetHelpProviders()
        {
        }

        internal override event HelpProgressHandler OnProgress;

        private void HelpSystem_OnProgress(object sender, HelpProgressInfo arg)
        {
        }

        internal override IEnumerable<HelpInfo> ExactMatchHelp(HelpRequest helpRequest)
        {
            return Array.Empty<HelpInfo>();
        }

        internal override IEnumerable<HelpInfo> GetHelp(HelpRequest helpRequest)
        {
            return Array.Empty<HelpInfo>();
        }

    }

        /// <summary>
    /// Help progress info
    /// </summary>
    internal class HelpProgressInfo
    {
        internal bool Completed;
        internal string Activity;
        internal int PercentComplete;
    }

    /// <summary>
    /// Help categories
    /// </summary>
    [Flags]
    internal enum HelpCategory
    {
        /// <summary>
        /// Undefined help category
        /// </summary>
        None = 0x00,

        /// <summary>
        /// Alias help
        /// </summary>
        Alias = 0x01,

        /// <summary>
        /// Cmdlet help
        /// </summary>
        Cmdlet = 0x02,

        /// <summary>
        /// Provider help
        /// </summary>
        Provider = 0x04,

        /// <summary>
        /// General keyword help
        /// </summary>
        General = 0x10,

        /// <summary>
        /// FAQ's
        /// </summary>
        FAQ = 0x20,

        /// <summary>
        /// Glossary and term definitions
        /// </summary>
        Glossary = 0x40,

        /// <summary>
        /// Help that is contained in help file
        /// </summary>
        HelpFile = 0x80,

        /// <summary>
        /// Help from a script block
        /// </summary>
        ScriptCommand = 0x100,

        /// <summary>
        /// Help for a function
        /// </summary>
        Function = 0x200,

        /// <summary>
        /// Help for a filter
        /// </summary>
        Filter = 0x400,

        /// <summary>
        /// Help for an external script (i.e. for a *.ps1 file)
        /// </summary>
        ExternalScript = 0x800,

        /// <summary>
        /// All help categories.
        /// </summary>
        All = 0xFFFFF,

        ///<summary>
        /// Default Help
        /// </summary>
        DefaultHelp = 0x1000,

        ///<summary>
        /// Help for a Workflow
        /// </summary>
        Workflow = 0x2000,

        ///<summary>
        /// Help for a Configuration
        /// </summary>
        Configuration = 0x4000,

        /// <summary>
        /// Help for DSC Resource
        /// </summary>
        DscResource = 0x8000,

        /// <summary>
        /// Help for PS Classes
        /// </summary>
        Class = 0x10000
    }

    internal abstract class ProviderContextBase
    {
    }

    internal class HelpCommentsParser
    {
        internal static Tuple<List<Language.Token>, List<string>> GetHelpCommentTokens(IParameterMetadataProvider ipmp,
            Dictionary<Ast, Token[]> scriptBlockTokenCache)
        {
            return null;
        }

        internal static CommentHelpInfo GetHelpContents(List<Language.Token> comments, List<string> parameterDescriptions)
        {
            return null;
        }

        internal static HelpInfo CreateFromComments(ExecutionContext context,
                                                    CommandInfo commandInfo,
                                                    List<Language.Token> comments,
                                                    List<string> parameterDescriptions,
                                                    bool dontSearchOnRemoteComputer,
                                                    out string helpFile, out string helpUriFromDotLink)
        {
            helpFile = null;
            helpUriFromDotLink = null;
            return null;
        }

        internal static readonly string mshURI = "http://msh";
        internal static readonly string commandURI = "http://schemas.microsoft.com/maml/dev/command/2004/10";
        internal static readonly string ProviderHelpCommandXPath =
            "/msh:helpItems/msh:providerHelp/msh:CmdletHelpPaths/msh:CmdletHelpPath{0}/command:command[command:details/command:verb='{1}' and command:details/command:noun='{2}']";
   }

}
