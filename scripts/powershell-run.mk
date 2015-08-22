# This is the makefile for all kinds of powershell hosts
#
# Currently there is powershell-run.exe, which is a very generic interactive and
# non-interactive host
#

POWERSHELL_RUN_FOLDER=../src/powershell-run
POWERSHELL_RUN_SRCS=$(addprefix $(POWERSHELL_RUN_FOLDER)/, main.cs host.cs ui.cs rawui.cs readline.cs powershell-run.assembly-info.cs)
POWERSHELL_SIMPLE_SRCS=$(addprefix $(POWERSHELL_RUN_FOLDER)/, powershell-simple.cs powershell-simple.assembly-info.cs)

# direct dependencies to be linked in
POWERSHELL_RUN_DEPS=dotnetlibs/System.Management.Automation.dll dotnetlibs/Microsoft.PowerShell.Commands.Management.dll dotnetlibs/$(ASSEMBLY_LOAD_CONTEXT_TARGET)
POWERSHELL_RUN_REFS=$(addprefix -r:,$(POWERSHELL_RUN_DEPS))

POWERSHELL_RUN_TARGETS=dotnetlibs/powershell-run.exe dotnetlibs/powershell-simple.exe dotnetlibs/libps.so

dotnetlibs/powershell-run.exe: $(POWERSHELL_RUN_SRCS) $(POWERSHELL_RUN_DEPS)
	    $(CSC) -out:$@ -noconfig -nostdlib -target:exe $(POWERSHELL_RUN_REFS) $(COREREF) $(POWERSHELL_RUN_SRCS)

dotnetlibs/powershell-simple.exe: $(POWERSHELL_SIMPLE_SRCS) $(POWERSHELL_RUN_DEPS)
	    $(CSC) -out:$@ -noconfig -nostdlib -target:exe $(POWERSHELL_RUN_REFS) $(COREREF) $(POWERSHELL_SIMPLE_SRCS)
