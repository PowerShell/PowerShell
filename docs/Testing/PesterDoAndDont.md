## Do
	1. Name your files <description>.tests.ps1
	2. Keep tests simple
		a. Test only what you need
		b. Reduce dependencies
	3. Be sure to tag your Describe blocks with "inner" and "outer"
	4. Make sure that Describe/Context/It descriptions are useful
		a. The error message should not be the place where you describe the test
	5. Use "Context" to group tests
		a. Multiple Contexts can help you group your test suite into logical sections
	6. Use BeforeAll/AfterAll/BeforeEach/AfterEach instead of custom initiators
	7. Use Try-Catch for expected errors and check $_.fullyQualifiedErrorId
	8. Loop It blocks for checking multiple properties
	9. Use code coverage functionality where appropriate
	10. Use Mock functionality when you don't have your entire environment
	11. Avoid free code in a Describe block
		a. Use [Before|After][Each|All]  see Free Code in a Describe block

## Don't
	1. Have too many evaluations in a single It block
		a. The first "Should" failure will stop that block
    2. Don't use "Should" anywhere but within an "It" Block

