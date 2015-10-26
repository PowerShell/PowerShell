# the monad-linux superproject and lib directory
export MONAD=$(realpath $(CURDIR))
export PSLIB=$(MONAD)/lib

all: powershell-native powershell-managed

# managed code

powershell-managed:
	$(MAKE) -j -C src/monad-build all test

# native code

powershell-native: src/monad-native/Makefile
	$(MAKE) -j -C src/monad-native

src/monad-native/Makefile:
	cd src/monad-native && cmake .

# one-time setup

tools/nuget.exe:
	cd tools && 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe'

bootstrap: tools/nuget.exe
	mono $< restore -PackagesDirectory tools

# run targets

export POWERSHELL=env LD_LIBRARY_PATH=$(PSLIB) CORE_ROOT=$(MONAD)/src/monad-ext/coreclr/Runtime PWRSH_ROOT=$(PSLIB) PSMODULEPATH=$(PSLIB)/Modules $(MONAD)/bin/powershell
export POWERSHELL_SIMPLE=$(POWERSHELL) $(PSLIB)/powershell-simple.exe

demo:
	$(POWERSHELL_SIMPLE) '"a","b","c","a","a" | Select-Object -Unique'

shell:
	$(POWERSHELL) lib/powershell-run.exe

# tests

test: test-pester

## TODO: fix this after refactoring bin/powershell
test-hashbang:
	PATH=$(PATH):$(PSLIB) src/3rdparty/hashbang/script.ps1

## Pester tests for PowerShell - results in pester-tests.xml
## - see https://github.com/pester/Pester
## - requires $TEMP to be set
## - we cd because some tests rely on the current working directory
PESTER=$(MONAD)/src/pester-tests
test-pester:
	$(POWERSHELL_SIMPLE) 'cd $(PESTER); $$env:TEMP="/tmp"; invoke-pester -OutputFile $(MONAD)/pester-tests.xml -OutputFormat NUnitXml'

## Pester self-tests
## - results in pester-self-tests.xml
test-pester-self:
	$(POWERSHELL_SIMPLE) 'cd $(PSLIB)/Modules/Pester/Functions; $$env:TEMP="/tmp"; invoke-pester -OutputFile $(MONAD)/pester-self-tests.xml -OutputFormat NUnitXml'

## tracing
## - use PAL_DBG_CHANNELS="+LOADER.TRACE" to enable CoreCLR tracing
## - use Set-PSDebug -Trace 2 to enable PowerShell tracing

# OMI
OMI=src/omi/Unix
OMI_FLAGS=--dev --enable-debug
$(OMI)/GNUmakefile:
	cd $(OMI) && ./configure $(OMI_FLAGS)

OMISERVER=$(OMI)/output/bin/omiserver
$(OMISERVER): $(OMI)/GNUmakefile
	$(MAKE) -j -C $(OMI)

## copy libpshost because OMI isn't configurable
MONAD_PROVIDER=src/monad-omi-provider
PSRP_OMI_PROVIDER=$(OMI)/output/lib/libTestShell.so
$(PSRP_OMI_PROVIDER): $(OMISERVER)
	cp lib/libpshost.a $(OMI)/output/lib
	$(MAKE) -j -C $(MONAD_PROVIDER)

psrp: $(PSRP_OMI_PROVIDER)

## phony targets so that the recursive make is always invoked
.PHONY: $(OMISERVER) $(PSRP_OMI_PROVIDER)

# clean targets

clean: clean-monad
	rm *-tests.xml

distclean: distclean-omi distclean-monad clean
	rm -rf tools/*

clean-monad:
	$(MAKE) -C src/monad-build clean

distclean-monad:
	$(MAKE) -C src/monad-build distclean

clean-native:
	-$(MAKE) -C src/monad-native clean

clean-omi:
	-$(MAKE) -C $(OMI) clean

distclean-omi:
	-$(MAKE) -C $(OMI) distclean

clean-psrp:
	-$(MAKE) -C $(MONAD_PROVIDER) clean
