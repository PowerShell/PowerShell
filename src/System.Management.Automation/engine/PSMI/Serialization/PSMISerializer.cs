using System.IO;
using System.Linq;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Tracing;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Xml;
using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Serialization;
using Microsoft.PowerShell;
using Dbg = System.Management.Automation.Diagnostics;
using System.Management.Automation.Remoting;
using System.Collections.Generic;

namespace System.Management.Automation
{
    /// <summary>
    /// This class provides public functionality for serializing a PSObject to a CimInstance
    /// </summary>
    internal class PSMISerializer
    {

        //TODO, insivara : Depth implementation will be added subsequently
        /// <summary>
        /// Default depth of serialization
        /// </summary>
        private static int mshDefaultMISerializationDepth = 1;

        internal PSMISerializer()
        {
        }

        /// <summary>
        /// Serializes an object into CimInstance
        /// </summary>
        /// <param name="source">The input object to serialize. Serializes to a default depth of 1</param>
        /// <returns>The serialized object, as CimInstance</returns>n
        public static CimInstance Serialize(Object source)
        {
            return Serialize(source, mshDefaultMISerializationDepth);
        }

        /// <summary>
        /// Serializes an object into CimInstance
        /// </summary>
        /// <param name="source">The input object to serialize. Serializes to a default depth of 1</param>
        /// <param name="serializationDepth">The input object to serialize. Serializes to a default depth of 1</param>
        /// <returns>The serialized object, as CimInstance</returns>n
        public static CimInstance Serialize(Object source, int serializationDepth)
        {
            MISerializer serializer = new MISerializer(serializationDepth);
            return serializer.Serialize(source);
        }
    }
}