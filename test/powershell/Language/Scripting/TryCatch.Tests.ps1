# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

#############################################################
#
# Test simple parsing, ensure newlines allowed everywhere
#
#############################################################

Describe "Test try/catch" -Tags "CI" {

    BeforeAll {
        function AssertArraysEqual ($result, $expected)
        {
            $result.Count | Should -BeExactly $expected.Count
            for ($i = 0; $i -lt $result.Count; $i++) {
                $result[$i] | Should -BeExactly $expected[$i]
            }
        }
    }

    It "Test simple parsing, ensure newlines allowed everywhere" {
        try
        {
        }
        catch
        {
        }

        try
        {
        }
        catch
        [int]
        {
        }

        try
        {
        }
        catch
        [int]
        ,
        [char]
        {
        }

        try
        {
        }
        finally
        {
        }

        try
        {
        }
        catch
        {
        }
        finally
        {
        }

        try
        {
        }
        catch
        [int]
        {
        }
        finally
        {
        }

        try
        {
        }
        catch
        [int]
        ,
        [char]
        {
        }
        finally
        {
        }

        $true | Should -BeTrue # we only verify that there is no parsing error. This line contains a dummy Should to make pester happy.
    }

    Context "Basic exception handling" {
        It "Simple throw and catch" {
            $a = . { try { 1; throw "exception"; "test failed" } catch { 2 } }
            AssertArraysEqual $a (1, 2)
        }

        It "Simple try finally" {
            $a = . { try { 1 } finally { 2 } }
            AssertArraysEqual $a (1,2)
        }

        It "Simple try, throw, catch, and finally" {
            $a = . { try { 1; throw "exception"; "test failed" } catch { 2 } finally { 3 } }
            AssertArraysEqual $a (1..3)
        }
    }

    Context "Mix traps with try/catch" {
        It "Trap shouldn't catch exception" {
            $a = . { trap { "test failed" } try { 1; throw "exception"; "test failed" } catch { 2 } }
            AssertArraysEqual $a (1,2)
        }

        It "Trap should catch exception" {
            $a = . { try { 1; throw "exception"; trap { 2; return }; "test failed" } catch { "test failed" } }
            AssertArraysEqual $a (1,2)
        }
    }

    Context "Catch by type" {
        It "Catch by type #1" {
            $a = . { try { 1; $a = 0; 1/$a; "test failed" } catch [DivideByZeroException] { 2 } }
            AssertArraysEqual $a (1,2)
        }

        It "Catch by type #2" {
            $a = . { try { 1; $a = 0; 1/$a; "test failed" } catch [DivideByZeroException] { 2 } catch [Exception] { "test failed" } }
            AssertArraysEqual $a (1,2)
        }

        It "Catch by type #3" {
            $a = . { try { 1; $a = 0; 1/$a; "test failed" } catch [DivideByZeroException] { 2 } catch { "test failed" } }
            AssertArraysEqual $a (1,2)
        }

        It "Catch by type #4" {
            $a = . { try { 1; $a = 0; 1/$a; "test failed" } catch [DivideByZeroException],[ArgumentNullException] { 2 } }
            AssertArraysEqual $a (1,2)
        }

        It "Catch by type #5" {
            $a = . { try { 1; throw ([ArgumentNullException]::new("bad")) } catch [DivideByZeroException],[ArgumentNullException] { 2 } }
            AssertArraysEqual $a (1,2)
        }
    }

    Context "Control flow in try [exit not tested and throw tested elsewhere]" {
        It "break in try" {
            $a = . {
              foreach ($i in (1..3)) {
                try {
                  if ($i -eq 2) {
                    break
                  }
                  $i
                } catch {
                  "test failed"
                } finally {
                  "finally: $i"
                }
              }
            }
            AssertArraysEqual $a (1, "finally: 1", "finally: 2")
        }

        It "continue in try" {
            $a = . {
              foreach ($i in (1..3)) {
                try {
                  if ($i -eq 2) {
                    continue
                  }
                  $i
                } catch {
                  "test failed"
                } finally {
                  "finally: $i"
                }
              }
            }
            AssertArraysEqual $a (1, "finally: 1", "finally: 2", 3, "finally: 3")
        }

        # Disabled - Compiled script has differing (but better) behavior
        It "return in try" -Pending {
            $a = . {
              function foo($i) {
                try {
                  if ($i -eq 2) {
                    return "return: $i"
                  }
                  $i
                } catch {
                  "test failed"
                } finally {
                  "finally: $i"
                }
              }
              foo 1
              foo 2
            }

            AssertArraysEqual $a (1, "finally: 1", "finally: 2", "return: 2")
        }

        It "continue in nested try within foreach loop" {
            $a = . {
                foreach ($i in (1..3)) {
                    try { #1
                        try { #2
                            if ($i -eq 2) {
                                continue
                            }
                            $i
                        } catch {
                            "test failed: catch#2"
                        } finally {
                            "finally#2: $i"
                        }
                    } catch {
                        "test failed: catch#1"
                    } finally {
                        "finally#1: $i"
                    }
                }
            }
            AssertArraysEqual $a (1, "finally#2: 1", "finally#1: 1", "finally#2: 2", "finally#1: 2", 3, "finally#2: 3", "finally#1: 3")
        }

        It "break in nested try within foreach loop" {
            $a = . {
                foreach ($i in (1..3)) {
                    try { #1
                        try { #2
                            if ($i -eq 2) {
                                break
                            }
                            $i
                        } catch {
                            "test failed: catch#2"
                        } finally {
                            "finally#2: $i"
                        }
                    } catch {
                        "test failed: catch#1"
                    } finally {
                        "finally#1: $i"
                    }
                }
            }
            AssertArraysEqual $a (1, "finally#2: 1", "finally#1: 1", "finally#2: 2", "finally#1: 2")
        }
    }

    Context "Control flow in catch [exit not tested and throw tested elsewhere]" {
        It "break in catch without loop" {
            $a = . {
              try {
                throw 1
              } catch {
                foreach ($i in (1..3)) {
                  if ($i -eq 2) {
                    break
                  }
                  $i
                }
              } finally {
                "finally"
              }
            }
            AssertArraysEqual $a (1, "finally")
        }

        It "break in catch within foreach loop" {
            $a = . {
              foreach ($i in (1..3)) {
                try {
                  throw 1
                } catch {
                  if ($i -eq 2) {
                    break
                  }
                  $i
                } finally {
                  "finally $i"
                }
              }
            }
            AssertArraysEqual $a (1, "finally 1", "finally 2")
        }

        It "continue in catch without loop" {
            $a = . {
              try {
                throw 1
              } catch {
                foreach ($i in (1..3)) {
                  if ($i -eq 2) {
                    continue
                  }
                  $i
                }
              } finally {
                "finally"
              }
            }
            AssertArraysEqual $a (1, 3, "finally")
        }

        It "continue in catch within foreach loop" {
            $a = . {
              foreach ($i in (1..3)) {
                try {
                throw 1
                } catch {
                  if ($i -eq 2) {
                    continue
                  }
                  $i
                } finally {
                  "finally $i"
                }
              }
            }
            AssertArraysEqual $a (1, "finally 1", "finally 2", 3, "finally 3")
        }

        It "continue in nested catch within foreach loop" {
            $a = . {
                foreach ($i in (1..3)) {
                    try { #1
                        try { #2
                            throw 1
                        } catch {
                            if ($i -eq 2) {
                                continue
                            }
                            $i
                        } finally {
                            "finally#2: $i"
                        }
                    } catch {
                        "test failed: catch#1"
                    } finally {
                        "finally#1: $i"
                    }
                }
            }
            AssertArraysEqual $a (1, "finally#2: 1", "finally#1: 1", "finally#2: 2", "finally#1: 2", 3, "finally#2: 3", "finally#1: 3")
        }

        It "break in nested catch within foreach loop" {
            $a = . {
                foreach ($i in (1..3)) {
                    try { #1
                        try { #2
                            throw 1
                        } catch {
                            if ($i -eq 2) {
                                break
                            }
                            $i
                        } finally {
                            "finally#2: $i"
                        }
                    } catch {
                        "test failed: catch#1"
                    } finally {
                        "finally#1: $i"
                    }
                }
            }
            AssertArraysEqual $a (1, "finally#2: 1", "finally#1: 1", "finally#2: 2", "finally#1: 2")
        }

        # Disabled - Compiled script has differing (but better) behavior
        It "return in catch without loop" -Pending {
            $a = . {
              function foo {
                try {
                  throw 1
                } catch {
                  foreach ($i in (1..3)) {
                    if ($i -eq 2) {
                      return "returned"
                    }
                    $i
                  }
                } finally {
                  "finally"
                }
              }
              foo
            }

            AssertArraysEqual $a (1, "finally", "returned") "return in catch"
        }

        # Disabled - Compiled script has differing (but better) behavior
        It "return in catch within foreach loop" -Pending {
            $a = . {
              function foo {
                foreach ($i in (1..3)) {
                  try {
                    throw 1
                  } catch {
                    if ($i -eq 2) {
                      return "returned"
                    }
                    $i
                  } finally {
                     "finally $i"
                  }
                }
              }
              foo
            }

            AssertArraysEqual $a (1, "finally 1", "finally 2", "returned")
        }
    }

    Context "Control flow in finally, normal execution" {
        It "break in finally normal execution" {
            $a = . {
              try {
                "try"
              } catch {
              } finally {
                "finally"
                foreach ($i in (1..3)) {
                  if ($i -eq 2) {
                    break
                  }
                  $i
                }
              }
            }

            AssertArraysEqual $a ("try", "finally", 1)
        }

        It "continue in finally normal execution" {
            $a = . {
              try {
                "try"
              } catch {
              } finally {
                "finally"
                foreach ($i in (1..3)) {
                  if ($i -eq 2) {
                    continue
                  }
                  $i
                }
              }
            }

            AssertArraysEqual $a ("try", "finally", 1, 3)
        }
    }

    Context "Control flow in finally, abnormal execution" {
        It "break in finally normal execution" {
            $a = . {
              try {
                "try"
                throw 1
              } catch {
                "catch"
              } finally {
                "finally"
                foreach ($i in (1..3)) {
                  if ($i -eq 2) {
                    break
                  }
                  $i
                }
              }
            }

            AssertArraysEqual $a ("try", "catch", "finally", 1)
        }

        It "continue in finally normal execution" {
            $a = . {
              try {
                "try"
                throw 1
              } catch {
                "catch"
              } finally {
                "finally"
                foreach ($i in (1..3)) {
                  if ($i -eq 2) {
                    continue
                  }
                  $i
                }
              }
            }

            AssertArraysEqual $a ("try", "catch", "finally", 1, 3)
        }
    }

    Context "Exception object" {
        It "ErrorRecord object is set correctly" {
            $a = . {
              try {
                throw 42
              } catch {
                $_
              }
            }

            [int]$a.ToString() | Should -Be 42
        }
    }

    It "Nested try/catch" {
        $a = . {
          try {
            "outer try"
            try {
              "inner try"
              $a = 0
              1 / $a
            }
            catch [OutOfMemoryException] {
              "test failed"
            }
            finally {
              "inner finally"
            }
          }
          catch [DivideByZeroException] {
            "caught"
          }
          finally {
            "outer finally"
          }
        }

        AssertArraysEqual $a ("outer try", "inner try", "inner finally", "caught", "outer finally")
    }

    Context "Rethrow" {
        It "rethrow flow up" {
            $a = . {
              try {
                try {
                  $a = 0
                  1 / $a
                } catch {
                  "inner catch"
                  $ex_inner = $_
                  throw
                }
              } catch {
                "outer catch"
                $ex_outer = $_
              }
            }

            AssertArraysEqual $a ("inner catch", "outer catch")
            $ex_inner.Exception | Should -BeExactly $ex_outer.Exception
        }

        It "throw; outside catch threw wrong object" {
            $a = . {
              function foo {
                trap [system.management.automation.runtimeexception] {
                  return "test passed"
                }
                trap {
                  return "test failed"
                }
                throw
              }
              try {
                $a = 0
                1 / $a
              } catch {
                foo
              }
            }

            $a | Should -BeExactly "test passed"
        }
    }

    Context "Additional try/catch tests by exception types" {

        It "Catch ActionPreferenceStopException" {
            $exception = $null
            $a = try {
                    Get-ChildItem TESTDRIVE:\NotExist -ErrorAction Stop
                 } catch [System.Management.Automation.ActionPreferenceStopException] {
                    $exception = $_.Exception.GetType().FullName
                    "ActionPreferenceStopException Caught"
                 }
            $a | Should -BeExactly "ActionPreferenceStopException Caught"
            ## Many legacy scripts from PSv2 catch 'ActionPreferenceStopException' and then check '$_.Exception' to do the real handling
            $exception | Should -BeExactly "System.Management.Automation.ItemNotFoundException"
        }

        It "Catch CmdletInvocationException" {
            $exception = $null
            $a = try {
                    Invoke-Expression "Get-Command -Name"
                 } catch [System.Management.Automation.CmdletInvocationException] {
                    $exception = $_.Exception.GetType().FullName
                    "CmdletInvocationException Caught"
                 }
            $a | Should -BeExactly "CmdletInvocationException Caught"
            $exception | Should -BeExactly "System.Management.Automation.ParameterBindingException"
        }

        It "Choose 'ItemNotFoundException' over 'Exception' when searching handler" {
            $a = try {
                    Get-ChildItem TESTDRIVE:\NotExist -ErrorAction Stop
                 } catch [System.Management.Automation.ItemNotFoundException] {
                    "ItemNotFoundException caught"
                 } catch [System.Exception] {
                    "System.Exception caught"
                 }
            $a | Should -BeExactly "ItemNotFoundException caught"
        }

        It "Choose 'ItemNotFoundException' over 'RuntimeException' when searching handler" {
            $a = try {
                    Get-ChildItem TESTDRIVE:\NotExist -ErrorAction Stop
                 } catch [System.Management.Automation.ItemNotFoundException] {
                    "ItemNotFoundException caught"
                 } catch [System.Management.Automation.RuntimeException] {
                    "RuntimeException caught"
                 } catch [System.Exception] {
                    "System.Exception caught"
                 }
            $a | Should -BeExactly "ItemNotFoundException caught"
        }

        It "Choose 'ItemNotFoundException' over 'RuntimeException' and 'Exception' when throw ItemNotFoundException directly" {
            $a = try {
                    throw [System.Management.Automation.ItemNotFoundException]::new()
                 } catch [System.Management.Automation.ItemNotFoundException] {
                    "ItemNotFoundException caught"
                 } catch [System.Management.Automation.RuntimeException] {
                    "RuntimeException caught"
                 } catch [System.Exception] {
                    "System.Exception caught"
                 }
            $a | Should -BeExactly "ItemNotFoundException caught"
        }
    }
}
