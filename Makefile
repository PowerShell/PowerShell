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
	wget 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe'

bootstrap: tools/nuget.exe
	mono $< restore -PackagesDirectory tools

# run targets

export POWERSHELL=env LD_LIBRARY_PATH=$(PSLIB) CORE_ROOT=$(MONAD)/src/monad-ext/coreclr/Runtime PWRSH_ROOT=$(PSLIB) PSMODULEPATH=$(PSLIB)/Modules $(MONAD)/bin/powershell
export POWERSHELL_SIMPLE=$(POWERSHELL) $(PSLIB)/powershell-simple.exe

demo:
	$(POWERSHELL_SIMPLE) '"a","b","c","a","a" | Select-Object -Unique'

shell:
	$(POWERSHELL) lib/powershell-run.exe

## TODO: fix this after refactoring bin/powershell
test-hashbang:
	PATH=$(PATH):$(PSLIB) src/3rdparty/hashbang/script.ps1

# clean targets

clean-monad:
	$(MAKE) -C src/monad-build clean

clean-native:
	$(MAKE) -C src/monad-native clean

clean: clean-monad
