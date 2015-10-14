.PHONY: all

# this should go away and be automatically generated
include assembly-load-context.mk

# main references to the CoreCLR reference assemblies
MONAD_EXT=../src/monad-ext
TARGETING_PACK=$(MONAD_EXT)/coreclr/TargetingPack
COREREF=$(addprefix -r:, $(shell ls $(TARGETING_PACK)/*.dll))
CORECLR_ASSEMBLY_BASE=$(MONAD_EXT)/coreclr/Runtime

# powershell-run is the main powershell executable
include powershell-run.mk

# this variable is needed by module specific include makefiles below
ADMIN_GIT_ROOT=../src/monad

# these are automatically generated from the PowerShell build sytem
# - System.Management.Automation.dll (the main PS dll)
# - commands/modules (they contain cmdlets): management and utility
# - Microsoft.Management.Infrastructure.dll (the first dll in the remoting code paths)
include system-automation.mk
include commands-management.mk
include commands-utility.mk
include management-infrastructure.mk
include security.mk

MI_NATIVE_REF=-r:dotnetlibs/Microsoft.Management.Infrastructure.Native.dll
MI_REF_ASSEMBLY=-r:$(MONAD_EXT)/PS/PS_refs_modil/microsoft.management.infrastructure.metadata_dll
MI_ASSEMBLY=dotnetlibs/Microsoft.Management.Infrastructure.dll
MI_REF=-r:$(MI_ASSEMBLY)

PRODUCT_BASE_REFS=$(COREREF)
PRODUCT_MI_REFS=$(COREREF) $(MI_NATIVE_REF)
PRODUCT_PS_REFS=$(COREREF) $(MI_REF) -r:dotnetlibs/$(ASSEMBLY_LOAD_CONTEXT_TARGET)
PRODUCT_COMMANDS_REFS=$(COREREF) -r:dotnetlibs/System.Management.Automation.dll

# compilers
# - Roslyn's csc is used for all the PS code
# - Mono's mcs is used for build helper tools
CSC=mono buildtemp/Microsoft.Net.Compilers.1.0.0/tools/csc.exe
MCS=mcs

CSCOPTS_BASE=-noconfig -nostdlib
CSCOPTS_LIB=$(CSCOPTS_BASE) -target:library
CSCOPTS_LIB_PS=$(CSCOPTS_LIB) -unsafe -define:CORECLR -define:_CORECLR /nowarn:CS1701,CS1702

RUN_TARGETS=$(POWERSHELL_RUN_TARGETS) $(addprefix dotnetlibs/, Microsoft.PowerShell.Commands.Management.dll Microsoft.PowerShell.Commands.Utility.dll Microsoft.PowerShell.Security.dll api-ms-win-core-registry-l1-1-0.dll host_cmdline)

all: dotnetlibs/System.Management.Automation.dll $(RUN_TARGETS) dotnetlibs/$(ASSEMBLY_LOAD_CONTEXT_TARGET)

# this is the build rule for SMA.dll
dotnetlibs/System.Management.Automation.dll: prepare $(SYS_AUTO_SRCS) dotnetlibs/Microsoft.Management.Infrastructure.dll ../src/assembly-info/System.Management.Automation.assembly-info.cs dotnetlibs/$(ASSEMBLY_LOAD_CONTEXT_TARGET) $(SYS_AUTO_RES_SRCS) $(SYS_AUTO_RES_CS_SRCS)
	$(CSC) -out:$@ $(CSCOPTS_LIB_PS) $(PRODUCT_PS_REFS) $(SYS_AUTO_SRCS) $(SYS_AUTO_RES_REF) $(SYS_AUTO_RES_CS_SRCS) ../src/assembly-info/System.Management.Automation.assembly-info.cs

# this is the build rule for MMI.dll
dotnetlibs/Microsoft.Management.Infrastructure.dll: prepare $(MAN_INFRA_SRCS) dotnetlibs/Microsoft.Management.Infrastructure.Native.dll ../src/assembly-info/Microsoft.Management.Infrastructure.assembly-info.cs $(MAN_INFRA_RES_SRCS) $(MAN_INFRA_RES_CS_SRCS)
	$(CSC) -out:$@ $(CSCOPTS_LIB_PS) $(PRODUCT_MI_REFS) $(MAN_INFRA_SRCS) $(MAN_INFRA_RES_REF) $(MAN_INFRA_RES_CS_SRCS) ../src/assembly-info/Microsoft.Management.Infrastructure.assembly-info.cs

# Commands

dotnetlibs/Microsoft.PowerShell.Commands.Management.dll: prepare $(COMMANDS_MANAGEMENT_SRCS) dotnetlibs/System.Management.Automation.dll dotnetlibs/Microsoft.PowerShell.Security.dll $(COMMANDS_MANAGEMENT_RES_SRCS) $(COMMANDS_MANAGEMENT_RES_CS_SRCS) $(MI_ASSEMBLY)
	$(CSC) -out:$@ $(CSCOPTS_LIB_PS) $(PRODUCT_COMMANDS_REFS) $(COMMANDS_MANAGEMENT_SRCS) $(COMMANDS_MANAGEMENT_RES_CS_SRCS) $(COMMANDS_MANAGEMENT_RES_REF) $(MI_REF) -r:dotnetlibs/Microsoft.PowerShell.Security.dll

dotnetlibs/Microsoft.PowerShell.Commands.Utility.dll: prepare $(COMMANDS_UTILITY_SRCS) dotnetlibs/System.Management.Automation.dll $(COMMANDS_UTILITY_RES_SRCS) $(COMMANDS_UTILITY_RES_CS_SRCS)
	$(CSC) -out:$@ $(CSCOPTS_LIB_PS) $(PRODUCT_COMMANDS_REFS) $(COMMANDS_UTILITY_SRCS) $(COMMANDS_UTILITY_RES_CS_SRCS) $(COMMANDS_UTILITY_RES_REF)

dotnetlibs/Microsoft.PowerShell.Security.dll: prepare $(SECURITY_SRCS) $(SECURITY_RES_SRCS) $(SECURITY_RES_CS_SRCS)
	$(CSC) -out:$@ $(CSCOPTS_LIB_PS) $(PRODUCT_COMMANDS_REFS) $(SECURITY_SRCS) $(SECURITY_RES_CS_SRCS) $(SECURITY_RES_REF)

# assembly load context

dotnetlibs/$(ASSEMBLY_LOAD_CONTEXT_TARGET): prepare $(ASSEMBLY_LOAD_CONTEXT_SRCS)
	$(CSC) -out:$@ $(CSCOPTS_LIB_PS) $(PRODUCT_BASE_REFS) $(ASSEMBLY_LOAD_CONTEXT_SRCS)

# this one is built from stubs
MI_STUBS=../src/stubs/Microsoft.Management.Infrastructure.Native-stub.cs ../src/stubs/Microsoft.Management.Infrastructure.Native-stub-assembly-info.cs
dotnetlibs/Microsoft.Management.Infrastructure.Native.dll: prepare $(MI_STUBS)
	$(CSC) -out:$@ $(CSCOPTS_LIB_PS) $(PRODUCT_BASE_REFS) $(MI_STUBS)

# this creates the type catalog generator
MPATH=/usr/lib/mono/4.5/Facades
REFLECTION_METADATA=buildtemp/System.Reflection.Metadata.1.0.22/lib/dotnet/System.Reflection.Metadata.dll
COLLECTIONS_IMMUTABLE=buildtemp/System.Collections.Immutable.1.1.37/lib/dotnet/System.Collections.Immutable.dll
buildtemp/TypeCatalogGen.exe: ../src/monad/monad/nttargets/assemblies/core/PSAssemblyLoadContext/TypeCatalogGen/TypeCatalogGen.cs $(REFLECTION_METADATA) $(COLLECTIONS_IMMUTABLE)
	$(MCS) -out:$@ -target:exe -pkg:dotnet $(addprefix -r:, $(REFLECTION_METADATA) $(COLLECTIONS_IMMUTABLE) $(MPATH)/System.Runtime.dll $(MPATH)/System.Reflection.Primitives.dll $(MPATH)/System.IO.dll) $<

# this generates the necessary file of CoreCLR references that is an artifact of the Windows build process
# since we don't have Make 4.1, we can't use $(file), and using @echo makes the escaping tricky
# the list of references MUST be line-by-line, to match '^\$\(PS_PROFILE_REF_PATH\)\\(.+);\s*\\$'
REFERENCE_ASSEMBLIES=$(notdir $(shell ls $(TARGETING_PACK)/*.dll))
PROFILE_REFERENCES=$(addsuffix ;\\\n, $(addprefix $$(PS_PROFILE_REF_PATH)\, $(REFERENCE_ASSEMBLIES)))
powershell.inc:
	@echo 'PROFILE_REF_PATH=$$(SDK_REF_PATH)\Profiles' > $@
	@echo 'PS_PROFILE_REF_PATH=$$(PROFILE_REF_PATH)\PowerShell' >> $@
	@echo 'PROFILE_REFERENCES=\\' >> $@
	@echo '$(PROFILE_REFERENCES)' >> $@

# generate the Core PS type catalog
# this comes from: ../src/monad/monad/nttargets/assemblies/core/PSAssemblyLoadContext/makefile.inc
CorePsTypeCatalog.cs: powershell.inc buildtemp/TypeCatalogGen.exe
	MONO_PATH=$(dir $(REFLECTION_METADATA)):$(dir $(COLLECTIONS_IMMUTABLE)) mono buildtemp/TypeCatalogGen.exe $< $@ $(MONAD_EXT)/coreclr/TargetingPack

# the native library, libps.so
LIBPS_BUILD=buildtemp/libps-build
$(LIBPS_BUILD)/Makefile: ../src/monad-native/src/CMakeLists.txt
	mkdir -p $(LIBPS_BUILD)
	# cached files can change the path to be incorrect, this must be cleaned
	rm -f ../src/monad-native/src/CMakeCache.txt
	rm -f ../src/monad-native/src/cmake_install.cmake 
	cd $(LIBPS_BUILD) && cmake ../../../src/monad-native/src/

NATIVE_BINARIES=libps.so monad_native host_cmdline
$(addprefix dotnetlibs/, $(NATIVE_BINARIES)): $(LIBPS_BUILD)/Makefile
	$(MAKE) -j -C $(LIBPS_BUILD)
	mkdir -p dotnetlibs
	cp $(addprefix $(LIBPS_BUILD)/, $(notdir $@)) dotnetlibs

# C++ tests for monad-native - results in native-tests.xml
# see https://github.com/google/googletest
native-tests: dotnetlibs/monad_native
	cd dotnetlibs && ./monad_native --gtest_output="xml:../native-tests.xml"

# this is a windows dll that is needed because CoreCLR tries to access
# registry functions that don't exist on Linux and there is no other good
# way of fixing this right now
# (TODO linux: this should be removed by addressing it in CoreCLR)
dotnetlibs/api-ms-win-core-registry-l1-1-0.dll: ../src/win-dll/lib-api-ms-win-core-registry-l1-1-0.c
	gcc -o $@ -fPIC -shared -Wall $^

nuget.exe:
	wget 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe'

buildtemp dotnetlibs:
	mkdir -p $@

prepare: nuget.exe buildtemp dotnetlibs
	mono nuget.exe restore -PackagesDirectory buildtemp

# this is the execution environment from which all managed code is run
APP_BASE=exec_env/app_base

# this is an internal target, it's not intended to be called manually
#
# it will:
# - create necessary directories for deployment layout
# - copy all the PowerShell stuff into PowerShell's app_base
# - copy the custom host and other scripts into PowerShell's app_base
# - copy PS modules
#
# This rule does not copy coreclr, because the selection of debug or
# release CoreCLR depends on the actual run target.
internal-prepare-exec_env: runps.sh $(POWERSHELL_RUN_TARGETS)
	rm -rf exec_env
	mkdir -p $(APP_BASE)/Modules
	cp ../src/monad/monad/miscfiles/display/*.ps1xml $(APP_BASE)
	cp ../src/monad/monad/miscfiles/types/CoreClr/*.ps1xml $(APP_BASE)
	cp -r ../src/monad/monad/miscfiles/modules/* $(APP_BASE)/Modules
	cp -r dotnetlibs/*.dll $(APP_BASE)
	cp -r dotnetlibs/*.exe $(APP_BASE)
	cp -r dotnetlibs/*.so $(APP_BASE)
	cp dotnetlibs/host_cmdline $(APP_BASE)
	cp -r ../ext-src/pester $(APP_BASE)/Modules/Pester
	cp runps*.sh $(APP_BASE)

internal-prepare-clr:
	mkdir -p exec_env/coreclr
	cp -r $(CORECLR_ASSEMBLY_BASE)/* exec_env/coreclr/

run: $(RUN_TARGETS) internal-prepare-exec_env internal-prepare-clr
	# execute a cmdlet, this will auto-load the utility module and print a, b and c in 3 lines
	$(APP_BASE)/runps-simple.sh '"a","b","c","a","a" | Select-Object -Unique'

shell: $(RUN_TARGETS) internal-prepare-exec_env internal-prepare-clr
	$(APP_BASE)/runps.sh

run-interactive: shell

run-file: $(RUN_TARGETS) internal-prepare-exec_env internal-prepare-clr
	$(APP_BASE)/runps.sh --file $(PSSCRIPT)

# easy way to run individual PowerShell scripts, `make script.ps1` where the path is relative to monad-linux/scripts (with TEMP set for Pester)
%.ps1: $(RUN_TARGETS) internal-prepare-exec_env internal-prepare-clr
	TEMP=/tmp $(APP_BASE)/runps.sh --file ../../$@

# compiles "Hello World" like executables using .NET Core
%.exe: %.cs prepare internal-prepare-clr
	$(CSC) -out:$@ $(CSCOPTS_BASE) -target:exe $(COREREF) $<
	@echo run with monad-tty ./exec_env/coreclr/corerun ./$@

run-debugclr: $(RUN_TARGETS) internal-prepare-exec_env internal-prepare-clr
	PAL_DBG_CHANNELS="+LOADER.TRACE" $(APP_BASE)/runps-simple.sh get-location

# xUnit tests for PowerShell - results in xunit-tests.xml
# see https://xunit.github.io/
TEST_FOLDER=../src/ps_test
TEST_SRCS=$(addprefix $(TEST_FOLDER)/, test_*.cs)

$(APP_BASE)/xunit%: $(MONAD_EXT)/xunit/xunit%
	cp -f $^ $@

$(APP_BASE)/ps_test.dll: prepare $(TEST_SRCS) $(addprefix $(APP_BASE)/, xunit.core.dll xunit.assert.dll) $(addprefix dotnetlibs/, System.Management.Automation.dll Microsoft.PowerShell.Commands.Management.dll $(ASSEMBLY_LOAD_CONTEXT_TARGET))
	$(CSC) $(CSCOPTS_LIB) -out:$@ $(addprefix -r:$(APP_BASE)/, xunit.core.dll xunit.assert.dll) $(addprefix -r:dotnetlibs/, System.Management.Automation.dll $(ASSEMBLY_LOAD_CONTEXT_TARGET)) $(COREREF) $(TEST_SRCS)

xunit-tests: $(RUN_TARGETS) internal-prepare-exec_env internal-prepare-clr $(addprefix $(APP_BASE)/, ps_test.dll xunit.console.netcore.exe xunit.runner.utility.dll xunit.abstractions.dll xunit.execution.dll)
	$(APP_BASE)/runps-test.sh ps_test.dll -xml ../../xunit-tests.xml

# Pester tests for PowerShell - results in pester-tests.xml
# see https://github.com/pester/Pester
# requires $TEMP to be set
pester-tests: $(RUN_TARGETS) internal-prepare-exec_env internal-prepare-clr
	$(APP_BASE)/runps-simple.sh 'cd ../../../src/pester-tests; $$env:TEMP="/tmp"; invoke-pester -OutputFile ../../scripts/pester-tests.xml -OutputFormat NUnitXml'

hashbang-tests: all
	# execute the 3rdparty/hashbang example
	PATH=$(PATH):$(shell pwd)/$(APP_BASE) $(shell pwd)/3rdparty/hashbang/script.ps1

test: native-tests xunit-tests pester-tests hashbang-tests

trace: $(RUN_TARGETS) internal-prepare-exec_env internal-prepare-clr
	$(APP_BASE)/runps-simple-trace.sh get-location

debug: $(RUN_TARGETS) internal-prepare-exec_env internal-prepare-clr
	# quoting here is a bit special if strings are passed in, because lldb seems to forward arguments strangely
	$(APP_BASE)/runps-simple-debug.sh get-location

# clean native library, libps
clean-native:
	rm -rf buildtemp/libps-build

clean:
	rm -rf dotnetlibs exec_env powershell.inc

# clean built stuff + prepare step
cleanall: clean clean-native
	rm -rf buildtemp *tests.xml

docker-build:
	docker build --no-cache=false -t image_ps .
	$(eval INSTANCE_ID := $(shell docker inspect -f '{{.Id}}' image_ps))

docker-run: docker-build
	# docker port is 4201 for shellinabox by default, but can be overridden
	$(eval SHELLINABOX_PORT := $(shell if [ "${SHELLINABOX_PORT}" = "" ]; then echo 4201; else echo ${SHELLINABOX_PORT}; fi))
	@echo "SHELLINABOX_PORT=${SHELLINABOX_PORT}"
	docker run -d -P -p $(SHELLINABOX_PORT):4201 --name test_ps image_ps
	# those two ports are container ports, so they can be hard-coded
	docker port test_ps 22
	docker port test_ps 4201
	$(eval HOST_IP := $(shell ifconfig eth0 | awk '/inet addr/{print substr($$2,6)}'))
	@echo "connect to docker at: http://${HOST_IP}:${SHELLINABOX_PORT}/"
	@echo "use username: 'test1' and password: 'pass' to connect"

docker-stop:
	docker stop test_ps
	docker rm test_ps
