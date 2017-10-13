<#
Enumerate all events in the manifest and create a hash table of event id to message id.
>  $manifest.assembly.instrumentation.events.provider.events.event

Enumerate all messages in the manifest and create a hash table of message id to message data.
> $manifest.assembly.localization.resources.stringTable.string
> Message data will be the message text and the number of replaceable parameters in the message.
> Only messages referenced by event ids will be in the table.

Generate a resx file containing the messages.
Generate a static C# class containing
> A hash table mapping event id to message data (resource path, resource id, and the number of replaceable parameters)
> A static method for formatting the message to log and calling the native SysLog.

NOTE: A native binary will also need to be generated that wraps the call to syslog and exports a function to call from
managed code.  The static method mentioned above will call this export through PInvoke.
#>

using namespace System.Collections.Generic
using namespace System.Globalization
using namespace System.Xml

#region resx string templates

[string] $resxPrologue = @"
<?xml version="1.0" encoding="utf-8"?>
<root>
<!--
    This is a generated file.
    It is produced from Microsoft-Windows-PowerShell-Instrumentation.man using ResxGen.ps1.
    To add or change logged events and the associated resources, edit Microsoft-Windows-PowerShell-Instrumentation.man
    then rerun ResxGen.ps1 to produce an updated CS and Resx file.
-->
<xsd:schema id="root" xmlns="" xmlns:xsd="https://www.w3.org/2001/XMLSchema" xmlns:msdata="urn:schemas-microsoft-com:xml-msdata">
<xsd:import namespace="https://www.w3.org/XML/1998/namespace" />
<xsd:element name="root" msdata:IsDataSet="true">
  <xsd:complexType>
    <xsd:choice maxOccurs="unbounded">
      <xsd:element name="metadata">
        <xsd:complexType>
          <xsd:sequence>
            <xsd:element name="value" type="xsd:string" minOccurs="0" />
          </xsd:sequence>
          <xsd:attribute name="name" use="required" type="xsd:string" />
          <xsd:attribute name="type" type="xsd:string" />
          <xsd:attribute name="mimetype" type="xsd:string" />
          <xsd:attribute ref="xml:space" />
        </xsd:complexType>
      </xsd:element>
      <xsd:element name="assembly">
        <xsd:complexType>
          <xsd:attribute name="alias" type="xsd:string" />
          <xsd:attribute name="name" type="xsd:string" />
        </xsd:complexType>
      </xsd:element>
      <xsd:element name="data">
        <xsd:complexType>
          <xsd:sequence>
            <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
            <xsd:element name="comment" type="xsd:string" minOccurs="0" msdata:Ordinal="2" />
          </xsd:sequence>
          <xsd:attribute name="name" type="xsd:string" use="required" msdata:Ordinal="1" />
          <xsd:attribute name="type" type="xsd:string" msdata:Ordinal="3" />
          <xsd:attribute name="mimetype" type="xsd:string" msdata:Ordinal="4" />
          <xsd:attribute ref="xml:space" />
        </xsd:complexType>
      </xsd:element>
      <xsd:element name="resheader">
        <xsd:complexType>
          <xsd:sequence>
            <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
          </xsd:sequence>
          <xsd:attribute name="name" type="xsd:string" use="required" />
        </xsd:complexType>
      </xsd:element>
    </xsd:choice>
  </xsd:complexType>
</xsd:element>
</xsd:schema>
<resheader name="resmimetype">
    <value>text/microsoft-resx</value>
</resheader>
<resheader name="version">
    <value>2.0</value>
</resheader>
<resheader name="reader">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
</resheader>
<resheader name="writer">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
</resheader>
<data name="MissingEventIdMessage" xml:space="preserve">
    <value>A message was not found for event id {0}.</value>
</data>
"@

[string] $resxEntryTemplate = @"

<data name="{0}" xml:space="preserve">
    <value>{1}</value>
</data>
"@

[string] $resxEpilogue = @"

</root>
"@

#endregion resx string templates

#region C# code template strings

# {0} The namespace to for the class
# {1} The class name
[string] $codePrologue = @'
/*
    This is a generated file.
    It is produced from Microsoft-Windows-PowerShell-Instrumentation.man using ResxGen.ps1.
    To add or change logged events and the associated resources, edit Microsoft-Windows-PowerShell-Instrumentation.man
    then rerun ResxGen.ps1 to produce an updated CS and Resx file.
*/
using System.Collections.Generic;

namespace {0}
{{
    /// <summary>
    /// Provides a class for describing a message resource for an ETW event.
    /// </summary>
    internal class {1}
    {{
        /// <summary>
        /// Initializes a new instance of this class
        /// </summary>
        /// <param name="resourceName">The name of the message resource</param>
        /// <param name="parameterCount">The number of required to format the message</param>
        public {1}(string resourceName, int parameterCount)
        {{
            this.ResourceName = resourceName;
            this.ParameterCount = parameterCount;
        }}

        /// <summary>
        /// The resource name for the message resource
        /// </summary>
        public string Name {{get; private set;}}

        /// <summary>
        /// The number of parameters needed to format the message
        /// </summary>
        public int ParameterCount {{get; private set;}}

        #region Static Fields and Methods

        private static Dictionary<int, string> _resources = new Dictionary<int, EventResource>;

        /// <summary>
        /// Gets the {1} describing the message resource for the specified event id.
        /// </summary>
        /// <param name="eventId">The event id for the message resource to retrieve.</param>
        /// <returns>A {1} for the specified event id; otherwise, a null reference.</returns>
        public static {1} GetMessage(int eventId)
        {{
            {1} result = null;
            _resources.TryGetValue(eventId, out  result);
            return result;
        }}

        const int MissingEventId = -1;

        /// <summary>
        /// Gets the {1} describing the a message resource used when an valid event id
        /// is not found.
        /// </summary>
        public static {1} GetMissingEventMessage()
        {{
            return GetMessage(MissingEventId);
        }}

        private static {1}()
        {{
            // Add the resource for mismatched event ids.
            _resources.Add(MissingEventId, new {1}("MissingEventIdMessage", 1));

'@

# Adds an entry to the eventid -> resource name dictionary
# {0} - class name
# {1} - event id
# {2} - the resource name for the event message
# {3} - the number of parameters required for to format the message.
[string] $codeEventEntryTemplate = @"

            _resources.Add({1}, new {0}("{2}", {3}));
"@

[string] $codeEpilogue = @"
s
        }}

        #endregion Static Fields and Methods
    }}
}}
"@

#endregion C# code template strings


<#
  Provides a class for encapsulating a resource string entry from an ETW manifest
#>
class EventMessage
{
    #region properties

    <#
      Gets the message id.
      This is used as a resource name.
    #>
    [string] $Id

    <#
      Gets the identifier used by an event to reference the message.
    #>
    [string] $EventReference

    <#
      The number of replaceable parameters in the message; from 0 through 99
      Used to determine if string.Format is needed.
    #>
    [int] $ParameterCount

    <#
      Gets the message text
    #>
    [string] $Value

    #endregion properties

    <#
    .SYNOPSIS
      replaces FormatMessage format specifiers with String.Format equivalent.

    .PARAMETER message
      The message string to update.

    .NOTES
      See https://msdn.microsoft.com/en-us/library/windows/desktop/ms679351(v=vs.85).aspx.
      Replaceable parameters are limited to %1 ... %99. Width and precision specifiers are
      not currently supported since the manifest does not use them at the time of this writing.
    #>
    hidden [void] SetMessage([string] $message)
    {
        foreach ($source in [EventMessage]::escapeStrings.Keys)
        {
            if ($message.Contains($source))
            {
                $dest = $[EventMessage]::escapeStrings[$source]
                $message = $message.Replace($source, $dest)
            }
        }

        [int] $paramCount = 0
        for ($index = 1; $index -le 99; $index++)
        {
            [string] $source = [string]::Format([CultureInfo]::InvariantCulture, '%{0}', $index)

            if ($message.Contains($source))
            {
                $paramCount = $index;
                # convert %1->%99 to {0}->{98}
                [string] $target = [string]::Format([CultureInfo]::InvariantCulture, '{0}{1}{2}', '{', $index - 1, '}')
                $message = $message.Replace($source, $target)
            }
        }
        $this.Value = $message
        $this.ParameterCount = $paramCount
    }

    EventMessage([XmlElement] $element)
    {
        $this.EventReference =[string]::Format([System.Globalization.CultureInfo]::InvariantCulture, '$(string.{0})', $element.Id)
        [string] $messageId = $element.id
        if ($messageId.EndsWith('.message'))
        {
            $messageId = $messageId.Substring(0, $messageId.Length - '.message'.Length)
        }
        if ($messageId.Contains('.'))
        {
            $this.Id = $messageId.Replace('.', '')
        }
        else
        {
            $this.Id = $messageId
        }
        $this.SetMessage($element.value)
    }

    static hidden $escapeStrings =
    @(
        {Source = '%t'; Dest = '`t'},
        {Source = '%n'; Dest = '`n'},
        {Source = '%r'; Dest = '`r'},
        {Source = '%%'; Dest = '`%'},
        {Source = '%space'; Dest = ' '},
        {Source = '%.'; Dest = '.'}
    )
}


enum LogLevel
{
    Always = 0
    Critical = 1
    Error = 2
    Warning = 3
    Information = 4
    Verbose = 5
}

class EventEntry
{
    [int] $EventId
    [string] $MessageReference
    [EventMessage] $EventMessage
    [string] $Channel
    [LogLevel] $Level
    [string]  $Task

    EventEntry ([XmlElement] $element)
    {
        $idValue = $element.value.Trim()
        if ($idValue.StartsWith('0x', [StringComparison]::OrdinalIgnoreCase))
        {
            $idValue = $idValue.SubString(2)
        }
        $this.EventId = [Int32]::Parse($idValue, [System.Globalization.NumberStyles]::HexNumber )
        $this.Channel = $element.channel
        $this.Level = [EventEntry]::levelNames[$element.level]
        $this.MessageReference = $element.message
        $this.Task = $element.Task
    }

    static hidden $levelNames =
    @{
        'win:Always' = [LogLevel]::Always;
        'win:Verbose' = [LogLevel]::Verbose;
        'win:Informational' = [LogLevel]::Information;
        'win:Warning' = [LogLevel]::Warning;
        'win:Error' = [LogLevel]::Error;
        'win:Critical' = [LogLevel]::Critical;
    }
}

class Manifest
{
    [Dictionary[int, EventEntry]] $Events
    [Dictionary[string, EventMessage]] $Messages
    [Dictionary[string, string]] $Tasks
    [Dictionary[string, string]] $Opcodes
    [Dictionary[string, string]] $Channels

    Manifest([string] $Path)
    {
        [xml] $man = Get-Content -Path $Path

        $messageTable = [Dictionary[string, EventMessage]]::new()
        foreach ($item in $man.assembly.localization.resources.stringTable.string)
        {
            $eventMessage = [EventMessage]::new($item)
            $messageTable.Add($eventMessage.EventReference, $eventMessage)
        }

        $this.Tasks =  [Dictionary[string, string]]::new()
        foreach ($item in $man.assembly.instrumentation.events.provider.tasks.task)
        {
            $this.Tasks.Add($item.Symbol, $item.Name)
        }

        $this.Opcodes = [Dictionary[string, string]]::new()
        foreach ($item in $man.assembly.instrumentation.events.provider.opcodes.opcode)
        {
            $this.Opcodes.Add($item.Symbol, $item.Name)
        }

        $this.Channels = [Dictionary[string, string]]::new()
        foreach ($item in $man.assembly.instrumentation.events.provider.channels.channel)
        {
            $this.Channels.Add($item.Symbol, $item.Type)
        }

        $this.Events = [Dictionary[int, EventMessage]]::new()
        foreach ($event in $man.assembly.instrumentation.events.provider.events.event)
        {
            [EventEntry] $eventEntry = [EventEntry]::new($event)
            $eventEntry.EventMessage = $messageTable[$eventEntry.MessageReference]
            $this.Events.Add($eventEntry.EventId, $eventEntry)
        }

        # NOTE: Build the final message dictionary.
        # $messageTable contains all strings defined in the manifest but not all are needed.
        # Some are for tasks, opcodes, channels, etc., and some events reference the same
        # message.
        $this.Messages = [Dictionary[int, EventMessage]]::new()
        foreach ($event in $this.Events.Values)
        {
            $eventMessage = $event.EventMessage
            if (!$this.Messages.ContainsKey($eventMessage.EventReference))
            {
                $this.Messages.Add($eventMessage.EventReference, $eventMessage)
            }
        }
    }
}

function New-ResourceCode
{
    param
    (
        [Parameter(Mandatory)]
        [ValidateNotNull()]
        [Manifest] $manifest,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $namespaceName,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $className
    )
    $sb = [System.Text.StringBuilder]::new()
    $null = $sb.AppendFormat($codePrologue, $namespaceName, $className)
    foreach ($eventEntry in $manifest.Events.Values)
    {
        $null = $sb.AppendFormat($codeEventEntryTemplate, $className, $eventEntry.EventId, $eventEntry.EventMessage.Id, $eventEntry.EventMessage.ParameterCount)
    }
    $null = $sb.Append($codeEpilogue)
    $code = $sb.ToString().Replace('}}', '}')
    return $code
}

<#
.SYNOPSIS
    Creates a resx file containing the messages from a manifest

.PARAMETER messages
    The EventMessage hash table containing the manifest messages
#>
function New-Resx
{
    param
    (
        [Manifest] $manifest
    )
    $messages = $manifest.Messages
    $sb = [System.Text.StringBuilder]::new()

    $null = $sb.Append($resxPrologue)
    foreach ($message in $messages.Values)
    {
        $null = $sb.AppendFormat($resxEntryTemplate, $message.Id, $message.Value)
    }
    $null = $sb.Append($resxEpilogue)
    return $sb.ToString()
}

<#
.SYNOPSIS
    Generates a resx file and code file from an ETW manifest.

.PARAMETER Manifest
    The path to the ETW manifest file to read.

.PARAMETER Name
    The name to use for the C# class, the code file, and the resx file.
    The default value is EventResource.

.PARAMETER Namespace
    The namespace to place the C# class.
    The default is System.Management.Automation.Tracing.

.PARAMETER ResxPath
    The path to the directory to use to create the resx file.

.PARAMETER CodePath
    The path to the directory to use to create the C# code file.
#>
function ConvertTo-Resx
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $Manifest,

        [string] $Name = 'EventResource',

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $Namespace = 'System.Management.Automation.Tracing',

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $ResxPath,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $CodePath
    )

    [Manifest] $etwmanifest = [Manifest]::new($Manifest)

    $resxFileName = Join-Path -Path $ResxPath -ChildPath "$($Name).resx"
    Write-Verbose -Message "Creating the resx file: $resxFileName" -Verbose

    $resx = New-Resx -manifest $etwmanifest
    $resx | Set-Content -Path $resxFileName -Encoding 'UTF8'

    $codeFileName = Join-Path -Path $CodePath -ChildPath "$($Name).cs"
    Write-Verbose -Message "Creating the C# file: $codeFileName" -Verbose

    $code = New-ResourceCode -manifest $etwmanifest -Namespace $Namespace -ClassName $Name
    $code | Set-Content -Path $codeFileName -Encoding 'UTF8'
}

Export-ModuleMember -Function ConvertTo-Resx
