# the monad-linux superproject
export MONAD=$(realpath $(CURDIR))

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
export POWERSHELL=env LD_LIBRARY_PATH=$(MONAD)/lib CORE_ROOT=$(MONAD)/src/monad-ext/coreclr/Runtime PWRSH_ROOT=$(MONAD)/lib PSMODULEPATH=$(MONAD)/lib/Modules $(MONAD)/bin/powershell

demo:
	$(POWERSHELL) lib/powershell-simple.exe '"a","b","c","a","a" | Select-Object -Unique'

shell:
	$(POWERSHELL) lib/powershell-run.exe

clean-monad:
	$(MAKE) -C src/monad-build clean

clean-native:
	$(MAKE) -C src/monad-native clean

clean: clean-monad
