# `PowerShell Core` configuration settings

`PowerShell Core` is configured using the following schemes:

- On Windows - Group Policy Objects (GPO), Group Policy Preferences (GPP) and configuration files
- On Unix - configuration files.

**Caution!** The configuration schemes differ from `PowerShell Core` _profile_ files.

Configuration schemes allow to customize `PowerShell Core` in the most flexible way:

- Enterprise administrators can use GPO, GPP and computer-wide confuguration files to apply approved configuration settings and mandatory security settings in a centralized manner. The same settings can be applied at user or application levels.
- Developers and consumers can use user and application level files.

`PowerShell Core` settings are grouped into policies and options. Options are normal configuration settings. Policies is high priority and overlap options.

## Priority of applying settings

Because a configuration setting can be in several schemes, the setting wins according to the priority of its scheme.

### Priorities for Policies in descending order

Scheme | Windows | Unix
-| - | -
GPO -> Computer Policy | HKLM\Software\Policies\Microsoft\Windows\PowerShellCore | /etc/powershell.config.json
GPO -> User Policy | HKCU\Software\Policies\Microsoft\Windows\PowerShellCore |
File -> Computer-Wide | %ProgramFiles%/PowerShell/powershell.config.json | /opt/Microsoft/powershell/powershell.config.json
File -> User-Wide | %APPDATA%/powershell.config.json | ~/powershell.config.json
File -> Application-Wide | $home/powershell.config.json | $home/powershell.config.json

### Priorities for Options in descending order

Scheme | Windows | Unix
-| - | -
File -> Application-Wide | $home\powershell.config.json | $home/powershell.config.json
File -> User-Wide | %APPDATA%\powershell.config.json | ~/powershell.config.json
File -> Computer-Wide | %ProgramFiles%\PowerShell\powershell.config.json | /opt/Microsoft/powershell/powershell.config.json
GPO -> User Config | HKCU\Software\Microsoft\Windows\PowerShellCore |
GPO -> Computer Config | HKLM\Software\Microsoft\Windows\PowerShellCore | /etc/powershell.config.json

## Configuration settings

A set of configuration settings in GPO scheme and file scheme for policies and options is the same. This allows to discover and configure settings in the simplest and fastest way.

## Registry keys and settings

| Key | SubKey | Option | Type
| -| - | - | -
Software\Policies\Microsoft\Windows\PowerShellCore | - | -
Software\Microsoft\PowerShellCore | - | -
| | | ExecutionPolicy | String
| | | PipelineMaxStackSizeMB | DWORD
| | ConsoleSessionConfiguration | EnableConsoleSessionConfiguration | DWORD
| | ConsoleSessionConfiguration | ConsoleSessionConfigurationName | String
| | ModuleLogging | EnableModuleLogging | DWORD
| | ModuleLogging | ModuleNames | String
| | ProtectedEventLogging | EncryptionCertificate | DWORD
| | ScriptBlockLogging | EnableScriptBlockInvocationLogging | DWORD
| | ScriptBlockLogging | EnableScriptBlockLogging | DWORD
| | Transcription | EnableTranscripting | DWORD
| | Transcription | EnableInvocationHeader | DWORD
| | Transcription | OutputDirectory | String
| | UpdatableHelp | DefaultSourcePath | String
|Software\Policies\Microsoft\Windows\EventLog | ProtectedEventLogging | EnableProtectedEventLogging | DWORD

## Json file settings

```json
{
  "PowerShellOptions": {
  // or "PowerShellPolicies": {
    "ConsoleSessionConfiguration": {
      "EnableConsoleSessionConfiguration": true,
      "ConsoleSessionConfigurationName": "name"
    },
    "ProtectedEventLogging": {
      "EnableProtectedEventLogging": false,
      "EncryptionCertificate": [
        "Joe"
      ]
    },
    "ScriptBlockLogging": {
      "EnableScriptBlockInvocationLogging": true,
      "EnableScriptBlockLogging": false
    },
    "ScriptExecution": {
      "ExecutionPolicy": "RemoteSigned",
      "PipelineMaxStackSizeMB": 10
    },
    "Transcription": {
      "EnableTranscripting": true,
      "EnableInvocationHeader": true,
      "OutputDirectory": "c:\\tmp"
    },
    "UpdatableHelp": {
      "DefaultSourcePath": "f:\\temp"
    }
  }
}
```
