;// Copyright (c) Microsoft Corporation. All rights reserved.

MessageId=1
SymbolicName=MISSING_COMMAND_LINE_ARGUMENT
Language=English
Missing argument for parameter %1!ls!.
.

MessageId=2
SymbolicName=INVALID_CONSOLE_FILE_PATH
Language=English
Windows PowerShell console file path "%1!ls!" is invalid.
.

MessageId=3
SymbolicName=CLR_VERSION_NOT_INSTALLED
Language=English
Version %1!ls! of the .NET Framework is not installed and it is required to run version %2!ls! of Windows PowerShell.
.

MessageId=4
SymbolicName=CORBINDTORUNTIME_FAILED
Language=English
CLR initialization failed with error %1!lx!.
.

MessageId=5
SymbolicName=STARTING_CLR_FAILED
Language=English
Starting the CLR failed with HRESULT %1!lx!.
.

MessageId=6
SymbolicName=GETTING_DEFAULT_DOMAIN_FAILED
Language=English
Internal Windows PowerShell error.  Default CLR domain initialization failed with error %1!lx!.
.

MessageId=7
SymbolicName=CREATING_MSH_ENTRANCE_FAILED
Language=English
Internal Windows PowerShell error.  Loading managed Windows PowerShell failed with error %1!lx!.
.

MessageId=8
SymbolicName=GETTING_DISPATCH_ID_FAILED
Language=English
Internal Windows PowerShell error.  Retrieving Dispatch ID for managed Windows PowerShell entrance method failed with error %1!lx!.
.

MessageId=9
SymbolicName=INOVKING_MSH_ENTRANCE_FAILED
Language=English
Internal Windows PowerShell error.  Invoking managed Windows PowerShell failed with error %1!lx!.
.

MessageId=10
SymbolicName=MANAGED_MSH_EXCEPTION
Language=English
Windows PowerShell terminated with the following error: %n %1!ls!
.

MessageId=11
SymbolicName=READ_XML_COM_INIT_FAILED
Language=English
Internal Windows PowerShell error.  COM initialization failed while reading Windows PowerShell console file with error %1!lx!.
.

MessageId=12
SymbolicName=READ_XML_CREATE_DOMDOCUMENT_FAILED
Language=English
Internal Windows PowerShell error.  DOMDocument creation for Windows PowerShell console file failed with error %1!lx!.
.

MessageId=13
SymbolicName=READ_XML_LOAD_FILE_FAILED
Language=English
Failed to load Windows PowerShell console file "%1!ls!".
.

MessageId=14
SymbolicName=EMPTY_REG_SZ_VALUE
Language=English
The value of the registry key %1!ls!\%2!ls! cannot be empty.
.

MessageId=15
SymbolicName=READ_XML_GET_CONSOLE_SCHEMA_VERSION_FAILED
Language=English
Cannot locate the required element ConsoleSchemaVersion in the Windows PowerShell console file "%1!ls!".
.

MessageId=16
SymbolicName=READ_XML_INVALID_CONSOLE_SCHEMA_VERSION
Language=English
A required element ConsoleSchemaVersion in Windows PowerShell console file "%1!ls!" is invalid.
.

MessageId=17
SymbolicName=INVALID_MONAD_VERSION
Language=English
"%1!ls!" is not a valid Windows PowerShell version.  Specify a valid Windows PowerShell version of the format major.minor version.
.

MessageId=18
SymbolicName=READ_XML_GET_MONAD_VERSION_TEXT_FAILED
Language=English
Cannot read the Windows PowerShell version from Windows PowerShell console file "%1!ls!".
.

MessageId=19
SymbolicName=SEARCH_LATEST_REG_KEY_FAILED_WITH
Language=English
Encountered a problem reading the registry.  Cannot read registry key %1!ls!.  System error:%n %2!ls!.
.

MessageId=20
SymbolicName=OPEN_REG_KEY_FAILED_WITH
Language=English
Encountered a problem reading the registry.  Cannot open registry key %1!ls!.  System error:%n  %2!ls!.
.

MessageId=21
SymbolicName=CLOSE_REG_KEY_FAILED_WITH
Language=English
Closing registry key %1!ls! causes the following Win32 error:%n  %2!ls!
.

MessageId=22
SymbolicName=READ_REG_VALUE_FAILED_WITH
Language=English
Reading the value of registry key %1!ls!\%2!ls! causes the following Win32 error:%n  %3!ls!
.

MessageId=23
SymbolicName=CREATE_MSHENGINE_REG_KEY_PATH_FAILED_WITH
Language=English
Encountered a problem creating the registry key path PowerShellEngine.  System error:%n %1!ls!.
.

MessageId=24
SymbolicName=EXPECT_REG_SZ_VALUE
Language=English
Invalid registry key value.  Value for registry key %1!ls!\%2!ls! must be REG_SZ.
.

MessageId=25
SymbolicName=MSH_VERSION_NOT_INSTALLED
Language=English
Cannot start Windows PowerShell version %1!ls! because it is not installed.
.

MessageId=26
SymbolicName=INCORRECT_CONSOLE_FILE_EXTENSION
Language=English
Windows PowerShell console file "%1!ls!" extension is not psc1. Windows PowerShell console file extension must be psc1.
.

MessageId=27
SymbolicName=INVALID_REG_MSHVERSION_VALUE
Language=English
The value of registry key %1!ls!\%2!ls! is an invalid .NET version. Valid version format is major.minor.build.revision.
.

MessageId=28
SymbolicName=INCOMPATIBLE_MINOR_VERSION
Language=English
Cannot start Windows PowerShell. No version of Windows PowerShell compatible to %1!ls! is installed.
.

MessageId=29
SymbolicName=NO_COMPLETELY_INSTALLED_FOUND_VERSION
Language=English
Cannot start Windows PowerShell.  No correctly installed versions of Windows PowerShell found.
.

MessageId=30
SymbolicName=MISSING_REG_KEY
Language=English
Encountered a problem reading the registry.  Cannot find registry key %1!ls!.
.

MessageId=31
SymbolicName=READ_XML_LOAD_FILE_FAILED_WITH_SPECIFIC_ERROR
Language=English
Failed to load Windows PowerShell console file "%1!ls!". WIN 32 error: %2!ls!
.

MessageId=32
SymbolicName=READ_XML_LOAD_FILE_FAILED_WITH_SPECIFIC_POSITION_ERROR
Language=English
Failed to load Windows PowerShell console file "%1!ls!": %2!ls!At line:%3!li! char:%4!li!
.

MessageId=33
SymbolicName=READ_XML_GET_EMPTY_MONAD_VERSION_TEXT
Language=English
Windows PowerShell version in the Windows PowerShell console file "%1!ls!" cannot be empty.
.

MessageId=34
SymbolicName=READ_XML_GET_PSCONSOLEFILE_FAILED
Language=English
Cannot locate the required element PSConsoleFile in the Windows PowerShell console file "%1!ls!".
.

MessageId=35
SymbolicName=NOTSUPPORTED_MONAD_VERSION
Language=English
The requested Windows PowerShell version %1!d! is not supported on WinPE. WinPE supports only Windows PowerShell %1!d!.
.

MessageId=36
SymbolicName=NONSTANDARD_CLR_VERSION
Language=English
Warning: Windows PowerShell was started with CLR version "%1!ls!". This CLR version has not been tested with Windows PowerShell and might not operate properly. For more information about supported versions of the CLR, see http://go.microsoft.com/fwlink/?LinkId=215538.%r%n
.

MessageId=37
SymbolicName=SHELLBANNER1
Language=English
Windows PowerShell
.

MessageId=38
SymbolicName=SHELLBANNER2
Language=English
Copyright (c) Microsoft Corporation. All rights reserved.
.

MessageId=39
SymbolicName=MISSING_REG_KEY1
Language=English
Encountered a problem reading the registry.  Cannot find registry key %1!ls!. The Windows PowerShell %2!ls! engine is not installed on this computer.
.
