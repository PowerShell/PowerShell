TEST_FOLDER=../src/ps_test
TEST_SRCS=$(addprefix $(TEST_FOLDER)/, test_*.cs)

dotnetlibs/xunit%: $(MONAD_EXT)/xunit/xunit%
	cp -f $^ $@

dotnetlibs/ps_test.dll: $(TEST_SRCS) $(addprefix dotnetlibs/, xunit.core.dll xunit.assert.dll System.Management.Automation.dll Microsoft.PowerShell.Commands.Management.dll $(ASSEMBLY_LOAD_CONTEXT_TARGET))
	$(CSC) -out:$@ -noconfig -nostdlib -target:library $(addprefix -r:dotnetlibs/, System.Management.Automation.dll $(ASSEMBLY_LOAD_CONTEXT_TARGET) xunit.core.dll xunit.assert.dll) ${COREREF} $(TEST_SRCS)

dotnetlibs/ps_test_runner.exe: $(TEST_FOLDER)/ps_test.cs $(addprefix dotnetlibs/, ps_test.dll System.Management.Automation.dll Microsoft.PowerShell.Commands.Management.dll $(ASSEMBLY_LOAD_CONTEXT_TARGET) xunit.core.dll xunit.assert.dll)
	$(CSC) -out:$@ -noconfig -nostdlib -target:exe $(addprefix -r:dotnetlibs/, ps_test.dll System.Management.Automation.dll $(ASSEMBLY_LOAD_CONTEXT_TARGET) xunit.core.dll xunit.assert.dll) ${COREREF} $<

xunit: $(addprefix dotnetlibs/, ps_test.dll corerun xunit.console.netcore.exe xunit.runner.utility.dll xunit.abstractions.dll xunit.execution.dll) internal-prepare-exec_env
	cd exec_env/app_base && PSMODULEPATH=$(shell pwd)/exec_env/app_base/Modules LD_LIBRARY_PATH=. ./corerun xunit.console.netcore.exe ps_test.dll
