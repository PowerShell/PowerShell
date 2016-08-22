Describe "Clear-Item tests" -Tag "CI" {
    BeforeAll {
        ${myClearItemVariableTest} = "Value is here"
    }
    It "Clear-Item can clear an item" {
        $myClearItemVariableTest | Should be "Value is here"
        Clear-Item variable:myClearItemVariableTest
        test-path variable:myClearItemVariableTest | should be $true
        $myClearItemVariableTest | Should BeNullOrEmpty
    }
}
