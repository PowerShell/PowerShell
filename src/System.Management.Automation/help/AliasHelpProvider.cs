// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Management.Automation.Internal;

namespace System.Management.Automation
{
    /// <summary>
    /// Implements the help provider for alias help.
    /// </summary>
    /// <remarks>
    /// Unlike other help providers, AliasHelpProvider directly inherits from HelpProvider
    /// instead of HelpProviderWithCache. This is because alias can be created/removed/updated
    /// in a Microsoft Command Shell session. And thus caching may result in old alias being cached.
    ///
    /// The real information for alias is stored in command help. To retrieve the real
    /// help information, help forwarding is needed.
    /// </remarks>
    internal sealed class AliasHelpProvider : HelpProvider
    {
        /// <summary>
        /// Initializes a new instance of AliasHelpProvider class.
        /// </summary>
        internal AliasHelpProvider(HelpSystem helpSystem) : base(helpSystem)
        {
            _sessionState = helpSystem.ExecutionContext.SessionState;
            _commandDiscovery = helpSystem.ExecutionContext.CommandDiscovery;
            _context = helpSystem.ExecutionContext;
        }

        private readonly ExecutionContext _context;

        /// <summary>
        /// Session state for current Microsoft Command Shell session.
        /// </summary>
        /// <remarks>
        /// _sessionState is mainly used for alias help search in the case
        /// of wildcard search patterns. This is currently not achievable
        /// through _commandDiscovery.
        /// </remarks>
        private readonly SessionState _sessionState;

        /// <summary>
        /// Command Discovery object for current session.
        /// </summary>
        /// <remarks>
        /// _commandDiscovery is mainly used for exact match help for alias.
        /// The AliasInfo object returned from _commandDiscovery is essential
        /// in creating AliasHelpInfo.
        /// </remarks>
        private readonly CommandDiscovery _commandDiscovery;

        #region Common Properties

        /// <summary>
        /// Name of alias help provider.
        /// </summary>
        /// <value>Name of alias help provider</value>
        internal override string Name
        {
            get
            {
                return "Alias Help Provider";
            }
        }

        /// <summary>
        /// Help category of alias help provider, which is a constant: HelpCategory.Alias.
        /// </summary>
        /// <value>Help category of alias help provider.</value>
        internal override HelpCategory HelpCategory
        {
            get
            {
                return HelpCategory.Alias;
            }
        }

        #endregion

        #region Help Provider Interface

        /// <summary>
        /// Exact match an alias help target.
        /// </summary>
        /// <remarks>
        /// This will
        ///     a. use _commandDiscovery object to retrieve AliasInfo object.
        ///     b. Create AliasHelpInfo object based on AliasInfo object
        /// </remarks>
        /// <param name="helpRequest">Help request object.</param>
        /// <returns>Help info found.</returns>
        internal override IEnumerable<HelpInfo> ExactMatchHelp(HelpRequest helpRequest)
        {
            CommandInfo commandInfo = null;

            try
            {
                commandInfo = _commandDiscovery.LookupCommandInfo(helpRequest.Target);
            }
            catch (CommandNotFoundException)
            {
                // CommandNotFoundException is expected here if target doesn't match any
                // commandlet. Just ignore this exception and bail out.
            }

            if ((commandInfo != null) && (commandInfo.CommandType == CommandTypes.Alias))
            {
                AliasInfo aliasInfo = (AliasInfo)commandInfo;

                HelpInfo helpInfo = AliasHelpInfo.GetHelpInfo(aliasInfo);
                if (helpInfo != null)
                {
                    yield return helpInfo;
                }
            }
        }

        /// <summary>
        /// Search an alias help target.
        /// </summary>
        /// <remarks>
        /// This will,
        ///     a. use _sessionState object to get a list of alias that match the target.
        ///     b. for each alias, retrieve help info as in ExactMatchHelp.
        /// </remarks>
        /// <param name="helpRequest">Help request object.</param>
        /// <param name="searchOnlyContent">
        /// If true, searches for pattern in the help content. Individual
        /// provider can decide which content to search in.
        ///
        /// If false, searches for pattern in the command names.
        /// </param>
        /// <returns>A IEnumerable of helpinfo object.</returns>
        internal override IEnumerable<HelpInfo> SearchHelp(HelpRequest helpRequest, bool searchOnlyContent)
        {
            // aliases do not have help content...so doing nothing in that case
            if (!searchOnlyContent)
            {
                string target = helpRequest.Target;
                string pattern = target;
                var allAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (!WildcardPattern.ContainsWildcardCharacters(target))
                {
                    pattern += "*";
                }

                WildcardPattern matcher = WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase);
                IDictionary<string, AliasInfo> aliasTable = _sessionState.Internal.GetAliasTable();

                foreach (string name in aliasTable.Keys)
                {
                    if (matcher.IsMatch(name))
                    {
                        HelpRequest exactMatchHelpRequest = helpRequest.Clone();
                        exactMatchHelpRequest.Target = name;
                        // Duplicates??
                        foreach (HelpInfo helpInfo in ExactMatchHelp(exactMatchHelpRequest))
                        {
                            // Component/Role/Functionality match is done only for SearchHelp
                            // as "get-help * -category alias" should not forward help to
                            // CommandHelpProvider..(ExactMatchHelp does forward help to
                            // CommandHelpProvider)
                            if (!Match(helpInfo, helpRequest))
                            {
                                continue;
                            }

                            if (allAliases.Contains(name))
                            {
                                continue;
                            }

                            allAliases.Add(name);

                            yield return helpInfo;
                        }
                    }
                }

                CommandSearcher searcher =
                        new CommandSearcher(
                            pattern,
                            SearchResolutionOptions.ResolveAliasPatterns, CommandTypes.Alias,
                            _context);

                while (searcher.MoveNext())
                {
                    CommandInfo current = ((IEnumerator<CommandInfo>)searcher).Current;

                    if (_context.CurrentPipelineStopping)
                    {
                        yield break;
                    }

                    AliasInfo alias = current as AliasInfo;

                    if (alias != null)
                    {
                        string name = alias.Name;
                        HelpRequest exactMatchHelpRequest = helpRequest.Clone();
                        exactMatchHelpRequest.Target = name;

                        // Duplicates??
                        foreach (HelpInfo helpInfo in ExactMatchHelp(exactMatchHelpRequest))
                        {
                            // Component/Role/Functionality match is done only for SearchHelp
                            // as "get-help * -category alias" should not forward help to
                            // CommandHelpProvider..(ExactMatchHelp does forward help to
                            // CommandHelpProvider)
                            if (!Match(helpInfo, helpRequest))
                            {
                                continue;
                            }

                            if (allAliases.Contains(name))
                            {
                                continue;
                            }

                            allAliases.Add(name);

                            yield return helpInfo;
                        }
                    }
                }

                foreach (CommandInfo current in ModuleUtils.GetMatchingCommands(pattern, _context, helpRequest.CommandOrigin))
                {
                    if (_context.CurrentPipelineStopping)
                    {
                        yield break;
                    }

                    AliasInfo alias = current as AliasInfo;

                    if (alias != null)
                    {
                        string name = alias.Name;

                        HelpInfo helpInfo = AliasHelpInfo.GetHelpInfo(alias);

                        if (allAliases.Contains(name))
                        {
                            continue;
                        }

                        allAliases.Add(name);

                        yield return helpInfo;
                    }
                }
            }
        }

        private static bool Match(HelpInfo helpInfo, HelpRequest helpRequest)
        {
            if (helpRequest == null)
                return true;

            if ((helpRequest.HelpCategory & helpInfo.HelpCategory) == 0)
            {
                return false;
            }

            if (!Match(helpInfo.Component, helpRequest.Component))
            {
                return false;
            }

            if (!Match(helpInfo.Role, helpRequest.Role))
            {
                return false;
            }

            if (!Match(helpInfo.Functionality, helpRequest.Functionality))
            {
                return false;
            }

            return true;
        }

        private static bool Match(string target, string[] patterns)
        {
            // patterns should never be null as shell never accepts
            // empty inputs. Keeping this check as a safe measure.
            if (patterns == null || patterns.Length == 0)
                return true;

            foreach (string pattern in patterns)
            {
                if (Match(target, pattern))
                {
                    // we have a match so return true
                    return true;
                }
            }

            // We dont have a match so far..so return false
            return false;
        }

        private static bool Match(string target, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return true;

            if (string.IsNullOrEmpty(target))
                target = string.Empty;

            WildcardPattern matcher = WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase);

            return matcher.IsMatch(target);
        }

        #endregion
    }
}
