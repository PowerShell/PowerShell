#
# This file includes OneCore powershell CoreCLR references. Do not import this file directly. It is automatically imported
# when MANAGED_PROFILE=PowerShell is specified in sources file.
#
# This file is also depended on by the tool 'TypeCatalogGen.exe'. The tool tries to parse this file to get the list of reference 
# assemblies used by OneCore powershell, and then use that list to generate the CSharp code for initializing the CoreCLR type
# catalog cache. That CSharp code will be compiled into 'Microsoft.PowerShell.CoreCLR.AssemblyLoadContext.dll' during the build.
# For more information about 'TypeCatalogGen.exe', see its source code under 'PSAssemblyLoadContext\TypeCatalogGen'.
#
# The parsing rule of this file in 'TypeCatalogGen.exe' is very simple:
#   - Read each line of this file and trim it;
#   - Skip comment lines;
#   - Match the line with the regular expression pattern '^\$\(PS_PROFILE_REF_PATH\)\\(.+);\s*\\$'
# So when adding new reference assembly entries, please make sure the new lines match the above pattern.
#
PROFILE_REF_PATH=$(SDK_REF_PATH)\Profiles
PS_PROFILE_REF_PATH=$(PROFILE_REF_PATH)\PowerShell

PROFILE_REFERENCES=\
    $(PS_PROFILE_REF_PATH)\Microsoft.CSharp.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\Microsoft.Win32.Registry.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\Microsoft.Win32.Primitives.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.collections.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\System.Collections.NonGeneric.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.collections.concurrent.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\System.Collections.Specialized.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\System.ComponentModel.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\System.ComponentModel.EventBasedAsync.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\System.ComponentModel.TypeConverter.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.dynamic.runtime.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.globalization.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.diagnostics.debug.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\System.Diagnostics.FileVersionInfo.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.diagnostics.tools.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\System.Diagnostics.TraceSource.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.diagnostics.contracts.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\System.Diagnostics.Process.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.io.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\System.IO.Pipes.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.io.filesystem.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\System.IO.FileSystem.DriveInfo.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.io.filesystem.primitives.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\System.IO.FileSystem.Watcher.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.linq.expressions.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.linq.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.net.primitives.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.reflection.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.reflection.primitives.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.reflection.emit.lightweight.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\System.Reflection.Emit.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.reflection.extensions.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\System.Reflection.TypeExtensions.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.reflection.emit.ilgeneration.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.resources.resourcemanager.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.runtime.interopservices.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\System.Runtime.Handles.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\System.Runtime.Loader.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.runtime.numerics.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.runtime.serialization.primitives.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\System.Runtime.Serialization.Xml.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.runtime.extensions.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\System.Runtime.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.text.encoding.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\System.Text.Encoding.Extensions.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.text.regularexpressions.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.threading.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.threading.tasks.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.threading.tasks.parallel.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.threading.timer.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.threading.thread.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.threading.threadpool.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.xml.readerwriter.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\System.Xml.XmlDocument.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\System.Xml.XmlSerializer.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\System.Xml.XPath.XmlDocument.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\System.Xml.XPath.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\System.Security.Cryptography.Encoding.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\System.Security.Cryptography.RandomNumberGenerator.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\System.Security.Cryptography.X509Certificates.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\System.Security.Cryptography.Encryption.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.security.securestring.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.security.principal.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.objectmodel.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\system.console.metadata_dll; \
    $(PS_PROFILE_REF_PATH)\System.Net.NetworkInformation.metadata_dll;\
    $(PS_PROFILE_REF_PATH)\mscorlib.metadata_dll;\
