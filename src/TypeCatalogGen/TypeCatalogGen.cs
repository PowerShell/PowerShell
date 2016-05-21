﻿/*
 * This is the source code for the tool 'TypeCatalogGen.exe', which has been checked in %SDXROOT%\tools\managed\v4.0\TypeCatalogGen.
 * The tool 'TypeCatalogGen.exe' is used when building 'Microsoft.PowerShell.CoreCLR.AssemblyLoadContext.dll' for OneCore powershell
 * to generate the CoreCLR type catalog initialization code, which will then be compiled into the same DLL.
 *
 * See files 'makefile.inc' and 'sources' under directory 'PSAssemblyLoadContext' to learn how the tool and the auto-generated CSharp
 * file is used.
 *
 * Compilation Note:
 *    .NET Fx Version    - 4.5
 *    Special Dependency - System.Reflection.Metadata.dll, System.Collections.Immutable.dll (Available as nuget package: http://www.nuget.org/packages/System.Reflection.Metadata)
 * To compile the code, create a VS project and get the 'System.Reflection.Metadata' package from nuget. Then add this file to the VS
 * project and compile it.
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.PowerShell.CoreCLR
{
    public class TypeCatalogGen
    {
        private const string Param_TargetCSharpFilePath = "TargetCSharpFilePath";
        private const string Param_ReferenceListPath = "ReferenceListPath";
        private const string HelpMessage = @"
Usage: TypeCatalogGen.exe <{0}> <{1}>
    - {0}: Path of the target C# source file to generate.
    - {1}: Path of the file containing all reference assembly paths, separated by semicolons.
";
        private const string TargetSourceDirNotFound = "Cannot find the target source directory. The path '{0}' doesn't exist.";
        private const string ReferenceListFileNotFound = "Cannot find the file that contains the reference list. The path '{0}' doesn't exist.";
        private const string RefAssemblyNotFound = "Reference assembly '{0}' is declared in the reference list file '{1}', but the assembly doesn't exist.";
        private const string UnexpectedFileExtension = "Cannot process '{0}' because its extension is neither '.DLL' nor '.METADATA_DLL'. Please make sure the file is an reference assembly.";

        /*
         * Go through all reference assemblies of .NET Core and generate the type catalog -> Dictionary<NamespaceQualifiedTypeName, TPAStrongName>
         * Then auto-generate the partial class 'PowerShellAssemblyLoader' that has the code to initialize the type catalog cache.
         *
         * In CoreCLR, there is no way to get all loaded TPA assemblies (.NET Framework Assemblies). In order to get type based on type name, powershell needs to know what .NET
         * types are available and in which TPA assemblies. So we have to generate the type catalog based on the reference assemblies of .NET Core.
         */
        public static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                string message = string.Format(CultureInfo.CurrentCulture, HelpMessage,
                                               Param_TargetCSharpFilePath,
                                               Param_ReferenceListPath);
                Console.WriteLine(message);
                return;
            }

            string targetFilePath = ResolveTargetFilePath(args[0]);
            List<string> refAssemblyFiles = ResolveReferenceAssemblies(args[1]);

            Dictionary<string, string> typeNameToAssemblyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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
                        // The visibilities are not combineable, e.g. you can't be both public and private, which is why these aren't independent powers of two.
                        TypeAttributes visibilityBits = typeDefinition.Attributes & TypeAttributes.VisibilityMask;
                        if (visibilityBits != TypeAttributes.Public && visibilityBits != TypeAttributes.NestedPublic)
                        {
                            continue;
                        }

                        string fullName = GetTypeFullName(metadataReader, typeDefinition);

                        // Only add unique types
                        if (!typeNameToAssemblyMap.ContainsKey(fullName))
                        {
                            typeNameToAssemblyMap.Add(fullName, strongAssemblyName);
                        }
                        else
                        {
                            Debug.WriteLine($"Not adding duplicate key {fullName}!");
                        }
                    }
                }
            }

            WritePowerShellAssemblyLoaderPartialClass(targetFilePath, typeNameToAssemblyMap);
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
                    hashImpl = HashAlgorithm.Create("SHA1");
                    break;
                case AssemblyHashAlgorithm.MD5:
                    hashImpl = HashAlgorithm.Create("MD5");
                    break;
                case AssemblyHashAlgorithm.Sha256:
                    hashImpl = HashAlgorithm.Create("SHA256");
                    break;
                case AssemblyHashAlgorithm.Sha384:
                    hashImpl = HashAlgorithm.Create("SHA384");
                    break;
                case AssemblyHashAlgorithm.Sha512:
                    hashImpl = HashAlgorithm.Create("SHA512");
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
            string strongAssemblyName = string.Format(CultureInfo.InvariantCulture,
                                                      "{0}, Version={1}, Culture={2}, PublicKeyToken={3}",
                                                      asmName, asmVersion, asmCulture, publicKeyTokenString);

            return strongAssemblyName;
        }

        /// <summary>
        /// Get the full name of a Type.
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
                fullName = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", nsName, typeName);
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
                        fullName = string.Format(CultureInfo.InvariantCulture, "{0}.{1}+{2}",
                                                 metadataReader.GetString(declaringTypeDef.Namespace),
                                                 metadataReader.GetString(declaringTypeDef.Name),
                                                 fullName);
                    }
                    else
                    {
                        fullName = string.Format(CultureInfo.InvariantCulture, "{0}+{1}",
                                                 metadataReader.GetString(declaringTypeDef.Name),
                                                 fullName);
                    }
                }
            }

            return fullName;
        }

        /// <summary>
        /// Resolve the target file path
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
        /// Resolve the reference assembly file paths
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
            string[] references = allText.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
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
        private static void WritePowerShellAssemblyLoaderPartialClass(string targetFilePath, Dictionary<string, string> typeNameToAssemblyMap)
        {
            const string SourceFormat = "            typeCatalog[\"{0}\"] = \"{1}\";";
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
    internal partial class PowerShellAssemblyLoader
    {{
        private Dictionary<string, string> InitializeTypeCatalog()
        {{
            Dictionary<string, string> typeCatalog = new Dictionary<string, string>({0}, StringComparer.OrdinalIgnoreCase);
";
            const string SourceEnd = @"
            return typeCatalog;
        }
    }
}
";

            StringBuilder sourceCode = new StringBuilder(string.Format(CultureInfo.InvariantCulture, SourceHead, typeNameToAssemblyMap.Count));
            foreach (KeyValuePair<string, string> pair in typeNameToAssemblyMap)
            {
                sourceCode.AppendLine(string.Format(CultureInfo.InvariantCulture, SourceFormat, pair.Key, pair.Value));
            }
            sourceCode.Append(SourceEnd);

            using (FileStream stream = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write))
            using (StreamWriter writer = new StreamWriter(stream, Encoding.ASCII))
            {
                writer.Write(sourceCode.ToString());
            }
        }
    }
}


