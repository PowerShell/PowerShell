// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Windows.Documents;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// ViewModel for the Help Dialog used to:
    ///     build the help document
    ///     search the help document
    ///     offer text for labels.
    /// </summary>
    internal class HelpViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// The builder for the help FlowDocument Paragraph  used in a RichEditText control.
        /// </summary>
        private readonly HelpParagraphBuilder helpBuilder;

        /// <summary>
        /// Searcher for selecting current matches in paragraph text.
        /// </summary>
        private readonly ParagraphSearcher searcher;

        /// <summary>
        /// Title of the help window.
        /// </summary>
        private readonly string helpTitle;

        /// <summary>
        /// the zoom bound to the zoom slider value.
        /// </summary>
        private double zoom = 100;

        /// <summary>
        /// Text to be found. This is bound to the find TextBox.
        /// </summary>
        private string findText;

        /// <summary>
        /// text for the number of matches found.
        /// </summary>
        private string matchesLabel;

        /// <summary>
        /// Initializes a new instance of the HelpViewModel class.
        /// </summary>
        /// <param name="psObj">Object containing help.</param>
        /// <param name="documentParagraph">Paragraph in which help text is built/searched.</param>
        internal HelpViewModel(PSObject psObj, Paragraph documentParagraph)
        {
            Debug.Assert(psObj != null, "ensured by caller");
            Debug.Assert(documentParagraph != null, "ensured by caller");

            this.helpBuilder = new HelpParagraphBuilder(documentParagraph, psObj);
            this.helpBuilder.BuildParagraph();
            this.searcher = new ParagraphSearcher();
            this.helpBuilder.PropertyChanged += this.HelpBuilder_PropertyChanged;
            this.helpTitle = string.Format(
                CultureInfo.CurrentCulture,
                HelpWindowResources.HelpTitleFormat,
                HelpParagraphBuilder.GetPropertyString(psObj, "name"));
        }

        #region INotifyPropertyChanged Members
        /// <summary>
        /// Used to notify of property changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        /// <summary>
        /// Gets or sets the Zoom bound to the zoom slider value.
        /// </summary>
        public double Zoom
        {
            get
            {
                return this.zoom;
            }

            set
            {
                this.zoom = value;
                this.OnNotifyPropertyChanged("Zoom");
                this.OnNotifyPropertyChanged("ZoomLabel");
                this.OnNotifyPropertyChanged("ZoomLevel");
            }
        }

        /// <summary>
        /// Gets the value bound to the RichTextEdit zoom, which is calculated based on the zoom.
        /// </summary>
        public double ZoomLevel
        {
            get
            {
                return this.zoom / 100.0;
            }
        }

        /// <summary>
        /// Gets the label to be displayed for the zoom.
        /// </summary>
        public string ZoomLabel
        {
            get
            {
                return string.Format(CultureInfo.CurrentCulture, HelpWindowResources.ZoomLabelTextFormat, this.zoom);
            }
        }

        /// <summary>
        /// Gets or sets the text to be found.
        /// </summary>
        public string FindText
        {
            get
            {
                return this.findText;
            }

            set
            {
                this.findText = value;
                this.Search();
                this.SetMatchesLabel();
            }
        }

        /// <summary>
        /// Gets the title of the window.
        /// </summary>
        public string HelpTitle
        {
            get
            {
                return this.helpTitle;
            }
        }

        /// <summary>
        /// Gets or sets the label for current matches.
        /// </summary>
        public string MatchesLabel
        {
            get
            {
                return this.matchesLabel;
            }

            set
            {
                this.matchesLabel = value;
                this.OnNotifyPropertyChanged("MatchesLabel");
            }
        }

        /// <summary>
        /// Gets a value indicating whether there are matches to go to.
        /// </summary>
        public bool CanGoToNextOrPrevious
        {
            get
            {
                return this.HelpBuilder.HighlightCount != 0;
            }
        }

        /// <summary>
        /// Gets the searcher for selecting current matches in paragraph text.
        /// </summary>
        internal ParagraphSearcher Searcher
        {
            get { return this.searcher; }
        }

        /// <summary>
        /// Gets the paragraph builder used to write help content.
        /// </summary>
        internal HelpParagraphBuilder HelpBuilder
        {
            get { return this.helpBuilder; }
        }

        /// <summary>
        /// Highlights all matches to this.findText
        /// Called when findText changes or whenever the search has to be refreshed
        /// </summary>
        internal void Search()
        {
            this.HelpBuilder.HighlightAllInstancesOf(this.findText, HelpWindowSettings.Default.HelpSearchMatchCase, HelpWindowSettings.Default.HelpSearchWholeWord);
            this.searcher.ResetSearch();
        }

        /// <summary>
        /// Increases Zoom if not above maximum.
        /// </summary>
        internal void ZoomIn()
        {
            if (this.Zoom + HelpWindow.ZoomInterval <= HelpWindow.MaximumZoom)
            {
                this.Zoom += HelpWindow.ZoomInterval;
            }
        }

        /// <summary>
        /// Decreases Zoom if not below minimum.
        /// </summary>
        internal void ZoomOut()
        {
            if (this.Zoom - HelpWindow.ZoomInterval >= HelpWindow.MinimumZoom)
            {
                this.Zoom -= HelpWindow.ZoomInterval;
            }
        }

        /// <summary>
        /// Called to update the matches label.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void HelpBuilder_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "HighlightCount")
            {
                this.SetMatchesLabel();
                this.OnNotifyPropertyChanged("CanGoToNextOrPrevious");
            }
        }

        /// <summary>
        /// Sets the current matches label.
        /// </summary>
        private void SetMatchesLabel()
        {
            if (this.findText == null || this.findText.Trim().Length == 0)
            {
                this.MatchesLabel = string.Empty;
            }
            else
            {
                if (this.HelpBuilder.HighlightCount == 0)
                {
                    this.MatchesLabel = HelpWindowResources.NoMatches;
                }
                else
                {
                    if (this.HelpBuilder.HighlightCount == 1)
                    {
                        this.MatchesLabel = HelpWindowResources.OneMatch;
                    }
                    else
                    {
                        this.MatchesLabel = string.Format(
                            CultureInfo.CurrentCulture,
                            HelpWindowResources.SomeMatchesFormat,
                            this.HelpBuilder.HighlightCount);
                    }
                }
            }
        }

        /// <summary>
        /// Called internally to notify when a proiperty changed.
        /// </summary>
        /// <param name="propertyName">Property name.</param>
        private void OnNotifyPropertyChanged(string propertyName)
        {
            #pragma warning disable IDE1005 // IDE1005: Delegate invocation can be simplified.
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
            #pragma warning restore IDE1005s
        }
    }
}
