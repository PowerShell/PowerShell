# the monad-linux superproject
export MONAD=$(realpath $(CURDIR))

all: powershell-native approot

# managed code

approot:
	./publish.sh

# native code

powershell-native: src/monad-native/Makefile
	$(MAKE) -j -C src/monad-native
	$(MAKE) -j -C src/monad-native test

src/monad-native/Makefile:
	cd src/monad-native && cmake .

# installation of pslib
install:
	$(MAKE) -C src/monad-native install
	ldconfig

# run targets

export POWERSHELL= $(MONAD)/approot/powershell

demo:
	$(POWERSHELL) --runspace --command '"a","b","c","a","a" | Select-Object -Unique'

shell:
	$(POWERSHELL)

# tests

test: test-xunit

test-xunit:
	./test.sh

## TODO: fix this after refactoring bin/powershell
test-hashbang:
	PATH=$(PATH):bin src/3rdparty/hashbang/script.ps1

## Pester tests for PowerShell - results in pester-tests.xml
## - see https://github.com/pester/Pester
## - requires $TEMP to be set
## - we cd because some tests rely on the current working directory
PESTER=$(MONAD)/src/pester-tests
test-pester:
	$(POWERSHELL) 'invoke-pester $(PESTER) -OutputFile $(MONAD)/pester-tests.xml -OutputFormat NUnitXml'

## Pester self-tests
## - results in pester-self-tests.xml
test-pester-self:
	$(POWERSHELL) 'cd $(PSLIB)/Modules/Pester/Functions; $$env:TEMP="/tmp"; invoke-pester -OutputFile $(MONAD)/pester-self-tests.xml -OutputFormat NUnitXml'

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
PSRP_OMI_PROVIDER=$(OMI)/output/lib/libpsrpomiprov.so
$(PSRP_OMI_PROVIDER): $(OMISERVER) powershell-native
	cp lib/libpshost.a $(OMI)/output/lib
	$(MAKE) -j -C $(MONAD_PROVIDER)

psrp: $(PSRP_OMI_PROVIDER)

## phony targets so that the recursive make is always invoked
.PHONY: $(OMISERVER) $(PSRP_OMI_PROVIDER)

# clean targets

clean: clean-monad
	-rm *-tests.xml

distclean: distclean-monad distclean-native distclean-omi clean
	-rm -rf $(CLRLIB)

clean-monad:
	rm -rf approot

distclean-monad:
	./nuke.sh

clean-native:
	-$(MAKE) -C src/monad-native clean

distclean-native:
	cd src/monad-native && git clean -fdx

clean-omi:
	-$(MAKE) -C $(OMI) clean

distclean-omi:
	-$(MAKE) -C $(OMI) distclean

clean-psrp:
	-$(MAKE) -C $(MONAD_PROVIDER) clean
