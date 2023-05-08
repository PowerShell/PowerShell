// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    /// <summary>
    /// Help request is a set of data to be passed into monad help engine for requesting help contents.
    ///
    /// Help request track information including
    ///
    ///     1. target
    ///     2. category filter
    ///     3. provider
    ///     4. dynamic parameters
    ///     5. components
    ///     6. functionalities
    ///     7. roles
    ///
    /// Upon getting a help request, help engine will validate the help request and send the request to
    /// necessary help providers for processing.
    /// </summary>
    internal class HelpRequest
    {
        /// <summary>
        /// Constructor for HelpRequest.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="helpCategory"></param>
        internal HelpRequest(string target, HelpCategory helpCategory)
        {
            Target = target;
            HelpCategory = helpCategory;
            CommandOrigin = CommandOrigin.Runspace;
        }

        /// <summary>
        /// Create a copy of current help request object.
        /// </summary>
        /// <returns></returns>
        internal HelpRequest Clone()
        {
            HelpRequest helpRequest = new HelpRequest(this.Target, this.HelpCategory);

            helpRequest.Provider = this.Provider;
            helpRequest.MaxResults = this.MaxResults;
            helpRequest.Component = this.Component;
            helpRequest.Role = this.Role;
            helpRequest.Functionality = this.Functionality;
            helpRequest.ProviderContext = this.ProviderContext;
            helpRequest.CommandOrigin = CommandOrigin;

            return helpRequest;
        }

        /// <summary>
        /// Defines which provider the user seeking help is curious about.
        /// </summary>
        internal ProviderContext ProviderContext { get; set; }

        /// <summary>
        /// Target for help.
        /// </summary>
        /// <value></value>
        internal string Target { get; set; }

        /// <summary>
        /// Help category filter.
        /// </summary>
        /// <value></value>
        internal HelpCategory HelpCategory { get; set; } = HelpCategory.None;

        /// <summary>
        /// Provider for this help.
        ///
        /// If provider is set and helpCategory is 'Provider', provider help will be returned. (Also
        /// the value of target will be set to this one).
        ///
        /// If provider is set and helpCategory is 'Command', this will add provider specific help
        /// to provider.
        /// </summary>
        /// <value></value>
        internal string Provider { get; set; }

        /// <summary>
        /// Maximum number of result to return for this request.
        /// </summary>
        /// <value></value>
        internal int MaxResults { get; set; } = -1;

        /// <summary>
        /// Component filter for command help.
        /// </summary>
        /// <value></value>
        internal string[] Component { get; set; }

        /// <summary>
        /// Role filter for command help.
        /// </summary>
        /// <value></value>
        internal string[] Role { get; set; }

        /// <summary>
        /// Functionality filter for command help.
        /// </summary>
        /// <value></value>
        internal string[] Functionality { get; set; }

        /// <summary>
        /// Keeps track of get-help cmdlet call origin. It can be called
        /// directly by the user or indirectly by a script that a user calls.
        /// </summary>
        internal CommandOrigin CommandOrigin { get; set; }

        /// <summary>
        /// Following validation will be done, (in order)
        ///
        /// 1. If everything is empty, do default help.
        /// 2. If target is empty, set it to be provider if currently doing provider help only. Otherwise, set it to be *
        /// 3. If any special parameters like component, role, functionality are specified, do command help only.
        /// 4. If command help is requested, search for alias also.
        /// 5. If help category is none, set it to be all.
        /// 6. Don't do default help.
        /// </summary>
        internal void Validate()
        {
            if (string.IsNullOrEmpty(Target)
                && HelpCategory == HelpCategory.None
                && string.IsNullOrEmpty(Provider)
                && Component == null
                && Role == null
                && Functionality == null
            )
            {
                Target = "default";
                HelpCategory = HelpCategory.DefaultHelp;
                return;
            }

            if (string.IsNullOrEmpty(Target))
            {
                if (!string.IsNullOrEmpty(Provider) &&
                    (HelpCategory == HelpCategory.None || HelpCategory == HelpCategory.Provider)
                )
                {
                    Target = Provider;
                }
                else
                {
                    Target = "*";
                }
            }

            // if either of component/role/functionality is specified then look in the
            // following help categories
            if ((!(Component == null && Role == null && Functionality == null)) &&
                (HelpCategory == HelpCategory.None))
            {
                HelpCategory = HelpCategory.Alias | HelpCategory.Cmdlet | HelpCategory.Function | HelpCategory.Filter | HelpCategory.ExternalScript | HelpCategory.ScriptCommand;

                return;
            }

            if ((HelpCategory & HelpCategory.Cmdlet) > 0)
            {
                HelpCategory |= HelpCategory.Alias;
            }

            if (HelpCategory == HelpCategory.None)
            {
                HelpCategory = HelpCategory.All;
            }

            HelpCategory &= ~HelpCategory.DefaultHelp;

            return;
        }
    }
}
