
COMMANDS_UTILITY_RES_SRCS = \
    ../src/monad/monad/src/commands/utility/resources/CsvCommandStrings.resources      \
    ../src/monad/monad/src/commands/utility/resources/Debugger.resources               \
    ../src/monad/monad/src/commands/utility/resources/EventingStrings.resources        \
    ../src/monad/monad/src/commands/utility/resources/NewObjectStrings.resources       \
    ../src/monad/monad/src/commands/utility/resources/MeasureObjectStrings.resources   \
    ../src/monad/monad/src/commands/utility/resources/SelectObjectStrings.resources    \
    ../src/monad/monad/src/commands/utility/resources/SortObjectStrings.resources      \
    ../src/monad/monad/src/commands/utility/resources/WriteErrorStrings.resources      \
    ../src/monad/monad/src/commands/utility/resources/VariableCommandStrings.resources \
    ../src/monad/monad/src/commands/utility/resources/AddTypeStrings.resources         \
    ../src/monad/monad/src/commands/utility/resources/GetMember.resources              \
    ../src/monad/monad/src/commands/utility/resources/GetRandomCommandStrings.resources\
    ../src/monad/monad/src/commands/utility/resources/UtilityCommonStrings.resources   \
    ../src/monad/monad/src/commands/utility/resources/HostStrings.resources            \
    ../src/monad/monad/src/commands/utility/resources/AddMember.resources              \
    ../src/monad/monad/src/commands/utility/resources/ConvertFromStringData.resources  \
    ../src/monad/monad/src/commands/utility/resources/ImportLocalizedDataStrings.resources    \
    ../src/monad/monad/src/commands/utility/resources/WriteProgressResourceStrings.resources  \
    ../src/monad/monad/src/commands/utility/resources/AliasCommandStrings.resources

COMMANDS_UTILITY_RESX_SRCS = \
    ../src/monad/monad/src/commands/utility/resources/CsvCommandStrings.resx      \
    ../src/monad/monad/src/commands/utility/resources/Debugger.resx               \
    ../src/monad/monad/src/commands/utility/resources/EventingStrings.resx        \
    ../src/monad/monad/src/commands/utility/resources/NewObjectStrings.resx       \
    ../src/monad/monad/src/commands/utility/resources/MeasureObjectStrings.resx   \
    ../src/monad/monad/src/commands/utility/resources/SelectObjectStrings.resx    \
    ../src/monad/monad/src/commands/utility/resources/SortObjectStrings.resx      \
    ../src/monad/monad/src/commands/utility/resources/WriteErrorStrings.resx      \
    ../src/monad/monad/src/commands/utility/resources/VariableCommandStrings.resx \
    ../src/monad/monad/src/commands/utility/resources/AddTypeStrings.resx         \
    ../src/monad/monad/src/commands/utility/resources/GetMember.resx              \
    ../src/monad/monad/src/commands/utility/resources/GetRandomCommandStrings.resx\
    ../src/monad/monad/src/commands/utility/resources/UtilityCommonStrings.resx   \
    ../src/monad/monad/src/commands/utility/resources/HostStrings.resx            \
    ../src/monad/monad/src/commands/utility/resources/AddMember.resx              \
    ../src/monad/monad/src/commands/utility/resources/ConvertFromStringData.resx  \
    ../src/monad/monad/src/commands/utility/resources/ImportLocalizedDataStrings.resx    \
    ../src/monad/monad/src/commands/utility/resources/WriteProgressResourceStrings.resx  \
    ../src/monad/monad/src/commands/utility/resources/AliasCommandStrings.resources


COMMANDS_UTILITY_RES_BASE_PATH = ../src/monad/monad/src/commands/utility/resources

$(COMMANDS_UTILITY_RES_BASE_PATH)/%.resources: $(COMMANDS_UTILITY_RES_BASE_PATH)/%.resx
	resgen2 $<

COMMANDS_UTILITY_RES_CS_SRCS = \
    commands-utility-strings/CsvCommandStrings.cs        \
    commands-utility-strings/Debugger.cs                 \
    commands-utility-strings/EventingStrings.cs          \
    commands-utility-strings/NewObjectStrings.cs         \
    commands-utility-strings/MeasureObjectStrings.cs     \
    commands-utility-strings/SelectObjectStrings.cs      \
    commands-utility-strings/SortObjectStrings.cs        \
    commands-utility-strings/WriteErrorStrings.cs        \
    commands-utility-strings/VariableCommandStrings.cs   \
    commands-utility-strings/GetMember.cs                \
    commands-utility-strings/GetRandomCommandStrings.cs  \
    commands-utility-strings/UtilityCommonStrings.cs     \
    commands-utility-strings/HostStrings.cs              \
    commands-utility-strings/AddMember.cs                \
    commands-utility-strings/AliasCommandStrings.cs      \
    commands-utility-strings/ConvertFromStringData.cs    \
    commands-utility-strings/ImportLocalizedDataStrings.cs    \
    commands-utility-strings/WriteProgressResourceStrings.cs
 
SOURCES_PATH=../src/monad/monad/src/commands/utility
FORMAT_AND_OUT_SOURCES_PATH=$(SOURCES_PATH)/FormatAndOutput

FORMAT_AND_OUT_SOURCES=\
    $(FORMAT_AND_OUT_SOURCES_PATH)/common/GetFormatDataCommand.cs        \
    $(FORMAT_AND_OUT_SOURCES_PATH)/common/WriteFormatDataCommand.cs      \
    $(FORMAT_AND_OUT_SOURCES_PATH)/format-list/Format-List.cs            \
    $(FORMAT_AND_OUT_SOURCES_PATH)/format-object/format-object.cs        \
    $(FORMAT_AND_OUT_SOURCES_PATH)/format-table/Format-Table.cs          \
    $(FORMAT_AND_OUT_SOURCES_PATH)/format-wide/Format-Wide.cs            \
    $(FORMAT_AND_OUT_SOURCES_PATH)/out-file/Out-File.cs                  \
    $(FORMAT_AND_OUT_SOURCES_PATH)/out-string/out-string.cs              

EVENTING_SOURCES=\
    $(SOURCES_PATH)/RegisterObjectEventCommand.cs     \
    $(SOURCES_PATH)/RegisterPSEventCommand.cs         \
    $(SOURCES_PATH)/WaitEventCommand.cs               \
    $(SOURCES_PATH)/GetEventCommand.cs                \
    $(SOURCES_PATH)/RemoveEventCommand.cs             \
    $(SOURCES_PATH)/GetEventSubscriberCommand.cs      \
    $(SOURCES_PATH)/UnregisterEventCommand.cs         \
    $(SOURCES_PATH)/neweventcommand.cs                

COMMANDS_UTILITY_SRCS=\
    $(SOURCES_PATH)/new-object.cs                  \
    $(SOURCES_PATH)/Measure-Object.cs              \
    $(SOURCES_PATH)/select-object.cs               \
    $(SOURCES_PATH)/sort-object.cs                 \
    $(SOURCES_PATH)/ObjectCommandComparer.cs       \
    $(SOURCES_PATH)/OrderObjectBase.cs             \
    $(SOURCES_PATH)/write.cs                       \
    $(SOURCES_PATH)/Var.cs                         \
    $(SOURCES_PATH)/GetMember.cs                   \
    $(SOURCES_PATH)/group-object.cs                \
    $(SOURCES_PATH)/WriteConsoleCmdlet.cs          \
    $(SOURCES_PATH)/ConsoleColorCmdlet.cs          \
    $(SOURCES_PATH)/AddMember.cs                   \
    $(SOURCES_PATH)/Write-Object.cs                \
    $(SOURCES_PATH)/StartSleepCommand.cs           \
    $(SOURCES_PATH)/Get-PSCallStack.cs             \
    $(SOURCES_PATH)/GetUnique.cs                   \
    $(SOURCES_PATH)/GetDateCommand.cs              \
    $(SOURCES_PATH)/compare-object.cs              \
    $(SOURCES_PATH)/GetHostCmdlet.cs               \
    $(SOURCES_PATH)/GetRandomCommand.cs            \
    $(SOURCES_PATH)/InvokeCommandCmdlet.cs         \
    $(SOURCES_PATH)/NewTimeSpanCommand.cs          \
    $(SOURCES_PATH)/tee-object.cs                  \
    $(SOURCES_PATH)/TimeExpressionCommand.cs       \
    $(SOURCES_PATH)/UtilityCommon.cs               \
    $(SOURCES_PATH)/SetAliasCommand.cs             \
    $(SOURCES_PATH)/GetAliasCommand.cs             \
    $(SOURCES_PATH)/NewAliasCommand.cs             \
    $(SOURCES_PATH)/WriteAliasCommandBase.cs       \
    $(SOURCES_PATH)/ExportAliasCommand.cs          \
    $(SOURCES_PATH)/ImportAliasCommand.cs          \
    $(SOURCES_PATH)/Import-LocalizedData.cs        \
    $(SOURCES_PATH)/ConvertFrom-StringData.cs      \
    $(SOURCES_PATH)/ReadConsoleCmdlet.cs           \
    $(SOURCES_PATH)/Csv.cs                         \
    $(SOURCES_PATH)/CSVCommands.cs                 \
    $(SOURCES_PATH)/Set-PSBreakpoint.cs            \
    $(SOURCES_PATH)/Get-PSBreakpoint.cs            \
    $(SOURCES_PATH)/Remove-PSBreakpoint.cs         \
    $(SOURCES_PATH)/Enable-PSBreakpoint.cs         \
    $(SOURCES_PATH)/Disable-PSBreakpoint.cs        \
    $(SOURCES_PATH)/DebugRunspaceCommand.cs        \
    $(SOURCES_PATH)/GetRunspaceCommand.cs          \
    $(SOURCES_PATH)/EnableDisableRunspaceDebugCommand.cs \
    $(SOURCES_PATH)/WriteProgressCmdlet.cs         \
    $(FORMAT_AND_OUT_SOURCES)                      \
    $(EVENTING_SOURCES)                            
