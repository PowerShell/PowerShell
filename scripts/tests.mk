TEST_FOLDER=../src/ps_test
TESTRUN_FOLDER=exec_env/app_base
TEST_SRCS=$(addprefix $(TEST_FOLDER)/, test_*.cs)

$(TESTRUN_FOLDER)/xunit%: $(MONAD_EXT)/xunit/xunit%
	cp -f $^ $@

$(TESTRUN_FOLDER)/ps_test.dll: $(TEST_SRCS) $(addprefix $(TESTRUN_FOLDER)/, xunit.core.dll xunit.assert.dll) $(addprefix dotnetlibs/, System.Management.Automation.dll Microsoft.PowerShell.Commands.Management.dll $(ASSEMBLY_LOAD_CONTEXT_TARGET))
	$(CSC) -out:$@ -noconfig -nostdlib -target:library $(addprefix -r:$(TESTRUN_FOLDER)/, xunit.core.dll xunit.assert.dll) $(addprefix -r:dotnetlibs/, System.Management.Automation.dll $(ASSEMBLY_LOAD_CONTEXT_TARGET)) ${COREREF} $(TEST_SRCS)
