// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/*
 * This is the source code for the tool 'TypeCatalogGen.exe', which has been checked in %SDXROOT%\tools\managed\v4.0\TypeCatalogGen.
 * The tool 'TypeCatalogGen.exe' is used when building 'Microsoft.PowerShell.CoreCLR.AssemblyLoadContext.dll' for OneCore powershell
 * to generate the CoreCLR type catalog initialization code, which will then be compiled into the same DLL.
 *
 * See files 'makefile.inc' and 'sources' under directory 'PSAssemblyLoadContext' to learn how the tool and the auto-generated CSharp
 * file is used.
 *
 * Compilation Note:
 *    .NET Fx Version    - 4.5
 *    Special Dependency - System.Reflection.Metadata.dll, System.Collections.Immutable.dll (Available as nuget package: https://www.nuget.org/packages/System.Reflection.Metadata)
 * To compile the code, create a VS project and get the 'System.Reflection.Metadata' package from nuget. Then add this file to the VS
 * project and compile it.
*/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.PowerShell.CoreCLR
{
    public static class TypeCatalogGen
    {
        // Help messages
        private const string Param_TargetCSharpFilePath = "TargetCSharpFilePath";
        private const string Param_ReferenceListPath = "ReferenceListPath";
        private const string Param_PrintDebugMessage = "-debug";
        private const string HelpMessage = @"
Usage: TypeCatalogGen.exe <{0}> <{1}> [{2}]
    - {0}: Path of the target C# source file to generate.
    - {1}: Path of the file containing all reference assembly paths, separated by semicolons.
    - [{2}]: Write out debug messages. Optional.
";

        // Error messages
        private const string TargetSourceDirNotFound = "Cannot find the target source directory. The path '{0}' doesn't exist.";
        private const string ReferenceListFileNotFound = "Cannot find the file that contains the reference list. The path '{0}' doesn't exist.";
        private const string RefAssemblyNotFound = "Reference assembly '{0}' is declared in the reference list file '{1}', but the assembly doesn't exist.";
        private const string UnexpectedFileExtension = "Cannot process '{0}' because its extension is neither '.DLL' nor '.METADATA_DLL'. Please make sure the file is an reference assembly.";

        // Format strings for constructing type names
        private const string Format_RegularType = "{0}.{1}";
        private const string Format_SingleLevelNestedType = "{0}.{1}+{2}";
        private const string Format_MultiLevelNestedType = "{0}+{1}";

        /*
         * Go through all reference assemblies of .NET Core and generate the type catalog -> Dictionary<NamespaceQualifiedTypeName, TPAStrongName>
         * Then auto-generate the partial class 'PowerShellAssemblyLoadContext' that has the code to initialize the type catalog cache.
         *
         * In CoreCLR, there is no way to get all loaded TPA assemblies (.NET Framework Assemblies). In order to get type based on type name, powershell needs to know what .NET
         * types are available and in which TPA assemblies. So we have to generate the type catalog based on the reference assemblies of .NET Core.
         */
        public static void Main(string[] args)
        {
            if (args.Length < 2 || args.Length > 3)
            {
                string message = string.Format(CultureInfo.CurrentCulture, HelpMessage,
                                               Param_TargetCSharpFilePath,
                                               Param_ReferenceListPath,
                                               Param_PrintDebugMessage);
                Console.WriteLine(message);
                return;
            }

            bool printDebugMessage = args.Length == 3 && string.Equals(Param_PrintDebugMessage, args[2], StringComparison.OrdinalIgnoreCase);
            string targetFilePath = ResolveTargetFilePath(args[0]);
            List<string> refAssemblyFiles = ResolveReferenceAssemblies(args[1]);

            Dictionary<string, TypeMetadata> typeNameToAssemblyMap = new Dictionary<string, TypeMetadata>(StringComparer.OrdinalIgnoreCase);

            // mscorlib.metadata_dll doesn't contain any type definition.
            foreach (string filePath in refAssemblyFiles)
            {
                if (!filePath.EndsWith(".METADATA_DLL", StringComparison.OrdinalIgnoreCase) &&
                    !filePath.EndsWith(".DLL", StringComparison.OrdinalIgnoreCase))
                {
                    string message = string.Format(CultureInfo.CurrentCulture, UnexpectedFileExtension, filePath);
                    throw new InvalidOperationException(message);
                }

                using (Stream stream = File.OpenRead(filePath))
                using (PEReader peReader = new PEReader(stream))
                {
                    MetadataReader metadataReader = peReader.GetMetadataReader();
                    string strongAssemblyName = GetAssemblyStrongName(metadataReader);

                    foreach (TypeDefinitionHandle typeHandle in metadataReader.TypeDefinitions)
                    {
                        // We only care about public types
                        TypeDefinition typeDefinition = metadataReader.GetTypeDefinition(typeHandle);
                        // The visibility mask is used to mask out the bits that contain the visibility.
                        // The visibilities are not combinable, e.g. you can't be both public and private, which is why these aren't independent powers of two.
                        TypeAttributes visibilityBits = typeDefinition.Attributes & TypeAttributes.VisibilityMask;
                        if (visibilityBits != TypeAttributes.Public && visibilityBits != TypeAttributes.NestedPublic)
                        {
                            continue;
                        }

                        string fullName = GetTypeFullName(metadataReader, typeDefinition);
                        bool isTypeObsolete = IsTypeObsolete(metadataReader, typeDefinition);

                        if (!typeNameToAssemblyMap.ContainsKey(fullName))
                        {
                            // Add unique type.
                            typeNameToAssemblyMap.Add(fullName, new TypeMetadata(strongAssemblyName, isTypeObsolete));
                        }
                        else if (typeNameToAssemblyMap[fullName].IsObsolete && !isTypeObsolete)
                        {
                            // Duplicate types found defined in different assemblies, but the previous one is obsolete while the current one is not.
                            // Replace the existing type with the current one.
                            if (printDebugMessage)
                            {
                                var existingTypeMetadata = typeNameToAssemblyMap[fullName];
                                Console.WriteLine($@"
REPLACE '{fullName}' from '{existingTypeMetadata.AssemblyName}' (IsObsolete? {existingTypeMetadata.IsObsolete})
  WITH '{strongAssemblyName}' (IsObsolete? {isTypeObsolete})");
                            }

                            typeNameToAssemblyMap[fullName] = new TypeMetadata(strongAssemblyName, isTypeObsolete);
                        }
                        else if (printDebugMessage)
                        {
                            // Duplicate types found defined in different assemblies, and fall into one of the following conditions:
                            //  - both are obsolete
                            //  - both are not obsolete
                            //  - the existing type is not obsolete while the new one is obsolete
                            var existingTypeMetadata = typeNameToAssemblyMap[fullName];
                            Console.WriteLine($@"
DUPLICATE key '{fullName}' from '{strongAssemblyName}' (IsObsolete? {isTypeObsolete}).
  -- Already exist in '{existingTypeMetadata.AssemblyName}' (IsObsolete? {existingTypeMetadata.IsObsolete})");
                        }
                    }
                }
            }

            WritePowerShellAssemblyLoadContextPartialClass(targetFilePath, typeNameToAssemblyMap);
        }

        /// <summary>
        /// Check if the type is obsolete.
        /// </summary>
        private static bool IsTypeObsolete(MetadataReader reader, TypeDefinition typeDefinition)
        {
            const string obsoleteFullTypeName = "System.ObsoleteAttribute";

            foreach (var customAttributeHandle in typeDefinition.GetCustomAttributes())
            {
                var customAttribute = reader.GetCustomAttribute(customAttributeHandle);
                if (IsAttributeOfType(reader, customAttribute, obsoleteFullTypeName))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if the attribute type name is what we expected.
        /// </summary>
        private static bool IsAttributeOfType(MetadataReader reader, CustomAttribute customAttribute, string expectedTypeName)
        {
            string attributeFullName = null;
            switch (customAttribute.Constructor.Kind)
            {
                case HandleKind.MethodDefinition:
                    // Attribute is defined in the same module
                    MethodDefinition methodDef = reader.GetMethodDefinition((MethodDefinitionHandle)customAttribute.Constructor);
                    TypeDefinitionHandle declaringTypeDefHandle = methodDef.GetDeclaringType();
                    if (declaringTypeDefHandle.IsNil) { /* Global method */ return false; }

                    TypeDefinition declaringTypeDef = reader.GetTypeDefinition(declaringTypeDefHandle);
                    attributeFullName = GetTypeFullName(reader, declaringTypeDef);
                    break;

                case HandleKind.MemberReference:
                    MemberReference memberRef = reader.GetMemberReference((MemberReferenceHandle)customAttribute.Constructor);
                    switch (memberRef.Parent.Kind)
                    {
                        case HandleKind.TypeReference:
                            TypeReference typeRef = reader.GetTypeReference((TypeReferenceHandle)memberRef.Parent);
                            attributeFullName = GetTypeFullName(reader, typeRef);
                            break;

                        case HandleKind.TypeDefinition:
                            TypeDefinition typeDef = reader.GetTypeDefinition((TypeDefinitionHandle)memberRef.Parent);
                            attributeFullName = GetTypeFullName(reader, typeDef);
                            break;

                        default:
                            // constructor is global method, vararg method, or from a generic type.
                            return false;
                    }

                    break;

                default:
                    throw new BadImageFormatException("Invalid custom attribute.");
            }

            return string.Equals(attributeFullName, expectedTypeName, StringComparison.Ordinal);
        }

        /// <summary>
        /// Get the strong name of a reference assembly represented by the 'metadataReader'
        /// </summary>
        private static string GetAssemblyStrongName(MetadataReader metadataReader)
        {
            AssemblyDefinition assemblyDefinition = metadataReader.GetAssemblyDefinition();
            string asmName = metadataReader.GetString(assemblyDefinition.Name);
            string asmVersion = assemblyDefinition.Version.ToString();
            string asmCulture = metadataReader.GetString(assemblyDefinition.Culture);
            asmCulture = (asmCulture == string.Empty) ? "neutral" : asmCulture;

            AssemblyHashAlgorithm hashAlgorithm = assemblyDefinition.HashAlgorithm;
            BlobHandle blobHandle = assemblyDefinition.PublicKey;
            BlobReader blobReader = metadataReader.GetBlobReader(blobHandle);
            byte[] publickey = blobReader.ReadBytes(blobReader.Length);

            HashAlgorithm hashImpl = null;
            switch (hashAlgorithm)
            {
                case AssemblyHashAlgorithm.Sha1:
                    hashImpl = SHA1.Create();
                    break;
                case AssemblyHashAlgorithm.MD5:
                    hashImpl = MD5.Create();
                    break;
                case AssemblyHashAlgorithm.Sha256:
                    hashImpl = SHA256.Create();
                    break;
                case AssemblyHashAlgorithm.Sha384:
                    hashImpl = SHA384.Create();
                    break;
                case AssemblyHashAlgorithm.Sha512:
                    hashImpl = SHA512.Create();
                    break;
                default:
                    throw new NotSupportedException();
            }

            byte[] publicKeyHash = hashImpl.ComputeHash(publickey);
            byte[] publicKeyTokenBytes = new byte[8];
            // Note that, the low 8 bytes of the hash of public key in reverse order is the public key tokens.
            for (int i = 1; i <= 8; i++)
            {
                publicKeyTokenBytes[i - 1] = publicKeyHash[publicKeyHash.Length - i];
            }

            // Convert bytes to hex format strings in lower case.
            string publicKeyTokenString = BitConverter.ToString(publicKeyTokenBytes).Replace("-", string.Empty).ToLowerInvariant();
            string strongAssemblyName = string.Create(CultureInfo.InvariantCulture, $"{asmName}, Version={asmVersion}, Culture={asmCulture}, PublicKeyToken={publicKeyTokenString}");

            return strongAssemblyName;
        }

        /// <summary>
        /// Get the full name of a Type reference.
        /// </summary>
        private static string GetTypeFullName(MetadataReader metadataReader, TypeReference typeReference)
        {
            string fullName;
            string typeName = metadataReader.GetString(typeReference.Name);
            string nsName = metadataReader.GetString(typeReference.Namespace);

            EntityHandle resolutionScope = typeReference.ResolutionScope;
            if (resolutionScope.IsNil || resolutionScope.Kind != HandleKind.TypeReference)
            {
                fullName = string.Format(CultureInfo.InvariantCulture, Format_RegularType, nsName, typeName);
            }
            else
            {
                // It's a nested type.
                fullName = typeName;
                while (!resolutionScope.IsNil && resolutionScope.Kind == HandleKind.TypeReference)
                {
                    TypeReference declaringTypeRef = metadataReader.GetTypeReference((TypeReferenceHandle)resolutionScope);
                    resolutionScope = declaringTypeRef.ResolutionScope;
                    if (resolutionScope.IsNil || resolutionScope.Kind != HandleKind.TypeReference)
                    {
                        fullName = string.Format(CultureInfo.InvariantCulture, Format_SingleLevelNestedType,
                                                 metadataReader.GetString(declaringTypeRef.Namespace),
                                                 metadataReader.GetString(declaringTypeRef.Name),
                                                 fullName);
                    }
                    else
                    {
                        fullName = string.Format(CultureInfo.InvariantCulture, Format_MultiLevelNestedType,
                                                 metadataReader.GetString(declaringTypeRef.Name),
                                                 fullName);
                    }
                }
            }

            return fullName;
        }

        /// <summary>
        /// Get the full name of a Type definition.
        /// </summary>
        private static string GetTypeFullName(MetadataReader metadataReader, TypeDefinition typeDefinition)
        {
            string fullName;
            string typeName = metadataReader.GetString(typeDefinition.Name);
            string nsName = metadataReader.GetString(typeDefinition.Namespace);

            // Get the enclosing type if the type is nested
            TypeDefinitionHandle declaringTypeHandle = typeDefinition.GetDeclaringType();
            if (declaringTypeHandle.IsNil)
            {
                fullName = string.Format(CultureInfo.InvariantCulture, Format_RegularType, nsName, typeName);
            }
            else
            {
                fullName = typeName;
                while (!declaringTypeHandle.IsNil)
                {
                    TypeDefinition declaringTypeDef = metadataReader.GetTypeDefinition(declaringTypeHandle);
                    declaringTypeHandle = declaringTypeDef.GetDeclaringType();
                    if (declaringTypeHandle.IsNil)
                    {
                        fullName = string.Format(CultureInfo.InvariantCulture, Format_SingleLevelNestedType,
                                                 metadataReader.GetString(declaringTypeDef.Namespace),
                                                 metadataReader.GetString(declaringTypeDef.Name),
                                                 fullName);
                    }
                    else
                    {
                        fullName = string.Format(CultureInfo.InvariantCulture, Format_MultiLevelNestedType,
                                                 metadataReader.GetString(declaringTypeDef.Name),
                                                 fullName);
                    }
                }
            }

            return fullName;
        }

        /// <summary>
        /// Resolve the target file path.
        /// </summary>
        private static string ResolveTargetFilePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(Param_TargetCSharpFilePath);
            }

            string targetPath = Path.GetFullPath(path);
            string targetParentFolder = Path.GetDirectoryName(targetPath);
            if (!Directory.Exists(targetParentFolder))
            {
                string message = string.Format(CultureInfo.CurrentCulture, TargetSourceDirNotFound, targetParentFolder ?? "null");
                throw new ArgumentException(message);
            }

            return targetPath;
        }

        /// <summary>
        /// Resolve the reference assembly file paths.
        /// </summary>
        private static List<string> ResolveReferenceAssemblies(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(Param_ReferenceListPath);
            }

            string referenceListPath = Path.GetFullPath(path);
            if (!File.Exists(referenceListPath))
            {
                string message = string.Format(CultureInfo.CurrentCulture, ReferenceListFileNotFound, referenceListPath ?? "null");
                throw new ArgumentException(message);
            }

            string allText = File.ReadAllText(referenceListPath);
            string[] references = allText.Split(';', StringSplitOptions.RemoveEmptyEntries);
            List<string> refAssemblyFiles = new List<string>(120);

            for (int i = 0; i < references.Length; i++)
            {
                // Ignore entries that only contain white spaces
                if (string.IsNullOrWhiteSpace(references[i]))
                {
                    continue;
                }

                string refAssemblyPath = references[i].Trim();
                if (File.Exists(refAssemblyPath))
                {
                    refAssemblyFiles.Add(refAssemblyPath);
                }
                else
                {
                    string message = string.Format(CultureInfo.CurrentCulture, RefAssemblyNotFound, refAssemblyPath, referenceListPath);
                    throw new InvalidDataException(message);
                }
            }

            return refAssemblyFiles;
        }

        /// <summary>
        /// Generate the CSharp source code that initialize the type catalog.
        /// </summary>
        private static void WritePowerShellAssemblyLoadContextPartialClass(string targetFilePath, Dictionary<string, TypeMetadata> typeNameToAssemblyMap)
        {
            const string SourceFormat = "{2}                {{\"{0}\", \"{1}\"}},";
            const string SourceHead = @"//
// This file is auto-generated by TypeCatalogGen.exe during build of Microsoft.PowerShell.CoreCLR.AssemblyLoadContext.dll.
// This file will be compiled into Microsoft.PowerShell.CoreCLR.AssemblyLoadContext.dll.
//
// In CoreCLR, there is no way to get all loaded TPA assemblies (.NET Framework Assemblies). In order to get type based on type
// name, powershell needs to know what .NET types are available and in which TPA assemblies. So we have to generate this type
// catalog based on the reference assemblies of .NET Core.
//
using System.Collections.Generic;

namespace System.Management.Automation
{{
    internal partial class PowerShellAssemblyLoadContext
    {{
        private static Dictionary<string, string> InitializeTypeCatalog()
        {{
            return new Dictionary<string, string>({0}, StringComparer.OrdinalIgnoreCase) {{";
            const string SourceEnd = @"
            };
        }
    }
}
";

            StringBuilder sourceCode = new StringBuilder(string.Format(CultureInfo.InvariantCulture, SourceHead, typeNameToAssemblyMap.Count));
            foreach (KeyValuePair<string, TypeMetadata> pair in typeNameToAssemblyMap)
            {
                sourceCode.Append(string.Format(CultureInfo.InvariantCulture, SourceFormat, pair.Key, pair.Value.AssemblyName, Environment.NewLine));
            }

            sourceCode.Append(SourceEnd);

            using (FileStream stream = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write))
            using (StreamWriter writer = new StreamWriter(stream, Encoding.ASCII))
            {
                writer.Write(sourceCode.ToString());
            }
        }

        /// <summary>
        /// Helper class to keep the metadata of a type.
        /// </summary>
        private sealed class TypeMetadata
        {
            internal readonly string AssemblyName;
            internal readonly bool IsObsolete;
            internal TypeMetadata(string assemblyName, bool isTypeObsolete)
            {
                this.AssemblyName = assemblyName;
                this.IsObsolete = isTypeObsolete;
            }
        }
    }
}

