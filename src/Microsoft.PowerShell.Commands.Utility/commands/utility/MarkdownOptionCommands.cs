// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Management.Automation;
using Microsoft.PowerShell.MarkdownRender;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// </summary>
    [Cmdlet(
        VerbsCommon.Set, "MarkdownOption",
        DefaultParameterSetName = IndividualSetting,
        HelpUri = "TBD"
    )]
    [OutputType(typeof(Microsoft.PowerShell.MarkdownRender.MarkdownOptionInfo))]
    public class SetMarkdownOptionCommand : PSCmdlet
    {
        /// <summary>
        /// </summary>
        [ValidatePattern(@"^\[*[0-9;]*?m{1}")]
        [Parameter(ParameterSetName = IndividualSetting)]
        public string Header1Color { get; set;}

        /// <summary>
        /// </summary>
        [ValidatePattern(@"^\[*[0-9;]*?m{1}")]
        [Parameter(ParameterSetName = IndividualSetting)]
        public string Header2Color { get; set;}

        /// <summary>
        /// </summary>
        [ValidatePattern(@"^\[*[0-9;]*?m{1}")]
        [Parameter(ParameterSetName = IndividualSetting)]
        public string Header3Color { get; set;}

        /// <summary>
        /// </summary>
        [ValidatePattern(@"^\[*[0-9;]*?m{1}")]
        [Parameter(ParameterSetName = IndividualSetting)]
        public string Header4Color { get; set;}

        /// <summary>
        /// </summary>
        [ValidatePattern(@"^\[*[0-9;]*?m{1}")]
        [Parameter(ParameterSetName = IndividualSetting)]
        public string Header5Color { get; set;}

        /// <summary>
        /// </summary>
        [ValidatePattern(@"^\[*[0-9;]*?m{1}")]
        [Parameter(ParameterSetName = IndividualSetting)]
        public string Header6Color { get; set;}

        /// <summary>
        /// </summary>
        [ValidatePattern(@"^\[*[0-9;]*?m{1}")]
        [Parameter(ParameterSetName = IndividualSetting)]
        public string CodeBlockForegroundColor { get; set;}

        /// <summary>
        /// </summary>
        [ValidatePattern(@"^\[*[0-9;]*?m{1}")]
        [Parameter(ParameterSetName = IndividualSetting)]
        public string CodeBlockBackgroundColor { get; set;}

        /// <summary>
        /// </summary>
        [ValidatePattern(@"^\[*[0-9;]*?m{1}")]
        [Parameter(ParameterSetName = IndividualSetting)]
        public string ImageAltTextForegroundColor { get; set;}

        /// <summary>
        /// </summary>
        [ValidatePattern(@"^\[*[0-9;]*?m{1}")]
        [Parameter(ParameterSetName = IndividualSetting)]
        public string LinkForegroundColor { get; set;}

        /// <summary>
        /// </summary>
        [ValidatePattern(@"^\[*[0-9;]*?m{1}")]
        [Parameter(ParameterSetName = IndividualSetting)]
        public string ItalicsForegroundColor { get; set;}

        /// <summary>
        /// </summary>
        [ValidatePattern(@"^\[*[0-9;]*?m{1}")]
        [Parameter(ParameterSetName = IndividualSetting)]
        public string BoldForegroundColor { get; set;}

        /// <summary>
        /// </summary>
        [Parameter()]
        public SwitchParameter PassThru { get; set;}

        /// <summary>
        /// </summary>
        [ValidateNotNullOrEmpty]
        [Parameter(ParameterSetName = ThemeParamSet, Mandatory = true)]
        public string Theme { get; set;}

        /// <summary>
        /// </summary>
        [ValidateNotNullOrEmpty]
        [Parameter(ParameterSetName = InputObjectParamSet, Mandatory = true, ValueFromPipeline = true)]
        public PSObject InputObject { get; set;}

        private const string IndividualSetting = "IndividualSetting";

        private const string InputObjectParamSet = "InputObject";

        private const string ThemeParamSet = "Theme";

        /// <summary>
        /// </summary>
        protected override void EndProcessing()
        {
            MarkdownOptionInfo mdOptionInfo = null;

            switch(ParameterSetName)
            {
                case ThemeParamSet:
                    mdOptionInfo = new MarkdownOptionInfo();
                    if(string.Equals(Theme, "Light", StringComparison.OrdinalIgnoreCase))
                    {
                        mdOptionInfo.SetLightTheme();
                    }
                    else if(string.Equals(Theme, "Dark", StringComparison.OrdinalIgnoreCase))
                    {
                        mdOptionInfo.SetDarkTheme();
                    }
                    break;

                case InputObjectParamSet:
                    Object baseObj = InputObject.BaseObject;
                    mdOptionInfo = baseObj as MarkdownOptionInfo;

                    if(mdOptionInfo == null)
                    {
                        throw new ArgumentException();
                    }

                    break;

                case IndividualSetting:
                    mdOptionInfo = new MarkdownOptionInfo();
                    SetOptions(mdOptionInfo);
                    break;
            }

            var sessionVar = SessionState.PSVariable;
            sessionVar.Set("MarkdownOptionInfo", mdOptionInfo);

            if(PassThru.IsPresent)
            {
                WriteObject(mdOptionInfo);
            }
        }

        private void SetOptions(MarkdownOptionInfo mdOptionInfo)
        {
            if (!String.IsNullOrEmpty(Header1Color))
            {
                mdOptionInfo.Header1 = Header1Color;
            }

            if (!String.IsNullOrEmpty(Header2Color))
            {
                mdOptionInfo.Header2 = Header2Color;
            }

            if (!String.IsNullOrEmpty(Header3Color))
            {
                mdOptionInfo.Header3 = Header3Color;
            }

            if (!String.IsNullOrEmpty(Header4Color))
            {
                mdOptionInfo.Header4 = Header4Color;
            }

            if (!String.IsNullOrEmpty(Header5Color))
            {
                mdOptionInfo.Header5 = Header5Color;
            }

            if (!String.IsNullOrEmpty(Header6Color))
            {
                mdOptionInfo.Header6 = Header6Color;
            }

            if (!String.IsNullOrEmpty(CodeBlockBackgroundColor))
            {
                mdOptionInfo.Code = CodeBlockBackgroundColor;
            }

            if (!String.IsNullOrEmpty(CodeBlockForegroundColor))
            {
                mdOptionInfo.Code = CodeBlockForegroundColor;
            }

            if (!String.IsNullOrEmpty(ImageAltTextForegroundColor))
            {
                mdOptionInfo.Image = ImageAltTextForegroundColor;
            }

            if (!String.IsNullOrEmpty(LinkForegroundColor))
            {
                mdOptionInfo.Link = LinkForegroundColor;
            }

            if (!String.IsNullOrEmpty(ItalicsForegroundColor))
            {
                mdOptionInfo.EmphasisItalics = ItalicsForegroundColor;
            }

            if (!String.IsNullOrEmpty(BoldForegroundColor))
            {
                mdOptionInfo.EmphasisBold = BoldForegroundColor;
            }
        }
    }

    /// <summary>
    /// </summary>
    [Cmdlet(
        VerbsCommon.Get, "MarkdownOption",
        HelpUri = "TBD"
    )]
    [OutputType(typeof(Microsoft.PowerShell.MarkdownRender.MarkdownOptionInfo))]
    public class GetMarkdownOptionCommand : PSCmdlet
    {
        /// <summary>
        /// </summary>
        protected override void EndProcessing()
        {
            WriteObject(SessionState.PSVariable.GetValue("MarkdownOptionInfo", new MarkdownOptionInfo()));
        }
    }
}
