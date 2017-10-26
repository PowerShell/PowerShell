;// Copyright (c) Microsoft Corporation. All rights reserved.

;#undef FACILITY_POWERSHELL
FacilityNames=(PowerShell=84:FACILITY_POWERSHELL)
SeverityNames=(Success=0x0
               CoError=0x2
              )

MessageId=1001
Facility=POWERSHELL
Severity=CoError
SymbolicName=g_INVALID_PLUGIN_CONTEXT
Language=English
The supplied plugin context is invalid.
.

MessageId=1002
Facility=POWERSHELL
Severity=CoError
SymbolicName=g_INVALID_INPUT
Language=English
Invalid parameter to plugin method %1!ls!.
.

MessageId=1003
Facility=POWERSHELL
Severity=CoError
SymbolicName=g_MANAGED_METHOD_RESOLUTION_FAILED
Language=English
Unable to find plugin methods WSManPluginShell, WSManPluginCommand, WSManPluginSend, WSManPluginReceive in the Managed plugin module.
.

MessageId=1004
Facility=POWERSHELL
Severity=CoError
SymbolicName=g_OPTION_SET_NOT_COMPLY
Language=English
Powershell plugin does not support the options requested. Make sure client is compatible with build %1!ls! of PowerShell.
.

MessageId=1005
Facility=POWERSHELL
Severity=CoError
SymbolicName=g_PSVERSION_NOT_FOUND_IN_CONFIG
Language=English
A "%1!ls!" name-value pair is expected in the "%2!ls!" element of the configuration xml. Contact your administrator.
.

MessageId=1006
Facility=POWERSHELL
Severity=CoError
SymbolicName=g_BAD_INITPARAMETERS
Language=English
The "%1!ls!" element in the configuration xml is badly formatted. Contact your administrator.
.

MessageId=1007
Facility=POWERSHELL
Severity=CoError
SymbolicName=g_INIT_CRITICALSECTION_FAILED
Language=English
An error occurred while initializing the plugin. Contact your administrator.
.

MessageId=1008
Facility=POWERSHELL
Severity=CoError
SymbolicName=g_CLR_LOAD_FAILED
Language=English
An error occurred while loading version %1!ls! of CLR. Contact your administrator.
.

MessageId=1009
Facility=POWERSHELL
Severity=CoError
SymbolicName=g_MANAGED_CONNECT_METHOD_RESOLUTION_FAILED
Language=English
An error occured while processing the connect operation. A relevant entry point is not found in the managed plugin module
Make sure that the configured plugin supports connect operation.
.
