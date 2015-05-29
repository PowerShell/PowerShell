
COMMANDS_MANAGEMENT_RES_SRCS = \
	../src/monad/monad/src/commands/management/resources/NavigationResources.resources    \
    ../src/monad/monad/src/commands/management/resources/CmdletizationResources.resources \
	../src/monad/monad/src/commands/management/resources/ProcessResources.resources

COMMANDS_MANAGEMENT_RESX_SRCS = \
	../src/monad/monad/src/commands/management/resources/NavigationResources.resx    \
    ../src/monad/monad/src/commands/management/resources/CmdletizationResources.resx \
	../src/monad/monad/src/commands/management/resources/ProcessResources.resx

COMMANDS_MANAGEMENT_RES_BASE_PATH = ../src/monad/monad/src/commands/management/resources

$(COMMANDS_MANAGEMENT_RES_BASE_PATH)/%.resources: $(COMMANDS_MANAGEMENT_RES_BASE_PATH)/%.resx
	resgen2 $<

COMMANDS_MANAGEMENT_RES_CS_SRCS = \
	commands-management-strings/NavigationResources.cs \
	commands-management-strings/CmdletizationResources.cs \
	commands-management-strings/ProcessResources.cs

COMMANDS_MANAGEMENT_SRCS = \
    ../src/monad/monad/src/commands/management/AddContentCommand.cs              \
    ../src/monad/monad/src/commands/management/ClearContentCommand.cs            \
    ../src/monad/monad/src/commands/management/ClearPropertyCommand.cs           \
    ../src/monad/monad/src/commands/management/CombinePathCommand.cs             \
    ../src/monad/monad/src/commands/management/CommandsCommon.cs                 \
    ../src/monad/monad/src/commands/management/ContentCommandBase.cs             \
    ../src/monad/monad/src/commands/management/ConvertPathCommand.cs             \
    ../src/monad/monad/src/commands/management/CopyPropertyCommand.cs            \
    ../src/monad/monad/src/commands/management/GetChildrenCommand.cs             \
    ../src/monad/monad/src/commands/management/GetContentCommand.cs              \
    ../src/monad/monad/src/commands/management/GetPropertyCommand.cs             \
    ../src/monad/monad/src/commands/management/MovePropertyCommand.cs            \
    ../src/monad/monad/src/commands/management/Navigation.cs                     \
    ../src/monad/monad/src/commands/management/NewPropertyCommand.cs             \
    ../src/monad/monad/src/commands/management/ParsePathCommand.cs               \
    ../src/monad/monad/src/commands/management/PassThroughContentCommandBase.cs  \
    ../src/monad/monad/src/commands/management/PassThroughPropertyCommandBase.cs \
    ../src/monad/monad/src/commands/management/PingPathCommand.cs                \
    ../src/monad/monad/src/commands/management/PropertyCommandBase.cs            \
    ../src/monad/monad/src/commands/management/RemovePropertyCommand.cs          \
    ../src/monad/monad/src/commands/management/RenamePropertyCommand.cs          \
    ../src/monad/monad/src/commands/management/ResolvePathCommand.cs             \
    ../src/monad/monad/src/commands/management/SetContentCommand.cs              \
    ../src/monad/monad/src/commands/management/SetPropertyCommand.cs             \
    ../src/monad/monad/src/commands/management/WriteContentCommandBase.cs        \
    ../src/monad/monad/src/commands/management/Process.cs                        \

