##
##  Remoting Implicit script tests
##

$path = $PSScriptRoot
if ($path -eq $null) { $path = Split-Path $MyInvocation.InvocationName }
if ($path -eq $null) { $path = $pwd }

import-module (join-path $path ".\RemotingCommon.psm1")


Describe "Implicit remoting and CIM cmdlets with AllSigned and Restricted policy" -tags 'Innerloop', 'P1' {

    try
    {
        ##############################################################################
        # TEST SETUP - CREATE TEMP DIRECTORY
        #

        $modulesDir = $env:TEMP -split ';' | select -first 1

        $tempdir = join-path $modulesDir ([IO.Path]::GetRandomFileName())
        mkdir $tempdir | Out-Null

        $fileName = [io.path]::GetFileName($tempdir)


        ##############################################################################
        # GET CERTIFICATE
        #

        $tempName = "$env:temp\signedscript_$(get-random).ps1"
        "123456" >$tempName
        $cert = $null
        foreach ($thisCertificate in (dir cert:\ -rec -codesigning))
        {
	        $null = set-authenticodesignature $tempName -cert $thisCertificate
	        if ((get-authenticodesignature $tempName).Status -eq "Valid")
	        {
		        $cert = $thisCertificate
		        break
	        }
        }

        if ($cert -eq $null) { return }

        # ensure the cert is trusted
        if (-not (Test-Path "cert:\currentuser\TrustedPublisher\$($cert.Thumbprint)"))
        {
            $store = New-Object System.Security.Cryptography.X509Certificates.X509Store "TrustedPublisher"
            $store.Open("ReadWrite")
            $store.Add($cert)
            $store.Close()
        }


        ##############################################################################
        # TEST - Verifying that Import-PSSession signs the files
        #

        $oldExecutionPolicy = Get-ExecutionPolicy -Scope Process
        Set-ExecutionPolicy AllSigned -Scope Process

        $s = New-PSSession

        $importedModule = Import-PSSession $s Get-Variable -Prefix Remote -Certificate $cert -AllowClobber
	    It "Verifies that Import-PSSession works in AllSigned if Certificate is used" {
		    $importedModule | Should Not Be $null
	    }

        $importedModule | Remove-Module -Force -ErrorAction SilentlyContinue

	    $caught = $false
	    try 
        {
	        $importedModule = Import-PSSession $s Get-Variable -Prefix Remote -AllowClobber
	    }
	    catch { $caught = $true; }
		    
        It "Verifies security error when Certificate parameter is not used" {
		    $caught | Should Be $true
	    }
    }
    finally
    {
        if ($oldExecutionPolicy -ne $null) { Set-ExecutionPolicy $oldExecutionPolicy -Scope Process }
        if ($s -ne $null) { Remove-PSSession $s -ErrorAction SilentlyContinue }
        if ($tempdir -ne $null) {  remove-item $tempdir -recurse -force -ea silentlycontinue }
    }
}

Describe "Tests Import-Proxy cmdlet function works with types unavailable on the client" -tags 'Innerloop', 'P1' {

    $typeDefinition = @"
    namespace MyTest
    {
	    public enum MyEnum
	    {
		    Value1 = 1,
		    Value2 = 2
	    }
    }
"@

    try
    {
        $r = New-PSSession
        icm -Session $r -Script { Add-Type -TypeDefinition $args[0] } -Args $typeDefinition
        icm -Session $r -Script { function foo { param([MyTest.MyEnum][Parameter(Mandatory = $true)]$x) $x } }

        $module = Import-PSSession -Session $r -CommandName foo -AllowClobber

	    It "Verifies client-side unavailable enum is treated as an int" {
		    (foo -x "Value2") | Should Be 2
	    }
	    It "Verifies client-side unavailable enum is to-string-ed appropriately" {
		    ((foo -x "Value2").ToString()) | Should Be "Value2"
	    }
    }
    finally
    {
        if ($r -ne $null) { Remove-PSSession $r -ErrorAction SilentlyContinue }
        if ($module -ne $null) { Remove-Module $module -Force -ErrorAction SilentlyContinue }
    }
}


Describe "Cmdlet help from remote session" -tags 'Innerloop', 'P1' {

    try
    {
        $s = New-PSSession
        $module = import-pssession $s -name select-object -prefix my -AllowClobber
        $gcmOutPut = (get-command select-myobject).Name
        $getHelpOutPut = (get-help select-myobject).Name

	    It "Verifies that get-help name for remote proxied commands matches the get-command name" {
		    $gcmOutPut | Should Be $getHelpOutPut
	    }
    }
    finally
    {
        if ($s -ne $null) { Remove-PSSession $s -ErrorAction SilentlyContinue }
        if ($module -ne $null) { Remove-Module $module -Force -ErrorAction SilentlyContinue }
    }
}


Describe "Import-Proxy Cmdlet error handling" -tags 'Innerloop', 'P1' {

    try
    {
        $s = New-PSSession

        ##############################################################################
        # TEST: error message for broken aliases

        Invoke-Command $s { set-alias BrokenAlias NonExistantCommand }
        $module = Import-PSSession $s -CommandName:BrokenAlias -CommandType:All -ErrorAction SilentlyContinue -ErrorVariable expectedError -AllowClobber

	    It "Verifies that broken alias results in one error" {
		    $expectedError | Should Not Be NullOrEmpty
	    }
	    It "Verifies that broken alias error contains expected 'BrokenAlias' name" {
		    $expectedError[0].ToString().Contains("BrokenAlias") | Should Be $true
	    }

        if ($module -ne $null) { Remove-Module $module -Force -ErrorAction SilentlyContinue; $module = $null }
        Invoke-Command $s { del alias:BrokenAlias }


        ##############################################################################
        # TEST: content and format of proxied error message (Windows 7: #319080)

        $module = Import-PSSession -Session:$s -Name:Get-Variable -Prefix My -AllowClobber

        # non-terminating error
        $results = Get-MyVariable blah,pid 2>&1
	    It "Verifies that returned PID is not for this session" {
		    ($results[1]).Value | Should Not Be $pid
	    }
        $errorString = $results[0] | Out-String
        It "Verifies error message for variable blah" {
            ($errorString -like "*VariableNotFound*") | Should Be $true
        }

        #terminating error
        $results = Get-MyVariable pid -Scope blah 2>&1
	    It "Verifies that remote session pid is not returned" {
		    $results.Count | Should Be 1
	    }
        $errorString = $results[0] | Out-String
        It "Verifes error message for incorrect Scope parameter argument" {
            ($errorString -like "*Argument*") | Should Be $true
        }

        if ($module -ne $null) { Remove-Module $module -Force -ErrorAction SilentlyContinue; $module = $null }


        ##############################################################################
        # TEST: ordering of a sequence of error and output messages (Windows 7: #405065)

        icm $s {function foo1{1; write-error 2; 3; write-error 4; 5; write-error 6}}
        $module = Import-PSSession $s -CommandName foo1 -AllowClobber

        $icmErr = $($icmOut = icm $s { foo1 }) 2>&1
        $proxiedErr = $($proxiedOut = foo1) 2>&1
        $proxiedOut2 = foo1 2>$null

        $icmOut = "$icmOut"
        $icmErr = "$icmErr"
        $proxiedOut = "$proxiedOut"
        $proxiedOut2 = "$proxiedOut2"
        $proxiedErr = "$proxiedErr"

	    It "Verifies proxied output = proxied output 2" {
		    $proxiedOut2 | Should Be $proxiedOut
	    }
	    It "Verifies proxied output = icm output (for mixed error and output results)" {
		    $icmOut | Should Be $proxiedOut
	    }
	    It "Verifies proxied error = icm error (for mixed error and output results)" {
		    $icmErr | Should Be $proxiedErr
	    }

        $icmOrder = icm $s { foo1 } 2>&1 | out-string
        $proxiedOrder = foo1 2>&1 | out-string

	    It "Verifies proxied order = icm order (for mixed error and output results)" {
		    $icmOrder | Should Be $proxiedOrder
	    }

        if ($module -ne $null) { Remove-Module $module -Force -ErrorAction SilentlyContinue; $module = $null }


        ##############################################################################
        # TEST: is WarningVariable parameter working with implicit remoting (Windows 8: #44861)

        $m = Import-PSSession $s -CommandName Write-Warning -Prefix Remote -AllowClobber
        $global:myWarningVariable = @()
        Write-RemoteWarning MyWarning -WarningVariable global:myWarningVariable
	    It "Verifies WarningVariable" {
		    ([string]($myWarningVariable[0])) | Should Be 'MyWarning'
	    }
    }
    finally
    {
        if ($s -ne $null) { Remove-PSSession $s -ErrorAction SilentlyContinue }
        if ($module -ne $null) { Remove-Module $module -Force -ErrorAction SilentlyContinue; $module = $null }
    }
}


Describe "Tests Export-PSSession" -tags 'Innerloop', 'P1' {

    function Get-TempModuleFile
    {
	    [IO.Path]::Combine([IO.Path]::GetTempPath(), [Guid]::NewGuid().ToString())
    }

    try
    {
        $sessionOption = New-PSSessionOption -ApplicationArguments @{myTest="MyValue"}
        $s = New-PSSession -SessionOption $sessionOption

        $file = Get-TempModuleFile
        $results = Export-PSSession -Session $s -CommandName Get-Variable -AllowClobber -ModuleName $file
	    It "Verifies Export-PSSession creates a file/directory" {
		    @(Get-Item $file).Count | Should Be 1
	    }
	    It "Verifies Export-PSSession creates a psd1 file" {
		    ($results | ?{ $_.Name -like "*$(Split-Path -Leaf $file).psd1" }) | Should Be $true
	    }
	    It "Verifies Export-PSSession creates a psm1 file" {
		    ($results | ?{ $_.Name -like "*.psm1" }) | Should Be $true
	    }
	    It "Verifies Export-PSSession creates a ps1xml file" {
		    ($results | ?{ $_.Name -like "*.ps1xml" }) | Should Be $true
	    }
        $oldTimestamp = $($results | Select -First 1).LastWriteTime

        #
        # error when trying to overwrite an existing directory
        #

        $msg = $null
        try
        {
	        Export-PSSession -Session $s -CommandName Get-Variable -AllowClobber -ModuleName $file -EA SilentlyContinue -ErrorVariable expectedError
        }
        catch { }
	    It "Verifies that Export-PSSession fails when a module directory already exists" {
		    $expectedError | Should Not Be NullOrEmpty
	    }
        $msg = [string]($expectedError[0])
	    It "Verifies Error contains reference to the directory that already exists" {
		    ($msg -like "*$file*") | Should Be $true
	    }

        $newResults = Export-PSSession -Session $s -CommandName Get-Variable -AllowClobber -ModuleName $file -Force
	    It "Verifies that Export-PSSession returns 4 files" {
		    (@($newResults).Count) | Should Be 4
	    }
        $newResults | % {
            It "Verifies that Export-PSSession creates *new* files" {
	            $_.LastWriteTime | Should BeGreaterThan $oldTimestamp
            }
        }

        #
        # the module is usable when the original runspace is still around
        #

        $module = import-Module $file -PassThru
	    It "Verifies that proxy returns remote pid" {
		    (Get-Variable -Name:pid).Value | Should Not Be $pid
	    }
        Remove-Module $module -Force -ErrorAction SilentlyContinue
	    It "Verfies Remove-Module doesn't remove user's runspace" {
		    (Get-PSSession -InstanceId $s.InstanceId) | Should Not Be NullOrEmpty
	    }

        #
        # only explicitly imported/exported commands are modifier
        #
	    It "Verifies that no Get-Variable function before this test" {
		    ((Get-Item function:Get-Variable -ErrorAction SilentlyContinue) -eq $null) | Should Be $true
	    }

        set-item function:global:Get-Variable { param($Name) Microsoft.PowerShell.Utility\Get-Variable -Name:$Name }
        $module = import-Module $file -PassThru -Force -Function @()
	    It "Verifes that our global function didn't get overwritten" {
		    (Get-Variable -Name:pid).Value | Should Be $pid
	    }
	    It "Verifies module.ExportedFunctions.Contains('Get-Variable')" {
		    ($module.ExportedFunctions.ContainsKey("Get-Variable")) | Should Be $true
	    }
        Remove-Module $module

        Remove-Item function:global:Get-Variable -ErrorAction SilentlyContinue
        Remove-Item function:script:Get-Variable -ErrorAction SilentlyContinue
        Remove-Item function:Get-Variable -ErrorAction SilentlyContinue
	    It "Verifies no Get-Variable function after this test" {
		    ((Get-Item function:Get-Variable -ErrorAction SilentlyContinue) -eq $null) | Should Be $true
	    }
    }
    finally
    {
        if ($s -ne $null) { Remove-PSSession $s -ErrorAction SilentlyContinue }
        if ($module -ne $null) { Remove-Module $module -Force -ErrorAction SilentlyContinue }
    }


    try
    {
        ##############################################################################
        # Export-PSSession tests continued - runspace created by the module

        # the module is usable when the original runspace is no longer around

        $testUI = $null
        try
        {
	        if ($host.Name -eq "test host")
	        {
		        $testUI = $host.UI.GetType().GetField("externalUI", "IgnoreCase,Instance,NonPublic").GetValue($host.UI)
		        $oldIgnoreWrites = $testUI.IgnoreWrites
		        $testUI.IgnoreWrites = $true # we expect "Creating a new runspace for implicit remoting of 'Get-Variable' command..." message
	        }
	        $module = import-Module $file -PassThru -Force
		    It "Verifies proxy should return remote pid" {
		        (Get-Variable -Name:pid).Value | Should Not Be $pid
	        }
        }
        finally
        {
	        if ($testUI)
	        {
		        $testUI.IgnoreWrites = $oldIgnoreWrites
	        }
        }

        # let's verify if ApplicationArguments got preserved correctly
        $s = & $module { $script:PSSession }
	    It "Verifies ApplicationArguments got preserved correctly" {
		    $(icm $s { $PSSenderInfo.ApplicationArguments.MyTest }) | Should Be "MyValue"
	    }

        # removing the module should remove the implicitly/magically created runspace
        Remove-Module $module
	    It "Verifies Remove-Module removed automatically created runspace" {
		    ((Get-PSSession -InstanceId $s.InstanceId -ErrorAction SilentlyContinue) -eq $null) | Should Be $true
	    }
	    It "Verifies Runspace is closed after removing module from Export-PSSession that got initialized with an internal r-space" {
		    ($s.Runspace.RunspaceStateInfo.ToString()) | Should Be "Closed"
	    }

        ##############################################################################
        # Export-PSSession tests continued - runspace created by the module with explicit session options

        try
        {
	        if ($host.Name -eq "test host")
	        {
		        $testUI = $host.UI.GetType().GetField("externalUI", "IgnoreCase,Instance,NonPublic").GetValue($host.UI)
		        $oldIgnoreWrites = $testUI.IgnoreWrites
		        $testUI.IgnoreWrites = $true # we expect "Creating a new runspace for implicit remoting of 'Get-Variable' command..." message
	        }
	        $explicitSessionOption = New-PSSessionOption -Culture fr-FR -UICulture de-DE
	        $module = import-Module $file -PassThru -Force -Args $null,$explicitSessionOption
		    It "Verifies proxy should return remote pid" {
		        (Get-Variable -Name:pid).Value | Should Not Be $pid
	        }
        }
        finally
        {
	        if ($testUI)
	        {
		        $testUI.IgnoreWrites = $oldIgnoreWrites
	        }
        }

        # culture settings should be taken from the explicitly passed session options
	    It "Verifies proxy returns modified culture" {
		    (Get-Variable -Name:PSCulture).Value | Should Be "fr-FR"
	    }
	    It "Verifies proxy returns modified culture" {
		    (Get-Variable -Name:PSUICulture).Value | Should Be "de-DE"
	    }

        # removing the module should remove the implicitly/magically created runspace
        $s = & $module { $script:PSSession }
        Remove-Module $module
	    It "Verifies Remove-Module removes automatically created runspace" {
		    ((Get-PSSession -InstanceId $s.InstanceId -ErrorAction SilentlyContinue) -eq $null) | Should Be $true
	    }
	    It "Verifies Runspace is closed after removing module from Export-PSSession that got initialized with an internal r-space" {
		    ($s.Runspace.RunspaceStateInfo.ToString()) | Should Be "Closed"
	    }

        ##############################################################################
        # Export-PSSession tests continued - passing a runspace into module

        $s = New-PSSession #
        $module = import-Module $file -PassThru -Force -Args $s

	    It "Verifies proxy returns remote pid" {
		    (Get-Variable -Name:pid).Value | Should Not Be $pid
	    }
	    It "Verifies switch parameters work" {
		    (Get-Variable -Name:pid -ValueOnly) | Should Not Be $pid
	    }
	    It "Verifies Adding a module affects runspace's state" {
		    ($s.Runspace.RunspaceStateInfo.ToString()) | Should Be "Opened"
	    }
        Remove-Module $module
	    It "Verifies Runspace stays opened after removing module from Export-PSSession that got initialized with an external runspace" {
		    ($s.Runspace.RunspaceStateInfo.ToString()) | Should Be "Opened"
	    }
    }
    finally
    {
        if ($s -ne $null) { Remove-PSSession $s -ErrorAction SilentlyContinue }
        if ($module -ne $null) { Remove-Module $module -Force -ErrorAction SilentlyContinue }
        if ($file -ne $null) { Remove-Item $file -Force -Recurse -ErrorAction SilentlyContinue }
    }
}


Describe "Import-Proxy with FormatAndTypes" -tags 'Innerloop', 'P1' {

	function CreateTempPs1xmlFile
	{
		do {
			$tmpFile = [IO.Path]::Combine([IO.Path]::GetTempPath(), [IO.Path]::GetRandomFileName()) + ".ps1xml";
		} while ([io.file]::exists($tmpFile))
		$tmpFile
	}

	function CreateTypeFile {
	$tmpFile = CreateTempPs1xmlFile
@"
<Types>
	    <Type>
		<Name>System.Management.Automation.Host.Coordinates</Name>
		    <Members>
			    <NoteProperty>
				<Name>MyTestLabel</Name>
				<Value>123</Value>
			    </NoteProperty>
		    </Members>
	    </Type>
	    <Type>
		    <Name>MyTest.Root</Name>
		    <Members>
		    <MemberSet>
			<Name>PSStandardMembers</Name>
			<Members>
			    <NoteProperty>
				<Name>SerializationDepth</Name>
				<Value>1</Value>
			    </NoteProperty>
			</Members>
		        </MemberSet>
		    </Members>
	    </Type>
	    <Type>
		    <Name>MyTest.Son</Name>
		    <Members>
		    <MemberSet>
			<Name>PSStandardMembers</Name>
			<Members>
			    <NoteProperty>
				<Name>SerializationDepth</Name>
				<Value>1</Value>
			    </NoteProperty>
			</Members>
		        </MemberSet>
		    </Members>
	    </Type>
	    <Type>
		    <Name>MyTest.Grandson</Name>
		    <Members>
		    <MemberSet>
			<Name>PSStandardMembers</Name>
			<Members>
			    <NoteProperty>
				<Name>SerializationDepth</Name>
				<Value>1</Value>
			    </NoteProperty>
			</Members>
		        </MemberSet>
		    </Members>
	    </Type>
	</Types>
"@ | set-content $tmpFile
	    $tmpFile
	    }

	function CreateFormatFile {
	$tmpFile = CreateTempPs1xmlFile
	@"
    <Configuration>
	        <ViewDefinitions>
		    <View>
		        <Name>MySizeView</Name>
		        <ViewSelectedBy>
			    <TypeName>System.Management.Automation.Host.Size</TypeName>
		        </ViewSelectedBy>
		        <TableControl>
			    <TableHeaders>
			        <TableColumnHeader>
				    <Label>MyTestWidth</Label>
			        </TableColumnHeader>
			        <TableColumnHeader>
				    <Label>MyTestHeight</Label>
			        </TableColumnHeader>
			    </TableHeaders>
			    <TableRowEntries>
			        <TableRowEntry>
				    <TableColumnItems>
				        <TableColumnItem>
					    <PropertyName>Width</PropertyName>
				        </TableColumnItem>
				        <TableColumnItem>
					    <PropertyName>Height</PropertyName>
				        </TableColumnItem>
				    </TableColumnItems>
			        </TableRowEntry>
			     </TableRowEntries>
		        </TableControl>
		    </View>
	        </ViewDefinitions>
	    </Configuration>
"@ | set-content $tmpFile
	    $tmpFile
	    }

    try
    {
	    $s = New-PSSession

	    ##############################################################################
	    # TEST: importing format file works

	    $date = Get-Date
	    $formattingScript = { new-object System.Management.Automation.Host.Size | %{ $_.Width = 123; $_.Height = 456; $_ } | Out-String }
	    $typeDefinition = @"
	    namespace MyTest
	    {
		    public enum MyEnum
		    {
			    Value1 = 1,
			    Value2 = 2
		    }
	    }
"@

	    icm -Session $s -Script { Add-Type -TypeDefinition $args[0] } -Args $typeDefinition
	    icm -Session $s -Script { function foo { param([MyTest.MyEnum][Parameter(Mandatory = $true)]$x) $x } }

	    $originalLocalFormatting = & $formattingScript
	    $originalRemoteFormatting = icm $s $formattingScript
		It "original local and remote formatting should be equal (sanity check)" {
		    $originalLocalFormatting | Should Be $originalRemoteFormatting
	    }

	    $formatFile = CreateFormatFile
	    icm $s { param($file) update-formatdata $file } -args $formatFile
	    $modifiedRemoteFormatting = icm $s $formattingScript
		It "original remote and modified remote formatting should not be equal (sanity check)" {
		    $originalRemoteFormatting | Should Not Be $modifiedRemoteFormatting
	    }

	    $module = import-pssession -Session $s -CommandName @() -FormatTypeName * -AllowClobber
	    $importedLocalFormatting = & $formattingScript
		It "modified remote and imported local should be equal" {
		    $modifiedRemoteFormatting | Should Be $importedLocalFormatting
	    }

	    Remove-Module $module
	    $unimportedLocalFormatting = & $formattingScript
		It "original local and unimported local should be equal" {
		    $originalLocalFormatting | Should Be $unimportedLocalFormatting
	    }

	    ##############################################################################
	    # TEST: updating type table in a middle of a command has effect on serializer

	    $typeFile = CreateTypeFile

	    $results = icm $s -args $typeFile { param($file)
		    new-object System.Management.Automation.Host.Coordinates
		    update-typedata $file
		    new-object System.Management.Automation.Host.Coordinates
	    }

		It "Should get 2 deserialized S.M.A.H.Coordinates objects" {
		    ($results.Count) | Should Be 2
	    }
		It "First object shouldn't have the additional ETS note property" {
		    ($results[0].MyTestLabel -eq $null) | Should Be $true
	    }
		It "Second object should have the additional ETS note property" {
		    ($results[1].MyTestLabel) | Should Be 123
	    }

	    ##############################################################################
	    # TEST: implicit remoting works even when types.ps1xml is missing on the client

	    $typeDefinition = @"
	    namespace MyTest
	    {
		    public class Root
		    {
			    public Root(string s) { text = s; }
			    public Son Son = new Son();
			    public string text;
		    }

		    public class Son
		    {
			    public Grandson Grandson = new Grandson();
		    }

		    public class Grandson
		    {
			    public string text = "Grandson";
		    }
	    }
"@

	    icm -Session $s -Script { Add-Type -TypeDefinition $args[0] } -Args $typeDefinition
	    icm -Session $s -Script { function foo { new-object MyTest.Root "root" } }
	    icm -Session $s -Script { function bar { param([Parameter(Mandatory = $true, ValueFromPipelineByPropertyName = $true)]$Son) $Son.Grandson.text } }

	    $m = import-pssession $s foo,bar -AllowClobber

	    $x = foo
		It "Serialization works for top-level properties" {
		    ($x.text) | Should Be "root"
	    }
		It "Serialization settings works for deep properties" {
		    ($x.Son.Grandson.text) | Should Be "Grandson"
	    }
	    $y = foo | bar
		It "Serialization settings are preserved even if types.ps1xml is missing on the client" {
		    $y | Should Be "Grandson"
	    }
    }
    finally
    {
	    if ($s -ne $null) { Remove-PSSession $s -ErrorAction SilentlyContinue }
        if ($formatFile -ne $null) { Remove-Item $formatFile -Force -ErrorAction SilentlyContinue }
        if ($typeFile -ne $null) { Remove-Item $typeFile -Force -ErrorAction SilentlyContinue }
        if ($m -ne $null) { Remove-Module $m -Force -ErrorAction SilentlyContinue }
    }
}


Describe "Import-PSSession functional tests" -tags 'Innerloop', 'P1' {

    try
    {
        $s = New-PSSession

        # define a remote function
        Invoke-Command -Session $s { function MyFunction { param($x) "x = '$x'; args = '$args'" } }

        # define a remote proxy script cmdlet
        $remoteCommandType = $ExecutionContext.InvokeCommand.GetCommand('Get-Variable', [System.Management.Automation.CommandTypes]::Cmdlet)
        $remoteProxyBody = [System.Management.Automation.ProxyCommand]::Create($remoteCommandType)
        $remoteProxyDeclaration = "function Get-VariableProxy { $remoteProxyBody }"
        Invoke-Command -Session $s { param($x) Invoke-Expression $x } -Arg $remoteProxyDeclaration
        $remoteAliasDeclaration = "set-alias gvalias Get-Variable"
        Invoke-Command -Session $s { param($x) Invoke-Expression $x } -Arg $remoteAliasDeclaration
        del alias:gvalias -force -ea silentlycontinue

        # import a remote function, script cmdlet, cmdlet, native application, alias
        $module = Import-PSSession -Session $s -Name MyFunction,Get-VariableProxy,Get-Variable,gvalias,cmd -AllowClobber -Type All
	    It "Import-PSSession should return a PSModuleInfo object" {
		    $module | Should Not Be NullOrEmpty
	    }
	    It "Import-PSSession should return a PSModuleInfo object" {
		    ($module -is [System.Management.Automation.PSModuleInfo]) | Should Not Be NullOrEmpty
	    }
	    It "Helper functions should not be imported" {
		    ((Get-Item function:*PSImplicitRemoting* -ErrorAction SilentlyContinue) -eq $null) | Should Be $true
	    }

        # test calling implicit remoting proxies
	    It "NoName-ef2e1dbb-6278-4c1f-99b8-5edd68aa1679" {
		    (MyFunction 1 2 3) | Should Be "x = '1'; args = '2 3'"
	    }

	    It "proxy should return remote pid" {
		    (Get-VariableProxy -Name:pid).Value | Should Not Be $pid
	    }
	    It "proxy should return remote pid" {
		    (Get-Variable -Name:pid).Value | Should Not Be $pid
	    }
	    It "proxy should return remote pid" {
		    $(& (Get-Command gvalias -Type alias) -Name:pid).Value | Should Not Be $pid
	    }

        Invoke-Command -Session $s { $env:TestImplicitRemotingVariable = 123 }
	    It "NoName-c8aeb5c8-2388-4d64-98c1-a9c6c218d404" {
		    (cmd.exe /c "echo TestImplicitRemotingVariable=%TestImplicitRemotingVariable%") | Should Be "TestImplicitRemotingVariable=123"
	    }

        # test what happens after the runspace is closed
        Remove-PSSession $s
        $s = $null

        # The loop below works around the fact that PSEventManager uses threadpool worker to queue event handler actions to process later
        # Usage of threadpool means that it is impossible to predict when the event handler will run (this is Windows 8 Bugs: #882977)
        $i = 0
        while ( ($i -lt 20) -and ($null -ne (Get-Module | ?{ $_.Path -eq $module.Path })) )
        {
	        $i++
	        Start-Sleep -Milliseconds 50
        }
        Write-Host "Workaround for bug 882977 used $i iterations"
	    It "Temporary module should be automatically removed after runspace is closed" {
		    ((Get-Module | ?{ $_.Path -eq $module.Path }) -eq $null) | Should Be $true
	    }

	    It "Temporary psm1 file should be automatically removed after runspace is closed" {
		    ((Get-Item $module.Path -ErrorAction SilentlyContinue) -eq $null) | Should Be $true
	    }

        # Check that the implicit remoting event has been removed.
        $implicitEventCount = 0
        foreach ($item in $ExecutionContext.Events.Subscribers)
        {
            if ($item.SourceIdentifier -match "Implicit remoting event") { $implicitEventCount++ }
        }
	    It "Event should be unregistered when the runspace is closed" {
		    0 | Should Be ($implicitEventCount)
	    }

	    It "Private functions from the implicit remoting module shouldn't get imported into global scope" {
		    0 | Should Be @(dir function:*Implicit* -ErrorAction SilentlyContinue).Count
	    }
    }
    finally
    {
        if ($s -ne $null) { Remove-PSSession $s -ErrorAction SilentlyContinue }
        if ($module -ne $null) { Remove-Module $module -ErrorAction SilentlyContinue }
    }
}


Describe "Implicit remoting parameter binding" -tags 'Innerloop', 'P1' {

    try
    {
	    $s = New-PSSession

	    ##############################################################################
	    # TEST: work-around for dynamic parameters via -ArgumentList parameter

	    $dynamicParametersTest = @'
	        function Get-DynamicParameters
	        {
		    [CmdletBinding(DefaultParameterSetName='DefaultParameterSet')]

		    param(
		        [Parameter(ParameterSetName = "DefaultParameterSet")]
		        $staticParameter
		    )

		    dynamicParam
		    {
		      $attributes = new-object System.Management.Automation.ParameterAttribute
		      $attributes.ParameterSetName = 'DefaultParameterSet'
		      $attributes.Mandatory = $false

		      $attributeCollection = new-object -Type System.Collections.ObjectModel.Collection``1[System.Attribute]
		      $attributeCollection.Add($attributes)

		      $dynParam1 = new-object System.Management.Automation.RuntimeDefinedParameter("dynamicParameter", [int], $attributeCollection)

		      $paramDictionary = new-object System.Management.Automation.RuntimeDefinedParameterDictionary
		      $paramDictionary.Add("dynamicParameter", $dynParam1)

		      return $paramDictionary
		    }

		    begin
		    {
		        "static=$staticParameter; dynamic=$($paramDictionary.dynamicParameter.Value)"
		    }
		    process {}
		    end {}
	        }
'@

	    # unfortunately -ArgumentList works only for cmdlets, not for script cmdlets
	    Invoke-Command -Session $s { param($x) Invoke-Expression $x } -Arg $dynamicParametersTest

	    ##############################################################################
	    # TEST: binding of ValueFromPipeline type of parameters

	    $module = Import-PSSession -Session:$s -Name:Get-Random -AllowClobber
	    $x = 1..20 | Get-Random -Count 5
		It "Binding of ValueFromPipeline should work" {
		    $x.Count | Should Be 5
	    }
	    Remove-Module $module

	    ##############################################################################
	    # TEST: pipeline-based parameter binding works even when client has no type constraints (Windows 7: #391157)

	    icm $s {
		    function foo {
			    [cmdletbinding(defaultparametersetname="string")]
			    param(
				    [string]
				    [parameter(ParameterSetName="string", ValueFromPipeline = $true)]
				    $string,

				    [ipaddress]
				    [parameter(ParameterSetName="ipaddress", ValueFromPipeline = $true)]
				    $ipaddress
			    )

			    "Bound parameter: $($myInvocation.BoundParameters.Keys | sort)"
		    }
	    }

		It "Sanity check (no remoting).  Pipeline binding works even if it relies on type constraints" {
		    $(icm $s {"s" | foo}) | Should Be "Bound parameter: string"
	    }
		It "Sanity check (no remoting).  Pipeline binding works even if it relies on type constraints" {
		    $(icm $s {[ipaddress]::parse("127.0.0.1") | foo}) | Should Be "Bound parameter: ipaddress"
	    }

	    $module = Import-PSSession $s foo -AllowClobber
		It "Pipeline binding works even if it relies on type constraints" {
		    $("s" | foo) | Should Be "Bound parameter: string"
	    }
		It "Pipeline binding works even if it relies on type constraints" {
		    $([ipaddress]::parse("127.0.0.1") | foo) | Should Be "Bound parameter: ipaddress"
	    }
	    Remove-Module $module

	    ##############################################################################
	    # TEST: pipeline-based parameter binding works even when client has no type constraints and parameterset is ambiguous (Windows 7: #430379)

	    icm $s {
		    function foo {
			    param(
				    [string]
				    [parameter(ParameterSetName="string", ValueFromPipeline = $true)]
				    $string,

				    [ipaddress]
				    [parameter(ParameterSetName="ipaddress", ValueFromPipeline = $true)]
				    $ipaddress
			    )

			    "Bound parameter: $($myInvocation.BoundParameters.Keys)"
		    }
	    }

		It "Sanity check (no remoting).  Pipeline binding works even if it relies on type constraints and parameter set is ambiguous" {
		    $(icm $s {"s" | foo}) | Should Be "Bound parameter: string"
	    }
		It "Sanity check (no remoting).  Pipeline binding works even if it relies on type constraints and parameter set is ambiguous" {
		    $(icm $s {[ipaddress]::parse("127.0.0.1") | foo}) | Should Be "Bound parameter: ipaddress"
	    }

	    $module = Import-PSSession $s foo -AllowClobber
		It "Pipeline binding works even if it relies on type constraints and parameter set is ambiguous" {
		    $("s" | foo) | Should Be "Bound parameter: string"
	    }
		It "Pipeline binding works even if it relies on type constraints and parameter set is ambiguous" {
		    $([ipaddress]::parse("127.0.0.1") | foo) | Should Be "Bound parameter: ipaddress"
	    }
	    Remove-Module $module

	    ##############################################################################
	    # TEST: pipeline-based parameter binding works even when one of parameters
	    #       that can be bound by pipeline gets bound by name

	    icm $s {
		    function foo {
			    param(
				    [DateTime]
				    [parameter(ValueFromPipeline = $true)]
				    $date,

				    [ipaddress]
				    [parameter(ValueFromPipeline = $true)]
				    $ipaddress
			    )

			    "Bound parameter: $($myInvocation.BoundParameters.Keys | sort)"
		    }
	    }

		It "Sanity check (no remoting)" {
		    $( icm $s { get-date | foo } ) | Should Be "Bound parameter: date"
	    }
		It "Sanity check (no remoting)" {
		    $( icm $s { [ipaddress]::parse("127.0.0.1") | foo } ) | Should Be "Bound parameter: ipaddress"
	    }
		It "Sanity check (no remoting)" {
		    $( icm $s { [ipaddress]::parse("127.0.0.1") | foo -date $(get-date)  } ) | Should Be "Bound parameter: date ipaddress"
	    }
		It "Sanity check (no remoting)" {
		    $( icm $s { get-date | foo -ipaddress ([ipaddress]::parse("127.0.0.1"))  } ) | Should Be "Bound parameter: date ipaddress"
	    }

	    $module = Import-PSSession $s foo -AllowClobber
		It "Pipeline binding works even when also binding by name" {
		    $( get-date | foo ) | Should Be "Bound parameter: date"
	    }
		It "Pipeline binding works even when also binding by name" {
		    $( [ipaddress]::parse("127.0.0.1") | foo ) | Should Be "Bound parameter: ipaddress"
	    }
		It "Pipeline binding works even when also binding by name" {
		    $( [ipaddress]::parse("127.0.0.1") | foo -date $(get-date) ) | Should Be "Bound parameter: date ipaddress"
	    }
		It "Pipeline binding works even when also binding by name" {
		    $( get-date | foo -ipaddress ([ipaddress]::parse("127.0.0.1")) ) | Should Be "Bound parameter: date ipaddress"
	    }
	    Remove-Module $module

	    ##############################################################################
	    # TEST: value from pipeline by property name - multiple parameters

	    icm $s {
		    function foo {
			    param(
				    [System.TimeSpan]
				    [parameter(ValueFromPipelineByPropertyName = $true)]
				    $TotalProcessorTime,

				    [System.Diagnostics.ProcessPriorityClass]
				    [parameter(ValueFromPipelineByPropertyName = $true)]
				    $PriorityClass
			    )

			    "Bound parameter: $($myInvocation.BoundParameters.Keys | sort)"
		    }
	    }

		It "Sanity check (no remoting)." {
		    $(icm $s { gps -pid $pid | foo }) | Should Be "Bound parameter: PriorityClass TotalProcessorTime"
	    }
		It "Sanity check (no remoting)." {
		    $(icm $s { gps -pid $pid | foo -Total 5 }) | Should Be "Bound parameter: PriorityClass TotalProcessorTime"
	    }
		It "Sanity check (no remoting)." {
		    $(icm $s { gps -pid $pid | foo -Priority normal }) | Should Be "Bound parameter: PriorityClass TotalProcessorTime"
	    }

	    $module = Import-PSSession $s foo -AllowClobber
		It "Pipeline binding works by property name" {
		    $( gps -id $pid | foo ) | Should Be "Bound parameter: PriorityClass TotalProcessorTime"
	    }
		It "Pipeline binding works by property name" {
		    $( gps -id $pid | foo -Total 5 ) | Should Be "Bound parameter: PriorityClass TotalProcessorTime"
	    }
		It "Pipeline binding works by property name" {
		    $( gps -id $pid | foo -Priority normal ) | Should Be "Bound parameter: PriorityClass TotalProcessorTime"
	    }
	    Remove-Module $module

	    ##############################################################################
	    # TEST: 2 parameters on the same position
	    #

	    icm $s {
		    function foo {
			    param(
				    [string]
				    [parameter(Position = 0, parametersetname = 'set1', mandatory = $true)]
				    $string,

				    [ipaddress]
				    [parameter(Position = 0, parametersetname = 'set2', mandatory = $true)]
				    $ipaddress
			    )

			    "Bound parameter: $($myInvocation.BoundParameters.Keys | sort)"
		    }
	    }

		    It "Sanity check (no remoting)." {
		    $(icm $s { foo ([ipaddress]::parse("127.0.0.1")) }) | Should Be "Bound parameter: ipaddress"
	    }
		    It "Sanity check (no remoting)." {
		    $(icm $s { foo "blah" }) | Should Be "Bound parameter: string"
	    }

	    $module = Import-PSSession $s foo -AllowClobber
		It "Positional binding works" {
		    $( foo "blah" ) | Should Be "Bound parameter: string"
	    }
		It "Positional binding works" {
		    $( foo ([ipaddress]::parse("127.0.0.1")) ) | Should Be "Bound parameter: ipaddress"
	    }
	    Remove-Module $module

	    ##############################################################################
	    # TEST: positional binding and array argument value
	    #

	    icm $s {
		    function foo {
			    param(
				    [object]
				    [parameter(Position = 0, mandatory = $true)]
				    $p1,

				    [object]
				    [parameter(Position = 1)]
				    $p2
			    )

			    "$p1 : $p2"
		    }
	    }

		It "Sanity check (no remoting)." {
		    $(icm $s { foo 1,2,3 }) | Should Be "1 2 3 : "
	    }
		It "Sanity check (no remoting)." {
		    $(icm $s { foo 1,2,3 4 }) | Should Be "1 2 3 : 4"
	    }
		It "Sanity check (no remoting)." {
		    $(icm $s { foo -p2 4 1,2,3 }) | Should Be "1 2 3 : 4"
	    }
		It "Sanity check (no remoting)." {
		    $(icm $s { foo 1 4 }) | Should Be "1 : 4"
	    }
		It "Sanity check (no remoting)." {
		    $(icm $s { foo -p2 4 1 }) | Should Be "1 : 4"
	    }

	    $module = Import-PSSession $s foo -AllowClobber
		It "Positional binding works when binding an array value" {
		    $( foo 1,2,3 ) | Should Be "1 2 3 : "
	    }
		It "Positional binding works when binding an array value" {
		    $( foo 1,2,3 4 ) | Should Be "1 2 3 : 4"
	    }
		It "Positional binding works when binding an array value" {
		    $( foo -p2 4 1,2,3 ) | Should Be "1 2 3 : 4"
	    }
		It "Positional binding works when binding an array value" {
		    $( foo 1 4 ) | Should Be "1 : 4"
	    }
		It "Positional binding works when binding an array value" {
		    $( foo -p2 4 1 ) | Should Be "1 : 4"
	    }
	    Remove-Module $module


	    ##############################################################################
	    # TEST: value from remaining arguments
	    #

	    icm $s {
		    function foo {
			    param(
				    [string]
				    [parameter(Position = 0)]
				    $firstArg,

				    [string[]]
				    [parameter(ValueFromRemainingArguments = $true)]
				    $remainingArgs
			    )

			    "$firstArg : $remainingArgs"
		    }
	    }

		It "Sanity check (no remoting)." {
		    $(icm $s { foo }) | Should Be " : "
	    }
		It "Sanity check (no remoting)." {
		    $(icm $s { foo 1 }) | Should Be "1 : "
	    }
		It "Sanity check (no remoting)." {
		    $(icm $s { foo -first 1 }) | Should Be "1 : "
	    }
		It "Sanity check (no remoting)." {
		    $(icm $s { foo 1 2 3 }) | Should Be "1 : 2 3"
	    }
		It "Sanity check (no remoting)." {
		    $(icm $s { foo -first 1 2 3 }) | Should Be "1 : 2 3"
	    }
		It "Sanity check (no remoting)." {
		    $(icm $s { foo 2 3 -first 1 4 5 }) | Should Be "1 : 2 3 4 5"
	    }
		It "Sanity check (no remoting)." {
		    $(icm $s { foo -remainingArgs 2,3 1 }) | Should Be "1 : 2 3"
	    }

	    $module = Import-PSSession $s foo -AllowClobber
		It "Value from remaining arguments works" {
		    $( foo ) | Should Be " : "
	    }
		It "Value from remaining arguments works" {
		    $( foo 1 ) | Should Be "1 : "
	    }
		It "Value from remaining arguments works" {
		    $( foo -first 1 ) | Should Be "1 : "
	    }
		It "Value from remaining arguments works" {
		    $( foo 1 2 3 ) | Should Be "1 : 2 3"
	    }
		It "Value from remaining arguments works" {
		    $( foo -first 1 2 3 ) | Should Be "1 : 2 3"
	    }
		It "Value from remaining arguments works" {
		    $( foo 2 3 -first 1 4 5 ) | Should Be "1 : 2 3 4 5"
	    }
		It "Value from remaining arguments works" {
		    $( foo -remainingArgs 2,3 1 ) | Should Be "1 : 2 3"
	    }

	    Remove-Module $module

	    ##############################################################################
	    # TEST: non cmdlet-based binding
	    #

	    icm $s {
		    function foo {
			    param(
				    $firstArg,
				    $secondArg
			    )

			    "$firstArg : $secondArg : $args"
		    }
	    }

		It "Sanity check (no remoting)." {
		    $(icm $s { foo }) | Should Be " :  : "
	    }
		It "Sanity check (no remoting)." {
		    $(icm $s { foo 1 }) | Should Be "1 :  : "
	    }
		It "Sanity check (no remoting)." {
		    $(icm $s { foo -first 1 }) | Should Be "1 :  : "
	    }
		It "Sanity check (no remoting)." {
		    $(icm $s { foo 1 2 }) | Should Be "1 : 2 : "
	    }
		It "Sanity check (no remoting)." {
		    $(icm $s { foo 1 -second 2 }) | Should Be "1 : 2 : "
	    }
		It "Sanity check (no remoting)." {
		    $(icm $s { foo -first 1 -second 2 }) | Should Be "1 : 2 : "
	    }
		It "Sanity check (no remoting)." {
		    $(icm $s { foo 1 2 3 4 }) | Should Be "1 : 2 : 3 4"
	    }
		It "Sanity check (no remoting)." {
		    $(icm $s { foo -first 1 2 3 4 }) | Should Be "1 : 2 : 3 4"
	    }
		It "Sanity check (no remoting)." {
		    $(icm $s { foo 1 -second 2 3 4 }) | Should Be "1 : 2 : 3 4"
	    }
		It "Sanity check (no remoting)." {
		    $(icm $s { foo 1 3 -second 2 4 }) | Should Be "1 : 2 : 3 4"
	    }
		It "Sanity check (no remoting)." {
		    $(icm $s { foo -first 1 -second 2 3 4 }) | Should Be "1 : 2 : 3 4"
	    }

	    $module = Import-PSSession $s foo -AllowClobber
		It "Non cmdlet-based binding works." {
		    $( foo ) | Should Be " :  : "
	    }

		It "Non cmdlet-based binding works." {
		    $( foo 1 ) | Should Be "1 :  : "
	    }
		It "Non cmdlet-based binding works." {
		    $( foo -first 1 ) | Should Be "1 :  : "
	    }
		It "Non cmdlet-based binding works." {
		    $( foo 1 2 ) | Should Be "1 : 2 : "
	    }
		It "Non cmdlet-based binding works." {
		    $( foo 1 -second 2 ) | Should Be "1 : 2 : "
	    }
		It "Non cmdlet-based binding works." {
		    $( foo -first 1 -second 2 ) | Should Be "1 : 2 : "
	    }
		It "Non cmdlet-based binding works." {
		    $( foo 1 2 3 4 ) | Should Be "1 : 2 : 3 4"
	    }
		It "Non cmdlet-based binding works." {
		    $( foo -first 1 2 3 4 ) | Should Be "1 : 2 : 3 4"
	    }
		It "Non cmdlet-based binding works." {
		    $( foo 1 -second 2 3 4 ) | Should Be "1 : 2 : 3 4"
	    }
		It "Non cmdlet-based binding works." {
		    $( foo 1 3 -second 2 4 ) | Should Be "1 : 2 : 3 4"
	    }
		It "Non cmdlet-based binding works." {
		    $( foo -first 1 -second 2 3 4 ) | Should Be "1 : 2 : 3 4"
	    }
	    Remove-Module $module

	    ##############################################################################
	    # TEST: default parameter initialization should be executed on the server

	    Invoke-Command -Session $s { param($x) IEx $x } -Args 'function MyInitializerFunction { param($x = $($pid)) $x }'
	    $module = Import-PSSession -Session:$s -Name:MyInitializerFunction -AllowClobber
	    $localPid = $pid
	    $remotePid = icm $s { $pid }
		It "Sanity check - remotePid != localPid" {
		    $localPid | Should Not Be $remotePid
	    }
		It "Initializer run on the remote server" {
		    (MyInitializerFunction) | Should Be $remotePid
	    }
		It "Initializer not run when value provided" {
		    (MyInitializerFunction 123) | Should Be 123
	    }
	    Remove-Module $module

	    ##############################################################################
	    # TEST: client-side parameters - cmdlet case

	    $module = Import-PSSession -Session:$s -Name:Get-Variable -Type:cmdlet -AllowClobber
		It "Importing by name/type should work" {
		    (Get-Variable -Name:pid).Value | Should Not Be $pid
	    }
	    $remotePid = (Get-Variable -Name:pid).Value

	    $job = Get-Variable -Name:pid -AsJob
		It "-AsJob should return something" {
		    $job | Should Not Be NullOrEmpty
	    }
		It "-AsJob returns the right type of object" {
		    ($job -is [System.Management.Automation.Job]) | Should Be $true
	    }
		It "Job completes within reasonable time" {
		    ($job.Finished.WaitOne([TimeSpan]::FromSeconds(10), $false)) | Should Be $true
	    }
		It "AsJob: $job.JobStateInfo.State" {
		    $job.JobStateInfo.State | Should Be 'Completed'
	    }
	    $childJob = $job.ChildJobs[0]
		It "AsJob: $childJob.Output.Count" {
		    $childJob.Output.Count | Should Be 1
	    }
		It "AsJob: $childJob.Output[0].Value" {
		    $childJob.Output[0].Value | Should Be $remotePid
	    }
	    Remove-Job $job

	    $result1 = Get-Variable -Name:pid -OutVariable global:result2
		It "OutVariable: $result1.Value" {
		    $result1.Value | Should Be $remotePid
	    }
		It "OutVariable: $result2[0].Value" {
		    $global:result2[0].Value | Should Be $remotePid
	    }

	    Remove-Module $module

	    ##############################################################################
	    # TEST: client-side parameters - Windows 7 bug #759434

	    $module = Import-PSSession -Session:$s -Name:Write-Warning -Type:cmdlet -Prefix Remote -AllowClobber

	    $jobWithWarnings = write-remotewarning foo -warningaction continue -asjob
	    $null = Wait-Job $jobWithWarnings
        It "Warnings present if -WarningAction Continue" {
            $jobWithWarnings.ChildJobs[0].Warning.Count | Should Be 1
        }
	    Remove-Job $jobWithWarnings

	    $jobWithoutWarnings = write-remotewarning foo -warningaction silentlycontinue -asjob
	    $null = Wait-Job $jobWithoutWarnings
		It "No warnings if -WarningAction SilentlyContinue" {
		    0 | Should Be ($jobWithoutWarnings.ChildJobs[0].Warning.Count)
	    }
	    Remove-Job $jobWithoutWarnings

	    Remove-Module $module

	    ##############################################################################
	    # TEST: client-side parameters - non-cmdlet case

	    icm $s { function foo { param($OutVariable) "OutVariable = $OutVariable" } }
		It "Sanity check: OutVariable is not intercepted for non-cmdlet-bound functions" {
		    $(icm $s { foo -OutVariable x }) | Should Be "OutVariable = x"
	    }
	    $module = Import-PSSession -Session:$s -Name:foo -Type:function -AllowClobber
		It "Implicit remoting: OutVariable is not intercepted for non-cmdlet-bound functions" {
		    $( foo -OutVariable x ) | Should Be "OutVariable = x"
	    }
	    Remove-Module $module

	    ##############################################################################
	    # TEST: switch and positional parameters

	    $module = Import-PSSession -Session $s -Name Get-Variable -Type cmdlet -Prefix Remote -AllowClobber
	    $remotePid = ICm $s { $pid }
		It "Sanity check: remote pid != local pid" {
		    $remotePid | Should Not Be $pid
	    }

	    # switch
	    $proxiedPid = Get-RemoteVariable -Name pid -ValueOnly
		It "Switch parameters work fine" {
		    $remotePid | Should Be $proxiedPid
	    }

	    # positional
	    $proxiedPid = Get-RemoteVariable pid
		It "Positional parameters work fine" {
		    $remotePid | Should Be ($proxiedPid.Value)
	    }
    }
    finally
    {
	    if ($s -ne $null) { Remove-PSSession $s -ErrorAction SilentlyContinue }
        if ($module -ne $null) { Remove-Module $module -Force -ErrorAction SilentlyContinue }
    }
}


Describe "Implicit remoting on restricted ISS" -tags 'Innerloop', 'P1' {

    if (${env:PROCESSOR_ARCHITECTURE} -eq 'ARM')
    {
	    Write-Warning "Skipping the test on ARM"
	    return
    }

    ##############################################################################
    # SETUP: create a remote end-point

    try
    {
        $sessionConfigurationDll = [IO.Path]::Combine([IO.Path]::GetTempPath(), "ImplicitRemotingRestrictedConfiguration$(Get-Random).dll")

        Add-Type -OutputAssembly $sessionConfigurationDll -TypeDefinition @"

        using System;
        using System.Collections.Generic;
        using System.Management.Automation;
        using System.Management.Automation.Runspaces;
        using System.Management.Automation.Remoting;

        namespace MySessionConfiguration
        {
        public class MySessionConfiguration : PSSessionConfiguration
        {
        public override InitialSessionState GetInitialSessionState(PSSenderInfo senderInfo)
        {
        //System.Diagnostics.Debugger.Launch();
        //System.Diagnostics.Debugger.Break();

        InitialSessionState iss = InitialSessionState.CreateRestricted(System.Management.Automation.SessionCapabilities.RemoteServer);

        // add Out-String for testing stuff
        iss.Commands["Out-String"][0].Visibility = SessionStateEntryVisibility.Public;

        // remove all commands that are not public
        List<string> commandsToRemove = new List<string>();
        foreach (SessionStateCommandEntry entry in iss.Commands)
        {
        List<SessionStateCommandEntry> sameNameEntries = new List<SessionStateCommandEntry>(iss.Commands[entry.Name]);
        if (!sameNameEntries.Exists(delegate(SessionStateCommandEntry e) { return e.Visibility == SessionStateEntryVisibility.Public; }))
        {
        commandsToRemove.Add(entry.Name);
        }
        }
        foreach (string commandToRemove in commandsToRemove)
        {
        iss.Commands.Remove(commandToRemove, null /* all types */);
        }

        return iss;
        }
        }
        }

"@

        Get-PSSessionConfiguration ImplicitRemotingRestrictedConfiguration* | Unregister-PSSessionConfiguration -Force

        $myConfiguration = Register-PSSessionConfiguration `
            -Name ImplicitRemotingRestrictedConfiguration `
            -ApplicationBase (Split-Path $sessionConfigurationDll) `
            -AssemblyName (Split-Path $sessionConfigurationDll -Leaf) `
            -ConfigurationTypeName "MySessionConfiguration.MySessionConfiguration" `
            -Force `

        $s = New-PSSession -Cn "localhost" -ConfigurationName $myConfiguration.Name
        It "Verifies that created PSSession is not null" {
            $s | Should Not Be $null
        }

        ##############################################################################
        # TEST: restrictions work
        #

	    It "Get-Variable is private" {
		    (@(ICm $s { Get-Command -Name Get-Variabl* }).Count) | Should Be 0
	    }
	    It "Only 9 commands are public" {
		    (@(ICm $s { Get-Command }).Count) | Should Be 9
	    }

        ##############################################################################
        # TEST: basic functionality of Import-PSSession works (against a directly exposed cmdlet and against a proxy function)

        $m = Import-PSSession $s Out-Strin*,Measure-Object -Type Cmdlet,Function -ArgumentList 123 -AllowClobber

	    It "Import-PSSession works against the ISS-restricted runspace (Out-String)" {
		    (@(Get-Command Out-String -Type Function).Count) | Should Be 1
	    }
	    It "Import-PSSession works against the ISS-restricted runspace (Measure-Object)" {
		    (@(Get-Command Measure-Object -Type Function).Count) | Should Be 1
	    }

        $remoteResult = Out-String -input ("blah " * 10) -Width 10
        $localResult = Microsoft.PowerShell.Utility\Out-String -input ("blah " * 10) -Width 10
	    It "Invoking an implicit remoting proxy works against the ISS-restricted runspace (Out-String)" {
		    $localResult | Should Be $remoteResult
	    }

        $remoteResult = 1..10 | Measure-Object
        $localResult = 1..10 | Microsoft.PowerShell.Utility\Measure-Object
	    It "Invoking an implicit remoting proxy works against the ISS-restricted runspace (Measure-Object)" {
		    ($localResult.Count) | Should Be ($remoteResult.Count)
	    }
    }
    finally
    {
        if ($m -ne $null) { Remove-Module $m -Force -ErrorAction SilentlyContinue }
        if ($s -ne $null) { Remove-PSSession $s -ErrorAction SilentlyContinue }
        if ($myConfiguration -ne $null) { Unregister-PSSessionConfiguration -Name ($myConfiguration.Name) -Force -ErrorAction SilentlyContinue }
        if ($sessionConfigurationDll -ne $null) { Remove-Item $sessionConfigurationDll -Force -ErrorAction SilentlyContinue }
    }
}


Describe "Implicit remoting tests" -tags 'Innerloop', 'P1' {

    try
    {
	    $s = New-PSSession

	    ##############################################################################
	    # TEST: Get-Command $m and $m.Name work (Windows 7: #334112)

	    $m = Import-PSSession $s Get-Variable -Prefix My -AllowClobber

		It "PSModuleInfo.Name shouldn't contain a psd1 extension" {
		    ($m.Name -notlike '*.psd1') | Should Be $true
	    }
		It "PSModuleInfo.Name shouldn't contain a psm1 extension" {
		    ($m.Name -notlike '*.psm1') | Should Be $true
	    }
		It "PSModuleInfo.Name shouldn't contain a path" {
		    ($m.Name -notlike "${env:TMP}*") | Should Be $true
	    }

	    $c = @(Get-Command -Module $m)
		It "Get-Command returns only 1 public command from implicit remoting module (1)" {
		    $c.Count | Should Be 1
	    }
		It "Get-Command returns the right public command from implicit remoting module (1)" {
		    $c[0].Name | Should Be "Get-MyVariable"
	    }

	    $c = @(Get-Command -Module $m.Name)
		It "Get-Command returns only 1 public command from implicit remoting module (2)" {
		    $c.Count | Should Be 1
	    }
		It "Get-Command returns the right public command from implicit remoting module (2)" {
		    $c[0].Name | Should Be "Get-MyVariable"
	    }

	    Remove-Module $m

	    ##############################################################################
	    # TEST: progress bar should be 1) present and 2) completed also

	    function Get-TempModuleFile
	    {
		    [IO.Path]::Combine([IO.Path]::GetTempPath(), [Guid]::NewGuid().ToString())
	    }

	    $file = Get-TempModuleFile

	    $powerShell = [PowerShell]::Create().AddCommand("Export-PSSession").AddParameter("Session", $s).AddParameter("ModuleName", $file).AddParameter("CommandName", "Get-Process").AddParameter("AllowClobber")
	    $powerShell.Invoke() | Out-Null
		It "'Completed' progress record should be present" {
		    ($powerShell.Streams.Progress | select -last 1).RecordType.ToString() | Should Be "Completed"
	    }
	    $powerShell.Dispose()

	    ##############################################################################
	    # TEST: display of property-less objects (not sure if this test belongs here) (Windows 7: #248499)

        $x = new-object random
	    $expected = $x.ToString()
	    # Since New-PSSession now only loads Microsoft.PowerShell.Core and for the session in the test, Autoloading is disabled, engine cannot find New-Object as it is part of Microsoft.PowerShell.Utility module.
        # The fix is to import this module before running the command.
		It "Display of local property-less objects" {
		    $expected | Should Be ($($x | out-string).Trim())
	    }
		It "Display of remote property-less objects" {
		    $expected | Should Be ($(ICm $s { Import-Module Microsoft.PowerShell.Utility; new-object random } | out-string).Trim())
	    }

	    ##############################################################################
	    # TEST: piping between remoting proxies should work

	    $module = Import-PSSession -Session:$s -Name:Write-Output -AllowClobber
	    $result = Write-Output 123 | Write-Output
		It "piping between remoting proxies should work" {
		    $result | Should Be 123
	    }
	    Remove-Module $module

	    ##############################################################################
	    # TEST: BUG: Windows 7: #269467: Security: Server can inject code that will be executed on a client during implicit remoting call.

	    icm $s { function attack(${foo="$(calc)"}){echo "It is done."}}
	    $m = Import-PSSession -Session $s -CommandName attack -EA SilentlyContinue -ErrorVariable expectedError -AllowClobber
		It "Strange parameter names should trigger an error" {
		    $expectedError | Should Not Be NullOrEmpty
	    }
	    Remove-Module $m

	    ##############################################################################
	    # TEST: Non-terminating error from a remote command

	    icm $s { $oldGetCommand = ${function:Get-Command} }
	    icm $s { function get-command { write-error blah } }

	    $module = Import-PSSession -Session:$s -EA SilentlyContinue -ErrorVariable expectedError -AllowClobber
		It "Non-terminating error from remote end got duplicated locally" {
		    $expectedError | Should Not Be NullOrEmpty
	    }
	    $msg = [string]($expectedError[0])
		It "Error message got duplicated correctly" {
		    ($msg.Contains("blah")) | Should Be $true
	    }

	    Remove-Module $module
	    icm $s { ${function:Get-Command} = $oldGetCommand }

	    ##############################################################################
	    # TEST: Get-Command returns something that wasn't asked for

	    icm $s { $oldGetCommand = ${function:Get-Command} }
	    icm $s { function notRequested { "notRequested" }; function get-command { Microsoft.PowerShell.Core\Get-Command Get-Variable,notRequested } }

	    $module = Import-PSSession -Session:$s Get-Variable -AllowClobber -EA SilentlyContinue -ErrorVariable expectedError
		It "We get an error if remote server returns something that wasn't asked for" {
		    $expectedError | Should Not Be NullOrEmpty
	    }
	    $msg = [string]($expectedError[0])
		It "Error message contains reference to the command that wasn't asked for" {
		    ($msg.Contains("notRequested")) | Should Be $true
	    }

	    Remove-Module $module
	    icm $s { ${function:Get-Command} = $oldGetCommand }

	    ##############################################################################
	    # TEST: Get-Command returns something that is not CommandInfo

	    icm $s { $oldGetCommand = ${function:Get-Command} }
	    icm $s { function get-command { Microsoft.PowerShell.Utility\Get-Variable } }

	    $expectedError = $null
	    try
	    {
		    $module = Import-PSSession -Session:$s -AllowClobber
	    }
	    catch
	    {
		    $expectedError = $_
	    }
		It "Got terminating error for malformed data" {
		    ($expectedError) | Should Not Be $null
	    }
	    $msg = [string]($expectedError)
		It "Error message contains reference to Get-Command" {
		    ($msg.Contains("Get-Command")) | Should Be $true
	    }

	    Remove-Module $module
	    icm $s { ${function:Get-Command} = $oldGetCommand }

	    ##############################################################################
	    # TEST: order of remote commands (alias > function > cmdlet > external script)

	    $tempdir = join-path $env:TEMP ([IO.Path]::GetRandomFileName())
	    $null = mkdir $tempdir
	    $oldpath = $env:PATH
	    try
	    {
		    'param([Parameter(Mandatory=$true)]$scriptParam) "external script / $scriptParam"' >$tempdir\myOrder.ps1
		    icm $s { param($x) $env:PATH = $env:PATH + ";" + $x } -Args $tempDir
		    icm $s { function myOrder { param([Parameter(Mandatory=$true)]$functionParam) "function / $functionParam" } }
		    icm $s { function helper { param([Parameter(Mandatory=$true)]$aliasParam) "alias / $aliasParam" }; set-alias myOrder helper }

		    $expectedResult = icm $s { myOrder -aliasParam 123 }

		    $m = Import-PSSession $s myOrder -CommandType All -AllowClobber
		    $actualResult = myOrder -aliasParam 123
			It "Command resolution myOrder should be respected by implicit remoting" {
		        $expectedResult | Should Be $actualResult
	        }
		    Remove-Module $m
	    }
	    finally
	    {
		    $env:PATH = $oldpath
		    del $tempDir -Force -Recurse -EA SilentlyContinue
	    }


	    ##############################################################################
	    # TEST: -Prefix parameter

	    $module = Import-PSSession -Session:$s -Name:Get-Variable -Type:cmdlet -Prefix My -AllowClobber
		It "proxy should return remote pid" {
		    (Get-MyVariable -Name:pid).Value | Should Not Be $pid
	    }
	    Remove-Module $module
		It "Prefixed commands are removed correctly" {
		    ((Get-Item function:Get-MyVariable -ErrorAction SilentlyContinue) -eq $null) | Should Be $true
	    }

	    ##############################################################################
	    # TEST: BadVerbs of functions should trigger a warning

	    icm $s { function BadVerb-Variable { param($name) Get-Variable $name } }

	    $ps = [powershell]::Create().AddCommand("Import-PSSession", $true).AddParameter("Session", $s).AddParameter("CommandName", "BadVerb-Variable")
	    $ps.Invoke() | out-null

		It "No errors from importing a function with a bad verb" {
		    $ps.Streams.Error.Count | Should Be 0
	    }
		It "Warnings should be emitted when importing a function with a bad verb" {
		    $ps.Streams.Warning.Count | Should Not Be 0
	    }

	    $m = Import-PSSession $s BadVerb-Variable -WarningAction SilentlyContinue -AllowClobber

	    $remotePid = icm $s { $pid }
	    $badVerbVariablePid = (BadVerb-Variable -Name:pid).Value
	    $getVariablePid = icm $s { (Get-Variable -Name:pid).Value }
		It "Importing function with bad verb should work" {
		    $badVerbVariablePid | Should Be $remotePid
	    }
		It "Importing function with bad verb should work" {
		    $getVariablePid | Should Be $remotePid
	    }

		It "Get-Variable function should be not exported when importing a BadVerb-Variable function" {
		    ((Get-Item function:Get-Variable -ErrorAction SilentlyContinue) -eq $null) | Should Be $true
	    }
		It "BadVerb-Variable should be a function, not an alias (1)" {
		    ((Get-Item function:BadVerb-Variable -ErrorAction SilentlyContinue) -eq $null) | Should Not Be $true
	    }
		It "BadVerb-Variable should be a function, not an alias (2)" {
		    ((Get-Item alias:BadVerb-Variable -ErrorAction SilentlyContinue) -eq $null) | Should Be $true
	    }

	    Remove-Module $m
	    icm $s { del function:BadVerb-Variable }

	    ##############################################################################
	    # TEST: BadVerbs of functions shouldn't trigger a warning when -DisableNameChecking is used

	    icm $s { function BadVerb-Variable { param($name) Get-Variable $name } }

	    $ps = [powershell]::Create().AddCommand("Import-PSSession", $true).AddParameter("Session", $s).AddParameter("CommandName", "BadVerb-Variable").AddParameter("DisableNameChecking", $true)
	    $ps.Invoke() | out-null

		It "No errors from importing a function with a bad verb + -DisableNameChecking" {
		    $ps.Streams.Error.Count | Should Be 0
	    }
		It "No warnings from importing a function with a bad verb + -DisableNameChecking" {
		    $ps.Streams.Warning.Count | Should Be 0
	    }

	    $m = Import-PSSession $s BadVerb-Variable -DisableNameChecking -AllowClobber

	    $remotePid = icm $s { $pid }
	    $badVerbVariablePid = (BadVerb-Variable -Name:pid).Value
	    $getVariablePid = icm $s { (Get-Variable -Name:pid).Value }
		It "Importing function with bad verb should work" {
		    $badVerbVariablePid | Should Be $remotePid
	    }
		It "Importing function with bad verb should work" {
		    $getVariablePid | Should Be $remotePid
	    }

		It "Get-Variable function should be not exported when importing a BadVerb-Variable function" {
		    ((Get-Item function:Get-Variable -ErrorAction SilentlyContinue) -eq $Null ) | Should Be $true
	    }
		It "BadVerb-Variable should be a function, not an alias (1)" {
		    ((Get-Item function:BadVerb-Variable -ErrorAction SilentlyContinue) -ne $null) | Should Be $true
	    }
		It "BadVerb-Variable should be a function, not an alias (2)" {
		    ((Get-Item alias:BadVerb-Variable -ErrorAction SilentlyContinue) -eq $null) | Should Be $true
	    }

	    Remove-Module $m
	    icm $s { del function:BadVerb-Variable }

	    ##############################################################################
	    # TEST: BadVerbs of aliases shouldn't trigger a warning
	    #       (+ can import an alias without saying -CommandType Alias)

	    icm $s { set-alias BadVerb-Variable Get-Variable }

	    $ps = [powershell]::Create().AddCommand("Import-PSSession", $true).AddParameter("Session", $s).AddParameter("CommandName", "BadVerb-Variable")
	    $ps.Invoke() | out-null

		It "No errors from importing an alias with a bad verb" {
		    $ps.Streams.Error.Count | Should Be 0
	    }
		It "No warnings from importing an alias with a bad verb" {
		    $ps.Streams.Warning.Count | Should Be 0
	    }

	    $m = Import-PSSession $s BadVerb-Variable -AllowClobber

	    $remotePid = icm $s { $pid }
	    $badVerbVariablePid = (BadVerb-Variable -Name:pid).Value
	    $getVariablePid = icm $s { (Get-Variable -Name:pid).Value }
		It "Importing alias with bad verb should work" {
		    $badVerbVariablePid | Should Be $remotePid
	    }
		It "Importing alias with bad verb should work" {
		    $getVariablePid | Should Be $remotePid
	    }

	    Remove-Module $m
	    icm $s { del alias:BadVerb-Variable }

	    ##############################################################################
	    # TEST: removing a module should clean-up event handlers (Windows 7: #268819)

	    $oldNumberOfHandlers = $executionContext.GetType().GetProperty("Events").GetValue($executionContext, $null).Subscribers.Count
	    $module = Import-PSSession -Session:$s -Name:Get-Random -AllowClobber
	    Remove-Module $module
	    $newNumberOfHandlers = $executionContext.GetType().GetProperty("Events").GetValue($executionContext, $null).Subscribers.Count
		It "Event should be unregistered when the module is removed" {
		    $oldNumberOfHandlers | Should Be $newNumberOfHandlers
	    }

		It "Private functions from the implicit remoting module shouldn't get imported into global scope" {
		    0 | Should Be @(dir function:*Implicit* -ErrorAction SilentlyContinue).Count
	    }
    }
    finally
    {
	    if ($s -ne $null) { Remove-PSSession $s -ErrorAction SilentlyContinue }
        if ($m -ne $null) { Remove-Module $m -Force -ErrorAction SilentlyContinue }
        if ($module -ne $null) { Remove-Module $module -Force -ErrorAction SilentlyContinue }
        if ($file -ne $null) { Remove-item $file -Force -Recurse -ErrorAction SilentlyContinue }
    }
}


Describe "Export-PSSession function" -tags 'Innerloop', 'P1' {

    try
    {
        ##############################################################################
        # TEST SETUP - CREATE TEMP DIRECTORY
        #

        $modulesDir = $env:TEMP -split ';' | select -first 1

        $tempdir = join-path $modulesDir ([IO.Path]::GetRandomFileName())
        mkdir $tempdir | Out-Null

        $fileName = [io.path]::GetFileName($tempdir)

        ##############################################################################
        # TEST: basic functionality of Export-PSSession

        $s = New-PSSession
        Invoke-Command -Session $s {Import-Module PSDiagnostics}
        Export-PSSession -Session $s -OutputModule $tempdir\Diag -CommandName Start-Trace -AllowClobber | Out-Null

        @"
        Import-Module `"$tempdir\Diag`"
        `$mod = Get-Module Diag
        Return `$mod
"@ > $tempdir\TestBug450687.ps1

        # Only the snapin Microsoft.PowerShell.Core is loaded
        $iss = [System.Management.Automation.Runspaces.InitialSessionState]::CreateDefault2()
        $ps = [PowerShell]::Create($iss)
        $result = $ps.AddScript(" & $tempdir\TestBug450687.ps1").Invoke()

	    It "The module created by Export-PSSession is imported successfully" {
		    ($result -ne $null -and $result.Count -eq 1 -and $result[0].Name -eq "Diag") | Should Be $true
	    }
        $c = $result[0].ExportedCommands["Start-Trace"]
	    It "The command Add-BitsFile is imported successfully" {
		    ($c -ne $null -and $c.CommandType -eq "Function") | Should Be $true
	    }
    }
    finally
    {
        if ($s -ne $null) { Remove-PSSession $s -ErrorAction SilentlyContinue }
        if ($tempdir -ne $null) { Remove-Item $tempDir -Force -Recurse -ErrorAction SilentlyContinue }
    }
}


Describe "Implicit remoting with disconnected session" -tags 'Innerloop', 'P1' {

    try
    {
        ## Create session for import.
        $s = nsn -Name Session102
        $m = Import-PSSession $s Get-Variable -prefix remote -AllowClobber

        ## Check local and remote versions of process Id variable.
        $thisPid = Get-Variable pid
        $sessionPid = Get-RemoteVariable pid
	    It "This and remote session process ids should be different." {
		    $thisPid.Value | Should Not Be $sessionPid.Value
	    }

        ## Disconnect session and use imported command.
        Disconnect-PSSession $s
        $dSessionPid = Get-RemoteVariable pid
	    It "Session process id should be same as before, with connected session." {
		    $dSessionPid.Value | Should Be $sessionPid.Value
	    }
	    It "Session should be reconnected." {
		    $s.State | Should Be 'Opened'
	    }

        ## Disconnect session and make it un-connectable.
        Disconnect-PSSession $s
        start powershell -arg 'Get-PSSession -cn localhost -name Session102 | Connect-PSSession' -Wait

        sleep 3

        ## This time a new session is created because the old one is unavailable.
        $dSessionPid = Get-RemoteVariable pid
	    It "Should have a new session process id because old session is unavailable." {
		    $dSessionPid.Value | Should Not Be $sessionPid.Value
	    }
    }
    finally
    {
        if ($s -ne $null) { Remove-PSSession $s -ErrorAction SilentlyContinue }
        if ($m -ne $null) { Remove-Module $m -Force -ErrorAction SilentlyContinue }
    }
}


Describe "Select-Object with implicit remoting" -tags 'Innerloop', 'P1' {

    try
    {

        $session = New-PSSession localhost
        icm $session { function foo { "a","b","c" } }
        $module = Import-PSSession $session foo -AllowClobber
        $bar = foo | select -First 2; "here"

	    It "Select -First failed with implicit remoting" {
		    $bar | Should Not Be NullOrEmpty
	    }
	    It "Select -First failed with implicit remoting" {
		    $bar.Count | Should Be 2
	    }
	    It "Select -First failed with implicit remoting" {
		    $bar[0] | Should Be "a"
	    }
	    It "Select -First failed with implicit remoting" {
		    $bar[1] | Should Be "b"
	    }
    }
    finally
    {
        if ($module -ne $null) { Remove-Module $module -Force -ErrorAction SilentlyContinue }
        if ($session -ne $null) { Remove-PSSession $session -ErrorAction SilentlyContinue }
    }
}

Describe "Get-FormatData used in Export-PSSession should work on DL targets" -Tags 'Innerloop','P1' {

    # Only run these tests if .NET 2.0 and PS 2.0 is installed on the machine
    if (! (test-path 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v2.0.50727') -or
        ! (test-path 'HKLM:\SOFTWARE\Microsoft\PowerShell\1\PowerShellEngine')
        )
    {
        It -SKip "PS 2.0 not installed.  Skipping test."
        return
    }

    $configName = "DLConfigTest"

    try
    {
        $null = Register-PSSessionConfiguration -Name $configName -PSVersion 2.0 -Force
        $s = New-PSSession -ComputerName . -ConfigurationName $configName

        $results = Export-PSSession -Session $s -OutputModule tempTest -CommandName Get-Process `
            -AllowClobber -FormatTypeName * -Force -ErrorAction Stop

        It "Verifies that Export-PSSession with PS 2.0 session and format type names succeeds" {
            $results.Count | Should Not Be 0
        }
    }
    finally
    {
        Unregister-PSSessionConfiguration -Name $configName -Force -ErrorAction SilentlyContinue
        if ($s -ne $null) { Remove-PSSession $s -ErrorAction SilentlyContinue }
        if ($results.Count -gt 0)
        {
            Remove-Item -Path $results[0].DirectoryName -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

Describe "GetCommand locally and remotely" -Tags 'Innerloop','P1' {

    BeforeAll {
        $s = New-PSSession -cn localhost
    }

    AfterAll {
        if ($s -ne $null) { Remove-PSSession $s -ErrorAction SilentlyContinue }
    }

    $localCommandCount = (Get-Command -Type Cmdlet).Count
    $remoteCommandCount = Invoke-Command { (Get-Command -Type Cmdlet).Count }

    It "Verifies that the number of local cmdlet command count is the same as remote cmdlet command count." {
        $localCommandCount | Should Be $remoteCommandCount
    }
}
