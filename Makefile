# default rule which passes all arguments to scripts/Makefile
.DEFAULT:
	$(MAKE) -C scripts $(MAKECMDGOALS)
