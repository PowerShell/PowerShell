#if UNIX

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation.Language;
using System.Text;

namespace System.Management.Automation
{
    /// <summary>
    /// The Unix command manpage information.
    /// </summary>
    public class ManpageInfo : CommandInfo
    {
        #region ctor

        /// <summary>
        /// Constructs a Manpage object for Unix command.  This should only
        /// be used for generating short desription for get-help.
        /// </summary>
        /// <param name="name">
        /// The name of the command.
        /// </param>
        /// <param name="manSectionNum">
        /// Man ection number.
        /// </param>
        /// <param name="shortDescription">
        /// A one line short description of the command.
        /// </param>
        internal ManpageInfo(
            string name,
            string manSectionNum,
            string shortDescription)
            : base(name, CommandTypes.Manpage)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            _manSectionNum = manSectionNum;
            _shortDescription = shortDescription;
        }

        /// <summary>
        /// This is a copy constructor, used primarily for get-command.
        /// </summary>
        internal ManpageInfo(ManpageInfo other)
            : base(other)
        {
            _manSectionNum = other._manSectionNum;
            _shortDescription = other._shortDescription;
        }

        #endregion ctor

        #region public members

        /// <summary>
        /// Gets the Man section number of the command.
        /// </summary>
        public string ManSectionNum
        {
            get
            {
                return _manSectionNum;
            }
        }

        private readonly string _manSectionNum = string.Empty;

        /// <summary>
        /// Gets the shortDescription of the command.
        /// </summary>
        public string ShortDescription
        {
            get
            {
                return _shortDescription;
            }
        }

        private readonly string _shortDescription = string.Empty;

        internal override HelpCategory HelpCategory
        {
            get { return HelpCategory.Manpage; }
        }

        /// <summary>
        /// No output type is referred to Manpage.
        /// </summary>
        public override ReadOnlyCollection<PSTypeName> OutputType
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the synopsis of the command.
        /// </summary>
        public override string Definition
        {
            get
            {
                return _shortDescription;
            }
        }

        #endregion public members

        #region internal/private members
        #endregion internal/private members
    }
}

#endif
