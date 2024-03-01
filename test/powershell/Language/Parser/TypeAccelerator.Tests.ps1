# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Type accelerators" -Tags "CI" {
    BeforeAll {
        $TypeAcceleratorsType = [psobject].Assembly.GetType("System.Management.Automation.TypeAccelerators")
        $TypeAccelerators = $TypeAcceleratorsType::Get
    }
    Context 'BuiltIn Accelerators' {
        BeforeAll {

            $TypeAcceleratorTestCases = @(
                @{
                    Accelerator = 'Alias'
                    Type        = [System.Management.Automation.AliasAttribute]
                }
                @{
                    Accelerator = 'AllowEmptyCollection'
                    Type        = [System.Management.Automation.AllowEmptyCollectionAttribute]
                }
                @{
                    Accelerator = 'AllowEmptyString'
                    Type        = [System.Management.Automation.AllowEmptyStringAttribute]
                }
                @{
                    Accelerator = 'AllowNull'
                    Type        = [System.Management.Automation.AllowNullAttribute]
                }
                @{
                    Accelerator = 'ArgumentCompleter'
                    Type        = [System.Management.Automation.ArgumentCompleterAttribute]
                }
                @{
                    Accelerator = 'ArgumentCompletions'
                    Type        = [System.Management.Automation.ArgumentCompletionsAttribute]
                }
                @{
                    Accelerator = 'array'
                    Type        = [System.Array]
                }
                @{
                    Accelerator = 'bool'
                    Type        = [System.Boolean]
                }
                @{
                    Accelerator = 'byte'
                    Type        = [System.Byte]
                }
                @{
                    Accelerator = 'char'
                    Type        = [System.Char]
                }
                @{
                    Accelerator = 'CmdletBinding'
                    Type        = [System.Management.Automation.CmdletBindingAttribute]
                }
                @{
                    Accelerator = 'datetime'
                    Type        = [System.DateTime]
                }
                @{
                    Accelerator = 'decimal'
                    Type        = [System.Decimal]
                }
                @{
                    Accelerator = 'double'
                    Type        = [System.Double]
                }
                @{
                    Accelerator = 'DscResource'
                    Type        = [System.Management.Automation.DscResourceAttribute]
                }
                @{
                    Accelerator = 'ExperimentAction'
                    Type        = [System.Management.Automation.ExperimentAction]
                }
                @{
                    Accelerator = 'Experimental'
                    Type        = [System.Management.Automation.ExperimentalAttribute]
                }
                @{
                    Accelerator = 'ExperimentalFeature'
                    Type        = [System.Management.Automation.ExperimentalFeature]
                }
                @{
                    Accelerator = 'float'
                    Type        = [System.Single]
                }
                @{
                    Accelerator = 'single'
                    Type        = [System.Single]
                }
                @{
                    Accelerator = 'guid'
                    Type        = [System.Guid]
                }
                @{
                    Accelerator = 'hashtable'
                    Type        = [System.Collections.Hashtable]
                }
                @{
                    Accelerator = 'int'
                    Type        = [System.Int32]
                }
                @{
                    Accelerator = 'int32'
                    Type        = [System.Int32]
                }
                @{
                    Accelerator = 'short'
                    Type        = [System.Int16]
                }
                @{
                    Accelerator = 'int16'
                    Type        = [System.Int16]
                }
                @{
                    Accelerator = 'long'
                    Type        = [System.Int64]
                }
                @{
                    Accelerator = 'int64'
                    Type        = [System.Int64]
                }
                @{
                    Accelerator = 'ciminstance'
                    Type        = [Microsoft.Management.Infrastructure.CimInstance]
                }
                @{
                    Accelerator = 'cimclass'
                    Type        = [Microsoft.Management.Infrastructure.CimClass]
                }
                @{
                    Accelerator = 'cimtype'
                    Type        = [Microsoft.Management.Infrastructure.CimType]
                }
                @{
                    Accelerator = 'cimconverter'
                    Type        = [Microsoft.Management.Infrastructure.CimConverter]
                }
                @{
                    Accelerator = 'IPEndpoint'
                    Type        = [System.Net.IPEndPoint]
                }
                @{
                    Accelerator = 'NullString'
                    Type        = [System.Management.Automation.Language.NullString]
                }
                @{
                    Accelerator = 'OutputType'
                    Type        = [System.Management.Automation.OutputTypeAttribute]
                }
                @{
                    Accelerator = 'ObjectSecurity'
                    Type        = [System.Security.AccessControl.ObjectSecurity]
                }
                @{
                    Accelerator = 'Parameter'
                    Type        = [System.Management.Automation.ParameterAttribute]
                }
                @{
                    Accelerator = 'PhysicalAddress'
                    Type        = [System.Net.NetworkInformation.PhysicalAddress]
                }
                @{
                    Accelerator = 'pscredential'
                    Type        = [System.Management.Automation.PSCredential]
                }
                @{
                    Accelerator = 'PSDefaultValue'
                    Type        = [System.Management.Automation.PSDefaultValueAttribute]
                }
                @{
                    Accelerator = 'pslistmodifier'
                    Type        = [System.Management.Automation.PSListModifier]
                }
                @{
                    Accelerator = 'psobject'
                    Type        = [System.Management.Automation.PSObject]
                }
                @{
                    Accelerator = 'pscustomobject'
                    Type        = [System.Management.Automation.PSObject]
                }
                @{
                    Accelerator = 'psprimitivedictionary'
                    Type        = [System.Management.Automation.PSPrimitiveDictionary]
                }
                @{
                    Accelerator = 'ref'
                    Type        = [System.Management.Automation.PSReference]
                }
                @{
                    Accelerator = 'PSTypeNameAttribute'
                    Type        = [System.Management.Automation.PSTypeNameAttribute]
                }
                @{
                    Accelerator = 'regex'
                    Type        = [System.Text.RegularExpressions.Regex]
                }
                @{
                    Accelerator = 'DscProperty'
                    Type        = [System.Management.Automation.DscPropertyAttribute]
                }
                @{
                    Accelerator = 'sbyte'
                    Type        = [System.SByte]
                }
                @{
                    Accelerator = 'string'
                    Type        = [System.String]
                }
                @{
                    Accelerator = 'SupportsWildcards'
                    Type        = [System.Management.Automation.SupportsWildcardsAttribute]
                }
                @{
                    Accelerator = 'switch'
                    Type        = [System.Management.Automation.SwitchParameter]
                }
                @{
                    Accelerator = 'cultureinfo'
                    Type        = [System.Globalization.CultureInfo]
                }
                @{
                    Accelerator = 'bigint'
                    Type        = [System.Numerics.BigInteger]
                }
                @{
                    Accelerator = 'securestring'
                    Type        = [System.Security.SecureString]
                }
                @{
                    Accelerator = 'timespan'
                    Type        = [System.TimeSpan]
                }
                @{
                    Accelerator = 'ushort'
                    Type        = [System.UInt16]
                }
                @{
                    Accelerator = 'uint16'
                    Type        = [System.UInt16]
                }
                @{
                    Accelerator = 'uint'
                    Type        = [System.UInt32]
                }
                @{
                    Accelerator = 'uint32'
                    Type        = [System.UInt32]
                }
                @{
                    Accelerator = 'ulong'
                    Type        = [System.Uint64]
                }
                @{
                    Accelerator = 'uint64'
                    Type        = [System.UInt64]
                }
                @{
                    Accelerator = 'uri'
                    Type        = [System.Uri]
                }
                @{
                    Accelerator = 'ValidateCount'
                    Type        = [System.Management.Automation.ValidateCountAttribute]
                }
                @{
                    Accelerator = 'ValidateDrive'
                    Type        = [System.Management.Automation.ValidateDriveAttribute]
                }
                @{
                    Accelerator = 'ValidateLength'
                    Type        = [System.Management.Automation.ValidateLengthAttribute]
                }
                @{
                    Accelerator = 'ValidateNotNull'
                    Type        = [System.Management.Automation.ValidateNotNullAttribute]
                }
                @{
                    Accelerator = 'ValidateNotNullOrEmpty'
                    Type        = [System.Management.Automation.ValidateNotNullOrEmptyAttribute]
                }
                @{
                    Accelerator = 'ValidateNotNullOrWhiteSpace'
                    Type        = [System.Management.Automation.ValidateNotNullOrWhiteSpaceAttribute]
                }
                @{
                    Accelerator = 'ValidatePattern'
                    Type        = [System.Management.Automation.ValidatePatternAttribute]
                }
                @{
                    Accelerator = 'ValidateRange'
                    Type        = [System.Management.Automation.ValidateRangeAttribute]
                }
                @{
                    Accelerator = 'ValidateScript'
                    Type        = [System.Management.Automation.ValidateScriptAttribute]
                }
                @{
                    Accelerator = 'ValidateSet'
                    Type        = [System.Management.Automation.ValidateSetAttribute]
                }
                @{
                    Accelerator = 'ValidateUserDrive'
                    Type        = [System.Management.Automation.ValidateUserDriveAttribute]
                }
                @{
                    Accelerator = 'version'
                    Type        = [System.Version]
                }
                @{
                    Accelerator = 'void'
                    Type        = [System.Void]
                }
                @{
                    Accelerator = 'ipaddress'
                    Type        = [System.Net.IPAddress]
                }
                @{
                    Accelerator = 'DscLocalConfigurationManager'
                    Type        = [System.Management.Automation.DscLocalConfigurationManagerAttribute]
                }
                @{
                    Accelerator = 'WildcardPattern'
                    Type        = [System.Management.Automation.WildcardPattern]
                }
                @{
                    Accelerator = 'X509Certificate'
                    Type        = [System.Security.Cryptography.X509Certificates.X509Certificate]
                }
                @{
                    Accelerator = 'X500DistinguishedName'
                    Type        = [System.Security.Cryptography.X509Certificates.X500DistinguishedName]
                }
                @{
                    Accelerator = 'xml'
                    Type        = [System.Xml.XmlDocument]
                }
                @{
                    Accelerator = 'CimSession'
                    Type        = [Microsoft.Management.Infrastructure.CimSession]
                }
                @{
                    Accelerator = 'mailaddress'
                    Type        = [System.Net.Mail.MailAddress]
                }
                @{
                    Accelerator = 'semver'
                    Type        = [System.Management.Automation.SemanticVersion]
                }
                @{
                    Accelerator = 'scriptblock'
                    Type        = [System.Management.Automation.ScriptBlock]
                }
                @{
                    Accelerator = 'psvariable'
                    Type        = [System.Management.Automation.PSVariable]
                }
                @{
                    Accelerator = 'type'
                    Type        = [System.Type]
                }
                @{
                    Accelerator = 'psmoduleinfo'
                    Type        = [System.Management.Automation.PSModuleInfo]
                }
                @{
                    Accelerator = 'powershell'
                    Type        = [System.Management.Automation.Powershell]
                }
                @{
                    Accelerator = 'runspacefactory'
                    Type        = [System.Management.Automation.Runspaces.RunspaceFactory]
                }
                @{
                    Accelerator = 'runspace'
                    Type        = [System.Management.Automation.Runspaces.Runspace]
                }
                @{
                    Accelerator = 'initialsessionstate'
                    Type        = [System.Management.Automation.Runspaces.InitialSessionState]
                }
                @{
                    Accelerator = 'psscriptmethod'
                    Type        = [System.Management.Automation.PSScriptMethod]
                }
                @{
                    Accelerator = 'psscriptproperty'
                    Type        = [System.Management.Automation.PSScriptProperty]
                }
                @{
                    Accelerator = 'psnoteproperty'
                    Type        = [System.Management.Automation.PSNoteProperty]
                }
                @{
                    Accelerator = 'psaliasproperty'
                    Type        = [System.Management.Automation.PSAliasProperty]
                }
                @{
                    Accelerator = 'psvariableproperty'
                    Type        = [System.Management.Automation.PSVariableProperty]
                }
                @{
                    Accelerator = 'pspropertyexpression'
                    Type        = [Microsoft.PowerShell.Commands.PSPropertyExpression]
                }
                @{
                    Accelerator = 'ordered'
                    Type        = [System.Collections.Specialized.OrderedDictionary]
                }
                @{
                    Accelerator = 'NoRunspaceAffinity'
                    Type        = [System.Management.Automation.Language.NoRunspaceAffinityAttribute]
                }
                @{
                    Accelerator = 'ArgumentTransform'
                    Type        = [System.Management.Automation.PSTransformAttribute]
                }
            )

            if ( !$IsWindows )
            {
                $totalAccelerators = 103
            }
            else
            {
                $totalAccelerators = 108

                $extraFullPSAcceleratorTestCases = @(
                    @{
                        Accelerator = 'adsi'
                        Type        = [System.DirectoryServices.DirectoryEntry]
                    }
                    @{
                        Accelerator = 'adsisearcher'
                        Type        = [System.DirectoryServices.DirectorySearcher]
                    }
                    @{
                        Accelerator = 'wmiclass'
                        Type        = [System.Management.ManagementClass]
                    }
                    @{
                        Accelerator = 'wmi'
                        Type        = [System.Management.ManagementObject]
                    }
                    @{
                        Accelerator = 'wmisearcher'
                        Type        = [System.Management.ManagementObjectSearcher]
                    }
                )
            }
        }

        It 'Should have all the type accelerators' {
            $TypeAccelerators.Count | Should -Be $totalAccelerators
        }

        It 'Should have a type accelerator for: <Accelerator>' -TestCases $TypeAcceleratorTestCases {
            param($Accelerator, $Type)
            $TypeAcceleratorsType::Get[$Accelerator] | Should -Be ($Type)
        }

        It 'Should have a type accelerator for non-dotnet-core type: <Accelerator>' -Skip:(!$IsWindows) -TestCases $extraFullPSAcceleratorTestCases {
            param($Accelerator, $Type)
            $TypeAcceleratorsType::Get[$Accelerator] | Should -Be ($Type)
        }
    }

    Context 'User Defined Accelerators' {
        BeforeAll {
            $TypeAcceleratorsType::Add('userDefinedAcceleratorType', [int])
            $TypeAcceleratorsType::Add('userDefinedAcceleratorTypeToRemove', [int])
        }

        AfterAll {
            $TypeAcceleratorsType::Remove('userDefinedAcceleratorType')
            $TypeAcceleratorsType::Remove('userDefinedAcceleratorTypeToRemove')
        }

        It "Basic type accelerator usage" {
            [userDefinedAcceleratorType] | Should -Be ([int])
        }

        It "Can remove type accelerator" {
            $TypeAcceleratorsType::Get['userDefinedAcceleratorTypeToRemove'] | Should -Be ([int])
            $TypeAcceleratorsType::Remove('userDefinedAcceleratorTypeToRemove')
            $TypeAcceleratorsType::Get['userDefinedAcceleratorTypeToRemove'] | Should -BeNullOrEmpty
        }
    }
}
