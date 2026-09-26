# Array and String Division Operator

## Summary

This document describes the implementation of enhanced division operator (`/`) support for arrays and strings in PowerShell. This feature extends PowerShell's existing division operator to provide intuitive syntax for splitting arrays into groups and strings into parts.

## Motivation

PowerShell users frequently need to split arrays and strings into smaller chunks for data processing tasks. Currently, this requires verbose syntax using methods like `Where-Object`, custom functions, or .NET methods. The division operator extension provides a natural, mathematical approach to data splitting that aligns with PowerShell's philosophy of intuitive syntax.

## Specification

### Array Division

#### Syntax
```powershell
<array> / <integer>           # Split into N equal groups
<array> / <integer-array>     # Split using custom group sizes
```

#### Behavior
- **Equal Division**: `@(1,2,3,4,5,6) / 2` creates 2 groups of 3 elements each
- **Custom Sizes**: `@(1,2,3,4,5,6,7,8) / @(2,3,2)` creates groups of sizes 2, 3, 2, with remainder
- **Uneven Distribution**: Extra elements are distributed to earlier groups
- **Type Preservation**: All elements maintain their original types

### String Division

#### Syntax
```powershell
<string> / <integer>          # Split into N equal parts
<string> / <integer-array>    # Split using custom part sizes
```

#### Behavior
- **Character-based**: Splits by character count, not word boundaries
- **Unicode Safe**: Properly handles multi-byte characters and emojis
- **Equal Division**: `"hello world" / 2` creates 2 parts of roughly equal length
- **Custom Sizes**: `"PowerShell" / @(3,4,2)` creates parts of lengths 3, 4, 2, with remainder

## Implementation Details

### Core Components

1. **ArrayOps.cs**: Contains `Divide(object[] lhs, object rhs)` method
2. **StringOps.cs**: Contains `Divide(string lhs, object rhs)` method  
3. **Binders.cs**: Routes division operator to appropriate handler
4. **Compiler.cs**: Provides cached reflection for performance

### Error Handling

- **Invalid Divisors**: Zero, negative, or non-numeric divisors throw `ArgumentException`
- **Empty Arrays**: Empty divisor arrays throw `ArgumentException`
- **Type Conversion**: Attempts conversion of compatible types (e.g., "2" → 2)

### Performance Characteristics

- **Time Complexity**: O(n) where n is the input length
- **Space Complexity**: O(n) for output storage
- **Memory Efficiency**: Minimal temporary object allocation
- **Caching**: Reflection calls are cached for repeated operations

## Examples

### Basic Array Division
```powershell
PS> @(1,2,3,4,5,6) / 2
1 2 3
4 5 6

PS> @("a","b","c","d","e") / 3  
a b
c d
e
```

### Custom Size Division
```powershell
PS> @(1,2,3,4,5,6,7,8) / @(2,3,2)
1 2
3 4 5  
6 7
8

PS> "PowerShell" / @(3,4,2)
Pow
erSh
el
l
```

### Mixed Data Types
```powershell
PS> @("hello", 123, $true, "world") / 2
hello 123
True world
```

## Backward Compatibility

This feature maintains full backward compatibility:
- All existing numeric division operations continue to work unchanged
- No changes to error messages for unsupported type combinations
- PowerShell's type coercion rules are respected
- Pipeline and parameter binding remain unaffected

## Testing

### Test Coverage
- Unit tests for all division scenarios
- Edge case handling (empty inputs, single elements)
- Error condition validation
- Performance regression tests
- Cross-platform compatibility verification

### Test Files
- `test/powershell/Language/Operators/ArrayDivision.Tests.ps1`
- `test/powershell/Language/Operators/StringDivision.Tests.ps1`
- `test/powershell/Language/Operators/DivisionOperatorIntegration.Tests.ps1`

## Future Considerations

### Potential Extensions
1. **Matrix Division**: Support for 2D array splitting
2. **Pattern-based Division**: Regex or delimiter-based string splitting
3. **Stream Support**: IEnumerable division for large datasets
4. **Custom Separators**: String division with specific delimiters

### Performance Improvements  
1. **Lazy Evaluation**: Defer computation until groups are accessed
2. **Memory Pooling**: Reuse arrays for large operations
3. **SIMD Operations**: Vectorized processing for numeric arrays

## References

- [PowerShell Operator Documentation](https://learn.microsoft.com/powershell/module/microsoft.powershell.core/about/about_operators)
- [PowerShell Type System](https://learn.microsoft.com/powershell/scripting/lang-spec/chapter-04)
- [Dynamic Language Runtime](https://docs.microsoft.com/dotnet/framework/reflection-and-codedom/dynamic-language-runtime-overview)

---

**Author**: PowerShell Language Team  
**Status**: Implemented  
**Version**: PowerShell 7.5+
