#  <Test>
#    <TestType>DRT</TestType>
#    <summary>Exception handling (try/catch/finally)</summary>
#  </Test>

param($path = $null)

if ($path -eq $null)
{
    $path = split-path $MyInvocation.InvocationName
}

. "$path\..\asserts.ps1"

#############################################################
#
# Test simple parsing, ensure newlines allowed everywhere
#
#############################################################

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


#############################################################
#
# Basic exception handling
#
#############################################################

$a = . { try { 1; throw "exception"; "test failed" } catch { 2 } }
AssertArraysEqual $a (1,2) "Simple throw and catch"

$a = . { try { 1 } finally { 2 } }
AssertArraysEqual $a (1,2) "Simple try finally"

$a = . { try { 1; throw "exception"; "test failed" } catch { 2 } finally { 3 } }
AssertArraysEqual $a (1..3) "Simple try, throw, catch, and finally"


#############################################################
#
# Mix traps with try/catch
#
#############################################################

$a = . { trap { "test failed" } try { 1; throw "exception"; "test failed" } catch { 2 } }
AssertArraysEqual $a (1,2) "Trap shouldn't catch exception"

$a = . { try { 1; throw "exception"; trap { 2; return }; "test failed" } catch { "test failed" } }
AssertArraysEqual $a (1,2) "Trap should catch exception"


#############################################################
#
# Catch by type
#
#############################################################

$a = . { try { 1; $a = 0; 1/$a; "test failed" } catch [DivideByZeroException] { 2 } }
AssertArraysEqual $a (1,2) "Catch by type #1"

$a = . { try { 1; $a = 0; 1/$a; "test failed" } catch [DivideByZeroException] { 2 } catch [Exception] { "test failed" } }
AssertArraysEqual $a (1,2) "Catch by type #2"

$a = . { try { 1; $a = 0; 1/$a; "test failed" } catch [DivideByZeroException] { 2 } catch { "test failed" } }
AssertArraysEqual $a (1,2) "Catch by type #3"

$a = . { try { 1; $a = 0; 1/$a; "test failed" } catch [DivideByZeroException],[StackOverflowException] { 2 } }
AssertArraysEqual $a (1,2) "Catch by type #3"

#############################################################
#
# Control flow in try
#    exit not tested
#    throw tested elsewhere
#
#############################################################

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
AssertArraysEqual $a (1, "finally: 1", "finally: 2") "break in try"

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
AssertArraysEqual $a (1, "finally: 1", "finally: 2", 3, "finally: 3") "continue in try"

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

# Disabled - Compiled script has differing (but better) behavior
#AssertArraysEqual $a (1, "finally: 1", "finally: 2", "return: 2") "return in try"

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


#############################################################
#
# Control flow in catch
#    exit not tested
#    throw tested elsewhere
#
#############################################################
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
AssertArraysEqual $a (1, "finally") "break in catch"

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
AssertArraysEqual $a (1, "finally 1", "finally 2") "break in catch"

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
AssertArraysEqual $a (1, 3, "finally") "continue in catch"

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
AssertArraysEqual $a (1, "finally 1", "finally 2", 3, "finally 3") "continue in catch"

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

# Disabled - Compiled script has differing (but better) behavior
#AssertArraysEqual $a (1, "finally", "returned") "return in catch"

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
# Disabled - Compiled script has differing (but better) behavior
#AssertArraysEqual $a (1, "finally 1", "finally 2", "returned") "return in catch"

#############################################################
#
# Control flow in finally, normal execution
#
#############################################################
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
AssertArraysEqual $a ("try", "finally", 1) "break in finally normal execution"

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
AssertArraysEqual $a ("try", "finally", 1, 3) "continue in finally normal execution"

#############################################################
#
# Control flow in finally, abnormal execution
#
#############################################################
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
AssertArraysEqual $a ("try", "catch", "finally", 1) "break in finally normal execution"

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
AssertArraysEqual $a ("try", "catch", "finally", 1, 3) "continue in finally normal execution"

#############################################################
#
# Exception object
#
#############################################################
$a = . {
  try {
    throw 42
  } catch {
    $_
  }
}
Assert ([int]$a.ToString() -eq 42) "ErrorRecord object is set correctly"

#############################################################
#
# Nested tries
#
#############################################################
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
AssertArraysEqual $a ("outer try", "inner try", "inner finally", "caught", "outer finally") "Nested try/catch"

#############################################################
#
# Rethrow
#
#############################################################
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
AssertArraysEqual $a ("inner catch", "outer catch") "rethrow flow"
Assert ($ex_inner.Exception -eq $ex_outer.Exception) "rethrow correct object"

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
Assert ($a -eq "test passed") "throw; outside catch threw wrong object"