// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Management;
using System.Management.Automation;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Services;
using System.Web.Services.Description;
using System.Web.Services.Discovery;
using System.Xml;

using Microsoft.CSharp;
using Microsoft.Win32;

using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    #region New-WebServiceProxy

    /// <summary>
    /// Cmdlet for new-WebService Proxy.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "WebServiceProxy", DefaultParameterSetName = "NoCredentials", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135238")]
    public sealed class NewWebServiceProxy : PSCmdlet
    {
        #region Parameters

        /// <summary>
        /// URI of the web service.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        [Alias("WL", "WSDL", "Path")]
        public System.Uri Uri
        {
            get { return _uri; }

            set
            {
                _uri = value;
            }
        }

        private System.Uri _uri;

        /// <summary>
        /// Parameter Class name.
        /// </summary>
        [Parameter(Position = 1)]
        [ValidateNotNullOrEmpty]
        [Alias("FileName", "FN")]
        public string Class
        {
            get { return _class; }

            set
            {
                _class = value;
            }
        }

        private string _class;

        /// <summary>
        /// Namespace.
        /// </summary>
        [Parameter(Position = 2)]
        [ValidateNotNullOrEmpty]
        [Alias("NS")]
        public string Namespace
        {
            get { return _namespace; }

            set
            {
                _namespace = value;
            }
        }

        private string _namespace;

        /// <summary>
        /// Credential.
        /// </summary>
        [Parameter(ParameterSetName = "Credential")]
        [ValidateNotNullOrEmpty]
        [Credential]
        [Alias("Cred")]
        public PSCredential Credential
        {
            get { return _credential; }

            set
            {
                _credential = value;
            }
        }

        private PSCredential _credential;

        /// <summary>
        /// Use default credential..
        /// </summary>
        [Parameter(ParameterSetName = "UseDefaultCredential")]
        [ValidateNotNull]
        [Alias("UDC")]
        public SwitchParameter UseDefaultCredential
        {
            get { return _usedefaultcredential; }

            set
            {
                _usedefaultcredential = value;
            }
        }

        private SwitchParameter _usedefaultcredential;

        #endregion

        #region overrides
        /// <summary>
        /// Cache for storing URIs.
        /// </summary>
        private static Dictionary<Uri, string> s_uriCache = new Dictionary<Uri, string>();

        /// <summary>
        /// Cache for storing sourcecodehashes.
        /// </summary>
        private static Dictionary<int, object> s_srccodeCache = new Dictionary<int, object>();

        /// <summary>
        /// Holds the hash code of the source generated.
        /// </summary>
        private int _sourceHash;
        /// <summary>
        /// Random class.
        /// </summary>

        private object _cachelock = new object();
        private static Random s_rnd = new Random();
        /// <summary>
        /// BeginProcessing code.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (string.IsNullOrWhiteSpace(_uri.ToString()))
            {
                Exception ex = new ArgumentException(WebServiceResources.InvalidUri);
                ErrorRecord er = new ErrorRecord(ex, "ArgumentException", ErrorCategory.InvalidOperation, null);
                ThrowTerminatingError(er);
            }
            // check if system.web is available.This assembly is not available in win server core.
            string AssemblyString = "System.Web, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            try
            {
                Assembly webAssembly = Assembly.Load(AssemblyString);
            }
            catch (FileNotFoundException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "SystemWebAssemblyNotFound", ErrorCategory.ObjectNotFound, null);
                er.ErrorDetails = new ErrorDetails(WebServiceResources.NotSupported);
                ThrowTerminatingError(er);
            }

            int sourceCache = 0;

            lock (s_uriCache)
            {
                if (s_uriCache.ContainsKey(_uri))
                {
                    // if uri is present in the cache
                    string ns;
                    s_uriCache.TryGetValue(_uri, out ns);
                    string[] data = ns.Split('|');
                    if (string.IsNullOrEmpty(_namespace))
                    {
                        if (data[0].StartsWith("Microsoft.PowerShell.Commands.NewWebserviceProxy.AutogeneratedTypes.", StringComparison.OrdinalIgnoreCase))
                        {
                            _namespace = data[0];
                            _class = data[1];
                        }
                    }

                    sourceCache = Int32.Parse(data[2].ToString(), CultureInfo.InvariantCulture);
                }
            }

            if (string.IsNullOrEmpty(_namespace))
            {
                _namespace = "Microsoft.PowerShell.Commands.NewWebserviceProxy.AutogeneratedTypes.WebServiceProxy" + GenerateRandomName();
            }
            // if class is null,generate a name for it
            if (string.IsNullOrEmpty(_class))
            {
                _class = "MyClass" + GenerateRandomName();
            }

            Assembly webserviceproxy = GenerateWebServiceProxyAssembly(_namespace, _class);
            if (webserviceproxy == null)
                return;
            object instance = InstantiateWebServiceProxy(webserviceproxy);

            // to set the credentials into the generated webproxy Object
            PropertyInfo[] pinfo = instance.GetType().GetProperties();
            foreach (PropertyInfo pr in pinfo)
            {
                if (pr.Name.Equals("UseDefaultCredentials", StringComparison.OrdinalIgnoreCase))
                {
                    if (UseDefaultCredential.IsSpecified)
                    {
                        bool flag = true;
                        pr.SetValue(instance, flag as object, null);
                    }
                }

                if (pr.Name.Equals("Credentials", StringComparison.OrdinalIgnoreCase))
                {
                    if (Credential != null)
                    {
                        NetworkCredential cred = Credential.GetNetworkCredential();
                        pr.SetValue(instance, cred as object, null);
                    }
                }
            }

            // disposing the entries in a cache
            // Adding to Cache
            lock (s_uriCache)
            {
                s_uriCache.Remove(_uri);
            }

            if (sourceCache > 0)
            {
                lock (_cachelock)
                {
                    s_srccodeCache.Remove(sourceCache);
                }
            }

            string key = string.Join("|", new string[] { _namespace, _class, _sourceHash.ToString(System.Globalization.CultureInfo.InvariantCulture) });
            lock (s_uriCache)
            {
                s_uriCache.Add(_uri, key);
            }

            lock (_cachelock)
            {
                s_srccodeCache.Add(_sourceHash, instance);
            }

            WriteObject(instance, true);
        }

        #endregion

        #region private

        private static ulong s_sequenceNumber = 1;
        private static object s_sequenceNumberLock = new object();

        /// <summary>
        /// Generates a random name.
        /// </summary>
        /// <returns>String.</returns>
        private string GenerateRandomName()
        {
            string rndname = null;
            string givenuri = _uri.ToString();
            for (int i = 0; i < givenuri.Length; i++)
            {
                Int32 val = System.Convert.ToInt32(givenuri[i], CultureInfo.InvariantCulture);
                if ((val >= 65 && val <= 90) || (val >= 48 && val <= 57) || (val >= 97 && val <= 122))
                {
                    rndname += givenuri[i];
                }
                else
                {
                    rndname += "_";
                }
            }

            string sequenceString;
            lock (s_sequenceNumberLock)
            {
                sequenceString = (s_sequenceNumber++).ToString(CultureInfo.InvariantCulture);
            }

            if (rndname.Length > 30)
            {
                return (sequenceString + rndname.Substring(rndname.Length - 30));
            }

            return (sequenceString + rndname);
        }

        /// <summary>
        /// Generates the Assembly.
        /// </summary>
        /// <param name="NameSpace"></param>
        /// <param name="ClassName"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        private Assembly GenerateWebServiceProxyAssembly(string NameSpace, string ClassName)
        {
            DiscoveryClientProtocol dcp = new DiscoveryClientProtocol();

            // if paramset is defaultcredential, set the flag in wcclient
            if (_usedefaultcredential.IsSpecified)
                dcp.UseDefaultCredentials = true;

            // if paramset is credential, assign the credentials
            if (ParameterSetName.Equals("Credential", StringComparison.OrdinalIgnoreCase))
                dcp.Credentials = _credential.GetNetworkCredential();

            try
            {
                dcp.AllowAutoRedirect = true;
                dcp.DiscoverAny(_uri.ToString());
                dcp.ResolveAll();
            }
            catch (WebException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "WebException", ErrorCategory.ObjectNotFound, _uri);
                if (ex.InnerException != null)
                    er.ErrorDetails = new ErrorDetails(ex.InnerException.Message);
                WriteError(er);
                return null;
            }
            catch (InvalidOperationException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "InvalidOperationException", ErrorCategory.InvalidOperation, _uri);
                WriteError(er);
                return null;
            }

            // create the namespace
            CodeNamespace codeNS = new CodeNamespace();
            if (!string.IsNullOrEmpty(NameSpace))
                codeNS.Name = NameSpace;

            // create the class and add it to the namespace
            if (!string.IsNullOrEmpty(ClassName))
            {
                CodeTypeDeclaration codeClass = new CodeTypeDeclaration(ClassName);
                codeClass.IsClass = true;
                codeClass.Attributes = MemberAttributes.Public;
                codeNS.Types.Add(codeClass);
            }

            // create a web reference to the uri docs
            WebReference wref = new WebReference(dcp.Documents, codeNS);
            WebReferenceCollection wrefs = new WebReferenceCollection();
            wrefs.Add(wref);

            // create a codecompileunit and add the namespace to it
            CodeCompileUnit codecompileunit = new CodeCompileUnit();
            codecompileunit.Namespaces.Add(codeNS);

            WebReferenceOptions wrefOptions = new WebReferenceOptions();
            wrefOptions.CodeGenerationOptions = System.Xml.Serialization.CodeGenerationOptions.GenerateNewAsync | System.Xml.Serialization.CodeGenerationOptions.GenerateOldAsync | System.Xml.Serialization.CodeGenerationOptions.GenerateProperties;
            wrefOptions.Verbose = true;

            // create a csharpprovider and compile it
            CSharpCodeProvider csharpprovider = new CSharpCodeProvider();
            StringCollection Warnings = ServiceDescriptionImporter.GenerateWebReferences(wrefs, csharpprovider, codecompileunit, wrefOptions);

            StringBuilder codegenerator = new StringBuilder();
            StringWriter writer = new StringWriter(codegenerator, CultureInfo.InvariantCulture);
            try
            {
                csharpprovider.GenerateCodeFromCompileUnit(codecompileunit, writer, null);
            }
            catch (NotImplementedException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "NotImplementedException", ErrorCategory.ObjectNotFound, _uri);
                WriteError(er);
            }
            // generate the hashcode of the CodeCompileUnit
            _sourceHash = codegenerator.ToString().GetHashCode();

            // if the sourcehash matches the hashcode in the cache,the proxy hasnt changed and so
            // return the instance of th eproxy in the cache
            if (s_srccodeCache.ContainsKey(_sourceHash))
            {
                object obj;
                s_srccodeCache.TryGetValue(_sourceHash, out obj);
                WriteObject(obj, true);
                return null;
            }

            CompilerParameters options = new CompilerParameters();
            CompilerResults results = null;

            foreach (string warning in Warnings)
            {
                this.WriteWarning(warning);
            }

            // add the references to the required assemblies
            options.ReferencedAssemblies.Add("System.dll");
            options.ReferencedAssemblies.Add("System.Data.dll");
            options.ReferencedAssemblies.Add("System.Xml.dll");
            options.ReferencedAssemblies.Add("System.Web.Services.dll");
            options.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);
            GetReferencedAssemblies(typeof(Cmdlet).Assembly, options);
            options.GenerateInMemory = true;
            options.TreatWarningsAsErrors = false;
            options.WarningLevel = 4;
            options.GenerateExecutable = false;
            try
            {
                results = csharpprovider.CompileAssemblyFromSource(options, codegenerator.ToString());
            }
            catch (NotImplementedException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "NotImplementedException", ErrorCategory.ObjectNotFound, _uri);
                WriteError(er);
            }

            return results.CompiledAssembly;
        }

        /// <summary>
        /// Function to add all the assemblies required to generate the web proxy.
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="parameters"></param>
        private void GetReferencedAssemblies(Assembly assembly, CompilerParameters parameters)
        {
            if (!parameters.ReferencedAssemblies.Contains(assembly.Location))
            {
                string location = Path.GetFileName(assembly.Location);
                if (!parameters.ReferencedAssemblies.Contains(location))
                {
                    parameters.ReferencedAssemblies.Add(assembly.Location);
                    foreach (AssemblyName referencedAssembly in assembly.GetReferencedAssemblies())
                        GetReferencedAssemblies(Assembly.Load(referencedAssembly.FullName), parameters);
                }
            }
        }
        /// <summary>
        /// Instantiates the object
        ///  if a type of WebServiceBindingAttribute is not found, throw an exception.
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        private object InstantiateWebServiceProxy(Assembly assembly)
        {
            Type proxyType = null;
            // loop through the types of the assembly and identify the type having
            // a web service binding attribute
            foreach (Type type in assembly.GetTypes())
            {
                object[] obj = type.GetCustomAttributes(typeof(WebServiceBindingAttribute), false);
                if (obj.Length > 0)
                {
                    proxyType = type;
                    break;
                }

                if (proxyType != null)
                {
                    break;
                }
            }

            System.Management.Automation.Diagnostics.Assert(
                proxyType != null,
                "Proxy class should always get generated unless there were some errors earlier (in that case we shouldn't get here)");

            return assembly.CreateInstance(proxyType.ToString());
        }

        #endregion
    }
    #endregion
}
