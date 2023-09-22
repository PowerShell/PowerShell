# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# This is a Pester test suite to validate the New-FileCatalog & Test-FileCatalog cmdlets on PowerShell.

try {
    #skip all tests on non-windows platform
    $defaultParamValues = $PSDefaultParameterValues.Clone()
    $PSDefaultParameterValues["it:skip"] = !$IsWindows

$script:catalogPath = ""

Describe "Test suite for NewFileCatalogAndTestFileCatalogCmdlets" -Tags "CI" {

    #compare two hashtables
    function CompareHashTables
    {
        param
        (
          $hashTable1,
          $hashTable2
        )

        foreach ($key in $hashTable1.keys)
        {
            $keyValue1 = $hashTable1["$key"]
            if($hashTable2.ContainsKey($key))
            {
                $keyValue2 = $hashTable2["$key"]
                $keyValue1 | Should -Be $keyValue2
            }
            else
            {
                throw "Failed to find the file $keyValue1 for $key in Hashtable"
            }
        }
    }

    BeforeAll {
        $testDataPath = "$PSScriptRoot\TestData\CatalogTestData"
    }

    Context "NewAndTestCatalogTests PositiveTestCases when validation Succeeds" {

        It "NewFileCatalogWithSingleFile with WhatIf" {

            $sourcePath = Join-Path $testDataPath '\CatalogTestFile1.mof'
            # use existent Path for the directory when .cat file name is not specified
            $catalogPath = $testDataPath
            $catalogFile = $catalogPath + "\catalog.cat"

            try
            {
                $null = New-FileCatalog -Path $sourcePath -CatalogFilePath $catalogPath -WhatIf
                $result = Test-Path -Path $catalogFile
            }
            finally
            {
                Remove-Item $catalogFile -Force -ErrorAction SilentlyContinue
            }

            # Validate result properties
            $result | Should -BeFalse
        }

        It "NewFileCatalogFolder" {

            $sourcePath = Join-Path $testDataPath 'UserConfigProv\DSCResources\scriptdsc'
            $catalogPath = "$testDataPath\NewFileCatalogFolder.cat"

            try
            {
                $null = New-FileCatalog -Path $sourcePath -CatalogFilePath $catalogPath -CatalogVersion 1.0
                $result = Test-FileCatalog -Path $sourcePath -CatalogFilePath $catalogPath -Detailed
            }
            finally
            {
                Remove-Item $catalogPath -Force -ErrorAction SilentlyContinue
            }

            # Validate result properties
            $result.Status | Should -Be "Valid"
            $result.Signature.Status | Should -Be "NotSigned"
            $result.HashAlgorithm | Should -Be "SHA1"
        }

        It "NewFileCatalogFolderWithSubFolders" {

            $sourcePath = Join-Path $testDataPath 'UserConfigProv'
            # use non existent Path for the directory when .cat file name is specified
            $catalogPath = "$testDataPath\OutPutCatalog\NewFileCatalogFolderWithSubFolders.cat"

            try
            {
                $null = New-FileCatalog -Path $sourcePath -CatalogFilePath $catalogPath
                $result = Test-FileCatalog -Path $sourcePath -CatalogFilePath $catalogPath -Detailed
            }
            finally
            {
                Remove-Item "$sourcePath\OutPutCatalog" -Force -ErrorAction SilentlyContinue -Recurse
            }

            # Validate result properties
            $result.Status | Should -Be "Valid"
            $result.Signature.Status | Should -Be "NotSigned"
            $result.HashAlgorithm | Should -Be "SHA1"
        }

        It "NewFileCatalogWithSingleFile" {

            $sourcePath = Join-Path $testDataPath '\CatalogTestFile1.mof'
            # use existent Path for the directory when .cat file name is not specified
            $catalogPath = $testDataPath
            try
            {
                $null = New-FileCatalog -Path $sourcePath -CatalogFilePath $catalogPath
                $result = Test-FileCatalog -Path $sourcePath -CatalogFilePath ($catalogPath + "\catalog.cat")
            }
            finally
            {
                Remove-Item "$catalogPath\catalog.cat" -Force -ErrorAction SilentlyContinue
            }

            # Validate result properties
            $result | Should -Be "Valid"
        }

        It "NewFileCatalogForFilesThatDoNotSupportEmbeddedSignatures" {

            $expectedPathsAndHashes = @{ "TestImage.gif" = "B0E4B9F0BB21284AA0AF0D525C913420AD73DA6A" ;
                                        "TestFileCatalog.txt" = "BA6A26C5F19AB50B0D5BE2A9D445B259998B0DD9" }

            # use non existent Path for the directory when .cat file name is not specified
            $catalogPath = "$testDataPath\OutPutCatalog"

            try
            {
                $null = New-FileCatalog -Path "$testDataPath\TestImage.gif","$testDataPath\TestFileCatalog.txt" -CatalogFilePath $catalogPath -CatalogVersion 1.0
                $result = Test-FileCatalog -Path "$testDataPath\TestImage.gif","$testDataPath\TestFileCatalog.txt"  -CatalogFilePath ($catalogPath + "\catalog.cat") -Detailed
            }
            finally
            {
                Remove-Item "$catalogPath" -Force -ErrorAction SilentlyContinue -Recurse
            }

            $result.Status | Should -Be "Valid"
            $result.CatalogItems.Count | Should -Be 2
            $result.PathItems.Count | Should -Be 2
            CompareHashTables $result.CatalogItems $result.PathItems
            CompareHashTables $result.CatalogItems $expectedPathsAndHashes
        }

        It "NewFileCatalogWithMultipleFoldersAndFiles" -Pending {

            $expectedPathsAndHashes = @{
                "UserConfigProv.psd1" = "748E5486814051DA3DFB79FE8964152727213248" ;
                "DSCResources\UserConfigProviderModVersion1\UserConfigProviderModVersion1.schema.mof" ="F7CAB050E32CF0C9B2AC2807C4F24D31EFCC8B61";
                "dscresources\UserConfigProviderModVersion3\UserConfigProviderModVersion3.psm1" = "F9DD6B02C7BD0FB98A25BE0D41210B2A2333E139";
                "DSCResources\scriptdsc\scriptDSC.schema.psm1"= "CDBAF85FEDE2E0CD09B1AEA0532010CEFCECBC12";
                "DSCResources\UserConfigProviderModVersion1\UserConfigProviderModVersion1.psm1" = "7599777B85B60377B1F3E492C817190090A754A7"
                "DSCResources\scriptdsc\scriptdsc.psd1"= "CDDC68AF9B863760A14031772DC9ADDAFD209D80";
                "DSCResources\UserConfigProviderModVersion3\UserConfigProviderModVersion3.schema.mof" ="AFEB46104F506FC64CAB4B0B2A9C6C50622B487A";
                "DSCResources\UserConfigProviderModVersion2\UserConfigProviderModVersion2.psm1"= "60CB9C8AEDA7A64127D34361ED4F30DEAFE37022";
                "DSCResources\UserConfigProviderModVersion2\UserConfigProviderModVersion2.schema.mof" = "E33FBFEA28E9A8FBA793FBC3D8015BCC9A10944B";
                "CatalogTestFile1.mof" = "083B0953D0D70FFF62710F0356FEB86BCE327FE7";
                "CatalogTestFile2.xml" = "E73BB7A0DD9FAC6A8182F67B750D9CA3094490F1" }

            $catalogPath = "$env:TEMP\NewFileCatalogWithMultipleFoldersAndFiles.cat"
            $catalogDataPath = @("$testDataPath\UserConfigProv\","$testDataPath\CatalogTestFile1.mof","$testDataPath\CatalogTestFile2.xml")

            try
            {
                $null =New-FileCatalog -Path $catalogDataPath -CatalogFilePath $catalogPath -CatalogVersion 1.0
                $result = Test-FileCatalog -Path $catalogDataPath -CatalogFilePath $catalogPath -Detailed
            }
            finally
            {
                Remove-Item "$catalogPath" -Force -ErrorAction SilentlyContinue
            }

            $result.Status | Should -Be "Valid"
            $result.Signature.Status | Should -Be "NotSigned"
            $result.HashAlgorithm | Should -Be "SHA1"
            $result.CatalogItems.Count | Should -Be 11
            $result.PathItems.Count | Should -Be 11

            CompareHashTables $result.CatalogItems $result.PathItems
            CompareHashTables $result.CatalogItems $expectedPathsAndHashes
        }

        It "NewFileCatalogVersion2WithMultipleFoldersAndFiles" -Pending {

            $expectedPathsAndHashes = @{
                "UserConfigProv.psd1" = "9FFE4CA2873CD91CDC9D71362526446ECACDA64D26DEA768E6CE489B84D888E4" ;
                "DSCResources\UserConfigProviderModVersion1\UserConfigProviderModVersion1.schema.mof" ="517F625CB6C465928586F5C613F768B33C20F477DAF843C179071B8C74B992AA";
                "DSCResources\UserConfigProviderModVersion3\UserConfigProviderModVersion3.psm1" = "0774A539E73B1A480E38CFFE2CF0B8AC46120A0B2E0377E0DE2630031BE83347";
                "DSCResources\scriptdsc\scriptdsc.schema.psm1"= "7DE80DED0F96FA7D34CF34089A1B088E91CD7B1D80251949FC7C78A6308D51C3";
                "DSCResources\UserConfigProviderModVersion1\UserConfigProviderModVersion1.psm1" = "EB0310C630EDFDFBDD1D993A636EC9B75BB1F04DF7E7FFE39CF6357679C852C7"
                "DSCResources\scriptdsc\scriptdsc.psd1"= "AB8E8D0840D4854CDCDE25058872413AF417FC016BD77FD5EC677BBB7393532B";
                "DSCResources\UserConfigProviderModVersion3\UserConfigProviderModVersion3.schema.mof" ="7163E607F067A3C4F91D3AFF3C466ECA47C0CF84B5F0DDA22B2C0E99929B5E21";
                "DSCResources\UserConfigProviderModVersion2\UserConfigProviderModVersion2.psm1"= "6591FE02528D7FB66F00E09D7F1A025D5D5BAF30A49C5FF1EC562FAE39B38F43";
                "DSCResources\UserConfigProviderModVersion2\UserConfigProviderModVersion2.schema.mof" = "679318201B012CC5936B29C095956B2131FAF828C0CCA4342A5914F721480FB9";
                "CatalogTestFile1.mof" = "7C1885AE5F76F58DAA232A5E962875F90308C3CB8580400EE12F999B4E10F940";
                "CatalogTestFile2.xml" = "00B7DA28CD285F796660D36B77B2EC6054F21A44D5B329EB6BC4EC7687D70B13";
                "TestImage.gif" = "2D938D255D0D6D547747BD21447CF7295318D34D9B4105D04C1C27487D2FF402" }

            $catalogPath = "$env:TEMP\NewFileCatalogVersion2WithMultipleFoldersAndFiles.cat"
            $catalogDataPath = @("$testDataPath\UserConfigProv\","$testDataPath\CatalogTestFile1.mof","$testDataPath\CatalogTestFile2.xml", "$testDataPath\TestImage.gif")

            try
            {
                $null = New-FileCatalog -Path $catalogDataPath -CatalogFilePath $catalogPath -CatalogVersion 2.0
                $result = Test-FileCatalog -Path $catalogDataPath -CatalogFilePath $catalogPath -Detailed
            }
            finally
            {
                Remove-Item "$catalogPath" -Force -ErrorAction SilentlyContinue
            }

            $result.Status | Should -Be "Valid"
            $result.Signature.Status | Should -Be "NotSigned"
            $result.HashAlgorithm | Should -Be "SHA256"
            $result.CatalogItems.Count | Should -Be 12
            $result.PathItems.Count | Should -Be 12
            CompareHashTables $result.CatalogItems $result.PathItems
            CompareHashTables $result.CatalogItems $expectedPathsAndHashes
        }

        # This is failing saying the exact thing that it says is supposed to work does not
        It "Test-FileCatalog should pass when catalog is in the same folder as files being tested" -Pending {

            $catalogPath = "$env:TEMP\UserConfigProv\catalog.cat"
            try
            {
                Copy-Item "$testDataPath\UserConfigProv" $env:temp -Recurse -ErrorAction SilentlyContinue
                Push-Location "$env:TEMP\UserConfigProv"
                # When -Path is not specified, it should use current directory
                $null = New-FileCatalog -CatalogFilePath $catalogPath -CatalogVersion 1.0
                $result = Test-FileCatalog -CatalogFilePath $catalogPath

                if($result -ne 'Valid')
                {
                    # We will fail, Write why.
                    $detailResult =  Test-FileCatalog -CatalogFilePath $catalogPath -Detailed
                    $detailResult | ConvertTo-Json | Write-Verbose -Verbose
                }
            }
            finally
            {
                Pop-Location
                Remove-Item "$catalogPath" -Force -ErrorAction SilentlyContinue
                Remove-Item "$env:temp\UserConfigProv\" -Force -ErrorAction SilentlyContinue -Recurse
            }

            $result | Should -Be "Valid"
        }

        It "NewFileCatalogWithUnicodeCharactersInFileNames" -Pending {

            $expectedPathsAndHashes = @{
                "UserConfigProv.psd1" = "9FFE4CA2873CD91CDC9D71362526446ECACDA64D26DEA768E6CE489B84D888E4" ;
                "DSCResources\UserConfigProviderModVersion1\UserConfigProviderModVersion1.schema.mof" ="517F625CB6C465928586F5C613F768B33C20F477DAF843C179071B8C74B992AA";
                "DSCResources\UserConfigProviderModVersion3\UserConfigProviderModVersion3.psm1" = "0774A539E73B1A480E38CFFE2CF0B8AC46120A0B2E0377E0DE2630031BE83347";
                "DSCResources\scriptdsc\scriptdsc.schema.psm1"= "7DE80DED0F96FA7D34CF34089A1B088E91CD7B1D80251949FC7C78A6308D51C3";
                "DSCResources\UserConfigProviderModVersion1\UserConfigProviderModVersion1.psm1" = "EB0310C630EDFDFBDD1D993A636EC9B75BB1F04DF7E7FFE39CF6357679C852C7"
                "DSCResources\scriptdsc\scriptdsc.psd1"= "AB8E8D0840D4854CDCDE25058872413AF417FC016BD77FD5EC677BBB7393532B";
                "DSCResources\UserConfigProviderModVersion3\UserConfigProviderModVersion3.schema.mof" ="7163E607F067A3C4F91D3AFF3C466ECA47C0CF84B5F0DDA22B2C0E99929B5E21";
                "DSCResources\UserConfigProviderModVersion2\UserConfigProviderModVersion2.psm1"= "6591FE02528D7FB66F00E09D7F1A025D5D5BAF30A49C5FF1EC562FAE39B38F43";
                "DSCResources\UserConfigProviderModVersion2\UserConfigProviderModVersion2.schema.mof" = "679318201B012CC5936B29C095956B2131FAF828C0CCA4342A5914F721480FB9";
                "ٿ ڀ ځ ڂ ڃ ڄ څ چ ڇ ڈ ډ ڊ ڋ ڌ ڍ ڎ ڏ ڐ ڑ.txt" = "EFD0AE8FF12C7387D51FFC03259B60E06DA012BF7D3B7B9D3480FAB2864846CE";
                "ɥ ɦ ɧ ɨ ɩ ɪ ɫ ɬ.txt" = "9FB57660EDD8DA898A9F1E7F5A36B8B760B4A21625F9968D87A32A55B3546BF9"}

            # Create Test Files with unicode characters in names and content
            $unicodeTempDir = Join-Path -Path $testDataPath -ChildPath "UnicodeTestDir"
            $null = New-Item -ItemType Directory -Path $unicodeTempDir -Force

            $null = New-Item -ItemType File -Path "$unicodeTempDir\ɥ ɦ ɧ ɨ ɩ ɪ ɫ ɬ.txt" -Force -ErrorAction SilentlyContinue
            $null = Add-Content -Path "$unicodeTempDir\ɥ ɦ ɧ ɨ ɩ ɪ ɫ ɬ.txt" -Value "Testing unicode"
            $null = Out-File -FilePath "$unicodeTempDir\ɥ ɦ ɧ ɨ ɩ ɪ ɫ ɬ.txt" -Encoding unicode -InputObject "ɗ ɘ ə ɚ ɛ ɜ ɝ ɞ ɟ ɠ ɡ ɢ ɣ ɤ ɥ ɦ ɧ ɨ ɩ ɪ ɫ ɬ ɭ ɮ ɯ ɰ ɱ ɲ ɳ ɴ ɵ ɶ ɷ ɸ ɹ ɺ ɻ ɼ ɽ ɾ ɿ ʀ ʁ ʂ ʃ ʄ ʅ" -Append
            $null = New-Item -ItemType File -Path "$unicodeTempDir\ٿ ڀ ځ ڂ ڃ ڄ څ چ ڇ ڈ ډ ڊ ڋ ڌ ڍ ڎ ڏ ڐ ڑ.txt" -Force -ErrorAction SilentlyContinue
            $null = Out-File -FilePath "$unicodeTempDir\ٿ ڀ ځ ڂ ڃ ڄ څ چ ڇ ڈ ډ ڊ ڋ ڌ ڍ ڎ ڏ ڐ ڑ.txt" -Encoding unicode -InputObject "ਅ ਆ ਇ ਈ ਉ ਊ ਏ ਐ ਓ ਔ ਕ ਖ ਗ ਘ ਙ ਚ ਛ ਜ ਝ ਞ ਟ ਠ ਡ ਢ ਣ ਤ ਥ ਦ ਧ ਨ ਪ ਫ ਬ ਭ ਮ ਯ ਰ ਲ ਲ਼ ਵ " -Append
            $null = Out-File -FilePath "$unicodeTempDir\ٿ ڀ ځ ڂ ڃ ڄ څ چ ڇ ڈ ډ ڊ ڋ ڌ ڍ ڎ ڏ ڐ ڑ.txt" -Encoding unicode -InputObject  "அ ஆ இ ஈ உ ஊ எ ஏ ஐ ஒ ஓ ஔ க ங ச ஜ ஞ ட ண த ந ன ப ம ய ர ற ல ள ழ வ ஷ ஸ ஹ " -Append

            $catalogPath = "$env:TEMP\క ఖ గ ఘ ఙ చ ఛ జ ఝ ఞ.cat"
            $catalogDataPath = @("$testDataPath\UserConfigProv\", "$unicodeTempDir\ٿ ڀ ځ ڂ ڃ ڄ څ چ ڇ ڈ ډ ڊ ڋ ڌ ڍ ڎ ڏ ڐ ڑ.txt" ,"$unicodeTempDir\ɥ ɦ ɧ ɨ ɩ ɪ ɫ ɬ.txt")

            try
            {
                $null = New-FileCatalog -Path $catalogDataPath -CatalogFilePath $catalogPath -CatalogVersion 2.0
                $result = Test-FileCatalog -Path $catalogDataPath -CatalogFilePath $catalogPath -Detailed
            }
            finally
            {
                Remove-Item $unicodeTempDir -Recurse -Force -ErrorAction SilentlyContinue
                Remove-Item "$catalogPath" -Force -ErrorAction SilentlyContinue
            }

            $result.Status | Should -Be "Valid"
            $result.Signature.Status | Should -Be "NotSigned"
            $result.HashAlgorithm | Should -Be "SHA256"
            $result.CatalogItems.Count | Should -Be 11
            $result.PathItems.Count | Should -Be 11
            CompareHashTables $result.CatalogItems $result.PathItems
            CompareHashTables $result.CatalogItems $expectedPathsAndHashes
        }
    }

    Context "NewAndTestCatalogTests NegativeTestCases when creation or validation Fails"{

        AfterEach {
            Remove-Item "$script:catalogPath" -Force -ErrorAction SilentlyContinue
            Remove-Item "$env:temp\UserConfigProv" -Force -Recurse -ErrorAction SilentlyContinue
        }

        It "TestCatalogWhenNewFileAddedtoFolderBeforeValidation" {

            $script:catalogPath = "$env:TEMP\TestCatalogWhenNewFileAddedtoFolderBeforeValidation.cat"
            $null = New-FileCatalog -Path $testDataPath\UserConfigProv\ -CatalogFilePath $script:catalogPath -CatalogVersion 2.0
            $null = Copy-Item $testDataPath\UserConfigProv $env:temp -Recurse -ErrorAction SilentlyContinue
            $null = New-Item $env:temp\UserConfigProv\DSCResources\NewFile.txt -ItemType File
            Add-Content $env:temp\UserConfigProv\DSCResources\NewFile.txt -Value "More Data" -Force
            $result = Test-FileCatalog -Path $env:temp\UserConfigProv -CatalogFilePath $script:catalogPath -Detailed

            $result.Status | Should -Be "ValidationFailed"
            $result.CatalogItems.Count | Should -Be 9
            $result.PathItems.Count | Should -Be 10
            $result.CatalogItems.ContainsKey("DSCResources\NewFile.txt") | Should -BeFalse
            $result.PathItems.ContainsKey("DSCResources\NewFile.txt") | Should -BeTrue

            # By Skipping the new added file validation will pass
            $result = Test-FileCatalog -Path $env:temp\UserConfigProv -CatalogFilePath $script:catalogPath -Detailed -FilesToSkip "NewFile.txt"
            $result.Status | Should -Be "Valid"
        }

        It "TestCatalogWhenNewFileDeletedFromFolderBeforeValidation" {

            $script:catalogPath = "$env:TEMP\TestCatalogWhenNewFileDeletedFromFolderBeforeValidation.cat"
            $null = New-FileCatalog -Path $testDataPath\UserConfigProv\ -CatalogFilePath $script:catalogPath -CatalogVersion 1.0
            $null = Copy-Item $testDataPath\UserConfigProv $env:temp -Recurse -ErrorAction SilentlyContinue
            del $env:temp\UserConfigProv\DSCResources\UserConfigProviderModVersion1\UserConfigProviderModVersion1.psm1 -Force -ErrorAction SilentlyContinue
            $result = Test-FileCatalog -Path $env:temp\UserConfigProv -CatalogFilePath $script:catalogPath -Detailed

            $result.Status | Should -Be "ValidationFailed"
            $result.CatalogItems.Count | Should -Be 9
            $result.PathItems.Count | Should -Be 8
            $result.CatalogItems.ContainsKey("DSCResources\UserConfigProviderModVersion1\UserConfigProviderModVersion1.psm1") | Should -BeTrue
            $result.PathItems.ContainsKey("DSCResources\UserConfigProviderModVersion1\UserConfigProviderModVersion1.psm1") | Should -BeFalse

            # By Skipping the deleted file validation will pass
            $result = Test-FileCatalog -Path $env:temp\UserConfigProv -CatalogFilePath $script:catalogPath -Detailed -FilesToSkip "UserConfigProviderModVersion1.psm1"
            $result.Status | Should -Be "Valid"
        }

        It "TestCatalogWhenFileContentModifiedBeforeValidation" {

            $script:catalogPath = "$env:TEMP\TestCatalogWhenFileContentModifiedBeforeValidation.cat"
            $null = New-FileCatalog -Path $testDataPath\UserConfigProv\ -CatalogFilePath $script:catalogPath -CatalogVersion 1.0
            $null = Copy-Item $testDataPath\UserConfigProv $env:temp -Recurse -ErrorAction SilentlyContinue
            Add-Content $env:temp\UserConfigProv\DSCResources\UserConfigProviderModVersion1\UserConfigProviderModVersion1.psm1 -Value "More Data" -Force
            $result = Test-FileCatalog -Path $env:temp\UserConfigProv -CatalogFilePath $script:catalogPath -Detailed

            $result.Status | Should -Be "ValidationFailed"
            $result.CatalogItems.Count | Should -Be 9
            $result.PathItems.Count | Should -Be 9
            $catalogHashValue = $result.CatalogItems["DSCResources\UserConfigProviderModVersion1\UserConfigProviderModVersion1.psm1"]
            $pathHashValue = $result.PathItems["DSCResources\UserConfigProviderModVersion1\UserConfigProviderModVersion1.psm1"]
            ($catalogHashValue -eq $pathHashValue) | Should -BeFalse

            # By Skipping the file with modifed contents validation will pass
            $result = Test-FileCatalog -Path $env:temp\UserConfigProv -CatalogFilePath $script:catalogPath -Detailed -FilesToSkip "UserConfigProviderModVersion1.psm1"
            $result.Status | Should -Be "Valid"
        }
    }

    Context "TestCatalog Skip Validation Tests"{

        AfterEach {
            Remove-Item "$script:catalogPath" -Force -ErrorAction SilentlyContinue
        }

        It "TestCatalogSkipSingleFileDuringValidation" {

            $script:catalogPath = "$env:TEMP\TestCatalogSkipSingleFileDuringValidation.cat"
            $null = New-FileCatalog -Path $testDataPath\UserConfigProv\ -CatalogFilePath $script:catalogPath -CatalogVersion 2.0
            $result = Test-FileCatalog -Path $testDataPath\UserConfigProv\ -CatalogFilePath $script:catalogPath -FilesToSkip "scriptdsc.schema"
            $result | Should -Be "Valid"
        }

        It "TestCatalogSkipCertainFileTypeDuringValidation" {

            $script:catalogPath = "$env:TEMP\TestCatalogSkipCertainFileTypeDuringValidation.cat"
            $null = New-FileCatalog -Path $testDataPath\UserConfigProv\ -CatalogFilePath $script:catalogPath -CatalogVersion 2.0
            $result = Test-FileCatalog -Path $testDataPath\UserConfigProv\ -CatalogFilePath $script:catalogPath -FilesToSkip "*.mof"
            $result | Should -Be "Valid"
        }

        It "TestCatalogSkipWildCardPatternDuringValidation" {

            $script:catalogPath = "$env:TEMP\TestCatalogSkipWildCardPatternDuringValidation.cat"
            $null = New-FileCatalog -Path $testDataPath\UserConfigProv\ -CatalogFilePath $script:catalogPath -CatalogVersion 1.0
            $result = Test-FileCatalog -Path $testDataPath\UserConfigProv\ -CatalogFilePath $script:catalogPath -FilesToSkip "UserConfigProvider*.psm1"
            $result | Should -Be "Valid"
        }

        It "TestCatalogSkipMultiplePattensDuringValidation" {

            $script:catalogPath = "$env:TEMP\TestCatalogSkipMultiplePattensDuringValidation.cat"
            $null = New-FileCatalog -Path $testDataPath\UserConfigProv\ -CatalogFilePath $script:catalogPath -CatalogVersion 1.0
            $result = Test-FileCatalog -Path $testDataPath\UserConfigProv\ -CatalogFilePath $script:catalogPath -FilesToSkip "*.psd1","UserConfigProviderModVersion2.psm1","*ModVersion1.schema.mof"
            $result | Should -Be "Valid"
        }

        It "New-FileCatalog -WhatIf does not create file" {
            $catalogPath = Join-Path "TestDrive:" "TestCatalogWhatIfForNewFileCatalog.cat"
            New-FileCatalog -CatalogFilePath $catalogPath -WhatIf
            $catalogPath | Should -Not -Exist
        }
    }
}

} finally {
    $global:PSdefaultParameterValues = $defaultParamValues
}
