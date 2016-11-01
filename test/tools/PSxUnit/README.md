# PSxUnit
This is a Xunit runner for Core Powershell.
It supports cross platforms and is compatible with Core Clr.

#How to install:
git clone --recursive https://github.com/chunqingchen/PSxUnit

dotnet build

#How to run Tests:
1. Once build, copy all the Powershell Assemblies from your Powershell Package 
   (you can dowanload latest release from https://github.com/PowerShell/PowerShell/releases) to the bin folder ..\PSxUnit\src\PSxUnit\bin\Debug\netcoreapp1.0\

2. Copy your xunit test assembly to the same location.

    To run from console:
    
    ..\PSxUnit\src\PSxUnit> dotnet run -Assembly "<xunit test assembly>" -TestType "All"

    To run from Visual Studio:
    
    Open PSxUnit project in Visual Studio->open property page of PSxUnit project->in the Debug page, 
    fill "Application Arguments" with "-Assembly "<xunit test assembly>" -TestType "All"". In this case you can set breakpoints to your test case.
 
 3. The test log will be stored as result.xml under PSxUnit folder.
 
#How to run Tests with classification(TestType):
xUnit supports three classification: "CiFact", "FeatureFact", "ScenarioFact". You can define these classification from "FactAttribute" class from Xunit tests.

    public class CiFact : FactAttribute {};
    public class FeatureFact : FactAttribute {};
    public class ScenarioFact : FactAttribute {};
  
