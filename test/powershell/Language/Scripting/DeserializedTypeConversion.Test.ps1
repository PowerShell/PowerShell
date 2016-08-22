#Tests conversion of deserialized types to original type using object properties.
Describe "Tests conversion of deserialized types to original type using object properties." -Tags "CI" {

    BeforeAll {
    # Test Import/Export Clixml scenario.
    $tempFile = [System.IO.Path]::GetTempFileName()

    # Create new types and test functions.
    $type1 = Add-Type -PassThru -TypeDefinition @'
    public class test1
    {
        public string name;
        public int port;
        public string scriptText;
    }
'@    

    $type2 = Add-Type -PassThru -TypeDefinition @'
    public class test2
    {
        private string name;
        private int port;
        private string scriptText;

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public int Port
        {
            get { return port; }
            set { port = value; }
        }

        public string ScriptText
        {
            get { return scriptText; }
            set { scriptText = value; }
        }
    }
'@

    $type3 = Add-Type -PassThru -TypeDefinition @'
    public class test3
    {
        private string name = "default";
        private int port = 80;
        private string scriptText = "1..6";

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public int Port
        {
            get { return port; }
            set { port = value; }
        }

        public string ScriptText
        {
            get { return scriptText; }
        }
    }
'@

$type4 = Add-Type -PassThru -TypeDefinition @'
public class test4
{
    private string name = "default";
    private int port = 80;
    private string scriptText = "1..6";

    public string Name
    {
        get { return name; }
        set { name = value; }
    }

    public int Port
    {
        get { return port; }
        set { port = value; }
    }

    internal void Compute()
    {
        scriptText = scriptText + " Computed";
    }
}
'@

    function test-1
    {
        param(
            [parameter(position=0, mandatory=1)]
            [test1] $test
        )

        $test | fl | Out-String
    }    

    function test-2
    {
        param(
            [parameter(position=0, mandatory=1)]
            [test2] $test
        )

        $test | fl | Out-String
    }

    function test-3
    {
        param(
            [parameter(position=0, mandatory=1)]
            [test3] $test
        )

        $test | fl | Out-String
    }

    function test-4
    {
        param(
            [parameter(position=0, mandatory=1)]
            [test4] $test
        )

        $test | fl | Out-String
    }

    $t1 = new-object test1 -Property @{name="TestName1";port=80;scriptText="1..5"}
    $t2 = new-object test2 -Property @{Name="TestName2";Port=80;ScriptText="1..5"}
    $t3 = new-object test3 -Property @{Name="TestName3";Port=80}
    $t4 = new-object test4 -Property @{Name="TestName4";Port=80}
    }
    

    AfterAll {
        Remove-Item $tempFile
        # Clean up.
        gsn | rsn
    } 

    It 'T1' { 

        Export-Clixml -InputObject $t1 -Path $tempFile
        $dst1 = Import-Clixml $tempFile

        # Type casts should *succeed*.
        $tc1 = [test1]$dst1

        # Parameter bindings should *succeed*.
        test-1 $dst1
    }

    It 'T1 Test remoting scenario.' {
        $s = New-PSSession

        $dsrt1 = Invoke-Command -Session $s -ArgumentList $t1 -ScriptBlock { $tr1 = $args[0]; $tr1 }

        # Type casts should *succeed*.
        $tcr1 = [test1]$dsrt1
    }

    It 'T2' {

        Export-Clixml -InputObject $t2 -Path $tempFile
        $dst2 = Import-Clixml $tempFile

        # Type casts should *succeed*.        
        $tc2 = [test2]$dst2

        # Parameter bindings should *succeed*.        
        test-2 $dst2
    }

    It 'T2 Test remoting scenario.' {
        $s = New-PSSession

        $dsrt2 = Invoke-Command -Session $s -ArgumentList $t2 -ScriptBlock { $tr2 = $args[0]; $tr2 }

        # Parameter bindings should *succeed*.
        test-2 $dsrt2
    }

    Context 'T3' {

        Export-Clixml -InputObject $t3 -Path $tempFile
        $dst3 = Import-Clixml $tempFile        

        # Type casts should *fail*.
        $failed = $true
        try
        {
            $tc3 = [test3]$dst3
            $failed = $false
        }
        catch {
            It '$_.FullyQualifiedErrorId' { $_.FullyQualifiedErrorId | Should Be 'InvalidCastConstructorException' }
        }
        It 'Type casts should *fail*' { $failed | Should Be $true }
        #Assert ($Error[0].FullyQualifiedErrorId -eq "InvalidCastConstructorException") "Test3 type should cause an invalid cast exception."

        # Parameter bindings should *fail*.
        $failed = $true
        try
        {
            test-3 $dst3
            $failed = $false
        }
        catch {
            It '$_.FullyQualifiedErrorId' { $_.FullyQualifiedErrorId | Should Be 'ParameterArgumentTransformationError,test-3' }
        }
        It 'Parameter bindings should *fail*' { $failed | Should Be $true }
        #Assert ($Error[0].FullyQualifiedErrorId -eq 'ParameterArgumentTransformationError,test-3') "Test3 type should cause an invalid binding exception."
        }

    Context 'T3 Test remoting scenario.' {
        $s = New-PSSession
        
        $dsrt3 = Invoke-Command -Session $s -ArgumentList $t3 -ScriptBlock { $tr3 = $args[0]; $tr3 }
        
        # Type casts should *fail*.
        try
        {
            $tcr3 = [test3]$dsrt3
        }
        catch {
            It '$_.FullyQualifiedErrorId' { $_.FullyQualifiedErrorId | Should Be 'InvalidCastConstructorException' }
        }
        #Assert ($Error[0].FullyQualifiedErrorId -eq "InvalidCastConstructorException") "Test3 type should cause an invalid cast exception."

        # Parameter bindings should *fail*.
        try
        {
            test-3 $dsrt3
        }
        catch {
            It '$_.FullyQualifiedErrorId' { $_.FullyQualifiedErrorId | Should Be 'ParameterArgumentTransformationError,test-3' }
        }
        #Assert ($Error[0].FullyQualifiedErrorId -eq 'ParameterArgumentTransformationError,test-3') "Test3 type should cause an invalid binding exception."       
    }

    It 'T4' {        

        Export-Clixml -InputObject $t4 -Path $tempFile
        $dst4 = Import-Clixml $tempFile
                
        $tc4 = [test4]$dst4       

        # Parameter bindings should *succeed*.        
        test-4 $dst4

        }

    It 'T4 Test remoting scenario.' {
        $s = New-PSSession

        $dsrt4 = Invoke-Command -Session $s -ArgumentList $t4 -ScriptBlock { $tr4 = $args[0]; $tr4 }

        # Type casts should *succeed*.        
        $tcr4 = [test4]$dsrt4

        # Parameter bindings should *succeed*.
        test-4 $dsrt4
    }
}