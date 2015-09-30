#Pester Testing Test Guide

## Who this is for

Cmdlet behavior is validated using the Pester testing framework.  The purpose of this document is to create a single standard to maximize unit test coverage while minimizing confusion on expectations.  What follows is a working document intended to guide those writing Pester unit tests for PSL.   

Test suites need to be created and many cmdlets added and unit-tested.  The following list is to be used to guide the thought process of the developer in writing a suite in minimal time, while enhancing quality. 

Test suites should proceed as functional and system tests of the cmdlets, and the code treated as a black box for the purpose of test suite design. 