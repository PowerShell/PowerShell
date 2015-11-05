# xUnit tests for PowerShell
# - results in xunit-tests.xml
# - see https://xunit.github.io/
TEST_FOLDER=$(MONAD)/src/xunit-tests
TEST_SRCS=$(TEST_FOLDER)/test_*.cs
TEST_TARGETS=$(addprefix $(PSLIB)/, System.Management.Automation.dll Microsoft.PowerShell.Commands.Management.dll)

$(PSLIB)/PowerShell.Linux.Test.dll: $(TEST_SRCS) $(TEST_TARGETS)
	$(CSC) $(CSCOPTS_LIB) -out:$@ $(addprefix -r:$(MONAD_EXT)/xunit/, xunit.core.dll xunit.assert.dll) $(addprefix -r:, $(TEST_TARGETS)) $(COREREF) $(TEST_SRCS)

test: $(PSLIB)/PowerShell.Linux.Test.dll
	$(POWERSHELL_HOST) $(PSLIB)/xunit.console.netcore.exe $< -xml $(MONAD)/xunit-tests.xml
