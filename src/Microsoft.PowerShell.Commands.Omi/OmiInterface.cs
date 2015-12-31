/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands.Omi
{
    internal static class Platform
    {
        internal static bool IsLinux()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        }

        internal static bool IsWindows()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }
    }

    /// <summary>
    /// Data from OMI
    /// </summary>    
    public class OmiData
    {
        public HashSet<string> Properties;
        public HashSet<Dictionary<string, string>> Values;
        public OmiData()
        {
            Properties = new HashSet<string>();
            Values = new HashSet<Dictionary<string, string>>();
        }
        public void Debug()
        {
            foreach (string p in Properties)
            {
                Console.Write("{0,-22}", p);
            }
            Console.WriteLine();

            foreach (Dictionary<string, string> d in Values)
            {
                foreach (string p in Properties)
                {
                    string value = String.Empty;
                    if (d.ContainsKey(p))
                    {
                        value = Truncate(d[p], 16);
                    }
                    Console.Write("{0,-22}", value);
                }
                Console.WriteLine();
            }
        }

        public Object[] ToObjectArray()
        {
            // Convert to array of objects
            ArrayList array = new ArrayList();
            foreach (Dictionary<string, string> d in Values)
            {
                PSObject o = new PSObject();

                foreach (string p in Properties)
                {
                    string value = String.Empty;
                    if (d.ContainsKey(p))
                    {
                        value = d[p];
                    }
                    PSNoteProperty psp = new PSNoteProperty(p, value);
                    o.Members.Add(psp);
                }
                array.Add(o);
            }

            return (Object[])(array.ToArray());
        }

        private string Truncate(string s, int maxChars)
        {
            return s.Length < maxChars ? s : s.Substring(0, maxChars) + " ...";
        }
    }

    /// <summary>
    /// Interfaces that cmdlets can use to interface with OMI
    /// </summary>
    public class OmiInterface
    {
        private string _xmlString = null;

        private void GetXML(string nameSpace, string className)
        {
            using (Process process = new Process())
            {
                // Assume omicli is somewhere in PATH...
                process.StartInfo.FileName = "omicli";
                process.StartInfo.Arguments = $"ei {nameSpace} {className} -xml";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();

                string output = process.StandardOutput.ReadToEnd();

                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new IOException();
                }

                _xmlString = $"<INSTANCES>{output}</INSTANCES>";
            }
            return;
        }

        private void GetValue(string className, string propertyName, out string type, out string value)
        {
            // parse xml
            XElement cim = XElement.Parse(_xmlString);
                    
            IEnumerable<XElement> elements = 
                from el in cim.Elements("INSTANCE")
                where (string)el.Attribute("CLASSNAME") == className
                select el;
            
            IEnumerable<XElement> properties = 
                from el in elements.First().Elements("PROPERTY")
                where (string)el.Attribute("NAME") == propertyName
                select el;

            XElement property = properties.First();
            XElement p = property.Element("VALUE");

            type = (string)property.Attribute("TYPE");
            value = p.Value;
        }

        private IEnumerable<XElement> GetValueIEnumerable()
        {
            // parse xml
            XElement cim = XElement.Parse(_xmlString);
                    
            IEnumerable<XElement> elements = cim.Elements();
            return elements;
        }

        private OmiData GetOmiData()
        {
            OmiData data = new OmiData();

            const string VALUE = "VALUE";
            const string VALUEARRAY = "VALUE.ARRAY";
            const string PROPERTY = "PROPERTY";
            const string PROPERTYARRAY = "PROPERTY.ARRAY";

            IEnumerable<XElement> instances = GetValueIEnumerable();
            foreach (XElement instance in instances)
            {
                // First, do PROPERTY elements
                IEnumerable<XElement> properties = instance.Elements(PROPERTY);

                foreach (XElement property in properties)
                {
                    Dictionary<string, string> d = new Dictionary<string, string>();
                    IEnumerable<XAttribute> attrs = property.Attributes();
                
                    foreach (XAttribute attr in attrs)
                    {
                        data.Properties.Add(attr.Name.LocalName);
                        d[attr.Name.LocalName] = attr.Value;
                    }
                
                    // Now look for "VALUE" sub-element
                    IEnumerable<XElement> values = property.Elements(VALUE);
                    foreach (XElement value in values)
                    {
                        data.Properties.Add(VALUE);
                        d[VALUE] = value.Value;
                    }
                
                    data.Values.Add(d);
                }

                // Next, do PROPERTY.ARRAY elements
                IEnumerable<XElement> propertyArrays = instance.Elements(PROPERTYARRAY);
                foreach (XElement property in propertyArrays)
                {
                    Dictionary<string, string> dCommon = new Dictionary<string, string>();
                    IEnumerable<XAttribute> attrs = property.Attributes();
                
                    foreach (XAttribute attr in attrs)
                    {
                        data.Properties.Add(attr.Name.LocalName);
                        dCommon[attr.Name.LocalName] = attr.Value;
                    }
                
                    IEnumerable<XElement> valueArrays = property.Elements(VALUEARRAY);
                    foreach (XElement valueArray in valueArrays)
                    {
                        IEnumerable<XElement> values = valueArray.Elements(VALUE);
                        foreach (XElement value in values)
                        {
                            Dictionary<string, string> d = new Dictionary<string, string>(dCommon);
                            data.Properties.Add(VALUE);
                            d[VALUE] = value.Value;
                            data.Values.Add(d);
                        }
                    }                
                }
            }

            return data;
        }

        /// <summary>
        /// Query OMI and return a string value
        /// </summary>
        /// <param name="namespace">The OMI namespace to query
        /// <param name="class">The OMI class to query
        /// <param name="property">The OMI property to query
        /// <param name="value">The return value
        /// </param>
        /// <exception>
        /// <cref type="IOException">Thrown if a unable to communicate with OMI
        /// <cref type="XmlException">Thrown if a unable to parse XML properly
	/// <cref type="ArgumentNullException>Thrown if any argument is null
	/// <cref type="ArgumentException>Thrown if any return value is of wrong type
        /// </exception>
        public void GetOmiValue(string nameSpace, string className, string property, out string value)
        {
            if (nameSpace == null || className == null || property == null)
            {
                throw new ArgumentNullException();
            }

            if (!Platform.IsLinux())
            {
                throw new PlatformNotSupportedException();
            }

            GetXML(nameSpace, className);

            string type;
            string stringValue;

            GetValue(className, property, out type, out stringValue);

            if (type != "string")
            {
                throw new ArgumentException();
            }

            value = stringValue;
        }

        /// <summary>
        /// Query OMI and return a UInt32 value
        /// </summary>
        /// <param name="namespace">The OMI namespace to query
        /// <param name="class">The OMI class to query
        /// <param name="property">The OMI property to query
        /// <param name="value">The return value
        /// </param>
        /// <exception>
        /// <cref type="IOException">Thrown if a unable to communicate with OMI
        /// <cref type="XmlException">Thrown if a unable to parse XML properly
	/// <cref type="ArgumentNullException>Thrown if any argument is null
	/// <cref type="ArgumentException>Thrown if any return value is of wrong type
        /// </exception>
        public void GetOmiValue(string nameSpace, string className, string property, out UInt32 value)
        {
            if (nameSpace == null || className == null || property == null)
            {
                throw new ArgumentNullException();
            }

            if (!Platform.IsLinux())
            {
                throw new PlatformNotSupportedException();
            }

            GetXML(nameSpace, className);

            string type;
            string stringValue;

            GetValue(className, property, out type, out stringValue);

            if (type != "uint32")
            {
                throw new ArgumentException();
            }

            value = UInt32.Parse(stringValue);
        }

        /// <summary>
        /// Query OMI and return a UInt64 value
        /// </summary>
        /// <param name="namespace">The OMI namespace to query
        /// <param name="class">The OMI class to query
        /// <param name="property">The OMI property to query
        /// <param name="value">The return value
        /// </param>
        /// <exception>
        /// <cref type="IOException">Thrown if a unable to communicate with OMI
        /// <cref type="XmlException">Thrown if a unable to parse XML properly
	/// <cref type="ArgumentNullException>Thrown if any argument is null
	/// <cref type="ArgumentException>Thrown if any return value is of wrong type
        /// </exception>
        public void GetOmiValue(string nameSpace, string className, string property, out UInt64 value)
        {
            if (nameSpace == null || className == null || property == null)
            {
                throw new ArgumentNullException();
            }

            if (!Platform.IsLinux())
            {
                throw new PlatformNotSupportedException();
            }

            GetXML(nameSpace, className);

            string type;
            string stringValue;

            GetValue(className, property, out type, out stringValue);

            if (type != "uint64")
            {
                throw new ArgumentException();
            }

            value = UInt64.Parse(stringValue);
        }

        /// <summary>
        /// Query OMI and return a collection of XElements
        /// </summary>
        /// <param name="namespace">The OMI namespace to query
        /// <param name="class">The OMI class to query
        /// <param name="elements">The return values
        /// </param>
        /// <exception>
        /// <cref type="IOException">Thrown if a unable to communicate with OMI
        /// <cref type="XmlException">Thrown if a unable to parse XML properly
	/// <cref type="ArgumentNullException>Thrown if any argument is null
	/// <cref type="ArgumentException>Thrown if any return value is of wrong type
        /// </exception>
        public void GetOmiValues(string nameSpace, string className, out IEnumerable<XElement> elements)
        {
            if (nameSpace == null || className == null)
            {
                throw new ArgumentNullException();
            }

            if (!Platform.IsLinux())
            {
                throw new PlatformNotSupportedException();
            }

            GetXML(nameSpace, className);

            elements = GetValueIEnumerable();
        }

        /// <summary>
        /// Query OMI and return an OmiData data class
        /// </summary>
        /// <param name="namespace">The OMI namespace to query
        /// <param name="class">The OMI class to query
        /// <param name="data">The return values
        /// </param>
        /// <exception>
        /// <cref type="IOException">Thrown if a unable to communicate with OMI
        /// <cref type="XmlException">Thrown if a unable to parse XML properly
	/// <cref type="ArgumentNullException>Thrown if any argument is null
	/// <cref type="ArgumentException>Thrown if any return value is of wrong type
        /// </exception>
        public void GetOmiValues(string nameSpace, string className, out OmiData data)
        {
            if (nameSpace == null || className == null)
            {
                throw new ArgumentNullException();
            }

            if (!Platform.IsLinux())
            {
                throw new PlatformNotSupportedException();
            }

            GetXML(nameSpace, className);

            data = GetOmiData();
        }

        /// <summary>
        /// Query OMI and return output as XElement
        /// </summary>
        /// <param name="namespace">The OMI namespace to query
        /// <param name="class">The OMI class to query
        /// <param name="cim">The return xml as a XElement
        /// </param>
        /// <exception>
        /// <cref type="IOException">Thrown if a unable to communicate with OMI
        /// <cref type="XmlException">Thrown if a unable to parse XML properly
	/// <cref type="ArgumentNullException>Thrown if any argument is null
	/// <cref type="ArgumentException>Thrown if any return value is of wrong type
        /// </exception>
        public void GetOmiValues(string nameSpace, string className, out XElement cim)
        {
            if (nameSpace == null || className == null)
            {
                throw new ArgumentNullException();
            }

            if (!Platform.IsLinux())
            {
                throw new PlatformNotSupportedException();
            }

            GetXML(nameSpace, className);
            cim = XElement.Parse(_xmlString);
        }

        /// <summary>
        /// Send DSC configuration to OMI based on a mof file
        /// </summary>
        /// <param name="mofPath">The full path to mof file
        /// </param>
        /// <exception>
        /// <cref type="IOException">Thrown if a unable to communicate with OMI
	/// <cref type="ArgumentNullException>Thrown if any argument is null
        /// </exception>
        public void StartDscConfiguration(string mofPath, out OmiData data)
        {
            if (mofPath == null)
            {
                throw new ArgumentNullException();
            }

            if (!Platform.IsLinux())
            {
                throw new PlatformNotSupportedException();
            }

            string mof = File.ReadAllText(mofPath);
            byte[] asciiBytes = Encoding.ASCII.GetBytes(mof);

            StringBuilder sb = new StringBuilder();
            const string className = "SendConfigurationApply";

            sb.Append("iv root/Microsoft/DesiredStateConfiguration { MSFT_DSCLocalConfigurationManager } ");
            sb.Append(className);
            sb.Append(" { ConfigurationData [ ");
            foreach (byte b in asciiBytes)
            {
                sb.Append(b.ToString());
                sb.Append(' ');
            }
            sb.Append(" ] ");
            sb.Append("} -xml");

            using (Process process = new Process())
            {
                // Assume omicli is somewhere in PATH...
                process.StartInfo.FileName = "omicli";
                process.StartInfo.Arguments = sb.ToString();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();

                string output = process.StandardOutput.ReadToEnd();

                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new IOException();
                }
                
                _xmlString = $"<INSTANCES>{output}</INSTANCES>";
                data = GetOmiData();

                return;
            }
        }
    }
}
