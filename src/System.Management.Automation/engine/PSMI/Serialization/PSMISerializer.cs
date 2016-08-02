using Microsoft.Management.Infrastructure;
using Dbg = System.Management.Automation.Diagnostics;

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
        private static int s_mshDefaultMISerializationDepth = 1;

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
            return Serialize(source, s_mshDefaultMISerializationDepth);
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