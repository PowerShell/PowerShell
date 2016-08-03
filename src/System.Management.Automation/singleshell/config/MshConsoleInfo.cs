/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Globalization;
using System.IO;
using System.Xml;
using System.Text;
using System.Collections.ObjectModel;
using System.Management.Automation.Internal;
using System.Collections.Generic;

namespace System.Management.Automation.Runspaces
{
    /// <summary>
    /// Class that understands Monad Console File Format. The format for the console
    /// file is applied/read by this class only.
    /// 
    /// Functionality:
    /// 1. Schema version verification check.
    /// 2. Data values for the content represented by Console file. Later this data
    /// is used by other components ( MshConsoleInfo ) to construct Monad Types ( like
    /// PSSnapInInfo ).
    /// 3. Owns responsibilty to read/write Files.
    /// 
    /// Risk:
    /// File Acces related security issues.
    /// 
    /// Requires:
    /// Might require Permissions to read/write into files.
    /// </summary>
    /// <!--
    /// Monad Console File is in the following format:
    /// 	    <?xml version="1.0"?>
    /// <PSConsoleFile ConsoleSchemaVersion=1.0>
    ///<PSVersion>1</PSVersion>
    ///<PSSnapIns>
    /// <PSSnapIn Name=ExchangeMshSnapin />
    /// <PSSnapIn Name=MOMMshSnapin />
    ///</PSSnapIns>
    ///</PSConsoleFile>
    /// -->
    internal class PSConsoleFileElement
    {
        #region Console format (xml) tags
        // Create constants for each of the xml tags 
        private const string MSHCONSOLEFILE = "PSConsoleFile";
        private const string CSCHEMAVERSION = "ConsoleSchemaVersion";
        private const string CSCHEMAVERSIONNUMBER = "1.0";
        private const string PSVERSION = "PSVersion";
        private const string SNAPINS = "PSSnapIns";
        private const string SNAPIN = "PSSnapIn";
        private const string SNAPINNAME = "Name";

        #endregion

        #region Data

        /// <summary>
        /// MonadVersion from the console file
        /// </summary>
        internal string MonadVersion { get; }

        /// <summary>
        /// List of MshSnapin IDs from the console file
        /// </summary>
        internal Collection<string> PSSnapIns { get; }

        #endregion

        #region Constructor

        private PSConsoleFileElement(string version)
        {
            MonadVersion = version;
            // Dont make collections null...
            // making them null, wont be good for foreach statements
            PSSnapIns = new Collection<string>();
        }

        #endregion

        #region tracer

        private static readonly PSTraceSource s_mshsnapinTracer = PSTraceSource.GetTracer("MshSnapinLoadUnload", "Loading and unloading mshsnapins", false);
        #endregion

        #region Static Methods

        /// <summary>
        /// Writes MshConsoleInfo object in Monad Console format into the file specified by the <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The absolute path of the file into which the content is saved.</param>        
        /// <param name="version">The version of PowerShell.</param>
        /// <param name="snapins">The external snapins that are loaded in the console</param>
        /// <exception cref="ArgumentNullException">
        /// The path value is null.
        /// </exception>
        /// <!--
        /// Caller should not pass a null value for path.
        /// -->
        internal static void WriteToFile(string path, Version version, IEnumerable<PSSnapInInfo> snapins)
        {
            Diagnostics.Assert(path != null, "Filename should not be null");

            s_mshsnapinTracer.WriteLine("Saving console info to file {0}.", path);

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = Encoding.UTF8;

            using (Stream stream = new FileStream(path, FileMode.OpenOrCreate))
            {
                // Generate xml for the consoleinfo object.
                using (XmlWriter writer = XmlWriter.Create(stream, settings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement(MSHCONSOLEFILE);
                    writer.WriteAttributeString(CSCHEMAVERSION, CSCHEMAVERSIONNUMBER);

                    writer.WriteStartElement(PSVERSION);
                    writer.WriteString(version.ToString());
                    writer.WriteEndElement(); // MonadVersion

                    writer.WriteStartElement(SNAPINS);
                    foreach (PSSnapInInfo mshSnapIn in snapins)
                    {
                        writer.WriteStartElement(SNAPIN);
                        writer.WriteAttributeString(SNAPINNAME, mshSnapIn.Name);
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement(); // MshSnapins

                    writer.WriteEndElement(); // Monad_ConsoleFile
                    writer.WriteEndDocument();
                }
            }

            s_mshsnapinTracer.WriteLine("Saving console info succeeded.");
        }

        /// <summary>
        /// Reads a Monad Console file specified by <paramref name="path"/> and constructs
        /// a PSConsoleFileElement.
        /// </summary>
        /// <param name="path">The absolute path of the file to read from.</param>
        /// <returns>A MShConsoleFileElement object that represents content of the console file.</returns>
        /// <remarks>The return object wont be null.</remarks>
        /// <exception cref="XmlException">
        /// There is a load or parser error in the XML.
        /// </exception>
        /// <!--
        /// Caller should not pass a null value for path.
        /// -->
        internal static PSConsoleFileElement CreateFromFile(string path)
        {
            Diagnostics.Assert(path != null, "Filename should not be null");

            s_mshsnapinTracer.WriteLine("Loading console info from file {0}.", path);

            XmlDocument doc = InternalDeserializer.LoadUnsafeXmlDocument(
                new FileInfo(path),
                false, /* ignore whitespace, comments, etc. */
                null); /* default maxCharactersInDocument */

            // Validate content
            if (doc[MSHCONSOLEFILE] == null)
            {
                s_mshsnapinTracer.TraceError("Console file {0} doesn't contain tag {1}.", path, MSHCONSOLEFILE);

                throw new XmlException(
                    StringUtil.Format(ConsoleInfoErrorStrings.MonadConsoleNotFound, path));
            }

            if ((doc[MSHCONSOLEFILE][PSVERSION] == null) ||
                 (string.IsNullOrEmpty(doc[MSHCONSOLEFILE][PSVERSION].InnerText)))
            {
                s_mshsnapinTracer.TraceError("Console file {0} doesn't contain tag {1}.", path, PSVERSION);

                throw new XmlException(
                    StringUtil.Format(ConsoleInfoErrorStrings.MonadVersionNotFound, path));
            }

            // This will never be null..
            XmlElement xmlElement = (XmlElement)doc[MSHCONSOLEFILE];

            if (xmlElement.HasAttribute(CSCHEMAVERSION))
            {
                if (!xmlElement.GetAttribute(CSCHEMAVERSION).Equals(CSCHEMAVERSIONNUMBER, StringComparison.OrdinalIgnoreCase))
                {
                    string resourceTemplate =
                            StringUtil.Format(ConsoleInfoErrorStrings.BadConsoleVersion, path);
                    string message = string.Format(CultureInfo.CurrentCulture,
                        resourceTemplate, CSCHEMAVERSIONNUMBER);

                    s_mshsnapinTracer.TraceError(message);

                    throw new XmlException(message);
                }
            }
            else
            {
                s_mshsnapinTracer.TraceError("Console file {0} doesn't contain tag schema version.", path);

                throw new XmlException(
                        StringUtil.Format(ConsoleInfoErrorStrings.BadConsoleVersion, path));
            }

            //process MonadVersion
            //This will never be null..
            xmlElement = (XmlElement)doc[MSHCONSOLEFILE][PSVERSION];

            // Construct PSConsoleFileElement as we seem to have valid data
            PSConsoleFileElement consoleFileElement = new PSConsoleFileElement(xmlElement.InnerText.Trim());

            bool isPSSnapInsProcessed = false;
            bool isPSVersionProcessed = false;

            for (XmlNode mshSnapInsNode = doc["PSConsoleFile"].FirstChild; mshSnapInsNode != null; mshSnapInsNode = mshSnapInsNode.NextSibling)
            {
                if (mshSnapInsNode.NodeType == XmlNodeType.Comment)
                {
                    // support comments inside a PSConsoleFile Element
                    continue;
                }

                //populate mshsnapin information
                xmlElement = mshSnapInsNode as XmlElement;

                if (null == xmlElement)
                {
                    throw new XmlException(ConsoleInfoErrorStrings.BadXMLFormat);
                }

                if (xmlElement.Name == PSVERSION)
                {
                    if (isPSVersionProcessed)
                    {
                        s_mshsnapinTracer.TraceError("Console file {0} contains more than one  msh versions", path);

                        throw new XmlException(StringUtil.Format(ConsoleInfoErrorStrings.MultipleMshSnapinsElementNotSupported, PSVERSION));
                    }

                    isPSVersionProcessed = true;
                    continue;
                }

                if (xmlElement.Name != SNAPINS)
                {
                    s_mshsnapinTracer.TraceError("Tag {0} is not supported in console file", xmlElement.Name);

                    throw new XmlException(StringUtil.Format(ConsoleInfoErrorStrings.BadXMLElementFound, xmlElement.Name, MSHCONSOLEFILE, PSVERSION, SNAPINS));
                }

                // PSSnapIns element is already processed. We dont support multiple
                // PSSnapIns elements
                if (isPSSnapInsProcessed)
                {
                    s_mshsnapinTracer.TraceError("Console file {0} contains more than one mshsnapin lists", path);

                    throw new XmlException(StringUtil.Format(ConsoleInfoErrorStrings.MultipleMshSnapinsElementNotSupported, SNAPINS));
                }

                // We are about to process mshsnapins element..so we should not 
                // process some more mshsnapins elements..this boolean keeps track
                // of this.
                isPSSnapInsProcessed = true;

                // decode all the child nodes of <PSSnapIns> node...
                for (XmlNode mshSnapInNode = xmlElement.FirstChild; mshSnapInNode != null; mshSnapInNode = mshSnapInNode.NextSibling)
                {
                    XmlElement mshSnapInElement = mshSnapInNode as XmlElement;

                    if ((null == mshSnapInElement) || (mshSnapInElement.Name != SNAPIN))
                    {
                        throw new XmlException(
                            StringUtil.Format(ConsoleInfoErrorStrings.PSSnapInNotFound, mshSnapInNode.Name));
                    }

                    string id = mshSnapInElement.GetAttribute(SNAPINNAME);

                    if (string.IsNullOrEmpty(id))
                    {
                        throw new XmlException(ConsoleInfoErrorStrings.IDNotFound);
                    }

                    consoleFileElement.PSSnapIns.Add(id);

                    s_mshsnapinTracer.WriteLine("Found in mshsnapin {0} in console file {1}", id, path);
                }
            }

            return consoleFileElement;
        }

        #endregion
    }

    /// <summary>
    /// Class that manages(reads/writes) Monad Console files and constructs objects
    /// that represent data in the console files.
    /// 
    /// Functionality:
    /// 1. Access point to the console files for Runspace and cmdlets
    /// 2. Depends on PSConsoleFileElement for reading/writing files
    /// </summary>
    /// <!--
    /// This object references PSSnapInInfo and PSSnapInReader classes and constructs
    /// PSSnapInfo objects that represent data in the console file. Runspace 
    /// Configuration and Cmdlets (add-pssnapin,remove-pssnapin etc ) are expected 
    /// to use this object.
    /// -->
    internal class MshConsoleInfo
    {
        #region Private Data

        // Monad Version that this console file depends on.
        // MshSnapins that are not shipped with monad.
        private readonly Collection<PSSnapInInfo> _externalPSSnapIns;
        // Monad specific mshsnapins
        private Collection<PSSnapInInfo> _defaultPSSnapIns;
        // An internal representation that tells whether a console file is modified.
        // A string that stores fileName of the current consoleinfo object

        #endregion

        #region Class specific data

        private static readonly PSTraceSource s_mshsnapinTracer = PSTraceSource.GetTracer("MshSnapinLoadUnload", "Loading and unloading mshsnapins", false);

        #endregion

        #region Properties

        /// <summary>
        /// Monad Version that the console file depends on.
        /// </summary>
        internal Version PSVersion { get; }

        /// <summary>
        /// Returns the major version of current console.
        /// </summary>
        internal string MajorVersion
        {
            get
            {
                Diagnostics.Assert(PSVersion != null,
                    "PSVersion is null");

                return PSVersion.Major.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// List of mshsnapins that are available. This includes both the monad 
        /// default mshsnapins as well as external mshsnapins as represented by the
        /// console file.
        /// </summary>
        /// <remarks>
        /// The list returned is an ordered-list with default mshsnapins at the
        /// start followed by external mshsnapins in the order represented by the
        /// console file.
        /// </remarks>
        internal Collection<PSSnapInInfo> PSSnapIns
        {
            get
            {
                return MergeDefaultExternalMshSnapins();
            }
        }

        /// <summary>
        /// List of external mshsnapins, as represented by the console file and cmdlets
        /// add-pssnapin,remove-pssnapin, that are available.
        /// </summary>
        internal Collection<PSSnapInInfo> ExternalPSSnapIns
        {
            get
            {
                // externalPSSnapIns is never null
                Diagnostics.Assert(_externalPSSnapIns != null, "externalPSSnapIns is null");

                return _externalPSSnapIns;
            }
        }

        /// <summary>
        /// A boolean which tells whether the console file is modified after it is read
        /// or created.
        /// </summary>
        /// <remarks>
        /// Modification refers to addition/deletion operations of the external mshsnapins.
        /// </remarks>
        internal bool IsDirty { get; private set; }

        /// <summary>
        /// A string representing the console file name of the current MshConsoleInfo object.
        /// If the filename is relative path, an absolute path will be constructed using
        /// Path.GetFullPath()
        /// </summary>
        /// <remarks>
        /// Once a MshConsoleInfo object is constructed, a user may update the object by
        /// adding,removing mshsnapins. These operations directly effect the state of the 
        /// MshConsoleInfo object but not update the console file.
        /// </remarks>
        internal string Filename { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Construct a MshConsoleInfo object for the Monad version specified.
        /// </summary>
        /// <param name="version">Monad Version.</param>
        private MshConsoleInfo(Version version)
        {
            PSVersion = version;
            IsDirty = false;
            Filename = null;

            // Intialize list of mshsnapins..
            _defaultPSSnapIns = new Collection<PSSnapInInfo>();
            _externalPSSnapIns = new Collection<PSSnapInInfo>();
        }

        #endregion

        #region Staic Methods

        /// <summary>
        /// Constructs a <see cref="System.Management.Automation.Runspaces.MshConsoleInfo"/> object for the
        /// current Monad version which is already started.
        /// </summary>
        /// <exception cref="PSSnapInException">
        /// One or more default mshsnapins cannot be loaded because the
        /// registry is not populated correctly.
        /// </exception>
        internal static MshConsoleInfo CreateDefaultConfiguration()
        {
            // Steps:
            // 1. Get the current Monad Version
            // 2. Create MshConsoleInfo object.
            // 3. Read default mshsnapins.

            MshConsoleInfo consoleInfo = new MshConsoleInfo(PSVersionInfo.PSVersion);
            try
            {
                consoleInfo._defaultPSSnapIns = PSSnapInReader.ReadEnginePSSnapIns();
            }
            catch (PSArgumentException ae)
            {
                string message = ConsoleInfoErrorStrings.CannotLoadDefaults;
                // If we were unalbe to load default mshsnapins throw PSSnapInException

                s_mshsnapinTracer.TraceError(message);

                throw new PSSnapInException(message, ae);
            }
            catch (System.Security.SecurityException se)
            {
                string message = ConsoleInfoErrorStrings.CannotLoadDefaults;
                // If we were unalbe to load default mshsnapins throw PSSnapInException

                s_mshsnapinTracer.TraceError(message);

                throw new PSSnapInException(message, se);
            }

            return consoleInfo;
        }

        /// <summary>
        /// Constructs a <see cref="System.Management.Automation.Runspaces.MshConsoleInfo"/> object from a
        /// Monad console file.
        /// </summary>
        /// <param name="fileName">
        /// Monad console file name. If the filename is not absolute path. Then absolute path is
        /// constructed by using Path.GetFullPath() API.
        /// </param>
        /// <param name="cle">
        /// PSConsoleLoadException occurred while loading this console file. This object
        /// also contains specific PSSnapInExceptions that occurred while loading.
        /// </param>
        /// <exception cref="PSSnapInException">
        /// One or more default mshsnapins cannot be loaded because the
        /// registry is not populated correctly.
        /// </exception>
        /// <exception cref="PSArgumentNullException">
        /// fileName is null.
        /// </exception>
        /// <exception cref="PSArgumentException">
        /// 1. fileName does not specify proper file extension.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// fileName contains one or more of the invalid characters defined in System.IO.Path.InvalidPathChars.
        /// </exception>
        /// <exception cref="XmlException">
        /// Unable to load/parse the file specified by fileName.
        /// </exception>
        internal static MshConsoleInfo CreateFromConsoleFile(string fileName, out PSConsoleLoadException cle)
        {
            s_mshsnapinTracer.WriteLine("Creating console info from file {0}", fileName);

            // Construct default mshsnapins                
            MshConsoleInfo consoleInfo = CreateDefaultConfiguration();

            // Check whether the filename specified is an absolute path.
            string absolutePath = Path.GetFullPath(fileName);
            consoleInfo.Filename = absolutePath;

            // Construct externalPSSnapIns by loading file.
            consoleInfo.Load(absolutePath, out cle);

            s_mshsnapinTracer.WriteLine("Console info created successfully");

            return consoleInfo;
        }

        #endregion

        #region Internal Instance Methods       

        /// <summary>
        /// Saves the current <see cref="MshConsoleInfo"/> object to a file specified
        /// by <paramref name="path"/>. IsDirty is set to false once file is saved.
        /// </summary>
        /// <param name="path">
        /// If path is not an absolute path, then an absolute path is constructed by 
        /// using Path.GetFullPath() API.
        /// </param>
        /// <exception cref="PSArgumentException">
        /// 1.Path does not specify proper file extension.
        /// </exception>
        /// <exception cref="PSArgumentNullException">
        /// 1. Path is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// path contains one or more of the invalid characters defined in System.IO.Path.InvalidPathChars.
        /// </exception>
        internal void SaveAsConsoleFile(string path)
        {
            if (null == path)
            {
                throw PSTraceSource.NewArgumentNullException("path");
            }

            // Check whether the filename specified is an absolute path.
            string absolutePath = path;

            if (!Path.IsPathRooted(absolutePath))
            {
                absolutePath = Path.GetFullPath(Filename);
            }

            // Ignore case when looking for file extension.
            if (!absolutePath.EndsWith(StringLiterals.PowerShellConsoleFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                s_mshsnapinTracer.TraceError("Console file {0} doesn't have the right extension {1}.", path, StringLiterals.PowerShellConsoleFileExtension);
                throw PSTraceSource.NewArgumentException("absolutePath", ConsoleInfoErrorStrings.BadConsoleExtension);
            }

            //ConsoleFileElement will write to file
            PSConsoleFileElement.WriteToFile(absolutePath, this.PSVersion, this.ExternalPSSnapIns);
            //update the console file variable
            Filename = absolutePath;
            IsDirty = false;
        }

        /// <summary>
        /// Saves the current <see cref="MshConsoleInfo"/> object to its console file.
        /// IsDirty is set to false once file is saved.
        /// </summary>
        /// <exception cref="PSInvalidOperationException">
        /// Msh is loaded with default mshsnapins. $console is currently empty.
        /// </exception>
        internal void Save()
        {
            if (null == Filename)
            {
                throw PSTraceSource.NewInvalidOperationException(ConsoleInfoErrorStrings.SaveDefaultError);
            }

            PSConsoleFileElement.WriteToFile(Filename, this.PSVersion, this.ExternalPSSnapIns);
            IsDirty = false;
        }

        /// <summary>
        /// Adds a mshsnapin specified by <paramref name="mshSnapInID"/> to the current list of
        /// mshsnapins. If the mshsnapin is successfully added, IsDirty property is set to true.
        /// </summary>
        /// <param name="mshSnapInID">ID of the mshsnapin which needs to be added.</param>
        /// <returns>A <see cref="PSSnapInInfo"/> object corresponding to mshSnapInID.</returns>
        /// <remarks>PSSnapIn information must be present in the registry for this call to succeed.</remarks>
        /// <exception cref="PSArgumentNullException">
        /// mshSnapInID is empty or null.
        /// </exception>
        /// <exception cref="PSArgumentException">
        /// PSSnapIn is already loaded.
        /// No PSSnapIn with given id found.
        /// PSSnapIn cannot be loaded.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        /// Caller doesn't have permission to read keys.
        /// </exception>
        internal PSSnapInInfo AddPSSnapIn(string mshSnapInID)
        {
            if (string.IsNullOrEmpty(mshSnapInID))
            {
                PSTraceSource.NewArgumentNullException("mshSnapInID");
            }

            // Check whether the mshsnapin is already present in defaultmshsnapins/externalMshSnapins
            if (IsDefaultPSSnapIn(mshSnapInID, _defaultPSSnapIns))
            {
                s_mshsnapinTracer.TraceError("MshSnapin {0} can't be added since it is a default mshsnapin", mshSnapInID);

                throw PSTraceSource.NewArgumentException("mshSnapInID", ConsoleInfoErrorStrings.CannotLoadDefault);
            }

            if (IsActiveExternalPSSnapIn(mshSnapInID))
            {
                s_mshsnapinTracer.TraceError("MshSnapin {0} is already loaded.", mshSnapInID);

                throw PSTraceSource.NewArgumentException("mshSnapInID", ConsoleInfoErrorStrings.PSSnapInAlreadyExists, mshSnapInID);
            }

            // Check whether the mshsnapin is present in the registry.
            PSSnapInInfo newPSSnapIn = PSSnapInReader.Read(this.MajorVersion, mshSnapInID);

            if (!Utils.IsPSVersionSupported(newPSSnapIn.PSVersion.ToString()))
            {
                s_mshsnapinTracer.TraceError("MshSnapin {0} and current monad engine's versions don't match.", mshSnapInID);

                throw PSTraceSource.NewArgumentException("mshSnapInID",
                                                         ConsoleInfoErrorStrings.AddPSSnapInBadMonadVersion,
                                                         newPSSnapIn.PSVersion.ToString(),
                                                         PSVersion.ToString());
            }

            // new mshsnapin will never be null
            //if this is a valid new mshsnapin,add this to external mshsnapins
            _externalPSSnapIns.Add(newPSSnapIn);
            s_mshsnapinTracer.WriteLine("MshSnapin {0} successfully added to consoleinfo list.", mshSnapInID);
            //Set IsDirty to true
            IsDirty = true;

            return newPSSnapIn;
        }

        /// <summary>
        /// Removes a mshsnapin specified by <paramref name="mshSnapInID"/> from the current
        /// list.
        /// </summary>
        /// <param name="mshSnapInID">ID of the mshsnapin which needs to be removed</param>
        /// <returns>PSSnapInInfo object for the mshsnapin that is removed.</returns>
        /// <remarks>MshSnapin is removed only from the console file. Registry entry
        /// is not touched.</remarks>
        /// <exception cref="PSArgumentNullException">
        /// mshSnapInID is null.
        /// </exception>
        /// <exception cref="PSArgumentException">
        /// 1. mshSnapInID is either a default mshsnapin or not loaded.
        /// 2. mshSnapInId is not valid.
        /// </exception>
        internal PSSnapInInfo RemovePSSnapIn(string mshSnapInID)
        {
            if (string.IsNullOrEmpty(mshSnapInID))
            {
                PSTraceSource.NewArgumentNullException("mshSnapInID");
            }

            // Monad has specific restrictions on the mshsnapinid like
            // mshsnapinid should be A-Za-z0-9.-_ etc.
            PSSnapInInfo.VerifyPSSnapInFormatThrowIfError(mshSnapInID);

            PSSnapInInfo removedPSSnapIn = null;
            // Check external mshsnapins
            foreach (PSSnapInInfo mshSnapIn in _externalPSSnapIns)
            {
                if (string.Equals(mshSnapInID, mshSnapIn.Name, System.StringComparison.OrdinalIgnoreCase))
                {
                    // We found the mshsnapin..remove from the list and break
                    removedPSSnapIn = mshSnapIn;
                    _externalPSSnapIns.Remove(mshSnapIn);

                    // The state of console file is changing..so set
                    // dirty flag.
                    IsDirty = true;
                    break;
                }
            }

            if (removedPSSnapIn == null)
            {
                if (IsDefaultPSSnapIn(mshSnapInID, _defaultPSSnapIns))
                {
                    s_mshsnapinTracer.WriteLine("MshSnapin {0} can't be removed since it is a default mshsnapin.", mshSnapInID);

                    throw PSTraceSource.NewArgumentException("mshSnapInID", ConsoleInfoErrorStrings.CannotRemoveDefault, mshSnapInID);
                }

                throw PSTraceSource.NewArgumentException("mshSnapInID", ConsoleInfoErrorStrings.CannotRemovePSSnapIn, mshSnapInID);
            }

            return removedPSSnapIn;
        }

        /// <summary>
        /// Searches for mshsnapin in either current console or registry as determined
        /// by <paramref name="searchRegistry"/>.
        /// </summary>
        /// <param name="pattern">
        /// Id/WildcardPattern of the mshsnapin to search for. This can contain wildcard characters as
        /// represented by WildCardPattern.
        /// </param>
        /// <param name="searchRegistry">
        /// A boolean which determines whether to search in the current console or registry.
        /// </param>
        /// <returns>A collection of mshsnapininfo objects.</returns>
        /// <exception cref="PSArgumentException">
        /// 1.Unable to read registry entries for mshsnapins.
        /// 2.Pattern specified is not valid. If pattern doesnt contain
        /// wildcard characters, this function checks for the validity
        /// of the mshsnapin name.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        /// Caller doesn't have permission to read keys.
        /// </exception>
        internal Collection<PSSnapInInfo> GetPSSnapIn(string pattern, bool searchRegistry)
        {
            // We want to improve the search speed by noting whether we want
            // to perform wildcard search.
            bool doWildCardSearch = WildcardPattern.ContainsWildcardCharacters(pattern);

            if (!doWildCardSearch)
            {
                // Verify PSSnapInID..
                // This will throw if it not a valid name
                PSSnapInInfo.VerifyPSSnapInFormatThrowIfError(pattern);
            }

            // Build the list to search..If searchRegistry is true get all mshsnapins available
            // from the registry, otherwise get mshsnapins from the current console.
            Collection<PSSnapInInfo> listToSearch = searchRegistry ?
                PSSnapInReader.ReadAll() : PSSnapIns;

            // Create a list to return..
            Collection<PSSnapInInfo> listToReturn = new Collection<PSSnapInInfo>();

            // If there is nothing to search..
            if (listToSearch == null)
                return listToReturn;

            if (!doWildCardSearch)
            {
                // We are not doing wildcard search..
                foreach (PSSnapInInfo mshSnapIn in listToSearch)
                {
                    if (string.Equals(mshSnapIn.Name, pattern, StringComparison.OrdinalIgnoreCase))
                    {
                        listToReturn.Add(mshSnapIn);
                    }
                }
            }
            else
            {
                WildcardPattern matcher = WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase);
                // We are doing WildCard search
                foreach (PSSnapInInfo mshSnapIn in listToSearch)
                {
                    if (matcher.IsMatch(mshSnapIn.Name))
                    {
                        listToReturn.Add(mshSnapIn);
                    }
                }
            }

            // return whatever we found..may be 0..
            return listToReturn;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Loads a Monad Console file specified by <paramref name="path"/>
        /// </summary>
        /// <param name="path">
        /// The absolute path from which the content is loaded.
        /// </param>
        /// <param name="cle">
        /// PSConsoleLoadException occurred while loading this console file. This object
        /// also contains specific PSSnapInExceptions that occurred while loading.
        /// </param>
        /// <returns>
        /// A list of <see cref="PSSnapInInfo"/> objects specified in the console file.
        /// </returns>
        /// <exception cref="PSArgumentNullException">
        /// Path is null.
        /// </exception>
        /// <exception cref="PSArgumentException">
        /// 1. Path does not specify proper file extension.
        /// 2. PSSnapInId doesnt contain valid characters.
        /// 3. Path is not an Absolute Path. 
        ///    Example of valid paths:"\\MyDir\\MyFile.txt" and "C:\\MyDir".
        /// </exception>
        /// <exception cref="ArgumentException">
        /// path contains one or more of the invalid characters defined in System.IO.Path.InvalidPathChars.
        /// </exception>
        /// <exception cref="XmlException">
        /// Unable to load/parse the file specified by path.
        /// </exception>
        private Collection<PSSnapInInfo> Load(string path, out PSConsoleLoadException cle)
        {
            // Intialize the out parameter..
            cle = null;

            s_mshsnapinTracer.WriteLine("Load mshsnapins from console file {0}", path);

            if (string.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentNullException("path");
            }

            // Check whether the path is an absolute path
            if (!Path.IsPathRooted(path))
            {
                s_mshsnapinTracer.TraceError("Console file {0} needs to be a absolute path.", path);

                throw PSTraceSource.NewArgumentException("path", ConsoleInfoErrorStrings.PathNotAbsolute, path);
            }

            if (!path.EndsWith(StringLiterals.PowerShellConsoleFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                s_mshsnapinTracer.TraceError("Console file {0} needs to have {1} extension.", path, StringLiterals.PowerShellConsoleFileExtension);

                throw PSTraceSource.NewArgumentException("path", ConsoleInfoErrorStrings.BadConsoleExtension);
            }

            PSConsoleFileElement consoleFileElement;

            // exceptions are thrown to the caller
            consoleFileElement = PSConsoleFileElement.CreateFromFile(path);

            // consoleFileElement will never be null..
            if (!Utils.IsPSVersionSupported(consoleFileElement.MonadVersion))
            {
                s_mshsnapinTracer.TraceError("Console version {0} is not supported in current monad session.", consoleFileElement.MonadVersion);

                throw PSTraceSource.NewArgumentException("PSVersion", ConsoleInfoErrorStrings.BadMonadVersion, consoleFileElement.MonadVersion,
                    PSVersion.ToString());
            }

            // Create a store for exceptions
            Collection<PSSnapInException> exceptions = new Collection<PSSnapInException>();

            foreach (string mshsnapin in consoleFileElement.PSSnapIns)
            {
                try
                {
                    this.AddPSSnapIn(mshsnapin);
                }
                catch (PSArgumentException ae)
                {
                    PSSnapInException sle = new PSSnapInException(mshsnapin, ae.Message, ae);

                    // Eat ArgumentException and continue..
                    exceptions.Add(sle);
                }
                catch (System.Security.SecurityException se)
                {
                    string message = ConsoleInfoErrorStrings.PSSnapInReadError;
                    PSSnapInException sle = new PSSnapInException(mshsnapin, message, se);
                    // Eat SecurityException and continue..

                    exceptions.Add(sle);
                }
            }

            // Before returning check whether there are any exceptions
            if (exceptions.Count > 0)
            {
                cle = new PSConsoleLoadException(this, exceptions);
            }

            // We are able to load console file and currently monad engine
            // can service this. So mark the isdirty flag.
            IsDirty = false;

            return _externalPSSnapIns;
        }

        /// <summary>
        /// Checks whether the mshsnapin is a default mshsnapin
        /// </summary>
        /// <param name="mshSnapInID">Id of the mshsnapin</param>
        /// <param name="defaultSnapins">List of default mshsnapins</param>
        /// <returns>True if PSSnapIn is default.False otherwise.</returns>
        internal static bool IsDefaultPSSnapIn(string mshSnapInID, IEnumerable<PSSnapInInfo> defaultSnapins)
        {
            // Check whether the mshsnapin is present in defaultmshsnapins.
            foreach (PSSnapInInfo mshSnapIn in defaultSnapins)
            {
                if (string.Equals(mshSnapInID, mshSnapIn.Name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks whether the mshsnapin is already loaded
        /// </summary>
        /// <param name="mshSnapInID">Id of the mshsnapin</param>
        /// <returns>True if PSSnapIn is loaded.False otherwise.</returns>
        private bool IsActiveExternalPSSnapIn(string mshSnapInID)
        {
            // Check whether the mshsnapin is present in externalmshsnapins.
            foreach (PSSnapInInfo mshSnapIn in _externalPSSnapIns)
            {
                if (string.Equals(mshSnapInID, mshSnapIn.Name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Constructs a new list of mshsnapins from defualt mshsnapins and external mshsnapins.
        /// </summary>
        /// <returns>A list of mshsnapins represented by the current console file</returns>
        private Collection<PSSnapInInfo> MergeDefaultExternalMshSnapins()
        {
            //Default mshsnapins should never be null
            Diagnostics.Assert(_defaultPSSnapIns != null, "Default MshSnapins for the current console is empty");
            Collection<PSSnapInInfo> mshSnapIns = new Collection<PSSnapInInfo>();

            foreach (PSSnapInInfo mshSnapIn in _defaultPSSnapIns)
            {
                mshSnapIns.Add(mshSnapIn);
            }

            foreach (PSSnapInInfo mshSnapIn in _externalPSSnapIns)
            {
                mshSnapIns.Add(mshSnapIn);
            }

            return mshSnapIns;
        }
        #endregion
    }
}
