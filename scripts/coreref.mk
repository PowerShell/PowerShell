
# this depends on the MONAD_EXT variable to be correctly set
TARGETING_PACK=$(MONAD_EXT)/coreclr/TargetingPack

COREREF=$(addprefix -r:, $(shell ls $(TARGETING_PACK)/*.dll))

CORECLR_ASSEMBLY_BASE=$(MONAD_EXT)/coreclr/Release

# COREREF_2 is here for dev/testing purposes, it should not be used anywhere (instead use the reference assemblies stored in COREREF)
COREREF_2=$(addprefix -r:, $(shell ls $(CORECLR_ASSEMBLY_BASE)/*.dll))
