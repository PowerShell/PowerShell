/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

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
        /// Constructor for HelpRequest
        /// </summary>
        /// <param name="target"></param>
        /// <param name="helpCategory"></param>
        internal HelpRequest(string target, HelpCategory helpCategory)
        {
            _target = target;
            _helpCategory = helpCategory;
            _origin = CommandOrigin.Runspace;
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
        internal ProviderContext ProviderContext
        {
            get { return _providerContext; }
            set { _providerContext = value; }
        }
        private ProviderContext _providerContext;

        private string _target;

        /// <summary>
        /// Target for help.
        /// </summary>
        /// <value></value>
        internal string Target
        {
            get
            {
                return _target;
            }
            set
            {
                _target = value;
            }
        }

        private HelpCategory _helpCategory = HelpCategory.None;

        /// <summary>
        /// Help category filter
        /// </summary>
        /// <value></value>
        internal HelpCategory HelpCategory
        {
            get
            {
                return _helpCategory;
            }
            set
            {
                _helpCategory = value;
            }
        }

        private string _provider;

        /// <summary>
        /// Provider for this help.
        /// 
        /// If provider is set and helpCategory is 'Provider', provider help will be returned. (Also
        /// the value of target will be set to this one).
        /// 
        /// If provider is set and helpCategory is 'Command', this will add provider specific help
        /// to provider. 
        /// 
        /// </summary>
        /// <value></value>
        internal string Provider
        {
            get
            {
                return _provider;
            }
            set
            {
                _provider = value;
            }
        }

        private int _maxResults = -1;

        /// <summary>
        /// Maximum number of result to return for this request.
        /// </summary>
        /// <value></value>
        internal int MaxResults
        {
            get
            {
                return _maxResults;
            }
            set
            {
                _maxResults = value;
            }
        }

        private string[] _component;

        /// <summary>
        /// Component filter for command help. 
        /// </summary>
        /// <value></value>
        internal string[] Component
        {
            get
            {
                return _component;
            }
            set
            {
                _component = value;
            }
        }

        private string[] _role;

        /// <summary>
        /// Role filter for command help. 
        /// </summary>
        /// <value></value>
        internal string[] Role
        {
            get
            {
                return _role;
            }
            set
            {
                _role = value;
            }
        }

        private string[] _functionality;

        /// <summary>
        /// Functionality filter for command help. 
        /// </summary>
        /// <value></value>
        internal string[] Functionality
        {
            get
            {
                return _functionality;
            }
            set
            {
                _functionality = value;
            }
        }

        /// <summary>
        /// Keeps track of get-help cmdlet call origin. It can be called 
        /// directly by the user or indirectly by a script that a user calls.
        /// </summary>
        internal CommandOrigin CommandOrigin
        {
            get { return _origin; }
            set { _origin = value; }
        }
        private CommandOrigin _origin;

        /// <summary>
        /// Following validation will be done, (in order)
        /// 
        /// 1. If everything is empty, do default help. 
        /// 2. If target is empty, set it to be provider if currently doing provider help only. Otherwise, set it to be *
        /// 3. If any special parameters like component, role, functionality are specified, do command help only.
        /// 4. If command help is requested, search for alias also.
        /// 5. If help category is none, set it to be all.
        /// 6. Don't do default help.
        /// 
        /// </summary>
        internal void Validate()
        {
            if (String.IsNullOrEmpty(_target)
                && _helpCategory == HelpCategory.None
                && String.IsNullOrEmpty(_provider)
                && _component == null
                && _role == null
                && _functionality == null
            )
            {
                _target = "default";
                _helpCategory = HelpCategory.DefaultHelp;
                return;
            }

            if (String.IsNullOrEmpty(_target))
            {
                if (!String.IsNullOrEmpty(_provider) &&
                    (_helpCategory == HelpCategory.None || _helpCategory == HelpCategory.Provider)
                )
                {
                    _target = _provider;
                }
                else
                {
                    _target = "*";
                }
            }

            // if either of component/role/functionality is specified then look in the
            // following help categories
            if ((!(_component == null && _role == null && _functionality == null)) &&
                (_helpCategory == HelpCategory.None))
            {
                _helpCategory = HelpCategory.Alias | HelpCategory.Cmdlet | HelpCategory.Function | HelpCategory.Filter | HelpCategory.ExternalScript | HelpCategory.ScriptCommand | HelpCategory.Workflow;

                return;
            }

            if ((_helpCategory & HelpCategory.Cmdlet) > 0)
            {
                _helpCategory |= HelpCategory.Alias;
            }

            if (_helpCategory == HelpCategory.None)
            {
                _helpCategory = HelpCategory.All;
            }

            _helpCategory &= ~HelpCategory.DefaultHelp;

            return;
        }
    }
}
