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
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands.Omi
{
    internal static class Platform
    {
        internal static bool IsLinux()
        {
#if CORECLR
            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
#else
            // FullCLR doesn't have a Linux version
            return false;
#endif
        }

        internal static bool IsWindows()
        {
#if CORECLR
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#else
            // FullCLR has only Windows version
            return true;
#endif
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

        public void ExecuteOmiCliCommand(string arguments)
        {
            using (Process process = new Process())
            {
                // Assume omicli is somewhere in PATH...
                process.StartInfo.FileName = "omicli";
                process.StartInfo.Arguments = arguments;
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

        public void GetValue(string className, string propertyName, out string type, out string value)
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

        public OmiData GetOmiData()
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

                    if (valueArrays.Count() > 0)
                    {
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
                    else
                    {
                        data.Values.Add(dCommon);
                    }
                }
            }

            return data;
        }
    }
}
