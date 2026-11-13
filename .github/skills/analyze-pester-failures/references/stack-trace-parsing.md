# Understanding Pester Test Failures

This reference explains how to interpret Pester test output and understand failure messages.

## Supported Formats

### Pester 4 Format
```
at line: 123 in C:\path\to\file.ps1
```

**Regex Pattern:**
```powershell
if ($StackTraceString -match 'at line:\s*(\d+)\s+in\s+(.+?)(?:\r|\n|$)') {
    $result.Line = $matches[1]
    $result.File = $matches[2].Trim()
    return $result
}
```

### Pester 5 Format (Common)
```
at 1 | Should -Be 2, C:\path\to\file.ps1:123
at 1 | Should -Be 2, /home/runner/work/PowerShell/PowerShell/test/file.ps1:123
```

**Regex Pattern:**
```powershell
if ($StackTraceString -match ',\s*((?:[A-Za-z]:)?[\/\\].+?\.ps[m]?1):(\d+)') {
    $result.File = $matches[1].Trim()
    $result.Line = $matches[2]
    return $result
}
```

### Alternative Format
```
at C:\path\to\file.ps1:123
at /path/to/file.ps1:123
```

**Regex Pattern:**
```powershell
if ($StackTraceString -match 'at\s+((?:[A-Za-z]:)?[\/\\][^,]+?\.ps[m]?1):(\d+)(?:\r|\n|$)') {
    $result.File = $matches[1].Trim()
    $result.Line = $matches[2]
    return $result
}
```

## Troubleshooting Parsing Failures

### Issue: Line Number Extracted But File Path Is Null

**Cause:** Stack trace matches line-with-path pattern but file extraction doesn't work

**Solution:**
1. Check if file path exists as expected in filesystem
2. Verify regex doesn't have too-greedy bounds (check use of `.+?` vs `.+`)
3. Test regex against actual stack trace string:
   ```powershell
   $trace = "at line: 42 in C:\path\to\test.ps1"
   if ($trace -match 'at line:\s*(\d+)\s+in\s+(.+?)(?:\r|\n|$)') {
       Write-Host "File: $($matches[2])"  # Should be "C:\path\to\test.ps1"
   }
   ```

### Issue: Special Characters in File Path Break Regex

**Cause:** Characters like parens `()`, brackets `[]`, pipes `|` have special meaning in regex

**Solution:**
1. Escape special chars in regex: `[Regex]::Escape($path)`
2. Use character class `[\/\\]` instead of alternation for path separators
3. Test with files containing problematic names:
   ```powershell
   $traces = @(
       "at line: 1 in C:\path\(with)\parens\test.ps1",
       "at /home/user/[brackets]/test.ps1:5",
       "at C:\path\with spaces\test.ps1:10"
   )
   # Test each against all patterns
   ```

### Issue: Regex Matches But Extracts Wrong Values

**Symptom:** $matches[1] is file instead of line, or vice versa

**Debug Steps:**
1. Print all captured groups: `$matches.Values | Format-Table -AutoSize`
2. Verify group order in regex matches expectations
3. Test with sample Pester output:
   ```powershell
   $sampleTrace = @"
   at 1 | Should -Be 2, /home/runner/work/PowerShell/test/file.ps1:42
   "@
   
   if ($sampleTrace -match ',\s*((?:[A-Za-z]:)?[\/\\].+?\.ps[m]?1):(\d+)') {
       Write-Host "Match 1: $($matches[1])"  # Should be file path
       Write-Host "Match 2: $($matches[2])"  # Should be line number
   }
   ```

## Testing the Parser

Use this PowerShell script to validate `Get-PesterFailureFileInfo`:

```powershell
# Import the function
. ./build.psm1

$testCases = @(
    @{
        Input = "at line: 42 in C:\path\to\test.ps1"
        Expected = @{ File = "C:\path\to\test.ps1"; Line = "42" }
    },
    @{
        Input = "at /home/runner/work/test.ps1:123"
        Expected = @{ File = "/home/runner/work/test.ps1"; Line = "123" }
    },
    @{
        Input = "at 1 | Should -Be 2, /path/to/file.ps1:99"
        Expected = @{ File = "/path/to/file.ps1"; Line = "99" }
    }
)

foreach ($test in $testCases) {
    $result = Get-PesterFailureFileInfo -StackTraceString $test.Input
    
    $fileMatch = $result.File -eq $test.Expected.File
    $lineMatch = $result.Line -eq $test.Expected.Line
    $status = if ($fileMatch -and $lineMatch) { "✓ PASS" } else { "✗ FAIL" }
    
    Write-Host "$status : $($test.Input)"
    if (-not $fileMatch) { Write-Host "  Expected file: $($test.Expected.File), got: $($result.File)" }
    if (-not $lineMatch) { Write-Host "  Expected line: $($test.Expected.Line), got: $($result.Line)" }
}
```

## Adding Support for New Formats

When Pester changes its output format:

1. **Capture sample output** from failing tests
2. **Identify the pattern** (e.g., "file path always after comma followed by colon")
3. **Write regex** to match pattern without over-matching
4. **Add to `Get-PesterFailureFileInfo`** before existing patterns (order matters for fallback)
5. **Test with samples** containing special characters, long paths, and edge cases

Example: Adding a new format at the top of the function:

```powershell
# Try pattern: "at <description>, <path>:<line>" (Pester 5.1 hypothetical)
if ($StackTraceString -match 'at .+?, ((?:[A-Za-z]:)?[\/\\].+?\.ps[m]?1):(\d+)') {
    $result.File = $matches[1].Trim()
    $result.Line = $matches[2]
    return $result
}

# Try existing patterns...
```

Place new patterns **first** so they take precedence over fallback patterns.
