# Protocol description for PowerShell Script Tracing #

## OVERVIEW

Script Tracing is a new PowerShell feature that allows users to log content of all compiled and executed PowerShell scripts using ETW events.

This directory contains two OPN (Open Protocol Notation) files that describe the Script Tracing protocol. These files can be loaded by Message Analyzer,which will parse incoming ETW events and dispaly UI with a tree of executed scripts.

## INSTALLATION
1. Install Protocol Engineering Framework or Message Analyzer from http://toolbox/pef (you can also use external download).
2. If you installed the internal Protocol Engineering Framework, copy ScriptTracingApplication.opn and ScriptTracingRaw.opn to "C:\Program Files\Microsoft Protocol Engineering Framework\OpnAndConfiguration\Opns\Microsoft\Windows\Others"
3. If you installed just Message Analyzer, copy ScriptTracingApplication.opn and ScriptTracingRaw.opn to "C:\Program Files\Microsoft Message Analyzer\OpnAndConfiguration\Opns\Microsoft\Windows\Others".
   
If you don't want to alter default Message Analyzer Configuration, copy both OPN files to any folder and start Message Analyzer using with /OpnLoadPathMerge argument. For example:

cp ScriptTracingApplication.opn d:\Temp
cp ScriptTracingRaw.opn d:\Temp

& 'C:\Program Files\Microsoft Protocol Engineering Framework\MessageAnalyzer.exe' /OpnLoadPathMerge="d:\Temp"

## USAGE
1. Start Message Analyzer.
2. Choose "File" -> "New Session" -> "Blank Session".
3. In the dialog, choose "Add Source" -> "Live Trace".
4. In the "Add Provider" section, choose "Microsoft-Windows-PowerShell".
5. Click the "OK" button and close the dialog.
6. Click the "Start" button.
7. Execute some PowerShell scripts with enabled Script Tracing.
7. Finish the capture by clicking the "Stop" button.

You will see captured events in the grid. The will be from three modules:
* Etw - raw ETW messages from the PowerShell provider.
* ScriptTracingRaw - only merged Script Tracing messages (Script Block Compiled / Execution Started / Execution Ended).
* ScriptTracingApplication - the most interesting ones, that show executed PowerShell scripts in a form of tree.

In the grid, click the "Module" column to sort by module, and select one event from ScriptBlockApplication module. Below, in "Details" section, expand "RootScriptExecution". When you click on the "ScriptBlock" property, you will see content of the script in the "Field Data" section. The "NestedExecutions" property contains list of scripts executed from the current one.

## ADDITIONAL INFORMATION
The OPN language is hard... It has programming guide available [here](http://download.microsoft.com/download/3/E/8/3E845130-349C-4EFC-B634-C7DBD46140B7/OPN%20Programming%20Guide%20v1.docx) , but it dives into language syntax details before explaining high-level concepts, such as actors, protocols, endpoints and messages. OPN is not used much outside of Microsoft, so you cannot rely on StackOverflow topics or external tutorials.

The best source of information I found are protocols that ship with Message Analyzer. You can find them at "C:\Program Files\Microsoft Message Analyzer\OpnAndConfiguration\Opns" or "C:\Program Files\Microsoft Protocol Engineering Framework\OpnAndConfiguration\Opns". You will need to reverse-engineer the language from samples, but it's still easier than following the official documentation.