@{
GUID="8B2D3C45-6E7F-4A8B-9C1D-2E3F4A5B6C7D"
Author="PowerShell"
CompanyName="Microsoft Corporation"
Copyright="Copyright (c) Microsoft Corporation."
ModuleVersion="1.0.0.0"
CompatiblePSEditions = @("Core")
PowerShellVersion="7.0"
FunctionsToExport = @()
CmdletsToExport=@(
    # Core Features
    "Get-ProjectContext",
    "Start-DevCommand",
    "Get-DevCommandStatus",
    "Wait-DevCommand",
    "Stop-DevCommand",
    "Receive-DevCommandOutput",
    "Register-CliTool",
    "Get-CliTool",
    "Unregister-CliTool",
    "Invoke-CliTool",
    "Format-ForAI",
    "Get-AIErrorContext",
    "Start-WorkflowRecording",
    "Stop-WorkflowRecording",
    "Save-WorkflowStep",
    "Get-Workflow",
    "Invoke-Workflow",
    "Remove-Workflow",
    "Get-TerminalSnapshot",
    "Get-CodeContext",
    "New-AIPrompt",
    # MCP Server
    "Start-MCPServer",
    "Stop-MCPServer",
    "Get-MCPServerStatus",
    # AI Response Parser
    "Convert-AIResponse",
    "Invoke-AISuggestions",
    # Session Replay
    "Start-SessionRecording",
    "Stop-SessionRecording",
    "Add-SessionMarker",
    "Add-SessionAnnotation",
    "Get-RecordedSession",
    "Invoke-SessionReplay",
    "Remove-RecordedSession",
    # Smart Suggestions
    "Get-SmartSuggestion",
    "Enable-SmartSuggestionLearning",
    "Clear-SmartSuggestionPatterns",
    "Get-SmartSuggestionStats",
    "Update-SmartSuggestionHistory",
    # Distributed Workflows
    "Register-RemoteTarget",
    "Get-RemoteTarget",
    "Unregister-RemoteTarget",
    "Test-RemoteTarget",
    "Invoke-DistributedWorkflow",
    "Invoke-RemoteCommand"
)
AliasesToExport = @(
    # Core Features
    "gpc",         # Get-ProjectContext
    "devcmd",      # Start-DevCommand
    "fai",         # Format-ForAI
    "gts",         # Get-TerminalSnapshot
    "snapshot",    # Get-TerminalSnapshot
    "gcc",         # Get-CodeContext
    "context",     # Get-CodeContext
    "prompt",      # New-AIPrompt
    "aiprompt",    # New-AIPrompt
    # AI Response Parser
    "parse-ai",    # Convert-AIResponse
    "aiparse",     # Convert-AIResponse
    "apply-ai",    # Invoke-AISuggestions
    "aiapply",     # Invoke-AISuggestions
    # Session Replay
    "rec",         # Start-SessionRecording
    "record",      # Start-SessionRecording
    "getsessions", # Get-RecordedSession
    "replay",      # Invoke-SessionReplay
    # Smart Suggestions
    "suggest",     # Get-SmartSuggestion
    "ss",          # Get-SmartSuggestion
    # Distributed Workflows
    "distflow",    # Invoke-DistributedWorkflow
    "remcmd"       # Invoke-RemoteCommand
)
NestedModules="Microsoft.PowerShell.Development.dll"
HelpInfoURI = 'https://aka.ms/powershell75-help'
Description = 'PowerShell cmdlets for AI-assisted software development and CLI tool integration'
}
