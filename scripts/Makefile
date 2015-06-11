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

# These are the main PS dlls:
# - System.Management.Automation.dll (the main PS dll)
# - commands/modules (they contain cmdlets): management and utility
# - Microsoft.Management.Infrastructure.dll (the first dll in the remoting code paths)
include system-automation.mk
include commands-management.mk
include commands-utility.mk
include management-infrastructure.mk

NUGETREF=-r:System.Reflection.Metadata.dll -r:System.Collections.Immutable.dll

MI_NATIVE_REF=-r:dotnetlibs/Microsoft.Management.Infrastructure.Native.dll
MI_REF_ASSEMBLY=-r:$(MONAD_EXT)/PS/PS_refs_modil/microsoft.management.infrastructure.metadata_dll
MI_ASSEMBLY=dotnetlibs/Microsoft.Management.Infrastructure.dll
MI_REF=-r:$(MI_ASSEMBLY)

PRODUCT_BASE_REFS=${COREREF}
PRODUCT_MI_REFS=${COREREF} ${MI_NATIVE_REF}
PRODUCT_PS_REFS=${COREREF} ${MI_REF_ASSEMBLY} -r:dotnetlibs/$(ASSEMBLY_LOAD_CONTEXT_TARGET)
PRODUCT_COMMANDS_REFS=${COREREF} -r:dotnetlibs/System.Management.Automation.dll

MCSOPTS_BASE=-unsafe -nostdlib -noconfig -define:CORECLR -define:_CORECLR
MCSOPTS_MI=${MCSOPTS_BASE} -target:library
MCSOPTS_LIB=${MCSOPTS_BASE} -target:library
MCSOPTS_PS=${STRING_RESOURCES_ORIG} ${MCSOPTS_BASE} -target:library
SRCS_ALL=${STRING_RESOURCES} ${SRCS}

# compilers
# - Roslyn's csc is used for all the PS code
# - Mono's mcs is used for build helper tools
CSC=mono Microsoft.Net.ToolsetCompilers.*/tools/csc.exe
MCS=mcs

all: dotnetlibs/System.Management.Automation.dll dotnetlibs/powershell-run.exe dotnetlibs/Microsoft.PowerShell.Commands.Management.dll dotnetlibs/Microsoft.PowerShell.Commands.Utility.dll dotnetlibs/$(ASSEMBLY_LOAD_CONTEXT_TARGET)

# this is the build rule for SMA.dll
dotnetlibs/System.Management.Automation.dll: ${SYS_AUTO_SRCS} dotnetlibs/Microsoft.Management.Infrastructure.dll ../src/assembly-info/System.Management.Automation.assembly-info.cs dotnetlibs/$(ASSEMBLY_LOAD_CONTEXT_TARGET) $(PLATFORM_SRCS) ${SYS_AUTO_RES_SRCS} ${SYS_AUTO_RES_CS_SRCS}
	$(CSC) -out:$@ ${MCSOPTS_LIB} ${PRODUCT_PS_REFS} ${SYS_AUTO_SRCS} ${SYS_AUTO_RES_REF} ${SYS_AUTO_RES_CS_SRCS} $(PLATFORM_SRCS) ../src/assembly-info/System.Management.Automation.assembly-info.cs

# this is the build rule for MMI.dll
dotnetlibs/Microsoft.Management.Infrastructure.dll: ${MAN_INFRA_SRCS} dotnetlibs/Microsoft.Management.Infrastructure.Native.dll ../src/assembly-info/Microsoft.Management.Infrastructure.assembly-info.cs $(MAN_INFRA_RES_SRCS) $(MAN_INFRA_RES_CS_SRCS)
	$(CSC) -out:$@ $(MCSOPTS_MI) $(PRODUCT_MI_REFS) $(MAN_INFRA_SRCS) $(MAN_INFRA_RES_REF) $(MAN_INFRA_RES_CS_SRCS) ../src/assembly-info/Microsoft.Management.Infrastructure.assembly-info.cs

# Commands

dotnetlibs/Microsoft.PowerShell.Commands.Management.dll: ${COMMANDS_MANAGEMENT_SRCS} dotnetlibs/System.Management.Automation.dll ${COMMAND_MANAGEMENT_RES_SRCS} ${COMMAND_MANAGEMENT_RES_CS_SRCS} $(MI_ASSEMBLY)
	$(CSC) -out:$@ ${MCSOPTS_LIB} ${PRODUCT_COMMANDS_REFS} ${COMMANDS_MANAGEMENT_SRCS} ${COMMAND_MANAGEMENT_RES} ${COMMANDS_MANAGEMENT_RES_CS_SRCS} $(COMMANDS_MANAGEMENT_RES_REF) $(MI_REF)

dotnetlibs/Microsoft.PowerShell.Commands.Utility.dll: ${COMMANDS_UTILITY_SRCS} dotnetlibs/System.Management.Automation.dll ${COMMAND_UTILITY_RES_SRCS} ${COMMAND_UTILITY_RES_CS_SRCS}
	$(CSC) -out:$@ ${MCSOPTS_LIB} ${PRODUCT_COMMANDS_REFS} ${COMMANDS_UTILITY_SRCS} ${COMMAND_UTILITY_RES} ${COMMANDS_UTILITY_RES_CS_SRCS} $(COMMANDS_UTILITY_RES_REF)

# assembly load context

dotnetlibs/$(ASSEMBLY_LOAD_CONTEXT_TARGET): $(ASSEMBLY_LOAD_CONTEXT_SRCS)
	$(CSC) -out:$@ ${MCSOPTS_LIB} ${PRODUCT_BASE_REFS} $(ASSEMBLY_LOAD_CONTEXT_SRCS)

# this will copy whatever the first version of the dll in the globber is
System.Reflection.Metadata.dll: System.Reflection.Metadata.*/lib/portable-net45+win8/System.Reflection.Metadata.dll
	cp -f $< $@

# this will copy whatever the first version of the dll in the globber is
System.Collections.Immutable.dll: System.Collections.Immutable.*/lib/portable-net45+win8+wp8+wpa81/System.Collections.Immutable.dll
	cp -f $< $@

dotnetlibs/Microsoft.Management.Infrastructure.Native.dll: $(MONAD_EXT)/PS/windows_libs_modil/Microsoft.Management.Infrastructure.Native.dll
	cp -f $^ $@

MPATH=/usr/lib/mono/4.5/Facades

TypeCatalogGen.exe: ../src/monad/monad/nttargets/assemblies/core/PSAssemblyLoadContext/TypeCatalogGen/TypeCatalogGen.cs System.Reflection.Metadata.dll System.Collections.Immutable.dll
	$(MCS) -out:$@ -target:exe ${NUGETREF} -pkg:dotnet -r:${MPATH}/System.Runtime.dll -r:${MPATH}/System.Reflection.Primitives.dll -r:${MPATH}/System.IO.dll $<

# generate the Core PS type catalog
# this comes from: ../src/monad/monad/nttargets/assemblies/core/PSAssemblyLoadContext/makefile.inc
CorePsTypeCatalog.cs: powershell-linux.inc TypeCatalogGen.exe System.Reflection.Metadata.dll System.Collections.Immutable.dll
	LD_LIBRARY_PATH=. mono TypeCatalogGen.exe powershell-linux.inc $@ $(MONAD_EXT)/coreclr/TargetingPack

dotnetlibs/powershell-run.exe: ../src/powershell-run/powershell-run.cs dotnetlibs/System.Management.Automation.dll dotnetlibs/Microsoft.PowerShell.Commands.Management.dll dotnetlibs/$(ASSEMBLY_LOAD_CONTEXT_TARGET)
	$(CSC) -out:$@ -noconfig -nostdlib -target:exe -r:dotnetlibs/System.Management.Automation.dll -r:dotnetlibs/$(ASSEMBLY_LOAD_CONTEXT_TARGET) ${COREREF} ../src/powershell-run/powershell-run.cs

# testing c# stuff

dotnetlibs/xunit.core.dll: $(MONAD_EXT)/xunit/xunit.core.dll
	cp -f $^ $@

dotnetlibs/xunit.abstractions.dll: $(MONAD_EXT)/xunit/xunit.abstractions.dll
	cp -f $^ $@

dotnetlibs/xunit.assert.dll: $(MONAD_EXT)/xunit/xunit.assert.dll
	cp -f $^ $@

dotnetlibs/xunit.console.exe: $(MONAD_EXT)/xunit/xunit.console.netcore.exe
	cp -f $^ $@

TEST_FOLDER=../src/ps_test
TEST_SRCS=$(addprefix $(TEST_FOLDER)/, test_CorePsPlatform.cs)

dotnetlibs/ps_test.dll: $(TEST_SRCS) dotnetlibs/xunit.core.dll dotnetlibs/xunit.assert.dll dotnetlibs/System.Management.Automation.dll dotnetlibs/Microsoft.PowerShell.Commands.Management.dll dotnetlibs/$(ASSEMBLY_LOAD_CONTEXT_TARGET)
	$(CSC) -out:$@ -noconfig -nostdlib -target:library -r:dotnetlibs/System.Management.Automation.dll -r:dotnetlibs/$(ASSEMBLY_LOAD_CONTEXT_TARGET) -r:dotnetlibs/xunit.core.dll -r:dotnetlibs/xunit.assert.dll ${COREREF} $(TEST_SRCS)

dotnetlibs/ps_test_runner.exe: $(TEST_FOLDER)/ps_test.cs dotnetlibs/ps_test.dll dotnetlibs/System.Management.Automation.dll dotnetlibs/Microsoft.PowerShell.Commands.Management.dll dotnetlibs/$(ASSEMBLY_LOAD_CONTEXT_TARGET) dotnetlibs/xunit.core.dll dotnetlibs/xunit.assert.dll
	$(CSC) -out:$@ -noconfig -nostdlib -target:exe -r:dotnetlibs/ps_test.dll -r:dotnetlibs/System.Management.Automation.dll -r:dotnetlibs/$(ASSEMBLY_LOAD_CONTEXT_TARGET) -r:dotnetlibs/xunit.core.dll -r:dotnetlibs/xunit.assert.dll ${COREREF} $<

run-test: dotnetlibs/ps_test_runner.exe dotnetlibs/corerun
	cd dotnetlibs && LD_LIBRARY_PATH=. ./corerun ps_test_runner.exe

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
	rm -rf System.Reflection.Metadata.* System.Collections.Immutable.* Microsoft.Net.ToolsetCompilers.*
	nuget install System.Reflection.Metadata
	nuget install System.Collections.Immutable
	nuget install Microsoft.Net.ToolsetCompilers -pre

prepare-exec_env:
	rm -rf exec_env
	mkdir exec_env
	cp -r ../src/monad_app_base/app_base exec_env
	cp -r dotnetlibs/*.dll exec_env/app_base
	cp -r dotnetlibs/*.exe exec_env/app_base
	cp -r dotnetlibs/lib* exec_env/app_base
	cp -r dotnetlibs/corerun exec_env/app_base

run: dotnetlibs/powershell-run.exe dotnetlibs/corerun dotnetlibs/Microsoft.PowerShell.Commands.Management.dll dotnetlibs/Microsoft.PowerShell.Commands.Utility.dll dotnetlibs/api-ms-win-core-registry-l1-1-0.dll prepare-exec_env
	# check if corerun is the right one (could be the debug version)
	if ! diff dotnetlibs/corerun $(MONAD_EXT)/coreclr/Release/corerun; then cp -r $(MONAD_EXT)/coreclr/Release/* dotnetlibs; fi
	#cd exec_env/app_base && PSMODULEPATH=. LD_LIBRARY_PATH=. ./corerun powershell-run.exe "\"test blah\""
	#cd exec_env/app_base && PSMODULEPATH=. LD_LIBRARY_PATH=. ./corerun powershell-run.exe "get-module -listavailable"
	#cd exec_env/app_base && PSMODULEPATH=. LD_LIBRARY_PATH=. ./corerun powershell-run.exe "get-module -listavailable -all"
	#cd exec_env/app_base && PSMODULEPATH=$(shell pwd)/exec_env/app_base/Modules LD_LIBRARY_PATH=. ./corerun powershell-run.exe "import-module -name Microsoft.PowerShell.Utility"
	cd exec_env/app_base && PSMODULEPATH=$(shell pwd)/exec_env/app_base/Modules LD_LIBRARY_PATH=. ./corerun powershell-run.exe "import-module -name Microsoft.PowerShell.Utility" '"a","b","c","a","a" | Select-Object -Unique'
	#LD_LIBRARY_PATH=exec_env/app_base exec_env/app_base/corerun exec_env/app_base/powershell-run.exe "\"Hello World!\" |out-default"

prepare-debugclr:
	cp -r $(MONAD_EXT)/coreclr/Debug/* dotnetlibs

run-debugclr: dotnetlibs/powershell-run.exe prepare-debugclr dotnetlibs/corerun dotnetlibs/Microsoft.PowerShell.Commands.Management.dll dotnetlibs/Microsoft.PowerShell.Commands.Utility.dll dotnetlibs/api-ms-win-core-registry-l1-1-0.dll prepare-exec_env
	#cd exec_env/app_base && PSMODULEPATH=$(shell pwd)/exec_env/app_base/Modules LD_LIBRARY_PATH=. ./corerun powershell-run.exe "import-module -name Microsoft.PowerShell.Utility" '"a","b","c","a","a" | Select-Object -Unique'
	cd exec_env/app_base && PAL_DBG_CHANNELS="+LOADER.TRACE" PSMODULEPATH=$(shell pwd)/exec_env/app_base/Modules LD_LIBRARY_PATH=. ./corerun powershell-run.exe "\"test blah\""

trace:
	cd exec_env/app_base && PSMODULEPATH=$(shell pwd)/exec_env/app_base/Modules LD_LIBRARY_PATH=. strace -e trace=file ./corerun powershell-run.exe "import-module -name Microsoft.PowerShell.Utility" '"a","b","c","a","a" | Select-Object -Unique'

debug: dotnetlibs/powershell-run.exe dotnetlibs/corerun prepare-exec_env dotnetlibs/api-ms-win-core-registry-l1-1-0.dll
	#LD_LIBRARY_PATH=dotnetlibs lldb-3.6 dotnetlibs/corerun dotnetlibs/powershell-run.exe
	cd exec_env/app_base && PSMODULEPATH=$(shell pwd)/exec_env/app_base/Modules LD_LIBRARY_PATH=.:/usr/lib/llvm-3.6/lib lldb-3.6 ./corerun powershell-run.exe "\"hello world\""

clean:
	rm -f dotnetlibs/System.Management.Automation.dll dotnetlibs/Microsoft.PowerShell.Commands.Management.dll dotnetlibs/powershell-run.exe dotnetlibs/Microsoft.Management.Infrastructure.Native.dll CorePsTypeCatalog.cs TypeCatalogGen.exe string-resources.dll test.dll System.Reflection.Metadata.dll System.Collections.Immutable.dll
	rm -rf exec_env
	rm -rf libps-build
	rm -rf dotnetlibs/*

# clean built stuff + prepare step
cleanall: clean
	rm -rf System.Reflection.Metadata.* System.Collections.Immutable.*

