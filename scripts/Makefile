# global configuration needed by some of the included makefiles
PSRC = ../src/monad/monad/src
MONAD_EXT=../src/monad-ext

# this variable is needed by module specific include makefiles below
ADMIN_GIT_ROOT=../src/monad

phony_all: all

# include all the external makefiles

# these should go away with the makefile restructuring
include assembly-load-context.mk

# main references to the CoreCLR reference assemblies
include coreref.mk

# make file which adds things that are necessary for the platform we are building for
include platform.mk

# builds unit tests
include tests.mk

# powershell-run is the main powershell executable
include powershell-run.mk

# These are the main PS dlls:
# - System.Management.Automation.dll (the main PS dll)
# - commands/modules (they contain cmdlets): management and utility
# - Microsoft.Management.Infrastructure.dll (the first dll in the remoting code paths)
include system-automation.mk
include commands-management.mk
include commands-utility.mk
include management-infrastructure.mk
include security.mk

NUGETREF=-r:buildtemp/System.Reflection.Metadata.dll -r:buildtemp/System.Collections.Immutable.dll

MI_NATIVE_REF=-r:dotnetlibs/Microsoft.Management.Infrastructure.Native.dll
MI_REF_ASSEMBLY=-r:$(MONAD_EXT)/PS/PS_refs_modil/microsoft.management.infrastructure.metadata_dll
MI_ASSEMBLY=dotnetlibs/Microsoft.Management.Infrastructure.dll
MI_REF=-r:$(MI_ASSEMBLY)

PRODUCT_BASE_REFS=${COREREF}
PRODUCT_MI_REFS=${COREREF} ${MI_NATIVE_REF}
PRODUCT_PS_REFS=${COREREF} ${MI_REF} -r:dotnetlibs/$(ASSEMBLY_LOAD_CONTEXT_TARGET)
PRODUCT_COMMANDS_REFS=${COREREF} -r:dotnetlibs/System.Management.Automation.dll

MCSOPTS_BASE=-unsafe -nostdlib -noconfig -define:CORECLR -define:_CORECLR
MCSOPTS_MI=${MCSOPTS_BASE} -target:library
MCSOPTS_LIB=${MCSOPTS_BASE} -target:library
MCSOPTS_PS=${STRING_RESOURCES_ORIG} ${MCSOPTS_BASE} -target:library
SRCS_ALL=${STRING_RESOURCES} ${SRCS}

# compilers
# - Roslyn's csc is used for all the PS code
# - Mono's mcs is used for build helper tools
CSC=mono buildtemp/Microsoft.Net.ToolsetCompilers.*/tools/csc.exe
MCS=mcs

all: dotnetlibs/System.Management.Automation.dll $(POWERSHELL_RUN_TARGETS) dotnetlibs/Microsoft.PowerShell.Commands.Management.dll dotnetlibs/Microsoft.PowerShell.Commands.Utility.dll dotnetlibs/Microsoft.PowerShell.Security.dll dotnetlibs/$(ASSEMBLY_LOAD_CONTEXT_TARGET)

# this is the build rule for SMA.dll
dotnetlibs/System.Management.Automation.dll: ${SYS_AUTO_SRCS} dotnetlibs/Microsoft.Management.Infrastructure.dll ../src/assembly-info/System.Management.Automation.assembly-info.cs dotnetlibs/$(ASSEMBLY_LOAD_CONTEXT_TARGET) $(PLATFORM_SRCS) ${SYS_AUTO_RES_SRCS} ${SYS_AUTO_RES_CS_SRCS}
	$(CSC) -out:$@ ${MCSOPTS_LIB} ${PRODUCT_PS_REFS} ${SYS_AUTO_SRCS} ${SYS_AUTO_RES_REF} ${SYS_AUTO_RES_CS_SRCS} $(PLATFORM_SRCS) ../src/assembly-info/System.Management.Automation.assembly-info.cs

# this is the build rule for MMI.dll
dotnetlibs/Microsoft.Management.Infrastructure.dll: ${MAN_INFRA_SRCS} dotnetlibs/Microsoft.Management.Infrastructure.Native.dll ../src/assembly-info/Microsoft.Management.Infrastructure.assembly-info.cs $(MAN_INFRA_RES_SRCS) $(MAN_INFRA_RES_CS_SRCS)
	$(CSC) -out:$@ $(MCSOPTS_MI) $(PRODUCT_MI_REFS) $(MAN_INFRA_SRCS) $(MAN_INFRA_RES_REF) $(MAN_INFRA_RES_CS_SRCS) ../src/assembly-info/Microsoft.Management.Infrastructure.assembly-info.cs

# Commands

dotnetlibs/Microsoft.PowerShell.Commands.Management.dll: ${COMMANDS_MANAGEMENT_SRCS} dotnetlibs/System.Management.Automation.dll ${COMMANDS_MANAGEMENT_RES_SRCS} ${COMMANDS_MANAGEMENT_RES_CS_SRCS} $(MI_ASSEMBLY)
	$(CSC) -out:$@ ${MCSOPTS_LIB} ${PRODUCT_COMMANDS_REFS} ${COMMANDS_MANAGEMENT_SRCS} ${COMMANDS_MANAGEMENT_RES_CS_SRCS} $(COMMANDS_MANAGEMENT_RES_REF) $(MI_REF)

dotnetlibs/Microsoft.PowerShell.Commands.Utility.dll: ${COMMANDS_UTILITY_SRCS} dotnetlibs/System.Management.Automation.dll ${COMMANDS_UTILITY_RES_SRCS} ${COMMANDS_UTILITY_RES_CS_SRCS}
	$(CSC) -out:$@ ${MCSOPTS_LIB} ${PRODUCT_COMMANDS_REFS} ${COMMANDS_UTILITY_SRCS} ${COMMANDS_UTILITY_RES_CS_SRCS} $(COMMANDS_UTILITY_RES_REF)

dotnetlibs/Microsoft.PowerShell.Security.dll: ${SECURITY_SRCS} dotnetlibs/System.Management.Automation.dll ${SECURITY_RES_SRCS} ${SECURITY_RES_CS_SRCS}
	$(CSC) -out:$@ ${MCSOPTS_LIB} ${PRODUCT_COMMANDS_REFS} ${SECURITY_SRCS} ${SECURITY_RES_CS_SRCS} $(SECURITY_RES_REF)

# assembly load context

dotnetlibs/$(ASSEMBLY_LOAD_CONTEXT_TARGET): $(ASSEMBLY_LOAD_CONTEXT_SRCS)
	$(CSC) -out:$@ ${MCSOPTS_LIB} ${PRODUCT_BASE_REFS} $(ASSEMBLY_LOAD_CONTEXT_SRCS)

# this will copy whatever the first version of the dll in the globber is
buildtemp/System.Reflection.Metadata.dll: buildtemp/System.Reflection.Metadata.*/lib/portable-net45+win8/System.Reflection.Metadata.dll
	cp -f $< $@

# this will copy whatever the first version of the dll in the globber is
buildtemp/System.Collections.Immutable.dll: buildtemp/System.Collections.Immutable.*/lib/portable-net45+win8+wp8+wpa81/System.Collections.Immutable.dll
	cp -f $< $@

# this one is built from stubs
dotnetlibs/Microsoft.Management.Infrastructure.Native.dll: ../src/stubs/Microsoft.Management.Infrastructure.Native-stub.cs ../src/stubs/Microsoft.Management.Infrastructure.Native-stub-assembly-info.cs
	$(CSC) -out:$@ $(MCSOPTS_LIB) $(PRODUCT_BASE_REFS) $^

MPATH=/usr/lib/mono/4.5/Facades

buildtemp/TypeCatalogGen.exe: ../src/monad/monad/nttargets/assemblies/core/PSAssemblyLoadContext/TypeCatalogGen/TypeCatalogGen.cs buildtemp/System.Reflection.Metadata.dll buildtemp/System.Collections.Immutable.dll
	$(MCS) -out:$@ -target:exe ${NUGETREF} -pkg:dotnet -r:${MPATH}/System.Runtime.dll -r:${MPATH}/System.Reflection.Primitives.dll -r:${MPATH}/System.IO.dll $<

# generate the Core PS type catalog
# this comes from: ../src/monad/monad/nttargets/assemblies/core/PSAssemblyLoadContext/makefile.inc
CorePsTypeCatalog.cs: powershell-linux.inc buildtemp/TypeCatalogGen.exe buildtemp/System.Reflection.Metadata.dll buildtemp/System.Collections.Immutable.dll
	LD_LIBRARY_PATH=. mono buildtemp/TypeCatalogGen.exe powershell-linux.inc $@ $(MONAD_EXT)/coreclr/TargetingPack

# the pinvoke library libps.so

libps-build/Makefile: ../src/monad-native/src/CMakeLists.txt
	rm -rf libps-build
	mkdir libps-build
	# cached files can change the path to be incorrect, this must be cleaned
	rm -f ../src/monad-native/src/CMakeCache.txt
	rm -f ../src/monad-native/src/cmake_install.cmake 
	cd libps-build && cmake ../../src/monad-native/src/

dotnetlibs/libps.so dotnetlibs/monad_native: libps-build/Makefile
	cd libps-build && make VERBOSE=0 && cp monad_native ../dotnetlibs && cp libps.so ../dotnetlibs

libps.so-test: dotnetlibs/libps.so dotnetlibs/monad_native
	cd dotnetlibs && LD_LIBRARY_PATH=. ./monad_native

# this is a windows dll that is needed because CoreCLR tries to access
# registry functions that don't exist on Linux and there is no other good
# way of fixing this right now
# (TODO linux: this should be removed by addressing it in CoreCLR)
dotnetlibs/api-ms-win-core-registry-l1-1-0.dll: ../src/win-dll/lib-api-ms-win-core-registry-l1-1-0.c
	gcc -o $@ -fPIC -shared -Wall $^

# this is the rule to copy over updated CoreCLR + .net libraries
dotnetlibs/corerun: $(MONAD_EXT)/coreclr/Release/corerun
	cp -r $(MONAD_EXT)/coreclr/Release/* dotnetlibs

# this is the manual step that will install some stuff using nuget and do other things that can't be dependency
# tracked that easily
prepare:
	rm -rf buildtemp/System.Reflection.Metadata.* buildtemp/System.Collections.Immutable.* buildtemp/Microsoft.Net.ToolsetCompilers.* buildtemp/nuget.exe
	wget -O buildtemp/nuget.exe https://nuget.org/nuget.exe
	cd buildtemp && mono nuget.exe install -Version 1.0.21 System.Reflection.Metadata
	cd buildtemp && mono nuget.exe install -Version 1.1.36 System.Collections.Immutable
	cd buildtemp && mono nuget.exe install Microsoft.Net.ToolsetCompilers -pre

# this is an internal target, it's not intended to be called manually
internal-prepare-exec_env: runps.sh $(POWERSHELL_RUN_TARGETS) dotnetlibs/corerun
	rm -rf exec_env
	mkdir -p exec_env/app_base/Modules
	mkdir -p exec_env/testrun
	cp ../src/monad/monad/miscfiles/display/*.ps1xml exec_env/app_base
	cp ../src/monad/monad/miscfiles/types/CoreClr/*.ps1xml exec_env/app_base
	cp -r ../src/monad/monad/miscfiles/modules/* exec_env/app_base/Modules
	cp -r dotnetlibs/*.dll exec_env/app_base
	cp -r dotnetlibs/*.exe exec_env/app_base
	cp -r dotnetlibs/lib* exec_env/app_base
	cp -r dotnetlibs/corerun exec_env/app_base
	cp -r ../ext-src/pester exec_env/app_base/Modules/Pester
	cp runps.sh exec_env/app_base

RUN_TARGETS=$(POWERSHELL_RUN_TARGETS) dotnetlibs/corerun dotnetlibs/Microsoft.PowerShell.Commands.Management.dll dotnetlibs/Microsoft.PowerShell.Commands.Utility.dll dotnetlibs/Microsoft.PowerShell.Security.dll dotnetlibs/api-ms-win-core-registry-l1-1-0.dll

run: $(RUN_TARGETS) internal-prepare-exec_env
	# check if corerun is the right one (could be the debug version)
	if ! diff dotnetlibs/corerun $(MONAD_EXT)/coreclr/Release/corerun; then cp -r $(MONAD_EXT)/coreclr/Release/* dotnetlibs; fi
	# execute a cmdlet, this will auto-load the utility module and print a, b and c in 3 lines
	cd exec_env/app_base && PSMODULEPATH=$(shell pwd)/exec_env/app_base/Modules LD_LIBRARY_PATH=. ./corerun powershell-simple.exe '"a","b","c","a","a" | Select-Object -Unique'

run-interactive: $(POWERSHELL_RUN_TARGETS) dotnetlibs/corerun dotnetlibs/Microsoft.PowerShell.Commands.Management.dll dotnetlibs/Microsoft.PowerShell.Commands.Utility.dll dotnetlibs/Microsoft.PowerShell.Security.dll dotnetlibs/api-ms-win-core-registry-l1-1-0.dll internal-prepare-exec_env
	# check if corerun is the right one (could be the debug version)
	if ! diff dotnetlibs/corerun $(MONAD_EXT)/coreclr/Release/corerun; then cp -r $(MONAD_EXT)/coreclr/Release/* dotnetlibs; fi
	cd exec_env/app_base && ./runps.sh

run-file: $(POWERSHELL_RUN_TARGETS) dotnetlibs/corerun dotnetlibs/Microsoft.PowerShell.Commands.Management.dll dotnetlibs/Microsoft.PowerShell.Commands.Utility.dll dotnetlibs/api-ms-win-core-registry-l1-1-0.dll internal-prepare-exec_env
	# check if corerun is the right one (could be the debug version)
	if ! diff dotnetlibs/corerun $(MONAD_EXT)/coreclr/Release/corerun; then cp -r $(MONAD_EXT)/coreclr/Release/* dotnetlibs; fi
	cd exec_env/app_base && ./runps.sh --file $(PSSCRIPT)

pester-tests: $(POWERSHELL_RUN_TARGETS) dotnetlibs/corerun dotnetlibs/Microsoft.PowerShell.Commands.Management.dll dotnetlibs/Microsoft.PowerShell.Commands.Utility.dll dotnetlibs/Microsoft.PowerShell.Security.dll dotnetlibs/api-ms-win-core-registry-l1-1-0.dll internal-prepare-exec_env
	# check if corerun is the right one (could be the debug version)
	if ! diff dotnetlibs/corerun $(MONAD_EXT)/coreclr/Release/corerun; then cp -r $(MONAD_EXT)/coreclr/Release/* dotnetlibs; fi
	# execute the pester tests, pester needs a TEMP environment variable to be set
	cd exec_env/app_base && PSMODULEPATH=$(shell pwd)/exec_env/app_base/Modules LD_LIBRARY_PATH=. ./corerun powershell-simple.exe 'cd ../../../src/pester-tests; $$env:TEMP="/tmp"; invoke-pester'

test: $(RUN_TARGETS) internal-prepare-exec_env $(addprefix $(TESTRUN_FOLDER)/, ps_test.dll xunit.console.netcore.exe xunit.runner.utility.dll xunit.abstractions.dll xunit.execution.dll) dotnetlibs/corerun pester-tests
	cd exec_env/testrun && PSMODULEPATH=$(shell pwd)/exec_env/app_base/Modules LD_LIBRARY_PATH=../app_base ../app_base/corerun xunit.console.netcore.exe ps_test.dll -xml ../../xunittests.xml

# this is an internal target, it's not intended to be called manually
internal-prepare-debugclr:
	cp -r $(MONAD_EXT)/coreclr/Debug/* dotnetlibs

run-debugclr: dotnetlibs/powershell-run.exe internal-prepare-debugclr dotnetlibs/corerun dotnetlibs/Microsoft.PowerShell.Commands.Management.dll dotnetlibs/Microsoft.PowerShell.Commands.Utility.dll dotnetlibs/Microsoft.PowerShell.Security.dll dotnetlibs/api-ms-win-core-registry-l1-1-0.dll internal-prepare-exec_env
	#cd exec_env/app_base && PSMODULEPATH=$(shell pwd)/exec_env/app_base/Modules LD_LIBRARY_PATH=. ./corerun powershell-run.exe "import-module -name Microsoft.PowerShell.Utility" '"a","b","c","a","a" | Select-Object -Unique'
	cd exec_env/app_base && PAL_DBG_CHANNELS="+LOADER.TRACE" PSMODULEPATH=$(shell pwd)/exec_env/app_base/Modules LD_LIBRARY_PATH=. ./corerun powershell-simple.exe "\"test blah\""

trace:
	cd exec_env/app_base && PSMODULEPATH=$(shell pwd)/exec_env/app_base/Modules LD_LIBRARY_PATH=. strace -e trace=file ./corerun powershell-simple.exe "import-module -name Microsoft.PowerShell.Utility" '"a","b","c","a","a" | Select-Object -Unique'

debug: dotnetlibs/powershell-run.exe dotnetlibs/corerun internal-prepare-exec_env dotnetlibs/api-ms-win-core-registry-l1-1-0.dll
	cd exec_env/app_base && PSMODULEPATH=$(shell pwd)/exec_env/app_base/Modules LD_LIBRARY_PATH=.:/usr/lib/llvm-3.6/lib lldb-3.6 ./corerun powershell-simple.exe "\"hello world\""

clean:
	rm -f dotnetlibs/System.Management.Automation.dll dotnetlibs/Microsoft.PowerShell.Commands.Management.dll dotnetlibs/powershell-run.exe dotnetlibs/Microsoft.Management.Infrastructure.Native.dll CorePsTypeCatalog.cs buildtemp/TypeCatalogGen.exe string-resources.dll test.dll buildtemp/System.Reflection.Metadata.dll buildtemp/System.Collections.Immutable.dll
	rm -rf exec_env
	rm -rf libps-build
	rm -rf dotnetlibs/*

# clean built stuff + prepare step
cleanall: clean
	rm -rf buildtemp/*

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

