# Start with latest NanoServer .NET Core image from https://hub.docker.com/r/microsoft/dotnet/
FROM @BASE_IMAGE@
MAINTAINER brycem@microsoft.com

# Install .NET Core SDK
COPY build.psm1 c:\\WorkingDir\\build.psm1
COPY jenkinsutils.psm1 c:\\WorkingDir\\jenkinsutils.psm1
COPY test c:\\WorkingDir\\test
COPY PowerShell c:\\WorkingDir\\PowerShell
ENV WORKINGDIR C:\\WorkingDir
RUN setx /M PATH "%PATH%;C:\\WorkingDir"
RUN powershell.exe -NonInteractive -Command \
  $ErrorActionPreference = 'Stop'; \
  cd C:\\WorkingDir ; \
  dotnet restore ; \
  Import-Module c:\\WorkingDir\\jenkinsutils.psm1 ; \
  Import-Module c:\\WorkingDir\\build.psm1 ; \
  Start-PSPester -Path C:\\WorkingDir\\test\\powershell\\ -OutputFormat NUnitXml -OutputFile c:\\WorkingDir\\pester-tests.xml -binDir C:\\WorkingDir\\PowerShell\\ -ThrowOnFailure ; \
