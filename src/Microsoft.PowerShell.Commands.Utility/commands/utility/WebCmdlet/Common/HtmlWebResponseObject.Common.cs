#if !CORECLR

/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System;
using System.Management.Automation;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using mshtml;
using System.Diagnostics;
using System.Threading;
using ExecutionContext = System.Management.Automation.ExecutionContext;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Response object for html content
    /// </summary>
    public partial class HtmlWebResponseObject : WebResponseObject, IDisposable
    {
#region Properties

        /// <summary>
        /// gets or protected sets the Content property
        /// </summary>
        public new string Content { get; private set; }

        // The HTML document
        private IHTMLDocument2 _parsedHtml;

        // The reset event for synchronizing the 'IHTMLDocument2.write()' call
        private ManualResetEventSlim _stateChangeResetEvent;

        // The reset event for synchronizing loading the document
        private ManualResetEventSlim _loadDocumentResetEvent;

        // The handler for the 'onreadystatechange' event
        private HTMLDocumentEvents2_onreadystatechangeEventHandler _onreadystatechangeEventHandler;

        // The exception thrown during the parsing
        private Exception _parsingException;

        // The current execution context
        private readonly ExecutionContext _executionContext;

        // The flag that notifies the worker thread to stop loading the document
        private bool _stopWorkerThread;

        // The flag that indicates the html is parsed
        private bool _htmlParsed = false;

        /// <summary>
        /// gets the ParsedHtml property
        /// </summary>
        public IHTMLDocument2 ParsedHtml
        {
            get
            {
                EnsureHtmlParser();

                return _parsedHtml;
            }
        }

        private FormObjectCollection _forms;

        /// <summary>
        /// gets the Forms property
        /// </summary>
        public FormObjectCollection Forms
        {
            get
            {
                if (_forms == null)
                {
                    _forms = BuildFormsCollection();
                }

                return _forms;
            }
        }

        private WebCmdletElementCollection _inputFields;

        /// <summary>
        /// gets the Fields property
        /// </summary>
        public WebCmdletElementCollection InputFields
        {
            get
            {
                if (_inputFields == null)
                {
                    EnsureHtmlParser();

                    List<PSObject> parsedFields = new List<PSObject>();
                    foreach (IHTMLElement element in _parsedHtml.all)
                    {
                        if (element.tagName.Equals("INPUT", StringComparison.OrdinalIgnoreCase))
                        {
                            parsedFields.Add(CreateHtmlObject(element, true));
                        }
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(element);
                    }

                    _inputFields = new WebCmdletElementCollection(parsedFields);
                }

                return _inputFields;
            }
        }

        private WebCmdletElementCollection _links;

        /// <summary>
        /// gets the Links property
        /// </summary>
        public WebCmdletElementCollection Links
        {
            get
            {
                if (_links == null)
                {
                    EnsureHtmlParser();

                    List<PSObject> parsedLinks = new List<PSObject>();
                    foreach (IHTMLElement element in _parsedHtml.links)
                    {
                        parsedLinks.Add(CreateHtmlObject(element, true));
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(element);
                    }

                    _links = new WebCmdletElementCollection(parsedLinks);
                }

                return _links;
            }
        }

        private WebCmdletElementCollection _images;

        /// <summary>
        /// gets the Images property
        /// </summary>
        public WebCmdletElementCollection Images
        {
            get
            {
                if (_images == null)
                {
                    EnsureHtmlParser();

                    List<PSObject> parsedImages = new List<PSObject>();
                    foreach (IHTMLElement element in _parsedHtml.images)
                    {
                        parsedImages.Add(CreateHtmlObject(element, true));
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(element);
                    }

                    _images = new WebCmdletElementCollection(parsedImages);
                }

                return _images;
            }
        }

        private WebCmdletElementCollection _scripts;

        /// <summary>
        /// gets the Scripts property
        /// </summary>
        public WebCmdletElementCollection Scripts
        {
            get
            {
                if (_scripts == null)
                {
                    EnsureHtmlParser();

                    List<PSObject> parsedScripts = new List<PSObject>();
                    foreach (IHTMLElement element in _parsedHtml.scripts)
                    {
                        parsedScripts.Add(CreateHtmlObject(element, true));
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(element);
                    }

                    _scripts = new WebCmdletElementCollection(parsedScripts);
                }

                return _scripts;
            }
        }

        private WebCmdletElementCollection _allElements;

        /// <summary>
        /// gets the Elements property
        /// </summary>
        public WebCmdletElementCollection AllElements
        {
            get
            {
                if (_allElements == null)
                {
                    EnsureHtmlParser();

                    List<PSObject> parsedElements = new List<PSObject>();
                    foreach (IHTMLElement element in _parsedHtml.all)
                    {
                        parsedElements.Add(CreateHtmlObject(element, true));
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(element);
                    }

                    _allElements = new WebCmdletElementCollection(parsedElements);
                }

                return _allElements;
            }
        }

#endregion Properties

#region Private Fields

        private static Regex _tagRegex;
        private static Regex _attribsRegex;
        private static Regex _attribNameValueRegex;

#endregion Private Fields

#region Methods

        // The "onreadystatechange" event handler
        private void ReadyStateChanged(IHTMLEventObj obj)
        {
            if (String.Equals("complete", _parsedHtml.readyState, StringComparison.OrdinalIgnoreCase))
            {
                _stateChangeResetEvent.Set();
            }
            System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
        }

        // Load the document in a worker thread
        private void LoadDocumentInMtaThread(Object state)
        {
            try
            {
                // Create a new IHTMLDocument2 object
                _parsedHtml = (IHTMLDocument2)new HTMLDocument();

                // Attach the event handler
                var events = (HTMLDocumentEvents2_Event)_parsedHtml;
                events.onreadystatechange += _onreadystatechangeEventHandler;

                // Write the content and close the document
                _parsedHtml.write(Content);
                _parsedHtml.close();

                // Wait for the onReadyStateChange event to be fired. On IE9, this never happens
                // so we check the readyState directly as well.
                bool wait = true;
                while (wait && !_stopWorkerThread)
                {
                    if (String.Equals("complete", _parsedHtml.readyState, StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    wait = !_stateChangeResetEvent.Wait(100);
                }

                // Detach the event handler
                events.onreadystatechange -= _onreadystatechangeEventHandler;
            }
            catch (Exception e)
            {
                _parsingException = e;
            }
            finally
            {
                _loadDocumentResetEvent.Set();
            }
        }

        private void EnsureHtmlParser()
        {
            if (_htmlParsed == false)
            {
                // Initialization
                _stopWorkerThread = false;
                _parsingException = null;
                _stateChangeResetEvent = new ManualResetEventSlim();
                _loadDocumentResetEvent = new ManualResetEventSlim();
                _onreadystatechangeEventHandler = new HTMLDocumentEvents2_onreadystatechangeEventHandler(ReadyStateChanged);

                // The IHTMLDocument events cannot be handled in STA ApartmentState, so we use a worker thread to load the document
                ThreadPool.QueueUserWorkItem(new WaitCallback(LoadDocumentInMtaThread));

                // Wait for the worker thread to finish loading the document. In the meantime, we check the Ctrl-C every 500ms
                bool wait = true;
                while (wait)
                {
                    if (_executionContext.CurrentPipelineStopping)
                    {
                        // Signal and wait for the worker thread to exit, then break out the loop
                        _stopWorkerThread = true;
                        _loadDocumentResetEvent.Wait();
                        break;
                    }

                    wait = !_loadDocumentResetEvent.Wait(500);
                }

                // Ctrl-C is typed
                if (_executionContext.CurrentPipelineStopping)
                {
                    throw new PipelineStoppedException();
                }

                // If there is no Ctrl-C, throw if an exception happened during the parsing
                if (_parsingException != null)
                {
                    throw _parsingException;
                }

                // Parsing was successful
                _htmlParsed = true;
            }

            if (_tagRegex == null)
            {
                _tagRegex = new Regex(@"<\w+((\s+[^""'>/=\s\p{Cc}]+(\s*=\s*(?:"".*?""|'.*?'|[^'"">\s]+))?)+\s*|\s*)/?>",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }

            if (_attribsRegex == null)
            {
                _attribsRegex = new Regex(@"(?<=\s+)([^""'>/=\s\p{Cc}]+(\s*=\s*(?:"".*?""|'.*?'|[^'"">\s]+))?)",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }

            if (_attribNameValueRegex == null)
            {
                _attribNameValueRegex = new Regex(@"([^""'>/=\s\p{Cc}]+)(?:\s*=\s*(?:""(.*?)""|'(.*?)'|([^'"">\s]+)))?",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }
        }

        private PSObject CreateHtmlObject(IHTMLElement element, bool addTagName)
        {
            PSObject elementObject = new PSObject();

            elementObject.Properties.Add(new PSNoteProperty("innerHTML", element.innerHTML));
            elementObject.Properties.Add(new PSNoteProperty("innerText", element.innerText));
            elementObject.Properties.Add(new PSNoteProperty("outerHTML", element.outerHTML));
            elementObject.Properties.Add(new PSNoteProperty("outerText", element.outerText));
            if (addTagName)
            {
                elementObject.Properties.Add(new PSNoteProperty("tagName", element.tagName));
            }

            ParseAttributes(element.outerHTML, elementObject);

            return elementObject;
        }


        private void ParseAttributes(string outerHtml, PSObject elementObject)
        {
            // We might get an empty input for a directive from the HTML file
            if (!string.IsNullOrEmpty(outerHtml))
            {
                // Extract just the opening tag of the HTML element (omitting the closing tag and any contents,
                // including contained HTML elements)
                var match = _tagRegex.Match(outerHtml);

                // Extract all the attribute specifications within the HTML element opening tag
                var attribMatches = _attribsRegex.Matches(match.Value);

                foreach (Match attribMatch in attribMatches)
                {
                    // Extract the name and value for this attribute (allowing for variations like single/double/no
                    // quotes, and no value at all)
                    var nvMatches = _attribNameValueRegex.Match(attribMatch.Value);
                    Debug.Assert(nvMatches.Groups.Count == 5);

                    // Name is always captured by group #1
                    string name = nvMatches.Groups[1].Value;

                    // The value (if any) is captured by group #2, #3, or #4, depending on quoting or lack thereof
                    string value = null;
                    if (nvMatches.Groups[2].Success)
                    {
                        value = nvMatches.Groups[2].Value;
                    }
                    else if (nvMatches.Groups[3].Success)
                    {
                        value = nvMatches.Groups[3].Value;
                    }
                    else if (nvMatches.Groups[4].Success)
                    {
                        value = nvMatches.Groups[4].Value;
                    }

                    elementObject.Properties.Add(new PSNoteProperty(name, value));
                }
            }
        }

        private FormObjectCollection BuildFormsCollection()
        {
            FormObjectCollection forms = new FormObjectCollection();

            EnsureHtmlParser();
            foreach (IHTMLFormElement form in _parsedHtml.forms)
            {
                string id = GetElementId(form as IHTMLElement);
                if (null == id)
                {
                    id = form.name;
                }

                FormObject f = new FormObject(id, form.method, form.action);
                foreach (IHTMLElement element in form)
                {
                    IHTMLInputElement input = element as IHTMLInputElement;
                    if (null != input)
                    {
                        id = GetElementId(input as IHTMLElement);
                        if (null == id)
                        {
                            id = input.name;
                        }

                        f.AddField(id, input.value);
                    }

                    System.Runtime.InteropServices.Marshal.ReleaseComObject(element);
                }

                forms.Add(f);

                System.Runtime.InteropServices.Marshal.ReleaseComObject(form);
            }

            return (forms);
        }

        private string GetElementId(IHTMLElement element)
        {
            return (null == element ? null : element.id);
        }

        /// <summary>
        /// Reads the response content from the web response.
        /// </summary>
        private void InitializeContent()
        {
            string contentType = ContentHelper.GetContentType(BaseResponse);
            if (ContentHelper.IsText(contentType))
            {
                // fill the Content buffer
                string characterSet = WebResponseHelper.GetCharacterSet(BaseResponse);
                this.Content = StreamHelper.DecodeStream(RawContentStream, characterSet);
            }
            else
            {
                this.Content = string.Empty;
            }
        }
#endregion Methods

        /// <summary>
        /// Dispose the the instance of the class.
        /// </summary>
        public void Dispose()
        {
            CleanupNativeResources();

            if (_loadDocumentResetEvent != null)
            {
                _loadDocumentResetEvent.Dispose();
            }
            if (_stateChangeResetEvent != null)
            {
                _stateChangeResetEvent.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizer to free the COM objects.
        /// </summary>
        ~HtmlWebResponseObject()
        {
            CleanupNativeResources();
        }

        private void CleanupNativeResources()
        {
            if (_parsedHtml != null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(_parsedHtml);
            }
        }
    }
}
#endif