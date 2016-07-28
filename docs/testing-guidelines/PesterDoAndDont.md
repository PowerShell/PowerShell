## Do
1. Name your files <descriptivetest>.tests.ps1
2. Keep tests simple
	1. Test only what you need
	2. Reduce dependencies
3. Be sure to tag your `Describe` blocks based on their purpose
	1. Tag `CI` indicates that it will be run as part of the continuous integration process. These should be unit test like, and fast.
	2. Tag `Feature` indicates a higher level feature test (we will run these on a regular basis), for example, tests which go to remote resources, or test broader functionality
	3. Tag `Scenario` indicates tests of integration with other features (these will be run on a less regular basis and test even broader functionality than feature tests.
4. Make sure that `Describe`/`Context`/`It` descriptions are useful
	1. The error message should not be the place where you describe the test
5. Use `Context` to group tests
	1. Multiple `Context` blocks can help you group your test suite into logical sections
6. Use `BeforeAll`/`AfterAll`/`BeforeEach`/`AfterEach` instead of custom initiators
7. Use Try-Catch for expected errors and check $_.fullyQualifiedErrorId (don't use `should throw`)
8. Use `-testcases` when iterating over multiple `It` blocks
9. Use code coverage functionality where appropriate
10. Use `Mock` functionality when you don't have your entire environment
11. Avoid free code in a `Describe` block
	1. Use `[Before|After][Each|All]` _see Free Code in a Describe block_

## Don't
1. Have too many evaluations in a single It block
	1. The first `Should` failure will stop that block
2.  `Should` be used in an `It` Block
