# This is an automatically generated file for the make dependency: SECURITY

SECURITY_SRCS_WIN=\
	../../../jws/pswin/admin/monad/src/security/AclCommands.cs	\
	../../../jws/pswin/admin/monad/src/security/CertificateProvider.cs	\
	../../../jws/pswin/admin/monad/src/security/certificateproviderexceptions.cs	\
	../../../jws/pswin/admin/monad/src/security/CredentialCommands.cs	\
	../../../jws/pswin/admin/monad/src/security/ExecutionPolicyCommands.cs	\
	../../../jws/pswin/admin/monad/src/security/SecureStringCommands.cs	\
	../../../jws/pswin/admin/monad/src/security/SignatureCommands.cs	\
	../../../jws/pswin/admin/monad/src/security/Utils.cs	\


SECURITY_SRCS=\
	$(ADMIN_GIT_ROOT)/monad/src/security/AclCommands.cs	\
	$(ADMIN_GIT_ROOT)/monad/src/security/CertificateProvider.cs	\
	$(ADMIN_GIT_ROOT)/monad/src/security/certificateproviderexceptions.cs	\
	$(ADMIN_GIT_ROOT)/monad/src/security/CredentialCommands.cs	\
	$(ADMIN_GIT_ROOT)/monad/src/security/ExecutionPolicyCommands.cs	\
	$(ADMIN_GIT_ROOT)/monad/src/security/SecureStringCommands.cs	\
	$(ADMIN_GIT_ROOT)/monad/src/security/SignatureCommands.cs	\
	$(ADMIN_GIT_ROOT)/monad/src/security/Utils.cs	\


SECURITY_RES_BASE_PATH=../../../jws/pswin/admin/monad/src/security/resources

SECURITY_RES_GEN_PATH=gen/SECURITY

SECURITY_RES_GEN_PATH_WIN=gen\SECURITY

SECURITY_RESX_SRCS=\
	../../../jws/pswin/admin/monad/src/security/resources/CertificateProviderStrings.resx	\
	../../../jws/pswin/admin/monad/src/security/resources/ExecutionPolicyCommands.resx	\
	../../../jws/pswin/admin/monad/src/security/resources/SecureStringCommands.resx	\
	../../../jws/pswin/admin/monad/src/security/resources/SignatureCommands.resx	\
	../../../jws/pswin/admin/monad/src/security/resources/UtilsStrings.resx	\


SECURITY_RES_SRCS=\
	gen/SECURITY/CertificateProviderStrings.resources	\
	gen/SECURITY/ExecutionPolicyCommands.resources	\
	gen/SECURITY/SecureStringCommands.resources	\
	gen/SECURITY/SignatureCommands.resources	\
	gen/SECURITY/UtilsStrings.resources	\


SECURITY_RES_CS_SRCS=\
	gen/SECURITY/CertificateProviderStrings.cs	\
	gen/SECURITY/ExecutionPolicyCommands.cs	\
	gen/SECURITY/SecureStringCommands.cs	\
	gen/SECURITY/SignatureCommands.cs	\
	gen/SECURITY/UtilsStrings.cs	\


SECURITY_RES_REF=\
	-resource:gen/SECURITY/CertificateProviderStrings.resources	\
	-resource:gen/SECURITY/ExecutionPolicyCommands.resources	\
	-resource:gen/SECURITY/SecureStringCommands.resources	\
	-resource:gen/SECURITY/SignatureCommands.resources	\
	-resource:gen/SECURITY/UtilsStrings.resources	\


# the commands below need the make variable SHELL to be set to "cmd", this is best done
# as a command line option to make
$(SECURITY_RES_GEN_PATH)/CertificateProviderStrings.resources: $(SECURITY_RES_BASE_PATH)/CertificateProviderStrings.resx
	mkdir $(SECURITY_RES_GEN_PATH_WIN) || exit /b 0
	resgen /useSourcePath $< $@

$(SECURITY_RES_GEN_PATH)/CertificateProviderStrings.cs: $(SECURITY_RES_GEN_PATH)/CertificateProviderStrings.resources
	resgen /useSourcePath /str:cs $<
	sed -i -- 's/using System;/using System;\r\nusing System.Reflection;/g' $@
	sed -i -- 's/)\.Assembly/).GetTypeInfo().Assembly/g' $@

$(SECURITY_RES_GEN_PATH)/ExecutionPolicyCommands.resources: $(SECURITY_RES_BASE_PATH)/ExecutionPolicyCommands.resx
	mkdir $(SECURITY_RES_GEN_PATH_WIN) || exit /b 0
	resgen /useSourcePath $< $@

$(SECURITY_RES_GEN_PATH)/ExecutionPolicyCommands.cs: $(SECURITY_RES_GEN_PATH)/ExecutionPolicyCommands.resources
	resgen /useSourcePath /str:cs $<
	sed -i -- 's/using System;/using System;\r\nusing System.Reflection;/g' $@
	sed -i -- 's/)\.Assembly/).GetTypeInfo().Assembly/g' $@

$(SECURITY_RES_GEN_PATH)/SecureStringCommands.resources: $(SECURITY_RES_BASE_PATH)/SecureStringCommands.resx
	mkdir $(SECURITY_RES_GEN_PATH_WIN) || exit /b 0
	resgen /useSourcePath $< $@

$(SECURITY_RES_GEN_PATH)/SecureStringCommands.cs: $(SECURITY_RES_GEN_PATH)/SecureStringCommands.resources
	resgen /useSourcePath /str:cs $<
	sed -i -- 's/using System;/using System;\r\nusing System.Reflection;/g' $@
	sed -i -- 's/)\.Assembly/).GetTypeInfo().Assembly/g' $@

$(SECURITY_RES_GEN_PATH)/SignatureCommands.resources: $(SECURITY_RES_BASE_PATH)/SignatureCommands.resx
	mkdir $(SECURITY_RES_GEN_PATH_WIN) || exit /b 0
	resgen /useSourcePath $< $@

$(SECURITY_RES_GEN_PATH)/SignatureCommands.cs: $(SECURITY_RES_GEN_PATH)/SignatureCommands.resources
	resgen /useSourcePath /str:cs $<
	sed -i -- 's/using System;/using System;\r\nusing System.Reflection;/g' $@
	sed -i -- 's/)\.Assembly/).GetTypeInfo().Assembly/g' $@

$(SECURITY_RES_GEN_PATH)/UtilsStrings.resources: $(SECURITY_RES_BASE_PATH)/UtilsStrings.resx
	mkdir $(SECURITY_RES_GEN_PATH_WIN) || exit /b 0
	resgen /useSourcePath $< $@

$(SECURITY_RES_GEN_PATH)/UtilsStrings.cs: $(SECURITY_RES_GEN_PATH)/UtilsStrings.resources
	resgen /useSourcePath /str:cs $<
	sed -i -- 's/using System;/using System;\r\nusing System.Reflection;/g' $@
	sed -i -- 's/)\.Assembly/).GetTypeInfo().Assembly/g' $@

SECURITY_make_rule_RES_SRCS: $(SECURITY_RES_SRCS)

SECURITY_make_rule_RES_CS_SRCS: $(SECURITY_RES_CS_SRCS)

SECURITY_TARGET=Microsoft.PowerShell.Security

